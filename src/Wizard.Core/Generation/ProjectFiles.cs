/// <summary>Solution-level and project files of the generated solution (plan 12.2).</summary>
public static class ProjectFiles
{
    public static string GlobalJson(Plan plan)
    {
        var sdk =
            $$"""
              "sdk": {
                "version": "{{WizardDefaults.SdkVersion}}",
                "rollForward": "latestFeature"
              }
            """;
        // Fixie runs on VSTest; the MTP runner switch would stop dotnet test from running it (plan D10).
        if (!plan.Framework.UsesTestingPlatform)
        {
            return
                $$"""
                {
                {{sdk}}
                }

                """;
        }

        return
            $$"""
            {
            {{sdk}},
              "test": {
                "runner": "Microsoft.Testing.Platform"
              }
            }

            """;
    }

    public static string DirectoryBuildProps(Plan plan)
    {
        var warnings =
            """
                <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
            """;
        if (plan.State.SponsorMode == SponsorMode.Ignore)
        {
            warnings =
                """
                    <!-- Not TreatWarningsAsErrors: opting out of the maintenance fee logs an SC023 warning on
                         every build, which would then fail it. -->
                """;
        }

        // F# has its own nullness checking, switched on differently; the Nullable setting is for C# only.
        // The sponsor block ends with a newline of its own, so the closing tag follows it on the same line here.
        return
            $"""
             <Project>
               <!-- Applies to every project under this directory. -->
               <PropertyGroup>
                 <LangVersion>latest</LangVersion>
                 <ImplicitUsings>enable</ImplicitUsings>
                 <Nullable Condition="'$(MSBuildProjectExtension)' == '.csproj'">enable</Nullable>
             {warnings}
               </PropertyGroup>
             {SponsorXml.Block(plan.State, "  ", plan.SponsorOwners)}</Project>

             """;
    }

    public static string DirectoryPackagesProps(Plan plan)
    {
        var groups = new List<string>
        {
            Versions($"Test framework: {plan.Framework.Framework.Name()}", plan.Framework.Packages, plan)
        };
        var listed = new HashSet<string>(plan.Framework.Packages, StringComparer.Ordinal);
        foreach (var plugin in plan.Plugins)
        {
            var packages = plugin.Packages
                .Where(_ => _.Kind == PackageKind.PackageReference && listed.Add(_.Id))
                .ToList();
            if (packages.Count == 0)
            {
                continue;
            }

            groups.Add(
                Versions(
                    $"{plugin.Definition.DisplayName}: {plugin.Definition.Description}",
                    packages.Select(_ => _.Id),
                    plan));
        }

        return
            $"""
             <Project>
               <!-- Central Package Management: every package version is set here, once. Projects reference
                    packages with <PackageReference Include="..." /> and no Version.
                    https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management -->
               <PropertyGroup>
                 <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
                 <!-- Transitive dependencies are held to the versions below too, so a package pulled in
                      indirectly cannot resolve to an older version than the one referenced directly. -->
                 <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
               </PropertyGroup>
             {Joined.Lines(groups)}
             </Project>

             """;
    }

    static string Versions(string comment, IEnumerable<string> packages, Plan plan) =>
        Joined.Lines(
        [
            $"  <!-- {comment} -->",
            "  <ItemGroup>",
            .. packages.Select(_ => $"""    <PackageVersion Include="{_}" Version="{plan.Version(_)}" />"""),
            "  </ItemGroup>"
        ]);

    static string PackageReference(string package) =>
        $"""    <PackageReference Include="{package}" />""";

    static string Property((string Name, string Value) property) =>
        $"    <{property.Name}>{property.Value}</{property.Name}>";

    /// <summary>An item group of a project file, one item per entry. Each entry carries its own indentation.</summary>
    static string ItemGroup(IEnumerable<string> items) =>
        Joined.Lines(
        [
            "  <ItemGroup>",
            .. items,
            "  </ItemGroup>"
        ]);

