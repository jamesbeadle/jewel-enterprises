using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Components;

/// <summary>
/// THE status vocabulary: every domain status maps to one <see cref="Tone"/> here and nowhere
/// else. A view renders a status as <c>&lt;Pill Tone="@status.ToTone()"&gt;</c> and never picks a
/// colour itself. The rule of thumb applied throughout: Positive = done/approved/healthy,
/// Negative = failed/rejected/overdue, Warning = someone must act, Info = in flight / neutral,
/// Muted = a plain fact (draft, cancelled, closed, withdrawn).
/// </summary>
public static class StatusTones
{
    public static Tone ToTone(this RequestStatus status) => status switch
    {
        RequestStatus.NeedsAction => Tone.Warning,
        RequestStatus.NeedsVariation => Tone.Warning,
        RequestStatus.Open => Tone.Info,
        _ => Tone.Muted
    };

    public static Tone ToTone(this WorkOrderStatus status) => status switch
    {
        WorkOrderStatus.Released => Tone.Positive,
        WorkOrderStatus.Rejected => Tone.Negative,
        WorkOrderStatus.Cancelled => Tone.Negative,
        WorkOrderStatus.Complete => Tone.Muted,
        _ => Tone.Muted
    };

    public static Tone ToTone(this VariationRequestStatus status) => status switch
    {
        VariationRequestStatus.Submitted => Tone.Warning,
        VariationRequestStatus.UnderReview => Tone.Warning,
        VariationRequestStatus.Accepted => Tone.Positive,
        VariationRequestStatus.Rejected => Tone.Negative,
        _ => Tone.Muted
    };

    public static Tone ToTone(this VariationOrderStatus status) => status switch
    {
        VariationOrderStatus.Approved => Tone.Positive,
        VariationOrderStatus.Rejected => Tone.Negative,
        VariationOrderStatus.AwaitingArchitectInstruction => Tone.Warning,
        VariationOrderStatus.Issued => Tone.Info,
        _ => Tone.Muted
    };

    public static Tone ToTone(this ValuationInvoiceStatus status) => status switch
    {
        ValuationInvoiceStatus.Paid => Tone.Positive,
        ValuationInvoiceStatus.Approved => Tone.Positive,
        ValuationInvoiceStatus.Issued => Tone.Info,
        ValuationInvoiceStatus.Submitted => Tone.Warning,
        ValuationInvoiceStatus.Rejected => Tone.Negative,
        _ => Tone.Muted
    };

    public static Tone ToTone(this BuildingControlInspectionStatus status) => status switch
    {
        BuildingControlInspectionStatus.Passed => Tone.Positive,
        BuildingControlInspectionStatus.ActionsRequired => Tone.Negative,
        BuildingControlInspectionStatus.Booked => Tone.Info,
        BuildingControlInspectionStatus.Inspected => Tone.Info,
        _ => Tone.Muted
    };

    public static Tone ToTone(this TimesheetStatus status) => status switch
    {
        TimesheetStatus.Approved => Tone.Positive,
        TimesheetStatus.Rejected => Tone.Negative,
        _ => Tone.Muted
    };

    public static Tone ToTone(this ComplianceStatus status) => status switch
    {
        ComplianceStatus.Current => Tone.Positive,
        ComplianceStatus.ExpiringSoon => Tone.Warning,
        ComplianceStatus.Expired => Tone.Negative,
        _ => Tone.Muted
    };

    public static Tone ToTone(this WorkOrderPaymentStatus status) => status switch
    {
        WorkOrderPaymentStatus.Paid => Tone.Positive,
        WorkOrderPaymentStatus.PartPaid => Tone.Info,
        _ => Tone.Muted
    };

    public static Tone ToTone(this WorkOrderInvoicingStatus status) => status switch
    {
        WorkOrderInvoicingStatus.FullyInvoiced => Tone.Positive,
        WorkOrderInvoicingStatus.OverInvoiced => Tone.Negative,
        WorkOrderInvoicingStatus.PartInvoiced => Tone.Info,
        _ => Tone.Muted
    };

    public static Tone ToTone(this ValuationClaimStatus status) => status switch
    {
        ValuationClaimStatus.Confirmed => Tone.Positive,
        ValuationClaimStatus.Preapproved => Tone.Warning,
        _ => Tone.Muted
    };

    /// <summary>The sales ladder: open stages read neutral → in flight, Won positive, Lost negative,
    /// Nurture and New plain facts. (The label carries the rung; the tone carries the verdict.)</summary>
    public static Tone ToTone(this LeadStage stage) => stage switch
    {
        LeadStage.Engaged => Tone.Info,
        LeadStage.SiteVisit => Tone.Info,
        LeadStage.Proposal => Tone.Info,
        LeadStage.Won => Tone.Positive,
        LeadStage.Lost => Tone.Negative,
        _ => Tone.Muted
    };

    public static Tone ToTone(this SalesStrategyStatus status) => status switch
    {
        SalesStrategyStatus.Active => Tone.Positive,
        SalesStrategyStatus.Paused => Tone.Info,
        _ => Tone.Muted
    };

    public static Tone ToTone(this HsSeverity severity) => severity switch
    {
        HsSeverity.Critical => Tone.Negative,
        HsSeverity.High => Tone.Negative,
        HsSeverity.Medium => Tone.Warning,
        _ => Tone.Muted
    };

    /// <summary>The three triage pathways as a categorical: Client positive, Subcontractor warning,
    /// Supplier info — the same reading TriagePathways gives the pathway chips.</summary>
    public static Tone PathwayTone(string? pathway) => pathway switch
    {
        "Client" => Tone.Positive,
        "Subcontractor" => Tone.Warning,
        "Supplier" => Tone.Info,
        _ => Tone.Muted
    };

    public static Tone ToTone(this DefectStatus status) => status switch
    {
        DefectStatus.Verified => Tone.Positive,
        DefectStatus.Resolved => Tone.Info,
        DefectStatus.InProgress => Tone.Warning,
        DefectStatus.Open => Tone.Negative,
        _ => Tone.Muted
    };

    public static Tone ToTone(this ProgrammeDraftStatus status) => status switch
    {
        ProgrammeDraftStatus.Open => Tone.Warning,
        ProgrammeDraftStatus.Applied => Tone.Positive,
        _ => Tone.Muted
    };

    /// <summary>How far to trust a draft line's mapping: confirmed by a person (saved earlier or
    /// set now) is positive, a rule match is neutral, Claude's suggestion is a warning to check,
    /// and nothing matched is negative — the task needs a hand before the draft can move it.</summary>
    public static Tone ToTone(this ProgrammeMappingSource source) => source switch
    {
        ProgrammeMappingSource.Saved => Tone.Positive,
        ProgrammeMappingSource.Person => Tone.Positive,
        ProgrammeMappingSource.Rule => Tone.Info,
        ProgrammeMappingSource.Claude => Tone.Warning,
        _ => Tone.Negative
    };
}
