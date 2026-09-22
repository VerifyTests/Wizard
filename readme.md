# Verify Wizard

A getting-started wizard for [Verify](https://github.com/VerifyTests/Verify), hosted at
https://verifytests.github.io/Wizard/.

It replaces the static markdown pages in `Verify/docs/wiz`. For a new project it asks for the operating
system, IDE, test framework, build server and optionally the tech stack. It then produces:

 * a guide rendered in the page,
 * a downloadable zipped solution that builds and runs,
 * markdown instructions for an AI coding assistant.

A second entry point adds Verify extensions to an existing project, and explains how they interact with
the extensions already in use.

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
