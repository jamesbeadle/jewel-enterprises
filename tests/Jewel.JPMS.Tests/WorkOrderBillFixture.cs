using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Xero.Ledger;
using Jewel.JPMS.Api.Features.Xero.Ledger.WorkOrderBills;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jewel.JPMS.Tests;

/// <summary>
/// The accountant's test case as a fixture: Anything Electrical with a Released order on By
/// France (WO-0026, ELE-STD, £97,810) and one on Ravenswood (WO-0001, £14,940), a two-code
/// order on Woodhouse, and bills 1724 / 1725 published by Dext as a 321/322 pair each. Every
/// test adds or removes from here rather than building its own world.
/// </summary>
internal sealed class WorkOrderBillFixture
{
    public const string Supplier = "Anything Electrical (London) Ltd";
    public const string SubcontractorId = "sub-ae";
    public const string ByFrance = "P-BF";
    public const string Ravenswood = "P-RA";
    public const string Woodhouse = "P-WH";

    public JpmsContext Context { get; }
    public RecordingWriteBack WriteBack { get; } = new();

    private WorkOrderBillFixture(JpmsContext context) { Context = context; }

    public static async Task<WorkOrderBillFixture> CreateAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"work-order-bills-{Guid.NewGuid():N}").Options);
        context.Projects.Add(new ProjectEntity { ProjectId = ByFrance, Reference = "JBB-2026-001", Name = "By France", ClientName = "Client", XeroSiteName = "By France" });
        context.Projects.Add(new ProjectEntity { ProjectId = Ravenswood, Reference = "JBB-2026-003", Name = "Ravenswood Ave", ClientName = "Client", XeroSiteName = "Ravenswood Ave" });
        context.Projects.Add(new ProjectEntity { ProjectId = Woodhouse, Reference = "JBB-2026-004", Name = "Woodhouse", ClientName = "Client", XeroSiteName = "Woodhouse" });
        context.CostCenters.Add(new CostCenterEntity { CostCenterId = "CC1", Code = "ELE-STD", Name = "Electrician", IsActive = true });
        context.CostCenters.Add(new CostCenterEntity { CostCenterId = "CC2", Code = "INT-PLS", Name = "Plastering", IsActive = true });
        context.CostCenters.Add(new CostCenterEntity { CostCenterId = "CC3", Code = "INT-PLB", Name = "Plaster boarding", IsActive = true });
        context.Subcontractors.Add(new SubcontractorEntity { SubcontractorId = SubcontractorId, CompanyName = Supplier });
        context.Subcontractors.Add(new SubcontractorEntity { SubcontractorId = "sub-dry", CompanyName = "Drywall Co Ltd" });
        AddOrder(context, "wo-bf-26", ByFrance, 26, SubcontractorId, 97810m, ("ELE-STD", 97810m));
        AddOrder(context, "wo-ra-01", Ravenswood, 1, SubcontractorId, 14940m, ("ELE-STD", 14940m));
        AddOrder(context, "wo-wh-01", Woodhouse, 1, "sub-dry", 10000m, ("INT-PLS", 6000m), ("INT-PLB", 4000m));
        AddBill(context, "inv-1724", "1724", Supplier, ("321", 6000m), ("322", 4000m));
        AddBill(context, "inv-1725", "1725", Supplier, ("321", 3000m), ("322", 2976m));
        await context.SaveChangesAsync();
        return new WorkOrderBillFixture(context);
    }

    public static void AddOrder(JpmsContext context, string id, string projectId, int number, string subcontractorId, decimal value, params (string Code, decimal Total)[] lines)
    {
        context.WorkOrders.Add(new WorkOrderEntity
        {
            WorkOrderId = id, ProjectId = projectId, Number = number, SubcontractorId = subcontractorId, Value = value,
            Title = $"Order {number}", Status = (int)WorkOrderStatus.Released, AwardedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow
        });
        var sortOrder = 0;
        foreach (var (code, total) in lines)
            context.WorkOrderLines.Add(new WorkOrderLineEntity
            {
                WorkOrderLineId = $"{id}:{code}", WorkOrderId = id, Title = code, CostCode = code, LineTotal = total, SortOrder = sortOrder++
            });
    }

    public static void AddBill(JpmsContext context, string invoiceId, string number, string contactName, params (string Account, decimal Net)[] lines)
    {
        var index = 0;
        foreach (var (account, net) in lines)
            context.XeroLedgerLines.Add(new XeroLedgerLineEntity
            {
                XeroLedgerLineId = $"{invoiceId}:{index}", XeroInvoiceId = invoiceId, XeroLineItemId = $"line-{index++}", Type = "ACCPAY",
                InvoiceNumber = number, ContactName = contactName, InvoiceStatus = "DRAFT", Net = net, InvoiceTotal = lines.Sum(line => line.Net),
                AmountDue = lines.Sum(line => line.Net), AccountCode = account, Description = "Electrical works",
                FirstSeenAtUtc = DateTimeOffset.UtcNow, LastSyncedAtUtc = DateTimeOffset.UtcNow
            });
    }

    public async Task<IReadOnlyList<XeroLedgerLine>> ReadUnallocatedAsync() =>
        await new ListXeroLedgerLinesHandler(Context).HandleAsync(new ListXeroLedgerLines(XeroAllocationStatus.Unallocated), CancellationToken.None);

    public async Task<IReadOnlyList<XeroLedgerLine>> ReadAllocatedAsync() =>
        await new ListXeroLedgerLinesHandler(Context).HandleAsync(new ListXeroLedgerLines(XeroAllocationStatus.Allocated), CancellationToken.None);

    public Task<WorkOrderBillApprovalOutcome> ApproveAsync(ApproveWorkOrderBill command) =>
        new ApproveWorkOrderBillHandler(Context, WriteBack, Audit()).HandleAsync(command with { ApprovedBy = "nigel@jewelbb.co.uk" }, CancellationToken.None);

    public Task<WorkOrderBillUndoOutcome> UndoAsync(string invoiceId) =>
        new UndoWorkOrderBillApprovalHandler(Context, WriteBack, Audit()).HandleAsync(new UndoWorkOrderBillApproval(invoiceId, "nigel@jewelbb.co.uk"), CancellationToken.None);

    /// <summary>The card's default: every line coded exactly as recognition proposed it.</summary>
    public async Task<ApproveWorkOrderBill> ProposedApprovalAsync(string invoiceId)
    {
        var lines = (await ReadUnallocatedAsync()).Where(line => line.XeroInvoiceId == invoiceId).ToList();
        return new ApproveWorkOrderBill(invoiceId,
            lines.Select(line => new WorkOrderBillLineCoding(line.XeroLedgerLineId, line.WorkOrderMatch!.ProposedShares)).ToList());
    }

    private AuditTrail Audit() => new(Context, new AuditActor { Email = "nigel@jewelbb.co.uk" }, NullLogger<AuditTrail>.Instance);
}
