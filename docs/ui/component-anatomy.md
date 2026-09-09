# Component anatomy — every JPMS view

Read out of the razor source on 2026-09-03 (526 files under `jpms/Pages`, `Components`,
`Layout`, `Features`; 89 routed views). Companion to `DESIGN-SYSTEM.md`: that file says what
the tokens are, this one says what the views are built from and which shared components are
missing.

**Marks.** ✅ a real reusable component is rendered · ⚠️ hand-rolled markup that recurs across
views (extract it) · 🔒 one-off markup local to that view.

## Where it stands (2026-09-06)

The Stage 1 rounds (`docs/ui/stage-1-components.md`) built and rolled out the spine below: every
⚠️ for AuthGate, Header, AlertBanner, StatusPill, EmptyState, SearchInput, ChipTabs/SegmentedTabs
(as `TabRow`/`FilterChips`), RecordsTable (the shell + skin), FormField and InlineConfirm is now a
real component — `Page`, `PageHeader`, `Notice`, `Pill`, `EmptyState`, `SearchInput`, `TabRow`,
`FilterChips`, `RecordsTable`, `FormField`, `Checkbox`, `SectionHeader`, `StatTile`,
`ConfirmDialog`, `InlineConfirm`. The per-view trees below describe the views as they were
surveyed on 2026-09-03 and are kept as the map; read a ⚠️ in them as "was hand-rolled, now the
named component". Still open: `KeyValueList`, `LedgerStatementRow`, `StatusTransitionMenu`,
numbered `Pagination`, the generic column-model table, `PageLoadScope`, `NoticeService`.

## The five duplications worth fixing first

| Pattern | Where it stands |
|---|---|
| **Auth preamble** | 52 of 89 pages hand-roll `!isLoaded → LoadGate` / `!IsApproved → RequestAccessView`. `ApprovedSessionGate` already does it; 24 pages use it. |
| **Page header** | 68 of 89 pages build their own eyebrow / title / subtitle / action cluster. No `PageHeader` component exists. |
| **Status pill** | `Components/StatusPill.razor` exists and **no page uses it**; 24 pages hand-roll `rounded-full` spans. |
| **Alert banner** | Reported by every survey pass, often 2-3 per page. Classes drift each time. Nothing owns it. |
| **Records table** | 29 pages hand-roll a `<table>`. `SortableColumnHeader` exists and no page uses it. Seven domain tables were extracted; the generic one never was. |

`LoadGate` (70 pages) is the counter-example: one component plus a written rule got adoption.

## Proposed component spine

**1 · Shell** — `ApprovedSessionGate` (idle, 24/76) · `ProjectPageShell` (33) ·
`WorkspaceSectionNav` (9) · `RecordTabBar` (2)

**2 · Page chrome** — `PageHeader` (build, 68) · `ActionCluster` (build, ~40) ·
`Toolbar`/`ToolbarButton` (have, 10) · `SectionPanel` (idle — `Panel` exists, 13)

**3 · In-view navigation** — `ChipTabs` (build, ~30; decide links vs buttons) ·
`SegmentedTabs` (build, ~12) · `SearchInput` (build, ~15) · `FilterBar` (build, ~14)

**4 · Data display** — `RecordsTable` (build, 29) · `StatusPill` (idle, 0/24) ·
`StatTileGrid`/`Stat` (idle, 1/~8) · `LedgerStatementRow` (build, ~6) ·
`KeyValueList` (build, ~12) · `EmptyState` (idle, 2/~35)

**5 · Feedback** — `AlertBanner` (build, ~60) · `LoadGate` (have, 70) · `ErrorToast` (have)

**6 · Forms, dialogs, destructive acts** — `Modal` (have, 26) · `FormField` (idle, 10/26) ·
`InlineEditPanel` (build, ~8) · `InlineConfirm` — the two-click armed delete (build, ~6) ·
`StatusTransitionMenu` (build, ~4)

Two rules the codebase has not written down and needs: **when CRUD is a modal and when it is an
inline panel**, and **whether a chip row is links or buttons**.

---


## Project

### RFIs — `/projects/{id}/requests` · `/projects/{id}/requests/{kind}`
`Pages/ProjectRequests.razor` (280 lines) + `.razor.cs` — the project's RFI register; legacy General requests one tab behind. **Note:** unlike most pages this one hand-rolls its own `!isLoaded`/`RequestAccessView` preamble instead of `ApprovedSessionGate`.

- AuthGate ⚠️ (hand-rolled, not the ApprovedSessionGate component) → LoadGate ✅ / RequestAccessView ✅
- ProjectPageShell ✅ (ActiveTab="requests")
  - Header 🔒 — Title "RFIs" · Subtitle "N RFIs · N overdue · N legacy requests" · ExportToExcelButton ✅ (include-all) · PrimaryButton "Raise RFI" 🔒
  - ChipTabs ⚠️ — document type (RFIs · General), links styled as pills
  - ExplainerPanel ⚠️ — document-type copy, follows active tab, with AlertBanner (negative) ⚠️ overdue-RFI warning nested inside
  - ChipTabs ⚠️ — status + counts, with SearchInput ⚠️ pushed right, plus match-count/"show all" strapline
  - Contextual action bars 🔒 — bulk email-draft prep panel (selection count + PrimaryButton) and 2-way merge panel (radio survivor picker + PrimaryButton), each its own bordered box
  - AlertBanner 🔒 ×3 (merge error/result, draft-batch error) + draft-batch outcome list ⚠️
  - EmptyState 🔒 / RecordsTable ✅ (`RequestTable` component — SelectAllCheckbox, StatusPill cells, RowActionMenu status-change, per-row ActivityFeed lookup)
  - Modals: RaiseRequestDialog ✅ (RFI-locked)

### Request Detail — `/projects/{id}/requests/view/{requestId}`
`Pages/ProjectRequestDetail.razor` (263 lines) — a single request/RFI's document, correspondence and lineage.

- AuthGate ⚠️ (hand-rolled, three states: !sessionReady → !Session.IsApproved → !dataLoaded) → LoadGate ✅ / RequestAccessView ✅
- ProjectPageShell ✅ (also wraps the not-found and loading branches)
  - Back link "← Request register" 🔒
  - RequestHeaderBar ✅ (Features/Requests — status menu, action menu, email/promote actions)
  - RecordTabBar ✅ — Request → RFI → Variation, local panes + ?tab=official deep link
  - RequestFactsStrip ✅
  - AlertBanner (negative) 🔒 — actionError
  - CriticalPathNudge ✅ — overdue-answer nudge
  - 2-col grid 🔒
    - Left: Panel "Detail" ✅ · RequestOfficialFormPanel ✅ · RequestResponsePanel ✅ · RequestAttachmentsPanel ✅ · RequestConversation ✅ · RecordAuditHistory ✅ (position depends on tab state)
    - Right: RecordAuditHistory ✅ (alt slot) · RequestPartyPanel ✅ · RequestVariationCard ✅
  - Modals: delete-request Modal ✅ · return-to-Control-Centre Modal ✅ · close-request Modal ✅ (+FormField ✅ date) · EmailDraftStagingModal ✅ · record-response Modal ✅ (+ textarea 🔒) · RequestHeaderEditModal ✅ · RequestFactsEditModal ✅ · RequestDetailEditModal ✅ · VariationDraftModal ✅

### Variations — `/projects/{id}/variations` · `/projects/{id}/requests/variations`
`Pages/ProjectVariations.razor` (412 lines) — the project's unified variation book (post `UnifyVariationOrders`), plus subcontractor-raised variation requests awaiting review.

- AuthGate ⚠️ → LoadGate ✅ / RequestAccessView ✅
- ProjectPageShell ✅
  - PageHeader 🔒 — Title "Variation Orders" · Subtitle "N open · N approved · £X approved value"
    - ActionCluster 🔒 — ExportToExcelButton ✅, PrimaryButton "Add variation manually" ⚠️ (role-gated)
  - ExplainerPanel ⚠️ — "Variation — one document, four stages" copy + AlertBanner (negative) ⚠️ "not approved money until Approved" + conditional AlertBanner (neutral) 🔒 for unlinked historic variations
  - SearchInput ⚠️ (right-aligned) + match-count line 🔒
  - AlertBanner (negative) ⚠️ — variationsError
  - StatRow 🔒 → Stat ✅ ×2 ("Variations" open/approved, "Approved value")
  - Panel 🔒 "Subcontractor variation requests" — header with open-count badge, per-request card list 🔒 (title, sub/WO/date, description, value, Accept→variation / Reject buttons ⚠️), inline reject-reason FormField ⚠️, `<details>` "Reviewed" disclosure 🔒
  - EmptyState 🔒 (no variations / no search matches)
  - RecordsTable ⚠️ — 7 cols (Ref, Title, Request, Status, Value, Issued, Approved, Work order), sticky headers, no checkboxes; StatusPill-as-dropdown cell 🔒 (click-to-change status menu), ActivityBadge ✅ next to title, "Issue WO" action link per row
  - Footnote text 🔒
- Modals: AddManualVariationDialog ✅; Modal ✅ "Decline {ref}?" — confirm-decline (ConfirmDialog pattern 🔒)

### Variation detail — `/projects/{id}/variations/{id}` · `/projects/{id}/voq/{id}` (legacy alias, same page)
`Pages/ProjectVariationDetail.razor` (302 lines, + .razor.cs) — one variation's full record: status, figures, linked AI, conversation, correspondence.

- AuthGate ⚠️ → LoadGate ✅ / RequestAccessView ✅
- ProjectPageShell ✅ (renders even while the order itself is still loading, or not found — tab chrome never flashes)
  - Back-link 🔒 — "← Originating request" or "← Variations"
  - Header row 🔒 — DisplayNumber (mono) + status pill that is also a dropdown menu (StatusPill ⚠️ doubling as a picker, custom-positioned menu 🔒)
  - Title, inline-editable 🔒 — h2 / FormField ✅ swap on "Edit"
  - RecordTabBar ✅ — Request → RFI → Variation, this tab active
  - AlertBanner (negative) ⚠️ — page error
  - AI-linked banner 🔒 — shows linked Architect's Instructions or, gated in a LoadGate ✅, "waiting on AI" notice
  - Two-column grid 🔒 (lg:col-span-2 + sidebar):
    - Left: VariationDocumentPanel ✅ → VariationLinesTable ✅ (once approved) → OriginatingRequestRepair ✅ (seeded records only) → VariationConversation ✅ → RecordCorrespondencePanel ✅ (in a Panel-shaped div 🔒)
    - Right: VariationDetailsCard ✅ → ApprovedFiguresPanel ✅ (approved) or VariationApproveOffer ✅ + StagedBuildUpPanel ✅ (pre-approval, CanManage) → RecordAgreedTenderPanel ✅ + DeleteVariationPanel ✅ (CanManage, unapproved)
- Modals:
  - Modal ✅ "Edit lines — {ref}" wrapping VariationApprovePanel ✅ (ShowFooter=false)
  - DeclineVariationModal ✅ — bound to `decliningOrder`

### Architect's Instructions — `/projects/{id}/architect-instructions`
`Pages/ProjectArchitectInstructions.razor` (298 lines, + .razor.cs) — the project's register of formal AIs and the variations each one covers.

- AuthGate ⚠️ → LoadGate ✅ / RequestAccessView ✅
- ProjectPageShell ✅
  - PageHeader ⚠️ — Title "Architect's Instructions" · Subtitle · ActionCluster ⚠️: PrimaryButton "File an instruction" 🔒 (CanManage only)
  - AlertBanner (negative) ⚠️ — load error
  - Panel ✅ (IsLoading gate)
    - EmptyState 🔒 — "No instructions recorded on this project yet."
    - RecordsTable ⚠️ — 7 cols (Ref, Title, Instructed, Issued by, Variations, Document, actions); variation links render as Badge ⚠️ pills; per-row "Link a variation…" / "Delete…" text actions 🔒
- Modals:
  - Modal ✅ "File an Architect's Instruction" — FormField ✅ ×4, textarea Notes 🔒, InputFile ✅ (document), checkbox list of variations to cover 🔒
  - Modal ✅ "Link a variation" — ShowFooter=false, list of variations with Link/Unlink SecondaryButton ⚠️ per row
  - ConfirmDialog-shaped Modal ✅ "Delete {ref}?" — confirm/cancel, irreversible-action copy

### Valuation Snapshots — `/projects/{id}/valuation-snapshots`
`Pages/ProjectValuationSnapshots.razor` (182 lines) — read-only register of frozen valuation-report snapshots (client-facing statements); managing them stays on the Valuation Report tab.

- AuthGate ⚠️ (manual preamble)
- ProjectPageShell ✅ (ActiveTab="valuation-snapshots")
  - Title "Valuation Snapshots" + Subtitle explainer 🔒
  - EmptyState 🔒 ("No snapshots yet") or RecordsTable ⚠️ — 7 cols (Captured, Snapshot label + "Superseded" Badge ⚠️, Valuation invoice ref+status/period or "Invoice deleted"/"Period-end capture", Works complete £, Certified £, Payment due £, PDF link + Email IconButton ⚠️), sticky header, row click opens viewer
- Modals: snapshot viewer Modal ✅ (Title "Valuation report snapshot", full-width) → ValuationSnapshotViewer ✅; ValuationSnapshotEmailModal ✅ (email-draft flow, role-gated)

### Drawing Register — `/projects/{id}/drawings`
`Pages/ProjectDrawings.razor` (169 lines, +.razor.cs) — the project's drawing register with folders and Bluebeam extraction.

