/// <summary>MSBuild-flavoured XML emitters (from SponsorCheck.Web).</summary>
public static class MsBuildXml
{
    public static string Escape(string value) =>
        value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");

    public static string PropertyGroup(IEnumerable<(string Name, string Value)> properties, string indent = "")
    {
        var builder = new StringBuilder();
        builder.Append(indent).Append("<PropertyGroup>\n");
        foreach (var (name, value) in properties)
        {
            builder.Append(indent).Append($"  <{name}>{Escape(value)}</{name}>\n");
        }

        builder.Append(indent).Append("</PropertyGroup>");
        return builder.ToString();
    }

    public static string Fenced(string content, string language = "xml") =>
        $"```{language}\n{content}\n```";
}
