# User Journeys

This is the **core artefact** of the scoping process. Every business process is captured here as a journey — a narrative walk-through of what an actor does, what they see, what data is created or changed, and where things can go wrong.

A journey is "done" when the actor in that role has walked through it during an on-site role-play session and signed off the confirmation checklist at the bottom of the file.

> 📁 **`_templates/`** holds reference-only scaffolding. Nothing in `_templates/` is ever treated as project content. Real journeys live in this folder root.

---

## Process for creating a new journey

1. **Capture the source.** Hold a discovery / role-play session and create a meeting note in `/docs/meetings/` first. Real content has a real origin.
2. **Copy the template.** Take [`_templates/journey-template.md`](_templates/journey-template.md) and save it here as `NN-short-kebab-name.md`. Use [`_templates/journey-example.md`](_templates/journey-example.md) as a reference for shape (never for content).
3. **Fill in.** Replace every placeholder with the actual scope decision.
4. **Walk it back.** In the next session, have the same actor walk the journey end-to-end and tick the confirmation checklist.
5. **Update the dashboard.** Add or move the journey's row in the root [`README.md`](../../README.md#5-user-journeys) journey table.

---

## Naming convention

`NN-short-kebab-name.md` where `NN` is a two-digit number giving rough order (group by area, e.g. `0x-` sales, `1x-` projects, `2x-` finance).

---

## Required structure for a journey

Every journey file MUST contain:

1. **Front-matter block** — actors, goal, frequency, success metric, status.
2. **Trigger** — what kicks the journey off.
3. **Steps** — numbered, each with: UI demo link, screenshot, fields/validation, decision points.
4. **Edge cases & exceptions** — captured live during role-play.
5. **Data structures** — JSON snippets referencing schemas in `/docs/data-models/`.
6. **Permissions** — which roles can do what at each step.
7. **Open questions** — anything we couldn't answer in the room.
8. **Confirmation checklist** — signed off by the actor.

---

## Demo links

Each journey can have an interactive demo. During scoping these can be:

- Plain HTML mockups in `./demos/`
- Figma frame links
- Later, Blazor pages in `/prototypes/blazor-journey-index/`

Always provide a screenshot too, in case the demo isn't reachable.

---

## Status legend

- **Not started** — no file
- **Draft** — first pass written, not reviewed
- **In Review** — walkthrough scheduled or in progress
- **Confirmed** — sign-off ticked at the bottom of the file

Update the journey row in the root [`README.md`](../../README.md#5-user-journeys) whenever status changes.

---

## Index

Five journeys cover the highest-value actor slices through the eleven JPMS workflows. Each journey is a persona slice through a workflow in [`/docs/workflows/`](../workflows/); the journey number takes the workflow number plus a letter (`a`, `b`, …) so the link back to the workflow is unambiguous.

| # | Journey | Persona | Source workflow | Status |
|---|---|---|---|---|
| 03a | Subcontractor: receive bid package and return a quote | P02 Subcontractor | [03](../workflows/03-subcontractor-procurement.md) | Draft |
| 04a | Architect / CA: respond to an RFI | P01 Architect | [04](../workflows/04-variations-rfis-delays.md) | Draft |
| 06a | Site Team: daily progress capture on mobile | P05 Site Team | [06](../workflows/06-site-reporting-and-progress.md) | Draft |
| 08a | Subcontractor: upload renewed compliance document | P02 Subcontractor | [08](../workflows/08-subcontractor-compliance-and-onboarding.md) | Draft |
| 10a | Finance Director: morning cashflow review _(primary pain-point anchor)_ | P06 Finance Director | [10](../workflows/10-cashflow-and-project-forecasting.md) | Draft |

> Other workflows can grow journey slices as deep-dives surface non-obvious actor cuts. The above are the journeys where capturing the actor's experience separately is materially more valuable than reading the workflow file directly.
