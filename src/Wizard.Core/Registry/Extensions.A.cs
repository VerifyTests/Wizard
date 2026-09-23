namespace Wizard.Core;

public static partial class Extensions
{
    /// <summary>Entries researched in plan-research/extension-catalogue-A.md.</summary>
    static IReadOnlyList<ExtensionDefinition> CatalogueA =>
    [
        new()
        {
            Id = "AngleSharp",
            DisplayName = "Verify.AngleSharp",
            RepoUrl = "https://github.com/VerifyTests/Verify.AngleSharp",
            Description = "Compares html, htm and svg snapshots as markup rather than as text, and pretty prints them.",
            Category = ExtensionCategory.Web,
            Packages = [new("Verify.AngleSharp")],
            // The class is VerifyAngleSharpDiffing, so plugin discovery, which looks for
            // VerifyTests.VerifyAngleSharp, never finds it (plan A1).
            PluginType = "VerifyAngleSharpDiffing",
            RetiredBy = "U2",
            Initialize =
            [
                new(
                    "VerifyAngleSharpDiffing.Initialize();",
                    "Verify.AngleSharp: compares html, htm and svg by parsing both sides and diffing the DOM, so",
                    "reordered attributes, reformatted markup and whitespace no longer fail a test.",
                    "The call is explicit because the class is named VerifyAngleSharpDiffing, and plugin discovery",
                    "looks for a type named after the assembly, VerifyAngleSharp, so it never finds this one.")
                {
                    Alternatives =
                    [
                        "VerifyAngleSharpDiffing.Initialize(_ => _.AddDefaultOptions().AddFilter(SpanFilter));"
                    ]
                }
            ],
            // AngleSharp.Diffing.Core holds FilterDecision and ComparisonSource, which a filter needs,
            // and AngleSharp.Diffing holds AddDefaultOptions, an extension method on the collection.
            Usings = ["AngleSharp.Diffing", "AngleSharp.Diffing.Core", "VerifyTests.AngleSharp"],
            MinimalSamples =
            [
                new(
                    "Html",
                    """"
                    var html =
                        """
                        <!DOCTYPE html>
                        <html>
                          <body>
                            <h1>My First Heading</h1>
                            <p>My first paragraph.</p>
                          </body>
                        </html>
                        """;
                    return Verify(html, "html");
                    """")
                {
                    Comment =
                    [
                        "The extension passed to Verify decides the snapshot's file extension, and so which",
                        "comparer runs. This one is compared as a DOM, not as text."
                    ]
                },
                new(
                    "PrettyPrint",
                    """
                    var html = "<html><body><h1>My First Heading</h1><p>My first paragraph.</p></body></html>";
                    return Verify(html, "html")
                        .PrettyPrintHtml();
                    """)
                {
                    Comment =
                    [
                        "Rendered html usually arrives as one long line, which makes a snapshot diff unreadable.",
                        "PrettyPrintHtml reformats it before it is written."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "IgnoreElements",
                    """
                    var html = "<div><span>ignored</span><p>kept</p></div>";
                    var settings = new VerifySettings();
                    settings.AngleSharpDiffingSettings(
                        _ =>
                        {
                            var options = _.AddDefaultOptions();
                            options.AddFilter(SpanFilter);
                        });
                    return Verify(html, "html", settings);
                    """)
                {
                    Comment =
                    [
                        "A filter drops nodes from the comparison without removing them from the snapshot, for",
                        "parts of a page that change on every render."
                    ],
                    Members =
                    [
                        """
                        static FilterDecision SpanFilter(in ComparisonSource source, FilterDecision decision)
                        {
                            if (source.Node.NodeName == "SPAN")
                            {
                                return FilterDecision.Exclude;
                            }

                            return decision;
                        }
                        """
                    ]
                },
                new(
                    "ScrubAttributes",
                    """
                    var html = "<div id=\"generated-4821\"><p class=\"kept\">text</p></div>";
                    return Verify(html, "html")
                        .PrettyPrintHtml(_ => _.ScrubAttributes("id"));
                    """)
                {
                    Comment =
                    [
                        "ScrubAttributes removes an attribute whose value changes per run. Overloads take a",
                        "predicate, or a function returning a replacement value instead of removing it."
                    ]
                },
                new(
                    "ScrubEmptyDivs",
                    """
                    var html = "<div><div></div><div><p>text</p></div></div>";
                    return Verify(html, "html")
                        .PrettyPrintHtml(_ => _.ScrubEmptyDivs());
                    """)
                {
                    Comment =
                    [
                        "Component frameworks emit wrapper divs that carry nothing. ScrubEmptyDivs removes the",
                        "empty ones and unwraps those with a single child and no attributes."
                    ]
                }
            ],
            Notes =
            [
                "This extension compares and scrubs; it never produces a new snapshot target of its own.",
                "Pretty printing and semantic comparison are independent: either can be used without the other.",
                "`HtmlPrettyPrint.All()` applies pretty printing to every html snapshot in the project, instead of per test."
            ]
        },
        new()
        {
            Id = "AspNetCore",
            DisplayName = "Verify.AspNetCore",
            RepoUrl = "https://github.com/VerifyTests/Verify.AspNetCore",
            Description = "Snapshots ASP.NET Core types: HttpContext, requests, responses, headers, cookies and every ActionResult.",
            Category = ExtensionCategory.Web,
            // The package declares the FrameworkReference itself, so the project needs nothing (plan A10).
            Packages = [new("Verify.AspNetCore")],
            PluginType = "VerifyAspNetCore",
            Initialize = [new("VerifyAspNetCore.Initialize();", "Verify.AspNetCore: writes ASP.NET Core types as readable snapshots instead of as their raw object graphs.")],
            LibraryFiles =
            [
                new(
                    "MyController.cs",
                    """
                    using Microsoft.AspNetCore.Mvc;

                    // Stands in for a real controller. MyControllerTests snapshots what its action returns.
                    public class MyController :
                        Controller
                    {
                        public ActionResult<string> Method(string input) =>
                            Ok($"received {input}");
                    }
                    """)
            ],
            // The sample controller lives in the class library, which otherwise knows nothing about
            // ASP.NET Core, so the reference is needed there as well as in the tests.
            ProjectItems = ["<FrameworkReference Include=\"Microsoft.AspNetCore.App\" />"],
            LibraryProjectItems = ["<FrameworkReference Include=\"Microsoft.AspNetCore.App\" />"],
            Usings = ["Microsoft.AspNetCore.Http", "Microsoft.AspNetCore.Mvc"],
            MinimalSamples =
            [
                new(
                    "Controller",
                    """
                    var context = new ControllerContext
                    {
                        HttpContext = new DefaultHttpContext()
                    };
                    var controller = new MyController
                    {
                        ControllerContext = context
                    };

                    var result = controller.Method("inputValue");
                    return Verify(
                        new
                        {
                            result,
                            context
                        });
                    """)
                {
                    Comment =
                    [
                        "Verifying the result and the context together shows both what the action returned and",
                        "what it did to the response: status code, headers and cookies."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "ScrubResponse",
                    """
                    var context = new DefaultHttpContext();
                    context.Response.Headers.Append("Set-Cookie", "session=abc123");
                    return Verify(context.Response)
                        .ScrubAspTextResponse(_ => _.Replace("abc123", "{session}"));
                    """)
                {
                    Comment =
                    [
                        "ScrubAspTextResponse edits the response body text before it is written, for values that",
                        "change on every run."
                    ]
                }
            ],
            Notes = ["The package targets net10.0 only."]
        },
        new()
        {
            Id = "Aspose",
            DisplayName = "Verify.Aspose",
            RepoUrl = "https://github.com/VerifyTests/Verify.Aspose",
            Description = "Converts pdf, docx, xlsx and pptx documents to a png per page, plus a metadata and text snapshot.",
            Category = ExtensionCategory.Documents,
            Packages = [new("Verify.Aspose")],
            PluginType = "VerifyAspose",
            ExternalRequirements =
            [
                new(
                    "An Aspose licence",
                    "Aspose is commercial. Without a licence its renderers watermark the output and cap the page count, so no snapshot matches.")
                {
                    Url = "https://purchase.aspose.com/policies/license-types",
                    EnvironmentVariable = "AsposeLicense",
                    CannotRunUnattended = true
                }
            ],
            ExclusiveGroups = ["pdf-converter", "xlsx-converter", "docx-converter", "pptx-converter"],
            Usings = ["Aspose.Cells", "VerifyTestsAspose"],
            MinimalSamples =
            [
                new(
                    "Pdf",
                    """
                    return VerifyFile("sample.pdf");
                    """)
                {
                    Comment =
                    [
                        "A pdf becomes one png per page plus a metadata snapshot holding the document properties,",
                        "the fonts and the extracted text, so a layout change and a text change are separate diffs."
                    ],
                    SkipReason = "needs an Aspose licence in the AsposeLicense environment variable, and a sample.pdf copied to the test output directory."
                },
                new(
                    "Workbook",
                    """
                    var book = new Workbook
                    {
                        BuiltInDocumentProperties =
                        {
                            Comments = "the comments"
                        }
                    };
                    var sheet = book.Worksheets.Add("New Sheet");
                    sheet.Cells[0, 0].PutValue("Some Text");
                    return Verify(book);
                    """)
                {
                    Comment =
                    [
                        "Aspose types are verified directly, without going through a file, so a document built in",
                        "code is snapshotted as it is: one png per sheet and the properties as text."
                    ],
                    SkipReason = "needs an Aspose licence in the AsposeLicense environment variable."
                }
            ],
            VerboseSamples =
            [
                new(
                    "PagesToInclude",
                    """
                    var settings = new VerifySettings();
                    VerifyAsposeSettings.PagesToInclude(settings, 1);
                    return VerifyFile("sample.pdf", settings);
                    """)
                {
                    Comment =
                    [
                        "Rendering every page of a long document is slow and makes a noisy diff. This caps the png",
                        "snapshots at the first pages; the binary and metadata targets are unaffected.",
                        "The static form is used because several document extensions define PagesToInclude as an",
                        "extension method, and referencing two of them makes the fluent call ambiguous."
                    ],
                    SkipReason = "needs an Aspose licence in the AsposeLicense environment variable, and a sample.pdf copied to the test output directory."
                },
                new(
                    "ExcludeXlsx",
                    """
                    return VerifyFile("sample.xlsx")
                        .ExcludeTargets("xlsx");
                    """)
                {
                    Comment =
                    [
                        "Rebuilding the workbook deterministically is the expensive part of an xlsx verification.",
                        "Dropping that target keeps the csv and metadata snapshots, which is usually what changed."
                    ],
                    SkipReason = "needs an Aspose licence in the AsposeLicense environment variable, and a sample.xlsx copied to the test output directory."
                }
            ],
            Notes =
            [
                "The licence is applied in code before the first verification, once per Aspose product in use: `Aspose.Pdf.License`, `Aspose.Cells.License`, `Aspose.Words.License` and `Aspose.Slides.License`.",
                "The settings methods live in the `VerifyTestsAspose` namespace rather than `VerifyTests`, so the using above is needed.",
                "One verification writes several files: metadata as text, a png per page or slide, and the deterministic binary document.",
                "`VerifierSettings.ExcludeTargets(\"xlsx\")` applies the same exclusion to every test instead of one.",
                "The file based samples need `sample.pdf` and `sample.xlsx` in the test project, copied to the output directory."
            ]
        },
        new()
        {
            Id = "Assertions",
            DisplayName = "Verify.Assertions",
            RepoUrl = "https://github.com/VerifyTests/Verify.Assertions",
            Description = "Runs assertions against objects as they are serialized, so a value nested in a large graph can be checked mid-verification.",
            Category = ExtensionCategory.Testing,
            Packages = [new("Verify.Assertions")],
            PluginType = "VerifyAssertions",
            MinimalSamples =
            [
                new(
                    "AssertNested",
                    """
                    var person = ClassBeingTested.FindPerson();
                    return Verify(person)
                        .Assert<Address>(_ => ArgumentException.ThrowIfNullOrWhiteSpace(_.Country));
                    """)
                {
                    Comment =
                    [
                        "The callback runs while the graph is serialized, so a value buried in it is asserted on",
                        "without being dug out first, and the snapshot still covers everything else.",
                        "Any assertion library works here; a BCL throw helper keeps the sample independent of the",
                        "test framework."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "AssertOnSettings",
                    """
                    var person = ClassBeingTested.FindPerson();
                    var settings = new VerifySettings();
                    settings.Assert<Person>(_ => ArgumentException.ThrowIfNullOrWhiteSpace(_.FamilyName));
                    return Verify(person, settings);
                    """)
                {
                    Comment =
                    [
                        "The same assertion on a VerifySettings instance, which accumulates a list, so settings",
                        "shared by several tests can carry the assertions they all need."
                    ]
                }
            ],
            Notes =
            [
                "`VerifyAssertions.Assert<T>(...)` in the module initializer applies an assertion to every verification in the project.",
                "An assertion for a type that is never serialized never runs and never fails, so a wrong type argument passes silently.",
                "The package is pre 1.0, so its api can still change."
            ]
        },
        new()
        {
            Id = "Avalonia",
            DisplayName = "Verify.Avalonia",
            RepoUrl = "https://github.com/VerifyTests/Verify.Avalonia",
            Description = "Renders an Avalonia control headlessly to png, alongside a text snapshot of its visual tree.",
            Category = ExtensionCategory.Ui,
            Packages =
            [
                new("Verify.Avalonia"),
                new("Avalonia.Headless.XUnit")
                {
                    TestFrameworks = [TestFramework.XunitV3],
                    Comment = "the [AvaloniaFact] attribute the samples need"
                },
                new("Avalonia.Headless.NUnit")
                {
                    TestFrameworks = [TestFramework.NUnit],
                    Comment = "the [AvaloniaTest] attribute the samples need"
                },
                new("Avalonia.Themes.Fluent") {Comment = "the theme the rendered control is styled with"},
                new("Avalonia.Skia") {Comment = "the renderer; headless drawing on its own produces a blank image"}
            ],
            PluginType = "VerifyAvalonia",
            UnsupportedTestFrameworks =
            [
                (TestFramework.TUnit, "Avalonia.Headless ships test attributes for xUnit and NUnit only, and a render has to run on the Avalonia ui thread."),
                (TestFramework.MSTest, "Avalonia.Headless ships test attributes for xUnit and NUnit only, and a render has to run on the Avalonia ui thread."),
                (TestFramework.Fixie, "Avalonia.Headless ships test attributes for xUnit and NUnit only, and a render has to run on the Avalonia ui thread.")
            ],
            Usings = ["Avalonia.Controls"],
            MinimalSamples =
            [
                new(
                    "UserControl",
                    """
                    var control = new UserControl
                    {
                        Width = 200,
                        Height = 100,
                        Content = new TextBlock
                        {
                            Text = "Welcome to Avalonia!"
                        }
                    };
                    return Verify(control);
                    """)
                {
                    Comment =
                    [
                        "A control is verified twice over: a png of what it renders, and text holding its visual",
                        "tree, so a binding that stopped producing text shows up as a readable diff."
                    ],
                    SkipReason = "an Avalonia render needs the method attributed AvaloniaFact (xUnit) or AvaloniaTest (NUnit), and the test assembly needs an AvaloniaTestApplication attribute naming an AppBuilder that calls UseSkia and sets UseHeadlessDrawing to false."
                }
            ],
            VerboseSamples =
            [
                new(
                    "ControlTree",
                    """
                    var control = new UserControl
                    {
                        Content = new StackPanel
                        {
                            Spacing = 10,
                            Children =
                            {
                                new TextBlock
                                {
                                    Text = "Welcome to Avalonia!"
                                },
                                new Button
                                {
                                    Content = "Press me"
                                }
                            }
                        }
                    };
                    return Verify(control);
                    """)
                {
                    Comment =
                    [
                        "The text snapshot lists each child with the properties that differ from their defaults,",
                        "so a layout change is legible even when the two pngs look the same."
                    ],
                    SkipReason = "an Avalonia render needs the method attributed AvaloniaFact (xUnit) or AvaloniaTest (NUnit), and the test assembly needs an AvaloniaTestApplication attribute naming an AppBuilder that calls UseSkia and sets UseHeadlessDrawing to false."
                }
            ],
            Notes =
            [
                "The test project needs `[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]` once, and a builder that calls `.UseSkia()` and `UseHeadless(new() { UseHeadlessDrawing = false })`.",
                "Avalonia's headless test host needs the test project to be an exe, which both xUnit and NUnit already set here.",
                "`VerifierSettings.UseSsimForPng()` stops small rendering differences between operating systems from failing a test.",
                "`VerifyAvalonia.IncludeThemeVariant()` in the module initializer renders every test light and dark, as `#light` and `#dark` snapshots.",
                "`VerifyAvalonia.AddAvaloniaConvertersForAssemblyOfType<T>()` registers converters for the controls of a third party library.",
                "An app project exposing controls to the tests needs an `InternalsVisibleTo` for the test assembly."
            ]
        },
        new()
        {
            Id = "Blazor",
            DisplayName = "Verify.Blazor",
            RepoUrl = "https://github.com/VerifyTests/Verify.Blazor",
            Description = "Renders a Blazor component with the raw Blazor apis and snapshots both its html and its state.",
            Category = ExtensionCategory.Web,
            Packages =
            [
                new("Verify.Blazor"),
                new("Microsoft.AspNetCore.Components")
                {
                    ForLibrary = true,
                    Comment = "the ComponentBase the sample component derives from"
                }
            ],
            // VerifyBlazor is internal, so neither plugin discovery nor an explicit call can reach it.
            // Render's static constructor is the only thing that initializes it (plan A2).
            PluginType = "VerifyBlazor",
            PluginTypeIsInternal = true,
            RetiredBy = "U1",
            Initialize =
            [
                new(
                    "RuntimeHelpers.RunClassConstructor(typeof(Render).TypeHandle);",
                    "Verify.Blazor: the plugin is initialized by the static constructor of Render, and initializing",
                    "it throws once any verification has run. Forcing the constructor here means the outcome does",
                    "not depend on whether a Blazor test happens to run before the others.",
                    "There is nothing to call instead: VerifyBlazor is internal, so plugin discovery skips it too.")
            ],
            InitializeUsings = ["VerifyTests.Blazor"],
            ExclusiveGroups = ["blazor-renderer"],
            Usings = ["Microsoft.AspNetCore.Components", "VerifyTests.Blazor"],
            LibraryFiles =
            [
                new(
                    "BlazorComponent.cs",
                    """
                    using Microsoft.AspNetCore.Components;
                    using Microsoft.AspNetCore.Components.Rendering;

                    // Stands in for a real .razor component. BlazorTests renders it and snapshots the html.
                    public class BlazorComponent :
                        ComponentBase
                    {
                        [Parameter]
                        public string Title { get; set; } = "";

                        [Parameter]
                        public string Name { get; set; } = "";

                        protected override void BuildRenderTree(RenderTreeBuilder builder)
                        {
                            builder.OpenElement(0, "div");
                            builder.OpenElement(1, "h1");
                            builder.AddContent(2, Title);
                            builder.CloseElement();
                            builder.OpenElement(3, "p");
                            builder.AddContent(4, Name);
                            builder.CloseElement();
                            builder.CloseElement();
                        }
                    }
                    """)
            ],
            MinimalSamples =
            [
                new(
                    "Parameters",
                    """
                    var parameters = ParameterView.FromDictionary(
                        new Dictionary<string, object?>
                        {
                            {
                                "Title", "The Title"
                            },
                            {
                                "Name", "Sam"
                            }
                        });

                    var target = Render.Component<BlazorComponent>(parameters: parameters);

                    return Verify(target);
                    """)
                {
                    Comment =
                    [
                        "Rendering writes two snapshots: the html the component produced, and the component state",
                        "as text, so a property that stopped being set is visible even when the markup is the same."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "Template",
                    """
                    var template = new BlazorComponent
                    {
                        Title = "The Title",
                        Name = "Sam"
                    };

                    var target = Render.Component(template: template);

                    return Verify(target);
                    """)
                {
                    Comment =
                    [
                        "Setting the properties on an instance is easier to read than a parameter dictionary, and",
                        "the values on the template win over anything passed as parameters."
                    ]
                }
            ],
            Notes =
            [
                "`VerifierSettings.InitializePlugins()` cannot enable this extension: its plugin type is internal, which is why the module initializer forces `Render`'s static constructor instead.",
                "Rendered html carries Blazor's comment markers; `BlazorScrubber.ScrubCommentLines()` drops them, and Verify.AngleSharp's `HtmlPrettyPrint.All()` turns the rest into a readable tree.",
                "`Render.Component` also takes a service provider, a logger factory and a callback that runs before the state is captured.",
                "The package suppresses BL0005 for the project that references it, so setting component properties directly does not warn."
            ]
        },
        new()
        {
            Id = "Brighter",
            DisplayName = "Verify.Brighter",
            RepoUrl = "https://github.com/VerifyTests/Verify.Brighter",
            Description = "A recording command processor for Brighter, so what a handler sent, published or deposited can be verified.",
            Category = ExtensionCategory.Messaging,
            Packages =
            [
                new("Verify.Brighter"),
                new("Paramore.Brighter")
                {
                    ForLibrary = true,
                    Comment = "the command and the handler the samples exercise"
                }
            ],
            PluginType = "VerifyBrighter",
            RetiredBy = "U4",
            Usings = ["VerifyTests.Brighter"],
            LibraryFiles =
            [
                new(
                    "BrighterHandler.cs",
                    """
                    using Paramore.Brighter;

                    // Stands in for real Brighter code. The handler takes the command processor by interface,
                    // so a test can hand it the recording one instead of a configured pipeline.
                    public class BrighterHandler(IAmACommandProcessor processor)
                    {
                        public Task Handle(BrighterCommand command) =>
                            processor.SendAsync(command);
                    }

                    public class BrighterCommand(string value) :
                        Command(Id.Random())
                    {
                        public string Value { get; } = value;
                    }
                    """)
            ],
            MinimalSamples =
            [
                new(
                    "Handler",
                    """
                    var processor = new RecordingCommandProcessor();
                    var handler = new BrighterHandler(processor);

                    await handler.Handle(new("the value"));

                    await Verify(processor);
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "The recording processor is a test double rather than an ambient recording: it is handed to",
                        "the code under test, and verifying it covers every request that code sent or published."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "Sends",
                    """
                    var processor = new RecordingCommandProcessor();
                    var handler = new BrighterHandler(processor);

                    await handler.Handle(new("the value"));

                    await Verify(processor.Sends);
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "Sends, Publishes, Deposits and Calls each expose one kind of call, for a test that is only",
                        "about one of them, or that wants to filter before verifying."
                    ]
                }
            ],
            Notes =
            [
                "`Post` and `PostAsync` are recorded under the `Publish` key, so the samples stay with `Send` until that is fixed.",
                "`Call` always returns null: the recording processor records the call rather than answering it.",
                "`VerifyBrighter.Initialize()` only adds converters, so it is safe to call after a verification has already run."
            ]
        },
        new()
        {
            Id = "Bunit",
            DisplayName = "Verify.Bunit",
            RepoUrl = "https://github.com/VerifyTests/Verify.Bunit",
            Description = "Snapshots a bUnit rendered Blazor component, with bUnit's full interaction api available first.",
            Category = ExtensionCategory.Web,
            Packages =
            [
                new("Verify.Bunit"),
                new("bunit") {Comment = "the BunitContext the samples render through"},
                new("Microsoft.AspNetCore.Components")
                {
                    ForLibrary = true,
                    Comment = "the ComponentBase the sample component derives from"
                }
            ],
            PluginType = "VerifyBunit",
            Initialize =
            [
                new(
                    "VerifyBunit.Initialize(excludeComponent: {bunit-component-state});",
                    "Verify.Bunit: renders a component through bUnit and writes its markup as an html snapshot.",
                    "excludeComponent decides whether the component's own state is written alongside the markup.")
            ],
            ExclusiveGroups = ["blazor-renderer"],
            Usings = ["Bunit"],
            Choices =
            [
                new(
                    "bunit-component-state",
                    "Component state snapshot",
                    "Whether a rendered component writes its state as well as its markup.",
                    [
                        new("false", "Markup and state", "An html file for the markup and a text file holding the component's properties."),
                        new("true", "Markup only", "Sets excludeComponent: true, so only the html file is written.")
                    ])
            ],
            LibraryFiles =
            [
                new(
                    "BunitComponent.cs",
                    """
                    using Microsoft.AspNetCore.Components;
                    using Microsoft.AspNetCore.Components.Rendering;

                    // Stands in for a real .razor component. BunitTests renders it and snapshots the markup.
                    public class BunitComponent :
                        ComponentBase
                    {
                        [Parameter]
                        public string Title { get; set; } = "";

                        [Parameter]
                        public string Name { get; set; } = "";

                        protected override void BuildRenderTree(RenderTreeBuilder builder)
                        {
                            builder.OpenElement(0, "div");
                            builder.OpenElement(1, "h1");
                            builder.AddContent(2, Title);
                            builder.CloseElement();
                            builder.OpenElement(3, "p");
                            builder.AddContent(4, Name);
                            builder.CloseElement();
                            builder.CloseElement();
                        }
                    }
                    """)
            ],
            MinimalSamples =
            [
                new(
                    "Component",
                    """
                    using var context = new BunitContext();
                    var component = context.Render<BunitComponent>(
                        _ =>
                        {
                            _.Add(parameter => parameter.Title, "The Title");
                            _.Add(parameter => parameter.Name, "Sam");
                        });
                    return Verify(component);
                    """)
                {
                    Comment =
                    [
                        "The parameters are set through bUnit's builder, so they are checked against the component's",
                        "properties at compile time rather than matched by name at run time."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "Nodes",
                    """
                    using var context = new BunitContext();
                    var component = context.Render<BunitComponent>(
                        _ => _.Add(parameter => parameter.Title, "The Title"));
                    return Verify(component.Nodes);
                    """)
                {
                    Comment =
                    [
                        "Verifying the nodes, or a single node, snapshots only that markup: useful when a test is",
                        "about one part of a page and the rest changes for unrelated reasons."
                    ]
                }
            ],
            Notes =
            [
                "The samples use the bUnit v2 api, `BunitContext` and `context.Render<T>(...)`, not v1's `TestContext` and `RenderComponent`.",
                "Verify.Bunit has no stable release on Verify 33 yet, so a prerelease version is referenced.",
                "`VerifyBunit.Initialize()` registers bUnit's own markup comparer for html, which Verify.AngleSharp also does.",
                "`component.WaitFor(...)` and `context.RenderComponentAndWait(...)` let a test verify a component that renders again after an async call.",
                "The package suppresses BL0005 for the project that references it."
            ]
        },
        new()
        {
            Id = "ClosedXml",
            DisplayName = "Verify.ClosedXml",
            RepoUrl = "https://github.com/VerifyTests/Verify.ClosedXml",
            Description = "Converts an Excel workbook into a metadata snapshot, a csv per sheet, and a deterministic xlsx.",
            Category = ExtensionCategory.Documents,
            Packages =
            [
                new("Verify.ClosedXml"),
                new("ClosedXML") {Comment = "the XLWorkbook the samples build"}
            ],
            PluginType = "VerifyClosedXml",
            ExclusiveGroups = ["xlsx-converter"],
            Usings = ["ClosedXML.Excel"],
            MinimalSamples =
            [
                new(
                    "Workbook",
                    """
                    using var book = new XLWorkbook();

                    var sheet = book.Worksheets.Add("Basic Data");

                    sheet.Cell("A1").Value = "Id";
                    sheet.Cell("B1").Value = "Name";

                    sheet.Cell("A2").Value = 1;
                    sheet.Cell("B2").Value = "John Doe";

                    return Verify(book);
                    """)
                {
                    Comment =
                    [
                        "The csv per sheet is what makes the diff readable: a changed cell is one changed line,",
                        "rather than a different binary file. Formulas are appended to the cell they belong to."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "WorkbookFile",
                    """
                    return VerifyFile("sample.xlsx");
                    """)
                {
                    Comment =
                    [
                        "The same conversion for a workbook that already exists, whether it is a fixture or",
                        "something the code under test wrote."
                    ],
                    SkipReason = "needs a sample.xlsx in the test project, copied to the output directory."
                },
                new(
                    "UniqueForRuntime",
                    """
                    using var book = new XLWorkbook();
                    book.Worksheets.Add("Basic Data");
                    return Verify(book)
                        .UniqueForRuntime();
                    """)
                {
                    Comment =
                    [
                        "Deflate compression differs between runtimes, so the xlsx bytes differ even when the",
                        "content does not. A snapshot per runtime keeps that from failing a test."
                    ]
                }
            ],
            Notes =
            [
                "One verification writes three files or more: the metadata as text, the deterministic xlsx, and a csv per sheet.",
                "Values that Verify scrubs, such as guids and dates, are scrubbed inside the csv cells too.",
                "`VerifyClosedXml.Initialize()` only registers converters, so it is safe to call after a verification has already run."
            ]
        },
        new()
        {
            Id = "CommunityToolkitMvvm",
            DisplayName = "Verify.CommunityToolkit.Mvvm",
            RepoUrl = "https://github.com/VerifyTests/Verify.CommunityToolkit.Mvvm",
            Description = "Serializes a RelayCommand or AsyncRelayCommand as the names of the methods behind it.",
            Category = ExtensionCategory.Ui,
            Packages =
            [
                new("Verify.CommunityToolkit.Mvvm"),
                new("CommunityToolkit.Mvvm") {Comment = "the commands the samples build"}
            ],
            PluginType = "VerifyCommunityToolkitMvvm",
            Usings = ["CommunityToolkit.Mvvm.Input"],
            MinimalSamples =
            [
                new(
                    "RelayCommand",
                    """
                    var command = new RelayCommand(Execute, CanExecute);
                    return Verify(command);
                    """)
                {
                    Comment =
                    [
                        "A command otherwise serializes as the compiler generated delegate behind it, which says",
                        "nothing. This writes the Execute and CanExecute method names instead.",
                        "That makes a view model snapshot show which command was wired to which handler."
                    ],
                    Members =
                    [
                        """
                        static void Execute()
                        {
                        }

                        static bool CanExecute() =>
                            true;
                        """
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "AsyncRelayCommand",
                    """
                    var command = new AsyncRelayCommand(ExecuteAsync, CanExecute);
                    return Verify(command);
                    """)
                {
                    Comment =
                    [
                        "The async command is treated the same way, including when it is typed as the",
                        "IAsyncRelayCommand interface rather than the concrete class."
                    ],
                    Members =
                    [
                        """
                        static Task ExecuteAsync() =>
                            Task.CompletedTask;
                        """
                    ]
                }
            ],
            Notes =
            [
                "The method names are read from the private delegate fields, so a command built from a lambda snapshots as the generated method name.",
                "This is most useful alongside a ui extension: a view model verified on its own otherwise hides what its commands do."
            ]
        },
        new()
        {
            Id = "Cosmos",
            DisplayName = "Verify.Cosmos",
            RepoUrl = "https://github.com/VerifyTests/Verify.Cosmos",
            Description = "Snapshots Azure Cosmos DB responses with diagnostics, ETags and policy metadata left out.",
            Category = ExtensionCategory.Data,
            Packages =
            [
                new("Verify.Cosmos"),
                new("Microsoft.Azure.Cosmos") {Comment = "the client the samples talk to the emulator with"},
                new("Newtonsoft.Json")
                {
                    ForLibrary = true,
                    Comment = "the attribute that maps the document's id property"
                }
            ],
            PluginType = "VerifyCosmos",
            ExternalRequirements =
            [
                new(
                    "A Cosmos DB endpoint",
                    "The samples read the key from a CosmosKey environment variable and talk to https://localhost:8081, where the Azure Cosmos DB Emulator listens.")
                {
                    Url = "https://learn.microsoft.com/azure/cosmos-db/emulator",
                    WindowsInstall = "& 'C:\\Program Files\\Azure Cosmos DB Emulator\\CosmosDB.Emulator.exe' /NoUI /NoExplorer /NoFirewall",
                    CannotRunUnattended = true
                }
            ],
            RetiredBy = "U23",
            Usings = ["Microsoft.Azure.Cosmos", "Microsoft.Azure.Cosmos.Linq"],
            LibraryFiles =
            [
                new(
                    "Family.cs",
                    """
                    using Newtonsoft.Json;

                    // Stands in for a document type of your own. Cosmos needs a lower case id property, which
                    // the attribute maps to, and the client serializes with Newtonsoft.Json by default.
                    public class Family
                    {
                        [JsonProperty("id")]
                        public string Id { get; set; } = null!;

                        public string LastName { get; set; } = null!;
                    }
                    """)
            ],
            MinimalSamples =
            [
                new(
                    "ItemResponse",
                    """
                    var container = GetContainer();
                    var family = new Family
                    {
                        Id = "Andersen",
                        LastName = "Andersen"
                    };

                    var response = await container.CreateItemAsync(family, new PartitionKey(family.LastName));

                    await Verify(response);
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "The response is snapshotted with its status code and the stored document, while the",
                        "diagnostics, the ETag and the policy metadata, which differ on every call, are left out."
                    ],
                    SkipReason = "needs a Cosmos DB account or the Azure Cosmos DB Emulator listening on localhost, with its key in a CosmosKey environment variable.",
                    Members =
                    [
                        """
                        // The emulator's endpoint. The key is read from the environment rather than hard coded.
                        static Container GetContainer()
                        {
                            var client = new CosmosClient(
                                "https://localhost:8081",
                                Environment.GetEnvironmentVariable("CosmosKey")!);
                            return client.GetContainer("SampleDatabase", "Families");
                        }
                        """
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "FeedResponse",
                    """
                    var container = GetContainer();
                    using var iterator = container
                        .GetItemLinqQueryable<Family>()
                        .Where(_ => _.LastName == "Andersen")
                        .ToFeedIterator();

                    var response = await iterator.ReadNextAsync();

                    await Verify(response);
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "A feed response is snapshotted the same way, so a query is covered by what it returned",
                        "rather than by assertions on a count."
                    ],
                    SkipReason = "needs a Cosmos DB account or the Azure Cosmos DB Emulator listening on localhost, with its key in a CosmosKey environment variable."
                }
            ],
            Notes =
            [
                "`Initialize()` ignores the member `ETag` on every type, not only on Cosmos types, so an ETag elsewhere in a snapshot disappears too.",
                "`RequestCharge` stays in the snapshot, rounded to one decimal place, and can change when the service version changes.",
                "Diagnostics, `IndexingPolicy`, `ContainerProperties` and `DatabaseProperties` are ignored with no way to opt back in."
            ]
        },
        new()
        {
            Id = "CsvHelper",
            DisplayName = "Verify.CsvHelper",
            RepoUrl = "https://github.com/VerifyTests/Verify.CsvHelper",
            Description = "Normalizes csv snapshots through CsvHelper, with per column ignore, scrub and translate hooks.",
            Category = ExtensionCategory.Data,
            Packages =
            [
                new("Verify.CsvHelper"),
                new("CsvHelper") {Comment = "the CsvReader one of the samples verifies"}
            ],
            PluginType = "VerifyCsvHelper",
            ExclusiveGroups = ["csv-scrubber"],
            Usings = ["CsvHelper", "System.Globalization"],
            MinimalSamples =
            [
                new(
                    "Csv",
                    """
                    var csv = BuildCsv();
                    return Verify(csv, "csv");
                    """)
                {
                    Comment =
                    [
                        "The csv is read and written again through CsvHelper before it is compared, so quoting and",
                        "escaping are normalized, and guids and dates are replaced with Verify's stable counters."
                    ],
                    Members =
                    [
                        """"
                        // The csv the samples verify. Real code would read a file instead: VerifyFile("sample.csv").
                        static string BuildCsv() =>
                            """
                            Id,Name,Dob
                            1,Sam,2000-01-01T00:00:00Z
                            2,Mary,2001-02-03T00:00:00Z
                            """;
                        """"
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "IgnoreColumns",
                    """
                    var csv = BuildCsv();
                    return Verify(csv, "csv")
                        .IgnoreCsvColumns("Dob");
                    """)
                {
                    Comment =
                    [
                        "The column is dropped from the snapshot entirely, for one that carries nothing worth",
                        "reviewing, such as an export timestamp."
                    ]
                },
                new(
                    "ScrubColumns",
                    """
                    var csv = BuildCsv();
                    return Verify(csv, "csv")
                        .ScrubCsvColumns("Name");
                    """)
                {
                    Comment =
                    [
                        "The column stays, with every value replaced by a placeholder, so the snapshot still shows",
                        "that the column was produced and how many rows had a value."
                    ]
                },
                new(
                    "TranslateColumn",
                    """
                    var csv = BuildCsv();
                    return Verify(csv, "csv")
                        .TranslateCsvColumn(
                            _ =>
                            {
                                if (_ != "Name")
                                {
                                    return null;
                                }

                                return value => value?.ToUpperInvariant();
                            });
                    """)
                {
                    Comment =
                    [
                        "The outer call is asked once per column and returns the transform for that column, or null",
                        "to leave it alone, so only the columns that need normalizing pay for it."
                    ]
                },
                new(
                    "Reader",
                    """
                    var csv = BuildCsv();
                    using var reader = new StringReader(csv);
                    using var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
                    return Verify(csvReader);
                    """)
                {
                    Comment =
                    [
                        "A CsvReader is verified directly, for code that already has one open rather than a string",
                        "or a file."
                    ]
                }
            ],
            Notes =
            [
                "The fluent form on a verification is `TranslateCsvColumn`, singular, while the form on a `VerifySettings` is `TranslateCsvColumns`.",
                "Reading and rewriting the csv normalizes quoting and line endings, so existing verified files can shift once when this is adopted.",
                "The package is pre 1.0, and is not strong named."
            ]
        },
        new()
        {
            Id = "Diagnostics",
            DisplayName = "Verify.Diagnostics",
            RepoUrl = "https://github.com/VerifyTests/Verify.Diagnostics",
            Description = "Records every System.Diagnostics.Activity a test creates and adds them to the snapshot under activity.",
            Category = ExtensionCategory.Observability,
            Packages = [new("Verify.Diagnostics")],
            PluginType = "VerifyDiagnostics",
            ExclusiveGroups = ["activity-listener"],
            RetiredBy = "U10",
            Usings = ["System.Diagnostics"],
            MinimalSamples =
            [
                new(
                    "Activities",
                    """
                    Recording.Start();
                    using var source = new ActivitySource("SampleSource");

                    using (var activity = source.StartActivity("SampleOperation"))
                    {
                        activity!.SetTag("key", "value");
                    }

                    return Verify("the result");
                    """)
                {
                    Comment =
                    [
                        "Every activity started while the recording runs is added to the snapshot under activity,",
                        "so the tracing a piece of code emits is covered without asserting on it one tag at a time.",
                        "Ids, timings and durations are left out, which is what makes the snapshot stable."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "StopRecording",
                    """
                    Recording.Start();
                    using var source = new ActivitySource("SampleSource");

                    using (source.StartActivity("SampleOperation"))
                    {
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
                "The readme calls for `RecordingActivityListener.Start()`, which does not exist: the recording is started with Verify's own `Recording.Start()`.",
                "The listener is process wide and is never removed, so activities from every source in the test process are recorded.",
                "An activity's `DisplayName`, `Kind` and `Status` are only written when they differ from their defaults."
            ]
        },
        new()
        {
            Id = "DiffPlex",
            DisplayName = "Verify.DiffPlex",
            RepoUrl = "https://github.com/VerifyTests/Verify.DiffPlex",
            Description = "Shows an inline diff in the failure message of a text snapshot, instead of both files in full.",
            Category = ExtensionCategory.DeveloperExperience,
            Packages = [new("Verify.DiffPlex")],
            PluginType = "VerifyDiffPlex",
            Phase = InitializePhase.Comparers,
            Initialize =
            [
                new(
                    "VerifyDiffPlex.Initialize(OutputType.{diffplex-output});",
                    "Verify.DiffPlex: when a text snapshot does not match, the failure message shows an inline",
                    "diff instead of the whole received and verified text.",
                    "OutputType.Compact prints only the changed lines, with a line of context either side.",
                    "Alternatives: OutputType.Full, OutputType.Minimal.")
            ],
            InitializeUsings = ["VerifyTests.DiffPlex"],
            Usings = ["VerifyTests.DiffPlex"],
            Choices =
            [
                new(
                    "diffplex-output",
                    "Failure message detail",
                    "How much of the text a failed comparison prints.",
                    [
                        new("Compact", "Compact", "Only changed lines, with one line of context and its line number."),
                        new("Full", "Full", "The whole received text, with + and - markers on the changed lines."),
                        new("Minimal", "Minimal", "Only the changed lines.")
                    ])
            ],
            MinimalSamples =
            [
                new(
                    "PerTestOutput",
                    """
                    var target = "The text";
                    return Verify(target)
                        .UseDiffPlex(OutputType.Full);
                    """)
                {
                    Comment =
                    [
                        "The module initializer sets the mode for every test. UseDiffPlex overrides it for one,",
                        "which is useful when a single snapshot is easier to read in full."
                    ],
                    // Verifying a string writes it verbatim, so the snapshot is known and can ship.
                    // Verify.DiffPlex is selected by default, and a download whose first test run fails
                    // is a poor way to meet a tool.
                    VerifiedOutput = "The text"
                }
            ],
            Notes =
            [
                "This changes only the failure message; what is written to a `.verified.` file is unaffected.",
                "It is the default comparer for text. An extension that registers a comparer for a specific file extension, such as html or json, takes precedence for that extension."
            ]
        },
        new()
        {
            Id = "DocNet",
            DisplayName = "Verify.DocNet",
            RepoUrl = "https://github.com/VerifyTests/Verify.DocNet",
            Description = "Renders a pdf to a png per page, with the text of each page as a metadata snapshot, using pdfium.",
            Category = ExtensionCategory.Documents,
            Packages = [new("Verify.DocNet")],
            PluginType = "VerifyDocNet",
            ExclusiveGroups = ["pdf-converter"],
            MinimalSamples =
            [
                new(
                    "Pdf",
                    """
                    return VerifyFile("sample.pdf");
                    """)
                {
                    Comment =
                    [
                        "A pdf becomes a png per page plus a metadata snapshot with the page count and the text of",
                        "each page, so a wording change and a layout change show up as different files."
                    ],
                    SkipReason = "needs a sample.pdf in the test project, copied to the output directory."
                }
            ],
            VerboseSamples =
            [
                new(
                    "FirstPage",
                    """
                    var stream = File.OpenRead("sample.pdf");
                    return Verify(stream, "pdf")
                        .SinglePage(0);
                    """)
                {
                    Comment =
                    [
                        "Rendering one page of a long document keeps a test fast and its diff small. The index is",
                        "zero based, and the metadata still reports the full page count."
                    ],
                    SkipReason = "needs a sample.pdf in the test project, copied to the output directory."
                },
                new(
                    "PageDimensions",
                    """
                    return VerifyFile("sample.pdf")
                        .PageDimensions(new(1080, 1920));
                    """)
                {
                    Comment =
                    [
                        "The render size is fixed here rather than taken from the page, which is what makes the png",
                        "comparable between machines with different defaults."
                    ],
                    SkipReason = "needs a sample.pdf in the test project, copied to the output directory."
                },
                new(
                    "PreserveTransparency",
                    """
                    return VerifyFile("sample.pdf")
                        .PreserveTransparency();
                    """)
                {
                    Comment =
                    [
                        "The alpha channel is kept instead of being flattened onto white, for a document meant to",
                        "be drawn over something else."
                    ],
                    SkipReason = "needs a sample.pdf in the test project, copied to the output directory."
                }
            ],
            Notes =
            [
                "pdfium renders slightly differently on each operating system, so `VerifierSettings.UseSsimForPng(0.95)` is the recommended comparison for these snapshots.",
                "The rendering is done by native pdfium binaries that arrive with the package, one per runtime identifier.",
                "`PagesToInclude(count)` and `SkipPdfNormalization()` exist but are not in the readme; toggling normalization rewrites existing verified pdfs once.",
                "With a second pdf extension referenced, `PagesToInclude` and `SkipPdfNormalization` are ambiguous at compile time and have to be called in their static form.",
                "The samples need a `sample.pdf` in the test project, copied to the output directory."
            ]
        },
        new()
        {
            Id = "EmailPreviewServices",
            DisplayName = "Verify.EmailPreviewServices",
            RepoUrl = "https://github.com/VerifyTests/Verify.EmailPreviewServices",
            Description = "Renders an html email in real email clients through a preview service, one scrubbed webp per device.",
            Category = ExtensionCategory.Email,
            Packages = [new("Verify.EmailPreviewServices")],
            PluginType = "VerifyEmailPreviewServices",
            ExternalRequirements =
            [
                new(
                    "An EmailPreviewServices api key",
                    "EmailPreviewServices is a paid service. The key is read from the EmailPreviewServicesApiKey environment variable, or passed to Initialize, and the rendering happens over the network.")
                {
                    Url = "https://emailpreviewservices.com/en/pricing",
                    EnvironmentVariable = "EmailPreviewServicesApiKey",
                    CannotRunUnattended = true
                }
            ],
            Usings = ["VerifyTests.EmailPreviewServices"],
            MinimalSamples =
            [
                new(
                    "Preview",
                    """
                    var preview = new EmailPreview
                    {
                        Html = "<html><body><h1>Hello from Verify</h1></body></html>",
                        Devices =
                        [
                            Device.Outlook2019,
                            Device.iPhone13
                        ]
                    };

                    await Verify(preview);
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "Each device gives one webp snapshot, named after it, so a change to the email is reviewed",
                        "as what each client actually shows rather than as html.",
                        "The client's own chrome is cropped out per device, so a browser update does not fail a test."
                    ],
                    SkipReason = "calls a paid service over the network and takes about twenty seconds per device."
                }
            ],
            Notes =
            [
                "The key can be passed directly with `VerifyEmailPreviewServices.Initialize(\"ApiKey\")` instead of the environment variable; without either, initialization throws.",
                "Rendering is slow: roughly twenty seconds for one device, and the call throws after six minutes.",
                "The snapshots are webp, so `VerifierSettings.UseSsimForPng()` does not apply to them.",
                "Listing the same device twice throws, and each preview is deleted from the service once it has been fetched.",
                "Around fifty devices are available, from Outlook 2003 to current mobile clients, light and dark."
            ]
        },
        new()
        {
            Id = "EntityFramework",
            DisplayName = "Verify.EntityFramework (EF Core)",
            RepoUrl = "https://github.com/VerifyTests/Verify.EntityFramework",
            Description = "Records the SQL EF Core runs, snapshots ChangeTracker state, and turns an IQueryable into readable SQL.",
            Category = ExtensionCategory.Data,
            Packages =
            [
                new("Verify.EntityFramework"),
                new("Microsoft.EntityFrameworkCore.SqlServer")
                {
                    ForLibrary = true,
                    Comment = "the provider the sample DbContext uses"
                }
            ],
            PluginType = "VerifyEntityFramework",
            Initialize =
            [
                new(
                    "VerifyEntityFramework.Initialize(GetDbModel());",
                    "Verify.EntityFramework: the model is read once here so IgnoreNavigationProperties() can be",
                    "called without arguments, and so navigation properties stay out of entity snapshots.")
            ],
            InitializeMembers =
            [
                """
                // Builds a context against a connection string that is never opened, purely to read the model.
                static IModel GetDbModel()
                {
                    var options = new DbContextOptionsBuilder<SampleDbContext>();
                    options.UseSqlServer("fake");
                    using var data = new SampleDbContext(options.Options);
                    return data.Model;
                }
                """
            ],
            InitializeUsings = ["Microsoft.EntityFrameworkCore", "Microsoft.EntityFrameworkCore.Metadata"],
            Usings = ["Microsoft.EntityFrameworkCore"],
            LibraryFiles =
            [
                new(
                    "SampleDbContext.cs",
                    """
                    using Microsoft.EntityFrameworkCore;

                    // Stands in for a real DbContext. The samples record the SQL EF Core produces for it.
                    public class SampleDbContext(DbContextOptions<SampleDbContext> options) :
                        DbContext(options)
                    {
                        public DbSet<Company> Companies { get; set; } = null!;
                        public DbSet<Employee> Employees { get; set; } = null!;
                    }

                    public class Company
                    {
                        public int Id { get; set; }
                        public string Name { get; set; } = null!;
                        public IList<Employee> Employees { get; set; } = [];
                    }

                    public class Employee
                    {
                        public int Id { get; set; }
                        public int CompanyId { get; set; }
                        public Company Company { get; set; } = null!;
                        public string Name { get; set; } = null!;
                    }
                    """)
            ],
            Choices =
            [
                new(
                    "ef-sql-format",
                    "SQL formatting",
                    "Verify reformats SQL Server SQL before writing it, so a snapshot reads the same however EF emitted it.",
                    [
                        new("true", "Reformat the SQL", "Indented and keyword aligned."),
                        new("false", "Write it verbatim", "Sets VerifyEntityFramework.DisableSqlFormatting.")
                    ])
            ],
            MinimalSamples =
            [
                new(
                    "RecordCommands",
                    """
                    await using var data = NewContext();
                    Recording.Start();

                    await data
                        .Companies
                        .Where(_ => _.Name == "Title")
                        .ToListAsync();

                    await Verify();
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "Everything EF runs between Recording.Start() and the verification is added to the",
                        "snapshot under the name ef, with the command text, its type and the transaction state.",
                        "Nothing needs to be passed to Verify: recorded entries are included on their own."
                    ],
                    Members =
                    [
                        """
                        // EnableRecording attaches the interceptor, and belongs only in test code.
                        static SampleDbContext NewContext()
                        {
                            var builder = new DbContextOptionsBuilder<SampleDbContext>();
                            builder.UseSqlServer("Server=(local);Database=Sample;Integrated Security=true;Encrypt=false");
                            builder.EnableRecording();
                            return new(builder.Options);
                        }
                        """
                    ]
                },
                new(
                    "Queryable",
                    """
                    await using var data = NewContext();
                    var queryable = data
                        .Companies
                        .Where(_ => _.Name == "Title");
                    await Verify(queryable);
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "Verifying an IQueryable writes two snapshots: a .verified.txt with the materialized",
                        "results, and a .verified.sql with the SQL EF would run. The query is not executed for the",
                        "SQL file, so a broken query still shows up as a diff rather than as an exception."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "ChangeTracker",
                    """
                    await using var data = NewContext();
                    var company = new Company
                    {
                        Name = "new name"
                    };
                    data.Add(company);
                    await Verify(data.ChangeTracker);
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "A ChangeTracker snapshot lists the added, modified and deleted entities, and for a",
                        "modified one both the original and the current value of each changed property."
                    ]
                },
                new(
                    "IgnoreNavigationProperties",
                    """
                    var employee = new Employee
                    {
                        Name = "John"
                    };
                    return Verify(employee)
                        .IgnoreNavigationProperties();
                    """)
                {
                    Comment =
                    [
                        "Navigation properties make an entity snapshot recursive and noisy. This drops them,",
                        "using the model captured in the module initializer."
                    ]
                },
                new(
                    "StopRecording",
                    """
                    await using var data = NewContext();
                    Recording.Start();

                    var count = await data.Companies.CountAsync();

                    var entries = Recording.Stop();
                    await Verify(
                        new
                        {
                            count,
                            entries
                        });
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "Recording.Stop() returns the entries instead of adding them, so they can be filtered",
                        "or combined with other values before being verified."
                    ]
                },
                new(
                    "DescriptiveSql",
                    """
                    var builder = new DbContextOptionsBuilder<SampleDbContext>();
                    builder.UseSqlServer("Server=(local);Database=Sample;Integrated Security=true;Encrypt=false");
                    builder.EnableRecording();
                    builder.UseDescriptiveTableAliases();
                    builder.UseDescriptiveParameterNames();
                    await using var data = new SampleDbContext(builder.Options);

                    Recording.Start();
                    await data.Companies.Where(_ => _.Name == "Title").ToListAsync();
                    await Verify();
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "EF names tables c and e, and parameters @p0 and @p1. These two replace them with the",
                        "table and column names, so a change in the query is visible in the diff."
                    ]
                },
                new(
                    "ScrubDateTimes",
                    """
                    await using var data = NewContext();
                    Recording.Start();

                    await data.Companies.ToListAsync();

                    await Verify()
                        .ScrubInlineEfDateTimes();
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "Some queries, such as those against temporal tables, have EF inline a DateTime into the",
                        "SQL. This replaces each one with a stable token."
                    ]
                }
            ],
            Notes =
            [
                "`EnableRecording()` belongs in test code only: it attaches an interceptor to every command.",
                "`Recording.Start()` can be called on several contexts built from the same options, and the entries are aggregated.",
                "Testing through `WebApplicationFactory` needs a named recording, `Recording.Start(name)` and `Recording.Stop(name)`, and the entries have to be passed to Verify explicitly.",
                "The samples point at a local SQL Server. Selecting LocalDb as well generates a database per test instead."
            ]
        },
        new()
        {
            Id = "EntityFrameworkClassic",
            DisplayName = "Verify.EntityFrameworkClassic (EF6)",
            RepoUrl = "https://github.com/VerifyTests/Verify.EntityFramework",
            Description = "The same snapshot testing for EntityFramework 6: the SQL a query produces, and ChangeTracker state.",
            Category = ExtensionCategory.Data,
            Packages =
            [
                new("Verify.EntityFrameworkClassic"),
                new("EntityFramework")
                {
                    ForLibrary = true,
                    Comment = "the EF6 DbContext the samples query"
                }
            ],
            PluginType = "VerifyEntityFrameworkClassic",
            Usings = ["System.Data.Entity"],
            LibraryFiles =
            [
                new(
                    "ClassicSampleDbContext.cs",
                    """
                    using System.Data.Common;
                    using System.Data.Entity;
                    using System.Data.Entity.SqlServer;
                    using System.Data.SqlClient;

                    // Stands in for a real EF6 DbContext. The samples verify the SQL it produces.
                    public class ClassicSampleDbContext :
                        DbContext
                    {
                        public ClassicSampleDbContext() :
                            base("Server=(local);Database=Sample;Integrated Security=true;Encrypt=false")
                        {
                        }

                        public DbSet<ClassicCompany> Companies { get; set; } = null!;
                    }

                    public class ClassicCompany
                    {
                        public int Id { get; set; }
                        public string Name { get; set; } = null!;
                    }

                    // EF6 outside .NET Framework has no app.config to read, so the SQL Server provider is
                    // registered in code. EF6 finds this class because it sits beside the context.
                    public class ClassicDbConfiguration :
                        DbConfiguration
                    {
                        public ClassicDbConfiguration()
                        {
                            DbProviderFactories.RegisterFactory("System.Data.SqlClient", SqlClientFactory.Instance);
                            SetProviderFactory("System.Data.SqlClient", SqlClientFactory.Instance);
                            SetProviderServices("System.Data.SqlClient", SqlProviderServices.Instance);
                        }
                    }
                    """)
            ],
            MinimalSamples =
            [
                new(
                    "Queryable",
                    """
                    using var data = new ClassicSampleDbContext();
                    var queryable = data
                        .Companies
                        .Where(_ => _.Name == "Title");
                    return Verify(queryable);
                    """)
                {
                    Comment =
                    [
                        "Verifying a queryable snapshots the SQL EF6 would run for it. The query is never executed,",
                        "so a change to the model or to the query is a diff rather than a failing database call."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "ChangeTracker",
                    """
                    using var data = new ClassicSampleDbContext();
                    var company = new ClassicCompany
                    {
                        Name = "new name"
                    };
                    data.Companies.Add(company);
                    return Verify(data.ChangeTracker);
                    """)
                {
                    Comment =
                    [
                        "A ChangeTracker snapshot lists the added, modified and deleted entities, which covers what",
                        "a unit of work was about to save without a database being involved."
                    ]
                }
            ],
            Notes =
            [
                "EF6 writes the queryable SQL as a `.verified.txt`, while EF Core writes it as a `.verified.sql`.",
                "`VerifyEntityFrameworkClassic.Initialize()` takes no arguments: the model is only needed by the EF Core package.",
                "The connection string in the sample context points at a local SQL Server; nothing in these two samples opens a connection."
            ]
        }
    ];
}
