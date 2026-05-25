# JPMS — Consolidated Site Map (Phase 1)

This is the contract between scoping and Blazor implementation. Every Phase 1 user story has a route. Every route serves a stated set of stories. The 14 P2 stories listed in `06-backlog/phase-1-vs-phase-2-proposal.md` are deliberately omitted. The seven `US-NEW-*` Phase-1 stories from the coverage audit are included.

**Phase 1 scope counts:** 162 retained originals + 7 `US-NEW-*` = 169 stories. **Excluded from this site map:** US-01-01, US-01-09, US-01-10, US-01-11, US-01-12, US-02-01, US-02-02, US-02-03, US-02-04, US-02-06, US-03-16, US-03-17, US-07-41, US-07-43.

**Conventions:**
- Owning role(s) listed first in bold; read-only roles after.
- `US-NN-MM` IDs are the stories the route serves. Grouped where a single screen serves many.
- External-role routes live under `/portal/*` and are listed separately.
- Shared sub-components called out where they appear on three or more routes.

---

## 1. Complete site map

### 1.1 Root / auth / cross-cutting

```
/                                                  [Public — Microsoft sign-in]
/dashboard                                         [P01-P11 — role-aware landing]
/access-request                                    [Signed-in but not in directory]
/admin                                             [Admin only — user directory + role mgmt]
  /admin/users                                       Approved users table
  /admin/requests                                    Pending access requests
  /admin/roles                                       Role assignment (per directory user)
/me                                                [Any signed-in user]
  /me/preferences                                    Email + role-switch defaults
                                                   (Notifications delivered via PWA push, no dedicated inbox)
/reports                                           [Owner P02; Read P01,P03,P04]
                                                   US-07-10, US-07-22, plus any downstream-publish surface
  /reports/valuations                                Approved valuations CSV export
  /reports/timesheets                                Approved day-rate hours CSV export
                                                   (Direct Xero integration is a later phase)
```

### 1.2 Workflow 00 — CRM / Sales / Pre-project

```
/leads                                             [Owner P03; Read P01,P02,P04]
                                                   US-00-01,02,05,11,12,14
  /leads/new                                         US-00-01 (manual capture)
  /leads/{id}                                        Lead detail hub
    /leads/{id}/qualification                        US-00-02 (scorecard)
    /leads/{id}/site-visits                          US-00-03,04 (booking + mobile capture)
    /leads/{id}/info-chase                           US-00-05 (drawings/consents chase)
    /leads/{id}/bid-decision                         US-00-06 (bid/no-bid)
    /leads/{id}/proposal                             US-00-08,09 (issue + negotiation)
    /leads/{id}/outcome                              US-00-10,11 (Won → project-shell; Lost reason)

/estimating-queue                                  [Owner P04; Read P01,P03]
                                                   US-00-07 (prioritised queue with deadline)

/nurture                                           [Owner P03; Read P01]
                                                   US-00-12 (Lost → Nurture follow-up reminders)

/sales-analytics                                   [Owner P01; Read P02,P03,P04]
                                                   US-00-14 (source / ROI attribution)
```

### 1.3 Workflow 01 — Drawings & Document Control

Drawings are a project sub-area, not a top-level entity. There is no global `/drawings` page in Phase 1.

```
/projects/{id}/drawings                            [Owner P03; Read P04,P05,P06,P11]
                                                   US-01-02,03,04,05,07,08, US-NEW-01, US-NEW-04
  /projects/{id}/drawings/upload                     US-NEW-01 (manual PDF upload)
  /projects/{id}/drawings/{drawingId}                Drawing detail — revision list + viewer
    /projects/{id}/drawings/{drawingId}/audit        US-01-08 (viewer audit trail)
    /projects/{id}/drawings/{drawingId}/issue        US-NEW-04 (architect issue source)
  /projects/{id}/drawings/ambiguous                  US-01-04 (queue of unresolved revisions)
```

US-01-06 (current revision on phone) is delivered by the mobile site app, not a separate route.

### 1.4 Workflow 02 — Tender, BoQ, Rates

