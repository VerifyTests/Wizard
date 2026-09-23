namespace Wizard.Core;

/// <summary>One selected extension, with everything resolved that depends on the rest of the state.</summary>
/// <param name="Statements">Its module initializer statements, after any rule replaced them.</param>
public sealed record ResolvedExtension(
    ExtensionDefinition Definition,
    Depth Depth,
    IReadOnlyList<PackageRequirement> Packages,
    IReadOnlyList<Sample> Samples,
    IReadOnlyList<InitializeStatement> Statements)
{
    public string Id => Definition.Id;

    /// <summary>Windows-only extensions go in a second test project, so the main one stays portable (plan D5).</summary>
    public bool IsWindowsOnly => Definition.Platform == Platform.WindowsOnly;

    /// <summary>The test class holding this extension's samples.</summary>
    public string TestClass => $"{Id}Tests";
}
