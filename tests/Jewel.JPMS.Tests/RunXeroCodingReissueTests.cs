using Jewel.JPMS.Models;
using Xunit;
using static Jewel.JPMS.Tests.JewelPropertyServeFixture;

namespace Jewel.JPMS.Tests;

/// <summary>
/// Item K (the accountant, 8 Sep 2026): a bill the run matched that has since been voided must
/// not stop the month. INV-1252 was voided (f9ddeeeb) and keyed again (05a9fcba) before the
/// ledger had caught up; the live bill under the same contact, number and period is the one.
/// </summary>
public sealed class RunXeroCodingReissueTests
{
    private static readonly DateTime EndOfAugust = new(2026, 8, 31);

    [Fact]
    public async Task AVoidedBillTheLedgerStillHoldsIsFollowedToItsLiveReissueInXero()
    {
        var fixture = await CreateAsync();
        fixture.AddLedgerBill("old-bill", "INV-1252", EndOfAugust, 6270m);
        await fixture.Context.SaveChangesAsync();
        fixture.Xero.Bills["old-bill"] = Bill("old-bill", "VOIDED", 6270m);
        fixture.Xero.Bills["new-bill"] = Bill("new-bill", "AUTHORISED");
        fixture.Xero.BillsByNumber["INV-1252"] = new() { Bill("new-bill", "AUTHORISED"), Bill("old-bill", "VOIDED", 6270m) };
        fixture.Xero.RecodedLineIds = NineLineIds;

        var results = await fixture.RunAsync();

        Assert.All(results, result => Assert.Equal((XeroCodingOutcome.BillRecoded, "new-bill"), (result.Outcome, result.XeroBillId)));
        Assert.Equal(new[] { "GetBill:old-bill", "FindBills:INV-1252", "RecodeBill:new-bill" }, fixture.Xero.Calls);
        Assert.StartsWith("Bill \"INV-1252\" (old-bill) is voided; its live re-issue \"INV-1252\" (new-bill, AUTHORISED) is the bill. "
            + "Recoded Jewel Property Serve Ltd's bill \"INV-1252\" to 9 line(s)", results[0].Detail);
        Assert.Equal(9, fixture.Lines().Count);
        Assert.All(fixture.Lines(), line => Assert.Equal("new-bill", line.XeroInvoiceId));
        Assert.Equal(9, fixture.Covers().Count);
        Assert.All(fixture.Covers(), cover => Assert.StartsWith("new-bill:", cover.XeroLedgerLineId));
        var schedules = await fixture.SchedulesAsync();
        Assert.All(schedules.Workers, worker => Assert.Equal(ScheduleVerdict.Matches, worker.Verdict));
    }

    [Fact]
    public async Task ADryRunFollowsTheReissueTooAndStillWritesNothing()
    {
        var fixture = await CreateAsync();
        fixture.AddLedgerBill("old-bill", "INV-1252", EndOfAugust, 6270m);
        await fixture.Context.SaveChangesAsync();
        fixture.Xero.Bills["old-bill"] = Bill("old-bill", "VOIDED", 6270m);
        fixture.Xero.BillsByNumber["INV-1252"] = new() { Bill("new-bill", "DRAFT") };

        var results = await fixture.RunAsync(dryRun: true);

        Assert.All(results, result => Assert.Equal((XeroCodingOutcome.WouldRecodeBill, "new-bill"), (result.Outcome, result.XeroBillId)));
        Assert.Contains("its live re-issue \"INV-1252\" (new-bill, DRAFT) is the bill. Would recode", results[0].Detail);
        Assert.Equal(new[] { "GetBill:old-bill", "FindBills:INV-1252" }, fixture.Xero.Calls);
        Assert.Single(fixture.Lines());
        Assert.Empty(fixture.Runs());
    }

