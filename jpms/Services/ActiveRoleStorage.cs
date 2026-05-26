using Jewel.JPMS.Models;
using Microsoft.JSInterop;

namespace Jewel.JPMS.Services;

public sealed class ActiveRoleStorage
{
    private const string StorageKeyPrefix = "jpms.activeRole";
    private const string GetItem = "localStorage.getItem";
    private const string SetItem = "localStorage.setItem";

    private readonly IJSRuntime js;

    public ActiveRoleStorage(IJSRuntime js)
    {
        this.js = js;
    }

    public async Task<Role?> ReadAsync(string email)
    {
        var stored = await TryGetItem(StorageKeyFor(email));
        if (string.IsNullOrWhiteSpace(stored)) return null;
        return Enum.TryParse<Role>(stored, out var role) ? role : null;
    }

    public async Task WriteAsync(string email, Role role)
    {
        await TrySetItem(StorageKeyFor(email), role.ToString());
    }

    private async Task<string?> TryGetItem(string key)
    {
        try { return await js.InvokeAsync<string?>(GetItem, key); }
        catch { return null; }
    }

    private async Task TrySetItem(string key, string value)
    {
        try { await js.InvokeVoidAsync(SetItem, key, value); }
        catch { }
    }

    private static string StorageKeyFor(string email) =>
        $"{StorageKeyPrefix}.{email.Trim().ToLowerInvariant()}";
}
