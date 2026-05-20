# Personas

Nine canonical user roles for **JPMS**. Each card stands alone — there is one source of truth per role.

Each card stays **Draft** until JPMS is far enough along to validate it against real usage. The card template lives in [`_templates/personas-template.md`](_templates/personas-template.md). The Role × Workflow RBAC matrix lives in [`permission-matrix.md`](permission-matrix.md).

### Persona register

| # | Persona | Type |
|---|---|---|
| P01 | Architect | External client |
| P02 | Subcontractor | External delivery partner |
| P03 | Project & Commercial Lead | Internal |
| P04 | Office & Compliance Coordinator | Internal |
| P05 | Site Team | Internal field |
| P06 | Brand & Content | Internal |
| P07 | Finance Director (FD) | Internal executive |
| P08 | Directors / MD | Internal executive |
| P09 | Outsourced IT Helpdesk | External service partner |

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

### Pain points (current state)
- No shared system; documents passed via email or portal handoffs.
- Visibility into on-site progress is opaque.

### Involvement across workflows
- **Approver on:** 04 (RFI replies and variation sign-off), **23 (agrees the zero-rated VAT analysis at project close)**.
- **Source / contributor on:** 01 (issues drawings), 02 (issues tender), 14 (inbound client enquiries), 20 (content consent for marketing).
- **Read on:** 05 (receives Programme Valuation Report each Claim Period), 06 (live site dashboard, scoped), 07 (close-out pack), 10 (recipient of sales invoices), 22 (client portal — approved timesheet totals per cost code, where the contract provides).

### Permissions needed (coarse)
- Read on their own tenders and projects, including the live site dashboard, valuations, sales invoices, close-out pack, and (contract-permitting) timesheet totals.
- Write on tender submission and inbound enquiries.
- Approve / reject VOs on their work; approve the zero-rated VAT analysis at project close.

### Devices & environment
- Desktop primarily. May use tablet on site visits.

### Notes
- The architect's client-facing **cost codes** must be referenced consistently through every screen and report that touches that architect's tender. This is a cross-cutting requirement, not specific to one journey.
- Where Jewel works with an external Quantity Surveyor (consultant), they are treated as an invited contact rather than a separate persona — internal QS work is owned by P03 Project & Commercial Lead.

---

## P02 — Subcontractor

**Role:** Field worker delivering work against tender line items
**Reports to:** Project & Commercial Lead (commercially); Site Team (operationally on site)
**Tooling today:** Phone, paper, ad-hoc messaging
**Frequency on platform:** Daily — on site
**Status:** Draft

### Goals
- Quickly log progress and time on site.
- Get RFIs answered fast so work isn't blocked.
- Bid for new work without learning a new tool.
- Get paid on time and accurately.

### Pain points (current state)
- Paper timesheets get lost or delayed.
- RFI handling is slow and untracked.
- No personal view of progress against the tender.
- Compliance documentation scattered across systems; chased manually before expiry.

### Involvement across workflows
- **Contributor / source on:** 03 (returns quote against a bid package), 04 (raises RFIs from site), 06 (attendance check-in + photos), 07 (defect resolution with photo evidence), 08 (self-service upload of compliance documents), 09 (submits supplier invoices via the portal), 22 (day-rate timesheet entries where applicable).
- **Read on:** 01 (assigned drawings at current revision, on mobile).

### Permissions needed (coarse)
- Update completion on line items assigned to them. Submit timesheets (day-rate). Raise RFIs. Submit invoices. Read drawings and specs for their assigned work. Self-service upload of own compliance documents.

### Devices & environment
- Mobile phone primarily. Touch-friendly UI is essential, including in poor connectivity (offline-tolerant fields where possible).

### Notes
- Subcontractors are external to Jewel Enterprises. Onboarding needs to be lightweight — typically an invite link rather than a full account-creation flow.

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
- **Owner on:** 02 (Tender & BoQ), 03 (Procurement — also award approver), 04 (Variations / RFIs / Delays), 05 (Programme & Valuations — also issuance approver), 07 (Close-Out & Defects — also defect sign-off), 22 (Timesheet Management — weekly batch approval).
- **Approver on:** 01 (drawing supersedure override), 06 (site report sign-off), 14 (project-specific comms judgement), 15 (above-threshold material orders), 23 (commercial sign-off at project completion).
- **Contributor on:** 10 (triggers AR invoice draft on valuation / WO completion), 20 (project consent confirmation).
- **Read on:** 08 (subcontractor compliance status before award), 09 (project cost ledger visibility), 11 (project slice of cashflow), 18 (tender evidence pack consumer), 21 (project document custody).

