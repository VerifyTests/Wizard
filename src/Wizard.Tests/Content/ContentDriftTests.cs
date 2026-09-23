namespace Wizard.Tests.Content;

/// <summary>
/// Notices when Verify's docs or samples move on from the wizard's copies of them (plan 17.5). Needs
/// github.com, so explicit: run weekly by content-drift.yml, which never blocks a deploy, or with
/// <c>--treenode-filter "/*/*/ContentDriftTests/*"</c>.
/// </summary>
[Explicit]
public class ContentDriftTests
{
    const string raw = "https://raw.githubusercontent.com/VerifyTests/Verify/main/";

    static readonly HttpClient client = new();

    static async Task<string> Upstream(string path)
    {
        var text = await client.GetStringAsync(raw + path);
        return text.Replace("\r\n", "\n");
    }

    public static IEnumerable<string> Includes() =>
        typeof(ContentFiles).Assembly
            .GetManifestResourceNames()
            .Where(_ => _.EndsWith(".include.md"))
            .Select(_ => _["Content.".Length..])
            .Order();

    /// <summary>Include files are copied verbatim, so they must equal the originals.</summary>
    [Test]
    [MethodDataSource(nameof(Includes))]
    public async Task IncludeMatchesVerify(string fileName)
    {
        var upstream = await Upstream($"docs/mdsource/{fileName}");
        await Assert.That(ContentFiles.Raw(fileName)).IsEqualTo(upstream);
    }

    /// <summary>
    /// Sources the wizard adapts rather than copies: ai-usage.source.md (context.md is its context
    /// file template, without the inline snapshot sections), the core sample and its target library, and
    /// the Fixie convention. Each is snapshot as it was when the wizard's version was last brought in
    /// line; a failure is the upstream diff, to apply to the wizard's copy before accepting.
    /// </summary>
    [Test]
    [Arguments("AiUsage", "docs/mdsource/ai-usage.source.md")]
    [Arguments("ClassBeingTested", "src/TargetLibrary/ClassBeingTested.cs")]
    [Arguments("SampleModels", "src/TargetLibrary/SampleModels.cs")]
    [Arguments("SampleVerified", "src/Verify.XunitV3.Tests/Snippets/Sample.Test.verified.txt")]
    [Arguments("FixieTestProject", "src/Verify.Fixie.Tests/FixieSetup/TestProject.cs")]
    [Arguments("SampleXunitV3", "src/Verify.XunitV3.Tests/Snippets/Sample.cs")]
    [Arguments("SampleNUnit", "src/Verify.NUnit.Tests/Snippets/Sample.cs")]
    [Arguments("SampleTUnit", "src/Verify.TUnit.Tests/Snippets/Sample.cs")]
    [Arguments("SampleMSTest", "src/Verify.MSTest.Tests/Snippets/Sample.cs")]
    [Arguments("SampleFixie", "src/Verify.Fixie.Tests/Snippets/Sample.cs")]
    [Arguments("SampleExpecto", "src/Verify.Expecto.FSharpTests/Tests.fs")]
    public async Task AdaptedSourceIsUnchanged(string name, string path) =>
        await Verify(await Upstream(path), "txt")
            .UseDirectory("Upstream")
            .UseFileName(name);
}
