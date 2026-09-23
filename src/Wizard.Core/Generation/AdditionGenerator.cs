namespace Wizard.Core;

/// <summary>
/// The files of the add flows (plan 12.5): not a solution, but the changes to merge into one, rooted in a
/// folder named <c>verify-additions</c>. Every fragment says where it goes; the readme and CLAUDE.md
/// say in what order.
/// </summary>
public static class AdditionGenerator
{
    public const string PackagesFragment = "Directory.Packages.props.fragment.xml";
    public const string ProjectFragment = "TestProject.csproj.fragment.xml";
    public const string SponsorFragment = "Directory.Build.props.fragment.xml";
    public const string ToolsFragment = ".config/dotnet-tools.json.fragment.json";
    public const string SamplesFolder = "src-samples";

    public static IReadOnlyList<GeneratedFile> Build(Plan plan)
    {
        var files = new List<GeneratedFile>
        {
            new("readme.md", DocsGenerator.Build(plan)),
            new("CLAUDE.md", AiContentGenerator.Build(plan)),
            new(PackagesFragment, Packages(plan)),
            new(ProjectFragment, Project(plan))
        };

        if (plan.Framework.IsFSharp)
        {
            files.Add(new("initialize.fragment.fs", FSharpInitialize(plan)));
        }
        else
        {
            files.Add(new("ModuleInitializer.cs", ModuleInitializerGenerator.Build(plan)));
        }

        if (Sponsor(plan) is { } sponsor)
        {
            files.Add(new(SponsorFragment, sponsor));
        }

        if (plan.DotnetTools.Count > 0)
        {
            files.Add(new(ToolsFragment, Tools(plan)));
        }

        foreach (var test in PluginTestFiles.For(plan, windows: false))
        {
            files.Add(test);
        }

        foreach (var source in plan.Plugins.SelectMany(_ => _.Definition.LibraryFiles).DistinctBy(_ => _.Path))
        {
            files.Add(new($"{SamplesFolder}/{source.Path}", CodeFiles.Banner(plan) + source.Content.TrimEnd('\n') + "\n"));
        }

        return files;
    }

    /// <summary>The <c>PackageVersion</c> lines, for a solution using Central Package Management.</summary>
    public static string Packages(Plan plan)
    {
        var builder = new StringBuilder(
            """
            <!-- Merge into the ItemGroup of PackageVersion items in Directory.Packages.props.
                 Without Central Package Management, put each Version on the PackageReference in
                 TestProject.csproj.fragment.xml instead, and skip this file. -->
            <ItemGroup>

            """);
        foreach (var package in plan.AddedPackages)
        {
            builder.Append($"  <PackageVersion Include=\"{package}\" Version=\"{plan.Version(package)}\" />\n");
        }

        builder.Append("</ItemGroup>\n");
        return builder.ToString();
    }

    /// <summary>What the test project gains: package references, and any properties and items the plugins need.</summary>
    public static string Project(Plan plan)
    {
        var builder = new StringBuilder("<!-- Merge into the test project. -->\n");
        var properties = plan.Plugins
            .SelectMany(_ => _.Definition.ProjectProperties)
            .Distinct()
            .ToList();
        var windowsOnly = plan.Plugins.Where(_ => _.IsWindowsOnly).Select(_ => _.Id).ToList();
        if (properties.Count > 0 ||
            windowsOnly.Count > 0)
        {
            builder.Append("<PropertyGroup>\n");
            if (windowsOnly.Count > 0)
            {
                builder.Append($"  <!-- {InteractionRules.Join(windowsOnly)} only runs on Windows: the test project has to\n");
                builder.Append($"       target {WizardDefaults.TargetFramework}-windows, or these tests go in a separate project that does. -->\n");
            }

            foreach (var (name, value) in properties)
            {
                builder.Append($"  <{name}>{value}</{name}>\n");
            }

            builder.Append("</PropertyGroup>\n");
        }

        builder.Append("<ItemGroup>\n");
        foreach (var package in plan.AddedPackages)
        {
            builder.Append($"  <PackageReference Include=\"{package}\" />\n");
        }

        foreach (var item in plan.Plugins.SelectMany(_ => _.Definition.ProjectItems).Distinct(StringComparer.Ordinal))
        {
            builder.Append($"  {item}\n");
        }

        builder.Append("</ItemGroup>\n");
        return builder.ToString();
    }

    /// <summary>
    /// Only when there is something to declare: a mode chosen in the wizard, or another owner's fee check
    /// that the added packages bring in (plan A8). An existing project already declares its Verify status.
    /// </summary>
    public static string? Sponsor(Plan plan)
    {
        var owners = plan.SponsorOwners;
        if (plan.State.SponsorMode == SponsorMode.NotChosen)
        {
            if (owners.Count == 0)
            {
                return null;
            }

            return "<!-- Merge into Directory.Build.props. -->\n" +
                   SponsorXml.OwnersOnly(plan.State, "", owners);
        }

        return "<!-- Merge into Directory.Build.props, replacing any existing Verify_ declaration. -->\n" +
               SponsorXml.Block(plan.State, "", owners);
    }

    static string Tools(Plan plan)
    {
        var builder = new StringBuilder("{\n  \"tools\": {\n");
        var tools = plan.DotnetTools;
        for (var index = 0; index < tools.Count; index++)
        {
            var id = tools[index];
            var separator = index < tools.Count - 1 ? "," : "";
            builder.Append(
                $$"""
                    "{{id}}": {
                      "version": "{{plan.Version(id)}}",
                      "commands": [
                        "dotnet-verify"
                      ],
                      "rollForward": false
                    }{{separator}}

                """);
        }

        builder.Append("  }\n}\n");
        return builder.ToString();
    }

    /// <summary>
    /// Expecto has no module initializer; the calls go in the lazy value the project's tests force, in
    /// this order (plan D9).
    /// </summary>
    static string FSharpInitialize(Plan plan)
    {
        var builder = new StringBuilder(CodeFiles.Banner(plan));
        builder.Append("// Merge into the lazy initialization value the tests force before verifying.\n");
        foreach (var open in CodeFiles.ExpectoOpens(plan))
        {
            builder.Append($"{open}\n");
        }

        builder.Append('\n');
        builder.Append(CodeFiles.ExpectoInitialize(plan).Replace("\n        ", "\n").TrimStart());
        builder.Append('\n');
        return builder.ToString();
    }
}