    [Fact]
    public async Task ACoveredBillThatWasVoidedMovesItsCoverOntoTheReissuesLinesPerWorker()
    {
        var fixture = await CreateAsync();
        var oldLine = fixture.AddLedgerBill("old-bill", "INV-1252", EndOfAugust, 6270m);
        fixture.AddCover(oldLine.XeroLedgerLineId);
        await fixture.Context.SaveChangesAsync();
        fixture.Xero.Bills["old-bill"] = Bill("old-bill", "VOIDED", 6270m);
        fixture.Xero.Bills["new-bill"] = Bill("new-bill", "AUTHORISED");
        fixture.Xero.BillsByNumber["INV-1252"] = new() { Bill("new-bill", "AUTHORISED") };
        fixture.Xero.RecodedLineIds = NineLineIds;

        var results = await fixture.RunAsync();

        Assert.All(results, result => Assert.Equal((XeroCodingOutcome.BillRecoded, "new-bill"), (result.Outcome, result.XeroBillId)));
        var covers = fixture.Covers();
        Assert.Equal(9, covers.Count);
        Assert.DoesNotContain(covers, cover => cover.XeroLedgerLineId == "old-bill:old");
        Assert.All(covers, cover => Assert.Equal("jeremy@jewelbb.co.uk", cover.CreatedByEmail));
        Assert.Equal(new[] { ("W-DAN", 4), ("W-FINLEY", 3), ("W-JOHN", 2) },
            covers.GroupBy(cover => cover.WorkerId).Select(group => (WorkerId: group.Key!, Lines: group.Count())).OrderBy(pair => pair.WorkerId));
        Assert.DoesNotContain(fixture.Lines(), line => line.XeroInvoiceId == "old-bill");
    }

    [Fact]
    public async Task AVoidedBillWithNoLiveReissueLeavesTheMonthWhereItWasAndStagesNothing()
    {
        var fixture = await CreateAsync();
        fixture.AddLedgerBill("old-bill", "INV-1252", EndOfAugust, 6270m);
        await fixture.Context.SaveChangesAsync();
        fixture.Xero.Bills["old-bill"] = Bill("old-bill", "VOIDED", 6270m);
        fixture.Xero.BillsByNumber["INV-1252"] = new() { Bill("old-bill", "VOIDED", 6270m) };

        var results = await fixture.RunAsync();

        Assert.All(results, result => Assert.Equal((XeroCodingOutcome.Skipped, "old-bill"), (result.Outcome, result.XeroBillId)));
        Assert.StartsWith("Bill \"INV-1252\" for Aug 2026 can't be recoded — it is VOIDED, and no live bill under the same number has replaced it. "
            + "Nothing was written and no second bill was staged", results[0].Detail);
        Assert.Equal(new[] { "GetBill:old-bill", "FindBills:INV-1252" }, fixture.Xero.Calls);
        Assert.Single(fixture.Lines());
    }

    [Fact]
    public async Task AReissueFromAnotherContactOrAnotherMonthIsNotTheBill()
    {
        var fixture = await CreateAsync();
        fixture.AddLedgerBill("old-bill", "INV-1252", EndOfAugust, 6270m);
        await fixture.Context.SaveChangesAsync();
        fixture.Xero.Bills["old-bill"] = Bill("old-bill", "VOIDED", 6270m);
        fixture.Xero.BillsByNumber["INV-1252"] = new()
        {
            Bill("someone-elses", "AUTHORISED") with { ContactName = "Grant & Stone Limited" },
            Bill("next-month", "AUTHORISED", date: new DateTime(2026, 10, 2)),
        };

        var results = await fixture.RunAsync();

        Assert.All(results, result => Assert.Equal(XeroCodingOutcome.Skipped, result.Outcome));
        Assert.Equal(new[] { "GetBill:old-bill", "FindBills:INV-1252" }, fixture.Xero.Calls);
    }

    [Fact]
    public async Task TwoLiveReissuesUnderTheSameNumberAreAQuestionForAPerson()
    {
        var fixture = await CreateAsync();
        fixture.AddLedgerBill("old-bill", "INV-1252", EndOfAugust, 6270m);
        await fixture.Context.SaveChangesAsync();
        fixture.Xero.Bills["old-bill"] = Bill("old-bill", "VOIDED", 6270m);
        fixture.Xero.BillsByNumber["INV-1252"] = new() { Bill("new-bill", "AUTHORISED"), Bill("newer-bill", "DRAFT") };

        var results = await fixture.RunAsync();

        Assert.All(results, result => Assert.Equal(XeroCodingOutcome.Skipped, result.Outcome));
        Assert.Equal("2 live bills from Jewel Property Serve Ltd carry the number \"INV-1252\": new-bill (AUTHORISED, £6,470.00), "
            + "newer-bill (DRAFT, £6,470.00). Mark the right one as settlement on the Cost allocation page's Labour tab, then re-run.",
            results[0].Detail);
    }
}
