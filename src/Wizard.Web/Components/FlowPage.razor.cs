using Microsoft.AspNetCore.Components.Routing;

namespace Wizard.Web.Components;

/// <summary>
/// One of the three flows (plan 7). The state lives in the url (plan 8.1): it is parsed on load and on
/// browser back/forward, and every change is written back. Moving between steps adds a history entry,
/// so back goes to the previous step; editing a value replaces the current entry. On first load the
/// browser's memory fills in whatever the url leaves out (plan 8.2).
/// </summary>
public partial class FlowPage : IDisposable
{
    [Inject] public NavigationManager Navigation { get; set; } = null!;
    [Inject] public TimeProvider Time { get; set; } = null!;
    [Inject] public BrowserStorage Storage { get; set; } = null!;

    [Parameter, EditorRequired] public Flow Flow { get; set; }

    /// <summary>An extension to start with selected, from a deep link such as <c>/add/EntityFramework</c> (plan D11).</summary>
    [Parameter] public string? SeedExtension { get; set; }

    public WizardState State { get; private set; } = new();

    Date today;
    string lastUrl = "";
    bool ready;
    Restored restored = new(false, false, false);
    Remembered written = Remembered.None;

    // Recomputed per render: selecting no extensions drops the options step from the flow.
    IReadOnlyList<StepDefinition> Steps => FlowSteps.For(State);

    StepDefinition Current => Steps.Single(_ => _.Id == State.Step);

    int CurrentIndex => Steps.ToList().IndexOf(Current);

    StepDefinition? PreviousStep
    {
        get
        {
            if (CurrentIndex == 0)
            {
                return null;
            }

            return Steps[CurrentIndex - 1];
        }
    }

    StepDefinition? NextStep
    {
        get
        {
            if (CurrentIndex == Steps.Count - 1)
            {
                return null;
            }

            return Steps[CurrentIndex + 1];
        }
    }

    protected override async Task OnInitializedAsync()
    {
        today = Date.FromDateTime(Time.GetUtcNow().UtcDateTime);
        var query = new Uri(Navigation.Uri).Query;
        State = WizardStateUrl.Parse(Flow, query);

        if (SeedExtension is { } seed &&
            Extensions.Contains(seed))
        {
            State.Select(seed, true);
            State.Normalize();
        }

        var remembered = await Storage.ReadAsync();
        restored = BrowserMemory.Seed(State, query, remembered);
        written = remembered;
        State.Step = FlowSteps.Reachable(State, today).Id;

        // A url that landed past an incomplete step, held values the state dropped, or left out
        // something the browser remembered, is corrected in place, so the link on screen is complete.
        var url = WizardStateUrl.ToRelativeUrl(State);
        if (url != Navigation.ToBaseRelativePath(Navigation.Uri))
        {
            Navigate(url, replace: true);
        }

        Navigation.LocationChanged += LocationChanged;
        ready = true;
    }

    void LocationChanged(object? sender, LocationChangedEventArgs args)
    {
        // our own navigation echoes back here; only back/forward or an edited url needs a reload
        if (Navigation.ToBaseRelativePath(args.Location) == lastUrl)
        {
            return;
        }

        State = WizardStateUrl.Parse(Flow, new Uri(Navigation.Uri).Query);
        State.Step = FlowSteps.Reachable(State, today).Id;
        lastUrl = WizardStateUrl.ToRelativeUrl(State);
        StateHasChanged();
    }

    void Navigate(string url, bool replace)
    {
        lastUrl = url;
        Navigation.NavigateTo(url, replace: replace);
    }

    Task Set(Action<WizardState> change)
    {
        change(State);
        return Changed();
    }

    Task Changed()
    {
        State.Normalize();
        Navigate(WizardStateUrl.ToRelativeUrl(State), replace: true);
        return Remember();
    }

    /// <summary>Writes only what changed, so a step that never touches an answer never rewrites it.</summary>
    Task Remember()
    {
        var current = BrowserMemory.For(State);
        var changes = new Remembered(
            Difference(current.Tech, written.Tech),
            Difference(current.Existing, written.Existing),
            Difference(current.Sponsor, written.Sponsor));
        if (changes == Remembered.None)
        {
            return Task.CompletedTask;
        }

        written = new(current.Tech ?? written.Tech, current.Existing ?? written.Existing, current.Sponsor);
        return Storage.WriteAsync(changes);
    }

    /// <summary>The value to write: null when nothing changed, empty when it is to be forgotten.</summary>
    static string? Difference(string? current, string? kept)
    {
        if (current == null ||
            current == (kept ?? ""))
        {
            return null;
        }

        return current;
    }

    void GoTo(string stepId)
    {
        State.Step = stepId;
        State.Step = FlowSteps.Reachable(State, today).Id;
        Navigate(WizardStateUrl.ToRelativeUrl(State), replace: false);
    }

    public void Dispose() =>
        Navigation.LocationChanged -= LocationChanged;

    IReadOnlyList<Choice<Ide>> IdeChoices =>
        DisplayNames.IdesFor(State.Os ?? Os.Windows)
            .Select(_ => new Choice<Ide>(_, _.Name(), IdeDescription(_)))
            .ToList();

    static string IdeDescription(Ide ide) =>
        ide switch
        {
            Ide.VisualStudioWithReSharper => "Adds the ReSharper plugin and settings.",
            Ide.Rider => "Adds the Rider plugin and settings.",
            Ide.VsCode => "Includes the VS Code line ending guidance.",
            _ => ""
        };

    static readonly IReadOnlyList<Choice<Os>> osChoices =
    [
        new(Os.Windows, "Windows"),
        new(Os.MacOS, "MacOS"),
        new(Os.Linux, "Linux")
    ];

    static readonly IReadOnlyList<Choice<CliPreference>> cliChoices =
    [
        new(CliPreference.Cli, "Prefer CLI", "dotnet add package, and Verify.Terminal for reviewing snapshots."),
        new(CliPreference.Gui, "Prefer GUI", "Package references to paste into project files.")
    ];

    static readonly IReadOnlyList<Choice<TestFramework>> testFrameworkChoices =
    [
        new(TestFramework.XunitV3, "xUnit v3"),
        new(TestFramework.NUnit, "NUnit"),
        new(TestFramework.TUnit, "TUnit"),
        new(TestFramework.MSTest, "MSTest"),
        new(TestFramework.Fixie, "Fixie"),
        new(TestFramework.Expecto, "Expecto")
    ];

    static readonly IReadOnlyList<Choice<BuildServer>> buildServerChoices =
    [
        new(BuildServer.GitHubActions, "GitHub Actions"),
        new(BuildServer.AzureDevOps, "Azure DevOps"),
        new(BuildServer.AppVeyor, "AppVeyor"),
        new(BuildServer.None, "No build server")
    ];
}
