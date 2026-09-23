namespace Wizard.Core;

public static partial class Extensions
{
    /// <summary>Entries researched in plan-research/extension-catalogue-B.md.</summary>
    static IReadOnlyList<ExtensionDefinition> CatalogueB =>
    [
        new()
        {
            Id = "FakeItEasy",
            DisplayName = "Verify.FakeItEasy",
            RepoUrl = "https://github.com/VerifyTests/Verify.FakeItEasy",
            Description = "Snapshots FakeItEasy fakes: the calls one received, their arguments, and its FakeManager.",
            Category = ExtensionCategory.Mocking,
            Packages = [new("Verify.FakeItEasy")],
            PluginType = "VerifyFakeItEasy",
            Usings = ["FakeItEasy"],
            LibraryFiles =
            [
                new(
                    "ITarget.cs",
                    """
                    // Stands in for a real dependency. The mocking samples record the calls made to it.
                    public interface ITarget
                    {
                        string Method(int a, int b);
                    }
                    """)
            ],
            MinimalSamples =
            [
                new(
                    "ReceivedCalls",
                    """
                    var target = A.Fake<ITarget>();
                    target.Method(1, 2);
                    var calls = Fake.GetCalls(target);
                    return Verify(calls);
                    """)
                {
                    Comment =
                    [
                        "Verifying the recorded calls replaces a list of hand written asserts: the method, its",
                        "parameter names and the values passed all land in the snapshot."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "FakeManager",
                    """
                    var target = A.Fake<ITarget>();
                    target.Method(1, 2);
                    var fakeManager = Fake.GetFakeManager(target);
                    return Verify(fakeManager);
                    """)
                {
                    Comment =
                    [
                        "The FakeManager carries the same calls plus the rules configured on the fake, so a",
                        "snapshot of it also shows what was set up and never used."
                    ]
                }
            ],
            Notes =
            [
                "Purely a converter plugin: nothing is compared or converted to a file, only serialized more readably.",
                "The snapshot has the same shape as Verify.NSubstitute's, so switching mocking library keeps the verified files close."
            ]
        },
        new()
        {
            Id = "Flurl",
            DisplayName = "Verify.Flurl",
            RepoUrl = "https://github.com/VerifyTests/Verify.Flurl",
            Description = "Snapshots a Flurl HttpTest and the calls it recorded, instead of asserting on them one by one.",
            Category = ExtensionCategory.Web,
            Packages = [new("Verify.Flurl")],
            PluginType = "VerifyFlurl",
            Initialize =
            [
                new(
                    "VerifyFlurl.Initialize();",
                    "Verify.Flurl: an HttpTest, and each call it recorded, are written as a readable request",
                    "and response rather than as their raw object graphs.",
                    "It initializes Verify.Http itself when that has not already run, so it is called after it.")
            ],
            Usings = ["Flurl.Http", "Flurl.Http.Testing"],
            MinimalSamples =
            [
                new(
                    "HttpTest",
                    """
                    using var httpTest = new HttpTest();

                    httpTest.RespondWith("OK");

                    await "http://api.mysite.com/".GetAsync();
                    await "http://api.mysite.com/".PostAsync(new StringContent("the content"));

                    await Verify(httpTest);
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "Everything sent while the HttpTest is in scope is captured. Verifying the test itself",
                        "writes every call, in order, with the response Flurl was told to return."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "SingleCall",
                    """
                    using var httpTest = new HttpTest();

                    httpTest.RespondWith("OK");

                    await "http://api.mysite.com/".GetAsync();

                    await Verify(httpTest.CallLog.Single());
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "A single recorded call can be verified on its own, which keeps a snapshot focused when",
                        "the code under test makes calls that are not interesting."
                    ]
                }
            ],
            Notes =
            [
                "Verify.Flurl depends on Verify.Http and calls `VerifyHttp.Initialize()` itself when it has not already run, so a later explicit call throws \"Already Initialized\".",
                "All http scrubbing and settings come from Verify.Http; this package adds only the two converters.",
                "The package targets net10.0 only."
            ]
        },
        new()
        {
            Id = "Http",
            DisplayName = "Verify.Http",
            RepoUrl = "https://github.com/VerifyTests/Verify.Http",
            Description = "Snapshots http requests and responses, records what an HttpClient sent, and ships a mock HttpClient.",
            Category = ExtensionCategory.Web,
            Packages = [new("Verify.Http")],
            PluginType = "VerifyHttp",
            Initialize =
            [
                new(
                    "VerifyHttp.Initialize();",
                    "Verify.Http: requests, responses, headers and content are written as readable snapshots,",
                    "and calls made while a recording is running are appended under the name httpCall.",
                    "Called explicitly so it runs before Verify.Flurl, which initializes Verify.Http itself",
                    "when it has not already run; a later VerifyHttp.Initialize() throws \"Already Initialized\".")
            ],
            Usings = ["Microsoft.Extensions.DependencyInjection", "VerifyTests.Http"],
            MinimalSamples =
            [
                new(
                    "MockResponse",
                    """
                    using var client = new MockHttpClient("the response", "text/plain");

                    var result = await client.GetAsync("https://fake/get");

                    await Verify(result);
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "MockHttpClient answers every request without touching the network, so the test is",
                        "deterministic. The response is verified as status, headers and content."
                    ]
                },
                new(
                    "RecordCalls",
                    """
                    using var client = new MockHttpClient(recording: true);
                    Recording.Start();

                    await client.GetAsync("https://fake/get");

                    await Verify();
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "Every call made between Recording.Start() and the verification is added to the snapshot",
                        "under the name httpCall, so nothing has to be passed to Verify."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "ScrubResponseBody",
                    """
                    using var client = new MockHttpClient("Herman Melville - Moby-Dick", "text/plain");

                    var result = await client.GetAsync("https://fake/get");

                    await Verify(result)
                        .ScrubHttpTextResponse(_ => _.Replace("Herman Melville - Moby-Dick", "New title"));
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "ScrubHttpTextResponse rewrites a text response body before it is written, for values",
                        "that change on every run."
                    ]
                },
                new(
                    "IgnoreHeaders",
                    """
                    using var client = new MockHttpClient();

                    var result = await client.GetAsync("https://fake/get");

                    await Verify(result)
                        .IgnoreMembers(
                            "Server",
                            "Content-Length",
                            "Date");
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "Headers are serialized as members, so a header that varies per run is dropped with the",
                        "same IgnoreMembers that drops any other member."
                    ]
                },
                new(
                    "RecordingHttpClientFactory",
                    """
                    var collection = new ServiceCollection();
                    var (builder, recording) = collection.AddRecordingHttpClient();
                    builder.ConfigurePrimaryHttpMessageHandler(() => new MockHttpHandler("the response", "text/plain"));

                    await using var provider = collection.BuildServiceProvider();
                    var factory = provider.GetRequiredService<IHttpClientFactory>();
                    using var client = factory.CreateClient(builder.Name);

                    await client.GetAsync("https://fake/get");

                    await Verify(recording.Sends)
                        .IgnoreMember("Date");
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "AddRecordingHttpClient adds a client to the container together with a handler that keeps",
                        "every call it made, which is how a service built by dependency injection is covered.",
                        "The handler is instance based, so it needs no recording and no ordering."
                    ]
                },
                new(
                    "SimulateNetworkStream",
                    """
                    using var client = new MockHttpClient("the response", "text/plain")
                    {
                        SimulateNetworkStream = true
                    };

                    var result = await client.GetAsync(
                        "https://fake/get",
                        HttpCompletionOption.ResponseHeadersRead);

                    await Verify(result);
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "A real network response is a read once, non seekable stream. This reproduces that, so",
                        "code that reads the body twice fails in the test rather than in production."
                    ]
                },
                new(
                    "FilterRecordedCalls",
                    """
                    using var client = new MockHttpClient(recording: true);
                    Recording.Start();

                    await client.GetAsync("https://fake/get");

                    var httpCalls = Recording.Stop()
                        .Select(_ => _.Data)
                        .OfType<HttpCall>()
                        .ToList();
                    await Verify(httpCalls);
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "Recording.Stop() returns the entries instead of adding them, so they can be filtered or",
                        "asserted on before being verified. An HttpCall carries the request, response and status."
                    ]
                }
            ],
            Notes =
            [
                "Recorded http entries are named `httpCall`; `Recording.IgnoreNames(\"http\")` matches nothing.",
                "The readme's `HttpRecording.StartRecording()` does not exist; the API is `Recording.Start()` from Verify core.",
                "Nothing is captured unless a recording is running, so `Recording.Start()` has to run before the code under test makes its calls.",
                "`MockHttpClient` can also replay files, `new MockHttpClient(\"sample.html\", \"sample.json\")`, with the files copied to the output directory.",
                "`MockHttpHandler` has the same constructors, for when the handler rather than the client has to be injected.",
                "The package targets net10.0 only."
            ]
        },
        new()
        {
            Id = "ICSharpCodeDecompiler",
            DisplayName = "Verify.ICSharpCode.Decompiler",
            RepoUrl = "https://github.com/VerifyTests/Verify.ICSharpCode.Decompiler",
            Description = "Verifies assemblies, types, methods and properties as decompiled IL, so a change in codegen is visible.",
            Category = ExtensionCategory.Compiler,
            Packages = [new("Verify.ICSharpCode.Decompiler")],
            PluginType = "VerifyICSharpCodeDecompiler",
            Usings = ["ICSharpCode.Decompiler.Metadata", "VerifyTests.ICSharpCode.Decompiler"],
            MinimalSamples =
            [
                new(
                    "DecompileType",
                    """
                    using var file = new PEFile(AssemblyPath);
                    await Verify(new TypeToDisassemble(file, "ClassBeingTested"));
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "The snapshot is the IL of one type, written to a .verified.il file. A change to the",
                        "source that does not change the IL leaves the snapshot alone, and the reverse holds too."
                    ],
                    Members =
                    [
                        """
                        // The assembly the samples decompile: the class library this solution builds. Point this
                        // at whichever assembly the generated code under test ends up in.
                        static string AssemblyPath => typeof(ClassBeingTested).Assembly.Location;
                        """
                    ]
                },
                new(
                    "DecompileMethod",
                    """
                    using var file = new PEFile(AssemblyPath);
                    await Verify(new MethodToDisassemble(file, "ClassBeingTested", "FindPerson"));
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "One method is usually the right size for a snapshot: a whole type re-records on every",
                        "unrelated edit. An overload takes a MethodDefinitionHandle for a hand picked overload."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "DecompileAssembly",
                    """
                    using var file = new PEFile(AssemblyPath);
                    await Verify(new AssemblyToDisassemble(file, AssemblyOptions.IncludeAssemblyHeader));
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "AssemblyOptions is a flags enum: None, IncludeAssemblyReferences, IncludeAssemblyHeader,",
                        "IncludeModuleHeader, IncludeModuleContents and Full. The default is IncludeModuleContents."
                    ]
                },
                new(
                    "DontNormalize",
                    """
                    using var file = new PEFile(AssemblyPath);
                    await Verify(new TypeToDisassemble(file, "ClassBeingTested"))
                        .DontNormalizeIl();
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "Normalization sorts members by name and strips RVA address comments, so a test does not",
                        "fail only because the binary layout moved. This turns it off, for an exact snapshot."
                    ]
                },
                new(
                    "ScrubComments",
                    """
                    using var file = new PEFile(AssemblyPath);
                    await Verify(new MethodToDisassemble(file, "ClassBeingTested", "FindPerson"))
                        .ScrubComments();
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "The decompiler writes header size, code size and stack comments into the IL. This drops",
                        "them, leaving only the instructions. ScrubBinaryData() does the same for embedded blobs."
                    ]
                }
            ],
            Notes =
            [
                "Snapshots use the `.il` extension, which `Initialize()` registers as a text extension.",
                "Since version 3.2 the IL is normalized by default; turning that off with `DontNormalizeIl()` re-orders every existing verified file once.",
                "The types are split across two namespaces, `ICSharpCode.Decompiler.Metadata` for `PEFile` and `VerifyTests.ICSharpCode.Decompiler` for the rest.",
                "The package targets net48 and net8.0, so a newer project resolves the net8.0 assets."
            ]
        },
        new()
        {
            Id = "ImageHash",
            DisplayName = "Verify.ImageHash",
            RepoUrl = "https://github.com/VerifyTests/Verify.ImageHash",
            Description = "Compares png, jpg and bmp snapshots by perceptual hash, so a small rendering difference still passes.",
            Category = ExtensionCategory.Images,
            Packages = [new("Verify.ImageHash")],
            PluginType = "VerifyImageHash",
            Phase = InitializePhase.Comparers,
            ExclusiveGroups = ["image-comparer"],
            Initialize =
            [
                new(
                    "VerifyImageHash.Initialize();",
                    "Verify.ImageHash: png, jpg and bmp snapshots are compared by perceptual hash rather than",
                    "byte for byte, so anti-aliasing and font hinting differences no longer fail a test.",
                    "Initialize() registers the comparers with the default threshold of 95 and is what marks",
                    "the plugin initialized, so it has to run first: RegisterComparers() does not set that",
                    "flag, and the trailing InitializePlugins() would call Initialize() again and win."),
                new(
                    "VerifyImageHash.RegisterComparers(threshold: {imagehash-threshold}, algorithm: new {imagehash-algorithm}());",
                    "Re-registers the same three comparers with the values chosen in the wizard. The threshold",
                    "is a similarity percentage, so a higher number is stricter.")
                {
                    Alternatives =
                    [
                        "VerifyImageHash.RegisterComparer(threshold: 95, algorithm: null, extension: \"png\");"
                    ]
                }
            ],
            InitializeUsings = ["CoenM.ImageHash.HashAlgorithms"],
            Usings = ["CoenM.ImageHash.HashAlgorithms", "SixLabors.ImageSharp", "SixLabors.ImageSharp.PixelFormats"],
            Choices =
            [
                new(
                    "imagehash-threshold",
                    "Image similarity threshold",
                    "How similar two images have to be to count as equal. A similarity percentage: higher is stricter.",
                    [
                        new("95", "95 (default)", "Only near identical images match."),
                        new("90", "90", "Tolerates anti-aliasing and subpixel text rendering."),
                        new("85", "85", "Tolerates the rendering differences between operating systems.")
                    ]),
                new(
                    "imagehash-algorithm",
                    "Hashing algorithm",
                    "Which perceptual hash the comparison uses.",
                    [
                        new("DifferenceHash", "DifferenceHash", "The package default: fast, and sensitive to layout changes."),
                        new("PerceptualHash", "PerceptualHash", "Slower, and more tolerant of scaling and brightness changes."),
                        new("AverageHash", "AverageHash", "The simplest of the three, and the most tolerant.")
                    ])
            ],
            MinimalSamples =
            [
                new(
                    "CompareImage",
                    """
                    var stream = NewImage();
                    return Verify(stream, "png");
                    """)
                {
                    Comment =
                    [
                        "Nothing in the test names the comparer: it is registered for the png extension, so every",
                        "png snapshot in the project is compared this way."
                    ],
                    Members =
                    [
                        """
                        // Builds a small png in memory, so the samples need no image file checked in. Replace this
                        // with VerifyFile("sample.png"), or with whatever produces the image under test.
                        static Stream NewImage()
                        {
                            using var image = new Image<Rgba32>(11, 11)
                            {
                                [5, 5] = Color.ParseHex("#0000FF").ToPixel<Rgba32>()
                            };
                            var stream = new MemoryStream();
                            image.SaveAsPng(stream);
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
                    "PerTestThreshold",
                    """
                    var stream = NewImage();
                    return Verify(stream, "png")
                        .UseImageHash(threshold: 85);
                    """)
                {
                    Comment =
                    [
                        "UseImageHash overrides the registered threshold for one test, for a snapshot that is",
                        "known to be less stable than the rest."
                    ]
                },
                new(
                    "PerTestAlgorithm",
                    """
                    var stream = NewImage();
                    return Verify(stream, "png")
                        .UseImageHash(algorithm: new PerceptualHash());
                    """)
                {
                    Comment =
                    [
                        "The algorithm can be overridden per test too. PerceptualHash tolerates scaling and",
                        "brightness changes that DifferenceHash reports as a mismatch."
                    ]
                }
            ],
            Notes =
            [
                "The threshold is a similarity percentage, so higher is stricter. Verify.ImageSharp.Compare's threshold is an absolute error, where lower is stricter.",
                "A comparer only changes how an existing image snapshot is compared; nothing is rendered or converted.",
                "On a mismatch the failure message prints the measured similarity, which is the number to register.",
                "The package targets net8.0 only, and the assembly is not strong named."
            ]
        },
        new()
        {
            Id = "ImageMagick",
            DisplayName = "Verify.ImageMagick",
            RepoUrl = "https://github.com/VerifyTests/Verify.ImageMagick",
            Description = "Renders pdf and svg to png through Magick.NET, and compares images with a fuzz tolerance.",
            Category = ExtensionCategory.Images,
            Packages = [new("Verify.ImageMagick")],
            PluginType = "VerifyImageMagick",
            RetiredBy = "U8",
            Initialize =
            [
                new(
                    "VerifyImageMagick.Initialize();",
                    "Verify.ImageMagick: registers the stream converters for pdf, svg, png, webp and tiff, so",
                    "a document is snapshotted as one png per page rather than as bytes.",
                    "Despite what the readme says, Initialize() registers no comparers. It is still what marks",
                    "the plugin initialized, so it runs first: neither RegisterComparers() nor",
                    "RegisterPdfToPngConverter() sets that flag, and InitializePlugins() would call",
                    "Initialize() afterwards and overwrite whatever they had registered."),
                new(
                    "VerifyImageMagick.RegisterComparers(threshold: {imagemagick-threshold});",
                    "Registers the image comparers: png, jpg, bmp, tiff and webp as streams, and svg as text.",
                    "The threshold is a fuzz distortion, so a lower number is stricter.")
                {
                    // Not emitted for the pdf-only role, where another extension owns the comparers.
                    WhenChoice = "imagemagick-role:converter-and-comparer|comparer-only",
                    Alternatives =
                    [
                        "VerifyImageMagick.RegisterComparer(threshold: .005, metric: ErrorMetric.Fuzz, extension: \"png\");",
                        "ImageMagickSettings.ImageMagickBackground(MagickColors.White);"
                    ]
                }
            ],
            ExternalRequirements =
            [
                new(
                    "Ghostscript",
                    "pdf pages are rendered by shelling out to Ghostscript; without it the conversion fails with exit code 127. Nothing else here needs it.")
                {
                    Url = "https://www.ghostscript.com/releases/gsdnld.html",
                    WindowsInstall = "choco install ghostscript.app",
                    LinuxInstall = "sudo apt-get install --yes ghostscript"
                }
            ],
            ExclusiveGroups = ["pdf-converter", "image-comparer"],
            Usings = ["ImageMagick", "VerifyTestsImageMagick"],
            Choices =
            [
                new(
                    "imagemagick-role",
                    "What Verify.ImageMagick is used for",
                    "Initialize() registers the converters and RegisterComparers() the comparers; this says which of the two the rest of the selection should defer to.",
                    [
                        new("converter-and-comparer", "Render documents and compare images", "The pdf and svg converters and the image comparers all come from Verify.ImageMagick."),
                        new("comparer-only", "Compare images only", "Another extension renders pdf, and is initialized after this one so its converter wins."),
                        new("pdf-only", "Render pdf to png only", "Another extension registers the image comparers, so no RegisterComparers call is generated.")
                    ]),
                new(
                    "imagemagick-threshold",
                    "Image comparison threshold",
                    "The fuzz distortion allowed between two images. Lower is stricter, and .005 is the package default.",
                    [
                        new(".005", "0.005 (default)", "Only near identical images match."),
                        new(".05", "0.05", "Tolerates anti-aliasing and font hinting differences."),
                        new(".12", "0.12", "Tolerates the rendering differences between operating systems.")
                    ])
            ],
            MinimalSamples =
            [
                new(
                    "Svg",
                    """"
                    var svg =
                        """
                        <svg xmlns="http://www.w3.org/2000/svg" width="80" height="80">
                          <circle cx="40" cy="40" r="30" fill="blue" />
                        </svg>
                        """u8.ToArray();
                    var stream = new MemoryStream(svg);
                    return Verify(stream, "svg");
                    """")
                {
                    Comment =
                    [
                        "An svg is verified twice: the markup as .verified.svg, and the rendered image as",
                        ".verified.png, so a change that only the renderer sees still shows up as a diff."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "ComparerPerTest",
                    """
                    var stream = NewPng(MagickColors.CornflowerBlue);
                    return Verify(stream, "png")
                        .ImageMagickComparer(threshold: .05);
                    """)
                {
                    Comment =
                    [
                        "ImageMagickComparer overrides the registered threshold for one test, for a snapshot that",
                        "is known to be less stable than the rest."
                    ],
                    Members =
                    [
                        """
                        // Builds a png in memory, so the samples need no image file checked in. Replace this with
                        // VerifyFile("sample.png"), or with whatever produces the image under test.
                        static Stream NewPng(MagickColor color)
                        {
                            using var image = new MagickImage(color, 40, 40);
                            var stream = new MemoryStream();
                            image.Write(stream, MagickFormat.Png);
                            stream.Position = 0;
                            return stream;
                        }
                        """
                    ]
                },
                new(
                    "BackgroundColor",
                    """
                    var stream = NewPng(MagickColors.Transparent);
                    return Verify(stream, "png")
                        .ImageMagickBackground(MagickColors.Blue);
                    """)
                {
                    Comment =
                    [
                        "A transparent background renders differently depending on what is behind it. Flattening",
                        "it onto a known colour makes the snapshot mean the same thing everywhere."
                    ]
                },
                new(
                    "PdfToPng",
                    """
                    return VerifyFile("sample.pdf");
                    """)
                {
                    SkipReason = "needs a sample.pdf beside the test project, copied to the output directory, and Ghostscript installed",
                    Comment =
                    [
                        "A pdf is converted to one png per page, and each page becomes its own verified file, so",
                        "a diff points at the page that changed rather than at the whole document."
                    ]
                }
            ],
            Notes =
            [
                "`Initialize()` registers converters and no comparers, despite the readme saying it registers both.",
                "Neither `RegisterComparers()` nor `RegisterPdfToPngConverter()` sets `Initialized`, so `InitializePlugins()` would still call `Initialize()` afterwards. That is why `Initialize()` is emitted first.",
                "With the pdf-only role, delete the `RegisterComparers` line: the image comparers then come from another extension.",
                "The per test settings live in the `VerifyTestsImageMagick` namespace, not in `VerifyTests`.",
                "`PagesToInclude` and `SkipPdfNormalization` are also defined by the other pdf extensions, so with two of them referenced the fluent call is an ambiguous-call compile error.",
                "Rendering a pdf shells out to Ghostscript; svg, png, webp and tiff do not need it.",
                "`ImageMagickPdfPassword(\"password\")` opens a password protected pdf.",
                "The broadest target framework span of the extensions here: net48 through net10.0."
            ]
        },
        new()
        {
            Id = "ImageSharp",
            DisplayName = "Verify.ImageSharp",
            RepoUrl = "https://github.com/VerifyTests/Verify.ImageSharp",
            Description = "Verifies ImageSharp images as an info file plus the image, with optional SSIM based comparison.",
            Category = ExtensionCategory.Images,
            Packages = [new("Verify.ImageSharp")],
            PluginType = "VerifyImageSharp",
            RetiredBy = "U17",
            Initialize =
            [
                new(
                    "VerifyImageSharp.Initialize(ssimThreshold: {imagesharp-ssim});",
                    "Verify.ImageSharp: an Image is verified as an info file with its dimensions plus the image",
                    "itself, and bmp, gif, jpg, png and tif streams are re-encoded through ImageSharp.",
                    "The threshold has to be passed here: SSIM comparers are only registered when it is below",
                    "1.0, and a per test SsimThreshold() does nothing until they are.")
            ],
            Usings = ["SixLabors.ImageSharp", "SixLabors.ImageSharp.PixelFormats"],
            Choices =
            [
                new(
                    "imagesharp-ssim",
                    "Image comparison",
                    "Image comparison is byte exact unless a structural similarity threshold below 1.0 is passed to Initialize.",
                    [
                        new("1.0", "Byte exact (default)", "Any difference at all fails the test."),
                        new("0.999", "SSIM 0.999", "Tolerates anti-aliasing and subpixel text rendering."),
                        new("0.995", "SSIM 0.995", "Tolerates minor font and layout shifts between OS versions."),
                        new("0.99", "SSIM 0.99", "Tolerates moderate rendering variation.")
                    ])
            ],
            MinimalSamples =
            [
                new(
                    "VerifyImage",
                    """
                    using var image = new Image<Rgba32>(11, 11)
                    {
                        [5, 5] = Color.ParseHex("#0000FF").ToPixel<Rgba32>()
                    };
                    await Verify(image);
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "Two files are written: an info .verified.txt with the width, height and resolution, and",
                        "the image itself. The info file makes a size change readable in a text diff."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "SsimThreshold",
                    """
                    using var image = new Image<Rgba32>(11, 11)
                    {
                        [5, 5] = Color.ParseHex("#0000FF").ToPixel<Rgba32>()
                    };
                    await Verify(image)
                        .SsimThreshold(0.95);
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "Overrides the threshold for one test. It has no effect unless Initialize was given a",
                        "threshold below 1.0, because that is what registers the comparers."
                    ]
                },
                new(
                    "EncodeAsJpeg",
                    """
                    using var image = new Image<Rgba32>(11, 11)
                    {
                        [5, 5] = Color.ParseHex("#0000FF").ToPixel<Rgba32>()
                    };
                    await Verify(image)
                        .EncodeAsJpeg();
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "The encoder decides the snapshot's file extension. EncodeAsPng, EncodeAsGif, EncodeAsBmp",
                        "and EncodeAsTiff do the same, and each takes an encoder instance for its own options."
                    ]
                }
            ],
            Notes =
            [
                "Two files are produced per image: an info `.txt` and the image.",
                "SSIM comparers are registered only when `Initialize` is given a threshold below 1.0; a per test `SsimThreshold()` does nothing until then.",
                "The stream converters cover bmp, gif, jpg, png and tif, so a png another extension rendered is re-encoded by ImageSharp before it is written.",
                "`SsimComparer.Calculate(received, verified)` returns the raw similarity, which is the number to choose a threshold from."
            ]
        },
        new()
        {
            Id = "ImageSharpCompare",
            DisplayName = "Verify.ImageSharp.Compare",
            RepoUrl = "https://github.com/VerifyTests/Verify.ImageSharp.Compare",
            Description = "Compares png, jpg and bmp snapshots pixel by pixel, passing while the absolute error stays under a threshold.",
            Category = ExtensionCategory.Images,
            Packages = [new("Verify.ImageSharp.Compare")],
            PluginType = "VerifyImageSharpCompare",
            Phase = InitializePhase.Comparers,
            RetiredBy = "U7",
            ExclusiveGroups = ["image-comparer"],
            Initialize =
            [
                new(
                    "VerifyImageSharpCompare.RegisterComparers(threshold: {imagesharpcompare-threshold});",
                    "Verify.ImageSharp.Compare: png, jpg and bmp snapshots are compared pixel by pixel, and",
                    "pass while the absolute error stays under the threshold.",
                    "This runs before Initialize(): registration keeps the first comparer registered for each",
                    "extension, and Initialize() would otherwise get in first with the default threshold of 5."),
                new(
                    "VerifyImageSharpCompare.Initialize();",
                    "Marks the plugin initialized, so the trailing InitializePlugins() leaves the registration",
                    "above alone.")
            ],
            Usings = ["SixLabors.ImageSharp", "SixLabors.ImageSharp.PixelFormats"],
            Choices =
            [
                new(
                    "imagesharpcompare-threshold",
                    "Allowed pixel difference",
                    "The absolute error allowed between two images. Lower is stricter, and it grows with the size of the image.",
                    [
                        new("5", "5 (default)", "Only near identical images match."),
                        new("10", "10", "Tolerates anti-aliasing and subpixel text rendering."),
                        new("25", "25", "Tolerates the rendering differences between operating systems.")
                    ])
            ],
            MinimalSamples =
            [
                new(
                    "CompareImage",
                    """
                    var stream = NewImage();
                    return Verify(stream, "png");
                    """)
                {
                    Comment =
                    [
                        "Nothing in the test names the comparer: it is registered for the png extension, so every",
                        "png snapshot in the project is compared this way."
                    ],
                    Members =
                    [
                        """
                        // Builds a small png in memory, so the samples need no image file checked in. Replace this
                        // with VerifyFile("sample.png"), or with whatever produces the image under test.
                        static Stream NewImage()
                        {
                            using var image = new Image<Rgba32>(11, 11)
                            {
                                [5, 5] = Color.ParseHex("#0000FF").ToPixel<Rgba32>()
                            };
                            var stream = new MemoryStream();
                            image.SaveAsPng(stream);
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
                    "PerTestThreshold",
                    """
                    var stream = NewImage();
                    return Verify(stream, "png")
                        .UseImageHash(threshold: 10);
                    """)
                {
                    Comment =
                    [
                        "The per test method is called UseImageHash, copied from Verify.ImageHash. It is this",
                        "package's comparer that runs, and the number is an absolute error, not a similarity."
                    ]
                }
            ],
            Notes =
            [
                "The threshold is an absolute error: lower is stricter, and 5 is the default. Verify.ImageHash's threshold is a similarity percentage, where higher is stricter.",
                "The absolute error grows with the size of the image, so a threshold does not carry over between snapshots of different sizes.",
                "The per test method is named `UseImageHash`, and the readme and xmldoc describe a DifferenceHash algorithm; both are copy and paste from Verify.ImageHash and neither is accurate here.",
                "The package targets net8.0 only."
            ]
        },
        new()
        {
            Id = "MailMessage",
            DisplayName = "Verify.MailMessage",
            RepoUrl = "https://github.com/VerifyTests/Verify.MailMessage",
            Description = "Snapshots System.Net.Mail types: a MailMessage, its attachments, alternate views and content types.",
            Category = ExtensionCategory.Email,
            Packages = [new("Verify.MailMessage")],
            PluginType = "VerifyMailMessage",
            Usings = ["System.Net.Mail", "System.Net.Mime"],
            MinimalSamples =
            [
                new(
                    "MailMessage",
                    """
                    var mail = new MailMessage(
                        from: "from@mail.com",
                        to: "to@mail.com",
                        subject: "The subject",
                        body: "The body");
                    return Verify(mail);
                    """)
                {
                    Comment =
                    [
                        "The message is written as addresses, subject and body rather than as its object graph,",
                        "so the snapshot reads like the mail that would be sent."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "Attachment",
                    """
                    var attachment = new Attachment(
                        new MemoryStream("file content"u8.ToArray()),
                        new ContentType("text/html; charset=utf-8"))
                    {
                        Name = "name.txt"
                    };
                    return Verify(attachment);
                    """)
                {
                    Comment =
                    [
                        "An attachment has a file converter, so its content is written as a separate verified",
                        "file with the extension its content type implies."
                    ]
                },
                new(
                    "ContentType",
                    """
                    var content = new ContentType("text/html; charset=utf-8")
                    {
                        Name = "name.txt"
                    };
                    return Verify(content);
                    """)
                {
                    Comment =
                    [
                        "A ContentType is verified on its own, which is useful when the code under test builds",
                        "one rather than a whole message."
                    ]
                },
                new(
                    "ContentDisposition",
                    """
                    var content = new ContentDisposition("attachment; filename=\"filename.jpg\"");
                    return Verify(content);
                    """)
                {
                    Comment =
                    [
                        "The same for a ContentDisposition, which is what decides whether a client shows an",
                        "attachment inline or offers it as a download."
                    ]
                }
            ],
            Notes =
            [
                "File converters are registered for `MailMessage`, `Attachment`, `AlternateView` and `LinkedResource`, so attachments are split into their own verified files.",
                "`ContentId` values are generated per message, and are scrubbed to `Guid_1`.",
                "No third party dependency: the mail types are part of the base class library."
            ]
        },
        new()
        {
            Id = "MassTransit",
            DisplayName = "Verify.MassTransit",
            RepoUrl = "https://github.com/VerifyTests/Verify.MassTransit",
            Description = "Snapshots a MassTransit test harness: the messages sent, published and consumed, and saga state changes.",
            Category = ExtensionCategory.Messaging,
            Packages =
            [
                new("Verify.MassTransit"),
                new("MassTransit")
                {
                    ForLibrary = true,
                    Comment = "the consumer, saga and messages the samples exercise live in the class library"
                }
            ],
            PluginType = "VerifyMassTransit",
            Usings = ["MassTransit", "MassTransit.Testing"],
            LibraryFiles =
            [
                new(
                    "SubmitOrderConsumer.cs",
                    """
                    using MassTransit;

                    // Stands in for a real consumer. The samples send SubmitOrder to it through an in memory
                    // harness and snapshot everything the harness saw.
                    public class SubmitOrderConsumer :
                        IConsumer<SubmitOrder>
                    {
                        public Task Consume(ConsumeContext<SubmitOrder> context) =>
                            context.Publish(
                                new OrderSubmitted
                                {
                                    OrderId = context.Message.OrderId
                                });
                    }

                    public record SubmitOrder
                    {
                        public Guid OrderId { get; init; }
                    }

                    public record OrderSubmitted
                    {
                        public Guid OrderId { get; init; }
                    }
                    """),
                new(
                    "ConsumerSaga.cs",
                    """
                    using MassTransit;

                    // Stands in for a real saga. The saga sample verifies its harness alongside the bus harness.
                    public class ConsumerSaga :
                        ISaga,
                        InitiatedBy<Start>
                    {
                        public Guid CorrelationId { get; set; }

                        public Task Consume(ConsumeContext<Start> context) =>
                            Task.CompletedTask;
                    }

                    public record Start :
                        CorrelatedBy<Guid>
                    {
                        public Guid CorrelationId { get; init; }
                    }
                    """)
            ],
            MinimalSamples =
            [
                new(
                    "ConsumerHarness",
                    """
                    using var harness = new InMemoryTestHarness();
                    var consumer = harness.Consumer<SubmitOrderConsumer>();

                    await harness.Start();
                    try
                    {
                        await harness.InputQueueSendEndpoint
                            .Send(
                                new SubmitOrder
                                {
                                    OrderId = InVar.Id
                                });

                        await Verify(
                            new
                            {
                                harness,
                                consumer
                            });
                    }
                    finally
                    {
                        await harness.Stop();
                    }
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "Verifying the harness and the consumer harness together replaces a run of asserts: the",
                        "snapshot lists what was sent, what was published in response, and what was consumed.",
                        "The harness is in memory, so no broker is involved."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "SagaHarness",
                    """
                    using var harness = new InMemoryTestHarness();
                    var sagaHarness = harness.Saga<ConsumerSaga>();

                    var correlationId = NewId.NextGuid();

                    await harness.Start();
                    try
                    {
                        await harness.Bus.Publish(
                            new Start
                            {
                                CorrelationId = correlationId
                            });

                        await harness.Consumed.Any<Start>();

                        await Verify(
                            new
                            {
                                harness,
                                sagaHarness
                            });
                    }
                    finally
                    {
                        await harness.Stop();
                    }
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "A saga harness adds the saga instances and their state changes to the snapshot, so a",
                        "state machine is covered by one verified file instead of an assert per transition."
                    ]
                }
            ],
            Notes =
            [
                "Guids are scrubbed to `Guid_N` by Verify core, so `InVar.Id` and the correlation ids stay stable between runs.",
                "The harness has to be started and stopped around the test; the `finally` keeps a failure from leaving it running.",
                "Everything is in memory: no broker, container or transport is needed."
            ]
        },
        new()
        {
            Id = "MicrosoftLogging",
            DisplayName = "Verify.MicrosoftLogging",
            RepoUrl = "https://github.com/VerifyTests/Verify.MicrosoftLogging",
            Description = "Records what the code under test logged through ILogger and appends it to the snapshot.",
            Category = ExtensionCategory.Logging,
            Packages =
            [
                new("Verify.MicrosoftLogging"),
                new("Microsoft.Extensions.Logging.Abstractions")
                {
                    ForLibrary = true,
                    Comment = "the class under test takes an ILogger"
                }
            ],
            PluginType = "VerifyMicrosoftLogging",
            Initialize =
            [
                new(
                    "VerifyMicrosoftLogging.Initialize();",
                    "Verify.MicrosoftLogging: entries logged while a recording is running are appended to the",
                    "snapshot under the name log.",
                    "Called explicitly so it runs before Verify.NServiceBus, which initializes it itself when",
                    "it has not already run.")
            ],
            Usings = ["VerifyTests.MicrosoftLogging"],
            LibraryFiles =
            [
                new(
                    "ClassThatUsesLogging.cs",
                    """
                    using Microsoft.Extensions.Logging;

                    // Stands in for real code that logs. The samples snapshot what it logged as well as what it
                    // returned, so a change in the logging is a change in the test.
                    public class ClassThatUsesLogging(ILogger logger)
                    {
                        public string Method()
                        {
                            logger.LogWarning("The log entry");
                            using (logger.BeginScope("The scope"))
                            {
                                logger.LogWarning("Entry in scope");
                            }

                            return "result";
                        }
                    }

                    // The same, through a typed logger, which adds a Category to every entry.
                    public class ClassThatUsesTypedLogging(ILogger<ClassThatUsesTypedLogging> logger)
                    {
                        public string Method()
                        {
                            logger.LogWarning("The log entry");
                            return "result";
                        }
                    }
                    """)
            ],
            MinimalSamples =
            [
                new(
                    "Logging",
                    """
                    Recording.Start();
                    var logger = new RecordingLogger();
                    var target = new ClassThatUsesLogging(logger);

                    var result = target.Method();

                    return Verify(result);
                    """)
                {
                    Comment =
                    [
                        "The recording starts before anything logs: Recording.Add throws for an entry written",
                        "outside a recording, so a logger handed out earlier would fail the test.",
                        "Only the result is passed to Verify; the log entries are appended under log on their own."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "TypedLogger",
                    """
                    Recording.Start();
                    var logger = RecordingProvider.CreateLogger<ClassThatUsesTypedLogging>();
                    var target = new ClassThatUsesTypedLogging(logger);

                    var result = target.Method();

                    return Verify(result);
                    """)
                {
                    Comment =
                    [
                        "RecordingProvider also implements ILoggerProvider, so it can be added to a real logging",
                        "builder. CreateLogger<T> is the shortcut for a class that takes an ILogger<T>."
                    ]
                }
            ],
            Notes =
            [
                "Nothing is captured unless `Recording.Start()` has run, and `Recording.Add` throws for anything logged outside a recording, so start the recording before the code under test logs.",
                "The readme's `LoggerRecording` and `LoggerProvider` types do not exist; the API is `Recording.Start()`, `RecordingLogger` and `RecordingProvider`.",
                "Every logging extension records under the name `log`, so entries from two of them interleave and `Recording.IgnoreNames` cannot separate them.",
                "Scopes are recorded as `StartScope` and `EndScope` entries around the entries they contain.",
                "Verify.NServiceBus depends on this package and initializes it itself unless it has already run."
            ]
        },
        new()
        {
            Id = "Mockly",
            DisplayName = "Verify.Mockly",
            RepoUrl = "https://github.com/VerifyTests/Verify.Mockly",
            Description = "Snapshots a Mockly HttpMock and the requests it captured, instead of asserting on them one by one.",
            Category = ExtensionCategory.Mocking,
            Packages = [new("Verify.Mockly")],
            PluginType = "VerifyMockly",
            Usings = ["Mockly", "System.Net"],
            MinimalSamples =
            [
                new(
                    "GetRequest",
                    """
                    var mock = new HttpMock();

                    mock.ForGet()
                        .ForHttps()
                        .ForHost("api.example.com")
                        .WithPath("/api/users/123")
                        .RespondsWithJsonContent(
                            new
                            {
                                id = 123,
                                name = "John"
                            });

                    var client = mock.GetClient();
                    await client.GetAsync("https://api.example.com/api/users/123");
                    await Verify(mock);
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "Verifying the mock writes every request it captured, with the method, host, path and",
                        "whether it matched an expectation, so one snapshot covers what would be several asserts."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "RequestCollection",
                    """
                    var mock = new HttpMock();
                    var requests = new RequestCollection();

                    mock.ForGet()
                        .ForHttps()
                        .ForHost("api.example.com")
                        .WithPath("/api/users/*")
                        .CollectingRequestsIn(requests)
                        .RespondsWithJsonContent(
                            new
                            {
                                id = 1,
                                name = "John"
                            });

                    var client = mock.GetClient();
                    await client.GetAsync("https://api.example.com/api/users/1");
                    await client.GetAsync("https://api.example.com/api/users/2");
                    await Verify(requests);
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "CollectingRequestsIn narrows the snapshot to the requests one expectation matched, which",
                        "keeps unrelated traffic from the mock out of the verified file."
                    ]
                },
                new(
                    "ScrubBody",
                    """"
                    var mock = new HttpMock();

                    mock.ForPost()
                        .ForHttps()
                        .ForHost("api.example.com")
                        .WithPath("/api/users")
                        .RespondsWithStatus(HttpStatusCode.Created);

                    var client = mock.GetClient();
                    await client.PostAsync(
                        "https://api.example.com/api/users",
                        new StringContent("""{"name":"Jane"}"""));
                    await Verify(mock)
                        .ScrubMember("Body");
                    """")
                {
                    Async = true,
                    Comment =
                    [
                        "A captured request body is a member like any other, so ScrubMember from Verify core",
                        "removes it when it carries a value that changes on every run."
                    ]
                }
            ],
            Notes =
            [
                "The `HttpMock` fluent API comes from the Mockly package; this extension only teaches Verify how to write the results.",
                "It covers the same ground as Verify.Http's `MockHttpClient` and Verify.Flurl's `HttpTest`, so one of the three is usually enough.",
                "The only extension here that targets net472."
            ]
        },
        new()
        {
            Id = "Moq",
            DisplayName = "Verify.Moq",
            RepoUrl = "https://github.com/VerifyTests/Verify.Moq",
            Description = "Snapshots a Moq mock: the calls it received, their named arguments and what each one returned.",
            Category = ExtensionCategory.Mocking,
            Packages = [new("Verify.Moq")],
            PluginType = "VerifyMoq",
            Usings = ["Moq"],
            LibraryFiles =
            [
                new(
                    "ITarget.cs",
                    """
                    // Stands in for a real dependency. The mocking samples record the calls made to it.
                    public interface ITarget
                    {
                        string Method(int a, int b);
                    }
                    """)
            ],
            MinimalSamples =
            [
                new(
                    "RecordedInvocations",
                    """
                    var mock = new Mock<ITarget>();

                    mock.Setup(_ => _.Method(It.IsAny<int>(), It.IsAny<int>()))
                        .Returns("response");

                    var target = mock.Object;
                    target.Method(1, 2);
                    return Verify(mock);
                    """)
                {
                    Comment =
                    [
                        "The mock itself is verified, not a call list. The snapshot names each argument and",
                        "includes the value returned, which is what Moq records and the others do not."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "ScrubArguments",
                    """
                    var mock = new Mock<ITarget>();

                    mock.Setup(_ => _.Method(It.IsAny<int>(), It.IsAny<int>()))
                        .Returns("response");

                    var target = mock.Object;
                    target.Method(1, 2);
                    return Verify(mock)
                        .ScrubMember("a");
                    """)
                {
                    Comment =
                    [
                        "Arguments are recorded under their parameter names, so one that changes per run is",
                        "dropped by name with ScrubMember from Verify core."
                    ]
                }
            ],
            Notes =
            [
                "Verify the `Mock<T>`, not `mock.Object`: the converter is registered for the mock.",
                "The package has no net10.0 target framework, so a newer project resolves the net9.0 assets."
            ]
        },
        new()
        {
            Id = "NServiceBus",
            DisplayName = "Verify.NServiceBus",
            RepoUrl = "https://github.com/VerifyTests/Verify.NServiceBus",
            Description = "Snapshots what an NServiceBus handler or saga sent, published, replied and forwarded, with stable ids.",
            Category = ExtensionCategory.Messaging,
            Packages =
            [
                new("Verify.NServiceBus"),
                new("NServiceBus")
                {
                    ForLibrary = true,
                    Comment = "the handler and messages the samples exercise live in the class library"
                }
            ],
            PluginType = "VerifyNServiceBus",
            RetiredBy = "U21",
            Initialize =
            [
                new(
                    "VerifyNServiceBus.Initialize();",
                    "Verify.NServiceBus: a recording context is written as the sends, publishes, replies and",
                    "forwards it saw, with deterministic message, conversation and correlation ids.",
                    "captureLogs defaults to true and only initializes Verify.MicrosoftLogging when that has",
                    "not already run, so passing it changes nothing: the package depends on it either way.")
                {
                    Alternatives =
                    [
                        "VerifyNServiceBus.AddSharedHeader(\"sharedKey\", \"sharedValue\");"
                    ]
                }
            ],
            Usings = ["VerifyTests.NServiceBus"],
            LibraryFiles =
            [
                new(
                    "MyHandler.cs",
                    """
                    using NServiceBus;

                    // Stands in for a real handler. The samples snapshot what it did with the context it was given.
                    public class MyHandler :
                        IHandleMessages<MyRequest>
                    {
                        public Task Handle(MyRequest message, IMessageHandlerContext context) =>
                            context.Reply(new MyReply());
                    }

                    public class MyRequest :
                        IMessage;

                    public class MyReply :
                        IMessage;
                    """)
            ],
            MinimalSamples =
            [
                new(
                    "HandlerContext",
                    """
                    var handler = new MyHandler();
                    var context = new RecordingHandlerContext();

                    var message = new MyRequest();
                    await handler.Handle(message, context);

                    await Verify(context);
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "RecordingHandlerContext is a test double for IMessageHandlerContext. Verifying it after",
                        "the handler has run covers every message the handler produced, in one file.",
                        "No transport, endpoint or broker is started."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "Recording",
                    """
                    Recording.Start();
                    var handler = new MyHandler();
                    var context = new RecordingHandlerContext();

                    await handler.Handle(new MyRequest(), context);

                    await Verify("some other data");
                    """)
                {
                    Async = true,
                    Comment =
                    [
                        "With a recording running, the interactions are appended to whatever is verified instead",
                        "of being the thing verified, which suits a test whose subject is something else."
                    ]
                },
                new(
                    "MessageToHandlerMap",
                    """
                    var map = new MessageToHandlerMap();
                    map.AddMessagesFromAssembly<MyRequest>();
                    map.AddHandlersFromAssembly<MyHandler>();
                    return Verify(map);
                    """)
                {
                    Comment =
                    [
                        "The map pairs every message type with the handlers that take it, so a message that",
                        "nobody handles shows up as a diff rather than as a message that silently goes nowhere."
                    ]
                }
            ],
            Notes =
            [
                "`Initialize(captureLogs: true)` is the default, and it only initializes Verify.MicrosoftLogging when that has not already run, so the parameter has no effect on the generated code.",
                "The package depends on Verify.MicrosoftLogging, so log entries are recorded under `log` whichever way the plugins are initialized.",
                "Deterministic message, conversation and correlation ids are registered as named guids, which is what keeps the snapshots stable.",
                "`VerifyNServiceBus.AddSharedHeaders(...)` in the module initializer adds headers to every recorded message.",
                "`RecordingInvokeHandlerContext`, `RecordingIncomingPhysicalMessageContext` and `RecordingMessageSession` cover the other pipeline stages.",
                "The package targets net10.0 only."
            ]
        },
        new()
        {
            Id = "NSubstitute",
            DisplayName = "Verify.NSubstitute",
            RepoUrl = "https://github.com/VerifyTests/Verify.NSubstitute",
            Description = "Snapshots the calls an NSubstitute substitute received, with their arguments.",
            Category = ExtensionCategory.Mocking,
            Packages = [new("Verify.NSubstitute")],
            PluginType = "VerifyNSubstitute",
            Usings = ["NSubstitute"],
            LibraryFiles =
            [
                new(
                    "ITarget.cs",
                    """
                    // Stands in for a real dependency. The mocking samples record the calls made to it.
                    public interface ITarget
                    {
                        string Method(int a, int b);
                    }
                    """)
            ],
            MinimalSamples =
            [
                new(
                    "ReceivedCalls",
                    """
                    var target = Substitute.For<ITarget>();
                    target.Method(1, 2);
                    return Verify(target.ReceivedCalls());
                    """)
                {
                    Comment =
                    [
                        "ReceivedCalls() is verified rather than the substitute: one snapshot replaces a",
                        "Received().Method(...) assert per call, and it fails on a call nobody expected too."
                    ]
                }
            ],
            Notes =
            [
                "The snapshot has the same shape as Verify.FakeItEasy's, so switching mocking library keeps the verified files close.",
                "The package adds one converter and nothing else: no settings, scrubbers or recording."
            ]
        },
        new()
        {
            Id = "NUlid",
            DisplayName = "Verify.NUlid",
            RepoUrl = "https://github.com/VerifyTests/Verify.NUlid",
            Description = "Scrubs NUlid ULIDs out of snapshots, both as members and inline in text, the way Verify scrubs guids.",
            Category = ExtensionCategory.Scrubbing,
            Packages =
            [
                new("Verify.NUlid"),
                new("NUlid")
                {
                    ForLibrary = true,
                    Comment = "the model the samples verify carries a Ulid"
                }
            ],
            PluginType = "VerifyNUlid",
            ExclusiveGroups = ["ulid-scrubber"],
            Usings = ["NUlid"],
            LibraryFiles =
            [
                new(
                    "UlidPerson.cs",
                    """
                    using NUlid;

                    // Stands in for a model carrying a ULID. The samples show it scrubbed in both places it
                    // appears: as a member, and inside a string.
                    public class UlidPerson
                    {
                        public Ulid Id { get; set; }
                        public string Name { get; set; } = null!;
                        public string Description { get; set; } = null!;
                    }
                    """)
            ],
            MinimalSamples =
            [
                new(
                    "UlidScrubbing",
                    """
                    var id = Ulid.NewUlid();
                    var target = new UlidPerson
                    {
                        Id = id,
                        Name = "Sarah",
                        Description = $"Sarah ({id})"
                    };
                    return Verify(target);
                    """)
                {
                    Comment =
                    [
                        "A new ULID every run would fail every test. Both the Id member and the one inside",
                        "Description become Ulid_1: the same value maps to the same counter within a test."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "DontScrubFluent",
                    """
                    var id = Ulid.Parse("01JGXG0GDGQEP47CBQ65E50HYH");
                    var target = new UlidPerson
                    {
                        Id = id,
                        Name = "Sarah",
                        Description = $"Sarah ({id})"
                    };
                    return Verify(target)
                        .DontScrubUlids();
                    """)
                {
                    Comment =
                    [
                        "When the ULID is fixed, scrubbing hides the value the test is about. DontScrubUlids",
                        "turns it off for that test and writes the real identifier."
                    ]
                },
                new(
                    "DontScrubInstance",
                    """
                    var id = Ulid.Parse("01JGXG0GDGQEP47CBQ65E50HYH");
                    var target = new UlidPerson
                    {
                        Id = id,
                        Name = "Sarah",
                        Description = $"Sarah ({id})"
                    };
                    var settings = new VerifySettings();
                    settings.DontScrubUlids();
                    return Verify(target, settings);
                    """)
                {
                    Comment =
                    [
                        "The same setting on a VerifySettings instance, for a settings object shared by several",
                        "tests rather than built fluently in one."
                    ]
                }
            ],
            Notes =
            [
                "Scrubbing is both member level and inline: a 26 character ULID inside a string becomes `Ulid_1` too, which is why a word boundary is required.",
                "Verify.Ulid scrubs the same 26 character window and defines the same `DontScrubUlids`, so referencing both fails to compile.",
                "Nothing is converted or compared; this only rewrites what is written."
            ]
        },
        new()
        {
            Id = "Playwright",
            DisplayName = "Verify.Playwright",
            RepoUrl = "https://github.com/VerifyTests/Verify.HeadlessBrowsers",
            Description = "Verifies a Playwright page, element or locator as both its html and a screenshot png.",
            Category = ExtensionCategory.Web,
            Packages =
            [
                new("Verify.Playwright"),
                new("Microsoft.Playwright") {Comment = "the browser driver, and the playwright install script"}
            ],
            PluginType = "VerifyPlaywright",
            RetiredBy = "U18",
            Initialize =
            [
                new(
                    "VerifyPlaywright.Initialize(installPlaywright: true);",
                    "Verify.Playwright: an IPage, IElementHandle or ILocator is verified as its html and as a",
                    "screenshot png, so both the markup and what the page looks like are covered.",
                    "installPlaywright: true runs the Playwright browser install once when the assembly loads,",
                    "so a clean machine or build agent downloads the browsers instead of failing.")
                {
                    Alternatives =
                    [
                        "VerifyPlaywright.Initialize();"
                    ]
                }
            ],
            ExternalRequirements =
            [
                new(
                    "A Playwright browser",
                    "Playwright drives a real Chromium, which has to be downloaded once. Initialize(installPlaywright: true) does it on assembly load, and the samples also need the site under test running.")
                {
                    Url = "https://playwright.dev/dotnet/docs/browsers",
                    WindowsInstall = "pwsh bin/Debug/net10.0/playwright.ps1 install",
                    LinuxInstall = "pwsh bin/Debug/net10.0/playwright.ps1 install --with-deps",
                    CannotRunUnattended = true
                }
            ],
            Usings = ["Microsoft.Playwright", "VerifyTestsPlaywright"],
            MinimalSamples =
            [
                new(
                    "Page",
                    """
                    // Waits for the site under test to start before driving the browser.
                    await SocketWaiter.Wait(port: 5000);

                    using var playwright = await Playwright.CreateAsync();
                    await using var browser = await playwright.Chromium.LaunchAsync(
                        new()
                        {
                            Args = ["--disable-lcd-text"]
                        });
                    var page = await browser.NewPageAsync();
                    await page.GotoAsync("http://localhost:5000");

                    await Verify(page);
                    """)
                {
                    Async = true,
                    SkipReason = "needs Chromium installed and the site under test listening on http://localhost:5000",
                    Comment =
                    [
                        "Two files are written: the page html and a png screenshot.",
                        "--disable-lcd-text turns off subpixel text rendering, which otherwise differs between",
                        "machines and makes the screenshot fail on a build agent."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "ScreenshotOnly",
                    """
                    await SocketWaiter.Wait(port: 5000);

                    using var playwright = await Playwright.CreateAsync();
                    await using var browser = await playwright.Chromium.LaunchAsync();
                    var page = await browser.NewPageAsync();
                    await page.GotoAsync("http://localhost:5000");

                    await Verify(page)
                        .PageScreenshotOptions(
                            new()
                            {
                                Quality = 50,
                                Type = ScreenshotType.Jpeg
                            },
                            screenshotOnly: true);
                    """)
                {
                    Async = true,
                    SkipReason = "needs Chromium installed and the site under test listening on http://localhost:5000",
                    Comment =
                    [
                        "screenshotOnly: true drops the html file, leaving only the image, for a page whose",
                        "markup is generated and therefore not worth reviewing.",
                        "Setting Path on the options throws: the file name is Verify's to decide."
                    ]
                },
                new(
                    "Locator",
                    """
                    await SocketWaiter.Wait(port: 5000);

                    using var playwright = await Playwright.CreateAsync();
                    await using var browser = await playwright.Chromium.LaunchAsync();
                    var page = await browser.NewPageAsync();
                    await page.GotoAsync("http://localhost:5000");

                    await Verify(page.Locator("#someId"));
                    """)
                {
                    Async = true,
                    SkipReason = "needs Chromium installed and the site under test listening on http://localhost:5000",
                    Comment =
                    [
                        "A locator narrows the snapshot to one part of the page, so an unrelated change",
                        "elsewhere does not re-record it. LocatorScreenshotOptions sets its image options."
                    ]
                }
            ],
            Notes =
            [
                "Each verification writes two files, the html and a png screenshot, unless `screenshotOnly: true` is passed.",
                "Screenshots vary between operating systems and renderers, so an image comparer, or `VerifierSettings.UseSsimForPng(...)`, is what keeps CI stable.",
                "`--disable-lcd-text` makes text rendering reproducible; existing screenshots have to be re-accepted once after adding it.",
                "Setting `Path` on any screenshot options throws \"ScreenshotOptions Path not supported.\".",
                "`SocketWaiter` is defined separately by each of the three headless browser packages, here in `VerifyTestsPlaywright`, so the usings are per file.",
                "Verify.Playwright, Verify.Puppeteer and Verify.Selenium ship from one repository and share a version.",
                "The package targets net10.0 only."
            ]
        },
        new()
        {
            Id = "Puppeteer",
            DisplayName = "Verify.Puppeteer",
            RepoUrl = "https://github.com/VerifyTests/Verify.HeadlessBrowsers",
            Description = "Verifies a PuppeteerSharp page or element as both its html and a screenshot png.",
            Category = ExtensionCategory.Web,
            Packages = [new("Verify.Puppeteer")],
            PluginType = "VerifyPuppeteer",
            ExternalRequirements =
            [
                new(
                    "A Chrome download",
                    "PuppeteerSharp downloads its own Chrome through BrowserFetcher on first use, and the samples also need the site under test running.")
                {
                    Url = "https://www.puppeteersharp.com/",
                    CannotRunUnattended = true
                }
            ],
            Usings = ["PuppeteerSharp"],
            MinimalSamples =
            [
                new(
                    "Page",
                    """
                    // Downloads the browser if it is not already there, then waits for the site under test.
                    await new BrowserFetcher(SupportedBrowser.Chrome).DownloadAsync();
                    await VerifyTests.Puppeteer.SocketWaiter.Wait(port: 5000);

                    await using var browser = await PuppeteerSharp.Puppeteer.LaunchAsync(
                        new()
                        {
                            Browser = SupportedBrowser.Chrome,
                            Args = ["--disable-lcd-text"]
                        });
                    await using var page = await browser.NewPageAsync();
                    await page.GoToAsync("http://localhost:5000");

                    await Verify(page);
                    """)
                {
                    Async = true,
                    SkipReason = "downloads a browser on first run, and needs the site under test listening on http://localhost:5000",
                    Comment =
                    [
                        "Two files are written: the page html and a png screenshot.",
                        "PuppeteerSharp's Puppeteer class and the VerifyTests.Puppeteer namespace share a name, so",
                        "both are written out in full rather than imported."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "Element",
                    """
                    await new BrowserFetcher(SupportedBrowser.Chrome).DownloadAsync();
                    await VerifyTests.Puppeteer.SocketWaiter.Wait(port: 5000);

                    await using var browser = await PuppeteerSharp.Puppeteer.LaunchAsync(
                        new()
                        {
                            Browser = SupportedBrowser.Chrome
                        });
                    await using var page = await browser.NewPageAsync();
                    await page.GoToAsync("http://localhost:5000");

                    // QuerySelectorAsync returns null when nothing matches, which a real test asserts on.
                    var element = await page.QuerySelectorAsync("#someId");
                    await Verify(element!);
                    """)
                {
                    Async = true,
                    SkipReason = "downloads a browser on first run, and needs the site under test listening on http://localhost:5000",
                    Comment =
                    [
                        "An element narrows the snapshot to one part of the page, so an unrelated change",
                        "elsewhere does not re-record it."
                    ]
                }
            ],
            Notes =
            [
                "Each verification writes two files: the html and a png screenshot.",
                "Screenshots vary between operating systems and renderers, so an image comparer, or `VerifierSettings.UseSsimForPng(...)`, is what keeps CI stable.",
                "`--disable-lcd-text` makes text rendering reproducible; existing screenshots have to be re-accepted once after adding it.",
                "PuppeteerSharp's `Puppeteer` class and the `VerifyTests.Puppeteer` namespace have the same name, so importing both makes every use of `Puppeteer` ambiguous.",
                "`SocketWaiter` is defined separately by each of the three headless browser packages, here in `VerifyTests.Puppeteer`.",
                "The assembly is not strong named, and the package targets net10.0 only."
            ]
        },
        new()
        {
            Id = "Selenium",
            DisplayName = "Verify.Selenium",
            RepoUrl = "https://github.com/VerifyTests/Verify.HeadlessBrowsers",
            Description = "Verifies a Selenium WebDriver page or element as both its html and a screenshot png.",
            Category = ExtensionCategory.Web,
            Packages =
            [
                new("Verify.Selenium"),
                new("Selenium.WebDriver.ChromeDriver") {Comment = "the chromedriver executable, copied to the output directory"}
            ],
            PluginType = "VerifySelenium",
            ExternalRequirements =
            [
                new(
                    "Chrome and chromedriver",
                    "Selenium drives an installed Chrome through chromedriver, whose version has to match the browser, and the samples also need the site under test running.")
                {
                    Url = "https://www.selenium.dev/documentation/webdriver/troubleshooting/errors/driver_location/",
                    CannotRunUnattended = true
                }
            ],
            Usings = ["OpenQA.Selenium", "OpenQA.Selenium.Chrome", "VerifyTests.Selenium"],
            MinimalSamples =
            [
                new(
                    "Driver",
                    """
                    // Waits for the site under test to start before driving the browser.
                    await SocketWaiter.Wait(port: 5000);

                    var options = new ChromeOptions();
                    options.AddArgument("--headless=new");
                    options.AddArgument("--disable-lcd-text");
                    using var driver = new ChromeDriver(options);
                    driver.Navigate().GoToUrl("http://localhost:5000");
                    driver.WaitForIsReady();

                    await Verify(driver);
                    """)
                {
                    Async = true,
                    SkipReason = "needs Chrome and a matching chromedriver, and the site under test listening on http://localhost:5000",
                    Comment =
                    [
                        "Two files are written: the page html and a png screenshot.",
                        "WaitForIsReady comes with the package and blocks until the document has finished loading,",
                        "which is what keeps the html from being captured half rendered."
                    ]
                }
            ],
            VerboseSamples =
            [
                new(
                    "Element",
                    """
                    await SocketWaiter.Wait(port: 5000);

                    var options = new ChromeOptions();
                    options.AddArgument("--headless=new");
                    using var driver = new ChromeDriver(options);
                    driver.Navigate().GoToUrl("http://localhost:5000");
                    driver.WaitForIsReady();

                    await Verify(driver.FindElement(By.Id("someId")));
                    """)
                {
                    Async = true,
                    SkipReason = "needs Chrome and a matching chromedriver, and the site under test listening on http://localhost:5000",
                    Comment =
                    [
                        "An element narrows the snapshot to one part of the page. GetSource() returns the same",
                        "markup as a string, for a test that wants to assert on it instead."
                    ]
                }
            ],
            Notes =
            [
                "Each verification writes two files: the html and a png screenshot.",
                "Screenshots vary between operating systems and renderers, so an image comparer, or `VerifierSettings.UseSsimForPng(...)`, is what keeps CI stable.",
                "`--disable-lcd-text` makes text rendering reproducible; existing screenshots have to be re-accepted once after adding it.",
                "The `Selenium.WebDriver.ChromeDriver` package puts a chromedriver in the output directory, so nothing has to be on the PATH, but its version has to match the installed Chrome.",
                "`SocketWaiter` is defined separately by each of the three headless browser packages, here in `VerifyTests.Selenium`.",
                "The assembly is not strong named, and the package targets net10.0 only."
            ]
        }
    ];
}
