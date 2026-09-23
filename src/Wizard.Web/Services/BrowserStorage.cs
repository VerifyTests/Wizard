/// <summary>
/// The answers the wizard remembers between visits (plan 8.2), kept in localStorage through the
/// <c>verifyWizard.storage*</c> helpers in <c>wwwroot/js/interop.js</c>. What to keep, and when a kept
/// value applies, is decided by <see cref="BrowserMemory"/>; this only reads and writes.
/// </summary>
public sealed class BrowserStorage(IJSRuntime js)
{
    public async Task<Remembered> ReadAsync() =>
        new(
            await GetAsync(BrowserMemory.TechKey),
            await GetAsync(BrowserMemory.ExistingKey),
            await GetAsync(BrowserMemory.SponsorKey));

    /// <summary>A null value is left alone; an empty one is forgotten.</summary>
    public async Task WriteAsync(Remembered remembered)
    {
        await SetAsync(BrowserMemory.TechKey, remembered.Tech);
        await SetAsync(BrowserMemory.ExistingKey, remembered.Existing);
        await SetAsync(BrowserMemory.SponsorKey, remembered.Sponsor);
    }

    public async Task ForgetAsync()
    {
        foreach (var key in BrowserMemory.Keys)
        {
            await js.InvokeVoidAsync("verifyWizard.storageRemove", key);
        }
    }

    ValueTask<string?> GetAsync(string key) =>
        js.InvokeAsync<string?>("verifyWizard.storageGet", key);

    async Task SetAsync(string key, string? value)
    {
        if (value == null)
        {
            return;
        }

        if (value.Length == 0)
        {
            await js.InvokeVoidAsync("verifyWizard.storageRemove", key);
            return;
        }

        await js.InvokeVoidAsync("verifyWizard.storageSet", key, value);
    }
}
