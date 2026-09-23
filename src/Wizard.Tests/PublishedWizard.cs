namespace Wizard.Tests;

/// <summary>
/// Shared plumbing for browser-based tests: serves the published Blazor output
/// (bin/&lt;Configuration&gt;/blazor-publish) from an in-memory Kestrel host and provides a Chromium
/// instance, exercising the actual WASM runtime (which the bunit tests do not).
/// </summary>
public sealed class PublishedWizard : IAsyncDisposable
{
    readonly WebApplication app;
    readonly IPlaywright playwright;

    public IBrowser Browser { get; }
    public int Port { get; }

    static PublishedWizard? shared;

    /// <summary>
    /// One Kestrel host and one Chromium for the whole assembly. A per-class instance meant several cold
    /// WASM boots racing each other on a two-core CI agent, and the losers blew Playwright's 30s default
    /// before the runtime finished starting.
    /// </summary>
    public static PublishedWizard Shared =>
        shared ?? throw new("PublishedWizard has not been started.");

    [Before(HookType.Assembly)]
    public static async Task StartShared() => shared = await Start();

    [After(HookType.Assembly)]
    public static async Task StopShared()
    {
        if (shared != null)
        {
            await shared.DisposeAsync();
            shared = null;
        }
    }

    /// <summary>
    /// The one context every page opens in. <see cref="IBrowser.NewPageAsync"/> creates a fresh context
    /// per page, and a context is an isolated profile with its own HTTP cache, so each test would
    /// re-fetch and re-compile the WASM runtime from cold. The wizard keeps no browser storage yet, so
    /// sharing the context leaks no state between tests. Once localStorage is used (plan 8.2), tests that
    /// depend on it clear it first.
    /// </summary>
    readonly IBrowserContext context;

    PublishedWizard(WebApplication app, IPlaywright playwright, IBrowser browser, IBrowserContext context, int port)
    {
        this.app = app;
        this.playwright = playwright;
        this.context = context;
        Browser = browser;
        Port = port;
    }

    public string Url(string path = "/") => $"http://localhost:{Port}{path}";

    public Task<IPage> NewPage() =>
        context.NewPageAsync();

    /// <summary>What every page believes "now" is, so dates the wizard derives are the same every run.</summary>
    public static readonly DateTime FixedTime = new(2026, 9, 22, 10, 0, 0, DateTimeKind.Utc);

    public static async Task<PublishedWizard> Start()
    {
        var installExitCode = Program.Main(["install", "chromium"]);
        if (installExitCode != 0)
        {
            throw new($"Playwright Chromium install failed with exit code {installExitCode}.");
        }

        var port = GetAvailablePort();

        var testAssemblyDirectory = Path.GetDirectoryName(typeof(PublishedWizard).Assembly.Location)!;
        var wwwroot = Path.Combine(testAssemblyDirectory, "..", "blazor-publish", "wwwroot");

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://localhost:{port}");
        builder.Logging.ClearProviders();
        var app = builder.Build();

        var contentTypes = new FileExtensionContentTypeProvider
        {
            Mappings =
            {
                [".wasm"] = "application/wasm"
            }
        };
        var files = new PhysicalFileProvider(wwwroot);

        app.UseDefaultFiles(new DefaultFilesOptions {FileProvider = files});
        app.UseStaticFiles(
            new StaticFileOptions
            {
                FileProvider = files,
                ContentTypeProvider = contentTypes,
                ServeUnknownFileTypes = true
            });
        // The pattern-less MapFallbackToFile registers {*path:nonfile}, which treats a dotted last
        // segment (a deep link such as /add/Verify.X) as a file request and 404s it. GitHub Pages serves
        // 404.html (a copy of index.html) for any missing path, so the host matches that with an
        // unconstrained pattern. That pattern also matches every asset url, and the static file
        // middleware steps aside once routing has picked an endpoint, so routing has to run after the
        // static files rather than at the start of the pipeline.
        app.UseRouting();
        app.MapFallbackToFile(
            "{*path}",
            "index.html",
            new()
            {
                FileProvider = files
            });

        await app.StartAsync();

        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync();
        // Fixed viewport and locale so screenshots are deterministic across machines: date and month
        // pickers render in the browser's locale.
        var context = await browser.NewContextAsync(
            new()
            {
                Locale = "en-US",
                ViewportSize = new()
                {
                    Width = 1280,
                    Height = 900
                }
            });
        // Fixed on the context rather than per page: the wizard derives dates from today, and installing
        // the clock on a page that has not navigated yet fails, because the script it calls into is
        // injected on navigation.
        await context.Clock.SetFixedTimeAsync(FixedTime);

        var wizard = new PublishedWizard(app, playwright, browser, context, port);
        await wizard.WarmUp();
        return wizard;
    }

    /// <summary>
    /// Boot the app once, serially, before any test runs. The first load downloads and initializes the
    /// WASM runtime; on a cold CI agent that alone can outlast Playwright's 30s default. After this the
    /// shared context's HTTP cache is warm.
    /// </summary>
    async Task WarmUp()
    {
        var page = await NewPage();
        await page.GotoAsync(Url());
        await page.WaitForSelectorAsync(
            ".entry-cards",
            new()
            {
                Timeout = 120_000
            });
        await page.CloseAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await context.CloseAsync();
        await Browser.CloseAsync();
        playwright.Dispose();
        await app.StopAsync();
        await app.DisposeAsync();
    }

    static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint) listener.LocalEndpoint).Port;
    }
}
