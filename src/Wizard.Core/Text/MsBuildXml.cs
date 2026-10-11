/// <summary>MSBuild-flavoured XML emitters (from SponsorCheck.Web).</summary>
public static class MsBuildXml
{
    public static string Escape(string value) =>
        value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");

    public static string PropertyGroup(IEnumerable<(string Name, string Value)> properties, string indent = "") =>
        Joined.Lines(
        [
            $"{indent}<PropertyGroup>",
            .. properties.Select(_ => $"{indent}  <{_.Name}>{Escape(_.Value)}</{_.Name}>"),
            $"{indent}</PropertyGroup>"
        ]);

    public static string Fenced(string content, string language = "xml") =>
        $"```{language}\n{content}\n```";
}
