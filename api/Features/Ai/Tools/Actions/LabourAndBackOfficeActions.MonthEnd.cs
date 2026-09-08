using Jewel.JPMS.Api.Features.Labour;
using Jewel.JPMS.Api.Features.Labour.Commands;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class LabourAndBackOfficeActions
{
    // The whole month-end chain from the connector (2026-08-31, the accountant's ask): sign off
    // → preview the coding → run the coding → (a draft the run staged is approved by a human in
    // Xero) → post any variance. Since 2026-09-03 the run's normal path is to recode the
    // worker's EXISTING authorised bill (the cover route) and re-point the cover itself;
    // view_settlement_month is the read that pairs with these.
    private static IEnumerable<AiAction> MonthEndActions() => new AiAction[]
    {
        new AiAction(
            Name: "sign_off_labour_week",
            Area: "Labour",
            Description: "Places the weekly sign-off marker on a worker's week — the Labour "
                + "overview's Sign off, by name. Sign-off freezes the week for settlement: only "
                + "fully signed-off worker-months are written to Xero by run_xero_coding. The "
                + "server re-checks the signable rule at the moment of signing — every elapsed "
                + "day must be approved, rejected or recorded as absence — and refuses with the "
                + "reason when it fails. A week that straddles a month end signs off PER MONTH: "
                + "its old-month days on their own (so the old month settles on the 1st) and its "
                + "new-month days later. Touches no timesheet; sign-off is a marker over "
                + "approval, never a second state machine.",
            CommandType: typeof(SignOffWorkerWeekByName),
            ResultType: typeof(LabourWeekSignOff),
            AuthorisationType: typeof(SignOffWorkerWeekByNameAuthorisation),
            ValidationType: typeof(SignOffWorkerWeekByNameValidation),
            VisibleTo: LabourRoleSets.ApproveTimesheets,
            EmailStamps: new[] { "SignedOffByEmail" },
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "workerName as the user says it; weekStart is any date in the week wanted "
                + "(normalised to that week's Monday). monthStart names the month whose part of "
                + "the week to sign — any date in that month — and matters only when the week "
                + "straddles a month end (view_worker_month marks those with monthPart); left "
                + "out, it is the month of the weekStart date you pass, so 31 Aug signs August's "
                + "days of that week and 1 Sep signs September's. Show the user the week first "
                + "(view_labour_week) and get their yes — signing off is what arms the Xero "
                + "coding run for that week. A refusal names the unsettled days: approve or "
                + "reject them, or record a genuine absence (record_worker_absence), then sign "
                + "again. remove_labour_week_sign_off is the undo."),

        new AiAction(
            Name: "remove_labour_week_sign_off",
            Area: "Labour",
            Description: "Removes the weekly sign-off marker from a worker's week — the undo of "
                + "sign_off_labour_week. Touches no timesheet; the week simply drops back out of "
                + "settlement scope until it is signed off again.",
            CommandType: typeof(RemoveWorkerWeekSignOffByName),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(RemoveWorkerWeekSignOffByNameAuthorisation),
            ValidationType: typeof(RemoveWorkerWeekSignOffByNameValidation),
            VisibleTo: LabourRoleSets.ApproveTimesheets,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "workerName as the user says it; weekStart is any date in the week; "
                + "monthStart picks the month's part of a week that straddles a month end (left "
                + "out: the month of weekStart as given). A worker-month the coding run has "
                + "ALREADY written stays written — removing a sign-off never un-codes a bill; "
                + "re-running after a schedule change is a deliberate human decision taken on "
                + "the settlement view."),

        new AiAction(
            Name: "preview_xero_coding",
            Area: "Labour",
            Description: "DRY RUN of run_xero_coding (writes nothing, to Xero or to the run "
                + "history): for the month it reports per worker exactly what the run WOULD do — "
                + "WouldRecodeBill (naming the bill, its status, net/VAT/total and the line split), "
                + "WouldStageDraft (no bill exists for the worker-month), or Skipped with the "
                + "reason (not signed off, mapping gap, bill paid/credited/voided, already coded, "
                + "two candidate bills). Same gates, same bill search, same skip reasons as the "
                + "real run — the list IS the confirmation list.",
            CommandType: typeof(PreviewXeroCodingByName),
            ResultType: typeof(XeroCodingRunReport),
            AuthorisationType: typeof(PreviewXeroCodingByNameAuthorisation),
            ValidationType: typeof(PreviewXeroCodingByNameValidation),
            VisibleTo: LabourRoleSets.ManageSettlement,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "ALWAYS run this before run_xero_coding and show the user the per-worker list "
                + "verbatim — the run writes to the live ledger and a duplicate is far cheaper to "
                + "prevent than to find. workerNames narrows it the same way. A WouldRecodeBill "
                + "row that differs from the schedule is not a blocker: the bill's money is split "
                + "in the schedule's proportions and the difference is posted with "
                + "add_labour_settlement_variance."),

        new AiAction(
            Name: "run_xero_coding",
            Area: "Labour",
            Description: "WRITES TO XERO: runs the month's automated labour coding — the Labour "
                + "overview's Code month into Xero. For each fully signed-off worker-month it "
                + "FINDS the worker's existing bill for the month — the covered bill, or one "
                + "recognised by contact + period, draft OR AUTHORISED (the cover route "
                + "authorises the bill before the run sees it, so authorised is the normal state "
                + "for our sole traders) — and recodes that bill's lines to the settlement "
                + "schedule's split (Sites and Cost Code tracking per the effective-dated "
                + "mappings), keeping the bill's status, total, VAT treatment and attachment and "
                + "moving the timesheet cover onto the new lines in the same transaction, so the "
                + "settlement verdict reads the same before and after. Only where NO bill exists "
                + "does it stage a DRAFT bill — VAT from the contact's default in Xero (or their "
                + "last bill), never assumed. A bill it cannot recode (paid, part-paid, credited, "
                + "voided) skips naming the bill and its status; it NEVER stages a second bill "
                + "beside an existing one. Unsigned weeks, mapping gaps and already-coded months "
                + "skip-and-report. Workers linked to the same company are coded as ONE (8 Sep "
                + "2026): the company's one bill for the month is recoded once to every worker's "
                + "lines (one line per worker per site per cost code) with the cover marked per "
                + "worker, so each worker's verdict reads on their own lines; one unsigned worker "
                + "holds the whole company bill and every worker's outcome says so. A matched bill "
                + "Xero says is voided is followed to its live re-issue under the same contact, "
                + "number and period. Returns per-worker outcomes: BillRecoded, DraftStaged, "
                + "Skipped or Failed, each with the detail in the run's own words.",
            CommandType: typeof(RunXeroCodingByName),
            ResultType: typeof(XeroCodingRunReport),
            AuthorisationType: typeof(RunXeroCodingByNameAuthorisation),
            ValidationType: typeof(RunXeroCodingByNameValidation),
            VisibleTo: LabourRoleSets.ManageSettlement,
            EmailStamps: new[] { "RunByEmail" },
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Call preview_xero_coding FIRST and put its per-worker list in the confirm "
                + "turn — the user confirms against that list, not against a summary. "
                + "workerNames narrows the run to named workers — naming one worker on a company "
                + "bill runs and reports every worker on it, because the bill is coded whole; leave "
                + "it out to run everyone with activity in the month. Every skip's detail names its fix: not signed off "
                + "→ sign_off_labour_week; a mapping gap → set_site_xero_mapping or "
                + "set_cost_code_xero_mapping, then re-run; two candidate bills → mark the right "
                + "one as settlement (set_xero_line_timesheet_cover). Already-coded worker-months "
                + "skip by design (run-once) UNLESS the bill they were coded to has since been "
                + "deleted or voided in Xero, in which case the run takes them again; otherwise "
                + "reset_xero_coding_outcome (with a reason) is the deliberate way to re-run one. "
                + "Running the same month twice produces the same end state, never two bills."),

        new AiAction(
            Name: "reset_xero_coding_outcome",
            Area: "Labour",
            Description: "Resets one worker-month's Xero coding outcome so run_xero_coding will "
                + "take it again. The run-once gate reads the LATEST recorded outcome, so a "
                + "worker-month coded to a bill that was then deleted by hand sat behind "
                + "DraftStaged for ever; the reset appends a Reset outcome — who, why, what it "
                + "was — and the history reads staged → reset → recoded. Touches nothing in "
                + "Xero. Refuses when the latest outcome is not BillRecoded/DraftStaged (nothing "
                + "is blocking the run).",
            CommandType: typeof(ResetXeroCodingOutcomeByName),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(ResetXeroCodingOutcomeByNameAuthorisation),
            ValidationType: typeof(ResetXeroCodingOutcomeByNameValidation),
            VisibleTo: LabourRoleSets.ManageSettlement,
            EmailStamps: new[] { "ResetByEmail" },
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "workerName as the user says it; year/month are the worker-month; reason is "
                + "mandatory and is what future readers of the run history see. In the confirm "
                + "turn show the current outcome (view_settlement_month's lastCodingOutcome) and "
                + "say plainly that the next run WILL write to Xero for this worker — then "
                + "preview_xero_coding to show what it would do."),

        new AiAction(
            Name: "approve_labour_bill",
            Area: "Labour",
            Description: "WRITES TO XERO: approves a covered labour bill — DRAFT → AUTHORISED — exactly "
                + "as the coding run coded it, touching no line (2026-09-08, the Settlement view's "
                + "\"Approve in Xero\"). Allowed only while EVERY worker-month the bill covers reads "
                + "Matches; a worker on VarianceOpen or NoBillYet refuses with their name. The "
                + "ledger's copy reads AUTHORISED at once and a BillApproved outcome is recorded "
                + "against every worker on the bill. A bill Xero already holds approved is "
                + "acknowledged (wasAlreadyApproved), never rewritten; a paid, voided or deleted "
                + "bill refuses with Xero's own words. Nothing is paid.",
            CommandType: typeof(ApproveLabourBill),
            ResultType: typeof(LabourBillApproval),
            AuthorisationType: typeof(ApproveLabourBillAuthorisation),
            ValidationType: typeof(ApproveLabourBillValidation),
            VisibleTo: LabourRoleSets.ManageSettlement,
            EmailStamps: new[] { "ApprovedByEmail" },
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "xeroInvoiceId is the bill as view_settlement_month's coveredBill names it (its "
                + "isApprovable says the rule is met); year/month are the settlement month. In the "
                + "confirm turn show the bill's label, total, status and the workers it covers with "
                + "their verdicts, and say plainly that this authorises the bill in Xero. The step "
                + "after run_xero_coding, once every worker on the bill reads Matches; a variance "
                + "must be posted (add_labour_settlement_variance) or the bill corrected first — "
                + "approve nothing that does not match."),

        new AiAction(
            Name: "set_xero_line_timesheet_cover",
            Area: "Labour",
            Description: "Marks (or with isCovered: false unmarks) a Xero purchase line as "
                + "settlement of approved timesheets — the reconciliation tick once the "
                + "authorised bill syncs back. Covered lines are excluded from the cost-of-sales "
                + "aggregation so labour is never double-counted: the approved timesheet is the "
                + "actual, the invoice is settlement of it.",
            CommandType: typeof(SetXeroLineTimesheetCover),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(SetXeroLineTimesheetCoverAuthorisation),
            ValidationType: typeof(SetXeroLineTimesheetCoverValidation),
            VisibleTo: LabourRoleSets.ManageSettlement,
            EmailStamps: new[] { "CreatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "xeroLedgerLineId comes from list_xero_ledger_lines; projectId from "
                + "list_projects; subcontractorId is the worker's linked subcontractor company "
                + "(view_settlement_month carries it); periodStart/periodEnd bound the month the "
                + "line settles. Tell the user plainly which line you marked against which "
                + "worker-month — the marking moves reported cost of sales, and isCovered: false "
                + "is the undo if a line was marked in error."),

        new AiAction(
            Name: "add_labour_settlement_variance",
            Area: "Labour",
            Description: "Posts an accepted invoice-vs-timesheet difference as a visible "
                + "settlement variance against a cost code on a project, so posted cost of sales "
                + "equals cash paid and nothing is silently absorbed. This is resolution path (4) "
                + "when a covered bill's total will not tie to the schedule: real money posts "
                + "against the code and there is no remove — a wrong variance is corrected by "
                + "posting its opposite, visibly.",
            CommandType: typeof(AddLabourSettlementVariance),
            ResultType: typeof(LabourSettlementVariance),
            AuthorisationType: typeof(AddLabourSettlementVarianceAuthorisation),
            ValidationType: typeof(AddLabourSettlementVarianceValidation),
            VisibleTo: LabourRoleSets.ManageSettlement,
            EmailStamps: new[] { "CreatedByEmail" },
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "projectId from list_projects; costCode from list_cost_codes; amount is the "
                + "signed difference being accepted (positive = paying more than the timesheets "
                + "say); reason is mandatory and shows on the settlement view; xeroLedgerLineId "
                + "ties it to the bill line when there is one. In the confirm turn put the "
                + "schedule total, the bill total and the difference side by side — re-coding or "
                + "chasing a corrected invoice may be the honest route instead."),

        new AiAction(
            Name: "set_site_xero_mapping",
            Area: "Labour",
            Description: "Points a project at a Xero site tracking option from now on — the "
                + "mapping the coding run uses for the Sites tracking on every schedule line. "
                + "Effective-dated close-and-replace: the open row (if any) is closed with "
                + "EffectiveTo = now, never edited, so historic reads keep translating through "
                + "it; a mid-month re-map codes the month the new way.",
            CommandType: typeof(SetSiteXeroMapping),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(SetSiteXeroMappingAuthorisation),
            ValidationType: typeof(SetSiteXeroMappingValidation),
            VisibleTo: LabourRoleSets.ManageSettlement,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "projectId from list_projects; xeroTrackingOptionName spelled EXACTLY as Xero "
                + "holds the option — the coding run matches by this name and a misspelling just "
                + "moves the skip. xeroTrackingOptionId is optional — leave it out unless the "
                + "user gives you Xero's own option id. get_xero_mappings shows the current rows; "
                + "in the confirm turn show the user the old mapping (or \"unmapped\") next to "
                + "the new one."),

        new AiAction(
            Name: "set_cost_code_xero_mapping",
            Area: "Labour",
            Description: "Sets a cost code's Xero mapping from now on: its Cost Code tracking "
                + "option and the account code per line nature (labour / materials / travel) the "
                + "coding run posts to. Same effective-dated close-and-replace contract as "
                + "set_site_xero_mapping — history is never edited.",
            CommandType: typeof(SetCostCodeXeroMapping),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(SetCostCodeXeroMappingAuthorisation),
            ValidationType: typeof(SetCostCodeXeroMappingValidation),
            VisibleTo: LabourRoleSets.ManageSettlement,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "costCode from list_cost_codes. A blank account code for a nature the "
                + "worker's lines use makes the run SKIP that worker (\"no CisLabour account "
                + "code\") — when fixing a gap the run reported, set the account code for every "
                + "nature in play. get_xero_mappings shows the current rows; show old → new in "
                + "the confirm turn. xeroTrackingOptionName may stay blank — the run then uses "
                + "the cost code itself as the tracking option name."),
    };
}
