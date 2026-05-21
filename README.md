# Jewel Enterprises — Business Platform

> Working repository for **JPMS — Jewel Project Management System**.
> The `/docs` and `/prototypes` folders hold the scoping material (user journeys, personas, workflows, data models, UI components, decisions). The `/jpms` folder holds the production application — a Blazor WebAssembly PWA being built against this scope.

---

## 1. Project at a Glance

| | |
|---|---|
| **Vision** | A master operating system for the business, with projects as the central organising concept. |
| **Product** | Manage the programme of work for each project from tender through delivery to cash call. |
| **Business context** | Construction. Jewel Enterprises delivers work commissioned by architects. See [`/docs/requirements/glossary.md`](docs/requirements/glossary.md) for domain terms (Tender, RFI, VO, Cash Call, etc.). |
| **Primary goals** | (1) Run construction projects end-to-end — drawings, tender, BoQ, procurement, site reporting, valuations, close-out. (2) Produce two outputs from project data alone: the **Programme Valuation Report** per Claim Period and the **cashflow forecast**. (3) Publish project data cleanly so the accountancy team can run AP / AR / payroll / VAT in their own tools (Xero, Brightpay) downstream. |
| **Platform** | Progressive Web App (PWA), installable as a desktop/mobile app. |
| **Tech stack** | Blazor (WebAssembly) front-end · ASP.NET Core back-end · Azure hosting · Azure SQL primary database · OAuth sign-in (Google, Microsoft, email/password) — users invited by admins or project managers and pick their sign-in method on first access · Microsoft Teams / Graph integrations. |
| **Phase** | Discovery & scoping. |
| **Methodology** | User-journey-driven scoping with on-site role-play validation sessions. |

---

## 2. How This Repo is Organised

```
/docs
  /user-journeys     Markdown journeys + interactive demos (the core artefact)
    /_templates      Reference-only scaffolding
  /ui-components     Atomic-design component library spec
    /_templates      Reference-only scaffolding
  /workflows         BPMN / Mermaid process diagrams
    /_templates      Reference-only scaffolding
  /data-models       JSON Schemas + entity-relationship diagrams
    /_templates      Reference-only scaffolding
  /requirements      Personas, glossary, permission matrix, non-functional requirements
    /_templates      Reference-only scaffolding
  /meetings          Session notes + decisions log
    /_templates      Reference-only scaffolding
/prototypes          Blazor PWA Journey Index + HTML demos (scoping only)
/jpms                JPMS — production Blazor WebAssembly client app (see Section 11)
/assets              Screenshots, icons, branding
```

Each folder has its own `README.md` explaining its purpose, its `_templates/` reference, and the process for adding real content. Start there before adding new files.

---

## 3. Templates vs project content — the rule

This repository deliberately separates **reference scaffolding** from **confirmed project content**:

- 📁 **`_templates/`** subfolders in every section hold blank templates and worked examples. They exist so we know *what shape* a journey, persona, schema, or component spec should take. **They are reference only.**
- 📁 **The section root** (e.g. `/docs/user-journeys/`) holds **real content** — the scope decisions for JPMS.

When a real journey/persona/component is created, it's copied **from** a template and lives **outside** `_templates/`.

---

## 4. Discovery Phase Progress

Tick items off as we go. This is the single source of truth for "how scoped are we?"

### 4.1 Foundation
- [x] GitHub repository created
- [x] Folder structure scaffolded
- [x] Templates and worked examples in place under `_templates/`
- [x] Kick-off / domain discovery meeting captured ([`2026-05-18-domain-discovery.md`](docs/meetings/2026-05-18-domain-discovery.md))
- [x] JBB operational workflow audit ingested ([`2026-05-20-jbb-workflow-audit.md`](docs/meetings/2026-05-20-jbb-workflow-audit.md))
- [x] Product name: **JPMS — Jewel Project Management System**
- [x] Glossary of business terms started ([`glossary.md`](docs/requirements/glossary.md))

