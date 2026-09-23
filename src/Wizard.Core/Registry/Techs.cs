/// <param name="Id">Stable, because it is a url value and is kept in browser storage.</param>
/// <param name="Recommended">Plugins pre-checked when the tech is chosen.</param>
/// <param name="Related">Plugins listed under the suggestions but left unchecked.</param>
public sealed record Tech(
    string Id,
    string DisplayName,
    string Group,
    IReadOnlyList<string> Recommended,
    IReadOnlyList<string> Related);

/// <summary>
/// The tech stack a project uses, and the plugins each one suggests (plan 10). This table is the one
/// place suggestions come from; plugins carry no tags of their own, so the mapping cannot disagree
/// with itself.
/// </summary>
public static class Techs
{
    public static IReadOnlyList<Tech> All { get; } =
    [
        new("efcore", "Entity Framework Core", "Data", ["EntityFramework", "LocalDb"], ["SqlServer", "ReadableExpressions"]),
        new("ef6", "Entity Framework 6", "Data", ["EntityFrameworkClassic"], ["LocalDb"]),
        new("sqlserver", "SQL Server", "Data", ["SqlServer"], ["LocalDb", "EntityFramework"]),
        new("cosmos", "Azure Cosmos DB", "Data", ["Cosmos"], []),
        new("ravendb", "RavenDB", "Data", ["RavenDB"], []),
        new("csv", "CSV files", "Data", ["CsvHelper"], ["Sep"]),
        new("excel", "Excel files", "Data", ["ClosedXml"], ["OpenXml", "Sylvan", "Aspose", "Syncfusion"]),

        new("word", "Word documents", "Documents", ["OpenXml"], ["Pandoc", "Aspose", "Syncfusion"]),
        new("powerpoint", "PowerPoint", "Documents", ["OpenXml"], ["Aspose", "Syncfusion"]),
        new("pdf", "PDF files", "Documents", ["PdfPig"], ["PDFium", "DocNet", "ImageMagick", "Aspose", "Syncfusion"]),
        new("questpdf", "PDF generation with QuestPDF", "Documents", ["QuestPDF"], ["PdfPig"]),

        new("aspnetcore", "ASP.NET Core", "Web", ["AspNetCore", "Http"], ["AngleSharp"]),
        new("http", "HttpClient and REST calls", "Web", ["Http"], ["Mockly", "Flurl"]),
        new("flurl", "Flurl", "Web", ["Flurl"], []),
        new("blazor", "Blazor", "Web", ["Bunit", "AngleSharp"], ["Blazor"]),
        new("html", "HTML and Razor output", "Web", ["AngleSharp"], []),
        new("browser", "Browser UI tests", "Web", ["Playwright", "AngleSharp"], ["Puppeteer", "Selenium", "ImageMagick"]),

        new("wpf", "WPF", "Desktop UI", ["Xaml", "Phash"], ["CommunityToolkitMvvm"]),
        new("winforms", "WinForms", "Desktop UI", ["WinForms"], []),
        new("avalonia", "Avalonia", "Desktop UI", ["Avalonia", "CommunityToolkitMvvm"], []),
        new("mvvmtoolkit", "CommunityToolkit.Mvvm", "Desktop UI", ["CommunityToolkitMvvm"], []),

        new("masstransit", "MassTransit", "Messaging", ["MassTransit"], []),
        new("nservicebus", "NServiceBus", "Messaging", ["NServiceBus"], ["MicrosoftLogging"]),
        new("wolverine", "Wolverine", "Messaging", ["Wolverine"], []),
        new("brighter", "Brighter", "Messaging", ["Brighter"], []),

        new("melogging", "Microsoft.Extensions.Logging", "Logging", ["MicrosoftLogging"], []),
        new("serilog", "Serilog", "Logging", ["Serilog"], []),
        new("zerolog", "ZeroLog", "Logging", ["ZeroLog"], []),

        new("otel", "OpenTelemetry and Activity", "Observability", ["OpenTelemetry"], ["Diagnostics"]),

        new("stj", "System.Text.Json", "Serialization", ["SystemJson"], ["Quibble"]),
        new("newtonsoft", "Newtonsoft.Json", "Serialization", ["NewtonsoftJson"], []),
        new("yaml", "YAML", "Serialization", ["Yaml"], []),
        new("nodatime", "NodaTime", "Serialization", ["NodaTime"], []),
        new("ulid", "ULIDs", "Serialization", ["Ulid"], ["NUlid"]),

        new("mailmessage", "System.Net.Mail", "Email", ["MailMessage"], []),
        new("sendgrid", "SendGrid", "Email", ["SendGrid"], []),
        new("htmlemail", "HTML email rendering", "Email", ["EmailPreviewServices"], ["AngleSharp"]),

        new("images", "Images and screenshots", "Images", ["ImageSharp"], ["ImageMagick", "ImageHash", "ImageSharpCompare", "Phash"]),

        new("moq", "Moq", "Mocking", ["Moq"], []),
        new("nsubstitute", "NSubstitute", "Mocking", ["NSubstitute"], []),
        new("fakeiteasy", "FakeItEasy", "Mocking", ["FakeItEasy"], []),
        new("mockly", "Mockly", "Mocking", ["Mockly"], []),

        new("sourcegen", "Source generators", "Compiler", ["SourceGenerators"], ["ICSharpCodeDecompiler"]),
        new("il", "IL and assembly comparison", "Compiler", ["ICSharpCodeDecompiler"], []),
        new("expressions", "Expression trees", "Compiler", ["ReadableExpressions"], []),

        new("assertions", "Assertion libraries inside snapshots", "Testing", ["Assertions"], []),
        new("longnames", "Long parameterised test names", "Testing", ["ParametersHashing"], [])
    ];

    public static IReadOnlyDictionary<string, Tech> ById { get; } =
        All.ToDictionary(_ => _.Id, StringComparer.Ordinal);

    public static bool Contains(string id) =>
        ById.ContainsKey(id);

    /// <summary>Groups in the order the tech step shows them.</summary>
    public static IEnumerable<IGrouping<string, Tech>> Groups =>
        All.GroupBy(_ => _.Group);

    /// <summary>
    /// Plugins no tech suggests: they are useful whatever the stack, so they are listed under
    /// "Everything else" rather than tied to one (plan 9.2).
    /// </summary>
    public static IReadOnlyList<string> NotSuggestedByTech { get; } = [Plugins.DiffPlexId, "Terminal"];
}
