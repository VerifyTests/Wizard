// System.Xml.Linq, which the test project's implicit usings bring in, also has an Extensions class.
using Extensions = Wizard.Core.Extensions;

/// <summary>Invariants the extension registry has to hold (plan 9.2, 17.2).</summary>
public class RegistryTests
{
    [Test]
    public async Task IdsAreUnique()
    {
        var duplicates = Extensions.All
            .GroupBy(_ => _.Id, StringComparer.Ordinal)
            .Where(_ => _.Count() > 1)
            .Select(_ => _.Key);
        await Assert.That(duplicates).IsEmpty();
    }

    /// <summary>The whole registry as text, so any change to an entry is reviewed as a diff.</summary>
    [Test]
    public Task Registry() =>
        Verify(
            Extensions.All.Select(
                _ => new
                {
                    _.Id,
                    _.DisplayName,
                    _.Category,
                    Platform = _.Platform.ToString(),
                    Packages = _.Packages.Select(package => package.Id),
                    _.PluginType,
                    _.DiscoveredByInitializePlugins,
                    Initialize = _.Initialize.Select(statement => statement.Code),
                    _.Usings,
                    _.ExclusiveGroups,
                    Choices = _.Choices.Select(choice => choice.Id),
                    Minimal = _.MinimalSamples.Select(sample => sample.Name),
                    Verbose = _.VerboseSamples.Select(sample => sample.Name),
                    Requirements = _.ExternalRequirements.Select(requirement => requirement.Name)
                }));

    [Test]
    public Task Rules() =>
        Verify(
            new
            {
                InteractionRules.Groups,
                Rules = InteractionRules.Rules.Select(
                    _ => new
                    {
                        _.Id,
                        _.Severity,
                        _.All,
                        _.Any,
                        _.AnyCount,
                        _.Without,
                        Choice = _.Choice?.Id,
                        Order = _.Order.Select(edge => $"{edge.Before} before {edge.After}"),
                        _.RetiredBy
                    })
            });

    [Test]
    public async Task GroupMembersExist()
    {
        foreach (var group in InteractionRules.Groups)
        {
            foreach (var member in group.Members)
            {
                await Assert.That(Extensions.Contains(member))
                    .IsTrue()
                    .Because($"group '{group.Id}' names '{member}', which the registry does not have");
            }

            await Assert.That(group.Members.Count).IsGreaterThan(1);
        }
    }

    /// <summary>A group a member does not claim, or a claim no group honours, is a data mistake.</summary>
    [Test]
    public async Task GroupMembershipAgreesBothWays()
    {
        foreach (var extension in Extensions.All)
        {
            foreach (var id in extension.ExclusiveGroups)
            {
                var group = InteractionRules.Groups.SingleOrDefault(_ => _.Id == id);
                await Assert.That(group)
                    .IsNotNull()
                    .Because($"'{extension.Id}' claims group '{id}', which does not exist");
                await Assert.That(group!.Members)
                    .Contains(extension.Id)
                    .Because($"'{extension.Id}' claims group '{id}', which does not list it");
            }
        }
    }

    /// <summary>A condition is meaningless unless the member it names declares that choice.</summary>
    [Test]
    public async Task GroupConditionsNameARealChoice()
    {
        foreach (var group in InteractionRules.Groups)
        {
            foreach (var condition in group.Conditions)
            {
                var member = Extensions.ById[condition.Member];
                var choice = member.Choices.SingleOrDefault(_ => _.Id == condition.ChoiceId);
                await Assert.That(choice)
                    .IsNotNull()
                    .Because($"group '{group.Id}' conditions '{condition.Member}' on choice '{condition.ChoiceId}', which it does not declare");
                await Assert.That(choice!.Default).IsEqualTo(condition.Default);
                foreach (var value in condition.Values)
                {
                    await Assert.That(choice.Options.Select(_ => _.Value)).Contains(value);
                }
            }
        }
    }

