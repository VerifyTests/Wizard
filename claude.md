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

Building `Wizard.Tests` also publishes `Wizard.Web` into `src/Wizard.Tests/bin/<Configuration>/blazor-publish`
(the `PublishBlazorForTests` target). `PublishedWizard` serves that output from Kestrel for the Playwright
tests, with a `{*path}` fallback that mirrors GitHub Pages serving `404.html` for deep links.

## Layout

- `src/Wizard.Core`: models, registry, generators. A plain class library with no Blazor dependency, so
  generator tests stay fast. `GenerateWizardDefaults` in its csproj bakes the SDK version (from `global.json`)
  and the target framework (`GeneratedTargetFramework` in `src/Directory.Build.props`) into
  `WizardDefaults`. Generated solutions use those values.
- `src/Wizard.Web`: the Blazor UI.
- `src/Wizard.Tests`: TUnit tests. There are bunit component tests, Playwright screen snapshots (PNG and
  HTML), and `RepoContractTests` anti-rot checks.

## Conventions

- Snapshot everything the generators produce with Verify; generators are pure functions of their inputs.
- The bundled fonts in `wwwroot/fonts` keep screenshots identical across OSes.
  `RepoContractTests.ShippedFontsCoverRenderedText` fails if rendered text uses a character the fonts
  don't cover.
- Lambda parameters are `_` (nested lambdas get a descriptive name).
- No single-character locals other than loop counters.
- No abbreviations in identifiers.
- Never return a ternary; use an `if` that returns, then the fallback `return`.
- No `else` after a branch that returns.
