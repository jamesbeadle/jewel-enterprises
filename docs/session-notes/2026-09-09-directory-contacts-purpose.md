# 2026-09-09 — Directory contacts: Purpose field dropped, primary/other named

Session: https://claude.ai/code/session_01NnqdFKSo3LnStdNs89VdFV. James working with Claude; Jeremy (accountant) is the requester.

## What Jeremy asked

1. Slack, 09:14, screenshot of the Sussex Tiling record's Contacts panel with the Purpose dropdown open (Accounts / Projects / Estimating / Site) while editing Tom Dixon:
   > "What is this purpose for - them or us - So he is the owner of that company running the project - So I assume it is projects?"
2. Follow-up after James chose to drop the field:
   > "Maybe it should be owner - contact - Project contact - accounts contact - Something like if there were multiple people. It may be easier to just have Primary contact - then Secondary contact then we can add those in the portal."
3. Separate ask, same day:
   > "When a directory record is linked to a Xero contact, could the portal push the record's contacts to Xero as well as pull them? Today I added Craig Taylor and Dominic as contacts on Wilson Electric (Battersea) Ltd in the portal, and then had to go into Xero and type the same two people in as additional persons. If the primary contact and the Contacts table rows could be written to the linked Xero contact (primary person plus additional people), with a confirm step so nothing in Xero is overwritten by accident, that would save me doing it twice on every subcontractor. I know the Cost Integration app is deliberately kept to accounting.transactions, so if this needs the contacts scope and you would rather not widen it, say so and I will keep doing the Xero side by hand."

## What was found

- `CompanyContacts.Purpose` was free text describing the contact's role at *their* company. It was read in exactly one place a user sees: `ListEmailRecipientsHandler` passed it as the `Detail` caption on a company contact's chip in the email To/Cc picker ("Sussex Tiling — Accounts"). Nothing routed or filtered on it. `ConsolidateDirectoryRecordsHandler` and `ImportXeroSupplierHandler` always wrote `""`. The rows in Jeremy's screenshot showed a dash (blank).
- The record already has a primary contact (`Subcontractor.ContactName/ContactEmail/ContactPhone`, shown in the page subtitle and edited under "Edit details") and the Contacts panel is everyone else — so "Primary then Secondary" needed no new field, only labelling.
- The original column (`20260728150000_AddXeroSupplierLinksAndCompanyContacts`) is `NOT NULL` with no default, so a single drop migration was unsafe in either deploy order.

## Decisions

- Drop Purpose (James, after being offered rename vs drop). Reason: read as a question about Jewel's side; almost always blank; one read site.
- Redo shape (James, from three options): "Primary / Other, no role" — keep the drop, retitle the page. Not chosen: fixed role picker (Owner / Projects / Accounts / Estimating / Site as an enum + Pill + picker caption); both. The role picker remains the answer if they later want to say which secondary handles accounts.
- Migration split into expand (default) before deploy and contract (drop) after.

## Built — branch `chore/drop-company-contact-purpose` off `main` (1ac060f), not pushed, not built

- `84a1005` Directory: drop the Purpose field from company contacts
  - Removed from `api/Data/Entities/PeopleEntities.cs` (`CompanyContactEntity.Purpose`), `contracts/Models/Subcontractor.cs` (`CompanyContact`), `contracts/Subcontractors/CompanyContacts.cs` (`UpsertCompanyContact`), `api/Features/Subcontractors/SubcontractorEntityMapping.cs`, `Commands/UpsertCompanyContactHandler.cs`, `Commands/ConsolidateDirectoryRecordsHandler.cs`, `Commands/ImportXeroSupplierHandler.cs`, `api/Features/Directory/Queries/ListEmailRecipientsHandler.cs` (company contacts now `EmailRecipientKind.Company` with no `Detail`), `api/Migrations/JpmsContextModelSnapshot.cs`.
  - `jpms/Pages/SubcontractorDetail.razor` / `.razor.cs`: Purpose column, input and `contact-purposes` datalist removed; add/edit row is `md:grid-cols-4`; `cPurpose` field gone.
  - Copy: `api/Features/Ai/Tools/Actions/SubcontractorsAndLeadsActions.Subcontractors.cs` (`upsert_company_contact` description), `contracts/Ai/PageGuides/ProcurementPageGuides.cs`, `.claude/skills/jpms-operator/references/site-map.md`, `docs/ui/component-anatomy.md`.