### 4.2 Current-State Mapping
- [x] Primary pain point identified — cashflow forecast accuracy, produced as a JPMS output (see workflow 10)
- [x] Project-management tool landscape inventoried (replaced systems + JPMS inputs + downstream consumers) — see [`integrations.md`](docs/requirements/integrations.md)
- [x] Pain points captured per role — see [`personas.md`](docs/requirements/personas.md) (P01–P07) and per-workflow files in [`/docs/workflows/`](docs/workflows/)
- [x] Manual steps and workarounds documented — current-state section of every workflow file
- [x] Project lifecycle mapped — workflows [`01`](docs/workflows/01-drawing-receipt.md) through [`07`](docs/workflows/07-project-close-out-and-defects.md), plus [`09`](docs/workflows/09-timesheet-management.md), [`10`](docs/workflows/10-cashflow-and-project-forecasting.md), [`11`](docs/workflows/11-project-completion-settlement.md)
- [x] Out-of-scope tasks documented and reasoned — see [`automation-task-coverage.md`](docs/requirements/automation-task-coverage.md)

### 4.3 Personas
- [x] Canonical user roles identified — seven personas (P01–P07): Architect, Subcontractor, Project & Commercial Lead, Office & Compliance Coordinator, Site Team, Finance Director, Directors / MD
- [x] Persona card drafted for each (seven total — [`personas.md`](docs/requirements/personas.md))
- [x] Adjacent roles checked (site manager, internal QS, marketing, IT helpdesk, etc.) — internal QS work owned by P03; external QS consultants treated as invited contacts; marketing and IT helpdesk are out of JPMS scope per the 2026-05-21 scope refinement

### 4.4 Business Entities
- [x] All domain entities listed — see [`data-models/entity-relationship.md`](docs/data-models/entity-relationship.md) entity index
- [ ] JSON Schema drafted for each major entity _(written workflow-by-workflow as each moves Draft → In Review)_
- [x] Entity-relationship diagram drawn (first cut, three sub-diagrams) — [`data-models/entity-relationship.md`](docs/data-models/entity-relationship.md)

### 4.5 User Journeys & Workflows
- [x] All in-scope workflows identified (11 — see [`/docs/workflows/`](docs/workflows/))
- [x] Each workflow drafted (purpose, current state, target flow, JPMS functionality, integrations, acceptance criteria)
- [x] Per-persona user-journey slices drafted for the highest-value actor cuts ([`/docs/user-journeys/`](docs/user-journeys/))
- [ ] Edge cases captured per workflow (refined in deep-dives)
- [ ] Each workflow walked through with the named operational owner
- [ ] Confirmation checklist signed off per workflow / journey

### 4.6 UI Component Library
- [ ] Atoms inventoried
- [ ] Molecules inventoried
- [ ] Organisms inventoried
- [ ] Page layouts drafted
- [ ] Accessibility notes added per component

