# Session note — 2026-09-09 — CIS verification fields and the compliance register

Session: https://claude.ai/code/session_01EfzsKkwtmbsguHB9HaCLn5
Working folder: `jewel-portal`, branch state at start `84a1005` (Directory: drop the Purpose field from company contacts). Nothing committed; every change below is an uncommitted edit in the working tree.

## What Jeremy (accountant) asked for

Relayed by James ("please see accountant message"), Jeremy's words:

1. "CIS status field. update_subcontractor refused 'Verified 20% standard, SVN V1415495651, 9 Sep 2026' with 'An error occurred while saving the entity changes'. 'Verified 20%' saved. Looks like a length limit. Can you widen it, or better, add SVN and verification date fields so the HMRC result is held properly."
2. "CIS status is not shown anywhere on the directory record page. Please display it, with SVN and date, next to the compliance documents."
3. "Insurance expiry across subcontractors. The Compliance pill on each Directory row is useful, but I need one place to see everyone whose insurance has expired or is expiring. A filter on the Directory list (Expired / Expiring soon / Missing) and a compliance register page would do it."

James added: "maybe relates to this not sure you check cse_01BnqZ1dVsobcDbLBiQgNpDy". That id is not in the codebase and is not a JPMS error reference (those read `JPMS-XXXXXX`); it looks like a Claude conversation id from Jeremy's chat. Not investigated further — the failure is fully explained by the column width below.

## Cause of item 1

`SubcontractorEntity.CisStatus` was `[MaxLength(32)]` / `nvarchar(32)`. The value Jeremy sent is 51 characters; SQL Server rejected the row and EF surfaced its generic "error occurred while saving the entity changes". `UpdateSubcontractorValidation` had no length check, so the caller saw a 500 rather than a 400 that said why.

## Decisions

- Do both halves of Jeremy's ask: widen `CisStatus` to 64 AND add two fields, `CisVerificationNumber` (nvarchar 32) and `CisVerifiedOn` (date, nullable). `CisStatus` stays the short reading of the HMRC result ("Verified 20% standard"); the number and date get their own columns.
- The HMRC result is one fact, so it is written by ONE new command, `RecordCisVerification(SubcontractorId, CisStatus, CisVerificationNumber, CisVerifiedOn)`, which replaces all three fields together (empty number / null date allowed). `UpdateSubcontractor` keeps its `CisStatus` parameter unchanged and gains no new fields — "null means leave unchanged" would have made the date impossible to clear.
- Gate for the new command mirrors `UpdateSubcontractorAuthorisation` (Director, FinanceDirector, ProjectManager; Admin implicit).
- Compliance standing on the Directory is per company (worst status among current documents, Missing when none — the existing `ComplianceOverviewReadModel.WorstStatusFor`). The register is per document, plus one Missing row per company with nothing on file.
- The register lives at `/directory/compliance`, a sibling view of the Directory's Subcontractors group, linked both ways by a `TabRow` (Companies | Compliance register). `PageContext.LabelFor` resolves it to "Directory" through the sidebar item's prefix match — no fallback added.
- Consolidation is unchanged: the master keeps its own CIS fields; merged-away records' SVN/date are dropped with their other non-winning fields.
- The dashboard's "Documents expiring" tile now lands on the register instead of `/directory`.
- `CisVerificationPanel` is a new component rather than more lines in `SubcontractorDetail.razor` (already far over the 100-line target). `Subcontractors.razor.cs` (258 lines before) grew by ~8 lines; the filter logic was extracted to `DirectoryComplianceFilter`.

## Built / changed

### contracts
- `contracts/Models/Subcontractor.cs` — `Subcontractor` record gains trailing parameters `CisVerificationNumber = ""` and `DateOnly? CisVerifiedOn = null`, and `HasCisVerification`. Appended with defaults, so positional callers (`DirectoryContactForm.razor.cs`, `tests/.../WorkOrderDepositTests.cs`) are unaffected.
- `contracts/Subcontractors/RecordCisVerification.cs` — NEW: the command, plus `CisVerificationLimits` (`StatusMaxLength = 64`, `NumberMaxLength = 32`) shared by validation and the form.

