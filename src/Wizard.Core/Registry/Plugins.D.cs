namespace Wizard.Core;

public static partial class Plugins
{
    /// <summary>
    /// LocalDb's test base lives in a namespace named after the test framework, so the call is
    /// qualified rather than imported: an import of a namespace the project does not reference is
    /// itself a compile error.
    /// </summary>
    static InitializeStatement LocalDbInitialize(TestFramework framework, string namespaceName) =>
        new(
            $"{namespaceName}.LocalDbTestBase<LocalDbContext>.Initialize();",
            "LocalDb: builds the template database once, so each test can clone it in milliseconds",
            "instead of creating a schema of its own.",
            "It runs after InitializePlugins() because the base class expects the Verify plugins,",
            "Verify.EntityFramework among them, to be registered before the template is built.",
            "It does not pass the EF model to Verify.EntityFramework, so an explicit",
            "VerifyEntityFramework.Initialize(GetDbModel()) is still what enables",
            "IgnoreNavigationProperties() without arguments.")
        {
            TestFrameworks = [framework]
        };

    /// <summary>Entries researched in plan-research/plugin-catalogue-D.md.</summary>
    static IReadOnlyList<PluginDefinition> CatalogueD =>
    [
        new()
        {
            Id = "LocalDb",
            DisplayName = "LocalDb (EfLocalDb)",
            RepoUrl = "https://github.com/SimonCropp/LocalDb",
            Description = "Gives each test its own SQL Server LocalDB database, cloned from a template, with Verify.EntityFramework folded in.",
            Category = PluginCategory.Data,
            // Not a Verify.* package: the Verify integration ships as EfLocalDb.<TestFramework>, each of
            // which already depends on Verify.EntityFramework and the matching Verify adapter.
            Packages =
            [
                new("EfLocalDb.Xunit.V3") {TestFrameworks = [TestFramework.XunitV3]},
                new("EfLocalDb.NUnit") {TestFrameworks = [TestFramework.NUnit]},
                new("EfLocalDb.MSTest") {TestFrameworks = [TestFramework.MSTest]},
                new("EfLocalDb.TUnit") {TestFrameworks = [TestFramework.TUnit]},
                new("EfLocalDb")
                {
                    TestFrameworks = [TestFramework.Fixie, TestFramework.Expecto],
                    Comment = "no base class package exists for these two, so they get the plain package"
                },
                new("Verify.EntityFramework")
                {
                    TestFrameworks = [TestFramework.Fixie, TestFramework.Expecto],
                    Comment = "the EfLocalDb.<TestFramework> packages bring this themselves"
                },
                new("Microsoft.EntityFrameworkCore.SqlServer")
                {
                    ForLibrary = true,
                    Comment = "the provider the sample DbContext uses"
                }
            ],
            // There is no VerifyLocalDb type: LocalDbTestBase<T>.Initialize() is a plain static that has
            // to be called explicitly, so plugin discovery has nothing to find here (plan A9).
            PluginType = null,
            RetiredBy = "U15",
            // Placing the call after InitializePlugins() is the convention every LocalDb doc page shows.
            Phase = InitializePhase.AfterDiscovery,
            Initialize =
            [
                // LocalDbTestBase ships only in the EfLocalDb.<TestFramework> packages, each in a
                // namespace of its own, and Fixie and Expecto get plain EfLocalDb, which has no such
                // type. So the call is written per framework, fully qualified, and omitted for the two
                // that cannot have it.
                LocalDbInitialize(TestFramework.XunitV3, "EfLocalDbXunitV3"),
                LocalDbInitialize(TestFramework.NUnit, "EfLocalDbNunit"),
                LocalDbInitialize(TestFramework.MSTest, "EfLocalDbMSTest"),
                LocalDbInitialize(TestFramework.TUnit, "EfLocalDbTUnit"),
                new(
                    "VerifierSettings.ScrubReplace(\"chatbot_\", \"\");",
                    "LocalDb prefixes its instance name with chatbot_ when the tests run under an AI CLI, which",
                    "would otherwise show up in any snapshot holding a connection string or a data source.")
            ],
            LibraryFiles =
            [
                new(
                    "LocalDbContext.cs",
                    """
                    using Microsoft.EntityFrameworkCore;

                    // Stands in for a real DbContext. LocalDb builds one database per test from this model.
                    public class LocalDbContext(DbContextOptions<LocalDbContext> options) :
                        DbContext(options)
                    {
                        public DbSet<StockItem> StockItems { get; set; } = null!;
                    }

                    public class StockItem
                    {
                        public int Id { get; set; }
                        public string Name { get; set; } = null!;
                    }
                    """)
            ],
            ExternalRequirements =
            [
                new(
                    "SQL Server LocalDB",
                    "part of SQL Server Express LocalDB. LocalDb starts and stops the instance itself, but the " +
                    "SqlLocalDB installation has to exist.")
                {
                    Url = "https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb",
                    WindowsInstall = "choco install sqllocaldb",
                    CannotRunUnattended = true
                }
            ],
            Platform = Platform.WindowsOnly,
            UnsupportedTestFrameworks =
            [
                (TestFramework.Fixie, "There is no EfLocalDb.Fixie package, so the LocalDbTestBase<T> base class does not exist for it."),
                (TestFramework.Expecto, "There is no EfLocalDb package for Expecto, and the samples are C# only.")
            ],
            MinimalSamples =
            [
                new(
                    "AddAndVerify",
                    """
                    await using var database = await sqlInstance.Build();

                    await using (var data = database.NewDbContext())
                    {
                        data.Add(
                            new StockItem
                            {
                                Name = "the name"
                            });
                        await data.SaveChangesAsync();
                    }

                    await using var reader = database.NewDbContext();
                    await Verify(reader.StockItems);
                    """)
                {
                    Async = true,
                    SkipReason = "needs a SQL Server LocalDB installation, which a build agent does not have by default.",
                    Comment =
                    [
                        "Build() clones the template into a database named after this test, so nothing has to be",
                        "cleaned up and tests never see each other's rows.",
                        "The rows come back from a real SQL Server, so the snapshot covers what the database",
                        "stored rather than what the in-memory provider would have returned."
                    ],
                    Members =
                    [
                        """
                        // One instance per class: building the template is the expensive part, and every
                        // Build() call clones it. The EfLocalDb namespace is a global using the package adds.
                        static SqlInstance<LocalDbContext> sqlInstance = new(_ => new(_.Options));
                        """
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "FilteredQuery",
                    """
                    await using var database = await sqlInstance.Build();
                    await using var data = database.NewDbContext();
                    data.Add(
                        new StockItem
                        {
                            Name = "the name"
                        });
                    await data.SaveChangesAsync();

                    var queryable = data
                        .StockItems
                        .Where(_ => _.Name == "the name");
                    await Verify(queryable);
                    """)
                {
                    Async = true,
                    SkipReason = "needs a SQL Server LocalDB installation, which a build agent does not have by default.",
                    Comment =
                    [
                        "Verify.EntityFramework writes two snapshots for an IQueryable: the materialized rows and",
                        "the SQL EF would run. With a real database behind it, a change in either is visible."
                    ]
                }
            ],
            Notes =
            [
                "`LocalDbTestBase<T>` lives in a namespace named after the test framework, such as `EfLocalDbNunit` or `EfLocalDbMSTest`; each package imports it as a global using.",
                "`LocalDbTestBase<T>.Initialize()` throws when the run uses `--report-trx`; call it from assembly setup instead of a module initializer in that case (plan A9, retired by U15).",
                "`LocalDbTestBase<T>` does not pass the EF model to Verify.EntityFramework, so the explicit `VerifyEntityFramework.Initialize(GetDbModel())` call stays (plan A9).",
                "The base class also gives `ArrangeData`, `ActData` and `AssertData`, which throw when a phase is used out of order, plus `VerifyEntity` and `VerifyEntities`.",
                "`[NewDb]`, `[PooledDb]`, `[SharedDb]` and `[SharedDbWithTransaction]` choose the database lifecycle per test.",
                "Calling `Build()` once and sharing the connection string defeats the template cloning: every test then shares one database and has to clean up after itself.",
                "SqlLocalDB only exists on Windows, so the samples go in the Windows test project.",
                "Fixie and Expecto get the plain `EfLocalDb` package: no base class ships for them, so no samples are generated."
            ]
        },
        new()
        {
            Id = "SourceGenerators",
            DisplayName = "Verify.SourceGenerators",
            RepoUrl = "https://github.com/VerifyTests/Verify.SourceGenerators",
            Description = "Snapshots what a C# or VB source generator emits: one file per generated source, plus its diagnostics.",
            Category = PluginCategory.Compiler,
            Packages =
            [
                new("Verify.SourceGenerators"),
                new("Microsoft.CodeAnalysis.CSharp")
                {
                    Comment = "the sample generator and the driver that runs it are written against it"
                }
            ],
            PluginType = "VerifySourceGenerators",
            // RS1036 and RS1041 police assemblies that ship as a real analyzer: one wants
            // EnforceExtendedAnalyzerRules, the other a netstandard2.0 target. This generator is only
            // ever constructed in process by CSharpGeneratorDriver, never packaged or loaded by the
            // compiler, so neither applies, and the build treats warnings as errors.
            ProjectProperties = [("NoWarn", "$(NoWarn);RS1036;RS1041")],
            Usings =
            [
                "System.Text",
                "Microsoft.CodeAnalysis",
                "Microsoft.CodeAnalysis.CSharp",
                "Microsoft.CodeAnalysis.Text"
            ],
            MinimalSamples =
            [
                new(
                    "Driver",
                    """
                    var driver = BuildDriver();

                    return Verify(driver);
                    """)
                {
                    Comment =
                    [
                        "Verifying the driver writes one snapshot per generated source, named after its hint name,",
                        "plus a text snapshot holding the diagnostics the generator reported.",
                        "Reviewing the generated code as a file is the point: a diff shows exactly what changed."
                    ],
                    Members =
                    [
                        """
                        // An empty compilation is enough here: this generator emits during post initialization.
                        // A generator that reads syntax or symbols needs syntax trees and references added.
                        static GeneratorDriver BuildDriver()
                        {
                            var compilation = CSharpCompilation.Create("name");
                            var generator = new HelloWorldGenerator();

                            var driver = CSharpGeneratorDriver.Create(generator);
                            return driver.RunGenerators(compilation);
                        }
                        """,
                        """"
                        // Stands in for a generator of your own. Nested here rather than in the class library
                        // so that the analyzer rules for a shipped generator stay confined to this project.
                        [Generator(LanguageNames.CSharp)]
                        class HelloWorldGenerator :
                            IIncrementalGenerator
                        {
                            public void Initialize(IncrementalGeneratorInitializationContext context) =>
                                context.RegisterPostInitializationOutput(
                                    _ => _.AddSource(
                                        "helloWorld.cs",
                                        SourceText.From(
                                            """
                                            public static class HelloWorld
                                            {
                                                public static void SayHello() =>
                                                    System.Console.WriteLine("Hello World");
                                            }
                                            """,
                                            Encoding.UTF8)));
                        }
                        """"
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "RunResults",
                    """
                    var driver = BuildDriver();

                    var results = driver.GetRunResult();
                    return Verify(results);
                    """)
                {
                    Comment =
                    [
                        "GeneratorDriverRunResult is the same content seen through the results rather than the",
                        "driver, which is what an assertion on a single run usually has to hand."
                    ]
                },
                new(
                    "SingleResult",
                    """
                    var driver = BuildDriver();

                    var result = driver
                        .GetRunResult()
                        .Results
                        .Single();
                    return Verify(result);
                    """)
                {
                    Comment =
                    [
                        "With several generators registered, verifying one GeneratorRunResult keeps a snapshot",
                        "tied to the generator it belongs to."
                    ]
                },
                new(
                    "IgnoreGeneratedResult",
                    """
                    var driver = BuildDriver();

                    return Verify(driver)
                        .IgnoreGeneratedResult(_ => _.HintName.Contains("helper"));
                    """)
                {
                    Comment =
                    [
                        "Generators often emit boilerplate that never changes. This drops a generated source from",
                        "the snapshot entirely, so only the interesting output is reviewed. Calls accumulate."
                    ]
                },
                new(
                    "ScrubLines",
                    """
                    var driver = BuildDriver();

                    return Verify(driver)
                        .ScrubLines(_ => _.StartsWith("using "));
                    """)
                {
                    Comment =
                    [
                        "Generated source is scrubbed with core Verify scrubbers, so churn such as a reordered",
                        "using block can be dropped without ignoring the whole file."
                    ]
                }
            ],
            Notes =
            [
                "One verification produces several targets: a text snapshot for the diagnostics, and one `.cs` or `.vb` per generated source, each named after its hint name.",
                "The package ships an MSBuild targets file that takes `*.received.cs` and `*.verified.cs` out of `Compile` and nests them, so a generated snapshot is never compiled into the test project. Do not add a `Compile Remove` for them.",
                "The output plugin comes from the generated file's path: a `.vb` hint name produces a `vb` target, anything else a `cs` one.",
                "An exception thrown by a generator is rethrown rather than snapshotted, singly or as an `AggregateException`.",
                "`Microsoft.CodeAnalysis.CSharp` is pinned to the version the package was built against, so the generator compiles against the same Roslyn the driver runs.",
                "Referencing `Microsoft.CodeAnalysis.CSharp` turns on the analyzer authoring rules. `RS1036` and `RS1041` both assume the assembly ships as a real analyzer, which this one never does: it is only constructed in process by `CSharpGeneratorDriver`. Both are in `NoWarn` for that reason. A generator you do ship belongs in its own `netstandard2.0` project with `EnforceExtendedAnalyzerRules`.",
                "The sample generator is nested in the test class rather than put in the class library, so those analyzer rules never reach the code under test."
            ]
        },
        new()
        {
            Id = "SqlServer",
            DisplayName = "Verify.SqlServer",
            RepoUrl = "https://github.com/VerifyTests/Verify.SqlServer",
            Description = "Snapshots a SQL Server database schema, and records every command executed on a connection.",
            Category = PluginCategory.Data,
            Packages =
            [
                new("Verify.SqlServer"),
                new("Microsoft.Data.SqlClient") {Comment = "the SqlConnection the samples open"}
            ],
            PluginType = "VerifySqlServer",
            Initialize =
            [
                new(
                    "VerifySqlServer.Initialize();",
                    "Verify.SqlServer: subscribes to the Microsoft.Data.SqlClient diagnostic listener, so every",
                    "command executed while a recording is running is added to the snapshot under the name sql.",
                    "Registers the schema converter for SqlConnection either way.")
                {
                    Alternatives =
                    [
                        "VerifySqlServer.Initialize(recordCommands: false);"
                    ]
                }
            ],
            // DbObjects and the SMO objects a schema filter sees live in a sub-namespace of their own (plan A7).
            Usings = ["Microsoft.Data.SqlClient", "VerifyTests.SqlServer"],
            ExternalRequirements =
            [
                new(
                    "A SQL Server instance",
                    "the samples open a connection to a local SQL Server. Selecting LocalDb as well gives each " +
                    "test a database of its own instead.")
                {
                    CannotRunUnattended = true
                }
            ],
            MinimalSamples =
            [
                new(
                    "Schema",
                    """
                    await using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync();

                    await Verify(connection);
                    """)
                {
                    Async = true,
                    SkipReason = "needs a SQL Server instance reachable at the connection string in this file.",
                    Comment =
                    [
                        "Verifying the connection scripts the whole schema into a snapshot, so a migration that",
                        "changes a table, a view or a stored procedure shows up as a reviewable diff."
                    ],
                    Members =
                    [
                        """
                        // Points at a local SQL Server. Selecting LocalDb as well builds a database per test.
                        const string connectionString = "Server=(local);Database=Sample;Integrated Security=true;Encrypt=false";
                        """
                    ]
                },
                new(
                    "RecordCommands",
                    """
                    await using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync();
                    Recording.Start();

                    await using var command = connection.CreateCommand();
                    command.CommandText = "select Value from MyTable";
                    var value = await command.ExecuteScalarAsync();

                    await Verify(value!);
                    """)
                {
                    Async = true,
                    SkipReason = "needs a SQL Server instance reachable at the connection string in this file.",
                    Comment =
                    [
                        "Everything executed between Recording.Start() and the verification is added under the",
                        "name sql, so the snapshot shows both the result and the SQL that produced it.",
                        "The recorded text is reformatted first, so it reads the same however it was built."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "SchemaIncludes",
                    """
                    await using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync();

                    await Verify(connection)
                        .SchemaIncludes(DbObjects.Tables | DbObjects.Views);
                    """)
                {
                    Async = true,
                    SkipReason = "needs a SQL Server instance reachable at the connection string in this file.",
                    Comment =
                    [
                        "A full schema is large and mostly stable. Restricting it to the object types under test",
                        "keeps the snapshot to what a change is expected to touch."
                    ]
                },
                new(
                    "SchemaFilter",
                    """
                    await using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync();

                    await Verify(connection)
                        .SchemaFilter(_ => _.Name != "sysdiagrams");
                    """)
                {
                    Async = true,
                    SkipReason = "needs a SQL Server instance reachable at the connection string in this file.",
                    Comment =
                    [
                        "A filter decides per object rather than per type, which is how a table a tool created",
                        "is kept out without dropping every other table with it."
                    ]
                },
                new(
                    "SchemaAsSql",
                    """
                    await using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync();

                    await Verify(connection)
                        .SchemaAsSql();
                    """)
                {
                    Async = true,
                    SkipReason = "needs a SQL Server instance reachable at the connection string in this file.",
                    Comment =
                    [
                        "The default schema snapshot is markdown, which reads well in a review. SchemaAsSql writes",
                        "a .verified.sql instead, which can be run against another database."
                    ]
                }
            ],
            Notes =
            [
                "Recorded SQL is reformatted before it is written, so a snapshot is stable against whitespace and bracket differences in the command text.",
                "`Recording.Stop()` returns the entries instead of adding them; a failed command arrives as an `ErrorEntry`.",
                "`DbObjects` and the objects a `SchemaFilter` sees are in the `VerifyTests.SqlServer` namespace (plan A7).",
                "`Initialize` can only run once per process, so a project needing both the recording and the non-recording variant needs two test assemblies.",
                "Scripting the schema uses SQL Server Management Objects, which is a large dependency.",
                "The obsolete `SchemaSettings(...)` is replaced by `SchemaIncludes` and `SchemaFilter`."
            ]
        },
        new()
        {
            Id = "Sylvan",
            DisplayName = "Verify.Sylvan.Data.Excel",
            RepoUrl = "https://github.com/VerifyTests/Verify.Sylvan.Data.Excel",
            Description = "Verifies xls, xlsb and xlsx workbooks by converting each sheet to csv, with no Office install and no licence.",
            Category = PluginCategory.Documents,
            Packages = [new("Verify.Sylvan.Data.Excel")],
            PluginType = "VerifySylvanDataExcel",
            Usings = ["Sylvan.Data.Csv", "Sylvan.Data.Excel"],
            ExclusiveGroups = ["xlsx-converter"],
            MinimalSamples =
            [
                new(
                    "ExcelFile",
                    """
                    return VerifyFile("sample.xlsx");
                    """)
                {
                    SkipReason = "needs a sample.xlsx of your own beside the test, copied to the output directory.",
                    Comment =
                    [
                        "The workbook is converted to csv before it is compared, so a snapshot diff shows the cell",
                        "values that changed rather than a binary file that differs everywhere."
                    ]
                },
                new(
                    "ExcelStream",
                    """
                    await using var stream = File.OpenRead("sample.xlsx");

                    await Verify(stream, "xlsx");
                    """)
                {
                    Async = true,
                    SkipReason = "needs a sample.xlsx of your own beside the test, copied to the output directory.",
                    Comment =
                    [
                        "Code that builds a workbook in memory has a stream rather than a file. The plugin",
                        "passed to Verify is what picks the converter, so it has to be given explicitly."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "ExcelReader",
                    """
                    await using var stream = File.OpenRead("sample.xlsx");
                    using var reader = ExcelDataReader.Create(stream, ExcelWorkbookType.ExcelXml);

                    await Verify(reader);
                    """)
                {
                    Async = true,
                    SkipReason = "needs a sample.xlsx of your own beside the test, copied to the output directory.",
                    Comment =
                    [
                        "A reader can be verified directly, which is what code already reading a workbook has,",
                        "and it names the workbook type rather than inferring it from a plugin."
                    ]
                },
                new(
                    "WriterOptions",
                    """
                    await using var stream = File.OpenRead("sample.xlsx");
                    var options = new CsvDataWriterOptions
                    {
                        Delimiter = '\t'
                    };

                    await Verify(stream, "xlsx")
                        .CsvDataWriterOptions(options);
                    """)
                {
                    Async = true,
                    SkipReason = "needs a sample.xlsx of your own beside the test, copied to the output directory.",
                    Comment =
                    [
                        "Cell values holding commas make a comma separated snapshot hard to read. A tab delimiter",
                        "keeps the columns lined up. NewLine has to stay \\n or the call throws."
                    ]
                }
            ],
            Notes =
            [
                "The samples need a workbook of your own: add it to the test project with `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>` and drop the skip attribute.",
                "A workbook with one sheet produces a single unnamed csv target; several sheets produce one target per sheet, named after the worksheet.",
                "An info object listing the sheet names is always written beside the csv targets.",
                "`CsvDataWriterOptions` throws unless `NewLine` is `\\n`, so snapshots stay identical across operating systems.",
                "`ExcelDataReaderOptions` configures the reading side and is in the source but not in the readme.",
                "Stream converters are registered for `xls`, `xlsb` and `xlsx`.",
                "Pure managed and cross platform: unlike Verify.Syncfusion there is no licence and no native dependency."
            ]
        },
        new()
        {
            Id = "Syncfusion",
            DisplayName = "Verify.Syncfusion",
            RepoUrl = "https://github.com/VerifyTests/Verify.Syncfusion",
            Description = "Verifies pdf, docx, xlsx and pptx documents through Syncfusion File Formats, as text, csv and rendered pages.",
            Category = PluginCategory.Documents,
            Packages = [new("Verify.Syncfusion")],
            PluginType = "VerifySyncfusion",
            RetiredBy = "C4",
            Initialize =
            [
                new(
                    "SyncfusionLicenseProvider.RegisterLicense(Environment.GetEnvironmentVariable(\"SyncfusionLicense\")!);",
                    "Verify.Syncfusion: the licence has to be registered before anything is rendered, so it",
                    "happens here rather than in a test.",
                    "Without a licence every rendered page carries a trial watermark, which then lands in the",
                    "verified png and makes the snapshot useless.")
            ],
            InitializeUsings = ["Syncfusion.Licensing"],
            ExternalRequirements =
            [
                new(
                    "A Syncfusion licence",
                    "Syncfusion File Formats is commercial. The key is read from an environment variable so it " +
                    "never lands in source control.")
                {
                    Url = "https://www.syncfusion.com/sales/licensing",
                    EnvironmentVariable = "SyncfusionLicense",
                    CannotRunUnattended = true
                }
            ],
            ExclusiveGroups = ["pdf-converter", "xlsx-converter", "docx-converter", "pptx-converter"],
            MinimalSamples =
            [
                new(
                    "Pdf",
                    """
                    return VerifyFile("sample.pdf");
                    """)
                {
                    SkipReason = "needs a sample.pdf of your own beside the test, and a SyncfusionLicense environment variable.",
                    Comment =
                    [
                        "A pdf is verified as an info object holding its metadata plus one rendered png per page,",
                        "so a layout change is visible rather than hidden inside a binary."
                    ]
                },
                new(
                    "Excel",
                    """
                    return VerifyFile("sample.xlsx")
                        .ExcludeTargets("xlsx");
                    """)
                {
                    SkipReason = "needs a sample.xlsx of your own beside the test, and a SyncfusionLicense environment variable.",
                    Comment =
                    [
                        "A workbook is verified as csv and as rendered pages, and the source document is also",
                        "written back as a deterministic .verified.xlsx.",
                        "Building that binary is expensive and it is rarely wanted in source control, so",
                        "ExcludeTargets drops it and skips the work."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "Word",
                    """
                    return VerifyFile("sample.docx")
                        .ExcludeTargets("docx");
                    """)
                {
                    SkipReason = "needs a sample.docx of your own beside the test, and a SyncfusionLicense environment variable.",
                    Comment =
                    [
                        "A Word file is verified twice over: its text, which reads well in a diff, and a png of",
                        "each page, which catches a layout change the text cannot show."
                    ]
                },
                new(
                    "PowerPoint",
                    """
                    return VerifyFile("sample.pptx")
                        .ExcludeTargets("pptx");
                    """)
                {
                    SkipReason = "needs a sample.pptx of your own beside the test, and a SyncfusionLicense environment variable.",
                    Comment =
                    [
                        "Each slide is rendered to its own numbered png, so a deck's diff names the slide that",
                        "changed instead of the whole file."
                    ]
                },
                new(
                    "FirstPageOnly",
                    """
                    var settings = new VerifySettings();
                    VerifySyncfusionSettings.PagesToInclude(settings, 1);

                    return VerifyFile("sample.pdf", settings);
                    """)
                {
                    SkipReason = "needs a sample.pdf of your own beside the test, and a SyncfusionLicense environment variable.",
                    Comment =
                    [
                        "Rendering every page of a long document is slow and commits a lot of binary. This keeps",
                        "the first page only; a full document binary target is unaffected.",
                        "The static form is used because six packages define PagesToInclude as an extension method",
                        "in the VerifyTests namespace, so the instance call is ambiguous once two are referenced."
                    ]
                },
                new(
                    "UniqueForRuntime",
                    """
                    await using var stream = File.OpenRead("sample.xlsx");

                    await Verify(stream, "xlsx")
                        .UniqueForRuntime();
                    """)
                {
                    Async = true,
                    SkipReason = "needs a sample.xlsx of your own beside the test, and a SyncfusionLicense environment variable.",
                    Comment =
                    [
                        "Deflate compresses differently per runtime, so the bytes of a binary target differ across",
                        "target frameworks even though the xml inside is identical. A snapshot per runtime avoids it."
                    ]
                }
            ],
            Notes =
            [
                "A licence key is required. The samples read it from the `SyncfusionLicense` environment variable; without one every rendered page is watermarked.",
                "The package carries an open source maintenance fee and an EULA, so `PackageRequireLicenseAcceptance` is set and the fee applies to every organization using it.",
                "Each verification writes numbered targets: `#00` for the document info and `#01` onward for the rendered pages or slides.",
                "`PagesToInclude` and `PdfPngDevice` are in the source but not in the readme.",
                "`VerifierSettings.ExcludeTargets(\"xlsx\")` applies the binary exclusion to every test instead of one.",
                "Rendered png differs slightly between machines; the plugin's own tests call `VerifierSettings.UseSsimForPng(.7)` to tolerate it.",
                "Sample documents need `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>` in the test project.",
                "`PagesToInclude` and `SkipPdfNormalization` are defined by six packages in the `VerifyTests` namespace, so the samples call the static form to avoid CS0121 (plan A6, retired by C4)."
            ]
        },
        new()
        {
            Id = "SystemJson",
            DisplayName = "Verify.SystemJson",
            RepoUrl = "https://github.com/VerifyTests/Verify.SystemJson",
            Description = "Writes System.Text.Json types, JsonDocument, JsonElement and the JsonNode family, as readable snapshots.",
            Category = PluginCategory.Serialization,
            Packages = [new("Verify.SystemJson")],
            PluginType = "VerifySystemJson",
            Usings = ["System.Text.Json"],
            MinimalSamples =
            [
                new(
                    "ParseJsonDocument",
                    """"
                    var json =
                        """
                        {
                          "short": {
                            "original": "http://www.foo.com/",
                            "short": "foo",
                            "error": {
                              "code": 0,
                              "msg": "No action taken"
                            }
                          }
                        }
                        """;

                    var document = JsonDocument.Parse(json);
                    return Verify(document);
                    """")
                {
                    Comment =
                    [
                        "Without this plugin a JsonDocument serializes as its internal structure. With it the",
                        "snapshot is the json itself, in Verify's relaxed form: a .verified.txt, not a .verified.json.",
                        "Values that parse as a guid are scrubbed on the way, so an id generated per run is stable."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "StrictJson",
                    """
                    var document = ParseSample();

                    return Verify(document)
                        .UseStrictJson();
                    """)
                {
                    Comment =
                    [
                        "The relaxed form is easier to read but is not valid json. UseStrictJson writes a",
                        ".verified.json instead, which tooling and json aware diff viewers can parse.",
                        "VerifierSettings.UseStrictJson() in the module initializer applies it to every test."
                    ],
                    Members =
                    [
                        """"
                        static JsonDocument ParseSample() =>
                            JsonDocument.Parse(
                                """
                                {
                                  "short": {
                                    "original": "http://www.foo.com/",
                                    "short": "foo",
                                    "error": {
                                      "code": 0,
                                      "msg": "No action taken"
                                    }
                                  }
                                }
                                """);
                        """"
                    ]
                },
                new(
                    "ScrubMembers",
                    """
                    var document = ParseSample();

                    return Verify(document)
                        .ScrubMember("short")
                        .IgnoreMember("msg");
                    """)
                {
                    Comment =
                    [
                        "The core member scrubbers match json property names, so a value that changes per run is",
                        "replaced, and a member that carries nothing worth reviewing is dropped entirely."
                    ]
                },
                new(
                    "ScrubInline",
                    """
                    var document = ParseSample();

                    return Verify(document)
                        .ScrubInlineDates("yyyy/MM/dd")
                        .ScrubInlineGuids();
                    """)
                {
                    Comment =
                    [
                        "A date or guid embedded inside a string value is not a json value of its own, so it is",
                        "not scrubbed automatically. These find them inside the text."
                    ]
                }
            ],
            Notes =
            [
                "The default snapshot is Verify's relaxed json in a `.verified.txt`, which is deliberate: it is easier to read than strict json.",
                "`UseStrictJson()` switches to a `.verified.json`. Applied globally it changes every snapshot in the assembly, so a project wanting both forms needs two test assemblies.",
                "Json values that map to a known guid format are scrubbed with no opt-in.",
                "`VerifierSettings.ScrubInlineDateTimes` and `VerifierSettings.ScrubInlineGuids` are the assembly-wide forms of the inline scrubbers.",
                "This covers the `System.Text.Json` type family only; `JObject` and `JArray` need Verify.NewtonsoftJson."
            ]
        },
        new()
        {
            Id = "Terminal",
            DisplayName = "Verify.Terminal",
            RepoUrl = "https://github.com/VerifyTests/Verify.Terminal",
            Description = "A dotnet tool for reviewing, accepting and rejecting pending snapshots from the terminal.",
            Category = PluginCategory.Tooling,
            // A dotnet tool rather than a library: nothing is referenced and there is no Initialize.
            Packages = [new("verify.tool") {Kind = PackageKind.DotnetTool}],
            PluginType = null,
            Notes =
            [
                "The package id is `verify.tool`, not Verify.Terminal, and the command it installs is `dotnet verify`.",
                "`dotnet verify review` walks each pending snapshot showing a diff; `dotnet verify accept` and `dotnet verify reject` take or drop them all, with `-y` to skip the prompts.",
                "`-w` sets the working directory and `-c` the number of context lines a review shows.",
                "The working directory has to contain the test project's `obj`: staged inline snapshots live there, so pointing `-w` at a snapshot folder alone finds nothing.",
                "Inline snapshots are handled beside file snapshots; accepting one rewrites the literal in the source file rather than moving a file.",
                "A pending inline snapshot is not written to disk while DiffEngineTray or a DiffEngineViewer owns the queue.",
                "Two kinds cannot be accepted: snapshots that disagree between target frameworks, and ones whose call site has moved or gone.",
                "Verify records which verified file each received file belongs to, so the pairing the tool shows is exact rather than guessed."
            ]
        },
        new()
        {
            Id = "Ulid",
            DisplayName = "Verify.Ulid",
            RepoUrl = "https://github.com/VerifyTests/Verify.Ulid",
            Description = "Scrubs ULIDs, both typed Ulid members and the 26 character form embedded in strings.",
            Category = PluginCategory.Scrubbing,
            Packages = [new("Verify.Ulid")],
            PluginType = "VerifyUlid",
            ExclusiveGroups = ["ulid-scrubber"],
            MinimalSamples =
            [
                new(
                    "UlidScrubbing",
                    """
                    var id = Ulid.NewUlid();
                    var customer = new Customer
                    {
                        Id = id,
                        Name = "Sarah",
                        Description = $"Sarah ({id})"
                    };

                    return Verify(customer);
                    """)
                {
                    Comment =
                    [
                        "A new ULID every run would fail every snapshot. Both the typed member and the copy inside",
                        "the string become Ulid_1, and the same value keeps the same number in both places.",
                        "Nothing is called per test: referencing the package is the whole opt-in."
                    ],
                    Members =
                    [
                        """
                        // Stands in for a type of your own that carries a ULID. Nested here rather than in the
                        // class library, so the library needs no reference to the Ulid package.
                        class Customer
                        {
                            public Ulid Id { get; set; }
                            public string Name { get; set; } = null!;
                            public string Description { get; set; } = null!;
                        }
                        """
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "DontScrubFluent",
                    """
                    var id = Ulid.Parse("01JGXG0GDGQEP47CBQ65E50HYH");
                    var customer = new Customer
                    {
                        Id = id,
                        Name = "Sarah",
                        Description = $"Sarah ({id})"
                    };

                    return Verify(customer)
                        .DontScrubUlids();
                    """)
                {
                    Comment =
                    [
                        "A ULID that is fixed data rather than generated is worth seeing in full, for example when",
                        "the test is about the identifier itself. This turns the scrubbing off for one test."
                    ]
                },
                new(
                    "DontScrubInstance",
                    """
                    var id = Ulid.Parse("01JGXG0GDGQEP47CBQ65E50HYH");
                    var customer = new Customer
                    {
                        Id = id,
                        Name = "Sarah",
                        Description = $"Sarah ({id})"
                    };

                    var settings = new VerifySettings();
                    settings.DontScrubUlids();
                    return Verify(customer, settings);
                    """)
                {
                    Comment =
                    [
                        "The same switch on a VerifySettings instance, which is what a shared settings object or a",
                        "test base class can hand to several tests."
                    ]
                }
            ],
            Notes =
            [
                "Typed `Ulid` members go through a converter; ULIDs inside strings are found by a 26 character window that has to sit on word boundaries and parse as a valid ULID, so false positives are unlikely.",
                "The numbering is shared between the typed and the inline form, so one value reads the same everywhere in a snapshot.",
                "There is no \"verify a ULID\" entry point: the value here is that no test has to opt in.",
                "Verify.NUlid scrubs the same values and defines the same `DontScrubUlids`, so referencing both fails to compile (retired by C6).",
                "The `Ulid` type is in the `System` namespace, so no using is needed."
            ]
        },
        new()
        {
            Id = "WinForms",
            DisplayName = "Verify.WinForms",
            RepoUrl = "https://github.com/VerifyTests/Verify.WinForms",
            Description = "Renders a WinForms form, control or context menu to png and verifies the image.",
            Category = PluginCategory.Ui,
            Packages = [new("Verify.WinForms")],
            PluginType = "VerifyWinForms",
            ProjectProperties = [("UseWindowsForms", "true")],
            Platform = Platform.WindowsOnly,
            MinimalSamples =
            [
                new(
                    "FormUsage",
                    "return Verify(new SampleForm());")
                {
                    Comment =
                    [
                        "The form is drawn to a bitmap and the png is the snapshot, so a change to the layout is",
                        "reviewed as a picture rather than inferred from control properties."
                    ],
                    Members =
                    [
                        """
                        // Stands in for a form of your own. Nested here rather than in the class library, which
                        // is not a WinForms project, so a Form cannot live there.
                        class SampleForm :
                            Form
                        {
                            public SampleForm()
                            {
                                Text = "Sample";
                                ClientSize = new(300, 120);
                                // Qualified because WPF defines a Label too, and selecting Verify.Xaml as well
                                // puts both namespaces in the same project as global usings.
                                Controls.Add(
                                    new System.Windows.Forms.Label
                                    {
                                        Text = "Hello",
                                        Location = new(10, 10),
                                        AutoSize = true
                                    });
                            }
                        }
                        """
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "ContextMenuStrip",
                    """
                    var menu = new ContextMenuStrip();
                    var items = menu.Items;

                    items.Add(new ToolStripMenuItem("About"));
                    items.Add(new ToolStripMenuItem("Exit"));
                    return Verify(menu);
                    """)
                {
                    Comment =
                    [
                        "A menu has no surface of its own to draw on, so it gets a converter that hosts it first.",
                        "Verifying it covers the item text and ordering a user actually sees."
                    ]
                },
                new(
                    "UniqueForRuntime",
                    """
                    return Verify(new SampleForm())
                        .UniqueForRuntime();
                    """)
                {
                    Comment =
                    [
                        "Rendering differs slightly between Windows versions and runtimes. A snapshot per runtime",
                        "keeps a shared machine and a build agent from fighting over the same file."
                    ]
                }
            ],
            Notes =
            [
                "The only output is a png; there is no text snapshot, so an exact byte comparison is brittle. `VerifierSettings.UseSsimForPng()` or an image comparer plugin makes it tolerant.",
                "Converters are registered for `Form`, `ContextMenuStrip`, `UserControl` and `Control`, each producing one png.",
                "Showing a control installs a `WindowsFormsSynchronizationContext` on the thread, which would deadlock Verify's async IO, so the converter saves and restores it around the rendering.",
                "Unlike Verify.Xaml, no STA apartment attribute is needed.",
                "The sample form is nested in the test class: the generated class library is not a WinForms project, so a `Form` cannot live there.",
                "WinForms and WPF define many of the same control names, so selecting Verify.Xaml as well makes `Label`, `Button` and their siblings ambiguous; the sample qualifies the one it uses.",
                "Windows only: the test project targets `net10.0-windows` and sets `UseWindowsForms`."
            ]
        },
        new()
        {
            Id = "Wolverine",
            DisplayName = "Verify.Wolverine",
            RepoUrl = "https://github.com/VerifyTests/Verify.Wolverine",
            Description = "Verifies a Wolverine message handler by recording everything it sends, publishes, invokes and broadcasts.",
            Category = PluginCategory.Messaging,
            Packages =
            [
                new("Verify.Wolverine"),
                new("WolverineFx")
                {
                    ForLibrary = true,
                    Comment = "the sample handler in the class library takes an IMessageBus"
                }
            ],
            PluginType = "VerifyWolverine",
            // Initialize() only sets Initialized: it registers nothing, so there is nothing to order and
            // nothing to pass. Plugin discovery calls it, and the samples work either way.
            Initialize = [],
            // RecordingMessageContext is in VerifyTests.Wolverine, not in the Wolverine namespace.
            Usings = ["VerifyTests.Wolverine"],
            LibraryFiles =
            [
                new(
                    "OrderHandlers.cs",
                    """
                    using Wolverine;

                    // Stands in for real Wolverine handlers. WolverineTests verifies what each one sends.
                    public class OrderHandler(IMessageBus bus)
                    {
                        public ValueTask Handle(OrderPlaced message) =>
                            bus.SendAsync(new OrderConfirmed(message.Reference));
                    }

                    public class OrderRequestHandler(IMessageBus bus)
                    {
                        public async ValueTask<OrderConfirmed> Handle(OrderPlaced message) =>
                            await bus.InvokeAsync<OrderConfirmed>(message);
                    }

                    public record OrderPlaced(string Reference);

                    public record OrderConfirmed(string Reference);
                    """)
            ],
            MinimalSamples =
            [
                new(
                    "HandlerSends",
                    """
                    var context = new RecordingMessageContext();
                    var handler = new OrderHandler(context);

                    await handler.Handle(new("the reference"));

                    await Verify(context);
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "RecordingMessageContext is an IMessageBus that records instead of sending, so the handler",
                        "runs unchanged with no broker, no host and no infrastructure.",
                        "Verifying the context covers every message and every delivery option in one snapshot,",
                        "rather than one assertion per property."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "RequestReply",
                    """
                    var context = new RecordingMessageContext();
                    context.AddInvokeResult<OrderConfirmed>(_ => new OrderConfirmed(((OrderPlaced) _).Reference));
                    var handler = new OrderRequestHandler(context);

                    var confirmed = await handler.Handle(new("the reference"));

                    await Verify(
                        new
                        {
                            confirmed,
                            context
                        });
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "InvokeAsync<T> expects a reply, and a recording context has nobody to reply. AddInvokeResult",
                        "supplies one, so a request and reply handler can be tested without a running bus."
                    ]
                },
                new(
                    "Broadcast",
                    """
                    var context = new RecordingMessageContext();

                    await context.BroadcastToTopicAsync("orders", new OrderPlaced("the reference"));

                    await Verify(context.Broardcasted);
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "Topic broadcasts are recorded separately from sends, with the topic name beside the",
                        "message, which is what a routing change shows up in.",
                        "Broardcasted is spelled that way in the API; it is not a slip in this sample."
                    ]
                }
            ],
            Notes =
            [
                "`VerifyWolverine.Initialize()` only sets `Initialized`: it registers no converters and does nothing else. It exists so plugin discovery has something to find, which is why no call is generated.",
                "The real API here is `RecordingMessageContext`. Pass it into the handler as its `IMessageBus`, then verify the context.",
                "Recorded messages arrive under `Sent`, `Published`, `Invoked` and `Broardcasted`, alongside `Envelope`, `CorrelationId`, `TenantId` and `UserName`.",
                "`Broardcasted` is misspelled in the public API, so the sample matches it (retired by the rename in 22.4).",
                "Request and reply through `InvokeAsync<T>` needs `AddInvokeResult<T>`, either a delegate computing the reply from the message or a constant.",
                "Nothing external is needed: the context is in memory, so the tests run anywhere.",
                "The package targets net9.0 only."
            ]
        },
        new()
        {
            Id = "Xaml",
            DisplayName = "Verify.Xaml",
            RepoUrl = "https://github.com/VerifyTests/Verify.Xaml",
            Description = "Verifies a WPF window or element twice over: the serialized XAML tree and a rendered png.",
            Category = PluginCategory.Ui,
            Packages = [new("Verify.Xaml")],
            PluginType = "VerifyXaml",
            ProjectProperties = [("UseWPF", "true")],
            // UseWPF only brings the assemblies in; Window and TextBlock still need their namespaces,
            // and the WPF ones are not among the SDK's implicit usings.
            Usings = ["System.Windows", "System.Windows.Controls"],
            Platform = Platform.WindowsOnly,
            UnsupportedTestFrameworks =
            [
                (TestFramework.Fixie, "WPF needs an STA apartment, and Fixie has no attribute or convention that gives a test one."),
                (TestFramework.Expecto, "WPF needs an STA apartment, and the samples are C# only.")
            ],
            MinimalSamples =
            [
                new(
                    "WindowUsage",
                    "return Verify(new SampleWindow());")
                {
                    Comment =
                    [
                        "Two snapshots come out of this: a .verified.xml holding the XAML tree, which reads well in",
                        "a diff, and a .verified.png of the rendered window, which catches what the tree cannot.",
                        "The class needs the STA attribute for the test framework in use; see the notes above."
                    ],
                    Members =
                    [
                        """
                        // Stands in for a window of your own. Nested here rather than in the class library, which
                        // is not a WPF project, so a Window cannot live there.
                        class SampleWindow :
                            Window
                        {
                            public SampleWindow()
                            {
                                Title = "Sample";
                                Width = 200;
                                Height = 100;
                                Content = new TextBlock
                                {
                                    Text = "Hello"
                                };
                            }
                        }
                        """
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "FrameworkElement",
                    """
                    var panel = new StackPanel();
                    panel.Children.Add(
                        new TextBlock
                        {
                            Text = "Hello"
                        });

                    return Verify(panel);
                    """)
                {
                    Comment =
                    [
                        "A bare element has no window to be drawn in, so the converter hosts it in one. That means",
                        "a user control can be verified on its own, without the screen it normally lives on."
                    ]
                }
            ],
            Notes =
            [
                "The tests have to run on an STA thread: `[Apartment(ApartmentState.STA)]` on the class for NUnit, `[STAFact]` for xUnit v3, `[STATestClass]` for MSTest, `[STAThreadExecutor]` for TUnit. Add the one for your framework to the generated class (plan U14).",
                "Fixie and Expecto have no apartment mechanism, so no samples are generated for them.",
                "Converters are registered for `Window` and `FrameworkElement` only, and each writes an xml target first and a png target second.",
                "The XAML is re-emitted with one attribute per line, so a diff names the attribute that changed.",
                "Attributes the capture path mutates, `AllowsTransparency`, `ShowInTaskbar`, `WindowStyle`, `Opacity` and `Visibility`, are stripped so the captured XAML matches the element as written.",
                "The window is shown invisibly to force layout before it is rendered.",
                "Rendering differs slightly between Windows versions; Verify.Phash or a custom comparer keeps the png target stable on a build agent.",
                "The sample window is nested in the test class: the generated class library is not a WPF project.",
                "WPF only: not UWP, WinUI, MAUI or Avalonia.",
                "Windows only: the test project targets `net10.0-windows` and sets `UseWPF`."
            ]
        },
        new()
        {
            Id = "Yaml",
            DisplayName = "Verify.Yaml",
            RepoUrl = "https://github.com/VerifyTests/Verify.Yaml",
            Description = "Verifies YamlDotNet types: a whole stream, a document, or a mapping, sequence or scalar node.",
            Category = PluginCategory.Serialization,
            Packages = [new("Verify.Yaml")],
            PluginType = "VerifyYaml",
            Usings = ["YamlDotNet.RepresentationModel"],
            MinimalSamples =
            [
                new(
                    "YamlStreamSample",
                    """"
                    var yaml =
                        """
                        node:
                          original: http://www.foo.com/
                          short: foo
                          error:
                            code: 0
                            msg: No action taken
                        """;

                    var yamlStream = new YamlStream();
                    yamlStream.Load(new StringReader(yaml));
                    return Verify(yamlStream);
                    """")
                {
                    Comment =
                    [
                        "The snapshot is Verify's relaxed json, one array entry per document in the stream, not",
                        "yaml: it is the parsed structure that is being compared, not the file's formatting.",
                        "Mapping keys come out in reverse order, so the snapshot lists msg before code and error",
                        "before short. That is a defect in the converter, not something the document controls."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "ScrubMembers",
                    """
                    var yamlStream = LoadSample();

                    return Verify(yamlStream)
                        .ScrubMember("short")
                        .IgnoreMember("msg");
                    """)
                {
                    Comment =
                    [
                        "The core member scrubbers match yaml keys, so a value that changes per run is replaced and",
                        "a key carrying nothing worth reviewing is dropped."
                    ],
                    Members =
                    [
                        """"
                        static YamlStream LoadSample()
                        {
                            var yaml =
                                """
                                node:
                                  original: http://www.foo.com/
                                  short: foo
                                  error:
                                    code: 0
                                    msg: No action taken
                                """;

                            var yamlStream = new YamlStream();
                            yamlStream.Load(new StringReader(yaml));
                            return yamlStream;
                        }
                        """"
                    ]
                },
                new(
                    "ScrubInline",
                    """
                    var yamlStream = LoadSample();

                    return Verify(yamlStream)
                        .ScrubInlineDates("yyyy/MM/dd")
                        .ScrubInlineGuids();
                    """)
                {
                    Comment =
                    [
                        "A date or guid sitting inside a longer scalar is not a value of its own, so it is not",
                        "scrubbed automatically. These find them inside the text."
                    ]
                }
            ],
            Notes =
            [
                "Verifying a `YamlStream` writes Verify's relaxed json, one entry per document, in a `.verified.txt`. It is not yaml.",
                "Mapping keys are emitted in reverse order, so the snapshot's member order is not the document's (plan A14, retired by U12).",
                "Scalars that parse as a date or a guid are scrubbed with no opt-in.",
                "`YamlStream`, `YamlDocument`, `YamlMappingNode`, `YamlSequenceNode` and `YamlScalarNode` are all verifiable.",
                "The package is at 0.1.0, so its api is less settled than its siblings."
            ]
        },
        new()
        {
            Id = "ZeroLog",
            DisplayName = "Verify.ZeroLog",
            RepoUrl = "https://github.com/VerifyTests/Verify.ZeroLog",
            Description = "Captures what ZeroLog logs during a test into the recording, so the log lands in the snapshot.",
            Category = PluginCategory.Logging,
            Packages = [new("Verify.ZeroLog")],
            PluginType = "VerifyZeroLog",
            Usings = ["ZeroLog"],
            MinimalSamples =
            [
                new(
                    "Usage",
                    """
                    Recording.Start();

                    var result = Method();

                    return Verify(result);
                    """)
                {
                    Comment =
                    [
                        "Everything logged between Recording.Start() and the verification is added to the snapshot",
                        "under the name log, so what the code logged is reviewed alongside what it returned.",
                        "Nothing about the logging has to be passed to Verify."
                    ],
                    Members =
                    [
                        """
                        // Stands in for code under test that logs. The logger is resolved the usual way: the
                        // appender the plugin installed is what captures the messages.
                        static string Method()
                        {
                            var logger = LogManager.GetLogger("Sample");
                            logger.Error("The error");
                            logger.Warn("The warning");
                            return "Result";
                        }
                        """
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "StopRecording",
                    """
                    Recording.Start();

                    var result = Method();

                    var entries = Recording.Stop();
                    return Verify(
                        new
                        {
                            result,
                            entries
                        });
                    """)
                {
                    Comment =
                    [
                        "Recording.Stop() returns the entries instead of adding them, which is how a noisy log can",
                        "be filtered, or counted, before any of it reaches the snapshot."
                    ]
                }
            ],
            Notes =
            [
                "`Initialize()` takes over ZeroLog's global `LogManager`: with no configuration it builds a test configuration, and with one already set it adds its appender to the existing root logger.",
                "An existing configuration that keeps the default asynchronous appending records nothing, because the appender then runs outside the recording's AsyncLocal state. Configure `AppendingStrategy.Synchronous` before initializing (plan A14, retired by U11).",
                "Entries are recorded under the name `log`, each keyed by its level, with the logger name beside it.",
                "Every logging plugin records under `log`, so entries from two of them interleave and `Recording.IgnoreNames` cannot separate them (plan A12).",
                "`VerifyTests.ZeroLog.RecordingLogger` is obsolete and now empty; use `VerifyTests.Recording`.",
                "Anything logged outside a recording currently throws, so the logging has to sit between `Recording.Start()` and the verification (retired by U13)."
            ]
        }
    ];
}