```
/projects/{id}/boq                                 [Owner P04; Read P01,P03,P05]
                                                   US-02-05,07,08,09,10,11; US-NEW-02,03,06
  /projects/{id}/boq/new-line                        US-NEW-02 (direct line entry)
  /projects/{id}/boq/import                          US-NEW-03 (CSV/Excel bulk import)
  /projects/{id}/boq/compare                         US-02-08 (re-tender last-priced vs current)
  /projects/{id}/boq/walk-round                      US-02-09 (mobile walk-round)
  /projects/{id}/boq/sign-off                        US-02-11 (Director sign-off)

/rate-library                                      [Owner P04; Read P01,P03]
                                                   US-02-05, US-NEW-06
  /rate-library/{rateId}                             Rate detail + version history
  /rate-library/stale                                US-NEW-06 (rates not priced in N days)
```

### 1.5 Workflow 03 — Procurement & Subcontractor Onboarding

```
/projects/{id}/procurement                         [Owner P03; Read P01,P02,P04,P07]
                                                   US-03-01..03, 08, 10, 11, 12, 21
  /projects/{id}/procurement/new-bid                 US-03-01,02 (bid package builder)
  /projects/{id}/procurement/{packageId}             Bid package detail
    /projects/{id}/procurement/{packageId}/invite    US-03-03 (subcontractor invite)
    /projects/{id}/procurement/{packageId}/compare   US-03-08 (side-by-side comparison)
    /projects/{id}/procurement/{packageId}/award     US-03-10,11,21 (award + gate + sign-off)
  /projects/{id}/procurement/history                 US-03-12 (tender history)

/subcontractors                                    [Owner P07; Read P01,P02,P03,P06]
                                                   US-03-13,14,18,19,20,22
  /subcontractors/{id}                               Subcontractor master record
    /subcontractors/{id}/compliance                  US-03-13,14,18,22
    /subcontractors/{id}/rams                        US-03-19,20
    /subcontractors/{id}/projects                    Read — which projects this sub is on

/work-orders                                       [Owner P03; Read P01,P02,P04,P07; FD portfolio view]
                                                   US-03-10 (auto-generated post-award)
  /work-orders                                       Portfolio-wide WO list with filter (status, value, sub, org)
  /work-orders/{id}                                  Work Order artefact
```

### 1.6 Workflow 04 — H&S Mobilisation & Compliance Engine

```
/hs                                                [Owner P06; Read P01]
                                                   US-04-15 (portfolio H&S view)
  /hs/templates                                      US-04-01 (templates library)
  /hs/inspections                                    US-04-05 (scheduled — portfolio)
  /hs/audits                                         US-04-06 (formal audits)
  /hs/incidents                                      US-04-08 (incident register)
  /hs/observations                                   US-04-07 (observation queue)
  /hs/corrective-actions                             US-04-09,16
  /hs/toolbox-talks                                  US-04-11, US-NEW-05
  /hs/temporary-works                                US-04-13

/projects/{id}/mobilisation                        [Co-owners P05,P06; Approver P03]
                                                   US-04-02,03,04
  /projects/{id}/mobilisation/checklist              US-04-02 (per-project)
  /projects/{id}/mobilisation/gate                   US-04-04 (hard block)

/projects/{id}/hs                                  [P06 owner; P05 co-owner; P03 read]
                                                   US-04-07..14, US-NEW-05
  /projects/{id}/hs/inspections                      US-04-05
  /projects/{id}/hs/observations                     US-04-07
  /projects/{id}/hs/incidents                        US-04-08
  /projects/{id}/hs/corrective-actions               US-04-09,16
  /projects/{id}/hs/toolbox-talks                    US-04-11 + US-NEW-05
  /projects/{id}/hs/permits                          US-04-12
  /projects/{id}/hs/golden-thread                    US-04-14
```

### 1.7 Workflow 05 — RFIs, Submittals, Variations, Delays

```
/projects/{id}/changes                             [Owner P03; Approver P08,P09; Read P01,P02,P04]
                                                   US-05-01
  /projects/{id}/changes/new                         US-05-01 (raise + classify)
  /projects/{id}/changes/rfis                        US-05-05,08 (RFI queue + auto-chase)
    /projects/{id}/changes/rfis/{id}                 RFI detail
  /projects/{id}/changes/variations                  US-05-02,03,04,07,10,11,12
    /projects/{id}/changes/variations/{id}           Variation detail
    /projects/{id}/changes/variations/report         US-05-11 (VO list)
  /projects/{id}/changes/delays                      US-05-09 (NoD)
  /projects/{id}/changes/submittals                  Submittal approvals (cross-cutting)
```

US-05-06 (architect responds) lives in Architect portal. US-05-12 is server logic.

### 1.8 Workflow 06 — Site Delivery, Programme & Reporting

