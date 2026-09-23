namespace Wizard.Core;

/// <summary>
/// The query string encoding of <see cref="WizardState"/> (plan 8.1). Values stay readable, because
/// shared links are meant to be read. Defaults are omitted. Parsing is total: an unknown or malformed
/// value is dropped rather than throwing, so any url lands somewhere sensible.
/// </summary>
public static class WizardStateUrl
{
    public const string StepKey = "step";
    public const string OsKey = "os";
    public const string IdeKey = "ide";
    public const string CliKey = "cli";
    public const string TestFrameworkKey = "tf";
    public const string BuildServerKey = "ci";
    public const string NameKey = "name";
    public const string ExtensionsKey = "ext";
    public const string MinimalKey = "min";
    public const string ChoicesKey = "opt";

    /// <summary>The <see cref="ExtensionsKey"/> value meaning "nothing at all", as opposed to "unset".</summary>
    public const string NoExtensions = "none";
    public const string SponsorKey = "sponsor";
    public const string AccountKey = "account";
    public const string StartKey = "start";
    public const string PrivateKey = "private";
    public const string ExemptKey = "exempt";
    public const string UntilKey = "until";

    public static string RoutePath(Flow flow) =>
        flow switch
        {
            Flow.New => "new",
            Flow.Add => "add",
            Flow.AddByTech => "add/by-tech",
            _ => throw new ArgumentOutOfRangeException(nameof(flow), flow, null)
        };

    /// <summary>Relative to the site base, e.g. <c>new?step=tf&amp;os=Windows</c>.</summary>
    public static string ToRelativeUrl(WizardState state)
    {
        var query = ToQuery(state);
        var path = RoutePath(state.Flow);
        if (query.Length == 0)
        {
            return path;
        }

        return $"{path}?{query}";
    }

    public static string ToAbsoluteUrl(WizardState state) =>
        $"{WizardLinks.Base}/{ToRelativeUrl(state)}";

    public static string ToQuery(WizardState state)
    {
        var pairs = new List<(string Key, string Value)>();

        void Add(string key, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                pairs.Add((key, value));
            }
        }

        Add(StepKey, state.Step);
        Add(OsKey, state.Os?.ToString());
        Add(IdeKey, state.Ide?.ToString());
        Add(CliKey, state.Cli?.ToString());
        Add(TestFrameworkKey, state.TestFramework?.ToString());
        Add(BuildServerKey, state.BuildServer?.ToString());
        if (state.SolutionName != WizardState.DefaultSolutionName)
        {
            Add(NameKey, state.SolutionName);
        }

        // Registry order, not insertion order, so the same selection is always the same link.
        var selected = Extensions.All
            .Where(_ => state.Has(_.Id))
            .Select(_ => _.Id)
            .ToList();
        if (!selected.SequenceEqual(WizardState.DefaultExtensions, StringComparer.Ordinal))
        {
            // A link that selects nothing still has to say so, or it would read as the default.
            Add(ExtensionsKey, selected.Count == 0 ? NoExtensions : string.Join(",", selected));
        }

        Add(MinimalKey, string.Join(",", selected.Where(_ => state.DepthOf(_) == Depth.Minimal)));
        Add(ChoicesKey, string.Join(",", state.Choices.OrderBy(_ => _.Key, StringComparer.Ordinal).Select(_ => $"{_.Key}:{_.Value}")));

        switch (state.SponsorMode)
        {
            case SponsorMode.Sponsor:
                Add(SponsorKey, "Sponsor");
                Add(AccountKey, state.SponsorAccount);
                Add(StartKey, state.SponsorshipStart?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                Add(PrivateKey, state.SponsorshipPrivateUntil);
                break;
            case SponsorMode.Exempt:
                Add(SponsorKey, "Exempt");
                Add(ExemptKey, state.Exemption?.ToString());
                Add(UntilKey, state.SponsorUntil);
                break;
            case SponsorMode.PrivateArrangement:
                Add(SponsorKey, "Private");
                Add(UntilKey, state.SponsorUntil);
                break;
            case SponsorMode.Ignore:
                Add(SponsorKey, "Ignore");
                break;
        }

        return string.Join("&", pairs.Select(_ => $"{_.Key}={Escape(_.Value)}"));
    }

