/// <summary>
/// One test file per selected plugin (plan 12.2), holding its samples at the chosen depth. Every
/// method has a comment block above it saying what it shows, so the file is readable on its own.
/// Usings go in the file rather than in global usings: two plugins can define the same type name,
/// and importing both namespaces would not compile (plan A7).
/// </summary>
public static class PluginTestFiles
{
    public static IEnumerable<GeneratedFile> For(Plan plan, bool windows)
    {
        foreach (var plugin in plan.PluginsIn(windows).Where(_ => _.Samples.Count > 0))
        {
            yield return new($"Plugins/{plugin.TestClass}.cs", Build(plan, plugin));

            // With inline snapshots a known snapshot is a literal in the sample instead.
            if (plan.Inline)
            {
                continue;
            }

            // A sample whose snapshot is known ships it, so it passes on the first run. Verified text
            // files are UTF-8 with a BOM and no trailing newline.
            foreach (var sample in plugin.Samples.Where(_ => _.VerifiedOutput != null))
            {
                yield return new(
                    $"Plugins/{plugin.TestClass}.{sample.Name}.verified.txt",
                    sample.VerifiedOutput!,
                    Bom: true);
            }
        }
    }

    static string Build(Plan plan, ResolvedPlugin plugin)
    {
        var framework = plan.Framework;
        var definition = plugin.Definition;
        var builder = new StringBuilder(CodeFiles.Banner(plan));

        foreach (var name in definition.Usings)
        {
            builder.Append($"using {name};\n");
        }

        if (definition.Usings.Count > 0)
        {
            builder.Append('\n');
        }

        builder.Append($"// {definition.DisplayName}: {definition.Description}\n");
        builder.Append($"// {definition.RepoUrl}\n");
        foreach (var note in definition.Notes)
        {
            foreach (var line in Wrap($"Note: {Plain(note)}"))
            {
                builder.Append($"// {line}\n");
            }
        }

        foreach (var attribute in framework.ClassAttributes)
        {
            builder.Append($"{attribute}\n");
        }

        var partial = framework.PartialClasses ? "partial " : "";
        builder.Append($"public {partial}class {plugin.TestClass}\n{{\n");

        var first = true;
        foreach (var sample in plugin.Samples)
        {
            if (!first)
            {
                builder.Append('\n');
            }

            first = false;
            AppendSample(builder, framework, sample, plan.Inline);
        }

        foreach (var member in plugin.Samples.SelectMany(_ => _.Members))
        {
            builder.Append('\n');
            builder.Append(ModuleInitializerGenerator.Indent(member, "    "));
        }

        builder.Append("}\n");
        return builder.ToString();
    }

    static void AppendSample(StringBuilder builder, TestFrameworkInfo framework, Sample sample, bool inline)
    {
        foreach (var line in sample.Comment)
        {
            builder.Append($"    // {line}\n");
        }

        if (sample.SkipReason is { } reason)
        {
            foreach (var line in Wrap($"Not run by default: {reason}"))
            {
                builder.Append($"    // {line}\n");
            }
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
            builder.Append($"    {attribute}\n");
        }

        // Fixie runs every public method of the class, so a skipped sample is made private instead.
        var visibility = "public";
        if (sample.SkipReason != null &&
            framework.SkipAttribute == null)
        {
            visibility = "static";
        }

        var signature = sample.Async ? "async Task" : "Task";
        builder.Append($"    {visibility} {signature} {sample.Name}()\n    {{\n");
        var body = sample.Body;
        if (inline &&
            sample.VerifiedOutput is { } verified)
        {
            body = InlineSnapshots.AddSnapshot(body, verified);
        }

        builder.Append(ModuleInitializerGenerator.Indent(body, "        "));
        builder.Append("    }\n");
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
