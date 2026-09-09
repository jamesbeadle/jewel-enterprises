# JPMS site map — every page, what it does, what's done elsewhere

Routes are as the Blazor router spells them; `{project}` is the picked project's
id. Legacy aliases are noted — they still land (labels move, slugs don't).

## Standing work queues

### Control Centre — `/control-centre` (alias `/requests/triage`)
The live mailbox intake queue and router for ALL correspondence across every
project — the projects mailbox read in place; triage only adds or removes
category tags, nothing ever moves. Two-window workspace with panes on an icon
rail: Inbox (Queue / Tagged tabs), Email + read-only mirror, System Tags,
System Actions, Records, Xero, Subcontractor Comms, Outbox, Compose, Preview.
The triage bar holds the selected email's project (auto-matched where possible),
two mandatory Yes/No decisions — **Relevant Event for Programme** and **Entire
thread** — an armable Discard, and ONE **Apply** button that lands everything
staged (tags, new records, to-dos, replies and forwards, "Send to document
triage" ticks) in a single act. Unfinished work parks per email as a draft.
Records (RFIs, defects, variations, bid packages, to-dos) can be raised from an
email in System Actions; the New email composer for any fresh outbound email
also lives here. Attachment filing is NOT done here — the "Send to document
triage" tick copies files to `/document-triage`.

### Document Triage — `/document-triage` (alias `/document-control`)
The attachment triage queue for all projects: each item is a point-in-time copy
of one email attachment ticked over from the Control Centre. Left: Queue /
Filed / Discarded tabs. Right: preview (PDF/images inline), collapsible source
email, and the filing form with three destinations — **Drawings** (new drawing
or revision, code/revision/title prefilled from the filename, lands as an
unapproved revision), **Payment certificate** (project, optional valuation
claim, number, amount, issued date), **Subcontractor document** (subcontractor,
kind, expiry). Discard is restorable, never a delete. Emails themselves are
triaged in the Control Centre; a filed drawing's approval follows the normal
drawings workflow.

### Xero Cost Allocation — `/finance/allocation`
The workbench reconciling Xero with the projects: every cost-of-sales purchase
line (nominal accounts starting 3) is allocated to a project and master cost
centre, or split across several. Tabs: Unallocated, one per project, Allocated,
Buckets, Disputed, Ignored; per-row Allocate / Set / Split… / Allocate to
bucket / Ignore / Dispute…, bulk bars, "Allocate all matched" banner, Sync from
Xero, Re-check matches, search, Excel export. Allocating every line of a draft
bill confirms its Sites/Cost Code tracking to Xero **and approves the bill**.

### Valuation Report — `/projects/{project}/valuation`
The picked project's LIVE valuation report — the system's flagship output,
internal only (clients only ever receive frozen snapshots). Work runs in
monthly claims; the claim card's stepper is **Value & lock → Claim → Approve →
Invoice → Paid → Confirm & roll over**, one primary button per stage, with an
Actions menu for rename, reopen, record rejection/payment, issue without
approval, delete. Lines are added/edited while the claim is Draft; Valuation
Invoices, Snapshots and Correspondence sections sit inline; working-copy
PDF/Excel exports always available. Each claim is a linkable record in the
Control Centre (Client → Valuation claims): mail tagged to the live claim
reads back in Correspondence and on every snapshot frozen from it; rolling
over starts the next claim with its own tag. Approving variations — which
writes their lines here — happens on the variation record, not here.

## Project folder

### RFI register — `/projects/{project}/requests`
The project's RFIs (contractual response-due dates) with legacy General
requests one tab behind (requests are being sunset — nothing raises a new one).
Tabs RFIs/Requests, Active/Closed/All chips, free-text search. Actions: Raise
RFI (manual, no email behind it), Excel export, row status chip, tick RFIs →
"Prepare email drafts" (one Outlook draft each from the projects mailbox, PDF
attached — sent from Outlook, never here), merge exactly two open General
requests. Most RFIs are raised from emails in the Control Centre.

### Request / RFI detail — `/projects/{project}/requests/view/{requestId}`
One request's full working papers: detail, official form (itemised queries,
basis, response/action, impact-if-late), response, attachments, tagged email
conversation, audit history; Request and Official-form panes (deep-link
`?tab=official`). Status pill dropdown; Email button (Outlook draft — fresh or
reply-all into the tagged chain, PDF attached); Promote to RFI; Actions menu:
record response, close (date + agent gate), edit subject/dates/description/
official form, critical-path tag, create bid packages, return to Control
Centre, delete (admin). "Find & tag emails" adds correspondence. The Create
Variation Order Quote form (variation_draft) opens here.

### Variation Orders — `/projects/{project}/variations` (alias `…/requests/variations`)
The variation book — one row per variation (ref, title, originating request,
status, value, dates, work orders), search, Excel export. Ladder: Quoting →
Issued → Awaiting AI → Approved/Rejected; **only Approved writes value onto the
valuation report**. Status chip moves side-effect-free stages directly; Approve
and post-approval transitions link to the variation itself; pre-approval
Rejected confirms first (terminal). Subcontractor variation requests are
accepted (creating a variation) or rejected here; approved variations with a
selected sub get "Issue WO". "Add variation manually" (manual_variation) for a
standalone variation. RFI-led drafting happens on the request page.

### Variation detail — `/projects/{project}/variations/{id}` (alias `…/voq/{id}`)
One variation document through the ladder, with the official Variation Order
PDF (scope, commercial basis, programme impact, exclusions — all editable in
place). Status pill moves free stages; "Approved…" opens the approve modal
where priced lines (one per cost centre) are built and written to the Valuation
Report, CVR and budgets; post-approval: Edit lines, Revise value, Issue work
order, Return to quoting (un-approve), Reject (reverses the writes). "Record
agreed tender" captures the chosen sub and value pre-approval; an Awaiting-AI
banner shows whether an Architect's Instruction is linked. Communications panel
reads tagged mail with reply/forward and "Find & tag emails".

### Architect's Instructions — `/projects/{project}/architect-instructions`
Register of formal AIs — the written authority a variation at Awaiting AI needs
before approval. Instructions arrive by email (imported from the attachment) or
are filed via "File an instruction": architect's own reference, date, title,
issuing architect's email, notes, optional document ("Awaited" until it
arrives), tick-boxes for the variations covered (Awaiting-AI first). One
instruction routinely covers several variations. Rows link to variations,
offer Link/Unlink, the stored document, Delete (variations survive).

