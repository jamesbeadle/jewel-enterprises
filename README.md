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
    /_templates      Reference-only scaffolding
  /ui-components     Atomic-design component library spec
    /_templates      Reference-only scaffolding
  /workflows         BPMN / Mermaid process diagrams
    /_templates      Reference-only scaffolding
  /data-models       JSON Schemas + entity-relationship diagrams
    /_templates      Reference-only scaffolding
  /requirements      Personas, permission matrix, non-functional requirements
    /_templates      Reference-only scaffolding
  /meetings          Session notes + decisions log
    /_templates      Reference-only scaffolding
/prototypes          Blazor SPA journey index + HTML demos (deferred — see folder README)
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
- [ ] Kick-off meeting with business owner (capture in `/docs/meetings/`)
- [ ] Stakeholder list confirmed
- [ ] Glossary of business terms started

### 4.2 Current-State Mapping
- [ ] Existing tools and spreadsheets inventoried
- [ ] Pain points captured per role
- [ ] Manual steps and workarounds documented
- [ ] Existing finance processes mapped
- [ ] Existing project lifecycle mapped

### 4.3 Personas
- [ ] All user roles identified
- [ ] Persona card drafted for each (in `/docs/requirements/personas.md`)
- [ ] Each persona reviewed by an actual person in that role

### 4.4 Business Entities
- [ ] All domain entities listed
- [ ] JSON Schema drafted for each major entity
- [ ] Entity-relationship diagram drawn (`/docs/data-models/entity-relationship.md`)

### 4.5 User Journeys
- [ ] All major journeys identified
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

### 4.8 Sign-off
- [ ] All journeys confirmed by business owner
- [ ] Data models exported to OpenAPI skeleton
- [ ] Handover document prepared for development

---

## 5. Personas

> Add one row per persona as they emerge from discovery. Each row links to the card inside `/docs/requirements/personas.md` once that file exists.

| # | Persona | Role summary | Card status | Reviewed by |
|---|---|---|---|---|
| _No personas yet — captured during discovery sessions._ | | | | |

**Status legend:** Draft · In Review · Confirmed

---

## 6. User Journeys

> One row per journey. Status moves left-to-right: Draft → In Review → Confirmed.

| # | Journey | Primary actor(s) | File | Status | Last reviewed |
|---|---|---|---|---|---|
| _No journeys yet — captured during discovery sessions._ | | | | | |

---

## 7. Business Entities

> One row per major domain entity. Link to its JSON Schema in `/docs/data-models/` once drafted.

| Entity | Description | Schema | Owner |
|---|---|---|---|
| _No entities yet — uncovered as journeys are written._ | | | |

---

## 8. Next Session

- **Date:** _to be confirmed_
- **Attendees:** _to be confirmed_
- **Agenda placeholder:**
  1. Walk through this scoping dashboard.
  2. Confirm the list of user roles.
  3. Pick the first three journeys to deep-dive.
  4. Schedule the first on-site role-play session.

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
- [UI Components index](docs/ui-components/README.md)
- [Workflows](docs/workflows/README.md)
- [Data Models](docs/data-models/README.md)
- [Requirements & Personas](docs/requirements/README.md)
- [Meeting Notes](docs/meetings/README.md)
- [Prototypes](prototypes/README.md)
- [Assets](assets/README.md)
