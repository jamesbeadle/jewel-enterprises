# Jewel Bespoke Build — working notes

## Terminology

- **Programme** is the canonical term for the project's plan of work and the project tab that holds it (the programme itself, its claims documents, and its correspondence). Never call it "Schedule" (or US-spelled "Program") in UI copy, code identifiers, routes, or docs. "Scheduling"/"schedule" survive only in persisted backend identifiers (e.g. `RecordType.Scheduling`, the `JPMS/SCH-` mail tag, API routes), immutable EF migrations, and the distinct retention-release concept `RetentionSchedule`, which is not the programme.
- **Valuation invoice** is the canonical term for an amount of money Jewel has claimed for the client to pay (raised against the current valuation; lifecycle: Raised — accounts' first move once the project team has valued & locked the claim; files a draft and freezes the report snapshot, sends nothing — → Submitted, i.e. the claim recorded as sent to the architect/client (the portal never emails it; "Record claim sent") → Approved → Issued → Paid; one click per material stage, driven from the claim card on the valuation page, and every button either creates a portal record ("Raise …") or records an outside event ("Record …") — none says "send"; since 2026-09-09 Issue is "Raise in Xero & issue…", which creates the AUTHORISED sales invoice in Xero and issues here in one press). Never introduce "cash call", "payment application", "application for payment", or "client invoice" for this concept in UI copy, code identifiers, or docs. "Cash call" survives only in historical meeting notes and immutable EF migrations. See `docs/00-business-context/glossary.md`.
- **Variation** is the canonical term for the priced change item, and it is **one document with one number through every stage** — its `VariationOrderStatus` (Quoting → Issued → Awaiting AI → Approved / Rejected) is what says where it has got to. Never present "VOQ" and "VO" as two records or two ladder steps: the 2026-07-23 `UnifyVariationOrders` migration folded them into one row, and the UI followed. The record lineage is **three** stages — Request → RFI → Variation. (Bid packages left the chain on 2026-08-12: a variation order sets the sales side for a cost code, a bid package groups works across cost codes by trade — they are separate records, and tendering runs entirely on the bid package. `SelectedBidPackageId` and the packages' parent `VariationOrderQuoteId` column survive as legacy data only.) A user always reads the number as `V72` (`VariationOrder.DisplayNumber`, and the `VariationRef` minted at approval, which is the same number). "VOQ" survives only in persisted identifiers and API surface: the `VariationOrderQuotes` table and its `VariationOrderQuoteId` column, the stored `Reference` (`VOQ-0072`), the `JPMS/VOQ-…` mail tags, the `/api/…/voq(s)/…` routes, `RecordType.VariationQuote`, and command names like `CreateVoqFromRfq`. The page route is `/projects/{id}/variations/{id}`; the old `/voq/{id}` route is kept on the same page so links already sent out still land.

- **Sales strategy** and **lead** (Sales folder, 2026-09-06). A *strategy* is a methodology for
  FINDING leads written down with its justification — audience, target area, hypothesis (why these
  people, why now), evidence, channel, proposition, a Claude-drafted approach plan, a status and
  the funnel its leads make. A *lead* is a person we might convince to build with Jewel plus the
  property the work would be on; every lead lands in the one register whatever found it and
  carries its strategy's id when a strategy did. One ladder for every lead: New → Contacted →
  Engaged → Site visit → Proposal → Won / Lost, Nurture for the parked (`LeadStage`, ints
  remapped from the May prototype by `AddSalesStrategies`). **Won is `WinLead`**, never a stage
  move — it creates the Client account and the project shell in one handler. Code lives in
  `contracts/Sales`, `api/Features/Sales`, `jpms/Features/Sales` + `jpms/Pages/Sales*.razor`;
  the prototype's satellite CRM tables (QualificationAssessments, SiteVisits, InfoChaseItems,
  BidDecisions, Proposals, LeadOutcomes) stay in the database, unread.

- **Draft programme update** (Programme tab, 2026-09-08) is the canonical term for the
  certified valuation's percentages proposed onto the programme's tasks for review — never
  "sync", "auto-update" or "programme import". One opens automatically when **Record approval**
  is taken on the claim card (the architect's certification: `ApproveValuationInvoiceHandler`,
  best effort, after the approval is committed) and by hand from the Programme tab's "Draft from
  valuation…" door against any locked claim. A task's progress is proposed as the £-weighted
  completion of the valuation lines on its **cost centres**; the mapping comes from the task's
  saved mapping (`ProgrammeTaskCostCentres`, written when a draft is applied), else the
  trade-word rulebook (`ProgrammeCostCentreRules`), else Claude (asked once per draft), else the
  reviewer. Progress only — planned dates never move. Only the newest draft on a project is
  Open; the rest are Applied / Discarded / Superseded, never deleted. Code: `contracts/Site/
  ProgrammeDrafts.cs`, `api/Features/Site/Drafts`, `jpms/Features/Site/Programme/
  ProgrammeDraftReview`; spec `docs/Programme-Draft-From-Valuation-Spec.md`.
- **Variations and EOTs on the programme** (Programme tab, 2026-09-09). The Gantt has two
  sections beneath the tasks on the same ruler (`ProgrammeTimeline`): **Variations** — every
  variation but a rejected one, approved or not, placed on the tasks its cost centres map to
  through `ProgrammeTaskCostCentres` — and **Extensions of time**. The programme READS
  variations and never writes them: what a variation does to the programme is the programme's
  OWN record, `ProgrammeVariationEffect` (variation + task + days; `RecordProgrammeVariationEffect`
  / `RemoveProgrammeVariationEffect`, Director + PM, connector actions of the same names). A
  push is an OVERLAY — a segment on the task's end, dashed until the variation is approved —
  and planned dates, cost centres and the valuation never move because of one. An EOT's days
  are the request's own facts (`Request.EotDaysClaimed` / `EotDaysGranted`, EOT only, written by
  `RaiseRequest` / `UpdateRequestDetails` under the "null means not supplied" convention) and
  the row is drawn from the completion it extends (baselined, else current). Placement is pure
  and shared with `get_programme` (`ProgrammeVariationPlacement`, `ProgrammeExtensionPlacement`,
  contracts/Models). RFIs are not on the chart: nothing links an RFI to a task yet.


