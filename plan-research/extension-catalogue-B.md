# Verify extension catalogue - batch B

## Verify.FakeItEasy
- **NuGet package id(s)**: Verify.FakeItEasy (single package)
- **Current version** (from src/Directory.Build.props <Version>): 2.1.0
- **Target frameworks**: net48;net8.0;net9.0
- **One-line description**: Adds Verify support for verifying FakeItEasy types (fakes, recorded calls, FakeManager).
- **Tech tags**: fakeiteasy, mocking, fakes, test-doubles, unit-testing
- **Third-party dependencies**: FakeItEasy 9.0.1; Verify 33.1.0; Polyfill 11.4.0 (PrivateAssets="all"); ProjectDefaults 1.0.181 (PrivateAssets="all")
- **Initialize API**: `VerifyFakeItEasy.Initialize()` - no parameters. Registers `CallConverter` and `FakeConverter` via `VerifierSettings.AddExtraSettings`. Throws "Already Initialized" if called twice; calls `InnerVerifier.ThrowIfVerifyHasBeenRun()`.
- **Requires InitializePlugins-compatible pattern?** yes (`public static bool Initialized { get; private set; }` + `public static void Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyFakeItEasy.Initialize();
```
```cs
[Fact]
public Task ReceivedCalls()
{
    var target = A.Fake<ITarget>();
    target.Method(1, 2);
    var calls = Fake.GetCalls(target);
    return Verify(calls);
}
```
- **Verbose / edge-case APIs**:
  - Verifying a `FakeManager` instance (readme: "A instance of `FakeManager` can also be verified."):
```cs
[Fact]
public Task FakeManager()
{
    var target = A.Fake<ITarget>();
    target.Method(1, 2);
    var fakeManager = Fake.GetFakeManager(target);
    return Verify(fakeManager);
}
```
  - No extension-specific `VerifySettings` extension methods, scrubbers, or Recording APIs exist in this package.
- **Interactions with other Verify extensions**: readme says nothing. The test project's ModuleInitializer shows the combined pattern: `VerifyFakeItEasy.Initialize();` in one `[ModuleInitializer]` and `VerifierSettings.InitializePlugins();` in another.
- **MSBuild / project requirements**: none beyond a test project referencing Verify + FakeItEasy. Repo tests use xunit.v3 4.0.1 / Verify.XunitV3 33.1.0.
- **Sample verified output**:
```txt
[
  {
    Method: ITarget.Method(int a, int b),
    Arguments: [
      1,
      2
    ]
  }
]
```
- **Notes for the wizard**: Purely a serializer-converter plugin - no file converters, no comparers. Same shape (and nearly identical verified output) as Verify.NSubstitute and Verify.Moq; these three are alternatives picked by which mocking library the user uses. Repo has no code_of_conduct.md but does have readme.md.

## Verify.Flurl
- **NuGet package id(s)**: Verify.Flurl (single package)
- **Current version**: 2.0.0
- **Target frameworks**: net10.0 (single TFM)
- **One-line description**: Extends Verify to allow verification of Flurl bits - primarily `HttpTest` and its recorded calls.
- **Tech tags**: flurl, http, httpclient, rest, web-api
- **Third-party dependencies**: Flurl 4.0.0; Flurl.Http 4.0.2; Verify 33.1.0; Verify.Http 7.5.1; Argon 0.37.0; Argon.Xml 0.37.0; Polyfill 11.4.0 (private); ProjectDefaults 1.0.181 (private)
- **Initialize API**: `VerifyFlurl.Initialize()` - no parameters. Registers `HttpTestConverter` and `CallConverter`. IMPORTANT: internally does `if (!VerifyHttp.Initialized) { VerifyHttp.Initialize(); }` before setting `Initialized = true`, so Verify.Http is auto-initialized.
- **Requires InitializePlugins-compatible pattern?** yes (`Initialized` property + `Initialize()`)
- **Minimal usage** (readme):
```cs
public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize() =>
        VerifyFlurl.Initialize();
}
```
```cs
[Fact]
public async Task Usage()
{
    using var httpTest = new HttpTest();

    httpTest.RespondWith("OK");

    await "http://api.mysite.com/".GetAsync();
    await "http://api.mysite.com/".PostAsync(new StringContent("the content"));

    await Verify(httpTest);
}
```
- **Verbose / edge-case APIs**: none beyond `Initialize()`. The readme documents no settings extensions, scrubbers or recording APIs of its own (all http scrubbing/settings come from Verify.Http).
- **Interactions with other Verify extensions**: readme: "Alternatively, use `VerifierSettings.InitializePlugins()` to initialize all Verify plugins with default settings."
```cs
public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize() =>
        VerifierSettings.InitializePlugins();
}
```
  The shipped package takes a hard `PackageReference` on **Verify.Http 7.5.1**, and `VerifyFlurl.Initialize()` transitively calls `VerifyHttp.Initialize()` when it has not already run - so ordering is safe either way, but do not assume Verify.Http is uninitialized afterwards. The repo's own Tests ModuleInitializer uses only `VerifierSettings.InitializePlugins();`.
- **MSBuild / project requirements**: net10.0 only. Repo also contains a `SampleWebApplication` (ASP.NET + Swashbuckle.AspNetCore / Microsoft.AspNetCore.OpenApi) used by tests only.
- **Sample verified output**:
```txt
[
  {
    Request: http://api.mysite.com/,
    Response: {
      Status: 200 OK,
      Content: {
        Headers: {
          Content-Length: 2,
          Content-Type: text/plain; charset=utf-8
        },
        Value: OK
      }
    }
  }
]
```
- **Notes for the wizard**: Effectively a thin add-on to Verify.Http - anyone using Verify.Flurl automatically gets the Verify.Http converters. net10.0-only, so it cannot be offered for older TFMs.

## Verify.HeadlessBrowsers
- **NuGet package id(s)**: THREE packages from one repo - Verify.Playwright, Verify.Puppeteer, Verify.Selenium (one csproj each under src/; PackageId comes from the folder/project name).
- **Current version**: 3.1.1 (shared `<Version>` in src/Directory.Build.props, applies to all three packages)
- **Target frameworks**: net10.0 for all three (Verify.Puppeteer and Verify.Selenium set `<SignAssembly>false</SignAssembly>`)
- **One-line description**: Extends Verify to allow verification of Web UIs (page/element HTML plus screenshots) using headless browsers via Playwright, Puppeteer or Selenium.
- **Tech tags**: playwright, puppeteer, selenium, headless-browser, web-ui, screenshot, html, chromium, browser-automation
- **Third-party dependencies**:
  - Verify.Playwright: Microsoft.Playwright 1.62.0; Verify 33.1.0
  - Verify.Puppeteer: PuppeteerSharp 25.11.0; Verify 33.1.0 (also `<Compile Include="..\Verify.Playwright\InnerSocketWaiter.cs" />`)
  - Verify.Selenium: Selenium.WebDriver 4.49.0; Verify 33.1.0 (also `<Compile Include="..\Verify.Playwright\InnerSocketWaiter.cs" />`)
  - test-only (NOT shipped): Selenium.WebDriver.ChromeDriver 153.0.8010.5200, Verify.ImageMagick 3.10.0, Magick.NET-Q16-AnyCPU 14.17.1, Verify.AngleSharp 5.1.2, Verify.NUnit 33.1.0, Microsoft.AspNetCore.TestHost 10.0.12
- **Initialize API**:
  - `VerifyPlaywright.Initialize(bool installPlaywright = false)` - when `installPlaywright: true` it runs `Program.Main(["install"])` (the Playwright browser install). Registers file converters for `IPage`, `IElementHandle`, `ILocator`.
  - `VerifyPuppeteer.Initialize()` - no parameters. Registers file converters for `ElementHandle` and `Page`.
  - `VerifySelenium.Initialize()` - no parameters. Registers file converters for `WebDriver` and `IWebElement`.
- **Requires InitializePlugins-compatible pattern?** yes for all three (each has `public static bool Initialized { get; private set; }` + `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void InitPlaywright() =>
    VerifyPlaywright.Initialize(installPlaywright: true);
```
```cs
var page = await browser.NewPageAsync();
await page.GotoAsync("http://localhost:5000");
await Verify(page);
```
```cs
[ModuleInitializer]
public static void InitSelenium() =>
    VerifySelenium.Initialize();
```
```cs
await Verify(driver);
```
- **Verbose / edge-case APIs**:
  - `SocketWaiter.Wait(int port)` - wait for the target server to start before driving the browser. NOTE three different namespaces: `VerifyTestsPlaywright.SocketWaiter`, `VerifyTests.Puppeteer.SocketWaiter`, `VerifyTests.Selenium.SocketWaiter`.
```cs
// wait for target server to start
await SocketWaiter.Wait(port: 5000);
```
  - `PageScreenshotOptions(this VerifySettings, PageScreenshotOptions)` and `PageScreenshotOptions(this SettingsTask, PageScreenshotOptions, bool screenshotOnly = false)` - Playwright only; `screenshotOnly: true` suppresses the HTML file and produces only the page image.
```cs
await Verify(page)
    .PageScreenshotOptions(
        new()
        {
            Quality = 50,
            Type = ScreenshotType.Jpeg
        },
        screenshotOnly: true);
```
  - `ElementScreenshotOptions(this VerifySettings, ElementHandleScreenshotOptions)` / `(this SettingsTask, ElementHandleScreenshotOptions, bool screenshotOnly = false)` - Playwright `IElementHandle` screenshots.
  - `LocatorScreenshotOptions(this VerifySettings, LocatorScreenshotOptions)` / `(this SettingsTask, LocatorScreenshotOptions, bool screenshotOnly = false)` - Playwright `ILocator` screenshots.
  - Internal `ValidateNoPath` throws "ScreenshotOptions Path not supported." if `Path` is set on any screenshot options object.
  - Selenium extras (`SeleniumExtensions`): `driver.WaitForIsReady()` and `element.GetSource()`.
  - Verifiable targets: Playwright `await Verify(page)`, `await Verify(await page.QuerySelectorAsync("#someId"))`, `await Verify(page.Locator("#someId"))`; Puppeteer `await Verify(page)` / `await Verify(element)`; Selenium `await Verify(driver)` / `await Verify(driver.FindElement(By.Id("someId")))`.
- **Interactions with other Verify extensions**: readme section "Compatibility with other Verify plugins": "This projects is designed to be compatible with other Verify plugins. One common scenario for combining plugins is using [Verify.AngleSharp](https://github.com/VerifyTests/Verify.AngleSharp) to manipulate the verified html." and "For example using the Verify.AngleSharp [Pretty Print](https://github.com/VerifyTests/Verify.AngleSharp?tab=readme-ov-file#pretty-print) extension."
```cs
var page = await browser.NewPageAsync();
await page.GotoAsync("http://localhost:5000");
await Verify(page)
    .PrettyPrintHtml();
```
  Also: "The rendering can vary slightly between different OS versions. This can make verification on different machines (eg CI) problematic. A [custom comparer](https://github.com/VerifyTests/Verify/blob/master/docs/comparer.md) can be used to mitigate this." The repo's own tests do exactly that with `VerifyImageMagick.RegisterComparers(.12);` next to `VerifierSettings.InitializePlugins();`.
- **MSBuild / project requirements**: needs a real browser. Playwright browsers install via `Initialize(installPlaywright: true)`; Puppeteer needs `new BrowserFetcher(SupportedBrowser.Chrome).DownloadAsync()`; Selenium needs the `Selenium.WebDriver.ChromeDriver` package (or a chromedriver on PATH). Tests need a web server listening (the repo starts Kestrel from a `[ModuleInitializer]` in `WebStartup`, port 5000). net10.0 only. Screenshot output is OS/renderer sensitive.
- **Sample verified output** (`.html`; a `.png` screenshot is emitted alongside it):
```html
<!DOCTYPE html><html lang="en"><head>
  <meta charset="utf-8">
  <title>The Title</title>
  <link href="https://getbootstrap.com/docs/4.0/dist/css/bootstrap.min.css" rel="stylesheet">
</head>
<body>
  <div class="jumbotron">
    <h1 class="display-4">The Awareness Of Relative Idealism</h1>
    <a id="someId" class="btn btn-primary btn-lg" href="#" role="button">Learn more</a>
  </div>

</body></html>
```
- **Notes for the wizard**: one repo, three independent NuGet packages sharing one version - ask which driver the user wants (they are alternatives, though all three can be initialized in the same test assembly, as the repo's own ModuleInitializer does). Each `Verify(page)` normally produces TWO verified files (`.html` + `.png`) unless `screenshotOnly: true`. Readme "Subpixel text rendering": launch Chromium with `--disable-lcd-text` (Playwright: `Args = ["--disable-lcd-text"]`; same argument for Puppeteer and Selenium/Chrome) for reproducible screenshots, and re-accept existing verified screenshots once after enabling it. Puppeteer and Selenium assemblies are NOT strong-name signed.

## Verify.Http
- **NuGet package id(s)**: Verify.Http (single package)
- **Current version**: 8.0.0
- **Target frameworks**: net10.0 (single TFM)
- **One-line description**: Extends Verify to allow verification of Http bits - `HttpRequestMessage`/`HttpResponseMessage` converters, HttpClient recording, and a `MockHttpClient`.
- **Tech tags**: http, httpclient, rest, web-api, mocking, recording, httprequestmessage, httpresponsemessage
- **Third-party dependencies**: Microsoft.Extensions.Http 10.0.12; Microsoft.Net.Http.Headers 10.0.12; Microsoft.Extensions.DiagnosticAdapter 3.1.32; Argon.Xml 0.37.0; Verify 33.1.0; Polyfill 11.4.0 (private); ProjectDefaults 1.0.181 (private)
- **Initialize API**: `VerifyHttp.Initialize()` - no parameters. Subscribes a `DiagnosticListener.AllListeners` observer (for recording), registers a `HttpResponseMessage` file converter and ~13 json converters (`HttpStatusCodeConverter` inserted at 0, `XmlNodeConverter`, `HttpMethodConverter`, `UriConverter`, `HttpHeadersConverter`, `HttpContentHeadersConverter`, `HttpContentConverter`, `HttpRequestMessageConverter`, `HttpResponseMessageConverter`, `HttpRequestConverter`, `HttpResponseConverter`, `MockHttpClientConverter`, `MockHttpHandlerConverter`).
- **Requires InitializePlugins-compatible pattern?** yes (`Initialized` property + `Initialize()`)
- **Minimal usage** (readme "Initialize": "Call `VerifierSettings.InitializePlugins()` in a `[ModuleInitializer]`." / "Or, if order of plugins is important, use `VerifyHttp.Initialize()` in a `[ModuleInitializer]`."):
```cs
public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize() =>
        VerifierSettings.InitializePlugins();
}
```
```cs
[Test]
public async Task HttpResponse()
{
    using var client = new HttpClient();

    var result = await client.GetAsync("https://httpcan.org/json");

    await Verify(result);
}
```
```cs
[Test]
public async Task DefaultContent()
{
    using var client = new MockHttpClient();

    var result = await client.GetAsync("https://fake/get");

    await Verify(result);
}
```
- **Verbose / edge-case APIs**:
  - Converters shipped for: `HttpMethod`, `Uri`, `HttpHeaders`, `HttpContent`, `HttpRequestMessage`, `HttpResponseMessage`.
  - `ScrubHttpTextResponse(this VerifySettings, Func<string,string>)` / `(this SettingsTask, Func<string,string>)` - rewrite text http response bodies before verification.
```cs
await Verify(result)
    .ScrubHttpTextResponse(_ => _.Replace("Herman Melville - Moby-Dick", "New title"));
```
  - Ignoring headers - "Headers are treated as properties, and hence can be ignored using `IgnoreMember`":
```cs
await Verify(result)
    .IgnoreMembers(
        "Server",
        "Content-Length",
        "Access-Control-Allow-Credentials");
```
  - `IHttpClientBuilder.AddRecording()` returns a `RecordingHandler`; all calls land in `recording.Sends`.
```cs
var httpBuilder = collection.AddHttpClient<MyService>();
var recording = httpBuilder.AddRecording();
...
await Verify(recording.Sends)
    .IgnoreMember("Date");
```
  - `ServiceCollection.AddRecordingHttpClient(string? name = null)` returns `(IHttpClientBuilder builder, RecordingHandler recording)` - "Adds a AddHttpClient and adds a RecordingHandler using AddHttpMessageHandler".
```cs
var (builder, recording) = collection.AddRecordingHttpClient();
```
  - `RecordingHandler(bool recording = true)` with `recording.Pause()`, `recording.Resume()`, `recording.Recording`, `recording.Sends` - "Recording is enabled by default. So Pause to stop recording". Can be added explicitly via `builder.AddHttpMessageHandler(() => recording);`.
  - Listener-based recording: `Recording.Start()` (readme "Enable at any point in a test using `VerifyTests.Recording.Start()`"); requests/responses are appended to the verified file automatically.
  - Explicit recording: `Recording.Stop().Select(_ => _.Data).OfType<HttpCall>()` - `HttpCall` exposes `Request` (`HttpRequest`), `Response` (`HttpResponse?`), `Duration` (`TimeSpan?`, `[JsonIgnore]`), `Status` (`TaskStatus?`). Used to filter/assert:
```cs
var httpCalls = Recording.Stop()
    .Select(_ => _.Data)
    .OfType<HttpCall>()
    .ToList();
```
  - `MockHttpClient` constructor overloads (all with `bool recording = false`): `()`/`(HttpStatusCode status = HttpStatusCode.OK)`, `(string content, string mediaType)`, `(HttpResponseMessage response)`, `(params IEnumerable<HttpResponseMessage> responses)`, `(params IEnumerable<HttpStatusCode> statuses)`, `(params IEnumerable<string> files)`, `(Func<HttpRequestMessage, HttpResponseMessage> responseBuilder)`. Also `MockHttpClient.Calls` (`IReadOnlyCollection<HttpCall>`) and the settable `SimulateNetworkStream`.
  - `MockHttpHandler` has the same constructor set plus `Calls` and `SimulateNetworkStream { get; set; }` - use when you need the handler rather than the client.
  - Files as responses - include them in the csproj:
```csproj
<None Include="sample.*">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</None>
```
```cs
using var client = new MockHttpClient(
    "sample.html",
    "sample.json",
    "sample.xml");
```
  - Recording mock interactions: `new MockHttpClient(recording: true)` + `Recording.Start()`.
  - `SimulateNetworkStream = true` reproduces `HttpCompletionOption.ResponseHeadersRead` semantics: non-seekable, unbuffered, read-once stream (`stream.Position` throws `NotSupportedException`).
```cs
using var client = new MockHttpClient("sample.html");
client.SimulateNetworkStream = true;
var result = await client.GetAsync(
    "https://fake/get",
    HttpCompletionOption.ResponseHeadersRead);
```
  - Other public helpers: `HttpExtensions.StatusText`, `IsDefaultVersion`, `IsDefaultVersionPolicy`, `TryReadStringContent`, `TryGetExtension`, `IsText`; `Extensions.ToDictionary/Simplify/NotCookies/Cookies` on `HttpHeaders`.
- **Interactions with other Verify extensions**: the readme mentions no other Verify.* package by name (no Verify.AspNetCore section). It only says ordering matters between plugins: "Or, if order of plugins is important, use `VerifyHttp.Initialize()` in a `[ModuleInitializer]`." Verify.Flurl depends on and auto-initializes this package. The repo's test project references Verify.AngleSharp and Verify.DiffPlex (test-only).
- **MSBuild / project requirements**: net10.0 only. Mock response files need `<None Include="sample.*"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></None>`. Live-http samples in the readme hit `https://httpcan.org`, so the repo's own tests need internet; users do not.
- **Sample verified output**:
```txt
{
  Status: 200 OK,
  Headers: {
    Connection: keep-alive,
    Date: DateTime_1,
    Server: cloudflare
  },
  Content: {
    Headers: {
      Content-Length: 274,
      Content-Type: application/json
    },
    Value: {
      slideshow: {
        title: Sample Slide Show
      }
    }
  }
}
```
- **Notes for the wizard**: largest/most feature-rich package in this batch; three distinct feature areas (converters, recording, mocking) that can be offered independently. `useGlobalUsings` note: the repo tests use `global using VerifyTests.Http;` to reach `HttpCall`. Recording requires `Recording.Start()` from Verify core. `MockHttpClient`/`MockHttpHandler` are instance-based APIs, everything else is static.

## Verify.ICSharpCode.Decompiler
- **NuGet package id(s)**: Verify.ICSharpCode.Decompiler (single package)
- **Current version**: 3.5.0
- **Target frameworks**: net48;net8.0
- **One-line description**: Extends Verify to allow verification of assemblies, types, methods and properties as decompiled IL via ICSharpCode.Decompiler.
- **Tech tags**: il, decompiler, ilspy, assembly, reflection, metadata, cil, source-generators
- **Third-party dependencies**: ICSharpCode.Decompiler 11.0.0.9375; System.Reflection.Metadata 10.0.12; Verify 33.1.0; ProjectDefaults 1.0.181 (private)
- **Initialize API**: `VerifyICSharpCodeDecompiler.Initialize()` - no parameters. Registers file converters for `TypeToDisassemble`, `MethodToDisassemble`, `PropertyToDisassemble`, `AssemblyToDisassemble` and `PEFile`, and calls `FileExtensions.AddTextExtension("il")` so `.il` is treated as text. Note: unlike most plugins it does NOT call `InnerVerifier.ThrowIfVerifyHasBeenRun()`.
- **Requires InitializePlugins-compatible pattern?** yes (`Initialized` property + `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyICSharpCodeDecompiler.Initialize();
```
```cs
[Test]
public async Task TypeNameUsage()
{
    using var file = new PEFile(assemblyPath);
    await Verify(new TypeToDisassemble(file, "Target"));
}
```
- **Verbose / edge-case APIs**:
  - `TypeToDisassemble(PEFile file, TypeDefinitionHandle type)` and `TypeToDisassemble(PEFile file, string typeName)` - verify a single type; the handle form is used with a hand-picked `TypeDefinitionHandle`:
```cs
[Test]
public async Task TypeDefinitionUsage()
{
    using var file = new PEFile(assemblyPath);
    var type = file.Metadata.TypeDefinitions
        .Single(
            _ =>
            {
                var fullName = _.GetFullTypeName(file.Metadata);
                return fullName.Name == "Target";
            });
    await Verify(new TypeToDisassemble(file, type));
}
```
  - `MethodToDisassemble(PEFile file, MethodDefinitionHandle method)` and `MethodToDisassemble(PEFile file, string typeName, string methodName, Func<IMethod,bool>? predicate = null)`:
```cs
await Verify(
    new MethodToDisassemble(
        file,
        "Target",
        "OnPropertyChanged"));
```
  - `PropertyToDisassemble(PEFile file, PropertyDefinitionHandle property)`, `(PEFile file, IProperty property, PropertyParts partsToDisassemble = PropertyParts.GetterAndSetter)`, `(PEFile file, string typeName, string propertyName, PropertyParts partsToDisassemble = default)`:
```cs
await Verify(
    new PropertyToDisassemble(
        file,
        "Target",
        "Property",
        PropertyParts.GetterAndSetter));
```
  - `enum PropertyParts` `[Flags]`: `Definition = 0`, `Getter = 1`, `Setter = 2`, `GetterAndSetter = Getter | Setter`.
  - `AssemblyToDisassemble(PEFile file, AssemblyOptions options = AssemblyOptions.IncludeModuleContents)`; `enum AssemblyOptions` `[Flags]`: `None = 0`, `IncludeAssemblyReferences = 1`, `IncludeAssemblyHeader = 2`, `IncludeModuleHeader = 4`, `IncludeModuleContents = 8`, `Full = 15`. A bare `PEFile` can also be verified directly.
  - `DontNormalizeIl(this VerifySettings)` / `(this SettingsTask)` - readme: "Starting with version 3.2 the generated IL is normalized by default, to avoid failed tests only because the binary layout has changed: types and members are sorted by name; RVA adress comments are stripped. To turn of the sorting, use the `DontNormalizeIL` setting."
```cs
[Test]
public async Task BackwardCompatibility()
{
    using var file = new PEFile(assemblyPath);
    await Verify(new TypeToDisassemble(file, "Target"))
        .DontNormalizeIl();
}
```
  - `ScrubComments(this VerifySettings)` / `(this SettingsTask)` - strips IL comments (not documented in readme).
  - `ScrubBinaryData(this VerifySettings)` / `(this SettingsTask)` - strips binary blobs from the IL (not documented in readme).
  - `Extensions` helpers on `PEFile`: `FindTypeDefinition(string typeName)`, `FindType(string typeName)`, `FindProperty(string typeName, string propertyName)`, `FindPropertyInfo(string typeName, string propertyName)`, `FindMethod(string typeName, string methodName, Func<IMethod,bool>? predicate = null)`.
- **Interactions with other Verify extensions**: readme mentions none. Repo test ModuleInitializer pairs `VerifyICSharpCodeDecompiler.Initialize();` with `VerifierSettings.InitializePlugins();`.
- **MSBuild / project requirements**: consumers need `global using ICSharpCode.Decompiler;`, `ICSharpCode.Decompiler.Metadata;` and `VerifyTests.ICSharpCode.Decompiler;` (as the repo's Tests GlobalUsings.cs does). Verified files use the `.il` extension (registered as a text extension by `Initialize`). Repo has an `AssemblyToProcess` project that tests decompile.
- **Sample verified output** (`.verified.il`, truncated):
```il
.method private hidebysig 
	instance void OnPropertyChanged (
		[opt] string propertyName
	) cil managed 
{
	// Header size: 1
	// Code size: 26 (0x1a)
	.maxstack 8

	IL_0000: ldarg.0
	IL_0001: ldfld class ...PropertyChangedEventHandler Target::PropertyChanged
	IL_0019: ret
} // end of method Target::OnPropertyChanged
```
- **Notes for the wizard**: output is `.il` text, not `.txt`. IL is normalized (members sorted by name, RVA comments stripped) by default since 3.2 - switching `DontNormalizeIl()` on or off will re-order existing verified files. Targets only net48 and net8.0 (no net9/net10 TFM).

## Verify.ImageHash
- **NuGet package id(s)**: Verify.ImageHash (single package)
- **Current version**: 2.1.3
- **Target frameworks**: net8.0 (single TFM; `<SignAssembly>False</SignAssembly>`)
- **One-line description**: Extends Verify to allow fuzzy comparison of images (png/jpg/bmp) via perceptual hashing using coenm/ImageHash.
- **Tech tags**: image, image-comparison, perceptual-hash, imagehash, imagesharp, png, jpg, bmp, screenshot
- **Third-party dependencies**: CoenM.ImageSharp.ImageHash 1.3.6; SixLabors.ImageSharp 4.1.2; Verify 33.1.0; Polyfill 11.4.0 (private); ProjectDefaults 1.0.181 (private)
- **Initialize API**: `VerifyImageHash.Initialize()` - no parameters; documented as "Helper method that calls `RegisterComparers`(threshold = 95, new DifferenceHash()) for png, bmp, and jpg."
- **Requires InitializePlugins-compatible pattern?** yes (`Initialized` property + `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyImageHash.Initialize();
```
```cs
[Test]
public Task CompareImage() =>
    VerifyFile("sample.jpg");
```
- **Verbose / edge-case APIs**:
  - `VerifyImageHash.RegisterComparers(double threshold = 95, IImageHash? algorithm = null)` - registers comparers for png, bmp and jpg. Readme: "All comparers can be registered: `VerifyImageHash.RegisterComparers();`"
  - `VerifyImageHash.RegisterComparer(double threshold, IImageHash? algorithm, string extension)` - register for one extension.
  - `UseImageHash(this VerifySettings, double threshold = 95, IImageHash? algorithm = null)` / `(this SettingsTask, ...)` - per-test threshold and algorithm:
```cs
[Test]
public Task CompareImageThreshold() =>
    VerifyFile("sample.jpg")
        .UseImageHash(threshold: 85);
```
```cs
[Test]
public Task CompareImageAlgorithm() =>
    VerifyFile("sample.jpg")
        .UseImageHash(algorithm: new PerceptualHash());
```
  - On mismatch the failure message suggests the fix: "Globally: VerifyImageHash.RegisterComparers({similarity});" / "For one test: Verifier.VerifyFile(\"file.jpg\").UseImageHash({similarity});"
- **Interactions with other Verify extensions**: readme says only "Contains [comparers](https://github.com/VerifyTests/Verify/blob/master/docs/comparer.md) for png, jpg, and bmp." It is a direct ALTERNATIVE to Verify.ImageMagick's comparers and to Verify.ImageSharp.Compare - all three register stream comparers for the same extensions (png/jpg/bmp), so enabling more than one means last-registration-wins. Confusingly, Verify.ImageSharp.Compare exposes an identically named `UseImageHash(...)` extension.
- **MSBuild / project requirements**: none special; net8.0 only, assembly not strong-name signed.
- **Sample verified output**: not found (readme shows no verified text; the verified artifacts are the image files themselves).
- **Notes for the wizard**: comparer-only plugin - it does not convert or render anything, it only changes how existing image verified files are compared. Threshold is a *similarity* percentage (higher = stricter, default 95); algorithm defaults to `DifferenceHash`, `PerceptualHash` is the documented alternative (types from `CoenM.ImageHash.HashAlgorithms`).

## Verify.ImageMagick
- **NuGet package id(s)**: Verify.ImageMagick (single package)
- **Current version**: 3.10.0
- **Target frameworks**: net48;net6.0;net7.0;net8.0;net9.0;net10.0
- **One-line description**: Extends Verify to convert documents (pdf, svg) to png and to compare images via Magick.NET/ImageMagick.
- **Tech tags**: imagemagick, magick.net, image, image-comparison, pdf, svg, png, webp, tiff, document, ghostscript
- **Third-party dependencies**: Magick.NET-Q16-AnyCPU 14.17.1; DeterministicPdf 2.0.2; Verify 33.1.0; Polyfill 11.4.0 (private); ProjectDefaults 1.0.181 (private)
- **Initialize API**: `VerifyImageMagick.Initialize()` - no parameters. Registers stream converters for `svg` (emits both `.svg` and a rendered `.png`), `png`, `webp`, `tiff`, and calls `RegisterPdfToPngConverter()`. Readme: "`Initialize` registers the pdf to png converter and all comparers."
- **Requires InitializePlugins-compatible pattern?** yes (`Initialized` property + `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Init()
{
    VerifyImageMagick.Initialize();
    VerifyImageMagick.RegisterComparers(threshold: 0.5);
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
    var stream = new MemoryStream(File.ReadAllBytes("sample.pdf"));
    return Verify(stream, "pdf");
}
```
- **Verbose / edge-case APIs**:
  - `VerifyImageMagick.RegisterPdfToPngConverter()` - readme: "To register only the pdf to png converter".
  - `VerifyImageMagick.RegisterComparers(double threshold = .005, ErrorMetric metric = ErrorMetric.Fuzz)` - registers stream comparers for png, jpg, bmp, tiff, webp plus a *string* comparer for svg.
  - `VerifyImageMagick.RegisterComparer(double threshold, ErrorMetric metric, string extension)` - one extension.
  - `ImageMagickComparer(this VerifySettings, double threshold = .005, ErrorMetric metric = ErrorMetric.Fuzz)` / `(this SettingsTask, ...)` - per-test comparer over `["png","jpg","bmp","tiff","webp"]`.
  - `ImageMagickBackground(this VerifySettings, MagickColor)` / `(this SettingsTask, MagickColor)` and the global `ImageMagickSettings.ImageMagickBackground(MagickColor color)` - readme "Override transparent background":
```cs
[Test]
public Task BackgroundColor() =>
    VerifyFile("transparent.png")
        .ImageMagickBackground(MagickColors.Blue);
```
  - `ImageMagickPdfPassword(this VerifySettings, string)` / `(this SettingsTask, string)` and global `ImageMagickSettings.ImageMagickPdfPassword(string password)` - readme "Open password-protected PDFs":
```cs
[Test]
public Task PdfPassword() =>
    VerifyFile("password.pdf")
        .ImageMagickPdfPassword("password");
```
  - `PagesToInclude(this VerifySettings, int count)` / `(this SettingsTask, int count)` - limit how many pdf pages are rendered (not in readme).
  - `MagickReadSettings(this VerifySettings, MagickReadSettings)` / `(this SettingsTask, MagickReadSettings)` - full control of the Magick read settings (not in readme).
  - `SkipPdfNormalization(this VerifySettings)` / `(this SettingsTask)` - xmldoc: "Snapshots the pdf bytes exactly as produced, skipping the normalization that neutralizes the trailer /ID, the /CreationDate and /ModDate, and the XMP dates and identifiers." Only safe when the producer is byte-deterministic; toggling it shifts existing `.verified.pdf` files once.
- **Interactions with other Verify extensions**: readme mentions no other Verify.* package, only "Contains [comparers](https://github.com/VerifyTests/Verify/blob/master/docs/comparer.md) for png, jpg, bmp, and tiff." It is an ALTERNATIVE image comparer to Verify.ImageHash / Verify.ImageSharp.Compare, and its converters overlap with Verify.ImageSharp's (both register stream converters for png/tiff etc.) - do not initialize both without deciding which wins. Verify.HeadlessBrowsers' own tests combine it: `VerifyImageMagick.RegisterComparers(.12);` before `VerifierSettings.InitializePlugins();`. The repo's tests also show that a later registration replaces the pdf converter: "// must run after Init, since it replaces the pdf converter registered by Initialize".
- **MSBuild / project requirements**: pdf rendering shells out to **Ghostscript** (gswin64c.exe on Windows, gs elsewhere); without it, conversion fails with exit code 127 - the repo's CI does `choco install ghostscript.app`. Consumers typically need `global using ImageMagick;` and `global using VerifyTestsImageMagick;` (the settings extensions live in the `VerifyTestsImageMagick` namespace, not `VerifyTests`). Magick.NET-Q16-AnyCPU carries native binaries.
- **Sample verified output**: not found as text - a pdf verifies as `Samples.VerifyPdf#00.verified.png` (one png per page); svg verifies as both `.svg` and `.png`.
- **Notes for the wizard**: broadest TFM coverage in this batch (net48 through net10.0). Two namespaces matter: `VerifyTests.VerifyImageMagick` (initialize/comparers) and `VerifyTestsImageMagick.ImageMagickSettings` (per-test settings). Needs Ghostscript for pdf. `ErrorMetric` comes from Magick.NET; default threshold `.005` with `ErrorMetric.Fuzz`.

## Verify.ImageSharp
- **NuGet package id(s)**: Verify.ImageSharp (single package)
- **Current version**: 5.0.1
- **Target frameworks**: net8.0;net9.0;net10.0
- **One-line description**: Extends Verify to verify images via SixLabors.ImageSharp - emits an info `.txt` plus the image file, with optional SSIM-based comparison.
- **Tech tags**: imagesharp, sixlabors, image, png, jpg, bmp, gif, tiff, ssim, image-comparison
- **Third-party dependencies**: SixLabors.ImageSharp 4.1.2; Verify 33.1.0; ProjectDefaults 1.0.181 (private)
- **Initialize API**: `VerifyImageSharp.Initialize(double ssimThreshold = 1.0)` - registers stream converters for `bmp`, `gif`, `jpg`, `png`, `tif` and a file converter for `SixLabors.ImageSharp.Image`. When `ssimThreshold < 1.0` it also registers SSIM stream comparers for all five extensions (per-test override read from context key `ImageSharpSsimThreshold`).
- **Requires InitializePlugins-compatible pattern?** yes (`Initialized` property + `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyImageSharp.Initialize();
```
```cs
[Test]
public Task VerifyImageFile() =>
    VerifyFile("sample.jpg");
```
```cs
[Test]
public Task VerifyImage()
{
    var image = new Image<Rgba32>(11, 11)
    {
        [5, 5] = Color.ParseHex("#0000FF").ToPixel<Rgba32>()
    };
    return Verify(image);
}
```
- **Verbose / edge-case APIs**:
  - SSIM comparison, readme: "By default, image comparison is byte-exact. To tolerate minor rendering differences (anti-aliasing, font hinting, subpixel rendering), enable SSIM (Structural Similarity Index) comparison by passing a threshold to `Initialize`":
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyImageSharp.Initialize(ssimThreshold: 0.999);
```
    Recommended thresholds from the readme: `0.999` tolerates anti-aliasing and subpixel rendering differences; `0.995` tolerates minor font/layout shifts across OS versions; `0.99` tolerates moderate rendering variation.
  - `SsimThreshold(this VerifySettings, double threshold)` / `(this SettingsTask, double threshold)` - per-test override:
```cs
[Test]
public Task VerifyImageWithSsimThreshold()
{
    var image = new Image<Rgba32>(11, 11)
    {
        [5, 5] = Color.ParseHex("#0000FF").ToPixel<Rgba32>()
    };
    return Verify(image)
        .SsimThreshold(0.95);
}
```
  - `SsimComparer.Calculate(Stream received, Stream verified)` returns the raw SSIM value: ```double ssim = SsimComparer.Calculate(receivedStream, verifiedStream);```
  - Encoder overrides, each with a `VerifySettings` and a `SettingsTask` overload and an optional encoder instance: `EncodeAsPng(PngEncoder? encoder = null)`, `EncodeAsJpeg(JpegEncoder? encoder = null)`, `EncodeAsGif(GifEncoder? encoder = null)`, `EncodeAsBmp(BmpEncoder? encoder = null)`, `EncodeAsTiff(TiffEncoder? encoder = null)`:
```cs
[Test]
public Task VerifyImageFileWithCustomEncoder() =>
    VerifyFile("sample.jpg")
        .EncodeAsPng();
```
  - Readme: "Two files are produced" - an info `.txt` and the image file.
- **Interactions with other Verify extensions**: readme mentions none. In practice its stream converters for png/tiff/etc. overlap with Verify.ImageMagick's, and its SSIM comparers compete with the comparers from Verify.ImageHash / Verify.ImageSharp.Compare / Verify.ImageMagick - treat them as alternatives.
- **MSBuild / project requirements**: none special. Repo has a `claude.md` noting projects "target .NET 8.0 and require .NET SDK 10.0.200 (preview, see `src/global.json`)" (the shipped csproj actually multi-targets net8.0;net9.0;net10.0).
- **Sample verified output** (the info file, alongside `Samples.VerifyImageFile.verified.jpg`):
```txt
{
  Width: 1599,
  Height: 1066,
  HorizontalResolution: 1.0,
  VerticalResolution: 1.0
}
```
- **Notes for the wizard**: the only image plugin here that both converts AND (optionally) compares. SSIM comparers are registered only when `ssimThreshold < 1.0` is passed to `Initialize` - a per-test `SsimThreshold()` has no effect unless SSIM was enabled globally. Test ModuleInitializer in the repo is named `ModuleInit` and calls only `VerifyImageSharp.Initialize();` (no `InitializePlugins`).

## Verify.ImageSharp.Compare
- **NuGet package id(s)**: Verify.ImageSharp.Compare (single package)
- **Current version**: 3.0.3
- **Target frameworks**: net8.0 (single TFM)
- **One-line description**: Extends Verify to allow tolerance-based comparison of images (png/jpg/bmp) via Codeuctivity.ImageSharp.Compare.
- **Tech tags**: imagesharp, image-comparison, pixel-diff, codeuctivity, png, jpg, bmp
- **Third-party dependencies**: Codeuctivity.ImageSharpCompare 4.1.390; SixLabors.ImageSharp 4.1.2; Verify 33.1.0; ProjectDefaults 1.0.181 (private)
- **Initialize API**: `VerifyImageSharpCompare.Initialize()` - no parameters; xmldoc says "Helper method that calls `RegisterComparers`(threshold = 95, new DifferenceHash()) for png, bmp, and jpg" but the code actually calls `RegisterComparers()` whose default is `int threshold = 5` (absolute error).
- **Requires InitializePlugins-compatible pattern?** yes (`Initialized` property + `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyImageSharpCompare.Initialize();
```
```cs
[Test]
public Task CompareImage() =>
    VerifyFile("sample1.jpg");
```
- **Verbose / edge-case APIs**:
  - `VerifyImageSharpCompare.RegisterComparers(int threshold = 5)` - readme: "All comparers can be registered: `VerifyImageSharpCompare.RegisterComparers();`". Registers png, bmp, jpg; internally de-duplicates by extension.
  - `VerifyImageSharpCompare.RegisterComparer(int threshold, string extension)`.
  - `UseImageHash(this VerifySettings, int threshold = 5)` / `(this SettingsTask, int threshold = 5)` - per-test threshold (note the method name says ImageHash even though this package uses ImageSharpCompare; not shown in the readme).
  - Mismatch message: "similarity({absoluteError}) > threshold({threshold})" plus "Globally: VerifyImageSharpCompare.RegisterComparers({absoluteError});" / "For one test: Verifier.VerifyFile(\"file.jpg\").UseImageHash({absoluteError});"
  - Repo has a second test project `Tests.CustomThreshold` demonstrating a non-default threshold in the ModuleInitializer:
```cs
[ModuleInitializer]
public static void Init()
{
    VerifyImageSharpCompare.RegisterComparers(threshold: 10);
    VerifierSettings.InitializePlugins();
}
```
- **Interactions with other Verify extensions**: readme says "Contains [comparers](https://github.com/VerifyTests/Verify/blob/master/docs/comparer.md) for png, jpg, and bmp." and (copy/paste artefact) "The following will use ImageHash to compare the images instead of the default DifferenceHash algorithm." It is an ALTERNATIVE to Verify.ImageHash and to Verify.ImageMagick's comparers - all register comparers for the same extensions.
- **MSBuild / project requirements**: none special; net8.0 only.
- **Sample verified output**: not found (readme shows no verified text; verified artifacts are the image files).
- **Notes for the wizard**: threshold here is an ABSOLUTE ERROR (lower = stricter, default 5), the inverse sense of Verify.ImageHash's similarity threshold (higher = stricter, default 95). Readme and xmldoc contain stale copy/paste from Verify.ImageHash - do not repeat the "DifferenceHash" wording as fact.

## Verify.MailMessage
- **NuGet package id(s)**: Verify.MailMessage (single package)
- **Current version**: 1.1.1
- **Target frameworks**: net48;net8.0
- **One-line description**: Extends Verify to allow verification of `System.Net.Mail` types - `MailMessage`, `Attachment`, `AlternateView`, `LinkedResource`, `ContentType`, `ContentDisposition`, `MailAddress`.
- **Tech tags**: email, mail, mailmessage, smtp, attachments, system.net.mail
- **Third-party dependencies**: none beyond Verify 33.1.0 (the mail types are BCL); Polyfill 11.4.0 (private); ProjectDefaults 1.0.181 (private)
- **Initialize API**: `VerifyMailMessage.Initialize()` - no parameters. Adds seven converters (`ContentDispositionConverter`, `ContentTypeConverter`, `AlternateViewConverter`, `AddressConverter`, `AttachmentConverter`, `LinkedResourceConverter`, `MessageConverter`) and registers file converters for `MailMessage`, `Attachment`, `AlternateView` and `LinkedResource`.
- **Requires InitializePlugins-compatible pattern?** yes (`Initialized` property + `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Initialize() =>
    VerifyMailMessage.Initialize();
```
```cs
[Fact]
public Task MailMessage()
{
    var mail = new MailMessage(
        from: "from@mail.com",
        to: "to@mail.com",
        subject: "The subject",
        body: "The body");
    return Verify(mail);
}
```
- **Verbose / edge-case APIs**: no settings extensions or scrubbers. The readme documents one section per verifiable type:
  - `ContentDisposition`:
```cs
[Fact]
public Task ContentDisposition()
{
    var content = new ContentDisposition("attachment; filename=\"filename.jpg\"");
    return Verify(content);
}
```
  - `ContentType`:
```cs
[Fact]
public Task ContentType()
{
    var content = new ContentType("text/html; charset=utf-8")
    {
        Name = "name.txt"
    };
    return Verify(content);
}
```
  - `Attachment` (stream + content type):
```cs
[Fact]
public Task Attachment()
{
    var attachment = new Attachment(
        new MemoryStream("file content"u8.ToArray()),
        new ContentType("text/html; charset=utf-8"))
    {
        Name = "name.txt"
    };
    return Verify(attachment);
}
```
- **Interactions with other Verify extensions**: readme mentions none. Test ModuleInitializer pairs `VerifyMailMessage.Initialize();` with `VerifierSettings.InitializePlugins();`.
- **MSBuild / project requirements**: none special. Tests use xunit.v3 / Verify.XunitV3.
- **Sample verified output**:
```txt
{
  From: from@mail.com,
  To: to@mail.com,
  Subject: The subject,
  IsBodyHtml: false,
  Body: The body
}
```
- **Notes for the wizard**: registering file converters means attachments/views can be split out into separate verified files. `ContentId` values surface as scrubbed `Guid_1`. Package tags in Directory.Build.props are (incorrectly) `Http, Verify`.

## Verify.MassTransit
- **NuGet package id(s)**: Verify.MassTransit (single package)
- **Current version**: 2.3.0
- **Target frameworks**: net8.0;net9.0;net10.0
- **One-line description**: Adds Verify support for MassTransit test helpers - verify an `InMemoryTestHarness`, consumer harnesses and saga harnesses instead of hand-written asserts.
- **Tech tags**: masstransit, messaging, message-bus, saga, consumer, test-harness, event-driven
- **Third-party dependencies**: MassTransit 9.2.2; Verify 33.1.0; ProjectDefaults 1.0.181 (private)
- **Initialize API**: `VerifyMassTransit.Initialize()` - no parameters. Adds 18 converters covering test harnesses (`TestHarnessConverter`, `ConsumerTestHarnessConverter`), message lists (`Received`/`Sent`/`Published` single + list converters), saga state (`SagaStateMachineTestHarnessConverter`, `SagaTestHarnessConverter`, `SagaListConverter`, `ReceivedEventConverter(+List)`, `StateChangeConverter(+List)`) and contexts (`MessageContextConverter`, `SendContextConverter`, `ReceiveContextConverter`).
- **Requires InitializePlugins-compatible pattern?** yes (`Initialized` property + `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Initialize() =>
    VerifyMassTransit.Initialize();
```
```cs
[Fact]
public async Task TestWithVerify()
{
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

        await Verify(new
        {
            harness,
            consumer
        });
    }
    finally
    {
        await harness.Stop();
    }
}
```
- **Verbose / edge-case APIs**: no settings extensions, scrubbers or recording APIs. Readme documents one more scenario - saga harnesses, where `harness.Saga<ConsumerSaga>()` is verified alongside the harness:
```cs
[Fact]
public async Task Run()
{
    using var harness = new InMemoryTestHarness();
    var sagaHarness = harness.Saga<ConsumerSaga>();

    var correlationId = NewId.NextGuid();

    await harness.Start();
    try
    {
        await harness.Bus.Publish(new Start {CorrelationId = correlationId});

        await harness.Consumed.Any<Start>();

        await Verify(new {harness, sagaHarness});
    }
    finally
    {
        await harness.Stop();
    }
}
```
  Readme also contrasts the Verify approach with the traditional assert style (`Assert.True(await harness.Consumed.Any<SubmitOrder>())` etc.) - useful as migration copy.
- **Interactions with other Verify extensions**: readme mentions none. Test ModuleInitializer pairs `VerifyMassTransit.Initialize();` with `VerifierSettings.InitializePlugins();`.
- **MSBuild / project requirements**: none special (in-memory harness, no broker needed). Consumers need `using MassTransit;` and `using MassTransit.Testing;`. Repo test project also references Microsoft.Extensions.DependencyInjection 10.0.12 and xunit.v3.
- **Sample verified output** (truncated):
```txt
{
  harness: {
    Messages: [
      {
        Sent: ConsumerTests.SubmitOrder,
        MessageId: Guid_1,
        ConversationId: Guid_2,
        DestinationAddress: input_queue,
        Message: {
          OrderId: Guid_3
        }
      },
      {
        Published: ConsumerTests.OrderSubmitted,
        MessageId: Guid_4,
        Message: {
          OrderId: Guid_3
        }
      }
    ]
  },
  consumer: {
    Consumed: [ ... ]
  }
}
```
- **Notes for the wizard**: readme links to the MassTransit testing docs (masstransit-project.com/usage/testing.html). Guids are auto-scrubbed to `Guid_N` by Verify core. Repo has no code_of_conduct.md. A `TestResults` folder exists in src (build artefact).

## Verify.MicrosoftLogging
- **NuGet package id(s)**: Verify.MicrosoftLogging (single package)
- **Current version**: 5.0.0
- **Target frameworks**: net48;net8.0;net9.0;net10.0
- **One-line description**: Extends Verify to record `Microsoft.Extensions.Logging` output during a test and append it to the verified file.
- **Tech tags**: logging, microsoft.extensions.logging, ilogger, recording, diagnostics
- **Third-party dependencies**: Microsoft.Extensions.Logging.Abstractions 10.0.12; Verify 33.1.0; Polyfill 11.4.0 (private); ProjectDefaults 1.0.181 (private)
- **Initialize API**: `VerifyMicrosoftLogging.Initialize()` - no parameters. Adds `LogItemConverter` via `VerifierSettings.AddExtraSettings`.
- **Requires InitializePlugins-compatible pattern?** yes (`Initialized` property + `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Initialize() =>
    VerifyMicrosoftLogging.Initialize();
```
```cs
[Fact]
public Task Logging()
{
    Recording.Start();
    var logger = new RecordingLogger();
    var target = new ClassThatUsesLogging(logger);

    var result = target.Method();

    return Verify(result);
}
```
- **Verbose / edge-case APIs**:
  - `RecordingLogger(string? category = null)` - implements `ILogger`; pass it to the class under test. Supports scopes via `BeginScope` (rendered as `StartScope`/`EndScope` entries).
  - `RecordingProvider` - implements `ILoggerProvider`; `RecordingProvider.CreateLogger<T>()` (static) builds a typed `ILogger<T>`, and the instance `CreateLogger(string category)` satisfies `ILoggerProvider`. Readme "Typed" section:
```cs
[Fact]
public Task LoggingTyped()
{
    Recording.Start();
    var logger = RecordingProvider.CreateLogger<ClassThatUsesTypedLogging>();
    var target = new ClassThatUsesTypedLogging(logger);

    var result = target.Method();

    return Verify(result);
}
```
  - `LogItem` record - the serialized shape of each captured entry.
  - Recording is driven by Verify core's `Recording.Start()`; captured entries are appended under a `log:` node in the verified file.
  - Readme staleness warning: the prose says "Call `LoggerRecording.Start();` to get an instance of the `LoggerProvider`. `LoggerProvider` implements both `ILogger` and `ILoggerProvider`." - there is no `LoggerRecording` or `LoggerProvider` type in the current source; the real API is `Recording.Start()`, `RecordingLogger` and `RecordingProvider`. Use the snippets, not the prose.
- **Interactions with other Verify extensions**: readme mentions none, but Verify.NServiceBus takes a `PackageReference` on **Verify.MicrosoftLogging 5.0.0** and `VerifyNServiceBus.Initialize(captureLogs: true)` calls `VerifyMicrosoftLogging.Initialize()` when it has not already run.
- **MSBuild / project requirements**: none special; `<OutputType>Library</OutputType>` is set explicitly in the csproj.
- **Sample verified output**:
```txt
{
  target: result,
  log: [
    {
      Warning: The log entry
    },
    {
      Message: StartScope,
      State: The scope
    },
    {
      Warning: Entry in scope
    },
    {
      Message: EndScope
    }
  ]
}
```
- **Notes for the wizard**: this is a Recording-style plugin - nothing is captured unless `Recording.Start()` is called in the test. Typed loggers add a `Category` member to the output. Widest TFM span apart from ImageMagick/Mockly.

## Verify.Mockly
- **NuGet package id(s)**: Verify.Mockly (single package)
- **Current version**: 1.0.1
- **Target frameworks**: net472;net48;net8.0;net9.0;net10.0
- **One-line description**: Adds Verify support for verifying Mockly types - an `HttpMock`, its captured requests, and a `RequestCollection`.
- **Tech tags**: mockly, http, httpclient, mocking, http-mock, rest
- **Third-party dependencies**: Mockly 1.12.0; Verify 33.1.0; Polyfill 11.4.0 (private); ProjectDefaults 1.0.181 (private)
- **Initialize API**: `VerifyMockly.Initialize()` - no parameters. Adds three cached converter instances: `HttpMockConverter`, `CapturedRequestConverter`, `RequestCollectionConverter`.
- **Requires InitializePlugins-compatible pattern?** yes (`Initialized` property + `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyMockly.Initialize();
```
```cs
[Test]
public async Task VerifyGetRequest()
{
    var mock = new HttpMock();

    mock.ForGet()
        .ForHttps()
        .ForHost("api.example.com")
        .WithPath("/api/users/123")
        .RespondsWithJsonContent(new { id = 123, name = "John" });

    var client = mock.GetClient();
    await client.GetAsync("https://api.example.com/api/users/123");
    await Verify(mock);
}
```
- **Verbose / edge-case APIs**:
  - POST with body (readme "Verifying POST Requests with Body") - same shape, using `mock.ForPost()` + `RespondsWithStatus(System.Net.HttpStatusCode.Created)`, then `await Verify(mock);`. The captured `Body` appears in the verified file.
  - `RequestCollection` (readme "Verifying a RequestCollection") - collect matching requests and verify the collection rather than the mock:
```cs
[Test]
public async Task VerifyRequestCollection()
{
    var mock = new HttpMock();
    var requests = new RequestCollection();

    mock.ForGet()
        .ForHttps()
        .ForHost("api.example.com")
        .WithPath("/api/users/*")
        .CollectingRequestsIn(requests)
        .RespondsWithJsonContent(new { id = 1, name = "John" });

    var client = mock.GetClient();
    await client.GetAsync("https://api.example.com/api/users/1");
    await client.GetAsync("https://api.example.com/api/users/2");
    await Verify(requests);
}
```
  - Scrubbing members by name (readme "Scrubbing Members"), using Verify core's `ScrubMember`:
```cs
await Verify(mock)
    .ScrubMember("Body");
```
  - No package-specific `VerifySettings` extension methods.
- **Interactions with other Verify extensions**: readme mentions none. It covers the same ground as Verify.Http's `MockHttpClient` and as Verify.Flurl's `HttpTest` - practical alternatives for mocking http. Test ModuleInitializer pairs `VerifyMockly.Initialize();` with `VerifierSettings.InitializePlugins();`.
- **MSBuild / project requirements**: none special. Repo tests use NUnit (NUnit3TestAdapter 6.3.0) + Verify.NUnit.
- **Sample verified output**:
```txt
[
  {
    Method: POST,
    Scheme: https,
    Host: api.example.com,
    Path: /api/users,
    Body: {"name":"Jane"},
    WasExpected: true,
    StatusCode: Created
  }
]
```
- **Notes for the wizard**: newest/lowest-version package in this batch (1.0.1) and the only one targeting net472. Mockly types (`HttpMock`, `RequestCollection`, the `ForGet()/ForHttps()/ForHost()/WithPath()/RespondsWith*()` fluent chain) come from the Mockly package, not from this one. Repo has no code_of_conduct.md.

## Verify.Moq
- **NuGet package id(s)**: Verify.Moq (single package)
- **Current version**: 2.2.0
- **Target frameworks**: net48;net6.0;net7.0;net8.0;net9.0
- **One-line description**: Adds Verify support for verifying Moq types - a `Mock<T>` and its recorded invocations.
- **Tech tags**: moq, mocking, mocks, test-doubles, unit-testing
- **Third-party dependencies**: Moq 4.20.72; Verify 33.1.0; Polyfill 11.4.0 (private); ProjectDefaults 1.0.181 (private)
- **Initialize API**: `VerifyMoq.Initialize()` - no parameters. Adds `InvocationConverter` and `MockConverter`.
- **Requires InitializePlugins-compatible pattern?** yes (`Initialized` property + `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyMoq.Initialize();
```
```cs
[Test]
public Task Test()
{
    var mock = new Mock<ITarget>();

    mock.Setup(_ => _.Method(It.IsAny<int>(), It.IsAny<int>()))
        .Returns("response");

    var target = mock.Object;
    target.Method(1, 2);
    return Verify(mock);
}
```
- **Verbose / edge-case APIs**:
  - Readme "Scrubbing Arguments": "Arguments can be scrubbed by name" using Verify core's `ScrubMember`:
```cs
[Test]
public Task ScrubArguments()
{
    var mock = new Mock<ITarget>();

    mock.Setup(_ => _.Method(It.IsAny<int>(), It.IsAny<int>()))
        .Returns("response");

    var target = mock.Object;
    target.Method(1, 2);
    return Verify(mock)
        .ScrubMember("a");
}
```
  - No package-specific settings extensions.
- **Interactions with other Verify extensions**: readme mentions none. Alternative to Verify.NSubstitute / Verify.FakeItEasy. Test ModuleInitializer pairs `VerifyMoq.Initialize();` with `VerifierSettings.InitializePlugins();`.
- **MSBuild / project requirements**: none special. Repo tests use NUnit + Verify.NUnit.
- **Sample verified output**:
```txt
[
  {
    Method: ITarget.Method(int a, int b),
    Arguments: {
      Arguments: {
        a: 1,
        b: 2
      }
    },
    ReturnValue: response
  }
]
```
- **Notes for the wizard**: verifies the `Mock<T>` object itself (not a call list), and unlike NSubstitute/FakeItEasy it captures `ReturnValue` and names arguments. No net10.0 TFM. Repo has no code_of_conduct.md.

## Verify.NServiceBus
- **NuGet package id(s)**: Verify.NServiceBus (single package)
- **Current version**: 12.2.0
- **Target frameworks**: net10.0 (single TFM)
- **One-line description**: Adds Verify support for NServiceBus - recording handler/saga contexts and verifying the resulting sends, publishes, replies, forwards and subscriptions.
- **Tech tags**: nservicebus, messaging, message-bus, saga, handler, particular, event-driven
- **Third-party dependencies**: NServiceBus 10.2.9; Verify 33.1.0; Verify.MicrosoftLogging 5.0.0; Polyfill 11.4.0 (private); ProjectDefaults 1.0.181 (private)
- **Initialize API**: `VerifyNServiceBus.Initialize(bool captureLogs = true)` - when `captureLogs` is true and `VerifyMicrosoftLogging.Initialized` is false it calls `VerifyMicrosoftLogging.Initialize()`. Then adds named guids (`DefaultMessageId` -> "MessageId", `DefaultConversationId` -> "ConversationId", `DefaultCorrelationId` -> "CorrelationId"), `VerifierSettings.IgnoreInstance<ContextBag>(...)` for empty bags, and ~19 converters.
- **Requires InitializePlugins-compatible pattern?** yes (`Initialized` property + `Initialize()`; note the optional `captureLogs` parameter)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Initialize() =>
    VerifyNServiceBus.Initialize();
```
```cs
[Fact]
public async Task VerifyHandlerResult()
{
    var handler = new MyHandler();
    var context = new RecordingHandlerContext();

    var message = new MyRequest();
    await handler.Handle(message, context);

    await Verify(context);
}
```
- **Verbose / edge-case APIs**:
  - `RecordingHandlerContext(IEnumerable<KeyValuePair<string,string>>? headers = null)` - the main test double; exposes `Sent`, `Published`, `Replied`, `Forwarded`, `MessageHeaders`, `Extensions`, `MessageId`, `ReplyToAddress`, `DoNotContinueDispatchingCurrentMessageToHandlersWasCalled`, `SynchronizedStorageSession`, and static `SharedContextBag`.
  - Other recording contexts: `RecordingInvokeHandlerContext`, `RecordingIncomingPhysicalMessageContext`, `RecordingIncomingLogicalMessageContext`, `RecordingMessageSession`.
  - Recording mode (readme "#### Recording": "Recording allows all message interaction with the test context to be captured and then verified."):
```cs
[Fact]
public async Task VerifyHandlerResult()
{
    Recording.Start();
    var handler = new MyHandler();
    var context = new RecordingHandlerContext();

    var message = new MyRequest();
    await handler.Handle(message, context);

    await Verify("some other data");
}
```
  - Saga verification - verify the context and the saga together:
```cs
await Verify(new
{
    context,
    saga
});
```
  - `MessageToHandlerMap` (readme: "`MessageToHandlerMap` allows verification of message that do not have a handler."): `AddMessage(Type)`, `AddMessage<T>()`, `AddMessagesFromAssembly(Assembly)`, `AddMessagesFromAssembly<T>()`, `AddHandler(Type)`, `AddHandler<T>()`, `AddHandlersFromAssembly(Assembly)`, `AddHandlersFromAssembly<T>()`:
```cs
var map = new MessageToHandlerMap();
map.AddMessagesFromAssembly<MyMessage>();
map.AddHandlersFromAssembly<MyHandler>();
await Verify(map);
```
  - Header defaults and overrides (not in readme): `VerifyNServiceBus.AddSharedHeader(string key, string value)`, `AddSharedHeaders(IEnumerable<KeyValuePair<string,string>>)`, and the public constants `DefaultMessageIdString`/`DefaultMessageId`, `DefaultConversationIdString`/`DefaultConversationId`, `DefaultCorrelationIdString`/`DefaultCorrelationId`, `DefaultOriginatingEndpoint`, `DefaultReplyToAddress`. The repo's ModuleInitializer shows:
```cs
VerifyNServiceBus.AddSharedHeaders(
    new Dictionary<string, string>
    {
        {
            "sharedKey", "sharedValue"
        }
    });
```
- **Interactions with other Verify extensions**: readme mentions none explicitly, but the package has a hard dependency on **Verify.MicrosoftLogging 5.0.0** and `Initialize(captureLogs: true)` (the default) initializes it for you - pass `captureLogs: false` to opt out, or initialize Verify.MicrosoftLogging first and it will be left alone.
- **MSBuild / project requirements**: net10.0 only. The csproj adds implicit usings (`VerifyTests.NServiceBus`, `NServiceBus.Extensibility`, `NServiceBus.Transport`) for the package itself; consumers need `using VerifyTests.NServiceBus;` to reach `RecordingHandlerContext`/`MessageToHandlerMap`. No broker/transport needed - everything is in-memory test doubles.
- **Sample verified output**:
```txt
{
  Forward: [
    newDestination
  ],
  Publish: [
    {
      MyPublishMessage: {
        Property: Value
      }
    }
  ],
  Reply: [
    {
      MyReplyMessage: {
        Property: Value
      }
    }
  ],
  Send: [
    {
      MySendMessage: {
        Property: Value
      },
      Options: {
        DeliveryDelay: 12:00:00
      }
    }
  ]
}
```
- **Notes for the wizard**: highest version number in the batch (12.2.0) - long history, so version-specific docs matter. Two usage modes: verify the `RecordingHandlerContext` directly, or `Recording.Start()` and let interactions append to whatever is verified. Deterministic message/conversation/correlation ids are injected so snapshots are stable. Repo has no code_of_conduct.md.

## Verify.NSubstitute
- **NuGet package id(s)**: Verify.NSubstitute (single package)
- **Current version**: 2.1.0
- **Target frameworks**: net48;net8.0
- **One-line description**: Adds Verify support for verifying NSubstitute types - principally `substitute.ReceivedCalls()`.
- **Tech tags**: nsubstitute, mocking, substitutes, test-doubles, unit-testing
- **Third-party dependencies**: NSubstitute 6.2.0; Verify 33.1.0; Polyfill 11.4.0 (private); ProjectDefaults 1.0.181 (private)
- **Initialize API**: `VerifyNSubstitute.Initialize()` - no parameters. Adds a single `CallConverter`.
- **Requires InitializePlugins-compatible pattern?** yes (`Initialized` property + `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyNSubstitute.Initialize();
```
```cs
[Fact]
public Task Test()
{
    var target = Substitute.For<ITarget>();
    target.Method(1, 2);
    return Verify(target.ReceivedCalls());
}
```
- **Verbose / edge-case APIs**: none - the readme documents only `Initialize()` and `ReceivedCalls()`. No settings extensions, scrubbers, recording or file/stream overloads.
- **Interactions with other Verify extensions**: readme mentions none. Alternative to Verify.Moq / Verify.FakeItEasy. Test ModuleInitializer pairs `VerifyNSubstitute.Initialize();` with `VerifierSettings.InitializePlugins();`.
- **MSBuild / project requirements**: none special. Repo tests use xunit.v3 / Verify.XunitV3.
- **Sample verified output**:
```txt
[
  {
    Method: ITarget.Method(int a, int b),
    Arguments: [
      1,
      2
    ]
  }
]
```
- **Notes for the wizard**: smallest package in the batch (two source files). Output is identical in shape to Verify.FakeItEasy. Repo has no code_of_conduct.md.

## Verify.NUlid
- **NuGet package id(s)**: Verify.NUlid (single package)
- **Current version**: 1.0.1
- **Target frameworks**: net48;net8.0;net9.0
- **One-line description**: Extends Verify to scrub ULIDs (Universally Unique Lexicographically Sortable Identifiers) from snapshots, via the NUlid package.
- **Tech tags**: ulid, nulid, identifiers, scrubbing, guid-like, sortable-ids
- **Third-party dependencies**: NUlid 1.7.3; Verify 33.1.0; Polyfill 11.4.0 (private); ProjectDefaults 1.0.181 (private)
- **Initialize API**: `VerifyNUlid.Initialize()` - no parameters. Readme: "Call `VerifyNUlid.Initialize()` once at assembly load time." Initializes the ULID counter context, registers `VerifierSettings.ScrubWindow(26, 26, ScrubInline, requireWordBoundary: true)` (26 = ULID length) and adds a `UlidConverter`.
- **Requires InitializePlugins-compatible pattern?** yes (`Initialized` property + `Initialize()`)
- **Minimal usage**:
```cs
[ModuleInitializer]
public static void Init() =>
    VerifyNUlid.Initialize();
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
  - `DontScrubUlids(this VerifySettings)` and `DontScrubUlids(this SettingsTask)` - readme "Disabling Scrubbing": "To disable scrubbing use `DontScrubUlids()`". Both fluent and instance-settings forms:
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
  - Scrubbing is both member-level (via `UlidConverter`) and inline in strings (via the 26-char scrub window with word-boundary requirement), so `Sarah (01JGX...)` becomes `Sarah (Ulid_1)`.
- **Interactions with other Verify extensions**: readme mentions none. Repo test ModuleInitializer additionally calls `VerifyDiffPlex.Initialize(OutputType.Compact);` and `VerifierSettings.InitializePlugins();` (test-only).
- **MSBuild / project requirements**: none special. Repo tests use NUnit + Verify.NUnit.
- **Sample verified output**:
```txt
{
  Id: Ulid_1,
  Name: Sarah,
  Description: Sarah (Ulid_1)
}
```
- **Notes for the wizard**: pure-scrubbing plugin (closest analogue to Verify's built-in Guid scrubbing); nothing is converted or compared. Named counters are per-context, so the same ULID always maps to the same `Ulid_N` within a test. Readme has no `## Sponsors` badge block variations worth copying; its NuGet heading is "## NuGet package" (singular) unlike the other repos.
