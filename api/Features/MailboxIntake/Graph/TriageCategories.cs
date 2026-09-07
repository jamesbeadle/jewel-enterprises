
namespace Jewel.JPMS.Api.Features.MailboxIntake.Graph;

/// <summary>The categories the triage system stamps on a mailbox message. Triage never moves an
/// email — it tags it — so the Inbox stays whole and each view is a category filter.
///
/// Lives in its own file (rather than MailboxGraphClient.cs) because the worker compiles it too,
/// via a linked include in Jewel.JPMS.Worker.csproj — the outbound draft's categories must be
/// stamped with exactly the same marker and tag stems the API's triage views filter on.</summary>
public static class TriageCategories
{
    /// <summary>The marker present on any email that carries a JPMS workflow tag. The triage queue is
    /// Inbox WITHOUT this; the Tagged view is Inbox WITH it. Graph only filters categories by exact
    /// match (no "starts-with"), so this single marker is how we express "has any JPMS tag".</summary>
    public const string Marker = "JPMS";

    /// <summary>Prefix shared by every workflow tag (e.g. "JPMS/Discarded", "JPMS/RFI-001"). The bare
    /// <see cref="Marker"/> has no trailing slash, so it never matches this — that's how RemoveTag
    /// decides whether any workflow tags remain.</summary>
    public const string WorkflowPrefix = "JPMS/";

    /// <summary>Present on a discarded ("not a request") email.</summary>
    public const string Discarded = "JPMS/Discarded";

    /// <summary>Present on a thread dealt with by REPLYING from the portal (triage compose) without
    /// filing it to a record — answering an email is as real a triage decision as linking it. An
    /// ordinary workflow tag (not a bucket): it carries the marker, removing it returns the thread
    /// to the queue, and later replies surface it as a "Thread:" hint like any record tag.</summary>
    public const string Replied = "JPMS/Replied";

    /// <summary>Present on an email an ADMINISTRATOR has dealt with outside the record model — today
    /// the Control Centre's "Mark as KPI" (2026-09-03). The queue is Inbox-without-a-JPMS-tag, so the
    /// KPI mark (which lives in the administrators-only register, never in the mailbox) needs SOME
    /// workflow tag to take the email out of the queue; this deliberately neutral one says only
    /// "admin handled it" and nothing about why. An ordinary workflow tag like Replied: carries the
    /// marker, removing it returns the email to the queue.</summary>
    public const string Admin = "JPMS/Admin";

    // --- Communication pathways (buckets) ---
    // Every triaged thread is filed under exactly one pathway: who the correspondence is with.
    // The pathway is a category tag stamped thread-wide alongside the record tag, so each pathway
    // view is one cheap exact-match Graph filter (same trick as the marker). A thread CAN be filed
    // under more than one pathway — any dual filing (the former hard "client wall" included,
    // removed 2026-08-21) is a soft check: refused once, allowed with an explicit confirm
    // (AllowCrossPathway).

    /// <summary>Pathway tag: correspondence with the client side (client, architect).</summary>
    public const string Client = "JPMS/Client";

    /// <summary>Pathway tag: correspondence with subcontractors.</summary>
    public const string Subcontractor = "JPMS/Subcontractor";

    /// <summary>Pathway tag: correspondence with materials/goods suppliers, as distinct from
    /// subcontractors (the Control Centre's pathway restructure, 2026-08-27).</summary>
    public const string Supplier = "JPMS/Supplier";

    /// <summary>Pathway tag: internal Jewel correspondence (to-dos, company admin).</summary>
    public const string Internal = "JPMS/Internal";

    /// <summary>The four pathway tags. Order matters only for display.</summary>
    public static readonly IReadOnlyList<string> AllBuckets = new[] { Client, Subcontractor, Supplier, Internal };

    /// <summary>True if a category is one of the four pathway (bucket) tags. Bucket tags share the
    /// JPMS/ prefix but are NOT workflow tags for queue-membership purposes: an email carrying only a
    /// bucket has no triage decision, so every "does it have a decision" test must exclude them.</summary>
    public static bool IsBucketTag(string category) =>
        category.Equals(Client, StringComparison.OrdinalIgnoreCase)
        || category.Equals(Subcontractor, StringComparison.OrdinalIgnoreCase)
        || category.Equals(Supplier, StringComparison.OrdinalIgnoreCase)
        || category.Equals(Internal, StringComparison.OrdinalIgnoreCase);

