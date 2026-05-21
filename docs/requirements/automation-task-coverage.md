# Automation Task Coverage Audit

Maps every "Automated Tasks" row in `Jewel Task Analysis (1).xlsx` against the JPMS scope decision from 2026-05-21.

**Source spreadsheet:** `Jewel Task Analysis (1).xlsx` — *Automated Tasks* tab (rows 4–50). Column A holds Nigel's automation notes; column E names the staff member; column F names the task.

**Status:** Draft.

---

## How to read this audit

Each spreadsheet task falls into one of three buckets:

- **IN** — the task is captured by a JPMS workflow (01–11). Listed with the workflow number.
- **DOWNSTREAM** — the task happens *after* JPMS. JPMS publishes the data; the accountancy or operations team picks it up in their own tools (Xero, Dext, Brightpay, Chaser HQ, online banking, HMRC). JPMS is not asked to do this task.
- **OUT** — back-office / governance / facilities / one-off. Not a JPMS workflow at all.

The scope rule (full text in [`/docs/meetings/2026-05-21-scope-refinement.md`](../meetings/2026-05-21-scope-refinement.md)):

> A task belongs in JPMS if and only if it captures or modifies project data, produces a project output JPMS is responsible for, or gates a project decision inside JPMS.

---

## Coverage table

