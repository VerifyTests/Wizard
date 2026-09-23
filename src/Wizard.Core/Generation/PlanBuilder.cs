namespace Wizard.Core;

/// <summary>
/// Resolves the parts of a <see cref="Plan"/> that depend on the whole selection (plan 6.3): which
/// packages each extension contributes, which of its samples are generated, which statements survive a
/// rule, and the notices that follow from one extension's own data rather than from a combination.
/// </summary>
public static class PlanBuilder
{
    public static IReadOnlyList<ResolvedExtension> Resolve(WizardState state, TestFramework framework)
    {
        var resolved = new List<ResolvedExtension>();
        foreach (var definition in Extensions.Selected(state))
        {
            var depth = state.DepthOf(definition.Id);
            resolved.Add(
                new(
                    definition,
                    depth,
                    [.. definition.Packages.Where(_ => _.AppliesTo(framework, state.Choices))],
                    SamplesFor(definition, depth, framework),
                    InteractionRules.Replacement(state, definition.Id) ?? Substitute(definition.Initialize, state, framework)));
        }

        return resolved;
    }

    /// <summary>
    /// Samples are only generated for a framework the extension supports, and never for F#: the Expecto
    /// project gets the core sample only (plan D9).
    /// </summary>
    static IReadOnlyList<Sample> SamplesFor(ExtensionDefinition definition, Depth depth, TestFramework framework)
    {
        if (framework == TestFramework.Expecto ||
            !definition.Supports(framework))
        {
            return [];
        }

        return [.. definition.SamplesFor(depth)];
    }

    /// <summary>Replaces each <c>{choice-id}</c> with the value in force.</summary>
    static IReadOnlyList<InitializeStatement> Substitute(
        IReadOnlyList<InitializeStatement> statements,
        WizardState state,
        TestFramework framework)
    {
        if (statements.Count == 0)
        {
            return statements;
        }

        var values = ChoiceValues(state);
        return
        [
            .. statements
                .Where(_ => _.AppliesTo(values, framework))
                .Select(_ => _ with
                {
                    Code = Substitute(_.Code, values),
                    Alternatives = [.. _.Alternatives.Select(alternative => Substitute(alternative, values))]
                })
        ];
    }

    static string Substitute(string template, IReadOnlyDictionary<string, string> values)
    {
        foreach (var (id, value) in values)
        {
            template = template.Replace($"{{{id}}}", value);
        }

        return template;
    }

    /// <summary>Every choice in force, chosen or defaulted, from both extensions and rules.</summary>
    public static IReadOnlyDictionary<string, string> ChoiceValues(WizardState state)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var choice in AvailableChoices(state))
        {
            values[choice.Id] = state.Choices.GetValueOrDefault(choice.Id, choice.Default);
        }

        return values;
    }

    /// <summary>The choices the options step shows, in the order it shows them.</summary>
    public static IEnumerable<ExtensionChoice> AvailableChoices(WizardState state) =>
        Extensions.Selected(state)
            .SelectMany(_ => _.Choices)
            .Concat(InteractionRules.ChoicesFor(state.SelectedExtensions))
            .DistinctBy(_ => _.Id);

    /// <summary>
    /// Notices that follow from a single extension's own data: needing Windows, a licence key, an
    /// external tool, a framework it does not support, or an explicit call because plugin discovery
    /// cannot find it. Kept out of <see cref="InteractionRules"/>, which holds only combinations.
    /// </summary>
    public static IReadOnlyList<InteractionResult> Notices(WizardState state, Os os, TestFramework framework)
    {
        var notices = new List<InteractionResult>();
        var selected = Extensions.Selected(state);

        var windowsOnly = selected.Where(_ => _.Platform == Platform.WindowsOnly).Select(_ => _.Id).ToList();
        if (windowsOnly.Count > 0 &&
            os != Os.Windows)
        {
            notices.Add(
                new(
                    "windows-only",
                    Severity.Warning,
                    windowsOnly,
                    $"{InteractionRules.Join(windowsOnly)} only runs on Windows. The samples go in a second test " +
                    "project targeting windows, which the main one does not reference, so the rest of the " +
                    $"solution still builds and runs on {os.Name()}."));
        }

        // An extension with no plugin type at all, such as a dotnet tool, is not something discovery
        // could have found, so saying it was missed would be misleading.
        var undiscovered = selected
            .Where(_ => _.PluginType != null && !_.DiscoveredByInitializePlugins)
            .Select(_ => _.Id)
            .ToList();
        if (undiscovered.Count > 0)
        {
            notices.Add(
                new(
                    "plugin-not-discovered",
                    Severity.Info,
                    undiscovered,
                    $"VerifierSettings.InitializePlugins() cannot find {InteractionRules.Join(undiscovered)}: it " +
                    "looks for a type named after the assembly, and silently skips an assembly that has none. " +
                    "The module initializer calls each of them explicitly, so they are enabled either way."));
        }

        var unsupported = selected
            .Where(_ => !_.Supports(framework))
            .Select(_ => (_.Id, _.UnsupportedTestFrameworks.First(framework2 => framework2.Framework == framework).Reason))
            .ToList();
        foreach (var (id, reason) in unsupported)
        {
            notices.Add(
                new(
                    "test-framework-unsupported",
                    Severity.Warning,
                    [id],
                    $"{id} has no samples for {framework.Name()}: {reason} Everything else is still generated."));
        }

        foreach (var definition in selected)
        {
            foreach (var requirement in definition.ExternalRequirements)
            {
                var rule = "external-tool";
                if (requirement.EnvironmentVariable != null)
                {
                    rule = "licence-required";
                }

                notices.Add(
                    new(
                        rule,
                        Severity.Warning,
                        [definition.Id],
                        $"{definition.Id} needs {requirement.Name}: {requirement.Description}"));
            }
        }

        var beta = selected.Where(_ => _.Beta).Select(_ => _.Id).ToList();
        if (beta.Count > 0)
        {
            notices.Add(
                new(
                    "beta-package",
                    Severity.Info,
                    beta,
                    $"{InteractionRules.Join(beta)} has no stable release; the version used is a prerelease."));
        }

        return notices;
    }
}
