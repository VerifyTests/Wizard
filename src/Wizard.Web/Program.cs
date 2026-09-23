var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<ClipboardService>();
builder.Services.AddScoped<DownloadService>();
builder.Services.AddScoped<BrowserStorage>();
builder.Services.AddScoped<PackageVersionLookup>();
builder.Services.AddScoped(_ => new HttpClient {Timeout = TimeSpan.FromSeconds(60)});
builder.Services.AddSingleton(TimeProvider.System);

await builder.Build().RunAsync();