### Valuation Snapshots — `/projects/{project}/valuation-snapshots`
Read-only register of frozen valuation report snapshots — what the client was
actually sent. A snapshot freezes automatically when a valuation invoice is
raised, and again on submit/issue after an amendment; superseded rows stay,
muted. Row click opens the frozen report; branded PDF download; Email button
(report-running roles) drafts the report to the client from the shared mailbox
— nothing sends from this page. Managing invoices/snapshots is done on the
Valuation Report tab.

### Drawings — `/projects/{project}/drawings`
Drawing register with revisions: each row a drawing with latest approved
revision, pending/archived counts, pipeline status, "ambiguous" badge for
uploads that couldn't auto-classify (`…/drawings/ambiguous` is the queue,
reached by URL). Approved-only toggle; "+ Upload drawing" (Admin/MD/PM); Excel
export. Incoming drawing files from correspondence are filed here from Document
Triage, not uploaded here. Detail page (`…/drawings/{drawingId}`): revision
history + inline viewer, "+ Upload new version", Delete drawing (permanent).

### Programme — `/projects/{project}/programme`
Four sub-tabs: **Programme** (Gantt against latest baseline, movement banner
when completion slips — banner offers Raise NOD per delay event), **Claims**
(Raise Notice of Delay, Raise Extension of Time, Record LADs claim), **Critical
Path RFIs**, **Relevant Events** (emails tagged to the project's scheduling
bucket, read live; "Reply in thread" creates a reply-all Outlook draft). Add
task / Add dependency / Baselines build the programme. Emails become Relevant
Events in the Control Centre; an RFI is marked critical path on its own page.

### To-do — `/projects/{project}/todos` and master `/todos`, detail `/todos/{id}`
Project list and company-wide master (project filter; MD/admin see everything,
others see items for a role they hold minus items pinned to someone else).
Board (drag Open↔Done) or List views; "Add to-do" modal (project blank =
company-wide, MD/admin only; title, assignee, due date, notes). Detail page:
Mark done/Reopen, armed Delete, reassign (role, optionally pinned to a person),
move (project / company-wide), linked to-dos via shared tagged mail, tagged
emails read live and answerable, new outbound email filed to the item. To-dos
from an email are staged in the Control Centre.

### Progress — `/projects/{project}/progress`
Client-facing progress reports assembled from progress updates (titled photo
groups with description, work date, recorded weather). "+ New report" / Edit;
Download PDF (regenerated on every download); "+ Record progress"; photos
added/deleted; two-click deletes.