## Record tabs & the in-view toolbar (jpms)

- **The request chain renders as document tabs, not chips.** `RecordTabBar` (Components) is on
  every page in the chain — Request → official stage (RFI/NOD/EOT) → Variation. Only records that
  EXIST get a tab; the action that creates the next stage lives on the current stage's tab, never
  on a placeholder. On the request page the Request and official tabs are local panes
  (`LocalRequestTabs` + `OnSelect`); the variation tab navigates to the record's own page, which
  renders the same bar — moving along the chain reads as switching tabs. Deep-link the official
  pane with `?tab=official`. Bid packages are NOT on the bar (separation 2026-08-12) — they are
  standalone records under the project's Bid Package Invites tab.
- **Two dates, two meanings.** `Issued` is the official date the correspondent/client was notified
  — it is what lists lead with, and it is user-editable (requests) or stamped by the status
  transition (variations). `Created` is the system's own stamp (`Request.RaisedAt`,
  `VariationOrder.CreatedAt`) — shown only as a secondary fact on detail pages, and never as a
  list's lead date. Don't label `CreatedAt` "Raised".
- **In-view menu options are a `Toolbar` of icon buttons** (`ToolbarButton`, glyphs from
  `ActionIcon`, hover text mandatory), grouped by related functionality with `ToolbarDivider` —
  e.g. document actions (download PDF, email) | data actions (export, refresh). Underlined text
  links and one-off `btn-secondary`s are not the way to add a view action any more. The labelled
  `btn-primary` next to a toolbar stays reserved for the view's one primary act of creation
  ("Raise request"). `ExportToExcelButton` already renders as a toolbar button — keep passing
  `ShowIncludeAllRows`/`IncludeAllLabel` and it offers the current-view / include-all choice as a
  menu. Never wrap a toolbar in a LoadGate; pass `Disabled`/`Busy` to the buttons instead.

## Shared components (jpms) — the look lives in components, never in a view

The Open Book Figma is the design (`docs/ui/open-book-design-rules.md`; tokens in
`jpms/tailwind.config.js`, recipes in `jpms/Styles/app.tailwind.css`, the short form in
`jpms/DESIGN-SYSTEM.md`). A view composes the components below and never re-types their class
strings; when a view needs a look none of them gives, the answer is a new shared component with
its rule written here, not a one-off div. `jpms/DESIGN-SYSTEM.md` §5 has the lint grep that
finds drift.

- **`Page`** wraps every routed view except the landing page: the signed-in + approved gate and
  the one content gutter. `CanAccess`/`AccessDeniedMessage` is the page's own role check; `Bare`
  is for a full-bleed workspace (the Control Centre). No page types `<section class="px-…">` or
  the `sessionReady`/`RequestAccessView` preamble.
- **`PageHeader`** is the page's one header, directly inside `Page`. The top bar (`PageHeading`,
  fed by `PageContext.LabelFor`) IS the page title, exactly as the Figma puts it — so a register
  page gives no `Title`, only the subtitle/count strapline and the actions. A record page's
  `Title` is the record (reference as `Eyebrow`). `Primary` holds the ONE `btn-primary` on the
  page; everything else goes in `Actions` (a `Toolbar`, `SearchInput`, `FilterChips`). Every
  route must answer `PageContext.LabelFor` — add a fallback there before shipping a new route.
- **`SectionHeader`** titles an un-boxed region (18/Semi + its `Actions`); **`Panel`** is the boxed
  one (`Title` + `HeaderActions` + `IsLoading`). `class="panel"` is not written in pages.
- **`Notice`** is the only message box: a fact ABOUT an action or a state, in a `Tone`
  (`Tone.cs`: Negative / Warning / Positive / Info / Muted). Field validation (400/409/422)
  stays next to the field as `FormField Error="…"`; the app-wide error is `ErrorToast`. No
  hand-rolled `rounded … bg-negative/10` box, and no raw Tailwind colour (`amber-*`, `red-*`,
  `emerald-*`) anywhere — `warning` is a token.
- **`Pill`** is every status badge. A view never picks a status colour: it maps the enum to a
  `Tone` in `StatusTones.cs` (`status.ToTone()`) and that mapping is the status vocabulary.
  Domain badges (`ComplianceStatusPill`, `LeadStagePill`, `StatusPill`…) are one-line `Pill`
  wrappers. With `OnClick` a Pill is the status's transition menu trigger.
- **`FormField`** wraps every labelled control — label (14/Med white), the control wearing the
  `field` class, `Hint`, `Error`, `Required`. No bare `<label>` over an input, select, textarea
  or picker. **`Checkbox`** for a labelled tick; native checkboxes and radios get the Figma box
  from the base stylesheet.