```
/projects/{id}/site                                [Owner P05; Read P01,P02,P03,P04]
                                                   US-06-09,10,12
  /projects/{id}/site/dashboard                      Live project dashboard
  /projects/{id}/site/reports                        US-06-09 (auto-assembled)
    /projects/{id}/site/reports/{id}                 US-06-10 (narrative + approve)
  /projects/{id}/site/snags                          Snag log
  /projects/{id}/site/photos                         US-06-12 (re-tag photos)
  /projects/{id}/site/attendance                     US-06-02,03

/projects/{id}/programme                           [Owner P03; Read P04,P05]
                                                   US-07-01 (Gantt tied to BoQ)

/site                                              [Mobile-first PWA — P05,P11; Subcontractor uses /portal]
                                                   US-01-06, US-06-01,04,05,06,07,08,
                                                   US-04-03,07,11, US-02-09, US-08-02, US-07-12,14,16
  /site/today                                        US-06-01 (today's projects)
  /site/projects/{id}                                Active-project mobile home
    /site/projects/{id}/capture-progress             US-06-04 (% slider)
    /site/projects/{id}/capture-photo                US-06-05,06
    /site/projects/{id}/raise-snag                   US-06-07 (two-tap snag)
    /site/projects/{id}/drawings                     US-01-06
    /site/projects/{id}/walk-round                   US-02-09
    /site/projects/{id}/mob-checklist-confirm        US-04-03
    /site/projects/{id}/observation                  US-04-07
    /site/projects/{id}/toolbox-talk                 US-04-11
    /site/projects/{id}/snag                         US-08-02
    /site/projects/{id}/timesheet                    US-07-12,14,16
  /site/sync                                         US-06-08 (offline queue)
```

### 1.9 Workflow 07 — Valuations, CVR, Cashflow

```
/projects/{id}/commercial                          [Owner P04; Approver P03,P01; Read P02]
  /projects/{id}/commercial/pvr                      Programme Valuation Report
    /projects/{id}/commercial/pvr/draft              US-07-03,04,05
    /projects/{id}/commercial/pvr/{periodId}         US-07-06,08,09
    /projects/{id}/commercial/pvr/setup              US-07-02 (Claim Period)
  /projects/{id}/commercial/cvr                      CVR — package grid + Movement column
                                                     US-07-23,26,27
    /projects/{id}/commercial/cvr/accruals           US-07-24 (QS Accruals)
    /projects/{id}/commercial/cvr/prelims            US-07-28,30 (week × item grid)
    /projects/{id}/commercial/cvr/eots               US-07-31 (EOT register)
    /projects/{id}/commercial/cvr/packages           US-07-32,33,34
    /projects/{id}/commercial/cvr/{snapshotId}       US-07-27 (historic)
  /projects/{id}/commercial/programme-header         US-07-29 (Weeks Ahead/Behind)
  /projects/{id}/commercial/cost-codes               Cost-code register + budgets
    /projects/{id}/commercial/cost-codes/overruns    US-07-20,21
  /projects/{id}/commercial/timesheets               US-07-13,17,18,22

/cashflow                                          [Owner P02; Read P01,P03]
                                                   US-07-36,37,38,39,40,42,44,46
  /cashflow/dashboard                                US-07-36,37 (13-week rolling)
  /cashflow/attention                                US-07-39 (items needing attention)
  /cashflow/director                                 US-07-44
  /cashflow/pm                                       US-07-46
  /cashflow/snapshots                                US-07-42

/portfolio-cvr                                     [Owner P04; Read P01,P02,P03]
                                                   US-07-35 (portfolio margin)
```

### 1.10 Workflow 08 — Close-Out, Defects, Aftercare

```
/projects/{id}/closeout                            [Owner P03 commercial; P05 snag; P02 retention/VAT]
                                                   US-08-01,03,05,06,12
  /projects/{id}/closeout/defects                    Defect register
    /projects/{id}/closeout/defects/{id}             US-08-05 (sign-off)
  /projects/{id}/closeout/settlement                 [Owner P02]
                                                   US-08-09,10,11,18,20
    /projects/{id}/closeout/settlement/open-items    US-08-10,11
  /projects/{id}/closeout/vat                        [Owner P02; Approver P01,P08,P09]
                                                   US-08-13,14,16,17
  /projects/{id}/closeout/retention                  US-08-07,19
  /projects/{id}/closeout/pack                       US-08-06
```

