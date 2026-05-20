# Meeting: Initial domain discovery — user roles and core pain point

**Date:** 2026-05-18
**Location:** Remote conversation
**Attendees:** Nigel Reilly _(project lead)_, Business Owner

---

## Agenda

1. Establish the business context for Jewel Enterprises.
2. Identify the primary user roles to be served by the initial software.
3. Identify the core pain point driving the initial scope.
4. Agree a working name for the first piece of software.

---

## Notes

- Jewel Enterprises operates in **construction**, delivering work commissioned by architects.
- Architects send Jewel Enterprises **tenders** — packages of drawings and specifications describing what needs to be built. Architects may want to create tenders inside the system themselves; alternatively, Jewel staff add tenders using the architect's documentation.
- Tenders are priced by the **Quantity Surveyor (QS)** into a line-by-line breakdown — the **tender line items**. Line items are the unit of both pricing and progress tracking.
- **Subcontractors** deliver work on site. They:
  - update completion status against line items,
  - submit timesheets,
  - raise **RFIs (Requests for Information)** when scope is unclear or blocked,
  - action **VOs (Variation Orders)** once approved.
- An RFI typically resolves into a VO, which updates line items. Completed VO work is billable, so VOs feed the cash-call procedure.
- Clients are billed via **cash calls** — requests for payment based on the % of work completed.
- The **Accountant** produces a **cashflow forecast** for the MD, derived from the same project / line-item data.
- The **MD (Managing Director)** makes executive decisions across the business.
- Architects often carry **cost codes** — client-facing references — that must propagate through every screen and report touching that architect's tender.

---

## Decisions

| # | Decision | Owner | Date |
|---|---|---|---|
| D1 | Initial scope = a Manage the programme of work for each project from tender through delivery to cash call. "Program" = the program of work delivered for the architect. | Business Owner | 2026-05-18 |
| D2 | Five primary user roles for the initial system: **Architect, Quantity Surveyor, Subcontractor, Accountant, Managing Director**. | Business Owner | 2026-05-18 |
| D3 | The Accountant's cashflow forecast accuracy is the **primary pain point** driving the initial software design. Every scoping decision should be tested against: "does this help the Accountant produce an accurate forecast?" | Business Owner | 2026-05-18 |
| D4 | Browse-by-user is the navigation pattern for the prototype Journey Index — each user gets a dedicated page listing the journeys they participate in. | Nigel | 2026-05-18 |

---

## Action items

- [ ] Validate the **Architect** persona with an external architect Jewel works with — **Nigel**, due tbd
- [ ] Validate the **QS** persona with the on-site QS — **Nigel**, due tbd
- [ ] Validate the **Subcontractor** persona by shadowing one on site — **Nigel**, due tbd
- [ ] Validate the **Accountant** persona with the on-site accountant — **Nigel**, due tbd
- [ ] Validate the **MD** persona with the Business Owner — **Nigel**, due tbd
- [ ] Confirm the full entity list and relationships in a follow-up session (Tender, Line Item, RFI, VO, Cash Call, Timesheet, Cost Code, Project, etc.) — **Nigel**, due tbd

---

## Open questions raised

- [ ] The phrase _"The tender line items"_ was started but not finished in this conversation. What was the intended detail?
- [ ] Are there other user roles in the initial system (site managers, office / admin staff, subcontractor admins, external collaborators)?
- [ ] What systems are currently in use for: accounting, drawing storage, timesheets, RFI / VO tracking?
- [ ] How are RFIs raised and tracked today? Email, paper, spreadsheet?
- [ ] How are cost codes structured by architects — is there a standard format or does each architect use their own?
- [ ] What approval thresholds apply to VOs? Who signs them off (QS, MD, architect)?
- [ ] Is the Accountant role one person across all projects, or per project / per region?
- [ ] Is the QS internal to Jewel Enterprises, or sometimes external / consultant?
- [ ] What is the typical cash-call cadence (monthly? per milestone?) and who triggers it — the Accountant from data, or the MD?

---

## Artefacts updated

- Created `/docs/requirements/personas.md` with 5 personas (P01–P05), all status **Draft**.
- Created `/docs/requirements/glossary.md` with construction-specific terms.
- Updated root `README.md` — added personas to the dashboard, surfaced the "JPMS" working name, ticked progress boxes for foundation and persona identification.
- Updated `/docs/requirements/README.md` — `personas.md` and `glossary.md` removed from "to be created" list.
- Seeded the Blazor prototype `/prototypes/journey-index/` with the persona data (`Models/Persona.cs`, `Data/Personas.cs`), reworked the home page with a Users grid, and added per-user pages at `/users/{slug}`.
