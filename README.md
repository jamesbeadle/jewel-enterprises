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
| **Primary goals** | (1) Smooth project management end-to-end. (2) Robust finance workflows tied to projects — **the Accountant's cashflow forecast accuracy is the primary pain point driving scope.** (3) A "second brain" of domain knowledge usable later as an API endpoint by Claude or similar. |
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
- [x] Primary pain point identified — cashflow forecast accuracy (Finance Director — see Section 11 workflow 11)
- [x] Existing tools and spreadsheets inventoried — see [`integrations.md`](docs/requirements/integrations.md)
- [x] Pain points captured per role — see [`personas.md`](docs/requirements/personas.md) (P01–P09) and per-workflow files in [`/docs/workflows/`](docs/workflows/)
- [x] Manual steps and workarounds documented — current-state section of every workflow file
- [x] Existing finance processes mapped — workflows [`09`](docs/workflows/09-accounts-payable.md), [`10`](docs/workflows/10-accounts-receivable.md), [`11`](docs/workflows/11-cashflow-and-management-reporting.md), [`12`](docs/workflows/12-payroll.md), [`13`](docs/workflows/13-accounts-inbox-triage.md)
- [x] Existing project lifecycle mapped — workflows [`01`](docs/workflows/01-drawing-receipt.md) through [`07`](docs/workflows/07-project-close-out-and-defects.md)

### 4.3 Personas
- [x] Canonical user roles identified — nine personas (P01–P09): Architect, Subcontractor, Project & Commercial Lead, Office & Compliance Coordinator, Site Team, Brand & Content, Finance Director, Directors / MD, Outsourced IT Helpdesk
- [x] Persona card drafted for each (nine total — [`personas.md`](docs/requirements/personas.md))
- [x] Adjacent roles checked (site manager, admin staff, subcontractor admin, external collaborators, internal QS) — covered by P01–P09; external QS consultants treated as invited contacts rather than a separate persona

### 4.4 Business Entities
- [x] All domain entities listed — see [`data-models/entity-relationship.md`](docs/data-models/entity-relationship.md) entity index
- [ ] JSON Schema drafted for each major entity _(written workflow-by-workflow as each moves Draft → In Review)_
- [x] Entity-relationship diagram drawn (first cut, four sub-diagrams) — [`data-models/entity-relationship.md`](docs/data-models/entity-relationship.md)

