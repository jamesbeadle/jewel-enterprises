# Jewel Enterprises — Business Platform Scoping

> The living scoping dashboard for the Jewel Enterprises master business platform.
> Everything in this repository is **discovery-phase material**: user journeys, personas, workflows, data models, UI components, and meeting decisions.
> No production code lives here yet. Once journeys are signed off, this material becomes the acceptance criteria for development.

---

## 1. Project at a Glance

| | |
|---|---|
| **Vision** | A master operating system for the business, with projects as the central organising concept. |
| **Primary goals** | (1) Smooth project management end-to-end. (2) Robust finance workflows tied to projects. (3) A "second brain" of domain knowledge usable later as an API endpoint by Claude or similar. |
| **Platform** | Progressive Web App (PWA), installable as a desktop/mobile app. |
| **Tech stack** | Blazor (WebAssembly) front-end · ASP.NET Core back-end · Azure hosting · Azure SQL primary database · Microsoft Entra ID for auth · Microsoft Teams / Graph integrations. |
| **Phase** | Discovery & scoping. |
| **Methodology** | User-journey-driven scoping with on-site role-play validation sessions. |

---

## 2. How This Repo is Organised

```
/docs
  /user-journeys     Markdown journeys + interactive demos (the core artefact)
  /ui-components     Atomic-design component library spec
  /workflows         BPMN / Mermaid process diagrams
  /data-models       JSON Schemas + entity-relationship diagrams
  /requirements      Personas, permission matrix, non-functional requirements
  /meetings          Session notes + decisions log
/prototypes          Blazor SPA journey index + HTML demos (deferred — see folder README)
/assets              Screenshots, icons, branding
```

Each folder has its own `README.md` explaining its purpose, the template to follow, and conventions. Start there before adding new files.

---

## 3. Discovery Phase Progress

Tick items off as we go. This is the single source of truth for "how scoped are we?"

### 3.1 Foundation
- [x] GitHub repository created
- [x] Folder structure scaffolded
- [x] Templates and worked examples in place
- [ ] Kick-off meeting with business owner (see `/docs/meetings/`)
- [ ] Stakeholder list confirmed
- [ ] Glossary of business terms started

### 3.2 Current-State Mapping
- [ ] Existing tools and spreadsheets inventoried
- [ ] Pain points captured per role
- [ ] Manual steps and workarounds documented
- [ ] Existing finance processes mapped
- [ ] Existing project lifecycle mapped

### 3.3 Personas
- [ ] All user roles identified
- [ ] Persona card drafted for each (see `/docs/requirements/personas.md`)
- [ ] Each persona reviewed by an actual person in that role

### 3.4 Business Entities
- [ ] All domain entities listed
- [ ] JSON Schema drafted for each major entity
- [ ] Entity-relationship diagram drawn (`/docs/data-models/entity-relationship.md`)

### 3.5 User Journeys
- [ ] All major journeys identified
- [ ] Each journey drafted (Markdown + demo)
- [ ] Edge cases captured per journey
- [ ] Each journey walked through with an on-site stakeholder
- [ ] Confirmation checklist signed off per journey

### 3.6 UI Component Library
- [ ] Atoms inventoried
- [ ] Molecules inventoried
- [ ] Organisms inventoried
- [ ] Page layouts drafted
- [ ] Accessibility notes added per component

### 3.7 Cross-cutting
- [ ] Permission matrix populated (Role × Feature)
- [ ] Non-functional requirements documented (performance, security, reporting, offline)
- [ ] Integration points with Microsoft 365 / Teams documented

### 3.8 Sign-off
- [ ] All journeys confirmed by business owner
- [ ] Data models exported to OpenAPI skeleton
- [ ] Handover document prepared for development

---

## 4. Personas

> Add one row per persona as they emerge. Link to the full card in `/docs/requirements/personas.md`.

| # | Persona | Role summary | Card status | Reviewed by |
|---|---|---|---|---|
| 1 | _Business Owner_ | Strategic oversight, financial sign-off, project pipeline. | Draft | — |
| 2 | _Project Manager_ | Owns project from kickoff to close, runs day-to-day delivery. | Not started | — |
| _..._ | _..._ | _..._ | _..._ | _..._ |

**Status legend:** Not started · Draft · In Review · Confirmed

---

## 5. User Journeys

> One row per journey. Status moves left-to-right: Draft → In Review → Confirmed.

| # | Journey | Primary actor(s) | File | Status | Last reviewed |
|---|---|---|---|---|---|
| 01 | Onboarding a sales lead through to a won deal | Sales Rep, Sales Manager, Finance | [`01-onboarding-sales-lead.md`](docs/user-journeys/01-onboarding-sales-lead.md) | Draft (example) | — |
| 02 | _Project kickoff_ | Project Manager | _to be created_ | Not started | — |
| 03 | _Invoice raise & approval_ | Finance, Project Manager | _to be created_ | Not started | — |

---

## 6. Business Entities

> One row per major domain entity. Link to its JSON Schema once drafted.

| Entity | Description | Schema | Owner |
|---|---|---|---|
| Lead | A potential customer enquiry before qualification. | [`lead.schema.json`](docs/data-models/lead.schema.json) | Sales |
| Project | A unit of work delivered to a customer. The platform's organising concept. | _to be created_ | Delivery |
| Invoice | A billable document raised against a project. | _to be created_ | Finance |
| _..._ | _..._ | _..._ | _..._ |

---

## 7. Next Session

- **Date:** _to be confirmed_
- **Attendees:** _to be confirmed_
- **Agenda placeholder:**
  1. Walk through this scoping dashboard.
  2. Confirm the list of user roles.
  3. Pick the first three journeys to deep-dive.
  4. Schedule the first on-site role-play session.

Capture notes in `/docs/meetings/` using the date-prefixed template.

---

## 8. Conventions

- **File names:** lower-kebab-case, with a numeric prefix for ordering where it helps (`01-`, `02-`).
- **Markdown only** for narrative content. Diagrams in Mermaid where possible (renders natively on GitHub).
- **JSON Schema (draft 2020-12)** for entity definitions. Schemas live in `/docs/data-models/` and are referenced by journeys.
- **One PR per journey** during deep-dive sessions, so reviews stay focused.
- **Confirmation Checklist** at the bottom of every journey file — a journey is not "Confirmed" until that checklist is ticked by the actor.

---

## 9. Quick Links

- [User Journeys index](docs/user-journeys/README.md)
- [UI Components index](docs/ui-components/README.md)
- [Workflows](docs/workflows/README.md)
- [Data Models](docs/data-models/README.md)
- [Requirements & Personas](docs/requirements/README.md)
- [Meeting Notes](docs/meetings/README.md)
- [Prototypes](prototypes/README.md)
- [Assets](assets/README.md)
