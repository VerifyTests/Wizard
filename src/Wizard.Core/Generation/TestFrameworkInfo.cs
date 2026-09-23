namespace Wizard.Core;

/// <summary>
/// Everything that differs per test framework (plan 13). Package lists and project properties match
/// Verify's usages/*NugetUsage projects; sample and VerifyChecks code match the snippets in the Verify
/// repo, which the old docs/wiz pages rendered.
/// </summary>
/// <param name="Packages">Test project packages, in the order the Verify docs list them. Verify.DiffPlex is added separately.</param>
/// <param name="Properties">Test project properties that switch on Microsoft.Testing.Platform.</param>
/// <param name="UsesTestingPlatform">False for Fixie, which has no MTP runner and keeps VSTest (plan D10).</param>
/// <param name="SampleVerifiedFile">The name of the core sample's snapshot, which is shipped (plan D6).</param>
public sealed record TestFrameworkInfo(
    TestFramework Framework,
    IReadOnlyList<string> Packages,
    IReadOnlyList<(string Name, string Value)> Properties,
    bool UsesTestingPlatform,
    bool IsFSharp,
    string SampleTest,
    string VerifyChecksTest,
    string SampleVerifiedFile)
{
    public string ProjectExtension
    {
        get
        {
            if (IsFSharp)
            {
                return "fsproj";
            }

            return "csproj";
        }
    }

    public string CodeLanguage
    {
        get
        {
            if (IsFSharp)
            {
                return "fs";
            }

            return "cs";
        }
    }

    /// <summary>The class (and C# file) holding the sample test: the snapshot name's first segment.</summary>
    public string SampleClass => SampleVerifiedFile[..SampleVerifiedFile.IndexOf('.')];

    /// <summary>Attributes on a generated test class, such as MSTest's <c>[TestClass]</c>.</summary>
    public IReadOnlyList<string> ClassAttributes { get; init; } = [];

    /// <summary>MSTest's source generator only handles partial classes.</summary>
    public bool PartialClasses { get; init; }

    /// <summary>The attribute marking a method as a test; empty for Fixie, which uses a convention.</summary>
    public string TestAttribute { get; init; } = "";

    /// <summary>
    /// How a test that cannot run unattended, such as one needing a licence key, is kept out of a normal
    /// run. Fixie has no such attribute, so the method is made private and its convention skips it.
    /// </summary>
    public Func<string, string>? SkipAttribute { get; init; }

    /// <summary>
    /// Analyzer warnings the generated test project suppresses, with the reason. The solution builds
    /// with warnings as errors, so a rule the samples cannot satisfy has to be turned off rather than
    /// left to fail the first build.
    /// </summary>
    public IReadOnlyList<(string Code, string Reason)> SuppressedWarnings { get; init; } = [];

    public static TestFrameworkInfo For(TestFramework framework) =>
        framework switch
        {
            TestFramework.XunitV3 => xunitV3,
            TestFramework.NUnit => nunit,
            TestFramework.TUnit => tunit,
            TestFramework.MSTest => msTest,
            TestFramework.Fixie => fixie,
            TestFramework.Expecto => expecto,
            _ => throw new ArgumentOutOfRangeException(nameof(framework), framework, null)
        };

    static TestFrameworkInfo xunitV3 = new(
        TestFramework.XunitV3,
        ["Verify.XunitV3", "xunit.v3"],
        [("OutputType", "Exe")],
        UsesTestingPlatform: true,
        IsFSharp: false,
        SampleTest:
        """
        public class Sample
        {
            [Fact]
            public Task Test()
            {
                var person = ClassBeingTested.FindPerson();
                return Verify(person);
            }
        }
        """,
        VerifyChecksTest:
        """
        public class VerifyChecksTests
        {
            [Fact]
            public Task Run() =>
                VerifyChecks.Run();
        }
        """,
        "Sample.Test.verified.txt")
    {
        TestAttribute = "[Fact]",
        SkipAttribute = _ => $"[Fact(Skip = \"{_}\")]",
        SuppressedWarnings =
        [
            ("xUnit1051",
                "the plugin samples call library methods that take an optional CancellationToken " +
                "without passing TestContext.Current.CancellationToken. They are illustrations of one " +
                "API each, and threading a token through every one would bury the thing being shown. " +
                "Remove this once the samples become real tests.")
        ]
    };

    static TestFrameworkInfo nunit = new(
        TestFramework.NUnit,
        ["NUnit", "NUnit3TestAdapter", "Verify.NUnit"],
        [("OutputType", "Exe"), ("EnableNUnitRunner", "true")],
        UsesTestingPlatform: true,
        IsFSharp: false,
        SampleTest:
        """
        [TestFixture]
        public class Sample
        {
            [Test]
            public Task Test()
            {
                var person = ClassBeingTested.FindPerson();
                return Verify(person);
            }
        }
        """,
        VerifyChecksTest:
        """
        [TestFixture]
        public class VerifyChecksTests
        {
            [Test]
            public Task Run() =>
                VerifyChecks.Run();
        }
        """,
        "Sample.Test.verified.txt")
    {
        ClassAttributes = ["[TestFixture]"],
        TestAttribute = "[Test]",
        SkipAttribute = _ => $"[Explicit(\"{_}\")]"
    };

    // TUnit sets OutputType itself and is always a Microsoft.Testing.Platform app
    static TestFrameworkInfo tunit = new(
        TestFramework.TUnit,
        ["TUnit", "Verify.TUnit"],
        [],
        UsesTestingPlatform: true,
        IsFSharp: false,
        SampleTest:
        """
        public class Sample
        {
            [Test]
            public Task Test()
            {
                var person = ClassBeingTested.FindPerson();
                return Verify(person);
            }
        }
        """,
        VerifyChecksTest:
        """
        public class VerifyChecksTests
        {
            [Test]
            public Task Run() =>
                VerifyChecks.Run();
        }
        """,
        "Sample.Test.verified.txt")
    {
        TestAttribute = "[Test]",
        // TUnit's [Explicit] carries no reason; the generated comment above the method has it.
        SkipAttribute = _ => "[Explicit]"
    };

    static TestFrameworkInfo msTest = new(
        TestFramework.MSTest,
        ["MSTest.TestAdapter", "MSTest.TestFramework", "Verify.MSTest"],
        [("OutputType", "Exe"), ("EnableMSTestRunner", "true")],
        UsesTestingPlatform: true,
        IsFSharp: false,
        SampleTest:
        """
        [TestClass]
        public partial class Sample
        {
            [TestMethod]
            public Task Test()
            {
                var person = ClassBeingTested.FindPerson();
                return Verify(person);
            }
        }
        """,
        VerifyChecksTest:
        """
        [TestClass]
        public partial class VerifyChecksTests
        {
            [TestMethod]
            public Task Run() =>
                VerifyChecks.Run();
        }
        """,
        "Sample.Test.verified.txt")
    {
        ClassAttributes = ["[TestClass]"],
        PartialClasses = true,
        TestAttribute = "[TestMethod]",
        SkipAttribute = _ => $"[Ignore(\"{_}\")]"
    };

    static TestFrameworkInfo fixie = new(
        TestFramework.Fixie,
        ["Fixie.TestAdapter", "Microsoft.NET.Test.Sdk", "Verify.Fixie"],
        [],
        UsesTestingPlatform: false,
        IsFSharp: false,
        // Fixie's default discovery only runs classes whose names end with "Tests"; Verify's own Fixie
        // snippet names the class Sample, which that convention would never run.
        SampleTest:
        """
        public class SampleTests
        {
            public Task Test()
            {
                var person = ClassBeingTested.FindPerson();
                return Verify(person);
            }
        }
        """,
        VerifyChecksTest:
        """
        public class VerifyChecksTests
        {
            public Task Run() =>
                VerifyChecks.Run(GetType().Assembly);
        }
        """,
        "SampleTests.Test.verified.txt");

    // Verify.Expecto needs a newer FSharp.Core than the SDK's implicit reference, so FSharp.Core is
    // referenced explicitly (see ProjectFiles).
    static TestFrameworkInfo expecto = new(
        TestFramework.Expecto,
        ["YoloDev.Expecto.TestSdk", "Expecto", "FSharp.Core", "Verify.Expecto"],
        [("OutputType", "Exe"), ("EnableExpectoTestingPlatformIntegration", "true")],
        UsesTestingPlatform: true,
        IsFSharp: true,
        SampleTest:
        """
        [<Tests>]
        let findPerson =
            testTask "findPerson" {
                initialize.Force()
                let person = ClassBeingTested.FindPerson()
                do! Verifier.Verify("findPerson", person).ToTask()
            }
        """,
        VerifyChecksTest:
        """
        [<Tests>]
        let verifyChecks =
            testTask "verifyChecks" {
                initialize.Force()
                do! VerifyChecks.Run(Assembly.GetExecutingAssembly())
            }
        """,
        "Tests.findPerson.verified.txt");

    /// <summary>The C# samples reference types from the class library and the test framework's attributes;
    /// these are the snippets the Verify docs show for the framework-specific conventions.</summary>
    public static IReadOnlyDictionary<string, string> DocSnippets { get; } = new Dictionary<string, string>
    {
        ["VerifyBaseUsage.cs"] =
            """
            ```cs
            [TestClass]
            public class VerifyBaseUsage :
                VerifyBase
            {
                [TestMethod]
                public Task Simple() =>
                    Verify("The content");
            }
            ```
            """,
        ["TestProject.cs"] = $"```cs\n{FixieTestProject}\n```"
    };

    /// <summary>The Fixie convention Verify needs (Verify's src/Verify.Fixie.Tests/TestProject.cs).</summary>
    public const string FixieTestProject =
        """
        public class TestProject :
            ITestProject,
            IExecution
        {
            public void Configure(TestConfiguration configuration, TestEnvironment environment)
            {
                VerifierSettings.AssignTargetAssembly(environment.Assembly);
                configuration.Conventions.Add<DefaultDiscovery, TestProject>();
            }

            public async Task Run(TestSuite testSuite)
            {
                foreach (var testClass in testSuite.TestClasses)
                {
                    foreach (var test in testClass.Tests)
                    {
                        if (test.HasParameters)
                        {
                            foreach (var parameters in test
                                         .GetAll<TestCase>()
                                         .Select(_ => _.Parameters))
                            {
                                using (ExecutionState.Set(testClass, test, parameters))
                                {
                                    await test.Run(parameters);
                                }
                            }
                        }
                        else
                        {
                            using (ExecutionState.Set(testClass, test, null))
                            {
                                await test.Run();
                            }
                        }
                    }
                }
            }
        }
        """;
}
