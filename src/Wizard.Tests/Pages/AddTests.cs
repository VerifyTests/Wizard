/// <summary>The add flows (plan 7.2, 7.3) and the browser's memory (plan 8.2), against bunit's fake NavigationManager.</summary>
public class AddTests : WebTestContext
{
    NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    string CurrentUrl => Navigation.ToBaseRelativePath(Navigation.Uri);

    IRenderedComponent<Add> OpenAdd(string url, string? pluginId = null)
    {
        Navigation.NavigateTo(url);
        return Render<Add>(_ => _.Add(page => page.PluginId, pluginId));
    }

    IRenderedComponent<AddByTech> OpenAddByTech(string url)
    {
        Navigation.NavigateTo(url);
        return Render<AddByTech>();
    }

    void Remember(string key, string value) =>
        JSInterop.Setup<string?>("verifyWizard.storageGet", key).SetResult(value);

    IEnumerable<(string Key, string Value)> Written() =>
        JSInterop.Invocations
            .Where(_ => _.Identifier == "verifyWizard.storageSet")
            .Select(_ => ((string) _.Arguments[0]!, (string) _.Arguments[1]!));

    /// <summary>Adding to a project asks only the test framework before the plugins (plan 7.2).</summary>
    [Test]
    public async Task StartsAtTheTestFramework()
    {
        var page = OpenAdd("add");
        await Assert.That(page.Find("section.step-body").GetAttribute("data-step")).IsEqualTo("tf");
        var steps = page.FindAll(".breadcrumb li .step-title").Select(_ => _.TextContent);
        await Assert.That(string.Join(" | ", steps)).IsEqualTo("Test framework | Already using | Plugins | Options | Maintenance fee | Result");
    }

    /// <summary>A plugin readme links to /add/{Id}, which starts with that one selected (plan D11).</summary>
    [Test]
    public async Task DeepLinkSelectsThePlugin()
    {
        OpenAdd("add/EntityFramework", "EntityFramework");
        await Assert.That(CurrentUrl).IsEqualTo("add?step=tf&ext=EntityFramework");
    }

    [Test]
    public async Task DeepLinkToAnUnknownPluginIsIgnored()
    {
        OpenAdd("add/NotAPlugin", "NotAPlugin");
        await Assert.That(CurrentUrl).IsEqualTo("add?step=tf");
    }

    /// <summary>Something the project already has is shown ticked and locked, never selectable.</summary>
    [Test]
    public async Task ExistingPluginsAreLocked()
    {
        var page = OpenAdd("add?step=plugins&tf=XunitV3&have=SqlServer");
        var card = page.Find(".plugin-card[data-id=SqlServer]");
        await Assert.That(card.ClassList.Contains("existing")).IsTrue();
        await Assert.That(card.QuerySelector("input")!.HasAttribute("disabled")).IsTrue();
    }

    /// <summary>
    /// The side effect the interaction rules exist for: adding EF Core next to an existing SqlServer
    /// raises the recording notice, although only one of the two is being added (plan 17.3).
    /// </summary>
    [Test]
    public async Task AddingNextToAnExistingPluginRaisesItsInteraction()
    {
        var page = OpenAdd("add?step=plugins&tf=XunitV3&have=SqlServer&ext=EntityFramework");
        await Assert.That(page.FindAll(".interaction-notice[data-rule=ef-sql-recording]").Count).IsEqualTo(1);
    }

    [Test]
    public async Task TickingAnExistingPluginUpdatesTheUrlAndIsRemembered()
    {
        var page = OpenAdd("add?step=have&tf=XunitV3");
        await page.Find(".existing-item[data-id=SqlServer] input")
            .ChangeAsync(
                new()
                {
                    Value = true
                });

        await Assert.That(CurrentUrl).IsEqualTo("add?step=have&tf=XunitV3&have=SqlServer");
        await Assert.That(Written()).Contains((BrowserMemory.ExistingKey, "SqlServer"));
    }

    /// <summary>Choosing a tech selects what it recommends, and the browser remembers the stack.</summary>
    [Test]
    public async Task ChoosingATechSelectsItsRecommendations()
    {
        var page = OpenAddByTech("add/by-tech?step=tech&tf=XunitV3");
        await page.Find(".chip[data-tech=aspnetcore]").ClickAsync(new());

        await Assert.That(CurrentUrl).IsEqualTo("add/by-tech?step=tech&tf=XunitV3&tech=aspnetcore&ext=AspNetCore,Http");
        await Assert.That(Written()).Contains((BrowserMemory.TechKey, "aspnetcore"));
    }

    /// <summary>What the browser remembers fills in what the url leaves out, and the step says so.</summary>
    [Test]
    public async Task RememberedAnswersAreRestored()
    {
        Remember(BrowserMemory.ExistingKey, "SqlServer,DiffPlex");
        var page = OpenAdd("add?step=have&tf=XunitV3");

        await Assert.That(CurrentUrl).IsEqualTo("add?step=have&tf=XunitV3&have=DiffPlex,SqlServer");
        await Assert.That(page.Find(".restored").TextContent).IsEqualTo("Restored from this browser.");
    }

    [Test]
    public async Task TheUrlWinsOverWhatIsRemembered()
    {
        Remember(BrowserMemory.ExistingKey, "SqlServer");
        var page = OpenAdd("add?step=have&tf=XunitV3&have=Http");

        await Assert.That(CurrentUrl).IsEqualTo("add?step=have&tf=XunitV3&have=Http");
        await Assert.That(page.FindAll(".restored").Count).IsEqualTo(0);
    }

    /// <summary>An existing project already declares its status, so leaving it alone is the first choice.</summary>
    [Test]
    public async Task SponsorDefaultsToNoChange()
    {
        var page = OpenAdd("add?step=sponsor&tf=XunitV3&ext=Http");
        await Assert.That(page.Find("#sponsor-NotChosen").TextContent).Contains("No change");
        await Assert.That(page.Find("button.primary").HasAttribute("disabled")).IsFalse();
    }

    [Test]
    public async Task OutputOffersTheChangesRatherThanASolution()
    {
        var page = OpenAdd("add?step=output&tf=XunitV3&have=SqlServer&ext=EntityFramework");
        await Assert.That(page.Find("button.download-zip").TextContent).IsEqualTo("Download changes (.zip)");
        await Assert.That(page.FindAll("#solutionName").Count).IsEqualTo(0);

        await page.Find("#tab-Files").ClickAsync(new());
        await Assert.That(page.Find(".file-root").TextContent).IsEqualTo("verify-additions/");
        var files = page.FindAll(".file-link").Select(_ => _.TextContent).ToList();
        await Assert.That(files).Contains(AdditionGenerator.PackagesFragment);
        await Assert.That(files).Contains("ModuleInitializer.cs");
    }
}
