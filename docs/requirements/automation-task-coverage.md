# Automation Task Coverage Audit

Maps every "Automated Tasks" row in `Jewel Task Analysis (1).xlsx` to the workflow and (where created) user journey that captures it inside JPMS. Confirms the actor's persona exists.


**Source spreadsheet:** `Jewel Task Analysis (1).xlsx` — *Automated Tasks* tab (rows 4–50). Column A holds Nigel's automation notes; column E names the staff member; column F names the task.

**Status:** Draft — rows confirmed as the named owners walk each workflow.

---

## Coverage status legend

- ✅ **Covered** — explicit workflow exists and captures the task.
- 🟡 **Partial** — workflow exists but the task surfaces a sub-step or rule that should be made explicit.
- 🆕 **New scope** — not in the original 21 workflows; added as part of this audit (workflow 22 / 23 / etc.).
- ⚪ **Out of operational scope** — meta / governance / one-off task that doesn't belong in an operational workflow.

---

## Coverage table

| Row | Staff | Task (summary) | Persona | Workflow(s) | Journey | Status |
|---|---|---|---|---|---|---|
| 4 | James Clark | Check emails every morning | P03 PCL | [13](../workflows/13-accounts-inbox-triage.md) + [14](../workflows/14-client-and-reactive-comms.md) | — | ✅ |
| 5 | James Clark | Update to-do list from previous day | P03 PCL | _(cross-cutting UI — JPMS task dashboard, not a workflow per se)_ | — | ⚪ |
| 6 | James Clark | PDF drawings from emails, save, upload, print | P03 PCL | [01](../workflows/01-drawing-receipt.md) | — | ✅ |
| 7 | James Clark | Update valuation Excel sheet | P03 PCL | [05](../workflows/05-programme-and-valuations.md) | — | ✅ |
| 8 | James Clark | Update Excel programmes / MS Project | P03 PCL | [05](../workflows/05-programme-and-valuations.md) | — | ✅ |
| 9 | James Clark | Email subcontractor quotes / bid packages | P03 PCL | [03](../workflows/03-subcontractor-procurement.md) | [03a](../user-journeys/03a-subcontractor-quote-return.md) | ✅ |
| 10 | James Clark | Work-order contracts in Buildertrend / Planyard | P03 PCL | [03](../workflows/03-subcontractor-procurement.md) | — | ✅ |
| 11 | James Clark | Contractor report (photos to PDF, email to CAs) | P03 PCL | [06](../workflows/06-site-reporting-and-progress.md) | [06a](../user-journeys/06a-site-team-daily-capture.md) | ✅ |
| 12 | James Clark | Process supplier invoices and receipts in Dext | P07 FD (review) / P02 Subcontractor (upload) | [09](../workflows/09-accounts-payable.md) | [09a](../user-journeys/09a-fd-ap-exception-review.md) | ✅ |
| 13 | Chris Reeves | Bluebeam take-off of quants of materials and labour | P03 PCL | [02](../workflows/02-preconstruction-tender-boq.md) | — | ✅ |
| 14 | Chris Reeves | Research updated rates | P03 PCL | [02](../workflows/02-preconstruction-tender-boq.md) | — | ✅ |
| 15 | Chris Reeves | Create Variation Order Records (VORs) | P03 PCL | [04](../workflows/04-variations-rfis-delays.md) | — | ✅ |
| 16 | Chris Reeves | Send out bid packages | P03 PCL | [03](../workflows/03-subcontractor-procurement.md) | [03a](../user-journeys/03a-subcontractor-quote-return.md) | ✅ |
| 17 | Chris Reeves | Create tender document folders per trade bid package | P03 PCL | [03](../workflows/03-subcontractor-procurement.md) + [21](../workflows/21-document-management.md) | — | ✅ |
| 18 | Chris Reeves | Compare submitted tenders from subcontractors | P03 PCL | [03](../workflows/03-subcontractor-procurement.md) | — | ✅ |
| 19 | Chris Reeves | Create defect schedules for completed product | P03 PCL | [07](../workflows/07-project-close-out-and-defects.md) | — | ✅ |
| 20 | Chris Reeves | Retender current projects to confirm quants & rates | P03 PCL | [02](../workflows/02-preconstruction-tender-boq.md) | — | ✅ (re-tender comparison view) |
| 21 | Chris Reeves | Use Bluebeam-formatted quants to create a completed take-off | P03 PCL | [02](../workflows/02-preconstruction-tender-boq.md) | — | ✅ |
| 22 | Chris Reeves | Review documents produced by AI | P03 PCL | _(cross-cutting exception/review queues across [09](../workflows/09-accounts-payable.md), [13](../workflows/13-accounts-inbox-triage.md), [03](../workflows/03-subcontractor-procurement.md))_ | — | 🟡 |
| 23 | Chris Reeves | Review M&E drawings to create BoQs | P03 PCL | [02](../workflows/02-preconstruction-tender-boq.md) (M&E discipline tagging) | — | ✅ |
| 24 | Chris Reeves | Complete AI audit template & weekly reports | P03 PCL | _(governance — outside operational workflows)_ | — | ⚪ |
| 25 | Chris Reeves | Reviewing workflows of JBB | P03 PCL | _(this scoping repo is the answer — meta)_ | — | ⚪ |
| 26 | Jeremy Ferendinos | Raise sales invoices for Jewel BB | P07 FD | [10](../workflows/10-accounts-receivable.md) | — | ✅ |
| 27 | Jeremy Ferendinos | Reconcile supplier statements; identify missing invoices | P07 FD | [09](../workflows/09-accounts-payable.md) (statement reconciliation engine) | — | ✅ (Nigel: deferred to later phase) |
| 28 | Jeremy Ferendinos | Read accounts inbox in BB / PS / PFP | P07 FD | [13](../workflows/13-accounts-inbox-triage.md) | [13a](../user-journeys/13a-fd-inbox-triage-exceptions.md) | ✅ (Nigel: keep human-in-the-loop for posting itself; classify only) |
| 29 | Jeremy Ferendinos | Maintain shared accounts inbox triage | P07 FD | [13](../workflows/13-accounts-inbox-triage.md) | [13a](../user-journeys/13a-fd-inbox-triage-exceptions.md) | ✅ |
| 30 | Jeremy Ferendinos | Chasing outstanding invoices JBB / JPS / JPFP | P07 FD | [09](../workflows/09-accounts-payable.md) (supplier side) / [10](../workflows/10-accounts-receivable.md) (customer side) | — | ✅ (Nigel: Chaser HQ + human-in-the-loop cross-entity) |
| 31 | Jeremy Ferendinos | Chase outstanding sales invoices and agree payment dates | P07 FD | [10](../workflows/10-accounts-receivable.md) | — | ✅ |
| 32 | Jeremy Ferendinos | Track in-house costs and cross-charge costs between entities / projects | P07 FD | [11](../workflows/11-cashflow-and-management-reporting.md) (cross-entity flag) | [11a](../user-journeys/11a-fd-cashflow-forecast.md) | ✅ |
| 33 | Jeremy Ferendinos | Match supplier invoices to work order / project | P07 FD | [09](../workflows/09-accounts-payable.md) (matching engine) | [09a](../user-journeys/09a-fd-ap-exception-review.md) | ✅ |
| 34 | Jeremy Ferendinos | Track subcontractor corrected invoices and chase amendments | P07 FD | [09](../workflows/09-accounts-payable.md) (subcontractor invoice validation) | [09a](../user-journeys/09a-fd-ap-exception-review.md) | ✅ |
| 35 | Sofia | Categorising / organising folders | P04 OCC | [21](../workflows/21-document-management.md) | — | ✅ |
| 36 | Sarah Collins | Send RAMS to client for approval | P04 OCC | [08](../workflows/08-subcontractor-compliance-and-onboarding.md) (RAMS template engine) | — | ✅ |
| 37 | Sarah Collins | Draft RAMS for new won project | P04 OCC | [08](../workflows/08-subcontractor-compliance-and-onboarding.md) | — | ✅ |
| 38 | Sarah Collins | Forward H&S bulletin to directors for client sharing | P04 OCC | [14](../workflows/14-client-and-reactive-comms.md) (client comms) / [20](../workflows/20-marketing-and-brand.md) (scheduled content) | — | 🟡 (Nigel: scheduled content distribution rule — add to 20 target flow) |
| 39 | Sarah Collins | Check info inbox for new enquiries | P04 OCC | [14](../workflows/14-client-and-reactive-comms.md) | — | ✅ |
| 40 | Sarah Collins | Reply to Nigel on AI ethical/legal process chain | P04 OCC | _(one-off project implementation task)_ | — | ⚪ |
| 41 | Sarah Collins | Assign weekly Toolbox Talk (TBT) on professional etiquette / handling abuse | P04 OCC | [18](../workflows/18-compliance-insurance-accreditation.md) (compliance comms) / [08](../workflows/08-subcontractor-compliance-and-onboarding.md) (subcontractor reminders) | — | 🟡 (Toolbox Talks not explicit yet — add to 18 / 08) |
| 42 | Sarah Collins | Contact operatives to confirm uniform / PPE requirements | P04 OCC | [08](../workflows/08-subcontractor-compliance-and-onboarding.md) (broad subcontractor notification) / [15](../workflows/15-materials-and-deliveries.md) (PPE procurement) | — | 🟡 (general subcontractor notification rule — add to 08) |
| 43 | Sarah Collins | AI meeting | P04 OCC | _(meta — temporary)_ | — | ⚪ |
| 44 | Sarah Collins | Review update emails from Akeva (courses, recalls, H&S case law) | P04 OCC | [18](../workflows/18-compliance-insurance-accreditation.md) | — | ✅ (Nigel: knowledge lives in LLM; manual review tapers) |
| 45 | Sarah Collins | Review IT/AI/Cybersecurity policy, GDPR booklet, took quiz | P04 OCC | [17](../workflows/17-it-and-systems-administration.md) (governance reminders) | — | 🟡 (policy review cadence — add to 17 governance reminders) |
| 46 | Katie-Louise Hicks | Organise & monitor subcontractor attendance; put into MD calendar | P04 OCC | [06](../workflows/06-site-reporting-and-progress.md) (attendance check-in) | [06a](../user-journeys/06a-site-team-daily-capture.md) | ✅ |
| 47 | Katie-Louise Hicks | Raise WOs for subcontractors, chase progress, save documentation | P04 OCC | [03](../workflows/03-subcontractor-procurement.md) (WO generation) + [06](../workflows/06-site-reporting-and-progress.md) (progress) | — | ✅ |
| 48 | Katie-Louise Hicks | Monitor subcontractor insurance / certs / tickets and chase renewals | P04 OCC | [08](../workflows/08-subcontractor-compliance-and-onboarding.md) | [08a](../user-journeys/08a-subcontractor-compliance-upload.md) | ✅ |
| 49 | Katie-Louise Hicks | Maintain subcontractor details and documents | P04 OCC | [08](../workflows/08-subcontractor-compliance-and-onboarding.md) | — | ✅ |
| 50 | Katie-Louise Hicks | Provide updates to clients on access, progress and issues | P04 OCC | [14](../workflows/14-client-and-reactive-comms.md) | — | ✅ |

