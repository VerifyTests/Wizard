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
