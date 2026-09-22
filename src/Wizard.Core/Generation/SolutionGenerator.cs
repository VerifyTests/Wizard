namespace Wizard.Core;

/// <summary>The file tree of the downloadable solution for the new-project flow (plan 12.2).</summary>
public static class SolutionGenerator
{
    public static IReadOnlyList<GeneratedFile> Build(Plan plan)
    {
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
        Add(".claude/skills/verify-snapshot-testing/SKILL.md", AiContentGenerator.Skill());
        if (buildServer is { } file)
        {
            Add(file.Path, file.Content);
        }

        var library = $"src/{plan.LibraryProject}";
        Add($"{library}/{plan.LibraryProject}.csproj", ProjectFiles.LibraryProject(plan));
        Add($"{library}/ClassBeingTested.cs", CodeFiles.ClassBeingTested(plan));
        Add($"{library}/SampleModels.cs", CodeFiles.SampleModels(plan));

        var tests = $"src/{plan.TestProject}";
        Add($"{tests}/{plan.TestProject}.{plan.Framework.ProjectExtension}", ProjectFiles.TestProject(plan));
        Add($"{tests}/{plan.Framework.SampleVerifiedFile}", CodeFiles.SampleVerified, bom: true);
        if (plan.Framework.IsFSharp)
        {
            Add($"{tests}/Tests.fs", CodeFiles.ExpectoTests(plan));
        }
        else
        {
            Add($"{tests}/ModuleInitializer.cs", CodeFiles.ModuleInitializer(plan));
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

        return files;
    }
}
