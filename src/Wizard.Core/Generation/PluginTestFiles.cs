/// <summary>
/// One test file per selected plugin (plan 12.2), holding its samples at the chosen depth. Every
/// method has a comment block above it saying what it shows, so the file is readable on its own.
/// Usings go in the file rather than in global usings: two plugins can define the same type name,
/// and importing both namespaces would not compile (plan A7).
/// </summary>
public static class PluginTestFiles
{
    /// <remarks>
    /// No plugin sample ships a verified file (plan D6): what a plugin writes depends on its version
    /// and the machine, so each writes a received file on its first run instead.
    /// </remarks>
    public static IEnumerable<GeneratedFile> For(Plan plan, bool windows) =>
        plan.PluginsIn(windows)
            .Where(_ => _.Samples.Count > 0)
            .Select(_ => new GeneratedFile($"Plugins/{_.TestClass}.cs", Build(plan, _)));

    static string Build(Plan plan, ResolvedPlugin plugin)
    {
        var framework = plan.Framework;
        var definition = plugin.Definition;
        var lines = new List<string>();
        lines.AddRange(definition.Usings.Select(_ => $"using {_};"));
        if (definition.Usings.Count > 0)
        {
            lines.Add("");
        }

        lines.Add($"// {definition.DisplayName}: {definition.Description}");
        lines.Add($"// {definition.RepoUrl}");
        lines.AddRange(definition.Notes.SelectMany(_ => Wrap($"Note: {Plain(_)}")).Select(_ => $"// {_}"));
        lines.AddRange(framework.ClassAttributes);

        var partial = framework.PartialClasses ? "partial " : "";
        lines.Add($"public {partial}class {plugin.TestClass}");
        lines.Add("{");

        var first = true;
        foreach (var sample in plugin.Samples)
        {
            if (!first)
            {
                lines.Add("");
            }

            first = false;
            lines.AddRange(SampleLines(framework, sample));
        }

        foreach (var member in plugin.Samples.SelectMany(_ => _.Members))
        {
            lines.Add("");
            lines.AddRange(ModuleInitializerGenerator.Indented(member, "    "));
        }

        lines.Add("}");
        return CodeFiles.Banner(plan) + Joined.File(lines);
    }

    static List<string> SampleLines(TestFrameworkInfo framework, Sample sample)
    {
        var lines = new List<string>();
        lines.AddRange(sample.Comment.Select(_ => $"    // {_}"));
        if (sample.SkipReason is { } reason)
        {
            lines.AddRange(Wrap($"Not run by default: {reason}").Select(_ => $"    // {_}"));
        }

        var attribute = framework.TestAttribute;
        if (sample.SkipReason is { } skip &&
            framework.SkipAttribute is { } skipAttribute)
        {
            // The reason goes inside a C# string literal in the attribute.
            attribute = skipAttribute(skip.Replace("\\", "\\\\").Replace("\"", "\\\""));
        }

        if (attribute.Length > 0)
        {
            lines.Add($"    {attribute}");
        }

        // Fixie runs every public method of the class, so a skipped sample is made private instead.
        var visibility = "public";
        if (sample.SkipReason != null &&
            framework.SkipAttribute == null)
        {
            visibility = "static";
        }

        var signature = sample.Async ? "async Task" : "Task";
        lines.Add($"    {visibility} {signature} {sample.Name}()");
        lines.Add("    {");
        lines.AddRange(ModuleInitializerGenerator.Indented(sample.Body, "        "));
        lines.Add("    }");
        return lines;
    }

    /// <summary>Notes are written as markdown for the guide; a code comment wants them plain.</summary>
    internal static string Plain(string markdown) =>
        markdown.Replace("`", "");

    /// <summary>Wraps a comment at a width that leaves room for the indent and the slashes.</summary>
    internal static IEnumerable<string> Wrap(string text, int width = 96)
    {
        var line = new StringBuilder();
        foreach (var word in text.Split(' '))
        {
            if (line.Length > 0 &&
                line.Length + 1 + word.Length > width)
            {
                yield return line.ToString();
                line.Clear();
            }

            if (line.Length > 0)
            {
                line.Append(' ');
            }

            line.Append(word);
        }

        if (line.Length > 0)
        {
            yield return line.ToString();
        }
    }
}
