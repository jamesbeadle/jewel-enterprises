# Workflows

Process maps for JPMS. Where a journey describes **what one actor experiences**, a workflow describes **what happens across actors and systems** for a given process.

> 📁 **`_templates/`** holds reference-only example diagrams. Real workflows live in this folder root.

---

## Scope

The eleven workflows in this folder are the JPMS in-scope set, decided on 2026-05-21. See [`/docs/meetings/2026-05-21-scope-refinement.md`](../meetings/2026-05-21-scope-refinement.md) for the rule and the twelve workflows that were considered and ruled out (back-office accountancy, payroll, HR, IT admin, facilities, marketing).

---

## Conventions

- One file per workflow: `NN-short-kebab.md`.
- Each file follows the same shape: purpose, trigger, frequency, owner, current state, target flow, JPMS functionality required, integrations, acceptance criteria, entities touched, roles involved, open questions, confirmation checklist.
- Mermaid is the default notation for any embedded diagrams (renders natively on GitHub).
- BPMN (via Camunda Modeler or bpmn.io) for finance / approval flows where formal notation is needed for audit. Use `.bpmn` source files alongside the `.md`.

---

## Index

Status legend: **Draft** · **In Review** · **Confirmed**.

| # | Workflow | File | Owner | Status |
|---|---|---|---|---|
| 01 | Drawing Receipt & Distribution | [`01-drawing-receipt.md`](01-drawing-receipt.md) | P03 Project & Commercial Lead | Draft |
| 02 | Pre-Construction: Tender & BoQ | [`02-preconstruction-tender-boq.md`](02-preconstruction-tender-boq.md) | P03 Project & Commercial Lead | Draft |
| 03 | Subcontractor Procurement (Bid → Award) | [`03-subcontractor-procurement.md`](03-subcontractor-procurement.md) | P03 Project & Commercial Lead | Draft |
| 04 | Variations, RFIs & Delays | [`04-variations-rfis-delays.md`](04-variations-rfis-delays.md) | P03 Project & Commercial Lead | Draft |
| 05 | Programme & Valuations | [`05-programme-and-valuations.md`](05-programme-and-valuations.md) | P03 Project & Commercial Lead | Draft |
| 06 | Site Reporting & Progress | [`06-site-reporting-and-progress.md`](06-site-reporting-and-progress.md) | P05 Site Team | Draft |
| 07 | Project Close-Out & Defects | [`07-project-close-out-and-defects.md`](07-project-close-out-and-defects.md) | P03 Project & Commercial Lead | Draft |
| 08 | Subcontractor Compliance & Onboarding | [`08-subcontractor-compliance-and-onboarding.md`](08-subcontractor-compliance-and-onboarding.md) | P04 Office & Compliance Coordinator | Draft |
| 09 | Timesheet Management (cost-code-aware) | [`09-timesheet-management.md`](09-timesheet-management.md) | P03 Project & Commercial Lead | Draft |
| 10 | Cashflow & Project Forecasting | [`10-cashflow-and-project-forecasting.md`](10-cashflow-and-project-forecasting.md) | P06 Finance Director | Draft |
| 11 | Project Completion Settlement & VAT Analysis | [`11-project-completion-settlement.md`](11-project-completion-settlement.md) | P06 Finance Director | Draft |

---

## Phased delivery

1. **Platform foundations** — OAuth sign-in, the ASP.NET Core API, Azure SQL schema, admin-managed directory.
2. **Phase 1 — Project lifecycle core:** 01, 02, 08, 03, 04, 06, 09.
3. **Phase 2 — Outputs:** 05 (Programme Valuation Report), 10 (cashflow forecast — primary pain-point anchor).
4. **Phase 3 — Close-out:** 07, 11.

This phasing is mirrored in root [`README.md`](../../README.md#116-roadmap-rough) Section 11.6.
