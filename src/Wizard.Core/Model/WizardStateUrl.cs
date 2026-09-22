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

        return string.Join("&", pairs.Select(_ => $"{_.Key}={Uri.EscapeDataString(_.Value)}"));
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
            SolutionName = Get(NameKey) ?? WizardState.DefaultSolutionName
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
