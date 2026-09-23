# Verify extension catalogue - batch C

Source of truth: local clones under `D:\Code\VerifyTests\`. Versions read from `src/Directory.Build.props` and `src/Directory.Packages.props` on disk.

## Verify.NewtonsoftJson
- **NuGet package id(s)**: `Verify.NewtonsoftJson` (single package)
- **Current version** (from src/Directory.Build.props <Version>): `1.1.0`
- **Target frameworks**: `net48;net6.0;net7.0;net8.0`
- **One-line description**: Adds Verify support for converting Newtonsoft.Json types (`JObject` and `JArray`) so they serialize cleanly into snapshots.
- **Tech tags**: json, newtonsoft, serialization, jobject, jarray
- **Third-party dependencies**: `Newtonsoft.Json` 13.0.4; `Verify` 33.1.0. (PrivateAssets/build-only: `Polyfill` 11.4.0, `ProjectDefaults` 1.0.181, `Microsoft.Sbom.Targets` 4.1.12.)
- **Initialize API**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyNewtonsoftJson.Initialize();
```
  `VerifyNewtonsoftJson.Initialize()` takes no parameters. Throws `"Already Initialized"` if called twice, calls `InnerVerifier.ThrowIfVerifyHasBeenRun()`, then `VerifierSettings.AddExtraSettings` adding `JArrayConverter` and `JObjectConverter`.
- **Requires InitializePlugins-compatible pattern?**: yes (`public static bool Initialized { get; private set; }` + `public static void Initialize()`)
- **Minimal usage**: the readme shows only the enable snippet; everything else is plain `Verify(...)`:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyNewtonsoftJson.Initialize();
```
  Representative tests (`src/Tests/Tests.cs`):
```cs
[Fact]
public Task TestJToken() =>
    Verify(JToken.Parse(json));

[Fact]
public Task TestJArray() =>
    Verify(JArray.Parse(jsonArray));
```
- **Verbose / edge-case APIs**: none. `VerifyNewtonsoftJson` exposes only `Initialized` and `Initialize()`. No settings extension methods, no scrubbers, no file/stream overloads.
- **Interactions with other Verify extensions**: readme says nothing. Context for a wizard: this is the Newtonsoft counterpart to `Verify.SystemJson` (referenced in Verify.Sample); they solve the same "json DOM type" problem for different json stacks. Verify's serializer is Argon (a Newtonsoft fork), so raw `Newtonsoft.Json` types are not handled out of the box - that is what this adds.
- **MSBuild / project requirements**: none beyond the transitive `Newtonsoft.Json`. Cross-platform. `PackageRequireLicenseAcceptance=true`.
- **Sample verified output** (`src/Tests/Tests.TestJArray.verified.txt`):
```txt
[
  Small,
  Medium,
  Large
]
```
- **Notes for the wizard**: `<PackageTags>` in Directory.Build.props are wrong (`NodaTime, Verify` - copy/paste from Verify.NodaTime). Tests use xunit.v3 + `Verify.XunitV3`. TFMs stop at net8.0. Test `ModuleInitializer` pairs `VerifyNewtonsoftJson.Initialize()` with a second `[ModuleInitializer]` calling `VerifierSettings.InitializePlugins()`.

## Verify.NodaTime
- **NuGet package id(s)**: `Verify.NodaTime` (single package)
- **Current version**: `2.3.0`
- **Target frameworks**: `net48;net6.0;net7.0;net8.0`
- **One-line description**: Adds Verify support for NodaTime types - date/time values are scrubbed to counter-based placeholders (e.g. `LocalDateTime_1`) so snapshots stay deterministic.
- **Tech tags**: nodatime, dates, times, scrubbing, datetime
- **Third-party dependencies**: `NodaTime` 3.3.4; `Argon.NodaTime` 0.37.0; `Verify` 33.1.0. (PrivateAssets: `Polyfill` 11.4.0, `ProjectDefaults` 1.0.181, `Microsoft.Sbom.Targets` 4.1.12.)
- **Initialize API**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyNodaTime.Initialize();
```
  `VerifyNodaTime.Initialize()` - no parameters. Throws if already initialized, calls `InnerVerifier.ThrowIfVerifyHasBeenRun()`, `CounterContext.Init()`, then registers converters for `AnnualDate`, `Instant`, `LocalDate`, `LocalDateTime`, `OffsetDate`, `OffsetDateTime`, `ZonedDateTime`, `YearMonth`, `DateInterval`.
  Global opt-out variant (readme, `src/StaticSettingsTests/ModuleInitializer.cs`):
```cs
[ModuleInitializer]
public static void Init()
{
    VerifyNodaTime.DontScrub();
    VerifyNodaTime.Initialize();
}
```
- **Requires InitializePlugins-compatible pattern?**: yes
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyNodaTime.Initialize();
```
```cs
[Fact]
public Task ScrubbingExample()
{
    var target = new Person
    {
        Dob = LocalDateTime.FromDateTime(DateTime.Now)
    };

    return Verify(target);
}
```
- **Verbose / edge-case APIs**:
  - `VerifyNodaTime.DontScrub()` - static and global; disables NodaTime scrubbing for the whole assembly. Readme: "To disable scrubbing globally use `VerifyNodaTime.DontScrub`". Call it before `Initialize()` as the snippet does.
  - `DontScrubNodaTimes(this VerifySettings settings)` - per-test, instance settings form.
  - `DontScrubNodaTimes(this SettingsTask settings)` - per-test, fluent form:
```cs
[Fact]
public Task DisableExample()
{
    var target = new Person
    {
        Dob = LocalDateTime.FromDateTime(new(2010, 2, 10))
    };

    return Verify(target)
        .DontScrubNodaTimes();
}
```
- **Interactions with other Verify extensions**: readme names none. It depends on `Argon.NodaTime` (NodaTime support for Argon, Verify's serializer) and layers on Verify's own date/time counter scrubbing rather than replacing it - hence the unscrubbed output still shows `DateTimeOffset_1`.
- **MSBuild / project requirements**: none special; cross-platform. `TreatWarningsAsErrors=true`, `PackageRequireLicenseAcceptance=true`.
- **Sample verified output** (scrubbed):
```txt
{
  Dob: LocalDateTime_1
}
```
  With `DontScrubNodaTimes()`:
```txt
{
  Dob: DateTimeOffset_1
}
```
- **Notes for the wizard**: Two test projects - `src/Tests` (default) and `src/StaticSettingsTests` (the global `DontScrub()` path), because `DontScrub()` is process-wide and cannot be toggled per test. xunit.v3 + `Verify.XunitV3`.

## Verify.OpenTelemetry
- **NuGet package id(s)**: `Verify.OpenTelemetry` (single package)
- **Current version**: `1.0.0`
- **Target frameworks**: `net10.0` (single TFM)
- **One-line description**: Extends Verify to record and verify OpenTelemetry types - `System.Diagnostics.Activity` spans captured via a global `ActivityListener`, plus `LogRecord` instances.
- **Tech tags**: opentelemetry, tracing, telemetry, activity, diagnostics, logging, observability
- **Third-party dependencies**: `OpenTelemetry` 1.19.0; `Verify` 33.1.0. (PrivateAssets: `Polyfill` 11.4.0, `ProjectDefaults` 1.0.181, `Microsoft.Sbom.Targets` 4.1.12.) Test-only: `OpenTelemetry.Exporter.InMemory` 1.19.0, `Microsoft.Extensions.Logging` 10.0.12.
- **Initialize API**:
```cs
[ModuleInitializer]
public static void Initialize() =>
    VerifyOpenTelemetry.Initialize();
