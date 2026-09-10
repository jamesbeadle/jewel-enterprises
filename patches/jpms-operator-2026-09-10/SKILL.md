---
name: jpms-operator
description: >-
  Operator's guide to JPMS, the Jewel Bespoke Build project-management portal
  (the "site" / "the portal"). Use whenever the user asks how to do something in
  JPMS — raise, tag, approve, issue, chase, file, allocate, invoice — where a
  page or record lives, what a status or reference (V72, RFI-049, REQ-0113,
  DEF-0012) means, how the Control Centre / triage / valuation / variation /
  bid-package / work-order / Xero workflows run, or when answering questions
  about portal data. Covers every page of the site, every record lifecycle, the
  Jewel_Portal MCP connector (how Claude reads and acts on the portal — there
  is no chat inside the site), and how to query the system's data directly.
---

# JPMS operator guide

JPMS is the project management system for Jewel Bespoke Build, a super-prime
residential contractor. It runs each project's full record chain — correspondence
intake, RFIs, variations, bid packages, work orders, labour, drawings, defects —
and produces the money outputs: the live Valuation Report per project (the
flagship), the cash forecast, and the profit summary, reconciled against Xero.
Today only the directors (MD, FD) and administrators use it; every other role
sees Home alone until its nav is designed. Front-end is Blazor WASM (`/jpms`),
API is Azure Functions (`/api`), shared contracts in `/contracts`, on Azure SQL.

## House language — never deviate

- **Programme**, never "schedule"/"program", for the plan of work.
- **Valuation invoice**, never "cash call", "payment application" or "client invoice".
- **Variation** is ONE document with one number through every stage; users read
  it as **V72**. Never say "VOQ" or "VO" to a user — those survive only in
  stored identifiers (`VariationOrderQuotes` table, `VOQ-0072` references,
  `/voq/` legacy routes, `JPMS/VOQ-…` mail tags).
- Record lineage: **Request → RFI → Variation** (three stages). Bid packages are
  separate records, not part of the chain (split 2026-08-12).
- **"AI"** on a variation means **Architect's Instruction**, not artificial
  intelligence. "Awaiting AI" = waiting for the architect's written authority.
- Plain UK English. Lead with the commercial position, then the reasoning.
- Never state a figure, date, status or reference you haven't read from the
  code, the database or a page — a plausible wrong reference ends up in a
  client email.

## The site at a glance

One sidebar, project picker at the top; `{project}`-templated rows follow the
picked project. Folders (in rail order):

| Folder | Rows |
|---|---|
| **Project** | RFIs (`/projects/{p}/requests`) · Variation Orders (`…/variations`) · Architect's Instructions · Valuation Report Snapshots · Drawings · Programme · To-do · Progress · Defects · Communications · Useful Information · Project Settings |
| **Subcontractor** | Bid Package Invites · Work Orders · Communications (`/subcontractors/communications`) |
| **Internal** | Todo (`/todos`) · Directory · Registers · Policies |
| **Time** | Labour overview · Labour (per project) · Workers · Xero mapping |
| **Finance** | Financials (`…/financials`) · WO Allocation · Payment Certificates · Cost Codes · Rates |
| **Financial Reports** | Project Cashflow · Cash Forecast (`/finance/cash-forecast`) · Weekly Cashflow (`/finance/weekly-cashflow`, directors + Accounts) · Profit Summary |
| **Xero** | Xero Transactions · Aged Receivables · Aged Payables |
| **Audit** | Reconciliation Audit · System Audit Trail (`/audit`) · Agent Activity |
| **Admin** | Users · System · Trades · AI Agents · AI Skills |

Standing work queues pinned at the foot of the rail (whole-company, work that
NEEDS doing): **Control Centre** (`/control-centre` — mailbox intake and router
for ALL correspondence), **Document Triage** (`/document-triage` — attachment
filing queue), **Xero Cost Allocation** (`/finance/allocation`), and the picked
project's live **Valuation Report** (`/projects/{p}/valuation`).

`/projects/{p}` bare is a redirect to the role's first tab — always link a
specific tab. Full page-by-page detail: `references/site-map.md`.

## Getting things done — task → where

