public enum Severity
{
    /// <summary>Worth knowing: a companion suggestion, or an explanation of combined behaviour.</summary>
    Info,

    /// <summary>A side effect the combination has that neither plugin has alone.</summary>
    Warning,

    /// <summary>Mutually exclusive. The plugin step blocks moving on until one is deselected.</summary>
    Conflict
}

/// <param name="Members">Plugin ids. Two or more selected is a <see cref="Severity.Conflict"/>.</param>
/// <param name="Reason">Why they cannot co-exist, rendered in the notice and in the guide.</param>
public sealed record ExclusiveGroup(
    string Id,
    string Label,
    string Reason,
    IReadOnlyList<string> Members)
{
    /// <summary>Members that only belong to the group while a choice holds one of the listed values.</summary>
    public IReadOnlyList<GroupCondition> Conditions { get; init; } = [];

    public bool Holds(string member, IReadOnlyDictionary<string, string> choices)
    {
        var condition = Conditions.FirstOrDefault(_ => _.Member == member);
        if (condition == null)
        {
            return true;
        }

        return condition.Values.Contains(choices.GetValueOrDefault(condition.ChoiceId, condition.Default));
    }
}

public sealed record GroupCondition(
    string Member,
    string ChoiceId,
    string Default,
    IReadOnlyList<string> Values);

/// <summary>One rule firing for one selection (plan 11).</summary>
/// <param name="Involved">Plugin ids, in registry order, that made the rule fire.</param>
public sealed record InteractionResult(
    string RuleId,
    Severity Severity,
    IReadOnlyList<string> Involved,
    string Message)
{
    /// <summary>The option that resolves it, rendered on the options step.</summary>
    public PluginChoice? Choice { get; init; }

    /// <summary>The value in force, whether chosen or defaulted.</summary>
    public string? ChosenValue { get; init; }

    /// <summary>What the choice means for the generated code, one line per option.</summary>
    public IReadOnlyList<string> Notes { get; init; } = [];
}

/// <param name="All">Every one of these must be selected.</param>
/// <param name="Any">At least <see cref="InteractionRule.AnyCount"/> of these must be selected.</param>
/// <param name="Without">None of these may be selected.</param>
public sealed record InteractionRule
{
    public required string Id { get; init; }
    public required Severity Severity { get; init; }
    public required string Message { get; init; }
    public IReadOnlyList<string> All { get; init; } = [];
    public IReadOnlyList<string> Any { get; init; } = [];
    public int AnyCount { get; init; } = 1;
    public IReadOnlyList<string> Without { get; init; } = [];
    public PluginChoice? Choice { get; init; }
    public IReadOnlyList<string> Notes { get; init; } = [];

    /// <summary>Ordering edges the rule imposes on the module initializer (plan 11.3).</summary>
    public IReadOnlyList<OrderEdge> Order { get; init; } = [];

    /// <summary>
    /// Statements that stand in for a plugin's own while this rule fires and its choice holds
    /// <see cref="RuleStatements.WhenValue"/>. A rule without a choice uses an empty value.
    /// </summary>
    public IReadOnlyList<RuleStatements> Replace { get; init; } = [];

    /// <summary>Statements the rule adds that belong to no single plugin.</summary>
    public IReadOnlyList<RuleStatements> Add { get; init; } = [];

    /// <summary>The §23 upstream change that would make this rule unnecessary.</summary>
    public string? RetiredBy { get; init; }

    public bool Fires(IReadOnlySet<string> selected)
    {
        if (!All.All(selected.Contains))
        {
            return false;
        }

        if (Any.Count > 0 &&
            Any.Count(selected.Contains) < AnyCount)
        {
            return false;
        }

        return !Without.Any(selected.Contains);
    }

    public IReadOnlyList<string> InvolvedIn(IReadOnlySet<string> selected) =>
    [
        .. Plugins.All
            .Select(_ => _.Id)
            .Where(_ => selected.Contains(_) && (All.Contains(_) || Any.Contains(_)))
    ];
}

/// <param name="WhenValue">The choice value this edge applies to; empty means whatever is chosen.</param>
public sealed record OrderEdge(string Before, string After, string WhenValue = "");

