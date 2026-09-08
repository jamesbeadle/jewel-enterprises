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
        var edited = command with { Lines = new[] { new WorkOrderBillLineCoding("inv-dry:0", new[] { new XeroCostSplit("INT-PLS", 700m), new XeroCostSplit("INT-PLB", 300m) }) } };

        await fixture.ApproveAsync(edited);

        var line = fixture.Context.XeroLedgerLines.AsNoTracking().Single(candidate => candidate.XeroLedgerLineId == "inv-dry:0");
        Assert.Equal((WorkOrderBillFixture.Woodhouse, (string?)null), (line.ProjectId, line.CostCenterCode));
        Assert.Equal(new[] { ("INT-PLB", 300m), ("INT-PLS", 700m) },
            fixture.Context.XeroCostSplits.AsNoTracking().OrderBy(split => split.CostCenterCode).AsEnumerable().Select(split => (split.CostCenterCode, split.Net)));
        Assert.Equal(1000m, Assert.Single(fixture.Context.XeroLineWorkOrderLinks).Amount);
    }

    [Fact]
    public async Task ACodeTheOrderDoesNotCarryIsRefused()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        await ReferenceAsync(fixture, "inv-1724", "WO-0026");
        var command = await fixture.ProposedApprovalAsync("inv-1724");
        var foreign = command with { Lines = command.Lines.Select(line => line with { Splits = new[] { new XeroCostSplit("INT-PLS", line.Splits[0].Net) } }).ToList() };

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
        var short_ = command with { Lines = command.Lines.Select(line => line with { Splits = new[] { new XeroCostSplit("ELE-STD", 1m) } }).ToList() };

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.ApproveAsync(short_));

        Assert.Contains("must add up to its net", refusal.Message);
    }

    [Fact]
    public async Task AnOrderTheRuleWouldNotGiveTheBillIsRefused()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        await ReferenceAsync(fixture, "inv-1724", "WO-0026");
        var command = await fixture.ProposedApprovalAsync("inv-1724");

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.ApproveAsync(command with { WorkOrderId = "wo-ra-01" }));

        Assert.Contains("now matches WO-0026", refusal.Message);
    }

    [Fact]
    public async Task ABillWithNoMatchIsRefusedWithTheReason()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        var command = new ApproveWorkOrderBill("inv-1724", "wo-bf-26", new[]
        {
            new WorkOrderBillLineCoding("inv-1724:0", new[] { new XeroCostSplit("ELE-STD", 6000m) }),
            new WorkOrderBillLineCoding("inv-1724:1", new[] { new XeroCostSplit("ELE-STD", 4000m) })
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
