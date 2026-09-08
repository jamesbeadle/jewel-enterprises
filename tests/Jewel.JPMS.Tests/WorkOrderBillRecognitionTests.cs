using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero.Ledger;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// The matching rules (2026-09-08) as the unallocated read applies them: reference beats
/// supplier, numbers are per project, the labour registry wins, the value gate holds, and the
/// proposed coding is pro rata to the order's own lines to the penny.
/// </summary>
public sealed class WorkOrderBillRecognitionTests
{
    [Fact]
    public async Task ASupplierWithTwoOpenOrdersAndNoReferenceStaysInTheQueueWithTheReason()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();

        var lines = await fixture.ReadUnallocatedAsync();

        var line = lines.Single(candidate => candidate.XeroLedgerLineId == "inv-1724:0");
        Assert.Null(line.WorkOrderMatch);
        Assert.Contains("2 open work orders", line.WorkOrderExceptionReason);
        Assert.Contains("WO-0026 By France", line.WorkOrderExceptionReason);
    }

    [Fact]
    public async Task AReferenceOnTheBillPicksTheOrderAndEveryLineCarriesTheMatch()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        await SetReferenceAsync(fixture, "inv-1724", "WO-0026");

        var lines = (await fixture.ReadUnallocatedAsync()).Where(line => line.XeroInvoiceId == "inv-1724").ToList();

        Assert.All(lines, line =>
        {
            var match = Assert.IsType<WorkOrderBillMatch>(line.WorkOrderMatch);
            Assert.Equal(("wo-bf-26", "WO-0026", WorkOrderMatchRule.ByReference, WorkOrderBillFixture.ByFrance), (match.WorkOrderId, match.WorkOrderReference, match.Rule, match.ProjectId));
            Assert.Equal((97810m, 0m), (match.OrderValue, match.InvoicedToDate));
            var share = Assert.Single(match.ProposedSplits);
            Assert.Equal(("ELE-STD", line.Net, WorkOrderBillFixture.ByFrance), (share.CostCenterCode, share.Net, share.ProjectId));
        });
    }

    [Theory]
    [InlineData("WO-0026")]
    [InlineData("wo 26")]
    [InlineData("PO-26 electrics")]
    [InlineData("Ref WO#26")]
    public async Task TheReferenceIsReadHoweverTheSupplierWroteIt(string reference)
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        await SetReferenceAsync(fixture, "inv-1724", reference);

        var line = (await fixture.ReadUnallocatedAsync()).First(candidate => candidate.XeroInvoiceId == "inv-1724");

        Assert.Equal("WO-0026", line.WorkOrderMatch?.WorkOrderReference);
    }

    [Fact]
    public async Task ANumberOpenOnTwoProjectsIsBrokenByTheBillsOwnSiteOrStays()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        WorkOrderBillFixture.AddOrder(fixture.Context, "wo-wh-26", WorkOrderBillFixture.Woodhouse, 26, WorkOrderBillFixture.SubcontractorId, 5000m, ("ELE-STD", 5000m));
        await fixture.Context.SaveChangesAsync();
        await SetReferenceAsync(fixture, "inv-1724", "WO-0026");

        var unresolved = (await fixture.ReadUnallocatedAsync()).First(candidate => candidate.XeroInvoiceId == "inv-1724");
        Assert.Null(unresolved.WorkOrderMatch);
        Assert.Contains("more than one project", unresolved.WorkOrderExceptionReason);

        await SetSiteAsync(fixture, "inv-1724", "By France");
        var resolved = (await fixture.ReadUnallocatedAsync()).First(candidate => candidate.XeroInvoiceId == "inv-1724");
        Assert.Equal("wo-bf-26", resolved.WorkOrderMatch?.WorkOrderId);
    }

    [Fact]
    public async Task ASupplierWithOneOpenOrderMatchesOnTheSupplierAlone()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        WorkOrderBillFixture.AddBill(fixture.Context, "inv-dry", "77", "Drywall Co Ltd", ("321", 1000m));
        await fixture.Context.SaveChangesAsync();

        var line = (await fixture.ReadUnallocatedAsync()).Single(candidate => candidate.XeroInvoiceId == "inv-dry");

        var match = Assert.IsType<WorkOrderBillMatch>(line.WorkOrderMatch);
        Assert.Equal((WorkOrderMatchRule.BySupplier, "wo-wh-01"), (match.Rule, match.WorkOrderId));
        Assert.Contains("only open order", match.Detail);
    }

    [Fact]
    public async Task AMultiCodeOrderProposesAProRataSplitThatSumsToTheLineToThePenny()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        WorkOrderBillFixture.AddBill(fixture.Context, "inv-dry", "77", "Drywall Co Ltd", ("321", 1000.01m));
        await fixture.Context.SaveChangesAsync();

        var line = (await fixture.ReadUnallocatedAsync()).Single(candidate => candidate.XeroInvoiceId == "inv-dry");

        var splits = line.WorkOrderMatch!.ProposedSplits;
        Assert.Equal(new[] { ("INT-PLS", 600.01m), ("INT-PLB", 400.00m) }, splits.Select(split => (split.CostCenterCode, split.Net)));
        Assert.Equal(1000.01m, splits.Sum(split => split.Net));
    }

    [Fact]
    public async Task ABillThatTakesTheOrderOverItsValueStaysWithTheShortfallOnTheRow()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        WorkOrderBillFixture.AddBill(fixture.Context, "inv-big", "78", "Drywall Co Ltd", ("321", 12000m));
        await fixture.Context.SaveChangesAsync();

        var line = (await fixture.ReadUnallocatedAsync()).Single(candidate => candidate.XeroInvoiceId == "inv-big");

        Assert.Null(line.WorkOrderMatch);
        Assert.Contains("over its value by £2,000.00", line.WorkOrderExceptionReason);
    }

    [Fact]
    public async Task AnOrderAlreadyInvoicedToItsValueIsNotOpen()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        fixture.Context.XeroLineWorkOrderLinks.Add(new XeroLineWorkOrderLinkEntity { XeroLineWorkOrderLinkId = "L1", XeroLedgerLineId = "old", WorkOrderId = "wo-ra-01", ProjectId = WorkOrderBillFixture.Ravenswood, Amount = 14940m });
        await fixture.Context.SaveChangesAsync();

        // Ravenswood is fully invoiced, so By France is the supplier's only open order.
        var line = (await fixture.ReadUnallocatedAsync()).First(candidate => candidate.XeroInvoiceId == "inv-1725");

        Assert.Equal("wo-bf-26", line.WorkOrderMatch?.WorkOrderId);
    }

    [Fact]
    public async Task ASupplierOnTheLabourRegistryStaysOnTheCoverRoute()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        fixture.Context.Workers.Add(new WorkerEntity { WorkerId = "w1", Name = "Dave Drywall", SubcontractorId = "sub-dry", IsActive = true });
        WorkOrderBillFixture.AddBill(fixture.Context, "inv-dry", "77", "Drywall Co Ltd", ("321", 1000m));
        await fixture.Context.SaveChangesAsync();

        var line = (await fixture.ReadUnallocatedAsync()).Single(candidate => candidate.XeroInvoiceId == "inv-dry");

        Assert.Null(line.WorkOrderMatch);
        Assert.Contains("labour registry", line.WorkOrderExceptionReason);
        Assert.Equal("w1", line.MatchedWorkerId);
    }

    [Fact]
    public async Task ASupplierWithNoOpenOrderCarriesNeitherAMatchNorAReason()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        WorkOrderBillFixture.AddBill(fixture.Context, "inv-gs", "0056", "Grant & Stone Limited", ("310", 353.33m));
        await fixture.Context.SaveChangesAsync();

        var line = (await fixture.ReadUnallocatedAsync()).Single(candidate => candidate.XeroInvoiceId == "inv-gs");

        Assert.Null(line.WorkOrderMatch);
        Assert.Null(line.WorkOrderExceptionReason);
    }

    private static async Task SetReferenceAsync(WorkOrderBillFixture fixture, string invoiceId, string reference)
    {
        foreach (var line in await fixture.Context.XeroLedgerLines.Where(line => line.XeroInvoiceId == invoiceId).ToListAsync())
            line.Reference = reference;
        await fixture.Context.SaveChangesAsync();
    }

    private static async Task SetSiteAsync(WorkOrderBillFixture fixture, string invoiceId, string site)
    {
        foreach (var line in await fixture.Context.XeroLedgerLines.Where(line => line.XeroInvoiceId == invoiceId).ToListAsync())
            line.XeroSite = site;
        await fixture.Context.SaveChangesAsync();
    }
}
