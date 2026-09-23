/// <param name="Id">The url value of <c>step</c>.</param>
/// <param name="Summary">The chosen value shown next to a completed step in the breadcrumb, or null when nothing is chosen.</param>
/// <param name="IsComplete">Whether the step's input is valid, which gates moving past it.</param>
public sealed record StepDefinition(
    string Id,
    string Title,
    Func<WizardState, string?> Summary,
    Func<WizardState, Date, bool> IsComplete);

/// <summary>The ordered steps of each flow (plan 7).</summary>
public static class FlowSteps
{
    public static readonly StepDefinition Os = new(
        "os",
        "Operating system",
        _ => _.Os?.Name(),
        (state, _) => state.Os != null);

    public static readonly StepDefinition Ide = new(
        "ide",
        "IDE",
        _ => _.Ide?.Name(),
        (state, _) => state.Ide != null);

    public static readonly StepDefinition Cli = new(
        "cli",
        "CLI or GUI",
        _ => _.Cli?.Name(),
        (state, _) => state.Cli != null);

    public static readonly StepDefinition TestFramework = new(
        "tf",
        "Test framework",
        _ => _.TestFramework?.Name(),
        (state, _) => state.TestFramework != null);

    public static readonly StepDefinition BuildServer = new(
        "ci",
        "Build server",
        _ => _.BuildServer?.Name(),
        (state, _) => state.BuildServer != null);

    /// <summary>Optional: choosing nothing just means no suggestions (plan 7.1 step 6).</summary>
    public static readonly StepDefinition Tech = new(
        "tech",
        "Tech stack",
        TechSummary,
        (_, _) => true);

    /// <summary>Optional: the plugins the project already has (plan 7.2 step 2).</summary>
    public static readonly StepDefinition Existing = new(
        "have",
        "Already using",
        ExistingSummary,
        (_, _) => true);

    public static readonly StepDefinition PluginsStep = new(
        "plugins",
        "Plugins",
        PluginSummary,
        // Two plugins that register the same thing for the same file extension have to be resolved
        // here: whichever the generated code initialized last would silently win.
        (state, _) => !InteractionRules.For(state).Any(_ => _.Severity == Severity.Conflict));

    public static readonly StepDefinition Options = new(
        "options",
        "Options",
        OptionsSummary,
        // Every option has a default, so there is nothing to gate on. The step always applies, since
        // the inline snapshot switch does not depend on the plugins.
        (_, _) => true);

    public static readonly StepDefinition Sponsor = new(
        "sponsor",
        "Maintenance fee",
        SponsorRules.Summary,
        (state, today) => SponsorRules.Errors(state, today).Count == 0);

    public static readonly StepDefinition Output = new(
        "output",
        "Result",
        _ => null,
        (_, _) => false);

    static string? PluginSummary(WizardState state)
    {
        var selected = Plugins.Selected(state);
        if (selected.Count == 0)
        {
            return "None";
        }

        if (selected.Count > 3)
        {
            return $"{selected.Count} plugins";
        }

        return string.Join(", ", selected.Select(_ => _.Id));
    }

    static string? TechSummary(WizardState state)
    {
        var count = state.Techs.Count;
        if (count == 0)
        {
            return "None";
        }

        if (count == 1)
        {
            return Techs.ById[state.Techs.First()].DisplayName;
        }

        return $"{count} techs";
    }

    static string? ExistingSummary(WizardState state)
    {
        var existing = Plugins.All.Where(_ => state.IsExisting(_.Id)).ToList();
        if (existing.Count == 0)
        {
            return "None";
        }

        if (existing.Count > 3)
        {
            return $"{existing.Count} plugins";
        }

        return string.Join(", ", existing.Select(_ => _.Id));
    }

    static string? OptionsSummary(WizardState state)
    {
        var minimal = state.SelectedPlugins.Count(_ => state.DepthOf(_) == Depth.Minimal);
        var changed = state.Choices.Count;
        var parts = new List<string>();
        if (state.InlineSnapshots)
        {
            parts.Add("inline");
        }

        if (minimal > 0)
        {
            parts.Add($"{minimal} minimal");
        }

        if (changed > 0)
        {
            parts.Add($"{changed} changed");
        }

        if (parts.Count == 0)
        {
            return "Defaults";
        }

        return string.Join(", ", parts);
    }

    static IReadOnlyList<StepDefinition> newFlow =
    [
        Os,
        Ide,
        Cli,
        TestFramework,
        BuildServer,
        Tech,
        PluginsStep,
        Options,
        Sponsor,
        Output
    ];

    // Adding to an existing project: the environment is already set up, so only the test framework is
    // asked, which decides the attributes in the generated tests (plan 7.2).
    static IReadOnlyList<StepDefinition> addFlow =
    [
        TestFramework,
        Existing,
        PluginsStep,
        Options,
        Sponsor,
        Output
    ];

    static IReadOnlyList<StepDefinition> addByTechFlow =
    [
        TestFramework,
        Existing,
        Tech,
        PluginsStep,
        Options,
        Sponsor,
        Output
    ];

    public static IReadOnlyList<StepDefinition> For(WizardState state) =>
        For(state.Flow);

    /// <summary>The steps of a flow, in order.</summary>
    public static IReadOnlyList<StepDefinition> For(Flow flow) =>
        flow switch
        {
            Flow.New => newFlow,
            Flow.Add => addFlow,
            Flow.AddByTech => addByTechFlow,
            _ => throw new ArgumentOutOfRangeException(nameof(flow), flow, null)
        };

    /// <summary>The step asked for, or the flow's first step when the flow does not have it.</summary>
    public static string Nearest(WizardState state, string stepId)
    {
        var steps = For(state);
        if (steps.Any(_ => _.Id == stepId))
        {
            return stepId;
        }

        return steps[0].Id;
    }

    /// <summary>
    /// The step a visitor can be on: the requested one, unless an earlier step is incomplete, in which
    /// case the first incomplete step. Keeps a shared or hand-edited url from landing past a gap.
    /// </summary>
    public static StepDefinition Reachable(WizardState state, Date today)
    {
        var steps = For(state);
        foreach (var step in steps)
        {
            if (step.Id == state.Step || !step.IsComplete(state, today))
            {
                return step;
            }
        }

        return steps[^1];
    }
}
