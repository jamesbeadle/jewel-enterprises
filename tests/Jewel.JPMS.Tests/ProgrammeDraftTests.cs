using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Site.Commands;
using Jewel.JPMS.Api.Features.Site.Drafts;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// The draft programme update end to end over an in-memory database: the capture (saved
// mappings first, rules second, the rest unmapped; one open draft per project), the reviewer's
// remap, and the apply that moves the programme and saves the mappings.
public sealed class ProgrammeDraftTests
{
    private const string Project = "P1";
    private const string Claim = "CLAIM-20";

    [Fact]
    public async Task TheCaptureMapsBySavedMappingThenByRuleAndLeavesTheRestForAHand()
    {
        var context = await Fixture.CreateAsync();

        var draft = await ProgrammeDraftCapture.OpenAsync(context, Project, Claim, "pm@jewelbb.co.uk", CancellationToken.None);
        await context.SaveChangesAsync();

        Assert.NotNull(draft);
        Assert.Equal((int)ProgrammeDraftStatus.Open, draft!.Status);
        Assert.Equal("Valuation 20 - August 2026", draft.ClaimName);
        Assert.Equal("pm@jewelbb.co.uk", draft.CreatedByEmail);

        var detail = await ProgrammeDraftReader.DetailAsync(context, draft, CancellationToken.None);
        Assert.Equal(4, detail.Lines.Count);

        // A saved mapping wins over the title's trade word — the reviewer said so last time.
        var plumbingFirstFloor = LineFor(detail, "T-PLUMB-FF");
        Assert.Equal(ProgrammeMappingSource.Saved, plumbingFirstFloor.MappingSource);
        Assert.Equal(new[] { "MEC-PLM", "MEC-UFH" }, plumbingFirstFloor.CostCodes);
        // £ weighted: plumbing 40,000 of 50,000 + UFH 5,000 of 10,000 = 45,000 of 60,000.
        Assert.Equal(75m, plumbingFirstFloor.ProposedPercent);
        Assert.True(plumbingFirstFloor.IsIncluded);
        Assert.Equal(60m, plumbingFirstFloor.CurrentPercent);

        // The rulebook reads the title.
        var plumbingSecondFloor = LineFor(detail, "T-PLUMB-SF");
        Assert.Equal(ProgrammeMappingSource.Rule, plumbingSecondFloor.MappingSource);
        Assert.Equal(new[] { "MEC-PLM" }, plumbingSecondFloor.CostCodes);
        Assert.Equal(80m, plumbingSecondFloor.ProposedPercent);
        Assert.Contains("MEC-PLM Plumber", plumbingSecondFloor.Evidence);

        // A family narrows to what the claim prices.
        var windows = LineFor(detail, "T-WINDOWS");
        Assert.Equal(new[] { "WDR-ALU" }, windows.CostCodes);
        Assert.Equal(100m, windows.ProposedPercent);

        // Nothing matched: no proposal, not included, waiting for Claude or a person.
        var gate = LineFor(detail, "T-GATE");
        Assert.Equal(ProgrammeMappingSource.None, gate.MappingSource);
        Assert.Empty(gate.CostCodes);
        Assert.Null(gate.ProposedPercent);
        Assert.False(gate.IsIncluded);

        // The claim's centres travel with the draft for the mapping editor.
        Assert.Equal(new[] { "MEC-PLM", "MEC-UFH", "WDR-ALU" }, detail.CostCentres.Select(centre => centre.CostCode));
        Assert.Equal(3, detail.MappedCount);
        Assert.Equal(1, detail.UnmappedCount);
    }

    [Fact]
    public async Task OpeningASecondDraftSupersedesTheOpenOne()
    {
        var context = await Fixture.CreateAsync();
        var first = await ProgrammeDraftCapture.OpenAsync(context, Project, Claim, "pm@jewelbb.co.uk", CancellationToken.None);
        await context.SaveChangesAsync();

        var second = await ProgrammeDraftCapture.OpenAsync(context, Project, Claim, "fd@jewelbb.co.uk", CancellationToken.None);
        await context.SaveChangesAsync();

        var firstAgain = await context.ProgrammeDrafts.SingleAsync(row => row.ProgrammeDraftId == first!.ProgrammeDraftId);
        Assert.Equal((int)ProgrammeDraftStatus.Superseded, firstAgain.Status);
        Assert.Equal("fd@jewelbb.co.uk", firstAgain.ResolvedByEmail);
        Assert.NotNull(firstAgain.ResolvedAt);

        var open = await ProgrammeDraftReader.OpenForProjectAsync(context, Project, CancellationToken.None);
        Assert.Equal(second!.ProgrammeDraftId, open!.Draft.ProgrammeDraftId);
    }

