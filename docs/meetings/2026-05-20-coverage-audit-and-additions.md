# Meeting: Automation-task coverage audit + new JPMS scope (timesheets / settlement / VAT)

**Date:** 2026-05-20
**Location:** Async, follow-on to the 2026-05-20 JBB workflow audit
**Attendees:** Nigel Reilly _(project lead)_

---

## Agenda

1. Read the *Automated Tasks* tab of `Jewel Task Analysis (1).xlsx` and audit coverage against the existing 21 workflows and 8 user journeys.
2. Confirm that every task's actor maps onto a persona in P01–P09.
3. Capture additional scope Nigel called out in the same conversation:
   - **Client-facing Timesheet Management** with cost-code allocation rules.
   - **Project Completion Settlement** workflow.
   - **Zero-rated VAT analysis** agreed with the client at the end of the project.
4. Reconcile Nigel's hierarchical Project Management outline against existing workflows and make any implicit links explicit.

---

## Notes

- The spreadsheet has five tabs. This note covers only **Automated Tasks** (47 rows, staff members: James Clark, Chris Reeves, Jeremy Ferendinos, Sofia, Sarah Collins, Katie-Louise Hicks). The *Human In The Loop Tasks* tab is intentionally out of scope — those tasks don't go through JPMS.
- Nigel's notes (column A) consistently make the same point: "this can be automated by the new project management system because the inputs originate inside JPMS rather than being scraped from email and spreadsheets". Where Nigel specifically defers automation (e.g. statement reconciliation, accounts inbox posting, cross-entity invoice chasing) the relevant workflow is already aware of the human-in-the-loop expectation.
- Full row-by-row coverage table lives in [`/docs/requirements/automation-task-coverage.md`](../requirements/automation-task-coverage.md). Headline result: **40 of 47 tasks fully covered, 5 partial (sub-rules to add to existing workflows), 7 out of operational scope (governance / meta).**
- Persona check: every named staff member maps cleanly to an existing persona — **no new personas required.** Chris Reeves's estimating-heavy task set confirms P03 Project & Commercial Lead absorbs the internal QS function as intended.
- The new scope on timesheets / settlement / VAT is genuinely new — it isn't a re-cut of any existing workflow. The cost-code budget rule in particular adds a new validation surface that touches AP (09), procurement (03), and the to-be-written timesheet workflow.
- Nigel's Project Management outline (New Project → Architect drawings → bid packages → approval/rejection; Project Change → updated drawings / client change / site issues; Project Change Actions → VO loop, RFI, NoD; Reports → Programme Valuation Report with Claim Values per Claim Period, VO List) is **mostly covered** by workflows 01, 03, 04, 05. The points that needed making explicit are listed in D5 below.

---

## Decisions

| # | Decision | Owner | Date |
|---|---|---|---|
| D1 | All 47 spreadsheet tasks are audited against existing workflows in [`/docs/requirements/automation-task-coverage.md`](../requirements/automation-task-coverage.md). The coverage table is the single source of truth for "does JPMS cover this task". | Nigel | 2026-05-20 |
| D2 | No new personas are added. Every named staff member maps onto an existing P01–P09 role. Chris Reeves's task set confirms internal QS work sits with P03 Project & Commercial Lead. | Nigel | 2026-05-20 |
| D3 | A new **Workflow 22 — Timesheet Management** is added under [`/docs/workflows/22-timesheet-management.md`](../workflows/22-timesheet-management.md). Hard rule: a timesheet entry **cannot be allocated to a cost code with no remaining budget** unless either (a) a work order is raised against that cost code, or (b) the allocation is moved to a different cost code with available budget. Timesheet approval is a project-side flow, client-facing where the contract carries client visibility. | Nigel | 2026-05-20 |
| D4 | A new **Workflow 23 — Project Completion Settlement** is added under [`/docs/workflows/23-project-completion-settlement.md`](../workflows/23-project-completion-settlement.md). Triggered by Practical Completion (the same trigger as workflow 07 defects). Covers: settling all open timesheet / cost-code allocations; performing the zero-rated VAT analysis; obtaining client agreement on the VAT outcome; producing the final settlement record for retention release. | Nigel | 2026-05-20 |
| D5 | Two existing workflows are extended to make Nigel's hierarchy explicit: **Workflow 04** now states that a Variation Order can trigger the workflow 03 procurement loop (bid package → approval / rejection → award → updated WO) when the change requires a subcontractor price. **Workflow 05** now names "Programme Valuation Report" and "Claim Period" as first-class concepts, and adds a "Variation Orders list" as a sibling report. | Nigel | 2026-05-20 |
| D6 | Five "partial" coverage rows produce backlog items captured inside existing workflow files (no new workflows): TBT reminders in 18 / 08; broad subcontractor notifications in 08; scheduled content distribution in 20; policy review cadence in 17; consistent AI exception-queue UX across 09 / 13 / 03. Each is captured as an open question on the relevant workflow. | Nigel | 2026-05-20 |
| D7 | New entities surfaced by this conversation are added to the data model: **Cost Code Budget, Cost Code Allocation, Timesheet Approval, Claim Period, VAT Analysis, Settlement Record.** All schemas remain *to be created* until the relevant workflow moves Draft → In Review. | Nigel | 2026-05-20 |

