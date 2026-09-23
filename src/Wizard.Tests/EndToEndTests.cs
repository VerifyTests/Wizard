namespace Wizard.Tests;

/// <summary>Journeys through the published app in Chromium (plan 17.3).</summary>
[ParallelLimiter<BrowserParallelLimit>]
public class EndToEndTests
{
    static PublishedWizard wizard => PublishedWizard.Shared;

    [Test]
    public async Task NewFlowToZipDownload()
    {
        var page = await ScreenSnapshotTests.Open("/", ".entry-cards");
        await page.ClickAsync("a.entry-card[href=new]");
        foreach (var option in new[] {"#os-Linux", "#ide-Rider", "#cli-Gui", "#tf-NUnit", "#ci-AzureDevOps"})
        {
            await page.ClickAsync(option);
            await page.ClickAsync("button.primary");
        }

        // The extension step starts on the default selection, and the options step on the defaults,
        // so both are passed by moving on.
        await page.WaitForSelectorAsync(".extension-card[data-id=DiffPlex]");
        await page.ClickAsync("button.primary");
        await page.WaitForSelectorAsync(".depth-row[data-id=DiffPlex]");
        await page.ClickAsync("button.primary");

        await page.ClickAsync("#sponsor-Exempt");
        await page.ClickAsync("button.primary");

        await page.WaitForSelectorAsync("button.download-zip");
        await Assert.That(page.Url).EndsWith("/new?step=output&os=Linux&ide=Rider&cli=Gui&tf=NUnit&ci=AzureDevOps&sponsor=Exempt&exempt=OpenSource&until=2027-09");

        var download = await page.RunAndWaitForDownloadAsync(() => page.ClickAsync("button.download-zip"));
        await Assert.That(download.SuggestedFilename).IsEqualTo("VerifySample.zip");
        var path = Path.Combine(Path.GetTempPath(), $"wizard-{Guid.NewGuid():N}.zip");
        await download.SaveAsAsync(path);
        try
        {
            using var archive = ZipFile.OpenRead(path);
            var entries = archive.Entries.Select(_ => _.FullName).ToList();
            await Assert.That(entries).Contains("VerifySample/src/VerifySample.Tests/ModuleInitializer.cs");
            await Assert.That(entries).Contains("VerifySample/azure-pipelines.yml");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task SharedLinkOpensTheSameState()
    {
        const string url = "/new?step=output&os=MacOS&ide=Rider&cli=Cli&tf=Expecto&ci=None&name=Acme&sponsor=Private&until=2027-06";
        var first = await ScreenSnapshotTests.Open(url, "button.download-zip");
        var second = await ScreenSnapshotTests.Open(new Uri(first.Url).PathAndQuery, "button.download-zip");

        await Assert.That(second.Url).IsEqualTo(first.Url);
        await Assert.That(await second.InputValueAsync("#solutionName")).IsEqualTo("Acme");
        var values = await second.Locator(".breadcrumb .step-value").AllTextContentsAsync();
        await Assert.That(string.Join(" | ", values)).IsEqualTo("MacOS | JetBrains Rider | Prefer CLI | Expecto | No build server | DiffPlex | Defaults | Private arrangement");
    }

    /// <summary>
    /// Selecting two extensions that register the same thing blocks the step, and the notice says which
    /// ones (plan 11.1). Deselecting one unblocks it.
    /// </summary>
    [Test]
    public async Task ConflictingExtensionsBlockTheStep()
    {
        var page = await ScreenSnapshotTests.Open(
            "/new?step=extensions&os=Windows&ide=Rider&cli=Cli&tf=XunitV3&ci=None&ext=Diagnostics,OpenTelemetry",
            ".interaction-notice[data-rule=activity-listener]");

        var next = page.Locator("button.primary");
        await Assert.That(await next.IsDisabledAsync()).IsTrue();

        await page.ClickAsync(".extension-card[data-id=OpenTelemetry] input");
        await page.WaitForSelectorAsync(".interaction-notice[data-rule=activity-listener]", new() {State = WaitForSelectorState.Detached});
        await Assert.That(await next.IsDisabledAsync()).IsFalse();
    }

    /// <summary>The options step writes the depth and the choice into the url, so the link carries them.</summary>
    [Test]
    public async Task OptionsAreCarriedInTheUrl()
    {
        var page = await ScreenSnapshotTests.Open(
            "/new?step=options&os=Windows&ide=Rider&cli=Cli&tf=XunitV3&ci=None&ext=DiffPlex",
            ".depth-row[data-id=DiffPlex]");

        await page.ClickAsync(".depth-row[data-id=DiffPlex] .depth-option:text-is('Minimal')");
        await page.ClickAsync(".choice[data-choice=diffplex-output] label:has-text('Full') input");

        // The wizard replaces the history entry rather than navigating, so there is no load to wait for.
        await page.WaitForFunctionAsync(
            "() => location.search.includes('min=DiffPlex') && location.search.includes('opt=diffplex-output:Full')");
    }

    [Test]
    public async Task BackAndForwardMoveBetweenSteps()
    {
        var page = await ScreenSnapshotTests.Open("/new", "#os-Windows");
        await page.ClickAsync("#os-Windows");
        await page.ClickAsync("button.primary");
        await page.WaitForSelectorAsync("#ide-Rider");

        await page.GoBackAsync();
        await page.WaitForSelectorAsync("#os-Windows.selected");

        await page.GoForwardAsync();
        await page.WaitForSelectorAsync("#ide-Rider");
    }
}
