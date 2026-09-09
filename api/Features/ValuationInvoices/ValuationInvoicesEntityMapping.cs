using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.ValuationInvoices;

internal static class ValuationInvoicesEntityMapping
{
    public static ValuationInvoice ToModel(this ValuationInvoiceEntity entity) => new(
        ValuationInvoiceId: entity.ValuationInvoiceId,
        ProjectId: entity.ProjectId,
        ValuationClaimId: entity.ValuationClaimId,
        Number: entity.Number,
        Reference: entity.Reference,
        PeriodMonth: entity.PeriodMonth,
        Amount: entity.Amount,
        AmountPaid: entity.AmountPaid,
        Status: (ValuationInvoiceStatus)entity.Status,
        RaisedAt: entity.RaisedAt,
        IssuedAt: entity.IssuedAt,
        PaidAt: entity.PaidAt,
        SubmittedAt: entity.SubmittedAt,
        ApprovedAt: entity.ApprovedAt,
        RejectedAt: entity.RejectedAt,
        CancelledAt: entity.CancelledAt,
        RejectionReason: entity.RejectionReason,
        AmendmentCount: entity.AmendmentCount,
        IsManual: entity.IsManual,
        ValuationReportSnapshotId: entity.ValuationReportSnapshotId,
        DepositCredited: entity.DepositCredited,
        XeroInvoiceId: entity.XeroInvoiceId,
        XeroInvoiceNumber: entity.XeroInvoiceNumber,
        XeroRaisedAt: entity.XeroRaisedAt);

    public static ValuationInvoiceEvent ToModel(this ValuationInvoiceEventEntity entity) => new(
        ValuationInvoiceEventId: entity.ValuationInvoiceEventId,
        ValuationInvoiceId: entity.ValuationInvoiceId,
        EventType: (ValuationInvoiceEventType)entity.EventType,
        OccurredAt: entity.OccurredAt,
        Note: entity.Note,
        AmountBefore: entity.AmountBefore,
        AmountAfter: entity.AmountAfter);
}
