namespace Wizard.Tests.Model;

public class WizardStateUrlTests
{
    public static IEnumerable<Func<WizardState>> States()
    {
        yield return () => new() {Flow = Flow.New, Step = "os"};
        yield return () => GeneratorTests.State();
        yield return () => GeneratorTests.State(TestFramework.Expecto, BuildServer.None, Os.Linux, Ide.VsCode, CliPreference.Gui) with {SolutionName = "My.Product"};
        yield return () => GeneratorTests.State() with
        {
            SponsorMode = SponsorMode.Sponsor,
            SponsorAccount = "acme",
            SponsorshipStart = new Date(2026, 9, 1),
            SponsorshipPrivateUntil = "2027-01"
        };
        yield return () => GeneratorTests.State() with {SponsorMode = SponsorMode.PrivateArrangement, SponsorUntil = "2027-06"};
        yield return () => GeneratorTests.State() with {SponsorMode = SponsorMode.Ignore};
        yield return () => GeneratorTests.State() with {SelectedExtensions = new HashSet<string>(StringComparer.Ordinal)};
        yield return () => GeneratorTests.WithExtensions(GeneratorTests.State(), "EntityFramework", "SqlServer");
        yield return () =>
        {
            var state = GeneratorTests.WithExtensions(GeneratorTests.State(), "AngleSharp", "DiffPlex");
            state.SetDepth("AngleSharp", Depth.Minimal);
            state.SetChoice("diffplex-output", "Full");
            return state;
        };
        yield return () => GeneratorTests.Addition(Flow.Add, ["SqlServer"], ["EntityFramework"]);
        yield return () =>
        {
            var state = GeneratorTests.Addition(Flow.AddByTech, ["DiffPlex"], ["Http"]);
            state.Techs = new HashSet<string>(["http", "aspnetcore"], StringComparer.Ordinal);
            return state;
        };
    }

    /// <summary>The add flows start from nothing, rather than from Verify.DiffPlex (plan 7.2).</summary>
    [Test]
    public async Task AddFlowsStartWithNothingSelected()
    {
        var state = WizardStateUrl.Parse(Flow.Add, "tf=NUnit");
        await Assert.That(state.SelectedExtensions).IsEmpty();
        await Assert.That(WizardStateUrl.ToQuery(state)).DoesNotContain("ext=");
    }

    /// <summary>Answers to questions a flow does not ask are dropped, so the link holds only what matters.</summary>
    [Test]
    public async Task QuestionsAFlowDoesNotAskAreDropped()
    {
        var added = WizardStateUrl.Parse(Flow.Add, "os=Windows&ci=None&tech=efcore&have=SqlServer&ext=SqlServer,Http");
        await Assert.That(added.Os).IsNull();
        await Assert.That(added.BuildServer).IsNull();
        await Assert.That(added.Techs).IsEmpty();
        // something the project already has cannot be added again
        await Assert.That(added.SelectedExtensions).IsEquivalentTo(["Http"]);

        var created = WizardStateUrl.Parse(Flow.New, "have=SqlServer");
        await Assert.That(created.ExistingExtensions).IsEmpty();
    }

    /// <summary>
    /// An absent ext key means the default selection, not an empty one, so a link made before the
    /// extension step existed still generates what it used to.
    /// </summary>
    [Test]
    public async Task AbsentExtensionsMeansTheDefault()
    {
        var state = WizardStateUrl.Parse(Flow.New, "os=Windows");
        await Assert.That(state.SelectedExtensions).IsEquivalentTo(WizardState.DefaultExtensions(Flow.New));
    }

    [Test]
    public async Task NoneMeansNothingSelected()
    {
        var state = WizardStateUrl.Parse(Flow.New, $"os=Windows&ext={WizardStateUrl.NoExtensions}");
        await Assert.That(state.SelectedExtensions).IsEmpty();
    }

