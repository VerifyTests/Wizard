namespace Wizard.Core;

/// <summary>
/// One selectable Verify plugin (plan 9.1). Hand-written data, sourced from the per-plugin
/// research in plan-research/plugin-catalogue-*.md.
/// </summary>
public sealed record PluginDefinition
{
    /// <summary>Stable across urls and deep links, so it never changes once shipped.</summary>
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required string RepoUrl { get; init; }

    /// <summary>One line, shown on the plugin card and as the first line of the guide's section.</summary>
    public required string Description { get; init; }

    public required PluginCategory Category { get; init; }

    public required IReadOnlyList<PackageRequirement> Packages { get; init; }

    /// <summary>
    /// The assembly holding the plugin type, when it is not the first package id. Plugin discovery
    /// works off the assembly name, so this is what decides whether discovery finds it.
    /// </summary>
    public string? AssemblyName { get; init; }

    /// <summary>
    /// The public static class with <c>Initialized</c> and <c>Initialize()</c>, unqualified. Null when
    /// the plugin has none, which means it can only be enabled explicitly (plan A1, A2).
    /// </summary>
    public string? PluginType { get; init; }

    /// <summary>Set when <see cref="PluginType"/> is not public, which discovery also needs.</summary>
    public bool PluginTypeIsInternal { get; init; }

    public string PluginAssembly => AssemblyName ?? Packages[0].Id;

    /// <summary>The type name plugin discovery looks for: the assembly name without its dots.</summary>
    public string ExpectedPluginType => PluginAssembly.Replace(".", "");

    /// <summary>
    /// Core looks for the type <c>VerifyTests.</c> plus <see cref="ExpectedPluginType"/>, case
    /// sensitively, and silently skips a miss (plan A1). A plugin this returns false for is never
    /// enabled by <c>InitializePlugins()</c> alone, so the generated code always calls it explicitly.
    /// </summary>
    public bool DiscoveredByInitializePlugins =>
        PluginType == ExpectedPluginType &&
        !PluginTypeIsInternal;

    /// <summary>The §23 suggestion that would let a workaround here be removed (plan 23.7).</summary>
    public string? RetiredBy { get; init; }

    /// <summary>Statements for the module initializer, in the order they must run within this plugin.</summary>
    public IReadOnlyList<InitializeStatement> Initialize { get; init; } = [];

    /// <summary>Which block of the module initializer this plugin's statements belong to.</summary>
    public InitializePhase Phase { get; init; } = InitializePhase.Plugins;

    /// <summary>Helper members the statements need, such as EntityFramework's <c>GetDbModel()</c>.</summary>
    public IReadOnlyList<string> InitializeMembers { get; init; } = [];

    /// <summary>Namespaces the module initializer needs for the statements above.</summary>
    public IReadOnlyList<string> InitializeUsings { get; init; } = [];

    /// <summary>Namespaces the generated test file needs. Per file, never global: two plugins can
    /// define the same type name (plan A7).</summary>
    public IReadOnlyList<string> Usings { get; init; } = [];

    /// <summary>Properties the test project needs, such as <c>UseWPF</c>.</summary>
    public IReadOnlyList<(string Name, string Value)> ProjectProperties { get; init; } = [];

    /// <summary>Raw item xml the test project needs, such as a <c>FrameworkReference</c>.</summary>
    public IReadOnlyList<string> ProjectItems { get; init; } = [];

    /// <summary>
    /// The same for the class library, when <see cref="LibraryFiles"/> compile against something the
    /// library does not otherwise reference.
    /// </summary>
    public IReadOnlyList<string> LibraryProjectItems { get; init; } = [];

    public IReadOnlyList<ExternalRequirement> ExternalRequirements { get; init; } = [];

    public Platform Platform { get; init; } = Platform.CrossPlatform;

    /// <summary>Frameworks the samples cannot be generated for, with the reason (plan 11.2).</summary>
    public IReadOnlyList<(TestFramework Framework, string Reason)> UnsupportedTestFrameworks { get; init; } = [];

    /// <summary>Ids of <see cref="InteractionRules.Groups"/> this plugin belongs to (plan 11.1).</summary>
    public IReadOnlyList<string> ExclusiveGroups { get; init; } = [];

