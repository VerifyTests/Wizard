/// <summary>Snapshots of everything the generators produce (plan 17.1). The clock is frozen.</summary>
public class GeneratorTests
{
    public static readonly Date Today = new(2026, 9, 22);

    public static WizardState State(
        TestFramework framework = TestFramework.XunitV3,
        BuildServer buildServer = BuildServer.GitHubActions,
        Os os = Os.Windows,
        Ide ide = Ide.Rider,
        CliPreference cli = CliPreference.Cli) =>
        new()
        {
            Flow = Flow.New,
            Os = os,
            Ide = ide,
            Cli = cli,
            TestFramework = framework,
            BuildServer = buildServer,
            SponsorMode = SponsorMode.Exempt,
            Exemption = Exemption.OpenSource,
            SponsorUntil = "2027-09",
            Step = "output"
        };

    /// <summary>A copy of the state with exactly these plugins selected.</summary>
    public static WizardState WithPlugins(WizardState state, params string[] ids)
    {
        var copy = state with
        {
            SelectedPlugins = new HashSet<string>(ids, StringComparer.Ordinal)
        };
        copy.Normalize();
        return copy;
    }

    /// <summary>An add-flow state: what the project has, and what is being added to it.</summary>
    public static WizardState Addition(
        Flow flow,
        string[] existing,
        string[] added,
        TestFramework framework = TestFramework.XunitV3)
    {
        var state = new WizardState
        {
            Flow = flow,
            TestFramework = framework,
            ExistingPlugins = new HashSet<string>(existing, StringComparer.Ordinal),
            SelectedPlugins = new HashSet<string>(added, StringComparer.Ordinal),
            Step = "output"
        };
        state.Normalize();
        return state;
    }

    /// <summary>
    /// Every package at 1.0.0. The baked file is refreshed weekly (plan 15.2), and snapshots taken with
    /// it would all change each time; that file's own diff is where a version change is reviewed.
    /// </summary>
    public static readonly PackageVersions Versions =
        PackageVersions.Create(Today, PackageVersions.Baked.Ids.ToDictionary(_ => _, _ => "1.0.0"));

    static Plan PlanFor(WizardState state) =>
        Plan.Build(state, Versions, Today);

    [Test]
    [MatrixDataSource]
    public Task Guide(TestFramework framework, BuildServer buildServer) =>
        Verify(DocsGenerator.Build(PlanFor(State(framework, buildServer))), "md")
            .UseParameters(framework, buildServer);

    [Test]
    [Arguments(Os.Windows, Ide.VisualStudio, CliPreference.Gui)]
    [Arguments(Os.Windows, Ide.VisualStudioWithReSharper, CliPreference.Cli)]
    [Arguments(Os.MacOS, Ide.Rider, CliPreference.Gui)]
    [Arguments(Os.Linux, Ide.VsCode, CliPreference.Cli)]
    [Arguments(Os.Linux, Ide.Other, CliPreference.Gui)]
    public Task GuideEnvironment(Os os, Ide ide, CliPreference cli) =>
        Verify(DocsGenerator.Build(PlanFor(State(os: os, ide: ide, cli: cli))), "md");

    [Test]
    [MatrixDataSource]
    public Task SolutionFiles(TestFramework framework) =>
        Verify(Render(SolutionGenerator.Build(PlanFor(State(framework)))))
            .UseParameters(framework);

    [Test]
    [MatrixDataSource]
    public Task Ai(TestFramework framework) =>
        Verify(AiContentGenerator.Build(PlanFor(State(framework))), "md")
            .UseParameters(framework);

    /// <summary>
    /// Inline snapshots (plan 12.8): the initializer switch, the core and DiffPlex samples carrying
    /// their snapshots as literals, no verified files, and the guide and AI text that go with them.
    /// </summary>
    [Test]
    [Arguments(TestFramework.XunitV3)]
    [Arguments(TestFramework.Expecto)]
    public Task Inline(TestFramework framework)
    {
        var plan = PlanFor(State(framework) with {InlineSnapshots = true});
        return Verify(
                $"""
                 ==== readme.md

                 {DocsGenerator.Build(plan)}
                 ==== CLAUDE.md

                 {AiContentGenerator.Build(plan)}
                 {Render(SolutionGenerator.Build(plan).Where(_ => _.Path.EndsWith(".cs") || _.Path.EndsWith(".fs") || _.Path.Contains(".verified.")))}
                 """)
            .UseParameters(framework);
    }

    [Test]
    public Task InlineAddition() =>
        Verify(Render(SolutionGenerator.Build(PlanFor(Addition(Flow.Add, [], ["DiffPlex"]) with {InlineSnapshots = true}))));

    /// <summary>Visual Studio without ReSharper, and no build server, drop the JetBrains settings and the build definition.</summary>
    [Test]
    public Task SolutionFileListMinimal() =>
        Verify(SolutionGenerator.Build(PlanFor(State(buildServer: BuildServer.None, ide: Ide.VisualStudio))).Select(_ => _.Path));

