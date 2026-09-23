namespace Wizard.Core;

/// <summary>
/// The plugin registry (plan 9). The data is split across partial files by catalogue letter, and
/// <see cref="All"/> is the single ordered list everything else reads: urls, the plugin step, the
/// guide and the generated code all use this order, so output never depends on insertion order.
/// </summary>
public static partial class Plugins
{
    public static IReadOnlyList<PluginDefinition> All { get; } =
        [.. Build().OrderBy(_ => _.Id, StringComparer.Ordinal)];

    // One partial file per catalogue file, so an entry sits next to the research it came from.
    static IEnumerable<PluginDefinition> Build() =>
        [.. CatalogueA, .. CatalogueB, .. CatalogueC, .. CatalogueD];

    public static IReadOnlyDictionary<string, PluginDefinition> ById { get; } =
        All.ToDictionary(_ => _.Id, StringComparer.Ordinal);

    public static bool Contains(string id) =>
        ById.ContainsKey(id);

    /// <summary>
    /// Always pre-checked: an inline diff on a failed text snapshot is useful in every project, and the
    /// old wizard's pages recommended it unconditionally.
    /// </summary>
    public const string DiffPlexId = "DiffPlex";

    /// <summary>The selected plugins in registry order, ignoring ids the registry does not have.</summary>
    public static IReadOnlyList<PluginDefinition> Selected(WizardState state) =>
        [.. All.Where(_ => state.Has(_.Id))];

    public static IEnumerable<PluginCategory> Categories =>
        Enum.GetValues<PluginCategory>();

    public static string Label(this PluginCategory category) =>
        category switch
        {
            PluginCategory.DeveloperExperience => "Developer experience",
            PluginCategory.Data => "Data",
            PluginCategory.Web => "Web",
            PluginCategory.Ui => "Desktop UI",
            PluginCategory.Documents => "Documents",
            PluginCategory.Images => "Images",
            PluginCategory.Messaging => "Messaging",
            PluginCategory.Logging => "Logging",
            PluginCategory.Observability => "Observability",
            PluginCategory.Serialization => "Serialization",
            PluginCategory.Email => "Email",
            PluginCategory.Mocking => "Mocking",
            PluginCategory.Compiler => "Compiler and code generation",
            PluginCategory.Scrubbing => "Scrubbing",
            PluginCategory.Testing => "Testing",
            PluginCategory.Tooling => "Tooling",
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
        };
}