```
  `VerifyOpenTelemetry.Initialize()` - no parameters. Installs a process-wide `ActivityListener` with `ShouldListenTo = _ => true` and `Sample = ActivitySamplingResult.AllDataAndRecorded`, whose `ActivityStopped` calls `Recording.TryAdd("activity", activity)`. Registers `ActivityConverter`, `ActivityEventConverter`, `ActivityLinkConverter`, `ActivityContextConverter`, `LogRecordConverter`.
- **Requires InitializePlugins-compatible pattern?**: yes
- **Minimal usage**:
```cs
[Fact]
public Task Usage()
{
    Recording.Start();
    using var source = new ActivitySource("TestSource");

    using (var activity = source.StartActivity("MyOperation"))
    {
        activity!.SetTag("key1", "value1");
        activity.SetTag("key2", 42);
    }

    return Verify("result");
}
```
- **Verbose / edge-case APIs**: the class exposes only `Initialized` + `Initialize()`; the rest of the readme is core Verify surface used with it.
  - `Recording.Start()` - begins capturing. Readme: "Call `Recording.Start()` to begin listening. All activities from any `ActivitySource` will be captured by default."
  - LogRecord verification via the OpenTelemetry `InMemoryExporter` - no Verify-specific API, just `Verify(logRecords)`:
```cs
[Fact]
public Task LogRecordVerification()
{
    var logRecords = new List<LogRecord>();
    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddOpenTelemetry(options =>
        {
            options.AddInMemoryExporter(logRecords);
        });
    });

    var logger = loggerFactory.CreateLogger("TestCategory");
    logger.LogInformation("Hello {Name}", "World");

    return Verify(logRecords);
}
```
  - Serialization conventions (readme). Activities: `OperationName` is the JSON property key; `DisplayName` only if different from `OperationName`; `Kind` only if not `Internal`; `Status`/`StatusDescription` only if not `Unset`; `Tags`, `Events`, `Links`, `Baggage` when present; `Id`, `TraceId`, `SpanId`, `ParentSpanId`, `Duration`, `StartTimeUtc`, `Source` omitted as non-deterministic. LogRecords: `Timestamp`, `TraceId`, `SpanId` omitted; `CategoryName`, `LogLevel`, `Body`, `FormattedMessage` when present; `EventId` only if non-default; `Exception` and `Attributes` when present.
- **Interactions with other Verify extensions**: readme says nothing explicit. Behavioural note: it writes into the shared `Recording` bucket under the key `"activity"` (Verify.Serilog uses `"log"`), so recording-based extensions coexist and all appear in the same snapshot.
- **MSBuild / project requirements**: net10.0 only, so the consuming test project must target net10.0+. The `ActivityListener` is process-wide, so parallel tests can cross-capture activities.
- **Sample verified output**:
```txt
{
  target: result,
  activity: {
    MyOperation: {
      Tags: {
        key1: value1,
        key2: 42
      }
    }
  }
}
```
- **Notes for the wizard**: Newest package in this batch (v1.0.0). xunit.v3 + `Verify.XunitV3`. `Initialize()` must run before any `Verify` (guarded by `InnerVerifier.ThrowIfVerifyHasBeenRun()`).

## Verify.OpenXml
- **NuGet package id(s)**: `Verify.OpenXml` (project/assembly name; readme badge and NuGet links spell it `Verify.OpenXML` - NuGet ids are case-insensitive, so it is one package). No explicit PackageId in the csproj.
- **Current version**: `1.27.0`
- **Target frameworks**: `net472;net48;net8.0;net9.0;net10.0` (PNG rendering compiled in on `net10.0` only)
- **One-line description**: Extends Verify to verify Word (docx), Excel (xlsx) and PowerPoint (pptx) documents via the Open-XML-SDK - text/CSV extraction, document properties, a deterministic binary copy, and optional per-page PNG rendering.
- **Tech tags**: openxml, office, word, docx, excel, xlsx, powerpoint, pptx, documents, spreadsheets, presentations
- **Third-party dependencies**: `DocumentFormat.OpenXml` 3.5.1; `DeterministicIoPackaging` 0.31.0; `Verify` 33.1.0; `Morph` 1.13.3 (referenced only on `net10.0`). PrivateAssets: `Polyfill` 11.4.0, `ProjectDefaults` 1.0.181, `Microsoft.Sbom.Targets` 4.1.12. Opt-in in the consuming test project, not shipped: `Morph.Skia` 1.13.3 or `Morph.ImageSharp` 1.13.3.
- **Initialize API**:
```cs
[ModuleInitializer]
public static void Initialize() =>
    VerifyOpenXml.Initialize();
```
  `VerifyOpenXml.Initialize()` - no parameters. Registers three stream converters (`xlsx`, `docx`, `pptx`) and three file converters (`SpreadsheetDocument`, `WordprocessingDocument`, `PresentationDocument`).
  Two static properties configure rendering and are set separately (the repo own ModuleInitializer sets both):
  - `VerifyOpenXml.FontDirectory` (`string?`) - directory holding fonts to render with; when set, only fonts found there are used and a missing face throws rather than silently resolving elsewhere. Null = machine fonts (non-reproducible).
  - `VerifyOpenXml.UseLetterPageSize` (`bool?`) - paper when the document states none: `true` = US Letter, `false` = A4, null = machine region.
```cs
[ModuleInitializer]
public static void InitializeRendering()
{
    VerifyOpenXml.FontDirectory = Path.Combine(ProjectDir(), "..", "Fonts");
    VerifyOpenXml.UseLetterPageSize = false;
}
```
- **Requires InitializePlugins-compatible pattern?**: yes
- **Minimal usage**:
```cs
[Test]
public Task VerifyExcel() =>
    VerifyFile("sample.xlsx");
```
```cs
[Test]
public Task VerifyWord() =>
    VerifyFile("sample.docx")
        .Snapshot(
            """
            {
              Properties: {
                Subject: Test Subject,
                Title: Sample Document
              }
            }
            """);
```
```cs
[Test]
public Task VerifyPowerpoint() =>
    VerifyFile("sample.pptx")
        .Snapshot(
            """
            {
              Properties: {
                Title: Sample Presentation
              },
              SlideCount: 1
            }
            """);
```
- **Verbose / edge-case APIs** (this package adds **no** `VerifySettings`/`SettingsTask` extension methods of its own - all of the below is core Verify used with it):
  - Stream overloads, one per extension: `Verify(stream, "xlsx")`, `Verify(stream, "docx")`, `Verify(stream, "pptx")`:
```cs
[Test]
public Task VerifyExcelStream()
{
    var stream = new MemoryStream(File.ReadAllBytes("sample.xlsx"));
    return Verify(stream, "xlsx");
}
```
  - Document-object overloads: `Verify(SpreadsheetDocument)`, `Verify(WordprocessingDocument)`, `Verify(PresentationDocument)`:
```cs
[Test]
public async Task VerifySpreadsheetDocument()
{
    await using var stream = File.OpenRead("sample.xlsx");
    using var reader = SpreadsheetDocument.Open(stream, false);
    await Verify(reader);
}
```
  - `.ExcludeTargets("xlsx")` (also `"docx"` / `"pptx"`) - readme: "Building the deterministic package is expensive, and committing it is not always wanted. `ExcludeTargets` drops it from a verification and skips the build, while the info, text, csv, and rendered pages still verify". Global form: `VerifierSettings.ExcludeTargets("xlsx")` at initialization.
```cs
// Skips the .verified.xlsx (and building it), keeping the info and csv sheets.
[Test]
public Task ExcludeExcel() =>
    VerifyFile("sample.xlsx")
        .ExcludeTargets("xlsx");
```
  - `.UniqueForRuntime()` - for binary package output across TFMs: "the binary output may differ due to Deflate compression implementation differences. The XML content within entries is identical - only the compressed bytes differ."
```cs
await Verify(stream, extension: "xlsx")
    .UniqueForRuntime();
```
  - `.UniqueForOSPlatform()` - recommended for cross-platform CI once PNG rendering is enabled.
```cs
await Verify(stream, "docx")
    .UniqueForOSPlatform();
```
  - `Verifier.DerivePathInfo(...)` in the ModuleInitializer - documented pattern for running one linked test suite against both render backends (`src/Tests.Skia`, `src/Tests.ImageSharp`).
  - Opt-in PNG rendering: add exactly one of `Morph.Skia` or `Morph.ImageSharp` as a PackageReference in the test project. "The backend is detected at runtime by probing for the assembly. No code changes are needed in `ModuleInitializer.cs` - the existing `VerifyOpenXml.Initialize()` call picks it up automatically." Rules: neither referenced = rendering silently skipped; one = used for all verifications; "Both backends referenced - an exception is thrown on the first verification with a clear message. Pick one."
  - Page semantics: "**Word** - one page per laid-out page of the document. **PowerPoint** - one page per slide, in `p:sldIdLst` order. **Excel** - pages come from the print layout rather than the sheet".
- **Interactions with other Verify extensions**: the readme discusses render backends rather than sibling Verify packages: "The base [`Morph`](https://nuget.org/packages/Morph) package is referenced automatically by Verify.OpenXml on `net10.0`. To turn on rendering, add **exactly one** backend package to the test project"; and for image tolerance, "Consider [PNG SSIM comparer](https://github.com/VerifyTests/Verify/blob/main/docs/comparer.md#png-ssim-comparer) for tolerance-based image diffing." Conflict not stated in the readme but wizard-relevant: it claims the `docx` stream-converter extension, which overlaps with Verify.Pandoc (which also registers a `docx` converter) - the two cannot both own `docx` in one test project.
- **MSBuild / project requirements**: PNG rendering requires `net10.0` ("Rendering is only available on `net10.0` because Morph targets `net10.0` only"); on `net472`, `net48`, `net8.0`, `net9.0` the rendering code is conditionally compiled out. Sample documents need a None Update item with CopyToOutputDirectory=PreserveNewest. InternalsVisibleTo Verify.OpenXml.Tests with a public key. NoWarn includes CA1416. Rendered PNGs are font/platform sensitive: "Generate and commit `.verified.png` files from a single canonical machine (often a CI agent)."
- **Sample verified output** - Excel emits a `.verified.csv` per sheet:
```csv
0,First Name,Last Name,Gender,Country,Date,Age,Id,Formula
1,Dulce,Abril,Female,United States,2017-10-15,32,1562,G2+H21594 (G2+H2)
2,Mara,Hashimoto,Female,Great Britain,2016-08-16,25,1582,1607
3,Philip,Gent,Male,France,2015-05-21,36,2587,2623
```
  plus a `.verified.txt` info file (`{ Sheets: [ { Name: Sheet1, Columns: [...] } ] }`), a `.verified.xlsx`, and with a backend indexed `#00.verified.png` pages. Word text lands in `#01.verified.txt`:
