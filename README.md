# JPMS — Jewel Project Management System

A construction project management system built for Jewel Bespoke Build. JPMS runs the full project lifecycle — from the first lead touchpoint through tender, procurement, mobilisation, site delivery, valuations, close-out and aftercare — and produces the three business-critical commercial outputs that fall out of project data automatically: the **Programme Valuation Report** issued each claim period to the client, the **CVR (Cost-Value Reconciliation)** that gives the QS and PM live commercial control over margin per package, and the **cashflow forecast** that gives the directors a live view of where the business stands.

---

## 1. What JPMS does

JPMS owns the project lifecycle end-to-end. Every project artefact lives in one place: leads, drawings, BoQs, work orders, RFIs, submittals, variations, site reports, inspections, incidents, timesheets, defects, settlement records.

Three commercial outputs come out of that data, each for a different audience:

- **The Programme Valuation Report (PVR).** Issued every claim period to the client. Built directly from approved progress and approved variations — review, not rebuild.
- **The CVR (Cost-Value Reconciliation).** Live commercial control for the QS and PM: actual vs forecast vs tender per package, margin by trade, Prelims and EOTs visible separately, variations rolled up against the original BoQ headings AND on the central register. **Replaces the Excel CVR workbooks JBB use today and removes any need for tools like Planyard.**
- **The cashflow forecast.** A live picture of expected income, forward commitments and predicted project completion across the portfolio. Built purely from project data. Solving cashflow forecast accuracy is the primary reason JPMS exists.

All three come from the same project data. The CVR, the PVR and the cashflow can never disagree because they share one source of truth.

JPMS is not an accountancy tool. Xero, Brightpay, Dext and the rest of the back-office stack carry on doing AP, AR, payroll and bookkeeping; JPMS publishes the project data those tools need so the accountancy team can run their own workflows without re-keying anything from JPMS.

---

## 2. User Roles

Eleven roles use JPMS. Each link opens the full role card.

| # | Role | What they do in JPMS |
|---|---|---|
| P01 | [Director / MD](docs/01-personas/01-director-md.md) | Approves high-value commercial decisions and the final VAT outcome. Reads cashflow and portfolio status in real time. |
| P02 | [Finance Director](docs/01-personas/02-finance-director.md) | Owns the cashflow forecast and the project completion settlement / zero-rated VAT analysis. Their accountancy day-job remains outside JPMS. |
| P03 | [Project Manager](docs/01-personas/03-project-manager.md) | Owns drawings, programme coordination, the project change layer (RFIs / submittals / variations / NoDs), correspondence. |
| P04 | [QS / Estimator](docs/01-personas/04-quantity-surveyor-estimator.md) | Owns tender, BoQ, valuations and variation pricing. |
| P05 | [Site Manager](docs/01-personas/05-site-manager.md) | Owns live-site delivery — sequencing, quality, snag coordination, on-site H&S confirmation. |
| P06 | [Health & Safety Officer (H&SO)](docs/01-personas/06-health-safety-officer.md) | Owns the H&S framework, inspections, audits, incident governance, corrective actions. |
| P07 | [Office & Compliance Coordinator](docs/01-personas/07-office-compliance-coordinator.md) | Owns subcontractor compliance admin: onboarding records, insurance / cert / CIS / RAMS, expiry tracking. |
| P08 | [Architect / Designer / Consultant](docs/01-personas/08-architect-designer-consultant.md) | Issues drawings and tenders. Approves RFIs, submittals, variations and the VAT analysis at project close. |
| P09 | [Client / Homeowner](docs/01-personas/09-client-homeowner.md) | Selects, approves, instructs, pays, signs off Practical Completion and defects. |
| P10 | [Subcontractor](docs/01-personas/10-subcontractor.md) | Submits quotes, captures site work and timesheets, uploads compliance documents, accepts RAMS / inductions / permits. |
| P11 | [Foreman / Site Team](docs/01-personas/11-foreman-site-team.md) | Workface — daily progress, photos, attendance, immediate issue reporting up to Site Manager. |

Full role cards live in [`/docs/01-personas/`](docs/01-personas/). The role × workflow responsibility matrix is in [`/docs/05-data-model/permissions-matrix.md`](docs/05-data-model/permissions-matrix.md).

---

## 3. Workflows JPMS handles

Ten lifecycle stages, in order. Click through for the detailed workflow.

