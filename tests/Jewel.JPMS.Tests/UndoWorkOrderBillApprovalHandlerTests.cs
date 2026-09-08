using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// The undo (2026-09-08): everything the approval wrote reversed in one save, the tracking
/// clear asked of Xero, and the bill's Xero status reported honestly — plus the refusals for
/// a paid bill and a line someone has since re-coded by hand.
/// </summary>
public sealed class UndoWorkOrderBillApprovalHandlerTests
{
    [Fact]
    public async Task UndoReturnsTheLinesRemovesTheLinksMarksTheApprovalAndClearsTheTracking()
    {
        var fixture = await ApprovedFixtureAsync();
        fixture.Context.ReconciliationPackageCostLines.Add(new ReconciliationPackageCostLineEntity { ReconciliationPackageCostLineId = "RP1", ReconciliationPackageId = "PKG", ProjectId = WorkOrderBillFixture.ByFrance, XeroLedgerLineId = "inv-1724:0", Amount = 6000m });
        await fixture.Context.SaveChangesAsync();

        var outcome = await fixture.UndoAsync("inv-1724");

        Assert.Equal((2, true, "AUTHORISED"), (outcome.LinesReturned, outcome.TrackingCleared, outcome.XeroStatus));
        Assert.All(fixture.Context.XeroLedgerLines.AsNoTracking().Where(line => line.XeroInvoiceId == "inv-1724"), line =>
        {
            Assert.Equal((int)XeroAllocationStatus.Unallocated, line.AllocationStatus);
            Assert.Equal((null, null, null, null, (int)XeroWriteBackStatus.None), (line.ProjectId, line.CostCenterCode, line.AllocatedBy, line.Note, line.WriteBackStatus));
        });
        Assert.Empty(fixture.Context.XeroLineWorkOrderLinks);
        Assert.Empty(fixture.Context.ReconciliationPackageCostLines);
        var approval = Assert.Single(fixture.Context.WorkOrderBillApprovals);
        Assert.Equal("nigel@jewelbb.co.uk", approval.UndoneByEmail);
        Assert.NotNull(approval.UndoneAtUtc);
        Assert.Contains("ClearTracking:inv-1724", fixture.WriteBack.Calls);
        Assert.Equal((int)AuditEventType.WorkOrderBillApprovalUndone, fixture.Context.AuditEvents.OrderBy(row => row.OccurredAt).Last().EventType);
        // Back on the tab, ready to be approved again.
        var line = (await fixture.ReadUnallocatedAsync()).First(candidate => candidate.XeroInvoiceId == "inv-1724");
        Assert.Equal("wo-bf-26", line.WorkOrderMatch?.WorkOrderId);
    }

    [Fact]
    public async Task ReApprovingAfterAnUndoAddsASecondApprovalRow()
    {
        var fixture = await ApprovedFixtureAsync();
        await fixture.UndoAsync("inv-1724");

        await fixture.ApproveAsync(await fixture.ProposedApprovalAsync("inv-1724"));

        Assert.Equal(2, fixture.Context.WorkOrderBillApprovals.Count());
        Assert.Single(fixture.Context.WorkOrderBillApprovals.Where(row => row.UndoneAtUtc == null));
        Assert.Equal(2, fixture.Context.XeroLineWorkOrderLinks.Count());
    }

    [Fact]
    public async Task UndoWithNothingStandingIsRefused()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.UndoAsync("inv-1724"));

        Assert.Contains("No Work Order bill approval stands", refusal.Message);
    }

    [Fact]
    public async Task APaidBillIsRefused()
    {
        var fixture = await ApprovedFixtureAsync();
        foreach (var line in await fixture.Context.XeroLedgerLines.Where(line => line.XeroInvoiceId == "inv-1724").ToListAsync())
            line.InvoiceStatus = "PAID";
        await fixture.Context.SaveChangesAsync();

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.UndoAsync("inv-1724"));

        Assert.Contains("paid in Xero", refusal.Message);
    }

    [Fact]
    public async Task ALineReAllocatedByHandSinceTheApprovalIsRefused()
    {
        var fixture = await ApprovedFixtureAsync();
        var line = await fixture.Context.XeroLedgerLines.FirstAsync(candidate => candidate.XeroLedgerLineId == "inv-1724:0");
        line.AllocatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(5);
        line.CostCenterCode = "INT-PLS";
        await fixture.Context.SaveChangesAsync();

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.UndoAsync("inv-1724"));

        Assert.Contains("re-allocated by hand", refusal.Message);
        Assert.Equal(2, fixture.Context.XeroLineWorkOrderLinks.Count());
    }

    private static async Task<WorkOrderBillFixture> ApprovedFixtureAsync()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        await ApproveWorkOrderBillHandlerTests.ReferenceAsync(fixture, "inv-1724", "WO-0026");
        await fixture.ApproveAsync(await fixture.ProposedApprovalAsync("inv-1724"));
        fixture.WriteBack.Calls.Clear();
        return fixture;
    }
}
