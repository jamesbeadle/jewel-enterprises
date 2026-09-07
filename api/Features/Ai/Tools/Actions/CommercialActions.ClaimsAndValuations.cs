using Jewel.JPMS.Api.Features.Cashflow.Commands;
using Jewel.JPMS.Api.Features.Commercial.Commands;
using Jewel.JPMS.Api.Features.CommercialInputs.Commands;
using Jewel.JPMS.Api.Features.Cvr.Commands;
using Jewel.JPMS.Contracts.Cashflow;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.CommercialInputs;
using Jewel.JPMS.Contracts.Cvr;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class CommercialActions
{
    private static IEnumerable<AiAction> ClaimsAndValuationsActions() => new AiAction[]
    {
        // ── Commercial: claim periods, valuations and the valuation report bill ──────────

        new AiAction(
            Name: "add_claim_period",
            Area: "Commercial",
            Description: "Defines a numbered claim period (start and end dates) on a project — the "
                + "billing calendar valuations are drafted against.",
            CommandType: typeof(AddClaimPeriod),
            ResultType: typeof(ClaimPeriod),
            AuthorisationType: typeof(AddClaimPeriodAuthorisation),
            ValidationType: typeof(AddClaimPeriodValidation),
            VisibleTo: ClaimPeriodDefiners,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. Dates are ISO 8601."),

        new AiAction(
            Name: "draft_valuation",
            Area: "Commercial",
            Description: "Creates a Draft valuation (gross value and retention percent) against one of "
                + "a project's claim periods — the money the company intends to certify for that period. "
                + "Draft only; nothing is issued to the client until issue_valuation.",
            CommandType: typeof(DraftValuation),
            ResultType: typeof(Valuation),
            AuthorisationType: typeof(DraftValuationAuthorisation),
            ValidationType: typeof(DraftValuationValidation),
            VisibleTo: ValuationDrafters,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects; claimPeriodId from the project's claim periods "
                + "(add_claim_period creates them)."),

        new AiAction(
            Name: "revise_valuation",
            Area: "Commercial",
            Description: "Changes an existing valuation's gross value and retention percent — a direct "
                + "edit of the money figures on the valuation record.",
            CommandType: typeof(ReviseValuation),
            ResultType: typeof(Valuation),
            AuthorisationType: typeof(ReviseValuationAuthorisation),
            ValidationType: typeof(ReviseValuationValidation),
            VisibleTo: ValuationDrafters,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "valuationId comes from the project's valuations list."),

        new AiAction(
            Name: "issue_valuation",
            Area: "Commercial",
            Description: "Issues a drafted valuation — the formal act that moves it from Draft to "
                + "Issued, committing the certified money position for the period. Directors only.",
            CommandType: typeof(IssueValuation),
            ResultType: typeof(Valuation),
            AuthorisationType: typeof(IssueValuationAuthorisation),
            ValidationType: typeof(IssueValuationValidation),
            VisibleTo: DirectorsOnly,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling — issuing is a formal financial step. "
                + "valuationId comes from the project's valuations list."),

        new AiAction(
            Name: "add_valuation_line_item",
            Area: "Commercial",
            Description: "Adds a priced line to a project's valuation report bill of quantities "
                + "(section, cost code, description, quantity, rate) — changing the total value the "
                + "report claims against.",
            CommandType: typeof(AddValuationLineItem),
            ResultType: typeof(ValuationLineItem),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: typeof(AddValuationLineItemValidation),
            VisibleTo: ValuationBillEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects; cost codes from list_cost_codes. "
                + "get_valuation_context shows the existing bill and its sections."),

        new AiAction(
            Name: "update_valuation_line_item",
            Area: "Commercial",
            Description: "Rewrites one valuation report line's full details — section, cost code, "
                + "description, quantity, rate — changing the value that line contributes to the bill.",
            CommandType: typeof(UpdateValuationLineItem),
            ResultType: typeof(ValuationLineItem),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: typeof(UpdateValuationLineItemValidation),
            VisibleTo: ValuationBillEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "This replaces every field on the line, not just the ones being changed — read the "
                + "current line first (get_valuation_context) and carry forward what should not change. "
                + "valuationLineItemId comes from the valuation report's lines."),

        new AiAction(
            Name: "remove_valuation_line_item",
            Area: "Commercial",
            Description: "Deletes a line from the valuation report bill permanently, removing its value "
                + "from the report. There is no undo.",
            CommandType: typeof(RemoveValuationLineItem),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: null,
            VisibleTo: ValuationBillEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which line, by description and value, before calling."),

        // ── Commercial: valuation claim lifecycle ────────────────────────────────────────

        new AiAction(
            Name: "start_valuation_claim",
            Area: "Commercial",
            Description: "Starts a new valuation claim on a project (claim number and date), optionally "
                + "seeding every line's % complete from a previous claim. Retention terms are stamped "
                + "from the project's contract unless explicitly overridden.",
            CommandType: typeof(StartValuationClaim),
            ResultType: typeof(ValuationClaim),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: typeof(StartValuationClaimValidation),
            VisibleTo: ClaimLifecycleManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. Leave retentionPercent and "
                + "retentionReleasePercent null so the contract terms apply — a value is for "
                + "seeding/backfill only. seedFromClaimId (a previous claim's id) rolls the prior "
                + "claim's per-line % complete forward."),

        new AiAction(
            Name: "record_claim_entry",
            Area: "Commercial",
            Description: "Sets one valuation line's cumulative % complete on a Draft claim — the "
                + "commercial input that drives the amount claimed this period. The claim line's "
                + "cumulative claimed and period increment are recomputed from it.",
            CommandType: typeof(RecordClaimEntry),
            ResultType: typeof(ClaimLine),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: typeof(RecordClaimEntryValidation),
            VisibleTo: ClaimLifecycleManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Only works on a Draft claim. valuationClaimId and valuationLineItemId come from "
                + "get_valuation_context."),

        new AiAction(
            Name: "record_claim_entries",
            Area: "Commercial",
            Description: "Bulk-sets many valuation lines' cumulative % complete on a Draft claim in one "
                + "call — the same financial effect as record_claim_entry, batched for opening positions "
                + "or heavy-update months across large bills.",
            CommandType: typeof(RecordClaimEntries),
            ResultType: typeof(IReadOnlyList<ClaimLine>),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: typeof(RecordClaimEntriesValidation),
            VisibleTo: ClaimLifecycleManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Only works on a Draft claim. Each entry pairs a valuationLineItemId with its "
                + "cumulative percentComplete."),

        new AiAction(
            Name: "preapprove_valuation_claim",
            Area: "Commercial",
            Description: "Locks a Draft claim's amounts and moves it to Preapproved — the \"we are "
                + "claiming this\" step that freezes what will be put to the client. Reversible only "
                + "via reopen_valuation_claim.",
            CommandType: typeof(PreapproveValuationClaim),
            ResultType: typeof(ValuationClaim),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: null,
            VisibleTo: ClaimLifecycleManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling — this freezes the claim's amounts. "
                + "valuationClaimId comes from get_valuation_context."),

        new AiAction(
            Name: "reopen_valuation_claim",
            Area: "Commercial",
            Description: "Undoes an unintended preapproval: moves a Preapproved claim back to Draft, "
                + "clearing the frozen totals so amounts compute live from entries again. Confirmed "
                + "claims are final and cannot be reopened.",
            CommandType: typeof(ReopenValuationClaim),
            ResultType: typeof(ValuationClaim),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: null,
            VisibleTo: ClaimLifecycleManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "confirm_valuation_claim",
            Area: "Commercial",
            Description: "Records that the client has paid: freezes the claim's summary totals and "
                + "per-row claimed amounts and advances the project's certified-to-date position. "
                + "The next claim's this-period figures are unaffected — they always measure from the "
                + "claim immediately before, paid or not. Final — a Confirmed claim cannot be reopened.",
            CommandType: typeof(ConfirmValuationClaim),
            ResultType: typeof(ValuationClaim),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: null,
            VisibleTo: ClaimLifecycleManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Irreversible. Confirm with the user, naming the claim, before calling."),

        new AiAction(
            Name: "rename_valuation_claim",
            Area: "Commercial",
            Description: "Sets a claim's free-text period name (e.g. \"June 2026\"). Bookkeeping only — "
                + "no amounts change, and a locked claim may still be renamed.",
            CommandType: typeof(RenameValuationClaim),
            ResultType: typeof(ValuationClaim),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: null,
            VisibleTo: ClaimLifecycleManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "delete_valuation_claim",
            Area: "Commercial",
            Description: "Deletes a claim and its per-line entries permanently (test claims, false "
                + "starts). Invoices and snapshots that referenced it survive with the link cleared — "
                + "money already invoiced or certified does not move. There is no undo.",
            CommandType: typeof(DeleteValuationClaim),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: null,
            VisibleTo: ClaimLifecycleManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which claim, by number and name, before calling."),

    };
}
