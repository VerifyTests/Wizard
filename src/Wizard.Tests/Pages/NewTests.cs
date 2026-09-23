using Wizard.Web.Pages;

namespace Wizard.Tests.Pages;

/// <summary>The new-project flow against bunit's fake NavigationManager: url in, url out (plan 8.1).</summary>
public class NewTests : WebTestContext
{
    NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    IRenderedComponent<New> Open(string url)
    {
        Navigation.NavigateTo(url);
        return Render<New>();
    }

    string CurrentUrl => Navigation.ToBaseRelativePath(Navigation.Uri);

    [Test]
    public async Task StartsAtTheFirstStep()
    {
        var page = Open("new");
        await Assert.That(page.Find("section.step-body").GetAttribute("data-step")).IsEqualTo("os");
        await Assert.That(CurrentUrl).IsEqualTo("new?step=os");
    }

    [Test]
    public async Task RestoresStateFromTheUrl()
    {
        var page = Open("new?step=ci&os=MacOS&ide=Rider&cli=Gui&tf=NUnit");
        await Assert.That(page.Find("section.step-body").GetAttribute("data-step")).IsEqualTo("ci");
        await Assert.That(page.Find("li.done button[data-step=tf] .step-value").TextContent).IsEqualTo("NUnit");
    }

    [Test]
    public async Task UrlPastAnIncompleteStepIsCorrected()
    {
        Open("new?step=output&os=Windows");
        await Assert.That(CurrentUrl).IsEqualTo("new?step=ide&os=Windows");
    }

    [Test]
    public async Task ChoosingAnOptionUpdatesTheUrl()
    {
        var page = Open("new");
        await page.Find("#os-Linux").ClickAsync(new());
        await Assert.That(CurrentUrl).IsEqualTo("new?step=os&os=Linux");
        await Assert.That(page.Find("#os-Linux").ClassList.Contains("selected")).IsTrue();
    }

    [Test]
    public async Task NextIsGatedByTheCurrentStep()
    {
        var page = Open("new");
        await Assert.That(page.Find("button.primary").HasAttribute("disabled")).IsTrue();

        await page.Find("#os-Windows").ClickAsync(new());
        await Assert.That(page.Find("button.primary").HasAttribute("disabled")).IsFalse();

        await page.Find("button.primary").ClickAsync(new());
        await Assert.That(CurrentUrl).IsEqualTo("new?step=ide&os=Windows");
    }

    [Test]
    public async Task ChangingOsDropsAnIdeItDoesNotOffer()
    {
        var page = Open("new?step=os&os=Windows&ide=VisualStudio");
        await page.Find("#os-Linux").ClickAsync(new());
        await Assert.That(CurrentUrl).IsEqualTo("new?step=os&os=Linux");
    }

    [Test]
    public async Task SponsorExemptFillsTheMaximumTerm()
    {
        var page = Open("new?step=sponsor&os=Windows&ide=Rider&cli=Cli&tf=XunitV3&ci=None");
        await page.Find("#sponsor-Exempt").ClickAsync(new());
        await Assert.That(CurrentUrl).EndsWith("&sponsor=Exempt&exempt=OpenSource&until=2027-09");

        await page.Find("#exempt-MaintainerConsulting").ClickAsync(new());
        await Assert.That(CurrentUrl).EndsWith("&sponsor=Exempt&exempt=MaintainerConsulting&until=2027-03");
    }

    [Test]
    public async Task SponsorWithoutAccountBlocksNext()
    {
        var page = Open("new?step=sponsor&os=Windows&ide=Rider&cli=Cli&tf=XunitV3&ci=None&sponsor=Sponsor");
        await Assert.That(page.Find("button.primary").HasAttribute("disabled")).IsTrue();
        await Assert.That(page.Find(".validation-error").TextContent).Contains("GitHub account");

        await page.Find("#sponsorAccount").InputAsync(new ChangeEventArgs {Value = "acme"});
        await Assert.That(page.Find("button.primary").HasAttribute("disabled")).IsFalse();
        await Assert.That(CurrentUrl).EndsWith("&sponsor=Sponsor&account=acme");
    }

    /// <summary>Toggling a card writes the selection into the url, in registry order (plan 8.1).</summary>
    [Test]
    public async Task SelectingAnExtensionUpdatesTheUrl()
    {
        var page = Open("new?step=extensions&os=Windows&ide=Rider&cli=Cli&tf=XunitV3&ci=None");
        await page.Find(".extension-card[data-id=AngleSharp] input").ChangeAsync(new ChangeEventArgs {Value = true});
        await Assert.That(CurrentUrl).Contains("&ext=AngleSharp,DiffPlex");

        await page.Find(".extension-card[data-id=DiffPlex] input").ChangeAsync(new ChangeEventArgs {Value = false});
        await Assert.That(CurrentUrl).Contains("&ext=AngleSharp");
    }

    /// <summary>Nothing selected is a real answer, and the url has to say so, not read as the default.</summary>
    [Test]
    public async Task DeselectingEverythingIsCarriedInTheUrl()
    {
        var page = Open("new?step=extensions&os=Windows&ide=Rider&cli=Cli&tf=XunitV3&ci=None");
        await page.Find(".extension-card[data-id=DiffPlex] input").ChangeAsync(new ChangeEventArgs {Value = false});
        await Assert.That(CurrentUrl).Contains("&ext=none");
    }

