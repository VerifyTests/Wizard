/// <summary>
/// The generated test project's <c>ModuleInitializer.cs</c> (plan 12.4). Every call carries a comment
/// saying what it does and why it is where it is, so the file explains itself once the wizard is closed.
/// Explicit calls come before <c>InitializePlugins()</c>: a plugin can only be initialized once, so a
/// call with parameters has to win the race against discovery.
/// </summary>
public static class ModuleInitializerGenerator
{
    public static string Build(Plan plan) =>
        CodeFiles.Banner(plan) + Body(plan, windows: false);

    /// <summary>The second test project's copy, holding only the Windows-only plugins (plan D5).</summary>
    public static string BuildWindows(Plan plan) =>
        CodeFiles.Banner(plan) + Body(plan, windows: true);

    static string Body(Plan plan, bool windows)
    {
        var blocks = Blocks(plan, windows);
        var builder = new StringBuilder();

        builder.Append("using System.Runtime.CompilerServices;\n");
        foreach (var name in blocks.SelectMany(_ => _.Usings).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            builder.Append($"using {name};\n");
        }

        builder.Append(
            """

            // Everything here runs once, when the test assembly loads, before any test.
            public static class ModuleInitializer
            {
                [ModuleInitializer]
                public static void Initialize()
                {

            """);

        foreach (var line in Statements(plan, windows))
        {
            if (line.Length == 0)
            {
                builder.Append('\n');
                continue;
            }

            builder.Append($"        {line}\n");
        }

        builder.Append("    }\n");

        foreach (var member in blocks.SelectMany(_ => _.Members))
        {
            builder.Append('\n');
            builder.Append(Indent(member, "    "));
        }

        builder.Append("}\n");
        return builder.ToString();
    }

    /// <summary>
    /// The body of Initialize, one line per entry and an empty string between blocks, without
    /// indentation. Shared with the F# initializer, which needs the same calls in the same order.
    /// </summary>
    /// <param name="skipBlocksNeedingMembers">
    /// Set for F#, where the C# helper methods a few plugins need are not generated (plan D9).
    /// </param>
    public static IEnumerable<string> Statements(Plan plan, bool windows, bool skipBlocksNeedingMembers = false)
    {
        var blocks = Blocks(plan, windows);

        // Alternatives are only worth showing where that plugin's samples are verbose too.
        var verbose = blocks.Any(_ => _ != InlineSnapshots.Block && plan.State.DepthOf(_.Key) == Depth.Verbose);

        var discovery = new InitializeBlock(
            "",
            InitializePhase.Plugins,
            [
                new(
                    "VerifierSettings.InitializePlugins();",
                    "Initializes every Verify.* plugin the project references and has not initialized above.",
                    "Explicit calls come first so their parameters apply: a plugin can only be initialized once.")
            ]);

        var before = blocks.Where(_ => _.Phase != InitializePhase.AfterDiscovery);
        var after = blocks.Where(_ => _.Phase == InitializePhase.AfterDiscovery);
        var first = true;
        foreach (var block in before.Append(discovery).Concat(after))
        {
            if (!first)
            {
                yield return "";
            }

            first = false;
            if (skipBlocksNeedingMembers &&
                block.Members.Count > 0)
            {
                yield return $"// {block.Key} needs a helper method the F# initializer does not generate;";
                yield return "// plugin discovery below still enables it with its default settings.";
                continue;
            }

            foreach (var statement in block.Statements)
            {
                foreach (var comment in statement.Comment)
                {
                    yield return $"// {comment}";
                }

                yield return statement.Code;
                if (!verbose)
                {
                    continue;
                }

                foreach (var alternative in statement.Alternatives)
                {
                    yield return $"// {alternative}";
                }
            }
        }
    }

    /// <summary>
    /// One block per plugin, plus the blocks the rules add, sorted into the order the calls have to
    /// run in. The Windows project holds the Windows-only plugins and the main one holds the rest.
    /// </summary>
    public static IReadOnlyList<InitializeBlock> Blocks(Plan plan, bool windows)
    {
        var state = plan.State;
        var blocks = new List<InitializeBlock>();
        if (plan.Inline)
        {
            blocks.Add(InlineSnapshots.Block);
        }

        foreach (var plugin in plan.PluginsIn(windows))
        {
            if (plugin.Statements.Count == 0)
            {
                continue;
            }

            blocks.Add(
                new(plugin.Id, plugin.Definition.Phase, plugin.Statements)
                {
                    Usings = plugin.Definition.InitializeUsings,
                    Members = plugin.Definition.InitializeMembers
                });
        }

        var keys = blocks.Select(_ => _.Key).ToHashSet(StringComparer.Ordinal);

        // A call the project already makes, which a rule the new selection triggers changes. The
        // project's own initializer has it; the comment says what to do with it there.
        if (!windows)
        {
            foreach (var existing in plan.ExistingChanges)
            {
                keys.Add(existing.Id);
                if (existing.Statements.Count == 0)
                {
                    continue;
                }

                var statements = existing.Statements.ToList();
                statements[0] = statements[0] with
                {
                    Comment =
                    [
                        $"{existing.Definition.DisplayName} is already in the project, and the plugins being added",
                        "change how it has to be initialized. Replace its existing call with this one, or, where the",
                        "project relies on InitializePlugins() to enable it, add this call above that.",
                        .. statements[0].Comment
                    ]
                };
                blocks.Add(
                    new(existing.Id, existing.Definition.Phase, statements)
                    {
                        Usings = existing.Definition.InitializeUsings,
                        Members = existing.Definition.InitializeMembers
                    });
            }
        }

        foreach (var addition in InteractionRules.Additions(state).Where(_ => keys.Contains(_.Target)))
        {
            blocks.Add(
                new(addition.Target, addition.Phase, addition.Statements)
                {
                    Usings = addition.Usings,
                    FromRule = true
                });
        }

        return InitializeOrder.Sort(blocks, [.. InteractionRules.Edges(state)]);
    }

    /// <summary>
    /// Indents a block of source. The carriage return is dropped rather than carried into the output:
    /// the registry's code is written in string literals, and whether the file holding them uses crlf
    /// is not something the generated solution should inherit.
    /// </summary>
    internal static string Indent(string code, string indent) =>
        string.Join('\n', Lines(code).Select(_ => _.Length == 0 ? _ : indent + _)) + "\n";

    internal static IEnumerable<string> Lines(string text) =>
        text.Split('\n').Select(_ => _.TrimEnd('\r'));
}