---

## Persona coverage check

Every named task actor maps to an existing persona. No new personas surfaced.

| Staff member | Mapped persona | Notes |
|---|---|---|
| James Clark | P03 Project & Commercial Lead | Tasks span tender → bid → valuation → site reporting → AP triage. Confirms P03's broad scope. |
| Chris Reeves | P03 Project & Commercial Lead | Estimating / commercial slice of the same role. Confirms P03 absorbs internal QS work. |
| Jeremy Ferendinos | P07 Finance Director | All finance tasks fall to P07 as expected. |
| Sofia | P04 Office & Compliance Coordinator | Folder upkeep is residual after workflow 21 rollout. |
| Sarah Collins | P04 Office & Compliance Coordinator | Compliance + H&S + front-of-house bulletins all under P04. Surfaces TBT / PPE sub-needs (see partial coverage above). |
| Katie-Louise Hicks | P04 Office & Compliance Coordinator | Attendance + WOs + compliance dates + client updates all under P04. |

---

## Items added to backlog from this audit

The partial-coverage (🟡) rows above produce these follow-ups inside existing workflow files (no new workflows required for them):

1. **Workflow 18 / 08 — Toolbox Talks (TBT) reminders.** Add a weekly TBT distribution rule with topic catalogue and acknowledgement capture. Triggered by row 41.
2. **Workflow 08 — Broad subcontractor notifications.** Add a generic "notify all active subcontractors on this project" channel (uniform / PPE / TBT / one-off messages). Triggered by rows 41–42.
3. **Workflow 20 — Scheduled content distribution.** Add scheduling of "shareable bulletins" (industry H&S notices, etc.) to target groups. Triggered by row 38.
4. **Workflow 17 — Policy review cadence.** Add an annual review reminder for each governance policy (IT, AI, cybersecurity, GDPR). Triggered by row 45.
5. **Cross-cutting — AI exception / review queue UX.** Confirm in each workflow's exception queue (especially 09, 13, 03 award) that the review surface is consistent. Triggered by row 22.