| Row | Staff | Task (summary) | Scope | Notes |
|---|---|---|---|---|
| 4 | James Clark | Check emails every morning | **OUT** | General inbox triage. Not project management. |
| 5 | James Clark | Update to-do list from previous day | **OUT** | Personal task management. |
| 6 | James Clark | PDF drawings from emails, save, upload, print | **IN — workflow 01** | Drawing receipt and distribution. |
| 7 | James Clark | Update valuation Excel sheet | **IN — workflow 05** | Programme Valuation Report is a JPMS output. |
| 8 | James Clark | Update Excel programmes / MS Project | **IN — workflow 05** | Programme module in JPMS replaces MS Project. |
| 9 | James Clark | Email subcontractor quotes / bid packages | **IN — workflow 03** | Bid package builder and subcontractor portal. |
| 10 | James Clark | Work-order contracts in Buildertrend / Planyard | **IN — workflow 03** | Work-order generation on award. |
| 11 | James Clark | Contractor report (photos to PDF, email to CAs) | **IN — workflow 06** | Site reporting feeds the report assembly. |
| 12 | James Clark | Process supplier invoices and receipts in Dext | **DOWNSTREAM** | Accountancy task. Xero/Dext handle this. JPMS publishes work orders and approved timesheets for matching. |
| 13 | Chris Reeves | Bluebeam take-off of quants of materials and labour | **IN — workflow 02** | Bluebeam integration imports quants. |
| 14 | Chris Reeves | Research updated rates | **IN — workflow 02** | Rate library inside JPMS. |
| 15 | Chris Reeves | Create Variation Order Records (VORs) | **IN — workflow 04** | Project change register. |
| 16 | Chris Reeves | Send out bid packages | **IN — workflow 03** | Bid package builder. |
| 17 | Chris Reeves | Create tender document folders per trade bid package | **IN — workflow 03** | Auto-assembly of bid documents. |
| 18 | Chris Reeves | Compare submitted tenders from subcontractors | **IN — workflow 03** | Comparison and award workflow. |
| 19 | Chris Reeves | Create defect schedules for completed product | **IN — workflow 07** | Defect register. |
| 20 | Chris Reeves | Retender current projects to confirm quants & rates | **IN — workflow 02** | Re-tender comparison view. |
| 21 | Chris Reeves | Use Bluebeam-formatted quants to create a completed take-off | **IN — workflow 02** | Workflow 02 take-off flow. |
| 22 | Chris Reeves | Review documents produced by AI | **OUT** | Governance / quality assurance, cross-cutting. |
| 23 | Chris Reeves | Review M&E drawings to create BoQs | **IN — workflow 02** | M&E discipline tagging in the BoQ module. |
| 24 | Chris Reeves | Complete AI audit template & weekly reports | **OUT** | Governance / meta. |
| 25 | Chris Reeves | Reviewing workflows of JBB | **OUT** | This scoping work itself; not a JPMS workflow. |
| 26 | Jeremy Ferendinos | Raise sales invoices for Jewel BB | **DOWNSTREAM** | Xero raises the invoice. JPMS publishes the approved valuation that drives it. |
| 27 | Jeremy Ferendinos | Reconcile supplier statements; identify missing invoices | **DOWNSTREAM** | Accountancy in Xero. |
| 28 | Jeremy Ferendinos | Read accounts inbox in BB / PS / PFP | **OUT** | Accountancy inbox triage. |
| 29 | Jeremy Ferendinos | Maintain shared accounts inbox triage | **OUT** | Same as above. |
| 30 | Jeremy Ferendinos | Chasing outstanding invoices JBB / JPS / JPFP | **DOWNSTREAM** | Chaser HQ on Xero data. |
| 31 | Jeremy Ferendinos | Chase outstanding sales invoices and agree payment dates | **DOWNSTREAM** | Chaser HQ on Xero data. |
| 32 | Jeremy Ferendinos | Track in-house costs and cross-charge costs between entities / projects | **IN — workflow 10** | Cashflow forecast carries the cross-entity flag at source. |
| 33 | Jeremy Ferendinos | Match supplier invoices to work order / project | **DOWNSTREAM** | Xero AP matching against JPMS-published work orders. |
| 34 | Jeremy Ferendinos | Track subcontractor corrected invoices and chase amendments | **DOWNSTREAM** | Accountancy task. Uses JPMS work-order data downstream. |
| 35 | Sofia | Categorising / organising folders | **OUT** | Folder housekeeping. JPMS owns project documents natively as a consequence of the project-lifecycle workflows; no separate workflow needed. |
| 36 | Sarah Collins | Send RAMS to client for approval | **IN — workflow 08** | RAMS template engine. |
| 37 | Sarah Collins | Draft RAMS for new won project | **IN — workflow 08** | RAMS template engine. |
| 38 | Sarah Collins | Forward H&S bulletin to directors for client sharing | **OUT** | Operations / comms. |
| 39 | Sarah Collins | Check info inbox for new enquiries | **OUT** | Front-of-house inbox triage. |
| 40 | Sarah Collins | Reply to Nigel on AI ethical/legal process chain | **OUT** | One-off project task. |
| 41 | Sarah Collins | Assign weekly Toolbox Talk on professional etiquette / handling abuse | **OUT** | H&S comms. Not JPMS. |
| 42 | Sarah Collins | Contact operatives to confirm uniform / PPE requirements | **OUT** | H&S / facilities. |
| 43 | Sarah Collins | AI meeting | **OUT** | Meta. |
| 44 | Sarah Collins | Review update emails from Akeva (courses, recalls, H&S case law) | **OUT** | Compliance knowledge upkeep. |
| 45 | Sarah Collins | Review IT/AI/Cybersecurity policy, GDPR booklet, took quiz | **OUT** | Internal governance. |
| 46 | Katie-Louise Hicks | Organise & monitor subcontractor attendance; put into MD calendar | **IN — workflow 06** | Attendance check-in in the site app. |
| 47 | Katie-Louise Hicks | Raise WOs for subcontractors, chase progress, save documentation | **IN — workflow 03 + 06** | WO generation; progress chase via site reporting. |
| 48 | Katie-Louise Hicks | Monitor subcontractor insurance / certs / tickets and chase renewals | **IN — workflow 08** | Compliance register with reminders. |
| 49 | Katie-Louise Hicks | Maintain subcontractor details and documents | **IN — workflow 08** | Subcontractor master record. |
| 50 | Katie-Louise Hicks | Provide updates to clients on access, progress and issues | **OUT** | Client comms is operational; the client gets the live project dashboard from JPMS (workflow 06 + 10) which removes most of the chasing. |

---

## Summary

| Bucket | Count | What it means |
|---|---|---|
| IN — JPMS workflow | 22 of 47 | Covered by one of the 11 JPMS workflows. |
| DOWNSTREAM — JPMS data published, accountancy consumes | 7 of 47 | Not a JPMS workflow but JPMS publishes the underlying data. |
| OUT — back-office / governance / facilities / one-off | 18 of 47 | Not in JPMS scope. Stays where it is. |

---

## Persona coverage

Every IN-scope task's actor maps onto a persona in P01–P07. No new personas surfaced. See [`personas.md`](personas.md).
