# Journey 10a — Finance Director: morning cashflow review

> Persona slice through [Workflow 10 — Cashflow & Project Forecasting](../workflows/10-cashflow-and-project-forecasting.md). The FD's view of the JPMS-produced cashflow forecast — one of the two primary outputs of the platform.

**Actors:** P06 Finance Director (primary). Consumers: P07 Directors / MD, P01 Architect (project-scoped view where contract provides). Sources: P03 Project & Commercial Lead (forward commitments), P05 Site Team (completion %s via 06 and timesheets via 09).
**Goal:** Walk in, see an accurate live cashflow forecast across the project portfolio without rebuilding it in Excel, identify the items that need intervention, and act on them.
**Frequency:** Daily morning review; weekly deep dive.
**Success metric:** Forecast accuracy at +1 week and +4 weeks within agreed tolerance. FD spends most cashflow time on judgement, not data assembly.
**Status:** Draft

---

## Trigger

FD opens JPMS at the start of the day (or any Director opens the cashflow dashboard).

---

## Pre-conditions

- Project data is current in JPMS: work orders (workflow 03), variations (04), valuations (05), site reports (06), timesheets (09), settlement records (11) where applicable.

---

## Steps

### 1. Land on the cashflow dashboard
- 13-week rolling cashflow with entity filter (BB / PS / PFP / Consolidated).
- "As of" timestamp shows last refresh.
- KPI strip: opening cash, projected min cash in horizon, days-to-min, items needing FD attention.

### 2. Drill into items needing attention
- The forecast surfaces project-level concerns: a project whose completion-% prediction is sliding, a work order whose payment date is shifting, a Claim Period whose valuation hasn't been issued, retention release pending close-out.
- Each item is a one-click link to the underlying project record.

### 3. Inspect a project's contribution to cash
- Drill into a project and see: forward commitments out (open work orders), forward income in (next Programme Valuation Report, settlement), current completion % (site-reported + timesheet burn), retention timing.

### 4. Stress-test
- Slider: "what if completion-% slips by N% on this project?" or "what if the next valuation is approved a week late?" — re-runs the projection without changing source data.

### 5. Act
- Trigger the next Programme Valuation Report (links to workflow 05).
- Flag a project for a deep-dive with the Project & Commercial Lead.
- Approve a cost-code overrun (workflow 09).

### 6. Hand-off view for Directors and Client
- Toggle to the Director view — same data, scoped fields, no FD-level intervention controls.
- For client-facing projects, toggle to the project-scoped view that mirrors what the client sees.

---

## Edge cases & exceptions

- Project data is stale (no site report this week) — dashboard shows the stale-data flag on that project and disables the forecast for it until refreshed.
- Cross-entity charges not yet allocated — surfaces in the items-needing-attention queue.
- Multiple Directors viewing simultaneously — read-only consistent view; no concurrency concerns.

---

## Data structures (referenced)

- `CashflowForecast`, `WorkOrder`, `Valuation`, `ClaimPeriod`, `SiteReport`, `TimesheetApproval`, `CostCodeBudget`, `SettlementRecord`. See [`/docs/data-models/entity-relationship.md`](../data-models/entity-relationship.md).

---

## Permissions

| Step | Role | Can do |
|---|---|---|
| 1–4 | P06 Finance Director | Read all; drill; stress-test |
| 5 | P06 Finance Director | Trigger valuation refresh; approve overrun; flag projects |
| 6 | P07 Directors / MD | Read scoped Director view |
| 6 | P01 Architect | Read project-scoped view (where contract provides) |

See [`/docs/requirements/permission-matrix.md`](../requirements/permission-matrix.md).

---

## Open questions

- [ ] Stress-test scenarios — fixed presets, or freeform sliders?
- [ ] Director view scoping — by entity, by project, or full?
- [ ] Forecast horizon — 13-week rolling, or different default per audience?
- [ ] Snapshot per Claim Period — retained for historical comparison?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the Finance Director
- [ ] Items-needing-attention queue confirmed as the right list
- [ ] Stress-test behaviour confirmed
- [ ] Director and Client hand-off views confirmed
- [ ] Permissions confirmed
- [ ] Signed off