```txt
Hello World! This is a sample Word document.
This is the second paragraph with some more text.
```
- **Notes for the wizard**: Three test projects (`Verify.OpenXml.Tests` with no backend, `Tests.Skia`, `Tests.ImageSharp`) sharing linked source - running only Tests.csproj is not the full suite. Repo has a `claude.md`. `src/nuget.md` has a stale copy/paste link to `Verify.Sylvan.Data.Excel`. Tests use NUnit + `Verify.NUnit` and `VerifierSettings.Inline(maxLines: 10, applyMaxLinesToExisting: true)`. Every verification produces several target files (info txt, text/csv, binary, PNGs).

## Verify.PDFium
- **NuGet package id(s)**: `Verify.PDFium` (single package)
- **Current version**: `1.4.1`
- **Target frameworks**: `net10.0` (single TFM)
- **One-line description**: Extends Verify to verify PDF documents via PDFium - page count, per-page size and extracted text, document info dictionary, a normalized deterministic `.verified.pdf`, and a PNG render of every page.
- **Tech tags**: pdf, pdfium, documents, rendering, images
- **Third-party dependencies**: `Morph.PDFium` 1.3.1; `DeterministicPdf` 2.0.2; `Verify` 33.1.0. PrivateAssets: `ProjectDefaults` 1.0.181, `Microsoft.Sbom.Targets` 4.1.12.
- **Initialize API**:
```cs
[ModuleInitializer]
public static void Initialize() =>
    VerifyPDFium.Initialize();
```
  Actual signature is `public static void Initialize(double dpi = 96)`. Readme: "`Initialize` optionally takes the render resolution: `VerifyPDFium.Initialize(dpi: 150)`. The default 96 dpi renders an A4 page at 794 x 1123." Throws `ArgumentOutOfRangeException` when `dpi <= 0`. Registers a stream converter for `pdf`.
- **Requires InitializePlugins-compatible pattern?**: yes (Initialize has an optional `dpi` parameter, so a plugin-style parameterless call uses 96 dpi)
- **Minimal usage**:
```cs
[Test]
public Task VerifyPdf() =>
    VerifyFile("sample.pdf");
```
```cs
[Test]
public Task VerifyPdfStream()
{
    var stream = new MemoryStream(File.ReadAllBytes("sample.pdf"));
    return Verify(stream, "pdf");
}
```
- **Verbose / edge-case APIs**:
  - `VerifyPDFium.Initialize(double dpi = 96)` - render resolution for the page images.
  - `ExcludePdfDocument(this SettingsTask settings)` - drops the `.verified.pdf` for that verification, keeping rendered pages and info file. Readme: "Some pdf producers embed non-deterministic bytes that cannot be neutralized. For example [Aspose.Cells](https://products.aspose.com/cells/) always embeds the machine's system fonts (it has no way to restrict font resolution to a bundled set), so the pdf bytes differ from one machine to the next even for the same input."
```cs
[Test]
public Task ExcludePdfDocument() =>
    VerifyFile("sample.pdf")
        .ExcludePdfDocument();
```
  - `SkipPdfNormalization(this SettingsTask settings)` - snapshots the pdf bytes exactly as produced. Readme: "Only skip it when the producer is genuinely deterministic. Without normalization a freshly generated pdf carries a wall-clock `/CreationDate` and a fresh `/ID`, so the snapshot differs on every run." And: "The XMP canonicalization is worth calling out, because it is the pass that changes bytes even for an already-deterministic producer: it collapses the packet's whitespace."
```cs
[Test]
public Task SkipPdfNormalization() =>
    VerifyFile("sample.pdf")
        .SkipPdfNormalization();
```
  - Only `SettingsTask` (fluent) forms exist - there are no `VerifySettings` instance overloads for `ExcludePdfDocument`/`SkipPdfNormalization` (unlike Verify.PdfPig and Verify.QuestPDF, which ship both).
  - Output naming: PNG page targets are `#page_0001.verified.png`, `#page_0002.verified.png`, ... The pdf target sets `BypassComparersForSubsequentOnDifference = true`.
- **Interactions with other Verify extensions**: the readme names no sibling Verify packages, but it registers the `pdf` stream converter, which directly collides with Verify.PdfPig (also a `pdf` stream converter) - only one of the two can own `pdf` in a given test project. Verify.QuestPDF is complementary rather than colliding (it registers a file converter for `IDocument`). The readme does discuss the pdf-producer overlap: "Some pdf producers embed non-deterministic bytes that cannot be neutralized. For example Aspose.Cells always embeds the machine's system fonts". The repo ModuleInitializer pairs it with `UseSsimForPng` so page renders compare with tolerance:
```cs
[ModuleInitializer]
public static void InitializeOther()
{
    VerifierSettings.InitializePlugins();
    VerifierSettings.UseSsimForPng();
}
```
- **MSBuild / project requirements**: net10.0 only. Native PDFium binaries arrive via `Morph.PDFium`, which "wraps the prebuilt PDFium binaries from [pdfium-binaries](https://github.com/bblanchon/pdfium-binaries) (Windows, Linux, and macOS)" - no image-library dependency. Test pdfs need a None Update item with CopyToOutputDirectory=PreserveNewest. InternalsVisibleTo Tests.
- **Sample verified output** (`src/Tests/Samples.VerifyPdf.verified.txt`):
```txt
{
  PageCount: 1,
  Properties: {
    CreationDate: DateTimeOffset_1,
    Creator: Morph,
    ModDate: DateTimeOffset_1,
    Producer: PDFsharp 6.2.4
  },
  Pages: [
    {
      Width: 612.0,
      Height: 792.0,
      Text: Hello, World! This is a simple paragraph.
    }
  ]
}
```
- **Notes for the wizard**: Readme claims render determinism: "Rendering is deterministic for a given Morph.PDFium version: the same input produces byte-identical PNGs on every machine and OS, and no image library dependency is added." Each verification emits three kinds of target (txt + pdf + one png per page), so snapshot counts grow quickly for multi-page documents. Repo has a `claude.md` and a `nugets` folder. NUnit + `Verify.NUnit`.

## Verify.Pandoc
- **NuGet package id(s)**: `Verify.Pandoc` (single package)
- **Current version**: `0.1.1`
- **Target frameworks**: `net8.0;net9.0`
- **One-line description**: Extends Verify to convert documents to markdown for verification via PandocNet.
- **Tech tags**: pandoc, documents, markdown, docx, rtf, word, conversion
- **Third-party dependencies**: `Pandoc` 6.0.2 (PandocNet); `Verify` 33.1.0. PrivateAssets: `ProjectDefaults` 1.0.181, `Microsoft.Sbom.Targets` 4.1.12.
- **Initialize API**:
```cs
[ModuleInitializer]
public static void Initialize() =>
    VerifyPandoc.Initialize();
```
  `VerifyPandoc.Initialize()` - no parameters. Registers stream converters for `docx` (`DocxIn`) and `rtf` (`RtfIn`), each producing a single `md` target via `PandocInstance.ConvertToText<T, PandocMdOut>(stream)`.
- **Requires InitializePlugins-compatible pattern?**: yes
- **Minimal usage**:
```cs
[Test]
public Task VerifyRtf() =>
    VerifyFile("sample.rtf");
```
```cs
[Test]
public Task VerifyWord() =>
    VerifyFile("sample.docx");
```
- **Verbose / edge-case APIs**: the class exposes only `Initialized` and `Initialize()`. No settings extension methods, no scrubbers. The only other documented form is the stream overload per supported extension:
```cs
[Test]
public Task VerifyRtfStream()
{
    var stream = new MemoryStream(File.ReadAllBytes("sample.rtf"));
    return Verify(stream, "rtf");
}
```
```cs
[Test]
public Task VerifyWordStream()
{
    var stream = new MemoryStream(File.ReadAllBytes("sample.docx"));
    return Verify(stream, "docx");
}
```
  Readme lists the supported inputs: "Currently supported documents: docx, rtf".