US-08-08, US-08-15, US-08-04 live in portals.

### 1.11 Workflow 09 — Portfolio Reporting & Analytics

```
/portfolio                                         [Owner P02; Audience P01; Read P03,P04,P06]
                                                   US-09-01,11
  /portfolio/project-health                          US-09-02
  /portfolio/commercial                              US-09-03,06
  /portfolio/subcontractor-exposure                  US-09-04
  /portfolio/hs                                      US-09-05
  /portfolio/pipeline                                US-09-07,08
  /portfolio/estimator                               US-09-09
  /portfolio/thresholds                              US-09-10
  /portfolio/snapshots                               US-09-12
  /portfolio/alerts                                  Exception alert inbox
```

### 1.12 Project hub — the connecting tissue

```
/projects                                          [Owner P03; Read P01,P02,P04,P05,P06,P07]
                                                   List + filter + create-new
  /projects/new                                       From /leads/{id}/outcome → Won (US-00-10)
  /projects/{id}                                      Project home — KPI strip + tabs
    /projects/{id}/team                                Internal team + subs
    /projects/{id}/contract-setup                      US-07-02 (Claim Period, retention %, dates)
    /projects/{id}/client-instructions                 Client portal mirror
    /projects/{id}/correspondence                      Correspondence + Instruction log
```

---

## 2. External-role portals

External users (P08, P09, P10) authenticate via secure link (magic link) or full SSO. They never see desktop `/projects/...` routes.

### 2.1 Subcontractor portal — P10

```
/portal/subcontractor
  /portal/subcontractor/home                         Today + open items
  /portal/subcontractor/bids                         Open invitations
    /portal/subcontractor/bids/{packageId}           Read scope — US-03-04
    /portal/subcontractor/bids/{packageId}/quote     US-03-05,06
    /portal/subcontractor/bids/{packageId}/decline   US-03-07
  /portal/subcontractor/compliance                   Status home — US-03-15
    /portal/subcontractor/compliance/upload          Renewed docs — P1 version of US-03-16
    /portal/subcontractor/compliance/cis             Manual CIS — P1 version of US-03-17
  /portal/subcontractor/onboarding                   RAMS / induction / permits — US-04-10
  /portal/subcontractor/projects
    /projects/{id}/drawings                          US-01-07 (assigned @ current rev)
    /projects/{id}/rfis                              US-05-05 (raise from site)
    /projects/{id}/check-in                          US-06-02 (QR)
    /projects/{id}/timesheets                        US-07-13
    /projects/{id}/defects                           US-08-04
```

### 2.2 Architect portal — P08

```
/portal/architect
  /portal/architect/home                             Open RFIs across projects — US-05-06, journey 05a
  /portal/architect/leads/new                        Architect-introduced lead — US-00-13
  /portal/architect/projects/{id}
    /dashboard                                       Live progress — US-06-11
    /rfis/{id}                                       Reply + "implies VO" — US-05-06,07
    /submittals                                      Submittal approvals
    /variations                                      Variation approvals
    /pvr                                             Receive PVR — US-07-07
    /cashflow                                        Project cashflow slice — US-07-45
    /cost-codes                                      Approved timesheet totals — US-07-19
    /closeout-pack                                   US-08-08
    /vat                                             US-08-15
```

### 2.3 Client portal — P09

```
/portal/client
  /portal/client/home                                "Your project, in plain English"
  /portal/client/project/{id}
    /dashboard                                       US-06-11, US-NEW-07
    /selections                                      Selection/decision approvals
    /variations                                      Client-side approval — US-05-10
    /pvr                                             US-07-07
    /cashflow                                        US-07-45
    /cost-codes                                      US-07-19
    /practical-completion                            PC sign-off
    /defects                                         US-08-04 (raise side)
    /closeout-pack                                   US-08-08
    /vat                                             US-08-15
```

---

## 3. Shared sub-components

