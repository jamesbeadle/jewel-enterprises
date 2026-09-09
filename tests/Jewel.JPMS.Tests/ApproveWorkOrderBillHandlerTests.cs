using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// The FD's one press (2026-09-08): every line allocated from the order, every line linked for
/// its net, the approval recorded, one Xero write asked for — and the refusals that keep a
/// stale card or a foreign code from getting through.
/// </summary>
public sealed class ApproveWorkOrderBillHandlerTests
{
    [Fact]
    public async Task ApprovingAllocatesLinksRecordsAndAsksXeroOnce()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        await ReferenceAsync(fixture, "inv-1724", "WO-0026");

        var outcome = await fixture.ApproveAsync(await fixture.ProposedApprovalAsync("inv-1724"));

        Assert.Equal((2, true), (outcome.LinesAllocated, outcome.ApprovedInXero));
        var lines = fixture.Context.XeroLedgerLines.AsNoTracking().Where(line => line.XeroInvoiceId == "inv-1724").ToList();
        Assert.All(lines, line =>
        {
            Assert.Equal((int)XeroAllocationStatus.Allocated, line.AllocationStatus);
            Assert.Equal((WorkOrderBillFixture.ByFrance, "ELE-STD", "nigel@jewelbb.co.uk", "Work order WO-0026"), (line.ProjectId, line.CostCenterCode, line.AllocatedBy, line.Note));
        });
        var links = fixture.Context.XeroLineWorkOrderLinks.AsNoTracking().ToList();
        Assert.Equal(new[] { 6000m, 4000m }, links.OrderByDescending(link => link.Amount).Select(link => link.Amount));
        Assert.All(links, link => Assert.Equal(("wo-bf-26", WorkOrderBillFixture.ByFrance), (link.WorkOrderId, link.ProjectId)));
        var approval = Assert.Single(fixture.Context.WorkOrderBillApprovals);
        Assert.Equal(("inv-1724", "wo-bf-26", (int)WorkOrderMatchRule.ByReference, 10000m, "nigel@jewelbb.co.uk"), (approval.XeroInvoiceId, approval.WorkOrderId, approval.MatchRule, approval.BillNet, approval.ApprovedByEmail));
        Assert.Null(approval.UndoneAtUtc);
        Assert.Equal(new[] { "WorkOrderBill:inv-1724" }, fixture.WriteBack.Calls);
        Assert.Equal((int)AuditEventType.WorkOrderBillApproved, Assert.Single(fixture.Context.AuditEvents).EventType);
        Assert.Empty(fixture.Context.XeroCostSplits);
        // The 321/322 lines are untouched — the account codes are Xero's, and the order's own
        // coding never recodes the order.
        Assert.Equal(new[] { "321", "322" }, lines.OrderBy(line => line.AccountCode).Select(line => line.AccountCode));
        Assert.All(fixture.Context.WorkOrderLines.Where(line => line.WorkOrderId == "wo-bf-26"), line => Assert.Equal("ELE-STD", line.CostCode));
    }

    [Fact]
    public async Task TheAccountantsTestCase_BothBillsApprovedOnceEach_FourLinesLinked_QueueDownToTheRest()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        WorkOrderBillFixture.AddBill(fixture.Context, "inv-gs", "0056", "Grant & Stone Limited", ("310", 353.33m));
        await fixture.Context.SaveChangesAsync();
        await ReferenceAsync(fixture, "inv-1724", "WO-0026");
        await ReferenceAsync(fixture, "inv-1725", "Ravenswood WO-0001");
        await SiteAsync(fixture, "inv-1725", "Ravenswood Ave");

        await fixture.ApproveAsync(await fixture.ProposedApprovalAsync("inv-1724"));
        await fixture.ApproveAsync(await fixture.ProposedApprovalAsync("inv-1725"));

        Assert.Equal(4, fixture.Context.XeroLineWorkOrderLinks.Count());
        Assert.Equal(10000m, fixture.Context.XeroLineWorkOrderLinks.Where(link => link.WorkOrderId == "wo-bf-26").Sum(link => link.Amount));
        Assert.Equal(5976m, fixture.Context.XeroLineWorkOrderLinks.Where(link => link.WorkOrderId == "wo-ra-01").Sum(link => link.Amount));
        var remaining = await fixture.ReadUnallocatedAsync();
        Assert.Equal(new[] { "inv-gs:0" }, remaining.Select(line => line.XeroLedgerLineId));
        Assert.Equal(new[] { "WorkOrderBill:inv-1724", "WorkOrderBill:inv-1725" }, fixture.WriteBack.Calls);
        var allocated = await fixture.ReadAllocatedAsync();
        Assert.All(allocated, line => Assert.Equal("nigel@jewelbb.co.uk", line.WorkOrderApproval?.ApprovedBy));
    }

    [Fact]
    public async Task AMultiCodeOrderIsApprovedAsACentreSplitOnTheOrdersProject()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        WorkOrderBillFixture.AddBill(fixture.Context, "inv-dry", "77", "Drywall Co Ltd", ("321", 1000m));
        await fixture.Context.SaveChangesAsync();
        var command = await fixture.ProposedApprovalAsync("inv-dry");
        var edited = command with { Lines = new[] { new WorkOrderBillLineCoding("inv-dry:0", new[] { new WorkOrderBillShare("wo-wh-01", "INT-PLS", 700m), new WorkOrderBillShare("wo-wh-01", "INT-PLB", 300m) }) } };

        await fixture.ApproveAsync(edited);

        var line = fixture.Context.XeroLedgerLines.AsNoTracking().Single(candidate => candidate.XeroLedgerLineId == "inv-dry:0");
        Assert.Equal((WorkOrderBillFixture.Woodhouse, (string?)null), (line.ProjectId, line.CostCenterCode));
        Assert.Equal(new[] { ("INT-PLB", 300m), ("INT-PLS", 700m) },
            fixture.Context.XeroCostSplits.AsNoTracking().OrderBy(split => split.CostCenterCode).AsEnumerable().Select(split => (split.CostCenterCode, split.Net)));
        Assert.Equal(new[] { ("INT-PLB", 300m), ("INT-PLS", 700m) },
            fixture.Context.XeroLineWorkOrderLinks.AsNoTracking().OrderBy(link => link.CostCenterCode).AsEnumerable().Select(link => (link.CostCenterCode!, link.Amount)));
    }

    [Fact]
    public async Task TheAccountantsSecondCase_OneBillSplitAcrossTwoOrders_LinkedAndApprovedPerOrder()
    {
        // Sussex Tiling Lees Green-001 (2026-09-09): £1,748 to WO-0055 and £1,344 to WO-0056, one bill.
        var fixture = await WorkOrderBillFixture.CreateAsync();
        WorkOrderBillFixture.AddOrder(fixture.Context, "wo-lg-55", WorkOrderBillFixture.Woodhouse, 55, "sub-dry", 1748m, ("INT-PLS", 1748m));
        WorkOrderBillFixture.AddOrder(fixture.Context, "wo-lg-56", WorkOrderBillFixture.Woodhouse, 56, "sub-dry", 2000m, ("INT-PLB", 2000m));
        WorkOrderBillFixture.AddBill(fixture.Context, "inv-lg", "Lees Green-001", "Drywall Co Ltd", ("321", 3092m));
        await fixture.Context.SaveChangesAsync();
        await ReferenceAsync(fixture, "inv-lg", "WO-0055 / WO-0056");
        var proposed = await fixture.ProposedApprovalAsync("inv-lg");
        var split = proposed with { Lines = new[] { new WorkOrderBillLineCoding("inv-lg:0", new[] { new WorkOrderBillShare("wo-lg-55", "INT-PLS", 1748m), new WorkOrderBillShare("wo-lg-56", "INT-PLB", 1344m) }) } };

        var outcome = await fixture.ApproveAsync(split);

        Assert.Equal(new[] { "WO-0055", "WO-0056" }, outcome.WorkOrderReferences);
        var line = fixture.Context.XeroLedgerLines.AsNoTracking().Single(candidate => candidate.XeroLedgerLineId == "inv-lg:0");
        Assert.Equal((WorkOrderBillFixture.Woodhouse, (string?)null, "Work orders WO-0055, WO-0056"), (line.ProjectId, line.CostCenterCode, line.Note));
        Assert.Equal(new[] { ("wo-lg-55", "INT-PLS", 1748m), ("wo-lg-56", "INT-PLB", 1344m) },
            fixture.Context.XeroLineWorkOrderLinks.AsNoTracking().OrderBy(link => link.WorkOrderId).AsEnumerable().Select(link => (link.WorkOrderId, link.CostCenterCode!, link.Amount)));
        Assert.Equal(new[] { ("wo-lg-55", 1748m), ("wo-lg-56", 1344m) },
            fixture.Context.WorkOrderBillApprovals.AsNoTracking().OrderBy(row => row.WorkOrderId).AsEnumerable().Select(row => (row.WorkOrderId, row.BillNet)));
        var allocated = (await fixture.ReadAllocatedAsync()).Single(candidate => candidate.XeroLedgerLineId == "inv-lg:0");
        Assert.Equal("WO-0055 + WO-0056", allocated.WorkOrderApproval?.OrdersLabel);
    }

    [Fact]
    public async Task AShareOnAnOrderThatIsNotTheSuppliersIsRefused()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        await ReferenceAsync(fixture, "inv-1724", "WO-0026");
        var command = await fixture.ProposedApprovalAsync("inv-1724");
        var foreign = command with { Lines = command.Lines.Select(line => line with { Shares = new[] { new WorkOrderBillShare("wo-wh-01", "INT-PLS", line.Shares[0].Net) } }).ToList() };

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.ApproveAsync(foreign));

        Assert.Contains("not an open order of this supplier", refusal.Message);
    }

    [Fact]
    public async Task AShareThatTakesItsOrderOverValueIsRefused()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        await ReferenceAsync(fixture, "inv-1725", "WO-0001");
        await SiteAsync(fixture, "inv-1725", "Ravenswood Ave");
        var command = await fixture.ProposedApprovalAsync("inv-1725");
        // Ravenswood's WO-0001 is £14,940; £10,000 invoiced since the card was drawn leaves
        // £4,940, and the £5,976 bill no longer fits.
        fixture.Context.XeroLineWorkOrderLinks.Add(new XeroLineWorkOrderLinkEntity { XeroLineWorkOrderLinkId = "L1", XeroLedgerLineId = "old", WorkOrderId = "wo-ra-01", ProjectId = WorkOrderBillFixture.Ravenswood, Amount = 10000m });
        await fixture.Context.SaveChangesAsync();

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.ApproveAsync(command));

        Assert.Contains("over its value", refusal.Message);
    }

    [Fact]
    public async Task ACodeTheOrderDoesNotCarryIsRefused()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        await ReferenceAsync(fixture, "inv-1724", "WO-0026");
        var command = await fixture.ProposedApprovalAsync("inv-1724");
        var foreign = command with { Lines = command.Lines.Select(line => line with { Shares = new[] { new WorkOrderBillShare("wo-bf-26", "INT-PLS", line.Shares[0].Net) } }).ToList() };

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.ApproveAsync(foreign));

        Assert.Contains("carries no INT-PLS line", refusal.Message);
        Assert.Empty(fixture.WriteBack.Calls);
    }

    [Fact]
    public async Task SharesThatDoNotAddUpToTheLineAreRefused()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        await ReferenceAsync(fixture, "inv-1724", "WO-0026");
        var command = await fixture.ProposedApprovalAsync("inv-1724");
        var short_ = command with { Lines = command.Lines.Select(line => line with { Shares = new[] { new WorkOrderBillShare("wo-bf-26", "ELE-STD", 1m) } }).ToList() };

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.ApproveAsync(short_));

        Assert.Contains("must add up to its net", refusal.Message);
    }

    [Fact]
    public async Task ABillWithNoMatchIsRefusedWithTheReason()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        var command = new ApproveWorkOrderBill("inv-1724", new[]
        {
            new WorkOrderBillLineCoding("inv-1724:0", new[] { new WorkOrderBillShare("wo-bf-26", "ELE-STD", 6000m) }),
            new WorkOrderBillLineCoding("inv-1724:1", new[] { new WorkOrderBillShare("wo-bf-26", "ELE-STD", 4000m) })
        });

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.ApproveAsync(command));

        Assert.Contains("2 open work orders", refusal.Message);
    }

    [Fact]
    public async Task ALineAlreadyAllocatedOrALineLeftOutIsRefused()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        await ReferenceAsync(fixture, "inv-1724", "WO-0026");
        var command = await fixture.ProposedApprovalAsync("inv-1724");

        var partial = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.ApproveAsync(command with { Lines = command.Lines.Take(1).ToList() }));
        Assert.Contains("every line of the bill at once", partial.Message);

        var stored = await fixture.Context.XeroLedgerLines.FirstAsync(line => line.XeroLedgerLineId == "inv-1724:1");
        stored.AllocationStatus = (int)XeroAllocationStatus.Allocated;
        await fixture.Context.SaveChangesAsync();
        var moved = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.ApproveAsync(command));
        Assert.Contains("still be unallocated", moved.Message);
    }

    [Fact]
    public async Task XerosRefusalIsReportedButTheAllocationStands()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        await ReferenceAsync(fixture, "inv-1724", "WO-0026");
        fixture.WriteBack.WorkOrderBillOutcome = new XeroWriteBackOutcome(false, "No Xero site is mapped for By France");

        var outcome = await fixture.ApproveAsync(await fixture.ProposedApprovalAsync("inv-1724"));

        Assert.Equal((false, "No Xero site is mapped for By France"), (outcome.ApprovedInXero, outcome.XeroError));
        Assert.All(fixture.Context.XeroLedgerLines.Where(line => line.XeroInvoiceId == "inv-1724"), line => Assert.Equal((int)XeroAllocationStatus.Allocated, line.AllocationStatus));
    }

    internal static async Task ReferenceAsync(WorkOrderBillFixture fixture, string invoiceId, string reference)
    {
        foreach (var line in await fixture.Context.XeroLedgerLines.Where(line => line.XeroInvoiceId == invoiceId).ToListAsync())
            line.Reference = reference;
        await fixture.Context.SaveChangesAsync();
    }

    internal static async Task SiteAsync(WorkOrderBillFixture fixture, string invoiceId, string site)
    {
        foreach (var line in await fixture.Context.XeroLedgerLines.Where(line => line.XeroInvoiceId == invoiceId).ToListAsync())
            line.XeroSite = site;
        await fixture.Context.SaveChangesAsync();
    }
}