- **`RecordsTable`** is a list of records: it owns the panel, the scroll box, `IsLoading` and
  `IsEmpty`/`EmptyMessage`; the view writes the `<thead>`/`<tbody>` inside it. Every `<table>`
  wears `data-table` (sticky header, canvas header row, 48px rows; `data-table-dense` for long
  registers); cells carry only alignment/width classes — never padding, background or colour.
  A clickable row is `tr.is-clickable`, a totals row is `<tfoot>`. `SortableColumnHeader` is the
  header cell of any sortable column. Dates render through `DateText`/`DateTimeText`
  (`DateFormats.cs`, global using), money through `Money`/`WholeMoney`.
- **`TabRow`** (links, underline) switches sibling views by navigating or by pane; **`FilterChips`**
  (buttons, pills) narrows the rows on screen. The difference is visible on purpose. A page never
  defines a `*TabClass`/`*ChipClass` helper — the classes are `tab`/`tab-active` and
  `chip`/`chip-active` and only these two components (and `WorkspaceSectionNav`/`RecordTabBar`)
  render them.
- **`SearchInput`** is every search box (debounced, Escape clears). **`StatTile`** is every
  labelled figure (`IsLoading`, never a placeholder zero); `MetricStat` the un-boxed headline
  figure with delta. **`EmptyState`** is every "No … yet" line, rendered only once its region has
  loaded.
- **`ConfirmDialog`** (a `Modal` preset, `Danger` for the irreversible) and **`InlineConfirm`**
  (the two-click armed button, disarms on blur or after 5s) are the only ways to confirm; a
  page never holds its own `confirming*`/`*Armed` bool.
- **Buttons**: `btn-primary` once per view or dialog footer; `btn-secondary` (grey outline, white
  text) for everything else, `text-negative` on it for destructive; `btn-lg` in dialog footers
  (a `Modal` footer upsizes automatically); `btn-icon` inside a `Toolbar`. Never a hand-rolled
  button class string, never a green outline, never a red fill.
- **Shape and type**: corners are `rounded` (4px) on controls, `rounded-lg` on the modal only,
  none on panels/cards/tables; no shadows except the modal (and the dropdown until its Figma
  frame is read); nothing below `text-xs`; no `uppercase`/`tracking-*` — a label is the `eyebrow`
  class (14/Med G5) or `FormField`'s label.

## Labour settlement & the Xero coding run (api)

- **The run codes one settlement party at a time, never one worker.** `RunXeroCodingHandler`
  (`Commands/XeroCoding`, one partial per concern) groups the month's schedules by settlement
  counterparty (`CodingParty`, 2026-09-08): a sole trader is a party of one; every worker a
  company bills on one invoice is one party, and that invoice is recoded ONCE to every worker's
  lines — status, total, VAT and attachment kept — with `XeroLineTimesheetCover.WorkerId` stamped
  per line so `SettlementScheduleBuilder` reconciles each worker on their own lines (a cover
  without a worker is the counterparty's as a whole, the pre-2026-09-08 meaning). Every gate
  (sign-off, run-once, mapping) is answered per party: one worker not ready holds the company
  bill and every outcome says who is waiting for whom. Asking for one worker on a company bill
  runs and reports every worker on it. Outcomes and run records stay per worker-month.
- **Approving a covered labour bill is the portal's, once every worker on it reads Matches**
  (`ApproveLabourBillHandler`, 2026-09-08): DRAFT → AUTHORISED through the same
  `ApproveInvoiceAsync` the allocation write-back uses, with an EMPTY instruction list so every
  line passes through untouched. `CoveredBillResolver` tells the settlement view which bill
  covers each worker and whether the rule is met (`WorkerSettlementSchedule.CoveredBill`); the
  row's "Approve in Xero" and the `approve_labour_bill` action share the handler. The outcome is
  recorded per worker as `BillApproved`, and `XeroCodingOutcomes.IsWritten` is the ONE definition
  of a written month — the run-once gate, the reset, the correction guards, the chase list and
  the table's Reset button all read it; never re-list the outcomes by hand.
- **A matched bill Xero says is gone is followed to its live re-issue** (`.Reissue`): same
  invoice number (`IXeroClient.FindBillsByNumberAsync`), same contact, same period, DRAFT /
  SUBMITTED / AUTHORISED; the predecessor's ledger lines and cover move onto the re-issue's fresh
  lines. Several recognised candidates are read fresh from Xero before the run says "two bills"
  — the ledger's status can be a night old. Of several LIVE bills sharing one number,
  `ReissueChoice` takes the one whose net is the schedule's, else the newest (`UpdatedUtc`, then
  date), and the preface says which over which; a tie is a skip. Different numbers stay a skip.
  Never stage a draft beside a voided bill.
- **A settlement variance follows the cover of the line it names** (`SettlementVariances`,
  2026-09-08): the builder nets it into that worker's month (`PostedVariance`), so
  `Difference`/`Verdict` read what is still unexplained. A variance with no line has no month —
  the connector notes tell the accountant to post against `coveredBill.lineIds`.

## Work Order bills on the Cost allocation page (api + jpms)

