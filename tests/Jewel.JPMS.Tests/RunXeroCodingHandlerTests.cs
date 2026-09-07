using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Labour;
using Jewel.JPMS.Api.Features.Labour.Commands;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// Characterisation of the §6a coding run — the one path that writes a worker's month into Xero
/// — pinned before <c>RunXeroCodingSlice</c> is divided: every gate in order (sign-off, run-once,
/// mapping, settlement identity, one bill), what a staged draft and a recode each send, how the
/// ledger and the timesheet cover are re-pointed after a recode, what a dry run leaves alone,
/// and what the reset appends. A recording fake stands in for Xero; the schedule builder and the
/// run's own bookkeeping are the real classes over an in-memory database.
/// </summary>
public sealed class RunXeroCodingHandlerTests
{
    private static readonly DateTimeOffset August = new(new DateTime(2026, 8, 1), TimeSpan.Zero);
    private static readonly DateTimeOffset MondayAugust17 = new(new DateTime(2026, 8, 17), TimeSpan.Zero);

    // ---- The gates, in order -------------------------------------------------------------------

    [Fact]
    public async Task AnUnsignedMonthIsSkippedAndRecordedWithoutTouchingXero()
    {
        var fixture = await Fixture.SignedOff(false);

        var results = await fixture.RunAsync();

        var adam = Assert.Single(results);
        Assert.Equal(XeroCodingOutcome.Skipped, adam.Outcome);
        Assert.Equal("Not every week with approved time is signed off — sign the month off first.", adam.Detail);
        Assert.Empty(fixture.Xero.Calls);
        var run = Assert.Single(fixture.Runs());
        Assert.Equal((int)XeroCodingOutcome.Skipped, run.Outcome);
        Assert.Equal("accounts@jewelbb.co.uk", run.RunByEmail);
    }

    [Fact]
    public async Task AMappingGapSkipsWithEveryGapNamedAndNeverGuesses()
    {
        var fixture = await Fixture.SignedOff(true, mapBrickwork: false);

        var adam = Assert.Single(await fixture.RunAsync());

        Assert.Equal(XeroCodingOutcome.Skipped, adam.Outcome);
        Assert.Equal("Mapping gaps: cost code SUB-BRK has no Xero mapping. Fix the Xero mapping and re-run.", adam.Detail);
        Assert.Empty(fixture.Xero.Calls);
    }

    [Fact]
    public async Task AWorkerWithNoSettlementIdentityIsSkipped()
    {
        var fixture = await Fixture.SignedOff(true, soleTrader: false);

        var adam = Assert.Single(await fixture.RunAsync());

        Assert.Equal(XeroCodingOutcome.Skipped, adam.Outcome);
        Assert.StartsWith("The worker has no settlement identity — link a subcontractor company or flag them a sole trader", adam.Detail);
    }

    [Fact]
    public async Task AMonthAlreadyCodedToABillThatStillStandsIsSkipped()
    {
        var fixture = await Fixture.SignedOff(true);
        await fixture.RecordAsync(XeroCodingOutcome.BillRecoded, "bill-1", new DateTimeOffset(2026, 9, 3, 10, 15, 0, TimeSpan.Zero));
        fixture.Xero.Bills["bill-1"] = Bill("AUTHORISED", 1600m);

        var adam = Assert.Single(await fixture.RunAsync());

        Assert.Equal(XeroCodingOutcome.Skipped, adam.Outcome);
        Assert.Equal("Already coded (BillRecoded, 03 Sep 10:15): bill \"Aug 2026\" is AUTHORISED, £1,600.00. Reset the coding outcome to run this month again.", adam.Detail);
        Assert.Equal(new[] { "GetBill:bill-1" }, fixture.Xero.Calls);
    }

