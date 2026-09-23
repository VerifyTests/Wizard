/// <summary>One block of the module initializer: a plugin's statements, or a rule's addition.</summary>
/// <param name="Key">The plugin id the block belongs to, which is what ordering edges name.</param>
public sealed record InitializeBlock(
    string Key,
    InitializePhase Phase,
    IReadOnlyList<InitializeStatement> Statements)
{
    public IReadOnlyList<string> Usings { get; init; } = [];
    public IReadOnlyList<string> Members { get; init; } = [];

    /// <summary>Blocks added by a rule sort after the plugin they are keyed to.</summary>
    public bool FromRule { get; init; }
}

/// <summary>
/// Orders the module initializer's blocks (plan 11.3): by phase, then by the edges the rules impose,
/// then alphabetically. Within a phase the edges are a topological sort; a cycle is a registry bug, so
/// it throws rather than emitting code whose behaviour depends on which edge was dropped.
/// </summary>
public static class InitializeOrder
{
    public static IReadOnlyList<InitializeBlock> Sort(
        IReadOnlyList<InitializeBlock> blocks,
        IReadOnlyList<OrderEdge> edges)
    {
        var sorted = new List<InitializeBlock>();
        foreach (var phase in Enum.GetValues<InitializePhase>())
        {
            sorted.AddRange(SortPhase([.. blocks.Where(_ => _.Phase == phase)], edges));
        }

        return sorted;
    }

    static IEnumerable<InitializeBlock> SortPhase(
        IReadOnlyList<InitializeBlock> blocks,
        IReadOnlyList<OrderEdge> edges)
    {
        if (blocks.Count < 2)
        {
            return blocks;
        }

        // Alphabetical first, so the result is stable whenever the edges leave a choice open.
        var remaining = blocks
            .OrderBy(_ => _.Key, StringComparer.Ordinal)
            .ThenBy(_ => _.FromRule)
            .ToList();
        var within = edges
            .Where(_ => remaining.Any(block => block.Key == _.Before) && remaining.Any(block => block.Key == _.After))
            .ToList();

        var result = new List<InitializeBlock>();
        while (remaining.Count > 0)
        {
            var next = remaining.FirstOrDefault(
                block => !within.Any(
                    edge =>
                        edge.After == block.Key &&
                        edge.Before != block.Key &&
                        remaining.Any(other => other.Key == edge.Before)));
            if (next == null)
            {
                var cycle = string.Join(", ", remaining.Select(_ => _.Key).Distinct(StringComparer.Ordinal));
                throw new($"The interaction rules order these plugins in a cycle: {cycle}.");
            }

            result.Add(next);
            remaining.Remove(next);
        }

        return result;
    }
}