---

## Action items

- [ ] Walk the new workflows 22 and 23 with the named operational owners (P07 FD for settlement / VAT, P03 PCL + P05 Site Team for timesheet capture, P04 OCC for compliance gating) — **Nigel**, due before the next finance session.
- [ ] Resolve open questions on workflow 22 — particularly: who is the named approver per project (P03 PCL or a delegate); cost-code budget overrun policy (block vs warn); whether the timesheet flow handles subcontractor day-rate as well as PAYE.
- [ ] Resolve open questions on workflow 23 — particularly: VAT analysis sign-off responsibility (client-side signatory); retention release dependency between 07 (defects) and 23 (settlement).
- [ ] Add the five backlog items from D6 into the open-questions section of workflows 08, 17, 18, 20.

---

## Open questions raised

- [ ] Cost-code budget overrun — when "no remaining budget" is hit on a timesheet allocation, do we hard-block until a WO is raised or a different cost code chosen, or do we allow soft-warn-and-proceed with FD sign-off?
- [ ] Are timesheets allocated **per day** or **per task** within a day? Affects the UX and the entity shape.
- [ ] Who triggers Practical Completion → workflow 23? Is it the same trigger as workflow 07 defects, or a separate explicit "PC declared" event?
- [ ] Claim Period — does this term map to a fixed monthly cycle, a contract-specific cycle, or both? Affects valuation reporting.
- [ ] Zero-rated VAT analysis — is this always required on every project, or only certain contract types? Triggers the workflow conditionally.

---

## Artefacts updated

- Created [`/docs/requirements/automation-task-coverage.md`](../requirements/automation-task-coverage.md) — coverage table for the 47 Automated Tasks rows.
- Created [`/docs/workflows/22-timesheet-management.md`](../workflows/22-timesheet-management.md) — new client-facing timesheet workflow with the cost-code budget rule.
- Created [`/docs/workflows/23-project-completion-settlement.md`](../workflows/23-project-completion-settlement.md) — new project completion settlement + zero-rated VAT analysis workflow.
- Updated [`/docs/workflows/04-variations-rfis-delays.md`](../workflows/04-variations-rfis-delays.md) — explicit VO → bid-package loop when subcontractor pricing is required.
- Updated [`/docs/workflows/05-programme-and-valuations.md`](../workflows/05-programme-and-valuations.md) — "Programme Valuation Report" and "Claim Period" named as first-class concepts; "Variation Orders list" added as a sibling report.
- Updated [`/docs/workflows/README.md`](../workflows/README.md) — index extended to 23 workflows.
- Updated [`/docs/requirements/permission-matrix.md`](../requirements/permission-matrix.md) — two new rows for workflows 22 and 23.
- Updated [`/docs/data-models/entity-relationship.md`](../data-models/entity-relationship.md) — new entities (Timesheet Approval, Cost Code Budget, Cost Code Allocation, Claim Period, VAT Analysis, Settlement Record) added to ERD + entity index.
- Updated root [`README.md`](../../README.md) — Section 6.1 workflows table extended; Section 7 entities extended; Section 11.6 roadmap reflects the new workflows.
- Open questions appended to workflows 08, 17, 18, 20 covering the five partial-coverage backlog items from D6.
