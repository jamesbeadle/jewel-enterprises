using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Labour;
using Jewel.JPMS.Api.Features.Labour.Commands;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Tests;

/// <summary>
/// August 2026 as the accountant described it on 8 Sep 2026: Jewel Property Serve Ltd bills three
/// workers — Dan Prowse £4,200, Finley Taylor £1,350, John Ahern £920 — on one invoice across five
/// sites, every worker-month signed off, every site and the cost code mapped.
/// </summary>
internal sealed class JewelPropertyServeFixture
{
    public static readonly DateTimeOffset August = new(new DateTime(2026, 8, 1), TimeSpan.Zero);
    public static readonly DateTimeOffset MondayAugust17 = new(new DateTime(2026, 8, 17), TimeSpan.Zero);
    public const string Company = "sub-cl-jewelps";
    public const string CompanyName = "Jewel Property Serve Ltd";
    public static readonly string[] NineLineIds = { "n1", "n2", "n3", "n4", "n5", "n6", "n7", "n8", "n9" };

    public JpmsContext Context { get; }
    public RecordingXero Xero { get; } = new();

    private JewelPropertyServeFixture(JpmsContext context) { Context = context; }

    public static async Task<JewelPropertyServeFixture> CreateAsync(bool isFinleySignedOff = true)
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"jps-coding-{Guid.NewGuid():N}").Options);
        context.Subcontractors.Add(new SubcontractorEntity { SubcontractorId = Company, CompanyName = CompanyName });
        context.Workers.Add(Worker("W-DAN", "Dan Prowse"));
        context.Workers.Add(Worker("W-FINLEY", "Finley Taylor"));
        context.Workers.Add(Worker("W-JOHN", "John Ahern"));
        AddSite(context, "P-ABBOT", "Abbot Road", "17a Abbot Road");
        AddSite(context, "P-BYFRANCE", "By France", "By France");
        AddSite(context, "P-RAVENSWOOD", "Ravenswood Ave", "64 Ravenswood Avenue");
        AddSite(context, "P-COOMBE", "Coombe Lane", "149a Coombe Lane West");
        AddSite(context, "P-WOODHOUSE", "Woodhouse", "Woodhouse Lane");
        context.CostCodeXeroMappings.Add(new CostCodeXeroMappingEntity
        {
            CostCodeXeroMappingId = "CM-PRELIMS-LAB", CostCode = "PRELIMS-LAB", XeroTrackingOptionId = "opt-prelims-lab",
            XeroTrackingOptionName = "PRELIMS-LAB", LabourAccountCode = "321", MaterialsAccountCode = "322", TravelAccountCode = "323",
            EffectiveFrom = August.AddYears(-1),
        });
        AddApprovedTime(context, "W-DAN", ("P-BYFRANCE", 3400m), ("P-ABBOT", 400m), ("P-RAVENSWOOD", 100m), ("P-COOMBE", 300m));
        AddApprovedTime(context, "W-FINLEY", ("P-BYFRANCE", 450m), ("P-ABBOT", 750m), ("P-WOODHOUSE", 150m));
        AddApprovedTime(context, "W-JOHN", ("P-ABBOT", 690m), ("P-RAVENSWOOD", 230m));
        SignOff(context, "W-DAN");
        if (isFinleySignedOff) SignOff(context, "W-FINLEY");
        SignOff(context, "W-JOHN");
        await context.SaveChangesAsync();
        return new JewelPropertyServeFixture(context);
    }

    /// <summary>One ledger line for a one-line bill from the company, as the sync stores it.</summary>
    public XeroLedgerLineEntity AddLedgerBill(string billId, string number, DateTime date, decimal net, string status = "AUTHORISED")
    {
        var line = new XeroLedgerLineEntity
        {
            XeroLedgerLineId = $"{billId}:old", XeroInvoiceId = billId, XeroLineItemId = "old", Type = "ACCPAY", InvoiceNumber = number,
            ContactName = CompanyName, Date = date, InvoiceStatus = status, Net = net, InvoiceTotal = net, AmountDue = net,
            AccountCode = "321", AccountName = "CIS Labour Expense", FirstSeenAtUtc = date, LastSyncedAtUtc = date,
        };
        Context.XeroLedgerLines.Add(line);
        return line;
    }

    public void AddCover(string ledgerLineId, string? workerId = null) =>
        Context.XeroLineTimesheetCovers.Add(new XeroLineTimesheetCoverEntity
        {
            XeroLineTimesheetCoverId = $"C-{ledgerLineId}", XeroLedgerLineId = ledgerLineId, ProjectId = "", SubcontractorId = Company,
            WorkerId = workerId, PeriodStart = August, PeriodEnd = August.AddMonths(1), CreatedByEmail = "jeremy@jewelbb.co.uk", CreatedAt = August,
        });

    /// <summary>The company's bill as Xero holds it: DRC 20% (no VAT on the bill), £6,470 net unless said otherwise.</summary>
    public static XeroBillSummary Bill(string billId, string status, decimal total = 6470m, string number = "INV-1252", DateTime? date = null) =>
        new(billId, status, number, null, CompanyName, date ?? new DateTime(2026, 8, 31), "Exclusive",
            total, 0m, total, 0m, 0m, total, 1, "REVERSECHARGES");

    public async Task RecordAsync(string workerId, XeroCodingOutcome outcome, string billId, DateTimeOffset at)
    {
        Context.XeroCodingRuns.Add(new XeroCodingRunEntity
        {
            XeroCodingRunId = $"R-{Guid.NewGuid():N}", WorkerId = workerId, Month = August, Outcome = (int)outcome, XeroBillId = billId, Detail = "earlier", RunAt = at,
        });
        await Context.SaveChangesAsync();
    }

    public Task<IReadOnlyList<XeroCodingRunResult>> RunAsync(bool dryRun = false, IReadOnlyList<string>? workerIds = null) =>
        new RunXeroCodingHandler(Context, new SettlementScheduleBuilder(Context), Xero, new XeroOptions())
            .HandleAsync(new RunXeroCoding(2026, 8, workerIds, dryRun), "accounts@jewelbb.co.uk", CancellationToken.None);

    public Task<SettlementScheduleSnapshot> SchedulesAsync() => new SettlementScheduleBuilder(Context).BuildAsync(2026, 8, CancellationToken.None);

    public List<XeroLineTimesheetCoverEntity> Covers() => Context.XeroLineTimesheetCovers.AsNoTracking().OrderBy(cover => cover.XeroLedgerLineId).ToList();

    public List<XeroLedgerLineEntity> Lines() => Context.XeroLedgerLines.AsNoTracking().OrderBy(line => line.XeroLedgerLineId).ToList();

    public List<XeroCodingRunEntity> Runs() => Context.XeroCodingRuns.AsNoTracking().OrderBy(run => run.RunAt).ToList();

    private static WorkerEntity Worker(string id, string name) =>
        new() { WorkerId = id, Name = name, SubcontractorId = Company, HourlyRate = 25m, IsActive = true };

    private static void AddSite(JpmsContext context, string projectId, string name, string trackingOption)
    {
        context.Projects.Add(new ProjectEntity { ProjectId = projectId, Reference = $"JBB-2026-{projectId}", Name = name, ClientName = "Client" });
        context.SiteXeroMappings.Add(new SiteXeroMappingEntity
        {
            SiteXeroMappingId = $"SM-{projectId}", ProjectId = projectId, XeroTrackingOptionId = $"opt-{projectId}",
            XeroTrackingOptionName = trackingOption, EffectiveFrom = August.AddYears(-1),
        });
    }

    private static void AddApprovedTime(JpmsContext context, string workerId, params (string ProjectId, decimal Cost)[] days)
    {
        foreach (var (projectId, cost) in days)
            context.Timesheets.Add(new TimesheetEntity
            {
                TimesheetId = $"T-{workerId}-{projectId}", ProjectId = projectId, WorkerId = workerId, WorkedOn = MondayAugust17, Hours = cost / 25m,
                CostCode = "PRELIMS-LAB", Status = (int)TimesheetStatus.Approved, IsApproved = true, RateApplied = 25m, CostAmount = cost,
            });
    }

    private static void SignOff(JpmsContext context, string workerId) =>
        context.LabourWeekSignOffs.Add(new LabourWeekSignOffEntity
        {
            LabourWeekSignOffId = $"S-{workerId}", WorkerId = workerId, WeekStart = MondayAugust17, MonthStart = August,
            SignedOffByEmail = "accounts@jewelbb.co.uk", SignedOffAt = August,
        });
}