    [Fact]
    public async Task NothingToDraftAnswersNull()
    {
        var context = await Fixture.CreateAsync();

        Assert.Null(await ProgrammeDraftCapture.OpenAsync(context, Project, "NO-SUCH-CLAIM", "pm@jewelbb.co.uk", CancellationToken.None));
        Assert.Null(await ProgrammeDraftCapture.OpenAsync(context, "P-EMPTY", Claim, "pm@jewelbb.co.uk", CancellationToken.None));
    }

    [Fact]
    public async Task TheReviewerCanRemapALineAndOverrideTheFigure()
    {
        var context = await Fixture.CreateAsync();
        var draft = await ProgrammeDraftCapture.OpenAsync(context, Project, Claim, "pm@jewelbb.co.uk", CancellationToken.None);
        await context.SaveChangesAsync();
        var gate = await context.ProgrammeDraftLines.SingleAsync(line => line.ProgrammeTaskId == "T-GATE");
        var handler = new ReviewProgrammeDraftLineHandler(context);

        // Mapping the gate to the windows centre gives it the windows figure and a Person source.
        var remapped = await handler.HandleAsync(
            new ReviewProgrammeDraftLine(gate.ProgrammeDraftLineId, new[] { "WDR-ALU" }, IsIncluded: true, ReviewedPercent: null), CancellationToken.None);
        Assert.Equal(ProgrammeMappingSource.Person, remapped.MappingSource);
        Assert.Equal(100m, remapped.ProposedPercent);
        Assert.True(remapped.IsIncluded);

        // The reviewer's own figure rides over the proposal; the mapping is untouched.
        var overridden = await handler.HandleAsync(
            new ReviewProgrammeDraftLine(gate.ProgrammeDraftLineId, new[] { "WDR-ALU" }, IsIncluded: true, ReviewedPercent: 40m), CancellationToken.None);
        Assert.Equal(ProgrammeMappingSource.Person, overridden.MappingSource);
        Assert.Equal(40m, overridden.PercentToApply);

        // Unticking keeps everything else.
        var unticked = await handler.HandleAsync(
            new ReviewProgrammeDraftLine(gate.ProgrammeDraftLineId, new[] { "WDR-ALU" }, IsIncluded: false, ReviewedPercent: 40m), CancellationToken.None);
        Assert.False(unticked.IsIncluded);
        Assert.Equal(40m, unticked.ReviewedPercent);
    }