- AuthGate ⚠️ (hand-rolled) → LoadGate ✅ / RequestAccessView ✅
- ProjectPageShell ✅
  - Header 🔒 — Title "Drawing register" + count line (drawings · folders · sub-folders · CountBadge ⚠️ "ambiguous")
  - ActionCluster 🔒 — ExportToExcelButton ✅, toggle "Showing approved/All drawings" 🔒, "Extract all" 🔒 (Bluebeam-connected only), "+ New folder" 🔒, PrimaryButton "+ Add drawings" 🔒
  - DrawingUploadForm ✅ (conditional, upload panel)
  - EmptyState 🔒 or DrawingsTable ✅ (folders + drawings, manage actions)
  - Modals: folder create/rename Modal ✅ (+ plain `<input>` 🔒, not FormField) · delete-folder Modal ✅ · extract-all confirm Modal ✅
  - AlertBanner 🔒 — extractAllNote (positive-ish info strip)

### Drawing Detail — `/projects/{id}/drawings/{DrawingId}`
`Pages/ProjectDrawingDetail.razor` (245 lines) + `.razor.cs` — one drawing's revisions, preview and extraction pipeline.

- ApprovedSessionGate ✅ (the extracted AuthGate component — the canonical one, unlike most pages in this batch)
- ProjectPageShell ✅ (ActiveTab="drawings")
  - LoadGate ✅ "Loading drawing" (register fetch), wraps:
    - failed-load AlertBanner (negative) ⚠️ with retry SecondaryButton, or EmptyState "Drawing not found" 🔒
    - or drawing content:
      - Toolbar-ish nav 🔒 — "← Back to register" link, Previous/Next IconButton-with-label ⚠️, position counter "x of y" 🔒
      - header 🔒: DrawingDetailsEditor ✅ (inline editable title/number), "Latest approved" line, folder row (ActionIcon ✅ + folder `<select>` disabled-while-loading + DrawingFolderOptions ✅ + move confirmation Badge ⚠️), ActionCluster ⚠️: SecondaryButton "Extract data" (or disabled+tooltip) ⚠️, PrimaryButton "+ Upload new version" ⚠️, SecondaryButton "Delete drawing" ⚠️
      - AlertBanner (negative) ⚠️ ×2 — deleteError, extractError
      - ConfirmDialog ✅ (Modal, "Delete drawing?")
      - DrawingRevisionUploadForm ✅ (conditional)
      - LoadGate ✅ "Loading revisions" (nested), wraps:
        - failed-load AlertBanner (negative) ⚠️ with retry
        - or 2-col layout: PdfViewer ✅ / inline `<img>` preview 🔒 / unsupported-type EmptyState 🔒 — beside DrawingRevisionList ✅
        - DrawingExtractionPanel ✅
- Modals: delete-drawing ConfirmDialog ✅ (above)

### Ambiguous revisions — `/projects/{ProjectId}/drawings/ambiguous`
`Pages/ProjectDrawingsAmbiguous.razor` (65 lines) — the drawing uploads JPMS couldn't auto-classify by filename, parked for PM action.

- AuthGate ⚠️ → LoadGate ✅ / RequestAccessView ✅
- ProjectPageShell ✅ (ActiveTab="drawings")
  - breadcrumb nav 🔒 ("Drawings / Ambiguous queue")
  - Title "Revisions JPMS couldn't auto-classify" ⚠️ + Subtitle ⚠️
  - EmptyState ⚠️ ("Nothing pending classification.")
  - DrawingRevisionList ✅

### Programme — `/projects/{id}/programme`
`Pages/ProjectProgramme.razor` (79 lines) — the project's programme, its formal claims documents (NOD/EOT/LADs), critical-path RFIs and programme-tagged correspondence.

- ApprovedSessionGate ✅
- ProjectPageShell ✅
  - Title "Programme" · Subtitle 🔒
  - SegmentedTabs ⚠️ — Programme · Claims (CountBadge ⚠️) · Critical Path RFIs (CountBadge ⚠️) · Relevant Events (CountBadge ⚠️)
  - view == Programme → ProgrammeWorkbench ✅
  - view == Claims → ProgrammeClaimsWorkbench ✅
  - view == CriticalRfis → CriticalRfiList ✅
  - view == RelevantEvents → RelevantEventsList ✅

Modals: none directly on the page (claims/NOD forms live inside ProgrammeClaimsWorkbench).

### Calendar — `/projects/{id}/calendar`
`Pages/ProjectCalendar.razor` (265 lines) + `.razor.cs` — month grid of site visits/deliveries/meetings/subcontractor attendance, CAL-#### referenced.

- AuthGate ⚠️ (manual preamble)
- ProjectPageShell ✅ (ActiveTab="calendar")
  - PageHeader ⚠️ — Title "Calendar" · ActionCluster ⚠️: SecondaryButton "‹ Prev" ⚠️, month label 🔒, SecondaryButton "Next ›" ⚠️, SecondaryButton "Today" ⚠️, PrimaryButton "Add event" ⚠️
  - AlertBanner (negative) ⚠️ — actionError
  - LoadGate ✅ "Loading calendar" / failed-load text 🔒
  - Month grid 🔒 — 7-col day-name header + week rows of day cells, each cell: day number badge, up to 3 event chip buttons (KindDot + time + title + "Client" Badge ⚠️), "+N more"/"Show fewer" LoadMoreButton-ish ⚠️
  - Upcoming section 🔒 — Title "Upcoming" + EmptyState 🔒 or day-grouped ActivityFeed ⚠️ (KindDot, time, title, kind label, multi-day "to …", Client Badge, Reference)
- Modals: event editor Modal ✅ (Title "Add a calendar event" / "Calendar event {ref}") — FormField ✅ (Title, Start time, Date, End date), inline Kind `<select>` 🔒, Notes textarea 🔒, "Visible to client" checkbox 🔒, linked-mail sub-list 🔒 (Loading… / EmptyState / ActivityFeed of emails), footer Delete/Cancel/Save SecondaryButton+PrimaryButton ⚠️

### To-do — `/projects/{id}/todos`
`Pages/ProjectTodos.razor` (44 lines) — the project's to-do tab; almost entirely delegated to one component.

- AuthGate ⚠️ → LoadGate ✅ / RequestAccessView ✅
- ProjectPageShell ✅
  - ProjectTodoList ✅ (Components) — the whole page body; owns its own data and UI

