# JPMS navigation map — every view reachable in the portal

Read from `jpms/Services/Navigation/SidebarFolders.cs`, `DesktopNavigation.cs`,
`WorkspaceSections.cs` and every `@page` directive under `jpms/Pages`, on 2026-09-03
(after the CRM orphan removal): **89 page components, 99 routes, 63 rail destinations.**

## Shape of the rail

`Home` → project picker → **11 collapsible folders** (60 rows) → **4 folderless rows** at the foot
→ signed-in identity + Sign out.

Folder order: Project · Subcontractor · Supplier · Internal · Time · Finance · Financial Reports ·
Xero · Audit · Sales · Admin. Foot: Control Centre · Document Triage · Xero Cost Allocation ·
Valuation Reports.

**Nav clamp (decision 2026-08-11).** Every row is gated to Managing Director and Finance Director;
administrators bypass every gate. Weekly Cashflow adds Accounts — the one deliberate exception.
The Admin folder's first four rows are administrators only. Every other role sees Home alone.
API authorisation is untouched by the clamp — those pages stay reachable by URL.

Rows marked **{project}** resolve their template against the picker; the rest are company-wide.

## Above the folders

| View | Route | Notes |
|---|---|---|
| Home | `/dashboard` | AdminHome for administrators, RoleHome otherwise; My Day workspace inline for Site Manager / Foreman / Site Operative. Tiles link to `/rfis`, `/projects`, triage backlog. |
| Project picker | — | Recently opened, full list, "Show completed" toggle, "View all projects" → `/projects`. Only rendered when the role's rail has a project-scoped row. |
| Sign out | `/logout` | Foot of the rail. |

## 01 · Project  {project}

| Row | Route | Reached from it |
|---|---|---|
| RFIs | `/projects/{id}/requests` | tabs `/all` `/general` `/rfis`; detail `/requests/view/{requestId}` |
| Variation Orders | `/projects/{id}/variations` | detail `/variations/{voId}`; legacy `/requests/variations`, `/voq/{id}` |
| Architect's Instructions | `/projects/{id}/architect-instructions` | |
| Valuation Report Snapshots | `/projects/{id}/valuation-snapshots` | |
| Documents (was Drawings until 2026-09-03; `/drawings` URLs redirect) | `/projects/{id}/documents` | detail `/documents/{drawingId}`; ambiguous `/documents/ambiguous` |
| Programme | `/projects/{id}/programme` | |
| Calendar | `/projects/{id}/calendar` | |
| To-do | `/projects/{id}/todos` | |
| Progress | `/projects/{id}/progress` | |
| Defects | `/projects/{id}/defects` | |
| Inventory | `/projects/{id}/inventory` | |
| Site Instructions | `/projects/{id}/site-instructions` | SI-#### — written instructions to site; raised here or from the Control Centre Internal pane (2026-09-03) |
| Building Control | `/projects/{id}/building-control` | inspection `/building-control/inspections/{id}` |
| Communications | `/projects/{id}/communications` | |
| Useful Information | `/projects/{id}/useful-information` | |
| Project Settings | `/projects/{id}/settings` | absorbs `/setup`, `/financials-setup`, `/operations-setup` (all redirect) |

## 02 · Subcontractor

Bid Package Invites `/projects/{id}/bid-package-invites` (detail `/{bidPackageId}`) ·
Work Orders `/projects/{id}/work-orders` (printable PO `/work-orders/{woId}/po`) ·
Communications `/subcontractors/communications` · Chasers `/chaser` ·
Info Requests `/info-request` · H&S `/h-s`

## 03 · Supplier

Communications `/suppliers/communications` · Materials `/materials` · Finishes `/finishes`

## 04 · Internal

Todo `/todos` (detail `/todos/{id}`) · Tender Enquiries `/tender-enquiries` (detail `/{id}`) ·
Directory `/directory` (detail `/directory/{id}`) · Communications `/internal/communications` ·
Registers `/registers` · Policies `/policies` (Site Instructions moved to the Project folder 2026-09-03)

## 05 · Time