### Defects — `/projects/{project}/defects`
Defect register — each defect's sequential **DEF-####** reference is also its
mailbox tag stem, so tagged mail reads back live under it. "Raise defect"
inline form (location, assigned-to email, description); Status dropdown walks
Open → In progress → Resolved → Verified; "Emails" expands a row to its tagged
correspondence. Also raised from a subcontractor email in the Control Centre
(System Tags → Create new → Defect); further tagging happens there too.

### Communications — `/projects/{project}/communications`
Roll-up of ALL correspondence tagged to this project's records, read live,
newest first. Pathway filter (Client / Subcontractor / Internal) and "Tagged
to" record-type dropdown; Reply/Forward opens the composer above the list —
sending happens there and then from the projects mailbox, and the sent copy
files back by the thread's tags. Tagging and new emails: Control Centre.

### Useful Information — `/projects/{project}/useful-information`
Titled free-text internal notes — door codes, key safes, site access. API-gated
to internal roles; can never reach a client, architect or subcontractor login.

### Project Settings — `/projects/{project}/settings`
Four panes: Details (stage, entity, PM, client, site address, Xero "Sites"
mapping — "Not set" blocks the Xero write-back), Deposits/retentions/valuation
(next valuation date, retention profile), Contract (executed document + terms),
Correspondence (the profile that routes documents the project issues).

### Projects — `/projects`
Portfolio register (reference, name, client, entity, stage), completed hidden
unless "Show completed". "+ New project" (MD/PM) — though projects normally
come from a won lead; Excel export. `/projects/{project}` bare = redirect to
the role's first tab; always link a specific tab.

## Subcontractor folder

### Bid Package Invites — `/projects/{project}/bid-package-invites`
Register of the project's bid packages (title, trade, status; closed sort
last). "New bid package" (Admin/MD/PM): title + trade → Draft → straight to
detail. "Suggest bid packages": AI reads the live valuation report and proposes
trade packages for remaining works; ticked suggestions become Drafts. Excel
export. All build-out happens on the detail page.

### Bid package detail — `/projects/{project}/bid-package-invites/{id}`
Five tabs: **Details** (specification summary + line schedule, edited together
in one "Edit details" dialog; lines link to a cost centre or a variation),
**Tender list** (add subs from the directory, quick-add, "Find local
subcontractors" web search, Invite email composer — sends from the projects
mailbox, tenderers in BCC, standard T&C PDF auto-attached), **Submissions**
("Extract information" AI-reads a filed tender email against the line
schedule; "Record tender manually"; **Award** — raises the work order),
**Documents**, **Emails** (tagged thread with Reply/Forward).

### Work Orders — `/projects/{project}/work-orders`
Financial roll-up grouped by cost centre or supplier (remembered per user):
committed, paid (from Xero bills), remaining, left to invoice; supplier search;
Excel export. "Add work order" raises a manual order (same modal edits one);
drafts await two-click **Approve** (mints the next WO number and emails the PO
from the projects mailbox) or Reject (terminal). Issued lines offer PO
(printable page), Re-code (move/split across cost centres without changing
value) and Cancel (MD/FD only; refused while bills linked or money paid).
Awarding a tender happens on the bid package; invoice linking on WO Allocation.

### Purchase order — `/projects/{project}/work-orders/{id}/po`
The printable PO as issued: supplier and site addresses, approver, payment
terms, status pill (Draft / Awaiting supplier acceptance / Accepted / Rejected
/ Cancelled). "Print / save PDF"; "Draft email to supplier" (Outlook draft in
the shared mailbox; hidden for draft/rejected/cancelled). Record-keeping
attachments panel below never prints or goes to the supplier.

