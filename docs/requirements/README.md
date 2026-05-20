# Requirements

Cross-cutting requirements that are not specific to a single journey.

> 📁 **`_templates/`** holds reference-only scaffolding (persona card template, example permission matrix). Real requirements live in this folder root.

---

## Files

- [`personas.md`](personas.md) — Persona cards for every user role. Nine canonical cards (P01–P09).
- [`glossary.md`](glossary.md) — Construction and Jewel-Enterprises-specific terms (Tender, RFI, VO, Cash Call, Cost Code, etc.).
- [`permission-matrix.md`](permission-matrix.md) — Role × Workflow RBAC matrix (coarse). Drafted from the workflow audit; refined per workflow as deep-dives confirm per-step CRUD.
- [`integrations.md`](integrations.md) — Catalogue of every external/adjacent system named across the twenty-one workflows, with direction, target status (keep/replace/archive), and the phase-1 shortlist.
- _(to be created)_ `non-functional.md` — performance, security, reporting, offline behaviour, audit, retention.

A requirement is "Confirmed" once it has been referenced from at least one journey and signed off by the relevant actor in the role-play session for that journey.

---

## Process for adding a persona

1. Hold the persona conversation with someone in that role (or shadow them).
2. Capture the conversation in a meeting note under `/docs/meetings/`.
3. Add or update the relevant card in `personas.md` using the structure from `_templates/personas-template.md`. Status starts as **Draft** and becomes **Confirmed** only when the person reviews their own card.
4. Update the personas table in the root [`README.md`](../../README.md#5-personas).
