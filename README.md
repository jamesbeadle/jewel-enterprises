# JPMS — Jewel Project Management System

A construction project management system built for Jewel Bespoke Build. From the moment an architect sends drawings, JPMS runs the project end-to-end — tender, BoQ, subcontractor procurement, on-site reporting, weekly valuations, and final settlement — and produces the two business-critical outputs that fall out of project data automatically: the **Programme Valuation Report** issued each claim period to the client, and the **cashflow forecast** that gives the directors a live view of where the business stands.

---

## 1. What JPMS does

JPMS owns the project lifecycle. Every project artefact — drawings, BoQ, work orders, RFIs, variations, site reports, timesheets, defects, settlement — lives in one place.

Two outputs come out of that data:

- **The Programme Valuation Report.** Issued every claim period to the client. Built directly from approved progress and approved variations, so the next valuation takes minutes to review rather than hours to rebuild.
- **The cashflow forecast.** A live picture of expected income, forward commitments, and predicted project completion. Built purely from project data. Solving cashflow forecast accuracy is the primary reason JPMS exists.

JPMS is not an accountancy tool. Xero, Brightpay, Dext and the rest of the back-office stack carry on doing AP, AR, payroll and bookkeeping; JPMS publishes the project data those tools need so the accountancy team can run their own workflows without re-keying anything from JPMS.

---

## 2. User Roles

Seven roles use JPMS. Click any name for the full role card.

