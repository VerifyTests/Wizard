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
        var directory = await Generate(State(framework), framework.ToString());

        await Run(directory, "build --configuration Release");
        // Plan.TestCommand, as the generated guide and build definitions run it
        var output = await Run(directory, "test --configuration Release --no-build");

        var received = Directory.EnumerateFiles(directory, "*.received.*", SearchOption.AllDirectories).ToList();
        await Assert.That(received).IsEmpty();

        // An undiscovered test is not a failure, so count them: the core sample, the conventions check,
        // and every plugin sample that ships its snapshot. The default selection is Verify.DiffPlex,
        // whose sample verifies a literal string, so a first run of the download is green.
        var expected = 2 + Plan
            .Build(State(framework), PackageVersions.Baked, Date.FromDateTime(DateTime.UtcNow))
            .Plugins
            .SelectMany(_ => _.Samples)
            .Count(_ => _.VerifiedOutput != null);
        await Assert.That(PassedCount(output)).IsEqualTo(expected).Because(output);
    }

    /// <summary>
    /// Every plugin on its own, at verbose depth, has to compile. This is the guarantee behind the
    /// samples: they are copied from readmes, which drift, and only the compiler notices (plan 17.4).
    /// The tests are not run, because most plugins need a database, a browser or a licence; the core
    /// solution above covers running.
    /// </summary>
    [Test]
    [MethodDataSource(nameof(EveryPlugin))]
    public async Task PluginSolutionBuilds(string id)
    {
        var state = State(TestFramework.XunitV3) with
        {
            SelectedPlugins = new HashSet<string>([id], StringComparer.Ordinal)
        };
        state.Normalize();

        var directory = await Generate(state, $"plugin-{id}");
        await Run(directory, "build --configuration Release");
    }

    public static IEnumerable<Func<string>> EveryPlugin() =>
        Plugins.All
            .Where(_ => _.Platform == Platform.CrossPlatform || OperatingSystem.IsWindows())
            .Select<PluginDefinition, Func<string>>(_ => () => _.Id);

    /// <summary>
    /// The combinations the interaction rules exist for, which is where a wrong ordering or a method
    /// that two packages both define shows up as a compile error (plan A16).
    /// </summary>
    [Test]
    [MethodDataSource(nameof(Combinations))]
    public async Task CombinationBuilds((string Name, string[] Ids) combination)
    {
        var state = State(TestFramework.XunitV3) with
        {
            SelectedPlugins = new HashSet<string>(combination.Ids, StringComparer.Ordinal)
        };
        state.Normalize();

        var directory = await Generate(state, $"combination-{combination.Name}");
        await Run(directory, "build --configuration Release");
    }

    public static IEnumerable<Func<(string Name, string[] Ids)>> Combinations()
    {
        yield return () => ("EfAndSql", ["DiffPlex", "EntityFramework", "SqlServer"]);
        yield return () => ("BunitAndAngleSharp", ["AngleSharp", "Bunit", "DiffPlex"]);
        // Blazor's Render initializes the plugin from its static constructor, which throws once any
        // verification has run, so the core sample and a Blazor test in one assembly is the case to
        // prove (plan A2).
        yield return () => ("BlazorAndCore", ["AngleSharp", "Blazor", "DiffPlex"]);
        // Both define PagesToInclude and SkipPdfNormalization in the VerifyTests namespace (plan A6).
        yield return () => ("QuestPdfAndPdfPig", ["PdfPig", "QuestPDF"]);
        // Verify.Flurl is built against an older Verify.Http than the one pinned here (plan A11).
        yield return () => ("FlurlAndHttp", ["Flurl", "Http"]);
        yield return () => ("Recording", ["EntityFramework", "Http", "MicrosoftLogging", "SqlServer"]);
    }

    static WizardState State(TestFramework framework)
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
            // SmallRevenue rather than OpenSource: a few plugins depend on a package with a
            // maintenance fee check of its own, and not every owner offers an open source exemption,
            // so the declaration would not carry over and the build would stop for a decision.
            Exemption = Exemption.SmallRevenue
        };
        SponsorRules.ApplyDefaults(state, Date.FromDateTime(DateTime.UtcNow));
        return state;
    }

    static async Task<string> Generate(WizardState state, string name)
    {
        var today = Date.FromDateTime(DateTime.UtcNow);
        var plan = Plan.Build(state, PackageVersions.Baked, today);

        var directory = Path.Combine(Path.GetTempPath(), "VerifyWizardIntegration", name);
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

        return directory;
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
