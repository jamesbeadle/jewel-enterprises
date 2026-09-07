using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.RecordLinks;

namespace Jewel.JPMS.Api.Features.Todos;

/// <summary>
/// The "about this record" side of a to-do (TodoItemEntity.AboutRecordType/AboutRecordId): the
/// record-type-agnostic checks and reads, done through the linkable-record providers so any type
/// the link layer knows (a defect today, a work order or RFI tomorrow) works without a to-do
/// change. Scoped — carries the registry and the per-request context.
/// </summary>
public sealed class TodoAboutRecords
{
    private readonly RecordProviderRegistry providers;

    public TodoAboutRecords(RecordProviderRegistry providers) { this.providers = providers; }

    public static string Key(int? recordType, string recordId) => $"{recordType}:{recordId}";

    /// <summary>The record a write names, verified: it exists, and it belongs to the item's
    /// project — a to-do is never about a record on another project. Throws with the fix in the
    /// message; null when the command names no record.</summary>
    public async Task<LinkableRecord?> VerifyAsync(
        RecordType? type, string? recordId, string projectId, CancellationToken cancellationToken)
    {
        if (type is null && string.IsNullOrWhiteSpace(recordId)) return null;
        if (type is null || string.IsNullOrWhiteSpace(recordId))
            throw new InvalidOperationException("A to-do about a record needs both the record type and the record id.");
        if (!providers.TryGet(type.Value, out var provider))
            throw new InvalidOperationException($"To-dos can't be about a {type} record.");
        var record = await provider.FindAsync(recordId, cancellationToken)
            ?? throw new InvalidOperationException($"That {type} record wasn't found — it may have been deleted.");
        if (!string.Equals(record.ProjectId, projectId, StringComparison.Ordinal))
            throw new InvalidOperationException($"{record.Reference} is on a different project — a to-do is raised on the record's own project.");
        return record;
    }

    /// <summary>The human references ("DEF-0012") of every record the given items are about, keyed
    /// by <see cref="Key"/>. One provider read per distinct record; a record that no longer
    /// resolves is simply absent (the item then shows no reference).</summary>
    public async Task<IReadOnlyDictionary<string, string>> ReferencesForAsync(
        IEnumerable<TodoItemEntity> entities, CancellationToken cancellationToken)
    {
        var references = new Dictionary<string, string>(StringComparer.Ordinal);
        var wanted = entities
            .Where(e => e.AboutRecordType is not null && !string.IsNullOrWhiteSpace(e.AboutRecordId))
            .Select(e => (Type: e.AboutRecordType!.Value, Id: e.AboutRecordId!))
            .Distinct()
            .ToList();
        foreach (var (type, id) in wanted)
        {
            if (!providers.TryGet((RecordType)type, out var provider)) continue;
            var record = await provider.FindAsync(id, cancellationToken);
            if (record is not null) references[Key(type, id)] = record.Reference;
        }
        return references;
    }
}
