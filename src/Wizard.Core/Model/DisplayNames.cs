namespace Wizard.Core;

/// <summary>Display names. The Os, Ide, Cli and BuildServer names match the old docs/wiz pages (plan Appendix C).</summary>
public static class DisplayNames
{
    public static string Name(this Os os) =>
        os switch
        {
            Os.Windows => "Windows",
            Os.MacOS => "MacOS",
            Os.Linux => "Linux",
            _ => throw new ArgumentOutOfRangeException(nameof(os), os, null)
        };

    public static string Name(this Ide ide) =>
        ide switch
        {
            Ide.VisualStudio => "Visual Studio",
            Ide.VisualStudioWithReSharper => "Visual Studio with ReSharper",
            Ide.Rider => "JetBrains Rider",
            Ide.VsCode => "Visual Studio Code",
            Ide.Other => "Other",
            _ => throw new ArgumentOutOfRangeException(nameof(ide), ide, null)
        };

    public static string Name(this CliPreference cli) =>
        cli switch
        {
            CliPreference.Cli => "Prefer CLI",
            CliPreference.Gui => "Prefer GUI",
            _ => throw new ArgumentOutOfRangeException(nameof(cli), cli, null)
        };

    public static string Name(this TestFramework framework) =>
        framework switch
        {
            TestFramework.XunitV3 => "xUnit v3",
            TestFramework.NUnit => "NUnit",
            TestFramework.TUnit => "TUnit",
            TestFramework.MSTest => "MSTest",
            TestFramework.Fixie => "Fixie",
            TestFramework.Expecto => "Expecto",
            _ => throw new ArgumentOutOfRangeException(nameof(framework), framework, null)
        };

    public static string Name(this BuildServer buildServer) =>
        buildServer switch
        {
            BuildServer.GitHubActions => "GitHub Actions",
            BuildServer.AzureDevOps => "Azure DevOps",
            BuildServer.AppVeyor => "AppVeyor",
            BuildServer.None => "No build server",
            _ => throw new ArgumentOutOfRangeException(nameof(buildServer), buildServer, null)
        };

    public static string Name(this SponsorMode mode) =>
        mode switch
        {
            SponsorMode.NotChosen => "Decide later",
            SponsorMode.Sponsor => "Sponsoring",
            SponsorMode.Exempt => "Exempt",
            SponsorMode.PrivateArrangement => "Private arrangement",
            SponsorMode.Ignore => "Opting out",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

    public static string Name(this Flow flow) =>
        flow switch
        {
            Flow.New => "New project",
            Flow.Add => "Add plugins",
            Flow.AddByTech => "Add by tech stack",
            _ => throw new ArgumentOutOfRangeException(nameof(flow), flow, null)
        };

    /// <summary>
    /// IDEs offered per OS: the old wizard's list (Visual Studio and ReSharper only on Windows) plus
    /// VS Code everywhere, since the text-file-settings content has VS Code specific guidance.
    /// </summary>
    public static IReadOnlyList<Ide> IdesFor(Os os)
    {
        if (os == Os.Windows)
        {
            return [Ide.VisualStudio, Ide.VisualStudioWithReSharper, Ide.Rider, Ide.VsCode, Ide.Other];
        }

        return [Ide.Rider, Ide.VsCode, Ide.Other];
    }

    public static bool UsesJetBrains(this Ide ide) =>
        ide is Ide.Rider or Ide.VisualStudioWithReSharper;
}
