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
        foreach (var option in new[] {"#os-Linux", "#ide-Rider", "#cli-Gui", "#tf-NUnit", "#ci-AzureDevOps", "#sponsor-Exempt"})
        {
            await page.ClickAsync(option);
            await page.ClickAsync("button.primary");
        }

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
        await Assert.That(string.Join(" | ", values)).IsEqualTo("MacOS | JetBrains Rider | Prefer CLI | Expecto | No build server | Private arrangement");
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
