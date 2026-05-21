# Meeting: JPMS scope refinement — what's in, what's out, why

**Date:** 2026-05-21
**Attendees:** Nigel Reilly _(project lead)_

---

## Why this note exists

The 2026-05-20 audit ingested the JBB operational workflow document end-to-end and turned every task into a workflow. That was too broad. **JPMS is a project management system.** It exists to run projects — drawings → tender → BoQ → procurement → site reporting → valuations → close-out — and to produce two key outputs from that data: the **cashflow forecast** and the **Programme Valuation Report**.

Everything else JBB does — back-office accountancy, payroll, internal HR, IT administration, office facilities, marketing, fleet — is **outside JPMS scope**. Some of it (Xero, Dext, Brightpay, Chaser HQ) consumes JPMS data downstream; that doesn't make those systems integrations *into* JPMS, and it doesn't make those tasks JPMS workflows.

---

## The scope rule

A task belongs in JPMS if and only if at least one of these is true:

1. It captures or modifies project data inside JPMS (drawings, BoQ, work orders, RFIs, variations, site reports, defects, timesheets-against-cost-codes).
2. It produces a project output JPMS is responsible for (Programme Valuation Report, cashflow forecast, settlement record, VAT analysis).
3. It gates a project decision inside JPMS (subcontractor compliance / CIS verification before award).

If a task lives entirely in an external accountancy system, or is back-office facilities work, it is out of scope.

---

## Decisions

| # | Decision | Date |
|---|---|---|
| D1 | The JPMS workflow set is reduced from 23 to **11 workflows** matching the scope rule above. | 2026-05-21 |
| D2 | Twelve out-of-scope workflow files (former Accounts Payable, Accounts Receivable, Payroll, Accounts Inbox Triage, Client & Reactive Comms, Materials & Deliveries, HR/Onboarding/IT Access, IT & Systems Admin, Compliance/Insurance/Accreditation, Fleet, Marketing & Brand, Document Management) are deleted from `/docs/workflows/`. The coverage doc records they were considered and ruled out. | 2026-05-21 |
| D3 | The persona register is reduced from nine to **seven**. The former Brand & Content and Outsourced IT Helpdesk personas are removed — both only owned out-of-scope workflows. Internal HR and back-office IT work is not part of JPMS. Office & Compliance Coordinator stays, scoped down to subcontractor compliance only. The two executive personas are kept (Finance Director, Directors / MD) and become P06 and P07 in the renumbered register. | 2026-05-21 |
| D4 | Workflows are renumbered **01–11** with no gaps. The former Timesheet workflow becomes 09; the former Cashflow workflow becomes 10; the former Project Completion Settlement & VAT workflow becomes 11. | 2026-05-21 |
| D5 | **Workflow 09 (Timesheets) is reframed.** Purpose is to capture site team timesheets, allocate them to cost codes, and feed completion-% prediction. JPMS does **not** run payroll. Brightpay is downstream (out of JPMS). Subcontractor day-rate entries are captured for invoice reconciliation by accountancy downstream — JPMS does not pay subcontractors. | 2026-05-21 |
| D6 | **Workflow 10 (Cashflow & Project Forecasting) is reframed.** Produced purely from JPMS project data — work-order commitments, expected valuations per Claim Period, predicted completion %s. No dependency on Xero / Brightpay / Dext for input. Accountancy consumes the forecast downstream. | 2026-05-21 |
| D7 | The integrations page is rewritten into three sections: (1) Inputs into JPMS — OAuth providers, Bluebeam, monitored inboxes, HMRC CIS; (2) What JPMS replaces for project management — MS Project, Buildertrend, Planyard, Monday.com, Dashpivot, RAMsApp, WhatsApp operational use, various Excel/Word artefacts; (3) Downstream consumers — accountancy tooling (Xero, Dext, Brightpay, Chaser HQ, online banking, HMRC) listed once as systems that draw on JPMS data, not as integrations into it. | 2026-05-21 |
| D8 | The 2026-05-20 audit and coverage notes are kept as historical record. Where they reference the broader workflow set, this 2026-05-21 note supersedes them on scope. | 2026-05-21 |

---

## The 11 in-scope workflows

