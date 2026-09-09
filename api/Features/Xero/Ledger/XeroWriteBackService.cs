using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

/// <summary>
/// Decides when an invoice's allocation is confirmed back to Xero and records what
/// happened. An invoice is written back once EVERY stored line of it is Allocated
/// (a project + cost centre(s) — bucketed/ignored/disputed lines block
/// auto-approval, since approving a bill whose lines were waved through
/// unallocated — or are actively contested — defeats the point)
/// AND the invoice is still awaiting approval in Xero (DRAFT/SUBMITTED). Invoices
/// approved outside JPMS keep flowing through allocation portal-side only.
///
/// Write-back is best-effort by design: the JPMS allocation always saves first and
/// survives regardless; a failed write-back is stamped on the lines (visible on the
/// allocation page) and can be retried without re-allocating.
/// </summary>
public interface IXeroWriteBackService
{
    /// <summary>Attempts write-back for each invoice that has just become eligible. Never throws.</summary>
    Task TryWriteBackAsync(IReadOnlyCollection<string> xeroInvoiceIds, CancellationToken ct);

    /// <summary>Explicit re-attempt for one invoice (the allocation page's Retry button).</summary>
    Task<XeroWriteBackOutcome> RetryAsync(string xeroInvoiceId, CancellationToken ct);

    /// <summary>
    /// Best-effort Sites-tracking write for lines whose project was just set or
    /// moved — no approval. Covers the SetProject half-step on queued lines AND
    /// a re-allocation that moves an approved bill's line between projects
    /// (decision 2026-08-14: a change of mind after approval follows through to
    /// Xero). Never throws; a failure is stamped on the affected lines only
    /// (WriteBackStatus Failed, so the page can flag it), and pressing Set /
    /// re-sending is the retry. Paid bills are skipped silently — Xero locks
    /// their lines once payments are applied, so those move portal-side only.
    /// </summary>
    Task TrySetSiteAsync(IReadOnlyCollection<string> xeroLedgerLineIds, CancellationToken ct);

    /// <summary>
    /// The Work Order bill approval's write (2026-09-08): the same tracking + approval as
    /// <see cref="TryWriteBackAsync"/>, reported back to the caller — and, because Xero never
    /// un-approves, a bill re-approved after an undo is still AUTHORISED there, so an approved
    /// bill with nothing paid against it takes the tracking rewrite too. Never throws.
    /// </summary>
    Task<XeroWriteBackOutcome> WriteBackWorkOrderBillAsync(string xeroInvoiceId, CancellationToken ct);

    /// <summary>
    /// The undo's write (2026-09-08): strips Sites and Cost Code tracking off every line of the
    /// bill in Xero, status untouched. Never throws; the outcome carries Xero's answer and the
    /// bill's status as Xero reported it, so the caller can say plainly that an approved bill
    /// stays approved.
    /// </summary>
    Task<XeroTrackingClearOutcome> TryClearTrackingAsync(string xeroInvoiceId, CancellationToken ct);
}

/// <summary>What the tracking clear did; XeroStatus is the bill's status as Xero reported it ("" when unknown).</summary>
public sealed record XeroTrackingClearOutcome(bool Succeeded, string? Error, string XeroStatus);

public sealed partial class XeroWriteBackService : IXeroWriteBackService
{
    // Only invoices still awaiting approval in Xero are written back.
    private static readonly string[] AwaitingApprovalStatuses = { "DRAFT", "SUBMITTED" };

    private readonly IXeroClient xero;
    private readonly JpmsContext context;
    private readonly ILogger<XeroWriteBackService> logger;

    public XeroWriteBackService(IXeroClient xero, JpmsContext context, ILogger<XeroWriteBackService> logger)
    {
        this.xero = xero;
        this.context = context;
        this.logger = logger;
    }

