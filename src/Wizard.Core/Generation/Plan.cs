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
    /// <summary>The selected extensions in registry order, each resolved against the whole state.</summary>
    public required IReadOnlyList<ResolvedExtension> Extensions { get; init; }

    /// <summary>Every rule and group that fires, plus the notices derived from the extensions themselves.</summary>
    public required IReadOnlyList<InteractionResult> Interactions { get; init; }

    public string SolutionName => State.SolutionName;
    public string LibraryProject => SolutionName;
    public string TestProject => $"{SolutionName}.Tests";

    /// <summary>
    /// The second test project, for extensions that only run on Windows (plan D5). Nothing references
    /// it, so the rest of the solution builds and runs on any OS.
    /// </summary>
    public string WindowsTestProject => $"{SolutionName}.Tests.Windows";

    public IReadOnlyList<ResolvedExtension> WindowsExtensions =>
        [.. Extensions.Where(_ => _.IsWindowsOnly)];

    public IReadOnlyList<ResolvedExtension> PortableExtensions =>
        [.. Extensions.Where(_ => !_.IsWindowsOnly)];

    /// <summary>
    /// Not for Expecto: the samples are C# (plan D9), so a second project would hold nothing but an
    /// initializer, and the F# project's packages do not belong in a C# one.
    /// </summary>
    public bool HasWindowsProject =>
        WindowsExtensions.Count > 0 &&
        !Framework.IsFSharp;

    public IEnumerable<ResolvedExtension> ExtensionsIn(bool windows) =>
        Extensions.Where(_ => _.IsWindowsOnly == windows);

    public bool Blocked =>
        Interactions.Any(_ => _.Severity == Severity.Conflict);

    /// <summary>The url that reopens the wizard on this exact state's output.</summary>
    public string WizardUrl =>
        WizardStateUrl.ToAbsoluteUrl(State with {Step = FlowSteps.Output.Id});

    /// <summary>Packages the test project references, framework first, then the extensions in registry order.</summary>
    public IReadOnlyList<string> TestPackages =>
        [.. Framework.Packages, .. ExtensionPackages(windows: false)];

    public IReadOnlyList<string> WindowsTestPackages =>
        [.. Framework.Packages, .. ExtensionPackages(windows: true)];

    /// <summary>Every package version the solution has to pin, whichever project references it.</summary>
    public IReadOnlyList<string> AllPackages =>
        [.. TestPackages.Concat(WindowsTestPackages).Concat(LibraryPackages).Distinct(StringComparer.Ordinal)];

    public IReadOnlyList<string> LibraryPackages =>
        [
            .. Extensions
                .SelectMany(_ => _.Packages)
                .Where(_ => _.ForLibrary && _.Kind == PackageKind.PackageReference)
                .Select(_ => _.Id)
                .Distinct(StringComparer.Ordinal)
        ];

    // A package the class library needs is usually needed by the tests too, which build the same types
    // up, so ForLibrary adds a reference rather than moving one.
    IEnumerable<string> ExtensionPackages(bool windows) =>
        ExtensionsIn(windows)
            .SelectMany(_ => _.Packages)
            .Where(_ => _.Kind == PackageKind.PackageReference)
            .Select(_ => _.Id)
            .Distinct(StringComparer.Ordinal);

    /// <summary>Tools installed into <c>.config/dotnet-tools.json</c>.</summary>
    public IReadOnlyList<string> DotnetTools =>
        [
            .. Extensions
                .SelectMany(_ => _.Packages)
                .Where(_ => _.Kind == PackageKind.DotnetTool)
                .Select(_ => _.Id)
                .Distinct(StringComparer.Ordinal)
        ];

    public string Version(string packageId) => Versions[packageId];

    /// <summary>
    /// Missing answers fall back to the most common choice, so output can be previewed from any step
    /// and every generator can assume a complete state.
    /// </summary>
    public static Plan Build(WizardState state, PackageVersions versions, Date today)
    {
        var framework = state.TestFramework ?? TestFramework.XunitV3;
        var os = state.Os ?? Os.Windows;
        return new(
            state,
            os,
            state.Ide ?? Ide.Rider,
            state.Cli ?? CliPreference.Cli,
            TestFrameworkInfo.For(framework),
            state.BuildServer ?? BuildServer.GitHubActions,
            versions,
            today)
        {
            Extensions = PlanBuilder.Resolve(state, framework),
            Interactions =
            [
                .. InteractionRules.For(state)
                    .Concat(PlanBuilder.Notices(state, os, framework))
                    .OrderByDescending(_ => _.Severity)
                    .ThenBy(_ => _.RuleId, StringComparer.Ordinal)
            ]
        };
    }

    /// <summary>How to run the tests from the solution directory. The same for every framework: MTP
    /// frameworks through the runner set in global.json, Fixie through VSTest.</summary>
    public const string TestCommand = "dotnet test";
}