    /// <param name="query">The query string, with or without the leading <c>?</c>.</param>
    public static WizardState Parse(Flow flow, string? query)
    {
        var values = ParseQuery(query);
        string? Get(string key) => values.GetValueOrDefault(key);

        var state = new WizardState
        {
            Flow = flow,
            Step = Get(StepKey) ?? "",
            Os = ParseEnum<Os>(Get(OsKey)),
            Ide = ParseEnum<Ide>(Get(IdeKey)),
            Cli = ParseEnum<CliPreference>(Get(CliKey)),
            TestFramework = ParseEnum<TestFramework>(Get(TestFrameworkKey)),
            BuildServer = ParseEnum<BuildServer>(Get(BuildServerKey)),
            SolutionName = Get(NameKey) ?? WizardState.DefaultSolutionName,
            SelectedExtensions = ParseExtensions(Get(ExtensionsKey)),
            Choices = ParseChoices(Get(ChoicesKey)),
            Depths = SplitList(Get(MinimalKey))
                .ToDictionary(_ => _, _ => Depth.Minimal, StringComparer.Ordinal)
        };

        switch (Get(SponsorKey))
        {
            case "Sponsor":
                state.SponsorMode = SponsorMode.Sponsor;
                state.SponsorAccount = Get(AccountKey) ?? "";
                if (Date.TryParseExact(Get(StartKey), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start))
                {
                    state.SponsorshipStart = start;
                }

                state.SponsorshipPrivateUntil = Get(PrivateKey) ?? "";
                break;
            case "Exempt":
                state.SponsorMode = SponsorMode.Exempt;
                state.Exemption = ParseEnum<Exemption>(Get(ExemptKey));
                state.SponsorUntil = Get(UntilKey) ?? "";
                break;
            case "Private":
                state.SponsorMode = SponsorMode.PrivateArrangement;
                state.SponsorUntil = Get(UntilKey) ?? "";
                break;
            case "Ignore":
                state.SponsorMode = SponsorMode.Ignore;
                break;
        }

        state.Normalize();
        return state;
    }

    /// <summary>
    /// Commas and colons separate the list and pair values, and both are legal unescaped in a query
    /// string, so they are put back: a shared link is meant to be read, and <c>%2C</c> is not.
    /// </summary>
    static string Escape(string value) =>
        Uri.EscapeDataString(value)
            .Replace("%2C", ",")
            .Replace("%3A", ":");

    /// <summary>
    /// An absent key means the default selection, so a link made before an extension existed still
    /// means what it meant. <see cref="NoExtensions"/> is how "nothing selected" is written.
    /// </summary>
    static HashSet<string> ParseExtensions(string? value)
    {
        if (value == null)
        {
            return new(WizardState.DefaultExtensions, StringComparer.Ordinal);
        }

        if (value == NoExtensions)
        {
            return new(StringComparer.Ordinal);
        }

        return new(SplitList(value), StringComparer.Ordinal);
    }

    /// <summary>A comma list. Empty entries are dropped; <see cref="WizardState.Normalize"/> drops unknown ids.</summary>
    static IEnumerable<string> SplitList(string? value)
    {
        if (value == null)
        {
            return [];
        }

        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.Ordinal);
    }

    /// <summary>A comma list of <c>key:value</c>. <see cref="WizardState.Normalize"/> drops unknown pairs.</summary>
    static Dictionary<string, string> ParseChoices(string? value)
    {
        var choices = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in SplitList(value))
        {
            var separator = pair.IndexOf(':');
            if (separator > 0)
            {
                choices.TryAdd(pair[..separator], pair[(separator + 1)..]);
            }
        }

        return choices;
    }

    /// <summary>Only exact member names are accepted: numbers and case variants would make several urls mean one state.</summary>
    static T? ParseEnum<T>(string? value)
        where T : struct, Enum
    {
        if (value != null &&
            Enum.GetNames<T>().Contains(value, StringComparer.Ordinal))
        {
            return Enum.Parse<T>(value);
        }

        return null;
    }

    static Dictionary<string, string> ParseQuery(string? query)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.IsNullOrEmpty(query))
        {
            return values;
        }

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            // UnescapeDataString leaves a malformed escape as-is rather than throwing
            var key = Uri.UnescapeDataString(pair[..separator]);
            var value = Uri.UnescapeDataString(pair[(separator + 1)..].Replace('+', ' '));
            // first occurrence wins, so appending a duplicate key cannot change a shared link
            values.TryAdd(key, value);
        }

        return values;
    }
}
