using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero.Ledger;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// Characterisation of the allocation command — every action the Cost allocation page can take
/// on a ledger line — pinned before <c>SetXeroAllocationHandler</c> is divided: what each action
/// leaves on the line, which refusals guard it and with what words, how a split is reconciled
/// against the rows already there, when work-order links survive, what the dispute thread
/// records, and which Xero write the handler asks for afterwards. A recording fake stands in
/// for the write-back service; the handler runs over an in-memory database.
/// </summary>
public sealed class SetXeroAllocationHandlerTests
{
    // ---- Allocate ------------------------------------------------------------------------------

    [Fact]
    public async Task AllocatingABatchStampsEveryLineAndAsksForTheWriteBackOncePerInvoice()
    {
        var fixture = await Fixture.CreateAsync();

        var count = await fixture.HandleAsync(Command(new[] { "inv-1:a", "inv-1:b" }, XeroAllocationAction.Allocate, projectId: "P1", costCenterCode: "SUB-GWK", note: "Groundworks"));

        Assert.Equal(2, count);
        foreach (var line in fixture.Lines("inv-1:a", "inv-1:b"))
        {
            Assert.Equal((int)XeroAllocationStatus.Allocated, line.AllocationStatus);
            Assert.Equal(("P1", "SUB-GWK", null, "nigel@jewelbb.co.uk", "Groundworks"), (line.ProjectId, line.CostCenterCode, line.Bucket, line.AllocatedBy, line.Note));
            Assert.NotNull(line.AllocatedAtUtc);
        }
        Assert.Equal(new[] { "WriteBack:inv-1" }, fixture.WriteBack.Calls);
    }

    [Fact]
    public async Task AllocatingRefusesAnUnknownProjectOrAnInactiveCentre()
    {
        var fixture = await Fixture.CreateAsync();

        var noProject = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.HandleAsync(Command(new[] { "inv-1:a" }, XeroAllocationAction.Allocate, projectId: "P9", costCenterCode: "SUB-GWK")));
        Assert.Equal("Choose a project before allocating.", noProject.Message);

