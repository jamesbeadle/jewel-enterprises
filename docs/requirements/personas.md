# Personas

Seven canonical user roles for **JPMS**. Each card stands alone — there is one source of truth per role.

Each card stays **Draft** until JPMS is far enough along to validate it against real usage. The card template lives in [`_templates/personas-template.md`](_templates/personas-template.md). The Role × Workflow RBAC matrix lives in [`permission-matrix.md`](permission-matrix.md).

### Persona register

| # | Persona | Type |
|---|---|---|
| P01 | Architect | External client |
| P02 | Subcontractor | External delivery partner |
| P03 | Project & Commercial Lead | Internal |
| P04 | Office & Compliance Coordinator | Internal |
| P05 | Site Team | Internal field |
| P06 | Finance Director (FD) | Internal executive |
| P07 | Directors / MD | Internal executive |

> Scope note (2026-05-21): the earlier P06 Brand & Content and P09 Outsourced IT Helpdesk personas have been removed — both only owned workflows that fell outside JPMS scope (marketing, IT administration). Internal HR and back-office IT work is not part of JPMS.

---

## P01 — Architect

**Role:** External client architect commissioning work from Jewel Enterprises
**Reports to:** Their own firm / their own client
**Tooling today:** Architecture / CAD software, email, drawing packages, client portals
**Frequency on platform:** Periodic — at tender submission and during VO discussions
**Status:** Draft

### Goals
- Get work delivered on time and to specification.
- Track their cost codes accurately through to billing.
- Be informed of Variation Orders before they affect timeline or cost.
- Agree the zero-rated VAT outcome at project close cleanly.

### Pain points (current state)
- No shared system; documents passed via email or portal handoffs.
- Visibility into on-site progress is opaque.
- VAT outcome agreement happens by email chain at project end.

### Involvement across workflows
- **Approver on:** 04 (RFI replies and variation sign-off), **11 (agrees the zero-rated VAT analysis at project close)**.
- **Source / contributor on:** 01 (issues drawings), 02 (issues tender).
- **Read on:** 05 (receives Programme Valuation Report each Claim Period), 06 (live site dashboard, scoped), 07 (close-out pack), 09 (client portal — approved timesheet totals per cost code, where the contract provides), 10 (cashflow / project status dashboard, scoped).

### Permissions needed (coarse)
- Read on their own tenders and projects, including the live site dashboard, Programme Valuation Report, close-out pack, and — where the contract provides — timesheet totals per cost code.
- Write on tender submission.
- Approve / reject VOs on their work; approve the zero-rated VAT analysis at project close.

### Devices & environment
- Desktop primarily. May use tablet on site visits.

### Notes
- The architect's client-facing **cost codes** must be referenced consistently through every screen and report that touches that architect's tender.
- Where Jewel works with an external Quantity Surveyor (consultant), they are treated as an invited contact rather than a separate persona — internal QS work is owned by P03 Project & Commercial Lead.

---

## P02 — Subcontractor

**Role:** Field delivery — performs the work on site against tender line items
**Reports to:** Project & Commercial Lead (commercially); Site Team (operationally on site)
**Tooling today:** Phone, paper, ad-hoc messaging
**Frequency on platform:** Daily — on site
**Status:** Draft

### Goals
- Bid for new work without learning a new tool.
- Log progress and time on site quickly.
- Get RFIs answered fast so work isn't blocked.
- Keep compliance documents current without being chased.

### Pain points (current state)
- Paper timesheets get lost or delayed.
- RFI handling is slow and untracked.
- No personal view of progress against the tender.
- Compliance documentation scattered across systems; chased manually before expiry.

### Involvement across workflows
- **Contributor / source on:** 03 (returns quote against a bid package), 04 (raises RFIs from site), 06 (attendance check-in and photo capture), 07 (defect resolution with photo evidence), 08 (self-service upload of compliance documents), 09 (day-rate timesheet entries where applicable, used downstream by accountancy for invoice reconciliation).
- **Read on:** 01 (assigned drawings at current revision, on mobile).