    public async Task TryWriteBackAsync(IReadOnlyCollection<string> xeroInvoiceIds, CancellationToken ct)
    {
        foreach (var invoiceId in xeroInvoiceIds.Distinct())
        {
            try
            {
                await WriteBackInvoiceAsync(invoiceId, explicitRetry: false, ct);
            }
            catch (Exception unexpected)
            {
                // Never let a write-back hiccup undo or fail the allocation itself.
                logger.LogError(unexpected, "Xero write-back failed unexpectedly for invoice {InvoiceId}.", invoiceId);
            }
        }
    }

    public async Task<XeroWriteBackOutcome> RetryAsync(string xeroInvoiceId, CancellationToken ct)
    {
        try
        {
            return await WriteBackInvoiceAsync(xeroInvoiceId, explicitRetry: true, ct);
        }
        catch (Exception unexpected)
        {
            logger.LogError(unexpected, "Xero write-back retry failed unexpectedly for invoice {InvoiceId}.", xeroInvoiceId);
            return new XeroWriteBackOutcome(false, unexpected.Message);
        }
    }

    public async Task TrySetSiteAsync(IReadOnlyCollection<string> xeroLedgerLineIds, CancellationToken ct)
    {
        List<XeroLedgerLineEntity> lines;
        try
        {
            // Lines with a project get its site stamped; lines whose project was just
            // cleared (unset) get their Site tracking removed from the draft bill.
            var ids = xeroLedgerLineIds.Distinct().ToList();
            lines = await context.XeroLedgerLines
                .Where(line => ids.Contains(line.XeroLedgerLineId))
                .ToListAsync(ct);
        }
        catch (Exception unexpected)
        {
            logger.LogError(unexpected, "Loading lines for a Xero site-tracking write failed unexpectedly.");
            return;
        }

        foreach (var invoiceLines in lines.GroupBy(line => line.XeroInvoiceId))
        {
            try
            {
                await SetSiteForInvoiceLinesAsync(invoiceLines.ToList(), ct);
            }
            catch (Exception unexpected)
            {
                // Same contract as TryWriteBackAsync: a Xero hiccup never undoes the save.
                logger.LogError(unexpected, "Xero site-tracking write failed unexpectedly for invoice {InvoiceId}.",
                    invoiceLines.Key);
            }
        }
    }

    private async Task SetSiteForInvoiceLinesAsync(List<XeroLedgerLineEntity> lines, CancellationToken ct)
    {
        // Draft, submitted AND approved bills carry the update — moving a cost between
        // sites is legitimate after approval (decision 2026-08-14), and the client
        // re-reads the live status before writing anyway. Only paid bills stay
        // portal-side, silently: Xero locks their lines once payments are applied.
        if (lines[0].InvoiceStatus.Equals("PAID", StringComparison.OrdinalIgnoreCase))
            return;

        var projectIds = lines.Where(line => line.ProjectId is not null)
            .Select(line => line.ProjectId!).Distinct().ToList();
        var projects = await context.Projects
            .Where(project => projectIds.Contains(project.ProjectId))
            .ToDictionaryAsync(project => project.ProjectId, ct);
        var unmapped = projectIds
            .Where(id => !projects.TryGetValue(id, out var project) || string.IsNullOrWhiteSpace(project.XeroSiteName))
            .Select(id => projects.TryGetValue(id, out var project) ? project.Name : id)
            .ToList();
        if (unmapped.Count > 0)
        {
            await StampFailureAsync(lines,
                "No Xero site is mapped for " + string.Join(", ", unmapped)
                + " — set \"Xero site (tracking option)\" in the project's details, then press Set again.", ct);
            return;
        }

        // A null SiteOption removes the line's Sites tracking (the unset case).
        var result = await xero.SetSiteTrackingAsync(new XeroSiteTrackingRequest(
            lines[0].XeroInvoiceId,
            lines[0].Type == "ACCPAYCREDIT",
            lines.Select(line => new XeroSiteTrackingLine(
                line.XeroLineItemId,
                line.ProjectId is null ? null : projects[line.ProjectId].XeroSiteName!)).ToList()), ct);

        if (result.Succeeded)
        {
            // Lift any earlier failure mark; these lines are queued, so their write-back story
            // goes back to None (not Approved — nothing was approved here). The last error and
            // when it failed stay on the line (2026-09-08): a failure must not vanish the moment
            // a later write works. Xero's fresh status is stamped too, so the ledger reads what
            // Xero last said without waiting for a sync.
            var changed = StampXeroStatus(lines, result.FreshStatus);
            foreach (var line in lines.Where(line => line.WriteBackStatus == (int)XeroWriteBackStatus.Failed))
            {
                line.WriteBackStatus = (int)XeroWriteBackStatus.None;
                line.WriteBackAtUtc = null;
                changed = true;
            }
            if (changed) await context.SaveChangesAsync(ct);
            logger.LogInformation("Xero site tracking set for {LineCount} line(s) of invoice {InvoiceId}{Skipped}.",
                lines.Count, lines[0].XeroInvoiceId, result.AlreadyApproved ? " (paid in Xero — skipped)" : "");
            return;
        }

        await StampFailureAsync(lines, result.Error ?? "Xero rejected the tracking update.", ct);
    }