- **A Work Order bill is coded from its order — the one place the ORDER drives the invoice.**
  Everywhere else the invoice drives the order (`WorkOrderInvoiceRecoding`: linking recodes the
  order's lines to the invoice's centre); `ApproveWorkOrderBillHandler` (2026-09-08, the
  accountant's ask) never calls it. `WorkOrderBillRecognition` runs on every unallocated read
  beside `LabourSupplierRecognition` and stamps `XeroLedgerLine.WorkOrderMatch` / 
  `WorkOrderExceptionReason` per BILL (decided once per invoice, memoised): the labour registry
  wins → the supplier resolves to its directory record through `DirectoryXeroMatcher` → a WO
  number on the bill (`WorkOrderBillReference`: Reference, then descriptions, then invoice
  number; supplier + number, since numbers are per project; the bill's site breaks a tie — the
  project set on the bill in the portal first, else its Xero Sites hint, the sweep's own
  precedence, 2026-09-09) → else exactly one open order → the value gate. "Open" = Released with remaining value
  > 0 (decision 2026-09-08). Nothing is persisted for the match, so Sync and Re-check re-run it
  for free; the sweep (`AllocateSuggestedXeroLinesHandler`, page button and nightly worker
  alike) skips matched bills exactly as it skips labour lines.
- **One bill may pay several of the supplier's open orders, as a figure per order off the
  BILL TOTAL — never off the Xero lines** (2026-09-09, the accountant's ask: the lines are the
  supplier's own CIS labour / materials split and are left exactly as raised). The card is one
  `WorkOrderBillOrderSlice` per open order (`WorkOrderBillMatch.ProposedSlices` seeds it, the
  same on every line of the bill; `SupplierOrders` lists the orders) and checks the figures tie
  to the bill's net. The read proposes them: the whole bill on the matched order; a slice per
  order when the lines' descriptions name two or more different orders
  (`WorkOrderMatchRule.ByLineReference`, `…Recognition.ByLine`; an unnamed line goes with the
  bill's reference, else the supplier's only order, else the first named order, and the detail
  says so); a bill whose reference names several orders reaches the card on the first, gated
  against the orders' COMBINED remaining value. Otherwise the gate is per order. At approval
  `WorkOrderBillSliceSpread` spreads each slice over the lines pro rata (penny-safe per line,
  the drift settled on the largest line so every order is exact too) and each line's portion
  over the order's cost codes — portal-side only. `WriteBackWorkOrderBillAsync` passes
  `keepLinesWhole`: when every line lands on one centre the tracking is written as before; when
  any line would need two tracking values the bill is approved in Xero with NO tracking at all
  (`XeroWriteBackOutcome.Note` = `WorkOrderBillTracking.NotWrittenNote`, the line's `Note` gains
  "no Xero tracking", the card says so before Approve via `WorkOrderBillTracking.CanBeWritten`).
  The accountant's rule (2026-09-09 15:08): lines exactly as raised beats the tracking — the
  order split is the portal's work-order links, Xero tracking is a convenience. Never split a
  Xero line for a Work Order bill.
- **Approve is per bill, undo is per bill, both FD/Director/Admin only** (`WorkOrderBillRoles`).
  Approve re-runs the match server-side, refuses an order that is not the supplier's, figures
  that do not add up to the bill, and a slice over its order's remaining value, stamps every
  line `Note = "Work order WO-0026"` (or "Work orders WO-0055, WO-0056"), writes one
  `XeroLineWorkOrderLinks` row per line share — order AND
  `CostCenterCode` (nullable; hand links leave it null; the unique index is (line, order, code)
  since `AddXeroLineWorkOrderLinkCostCenterCode`) — and one `WorkOrderBillApprovals` row per
  order with that order's slice as `BillNet` (the audit's "which rule matched" and the undo's
  handle; `WorkOrderBillApprovalStamp.Orders` lists them), then `IXeroWriteBackService.WriteBackWorkOrderBillAsync`
  (tolerates an AUTHORISED-unpaid bill via `XeroApprovalRequest.RecodeApproved` — the
  re-approval after an undo). Undo reverses lines, splits, links and package slices in one
  save and clears the tracking off the bill in Xero (`IXeroClient.ClearTrackingAsync`, by bill,
  not by line id — a split approval replaced the Xero lines); **Xero never un-approves**, so
  the outcome and the toast say the bill stays awaiting payment there. Never void from the undo.
- **Links may sit on a same-project centre split since 2026-09-08** (a Work Order bill against a
  multi-code order). `KeepOrClearLinksAsync` keeps links through a same-project re-cut and
  recodes the orders only for a whole-line move; `WorkOrderLinkSlices` expands a split line's
  link into one slice per share for the financial summary — a link that carries its own
  `CostCenterCode` is one slice on that centre outright. A cross-project split still clears.
  The WO Allocation tab's hand link (`SetXeroLineWorkOrderLinks`) still refuses centre splits.
- On the page the tab is a sub-view of Unallocated like Labour (`workOrderBillsTab`, token
  `WorkOrderBills` in the tab memory); the cards (`WorkOrderBillCard` + `…OrderSlices` — the
  figure per order — and `…LinesTable`, read-only) render instead of the table; `notWorkOrderBillInvoiceIds` is
  the this-visit escape to the plain queue; the Allocated row's Undo becomes "Undo bill"
  (`ConfirmDialog`, Danger) when `WorkOrderApproval` is set.

## Xero write-back state on a ledger line (api + jpms)

- **`InvoiceStatus` is what Xero holds; `WriteBackStatus` is what the portal did.** Two facts,
  never one: `None` covers "approved outside JPMS" AND "still draft, nothing written yet", so a
  "still draft in Xero?" question reads `InvoiceStatus` (`IsAwaitingApproval`), never the
  write-back status. Sync refreshes `InvoiceStatus`; every write (`XeroWriteBackService`:
  approval, site write, tracking clear) stamps Xero's `FreshStatus` back onto the lines through
  `StampXeroStatus` so the ledger does not wait a night to agree with Xero.
- **A failure is never forgotten by the success that cures it** (2026-09-08, the accountant's
  ask): `WriteBackError` + `WriteBackFailedAtUtc` are the LAST failure and survive a later
  Approved / None; only a fresh failure rewrites them and only the Work Order bill undo clears them. The
  Allocated row reads "Approved in Xero by JPMS · Earlier attempt failed <when>: <error>"; the
  `Draft in Xero` / `Write-back failed` chips (`XeroAllocation.XeroState.cs`, `FilterChips`) and
  the export's Xero status / Write-back / Last write-back error columns read the same fields.

## Directory ↔ Xero links (api + jpms)

- **A directory record's Xero link is one `SubcontractorXeroLinks` row, written three ways and
  read one way.** Import from Xero writes it with a NEW record; `LinkDirectoryRecordToXeroContact`
  (2026-09-08, the accountant's ask) writes it onto an EXISTING record — the one-field change that
  replaces "import a duplicate, then Consolidate"; consolidation moves it to the master. Both
  sides must be free: a record already holding a link, or a contact linked to another record, is
  refused naming the holder, and `UnlinkDirectoryRecordFromXeroContact` is the only way to free
  one. `Subcontractor.XeroLinks` carries the contact id/name and who linked it; `XeroLinked` stays
  the bool every list reads. Every link/unlink is audited (`DirectoryRecordXeroLinkChanged`).
- **Name matching between the directory and Xero is `DirectoryXeroMatcher`, which IS
  `WorkerDirectoryMatcher`** — one rule for every "does this name mean that company" question.
  `ListXeroSuppliers` stamps each unlinked contact with the ONE unlinked record it matches
  (`MatchingSubcontractorId`), which is what the import modal's "Link to …" and the record page's
  "Suggested" read; several matches stamp nothing. The connector's `list_unlinked_directory_records`
  shows every candidate, and a match is a suggestion a human confirms — nothing links by itself.
- **Xero's primary person is read, and details move only on request** (2026-09-09, the
  accountant's ask). `XeroSupplier.PrimaryPersonName` is the contact's own FirstName + LastName
  (Xero's "Primary person"), held apart from `ContactPersons`; `XeroDetailsPull.PrimaryPersonOf`
  is the one reading (primary person, else the first additional person) the import and the link
  share. Linking never touches the record unless `LinkDirectoryRecordToXeroContact.
  PullDetailsFromXero` is asked for — a choice, never automatic, because Xero's details are
  often older than the directory's — and then `XeroDetailsPull` copies only where Xero has a
  value and adds Xero's people as contacts where the record lacks them.
- **Contacts push to Xero when a person presses it** (`PushDirectoryContactsToXeroContact`,
  `api/Features/Subcontractors/XeroContacts`). One rule, `XeroContactPushPlanner`, plans both the
  preview (`PreviewXeroContactPush`, the record page's modal: Xero's people NOW beside AFTER) and
  the write, read fresh from Xero each time (`IXeroClient.GetContactPeopleAsync`): the record's
  primary contact → Xero's primary person, its company contacts → Xero's additional persons
  (Xero replaces the list wholesale, five at most), and an EMPTY side on the portal never clears
  Xero's. Names go as first + last split on the first space (`XeroClient.NameParts`). Needs the
  Cost Integration app's `accounting.contacts` scope; a 403 comes back saying so. Audited
  (`DirectoryContactsPushedToXero`). Never call the push from a handler.

## Directory: CIS verification & the compliance register (api + jpms)

- **The HMRC CIS verification result is three fields written together** (2026-09-09, the
  accountant's ask): `CisStatus` is the SHORT reading only ("Verified 20% standard", 64 chars);
  the verification number ("V1415495651") and the verified-on date are `CisVerificationNumber` /
  `CisVerifiedOn`, and `RecordCisVerification` (the record page's "Record verification…", the
  connector's `record_cis_verification`) is the ONE writer of all three. `UpdateSubcontractor`
  keeps its `CisStatus` parameter and refuses one over 64 characters with a 400 that points at
  `RecordCisVerification` — never squeeze the number and date into the status again. On
  consolidation the number and date follow whichever record's status was chosen.
- **Compliance standing is per company and reads in one order.** A company's standing is the
  worst status among its current documents, Missing when it holds none
  (`ComplianceOverviewReadModel.WorstStatusFor`); `DirectoryComplianceFilter.WorstFirst`
  (Expired → Expiring soon → Missing → Current) is the order every compliance list and chip row
  reads in. The Directory's Compliance `FilterChips` and the register at `/directory/compliance`
  (`ComplianceRegister` page, `ComplianceRegisterRow.Build`: one row per current document plus
  one Missing row per empty company) both read it; the two views are siblings joined by
  `DirectoryViewTabs` (a `TabRow`), and the dashboard's "Documents expiring" tile lands on the
  register. `CisVerificationPanel` sits directly above `SubcontractorComplianceList` on the
  record page — the two together are the record's standing to be paid.

## The connector mirrors the page — every button the accountant gets, the assistant gets (api)

- **A feature is not done until the MCP connector can do it too** (2026-09-09, the coverage
  audit). Every command that gains an endpoint gains an `AiAction` in the same commit
  (`api/Features/Ai/Tools/Actions`, one partial per area — `CommercialActions.WorkOrderBills`
  is the Work Order bill approve/undo, `ApprovedBy`/`UndoneBy` stamped from the caller); every
  page read the model would need to make that decision is on a read tool (`list_xero_ledger_lines`
  carries `workOrderBill` — the card's orders, proposed slices and the supplier's open orders —
  `workOrderExceptionReason` and `workOrderApproval`; `list_compliance_register` is
  `/directory/compliance`; `preview_xero_contact_push` is the push modal's now/after;
  `search_directory` carries `complianceStanding` and the CIS fields). Pin each batch in
  `AiConnectorTests` (`…_reachTheConnector`) so a rename never drops one. A page-only feature
  is a gap the accountant finds first.
- **A context read hands over the ids its actions take** (2026-09-10, the accountant's ask:
  `get_bid_package_context` listed the tender list as company + status, so
  `decline_bid_package_recipient` could never be given the `recipientId` it wants). Every list a
  `get_*_context` tool returns carries the id the matching action takes — `tenderList[]` has
  `recipientId` + `subcontractorId`, `quotes[]` has `quoteId`, `lineItems[]` has `lineItemId`,
  `linkedDocuments[]` has `drawingId` — and the action's `Notes` name that field
  (`tenderList[].recipientId — never the company name`), not "the recipient list". Code:
  `AiRecordTools.BidPackageContext.cs` + `BidPackageContextReads.cs`; pinned by
  `BidPackageContext_handsOverTheIdsItsActionsTake`.

## The sales invoice raised in Xero from the claim card (api + jpms)

- **Issue IS raise-in-Xero** (2026-09-09, the accountant's ask: Cert 15 was raised, tracked and
  had its PDF attached by hand). The claim card's "Raise in Xero & issue…" and the invoices
  section's menu open `ValuationInvoiceXeroRaiseModal`, which shows the plan first
  (`PreviewValuationInvoiceXeroRaise`) and then runs `RaiseValuationInvoiceInXero`: one
  AUTHORISED ACCREC invoice on **the Xero contact mapped on the project** (`Projects.XeroContactId`
  / `XeroContactName`, picked from Xero's contacts in Project settings → Edit details, beside the
  Sites option; 2026-09-10, after a by-name match created a duplicate contact — the raise never
  matches by name and never creates a contact, and an unmapped or not-found contact is a
  blocker), one line for the invoice's cash `Amount` on `XeroOptions.SalesAccountCode` ("200"
  unless `Xero__SalesAccountCode`), the project's `XeroSiteName` as Sites tracking (no cost code
  on income), Xero reference `Valuation NN` and description `Valuation NN - Payment due as per
  <Month yyyy> valuation report (ex VAT)` numbered from the INVOICE (never the claim), and the
  dates the user gives per call (`InvoiceDate` / `DueDate` on the preview and the command; blank
  = today, and certificate issue date + the contract's `FinalDateForPaymentDays` else Xero's
  sales default — `DueDateNote` says which applied). **The VAT treatment is never assumed**:
  `XeroClient.ResolveSalesContactAsync` — the contact's `AccountsReceivableTaxType`, else their
  most recent ACCREC invoice, else Xero's account default — and the note says which, in the
  preview, the outcome and the audit event.
- **Nothing is lost between Xero and the portal.** `RaiseValuationInvoiceInXeroHandler` plans
  (every blocker named before anything is touched), raises, stamps `XeroInvoiceId` /
  `XeroInvoiceNumber` / `XeroRaisedAt` and SAVES, then attaches the register's newest
  certificate for the claim (`IXeroClient.AttachToInvoiceAsync`, best effort — the outcome's
  `AttachmentError` and a `RaisedInXero` audit event say when it did not), then calls the
  existing `IssueValuationInvoice` handler so the issue rules, snapshot re-freeze and certified
  totals are the one implementation. An invoice carrying a Xero id OR number is refused a
  second raise; "Issue without raising in Xero" (`IssueValuationInvoice`, now with an optional
  `XeroInvoiceNumber`) stays for one raised by hand — the number is stamped with `XeroRaisedAt`
  and `XeroInvoiceId` stays null (`ValuationInvoice.IsRaisedInXeroByPortal`) — and
  `RecordValuationInvoiceXeroNumber` (`record_valuation_invoice_xero_number`, "Record Xero
  number…" on the invoice row) back-fills the number on an Issued/Paid row; it refuses one the
  portal raised. Xero never un-raises — a wrong invoice is voided in Xero. Connector:
  `preview_valuation_invoice_xero_raise` + `raise_valuation_invoice_in_xero` (confirm-first,
  `RaisedBy` stamped); the preview tells the model to STOP on a missing contact mapping and to
  fix it in Project settings (or `update_project_details` with the user's yes). Needs the Cost
  Integration app's `accounting.attachments` scope for the PDF.

## Reading scans — the assistant's document reader (api)

- **A PDF with no text layer is a scan, kept and read — never refused** (2026-09-09, the
  accountant's ask: Quarry's certificates and the executed contracts are all scans).
  `AiSourceReader.LoadPdf` returns the document with `ScanBytes`; `ScannedPdfReading.FillAsync`
  (called by `AiSourceTools.LoadAsync` on every open) OCRs every page through `IDocumentOcr`
  (`AzureVisionOcr` when `DocumentOcr__Endpoint` / `__ApiKey` are set, `NullDocumentOcr`
  otherwise) and caches the result in `DocumentOcrResults` by the file's SHA-256. OCR text is
  FLAGGED: `AiSourceDocument.TextSource = "ocr"` with `OcrConfidence`, on the manifest, in every
  `read_source` result (`text_source`, `ocr_confidence`, a note to say figures came off a scan)
  — never passed off as an extracted layer.
- **Any page of a scan can be SHOWN as a picture** — `ScannedPdfPages.Render` (pdfium via
  Docnet, 150 dpi, `PngEncoder`), returned through `AiImageToolResult`. `read_source` shows the
  page when asked (`as_image`), when its OCR is missing or below
  `ScannedPdfReading.TrustedConfidence`, and — with no OCR at all — for the first page by
  default; `find_in_source` says so instead of searching nothing. The flat
  `AiAttachmentReader.Extract` (tender extractor) still refuses a scan, naming the route.
- **A refusal names the format and the route that works** (`AiSourceReader.RefusalFor`): .xls →
  Save As .xlsx or upload to the chat; .doc → .docx; .msg → open the email; .zip → name the file.
  Never a bare "unsupported".

## Loading states (jpms)

- **Never render a figure, a row count or an empty state from a store that has not loaded.** A `0`
  that silently becomes `47` a second later is worse than no number: the reader has already believed
  it. Stores expose `IsLoaded`; read models expose a nullable `Current` (null = no fetch has landed).
- **The pulsing jewel is the only loading mark, and `LoadGate` is the only thing allowed to draw
  it.** `JewelSpinner` is a private part of the gate — a view that renders one directly is a mark
  no ancestor can silence, which is exactly how a screen ends up with two of them. `Panel`
  (`IsLoading`) and `RecordsTable` (`IsLoading`) are gates too; they wrap one.
- **Two shapes, and the question that picks between them is "is anything on screen yet?"**
  - `<LoadGate IsLoading="…">` — the COVER. Nothing to show: the gate holds the region's space and
    puts the jewel in it. `Prominent="true"` for a main panel (roughly a third of the screen or
    more). This is a first load.
  - `<LoadGate Overlay="true" IsLoading="…">` — the REFRESH. A previous answer is on screen and is
    being replaced or acted on (a sort, a pager, a filter, a command in flight): the content stays
    put, an opaque veil takes the clicks, and the mark rides in a chip that sticks to the visible
    slab of the region — a 3,000px queue must not centre its spinner 1,500px down.
- **One mark per wait — the gate is relational.** A gate showing its mark cascades a claimed
  `LoadScope`; every gate INSIDE it holds its space and says nothing until the claim lifts. So a
  page cover silences the panels beneath it and a workspace-wide busy overlay silences the list
  refreshing under it, without either knowing what is nested inside. Nothing cascades sideways, so
  the rule left to the call site is: **a screen with nothing to show gates ONCE**, around the region
  the reader is waiting for — `LoadState.UntilAll(a.IsLoaded, b.IsLoaded)` composes the sources.
  Two sibling panels that fill from the same page open share one gate around their container; they
  keep their own only when one of them can load while the other is idle.
- **The whole-page mark belongs to the boot, and lives in `wwwroot/index.html`.** It is a sibling
  overlay of `#app` (not inside it, which is what made Blazor wipe it a beat too early), taken
  down through `BootScreen.DismissAsync` (Services; idempotent, never throws) by whichever of
  three components first has something real to show: `ApprovedSessionGate` for every page behind
  it, `LandingLayout` for the routes outside it, `ReportingErrorBoundary` when a page render has
  failed under it. `js/boot-screen.js` owns the overlay and a 12s failsafe that removes it once
  the app has rendered anything — longer than a cold API session check on purpose, because a
  failsafe firing mid-check swaps the boot jewel for the gate's, the very flicker the overlay
  exists to remove. `js/blazor-start.js` is the manual `Blazor.start` with download retries; when
  boot itself fails it swaps the overlay's "Loading" line for the `#boot-failed` template in
  `index.html`. There is no in-app full-page loader: `ApprovedSessionGate` reads
  `SessionService.IsLoaded` synchronously, so an in-app navigation never flashes a session check
  nobody is waiting on.
  **`index.html` and the assemblies are fetched separately**, so a client can run today's DLLs
  against yesterday's shell (JPMS-4BF13E, 2026-09-08 — a deep link served a cached shell that
  predated `js/boot-screen.js`). `staticwebapp.config.json` sends `no-cache, must-revalidate` on
  every path (`/*`), not only the literal `/index.html`, so deep links served through the
  navigation fallback revalidate too. Any NEW JS global that .NET calls must tolerate being absent
  until every client has refetched the shell.
- **Restraint: one jewel per screen.** A gate is for a REGION that will definitely render something
  and occupies real space. In particular:
  - **Never gate a control.** A filter, a picker, a form field: render it `disabled` with a
    "Loading…" placeholder option instead. That says "not ready" in the control's own language,
    holds the layout still, and cannot be used to make a wrong choice.
  - **Never gate a single line of text.** A count or strapline simply does not render until it is
    known — a line of muted text arriving late is invisible, a spinner in its place is an event.
  - **Never gate a conditional panel.** If the panel only exists when there is something to show,
    its absence during the load is the same as its absence after it; a gate there announces a wait
    for something that usually never arrives.
- **`sessionReady` is not `dataReady`.** A page's auth flag says the session has been checked and
  the user is signed in — nothing more. It gates the RequestAccessView branch and the page chrome
  (title, intro, footnotes, tab nav), which need no data. Every data-bearing panel gates on its own
  sources. Naming the flag `isLoaded` and setting it before the awaits is what produced the
  zero-then-value flash this convention exists to prevent.
- **A failed fetch must open the gate.** Pair each gate with a `dataFailed` flag set in a
  `try/catch` around the awaited queries (and `|| X.LastRefreshFailed(id)` for read models that
  record failure rather than throwing), so the panel says what went wrong instead of pulsing
  forever. The error toast already carries the reference and the detail.
- **Backing fields are nullable, not `Array.Empty<T>()`.** An empty list is a real answer that sums
  to a real-looking zero; `null` is the only honest "not fetched yet". Expose a non-null accessor
  (`Rows => rows ?? Array.Empty<T>()`) for the computations and gate on the nullable field.
- Signals to gate on: `IsLoaded` / `LoadedFor(key)` / `XxxLoadedFor(key)` on stores and read models,
  `AsyncQueryCache.Has(key)` underneath most of them, or `Current is not null` on a read model.
  If the signal you need is missing, add it — do not gate on a proxy that happens to correlate.

## Error reporting (jpms)

- `ErrorReporter` holds the single current error; `ErrorToast` renders it full-width along the top
  of both layouts. One at a time, newest wins.
- Every report carries a short reference (`JPMS-7F3A2C`), the time, the signed-in user, the page,
  the endpoint + status, the server's own message and the stack — copyable in one click, so a user
  can forward something actionable rather than "it went red".
- What reaches the toast: all query failures, command failures **except** 400/409/422 (those are
  validation answers the calling dialog already shows next to the field), and any unhandled
  exception — caught either by `ReportingErrorBoundary` in `App.razor` or by
  `ErrorReportingLoggerProvider`, which watches the framework's own error logging because Blazor
  WASM has no usable `AppDomain.UnhandledException`.
- Blazor's `#blazor-error-ui` strip is last-resort only: it appears when the renderer itself has
  stopped and the only honest option left is Reload.

## Front-end data-loading convention (jpms, Blazor WASM)

- Stores that back synchronous render-time reads (e.g. `ForProject`, `LinesFor`, `PackagesFor`) fetch at most once per key to avoid render → fetch → render loops. Every project tab page must therefore call the store's `Refresh(projectId)` once from `OnInitializedAsync` (never from render) so navigating between tabs revalidates cached data in the background (stale-while-revalidate). Follow this pattern when adding new tabs or stores.
- The router (`App.razor`) uses `KeyedPageRouteView`, which keys each page by its type + route parameter values. Navigating between two URLs of the same route template (e.g. the project header's prev/next arrows) therefore recreates the page and re-runs `OnInitializedAsync`, so the convention above fires there too — pages never need `OnParametersSetAsync` guards for route-value changes.

## Project ordering (jpms)

- **Every list of projects is in one order: live work first.** `ProjectOrdering.InWorkOrder()`
  (contracts/Models) sorts by a coarse four-band rank — Pre-Construction/Procurement/Mobilisation/
  Live Delivery/Close-Out (0) → Defects Period (1) → Lead (2) → Completed (3) — then A–Z by name,
  then by reference. The bands are deliberately coarse so a project moving from Procurement to
  Mobilisation does not jump the list mid-build.
- It is applied **once**, in `ListProjectsVisibleToUserHandler`, so everything reading
  `ProjectListReadModel` inherits it. Callers that narrow the list (`.Where(Stage != Completed)`)
  re-apply `.InWorkOrder()` after the filter; nothing sorts projects by its own rule. If a list
  needs a different order, it needs a reason written next to it.
- **Completed projects are ordered last, not hidden** — except from the side-nav switcher, the
  header's prev/next cycle and the finance overview, which are about work in progress. The
  switcher carries a per-user "Show completed" toggle (`ProjectStageFilter`, decision 2026-08-03)
  that adds completed projects back into the picker, the prev/next cycle and project-scoped
  navigation (`CurrentProjectService.ResolveFor(projects, includeCompleted)`) so their records —
  the valuation report above all — stay reachable after handover; the finance overview ignores
  the toggle. Anywhere costs or history are recoded (Xero allocation, audit trail) the full list
  stays available.
- `SearchSelect` already leads its unfiltered list with a blank entry labelled with its
  `Placeholder`, which *is* the clear/"All …" row. Do not prepend another one — that is what put
  "All projects" in the Xero allocation filter twice.

## Database migrations (prod)

- **Every schema change ships with its apply commands, immediately.** When work adds an EF
  migration, hand the user the exact ready-to-run commands in the same reply as the code — never
  leave the database update as a follow-up. The database is updated *before or with* the deploy
  (additive/expand first), because people are using the system and the deployed code must never
  query columns that don't exist yet. That is exactly what broke sign-in on 2026-07-30.
- **Scoped scripts only — the full idempotent script is permanently broken against prod.**
  `20260702170000_SeparateArchitectsFromClients` embeds raw SQL reading `Clients.ArchitectEmail`,
  which a later step drops; SQL Server fails that batch at *compile time* on any database where
  the column is already gone, so `dotnet ef migrations script --idempotent` (unscoped) can never
  run again. Always generate from the last applied migration:
  1. `sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -Q "SELECT TOP 1 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC"`
  2. `cd api && dotnet ef migrations script <that-id> --idempotent -o migrate.sql`
  3. `sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -i migrate.sql -b -o migrate.log`
  4. Read `migrate.log` — `-b` stops at the first error, and "completed" printed by an *earlier*
     script run proves nothing about the current one.
- **Raw SQL inside migrations must survive the column being dropped later.** Wrap data-moving SQL
  in `EXEC sp_executesql N'...'` so it compiles only when the guard actually runs; inline raw SQL
  referencing columns that a later migration drops is what poisoned the full script.
- One-off data fixes (seeds, role grants, remaps) stay as reviewed scripts under `infra/` /
  `scripts/` run via sqlcmd — they are not EF migrations and must never touch schema.

## Never commit package caches or bundles

`_nuget/`, `_nuget_transfer/`, `*.bundle` and `_copy-nuget-cache.sh` exist only so a sandbox can restore
NuGet offline or receive a branch; they are gitignored. If a build-verification step copies a NuGet cache
into the repo under any other name, add that name to `.gitignore` in the same commit — 400 MB of tarballs
in history made every push take an hour on 2026-09-06.
