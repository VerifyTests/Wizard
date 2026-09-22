namespace Wizard.Tests.Pages;

public class HomeTests : WebTestContext
{
    [Test]
    public async Task RendersTheThreeEntryPoints()
    {
        var cut = Render<Wizard.Web.Pages.Home>();

        var hrefs = cut.FindAll("a.entry-card").Select(_ => _.GetAttribute("href"));
        await Assert.That(string.Join(" ", hrefs)).IsEqualTo("new add add/by-tech");
    }

    /// <summary>
    /// The markup only, as html so it is pretty printed. Verifying the component itself adds an info
    /// part (instance and node count) that carries nothing worth reviewing.
    /// </summary>
    [Test]
    public Task Markup() =>
        Verify(Render<Wizard.Web.Pages.Home>().Markup, "html");
}
