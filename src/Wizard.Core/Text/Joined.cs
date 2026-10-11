/// <summary>
/// Joins the lines of a generated file with lf. Builders collect lines and call this, rather than
/// appending strings that end in an escaped newline. An entry can itself hold several lines, such as
/// a raw string literal for a fixed block.
/// </summary>
public static class Joined
{
    public static string Lines(IEnumerable<string> lines) =>
        string.Join('\n', lines);

    /// <summary>The lines, and the newline a file ends with.</summary>
    public static string File(IEnumerable<string> lines) =>
        Lines(lines) + '\n';
}
