namespace Wizard.Core;

/// <summary>
/// Text copied from Verify's docs (plan D15, Appendix A). The files are kept verbatim so they can be
/// diffed against the originals; the differences that matter outside the Verify repo are applied at
/// read time: <c>snippet:</c> lines are resolved and root-relative links are made absolute.
/// </summary>
public static class ContentFiles
{
    const string docsBlob = "https://github.com/VerifyTests/Verify/blob/main";
    const string docsRaw = "https://raw.githubusercontent.com/VerifyTests/Verify/main";

    static Regex snippetLine = new(@"^snippet: (?<name>\S+)\s*$", RegexOptions.Multiline);

    public static string Raw(string fileName)
    {
        using var stream = typeof(ContentFiles).Assembly.GetManifestResourceStream($"Content.{fileName}") ??
                           throw new($"No embedded content named '{fileName}'.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Replace("\r\n", "\n");
    }

    /// <summary>An <c>*.include.md</c> file, ready to embed in generated markdown.</summary>
    public static string Include(string name, IReadOnlyDictionary<string, string>? snippets = null)
    {
        var text = Raw($"{name}.include.md");
        text = snippetLine.Replace(
            text,
            match =>
            {
                var snippetName = match.Groups["name"].Value;
                if (snippets != null &&
                    snippets.TryGetValue(snippetName, out var code))
                {
                    return code;
                }

                throw new($"Content '{name}' references snippet '{snippetName}', which was not supplied.");
            });
        return MakeLinksAbsolute(text).TrimEnd();
    }

    /// <summary>Root-relative links and images point into the Verify repository.</summary>
    public static string MakeLinksAbsolute(string markdown)
    {
        markdown = markdown.Replace("src=\"/", $"src=\"{docsRaw}/");
        return Regex.Replace(markdown, @"\]\(/(?!/)", $"]({docsBlob}/");
    }
}