| Component | Used on | Why shared |
|---|---|---|
| `CostCodeChip` | BoQ, CVR, variations, cost-codes, timesheets, work-orders | Cost code threads through every commercial screen |
| `BoqLineRef` | Changes, mobile progress, photos, defects, RFIs | Universal commercial-tracking link |
| `DrawingRevBadge` | Drawings, mobile, RFIs, procurement, architect portal | Current revision shown wherever drawing referenced |
| `RoleBadge` | Admin, project team, dashboard, approvals | Already exists |
| `ComplianceStatusPill` | Subs directory, sub compliance, award, sub portal | Status drives gating logic |
| `ApprovalRibbon` | PVR, BoQ sign-off, VO, WO, retention, VAT | One consistent approval surface |
| `MovementCell` | CVR landing, packages, snapshots, portfolio-cvr | Required by US-07-26 |
| `ClaimPeriodPicker` | PVR, CVR snapshots, cashflow snapshots, portfolio | Unifying time axis |
| `ProjectKpiStrip` | Project home, site dashboard, commercial landings | Same summary line everywhere |
| `OpenItemsDrillCard` | Cashflow attention, closeout open-items, portfolio alerts | "Click here to act" idiom |
| `EntityFilter` | Cashflow, portfolio, portfolio-cvr | BB / PS / PFP / Consolidated |
| `PhotoGridTile` | Site photos, defects, RFIs, observations, walk-round | Auto-tagged photo display |
| `SecureLinkBanner` | Every portal landing | "Viewing via secure link" pattern |

---

## 4. Phased build-out roadmap

Lifecycle order (00 → 09) with two corrections: (a) Project hub ships before any workflow can attach; (b) cross-cutting engines (Inspections / Observations / Corrective Actions / Compliance Docs) ship as a horizontal slice before workflows 03 / 04 / 06 / 08 that all consume them.

### Slice 0 — Foundations (in progress)

Already shipped: Login, /dashboard, AdminHome, RoleSwitcher, DirectoryUser with Roles, SessionService with ActiveRole. Next foundation steps:

- Azure SQL persistence (replace `AllowListUserDirectory` and `InMemoryAccessRequestStore`).
- Invite-by-email.
- Project entity + `/projects` list + `/projects/{id}` shell with empty tabs.
- `/me/notifications` stub.

**Why first:** every other route hangs off a project. The hub is the spine.

### Slice 1 — Workflow 00 CRM (vertical slice)

`/leads` → `/leads/{id}` → five sub-tabs → outcome → on Won creates a `/projects/{id}` shell. Plus `/estimating-queue`, `/nurture`, `/portal/architect/leads/new`.

**Why first:** lifecycle begins at CRM. Every subsequent slice operates on real lead-to-project data rather than seed data.

**Entities:** Lead, Opportunity, Contact, Company, Architect Practice, Site Visit, Proposal, Win/Loss Reason, Project (shell).

### Slice 2 — Workflow 02 BoQ + Rate Library

`/projects/{id}/boq` with direct entry (US-NEW-02), bulk import (US-NEW-03), discipline tag, re-tender compare, sign-off. `/rate-library`.

**Why second:** projects without a BoQ have no commercial basis. Procurement, variations, valuations all reference BoQ line items.

**Entities:** BoQ, BoQ Line Item, Rate, Rate Library, Cost Code, Walk-Round Note.

### Slice 3 — Workflow 01 Drawings (P1 manual path)

`/projects/{id}/drawings` with US-NEW-01 upload, US-NEW-04 issue audit, US-01-02..05 supersedure logic. Mobile current-rev viewer at `/site/projects/{id}/drawings`.

**Why third:** BoQ and procurement both reference drawings; site delivery needs the mobile viewer; RFIs attach drawing extracts.

**Entities:** Drawing, Drawing Revision, Drawing Issue Record.

### Slice 4 — Cross-cutting engines

`/hs/*` (templates, inspections, audits, incidents, observations, corrective actions, toolbox talks, temp works). `/subcontractors` master directory + `/subcontractors/{id}/compliance`.

**Why now:** workflows 03, 04, 06, 08 all consume these engines. Building them horizontally prevents three half-versions.

**Entities:** Inspection Template, Inspection, Audit, Observation, Incident, Near Miss, Corrective Action, Toolbox Talk, Induction Record, Permit, Temporary Works, Subcontractor, Compliance Document, RAMS, CIS Status, Renewal Event.

### Slice 5 — Workflow 03 Procurement + Subcontractor portal

Desktop `/projects/{id}/procurement/*`, `/work-orders/{id}`. Portal `/portal/subcontractor/*` (bid + onboarding tracks).

**Why now:** BoQ + drawings + sub master + H&S gate are in place. Awards close pre-construction, unlock mobilisation.

**Entities:** Bid Package, Quote, Work Order.

### Slice 6 — Workflow 04 Mobilisation + Workflow 05 Changes

