namespace Wizard.Tests.Model;

/// <summary>What the browser remembers, and when it applies (plan 8.2).</summary>
public class BrowserMemoryTests
{
    static readonly Remembered everything = new("efcore", "SqlServer", "sponsor=Exempt&exempt=SmallRevenue&until=2027-09");

    [Test]
    public async Task RememberedAnswersFillInWhatTheUrlLeavesOut()
    {
        const string query = "step=extensions&tf=XunitV3";
        var state = WizardStateUrl.Parse(Flow.AddByTech, query);

        var restored = BrowserMemory.Seed(state, query, everything);

        await Assert.That(restored).IsEqualTo(new(Tech: true, Existing: true, Sponsor: true));
        await Assert.That(state.Techs).IsEquivalentTo(["efcore"]);
        await Assert.That(state.ExistingExtensions).IsEquivalentTo(["SqlServer"]);
        await Assert.That(state.Exemption).IsEqualTo(Exemption.SmallRevenue);
        // the remembered stack's recommendations are applied, as if each tech had just been chosen
        await Assert.That(state.Has("EntityFramework")).IsTrue();
    }

    /// <summary>A shared link means the same thing in every browser.</summary>
    [Test]
    public async Task TheUrlWins()
    {
        const string query = "tf=XunitV3&tech=stj&have=DiffPlex&sponsor=Ignore";
        var state = WizardStateUrl.Parse(Flow.AddByTech, query);

        var restored = BrowserMemory.Seed(state, query, everything);

        await Assert.That(restored.Any).IsFalse();
        await Assert.That(state.Techs).IsEquivalentTo(["stj"]);
        await Assert.That(state.ExistingExtensions).IsEquivalentTo(["DiffPlex"]);
        await Assert.That(state.SponsorMode).IsEqualTo(SponsorMode.Ignore);
    }

    /// <summary>A url that names extensions keeps exactly those, even when it leaves the stack out.</summary>
    [Test]
    public async Task ARememberedStackDoesNotAddToAnExplicitSelection()
    {
        const string query = "tf=XunitV3&ext=Http";
        var state = WizardStateUrl.Parse(Flow.AddByTech, query);

        BrowserMemory.Seed(state, query, everything);

        await Assert.That(state.Techs).IsEquivalentTo(["efcore"]);
        await Assert.That(state.SelectedExtensions).IsEquivalentTo(["Http"]);
    }

    /// <summary>Only answers to questions the flow asks are read, or written.</summary>
    [Test]
    public async Task AFlowOnlyUsesWhatItAsks()
    {
        var added = WizardStateUrl.Parse(Flow.Add, "");
        BrowserMemory.Seed(added, "", everything);
        await Assert.That(added.Techs).IsEmpty();
        await Assert.That(added.ExistingExtensions).IsEquivalentTo(["SqlServer"]);

        var created = WizardStateUrl.Parse(Flow.New, "");
        BrowserMemory.Seed(created, "", everything);
        await Assert.That(created.ExistingExtensions).IsEmpty();
        await Assert.That(created.Techs).IsEquivalentTo(["efcore"]);

        // what the new-project flow writes leaves the existing list alone
        var remembered = BrowserMemory.For(created);
        await Assert.That(remembered.Existing).IsNull();
        await Assert.That(remembered.Tech).IsEqualTo("efcore");
    }

    [Test]
    public async Task UnknownRememberedIdsAreDropped()
    {
        var state = WizardStateUrl.Parse(Flow.AddByTech, "");
        BrowserMemory.Seed(state, "", new("nope,efcore", "Gone,SqlServer", "sponsor=Nonsense"));

        await Assert.That(state.Techs).IsEquivalentTo(["efcore"]);
        await Assert.That(state.ExistingExtensions).IsEquivalentTo(["SqlServer"]);
        await Assert.That(state.SponsorMode).IsEqualTo(SponsorMode.NotChosen);
    }
}
