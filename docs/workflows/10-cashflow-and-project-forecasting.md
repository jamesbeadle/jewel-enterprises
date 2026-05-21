# Workflow 10 — Cashflow & Project Forecasting

**Group:** Project lifecycle (output)
**Purpose:** Produce a live cashflow forecast for the project portfolio, built **purely from JPMS project data** — work-order commitments, expected valuations per Claim Period, predicted completion %s, and retention timing. One of the two primary outputs of JPMS (the other is the Programme Valuation Report from workflow 05).
**Trigger:** Continuous — refreshed whenever upstream project data changes (new work order, approved variation, valuation issued, site % updated, timesheet approved).
**Frequency:** Live; daily review; weekly deep dive.
**Owner (target):** Finance Director (review and judgement); JPMS for the data assembly.
**Status:** Draft

---

## Scope rule for this workflow

The cashflow forecast in JPMS is built from **project data inside JPMS only**. It does **not** pull from Xero, Brightpay, Dext, or any accountancy system. The forecast is the JPMS view of what the project portfolio is going to bring in (approved AR-eligible valuations) and what it's going to cost (work-order commitments, approved timesheets, retention). The accountancy team uses this forecast plus their own ledger view to manage actual cash — that bookkeeping work is downstream and outside JPMS.

---

## Current state

1. FD rebuilds the cashflow tracker in Excel weekly from scattered sources (Xero balances, Brightpay payroll runs, the FD's mental model of project commitments).
2. Cross-entity charges between BB / PS / PFP are reconciled manually in a separate sheet.
3. Management view exists only in the FD's head until the Excel is updated.

---

## Target flow

1. **Live forecast assembled from JPMS data alone:**
   - **Forward commitments out** — open work orders by expected payment date (workflow 03), retention release dates from open close-outs (workflow 07), open subcontractor day-rate hours not yet invoiced (workflow 09).
   - **Forward income in** — Programme Valuation Report draft for the next Claim Period (workflow 05), approved variations awaiting client sign-off (workflow 04), settlement payments at project close (workflow 11).
   - **Completion-% prediction** — combines site-reported % (workflow 06) with timesheet-vs-budget burn rate (workflow 09) to give an expected close date per project.
2. **Cross-entity flag** on every commitment and expected income line so a consolidated and per-entity view both work.
3. **FD view** — full dashboard with drill-down to the underlying project record.
4. **Directors / MD view** — scoped read with the same totals, no drill-down into individual subcontractor data.
5. **Architect / Client view (where contract provides)** — scoped to their own project: programme valuation, completion %, expected close.

---

## JPMS functionality required

- Forecast engine that assembles forward commitments out and forward income in from workflows 03, 04, 05, 06, 07, 09, 11.
- Cross-entity flag on every commitment and income line.
- Completion-% prediction model — site-reported % blended with timesheet burn rate.
- Three scoped dashboards: FD (full), Directors / MD (scoped), Architect / Client (per project, optional).
- Snapshot per Claim Period so a historical series is retained.

---

## Inputs (all from JPMS)

- Work-order register (workflow 03) — expected payment dates and amounts.
- Approved variations (workflow 04) — adds to expected income / commitments.
- Programme Valuation Report (workflow 05) — current and next Claim Period.
- Site reports (workflow 06) — completion % per BoQ section.
- Defect register and retention timing (workflow 07).
- Approved timesheets (workflow 09) — labour burn vs cost-code budget.
- Settlement records (workflow 11) — final values at project close.

## Outputs (consumed downstream)

- **Accountancy team** uses the forecast alongside their Xero ledger view to manage actual cash, run AP / AR, and time payment runs. That bookkeeping work is outside JPMS.
- **Directors / MD** read the dashboard directly in JPMS.
- **Client (where contract provides)** sees the project-scoped slice.

---

## Acceptance criteria — "done looks like"

- The cashflow forecast is a live dashboard built from JPMS data, not an Excel rebuild.
- Forecast accuracy improves because completion % is real, not guessed.
- Cross-entity views (consolidated / per-entity) are accurate without month-end manual correction.
- Directors view the cash position any time without asking the FD.

---

## Entities touched

`Cashflow Forecast` · `Project` · `Work Order` · `Variation` · `Valuation` · `Claim Period` · `Site Report` · `Timesheet Approval` · `Cost Code Budget` · `Settlement Record`

See [`/docs/data-models/entity-relationship.md`](../data-models/entity-relationship.md).

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| P01 Architect | Read — project-scoped dashboard where the contract provides |
| P03 Project & Commercial Lead | Contributor — read the project slice; trigger refresh on valuation / WO completion |
| P06 Finance Director | **Owner** — review, judgement, intervention |
| P07 Directors / MD | Approver — strategic cashflow decisions |

See [`/docs/requirements/permission-matrix.md`](../requirements/permission-matrix.md).

---

## Open questions

- [ ] Forecast horizon — 13-week rolling? Longer for some audiences?
- [ ] Completion-% prediction model — purely site-reported, purely timesheet-vs-budget, or a blend? Affects forecast accuracy.
- [ ] Cross-entity reporting — separate dashboards per entity, or one consolidated view with filters?
- [ ] Architect / Client scoped view — opt-in per contract, or default for all client-facing projects?
- [ ] Snapshot cadence — does JPMS retain a full forecast snapshot per Claim Period for historical comparison?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the Finance Director
- [ ] Forecast horizon and refresh cadence agreed
- [ ] Completion-% prediction model agreed
- [ ] Cross-entity view confirmed
- [ ] Director scoped view confirmed
- [ ] Architect / Client view confirmed against at least one client contract
- [ ] Permissions confirmed
- [ ] Signed off
