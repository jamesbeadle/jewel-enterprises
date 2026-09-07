using System.Text.Json.Nodes;

namespace Jewel.JPMS.Api.Features.Xero;

// -- §6a settlement-schedule coding ------------------------------------------------------
//
// 2026-09-03 (the accountant's "coding run must settle a worker who already has a bill"):
//   * RecodeBillAsync takes DRAFT, SUBMITTED and AUTHORISED bills — the cover route authorises
//     the bill before the run sees it, so authorised is the NORMAL state, not an edge case —
//     and refuses anything paid, credited, voided or deleted with the reason.
//   * A recode PRESERVES the bill: status untouched, LineAmountTypes echoed, the existing tax
//     type carried onto every new line, and the bill's own SubTotal / TotalTax / Total
//     pro-rated across the schedule's weights penny-safe (XeroSplitMaths) — the schedule
//     supplies the split, never the money. Adam's £3,200 No-VAT bill stays £3,200 No VAT.
//   * A staged draft never assumes a tax type: the contact's default purchases tax type,
//     else the tax type on the contact's most recent bill, else omitted (Xero's account
//     default, which for CIS labour 321 is INPUT2 — the fault that produced £640 of VAT on
//     a non-registered sole trader). The Note says which applied.
public sealed partial class XeroClient
{
    /// <summary>
    /// Shared validation + tracking for the §6a writes: every site option AND every cost-code
    /// option must already exist in Xero — a missing one is reported with the fix (the portal
    /// never creates tracking options, same as approval). Returns the tracking lookup so the
    /// caller can build the lines.
    /// </summary>
    private async Task<(TrackingCategoryLookup? Categories, string? Error)> PrepareScheduleTrackingAsync(
        string token, IReadOnlyList<XeroScheduleLine> lines, CancellationToken ct)
    {
        var categories = await GetTrackingCategoriesAsync(token, ct);
        if (MissingSitesError(lines.Select(line => line.SiteOption), categories, "site's Xero mapping") is { } missingSites)
            return (null, missingSites);
        if (MissingCostCodeOptionsError(lines.Select(line => line.CostCodeOption), categories) is { } missingCodes)
            return (null, missingCodes);
        return (categories, null);
    }

    /// <summary>
    /// The refusal for cost-code options Xero doesn't hold — null when every one exists. Until
    /// 2026-09-03 the portal created these on the fly; it no longer holds (by decision) the Xero
    /// settings write scope, so the fix is a person creating the option in Xero, spelt exactly.
    /// </summary>
    private string? MissingCostCodeOptionsError(IEnumerable<string> costCodeOptions, TrackingCategoryLookup categories)
    {
        var missing = costCodeOptions
            .Where(code => !categories.CostCodeOptions.Contains(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (missing.Count == 0) return null;
        return $"Xero's \"{_options.CostCodeTrackingCategory}\" tracking category has no option named "
            + string.Join(", ", missing.Select(code => $"\"{code}\""))
            + " — create it in Xero (Settings → Tracking categories), spelt exactly like that, then retry. "
            + "The portal doesn't create tracking options; Cost codes → Xero cost codes lists everything missing.";
    }

    /// <summary>One schedule line as a Xero line item: the given amounts, the tax type when known
    /// (omitted = Xero's account default), the schedule's tracking.</summary>
    private static JsonObject ScheduleLineItem(
        XeroScheduleLine line, decimal lineAmount, decimal? taxAmount, string? taxType, TrackingCategoryLookup categories)
    {
        var item = new JsonObject
        {
            ["Description"] = line.Description,
            ["Quantity"] = 1m,
            ["UnitAmount"] = lineAmount,
            ["LineAmount"] = lineAmount,
            ["AccountCode"] = line.AccountCode,
            ["Tracking"] = TrackingFor(categories, line.SiteOption, line.CostCodeOption)
        };
        if (taxAmount is not null) item["TaxAmount"] = taxAmount.Value;
        if (!string.IsNullOrWhiteSpace(taxType)) item["TaxType"] = taxType;
        return item;
    }
}
