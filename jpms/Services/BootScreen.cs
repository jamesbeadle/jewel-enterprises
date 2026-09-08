using Microsoft.JSInterop;

namespace Jewel.JPMS.Services;

/// <summary>
/// The app's side of the boot overlay in wwwroot/index.html (js/boot-screen.js).
/// </summary>
public static class BootScreen
{
    /// <summary>
    /// Takes the boot overlay down. Never throws: the .NET assemblies and index.html are fetched
    /// separately, so a client can be running today's code against yesterday's shell — one that
    /// predates <c>jpmsBoot</c>, or a future one that renames it. A missing dismiss must not take the
    /// page render down with it (JPMS-4BF13E, 2026-09-08): the old shell kept its boot mark inside
    /// #app, which Blazor has already wiped, and the new shell's failsafe removes the overlay anyway.
    /// </summary>
    public static async Task DismissAsync(IJSRuntime js)
    {
        try
        {
            await js.InvokeVoidAsync("jpmsBoot.dismiss");
        }
        catch (JSException)
        {
        }
    }
}
