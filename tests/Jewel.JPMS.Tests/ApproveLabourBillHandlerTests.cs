using Jewel.JPMS.Api.Features.Labour.Commands;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Xunit;
using static Jewel.JPMS.Tests.JewelPropertyServeFixture;

namespace Jewel.JPMS.Tests;

/// <summary>
/// The accountant's ask once J and K ran (8 Sep 2026): a covered labour bill that reads Matches
/// for every worker on it is approved in Xero from the portal — DRAFT → AUTHORISED, lines
/// untouched — and never while any worker on it reads anything else. The settlement view says
/// which bill covers each worker and when the rule is met; approving is recorded per worker.
/// </summary>
public sealed class ApproveLabourBillHandlerTests
{
    [Fact]
    public async Task TheSettlementViewNamesEachWorkersCoveredBillAndWhenItCanBeApproved()
    {
        var fixture = await CreateAsync();
        fixture.AddCoveredCompanyBill("jps-bill", "DRAFT");
        await fixture.Context.SaveChangesAsync();

        var schedules = await fixture.SchedulesAsync();

        Assert.All(schedules.Workers, worker => Assert.Equal(ScheduleVerdict.Matches, worker.Verdict));
        Assert.All(schedules.Workers, worker =>
        {
            var bill = worker.CoveredBill!;
            Assert.Equal(("jps-bill", "INV-1252", "DRAFT", 6470m, true), (bill.XeroInvoiceId, bill.Label, bill.Status, bill.Total, bill.IsApprovable));
            Assert.Equal(new[] { "Dan Prowse", "Finley Taylor", "John Ahern" }, bill.WorkerNames);
        });
    }

    [Fact]
    public async Task ABillIsNotApprovableWhileAnyWorkerOnItReadsAVariance()
    {
        var fixture = await CreateAsync();
        fixture.AddCoveredCompanyBill("jps-bill", "DRAFT", johnNet: 900m);
        await fixture.Context.SaveChangesAsync();

        var schedules = await fixture.SchedulesAsync();

        Assert.Equal(ScheduleVerdict.VarianceOpen, schedules.Workers.Single(worker => worker.WorkerName == "John Ahern").Verdict);
        Assert.All(schedules.Workers, worker => Assert.False(worker.CoveredBill!.IsApprovable));
    }

    [Fact]
    public async Task ACoveredDraftBillWhoseWorkersAllMatchIsApprovedAsCodedAndRecordedPerWorker()
    {
        var fixture = await CreateAsync();
        fixture.AddCoveredCompanyBill("jps-bill", "DRAFT");
        await fixture.Context.SaveChangesAsync();
        fixture.Xero.Bills["jps-bill"] = Bill("jps-bill", "DRAFT");

        var approval = await fixture.ApproveAsync("jps-bill");

        Assert.Equal(("jps-bill", "INV-1252", "AUTHORISED", false), (approval.XeroInvoiceId, approval.Label, approval.Status, approval.WasAlreadyApproved));
        Assert.Equal(new[] { "Dan Prowse", "Finley Taylor", "John Ahern" }, approval.WorkerNames);
        Assert.Equal(new[] { "ApproveInvoice:jps-bill" }, fixture.Xero.Calls);
        Assert.Empty(fixture.Xero.Approval!.Lines);
        Assert.All(fixture.Lines(), line => Assert.Equal("AUTHORISED", line.InvoiceStatus));
        var runs = fixture.Runs();
        Assert.Equal(new[] { "W-DAN", "W-FINLEY", "W-JOHN" }, runs.Select(run => run.WorkerId).OrderBy(id => id));
        Assert.All(runs, run => Assert.Equal(((int)XeroCodingOutcome.BillApproved, "jps-bill", "jeremy@jewelbb.co.uk"), (run.Outcome, run.XeroBillId, run.RunByEmail)));
        Assert.Equal("Approved in Xero by jeremy@jewelbb.co.uk: bill \"INV-1252\" DRAFT → AUTHORISED, £6,470.00, covering Dan Prowse, Finley Taylor, John Ahern — every worker read Matches.", runs[0].Detail);
        var schedules = await fixture.SchedulesAsync();
        Assert.All(schedules.Workers, worker => Assert.Equal(("AUTHORISED", false, "BillApproved"), (worker.CoveredBill!.Status, worker.CoveredBill.IsApprovable, worker.LastCodingOutcome)));
    }

