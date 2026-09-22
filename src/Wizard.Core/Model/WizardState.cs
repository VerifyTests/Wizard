namespace Wizard.Core;

/// <summary>
/// Everything the wizard knows. The UI mutates it; <see cref="WizardStateUrl"/> round-trips it through
/// the query string so every point in a flow is a bookmark (plan 8.1); the generators read it.
/// </summary>
public sealed record WizardState
{
    public const string DefaultSolutionName = "VerifySample";

    public Flow Flow { get; set; }

    public Os? Os { get; set; }
    public Ide? Ide { get; set; }
    public CliPreference? Cli { get; set; }
    public TestFramework? TestFramework { get; set; }
    public BuildServer? BuildServer { get; set; }

    public string SolutionName { get; set; } = DefaultSolutionName;

    public SponsorMode SponsorMode { get; set; }

    /// <summary>The GitHub organization or user the VerifyTests sponsorship is made from.</summary>
    public string SponsorAccount { get; set; } = "";

    /// <summary>Set when the sponsorship began after the referenced Verify version was packed (plan D8).</summary>
    public Date? SponsorshipStart { get; set; }

    /// <summary>yyyy-MM. Set when the sponsorship is private on GitHub, and so never bundled.</summary>
    public string SponsorshipPrivateUntil { get; set; } = "";

    public Exemption? Exemption { get; set; }

    /// <summary>yyyy-MM. The end month for <see cref="SponsorMode.Exempt"/> and <see cref="SponsorMode.PrivateArrangement"/>.</summary>
    public string SponsorUntil { get; set; } = "";

    /// <summary>The current step id; see <see cref="FlowSteps"/>.</summary>
    public string Step { get; set; } = "";

    /// <summary>
    /// Drops values that cannot apply: an IDE the chosen OS does not offer, a solution name that is not
    /// a usable project name, and a step id the flow does not have. Called after every parse and change.
    /// </summary>
    public void Normalize()
    {
        if (Os != null &&
            Ide != null &&
            !DisplayNames.IdesFor(Os.Value).Contains(Ide.Value))
        {
            Ide = null;
        }

        SolutionName = SolutionNames.Clean(SolutionName);

        // Values for other sponsor modes are dropped, so the url holds everything the state does.
        if (SponsorMode != SponsorMode.Sponsor)
        {
            SponsorAccount = "";
            SponsorshipStart = null;
            SponsorshipPrivateUntil = "";
        }

        if (SponsorMode != SponsorMode.Exempt)
        {
            Exemption = null;
        }

        if (SponsorMode is not (SponsorMode.Exempt or SponsorMode.PrivateArrangement))
        {
            SponsorUntil = "";
        }

        var steps = FlowSteps.For(Flow);
        if (!steps.Any(_ => _.Id == Step))
        {
            Step = steps[0].Id;
        }
    }
}
