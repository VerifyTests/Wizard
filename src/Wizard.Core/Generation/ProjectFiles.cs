namespace Wizard.Core;

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
            return $"{{\n{sdk}\n}}\n";
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
        var builder = new StringBuilder();
        builder.Append(
            """
            <Project>
              <!-- Applies to every project under this directory. -->
              <PropertyGroup>
                <LangVersion>latest</LangVersion>
                <ImplicitUsings>enable</ImplicitUsings>

            """);
        // F# has its own nullness checking, switched on differently; this setting is for C# only.
        builder.Append(
            """
                <Nullable Condition="'$(MSBuildProjectExtension)' == '.csproj'">enable</Nullable>

            """);
        if (plan.State.SponsorMode == SponsorMode.Ignore)
        {
            builder.Append(
                """
                    <!-- Not TreatWarningsAsErrors: opting out of the maintenance fee logs an SC023 warning on
                         every build, which would then fail it. -->

                """);
        }
        else
        {
            builder.Append(
                """
                    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>

                """);
        }

        builder.Append(
            """
              </PropertyGroup>

            """);
        builder.Append(SponsorXml.Block(plan.State, "  "));
        builder.Append("</Project>\n");
        return builder.ToString();
    }

    public static string DirectoryPackagesProps(Plan plan)
    {
        var builder = new StringBuilder();
        builder.Append(
            """
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

            """);
        AppendVersions(builder, $"Test framework: {plan.Framework.Framework.Name()}", plan.Framework.Packages, plan);
        AppendVersions(builder, "Verify.DiffPlex: text snapshot failures show an inline diff", ["Verify.DiffPlex"], plan);
        builder.Append("</Project>\n");
        return builder.ToString();
    }

    static void AppendVersions(StringBuilder builder, string comment, IEnumerable<string> packages, Plan plan)
    {
        builder.Append($"  <!-- {comment} -->\n");
        builder.Append("  <ItemGroup>\n");
        foreach (var package in packages)
        {
            builder.Append($"    <PackageVersion Include=\"{package}\" Version=\"{plan.Version(package)}\" />\n");
        }

        builder.Append("  </ItemGroup>\n");
    }

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
        var builder = new StringBuilder("<Solution>\n  <Folder Name=\"/Solution Items/\">\n");
        foreach (var item in solutionItems)
        {
            builder.Append($"    <File Path=\"{item}\" />\n");
        }

        builder.Append("  </Folder>\n");
        builder.Append($"  <Project Path=\"src/{plan.LibraryProject}/{plan.LibraryProject}.csproj\" />\n");
        builder.Append($"  <Project Path=\"src/{plan.TestProject}/{plan.TestProject}.{plan.Framework.ProjectExtension}\" />\n");
        builder.Append("</Solution>\n");
        return builder.ToString();
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

    public static string LibraryProject(Plan plan) =>
        $"""
         <Project Sdk="Microsoft.NET.Sdk">
           <!-- The code under test. It references nothing test related. -->
           <PropertyGroup>
             <TargetFramework>{WizardDefaults.TargetFramework}</TargetFramework>
             <RootNamespace>{SolutionNames.RootNamespace(plan.LibraryProject)}</RootNamespace>
           </PropertyGroup>
         </Project>

         """;

    public static string TestProject(Plan plan)
    {
        var framework = plan.Framework;
        var builder = new StringBuilder("<Project Sdk=\"Microsoft.NET.Sdk\">\n");
        builder.Append("  <PropertyGroup>\n");
        builder.Append($"    <TargetFramework>{WizardDefaults.TargetFramework}</TargetFramework>\n");
        builder.Append($"    <RootNamespace>{SolutionNames.RootNamespace(plan.TestProject)}</RootNamespace>\n");
        if (framework.Properties.Count > 0)
        {
            builder.Append("    <!-- Run as a Microsoft.Testing.Platform app; dotnet test uses it through the runner set in global.json. -->\n");
        }

        foreach (var (name, value) in framework.Properties)
        {
            builder.Append($"    <{name}>{value}</{name}>\n");
        }

        if (framework.Framework == TestFramework.Expecto)
        {
            builder.Append("    <!-- Verify.Expecto needs a newer FSharp.Core than the SDK references implicitly, so it is referenced below. -->\n");
            builder.Append("    <DisableImplicitFSharpCoreReference>true</DisableImplicitFSharpCoreReference>\n");
        }

        builder.Append("  </PropertyGroup>\n");

        if (framework.IsFSharp)
        {
            // F# compiles in file order
            builder.Append("  <ItemGroup>\n");
            builder.Append("    <Compile Include=\"Tests.fs\" />\n");
            builder.Append("  </ItemGroup>\n");
        }

        builder.Append("  <ItemGroup>\n");
        foreach (var package in plan.TestPackages)
        {
            builder.Append($"    <PackageReference Include=\"{package}\" />\n");
        }

        builder.Append("  </ItemGroup>\n");
        builder.Append("  <ItemGroup>\n");
        builder.Append($"    <ProjectReference Include=\"..\\{plan.LibraryProject}\\{plan.LibraryProject}.csproj\" />\n");
        builder.Append("  </ItemGroup>\n");
        builder.Append("</Project>\n");
        return builder.ToString();
    }
}
