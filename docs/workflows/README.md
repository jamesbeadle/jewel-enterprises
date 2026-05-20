# Workflows

Process maps that complement user journeys. Where a journey describes **what one actor experiences**, a workflow describes **what happens across actors and systems** for a given process.

> 📁 **`_templates/`** holds reference-only example diagrams. Real workflows live in this folder root.

---

## Where the workflows come from

The current twenty-three workflows were captured from the JBB workflow audit (May 2026, `JBB_Workflow_Maps.docx`) plus follow-on scope added directly during JPMS planning. The audit produced acceptance criteria for each workflow; those become the JPMS test cases for that workflow.

---

## Conventions

- One file per workflow: `NN-short-kebab.md` (e.g. `09-accounts-payable.md`).
- Mirror the audit structure: purpose, trigger, frequency, owner, current monthly hours, current state, target flow, JPMS functionality required, integrations, acceptance criteria, entities touched, roles involved, open questions, confirmation checklist.
- Mermaid is the default notation for any embedded diagrams (renders natively on GitHub).
- BPMN (via Camunda Modeler or bpmn.io) for finance / approval flows where formal notation is needed for audit. Use `.bpmn` source files alongside the `.md`.

---

## Process for adding a real workflow

1. A workflow is usually identified during an audit or while reviewing a journey. Capture which meeting it came from.
2. Look at [`_templates/workflow-example.mmd`](_templates/workflow-example.mmd) for the shape of a Mermaid flowchart — reference only.
3. Create the new file in this folder using the next number prefix.
4. Add the row to the index below and to root [`README.md`](../../README.md#6-user-journeys--workflows) Section 6.
5. Walk it with the named owner; tick the confirmation checklist when signed off.

---

## Index

Status legend: **Draft** · **In Review** · **Confirmed**.

### Project lifecycle

| # | Workflow | File | Target owner | Current h/mo | Status |
|---|---|---|---|---|---|
| 01 | Drawing Receipt & Distribution | [`01-drawing-receipt.md`](01-drawing-receipt.md) | Project & Commercial Lead | ~15 | Draft |
| 02 | Pre-Construction: Tender & BoQ | [`02-preconstruction-tender-boq.md`](02-preconstruction-tender-boq.md) | Project & Commercial Lead | ~50 | Draft |
| 03 | Subcontractor Procurement (Bid → Award) | [`03-subcontractor-procurement.md`](03-subcontractor-procurement.md) | Project & Commercial Lead | ~35 | Draft |
| 04 | Variations, RFIs & Delays | [`04-variations-rfis-delays.md`](04-variations-rfis-delays.md) | Project & Commercial Lead | ~25 | Draft |
| 05 | Programme & Valuations | [`05-programme-and-valuations.md`](05-programme-and-valuations.md) | Project & Commercial Lead | ~10 | Draft |
| 06 | Site Reporting & Progress | [`06-site-reporting-and-progress.md`](06-site-reporting-and-progress.md) | Site Team / Project Lead | ~25 | Draft |
| 07 | Project Close-Out & Defects | [`07-project-close-out-and-defects.md`](07-project-close-out-and-defects.md) | Project & Commercial Lead | ~5 | Draft |
| 22 | Timesheet Management (cost-code-aware) | [`22-timesheet-management.md`](22-timesheet-management.md) | Project & Commercial Lead | tbc | Draft |
| 23 | Project Completion Settlement & VAT Analysis | [`23-project-completion-settlement.md`](23-project-completion-settlement.md) | Finance Director | tbc | Draft |

### Supplier & subcontractor management

| # | Workflow | File | Target owner | Current h/mo | Status |
|---|---|---|---|---|---|
| 08 | Subcontractor Compliance & Onboarding | [`08-subcontractor-compliance-and-onboarding.md`](08-subcontractor-compliance-and-onboarding.md) | Office & Compliance Coordinator | ~10 | Draft |

### Finance

| # | Workflow | File | Target owner | Current h/mo | Status |
|---|---|---|---|---|---|
| 09 | Accounts Payable | [`09-accounts-payable.md`](09-accounts-payable.md) | Finance Director | **~80** | Draft |
| 10 | Accounts Receivable | [`10-accounts-receivable.md`](10-accounts-receivable.md) | Finance Director | ~25 | Draft |
| 11 | Cashflow & Management Reporting | [`11-cashflow-and-management-reporting.md`](11-cashflow-and-management-reporting.md) | Finance Director | ~25 | Draft |
| 12 | Payroll | [`12-payroll.md`](12-payroll.md) | Finance Director | ~10 | Draft |
| 13 | Accounts Inbox Triage | [`13-accounts-inbox-triage.md`](13-accounts-inbox-triage.md) | Finance Director | ~60 | Draft |

### Operations & comms

| # | Workflow | File | Target owner | Current h/mo | Status |
|---|---|---|---|---|---|
| 14 | Client & Reactive Comms | [`14-client-and-reactive-comms.md`](14-client-and-reactive-comms.md) | Office & Compliance Coordinator | ~20 | Draft |
| 15 | Materials & Deliveries | [`15-materials-and-deliveries.md`](15-materials-and-deliveries.md) | Office & Compliance Coordinator | ~20 | Draft |

### People, systems & support

| # | Workflow | File | Target owner | Current h/mo | Status |
|---|---|---|---|---|---|
| 16 | HR, Onboarding & IT Access | [`16-hr-onboarding-and-it-access.md`](16-hr-onboarding-and-it-access.md) | Office & Compliance Coordinator / FD | ~10 | Draft |
| 17 | IT & Systems Administration | [`17-it-and-systems-administration.md`](17-it-and-systems-administration.md) | Outsourced IT Helpdesk (target) | ~50 | Draft |
| 18 | Compliance, Insurance & Accreditation | [`18-compliance-insurance-accreditation.md`](18-compliance-insurance-accreditation.md) | Office & Compliance Coordinator | ~5 | Draft |
| 19 | Fleet Administration | [`19-fleet-administration.md`](19-fleet-administration.md) | Office & Compliance Coordinator | ~3 | Draft |
| 20 | Marketing & Brand | [`20-marketing-and-brand.md`](20-marketing-and-brand.md) | Brand & Content | ~20 | Draft |
| 21 | Document Management & Filing | [`21-document-management.md`](21-document-management.md) | Office & Compliance Coordinator | ~10 | Draft |

---

## Phased delivery

Original phasing from the JBB workflow audit, with the two workflows added on the 2026-05-20 coverage-audit-and-additions note slotted in:

1. **Phase 1 — Finance ROI:** 09, 10, 11, 13. Highest current-hour cost; primary pain-point anchor (workflow 11) sits here.
2. **Phase 2 — JPMS project-lifecycle core:** 03, 04, 01, 05, 06. **Workflow 22 (Timesheet Management)** joins phase 2 because it feeds 05 (valuations), 09 (AP for subcontractor day-rate), 11 (cashflow forward commitment) and 12 (payroll).
3. **Phase 3 — Everything else:** 02, 07, 08, 12, 14, 15, 16, 17, 18, 19, 20, 21. **Workflow 23 (Project Completion Settlement & VAT Analysis)** joins phase 3 because it depends on workflows 03, 05, 09, 22 being in place first.

This phasing is mirrored in root [`README.md`](../../README.md#116-roadmap-rough) Section 11.6.