    [Fact]
    public async Task AMonthWhoseStagedBillWasDeletedInXeroIsCodedAgainWithAPreface()
    {
        var fixture = await Fixture.SignedOff(true);
        await fixture.RecordAsync(XeroCodingOutcome.DraftStaged, "bill-gone", new DateTimeOffset(2026, 9, 3, 10, 15, 0, TimeSpan.Zero));
        fixture.Xero.StagedBillId = "bill-2";

        var adam = Assert.Single(await fixture.RunAsync());

        Assert.Equal(XeroCodingOutcome.DraftStaged, adam.Outcome);
        Assert.StartsWith("The bill this month was coded to on 03 Sep 10:15 (DraftStaged) is no longer in Xero — coding again. Draft bill", adam.Detail);
        Assert.Equal(new[] { "GetBill:bill-gone", "CreateDraftBill" }, fixture.Xero.Calls);
    }

    // ---- Staging a draft ------------------------------------------------------------------------

    [Fact]
    public async Task WithNoBillAnywhereADraftMatchingTheScheduleIsStagedAndRecorded()
    {
        var fixture = await Fixture.SignedOff(true);
        fixture.Xero.StagedBillId = "bill-2";

        var adam = Assert.Single(await fixture.RunAsync());

        Assert.Equal(XeroCodingOutcome.DraftStaged, adam.Outcome);
        Assert.Equal("bill-2", adam.XeroBillId);
        Assert.Equal("Draft bill \"JPMS labour Aug 2026 — Adam Midgley\" staged for Adam Midgley with 2 line(s), net £1,600.00. Tax from the contact. Reconcile when the real invoice lands.", adam.Detail);
        var draft = fixture.Xero.Draft!;
        Assert.Equal("Adam Midgley", draft.ContactName);
        Assert.Equal((new DateTime(2026, 8, 31), new DateTime(2026, 9, 30)), (draft.Date, draft.DueDate));
        Assert.Equal(new[]
        {
            new XeroScheduleLine("Adam Midgley — Woodhouse [SUB-BRK] labour Aug 2026", 600m, "321", "Woodhouse", "SUB-BRK"),
            new XeroScheduleLine("Adam Midgley — Woodhouse [SUB-GWK] labour Aug 2026", 1000m, "321", "Woodhouse", "SUB-GWK"),
        }, draft.Lines);
        var run = Assert.Single(fixture.Runs());
        Assert.Equal(((int)XeroCodingOutcome.DraftStaged, "bill-2"), (run.Outcome, run.XeroBillId));
    }

    [Fact]
    public async Task ADraftGoesToTheContactXeroAlreadyHoldsForTheWorker()
    {
        var fixture = await Fixture.SignedOff(true);
        fixture.Context.XeroLedgerLines.Add(LedgerLine("old-bill:1", "old-bill", "Adam Midgley Ltd", "Jul 2026", new DateTime(2026, 7, 28), 900m));
        await fixture.Context.SaveChangesAsync();
        fixture.Xero.StagedBillId = "bill-2";

        await fixture.RunAsync();

        Assert.Equal("Adam Midgley Ltd", fixture.Xero.Draft!.ContactName);
    }

    [Fact]
    public async Task ADryRunPreviewsTheDraftAndWritesAndRecordsNothing()
    {
        var fixture = await Fixture.SignedOff(true);

        var adam = Assert.Single(await fixture.RunAsync(dryRun: true));

        Assert.Equal(XeroCodingOutcome.WouldStageDraft, adam.Outcome);
        Assert.StartsWith("No bill from Adam Midgley for Aug 2026 in Xero — would stage a DRAFT bill \"JPMS labour Aug 2026 — Adam Midgley\" dated 31 Aug 2026: 2 line(s), net £1,600.00", adam.Detail);
        Assert.Empty(fixture.Xero.Calls);
        Assert.Empty(fixture.Runs());
    }

    // ---- Recoding the worker's own bill -------------------------------------------------------