    [Test]
    public async Task ZipHasTheSolutionUnderOneFolder()
    {
        var plan = PlanFor(State());
        var files = SolutionGenerator.Build(plan);
        var zip = ZipBuilder.Build(plan.SolutionName, files);

        await using var archive = new ZipArchive(new MemoryStream(zip));
        var entries = archive.Entries.Select(_ => _.FullName).ToList();
        await Assert.That(entries.Count).IsEqualTo(files.Count);
        await Assert.That(entries.All(_ => _.StartsWith("VerifySample/", StringComparison.Ordinal))).IsTrue();

        // same input, same bytes
        await Assert.That(ZipBuilder.Build(plan.SolutionName, files).SequenceEqual(zip)).IsTrue();
    }

    [Test]
    public async Task VerifiedFileHasBomAndNoTrailingNewline()
    {
        var files = SolutionGenerator.Build(PlanFor(State()))
            .Where(_ => _.Path.EndsWith(".verified.txt", StringComparison.Ordinal))
            .ToList();
        // The core sample's snapshot, and any plugin sample whose output is known (plan D6).
        await Assert.That(files).IsNotEmpty();
        foreach (var file in files)
        {
            var bytes = file.ToBytes();
            await Assert.That(bytes.Take(3).SequenceEqual(new byte[] {0xEF, 0xBB, 0xBF}))
                .IsTrue()
                .Because($"{file.Path} should start with a byte order mark");
            await Assert.That((char) bytes[^1])
                .IsNotEqualTo('\n')
                .Because($"{file.Path} should have no trailing newline");
        }
    }

    public static IEnumerable<Func<(string Name, WizardState State)>> SponsorStates()
    {
        yield return () => ("NotChosen", State() with {SponsorMode = SponsorMode.NotChosen});
        yield return () => ("Sponsor", SponsorState());
        yield return () => ("SponsorNew", SponsorState(start: new Date(2026, 9, 1)));
        yield return () => ("SponsorPrivate", SponsorState(privateUntil: "2027-03"));
        yield return () => ("ExemptOpenSource", State());
        yield return () => ("ExemptConsulting", State() with {Exemption = Exemption.MaintainerConsulting, SponsorUntil = "2027-03"});
        yield return () => ("PrivateArrangement", State() with {SponsorMode = SponsorMode.PrivateArrangement, SponsorUntil = "2027-06"});
        yield return () => ("Ignore", State() with {SponsorMode = SponsorMode.Ignore});
    }

    static WizardState SponsorState(Date? start = null, string privateUntil = "") =>
        State() with
        {
            SponsorMode = SponsorMode.Sponsor,
            SponsorAccount = "acme",
            SponsorshipStart = start,
            SponsorshipPrivateUntil = privateUntil
        };

    [Test]
    [MethodDataSource(nameof(SponsorStates))]
    public Task Sponsor((string Name, WizardState State) sponsor)
    {
        var plan = PlanFor(sponsor.State);
        return Verify(
                $"""
                 {ProjectFiles.DirectoryBuildProps(plan)}
                 Summary: {SponsorRules.Summary(plan.State)}

                 Outcome: {SponsorRules.Outcome(plan.State)}

                 Url: {WizardStateUrl.ToQuery(plan.State)}
                 """)
            .UseParameters(sponsor.Name);
    }

    /// <summary>
    /// Every plugin on its own, at both depths (plan 17.1): the module initializer, the test file
    /// and the packages it adds. One snapshot per plugin makes a registry edit reviewable.
    /// </summary>
    [Test]
    [MethodDataSource(nameof(EachPlugin))]
    public Task Plugin((string Id, Depth Depth) plugin)
    {
        var state = WithPlugins(State(), plugin.Id);
        state.SetDepth(plugin.Id, plugin.Depth);
        var plan = PlanFor(state);
        var files = SolutionGenerator.Build(plan)
            .Where(_ => _.Path.Contains("/Plugins/") || _.Path.EndsWith("ModuleInitializer.cs", StringComparison.Ordinal));

        return Verify(
                $"""
                 {Render(files)}
                 ==== packages

                 {string.Join('\n', plan.AllPackages)}

                 ==== interactions

                 {string.Join('\n', plan.Interactions.Select(_ => $"{_.Severity} {_.RuleId}: {_.Message}"))}
                 """)
            .UseParameters($"{plugin.Id}-{plugin.Depth}");
    }

    public static IEnumerable<Func<(string Id, Depth Depth)>> EachPlugin()
    {
        foreach (var definition in Plugins.All)
        {
            foreach (var depth in new[] {Depth.Minimal, Depth.Verbose})
            {
                var id = definition.Id;
                yield return () => (id, depth);
            }
        }
    }

    /// <summary>The combinations the interaction rules exist for (plan 17.1).</summary>
    public static IEnumerable<Func<(string Name, string[] Ids)>> Combinations()
    {
        yield return () => ("EfAndSql", ["DiffPlex", "EntityFramework", "SqlServer"]);
        yield return () => ("BunitAndAngleSharp", ["AngleSharp", "Bunit", "DiffPlex"]);
        yield return () => ("Recording", ["EntityFramework", "Http", "MicrosoftLogging", "SqlServer"]);
        yield return () => ("Windows", ["DiffPlex", "WinForms", "Xaml"]);
        yield return () => ("Everything", [.. Plugins.All.Select(_ => _.Id)]);
    }