    private async Task<XeroWriteBackOutcome> WriteBackInvoiceAsync(
        string invoiceId, bool explicitRetry, CancellationToken ct, bool recodeApproved = false, bool keepLinesWhole = false)
    {
        var lines = await context.XeroLedgerLines
            .Where(line => line.XeroInvoiceId == invoiceId)
            .ToListAsync(ct);
        if (lines.Count == 0)
            return new XeroWriteBackOutcome(false, "No stored ledger lines for this invoice.");

        var status = lines[0].InvoiceStatus;
        var takesApprovedRecode = recodeApproved && status.Equals("AUTHORISED", StringComparison.OrdinalIgnoreCase);
        if (!AwaitingApprovalStatuses.Contains(status, StringComparer.OrdinalIgnoreCase) && !takesApprovedRecode)
        {
            // Approved/paid outside JPMS: the allocation stays portal-side, nothing to do —
            // a retry that races an out-of-band approval is a success, not an error.
            if (status.Equals("AUTHORISED", StringComparison.OrdinalIgnoreCase)
                || status.Equals("PAID", StringComparison.OrdinalIgnoreCase))
                return new XeroWriteBackOutcome(true, null);
            return new XeroWriteBackOutcome(!explicitRetry, explicitRetry
                ? $"The invoice is {status} in Xero — only draft or submitted bills are approved from here."
                : null);
        }

        if (lines.Any(line => line.AllocationStatus != (int)XeroAllocationStatus.Allocated))
        {
            // Not ready (other lines still queued, or bucketed/ignored). Silent for the
            // automatic path — this is the normal state between allocating line 1 and line N.
            return new XeroWriteBackOutcome(false, explicitRetry
                ? "Every line of the bill must be allocated to a project and cost centre before it can be approved in Xero."
                : null);
        }

        // Splits: whole-line allocations carry their project + code on the line itself;
        // split lines carry rows in XeroCostSplits, each with its own project.
        var lineIds = lines.Select(line => line.XeroLedgerLineId).ToList();
        var splitsByLine = (await context.XeroCostSplits
                .Where(split => lineIds.Contains(split.XeroLedgerLineId))
                .ToListAsync(ct))
            .GroupBy(split => split.XeroLedgerLineId)
            .ToDictionary(group => group.Key, group => group.ToList());

        // Resolve every referenced project → Xero Sites option; explicit mapping, no guessing.
        var projectIds = lines.Select(line => line.ProjectId)
            .Concat(splitsByLine.Values.SelectMany(rows => rows).Select(row => (string?)row.ProjectId))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct()
            .ToList();
        var projects = await context.Projects
            .Where(project => projectIds.Contains(project.ProjectId))
            .ToDictionaryAsync(project => project.ProjectId, ct);
        var unmapped = projectIds
            .Where(id => !projects.TryGetValue(id, out var project) || string.IsNullOrWhiteSpace(project.XeroSiteName))
            .Select(id => projects.TryGetValue(id, out var project) ? project.Name : id)
            .ToList();
        if (unmapped.Count > 0)
            return await StampFailureAsync(lines,
                "No Xero site is mapped for " + string.Join(", ", unmapped)
                + " — set \"Xero site (tracking option)\" in the project's details, then retry.", ct);

        var instructions = new List<XeroApprovalLineInstruction>();
        foreach (var line in lines)
        {
            List<XeroApprovalShare>? shares;
            if (splitsByLine.TryGetValue(line.XeroLedgerLineId, out var rows))
            {
                shares = rows
                    .Select(row => new XeroApprovalShare(projects[row.ProjectId].XeroSiteName!, row.CostCenterCode, row.Net))
                    .ToList();
                // A Work Order bill never splits a Xero line (2026-09-09, the accountant's ask —
                // the lines are the supplier's CIS split, left as raised): the line is stamped
                // whole with the centre carrying most of it; the exact split stays portal-side.
                if (keepLinesWhole && shares.Count > 1)
                    shares = new List<XeroApprovalShare> { shares.OrderByDescending(share => share.Net).First() with { Net = line.Net } };
            }
            else if (line.ProjectId is not null && line.CostCenterCode is not null)
            {
                shares = new List<XeroApprovalShare>
                {
                    new(projects[line.ProjectId].XeroSiteName!, line.CostCenterCode, line.Net)
                };
            }
            else
            {
                shares = null;
            }

            if (shares is null or { Count: 0 })
                return await StampFailureAsync(lines,
                    $"Line \"{line.Description}\" is allocated but carries no project + cost centre — re-allocate it.", ct);
            instructions.Add(new XeroApprovalLineInstruction(line.XeroLineItemId, shares));
        }

        var result = await xero.ApproveInvoiceAsync(
            new XeroApprovalRequest(invoiceId, lines[0].Type == "ACCPAYCREDIT", instructions, recodeApproved), ct);

        var now = DateTimeOffset.UtcNow;
        if (result.Succeeded)
        {
            // WriteBackError and WriteBackFailedAtUtc are deliberately left as they are: the
            // line now reads "approved — after a failed attempt at …" rather than forgetting.
            foreach (var line in lines)
            {
                line.WriteBackStatus = (int)XeroWriteBackStatus.Approved;
                line.WriteBackAtUtc = now;
                // Reflect the approval immediately; the next sync re-confirms from Xero.
                line.InvoiceStatus = result.FreshStatus ?? "AUTHORISED";
            }
            await context.SaveChangesAsync(ct);
            logger.LogInformation("Xero invoice {InvoiceId} approved with tracking for {LineCount} lines{Already}.",
                invoiceId, lines.Count, result.AlreadyApproved ? " (already approved in Xero)" : "");
            return new XeroWriteBackOutcome(true, null);
        }

        return await StampFailureAsync(lines, result.Error ?? "Xero rejected the update.", ct);
    }

    private async Task<XeroWriteBackOutcome> StampFailureAsync(
        List<XeroLedgerLineEntity> lines, string error, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var line in lines)
        {
            line.WriteBackStatus = (int)XeroWriteBackStatus.Failed;
            line.WriteBackError = error.Length <= 1024 ? error : error[..1024];
            line.WriteBackAtUtc = now;
            line.WriteBackFailedAtUtc = now;
        }
        await context.SaveChangesAsync(ct);
        logger.LogWarning("Xero write-back failed for invoice {InvoiceId}: {Error}", lines[0].XeroInvoiceId, error);
        return new XeroWriteBackOutcome(false, error);
    }

    /// <summary>The bill's status as Xero just reported it, onto every stored line; false when Xero said nothing new.</summary>
    private static bool StampXeroStatus(List<XeroLedgerLineEntity> lines, string? freshStatus)
    {
        if (string.IsNullOrWhiteSpace(freshStatus)) return false;
        var changed = false;
        foreach (var line in lines.Where(line => !line.InvoiceStatus.Equals(freshStatus, StringComparison.OrdinalIgnoreCase)))
        {
            line.InvoiceStatus = freshStatus;
            changed = true;
        }
        return changed;
    }
}
