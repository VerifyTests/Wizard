# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

The Verify getting-started wizard: a Blazor WebAssembly app deployed to https://verifytests.github.io/Wizard/
by `.github/workflows/deploy.yml`. `plan.md` is the implementation plan and the record of every design
decision; read the relevant section before changing behaviour, and update it when a decision changes.
`plan-research/extension-catalogue-*.md` holds the per-extension research the registry is built from.

## Build & test

```pwsh
dotnet build src/Wizard.Tests --configuration Release
dotnet run --project src/Wizard.Tests --configuration Release --no-build
```

Filter to a single test (TUnit uses `--treenode-filter`, not `--filter`):

```pwsh
dotnet run --project src/Wizard.Tests --configuration Release --no-build -- --treenode-filter '/*/*/HomeTests/*'
```

`Integration/GeneratedSolutionTests` is `[Explicit]`. It writes a generated solution for each test framework to the temp directory, then runs `dotnet build` and `dotnet test` on it (restoring from nuget.org). Run it after changing anything in `Wizard.Core/Generation`:

```pwsh
dotnet run --project src/Wizard.Tests --configuration Release --no-build -- --treenode-filter '/*/*/GeneratedSolutionTests/*'
```

`Content/ContentDriftTests` is also `[Explicit]` and run weekly by `content-drift.yml`. It checks that
`Wizard.Core/Content/*.include.md` still equal Verify's originals, and snapshots the upstream files the
wizard adapts rather than copies (ai-usage, the core samples) under `Content/Upstream`. When one fails,
bring the wizard's copy in line with the upstream diff, then accept the snapshot.

Building `Wizard.Tests` also publishes `Wizard.Web` into `src/Wizard.Tests/bin/<Configuration>/blazor-publish`
(the `PublishBlazorForTests` target). `PublishedWizard` serves that output from Kestrel for the Playwright
tests, with a `{*path}` fallback that mirrors GitHub Pages serving `404.html` for deep links.

The Playwright tests share one browser context, so each page from `PublishedWizard.NewPage()` gets an
in-memory localStorage of its own: the wizard restores remembered answers on load (plan 8.2), and a shared
storage would let parallel tests leak into each other. A test about remembering between visits uses
`NewIsolatedPage()`, which has real storage in a context of its own, and closes that context when done.

## Layout

- `src/Wizard.Core`: models, registry, generators. A plain class library with no Blazor dependency, so
  generator tests stay fast. `GenerateWizardDefaults` in its csproj bakes the SDK version (from `global.json`)
  and the target framework (`GeneratedTargetFramework` in `src/Directory.Build.props`) into
  `WizardDefaults`. Generated solutions use those values.
  - `Registry/Extensions.<A-D>.cs` hold one `ExtensionDefinition` per extension, split by the catalogue
    file each was researched from. `Registry/InteractionRules.Data.cs` holds the combinations. Read
    `ExtensionDefinition.cs` before adding an entry: its xml docs are the contract, and `RegistryTests`
    enforces most of it.
  - Every package id an entry names needs a version in `Versions/package-versions.json`, and it must be
    one that exists on nuget.org. The catalogue records each repo's own `<Version>`, which is often the
    next unreleased one. `src/Wizard.VersionRefresh` rewrites that file with the newest stable versions
    (`refresh-versions.yml` runs it weekly); run it locally rather than editing versions by hand:
    `dotnet run --project src/Wizard.VersionRefresh -- src/Wizard.Core/Versions/package-versions.json summary.md`.
  - Generator snapshots use `GeneratorTests.Versions`, with every package at 1.0.0, so a refresh never
    changes them; the browser tests answer for nuget.org through `FakeNuGet`.
- `src/Wizard.Web`: the Blazor UI.
- `src/Wizard.Tests`: TUnit tests. There are bunit component tests, Playwright screen snapshots (PNG and
  HTML), and `RepoContractTests` anti-rot checks.

## Conventions

- Snapshot everything the generators produce with Verify; generators are pure functions of their inputs.
- Generated code is written in string literals, so nothing may leak the line endings of the file holding
  them. Split source with `ModuleInitializerGenerator.Lines`, which drops the carriage return.
- A registry sample has to compile for xUnit v3, NUnit, TUnit, MSTest and Fixie, so it is a method body
  only; the generator supplies the signature and the framework's attribute. `GeneratedSolutionTests`
  proves it by building a solution per extension.
- The bundled fonts in `wwwroot/fonts` keep screenshots identical across OSes.
  `RepoContractTests.ShippedFontsCoverRenderedText` fails if rendered text uses a character the fonts
  don't cover.
- Lambda parameters are `_` (nested lambdas get a descriptive name).
- No single-character locals other than loop counters.
- No abbreviations in identifiers.
- Never return a ternary; use an `if` that returns, then the fallback `return`.
- No `else` after a branch that returns.