    public IReadOnlyList<PluginChoice> Choices { get; init; } = [];

    /// <summary>The one or two most common usages; always generated.</summary>
    public IReadOnlyList<Sample> MinimalSamples { get; init; } = [];

    /// <summary>Everything else the catalogue documents; generated at <see cref="Depth.Verbose"/> (plan D7).</summary>
    public IReadOnlyList<Sample> VerboseSamples { get; init; } = [];

    /// <summary>Types the samples exercise, generated into the class library.</summary>
    public IReadOnlyList<PluginFile> LibraryFiles { get; init; } = [];

    /// <summary>Gotchas, rendered under "Notes" in the guide and as comments in the test file.</summary>
    public IReadOnlyList<string> Notes { get; init; } = [];

    /// <summary>Set when no stable version exists on nuget.org.</summary>
    public bool Beta { get; init; }

    public IEnumerable<Sample> SamplesFor(Depth depth)
    {
        if (depth == Depth.Verbose)
        {
            return MinimalSamples.Concat(VerboseSamples);
        }

        return MinimalSamples;
    }

    public bool Supports(TestFramework framework) =>
        !UnsupportedTestFrameworks.Any(_ => _.Framework == framework);
}

public enum PluginCategory
{
    DeveloperExperience,
    Data,
    Web,
    Ui,
    Documents,
    Images,
    Messaging,
    Logging,
    Observability,
    Serialization,
    Email,
    Mocking,
    Compiler,
    Scrubbing,
    Testing,
    Tooling
}

public enum Platform
{
    CrossPlatform,
    WindowsOnly
}

/// <summary>The blocks of the generated module initializer, in the order they are emitted (plan 11.3).</summary>
public enum InitializePhase
{
    /// <summary>Settings a plugin reads while initializing, such as <c>UseStrictJson()</c>.</summary>
    Settings,

    /// <summary>Comparer registration, before the converters whose output is compared.</summary>
    Comparers,

    /// <summary>Plugins with parameters or an ordering constraint.</summary>
    Plugins,

    /// <summary>Everything that has to run once every plugin is initialized.</summary>
    AfterDiscovery
}

/// <param name="Code">One statement. <c>{choice-id}</c> is replaced with the chosen value.</param>
/// <param name="Comment">Lines emitted above the statement, without the leading slashes.</param>
public sealed record InitializeStatement(string Code, params string[] Comment)
{
    /// <summary>Statements emitted commented out at <see cref="Depth.Verbose"/>, to show the alternatives.</summary>
    public IReadOnlyList<string> Alternatives { get; init; } = [];

    /// <summary>
    /// Only emitted while a choice holds one of these values, as <c>choice-id:value|value</c>. For a
    /// registration the plugin makes in one role but not another.
    /// </summary>
    public string? WhenChoice { get; init; }

    /// <summary>
    /// Only emitted for these test frameworks; empty means all of them. For a call into a type that
    /// only one framework's integration package ships, such as LocalDb's test base.
    /// </summary>
    public IReadOnlyList<TestFramework> TestFrameworks { get; init; } = [];

    public bool AppliesTo(IReadOnlyDictionary<string, string> choices, TestFramework framework)
    {
        if (TestFrameworks.Count > 0 &&
            !TestFrameworks.Contains(framework))
        {
            return false;
        }

        if (WhenChoice == null)
        {
            return true;
        }

        var separator = WhenChoice.IndexOf(':');
        var id = WhenChoice[..separator];
        var values = WhenChoice[(separator + 1)..].Split('|');
        return values.Contains(choices.GetValueOrDefault(id), StringComparer.Ordinal);
    }
}

/// <param name="Kind">How the package is referenced.</param>
/// <param name="TestFrameworks">Only referenced for these frameworks; empty means all of them.</param>
/// <param name="WhenChoice">Only referenced when a choice has a value, as <c>choice-id:value</c>.</param>
public sealed record PackageRequirement(string Id)
{
    public PackageKind Kind { get; init; } = PackageKind.PackageReference;
    public IReadOnlyList<TestFramework> TestFrameworks { get; init; } = [];
    public string? WhenChoice { get; init; }

