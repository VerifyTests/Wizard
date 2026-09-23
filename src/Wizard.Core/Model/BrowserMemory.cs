/// <summary>What the browser remembers between visits (plan 8.2), each as it appears in a url.</summary>
/// <param name="Tech">A comma list of tech ids.</param>
/// <param name="Existing">A comma list of the plugin ids a project already has.</param>
/// <param name="Sponsor">The maintenance fee declaration, as a query string.</param>
public sealed record Remembered(string? Tech, string? Existing, string? Sponsor)
{
    public static Remembered None { get; } = new(null, null, null);
}

/// <summary>Which remembered values filled in a state, so the steps can say where an answer came from.</summary>
public sealed record Restored(bool Tech, bool Existing, bool Sponsor)
{
    public bool Any => Tech || Existing || Sponsor;
}

/// <summary>
/// The rules for the browser's memory (plan 8.2). A url always wins: remembered values only fill in
/// what a url leaves out, so a shared link means the same thing in every browser. Only answers to
/// questions the flow asks are read or written, so the new-project flow neither clears nor overwrites
/// the list of existing plugins an add flow kept.
/// </summary>
public static class BrowserMemory
{
    public const string Prefix = "verify-wizard:";
    public const string TechKey = Prefix + "tech";
    public const string ExistingKey = Prefix + "existing";
    public const string SponsorKey = Prefix + "sponsor";

    public static IReadOnlyList<string> Keys { get; } = [TechKey, ExistingKey, SponsorKey];

    public static bool AsksTech(Flow flow) =>
        FlowSteps.For(flow).Contains(FlowSteps.Tech);

    public static bool AsksExisting(Flow flow) =>
        FlowSteps.For(flow).Contains(FlowSteps.Existing);

    /// <summary>Fills in, from what the browser remembers, whatever <paramref name="query"/> leaves out.</summary>
    public static Restored Seed(WizardState state, string? query, Remembered remembered)
    {
        var tech = false;
        var existing = false;
        var sponsor = false;

        if (AsksExisting(state.Flow) &&
            !string.IsNullOrEmpty(remembered.Existing) &&
            !WizardStateUrl.HasKey(query, WizardStateUrl.ExistingKey))
        {
            foreach (var id in Split(remembered.Existing).Where(Plugins.Contains))
            {
                state.SetExisting(id, true);
            }

            existing = state.ExistingPlugins.Count > 0;
        }

        if (AsksTech(state.Flow) &&
            !string.IsNullOrEmpty(remembered.Tech) &&
            !WizardStateUrl.HasKey(query, WizardStateUrl.TechKey))
        {
            state.Techs = new HashSet<string>(Split(remembered.Tech).Where(Techs.Contains), StringComparer.Ordinal);
            tech = state.Techs.Count > 0;

            // A url that says nothing about plugins gets what the remembered stack recommends, as if
            // each tech had just been chosen. One that names plugins keeps exactly those.
            if (!WizardStateUrl.HasKey(query, WizardStateUrl.PluginsKey))
            {
                TechSuggestions.ApplyAll(state);
            }
        }

        if (!string.IsNullOrEmpty(remembered.Sponsor) &&
            !WizardStateUrl.HasKey(query, WizardStateUrl.SponsorKey))
        {
            WizardStateUrl.ApplySponsorQuery(state, remembered.Sponsor);
            sponsor = state.SponsorMode != SponsorMode.NotChosen;
        }

        state.Normalize();
        return new(tech, existing, sponsor);
    }

    /// <summary>
    /// What to remember for a state: a value for each question the flow asks, empty meaning forget it.
    /// Questions the flow does not ask come back null, meaning leave whatever is kept alone.
    /// </summary>
    public static Remembered For(WizardState state)
    {
        string? tech = null;
        if (AsksTech(state.Flow))
        {
            tech = string.Join(",", Techs.All.Where(_ => state.Techs.Contains(_.Id)).Select(_ => _.Id));
        }

        string? existing = null;
        if (AsksExisting(state.Flow))
        {
            existing = string.Join(",", Plugins.All.Where(_ => state.IsExisting(_.Id)).Select(_ => _.Id));
        }

        return new(tech, existing, WizardStateUrl.SponsorQuery(state));
    }

    static IEnumerable<string> Split(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
