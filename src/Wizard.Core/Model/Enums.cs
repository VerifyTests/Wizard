// Member names are URL values (plan 8.1) and match the WizardGen enums in the Verify repo where they
// overlap, so the old docs/wiz file names map onto wizard urls (plan 19). Do not rename members.

public enum Flow
{
    New,
    Add,
    AddByTech
}

public enum Os
{
    Windows,
    MacOS,
    Linux
}

public enum Ide
{
    VisualStudio,
    VisualStudioWithReSharper,
    Rider,
    VsCode,
    Other
}

public enum CliPreference
{
    Cli,
    Gui
}

public enum TestFramework
{
    XunitV3,
    NUnit,
    TUnit,
    MSTest,
    Fixie,
    Expecto
}

public enum BuildServer
{
    GitHubActions,
    AzureDevOps,
    AppVeyor,
    None
}

/// <summary>How the build declares its Open Source Maintenance Fee status (plan 14).</summary>
public enum SponsorMode
{
    /// <summary>"Decide later": the declaration block is emitted commented out, so the first build fails with SC021.</summary>
    NotChosen,
    Sponsor,
    Exempt,
    PrivateArrangement,
    Ignore
}

/// <summary>How much of a plugin's API the generated samples cover (plan D7).</summary>
public enum Depth
{
    /// <summary>Everything the catalogue documents, one commented test per API. The default.</summary>
    Verbose,

    /// <summary>The enable call and the one or two most common usages.</summary>
    Minimal
}

public enum Exemption
{
    OpenSource,
    SmallRevenue,
    MaintainerConsulting
}
