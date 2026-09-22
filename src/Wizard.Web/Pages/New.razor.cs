using Microsoft.AspNetCore.Components.Routing;
using Wizard.Web.Components;

namespace Wizard.Web.Pages;

/// <summary>
/// Flow A, new to Verify (plan 7.1). The state lives in the url (plan 8.1): it is parsed on load and on
/// browser back/forward, and every change is written back. Moving between steps adds a history entry,
/// so back goes to the previous step; editing a value replaces the current entry.
/// </summary>
public partial class New : IDisposable
{
    [Inject] public NavigationManager Navigation { get; set; } = null!;
    [Inject] public TimeProvider Time { get; set; } = null!;

    WizardState state = new();
    Date today;
    string lastUrl = "";

    static readonly IReadOnlyList<StepDefinition> steps = FlowSteps.For(Flow.New);

    StepDefinition current => steps.Single(_ => _.Id == state.Step);

    int CurrentIndex => steps.ToList().IndexOf(current);

    StepDefinition? PreviousStep
    {
        get
        {
            if (CurrentIndex == 0)
            {
                return null;
            }

            return steps[CurrentIndex - 1];
        }
    }

    StepDefinition? NextStep
    {
        get
        {
            if (CurrentIndex == steps.Count - 1)
            {
                return null;
            }

            return steps[CurrentIndex + 1];
        }
    }

    protected override void OnInitialized()
    {
        today = Date.FromDateTime(Time.GetUtcNow().UtcDateTime);
        LoadFromUrl();
        // A url that landed past an incomplete step, or that held values the state dropped, is corrected in place.
        var url = WizardStateUrl.ToRelativeUrl(state);
        if (url != Navigation.ToBaseRelativePath(Navigation.Uri))
        {
            Navigate(url, replace: true);
        }

        Navigation.LocationChanged += LocationChanged;
    }

    void LoadFromUrl()
    {
        var query = new Uri(Navigation.Uri).Query;
        state = WizardStateUrl.Parse(Flow.New, query);
        state.Step = FlowSteps.Reachable(state, today).Id;
    }

    void LocationChanged(object? sender, LocationChangedEventArgs args)
    {
        // our own navigation echoes back here; only back/forward or an edited url needs a reload
        if (Navigation.ToBaseRelativePath(args.Location) == lastUrl)
        {
            return;
        }

        LoadFromUrl();
        lastUrl = WizardStateUrl.ToRelativeUrl(state);
        StateHasChanged();
    }

    void Navigate(string url, bool replace)
    {
        lastUrl = url;
        Navigation.NavigateTo(url, replace: replace);
    }

    void Set(Action<WizardState> change)
    {
        change(state);
        Changed();
    }

    void Changed()
    {
        state.Normalize();
        Navigate(WizardStateUrl.ToRelativeUrl(state), replace: true);
    }

    void GoTo(string stepId)
    {
        state.Step = stepId;
        state.Step = FlowSteps.Reachable(state, today).Id;
        Navigate(WizardStateUrl.ToRelativeUrl(state), replace: false);
    }

    public void Dispose() =>
        Navigation.LocationChanged -= LocationChanged;

    IReadOnlyList<Choice<Ide>> IdeChoices =>
        DisplayNames.IdesFor(state.Os ?? Os.Windows)
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
        new(TestFramework.MSTest, "MSTest", "Test classes opt in with [UsesVerify]."),
        new(TestFramework.Fixie, "Fixie", "No Microsoft.Testing.Platform runner, so it uses VSTest."),
        new(TestFramework.Expecto, "Expecto", "Tests in F#.")
    ];

    static readonly IReadOnlyList<Choice<BuildServer>> buildServerChoices =
    [
        new(BuildServer.GitHubActions, "GitHub Actions"),
        new(BuildServer.AzureDevOps, "Azure DevOps"),
        new(BuildServer.AppVeyor, "AppVeyor"),
        new(BuildServer.None, "No build server")
    ];
}