    public const string NugetConfig =
        """
        <?xml version="1.0" encoding="utf-8"?>
        <configuration>
          <config>
            <!-- Only install packages whose signature matches a trusted signer below. -->
            <add key="signatureValidationMode" value="require" />
          </config>
          <packageSources>
            <clear />
            <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
          </packageSources>
          <trustedSigners>
            <repository name="nuget.org" serviceIndex="https://api.nuget.org/v3/index.json">
              <certificate fingerprint="5A2901D6ADA3D18260B9C6DFE2133C95D74B9EEF6AE0E5DC334C8454D1477DF4" hashAlgorithm="SHA256" allowUntrustedRoot="false" />
              <certificate fingerprint="1F4B311D9ACC115C8DC8018B5A49E00FCE6DA8E2855F9F014CA6F34570BC482D" hashAlgorithm="SHA256" allowUntrustedRoot="false" />
              <certificate fingerprint="0E5F38F57DC1BCC806D8494F4F90FBCEDD988B46760709CBEEC6F4219AA6157D" hashAlgorithm="SHA256" allowUntrustedRoot="false" />
            </repository>
          </trustedSigners>
        </configuration>

        """;

    public const string GitIgnore =
        """
        # Verify: received files are the output of a failed run, never committed
        *.received.*
        *.received/

        bin/
        obj/
        .vs/
        .idea/
        *.user
        TestResults/

        """;

    public const string GitAttributes =
        """
        * text=auto

        # Verify: text snapshots are UTF-8 (with BOM, which git passes through) and lf, on every OS
        *.verified.txt text eol=lf working-tree-encoding=UTF-8
        *.verified.xml text eol=lf working-tree-encoding=UTF-8
        *.verified.json text eol=lf working-tree-encoding=UTF-8
        *.verified.md text eol=lf working-tree-encoding=UTF-8
        *.verified.html text eol=lf working-tree-encoding=UTF-8
        *.verified.csv text eol=lf working-tree-encoding=UTF-8
        *.verified.cs text eol=lf working-tree-encoding=UTF-8
        *.verified.bin binary
        *.verified.png binary

        """;

    public static string EditorConfig(Plan plan)
    {
        var builder = new StringBuilder(
            """
            root = true

            [*]
            indent_style = space
            insert_final_newline = true

            [*.{cs,fs}]
            indent_size = 4
            charset = utf-8

            [*.{csproj,fsproj,props,targets,slnx,json,yml,yaml,config}]
            indent_size = 2

            """);
        if (plan.Ide.UsesJetBrains())
        {
            builder.Append(
                """

                # Verify marks methods whose result must be used (such as Verify()) with [Pure];
                # Rider and ReSharper can make ignoring that result an error.
                [*.cs]
                resharper_return_value_of_pure_method_is_not_used_highlighting = error

                """);
        }

        builder.Append(
            """

            # Verify settings
            [*.{received,verified}.{json,txt,xml}]
            charset = utf-8-bom
            end_of_line = lf
            indent_size = unset
            indent_style = unset
            insert_final_newline = false
            tab_width = unset
            trim_trailing_whitespace = false

            [*.{received,verified}.{json,xml,html,htm,yaml,svg}]
            indent_size = 2
            indent_style = space

            """);
        return builder.ToString();
    }

    public static string Slnx(Plan plan, IEnumerable<string> solutionItems)
    {
        var projects = new List<string>
        {
            $"src/{plan.LibraryProject}/{plan.LibraryProject}.csproj",
            $"src/{plan.TestProject}/{plan.TestProject}.{plan.Framework.ProjectExtension}"
        };
        if (plan.HasWindowsProject)
        {
            projects.Add($"src/{plan.WindowsTestProject}/{plan.WindowsTestProject}.csproj");
        }

        return Joined.File(
        [
            "<Solution>",
            """  <Folder Name="/Solution Items/">""",
            .. solutionItems.Select(_ => $"""    <File Path="{_}" />"""),
            "  </Folder>",
            .. projects.Select(_ => $"""  <Project Path="{_}" />"""),
            "</Solution>"
        ]);
    }

    /// <summary>
    /// Rider and ReSharper offer to kill processes a test run leaves behind, which includes diff tools
    /// Verify launched. Verify's docs recommend turning that off per solution.
    /// </summary>
    public const string SlnxDotSettings =
        """
        <wpf:ResourceDictionary xml:space="preserve" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:s="clr-namespace:System;assembly=mscorlib" xmlns:ss="urn:shemas-jetbrains-com:settings-storage-xaml" xmlns:wpf="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
        	<s:String x:Key="/Default/Housekeeping/UnitTestingMru/UnitTestRunner/SpawnedProcessesResponse/@EntryValue">DoNothing</s:String>
        </wpf:ResourceDictionary>

        """;

