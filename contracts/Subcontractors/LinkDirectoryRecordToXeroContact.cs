using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Subcontractors;

/// <summary>
/// Links an EXISTING directory record to a Xero contact — the one-field change the accountant
/// asked for (2026-09-08), replacing "Import from Xero then Consolidate" for a supplier that is
/// already in the directory. Writes the same SubcontractorXeroLink row an import writes, so the
/// record reads "Linked to Xero" everywhere an imported one does; touches nothing else on the
/// record (work orders, email tags, contacts and trades stay exactly as they were) — unless
/// <paramref name="PullDetailsFromXero"/> is asked for (2026-09-09, a choice, never automatic:
/// Xero's details are often older than the directory's), which then copies the contact's
/// primary person, email, phones and address onto the record where Xero has a value, and adds
/// Xero's additional persons as company contacts where the record lacks them. Refused when the
/// record already holds a Xero link or the Xero contact is linked to another record — unlink
/// first, deliberately. Audited (DirectoryRecordXeroLinkChanged) with who linked it.
/// </summary>
public sealed record LinkDirectoryRecordToXeroContact(
    string SubcontractorId,
    string XeroContactId,
    bool PullDetailsFromXero = false) : ICommand<Subcontractor>;
