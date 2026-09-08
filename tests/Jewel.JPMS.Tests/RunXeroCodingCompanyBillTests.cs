using Jewel.JPMS.Models;
using Xunit;
using static Jewel.JPMS.Tests.JewelPropertyServeFixture;

namespace Jewel.JPMS.Tests;

/// <summary>
/// Item J (the accountant, 8 Sep 2026): one bill from a settlement counterparty covering several
/// workers is recoded ONCE, in place, to every worker's lines — status, total and VAT kept, cover
/// marked per worker so each schedule reconciles on its own — and never staged beside. INV-1252
/// is the acceptance case: nine tracked lines, £6,470, all three workers Matches.
/// </summary>
public sealed class RunXeroCodingCompanyBillTests
{
    [Fact]
    public async Task ACompanyBillCoveringThreeWorkersIsRecodedOnceToEveryWorkersLinesWithCoverPerWorker()
    {
        var fixture = await CreateAsync();
        fixture.AddLedgerBill("jps-bill", "INV-1252", new DateTime(2026, 8, 31), 6470m);
        await fixture.Context.SaveChangesAsync();
        fixture.Xero.Bills["jps-bill"] = Bill("jps-bill", "AUTHORISED");
        fixture.Xero.RecodedLineIds = NineLineIds;

        var results = await fixture.RunAsync();

        Assert.Equal(new[] { "Dan Prowse", "Finley Taylor", "John Ahern" }, results.Select(result => result.WorkerName));
        Assert.All(results, result => Assert.Equal((XeroCodingOutcome.BillRecoded, "jps-bill"), (result.Outcome, result.XeroBillId)));
        Assert.Equal(new[] { "GetBill:jps-bill", "RecodeBill:jps-bill" }, fixture.Xero.Calls);
        Assert.Equal(new[]
        {
            ("Dan Prowse — Abbot Road [PRELIMS-LAB] labour Aug 2026", "17a Abbot Road", 400m),
            ("Dan Prowse — By France [PRELIMS-LAB] labour Aug 2026", "By France", 3400m),
            ("Dan Prowse — Coombe Lane [PRELIMS-LAB] labour Aug 2026", "149a Coombe Lane West", 300m),
            ("Dan Prowse — Ravenswood Ave [PRELIMS-LAB] labour Aug 2026", "64 Ravenswood Avenue", 100m),
            ("Finley Taylor — Abbot Road [PRELIMS-LAB] labour Aug 2026", "17a Abbot Road", 750m),
            ("Finley Taylor — By France [PRELIMS-LAB] labour Aug 2026", "By France", 450m),
            ("Finley Taylor — Woodhouse [PRELIMS-LAB] labour Aug 2026", "Woodhouse Lane", 150m),
            ("John Ahern — Abbot Road [PRELIMS-LAB] labour Aug 2026", "17a Abbot Road", 690m),
            ("John Ahern — Ravenswood Ave [PRELIMS-LAB] labour Aug 2026", "64 Ravenswood Avenue", 230m),
        }, fixture.Xero.Recode!.Lines.Select(line => (line.Description, line.SiteOption, line.Net)));
        Assert.All(fixture.Xero.Recode.Lines, line => Assert.Equal(("321", "PRELIMS-LAB"), (line.AccountCode, line.CostCodeOption)));
        Assert.Equal(6470m, fixture.Xero.Recode.Lines.Sum(line => line.Net));

        var covers = fixture.Covers();
        Assert.Equal(new[] { ("W-DAN", 4), ("W-FINLEY", 3), ("W-JOHN", 2) },
            covers.GroupBy(cover => cover.WorkerId).Select(group => (WorkerId: group.Key!, Lines: group.Count())).OrderBy(pair => pair.WorkerId));
        Assert.All(covers, cover => Assert.Equal((Company, "", "accounts@jewelbb.co.uk"), (cover.SubcontractorId, cover.ProjectId, cover.CreatedByEmail)));
        Assert.Equal(9, fixture.Lines().Count);
        Assert.DoesNotContain(fixture.Lines(), line => line.XeroLedgerLineId == "jps-bill:old");

        var schedules = await fixture.SchedulesAsync();
        Assert.All(schedules.Workers, worker => Assert.Equal((ScheduleVerdict.Matches, worker.GrossTotal), (worker.Verdict, worker.CoveredBillTotal)));
        Assert.Equal(0, schedules.WorkersToReconcile);

        var dan = results.Single(result => result.WorkerName == "Dan Prowse");
        Assert.Equal("Recoded Jewel Property Serve Ltd's bill \"INV-1252\" to 9 line(s) for 3 workers (Dan Prowse, Finley Taylor, John Ahern); "
            + "left AUTHORISED in Xero. Total £6,470.00 and VAT £0.00 unchanged. Cover marked per worker — 4 line(s) are Dan Prowse's. "
            + "Bill \"INV-1252\": AUTHORISED, net £6,470.00, VAT £0.00 (REVERSECHARGES, Exclusive), total £6,470.00. "
            + "Combined schedule (3 workers) £6,470.00 — matches.", dan.Detail);
        Assert.Equal(3, fixture.Runs().Count);
        Assert.All(fixture.Runs(), run => Assert.Equal(((int)XeroCodingOutcome.BillRecoded, "jps-bill"), (run.Outcome, run.XeroBillId)));
    }