| # | Workflow | User stories | Notes |
|---|---|---|---|
| 00 | [Sales, Marketing & CRM](docs/03-workflows/00-sales-marketing-crm.md) | ✅ 14 drafted | Lifecycle starts here — lead → opportunity → won → project shell into 01. |
| 01 | [Drawing Receipt & Document Control](docs/03-workflows/01-drawing-receipt-document-control.md) | ✅ 8 drafted | Live revision control + issue / acknowledgment register. |
| 02 | [Pre-Construction: Tender & BoQ](docs/03-workflows/02-preconstruction-tender-boq.md) | ✅ 9 drafted | |
| 03 | [Subcontractor Procurement & Onboarding](docs/03-workflows/03-subcontractor-procurement-onboarding.md) | ✅ 22 drafted | Procurement + compliance onboarding combined; compliance gate before award. |
| 04 | [H&S Site Mobilisation & Compliance](docs/03-workflows/04-hs-site-mobilisation-compliance.md) | ✅ 16 drafted | Mobilisation gate, inspections engine, incidents & corrective actions. |
| 05 | [RFIs, Submittals, Variations & Delays](docs/03-workflows/05-rfis-submittals-variations-delays.md) | ✅ 12 drafted | Unified change layer; submittals before installation. |
| 06 | [Site Delivery, Programme & Reporting](docs/03-workflows/06-site-delivery-programme-reporting.md) | ✅ 12 drafted | Mobile-first site app. |
| 07 | [Valuations, Cashflow & Forecasting](docs/03-workflows/07-valuations-cashflow-forecasting.md) | ✅ 45 drafted | **Produces all three commercial outputs: Programme Valuation Report, CVR, and cashflow forecast.** Replaces JBB's Excel CVR and Planyard. Includes timesheet cost-code allocation. |
| 08 | [Quality, Snags, Handover & Aftercare](docs/03-workflows/08-quality-snags-handover-aftercare.md) | ✅ 20 drafted | Snags, completion packs, Practical Completion, zero-rated VAT analysis, settlement, retention release, defects-period. |
| 09 | [Portfolio Reporting & Analytics](docs/03-workflows/09-portfolio-reporting-analytics.md) | ✅ 12 drafted | Director / FD cross-project view. |

**Total: 170 user stories drafted across 10 workflows.** Each story is in the format *"as X user I want Y, so that Z"* with a status flag (Drafted → In Review → Confirmed), and a `US-NN-MM` ID so screens and code reference back to the story they're delivering.

The high-level lifecycle index is in [`/docs/02-lifecycle/`](docs/02-lifecycle/); the detailed workflow files in [`/docs/03-workflows/`](docs/03-workflows/); per-role journey slices in [`/docs/04-user-journeys/`](docs/04-user-journeys/).

---

## 4. Domain Concepts

The shared language between the business and the system. The most important concepts:

- **Project** — central organising concept; everything else belongs to a project.
- **Lead** and **Opportunity** — the front end of the lifecycle, before a project exists.
- **Tender** and **Bill of Quantities (BoQ)** — what the architect asks for and the priced breakdown of how Jewel will deliver it.
- **Cost Code** — the architect's client-facing code that threads through every screen and report.
- **Work Order** — the contract artefact when a subcontractor is awarded work.
- **RFI / Submittal / Variation / NoD** — the change layer on a live project.
- **Mobilisation Checklist / Inspection / Observation / Incident / Corrective Action** — the H&S engine that gates site delivery.
- **Claim Period** — the contractual cycle (typically monthly) for issuing the Programme Valuation Report and the CVR.
- **Programme Valuation Report (PVR)** — the per-claim-period valuation issued to the client.
- **CVR (Cost-Value Reconciliation)** — internal commercial control per project: actual vs forecast vs tender per package, margin per trade. Built from the same data as the PVR, but framed for the QS and PM.
- **Forecast Component** — every Forecast Final Cost in the CVR is the sum of explicit components (Cost Incurred / Cost Committed / QS Accruals / Prelim Forecast / Cost to Complete). Never a black-box number.
- **QS Accrual** — explicit QS judgement adjustment (Add / Omit / Liability) that feeds the forecast with a sign-off and audit trail.
- **Prelim Item** and **Prelim Forecast Entry** — Prelims live as a distinct CVR section above the BoQ packages, with Tendered vs Actual vs Difference per item.
- **EOT (Extension of Time)** — tracked per project with programme impact, surfaced on the CVR header alongside Weeks Ahead / Behind.
- **Cashflow Forecast** — the live view of forward income, commitments and predicted completion across the portfolio, from project data alone.
- **Settlement Record** and **VAT Analysis** — the audit-grade summary and zero-rated VAT analysis at project completion.

Full entity model: [`/docs/05-data-model/entities.md`](docs/05-data-model/entities.md). Construction glossary: [`/docs/00-business-context/glossary.md`](docs/00-business-context/glossary.md).

---

## 5. How we got here, and what's next

### Done