Labour overview `/labour/overview` · Labour `/projects/{id}/labour` ·
Workers `/labour/workers` · Xero mapping `/labour/xero-mapping`

## 06 · Finance

Financials `/projects/{id}/financials` · WO Allocation `/projects/{id}/work-order-allocation` ·
Payment Certificates `/finance/payment-certificates` · Cost Codes `/cost-codes` ·
Rates `/rate-library` (stale `/rate-library/stale`)

Cost Codes and Rates are sibling tabs of the **Setup** workspace section.

## 07 · Financial Reports

Project Cashflow `/projects/{id}/cashflow` ·
Cash Forecast `/finance/cash-forecast` (aliases `/finance`, `/finance/cash-summary`) ·
Weekly Cashflow `/finance/weekly-cashflow` (**+ Accounts**) · Profit Summary `/finance/profit-summary`

## 08 · Xero

Xero Transactions `/finance/xero` · Aged Receivables `/finance/aged-receivables` ·
Aged Payables `/finance/aged-payables`

Transactions and Allocation are sibling tabs of the **Xero** workspace section.

## 09 · Audit

Reconciliation Audit `/projects/{id}/reconciliation-audit` · System Audit Trail `/audit` ·
Agent Activity `/agents/activity` · AI Connections `/settings/ai-connections`
(any signed-in user can open that last one directly; only the nav row is clamped)

## 09a · Sales (2026-09-06)

Leads `/sales/leads` (lead page `/sales/leads/{id}`) · Strategies `/sales/strategies`
(strategy page `/sales/strategies/{id}`)

The lead register and the strategies — methodologies for finding leads, each with its
justification, a Claude-drafted approach plan and the funnel its leads make — that feed it.
Leads and Strategies are sibling tabs of the **Sales** workspace section. Between Audit and
Admin by James's call. Won on a lead creates the client account and the project shell.

## 10 · Admin

Users `/admin/users` (Revoked `/admin/users/revoked` — sibling tab, no rail row) ·
System `/admin/system` · Integrations `/admin/integrations` · Trades `/admin/trades` ·
KPI emails `/admin/kpis` ·
AI Skills `/admin/skills` · AI Actions `/admin/ai-actions`

First five are administrators only; the three AI rows are directors too. The admin homepage
(`/dashboard` as Administrator) rolls the same destinations into one **Actions** dropdown.

## Foot of the rail

| Row | Route | Notes |
|---|---|---|
| Control Centre | `/control-centre` | mailbox intake and router, all projects. Legacy `/requests/triage`. |
| Document Triage | `/document-triage` | attachment queue the Control Centre feeds. Legacy `/document-control`. |
| Xero Cost Allocation | `/finance/allocation` | standing work queue, moved out of the Xero folder 2026-08-14. |
| Valuation Reports {project} | `/projects/{id}/valuation` | the flagship output, elevated out of its folder. |

## Reachable, but not from the rail

`/projects` (picker link + Home tiles) · `/projects/{id}` (redirects to first project tab) ·
`/rfis` (Home tiles, Open Requests panel) · `/clients` and `/architects` (linked from the merged
Directory's tables) · `/my-day` (redirects to `/dashboard`)

## Outside the rail

Subcontractor portal `/portal`, `/portal/work-orders/{id}` ·
Client portal `/client`, `/client/requests/{id}`, `/client/variations/{id}` ·
Auth `/`, `/login`, `/logout`, `/forgot-password`, `/set-password` ·
Connector consent `/connect/authorize`

## Removed 2026-09-03

The CRM front end — `/estimating-queue`, `/nurture`, `/sales-analytics` — three routed pages with
no rail row and no inbound link anywhere. Deleted with their plumbing (`LeadsTable`,
`LeadsBySourceTable`, `LeadStageBadge`, `ILeadStore`/`HttpLeadStore`, `jpms/Features/Leads`, the DI
and route registrations, three AI page guides). **Kept:** the Leads API (`api/Features/Leads`,
~20 endpoints) and the `list_leads` connector tool. The pipeline has no front door now — a decision
to revisit, not an accident.
