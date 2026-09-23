namespace Wizard.Core;

public static partial class InteractionRules
{
    /// <summary>
    /// Sets of extensions that register the same thing for the same file extension, so only one of them
    /// can be in effect (plan 11.1). Registration is last wins, which makes the outcome depend on the
    /// order plugin discovery happens to find the assemblies in, so the wizard treats them as exclusive.
    /// </summary>
    public static IReadOnlyList<ExclusiveGroup> Groups { get; } =
    [
        new(
            "pdf-converter",
            "PDF converter",
            "each registers a converter for the pdf extension, and the last one registered wins.",
            ["Aspose", "DocNet", "ImageMagick", "PDFium", "PdfPig", "QuestPDF", "Syncfusion"])
        {
            // ImageMagick always registers the pdf converter; comparer-only means another extension's
            // converter is meant to override it, so the group no longer holds (plan A3).
            Conditions =
            [
                new("ImageMagick", "imagemagick-role", "converter-and-comparer", ["converter-and-comparer", "pdf-only"])
            ]
        },
        new(
            "xlsx-converter",
            "Excel converter",
            "each registers a converter for the xlsx extension, and the last one registered wins.",
            ["Aspose", "ClosedXml", "OpenXml", "Sylvan", "Syncfusion"]),
        new(
            "docx-converter",
            "Word converter",
            "each registers a converter for the docx extension, and the last one registered wins.",
            ["Aspose", "OpenXml", "Pandoc", "Syncfusion"]),
        new(
            "pptx-converter",
            "PowerPoint converter",
            "each registers a converter for the pptx extension, and the last one registered wins.",
            ["Aspose", "OpenXml", "Syncfusion"]),
        new(
            "csv-scrubber",
            "CSV handling",
            "both register csv converters and scrubbers, and both define the same settings methods, which then fail to compile.",
            ["CsvHelper", "Sep"]),
        new(
            "image-comparer",
            "Image comparer",
            "each registers a comparer for png and the other image extensions, and the last one registered wins.",
            ["ImageHash", "ImageMagick", "ImageSharpCompare", "Phash"])
        {
            Conditions =
            [
                new("ImageMagick", "imagemagick-role", "converter-and-comparer", ["converter-and-comparer", "comparer-only"])
            ]
        },
        new(
            "blazor-renderer",
            "Blazor renderer",
            "they are alternative ways to render a component, and both register a comparer for html.",
            ["Blazor", "Bunit"]),
        new(
            "activity-listener",
            "Activity listener",
            "each installs a process-wide ActivityListener recording under the name activity, so every activity would be recorded twice.",
            ["Diagnostics", "OpenTelemetry"]),
        new(
            "ulid-scrubber",
            "ULID scrubbing",
            "both scrub 26 character ULIDs and both define DontScrubUlids, which then fails to compile.",
            ["NUlid", "Ulid"])
    ];

