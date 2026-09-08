using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

public sealed partial class WorkOrderBillRecognition
{
    /// <summary>What recognition says about one line: its bill's match, shaped for this line
    /// (the proposed shares are per line), or the reason the bill stayed in the queue.</summary>
    public readonly record struct LineVerdict(WorkOrderBillMatch? Match, string? ExceptionReason);

    /// <summary>The bill-level decision the line verdicts are cut from.</summary>
    private sealed record BillVerdict(OpenOrder? Order, WorkOrderMatchRule Rule, string? Detail, string? ExceptionReason);

    /// <summary>An order a bill could pay, with the figures the rules and the card need.</summary>
    private sealed record OpenOrder(
        string WorkOrderId,
        string Reference,
        int Number,
        string Title,
        string ProjectId,
        string ProjectName,
        string SubcontractorId,
        decimal Value,
        decimal InvoicedToDate,
        IReadOnlyList<KeyValuePair<string, decimal>> CodeWeights)
    {
        public decimal Remaining => Value - InvoicedToDate;
    }

    /// <summary>A name a supplier's bills may arrive under and the directory record it means.</summary>
    private readonly record struct SupplierName(string Name, string SubcontractorId);
}
