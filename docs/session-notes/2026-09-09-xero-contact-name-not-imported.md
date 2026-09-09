# 2026-09-09 — Sussex Tiling contact name / Xero link

Session was diagnosis only. No code, schema or portal data was changed.

## What Jeremy asked

Two screenshots sent 08:43, of the JPMS Directory page for **Sussex Tiling and Mastic LTD**
(`subcontractorId` `eccc36efcc514d4a85a1a2cb5e61b944`) and of the same company in Xero.

1. On the JPMS screenshot, over the Contacts panel with a contact row being edited:
   > "It says I can't edit this field - on the portal -"
2. On the Xero screenshot, showing the contact's *Primary person* card reading **Tom Dix**:
   > "But if we linked it to Xero it has not brought through his first name and last name?"

The row in question shows Name `—`, Purpose `—`, email `tom@sussextiling.co.uk`,
phone `07908 450 325`.

## What was found

**The Xero name.** The record was linked, not imported.
`SubcontractorXeroLinks` row for this record: Xero ContactId
`b50bef11-b52b-4573-9277-e02dc288eb06`, name "Sussex Tiling and Mastic LTD",
linked 2026-08-29 10:14 UTC by jeremy.ferendinos@jewelenterprises.co.uk.

- `api/Features/Subcontractors/Commands/LinkDirectoryRecordToXeroContactHandler.cs`
  writes only the link row; nothing on the record moves. Only
  `ImportXeroSupplierHandler.cs` copies name / email / phone / address / contact persons,
  and that path always creates a new record.
- `api/Features/Xero/XeroClient.ReadsSuppliers.cs` — `ReadSupplier` / `ReadContactPersons`
  read Xero's `ContactPersons[]` only. The contact's own top-level `FirstName` / `LastName`
  — what Xero's UI labels *Primary person*, and where Tom Dix sits — is never read.
  `contracts/Xero/XeroSuppliers.cs` `XeroSupplier` has no field for it.
  So an import would also have missed him.
- The directory record itself does hold `contactName` "Tom Dix" (renders on the header line
  under the company name). Only the `CompanyContacts` row is nameless. It was not written by
  the import path — `ImportXeroSupplierHandler` always sets `Phone = ""` and this row has a
  phone — so it was added by hand or by `ConsolidateDirectoryRecordsHandler`.

**The un-editable field.** Not reproduced and not located.

- `jpms/Pages/SubcontractorDetail.razor` line ~190: the Name input is
  `<input class="field" @bind="cName" />` — no `disabled`, no `readonly`.
- No string matching "can't edit" / "cannot edit" / "not editable" / "read-only" exists
  anywhere in `jpms/`, `api/` or `contracts/`.
- `UpsertCompanyContactAuthorisation.RolesThatMayEditContacts` = Director, FinanceDirector,
  ProjectManager. Jeremy's screenshot shows "Viewing as Finance Director", so the gate passes.
- `UpsertCompanyContactValidation` only refuses a row with no name, email and phone at all.

## What was decided

- Two fixes to make, both agreed in principle, neither started:
  1. Read the Xero contact's `FirstName` / `LastName` so the primary person comes through
     on import (touches `XeroClient.ReadsSuppliers.cs`, `contracts/Xero/XeroSuppliers.cs`,
     `ImportXeroSupplierHandler.cs`).
  2. Give `LinkDirectoryRecordToXeroContact` an opt-in "pull details from Xero" so linking
     an existing record is not a dead end.
- Fix 2 to be a **choice, not automatic** — Xero's address and contact details are often
  older than the directory's, and a link must not silently overwrite a corrected record.
  Jeremy asked in the reply whether he would rather it just take Xero's version.
- No migration is expected for either fix; nothing new is persisted.

## What was sent

A reply to Jeremy, drafted in-session and signed off by Nigel. Not a file — pasted into
whatever channel Nigel sends it on. Content: the link-vs-import distinction, the
primary-person gap, the two proposed fixes with the not-automatic caveat, and the three
questions below.

## Open / waiting

- **Waiting on Jeremy** — three questions asked in the reply, needed before the
  un-editable-field report can go anywhere:
  - the exact wording of the message;
  - whether it appeared on clicking into the Name box or on pressing **Save contact**;
  - whether it looked like part of the portal or a browser pop-up.
  A screenshot with the message visible was requested. Suspected to be Chrome or an
  extension rather than JPMS, but unconfirmed.
- Both Xero fixes are unstarted, pending Jeremy's answer on automatic-vs-choice.
- The blank `CompanyContacts` row on Sussex Tiling is still blank. Claude offered to correct
  it directly; Nigel has not said either way.

## Nigel's own actions

- Send the reply to Jeremy.