    /// <summary>
    /// Combinations that change behaviour without being mutually exclusive (plan 11.2). Everything that
    /// follows from one extension's own data, such as needing Windows, a licence key or an external
    /// tool, is derived in <see cref="PlanBuilder"/> and is not a rule here.
    /// </summary>
    public static IReadOnlyList<InteractionRule> Rules { get; } =
    [
        new()
        {
            Id = "ef-sql-recording",
            Severity = Severity.Warning,
            All = ["EntityFramework", "SqlServer"],
            Message =
                "Verify.EntityFramework and Verify.SqlServer both record every command EF Core runs: " +
                "Verify.EntityFramework under the name ef, with the command type and transaction state, " +
                "and Verify.SqlServer under the name sql. Without a choice here every command appears " +
                "twice in each snapshot.",
            Choice = new(
                "ef-sql-recording",
                "Which extension records EF commands",
                "Both record them; recording is switched off in the other.",
                [
                    new("keep-ef", "Verify.EntityFramework records them", "Adds the command type and transaction state. Verify.SqlServer still snapshots schema."),
                    new("keep-sql", "Verify.SqlServer records them", "Records every SqlCommand, including ones EF did not issue."),
                    new("ignore-names", "Both record, the sql entries are dropped", "Recording.IgnoreNames(\"sql\") needs no ordering, but every command is still cloned before being discarded."),
                    new("both", "Both record, both appear", "Every EF command appears twice in the snapshot.")
                ]),
            Notes =
            [
                "The extension that stops recording is initialized with `recordCommands: false`, which has to happen before `VerifierSettings.InitializePlugins()`: discovery would otherwise initialize it with recording on, and the second call throws \"Already Initialized\".",
                "Verify.SqlServer's schema snapshots are unaffected by this choice."
            ],
            Order = [new("SqlServer", "EntityFramework")],
            Replace =
            [
                new(
                    "keep-ef",
                    "SqlServer",
                    [
                        new(
                            "VerifySqlServer.Initialize(recordCommands: false);",
                            "Verify.SqlServer: schema snapshots stay, but recording is off, because",
                            "Verify.EntityFramework records the same commands under the name ef, with the command",
                            "type and the transaction state.",
                            "This has to run before InitializePlugins(): discovery would otherwise initialize",
                            "Verify.SqlServer with recording on, and a second Initialize throws \"Already Initialized\".")
                    ]),
                new(
                    "keep-sql",
                    "EntityFramework",
                    [
                        new(
                            "VerifyEntityFramework.Initialize(GetDbModel(), recordCommands: false);",
                            "Verify.EntityFramework: the converters and the queryable to SQL converter stay",
                            "registered, but recording is off, because Verify.SqlServer records the same commands",
                            "under the name sql.")
                    ])
            ],
            Add =
            [
                new(
                    "ignore-names",
                    "SqlServer",
                    [
                        new(
                            "Recording.IgnoreNames(\"sql\");",
                            "Both extensions record every EF command. This drops the sql entries from every",
                            "snapshot, which needs no ordering, but each command is still cloned before being",
                            "discarded.")
                    ]),
                new(
                    "both",
                    "SqlServer",
                    [
                        new(
                            "// Both extensions record every EF command, so each one appears twice in a snapshot:",
                            "once under ef, with the command type and transaction state, and once under sql.")
                    ])
            ],
            RetiredBy = "U5"
        },
        new()
        {
            Id = "ef-localdb",
            Severity = Severity.Info,
            All = ["EntityFramework", "LocalDb"],
            Message =
                "LocalDb runs the tests against a real SQL Server LocalDB instance, one database per test, " +
                "so EF snapshots cover what the database actually returns. The EfLocalDb package for the " +
                "chosen test framework already depends on Verify.EntityFramework.",
            Notes =
            [
                "`LocalDbTestBase<T>` does not pass the EF model to Verify.EntityFramework, so the explicit `VerifyEntityFramework.Initialize(GetDbModel())` call stays (plan A9).",
                "LocalDb names each database after the test, and scrubs the generated `chatbot_` prefix from snapshots."
            ],
            RetiredBy = "U15"
        },
        new()
        {
            Id = "sql-localdb",
            Severity = Severity.Info,
            All = ["SqlServer", "LocalDb"],
            Message =
                "LocalDb gives Verify.SqlServer a real database to read a schema from, without needing a " +
                "SQL Server installation.",
            Notes = ["The schema sample builds its instance with `SqlInstance.Build()`."]
        },
        new()
        {
            Id = "flurl-http",
            Severity = Severity.Info,
            All = ["Flurl", "Http"],
            Message =
                "Verify.Flurl depends on Verify.Http and initializes it itself, so Verify.Http is " +
                "initialized first here and Verify.Flurl leaves it alone.",
            Notes = ["Calling `VerifyHttp.Initialize()` after Verify.Flurl has initialized it throws \"Already Initialized\"."],
            Order = [new("Http", "Flurl")]
        },
        new()
        {
            Id = "nservicebus-logging",
            Severity = Severity.Info,
            All = ["NServiceBus", "MicrosoftLogging"],
            Message =
                "Verify.NServiceBus depends on Verify.MicrosoftLogging, so plugin discovery initializes " +
                "it either way. Verify.MicrosoftLogging is initialized first here, and both record under " +
                "the name log.",
            Order = [new("MicrosoftLogging", "NServiceBus")],
            RetiredBy = "U21"
        },
        new()
        {
            Id = "avalonia-mvvm",
            Severity = Severity.Info,
            Any = ["Avalonia", "Xaml"],
            Without = ["CommunityToolkitMvvm"],
            Message =
                "Many Avalonia and WPF projects use CommunityToolkit.Mvvm. Verify.CommunityToolkit.Mvvm " +
                "snapshots its observable properties and commands readably, rather than as generated members."
        },
        new()
        {
            Id = "html-prettyprint",
            Severity = Severity.Info,
            All = ["AngleSharp"],
            Any = ["AspNetCore", "Blazor", "Bunit", "Playwright", "Puppeteer", "Selenium"],
            Message =
                "Rendered html is a single long line, which makes snapshot diffs unreadable. " +
                "`HtmlPrettyPrint.All()` formats every html snapshot, and Verify.AngleSharp compares them " +
                "semantically rather than as text.",
            Notes = ["`HtmlPrettyPrint.All()` is in the `VerifyTests.AngleSharp` namespace, and runs after plugin discovery."],
            Add =
            [
                new(
                    "",
                    "AngleSharp",
                    [
                        new(
                            "HtmlPrettyPrint.All();",
                            "Formats every html and htm snapshot before it is written, so a rendered page is a",
                            "readable tree rather than one long line. Takes an optional action for scrubbing the",
                            "nodes, for example _ => _.ScrubAttributes(\"id\").")
                    ])
                {
                    Usings = ["VerifyTests.AngleSharp"]
                }
            ]
        },
        new()
        {
            Id = "bunit-anglesharp-comparer",
            Severity = Severity.Warning,
            All = ["AngleSharp", "Bunit"],
            Message =
                "Both register a string comparer for html. Verify.Bunit is initialized first, so " +
                "Verify.AngleSharp's semantic comparison wins.",
            Choice = new(
                "bunit-html-comparer",
                "Which html comparer wins",
                "The last one initialized is the one in effect.",
                [
                    new("anglesharp", "Verify.AngleSharp", "Compares the DOM, so attribute order and whitespace do not fail a test."),
                    new("bunit", "Verify.Bunit", "bUnit's own markup comparison.")
                ]),
            Order =
            [
                new("Bunit", "AngleSharp", "anglesharp"),
                new("AngleSharp", "Bunit", "bunit")
            ],
            RetiredBy = "U19"
        },
        new()
        {
            Id = "recording-bus",
            Severity = Severity.Info,
            Any =
            [
                "Diagnostics", "EntityFramework", "Http", "MicrosoftLogging", "NServiceBus",
                "OpenTelemetry", "Serilog", "SqlServer", "ZeroLog"
            ],
            AnyCount = 2,
            Message =
                "These extensions all append to the same recording, which lands in every snapshot taken " +
                "while it is running, each entry under its own name: ef, sql, httpCall, activity and log.",
            Notes =
            [
                "`Recording.Start()` begins one; `Recording.Stop()` returns the entries instead of adding them to the snapshot.",
                "`Recording.IgnoreNames(\"sql\")` drops one kind of entry everywhere, but the extension still produces it first.",
                "Every logging extension records under `log`, so `IgnoreNames` cannot separate them (plan A12)."
            ],
            RetiredBy = "U25"
        },
        new()
        {
            Id = "shared-log-name",
            Severity = Severity.Warning,
            Any = ["MicrosoftLogging", "Serilog", "ZeroLog"],
            AnyCount = 2,
            Message =
                "Every logging extension records under the name log, so their entries interleave in one " +
                "list and cannot be told apart or ignored separately.",
            RetiredBy = "U25"
        },
        new()
        {
            Id = "diffplex-default-comparer",
            Severity = Severity.Info,
            All = ["DiffPlex"],
            Any = ["AngleSharp", "Bunit", "ImageMagick", "Quibble"],
            Message =
                "Verify.DiffPlex is the default comparer for text snapshots. The extensions selected here " +
                "register comparers for specific extensions (html, json, svg), which take precedence over it.",
            Notes = ["A per test `UseDiffPlex()` overrides those comparers again, for that test only."]
        },
        new()
        {
            Id = "readable-expressions-priority",
            Severity = Severity.Info,
            All = ["EntityFramework", "ReadableExpressions"],
            Message =
                "Verify.ReadableExpressions inserts its converter at the front of the list, so expression " +
                "trees inside EF snapshots render as readable C# instead of as node graphs."
        },
        new()
        {
            Id = "settings-method-ambiguity",
            Severity = Severity.Warning,
            Any = ["Aspose", "DocNet", "PDFium", "PdfPig", "QuestPDF", "Syncfusion"],
            AnyCount = 2,
            Message =
                "These packages each define PagesToInclude and SkipPdfNormalization as extension methods " +
                "in the VerifyTests namespace. With two of them referenced, calling one fails to compile " +
                "with CS0121, ambiguous call, even though nothing conflicts at run time.",
            Notes = ["The generated samples call the static form instead, for example `PdfPigSettings.PagesToInclude(settings, 2)`."],
            RetiredBy = "C4"
        },
        new()
        {
            Id = "imagemagick-svg-comparer",
            Severity = Severity.Warning,
            All = ["AngleSharp", "ImageMagick"],
            Message =
                "Both register a comparer for svg: Verify.AngleSharp compares it as markup, " +
                "Verify.ImageMagick as an image. Whichever is initialized last is the one in effect.",
            Order = [new("ImageMagick", "AngleSharp")]
        },
        new()
        {
            Id = "png-converter",
            Severity = Severity.Warning,
            All = ["ImageMagick", "ImageSharp"],
            Message =
                "Both register a converter for png. The last one registered wins, so png snapshots are " +
                "re-encoded by one library or the other, which changes the bytes.",
            RetiredBy = "C5"
        },
        new()
        {
            Id = "imagesharp-reencode",
            Severity = Severity.Warning,
            All = ["ImageSharp"],
            Any = ["Aspose", "DocNet", "ImageMagick", "OpenXml", "PDFium", "Syncfusion"],
            Message =
                "The selected extensions render pages to png, and Verify.ImageSharp then re-encodes that " +
                "png: core ignores PerformConversion for a target a converter produced, so the image is " +
                "written by ImageSharp rather than by the extension that rendered it.",
            RetiredBy = "C5"
        },
        new()
        {
            Id = "cosmos-etag",
            Severity = Severity.Info,
            All = ["Cosmos"],
            Any = ["AspNetCore", "Http"],
            Message =
                "Verify.Cosmos ignores the member ETag on every type, not only on Cosmos types, so an " +
                "ETag on an http response or an ASP.NET Core result is left out of the snapshot too.",
            RetiredBy = "U23"
        }
    ];
}