- **Interactions with other Verify extensions**: readme says nothing. Wizard-relevant: it owns the `docx` stream-converter extension, which collides with Verify.OpenXml (which also registers `docx`, plus `xlsx`/`pptx`). Pick one per test project. Output extension is `md`, so it does not collide with the pdf family.
- **MSBuild / project requirements**: requires the Pandoc tool at runtime - PandocNet shells out to a pandoc executable, so a pandoc install (or the PandocNet-bundled binary) must be available on the machine/CI agent. Sample files must be copied to output. `Papyrine_SponsorshipLicenseIgnored=true` is set in Directory.Build.props.
- **Sample verified output** (`.verified.md`, first lines of `Samples.VerifyWord.verified.md`):
```md
# Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nunc ac faucibus odio.

Vestibulum neque massa, scelerisque sit amet ligula eu, congue molestie
mi. Praesent ut varius sem. Nullam at porttitor arcu, nec lacinia nisi.
...

- **Maecenas non lorem quis tellus placerat varius.**

- *Nulla facilisi.*
```
- **Notes for the wizard**: Pre-1.0 (0.1.1). The readme snapshot samples are very long (a whole lorem-ipsum document), so generated docs should truncate. NUnit + `Verify.NUnit`. The repo test ModuleInitializer also calls `VerifierSettings.UseSsimForPng()` and `VerifierSettings.InitializePlugins()`.

## Verify.ParametersHashing
- **NuGet package id(s)**: `Verify.ParametersHashing` (single package)
- **Current version**: `1.0.0`
- **Target frameworks**: `net472;net48;net8.0;net9.0`
- **One-line description**: Extends Verify to hash test parameters into the snapshot file name (XxHash64) instead of stringifying them, avoiding OS path-length limits.
- **Tech tags**: naming, parameters, hashing, xxhash, file-names, long-paths
- **Third-party dependencies**: `System.IO.Hashing` 10.0.12; `Verify` 33.1.0. PrivateAssets: `Polyfill` 11.4.0, `ProjectDefaults` 1.0.181, `Microsoft.Sbom.Targets` 4.1.12.
- **Initialize API**: `VerifyParametersHashing.Initialize()` exists and simply sets `Initialized = true` - it registers nothing. The readme does **not** show a ModuleInitializer, and the repo test project ModuleInitializer only calls `VerifierSettings.InitializePlugins()`. The feature works purely through the `HashParameters()` extension methods.
```cs
public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void OtherInitialize() =>
        VerifierSettings.InitializePlugins();
}
```
- **Requires InitializePlugins-compatible pattern?**: yes (`Initialized` + `Initialize()` are both present, though `Initialize()` is a no-op beyond setting the flag)
- **Minimal usage** - instance form:
```cs
[TestCase("Value1")]
[TestCase("Value2")]
public Task HashParametersUsage(string arg)
{
    var settings = new VerifySettings();
    settings.HashParameters();
    return Verify(arg, settings);
}
```
  fluent form:
```cs
[TestCase("Value1")]
[TestCase("Value2")]
public Task HashParametersUsageFluent(string arg) =>
    Verify(arg)
        .HashParameters();
```
- **Verbose / edge-case APIs**:
  - `HashParameters(this VerifySettings settings)` - instance form; calls `settings.UseParametersAppender(...)`.
  - `HashParameters(this SettingsTask settings)` - fluent form, marked `[Pure]`.
  - Readme: "Parameters can be hashed as an alternative to being stringified. This is useful when the parameters are large and could potentially generate file names that exceed allowances of the OS." and "[XxHash64](https://learn.microsoft.com/en-us/dotnet/api/system.io.hashing.xxhash64) is used to perform the hash."
  - No global/static opt-in is exposed - hashing is per-verification only.
- **Interactions with other Verify extensions**: readme says nothing. Mechanically it overrides Verify's parameter appender (`UseParametersAppender`), so it conflicts with any other extension or user code that sets a custom parameters appender; it internally still calls `VerifierSettings.AppendParameter` to preserve core naming semantics before hashing.
- **MSBuild / project requirements**: none special. Cross-platform. `TreatWarningsAsErrors=true`.
- **Sample verified output**: this extension changes file *names*, not content. Verified files in `src/Tests` look like:
```txt
ParametersHashSample.HashParametersUsage_25a91d2c54f27235.verified.txt
ParametersHashSample.HashParametersUsage_80cceeb56ef4cffc.verified.txt
ParametersHashSample.HashParametersUsageFluent_25a91d2c54f27235.verified.txt
ParametersHashSample.HashParametersUsageFluent_80cceeb56ef4cffc.verified.txt
```
  (the hash is lowercase hex of the XxHash64 of the joined `_name=value` string)
- **Notes for the wizard**: This is a naming/infrastructure extension, not a type/format extension - it has no converters, no comparers and no scrubbers, so a docs template geared to "verify type X" does not fit. Both static-settings and per-test instance APIs exist. NUnit + `Verify.Nunit` test project. Source suppresses `AppendParameter`/`UseParametersAppender` analyzer warnings.

## Verify.PdfPig
- **NuGet package id(s)**: `Verify.PdfPig` (single package)
- **Current version**: `2.6.0`
- **Target frameworks**: `net48;net6.0;net7.0;net8.0;net8.0;net10.0` (note the duplicated `net8.0` in the csproj `TargetFrameworks`)
- **One-line description**: Extends Verify to verify PDFs via PdfPig - converts a pdf to a text/info snapshot (document information, page count, per-page size, rotation and extracted text) plus a normalized `.verified.pdf`.
- **Tech tags**: pdf, pdfpig, documents, text-extraction
- **Third-party dependencies**: `PdfPig` 0.1.16; `DeterministicPdf` 2.0.2; `Verify` 33.1.0. PrivateAssets: `Polyfill` 11.4.0, `ProjectDefaults` 1.0.181, `Microsoft.Sbom.Targets` 4.1.12.
- **Initialize API**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyPdfPig.Initialize();
```
  `VerifyPdfPig.Initialize()` - no parameters. Adds a `DocumentInformationConverter` extra setting and registers a stream converter for `pdf`.
- **Requires InitializePlugins-compatible pattern?**: yes
- **Minimal usage**:
```cs
[Test]
public Task VerifyPdf() =>
    VerifyFile("sample.pdf")
        .PagesToInclude(2);
```
```cs
[Test]
public Task VerifyPdfStream() =>
    Verify(File.OpenRead("sample.pdf"), "pdf");
```
- **Verbose / edge-case APIs**:
  - `PagesToInclude(this VerifySettings settings, int count)` and `PagesToInclude(this SettingsTask settings, int count)` - "Limits the number of pages included in the info/text snapshot to `count`. The `pdf` snapshot is unaffected and always contains the full source document."
  - `PdfPigParsingOptions(this VerifySettings settings, ParsingOptions options)` and the `SettingsTask` overload - passes PdfPig `ParsingOptions` (e.g. passwords, lenient parsing) to `PdfDocument.Open`. Not covered in the readme; present in `PdfPigSettings.cs`.
  - `SkipPdfNormalization(this VerifySettings settings)` and the `SettingsTask` overload - "Snapshots the pdf bytes exactly as produced, skipping the normalization that neutralizes the trailer `/ID`, the `/CreationDate` and `/ModDate`, and the XMP dates and identifiers." Not covered in the readme; present in `PdfPigSettings.cs`.
  - `.ExcludeTargets("pdf")` (core Verify) - readme: "The source pdf is included in the snapshot as a `.verified.pdf`. Where committing it is not wanted, `ExcludeTargets` drops it from a verification and skips normalizing it, while the info and text still verify". Global form: `VerifierSettings.ExcludeTargets("pdf")` at initialization.
```cs
[Test]
public Task ExcludePdf() =>
    VerifyFile("sample.pdf")
        .ExcludeTargets("pdf");
```
- **Interactions with other Verify extensions**: readme names none, but this registers the `pdf` stream converter and therefore collides head-on with Verify.PDFium (same extension, different engine). PdfPig is the text-extraction option (no rendering, fully managed); PDFium is the render-plus-text option (native binaries, PNG pages). Verify.QuestPDF is complementary (file converter on `IDocument`, then emits `pdf`/`png` targets).
- **MSBuild / project requirements**: none native - PdfPig is fully managed. Test pdfs need CopyToOutputDirectory. InternalsVisibleTo Tests. `ImplicitUsings` enabled.
- **Sample verified output** (trimmed from the readme `Samples.VerifyPdf.verified.txt`):
```txt
{
  Information: {
    Creator: Writer,
    Producer: LibreOffice 4.2,
    CreationDate: DateTimeOffset_1
  },
  PageCount: 4,
  Pages: [
    {
      Size: A4,
      Text:
Lorem ipsum
...
    }
  ]
}
```
- **Notes for the wizard**: `PagesToInclude` trims only the info/text pages - the `.verified.pdf` is always the full document ("PdfPig has no in-place page splitter"). Duplicate `net8.0` entry in TargetFrameworks is a real (harmless) bug worth not replicating. NUnit + `Verify.NUnit`. Two of the three public settings APIs (`PdfPigParsingOptions`, `SkipPdfNormalization`) are undocumented in the readme.

## Verify.Phash
- **NuGet package id(s)**: `Verify.Phash` (single package)
- **Current version**: `3.1.0`
- **Target frameworks**: `net48;net8.0-windows`
- **One-line description**: Extends Verify with a perceptual-hash image comparer (Shipwreck.Phash) so image snapshots pass when they are perceptually equivalent rather than byte-identical.
- **Tech tags**: images, image-comparison, phash, perceptual-hash, png, comparer
- **Third-party dependencies**: `Shipwreck.Phash.Bitmaps` 0.5.0; `System.Drawing.Common` 10.0.12 (explicit reference to avoid a CVE); `Verify` 33.1.0. PrivateAssets: `Polyfill` 11.4.0, `ProjectDefaults` 1.0.181, `Microsoft.Sbom.Targets` 4.1.12.
- **Initialize API**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyPhash.Initialize();
```
  `VerifyPhash.Initialize()` - no parameters. Throws if already initialized, calls `InnerVerifier.ThrowIfVerifyHasBeenRun()`, then `RegisterComparer("png")`. Note: only `png` is registered by `Initialize()` despite the readme claiming comparers "for png, jpg, bmp, and tiff".
