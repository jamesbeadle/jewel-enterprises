# Meeting: JBB workflow audit — 21 operational workflows captured

**Date:** 2026-05-20
**Location:** Remote — async review of uploaded audit document
**Attendees:** Nigel Reilly _(project lead)_, JBB Management _(authors of `JBB_Workflow_Maps.docx`)_

---

## Agenda

1. Receive the JBB workflow-audit document (twenty-one workflows) into the scoping repo.
2. Decide where each workflow lives in the repo (workflow map vs. user-journey slice).
3. Surface the system roles implied by the audit and reconcile against the existing five personas.
4. Surface the business entities and adjacent systems implied by the audit.
5. Update the JPMS roadmap to reflect the priority order recommended by the audit.

---

## Notes

- Source document: `JBB_Workflow_Maps.docx` — "Operational Workflow Maps · Twenty-one workflows for the JPMS scope and adjacent systems · Prepared for: Management · May 2026". Uploaded into this session by SmokingLabs (Nigel Reilly).
- The audit groups workflows into five families:
  - **Project lifecycle** (01–07): Drawing receipt; pre-construction tender & BoQ; subcontractor procurement; variations/RFIs/delays; programme & valuations; site reporting; close-out & defects.
  - **Supplier & subcontractor management** (08): Compliance & onboarding.
  - **Finance** (09–13): AP; AR; cashflow & management reporting; payroll; accounts inbox triage.
  - **Operations & comms** (14–15): Client/reactive comms; materials & deliveries.
  - **People, systems & support** (16–21): HR/onboarding/IT access; IT & systems admin; compliance/insurance/accreditation; fleet; marketing & brand; document management.
- Heaviest current monthly hours are in finance — AP (~80h), Accounts inbox triage (~60h), IT support (~50h) — and in pre-construction BoQ (~50h) and subcontractor procurement (~35h). These dominate JPMS phase-one ROI.
- The audit names operational roles that go beyond the five personas captured on 2026-05-18:
  - **Project & Commercial Lead** (PM + commercial responsibilities — owns workflows 02, 03, 04, 05, 07; reviewer on 06).
  - **Office & Compliance Coordinator** (owns workflows 08 oversight, 14, 15, 18, 19; co-owner on 16).
  - **Site Team / Site Manager** (capture role on 06, 12, 15).
  - **Finance Director (FD)** (owns workflows 09, 10, 11, 12, 13 approvals; current owner of 17 IT support).
  - **Brand & Content** (owns workflow 20).
  - **Directors / MD** (sign-off across 11, 16, 20).
  - **Outsourced IT helpdesk** (target owner for tier-1 of workflow 17).
  - **Subcontractor self-service** (target user on 03, 07, 08).
  - **Architect / CA / Client portal user** (target external on 04, 05, 06).
- The persona register is normalised to nine canonical roles with no duplication: **P01 Architect, P02 Subcontractor, P03 Project & Commercial Lead, P04 Office & Compliance Coordinator, P05 Site Team, P06 Brand & Content, P07 Finance Director, P08 Directors / MD, P09 Outsourced IT Helpdesk.** Internal QS work is owned by P03 Project & Commercial Lead; external QS consultants are treated as invited contacts rather than a separate persona. The earlier "Accountant" and "MD" working names are superseded by P07 Finance Director and P08 Directors / MD respectively.
- Adjacent systems named across the audit, in alphabetical order: 1Password, Amazon, Bluebeam, Brightpay, Buildertrend, Canva, Chaser HQ, Dashpivot, Defender, Dext, Dwellant, Entra, HMRC CIS, Intune, LinkedIn, M365 Admin, Meta Business, Monday.com, Onetrace, OneDrive, Outlook, Paperstone, Planyard, RAMsApp, SharePoint, Teams, TfL/council portals, Vantify, WhatsApp, Xero, online banking. These feed the new `/docs/requirements/integrations.md` catalogue.
- Entities surfaced beyond the original ten (Tender, Line Item, RFI, VO, Cash Call, Cost Code, Timesheet, Project, Drawing, Cashflow Forecast): **Drawing Revision, Bill of Quantities (BoQ), Rate / Rate Library entry, Bid Package, Quote, Work Order, Notice of Delay (NoD), Programme Task, Valuation, Site Report, Defect, Subcontractor, Compliance Document, Supplier Invoice, Sales Invoice, Payment Run, Procurement Request, Vehicle, Fine, Compliance Policy / Accreditation, Onboarding Event, Inbox Message Classification, Content Item.**

---

## Decisions