### 4.7 Cross-cutting
- [x] Permission matrix drafted (Role × Workflow) — [`permission-matrix.md`](docs/requirements/permission-matrix.md) _(coarse; refined per workflow)_
- [ ] Non-functional requirements documented (performance, security, reporting, offline)
- [x] Integration points catalogued (Microsoft 365 / Teams / Xero / Dext / Brightpay / HMRC CIS / …) — [`integrations.md`](docs/requirements/integrations.md)
- [ ] Cost-code propagation rules captured (must follow every architect's tender end-to-end)

### 4.8 Sign-off
- [ ] All journeys confirmed by business owner
- [ ] Data models exported to OpenAPI skeleton
- [ ] Handover document prepared for development

---

## 5. Personas

> Each row links to its card in [`docs/requirements/personas.md`](docs/requirements/personas.md). The Role × Workflow RBAC matrix is in [`permission-matrix.md`](docs/requirements/permission-matrix.md).

| # | Persona | Type | Role summary | Card status |
|---|---|---|---|---|
| P01 | [Architect](docs/requirements/personas.md#p01--architect) | External client | Sends tenders with drawings and specs; defines cost codes carried through the system; approves RFIs, variations, and the zero-rated VAT analysis at project close. | Draft |
| P02 | [Subcontractor](docs/requirements/personas.md#p02--subcontractor) | External delivery | On-site delivery. Returns quotes, updates progress, raises RFIs, captures day-rate timesheets, uploads compliance documents. | Draft |
| P03 | [Project & Commercial Lead](docs/requirements/personas.md#p03--project--commercial-lead) | Internal | PM + commercial. Owns the project lifecycle (workflows 02–05, 07, 09) and the internal QS function. | Draft |
| P04 | [Office & Compliance Coordinator](docs/requirements/personas.md#p04--office--compliance-coordinator) | Internal | Owns subcontractor compliance & onboarding (workflow 08). | Draft |
| P05 | [Site Team](docs/requirements/personas.md#p05--site-team) | Internal field | Site managers, foremen, operatives. The capture layer for site reality. | Draft |
| P06 | [Finance Director (FD)](docs/requirements/personas.md#p06--finance-director-fd) | Internal executive | Owns the JPMS finance outputs: cashflow forecast (workflow 10) and project completion settlement & VAT (workflow 11). **Drives the primary pain point.** | Draft |
| P07 | [Directors / MD](docs/requirements/personas.md#p07--directors--md) | Internal executive | Executive decisions. Approver on high-value commercial items and the final VAT outcome. | Draft |

**Status legend:** Draft · In Review · Confirmed

---

## 6. User Journeys & Workflows

Workflows are the cross-actor process maps for JPMS (one per file under [`/docs/workflows/`](docs/workflows/)). Journeys are per-persona slices through those workflows where the actor experience deserves its own walkthrough (under [`/docs/user-journeys/`](docs/user-journeys/)). Status moves left-to-right: Draft → In Review → Confirmed.

### 6.1 Workflows (process maps)

| # | Workflow | Owner | Status |
|---|---|---|---|
| 01 | [Drawing Receipt & Distribution](docs/workflows/01-drawing-receipt.md) | P03 PCL | Draft |
| 02 | [Pre-Construction: Tender & BoQ](docs/workflows/02-preconstruction-tender-boq.md) | P03 PCL | Draft |
| 03 | [Subcontractor Procurement (Bid → Award)](docs/workflows/03-subcontractor-procurement.md) | P03 PCL | Draft |
| 04 | [Variations, RFIs & Delays](docs/workflows/04-variations-rfis-delays.md) | P03 PCL | Draft |
| 05 | [Programme & Valuations](docs/workflows/05-programme-and-valuations.md) — produces the Programme Valuation Report per Claim Period | P03 PCL | Draft |
| 06 | [Site Reporting & Progress](docs/workflows/06-site-reporting-and-progress.md) | P05 Site | Draft |
| 07 | [Project Close-Out & Defects](docs/workflows/07-project-close-out-and-defects.md) | P03 PCL | Draft |
| 08 | [Subcontractor Compliance & Onboarding](docs/workflows/08-subcontractor-compliance-and-onboarding.md) | P04 OCC | Draft |
| 09 | [Timesheet Management (cost-code-aware)](docs/workflows/09-timesheet-management.md) | P03 PCL | Draft |
| 10 | [Cashflow & Project Forecasting](docs/workflows/10-cashflow-and-project-forecasting.md) — produces the cashflow forecast _(primary pain-point anchor)_ | P06 FD | Draft |
| 11 | [Project Completion Settlement & VAT Analysis](docs/workflows/11-project-completion-settlement.md) | P06 FD | Draft |

> Twelve workflows from the original JBB audit were ruled out of JPMS scope on 2026-05-21 — accountancy, payroll, HR, IT admin, facilities, marketing. See [`/docs/meetings/2026-05-21-scope-refinement.md`](docs/meetings/2026-05-21-scope-refinement.md) for the reasoning and [`automation-task-coverage.md`](docs/requirements/automation-task-coverage.md) for the task-level coverage table.

### 6.2 User journeys (per-persona slices)

| # | Journey | Persona | Source workflow | Status |
|---|---|---|---|---|
| 03a | [Subcontractor: receive bid package and return a quote](docs/user-journeys/03a-subcontractor-quote-return.md) | P02 Subcontractor | 03 | Draft |
| 04a | [Architect / CA: respond to an RFI](docs/user-journeys/04a-architect-rfi-response.md) | P01 Architect | 04 | Draft |
| 06a | [Site Team: daily progress capture on mobile](docs/user-journeys/06a-site-team-daily-capture.md) | P05 Site Team | 06 | Draft |
| 08a | [Subcontractor: upload renewed compliance document](docs/user-journeys/08a-subcontractor-compliance-upload.md) | P02 Subcontractor | 08 | Draft |
| 10a | [Finance Director: morning cashflow review](docs/user-journeys/10a-fd-cashflow-forecast.md) _(primary pain-point anchor)_ | P06 Finance Director | 10 | Draft |

---

## 7. Business Entities

> JSON Schemas are written workflow-by-workflow as each workflow moves Draft → In Review. The first-cut ERD (three sub-diagrams: project lifecycle, subcontractor & compliance, timesheets / cashflow / settlement) is in [`data-models/entity-relationship.md`](docs/data-models/entity-relationship.md).

### 7.1 Project lifecycle

| Entity | Description | Schema | First surfaced |
|---|---|---|---|
| Organisation | The JBB / Jewel entity (BB, PS, PFP). Cross-entity flag on most other records. | _to be created_ | All |
| Project | Central organising concept. | _to be created_ | All |
| Architect | External; issues tenders and approves variations / VAT outcome. | _to be created_ | 01, 02 |
| Tender | Becomes a project on award. | _to be created_ | 02 |
| Cost Code | Architect's client-facing code; threads through project / WO / timesheet / valuation. | _to be created_ | 02, 09 |
| Drawing | A drawing per scope. | _to be created_ | 01, 02 |
| Drawing Revision | Versioned with supersede logic. | _to be created_ | 01 |
| BoQ | Bill of Quantities (one per project). | _to be created_ | 02 |
| BoQ Line Item | Discrete unit of priced and tracked work. | _to be created_ | 02, 04, 05 |
| Rate / Rate Library | Pricing source for BoQ; versioned, supplier-linked. | _to be created_ | 02 |
| Bid Package | Trade-scoped bid issued to subcontractors. | _to be created_ | 03 |
| Quote | Subcontractor's returned price. | _to be created_ | 03 |
| Work Order | Contract artefact post-award; drives commitments on the cashflow forecast. | _to be created_ | 03, 07, 09, 10 |
| Variation | Updates BoQ; rolls up to valuation; may trigger a bid-package loop. | _to be created_ | 04, 05 |
| RFI | Request for Information; routes to architect. | _to be created_ | 04 |
| NoD | Notice of Delay. | _to be created_ | 04 |
| Programme Task | Tied to BoQ line items. | _to be created_ | 05 |
| Valuation | Per Claim Period; feeds expected income on the cashflow forecast. | _to be created_ | 05 |
| Claim Period | Contractual cycle for valuation reporting. | _to be created_ | 05, 10 |
| Site Report | Daily capture from site app. | _to be created_ | 06 |
| Defect | Snag register per project. | _to be created_ | 07 |
| Practical Completion | PC event; triggers workflows 07 and 11 in parallel. | _to be created_ | 07, 11 |

### 7.2 Subcontractor & compliance

| Entity | Description | Schema | First surfaced |
|---|---|---|---|
| Subcontractor | Master record with trade tags. | _to be created_ | 03, 08 |
| Compliance Document | Insurance, certs, tickets — with expiry. | _to be created_ | 08 |
| Renewal Event | Compliance renewal cycle. | _to be created_ | 08 |
| RAMS | Project-specific risk & method statement. | _to be created_ | 08 |
| CIS Status | HMRC verification status. | _to be created_ | 08 |

### 7.3 Timesheets, cashflow forecast & settlement

| Entity | Description | Schema | First surfaced |
|---|---|---|---|
| Person | Internal staff (site team and project leads). | _to be created_ | 09 |
| Cost Code Budget | Per-cost-code budget (allocated / committed / spent / remaining). Arbiter of the workflow 09 hard-block rule. | _to be created_ | 09 |
| Cost Code Allocation | Each timesheet entry's allocation to a cost code. | _to be created_ | 09 |
| Timesheet | Daily entry per person × project × date. | _to be created_ | 09 |
| Timesheet Approval | Weekly batch approval record. | _to be created_ | 09 |
| Cashflow Forecast Snapshot | A JPMS-produced forecast at a point in time. Built from project data alone. | _to be created_ | 10 |
| Settlement Record | Final audit-grade summary at project close. Triggers retention release. | _to be created_ | 11 |
| VAT Analysis | Zero-rated vs standard-rated breakdown; carries client agreement. | _to be created_ | 11 |
| Retention Release | Trigger published for accountancy to action in AP downstream. | _to be created_ | 11 |

---

## 8. Next steps

1. Resolve the open questions on workflows 09 (cost-code budget overrun rule) and 10 (cashflow forecast horizon and the completion-% prediction model).
2. Walk through journey [10a](docs/user-journeys/10a-fd-cashflow-forecast.md) and validate the FD's view of the cashflow dashboard.
3. Decide the next two workflows to deep-dive into beyond the five already drafted as journeys (03a, 04a, 06a, 08a, 10a).
4. Begin writing JSON Schemas for the project-lifecycle entities so the data model has concrete shapes.

---

## 9. Conventions

- **File names:** lower-kebab-case, with a numeric prefix for ordering where it helps (`01-`, `02-`).
- **Markdown only** for narrative content. Diagrams in Mermaid where possible (renders natively on GitHub).
- **JSON Schema (draft 2020-12)** for entity definitions. Schemas live in `/docs/data-models/` and are referenced by journeys.
- **One PR per journey** during deep-dive sessions, so reviews stay focused.
- **`_templates/` is reference only** — never quoted, never linked from real content.

---

## 10. Quick Links

- [Workflows index](docs/workflows/README.md) — 11 JPMS workflows
- [User Journeys index](docs/user-journeys/README.md) — 5 per-persona slices
- [Personas](docs/requirements/personas.md) · [Permission Matrix](docs/requirements/permission-matrix.md) · [Integrations](docs/requirements/integrations.md) · [Automation-task coverage](docs/requirements/automation-task-coverage.md) · [Glossary](docs/requirements/glossary.md) · [Requirements index](docs/requirements/README.md)
- [UI Components index](docs/ui-components/README.md)
- [Data Models index](docs/data-models/README.md) · [Entity-Relationship Diagram](docs/data-models/entity-relationship.md)
- [Meeting Notes](docs/meetings/README.md) · [Scope refinement 2026-05-21](docs/meetings/2026-05-21-scope-refinement.md) · [Coverage audit + additions 2026-05-20](docs/meetings/2026-05-20-coverage-audit-and-additions.md) · [JBB workflow audit 2026-05-20](docs/meetings/2026-05-20-jbb-workflow-audit.md) · [Domain discovery 2026-05-18](docs/meetings/2026-05-18-domain-discovery.md)
- [Prototype Journey Index](prototypes/journey-index/README.md)
- [JPMS — production app](jpms/README.md)
- [Assets](assets/README.md)

---

## 11. Production — JPMS (Jewel Project Management System)

> Scoping happens in `/docs` and `/prototypes`. The **actual product** is being built in [`/jpms`](jpms/README.md).
> Read this section before touching anything in that folder.

### 11.1 What JPMS is

**JPMS — Jewel Project Management System** — is the production web app the company and its clients will use day-to-day. It's a multi-tenant Blazor WebAssembly PWA. Each user (architects, subcontractors, internal staff) is invited by an admin or project manager and signs in via their chosen OAuth method — Google, Microsoft, or email/password — seeing only the projects they belong to.

### 11.2 Where it differs from `/prototypes`

| | `/prototypes/journey-index` | `/jpms` |
|---|---|---|
| Purpose | Scoping & role-play walkthroughs | The real product |
| Audience | Internal stakeholders during discovery sessions | Jewel staff + external clients |
| Lifetime | Disposable | Long-lived |
| Auth | None | OAuth — Google, Microsoft, or email/password — gated by invitation from an admin or project manager |
| Data | Hard-coded in `.cs` files | ASP.NET Core API + Azure SQL (to be added) |

The two share a deliberate **visual language** — slate palette, rounded cards, same typography stack — so design learnings from the prototype carry over without rework.

### 11.3 Tech stack

- **Front-end:** Blazor WebAssembly · .NET 8 LTS · Tailwind (CDN today, CLI build before launch)
- **PWA:** installable on desktop and mobile, theme `#0f172a`
- **Auth (target):** OAuth — Google, Microsoft, and email/password. Users are invited by admins or project managers; an invitation email lands in their inbox and they pick a sign-in method on first access. A user who chose "Sign in with Google" with `alice@example.com` can equally use that same address for the email/password path; the account is identified by email, not by the provider chosen on first sign-in.
- **Back-end (next):** ASP.NET Core Web API · Azure SQL · Microsoft Graph integrations
- **Hosting:** Azure Static Web Apps initially, Azure App Service once the API lands

### 11.4 What's built so far

The opening slice — sign-in and approved-user gating — is in place:

- **Landing page** at `/` is the login screen with `Continue with Microsoft` and `Continue with Google` buttons.
- **Dashboard** at `/dashboard` is shown to users on the internal allow-list.
- **Request-access** view is shown to users who sign in successfully but aren't on the allow-list.
- **Mock sign-in:** real OAuth isn't wired yet — the buttons accept any email so we can iterate on the UI first.
- **Hard-coded allow-list** in `jpms/Services/AllowListUserDirectory.cs` controls who reaches the dashboard. Edit that file to test both flows.

### 11.5 Two seams ready for real wiring

The mock layer was designed so each piece can be swapped in isolation:

1. **OAuth** — `AuthService.SignInAsync(...)` is the one method that becomes real OAuth. Add `Microsoft.Authentication.WebAssembly.Msal` for Microsoft, Google Identity Services for Google, and replace the body. Client IDs go in `wwwroot/appsettings.json`.
2. **User directory** — `IUserDirectory` is already the right shape for a backend lookup. A new `HttpUserDirectory : IUserDirectory` calls `/api/users/me` on the ASP.NET Core API; one DI line in `Program.cs` swaps it in.

Both can land as small, focused PRs.

### 11.6 Roadmap (rough)

Re-shaped on 2026-05-21 when JPMS scope was tightened to project management proper. The roadmap groups the eleven in-scope workflows into platform foundations, the core project lifecycle, the JPMS outputs, and close-out.

#### Platform foundations
1. Wire real OAuth sign-in — Google, Microsoft, and email/password — with invitation-by-admin/PM flow.
2. Stand up the ASP.NET Core API and Azure SQL schema.
3. Replace the allow-list with an admin-managed directory keyed on the seven personas.
4. Move hosting from Static Web Apps to App Service once an API is in place.

#### Phase 1 — Project lifecycle core
5. **Workflow 01 — Drawing Receipt & Distribution** — earliest project touchpoint; sets the project record up.
6. **Workflow 02 — Pre-Construction: Tender & BoQ** — the priced BoQ that the rest of the project hangs off.
7. **Workflow 08 — Subcontractor Compliance & Onboarding** — gates who can be awarded work. Journey [08a](docs/user-journeys/08a-subcontractor-compliance-upload.md) is the subcontractor self-service slice.
8. **Workflow 03 — Subcontractor Procurement** — bid → award → work order. Journey [03a](docs/user-journeys/03a-subcontractor-quote-return.md).
9. **Workflow 04 — Variations / RFIs / Delays** — change layer on a live project. Journey [04a](docs/user-journeys/04a-architect-rfi-response.md).
10. **Workflow 06 — Site Reporting & Progress** — site app captures progress, photos, attendance. Journey [06a](docs/user-journeys/06a-site-team-daily-capture.md).
11. **Workflow 09 — Timesheet Management (cost-code-aware)** — site timesheets allocated to cost codes; the cost-code budget hard-block rule.

#### Phase 2 — Outputs (the why-we're-building-this)
12. **Workflow 05 — Programme & Valuations** — produces the **Programme Valuation Report** per Claim Period. Key output #1.
13. **Workflow 10 — Cashflow & Project Forecasting** — produces the **cashflow forecast** from project data alone. Key output #2 — the primary pain-point anchor from [2026-05-18](docs/meetings/2026-05-18-domain-discovery.md). Journey [10a](docs/user-journeys/10a-fd-cashflow-forecast.md).

#### Phase 3 — Close-out
14. **Workflow 07 — Project Close-Out & Defects** — snag register, defect sign-off, retention release trigger.
15. **Workflow 11 — Project Completion Settlement & VAT Analysis** — client-facing settlement, zero-rated VAT analysis, retention release transaction published downstream for accountancy.

> Twelve workflows from the original JBB audit were ruled out of JPMS scope on 2026-05-21 (accountancy, payroll, HR, IT admin, facilities, marketing). They were considered, recorded in [`automation-task-coverage.md`](docs/requirements/automation-task-coverage.md), and intentionally left to the systems that already handle them. See [`/docs/meetings/2026-05-21-scope-refinement.md`](docs/meetings/2026-05-21-scope-refinement.md).

### 11.7 Running JPMS locally

See [`jpms/README.md`](jpms/README.md) for the full setup. TL;DR:

```bash
cd jpms
dotnet restore
dotnet run
```

Then open the URL the console prints. Try one of the emails in `Services/AllowListUserDirectory.cs` to land on the dashboard, or any other email to see the request-access flow.
