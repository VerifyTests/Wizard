namespace Wizard.Core;

public static partial class Extensions
{
    /// <summary>Entries researched in plan-research/extension-catalogue-C.md.</summary>
    static IReadOnlyList<ExtensionDefinition> CatalogueC =>
    [
        new()
        {
            Id = "NewtonsoftJson",
            DisplayName = "Verify.NewtonsoftJson",
            RepoUrl = "https://github.com/VerifyTests/Verify.NewtonsoftJson",
            Description = "Writes Newtonsoft.Json JObject and JArray values as readable json, instead of as their object graphs.",
            Category = ExtensionCategory.Serialization,
            Packages =
            [
                new("Verify.NewtonsoftJson"),
                new("Newtonsoft.Json") {Comment = "the JObject, JArray and JToken types the samples parse"}
            ],
            PluginType = "VerifyNewtonsoftJson",
            Usings = ["Newtonsoft.Json.Linq"],
            MinimalSamples =
            [
                new(
                    "JsonObject",
                    """"
                    var target = JObject.Parse(
                        """
                        {
                          "Property1": "Value1",
                          "Property2": 42
                        }
                        """);
                    return Verify(target);
                    """")
                {
                    Comment =
                    [
                        "Verify serializes with Argon, a Newtonsoft.Json fork, so a JObject is otherwise written",
                        "as its internal node graph. This converter writes the json it represents."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "JsonArray",
                    """"
                    var target = JArray.Parse(
                        """
                        ["Small", "Medium", "Large"]
                        """);
                    return Verify(target);
                    """")
                {
                    Comment = ["A JArray is handled by its own converter, and lands in the snapshot as a list."]
                },
                new(
                    "JsonToken",
                    """"
                    var target = JToken.Parse(
                        """
                        {
                          "Nested": {
                            "Property": "Value"
                          }
                        }
                        """);
                    return Verify(target);
                    """")
                {
                    Comment =
                    [
                        "JToken.Parse returns whichever node type the json holds, so the same call covers an",
                        "object, an array or a scalar."
                    ]
                }
            ],
            Notes =
            [
                "This is the Newtonsoft counterpart to Verify.SystemJson: both solve the \"json DOM type\" problem, for different json stacks.",
                "`VerifyNewtonsoftJson` exposes only `Initialize()`. There are no settings methods, scrubbers or per test options.",
                "Json parsed into these types is still scrubbed by Verify's own counters, so a Guid or a date inside it is replaced."
            ]
        },
        new()
        {
            Id = "NodaTime",
            DisplayName = "Verify.NodaTime",
            RepoUrl = "https://github.com/VerifyTests/Verify.NodaTime",
            Description = "Snapshots NodaTime types, scrubbing each value to a counter such as LocalDateTime_1 so runs stay stable.",
            Category = ExtensionCategory.Serialization,
            Packages =
            [
                new("Verify.NodaTime"),
                new("NodaTime") {Comment = "the LocalDateTime and Instant types the samples use"}
            ],
            PluginType = "VerifyNodaTime",
            Usings = ["NodaTime"],
            MinimalSamples =
            [
                new(
                    "Scrubbing",
                    """
                    var target = new
                    {
                        Dob = LocalDateTime.FromDateTime(DateTime.Now)
                    };
                    return Verify(target);
                    """)
                {
                    Comment =
                    [
                        "A wall clock value changes on every run, so the snapshot would never match. Each distinct",
                        "value is replaced with a counter, here LocalDateTime_1, which keeps the ordering visible."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "DontScrub",
                    """
                    var target = new
                    {
                        Dob = LocalDateTime.FromDateTime(new(2010, 2, 10))
                    };
                    return Verify(target)
                        .DontScrubNodaTimes()
                        .DontScrubDateTimes();
                    """)
                {
                    Comment =
                    [
                        "A fixed date is worth reading in full. DontScrubNodaTimes turns this extension off for the",
                        "test, and DontScrubDateTimes turns off Verify's own date scrubbing, which would otherwise",
                        "still write the value as DateTimeOffset_1."
                    ]
                },
                new(
                    "InstantAndInterval",
                    """
                    var target = new
                    {
                        Recorded = Instant.FromUtc(2010, 2, 10, 9, 30),
                        Interval = new DateInterval(
                            new(2010, 2, 10),
                            new(2010, 3, 10))
                    };
                    return Verify(target);
                    """)
                {
                    Comment =
                    [
                        "Converters are registered for AnnualDate, Instant, LocalDate, LocalDateTime, OffsetDate,",
                        "OffsetDateTime, ZonedDateTime, YearMonth and DateInterval, and each gets its own counter."
                    ]
                }
            ],
            Notes =
            [
                "`VerifyNodaTime.DontScrub()` in the module initializer, before `Initialize()`, turns scrubbing off for the whole assembly. It is process wide, so it cannot be toggled per test.",
                "Turning NodaTime scrubbing off leaves Verify's own date scrubbing in place, so a value still reads `DateTimeOffset_1` unless `DontScrubDateTimes()` is called as well.",
                "This layers on top of Verify's date and time counters rather than replacing them.",
                "The package targets net48 through net8.0."
            ]
        },
        new()
        {
            Id = "OpenTelemetry",
            DisplayName = "Verify.OpenTelemetry",
            RepoUrl = "https://github.com/VerifyTests/Verify.OpenTelemetry",
            Description = "Records Activity spans through a process-wide listener, and snapshots them and OpenTelemetry LogRecords.",
            Category = ExtensionCategory.Observability,
            Packages =
            [
                new("Verify.OpenTelemetry"),
                new("OpenTelemetry.Exporter.InMemory") {Comment = "collects LogRecords for the log sample"},
                new("Microsoft.Extensions.Logging") {Comment = "the LoggerFactory the log sample writes through"}
            ],
            PluginType = "VerifyOpenTelemetry",
            ExclusiveGroups = ["activity-listener"],
            Usings = ["System.Diagnostics", "Microsoft.Extensions.Logging", "OpenTelemetry", "OpenTelemetry.Logs"],
            MinimalSamples =
            [
                new(
                    "Activity",
                    """
                    Recording.Start();
                    using var source = new ActivitySource("TestSource");

                    using (var activity = source.StartActivity("MyOperation"))
                    {
                        activity!.SetTag("key1", "value1");
                        activity.SetTag("key2", 42);
                    }

                    return Verify("result");
                    """)
                {
                    Comment =
                    [
                        "Every activity that stops between Recording.Start() and the verification is added to the",
                        "snapshot under the name activity, keyed by its operation name.",
                        "Ids, timestamps and durations are left out, because they change on every run."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "LogRecords",
                    """
                    var logRecords = new List<LogRecord>();
                    using (var loggerFactory = LoggerFactory.Create(
                               _ => _.AddOpenTelemetry(
                                   options => options.AddInMemoryExporter(logRecords))))
                    {
                        var logger = loggerFactory.CreateLogger("TestCategory");
                        logger.LogInformation("Hello {Name}", "World");
                    }

                    await Verify(logRecords);
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "LogRecords are collected by OpenTelemetry's in memory exporter and verified directly:",
                        "there is no Verify specific api for them, only the converter this extension registers.",
                        "The factory is disposed before verifying so the exporter has flushed."
                    ]
                },
                new(
                    "StopRecording",
                    """
                    Recording.Start();
                    using var source = new ActivitySource("TestSource");

                    using (var activity = source.StartActivity("MyOperation"))
                    {
                        activity!.SetTag("key", "value");
                    }

                    var entries = Recording.Stop();
                    return Verify(entries);
                    """)
                {
                    Comment =
                    [
                        "Recording.Stop() returns the entries instead of adding them, so they can be filtered or",
                        "combined with other values before being verified."
                    ]
                }
            ],
            Notes =
            [
                "The `ActivityListener` is process wide and listens to every `ActivitySource`, so a parallel test can capture activities another test started.",
                "Activities land in the shared recording under `activity`; other recording extensions add their own entries to the same snapshot.",
                "Kept for an activity: operation name, display name when it differs, kind when it is not Internal, status, tags, events, links and baggage.",
                "Kept for a LogRecord: category name, level, body, formatted message, a non-default event id, the exception and the attributes.",
                "`Initialize()` has to run before the first verification; it throws otherwise.",
                "The package targets net10.0 only."
            ]
        },
        new()
        {
            Id = "OpenXml",
            DisplayName = "Verify.OpenXml",
            RepoUrl = "https://github.com/VerifyTests/Verify.OpenXml",
            Description = "Verifies Word, Excel and PowerPoint files: text, a csv per sheet, properties, a deterministic copy and page renders.",
            Category = ExtensionCategory.Documents,
            Packages =
            [
                new("Verify.OpenXml"),
                new("DocumentFormat.OpenXml")
                {
                    ForLibrary = true,
                    Comment = "builds the sample documents, so no fixture file has to be added"
                },
                new("Morph.Skia")
                {
                    WhenChoice = "openxml-render:skia",
                    Comment = "the render backend that turns each page into a png"
                },
                new("Morph.ImageSharp")
                {
                    WhenChoice = "openxml-render:imagesharp",
                    Comment = "the render backend that turns each page into a png"
                }
            ],
            PluginType = "VerifyOpenXml",
            ExclusiveGroups = ["xlsx-converter", "docx-converter", "pptx-converter"],
            Initialize =
            [
                new(
                    "VerifyOpenXml.UseLetterPageSize = false;",
                    "Verify.OpenXml: the paper size used when a document states none. Left null it follows the",
                    "machine's region, so a rendered page would differ between a US and a European agent.",
                    "False is A4, true is US Letter. It only matters once a render backend is referenced.")
                {
                    Alternatives =
                    [
                        "VerifyOpenXml.FontDirectory = Path.Combine(AppContext.BaseDirectory, \"Fonts\");"
                    ]
                }
            ],
            Usings = ["DocumentFormat.OpenXml.Packaging"],
            LibraryFiles =
            [
                new(
                    "WordDocument.cs",
                    """
                    using DocumentFormat.OpenXml;
                    using DocumentFormat.OpenXml.Packaging;
                    using DocumentFormat.OpenXml.Wordprocessing;

                    // Stands in for the reader's own document. It is built in code so the samples need no
                    // fixture file, and it lives here because the Wordprocessing and Spreadsheet namespaces
                    // define colliding type names and cannot be imported into one file.
                    public static class WordDocument
                    {
                        public static MemoryStream Build()
                        {
                            var stream = new MemoryStream();
                            using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
                            {
                                var part = document.AddMainDocumentPart();
                                part.Document = new(
                                    new Body(
                                        new Paragraph(
                                            new Run(
                                                new Text("Hello World! This is a sample Word document."))),
                                        new Paragraph(
                                            new Run(
                                                new Text("This is the second paragraph with some more text.")))));
                            }

                            return new(stream.ToArray());
                        }
                    }
                    """),
                new(
                    "ExcelDocument.cs",
                    """
                    using DocumentFormat.OpenXml;
                    using DocumentFormat.OpenXml.Packaging;
                    using DocumentFormat.OpenXml.Spreadsheet;

                    // Stands in for the reader's own workbook, built in code for the same reason as WordDocument.
                    public static class ExcelDocument
                    {
                        public static MemoryStream Build()
                        {
                            var stream = new MemoryStream();
                            using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                            {
                                var workbookPart = document.AddWorkbookPart();
                                workbookPart.Workbook = new();
                                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                                worksheetPart.Worksheet = new(
                                    new SheetData(
                                        BuildRow("First Name", "Last Name", "Country"),
                                        BuildRow("Dulce", "Abril", "United States"),
                                        BuildRow("Mara", "Hashimoto", "Great Britain")));
                                workbookPart.Workbook
                                    .AppendChild(new Sheets())
                                    .AppendChild(
                                        new Sheet
                                        {
                                            Id = workbookPart.GetIdOfPart(worksheetPart),
                                            SheetId = 1U,
                                            Name = "Sheet1"
                                        });
                            }

                            return new(stream.ToArray());
                        }

                        static Row BuildRow(params string[] values)
                        {
                            var row = new Row();
                            foreach (var value in values)
                            {
                                row.AppendChild(
                                    new Cell
                                    {
                                        DataType = CellValues.String,
                                        CellValue = new(value)
                                    });
                            }

                            return row;
                        }
                    }
                    """)
            ],
            Choices =
            [
                new(
                    "openxml-render",
                    "Page rendering backend",
                    "Rendering each page to png is opt in, and needs exactly one backend package. With none, the info, text and csv targets still verify.",
                    [
                        new("none", "No rendering", "No backend package, and no png targets."),
                        new("skia", "Morph.Skia", "Renders through Skia, with native binaries."),
                        new("imagesharp", "Morph.ImageSharp", "Renders through ImageSharp, fully managed.")
                    ])
            ],
            MinimalSamples =
            [
                new(
                    "Word",
                    """
                    return Verify(WordDocument.Build(), "docx");
                    """)
                {
                    Comment =
                    [
                        "The extension passed to Verify decides which converter runs. A docx writes an info file",
                        "with the document properties, the extracted text, and a deterministic copy of the file."
                    ]
                },
                new(
                    "Excel",
                    """
                    return Verify(ExcelDocument.Build(), "xlsx");
                    """)
                {
                    Comment =
                    [
                        "A workbook writes one .verified.csv per sheet, plus an info file listing the sheets and",
                        "their columns, so a changed cell shows up as a one line diff."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "WordDocumentObject",
                    """
                    await using var stream = WordDocument.Build();
                    using var document = WordprocessingDocument.Open(stream, false);
                    await Verify(document);
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "An open SpreadsheetDocument, WordprocessingDocument or PresentationDocument can be",
                        "verified directly, which is useful when the test already has one in hand."
                    ]
                },
                new(
                    "ExcludeExcelPackage",
                    """
                    return Verify(ExcelDocument.Build(), "xlsx")
                        .ExcludeTargets("xlsx");
                    """)
                {
                    Comment =
                    [
                        "Building the deterministic package is expensive, and committing a binary is not always",
                        "wanted. This drops it and skips building it; the info, csv and rendered pages still verify."
                    ]
                },
                new(
                    "UniqueForRuntime",
                    """
                    return Verify(WordDocument.Build(), "docx")
                        .UniqueForRuntime();
                    """)
                {
                    Comment =
                    [
                        "The xml inside the package is identical across runtimes, but the Deflate implementation",
                        "differs, so the binary target needs a snapshot per runtime when the project multi targets."
                    ]
                }
            ],
            Notes =
            [
                "One verification writes several targets: an info txt, the text or a csv per sheet, the deterministic binary, and a png per page once a backend is referenced.",
                "Png rendering needs exactly one of `Morph.Skia` or `Morph.ImageSharp`. With neither, rendering is silently skipped; with both, the first verification throws.",
                "The backend is found by probing for the assembly, so switching it is a package change only; no code changes.",
                "Rendering is compiled in on net10.0 only, because Morph targets net10.0.",
                "Rendered pages depend on the installed fonts, so set `VerifyOpenXml.FontDirectory` to a bundled font folder and generate the `.verified.png` files on one canonical machine.",
                "Page counts differ per format: Word is one page per laid out page, PowerPoint one per slide in `p:sldIdLst` order, and Excel follows the print layout rather than the sheet.",
                "pptx is verified the same way, by passing a stream with the extension `pptx` or a `PresentationDocument`.",
                "`UniqueForOSPlatform()` is worth adding once rendering is on and the tests run on more than one OS."
            ]
        },
        new()
        {
            Id = "PDFium",
            DisplayName = "Verify.PDFium",
            RepoUrl = "https://github.com/VerifyTests/Verify.PDFium",
            Description = "Verifies pdf documents through PDFium: page sizes, extracted text, a normalized pdf and a png per page.",
            Category = ExtensionCategory.Documents,
            Packages = [new("Verify.PDFium")],
            PluginType = "VerifyPDFium",
            ExclusiveGroups = ["pdf-converter"],
            MinimalSamples =
            [
                new(
                    "Pdf",
                    """
                    return VerifyFile("sample.pdf");
                    """)
                {
                    SkipReason = "needs a sample.pdf in the test project, copied to the output directory.",
                    Comment =
                    [
                        "A pdf writes three kinds of target: an info file with the page count, sizes, text and",
                        "document properties, a normalized .verified.pdf, and one png per rendered page."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "PdfStream",
                    """
                    return Verify(File.OpenRead("sample.pdf"), "pdf");
                    """)
                {
                    SkipReason = "needs a sample.pdf in the test project, copied to the output directory.",
                    Comment = ["A pdf already in memory or coming from a producer is verified as a stream instead."]
                },
                new(
                    "ExcludePdfDocument",
                    """
                    return VerifyFile("sample.pdf")
                        .ExcludePdfDocument();
                    """)
                {
                    SkipReason = "needs a sample.pdf in the test project, copied to the output directory.",
                    Comment =
                    [
                        "Some producers embed bytes that normalization cannot neutralize, such as the machine's",
                        "system fonts. This drops the .verified.pdf and keeps the info file and the page renders."
                    ]
                },
                new(
                    "SkipPdfNormalization",
                    """
                    return VerifyFile("sample.pdf")
                        .SkipPdfNormalization();
                    """)
                {
                    SkipReason = "needs a sample.pdf in the test project, copied to the output directory.",
                    Comment =
                    [
                        "Snapshots the bytes exactly as produced. Only worth it for a genuinely deterministic",
                        "producer: otherwise a fresh /CreationDate and /ID make the snapshot differ on every run."
                    ]
                }
            ],
            Notes =
            [
                "The samples read `sample.pdf` from the output directory. Add the file to the test project with a `None Update` item and `CopyToOutputDirectory=PreserveNewest`.",
                "`VerifyPDFium.Initialize(dpi: 150)` in the module initializer, before `InitializePlugins()`, changes the render resolution. The default 96 dpi renders an A4 page at 794 x 1123.",
                "Renders are byte identical for a given Morph.PDFium version on every machine and OS, and no image library is added.",
                "Each verification emits an info txt, a pdf and one png per page, so the snapshot count grows quickly for a long document.",
                "`ExcludePdfDocument()` and `SkipPdfNormalization()` exist only in the fluent form here; the other pdf extensions also ship `VerifySettings` overloads.",
                "`SkipPdfNormalization` is defined in the `VerifyTests` namespace by several pdf extensions, so referencing two of them makes the fluent call ambiguous (CS0121).",
                "The package targets net10.0 only, and the native PDFium binaries arrive with Morph.PDFium for Windows, Linux and macOS."
            ]
        },
        new()
        {
            Id = "Pandoc",
            DisplayName = "Verify.Pandoc",
            RepoUrl = "https://github.com/VerifyTests/Verify.Pandoc",
            Description = "Converts docx and rtf documents to markdown through pandoc, and verifies the markdown.",
            Category = ExtensionCategory.Documents,
            Packages = [new("Verify.Pandoc")],
            PluginType = "VerifyPandoc",
            ExclusiveGroups = ["docx-converter"],
            ExternalRequirements =
            [
                new(
                    "Pandoc",
                    "PandocNet shells out to the pandoc executable, so pandoc has to be installed on the machine running the tests.")
                {
                    Url = "https://pandoc.org/installing.html",
                    WindowsInstall = "choco install pandoc",
                    LinuxInstall = "sudo apt-get install -y pandoc",
                    CannotRunUnattended = true
                },
                new(
                    "Papyrine sponsorship declaration",
                    "The Pandoc package this depends on ships a second SponsorCheck gate, for the owner Papyrine. Without Papyrine_ prefixed sponsorship properties the build fails, on top of Verify's own Verify_ properties.")
                {
                    CannotRunUnattended = true
                }
            ],
            MinimalSamples =
            [
                new(
                    "Word",
                    """
                    return VerifyFile("sample.docx");
                    """)
                {
                    SkipReason = "needs pandoc installed and a sample.docx in the test project's output directory.",
                    Comment =
                    [
                        "A docx is converted to markdown and verified as a single .verified.md, so the snapshot is",
                        "the readable content rather than the package's xml."
                    ]
                },
                new(
                    "Rtf",
                    """
                    return VerifyFile("sample.rtf");
                    """)
                {
                    SkipReason = "needs pandoc installed and a sample.rtf in the test project's output directory.",
                    Comment = ["rtf is the other supported input, and converts to markdown the same way."]
                }
            ],
            VerboseSamples =
            [
                new(
                    "WordStream",
                    """
                    return Verify(File.OpenRead("sample.docx"), "docx");
                    """)
                {
                    SkipReason = "needs pandoc installed and a sample.docx in the test project's output directory.",
                    Comment = ["The same converter runs for a stream, with the extension naming the input format."]
                },
                new(
                    "RtfStream",
                    """
                    return Verify(File.OpenRead("sample.rtf"), "rtf");
                    """)
                {
                    SkipReason = "needs pandoc installed and a sample.rtf in the test project's output directory.",
                    Comment = ["The stream overload also suits content produced in the test rather than read from disk."]
                }
            ],
            Notes =
            [
                "The supported inputs are docx and rtf. Both produce one `md` target, so this never collides with the pdf or image extensions.",
                "The `Pandoc` package it depends on brings a second SponsorCheck gate, for the owner `Papyrine`, so the project needs `Papyrine_` prefixed sponsorship properties as well as Verify's own.",
                "The samples read `sample.docx` and `sample.rtf` from the output directory. Add them with a `None Update` item and `CopyToOutputDirectory=PreserveNewest`.",
                "It owns the `docx` converter, so it cannot be combined with another extension that also converts docx.",
                "`VerifyPandoc` exposes only `Initialize()`: there are no settings methods or scrubbers.",
                "Verify.Pandoc is pre 1.0 (0.1.1)."
            ]
        },
        new()
        {
            Id = "ParametersHashing",
            DisplayName = "Verify.ParametersHashing",
            RepoUrl = "https://github.com/VerifyTests/Verify.ParametersHashing",
            Description = "Hashes test parameters into the snapshot file name with XxHash64, so large parameters cannot exceed path limits.",
            Category = ExtensionCategory.Testing,
            Packages = [new("Verify.ParametersHashing")],
            PluginType = "VerifyParametersHashing",
            MinimalSamples =
            [
                new(
                    "HashParameters",
                    """
                    var settings = new VerifySettings();
                    settings.HashParameters();
                    return Verify("The target", settings);
                    """)
                {
                    Comment =
                    [
                        "The snapshot file name normally ends in the stringified parameters, which can push the",
                        "path past what the OS allows. This appends an XxHash64 of them instead.",
                        "Add the test framework's parameterised attribute and a parameter to see it: the name then",
                        "ends in _25a91d2c54f27235 rather than in the value."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "HashParametersFluent",
                    """
                    return Verify("The target")
                        .HashParameters();
                    """)
                {
                    Comment =
                    [
                        "The fluent form does the same thing, for a test that does not already build a",
                        "VerifySettings of its own."
                    ]
                }
            ],
            Notes =
            [
                "This changes file *names*, not content. It has no converters, comparers or scrubbers.",
                "`VerifyParametersHashing.Initialize()` registers nothing and only sets the initialized flag; the feature is entirely in the per verification `HashParameters()` call.",
                "There is no global opt in: hashing is per verification.",
                "The hash is the lowercase hex XxHash64 of the joined `_name=value` string, so changing a parameter renames its snapshot.",
                "It replaces Verify's parameters appender, so it clashes with anything else that calls `UseParametersAppender`."
            ]
        },
        new()
        {
            Id = "PdfPig",
            DisplayName = "Verify.PdfPig",
            RepoUrl = "https://github.com/VerifyTests/Verify.PdfPig",
            Description = "Verifies pdf documents through PdfPig: document information, page count, page sizes and extracted text.",
            Category = ExtensionCategory.Documents,
            Packages = [new("Verify.PdfPig")],
            PluginType = "VerifyPdfPig",
            ExclusiveGroups = ["pdf-converter"],
            RetiredBy = "C4",
            MinimalSamples =
            [
                new(
                    "Pdf",
                    """
                    var settings = new VerifySettings();
                    PdfPigSettings.PagesToInclude(settings, 2);
                    return VerifyFile("sample.pdf", settings);
                    """)
                {
                    SkipReason = "needs a sample.pdf in the test project, copied to the output directory.",
                    Comment =
                    [
                        "A pdf writes an info file with the document information, page count, page sizes and the",
                        "extracted text, plus a normalized .verified.pdf.",
                        "PagesToInclude trims the info file to the first pages, so a long document stays readable.",
                        "It is called in its static form because several pdf extensions define the same extension",
                        "method in the VerifyTests namespace, and the fluent call is then ambiguous (CS0121)."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "PdfStream",
                    """
                    return Verify(File.OpenRead("sample.pdf"), "pdf");
                    """)
                {
                    SkipReason = "needs a sample.pdf in the test project, copied to the output directory.",
                    Comment = ["A pdf produced in the test is verified as a stream, with pdf as the extension."]
                },
                new(
                    "ExcludePdf",
                    """
                    return VerifyFile("sample.pdf")
                        .ExcludeTargets("pdf");
                    """)
                {
                    SkipReason = "needs a sample.pdf in the test project, copied to the output directory.",
                    Comment =
                    [
                        "The source pdf is included as a .verified.pdf. Where committing a binary is not wanted,",
                        "ExcludeTargets drops it and skips normalizing it, while the info and text still verify."
                    ]
                },
                new(
                    "SkipNormalization",
                    """
                    var settings = new VerifySettings();
                    PdfPigSettings.SkipPdfNormalization(settings);
                    return VerifyFile("sample.pdf", settings);
                    """)
                {
                    SkipReason = "needs a sample.pdf in the test project, copied to the output directory.",
                    Comment =
                    [
                        "Snapshots the bytes as produced, skipping the pass that neutralizes the trailer /ID, the",
                        "/CreationDate and /ModDate, and the XMP dates. Only safe for a deterministic producer.",
                        "The static form again, for the same ambiguity reason as PagesToInclude."
                    ]
                }
            ],
            Notes =
            [
                "The samples read `sample.pdf` from the output directory. Add the file to the test project with a `None Update` item and `CopyToOutputDirectory=PreserveNewest`.",
                "`PagesToInclude` trims only the info and text output. The `.verified.pdf` is always the whole document, because PdfPig has no in-place page splitter.",
                "`PagesToInclude` and `SkipPdfNormalization` are defined in the `VerifyTests` namespace by several pdf extensions, so the fluent call fails to compile with CS0121 when two are referenced. The samples use the static form.",
                "`PdfPigSettings.PdfPigParsingOptions(settings, options)` passes PdfPig `ParsingOptions`, for a password protected or malformed pdf. It is not in the readme.",
                "PdfPig is fully managed, so there is no native dependency and nothing to install.",
                "This extracts text and does not render, so there are no png targets. Verify.PDFium is the rendering alternative."
            ]
        },
        new()
        {
            Id = "Phash",
            DisplayName = "Verify.Phash",
            RepoUrl = "https://github.com/VerifyTests/Verify.Phash",
            Description = "Compares image snapshots by perceptual hash, so a render that is perceptually equivalent still passes.",
            Category = ExtensionCategory.Images,
            Packages =
            [
                new("Verify.Phash"),
                new("System.Drawing.Common") {Comment = "draws the sample image, so no fixture file has to be added"}
            ],
            PluginType = "VerifyPhash",
            Platform = Platform.WindowsOnly,
            Phase = InitializePhase.Comparers,
            ExclusiveGroups = ["image-comparer"],
            RetiredBy = "U22",
            Usings = ["System.Drawing", "System.Drawing.Imaging"],
            MinimalSamples =
            [
                new(
                    "Png",
                    """
                    return Verify(BuildImage(), "png");
                    """)
                {
                    Comment =
                    [
                        "This registers a comparer, not a converter, so the snapshot is an ordinary .verified.png.",
                        "What changes is the comparison: a re-render whose bytes differ, because of antialiasing or",
                        "a font hinting change, still passes while it looks the same."
                    ],
                    Members =
                    [
                        """
                        // Stands in for whatever the test renders. It is drawn in code so the samples need no
                        // fixture file. System.Drawing is why this extension is Windows only.
                        static MemoryStream BuildImage()
                        {
                            var stream = new MemoryStream();
                            using (var bitmap = new Bitmap(120, 120))
                            {
                                using (var graphics = Graphics.FromImage(bitmap))
                                {
                                    graphics.Clear(Color.CornflowerBlue);
                                    graphics.FillEllipse(Brushes.White, 20, 20, 80, 80);
                                }

                                bitmap.Save(stream, ImageFormat.Png);
                            }

                            stream.Position = 0;
                            return stream;
                        }
                        """
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "CompareSettings",
                    """
                    return Verify(BuildImage(), "png")
                        .PhashCompareSettings(
                            threshold: .8f,
                            sigma: 4f,
                            gamma: 2f,
                            angles: 170);
                    """)
                {
                    Comment =
                    [
                        "Per test tuning of the comparison. threshold is a cross correlation of the two digests, so",
                        "a higher value is stricter; the default is .999.",
                        "The failure message reads: diff > threshold. threshold: {threshold}, score: {score}."
                    ]
                }
            ],
            Notes =
            [
                "Windows only: the package targets `net48` and `net8.0-windows`, and decodes images with System.Drawing.",
                "`Initialize()` registers the comparer for png only, although the readme says png, jpg, bmp and tiff.",
                "There is no parameterless `RegisterComparer()`. The extension argument is required, for example `VerifyPhash.RegisterComparer(\"jpg\")` in the module initializer.",
                "`RegisterComparer` also takes threshold, sigma, gamma and angles, so a second extension can be registered with its own tuning.",
                "It writes no new snapshot target, so nothing changes about what is committed.",
                "Verify's own `VerifierSettings.UseSsimForPng()` solves the same problem without a package, and only one image comparer can be in effect."
            ]
        },
        new()
        {
            Id = "QuestPDF",
            DisplayName = "Verify.QuestPDF",
            RepoUrl = "https://github.com/VerifyTests/Verify.QuestPDF",
            Description = "Verifies a QuestPDF IDocument: a metadata and settings snapshot, a deterministic pdf, and a png per page.",
            Category = ExtensionCategory.Documents,
            Packages =
            [
                new("Verify.QuestPDF"),
                new("QuestPDF") {Comment = "the fluent document api the sample builds a report with"}
            ],
            // The class is VerifyQuestPdf, so plugin discovery, which looks for VerifyTests.VerifyQuestPDF,
            // never finds it (plan A1).
            PluginType = "VerifyQuestPdf",
            ExclusiveGroups = ["pdf-converter"],
            RetiredBy = "C2",
            ExternalRequirements =
            [
                new(
                    "QuestPDF licence",
                    "QuestPDF refuses to generate a document until a licence type is set. Community covers open source and small business use; commercial use above their threshold needs a paid key.")
                {
                    Url = "https://www.questpdf.com/license/"
                }
            ],
            Initialize =
            [
                new(
                    "QuestPDF.Settings.License = LicenseType.Community;",
                    "Verify.QuestPDF: QuestPDF throws on the first generated document until a licence type is",
                    "set. Community covers open source and small business use; above their revenue threshold a",
                    "paid key is needed instead."),
                new(
                    "VerifyQuestPdf.Initialize();",
                    "Verify.QuestPDF: registers a file converter for IDocument, so verifying a document writes",
                    "its metadata and settings, a deterministic pdf, and a png per rendered page.",
                    "The call is explicit because the class is named VerifyQuestPdf, and plugin discovery looks",
                    "for a type named after the assembly, VerifyQuestPDF, so it never finds this one.")
            ],
            InitializeUsings = ["QuestPDF.Infrastructure"],
            Usings = ["QuestPDF.Fluent", "QuestPDF.Helpers", "QuestPDF.Infrastructure", "VerifyQuestPDF"],
            MinimalSamples =
            [
                new(
                    "VerifyDocument",
                    """
                    var document = GenerateDocument();
                    return Verify(document);
                    """)
                {
                    Comment =
                    [
                        "An IDocument is verified directly: QuestPDF generates the pdf, and the snapshot is the",
                        "page count and the document settings, beside the pdf and the rendered pages.",
                        "The creation and modified dates are pinned before generation, so the bytes are stable."
                    ],
                    Members =
                    [
                        """
                        // Stands in for a real report. The document is built in code, so the samples need no
                        // fixture file and a change to the layout shows up in the snapshot.
                        static IDocument GenerateDocument() =>
                            Document.Create(
                                _ => _.Page(
                                    page =>
                                    {
                                        page.Size(PageSizes.A4);
                                        page.Margin(2, Unit.Centimetre);
                                        page.Header()
                                            .Text("Sample report");
                                        page.Content()
                                            .Text("Hello World!");
                                    }));
                        """
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "PagesToInclude",
                    """
                    var document = GenerateDocument();
                    var settings = new VerifySettings();
                    QuestPDFSettings.PagesToInclude(settings, 1);
                    return Verify(document, settings);
                    """)
                {
                    Comment =
                    [
                        "Renders only the first pages, which keeps the png count down for a long report. The pdf",
                        "target is unaffected and always holds the whole document.",
                        "The static form is used because several pdf extensions define PagesToInclude in the",
                        "VerifyTests namespace, and the fluent call is then ambiguous (CS0121)."
                    ]
                },
                new(
                    "PagesToIncludeDynamic",
                    """
                    var document = GenerateDocument();
                    var settings = new VerifySettings();
                    ShouldIncludePage include = pageNumber => pageNumber % 2 == 1;
                    QuestPDFSettings.PagesToInclude(settings, include);
                    return Verify(document, settings);
                    """)
                {
                    Comment =
                    [
                        "The delegate overload picks pages by number, for a report where only some pages matter.",
                        "ShouldIncludePage lives in the VerifyQuestPDF namespace, not in VerifyTests."
                    ]
                },
                new(
                    "ExcludePdf",
                    """
                    var document = GenerateDocument();
                    return Verify(document)
                        .ExcludeTargets("pdf");
                    """)
                {
                    Comment =
                    [
                        "Generating the pdf is expensive and committing it is not always wanted. This drops the",
                        "pdf target, leaving the info snapshot and the rendered pages."
                    ]
                }
            ],
            Notes =
            [
                "The plugin class is named `VerifyQuestPdf` and discovery looks for `VerifyQuestPDF`, so `InitializePlugins()` alone never enables it and the explicit call is required.",
                "`QuestPDF.Settings.License` has to be set before any document is generated.",
                "Pages are rasterized through Skia, so they vary with the OS and the installed fonts. Pair them with `VerifierSettings.UseSsimForPng()` or an image comparer.",
                "`PagesToInclude` trims only the png pages; the `.verified.pdf` is always the whole document.",
                "`Metadata` is left out of the info snapshot when the document sets no metadata members.",
                "It registers a file converter on `IDocument` rather than a `pdf` stream converter, so the pdf it emits is what another pdf extension would then process.",
                "`PagesToInclude` and `SkipPdfNormalization` are defined in the `VerifyTests` namespace by several pdf extensions, so the samples call the static form."
            ]
        },
        new()
        {
            Id = "Quibble",
            DisplayName = "Verify.Quibble",
            RepoUrl = "https://github.com/VerifyTests/Verify.Quibble",
            Description = "Reports a failing json snapshot as structural, JSON-path level differences instead of as a text diff.",
            Category = ExtensionCategory.Serialization,
            Packages = [new("Verify.Quibble")],
            PluginType = "VerifyQuibble",
            // UseStrictJson has to run before Initialize, which reads it, so both statements go in the
            // settings block ahead of InitializePlugins() (plan A14).
            Phase = InitializePhase.Settings,
            RetiredBy = "U20",
            Initialize =
            [
                new(
                    "VerifierSettings.UseStrictJson();",
                    "Verify.Quibble: Verify writes a relaxed variant of json by default, which Quibble cannot",
                    "parse, so strict json is required. This changes every snapshot in the project to real json.",
                    "It has to come first: Initialize() below throws without it, and it sets Initialized before",
                    "it throws, so a failed call cannot be retried."),
                new(
                    "VerifyQuibble.Initialize();",
                    "Verify.Quibble: registers a string comparer for json, so a mismatch is reported per JSON",
                    "path rather than as a diff of the whole file.",
                    "Both calls run before InitializePlugins(), which would otherwise initialize Quibble first,",
                    "with strict json still off.")
            ],
            MinimalSamples =
            [
                new(
                    "Json",
                    """
                    var target = new
                    {
                        Property1 = "ValueA",
                        Property2 = "ValueB"
                    };
                    return Verify(target);
                    """)
                {
                    Comment =
                    [
                        "With strict json on, the snapshot is a .verified.json rather than Verify's relaxed txt.",
                        "When a value changes, the failure reads: String value difference at $.Property1: ValueC",
                        "vs ValueA, instead of showing both files in full."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "NestedJson",
                    """
                    var target = new
                    {
                        Name = "the name",
                        Address = new
                        {
                            Street = "the street",
                            City = "the city"
                        }
                    };
                    return Verify(target);
                    """)
                {
                    Comment =
                    [
                        "The value of a structural comparison shows on nested data: the report names the path,",
                        "for example $.Address.City, rather than a line number in a reformatted file."
                    ]
                }
            ],
            Notes =
            [
                "`VerifierSettings.UseStrictJson()` is mandatory and has to run first. `Initialize()` sets `Initialized` before it throws about strict json, so a failed call cannot be retried.",
                "Strict json changes every snapshot in the project from Verify's relaxed format to real json, so it is not a drop in for an existing suite.",
                "It registers a string comparer for `json`, so it takes precedence over Verify.DiffPlex for json snapshots. A per test `UseDiffPlex()` overrides it again.",
                "An empty verified file is treated as `{}`.",
                "Quibble is an F# library, so `FSharp.Core` arrives transitively.",
                "There are no settings methods or per test toggles: the only configuration is the `UseStrictJson()` call."
            ]
        },
        new()
        {
            Id = "RavenDB",
            DisplayName = "Verify.RavenDB",
            RepoUrl = "https://github.com/VerifyTests/Verify.RavenDB",
            Description = "Verifies a RavenDB IDocumentSession, writing every pending change in that session to the snapshot.",
            Category = ExtensionCategory.Data,
            Packages =
            [
                new("Verify.RavenDB"),
                new("RavenDB.Embedded") {Comment = "starts a server for the samples; the shipped package only needs RavenDB.Client"}
            ],
            PluginType = "VerifyRavenDB",
            ExternalRequirements =
            [
                new(
                    "RavenDB embedded server",
                    "RavenDB.Embedded downloads a RavenDB server on first use and starts it against a temporary data directory, so the tests need network access and a machine that can run it.")
                {
                    Url = "https://ravendb.net/docs/article-page/latest/csharp/start/installation/embedded",
                    CannotRunUnattended = true
                }
            ],
            Usings = ["Raven.Client.Documents", "Raven.Embedded"],
            LibraryFiles =
            [
                new(
                    "RavenPerson.cs",
                    """
                    // Stands in for a real document type. The samples store it in a RavenDB session and verify
                    // the changes the session is holding.
                    public class RavenPerson
                    {
                        public string Id { get; set; } = null!;
                        public string Name { get; set; } = null!;
                    }
                    """)
            ],
            MinimalSamples =
            [
                new(
                    "AddDocument",
                    """
                    using var session = store.Value.OpenSession();
                    var person = new RavenPerson
                    {
                        Name = "John"
                    };
                    session.Store(person);
                    await Verify(session);
                    """)
                {
                    Async = true,
                    SkipReason = "downloads and starts an embedded RavenDB server the first time it runs.",
                    Comment =
                    [
                        "Verifying the session snapshots what it is about to send: the document key and a",
                        "DocumentAdded change carrying the new value. Nothing is saved, so no cleanup is needed."
                    ],
                    Members =
                    [
                        """
                        static readonly Lazy<IDocumentStore> store = new(StartServer);

                        // The embedded server is downloaded on first use and started against a temp directory,
                        // which is why the samples are skipped by default.
                        static IDocumentStore StartServer()
                        {
                            EmbeddedServer.Instance.StartServer();
                            return EmbeddedServer.Instance.GetDocumentStore("Samples");
                        }
                        """
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "UpdateDocument",
                    """
                    using var session = store.Value.OpenSession();
                    var person = new RavenPerson
                    {
                        Name = "John"
                    };
                    session.Store(person);
                    session.SaveChanges();

                    person.Name = "Joe";
                    await Verify(session);
                    """)
                {
                    Async = true,
                    SkipReason = "downloads and starts an embedded RavenDB server the first time it runs.",
                    Comment =
                    [
                        "Once the document exists, the session tracks a FieldChanged instead, with the field name",
                        "and both the old and the new value, so the diff is the change rather than the document."
                    ]
                }
            ],
            Notes =
            [
                "Verifying an `IDocumentSession` writes its pending changes: documents added, and the field name with the old and new value for each change.",
                "`VerifyRavenDB` exposes only `Initialize()`: there are no settings methods, scrubbers or recording.",
                "The shipped package depends only on `RavenDB.Client`; `RavenDB.Embedded` is for tests.",
                "No licence key is needed for the embedded community server.",
                "The package targets net9.0 only.",
                "This is the RavenDB counterpart to Verify.EntityFramework's ChangeTracker snapshots, and the two do not overlap."
            ]
        },
        new()
        {
            Id = "ReadableExpressions",
            DisplayName = "Verify.ReadableExpressions",
            RepoUrl = "https://github.com/VerifyTests/Verify.ReadableExpressions",
            Description = "Renders LINQ expression trees as readable C# source, instead of the default ToString() of the tree.",
            Category = ExtensionCategory.Compiler,
            Packages = [new("Verify.ReadableExpressions")],
            PluginType = "VerifyReadableExpressions",
            Usings = ["System.Linq.Expressions"],
            MinimalSamples =
            [
                new(
                    "QueryExpression",
                    """
                    Expression<Func<IEnumerable<int>, IEnumerable<int>>> expression =
                        numbers => numbers
                            .Where(number => number > 10)
                            .Select(number => number * 2);
                    return Verify(expression);
                    """)
                {
                    Comment =
                    [
                        "An expression tree's own ToString() is barely readable once it nests. This writes the C#",
                        "the tree represents, so a change to a query shows up as a change in source."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "MethodCall",
                    """
                    Expression<Func<decimal, decimal>> expression =
                        salary => Math.Round(salary * 1.15m, 2);
                    return Verify(expression);
                    """)
                {
                    Comment =
                    [
                        "A method call renders with its arguments as C#, rather than as a MethodCallExpression",
                        "node with a MethodInfo and an argument list."
                    ]
                },
                new(
                    "NestedExpression",
                    """
                    Expression<Func<int, bool>> predicate = value => value > 10;
                    var target = new
                    {
                        Name = "the filter",
                        predicate
                    };
                    return Verify(target);
                    """)
                {
                    Comment =
                    [
                        "TreatAsString<Expression> is registered too, so an expression nested inside another",
                        "object renders inline as C# rather than as a nested node graph."
                    ]
                }
            ],
            Notes =
            [
                "The converter is inserted at index 0, so it wins over any other converter that handles `Expression`, including one an ORM extension registers.",
                "With Verify.EntityFramework selected, expression trees inside EF snapshots therefore render as C#.",
                "`VerifyReadableExpressions` exposes only `Initialize()`: no settings methods, no formatting options.",
                "Verify.ReadableExpressions is pre 1.0 (0.1.0)."
            ]
        },
        new()
        {
            Id = "SendGrid",
            DisplayName = "Verify.SendGrid",
            RepoUrl = "https://github.com/VerifyTests/Verify.SendGrid",
            Description = "Snapshots SendGrid types: SendGridMessage, EmailAddress, Attachment and Personalization.",
            Category = ExtensionCategory.Email,
            Packages =
            [
                new("Verify.SendGrid"),
                new("SendGrid") {Comment = "the message types the samples build"}
            ],
            PluginType = "VerifySendGrid",
            Usings = ["SendGrid.Helpers.Mail"],
            MinimalSamples =
            [
                new(
                    "Message",
                    """
                    var mail = new SendGridMessage
                    {
                        From = new("test@example.com", "DX Team"),
                        Subject = "Sending with Twilio SendGrid is Fun",
                        PlainTextContent = "and easy to do anywhere, even with C#",
                        HtmlContent = "<strong>and easy to do anywhere, even with C#</strong>"
                    };
                    mail.AddTo(new EmailAddress("test@example.com", "Test User"));
                    return Verify(mail);
                    """)
                {
                    Comment =
                    [
                        "The message is verified, not the delivery: no account, api key or network access is",
                        "needed, and the snapshot shows what would have been sent.",
                        "Addresses render as Name <address>, and empty members are left out."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "EmailAttachment",
                    """
                    var contentBytes = "The content"u8.ToArray();
                    var attachment = new Attachment
                    {
                        Filename = "name.txt",
                        Content = Convert.ToBase64String(contentBytes),
                        Type = "text/html",
                        Disposition = "attachment"
                    };
                    return Verify(attachment);
                    """)
                {
                    Comment =
                    [
                        "SendGrid holds attachment content as base64, which is unreadable in a diff. The converter",
                        "decodes it, so the snapshot shows the text and a changed attachment is visible."
                    ]
                },
                new(
                    "Personalizations",
                    """
                    var mail = new SendGridMessage
                    {
                        From = new("test@example.com", "DX Team"),
                        Subject = "The subject",
                        ReplyTo = new("reply@example.com", "Reply To")
                    };
                    mail.AddTo(new EmailAddress("first@example.com", "First"));
                    mail.AddTo(new EmailAddress("second@example.com", "Second"));
                    return Verify(mail);
                    """)
                {
                    Comment =
                    [
                        "Recipients live in a Personalization list rather than on the message, and the converter",
                        "flattens it, so the reply-to and both recipients read as plain lines."
                    ]
                }
            ],
            Notes =
            [
                "It registers serializer converters only, with no file extension converter, so it cannot collide with a format based extension.",
                "`VerifySendGrid` exposes only `Initialize()`: there are no settings methods or scrubbers.",
                "Covered types are `SendGridMessage`, `EmailAddress`, `Attachment` and `Personalization`, including the reply-to variants.",
                "The package's description and tags are copied from Verify.MailMessage and its `RootNamespace` says Verify.ZeroLog; the package itself is right."
            ]
        },
        new()
        {
            Id = "Sep",
            DisplayName = "Verify.Sep",
            RepoUrl = "https://github.com/VerifyTests/Verify.Sep",
            Description = "Verifies csv through Sep: a csv scrubber and a SepReader converter, with per column ignore, scrub and translate.",
            Category = ExtensionCategory.Data,
            Packages = [new("Verify.Sep")],
            PluginType = "VerifySep",
            ExclusiveGroups = ["csv-scrubber"],
            MinimalSamples =
            [
                new(
                    "Csv",
                    """
                    return Verify(SampleCsv(), "csv");
                    """)
                {
                    Comment =
                    [
                        "A csv is scrubbed before it is written: each Guid and date goes through Verify's counters,",
                        "so the snapshot reads Guid_1 and Date_1 and stays stable across runs."
                    ],
                    Members =
                    [
                        """"
                        // The csv the samples verify, built in code so there is no fixture file to add.
                        static MemoryStream SampleCsv() =>
                            new(
                                """
                                Index,Customer Id,First Name,Last Name,Country,Dob
                                1,1f9d5b0c-1b6b-4f7d-9a1a-2c3d4e5f6a7b,Sheryl,Baxter,Chile,2017-10-15
                                2,2a8c4d1e-3f5b-4c6d-8e9f-0a1b2c3d4e5f,Preston,Lozano,Djibouti,2016-08-16
                                3,3b7e6f2a-4c8d-4e9f-8a0b-1c2d3e4f5a6b,Roy,Berry,Antigua,2015-05-21
                                """u8.ToArray());
                        """"
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "IgnoreColumns",
                    """
                    return Verify(SampleCsv(), "csv")
                        .IgnoreCsvColumns("Customer Id");
                    """)
                {
                    Comment =
                    [
                        "Drops the column from the snapshot entirely, for data that is noise rather than something",
                        "the test is about."
                    ]
                },
                new(
                    "ScrubColumns",
                    """
                    return Verify(SampleCsv(), "csv")
                        .ScrubCsvColumns("Customer Id");
                    """)
                {
                    Comment =
                    [
                        "Replaces the column's values with {Scrubbed}, which keeps the column in the snapshot so a",
                        "change in the shape of the file is still visible."
                    ]
                },
                new(
                    "TranslateColumn",
                    """
                    return Verify(SampleCsv(), "csv")
                        .TranslateCsvColumn(
                            _ =>
                            {
                                if (_ == "Country")
                                {
                                    return country => country.ToUpperInvariant();
                                }

                                return null;
                            });
                    """)
                {
                    Comment =
                    [
                        "Per column value transform: the outer function is given the column name and returns the",
                        "transform for it, or null to leave the column alone.",
                        "The fluent overload is named TranslateCsvColumn and the VerifySettings one",
                        "TranslateCsvColumns; the names differ in the package."
                    ]
                }
            ],
            Notes =
            [
                "Guid and date values are respected by Verify's counters, so an untranslated cell becomes `Guid_1`, `Date_1` and so on.",
                "A Sep `SepReader` can be verified directly, through a registered file converter: `Verify(reader)`.",
                "It hooks `csv` as a scrubber, so it also scrubs the csv per sheet that Verify.OpenXml writes for a workbook.",
                "Verify.Sep and Verify.CsvHelper both add a `csv` scrubber and define the same settings method names, so referencing both fails to compile.",
                "`TranslateCsvColumns` on `VerifySettings` and `TranslateCsvColumn` on `SettingsTask` are the same feature under two names, and neither is in the readme.",
                "Verify.Sep is pre 1.0 (0.2.0) and targets net8.0 only."
            ]
        },
        new()
        {
            Id = "Serilog",
            DisplayName = "Verify.Serilog",
            RepoUrl = "https://github.com/VerifyTests/Verify.Serilog",
            Description = "Captures the Serilog events written during a test and includes them in the snapshot under log.",
            Category = ExtensionCategory.Logging,
            Packages =
            [
                new("Verify.Serilog"),
                new("Serilog") {Comment = "the static Log the samples write through"}
            ],
            PluginType = "VerifySerilog",
            Usings = ["Serilog"],
            MinimalSamples =
            [
                new(
                    "Logging",
                    """
                    Recording.Start();

                    var result = Method();

                    return Verify(result);
                    """)
                {
                    Comment =
                    [
                        "Everything logged between Recording.Start() and the verification is added to the snapshot",
                        "under the name log, so the assertion covers what the code logged as well as what it",
                        "returned. Nothing has to be passed to Verify for the log entries to appear."
                    ],
                    Members =
                    [
                        """
                        // Stands in for the code under test. It logs through the global Serilog Log, which
                        // Verify.Serilog has pointed at its own sink.
                        static string Method()
                        {
                            Log.Error("The Message");
                            return "Result";
                        }
                        """
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "StructuredProperties",
                    """
                    Recording.Start();

                    Log.Information("Hello {Name}, you are {Age}", "World", 42);

                    return Verify("result");
                    """)
                {
                    Comment =
                    [
                        "Message template properties are kept as structured values rather than being flattened into",
                        "the rendered message, so a changed property is a one line diff."
                    ]
                },
                new(
                    "StopRecording",
                    """
                    Recording.Start();

                    Log.Warning("The warning");

                    var entries = Recording.Stop();
                    return Verify(entries);
                    """)
                {
                    Comment =
                    [
                        "Recording.Stop() returns the entries instead of adding them, so they can be filtered or",
                        "combined with other values before being verified."
                    ]
                }
            ],
            Notes =
            [
                "`Initialize()` replaces the process wide `Log.Logger` with a logger writing to Verify's sink, so a test setup that configures Serilog itself is overwritten.",
                "Anything logged outside a recording throws, so `Recording.Start()` has to run before the code under test logs. Logging during host startup is the usual trap.",
                "Entries land in the shared recording under `log`, which is the name every logging extension uses, so `Recording.IgnoreNames` cannot tell them apart.",
                "`VerifySerilog.IgnoreSourceContext<T>()` and its string overload, in the module initializer, drop events whose `SourceContext` matches a known type.",
                "`Initialize` takes an optional `Action<LoggerConfiguration>` for destructuring policies, enrichers and filters. Using it means calling `Initialize` explicitly, before `InitializePlugins()`.",
                "The callback runs after `MinimumLevel.Verbose`, `Enrich.FromLogContext` and the sink are wired up, so it layers on top of those defaults."
            ]
        }
    ];
}