    /// <summary>The pathway a record type files its thread under, or null when the type is
    /// pathway-neutral: a Todo link never sets or changes a pathway, and CostCentre mail can be
    /// valuation-side (Client) or subcontract-side (Subcontractor) — the triager's explicit pathway
    /// choice decides, per email.</summary>
    public static string? BucketFor(RecordType type) => type switch
    {
        RecordType.Request          => Client,
        RecordType.Variation        => Client,
        RecordType.VariationQuote   => Client,
        RecordType.Scheduling       => Client,   // programme correspondence is client/architect-facing
        RecordType.Lad              => Client,   // LAD claims sit between Jewel and the client
        RecordType.ValuationReportSnapshot => Client, // the snapshot is the only client-facing form of the valuation report
        RecordType.ValuationClaim   => Client,   // the period the snapshot is frozen from — the valuation is what Jewel puts to the client
        RecordType.TenderEnquiry    => Client,   // the architect's invitation to tender — client-side from the first email
        RecordType.BuildingControlCase => Client, // statutory/consultant correspondence travels the client-side pathway
        RecordType.BuildingControlInspection => Client, // the inspector's booking/report thread — same side as the case
        RecordType.BidPackageInvite => Subcontractor,
        RecordType.WorkOrder        => Subcontractor, // the order Jewel places with the subcontractor
        // The remediation is chased with the company that caused it. Raisable from the Supplier
        // pane too since 2026-09-07 (faulty goods are the merchant's to put right); the thread
        // still files under Subcontractor, because the pathway is keyed on record type — see
        // DefectLinkProvider for why that is left alone.
        RecordType.Defect           => Subcontractor,
        RecordType.SubcontractorComms => Subcontractor, // general subcontractor correspondence — the tag IS the filing
        RecordType.SupplierComms    => Supplier,     // general supplier correspondence — the tag IS the filing
        RecordType.Inventory        => Supplier,     // the goods come from a materials/goods supplier
        RecordType.InternalComms    => Internal,     // general staff-to-staff correspondence — the tag IS the filing
        RecordType.SiteInstruction  => Internal,     // Jewel instructing its own site — staff-side by nature (2026-09-03)
        RecordType.CostCentre       => null,     // triager picks the side, per email
        RecordType.CalendarEvent    => null,     // neutral: a site visit, a delivery or a meeting belongs to whichever side arranged it
        RecordType.Todo             => null,     // neutral: never sets or changes a pathway
        _ => null
    };

    /// <summary>True when the two categories are pathway tags on opposite sides of the former client
    /// wall — one is <see cref="Client"/> and the other is a non-client pathway. Since 2026-08-21 this
    /// combination is no longer specially blocked: like any dual filing, it goes through the standard
    /// cross-filing confirm. Kept for reporting/diagnostics.</summary>
    public static bool CrossesClientWall(string bucketA, string bucketB) =>
        !bucketA.Equals(bucketB, StringComparison.OrdinalIgnoreCase)
        && (bucketA.Equals(Client, StringComparison.OrdinalIgnoreCase)
            || bucketB.Equals(Client, StringComparison.OrdinalIgnoreCase));

    /// <summary>The workflow tag for an email linked to a record, from its reference
    /// (e.g. "RFI-001" -> "JPMS/RFI-001", "BPI-0001" -> "JPMS/BPI-0001"). The record reads its emails
    /// back by this exact tag. Record-type-agnostic: the tag is just the reference stem.</summary>
    public static string ForRecord(string reference) => $"JPMS/{reference.Trim()}";

    /// <summary>Back-compat alias for <see cref="ForRecord"/>, kept while the Request path migrates to
    /// the record-agnostic link layer. Prefer <see cref="ForRecord"/> in new code.</summary>
    public static string ForRequest(string reference) => ForRecord(reference);

    /// <summary>True if a category is a JPMS workflow tag (not the bare marker, not a user category).
    /// Note this includes bucket tags — callers deciding queue membership or "is this thread triaged"
    /// must additionally exclude <see cref="IsBucketTag"/> matches.</summary>
    public static bool IsWorkflowTag(string category) =>
        category.StartsWith(WorkflowPrefix, StringComparison.OrdinalIgnoreCase);
}
