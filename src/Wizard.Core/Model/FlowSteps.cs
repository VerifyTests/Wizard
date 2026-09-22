namespace Wizard.Core;

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

    static readonly IReadOnlyList<StepDefinition> newFlow =
    [
        Os,
        Ide,
        Cli,
        TestFramework,
        BuildServer,
        Sponsor,
        Output
    ];

    public static IReadOnlyList<StepDefinition> For(Flow flow)
    {
        if (flow == Flow.New)
        {
            return newFlow;
        }

        throw new NotSupportedException($"The {flow} flow is added in a later phase (plan 20).");
    }

    /// <summary>
    /// The step a visitor can be on: the requested one, unless an earlier step is incomplete, in which
    /// case the first incomplete step. Keeps a shared or hand-edited url from landing past a gap.
    /// </summary>
    public static StepDefinition Reachable(WizardState state, Date today)
    {
        var steps = For(state.Flow);
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