    [Test]
    public async Task RulesNameRealExtensions()
    {
        foreach (var rule in InteractionRules.Rules)
        {
            foreach (var id in rule.All.Concat(rule.Any).Concat(rule.Without))
            {
                await Assert.That(Extensions.Contains(id))
                    .IsTrue()
                    .Because($"rule '{rule.Id}' names '{id}', which the registry does not have");
            }

            foreach (var edge in rule.Order)
            {
                await Assert.That(Extensions.Contains(edge.Before)).IsTrue();
                await Assert.That(Extensions.Contains(edge.After)).IsTrue();
            }

            foreach (var statements in rule.Replace.Concat(rule.Add))
            {
                await Assert.That(Extensions.Contains(statements.Target)).IsTrue();
                if (rule.Choice is { } choice)
                {
                    await Assert.That(choice.Options.Select(_ => _.Value)).Contains(statements.WhenValue);
                }
            }
        }
    }

    /// <summary>Every package the registry can emit needs a version, or generation throws.</summary>
    [Test]
    public async Task EveryPackageHasAVersion()
    {
        var known = PackageVersions.Baked.Ids.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = Extensions.All
            .SelectMany(_ => _.Packages)
            .Select(_ => _.Id)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(_ => !known.Contains(_))
            .Order(StringComparer.Ordinal);
        await Assert.That(missing).IsEmpty();
    }

    /// <summary>
    /// Core finds a plugin by looking for the assembly name without dots, so an extension whose class
    /// is named otherwise is never enabled by discovery. Those have to carry an explicit call (plan A1).
    /// </summary>
    [Test]
    public async Task UndiscoveredExtensionsAreInitializedExplicitly()
    {
        foreach (var extension in Extensions
                     .All
                     .Where(_ => _ is
                     {
                         PluginType: not null,
                         DiscoveredByInitializePlugins: false
                     }))
        {
            await Assert.That(extension.Initialize)
                .IsNotEmpty()
                .Because($"InitializePlugins() cannot find '{extension.Id}' (its type is {extension.PluginType}, not {extension.ExpectedPluginType}), so it needs an explicit call");
        }
    }

    [Test]
    public async Task ChoiceIdsAreUniqueAcrossExtensionsAndRules()
    {
        var ids = Extensions.All
            .SelectMany(_ => _.Choices)
            .Concat(InteractionRules.Rules.Where(_ => _.Choice != null).Select(_ => _.Choice!))
            .GroupBy(_ => _.Id, StringComparer.Ordinal)
            .Where(_ => _.Select(choice => choice).Distinct().Count() > 1)
            .Select(_ => _.Key);
        await Assert.That(ids).IsEmpty();
    }

    /// <summary>
    /// Library files are written once per path, so two extensions sharing a path have to agree on the
    /// content: otherwise whichever sorts first would silently decide what the other one compiles against.
    /// </summary>
    [Test]
    public async Task LibraryFilesSharingAPathAgree()
    {
        var clashes = Extensions.All
            .SelectMany(_ => _.LibraryFiles.Select(file => (_.Id, file.Path, file.Content)))
            .GroupBy(_ => _.Path, StringComparer.Ordinal)
            .Where(_ => _.Select(file => file.Content).Distinct(StringComparer.Ordinal).Count() > 1)
            .Select(_ => $"{_.Key}: {string.Join(", ", _.Select(file => file.Id))}");
        await Assert.That(clashes).IsEmpty();
    }

    /// <summary>A statement gated on a choice the extension does not declare would never be emitted.</summary>
    [Test]
    public async Task StatementConditionsNameARealChoice()
    {
        foreach (var extension in Extensions.All)
        {
            foreach (var statement in extension.Initialize.Where(_ => _.WhenChoice != null))
            {
                var separator = statement.WhenChoice!.IndexOf(':');
                var choice = extension.Choices.SingleOrDefault(_ => _.Id == statement.WhenChoice[..separator]);
                await Assert.That(choice)
                    .IsNotNull()
                    .Because($"'{extension.Id}' gates a statement on '{statement.WhenChoice}', a choice it does not declare");
                foreach (var value in statement.WhenChoice[(separator + 1)..].Split('|'))
                {
                    await Assert.That(choice!.Options.Select(_ => _.Value)).Contains(value);
                }
            }
        }
    }

