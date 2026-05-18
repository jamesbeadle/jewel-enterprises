# User Journeys

This is the **core artefact** of the scoping process. Every business process is captured here as a journey — a narrative walk-through of what an actor does, what they see, what data is created or changed, and where things can go wrong.

A journey is "done" when the actor in that role has walked through it during an on-site role-play session and signed off the confirmation checklist at the bottom of the file.

> 📁 **`_templates/`** holds reference-only scaffolding. Nothing in `_templates/` is ever treated as project content. Real journeys live in this folder root.

---

## Process for creating a new journey

1. **Capture the source.** Hold a discovery / role-play session and create a meeting note in `/docs/meetings/` first. Real content has a real origin.
2. **Copy the template.** Take [`_templates/journey-template.md`](_templates/journey-template.md) and save it here as `NN-short-kebab-name.md`. Use [`_templates/journey-example.md`](_templates/journey-example.md) as a reference for shape (never for content).
3. **Fill in from the session.** Replace every placeholder with what the actor actually told you. Link `Sourced from:` to the meeting note.
4. **Walk it back.** In the next session, have the same actor walk the journey end-to-end and tick the confirmation checklist.
5. **Update the dashboard.** Add or move the journey's row in the root [`README.md`](../../README.md#5-user-journeys) journey table.

---

## Naming convention

`NN-short-kebab-name.md` where `NN` is a two-digit number giving rough order (group by area, e.g. `0x-` sales, `1x-` projects, `2x-` finance).

---

## Required structure for a journey

Every journey file MUST contain:

1. **Front-matter block** — actors, goal, frequency, success metric, status, `Sourced from:`.
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

| # | Journey | File | Status |
|---|---|---|---|
| _No real journeys yet — created during discovery._ | | | |
