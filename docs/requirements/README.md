# Requirements

Cross-cutting requirements that are not specific to a single journey.

> 📁 **`_templates/`** holds reference-only scaffolding (persona card template, example permission matrix). Real requirements live in this folder root.

---

## Files (created during discovery, not before)

- `personas.md` — Persona cards for every confirmed user role. Built using the card template in [`_templates/personas-template.md`](_templates/personas-template.md). Each card has a `Sourced from:` link to the meeting where it was captured.
- `permission-matrix.md` — Role × Feature matrix. Shape reference in [`_templates/permission-matrix-example.md`](_templates/permission-matrix-example.md).
- `non-functional.md` — performance, security, reporting, offline behaviour, audit, retention. _(to be created)_
- `integrations.md` — Microsoft 365, Teams, Graph, Power BI, accounting system. _(to be created)_
- `glossary.md` — business terms that appear in the platform. _(to be created)_

A requirement is "Confirmed" once it has been referenced from at least one journey and signed off by the relevant actor in the role-play session for that journey.

---

## Process for adding a persona

1. Hold the persona conversation with someone in that role (or shadow them).
2. Capture the conversation in a meeting note under `/docs/meetings/`.
3. If `personas.md` doesn't exist yet, create it using the template in `_templates/personas-template.md`.
4. Add the card. Status starts as **Draft** and becomes **Confirmed** only when the person reviews their own card.
5. Update the personas table in the root [`README.md`](../../README.md#4-personas).