---

## New workflows surfaced by this conversation (not in the spreadsheet)

Outside the 47 spreadsheet rows, Nigel's notes on 2026-05-20 added scope that warranted **new** workflow files:

| # | New workflow | Why |
|---|---|---|
| 22 | [Timesheet Management (cost-code-aware)](../workflows/22-timesheet-management.md) | Client-facing timesheet approval with cost-code allocation. Hard rule: cannot allocate to a cost code with no budget unless a work order is raised or allocation moves to a different cost code. |
| 23 | [Project Completion Settlement & VAT Analysis](../workflows/23-project-completion-settlement.md) | On project completion, settle all timesheet / cost-code allocations; perform zero-rated VAT analysis and agree with client. |

---

## Workflow extensions surfaced by Nigel's project-management outline

Nigel's hierarchical outline (New Project → Architect drawings → bid packages → approval/rejection; Project Change → Architect updated drawings / Client change / Site issues; Project Change Actions → VO → bid packages → approval/rejection → VO approved; RFI; NoD; Reports — Programme Valuation Report with Claim Values per Claim Period; VO List) maps mostly onto existing workflows. The following points were under-specified in the original audit and have been made explicit:

| Outline item | Current coverage | Action taken |
|---|---|---|
| New Project → Architect issues drawings | Workflow 01 (drawing receipt) | ✅ already covered. |
| Send work tender (bid packages) | Workflow 03 | ✅ already covered. |
| Subcontractor approval / rejection | Workflow 03 | ✅ already covered (comparison + award routing). |
| Project Change — Architect updated drawings / Client change / Site issues | Workflow 04 trigger | ✅ already in workflow 04 trigger. |
| Project Change Actions — **VO triggers a bid package loop** | Workflow 04 implied; not explicit | 🆕 Made explicit in workflow 04 (variation with subcontractor procurement). |
| VO approved → updates programme + valuation | Workflow 04 → 05 | ✅ already covered. |
| Reports — **Programme Valuation Report with Claim Values per Claim Period** | Workflow 05 (valuation generator) | 🆕 Made explicit: workflow 05 now names "Programme Valuation Report" and "Claim Period" as first-class concepts. |
| Reports — **Variation Orders list report** | Workflow 04 (register) | 🆕 Made explicit: workflow 04 now lists the VO List report as a deliverable of the register. |