### Permissions needed (coarse)
- Owner on project records, BoQ, project change register, programme, valuations, defect register, timesheet approval.
- Approver on bids, variations, valuations, defect sign-off, drawing supersedure, site reports, threshold material orders, project completion settlement.
- Read across all projects and across the operational layers (compliance, AP, cashflow, documents) that affect their projects.

### Devices & environment
- Laptop primarily; tablet on site.

---

## P04 — Office & Compliance Coordinator

**Role:** Internal office and compliance hub — keeps the operational machinery running
**Reports to:** Directors / FD
**Tooling today:** Outlook, SharePoint, Monday.com, Dashpivot, RAMsApp, phone system
**Frequency on platform:** Daily
**Status:** Draft

### Goals
- Keep compliance, insurance, fleet, procurement and front-of-house comms clean and current.
- Stop being the human router for documents and client enquiries.

### Pain points (current state)
- Subcontractor compliance scattered across Monday, SharePoint and Excel.
- Insurance / accreditation renewals tracked in Outlook calendar and chased manually.
- Material orders raised via WhatsApp / email with no project linkage.
- Document filing in SharePoint is a never-ending tidy-up task.

### Involvement across workflows
- **Owner on:** 08 (Subcontractor Compliance), 14 (Client & Reactive Comms), 15 (Materials & Deliveries), 16 (HR, Onboarding & IT Access — admin / orchestration), 18 (Compliance / Insurance / Accreditation), 19 (Fleet), 21 (Document Management — residual after JPMS rollout).
- **Contributor on:** 03 (supplier & subcontractor directory upkeep), 12 (starter / leaver events feed payroll), 13 (some inbox-triage queries route here), 22 (back-office timesheet capture for non-site staff).
- **Read on:** 09 (supplier directory feeds AP), 17 (IT support routing).

### Permissions needed (coarse)
- Owner on supplier & subcontractor directory, compliance register, fleet register, procurement requests, comms log, document register, onboarding orchestration.
- Approver on routine renewals and onboarding checklist items.
- Contributor on starter/leaver events, timesheet back-office capture, and inbox-triage queries that need a non-finance route.

### Devices & environment
- Desktop primarily.

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
- Submit timesheets without being chased.

### Pain points (current state)
- Site information lives across WhatsApp, Photos and inboxes.
- Attendance tracking is a separate Excel / Dashpivot / calendar.
- No personal view of progress against the BoQ.

### Involvement across workflows
- **Owner on:** 06 (Site Reporting & Progress — daily capture).
- **Contributor on:** 02 (walk-round notes / photos during pre-construction), 04 (raises RFIs from site), 05 (% progress per BoQ section feeds the programme), 07 (defect identification with photos), 12 (timesheet submission), 15 (procurement requests + goods-in), 19 (driver of record), 22 (site timesheet capture via mobile).
- **Read on:** 01 (current drawing revision on mobile), 21 (project documents in scope).

### Permissions needed (coarse)
- Write on site reports, photos, attendance, snags, timesheets (own), procurement requests, goods-in confirmations, RFI raises.
- Read drawings, BoQ sections, and project documents for their assigned project.

### Devices & environment
- Phone primarily. Touch-friendly, offline-tolerant capture is essential.

---

## P06 — Brand & Content

**Role:** Marketing and brand custodian across Jewel entities
**Reports to:** Directors
**Tooling today:** Canva, Meta Business, LinkedIn, SharePoint / OneDrive
**Frequency on platform:** Weekly
**Status:** Draft

### Goals
- Run the content calendar from current project data, not screenshot hunts.
- Get Director sign-off recorded against each post before publish.

### Pain points (current state)
- Brand assets duplicated across folders.
- Eligibility-for-content and client consent are tribal knowledge.

### Involvement across workflows
- **Owner on:** 20 (Marketing & Brand).
- **Read on:** 21 (brand-asset library inside the document register).

### Permissions needed (coarse)
- Read on project completion + consent flags. Write on content items and brand assets. Read on the brand-asset library.

### Devices & environment
- Desktop + mobile.

### Notes
- The JewelBB brand voice and palette are encoded in the `jewelbb-brand-voice` Cowork skill — that's where copy and design briefs are reviewed against the brand system. JPMS only needs to surface the consent flag and asset-library link.

---

## P07 — Finance Director (FD)

**Role:** Internal owner of finance across BB / PS / PFP
**Reports to:** Directors / MD
**Tooling today:** Dext, Xero, Brightpay, Chaser HQ, online banking, Outlook, Excel
**Frequency on platform:** Daily — heavy
**Status:** Draft

### Goals — **the platform's primary driver**
- Produce an accurate cashflow forecast across entities without rebuilding it weekly in Excel.
- Spend time on judgement and intervention, not data assembly.
- Catch subcontractor invoice errors before payment, not after.
- Ring-fence incoming cash to the correct project; time cash calls to actual % completion.
- Get out of the IT-helpdesk role.

