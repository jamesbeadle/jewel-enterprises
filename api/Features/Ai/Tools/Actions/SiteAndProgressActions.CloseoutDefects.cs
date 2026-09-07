using Jewel.JPMS.Api.Features.Closeout.Commands;
using Jewel.JPMS.Api.Features.Drawings.Commands;
using Jewel.JPMS.Api.Features.Progress;
using Jewel.JPMS.Api.Features.Progress.Commands;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Features.Site.Commands;
using Jewel.JPMS.Api.Features.Todos;
using Jewel.JPMS.Api.Features.Todos.Commands;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class SiteAndProgressActions
{
    private static IEnumerable<AiAction> CloseoutDefectsActions() => new AiAction[]
    {
        new AiAction(
            Name: "raise_defect",
            Area: "Closeout & defects",
            Description: "Raises a defect on a project (description, location, the supplier it is "
                + "raised with). It is numbered from the global defect sequence (DEF-####) and "
                + "opens in Open status.",
            CommandType: typeof(RaiseDefect),
            ResultType: typeof(Defect),
            AuthorisationType: typeof(RaiseDefectAuthorisation),
            ValidationType: typeof(RaiseDefectValidation),
            VisibleTo: DefectRaisers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "subcontractorId is the DIRECTORY record of the supplier who should fix it "
                + "(search_directory resolves a company name) — the way a work order names its "
                + "supplier; assignedToEmail is the older free-typed contact, still accepted and "
                + "promoted to the matching directory record when one has that contact email. "
                + "Sending the defect to the supplier is done on the defect's page "
                + "(/projects/{projectId}/defects/{defectId}); raising it does not email anyone."),

        new AiAction(
            Name: "create_defect_from_message",
            Area: "Closeout & defects",
            Description: "Raises a defect from a mailbox message (triage pathway) and tags the "
                + "originating email to it — same numbering and Open status as a manually raised "
                + "defect, whichever door it came in through.",
            CommandType: typeof(CreateDefectFromMessage),
            ResultType: typeof(Defect),
            AuthorisationType: typeof(CreateDefectFromMessageAuthorisation),
            ValidationType: typeof(CreateDefectFromMessageValidation),
            VisibleTo: SiteTeamManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "messageId is a mailbox message id from the triage queue. An email already "
                + "tagged to another pathway is refused unless allowCrossPathway is true."),

        new AiAction(
            Name: "update_defect",
            Area: "Closeout & defects",
            Description: "Updates a defect's description, location, supplier (subcontractorId) "
                + "and status. Moving it to Resolved or Verified for the first time stamps the "
                + "resolution time.",
            CommandType: typeof(UpdateDefect),
            ResultType: typeof(Defect),
            AuthorisationType: typeof(UpdateDefectAuthorisation),
            ValidationType: typeof(UpdateDefectValidation),
            VisibleTo: SiteTeamManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Every field is written as posted — read the current defect (list_defects) "
                + "first and carry forward what should not change. Confirm with the user before "
                + "marking a defect Resolved or Verified."),

        new AiAction(
            Name: "agree_settlement",
            Area: "Closeout & defects",
            Description: "Records (or overwrites) a project's agreed final-account settlement — "
                + "final contract value, final cost, final margin and whether the client has "
                + "signed. One settlement record per project; calling again replaces the figures "
                + "and re-stamps the agreement time.",
            CommandType: typeof(AgreeSettlement),
            ResultType: typeof(SettlementRecord),
            AuthorisationType: typeof(AgreeSettlementAuthorisation),
            ValidationType: typeof(AgreeSettlementValidation),
            VisibleTo: CloseoutDirectors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "A financial sign-off — confirm the figures with the user before calling."),

        new AiAction(
            Name: "agree_vat_analysis",
            Area: "Closeout & defects",
            Description: "Records (or overwrites) a project's agreed VAT analysis — zero-rated "
                + "and standard-rated amounts, notes, and client/architect confirmation flags. "
                + "One analysis per project; calling again replaces it.",
            CommandType: typeof(AgreeVatAnalysis),
            ResultType: typeof(VatAnalysis),
            AuthorisationType: typeof(AgreeVatAnalysisAuthorisation),
            ValidationType: typeof(AgreeVatAnalysisValidation),
            VisibleTo: CloseoutDirectors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "A financial sign-off — confirm the figures with the user before calling."),

        new AiAction(
            Name: "release_retention",
            Area: "Closeout & defects",
            Description: "Records a retention release for a project — the amount, the release "
                + "time (now) and whether it is published downstream. Each call adds a new "
                + "release record.",
            CommandType: typeof(ReleaseRetention),
            ResultType: typeof(RetentionRelease),
            AuthorisationType: typeof(ReleaseRetentionAuthorisation),
            ValidationType: typeof(ReleaseRetentionValidation),
            VisibleTo: CloseoutDirectors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "A financial action — confirm the amount with the user before calling. "
                + "Distinct from confirm_retention_release, which acts on the commercial "
                + "retention schedule."),
    };
}
