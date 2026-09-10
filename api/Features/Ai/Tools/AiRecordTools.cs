using Ganss.Xss;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Contracts.RecordLinks;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// Tools that reach into a single record's substance — its correspondence and its attachments — and
/// the one tool that writes back into a form the user has open.
///
/// <para>The attachment pair is deliberately a negotiation rather than a dump. Listing is cheap and
/// returns names only; the model decides what it actually needs and asks for that. Pushing every
/// email body and every attachment into the prompt would cost a fortune and bury the answer.</para>
/// </summary>
internal static partial class AiRecordTools
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });


    public static IReadOnlyList<AiTool> Build() =>
        CorrespondenceTools()
            .Concat(ContextTools())
            .Concat(DirectoryTools())
            .Concat(ComplianceTools())
            .Concat(XeroContactPushTools())
            .ToList();

    /// <summary>The model's (or the route's) name for a record type onto the enum the record-link
    /// layer keys on. Tolerant of spacing and underscores; strict about meaning — an unknown name
    /// fails rather than guessing. Internal so every tool that names a record type speaks the same
    /// vocabulary — one mapping, not two that drift.</summary>
    internal static bool TryMapRecordType(string value, out RecordType recordType)
    {
        var normalised = value.Trim().ToLowerInvariant().Replace('-', ' ').Replace('_', ' ');
        RecordType? mapped = normalised switch
        {
            "request" or "rfi" or "rfa" or "rfc" or "rfq" or "rfp" or "nod" or "eot" => RecordType.Request,
            "bid package" or "bid package invite" or "bidpackage" or "bpi" => RecordType.BidPackageInvite,
            "variation" or "variation order" or "vo" => RecordType.Variation,
            "variation quote" or "voq" => RecordType.VariationQuote,
            "work order" or "purchase order" or "po" => RecordType.WorkOrder,
            "defect" => RecordType.Defect,
            "todo" or "to do" => RecordType.Todo,
            "calendar event" or "calendar" or "event" => RecordType.CalendarEvent,
            "lad" or "liquidated damages" => RecordType.Lad,
            "cost centre" or "cost center" => RecordType.CostCentre,
            "scheduling" or "programme" => RecordType.Scheduling,
            "subcontractor comms" => RecordType.SubcontractorComms,
            "supplier comms" => RecordType.SupplierComms,
            "valuation snapshot" or "valuation report snapshot" => RecordType.ValuationReportSnapshot,
            "valuation claim" or "valuation period" => RecordType.ValuationClaim,
            _ => null
        };
        recordType = mapped ?? default;
        return mapped is not null;
    }
}
