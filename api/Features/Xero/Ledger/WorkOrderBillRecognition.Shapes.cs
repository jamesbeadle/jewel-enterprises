using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

public sealed partial class WorkOrderBillRecognition
{
    /// <summary>What recognition says about one line: its bill's match, shaped for this line
    /// (the order and the proposed shares are per line), or the reason the bill stayed in the queue.</summary>
    public readonly record struct LineVerdict(WorkOrderBillMatch? Match, string? ExceptionReason);

    /// <summary>The bill-level decision the line verdicts are cut from: which order each line
    /// pays (null when the bill stays in the queue), and every open order of the supplier.</summary>
    private sealed record BillVerdict(
        IReadOnlyDictionary<string, OpenOrder>? OrderByLineId,
        WorkOrderMatchRule Rule,
        string? Detail,
        string? ExceptionReason,
        IReadOnlyList<OpenOrder> SupplierOrders);

    /// <summary>One rule's answer: the order each line pays, or the reason none does. Pool is
    /// set when the bill names several orders and is going to the card to be split across them —
    /// the value gate then holds the bill against their combined remaining value.</summary>
    private sealed record Assignment(
        IReadOnlyDictionary<string, OpenOrder>? OrderByLineId,
        WorkOrderMatchRule Rule,
        string? Detail,
        string? Reason,
        IReadOnlyList<OpenOrder>? Pool = null)
    {
        public static Assignment Refused(string reason) => new(null, default, null, reason);
    }

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

        public WorkOrderBillOrderOption ToOption() =>
            new(WorkOrderId, Reference, Title, ProjectId, ProjectName, Value, InvoicedToDate,
                CodeWeights.Select(pair => pair.Key).ToList());
    }

    /// <summary>A name a supplier's bills may arrive under and the directory record it means.</summary>
    private readonly record struct SupplierName(string Name, string SubcontractorId);
}
