# Jewel Bespoke Build — working notes

## Terminology

- **Programme** is the canonical term for the project's plan of work and the project tab that holds it (the programme itself, its claims documents, and its correspondence). Never call it "Schedule" (or US-spelled "Program") in UI copy, code identifiers, routes, or docs. "Scheduling"/"schedule" survive only in persisted backend identifiers (e.g. `RecordType.Scheduling`, the `JPMS/SCH-` mail tag, API routes), immutable EF migrations, and the distinct retention-release concept `RetentionSchedule`, which is not the programme.
- **Valuation invoice** is the canonical term for an amount of money Jewel has claimed for the client to pay (raised against the current valuation; lifecycle: Raised — accounts' first move once the project team has valued & locked the claim; files a draft and freezes the report snapshot, sends nothing — → Submitted, i.e. the claim recorded as sent to the architect/client (the portal never emails it; "Record claim sent") → Approved → Issued → Paid; one click per material stage, driven from the claim card on the valuation page, and every button either creates a portal record ("Raise …") or records an outside event ("Record …") — none says "send"). Never introduce "cash call", "payment application", "application for payment", or "client invoice" for this concept in UI copy, code identifiers, or docs. "Cash call" survives only in historical meeting notes and immutable EF migrations. See `docs/00-business-context/glossary.md`.
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
  overlay of `#app` (not inside it, which is what made Blazor wipe it a beat too early), held up
  until `ApprovedSessionGate` — or `LandingLayout`, for the routes outside it — takes it down
  through `js/boot-screen.js`, with a failsafe that dismisses it anyway once the app has rendered.
  There is no in-app full-page loader: `ApprovedSessionGate` reads `SessionService.IsLoaded`
  synchronously, so an in-app navigation never flashes a session check nobody is waiting on.
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
