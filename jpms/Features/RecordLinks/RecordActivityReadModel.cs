
namespace Jewel.JPMS.Features.RecordLinks;

// A project's per-record activity summaries, keyed for the badge lookups the register rows and
// record tab dots make at render time. Follows the fetch-at-most-once-per-key convention
// (see HttpRequestRegister / CLAUDE.md data-loading): For(...) may start the first load but never
// re-triggers on re-render; pages call Refresh(projectId) once from OnInitializedAsync so
// navigating between tabs revalidates in the background (stale-while-revalidate).
public sealed class RecordActivityReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, Dictionary<(RecordType Type, string RecordId), RecordActivitySummary>> byProject = new();

    // Projects whose activity has had a load started — prevents an empty result from re-triggering
    // a fetch on every re-render.
    private readonly HashSet<string> requested = new();

    public RecordActivityReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public bool LoadedFor(string projectId) => byProject.ContainsKey(projectId);

    /// <summary>
    /// This record's recent triage activity, or null when there is none — or when the project's
    /// activity has not loaded yet. The two nulls deliberately render the same way (no badge):
    /// per the loading conventions a single inline mark is never gated, and unlike a figure, an
    /// absent badge cannot mislead — absence during the load and absence after it both mean
    /// "nothing to show". The api coalesces VariationQuote rows onto RecordType.Variation, so
    /// variation lookups always use Variation + VariationOrderId.
    /// </summary>
    public RecordActivitySummary? For(string projectId, RecordType type, string recordId)
    {
        if (requested.Add(projectId)) _ = LoadAsync(projectId);
        return byProject.TryGetValue(projectId, out var summaries)
            && summaries.TryGetValue((type, recordId), out var summary)
            ? summary
            : null;
    }

    // Forces a background reload even when the project is already cached. Pages call this once
    // on entry (never from render) so tab navigation picks up links made elsewhere — a triager,
    // another user, an agent.
    public void Refresh(string projectId)
    {
        requested.Add(projectId);
        _ = LoadAsync(projectId);
    }

    private async Task LoadAsync(string projectId)
    {
        try
        {
            var summaries = await queries.AskAsync(new ListRecordActivity(projectId), CancellationToken.None);
            byProject[projectId] = summaries.ToDictionary(s => (s.Type, s.RecordId));
            OnChanged?.Invoke();
        }
        catch
        {
            // Absence is the quiet answer (ListRecordActivity is best-effort, so nothing was
            // reported either). The project deliberately STAYS in `requested`: dropping it would
            // let the next render — any click on the page — fire the same failing request again,
            // and a page re-renders far more often than a host recycles. The next page entry's
            // Refresh(projectId) is the retry, which is the same cadence a success revalidates on.
        }
    }
}
