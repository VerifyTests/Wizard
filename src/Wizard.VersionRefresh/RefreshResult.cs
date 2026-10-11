/// <param name="Changes">Package id, old version, new version, in file order.</param>
/// <param name="Unanswered">Ids nuget.org gave no answer for, left as they were.</param>
public sealed record RefreshResult(
    string Json,
    IReadOnlyList<(string Id, string From, string To)> Changes,
    IReadOnlyList<string> Unanswered)
{
    public bool Changed => Changes.Count > 0;

    /// <summary>The pull request body: what moved, and what could not be checked.</summary>
    public string Summary()
    {
        var lines = new List<string>();
        if (Changed)
        {
            lines.Add(
                """
                Newest stable versions on nuget.org (plan 15.2).

                | Package | From | To |
                |---|---|---|
                """);
            lines.AddRange(Changes.Select(_ => $"| {_.Id} | {_.From} | {_.To} |"));
        }
        else
        {
            lines.Add("Every package is already at its newest stable version.");
        }

        if (Unanswered.Count > 0)
        {
            lines.Add("");
            lines.Add($"Not checked, because nuget.org did not answer: {string.Join(", ", Unanswered)}.");
        }

        return Joined.File(lines);
    }
}