- **Requires InitializePlugins-compatible pattern?**: yes
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyPhash.Initialize();
```
  then any image verification (`VerifyFile("sample.png")`, `Verify(stream, "png")`) compares with phash instead of byte equality.
- **Verbose / edge-case APIs**:
  - `VerifyPhash.RegisterComparer(string extension, float threshold = 0.999f, float sigma = 3.5f, float gamma = 1f, int angles = 180)` - registers the phash stream comparer for an additional extension (jpg, bmp, tiff, ...) with optional tuning. **Readme discrepancy**: the readme shows `VerifyPhash.RegisterComparer();` with no arguments under "Register all comparers", but no parameterless overload exists in the source - `extension` is required.
  - `PhashCompareSettings(this VerifySettings settings, float threshold = 0.999f, float sigma = 3.5f, float gamma = 1f, int angles = 180)` and the `SettingsTask` overload - per-test override of the comparison parameters:
```cs
[Test]
public Task LocalSettings() =>
    VerifyFile("sample.png")
        .PhashCompareSettings(
            threshold: .8f,
            sigma: 4f,
            gamma: 2f,
            angles: 170);
```
  - Failure message shape: `diff > threshold. threshold: {threshold}, score: {score}` (cross-correlation of the two digests; equal when `score > threshold`).
- **Interactions with other Verify extensions**: the readme itself is quiet, but Verify.QuestPDF readme lists this package as one of the interchangeable image comparers: "Other [compares](https://github.com/VerifyTests/Verify/blob/main/docs/comparer.md) options: https://github.com/VerifyTests/Verify.ImageHash / https://github.com/VerifyTests/Verify.ImageMagick / https://github.com/VerifyTests/Verify.Phash / https://github.com/VerifyTests/Verify.ImageSharp.Compare". These are alternatives - registering more than one comparer for the same extension means the last registration wins. It also competes with Verify core `VerifierSettings.UseSsimForPng()`, which several sibling repos (PDFium, QuestPDF, Pandoc) use for the same purpose.
- **MSBuild / project requirements**: **Windows only** on modern .NET - the package targets `net48;net8.0-windows` and uses `System.Drawing` (`Bitmap`, `Image.FromStream`). The test project declares `[assembly: SupportedOSPlatform("windows")]`. `SignAssembly=false` for this package.
- **Sample verified output**: not applicable - this is a comparer, not a converter. Verified files are ordinary `.verified.png` images (see `src/Tests/Samples.VerifyPng.verified.png`). The readme shows no text snapshot.
- **Notes for the wizard**: Windows-only; the readme overstates what `Initialize()` registers (png only) and shows a `RegisterComparer()` overload that does not exist. `PackageTags` in Directory.Build.props are wrong (`Aspose, Verify`). NUnit + `Verify.Nunit`.

## Verify.QuestPDF
- **NuGet package id(s)**: `Verify.QuestPDF` (single package)
- **Current version**: `2.9.0`
- **Target frameworks**: `net48;net6.0;net7.0;net8.0;net9.0;net10.0`
- **One-line description**: Extends Verify to verify QuestPDF `IDocument` instances - emits a metadata/settings info snapshot, a deterministic `.verified.pdf`, and a PNG per rendered page.
- **Tech tags**: pdf, questpdf, documents, reporting, rendering, images
- **Third-party dependencies**: `QuestPDF` 2026.9.0; `DeterministicPdf` 2.0.2; `Verify` 33.1.0. PrivateAssets: `Polyfill` 11.4.0, `ProjectDefaults` 1.0.181, `Microsoft.Sbom.Targets` 4.1.12.
- **Initialize API** (readme snippet pairs it with the PNG SSIM comparer):
```cs
[ModuleInitializer]
public static void Init()
{
    VerifierSettings.UseSsimForPng();
    VerifyQuestPdf.Initialize();
}
```
  `VerifyQuestPdf.Initialize()` - no parameters. Adds `DocumentMetadataConverter` and `DocumentSettingsConverter`, then `VerifierSettings.RegisterFileConverter<IDocument>(...)`. It pins `metadata.CreationDate`/`ModifiedDate` to `2000-01-01T00:00:00+00:00` before generation, then runs `PdfNormalizer.Normalize` over the bytes.
- **Requires InitializePlugins-compatible pattern?**: yes
- **Minimal usage**:
```cs
[Test]
public Task VerifyDocument()
{
    var document = GenerateDocument();
    return Verify(document);
}
```
- **Verbose / edge-case APIs**:
  - `PagesToInclude(this VerifySettings settings, int count)` / `PagesToInclude(this SettingsTask settings, int count)` - "To render only a defined number of pages at the start of a document". Only the png page snapshots are trimmed; "The `pdf` snapshot is unaffected and always contains the full source document."
```cs
[Test]
public Task PagesToInclude()
{
    var document = GenerateDocument();
    return Verify(document)
        .PagesToInclude(1);
}
```
  - `PagesToInclude(this VerifySettings settings, ShouldIncludePage include)` / `SettingsTask` overload, where `public delegate bool ShouldIncludePage(int pageNumber);` - "To dynamically control what pages are rendered":
```cs
[Test]
public Task PagesToIncludeDynamic()
{
    var document = GenerateDocument();
    return Verify(document)
        .PagesToInclude(pageNumber => pageNumber == 2);
}
```
  - `SkipPdfNormalization(this VerifySettings settings)` / `SettingsTask` overload - snapshots pdf bytes as produced. Present in `QuestPDFSettings.cs`, not mentioned in the readme.
  - `.ExcludeTargets("pdf")` (core Verify) - readme: "QuestPDF renders the source pdf, and it is included in the snapshot as a `.verified.pdf`. Generating it is expensive, and committing it is not always wanted." Global form: `VerifierSettings.ExcludeTargets("pdf")` at initialization.
```cs
[Test]
public Task ExcludePdf()
{
    var document = GenerateDocument();
    return Verify(document)
        .ExcludeTargets("pdf");
}
```
- **Interactions with other Verify extensions**: the readme is explicit about image comparers. "This sample uses [Verify.ImageMagick](https://github.com/VerifyTests/Verify.ImageMagick) to ignore small rendering differences that are expected between differens operating systesm." and lists alternatives: "Other [compares](https://github.com/VerifyTests/Verify/blob/main/docs/comparer.md) options: https://github.com/VerifyTests/Verify.ImageHash / https://github.com/VerifyTests/Verify.ImageMagick / https://github.com/VerifyTests/Verify.Phash / https://github.com/VerifyTests/Verify.ImageSharp.Compare". Note the readme's enable snippet actually uses core `VerifierSettings.UseSsimForPng()` rather than an external comparer package. Because it registers a *file* converter on `IDocument` (not a `pdf` stream converter), it can coexist with Verify.PdfPig or Verify.PDFium - and in fact those would then process the emitted `pdf` target.
- **MSBuild / project requirements**: QuestPDF licensing must be set by the consumer - the repo test ModuleInitializer sets `QuestPDF.Settings.License = LicenseType.Community;`. QuestPDF uses SkiaSharp natively for rendering. `SignAssembly=false`.
- **Sample verified output** (`Samples.VerifyDocument.verified.txt`), plus `#00.verified.png` per page and a `.verified.pdf`:
```txt
{
  Pages: 2,
  Settings: {
    ContentDirection: LeftToRight,
    PDFA_Conformance: None,
    PDFUA_Conformance: None,
    ImageCompressionQuality: High,
    ImageRasterDpi: 288
  }
}
```
- **Notes for the wizard**: Needs a QuestPDF license setting (`LicenseType.Community` for OSS/small business - commercial use may need a paid key). PNGs are rasterized via Skia so they vary across OS/fonts; pair with SSIM or an image comparer. `Metadata` is omitted from the info snapshot when no metadata members are set. NUnit + `Verify.NUnit`.

