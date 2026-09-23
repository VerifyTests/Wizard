# Verify Wizard

A getting-started wizard for [Verify](https://github.com/VerifyTests/Verify), hosted at
https://verifytests.github.io/Wizard/.

It replaces the static markdown pages in `Verify/docs/wiz`. For a new project it asks for the operating
system, IDE, test framework, build server and optionally the tech stack. It then produces:

 * a guide rendered in the page,
 * a downloadable zipped solution that builds and runs,
 * markdown instructions for an AI coding assistant.

A second entry point adds Verify plugins to an existing project, and explains how they interact with
the plugins already in use.

The site is a Blazor WebAssembly app that runs entirely in the browser; nothing is sent to a server.

The implementation plan, including every design decision, is in [plan.md](plan.md).


## Running locally

```
dotnet run --project src/Wizard.Web
```

Then browse to http://localhost:5179.


## Tests

```
dotnet build src/Wizard.Tests --configuration Release
dotnet run --project src/Wizard.Tests --configuration Release --no-build
```

The test project publishes the Blazor app after it builds. The Playwright tests serve that published
output and drive it in Chromium.

Two explicit suites need network, and are run by workflows rather than on every build:

```
dotnet run --project src/Wizard.Tests --configuration Release --no-build -- --treenode-filter "/*/*/GeneratedSolutionTests/*"
dotnet run --project src/Wizard.Tests --configuration Release --no-build -- --treenode-filter "/*/*/ContentDriftTests/*"
```


## Linking to the wizard

Every page's address holds all the answers given so far, so any point in the wizard can be bookmarked or
shared. Three entry points are stable:

 * https://verifytests.github.io/Wizard/new starts a new project.
 * https://verifytests.github.io/Wizard/add adds plugins to an existing project.
 * `https://verifytests.github.io/Wizard/add/{PluginId}` adds one plugin, already selected. An
   plugin's readme can link here, for example https://verifytests.github.io/Wizard/add/EntityFramework.

The browser remembers the tech stack, the plugins already in use and the sponsor answers between
visits. The home page has a button that forgets them.


## Adding a plugin

 1. Add an `PluginDefinition` to the `src/Wizard.Core/Registry/Plugins.<A-D>.cs` file for its
    catalogue. The xml docs in `PluginDefinition.cs` describe each member, and `RegistryTests` checks
    most of them.
 2. Add each of its packages to `src/Wizard.Core/Versions/package-versions.json` with any version, then
    run the refresh tool, which moves every package to its newest stable version:
    `dotnet run --project src/Wizard.VersionRefresh -- src/Wizard.Core/Versions/package-versions.json summary.md`.
 3. If it conflicts with or depends on another plugin, add a rule to
    `src/Wizard.Core/Registry/InteractionRules.Data.cs`.
 4. Add it to a tech in `src/Wizard.Core/Registry/TechSuggestions.cs`, or to `Techs.NotSuggestedByTech`.
 5. Run the tests, accept the new snapshots, then run the generated solution tests to prove its sample
    builds with every test framework.


## Workflows

 * `deploy.yml` runs the tests and deploys the site on every push to main.
 * `integration.yml` builds and tests a generated solution per plugin and per test framework, nightly
   and on pull requests that change the generators.
 * `refresh-versions.yml` moves every package to its newest stable version each Monday, and opens a pull
   request saying whether the generated solutions build with them.
 * `content-drift.yml` compares the docs and samples copied from Verify with Verify's main branch each
   Monday.
