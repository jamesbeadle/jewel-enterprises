using Jewel.JPMS.Api.Features.Architects;
using Jewel.JPMS.Api.Features.Architects.Commands;
using Jewel.JPMS.Api.Features.Clients;
using Jewel.JPMS.Api.Features.Clients.Commands;
using Jewel.JPMS.Api.Features.Directory.Commands;
using Jewel.JPMS.Api.Features.Parties;
using Jewel.JPMS.Api.Features.Subcontractors.Commands;
using Jewel.JPMS.Contracts.Architects;
using Jewel.JPMS.Contracts.Clients;
using Jewel.JPMS.Contracts.Directory;
using Jewel.JPMS.Contracts.Parties;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class SubcontractorsAndLeadsActions
{
    private static IEnumerable<AiAction> SubcontractorsActions() => new AiAction[]
    {
        new AiAction(
            Name: "add_subcontractor_to_directory",
            Area: "Subcontractors",
            Description: "Creates a new company record in the subcontractor/supplier directory with its "
                + "trades, primary contact, CIS status, payment terms and postal address. With isProspect "
                + "true the record is minted for a bid-package tender list only and stays out of the "
                + "Directory until promoted.",
            CommandType: typeof(AddSubcontractorToDirectory),
            ResultType: typeof(Subcontractor),
            AuthorisationType: typeof(AddSubcontractorToDirectoryAuthorisation),
            ValidationType: typeof(AddSubcontractorToDirectoryValidation),
            VisibleTo: DirectoryCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "tradeIds come from list_trades (add_trade can mint a missing one). Check "
                + "search_directory for an existing record first — duplicates are merged later with "
                + "consolidate_directory_records, so avoid creating them."),

        new AiAction(
            Name: "update_subcontractor",
            Area: "Subcontractors",
            Description: "Updates a directory record's company name, trades, primary contact, CIS status, "
                + "payment terms and address. Null paymentTermsDays or address fields mean \"leave "
                + "unchanged\"; an empty string clears a field.",
            CommandType: typeof(UpdateSubcontractor),
            ResultType: typeof(Subcontractor),
            AuthorisationType: typeof(UpdateSubcontractorAuthorisation),
            ValidationType: typeof(UpdateSubcontractorValidation),
            VisibleTo: DirectoryRecordEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "subcontractorId comes from search_directory, which also returns the record's current "
                + "trades — send the full trade list back, removing the last trade is refused. Never "
                + "guess or derive the id (a Xero contact id is NOT a directory id)."),

        new AiAction(
            Name: "promote_subcontractor_to_directory",
            Area: "Subcontractors",
            Description: "Promotes a tender-only prospect record into the Directory proper — the "
                + "deliberate \"this company is worth keeping\" act. Idempotent: promoting a record "
                + "already in the directory returns it unchanged.",
            CommandType: typeof(PromoteSubcontractorToDirectory),
            ResultType: typeof(Subcontractor),
            AuthorisationType: typeof(PromoteSubcontractorToDirectoryAuthorisation),
            ValidationType: typeof(PromoteSubcontractorToDirectoryValidation),
            VisibleTo: DirectoryCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "consolidate_directory_records",
            Area: "Subcontractors",
            Description: "MERGES duplicate directory records into one master and PERMANENTLY DELETES the "
                + "merged-away records. Applies the supplied winning field values to the master, unions "
                + "trades, re-points everything that referenced a merged record (work orders, tenders, "
                + "compliance documents, portal logins, Xero links…) and keeps losing contact details as "
                + "company contact rows. There is no undo.",
            CommandType: typeof(ConsolidateDirectoryRecords),
            ResultType: typeof(Subcontractor),
            AuthorisationType: typeof(ConsolidateDirectoryRecordsAuthorisation),
            ValidationType: typeof(ConsolidateDirectoryRecordsValidation),
            VisibleTo: DirectoryCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Irreversible. Confirm with the user exactly which record is the master, which are "
                + "merged away, and each winning field value before calling. mergedSubcontractorIds must "
                + "never include the master."),

        new AiAction(
            Name: "import_xero_supplier",
            Area: "Subcontractors",
            Description: "Copies one Xero supplier into the company directory as a new record (category "
                + "Supplier, no trades) linked to the Xero contact; Xero's additional contact persons "
                + "become company contact rows. Never merges into an existing record — duplicates are "
                + "resolved afterwards with consolidate_directory_records.",
            CommandType: typeof(ImportXeroSupplier),
            ResultType: typeof(Subcontractor),
            AuthorisationType: typeof(ImportXeroSupplierAuthorisation),
            ValidationType: typeof(ImportXeroSupplierValidation),
            VisibleTo: DirectoryCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "xeroContactId is Xero's contact id. Refused if the supplier is already imported or "
                + "Xero is unreachable. The import is recorded against the signed-in user. If the "
                + "supplier is ALREADY in the directory under its own record, do not import — use "
                + "link_directory_record_to_xero_contact (list_unlinked_directory_records suggests "
                + "the pairings)."),

        new AiAction(
            Name: "link_directory_record_to_xero_contact",
            Area: "Subcontractors",
            Description: "Links an EXISTING directory record to a Xero contact — the one-field change "
                + "for a supplier that is already in the directory but was never imported from Xero. "
                + "Writes only the Xero link; the record's work orders, email tags, contacts and "
                + "trades are untouched, and no second record is created. Refused when the record "
                + "already holds a Xero link or the Xero contact is linked to another record (unlink "
                + "first, or consolidate). Audited with who linked it.",
            CommandType: typeof(LinkDirectoryRecordToXeroContact),
            ResultType: typeof(Subcontractor),
            AuthorisationType: typeof(LinkDirectoryRecordToXeroContactAuthorisation),
            ValidationType: typeof(LinkDirectoryRecordToXeroContactValidation),
            VisibleTo: DirectoryCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "subcontractorId comes from search_directory or list_unlinked_directory_records; "
                + "xeroContactId is Xero's ContactID (the suggestions in list_unlinked_directory_records "
                + "carry it). A name match is a suggestion, not proof — show the user the pairing "
                + "(record name ↔ Xero contact name) and take their yes before calling, then call "
                + "once per confirmed pair. unlink_directory_record_from_xero_contact is the undo."),

        new AiAction(
            Name: "unlink_directory_record_from_xero_contact",
            Area: "Subcontractors",
            Description: "Removes the link between a directory record and a Xero contact — the undo "
                + "of link_directory_record_to_xero_contact or of a mistaken import's link. Only the "
                + "link goes; the record and everything referencing it stay. Refused when the record "
                + "is not linked to that contact. Audited.",
            CommandType: typeof(UnlinkDirectoryRecordFromXeroContact),
            ResultType: typeof(Subcontractor),
            AuthorisationType: typeof(UnlinkDirectoryRecordFromXeroContactAuthorisation),
            ValidationType: typeof(UnlinkDirectoryRecordFromXeroContactValidation),
            VisibleTo: DirectoryCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "subcontractorId and xeroContactId come from search_directory (xeroLinks on the "
                + "record). Confirm with the user which record and which Xero contact before calling — "
                + "an unlinked supplier's bills stop reconciling against the record until re-linked."),

        new AiAction(
            Name: "upsert_company_contact",
            Area: "Subcontractors",
            Description: "Adds or updates a person on a directory record's contact list, with the "
                + "free-text purpose the contact serves (\"Accounts\", \"Projects\", \"Estimating\"…). A "
                + "null/blank companyContactId inserts; a populated one updates in place.",
            CommandType: typeof(UpsertCompanyContact),
            ResultType: typeof(CompanyContact),
            AuthorisationType: typeof(UpsertCompanyContactAuthorisation),
            ValidationType: typeof(UpsertCompanyContactValidation),
            VisibleTo: DirectoryRecordEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "companyContactId for an update comes from the record's contact list "
                + "(ListCompanyContacts)."),

        new AiAction(
            Name: "remove_company_contact",
            Area: "Subcontractors",
            Description: "Deletes a person from a directory record's contact list permanently. There is "
                + "no undo.",
            CommandType: typeof(RemoveCompanyContact),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(UpsertCompanyContactAuthorisation),
            ValidationType: null,
            VisibleTo: DirectoryRecordEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which contact, by name and company, before calling. "
                + "companyContactId comes from the record's contact list (ListCompanyContacts)."),

        new AiAction(
            Name: "upload_compliance_document",
            Area: "Subcontractors",
            Description: "Records a compliance document entry (metadata only — no file bytes travel "
                + "through this action) against a directory record: the document kind, file name and "
                + "expiry date. Recording an existing kind supersedes the previous version rather than "
                + "duplicating it.",
            CommandType: typeof(UploadComplianceDocument),
            ResultType: typeof(ComplianceDocument),
            AuthorisationType: typeof(UploadComplianceDocumentAuthorisation),
            ValidationType: typeof(UploadComplianceDocumentValidation),
            VisibleTo: RoleSet.Of(
                JpmsRoles.Director, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing,
                JpmsRoles.Subcontractor),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "A subcontractor portal login may only record against its own company's record — "
                + "further per-record checks apply at execution. The actual file, if there is one, is "
                + "uploaded in the portal; use this only to log a document's existence and expiry."),

        new AiAction(
            Name: "add_trade",
            Area: "Subcontractors",
            Description: "Adds a trade to the curated master trade list. The name is normalised (trimmed, "
                + "first letter capitalised) and matched case-insensitively — adding an existing trade "
                + "returns it unchanged.",
            CommandType: typeof(AddTrade),
            ResultType: typeof(Trade),
            AuthorisationType: typeof(AddTradeAuthorisation),
            ValidationType: typeof(AddTradeValidation),
            VisibleTo: DirectoryCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "rename_trade",
            Area: "Subcontractors",
            Description: "Renames a trade on the curated master list — every directory record carrying "
                + "the trade shows the new name at once (bid packages keep the snapshot name they were "
                + "created with). Renaming to a name another trade already holds is refused.",
            CommandType: typeof(RenameTrade),
            ResultType: typeof(Trade),
            AuthorisationType: typeof(RenameTradeAuthorisation),
            ValidationType: typeof(RenameTradeValidation),
            VisibleTo: DirectoryCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "tradeId comes from list_trades."),

        new AiAction(
            Name: "delete_trade",
            Area: "Subcontractors",
            Description: "Deletes a trade from the curated master list permanently. Refused while any "
                + "directory record still carries the trade — reassign those records first.",
            CommandType: typeof(DeleteTrade),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteTradeAuthorisation),
            ValidationType: typeof(DeleteTradeValidation),
            VisibleTo: DirectoryCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which trade, by name, before calling. tradeId comes from "
                + "list_trades.")
    };
}