## Verify.Quibble
- **NuGet package id(s)**: `Verify.Quibble` (single package)
- **Current version**: `2.1.1`
- **Target frameworks**: `net48;net10.0`
- **One-line description**: Extends Verify with a json string comparer built on Quibble, so failing json snapshots report structural, JSON-path-level differences instead of a raw text diff.
- **Tech tags**: json, comparer, diff, quibble
- **Third-party dependencies**: `Quibble` 0.3.1; `System.Text.Json` 10.0.12 (explicit, to avoid a CVE); `Verify` 33.1.0. PrivateAssets: `Polyfill` 11.4.0, `ProjectDefaults` 1.0.181, `Microsoft.Sbom.Targets` 4.1.12.
- **Initialize API** (order matters - `UseStrictJson` first):
```cs
[ModuleInitializer]
public static void Init()
{
    VerifierSettings.UseStrictJson();
    VerifyQuibble.Initialize();
}
```
  `VerifyQuibble.Initialize()` - no parameters. Throws `"Already Initialized"` on a second call, calls `InnerVerifier.ThrowIfVerifyHasBeenRun()`, then **throws `"VerifyQuibble requires that VerifierSettings.UseStrictJson() is enabled"`** if `VerifierSettings.StrictJson` is false. Registers a string comparer for `json` (an empty verified file is treated as `{}`).
- **Requires InitializePlugins-compatible pattern?**: yes (but a bare `InitializePlugins()` will throw unless `UseStrictJson()` has already been called)
- **Minimal usage**: given an existing verified file:
```json
{
  "Property1": "ValueA",
  "Property2": "ValueB"
}
```
  and a test:
```cs
[Test]
public async Task Sample()
{
    var target = new Target(
        Property1: "ValueC",
        Property2: "ValueD");
    await Verifier.Verify(target);
}
```
- **Verbose / edge-case APIs**: none beyond `Initialized` + `Initialize()`. No settings extension methods, no scrubbers, no per-test toggles. The only configuration is the mandatory core call `VerifierSettings.UseStrictJson()`.
- **Interactions with other Verify extensions**: the hard dependency is on core Verify, quoted in the readme: "`UseStrictJson` is required since Verify by default [uses a variant of json](https://github.com/VerifyTests/Verify/blob/main/docs/serializer-settings.md#not-valid-json) which Quibble cannot parse." Turning on strict json changes every snapshot in the project from Verify's relaxed format to real json (`.verified.json`), so it is not a drop-in for an existing suite. It registers a *string* comparer for `json`, so it conflicts with any other json comparer but not with converters.
- **MSBuild / project requirements**: none native. Quibble is an F# library, so `FSharp.Core` comes in transitively.
- **Sample verified output**: verified files are real json (`Tests.Sample.verified.json`):
```json
{
  "Property1": "ValueC",
  "Property2": "ValueD"
}
```
  and a failing comparison reports:
```txt
Results do not match.
Use DiffEngineTray to verify files.
Differences:
Received: Tests.Sample.received.json
Verified: Tests.Sample.verified.json
Compare Result:
String value difference at $.Property1: ValueC vs ValueA.
String value difference at $.Property2: ValueD vs ValueB.
```
- **Notes for the wizard**: Ordering of the two initialization calls is load-bearing and `Initialize()` throws with a clear message if violated - worth generating as a single ModuleInitializer body. xunit.v3 + `Verify.XunitV3`. `SignAssembly=false`.

## Verify.RavenDB
- **NuGet package id(s)**: `Verify.RavenDB` (single package)
- **Current version**: `2.1.0`
- **Target frameworks**: `net9.0` (single TFM)
- **One-line description**: Extends Verify to verify RavenDB bits - verifying an `IDocumentSession` writes all pending changes (adds, field changes) to the snapshot.
- **Tech tags**: ravendb, database, nosql, document-database, persistence
- **Third-party dependencies**: `RavenDB.Client` 7.2.6; `Verify` 33.1.0. PrivateAssets: `ProjectDefaults` 1.0.181, `Microsoft.Sbom.Targets` 4.1.12. Test-only: `RavenDB.Embedded` 7.2.6.
- **Initialize API**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyRavenDB.Initialize();
```
  `VerifyRavenDB.Initialize()` - no parameters. Adds `SessionConverter` and `LazyStringValueConverter` via `VerifierSettings.AddExtraSettings`. Readme: "Enable VerifyRavenDB once at assembly load time".
- **Requires InitializePlugins-compatible pattern?**: yes
- **Minimal usage** - readme: "Verifiying an [IDocumentSession](https://ravendb.net/docs/article-page/5.0/Csharp/client-api/session/what-is-a-session-and-how-does-it-work) will result in all pending changes being written to a snapshot."
```cs
var entity = new Person
{
    Name = "John"
};
session.Store(entity);
await Verify(session);
```
  update case:
```cs
var entity = new Person
{
    Name = "John"
};
session.Store(entity);
session.SaveChanges();
entity.Name = "Joe";
await Verify(session);
```
- **Verbose / edge-case APIs**: none. `VerifyRavenDB` exposes only `Initialized` and `Initialize()`. No settings extension methods, no scrubbers, no recording API. Everything else is plain `Verify(session)`.
- **Interactions with other Verify extensions**: readme says nothing. Conceptually it is the RavenDB sibling of `Verify.EntityFramework` (session/change-tracker snapshotting) - the two do not overlap technically.
- **MSBuild / project requirements**: net9.0 only. Tests need a running RavenDB - the repo test project references `RavenDB.Embedded` and boots `EmbeddedServer.Instance.StartServer(...)` against a temp data directory in a static constructor, so the embedded server (a downloaded RavenDB server binary) must be able to start on the machine/CI agent. No license key needed for the embedded/community server in this setup.
- **Sample verified output** - document added:
```txt
[
  {
    Key: people/1-A,
    Changes: [
      {
        Type: DocumentAdded,
        NewValue: {
          Name: John
        }
      }
    ]
  }
]
```
  document updated:
```txt
[
  {
    Key: people/1-A,
    Changes: [
      {
        Type: FieldChanged,
        FieldName: Name,
        NewValue: Joe,
        OldValue: John
      }
    ]
  }
]
```
- **Notes for the wizard**: Needs a real (embedded) RavenDB server for the tests, which makes the generated sample heavier than most - the embedded server download/startup is the main friction. The shipped package only depends on `RavenDB.Client`; `RavenDB.Embedded` is test-only. NUnit + `Verify.NUnit`.

## Verify.ReadableExpressions
- **NuGet package id(s)**: `Verify.ReadableExpressions` (single package)
- **Current version**: `0.1.0`
- **Target frameworks**: `net48;net6.0;net7.0;net8.0;net9.0`
- **One-line description**: Adds Verify support for LINQ Expression trees, rendering them as readable C# source via AgileObjects.ReadableExpressions instead of the default `ToString()`.
- **Tech tags**: expressions, expression-trees, linq, readableexpressions, code-generation
- **Third-party dependencies**: `AgileObjects.ReadableExpressions` 4.1.3; `Verify` 33.1.1. PrivateAssets: `Polyfill` 11.4.0, `ProjectDefaults` 1.0.181, `Microsoft.Sbom.Targets` 4.1.12.
- **Initialize API**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyReadableExpressions.Initialize();
```
  `VerifyReadableExpressions.Initialize()` - no parameters. Inserts an `ExpressionConverter` at index 0 of the converters (so it wins over anything already registered) and calls `VerifierSettings.TreatAsString<Expression>((target, _) => ExpressionConverter.Convert(target), true)`.
- **Requires InitializePlugins-compatible pattern?**: yes
- **Minimal usage**:
```cs
[Fact]
public Task VerifySalaryCalculation()
{
    var expression = ComplexExpressionTrees.SalaryCalculation();
    return Verify(expression);
}
```
- **Verbose / edge-case APIs**: none. The class exposes only `Initialized` and `Initialize()`. No settings extension methods, no per-test toggles, no formatting options surfaced.
- **Interactions with other Verify extensions**: readme says nothing. Mechanically it inserts its converter at position 0 and registers `TreatAsString<Expression>`, so it takes priority over any other converter that might handle `Expression`. Relevant where an ORM/query extension (e.g. Verify.EntityFramework) also serializes expression trees.
- **MSBuild / project requirements**: none special. Cross-platform. `TreatWarningsAsErrors=true`, `PackageRequireLicenseAcceptance=true`.
- **Sample verified output**:
```txt
person => (person.Age < 30)
  ? person.Salary * 1.15m
  : (person.Age < 50) ? person.Salary * 1.08m : person.Salary * 1.03m
```
- **Notes for the wizard**: Pre-1.0 (0.1.0) and the newest-versioned Verify dependency in this batch (33.1.1 rather than 33.1.0). `src/nuget.md` is stale - it points at `Verify.NodaTime` documentation and milestones. xunit.v3 + `Verify.XunitV3`.