### Permissions needed (coarse)
- Read drawings and specs for their assigned work.
- Write own quote against bid packages they've been invited to.
- Update completion on line items assigned to them; raise RFIs; submit day-rate timesheet entries.
- Self-service upload of own compliance documents.

### Devices & environment
- Mobile phone primarily. Touch-friendly UI is essential, including in poor connectivity (offline-tolerant fields where possible).

### Notes
- Subcontractors are external. Onboarding needs to be lightweight — typically an invite link rather than a full account-creation flow.
- JPMS does **not** pay subcontractors. Subcontractor invoices are paid by accountancy via AP (Xero) using JPMS work-order and timesheet data downstream.

---

## P03 — Project & Commercial Lead

**Role:** Internal project lead combining PM and commercial responsibilities — owns the project from BoQ through close-out, including the internal QS function
**Reports to:** Directors / MD
**Tooling today:** Bluebeam, MS Project, Excel BoQ, Buildertrend, Planyard, SharePoint, Outlook
**Frequency on platform:** Daily
**Status:** Draft

### Goals
- Run the project programme and valuation cycle cleanly without manual reconciliation between MS Project and Excel.
- Make award decisions on bids and variations with side-by-side data, not assembled spreadsheets.
- Price tenders accurately into line items and keep them in sync as scope changes via VOs.
- Stop being the human bridge between drawings, BoQ, RFIs, and the programme.

### Pain points (current state)
- Line items live in spreadsheets disconnected from completion tracking and billing.
- Programme and valuation reconciliation is manual and slow.
- Three separate Excel logs for variations, RFIs and delay notices that don't talk to each other.
- Bid packages are assembled by hand from SharePoint / Outlook / Excel.
- Drawing supersedure relies on memory.

### Involvement across workflows
- **Owner on:** 02 (Tender & BoQ), 03 (Subcontractor Procurement — also award approver), 04 (Variations / RFIs / Delays), 05 (Programme & Valuations — also issuance approver), 07 (Close-Out & Defects — also defect sign-off), 09 (Timesheet Management — weekly batch approval).
- **Approver on:** 01 (drawing supersedure override), 06 (site report sign-off), 11 (commercial sign-off at project completion).
- **Contributor on:** 10 (triggers cashflow refresh on valuation / WO completion).
- **Read on:** 08 (subcontractor compliance status before award).

### Permissions needed (coarse)
- Owner on project records, BoQ, project change register, programme, valuations, defect register, timesheet approval.
- Approver on bids, variations, valuations, defect sign-off, drawing supersedure, site reports, project completion settlement.
- Read across all projects.

### Devices & environment
- Laptop primarily; tablet on site.

---

## P04 — Office & Compliance Coordinator

**Role:** Internal owner of subcontractor compliance and onboarding inside JPMS
**Reports to:** Directors / FD
**Tooling today:** Outlook, SharePoint, Monday.com, RAMsApp, phone system
**Frequency on platform:** Daily
**Status:** Draft

### Goals
- Keep every subcontractor's insurance, certifications, RAMS, and CIS status current and visible at the point of award.
- Stop chasing expiring documents by email.

### Pain points (current state)
- Subcontractor compliance scattered across Monday, SharePoint and Excel.
- Insurance / accreditation renewals tracked in Outlook calendar and chased manually.
- RAMS drafted per project in a separate tool.

### Involvement across workflows
- **Owner on:** 08 (Subcontractor Compliance & Onboarding).
- **Contributor on:** 03 (supplier directory upkeep at procurement time).

### Permissions needed (coarse)
- Owner on the subcontractor master record, compliance document register, and renewal reminders.
- Read on procurement so they can see who is being considered and check status before award.

### Devices & environment
- Desktop primarily.

### Notes
- The wider operational coordinator role at JBB (fleet, comms, materials, HR, document filing) is outside JPMS scope. JPMS covers the subcontractor-compliance slice that gates project award.

---

## P05 — Site Team

**Role:** Internal site managers, foremen, and operatives — the capture layer for site reality
**Reports to:** Project & Commercial Lead
**Tooling today:** WhatsApp groups, phone camera, paper notes, Dashpivot
**Frequency on platform:** Daily — on site
**Status:** Draft

