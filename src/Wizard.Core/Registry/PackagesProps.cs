using System.Xml;
using System.Xml.Linq;

/// <summary>
/// Finds the plugins a solution already references, from its <c>Directory.Packages.props</c>, so the
/// "already using" step can be filled in from the file (plan 7.2 step 2).
/// </summary>
public static class PackagesProps
{
    public const string FileName = "Directory.Packages.props";

    static readonly string[] itemNames = ["PackageVersion", "GlobalPackageReference"];

    static readonly IReadOnlyDictionary<string, string> pluginByPackage = BuildPluginByPackage();

    /// <summary>
    /// A plugin is recognised by its first package. The packages after it are what its samples use,
    /// and a solution can reference NodaTime without Verify.NodaTime. LocalDb is the exception: its
    /// integration ships as a package per test framework, so each of those identifies it, other than
    /// the ones another plugin leads with.
    /// </summary>
    static Dictionary<string, string> BuildPluginByPackage()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in Plugins.All)
        {
            map[definition.Packages[0].Id] = definition.Id;
        }

        foreach (var definition in Plugins.All.Where(_ => _.Packages[0].TestFrameworks.Count > 0))
        {
            foreach (var package in definition.Packages.Where(_ => _.TestFrameworks.Count > 0))
            {
                map.TryAdd(package.Id, definition.Id);
            }
        }

        return map;
    }

    /// <summary>The ids of the plugins the file references, in registry order. False when it is not xml.</summary>
    public static bool TryReadPlugins(string xml, out IReadOnlyList<string> plugins)
    {
        XDocument document;
        try
        {
            document = XDocument.Parse(xml);
        }
        catch (XmlException)
        {
            plugins = [];
            return false;
        }

        var found = document
            .Descendants()
            .Where(_ => itemNames.Contains(_.Name.LocalName))
            .SelectMany(PackageIds)
            .Select(_ => pluginByPackage.GetValueOrDefault(_))
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        plugins = [.. Plugins.All.Select(_ => _.Id).Where(found.Contains)];
        return true;
    }

    static IEnumerable<string> PackageIds(XElement item)
    {
        var value = item.Attribute("Include")?.Value ?? item.Attribute("Update")?.Value ?? "";
        return value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
