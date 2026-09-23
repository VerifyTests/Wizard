using Wizard.Web.Components;

public class BreadcrumbTests : WebTestContext
{
    static WizardState MidFlow() =>
        new()
        {
            Flow = Flow.New,
            Os = Os.Linux,
            Ide = Ide.Rider,
            Cli = CliPreference.Gui,
            Step = "tf"
        };

    IRenderedComponent<Breadcrumb> RenderRail(WizardState state, Action<string>? selected = null) =>
        Render<Breadcrumb>(_ => _
            .Add(breadcrumb => breadcrumb.State, state)
            .Add(breadcrumb => breadcrumb.Steps, FlowSteps.For(Flow.New))
            .Add(breadcrumb => breadcrumb.Today, GeneratorTests.Today)
            .Add(breadcrumb => breadcrumb.StepSelected, selected ?? (_ => { })));

    [Test]
    public Task Markup() =>
        Verify(RenderRail(MidFlow()).Markup, "html");

    [Test]
    public async Task CompletedStepsShowTheirValue()
    {
        var rail = RenderRail(MidFlow());
        var done = rail.FindAll("li.done .step-value").Select(_ => _.TextContent);
        await Assert.That(string.Join(" | ", done)).IsEqualTo("Linux | JetBrains Rider | Prefer GUI");
    }

    [Test]
    public async Task ClickingACompletedStepSelectsIt()
    {
        string? selected = null;
        var rail = RenderRail(MidFlow(), _ => selected = _);
        await rail.Find("button[data-step=ide]").ClickAsync(new());
        await Assert.That(selected).IsEqualTo("ide");
    }

    /// <summary>Steps after the first incomplete one are gated, so they are text rather than buttons.</summary>
    [Test]
    public async Task StepsPastAnIncompleteStepAreNotClickable()
    {
        var rail = RenderRail(MidFlow());
        await Assert.That(rail.FindAll("li.future").Select(_ => _.TextContent.Trim()))
            .IsEquivalentTo(
                ["Build server", "Tech stack", "Plugins", "Plugin options", "Maintenance fee", "Result"],
                TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(rail.FindAll("button[data-step=ci]").Count).IsEqualTo(0);
    }
}
