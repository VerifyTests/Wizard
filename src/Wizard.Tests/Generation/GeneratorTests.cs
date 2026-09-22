namespace Wizard.Tests.Generation;

/// <summary>Snapshots of everything the generators produce (plan 17.1). The clock is frozen.</summary>
public class GeneratorTests
{
    public static readonly Date Today = new(2026, 9, 22);

    public static WizardState State(
        TestFramework framework = TestFramework.XunitV3,
        BuildServer buildServer = BuildServer.GitHubActions,
        Os os = Os.Windows,
        Ide ide = Ide.Rider,
        CliPreference cli = CliPreference.Cli) =>
        new()
        {
            Flow = Flow.New,
            Os = os,
            Ide = ide,
            Cli = cli,
            TestFramework = framework,
            BuildServer = buildServer,
            SponsorMode = SponsorMode.Exempt,
            Exemption = Exemption.OpenSource,
            SponsorUntil = "2027-09",
            Step = "output"
        };

    static Plan PlanFor(WizardState state) =>
        Plan.Build(state, PackageVersions.Baked, Today);

    [Test]
    [MatrixDataSource]
    public Task Guide(TestFramework framework, BuildServer buildServer) =>
        Verify(DocsGenerator.Build(PlanFor(State(framework, buildServer))), "md")
            .UseParameters(framework, buildServer);

    [Test]
    [Arguments(Os.Windows, Ide.VisualStudio, CliPreference.Gui)]
    [Arguments(Os.Windows, Ide.VisualStudioWithReSharper, CliPreference.Cli)]
    [Arguments(Os.MacOS, Ide.Rider, CliPreference.Gui)]
    [Arguments(Os.Linux, Ide.VsCode, CliPreference.Cli)]
    [Arguments(Os.Linux, Ide.Other, CliPreference.Gui)]
    public Task GuideEnvironment(Os os, Ide ide, CliPreference cli) =>
        Verify(DocsGenerator.Build(PlanFor(State(os: os, ide: ide, cli: cli))), "md");

    [Test]
    [MatrixDataSource]
    public Task SolutionFiles(TestFramework framework) =>
        Verify(Render(SolutionGenerator.Build(PlanFor(State(framework)))))
            .UseParameters(framework);

    [Test]
    [MatrixDataSource]
    public Task Ai(TestFramework framework) =>
        Verify(AiContentGenerator.Build(PlanFor(State(framework))), "md")
            .UseParameters(framework);

    /// <summary>Visual Studio without ReSharper, and no build server, drop the JetBrains settings and the build definition.</summary>
    [Test]
    public Task SolutionFileListMinimal() =>
        Verify(SolutionGenerator.Build(PlanFor(State(buildServer: BuildServer.None, ide: Ide.VisualStudio))).Select(_ => _.Path));

    [Test]
    public async Task ZipHasTheSolutionUnderOneFolder()
    {
        var plan = PlanFor(State());
        var files = SolutionGenerator.Build(plan);
        var zip = ZipBuilder.Build(plan.SolutionName, files);

        using var archive = new ZipArchive(new MemoryStream(zip));
        var entries = archive.Entries.Select(_ => _.FullName).ToList();
        await Assert.That(entries.Count).IsEqualTo(files.Count);
        await Assert.That(entries.All(_ => _.StartsWith("VerifySample/", StringComparison.Ordinal))).IsTrue();

        // same input, same bytes
        await Assert.That(ZipBuilder.Build(plan.SolutionName, files).SequenceEqual(zip)).IsTrue();
    }

    [Test]
    public async Task VerifiedFileHasBomAndNoTrailingNewline()
    {
        var files = SolutionGenerator.Build(PlanFor(State()));
        var verified = files.Single(_ => _.Path.EndsWith(".verified.txt", StringComparison.Ordinal));
        var bytes = verified.ToBytes();
        await Assert.That(bytes.Take(3).SequenceEqual(new byte[] {0xEF, 0xBB, 0xBF})).IsTrue();
        await Assert.That(bytes[^1]).IsEqualTo((byte) '}');
    }

    public static IEnumerable<Func<(string Name, WizardState State)>> SponsorStates()
    {
        yield return () => ("NotChosen", State() with {SponsorMode = SponsorMode.NotChosen});
        yield return () => ("Sponsor", SponsorState());
        yield return () => ("SponsorNew", SponsorState(start: new Date(2026, 9, 1)));
        yield return () => ("SponsorPrivate", SponsorState(privateUntil: "2027-03"));
        yield return () => ("ExemptOpenSource", State());
        yield return () => ("ExemptConsulting", State() with {Exemption = Exemption.MaintainerConsulting, SponsorUntil = "2027-03"});
        yield return () => ("PrivateArrangement", State() with {SponsorMode = SponsorMode.PrivateArrangement, SponsorUntil = "2027-06"});
        yield return () => ("Ignore", State() with {SponsorMode = SponsorMode.Ignore});
    }

    static WizardState SponsorState(Date? start = null, string privateUntil = "") =>
        State() with
        {
            SponsorMode = SponsorMode.Sponsor,
            SponsorAccount = "acme",
            SponsorshipStart = start,
            SponsorshipPrivateUntil = privateUntil
        };

    [Test]
    [MethodDataSource(nameof(SponsorStates))]
    public Task Sponsor((string Name, WizardState State) sponsor)
    {
        var plan = PlanFor(sponsor.State);
        return Verify(
                $"""
                 {ProjectFiles.DirectoryBuildProps(plan)}
                 Summary: {SponsorRules.Summary(plan.State)}

                 Outcome: {SponsorRules.Outcome(plan.State)}

                 Url: {WizardStateUrl.ToQuery(plan.State)}
                 """)
            .UseParameters(sponsor.Name);
    }

    static string Render(IEnumerable<GeneratedFile> files)
    {
        var builder = new StringBuilder();
        foreach (var file in files)
        {
            builder.Append($"==== {file.Path}");
            if (file.Bom)
            {
                builder.Append(" (UTF-8 with BOM)");
            }

            builder.Append("\n\n");
            builder.Append(file.Text.TrimEnd('\n'));
            builder.Append("\n\n");
        }

        return builder.ToString();
    }
}
