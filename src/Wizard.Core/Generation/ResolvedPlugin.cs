/// <summary>One selected plugin, with everything resolved that depends on the rest of the state.</summary>
/// <param name="Statements">Its module initializer statements, after any rule replaced them.</param>
public sealed record ResolvedPlugin(
    PluginDefinition Definition,
    Depth Depth,
    IReadOnlyList<PackageRequirement> Packages,
    IReadOnlyList<Sample> Samples,
    IReadOnlyList<InitializeStatement> Statements)
{
    /// <summary>
    /// Already in the project (plan 7.2). Nothing is generated for it except a change to its existing
    /// initialization, when a rule the new selection triggers requires one.
    /// </summary>
    public bool Existing { get; init; }

    public string Id => Definition.Id;

    /// <summary>Windows-only plugins go in a second test project, so the main one stays portable (plan D5).</summary>
    public bool IsWindowsOnly => Definition.Platform == Platform.WindowsOnly;

    /// <summary>The test class holding this plugin's samples.</summary>
    public string TestClass => $"{Id}Tests";
}