- `16c660a` Directory: name the primary contact and the other contacts as such
  - Contacts panel titled "Other contacts" with strapline from new `PrimaryContactStrapline(Subcontractor)` in `SubcontractorDetail.razor.cs` ("The primary contact is {name}, {email} (edit under Edit details). Anyone else at the company goes here." / no-primary variant). Empty state "No other contacts on this record." Edit details modal label "Contact name" → "Primary contact name". Page guide, site-map and component-anatomy updated to match.
- `85599e1` Directory: split the Purpose drop into expand (default) and contract (drop)
  - `api/Migrations/20260909100000_DefaultCompanyContactPurpose.cs` — `AlterColumn` adding `DEFAULT ''`. Apply BEFORE deploy.
  - `api/Migrations/20260909110000_DropCompanyContactPurpose.cs` (renamed from `…100000_…`) — `DropColumn`. Apply AFTER deploy.

## Migration run order (given to James)

0. `sqlcmd … -Q "SELECT TOP 1 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC"` — expected `20260908200000_AddXeroLineWriteBackFailedAt`; if not, use what it prints as the `<from>`.
1. Before deploy: `dotnet ef migrations script 20260908200000_AddXeroLineWriteBackFailedAt 20260909100000_DefaultCompanyContactPurpose --idempotent -o migrate.sql`, then `sqlcmd … -i migrate.sql -b -o migrate.log`.
2. After deploy: `dotnet ef migrations script 20260909100000_DefaultCompanyContactPurpose --idempotent -o migrate.sql`, then the same sqlcmd.
- Optional before step 2: `SELECT CompanyContactId, Name, Purpose FROM CompanyContacts WHERE Purpose <> ''` to keep any non-blank values.

## Sent / drafted

- Draft message to Jeremy explaining what goes live (Purpose column gone; primary contact = header, others = "Other contacts"; email picker chips lose the "— Accounts" caption; offer of a role picker if wanted). Given to James in chat; not sent by Claude.
- Draft reply to Jeremy on the Xero contact push (below). Given to James in chat; not sent by Claude.

## Xero contact push — assessed, not started

- The connection already reads Xero contacts (`XeroClient.ReadsSuppliers.cs`, GET `/Contacts`, `ReadContactPersons`), settings, reports and attachments, so it is not limited to `accounting.transactions`. `XeroOptions.Scopes` is unset, so the token carries whatever is ticked in the developer portal; the code cannot tell whether that is `accounting.contacts` (write) or `accounting.contacts.read`. Jeremy to check the Cost Integration app in the Xero developer portal.
- Proposed shape: one command `PushDirectoryContactsToXeroContact`, same roles/audit as link/unlink, toolbar action on the record page, `ConfirmDialog` previewing Xero's current people vs the portal's before sending. Constraints from Xero's model: additional persons are replaced wholesale on write (portal becomes master for who the people are); people are FirstName/LastName (portal `Name` split on first space); phones are company-level in Xero so other contacts' phones stay portal-side; max 5 additional persons. Estimated about a day. To be its own branch after this one ships.

## Open / waiting

- James: `dotnet build` the branch (no dotnet on the sandbox; not built by Claude), push, PR, deploy in the order above.
- James: send (or not) the two drafted messages to Jeremy.
- Jeremy: which contacts scope the Cost Integration app has; agreement to the replace-wholesale / name-split / five-person constraints before the push feature is built.
- James mentioned `cse_01BnqZ1dVsobcDbLBiQgNpDy` as "might be related"; Claude could not open it and asked what it contains — unanswered.
- At the time of writing, the working tree on this branch holds a separate, uncommitted body of work from another session (CIS verification: `RecordCisVerification*` command files, `20260909120000_AddCisVerification.cs`, `jpms/Features/Directory/CisVerificationPanel.*`, `jpms/Pages/ComplianceRegister.*`, plus edits to `SubcontractorDetail.razor`, `Subcontractors.razor(.cs)`, `JpmsContextModelSnapshot.cs` and others). Not Claude's from this session; not committed; left untouched. It sits on top of the three commits above, so the two bodies of work will need separating before this branch's PR.