1. **Domain discovery** — established the construction context, the user roles, and the primary pain point (cashflow forecast accuracy).
2. **Operational audit** — ingested the JBB operational task analysis covering every task the business handles today.
3. **Scope refinement** — defined what JPMS is and isn't. Accountancy, HR, IT admin, facilities and marketing are out of scope; project management is in.
4. **Procore-style alignment pass** — restructured into 11 user roles and 10 lifecycle workflows with three new modules (CRM, H&S Mobilisation, Portfolio Analytics) and explicit cross-cutting engines (Inspections, Observations / Incidents, Submittals, Correspondence). See [2026-05-22 alignment note](docs/00-business-context/meetings/2026-05-22-procore-alignment.md).
5. **Workflow definition** — each of the ten JPMS workflows captured with purpose, current state, target flow, JPMS functionality required, integrations, and acceptance criteria.
6. **User stories** — every workflow now carries the user stories that drive UI design. **170 stories drafted across the 10 workflows** (counts per workflow are in section 3 above). Each story has a `US-NN-MM` ID and a status flag (Drafted → In Review → Confirmed).
7. **Domain concepts and permissions** — entity model, Role × Workflow permissions matrix, status models, approval flows.
8. **CVR alignment pass** — workflow 07 expanded to deliver the CVR as a third primary output alongside the PVR and the cashflow forecast, fixing the three issues JBB's QS lead called out on the Planyard-style pilot workbook: traceable Forecast Final Cost components, Prelims and EOTs visible against tender separately, and per-package variation margin alongside the central register. Planyard subscription not required. See [2026-05-23 CVR alignment](docs/00-business-context/meetings/2026-05-23-cvr-alignment.md).
9. **Bluebeam integration pass** — Bluebeam Studio Projects designated as the canonical drawing store from day one. JPMS reads new drawing revisions automatically via the Studio Projects API + webhooks (workflow 01 — the QS never re-uploads a drawing). Take-off lands in JPMS via Bluebeam Markups List CSV import in v1, with the Markups API direct path planned for phase 2 once the QS has adopted the JPMS-published Bluebeam tool-set consistently. See [2026-05-25 Bluebeam integration](docs/00-business-context/meetings/2026-05-25-bluebeam-integration.md).

### Next

1. **Story walkthroughs.** Each role-owner walks the stories for the workflows they own and moves them from Drafted → In Review → Confirmed. Confirmed stories are the contract for what the screen has to deliver.
2. **UI scoping.** Each Confirmed story produces the screens needed to deliver it.
3. **Build.** The JPMS production application in [`/jpms`](jpms/) is built screen-by-screen against the user stories. OAuth sign-in and the approved-user gate are already in place; data layer and per-workflow screens follow.

---

## 6. Technical summary

- **Front-end:** Blazor WebAssembly · .NET 8 LTS · Tailwind.
- **Back-end (next):** ASP.NET Core Web API · Azure SQL · Microsoft Graph integrations where needed.
- **Auth:** OAuth — Google, Microsoft, or email/password. Users are invited by an admin or Project Manager; on first access they pick a sign-in method. The JPMS account is identified by email, so a user who first signs in with Google for `alice@example.com` can equally use the email/password path against the same address later.
- **Hosting:** Azure Static Web Apps initially; Azure App Service once the API lands.
- **PWA:** installable on desktop and mobile.

Production source: [`/jpms`](jpms/) — see [its README](jpms/README.md) for what's built today and how to run it locally.

---

## 7. Repository layout

```
/docs
  /00-business-context     Business overview, operating model, delivery principles, commercial model, glossary, meeting notes archive
  /01-personas             The 11 user-role cards (one file per role)
  /02-lifecycle            The 10 lifecycle stages (high-level, one file per stage)
  /03-workflows            The 10 detailed workflow files with user stories
  /04-user-journeys        Per-role slices through workflows
  /05-data-model           Entities, permissions matrix, status models, approval flows, integrations
  /06-backlog              Must-have-v1, phase-2, consolidated open questions
  site-map.md              Consolidated Phase 1 site map: every route, every story it serves, the build roadmap
/jpms                      Production Blazor WebAssembly application
/assets                    Screenshots, icons, branding
```

Each folder has its own README.

---

## 8. Find anything

- **Business context** — [Overview](docs/00-business-context/business-overview.md) · [Operating model](docs/00-business-context/operating-model.md) · [Delivery principles](docs/00-business-context/delivery-principles.md) · [Commercial model](docs/00-business-context/commercial-model.md) · [Glossary](docs/00-business-context/glossary.md) · [Meeting notes](docs/00-business-context/meetings/)
- **User roles** — [Register](docs/01-personas/README.md) · 11 cards in [`/docs/01-personas/`](docs/01-personas/)
- **Lifecycle** — [High-level stages](docs/02-lifecycle/README.md)
- **Workflows** — [Index](docs/03-workflows/README.md) · 10 detailed files in [`/docs/03-workflows/`](docs/03-workflows/)
- **User journeys** — [Index](docs/04-user-journeys/README.md) · 5 journey files in [`/docs/04-user-journeys/`](docs/04-user-journeys/)
- **Data model** — [Entities + ERD](docs/05-data-model/entities.md) · [Permissions matrix](docs/05-data-model/permissions-matrix.md) · [Status models](docs/05-data-model/status-models.md) · [Approval flows](docs/05-data-model/approval-flows.md) · [Integrations](docs/05-data-model/integrations.md)
- **Backlog** — [Must-have v1](docs/06-backlog/must-have-v1.md) · [Phase 2](docs/06-backlog/phase-2.md) · [Open questions](docs/06-backlog/open-questions.md)
- **Site map** — [Phase 1 site map](docs/site-map.md) — every route, every story, build roadmap
- **Production application** — [`/jpms`](jpms/README.md)