Modals: owned inside ProjectTodoList (not inspected — out of this page's own markup).

### Progress — `/projects/{id}/progress`
`Pages/ProjectProgress.razor` (295 lines) — client-facing progress reports plus the raw progress-update log with photos.

- ApprovedSessionGate ✅
- ProjectPageShell ✅
  - Section: Title "Progress reports" + subtitle 🔒, PrimaryButton "+ New report" 🔒
    - ProgressReportForm ✅ (conditional)
    - LoadGate ✅ → EmptyState 🔒 or ActivityFeed ⚠️ of report rows 🔒 (title, period, download-PDF link, Edit, two-click-confirm Delete 🔒)
  - Section: Title "Progress of the works" + count line 🔒, PrimaryButton "+ Record progress" 🔒
    - ProgressUpdateForm ✅ (conditional)
    - LoadGate ✅ → EmptyState 🔒 or ActivityFeed ⚠️ of update cards 🔒 (title, meta, description, weather line, photo thumbnail grid, two-click-confirm per-photo Delete 🔒, InputFile ✅ "Add photos" drop tile)
  - AlertBanner (negative) 🔒 — actionError
  - Modals: none (deletes are inline arm/confirm buttons, not ConfirmDialog)

### Defects — `/projects/{id}/defects`
`Pages/ProjectDefects.razor` (266 lines, no partial — code in `@code` block) — the project's defect register with inline raise-form and per-row status changer and filed emails.

- AuthGate ⚠️ → LoadGate ✅ / RequestAccessView ✅
- ProjectPageShell ✅
  - Row 🔒 — Title "Defects" · PrimaryButton "Raise defect"/"Close" toggle ⚠️
  - AlertBanner (negative) ⚠️ — actionError
  - Panel 🔒 "Raise a defect" (conditional) — FormField ⚠️ ×3 (Location, Assigned to, Description) + PrimaryButton "Raise defect"
  - LoadGate ✅ (Prominent) / dataFailed text 🔒 / EmptyState 🔒 / else:
  - RecordsTable ⚠️ — 6 cols (Ref, Location, Description, Assigned to, Raised, Status) + actions col; StatusPill-as-`<select>` cell 🔒, per-row "Emails" toggle → expanded row with FiledEmailList ✅

Modals: none (raise form and status change are both inline, no dialog).

### Inventory — `/projects/{ProjectId}/inventory`
`Pages/ProjectInventory.razor` (285 lines) — project goods register with per-item filed-email thread, sequential INV-#### refs.

- AuthGate ⚠️ → LoadGate ✅ / RequestAccessView ✅
- ProjectPageShell ✅ (ActiveTab="inventory")
  - PageHeader ⚠️ — Title "Inventory", PrimaryButton "Add item"/"Close" toggle ⚠️
  - AlertBanner (negative, actionError) 🔒
  - Inline add/edit form 🔒 (not a Modal) — FormField ⚠️ ×4 (Product name, Location, Product details, Location details), PrimaryButton "Add item"/"Save changes"
  - LoadGate ✅ (inventory rows) → error text 🔒 / EmptyState 🔒
  - RecordsTable ⚠️ — 6 cols (Ref, Product, Details, Location, Added, actions), row actions "Edit" / "Emails" (IconButton-as-text-link ⚠️), expandable row → FiledEmailList ✅
- Modals: none (edit reuses the inline form, not a dialog)

### Building Control — `/projects/{id}/building-control`
`Pages/ProjectBuildingControl.razor` (337 lines) + `.razor.cs` — the project's statutory sign-off trail: one case (body, reference, dates) plus its register of inspection stages.

- AuthGate ⚠️ (manual `!sessionReady → LoadGate` / `!IsApproved → RequestAccessView`, not the extracted `ApprovedSessionGate`)
- ProjectPageShell ✅ (ActiveTab="building-control")
  - Title "Building Control" 🔒
  - AlertBanner (negative) ⚠️ — actionError text bar
  - LoadGate ✅ "Loading building control" / failed-load text 🔒
  - Case panel 🔒
    - EmptyState 🔒 "No building control case yet" + PrimaryButton "Set up building control" ⚠️
    - or: header (BodyName, StatusPill ⚠️, Regime · BodyReference · Case ref) + inline `<select>` status changer 🔒 + IconButton "Edit" ⚠️
    - KeyValueList ⚠️ (Contact, Notice submitted, Accepted, Completion certified — 4-col `<dl>`)
    - Case documents sub-panel 🔒 — FileDropZone-ish "+ Add files" label wrapping InputFile ✅, upload-kind `<select>`, file list with download link + Remove IconButton ⚠️
  - Case set-up/edit form 🔒 — inline FormField-style `<input>`/`<select>`/`<textarea>` grid (Regime, Body, Reference, Notice date, Accepted date, Contact ×3, Notes, seed-checklist checkbox), PrimaryButton/SecondaryButton ⚠️
  - Inspections section 🔒
    - PageHeader-lite: Title "Inspections" + PrimaryButton "Add inspection" ⚠️
    - Add-inspection inline form 🔒 (Stage, Booked-for date, submit button)
    - EmptyState 🔒 or RecordsTable ⚠️ — 7 cols (Ref, Stage, Status StatusPill cells, Booked for, Inspected, Files, Remove RowActionMenu-lite), row click → inspection detail page
- Modals: none (case/stage forms are inline panels, not Modal)

### Building Control Inspection — `/projects/{ProjectId}/building-control/inspections/{InspectionId}`
`Pages/ProjectBuildingControlInspection.razor` (243 lines) — one inspection stage: status ladder, dates, photo/document evidence and live tagged correspondence.

- ApprovedSessionGate ✅
- ProjectPageShell ✅ (ActiveTab="building-control")
  - LoadGate ✅ / "not found" text 🔒 (back link)
  - breadcrumb "← Building Control" 🔒
  - header row 🔒 — Title (StageName) ⚠️, reference Badge 🔒, status `<select>` StatusPill-driver 🔒
  - AlertBanner (negative) ⚠️ — action error
  - Panel "Details" 🔒 — FormField-like inline inputs 🔒 ×5 (Booked for, Inspected, Inspector, Outcome/actions textarea, Stage name), SecondaryButton "Save changes" 🔒, Created-stamp KeyValueList line 🔒
  - Panel "Photos" 🔒 — header + InputFile ✅ ("+ Add photos"), EmptyState ⚠️, photo grid 🔒 (thumbnail + hover remove IconButton 🔒)
  - Panel "Documents" 🔒 — header + InputFile ✅ ("+ Add files"), EmptyState ⚠️, file list 🔒 (name link, kind/size/source KeyValueList-style line 🔒, Remove IconButton 🔒)
  - Panel "Correspondence" 🔒
    - header — CountBadge 🔒 + EmailFinder ✅
    - LoadGate ✅
      - AlertBanner (positive) 🔒 — sent confirmation, dismissible
      - MailReplyComposer ✅ (reply/forward composer, inline)
      - EmptyState ⚠️
      - per-email "Copy attachments" link 🔒 (conditional)
      - CorrespondenceThreadList ✅

### Communications — `/projects/{id}/communications`
`Pages/ProjectCommunications.razor` (170 lines) + `.razor.cs` — cross-record roll-up of every tagged email on the project, with inline reply/forward.

- ApprovedSessionGate ✅
- ProjectPageShell ✅ (ActiveTab="communications")
  - ChipTabs ⚠️ — pathway bucket (All · Client · Subcontractor · Supplier · Internal)
  - FilterBar 🔒 — "Tagged to" type `<select>` + "Showing X of Y" count text
  - AlertBanner (negative) 🔒 — loadError
  - LoadGate ✅ ("Loading communications")
    - EmptyState 🔒 (only when no error)
    - AlertBanner (positive) 🔒 — replySent confirmation, dismissible
    - MailReplyComposer ✅ — inline reply/forward, opens above the list
    - ActivityFeed ⚠️ (`<ul>` of email cards) — each: sender + pathway Badge ⚠️, timestamp, subject + attachment Badge, body preview, record-link CountBadge-style chips ⚠️ (type + reference + title) plus unresolved-tag chips, Reply/Forward text links
    - LoadMoreButton ⚠️

No modals (compose is inline, not a dialog).

### Useful Information — `/projects/{id}/useful-information`
`Pages/ProjectUsefulInformation.razor` (32 lines, `@code` inline) — internal-only free-text notes tab; all content delegated to one domain component.

- ApprovedSessionGate ✅
- ProjectPageShell ✅ (ActiveTab="useful-information")
  - UsefulInformationPanel ✅ — entire body (list, add/edit, gating) lives inside this component

No modals opened directly by the page.

### Project settings — `/projects/{ProjectId}/settings`
`Pages/ProjectSettings.razor` (123 lines) — the project's one Setup surface: Details, Deposits/retentions & valuation, Contract, Correspondence panes recombined from three legacy routes.

- AuthGate ⚠️ → LoadGate ✅ / RequestAccessView ✅
- ProjectPageShell ✅ (ActiveTab="settings")
  - SegmentedTabs 🔒 (4 panes: Details · Deposits, retentions & valuation · Contract · Correspondence)
  - **Details pane:** ActionCluster ⚠️ (ProjectDetailsEditor ✅), StatRow ⚠️ — 6× Stat ✅ (Stage, Entity, Project Manager, Client, Site address, Xero site), Created-stamp text 🔒
  - **Deposits/retentions pane:** grid 🔒 — NextValuationDateEditor ✅; ProjectRetentionPanel ✅
  - **Contract pane:** ProjectContractPanel ✅
  - **Correspondence pane:** ProjectCorrespondencePanel ✅

### Project (bare route) — `/projects/{id}`
`Pages/ProjectDetail.razor` (25 lines, `@code` inline) — redirect-only stub, no rendered markup. `OnInitialized` sends the user to `DesktopNavigation.FirstProjectTabHref(role, ProjectId)` (falling back to `/projects/{id}/requests`) via `Nav.NavigateTo(..., replace: true)`. No component tree to document.

### Project setup — `/projects/{id}/setup`
`Pages/ProjectSetup.razor` (12 lines) — pure redirect stub: `OnInitialized` navigates (replace) to `/projects/{id}/settings`, the setup tabs having been folded into one Project settings page. No render tree.

### Project Operations Setup — `/projects/{ProjectId}/operations-setup`
`Pages/ProjectOperationsSetup.razor` (12 lines) — pure redirect stub, no markup. `OnInitialized` navigates (`replace: true`) to `/projects/{ProjectId}/settings` — the per-section Setup tabs were recombined into the single Project settings page. No component tree to document.

### Project Financials Setup — `/projects/{id}/financials-setup`
Redirect stub only: `Pages/ProjectFinancialsSetup.razor` (12 lines) immediately navigates (`replace: true`) to `/projects/{id}/settings`. No render tree.


## Subcontractor & Supplier

### Bid package invites — `/projects/{id}/bid-package-invites`
`Pages/ProjectBidPackageInvites.razor` (255 lines) + `.razor.cs` — the project's tender/bid-package register, plus an AI "suggest bid packages" flow.

- Inline 3-branch gate 🔒⚠️ — LoadGate ✅ / RequestAccessView ✅
- ProjectPageShell ✅
  - Title "Bid package invites" · Subtitle "N packages on this project" 🔒
  - ActionCluster 🔒 — ExportToExcelButton ✅, SecondaryButton "Suggest bid packages" ⚠️, PrimaryButton "New bid package" ⚠️ (CanManage only)
  - AlertBanner (negative) ⚠️ — error
  - EmptyState 🔒 (copy varies by CanManage)
  - RecordsTable ⚠️ — 4 cols (Package link, Trade, Status badge, Created), sticky headers, closed rows dimmed
- Modal ✅ "New bid package" — Title/Trade inputs 🔒, trade `<select>` disabled+"Loading trades…" while unloaded 🔒, "Materials apply" checkbox 🔒
- Modal ✅ "Suggest bid packages" (custom FooterContent)
  - explainer paragraph 🔒, AlertBanner (negative) ⚠️ — suggestError
  - LoadGate ✅ — "Analysing… part N" (multi-hop progress)
  - model `<select>` 🔒 (before first run)
  - suggestion cards 🔒 (checkbox + title/trade Badge ⚠️ + value + scope + rationale + source-line count), all pre-ticked

Modals: New bid package (Modal ✅), Suggest bid packages (Modal ✅).

### Bid Package Invite Detail — `/projects/{id}/bid-package-invites/{BidPackageId}`
`Pages/ProjectBidPackageInviteDetail.razor` (266 lines) — one bid package's full tender lifecycle: invite list, details/line items, submissions, documents, tagged emails.

- AuthGate ⚠️ → LoadGate ✅ / RequestAccessView ✅
- ProjectPageShell ✅
  - Breadcrumb nav 🔒 — "Bid package invites"
  - AlertBanner (negative) ⚠️ — error
  - LoadGate ✅ (package load) wrapping:
    - Header 🔒 — Reference, Title + Badge ⚠️ (status), Trade subtitle, materials-applicable checkbox/text 🔒
    - DropdownMenu ✅ "Actions" (Close/Reopen package, Delete package…)
    - AlertBanner (neutral) ⚠️ — closed-package notice
    - ChipTabs ⚠️ — Details · Tender list · Submissions · Documents · Emails
    - activeTab == details → PackageDetailsSections ✅
    - activeTab == tender-list → InvitedSubcontractorsSection ✅
    - activeTab == submissions → TenderSubmissionsSection ✅
    - activeTab == documents → PackageDocumentsSection ✅
    - activeTab == emails → RecordCorrespondencePanel ✅ (EmptyState slot 🔒)
- Modals: SubcontractorInvitePickerModal ✅, LocalSubcontractorFinderModal ✅, LineCoverageModal ✅, LinkDrawingsModal ✅, TenderInviteComposerModal ✅, TenderSubmissionModal ✅, WorkOrderEmailDraftModal ✅, DeletePackageModal ✅ (ConfirmDialog pattern), PackageDetailsEditorModal ✅, ValuationLinePickerModal ✅

### Work Orders — `/projects/{id}/work-orders`
`Pages/ProjectWorkOrders.razor` (194 lines) — the project's issued work orders grouped by cost centre or supplier, plus drafts/rejected/cancelled sections and line re-coding.

- ApprovedSessionGate ✅ (wraps AuthGate ⚠️ internally)
- ProjectPageShell ✅
  - Row 🔒 — Title "Work orders by {cost centre|supplier}"
    - SegmentedTabs ⚠️ — Cost centre / Supplier grouping toggle
    - SearchInput ⚠️ — "Search by supplier…"
    - ExportToExcelButton ✅
    - SecondaryButton "Add work order" ⚠️
  - AlertBanner (info) ⚠️ — PO-email note, dismissible
  - LoadGate ✅ (Prominent) wrapping:
    - dataFailed text 🔒 / EmptyState 🔒 / else:
    - Summary line 🔒 — counts + committed/paid/remaining Money, Xero-sync timestamp
    - DraftWorkOrdersPanel ✅ (conditional)
    - RejectedWorkOrdersList ✅ (conditional)
    - CancelledWorkOrdersList ✅ (conditional)
    - EmptyState 🔒 (no live rows) / WorkOrdersTable ✅ (grouped RecordsTable with expand/collapse, cancel actions)
    - UnpricedWorkOrdersList ✅ (conditional)
    - Footnote 🔒 — committed/paid/remaining methodology
    - WorkOrderLineRecodeModal ✅
  - ManualWorkOrderModal ✅
  - DeleteWorkOrderModal ✅ (ConfirmDialog pattern)

Modals: WorkOrderLineRecodeModal ✅, ManualWorkOrderModal ✅, DeleteWorkOrderModal ✅.

### Purchase Order — `/projects/{ProjectId}/work-orders/{WorkOrderId}/po`
`Pages/WorkOrderPo.razor` (338 lines) — the printable PO document plus screen-only office record-keeping (attachments, timeline, audit history).

- AuthGate ⚠️ → LoadGate ✅ / RequestAccessView ✅ / EmptyState 🔒 ("Work order not found")
- Section (print-styled, `.po-toolbar` etc. hidden via `@media print`) 🔒
  - Toolbar-like row ⚠️ — back link, StatusPill 🔒 (Cancelled/Accepted/Awaiting/Draft/Rejected, hand-rolled pill spans not the canonical StatusPill), SecondaryButton "Draft email to supplier…" ⚠️, SecondaryButton "Print / save PDF" ⚠️
  - AlertBanner (email note / error) 🔒 ×2
  - PurchaseOrderSheet ✅ — the actual PO document (order, lines, supplier, project, approver)
  - WorkOrderAttachmentsPanel ✅ (screen-only, hidden from print)
  - Panel ✅ "Timeline" — Timeline ⚠️ hand-rolled `<ol>` with dot markers (Raised → Approved & issued → Accepted)
  - RecordAuditHistory ✅ "History" (screen-only, hidden from print)
- Modals: Email covering draft (Modal ✅ + FormField ✅ ×2 — subject input, HTML body textarea)

### Communications (Subcontractor / Supplier / Internal) — `/subcontractors/communications[/{category}]` · `/suppliers/communications[/{category}]` · `/internal/communications[/{category}]`
`Pages/SubcontractorCommunications.razor` (134 lines) — one page serving three "families" of tagged mailbox threads by route.

- AuthGate ⚠️ → LoadGate ✅ / RequestAccessView ✅
- section 🔒
  - header 🔒 — Eyebrow (family pathway) · Title "Communications" · count line ("N emails tagged as…")
  - ChipTabs ⚠️ — "All" + one chip per family category, each titled with its summary
  - AlertBanner (negative) ⚠️ — load error
  - LoadGate ✅ (Prominent) wrapping:
    - EmptyState 🔒 — "No {family}s yet" / "Nothing tagged {filter} yet", with instructions to tag in Control Centre
    - AlertBanner (positive) ⚠️ — dismissible reply-sent confirmation
    - Inline reply/forward composer 🔒 — context line + MailReplyComposer ✅
    - CorrespondenceThreadList ✅ (Features/Triage/Panels) — the threaded email list itself
    - LoadMoreButton ⚠️ — "Load more" / "Loading…"

Modals: none — reply/forward is inline, not a dialog.


## Internal

### To-dos — `/todos`
`Pages/Todos.razor` (348 lines) — the master to-do list: every company-wide and project item, board or list view, with search and role/person/project filters.

- AuthGate ⚠️ → LoadGate ✅ / RequestAccessView ✅ / "Not available" text 🔒
- Body section 🔒
  - PageHeader ⚠️ — Eyebrow "JPMS" ⚠️, Title "To-dos" ⚠️, Subtitle (role-dependent copy) ⚠️, ActionCluster ⚠️ (PrimaryButton "Add to-do" 🔒, CanManage-gated)
  - AlertBanner (negative) ⚠️ — load error
  - FilterBar 🔒 — SearchInput ⚠️ (with clear ✕), SegmentedTabs 🔒 (Board/List view toggle), ChipTabs ⚠️ (Open/Done/All, disabled while searching), SearchSelect ✅ ×3 (Project scope, Role, Person — disabled placeholders while loading), contextual link "Open X's To-do tab" 🔒
  - AlertBanner 🔒 — post-add confirmation note with link
  - LoadGate ✅
    - Panel ⚠️ (p-5 card)
      - header row 🔒 — title (To-do board/list) + "n of m done" CountBadge-like text ⚠️
      - EmptyState ⚠️ (3 message variants)
      - TodoBoard ✅ (drag-between-columns) OR RecordsTable-as-rows 🔒 (button-rows: reference, title, ScopeChip 🔒, "In progress" Badge 🔒, due-date StatusPill-style text 🔒, TodoAssigneeBadge ✅, completed/added-by text)
      - hidden-match note 🔒
      - TaggedEmailSearch ✅ (triage-role-gated, renders while searching)
- Modals: Add to-do (Modal ✅ + SearchSelect ✅ project picker, FormField ✅ Title, SearchSelect ✅ assignee, FormField ✅ Due date, textarea Notes 🔒) 🔒

### To-do Detail — `/todos/{TodoItemId}`
`Pages/TodoDetail.razor` (113 lines) + `.razor.cs` — one to-do's facts, activity, linked to-dos and mail thread; replaced a modal (2026-08-10).

- AuthGate ⚠️ (manual preamble) → not-available text panel 🔒 for non-internal roles
- "← All to-dos" back link 🔒
- LoadGate ✅ "Loading the to-do", wraps:
  - failed text 🔒, or EmptyState "This to-do is gone" 🔒, or:
  - PageHeader ⚠️ — Eyebrow "JPMS · {ref}" · Title (struck-through if complete) + StatusPill ⚠️ · ActionCluster ⚠️: SecondaryButton "Delete" (two-step arm/confirm) ⚠️, SecondaryButton "Working on it" ⚠️, PrimaryButton "Mark done/Reopen" ⚠️
  - AlertBanner (negative) ⚠️ — error
  - 3-col layout 🔒:
    - TodoCommunicationsPanel ✅ (2 cols — mail thread + compose)
    - side column: TodoFactsPanel ✅ (KeyValueList-style with reassign/move editors), TodoActivityPanel ✅ (Timeline), LinkedTodosPanel ✅
- Modals: none (dialog retired in favour of this page)

### Tender enquiries — `/tender-enquiries`
`Pages/TenderEnquiries.razor` (148 lines) — company-wide bid pipeline register, one row per enquiry, live open ones first.

- ApprovedSessionGate ✅ (the shared AuthGate component — the only page in this batch not hand-rolling the preamble)
- section 🔒
  - header 🔒 — Eyebrow "JPMS · Internal" · Title "Tender enquiries" · Subtitle · ActionCluster ⚠️: PrimaryButton "Log enquiry" ⚠️ (CanManage)
  - AlertBanner (negative) ⚠️ — load error
  - LoadGate ✅ (Prominent)
    - EmptyState 🔒 — "No tender enquiries yet."
    - "Show ended (N)" checkbox filter 🔒 — right-aligned, plain input, not a ChipTabs/SegmentedTabs
    - TenderEnquiriesTable ✅ (Features/TenderEnquiries)
- Modals:
  - Modal ✅ "Log tender enquiry" — TenderEnquiryDetailsForm ✅ + TenderEnquiryNewProjectFields ✅ (new Lead-stage project), helper copy

### Tender Enquiry — `/tender-enquiries/{id}`
`Pages/TenderEnquiryDetail.razor` (183 lines, `@code` inline) — single enquiry's overview, PQQ response, documents and emails.

- ApprovedSessionGate ✅
  - Breadcrumb 🔒 — "Tender enquiries" link
  - AlertBanner (negative) 🔒 — error
  - LoadGate ✅
    - EmptyState 🔒 — "could not be loaded"
    - Header 🔒 — reference, Title (h1) + TenderEnquiryStatusBadge ✅, Subtitle (architect · contact · project ref + ProjectStageBadge ✅), SecondaryButton "Edit details" 🔒
    - SegmentedTabs ⚠️ — Overview · PQQ response · Documents · Emails
    - **Overview:** Panel 🔒 → KeyValueList ⚠️ (`<dl>`: Architect/Contact/Contract form/Bid owner) + scope text, RecordAuditHistory ✅ (Timeline); sidebar TenderEnquiryStatusPanel ✅
    - **PQQ:** TenderEnquiryAnswersEditor ✅, TenderEnquiryResponseSender ✅
    - **Documents:** TenderEnquiryAttachmentsPanel ✅ (FileDropZone-style upload inside)
    - **Emails:** TenderEnquiryEmailsPanel ✅
  - Modals: Modal ✅ ("Edit enquiry details" → TenderEnquiryDetailsForm ✅)

### Directory — `/directory`
`Pages/Subcontractors.razor` (159 lines) — the unified directory: Clients / Architects / Subcontractors / Internal staff behind one chip row, subcontractors' own company table as the default.

- AuthGate ⚠️ → LoadGate ✅ / RequestAccessView ✅ / "Not available" text 🔒
- Body section 🔒
  - PageHeader ⚠️ — Eyebrow "JPMS · Directory" ⚠️, Title "Directory" ⚠️, Subtitle (group-dependent summary) ⚠️
    - ActionCluster ⚠️ (Subcontractors group only) — ExportToExcelButton ✅, PrimaryButton "+ Add company" 🔒
  - ChipTabs ⚠️ — group chips (Clients · Architects · Subcontractors · Internal staff)
  - **Clients group:** ClientsDirectoryTable ✅
  - **Architects group:** ArchitectsDirectoryTable ✅
  - **Staff group:** StaffDirectoryTable ✅
  - **Subcontractors group (default):**
    - FilterBar 🔒 — SearchInput ⚠️, `<select>` Type filter 🔒, FilterChips ✅ Xero link (All / Linked to Xero / Not linked to Xero, counts once loaded), live CountSummary text 🔒 (loaded-gated), SecondaryButton "Import from Xero" 🔒 (with inline SVG icon), PrimaryButton "Consolidate (n)" 🔒 (conditional, ≥2 selected)
    - CompaniesDirectoryTable ✅ (SelectedIds, compliance pills)
- Modals: Add company (Modal ✅ + DirectoryContactForm ✅) 🔒 · XeroImportModal ✅ (offers "Link to <record>" beside "Import as new" when a contact's name matches one unlinked record) · ConsolidateRecordsModal ✅

### Subcontractor Detail — `/directory/{SubcontractorId}`
`Pages/SubcontractorDetail.razor` — one directory company: trades, contacts, portal invite, statement of account, edit form; header actions carry "Link to Xero contact…" (XeroLinkModal ✅) when unlinked and an InlineConfirm ✅ "Unlink from <contact>" per link when linked.

- AuthGate ⚠️ → LoadGate ✅ / RequestAccessView ✅ / role-gate text 🔒 / LoadGate ✅ (directory fetch) / "not found" 🔒
- Section 🔒
  - Breadcrumb nav 🔒 — "Subcontractors / {CompanyName}"
  - Header 🔒 — CompanyName + Xero-linked Badge ⚠️ (inline SVG icon), ContactLine subtitle, SecondaryButton "Edit details…" ⚠️
  - Panel 🔒 "Trades" — chip list of trades (each a removable pill ⚠️), inline `<select>` add-existing + text input + SecondaryButton "Add new trade" 🔒, AlertBanner (negative) ⚠️ for tradesError
  - Panel 🔒 "Contacts" — LoadGate ✅ (per-record contacts) / EmptyState 🔒 / RecordsTable ⚠️ (4 cols: Name, Email, Phone, actions — Edit/Remove text links, no StatusPill, no header sort), inline add/edit FormField ⚠️ row (Name/Email/Phone + Save/Cancel)
  - Panel 🔒 "Portal access" — AlertBanner (negative) ⚠️ invite error, AlertBanner (positive) ⚠️ invite-success with copyable link, SecondaryButton "Invite to portal" / "Re-send invite" ⚠️
  - Panel 🔒 "Statement of account" — explainer text, SecondaryButton "Download PDF" (anchor) ⚠️, SecondaryButton "Email statement…" ⚠️
  - SubcontractorComplianceList ✅
  - Modals: SubcontractorStatementModal ✅ (email-statement flow); Modal ✅ "Edit company details" — FormField ✅ ×9 (company name, contact name/email/phone, payment terms, address line, town/county/postcode)

### Clients — `/clients`
`Pages/Clients.razor` (322 lines) — the client-account register: name, primary contact, portal-invite and contacts management.

- Inline 3-branch gate 🔒⚠️ — LoadGate ✅ / RequestAccessView ✅
- "no access" text 🔒
- section 🔒
  - Eyebrow "JPMS" · Title "Client accounts" · Subtitle w/ live count 🔒 (withheld until loaded) + link to Architects
  - ActionCluster 🔒 — ExportToExcelButton ✅, PrimaryButton "New client" ⚠️
  - AlertBanner (negative) ⚠️ — loadError
  - LoadGate ✅ Prominent
    - negative-state text 🔒 / EmptyState 🔒 "No client accounts yet"
    - RecordsTable ⚠️ — 4 cols (Client, Primary contact, Contact email, actions: Contacts/Edit/Invite links)
- PartyContactsEditor ✅ (ref'd, opened via "Contacts" action)
- Modal ✅ "New client account" — AlertBanner (negative) ⚠️, FormField ✅ ×3 (name, contact name, contact email), footnote linking Architects
- Modal ✅ "Invite to the client portal" — explainer, AlertBanner (neg/pos) ⚠️, invite-link `<code>` block 🔒
- Modal ✅ "Edit client" — AlertBanner (negative) ⚠️, FormField ✅ ×3

Modals: New client (Modal ✅), Invite to portal (Modal ✅), Edit client (Modal ✅).

### Architects — `/architects`
`Pages/Architects.razor` (260 lines) — the architect-practice master list; contact email here is where RFIs and other request documents go.

- AuthGate ⚠️ → LoadGate ✅ / RequestAccessView ✅ / "no access" text 🔒
- Body section 🔒
  - header 🔒 — Eyebrow "JPMS" ⚠️, Title "Architect practices" ⚠️, Subtitle (count-prefixed, loaded-gated) ⚠️
    - ActionCluster ⚠️ — ExportToExcelButton ✅, PrimaryButton "New architect" 🔒
  - AlertBanner (negative) ⚠️ — load error
  - LoadGate ✅
    - EmptyState ⚠️
    - RecordsTable ⚠️ — 4 cols (Practice, Contact, Contact email, row actions "Contacts"/"Edit" text links 🔒)
- PartyContactsEditor ✅ (ref-driven side editor, opens per row)
- Modals: New architect practice (Modal ✅ + FormField ✅ ×3) 🔒 · Edit architect (Modal ✅ + FormField ✅ ×3) 🔒

### Registers — `/registers`
`Pages/Registers.razor` (339 lines, `@code` inline) — company registers (insurances, subscriptions, vans, trade accounts) with renewal-date flagging.

- ApprovedSessionGate ✅
  - Header 🔒 — Title "Registers" (h2) · Toolbar ✅ (ToolbarButton "Refresh", ExportToExcelButton ✅) · PrimaryButton "Add item" 🔒 (outside Toolbar)
  - Subtitle 🔒
  - AlertBanner (negative) 🔒 — actionError
  - SegmentedTabs ⚠️ — Insurances · Subscriptions · Vans · Trade accounts
  - Panel ✅ (titled per active kind, IsLoading-gated)
    - AlertBanner 🔒 (dataFailed) / EmptyState 🔒
    - RecordsTable ⚠️ — 8 cols (labels relabel per kind: Name/Counterparty/Reference/Cost/Cycle/2 date cols with StatusPill-style lapsed/due-soon chips 🔒/Actions), Edit + Deactivate buttons per row
  - Modal ✅ — Add/Edit item, hand-rolled `<input>`/`<select>` grid 🔒 (not FormField)

### Policies & Sign-off — `/policies`
`Pages/Policies.razor` (287 lines) — staff document sign-off: personal queue for everyone, admin publish + tracking panel.

- ApprovedSessionGate ✅
- Section (max-w-5xl) 🔒
  - PageHeader ⚠️ — Title "Policies & sign-off", PrimaryButton "Publish for signing" ⚠️ (admin roles only), Subtitle
  - AlertBanner (negative, actionError) 🔒
  - Panel ✅ "For you to sign" — per-document card 🔒 (title, rev Badge 🔒, summary, requested date, FormField ✅ typed-name input + PrimaryButton "Sign"), "Signed" sub-list 🔒 with StatusPill-ish text ⚠️; EmptyState 🔒
  - Panel ✅ "Published documents" (admin-only, conditional panel) — RecordsTable ⚠️ (5 cols: Title, Rev, Published, Signed CountBadge 🔒, Outstanding Badge 🔒), row click expands an inline detail row (KeyValueList ⚠️ of recipient → signed/not-signed)
- Modals: "Publish for signing" (Modal ✅ + FormField ✅ ×3 — title, summary/body textarea, recipients textarea)


## Time

### Labour Overview — `/labour/overview`
`Pages/LabourOverview.razor` (144 lines) — company-wide labour forecast: header stat, five switchable views (workers/sites/cost codes/sign-off/settlement), chase list.

- AuthGate ⚠️ → LoadGate ✅ / RequestAccessView ✅
- Section 🔒 (project-agnostic — no ProjectPageShell)
  - Row 🔒 — Title "Labour overview"
    - Month-nav cluster 🔒 — SecondaryButton "‹ Prev" / label / SecondaryButton "Next ›"
    - Toolbar ✅ — ToolbarButton ✅ "Refresh", ExportToExcelButton ✅
    - PrimaryButton "Enter a week" ⚠️
  - Explainer paragraph 🔒
  - AlertBanner (negative) ⚠️ — actionError
  - AlertBanner (positive) ⚠️ — weekSummaryLines
  - LabourForecastHeader ✅ — MetricStat-style forecast figure + confidence bar
  - SegmentedTabs ⚠️ — By worker / By site / By cost code / Sign-off / Settlement
  - view switch → WorkerPlacementTable ✅ | SiteCostTable ✅ | CostCodeTable ✅ | WeeklySignOffTable ✅ | SettlementSchedulesPanel ✅
  - ChaseListPanel ✅ (conditional, non-empty chase)
  - "Chase list clear" note 🔒 (conditional, empty + dismissed>0)
- Modals: AbsenceModal ✅, SettlementLineModal ✅, WeekEntryModal ✅

### Labour — `/projects/{ProjectId}/labour`
`Pages/ProjectLabour.razor` (299 lines) — weekly timesheet approval grid, site register, worker assignment, subcontractor settlement.

- ApprovedSessionGate ✅
- ProjectPageShell ✅ (ActiveTab="labour")
  - AlertBanner (negative, action error) 🔒
  - ApprovalFailuresBanner ✅ — budget-block failures, with over-budget override CTA
  - PageHeader ⚠️ — Title "Labour" · week-nav (SecondaryButton ‹Prev/Next› 🔒) · Toolbar ✅ (ExportToExcelButton ✅) · PrimaryButton "Add a day" ⚠️
  - Panel ✅ "Timesheets" (IsLoading on TimesheetsReady)
    - FilterBar ⚠️ — single worker `<select>`, not SearchSelect
    - RecordsTable ⚠️ — 8 cols, SelectAllCheckbox, rows via TimesheetRow ✅ (inline edit mode)
    - TimesheetApprovalFooter ✅ — bulk cost-code + Approve selected
    - EmptyState 🔒 ×2 (no timesheets this week / for this worker)
  - Grid (2-col) 🔒
    - Panel ✅ "Workers on this project" — assign dropdown + Remove list (KeyValueList-ish ⚠️), footnote copy
    - SiteRegisterPanel ✅ (attendance rows, own loading/fail props)
  - Panel ✅ "Subcontractor settlement" — SettlementSummaryTable ✅, "Mark invoice lines as covered…" SecondaryButton ⚠️ → CoverInvoiceLinesTable ✅ (LoadGate ✅ inline while ledger loads)
- Modals: "Add a day" (Modal ✅ + FormField ✅ ×4 incl. SearchSelect ✅ for cost code), "Reject timesheet" (Modal ✅ + FormField ✅), "Approve over budget" (Modal ✅ + FormField ✅ + failure list 🔒)

### Workers — `/labour/workers`
`Pages/Workers.razor` (263 lines) + `.razor.cs` — the day-rate worker registry (name, cost rate, subcontractor link); a directory-matching card for settlement identity sits below it.

- ApprovedSessionGate ✅ (AuthGate)
  - section 🔒
    - Title "Workers" · PrimaryButton "Add worker" ⚠️ (header row, no ActionCluster wrapper)
    - Subtitle paragraph 🔒 (explains the My-day/registry link)
    - AlertBanner (negative) ⚠️ — actionError
    - LoadGate ✅ Prominent — gates on Labour+Subcontractors together
      - negative-state text 🔒 / EmptyState 🔒 "No workers yet"
      - ExportToExcelButton ✅
      - RecordsTable ⚠️ — 7 cols (Name, Portal email, Subcontractor, Hourly £, Day £, Status, actions), StatusPill inline ⚠️ (Active/Inactive), RowActionMenu ⚠️ (Edit / Delete↔Confirm-Cancel swap per row)
    - Directory-matching Panel 🔒 (conditional, only when unlinked workers exist)
      - header + SecondaryButton "Find matches"/"Re-check matches" ⚠️
      - AlertBanner (negative) ⚠️ — matchError
      - RecordsTable ⚠️ — 3 cols (Worker, Directory match, actions: Link/Sole trader)
      - PrimaryButton "Link all matched (n)" ⚠️
  - Modal ✅ "Add worker"/"Edit worker" (shared Add+Edit form)
    - AlertBanner (negative) ⚠️ — formError
    - hand-rolled label+input pairs 🔒 (Name, Portal email, Day rate, Phone) — not FormField
    - SearchSelect ✅ — Subcontractor (disabled+"Loading…" while directory loads)
    - checkbox "Sole trader" 🔒, date inputs (Engaged from/to) 🔒, checkbox "Active" (edit only) 🔒
    - computed hourly-rate footnote 🔒

Modals: Add/Edit worker (Modal ✅, shared).

### Labour Xero Mapping — `/labour/xero-mapping`
`Pages/LabourXeroMapping.razor` (283 lines) — effective-dated site→tracking and cost-code→account mappings.

- AuthGate ⚠️ (hand-rolled, `sessionReady`/dataFailed) → LoadGate ✅ / RequestAccessView ✅
- Section 🔒 — Title "Xero mapping" + explainer, Toolbar ✅ (ToolbarButton "Refresh" ✅)
- AlertBanner (negative) 🔒 — actionError
- Panel ✅ "Sites → Xero tracking" (IsLoading-gated)
  - FilterBar ⚠️ — project `<select>` 🔒 + Xero-option `<select>` 🔒 + SecondaryButton "Set mapping" 🔒
  - RecordsTable 🔒 (3 cols) or EmptyState 🔒
- Panel ✅ "Cost codes → Xero tracking & accounts" (IsLoading-gated)
  - FilterBar ⚠️ — cost-code `<select>` 🔒 + 3 account `<input>`s 🔒 + SecondaryButton 🔒
  - RecordsTable 🔒 (5 cols) or EmptyState 🔒
- Modals: none


## Finance

### Financials — `/projects/{id}/financials`
`Pages/ProjectFinancials.razor` (194 lines, + .razor.cs) — the project's cost-centre P&L: contract sales vs. work orders vs. actual cost of sales, with roll-ups and reconciliation packages.

- AuthGate ⚠️ (isLoaded flag, not sessionReady) → LoadGate ✅ / RequestAccessView ✅
- ProjectPageShell ✅
  - Title "Financial summary" 🔒 (plain h2, no PageHeader/ActionCluster)
  - EmptyState 🔒 — no cost centres seeded
  - AlertBanner (negative) ⚠️ ×2 — summary refresh failed (with inline Retry link), action error
  - AlertBanner (warning/amber) ⚠️ — pending labour total, links to Labour tab
  - FinancialsTable ✅ (Features/Commercial) — the cost-centre grid with tick-to-group rows, click-through cells for sales/WO/cost-of-sales/reconciliation, inline % complete + lock editing
  - KeyValueList-style explainer paragraph 🔒 — long prose defining every column
  - PackageReconciliationSection ✅ (Features/Procurement) — sub-package reconciliation sub-panel
- Modals:
  - CostCentreSalesLinesModal ✅ · CostCentreWorkOrdersModal ✅ · CostCentreReconciliationModal ✅ · CostCentreCostOfSalesModal ✅ — all click-through drill-downs, each IsOpen-bound to a selected row
  - Modal ✅ "Roll up into one line" — free-text group name input 🔒, merge-existing-group copy

### WO Allocation — `/projects/{id}/work-order-allocation`
`Pages/ProjectWorkOrderAllocation.razor` (350 lines, +.razor.cs) — ties Xero purchase lines to work orders; recodes the order's cost centre from the invoice.

- AuthGate ⚠️ (hand-rolled isLoaded/Session.IsApproved scaffold, not the ApprovedSessionGate component) → LoadGate ✅ / RequestAccessView ✅
- ProjectPageShell ✅
  - Title "Work order invoice allocation" + explainer paragraph 🔒 (plain `<h2>`/`<p>`, not PageHeader)
  - StatRow ⚠️ — 4 hand-rolled KPI cards 🔒 (Cost of sales · Linked to work orders · Not linked · Orders fully invoiced)
  - ActionCluster 🔒 — ExportToExcelButton ✅ (include-all menu)
  - SearchInput ⚠️ — "Search supplier or title…", filters both tables below
  - RecordsTable 🔒 — Work orders, 8 cols, click-to-expand rows, StatusPill via inline InvoicingPill 🔒, progress bar cell, tfoot totals
    - nested RecordsTable 🔒 (per expanded order) — linked invoice lines, 7 cols, per-row Unlink button
  - ChipTabs ⚠️ — queue filter (Unlinked · Linked · All)
  - RecordsTable 🔒 — Invoice-line queue, 7 cols, inline `<select>` work-order picker with optgroups, "Split…" trigger, tfoot totals
  - AlertBanner (negative) 🔒 — linkError
  - Modals: WorkOrderLinkSplitModal ✅

### Payment Certificates — `/finance/payment-certificates`
`Pages/PaymentCertificates.razor` (236 lines, `@code` inline) — company-wide certified-payment register, grouped by project.

- ApprovedSessionGate ✅
  - Header 🔒 — Eyebrow "Finance" · Title "Payment Certificates" · Subtitle (count)
  - SearchSelect ✅ — project filter (blank row = "All projects")
  - AlertBanner (negative) 🔒 — loadError
  - LoadGate ✅
    - EmptyState 🔒
    - Per-project groups 🔒 (Panel-like card, InWorkOrder-sorted) — header row (title + count/total), RecordsTable ⚠️ (5 cols: Certificate+filename/Issued/Valuation claim/Certified amount/File), Preview toggle → inline PdfViewer ✅ row, Download link

No modals.

### Cost codes — `/cost-codes`
`Pages/CostCodes.razor` (366 lines) — the global cost-code master, plus two read-only Xero tracking-category mirrors (Sites, Cost codes) for spelling reconciliation.

- AuthGate ⚠️ (`sessionReady` preamble) → LoadGate ✅ / RequestAccessView ✅ / `!CanManage` text fallback 🔒
- Body section 🔒
  - WorkspaceSectionNav ✅
  - header 🔒 — Eyebrow "JPMS · Commercial" ⚠️, Title "Cost codes" ⚠️, Subtitle (tab-dependent copy) ⚠️
    - ActionCluster ⚠️ — PrimaryButton "New cost code" 🔒 (Ours tab) OR Toolbar ✅ (ToolbarButton "Refresh from Xero" ✅) (Xero tabs)
  - SegmentedTabs 🔒 (hand-rolled pill group: Our cost codes · Xero sites · Xero cost codes)
  - AlertBanner (negative) ⚠️ — load error
  - **Ours pane:**
    - LoadGate ✅ wrapping the whole tab body
      - EmptyState ⚠️ ("No cost codes yet…")
      - controls row 🔒 — "Show retired" checkbox, ExportToExcelButton ✅ (include-retired menu)
      - EmptyState ⚠️ (no active codes, retired hidden)
      - RecordsTable ⚠️ — 5 cols (Code, Name, Sort order, StatusPill ✅×2 states, row actions Edit/Retire-Reinstate text links 🔒)
  - **Xero tabs pane** (Sites / Cost codes, shared layout):
    - LoadGate ✅ wrapping the tab
      - AlertBanner 🔒 ×3 states (not configured / Xero error / category not found)
      - KeyValueList 🔒 (category name · option count · fetched time)
      - RecordsTable ⚠️ — 3 cols (Xero option, StatusPill ✅ Active/Archived, Linked project or System cost code)
      - AlertBanner 🔒 — "Project mappings that match no Xero site option" list
      - AlertBanner 🔒 — "Active system codes with no Xero option yet" list
- Modals: New cost code (Modal ✅ + FormField ✅ ×3) 🔒 · Edit cost code (Modal ✅ + FormField ✅ ×3) 🔒

### Rate Library — `/rate-library`
`Pages/RateLibrary.razor` (88 lines) — the QS rate register.

- AuthGate ⚠️ (hand-rolled) → LoadGate ✅ / RequestAccessView ✅
- Section 🔒
  - WorkspaceSectionNav ✅
  - Header 🔒 — Eyebrow "JPMS · QS" + Title "Rate library" + Subtitle (rate count · stale count)
  - ActionCluster 🔒 — ExportToExcelButton ✅, text link "View stale rates →" 🔒
  - RateTable ✅
- Modals: none

### Stale Rates — `/rate-library/stale`
`Pages/StaleRates.razor` (96 lines) — rates not re-priced in 60+ days.

- Shell preamble 🔒 (`!isLoaded → LoadGate ✅` / `!IsApproved → RequestAccessView ✅`, manual, not ApprovedSessionGate)
- breadcrumb nav 🔒 — "Rate library / Stale rates"
- Title "Stale rates" + Subtitle 🔒 ("Rates not priced in over 60 days…")
- ActionCluster ⚠️ — ExportToExcelButton ✅ (disabled when empty)
- EmptyState 🔒 ("No rates currently stale") or RateTable ✅
- Modals: none


## Financial Reports

### Project Cashflow — `/projects/{ProjectId}/cashflow`
`Pages/ProjectCashflow.razor` (300 lines) — one running statement from claim to practical/project completion, with a side "where these figures come from" glossary.

- ApprovedSessionGate ✅
- ProjectPageShell ✅ (ActiveTab="cashflow")
  - Title "Project Cashflow" 🔒 + Subtitle (links to Cash Forecast) 🔒
  - Two-column grid 🔒: statement | notes
    - LoadGate ✅ (Prominent, whole statement gated on one `StatementReady`)
      - KeyValueList ⚠️ — 11 hand-rolled label/value rows (Project claim, Cash allocated, Left to claim, Drawdowns, Uninvoiced WOs, Unpaid Xero invoices w/ IconButton 🔍 to modal, Retention release 1, Practical completion cashflow, Retention release 2, Project completion cashflow), each row a `flex justify-between` div with `title=` tooltip
      - Panel 🔒 (dashed-optional) "Cost centre overspends — available to buy back" — 2 more KeyValueList ⚠️ rows
      - Panel 🔒 (dashed border) "Potential — unapproved variations" — KeyValueList ⚠️ rows per pending VariationOrder + 2 totals
    - ExplainerPanel ⚠️ (aside, ungated static copy) — 8 muted paragraphs defining each line of the statement
- Modals: UnpaidXeroInvoicesModal ✅ (unpaid cost-of-sales lines + unallocated site bills, opened from the magnifier IconButton)

### Cash Forecast — `/finance/cash-forecast` · `/finance` · `/finance/cash-summary`
`Pages/CashForecast.razor` (221 lines) + `.razor.cs` — company-wide phased cash forecast plus the legacy to-completion statement, on one load.

- ApprovedSessionGate ✅
  - Header 🔒 — Eyebrow (`.eyebrow`) · Title "Cash Forecast" · Subtitle
  - AlertBanner (amber) 🔒 — "Unconfirmed figures" standing notice
  - ForecastKpiStrip ✅ (directors only)
  - AlertBanner 🔒 — project-list load failure
  - FilterBar 🔒 — ProjectMultiSelect ✅ (or disabled `<select>` 🔒 while loading) + ExportToExcelButton ✅
  - LoadGate ✅ ("Loading project cashflows")
    - EmptyState 🔒 — "No projects selected"
    - AlertBanner 🔒 — per-project load failures, inline Retry
    - CashForecastTable ✅ (inline-editable NextExpectedValuationDate / ExpectedMonthlyValuation, expandable categories)
    - Panel 🔒 → ForecastBalanceChart ✅ (directors, ≥2 months)
    - AlertBanner ⚠️ — reconciliation tie-out (positive ✓ or negative variance list)
    - Divider 🔒 ("Position to completion")
    - CombinedStatementCard ✅
    - ProjectCashTable ✅

No modals.

### Weekly Cashflow — `/finance/weekly-cashflow`
`Pages/WeeklyCashflow.razor` (177 lines) + `.razor.cs` — the 13-week working payment plan: every outstanding Xero bill/invoice plus manual items, on one movable grid.

- ApprovedSessionGate ✅ (AuthGate)
  - section 🔒
    - Eyebrow "JPMS · Company" · Title "Weekly Cashflow" · Subtitle 🔒
    - ActionCluster 🔒 — Toolbar ✅ (ExportToExcelButton ✅, ToolbarDivider ✅, ToolbarButton ✅ "Group suppliers", ToolbarButton ✅ "Refresh from Xero"), PrimaryButton "Add item" ⚠️ (kept outside the toolbar, per CLAUDE.md)
    - WeeklyKpiStrip ✅ — a grid of FigureTile ✅ (MetricStat-equivalent, 3–4 tiles: Cash in bank*, To pay this week, Lowest week*, Balance at horizon end*; *director-only)
    - AlertBanner (negative) ⚠️ — moveError, dismissible
    - LoadGate ✅ Prominent — "Loading the weekly cashflow" (gates payables+receivables+plan together)
      - failure/not-configured text 🔒 (three variants: plan failed, Xero not connected, Xero error×2)
      - AlertBanner ⚠️ — truncated-results warning
      - ExplainerPanel-like inline copy 🔒 (grid legend: signs, moved-entry marker, Group suppliers, exclude)
      - WeeklyCashflowGrid ✅ (LedgerTable-equivalent — bands, per-week cells, move/reset/exclude controls, supplier grouping)
      - footnote paragraph w/ links to Aged Payables/Receivables 🔒
- CashflowItemModal ✅ (ref'd, add/edit manual item)
- SupplierGroupsModal ✅ (ref'd, "Group suppliers")

Modals: CashflowItemModal ✅, SupplierGroupsModal ✅.

### Profit Summary — `/finance/profit-summary`
`Pages/ProfitSummary.razor` (196 lines) — company-wide gross-profit-by-project board report: trend grid, summary strip, budget→forecast bridge, table, trajectory & cumulative panels.

- ApprovedSessionGate ✅
- Section 🔒
  - PageHeader ⚠️ — Eyebrow "JPMS · Company" 🔒, Title "Profit Summary", Subtitle
  - AlertBanner (negative, projectsFailed) 🔒
  - ActionCluster 🔒 — ProjectMultiSelect ✅ (disabled `<select>` placeholder while loading, per convention) + ExportToExcelButton ✅
  - RunningProfitPanel ✅ (conditional on SitePnl configured) — leads the page, above the strip
  - ProfitSummaryStrip ✅ (Stat/MetricStat row, conditional)
  - LoadGate ✅ (Prominent; bridge + table share one gate)
    - EmptyState 🔒 ("No projects selected")
    - AlertBanner (negative, per-project load failures + Retry) 🔒
    - BudgetForecastBridge ✅
    - ProfitTable ✅ (SummaryTable, sortable columns) + ProfitTableNotes ✅
  - TrajectoryPanel ✅ (per-job mini-charts, conditional)
  - CumulativePositionPanel ✅ (conditional)
- Modals: none

### Cash Summary — retired stub
`Pages/CashSummary.razor` (10 lines) — no `@page` directive at all; the file is a dead comment-only stub ("Gone: … became the Cash Forecast … DELETE THIS FILE") left behind because a remote session couldn't `git rm` it. Not routable, renders nothing — no component tree to give.

### Finance Overview — `/finance` (route retired)
`Pages/FinanceOverview.razor` (10 lines) — **dead stub, not a page.** No `@page` directive, no markup or code — just a comment explaining the company-wide Financial Summary was replaced by Valuation Summary (`Pages/ValuationSummary.razor`, decision 2026-08-03) and an instruction to `git rm` the file. No tree to draw.

### Valuation Summary — `/finance/…` (route removed)
`Pages/ValuationSummary.razor` (10 lines) — dead stub, no `@page` directive and no markup. A code comment says the page was replaced by `Pages/CashSummary.razor` at `/finance` and asks for the file to be deleted (`git rm`); it renders nothing and has no route. Skipped — no component tree.


## Xero

### Transactions — `/finance/xero`
`Pages/XeroTransactions.razor` (251 lines) + `.razor.cs` — live Xero purchase-invoice feed with a site × cost code pivot.

- ApprovedSessionGate ✅
  - WorkspaceSectionNav ✅
  - Header 🔒 — Eyebrow "JPMS · Financials · Xero" · Title "Transactions" · Subtitle · Toolbar ✅ (ExportToExcelButton ✅ with include-all, ToolbarDivider ✅, ToolbarButton "Refresh from Xero")
  - LoadGate ✅ (whole-page, Snapshot null) / AlertBanner 🔒 (not configured / Xero error) / EmptyState 🔒
  - AlertBanner 🔒 — truncated-fetch warning
  - SegmentedTabs ⚠️ — Transactions · Site × cost code, plus bill/credit-note/fetched-at strapline
  - **Transactions view:** summary line 🔒, SearchInput ⚠️ + ChipTabs ⚠️ (status, toggleable), RecordsTable ⚠️ (8 cols: Date/Supplier/Number/Site/Cost code/Status/Net/Total, click-to-expand row → nested 5-col line-detail SummaryTable 🔒)
  - **Site × cost code view:** explainer text 🔒, SummaryTable ⚠️ — pivot by site → cost code, one column per year + Total, bold site subtotal rows, footer "All sites" totals row

No modals.

### Aged Receivables — `/finance/aged-receivables`
`Pages/AgedReceivables.razor` (330 lines) — outstanding sales invoices aged as in Xero, including drafts Xero's own report omits, expandable per client.

- ApprovedSessionGate ✅
- Body section 🔒
  - header 🔒 — Eyebrow "JPMS · Company" ⚠️, Title "Aged Receivables" ⚠️, Subtitle ⚠️
    - Toolbar ✅ — ExportToExcelButton ✅, ToolbarDivider ✅, ToolbarButton ✅ "Refresh from Xero"
  - LoadGate ✅ (whole page reads one snapshot) / AlertBanner 🔒 ×2 (not configured / Xero error)
  - AlertBanner 🔒 — truncated-results warning
  - StatRow ⚠️ — 3× Stat-like tiles 🔒 (Total receivables, Of which draft, Overdue)
  - sub-header row 🔒 — "By client" + SegmentedTabs 🔒 (Age by due date / invoice date)
  - EmptyState ⚠️ ("Nothing is outstanding…")
  - LedgerTable ⚠️ — dynamic bucket columns + Total, expandable rows (chevron toggle) revealing per-invoice sub-rows with Badge 🔒 (Credit/Draft), tfoot totals row 🔒

### Aged Payables — `/finance/aged-payables`
`Pages/AgedPayables.razor` (331 lines) — company payables aged as Xero ages them, including draft bills Xero's own report omits.

- ApprovedSessionGate ✅
  - Header 🔒 — Eyebrow "JPMS · Company" + Title "Aged Payables" + Subtitle
  - Toolbar ✅ — ExportToExcelButton ✅, ToolbarDivider ✅, ToolbarButton "Refresh from Xero" ✅
  - LoadGate ✅ (Snapshot null) → AlertBanner 🔒 (not configured) / AlertBanner (negative) 🔒 (Snapshot.Error) / content:
    - AlertBanner 🔒 — truncated-results warning
    - StatRow ⚠️ — 3 hand-rolled KPI cards 🔒 (Total payables · Of which draft · Overdue)
    - SegmentedTabs ⚠️ — "Age by due date" / "Age by invoice date"
    - RecordsTable 🔒 — by-supplier, dynamic ageing-bucket columns, click-to-expand rows to per-bill detail, tfoot totals, Badge 🔒 ("Incl. draft"/"Draft"/"Credit")
  - Modals: none


## Audit

### Reconciliation Audit — `/projects/{id}/reconciliation-audit`
`Pages/ProjectReconciliationAudit.razor` (204 lines) — read-only trail of cost-centre moves on the project's valuation report.

- Inline 3-branch gate 🔒⚠️ — LoadGate ✅ / RequestAccessView ✅
- "Not available" text 🔒 (non-commercial roles)
- ProjectPageShell ✅
  - Title "Reconciliation Audit" · Subtitle 🔒 + "Showing X of Y" count 🔒
  - AlertBanner (negative) ⚠️ — loadError
  - LoadGate ✅ Prominent — "Loading the reconciliation audit"
    - negative-state text 🔒 (failed w/ zero items)
    - EmptyState 🔒 "No cost centre moves recorded yet"
    - LedgerTable ⚠️ — 4 cols (When [relative + hover-title exact], Moved by, Line ref mono, Move detail), `@key`-tracked rows
    - LoadMoreButton ⚠️ (cursor-paginated, only when nextCursor is set)

Modals: none.

### Audit Trail — `/audit`
`Pages/AuditTrail.razor` (177 lines) + `.razor.cs` — company-wide register of client-facing interactions and finance recodes, admin/PM/FD only.

- AuthGate ⚠️ (manual preamble) → not-authorized text panel 🔒 for non-CanAccess
- PageHeader ⚠️ — Eyebrow "JPMS" · Title "Audit trail" · Subtitle
- FilterBar ⚠️ — ChipTabs ⚠️ (pathway: All/Client/Subcontractor/Internal, as buttons not links), event-type `<select>` 🔒, project `<select>` (SearchSelect-shaped but hand-rolled, disabled-while-loading) 🔒, trailing "Showing X of Y" count 🔒
- AlertBanner (negative) ⚠️ — loadError
- LoadGate ✅ "Loading the audit trail", wraps:
  - failed-with-no-rows text 🔒, or EmptyState 🔒 ("Nothing recorded…"), or:
  - RecordsTable ⚠️ — 7 cols (When w/ hover-title, Actor, Event, Pathway StatusPill-ish Badge ⚠️, Record link-or-text, Detail, "Open in Outlook" link), row `@key`, no select-all/sort
  - LoadMoreButton ⚠️ ("Load more" / "Loading…")
- Modals: none

### Agent activity — `/agents/activity`
`Pages/AgentActivityLog.razor` (223 lines) — every assistant run (chat + scheduled), newest first, for accountability and cost tracking.

- Inline 3-branch gate 🔒⚠️ — LoadGate ✅ / RequestAccessView ✅
- "no access" text 🔒
- Eyebrow "Assistant" · Title "Agent activity" · Subtitle 🔒
- ChipTabs ⚠️ — "All runs" / "Unattended only" (two buttons, active-state class swap)
- dataFailed Panel 🔒
- LoadGate ✅ — "Loading activity"
- EmptyState 🔒 (copy varies by filter)
- RecordsTable ⚠️ — 9 cols (When, Agent, Ran as [+ "unattended" Badge ⚠️], Action+Summary, Outcome StatusPill inline ⚠️, Tools, Took, Tokens, Cost), clickable rows (only where a Route exists)
- StatRow 🔒 (footer line: run count, total tokens, total cost or "not configured" note)

Modals: none.

### AI Connections — `/settings/ai-connections`
`Pages/AiConnections.razor` (154 lines, `@code` inline) — connected AI-tool sessions (MCP), with revoke.

- ApprovedSessionGate ✅
  - Header 🔒 — Title "AI Connections" (h1), Subtitle (you/anyone's), admin-only "Everyone's connections" checkbox toggle 🔒
  - AlertBanner (negative) 🔒 — error
  - LoadGate ✅
    - EmptyState 🔒
    - RecordsTable ⚠️ — Tool / (User, admin view) / Connected / Last used / SecondaryButton "Disconnect" per row

No modals.


## Admin

### Users — `/admin/users`
`Pages/AdminUsers.razor` (106 lines) — admin-only user directory: invites, roles, revocation.

- AuthGate ⚠️ (`!sessionReady` → LoadGate ✅; `!Session.IsApproved` → RequestAccessView ✅; `ActiveRole != Admin` → Panel ✅ "administrators only" notice)
- Section 🔒
  - WorkspaceSectionNav ✅
  - PageHeader ⚠️ — Eyebrow "JPMS · Admin" 🔒, Title "Users", Subtitle
  - AlertBanner-style Panel ✅ (load-failure message) — shown only if `dataFailed && !IsLoaded`
  - ApprovedUsersPanel ✅ (the whole directory table + invite/role/revoke actions live inside this one domain component)
- Modals: none at this level (owned inside ApprovedUsersPanel)

### Revoked Users — `/admin/users/revoked`
`Pages/AdminRevokedUsers.razor` (106 lines) — the admin's list of revoked user accounts, restore/delete.

- AuthGate ⚠️ → LoadGate ✅ / RequestAccessView ✅ / non-admin notice Panel ✅
- Section 🔒
  - WorkspaceSectionNav ✅
  - Header 🔒 — Eyebrow "JPMS · Admin" ⚠️, Title "Revoked users", Subtitle 🔒
  - dataFailed Panel ✅ text / else RevokedUsersPanel ✅

Modals: none on this page (RevokedUsersPanel owns any restore/delete confirmation internally, not inspected — outside batch scope).

### System — `/admin/system`
`Pages/AdminSystem.razor` (303 lines) — the announced app version (publish bumps every open tab's update prompt) and the company's single tender T&Cs PDF.

- AuthGate ⚠️ → LoadGate ✅ / RequestAccessView ✅ / non-admin notice (Panel ✅) 🔒
- Body section 🔒
  - WorkspaceSectionNav ✅
  - header 🔒 — Eyebrow "JPMS · Admin" ⚠️, Title "System" ⚠️, Subtitle ⚠️
  - AlertBanner (negative) ⚠️ — load failed, whole-page fallback
  - Panel "Announced version" ✅ (IsLoading)
    - version KeyValueList line 🔒 (v-number + caption), published-by line 🔒
    - divider 🔒 → PrimaryButton "Publish update" 🔒 → ConfirmDialog-style inline confirm 🔒 (PrimaryButton "Publish now" + SecondaryButton "Cancel")
  - Panel "Tender terms & conditions" ✅ (IsLoading)
    - AlertBanner (negative) ⚠️ — check failed
    - file link + KeyValueList meta line 🔒 / "not configured" text 🔒 / "nothing uploaded" text 🔒
    - divider 🔒 → confirmation note 🔒 → SecondaryButton "Replace/Upload the document" 🔒 (InputFile ✅ inside)

### Integrations (Admin) — `/admin/integrations`
`Pages/AdminIntegrations.razor` (260 lines, all code-behind in same file) — the portal's one shared external connection (Bluebeam Studio), connect/disconnect via OAuth redirect.

- AuthGate ⚠️ → LoadGate ✅ / RequestAccessView ✅ / role-blocked panel 🔒 (non-Admin)
- section 🔒
  - WorkspaceSectionNav ✅
  - PageHeader-shaped header ⚠️ — Eyebrow "JPMS · Admin" · Title "Integrations" · Subtitle
  - AlertBanner (info/negative, callback outcome) ⚠️ — reads `?bluebeam=connected|failed` once then strips the query
  - Panel ✅ Title "Bluebeam Studio" (IsLoading gate)
    - Not-configured copy 🔒 (env vars missing)
    - Not-connected state 🔒 — copy + PrimaryButton "Connect Bluebeam" ⚠️
    - Connected state 🔒 — status line, last-verified note, AlertBanner (negative) ⚠️ on refresh failure + "Reconnect" PrimaryButton ⚠️, then a Disconnect flow: text link → inline confirm copy + destructive button/SecondaryButton pair 🔒
    - AlertBanner (negative) ⚠️ — action error

Modals: none — disconnect confirmation is inline, not a dialog.

### Trades (Admin) — `/admin/trades`
`Pages/AdminTrades.razor` (341 lines, all code-behind in same file) — curated master list of trades; rename/delete with in-use guarding.

- AuthGate ⚠️ → LoadGate ✅ / RequestAccessView ✅ / role-blocked panel 🔒 (non-Admin)
- section 🔒
  - WorkspaceSectionNav ✅
  - PageHeader-shaped header ⚠️ — Eyebrow "JPMS · Admin" · Title "Trades" · Subtitle
  - EmptyState-as-error 🔒 — load failure, distinct from the Panel's own gate
  - Panel ✅ Title "Curated trades" (IsLoading gate on two combined stores)
    - Add-row 🔒 — text input + PrimaryButton "Add trade" ⚠️ (Enter-to-submit)
    - AlertBanner (negative) ⚠️ / AlertBanner (positive) ⚠️ — action error / action note
    - EmptyState 🔒 — "No trades yet"
    - RecordsTable ⚠️ — 3 cols (Trade, In use by, actions); per-row inline rename (input swap), inline delete-confirm (text + "Delete now"/"Cancel"), else Rename/Delete SecondaryButton ⚠️ pair

Modals: none — rename and delete are both inline row states, not dialogs.

### AI Agents — `/admin/agents`
`Pages/AiAgentsAdmin.razor` (247 lines) — live read of `AgentCatalogue`, each agent card merged with its skills from the store; admin/MD/FD only.

- AuthGate ⚠️ (manual preamble) → not-available text panel 🔒 for non-CanSee
- PageHeader ⚠️ — Eyebrow "Assistant" · Title "AI agents" · two explanatory Subtitle paragraphs
- Agent card list 🔒 (foreach over code-owned catalogue, not a store — needs no gate):
  - Panel ✅-shaped 🔒 per agent — header (DisplayName, agent-key Badge ⚠️, "FRONT OF HOUSE" Badge for orchestrator ⚠️), Description, "Available to" role list
  - "Engages on" / "Starts automatically on" tag rows — Badge ⚠️ chips
  - `<details>` disclosure 🔒 "Working instructions & what 'done' means" — PromptFragment + DoneMeans text
  - Skills sub-panel 🔒 — header + "Add a skill" link, then: "Loading…" text / EmptyState 🔒 / list of skill rows (KeyValueList-ish clickable rows ⚠️ — name, shared-tag, description, Pinned/On-demand + version + off Badge)
  - Trailing greyed "Chaser & Mailbox Triage" card 🔒 — declared-but-not-built placeholder, "autonomous · not built yet" Badge
  - Footer note 🔒 linking to Agent Activity
- Modals: none

### AI Skills Admin — `/admin/skills`
`Pages/AiSkillsAdmin.razor` (280 lines) — the skill-store admin: list of skills, and a full markdown editor for one skill plus its reference documents.

- AuthGate ⚠️ → LoadGate ✅ / RequestAccessView ✅ / "no access" text 🔒
- Section 🔒
  - Eyebrow "Assistant" ⚠️ · Title "AI skills" · explainer paragraph 🔒
  - List mode 🔒:
    - Row 🔒 — count text ("N skills"), PrimaryButton "New skill" ⚠️
    - LoadGate ✅ (skills null) / EmptyState 🔒 ("No skills yet…") / dataFailed text 🔒
    - Panel ✅ → RecordsTable ⚠️ — 8 cols (Skill, Agent, Pinned, Active, Version, Size, References, Updated), clickable rows (no checkboxes, no StatusPill — plain text states)
  - Editor mode 🔒 (replaces list):
    - Row 🔒 — SecondaryButton "Back to the list" ⚠️, saved-tick text, PrimaryButton "Create skill"/"Save new version" ⚠️
    - AlertBanner (negative) ⚠️ — save errors list
    - Panel ✅ — FormField ✅ ×6 (Skill key, Agent select, Display name, Description textarea, Pinned checkbox, Active checkbox, Body markdown textarea)
    - Panel ✅ "Reference documents" (existing skill only) — list of reference buttons 🔒, inline add/edit form 🔒 (FormField ✅ ×4: Ref key, Display name, Description, Body) with PrimaryButton "Save reference" / SecondaryButton "Cancel"

Modals: none (all forms are inline, no `Modal` component used).

### AI actions — `/admin/ai-actions`
`Pages/AiActionsAdmin.razor` (236 lines) + `.razor.cs` — the catalogue of assistant actions and which skills (doctrine) are attached to each action or area.

- Inline 3-branch gate 🔒⚠️ — LoadGate ✅ / RequestAccessView ✅
- "no access" text 🔒
- Eyebrow "Assistant" · Title "AI actions" · Subtitle 🔒 (long explainer, w/ link to /admin/skills)
- dataFailed Panel 🔒
- LoadGate ✅ — "Loading actions"
- count line + SearchInput ⚠️ (raw `<input>`, right-aligned)
- AlertBanner (negative) ⚠️ — orphaned attachments, each with a "Detach" link
- per-area Panel 🔒 (repeated, collapsible)
  - header: expand toggle ▾/▸ 🔒, area name + action count, SkillChip Badge ⚠️ ×n ("via area" variant), SecondaryButton "Area skills" ⚠️
  - SkillPicker 🔒 (inline edit panel, not a Modal) — checkbox grid of skills, AlertBanner (negative) ⚠️ save errors, PrimaryButton "Save" ⚠️ / SecondaryButton "Cancel" ⚠️
  - expanded: per-action row 🔒 — mono action name (+ "confirm-first" note), summary, SkillChip Badge ⚠️ ×n (own + inherited-via-area), "Skills" link ⚠️, same inline SkillPicker 🔒

Modals: none — all editing is inline expand/collapse panels, not dialogs.


## Foot of the rail

### Control Centre (Triage) — `/control-centre` · `/requests/triage`
`Pages/TriageQueue.razor` (410 lines, + .razor.cs) — live-read mailbox triage: one inbox pane, one working pane, seven pathways, no data stored until Apply.

- AuthGate ⚠️ → LoadGate ✅ / RequestAccessView ✅ / role-blocked panel 🔒
- section (workspace shell) 🔒 — full-viewport, draggable-divider layout
  - JewelSpinner ✅ (Overlay) — one busy veil over the whole workspace, not per-pane
  - TriageBar ✅ 🔒-composed — project picker, thread-tag toggles, Apply button, decisions-missing hints (Active view, email selected)
  - OutboxOnlyBar ✅ — Apply-only bar when queued replies exist with nothing selected
  - PanelWorkspace ✅ (Features/Triage/Workspace) — the icon-rail multi-pane engine; content supplied per pane:
    - InboxContent: InboxPaneHeader ✅ + ChipTabs-like view switch 🔒, then one of QueueInboxList ✅ / DiscardedInboxList ✅ / TaggedInboxBrowser ✅ (each: SearchInput ⚠️, RecordsTable-like rows ⚠️, LoadMoreButton-style pager 🔒)
    - EmailContent: TriageNoticesStack ✅ (dismissible AlertBanner stack) then QueueEmailReadingPane ✅ / DiscardedEmailPanel ✅ / TaggedEmailManagePanel ✅, each embedding ReplyComposerForm ✅
    - EmailMirrorContent: EmailMirrorPane ✅ (read-only mirror of the open email for a second window)
    - ClientContent/SubcontractorContent/SupplierContent/InternalContent: PathwayPane ✅ ×4, config-driven (tagging, staged actions, staged create, to-do rows)
    - RecordsContent: RecordExplorerPane ✅ · PreviewContent: PreviewPane ✅ · XeroContent: XeroExplorerPane ✅
    - ComposeContent: NewEmailComposerPane ✅ · OutboxContent: OutboxPane ✅
  - RecentTriageFold ✅ — collapsible strip of just-triaged items below the workspace

Modals: none (the "workspace instead of modals" pattern is explicit in the page's own comments) — all record creation/linking happens in-pane via PathwayPane's staged-create flow.

### Document Triage — `/document-triage` · `/document-control`
`Pages/DocumentControl.razor` (346 lines) — company-wide attachment triage: preview, source email, file to a destination.

- ApprovedSessionGate ✅ (the real AuthGate component, unlike the four pages above)
  - Header 🔒 — Eyebrow "JPMS · projects@jewelbb.co.uk" + Title "Document Triage" + count line (waits for `items`)
  - Two-pane workbench 🔒 (grid)
    - Left: ChipTabs ⚠️ (Queue · Filed · Discarded) · AlertBanner (negative) 🔒 loadError · LoadGate ✅ wrapping EmptyState 🔒 or list of DocumentListItem ✅
    - Right: EmptyState 🔒 (nothing selected) or Panel 🔒 (relative, JewelSpinner ✅ Overlay busy indicator)
      - Filename header + Download link 🔒, received/sent meta line 🔒
      - AlertBanner (negative) 🔒 actionError · AlertBanner (positive) 🔒 fileNote
      - DocumentOutcomeCard ✅ · DocumentPreview ✅ · SourceEmailCard ✅
      - Filing form (Pending only) 🔒: SegmentedTabs ⚠️ (Drawings · Payment certificate · Subcontractor) → per-destination FormField ✅ ×several, plain `<select>` 🔒 project/drawing/claim pickers, SearchSelect ✅ (subcontractor), `<input list>` datalist 🔒 (kind), PrimaryButton (File to…) 🔒 + Discard link 🔒
  - Modals: none — filing is inline, not dialog-driven

### Cost Allocation — `/finance/allocation`
`Pages/XeroAllocation.razor` (390 lines) — full-width Xero purchase-ledger allocation queue: unallocated → allocated/bucketed/disputed, plus a labour-recognition sub-view.

- AuthGate ⚠️ (`!sessionReady` → LoadGate ✅; `!Session.IsApproved` → RequestAccessView ✅)
- Section (full-width, no ProjectPageShell — company-level page) 🔒
  - WorkspaceSectionNav ✅
  - AllocationPageHeader ✅ (Features/Xero/Allocation) — Sync from Xero / Re-check buttons, busy states
  - AlertBanner (sync/error messages) 🔒 — two dismissable inline strips
  - LoadGate ✅ (ledger counts) → EmptyState 🔒 ("Nothing stored yet — Sync from Xero")
  - MatchedLinesBanner ✅ — shown only on Unallocated tab when fully-matched lines exist
  - LoadGate ✅ wrapping AllocationTabBar ✅ (status chips + per-project sub-tabs + labour tab, all with CountBadge)
  - ActionCluster 🔒 — ExportToExcelButton ✅ (include-all), SearchSelect ✅ (Allocated tab project filter), SearchInput ⚠️ (inline, not the canonical component)
  - LabourSectionStrip ✅ — covered-lines toggle (labour sub-tab only)
  - Conditional bulk-action bars: LabourBulkActions ✅ / QueueBulkActions ✅ / AllocatedBulkActions ✅ (selection-count driven, one at a time)
  - LoadGate ✅ (queue lines)
    - BucketChipStrip ✅ (Bucketed tab only)
    - RecordsTable ⚠️ — hand-rolled `<table>`, header via AllocationTableHeader ✅, SelectAllCheckbox ✅; body switches per-tab row component: LabourLineRow ✅ / QueueLineRow ✅ / DisputedLineRow ✅ / AllocatedSummaryRow ✅ (all `@key`ed)
    - EmptyState 🔒 (no lines / no matches copy)
    - StatRow 🔒 (line count + net) + LoadMoreButton-style Prev/Next pager 🔒 (text-underline buttons, not a component)
- Modals: Split-across-projects (Modal ✅ + LedgerLineSummary ✅ + SplitEditorForm ✅), SendLinesModal ✅ ×2 (send to cost centre / send to project), DisputeLineModal ✅, DisputeDiscussionModal ✅, Invoice document viewer (Modal ✅ + LedgerLineSummary ✅ + InvoiceViewerActions ✅ + InvoiceDocumentPreview ✅)

### Valuation Report — `/projects/{id}/valuation`
`Pages/ProjectValuation.razor` (378 lines) + `.razor.cs` — the project's claim-by-claim billing workflow: one card per selected claim driving invoices raised against the valuation report.

- ApprovedSessionGate ✅
- ProjectPageShell ✅ (ActiveTab="valuation")
  - Header row 🔒 — Title "Valuation Report" (h2, no PageHeader) · Toolbar ✅ (ToolbarButton "Download PDF", ExportToExcelButton ✅, ToolbarDivider ✅, ToolbarButton "Client references") · claim `<select>` 🔒 (disabled+"Loading claims…" while not ready) · SecondaryButton "New claim" 🔒
  - AlertBanner (negative) 🔒 — dismissible actionError
  - LoadGate ✅ ("Loading the claim")
    - ClaimProgressDialog ✅ (modal, IsOpen-bound)
    - Claim card 🔒 — name + StatusPill ⚠️, stage-hint text, DropdownMenu ✅ "Actions", 6-step inline Stepper ⚠️ (done/current dots), ONE stage-driven PrimaryButton 🔒 (9-way switch: Preapprove/Raise invoice/Send/Approve/Issue/Amend/Start next/Confirm)
    - EmptyState 🔒 — "No claim selected"
  - SecondaryButton "Add line" 🔒 (Draft claim only)
  - LoadGate ✅ ("Loading the valuation report")
    - ValuationReportTable ✅ → ExtraSections: ValuationInvoicesSection ✅, ValuationSnapshotsSection ✅
  - Modals: Modal ✅ (Add/Edit line → ValuationLineForm ✅) · ClientCostReferencesModal ✅ · Modal ✅ (snapshot → ValuationSnapshotViewer ✅) · ValuationSnapshotEmailModal ✅ · Modal ✅ (Start claim, hand-rolled date/name fields 🔒) · Modal ✅ (Rename → FormField ✅) · Modal ✅ (Delete claim, confirm copy) · Modal ✅ (Confirm nudge)


## Off the rail

### Dashboard — `/dashboard`
`Pages/Dashboard.razor` (92 lines) — the signed-in home; routes to one of three role-specific home views (client logins bounce to `/client` entirely).

- LoadGate cover ✅ (isLoaded false)
- RequestAccessView ✅ (not approved)
- AdminHome ✅ (Role.Admin)
- RoleHome ✅ (any other role)

No page-owned furniture at all — Dashboard is purely a role router; each destination component owns its own anatomy (out of batch scope). No modals.

### Project Portfolio — `/projects`
`Pages/Projects.razor` (159 lines) — the full project list, live-work-first.

- AuthGate ⚠️ (hand-rolled, plainest variant — no LoadGate, just muted "Loading projects…" text) → RequestAccessView ✅
- Section 🔒
  - Header 🔒 — Eyebrow ✅-style text/Title "Project portfolio"/Subtitle (active count + create-project hint), PrimaryButton "+ New project" 🔒 (role-gated)
  - FilterBar ⚠️ — checkbox "Overdue valuations only" (with CountBadge 🔒) + checkbox "Show completed" + ExportToExcelButton ✅
  - EmptyState 🔒 — no live project with an overdue valuation
  - ProjectsTable ✅
  - Modals: Modal ✅ "New project" → NewProjectForm ✅

### RFI Dashboard — `/rfis`
`Pages/RfiDashboard.razor` (312 lines) — portfolio-wide RFI register across all projects, grouped/ordered by project.

- Shell preamble 🔒 — plain "Loading RFIs…" text (NOT a LoadGate — deviates from the AuthGate convention used elsewhere) → RequestAccessView ✅
- PageHeader ⚠️ — Eyebrow "JPMS · RFIs" · Title "RFI register" · Subtitle "N total · N active · N overdue · across N projects"
- FilterBar ⚠️ — ChipTabs ⚠️ (Active/Closed/All, each "Label · count") + ExportToExcelButton ✅ (ShowIncludeAllRows/IncludeAllLabel)
- AlertBanner (negative) ⚠️ — loadError, with SecondaryButton "Retry" ⚠️
- EmptyState 🔒 ("No RFIs recorded") or RecordsTable ⚠️ — 9 cols (Project name+ref, Ref, Subject + "Variation" Badge, Drawing/Detail, Issued, Response Due, Days Out, Value, Status StatusPill cells), whole-row click → request detail page
- Modals: none

### My Day — `/my-day`
`Pages/MyDay.razor` (15 lines) — **redirect stub only.** `OnInitialized` immediately navigates to `/dashboard` (replace:true); the worker's day view now lives on the Dashboard's RoleHome → MyDayWorkspace. No tree to draw.


## Outside the rail

### My record — `/portal`
`Pages/PortalHome.razor` (366 lines) + `.razor.cs` — a subcontractor's own compliance-document, work-order and variation-request view (Subcontractor-portal login).

- Inline 3-branch gate 🔒⚠️ (not ApprovedSessionGate) — LoadGate ✅ / RequestAccessView ✅
- "Not available" text 🔒 (wrong role)
- "No record linked" text 🔒 (role but no linked subcontractor)
- LoadGate ✅ — PortalStore
- "No record linked" text 🔒 (record still null)
- section 🔒 max-w-3xl
  - header: Title = company name · Subtitle = trades/contact 🔒
  - AlertBanner (amber) ⚠️ — "Documents needing attention" list
  - Panel 🔒 "Your compliance documents" — header w/ count, list of documents (name, file link, dates), ComplianceStatusPill ✅ per row, `<details>` "Previous versions" EmptyState-adjacent 🔒
  - Panel 🔒 "Upload a document" — datalist-backed text input + date input (hand-rolled, not FormField) 🔒, InputFile ✅, SecondaryButton "Upload document" ⚠️, AlertBanner (neg/pos) ⚠️
  - Panel 🔒 "Your work orders" — header w/ count, list: number/title, project+dates, value, StatusPill inline ⚠️, "View & accept" link, `<details>` line-items list 🔒
  - Panel 🔒 "Your variation requests" — header w/ count, list: title, WO ref, value, StatusPill inline ⚠️, rejection reason, "Withdraw" link
  - Panel 🔒 "Raise a variation" — work-order `<select>` 🔒, value/title/description inputs 🔒, SecondaryButton "Send variation request" ⚠️, AlertBanner (neg/pos) ⚠️

Modals: none (all forms are inline panels, not dialogs).

### Work order (subcontractor portal) — `/portal/work-orders/{id}`
`Pages/PortalWorkOrderView.razor` (172 lines, code-behind in same file) — a subcontractor's own issued work order, printable, with one-click electronic acceptance.

- AuthGate ⚠️ (isLoaded flag) → LoadGate ✅ / RequestAccessView ✅
- Not-available EmptyState 🔒 (no linked subcontractor record)
- LoadGate ✅ — PortalStore not yet loaded
- Not-found EmptyState 🔒 — order not found / not issued, link back to portal
- section 🔒
  - Toolbar-shaped bar 🔒 (`.po-toolbar`, print-hidden) — back link, Badge ⚠️ "Accepted {date}" or PrimaryButton "Accept work order" ⚠️, SecondaryButton "Print / save PDF" ⚠️
  - AlertBanner (negative) ⚠️ / AlertBanner (positive) ⚠️ — accept error / accept note
  - PurchaseOrderSheet ✅ — the printable PO document itself (order, lines, supplier + site address, approver, payment terms)

Modals: none.

### Your projects — `/client`
`Pages/ClientPortalHome.razor` (101 lines) — a client's own view of their projects' RFIs and variation orders (Client-portal login).

- Inline 3-branch gate 🔒⚠️ — LoadGate ✅ / RequestAccessView ✅
- "Not available" text 🔒 (wrong role)
- "No account linked" text 🔒 (role but no linked client)
- section 🔒 max-w-5xl
  - Title "Your projects" · Subtitle 🔒
  - AlertBanner (negative) ⚠️ — loadError
  - Sub-header "Requests for information" 🔒 → LoadGate ✅ → ClientRequestList ✅ (Features/ClientPortal)
  - Sub-header "Variation orders" 🔒 → LoadGate ✅ → ClientVariationList ✅ (Features/ClientPortal)

Modals: none (list items link out to detail pages, not dialogs).

### Client Request View — `/client/requests/{RequestId}`
`Pages/ClientRequestView.razor` (101 lines) — a client portal user's own request thread.

- AuthGate ⚠️ (manual `!sessionReady` preamble) → RequestAccessView ✅ → not-a-client-account text 🔒 → nested LoadGate ✅ "Loading request"
- EmptyState 🔒 "Request not found" (record is null after load)
- "← Your projects" back link 🔒
- header 🔒 — Eyebrow (Reference · Kind · ProjectName) · Title · Subtitle (Status · raised date · responded date)
- Panel ✅ "Detail" (conditional, if description present)
- Panel ✅ "Response" (conditional, if response text present)
- ClientConversationPanel ✅ (Load/Post callbacks — the client-side message thread)
- Modals: none

### Client Variation View — `/client/variations/{VariationOrderId}`
`Pages/ClientVariationView.razor` (93 lines) — client-portal read view of one Variation, with a Q&A thread back to Jewel.

- AuthGate ⚠️ (`!sessionReady` → LoadGate ✅; `!Session.IsApproved` → RequestAccessView ✅; `!CanAccess` → EmptyState 🔒 "for client accounts"; `!loaded` → LoadGate ✅; `record is null` → EmptyState 🔒 "not found")
- Section (max-w-3xl) 🔒
  - Back link "← Your projects" 🔒
  - PageHeader ⚠️ — Eyebrow (ref · "Variation order" · project name) 🔒, Title (variation Title), Subtitle (StatusPill-as-text ⚠️ status · value · approved date)
  - Panel ✅ "Detail" (conditional on non-empty description)
  - ClientConversationPanel ✅ (Load/Post callbacks — the Q&A ActivityFeed + composer)
- Modals: none

### Sign In — `/` · `/login`
`Pages/Login.razor` (113 lines, `LandingLayout`) — email/password sign-in with returnUrl handling.

- Split layout 🔒 — image panel 🔒 (right on desktop) / form panel 🔒 (left)
  - JewelIcon ✅ + wordmark 🔒
  - Title "Welcome back" + Subtitle 🔒
  - Form 🔒 — email `<input>` 🔒, password `<input>` 🔒 + "Forgot password?" link 🔒, AlertBanner (negative) 🔒 inline error text, PrimaryButton "Sign in" 🔒
  - Footnote copy 🔒 ×2 (access-request note, product blurb)
- Modals: none

### Signing out — `/logout`
`Pages/Logout.razor` (21 lines) — pure redirect stub: clears the session and auth state, then navigates to `/`. No component tree to document beyond a centered "Signing you out…" message inside LandingLayout 🔒.

### Reset your password — `/forgot-password`
`Pages/ForgotPassword.razor` (89 lines), `@layout LandingLayout` — self-service reset request; deliberately answers identically whether or not the address has an account.

- split-screen layout 🔒 (LandingLayout convention: form pane + image pane, image reordered above form on mobile)
  - image panel 🔒 (`about-team.png`, object-cover)
  - form panel 🔒
    - JewelIcon ✅ + wordmark "Jewel Project Management System"
    - if sent: confirmation view 🔒 — JewelIcon ✅ mark, Title "Check your email", Subtitle = server acknowledgement text, "Back to sign in" link
    - else: Title "Reset your password" · Subtitle 🔒
      - hand-rolled email field 🔒 (label + `<input>`, not FormField)
      - PrimaryButton "Send reset link" ⚠️ (raw styled `<button>`, not `.btn-primary` class — bespoke gradient/rounded style distinct from the app-shell button classes) 🔒
      - "Back to sign in" link 🔒, footnote about invite-only accounts 🔒

Modals: none.

### Set Password — `/set-password`
`Pages/SetPassword.razor` (162 lines) — single-use invite/reset link → new password → auto sign-in. Unauthenticated, `LandingLayout`.

- LandingLayout ✅ (split-screen: image panel + form panel)
  - JewelIcon ✅ + wordmark 🔒
  - State switch 🔒: JewelSpinner ✅ ("Checking your link") → EmptyState 🔒 ("Link expired", SecondaryButton "Send me a new link" + back-to-sign-in link) → form
  - Title/Subtitle ⚠️ (copy adapts to invite vs reset via `isReset`)
  - Form 🔒 — FormField ⚠️ ×2 (New password, Confirm password, both hand-rolled `<input type="password">`), helper text, AlertBanner (negative, inline error text) 🔒, PrimaryButton (full-width, label swaps Set/Save × Setting…/Saving…) ⚠️
- Modals: none

### Connect an AI tool — `/connect/authorize`
`Pages/ConnectAuthorize.razor` (174 lines) — the OAuth consent screen an AI connector (Claude, Perplexity) lands on before the portal mints its single-use code.

- LandingLayout 🔒 (centered card shell, distinct from the app shell — no AuthGate/ProjectPageShell)
  - brand row 🔒 — JewelIcon ✅ + wordmark
  - LoadGate ✅ ("Checking the request") / AlertBanner (negative) ⚠️ (error card, terminal state) / consent Panel 🔒
    - Title "Connect {clientName}" ⚠️
    - consent copy 🔒 (who's asking, as whom)
    - ExplainerPanel ⚠️ — scope-of-access note (read + posting/to-dos/skills, always-as-you, disconnect link)
    - AlertBanner (negative) ⚠️ — approve error
    - ActionCluster ⚠️ — SecondaryButton "Cancel" 🔒, PrimaryButton "Approve"/"Connecting…" 🔒

