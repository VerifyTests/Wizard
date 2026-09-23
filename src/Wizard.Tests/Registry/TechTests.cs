/// <summary>The tech stack table and what choosing from it does to the selection (plan 10).</summary>
public class TechTests
{
    [Test]
    public Task Table() =>
        Verify(Techs.All.Select(_ => $"{_.Group} / {_.Id} ({_.DisplayName}): {string.Join(", ", _.Recommended)}; related {string.Join(", ", _.Related)}"));

    [Test]
    public async Task IdsAreUnique()
    {
        var duplicates = Techs.All.GroupBy(_ => _.Id).Where(_ => _.Count() > 1).Select(_ => _.Key);
        await Assert.That(duplicates).IsEmpty();
    }

    [Test]
    public async Task SuggestionsNameRealPlugins()
    {
        foreach (var tech in Techs.All)
        {
            foreach (var id in tech.Recommended.Concat(tech.Related))
            {
                await Assert.That(Plugins.Contains(id))
                    .IsTrue()
                    .Because($"tech '{tech.Id}' suggests '{id}', which the registry does not have");
            }
        }
    }

    /// <summary>Every plugin is reachable from some tech, or is deliberately listed as universal (plan 9.2).</summary>
    [Test]
    public async Task EveryPluginIsSuggestedOrUniversal()
    {
        var suggested = Techs.All
            .SelectMany(_ => _.Recommended.Concat(_.Related))
            .Concat(Techs.NotSuggestedByTech)
            .ToHashSet(StringComparer.Ordinal);
        var orphans = Plugins.All.Select(_ => _.Id).Where(_ => !suggested.Contains(_));
        await Assert.That(orphans).IsEmpty();
    }

    [Test]
    public async Task ChoosingATechSelectsWhatItRecommends()
    {
        var state = GeneratorTests.State();
        TechSuggestions.Choose(state, "efcore", true);
        await Assert.That(state.Techs).IsEquivalentTo(["efcore"]);
        await Assert.That(state.Has("EntityFramework")).IsTrue();
        await Assert.That(state.Has("LocalDb")).IsTrue();
        // related, so listed but not selected
        await Assert.That(state.Has("SqlServer")).IsFalse();
    }

    /// <summary>Unchoosing takes back only what no remaining tech also recommends.</summary>
    [Test]
    public async Task UnchoosingATechKeepsWhatAnotherStillRecommends()
    {
        var state = GeneratorTests.State();
        TechSuggestions.Choose(state, "aspnetcore", true);
        TechSuggestions.Choose(state, "http", true);
        TechSuggestions.Choose(state, "aspnetcore", false);

        await Assert.That(state.Has("AspNetCore")).IsFalse();
        await Assert.That(state.Has("Http")).IsTrue();
    }

    /// <summary>
    /// Excel recommends ClosedXml and Word recommends OpenXml, and both convert xlsx. The second is
    /// skipped rather than selected into a conflict the user did not ask for.
    /// </summary>
    [Test]
    public async Task ARecommendationThatWouldConflictIsSkipped()
    {
        var state = GeneratorTests.State();
        TechSuggestions.Choose(state, "excel", true);
        TechSuggestions.Choose(state, "word", true);

        await Assert.That(state.Has("ClosedXml")).IsTrue();
        await Assert.That(state.Has("OpenXml")).IsFalse();
        await Assert.That(InteractionRules.For(state).Where(_ => _.Severity == Severity.Conflict)).IsEmpty();
    }

    [Test]
    public async Task APluginTheProjectHasIsNotSelectedAgain()
    {
        var state = new WizardState {Flow = Flow.AddByTech, SelectedPlugins = new HashSet<string>()};
        state.SetExisting("SqlServer", true);
        TechSuggestions.Choose(state, "sqlserver", true);
        await Assert.That(state.Has("SqlServer")).IsFalse();
    }

    /// <summary>Windows-only plugins cannot be selected on another OS, so they are not suggested there (plan 10).</summary>
    [Test]
    public async Task WindowsOnlyPluginsAreNotSuggestedElsewhere()
    {
        var state = GeneratorTests.State(os: Os.Linux, ide: Ide.Rider);
        TechSuggestions.Choose(state, "wpf", true);

        await Assert.That(state.Has("Xaml")).IsFalse();
        await Assert.That(TechSuggestions.For(state).Select(_ => _.PluginId)).DoesNotContain("Xaml");
    }

    [Test]
    public Task Suggestions()
    {
        var state = GeneratorTests.State();
        TechSuggestions.Choose(state, "efcore", true);
        TechSuggestions.Choose(state, "aspnetcore", true);
        return Verify(TechSuggestions.For(state).Select(_ => $"{_.PluginId}: {(_.Recommended ? "recommended" : "related")} by {string.Join(", ", _.Because)}"));
    }
}
