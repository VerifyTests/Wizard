namespace Wizard.Core;

/// <param name="Recommended">Pre-checked when its tech is chosen; otherwise it is listed as related.</param>
/// <param name="Because">The techs that suggest it, for the "related" tag and the card's hint.</param>
public sealed record Suggestion(string ExtensionId, bool Recommended, IReadOnlyList<string> Because);

/// <summary>Turns the chosen tech stack into suggested extensions (plan 10).</summary>
public static class TechSuggestions
{
    /// <summary>
    /// The extensions the stack suggests, in registry order. A Windows-only extension is left out when
    /// the chosen OS is not Windows, since it cannot be selected there.
    /// Verify.DiffPlex is recommended in every new project, and Verify.Terminal is listed alongside it.
    /// </summary>
    public static IReadOnlyList<Suggestion> For(WizardState state)
    {
        var recommended = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var related = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        void Note(Dictionary<string, List<string>> into, string extensionId, string because)
        {
            if (!into.TryGetValue(extensionId, out var list))
            {
                into[extensionId] = list = [];
            }

            list.Add(because);
        }

        foreach (var tech in Techs.All.Where(_ => state.Techs.Contains(_.Id)))
        {
            foreach (var id in tech.Recommended)
            {
                Note(recommended, id, tech.DisplayName);
            }

            foreach (var id in tech.Related)
            {
                Note(related, id, tech.DisplayName);
            }
        }

        if (state.Flow == Flow.New)
        {
            Note(recommended, Extensions.DiffPlexId, "every project");
            Note(related, "Terminal", "every project");
        }

        var suggestions = new List<Suggestion>();
        foreach (var definition in Extensions.All.Where(_ => state.Unavailable(_.Id) == null))
        {
            if (recommended.TryGetValue(definition.Id, out var because))
            {
                suggestions.Add(new(definition.Id, true, because));
                continue;
            }

            if (related.TryGetValue(definition.Id, out because))
            {
                suggestions.Add(new(definition.Id, false, because));
            }
        }

        return suggestions;
    }

    /// <summary>
    /// Chooses or unchooses a tech, and updates the selection to match: choosing one selects what it
    /// recommends, unchoosing it deselects what only it recommended. A recommendation is skipped when
    /// the project already has it, or when selecting it would conflict with something already there,
    /// because two techs can recommend alternatives (Excel suggests ClosedXml, Word suggests OpenXml,
    /// and both convert xlsx).
    /// </summary>
    public static void Choose(WizardState state, string techId, bool chosen)
    {
        if (!Techs.ById.TryGetValue(techId, out var tech))
        {
            return;
        }

        var techs = new HashSet<string>(state.Techs, StringComparer.Ordinal);
        if (chosen)
        {
            techs.Add(techId);
            state.Techs = techs;
            foreach (var id in tech.Recommended.Where(_ => state.Unavailable(_) == null))
            {
                if (state.IsExisting(id) ||
                    WouldConflict(state, id))
                {
                    continue;
                }

                state.Select(id, true);
            }

            return;
        }

        techs.Remove(techId);
        state.Techs = techs;
        var stillRecommended = Techs.All
            .Where(_ => techs.Contains(_.Id))
            .SelectMany(_ => _.Recommended)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var id in tech.Recommended.Where(_ => !stillRecommended.Contains(_)))
        {
            state.Select(id, false);
        }
    }

    /// <summary>Applies every chosen tech's recommendations, as if each had just been chosen.</summary>
    public static void ApplyAll(WizardState state)
    {
        foreach (var id in state.Techs.ToList())
        {
            Choose(state, id, true);
        }
    }

    static bool WouldConflict(WizardState state, string extensionId) =>
        InteractionRules.Groups
            .Where(_ => _.Members.Contains(extensionId) && _.Holds(extensionId, state.Choices))
            .Any(group => group.Members.Any(
                member => member != extensionId &&
                          state.Uses(member) &&
                          group.Holds(member, state.Choices)));
}