    /// <summary>
    /// Shipping a snapshot for a test that never runs would leave a file nothing can ever confirm, and
    /// Verify's conventions check reports a verified file with no test behind it.
    /// </summary>
    [Test]
    public async Task ShippedSnapshotsBelongToSamplesThatRun()
    {
        var contradictions = Extensions.All
            .SelectMany(_ => _.SamplesFor(Depth.Verbose).Select(sample => (_.Id, sample)))
            .Where(_ => _.sample.VerifiedOutput != null && _.sample.SkipReason != null)
            .Select(_ => $"{_.Id}.{_.sample.Name}");
        await Assert.That(contradictions).IsEmpty();
    }

    /// <summary>A package that carries another owner's fee gate has to name it, or the build fails SC021.</summary>
    [Test]
    public async Task SponsorOwnersAreComplete()
    {
        foreach (var owner in Extensions.All.SelectMany(_ => _.Packages).Select(_ => _.SponsorOwner).OfType<SponsorOwner>())
        {
            await Assert.That(owner.Prefix).IsNotEmpty();
            await Assert.That(owner.Package).IsNotEmpty();
            await Assert.That(owner.Prefix).DoesNotContain("_");
        }
    }

    [Test]
    public async Task ChoicesHaveAtLeastTwoOptions()
    {
        foreach (var choice in Extensions.All.SelectMany(_ => _.Choices))
        {
            await Assert.That(choice.Options.Count).IsGreaterThan(1);
        }
    }

    /// <summary>Sample names become method names in one class, so a repeat would not compile.</summary>
    [Test]
    public async Task SampleNamesAreUniquePerExtension()
    {
        foreach (var extension in Extensions.All)
        {
            var duplicates = extension.SamplesFor(Depth.Verbose)
                .GroupBy(_ => _.Name, StringComparer.Ordinal)
                .Where(_ => _.Count() > 1)
                .Select(_ => $"{extension.Id}.{_.Key}");
            await Assert.That(duplicates).IsEmpty();
        }
    }

    /// <summary>
    /// Selecting everything at once is not a sensible project, but it is the worst case for the
    /// ordering solver: if the rules can produce a cycle, it is here.
    /// </summary>
    [Test]
    public async Task OrderingHasNoCycleWithEverythingSelected()
    {
        var state = GeneratorTests.State() with
        {
            SelectedExtensions = Extensions.All.Select(_ => _.Id).ToHashSet(StringComparer.Ordinal)
        };
        state.Normalize();
        var plan = Plan.Build(state, PackageVersions.Baked, GeneratorTests.Today);

        // Throws when the edges cycle, which is the assertion.
        var blocks = ModuleInitializerGenerator.Blocks(plan, windows: false);
        await Assert.That(blocks).IsNotNull();
    }

    /// <summary>Each rule fires for the combination it declares, and for neither part alone.</summary>
    [Test]
    [MethodDataSource(nameof(AllRules))]
    public async Task RuleFiresOnlyForItsCombination(InteractionRule rule)
    {
        var involved = rule.All.Concat(rule.Any.Take(rule.AnyCount)).Distinct(StringComparer.Ordinal).ToList();
        await Assert.That(rule.Fires(new HashSet<string>(involved, StringComparer.Ordinal)))
            .IsTrue()
            .Because($"'{rule.Id}' should fire for {string.Join(", ", involved)}");

        foreach (var single in involved.Where(_ => involved.Count > 1))
        {
            var alone = new HashSet<string>([single], StringComparer.Ordinal);
            await Assert.That(rule.Fires(alone))
                .IsFalse()
                .Because($"'{rule.Id}' should not fire for '{single}' alone");
        }
    }

    public static IEnumerable<Func<InteractionRule>> AllRules() =>
        InteractionRules.Rules.Select<InteractionRule, Func<InteractionRule>>(rule => () => rule);
}
