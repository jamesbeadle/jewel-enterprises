# User Journeys

This is the **core artefact** of the scoping process. Every business process is captured here as a journey — a narrative walk-through of what an actor does, what they see, what data is created or changed, and where things can go wrong.

A journey is "done" when the actor in that role has walked through it during an on-site role-play session and signed off the confirmation checklist at the bottom of the file.

---

## Naming convention

`NN-short-kebab-name.md` where `NN` is a two-digit number giving rough order (group by area, e.g. `0x-` sales, `1x-` projects, `2x-` finance).

Example: `01-onboarding-sales-lead.md`, `11-project-kickoff.md`, `21-invoice-approval.md`.

---

## Template

Copy `_template.md` (or the worked example `01-onboarding-sales-lead.md`) and fill it in. Every journey file MUST contain:

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

- **Not started** — file doesn't exist yet
- **Draft** — first pass written, not reviewed
- **In Review** — walkthrough scheduled or in progress
- **Confirmed** — sign-off ticked at the bottom of the file

Update the journey row in the root [`README.md`](../../README.md#5-user-journeys) whenever status changes.

---

## Index

| # | Journey | File | Status |
|---|---|---|---|
| 01 | Onboarding a sales lead through to a won deal | [`01-onboarding-sales-lead.md`](01-onboarding-sales-lead.md) | Draft (worked example) |