    /// <summary>
    /// Ids the registry does not have yet are dropped by Normalize, so a combination still produces a
    /// reviewable snapshot while the registry is being filled in.
    /// </summary>
    [Test]
    [MethodDataSource(nameof(Combinations))]
    public Task Combination((string Name, string[] Ids) combination)
    {
        var plan = PlanFor(WithPlugins(State(), combination.Ids));
        return Verify(
                $"""
                 ==== selected

                 {string.Join('\n', plan.Plugins.Select(_ => _.Id))}

                 ==== interactions

                 {string.Join("\n\n", plan.Interactions.Select(_ => $"{_.Severity} {_.RuleId} [{string.Join(", ", _.Involved)}]\n{_.Message}"))}

                 {Render(SolutionGenerator.Build(plan).Where(_ => _.Path.EndsWith("ModuleInitializer.cs", StringComparison.Ordinal)))}
                 ==== files

                 {string.Join('\n', SolutionGenerator.Build(plan).Select(_ => _.Path))}
                 """)
            .UseParameters(combination.Name);
    }

    /// <summary>
    /// A conflicting selection still generates, so the output step can show what it would produce;
    /// the plugin step is what stops the user moving on (plan 11.1).
    /// </summary>
    [Test]
    public async Task ConflictingSelectionStillGenerates()
    {
        var plan = PlanFor(WithPlugins(State(), "Diagnostics", "OpenTelemetry"));
        await Assert.That(plan.Blocked).IsTrue();
        await Assert.That(SolutionGenerator.Build(plan)).IsNotEmpty();
    }

    // Keyed by name, so the test's display name is the name rather than the whole state.
    static Dictionary<string, Func<WizardState>> additions = new()
    {
        // The case the interaction rules were written for: EF Core added next to an existing SqlServer,
        // whose recording the project's own initializer now has to turn off.
        ["EfNextToExistingSql"] = () => Addition(Flow.Add, ["DiffPlex", "SqlServer"], ["EntityFramework"]),
        // An existing plugin plugin discovery never found, which the project may never have enabled.
        ["ExistingUndiscovered"] = () => Addition(Flow.Add, ["AngleSharp"], ["Bunit"]),
        // A package with a maintenance fee check of its own, into a project whose Verify declaration
        // is left alone.
        ["TransitiveSponsorship"] = () => Addition(Flow.Add, [], ["OpenXml"]),
        ["ChangedDeclaration"] = () => Addition(Flow.Add, [], ["Http"]) with
        {
            SponsorMode = SponsorMode.Exempt,
            Exemption = Exemption.SmallRevenue,
            SponsorUntil = "2027-09"
        },
        ["Windows"] = () => Addition(Flow.Add, [], ["WinForms", "Terminal"], TestFramework.NUnit),
        ["Expecto"] = () => Addition(Flow.Add, [], ["Http", "EntityFramework"], TestFramework.Expecto),
        ["ByTech"] = () =>
        {
            var state = Addition(Flow.AddByTech, ["DiffPlex"], []);
            TechSuggestions.Choose(state, "aspnetcore", true);
            return state;
        }
    };

    public static IEnumerable<string> Additions() =>
        additions.Keys;

    /// <summary>Every file the add flows download, and the guide and AI instructions with them (plan 12.5).</summary>
    [Test]
    [MethodDataSource(nameof(Additions))]
    public Task Addition(string name)
    {
        var plan = PlanFor(additions[name]());
        return Verify(
                $"""
                 ==== interactions

                 {string.Join("\n", plan.Interactions.Select(_ => $"{_.Severity} {_.RuleId} [{string.Join(", ", _.Involved)}]"))}

                 {Render(SolutionGenerator.Build(plan))}
                 """)
            .UseParameters(name);
    }

    [Test]
    public async Task AdditionsZipUnderTheirOwnFolder()
    {
        var plan = PlanFor(Addition(Flow.Add, [], ["Http"]));
        await Assert.That(plan.ZipRoot).IsEqualTo("verify-additions");
        await using var archive = new ZipArchive(new MemoryStream(ZipBuilder.Build(plan.ZipRoot, SolutionGenerator.Build(plan))));
        await Assert.That(archive.Entries.All(_ => _.FullName.StartsWith("verify-additions/", StringComparison.Ordinal))).IsTrue();
    }

    static string Render(IEnumerable<GeneratedFile> files)
    {
        var builder = new StringBuilder();
        foreach (var file in files)
        {
            builder.Append($"==== {file.Path}");
            if (file.Bom)
            {
                builder.Append(" (UTF-8 with BOM)");
            }

            builder.Append("\n\n");
            builder.Append(file.Text.TrimEnd('\n'));
            builder.Append("\n\n");
        }

        return builder.ToString();
    }
}