| # | Workflow | Why it's in JPMS |
|---|---|---|
| 01 | Drawing Receipt & Distribution | Captures and version-controls project drawings. |
| 02 | Pre-Construction: Tender & BoQ | Builds the priced BoQ that the rest of the project hangs off. |
| 03 | Subcontractor Procurement (Bid → Award) | Produces work orders that drive cost and programme. |
| 04 | Variations, RFIs & Delays | The change layer on a live project. Feeds programme and valuations. |
| 05 | Programme & Valuations | Produces the **Programme Valuation Report** per Claim Period — one of the two key outputs. |
| 06 | Site Reporting & Progress | Captures site reality (progress, photos, snags). Feeds completion %s for valuations and cashflow. |
| 07 | Project Close-Out & Defects | Snag register and final defect sign-off; gates retention release. |
| 08 | Subcontractor Compliance & Onboarding | Gates which subcontractors can be awarded work (insurance, RAMS, CIS). |
| 09 | Timesheet Management (cost-code-aware) | Site timesheets allocated to cost codes; cost-code budget hard-block; feeds completion-% prediction. |
| 10 | Cashflow & Project Forecasting | The **cashflow forecast** — one of the two key outputs. Built purely from JPMS data. |
| 11 | Project Completion Settlement & VAT Analysis | Client-facing close: cost-code settlement and zero-rated VAT analysis agreed with client. |

---

## The 12 out-of-scope workflows (deleted from /docs/workflows/)

| Former workflow | Why it's out |
|---|---|
| Accounts Payable | Pure accountancy. Xero / Dext / online banking. Consumes JPMS work-order and CIS data downstream, but the AP workflow itself is not JPMS. |
| Accounts Receivable | Accountancy. Xero issues invoices from JPMS valuation data downstream. The chasing flow is not JPMS. |
| Payroll | Internal staff payroll via Brightpay. JPMS does not handle staff payments. Subcontractor payment is via AP (accountancy, downstream). |
| Accounts Inbox Triage | Finance correspondence triage. Not project management. |
| Client & Reactive Comms | Redundant if project information is captured in JPMS in the right place originally. |
| Materials & Deliveries | Office and site procurement is not the project-delivery-through-subcontractors flow. Subcontractor materials sit inside their work order. |
| HR, Onboarding & IT Access | Internal HR. JPMS does not handle staff onboarding. |
| IT & Systems Administration | Back-office IT support. Not JPMS. |
| Compliance, Insurance & Accreditation | Corporate compliance, not project compliance. Subcontractor compliance is workflow 08. |
| Fleet Administration | Facilities. Not JPMS. |
| Marketing & Brand | Adjacent. Not JPMS. The JewelBB brand voice has its own dedicated skill outside the repo. |
| Document Management & Filing | Replaced *by* JPMS rather than a JPMS workflow itself. Project documents live in JPMS as a consequence of the project-lifecycle workflows. |

---

## Persona changes

- **Dropped** — the former Brand & Content persona (only owned Marketing & Brand workflow), and the former Outsourced IT Helpdesk persona (only owned the IT support / provisioning workflows).
- **Renumbered** — the executive personas are now P06 Finance Director and P07 Directors / MD (previously sat at higher numbers because of the brand/IT entries between them).
- **Reduced scope** — P04 Office & Compliance Coordinator now owns only workflow 08 (subcontractor compliance & onboarding). Their wider operational role (fleet, comms, materials, HR, document filing) is out of JPMS scope.
- **Reduced scope** — P06 Finance Director's role inside JPMS is the cashflow forecast (10) and the project settlement / VAT analysis (11). Their accountancy day-job (AP, AR, payroll, inbox triage) runs in Xero / Dext / Brightpay / Chaser HQ — not in JPMS.

The final persona register is **P01 Architect, P02 Subcontractor, P03 Project & Commercial Lead, P04 Office & Compliance Coordinator, P05 Site Team, P06 Finance Director, P07 Directors / MD**.

---

## Open questions

- [ ] Cashflow forecast horizon and refresh cadence now it's a pure JPMS output — confirm with the FD.
- [ ] Completion-% prediction model — purely site-reported %, or also derived from approved timesheets vs budget? Affects workflow 09 + 10 interactions.
- [ ] Subcontractor day-rate timesheet capture — required for AP reconciliation downstream; confirm the data shape JPMS publishes for accountancy to consume.

---

## Artefacts updated

- Deleted twelve out-of-scope workflow files and three out-of-scope journey files.
- Renamed the surviving timesheet / cashflow / settlement workflows and the cashflow journey to clean 09 / 10 / 11 / 10a slots.
- Reframed workflows 09 (timesheet) and 10 (cashflow) to match the scope rule.
- Rewrote `/docs/requirements/personas.md`, `permission-matrix.md`, `integrations.md`, `/docs/data-models/entity-relationship.md`, `/docs/requirements/automation-task-coverage.md`, root `README.md`, and the folder READMEs to reflect the trimmed scope.