| Role | What they do in JPMS |
|---|---|
| [Architect](docs/requirements/personas.md#p01--architect) | Issues drawings and tenders. Approves RFIs and variations. Agrees the zero-rated VAT analysis at project close. Sees the live project dashboard and the Programme Valuation Report on their projects. |
| [Subcontractor](docs/requirements/personas.md#p02--subcontractor) | Submits quotes against bid packages. Captures site progress and photos. Raises RFIs. Captures day-rate timesheets. Uploads compliance documents through a self-service portal. |
| [Project & Commercial Lead](docs/requirements/personas.md#p03--project--commercial-lead) | Runs the project end-to-end — owns tender, BoQ, procurement, variations, programme & valuations, timesheet approval, close-out. |
| [Office & Compliance Coordinator](docs/requirements/personas.md#p04--office--compliance-coordinator) | Owns subcontractor compliance: insurance, certifications, RAMS, CIS — current and visible before any award. |
| [Site Team](docs/requirements/personas.md#p05--site-team) | Captures site reality daily on mobile — progress against BoQ sections, photos, attendance, snags, timesheets allocated to cost codes. |
| [Finance Director](docs/requirements/personas.md#p06--finance-director-fd) | Owns the cashflow forecast and the project completion settlement. Produces the zero-rated VAT analysis. |
| [Directors / MD](docs/requirements/personas.md#p07--directors--md) | Approves high-value commercial decisions, valuations, the final VAT outcome. Sees cashflow and project status in real time. |

Full role cards: [`docs/requirements/personas.md`](docs/requirements/personas.md).
Role × workflow responsibility: [`permission-matrix.md`](docs/requirements/permission-matrix.md).

---

## 3. Workflows JPMS handles

Eleven workflows, sequenced as the project lifecycle. Click through for full detail.

| # | Workflow | Notes |
|---|---|---|
| 01 | [Drawing Receipt & Distribution](docs/workflows/01-drawing-receipt.md) | |
| 02 | [Pre-Construction: Tender & BoQ](docs/workflows/02-preconstruction-tender-boq.md) | |
| 03 | [Subcontractor Procurement (Bid → Award)](docs/workflows/03-subcontractor-procurement.md) | Journey: [subcontractor quote return](docs/user-journeys/03a-subcontractor-quote-return.md) |
| 04 | [Variations, RFIs & Delays](docs/workflows/04-variations-rfis-delays.md) | Journey: [architect RFI response](docs/user-journeys/04a-architect-rfi-response.md) |
| 05 | [Programme & Valuations](docs/workflows/05-programme-and-valuations.md) | **Produces the Programme Valuation Report** |
| 06 | [Site Reporting & Progress](docs/workflows/06-site-reporting-and-progress.md) | Journey: [site team daily capture](docs/user-journeys/06a-site-team-daily-capture.md) |
| 07 | [Project Close-Out & Defects](docs/workflows/07-project-close-out-and-defects.md) | |
| 08 | [Subcontractor Compliance & Onboarding](docs/workflows/08-subcontractor-compliance-and-onboarding.md) | Journey: [subcontractor compliance upload](docs/user-journeys/08a-subcontractor-compliance-upload.md) |
| 09 | [Timesheet Management (cost-code-aware)](docs/workflows/09-timesheet-management.md) | |
| 10 | [Cashflow & Project Forecasting](docs/workflows/10-cashflow-and-project-forecasting.md) | **Produces the cashflow forecast.** Journey: [FD morning review](docs/user-journeys/10a-fd-cashflow-forecast.md) |
| 11 | [Project Completion Settlement & VAT Analysis](docs/workflows/11-project-completion-settlement.md) | |

Twelve workflows from the original operational audit were considered and ruled out of JPMS scope because they are accountancy, HR, IT admin, facilities or marketing tasks JPMS isn't designed to handle. The scope decision is in [`2026-05-21-scope-refinement.md`](docs/meetings/2026-05-21-scope-refinement.md); the task-level coverage in [`automation-task-coverage.md`](docs/requirements/automation-task-coverage.md).

---

## 4. Domain Concepts

The terms JPMS uses are shared between the business and the system. Agreement on this language is what lets everything else hang together.

The most important concepts:

- **Project** — central organising concept. Everything else belongs to a project.
- **Tender** and **Bill of Quantities (BoQ)** — what the architect asks for, and the priced breakdown of how Jewel will deliver it.
- **Cost Code** — the architect's client-facing code; threads through every screen and report touching their project.
- **Work Order** — the contract artefact when a subcontractor is awarded work.
- **Variation** — a formal change to the BoQ; rolls up to the next valuation, and may trigger a fresh subcontractor procurement loop.
- **Claim Period** — the contractual cycle (typically monthly) for issuing the Programme Valuation Report.
- **Programme Valuation Report** — the per-claim-period valuation issued to the client.
- **Cashflow Forecast** — the live view of forward income, forward commitments and predicted completion, built from project data.
- **Settlement Record** and **VAT Analysis** — the audit-grade summary and zero-rated VAT analysis produced at project completion, agreed with the client.

Full domain vocabulary: [`docs/requirements/glossary.md`](docs/requirements/glossary.md).
Full entity model and relationships: [`docs/data-models/entity-relationship.md`](docs/data-models/entity-relationship.md).

---

## 5. How we got here, and what's next

### Done

1. **Domain discovery** — established the construction context, the user roles, and the primary pain point (cashflow forecast accuracy).
2. **Operational audit** — ingested the JBB operational task analysis covering every task the business handles today.
3. **Scope refinement** — defined what JPMS is and isn't. Twelve out-of-scope workflows ruled out; eleven in-scope workflows kept; seven user roles confirmed.
4. **Workflow definition** — each of the eleven JPMS workflows captured with purpose, current state, target flow, JPMS functionality required, integrations, and acceptance criteria.
5. **Domain concepts and permissions** — entity model drafted; Role × Workflow permission matrix drafted; each role's involvement reconciled with the matrix.
6. **Integrations map** — what feeds JPMS, what JPMS replaces, what consumes JPMS data downstream.

### Next

1. **User stories.** Convert each workflow into the user stories that drive UI design — *"as a Project & Commercial Lead I want to see remaining cost-code budget inline when allocating a timesheet, so that I never accidentally overrun"*. User stories are the bridge between the workflows defined in this repo and the actual screens.
2. **UI scoping.** Each user story produces the screens needed to deliver it. The component library scaffold under [`docs/ui-components/`](docs/ui-components/) gets populated as stories land.
3. **Build.** The JPMS production application in [`/jpms`](jpms/) is built screen-by-screen against the user stories. OAuth sign-in and the approved-user gate are already in place; data layer and per-workflow screens follow.

---

## 6. Technical summary

JPMS is a Progressive Web App built on Blazor WebAssembly, talking to an ASP.NET Core API backed by Azure SQL. Authentication is OAuth — Google, Microsoft, or email/password. Users are invited by an admin or Project & Commercial Lead; on first access they pick a sign-in method. The JPMS account is identified by email, so a user who chose "Sign in with Google" with `alice@example.com` can equally use the email/password path against the same address later.

- **Front-end:** Blazor WebAssembly · .NET 8 LTS · Tailwind.
- **Back-end (next):** ASP.NET Core Web API · Azure SQL · Microsoft Graph for downstream integrations where needed.
- **Hosting:** Azure Static Web Apps initially; Azure App Service once the API lands.
- **PWA:** installable on desktop and mobile.

The production application source lives in [`/jpms`](jpms/) — see [its README](jpms/README.md) for what's built today and how to run it locally.

---

## 7. Repository layout

```
/docs
  /workflows         The 11 JPMS workflows
  /user-journeys     Per-user-role slices through workflows where useful
  /requirements      User roles, permission matrix, integrations, glossary, task coverage
  /data-models       Entity relationship diagram + JSON Schemas (written workflow-by-workflow)
  /meetings          Working decisions log
  /ui-components     UI component library spec (populated as user stories land)
/jpms                Production Blazor WebAssembly application
/assets              Screenshots, icons, branding
```

Each folder has its own README explaining its content. `_templates/` subfolders inside each section hold blank reference scaffolding only — real content lives at the folder root.

---

## 8. Find anything

- **Workflows** — [index](docs/workflows/README.md) · the 11 workflow files in [`/docs/workflows/`](docs/workflows/)
- **User journeys** — [index](docs/user-journeys/README.md) · the 5 journey files in [`/docs/user-journeys/`](docs/user-journeys/)
- **Requirements** — [User roles](docs/requirements/personas.md) · [Permission matrix](docs/requirements/permission-matrix.md) · [Integrations](docs/requirements/integrations.md) · [Glossary](docs/requirements/glossary.md) · [Task coverage](docs/requirements/automation-task-coverage.md) · [Requirements index](docs/requirements/README.md)
- **Data models** — [Entity relationship diagram](docs/data-models/entity-relationship.md) · [Data models index](docs/data-models/README.md)
- **Meeting notes** — [Scope refinement 2026-05-21](docs/meetings/2026-05-21-scope-refinement.md) · [Coverage audit 2026-05-20](docs/meetings/2026-05-20-coverage-audit-and-additions.md) · [JBB workflow audit 2026-05-20](docs/meetings/2026-05-20-jbb-workflow-audit.md) · [Domain discovery 2026-05-18](docs/meetings/2026-05-18-domain-discovery.md) · [Meeting notes index](docs/meetings/README.md)
- **Production application** — [`/jpms`](jpms/README.md)
