namespace Wizard.Core;

/// <summary>
/// The extension registry (plan 9). The data is split across partial files by catalogue letter, and
/// <see cref="All"/> is the single ordered list everything else reads: urls, the extension step, the
/// guide and the generated code all use this order, so output never depends on insertion order.
/// </summary>
public static partial class Extensions
{
    public static IReadOnlyList<ExtensionDefinition> All { get; } =
        [.. Build().OrderBy(_ => _.Id, StringComparer.Ordinal)];

    // One partial file per catalogue file, so an entry sits next to the research it came from.
    static IEnumerable<ExtensionDefinition> Build() =>
        [.. CatalogueA, .. CatalogueB, .. CatalogueC, .. CatalogueD];

    public static IReadOnlyDictionary<string, ExtensionDefinition> ById { get; } =
        All.ToDictionary(_ => _.Id, StringComparer.Ordinal);

    public static bool Contains(string id) =>
        ById.ContainsKey(id);

    /// <summary>
    /// Always pre-checked: an inline diff on a failed text snapshot is useful in every project, and the
    /// old wizard's pages recommended it unconditionally.
    /// </summary>
    public const string DiffPlexId = "DiffPlex";

    /// <summary>The selected extensions in registry order, ignoring ids the registry does not have.</summary>
    public static IReadOnlyList<ExtensionDefinition> Selected(WizardState state) =>
        [.. All.Where(_ => state.Has(_.Id))];

    public static IEnumerable<ExtensionCategory> Categories =>
        Enum.GetValues<ExtensionCategory>();

    public static string Label(this ExtensionCategory category) =>
        category switch
        {
            ExtensionCategory.DeveloperExperience => "Developer experience",
            ExtensionCategory.Data => "Data",
            ExtensionCategory.Web => "Web",
            ExtensionCategory.Ui => "Desktop UI",
            ExtensionCategory.Documents => "Documents",
            ExtensionCategory.Images => "Images",
            ExtensionCategory.Messaging => "Messaging",
            ExtensionCategory.Logging => "Logging",
            ExtensionCategory.Observability => "Observability",
            ExtensionCategory.Serialization => "Serialization",
            ExtensionCategory.Email => "Email",
            ExtensionCategory.Mocking => "Mocking",
            ExtensionCategory.Compiler => "Compiler and code generation",
            ExtensionCategory.Scrubbing => "Scrubbing",
            ExtensionCategory.Testing => "Testing",
            ExtensionCategory.Tooling => "Tooling",
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
        };
}