### 4.5 User Journeys & Workflows
- [x] All major workflows identified (23 — 21 from the JBB audit plus workflows 22 and 23 added on 2026-05-20 — see [`/docs/workflows/`](docs/workflows/))
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
| P01 | [Architect](docs/requirements/personas.md#p01--architect) | External client | Sends tenders with drawings and specs; defines cost codes carried through the system; approves RFIs and variations. | Draft |
| P02 | [Subcontractor](docs/requirements/personas.md#p02--subcontractor) | External delivery | On-site delivery. Returns quotes, updates progress, submits timesheets, raises RFIs, uploads compliance documents. | Draft |
| P03 | [Project & Commercial Lead](docs/requirements/personas.md#p03--project--commercial-lead) | Internal | PM + commercial. Owns the project lifecycle (workflows 02–05, 07) and the internal QS function. | Draft |
| P04 | [Office & Compliance Coordinator](docs/requirements/personas.md#p04--office--compliance-coordinator) | Internal | Owns compliance, comms, materials, fleet, document upkeep. | Draft |
| P05 | [Site Team](docs/requirements/personas.md#p05--site-team) | Internal field | Site managers, foremen, operatives. The capture layer for site reality. | Draft |
| P06 | [Brand & Content](docs/requirements/personas.md#p06--brand--content) | Internal | Marketing and brand custodian across Jewel entities. | Draft |
| P07 | [Finance Director (FD)](docs/requirements/personas.md#p07--finance-director-fd) | Internal executive | Owns finance across BB / PS / PFP. **Drives the primary pain point** (cashflow forecast accuracy). | Draft |
| P08 | [Directors / MD](docs/requirements/personas.md#p08--directors--md) | Internal executive | Executive decisions. Approver on high-value commercial items. | Draft |
| P09 | [Outsourced IT Helpdesk](docs/requirements/personas.md#p09--outsourced-it-helpdesk) | External partner | Tier-1 IT support (target owner of workflow 17 — provider not yet selected). | Draft |

**Status legend:** Draft · In Review · Confirmed

---

## 6. User Journeys & Workflows

Workflows are the cross-actor process maps from the JBB audit (one per file under [`/docs/workflows/`](docs/workflows/)). Journeys are per-persona slices through those workflows where the actor experience deserves its own walkthrough (under [`/docs/user-journeys/`](docs/user-journeys/)). Status moves left-to-right: Draft → In Review → Confirmed.

### 6.1 Workflows (process maps)

| # | Workflow | Group | Owner | h/mo | Status |
|---|---|---|---|---|---|
| 01 | [Drawing Receipt](docs/workflows/01-drawing-receipt.md) | Project lifecycle | P03 PCL | ~15 | Draft |
| 02 | [Tender & BoQ](docs/workflows/02-preconstruction-tender-boq.md) | Project lifecycle | P03 PCL | ~50 | Draft |
| 03 | [Subcontractor Procurement](docs/workflows/03-subcontractor-procurement.md) | Project lifecycle | P03 PCL | ~35 | Draft |
| 04 | [Variations / RFIs / Delays](docs/workflows/04-variations-rfis-delays.md) | Project lifecycle | P03 PCL | ~25 | Draft |
| 05 | [Programme & Valuations](docs/workflows/05-programme-and-valuations.md) | Project lifecycle | P03 PCL | ~10 | Draft |
| 06 | [Site Reporting](docs/workflows/06-site-reporting-and-progress.md) | Project lifecycle | P05 Site | ~25 | Draft |
| 07 | [Close-Out & Defects](docs/workflows/07-project-close-out-and-defects.md) | Project lifecycle | P03 PCL | ~5 | Draft |
| 22 | [Timesheet Management (cost-code-aware)](docs/workflows/22-timesheet-management.md) | Project lifecycle | P03 PCL | tbc | Draft |
| 23 | [Project Completion Settlement & VAT Analysis](docs/workflows/23-project-completion-settlement.md) | Project lifecycle | P07 FD | tbc | Draft |
| 08 | [Subcontractor Compliance](docs/workflows/08-subcontractor-compliance-and-onboarding.md) | Subcontractor | P04 OCC | ~10 | Draft |
| 09 | [Accounts Payable](docs/workflows/09-accounts-payable.md) | Finance | P07 FD | **~80** | Draft |
| 10 | [Accounts Receivable](docs/workflows/10-accounts-receivable.md) | Finance | P07 FD | ~25 | Draft |
| 11 | [Cashflow & Mgmt Reporting](docs/workflows/11-cashflow-and-management-reporting.md) _(primary pain-point anchor)_ | Finance | P07 FD | ~25 | Draft |
| 12 | [Payroll](docs/workflows/12-payroll.md) | Finance | P07 FD | ~10 | Draft |
| 13 | [Accounts Inbox Triage](docs/workflows/13-accounts-inbox-triage.md) | Finance | P07 FD | ~60 | Draft |
| 14 | [Client & Reactive Comms](docs/workflows/14-client-and-reactive-comms.md) | Ops & comms | P04 OCC | ~20 | Draft |
| 15 | [Materials & Deliveries](docs/workflows/15-materials-and-deliveries.md) | Ops & comms | P04 OCC | ~20 | Draft |
| 16 | [HR / Onboarding / IT Access](docs/workflows/16-hr-onboarding-and-it-access.md) | People & systems | P04 OCC / P07 FD | ~10 | Draft |
| 17 | [IT & Systems Admin](docs/workflows/17-it-and-systems-administration.md) | People & systems | P09 IT _(target)_ | ~50 | Draft |
| 18 | [Compliance / Insurance / Accreditation](docs/workflows/18-compliance-insurance-accreditation.md) | People & systems | P04 OCC | ~5 | Draft |
| 19 | [Fleet](docs/workflows/19-fleet-administration.md) | People & systems | P04 OCC | ~3 | Draft |
| 20 | [Marketing & Brand](docs/workflows/20-marketing-and-brand.md) | People & systems | P06 Brand | ~20 | Draft |
| 21 | [Document Management](docs/workflows/21-document-management.md) | People & systems | P04 OCC | ~10 | Draft |

### 6.2 User journeys (per-persona slices)

| # | Journey | Persona | Source workflow | Status |
|---|---|---|---|---|
| 03a | [Subcontractor: receive bid package and return a quote](docs/user-journeys/03a-subcontractor-quote-return.md) | P02 Subcontractor | 03 | Draft |
| 04a | [Architect / CA: respond to an RFI](docs/user-journeys/04a-architect-rfi-response.md) | P01 Architect | 04 | Draft |
| 06a | [Site Team: daily progress capture on mobile](docs/user-journeys/06a-site-team-daily-capture.md) | P05 Site Team | 06 | Draft |
| 08a | [Subcontractor: upload renewed compliance document](docs/user-journeys/08a-subcontractor-compliance-upload.md) | P02 Subcontractor | 08 | Draft |
| 09a | [Finance Director: AP exception review](docs/user-journeys/09a-fd-ap-exception-review.md) | P07 Finance Director | 09 | Draft |
| 11a | [Finance Director: morning cashflow review](docs/user-journeys/11a-fd-cashflow-forecast.md) _(primary pain-point anchor)_ | P07 Finance Director | 11 | Draft |
| 13a | [Finance Director: inbox triage exception review](docs/user-journeys/13a-fd-inbox-triage-exceptions.md) | P07 Finance Director | 13 | Draft |
| 16a | [Coordinator: day-one starter onboarding](docs/user-journeys/16a-coordinator-starter-day-one.md) | P04 Office & Compliance Coordinator | 16 | Draft |

---

## 7. Business Entities

> Surfaced from the 2026-05-18 domain discovery and the 2026-05-20 JBB workflow audit. JSON Schemas are written workflow-by-workflow as each workflow moves Draft → In Review. The first-cut ERD (four sub-diagrams) is in [`data-models/entity-relationship.md`](docs/data-models/entity-relationship.md).

### 7.1 Project lifecycle entities

| Entity | Description | Schema | First surfaced |
|---|---|---|---|
| Project | Unit of work delivered for an architect; central concept. | _to be created_ | All workflows |
| Tender | Package of drawings + specs sent by an Architect. | _to be created_ | 02 |
| Drawing | Construction drawing attached to a tender. | _to be created_ | 01, 02 |
| Drawing Revision | Versioned drawing with supersede logic. | _to be created_ | 01 |
| BoQ | Bill of Quantities (one per project; replaces standalone Excel). | _to be created_ | 02 |
| BoQ Line Item | Discrete unit of priced and tracked work. | _to be created_ | 02, 04, 05 |
| Rate / Rate Library | Pricing source for BoQ; versioned, supplier-linked. | _to be created_ | 02 |
| Cost Code | Architect's client-facing code, referenced throughout. | _to be created_ | 2026-05-18 |
| Bid Package | Trade-scoped bid issued to subcontractors. | _to be created_ | 03 |
| Quote | Subcontractor's returned price against a bid package. | _to be created_ | 03 |
| Work Order | Contract artefact post-award; matching key for AP. | _to be created_ | 03, 07, 09 |
| Variation (VO) | Updates BoQ; rolls up into valuation. | _to be created_ | 04 |
| RFI | Request for Information raised on site. | _to be created_ | 04 |
| NoD | Notice of Delay — formal delay notice. | _to be created_ | 04 |
| Programme Task | Schedule item; tied to BoQ line items. | _to be created_ | 05 |
| Valuation | Monthly project valuation; feeds AR. | _to be created_ | 05 |
| Site Report | Daily capture from site app. | _to be created_ | 06 |
| Defect | Snag register per project. | _to be created_ | 07 |
| Claim Period | Contractual cycle for valuation reporting (typically monthly). | _to be created_ | 05 |
| Cost Code Budget | Per-cost-code budget (allocated / committed / spent / remaining). Arbiter of the workflow 22 hard-block rule. | _to be created_ | 22 |
| Cost Code Allocation | Each timesheet entry's allocation to a cost code. | _to be created_ | 22 |
| Timesheet Approval | Weekly batch approval record. | _to be created_ | 22 |
| Practical Completion | The PC event on a project. Triggers workflows 07 and 23 in parallel. | _to be created_ | 07, 23 |
| Settlement Record | Final audit-grade summary at project close. Triggers retention release. | _to be created_ | 23 |
| VAT Analysis | Zero-rated vs standard-rated breakdown; carries client agreement. | _to be created_ | 23 |

### 7.2 Subcontractor & compliance entities

| Entity | Description | Schema | First surfaced |
|---|---|---|---|
| Subcontractor | Master record with trade tags. | _to be created_ | 03, 08 |
| Compliance Document | Insurance, certs, tickets — with expiry. | _to be created_ | 08 |
| Renewal Event | Generic renewal — used by compliance, fleet, insurance. | _to be created_ | 08, 18, 19 |
| RAMS | Project-specific risk & method statement. | _to be created_ | 08 |
| CIS Status | HMRC verification status. | _to be created_ | 08, 09 |

### 7.3 Finance entities

| Entity | Description | Schema | First surfaced |
|---|---|---|---|
| Supplier | Materials suppliers. | _to be created_ | 09, 15 |
| Supplier Invoice | Captured via Dext, matched to Work Order. | _to be created_ | 09 |
| Sales Invoice | Drafted from valuation / milestone in Xero. | _to be created_ | 10 |
| Cash Call | Request to client for payment, % completion-based _(specialisation of Sales Invoice)_. | _to be created_ | 2026-05-18 |
| Payment Run | Weekly approval bundle. | _to be created_ | 09 |
| Cashflow Forecast | FD's projection across BB/PS/PFP. **Primary pain point.** | _to be created_ | 11, 2026-05-18 |
| Timesheet | Site app + office check-in. | _to be created_ | 06, 12 |
| Inbox Message | Generic inbound email/comm record. | _to be created_ | 01, 13, 14 |
| Inbox Classification | Tag assigned by the AI classifier. | _to be created_ | 13 |
| Statement | Supplier statement for reconciliation. | _to be created_ | 09, 13 |

### 7.4 People, ops & support entities

| Entity | Description | Schema | First surfaced |
|---|---|---|---|
| Organisation | The JBB / Jewel entity (BB, PS, PFP). | _to be created_ | All |
| Person | Internal staff. | _to be created_ | 12, 16, 19 |
| Role | Maps to permission matrix. | _to be created_ | 16 |
| Contract | Generated from role template. | _to be created_ | 16 |
| System Account | Cross-system audit. | _to be created_ | 16, 17 |
| Onboarding Event | Triggers the full orchestration. | _to be created_ | 16 |
| Procurement Request | Project or office materials request. | _to be created_ | 15 |
| Communication Log | Call/email log against project + contact. | _to be created_ | 14 |
| Contact | Lightweight CRM contact. | _to be created_ | 14 |
| Compliance Policy | Insurance, accreditation. | _to be created_ | 18 |
| Accreditation | Tender evidence asset. | _to be created_ | 18 |
| Vehicle | Fleet register. | _to be created_ | 19 |
| Driver Assignment | Person ↔ vehicle. | _to be created_ | 19 |
| Fine | TfL / council. | _to be created_ | 19 |
| Content Item | Marketing post or asset draft. | _to be created_ | 20 |
| Consent Record | Client consent to publish project content. | _to be created_ | 20 |
| Brand Asset | Version-controlled. | _to be created_ | 20 |
| Document | Generic project/corporate doc. | _to be created_ | 21 |
| Folder Template | Auto-creates project folders. | _to be created_ | 21 |

---

## 8. Next Session

- **Date:** _to be confirmed_
- **Attendees:** _to be confirmed_
- **Agenda placeholder:**
  1. Review the draft personas; walk each one with someone in that role where possible.
  2. Resolve the open questions in [`2026-05-18-domain-discovery.md`](docs/meetings/2026-05-18-domain-discovery.md).
  3. Map the **Accountant's cashflow-forecast journey** first — it drives the whole platform's design.
  4. Pick the next two journeys to deep-dive.

Create the meeting note in `/docs/meetings/` from the template **before** the session.

---

## 9. Conventions

- **File names:** lower-kebab-case, with a numeric prefix for ordering where it helps (`01-`, `02-`).
- **Markdown only** for narrative content. Diagrams in Mermaid where possible (renders natively on GitHub).
- **JSON Schema (draft 2020-12)** for entity definitions. Schemas live in `/docs/data-models/` and are referenced by journeys.
- **One PR per journey** during deep-dive sessions, so reviews stay focused.
- **`_templates/` is reference only** — never quoted, never linked from real content.

---

## 10. Quick Links

- [Workflows index](docs/workflows/README.md) — 21 workflow maps from the JBB audit
- [User Journeys index](docs/user-journeys/README.md) — per-persona slices through workflows
- [Personas](docs/requirements/personas.md) · [Permission Matrix](docs/requirements/permission-matrix.md) · [Integrations](docs/requirements/integrations.md) · [Automation-task coverage](docs/requirements/automation-task-coverage.md) · [Glossary](docs/requirements/glossary.md) · [Requirements index](docs/requirements/README.md)
- [UI Components index](docs/ui-components/README.md)
- [Data Models index](docs/data-models/README.md) · [Entity-Relationship Diagram](docs/data-models/entity-relationship.md)
- [Meeting Notes](docs/meetings/README.md) · [Coverage audit + additions 2026-05-20](docs/meetings/2026-05-20-coverage-audit-and-additions.md) · [JBB workflow audit 2026-05-20](docs/meetings/2026-05-20-jbb-workflow-audit.md) · [Domain discovery 2026-05-18](docs/meetings/2026-05-18-domain-discovery.md)
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

Re-ordered on 2026-05-20 to reflect the [JBB workflow audit](docs/meetings/2026-05-20-jbb-workflow-audit.md) recommended phasing: finance first (largest current-hour cost and clearest ROI), then project lifecycle (the JPMS core), then everything else.

#### Platform foundations
1. Wire real OAuth sign-in — Google, Microsoft, and email/password — with invitation-by-admin/PM flow.
2. Stand up the ASP.NET Core API and Azure SQL schema.
3. Replace the allow-list with an admin-managed directory.
4. Move hosting from Static Web Apps to App Service once an API is in place.

#### Phase 1 — Finance ROI
Driven by the FD's load (workflows 09 + 13 alone consume ~140 h/month today).

5. **Workflow 11 — Cashflow & Management Reporting** — the primary pain-point anchor from [2026-05-18](docs/meetings/2026-05-18-domain-discovery.md). Journey [11a](docs/user-journeys/11a-fd-cashflow-forecast.md) is the first deep-dive.
6. **Workflow 09 — Accounts Payable** — single largest workflow (~80 h/mo). Journey [09a](docs/user-journeys/09a-fd-ap-exception-review.md).
7. **Workflow 10 — Accounts Receivable** — closes the loop with valuations and cashflow.
8. **Workflow 13 — Accounts Inbox Triage** — ~60 h/mo today. Journey [13a](docs/user-journeys/13a-fd-inbox-triage-exceptions.md).

#### Phase 2 — JPMS project-lifecycle core
9. **Workflow 03 — Subcontractor Procurement** (~35 h/mo). Journey [03a](docs/user-journeys/03a-subcontractor-quote-return.md).
10. **Workflow 04 — Variations / RFIs / Delays** (~25 h/mo). Journey [04a](docs/user-journeys/04a-architect-rfi-response.md). Now includes explicit VO → bid-package loop into workflow 03.
11. **Workflow 01 — Drawing Receipt & Distribution** (~15 h/mo).
12. **Workflow 05 — Programme & Valuations** — feeds workflow 10 AR. Now produces a named **Programme Valuation Report** per **Claim Period**, with a sibling Variation Orders list.
13. **Workflow 06 — Site Reporting & Progress** — feeds workflow 05 and workflow 12. Journey [06a](docs/user-journeys/06a-site-team-daily-capture.md).
14. **Workflow 22 — Timesheet Management (cost-code-aware)** — feeds 05 (valuations), 09 (AP for subcontractor day-rate), 11 (cashflow forward commitment), 12 (payroll). Carries the cost-code budget hard-block rule.

#### Phase 3 — Everything else
15. Workflows 02, 07, 08, 12, 14, 15, 16, 17, 18, 19, 20, 21 sequenced per stakeholder priority. Journeys [08a](docs/user-journeys/08a-subcontractor-compliance-upload.md) and [16a](docs/user-journeys/16a-coordinator-starter-day-one.md) already drafted.
16. **Workflow 23 — Project Completion Settlement & VAT Analysis** — depends on workflows 03, 05, 07, 09, 22 being in place first. Settles open commercial items at PC, runs zero-rated VAT analysis with client agreement, triggers retention release.

The full phased view (with current-hour cost per workflow) lives in [`docs/workflows/README.md`](docs/workflows/README.md#phased-delivery-from-the-audits-recommended-order).

### 11.7 Running JPMS locally

See [`jpms/README.md`](jpms/README.md) for the full setup. TL;DR:

```bash
cd jpms
dotnet restore
dotnet run
```

Then open the URL the console prints. Try one of the emails in `Services/AllowListUserDirectory.cs` to land on the dashboard, or any other email to see the request-access flow.