    [Test]
    public async Task UnknownExtensionsAndStaleOptionsAreDropped()
    {
        var state = WizardStateUrl.Parse(Flow.New, "ext=DiffPlex,NotAnExtension&min=NotAnExtension&opt=nope:1");
        await Assert.That(state.SelectedExtensions).IsEquivalentTo(["DiffPlex"]);
        await Assert.That(state.Depths).IsEmpty();
        await Assert.That(state.Choices).IsEmpty();
    }

    /// <summary>The order ids were added in must not change the link.</summary>
    [Test]
    public async Task ExtensionOrderIsTheRegistryOrder()
    {
        var one = WizardStateUrl.Parse(Flow.New, "ext=SqlServer,DiffPlex");
        var other = WizardStateUrl.Parse(Flow.New, "ext=DiffPlex,SqlServer");
        await Assert.That(WizardStateUrl.ToQuery(one)).IsEqualTo(WizardStateUrl.ToQuery(other));
    }

    [Test]
    [MethodDataSource(nameof(States))]
    public async Task RoundTrips(WizardState state)
    {
        // the url carries the normalized state: values that cannot apply are not kept
        state.Normalize();
        var url = WizardStateUrl.ToRelativeUrl(state);
        var query = url.Contains('?') ? url[(url.IndexOf('?') + 1)..] : "";
        var parsed = WizardStateUrl.Parse(state.Flow, query);
        await Assert.That(parsed).IsEqualTo(state);
    }

    [Test]
    public Task Urls() =>
        Verify(States().Select(_ => WizardStateUrl.ToAbsoluteUrl(_())));

    [Test]
    [Arguments("")]
    [Arguments("?")]
    [Arguments("?os=windows&tf=42&ci=&step=nope&sponsor=Sponsorx")]
    [Arguments("?%zz=%zz&=&&os")]
    [Arguments("?start=2026-13-45&until=soon&sponsor=Exempt&exempt=Everyone")]
    public async Task JunkParsesToDefaults(string query)
    {
        var state = WizardStateUrl.Parse(Flow.New, query);
        await Assert.That(state.Os).IsNull();
        await Assert.That(state.TestFramework).IsNull();
        await Assert.That(state.BuildServer).IsNull();
        await Assert.That(state.SponsorshipStart).IsNull();
        await Assert.That(state.Exemption).IsNull();
        await Assert.That(state.Step).IsEqualTo("os");
    }

    [Test]
    public async Task FirstOccurrenceWins()
    {
        var state = WizardStateUrl.Parse(Flow.New, "os=Linux&os=Windows");
        await Assert.That(state.Os).IsEqualTo(Os.Linux);
    }

    [Test]
    public async Task IdeNotOfferedForOsIsDropped()
    {
        var state = WizardStateUrl.Parse(Flow.New, "os=Linux&ide=VisualStudio");
        await Assert.That(state.Ide).IsNull();
    }

    [Test]
    public async Task UrlCannotSkipAnIncompleteStep()
    {
        var state = WizardStateUrl.Parse(Flow.New, "step=output&os=Windows&ide=Rider");
        await Assert.That(FlowSteps.Reachable(state, GeneratorTests.Today).Id).IsEqualTo("cli");
    }

    [Test]
    [Arguments("VerifySample", "VerifySample")]
    [Arguments("  My Product! ", "MyProduct")]
    [Arguments("9lives", "VerifySample")]
    [Arguments("...", "VerifySample")]
    [Arguments("Acme.Tools", "Acme.Tools")]
    public async Task SolutionNamesAreCleaned(string input, string expected) =>
        await Assert.That(SolutionNames.Clean(input)).IsEqualTo(expected);

    [Test]
    [Arguments("Verify", "Verification")]
    [Arguments("Verify.Tests", "Verification.Tests")]
    [Arguments("VerifySample.Tests", "VerifySample.Tests")]
    [Arguments("Acme.Verify", "Acme.Verify")]
    public async Task RootNamespaceAvoidsAVerifyNamespace(string project, string expected) =>
        await Assert.That(SolutionNames.RootNamespace(project)).IsEqualTo(expected);
}