    public static string DotnetTools(Plan plan) =>
        $$"""
          {
            "version": 1,
            "isRoot": true,
            "tools": {
              "verify.tool": {
                "version": "{{plan.Version("verify.tool")}}",
                "commands": [
                  "dotnet-verify"
                ],
                "rollForward": false
              }
            }
          }

          """;

    public static string LibraryProject(Plan plan)
    {
        var lines = new List<string>
        {
            $"""
             <Project Sdk="Microsoft.NET.Sdk">
               <!-- The code under test. It references nothing test related. -->
               <PropertyGroup>
                 <TargetFramework>{WizardDefaults.TargetFramework}</TargetFramework>
                 <RootNamespace>{SolutionNames.RootNamespace(plan.LibraryProject)}</RootNamespace>
               </PropertyGroup>
             """
        };
        var items = plan.Plugins
            .SelectMany(_ => _.Definition.LibraryProjectItems)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (plan.LibraryPackages.Count > 0 ||
            items.Count > 0)
        {
            lines.Add("  <!-- What the sample types the plugins brought in compile against. -->");
            lines.Add(
                ItemGroup(
                [
                    .. plan.LibraryPackages.Select(PackageReference),
                    .. items.Select(_ => $"    {_}")
                ]));
        }

        lines.Add("</Project>");
        return Joined.File(lines);
    }

    /// <param name="windows">The second project, which holds the Windows-only plugins (plan D5).</param>
    public static string TestProject(Plan plan, bool windows = false)
    {
        var framework = plan.Framework;
        var name = windows ? plan.WindowsTestProject : plan.TestProject;
        var plugins = plan.PluginsIn(windows).ToList();
        var properties = new List<string>();
        if (windows)
        {
            properties.Add(
                $"""
                     <!-- Windows only: these plugins render with Windows APIs, so the target framework
                          has the windows suffix. Nothing references this project, so the rest of the
                          solution still builds and runs on any operating system. -->
                     <TargetFramework>{WizardDefaults.TargetFramework}-windows</TargetFramework>
                 """);
        }
        else
        {
            properties.Add($"    <TargetFramework>{WizardDefaults.TargetFramework}</TargetFramework>");
        }

        properties.Add($"    <RootNamespace>{SolutionNames.RootNamespace(name)}</RootNamespace>");
        properties.AddRange(plugins.SelectMany(_ => _.Definition.ProjectProperties).Distinct().Select(Property));

        if (framework.Properties.Count > 0)
        {
            properties.Add("    <!-- Run as a Microsoft.Testing.Platform app; dotnet test uses it through the runner set in global.json. -->");
        }

        properties.AddRange(framework.Properties.Select(Property));

        if (framework.Framework == TestFramework.Expecto)
        {
            properties.Add(
                """
                    <!-- Verify.Expecto needs a newer FSharp.Core than the SDK references implicitly, so it is referenced below. -->
                    <DisableImplicitFSharpCoreReference>true</DisableImplicitFSharpCoreReference>
                """);
        }

        if (plugins.Count > 0 &&
            framework.SuppressedWarnings.Count > 0)
        {
            properties.AddRange(
                framework.SuppressedWarnings
                    .SelectMany(_ => PluginTestFiles.Wrap($"{_.Code}: {_.Reason}", 92))
                    .Select(_ => $"    <!-- {_} -->"));
            properties.Add($"    <NoWarn>$(NoWarn);{string.Join(";", framework.SuppressedWarnings.Select(_ => _.Code))}</NoWarn>");
        }

        List<string> lines =
        [
            """<Project Sdk="Microsoft.NET.Sdk">""",
            "  <PropertyGroup>",
            .. properties,
            "  </PropertyGroup>"
        ];

        if (framework.IsFSharp)
        {
            // F# compiles in file order
            lines.Add(ItemGroup(["""    <Compile Include="Tests.fs" />"""]));
        }

        var packages = windows ? plan.WindowsTestPackages : plan.TestPackages;
        lines.Add(ItemGroup(packages.Select(PackageReference)));

        var items = plugins.SelectMany(_ => _.Definition.ProjectItems).Distinct(StringComparer.Ordinal).ToList();
        if (items.Count > 0)
        {
            lines.Add(ItemGroup(items.Select(_ => $"    {_}")));
        }

        lines.Add(ItemGroup([$"""    <ProjectReference Include="..\{plan.LibraryProject}\{plan.LibraryProject}.csproj" />"""]));
        lines.Add("</Project>");
        return Joined.File(lines);
    }
}
