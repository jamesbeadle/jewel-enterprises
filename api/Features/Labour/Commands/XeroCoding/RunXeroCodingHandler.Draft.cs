using Jewel.JPMS.Api.Features.Xero;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

public sealed partial class RunXeroCodingHandler
{
    /// <summary>No bill anywhere → stage a draft matching the schedule (the exception, item F) —
    /// one draft for the party, so a company that bills several workers on one invoice gets one
    /// draft carrying every worker's lines, exactly as its real invoice will.</summary>
    private async Task<IReadOnlyList<XeroCodingRunResult>> StageDraftAsync(
        CodingParty party, List<CodedLine> codedLines, CancellationToken cancellationToken)
    {
        var contactName = PreferredContactName(party);
        var monthEndDate = party.MonthStart.AddMonths(1).AddDays(-1).UtcDateTime.Date;
        var reference = $"JPMS labour {party.MonthStart:MMM yyyy} — {party.Owner}";
        var xeroLines = codedLines.Select(coded => coded.Line).ToList();
        var forWorkers = party.IsCompanyBill ? $" for {party.Workers.Count} workers ({party.WorkerNames})" : "";
        if (party.DryRun)
            return party.Each(worker => worker.Outcome(XeroCodingOutcome.WouldStageDraft,
                $"No bill from {contactName} for {party.MonthStart:MMM yyyy} in Xero — would stage a DRAFT bill "
                + $"\"{reference}\" dated {monthEndDate:dd MMM yyyy}: {xeroLines.Count} line(s), net £{party.ScheduleTotal:N2}{forWorkers} "
                + "(VAT per the contact's default in Xero, never assumed). " + WorkersLinesSummary(party, worker, codedLines)));

        var create = await xero.CreateDraftBillAsync(new XeroDraftBillRequest(
            contactName, monthEndDate, monthEndDate.AddDays(30), reference, xeroLines), cancellationToken);
        return create.Succeeded
            ? party.Each(worker => worker.Outcome(XeroCodingOutcome.DraftStaged,
                $"Draft bill \"{reference}\" staged for {contactName} with {xeroLines.Count} line(s), net £{party.ScheduleTotal:N2}{forWorkers}. "
                + (create.Note ?? "") + " Reconcile when the real invoice lands.",
                create.FreshStatus ?? ""))
            : party.Each(worker => worker.Failed(create.Error ?? "Xero refused the draft bill."));
    }

    /// <summary>The contact Xero already holds for the party (their latest bill's contact name),
    /// else the settlement name the schedule carries.</summary>
    private string PreferredContactName(CodingParty party)
    {
        var known = knownContacts.FirstOrDefault(contact => IsOneOf(contact, party.ContactNames));
        return known ?? party.CounterpartyName;
    }
}
