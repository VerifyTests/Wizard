/// <summary>Reading the plugins a solution already references out of its Directory.Packages.props (plan 7.2 step 2).</summary>
public class PackagesPropsTests
{
    static async Task<string> Read(string xml)
    {
        await Assert.That(PackagesProps.TryReadPlugins(xml, out var plugins)).IsTrue();
        return string.Join(",", plugins);
    }

    [Test]
    public async Task PackageVersions()
    {
        var plugins = await Read(
            """
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
              </PropertyGroup>
              <ItemGroup>
                <PackageVersion Include="Verify.SqlServer" Version="1.0.0" />
                <PackageVersion Include="Verify.XunitV3" Version="1.0.0" />
                <PackageVersion Include="verify.http" Version="1.0.0" />
                <GlobalPackageReference Include="Verify.Moq" Version="1.0.0" />
                <PackageVersion Update="Verify.Yaml" Version="1.0.0" />
              </ItemGroup>
            </Project>
            """);
        await Assert.That(plugins).IsEqualTo("Http,Moq,SqlServer,Yaml");
    }

    /// <summary>The library a plugin's samples use says nothing about the plugin itself.</summary>
    [Test]
    public async Task ASamplePackageIsNotThePlugin()
    {
        var plugins = await Read(
            """
            <Project>
              <ItemGroup>
                <PackageVersion Include="NodaTime" Version="1.0.0" />
                <PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer" Version="1.0.0" />
              </ItemGroup>
            </Project>
            """);
        await Assert.That(plugins).IsEqualTo("");
    }

    /// <summary>LocalDb's integration is a package per test framework, and any of them is LocalDb.</summary>
    [Test]
    [Arguments("EfLocalDb.Xunit.V3")]
    [Arguments("EfLocalDb.NUnit")]
    [Arguments("EfLocalDb")]
    public async Task LocalDbByAnyOfItsPackages(string package)
    {
        var plugins = await Read($"""<Project><ItemGroup><PackageVersion Include="{package}" Version="1.0.0" /></ItemGroup></Project>""");
        await Assert.That(plugins).IsEqualTo("LocalDb");
    }

    [Test]
    public async Task VerifyEntityFrameworkIsNotLocalDb()
    {
        var plugins = await Read("""<Project><ItemGroup><PackageVersion Include="Verify.EntityFramework" Version="1.0.0" /></ItemGroup></Project>""");
        await Assert.That(plugins).IsEqualTo("EntityFramework");
    }

    [Test]
    public async Task NotXml() =>
        await Assert.That(PackagesProps.TryReadPlugins("not a props file", out _)).IsFalse();
}