    [Fact]
    public async Task ADryRunOfACompanyBillTellsEveryWorkerWhoTheyShareItWithAndWritesNothing()
    {
        var fixture = await CreateAsync();
        fixture.AddLedgerBill("jps-bill", "INV-1252", new DateTime(2026, 8, 31), 6470m);
        await fixture.Context.SaveChangesAsync();
        fixture.Xero.Bills["jps-bill"] = Bill("jps-bill", "AUTHORISED");

        var results = await fixture.RunAsync(dryRun: true);

        Assert.All(results, result => Assert.Equal((XeroCodingOutcome.WouldRecodeBill, "jps-bill"), (result.Outcome, result.XeroBillId)));
        var finley = results.Single(result => result.WorkerName == "Finley Taylor");
        Assert.StartsWith("Would recode Jewel Property Serve Ltd's bill recognised by contact + period — shared with Dan Prowse, John Ahern — "
            + "to 9 line(s) (3 of them Finley Taylor's), keeping its status, total and VAT and marking cover per worker. "
            + "Bill \"INV-1252\": AUTHORISED, net £6,470.00", finley.Detail);
        Assert.EndsWith("Finley Taylor's lines: 17a Abbot Road / PRELIMS-LAB → 321 £750.00; By France / PRELIMS-LAB → 321 £450.00; "
            + "Woodhouse Lane / PRELIMS-LAB → 321 £150.00.", finley.Detail);
        Assert.Equal(new[] { "GetBill:jps-bill" }, fixture.Xero.Calls);
        Assert.Empty(fixture.Covers());
        Assert.Empty(fixture.Runs());
    }

    [Fact]
    public async Task OneUnsignedWorkerHoldsTheWholeCompanyBillAndEveryoneIsToldWhy()
    {
        var fixture = await CreateAsync(isFinleySignedOff: false);

        var results = await fixture.RunAsync();

        Assert.All(results, result => Assert.Equal(XeroCodingOutcome.Skipped, result.Outcome));
        Assert.Equal("Not every week with approved time is signed off — sign the month off first.",
            results.Single(result => result.WorkerName == "Finley Taylor").Detail);
        Assert.Equal("Waiting: Jewel Property Serve Ltd's bill for Aug 2026 covers Finley Taylor too, and their month is not ready — "
            + "Finley Taylor: Not every week with approved time is signed off — sign the month off first. "
            + "A company bill is coded once every worker on it is ready.",
            results.Single(result => result.WorkerName == "Dan Prowse").Detail);
        Assert.Empty(fixture.Xero.Calls);
        Assert.Equal(3, fixture.Runs().Count);
    }

    [Fact]
    public async Task WithNoCompanyBillOneDraftCarryingEveryWorkersLinesIsStaged()
    {
        var fixture = await CreateAsync();
        fixture.Xero.StagedBillId = "jps-draft";

        var results = await fixture.RunAsync();

        Assert.All(results, result => Assert.Equal((XeroCodingOutcome.DraftStaged, "jps-draft"), (result.Outcome, result.XeroBillId)));
        Assert.Equal(new[] { "CreateDraftBill" }, fixture.Xero.Calls);
        var draft = fixture.Xero.Draft!;
        Assert.Equal((CompanyName, "JPMS labour Aug 2026 — Jewel Property Serve Ltd", 9, 6470m),
            (draft.ContactName, draft.Reference, draft.Lines.Count, draft.Lines.Sum(line => line.Net)));
        Assert.Equal("Draft bill \"JPMS labour Aug 2026 — Jewel Property Serve Ltd\" staged for Jewel Property Serve Ltd with 9 line(s), "
            + "net £6,470.00 for 3 workers (Dan Prowse, Finley Taylor, John Ahern). Tax from the contact. Reconcile when the real invoice lands.",
            results.Single(result => result.WorkerName == "John Ahern").Detail);
    }

