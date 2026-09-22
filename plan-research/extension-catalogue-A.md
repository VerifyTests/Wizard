## Verify.AngleSharp
- **NuGet package id(s)**: `Verify.AngleSharp` (single package)
- **Current version**: `5.1.2`
- **Target frameworks**: `net48;net6.0;net7.0;net8.0;net9.0`
- **One-line description**: Semantic comparison of html/htm/svg snapshots plus html pretty-printing and DOM scrubbing, via AngleSharp.
- **Tech tags**: html, htm, svg, anglesharp, dom, markup diffing, pretty print
- **Third-party dependencies**: `AngleSharp` 1.8.2, `AngleSharp.Css` 1.1.2, `AngleSharp.Diffing` 1.1.1, `Verify` 33.1.0 (`Polyfill` 11.4.0 and `ProjectDefaults` 1.0.181 are `PrivateAssets="all"`)
- **Initialize API**: `VerifyAngleSharpDiffing.Initialize(Action<IDiffingStrategyCollection>? action = null)` — optional action configures global AngleSharp.Diffing options. Registers string comparers for `html`, `htm` and `svg`.
- **Requires InitializePlugins-compatible pattern?**: yes (`public static bool Initialized => initialized == 1;` backed by `Interlocked.Exchange`, plus `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyAngleSharpDiffing.Initialize();
```
```cs
[Test]
public Task Sample()
{
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
}
```
```cs
[Test]
public Task PrettyPrintHtml()
{
    var html = """
               <!DOCTYPE html>
               <html><body><h1>My First Heading</h1>
               <p>My first paragraph.</p></body></html>
               """;
    return Verify(html, "html")
        .PrettyPrintHtml();
}
```
- **Verbose / edge-case APIs**:
  - `settings.AngleSharpDiffingSettings(Action<IDiffingStrategyCollection>)` — per-test diffing options; exists on both `VerifySettings` and `SettingsTask`.
    ```cs
    var settings = new VerifySettings();
    settings.AngleSharpDiffingSettings(
        action =>
        {
            static FilterDecision SpanFilter(
                in ComparisonSource source,
                FilterDecision decision)
            {
                if (source.Node.NodeName == "SPAN")
                {
                    return FilterDecision.Exclude;
                }

                return decision;
            }

            var options = action.AddDefaultOptions();
            options.AddFilter(SpanFilter);
        });
    ```
  - `VerifyAngleSharpDiffing.Initialize(action)` — same filter configuration applied globally.
  - `HtmlPrettyPrint.All(Action<INodeList>? action = null)` — pretty print every `html`/`htm` file globally.
  - `PrettyPrintHtml(this VerifySettings/SettingsTask, Action<INodeList>? action = null)` — per-test pretty print with optional node manipulation.
  - `nodes.ScrubEmptyDivs()` (on `INodeList` or `IEnumerable<IElement>`) — remove/unwrap empty or single-child attribute-less divs.
  - `element.TryScrubDiv()` — returns true when the div was removed or unwrapped.
  - `ScrubAttributes(string name)`, `ScrubAttributes(Func<IAttr, bool> match)` (removal), `ScrubAttributes(Func<IAttr, string?> tryGetValue)` (replace value) — on `INodeList` and `IEnumerable<IElement>`.
  - `nodes.ScrubAspCacheBusterTagHelper()` — replaces `asp-append-version` cache busters with `{TAG_HELPER_VERSION}`.
  - `nodes.ScrubBrowserLink()` — strips Visual Studio Browser Link comments/scripts and adjacent whitespace.
  - `VerifyTests.AngleSharp.AngleSharpExtensions`: `Descendants()`, `Descendants<TNode>()`, `DescendantsAndSelf()`, `DescendantsAndSelf<TNode>()` over `INodeList`/`IEnumerable<INode>`.
- **Interactions with other Verify extensions**: Readme has no explicit cross-package section. Its own test ModuleInitializer pairs it with DiffPlex: `VerifyDiffPlex.Initialize(OutputType.Compact); VerifierSettings.InitializePlugins();`. Consumed by Verify.Blazor and Verify.Bunit (both reference `Verify.AngleSharp` 5.1.2 in their test projects and call `HtmlPrettyPrint.All();` in their scrubber snippets).
- **MSBuild / project requirements**: None special. `LangVersion preview`.
- **Sample verified output**:
```html
<!DOCTYPE html>
<html>
  <head></head>
  <body>
    <h1>My First Heading</h1>
    <p>My first paragraph.</p>
  </body>
</html>
```
  Comparer failure output:
```
Comparer result:
 * Node Diff
   Path: h1(0) > #text(0)
   Received: First Heading
   Verified: My First Heading
```
- **Notes for the wizard**: This is a *comparer* + *scrubber* extension, not a converter — it never produces new targets. Pretty-print and diffing are independent features; a user can use one without the other. Tests are NUnit on net9.0. Has a `src/Benchmarks` project.

---

## Verify.AspNetCore
- **NuGet package id(s)**: `Verify.AspNetCore` (single package)
- **Current version**: `5.0.0`
- **Target frameworks**: `net10.0` (single)
- **One-line description**: Serializes ASP.NET Core types (HttpContext/Request/Response, every `ActionResult` flavour, headers, cookies) into readable snapshots.
- **Tech tags**: asp.net core, mvc, controller, middleware, http, actionresult, integration testing
- **Third-party dependencies**: `Verify` 33.1.0 only. ASP.NET Core types come from the shared framework (project uses `Microsoft.NET.Sdk.Web`).
- **Initialize API**: `VerifyAspNetCore.Initialize()` — no parameters.
- **Requires InitializePlugins-compatible pattern?**: yes (`public static bool Initialized { get; private set; }` + `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Initialize() =>
    VerifyAspNetCore.Initialize();
```
```cs
[Test]
public Task Test()
{
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
}
```
- **Verbose / edge-case APIs**:
  - `UseSpecificControllers(this IMvcBuilder builder, params Type[] controllers)` and `UseSpecificControllers(this IMvcCoreBuilder builder, params Type[] controllers)` — restrict an integration-test web app to only the listed controllers.
    ```cs
    var controllers = builder.Services.AddControllers();
    // custom extension
    controllers.UseSpecificControllers(typeof(FooController));
    ```
  - `ScrubAspTextResponse(this VerifySettings/SettingsTask, Func<string, string> scrub)` — rewrite the text body of an `HttpResponse` before verification.
    ```cs
    return Verify(response)
        .ScrubAspTextResponse(_ => _.Replace("value", "replace"));
    ```
  - File-result handling (no public API, automatic): registered `RegisterFileConverter` for `FileStreamResult`, `FileContentResult`, `PhysicalFileResult`, `VirtualFileResult`; content type is mapped to a file extension and emitted as text or binary target.
  - Middleware verification: verify `context.Response` directly.
- **Interactions with other Verify extensions**: Nothing stated in the readme. Test ModuleInitializer order is `VerifyAspNetCore.Initialize();` in one initializer and `VerifyDiffPlex.Initialize(); VerifierSettings.InitializePlugins();` in another.
- **MSBuild / project requirements**: The shipped project is `Sdk="Microsoft.NET.Sdk.Web"` with `<OutputType>Library</OutputType>` and `<IsPackable>true</IsPackable>`. Consumers need the ASP.NET Core shared framework (Web SDK or `FrameworkReference Microsoft.AspNetCore.App`). net10.0 only. Test project pulls `Microsoft.AspNetCore.Mvc.Testing` 10.0.12 and `Microsoft.AspNetCore.TestHost` 10.0.12 (test-only).
- **Sample verified output**:
```txt
{
  result: [
    {
      Value: Value1
    },
    {
      Value: Value2
    }
  ],
  context: {
    HttpContext: {
      Request: {},
      Response: {
        StatusCode: OK,
        Headers: {
          headerKey: headerValue,
          receivedInput: inputValue
        },
        Cookies: {
          cookieKey: cookieValue
        }
      }
    }
  }
}
```
- **Notes for the wizard**: ~50 dedicated result converters are registered in one `Initialize()` — no opt-out. `Verify.AspNetCore` is `partial` across two files (`VerifyAspNetCore.cs` and `SelectedControlers/VerifyAspNetCore.cs`). Tests are NUnit; there are two sample web apps in the repo (`SampleWebApi`, `SampleWebApplication`).

---

## Verify.Aspose
- **NuGet package id(s)**: `Verify.Aspose` (single package)
- **Current version**: `5.28.0`
- **Target frameworks**: `net8.0;net9.0;net10.0`
- **One-line description**: Converts pdf, docx, xlsx and pptx documents to png pages plus metadata/text snapshots via Aspose.
- **Tech tags**: aspose, pdf, word, docx, excel, xlsx, powerpoint, pptx, document rendering, png
- **Third-party dependencies**: `Aspose.PDF` 26.9.0 (`Pinned="true"`), `Aspose.Cells` 26.9.0, `Aspose.Words` 26.9.0, `Aspose.Slides.NET` 26.9.0, `DeterministicIoPackaging` 0.31.0, `DeterministicPdf` 2.0.2, `Microsoft.Bcl.Memory` 10.0.12, `Verify` 33.1.0. (`Aspose.Email` 26.7.0 is listed in Directory.Packages.props but is **not** referenced by the shipped csproj — the license code for it is commented out.)
- **Initialize API**: `VerifyAspose.Initialize()` — no parameters.
- **Requires InitializePlugins-compatible pattern?**: yes (`public static bool Initialized { get; private set; }` + `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Initialize() =>
    VerifyAspose.Initialize();
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
  - Stream converters registered for `xlsx`, `xls`, `pdf`, `pptx`, `ppt`, `docx`, `doc`.
  - File converters registered for `Aspose.Cells.Workbook`, `Aspose.Cells.Worksheet`, `Aspose.Pdf.Document`, `Aspose.Slides.Presentation`, `Aspose.Words.Document` — so `Verify(book)` works directly.
    ```cs
    var book = new Workbook { BuiltInDocumentProperties = { Comments = "the comments" } };
    book.CustomDocumentProperties.Add("key", "value");
    var sheet = book.Worksheets.Add("New Sheet");
    var cells = sheet.Cells;
    cells[0, 0].PutValue("Some Text");
    return Verify(book);
    ```
  - `VerifyTestsAspose.VerifyAsposeSettings` (note the distinct namespace) extension methods on `VerifySettings` and `SettingsTask`:
    - `IncludeWordStyles()` — include Word style info in the snapshot.
    - `PagesToInclude(int count)` — cap rendered page/slide png snapshots to the first `count`; binary targets are unaffected.
    - `PdfPngDevice(Func<Aspose.Pdf.Page, PngDevice> func)` — control the pdf→png rendering device per page.
    - `SkipPdfNormalization()` — snapshot pdf bytes verbatim, skipping `/ID`, `/CreationDate`, `/ModDate` and XMP normalization. Docs warn enabling/disabling it shifts existing `.verified.pdf` once.
  - `ExcludeTargets("xlsx")` / `ExcludeTargets("docx")` (Verify core) to drop the expensive deterministic package build:
    ```cs
    [Test]
    public Task ExcludeXlsx() =>
        // ExcludeTargets skips the expensive deterministic xlsx build.
        VerifyFile("sample.xlsx")
            .ExcludeTargets("xlsx");
    ```
    Readme: "To exclude for every test, call `VerifierSettings.ExcludeTargets("xlsx")` at initialization."
  - Cross-framework binary drift: `await Verify(stream, extension: "xlsx").UniqueForRuntime();`
  - Auto-registered: `VerifierSettings.IgnoreMember<IDocumentProperties>(_ => _.AppVersion)` and an html scrubber that strips the `<meta name="generator" content="Aspose...">` tag.
- **Interactions with other Verify extensions**: Nothing explicit in the readme. Overlaps conceptually with **Verify.DocNet** and **Verify.ClosedXml** (both also convert pdf/xlsx) — registering both would fight over the same stream-converter extensions. Test ModuleInitializer uses `VerifierSettings.UseSsimForPng();`.
- **MSBuild / project requirements**: **An [Aspose License](https://purchase.aspose.com/policies/license-types) is required to use this tool.** The repo's tests read an `AsposeLicense` environment variable and throw if missing, applying it via `Aspose.Pdf.License`, `Aspose.Cells.License`, `Aspose.Words.License`, `Aspose.Slides.License`. `NoWarn` includes `CA1416` (platform-specific API warnings). Test assembly sets `[assembly: Culture("en-AU")]`.
- **Sample verified output**:
```txt
{
  Pages: 2,
  AllowReusePageContent: false,
  CenterWindow: false,
  DisplayDocTitle: false,
  FitWindow: False,
  IgnoreCorruptedObjects: True,
  Info: {
    Creator: RAD PDF,
    Producer: RAD PDF 3.9.0.0 - http://www.radpdf.com
  },
  IsEncrypted: False,
  PdfFormat: v_1_4,
  Version: 1.4,
  Fonts: [
    Helvetica
  ],
  Text:
...
}
```
  Plus one `.verified.png` per page/slice (e.g. `Samples.VerifyPdf#00.verified.png`, `Samples.VerifyExcel#Sheet1.verified.png`).
- **Notes for the wizard**: Commercial license key required (blocker for a getting-started wizard — surface this prominently). Emits multiple targets per verification (metadata txt + png per page + binary docx/xlsx), which can be expensive; suggest `ExcludeTargets` and `PagesToInclude`. Settings extensions live in namespace `VerifyTestsAspose`, **not** `VerifyTests` — a `using VerifyTestsAspose;` is needed. `VerifyAspose` is `partial` split across `_Excel`, `_Pdf`, `_PowerPoint`, `_Word` files. Repo has a `nugets/` folder containing `Verify.Aspose.5.7.0.nupkg`.

---

## Verify.Assertions
- **NuGet package id(s)**: `Verify.Assertions` (single package)
- **Current version**: `0.3.0`
- **Target frameworks**: `net48;net7.0;net8.0`
- **One-line description**: Runs assertion-library callbacks against objects as they are serialized, so large/complex graphs can be interrogated mid-verification.
- **Tech tags**: assertions, xunit, nunit, fluentassertions, shouldly, serialization callback
- **Third-party dependencies**: `Verify` 33.1.0 only (`Polyfill` 11.4.0 and `ProjectDefaults` are `PrivateAssets="all"`). FluentAssertions 8.11.0 / Shouldly 4.3.0 / NUnit / xunit.v3 are **test-only**.
- **Initialize API**: `VerifyAssertions.Initialize()` — no parameters. Hooks `VerifierSettings.AddExtraSettings(_ => _.Serializing += Serializing)`.
- **Requires InitializePlugins-compatible pattern?**: yes (`public static bool Initialized { get; private set; }` + `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyAssertions.Initialize();
```
```cs
[Fact]
public async Task XunitUsage()
{
    var nested = new Nested(Property: "value");
    var target = new Target(nested);
    await Verify(target)
        .Assert<Nested>(
            _ => Assert.Equal("value", _.Property));
}
```
- **Verbose / edge-case APIs**:
  - `VerifyAssertions.Assert<T>(Action<T> assert)` — static/global shared assertion applied to every verification.
    ```cs
    [ModuleInitializer]
    public static void AddSharedAssert() =>
        VerifyAssertions
            .Assert<SharedNested>(
                _ => Assert.Equal("value", _.Property));
    ```
  - `settings.Assert<T>(Action<T>)` on `VerifySettings` — accumulates into a per-test list.
  - `[Pure] SettingsTask Assert<T>(this SettingsTask, Action<T>)` — fluent form.
  - Assertion library is irrelevant to the package — readme shows NUnit (`Assert.That(_.Property, Is.EqualTo("value"))`), FluentAssertions (`_.Property.Should().Be("value")`) and Shouldly (`_.Property.ShouldBe("value")`) with the identical `.Assert<T>(...)` shape.
- **Interactions with other Verify extensions**: Nothing in the readme. Test ModuleInitializer: `VerifyAssertions.Initialize();` then a separate `VerifierSettings.InitializePlugins();`.
- **MSBuild / project requirements**: None. Note `NoWarn` includes `NU5105`; no `TreatWarningsAsErrors` in this repo's Directory.Build.props.
- **Sample verified output**: not found (readme shows no verified file; the point is the assertion, not the snapshot shape).
- **Notes for the wizard**: Pre-1.0 (`0.3.0`). The `Wrap<T>` helper silently no-ops when the serialized object is not of type `T` — assertions do not fail on "type never encountered", so a typo'd `T` passes silently. Has both static (global) and instance/fluent APIs. Test projects: xunit.v3 (`Tests`) and NUnit (`NUnitTests`). Repo has a `waitforport.ps1` but no DB dependency in the shipped package.

---

## Verify.Avalonia
- **NuGet package id(s)**: `Verify.Avalonia` (single package)
- **Current version**: `1.4.1`
- **Target frameworks**: `net10.0` (single)
- **One-line description**: Renders Avalonia `Window`/`TopLevel`/`UserControl` to png plus a serialized visual-tree txt snapshot, using Avalonia headless testing.
- **Tech tags**: avalonia, avaloniaui, xaml, desktop ui, headless rendering, screenshot, visual tree
- **Third-party dependencies**: `Avalonia.Controls.ColorPicker` 12.1.2, `Avalonia.Controls.DataGrid` 12.1.2, `Avalonia.Headless` 12.1.2, `EmptyFiles` 8.18.2, `Verify` 33.1.0
- **Initialize API**: `VerifyAvalonia.Initialize()` — no parameters. Separately: `VerifyAvalonia.IncludeThemeVariant()` (does **not** initialize; it only sets a flag, and readme's snippet pairs it with `VerifierSettings.InitializePlugins()`).
- **Requires InitializePlugins-compatible pattern?**: yes (`public static bool Initialized { get; private set; }` + `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Init()
{
    VerifierSettings.UseSsimForPng();
    VerifyAvalonia.Initialize();
}
```
```cs
[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseSkia()
            .UseHeadless(
                new()
                {
                    UseHeadlessDrawing = false
                });
}
```
```cs
[AvaloniaFact]
public Task Render()
{
    var control = new MyUserControl();
    return Verify(control);
}
```
- **Verbose / edge-case APIs**:
  - `VerifyAvalonia.IncludeThemeVariant()` — renders and snapshots both `ThemeVariant.Light` and `ThemeVariant.Dark`, producing `#light` and `#dark` pngs.
    ```cs
    [ModuleInitializer]
    public static void Init()
    {
        VerifyAvalonia.IncludeThemeVariant();
        VerifierSettings.InitializePlugins();
    }
    ```
  - `VerifyAvalonia.AddAvaloniaConvertersForAssemblyOfType<T>()` — register converters for every `AvaloniaObject` in the assembly containing `T` (for third-party control libraries).
    ```cs
    [ModuleInitializer]
    public static void Init()
    {
        VerifyAvalonia.AddAvaloniaConvertersForAssemblyOfType<SomeControl>();
        VerifierSettings.InitializePlugins();
    }
    ```
  - `VerifyAvalonia.AddAvaloniaConvertersForAssembly(Assembly assembly)` — same, by `Assembly`.
  - Built-in converters registered for `Thickness`, `CornerRadius`, `FontFamily`, plus auto-scan of `Avalonia.Controls`, `Avalonia.Base`, `Avalonia.Controls.ColorPicker`, `Avalonia.Controls.DataGrid`.
  - Standalone (no app project) setup: readme shows `VerifyAvaloniaSetupApplication : Application` combining `[assembly: AvaloniaTestApplication]`, `BuildAvaloniaApp()` and `Styles.Add(new FluentTheme())`.
- **Interactions with other Verify extensions**: Readme, "Verify.CommunityToolkit.Mvvm" section — "Many Avalonia projects use [CommunityToolkit.Mvvm]... To ensure proper serialization of MVVM commands, use [Verify.CommunityToolkit.Mvvm](https://github.com/VerifyTests/Verify.CommunityToolkit.Mvvm)." Both NUnit and XUnit test projects reference `Verify.CommunityToolkit.Mvvm` 1.1.0. Readme also leans on core `VerifierSettings.UseSsimForPng()` "to ignore small rendering differences that are expected between different operating systems."
- **MSBuild / project requirements**: Required NuGet packages listed in readme: `Verify.Avalonia`, `Avalonia.Headless.XUnit` (or `.NUnit`), `Avalonia.Themes.Fluent`, `Avalonia.Skia`. **`UseHeadlessDrawing` must be disabled and `.UseSkia()` set** — the readme states this explicitly. Test projects need `<OutputType>Exe</OutputType>`; NUnit one sets `<EnableNUnitRunner>true</EnableNUnitRunner>`; both set `<SignAssembly>false</SignAssembly>`. App project needs `InternalsVisibleTo` for named-control access:
```csproj
<ItemGroup>
  <PackageReference Include="Avalonia.Controls.ColorPicker" />
  <InternalsVisibleTo Include="NUnitTests" />
  <InternalsVisibleTo Include="XUnitTests" />
</ItemGroup>
```
  `[AvaloniaTestApplication]` must be declared once per test project.
- **Sample verified output**:
```txt
{
  Type: MyUserControl,
  Content: {
    Type: StackPanel,
    Spacing: 10.0,
    Orientation: Vertical,
    Margin: 10,
    HorizontalAlignment: Left,
    Children: [
      { Type: TextBlock, Text: Welcome to Avalonia! },
      { Type: Button, Content: Button }
    ]
  },
  Background: LightGray,
  Width: 200.0,
  Height: 100.0
}
```
  Plus `CalculatorTests.Should_Add_Numbers.verified.png` (and `#light`/`#dark` variants with `IncludeThemeVariant`).
- **Notes for the wizard**: Initialize also installs a workaround: `TypeDescriptor.AddAttributes(typeof(ICommand), new TypeConverterAttribute(typeof(TypeConverter)))` because .NET 10's `CommandConverter` throws. Window cleanup is marshalled to `Dispatcher.UIThread`. The `MarkdownSnippets.MsBuild` reference is in the *shipped* csproj (unusual, `PrivateAssets="all"`). This repo's Directory.Packages.props lacks `CentralPackageTransitivePinningEnabled`. There is a stray `nul` file at repo root.

---

## Verify.Blazor
- **NuGet package id(s)**: `Verify.Blazor` (single package). Note: `src/Verify.Bunit` and `src/Verify.Bunit.ExcludeComponentTests` folders exist but contain only stale `bin`/`obj` — no csproj, not shipped from this repo (Verify.Bunit lives in its own repo).
- **Current version**: `11.0.0`
- **Target frameworks**: `net10.0` (single)
- **One-line description**: Renders a Blazor component to html + a component-state txt snapshot using the raw Blazor rendering APIs (no bUnit).
- **Tech tags**: blazor, razor, components, html rendering, aspnetcore components
- **Third-party dependencies**: `Verify` 33.1.0, `Microsoft.AspNetCore.Components` 10.0.12, `Microsoft.Extensions.DependencyInjection` 10.0.12
- **Initialize API**: **No public Initialize.** `VerifyBlazor` is `static class VerifyBlazor` (internal, no accessibility modifier) with `Initialized` + `Initialize()`, called automatically from the static constructor of `VerifyTests.Blazor.Render`. There is nothing to call in a ModuleInitializer.
- **Requires InitializePlugins-compatible pattern?**: no — the `Initialized`/`Initialize()` pair exists but the class is **not public**, so it cannot participate in `VerifierSettings.InitializePlugins()` or be called by a user.
- **Minimal usage**:
```cs
[Fact]
public Task PassingParameters()
{
    var parameters = ParameterView.FromDictionary(
        new Dictionary<string, object?>
        {
            {
                "Title", "The Title"
            },
            {
                "Person", new Person
                {
                    Name = "Sam"
                }
            }
        });

    var target = Render.Component<TestComponent>(parameters: parameters);

    return Verify(target);
}
```
```cs
[Fact]
public Task PassingTemplateInstance()
{
    var template = new TestComponent
    {
        Title = "The Title",
        Person = new()
        {
            Name = "Sam"
        }
    };

    var target = Render.Component(template: template);

    return Verify(target);
}
```
- **Verbose / edge-case APIs**:
  - `Render.Component<T>(ServiceProvider? provider = null, ILoggerFactory? loggerFactory = null, ParameterView? parameters = null, T? template = null, Action<T>? callback = null) where T : ComponentBase` — the only entry point. `template` properties are merged over `parameters`; `callback` runs then triggers `StateHasChanged`.
  - `VerifyTests.Blazor.BlazorScrubber.ScrubCommentLines()` — removes Blazor comment markers from html output.
  - Recommended scrubbing block from the readme (uses core Verify APIs plus Verify.AngleSharp):
    ```cs
    // remove some noise from the html snapshot
    VerifierSettings.ScrubEmptyLines();
    BlazorScrubber.ScrubCommentLines();
    VerifierSettings.ScrubLinesWithReplace(
        line =>
        {
            var scrubbed = line.Replace("<!--!-->", "");
            if (string.IsNullOrWhiteSpace(scrubbed))
            {
                return null;
            }

            return scrubbed;
        });
    HtmlPrettyPrint.All();
    VerifierSettings.ScrubLinesContaining("<script src=\"_framework/dotnet.");
    ```
- **Interactions with other Verify extensions**: Readme intro: "Verify.Blazor uses the Blazor APIs to take a snapshot (metadata and html) of the current state of a Blazor component. It has fewer dependencies and is a simpler API than [Verify.Bunit approach](https://github.com/VerifyTests/Verify.Bunit), however it does not provide many of the other features, for example [trigger event handlers]". `HtmlPrettyPrint.All()` in the recommended scrubbers comes from **Verify.AngleSharp** (test project references `Verify.AngleSharp` 5.1.2). Effectively a mutually-exclusive alternative to Verify.Bunit.
- **MSBuild / project requirements**: Ships `build.targets` as both `build\Verify.Blazor.targets` and `buildMultiTargeting\Verify.Blazor.targets`, whose entire content is `<NoWarn>$(NoWarn);BL0005</NoWarn>` — consumers get BL0005 suppressed automatically. Repo-wide `NoWarn` also includes `BL0006`. Sets `<AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>`.
- **Sample verified output**:
```html
<div>
  <h1>The Title</h1>
  <p>Sam</p>
  <button>MyButton</button>
</div>
```
```txt
{
  Instance: {
    Intitialized: true,
    Title: The Title,
    Person: {
      Name: Sam
    }
  }
}
```
- **Notes for the wizard**: **No Initialize call needed or possible** — this is the odd one out among these repos; a wizard must not emit `VerifyBlazor.Initialize()`. `readme.md` is generated from `readme.source.md` by MarkdownSnippets ("GENERATED FILE - DO NOT EDIT"). Tests use xunit.v3 on net10.0. Two sample apps: `BlazorApp` (wasm) and `BlazorServerApp`.

---

## Verify.Brighter
- **NuGet package id(s)**: `Verify.Brighter` (single package)
- **Current version**: `2.0.0`
- **Target frameworks**: `net8.0` (single)
- **One-line description**: Supplies a recording `IAmACommandProcessor` so Brighter handler Send/Publish/Post/Deposit/Clear/Call calls can be captured and verified.
- **Tech tags**: brighter, paramore, command processor, cqrs, messaging, mediator
- **Third-party dependencies**: `Paramore.Brighter` 10.7.0, `Verify` 33.1.0
- **Initialize API**: `VerifyBrighter.Initialize()` — no parameters.
- **Requires InitializePlugins-compatible pattern?**: yes (`public static bool Initialized { get; private set; }` + `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyBrighter.Initialize();
```
```cs
[Fact]
public async Task HandlerTest()
{
    var context = new RecordingCommandProcessor();
    var handler = new Handler(context);
    await handler.HandleAsync(new Message("value"));
    await Verify(context);
}
```
- **Verbose / edge-case APIs**: The readme documents only the above. From source, `VerifyTests.Brighter.RecordingCommandProcessor : IAmACommandProcessor` also exposes query properties for inspecting/filtering records without verifying the whole processor:
  - `IEnumerable<SendRecord> Sends`
  - `IEnumerable<PublishRecord> Publishes`
  - `IEnumerable<PostRecord> Posts`
  - `IEnumerable<DepositPostRecord> Deposits`
  - `IEnumerable<CallRecord> Calls`
  - It implements the full `IAmACommandProcessor` surface: `Send`/`SendAsync` (incl. `DateTimeOffset at` and `TimeSpan delay` overloads), `Publish`/`PublishAsync` (same overloads), `Post`/`PostAsync` (same overloads), `DepositPost`/`DepositPostAsync` (single, batch, and `IAmABoxTransactionProvider<TTransaction>` overloads), `ClearOutbox`/`ClearOutboxAsync`, `Call<T, TResponse>` (always returns `null`).
  - Record types with converters: `SendRecord`, `PublishRecord`, `PostRecord`, `DepositPostRecord`, `ClearOutboxRecord`, `CallRecord`.
- **Interactions with other Verify extensions**: Nothing in the readme. Test ModuleInitializer separates `VerifyBrighter.Initialize();` from `VerifierSettings.InitializePlugins();`.
- **MSBuild / project requirements**: None. net8.0 only.
- **Sample verified output**:
```txt
{
  Send: SendRecord: {
    Request: {
      Property: Some data,
      Id: {
        Value: Guid_1
      }
    },
    ContinueOnCapturedContext: true
  },
  Publish: PublishRecord: {
    Request: {
      Property: Some other data,
      Id: {
        Value: Guid_2
      }
    },
    ContinueOnCapturedContext: true
  }
}
```
- **Notes for the wizard**: `VerifyBrighter.Initialize()` does **not** call `InnerVerifier.ThrowIfVerifyHasBeenRun()` (unlike most siblings) — it only adds converters. This is an instance-based ("test double") extension: the user constructs `RecordingCommandProcessor` and injects it; there is no ambient recording. Note `Post` overloads enqueue under `CommandType.Publish` (so posts serialize under a `Publish:` key — looks like a source quirk worth not documenting as intended behaviour). Tests are xunit.v3 on net8.0. Repo has no `code_of_conduct.md`.

---

## Verify.Bunit
- **NuGet package id(s)**: `Verify.Bunit` (single package)
- **Current version**: `14.1.0-beta.1` (pre-release)
- **Target frameworks**: `net10.0` (single)
- **One-line description**: Snapshots bUnit-rendered Blazor components (html + component state) with bUnit's full interaction feature set available.
- **Tech tags**: bunit, blazor, razor, components, html rendering, component testing
- **Third-party dependencies**: `bunit` 2.11.3, `Verify` 33.1.0
- **Initialize API**: `VerifyBunit.Initialize(bool excludeComponent = false)` — `excludeComponent: true` suppresses the component-state `.verified.txt` and emits only the markup.
- **Requires InitializePlugins-compatible pattern?**: yes (`public static bool Initialized { get; private set; }` + `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Initialize() =>
    VerifyBunit.Initialize();
```
```cs
[Fact]
public Task Component()
{
    using var context = new BunitContext();
    var component = context.Render<TestComponent>(
        builder =>
        {
            builder.Add(
                _ => _.Title,
                "New Title");
            builder.Add(
                _ => _.Person,
                new()
                {
                    Name = "Sam"
                });
        });
    return Verify(component);
}
```
- **Verbose / edge-case APIs**:
  - `VerifyBunit.Initialize(excludeComponent: true)` — markup-only snapshots.
    ```cs
    [ModuleInitializer]
    public static void Initialize() =>
        VerifyBunit.Initialize(excludeComponent: true);
    ```
  - Verify a node list or a single node instead of the whole component: `Verify(component.Nodes)` and `Verify(component.Nodes.First().FirstChild)` (registered `IMarkupFormattable` file converter).
  - `component.WaitFor(Func<bool> predicate, TimeSpan? timeout = null)` — extension on `IRenderedComponent<TComponent>`; waits (default one second per doc comment) for a predicate, re-evaluated on each render; throws `WaitForFailedException`.
  - `context.RenderComponentAndWait<TComponent>(Action<ComponentParameterCollectionBuilder<TComponent>> parameterBuilder, Func<TComponent, bool> renderedCheck, TimeSpan? timeout = null)` — renders then polls until `renderedCheck` passes (default 10 seconds, 10ms poll, throws `TimeoutException`).
  - `context.RenderAndWait<TComponent>(RenderFragment fragment, Func<TComponent, bool> renderedCheck, TimeSpan? timeout = null)` — same for a render fragment.
  - `VerifyTests.Bunit.BlazorScrubber.ScrubCommentLines()` — removes Blazor comment markers.
  - Registers a `RegisterStringComparer("html", BunitMarkupComparer.Compare)` — bUnit's own semantic markup comparison for `html` files.
  - Recommended scrubbing block (same as Verify.Blazor, uses `HtmlPrettyPrint.All()` from Verify.AngleSharp).
- **Interactions with other Verify extensions**: Readme intro contrasts with Verify.Blazor: "Since it leverages the bUnit API, snapshots can be on a component that has been manipulated using the full bUnit feature set, for example [trigger event handlers]". Test project references **Verify.AngleSharp** 5.1.2 and calls `HtmlPrettyPrint.All();`. **Conflict risk worth flagging:** `VerifyBunit.Initialize()` registers a string comparer for `html` (`BunitMarkupComparer.Compare`) and `VerifyAngleSharpDiffing.Initialize()` also registers one for `html` — last-in wins. Mutually exclusive in practice with Verify.Blazor.
- **MSBuild / project requirements**: Ships `build.targets` (as `build\Verify.Bunit.targets` and `buildMultiTargeting\Verify.Bunit.targets`) setting `<NoWarn>$(NoWarn);BL0005</NoWarn>`. The shipped csproj declares `InternalsVisibleTo Include="Tests"` with an explicit public key. Test project sets `<GenerateAssemblyInfo>true</GenerateAssemblyInfo>` and imports `..\build.targets`.
- **Sample verified output**:
```html
<div>
  <h1>New Title</h1>
  <p>Sam</p>
  <button>MyButton</button>
</div
```
```txt
{
  Instance: {
    Intitialized: true,
    Title: New Title,
    Person: {
      Name: Sam
    }
  },
  NodeCount: 9
}
```
- **Notes for the wizard**: Currently a **beta** version (`14.1.0-beta.1`). Requires bUnit v2 API shape (`BunitContext`, `context.Render<T>(...)`, `IRenderedComponent<T>`) — not bUnit v1's `TestContext`/`RenderComponent`. `readme.md` is generated from `readme.source.md`. Tests are xunit.v3 on net10.0.

---

## Verify.ClosedXml
- **NuGet package id(s)**: `Verify.ClosedXml` (single package)
- **Current version**: `1.4.0`
- **Target frameworks**: `net472;net48;net8.0;net9.0;net10.0`
- **One-line description**: Converts Excel workbooks (xlsx) into a metadata txt, one csv per sheet, and a deterministic xlsx, via ClosedXML.
- **Tech tags**: closedxml, excel, xlsx, spreadsheet, openxml, csv
- **Third-party dependencies**: `ClosedXML` 0.105.1, `DeterministicIoPackaging` 0.31.0, `DocumentFormat.OpenXml` 3.5.1, `Verify` 33.1.0 (`Polyfill` 11.4.0 and `ProjectDefaults` are `PrivateAssets="all"`)
- **Initialize API**: `VerifyClosedXml.Initialize()` — no parameters.
- **Requires InitializePlugins-compatible pattern?**: yes (`public static bool Initialized { get; private set; }` + `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Initialize() =>
    VerifyClosedXml.Initialize();
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
  - `Verify(book)` for a `ClosedXML.Excel.XLWorkbook` instance (registered file converter):
    ```cs
    [Test]
    public Task XLWorkbook()
    {
        using var book = new XLWorkbook();

        var sheet = book.Worksheets.Add("Basic Data");

        sheet.Cell("A1").Value = "ID";
        sheet.Cell("B1").Value = "Name";

        sheet.Cell("A2").Value = 1;
        sheet.Cell("B2").Value = "John Doe";

        sheet.Cell("A3").Value = 2;
        sheet.Cell("B3").Value = "Jane Smith";

        return Verify(book);
    }
    ```
  - Cross-framework binary drift — readme: "the binary output may differ due to Deflate compression implementation differences... Use `UniqueForRuntime`":
    ```cs
    await Verify(stream, extension: "xlsx")
        .UniqueForRuntime();
    ```
  - No package-specific `VerifySettings` extension methods exist. Converters registered for `XLColor`, `IXLFont`, `IXLStyle`, `IXLFill`, `IXLBorder`, `IXLProtection`, `IXLNumberFormat`, `IXLAlignment`, workbook properties — all default-value suppressed via `Defaults.IsDefault(...)`.
  - Formulas are appended to the csv cell as ` (A1-style)` or ` (R1C1-style)`; percentages formatted via `"P"`; Guid/date values run through Verify's `Counter` scrubbing.
- **Interactions with other Verify extensions**: Nothing in the readme. Overlaps with **Verify.Aspose**, which also registers a stream converter for `xlsx`/`xls` — registering both would conflict on the same extension.
- **MSBuild / project requirements**: Directory.Build.props adds `<Using Include="System.ReadOnlySpan&lt;System.Char&gt;" Alias="CharSpan" />` and `NoWarn` includes `CA1416`. Test project multi-targets `net48;net10.0`, references `Microsoft.Office.Interop.Excel` 16.0.18925.20022 and `System.Windows.Forms` on net48 (test-only, Windows-only). The test ModuleInitializer calls `VerifierSettings.UniqueForTargetFrameworkAndVersion();` — note verified filenames in the readme carry `.DotNet9_0`.
- **Sample verified output**:
```txt
{
  SheetNames: [
    Sheet1
  ],
  Properties: {
    Title: The Title
  },
  WorksheetCount: 1,
  DefaultFont: Arial,
  CalculateMode: Default,
  Style: {
    Font: {
      Name: Arial
    }
  }
}
```
```csv
0,First Name,Last Name,Gender,Country,Date,Age,Id,Formula
1,Dulce,Abril,Female,United States,DateTime_1,32,1562,1594 (G2+H2)
2,Mara,Hashimoto,Female,Great Britain,DateTime_2,25,1582,1607 (G3+H3)
```
- **Notes for the wizard**: Produces **3+ files per verification** (txt + xlsx + one csv per sheet) — readme says "For a given Verify, the result is 3 (or more files)". `Initialize()` does **not** call `InnerVerifier.ThrowIfVerifyHasBeenRun()`. Widest TFM span here (net472 → net10.0). Tests are NUnit with `NUnit.Framework.Legacy.ClassicAssert` static using.

---

## Verify.CommunityToolkit.Mvvm
- **NuGet package id(s)**: `Verify.CommunityToolkit.Mvvm` (single package)
- **Current version**: `1.1.0`
- **Target frameworks**: `net8.0` (single)
- **One-line description**: Serializes CommunityToolkit.Mvvm `RelayCommand`/`AsyncRelayCommand` as the names of their backing Execute/CanExecute methods.
- **Tech tags**: communitytoolkit.mvvm, mvvm, relaycommand, icommand, viewmodel
- **Third-party dependencies**: `CommunityToolkit.Mvvm` 8.4.2, `Verify` 33.1.0 (`Polyfill` 11.4.0 and `ProjectDefaults` are `PrivateAssets="all"`)
- **Initialize API**: `VerifyCommunityToolkitMvvm.Initialize()` — no parameters.
- **Requires InitializePlugins-compatible pattern?**: yes (`public static bool Initialized { get; private set; }` + `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Initialize() =>
    VerifyCommunityToolkitMvvm.Initialize();
```
```cs
[Fact]
public Task RelayCommand()
{
    var content = new RelayCommand(ActionMethod, CanExecuteMethod);
    return Verify(content);
}
```
```cs
[Fact]
public Task AsyncRelayCommand()
{
    var content = new AsyncRelayCommand(ActionMethodAsync, CanExecuteMethod);
    return Verify(content);
}
```
- **Verbose / edge-case APIs**: None beyond `Initialize()`. Four converters registered: `RelayCommandConverter`, `RelayCommandInterfaceConverter`, `AsyncRelayCommandConverter`, `AsyncRelayCommandInterfaceConverter` — the "Interface" variants handle commands typed as `IRelayCommand`/`IAsyncRelayCommand`. Method names are resolved by reflecting the private `execute`/`canExecute` delegate fields and rendered as `Type.Method`.
- **Interactions with other Verify extensions**: This package's own readme says nothing, but **Verify.Avalonia's readme recommends it**: "Many Avalonia projects use [CommunityToolkit.Mvvm]... To ensure proper serialization of MVVM commands, use [Verify.CommunityToolkit.Mvvm]". Verify.Avalonia's NUnit and XUnit test projects both reference it.
- **MSBuild / project requirements**: None. net8.0 only. Note this repo's Directory.Build.props has **no** `<LangVersion>preview</LangVersion>` (unlike the siblings).
- **Sample verified output**:
```txt
{
  Execute: Tests.ActionMethod,
  CanExecute: Tests.CanExecuteMethod
}
```
- **Notes for the wizard**: Smallest surface in this batch — a single `Initialize()` and nothing else. Most useful as a companion to a UI extension (Avalonia/WPF/MAUI) rather than standalone. Tests are xunit.v3 on net8.0.

---

## Verify.Cosmos
- **NuGet package id(s)**: `Verify.Cosmos` (single package)
- **Current version**: `3.0.0`
- **Target frameworks**: `net8.0` (single)
- **One-line description**: Snapshots Azure Cosmos DB `ItemResponse`/`FeedResponse` results with non-deterministic diagnostics, ETags and policy metadata stripped.
- **Tech tags**: cosmos db, azure, nosql, document database, itemresponse, feedresponse
- **Third-party dependencies**: `Microsoft.Azure.Cosmos` 3.63.1, `Newtonsoft.Json` 13.0.4, `Verify` 33.1.0
- **Initialize API**: `VerifyCosmos.Initialize()` — no parameters.
- **Requires InitializePlugins-compatible pattern?**: yes (`public static bool Initialized { get; private set; }` + `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyCosmos.Initialize();
```
```cs
var response = await container.CreateItemAsync(
    item,
    new PartitionKey(item.LastName));
await Verify(response);
```
```cs
using var iterator = container.GetItemLinqQueryable<Family>()
    .Where(b => b.Id == item.Id)
    .ToFeedIterator();
var feedResponse = await iterator.ReadNextAsync();
await Verify(feedResponse);
```
- **Verbose / edge-case APIs**: None documented beyond `Initialize()`. From source, `Initialize()` applies these automatic exclusions (no opt-out): `VerifierSettings.IgnoreMembers("ETag")`, `IgnoreMember<Database>(_ => _.Client)`, `IgnoreMembersWithType<CosmosDiagnostics>()`, `IgnoreMembersWithType<IndexingPolicy>()`, `IgnoreMembersWithType<ContainerProperties>()`, `IgnoreMembersWithType<DatabaseProperties>()`. `RequestCharge` is rounded to 1 decimal place. Converters: `HeadersConverter`, `FeedResponseConverter`, `ResponseConverter`.
- **Interactions with other Verify extensions**: Nothing in the readme. Test ModuleInitializer separates `VerifyCosmos.Init()` from `VerifierSettings.InitializePlugins()`.
- **MSBuild / project requirements**: `<PackageRequireLicenseAcceptance>true</PackageRequireLicenseAcceptance>` on the package. **Tests need a running Cosmos DB instance**: CI starts the Azure Cosmos DB Emulator (`"C:\Program Files\Azure Cosmos DB Emulator\CosmosDB.Emulator.exe" /NoUI /NoExplorer /NoFirewall`) and waits on port 8081 (`src/waitforport.ps1`). Windows-only in practice for local test runs against the emulator.
- **Sample verified output**:
```txt
{
  RequestCharge: 7.4,
  Headers: {},
  StatusCode: Created,
  Resource: {
    Id: Guid_1,
    LastName: Andersen,
    Address: {
      State: WA,
      County: King,
      City: Seattle
    }
  }
}
```
- **Notes for the wizard**: Requires a live Cosmos endpoint or the emulator — a generated sample test will not run out of the box. `RequestCharge` still leaks into snapshots (rounded, but real) and can vary across Cosmos versions. `PackageRequireLicenseAcceptance` is set, unusually. Tests are xunit.v3 on net8.0 with `<OutputType>Exe</OutputType>` and `NoWarn` for `xUnit1051`.

---

## Verify.CsvHelper
- **NuGet package id(s)**: `Verify.CsvHelper` (single package)
- **Current version**: `0.2.0`
- **Target frameworks**: `net8.0` (single)
- **One-line description**: Normalizes csv snapshots through CsvHelper, with per-column ignore, scrub and translate hooks and Verify's Guid/date counter scrubbing.
- **Tech tags**: csvhelper, csv, tabular data, column scrubbing
- **Third-party dependencies**: `CsvHelper` 33.1.0, `Verify` 33.1.0
- **Initialize API**: `VerifyCsvHelper.Initialize()` — no parameters. Adds a `csv` scrubber and a `CsvReader` file converter.
- **Requires InitializePlugins-compatible pattern?**: yes (`public static bool Initialized { get; private set; }` + `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Initialize() =>
    VerifyCsvHelper.Initialize();
```
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
  - `IgnoreCsvColumns(params string[] columns)` — drops the column from output. On `VerifySettings` and `SettingsTask`.
    ```cs
    [Test]
    public Task IgnoreColumns() =>
        VerifyFile("sample.csv")
            .IgnoreCsvColumns("Customer Id");
    ```
  - `ScrubCsvColumns(params string[] columns)` — replaces the column values with `{Scrubbed}`. On `VerifySettings` and `SettingsTask`.
    ```cs
    [Test]
    public Task ScrubCsvColumns() =>
        VerifyFile("sample.csv")
            .ScrubCsvColumns("Customer Id");
    ```
  - `TranslateCsvColumns(this VerifySettings, Func<string, Func<string?, string?>?> translate)` — per-column value transform factory. **Note the asymmetric naming:** the `SettingsTask` overload is singular — `TranslateCsvColumn(this SettingsTask, Func<string, Func<string?, string?>?>)`. Not covered in the readme. A `null` translate result renders `"null"`.
  - `Verify(csvReader)` for a `CsvHelper.CsvReader` instance:
    ```cs
    [Test]
    public Task VerifyReader()
    {
        using var reader = File.OpenText("sample.csv");
        using var csvReader = new CsvReader(reader, config);
        return Verify(csvReader);
    }
    ```
  - Readme: "Note that Guid and date scrubbing is respected" — values run through Verify's `Counter.TryConvert`.
- **Interactions with other Verify extensions**: Nothing in the readme. Note **Verify.ClosedXml** also emits `csv` targets, so a Verify.CsvHelper `csv` scrubber would also apply to ClosedXml's sheet csvs — a useful combination rather than a conflict.
- **MSBuild / project requirements**: `<SignAssembly>false</SignAssembly>` — this assembly is **not strong-named** (unlike most of the family). Internally uses `CsvConfiguration(CultureInfo.InvariantCulture) { NewLine = "\n" }`.
- **Sample verified output**:
```csv
Index,Customer Id,First Name,Last Name,Company,City,Country,Phone,Dob
1,Guid_1,Sheryl,Baxter,Rasmussen Group,East Leonard,Chile,229.077.5154,Date_1
2,Guid_2,Preston,Lozano,Vega-Gentry,East Jimmychester,Djibouti,5153435776,Date_2
3,Guid_3,Roy,Berry,Murillo-Perry,Isabelborough,Antigua and Barbuda,-1199,Date_3
```
- **Notes for the wizard**: Pre-1.0 (`0.2.0`). Not strong-named. The csv is round-tripped through CsvHelper (read then re-write), so quoting/escaping is normalized — existing verified files may shift on adoption. The `TranslateCsvColumn`/`TranslateCsvColumns` naming inconsistency is a real API wart to reproduce verbatim. Tests are NUnit on net8.0. Its ModuleInitializer calls `VerifyDiffPlex.Initialize()` but **not** `VerifierSettings.InitializePlugins()`.

---

## Verify.Diagnostics
- **NuGet package id(s)**: `Verify.Diagnostics` (single package)
- **Current version**: `1.0.0`
- **Target frameworks**: `net10.0` (single)
- **One-line description**: Records every `System.Diagnostics.Activity` created during a test and includes it in the snapshot under `activity`.
- **Tech tags**: opentelemetry, system.diagnostics, activity, activitysource, tracing, spans, observability
- **Third-party dependencies**: `Verify` 33.1.1 only (`Polyfill` 11.4.0 and `ProjectDefaults` are `PrivateAssets="all"`). No OpenTelemetry package — uses BCL `System.Diagnostics.ActivitySource`/`ActivityListener`.
- **Initialize API**: `VerifyDiagnostics.Initialize()` — no parameters. Installs a process-wide `ActivityListener` with `ShouldListenTo = _ => true` and `Sample = AllDataAndRecorded`, feeding `Recording.TryAdd("activity", activity)`.
- **Requires InitializePlugins-compatible pattern?**: yes (`public static bool Initialized { get; private set; }` + `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Initialize() =>
    VerifyDiagnostics.Initialize();
```
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
- **Verbose / edge-case APIs**:
  - Uses Verify core's `Recording` API (`Recording.Start()`, and by extension `Recording.Stop()`, `Recording.Start(identifier)`, `Recording.IgnoreNames("activity")`). Recorded under the name `activity`.
  - Serialization conventions, quoted from the readme:
    - `OperationName` is used as the JSON property key
    - `DisplayName` only included if different from `OperationName`
    - `Kind` only included if not `Internal`
    - `Status` and `StatusDescription` only included if not `Unset`
    - `Tags`, `Events`, `Links`, and `Baggage` included when present
    - Non-deterministic values (`Id`, `TraceId`, `SpanId`, `ParentSpanId`, `Duration`, `StartTimeUtc`, `Source`) are omitted
  - Converters: `ActivityConverter`, `ActivityEventConverter`, `ActivityLinkConverter`, `ActivityContextConverter`.
- **Interactions with other Verify extensions**: Nothing in the readme. It shares Verify's `Recording` bus with **Verify.EntityFramework** (name `ef`), **Verify.SqlServer** (name `sql`) and **Verify.Http** — all recorded entries land in the same verified file, so `Recording.IgnoreNames(...)` is the lever if a suite mixes them.
- **MSBuild / project requirements**: None. net10.0 only.
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
- **Notes for the wizard**: **Readme inaccuracy to work around** — it says "`RecordingActivityListener` allows... Call `RecordingActivityListener.Start()` to begin listening." No type named `RecordingActivityListener` exists anywhere in the source; the actual (and snippet-verified) call is Verify core's `Recording.Start()`. Do not generate `RecordingActivityListener.Start()`. The listener is global and never removed, so it captures activities from *all* sources for the whole test process. Version 1.0.0 (newest/least mature of this batch alongside EmailPreviewServices). Tests are xunit.v3 on net10.0 with `<OutputType>Exe</OutputType>`.

---

## Verify.DiffPlex
- **NuGet package id(s)**: `Verify.DiffPlex` (single package)
- **Current version**: `3.3.1`
- **Target frameworks**: `net462;net472;net48;net6.0;net7.0;net8.0;net9.0;net10.0` (widest in the family)
- **One-line description**: Replaces Verify's default text comparison failure message with a DiffPlex inline diff, in Full, Compact or Minimal form.
- **Tech tags**: diffplex, text diff, comparer, failure message, developer experience
- **Third-party dependencies**: `DiffPlex` 1.9.0, `Verify` 33.1.0 (`Polyfill` 11.4.0 and `ProjectDefaults` are `PrivateAssets="all"`)
- **Initialize API**: `VerifyDiffPlex.Initialize()` (equivalent to `Initialize(OutputType.Compact)`) and `VerifyDiffPlex.Initialize(OutputType outputType)`. Calls `VerifierSettings.SetDefaultStringComparer(...)`.
- **Requires InitializePlugins-compatible pattern?**: yes (`public static bool Initialized { get; private set; }` + `Initialize()`)
- **Minimal usage**:
```cs
public static class ModuleInitializer
{

    [ModuleInitializer]
    public static void Initialize() =>
        VerifyDiffPlex.Initialize();


    [ModuleInitializer]
    public static void OtherInitialize()
    {
        VerifierSettings.InitializePlugins();
        VerifierSettings.ScrubLinesContaining("DiffEngineTray");
        VerifierSettings.IgnoreStackTrace();
    }
}
```
- **Verbose / edge-case APIs**:
  - `VerifyTests.DiffPlex.OutputType` enum: `Full`, `Compact`, `Minimal`.
  - `VerifyDiffPlex.Initialize(OutputType.Compact)` — default. Readme: "It shows only the changed lines, with one line of context (with line number) before and after each changed section".
    ```cs
    [ModuleInitializer]
    public static void Init() =>
        VerifyDiffPlex.Initialize(OutputType.Compact);
    ```
  - `VerifyDiffPlex.Initialize(OutputType.Full)` — "shows the full contents of the received file, with differences... indicated by `+` and `-`".
  - `VerifyDiffPlex.Initialize(OutputType.Minimal)` — "show only the changed lines".
  - `UseDiffPlex(this VerifySettings settings, OutputType outputType = OutputType.Compact)`:
    ```cs
    [Test]
    public Task TestLevelUsage()
    {
        var target = "The text";
        var settings = new VerifySettings();
        settings.UseDiffPlex();
        return Verify(target, settings);
    }
    ```
  - `UseDiffPlex(this SettingsTask settings, OutputType outputType = OutputType.Compact)`:
    ```cs
    [Test]
    public Task TestLevelUsageFluent()
    {
        var target = "The text";
        return Verify(target)
            .UseDiffPlex();
    }
    ```
  - Compact mode emits `[BOF]`/`[EOF]` markers when the change is at the start/end, and zero-pads line numbers to the widest position width.
- **Interactions with other Verify extensions**: Nothing explicit in the readme, but it is the most widely paired package in this family — **every** repo surveyed except Verify.Avalonia references `Verify.DiffPlex` 3.3.1 in its test projects, and Verify.AspNetCore, Verify.AngleSharp and Verify.CsvHelper call `VerifyDiffPlex.Initialize(...)` in their ModuleInitializers. **Ordering caveat:** it calls `SetDefaultStringComparer`, which is the *default* comparer, so extension-specific comparers registered per-extension (`RegisterStringComparer("html", ...)` in Verify.AngleSharp / Verify.Bunit) take precedence for those extensions and DiffPlex still handles everything else.
- **MSBuild / project requirements**: None. Targets down to `net462`.
- **Sample verified output**: Not a snapshot producer. Failure-message output (Compact, the default):
```txt
Results do not match.
Differences:
Received: Tests.Sample.received.txt
Verified: Tests.Sample.verified.txt
Compare Result:
1 The
- before
+ after
3 text
```
- **Notes for the wizard**: This is a developer-experience extension — it never changes what is written to `.verified.*`, only the failure message. Safe and near-universally recommended as an add-on to any other extension. `OutputType` lives in `VerifyTests.DiffPlex`, so a `using VerifyTests.DiffPlex;` is needed when passing it. Note this repo's Directory.Packages.props spells the test package `Verify.Nunit` (lowercase `unit`). Tests are NUnit on net10.0; there is a second `CompactTests` project for the Compact output mode.

---

## Verify.DocNet
- **NuGet package id(s)**: `Verify.DocNet` (single package)
- **Current version**: `3.6.0`
- **Target frameworks**: `net6.0;net7.0;net8.0;net9.0;net10.0`
- **One-line description**: Converts pdf documents to png pages (plus per-page extracted text metadata) via DocNet/pdfium.
- **Tech tags**: docnet, pdfium, pdf, document rendering, png, page text
- **Third-party dependencies**: `Docnet.Core` 2.6.0, `DeterministicPdf` 2.0.2, `System.IO.Hashing` 10.0.12, `Verify` 33.1.0
- **Initialize API**: `VerifyDocNet.Initialize()` — no parameters. Registers a `pdf` stream converter and an `IDocReader` file converter.
- **Requires InitializePlugins-compatible pattern?**: yes (`public static bool Initialized { get; private set; }` + `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Initialize()
{
    VerifyDocNet.Initialize();
    // 0.95 tolerates cross-OS pdfium PNG rendering (default is 0.98).
    VerifierSettings.UseSsimForPng(0.95);
}
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
    var stream = File.OpenRead("sample.pdf");
    return Verify(stream, "pdf");
}
```
- **Verbose / edge-case APIs**:
  - `PreserveTransparency()` — keep the alpha channel when rendering. On `VerifySettings` and `SettingsTask`.
    ```cs
    [Test]
    public Task VerifyPreserveTransparency() =>
        VerifyFile("sample.pdf")
            .PreserveTransparency();
    ```
  - `PageDimensions(PageDimensions pageDimensions)` — set render size. On `VerifySettings` and `SettingsTask`.
    ```cs
    [Test]
    public Task VerifyPageDimensions() =>
        VerifyFile("sample.pdf")
            .PageDimensions(new(1080, 1920));
    ```
  - `SinglePage(int index)` — zero-based; doc comment: "Zero based index of single page to include (overrules PagesToInclude when in range)". Throws `ArgumentOutOfRangeException` when `index < 0`.
    ```cs
    [Test]
    public Task VerifyFirstPage()
    {
        var stream = File.OpenRead("sample.pdf");
        return Verify(stream, "pdf").SinglePage(0);
    }
    ```
  - `PagesToInclude(int count)` — cap rendered pages; throws `ArgumentOutOfRangeException` when `count < 1`. (Not in the readme, present in source on both `VerifySettings` and `SettingsTask`.)
  - `SkipPdfNormalization()` — snapshot pdf bytes verbatim, skipping trailer `/ID`, `/CreationDate`, `/ModDate` and XMP normalization. Doc comment warns toggling it shifts existing `.verified.pdf` once. (Not in the readme.)
  - `PdfInfo` metadata carries `Version`, `PageCount` (full document count regardless of filters) and `Pages` with each `PageInfo.Index` and `Text`.
- **Interactions with other Verify extensions**: Readme: "`VerifyImageMagick.RegisterComparers` (provided by https://github.com/VerifyTests/Verify.ImageMagick) allows minor image changes to be ignored." The repo's own ModuleInitializer instead uses core `VerifierSettings.UseSsimForPng(0.95)` — so the image comparer choice is effectively **either** `UseSsimForPng` **or** Verify.ImageMagick's comparers, not both. Also overlaps with **Verify.Aspose**, which registers its own `pdf` stream converter — the two conflict if both are initialized.
- **MSBuild / project requirements**: `<SignAssembly>false</SignAssembly>` — **not strong-named**. Pulls **native pdfium binaries** via `Docnet.Core` (per-RID native assets; can be a problem in trimmed/single-file or unusual RID scenarios). `<InternalsVisibleTo Include="Tests" />` in the shipped csproj. Rendering differs across OSes — hence the 0.95 SSIM threshold guidance.
- **Sample verified output**: The readme shows only the rendered png (`Samples.VerifyPdf#00.verified.png`); it does not include a `.verified.txt` sample. From source, the info target is shaped:
```txt
{
  Version: ...,
  PageCount: 2,
  Pages: [
    { Index: 0, Text: ... },
    { Index: 1, Text: ... }
  ]
}
```
- **Notes for the wizard**: Native dependency (pdfium) — cross-platform but RID-sensitive. Not strong-named. Lighter-weight, license-free alternative to Verify.Aspose for pdf-only scenarios; the two are mutually exclusive on the `pdf` extension. Two undocumented-in-readme settings (`PagesToInclude`, `SkipPdfNormalization`) worth surfacing. **Tests use TUnit** 1.68.17 + `Verify.TUnit` (the only TUnit repo in this batch), with `<OutputType>Exe</OutputType>`; `Magick.NET-Q16-AnyCPU` 14.17.1 is a test-only dependency.

---

## Verify.EmailPreviewServices
- **NuGet package id(s)**: `Verify.EmailPreviewServices` (single package)
- **Current version**: `1.0.0` (AssemblyVersion is `0.1.0`)
- **Target frameworks**: `net10.0` (single)
- **One-line description**: Renders an html email across real email clients/devices via the Email Preview Services API and snapshots one scrubbed webp per device.
- **Tech tags**: email, html email, email client preview, emailpreviewservices, webp, visual regression
- **Third-party dependencies**: `SixLabors.ImageSharp` 4.1.2, `Verify` 33.1.0 (`Polyfill` 11.4.0 and `ProjectDefaults` are `PrivateAssets="all"`). The API client is compiled in from `..\Swagger\EmailPreviewServicesClient.cs`, not a package.
- **Initialize API**: `VerifyEmailPreviewServices.Initialize(string? apiKey = null)` — when `apiKey` is null the key is read from the `EmailPreviewServicesApiKey` environment variable; if neither is present it throws "Provide an apiKey via VerifyEmailPreviewServices.Initialize(apiKey) or an EmailPreviewServicesApiKey environment variable."
- **Requires InitializePlugins-compatible pattern?**: yes (`public static bool Initialized { get; private set; }` + `Initialize()` with an optional parameter)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyEmailPreviewServices.Initialize();
```
```cs
[Test]
[Explicit]
public async Task GeneratePreview()
{
    var preview = new EmailPreview
    {
        Html = html,
        Devices =
            [
                Device.OutlookWebDarkModeChrome,
                Device.iPhone13
            ]
    };
    await Verify(preview);
}
```
- **Verbose / edge-case APIs**:
  - `VerifyEmailPreviewServices.Initialize("ApiKey")` — explicit key instead of the environment variable.
    ```cs
    [ModuleInitializer]
    public static void Init() =>
        VerifyEmailPreviewServices.Initialize("ApiKey");
    ```
  - `VerifyTests.EmailPreviewServices.EmailPreview` — `required string Html { get; init; }` and `required ICollection<Device> Devices { get; init; }`.
  - `VerifyTests.Device` enum — ~50 clients including `Android9`, `AOLBasic/Chrome/Firefox`, `AppleMailDark/Light`, `eMClient`, `Freenet`, `GmailFirefox`, `GMX`, `iCloud`, `iPadAir`, `iPhone8/11/12/12Pro/13/13ProMax/SE`, `Mailbirddark/light`, `Outlook2003/2007/2010/2013/2016/2016PlainText/2019`, `Outlook2019MacDark/Light`, `Office365Dark/Light`, `OutlookWebChrome/Firefox`, `OutlookWebDarkModeChrome/Firefox`, `Postbox6`, `RoundcubeChrome`, `Seznam`, `Thunderbird`, `Windows10MailDark/Light`, `WindowsLiveMail2012`, `YahooBasic/Chrome/Firefox`, `ZimbraDesktop`, `ZohoDark/Light`, `o2pl`, `onetpl`, `WPpl`.
  - Duplicate devices throw: `InvalidOperationException("Duplicate devices found.")`.
  - Per-device chrome cropping/scrubbing is applied automatically (`Scrubber` + per-device `ScrubSpec` top/bottom/left/right offsets), so client UI chrome doesn't cause false diffs.
  - Recommended debug-only gating from the readme:
    ```cs
    [Test]
    #if DEBUG
    [Explicit]
    #endif
    public async Task GeneratePreview()
    {
        var preview = new EmailPreview
        {
            Html = html,
            Devices = [Device.Outlook2019]
        };
        await Verify(preview);
    }
    ```
- **Interactions with other Verify extensions**: Nothing in the readme. Its test project references `Verify.Http` 7.5.1 (test-only) and the ModuleInitializer calls `VerifierSettings.InitializePlugins(); VerifierSettings.UseSsimForPng();` — SSIM is relevant since output is image-based (though targets are `webp`, not `png`).
- **MSBuild / project requirements**: **A paid EmailPreviewServices account and API key are required** — readme: "[EmailPreviewServices is a paid service](https://emailpreviewservices.com/en/pricing), an account is required to get an API key." Key supplied via `EmailPreviewServicesApiKey` environment variable or the `Initialize` overload. Directory.Build.props carries a `<SixLaborsLicenseKey>` (ImageSharp Community license, expires 2027-11-25) — **consumers of the package must supply their own ImageSharp license configuration** if required by their usage. Requires network access to `https://app.emailpreviewservices.com/api`. net10.0 only.
- **Sample verified output**: Image-only — one `.verified.webp` per device, named `Samples.GeneratePreview#OutlookWebDarkModeChrome.verified.webp`, `Samples.GeneratePreview#iPhone13.verified.webp`. No text snapshot.
- **Notes for the wizard**: Paid service + API key + network — flag as not runnable out of the box. **Very slow**: readme says ~20s for a single device, ~25s for five, and "Execution will timeout after 6min and throw an exception" (source: 360 × 1s polls; `HttpClient.Timeout` is 10 minutes). Strongly recommend generating tests marked `[Explicit]` under `#if DEBUG`. Previews are deleted server-side after retrieval (`DeletePreviewAsync` in a `finally`). Version `1.0.0` but `AssemblyVersion` is `0.1.0` (inconsistent with siblings' `1.0.0`). Tests are NUnit on net10.0. The build badge in the readme points at `Verify-Ulid` (copy/paste error, not this project).

---

## Verify.EntityFramework
- **NuGet package id(s)**: **two packages** — `Verify.EntityFramework` (EF Core) and `Verify.EntityFrameworkClassic` (EF6). Both built from this repo and versioned together.
- **Current version**: `15.4.1` (shared `src/Directory.Build.props`, applies to both packages)
- **Target frameworks**: `Verify.EntityFramework` → `net10.0`; `Verify.EntityFrameworkClassic` → `net48;net8.0;net9.0;net10.0`
- **One-line description**: Snapshot testing for EntityFramework — records executed SQL, verifies `ChangeTracker` state, converts `IQueryable` to formatted SQL, and replays recent migrations.
- **Tech tags**: entity framework core, entity framework 6, ef core, orm, sql, sql server, changetracker, queryable, migrations, recording
- **Third-party dependencies**:
  - `Verify.EntityFramework`: `Microsoft.EntityFrameworkCore` 10.0.12, `Microsoft.EntityFrameworkCore.Relational` 10.0.12, `Microsoft.SqlServer.TransactSql.ScriptDom` 180.107.0, `Verify` 33.1.1
  - `Verify.EntityFrameworkClassic`: `EntityFramework` 6.5.2, `System.Data.SqlClient` 4.9.1, `Verify` 33.1.1
  - Test-only: `EfLocalDb` 26.2.0, `EfClassicLocalDb` 26.2.0, `Microsoft.EntityFrameworkCore.SqlServer` 10.0.12, `Microsoft.EntityFrameworkCore.InMemory` 10.0.12, `Microsoft.AspNetCore.Mvc.Testing` 10.0.12, `Verify.SqlServer` 12.2.0
- **Initialize API**:
  - EF Core: `VerifyEntityFramework.Initialize(IModel? model = null)`, `Initialize(IModel? model, bool recordCommands)`, `Initialize(DbContext context)`, `Initialize(DbContext context, bool recordCommands)`. `model` caches navigation-property info for parameterless `IgnoreNavigationProperties()`. `recordCommands: false` leaves the recording interceptor unattached, making every `EnableRecording()` a no-op.
  - EF Classic: `VerifyEntityFrameworkClassic.Initialize()` — no parameters.
- **Requires InitializePlugins-compatible pattern?**: yes for both (`public static bool Initialized { get; private set; }` + `Initialize()`)
- **Minimal usage**:
```cs
static IModel GetDbModel()
{
    var options = new DbContextOptionsBuilder<SampleDbContext>();
    options.UseSqlServer("fake");
    using var data = new SampleDbContext(options.Options);
    return data.Model;
}

[ModuleInitializer]
public static void Init()
{
    var model = GetDbModel();
    VerifyEntityFramework.Initialize(model);
}
```
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyEntityFrameworkClassic.Initialize();
```
```cs
var builder = new DbContextOptionsBuilder<SampleDbContext>();
builder.UseSqlServer(connection);
builder.EnableRecording();
var data = new SampleDbContext(builder.Options);
```
```cs
Recording.Start();

await data
    .Companies
    .Where(_ => _.Name == "Title")
    .ToListAsync();

await Verify();
```
- **Verbose / edge-case APIs**:
  - `EnableRecording<TContext>(this DbContextOptionsBuilder<TContext>)` and `EnableRecording<TContext>(this DbContextOptionsBuilder<TContext>, string? identifier)` — attach the command interceptor. Readme: "`EnableRecording` should only be called in the test context."
  - `Recording.Start()` / `Recording.Stop()` (Verify core) — recorded under the name `ef`; results auto-included in the verified file unless an identifier is used.
    ```cs
    var entries = Recording.Stop();
    //TODO: optionally filter the results
    await Verify(
        new
        {
            target = data.Companies.Count(),
            entries
        });
    ```
  - Multi-`DbContext` aggregation — "`Recording.Start()` can be called on different DbContext instances (built from the same options) and the results will be aggregated."
  - `context.DisableRecording()` — per-instance opt-out mid-test (`data.DisableRecording();`).
  - `VerifyEntityFramework.Initialize(data, recordCommands: false)` — global recording opt-out; readme: "Only recording is disabled. The converters, and the queryable to SQL file converter, are still registered."
  - `Verify(data.ChangeTracker)` — Added / Deleted / Modified entity snapshots.
  - `Verify(queryable)` — EF Core emits a `.verified.txt` (materialized results) **and** a `.verified.sql`; EF Classic emits the SQL as `.verified.txt`.
  - `data.AllData()` (`IAsyncEnumerable<object>`) — every entity in the database, ordered by entity type name then `Id`:
    ```cs
    await Verify(data.AllData())
        .AddExtraSettings(
            serializer =>
                serializer.TypeNameHandling = TypeNameHandling.Objects);
    ```
  - `IgnoreNavigationProperties()` — overloads: static `VerifyEntityFramework.IgnoreNavigationProperties(IModel? model = null)`, and instance/fluent on `VerifySettings`/`SettingsTask` taking `DbContext` or `IModel?`:
    ```cs
    await Verify(employee)
        .IgnoreNavigationProperties();
    ```
    ```cs
    VerifyEntityFramework.IgnoreNavigationProperties();
    ```
  - `UseDescriptiveTableAliases<TContext>(this DbContextOptionsBuilder<TContext>)` — replaces EF's single-char aliases (`c`, `e`) with full table names in generated SQL.
  - `UseDescriptiveParameterNames<TContext>(this DbContextOptionsBuilder<TContext>)` — replaces `@p0`/`@p1` with column names; duplicates across tables in a batch get an entity-name prefix (`@Id` then `@EmployeeId`), with a counter suffix fallback on further collision.
  - `VerifyEntityFramework.ScrubInlineEfDateTimes()` (static), `settings.ScrubInlineEfDateTimes()` and `.ScrubInlineEfDateTimes()` fluent — readme: "a convenience method that calls `.ScrubInlineDateTimes("yyyy-MM-ddTHH:mm:ss.fffffffZ")`", for cases such as temporal-table queries where EF inlines DateTimes.
  - `VerifyEntityFramework.DisableSqlFormatting = true;` — static property; disables [SqlFormatter](https://github.com/SimonCropp/SqlFormatter) reformatting of SQL Server SQL for both Recording output and Queryable `.sql` files. "When disabled, the SQL is written verbatim as produced by EntityFramework."
  - `MigrationReplay.ReplayRecentMigrations<TDbContext>(this TDbContext data, ushort count = 5, Func<TDbContext, Task>? afterEachMigration = null, Cancel cancel = default)`:
    ```cs
    await using var database = await sqlInstance.Build();

    await database.Context.ReplayRecentMigrations(
        count: 5,
        afterEachMigration: ApplyDeploymentState);
    ```
    Requires a database with **no migrations applied** (throws otherwise, listing the applied ones); `count: 0` throws `ArgumentOutOfRangeException`.
  - WebApplicationFactory integration testing requires a named recording identifier:
    ```cs
    var dataBuilder = new DbContextOptionsBuilder<SampleDbContext>()
        .EnableRecording(name)
        .UseSqlServer(connectionString);
    ```
    ```cs
    Recording.Start(testName);
    var companies = await httpClient.GetFromJsonAsync<Company[]>("/companies");
    var entries = Recording.Stop(testName);
    ```
    Readme: "The results will not be automatically included in verified file so it will have to be verified manually."
  - Readme pointer: "To detect and correct missing `OrderBy` clauses in EF queries, use [EntityFramework.OrderBy](https://github.com/SimonCropp/EntityFramework.OrderBy)."
- **Interactions with other Verify extensions**: This is the richest cross-extension section in the batch. Readme, "Disabling Recording globally":
  > "This is useful when another package records the same commands. [Verify.SqlServer](https://github.com/VerifyTests/Verify.SqlServer) subscribes to the `Microsoft.Data.SqlClient` diagnostic listener and records under the name `sql`. Since EF Core executes its commands through `SqlCommand`, with both packages recording every command EF executes is captured twice: once as `ef` and once as `sql`. Disable one of the two:
  >  * `VerifyEntityFramework.Initialize(model, recordCommands: false)` keeps the `sql` entries.
  >  * `VerifySqlServer.Initialize(recordCommands: false)` keeps the `ef` entries, which also carry the command `Type` and transaction state. **It has to be called before `VerifierSettings.InitializePlugins()`**, otherwise plugin discovery initializes Verify.SqlServer first with recording enabled, and the explicit call throws `Already Initialized`."

  The repo's own test ModuleInitializer takes the third route: `VerifierSettings.InitializePlugins(); Recording.IgnoreNames("sql");`. Also shares the `Recording` bus with **Verify.Diagnostics** (`activity`) and **Verify.Http**.
- **MSBuild / project requirements**: `NoWarn` includes `EF1001` (internal EF API usage) and the source suppresses `EF9002` around `ReplaceService<ISqlAliasManagerFactory, ...>` — the descriptive-alias feature uses experimental EF APIs. **Tests need SQL Server LocalDB** (`EfLocalDb` / `EfClassicLocalDb`), so Windows-only for the full suite; `Microsoft.EntityFrameworkCore.InMemory` covers some tests. SQL formatting only applies when the model's type-mapping source is `SqlServerTypeMappingSource` — non-SQL-Server providers get verbatim SQL regardless of `DisableSqlFormatting`.
- **Sample verified output**:
```txt
{
  ef: {
    Type: ReaderExecutedAsync,
    HasTransaction: false,
    Text:
select c.Id,
       c.Name
from   Companies as c
where  c.Name = N'Title'
  }
}
```
```txt
{
  Modified: {
    Company: {
      Id: 0,
      Name: {
        Original: old name,
        Current: new name
      }
    }
  }
}
```
- **Notes for the wizard**: **The only multi-package repo in this batch** — a wizard must ask EF Core vs EF6 (or both) and pick `Verify.EntityFramework` / `Verify.EntityFrameworkClassic` accordingly; the two `Initialize` APIs differ (EF Core takes an `IModel`/`DbContext`, Classic takes nothing). The `GetDbModel()` bootstrap (build a context against `UseSqlServer("fake")` purely to get `Model`) is idiomatic here and worth generating verbatim. The Verify.SqlServer double-recording conflict and its **Initialize-before-InitializePlugins ordering requirement** are the sharpest edge in this whole catalogue. Has both static (`VerifyEntityFramework.IgnoreNavigationProperties()`, `DisableSqlFormatting`) and instance/fluent APIs. Four test projects exist specifically to isolate global-state combinations (`Tests`, `RecordingDisabledTests`, `StaticSettingsTests`, `Classic.Tests`) — a hint that these settings are process-global and cannot be mixed within one assembly. Tests are NUnit.