    [Fact]
    public async Task ABillRecognisedByContactAndPeriodIsRecodedAndMarkedAsSettlement()
    {
        var fixture = await Fixture.SignedOff(true);
        fixture.Context.XeroLedgerLines.Add(LedgerLine("bill-1:old", "bill-1", "Adam Midgley", "Aug 2026", new DateTime(2026, 8, 25), 1600m));
        await fixture.Context.SaveChangesAsync();
        fixture.Xero.Bills["bill-1"] = Bill("AUTHORISED", 1600m);
        fixture.Xero.RecodedLineIds = new[] { "new-1", "new-2" };

        var adam = Assert.Single(await fixture.RunAsync());

        Assert.Equal(XeroCodingOutcome.BillRecoded, adam.Outcome);
        Assert.Equal("bill-1", adam.XeroBillId);
        Assert.Equal(new[] { "GetBill:bill-1", "RecodeBill:bill-1" }, fixture.Xero.Calls);
        Assert.Equal(2, fixture.Xero.Recode!.Lines.Count);
        Assert.Equal("Recoded bill \"Aug 2026\" to 2 line(s); left AUTHORISED in Xero. Total £1,600.00 and VAT £0.00 unchanged. Marked as settlement of Aug 2026 (2 line(s)). Bill \"Aug 2026\": AUTHORISED, net £1,600.00, VAT £0.00 (NONE, Inclusive), total £1,600.00. Schedule £1,600.00 — matches.", adam.Detail);

        var lines = fixture.Context.XeroLedgerLines.OrderBy(line => line.XeroLedgerLineId).ToList();
        Assert.Equal(new[] { "bill-1:new-1", "bill-1:new-2" }, lines.Select(line => line.XeroLedgerLineId));
        Assert.All(lines, line => Assert.Equal(("bill-1", "ACCPAY", "AUTHORISED", "Adam Midgley"), (line.XeroInvoiceId, line.Type, line.InvoiceStatus, line.ContactName)));
        Assert.Equal(new[] { 600m, 1000m }, lines.Select(line => line.Net));
        var covers = fixture.Context.XeroLineTimesheetCovers.OrderBy(cover => cover.XeroLedgerLineId).ToList();
        Assert.Equal(new[] { "bill-1:new-1", "bill-1:new-2" }, covers.Select(cover => cover.XeroLedgerLineId));
        Assert.All(covers, cover => Assert.Equal(("", "W-ADAM", August, August.AddMonths(1), "accounts@jewelbb.co.uk"),
            (cover.ProjectId, cover.SubcontractorId, cover.PeriodStart, cover.PeriodEnd, cover.CreatedByEmail)));
    }

    [Fact]
    public async Task ACoveredBillIsRecodedFirstAndItsCoverMovedOntoTheNewLines()
    {
        var fixture = await Fixture.SignedOff(true);
        fixture.Context.XeroLedgerLines.Add(LedgerLine("bill-1:old", "bill-1", "A N Other Ltd", "INV-77", new DateTime(2026, 8, 25), 1500m));
        fixture.Context.XeroLineTimesheetCovers.Add(new XeroLineTimesheetCoverEntity
        {
            XeroLineTimesheetCoverId = "C1", XeroLedgerLineId = "bill-1:old", ProjectId = "P1", SubcontractorId = "W-ADAM",
            PeriodStart = August, PeriodEnd = August.AddMonths(1), CreatedByEmail = "nigel@jewelbb.co.uk", CreatedAt = August,
        });
        await fixture.Context.SaveChangesAsync();
        fixture.Xero.Bills["bill-1"] = Bill("DRAFT", 1500m, invoiceNumber: "INV-77");
        fixture.Xero.RecodedLineIds = new[] { "new-1", "new-2" };

        var adam = Assert.Single(await fixture.RunAsync());

        Assert.Equal(XeroCodingOutcome.BillRecoded, adam.Outcome);
        Assert.Contains("Cover moved onto 2 line(s).", adam.Detail);
        Assert.Contains("Schedule £1,600.00 — differs by £-100.00", adam.Detail);
        var covers = fixture.Context.XeroLineTimesheetCovers.ToList();
        Assert.Equal(2, covers.Count);
        Assert.All(covers, cover => Assert.Equal(("P1", "nigel@jewelbb.co.uk"), (cover.ProjectId, cover.CreatedByEmail)));
        Assert.DoesNotContain(covers, cover => cover.XeroLedgerLineId == "bill-1:old");
    }