## Verify.Sample
- **NuGet package id(s)**: none - **this repo ships no NuGet package**. It is the demo/sample solution for Verify itself.
- **Current version**: not found (root `Directory.Build.props` has no `<Version>`; there is no `src/` folder - projects live directly at the repo root)
- **Target frameworks**: `Sample` -> `net10.0` (x64, `OutputType=Exe`); `WinFormsAppTests` -> `net10.0-windows` (`UseWindowsForms=true`, `EnableNUnitRunner=true`); `WpfAppTests`/`WpfApp`/`WinFormsApp` -> Windows desktop TFMs.
- **One-line description**: A sample/demo repository showing Verify in real projects (xunit + NUnit, WinForms, WPF, EF/SQL Server, HTTP, images) plus a `runsheet.md` used for live demos and talks.
- **Tech tags**: sample, demo, documentation, winforms, wpf, entityframework, sqlserver, http, images
- **Third-party dependencies** (consumed, not shipped; from the root `Directory.Packages.props`): `Verify` 33.1.0, `Verify.XunitV3` 33.1.0, `Verify.Nunit` 33.1.0, `Verify.DiffPlex` 3.3.1, `Verify.AngleSharp` 5.1.2, `Verify.EntityFramework` 15.4.0, `Verify.Http` 7.5.1, `Verify.ImageMagick` 3.10.0, `Verify.ImageSharp` 5.0.1, `Verify.SqlServer` 12.2.0, `Verify.SystemJson` 1.4.1, `Verify.WinForms` 6.0.0, `Verify.Xaml` 5.0.0, `LocalDb` 26.2.0, `EfLocalDb` 26.2.0, `Microsoft.EntityFrameworkCore` 10.0.12, `xunit.v3` 4.0.1, `NUnit` 4.6.1.
- **Initialize API**: no `VerifySample` type exists. The repo instead demonstrates initializing other extensions. `Sample/ModuleInitializer.cs`:
```cs
[ModuleInitializer]
public static void Initialize()
{
    VerifyHttp.Initialize();
    VerifyImageMagick.RegisterComparers(.01);
    VerifyImageSharp.Initialize();
    VerifyDiffPlex.Initialize();
    VerifierSettings.IgnoreMembers(
        "Content-Length",
        "traceparent",
        ...);
    VerifierSettings
        .ScrubLinesContaining(
            "Traceparent",
            "Date",
            "X-Amzn-Trace-Id",
            "Content-Length");
}
```
  `WinFormsAppTests/ModuleInitializer.cs`:
```cs
[ModuleInitializer]
public static void Initialize()
{
    VerifyImageMagick.RegisterComparers(.4);
    VerifierSettings.InitializePlugins();
}
```
  `WpfAppTests/ModuleInitializer.cs`:
