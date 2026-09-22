using System.Diagnostics;

namespace Wizard.Tests.Integration;

/// <summary>
/// Generates solutions, writes them to disk, and runs <c>dotnet build</c> and <c>dotnet test</c> on
/// them (plan 17.4). Slow and needs network for restore, so explicit: run by integration.yml, or with
/// <c>--treenode-filter "/*/*/GeneratedSolutionTests/*"</c>.
/// </summary>
[Explicit]
[NotInParallel]
public class GeneratedSolutionTests
{
    [Test]
    [MatrixDataSource]
    public async Task CoreSolutionBuildsAndPasses(TestFramework framework)
    {
        var state = new WizardState
        {
            Flow = Flow.New,
            Os = Os.Windows,
            Ide = Ide.Rider,
            Cli = CliPreference.Cli,
            TestFramework = framework,
            BuildServer = BuildServer.GitHubActions,
            SolutionName = "VerifySample",
            SponsorMode = SponsorMode.Exempt,
            Exemption = Exemption.OpenSource
        };
        var today = Date.FromDateTime(DateTime.UtcNow);
        SponsorRules.ApplyDefaults(state, today);
        var plan = Plan.Build(state, PackageVersions.Baked, today);

        var directory = Path.Combine(Path.GetTempPath(), "VerifyWizardIntegration", framework.ToString());
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }

        foreach (var file in SolutionGenerator.Build(plan))
        {
            var path = Path.Combine(directory, file.Path);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, file.ToBytes());
        }

        await Run(directory, "build --configuration Release");
        // Plan.TestCommand, as the generated guide and build definitions run it
        var output = await Run(directory, "test --configuration Release --no-build");

        var received = Directory.EnumerateFiles(directory, "*.received.*", SearchOption.AllDirectories).ToList();
        await Assert.That(received).IsEmpty();

        // An undiscovered test is not a failure, so count them: the sample and the conventions check.
        await Assert.That(PassedCount(output)).IsEqualTo(2).Because(output);
    }

    /// <summary>VSTest prints "Passed: n", Microsoft.Testing.Platform prints "succeeded: n".</summary>
    static int PassedCount(string output)
    {
        var match = Regex.Match(output, @"(?:Passed:|succeeded:)\s+(\d+)");
        if (match.Success)
        {
            return int.Parse(match.Groups[1].Value);
        }

        return 0;
    }

    static async Task<string> Run(string directory, string arguments)
    {
        var startInfo = new ProcessStartInfo("dotnet", arguments)
        {
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            Environment =
            {
                ["DiffEngine_Disabled"] = "true",
                ["DOTNET_NOLOGO"] = "true"
            }
        };
        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            throw new($"dotnet {arguments} failed with exit code {process.ExitCode} in {directory}:\n{await output}\n{await error}");
        }

        return await output;
    }
}
