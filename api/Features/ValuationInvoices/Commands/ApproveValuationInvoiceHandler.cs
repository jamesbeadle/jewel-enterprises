using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Site.Drafts;
using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

/// <summary>
/// Submitted -> Approved. Records the client's approval; the amount still doesn't count toward
/// "Certified to date" until the invoice is issued.
///
/// <para>Approval is the certification moment — the architect has accepted the claim's
/// percentages — so it also opens a draft programme update from the claim behind the invoice
/// (decision 2026-09-08), for the Programme tab to review. The draft is best effort and saved in
/// its own unit of work AFTER the approval has been committed: a project with no programme tasks
/// simply gets no draft, and a failure in the drafting is logged, never allowed to undo or
/// refuse the approval that accounts just recorded.</para>
/// </summary>
public sealed class ApproveValuationInvoiceHandler : ICommandHandler<ApproveValuationInvoice, ValuationInvoice>
{
    private readonly JpmsContext context;
    private readonly AuditActor actor;
    private readonly ILogger<ApproveValuationInvoiceHandler> logger;

    public ApproveValuationInvoiceHandler(JpmsContext context, AuditActor actor, ILogger<ApproveValuationInvoiceHandler> logger)
    {
        this.context = context; this.actor = actor; this.logger = logger;
    }

    public async Task<ValuationInvoice> HandleAsync(ApproveValuationInvoice command, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationInvoices.FindAsync(new object[] { command.ValuationInvoiceId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Valuation invoice {command.ValuationInvoiceId} not found.");
        if (entity.Status != (int)ValuationInvoiceStatus.Submitted)
            throw new InvalidOperationException("Only a Submitted valuation invoice can be approved.");

        entity.Status = (int)ValuationInvoiceStatus.Approved;
        entity.ApprovedAt = DateTimeOffset.UtcNow;

        ValuationInvoiceAuditTrail.Append(context, entity.ValuationInvoiceId,
            ValuationInvoiceEventType.Approved, command.Note ?? "", amountAfter: entity.Amount);

        await context.SaveChangesAsync(cancellationToken);

        await OpenProgrammeDraftAsync(entity.ProjectId, entity.ValuationClaimId, cancellationToken);

        return entity.ToModel();
    }

    private async Task OpenProgrammeDraftAsync(string projectId, string? valuationClaimId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(valuationClaimId)) return;
        try
        {
            var openedBy = string.IsNullOrWhiteSpace(actor.Email) ? "the approval" : actor.Email;
            var draft = await ProgrammeDraftCapture.OpenAsync(context, projectId, valuationClaimId, openedBy, cancellationToken);
            if (draft is null) return;
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception failure) when (failure is not OperationCanceledException)
        {
            logger.LogWarning(failure,
                "The approval of the valuation invoice on project {ProjectId} was recorded, but opening the draft programme update from claim {ValuationClaimId} failed.",
                projectId, valuationClaimId);
        }
    }
}
