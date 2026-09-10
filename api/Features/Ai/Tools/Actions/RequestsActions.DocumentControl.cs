using Jewel.JPMS.Api.Features.DocumentControl;
using Jewel.JPMS.Api.Features.DocumentControl.Commands;
using Jewel.JPMS.Contracts.DocumentControl;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class RequestsActions
{
    private static IEnumerable<AiAction> DocumentControlActions() => new AiAction[]
    {
        new AiAction(
            Name: "send_attachments_to_document_control",
            Area: "Document control",
            Description: "Copies the chosen attachments of one mailbox email into the Document Control "
                + "queue as pending items (a point-in-time copy — the email itself is not consumed or "
                + "moved). Attachments already sent from that message are skipped, so a re-run cannot "
                + "double-send.",
            CommandType: typeof(SendAttachmentsToDocumentControl),
            ResultType: typeof(IReadOnlyList<DocumentControlItem>),
            AuthorisationType: typeof(SendAttachmentsToDocumentControlAuthorisation),
            ValidationType: typeof(SendAttachmentsToDocumentControlValidation),
            VisibleTo: DocumentControlRoles.AllowedToManage,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "messageId is a mailbox Graph message id (read_record_emails surfaces them), not a "
                + "request id. projectIdHint is only a filing hint and can be overridden when filing."),

        new AiAction(
            Name: "file_document_to_project_documents",
            Area: "Document control",
            Description: "Files a pending Document Control item into a project's Documents register "
                + "(the former drawing register — drawings, awards, letters, reports) as a document "
                + "revision — the item leaves the pending queue and the file becomes a versioned "
                + "document the project team sees. No email is sent.",
            CommandType: typeof(FileDocumentAsDrawing),
            ResultType: typeof(DocumentControlItem),
            AuthorisationType: typeof(FileDocumentAsDrawingAuthorisation),
            ValidationType: typeof(FileDocumentAsDrawingValidation),
            VisibleTo: DocumentControlRoles.AllowedToManage,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "documentControlItemId comes from the Document Control queue. Pass drawingId "
                + "(the parameter keeps the register's old name) to add a revision to an existing "
                + "document, or leave it null to create a new one with the given drawingCode/title."),

        new AiAction(
            Name: "file_document_as_payment_certificate",
            Area: "Document control",
            Description: "Files a pending Document Control item onto a project's payment certificate "
                + "register, with certificate number, certified amount and issued date — optionally "
                + "linked to a valuation claim. The item leaves the pending queue. No email is sent.",
            CommandType: typeof(FileDocumentAsPaymentCertificate),
            ResultType: typeof(DocumentControlItem),
            AuthorisationType: typeof(FileDocumentAsPaymentCertificateAuthorisation),
            ValidationType: typeof(FileDocumentAsPaymentCertificateValidation),
            VisibleTo: DocumentControlRoles.AllowedToManage,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "documentControlItemId comes from the Document Control queue; projectId from "
                + "list_projects. This is a financial record — confirm the certificate number and "
                + "amount with the user before calling."),

        new AiAction(
            Name: "file_document_to_subcontractor",
            Area: "Document control",
            Description: "Files a pending Document Control item onto a subcontractor's record as a "
                + "versioned compliance document (insurance, certification…), superseding the previous "
                + "version of the same kind exactly as a portal upload would. The item leaves the "
                + "pending queue. No email is sent.",
            CommandType: typeof(FileDocumentToSubcontractor),
            ResultType: typeof(DocumentControlItem),
            AuthorisationType: typeof(FileDocumentToSubcontractorAuthorisation),
            ValidationType: typeof(FileDocumentToSubcontractorValidation),
            VisibleTo: DocumentControlRoles.AllowedToManage,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "documentControlItemId comes from the Document Control queue. kind is the "
                + "compliance document kind as the portal names it; expiresAt sets the new version's "
                + "expiry where the kind carries one. publicLiabilityCover is the certificate's public "
                + "liability limit of indemnity in pounds (5000000 for £5m) — read it off the document "
                + "for an insurance filing, leave it null for anything else or when it is not stated."),
    };
}