### Goals
- Capture progress, photos, attendance and issues in seconds, not minutes.
- Get the right drawing on the phone without asking.
- Submit timesheets allocated to the right cost code, every day, without being chased.

### Pain points (current state)
- Site information lives across WhatsApp, Photos and inboxes.
- Attendance tracking is a separate Excel / Dashpivot / calendar.
- No personal view of progress against the BoQ.

### Involvement across workflows
- **Owner on:** 06 (Site Reporting & Progress — daily capture).
- **Contributor on:** 02 (walk-round notes / photos during pre-construction), 04 (raises RFIs from site), 05 (% progress per BoQ section feeds the programme), 07 (defect identification with photos), 09 (own timesheet capture allocated to cost codes).
- **Read on:** 01 (current drawing revision on mobile).

### Permissions needed (coarse)
- Write on site reports, photos, attendance, snags, RFI raises, own timesheets.
- Read drawings, BoQ sections, and current cost-code budgets on assigned projects.

### Devices & environment
- Phone primarily. Touch-friendly, offline-tolerant capture is essential.

---

## P06 — Finance Director (FD)

**Role:** Internal owner of the JPMS finance-output surface: the cashflow forecast and project completion settlement
**Reports to:** Directors / MD
**Tooling today:** Excel cashflow tracker; Xero / Dext / Brightpay / Chaser HQ for accountancy
**Frequency on platform:** Daily — heavy on the cashflow dashboard
**Status:** Draft

### Goals — **the platform's primary driver**
- Hold an accurate cashflow forecast across the project portfolio without rebuilding it weekly in Excel.
- Produce the zero-rated VAT analysis at project completion from project data rather than from scratch.
- Get a clean handover of project data into the accountancy systems they run downstream (Xero, Brightpay, etc.).

### Pain points (current state)
- Cashflow forecast accuracy depends on knowing real completion %, which is currently unreliable because line-item completion isn't tracked consistently.
- Cashflow rebuilt weekly in Excel from scattered sources; cross-entity charges manual.
- VAT analysis at project end is built from scratch in Excel.

### Involvement across workflows
- **Owner on:** 10 (Cashflow & Project Forecasting — produced from JPMS data), 11 (Project Completion Settlement & VAT — produces the draft zero-rated VAT analysis, triggers retention release).
- **Approver on:** 07 (retention release on close-out), 09 (cost-code overrun above threshold).
- **Read on:** 02, 03, 04, 05 (commercial visibility across project-side workflows), 08 (subcontractor compliance status — gates eventual payment in accountancy downstream).

### Permissions needed (coarse)
- Owner on the cashflow forecast and the project settlement / VAT analysis.
- Approver on cost-code overrun and retention release.
- Read across all projects so the forecast and settlement are well-informed.

### Devices & environment
- Desktop primary. Mobile for approvals.

### Notes
- The FD's day-job — AP, AR, payroll, inbox triage, statement reconciliation — runs in Xero / Dext / Brightpay / Chaser HQ. **That work is not a JPMS workflow.** JPMS publishes project data (work orders, valuations, approved timesheets, settlement records) that the accountancy systems consume downstream.

---

## P07 — Directors / MD

**Role:** Business owners — executive decisions across the project portfolio
**Reports to:** —
**Tooling today:** Email, Teams, reports from FD
**Frequency on platform:** Daily
**Status:** Draft

### Goals
- See cashflow position and project status independently, in real time.
- Sign-off on high-value commercial decisions in-system with audit trail.
- Identify at-risk projects early.

### Pain points (current state)
- Cashflow visibility lives in the FD's head until Excel is updated.
- Sign-offs scattered across email; no single audit trail.

### Involvement across workflows
- **Approver on:** 02 (tender sign-off), 03 (high-value award), 04 (high-value variations), 05 (valuations), 09 (cost-code overrun above threshold), 10 (strategic cashflow decisions), 11 (final VAT outcome agreed with client).
- **Read on:** 01, 06, 07, 08.

### Permissions needed (coarse)
- Read across all projects and outputs.
- Approve high-value commercial decisions and the final VAT outcome.
- Admin on users, roles, integrations.

### Devices & environment
- Mobile + desktop.
