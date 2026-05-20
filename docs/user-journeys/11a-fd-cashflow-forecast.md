# Journey 11a — Finance Director: morning cashflow review

> Persona slice through [Workflow 11 — Cashflow & Management Reporting](../workflows/11-cashflow-and-management-reporting.md). Anchored on the **primary platform pain point** identified in the [2026-05-18 domain discovery](../meetings/2026-05-18-domain-discovery.md).

**Actors:** P07 Finance Director (primary). Consumers: P08 Directors / MD. Sources: P03 Project & Commercial Lead (forward commitments), P05 Site Team (timesheets via 06 → 12).
**Goal:** Walk in, see an accurate live cashflow forecast across BB/PS/PFP without rebuilding it in Excel, identify the items that need intervention, and act on them before the morning is over.
**Frequency:** Daily morning review; weekly deep dive.
**Success metric:** FD spends ≥80% of cashflow time on judgement/intervention, ≤20% on data assembly. Forecast accuracy at +1 week and +4 weeks within agreed tolerance.
**Status:** Draft
**Last reviewed:** —

---

## Trigger

FD opens JPMS at the start of the day (or any director opens the cashflow dashboard).

---

## Pre-conditions

- Xero AP / AR balances synced.
- Brightpay payroll commitments synced.
- JPMS work-order forward commitments up to date (workflow 03).
- JPMS AR triggers fired for any approved valuations / WO completions (workflow 10).

---

## Steps

### 1. Land on the cashflow dashboard
- The default view is a 13-week rolling cashflow with entity filter (BB / PS / PFP / Consolidated).
- The "as of" timestamp shows last data sync per source (Xero AP, Xero AR, Brightpay, JPMS commitments).
- KPI strip: opening cash, projected min cash in horizon, days-to-min, total exception count.

### 2. Drill into exceptions
- Exception queue surfaces: late AR (workflow 10), AP invoices awaiting coding (workflow 09), subcontractor invoice errors (workflow 09), unresolved cross-entity charges (workflow 11), payroll variance alerts (workflow 12).
- Each exception is a one-click link to the underlying record.

### 3. Inspect a forward commitment
- Drill into a work order and see: agreed value, retention, expected payment date(s), CIS deduction, link to the approving Project & Commercial Lead.

### 4. Stress-test
- Slider: "what if AR slips by N days?" — re-runs the projection without changing source data.

### 5. Act
- Approve a payment run draft (workflow 09).
- Release queued AR invoices (workflow 10).
- Re-route a query from the inbox (workflow 13).

### 6. Hand-off view for Directors
- Toggle to the Director view — same data, scoped fields, no exception queue.

---

## Edge cases & exceptions

- A source feed is stale (Xero hasn't synced) — dashboard shows the stale timestamp prominently and disables affected projections.
- A cross-entity charge isn't yet allocated — surfaces in the exception queue with a "decide entity" action.
- Multiple users hit "approve" on the same payment run — second one sees a "now superseded" message with a diff link.

---

## Data structures (referenced)

- `CashflowForecast`, `SupplierInvoice`, `SalesInvoice`, `PaymentRun`, `WorkOrder`, `Valuation`, `Timesheet`. See [`/docs/data-models/entity-relationship.md`](../data-models/entity-relationship.md).

---

## Permissions

| Step | Role | Can do |
|---|---|---|
| 1–4 | P07 Finance Director | Read all; drill |
| 5 | P07 Finance Director | Approve payment runs, release AR, route inbox |
| 6 | P08 Directors / MD | Read scoped Director view |
| All | P09 Outsourced IT | — |

See [`/docs/requirements/permission-matrix.md`](../requirements/permission-matrix.md).

---

## Open questions

- [ ] Stress-test scenarios — fixed presets, or freeform sliders?
- [ ] Director view scoping — by entity, by project, or full?
- [ ] Forecast horizon — 13-week rolling, or different default per audience?

---

## Confirmation checklist

- [ ] Walked through end-to-end during a role-play session with the Finance Director
- [ ] All exception types confirmed
- [ ] Stress-test behaviour confirmed
- [ ] Director hand-off view confirmed
- [ ] Permissions confirmed
- [ ] Signed off by: _name, role, date_
