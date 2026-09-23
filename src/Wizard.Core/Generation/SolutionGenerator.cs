namespace Wizard.Core;

/// <summary>
/// The file tree of the download: a whole solution for the new-project flow (plan 12.2), or the
/// changes to merge into one for the add flows (plan 12.5).
/// </summary>
public static class SolutionGenerator
{
    public static IReadOnlyList<GeneratedFile> Build(Plan plan)
    {
        if (plan.IsAddition)
        {
            return AdditionGenerator.Build(plan);
        }

        var files = new List<GeneratedFile>();
        void Add(string path, string text, bool bom = false) =>
            files.Add(new(path, text, bom));

        var guide = DocsGenerator.Build(plan);
        var ai = AiContentGenerator.Build(plan);
        var buildServer = BuildServerFiles.For(plan);

        var rootFiles = new List<string>
        {
            "global.json",
            "Directory.Build.props",
            "Directory.Packages.props",
            "nuget.config",
            ".gitignore",
            ".gitattributes",
            ".editorconfig",
            "readme.md",
            ".config/dotnet-tools.json"
        };
        if (buildServer is { } server)
        {
            rootFiles.Add(server.Path);
        }

        Add("global.json", ProjectFiles.GlobalJson(plan));
        Add("Directory.Build.props", ProjectFiles.DirectoryBuildProps(plan));
        Add("Directory.Packages.props", ProjectFiles.DirectoryPackagesProps(plan));
        Add("nuget.config", ProjectFiles.NugetConfig);
        Add(".gitignore", ProjectFiles.GitIgnore);
        Add(".gitattributes", ProjectFiles.GitAttributes);
        Add(".editorconfig", ProjectFiles.EditorConfig(plan));
        Add($"{plan.SolutionName}.slnx", ProjectFiles.Slnx(plan, rootFiles));
        if (plan.Ide.UsesJetBrains())
        {
            Add($"{plan.SolutionName}.slnx.DotSettings", ProjectFiles.SlnxDotSettings);
        }

        Add(".config/dotnet-tools.json", ProjectFiles.DotnetTools(plan));
        Add("readme.md", guide);
        Add("CLAUDE.md", ai);
        Add(".github/copilot-instructions.md", ai);
        if (buildServer is { } definition)
        {
            Add(definition.Path, definition.Content);
        }

        var library = $"src/{plan.LibraryProject}";
        Add($"{library}/{plan.LibraryProject}.csproj", ProjectFiles.LibraryProject(plan));
        Add($"{library}/ClassBeingTested.cs", CodeFiles.ClassBeingTested(plan));
        Add($"{library}/SampleModels.cs", CodeFiles.SampleModels(plan));
        // Types the extension samples exercise: a DbContext, a controller, a component. Each one is a
        // placeholder for the reader's own code, which is why they live beside it rather than in the tests.
        foreach (var file in plan.Extensions.SelectMany(_ => _.Definition.LibraryFiles).DistinctBy(_ => _.Path))
        {
            Add($"{library}/{file.Path}", CodeFiles.Banner(plan) + file.Content.TrimEnd('\n') + "\n");
        }

        var tests = $"src/{plan.TestProject}";
        Add($"{tests}/{plan.TestProject}.{plan.Framework.ProjectExtension}", ProjectFiles.TestProject(plan));
        Add($"{tests}/{plan.Framework.SampleVerifiedFile}", CodeFiles.SampleVerified, bom: true);
        if (plan.Framework.IsFSharp)
        {
            Add($"{tests}/Tests.fs", CodeFiles.ExpectoTests(plan));
        }
        else
        {
            Add($"{tests}/ModuleInitializer.cs", ModuleInitializerGenerator.Build(plan));
            Add($"{tests}/{plan.Framework.SampleClass}.cs", CodeFiles.SampleTest(plan));
            Add($"{tests}/VerifyChecksTests.cs", CodeFiles.VerifyChecksTest(plan));
        }

        if (plan.Framework.Framework == TestFramework.MSTest)
        {
            Add($"{tests}/AssemblyInfo.cs", CodeFiles.MsTestAssemblyInfo(plan));
        }

        if (plan.Framework.Framework == TestFramework.Fixie)
        {
            Add($"{tests}/TestProject.cs", CodeFiles.FixieTestProject(plan));
        }

        foreach (var test in ExtensionTestFiles.For(plan, windows: false))
        {
            Add($"{tests}/{test.Path}", test.Text, test.Bom);
        }

        if (plan.HasWindowsProject)
        {
            var windows = $"src/{plan.WindowsTestProject}";
            Add($"{windows}/{plan.WindowsTestProject}.csproj", ProjectFiles.TestProject(plan, windows: true));
            Add($"{windows}/ModuleInitializer.cs", ModuleInitializerGenerator.BuildWindows(plan));
            if (plan.Framework.Framework == TestFramework.MSTest)
            {
                Add($"{windows}/AssemblyInfo.cs", CodeFiles.MsTestAssemblyInfo(plan));
            }

            if (plan.Framework.Framework == TestFramework.Fixie)
            {
                Add($"{windows}/TestProject.cs", CodeFiles.FixieTestProject(plan));
            }

            foreach (var test in ExtensionTestFiles.For(plan, windows: true))
            {
                Add($"{windows}/{test.Path}", test.Text, test.Bom);
            }
        }

        return files;
    }
}