    [Fact]
    public async Task ADryRunOfARecodeReadsTheBillButWritesNothing()
    {
        var fixture = await Fixture.SignedOff(true);
        fixture.Context.XeroLedgerLines.Add(LedgerLine("bill-1:old", "bill-1", "Adam Midgley", "Aug 2026", new DateTime(2026, 8, 25), 1600m));
        await fixture.Context.SaveChangesAsync();
        fixture.Xero.Bills["bill-1"] = Bill("AUTHORISED", 1600m);

        var adam = Assert.Single(await fixture.RunAsync(dryRun: true));

        Assert.Equal(XeroCodingOutcome.WouldRecodeBill, adam.Outcome);
        Assert.StartsWith("Would recode the bill recognised by contact + period to 2 line(s), keeping its status, total and VAT and marking it as settlement of the month.", adam.Detail);
        Assert.Equal(new[] { "GetBill:bill-1" }, fixture.Xero.Calls);
        Assert.Single(fixture.Context.XeroLedgerLines);
        Assert.Empty(fixture.Runs());
    }

    [Fact]
    public async Task APaidBillIsSkippedWithItsStatusAndNoSecondBillIsStaged()
    {
        var fixture = await Fixture.SignedOff(true);
        fixture.Context.XeroLedgerLines.Add(LedgerLine("bill-1:old", "bill-1", "Adam Midgley", "Aug 2026", new DateTime(2026, 8, 25), 1600m));
        await fixture.Context.SaveChangesAsync();
        fixture.Xero.Bills["bill-1"] = Bill("PAID", 1600m);

        var adam = Assert.Single(await fixture.RunAsync());

        Assert.Equal(XeroCodingOutcome.Skipped, adam.Outcome);
        Assert.StartsWith("Bill \"Aug 2026\" for Aug 2026 can't be recoded — it is PAID. Nothing was written and no second bill was staged", adam.Detail);
        Assert.Equal(new[] { "GetBill:bill-1" }, fixture.Xero.Calls);
    }

    [Fact]
    public async Task ABillWithHandAllocatedLinesIsSkippedBeforeXeroIsRead()
    {
        var fixture = await Fixture.SignedOff(true);
        var allocated = LedgerLine("bill-1:old", "bill-1", "Adam Midgley", "Aug 2026", new DateTime(2026, 8, 25), 1600m);
        allocated.AllocationStatus = (int)XeroAllocationStatus.Allocated;
        fixture.Context.XeroLedgerLines.Add(allocated);
        await fixture.Context.SaveChangesAsync();

        var adam = Assert.Single(await fixture.RunAsync());

        Assert.Equal(XeroCodingOutcome.Skipped, adam.Outcome);
        Assert.StartsWith("Bill \"Aug 2026\" has 1 line(s) already allocated by hand", adam.Detail);
        Assert.Empty(fixture.Xero.Calls);
    }

    [Fact]
    public async Task TwoBillsThatBothLookLikeTheWorkersAreSkippedAndListed()
    {
        var fixture = await Fixture.SignedOff(true);
        fixture.Context.XeroLedgerLines.Add(LedgerLine("bill-1:a", "bill-1", "Adam Midgley", "Aug 2026", new DateTime(2026, 8, 25), 1600m));
        fixture.Context.XeroLedgerLines.Add(LedgerLine("bill-2:a", "bill-2", "Adam Midgley", "INV-9", new DateTime(2026, 9, 2), 800m));
        await fixture.Context.SaveChangesAsync();

        var adam = Assert.Single(await fixture.RunAsync());

        Assert.Equal(XeroCodingOutcome.Skipped, adam.Outcome);
        Assert.Equal("2 bills in Xero look like Adam Midgley's for Aug 2026: \"Aug 2026\" (AUTHORISED, £1,600.00), \"INV-9\" (AUTHORISED, £800.00). Mark the right one as settlement on the Cost allocation page's Labour tab, then re-run.", adam.Detail);
    }

