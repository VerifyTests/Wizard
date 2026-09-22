## Verify.SourceGenerators
- **NuGet package id(s)**: `Verify.SourceGenerators` (single package)
- **Current version**: `2.6.0`
- **Target frameworks**: `net472;net48;net6.0;net7.0;net8.0;net9.0`
- **One-line description**: Extends Verify to allow verification of C# (and VB) Source Generators.
- **Tech tags**: source generators, roslyn, incremental generators, code generation, analyzers
- **Third-party dependencies** (shipped package): `Verify` 33.1.0; `Microsoft.CodeAnalysis.CSharp` 4.9.2 (Pinned="true"); `Microsoft.CodeAnalysis.Analyzers` 3.11.0 (PrivateAssets=all); `Polyfill` 11.4.0 (PrivateAssets=all); `ProjectDefaults` 1.0.181 (PrivateAssets=all). Test-only: `Microsoft.CodeAnalysis.VisualBasic` 4.9.2.
- **Initialize API**: `VerifySourceGenerators.Initialize()` — no parameters/overloads.
- **Requires InitializePlugins-compatible pattern?** yes (`public static bool Initialized { get; private set; }` + `Initialize()`; throws `"Already Initialized"` on second call)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifySourceGenerators.Initialize();
```
```cs
public class SampleTest
{
    [Fact]
    public Task Driver()
    {
        var driver = BuildDriver();

        return Verify(driver);
    }

    [Fact]
    public Task RunResults()
    {
        var driver = BuildDriver();

        var results = driver.GetRunResult();
        return Verify(results);
    }

    [Fact]
    public Task RunResult()
    {
        var driver = BuildDriver();

        var result = driver.GetRunResult().Results.Single();
        return Verify(result);
    }

    static GeneratorDriver BuildDriver()
    {
        var compilation = CSharpCompilation.Create("name");
        var generator = new HelloWorldGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        return driver.RunGenerators(compilation);
    }
}
```
- **Verbose / edge-case APIs**:
  - `IgnoreGeneratedResult(Func<GeneratedSourceResult, bool> shouldIgnore)` — extension on both `SettingsTask` and `VerifySettings`; excludes specific generated outputs from the snapshot. Multiple calls accumulate.
    ```cs
    [Fact]
    public Task IgnoreFile()
    {
        var driver = GeneratorDriver();

        return Verify(driver)
            .IgnoreGeneratedResult(
                _ => _.HintName.Contains("helper") ||
                     _.SourceText
                         .ToString()
                         .Contains("static void SayHello()"));
    }
    ```
  - Manipulating generated source uses core Verify scrubbers, e.g.
    ```cs
    [Fact]
    public Task ScrubLines()
    {
        var driver = GeneratorDriver();

        return Verify(driver)
            .ScrubLines(_ => _.StartsWith("using "));
    }
    ```
  - Three verifiable target types are registered/supported: `GeneratorDriver`, `GeneratorDriverRunResult` (file converters) and `GeneratorRunResult` (via converters). Serializer converters added: `LocalizableStringConverter`, `DiagnosticConverter`, `LocationConverter`, `FileLinePositionSpanConverter`, `GeneratedSourceResultConverter`, `DiagnosticDescriptorConverter`, `SourceTextConverter`.
  - Generator exceptions are rethrown (single) or aggregated (`AggregateException`) rather than snapshotted.
  - Output extension is derived from the syntax tree file path; `.vb` hint names produce `vb` targets, otherwise `cs`.
- **Interactions with other Verify extensions**: readme mentions none. The test project pairs it with `Verify.DiffPlex` (`VerifyDiffPlex.Initialize()` in a second ModuleInitializer) and `Verify.XunitV3`.
- **MSBuild / project requirements**: Ships `build\Verify.SourceGenerators.targets` inside the package (auto-imported by consumers). It removes `**\*.received.cs;**\*.verified.cs` from `Compile` and re-adds them as `None` with `DependentUpon` nesting, so generated snapshot `.cs` files don't get compiled into the test project. Readme note: "This snippets assumes use of the XUnit Verify adapter, change the `using VerifyXUnit` if using other testing frameworks."
- **Sample verified output**:
```txt
{
  Diagnostics: [
    {
      Location: dir\theFile.cs: (1,2)-(3,4),
      Message: the message from hello world generator,
      Severity: Info,
      WarningLevel: 1,
      Descriptor: {
        Id: theId,
        Title: the title,
        MessageFormat: the message from {0},
        Category: the category,
        DefaultSeverity: Info,
        IsEnabledByDefault: true
      }
    }
  ]
}
```
  Plus one file per generated source, e.g. `SampleTest.Driver#helloWorld.verified.cs` starting with `//HintName: helloWorld.cs`.
- **Notes for the wizard**: Produces multiple targets (an info `.txt` plus one `.cs`/`.vb` per generated source, named by hint name). The `.targets` file is the only extension in this set with a packaged MSBuild asset — a generated sample project must not manually `Compile Remove` those files. `Microsoft.CodeAnalysis.*` versions are pinned at 4.9.2 deliberately (`Pinned="true"`, `NoWarn` includes `RS1035`, `NU1608`).

## Verify.SqlServer
- **NuGet package id(s)**: `Verify.SqlServer` (single package)
- **Current version**: `12.2.0`
- **Target frameworks**: `net48;net8.0;net9.0;net10.0`
- **One-line description**: Extends Verify to allow verification of SQL Server bits — database schema snapshots plus recording of executed SQL commands.
- **Tech tags**: sql server, database, sql, schema, ado.net, smo, diagnostics
- **Third-party dependencies** (shipped package): `Microsoft.Data.SqlClient` 7.1.0; `Microsoft.Extensions.DiagnosticAdapter` 3.1.32; `Microsoft.SqlServer.TransactSql.ScriptDom` 180.107.0; `Microsoft.SqlServer.SqlManagementObjects` 181.36.0; `System.Diagnostics.DiagnosticSource` 10.0.12; `Verify` 33.1.0; `ProjectDefaults` 1.0.181 (private). Test-only: `LocalDb` 26.2.0, `NUnit` 4.6.1.
- **Initialize API**:
  - `VerifySqlServer.Initialize()` — equivalent to `Initialize(recordCommands: true)`.
  - `VerifySqlServer.Initialize(bool recordCommands)` — XML doc verbatim: *"Subscribe to `SqlConnection` diagnostic events, so executed commands are added to `Recording` under the name `sql`. Disable when another package, for example Verify.EntityFramework, already records the same commands."*
