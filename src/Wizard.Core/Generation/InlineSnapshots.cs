/// <summary>
/// Opt-in inline snapshots (plan 12.8). The module initializer turns them on for every verification,
/// and a snapshot the wizard knows is written into the test as a <c>.Snapshot(...)</c> literal rather
/// than shipped as a <c>.verified.</c> file, which the global switch would otherwise report as stale.
/// </summary>
public static class InlineSnapshots
{
    public const string DocsUrl = "https://github.com/VerifyTests/Verify/blob/main/docs/inline-snapshots.md";

    public static InitializeBlock Block { get; } = new(
        "Inline",
        InitializePhase.Settings,
        [
            new(
                "VerifierSettings.Inline();",
                "Keeps each text snapshot in the test source, as a .Snapshot(...) literal, instead of a .verified. file.",
                "A verification that cannot be inlined, such as an image or a parameterised test, still uses a file.")
            {
                Alternatives =
                [
                    "VerifierSettings.Inline(maxLines: 30);"
                ]
            }
        ]);

    /// <summary>
    /// Chains <c>.Snapshot(...)</c> holding <paramref name="snapshot"/> onto the verification in
    /// <paramref name="code"/>: the statement that ends at or after the last line calling Verify. A C#
    /// statement ends with <c>;</c>, and an F# one with the <c>.ToTask()</c> its chain has to end with.
    /// </summary>
    public static string AddSnapshot(string code, string snapshot)
    {
        var lines = ModuleInitializerGenerator.Lines(code).ToList();
        var verify = lines.FindLastIndex(_ => _.Contains("Verify(", StringComparison.Ordinal));
        var end = lines.FindIndex(verify, _ => _.EndsWith(';') || _.EndsWith(".ToTask()", StringComparison.Ordinal));
        var line = lines[end];

        var suffix = ";";
        // F# needs the chain indented past the start of the expression, which follows "do! ".
        var step = 4;
        if (line.EndsWith(".ToTask()", StringComparison.Ordinal))
        {
            suffix = ".ToTask()";
            step = 8;
        }

        var trimmed = line.TrimStart();
        var indentation = line.Length - trimmed.Length;
        if (!trimmed.StartsWith('.'))
        {
            indentation += step;
        }

        var chain = new string(' ', indentation);
        var literal = chain + "    ";
        var snapshotLines = ModuleInitializerGenerator.Lines(snapshot)
            .Select(_ => _.Length == 0 ? _ : literal + _);

        lines[end] = line[..^suffix.Length];
        lines.InsertRange(
            end + 1,
            [
                $"{chain}.Snapshot(",
                $"{literal}\"\"\"",
                .. snapshotLines,
                $"{literal}\"\"\"){suffix}"
            ]);
        return string.Join('\n', lines);
    }
}