    [Fact]
    public async Task AskingForOneWorkerCodesTheCompanyBillTheyShareAndReportsEveryWorkerOnIt()
    {
        var fixture = await CreateAsync();
        fixture.AddLedgerBill("jps-bill", "INV-1252", new DateTime(2026, 8, 31), 6470m);
        await fixture.Context.SaveChangesAsync();
        fixture.Xero.Bills["jps-bill"] = Bill("jps-bill", "AUTHORISED");
        fixture.Xero.RecodedLineIds = NineLineIds;

        var results = await fixture.RunAsync(workerIds: new[] { "W-JOHN" });

        Assert.Equal(3, results.Count);
        Assert.Equal(new[] { "GetBill:jps-bill", "RecodeBill:jps-bill" }, fixture.Xero.Calls);
        Assert.Equal(9, fixture.Xero.Recode!.Lines.Count);
    }

    [Fact]
    public async Task AWorkerAlreadyCodedToTheCompanyBillIsWrittenAgainWhenAnotherWorkerOnItIsOpen()
    {
        var fixture = await CreateAsync();
        fixture.AddLedgerBill("jps-bill", "INV-1252", new DateTime(2026, 8, 31), 6470m);
        await fixture.Context.SaveChangesAsync();
        await fixture.RecordAsync("W-DAN", XeroCodingOutcome.BillRecoded, "jps-bill", new DateTimeOffset(2026, 9, 3, 10, 15, 0, TimeSpan.Zero));
        fixture.Xero.Bills["jps-bill"] = Bill("jps-bill", "AUTHORISED");
        fixture.Xero.RecodedLineIds = NineLineIds;

        var results = await fixture.RunAsync();

        Assert.All(results, result => Assert.Equal(XeroCodingOutcome.BillRecoded, result.Outcome));
        Assert.StartsWith("Already coded (BillRecoded, 03 Sep 10:15) — written again because Jewel Property Serve Ltd's bill is coded whole "
            + "and Finley Taylor, John Ahern's Aug 2026 is open. Recoded Jewel Property Serve Ltd's bill \"INV-1252\"",
            results.Single(result => result.WorkerName == "Dan Prowse").Detail);
        Assert.StartsWith("Recoded Jewel Property Serve Ltd's bill", results.Single(result => result.WorkerName == "John Ahern").Detail);
        Assert.Equal(new[] { "GetBill:jps-bill", "GetBill:jps-bill", "RecodeBill:jps-bill" }, fixture.Xero.Calls);
    }

    [Fact]
    public async Task WhenEveryWorkerIsAlreadyCodedToAStandingBillTheCompanyBillIsLeftAlone()
    {
        var fixture = await CreateAsync();
        var at = new DateTimeOffset(2026, 9, 3, 10, 15, 0, TimeSpan.Zero);
        await fixture.RecordAsync("W-DAN", XeroCodingOutcome.BillRecoded, "jps-bill", at);
        await fixture.RecordAsync("W-FINLEY", XeroCodingOutcome.BillRecoded, "jps-bill", at);
        await fixture.RecordAsync("W-JOHN", XeroCodingOutcome.BillRecoded, "jps-bill", at);
        fixture.Xero.Bills["jps-bill"] = Bill("jps-bill", "AUTHORISED");

        var results = await fixture.RunAsync();

        Assert.All(results, result => Assert.Equal(XeroCodingOutcome.Skipped, result.Outcome));
        Assert.All(results, result => Assert.StartsWith("Already coded (BillRecoded, 03 Sep 10:15): bill \"INV-1252\" is AUTHORISED, £6,470.00.", result.Detail));
        Assert.Equal(new[] { "GetBill:jps-bill", "GetBill:jps-bill", "GetBill:jps-bill" }, fixture.Xero.Calls);
    }

    [Fact]
    public async Task AWorkerCodedToAnotherStandingBillStopsTheCompanyBillUntilAPersonDecides()
    {
        var fixture = await CreateAsync();
        fixture.AddLedgerBill("jps-bill", "INV-1252", new DateTime(2026, 8, 31), 6470m);
        await fixture.Context.SaveChangesAsync();
        await fixture.RecordAsync("W-DAN", XeroCodingOutcome.BillRecoded, "other-bill", new DateTimeOffset(2026, 9, 3, 10, 15, 0, TimeSpan.Zero));
        fixture.Xero.Bills["other-bill"] = Bill("other-bill", "AUTHORISED", 4200m, number: "INV-1240");
        fixture.Xero.Bills["jps-bill"] = Bill("jps-bill", "AUTHORISED");

        var results = await fixture.RunAsync();

        Assert.All(results, result => Assert.Equal(XeroCodingOutcome.Skipped, result.Outcome));
        Assert.All(results, result => Assert.Equal("Dan Prowse's Aug 2026 is already coded to bill other-bill, but Jewel Property Serve Ltd's bill "
            + "for the month resolves to jps-bill — reset their coding outcome, or mark the right bill as settlement on the Cost allocation "
            + "page's Labour tab, then re-run.", result.Detail));
        Assert.Equal(new[] { "GetBill:other-bill" }, fixture.Xero.Calls);
    }
}