Mobilisation `/projects/{id}/mobilisation/*` with hard gate. Unified change register `/projects/{id}/changes/*`. Architect portal RFI/submittal/variation flows.

**Why now:** mobilisation gates workflow 06. Changes must be ready by the time site goes live; variation→procurement loop only works once procurement exists.

**Entities:** Mobilisation Checklist, RFI, Submittal, Variation, NoD.

### Slice 7 — Workflow 06 Site Delivery + mobile site app

Desktop `/projects/{id}/site/*` and `/programme`. Mobile PWA shell `/site/*`. Subcontractor portal check-in + RFI raise.

**Why now:** all prerequisites exist. The site app is the daily-driver for P05/P11.

**Entities:** Programme Task, Site Report, Defect (early identification).

### Slice 8 — Workflow 07 Commercial backbone

Largest slice. Internal order:

1. Timesheets + cost-code hard-block (desktop, mobile, subcontractor portal).
2. Programme Valuation Report.
3. CVR — full Fix #1/2/3 surface (forecast components, prelims, EOTs, packages, movement, snapshots).
4. Portfolio CVR.
5. Cashflow.

**Why now:** this is the headline justification for JPMS. Building it earlier on simulated data risks the team building toward Excel rather than reality.

**Entities:** Cost Code Budget, Cost Code Allocation, Claim Period, Valuation, Programme Valuation Report, CVR Snapshot, QS Accrual, Prelim Item, Prelim Forecast Entry, EOT, Forecast Component, Margin Trace, Daywork, Contra Charge, Subcontractor Retention, Cashflow Forecast Snapshot, Timesheet, Timesheet Approval.

### Slice 9 — Workflow 08 Close-Out

Desktop `/projects/{id}/closeout/*`. Portal close-out pack + VAT review. Subcontractor portal defects.

**Why now:** valuations and cost ledger must exist to draft VAT and settlement. Defects mature from snag log. Retention publishes downstream.

**Entities:** Defect (mature), Settlement Record, Zero-Rated VAT Analysis, Close-Out Pack.

### Slice 10 — Workflow 09 Portfolio Reporting

`/portfolio/*` reads from every upstream workflow. Snapshots per Claim Period.

**Why last:** dashboard only useful when source data is live across the portfolio.

**Entities:** Portfolio Snapshot, Leading Indicator, Threshold, Exception Alert.

---

## 5. Resolved gaps

| # | Gap | Resolution |
|---|---|---|
| 1 | US-05-12 (variation feeds programme + valuation on approval) | Server logic only, no UI. Triggered after the client/architect has approved in their portal. The downstream effect surfaces on the appropriate reports. No route. |
| 2 | US-07-10 / US-07-22 (publish approved valuations + day-rate hours) | Build a `/reports` section where the data is viewable and downloadable as CSV. Direct Xero integration is later — the platform is contained software for now. |
| 3 | US-09-09 (bid-hit rate per estimator) | Lives at `/portfolio/estimator` — it is estimator analytics, which is a portfolio-reporting concern. |
| 4 | US-08-15 (VAT confirmation) | VAT amount is manually entered (matching the Xero figure) at end of project; client confirms on the site. Same VAT route serves both architect (`/portal/architect/.../vat`) and client (`/portal/client/.../vat`); architect view is read-only advisory, client confirms. |
| 5 | US-07-19 (timesheet totals visible to architect/client) | Same view location; role gates the actions. Architect/client see the same totals; capability differs (e.g. approval powers). One shared component, role-aware controls. |
| 6 | Drill-down from summary to detailed views | Industry-standard pattern. Tabular summary rows are clickable, navigate to deeper detail. Breadcrumbs trail the hierarchy. Site map remains as drafted. |
| 7 | Submittals approval UI | No `US-05-NN` story confirms it. The `must-have-v1.md` mention may be stale. Treat the user stories as authoritative — leave `/projects/{id}/changes/submittals` and `/portal/architect/.../submittals` as placeholder routes until a story confirms the workflow. |
| 8 | Director annual H&S review | Route under `/portfolio/hs` as expected. Leave as a placeholder until a story confirms the exact view. |
| 9 | `/me/notifications` | Dropped. Notifications use PWA push (no in-app inbox). |
| 10 | `/work-orders` top-level | Add `/work-orders` portfolio-wide list with filtering (status, value, subcontractor, organisation). FD's view. Per-project list remains under `/projects/{id}/procurement`. |