- **Requires InitializePlugins-compatible pattern?** yes (`Initialized` property + parameterless `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifySqlServer.Initialize();
```
```cs
await Verify(connection);
```
```cs
await using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();
Recording.Start();
await using var command = connection.CreateCommand();
command.CommandText = "select Value from MyTable";
var value = await command.ExecuteScalarAsync();
await Verify(value!);
```
- **Verbose / edge-case APIs**:
  - `SchemaIncludes(DbObjects includes)` — restrict which object types are scripted (`SettingsTask` + `VerifySettings` overloads).
    ```cs
    await Verify(connection)
        // include only tables and views
        .SchemaIncludes(DbObjects.Tables | DbObjects.Views);
    ```
  - `DbObjects` flags enum (namespace `VerifyTests.SqlServer`): `StoredProcedures = 1, Synonyms = 2, Tables = 4, UserDefinedFunctions = 8, Views = 16, All = ...`.
  - `SchemaFilter(Func<NamedSmoObject, bool> filter)` — dynamic per-object filtering.
    ```cs
    await Verify(connection)
        // include tables & views, or named MyTrigger
        .SchemaFilter(
            _ => _ is TableViewBase ||
                 _.Name == "MyTrigger");
    ```
  - `SchemaAsMarkdown()` / `SchemaAsSql()` — output format for the schema snapshot; default is Markdown (`.verified.md`), `SchemaAsSql()` gives `.verified.sql`. (Both on `SettingsTask` and `VerifySettings`.)
  - `Recording.Start()` / `Recording.Stop()` — core Verify recording; entries are keyed `sql`.
    ```cs
    var entries = Recording.Stop();

    // all sql entries via key
    var sqlEntries = entries
        .Where(_ => _.Name == "sql")
        .Select(_ => _.Data);

    // successful Commands via Type
    var sqlCommandsViaType = entries
        .Select(_ => _.Data)
        .OfType<SqlCommand>();

    // failed Commands via Type
    var sqlErrorsViaType = entries
        .Select(_ => _.Data)
        .OfType<ErrorEntry>();
    ```
  - `ErrorEntry` — recorded failed command type (`VerifyTests.SqlServer` recording namespace).
  - `Recording.IgnoreNames("sql")` — alternative to disabling recording (see interactions).
  - Obsolete (error-level, do not generate): `SchemaSettings(bool storedProcedures, bool tables, bool views, bool userDefinedFunctions, bool synonyms, Func<string,bool>? includeItem)` — replaced by `SchemaIncludes`/`SchemaFilter`.
  - Serializer converters registered: `ErrorConverter`, `ConnectionConverter`, `CommandConverter`, `ExceptionConverter`, `ParameterConverter`, `ParameterCollectionConverter`. File converter registered for `SqlConnection`.
- **Interactions with other Verify extensions** (readme, quoted):
  - "Recording is enabled by `VerifySqlServer.Initialize()`, which subscribes to the `Microsoft.Data.SqlClient` diagnostic listener. Pass `recordCommands: false` to leave that listener unsubscribed"
  - "Only recording is disabled. The converters, and the `SqlConnection` schema file converter, are still registered."
  - "This is useful when another package records the same commands. [Verify.EntityFramework](https://github.com/VerifyTests/Verify.EntityFramework) records EF Core commands under the name `ef`, and EF Core executes those commands through `SqlCommand`. So with both packages recording, every command EF executes is captured twice: once as `sql` and once as `ef`."
  - "`VerifierSettings.InitializePlugins()` discovers this package and calls `Initialize()`, which enables recording. So `Initialize(recordCommands: false)` has to run *before* `InitializePlugins()`, otherwise discovery initializes first and the explicit call throws `Already Initialized`."
  - "`Recording.IgnoreNames(\"sql\")` is an alternative that needs no ordering, since it can be called at any point before recording starts. It discards `sql` entries as they are added, but the listener stays subscribed, so each command is still cloned and then thrown away."
  - Example of the ordering-sensitive init:
    ```cs
    [ModuleInitializer]
    public static void Init() =>
        VerifySqlServer.Initialize(recordCommands: false);
    ```
- **MSBuild / project requirements**: Schema verification needs a live SQL Server connection; the repo's own tests require LocalDB (claude.md: "Tests use **NUnit** and **LocalDb** — a running SQL Server LocalDb instance is required"). SMO (`Microsoft.SqlServer.SqlManagementObjects`) is a heavyweight, Windows-friendly dependency. Assembly is strong-named (`key.snk`) and exposes `InternalsVisibleTo` to `Tests`.
- **Sample verified output** (schema, Markdown format — from `src/Tests/Tests.Schema.verified.md`):
```md
## Tables

### ChildTable

```sql
CREATE TABLE [dbo].[ChildTable](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ParentId] [int] NOT NULL,
	[Value] [int] NOT NULL,
 CONSTRAINT [PK_ChildTable] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
) ON [PRIMARY]
) ON [PRIMARY]
```
```
  Recording output:
```txt
{
  target: 42,
  sql: {
    Text:
select Value
from   MyTable,
    HasTransaction: false
  }
}
```
- **Notes for the wizard**: Recorded SQL text is reformatted by `FormattedScriptGenerator`/`SqlFormatter` using ScriptDom visitors (`RemoveSquareBracketVisitor`, `InPredicateCollector`, `OrderByCollector`), so verified SQL is normalized rather than raw. The repo has a second test project `RecordingDisabledTests` purely to cover `Initialize(recordCommands: false)` — a wizard generating that variant should put it in its own assembly, since `Initialize` can only be called once per process. Pairs naturally with LocalDb (see the LocalDb section) and Verify.EntityFramework.

## Verify.Sylvan.Data.Excel
- **NuGet package id(s)**: `Verify.Sylvan.Data.Excel` (single package)
- **Current version**: `1.0.0`
- **Target frameworks**: `net472;net8.0;net9.0`
- **One-line description**: Extends Verify to verify Excel documents (xls, xlsb, xlsx) by converting them to CSV via Sylvan.Data.Excel.
- **Tech tags**: excel, xlsx, xls, xlsb, csv, spreadsheet, sylvan
- **Third-party dependencies** (shipped package): `Sylvan.Data.Csv` 1.4.4; `Sylvan.Data.Excel` 0.5.8; `Verify` 33.1.0; `Polyfill` 11.4.0 (private); `ProjectDefaults` 1.0.181 (private).
- **Initialize API**: `VerifySylvanDataExcel.Initialize()` — no parameters.
- **Requires InitializePlugins-compatible pattern?** yes (`Initialized` + `Initialize()`). Note: unlike most others, it does *not* call `InnerVerifier.ThrowIfVerifyHasBeenRun()`.
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Initialize() =>
    VerifySylvanDataExcel.Initialize();
```
```cs
[Test]
public Task VerifyExcel() =>
    VerifyFile("sample.xlsx");
```
```cs
[Test]
public Task VerifyExcelStream()
{
    var stream = new MemoryStream(File.ReadAllBytes("sample.xlsx"));
    return Verify(stream, "xlsx");
}
```
- **Verbose / edge-case APIs**:
  - Verify an `ExcelDataReader` directly (file converter registered for the type):
    ```cs
    [Test]
    public Task VerifyExcelDataReader()
    {
        using var stream = File.OpenRead("sample.xlsx");
        using var reader = ExcelDataReader.Create(stream, ExcelWorkbookType.ExcelXml);
        return Verify(reader);
    }
    ```
  - `CsvDataWriterOptions(CsvDataWriterOptions options)` — configures CSV writing (`SettingsTask` + `VerifySettings` overloads). Throws `"NewLine must be '\n'"` if `options.NewLine != "\n"`.
    ```cs
    [Test]
    public Task CsvDataWriterOptions()
    {
        using var stream = File.OpenRead("sample.xlsx");
        var options = new CsvDataWriterOptions
        {
            Delimiter = '\t',
            Quote = '"',
        };

        return Verify(stream)
            .CsvDataWriterOptions(options);
    }
    ```
  - `ExcelDataReaderOptions(ExcelDataReaderOptions options)` — configures reading (`SettingsTask` + `VerifySettings` overloads). Present in `VerifySylvanDataExcelSettings.cs` but **not documented in the readme**.
  - Stream converters registered for extensions `xls` (`ExcelWorkbookType.Excel`), `xlsb` (`ExcelWorkbookType.ExcelBinary`), `xlsx` (`ExcelWorkbookType.ExcelXml`).
  - Multi-sheet behaviour: a single sheet yields one unnamed `csv` target; multiple sheets yield one `csv` target per sheet, named by worksheet name. An info object with `SheetNames` is always emitted.
- **Interactions with other Verify extensions**: readme mentions none. Overlaps functionally with `Verify.Syncfusion` (which also registers `xlsx`/`xls` stream converters) — **the two cannot both be initialized in one assembly** without the later registration conflicting. Test project uses `Verify.NUnit` + `Verify.DiffPlex` and `VerifierSettings.InitializePlugins()`.
- **MSBuild / project requirements**: Sample files need `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>`. `SignAssembly` is set to `false` in `Directory.Build.props` (this repo is not strong-named). Uses a `CharSpan` using-alias for `System.ReadOnlySpan<char>`.
- **Sample verified output**:
```csv
0,First Name,Last Name,Gender,Country,Date,Age,Id,Formula
1,Dulce,Abril,Female,United States,2017-10-15,32,1562,1594
2,Mara,Hashimoto,Female,Great Britain,2016-08-16,25,1582,1607
3,Philip,Gent,Male,France,2015-05-21,36,2587,2623
4,Kathleen,Hanner,Female,United States,2017-10-15,25,3549,3574
5,Nereida,Magwood,Female,United States,2016-08-16,58,2468,2526
6,Gaston,Brumm,Male,United States,2015-05-21,24,2554,2578
```
- **Notes for the wizard**: Readme credits "Code provided by Cédric Luthi https://github.com/0xced". No license key needed (contrast with Verify.Syncfusion). Lowest-friction Excel option — pure managed, cross-platform, no Office/native deps.

## Verify.Syncfusion
- **NuGet package id(s)**: `Verify.Syncfusion` (single package)
- **Current version**: `3.1.0`
- **Target frameworks**: `net48;net8.0;net9.0;net10.0`
- **One-line description**: Extends Verify to verify documents (pdf, docx, xlsx, pptx) via Syncfusion File Formats, converting them to png/csv/text.
- **Tech tags**: pdf, word, docx, excel, xlsx, powerpoint, pptx, documents, syncfusion, rendering
- **Third-party dependencies** (shipped package): `Syncfusion.DocIO.Net.Core` 34.2.8, `Syncfusion.DocIORenderer.Net.Core` 34.2.8, `Syncfusion.EJ2.PdfViewer.AspNet.Core` 34.2.8, `Syncfusion.Pdf.Net.Core` 34.2.8, `Syncfusion.Presentation.Net.Core` 34.2.8, `Syncfusion.PresentationRenderer.Net.Core` 34.2.8, `Syncfusion.XlsIO.Net.Core` 34.2.8, `Syncfusion.XlsIORenderer.Net.Core` 34.2.8; `DeterministicPdf` 2.0.2; `DeterministicIoPackaging` 0.31.0; `System.Text.Json` 10.0.12 (explicit, CVE); `Microsoft.Extensions.Caching.Memory` 10.0.12; `Verify` 33.1.0; `ProjectDefaults` 1.0.181 (private).
- **Initialize API**: `VerifySyncfusion.Initialize()` — no parameters.
- **Requires InitializePlugins-compatible pattern?** yes (`Initialized` + `Initialize()`). Does not call `ThrowIfVerifyHasBeenRun()`.
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Initialize() =>
    VerifySyncfusion.Initialize();
```
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
  - Per-format file/stream verification, all following the same `VerifyFile("sample.x")` / `Verify(stream, "x")` pattern: `VerifyExcel` (`xlsx`), `VerifyWord` (`docx`), `VerifyPowerPoint` (`pptx`). Word is documented as: "When verifying a Word file or stream, both the textual content of the Word file as well as a png export of the pages in the Word file are verified."
  - `ExcludeTargets("xlsx")` — "The source document is included in the snapshot as a `.verified.docx`, `.verified.xlsx`, or `.verified.pptx`. Building the deterministic package is expensive, and committing it is not always wanted. `ExcludeTargets` drops it from a verification and skips the build, while the info, text, csv, and rendered pages still verify".
    ```cs
    // Excludes xlsx, so the deterministic xlsx target is skipped.
    [Test]
    public Task ExcludeXlsx() =>
        VerifyFile("sample.xlsx")
            .ExcludeTargets("xlsx");
    ```
    "The same applies to `docx` and `pptx`. To exclude for every test, call `VerifierSettings.ExcludeTargets(\"xlsx\")` at initialization."
  - `UniqueForRuntime()` — "When verifying binary package output (xlsx, docx, nupkg, etc.) across multiple target frameworks (e.g. net48 and net10.0), the binary output may differ due to Deflate compression implementation differences. The XML content within entries is identical — only the compressed bytes differ."
    ```cs
    await Verify(stream, extension: "xlsx")
        .UniqueForRuntime();
    ```
  - `PagesToInclude(int count)` — **in source (`VerifySyncfusionSettings.cs`) but not in the readme**. XML doc: "Limits the number of rendered slide/page `png` snapshots to the first `count`. Any full-document binary target (for example the `pptx` emitted for PowerPoint) is unaffected and always contains the full source document." `SettingsTask` + `VerifySettings` overloads.
  - `PdfPngDevice(Func<PdfDocumentBase, PdfRenderer> func)` — **in source, not in the readme**. Supplies a custom `Syncfusion.EJ2.PdfViewer.PdfRenderer` for PDF → PNG rendering. `SettingsTask` + `VerifySettings` overloads.
  - Registered converters: stream converters for `xlsx`, `xls`, `pdf`, `pptx`, `ppt`, `docx`, `doc`; file converters for `IWorkbook`, `PdfDocument`, `PdfLoadedDocument`, `IPresentation`, `WordDocument`.
