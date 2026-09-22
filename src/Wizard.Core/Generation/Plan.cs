namespace Wizard.Core;

/// <summary>
/// The resolved view of a <see cref="WizardState"/> that every generator reads (plan 6.3): defaults
/// filled in, the framework data looked up, and every package paired with the version to emit.
/// Building it is pure; <paramref name="Today"/> is injected so output is reproducible.
/// </summary>
public sealed record Plan(
    WizardState State,
    Os Os,
    Ide Ide,
    CliPreference Cli,
    TestFrameworkInfo Framework,
    BuildServer BuildServer,
    PackageVersions Versions,
    Date Today)
{
    public string SolutionName => State.SolutionName;
    public string LibraryProject => SolutionName;
    public string TestProject => $"{SolutionName}.Tests";

    /// <summary>The url that reopens the wizard on this exact state's output.</summary>
    public string WizardUrl =>
        WizardStateUrl.ToAbsoluteUrl(State with {Step = FlowSteps.Output.Id});

    /// <summary>Packages the test project references, framework first, then extensions.</summary>
    public IReadOnlyList<string> TestPackages => [.. Framework.Packages, "Verify.DiffPlex"];

    public string Version(string packageId) => Versions[packageId];

    /// <summary>
    /// Missing answers fall back to the most common choice, so output can be previewed from any step
    /// and every generator can assume a complete state.
    /// </summary>
    public static Plan Build(WizardState state, PackageVersions versions, Date today) =>
        new(
            state,
            state.Os ?? Os.Windows,
            state.Ide ?? Ide.Rider,
            state.Cli ?? CliPreference.Cli,
            TestFrameworkInfo.For(state.TestFramework ?? TestFramework.XunitV3),
            state.BuildServer ?? BuildServer.GitHubActions,
            versions,
            today);

    /// <summary>How to run the tests from the solution directory. The same for every framework: MTP
    /// frameworks through the runner set in global.json, Fixie through VSTest.</summary>
    public const string TestCommand = "dotnet test";
}
