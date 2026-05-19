# Jewel Enterprises — Business Platform Scoping

> The living scoping dashboard for the Jewel Enterprises master business platform.
> Everything in this repository is **discovery-phase material**: user journeys, personas, workflows, data models, UI components, and meeting decisions.
> No production code lives here yet. Once journeys are signed off, this material becomes the acceptance criteria for development.

---

## 1. Project at a Glance

| | |
|---|---|
| **Vision** | A master operating system for the business, with projects as the central organising concept. |
| **Working name (initial software)** | **Project Program Scheduler** — manage the program of work for each project from tender through delivery to cash call. |
| **Business context** | Construction. Jewel Enterprises delivers work commissioned by architects. See [`/docs/requirements/glossary.md`](docs/requirements/glossary.md) for domain terms (Tender, RFI, VO, Cash Call, etc.). |
| **Primary goals** | (1) Smooth project management end-to-end. (2) Robust finance workflows tied to projects — **the Accountant's cashflow forecast accuracy is the primary pain point driving scope.** (3) A "second brain" of domain knowledge usable later as an API endpoint by Claude or similar. |
| **Platform** | Progressive Web App (PWA), installable as a desktop/mobile app. |
| **Tech stack** | Blazor (WebAssembly) front-end · ASP.NET Core back-end · Azure hosting · Azure SQL primary database · Microsoft Entra ID for auth · Microsoft Teams / Graph integrations. |
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
/jpms                JPMS — production Blazor WebAssembly client app (see §11)
/assets              Screenshots, icons, branding
```

Each folder has its own `README.md` explaining its purpose, its `_templates/` reference, and the process for adding real content. Start there before adding new files.

---

## 3. Templates vs project content — the rule

This repository deliberately separates **reference scaffolding** from **confirmed project content**:

- 📁 **`_templates/`** subfolders in every section hold blank templates and worked examples. They exist so we know *what shape* a journey, persona, schema, or component spec should take. **They are reference only.** Nothing in `_templates/` is ever treated as a decision about Jewel Enterprises and should never be quoted in a stakeholder session as if it were.
- 📁 **The section root** (e.g. `/docs/user-journeys/`) holds **real content** only — material that came out of a discovery or role-play session with someone who actually does the work.
- 🔗 **Every real file has a `Sourced from:` line** pointing at the meeting note that captured it. If a file has no source, it's an assumption — not a decision.

When a real journey/persona/component is created, it's copied **from** a template, populated **from** a meeting note, and lives **outside** `_templates/`.

---

## 4. Discovery Phase Progress

Tick items off as we go. This is the single source of truth for "how scoped are we?"

### 4.1 Foundation
- [x] GitHub repository created
- [x] Folder structure scaffolded
- [x] Templates and worked examples in place under `_templates/`
- [x] Kick-off / domain discovery meeting captured ([`2026-05-18-domain-discovery.md`](docs/meetings/2026-05-18-domain-discovery.md))
- [x] Working name agreed: **Project Program Scheduler**
- [ ] Stakeholder list confirmed (names against each role)
- [x] Glossary of business terms started ([`glossary.md`](docs/requirements/glossary.md))

### 4.2 Current-State Mapping
- [x] Primary pain point identified — cashflow forecast accuracy (Accountant)
- [ ] Existing tools and spreadsheets inventoried
- [ ] Pain points captured per role
- [ ] Manual steps and workarounds documented
- [ ] Existing finance processes mapped (cash calls, allocation, forecast cadence)
- [ ] Existing project lifecycle mapped (tender → line items → delivery → cash call)

### 4.3 Personas
- [x] Initial user roles identified — Architect, QS, Subcontractor, Accountant, MD
- [x] Persona card drafted for each ([`personas.md`](docs/requirements/personas.md))
- [ ] Each persona reviewed by an actual person in that role
- [ ] Other roles checked (site manager, admin staff, subcontractor admin, external collaborators)

### 4.4 Business Entities
- [ ] All domain entities listed (Tender, Line Item, RFI, VO, Cash Call, Cost Code, Timesheet, Project, Drawing, Cashflow Forecast)
- [ ] JSON Schema drafted for each major entity
- [ ] Entity-relationship diagram drawn ([`data-models/entity-relationship.md`](docs/data-models/) — to be created)

### 4.5 User Journeys
- [ ] All major journeys identified (one list per persona)
- [ ] Each journey drafted (Markdown + demo)
- [ ] Edge cases captured per journey
- [ ] Each journey walked through with an on-site stakeholder
- [ ] Confirmation checklist signed off per journey

### 4.6 UI Component Library
- [ ] Atoms inventoried
- [ ] Molecules inventoried
- [ ] Organisms inventoried
- [ ] Page layouts drafted
- [ ] Accessibility notes added per component

### 4.7 Cross-cutting
- [ ] Permission matrix populated (Role × Feature)
- [ ] Non-functional requirements documented (performance, security, reporting, offline)
- [ ] Integration points with Microsoft 365 / Teams documented
- [ ] Cost-code propagation rules captured (must follow every architect's tender end-to-end)

### 4.8 Sign-off
- [ ] All journeys confirmed by business owner
- [ ] Data models exported to OpenAPI skeleton
- [ ] Handover document prepared for development

---

## 5. Personas

> Each row links to its card in [`docs/requirements/personas.md`](docs/requirements/personas.md).

| # | Persona | Role summary | Card status | Reviewed by |
|---|---|---|---|---|
| P01 | [Architect](docs/requirements/personas.md#p01--architect) | External client who sends tenders with drawings and specs; defines cost codes carried through the system. | Draft | — |
| P02 | [Quantity Surveyor](docs/requirements/personas.md#p02--quantity-surveyor-qs) | Prices tenders into line items; captures site measurements; updates line items on VOs. | Draft | — |
| P03 | [Subcontractor](docs/requirements/personas.md#p03--subcontractor) | On-site delivery. Updates line-item completion, submits timesheets, raises RFIs, actions VOs. | Draft | — |
| P04 | [Accountant](docs/requirements/personas.md#p04--accountant) | Produces cashflow forecast; issues cash calls; allocates incoming cash. **Drives the primary pain point.** | Draft | — |
| P05 | [Managing Director](docs/requirements/personas.md#p05--managing-director-md) | Executive decisions. Consumes forecast and project status. | Draft | — |

**Status legend:** Draft · In Review · Confirmed

---

## 6. User Journeys

> One row per journey. Status moves left-to-right: Draft → In Review → Confirmed.

| # | Journey | Primary actor(s) | File | Status | Last reviewed |
|---|---|---|---|---|---|
| _No journeys yet — captured during discovery sessions, grouped by persona in the prototype Journey Index._ | | | | | |

---

## 7. Business Entities

> Captured so far from the domain discovery. JSON Schemas will be created as journeys define them precisely.

| Entity | Description | Schema | Owner |
|---|---|---|---|
| Tender | Package of drawings + specs sent by an Architect. | _to be created_ | Architect / QS |
| Tender Line Item | Discrete unit of priced work within a tender. | _to be created_ | QS / Subcontractor |
| RFI | Request for Information raised on site. | _to be created_ | Subcontractor / QS |
| VO | Variation Order — updates to line items, billable when done. | _to be created_ | QS |
| Cash Call | Request to client for payment, % completion-based. | _to be created_ | Accountant |
| Cost Code | Architect's client-facing code, referenced throughout. | _to be created_ | Architect |
| Timesheet | Subcontractor time record. | _to be created_ | Subcontractor |
| Project | Unit of work delivered for an architect; central concept. | _to be created_ | All |
| Drawing | Construction drawing attached to a tender. | _to be created_ | Architect / QS |
| Cashflow Forecast | Accountant's projection for the MD. | _to be created_ | Accountant |

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
- **Confirmation Checklist** at the bottom of every journey file — a journey is not "Confirmed" until that checklist is ticked by the actor.
- **`Sourced from:`** line on every real artefact, pointing at the meeting note behind it.
- **`_templates/` is reference only** — never quoted, never linked from real content as a source.

---

## 10. Quick Links

- [User Journeys index](docs/user-journeys/README.md)
- [Personas](docs/requirements/personas.md) · [Glossary](docs/requirements/glossary.md) · [Requirements index](docs/requirements/README.md)
- [UI Components index](docs/ui-components/README.md)
- [Workflows](docs/workflows/README.md)
- [Data Models](docs/data-models/README.md)
- [Meeting Notes](docs/meetings/README.md) · [Latest: domain discovery 2026-05-18](docs/meetings/2026-05-18-domain-discovery.md)
- [Prototype Journey Index](prototypes/journey-index/README.md)
- [JPMS — production app](jpms/README.md)
- [Assets](assets/README.md)

---

## 11. Production — JPMS (Jewel Project Management System)

> Scoping happens in `/docs` and `/prototypes`. The **actual product** is being built in [`/jpms`](jpms/README.md).
> Read this section before touching anything in that folder.

### 11.1 What JPMS is

**JPMS — Jewel Project Management System** — is the production web app the company and its clients will use day-to-day. It's a multi-tenant Blazor WebAssembly PWA. Each Jewel client (architect firms, QSs, subcontractors, the internal team) signs in with their own work identity and sees only the projects they belong to.

Working name from the scoping repo is *Project Program Scheduler*; the product brand is **JPMS**.

### 11.2 Where it differs from `/prototypes`

| | `/prototypes/journey-index` | `/jpms` |
|---|---|---|
| Purpose | Scoping & role-play walkthroughs | The real product |
| Audience | Internal stakeholders during discovery sessions | Jewel staff + external clients |
| Lifetime | Disposable | Long-lived |
| Auth | None | Microsoft + Google sign-in, gated by an internal directory |
| Data | Hard-coded in `.cs` files | ASP.NET Core API + Azure SQL (to be added) |

The two share a deliberate **visual language** — slate palette, rounded cards, same typography stack — so design learnings from the prototype carry over without rework.

### 11.3 Tech stack

- **Front-end:** Blazor WebAssembly · .NET 8 LTS · Tailwind (CDN today, CLI build before launch)
- **PWA:** installable on desktop and mobile, theme `#0f172a`
- **Auth (target):** Microsoft Entra ID (MSAL) + Google Identity Services
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

1. Wire real Microsoft + Google sign-in.
2. Stand up the ASP.NET Core API and Azure SQL schema.
3. Replace the allow-list with an admin-managed directory.
4. Build the **Accountant's cashflow-forecast journey** first (the primary pain point — see §1).
5. Build out the journeys signed off in `/docs/user-journeys/` in priority order.
6. Move hosting from Static Web Apps to App Service once an API is in place.

### 11.7 Running JPMS locally

See [`jpms/README.md`](jpms/README.md) for the full setup. TL;DR:

```bash
cd jpms
dotnet restore
dotnet run
```

Then open the URL the console prints. Try one of the emails in `Services/AllowListUserDirectory.cs` to land on the dashboard, or any other email to see the request-access flow.
