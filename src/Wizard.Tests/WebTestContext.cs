using Wizard.Tests.Generation;

namespace Wizard.Tests;

/// <summary>bunit base context for component tests: loose JS interop (clipboard and downloads are
/// no-ops), the app's DI services, and a clock frozen at <see cref="GeneratorTests.Today"/>.</summary>
public abstract class WebTestContext : BunitContext
{
    protected WebTestContext()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<ClipboardService>();
        Services.AddScoped<DownloadService>();
        Services.AddScoped<BrowserStorage>();
        Services.AddSingleton<TimeProvider>(new FixedTimeProvider());
    }

    sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() =>
            new(GeneratorTests.Today.ToDateTime(new(10, 0)), TimeSpan.Zero);
    }
}