/// <param name="WhenValue">The choice value this applies to; empty for a rule with no choice.</param>
/// <param name="Target">The plugin whose statements are replaced, or, for an added statement, the
/// plugin it is ordered with.</param>
public sealed record RuleStatements(
    string WhenValue,
    string Target,
    IReadOnlyList<InitializeStatement> Statements)
{
    /// <summary>Which block of the module initializer an added statement belongs to.</summary>
    public InitializePhase Phase { get; init; } = InitializePhase.AfterDiscovery;

    public IReadOnlyList<string> Usings { get; init; } = [];
}

/// <summary>
/// The hard-coded interactions between plugins (plan 11). Notices that follow from a single
/// plugin's own data, such as needing Windows or a licence key, are derived in
/// <see cref="PlanBuilder"/> instead, so this list holds only genuine combinations.
/// </summary>
public static partial class InteractionRules
{
    /// <summary>
    /// Every rule and group that fires for a selection, ordered most severe first. Existing plugins
    /// count: adding one next to something the project already has can change how that one has to be
    /// initialized (plan 7.2).
    /// </summary>
    public static IReadOnlyList<InteractionResult> For(WizardState state)
    {
        var selected = state.AllPlugins;
        var results = new List<InteractionResult>();

        foreach (var group in Groups)
        {
            var members = Plugins.All
                .Select(_ => _.Id)
                .Where(_ => selected.Contains(_) && group.Members.Contains(_) && group.Holds(_, state.Choices))
                .ToList();
            if (members.Count > 1)
            {
                results.Add(
                    new(
                        group.Id,
                        Severity.Conflict,
                        members,
                        $"{Join(members)} cannot be used together: {group.Reason} Keep one of them."));
            }
        }

        foreach (var rule in Rules.Where(_ => _.Fires(selected)))
        {
            results.Add(
                new(rule.Id, rule.Severity, rule.InvolvedIn(selected), rule.Message)
                {
                    Choice = rule.Choice,
                    ChosenValue = Value(rule.Choice, state),
                    Notes = rule.Notes
                });
        }

        return
        [
            .. results
                .OrderByDescending(_ => _.Severity)
                .ThenBy(_ => _.RuleId, StringComparer.Ordinal)
        ];
    }

    /// <summary>The choices the options step shows for rules that are currently firing.</summary>
    public static IEnumerable<PluginChoice> ChoicesFor(IReadOnlySet<string> selected) =>
        Rules
            .Where(_ => _.Choice != null && _.Fires(selected))
            .Select(_ => _.Choice!);

    /// <summary>Ordering edges every rule in force imposes, as plugin id pairs (plan 11.3).</summary>
    public static IEnumerable<OrderEdge> Edges(WizardState state) =>
        Active(state)
            .SelectMany(_ => _.Rule.Order.Where(edge => edge.WhenValue is "" || edge.WhenValue == _.Value))
            .Where(_ => state.Uses(_.Before) && state.Uses(_.After));

    /// <summary>The rules in force for a state, paired with the choice value that applies.</summary>
    public static IEnumerable<(InteractionRule Rule, string Value)> Active(WizardState state)
    {
        foreach (var rule in Rules.Where(_ => _.Fires(state.AllPlugins)))
        {
            yield return (rule, Value(rule.Choice, state) ?? "");
        }
    }

    /// <summary>The statements standing in for a plugin's own, when a rule replaces them.</summary>
    public static IReadOnlyList<InitializeStatement>? Replacement(WizardState state, string pluginId)
    {
        foreach (var (rule, value) in Active(state))
        {
            var replacement = rule.Replace.FirstOrDefault(_ => _.Target == pluginId && _.WhenValue == value);
            if (replacement != null)
            {
                return replacement.Statements;
            }
        }

        return null;
    }

    /// <summary>Statements the active rules add that belong to no single plugin.</summary>
    public static IEnumerable<RuleStatements> Additions(WizardState state) =>
        Active(state).SelectMany(_ => _.Rule.Add.Where(addition => addition.WhenValue == _.Value));

    static string? Value(PluginChoice? choice, WizardState state)
    {
        if (choice == null)
        {
            return null;
        }

        return state.Choices.GetValueOrDefault(choice.Id, choice.Default);
    }

    /// <summary>"A and B", "A, B and C".</summary>
    internal static string Join(IReadOnlyList<string> names)
    {
        if (names.Count == 1)
        {
            return names[0];
        }

        return $"{string.Join(", ", names.Take(names.Count - 1))} and {names[^1]}";
    }
}