    [Fact]
    public async Task ABillStatedForAnotherMonthIsNotTheWorkersForThisOne()
    {
        var fixture = await Fixture.SignedOff(true);
        fixture.Context.XeroLedgerLines.Add(LedgerLine("bill-1:a", "bill-1", "Adam Midgley", "Jul 2026", new DateTime(2026, 8, 3), 1600m));
        await fixture.Context.SaveChangesAsync();
        fixture.Xero.StagedBillId = "bill-2";

        var adam = Assert.Single(await fixture.RunAsync());

        Assert.Equal(XeroCodingOutcome.DraftStaged, adam.Outcome);
    }

    [Fact]
    public async Task OnlyTheWorkersAskedForAreRun()
    {
        var fixture = await Fixture.SignedOff(true);

        var results = await fixture.RunAsync(workerIds: new[] { "W-NOBODY" });

        Assert.Empty(results);
        Assert.Empty(fixture.Runs());
    }

    // ---- Resetting an outcome -------------------------------------------------------------------

    [Fact]
    public async Task AResetAppendsAResetOutcomeCarryingWhatItWas()
    {
        var fixture = await Fixture.SignedOff(true);
        await fixture.RecordAsync(XeroCodingOutcome.DraftStaged, "bill-9", new DateTimeOffset(2026, 9, 3, 10, 15, 0, TimeSpan.Zero));

        var acknowledgement = await new ResetXeroCodingOutcomeHandler(fixture.Context)
            .HandleAsync(new ResetXeroCodingOutcome("W-ADAM", 2026, 8, "Draft deleted by hand"), "nigel@jewelbb.co.uk", CancellationToken.None);

        var runs = fixture.Runs();
        Assert.Equal(2, runs.Count);
        var reset = runs.Single(run => run.XeroCodingRunId == acknowledgement.EntityId);
        Assert.Equal(((int)XeroCodingOutcome.Reset, "bill-9", "nigel@jewelbb.co.uk"), (reset.Outcome, reset.XeroBillId, reset.RunByEmail));
        Assert.Equal("Reset by nigel@jewelbb.co.uk: Draft deleted by hand (was DraftStaged at 03 Sep 2026 10:15, bill bill-9) — the next run takes this month again.", reset.Detail);
        Assert.Empty(fixture.Xero.Calls);
    }

