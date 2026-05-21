# Workflow 09 — Timesheet Management (cost-code-aware)

**Group:** Project lifecycle
**Purpose:** Capture site team and subcontractor timesheets against the right cost code on the project so completion-% prediction stays accurate, the cost-code budget rule is enforced, and approved entries are available for downstream accountancy to reconcile against subcontractor invoices.
**Trigger:** Time spent on a project — captured during the day on mobile (site) or end-of-period (manual).
**Frequency:** Daily capture; weekly approval cycle. Client-visible where the contract provides for it.
**Owner (target):** Project & Commercial Lead (approval); Site Team and subcontractors (capture).
**Status:** Draft

---

## Scope rule for this workflow

JPMS captures timesheets for **cost-code allocation and completion-% prediction on the project**. JPMS does **not** run payroll — internal staff payroll is handled by the accountancy team in Brightpay, outside JPMS. JPMS does **not** pay subcontractors — subcontractors are paid via AP (Xero) by the accountancy team using JPMS work-order and timesheet data downstream.

---

## Current state

1. Timesheets captured ad hoc (paper, phone, WhatsApp).
2. Manual cost-code allocation in Excel — no inline check that the cost code has remaining budget.
3. Overruns surface only at month-end valuation or post-completion settlement.
4. Client visibility into timesheet-driven cost is typically a manual report on request.

---

## Target flow

1. **Capture** — each entry is tied to a project + a person + a date / duration. Site team capture via the mobile site app (workflow 06); subcontractors capture day-rate entries via the subcontractor portal where applicable.
2. **Allocation** — each entry allocated to a cost code on the project. The allocator (capturer or approver) picks from the project's cost-code list with remaining budget visible inline.
3. **Cost-code budget rule** — if the chosen cost code has **no remaining budget**, JPMS blocks the allocation and offers exactly two paths:
   - **Raise a work order** against that cost code (routes into workflow 03 Procurement) and re-attempt allocation once approved.
   - **Allocate to a different cost code** with available budget (back to step 2).
4. **Approval** — Project & Commercial Lead approves the week's timesheet batch in JPMS. Approval is auditable per entry.
5. **Client view (where the contract provides)** — approved timesheet totals per cost code surface in the client portal alongside the live Programme Valuation Report (workflow 05).
6. **Downstream publication** — approved timesheets are made available to the accountancy team for invoice reconciliation (subcontractor day-rate invoices matched against approved hours) and to workflow 10 for completion-% input into the cashflow forecast.

---

## JPMS functionality required

- Timesheet entry on mobile (site) and the subcontractor portal (day-rate).
- Cost-code register per project with budget per code.
- Inline "remaining budget" indicator at the point of allocation.
- **Hard validation:** no allocation to a cost code at 0 remaining budget unless a linked work order is raised or the entry is re-allocated.
- Inline "raise WO" action that hands off to workflow 03 with the cost code pre-filled.
- Weekly bulk approval surface for the Project & Commercial Lead.
- Client portal view (read-only, per-cost-code totals) — gated on contract setting.
- Audit trail of allocation, re-allocation, approval, and any budget-override decisions.
- Cross-entity flag (BB / PS / PFP) on every entry.

---

## Inputs

- Site app captures (workflow 06) for site staff.
- Subcontractor portal entries (day-rate, where applicable).

## Outputs (consumed downstream)

- **Workflow 10 — Cashflow & Project Forecasting** — approved hours by cost code feed completion-% prediction and the labour commitment line.
- **Workflow 05 — Programme & Valuations** — approved hours by cost code roll up into the Programme Valuation Report.
- **Workflow 11 — Project Completion Settlement** — open timesheet items at PC are part of the settlement open-items dashboard.
- **Accountancy (downstream, out of JPMS)** — approved subcontractor day-rate hours are published for AP invoice reconciliation in Xero. JPMS does not pay anyone.

---

## Acceptance criteria — "done looks like"

- No timesheet entry is ever booked to a cost code with no remaining budget without an explicit, audited resolution (WO raised, or re-allocation).
- The Project & Commercial Lead approves a clean weekly batch from one surface, not a spreadsheet round-up.
- The client sees the same approved totals as the project team on any given day.
- Completion-% predictions on workflow 10 reflect the period's labour within hours of approval, not at month-end.
- Accountancy can match a subcontractor day-rate invoice against approved JPMS hours without re-keying.

---

## Entities touched

`Project` · `Person` · `Subcontractor` · `Timesheet` · `Timesheet Approval` · `Cost Code` · `Cost Code Budget` · `Cost Code Allocation` · `Work Order` · `BoQ Line Item`

See [`/docs/data-models/entity-relationship.md`](../data-models/entity-relationship.md).

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| P01 Architect | Read (client portal view — where the contract provides) |
| P02 Subcontractor | Contributor — captures own entries where day-rate applies |
| P03 Project & Commercial Lead | **Owner / Approver** — weekly batch approval; raises WO on overrun |
| P05 Site Team | Contributor — site capture via mobile |
| P06 Finance Director | Approver — cost-code overrun above threshold |
| P07 Directors / MD | Approver — overrun above threshold |

See [`/docs/requirements/permission-matrix.md`](../requirements/permission-matrix.md).

---

## Open questions

- [ ] Are timesheets allocated **per day** or **per task** within a day? Affects UX and entity shape.
- [ ] When budget is hit, do we **hard-block** until a WO is raised or a different cost code chosen, or do we allow **soft-warn-and-proceed** with sign-off above a threshold?
- [ ] Subcontractor day-rate — is the entry captured by the subcontractor, the site manager, or both?
- [ ] Client portal visibility — opt-in per contract, or default for all client-facing projects?
- [ ] Late corrections — how far back can a person amend an already-approved entry, and who re-approves?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the Project & Commercial Lead
- [ ] Walked through capture with a site manager and a subcontractor
- [ ] Cost-code budget rule confirmed (hard-block or soft-warn decision recorded)
- [ ] "Raise WO" handoff to workflow 03 confirmed
- [ ] Client portal view confirmed with at least one client contract
- [ ] Downstream publication confirmed with the accountancy team (the data shape they need for invoice reconciliation)
- [ ] Permissions confirmed
- [ ] Signed off
