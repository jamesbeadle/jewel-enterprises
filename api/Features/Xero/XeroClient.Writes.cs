using System.Text.Json.Nodes;

namespace Jewel.JPMS.Api.Features.Xero;

/// <summary>
/// What every write shares: the not-connected refusal, the fresh read a write is always decided
/// on, the site-option check, and forgetting the snapshot a write has just made stale. The writes
/// themselves are one partial each — Approve, SiteTracking, Recode, DraftBill — with the
/// schedule-line building they share in ScheduleLines and the bill read in Bills.
/// </summary>
public sealed partial class XeroClient
{
    private const string NotConnected = "Xero isn't connected — add the Xero__ClientId / Xero__ClientSecret app settings.";

    /// <summary>
    /// Always work from a fresh read — the stored ledger line may be minutes or days old, and
    /// every write below replaces the invoice's entire line list. The caller owns the document;
    /// Invoice is null when Xero returned nothing for the id.
    /// </summary>
    private async Task<(JsonDocument Document, JsonElement? Invoice)> ReadFreshAsync(
        string token, string baseUrl, string collection, string invoiceId, CancellationToken ct)
    {
        var doc = await GetJsonAsync(token, $"{baseUrl}/{invoiceId}", collection.ToLowerInvariant(), ct);
        var found = doc.RootElement.TryGetProperty(collection, out var items)
            && items.ValueKind == JsonValueKind.Array && items.GetArrayLength() > 0;
        return (doc, found ? items[0] : null);
    }

    /// <summary>Bill or credit note: the URL, the collection Xero answers with, and the id property the update names.</summary>
    private static (string BaseUrl, string Collection, string IdProperty) InvoiceOrCreditNote(bool isCreditNote) =>
        isCreditNote ? (CreditNotesUrl, "CreditNotes", "CreditNoteID") : (InvoicesUrl, "Invoices", "InvoiceID");

    /// <summary>
    /// Sites are an explicit per-project mapping — a missing option means the mapping is wrong
    /// (or the option was renamed in Xero), so fail loudly. Null when every option exists.
    /// </summary>
    private string? MissingSitesError(IEnumerable<string> siteOptions, TrackingCategoryLookup categories, string whoseMapping)
    {
        var missing = siteOptions
            .Where(site => !categories.SiteOptions.Contains(site))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (missing.Count == 0) return null;
        return $"Xero's \"{_options.SiteTrackingCategory}\" tracking category has no option named "
            + string.Join(", ", missing.Select(site => $"\"{site}\""))
            + $" — check the {whoseMapping} against Xero's tracking options.";
    }

    /// <summary>The cached snapshot now lies about this invoice; drop it so the next sync
    /// re-reads rather than resurrecting a stale status or tracking for up to CacheMinutes.</summary>
    private void ForgetSnapshot()
    {
        _cachedSnapshot = null;
        _cachedSnapshotAt = DateTimeOffset.MinValue;
    }

    private static JsonElement? FirstOf(JsonDocument response, string collection) =>
        response.RootElement.TryGetProperty(collection, out var items)
        && items.ValueKind == JsonValueKind.Array && items.GetArrayLength() > 0
            ? items[0]
            : null;
}