    [Fact]
    public async Task ABillIsRefusedWhileAnyWorkerOnItDoesNotMatchAndXeroIsNotTouched()
    {
        var fixture = await CreateAsync();
        fixture.AddCoveredCompanyBill("jps-bill", "DRAFT", johnNet: 900m);
        await fixture.Context.SaveChangesAsync();
        fixture.Xero.Bills["jps-bill"] = Bill("jps-bill", "DRAFT", 6450m);

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.ApproveAsync("jps-bill"));

        Assert.Equal("Bill INV-1252 can't be approved yet — every worker on it must read Matches, and John Ahern reads VarianceOpen.", refusal.Message);
        Assert.Empty(fixture.Xero.Calls);
        Assert.Empty(fixture.Runs());
    }

    [Fact]
    public async Task ABillNoWorkerMonthIsCoveredByIsRefused()
    {
        var fixture = await CreateAsync();

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.ApproveAsync("nobodys-bill"));

        Assert.StartsWith("No worker-month in Aug 2026 is covered by bill nobodys-bill", refusal.Message);
        Assert.Empty(fixture.Xero.Calls);
    }

    [Fact]
    public async Task ABillXeroAlreadyHoldsApprovedIsAcknowledgedAndTheLedgerCatchesUp()
    {
        var fixture = await CreateAsync();
        fixture.AddCoveredCompanyBill("jps-bill", "DRAFT");
        await fixture.Context.SaveChangesAsync();
        fixture.Xero.Bills["jps-bill"] = Bill("jps-bill", "AUTHORISED");

        var approval = await fixture.ApproveAsync("jps-bill");

        Assert.Equal(("AUTHORISED", true), (approval.Status, approval.WasAlreadyApproved));
        Assert.All(fixture.Lines(), line => Assert.Equal("AUTHORISED", line.InvoiceStatus));
        Assert.Empty(fixture.Runs());
    }

    [Fact]
    public async Task AVoidedBillRefusesInXerosOwnWords()
    {
        var fixture = await CreateAsync();
        fixture.AddCoveredCompanyBill("jps-bill", "DRAFT");
        await fixture.Context.SaveChangesAsync();
        fixture.Xero.Bills["jps-bill"] = Bill("jps-bill", "VOIDED");

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.ApproveAsync("jps-bill"));

        Assert.Equal("The invoice is VOIDED in Xero and can't be approved.", refusal.Message);
        Assert.All(fixture.Lines(), line => Assert.Equal("DRAFT", line.InvoiceStatus));
    }

    [Fact]
    public async Task AnApprovedMonthIsAlreadyWrittenForTheCodingRun()
    {
        var fixture = await CreateAsync();
        fixture.AddCoveredCompanyBill("jps-bill", "AUTHORISED");
        await fixture.Context.SaveChangesAsync();
        var at = new DateTimeOffset(2026, 9, 8, 11, 5, 0, TimeSpan.Zero);
        foreach (var workerId in new[] { "W-DAN", "W-FINLEY", "W-JOHN" })
            await fixture.RecordAsync(workerId, XeroCodingOutcome.BillApproved, "jps-bill", at);
        fixture.Xero.Bills["jps-bill"] = Bill("jps-bill", "AUTHORISED");

        var results = await fixture.RunAsync();

        Assert.All(results, result => Assert.Equal(XeroCodingOutcome.Skipped, result.Outcome));
        Assert.All(results, result => Assert.StartsWith("Already coded (BillApproved, 08 Sep 11:05): bill \"INV-1252\" is AUTHORISED, £6,470.00.", result.Detail));
        Assert.Equal(new[] { "GetBill:jps-bill", "GetBill:jps-bill", "GetBill:jps-bill" }, fixture.Xero.Calls);
    }
}
