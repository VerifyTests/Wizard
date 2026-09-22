namespace Wizard.Tests;

/// <summary>
/// One Verify.Playwright snapshot (PNG + HTML) per wizard screen, driven through the real WASM runtime.
/// The wizard bundles its own fonts, so layout is identical on every OS and the PNG baselines hold
/// cross-platform; SSIM with a lenient threshold (see ModuleInitializer) absorbs the remaining
/// rasterization differences. The HTML target pins exact markup. The page clock is fixed, so dates the
/// wizard derives from today are the same on every run.
/// </summary>
[ParallelLimiter<BrowserParallelLimit>]
public class ScreenSnapshotTests
{
    static PublishedWizard wizard => PublishedWizard.Shared;

    const string beforeOutput = "os=Windows&ide=Rider&cli=Cli&tf=XunitV3&ci=GitHubActions";
    const string output = $"/new?step=output&{beforeOutput}&sponsor=Exempt&exempt=OpenSource&until=2027-09";

    public static async Task<IPage> Open(string path, string readySelector)
    {
        var page = await wizard.NewPage();
        await page.Clock.SetFixedTimeAsync(new DateTime(2026, 9, 22, 10, 0, 0, DateTimeKind.Utc));
        await page.GotoAsync(wizard.Url(path));
        await page.WaitForSelectorAsync(readySelector);
        return page;
    }

    /// <summary>
    /// The bundled webfonts are declared <c>font-display: block</c>, so text stays unpainted until
    /// they load and a screenshot taken too early captures a blank or differently-measured page.
    /// </summary>
    static async Task VerifyScreen(IPage page, bool fullPage = true)
    {
        await page.EvaluateAsync("async () => { await document.fonts.ready; }");
        await Verify(page)
            .PageScreenshotOptions(
                new()
                {
                    FullPage = fullPage
                });
    }

    [Test]
    public async Task Home()
    {
        var page = await Open("/", ".entry-cards");
        await VerifyScreen(page);
    }

    [Test]
    public async Task NewOs()
    {
        var page = await Open("/new", "#os-Windows");
        await VerifyScreen(page);
    }

    [Test]
    public async Task NewIde()
    {
        var page = await Open("/new?step=ide&os=Windows&ide=Rider", "#ide-Rider.selected");
        await VerifyScreen(page);
    }

    [Test]
    public async Task NewCli()
    {
        var page = await Open("/new?step=cli&os=MacOS&ide=Rider", "#cli-Cli");
        await VerifyScreen(page);
    }

    [Test]
    public async Task NewTestFramework()
    {
        var page = await Open("/new?step=tf&os=Windows&ide=Rider&cli=Cli&tf=XunitV3", "#tf-XunitV3.selected");
        await VerifyScreen(page);
    }

    [Test]
    public async Task NewBuildServer()
    {
        var page = await Open("/new?step=ci&os=Windows&ide=Rider&cli=Cli&tf=XunitV3", "#ci-GitHubActions");
        await VerifyScreen(page);
    }

    [Test]
    public async Task NewSponsorDecideLater()
    {
        var page = await Open($"/new?step=sponsor&{beforeOutput}", "#sponsor-Sponsor");
        await VerifyScreen(page);
    }

    [Test]
    public async Task NewSponsoring()
    {
        var page = await Open($"/new?step=sponsor&{beforeOutput}", "#sponsor-Sponsor");
        await page.ClickAsync("#sponsor-Sponsor");
        await page.FillAsync("#sponsorAccount", "acme");
        await page.CheckAsync("#sponsorshipNew");
        await page.WaitForSelectorAsync("#sponsorshipStart");
        await VerifyScreen(page);
    }

    [Test]
    public async Task NewSponsorExempt()
    {
        var page = await Open($"/new?step=sponsor&{beforeOutput}", "#sponsor-Exempt");
        await page.ClickAsync("#sponsor-Exempt");
        await page.WaitForSelectorAsync("#sponsorUntil");
        await VerifyScreen(page);
    }

    // The guide is long; the viewport shows the output step's controls and the start of the guide.
    [Test]
    public async Task NewOutputGuide()
    {
        var page = await Open(output, ".markdown h1");
        await VerifyScreen(page, fullPage: false);
    }

    [Test]
    public async Task NewOutputFiles()
    {
        var page = await Open(output, ".markdown h1");
        await page.ClickAsync("#tab-Files");
        await page.ClickAsync(".file-link >> text=src/VerifySample.Tests/ModuleInitializer.cs");
        await page.WaitForSelectorAsync(".code-box-title >> text=ModuleInitializer.cs");
        await VerifyScreen(page);
    }

    [Test]
    public async Task NewOutputAi()
    {
        var page = await Open(output, ".markdown h1");
        await page.ClickAsync("#tab-AI");
        await page.WaitForSelectorAsync("pre.ai-markdown");
        await VerifyScreen(page, fullPage: false);
    }

    /// <summary>Below 768px the rail becomes a strip of chosen values above the step (plan 7.4).</summary>
    [Test]
    public async Task NewNarrow()
    {
        var page = await Open($"/new?step=tf&os=Linux&ide=Rider&cli=Cli", "#tf-XunitV3");
        await page.SetViewportSizeAsync(375, 812);
        await VerifyScreen(page);
    }
}