- **Interactions with other Verify extensions**: readme mentions none explicitly. Conflicts in practice with `Verify.Sylvan.Data.Excel` (both register `xlsx`/`xls` stream converters). Test project also calls `VerifierSettings.UseSsimForPng(.7)` before `VerifierSettings.InitializePlugins()` to tolerate rendering differences in the PNG targets.
- **MSBuild / project requirements**:
  - **Syncfusion license required.** Readme: "An [Syncfusion License](https://www.syncfusion.com/sales/licensing) is required to use this tool." The repo's tests read a `SyncfusionLicense` environment variable and call `SyncfusionLicenseProvider.RegisterLicense(license)` in a `[ModuleInitializer]`, throwing `"Expected a \`SyncfusionLicense\` environment variable"` when absent. Without a license, rendered output is stamped with trial watermarks (visible in the sample verified files).
  - **Open Source Maintenance Fee.** Readme (important callout): "To ensure the long-term sustainability of this project, a monthly maintenance fee has been introduced. This fee is required to be paid by all organizations or users of this library. Pay the fee via [GitHub Sponsors](https://github.com/sponsors/VerifyTests). … To enact this, an EULA on binary releases has be added to the repo and Nuget packages that requires payment of the maintenance fee."
  - csproj sets `<PackageLicenseFile>OsmfEula.txt</PackageLicenseFile>` and `<PackageRequireLicenseAcceptance>true</PackageRequireLicenseAcceptance>`.
  - This is the only repo in this batch whose `Directory.Build.props` has **no** `Verify_SponsorshipExemption` properties.
  - Sample files need `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>`.
- **Sample verified output** (PDF info target):
```txt
{
  PageCount: 2,
  Author: ,
  CreationDate: DateTime_1,
  Creator: RAD PDF,
  CustomMetadata: [],
  Keywords: ,
  ModificationDate: DateTime_2,
  Producer: RAD PDF 3.9.0.0 - http://www.radpdf.com,
  Subject: ,
  Title: 
}
```
  Accompanied by `Samples.VerifyPdf#01.verified.png` (one PNG per rendered page/slide).
- **Notes for the wizard**: Emits multiple numbered targets per test (`#00` info, `#01…` rendered pages). Requires a license key at runtime — a generated sample should surface the `SyncfusionLicense` env var requirement prominently, and should probably default to `ExcludeTargets` for the binary document target so no large binary lands in source control. `VerifySyncfusion` is a `partial` class split per document type.