### WO Allocation — `/projects/{project}/work-order-allocation`
Ties each Xero purchase line to the work order it pays: headline cards (cost of
sales, linked, not linked, fully invoiced); expand an order to unlink a slice;
in the invoice-line queue pick the paying order or split a line (options that
can't take its value disabled). Linking recodes the whole order to the
invoice's cost centre; an order can never be invoiced past its value. Unlinked
lines count as non-work-order cost of sales on Financials.

### Subcontractor portal — `/portal`, `/portal/work-orders/{id}`
The external login's home ("My record"): a subcontractor invited from their
directory entry signs in scoped to their own company's data — their work
orders and documents. Internal users never land here; subcontractors land here
instead of `/dashboard`.

### Subcontractor communications — `/subcontractors/communications`
Every email tagged with the SubComms family (general + Chaser / Info request /
Materials / H&S categories), read live, newest first, category chip filter.
Expand to full body; Reply/Forward send from the projects mailbox in place, the
sent copy inheriting the thread's tags. Tagging/untagging: Control Centre.

## Internal folder

### Directory — `/directory`, entry `/directory/{subcontractorId}`
Unified directory: chips for Clients, Architects, Subcontractors (default),
Internal staff. Subcontractors: search, Type filter, "+ Add company"
(Admin/MD/FD), "Import from Xero", Consolidate (pick master, winning value per
field; references re-point). Clients/Architects render read-only with links to
`/clients` and `/architects` where creation/editing lives. Entry page: company
and contacts, Xero-link badge (name should exactly match the Xero supplier so
invoices line up on WO Allocation), trades against the curated list, additional
contacts, "Invite to portal" (scoped login), statement of account (every WO with invoices claimed against it — PDF
or email draft).

### Registers — `/registers` · Policies — `/policies`
Registers: the Monday replacement — insurances, subscriptions, vans, trade
accounts. Policies: staff sign-off forms (NDAs, policies, H&S acknowledgements).

### Architects — `/architects` · Clients — `/clients`
Practice/account registers; the contact email held here is where a project's
request documents go when that party is on the project. New/Edit/Contacts
buttons, Excel export.

## Time folder

### Labour — `/projects/{project}/labour`
Weekly timesheet approval grid + daily site register + subcontractor
settlement. Prev/Next week; tick and bulk-approve submitted rows (**only
approved time posts to Financials as cost**; per-cost-code budget hard-block
server-side), Adjust (half-hour steps, re-code) or Reject with a reason the
worker sees; manual-entry form for missed sign-outs; assign/remove workers;
"Mark invoice lines as covered…" reconciles Xero invoice lines against
approved timesheet cost; Excel exports. Workers log time on My day.

### Workers — `/labour/workers`
Company-wide registry of day-rate operatives with cost rates. Add/Edit modal
(name, portal email, day rate stored as hourly = day rate ÷ 8, phone, linked
subcontractor, Active toggle); a worker with history can only be deactivated.
Rate changes apply to future approvals only — approved timesheets keep their
snapshotted rate. `/labour/overview` is the cross-project labour view;
`/labour/xero-mapping` maps labour to Xero.

## Finance folder

### Project Financials — `/projects/{project}/financials`
The cost ledger by cost centre: contract sales value, % complete, target cost,
committed WOs, actual cost of sales (Xero spend from `/finance/allocation`),
drawdown/overspend by sign, forecasted cost of sales. Figures click through to
modals (valuation lines, work orders, invoices — an invoice can be linked to a
WO or moved between centres); edit cost % complete inline; lock finalised lines
(realising drawdown to P/L); roll rows into named groups; reconciliation
packages below.

### Payment Certificates — `/finance/payment-certificates`
Company-wide register grouped by project (live-work order) with certified
totals: certificate number, issued date, the valuation claim certified, amount,
file (PDF preview / download). Only control is the project filter. Certificates
arrive filed from Document Triage.

### Cost Codes — `/cost-codes`
The global cost-centre master everything groups by. Tabs: Our cost codes (New,
Edit, Retire/Reinstate — retired codes keep history; Excel export), Xero sites
and Xero cost codes (tracking-category options exactly as Xero holds them,
Refresh from Xero). Use exact spellings from here rather than guessing.

### Rates — `/rate-library`, `/rate-library/stale`
Read-only rate library by trade (description, supplier, unit, rate,
last-priced; header counts stale = not priced in 60+ days). Stale page lists
what to re-price before the next tender.

## Financial Reports folder

### Project Cashflow — `/projects/{project}/cashflow`
The project's to-completion cash statement: project claim, cash allocated
(received + retention held), left to claim, then cash still to move (cost
centre drawdowns, uninvoiced WOs, unpaid purchase invoices) through retention
releases 1 and 2 to practical/project completion totals. Cards for overspends
available to buy back and (dashed) potential from unapproved variations.
Read-only — inputs edited on Financials, the valuation report, Settings
(retention terms) and `/finance/allocation`.

### Cash Forecast — `/finance/cash-forecast` (aliases `/finance`, `/finance/cash-summary`)
Company time-phased forecast: every known future cash movement in its expected
month; directors-only KPI strip and closing-bank-balance row seeded from Xero,
lowest month flagged. Project multi-select (defaults to live jobs); expand
cash-in/out categories to per-project lines; inline edits: monthly company
overheads default, per-month overrides, and each project's next expected
valuation date + expected monthly valuation. Position to Completion statement
below; Excel export. Phased months tie to each project's Cashflow tab to the
penny.

### Weekly Cashflow — `/finance/weekly-cashflow` (directors + Accounts)
The accountant's live 13-week payment plan, one column per week: every
outstanding Xero bill and sales invoice seeded at its due week (or its Xero
Planned / Expected date), plus manual items Xero can't see (subcontractors,
staff, subscriptions, direct debits, other). Overdue sits in the current week;
Later is beyond the horizon. Directors' tiles: cash in bank, to pay this week,
lowest week, horizon-end balance; Accounts sees to pay this week plus cash
out / cash in over the 13 weeks (the bank position is directors only). Work it with ‹ › on a
cell to move an entry to the week it will really be paid (↺ returns it, ‣
marks a moved entry — moves change WHEN, never HOW MUCH, and are shared);
**Group suppliers** folds chosen suppliers into one line (a group row is one
line; its ‹ › move every bill in that cell); ⊘ parks an entry already covered
elsewhere, struck through and uncounted; **Add item** adds manual outgoings.
Excel export is the grid on paper: **Weekly plan** (one line per supplier,
groups honoured, a column per week, band totals, net movement, directors'
closing balance), **Detail** (every bill under its line with due/expected
dates), **Data** (flat list for pivoting); a shaded amount was moved. Real
payment agreements belong in Xero (bill Planned date / invoice Expected date);
ageing lives on Aged Payables/Receivables. Over the connector:
`get_weekly_cashflow_grid` returns this grid line by line (includeEntries for
the bills behind a line), `get_weekly_cashflow_plan` the raw overlay, and the
`*_weekly_cashflow_*` actions the writes.

