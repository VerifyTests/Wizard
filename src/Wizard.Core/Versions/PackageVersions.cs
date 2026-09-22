namespace Wizard.Core;

/// <summary>
/// The package versions emitted into generated output (plan D3). Baked from package-versions.json;
/// the web app can overlay live nuget.org lookups with <see cref="With"/> (plan 15.3).
/// </summary>
public sealed class PackageVersions
{
    readonly IReadOnlyDictionary<string, string> versions;

    PackageVersions(Date updated, IReadOnlyDictionary<string, string> versions)
    {
        Updated = updated;
        this.versions = versions;
    }

    /// <summary>When the baked versions were last refreshed.</summary>
    public Date Updated { get; }

    public IEnumerable<string> Ids => versions.Keys;

    public static PackageVersions Baked { get; } = Load();

    public string this[string packageId]
    {
        get
        {
            if (versions.TryGetValue(packageId, out var version))
            {
                return version;
            }

            throw new($"package-versions.json has no version for '{packageId}'.");
        }
    }

    /// <summary>A copy with some versions replaced, e.g. by newer ones found on nuget.org.</summary>
    public PackageVersions With(IReadOnlyDictionary<string, string> overrides)
    {
        var merged = new Dictionary<string, string>(versions, StringComparer.OrdinalIgnoreCase);
        foreach (var (id, version) in overrides)
        {
            merged[id] = version;
        }

        return new(Updated, merged);
    }

    static PackageVersions Load()
    {
        using var stream = typeof(PackageVersions).Assembly.GetManifestResourceStream("package-versions.json")!;
        var file = JsonSerializer.Deserialize(stream, PackageVersionsJsonContext.Default.PackageVersionsFile)!;
        return new(
            Date.ParseExact(file.Updated, "yyyy-MM-dd", CultureInfo.InvariantCulture),
            new Dictionary<string, string>(file.Packages, StringComparer.OrdinalIgnoreCase));
    }
}

public sealed record PackageVersionsFile(
    string Updated,
    Dictionary<string, string> Packages,
    Dictionary<string, string>? Pinned);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PackageVersionsFile))]
partial class PackageVersionsJsonContext : JsonSerializerContext;