    /// <summary>Why the package is needed, when that is not obvious from its name.</summary>
    public string? Comment { get; init; }

    /// <summary>
    /// Set when the package, or something it depends on, carries a SponsorCheck gate of its own, so
    /// the build fails with SC021 until that owner's declaration is there too (plan A8).
    /// </summary>
    public SponsorOwner? SponsorOwner { get; init; }

    /// <summary>Referenced by the class library rather than the test project.</summary>
    public bool ForLibrary { get; init; }

    public bool AppliesTo(TestFramework framework, IReadOnlyDictionary<string, string> choices)
    {
        if (TestFrameworks.Count > 0 &&
            !TestFrameworks.Contains(framework))
        {
            return false;
        }

        if (WhenChoice == null)
        {
            return true;
        }

        var separator = WhenChoice.IndexOf(':');
        var id = WhenChoice[..separator];
        var value = WhenChoice[(separator + 1)..];
        return choices.GetValueOrDefault(id) == value;
    }
}

/// <param name="Prefix">The MSBuild property prefix, such as <c>Papyrine</c> in <c>Papyrine_SponsorshipExemption</c>.</param>
/// <param name="Package">The package whose build carries the gate, which is not always the one referenced.</param>
public sealed record SponsorOwner(string Prefix, string DisplayName, string Package)
{
    public string? SponsorsPage { get; init; }

    /// <summary>
    /// The exemptions this owner accepts, which are not the same set for every owner: Papyrine has no
    /// open source exemption, so a project exempt from Verify's fee on that ground is not exempt from
    /// this one. An exemption it does not list cannot be repeated for it.
    /// </summary>
    public IReadOnlyList<Exemption> Exemptions { get; init; } = [];

    public bool Accepts(Exemption? exemption) =>
        exemption != null &&
        Exemptions.Contains(exemption.Value);
}

public enum PackageKind
{
    PackageReference,
    DotnetTool
}

/// <param name="Name">Shown in the guide's "Before running" list.</param>
/// <param name="EnvironmentVariable">The variable a licence key or api key is read from.</param>
/// <param name="WindowsInstall">A command that installs it on a Windows build agent, when one exists.</param>
/// <param name="LinuxInstall">The same for a Linux agent.</param>
public sealed record ExternalRequirement(string Name, string Description)
{
    public string? Url { get; init; }
    public string? EnvironmentVariable { get; init; }
    public string? WindowsInstall { get; init; }
    public string? LinuxInstall { get; init; }

    /// <summary>Set when the samples cannot run unattended, so they are generated skipped (plan 11.2).</summary>
    public bool CannotRunUnattended { get; init; }
}

/// <param name="Id">The key in <see cref="WizardState.Choices"/> and in the <c>opt</c> url value.</param>
public sealed record PluginChoice(
    string Id,
    string Label,
    string Hint,
    IReadOnlyList<ChoiceOption> Options)
{
    /// <summary>The first option, used when nothing is chosen.</summary>
    public string Default => Options[0].Value;
}

public sealed record ChoiceOption(string Value, string Label, string Hint = "");

/// <param name="Name">The generated method name; also the snapshot's name.</param>
/// <param name="Body">The method body, without braces, indented from column zero.</param>
public sealed record Sample(string Name, string Body)
{
    /// <summary>What the sample shows, emitted above the method (plan 12.2).</summary>
    public IReadOnlyList<string> Comment { get; init; } = [];

    /// <summary>The body awaits, so the method is <c>async Task</c> rather than returning one.</summary>
    public bool Async { get; init; }

    /// <summary>Why the test cannot run unattended; generates the framework's skip attribute.</summary>
    public string? SkipReason { get; init; }

    /// <summary>Extra members the body needs, emitted after the method.</summary>
    public IReadOnlyList<string> Members { get; init; } = [];

    /// <summary>
    /// The snapshot this sample produces, when it is known exactly and does not depend on the package
    /// version or the machine. Shipping it means the sample passes on the first run instead of writing
    /// a received file; most samples cannot (plan D6), and the guide explains what to do with those.
    /// </summary>
    public string? VerifiedOutput { get; init; }
}

/// <param name="Path">Relative to the project the file belongs to.</param>
public sealed record PluginFile(string Path, string Content);
