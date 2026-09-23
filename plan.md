# Verify Wizard – implementation plan

A Blazor WebAssembly app, deployed to GitHub Pages from this repository (`https://github.com/VerifyTests/Wizard`), that replaces the static markdown wizard currently generated into `Verify/docs/wiz` and extends it with extension selection, tech-stack seeding, per-extension verbosity, a downloadable zipped solution, and AI-oriented markdown.

This document is written for an implementer who has not seen the conversation that produced it. It is deliberately verbose. Every decision that was not explicitly specified by the requester is listed in [Decisions and assumptions](#2-decisions-and-assumptions) so it can be overridden before or during implementation.

Companion research files (raw per-extension catalogue produced while researching this plan) live in `plan-research/`. They are referenced throughout as "the catalogue". Read them before implementing the extension registry.

---

## Table of contents

1. [What exists today](#1-what-exists-today)
2. [Decisions and assumptions](#2-decisions-and-assumptions)
3. [Requirements checklist](#3-requirements-checklist)
4. [Repository layout](#4-repository-layout)
5. [Build, SDK and target framework](#5-build-sdk-and-target-framework)
6. [Domain model](#6-domain-model)
7. [Flows, steps and navigation](#7-flows-steps-and-navigation)
8. [URL state and browser persistence](#8-url-state-and-browser-persistence)
9. [Extension registry](#9-extension-registry)
10. [Tech stack to extension suggestions](#10-tech-stack-to-extension-suggestions)
11. [Interaction rules](#11-interaction-rules)
12. [Generated outputs](#12-generated-outputs)
13. [Test framework specifics](#13-test-framework-specifics)
14. [Sponsor step](#14-sponsor-step)
15. [NuGet version sourcing](#15-nuget-version-sourcing)
16. [Web app implementation](#16-web-app-implementation)
17. [Tests](#17-tests)
18. [CI and deployment](#18-ci-and-deployment)
19. [Changes in the Verify repository](#19-changes-in-the-verify-repository)
20. [Implementation phases](#20-implementation-phases)
21. [Risks and open questions](#21-risks-and-open-questions)
22. [Suggested changes to Verify and its extensions](#22-suggested-changes-to-verify-and-its-extensions)
- [Appendix A: static content to copy from Verify docs](#appendix-a-static-content-to-copy-from-verify-docs)
- [Appendix B: extension registry table](#appendix-b-extension-registry-table)
- [Appendix C: current wizard content, per section](#appendix-c-current-wizard-content-per-section)
- [Appendix D: upstream defects the generated code works around](#appendix-d-upstream-defects-the-generated-code-works-around)

---

## 1. What exists today

### 1.1 The current wizard

- Generator: `D:\Code\VerifyTests\Verify\src\Verify.Tests\Wizard\WizardGen.cs` (an xUnit test compiled only for `net8.0`, run only when `mdsnippets` is on `PATH`). Enums: `Os` (Windows, MacOS, Linux), `Ide` (VisualStudio, VisualStudioWithReSharper, Rider, Other), `CliPreference` (Cli, Gui), `TestFramework` (XunitV3, NUnit, TUnit, Fixie, MSTest, Expecto), `BuildServer` (AppVeyor, GitHubActions, AzureDevOps, None).
- Output: 508 static markdown files in `Verify/docs/wiz/`, one per path prefix (`Windows.md`, `Windows_Rider.md`, `Windows_Rider_Cli.md`, ... `Windows_Rider_Cli_XunitV3_GitHubActions.md`). The terminal page contains, in order: NuGet packages, Microsoft.Testing.Platform, Implicit usings, Conventions (gitignore/gitattributes/editorconfig/`VerifyChecks.Run()`), DiffEngineTray (Windows), Rider plugin or ReSharper plugin, DiffPlex, Verify.Terminal (CLI), Sample test, MSTest marker / Fixie convention, Diff tools for the OS, Build server artifact upload.
- The IDE list depends on OS (Windows: all four; MacOS/Linux: Rider, Other). A breadcrumb `[Home] > [Windows] > [Rider] > ...` is rendered at the top of each page.
- Content is stitched from `docs/mdsource/*.include.md` includes and `snippet:` references resolved by MarkdownSnippets. Appendix C lists every piece of content with its source so it can be ported.
- The Verify readme links to `/docs/wiz/readme.md` under "Getting started wizard".

### 1.2 The plumbing to reuse: SponsorCheck.Web

`D:\Code\SponsorCheck\src\SponsorCheck.Web` is a Blazor WASM wizard deployed to `https://simoncropp.github.io/SponsorCheck/` by `.github/workflows/deploy-blazor.yml`. Reuse (copy and adapt) the following; each item is called out again where it is used:

| SponsorCheck file | What to reuse |
|---|---|
| `SponsorCheck.Web.csproj` | `Microsoft.NET.Sdk.BlazorWebAssembly`, `PublishTrimmed`, `TrimMode=full`, `InvariantGlobalization`, `ILLink.Descriptors.xml` root descriptor, the `GenerateWizardDefaults` target that bakes `$(Version)` into a generated `WizardDefaults.g.cs` |
| `Program.cs` | host builder, scoped `HttpClient` with 60 s timeout, scoped services |
| `wwwroot/index.html`, `wwwroot/css/app.css`, `wwwroot/fonts/*` | loading spinner, bundled woff2 fonts with `unicode-range`, `text-rendering: geometricPrecision`, monospace pinning; these make Playwright PNG snapshots identical across OSes |
| `wwwroot/js/interop.js` + `Services/ClipboardService.cs` | clipboard copy with fallback |
| `Components/Stepper.razor` | step navigation model (rewritten as a left breadcrumb rail, see 7.4) |
| `Components/CodeBox.razor`, `CopyMarkdownButton.razor` | code display with copy |
| `Services/PackageLookup.cs` | nuget.org flat-container lookup from the browser (CORS works, `BrowserRequestCache.NoStore`), used for version refresh |
| `Services/MsBuildXml.cs` | MSBuild XML emitters |
| `Services/ConsumerConfigGenerator.cs`, `Models/MonthBound.cs` | owner-mode sponsorship snippet rules and month arithmetic for the sponsor step |
| `WizardLinks.cs`, `DocLinks.cs` | canonical URL constants pattern |
| `SponsorCheck.Web.Tests/*` | bunit `WebTestContext`, `PublishedWizard` (Kestrel host serving the published output + Chromium via Playwright, `{*path}` fallback), `ScreenSnapshotTests`, `EndToEndTests`, `RepoContractTests` (anti-rot), `FakeNuGetFeed`/`StubNuGetHandler`, `ModuleInitializer` (AngleSharp pretty print, Blazor marker scrubbing, `UseSsimForPng(.7)`) |
| `.github/workflows/deploy-blazor.yml` | build, test, publish, `<base href>` rewrite, `.nojekyll`, `404.html`, Pages deploy |

### 1.3 Extensions

66 `Verify.*` folders exist under `D:\Code\VerifyTests\`, plus `D:\Code\LocalDb`. All were read (readme, `src/Directory.Build.props`, `src/Directory.Packages.props`, shipped csproj files, the `VerifyXxx` static class, test `ModuleInitializer`). The per-extension findings are in `plan-research/extension-catalogue-*.md`. Highlights that shape the design:

- Most extensions expose `public static bool Initialized` + `public static void Initialize()`, so `VerifierSettings.InitializePlugins()` discovers them. Exceptions: **Verify.Blazor** has no public `Initialize` (auto-initialized by `Render`'s static constructor, which throws once any verification has run; see 9.1); **Verify.Wolverine** and **Verify.ParametersHashing** have a no-op `Initialize`; **Verify.Terminal** is a dotnet tool (`verify.tool`), not a library; **LocalDb** integrates through `EfLocalDb.<TestFramework>` packages, not a `Verify.*` package.
- `InitializePlugins()` does not find every extension.
  - Core looks for a type named `VerifyTests.` plus the assembly name without dots. The match is case-sensitive, and an assembly without that type is skipped silently (`Verify\src\Verify\VerifierSettings_PluginConvention.cs:171-176, 202-208`).
  - As a result it never initializes **Verify.AngleSharp** (its class is `VerifyAngleSharpDiffing`), **Verify.QuestPDF** (`VerifyQuestPdf`) or **Verify.Blazor** (an internal class in the global namespace).
  - Plugins are enumerated in file-system order (`:34`), so when two plugins register for the same thing, the winner can differ between operating systems.
- Some `Initialize` methods take parameters that matter: `VerifyDiffPlex.Initialize(OutputType)`, `VerifySqlServer.Initialize(recordCommands)`, `VerifyEntityFramework.Initialize(model, recordCommands)`, `VerifyNServiceBus.Initialize(captureLogs)` (no effect in practice, see 11.2), `VerifyBunit.Initialize(excludeComponent)`, `VerifyImageSharp.Initialize(ssimThreshold)`, `VerifyPDFium.Initialize(dpi)`, `VerifyPlaywright.Initialize(installPlaywright)`, `VerifySerilog.Initialize(custom)`, `VerifyEmailPreviewServices.Initialize(apiKey)`, `VerifyAngleSharpDiffing.Initialize(action)`.
- Several groups are mutually exclusive because they register a converter or comparer for the same file extension (pdf, xlsx, docx, pptx, csv, png/jpg, html, json) or install the same global listener (Activity listeners, 26-char ULID scrub windows).
- The documented interaction the requester cited: Verify.EntityFramework and Verify.SqlServer both record every EF command (`ef` and `sql`), and `VerifySqlServer.Initialize(recordCommands: false)` **must** run before `VerifierSettings.InitializePlugins()`.

---

## 2. Decisions and assumptions

Numbered so they can be referenced. Each is a choice the requester did not spell out; override as needed.

- **D1. Three projects, not one.** `Wizard.Core` (models, registry, generators, zip builder; plain class library), `Wizard.Web` (Blazor WASM UI), `Wizard.Tests` (TUnit). SponsorCheck keeps everything in the Web project; splitting the generators out keeps snapshot tests fast and lets a future CLI reuse them.
- **D2. SDK and TFM come from this repository, not from the Verify repository.** The requester said the generated solution's "dotnet version and sdk should be the same used by the code for this project". Interpreted as: one source of truth, which is this repository's `global.json` (SDK) and `Wizard.Web`'s `TargetFramework` (TFM). Both are baked into `WizardDefaults.g.cs` at build time (the SponsorCheck `GenerateWizardDefaults` pattern) and emitted verbatim into generated output. Initial values: **SDK `10.0.401`** (`rollForward: latestFeature`, `allowPrerelease: true`) and **`net10.0`**. This matches SponsorCheck.Web and the consumer-facing `Verify/usages/*` projects. The Verify repository itself is on `11.0.100-rc.1`; a release-candidate SDK is not appropriate for a getting-started wizard. If the requester meant "track Verify", bump this repository's `global.json` and everything follows.
- **D3. NuGet versions: baked plus live refresh.** A `package-versions.json` in `Wizard.Core` holds the last known stable version of every package the wizard can emit. A weekly GitHub Actions workflow refreshes it from nuget.org and opens a pull request. At runtime the app also queries nuget.org (same flat-container endpoint SponsorCheck already uses from the browser) and uses the newest **non-prerelease** version when the query succeeds; the baked value is the offline fallback. "Stable" means no `-` prerelease label. If a package has no stable version (none currently), fall back to newest prerelease and say so in the output.
- **D4. Explicit `Initialize` calls, then `InitializePlugins`.** The generated `ModuleInitializer` calls each selected extension's `Initialize` explicitly, in an order computed from the interaction rules, each with a verbose comment, and then calls `VerifierSettings.InitializePlugins()` (which skips already-initialized plugins) with a comment explaining that it picks up plugins added later. Explicit calls are needed anyway for parameters and ordering; making them universal keeps the output predictable and self-documenting. They are also needed for correctness, because `InitializePlugins()` never initializes Verify.AngleSharp, Verify.QuestPDF or Verify.Blazor (1.3).
- **D5. Windows-only extensions go in a second test project.** WinForms and Xaml need `net10.0-windows` (`UseWindowsForms`/`UseWPF`), Phash targets `net8.0-windows`. Putting them in the main test project would make the whole solution Windows-only. The zip therefore contains `<Name>.Tests` (cross-platform) and, when needed, `<Name>.Tests.Windows`. Each project has its own `ModuleInitializer`.
- **D6. Verified files are not included in the zip**, except where the exact output is known: the core sample (`Sample.Test.verified.txt`, from the Verify repository) and any extension sample that sets `VerifiedOutput`, which today is Verify.DiffPlex's, because it verifies a literal string and DiffPlex is selected by default. A download whose first `dotnet test` fails is a poor way to meet a tool, so the default one passes. Extension sample outputs depend on package versions and machine state; shipping wrong `.verified.` files is worse than shipping none. The README and AI content explain that the first run produces `.received.` files and how to accept them.
- **D7. "Verbose" vs "minimal" is per extension** and defaults to **verbose**, because the requester asked to bias toward more content that users can delete. Minimal = the enable call plus the one or two most common usages. Verbose = minimal plus every documented API in the catalogue's "Verbose / edge-case APIs" list, each as its own commented test method.
- **D8. Sponsor step interprets `<Verify_SponsorshipStart>One Month From Now</Verify_SponsorshipStart>`** from the request as "emit a real date". The wizard emits `Verify_SponsorshipStart` = **today (UTC, `yyyy-MM-dd`)** when the user says the sponsorship is new, because the bundled sponsor list is frozen at pack time and a future date fails with SC028. The user can edit the date. The alternative reading (a date one month ahead) is rejected by the verifier, so it is not offered. See section 14.
- **D9. Expecto (F#) is supported for the core sample only.** Extension usage samples are C#; for Expecto the docs show them as C# with a note, the F# test project configures Verify in a `lazy` value in `Tests.fs` that every test forces before verifying, and extension tests are not generated into the fsproj. A custom `main` is not possible there: with `EnableExpectoTestingPlatformIntegration` the SDK generates the entry point (FS0433), and F# has no module initializers. This is a scoping decision, not a limitation of Verify. The F# initializer is generated from the same ordered call list as the C# one, so a selected plugin is enabled either way; a call needing one of the C# helper methods the generator emits, such as EntityFramework's `GetDbModel()`, is replaced by a comment saying plugin discovery enables that extension with its defaults instead.
- **D10. Fixie keeps VSTest.** Fixie has no Microsoft.Testing.Platform runner, so the generated Fixie solution references `Fixie.TestAdapter` + `Microsoft.NET.Test.Sdk` and its `global.json` omits the `"test": { "runner": ... }` block (which would break `dotnet test` for VSTest projects).
- **D11. Deep link per extension**: `/add/{ExtensionId}` seeds the "add extension" flow with that extension pre-selected, so each extension readme can link to it (the SponsorCheck `/package/{id}` pattern).
- **D12. Breadcrumb lives in a left rail** on wide screens and collapses to a horizontal strip above the content on narrow screens.
- **D13. "Existing extensions" and "tech stack" are persisted in `localStorage`**, along with the sponsor account. Nothing else is persisted; everything else is in the URL.
- **D14. The site URL is `https://verifytests.github.io/Wizard/`** (GitHub Pages for the `VerifyTests/Wizard` repository). Base href `/Wizard/`.
- **D15. Content copied from Verify docs is embedded as resources** in `Wizard.Core/Content/*.md`, with an opt-in network test that diffs them against `raw.githubusercontent.com` so drift is noticed (section 17.5).
- **D16. No binary fixtures are shipped** (revised in phase 2; replaces shipping `wwwroot/fixtures/` and fetching them when building the zip). A sample that needs a document or an image builds it in the test, with a package the sample already references: QuestPDF writes a pdf, ClosedXml a workbook, ImageSharp an image. Where that is impossible, because the extension only reads a format nothing else selected can write, the sample is generated with the test framework's skip attribute and a comment naming the file the reader has to supply. This keeps generation a pure synchronous function of the state, which is what makes every generator snapshot-testable, and it keeps the zip free of binaries whose provenance and licensing would have to be explained.
- **D17. Solution name is an input** (default `VerifySample`). Project names are `<SolutionName>` (class library holding `ClassBeingTested`) and `<SolutionName>.Tests`. The word `Verify` is allowed but the docs warn that an MTP entry point generated into a namespace containing `Verify` can shadow the `Verify()` method (the Verify repository works around this with `<RootNamespace>Fake</RootNamespace>`); the generator sets `<RootNamespace>` to the project name with `Verify` replaced by `VerifyTests` when needed.
- **D18. Markdown rendering in the browser uses Markdig** (pure managed, works under WASM). Output is our own text, so no HTML sanitisation is needed.
- **D19. Zip creation happens in the browser** with `System.IO.Compression.ZipArchive` (available in WASM) and is handed to JS as a `Blob` for download. No server.
- **D20. `Verify.Sample` is not an extension** and is excluded from the registry. Third-party extensions listed in Verify's `plugin-list.include.md` but not under `D:\Code\VerifyTests` (Verify.AustralianProtectiveMarkings, Verify.Cli, Verify.GraphQL, Verify.TestableIO.System.IO.Abstractions, YellowDogMan.*, Verify.MongoDB, Verify.Nupkg, Spectre.Verify.Extensions, Verify.Xamarin) are listed in a "Other community extensions" section of the output as links only; they are not selectable in v1.

---

## 3. Requirements checklist

Traceability from the request to the sections that satisfy it.

| Requirement (paraphrased from the request) | Where |
|---|---|
| More advanced version of `docs/wiz`, currently generated by `WizardGen` | whole plan; 19 for retirement of the old one |
| Entry point for adding a Verify extension to an existing project | 7.2 (flow B) |
| Select multiple extensions; some interact and need custom config (EF + SqlServer example) | 9, 11 |
| Hard-coding known interactions is fine | 11 |
| Optionally select extensions already in use; list side effects; do not re-prompt them; persist in browser | 7.2 step "Already using", 8.2, 11 |
| Alternate entry seeded with "what tech are you using", suggesting a subset | 7.3 (flow C), 10 |
| New-user flow also gets optional tech step and suggested subset, user picks | 7.1, 10 |
| Persist tech to browser memory | 8.2 |
| Every extension usage verbosely commented in docs and code | 12.3, 12.4 |
| Per extension: "verbose" or "minimal" | 6.2 (`Depth`), 12.4 |
| LocalDb has an extension to Verify | 9 (`LocalDb` entry), 11 (EF/SqlServer/LocalDb rules) |
| Render the current breadcrumb path on the left | 7.4 |
| Every terminating point: rendered markdown page, downloadable zipped solution (bias to more options), AI markdown (download or copy) | 12 |
| Add-extension flow also gets download and AI content | 12.5 |
| NuGet versions current stable | 15 |
| dotnet/SDK same as this project | 5, D2 |
| Downloaded solution uses Central Package Management | 12.2 |
| GH sponsor org input feeding `Verify_GitHubSponsorAccount` / `Verify_SponsorshipStart`; persisted | 14, 8.2 |
| URL changes so state is bookmarkable/shareable | 8.1 |
| WASM app deployed through GitHub Actions, reusing SponsorCheck.Web plumbing | 16, 18 |

---

## 4. Repository layout

```
D:\Code\VerifyTests\Wizard\
  .editorconfig                      copy from SponsorCheck, plus the Verify settings block
  .gitattributes                     copy from Verify (verified.* eol/encoding rules)
  .gitignore                         *.received.*, bin/, obj/, .vs/, .idea/, *.user, publish/
  global.json                        { sdk: { version: "10.0.401", allowPrerelease: true, rollForward: "latestFeature" }, test: { runner: "Microsoft.Testing.Platform" } }
  readme.md                          what the site is, link to it, how to run locally, how to add an extension to the registry
  claude.md                          repo guidance for AI assistants (mirror SponsorCheck's structure; see 4.1)
  license.txt                        MIT
  plan.md                            this file
  plan-research/                     the four catalogue files (input to section 9)
  .github/
    workflows/
      deploy.yml                     build + test + publish + Pages deploy (section 18.1)
      refresh-versions.yml           weekly nuget.org version refresh PR (section 15.2)
      integration.yml                compile the generated solutions (section 17.4), on PR and nightly
    dependabot.yml                   nuget + github-actions
  src/
    Wizard.slnx
    Directory.Build.props            Version, LangVersion preview, ImplicitUsings, TreatWarningsAsErrors, Verify_SponsorshipExemption OpenSource + until, NoWarn
    Directory.Packages.props         CPM for this repo (see 5.2)
    nuget.config                     copy from SponsorCheck (signature validation + microsoft author cert note)
    Wizard.Core/
      Wizard.Core.csproj             net10.0 class library; embeds Content/*.md and package-versions.json
      Model/                         WizardState, enums, records (section 6)
      Registry/                      ExtensionDefinition, Extensions.All, Techs.All, TechSuggestions, InteractionRules (sections 9-11)
      Content/                       copied include texts (Appendix A)
      Versions/                      package-versions.json, PackageVersions (baked), IPackageVersionSource
      Generation/
        DocsGenerator.cs             human markdown (12.3)
        AiContentGenerator.cs        AI markdown (12.6)
        SolutionGenerator.cs         file tree for the zip (12.2, 12.5)
        ModuleInitializerGenerator.cs (12.4)
        ProjectFileGenerator.cs      csproj/fsproj/props/slnx/global.json
        BuildServerGenerator.cs      GH Actions / AppVeyor / Azure DevOps files
        TestCodeGenerator.cs         per framework test attributes and per-extension sample tests
        ZipBuilder.cs                ZipArchive from the file tree
      Text/                          MsBuildXml (from SponsorCheck), CodeBuilder (indentation helper), MarkdownBuilder
    Wizard.Web/
      Wizard.Web.csproj              Blazor WASM (5.1)
      Program.cs
      App.razor, _Imports.razor, GlobalUsings.cs, ILLink.Descriptors.xml
      Layout/MainLayout.razor        header, left rail slot, main, footer
      Pages/
        Home.razor                   three entry cards + "already have a link" note
        New.razor (+ .cs)            flow A
        Add.razor (+ .cs)            flow B; also handles /add/{ExtensionId}
        AddByTech.razor (+ .cs)      flow C
      Components/
        Breadcrumb.razor             left rail (7.4)
        Steps/                       one component per step (OsStep, IdeStep, CliStep, TestFrameworkStep, BuildServerStep, TechStep, ExistingExtensionsStep, ExtensionsStep, ExtensionOptionsStep, SponsorStep, OutputStep)
        ExtensionCard.razor          checkbox, description, tags, badges (Windows-only, licence, external tool, beta), depth toggle
        InteractionNotice.razor      rendered interaction rule
        CodeBox.razor, CopyMarkdownButton.razor, MarkdownView.razor (Markdig)
        DownloadButton.razor
      Services/
        WizardStateUrl.cs            query-string (de)serialisation (8.1)
        BrowserStorage.cs            localStorage wrapper (8.2)
        ClipboardService.cs, DownloadService.cs (JS interop)
        PackageVersionLookup.cs      runtime nuget.org refresh (15.3)
        FixtureLoader.cs             fetches wwwroot/fixtures/* for the zip
      wwwroot/
        index.html, css/app.css, fonts/, js/interop.js, favicon.svg, fixtures/
    Wizard.Tests/
      Wizard.Tests.csproj            TUnit + Verify.TUnit + Verify.DiffPlex + bunit + Verify.Bunit + Verify.AngleSharp + Microsoft.Playwright + Verify.Playwright + DiffEngine
      Generation/*Tests.cs           snapshot tests of every generator
      Registry/*Tests.cs             registry invariants
      Components/*Tests.cs           bunit
      Pages/*Tests.cs                bunit
      ScreenSnapshotTests.cs         Playwright PNG + HTML per screen
      EndToEndTests.cs               journeys incl. URL round-trip and localStorage
      Integration/                   compile-the-zip tests (opt-in)
      PublishedWizard.cs, WebTestContext.cs, FakeNuGetFeed.cs, StubNuGetHandler.cs, ModuleInitializer.cs
```

### 4.1 Conventions for this repository

- Follow the requester's global C# style: lambda parameters are `_` (nested lambdas get a descriptive name), no single-character locals other than loop counters, no abbreviations in identifiers (`context` not `ctx`, `extension` not `ext`, `configuration` not `config`).
- `ProjectDefaults` package with `PrivateAssets="all"` in every project (as SponsorCheck and Verify do).
- Snapshot everything the generators produce; the generators are pure functions of `WizardState` + `PackageVersions` + `DateOnly today`.
- No heredocs in scripts; MSBuild `WriteLinesToFile` for generated constants.

---

## 5. Build, SDK and target framework

### 5.1 Wizard.Web.csproj

Start from `SponsorCheck.Web.csproj` and change:

- `GenerateWizardDefaults` target emits, into `WizardDefaults.g.cs`:
  - `public const string WizardVersion = "$(Version)";`
  - `public const string TargetFramework = "$(TargetFramework)";` (the TFM emitted into generated csproj files)
  - `public const string SdkVersion = "<read from ../../global.json>";` — read with a small inline task or `$([System.Text.RegularExpressions.Regex]::Match($([System.IO.File]::ReadAllText('$(MSBuildThisFileDirectory)../../global.json')), '"version"\s*:\s*"([^"]+)"').Groups[1].Value)`.
  - `public const string SiteBase = "https://verifytests.github.io/Wizard";`
- Put this target in `Wizard.Core.csproj` instead of the Web project, since the generators live in Core. Core cannot read the Web project's TFM, so define `<GeneratedTargetFramework>net10.0</GeneratedTargetFramework>` once in `src/Directory.Build.props` and use it for both `<TargetFramework>` of every project and the baked constant. A test asserts `WizardDefaults.TargetFramework == typeof(WizardDefaults).Assembly` target framework attribute, and another asserts `WizardDefaults.SdkVersion` equals the `global.json` value (RepoContractTests pattern).
- Keep `PublishTrimmed=true`, `TrimMode=full`, `InvariantGlobalization=true`, `BlazorEnableTimeZoneSupport=false`. Add `<TrimmerRootDescriptor Include="ILLink.Descriptors.xml" />` preserving `Wizard.Web` and `Wizard.Core` (Markdig and System.Text.Json reflection are fine under trimming when using source-generated JSON contexts; use `JsonSerializerContext` for `package-versions.json`).
- Sponsor properties for this repo's own build: `<Verify_SponsorshipExemption>OpenSource</Verify_SponsorshipExemption>` + `<Verify_SponsorshipExemptionUntil>` in `Directory.Build.props` (Verify packages are referenced by the tests).

### 5.2 Directory.Packages.props (this repository)

`ManagePackageVersionsCentrally=true`, `CentralPackageTransitivePinningEnabled=true`. Packages: `Microsoft.AspNetCore.Components.WebAssembly`, `Markdig`, `ProjectDefaults`, `Polyfill`, `TUnit`, `Verify`, `Verify.TUnit`, `Verify.DiffPlex`, `Verify.AngleSharp`, `bunit`, `Verify.Bunit`, `Microsoft.Playwright`, `Verify.Playwright`, `DiffEngine`, `MarkdownSnippets.MsBuild` (for readme generation), `System.Text.Json`. Take initial versions from `SponsorCheck/src/Directory.Packages.props` (all current as of this plan: Verify 33.1.0, Verify.DiffPlex 3.3.1, Verify.AngleSharp 5.1.2, bunit 2.11.3, Verify.Bunit 14.0.0, Microsoft.Playwright 1.62.0, Verify.Playwright 3.1.1, DiffEngine 20.3.1, TUnit 1.68.17, Microsoft.AspNetCore.Components.WebAssembly 10.0.12).

Note: `DiffEngine` is referenced by the **tests** so a registry test can assert the wizard's static diff-tool list matches `DiffEngine.Definitions.Tools` (section 17.2). Do not reference DiffEngine from `Wizard.Core` (it uses `Process` and is not meant for WASM).

---

## 6. Domain model

All in `Wizard.Core/Model`. Records are immutable; the UI mutates a mutable `WizardState` and converts.

### 6.1 Enums

```
Flow            { New, Add, AddByTech }
Os              { Windows, MacOS, Linux }
Ide             { VisualStudio, VisualStudioWithReSharper, Rider, VsCode, Other }   // VsCode added: the text-file-settings include has VS Code specific guidance
CliPreference   { Cli, Gui }
TestFramework   { XunitV3, NUnit, TUnit, MSTest, Fixie, Expecto }
BuildServer     { GitHubActions, AzureDevOps, AppVeyor, None }
Depth           { Minimal, Verbose }
SponsorMode     { NotChosen, Sponsor, Exempt, PrivateArrangement, Ignore }
Exemption       { OpenSource, SmallRevenue, MaintainerConsulting }
```

Keep the enum member names identical to the existing `WizardGen` enums where they overlap, because they become URL values (8.1) and readmes may already link to `docs/wiz` file names; a redirect map from old file names to new URLs is derived from these names (19.2).

### 6.2 WizardState

```
sealed class WizardState
{
    Flow Flow
    // environment (New flow asks all; Add flows ask only TestFramework, and Os/Ide/Cli optionally for tooling advice)
    Os? Os
    Ide? Ide
    CliPreference? Cli
    TestFramework? TestFramework
    BuildServer? BuildServer
    // project
    string SolutionName = "VerifySample"
    // extensions
    HashSet<string> Techs                         // Tech ids (section 10)
    HashSet<string> ExistingExtensions            // Extension ids already in the user's project (Add flows)
    HashSet<string> SelectedExtensions            // Extension ids to add / include
    Dictionary<string, Depth> Depths              // per selected extension; default Verbose
    Dictionary<string, string> Choices            // per-extension or per-rule choices, e.g. "ef-sql-recording" => "keep-ef"; "diffplex-output" => "Compact"; "systemjson-strict" => "false"
    // sponsor
    SponsorMode SponsorMode
    string SponsorAccount
    DateOnly? SponsorshipStart
    Exemption? Exemption
    string ExemptionUntilMonth                    // yyyy-MM
    string LicensedUntilMonth                     // yyyy-MM
    // navigation
    string Step                                   // current step id (8.1)
}
```

`WizardState.Normalize()` removes selections that are invalid for the current flow (e.g. an `ExistingExtensions` entry that is also in `SelectedExtensions` is removed from `SelectedExtensions`), fills `Depths` defaults, and is called after every URL parse and every UI change.

### 6.3 Derived objects

- `Plan` (`Wizard.Core/Generation/PlanBuilder.cs`): the resolved view the generators consume. Contains the ordered list of `ResolvedExtension` (definition + depth + choices + the packages and versions to reference), the applicable `InteractionResult`s (rule, severity, message, generated-code effects), the test projects to emit (main and optional Windows), the tools to install, the licences/environment variables required, and warnings. Building the plan is pure and unit-tested by snapshot for many states.
- `GeneratedSolution`: `IReadOnlyList<GeneratedFile>` where `GeneratedFile` = (relative path, text content or byte content, `IsBinary`). Both the zip and the docs "file list" derive from it.

---

## 7. Flows, steps and navigation

Every flow is a linear list of steps; each step is a Razor component bound to `WizardState`. Steps are skipped when they do not apply. The left breadcrumb shows every step in the flow, marks completed ones with the chosen value, and lets the user jump back.

### 7.1 Flow A – New to Verify (`/new`)

1. **Operating system** (Windows / MacOS / Linux). Same as today.
2. **IDE** (filtered by OS as `WizardGen.GetIdesForOs`, plus VS Code on every OS).
3. **CLI or GUI preference**. Same text as today ("This will affect the approach to installing NuGet packages and snapshot management options").
4. **Test framework** (xUnit v3, NUnit, TUnit, MSTest, Fixie, Expecto). Show one-line notes: Fixie has no MTP runner; Expecto is F#; MSTest needs `[UsesVerify]`.
5. **Build server** (GitHub Actions, Azure DevOps, AppVeyor, none).
6. **Tech stack** (optional, multi-select chips grouped by category; persisted). Skippable with "I just want the basics". Selecting tech pre-checks extensions on the next step.
7. **Extensions** (multi-select cards grouped by category; suggested ones pre-checked and shown first under "Suggested for your stack", the rest under "Everything else"). Verify.DiffPlex is pre-checked always. Cards show badges: Windows only, licence required, external tool required, needs running service, beta, net10 only. Interaction notices appear inline as soon as two interacting extensions are both checked (section 11).
8. **Extension options**: per selected extension, a Minimal/Verbose toggle (default Verbose), plus any rule choices (EF/SqlServer recording owner, DiffPlex output type, SystemJson strict JSON, Bunit exclude component, image comparer tolerance, PDFium dpi, HeadlessBrowsers driver, EF Core vs EF6, Serilog custom configuration stub). One compact form.
9. **Sponsor** (section 14).
10. **Output** (section 12): rendered docs, three download/copy actions, file list.

### 7.2 Flow B – Add extensions to an existing project (`/add`, `/add/{ExtensionId}`)

1. **Test framework** (needed to generate tests and the correct LocalDb package).
2. **Already using** (optional): multi-select of extensions already in the project. Pre-filled from `localStorage`. Everything checked here is excluded from step 3's pick list (shown greyed as "already in your project") and participates in interaction rules with a "Existing" role so the side effects of adding a new extension next to an existing one are listed.
3. **Extensions to add** (same card grid as A7, without tech pre-selection; deep-link id pre-checked). Nothing is selected by default: Verify.DiffPlex, pre-checked for a new project, is not assumed for an existing one. An existing extension's card is shown ticked and locked. When a rule the new selection triggers changes how an existing extension has to be initialized (EntityFramework added next to an existing SqlServer turns SqlServer's recording off), the generated `ModuleInitializer.cs` carries that existing call too, with a comment saying to replace the project's own; an existing extension that plugin discovery cannot find (A1) raises an `existing-not-discovered` warning, because a project relying on `InitializePlugins()` alone never enabled it.
4. **Extension options** (as A8).
5. **Sponsor**: always shown, with **No change** as the first and default option, because an existing project already declares its status; a remembered declaration pre-fills it. The output has a `Directory.Build.props` fragment only when a mode is chosen, or when an added package brings another owner's fee check (A8), whose block is emitted even when Verify's is left alone.
6. **Output**: docs with "Change to make" sections (PackageVersion/PackageReference lines, ModuleInitializer merge instructions, new test files), a zip of just the new/changed files, and AI markdown that instructs an assistant to perform the merge.

Optional environment questions (OS / IDE / CLI) and the build server are **not** asked in flow B, and answers to them in a url are dropped. The guide assumes the tooling is already set up.

### 7.3 Flow C – Suggest extensions for my tech stack (`/add/by-tech`)

Identical to flow B with a **Tech stack** step inserted before "Extensions to add", which pre-checks the suggestions (10). Persisted tech pre-fills the step. Flow A has the same step, between the build server and the extensions.

### 7.4 Left breadcrumb rail

`Components/Breadcrumb.razor`, a vertical list rendered in `MainLayout`'s left column (`min-width: 240px`), one row per step in the current flow:

- Completed step: step title in muted text, chosen value(s) in normal text (`Windows`, `Rider`, `xUnit v3`, `GitHub Actions`, `3 techs`, `EntityFramework, SqlServer, DiffPlex`, `Verbose ×3`, `Sponsor: acme`), clickable (navigates to that step; later steps keep their values, which is what makes the URL sharable mid-flow).
- Current step: highlighted, not clickable.
- Future step: muted title only, not clickable (gated by validation of the current step, as SponsorCheck's `Stepper`).
- Top row: the flow name ("New project", "Add extensions", "Add by tech stack") linking to Home.
- Below `768px` width the rail becomes a horizontal, scrollable strip above the content showing only completed values (`Windows › Rider › CLI › xUnit v3 › …`).
- Accessible: `<nav aria-label="Wizard progress"><ol>…`.

Replace SponsorCheck's `Stepper` with this component; keep its `StepSelected` callback semantics.

### 7.5 Home (`/`)

Three cards: "New to Verify" → `/new`, "Add extensions to an existing project" → `/add`, "Suggest extensions for my tech stack" → `/add/by-tech`. Below: a line saying that any page's URL can be bookmarked or shared, and that the wizard stores tech stack, existing extensions and sponsor account in the browser (with a "Forget everything" button that clears `localStorage`).

---

## 8. URL state and browser persistence

### 8.1 URL encoding

The full `WizardState` is encoded in the query string of the flow route so that any point is a bookmark and a share link. Blazor `NavigationManager.NavigateTo(url, replace: true)` after every change; `OnParametersSet` parses on load and on browser back/forward (`LocationChanged`).

Route: `/new`, `/add`, `/add/by-tech`, `/add/{ExtensionId}` (the last redirects to `/add?ext=…&step=extensions` after seeding).

Query keys (short, stable, documented in `WizardStateUrl.cs`):

| key | value | example |
|---|---|---|
| `step` | step id | `extensions` |
| `os` | `Os` name | `Windows` |
| `ide` | `Ide` name | `Rider` |
| `cli` | `Cli` or `Gui` | `Cli` |
| `tf` | `TestFramework` name | `XunitV3` |
| `ci` | `BuildServer` name | `GitHubActions` |
| `name` | solution name | `VerifySample` |
| `tech` | comma list of tech ids | `efcore,sqlserver,aspnetcore` |
| `have` | comma list of existing extension ids | `DiffPlex,Http` |
| `ext` | comma list of selected extension ids, in registry order. Absent means the default selection, `DiffPlex`, so a link made before an extension existed still means what it meant; the literal `none` means nothing is selected | `EntityFramework,SqlServer` |
| `min` | comma list of extension ids set to Minimal (absence = Verbose) | `SqlServer` |
| `opt` | comma list of `key:value` choices | `ef-sql-recording:keep-ef,diffplex-output:Compact` |
| `sponsor` | mode | `Sponsor`, `Exempt`, `Private`, `Ignore` |
| `account` | GitHub account | `acme` |
| `start` | `yyyy-MM-dd` | `2026-09-22` |
| `exempt` | exemption name | `OpenSource` |
| `until` | `yyyy-MM` | `2027-09` |

Rules: omit defaults; ids are validated against the registry and unknown ones dropped silently; the parse is total (never throws). `WizardStateUrl` has round-trip snapshot tests. Keep values readable rather than compressed: shareable links are meant to be read.

Because GitHub Pages serves `404.html` (a copy of `index.html`) for unknown paths, deep routes work; the test host replicates this with `MapFallbackToFile("{*path}", …)` exactly as `PublishedWizard.cs` does.

### 8.2 localStorage

`BrowserStorage` (JS interop over `localStorage`, all reads/writes in try/catch, no-ops in private mode). Keys, all prefixed `verify-wizard:`:

| key | content | written when |
|---|---|---|
| `tech` | comma list of tech ids, as in the url | tech step changes |
| `existing` | comma list of extension ids, as in the url | "already using" step changes |
| `sponsor` | the declaration as a query string (`sponsor=Exempt&exempt=SmallRevenue&until=2027-09`) | sponsor step changes |

Values are stored in their url form rather than as JSON, so reading one back is the url parser's job and the two cannot disagree. Depth is not remembered: it is a per-project answer, and the url already holds it.

Precedence: a value present in the URL wins over `localStorage`; when the URL has none, the stored value seeds the state and the URL is updated (so the resulting link is complete). A remembered stack applies its recommendations only when the url says nothing about extensions. Each step that was filled in from memory says so ("Restored from this browser"). The rules are the pure `BrowserMemory` class in Core, so they are unit-tested without a browser.

A flow reads and writes only the answers to questions it asks: the new-project flow never clears the list of existing extensions an add flow kept. Changing an answer to nothing forgets it. The Home page has a "Forget them" button that clears every key.

bunit tests stub `IJSRuntime` for storage. The Playwright tests share one browser context for speed, so each page in it gets an in-memory localStorage of its own from an init script, and parallel tests cannot restore each other's answers; the two journeys about remembering between visits use a context of their own, with real storage.

---

## 9. Extension registry

`Wizard.Core/Registry/Extensions.cs` holds one `ExtensionDefinition` per selectable extension. This is hand-written data, sourced from `plan-research/extension-catalogue-*.md`. Appendix B has the summary table; the catalogue has the code samples to copy into `MinimalSamples`/`VerboseSamples`.

### 9.1 ExtensionDefinition

```
sealed record ExtensionDefinition(
    string Id,                                   // "EntityFramework", "Playwright", "LocalDb"
    string DisplayName,                          // "Verify.EntityFramework (EF Core)"
    string RepoUrl,
    string Description,                          // one line from plugin-list.include.md / readme
    ExtensionCategory Category,                  // DeveloperExperience, Web, Ui, Documents, Images, Data, Messaging, Logging, Mocking, Serialization, Scrubbing, Infrastructure, Tooling
    IReadOnlyList<string> Tags,                  // tech ids (section 10) this extension serves
    IReadOnlyList<PackageRequirement> Packages,  // NuGet packages to reference; may depend on TestFramework (LocalDb) or a choice (EF Core vs Classic)
    InitializeShape Initialize,                  // see below
    IReadOnlyList<string> Usings,                // namespaces the generated tests need (e.g. VerifyTestsAspose, VerifyTests.DiffPlex, VerifyTests.Http)
    IReadOnlyList<ProjectRequirement> ProjectRequirements,   // csproj properties/items: UseWPF, UseWindowsForms, FrameworkReference Microsoft.AspNetCore.App, Sdk Web, CopyToOutputDirectory fixtures, InternalsVisibleTo
    IReadOnlyList<ExternalRequirement> ExternalRequirements, // Ghostscript, Pandoc, SQL LocalDB, Cosmos emulator, RavenDB embedded, browser install, licence key + env var name, API key
    Platform Platform,                           // CrossPlatform | WindowsOnly
    IReadOnlyList<string> SupportedTargetFrameworks,   // informational, from the catalogue; used to warn if emitted TFM is not covered (none today)
    IReadOnlyList<TestFramework> UnsupportedTestFrameworks,  // e.g. LocalDb base class: Fixie, Expecto; Avalonia headless attributes: TUnit, MSTest, Fixie, Expecto
    IReadOnlyList<string> ExclusiveGroups,       // group ids (11.1)
    IReadOnlyList<ExtensionChoice> Choices,      // per-extension choices rendered on the options step
    IReadOnlyList<Sample> MinimalSamples,        // each: title, description, code (test method body or full test), fixtures
    IReadOnlyList<Sample> VerboseSamples,        // superset additions
    IReadOnlyList<string> Notes,                 // gotchas rendered under "Notes" in docs and as comments in code
    bool DiscoveredByInitializePlugins,          // false for AngleSharp, QuestPDF, Blazor (1.3); asserted by a registry test against core's naming rule
    bool Beta = false,                            // e.g. Verify.Bunit 14.1.0-beta.1 (only if no stable exists)
    string? SampleVerifiedOutput = null)          // shown in docs as "resulting snapshot"
```

`InitializeShape` is a small discriminated set:

- `None` (Terminal: a dotnet tool, not a library),
- `NoOp` (Wolverine, ParametersHashing – emitted as a comment only),
- `Static(call)` e.g. `VerifyHttp.Initialize()`,
- `StaticWithParameters(template, parameters)` e.g. `VerifyDiffPlex.Initialize(OutputType.{diffplex-output})`, `VerifySqlServer.Initialize(recordCommands: {bool})`, `VerifyEntityFramework.Initialize(GetDbModel(), recordCommands: {bool})` with a preamble (the `GetDbModel()` helper),
- `Custom(generator)` for three extensions:
  - LocalDb: `LocalDbTestBase<TheDbContext>.Initialize()` after `InitializePlugins`.
  - Quibble: `VerifierSettings.UseStrictJson()` before `VerifyQuibble.Initialize()`.
  - Blazor: `RuntimeHelpers.RunClassConstructor(typeof(Render).TypeHandle);`, with a comment.
    - Why: `Render`'s static constructor calls `VerifyBlazor.Initialize()`, which throws once any verification has run (`Verify.Blazor\src\Verify.Blazor\Render.cs:5-6`, `VerifyBlazor.cs:19`).
    - Left lazy, a generated project whose core `Sample` test runs before the first `Render.Component` fails every Blazor test with `TypeInitializationException`, depending on test order.
    - Retired by U1.

`PackageRequirement` = (package id, kind: `PackageReference` | `FrameworkReference` | `DotnetTool`, optional condition: test framework or choice, optional `SponsorOwner` for packages that carry their own sponsorship gate, see 14). Examples:

- Verify.Avalonia: `Verify.Avalonia`, `Avalonia.Headless.XUnit` (when xUnit v3) or `Avalonia.Headless.NUnit` (when NUnit), `Avalonia.Themes.Fluent`, `Avalonia.Skia`.
- Verify.AspNetCore: `Verify.AspNetCore` (the package already declares `FrameworkReference Microsoft.AspNetCore.App`, so none is emitted) + (verbose) `Microsoft.AspNetCore.Mvc.Testing`.
- LocalDb: `EfLocalDb.Xunit.V3` / `EfLocalDb.NUnit` / `EfLocalDb.MSTest` / `EfLocalDb.TUnit` by test framework; for Fixie/Expecto fall back to `EfLocalDb` + `Verify.EntityFramework` with a note.
- HeadlessBrowsers: three separate definitions `Playwright`, `Puppeteer`, `Selenium` (repo `Verify.HeadlessBrowsers`, packages `Verify.Playwright`/`Verify.Puppeteer`/`Verify.Selenium`; Selenium also needs `Selenium.WebDriver.ChromeDriver`).
- EntityFramework: two definitions `EntityFramework` (EF Core; also `Microsoft.EntityFrameworkCore.SqlServer` for the sample) and `EntityFrameworkClassic` (EF6; `EntityFramework`).
- Terminal: `DotnetTool verify.tool` (no PackageReference, no Initialize; emitted into `.config/dotnet-tools.json` and the docs).
- DiffEngineTray: not an extension but a tool; handled in the docs generator (Windows only), and added to the tools list.

`Usings` beyond `VerifyTests` (which Verify imports implicitly), as found in the extensions' sources:

| Namespace | Needed for |
|---|---|
| `VerifyTests.AngleSharp` | `HtmlPrettyPrint` (wherever `html-prettyprint` fires) |
| `VerifyTestsAspose` | Aspose settings |
| `VerifyTestsImageMagick` | ImageMagick settings |
| `VerifyTests.ICSharpCode.Decompiler` | `TypeToDisassemble` |
| `VerifyTests.MicrosoftLogging` | `RecordingLogger`, `RecordingProvider` |
| `VerifyTests.NServiceBus` | `Recording*` types |
| `VerifyTests.SqlServer` | schema types |
| `VerifyTestsPlaywright`, `VerifyTests.Puppeteer`, `VerifyTests.Selenium` | `SocketWaiter` |
| `VerifyQuestPDF` | `ShouldIncludePage` |

Extension usings go in each test file, never in global usings. The three browser namespaces each define their own `SocketWaiter`, so importing two of them is a compile error (CS0104).

### 9.2 Registry invariants (tests)

- Ids unique; every `Tags` entry exists in `Techs.All`; every `ExclusiveGroups` id exists in `InteractionRules.Groups`; every package id has a version in `package-versions.json`; every fixture referenced by a sample exists in `wwwroot/fixtures`; every extension appears in at least one tech suggestion or is in the explicit `NotSuggestedByTech` list (DiffPlex, Terminal, ParametersHashing, Assertions, Quibble are universal/niche and appear under "Everything else" only).
- Snapshot of the whole registry as JSON (`Registry.verified.txt`) so any change is reviewed.
- `DiscoveredByInitializePlugins` matches core's naming rule (`VerifyTests.` + assembly name without dots, case-sensitive) for the shipped assembly. Every `RetiredBy` names an id from section 22.

---

## 10. Tech stack to extension suggestions

`Wizard.Core/Registry/Techs.cs`: `Tech(Id, DisplayName, Group)` and `TechSuggestions`: tech id → (`Recommended` extension ids, pre-checked; `Related` extension ids, listed but unchecked). Groups and initial mapping:

| Group | Tech (id) | Recommended | Related |
|---|---|---|---|
| Data | Entity Framework Core (`efcore`) | EntityFramework, LocalDb (Windows) | SqlServer, ReadableExpressions |
| Data | Entity Framework 6 (`ef6`) | EntityFrameworkClassic | LocalDb |
| Data | SQL Server (`sqlserver`) | SqlServer | LocalDb, EntityFramework |
| Data | Azure Cosmos DB (`cosmos`) | Cosmos | |
| Data | RavenDB (`ravendb`) | RavenDB | |
| Data | CSV files (`csv`) | CsvHelper | Sep |
| Data | Excel files (`excel`) | ClosedXml | OpenXml, Sylvan, Aspose, Syncfusion |
| Documents | Word documents (`word`) | OpenXml | Pandoc, Aspose, Syncfusion |
| Documents | PowerPoint (`powerpoint`) | OpenXml | Aspose, Syncfusion |
| Documents | PDF files (`pdf`) | PdfPig | PDFium, DocNet, ImageMagick, Aspose, Syncfusion |
| Documents | PDF generation with QuestPDF (`questpdf`) | QuestPDF | PdfPig |
| Web | ASP.NET Core (`aspnetcore`) | AspNetCore, Http | AngleSharp |
| Web | HttpClient / REST calls (`http`) | Http | Mockly, Flurl |
| Web | Flurl (`flurl`) | Flurl | |
| Web | Blazor (`blazor`) | Bunit, AngleSharp | Blazor |
| Web | HTML / Razor output (`html`) | AngleSharp | |
| Web | Browser UI tests (`browser`) | Playwright, AngleSharp | Puppeteer, Selenium, ImageMagick |
| UI | WPF (`wpf`) | Xaml, Phash | CommunityToolkitMvvm |
| UI | WinForms (`winforms`) | WinForms | |
| UI | Avalonia (`avalonia`) | Avalonia, CommunityToolkitMvvm | |
| UI | CommunityToolkit.Mvvm (`mvvmtoolkit`) | CommunityToolkitMvvm | |
| Messaging | MassTransit (`masstransit`) | MassTransit | |
| Messaging | NServiceBus (`nservicebus`) | NServiceBus | MicrosoftLogging |
| Messaging | Wolverine (`wolverine`) | Wolverine | |
| Messaging | Brighter (`brighter`) | Brighter | |
| Logging | Microsoft.Extensions.Logging (`melogging`) | MicrosoftLogging | |
| Logging | Serilog (`serilog`) | Serilog | |
| Logging | ZeroLog (`zerolog`) | ZeroLog | |
| Observability | OpenTelemetry / Activity (`otel`) | OpenTelemetry | Diagnostics |
| Serialization | System.Text.Json (`stj`) | SystemJson | Quibble |
| Serialization | Newtonsoft.Json (`newtonsoft`) | NewtonsoftJson | |
| Serialization | YAML (`yaml`) | Yaml | |
| Serialization | NodaTime (`nodatime`) | NodaTime | |
| Serialization | ULIDs (`ulid`) | Ulid | NUlid |
| Email | System.Net.Mail (`mailmessage`) | MailMessage | |
| Email | SendGrid (`sendgrid`) | SendGrid | |
| Email | HTML email rendering (`htmlemail`) | EmailPreviewServices (paid) | AngleSharp |
| Images | Images / screenshots (`images`) | ImageSharp | ImageMagick, ImageHash, ImageSharpCompare, Phash |
| Mocking | Moq (`moq`) | Moq | |
| Mocking | NSubstitute (`nsubstitute`) | NSubstitute | |
| Mocking | FakeItEasy (`fakeiteasy`) | FakeItEasy | |
| Mocking | Mockly (`mockly`) | Mockly | |
| Compiler | Source generators (`sourcegen`) | SourceGenerators | ICSharpCodeDecompiler |
| Compiler | IL / assembly comparison (`il`) | ICSharpCodeDecompiler | |
| Compiler | Expression trees (`expressions`) | ReadableExpressions | |
| Testing | Assertion libraries inside snapshots (`assertions`) | Assertions | |
| Testing | Long parameterised test names (`longnames`) | ParametersHashing | |

Universal: `DiffPlex` is always recommended; `Terminal` is recommended when CLI preference is Cli (new flow) and always listed. Windows-only recommendations (LocalDb, Xaml, WinForms, Phash) are downgraded to `Related` when the chosen OS is MacOS or Linux.

The tech step renders groups as headed chip sets; the extension step shows "Suggested for your stack" first (recommended checked, related unchecked but listed with a "related" tag), then "Everything else" grouped by `ExtensionCategory`.

As built (phase 3): this table is the only source of suggestions. `ExtensionDefinition` has no tags of its own, so the mapping cannot disagree with itself, and a registry test checks every extension is reachable from a tech or is universal. Choosing a tech selects what it recommends; unchoosing it deselects only what no remaining tech also recommends. A recommendation is skipped when the project already has it, or when selecting it would conflict with something already selected: the table's own Excel and Word rows recommend ClosedXml and OpenXml, which both convert xlsx.

---

## 11. Interaction rules

`Wizard.Core/Registry/InteractionRules.cs`. A rule is evaluated against the union of existing and selected extensions and produces zero or more `InteractionResult(rule id, severity, involved ids, message, code effect)`. Severity: `Info` (companion suggestion), `Warning` (side effect), `Conflict` (mutually exclusive; the UI blocks Next until resolved, unless the rule has a choice that resolves it). Results are rendered on the extension step (inline, as soon as the combination appears), on the options step (with the choice control), in the docs ("Interactions between selected extensions"), as comments in the `ModuleInitializer`, and in the AI markdown. A rule or sample restriction that only works around an upstream defect carries `RetiredBy`: the section 22 id of the fix that makes it unnecessary (22.7).

### 11.1 Exclusive groups (Conflict unless the group allows co-existence with an explicit order)

| Group id | Members | Reason | Resolution UI |
|---|---|---|---|
| `pdf-converter` | PdfPig, PDFium, DocNet, Aspose, Syncfusion, ImageMagick | each registers the `pdf` stream converter; last wins | radio: pick one; the others are unchecked |
| `xlsx-converter` | ClosedXml, OpenXml, Sylvan, Aspose, Syncfusion | `xlsx`/`xls` stream converters | radio |
| `docx-converter` | OpenXml, Pandoc, Aspose, Syncfusion | `docx` stream converters | radio |
| `pptx-converter` | OpenXml, Aspose, Syncfusion | `pptx` | radio |
| `csv-scrubber` | CsvHelper, Sep | both add a `csv` scrubber and `csv` reader converters | radio |
| `image-comparer` | ImageHash, ImageMagick (comparers), ImageSharpCompare, Phash, plus the core `UseSsimForPng` option | comparers for png/jpg/bmp; last registration wins | radio incl. "Verify built-in SSIM (`VerifierSettings.UseSsimForPng`)" as default when any rendering extension is selected and no comparer package is chosen |
| `blazor-renderer` | Blazor, Bunit | alternatives; Bunit and AngleSharp both register an `html` string comparer | radio |
| `activity-listener` | Diagnostics, OpenTelemetry | both install a process-wide `ActivityListener` recording under `activity`; duplicates | radio |
| `ulid-scrubber` | Ulid, NUlid | both register a 26-char `ScrubWindow` | radio |
| `json-comparer` | Quibble only, but it forces `UseStrictJson` project-wide | Warning: every snapshot becomes `.verified.json` | none |

ImageMagick appears in two groups.
- What each call registers:
  - `Initialize()` registers svg, png, webp, tiff and pdf converters, and no comparers.
  - `RegisterComparers()` registers png/jpg/bmp/tiff/webp comparers and an svg string comparer.
- Why a role can't come from calling only part of the API: `RegisterComparers()` and `RegisterPdfToPngConverter()` do not set `Initialized`, so the trailing `InitializePlugins()` would still call `Initialize()` (`Verify.ImageMagick\src\Verify.ImageMagick\VerifyImageMagick.cs:7-30, 59-70`).
- The registry models a choice `imagemagick-role`:
  - `converter-and-comparer`: `Initialize()` then `RegisterComparers()`.
  - `comparer-only`: the same calls, with another pdf-converter member selected.
  - `pdf-only`: `Initialize()` alone.
- How roles are produced: by ordering. `VerifyImageMagick.Initialize()` is emitted first and the chosen pdf converter's `Initialize` after it, because converter registration is last-wins (`Verify\src\Verify\Splitters\Settings_Extension.cs:26`).
- A comment lists the image converters that stay registered.
- Retired by U8.

### 11.2 Ordering, duplication and dependency rules

| Rule id | Involves | Severity | Effect |
|---|---|---|---|
| `ef-sql-recording` | EntityFramework + SqlServer (either may be existing) | Warning with choice | Choice `keep-ef` (default): emit `VerifySqlServer.Initialize(recordCommands: false);` **before** `VerifierSettings.InitializePlugins()` with the readme quote as a comment. `keep-sql`: `VerifyEntityFramework.Initialize(GetDbModel(), recordCommands: false);`. `ignore-names`: `VerifierSettings.InitializePlugins(); Recording.IgnoreNames("sql");` (no ordering constraint, listener stays subscribed). `both`: no change, comment explains the double entries. Docs explain all four. |
| `sql-initialize-before-plugins` | SqlServer with `recordCommands:false` | Info | enforced by the ordering solver (11.3) |
| `ef-localdb` | EntityFramework + LocalDb | Info | use `EfLocalDb.<TestFramework>` package which already depends on Verify.EntityFramework; generate `LocalDbTestBase<SampleDbContext>` sample; `LocalDbTestBase<T>.Initialize()` after `InitializePlugins()` (a convention; no code requires the order); `LocalDbTestBase` never passes the model to Verify.EntityFramework, so the explicit `VerifyEntityFramework.Initialize(GetDbModel())` stays; scrub `chatbot_` prefix; `Initialize` throws under `--report-trx` (`LocalDb\src\EfLocalDb\ReportTrxGuard.cs:9-17`), so generated CI omits it; Windows only (the scrub and the `--report-trx` restriction are retired by U15) |
| `sql-localdb` | SqlServer + LocalDb | Info | schema snapshot sample uses `SqlInstance.Build()` |
| `localdb-framework` | LocalDb with Fixie/Expecto | Warning | no base-class package; fall back to `EfLocalDb` |
| `flurl-http` | Flurl (+ Http) | Info | Verify.Flurl depends on and auto-initializes Verify.Http; do not emit a separate `VerifyHttp.Initialize()` unless Http is selected with its own samples, in which case call `VerifyHttp.Initialize()` first (safe: Flurl checks `Initialized`, while a later explicit `VerifyHttp.Initialize()` throws). Verify.Flurl is built against Verify.Http 7.5.1, so CPM lifts it to 8.x when Http is selected; the integration matrix covers the pair (retired by the Verify.Flurl release in 22.6) |
| `nservicebus-logging` | NServiceBus (+ MicrosoftLogging) | Info | no choice: `captureLogs` only initializes Verify.MicrosoftLogging when it is not already initialized, and `InitializePlugins()` initializes it anyway as a transitive dependency (`Verify.NServiceBus\src\Verify.NServiceBus\VerifyNServiceBus.cs:30-43`); emit `VerifyNServiceBus.Initialize()`; if MicrosoftLogging is also selected, initialize it first; samples show logging through `RecordingProvider.CreateLogger<T>()` (retired by U21) |
| `avalonia-mvvm` | Avalonia without CommunityToolkitMvvm | Info | suggest adding CommunityToolkit.Mvvm converter ("Many Avalonia projects use CommunityToolkit.Mvvm…") |
| `html-prettyprint` | AngleSharp with Blazor, Bunit, Playwright, Puppeteer, Selenium, AspNetCore | Info | add `HtmlPrettyPrint.All(...)` with the Blazor marker scrubbing block to the ModuleInitializer |
| `bunit-anglesharp-comparer` | Bunit + AngleSharp | Warning | both register an `html` comparer; order emitted: `VerifyBunit.Initialize()` then `VerifyAngleSharpDiffing.Initialize()` so AngleSharp diffing wins, with a comment and a choice to swap |
| `quibble-strict-json` | Quibble | Warning | emit `VerifierSettings.UseStrictJson();` before `VerifyQuibble.Initialize();` in one method; note project-wide effect; the order is mandatory because `Initialize()` sets `Initialized` before it throws about strict JSON, so a failed call cannot be retried (retired by U20) |
| `systemjson-strict` | SystemJson | Info with choice | choice `systemjson-strict` true/false toggles `VerifierSettings.UseStrictJson()` and the sample's expected file extension |
| `rendering-needs-comparer` | any of Avalonia, WinForms, Xaml, Playwright, Puppeteer, Selenium, DocNet, PDFium, QuestPDF, Aspose, Syncfusion, OpenXml, ImageMagick, ImageSharp without an `image-comparer` choice (not EmailPreviewServices: its snapshots are `webp`, so `UseSsimForPng` has no effect) | Info | default to `VerifierSettings.UseSsimForPng(threshold)` with per-extension recommended threshold (DocNet 0.95, SponsorCheck-style 0.7 for browser screenshots, default 0.98) and a comment |
| `recording-bus` | two or more of EntityFramework, SqlServer, Http, Diagnostics, OpenTelemetry, MicrosoftLogging, Serilog, ZeroLog, NServiceBus | Info | explain that all land in the same snapshot keyed `ef`/`sql`/`httpCall`/`activity`/`log`, and show `Recording.IgnoreNames(...)`. Serilog, ZeroLog and MicrosoftLogging all use `log`, so `IgnoreNames` cannot separate them. The logging extensions call `Recording.Add`, which throws outside a recording, so samples call `Recording.Start()` before anything logs and the guide warns about logging during host startup (retired by U13) |
| `logger-takeover` | Serilog, ZeroLog | Warning | `Initialize` replaces the global logger; let the extension own it. For ZeroLog, an existing configuration only works with `AppendingStrategy.Synchronous`: the default asynchronous appender loses the recording context and captures nothing (retired by U11, U18) |
| `diffplex-default-comparer` | DiffPlex with AngleSharp, Bunit, Quibble, ImageMagick | Info | DiffPlex is the default string comparer; extension-specific comparers for html/json/svg take precedence, except that a per-test `UseDiffPlex()` overrides them for that test |
| `readable-expressions-priority` | ReadableExpressions + EntityFramework | Info | converter inserted at index 0; expression trees inside EF snapshots render as C# |
| `plugin-not-discovered` | AngleSharp, QuestPDF or Blazor listed as existing (Add flows) | Warning | `InitializePlugins()` never initialized these (1.3); the Add output tells users to add the explicit call even though the package is already referenced (retired by C2, U1, U2) |
| `settings-method-ambiguity` | two or more of DocNet, PdfPig, QuestPDF, Syncfusion, PDFium, Aspose | Info | each defines `PagesToInclude(int)` and/or `SkipPdfNormalization()` as extension methods (Aspose in `VerifyTestsAspose`, the rest in `VerifyTests`), so the fluent call is a CS0121 compile error even without a runtime conflict (QuestPDF + PdfPig is a suggested pair); samples use the static form, e.g. `PdfPigSettings.PagesToInclude(settings, 2)`, with a comment (retired by C4) |
| `transitive-sponsorship` | Pandoc | Warning | the `Pandoc` package ships its own sponsorship gate for owner `Papyrine`; without `Papyrine_*` properties the build fails (section 14) |
| `imagemagick-svg-comparer` | ImageMagick (with comparers) + AngleSharp | Warning | both register an `svg` string comparer and the last registration wins; emitted so the chosen one is last, with a choice |
| `png-converter` | ImageMagick + ImageSharp | Warning | both register a `png` stream converter and the last registration wins; emitted so the chosen one is last, with a choice |
| `imagesharp-reencode` | ImageSharp + any of PDFium, DocNet, OpenXml (with a render backend), ImageMagick, Aspose, Syncfusion | Warning | ImageSharp's converters re-encode the png pages those extensions produce and add info files, because core ignores `PerformConversion` for converter output (retired by C5) |
| `cosmos-etag` | Cosmos + Http or AspNetCore | Info | Verify.Cosmos applies `IgnoreMembers("ETag")` to every type, so ETag headers also disappear from http snapshots (retired by U23) |
| `windows-only` | WinForms, Xaml, Phash, LocalDb, Cosmos (emulator) with OS MacOS/Linux | Warning | still allowed; tests go in `<Name>.Tests.Windows`; CI workflow gets a `windows-latest` job for it |
| `licence-required` | Aspose (`AsposeLicense`), Syncfusion (`SyncfusionLicense`), EmailPreviewServices (`EmailPreviewServicesApiKey`), QuestPDF (`Settings.License = LicenseType.Community` or key) | Warning | generated ModuleInitializer reads the environment variable and throws a clear message; docs and CI workflow mention the secret |
| `external-tool` | ImageMagick pdf (Ghostscript), Pandoc (pandoc), Playwright (`installPlaywright: true` or `playwright.ps1 install`), Selenium (chromedriver), Puppeteer (`BrowserFetcher`), Cosmos (emulator), RavenDB (embedded server download), LocalDb (SqlLocalDB) | Warning | docs "Before running" section; CI workflow steps where automatable (Playwright install, `choco install ghostscript.app`) |
| `paid-service` | EmailPreviewServices | Warning | tests generated as `[Explicit]`/skipped by default (framework-specific attribute) |
| `test-framework-unsupported` | Avalonia headless with TUnit/MSTest/Fixie/Expecto; LocalDb base class with Fixie/Expecto; MSTest `[UsesVerify]` requirement | Warning | generate what is possible, explain the gap |
| `net10-only` | AspNetCore, Blazor, Bunit, Avalonia, Diagnostics, OpenTelemetry, PDFium, Flurl, Http, NServiceBus, HeadlessBrowsers, EmailPreviewServices, EntityFramework (EF Core) | Info | irrelevant while the emitted TFM is `net10.0`; keep the data so a future TFM change surfaces it |
| `beta-package` | any registry entry with `Beta` or when the live lookup finds only prereleases | Info | mention in docs |

### 11.3 Ordering solver

`ModuleInitializerGenerator` orders explicit `Initialize` calls by: (1) rule-imposed "before" edges (e.g. `VerifySqlServer.Initialize(recordCommands:false)` before `InitializePlugins`, `UseStrictJson` before `VerifyQuibble.Initialize`, `VerifyMicrosoftLogging.Initialize` before `VerifyNServiceBus.Initialize`, `VerifyHttp.Initialize` before `VerifyFlurl.Initialize`, `VerifyBunit.Initialize` before `VerifyAngleSharpDiffing.Initialize`, `UseSsimForPng`/comparer registration before converters that emit png, `VerifyImageHash.Initialize` before `VerifyImageHash.RegisterComparers` (a threshold registered earlier is overwritten when `InitializePlugins` calls `Initialize`; retired by U9), `VerifyImageSharpCompare.RegisterComparers` before `VerifyImageSharpCompare.Initialize` (its first registration wins; retired by U7), `VerifyImageMagick.Initialize` before any other pdf converter's `Initialize` (11.1)), (2) category order (comparers, then converters/plugins, then scrubbers/global settings), (3) alphabetical. A topological sort with the comparison as tie-break; a cycle is a registry bug, and the generator throws rather than emitting code whose behaviour depends on which edge was dropped. A test selects every extension at once, which is the worst case for the solver. The generated file carries no headings for these groups: each call's own comment says why it is where it is, which survives the reader deleting the calls around it. `VerifierSettings.InitializePlugins()` is always last among plugin calls; `LocalDbTestBase<T>.Initialize()` and `VerifierSettings.Inline(...)`-style global settings come after it.

---

## 12. Generated outputs

All generation is in `Wizard.Core/Generation`, pure, and snapshot-tested. Every output starts from `Plan` (6.3).

### 12.1 Output step UI

- Tabs: **Guide** (rendered markdown, default), **Files** (tree of the zip with click-to-view for text files), **AI** (the AI markdown, rendered as plain `<pre>`).
- Actions, always visible: `Download solution (.zip)`, `Copy guide as markdown`, `Download guide (.md)`, `Copy AI instructions`, `Download AI instructions (.md)`, `Copy link to this page`.
- The guide's top block restates the choices and the link (as `ConsumerConfigGenerator.BuildMarkdown` does with "Generated by the … wizard").

### 12.2 Zip layout – New flow

```
<SolutionName>/
  global.json                       SDK from WizardDefaults; "test.runner" MTP unless Fixie
  Directory.Build.props             ImplicitUsings enable, LangVersion latest, Nullable enable, TreatWarningsAsErrors true, sponsor block (14), <!-- verbose comments -->
  Directory.Packages.props          ManagePackageVersionsCentrally + CentralPackageTransitivePinningEnabled; every PackageVersion with the resolved version; grouped and commented by extension
  nuget.config                      nuget.org only (the SponsorCheck one minus the Microsoft author cert block)
  .gitignore                        from include-exclude: *.received.*, *.received/, bin/, obj/, .vs/, .idea/, *.user, TestResults/
  .gitattributes                    text-file-settings: *.verified.txt|xml|json|md|sql|csv|html|cs|il|yaml text eol=lf working-tree-encoding=UTF-8; *.verified.png|pdf|xlsx|docx|pptx|webp|bin binary
  .editorconfig                     the "Verify settings" block (charset utf-8-bom, lf, no final newline) + pure-method error for Rider/ReSharper (only when IDE is Rider or VS+ReSharper) + basic C# defaults
  <SolutionName>.slnx               Solution Items folder listing the root files (Verify.slnx style) + the projects
  <SolutionName>.slnx.DotSettings   only for Rider / ReSharper: SpawnedProcessesResponse = DoNothing
  .config/dotnet-tools.json         verify.tool (always; docs explain it is optional); nothing else (DiffEngineTray is a global tool)
  readme.md                         the generated guide (same markdown as the page)
  CLAUDE.md                         AI context (12.6)
  .github/copilot-instructions.md   same content as CLAUDE.md
  .claude/skills/verify-snapshot-testing/SKILL.md   Verify's ai-usage skill text (Appendix A)
  .github/workflows/build.yml       when GitHub Actions: setup-dotnet with global-json-file, build, test per project, upload **/*.received.* on failure (build-server-githubactions include), Windows job when a Windows test project exists, Playwright install / Ghostscript / secrets steps per external requirement; `--report-trx` is never passed when LocalDb is selected (11.2 `ef-localdb`)
  azure-pipelines.yml               when Azure DevOps (build-server-azuredevops include)
  appveyor.yml                      when AppVeyor (build-server-appveyor include)
  src/
    <SolutionName>/<SolutionName>.csproj              class library, TFM
    <SolutionName>/ClassBeingTested.cs                Verify's TargetLibrary sample (Person/Address/Title + FindPerson)
    <SolutionName>/<per-extension production code>    e.g. SampleDbContext + Company/Employee (EF), MyController (AspNetCore), TestComponent.razor (Blazor/Bunit: note the library becomes Razor SDK), Handler/Message (Wolverine/Brighter/NServiceBus), MyForm (WinForms), MyWindow.xaml (Xaml), MyUserControl.axaml (Avalonia), HelloWorldGenerator (SourceGenerators), etc. Each file has a header comment saying it exists only to be snapshot tested.
    <SolutionName>.Tests/<SolutionName>.Tests.csproj  per framework (13); PackageReferences without versions (CPM); properties and items the selected extensions need
    <SolutionName>.Tests/ModuleInitializer.cs         (12.4)
    <SolutionName>.Tests/Sample.cs                    core sample test for the framework (Appendix C)
    <SolutionName>.Tests/Sample.Test.verified.txt     the known core snapshot (D6)
    <SolutionName>.Tests/VerifyChecksTests.cs         VerifyChecks.Run()
    <SolutionName>.Tests/Extensions/<Id>Tests.cs      one file per extension; minimal or verbose samples, every method preceded by a comment block: what it shows, the API used, link to the readme section, what the snapshot will contain
    <SolutionName>.Tests/TestProject.cs               Fixie only (convention)
    <SolutionName>.Tests.Windows/…                    only when a Windows-only extension is selected (D5): its own csproj (net10.0-windows, UseWPF/UseWindowsForms), ModuleInitializer, tests. Nothing references it, so the rest of the solution builds anywhere; a solution-wide `dotnet build` still fails off Windows, so the guide and the generated CI name the portable projects instead. Not generated for Expecto, whose samples are C# (D9).
```

Every generated code file starts with a comment banner: generated by the Verify wizard, link back to the exact wizard URL (so the state can be re-opened), and "delete what is not needed".

### 12.3 The guide (human markdown)

Section order mirrors today's terminal page, then adds extension sections. All the static texts come from Appendix A. Headings use third-person phrasing (Verify's `mdsnippets` content validation forbids "you"; keep the same voice for consistency).

1. Title + "Generated by the Verify wizard" + the choices as a bullet list + the wizard link.
2. **Add NuGet packages** – CLI: `dotnet add package …` lines (test framework + Verify adapter + each extension package + Verify.DiffPlex); GUI: a `Directory.Packages.props` + `PackageReference` block. Both variants mention CPM.
3. **Microsoft.Testing.Platform** (not for Fixie) + test project settings per framework (`OutputType Exe`, `EnableNUnitRunner`, `EnableMSTestRunner`, `EnableExpectoTestingPlatformIntegration`) + `global.json` runner.
4. **Implicit usings**.
5. **Conventions**: source control includes/excludes, text file settings, EditorConfig, conventions check.
6. **Snapshot management**: DiffEngineTray (Windows), Rider plugin or ReSharper plugin + orphaned process detection + pure-method error, Verify.Terminal (CLI), clipboard link.
8. **Sample test** for the framework; MSTest marker / Fixie convention where relevant.
9. **Extensions** – one subsection per selected extension: description + repo link; "Add the NuGet" (already covered above, restated per extension for copy/paste); "Enable" (the exact Initialize call with parameters and the comment explaining each); "Usage" (minimal samples) and, at verbose depth, "More APIs" (every verbose sample with its explanation); "Resulting snapshot" when known; "Requirements" (platform, licence, tools, services); "Notes".
10. **Interactions between selected extensions** – every `InteractionResult`, with the chosen resolution and the alternatives.
11. **ModuleInitializer** – the full generated file, so the guide is usable without the zip.
12. **Diff tools** for the OS (static list from `WizardGen`/DiffEngine, verified by test against `Definitions.Tools`).
13. **Build server** – artifact upload snippet + extra steps for licences/tools.
14. **Open Source Maintenance Fee** – the sponsor block that was generated and a summary of the alternatives (14).
15. **Running it**: `dotnet test` / `dotnet run --project` commands per framework (TUnit and Expecto are executables; Fixie uses VSTest); first-run behaviour (received files; accept via DiffEngineTray / `dotnet verify accept` / rename); `DiffEngine_Disabled=true` for CI and AI runs.
16. **Other community extensions** (D20 links).
17. **Reference links**: Verify docs index entries relevant to the selections (recording, comparer, converter, scrubbers, naming, parameterised tests for the framework, plugins, maintenance fee).

### 12.4 ModuleInitializer generation

Template (C#; Expecto gets an F# equivalent in `main`):

```cs
// Generated by the Verify wizard: <url>
// Everything in this file runs once when the test assembly loads. Delete what is not needed.
using VerifyTests.DiffPlex;   // only the usings the selected extensions need

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // ---- comparers (must be registered before converters that emit images) ----
        // Verify.DiffPlex: shows an inline diff of text snapshots in the failure message instead of the full received and verified text.
        // OutputType.Compact prints only changed lines with one line of context. Alternatives: Full, Minimal.
        VerifyDiffPlex.Initialize(OutputType.Compact);

        // Verify's built in SSIM comparer for png. Rendering differs slightly between operating systems and font stacks;
        // 0.95 tolerates that while still failing on real layout changes. Remove if png snapshots must be byte exact.
        VerifierSettings.UseSsimForPng(0.95);

        // ---- plugins with parameters or ordering constraints ----
        // Verify.SqlServer records every SqlCommand under the name `sql`. Verify.EntityFramework already records the same
        // commands under `ef` (with the command Type and transaction state), so recording is disabled here to avoid every
        // command appearing twice. This call must run before InitializePlugins(), otherwise plugin discovery initializes
        // Verify.SqlServer with recording enabled and this call throws "Already Initialized".
        // Alternatives: VerifyEntityFramework.Initialize(model, recordCommands: false) keeps the `sql` entries;
        //               Recording.IgnoreNames("sql") needs no ordering but still clones every command before discarding it.
        VerifySqlServer.Initialize(recordCommands: false);

        // Verify.EntityFramework: the model is captured once so IgnoreNavigationProperties() can be called without arguments.
        VerifyEntityFramework.Initialize(GetDbModel());

        // ---- everything else ----
        // Discovers every Verify.*.dll the test project references and calls its Initialize(); plugins initialized above are skipped.
        VerifierSettings.InitializePlugins();

        // ---- global settings ----
        // (LocalDb) LocalDbTestBase<T>.Initialize() must run after the plugins.
        // (Blazor/Bunit + AngleSharp) HtmlPrettyPrint.All(...) with marker scrubbing.
        // (Serilog) IgnoreSourceContext<T>() examples at verbose depth.
    }

    // Verify.EntityFramework: builds a context against a fake connection string purely to read the model.
    static IModel GetDbModel()
    {
        var options = new DbContextOptionsBuilder<SampleDbContext>();
        options.UseSqlServer("fake");
        using var data = new SampleDbContext(options.Options);
        return data.Model;
    }
}
```

Rules: every call has a comment; comments quote the readme where the catalogue has a quote; parameters come from `Choices`; verbose depth adds commented-out alternatives (`// VerifyDiffPlex.Initialize(OutputType.Full);`). Licence keys are read from environment variables with a throw-if-missing message copied from the extension's own tests. For MSTest add `[assembly: UsesVerify]` in a separate `AssemblyInfo.cs`. For Fixie the `TestProject` convention file is emitted (Appendix C). Extension usings are per test file (9.1). When `settings-method-ambiguity` applies, the affected calls use the static form.

### 12.5 Zip layout – Add flows

Same generators, different assembly of files, rooted in a folder named `verify-additions/`:

```
verify-additions/
  readme.md                                      the guide, phrased as "changes to make"
  CLAUDE.md                                      AI instructions to perform the merge (12.6)
  Directory.Packages.props.fragment.xml          <PackageVersion> lines to merge. The guide also shows the <PackageReference Version=…> form and the `dotnet add package` commands, rather than asking whether the project uses Central Package Management
  TestProject.csproj.fragment.xml                <PackageReference>/<FrameworkReference>/properties to merge; Windows-only extensions add a note that the project has to target the -windows TFM, since an existing project has one test project and the reader decides where they go
  ModuleInitializer.cs                           complete file; the guide explains how to merge into an existing initializer (explicit calls go before InitializePlugins), and any existing extension's changed call is in it with a comment (7.2). Expecto gets initialize.fragment.fs instead
  Directory.Build.props.fragment.xml             only when a fee mode is chosen, or an added package brings another owner's check (A8)
  Extensions/<Id>Tests.cs                        per added extension, with any shipped snapshot beside it (D6)
  src-samples/…                                  production-side sample types the tests need (SampleDbContext etc.), with a note that they are placeholders for the user's own types
  .config/dotnet-tools.json.fragment.json        when Terminal selected
```

### 12.6 AI markdown

`AiContentGenerator` produces one document, used for the AI tab, the download, and `CLAUDE.md`/`copilot-instructions.md` in the zip. Structure:

1. Purpose line ("Instructions for an AI coding assistant working in this repository / applying these changes"), generated-by link.
2. **Project facts**: test framework, runner command per project (`dotnet test`, `dotnet run --project`, Fixie), TFM/SDK, CPM, the list of Verify packages and versions, the extensions and their depth, the interaction decisions taken (from `InteractionResult`s, as imperative statements: "Do not enable recording in Verify.SqlServer; Verify.EntityFramework owns it").
3. **Verify context** – the Verify `ai-usage` context-file template (Appendix A) with the framework-specific test command substituted and the inline-snapshot section dropped when not enabled (inline is still marked beta in the docs; it is not enabled by the wizard).
4. **Handling snapshot failures** – covered by the context template; the skill text (Appendix A) ships as its own file, `.claude/skills/verify-snapshot-testing/SKILL.md`, rather than being repeated here.
5. **For the Add flow only**: an ordered task list for the merge (add PackageVersion lines; add PackageReference lines; merge ModuleInitializer respecting order; copy tests; run; accept first snapshots after review), each pointing at the fragment file.
6. **Extension cheat sheet** – for each selected extension: the enable call, the 3–5 most important APIs (minimal samples' method signatures), the snapshot key names it uses (`ef`, `sql`, `httpCall`, `log`, `activity`), and its gotchas.
7. **Environment**: `DiffEngine_Disabled=true`; licence environment variables required; tools required.

The skill file (`.claude/skills/verify-snapshot-testing/SKILL.md`) is emitted verbatim from Verify's ai-usage doc.

---

## 13. Test framework specifics

Data table used by `ProjectFileGenerator` and `TestCodeGenerator` (values verified against `Verify/usages/*NugetUsage` and the Verify test projects):

| Framework | Packages | csproj properties | Test attributes | Runner | Notes |
|---|---|---|---|---|---|
| XunitV3 | `Verify.XunitV3`, `xunit.v3` | `OutputType Exe` | class, `[Fact]` | `dotnet test` (MTP) | sample from `Verify.XunitV3.Tests/Snippets/Sample.cs` |
| NUnit | `NUnit`, `Verify.NUnit`, `NUnit3TestAdapter` | `OutputType Exe`, `EnableNUnitRunner true` | `[TestFixture]`, `[Test]` | `dotnet test` (MTP) | STA: `[Apartment(ApartmentState.STA)]` for Xaml |
| TUnit | `TUnit`, `Verify.TUnit` | none (TUnit sets OutputType) | `[Test]` | `dotnet run --project` or `dotnet test` | `[Explicit]` exists for opt-in tests |
| MSTest | `MSTest.TestAdapter`, `MSTest.TestFramework`, `Verify.MSTest` | `OutputType Exe`, `EnableMSTestRunner true` | `[TestClass] partial`, `[TestMethod]`, `[assembly: UsesVerify]` | `dotnet test` (MTP) | source generator requires `partial` classes |
| Fixie | `Fixie`, `Verify.Fixie`, `Fixie.TestAdapter`, `Microsoft.NET.Test.Sdk` | none | plain public class/method | `dotnet test` (VSTest); no MTP; no `test.runner` in global.json | `TestProject.cs` convention required, plus the `[TestCase]` attribute it uses (Verify's `FixieSetup/TestCase.cs`). `DefaultDiscovery` only runs classes whose names end with `Tests`, so the sample class is `SampleTests` (snapshot `SampleTests.Test.verified.txt`); Verify's own Fixie `Sample` snippet would never run |
| Expecto | `YoloDev.Expecto.TestSdk`, `Expecto`, `FSharp.Core`, `Verify.Expecto` | `OutputType Exe`, `EnableExpectoTestingPlatformIntegration true`, `DisableImplicitFSharpCoreReference true`, fsproj | `[<Tests>] testTask` | `dotnet test` (MTP) or `dotnet run --project` | F# core sample only (D9); initialization in a `lazy` value (D9); snapshot `Tests.findPerson.verified.txt`. Verify.Expecto 33.1.1 needs FSharp.Core ≥ 11.0.100, newer than the SDK's implicit reference, so it is referenced explicitly. YoloDev.Expecto.TestSdk is pinned at 0.16.1: 1.0.0 sorts newer but requires Expecto < 10 |

The "explicit / skip" attribute per framework (used for paid-service tests): xUnit `[Fact(Skip = "…")]`, NUnit `[Explicit]`, TUnit `[Explicit]`, MSTest `[Ignore("…")]`, Fixie: a `#if` guard, Expecto: `ptestTask`.

---

## 14. Sponsor step

One step, both flows. Inputs and generated `Directory.Build.props` block (owner mode, prefix `Verify_`, exactly as `Verify/docs/maintenance-fee.md`):

- **GitHub sponsor account** (text; persisted). Hint: the organisation or user that sponsors VerifyTests; link to `https://github.com/sponsors/VerifyTests` and the tier table from the maintenance-fee doc.
- **Mode** cards: `Sponsoring` / `Exempt` / `Private arrangement` / `Ignore` / `Decide later`.
  - Sponsoring: emits `<Verify_GitHubSponsorAccount>{account}</Verify_GitHubSponsorAccount>`. Checkbox "The sponsorship is new (started after the referenced Verify version was packed)" → emits `<Verify_SponsorshipStart>{date}</Verify_SponsorshipStart>` with the date defaulting to **today** (D8), editable, validated `yyyy-MM-dd`, warning if in the future (SC028). Checkbox "The sponsorship is private" → `<Verify_SponsorshipPrivateUntil>{yyyy-MM}</Verify_SponsorshipPrivateUntil>` (default today + 12 months, max 12).
  - Exempt: radio OpenSource / SmallRevenue / MaintainerConsulting; until-month default today + 12 months (6 for MaintainerConsulting), validated with `MonthBound` (copied from SponsorCheck); emits `Verify_SponsorshipExemption` + `Verify_SponsorshipExemptionUntil`.
  - Private arrangement: `Verify_SponsorshipLicensedUntil` (max 12 months).
  - Ignore: `Verify_SponsorshipLicenseIgnored=true` with the SC023 warning text.
  - Decide later: the props file contains the whole block commented out with each option and the SC021 note, so the first build's error message and the file agree.
- The generated block always carries a comment linking to `docs/maintenance-fee.md` and to the SponsorCheck package wizard (`https://simoncropp.github.io/SponsorCheck/package/Verify`).
- Reuse `ConsumerConfigGenerator`'s outcome prose for owner mode (copy the relevant `BuildOutcome` branches; keep the SC codes) so the guide's "Expected build outcome" sentence is accurate.
- **Other sponsorship owners.** Some packages the plan can reference carry their own sponsorship gate, built on the same SponsorCheck mechanism.
  - The `Pandoc` package, a dependency of Verify.Pandoc, gates on owner `Papyrine`, with no severity overrides.
  - It reads `Papyrine_GitHubSponsorAccount`, `Papyrine_SponsorshipStart`, `Papyrine_SponsorshipExemption[Until]`, `Papyrine_SponsorshipLicensedUntil` and `Papyrine_SponsorshipLicenseIgnored` (`%USERPROFILE%\.nuget\packages\pandoc\6.0.2\buildTransitive\Pandoc.targets:16, 96-103`).
  - `PackageRequirement.SponsorOwner` records such owners (prefix, account, landing URL).
  - The step renders one block per owner present in the plan: VerifyTests always, others only when their package is selected. The modes are the same; non-Verify owners default to "Decide later".
  - Integration states (17.4) choose a mode that builds. The Verify.Pandoc repo itself uses `Papyrine_SponsorshipLicenseIgnored=true`.
  - The integration compile catches any gated package the registry misses.

---

## 15. NuGet version sourcing

### 15.1 Baked versions

`Wizard.Core/Versions/package-versions.json`: `{ "updated": "2026-09-22", "packages": { "Verify.XunitV3": "33.1.0", … } }` covering every package id the registry can emit (Verify adapters, test frameworks, every extension package, `EfLocalDb.*`, `LocalDb`, helper packages such as `Microsoft.EntityFrameworkCore.SqlServer`, `Avalonia.*`, `Selenium.WebDriver.ChromeDriver`, `Microsoft.AspNetCore.Mvc.Testing`, `Morph.Skia`, `Fixie.TestAdapter`, `Microsoft.NET.Test.Sdk`, `FSharp.Core`). Seed values: from the extension repos' `Directory.Build.props` `<Version>` (catalogue) and the Verify/SponsorCheck `Directory.Packages.props`. `verify.tool` (Verify.Terminal) has no version in its repo (MinVer); seed from nuget.org. The file also has a `pinned` map of package id to reason, for packages where the newest stable is wrong (`YoloDev.Expecto.TestSdk`, section 13); the refresh (15.2) and the runtime lookup (15.3) leave pinned packages alone.

As built (phase 4): the generator snapshot tests do not use the baked file. They use a fixed set with every package at 1.0.0, because a weekly refresh would otherwise change a couple of hundred snapshots each time, and the refresh's own diff of this file is where a version change is reviewed. The integration tests use the baked file, which is what they have to restore.

### 15.2 Weekly refresh workflow

`refresh-versions.yml` (schedule + `workflow_dispatch`): runs `dotnet run --project src/Wizard.Tests -- --treenode-filter "/*/*/PackageVersionsRefresh/*"` or a tiny console project `src/Wizard.VersionRefresh` that, for each package id, GETs `https://api.nuget.org/v3-flatcontainer/{id}/index.json`, picks the newest version without a `-` label (NuGet semver compare; use `NuGet.Versioning` in the refresh tool only), rewrites the JSON, and opens a PR with `peter-evans/create-pull-request`. Tests then re-run against the new versions (including the integration compile, 17.4), so a breaking bump is caught before deploy.

As built (phase 4):

- The tool is `src/Wizard.VersionRefresh`. Its rewrite is a pure function of the file and the version lists, tested without a network. It keeps the file's own format, so a refresh diff shows only the versions that moved, and it moves `updated` only when a version does, so a quiet week opens no pull request. A package nuget.org does not answer for keeps its version and is named in the pull request.
- "Newest stable" is `StableVersion` in Core rather than `NuGet.Versioning`, so the refresh and the browser's lookup share one rule and the browser downloads nothing extra.
- A pull request opened with `GITHUB_TOKEN` does not trigger other workflows, so `integration.yml` would never run on it. The refresh workflow builds every generated solution itself, on Windows, before opening the pull request, and puts the result in its body and title. A failing bump is still opened, marked, rather than lost.
- The flat container index lists unlisted versions too. Registration would exclude them at the cost of a much larger response; accepted, since unlisting a newest release is rare.

### 15.3 Runtime refresh

`PackageVersionLookup` (Web) mirrors `PackageLookup.LatestVersion` from SponsorCheck but filters prereleases. On the output step, for the packages in the plan only, fire the lookups in parallel with a 5 s budget; success replaces the baked version in the plan and re-renders; failure keeps the baked value and the guide says "versions as of {updated}". Never block output on the network. Playwright tests route `https://api.nuget.org/**` through `FakeNuGetFeed` (extended to serve `index.json` per package id) and bunit tests use a stub `HttpClient`, exactly as SponsorCheck does.

As built (phase 4): the output appears straight away with the baked versions and is regenerated when the answers arrive. Only the packages the output names are asked about (`Plan.EmittedPackages`), and answers are kept for the session, so changing a choice asks only about what is new. The guide says the versions are live only when every package answered; otherwise it gives the baked date. The step shows which it is, and marks it with `data-versions` (`pending`, `live`, `baked`), which the screenshot tests wait on. The fake feed answers every package with the same list, so screenshots never show a real version.

---

## 16. Web app implementation

- Copy `Program.cs`, `App.razor`, `index.html`, `app.css`, fonts, `interop.js`, `ClipboardService` from SponsorCheck.Web; rename the JS namespace to `verifyWizard`; add `downloadFile(name, contentType, base64OrBytes)` (creates a `Blob`, `URL.createObjectURL`, clicks an `<a download>`, revokes) and `storageGet/storageSet/storageRemove`.
- `DownloadService.DownloadAsync(string fileName, string contentType, byte[] bytes)` uses `IJSRuntime.InvokeVoidAsync("verifyWizard.downloadFile", …)` with a `DotNetStreamReference` for the zip (avoids base64 overhead for larger zips).
- `MarkdownView.razor`: `Markdig.Markdown.ToHtml(markdown, pipeline with AdvancedExtensions)` into a `MarkupString`; code blocks get the `CodeBox` copy button through a small JS enhancement after render (or render the guide from a structured model instead of markdown when simpler; the markdown must exist anyway for copy/download).
- Layout: `MainLayout` grid with the breadcrumb rail (7.4) as the left column on flow pages; Home has no rail.
- Theming: keep SponsorCheck's CSS variables; swap the primary colour to Verify's icon palette; light/dark via `prefers-color-scheme` as today.
- `WizardLinks` (site base, flow URLs, `AddExtension(extensionId)`; in `Wizard.Core`, because generated files link back to the wizard) and `DocLinks` (Verify docs URLs used in the guide) as constants classes, with an anti-rot test that each `DocLinks` target exists in the local Verify clone when available (opt-in) and that anchors resolve (GitHub slug rule from `RepoContractTests.DocLinkAnchorsResolveToHeadings`).
- Keep everything the tests need selectable by stable ids/classes: `#os-Windows`, `#tf-XunitV3`, `.extension-card[data-id=EntityFramework] input`, `.interaction-notice[data-rule=ef-sql-recording]`, `button.primary`, `.breadcrumb li.done`, `#sponsorAccount`, `button.download-zip`.

---

## 17. Tests

`Wizard.Tests` (TUnit, `OutputType Exe`, `PublishBlazorForTests` target copied from SponsorCheck so Playwright serves the real published output).

### 17.1 Generator snapshot tests (fast, always run)

For a matrix of `WizardState`s (each test framework with core only; each framework with DiffPlex + Http; the EF + SqlServer + LocalDb combination with each `ef-sql-recording` choice; every extension alone at Minimal and at Verbose; the "everything cross-platform and licence-free" set; the Windows set; each sponsor mode; each build server; Add flow with existing DiffPlex + adding EF; Add flow with existing SqlServer + adding EF), verify: the guide markdown, the AI markdown, the `ModuleInitializer.cs`, the csproj files, `Directory.Packages.props`, the file list of the zip. Use `VerifierSettings.UseStrictJson` off (default), `Verify.DiffPlex`, and `UniqueForRuntime` is not needed. Freeze `today` via an injected `DateOnly`.

### 17.2 Registry tests

Invariants from 9.2; ordering solver has no cycles for the full set; every interaction rule fires for its declared combination and not for the singletons; the OS diff-tool lists equal `DiffEngine.Definitions.Tools` filtered by OS support and `IsMdi == false` (the `WizardGen` logic).

### 17.3 UI tests

bunit: each step component (validation gates Next; choices bind), `Breadcrumb` (values, click-back, future steps disabled), `WizardStateUrl` round trips through the page (`NavigationManager` fake), storage seeding precedence (URL beats storage). Playwright screen snapshots (PNG + HTML) per screen for the three flows, plus end-to-end journeys: New flow to zip download (intercept the download and assert the zip contains `ModuleInitializer.cs`), Add flow from `/add/EntityFramework` with a pre-set `localStorage` "existing" list containing `SqlServer` (assert the `ef-sql-recording` notice appears), share-link round trip (copy URL, open in a new page, same state), "Forget everything".

### 17.4 Integration: the zip compiles and the tests run (opt-in, CI)

`Integration/GeneratedSolutionTests.cs`, marked `[Explicit]` and run by `integration.yml` (`--treenode-filter`): for a curated set of states, generate the solution, extract to a temp directory, run `dotnet build` and then the framework's test command with `DiffEngine_Disabled=true`, and assert the build succeeds and the only failures are Verify "New" snapshot failures (the core sample passes because its verified file is shipped). Matrix: ubuntu for the cross-platform set (every framework; extensions: DiffPlex, Http, AspNetCore, SystemJson, NewtonsoftJson, Yaml, NodaTime, Ulid, MailMessage, SendGrid, CsvHelper, Sep, ClosedXml, OpenXml, PdfPig, PDFium, QuestPDF, ImageSharp, ImageHash, AngleSharp, Bunit, Blazor, Moq, NSubstitute, FakeItEasy, Mockly, MassTransit, NServiceBus, Wolverine, Brighter, MicrosoftLogging, Serilog, ZeroLog, OpenTelemetry, Diagnostics, SourceGenerators, ICSharpCodeDecompiler, ReadableExpressions, ParametersHashing, Assertions, Quibble, Flurl, Playwright with browser install); windows for the Windows set (WinForms, Xaml, Phash, LocalDb + EntityFramework + SqlServer with SqlLocalDB available on `windows-latest`). Excluded from CI: Aspose, Syncfusion, EmailPreviewServices (licences), Cosmos (emulator), RavenDB (server download; try it, drop if flaky), ImageMagick pdf (Ghostscript install is possible via choco on Windows: include), Pandoc (choco/apt install is possible: include), Avalonia (needs a display? Avalonia headless works without one: include). This test is the guarantee behind "working bits" and behind the weekly version bump. Per extension the assertion is that `dotnet build` succeeds: the samples are copied from readmes, which drift, and only the compiler notices. Running them is not asserted per extension, because most need a database, a browser or a licence; the core solution, which ships its one verified file, is what asserts a passing run. The combination cases (A16) assert the build too, because that is where a wrong ordering or a method two packages both define shows up. These combinations run in addition to the per-extension set:
- Blazor with the core sample in one project, with the order forced so `Sample` runs first (Blazor's `Custom` shape, 9.1).
- QuestPDF + PdfPig with verbose samples (`settings-method-ambiguity`).
- Pandoc with `Papyrine_*` properties (14).
- Flurl + Http (`flurl-http`).
- ImageMagick comparer-only + PdfPig (11.1).

### 17.5 Content anti-rot (opt-in, network)

Diff each `Content/*.md` against `https://raw.githubusercontent.com/VerifyTests/Verify/main/docs/mdsource/<file>` and each extension's readme-derived sample against its repo, reporting drift as a failing test in a scheduled workflow only (never blocks deploy).

---

## 18. CI and deployment

### 18.1 deploy.yml

Copy `SponsorCheck/.github/workflows/deploy-blazor.yml` and change: paths to `src/Wizard.Web/**`, `src/Wizard.Core/**`, `src/Wizard.Tests/**`, props, `global.json`; `dotnet build src/Wizard.Tests -c Release`; Playwright install from `src/Wizard.Tests/bin/Release/net10.0/playwright.ps1`; `dotnet run --project src/Wizard.Tests -c Release --no-build -- --no-ansi --no-progress` (fast tests only; integration excluded by default); upload `**/*.received.*` on failure; `dotnet publish src/Wizard.Web -c Release -o publish`; `sed -i 's|<base href="/" />|<base href="/Wizard/" />|g' publish/wwwroot/index.html`; `.nojekyll`; `cp index.html 404.html`; `actions/upload-pages-artifact` + `actions/deploy-pages`. A `RepoContractTests.WizardLinksMatchTheDeployedSite` equivalent asserts `WizardLinks.Base` path equals the base href in the workflow. The test job runs on windows-latest, where the screenshot baselines are made. Only Windows is supported for the tests; Linux and macOS render sub-pixel differences that are not worth chasing. The publish and deploy jobs run on ubuntu-latest, because they compare nothing.

### 18.2 integration.yml

Triggered on pull requests touching `Wizard.Core` or `package-versions.json`, nightly, and manually. One job on windows-latest running the 17.4 tests with the tool installs they need (Playwright, Ghostscript and pandoc via choco; SqlLocalDB is preinstalled). Only Windows is supported for running this repo's tests, in CI and locally; macOS and Linux are not targets.

### 18.3 refresh-versions.yml

Section 15.2. Runs Monday 06:00 UTC.

### 18.4 Repository settings to apply manually

Enable GitHub Pages with source "GitHub Actions"; grant `pages: write`, `id-token: write`; allow Actions to create pull requests (for the refresh workflow). The last is Settings → Actions → General → Workflow permissions → "Allow GitHub Actions to create and approve pull requests"; without it the refresh runs, but opening its pull request fails.

---

## 19. Changes in the Verify repository

Do these after the site is live:

1. `readme.source.md`: point "Getting started wizard" at `https://verifytests.github.io/Wizard/` (keep the heading), and add one line under "Plugins" linking to the add-extension flow.
2. Each extension readme (optional, incremental): a "Add via the wizard" link to `https://verifytests.github.io/Wizard/add/<Id>`.
3. Retire `docs/wiz`: replace the 508 files with a single `docs/wiz/readme.md` that links to the site, and delete `src/Verify.Tests/Wizard/*` plus the `mdsnippets` wiring in `claude.md` ("Getting Started Wizard" section). Redirect map: old file name `Windows_Rider_Cli_XunitV3_GitHubActions.md` ↔ `/new?os=Windows&ide=Rider&cli=Cli&tf=XunitV3&ci=GitHubActions&step=output`; generate a table of those links into the new `docs/wiz/readme.md` so existing inbound links still land somewhere useful (the enum names were kept identical for this reason).
4. `Verify/usages/*NugetUsage` stay as the compile check for the GUI package lists; the wizard's registry test can also read them when the Verify clone is present locally (opt-in) to catch drift in the adapter package lists.
5. Suggested fixes to Verify core and the extensions are listed separately in section 22. None is a prerequisite for the site.

---

## 20. Implementation phases

Each phase ends with green tests and a deployable site. Estimated effort is relative.

Status: Phase 0 done and deployed. Phase 1 done: flow A without the tech and extension steps. Phase 2 done: the registry, the interaction rules, the ordering solver, the extension and options steps. Phase 3 done: the tech stack step and its suggestions, flows B and C, `/add/{Id}` deep links, the browser's memory, and the add-flow download. Phase 4 done: the weekly refresh workflow and tool, and the live nuget.org lookup.

**Phase 0 – Scaffold (small).** Repo layout (4), props, `global.json`, `nuget.config`, empty `Wizard.Core`/`Web`/`Tests`, copied SponsorCheck plumbing (index.html, css, fonts, interop.js, `PublishedWizard`, `WebTestContext`, `ModuleInitializer`), Home page with three cards, `deploy.yml` deploying the placeholder to Pages. Verify the base href and 404 fallback work at `https://verifytests.github.io/Wizard/`.

**Phase 1 – Port the existing wizard (medium).** `WizardState`, enums, `WizardStateUrl`, `Breadcrumb`, flow A steps 1–5 and 9–10 without extensions, `DocsGenerator` producing the same content as today's terminal pages (Appendix C), `ZipBuilder` producing the core solution for each framework, `AiContentGenerator` with the Verify ai-usage content, sponsor step (14). Snapshot tests for the six frameworks × four build servers; Playwright screens; integration compile for the six core solutions. At the end of this phase the site already supersedes `docs/wiz`.

**Phase 2 – Registry and interactions (large).** `ExtensionDefinition` data for every extension in Appendix B (copy samples from the catalogue), `InteractionRules`, ordering solver, `ModuleInitializerGenerator`, per-extension test file generation at both depths, Windows test project split, extension and options steps, extension sections in the guide and AI markdown. Snapshot tests per extension; integration compile for the cross-platform set.

**Phase 3 – Add flows and tech seeding (medium).** `Techs`, `TechSuggestions`, tech step, "already using" step, flows B and C, `/add/{Id}` deep link, `localStorage` persistence, add-flow zip layout and AI merge instructions. Tests for precedence rules and journeys.

**Phase 4 – Versions (small).** `package-versions.json`, `PackageVersionLookup`, `refresh-versions.yml`, `integration.yml` on version PRs.

**Phase 5 – Polish and hand-over (small).** Content anti-rot tests, readme and claude.md for this repo, Verify repository changes (19), pin the site in the VerifyTests org profile.

---

## 21. Risks and open questions

- **Version drift breaks generated code.** Mitigated by the weekly refresh PR gated by the integration compile; a failing bump stays a PR until the registry sample is fixed.
- **Sample code must match APIs.** All samples are copied from readmes/tests that are themselves snippet-verified; the integration compile catches drift. Known readme inaccuracies to avoid: Verify.Diagnostics readme references a non-existent `RecordingActivityListener.Start()` (use `Recording.Start()`); Verify.MicrosoftLogging prose references `LoggerRecording`/`LoggerProvider` (use `Recording.Start()`, `RecordingLogger`, `RecordingProvider`); Verify.Phash readme shows a parameterless `RegisterComparer()` that does not exist; Verify.ImageSharp.Compare readme/xmldoc copy-paste from ImageHash (threshold is absolute error, default 5); Verify.CsvHelper/Verify.Sep `TranslateCsvColumn` vs `TranslateCsvColumns` naming; Verify.Wolverine `Broardcasted` spelling.
  - Further inaccuracies found when the catalogue was re-checked against source:
    - Verify.Http's readme uses `HttpRecording.StartRecording()`; use `Recording.Start()`, which records under `httpCall`.
    - Verify.ImageMagick's readme says `Initialize` registers comparers; it registers converters only. Its `PdfPassword` snippet throws without `.ExcludeTargets("pdf")`.
    - Verify.EntityFramework's exception text names a non-existent `Enable()`.
    - Verify.Phash registers png only.
    - Verify.NodaTime's "disable scrubbing" example still shows scrubbed values; it also needs `DontScrubDateTimes()`.
    - Verify.Brighter records every `Post` as `Publish`, and `Publishes` throws after a Post. Samples do not use `Posts`/`Publishes` (retired by U4).
    - Verify.Yaml emits mapping keys in reverse order; the sample comment says so (retired by U12).
    - Verify.ImageSharp's per-test `SsimThreshold()` does nothing unless `Initialize(ssimThreshold < 1)` was called.
  - The full list, with upstream fixes, is in section 22.
- **Trimming.** Markdig and `System.Text.Json` under `TrimMode=full`: use a source-generated `JsonSerializerContext`; add `Wizard.Core` to the trimmer root descriptor if reflection surprises appear.
- **Zip size and fixtures.** Keep fixtures small; the zip is built in memory.
- **Expecto and Fixie coverage** is deliberately thinner (D9, D10). If demand appears, extend `TestCodeGenerator` with F# samples.
- **Sponsor start date semantics** (D8) should be confirmed with the requester; the current interpretation follows the SponsorCheck verifier's rules.
- **`Verify.Bunit` stable version.** The repo is at `14.1.0-beta.1`; nuget.org has `14.0.0` stable. The "newest non-prerelease" rule picks 14.0.0; the wizard's own tests use 14.0.0 too.
- **RavenDB and Cosmos samples** cannot run without a server; they are generated with a skip attribute and instructions.
- **`Verify.Sample` clone is mid-merge locally** (unresolved conflict markers in its `Directory.Packages.props`); irrelevant to this plan but do not copy from it.

---

## 22. Suggested changes to Verify and its extensions

These fixes were found on 2026-09-22, when the catalogue was re-checked against current source in every extension repo (including Verify.Syncfusion) and in LocalDb.
- Every item was checked in code, and paths are relative to each repo.
- None of this work is part of phases 0–5 (see 22.7).
- The workarounds for these defects live in the sections above; each names the id below in `RetiredBy`.

### 22.1 Verify core

| Id | Change | Evidence | Breaking | Retires |
|---|---|---|---|---|
| C1 | Sort plugin files ordinally before initializing them, so conflicting registrations resolve the same way on every OS | `src/Verify/VerifierSettings_PluginConvention.cs:34` (`Directory.EnumerateFiles`, unordered) | no | order-dependent outcomes in every exclusive group |
| C2 | Look up the plugin type case-insensitively (`GetType(typeName, false, ignoreCase: true)`); report a referenced `Verify.*.dll` that has no plugin type | same file `:173-176`, `:203` | no | QuestPDF being skipped; makes the AngleSharp and Blazor gaps visible (`plugin-not-discovered`) |
| C3 | Record which assembly registered each stream/string converter and comparer. Report cross-plugin overwrites from `VerifyChecks.Run()`; add an opt-in strict mode that throws | `src/Verify/Splitters/Settings_Extension.cs:26`; `src/Verify/Compare/SharedSettings.cs:32,39,46` | no (strict mode is opt-in) | group conflicts go unnoticed by users who don't use the wizard |
| C4 | Move `PagesToInclude` and `SkipPdfNormalization` into core as `VerifySettings`/`SettingsTask` methods with a context accessor (as `IsTargetExcluded` works). Extensions read the core value and mark their own copies `[Obsolete]` | defined by six packages, each with its own context key | no, with `[Obsolete]` forwarding | `settings-method-ambiguity` |
| C5 | Honour `Target.PerformConversion` for targets that converters produce | `src/Verify/InnerVerifier_Stream.cs:188-199`; compare `InnerVerifier_Inner.cs:371` | minor | `imagesharp-reencode` |
| C6 | Share ULID scrubbing, or add a keyed `Counter.Next<T>`, so Ulid and NUlid share numbering and `DontScrubUlids` exists once | `VerifyUlid.cs` and `VerifyNUlid.cs` are identical at `:19, 49-60` | coordinated major | `ulid-scrubber` group |
| C7 | Convention: extensions that use sub-namespaces ship `buildTransitive` `<Using>` items, as Verify.NUnit does | `src/Verify/buildTransitive/Verify.props:179` imports only `VerifyTests` | no | the sub-namespace `Usings` data (9.1) |
| C8 | Fix the link to the old Verify.AngleSharp.Diffing repo | `docs/mdsource/comparer.source.md:138` | no | — |

### 22.2 Bugs and wrong documentation (high priority)

| Id | Repo | Change | Evidence |
|---|---|---|---|
| U1 | Verify.Blazor | Make `VerifyTests.VerifyBlazor` public with `Initialized`/`Initialize()`, so both plugin discovery and an explicit call work. Keep the static-constructor fallback only for when nothing has run yet, and give it a message that points to the ModuleInitializer | `src/Verify.Blazor/Render.cs:5-6`, `VerifyBlazor.cs:3,19` |
| U2 | Verify.AngleSharp | Add `VerifyTests.VerifyAngleSharp` (`Initialized`, plus `Initialize(action?)` forwarding to `VerifyAngleSharpDiffing`). Release note: projects using `InitializePlugins()` start getting semantic html comparison | `src/Verify.AngleSharp/VerifyAngleSharpDiffing.cs:3` |
| U3 | Verify.QuestPDF | Until C2 ships, the readme says an explicit `VerifyQuestPdf.Initialize()` is required. Move the `QuestPDF.Settings.License` line into the `enable` snippet | `src/Verify.QuestPDF/VerifyQuestPdf.cs:3`, `src/Tests/ModuleInitializer.cs:17` |
| U4 | Verify.Brighter | Record `Post`/`PostAsync` as `CommandType.Post`. Add tests for `Posts` and `Publishes`. The snapshot key changes | `src/Verify.Brighter/RecordingCommandProcessor_Post.cs:12-45`, `RecordingCommandProcessor_Publish.cs:6-8` |
| U5 | Verify.SqlServer | Check a settable `RecordCommands` flag and `Recording.IsIgnored("sql")` for each event. Recording can then be disabled in any order, and `IgnoreNames("sql")` stops cloning commands. `Initialize(bool)` sets the flag, then initializes. Rewrite readme lines 221-240 | `src/Verify.SqlServer/VerifySqlServer.cs:9-26`, `Recording/Listener.cs:17-19` |
| U6 | Verify.EntityFramework | Same for EF: honour `Recording.IsIgnored("ef")` in the interceptor, and make `RecordCommands` and `Model` settable. Fix the exception text "wither … `Enable()`" | `src/Verify.EntityFramework/VerifyEntityFramework.cs:88,113-129`, `LogCommandInterceptor.cs:50-58` |
| U7 | Verify.ImageSharp.Compare | Rename the per-test `UseImageHash` to `UseImageSharpCompare` and mark the old name `[Obsolete]`. With both packages referenced, `UseImageHash(threshold: 85)` silently becomes "absolute error 85". Make the last registration win, or add `Initialize(threshold)`. Fix the xmldoc, readme and failure text copied from ImageHash | `src/Verify.ImageSharp.Compare/VerifyImageSharpCompare.cs:5,10,34-47,78` |
| U8 | Verify.ImageMagick | Add `Initialize(bool pdf, bool images, bool svg, bool comparers, double threshold, ErrorMetric metric)`. Readme: say `Initialize` registers converters but no comparers; add the Ghostscript requirement, webp and svg. Point the `PdfPassword` snippet at the tested sample: `#if DEBUG` hides the snippet that throws, and `#if Debug` (wrong case) never compiles | `src/Verify.ImageMagick/VerifyImageMagick.cs:7-30,59-70`, `src/Tests/Samples.cs:1,23-30`, `VerifyImageMagick_Pdf.cs:199-206` |
| U9 | Verify.ImageHash | Add `Initialize(threshold, algorithm)`. `RegisterComparers` marks the plugin initialized. Fix the readme wording about the default algorithm | `src/Verify.ImageHash/VerifyImageHash.cs:15-53` |
| U10 | Verify.Http, Verify.Diagnostics | In the readmes, `HttpRecording.StartRecording()` and `RecordingActivityListener.Start()` do not exist; use `Recording.Start()`. Name the `httpCall` key | Http `readme.md:321-323`, Diagnostics `readme.md:43-45` |
| U11 | Verify.ZeroLog | When a configuration already exists, fail with a clear message unless `AppendingStrategy.Synchronous` is set; the async appender runs outside the recording's AsyncLocal state. Document the `LogManager` takeover | `src/Verify.ZeroLog/VerifyZeroLog.cs:23-27` |
| U12 | Verify.Yaml | Remove `.Reverse()`, which emits mapping keys in reverse order | `src/Verify.Yaml/Converters/YamlMappingNodeConverter.cs:8` |
| U13 | Verify.MicrosoftLogging, Verify.Serilog, Verify.ZeroLog | Use `Recording.TryAdd` (or `IsEnabled => Recording.IsRecording()`), because `Recording.Add` throws for anything logged outside a recording. Make MicrosoftLogging's `Initialize()` safe to call twice | `RecordingLogger.cs:12-42`, `VerifySink.cs:12`, `VerifyAppender.cs:4` |
| U14 | Verify.Xaml | Readme requirements: the STA attribute per test framework, Windows, `net10.0-windows`, `UseWPF` | currently only in `claude.md:27` and `src/Tests/TheTests.cs:3` |

### 22.3 API changes that retire wizard rules (medium priority)

| Id | Repo | Change | Retires |
|---|---|---|---|
| U15 | LocalDb | Register the `chatbot_` scrub inside `LocalDbTestBase<T>.Initialize`. Make `Initialize` lazy so `--report-trx` works. Add a minimal docs snippet region instead of the whole test ModuleInitializer | the `ef-localdb` scrub step and `--report-trx` restriction |
| U16 | Verify.OpenTelemetry | Depend on Verify.Diagnostics and call its `Initialize()` if it hasn't run; add only `LogRecordConverter` (the other converters are byte-identical copies). Optional ActivitySource filter in both packages | `activity-listener` group |
| U17 | Verify.Aspose, Verify.Syncfusion, Verify.OpenXml, Verify.Pandoc, Verify.ImageSharp | Opt-in registration per format: `RegisterPdfConverter()`, `RegisterExcelConverters()` and so on, or `Initialize(bool excel, bool word, bool powerPoint)`; Pandoc `docx`/`rtf`; ImageSharp `registerStreamConverters`. `Initialize()` still registers everything | pdf, xlsx, docx and pptx groups become per-format choices; `png-converter` |
| U18 | Verify.PDFium, Verify.HeadlessBrowsers, Verify.Serilog | Static settings instead of `Initialize` parameters: `VerifyPDFium.Dpi`, `VerifyPlaywright.InstallBrowsers()`, and for Serilog a public `WriteTo.Verify()` sink plus `replaceGlobalLogger` | ordering edges; `logger-takeover` becomes a choice |
| U19 | Verify.Bunit | Add an `Initialize` option that skips the global `html` comparer. Ship 14.1.0 stable on Verify 33 | `bunit-anglesharp-comparer`; the `beta-package` pin |
| U20 | Verify.Quibble | Drop the `UseStrictJson` precondition: the comparer only sees `.json`, and can fall back when parsing fails. At minimum, check before setting `Initialized` | `quibble-strict-json` ordering |
| U21 | Verify.NServiceBus | Mark `Initialize(bool captureLogs)` `[Obsolete]` and add `Initialize()`. Document logging, `AddSharedHeader(s)` and the namespace | the `nservicebus-logging` note |
| U22 | Verify.Phash | Decode images with ImageSharp and target `net8.0`; System.Drawing is the only reason for `-windows`. Add `RegisterComparers(threshold)` for png, jpg, bmp and tiff | `windows-only` for Phash |
| U23 | Verify.Cosmos | Limit `IgnoreMembers("ETag")` to Cosmos types | `cosmos-etag` |
| U24 | ImageHash, ImageSharp.Compare, ImageMagick, Phash, ImageSharp | One shape for all image comparers: `Initialize(threshold)` and `RegisterComparers(...)` globally, `Use<Name>Comparer(...)` per test. Name the parameter `minSimilarity` or `maxDifference` according to the metric. Make comparisons inclusive, use the same extension set (`tif` as an alias), and consider a size-independent metric for ImageSharp.Compare | image-comparer UI text |
| U25 | Verify.DiffPlex, Verify.Assertions, logging extensions | `UseDiffPlex(params extensions)`; opt-in failure when `Assert<T>` never matched; an optional record name instead of the shared `log` | minor notes |

The image comparers today (context for U24):

| Package | Global setup | Per test | Default | Metric | Stricter when |
|---|---|---|---|---|---|
| ImageHash | `RegisterComparers` | `UseImageHash` | 95 | similarity % | higher |
| ImageSharp.Compare | `RegisterComparers` | `UseImageHash` | 5 | absolute error (depends on image size) | lower |
| ImageMagick | `RegisterComparers` | `ImageMagickComparer` | .005 | fuzz distortion | lower |
| Phash | `RegisterComparer(extension, …)` | `PhashCompareSettings` | .999 | cross-correlation | higher |
| ImageSharp | `Initialize(ssimThreshold)` | `SsimThreshold` | 1.0 (off) | SSIM | higher |
| Verify core | `UseSsimForPng` | — | .98 | SSIM | higher |

### 22.4 Naming and namespace consistency (at each repo's next major where breaking)

- **CsvHelper and Sep:**
  - Add `TranslateCsvColumns(this SettingsTask…)` and mark the singular `[Obsolete]`.
  - Rename CsvHelper's `VerifySep_Translate.cs`.
  - Fix the nullability of Sep's delegate.
  - Say in both readmes that the two packages can't be used together.
- **Ulid and NUlid:** say that they can't be used together, until C6 ships.
- **Wolverine:** rename `Broardcasted` to `Broadcasted` in the file, the API and the snapshots.
- **Namespaces:** move these into `VerifyTests.*` with obsolete shims:
  - `VerifyTestsAspose`, `VerifyTestsImageMagick` and `VerifyTestsPlaywright`.
  - QuestPDF's `ShouldIncludePage`, out of `VerifyQuestPDF`.
  - LocalDb: rename `EfLocalDbNunit` to `EfLocalDbNUnit`.
  - ICSharpCode.Decompiler's split namespaces: ship a `<Using>` now, merge at the next major.
- **NodaTime:** rename `DontScrub()` to `DontScrubNodaTimes()`.
- **PDFium:** honour `ExcludeTargets("pdf")`, mark `ExcludePdfDocument()` `[Obsolete]`, and add `VerifySettings` overloads.
- **EntityFrameworkClassic:** write queryable SQL as `.sql`, as EF Core does.

### 22.5 Documentation

Where to make these fixes:
- Most extension repos use MarkdownSnippets `InPlaceOverwrite`. Fix prose directly in `readme.md` outside snippet blocks, and snippet text in the `#region` under `src/Tests`.
- Verify.Blazor and Verify.Bunit have a `readme.source.md`.
- LocalDb uses `pages/mdsource`.

The fixes:
- **AngleSharp, Blazor, Bunit:** say that `HtmlPrettyPrint` needs Verify.AngleSharp and `using VerifyTests.AngleSharp;`. Fill the empty headings. Fix `Intitialized` in the sample component.
- **Aspose, Syncfusion:** show how to apply the licence, and add `ApplyLicense` helpers. Document `PagesToInclude`, `PdfPngDevice` and the other settings.
- **MicrosoftLogging:** replace `LoggerRecording`/`LoggerProvider` with `RecordingLogger`/`RecordingProvider`, and mention the namespace.
- **ICSharpCode.Decompiler:** `DontNormalizeIL` should be `DontNormalizeIl()`. Fix the typos. Document `ScrubComments`, `ScrubBinaryData` and `AssemblyToDisassemble`.
- **Phash:** remove the parameterless `RegisterComparer()`, say it is png only, and fix the intro.
- **QuestPDF:**
  - The comparer paragraph names ImageMagick, but the snippet uses SSIM.
  - Document the licence.
  - Document `SkipPdfNormalization`.
- **SqlServer:** the schema output include renders nothing (`readme.md:58-61`). Document `SchemaAsSql`/`SchemaAsMarkdown` and the namespace.
- **Serilog:** fix the typo. Say that the `custom` callback needs an explicit `Initialize` before `InitializePlugins`.
- **Flurl:** say it initializes Verify.Http itself, so a later explicit `VerifyHttp.Initialize()` throws.
- **Pandoc:** say it needs the pandoc executable and brings the Papyrine sponsorship gate.
- **SourceGenerators:** rewrite the sample as an `IIncrementalGenerator`, which drops the RS1042 suppression.
- **WinForms:** add requirements and the SSIM recommendation; fix the typos.
- **Smaller fixes:**
  - RavenDB: embedded-server test setup.
  - ReadableExpressions: the converter is inserted at index 0 and overrides other `Expression` handling.
  - EntityFramework: `IgnoreNavigationProperties` extends `VerifySettings`.
  - NodaTime: turning off scrubbing also needs `DontScrubDateTimes()`.
  - NServiceBus, MassTransit (DI harness) and Terminal (local tool manifest).
  - Undocumented options in DocNet, PdfPig and Sylvan.

### 22.6 Packaging and metadata (low priority)

- **Wrong metadata:**
  - Tags on MailMessage, NewtonsoftJson, Phash and ZeroLog.
  - ZeroLog's `nuget.md` link returns 404.
  - SendGrid's `RootNamespace`, tags and description are wrong, and it has no `nuget.md`.
  - OpenXml's `nuget.md` links to Sylvan; ReadableExpressions' links to NodaTime.
  - EmailPreviewServices' badge points at Verify.Ulid, and its `AssemblyVersion` is 0.1.0.
  - Terminal's repository URLs point at the pre-transfer repo.
  - Yaml says "YamlDotNey".
  - Cosmos sets `PackageRequireLicenseAcceptance`.
- **Target frameworks:**
  - AngleSharp: drop net6/net7, add net10.
  - Assertions and DocNet: drop end-of-life frameworks.
  - PdfPig: net8.0 is listed twice.
  - Quibble: add net8/net9.
  - RavenDB and Wolverine: net9 only.
  - WinForms and Xaml: add net8.0-windows.
  - ClosedXml: lists both net472 and net48.
- **Strong naming, where dependencies allow:** CsvHelper, DocNet, ImageHash, QuestPDF, Quibble, Sep, Sylvan, Wolverine.
- **Releases:** Verify.Flurl on Verify.Http 8; a stable Verify.Bunit (U19).
- **Hygiene:**
  - AspNetCore adds `ConflictResultConverter` twice and tracks a `.csproj.user` file.
  - Quibble has orphaned `.verified.json` files.
  - Avalonia forces the ColorPicker and DataGrid dependencies.
  - SourceGenerators' targets miss `.vb` and `buildTransitive`.

### 22.7 How to act on these

- Each item is done only on request: one branch per repo, with commits, pushes and PRs confirmed individually.
- Suggested order:
  1. C1 and C2: the smallest changes with the biggest effect.
  2. 22.2.
  3. 22.3, grouped by the rule each item retires.
  4. 22.4, at each repo's next major.
  5. 22.5 and 22.6, batched per repo.
- The wizard keeps every workaround marked `RetiredBy` until the release that fixes it is baked into `package-versions.json`.
  - Once that release ships, its minimum version is added to the workaround.
  - A registry test then fails while the workaround is still active past that version.

---

## Appendix A: static content to copy from Verify docs

Copy the current text of each into `Wizard.Core/Content/` (same file names) and render through `DocsGenerator`. All are under `D:\Code\VerifyTests\Verify\docs\mdsource\`:

| File | Used in |
|---|---|
| `testing-platform.include.md` | guide §3 |
| `implicit-usings.include.md` | guide §4 |
| `include-exclude.include.md` | guide §5 and `.gitignore` generation |
| `text-file-settings.include.md` | guide §5 and `.gitattributes`/`.editorconfig` generation (includes the VS Code `files.eol` note and the `autocrlf` guidance) |
| `mstest-marker.include.md` | MSTest |
| `fixie-convention.include.md` + `src/Verify.Fixie.Tests/TestProject.cs` | Fixie |
| `pure.include.md` | Rider / ReSharper `.editorconfig` |
| `rider-resharper-orphaned-process.include.md` | Rider / ReSharper (`.slnx.DotSettings`) |
| `build-server-githubactions.include.md`, `build-server-azuredevops.include.md`, `build-server-appveyor.include.md` | build server files and guide §13 |
| `ai-usage.source.md` | AI markdown and `SKILL.md` (strip the inline-snapshot beta sections unless enabled) |
| `maintenance-fee.source.md` | sponsor step texts and guide §14 |
| `plugins.source.md` | "Enabling plugins" explanation in the ModuleInitializer section |
| `plugin-list.include.md` | "Other community extensions" links (D20) |
| Sample tests: `src/Verify.XunitV3.Tests/Snippets/Sample.cs`, `Verify.NUnit.Tests/Snippets/Sample.cs`, `Verify.TUnit.Tests/Snippets/Sample.cs`, `Verify.MSTest.Tests/…/Sample.cs`, `Verify.Fixie.Tests/…/Sample.cs`, `Verify.Expecto.FSharpTests/Tests.fs`; `src/TargetLibrary/ClassBeingTested.cs` + `SampleModels.cs`; `src/Verify.XunitV3.Tests/Snippets/Sample.Test.verified.txt`; `VerifyChecksTests.cs` per framework; `VerifyBaseUsage.cs` (MSTest) | zip sources |
| `WizardGen.cs` texts for DiffEngineTray, DiffPlex, Verify.Terminal, Rider/ReSharper plugin paragraphs, diff-tool intro, CLI/GUI package lists | guide |
| Diff tool lists per OS (Windows/MacOS/Linux, non-MDI) as rendered in `docs/wiz/*_None.md` | guide §12 (verified by test against DiffEngine) |

---

## Appendix B: extension registry table

Summary of every selectable extension. Versions are the repo `Directory.Build.props` values at research time and are only seeds for `package-versions.json`. Full details (samples, verbose APIs, quotes) are in `plan-research/extension-catalogue-*.md`; file letter given in the last column.

| Id | Package(s) | Version | Initialize | Category / group | Platform, requirements, notes | Cat. |
|---|---|---|---|---|---|---|
| AngleSharp | Verify.AngleSharp | 5.1.2 | `VerifyAngleSharpDiffing.Initialize(action?)`; `HtmlPrettyPrint.All()` | Web; comparer for html/htm/svg | companion to Blazor/Bunit/HeadlessBrowsers/AspNetCore; not found by `InitializePlugins()` (1.3) | A |
| AspNetCore | Verify.AspNetCore (declares FrameworkReference Microsoft.AspNetCore.App itself) | 5.0.0 | `VerifyAspNetCore.Initialize()` | Web | net10 only; `UseSpecificControllers`, `ScrubAspTextResponse` | A |
| Aspose | Verify.Aspose | 5.28.0 | `VerifyAspose.Initialize()` | Documents; pdf/xlsx/docx/pptx groups | licence (`AsposeLicense`); namespace `VerifyTestsAspose`; `PagesToInclude`, `ExcludeTargets` | A |
| Assertions | Verify.Assertions | 0.3.0 | `VerifyAssertions.Initialize()` | Testing | `.Assert<T>(…)` fluent/instance/static | A |
| Avalonia | Verify.Avalonia + Avalonia.Headless.XUnit/NUnit + Avalonia.Themes.Fluent + Avalonia.Skia | 1.4.1 | `VerifyAvalonia.Initialize()`; `IncludeThemeVariant()` | UI; rendering | needs `[assembly: AvaloniaTestApplication]`, `UseSkia()`, `UseHeadlessDrawing=false`; xUnit/NUnit attributes only | A |
| Blazor | Verify.Blazor | 11.0.0 | `RuntimeHelpers.RunClassConstructor(typeof(Render).TypeHandle)` (9.1) | Web; blazor-renderer group | `Render.Component<T>(…)`; BL0005 suppressed by package; not found by `InitializePlugins()` | A |
| Brighter | Verify.Brighter | 2.0.0 | `VerifyBrighter.Initialize()` | Messaging | `RecordingCommandProcessor`; Post is recorded as Publish and `Publishes` throws after a Post, so samples avoid `Posts`/`Publishes` (U4) | A |
| Bunit | Verify.Bunit + bunit | 14.0.0 stable (repo 14.1.0-beta.1) | `VerifyBunit.Initialize(excludeComponent)` | Web; blazor-renderer group | bUnit v2 API; html comparer collides with AngleSharp | A |
| ClosedXml | Verify.ClosedXml | 1.4.0 | `VerifyClosedXml.Initialize()` | Documents; xlsx group | 3+ files per verification; `UniqueForRuntime` for binary | A |
| CommunityToolkitMvvm | Verify.CommunityToolkit.Mvvm | 1.1.0 | `VerifyCommunityToolkitMvvm.Initialize()` | UI | companion to Avalonia/WPF | A |
| Cosmos | Verify.Cosmos | 3.0.0 | `VerifyCosmos.Initialize()` | Data | needs Cosmos emulator; tests skipped by default | A |
| CsvHelper | Verify.CsvHelper | 0.2.0 | `VerifyCsvHelper.Initialize()` | Data; csv-scrubber group | `IgnoreCsvColumns`, `ScrubCsvColumns`, `TranslateCsvColumn(s)` | A |
| Diagnostics | Verify.Diagnostics | 1.0.0 | `VerifyDiagnostics.Initialize()` | Observability; activity-listener group | `Recording.Start()`; readme's `RecordingActivityListener` does not exist | A |
| DiffPlex | Verify.DiffPlex | 3.3.1 | `VerifyDiffPlex.Initialize(OutputType)` | DeveloperExperience | always suggested; `using VerifyTests.DiffPlex` | A |
| DocNet | Verify.DocNet | 3.6.0 | `VerifyDocNet.Initialize()` | Documents; pdf group | native pdfium; `UseSsimForPng(0.95)`; `PagesToInclude`, `SinglePage`, `PageDimensions`, `PreserveTransparency` | A |
| EmailPreviewServices | Verify.EmailPreviewServices | 1.0.0 | `VerifyEmailPreviewServices.Initialize(apiKey?)` | Email | paid API key (`EmailPreviewServicesApiKey`); slow; tests explicit; snapshots are webp | A |
| EntityFramework | Verify.EntityFramework (+ Microsoft.EntityFrameworkCore.SqlServer for the sample) | 15.4.1 | `VerifyEntityFramework.Initialize(model, recordCommands)` | Data | `EnableRecording()`, `Recording.Start()`, ChangeTracker, queryable → `.sql`, `IgnoreNavigationProperties`, descriptive aliases/parameters, `ReplayRecentMigrations`; EF+SqlServer rule | A |
| EntityFrameworkClassic | Verify.EntityFrameworkClassic | 15.4.1 | `VerifyEntityFrameworkClassic.Initialize()` | Data | EF6 | A |
| FakeItEasy | Verify.FakeItEasy | 2.1.0 | `VerifyFakeItEasy.Initialize()` | Mocking | `Fake.GetCalls`, `FakeManager` | B |
| Flurl | Verify.Flurl | 2.0.0 | `VerifyFlurl.Initialize()` | Web | depends on and initializes Verify.Http | B |
| Playwright | Verify.Playwright + Microsoft.Playwright | 3.1.1 | `VerifyPlaywright.Initialize(installPlaywright)` | Web; rendering | browser install; `PageScreenshotOptions`, `--disable-lcd-text` | B |
| Puppeteer | Verify.Puppeteer | 3.1.1 | `VerifyPuppeteer.Initialize()` | Web; rendering | `BrowserFetcher` download; not strong-named | B |
| Selenium | Verify.Selenium + Selenium.WebDriver.ChromeDriver | 3.1.1 | `VerifySelenium.Initialize()` | Web; rendering | chromedriver | B |
| Http | Verify.Http | 8.0.0 | `VerifyHttp.Initialize()` | Web | converters, `MockHttpClient`, `AddRecordingHttpClient`, `Recording.Start()`; `using VerifyTests.Http`; records under `httpCall` | B |
| ICSharpCodeDecompiler | Verify.ICSharpCode.Decompiler | 3.5.0 | `VerifyICSharpCodeDecompiler.Initialize()` | Compiler | `.il` output; `TypeToDisassemble`, `MethodToDisassemble`, `PropertyToDisassemble`, `AssemblyToDisassemble`, `DontNormalizeIl` | B |
| ImageHash | Verify.ImageHash | 2.1.3 | `VerifyImageHash.Initialize()` / `RegisterComparers(threshold, algorithm)` | Images; image-comparer group | similarity threshold (default 95, higher is stricter) | B |
| ImageMagick | Verify.ImageMagick | 3.10.0 | `VerifyImageMagick.Initialize()`; `RegisterComparers(threshold)`; `RegisterPdfToPngConverter()` | Images; pdf group and image-comparer group by role | Ghostscript for pdf; namespace `VerifyTestsImageMagick` for settings; `Initialize` registers converters only; roles by ordering (11.1) | B |
| ImageSharp | Verify.ImageSharp | 5.0.1 | `VerifyImageSharp.Initialize(ssimThreshold)` | Images; rendering + optional comparer | `EncodeAsPng` etc.; per-test `SsimThreshold` only when enabled globally | B |
| ImageSharpCompare | Verify.ImageSharp.Compare | 3.0.3 | `VerifyImageSharpCompare.Initialize()` / `RegisterComparers(threshold)` | Images; image-comparer group | absolute-error threshold (default 5, lower is stricter) | B |
| MailMessage | Verify.MailMessage | 1.1.1 | `VerifyMailMessage.Initialize()` | Email | | B |
| MassTransit | Verify.MassTransit | 2.3.0 | `VerifyMassTransit.Initialize()` | Messaging | `InMemoryTestHarness`, saga harness | B |
| MicrosoftLogging | Verify.MicrosoftLogging | 5.0.0 | `VerifyMicrosoftLogging.Initialize()` | Logging | `RecordingLogger`, `RecordingProvider.CreateLogger<T>()`, `Recording.Start()` | B |
| Mockly | Verify.Mockly | 1.0.1 | `VerifyMockly.Initialize()` | Mocking / Web | `HttpMock`, `RequestCollection` | B |
| Moq | Verify.Moq | 2.2.0 | `VerifyMoq.Initialize()` | Mocking | verify `Mock<T>`; `ScrubMember` | B |
| NServiceBus | Verify.NServiceBus | 12.2.0 | `VerifyNServiceBus.Initialize()` (`captureLogs` has no effect, 11.2) | Messaging | depends on MicrosoftLogging; `RecordingHandlerContext`, `MessageToHandlerMap`, shared headers; `using VerifyTests.NServiceBus` | B |
| NSubstitute | Verify.NSubstitute | 2.1.0 | `VerifyNSubstitute.Initialize()` | Mocking | `ReceivedCalls()` | B |
| NUlid | Verify.NUlid | 1.0.1 | `VerifyNUlid.Initialize()` | Scrubbing; ulid group | `DontScrubUlids()` | B |
| NewtonsoftJson | Verify.NewtonsoftJson | 1.1.0 | `VerifyNewtonsoftJson.Initialize()` | Serialization | JObject/JArray | C |
| NodaTime | Verify.NodaTime | 2.3.0 | `VerifyNodaTime.Initialize()`; `DontScrub()` | Serialization | `DontScrubNodaTimes()` | C |
| OpenTelemetry | Verify.OpenTelemetry | 1.0.0 | `VerifyOpenTelemetry.Initialize()` | Observability; activity-listener group | `LogRecord` via InMemoryExporter | C |
| OpenXml | Verify.OpenXml (+ optional Morph.Skia or Morph.ImageSharp) | 1.27.0 | `VerifyOpenXml.Initialize()`; `FontDirectory`, `UseLetterPageSize` | Documents; xlsx/docx/pptx groups | exactly one render backend; choice `openxml-render` none/skia/imagesharp | C |
| PDFium | Verify.PDFium | 1.4.1 | `VerifyPDFium.Initialize(dpi)` | Documents; pdf group | `ExcludePdfDocument()`, `SkipPdfNormalization()`; deterministic renders | C |
| Pandoc | Verify.Pandoc | 0.1.1 | `VerifyPandoc.Initialize()` | Documents; docx group | pandoc binary; outputs markdown | C |
| ParametersHashing | Verify.ParametersHashing | 1.0.0 | no-op | Testing | `.HashParameters()` per test | C |
| PdfPig | Verify.PdfPig | 2.6.0 | `VerifyPdfPig.Initialize()` | Documents; pdf group | managed; `PagesToInclude`, `PdfPigParsingOptions`, `SkipPdfNormalization`, `ExcludeTargets("pdf")` | C |
| Phash | Verify.Phash | 3.1.0 | `VerifyPhash.Initialize()`; `RegisterComparer(extension, …)` | Images; image-comparer group | Windows only (`net8.0-windows`); png only by default | C |
| QuestPDF | Verify.QuestPDF | 2.9.0 | `VerifyQuestPdf.Initialize()` | Documents; rendering | `QuestPDF.Settings.License = LicenseType.Community`; `PagesToInclude(count|delegate)`, `ExcludeTargets("pdf")`; class `VerifyQuestPdf` is not found by `InitializePlugins()` (1.3) | C |
| Quibble | Verify.Quibble | 2.1.1 | `VerifierSettings.UseStrictJson(); VerifyQuibble.Initialize()` | Serialization; json comparer | forces strict json project-wide | C |
| RavenDB | Verify.RavenDB | 2.1.0 | `VerifyRavenDB.Initialize()` | Data | embedded server for tests; net9 package | C |
| ReadableExpressions | Verify.ReadableExpressions | 0.1.0 | `VerifyReadableExpressions.Initialize()` | Compiler | converter at index 0 | C |
| SendGrid | Verify.SendGrid | 1.0.0 | `VerifySendGrid.Initialize()` | Email | | C |
| Sep | Verify.Sep | 0.2.0 | `VerifySep.Initialize()` | Data; csv-scrubber group | same API shape as CsvHelper | C |
| Serilog | Verify.Serilog | 3.4.0 | `VerifySerilog.Initialize(custom?)`; `IgnoreSourceContext<T>()` | Logging | takes over `Log.Logger` | C |
| SourceGenerators | Verify.SourceGenerators | 2.6.0 | `VerifySourceGenerators.Initialize()` | Compiler | package targets handle `.verified.cs`; `IgnoreGeneratedResult` | D |
| SqlServer | Verify.SqlServer | 12.2.0 | `VerifySqlServer.Initialize(recordCommands)` | Data | schema (`SchemaIncludes`, `SchemaFilter`, `SchemaAsSql`), recording under `sql`; EF rule | D |
| Sylvan | Verify.Sylvan.Data.Excel | 1.0.0 | `VerifySylvanDataExcel.Initialize()` | Documents; xlsx group | csv per sheet; `CsvDataWriterOptions`, `ExcelDataReaderOptions` | D |
| Syncfusion | Verify.Syncfusion | 3.1.0 | `VerifySyncfusion.Initialize()` | Documents; pdf/xlsx/docx/pptx groups | licence (`SyncfusionLicense`); `PagesToInclude`, `ExcludeTargets` | D |
| SystemJson | Verify.SystemJson | 2.0.0 | `VerifySystemJson.Initialize()` | Serialization | choice strict json | D |
| Terminal | dotnet tool `verify.tool` | (MinVer) | none | Tooling | `dotnet verify review|accept|reject`; working dir must contain `obj` | D |
| Ulid | Verify.Ulid | 1.0.1 | `VerifyUlid.Initialize()` | Scrubbing; ulid group | `DontScrubUlids()` | D |
| WinForms | Verify.WinForms | 6.0.0 | `VerifyWinForms.Initialize()` | UI; rendering | Windows only; `net10.0-windows`, `UseWindowsForms`; png only | D |
| Wolverine | Verify.Wolverine | 3.2.0 | no-op | Messaging | `RecordingMessageContext`, `AddInvokeResult<T>`; net9 package | D |
| Xaml | Verify.Xaml | 5.0.0 | `VerifyXaml.Initialize()` | UI; rendering | Windows only; `UseWPF`; STA apartment; xml + png targets | D |
| Yaml | Verify.Yaml | 0.1.0 | `VerifyYaml.Initialize()` | Serialization | YamlStream/YamlDocument/nodes; mapping keys emitted in reverse order (U12) | D |
| ZeroLog | Verify.ZeroLog | 2.0.0 | `VerifyZeroLog.Initialize()` | Logging | takes over `LogManager` | D |
| LocalDb | EfLocalDb.Xunit.V3 / EfLocalDb.NUnit / EfLocalDb.MSTest / EfLocalDb.TUnit (else EfLocalDb); LocalDb for raw SQL | 26.2.0 | `LocalDbTestBase<T>.Initialize()` after `InitializePlugins()` | Data | Windows only (SqlLocalDB); `ArrangeData/ActData/AssertData`, `VerifyEntity`, `VerifyEntities`, `[NewDb]`/`[PooledDb]`/`[SharedDb]`; scrub `chatbot_` | D |

---

## Appendix C: current wizard content, per section

What `WizardGen.AppendContents` emits today, in order, with the source of each fragment, so Phase 1 reproduces it exactly before extending it:

1. `AppendNugets` – CLI: hard-coded `dotnet add package` lists per framework (XunitV3: `Verify.XunitV3`, `xunit.v3`; NUnit: `NUnit`, `NUnit3TestAdapter`, `Verify.NUnit`; TUnit: `TUnit`, `Verify.TUnit`; Fixie: `Fixie`, `Verify.Fixie`; MSTest: `MSTest.TestAdapter`, `MSTest.TestFramework`, `Verify.MSTest`; Expecto: `YoloDev.Expecto.TestSdk`, `Expecto`, `Verify.Expecto`). GUI: the `*-nugets` snippets from `usages/*NugetUsage/*.csproj` (same packages with versions; Expecto adds `<PackageReference Update="FSharp.Core" …>`).
2. `AppendTestingPlatform` – `testing-platform.include.md` + per-framework `<PropertyGroup>` (skipped for Fixie; TUnit has no properties).
3. `AppendImplicitUsings` – `implicit-usings.include.md`.
4. `AppendConventions` – `include-exclude.include.md`, `text-file-settings.include.md`, `VerifyChecks{Framework}` snippet.
5. `AppendDiffEngineTray` – Windows only; `dotnet tool install -g DiffEngineTray`; run-at-startup link.
6. `AppendRider` / `AppendReSharper` – plugin links (`https://plugins.jetbrains.com/plugin/17240-verify-support` / `…17241-verify-support`), `rider-resharper-orphaned-process.include.md`, `pure.include.md`.
7. `AppendDiffPlex` – package add (CLI or `<PackageReference Include="Verify.DiffPlex" Version="*" />`), `VerifyDiffPlex.Initialize()`.
8. `AppendTerminal` – CLI only; `dotnet tool install -g verify.tool`.
9. `AppendSample` – `SampleTest{Framework}` snippet.
10. MSTest: `mstest-marker.include.md`; Fixie: `fixie-convention.include.md`.
11. `AppendDiffTool` – intro text + per-OS non-MDI tool links from `DiffEngine.Definitions.Tools`.
12. `AppendBuildServer` – `build-server-{server}.include.md` unless None.

Navigation today: `[Home](/docs/wiz/readme.md) > [Windows](Windows.md) > [Visual Studio](Windows_VisualStudio.md) > [Prefer CLI](…) > [XunitV3](…) > GitHub Actions`; display names: `Ide` → "Visual Studio", "Visual Studio with ReSharper", "JetBrains Rider", "Other"; `CliPreference` → "Prefer CLI", "Prefer GUI"; `BuildServer` → "AppVeyor", "GitHub Actions", "Azure DevOps", "No build server". Keep these display names.

---

## Appendix D: upstream defects the generated code works around

Re-checking the catalogue against current extension source found places where the plan as first
written would have generated code that fails or misleads. Each correction is folded into the section it
belongs to; this index exists so the `(plan A…)` references in the registry and the generators resolve,
and so a workaround can be removed once the upstream fix in section 22 ships. `RetiredBy` on an
`ExtensionDefinition` or an `InteractionRule` names that fix.

| Id | What is wrong | What the wizard does | Retired by |
|---|---|---|---|
| A1 | Plugin discovery looks for `VerifyTests.` plus the assembly name without dots, case sensitively, and silently skips a miss. It misses Verify.AngleSharp (`VerifyAngleSharpDiffing`), Verify.QuestPDF (`VerifyQuestPdf`) and Verify.Blazor (internal). | `ExtensionDefinition.DiscoveredByInitializePlugins` is computed from that rule; anything it returns false for carries an explicit call, and the guide says so. A registry test enforces it. | C2, U1, U2 |
| A2 | `Render`'s static constructor initializes Verify.Blazor, and initializing throws once any verification has run. Whether a Blazor test fails depends on test order. | The module initializer forces the constructor with `RuntimeHelpers.RunClassConstructor`. | U1 |
| A3 | Verify.ImageMagick's `Initialize()` registers converters and no comparers, and neither `RegisterComparers()` nor `RegisterPdfToPngConverter()` sets `Initialized`. | `Initialize()` is always emitted first, then `RegisterComparers(...)` when comparers are wanted. The `imagemagick-role` choice decides group membership. | U8 |
| A4 | Verify.ImageHash needs `Initialize()` before `RegisterComparers(...)`; Verify.ImageSharp.Compare needs the reverse, because its first registration wins. | Each extension's statements are ordered within its own block, and a registry test asserts the edges. | U7, U9, U18 |
| A5 | `VerifyNServiceBus.Initialize(captureLogs)` has no effect: discovery initializes Verify.MicrosoftLogging anyway, as a transitive dependency. | No choice is offered; the plain `Initialize()` is emitted. | U21 |
| A6 | `PagesToInclude` and `SkipPdfNormalization` are defined in namespace `VerifyTests` by DocNet, PdfPig, QuestPDF and Syncfusion. Two of them in one project makes the fluent call ambiguous (CS0121). | The `settings-method-ambiguity` rule warns, and the samples call the static form. | C4 |
| A7 | Extensions put types in sub-namespaces that core's `buildTransitive` usings do not import, and three of them define their own `SocketWaiter`. | `Usings` are per generated test file, never global. | C7 |
| A8 | Verify.Pandoc depends on `Pandoc`, and Verify.OpenXml on `Morph`. Both ship a SponsorCheck gate of their own, for owner `Papyrine`, so the build fails with SC021 until that owner is declared too. Worse, an owner's exemption list is its own: Papyrine offers `SmallRevenue` and `MaintainerConsulting` but no open source exemption, so a project exempt from Verify's fee on that ground is not exempt from this one, and claiming it fails with SC034. | `PackageRequirement.SponsorOwner` names the owner, its prefix and the exemptions it accepts. The generated `Directory.Build.props` repeats a declaration that is true for that owner — opting out and a private arrangement always are, an exemption only when the owner offers it — and otherwise emits that owner's own options commented out, with a `transitive-sponsorship` warning saying a decision is still needed. | — |
| A9 | `LocalDbTestBase<T>.Initialize()` throws under `--report-trx`, and never passes the EF model to Verify.EntityFramework. | The generated CI omits `--report-trx`; the explicit `VerifyEntityFramework.Initialize(GetDbModel())` stays. The base class ships only in the `EfLocalDb.<TestFramework>` packages, so the call is not emitted for Fixie or Expecto. | U15 |
| A10 | Verify.AspNetCore already declares the `Microsoft.AspNetCore.App` framework reference. | Not added a second time. | — |
| A11 | Verify.Flurl is built against Verify.Http 7.5.1. | Flurl and Http together are in the integration matrix. | — |
| A12 | Verify.Http records under `httpCall`, not `http`; every logging extension records under `log`, so `IgnoreNames` cannot separate them; `Recording.Add` throws outside a recording. | The `recording-bus` and `shared-log-name` rules say so, and the samples start a recording first. | U13 |
| A13 | Several combinations collide without being exclusive: ImageMagick and AngleSharp both compare svg, ImageMagick and ImageSharp both convert png, ImageSharp re-encodes png that another extension rendered, Cosmos ignores `ETag` on every type. | One rule each. | C5, U23 |
| A14 | Brighter records `Post` as `Publish`; Yaml reverses mapping keys; ZeroLog captures nothing with the default async appender; ImageSharp's per-test `SsimThreshold` does nothing unless enabled globally; Quibble sets `Initialized` before throwing about `UseStrictJson`. | The samples avoid the broken APIs, and the notes say why. | U4, U11, U12, U20 |
| A15 | Readmes document calls that do not exist, `HttpRecording.StartRecording()` and `RecordingActivityListener.Start()` among them. | The copies use the corrected form. | U10 |
| A16 | The cases where a wrong ordering only shows up at compile or run time, and, more broadly, samples copied from a readme that no longer compile. | Every extension gets a generated solution that has to build, plus those combinations. This found nine broken entries on its first full run, two of them APIs that are `internal` and so were never callable. | — |