| # | Decision | Owner | Date |
|---|---|---|---|
| D1 | The twenty-one workflows are captured **once** as process maps under `/docs/workflows/NN-{slug}.md`. Where a single actor's slice through a workflow is non-trivial, a derived user-journey file is created under `/docs/user-journeys/` and cross-links to its source workflow. | Nigel | 2026-05-20 |
| D2 | Each workflow file follows the structure of the audit: purpose, trigger, frequency, owner, current monthly hours, current state, target flow, JPMS functionality required, integrations, acceptance criteria. The audit becomes acceptance criteria once each owner confirms. | Nigel | 2026-05-20 |
| D3 | The persona register at `/docs/requirements/personas.md` is normalised to nine canonical roles (P01–P09) with no duplication. Internal QS work is owned by P03 Project & Commercial Lead; external QS consultants are treated as invited contacts. The earlier "Accountant" and "MD" working names are superseded by P07 Finance Director and P08 Directors / MD. | Nigel | 2026-05-20 |
| D4 | A draft Role × Workflow RBAC matrix is created at `/docs/requirements/permission-matrix.md`. The nine personas form the columns; the twenty-one workflows form the rows. Matrix granularity is coarse (`O` = owner, `C` = contributor, `A` = approver, `R` = read, `—` = no access) until each workflow's journey deep-dive refines it. | Nigel | 2026-05-20 |
| D5 | The integrations catalogue at `/docs/requirements/integrations.md` is the canonical list of adjacent systems for the JPMS programme. Every workflow file references integrations by name only; the catalogue is the single source of truth for direction (read/write), workflows that touch it, and target status (keep / replace / archive). | Nigel | 2026-05-20 |
| D6 | The JPMS roadmap in root `README.md` Section 11.6 adopts the audit's recommended order: finance workflows 09, 10, 11, 13 first; project lifecycle 03, 04, 01, 05, 06 second; everything else third. The Accountant's cashflow-forecast journey from the 2026-05-18 discovery sits inside workflow 11 and remains the primary pain-point anchor. | Nigel | 2026-05-20 |
| D7 | Entities surfaced by the audit are added to root `README.md` Section 7 (schemas still `to be created`) and to a new `/docs/data-models/entity-relationship.md` Mermaid ERD. Schemas are written workflow-by-workflow as each one moves from Draft → In Review. | Nigel | 2026-05-20 |

---

## Action items

- [ ] Walk each workflow file with the named operational owner and tick the confirmation checklist on it — **Nigel**, due rolling
- [ ] Validate the new operational role names with the JBB management team (specifically "Project & Commercial Lead" and "Office & Compliance Coordinator" — confirm titles) — **Nigel**, due tbd
- [ ] Reconcile workflow 11 (Cashflow & Management Reporting) with the Accountant's primary-pain-point cashflow-forecast journey from 2026-05-18 — they describe the same need from different angles — **Nigel**, due before the next finance session
- [ ] Confirm which integrations in the catalogue are **keep**, **replace**, or **archive** under JPMS phase one — **Nigel + FD**, due tbd
- [ ] Sequence the workflows for delivery phases (phase 1 / phase 2 / out-of-scope) and reflect in the root README progress section — **Nigel**, due after action 1 above

---

## Open questions raised

- [ ] Are "Project & Commercial Lead" and "Project Manager" the same role, or two distinct roles? Audit uses the combined term; on-site practice may differ.
- [ ] Are there cases where an **external QS consultant** needs a richer JPMS surface than a standard invited contact? If yes, revisit whether QS should be promoted back to a persona.
- [ ] How many "Project & Commercial Leads" exist concurrently? Affects multi-project routing rules.
- [ ] What is the boundary between JBB's three entities (BB / PS / PFP)? The audit references all three for finance flows; we need an org-structure entity in the data model.
- [ ] Subcontractor portal — single sign-on with which identity provider? Affects workflow 03, 06, 07, 08.
- [ ] Architect/CA external access — branded portal, or just email-with-link? Affects workflow 04, 05, 06.
- [ ] Workflow 20 (Marketing & Brand) is flagged "mostly outside JPMS" — confirm whether any of it should integrate (the project consent flag at minimum).
- [ ] Where does retention-money tracking belong? Audit references it in close-out (#07) and AP (#09) but doesn't make it a first-class entity.

---

## Artefacts updated

- Created `/docs/workflows/01-drawing-receipt.md` through `/docs/workflows/21-document-management.md` — twenty-one workflow files.
- Updated `/docs/workflows/README.md` — replaced placeholder index with the five-group workflow table.
- Created derived user-journey files under `/docs/user-journeys/` for the actor slices that benefit from a separate walk-through (notably 09a Accountant cashflow forecast, 03a Subcontractor quote return, 06a Site team daily progress capture, 09b FD payment-run approval, 04a Architect RFI response, 08a Subcontractor compliance upload, 16a Starter day-one onboarding).
- Updated `/docs/user-journeys/README.md` index with the new journey rows.
- Updated `/docs/requirements/personas.md` — normalised to nine canonical cards (P01 Architect, P02 Subcontractor, P03 Project & Commercial Lead, P04 Office & Compliance Coordinator, P05 Site Team, P06 Brand & Content, P07 Finance Director, P08 Directors / MD, P09 Outsourced IT Helpdesk). All draft status.
- Created `/docs/requirements/permission-matrix.md` — coarse Role × Workflow RBAC matrix per D4.
- Created `/docs/requirements/integrations.md` — catalogue of adjacent systems per D5.
- Created `/docs/data-models/entity-relationship.md` — Mermaid ERD covering the surfaced entities per D7.
- Updated root `README.md` — populated Section 6 workflows/journeys table, expanded Section 7 entities table, ticked Section 4 progress items now satisfied by the audit, re-ordered Section 11.6 JPMS roadmap per D6, added quick-links to the new artefacts.
- Updated `/docs/requirements/README.md` — `permission-matrix.md` and `integrations.md` removed from "to be created" list.
- Updated `/docs/data-models/README.md` — `entity-relationship.md` removed from "to be created" list; entity index populated with the surfaced entities.
