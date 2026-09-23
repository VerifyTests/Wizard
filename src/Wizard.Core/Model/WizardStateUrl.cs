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
    public const string PluginsKey = "ext";
    public const string MinimalKey = "min";
    public const string ChoicesKey = "opt";
    public const string TechKey = "tech";
    public const string ExistingKey = "have";
    public const string InlineKey = "inline";

    /// <summary>The <see cref="PluginsKey"/> value meaning "nothing at all", as opposed to "unset".</summary>
    public const string NoPlugins = "none";

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

        Add(TechKey, string.Join(',', Techs.All.Where(_ => state.Techs.Contains(_.Id)).Select(_ => _.Id)));
        Add(ExistingKey, string.Join(',', Plugins.All.Where(_ => state.IsExisting(_.Id)).Select(_ => _.Id)));

        // Registry order, not insertion order, so the same selection is always the same link.
        var selected = Plugins.All
            .Where(_ => state.Has(_.Id))
            .Select(_ => _.Id)
            .ToList();
        if (!selected.SequenceEqual(WizardState.DefaultPlugins(state.Flow), StringComparer.Ordinal))
        {
            // A link that selects nothing still has to say so, or it would read as the default.
            Add(PluginsKey, selected.Count == 0 ? NoPlugins : string.Join(',', selected));
        }

        Add(MinimalKey, string.Join(',', selected.Where(_ => state.DepthOf(_) == Depth.Minimal)));
        Add(ChoicesKey, string.Join(',', state.Choices.OrderBy(_ => _.Key, StringComparer.Ordinal).Select(_ => $"{_.Key}:{_.Value}")));
        if (state.InlineSnapshots)
        {
            Add(InlineKey, "true");
        }

        pairs.AddRange(SponsorPairs(state));

        return string.Join('&', pairs.Select(_ => $"{_.Key}={Escape(_.Value)}"));
    }

    /// <summary>The maintenance fee declaration alone, as a query string, which is what the browser keeps.</summary>
    public static string SponsorQuery(WizardState state) =>
        string.Join('&', SponsorPairs(state).Select(_ => $"{_.Key}={Escape(_.Value)}"));

    static List<(string Key, string Value)> SponsorPairs(WizardState state)
    {
        var pairs = new List<(string Key, string Value)>();

        void Add(string key, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                pairs.Add((key, value));
            }
        }

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

        return pairs;
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
            InlineSnapshots = Get(InlineKey) == "true",
            SelectedPlugins = ParsePlugins(flow, Get(PluginsKey)),
            ExistingPlugins = new HashSet<string>(SplitList(Get(ExistingKey)), StringComparer.Ordinal),
            Techs = new HashSet<string>(SplitList(Get(TechKey)), StringComparer.Ordinal),
            Choices = ParseChoices(Get(ChoicesKey)),
            Depths = SplitList(Get(MinimalKey))
                .ToDictionary(_ => _, _ => Depth.Minimal, StringComparer.Ordinal)
        };

        ApplySponsor(state, Get);
        state.Normalize();
        return state;
    }

    /// <summary>Reads a declaration kept by <see cref="SponsorQuery"/> into a state.</summary>
    public static void ApplySponsorQuery(WizardState state, string query)
    {
        var values = ParseQuery(query);
        ApplySponsor(state, _ => values.GetValueOrDefault(_));
    }

    static void ApplySponsor(WizardState state, Func<string, string?> get)
    {
        switch (get(SponsorKey))
        {
            case "Sponsor":
                state.SponsorMode = SponsorMode.Sponsor;
                state.SponsorAccount = get(AccountKey) ?? "";
                if (Date.TryParseExact(get(StartKey), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start))
                {
                    state.SponsorshipStart = start;
                }

                state.SponsorshipPrivateUntil = get(PrivateKey) ?? "";
                break;
            case "Exempt":
                state.SponsorMode = SponsorMode.Exempt;
                state.Exemption = ParseEnum<Exemption>(get(ExemptKey));
                state.SponsorUntil = get(UntilKey) ?? "";
                break;
            case "Private":
                state.SponsorMode = SponsorMode.PrivateArrangement;
                state.SponsorUntil = get(UntilKey) ?? "";
                break;
            case "Ignore":
                state.SponsorMode = SponsorMode.Ignore;
                break;
        }
    }

    /// <summary>
    /// Whether a query names a key at all. The browser's remembered values only fill in what a url
    /// leaves out, so a shared link always means the same thing (plan 8.2).
    /// </summary>
    public static bool HasKey(string? query, string key) =>
        ParseQuery(query).ContainsKey(key);

    /// <summary>
    /// Commas and colons separate the list and pair values, and both are legal unescaped in a query
    /// string, so they are put back: a shared link is meant to be read, and <c>%2C</c> is not.
    /// </summary>
    static string Escape(string value) =>
        Uri.EscapeDataString(value)
            .Replace("%2C", ",")
            .Replace("%3A", ":");

    /// <summary>
    /// An absent key means the default selection, so a link made before a plugin existed still
    /// means what it meant. <see cref="NoPlugins"/> is how "nothing selected" is written.
    /// </summary>
    static HashSet<string> ParsePlugins(Flow flow, string? value)
    {
        if (value == null)
        {
            return [with(StringComparer.Ordinal), .. WizardState.DefaultPlugins(flow)];
        }

        if (value == NoPlugins)
        {
            return new(StringComparer.Ordinal);
        }

        return [with(StringComparer.Ordinal), .. SplitList(value)];
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