### api
- `api/Data/Entities/PeopleEntities.cs` — `CisStatus` MaxLength 32 → 64; new `CisVerificationNumber` (MaxLength 32) and `DateOnly? CisVerifiedOn`.
- `api/Migrations/20260909105000_AddCisVerification.cs` — NEW, hand-written (no Designer file, as the recent migrations): AlterColumn CisStatus nvarchar(64); AddColumn CisVerificationNumber nvarchar(32) NOT NULL DEFAULT ''; AddColumn CisVerifiedOn date NULL. Down reverses. Additive only.
- `api/Migrations/JpmsContextModelSnapshot.cs` — SubcontractorEntity block updated by hand for the three properties.
- `api/Features/Subcontractors/SubcontractorEntityMapping.cs` — `ToModel` passes the two new fields.
- `api/Features/Subcontractors/Commands/RecordCisVerificationAuthorisation.cs`, `…Validation.cs`, `…Endpoint.cs`, `…Handler.cs` — NEW. Endpoint: `PUT /api/subcontractors/{subcontractorId}/cis-verification`. Validation: id and status required; status ≤ 64; number ≤ 32; date not in the future. Handler trims, upper-cases the number, saves, returns the record.
- `api/Features/Subcontractors/SubcontractorsFeatureRegistration.cs` — registers handler, authorisation, validation.
- `api/Features/Subcontractors/Commands/UpdateSubcontractorValidation.cs` — refuses `CisStatus` over 64 characters with a message pointing at `RecordCisVerification` (a 400 instead of the 500 Jeremy hit).
- `api/Features/Ai/Tools/Actions/SubcontractorsAndLeadsActions.Subcontractors.cs` — NEW action `record_cis_verification` (VisibleTo `DirectoryRecordEditors`, no confirmation); `update_subcontractor` notes now say cisStatus is the short reading only and point to the new action.
- `api/Features/Ai/Tools/AiRecordTools.Directory.cs` — `search_directory` rows now include `cisVerificationNumber` and `cisVerifiedOn`.

### jpms
- `jpms/Features/Subcontractors/SubcontractorsRouteRegistration.cs` — route for `RecordCisVerification` (PUT `/api/subcontractors/{id}/cis-verification`).
- `jpms/Services/ISubcontractorStore.cs`, `jpms/Services/HttpSubcontractorStore.cs` — `RecordCisVerificationAsync(RecordCisVerification)`; sends, then refreshes the read model.
- `jpms/Features/Directory/CisVerificationPanel.razor` + `.razor.cs` — NEW. `Panel` "CIS verification" with Status / Verification number / Verified on (`EmptyState` when none), "Record verification…" `Modal` with `FormField`s (status datalist: Verified 20% standard, Gross payment, Unverified 30%; `maxlength` from `CisVerificationLimits`). `CanEdit` parameter.
- `jpms/Pages/SubcontractorDetail.razor` — renders `<CisVerificationPanel Subcontractor="subcontractor" CanEdit="CanAccess" />` directly above `SubcontractorComplianceList`.
- `jpms/Features/Directory/DirectoryComplianceFilter.cs` — NEW: `All`, `WorstFirst` order (Expired, ExpiringSoon, Missing, Current), `RankOf`, `Chips(countFor)`, `CompanyChips(companies, compliance, isLoaded)`, `Passes(...)` overloads.
- `jpms/Features/Directory/DirectoryViewTabs.cs` — NEW: the two `TabItem`s (`/directory`, `/directory/compliance`).
- `jpms/Features/Directory/ComplianceRegisterRow.cs` — NEW: `record (Company, Document?, Status)` + `Build(companies, currentDocuments)` (current versions only; Missing row for a company with none; ordered worst first, then soonest expiry, then company name).
- `jpms/Pages/Subcontractors.razor` / `.razor.cs` — `TabRow` above the filters (Subcontractors group only); Compliance `FilterChips` beside the Xero chips, disabled until both the directory and `Compliance.Current` have loaded; `FiltersActive` and `Filtered()` include the new filter.
- `jpms/Pages/ComplianceRegister.razor`, `.razor.cs`, `.Export.cs` — NEW page `/directory/compliance`: same access gate as the Directory (Admin/MD/FD/PM); `PageHeader` strapline "N companies · N documents expired or due within 30 days" once loaded; `TabRow`; `SearchInput` (company, trade, document) + status `FilterChips` with counts; `RecordsTable` (Dense) Company / Trade / Document / Expires / Compliance, `tr.is-clickable` opens `/directory/{id}`; `ExportToExcelButton` with "Ignore search & filter". Rows are rebuilt on store/read-model change, not per render. `dataFailed` → `Notice`.
- `jpms/Components/RoleHome.razor.cs` — "Documents expiring" tile href `/directory` → `/directory/compliance`.