### Profit Summary — `/finance/profit-summary`
Gross profit by project three ways: running % by month end (Xero site P&L,
invoiced basis), summary tiles (budgeted, forecast, biggest swing vs the
deal), budget→forecast bridge, and the banded table (deal as signed, current
position — certified vs allocated Xero spend, to finish, forecast at
completion). Refresh re-pulls the stored Xero site P&L. Certified-basis table
and invoiced-basis panels deliberately differ.

## Xero folder

### Xero Transactions — `/finance/xero`
Purchase invoices read live from Xero with site and cost code per line. Views:
Transactions (search, status chips, expandable lines) and Site × cost code
(net-spend pivot per calendar year on the accountant's basis: paid, authorised
or awaiting approval, minus credit notes; drafts excluded). Refresh from Xero,
Excel export; banner when the fetch cap truncates. Coding happens on
`/finance/allocation`.

### Aged Receivables / Aged Payables — `/finance/aged-receivables`, `/finance/aged-payables`
Aged exactly as Xero ages them but **including drafts** — the only complete
picture, since the accounting procedure leaves bills in draft until coded
through the portal. Tiles (total, draft slice, overdue); one row per
client/supplier expanding to invoices/bills with Draft and Credit badges;
ageing toggle due date ↔ invoice date; Refresh, Excel export. Read-only.

## Audit folder

- **Reconciliation Audit** — `/projects/{project}/reconciliation-audit`: every
  cost-centre recode of a valuation report line, newest first — who, which
  line, from/to, value; the record finance reconciles against. Read-only.
- **System Audit Trail** — `/audit`: append-only register of who routed,
  linked and filed what; pathway tabs, event-type and project filters; "Open
  in Outlook" links to drafted emails.
- **Agent Activity** — `/agents/activity`: every assistant run — when, agent,
  who it ran as ("unattended" = nobody watching), action, outcome, tools,
  duration, tokens, cost; totals. Distinct from `/agents` (the queue of
  requests being watched by applied discipline agents — applied FROM a request
  page) and `/admin/agents` (the registry).

## Admin folder

- **Users** — `/admin/users` (+ `/admin/users/revoked`): invites, role
  assignments; revoke → restore or permanent delete from the revoked list.
- **System** — `/admin/system`: "Publish update" raises the refresh bar on
  every signed-in tab after a deploy; Tender T&Cs panel holds the single
  standard T&C PDF auto-attached to every tender-invite email.
- **Trades** — `/admin/trades`: the curated trade list every directory record
  and bid package picks from (delete blocked while in use).
- **AI Agents** — `/admin/agents`: the live agent registry (read-only; config
  is code). **AI Skills** — `/admin/skills`: versioned markdown skills, in
  force on the assistant's next message, no deploy; each save is a new version.

## Off-rail pages

`/rfis` (company-wide read-only RFI register, overdue day-counts in red).
`/dashboard` (alias `/my-day`) is the role-aware home. The CRM front end
(`/estimating-queue`, `/nurture`, `/sales-analytics`) was removed on
2026-09-03 — the leads API and the `list_leads` tool remain, with no page.