| The job | Where it's done |
|---|---|
| Triage an email (project, tags, to-dos, reply) | Control Centre — everything staged against the selected email, one **Apply** lands it all |
| File an attachment (drawing / payment cert / sub document) | Tick "Send to document triage" in Control Centre → file it at `/document-triage` |
| Raise an RFI | Usually from an email in the Control Centre; manual "Raise RFI" on the project's RFI register |
| Work / respond / close an RFI | Request detail page (`…/requests/view/{id}`) — status pill, Email button (Outlook draft, PDF attached), Actions menu |
| Draft a variation from an RFI | Request detail page → Create Variation Order Quote (`variation_draft` dialog) |
| Add a standalone variation | Variation Orders page → "Add variation manually" |
| Move a variation through its ladder | Status chip on the register for free stages; **Approve on the variation's own page** (builds priced lines per cost centre, writes to Valuation Report, CVR, budgets) |
| Record the architect's instruction | Architect's Instructions register — file it, tick the variations it covers |
| Run the monthly claim | Valuation Report claim card stepper: Value & lock → Claim → Approve → Invoice → Paid → Confirm & roll over |
| See what the client was sent | Valuation Report Snapshots (frozen; the live report is internal-only) |
| File an email about the month's valuation (site-meeting notes, the QS's working, the architect's early queries) | Control Centre → Client → **Valuation claims** — the live claim. Its mail reads back in the Valuation Report's Correspondence section and on every snapshot frozen from it; the client's reply to a sent statement can go on the snapshot instead. Confirm & roll over starts the next claim with its own tag |
| Tender a trade | Bid Package Invites → package detail: Details (lines) → Tender list (invite email, BCC) → Submissions → **Award** (raises the work order) |
| Raise / approve a work order | Work Orders tab — "Add work order", two-click Approve (mints WO number, emails the PO) |
| Pay-side reconciliation | Xero Cost Allocation (code purchase lines to project + cost centre; allocating a draft bill fully approves it in Xero) then WO Allocation (tie lines to work orders) |
| Approve the week's labour | Project Labour tab — tick and bulk-approve; only approved time posts to Financials |
| Raise / progress a defect | Defects register (DEF-#### = its mailbox tag) or from a sub's email in Control Centre |
| Delay events, NOD / EOT / LADs | Programme tab → Claims sub-tab |
| Chase money answers | Aged Receivables / Aged Payables (include drafts Xero's own reports can't see); Cash Forecast for the months ahead; Profit Summary for margin |
| Plan the next 13 weeks' payments — who gets paid which week | Weekly Cashflow (`/finance/weekly-cashflow`): move entries with ‹ ›, group suppliers into one line, park with ⊘, add manual items; the Excel export is the grid one line per supplier. Over the connector, `get_weekly_cashflow_grid` reads the same grid |

Lifecycles, statuses and reference formats (REQ-0001, V72, DEF-####, mail tags,
claim stepper): `references/lifecycles.md`.

## Sending rules — get these right

Nothing in JPMS silently sends on your behalf, but the pages differ and it
matters commercially:

- **Outlook-draft only** (a human sends from Outlook): RFI "Prepare email
  drafts", request-detail Email button, PO covering email, valuation snapshot
  Email button, statement-of-account draft.
- **Sends from the projects mailbox on the user's press**: Control Centre
  Apply (staged replies/forwards), Communications-page Reply/Forward,
  Subcontractor Communications Reply/Forward, bid-package Invite composer,
  work-order Approve (sends the PO).
- The connector (Claude acting through the Jewel_Portal MCP server) sends only
  through confirm-first actions — the mailbox reply/forward, the PO email, the
  tender invite — and only after the user's explicit yes in that conversation.
  It never emails from Outlook.

## The assistant — the Jewel_Portal MCP connector

The site has **no chat of its own**: the in-portal assistant was retired on
2026-08-27 and removed. "The assistant" is Claude — desktop, Cowork or claude.ai
— talking to the portal through the Jewel_Portal MCP server (`api/Features/Mcp`,
tools in `api/Features/Ai/Tools`). It acts as the signed-in portal user: it sees
what they see, does what their buttons do, and every call is logged under their
name at `/agents/activity`. Reads mirror the pages (a feature is not done until
the connector can read it — repo `CLAUDE.md`); writes are `perform_action`
actions mirroring the buttons, confirm-first for anything financial, external or
irreversible. Team doctrine lives as skills at `/admin/skills` (`list_skills`,
`load_skill`, `save_skill` — live from the next conversation). Claude renders
the answer (tables, dashboards); never propose a chat widget or page-side AI.
See `references/assistant-and-data.md` for the tool catalogue.

## Answering from data (Claude Code)

- Page behaviour: the authoritative per-page prose lives in code at
  `contracts/Ai/PageGuides/*.cs` — same facts as `references/site-map.md`.
- Nav truth: `jpms/Services/Navigation/SidebarFolders.cs`; route list:
  `@page` directives in `jpms/Pages/`.
- Status enums: `contracts/Models/` (Request, VariationOrder,
  ValuationInvoice, ProjectStage…). API + role gates: `docs/cqrs/06-api-surface.md`.
- Live figures: query prod Azure SQL read-only via `sqlcmd` (server, user and
  the mandatory scoped-migration rules are in the repo `CLAUDE.md` — never run
  the full idempotent EF script against prod).
- Repo-wide engineering conventions (loading states, toolbars, migrations)
  are in `CLAUDE.md` and override anything here for code changes.

## References

- `references/site-map.md` — every page: what it is, what's done there, what's
  deliberately done elsewhere. Load before walking anyone through a page.
- `references/lifecycles.md` — record lifecycles, statuses, numbering, mail
  tags, the claim stepper. Load before explaining or changing any status.
- `references/assistant-and-data.md` — the MCP connector (tool catalogue,
  rules) and direct data access (API surface, DB, key source files).