### Database apply (per CLAUDE.md — scoped script, never the full idempotent one)
```
sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -Q "SELECT TOP 1 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC"
cd api && dotnet ef migrations script <that-id> --idempotent -o migrate.sql
sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -i migrate.sql -b -o migrate.log
```
Read `migrate.log`. Safe to apply ahead of the code (additive; the old code never reads the new columns).

## Not verified

- **Nothing has been compiled.** `dotnet` is not available in the shell Claude gets on the Mac, and NuGet/dotnet downloads are blocked from the cloud container. All new C#/Razor is unbuilt. Specific things to watch on first build: `DateOnly?` binding in the AI schema (already handled for `DateOnly` in `AiActionSchema.cs`, untested for nullable); `FormField Required` as a valueless bool attribute; `@bind` of `DateTime?` to `type="date"` in `CisVerificationPanel` (same pattern as `SubcontractorComplianceList`); the hand-edited model snapshot matching the entity (`dotnet ef migrations has-pending-model-changes` in `api` will say).
- `RecordsTable` `IsLoading`/`IsEmpty` precedence on the register page was not read — check it does not flash "No documents match." while loading.
- Blazor route precedence between `/directory/compliance` and `/directory/{SubcontractorId}` relies on literal-segment-wins; confirm in the browser.
- No screenshots, no tests added or run.

## Open / waiting on someone

- **James**: run `dotnet build` on `api` and `jpms`, fix or send back errors; apply the migration with the commands above; commit (attribution lines are in the session).
- **James**: decide whether consolidation should carry the SVN/date with the winning `CisStatus` (currently the master keeps its own; merged records' values are lost).
- **Jeremy**: re-record the result that failed on 9 Sep on its record as three fields — status "Verified 20% standard", number `V1415495651`, verified on 2026-09-09 — via the record page's "Record verification…" or the `record_cis_verification` action, once deployed.

## Things Claude said it would do and did not get to (link to the Mac dropped)

- Update `contracts/Ai/PageGuides/ProcurementPageGuides.cs`: the `/directory` guide (compliance chips, TabRow), the `/directory/{subcontractorId}` guide (CIS verification panel), and a new `/directory/compliance` entry.
- Update `.claude/skills/jpms-operator/references/site-map.md` (Directory section) for the register route and the CIS panel.
- Add a short CLAUDE.md working note under Directory/Subcontractors: `CisStatus` is the short reading; number and date are `RecordCisVerification`'s; `/directory/compliance` is the register; `DirectoryComplianceFilter.WorstFirst` is the one order compliance lists read in.
- Read `RecordsTable.razor` and `SearchInput.razor` to confirm the parameter semantics used on the register page.
