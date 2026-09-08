using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Api.Features.Xero.Ledger;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// What the write-back leaves on the lines (2026-09-08, the accountant's ask): a failure is
/// stamped with its time and kept through the retry that cures it, and every answer from Xero
/// refreshes the bill's status on the lines without waiting for a sync.
/// </summary>
public sealed class XeroWriteBackServiceTests
{
    [Fact]
    public async Task AFailureIsStampedWithItsTimeAndTheErrorSurvivesTheRetryThatWorks()
    {
        var (context, xero) = await FixtureAsync();
        var service = new XeroWriteBackService(xero, context, NullLogger<XeroWriteBackService>.Instance);
        xero.ApprovalResult = XeroApprovalResult.Failed("Sites option 'By France' is archived");

        var first = await service.RetryAsync("inv-1", CancellationToken.None);

        Assert.False(first.Succeeded);
        var failed = Line(context);
        Assert.Equal(((int)XeroWriteBackStatus.Failed, "Sites option 'By France' is archived", "DRAFT"), (failed.WriteBackStatus, failed.WriteBackError, failed.InvoiceStatus));
        Assert.NotNull(failed.WriteBackFailedAtUtc);

        xero.ApprovalResult = XeroApprovalResult.Ok("AUTHORISED");
        var second = await service.RetryAsync("inv-1", CancellationToken.None);

        Assert.True(second.Succeeded);
        var approved = Line(context);
        Assert.Equal(((int)XeroWriteBackStatus.Approved, "AUTHORISED"), (approved.WriteBackStatus, approved.InvoiceStatus));
        Assert.Equal("Sites option 'By France' is archived", approved.WriteBackError);
        Assert.Equal(failed.WriteBackFailedAtUtc, approved.WriteBackFailedAtUtc);
        Assert.True(approved.WriteBackAtUtc > approved.WriteBackFailedAtUtc);
    }

    [Fact]
    public async Task ASiteWriteThatWorksLiftsTheFailureMarkButKeepsTheErrorAndStampsXerosStatus()
    {
        var (context, xero) = await FixtureAsync();
        var service = new XeroWriteBackService(xero, context, NullLogger<XeroWriteBackService>.Instance);
        var line = await context.XeroLedgerLines.FirstAsync();
        line.AllocationStatus = (int)XeroAllocationStatus.Unallocated;
        line.WriteBackStatus = (int)XeroWriteBackStatus.Failed;
        line.WriteBackError = "Xero was down";
        line.WriteBackFailedAtUtc = DateTimeOffset.UtcNow.AddHours(-1);
        await context.SaveChangesAsync();
        xero.SiteTrackingResult = XeroApprovalResult.Ok("SUBMITTED");

        await service.TrySetSiteAsync(new[] { "inv-1:a" }, CancellationToken.None);

        var after = Line(context);
        Assert.Equal(((int)XeroWriteBackStatus.None, "Xero was down", "SUBMITTED"), (after.WriteBackStatus, after.WriteBackError, after.InvoiceStatus));
        Assert.NotNull(after.WriteBackFailedAtUtc);
    }

    private static XeroLedgerLineEntity Line(JpmsContext context) =>
        context.XeroLedgerLines.AsNoTracking().Single(line => line.XeroLedgerLineId == "inv-1:a");

    private static async Task<(JpmsContext Context, RecordingXero Xero)> FixtureAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"write-back-{Guid.NewGuid():N}").Options);
        context.Projects.Add(new ProjectEntity { ProjectId = "P1", Reference = "JBB-2026-001", Name = "By France", ClientName = "Client", XeroSiteName = "By France" });
        context.XeroLedgerLines.Add(new XeroLedgerLineEntity
        {
            XeroLedgerLineId = "inv-1:a", XeroInvoiceId = "inv-1", XeroLineItemId = "a", Type = "ACCPAY", InvoiceStatus = "DRAFT",
            Net = 100m, InvoiceTotal = 100m, AmountDue = 100m, ContactName = "Super Structures", Description = "Steel",
            AllocationStatus = (int)XeroAllocationStatus.Allocated, ProjectId = "P1", CostCenterCode = "STR-STL",
            FirstSeenAtUtc = DateTimeOffset.UtcNow, LastSyncedAtUtc = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();
        return (context, new RecordingXero());
    }
}
