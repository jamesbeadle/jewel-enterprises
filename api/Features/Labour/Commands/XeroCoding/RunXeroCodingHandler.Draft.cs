using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.Labour;
using static Jewel.JPMS.Api.Features.Labour.Commands.XeroCodingWording;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

public sealed partial class RunXeroCodingHandler
{
    /// <summary>No bill anywhere → stage a draft matching the schedule (the exception, item F).</summary>
    private async Task<XeroCodingRunResult> StageDraftAsync(
        WorkerRun run, List<XeroScheduleLine> xeroLines, string preface, CancellationToken cancellationToken)
    {
        var schedule = run.Schedule;
        var contactName = PreferredContactName(schedule);
        var monthEndDate = run.MonthStart.AddMonths(1).AddDays(-1).UtcDateTime.Date;
        var reference = $"JPMS labour {run.MonthStart:MMM yyyy} — {schedule.WorkerName}";
        if (run.DryRun)
            return run.Outcome(XeroCodingOutcome.WouldStageDraft,
                preface + $"No bill from {contactName} for {run.MonthStart:MMM yyyy} in Xero — would stage a DRAFT bill "
                + $"\"{reference}\" dated {monthEndDate:dd MMM yyyy}: {xeroLines.Count} line(s), net £{schedule.GrossTotal:N2} "
                + "(VAT per the contact's default in Xero, never assumed). " + LinesSummary(xeroLines));

        var create = await xero.CreateDraftBillAsync(new XeroDraftBillRequest(
            contactName, monthEndDate, monthEndDate.AddDays(30), reference, xeroLines), cancellationToken);
        return create.Succeeded
            ? run.Outcome(XeroCodingOutcome.DraftStaged,
                preface + $"Draft bill \"{reference}\" staged for {contactName} with {xeroLines.Count} line(s), net £{schedule.GrossTotal:N2}. "
                + (create.Note ?? "") + " Reconcile when the real invoice lands.",
                create.FreshStatus ?? "")
            : run.Failed(preface + (create.Error ?? "Xero refused the draft bill."));
    }

    /// <summary>The contact Xero already holds for the worker (their latest bill's contact name),
    /// else the settlement name the schedule carries.</summary>
    private string PreferredContactName(WorkerSettlementSchedule schedule)
    {
        var names = new[] { schedule.WorkerName, schedule.SubcontractorName }
            .Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().ToList();
        var known = knownContacts.FirstOrDefault(contact => names.Any(name => WorkerDirectoryMatcher.Matches(contact, name)));
        return known ?? schedule.SubcontractorName;
    }
}