    /// <summary>A Windows-only extension is greyed out on another OS, says why, and is dropped from the url.</summary>
    [Test]
    public async Task WindowsOnlyExtensionsAreUnavailableOffWindows()
    {
        var page = Open("new?step=extensions&os=Linux&ide=Rider&cli=Cli&tf=XunitV3&ci=None&ext=DiffPlex,WinForms");
        await Assert.That(CurrentUrl).DoesNotContain("WinForms");

        var card = page.Find(".extension-card[data-id=WinForms]");
        await Assert.That(card.ClassList.Contains("unavailable")).IsTrue();
        await Assert.That(card.GetAttribute("title")).IsEqualTo("Only runs on Windows, and the operating system chosen is Linux.");
        await Assert.That(page.Find(".extension-card[data-id=WinForms] input").HasAttribute("disabled")).IsTrue();
    }

    /// <summary>An extension the chosen test framework cannot run is greyed out too, with the reason.</summary>
    [Test]
    public async Task ExtensionsTheFrameworkCannotRunAreUnavailable()
    {
        var page = Open("new?step=extensions&os=Windows&ide=Rider&cli=Cli&tf=TUnit&ci=None&ext=DiffPlex,Avalonia");
        await Assert.That(CurrentUrl).DoesNotContain("Avalonia");

        var card = page.Find(".extension-card[data-id=Avalonia]");
        await Assert.That(card.ClassList.Contains("unavailable")).IsTrue();
        await Assert.That(card.GetAttribute("title")).StartsWith("Not available for TUnit: Avalonia.Headless ships test attributes");
    }

    /// <summary>Two extensions registering the same thing cannot both be generated (plan 11.1).</summary>
    [Test]
    public async Task ConflictingExtensionsBlockNext()
    {
        var page = Open("new?step=extensions&os=Windows&ide=Rider&cli=Cli&tf=XunitV3&ci=None&ext=Diagnostics,OpenTelemetry");
        await Assert.That(page.Find("button.primary").HasAttribute("disabled")).IsTrue();
        await Assert.That(page.Find(".interaction-notice[data-rule=activity-listener]").TextContent).Contains("ActivityListener");

        await page.Find(".extension-card[data-id=OpenTelemetry] input").ChangeAsync(new ChangeEventArgs {Value = false});
        await Assert.That(page.Find("button.primary").HasAttribute("disabled")).IsFalse();
    }

    /// <summary>With nothing selected the options step has nothing to ask, so the flow leaves it out.</summary>
    [Test]
    public async Task OptionsStepIsSkippedWhenNothingIsSelected()
    {
        var page = Open("new?step=options&os=Windows&ide=Rider&cli=Cli&tf=XunitV3&ci=None&ext=none");
        await Assert.That(page.Find("section.step-body").GetAttribute("data-step")).IsEqualTo("sponsor");
        await Assert.That(page.FindAll("li [data-step=options]").Count).IsEqualTo(0);
    }

    /// <summary>The output uses nuget.org's newest versions once they arrive (plan 15.3).</summary>
    [Test]
    public async Task OutputUsesTheNewestVersionsOnNuGet()
    {
        var page = Open("new?step=output&os=Windows&ide=Rider&cli=Gui&tf=XunitV3&ci=None&ext=Http");
        page.WaitForState(() => page.Find("[data-versions]").GetAttribute("data-versions") == "live");

        await Assert.That(page.Find(".versions").TextContent).IsEqualTo("Package versions are the newest stable releases on nuget.org.");
        await page.Find("#tab-Files").ClickAsync(new());
        await page.FindAll(".file-link").Single(_ => _.TextContent == "Directory.Packages.props").ClickAsync(new());
        await Assert.That(page.Find(".code-box-content").TextContent).Contains($"<PackageVersion Include=\"Verify.Http\" Version=\"{FakeNuGet.Newest}\" />");
    }

    /// <summary>Offline, the output stands with the baked versions, and says how old they are.</summary>
    [Test]
    public async Task OfflineKeepsTheBakedVersions()
    {
        NuGet.Offline = true;
        var page = Open("new?step=output&os=Windows&ide=Rider&cli=Gui&tf=XunitV3&ci=None");
        page.WaitForState(() => page.Find("[data-versions]").GetAttribute("data-versions") == "baked");
        await Assert.That(page.Find(".versions").TextContent).StartsWith("nuget.org could not be reached");
    }

    [Test]
    public Task OutputMarkup()
    {
        var page = Open("new?step=output&os=Linux&ide=VsCode&cli=Gui&tf=TUnit&ci=None&sponsor=Ignore");
        return Verify(page.Find("section.step-body .output-actions").OuterHtml, "html");
    }

    [Test]
    public async Task RenamingTheSolutionRenamesTheFiles()
    {
        var page = Open("new?step=output&os=Linux&ide=Other&cli=Gui&tf=XunitV3&ci=None");
        await page.Find("#solutionName").ChangeAsync(new ChangeEventArgs {Value = "Acme Tools!"});
        await Assert.That(CurrentUrl).Contains("&name=AcmeTools");

        await page.Find("#tab-Files").ClickAsync(new());
        var files = page.FindAll(".file-link").Select(_ => _.TextContent).ToList();
        await Assert.That(page.Find(".file-root").TextContent).IsEqualTo("AcmeTools/");
        await Assert.That(files).Contains("src/AcmeTools.Tests/AcmeTools.Tests.csproj");
    }
}