    [Fact]
    public async Task ApplyingMovesTheIncludedTasksAndSavesTheMappings()
    {
        var context = await Fixture.CreateAsync();
        var draft = await ProgrammeDraftCapture.OpenAsync(context, Project, Claim, "pm@jewelbb.co.uk", CancellationToken.None);
        await context.SaveChangesAsync();

        // Untick the second-floor plumbing so it stays where it is.
        var secondFloor = await context.ProgrammeDraftLines.SingleAsync(line => line.ProgrammeTaskId == "T-PLUMB-SF");
        await new ReviewProgrammeDraftLineHandler(context).HandleAsync(
            new ReviewProgrammeDraftLine(secondFloor.ProgrammeDraftLineId, new[] { "MEC-PLM" }, IsIncluded: false, ReviewedPercent: null), CancellationToken.None);

        var applied = await new ApplyProgrammeDraftHandler(context).HandleAsync(
            new ApplyProgrammeDraft(draft!.ProgrammeDraftId, "pm@jewelbb.co.uk"), CancellationToken.None);

        Assert.Equal(ProgrammeDraftStatus.Applied, applied.Status);
        Assert.Equal("pm@jewelbb.co.uk", applied.ResolvedByEmail);

        var tasks = await context.ProgrammeTasks.ToDictionaryAsync(task => task.ProgrammeTaskId);
        Assert.Equal(75m, tasks["T-PLUMB-FF"].ProgressPercent);
        Assert.Equal(20m, tasks["T-PLUMB-SF"].ProgressPercent);   // unticked: untouched
        Assert.Equal(100m, tasks["T-WINDOWS"].ProgressPercent);
        Assert.Equal(0m, tasks["T-GATE"].ProgressPercent);        // unmapped: untouched

        // Mappings are saved per task, replacing what was there — and the unticked line's too:
        // the reviewer confirmed the mapping even if not the move.
        var mappings = await context.ProgrammeTaskCostCentres.ToListAsync();
        Assert.Equal(new[] { "MEC-PLM", "MEC-UFH" }, mappings.Where(m => m.ProgrammeTaskId == "T-PLUMB-FF").Select(m => m.CostCode).OrderBy(code => code));
        Assert.Equal(new[] { "MEC-PLM" }, mappings.Where(m => m.ProgrammeTaskId == "T-PLUMB-SF").Select(m => m.CostCode));
        Assert.Equal(new[] { "WDR-ALU" }, mappings.Where(m => m.ProgrammeTaskId == "T-WINDOWS").Select(m => m.CostCode));
        Assert.Empty(mappings.Where(m => m.ProgrammeTaskId == "T-GATE"));

        // Applied is closed: no draft is awaiting review, and applying again is refused.
        Assert.Null(await ProgrammeDraftReader.OpenForProjectAsync(context, Project, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ApplyProgrammeDraftHandler(context).HandleAsync(
            new ApplyProgrammeDraft(draft.ProgrammeDraftId, "pm@jewelbb.co.uk"), CancellationToken.None));
    }

    [Fact]
    public async Task TheNextDraftStartsFromTheSavedMappings()
    {
        var context = await Fixture.CreateAsync();
        var first = await ProgrammeDraftCapture.OpenAsync(context, Project, Claim, "pm@jewelbb.co.uk", CancellationToken.None);
        await context.SaveChangesAsync();
        var gate = await context.ProgrammeDraftLines.SingleAsync(line => line.ProgrammeTaskId == "T-GATE");
        await new ReviewProgrammeDraftLineHandler(context).HandleAsync(
            new ReviewProgrammeDraftLine(gate.ProgrammeDraftLineId, new[] { "WDR-ALU" }, IsIncluded: true, ReviewedPercent: null), CancellationToken.None);
        await new ApplyProgrammeDraftHandler(context).HandleAsync(new ApplyProgrammeDraft(first!.ProgrammeDraftId, "pm@jewelbb.co.uk"), CancellationToken.None);

        var second = await ProgrammeDraftCapture.OpenAsync(context, Project, Claim, "pm@jewelbb.co.uk", CancellationToken.None);
        await context.SaveChangesAsync();
        var detail = await ProgrammeDraftReader.DetailAsync(context, second!, CancellationToken.None);

        var gateAgain = LineFor(detail, "T-GATE");
        Assert.Equal(ProgrammeMappingSource.Saved, gateAgain.MappingSource);
        Assert.Equal(new[] { "WDR-ALU" }, gateAgain.CostCodes);
        Assert.Equal(0, detail.UnmappedCount);
    }

    [Fact]
    public async Task DiscardingClosesTheDraftAndLeavesTheProgrammeAlone()
    {
        var context = await Fixture.CreateAsync();
        var draft = await ProgrammeDraftCapture.OpenAsync(context, Project, Claim, "pm@jewelbb.co.uk", CancellationToken.None);
        await context.SaveChangesAsync();

        var discarded = await new DiscardProgrammeDraftHandler(context).HandleAsync(
            new DiscardProgrammeDraft(draft!.ProgrammeDraftId, "pm@jewelbb.co.uk"), CancellationToken.None);

        Assert.Equal(ProgrammeDraftStatus.Discarded, discarded.Status);
        Assert.Equal(60m, (await context.ProgrammeTasks.SingleAsync(task => task.ProgrammeTaskId == "T-PLUMB-FF")).ProgressPercent);
        // Only the fixture's own saved pair — nothing the draft proposed was kept.
        Assert.Equal(2, await context.ProgrammeTaskCostCentres.CountAsync());
        Assert.Null(await ProgrammeDraftReader.OpenForProjectAsync(context, Project, CancellationToken.None));
    }

    private static ProgrammeDraftLine LineFor(ProgrammeDraftDetail detail, string programmeTaskId) =>
        Assert.Single(detail.Lines, line => line.ProgrammeTaskId == programmeTaskId);

    // ---- A small By France: three centres, four tasks, one saved mapping ---------------------

    private static class Fixture
    {
        public static async Task<JpmsContext> CreateAsync()
        {
            var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
                .UseInMemoryDatabase($"programme-draft-{Guid.NewGuid():N}").Options);

            context.Projects.Add(new ProjectEntity { ProjectId = Project, Reference = "JBB-2026-001", Name = "By France", ClientName = "A Client" });
            context.Projects.Add(new ProjectEntity { ProjectId = "P-EMPTY", Reference = "JBB-2026-009", Name = "Empty", ClientName = "A Client" });

            context.CostCenters.Add(new CostCenterEntity { CostCenterId = "CC1", Code = "MEC-PLM", Name = "Plumber", IsActive = true });
            context.CostCenters.Add(new CostCenterEntity { CostCenterId = "CC2", Code = "MEC-UFH", Name = "Under-floor heating", IsActive = true });
            context.CostCenters.Add(new CostCenterEntity { CostCenterId = "CC3", Code = "WDR-ALU", Name = "Aluminium window and doors", IsActive = true });

            context.ValuationClaims.Add(new ValuationClaimEntity
            {
                ValuationClaimId = Claim, ProjectId = Project, ClaimNumber = 3, Name = "Valuation 20 - August 2026",
                ClaimDate = new DateTimeOffset(2026, 8, 25, 0, 0, 0, TimeSpan.Zero), Status = (int)ValuationClaimStatus.Preapproved
            });

            AddLine(context, "L-PLM-1", "MEC-PLM", 30_000m, claimed: 30_000m);
            AddLine(context, "L-PLM-2", "MEC-PLM", 20_000m, claimed: 10_000m);
            AddLine(context, "L-UFH", "MEC-UFH", 10_000m, claimed: 5_000m);
            AddLine(context, "L-WIN", "WDR-ALU", 80_000m, claimed: 80_000m);
            // An omit never takes part, whatever it claims.
            AddLine(context, "L-OMIT", "WDR-ALU", -20_000m, claimed: -20_000m, lineType: ValuationLineType.Omit);

            AddTask(context, "T-PLUMB-FF", "First Floor — Plumbing 2nd Fix", 60m, new DateTime(2026, 4, 6));
            AddTask(context, "T-PLUMB-SF", "Second Floor — Plumbing 2nd Fix", 20m, new DateTime(2026, 3, 16));
            AddTask(context, "T-WINDOWS", "Second Floor — Window Installation TBC", 50m, new DateTime(2026, 2, 23));
            AddTask(context, "T-GATE", "External Works — Entrance Gate Survey - Install TBC", 0m, new DateTime(2026, 4, 20));

            // The reviewer confirmed on an earlier draft that first-floor plumbing includes the UFH.
            context.ProgrammeTaskCostCentres.Add(new ProgrammeTaskCostCentreEntity { ProgrammeTaskCostCentreId = "M1", ProjectId = Project, ProgrammeTaskId = "T-PLUMB-FF", CostCode = "MEC-PLM" });
            context.ProgrammeTaskCostCentres.Add(new ProgrammeTaskCostCentreEntity { ProgrammeTaskCostCentreId = "M2", ProjectId = Project, ProgrammeTaskId = "T-PLUMB-FF", CostCode = "MEC-UFH" });

            await context.SaveChangesAsync();
            return context;
        }

        private static void AddLine(JpmsContext context, string id, string costCode, decimal amount, decimal claimed,
            ValuationLineType lineType = ValuationLineType.Priced)
        {
            context.ValuationLineItems.Add(new ValuationLineItemEntity
            {
                ValuationLineItemId = id, ProjectId = Project, ElementType = (int)ValuationElementType.ContractWorks,
                SectionCode = "R10", SectionName = "Services", LineType = (int)lineType, CostCode = costCode,
                Description = $"Line {id}", Unit = "item", Quantity = 1m, Rate = amount, LineAmount = amount
            });
            context.ClaimLines.Add(new ClaimLineEntity
            {
                ClaimLineId = $"CL-{id}", ValuationClaimId = Claim, ValuationLineItemId = id,
                PercentComplete = amount == 0m ? 0m : claimed / amount * 100m, CumulativeClaimed = claimed, PeriodIncrement = claimed
            });
        }

        private static void AddTask(JpmsContext context, string id, string title, decimal progress, DateTime start) =>
            context.ProgrammeTasks.Add(new ProgrammeTaskEntity
            {
                ProgrammeTaskId = id, ProjectId = Project, Title = title, ProgressPercent = progress,
                PlannedStart = new DateTimeOffset(start, TimeSpan.Zero), PlannedEnd = new DateTimeOffset(start.AddDays(14), TimeSpan.Zero)
            });
    }
}
