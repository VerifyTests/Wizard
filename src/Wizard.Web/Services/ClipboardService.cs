/// <summary>Thin wrapper over the <c>verifyWizard.copyToClipboard</c> JS helper in <c>wwwroot/js/interop.js</c>.</summary>
public sealed class ClipboardService(IJSRuntime js)
{
    public ValueTask CopyAsync(string text) =>
        js.InvokeVoidAsync("verifyWizard.copyToClipboard", text);
}
