using System.Text.Json.Nodes;

namespace Jewel.JPMS.Api.Features.Xero;

public sealed partial class XeroClient
{
    /// <summary>
    /// The full line list rebuilt: every line passes through as-is except the targets, whose
    /// Sites entry is replaced. No amount checks needed — nothing about the money changes, and
    /// no lines are split. Also how many targets the bill no longer has.
    /// </summary>
    private static (JsonArray Lines, int Unmatched) LinesWithSitesReplaced(
        JsonElement invoice, XeroSiteTrackingRequest request, TrackingCategoryLookup categories)
    {
        var sitesByLineId = request.Lines.ToDictionary(
            line => line.LineItemId, line => line.SiteOption, StringComparer.OrdinalIgnoreCase);
        var seenLineIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lineItems = new JsonArray();
        if (invoice.TryGetProperty("LineItems", out var originals) && originals.ValueKind == JsonValueKind.Array)
            foreach (var line in originals.EnumerateArray())
            {
                var lineItemId = StringOf(line, "LineItemID");
                var isTarget = lineItemId is not null && sitesByLineId.TryGetValue(lineItemId, out var siteOption);
                if (isTarget) seenLineIds.Add(lineItemId!);
                var copy = CopyLine(line, keepLineItemId: true, keepTracking: !isTarget, includeTaxAmount: true);
                if (isTarget) copy["Tracking"] = ReplaceSiteTracking(line, categories, sitesByLineId[lineItemId!]);
                lineItems.Add(copy);
            }
        return (lineItems, sitesByLineId.Keys.Count(id => !seenLineIds.Contains(id)));
    }

    /// <summary>
    /// The target line's new tracking: the Sites entry replaced with <paramref name="siteOption"/>
    /// — or removed outright when it is null (unset) — with every other category
    /// (Xero's own Cost Code, anything else) carried over untouched.
    /// </summary>
    private static JsonArray ReplaceSiteTracking(JsonElement line, TrackingCategoryLookup categories, string? siteOption)
    {
        var tracking = new JsonArray();
        if (siteOption is not null)
            tracking.Add(new JsonObject { ["TrackingCategoryID"] = categories.SiteCategoryId, ["Option"] = siteOption });
        if (line.TryGetProperty("Tracking", out var existing) && existing.ValueKind == JsonValueKind.Array)
            foreach (var entry in existing.EnumerateArray())
            {
                var categoryId = StringOf(entry, "TrackingCategoryID");
                if (categoryId is not null
                    && !categoryId.Equals(categories.SiteCategoryId, StringComparison.OrdinalIgnoreCase))
                    tracking.Add(JsonNode.Parse(entry.GetRawText()));
            }
        return tracking;
    }
}