        var inactive = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.HandleAsync(Command(new[] { "inv-1:a" }, XeroAllocationAction.Allocate, projectId: "P1", costCenterCode: "SUB-OLD")));
        Assert.Equal("Choose an active cost centre before allocating.", inactive.Message);
        Assert.Empty(fixture.WriteBack.Calls);
    }

    [Fact]
    public async Task ASplitIsReconciledAgainstTheRowsAlreadyThereAndNullsTheLinesOwnCoding()
    {
        var fixture = await Fixture.CreateAsync();
        fixture.Context.XeroCostSplits.Add(new XeroCostSplitEntity { XeroCostSplitId = "inv-1:a:P1:SUB-GWK", XeroLedgerLineId = "inv-1:a", ProjectId = "P1", CostCenterCode = "SUB-GWK", Net = 100m });
        fixture.Context.XeroCostSplits.Add(new XeroCostSplitEntity { XeroCostSplitId = "inv-1:a:P1:SUB-OLD", XeroLedgerLineId = "inv-1:a", ProjectId = "P1", CostCenterCode = "SUB-OLD", Net = 900m });
        await fixture.Context.SaveChangesAsync();

        await fixture.HandleAsync(Command(new[] { "inv-1:a" }, XeroAllocationAction.Allocate, projectId: "P1",
            splits: new[] { new XeroCostSplit("SUB-GWK", 400m), new XeroCostSplit("SUB-BRK", 600m, "P2") }));

        var line = fixture.Line("inv-1:a");
        Assert.Equal((int)XeroAllocationStatus.Allocated, line.AllocationStatus);
        Assert.Null(line.ProjectId);
        Assert.Null(line.CostCenterCode);
        var splits = fixture.Context.XeroCostSplits.OrderBy(split => split.XeroCostSplitId).ToList();
        Assert.Equal(new[] { "inv-1:a:P1:SUB-GWK", "inv-1:a:P2:SUB-BRK" }, splits.Select(split => split.XeroCostSplitId));
        Assert.Equal(new[] { 400m, 600m }, splits.Select(split => split.Net));
        Assert.Equal(new[] { "WriteBack:inv-1" }, fixture.WriteBack.Calls);
    }

    [Fact]
    public async Task ASplitWithinOneProjectKeepsThatProjectOnTheLine()
    {
        var fixture = await Fixture.CreateAsync();

        await fixture.HandleAsync(Command(new[] { "inv-1:a" }, XeroAllocationAction.Allocate, projectId: "P1",
            splits: new[] { new XeroCostSplit("SUB-GWK", 400m), new XeroCostSplit("SUB-BRK", 600m) }));

        Assert.Equal(("P1", null), (fixture.Line("inv-1:a").ProjectId, fixture.Line("inv-1:a").CostCenterCode));
    }

    [Fact]
    public async Task AOneEntrySplitCollapsesToAWholeLineAllocationThatMustStillMatchTheNet()
    {
        var fixture = await Fixture.CreateAsync();

        await fixture.HandleAsync(Command(new[] { "inv-1:a" }, XeroAllocationAction.Allocate, splits: new[] { new XeroCostSplit("SUB-GWK", 1000m, "P1") }));
        Assert.Equal(("P1", "SUB-GWK"), (fixture.Line("inv-1:a").ProjectId, fixture.Line("inv-1:a").CostCenterCode));
        Assert.Empty(fixture.Context.XeroCostSplits);

        var wrong = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.HandleAsync(Command(new[] { "inv-1:b" }, XeroAllocationAction.Allocate, splits: new[] { new XeroCostSplit("SUB-GWK", 10m, "P1") })));
        Assert.Equal("The split must add up to the line's net of 250.00 — it currently adds up to 10.00.", wrong.Message);
    }

    [Theory]
    [InlineData("two-lines", "A split applies to one line at a time.")]
    [InlineData("zero", "Every split amount must be greater than zero.")]
    [InlineData("no-project", "Every split row needs a project.")]
    [InlineData("duplicate", "Each project + cost centre combination can appear only once in a split.")]
    [InlineData("unknown-project", "Not a known project: P9.")]
    [InlineData("inactive", "Not an active cost centre: SUB-OLD.")]
    [InlineData("short", "The split must add up to the line's net of 1000.00 — it currently adds up to 900.00.")]
    public async Task ASplitIsRefusedForEachShapeMistake(string mistake, string expected)
    {
        var fixture = await Fixture.CreateAsync();
        var (ids, splits) = mistake switch
        {
            "two-lines" => (new[] { "inv-1:a", "inv-1:b" }, new[] { new XeroCostSplit("SUB-GWK", 500m, "P1"), new XeroCostSplit("SUB-BRK", 500m, "P1") }),
            "zero" => (new[] { "inv-1:a" }, new[] { new XeroCostSplit("SUB-GWK", 0m, "P1"), new XeroCostSplit("SUB-BRK", 1000m, "P1") }),
            "no-project" => (new[] { "inv-1:a" }, new[] { new XeroCostSplit("SUB-GWK", 500m), new XeroCostSplit("SUB-BRK", 500m) }),
            "duplicate" => (new[] { "inv-1:a" }, new[] { new XeroCostSplit("SUB-GWK", 500m, "P1"), new XeroCostSplit("sub-gwk", 500m, "P1") }),
            "unknown-project" => (new[] { "inv-1:a" }, new[] { new XeroCostSplit("SUB-GWK", 500m, "P1"), new XeroCostSplit("SUB-BRK", 500m, "P9") }),
            "inactive" => (new[] { "inv-1:a" }, new[] { new XeroCostSplit("SUB-GWK", 500m, "P1"), new XeroCostSplit("SUB-OLD", 500m, "P1") }),
            _ => (new[] { "inv-1:a" }, new[] { new XeroCostSplit("SUB-GWK", 400m, "P1"), new XeroCostSplit("SUB-BRK", 500m, "P1") }),
        };

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.HandleAsync(Command(ids, XeroAllocationAction.Allocate, splits: splits)));

        Assert.Equal(expected, refusal.Message);
    }

    [Fact]
    public async Task MovingAnApprovedLineToAnotherProjectRewritesItsSiteInXero()
    {
        var fixture = await Fixture.CreateAsync();
        await fixture.HandleAsync(Command(new[] { "inv-2:a" }, XeroAllocationAction.SetProject, projectId: "P1"));
        fixture.WriteBack.Calls.Clear();

        await fixture.HandleAsync(Command(new[] { "inv-2:a", "inv-1:a" }, XeroAllocationAction.Allocate, projectId: "P2", costCenterCode: "SUB-GWK"));

        Assert.Equal(new[] { "WriteBack:inv-1,inv-2", "SetSite:inv-2:a" }, fixture.WriteBack.Calls);
    }

    // ---- Work-order links -----------------------------------------------------------------------

    [Fact]
    public async Task MovingBetweenCentresWithinTheProjectKeepsTheLinksAndRecodesTheOrders()
    {
        var fixture = await Fixture.CreateAsync();
        await fixture.HandleAsync(Command(new[] { "inv-1:a" }, XeroAllocationAction.Allocate, projectId: "P1", costCenterCode: "SUB-GWK"));
        await fixture.LinkToOrderAsync("inv-1:a", "P1", "WO-1");

        await fixture.HandleAsync(Command(new[] { "inv-1:a" }, XeroAllocationAction.Allocate, projectId: "P1", costCenterCode: "SUB-BRK"));

        Assert.Single(fixture.Context.XeroLineWorkOrderLinks);
        Assert.Single(fixture.Context.ReconciliationPackageCostLines);
        Assert.All(fixture.Context.WorkOrderLines.Where(line => line.WorkOrderId == "WO-1"), line => Assert.Equal("SUB-BRK", line.CostCode));
    }

    [Theory]
    [InlineData("other-project")]
    [InlineData("split-across-projects")]
    [InlineData("bucket")]
    [InlineData("ignore")]
    [InlineData("reset")]
    [InlineData("dispute")]
    public async Task AnyOtherMoveClearsTheLinksAndPackageSlices(string move)
    {
        var fixture = await Fixture.CreateAsync();
        await fixture.HandleAsync(Command(new[] { "inv-1:a" }, XeroAllocationAction.Allocate, projectId: "P1", costCenterCode: "SUB-GWK"));
        await fixture.LinkToOrderAsync("inv-1:a", "P1", "WO-1");
        var command = move switch
        {
            "other-project" => Command(new[] { "inv-1:a" }, XeroAllocationAction.Allocate, projectId: "P2", costCenterCode: "SUB-GWK"),
            "split-across-projects" => Command(new[] { "inv-1:a" }, XeroAllocationAction.Allocate, projectId: "P1", splits: new[] { new XeroCostSplit("SUB-GWK", 400m, "P1"), new XeroCostSplit("SUB-BRK", 600m, "P2") }),
            "bucket" => Command(new[] { "inv-1:a" }, XeroAllocationAction.AllocateToBucket, bucket: XeroBuckets.Fuel),
            "ignore" => Command(new[] { "inv-1:a" }, XeroAllocationAction.Ignore),
            "reset" => Command(new[] { "inv-1:a" }, XeroAllocationAction.Reset),
            _ => Command(new[] { "inv-1:a" }, XeroAllocationAction.Dispute),
        };

        await fixture.HandleAsync(command);

        Assert.Empty(fixture.Context.XeroLineWorkOrderLinks);
        Assert.Empty(fixture.Context.ReconciliationPackageCostLines);
        Assert.All(fixture.Context.WorkOrderLines.Where(line => line.WorkOrderId == "WO-1"), line => Assert.Equal("SUB-GWK", line.CostCode));
    }

    // A Work Order bill against a multi-code order is a same-project centre split with links
    // (2026-09-08), so re-cutting the centres within the project keeps the links — only the
    // whole-line move recodes the orders, because a split has no single centre to recode to.
    [Fact]
    public async Task ASplitAcrossCentresOnTheSameProjectKeepsTheLinksWithoutRecodingTheOrders()
    {
        var fixture = await Fixture.CreateAsync();
        await fixture.HandleAsync(Command(new[] { "inv-1:a" }, XeroAllocationAction.Allocate, projectId: "P1", costCenterCode: "SUB-GWK"));
        await fixture.LinkToOrderAsync("inv-1:a", "P1", "WO-1");

        await fixture.HandleAsync(Command(new[] { "inv-1:a" }, XeroAllocationAction.Allocate, projectId: "P1",
            splits: new[] { new XeroCostSplit("SUB-GWK", 400m), new XeroCostSplit("SUB-BRK", 600m) }));

        Assert.Single(fixture.Context.XeroLineWorkOrderLinks);
        Assert.Single(fixture.Context.ReconciliationPackageCostLines);
        Assert.All(fixture.Context.WorkOrderLines.Where(line => line.WorkOrderId == "WO-1"), line => Assert.Equal("SUB-GWK", line.CostCode));
    }

    // ---- Bucket, ignore, reset --------------------------------------------------------------------

    [Fact]
    public async Task BucketingIgnoringAndResettingClearTheCodingEachInTheirOwnWay()
    {
        var fixture = await Fixture.CreateAsync();

        await fixture.HandleAsync(Command(new[] { "inv-1:a" }, XeroAllocationAction.AllocateToBucket, bucket: XeroBuckets.Parking, note: "Meter"));
        var bucketed = fixture.Line("inv-1:a");
        Assert.Equal(((int)XeroAllocationStatus.Bucketed, null, null, XeroBuckets.Parking, "nigel@jewelbb.co.uk", "Meter"),
            (bucketed.AllocationStatus, bucketed.ProjectId, bucketed.CostCenterCode, bucketed.Bucket, bucketed.AllocatedBy, bucketed.Note));

        await fixture.HandleAsync(Command(new[] { "inv-1:b" }, XeroAllocationAction.Ignore, note: "Personal"));
        var ignored = fixture.Line("inv-1:b");
        Assert.Equal(((int)XeroAllocationStatus.Ignored, null, "nigel@jewelbb.co.uk", "Personal"), (ignored.AllocationStatus, ignored.Bucket, ignored.AllocatedBy, ignored.Note));

        await fixture.HandleAsync(Command(new[] { "inv-1:a", "inv-1:b" }, XeroAllocationAction.Reset));
        foreach (var line in fixture.Lines("inv-1:a", "inv-1:b"))
            Assert.Equal(((int)XeroAllocationStatus.Unallocated, null, null, null, null, null, null),
                (line.AllocationStatus, line.ProjectId, line.CostCenterCode, line.Bucket, line.AllocatedBy, line.AllocatedAtUtc, line.Note));
        Assert.Empty(fixture.WriteBack.Calls);
    }

    [Fact]
    public async Task AnUnknownBucketIsRefused()
    {
        var fixture = await Fixture.CreateAsync();

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.HandleAsync(Command(new[] { "inv-1:a" }, XeroAllocationAction.AllocateToBucket, bucket: "Sweets")));

        Assert.Equal("Choose a bucket (Parking, Fuel, Software subscriptions or Other).", refusal.Message);
    }

    // ---- Set project (the half-step) ------------------------------------------------------------

    [Fact]
    public async Task SettingTheProjectSavesItWithoutAllocatingAndWritesTheSiteForQueuedLinesOnly()
    {
        var fixture = await Fixture.CreateAsync();
        await fixture.HandleAsync(Command(new[] { "inv-1:b" }, XeroAllocationAction.Dispute));
        fixture.WriteBack.Calls.Clear();

        await fixture.HandleAsync(Command(new[] { "inv-1:a", "inv-1:b" }, XeroAllocationAction.SetProject, projectId: "P1", costCenterCode: "SUB-GWK"));

        var queued = fixture.Line("inv-1:a");
        Assert.Equal(((int)XeroAllocationStatus.Unallocated, "P1", "SUB-GWK", null), (queued.AllocationStatus, queued.ProjectId, queued.CostCenterCode, queued.AllocatedBy));
        var disputed = fixture.Line("inv-1:b");
        Assert.Equal(((int)XeroAllocationStatus.Disputed, "P1", "SUB-GWK"), (disputed.AllocationStatus, disputed.ProjectId, disputed.CostCenterCode));
        Assert.Equal(new[] { "SetSite:inv-1:a" }, fixture.WriteBack.Calls);
    }

    [Fact]
    public async Task UnsettingTheProjectClearsTheCentreToo()
    {
        var fixture = await Fixture.CreateAsync();
        await fixture.HandleAsync(Command(new[] { "inv-1:a" }, XeroAllocationAction.SetProject, projectId: "P1", costCenterCode: "SUB-GWK"));

        await fixture.HandleAsync(Command(new[] { "inv-1:a" }, XeroAllocationAction.SetProject, projectId: null));

        Assert.Equal((null, null), (fixture.Line("inv-1:a").ProjectId, fixture.Line("inv-1:a").CostCenterCode));
    }

    [Fact]
    public async Task SettingTheProjectIsRefusedForUnknownProjectsAllocatedLinesAndInactiveCentres()
    {
        var fixture = await Fixture.CreateAsync();

        var unknown = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.HandleAsync(Command(new[] { "inv-1:a" }, XeroAllocationAction.SetProject, projectId: "P9")));
        Assert.Equal("Choose a project to set.", unknown.Message);

        await fixture.HandleAsync(Command(new[] { "inv-1:b" }, XeroAllocationAction.Allocate, projectId: "P1", costCenterCode: "SUB-GWK"));
        var allocated = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.HandleAsync(Command(new[] { "inv-1:b" }, XeroAllocationAction.SetProject, projectId: "P1")));
        Assert.Equal("Set applies to queued or disputed lines only.", allocated.Message);

        var inactive = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.HandleAsync(Command(new[] { "inv-1:a" }, XeroAllocationAction.SetProject, projectId: "P1", costCenterCode: "SUB-OLD")));
        Assert.Equal("Choose an active cost centre.", inactive.Message);
    }

    // ---- The dispute trio ---------------------------------------------------------------------------

    [Fact]
    public async Task DisputingParksTheLineKeepingItsCodingAndOpensTheThread()
    {
        var fixture = await Fixture.CreateAsync();
        await fixture.HandleAsync(Command(new[] { "inv-1:a" }, XeroAllocationAction.Allocate, projectId: "P1", costCenterCode: "SUB-GWK"));
        fixture.WriteBack.Calls.Clear();

        await fixture.HandleAsync(Command(new[] { "inv-1:a" }, XeroAllocationAction.Dispute, note: "  This is Abbot Road's, surely?  "));

        var line = fixture.Line("inv-1:a");
        Assert.Equal(((int)XeroAllocationStatus.Disputed, "P1", "SUB-GWK", "nigel@jewelbb.co.uk", "  This is Abbot Road's, surely?  "),
            (line.AllocationStatus, line.ProjectId, line.CostCenterCode, line.AllocatedBy, line.Note));
        var message = Assert.Single(fixture.Context.XeroDisputeMessages);
        Assert.Equal(("inv-1:a", "nigel@jewelbb.co.uk", "This is Abbot Road's, surely?"), (message.XeroLedgerLineId, message.Author, message.Body));
        Assert.Empty(fixture.WriteBack.Calls);
    }

    [Fact]
    public async Task DisputingWithoutAMessageOpensNoThreadAndBucketedLinesCannotBeDisputed()
    {
        var fixture = await Fixture.CreateAsync();
        await fixture.HandleAsync(Command(new[] { "inv-1:a" }, XeroAllocationAction.Dispute));
        Assert.Empty(fixture.Context.XeroDisputeMessages);

        await fixture.HandleAsync(Command(new[] { "inv-1:b" }, XeroAllocationAction.AllocateToBucket, bucket: XeroBuckets.Fuel));
        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.HandleAsync(Command(new[] { "inv-1:b" }, XeroAllocationAction.Dispute)));
        Assert.Equal("Dispute applies to queued or allocated lines.", refusal.Message);
    }

    [Fact]
    public async Task AMessageAppendsToTheThreadAndTouchesNothingElse()
    {
        var fixture = await Fixture.CreateAsync();
        await fixture.HandleAsync(Command(new[] { "inv-1:a" }, XeroAllocationAction.Allocate, projectId: "P1", costCenterCode: "SUB-GWK"));
        await fixture.HandleAsync(Command(new[] { "inv-1:a" }, XeroAllocationAction.Dispute, note: "Opening"));
        await fixture.LinkToOrderAsync("inv-1:a", "P1", "WO-1");
        fixture.Context.XeroCostSplits.Add(new XeroCostSplitEntity { XeroCostSplitId = "inv-1:a:P1:SUB-GWK", XeroLedgerLineId = "inv-1:a", ProjectId = "P1", CostCenterCode = "SUB-GWK", Net = 1000m });
        await fixture.Context.SaveChangesAsync();
        fixture.WriteBack.Calls.Clear();

        await fixture.HandleAsync(Command(new[] { "inv-1:a" }, XeroAllocationAction.AddDisputeMessage, note: "Reply"));

        Assert.Equal(new[] { "Opening", "Reply" }, fixture.Context.XeroDisputeMessages.OrderBy(message => message.SentAtUtc).Select(message => message.Body));
        Assert.Single(fixture.Context.XeroCostSplits);
        Assert.Single(fixture.Context.XeroLineWorkOrderLinks);
        Assert.Equal("Opening", fixture.Line("inv-1:a").Note);
        Assert.Empty(fixture.WriteBack.Calls);

        var blank = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.HandleAsync(Command(new[] { "inv-1:a" }, XeroAllocationAction.AddDisputeMessage, note: " ")));
        Assert.Equal("Write a message.", blank.Message);
        var notDisputed = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.HandleAsync(Command(new[] { "inv-1:b" }, XeroAllocationAction.AddDisputeMessage, note: "x")));
        Assert.Equal("Messages can only be added to disputed lines.", notDisputed.Message);
    }

    [Fact]
    public async Task ResolvingReturnsTheLineToTheQueueWithTheAgreedCodingAndWritesTheSite()
    {
        var fixture = await Fixture.CreateAsync();
        await fixture.HandleAsync(Command(new[] { "inv-1:a", "inv-1:b" }, XeroAllocationAction.Dispute, note: "Which site?"));
        await fixture.HandleAsync(Command(new[] { "inv-1:a" }, XeroAllocationAction.SetProject, projectId: "P2", costCenterCode: "SUB-BRK"));
        fixture.WriteBack.Calls.Clear();

        await fixture.HandleAsync(Command(new[] { "inv-1:a", "inv-1:b" }, XeroAllocationAction.ResolveDispute));

        var agreed = fixture.Line("inv-1:a");
        Assert.Equal(((int)XeroAllocationStatus.Unallocated, "P2", "SUB-BRK", null, null, null), (agreed.AllocationStatus, agreed.ProjectId, agreed.CostCenterCode, agreed.AllocatedBy, agreed.AllocatedAtUtc, agreed.Note));
        Assert.Equal(((int)XeroAllocationStatus.Unallocated, null), (fixture.Line("inv-1:b").AllocationStatus, fixture.Line("inv-1:b").ProjectId));
        Assert.Equal(2, fixture.Context.XeroDisputeMessages.Count());
        Assert.Equal(new[] { "SetSite:inv-1:a" }, fixture.WriteBack.Calls);

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.HandleAsync(Command(new[] { "inv-1:a" }, XeroAllocationAction.ResolveDispute)));
        Assert.Equal("Only disputed lines can be returned to allocation.", refusal.Message);
    }

    // ---- Builders ------------------------------------------------------------------------------------

    private static SetXeroAllocation Command(
        string[] ids, XeroAllocationAction action, string? projectId = null, string? costCenterCode = null,
        string? bucket = null, string? note = null, IReadOnlyList<XeroCostSplit>? splits = null) =>
        new(ids, action, projectId, costCenterCode, bucket, note, "nigel@jewelbb.co.uk", splits);

    private sealed class Fixture
    {
        public JpmsContext Context { get; }
        public RecordingWriteBack WriteBack { get; } = new();

        private Fixture(JpmsContext context) { Context = context; }

        public static async Task<Fixture> CreateAsync()
        {
            var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
                .UseInMemoryDatabase($"allocation-{Guid.NewGuid():N}").Options);
            context.Projects.Add(new ProjectEntity { ProjectId = "P1", Reference = "JBB-2026-004", Name = "Woodhouse", ClientName = "David Needham" });
            context.Projects.Add(new ProjectEntity { ProjectId = "P2", Reference = "JBB-2026-005", Name = "Abbot Road", ClientName = "A Client" });
            context.CostCenters.Add(new CostCenterEntity { CostCenterId = "CC1", Code = "SUB-GWK", Name = "Groundworks", IsActive = true });
            context.CostCenters.Add(new CostCenterEntity { CostCenterId = "CC2", Code = "SUB-BRK", Name = "Brickwork", IsActive = true });
            context.CostCenters.Add(new CostCenterEntity { CostCenterId = "CC3", Code = "SUB-OLD", Name = "Retired", IsActive = false });
            context.XeroLedgerLines.Add(Line("inv-1:a", "inv-1", "DRAFT", 1000m));
            context.XeroLedgerLines.Add(Line("inv-1:b", "inv-1", "DRAFT", 250m));
            context.XeroLedgerLines.Add(Line("inv-2:a", "inv-2", "AUTHORISED", 80m));
            context.WorkOrderLines.Add(new WorkOrderLineEntity { WorkOrderLineId = "WOL-1", WorkOrderId = "WO-1", Title = "Dig", CostCode = "SUB-GWK" });
            await context.SaveChangesAsync();
            return new Fixture(context);
        }

        private static XeroLedgerLineEntity Line(string id, string invoiceId, string status, decimal net) => new()
        {
            XeroLedgerLineId = id, XeroInvoiceId = invoiceId, XeroLineItemId = id.Split(':')[1], Type = "ACCPAY", InvoiceStatus = status,
            Net = net, InvoiceTotal = net, AmountDue = net, AccountCode = "321", ContactName = "A Supplier",
            FirstSeenAtUtc = DateTimeOffset.UtcNow, LastSyncedAtUtc = DateTimeOffset.UtcNow,
        };

        public async Task LinkToOrderAsync(string lineId, string projectId, string workOrderId)
        {
            Context.XeroLineWorkOrderLinks.Add(new XeroLineWorkOrderLinkEntity { XeroLineWorkOrderLinkId = $"L-{workOrderId}", XeroLedgerLineId = lineId, WorkOrderId = workOrderId, ProjectId = projectId, Amount = 1000m });
            Context.ReconciliationPackageCostLines.Add(new ReconciliationPackageCostLineEntity { ReconciliationPackageCostLineId = $"RP-{workOrderId}", ReconciliationPackageId = "RP1", ProjectId = projectId, XeroLedgerLineId = lineId, Amount = 1000m });
            await Context.SaveChangesAsync();
        }

        public Task<int> HandleAsync(SetXeroAllocation command) =>
            new SetXeroAllocationHandler(Context, WriteBack).HandleAsync(command, CancellationToken.None);

        public XeroLedgerLineEntity Line(string id) => Context.XeroLedgerLines.AsNoTracking().Single(line => line.XeroLedgerLineId == id);
        public List<XeroLedgerLineEntity> Lines(params string[] ids) => ids.Select(Line).ToList();
    }
}
