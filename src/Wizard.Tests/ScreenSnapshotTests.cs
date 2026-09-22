namespace Wizard.Tests;

/// <summary>
/// One Verify.Playwright snapshot (full-page PNG + HTML) per wizard screen, driven through the real
/// WASM runtime. The wizard bundles its own fonts, so layout is identical on every OS and the PNG
/// baselines hold cross-platform; SSIM with a lenient threshold (see ModuleInitializer) absorbs the
/// remaining rasterization differences. The HTML target pins exact markup.
/// </summary>
[ParallelLimiter<BrowserParallelLimit>]
public class ScreenSnapshotTests
{
    static PublishedWizard wizard => PublishedWizard.Shared;

    static async Task<IPage> Open(string path, string readySelector)
    {
        var page = await wizard.NewPage();
        await page.GotoAsync(wizard.Url(path));
        await page.WaitForSelectorAsync(readySelector);
        return page;
    }

    /// <summary>
    /// The bundled webfonts are declared <c>font-display: block</c>, so text stays unpainted until
    /// they load and a screenshot taken too early captures a blank or differently-measured page.
    /// </summary>
    static async Task VerifyScreen(IPage page)
    {
        await page.EvaluateAsync("async () => { await document.fonts.ready; }");
        await Verify(page);
    }

    [Test]
    public async Task Home()
    {
        var page = await Open("/", ".entry-cards");
        await VerifyScreen(page);
    }
}
