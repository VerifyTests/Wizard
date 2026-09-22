namespace Wizard.Tests.Pages;

public class HomeTests : WebTestContext
{
    [Test]
    public async Task RendersTheThreeEntryPoints()
    {
        var cut = Render<Wizard.Web.Pages.Home>();

        await Assert.That(cut.FindAll(".entry-card").Count).IsEqualTo(3);
        // the add flows are not linked until they exist (plan phase 3)
        var hrefs = cut.FindAll("a.entry-card").Select(_ => _.GetAttribute("href"));
        await Assert.That(string.Join(" ", hrefs)).IsEqualTo("new");
    }

    /// <summary>
    /// The markup only, as html so it is pretty printed. Verifying the component itself adds an info
    /// part (instance and node count) that carries nothing worth reviewing.
    /// </summary>
    [Test]
    public Task Markup() =>
        Verify(Render<Wizard.Web.Pages.Home>().Markup, "html");
}
