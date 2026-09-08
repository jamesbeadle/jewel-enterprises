using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

/// <summary>
/// Approves a covered labour bill in Xero (2026-09-08): DRAFT → AUTHORISED, exactly as the run
/// coded it — no line is touched — and only while every worker-month the bill covers reads
/// Matches. The decision is made on the settlement schedules as they stand and then on the
/// bill as Xero holds it now; the ledger's copy is stamped AUTHORISED and a BillApproved outcome
/// is recorded against every worker on the bill, in the same save.
/// </summary>
public sealed class ApproveLabourBillHandler : ICommandHandler<ApproveLabourBill, LabourBillApproval>
{
    private const string Authorised = "AUTHORISED";

    private readonly JpmsContext context;
    private readonly SettlementScheduleBuilder builder;
    private readonly IXeroClient xero;

    public ApproveLabourBillHandler(JpmsContext context, SettlementScheduleBuilder builder, IXeroClient xero)
    { this.context = context; this.builder = builder; this.xero = xero; }

    public Task<LabourBillApproval> HandleAsync(ApproveLabourBill command, CancellationToken cancellationToken) =>
        HandleAsync(command, command.ApprovedByEmail, cancellationToken);

    public async Task<LabourBillApproval> HandleAsync(ApproveLabourBill command, string approvedByEmail, CancellationToken cancellationToken)
    {
        var monthStart = new DateTimeOffset(new DateTime(command.Year, command.Month, 1), TimeSpan.Zero);
        var snapshot = await builder.BuildAsync(command.Year, command.Month, cancellationToken);
        var workersOnBill = snapshot.Workers.Where(worker => worker.CoveredBill?.XeroInvoiceId == command.XeroInvoiceId).ToList();
        if (workersOnBill.Count == 0)
            throw new InvalidOperationException(
                $"No worker-month in {monthStart:MMM yyyy} is covered by bill {command.XeroInvoiceId} — run the coding run, or mark the cover, first.");
        var bill = workersOnBill[0].CoveredBill!;
        var notMatching = workersOnBill.Where(worker => worker.Verdict != ScheduleVerdict.Matches).ToList();
        if (notMatching.Count > 0)
            throw new InvalidOperationException(
                $"Bill {bill.Label} can't be approved yet — every worker on it must read Matches, and "
                + string.Join(", ", notMatching.Select(worker => $"{worker.WorkerName} reads {worker.Verdict}")) + ".");

        var approval = await xero.ApproveInvoiceAsync(
            new XeroApprovalRequest(command.XeroInvoiceId, IsCreditNote: false, Array.Empty<XeroApprovalLineInstruction>()), cancellationToken);
        if (!approval.Succeeded) throw new InvalidOperationException(approval.Error ?? "Xero refused the approval.");

        var status = approval.FreshStatus ?? Authorised;
        await StampLedgerAsync(command.XeroInvoiceId, status, cancellationToken);
        if (!approval.AlreadyApproved)
            context.XeroCodingRuns.AddRange(workersOnBill.Select(worker => ApprovalRecord(worker, bill, monthStart, approvedByEmail)));
        await context.SaveChangesAsync(cancellationToken);
        return new LabourBillApproval(command.XeroInvoiceId, bill.Label, status, approval.AlreadyApproved, bill.WorkerNames);
    }

    /// <summary>The ledger's copy of the bill reads the new status at once, rather than after the
    /// next sync — the settlement view is looked at straight after the click.</summary>
    private async Task StampLedgerAsync(string billId, string status, CancellationToken cancellationToken)
    {
        var lines = await context.XeroLedgerLines.Where(line => line.XeroInvoiceId == billId).ToListAsync(cancellationToken);
        foreach (var line in lines) line.InvoiceStatus = status;
    }

    private static XeroCodingRunEntity ApprovalRecord(
        WorkerSettlementSchedule worker, CoveredBill bill, DateTimeOffset monthStart, string approvedByEmail) => new()
    {
        XeroCodingRunId = LabourIdentifierFactory.NextXeroCodingRunId(),
        WorkerId = worker.WorkerId,
        Month = monthStart,
        Outcome = (int)XeroCodingOutcome.BillApproved,
        XeroBillId = bill.XeroInvoiceId,
        Detail = XeroCodingWording.Truncate(
            $"Approved in Xero by {(string.IsNullOrWhiteSpace(approvedByEmail) ? "the portal" : approvedByEmail)}: bill \"{bill.Label}\" "
            + $"{bill.Status} → {Authorised}, £{bill.Total:N2}, covering {string.Join(", ", bill.WorkerNames)} — every worker read Matches.", 2000)!,
        RunByEmail = approvedByEmail,
        RunAt = DateTimeOffset.UtcNow,
    };
}