```cs
[ModuleInitializer]
public static void Initialize()
{
    VerifyImageMagick.RegisterComparers(.05);
    VerifyXaml.Initialize();
    VerifyDiffPlex.Initialize();
}
```
- **Requires InitializePlugins-compatible pattern?**: n/a - no extension class in this repo. (It *consumes* `VerifierSettings.InitializePlugins()` in `WinFormsAppTests`.)
- **Minimal usage**: not applicable. The demo entry point is `runsheet.md`, which walks through the snapshot flow (first run / subsequent run mermaid diagrams), snapshot management (DiffEngineTray, Rider/R# extension, clipboard), "Snapshot is Serialization", and global scrubbers.
- **Verbose / edge-case APIs**: none of its own. Demo areas visible in `Sample/`: `SimpleUsage.cs`, `ParamTest.cs`, `HttpRecordingTest.cs`, `HttpResponseTest.cs`, `ImageSharpTests.cs`, `SqlServerTests.cs` (schema snapshot as `.verified.md`, recording usage), `EntityFramework/`, `ComparedToAsserts/`; plus `WinFormsAppTests/Form1Tests.cs` (`.verified.png`) and `WpfAppTests`.
- **Interactions with other Verify extensions**: this repo *is* the multi-extension interaction example - a single test project references Verify.Http, Verify.ImageMagick, Verify.ImageSharp, Verify.DiffPlex, Verify.AngleSharp, Verify.EntityFramework, Verify.SqlServer, Verify.SystemJson at once, with image comparers registered at different tolerances per project (`.01`, `.05`, `.4`).
- **MSBuild / project requirements**: **Windows only** in practice (WinForms and WPF projects, `net10.0-windows`). SQL Server demos need LocalDB (`LocalDb`/`EfLocalDb`). HTTP demos hit live endpoints, hence the long `IgnoreMembers`/`ScrubLinesContaining` lists for trace and cache headers. Several csprojs pin `VersionOverride="33.0.1"` for `Verify`/`Verify.XunitV3`/`Verify.Nunit`.
- **Sample verified output**: e.g. `Sample/SimpleUsage.VerifyString.verified.txt`, `Sample/SqlServerTests.SqlServerSchema.verified.md`, `WinFormsAppTests/Form1Tests.FormUsage.verified.png` - a spread of text, markdown and image snapshots rather than one canonical format.
- **Notes for the wizard**: **Not an extension - do not generate an extension page for it.** No readme.md at the repo root (only `runsheet.md`, `license.txt`, and the usual repo furniture), no `src/` directory, no packable project. At the time of reading, `Directory.Packages.props` contains an **unresolved git merge conflict** (`<<<<<<< HEAD` / `>>>>>>>` markers around the `Microsoft.NET.Test.Sdk` PackageVersion) - the local clone is mid-merge.

## Verify.SendGrid
- **NuGet package id(s)**: `Verify.SendGrid` (single package)
- **Current version**: `1.0.0`
- **Target frameworks**: `net48;net8.0`
- **One-line description**: Extends Verify to verify SendGrid types - `SendGridMessage`, `EmailAddress`, `Attachment` and `Personalization` serialize into readable snapshots.
- **Tech tags**: sendgrid, email, mail, messaging, twilio
- **Third-party dependencies**: `SendGrid` 9.29.3; `Verify` 33.1.0. PrivateAssets: `Polyfill` 11.4.0, `ProjectDefaults` 1.0.181, `Microsoft.Sbom.Targets` 4.1.12.
- **Initialize API**:
```cs
[ModuleInitializer]
public static void Initialize() =>
    VerifySendGrid.Initialize();
```
  `VerifySendGrid.Initialize()` - no parameters. Adds `EmailAddressConverter`, `AttachmentConverter`, `PersonalizationConverter`, `SendGridMessageConverter`.
- **Requires InitializePlugins-compatible pattern?**: yes
- **Minimal usage**:
```cs
[Fact]
public Task SendGridMessage()
{
    var mail = new SendGridMessage
    {
        From = new("test@example.com", "DX Team"),
        Subject = "Sending with Twilio SendGrid is Fun",
        PlainTextContent = "and easy to do anywhere, even with C#",
        HtmlContent = "<strong>and easy to do anywhere, even with C#</strong>"
    };
    mail.AddTo(new EmailAddress("test@example.com", "Test User"));
    return Verify(mail);
}
```
- **Verbose / edge-case APIs**: the class exposes only `Initialized` and `Initialize()` publicly (plus an internal `UnixTimeStampToDateTime` helper). No settings extension methods, no scrubbers. The readme's second documented scenario is attachment verification - base64 `Content` is decoded into readable text by the converter:
```cs
[Fact]
public Task Attachment()
{
    var contentBytes = "The content"u8.ToArray();
    var attachment = new Attachment
    {
        Filename = "name.txt",
        Content = Convert.ToBase64String(contentBytes),
        Type = "text/html",
        Disposition = "attachment"
    };
    return Verify(attachment);
}
```
  Additional covered types (from the test snapshots, not the readme): `EmailAddress`, `Personalization`, and reply-to variants (`SingleReplyTo`, `SingleReplyTos`, `SingleReplyToAndReplyTos`).
- **Interactions with other Verify extensions**: readme says nothing. No extension registration (no stream/file converter), so it cannot collide with format-based extensions - it only adds serializer converters.
- **MSBuild / project requirements**: none special; no SendGrid account, API key or network access needed - it verifies the message objects, not delivery. Note the csproj sets `<RootNamespace>Verify.ZeroLog</RootNamespace>` (a copy/paste leftover) and `<OutputType>Library</OutputType>`.
- **Sample verified output** (`Tests.SendGridMessage.verified.txt`):
```txt
{
  From: DX Team <test@example.com>,
  Personalizations: [
    {
      To: Test User <test@example.com>
    }
  ],
  Subject: Sending with Twilio SendGrid is Fun,
  PlainTextContent: and easy to do anywhere, even with C#,
  HtmlContent: <strong>and easy to do anywhere, even with C#</strong>
}
```
  attachment:
```txt
{
  Filename: name.txt,
  Disposition: attachment,
  Type: text/html,
  Content: The content
}
```
- **Notes for the wizard**: This is the only repo in the batch with no `src/nuget.md` (the file is empty/absent) and no `icon` reference issue aside; `PackageTags` are `Http, Verify` and the `<Description>` says "verification of MailMessage bits", both stale/wrong for SendGrid. Wrong `RootNamespace` (`Verify.ZeroLog`). xunit.v3 + `Verify.XunitV3`.

## Verify.Sep
- **NuGet package id(s)**: `Verify.Sep` (single package)
- **Current version**: `0.2.0`
- **Target frameworks**: `net8.0` (single TFM)
- **One-line description**: Extends Verify to verify CSVs via Sep - registers a `csv` scrubber and a `SepReader` file converter, with per-column ignore/scrub/translate control and Guid/date counter scrubbing.
- **Tech tags**: csv, sep, tabular, data, scrubbing
- **Third-party dependencies**: `Sep` 0.17.0; `Verify` 33.1.0. PrivateAssets: `ProjectDefaults` 1.0.181, `Microsoft.Sbom.Targets` 4.1.12.
- **Initialize API**:
```cs
[ModuleInitializer]
public static void Initialize() =>
    VerifySep.Initialize();
```
  `VerifySep.Initialize()` - no parameters. Calls `VerifierSettings.AddScrubber("csv", Handle)` and `VerifierSettings.RegisterFileConverter<SepReader>(ConvertReader)`.
- **Requires InitializePlugins-compatible pattern?**: yes
- **Minimal usage**:
```cs
[Test]
public Task VerifyCsv() =>
    VerifyFile("sample.csv");
```
```cs
[Test]
public Task VerifyCsvStream()
{
    var stream = File.OpenRead("sample.csv");
    return Verify(stream, "csv");
}
```
- **Verbose / edge-case APIs**:
  - `IgnoreCsvColumns(this VerifySettings settings, params string[] columns)` / `SettingsTask` overload - "Excludes the column from the output."
```cs
[Test]
public Task IgnoreColumns() =>
    VerifyFile("sample.csv")
        .IgnoreCsvColumns("Customer Id");
```
  - `ScrubCsvColumns(this VerifySettings settings, params string[] columns)` / `SettingsTask` overload - "Replaces the column with `{Scrubbed}`."
```cs
[Test]
public Task ScrubCsvColumns() =>
    VerifyFile("sample.csv")
        .ScrubCsvColumns("Customer Id");
```
  - `TranslateCsvColumns(this VerifySettings settings, Func<string, Func<string, string>?> translate)` and the fluent form **named differently**: `TranslateCsvColumn(this SettingsTask settings, Func<string, Func<string, string>?> translate)` (singular on `SettingsTask`, plural on `VerifySettings`). Per-column value transform; a null inner result renders as `null`. Not documented in the readme.
  - `SepReader` file converter - verify a reader directly:
```cs
[Test]
public Task VerifyReader()
{
    using var reader = Sep.Reader().FromFile("sample.csv");
    return Verify(reader);
}
```
  - Readme: "Note that Guid and date scrubbing is respected." Untranslated cells go through Verify's `Counter.TryConvert`, producing `Guid_1`, `Date_1`, etc.
- **Interactions with other Verify extensions**: readme says nothing. It hooks the `csv` extension as a *scrubber*, so it composes with anything that emits `csv` targets - notably **Verify.OpenXml**, whose Excel converter writes one `.verified.csv` per sheet. Initializing both means Sep scrubs OpenXml's sheet output.
- **MSBuild / project requirements**: net8.0 only. Sample csv needs CopyToOutputDirectory. `SignAssembly=false`.
- **Sample verified output** (`Samples.VerifyCsv.verified.csv`, trimmed):
```csv
Index,Customer Id,First Name,Last Name,Company,City,Country,Phone,Dob
1,Guid_1,Sheryl,Baxter,Rasmussen Group,East Leonard,Chile,229.077.5154,Date_1
2,Guid_2,Preston,Lozano,Vega-Gentry,East Jimmychester,Djibouti,5153435776,Date_2
3,Guid_3,Roy,Berry,Murillo-Perry,Isabelborough,Antigua and Barbuda,-1199,Date_3
```
  with `ScrubCsvColumns("Customer Id")` the second column becomes `{Scrubbed}`; with `IgnoreCsvColumns("Customer Id")` it is dropped entirely.
- **Notes for the wizard**: Pre-1.0 (0.2.0). The Translate API is undocumented and has an inconsistent name between the two overloads (`TranslateCsvColumns` vs `TranslateCsvColumn`) - copy verbatim, do not normalize. The test ModuleInitializer here calls `VerifyDiffPlex.Initialize()` rather than `VerifierSettings.InitializePlugins()`. NUnit + `Verify.NUnit`.

## Verify.Serilog
- **NuGet package id(s)**: `Verify.Serilog` (single package)
- **Current version**: `3.4.0`
- **Target frameworks**: `net8.0;net9.0;net10.0`
- **One-line description**: Extends Verify to capture Serilog log events during a test (via a `VerifySink` wired into `Log.Logger`) and include them in the snapshot.
- **Tech tags**: serilog, logging, logs, recording, diagnostics
- **Third-party dependencies**: `Serilog` 4.4.0; `Verify` 33.1.0. PrivateAssets: `Polyfill` 11.4.0, `ProjectDefaults` 1.0.181, `Microsoft.Sbom.Targets` 4.1.12.
- **Initialize API**: signature is `public static void Initialize(Action<LoggerConfiguration>? custom = null)`. Readme, plain form:
```cs
[ModuleInitializer]
public static void Initialize() =>
    VerifySerilog.Initialize();
```
  Readme: "Or omit the above is `VerifierSettings.InitializePlugins();` is called:"
```cs
[ModuleInitializer]
public static void Initialize() =>
    VerifierSettings.InitializePlugins();
```
  With the optional configuration callback: "`VerifySerilog.Initialize` accepts an optional `Action<LoggerConfiguration>` callback for customising the underlying Serilog `LoggerConfiguration` - for example to register destructuring policies, enrichers, or filters. The callback runs after `MinimumLevel.Verbose`, `Enrich.FromLogContext`, and the `VerifySink` have been wired up, so it can layer on top of those defaults."
```cs
[ModuleInitializer]
public static void Initialize() =>
    VerifySerilog.Initialize(
        _ => _.Destructure.ByTransforming<Customer>(
            customer => new
            {
                customer.Name
            }));
```
  Internally it adds `LogEventPropertyConverter`, `LogEventConverter`, `ScalarValueConverter`, `PropertyEnricherConverter`, `DictionaryValueConverter`, `StructureValueConverter`, builds a `LoggerConfiguration` with `MinimumLevel.Verbose()`, `Enrich.FromLogContext()`, `WriteTo.Sink<VerifySink>()`, then assigns `Log.Logger`.
- **Requires InitializePlugins-compatible pattern?**: yes - and the readme explicitly states `InitializePlugins()` alone is sufficient (the optional `custom` parameter then defaults to null).
- **Minimal usage**:
```cs
[Test]
public Task Usage()
{
    Recording.Start();

    var result = Method();

    return Verify(result);
}

static string Method()
{
    Log.Error("The Message");
    return "Result";
}
```
- **Verbose / edge-case APIs**:
  - `VerifySerilog.Initialize(Action<LoggerConfiguration>? custom = null)` - optional Serilog configuration callback (destructuring policies, enrichers, filters).
  - `VerifySerilog.IgnoreSourceContext<T>()` - "filters out log events whose `SourceContext` property matches a known type"; calls `InnerVerifier.ThrowIfVerifyHasBeenRun()` then adds `typeof(T).FullName` to the ignore set.
  - `VerifySerilog.IgnoreSourceContext(string sourceContext)` - string overload. Readme: "Generic and string overloads are provided. Typically called from the module initializer alongside `Initialize`."
```cs
VerifySerilog.IgnoreSourceContext<MyNoisyType>();
VerifySerilog.IgnoreSourceContext("Some.Namespace.Logger");
```
  Readme: "Events emitted via `Log.ForContext<T>()` or `Log.ForContext("SourceContext", "...")` whose source context matches are dropped before being recorded."
  - `RecordingLogger` - a public static type in the package (`public static class RecordingLogger;`), not documented in the readme.
  - `Recording.Start()` (core Verify) - begins capture; captured events land under the `log` key in the snapshot.
- **Interactions with other Verify extensions**: readme says nothing explicit, but it takes over the global `Log.Logger`, so it conflicts with any test setup that configures Serilog itself. It shares Verify's `Recording` mechanism with other recording extensions (Verify.OpenTelemetry writes under `activity`, this writes under `log`), so they compose in one snapshot.
- **MSBuild / project requirements**: none native. Note it replaces the process-wide `Log.Logger`, and the repo test assembly runs highly parallel (`[assembly: Parallelizable(ParallelScope.All)]`, `[assembly: LevelOfParallelism(48)]`), so the sink must be recording-scoped rather than global-state-scoped.
- **Sample verified output**:
```txt
{
  target: Result,
  log: {
    Error: The Message
  }
}
```
- **Notes for the wizard**: One of the few extensions whose `Initialize` takes a meaningful optional parameter, and one of the few that explicitly documents the `InitializePlugins()` shortcut - the generated docs should show both. Repo has a `claude.md`. NUnit + `Verify.NUnit`.
