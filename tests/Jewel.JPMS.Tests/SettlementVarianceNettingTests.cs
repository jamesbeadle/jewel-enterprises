using Jewel.JPMS.Models;
using Xunit;
using static Jewel.JPMS.Tests.JewelPropertyServeFixture;

namespace Jewel.JPMS.Tests;

/// <summary>
/// The accountant's "the settlement view does not net a posted variance" (8 Sep 2026): a
/// variance posted against a covered line is the accepted part of that worker's difference,
/// so the month reads Matches once covered + variance = schedule, and the bill becomes
/// approvable. A variance that names no line has no month to land in.
/// </summary>
public sealed class SettlementVarianceNettingTests
{
    [Fact]
    public async Task AVariancePostedAgainstAWorkersCoveredLineNetsTheirDifferenceToMatches()
    {
        var fixture = await CreateAsync();
        fixture.AddCoveredCompanyBill("jps-bill", "DRAFT", johnNet: 900m);
        fixture.AddVariance(-20m, "jps-bill:W-JOHN");
        await fixture.Context.SaveChangesAsync();

        var schedules = await fixture.SchedulesAsync();

        var john = schedules.Workers.Single(worker => worker.WorkerName == "John Ahern");
        Assert.Equal((900m, -20m, 0m, ScheduleVerdict.Matches), (john.CoveredBillTotal, john.PostedVariance, john.Difference, john.Verdict));
        Assert.All(schedules.Workers, worker => Assert.Equal((ScheduleVerdict.Matches, true), (worker.Verdict, worker.CoveredBill!.IsApprovable)));
        Assert.Equal(new[] { "jps-bill:W-JOHN" }, john.CoveredBill!.LineIds);
        Assert.Equal(0, schedules.WorkersToReconcile);
    }

    [Fact]
    public async Task AVarianceOnOneWorkersLineDoesNotTouchTheOthers()
    {
        var fixture = await CreateAsync();
        fixture.AddCoveredCompanyBill("jps-bill", "DRAFT", danNet: 4100m, johnNet: 900m);
        fixture.AddVariance(-20m, "jps-bill:W-JOHN");
        await fixture.Context.SaveChangesAsync();

        var schedules = await fixture.SchedulesAsync();

        var dan = schedules.Workers.Single(worker => worker.WorkerName == "Dan Prowse");
        Assert.Equal((0m, -100m, ScheduleVerdict.VarianceOpen), (dan.PostedVariance, dan.Difference, dan.Verdict));
        Assert.Equal(ScheduleVerdict.Matches, schedules.Workers.Single(worker => worker.WorkerName == "John Ahern").Verdict);
        Assert.All(schedules.Workers, worker => Assert.False(worker.CoveredBill!.IsApprovable));
    }

    [Fact]
    public async Task AVarianceThatNamesNoLineHasNoMonthToNetInto()
    {
        var fixture = await CreateAsync();
        fixture.AddCoveredCompanyBill("jps-bill", "DRAFT", johnNet: 900m);
        fixture.AddVariance(-20m, ledgerLineId: null);
        await fixture.Context.SaveChangesAsync();

        var schedules = await fixture.SchedulesAsync();

        var john = schedules.Workers.Single(worker => worker.WorkerName == "John Ahern");
        Assert.Equal((0m, -20m, ScheduleVerdict.VarianceOpen), (john.PostedVariance, john.Difference, john.Verdict));
    }

    [Fact]
    public async Task ABillSettledWithAVarianceCanBeApproved()
    {
        var fixture = await CreateAsync();
        fixture.AddCoveredCompanyBill("jps-bill", "DRAFT", johnNet: 900m);
        fixture.AddVariance(-20m, "jps-bill:W-JOHN");
        await fixture.Context.SaveChangesAsync();
        fixture.Xero.Bills["jps-bill"] = Bill("jps-bill", "DRAFT", 6450m);

        var approval = await fixture.ApproveAsync("jps-bill");

        Assert.Equal(("AUTHORISED", false), (approval.Status, approval.WasAlreadyApproved));
        Assert.Equal(new[] { "ApproveInvoice:jps-bill" }, fixture.Xero.Calls);
    }
}