## Verify.SystemJson
- **NuGet package id(s)**: `Verify.SystemJson` (single package)
- **Current version**: `2.0.0`
- **Target frameworks**: `net48;net8.0;net9.0`
- **One-line description**: Adds Verify support for converting `System.Text.Json` types (`JsonDocument`, `JsonElement`, `JsonNode` family).
- **Tech tags**: json, system.text.json, serialization, stj
- **Third-party dependencies** (shipped package): `System.Text.Json` 10.0.12; `Verify` 33.1.0; `ProjectDefaults` 1.0.181 (private).
- **Initialize API**: `VerifySystemJson.Initialize()` — no parameters.
- **Requires InitializePlugins-compatible pattern?** yes (`Initialized` + `Initialize()` + `ThrowIfVerifyHasBeenRun()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifySystemJson.Initialize();
```
```cs
var document = JsonDocument.Parse(json);
return Verify(document);
```
- **Verbose / edge-case APIs**:
  - `UseStrictJson()` — readme: "Note that the above does not result in json files. [This is by design](…serializer-settings.md#not-valid-json). If json files are required then use [UseStrictJson]". Globally:
    ```cs
    [ModuleInitializer]
    public static void Init() =>
        VerifierSettings.UseStrictJson();
    ```
    Or per-test:
    ```cs
    var document = JsonDocument.Parse(json);
    return Verify(document)
        .UseStrictJson();
    ```
  - `ScrubMember(string)` / `IgnoreMember(string)` — core Verify APIs, documented here as working against JSON member names:
    ```cs
    var document = JsonDocument.Parse(json);
    return Verify(document)
        .ScrubMember("short")
        .IgnoreMember("msg");
    ```
  - Guid scrubbing — automatic: "Json values that map to known guid formats are scrubbed."
  - `ScrubInlineDates("yyyy/MM/dd")` / `ScrubInlineGuids()` — scrub dates/guids embedded inside string values:
    ```cs
    var document = JsonDocument.Parse(json);
    return Verify(document)
        .ScrubInlineDates("yyyy/MM/dd")
        .ScrubInlineGuids();
    ```
    Globally via `VerifierSettings.ScrubInlineDateTimes` and `VerifierSettings.ScrubInlineGuids`.
  - Converters registered: `JsonValueConverter`, `JsonArrayConverter`, `JsonObjectConverter`, `JsonElementConverter`, `JsonDocumentConverter`, `JsonPropertyConverter`.
- **Interactions with other Verify extensions**: readme says nothing about `Verify.NewtonsoftJson` or a SystemJson-vs-NewtonsoftJson choice — **not found** in the readme. (Worth the wizard flagging independently: both target JSON but different type families.)
- **MSBuild / project requirements**: none beyond a `System.Text.Json` reference. `<PackageRequireLicenseAcceptance>true</PackageRequireLicenseAcceptance>`. Repo has a second test project `TestsStrictJson` solely for the `UseStrictJson` variant.
- **Sample verified output** (default, non-strict — `.verified.txt`):
```txt
{
  short: {
    original: http://www.foo.com/,
    short: foo,
    error: {
      code: 0,
      msg: No action taken
    }
  }
}
```
  With `UseStrictJson()` it becomes a `.verified.json`:
```json
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
```
- **Notes for the wizard**: Two distinct snapshot shapes (txt vs json) depending on `UseStrictJson` — a wizard offering this extension should ask which. `InternalsVisibleTo.cs` present in the package source. The strict-json variant needs its own assembly (module initializer scope).

## Verify.Terminal
- **NuGet package id(s)**: `Verify.Tool` (note: the package id is **not** `Verify.Terminal`; the repo folder is). Tool command name: `dotnet-verify`. `IsPackable` is `false` by default in `Directory.Build.props`; only the `Verify.Terminal` project sets `IsPackable=true` / `PackAsTool=true`.
- **Current version**: not found — `src/Directory.Build.props` has **no** `<Version>`. Versioning is via **MinVer** 8.0.0 (git tags), with `MinVerSkip=true` in Debug and `MinVerDefaultPreReleaseIdentifiers=preview.0`.
- **Target frameworks**: `net10.0;net8.0` (`OutputType=Exe`)
- **One-line description**: A dotnet global tool for reviewing, accepting and rejecting pending Verify snapshots from the terminal.
- **Tech tags**: dotnet tool, cli, terminal, snapshot review, tooling, spectre.console
- **Third-party dependencies** (shipped package): `DiffEngine` 20.3.1; `DiffPlex` 1.9.0; `Microsoft.Extensions.DependencyInjection` 10.0.12; `Spectre.Console` 0.57.2; `Spectre.Console.Cli` 0.55.0; `Spectre.IO` 0.23.0; `Verify.ExceptionParsing` 33.1.0; `Spectre.Console.Analyzer` 1.0.0 (private); `MinVer` 8.0.0 (private); `Roslynator.Analyzers` 5.0.0 (private).
- **Initialize API**: **none** — this is an executable, not a library. There is no `VerifyTerminal` class and no `Initialize()`.
- **Requires InitializePlugins-compatible pattern?** no (not a plugin; nothing to discover)
- **Minimal usage**:
```bash
dotnet tool install -g verify.tool
```
```
> dotnet verify review
```
```
> dotnet verify accept
```
- **Verbose / edge-case APIs** (CLI surface, verbatim from readme):
  - `verify review [OPTIONS]` — `-h, --help`; `-w, --work <DIRECTORY>` (The working directory to use); `-c, --context <LINE-COUNT>` (The number of context lines to show. Defaults to 2)
  - `verify accept [OPTIONS]` — `-h, --help`; `-w, --work <DIRECTORY>`; `-y, --yes` (Confirm all prompts.)
  - `verify reject [OPTIONS]` — same options as `accept`.
  - **Inline snapshots**: "`review`, `accept` and `reject` all handle them beside file snapshots, so a run that produced both is dealt with in one pass. Accepting one rewrites the literal in the source file rather than moving a file, and `review` shows it as `(inline)`". Display form:
    ```
    ────────────────────────────────────────────────────────────────────────
    SampleTests.cs:42 (inline)
    ────────────────────────────────────────────────────────────────────────
    -old snapshot
    +new snapshot
    ```
  - **Inline queue ownership**: "Nothing is written to disk for a pending inline snapshot. The test run hands its patch to whichever process owns the inline queue, and only stages it under `obj/VerifyInline/` when nothing answers." The owner is "DiffEngineTray when one is running, and otherwise the DiffEngineViewer a test run launched."
  - **Snapshots that cannot be accepted**: "Conflicting snapshots" (multi-targeted runs disagreeing per framework) and "The call site could not be found" (the `Snapshot(...)` call was changed/removed).
  - **Pairing of received → verified files**: "From [Verify 31.27.0] … whenever a received file is left on disk, Verify records the verified file it belongs to. This tool reads those records, so the pairing is exact rather than guessed." Example: `MyTests.MyTest.DotNet11_0.received.txt  ->  MyTests.MyTest.verified.txt`. Fallback matching handles "multi targeting, `UniqueFor*`, and a trailing ignored parameter" but not a brand-new snapshot's runtime suffix nor "a leading or middle ignored parameter". Rerouted pairings are shown as `(rerouted)`.
- **Interactions with other Verify extensions**: Integrates with DiffEngine (DiffEngineTray / DiffEngineViewer own the inline-snapshot queue) and reads Verify's received-map records. No `Initialize()` ordering concerns. Uses `Spectre.Verify.Extensions` 28.16.0 + `Verify.XunitV3` in its own tests.
- **MSBuild / project requirements**: The working directory **must contain `obj`** — "Staged snapshots live in the intermediate (`obj`) directory of the test project, so … the working directory has to contain `obj`" and "Pointing `-w` at a snapshot subdirectory alone means the records are not seen." SDK pinned in `global.json` to 10.0.401 with `rollForward: latestFeature`; `"test": { "runner": "Microsoft.Testing.Platform" }`. Built via `dotnet build.cs` (a C# file-based build script, not a .sln target). Licensed MIT; authors "Patrik Svensson, Simon Cropp"; repo URL points at `github.com/patriksvensson/verify.terminal`.
- **Sample verified output**: not applicable (the tool does not produce snapshots).
- **Notes for the wizard**: **This is a dotnet tool, not a library.** A wizard must not generate `PackageReference Include="Verify.Terminal"` or a `ModuleInitializer` for it — generate `dotnet tool install -g verify.tool` (or a `dotnet-tools.json` manifest entry for `verify.tool`) plus `dotnet verify review` usage. The package id/repo-folder mismatch (`Verify.Tool` vs `Verify.Terminal`) is the single biggest trap here. Readme lives at `README.md` (uppercase) and `LICENSE.md`/`CODE_OF_CONDUCT.md` are also uppercase, unlike every other repo in this set.

## Verify.Ulid
- **NuGet package id(s)**: `Verify.Ulid` (single package)
- **Current version**: `1.0.1`
- **Target frameworks**: `net48;net8.0;net9.0`
- **One-line description**: Extends Verify to enable scrubbing of Universally Unique Lexicographically Sortable Identifiers (ULIDs) via the Ulid package.
- **Tech tags**: ulid, identifiers, scrubbing, guids, sortable ids
- **Third-party dependencies** (shipped package): `Ulid` 1.4.1; `Verify` 33.1.0; `Polyfill` 11.4.0 (private); `ProjectDefaults` 1.0.181 (private).
- **Initialize API**: `VerifyUlid.Initialize()` — no parameters. Readme: "Call `VerifyUlid.Initialize()` once at assembly load time."
- **Requires InitializePlugins-compatible pattern?** yes (`Initialized` + `Initialize()` + `ThrowIfVerifyHasBeenRun()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyUlid.Initialize();
```
```cs
[Test]
public Task UlidScrubbing()
{
    var id = Ulid.NewUlid();
    var target = new Person
    {
        Id = id,
        Name = "Sarah",
        Description = $"Sarah ({id})"
    };
    return Verify(target);
}
```
- **Verbose / edge-case APIs**:
  - `DontScrubUlids()` — disables ULID scrubbing; available on `SettingsTask` (fluent) and `VerifySettings` (instance).
    ```cs
    [Test]
    public Task DontScrubFluent()
    {
        var id = Ulid.Parse("01JGXG0GDGQEP47CBQ65E50HYH");
        var target = new Person
        {
            Id = id,
            Name = "Sarah",
            Description = $"Sarah ({id})"
        };
        return Verify(target)
            .DontScrubUlids();
    }

    [Test]
    public Task DontScrubInstance()
    {
        var id = Ulid.Parse("01JGXG0GDGQEP47CBQ65E50HYH");
        var target = new Person
        {
            Id = id,
            Name = "Sarah",
            Description = $"Sarah ({id})"
        };
        var settings = new VerifySettings();
        settings.DontScrubUlids();
        return Verify(target, settings);
    }
    ```
  - Mechanism: registers a `UlidConverter` for typed `Ulid` members plus `VerifierSettings.ScrubWindow(26, 26, ScrubInline, requireWordBoundary: true)` for inline ULIDs embedded in strings. Counter-based naming produces `Ulid_1`, `Ulid_2`, … and is consistent between typed and inline occurrences of the same value.
- **Interactions with other Verify extensions**: readme mentions none. Conceptually parallel to Verify's built-in Guid scrubbing. Test project also initializes `VerifyDiffPlex.Initialize(OutputType.Compact)` and `VerifierSettings.InitializePlugins()`.
- **MSBuild / project requirements**: none.
- **Sample verified output**:
```txt
{
  Id: Ulid_1,
  Name: Sarah,
  Description: Sarah (Ulid_1)
}
```
  With `DontScrubUlids()`:
```txt
{
  Id: 01JGXG0GDGQEP47CBQ65E50HYH,
  Name: Sarah,
  Description: Sarah (01JGXG0GDGQEP47CBQ65E50HYH)
}
```
- **Notes for the wizard**: Purely a scrubber — no "verify an X" entry point; the value is that it works with no per-test opt-in. Inline scrubbing only fires for 26-character word-boundary windows that are all letter-or-digit and parse as a valid ULID, so false positives are unlikely but a wizard sample should use a realistic ULID. Has both fluent (`SettingsTask`) and instance (`VerifySettings`) forms of `DontScrubUlids`.

## Verify.WinForms
- **NuGet package id(s)**: `Verify.WinForms` (single package)
- **Current version**: `6.0.0`
- **Target frameworks**: `net48;net10.0-windows` (with `<UseWindowsForms>true</UseWindowsForms>`)
- **One-line description**: Extends Verify to allow verification of WinForms UIs by rendering controls to PNG.
- **Tech tags**: winforms, windows forms, ui, desktop, gui, screenshot, png, windows
- **Third-party dependencies** (shipped package): `Verify` 33.1.0; `Polyfill` 11.4.0 (private); `ProjectDefaults` 1.0.181 (private). No other runtime deps — `System.Drawing` comes from the WinForms SDK. (`System.Drawing.Common` 10.0.12 is in Directory.Packages.props but not referenced by the shipped csproj.)
- **Initialize API**: `VerifyWinForms.Initialize()` — no parameters.
- **Requires InitializePlugins-compatible pattern?** yes (`Initialized` + `Initialize()` + `ThrowIfVerifyHasBeenRun()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyWinForms.Initialize();
```
```cs
[Test]
public Task FormUsage() =>
    Verify(new MyForm());
```
- **Verbose / edge-case APIs**:
  - `ContextMenuStrip` verification (its own registered converter, since a menu must be hosted to render):
    ```cs
    [Test]
    public Task ContextMenuStrip()
    {
        var menu = new ContextMenuStrip();
        var items = menu.Items;

        items.Add(new ToolStripMenuItem("About"));
        items.Add(new ToolStripMenuItem("Exit"));
        return Verify(menu);
    }
    ```
  - File converters registered for four types: `Form`, `ContextMenuStrip`, `UserControl`, `Control`. Each produces a single `png` target via `Control.DrawToBitmap` at `PixelFormat.Format32bppArgb`.
  - **OS-specific rendering** (readme): "The rendering of Form elements can very slightly between different OS versions. This can make verification on different machines (eg CI) problematic. There are several approaches to mitigate this: Using a [custom comparer](…/docs/comparer.md)". The repo's own tests use `VerifierSettings.UniqueForRuntime()` and `VerifierSettings.UseSsimForPng()`.
- **Interactions with other Verify extensions**: readme names only the custom-comparer doc. In practice pairs with an image comparer (`Verify.Phash`, or core `UseSsimForPng`). Conceptually parallel to `Verify.Xaml` (WPF) and Verify.Avalonia — all three register UI-element file converters, but for disjoint type hierarchies so they can coexist.
- **MSBuild / project requirements**: **Windows only.** Test project needs `<TargetFramework>net10.0-windows</TargetFramework>` + `<UseWindowsForms>true</UseWindowsForms>` (and, in the repo, `<SignAssembly>false</SignAssembly>`). `NoWarn` includes `CA1416` (platform compatibility) in `Directory.Build.props`.
- **Sample verified output**: binary PNG only (`TheTests.FormUsage.Net.verified.png`, `TheTests.ContextMenuStrip.Net.verified.png`) — no text snapshot. The `.Net` suffix comes from `UniqueForRuntime()`.
- **Notes for the wizard**: The source carries a long comment worth surfacing — showing a WinForms control installs a `WindowsFormsSynchronizationContext` on the current thread, which would deadlock Verify's async IO, so the converter saves and restores `SynchronizationContext.Current` around rendering. Unlike Verify.Xaml, WinForms tests do **not** need an STA apartment attribute in this repo's test suite. PNG-only output means a generated sample should also set up an image comparer to avoid brittle CI failures.

## Verify.Wolverine
- **NuGet package id(s)**: `Verify.Wolverine` (single package)
- **Current version**: `3.2.0`
- **Target frameworks**: `net9.0` (single TFM — `<TargetFramework>`, not `<TargetFrameworks>`)
- **One-line description**: Adds Verify support for verifying Wolverine message handlers via a recording `IMessageBus` test context.
- **Tech tags**: wolverine, messaging, message bus, cqrs, handlers, jasperfx
- **Third-party dependencies** (shipped package): `WolverineFx` 6.39.1; `Verify` 33.1.0; `Polyfill` 11.4.0 (private); `ProjectDefaults` 1.0.181 (private). (`System.Text.Json` 10.0.12 and `Microsoft.Extensions.Caching.Memory` 10.0.12 are pinned in Directory.Packages.props "explicit to avoid a cve" but not directly referenced by the shipped csproj.)
- **Initialize API**: `VerifyWolverine.Initialize()` — no parameters. **The body only sets `Initialized = true`**; it registers no converters and does not call `ThrowIfVerifyHasBeenRun()`. It is effectively a no-op kept for the plugin-discovery contract.
- **Requires InitializePlugins-compatible pattern?** yes (`Initialized` + `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyWolverine.Initialize();
```
```cs
public class Handler(IMessageBus context)
{
    public ValueTask Handle(Message message) =>
        context.SendAsync(new Response("Property Value"));
}
```
```cs
[Fact]
public async Task HandlerTest()
{
    var context = new RecordingMessageContext();
    var handler = new Handler(context);
    await handler.Handle(new Message("value"));
    await Verify(context);
}
```
  Readme: "Pass in instance of `RecordingMessageContext` in to the `Handle` method and then `Verify` that instance."
- **Verbose / edge-case APIs**:
  - `RecordingMessageContext.AddInvokeResult<T>(InvokeResult<T> invokeResult)` — supplies the "reply" for request/reply. Readme: "When using [Request/Reply] via `IMessageBus.InvokeAsync<T>` the message context is required to supply the \"Reply\" part."
    ```cs
    [Fact]
    public async Task HandlerTest()
    {
        var context = new RecordingMessageContext();
        context.AddInvokeResult<Response>(
            message =>
            {
                var request = (Request) message;
                return new Response(request.Property);
            });
        var handler = new Handler(context);
        await handler.Handle(new Message("value"));
        await Verify(context);
    }
    ```
  - `RecordingMessageContext.AddInvokeResult<T>(T result) where T : notnull` — constant-result overload (in source, not shown in readme).
  - `RecordingMessageContext(object? message = null, PreviewSubscription? previewSubscription = null)` — constructor parameters (not documented in readme).
  - Recorded collections exposed as properties: `Sent`, `Published`, `Invoked` (`IReadOnlyList<Invoked>`), `Broardcasted` (`IReadOnlyList<Broadcasted>`, note the spelling in the API), plus `Envelope`, `CorrelationId`, `TenantId`, `UserName`.
  - Record types: `Invoked(object Message, TimeSpan? Timeout = null, string? Endpoint = null, string? Tenant = null)`; `Broadcasted(string Topic, object Message, DeliveryOptions? Options = null)`; delegates `InvokeResult<out T>(object message)` and `PreviewSubscription(object message, DeliveryOptions options)`.
  - Supported context surface: `SendAsync`, `PublishAsync`, `InvokeAsync`/`InvokeAsync<T>`, `InvokeForTenantAsync`/`InvokeForTenantAsync<T>`, `BroadcastToTopicAsync`, `EndpointFor(string)` / `EndpointFor(Uri)` → `DestinationEndpoint` (with `SendAsync`, `InvokeAsync`, `SendRawMessageAsync`, `Uri`, `EndpointName`), `PreviewSubscriptions`, `RespondToSender`, `ReScheduleAsync`, streaming.
  - Readme on why it exists vs the built-in: "Uses the same pattern as the [Wolverine TestMessageContext] with some additions: All messaging parameters, eg DeliveryOptions and timeout, can be asserted. Support for `IMessageBus.InvokeAsync<T>` via [AddInvokeResult]."
- **Interactions with other Verify extensions**: readme mentions none. Test project uses `Verify.XunitV3` + `Verify.DiffPlex` and calls `VerifierSettings.InitializePlugins()`.
- **MSBuild / project requirements**: `SignAssembly=false` (not strong-named). `<PackageRequireLicenseAcceptance>true</PackageRequireLicenseAcceptance>`. Single TFM net9.0 — consumers on net8.0 or net10.0-only are not supported. No running broker/infrastructure required: `RecordingMessageContext` is purely in-memory.
- **Sample verified output**:
```txt
{
  Sent: [
    {
      Message: {
        Property: Property Value
      }
    }
  ]
}
```
- **Notes for the wizard**: The odd one out — `Initialize()` does nothing, so the real "API" is the `RecordingMessageContext` type, not converter registration. A generated sample must show constructing `RecordingMessageContext`, injecting it as `IMessageBus`, and verifying the context object. Note the misspelled public member `Broardcasted` (and the file `RecordingMessageContext_Broardcast.cs`) — copy it verbatim. Xunit v3 is the framework used in the repo's samples.

## Verify.Xaml
- **NuGet package id(s)**: `Verify.Xaml` (single package)
- **Current version**: `5.0.0`
- **Target frameworks**: `net48;net10.0-windows` (with `<UseWPF>true</UseWPF>`)
- **One-line description**: Extends Verify to allow verification of XAML/WPF UIs, producing both an XAML tree snapshot and a rendered PNG.
- **Tech tags**: xaml, wpf, ui, desktop, gui, screenshot, png, windows
- **Third-party dependencies** (shipped package): `Verify` 33.1.0; `ProjectDefaults` 1.0.181 (private). Nothing else — WPF comes from the SDK. (`Shipwreck.Phash.Bitmaps` 0.5.0, `Verify.Phash` 3.1.0 and `System.Drawing.Common` 10.0.12 are test-only.)
- **Initialize API**: `VerifyXaml.Initialize()` — no parameters.
- **Requires InitializePlugins-compatible pattern?** yes (`Initialized` + `Initialize()` + `ThrowIfVerifyHasBeenRun()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyXaml.Initialize();
```
```cs
[Test]
public async Task WindowUsage()
{
    var window = new MyWindow();
    await Verify(window);
}
```
  Readme: "A visual element (Window/Page/Control etc) can then be verified as follows".
- **Verbose / edge-case APIs**:
  - File converters registered for exactly two types: `Window` and `FrameworkElement`. Each returns **two targets**: an `xml` target (the serialized XAML tree, inserted first) and a `png` target (the rendered screenshot). If XAML serialization returns null, only the PNG is emitted.
  - **OS-specific rendering** (readme): "The rendering of XAML elements can very slightly between different OS versions. This can make verification on different machines (eg CI) problematic. There are several approaches to mitigate this: [Forcing elements to use a specific theme](https://arbel.net/2006/11/03/forcing-wpf-to-use-a-specific-windows-theme/); Using a [custom comparer](…/docs/comparer.md)".
  - Internals worth knowing (from claude.md, not the readme): `WpfUtils.ToXamlString` serializes via `XamlWriter.Save` into an `XDocument`, then re-emits with `NewLineOnAttributes = true` for stable diffs. `Purge` strips attributes the screen-capture path mutates — `AllowsTransparency`, `ShowInTaskbar`, `WindowStyle`, `Opacity`, `Visibility` — "so the captured XAML matches the original element". `ScreenCapture` "shows the window invisibly (Opacity 0, transparent, no taskbar) to force layout, then renders to a PNG" via `RenderTargetBitmap`. `HostWindow.xaml(.cs)` hosts bare `FrameworkElement`s for capture.
- **Interactions with other Verify extensions**: readme names only the comparer doc. The test project references `Verify.Phash` 3.1.0 + `Shipwreck.Phash.Bitmaps` for perceptual image comparison of the PNG target — the practical pairing for CI. Coexists fine with `Verify.WinForms` (disjoint type hierarchies) but both are Windows-only.
- **MSBuild / project requirements**: **Windows only, WPF.** Test project needs `<TargetFramework>net10.0-windows</TargetFramework>` + `<UseWPF>true</UseWPF>` (+ `SignAssembly=false` in the repo). **Tests must run on an STA thread** — claude.md: "Because the converter mutates a real WPF window during rendering, tests must run on an STA thread — `src/Tests/TheTests.cs` is annotated `[Apartment(ApartmentState.STA)]`." `NoWarn` includes `CA1416`. SDK pinned in `src/global.json` (10.0.202, prerelease allowed, `latestFeature`).
- **Sample verified output** (`.verified.xml`, truncated — the real file is ~203 lines; a `.verified.png` accompanies it):
```xml
<MyWindow
  Title="MyWindow"
  Width="525"
  Height="350"
  xmlns="clr-namespace:Tests;assembly=Tests"
  xmlns:av="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
  <av:DockPanel
    Name="MyPanel">
    <av:Menu
      Height="26"
      av:DockPanel.Dock="Top">
      <av:MenuItem
        Header="File">
        <av:MenuItem
          Header="Exit" />
      </av:MenuItem>
    </av:Menu>
  </av:DockPanel>
</MyWindow>
```
- **Notes for the wizard**: Two targets per verification (xml + png) — the xml one is the diffable, review-friendly snapshot; the png is what needs a fuzzy comparer. **Must generate `[Apartment(ApartmentState.STA)]` (NUnit) or the equivalent for the chosen framework** or tests will fail outright. Package tags are `Xaml, Wpf, Verify` — WPF-only, not UWP/WinUI/MAUI/Avalonia.

## Verify.Yaml
- **NuGet package id(s)**: `Verify.Yaml` (single package)
- **Current version**: `0.1.0` (pre-1.0)
- **Target frameworks**: `net48;net6.0;net7.0;net8.0;net9.0`
- **One-line description**: Adds Verify support for converting YamlDotNet types.
- **Tech tags**: yaml, yamldotnet, serialization, configuration
- **Third-party dependencies** (shipped package): `YamlDotNet` 18.1.0; `Verify` 33.1.0; `Polyfill` 11.4.0 (private); `ProjectDefaults` 1.0.181 (private).
- **Initialize API**: `VerifyYaml.Initialize()` — no parameters.
- **Requires InitializePlugins-compatible pattern?** yes (`Initialized` + `Initialize()` + `ThrowIfVerifyHasBeenRun()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyYaml.Initialize();
```
```cs
[Test]
public Task YamlDocumentSample()
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

    var input = new StringReader(yaml);
    var yamlStream = new YamlStream();
    yamlStream.Load(input);
    return Verify(yamlStream);
}
```
- **Verbose / edge-case APIs**:
  - `ScrubMember(string)` / `IgnoreMember(string)` — core Verify APIs applied to YAML nodes:
    ```cs
    return Verify(yamlStream)
        .ScrubMember("short")
        .IgnoreMember("msg");
    ```
  - Automatic date and Guid scrubbing: "Json values that map to known date and time formats are scrubbed." (readme wording says "Json"; it applies to YAML scalar values). Produces `Date_1`, `Guid_1`.
  - `ScrubInlineDates("yyyy/MM/dd")` / `ScrubInlineGuids()` — for dates/guids embedded inside scalar strings:
    ```cs
    return Verify(yamlStream)
        .ScrubInlineDates("yyyy/MM/dd")
        .ScrubInlineGuids();
    ```
    Globally: `VerifierSettings.ScrubInlineDateTimes`, `VerifierSettings.ScrubInlineGuids`.
  - Converters registered: `YamlStreamConverter`, `YamlDocumentConverter`, `YamlMappingNodeConverter`, `YamlSequenceNodeConverter`, `YamlScalarNodeConverter`. So `YamlStream`, `YamlDocument`, `YamlMappingNode`, `YamlSequenceNode` and `YamlScalarNode` are all verifiable.
- **Interactions with other Verify extensions**: readme mentions none. Test project uses `Verify.NUnit` + `Verify.DiffPlex` and `VerifierSettings.InitializePlugins()`.
- **MSBuild / project requirements**: none. `<PackageRequireLicenseAcceptance>true</PackageRequireLicenseAcceptance>`. Note the `Directory.Build.props` `<Description>` has a typo: "converting YamlDotNey types".
- **Sample verified output**:
```txt
[
  {
    node: {
      error: {
        msg: No action taken,
        code: 0
      },
      short: foo,
      original: http://www.foo.com/
    }
  }
]
```
- **Notes for the wizard**: Verifying a `YamlStream` produces a **JSON-ish array** (one entry per document in the stream), not YAML — the snapshot is a `.verified.txt` in Verify's relaxed JSON format, and **member ordering is not source order** (note `error` before `short` before `original` above). Version is 0.1.0, so the API may be less stable than siblings. Widest TFM span in this batch (net48 through net9.0, including net6.0/net7.0).

## Verify.ZeroLog
- **NuGet package id(s)**: `Verify.ZeroLog` (single package)
- **Current version**: `2.0.0`
- **Target frameworks**: `net8.0;net9.0` (`<OutputType>Library</OutputType>` set explicitly)
- **One-line description**: Extends Verify to allow verification of ZeroLog log output, capturing logged messages into Verify's `Recording`.
- **Tech tags**: logging, zerolog, log capture, recording, low allocation logging
- **Third-party dependencies** (shipped package): `ZeroLog` 2.6.1; `Verify` 33.1.0; `ProjectDefaults` 1.0.181 (private).
- **Initialize API**: `VerifyZeroLog.Initialize()` — no parameters. Side effects beyond converter registration: if `LogManager.Configuration` is already set, it adds a `VerifyAppender` to `config.RootLogger.Appenders` and calls `config.ApplyChanges()`; otherwise it builds `ZeroLogConfiguration.CreateTestConfiguration()`, adds the appender and calls `LogManager.Initialize(config)`.
- **Requires InitializePlugins-compatible pattern?** yes (`Initialized` + `Initialize()` + `ThrowIfVerifyHasBeenRun()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Initialize() =>
    VerifyZeroLog.Initialize();
```
```cs
[Fact]
public Task Usage()
{
    Recording.Start();
    var result = Method();

    return Verify(result);
}

static string Method()
{
    var logger = LogManager.GetLogger<Tests>();
    logger.Error("The error");
    logger.Warn("The warning");
    return "Result";
}
```
- **Verbose / edge-case APIs**:
  - `Recording.Start()` / `Recording.Stop()` — core Verify recording. Log entries are appended under the name `log`.
  - `VerifyTests.ZeroLog.RecordingLogger` — **obsolete**, marked `[Obsolete("Use VerifyTests.Recording")]` and now an empty static class. Do not generate.
  - Converters registered: `KeyValueListConverter`, `LoggedMessageConverter`.
  - `VerifyAppender` — the ZeroLog appender type this package installs (internal to the flow; not something users construct).
- **Interactions with other Verify extensions**: readme mentions none. Because `Initialize()` mutates global `LogManager` state, it should run once per test assembly; the repo's test project calls it in one ModuleInitializer and `VerifierSettings.InitializePlugins()` in a second. Shares the `Recording` mechanism with Verify.SqlServer (`sql`), Verify.EntityFramework (`ef`) and other recording extensions — entries are distinguished by name, so they can coexist in one snapshot.
- **MSBuild / project requirements**: none beyond a ZeroLog reference. Note `<PackageTags>Http, Verify</PackageTags>` in `Directory.Build.props` is wrong/copy-pasted (should be logging-related) — do not propagate. Shortest TFM list in this batch (net8.0/net9.0 only; no net48).
- **Sample verified output**:
```txt
{
  target: Result,
  log: [
    {
      Error: The error,
      Logger: Tests
    },
    {
      Warn: The warning,
      Logger: Tests
    }
  ]
}
```
- **Notes for the wizard**: Shortest readme in the set — no sections beyond Usage. The snapshot key is `log`, and each entry's property name is the log level (`Error:`, `Warn:`) with a `Logger:` field. `Initialize()` takes over ZeroLog's global `LogManager` configuration, which will surprise anyone who configures ZeroLog themselves — a generated sample should either configure ZeroLog *before* `VerifyZeroLog.Initialize()` (so the appender is added to the existing config) or leave configuration entirely to the extension. Uses xunit.v3 in the repo's samples.

## LocalDb
*(This repo is `D:\Code\LocalDb` — the LocalDb project by the same author, not a `Verify.*` repo. It ships several packages, four of which are Verify integrations.)*

- **NuGet package id(s)** (7 shipped packages, all from one repo):
  - **Core:** `LocalDb` (raw `SqlConnection`), `EfLocalDb` (EF Core), `EfClassicLocalDb` (EF6 / EF Classic)
  - **Verify integrations (one per test framework):** `EfLocalDb.NUnit`, `EfLocalDb.Xunit.V3`, `EfLocalDb.MSTest`, `EfLocalDb.TUnit`
  - Not packaged: `LocalDb.MultiProcessHelper`, `EfLocalDb.ReportTrxHelper`, `Benchmark`, `LoadTest`, `Helpers`, `StartUpScript` (`GeneratePackageOnBuild=false` / helper exes).
  - There is **no** package named `Verify.LocalDb`, `LocalDb.Verify` or `EfLocalDb.Verify`. The Verify integration is delivered through the four `EfLocalDb.<TestFramework>` packages, each of which `PackageReference`s `Verify.EntityFramework` + the matching `Verify.<Framework>` adapter and exposes a `LocalDbTestBase<T>` base class.
- **Current version** (from src/Directory.Build.props `<Version>`): `26.2.0` — shared by every package in the repo.
- **Target frameworks**:
  - `LocalDb`: `net8.0;net9.0;net10.0;net48`
  - `EfClassicLocalDb`: `net8.0;net9.0;net10.0;net48`
  - `EfLocalDb`: `net10.0`
  - `EfLocalDb.NUnit` / `.Xunit.V3` / `.MSTest` / `.TUnit`: `net10.0`
- **One-line description**: Provides a wrapper around SqlLocalDB to simplify running tests against Entity Framework or a raw SQL database, with an isolated database per test method; the `EfLocalDb.<TestFramework>` packages fold Verify + Verify.EntityFramework into an Arrange-Act-Assert test base class.
- **Tech tags**: localdb, sql server, database, entity framework, entity framework core, ef6, testing, test isolation, nunit, xunit, mstest, tunit
- **Third-party dependencies** (shipped packages, from `src/Directory.Packages.props`):
  - `LocalDb`: `Microsoft.Data.SqlClient` 7.1.0, `Microsoft.Win32.Registry` 5.0.0, `System.IO.FileSystem.AccessControl` 5.0.0, `System.Memory` 4.6.3, `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.12 (net8.0/net9.0 only); private: `ConfigureAwait.Fody` 3.4.1, `Fody` 6.9.3, `MethodTimer.Fody` 3.2.3, `Polyfill` 11.4.0, `ProjectDefaults` 1.0.181.
  - `EfLocalDb`: `Microsoft.EntityFrameworkCore.SqlServer` 10.0.12, `Microsoft.Data.SqlClient` 7.1.0, `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.12, `System.IO.FileSystem.AccessControl` 5.0.0; same private Fody/Polyfill set.
  - `EfClassicLocalDb`: `EntityFramework` 6.5.2, `System.Data.SqlClient` 4.9.1, `Microsoft.Win32.Registry` 5.0.0, `System.Drawing.Common` 10.0.12 (explicit, CVE), `System.IO.FileSystem.AccessControl` 5.0.0.
  - `EfLocalDb.NUnit`: `Verify.EntityFramework` **15.4.1**, `Verify.NUnit` 33.1.1, `Verify` 33.1.1, `Argon` 0.37.0, `DiffEngine` 20.3.1, `EmptyFiles` 8.18.2, `SimpleInfoName` 3.2.0 + project ref to `EfLocalDb`.
  - `EfLocalDb.Xunit.V3`: same, with `Verify.XunitV3` 33.1.1.
  - `EfLocalDb.MSTest`: same, with `Verify.MSTest` 33.1.1.
  - `EfLocalDb.TUnit`: same, with `Verify.TUnit` 33.1.1 and `TUnit` 1.68.17.
  - Also pinned for tests: `Verify.SqlServer` **12.2.0**, `Verify.DiffPlex` 3.3.1, `NetTopologySuite` 2.6.0, `Microsoft.EntityFrameworkCore.SqlServer.NetTopologySuite` 10.0.12.
- **Initialize API**:
  - Verify-integration packages: `LocalDbTestBase<T>.Initialize()` — readme: "`LocalDbTestBase<T>.Initialize` needs to be called once. This is best done in a [ModuleInitializer]". Typically alongside `VerifierSettings.InitializePlugins()`.
  - Raw/EF-core packages have no Verify `Initialize`; instead construct a `SqlInstance` / `SqlInstance<TDbContext>` once (static constructor or module initializer) and call `Build()` per test.
- **Requires InitializePlugins-compatible pattern?** no — there is no `VerifyLocalDb` class with an `Initialized` property. `LocalDbTestBase<T>.Initialize()` is a separate, explicitly-called static. The Verify plugins that *are* discovered by `InitializePlugins()` in a LocalDb test project are `Verify.EntityFramework` and (in `LocalDb.Tests`) `Verify.SqlServer`.
- **Minimal usage**:
```cs
public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifierSettings.Inline(maxLines: 10, applyMaxLinesToExisting: true);
        VerifierSettings.InitializePlugins();
        // AiCliDetector.Prefix ("chatbot_") is prepended to the LocalDb instance name when
        // running under an AI CLI (e.g. Claude Code). Scrub it so snapshots that capture the
        // instance name / DataSource are stable regardless of environment. No-op otherwise.
        VerifierSettings.ScrubReplace("chatbot_", "");
        LocalDbLogging.EnableVerbose();
        LocalDbSettings.ConnectionBuilder(_ => _.ConnectTimeout = 300);
        LocalDbTestBase<TheDbContext>.Initialize();
    }
}
```
  Raw `LocalDb` package:
```cs
static SqlInstance sqlInstance = new(
    name: "StaticConstructorInstance",
    buildTemplate: TestDbBuilder.CreateTable);

[Test]
public async Task Test()
{
    await using var database = await sqlInstance.Build();
    await TestDbBuilder.AddData(database);
    var data = await TestDbBuilder.GetData(database);
    AreEqual(1, data.Count);
}
```
  `EfLocalDb` package:
```cs
static SqlInstance<MyDbContext> sqlInstance;

static EfSnippetTests() =>
    sqlInstance = new(builder => new(builder.Options));

[Test]
public async Task TheTest()
{
    await using var database = await sqlInstance.Build();

    await using (var data = database.NewDbContext())
    {
        var entity = new TheEntity
        {
            Property = "prop"
        };
        data.Add(entity);
        await data.SaveChangesAsync();
    }
}
```
- **Verbose / edge-case APIs**:
  - `LocalDbTestBase<T>` phase properties — `ArrangeData`, `ActData`, `AssertData`: "These enforce phase ordering: accessing `ActData` transitions from Arrange to Act, and accessing `AssertData` transitions to Assert. Accessing a phase out of order throws an exception."
  - `VerifyEntity<TEntity>(Guid id)` / `(int id)` / `(long id)` → returns `QueryableSettingsTask<TEntity>`, enabling `.Include(...)` / `.ThenInclude(...)` chaining before verification. Uses `AsSplitQuery()` and resolves the primary key predicate via EF internals.
    ```cs
    [TestMethod]
    public async Task VerifyEntityById()
    {
        ...
        await VerifyEntity<Company>(company.Id);
    }
    ```
  - `VerifyEntity<TEntity>(IQueryable<TEntity> entities)` → `SettingsTask`; resolves via `SingleAsync()`.
  - `VerifyEntities<TEntity>(IQueryable<TEntity> entities)` → `SettingsTask`; resolves via `ToListAsync()`. Works with a `DbSet` or an `IQueryable`.
  - Both throw a wrapped error on `ObjectDisposedException`: `"ObjectDisposedException while executing IQueryable. It is possible the IQueryable targets an ActData or ArrangeData that has already been cleaned up"`.
  - Database-lifecycle attributes shipped by the integration packages: `[NewDb]`, `[PooledDb]`, `[SharedDb]`, `[SharedDbWithTransaction]`; plus `ILocalDbTestBase`, `Phase`, `CombinationCallbackSetup`.
  - Verify **Combinations** support: "[Verify Combinations] are supported. The database is reset for each combination" — `.Verify(Run, inputs)`.
  - Core LocalDb APIs: `SqlInstance` / `SqlInstance<TDbContext>` with `Build([CallerFilePath] string testFile, string? databaseSuffix, [CallerMemberName] string memberName)`, `BuildShared()`, `SqlDatabase.NewDbContext()`, `SqlDatabase.Connection`, `ConnectionString`; settings `LocalDbSettings.ConnectionBuilder(...)`, `LocalDbSettings.InstanceCleanupThreshold`, `LocalDbLogging.EnableVerbose()`, `ShutdownMode`, `ExistingTemplate`, `RowVersions`, `LocalDbInstanceInfo`.
  - Documented anti-pattern (readme): "**Do not call Build once and share the ConnectionString** … This defeats the purpose of LocalDb's template cloning. All tests share the same database, requiring manual cleanup (`DELETE FROM ...`) between tests, preventing parallel execution, and risking test interference." Instead "call `Build()` in each test method to get an isolated database clone".
- **Interactions with other Verify extensions** (quoted from the docs):
  - `pages/ef-nunit-usage.md`: "Combines [EfLocalDb](/pages/ef-usage.md), [NUnit](https://nunit.org/), [Verify.NUnit](https://github.com/VerifyTests/Verify#verifynunit), and [Verify.EntityFramework](https://github.com/VerifyTests/Verify.EntityFramework) into a test base class that provides an isolated database per test with [Arrange-Act-Assert](…) phase enforcement." The same sentence appears verbatim in `ef-mstest-usage.md` (Verify.MSTest), `ef-xunitv3-usage.md` (Verify.XunitV3) and `ef-tunit-usage.md` (Verify.TUnit).
  - Every integration page's ModuleInitializer snippet calls `VerifierSettings.InitializePlugins();` before `LocalDbTestBase<T>.Initialize();`.
  - `src/LocalDb.Tests` pairs the raw `LocalDb` package with **`Verify.SqlServer` 12.2.0** + `Verify.NUnit` + `Verify.DiffPlex`, using `VerifierSettings.InitializePlugins()` — this is the canonical LocalDb + Verify.SqlServer combination.
  - From the Verify.SqlServer side: `Verify.SqlServer`'s own `Directory.Packages.props` pins `LocalDb` 26.2.0 for its tests — so the two repos reference each other, and the readme's `recordCommands: false` note is exactly the guidance for LocalDb + EF + SqlServer stacks (EF records under `ef`, SqlServer under `sql`).
  - The instance-name scrubbing note (quoted in the Minimal usage snippet above) is LocalDb-specific and matters for stable snapshots under an AI CLI.
- **MSBuild / project requirements**:
  - **Windows only.** Readme, bolded: "**SqlLocalDB is only supported on Windows**".
  - Requires a SqlLocalDB installation (part of SQL Server Express LocalDB); processes start/stop automatically. `LocalDb` and `EfLocalDb` ship `buildTransitive\*.props` / `build\*.props` MSBuild assets.
  - `EfLocalDb`, `EfClassicLocalDb` and all four integration packages define `$(DefineConstants);EF`.
  - Test projects in the repo use `OutputType=Exe` with `EnableNUnitRunner` / `TestingPlatformDotnetTestSupport` (Microsoft.Testing.Platform).
  - When running with `--report-trx`, the docs say to call `Initialize` from assembly setup instead of a module initializer (see `/pages/report-trx.md`).
  - Uses Fody weavers (`ConfigureAwait.Fody`, `MethodTimer.Fody`) — consumers don't need Fody, but repo builds do.
  - Assembly is strong-named (`key.snk`); `ContinuousIntegrationBuild=false`; `AllowUnsafeBlocks=true`.
- **Sample verified output**: none of the readme/pages in this repo show a full `.verified.txt`; `VerifyEntity`/`VerifyEntities` output is standard Verify.EntityFramework entity serialization. Marked **not found**.
- **Notes for the wizard**:
  - The Verify-integration package **name pattern is `EfLocalDb.<TestFramework>`**, not `Verify.*` — a wizard guessing `Verify.LocalDb` will be wrong.
  - `EfLocalDb.NUnit`, `.Xunit.V3` and `.TUnit` are all built from the **same source files linked out of `EfLocalDb.MSTest`** (`ILocalDbTestBase.cs`, `Phase.cs`, `NewDbAttribute.cs`, `PooledDbAttribute.cs`, `SharedDbAttribute.cs`, `SharedDbWithTransactionAttribute.cs`, `QueryableSettingsTask/*`), so the API surface is identical across frameworks — only the base-class namespace (`EfLocalDbMSTest`, etc.) and the Verify adapter differ. Note the MSTest namespace is `EfLocalDbMSTest` (no dot).
  - For a combined sample using LocalDb + Verify.EntityFramework + Verify.SqlServer: use `LocalDb` 26.2.0 / `EfLocalDb` 26.2.0, `Verify.EntityFramework` 15.4.1, `Verify.SqlServer` 12.2.0, `Verify` 33.1.1. Remember the double-recording caveat — call `VerifySqlServer.Initialize(recordCommands: false)` *before* `VerifierSettings.InitializePlugins()`, or use `Recording.IgnoreNames("sql")`.
  - `EfLocalDb` and the integration packages are **net10.0 only**; `LocalDb` and `EfClassicLocalDb` still support net48.
  - `readme.md` is MarkdownSnippets-generated from `readme.source.md`; deep docs live in `/pages/*.md` (generated from `/pages/mdsource/*.source.md`), not `/docs` (which only holds include partials and images).