    [Fact]
    public async Task AResetRefusesAMonthThatNothingBlocks()
    {
        var fixture = await Fixture.SignedOff(true);
        var handler = new ResetXeroCodingOutcomeHandler(fixture.Context);

        var never = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new ResetXeroCodingOutcome("W-ADAM", 2026, 8, "why"), "", CancellationToken.None));
        Assert.Equal("Adam Midgley's Aug 2026 has no coding outcome to reset — the run has never written it, so it will run as it is.", never.Message);

        await fixture.RecordAsync(XeroCodingOutcome.Skipped, "", new DateTimeOffset(2026, 9, 3, 10, 15, 0, TimeSpan.Zero));
        var skipped = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new ResetXeroCodingOutcome("W-ADAM", 2026, 8, "why"), "", CancellationToken.None));
        Assert.Equal("Adam Midgley's Aug 2026 reads Skipped (03 Sep 10:15) — that does not block the run, so there is nothing to reset.", skipped.Message);
    }

    // ---- Builders ---------------------------------------------------------------------------------

    private static XeroBillSummary Bill(string status, decimal total, string invoiceNumber = "Aug 2026") =>
        new("bill-1", status, invoiceNumber, null, "Adam Midgley", new DateTime(2026, 8, 25), "Inclusive",
            total, 0m, total, status == "PAID" ? total : 0m, 0m, status == "PAID" ? 0m : total, 1, "NONE");

    private static XeroLedgerLineEntity LedgerLine(string id, string billId, string contact, string number, DateTime date, decimal net) => new()
    {
        XeroLedgerLineId = id, XeroInvoiceId = billId, XeroLineItemId = id.Split(':')[1], Type = "ACCPAY", InvoiceNumber = number,
        ContactName = contact, Date = date, InvoiceStatus = "AUTHORISED", Net = net, InvoiceTotal = net, AmountDue = net,
        AccountCode = "321", AccountName = "Subcontractors", FirstSeenAtUtc = date, LastSyncedAtUtc = date,
    };

    private sealed class Fixture
    {
        public JpmsContext Context { get; }
        public RecordingXero Xero { get; } = new();

        private Fixture(JpmsContext context) { Context = context; }

        public static async Task<Fixture> SignedOff(bool signedOff, bool mapBrickwork = true, bool soleTrader = true)
        {
            var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
                .UseInMemoryDatabase($"xero-coding-{Guid.NewGuid():N}").Options);
            context.Workers.Add(new WorkerEntity { WorkerId = "W-ADAM", Name = "Adam Midgley", HourlyRate = 25m, IsActive = true, IsSoleTrader = soleTrader });
            context.Projects.Add(new ProjectEntity { ProjectId = "P1", Reference = "JBB-2026-004", Name = "Woodhouse", ClientName = "David Needham" });
            context.Timesheets.Add(Timesheet("T1", "SUB-GWK", 1000m));
            context.Timesheets.Add(Timesheet("T2", "SUB-BRK", 600m));
            if (signedOff)
                context.LabourWeekSignOffs.Add(new LabourWeekSignOffEntity
                {
                    LabourWeekSignOffId = "S1", WorkerId = "W-ADAM", WeekStart = MondayAugust17, MonthStart = August,
                    SignedOffByEmail = "accounts@jewelbb.co.uk", SignedOffAt = August,
                });
            context.SiteXeroMappings.Add(new SiteXeroMappingEntity
            {
                SiteXeroMappingId = "SM1", ProjectId = "P1", XeroTrackingOptionId = "opt-woodhouse", XeroTrackingOptionName = "Woodhouse", EffectiveFrom = August.AddYears(-1),
            });
            context.CostCodeXeroMappings.Add(CostCodeMapping("SUB-GWK"));
            if (mapBrickwork) context.CostCodeXeroMappings.Add(CostCodeMapping("SUB-BRK"));
            await context.SaveChangesAsync();
            return new Fixture(context);
        }

        private static TimesheetEntity Timesheet(string id, string costCode, decimal cost) => new()
        {
            TimesheetId = id, ProjectId = "P1", WorkerId = "W-ADAM", WorkedOn = MondayAugust17, Hours = cost / 25m, CostCode = costCode,
            Status = (int)TimesheetStatus.Approved, IsApproved = true, RateApplied = 25m, CostAmount = cost,
        };

        private static CostCodeXeroMappingEntity CostCodeMapping(string code) => new()
        {
            CostCodeXeroMappingId = $"CM-{code}", CostCode = code, XeroTrackingOptionId = $"opt-{code}", XeroTrackingOptionName = code,
            LabourAccountCode = "321", MaterialsAccountCode = "322", TravelAccountCode = "323", EffectiveFrom = August.AddYears(-1),
        };

        public async Task RecordAsync(XeroCodingOutcome outcome, string billId, DateTimeOffset at)
        {
            Context.XeroCodingRuns.Add(new XeroCodingRunEntity
            {
                XeroCodingRunId = $"R-{Guid.NewGuid():N}", WorkerId = "W-ADAM", Month = August, Outcome = (int)outcome, XeroBillId = billId, Detail = "earlier", RunAt = at,
            });
            await Context.SaveChangesAsync();
        }

        public Task<IReadOnlyList<XeroCodingRunResult>> RunAsync(bool dryRun = false, IReadOnlyList<string>? workerIds = null) =>
            new RunXeroCodingHandler(Context, new SettlementScheduleBuilder(Context), Xero, new XeroOptions())
                .HandleAsync(new RunXeroCoding(2026, 8, workerIds, dryRun), "accounts@jewelbb.co.uk", CancellationToken.None);

        public List<XeroCodingRunEntity> Runs() => Context.XeroCodingRuns.AsNoTracking().OrderBy(run => run.RunAt).ToList();
    }

    /// <summary>Xero as the run sees it: bills by id, a draft that lands with a given id, a recode
    /// that answers with fresh line ids — every call recorded in order.</summary>
    private sealed class RecordingXero : IXeroClient
    {
        public List<string> Calls { get; } = new();
        public Dictionary<string, XeroBillSummary?> Bills { get; } = new();
        public string StagedBillId { get; set; } = "";
        public string[] RecodedLineIds { get; set; } = Array.Empty<string>();
        public XeroDraftBillRequest? Draft { get; private set; }
        public XeroBillCodingRequest? Recode { get; private set; }

        public bool IsConfigured => true;

        public Task<XeroBillSummary?> GetBillAsync(string invoiceId, CancellationToken ct)
        {
            Calls.Add($"GetBill:{invoiceId}");
            return Task.FromResult(Bills.TryGetValue(invoiceId, out var bill) ? bill : null);
        }

        public Task<XeroApprovalResult> CreateDraftBillAsync(XeroDraftBillRequest request, CancellationToken ct)
        {
            Calls.Add("CreateDraftBill");
            Draft = request;
            return Task.FromResult(XeroApprovalResult.Ok(StagedBillId, "Tax from the contact."));
        }

        public Task<XeroBillRecodeResult> RecodeBillAsync(XeroBillCodingRequest request, CancellationToken ct)
        {
            Calls.Add($"RecodeBill:{request.InvoiceId}");
            Recode = request;
            var before = Bills[request.InvoiceId]!;
            var lines = request.Lines.Select((line, index) => new XeroRecodedLine(
                RecodedLineIds[index], line.Description, line.Net, 0m, line.AccountCode, line.SiteOption, line.CostCodeOption)).ToList();
            return Task.FromResult(new XeroBillRecodeResult(true, null, before.Status, before.LineAmountTypes, before.TaxType,
                before.SubTotal, before.TotalTax, before.Total, lines));
        }

        public Task<XeroTransactionsSnapshot> GetPurchaseInvoicesAsync(bool force, CancellationToken ct) => throw new NotSupportedException();
        public Task<XeroCashSummarySnapshot> GetCashSummaryAsync(bool force, CancellationToken ct) => throw new NotSupportedException();
        public Task<XeroAgedPayablesSnapshot> GetAgedPayablesAsync(bool force, CancellationToken ct) => throw new NotSupportedException();
        public Task<XeroAgedReceivablesSnapshot> GetAgedReceivablesAsync(bool force, CancellationToken ct) => throw new NotSupportedException();
        public Task<XeroSuppliersSnapshot> GetSuppliersAsync(bool force, CancellationToken ct) => throw new NotSupportedException();
        public Task<XeroTrackingCategoriesSnapshot> GetTrackingCategoriesSnapshotAsync(bool force, CancellationToken ct) => throw new NotSupportedException();
        public Task<XeroApprovalResult> ApproveInvoiceAsync(XeroApprovalRequest request, CancellationToken ct) => throw new NotSupportedException();
        public Task<XeroApprovalResult> SetSiteTrackingAsync(XeroSiteTrackingRequest request, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<XeroInvoiceAttachment>> ListAttachmentsAsync(string invoiceId, bool isCreditNote, CancellationToken ct) => throw new NotSupportedException();
        public Task<XeroAttachmentContent?> GetAttachmentAsync(string invoiceId, bool isCreditNote, string fileName, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<XeroSitePnlMonthFigures>> GetSiteMonthlyPnlAsync(string siteOption, DateTime fromMonth, DateTime toMonth, CancellationToken ct) => throw new NotSupportedException();
        public Task<XeroSitePnlRangeFigures?> GetSiteRangePnlAsync(string siteOption, DateTime fromDate, DateTime toDate, CancellationToken ct) => throw new NotSupportedException();
    }
}
