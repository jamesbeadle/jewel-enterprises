# Workflow 22 — Timesheet Management (client-facing, cost-code-aware)

**Group:** Project lifecycle
**Purpose:** Capture, allocate, and approve project timesheets against the right cost code, with a hard rule that prevents allocations to a cost code with no remaining budget.
**Trigger:** Time spent on a project — captured during the day (site app, office check-in) or at the end of the period (manual entry).
**Frequency:** Daily capture; weekly approval cycle; client-visible where the contract provides for it.
**Owner (target):** Project & Commercial Lead (approval); Site Team and office staff (capture); Finance Director (payroll + commercial roll-up).
**Current monthly hours:** _to be confirmed_ — not in the original audit; surfaced 2026-05-20.
**Status:** Draft
**Last reviewed:** —

---

## Current state

1. Timesheets captured ad hoc (paper, phone, WhatsApp).
2. FD chases entries weekly; allocations to cost codes performed manually in Excel.
3. No automated check that the cost code has budget — overruns surface only at month-end valuation or post-completion settlement.
4. Client visibility into timesheet-driven cost is typically a manual report on request.

---

## Target flow (post-automation)

1. **Capture** — each entry tied to a project + a person + a date/duration. Captured via the site app (workflow 06) for site staff, or via office check-in for back-office.
2. **Allocation** — each entry allocated to a cost code on the project. The allocator (capturer or approver) picks from the project's cost-code list with remaining budget visible inline.
3. **Cost-code budget rule** — if the chosen cost code has **no remaining budget**, JPMS blocks the allocation and offers exactly two paths:
   - **Raise a work order** against that cost code (routes into workflow 03 Procurement) and re-attempt allocation once approved.
   - **Allocate to a different cost code** with available budget (back to step 2).
4. **Approval** — Project & Commercial Lead approves the week's timesheet batch in JPMS. Approval is auditable per entry.
5. **Client view (where the contract provides)** — approved timesheet totals per cost code surface in the client portal alongside the live programme valuation (workflow 05).
6. **Downstream feeds** — approved timesheets feed payroll (workflow 12), AP for subcontractor day-rate where applicable, and the cashflow forecast (workflow 11).

---

## JPMS functionality required

- Timesheet entry on mobile (site) and desktop (office).
- Cost-code register per project with budget per code.
- Inline "remaining budget" indicator at the point of allocation.
- **Hard validation:** zero allocation to a cost code at 0 remaining budget unless a linked work order is raised or the entry is re-allocated.
- Inline "raise WO" action that hands off to workflow 03 with the cost code pre-filled.
- Bulk weekly approval surface for the Project & Commercial Lead.
- Client portal view (read-only, per-cost-code totals) — gated on contract setting.
- Audit trail of allocation, re-allocation, approval, and any budget-override decisions.
- Cross-entity flag (BB / PS / PFP) on every entry.

---

## Integrations & adjacent systems

- **Brightpay** (workflow 12) — approved entries feed payroll.
- **Xero** (workflow 09 AP) — subcontractor day-rate entries match through to AP.
- **JPMS cost-code register** — the single source of truth for budgets and remaining balances (this workflow is the primary writer to "remaining").
- **Workflow 03** — invoked when "raise WO" is the chosen overrun resolution.
- **Workflow 11** — approved entries feed the live cashflow.

---

## Acceptance criteria — "done looks like"

- No timesheet entry is ever booked to a cost code with no remaining budget without an explicit, audited resolution (WO raised, or re-allocation).
- The Project & Commercial Lead approves a clean weekly batch from one surface, not a spreadsheet round-up.
- The client sees the same approved totals as the FD on any given day.
- The cashflow forecast (workflow 11) reflects the period's labour commitment within hours of approval, not at month-end.

---

## Entities touched

`Project` · `Person` · `Subcontractor` · `Timesheet` · `Timesheet Approval` · `Cost Code` · `Cost Code Budget` · `Cost Code Allocation` · `Work Order` · `Programme Task` · `Valuation`

See [`/docs/data-models/entity-relationship.md`](../data-models/entity-relationship.md).

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| P01 Architect | Read (client portal view — where the contract provides) |
| P02 Subcontractor | Contributor — captures own entries where day-rate applies |
| P03 Project & Commercial Lead | **Owner / Approver** — weekly batch approval; raises WO on overrun |
| P04 Office & Compliance Coordinator | Contributor — back-office capture for non-site staff |
| P05 Site Team | Contributor — site capture via mobile |
| P07 Finance Director | Approver — payroll exception review; cashflow impact |
| P08 Directors / MD | Approver — above-threshold budget overrides |

See [`/docs/requirements/permission-matrix.md`](../requirements/permission-matrix.md).

---

## Open questions

- [ ] Are timesheets allocated **per day** or **per task** within a day? Affects UX and entity shape.
- [ ] When budget is hit, do we **hard-block** until a WO is raised or a different cost code chosen, or do we allow **soft-warn-and-proceed** with FD or Director sign-off above a threshold?
- [ ] Does this workflow handle subcontractor day-rate in addition to PAYE timesheets, or only PAYE?
- [ ] Client portal visibility — opt-in per contract, or default for all client-facing projects?
- [ ] Late corrections — how far back can a person amend an already-approved entry, and who re-approves?
- [ ] Cross-entity charging — does an entry written against one entity's project ever need to settle against a different entity's payroll? If so, how does the cost-code rule behave at the join?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the Project & Commercial Lead
- [ ] Walked through capture with a site manager and a back-office user
- [ ] Cost-code budget rule confirmed (hard-block or soft-warn decision recorded)
- [ ] "Raise WO" handoff to workflow 03 confirmed
- [ ] Client portal view confirmed with at least one client contract
- [ ] Downstream feeds confirmed (Brightpay, Xero AP, cashflow)
- [ ] Permissions confirmed
- [ ] Signed off by: _name, role, date_
