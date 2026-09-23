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

    /// <summary>
    /// Plugin ids to include, as a set so membership is cheap; emitted in registry order everywhere,
    /// so the url and the generated output do not depend on insertion order. Collections are replaced
    /// rather than mutated, so <c>with { }</c> copies do not share them.
    /// </summary>
    public IReadOnlySet<string> SelectedPlugins { get; set; } = new HashSet<string>(DefaultPlugins(Flow.New), StringComparer.Ordinal);

    /// <summary>
    /// In a new project Verify.DiffPlex is selected until it is deselected: an inline diff on a failed
    /// text snapshot helps in any project, and the wizard has recommended it unconditionally since the
    /// old pages. Adding to an existing project starts from nothing, because the project already has
    /// whatever it had.
    /// </summary>
    public static IReadOnlyList<string> DefaultPlugins(Flow flow)
    {
        if (flow == Flow.New)
        {
            return [Plugins.DiffPlexId];
        }

        return [];
    }

    /// <summary>
    /// Plugins the project already has (plan 7.2 step 2). They are never generated, but they take
    /// part in the interaction rules, because adding a plugin next to one of them can change how
    /// the existing one has to be initialized.
    /// </summary>
    public IReadOnlySet<string> ExistingPlugins { get; set; } = emptySet;

    /// <summary>Tech ids (plan 10). They only seed the plugin selection; nothing is generated from them.</summary>
    public IReadOnlySet<string> Techs { get; set; } = emptySet;

    static IReadOnlySet<string> emptySet = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Plugin id to depth. Missing means <see cref="Depth.Verbose"/> (plan D7).</summary>
    public IReadOnlyDictionary<string, Depth> Depths { get; set; } = emptyDepths;

    /// <summary>Choice id to value, for the per-plugin and per-rule options (plan 7.1 step 8).</summary>
    public IReadOnlyDictionary<string, string> Choices { get; set; } = emptyChoices;

    static IReadOnlyDictionary<string, Depth> emptyDepths = new Dictionary<string, Depth>(StringComparer.Ordinal);
    static IReadOnlyDictionary<string, string> emptyChoices = new Dictionary<string, string>(StringComparer.Ordinal);

    public bool Has(string pluginId) =>
        SelectedPlugins.Contains(pluginId);

    public bool IsExisting(string pluginId) =>
        ExistingPlugins.Contains(pluginId);

    /// <summary>Selected or already in the project: what the interaction rules are evaluated against.</summary>
    public bool Uses(string pluginId) =>
        Has(pluginId) || IsExisting(pluginId);

    /// <summary>Everything the project will have, existing and selected.</summary>
    public IReadOnlySet<string> AllPlugins =>
        new HashSet<string>(SelectedPlugins.Concat(ExistingPlugins), StringComparer.Ordinal);

    public void SetExisting(string pluginId, bool existing)
    {
        var set = new HashSet<string>(ExistingPlugins, StringComparer.Ordinal);
        if (existing)
        {
            set.Add(pluginId);
        }
        else
        {
            set.Remove(pluginId);
        }

        ExistingPlugins = set;
    }

    public Depth DepthOf(string pluginId) =>
        Depths.GetValueOrDefault(pluginId, Depth.Verbose);

    public void Select(string pluginId, bool selected)
    {
        var selection = new HashSet<string>(SelectedPlugins, StringComparer.Ordinal);
        if (selected)
        {
            selection.Add(pluginId);
        }
        else
        {
            selection.Remove(pluginId);
        }

        SelectedPlugins = selection;
    }

    public void SetDepth(string pluginId, Depth depth) =>
        Depths = new Dictionary<string, Depth>(Depths, StringComparer.Ordinal)
        {
            [pluginId] = depth
        };

    public void SetChoice(string choiceId, string value) =>
        Choices = new Dictionary<string, string>(Choices, StringComparer.Ordinal)
        {
            [choiceId] = value
        };

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

        NormalizeFlow();
        NormalizePlugins();

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

        Step = FlowSteps.Nearest(this, Step);
    }

    /// <summary>
    /// A record compares its members with <see cref="object.Equals(object?)"/>, which for a set or a
    /// dictionary is reference equality, so two states holding the same selection would differ. Every
    /// comparison here is about what the state says, which is also what the url carries.
    /// </summary>
    public bool Equals(WizardState? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Flow == other.Flow &&
               Os == other.Os &&
               Ide == other.Ide &&
               Cli == other.Cli &&
               TestFramework == other.TestFramework &&
               BuildServer == other.BuildServer &&
               SolutionName == other.SolutionName &&
               SponsorMode == other.SponsorMode &&
               SponsorAccount == other.SponsorAccount &&
               SponsorshipStart == other.SponsorshipStart &&
               SponsorshipPrivateUntil == other.SponsorshipPrivateUntil &&
               Exemption == other.Exemption &&
               SponsorUntil == other.SponsorUntil &&
               Step == other.Step &&
               SelectedPlugins.SetEquals(other.SelectedPlugins) &&
               ExistingPlugins.SetEquals(other.ExistingPlugins) &&
               Techs.SetEquals(other.Techs) &&
               SameEntries(Depths, other.Depths) &&
               SameEntries(Choices, other.Choices);
    }

    static bool SameEntries<T>(IReadOnlyDictionary<string, T> left, IReadOnlyDictionary<string, T> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        return left.All(_ => right.TryGetValue(_.Key, out var value) && Equals(value, _.Value));
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Flow);
        hash.Add(Os);
        hash.Add(Ide);
        hash.Add(Cli);
        hash.Add(TestFramework);
        hash.Add(BuildServer);
        hash.Add(SolutionName);
        hash.Add(SponsorMode);
        hash.Add(SponsorAccount);
        hash.Add(SponsorshipStart);
        hash.Add(SponsorshipPrivateUntil);
        hash.Add(Exemption);
        hash.Add(SponsorUntil);
        hash.Add(Step);
        hash.Add(SelectedPlugins.Count);
        hash.Add(ExistingPlugins.Count);
        hash.Add(Techs.Count);
        hash.Add(Depths.Count);
        hash.Add(Choices.Count);
        return hash.ToHashCode();
    }

    /// <summary>
    /// Drops answers to questions the flow does not ask (plan 7.2): adding to an existing project skips
    /// the environment questions and the build server, and a new project has nothing existing. Only the
    /// flow that has a tech step keeps techs.
    /// </summary>
    void NormalizeFlow()
    {
        if (Flow != Flow.New)
        {
            Os = null;
            Ide = null;
            Cli = null;
            BuildServer = null;
        }
        else
        {
            ExistingPlugins = emptySet;
        }

        if (Flow == Flow.Add)
        {
            Techs = emptySet;
        }
    }

    /// <summary>
    /// Drops ids the registry does not have, and depths and choices that nothing selected uses, so the
    /// url holds exactly what the state holds and a stale link cannot carry hidden values.
    /// </summary>
    /// <summary>
    /// Why a plugin cannot be selected with the chosen operating system and test framework, or null
    /// when it can. An answer not given yet rules nothing out; the add flows never ask for the OS.
    /// </summary>
    public string? Unavailable(string pluginId)
    {
        var definition = Plugins.ById[pluginId];
        if (Os is global::Os.MacOS or global::Os.Linux &&
            definition.Platform == Platform.WindowsOnly)
        {
            return $"Only runs on Windows, and the operating system chosen is {Os.Value.Name()}.";
        }

        if (TestFramework is { } framework &&
            !definition.Supports(framework))
        {
            var reason = definition.UnsupportedTestFrameworks.First(_ => _.Framework == framework).Reason;
            return $"Not available for {framework.Name()}: {reason}";
        }

        return null;
    }

    void NormalizePlugins()
    {
        ExistingPlugins = new HashSet<string>(
            ExistingPlugins.Where(Plugins.Contains),
            StringComparer.Ordinal);

        // Something the project already has cannot be added again, and nothing that cannot run on the
        // chosen operating system can be added at all.
        SelectedPlugins = new HashSet<string>(
            SelectedPlugins.Where(_ => Plugins.Contains(_) && !IsExisting(_) && Unavailable(_) == null),
            StringComparer.Ordinal);

        Techs = new HashSet<string>(
            Techs.Where(Techs.Contains),
            StringComparer.Ordinal);

        Depths = new Dictionary<string, Depth>(
            Depths.Where(_ => _.Value != Depth.Verbose && Has(_.Key)),
            StringComparer.Ordinal);

        var available = SelectedPlugins
            .SelectMany(_ => Plugins.ById[_].Choices)
            .Concat(InteractionRules.ChoicesFor(AllPlugins))
            .ToDictionary(_ => _.Id, StringComparer.Ordinal);

        Choices = new Dictionary<string, string>(
            Choices.Where(_ =>
                available.TryGetValue(_.Key, out var choice) &&
                choice.Options.Any(option => option.Value == _.Value) &&
                choice.Default != _.Value),
            StringComparer.Ordinal);
    }
}