### Pain points (current state)
- Forecast accuracy depends on knowing real completion %, which is currently unreliable because line-item completion isn't tracked consistently.
- AP coding and matching consumes ~80 h/month — the single largest workflow in the audit.
- Accounts inbox triage consumes ~60 h/month.
- Cashflow forecast rebuilt weekly in Excel; cross-entity charges manual.
- Currently doubling as IT support (~50 h/month) at the cost of finance focus.

### Involvement across workflows
- **Owner on:** 09 (Accounts Payable), 10 (Accounts Receivable), 11 (Cashflow & Management Reporting), 12 (Payroll), 13 (Accounts Inbox Triage), 23 (Project Completion Settlement & VAT — produces the draft zero-rated VAT analysis and triggers retention release). Current owner (target outsourced to P09) on 17 (IT support).
- **Approver on:** 07 (retention release at close-out), 16 (IT access gate during onboarding), 17 (IT governance), 18 (compliance / insurance sign-off), 19 (insurance and fine payments), 22 (payroll exception review and cost-code overrun).
- **Read on:** 02, 03, 04, 05 (commercial visibility across project-side workflows), 08 (compliance status gates payment), 15 (material orders feed AP), 21 (corporate documents).

### Permissions needed (coarse)
- Owner on AP, AR, payroll, cashflow dashboard, accounts inbox triage, project settlement and VAT analysis.
- Approver on payment runs up to threshold, IT access provisioning, compliance and insurance renewals, retention release, timesheet cost-code overrun.
- Allocate received funds against projects. Read across all projects and across the operational layers that affect cash.

### Devices & environment
- Desktop primary. Mobile for approvals.

### Notes
- This is the **litmus test** for every scoping decision: if a proposed feature doesn't make the FD's cashflow forecast more accurate or remove inbox / AP load, ask whether it belongs in phase one.

---

## P08 — Directors / MD

**Role:** Business owners — executive decisions across BB / PS / PFP
**Reports to:** —
**Tooling today:** Email, Teams, reports from FD
**Frequency on platform:** Daily
**Status:** Draft

### Goals
- See cashflow position independently, in real time, without asking the FD.
- Sign-off on high-value commercial decisions in-system with audit trail.
- See at-a-glance status across the project portfolio.
- Identify at-risk projects early.

### Pain points (current state)
- Cashflow visibility lives in the FD's head until Excel is updated.
- Sign-offs scattered across email; no single audit trail.
- Pipeline and active-project visibility is fragmented across emails and spreadsheets.

### Involvement across workflows
- **Approver on:** 02 (tender sign-off), 03 (high-value award), 04 (high-value variations), 05 (valuations), 09 (above-threshold payments), 11 (strategic cashflow decisions), 12 (payroll), 16 (confirm starter / leaver), 18 (annual compliance review), 20 (brand sign-off before publish), 22 (cost-code overrun above threshold), 23 (final VAT outcome agreed with client).
- **Read on:** 01, 06, 07, 08, 10, 14, 17 (IT security / audit visibility), 19, 21.
- Day-to-day finance triage and materials ordering sit with P07 FD and P04 OCC respectively; Directors are not in the loop on those.

### Permissions needed (coarse)
- Read across most workflows (excluding the day-to-day inbox triage and materials ordering).
- Approve high-value commercial decisions, payroll, tender, valuations, brand publish, final VAT outcome, cost-code overrides above threshold, IT-access confirmations.
- Admin on users, roles, integrations.

### Devices & environment
- Mobile + desktop.

---

## P09 — Outsourced IT Helpdesk

**Role:** External tier-1 IT support partner — target owner of workflow 17
**Reports to:** Finance Director (governance only)
**Tooling today:** Their own ticketing system + M365 Admin / Entra / Intune access
**Frequency on platform:** Daily — limited surface
**Status:** Draft (provider not yet selected)

### Goals
- Resolve tier-1 IT issues from staff without involving the FD.
- Execute provisioning from JPMS workflow 16 triggers.

### Pain points (current state)
- N/A — the role doesn't exist yet; today the FD does it.

### Involvement across workflows
- **Owner on:** 17 (IT & Systems Administration — tier-1, target).
- **Contributor on:** 16 (executes provisioning items routed from the onboarding orchestrator).

### Permissions needed (coarse)
- Scoped admin on M365 Admin / Entra / Intune. No access to commercial or project data inside JPMS.

### Devices & environment
- Their own.

### Notes
- This is an **external partner** role. The JPMS surface should be deliberately narrow — provisioning hooks and audit reports only — to keep their access scope minimal.
