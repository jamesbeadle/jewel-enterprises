
namespace Jewel.JPMS.Services;

/// <summary>
/// Remembers how a user last viewed their to-dos (per browser, per user) — two preferences, both
/// shared by every to-do surface (the browser page, the project tab and the dashboard panel) so
/// the app doesn't flip idiom from one screen to the next:
///  - the status BOARD (the default — Open and Done columns, cards dragged between them) or the
///    flat LIST. Stored value is "board" or "list".
///  - the SORT direction of the open items: oldest raised first (the default — the server's
///    TODO-#### number order) or newest raised first. Stored value is "oldest" or "newest".
///    See Features.Todos.TodoSortOrder for what the direction does and doesn't touch.
/// </summary>
public sealed class TodoViewStorage
{
    private const string BoardValue = "board";
    private const string ListValue = "list";
    private const string OldestValue = "oldest";
    private const string NewestValue = "newest";

    private const string StorageKeyPrefix = "jpms.todoView";
    private const string SortKeyPrefix = "jpms.todoSort";
    private const string GetItem = "localStorage.getItem";
    private const string SetItem = "localStorage.setItem";

    private readonly IJSRuntime js;

    public TodoViewStorage(IJSRuntime js)
    {
        this.js = js;
    }

    public async Task<bool> ReadBoardAsync(string email)
    {
        try { return await js.InvokeAsync<string?>(GetItem, StorageKeyFor(StorageKeyPrefix, email)) != ListValue; }
        catch { return true; }
    }

    public async Task WriteAsync(string email, bool board)
    {
        try { await js.InvokeVoidAsync(SetItem, StorageKeyFor(StorageKeyPrefix, email), board ? BoardValue : ListValue); }
        catch { }
    }

    /// <summary>True = newest raised first; false (the default, and on any failure) = oldest first.</summary>
    public async Task<bool> ReadNewestFirstAsync(string email)
    {
        try { return await js.InvokeAsync<string?>(GetItem, StorageKeyFor(SortKeyPrefix, email)) == NewestValue; }
        catch { return false; }
    }

    public async Task WriteNewestFirstAsync(string email, bool newestFirst)
    {
        try { await js.InvokeVoidAsync(SetItem, StorageKeyFor(SortKeyPrefix, email), newestFirst ? NewestValue : OldestValue); }
        catch { }
    }

    private static string StorageKeyFor(string prefix, string email) =>
        $"{prefix}.{email.Trim().ToLowerInvariant()}";
}
