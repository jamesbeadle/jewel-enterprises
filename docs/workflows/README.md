# Workflows

Process diagrams that complement user journeys. Where a journey describes **what one actor experiences**, a workflow describes **what happens across actors and systems** for a given process.

---

## Notation

- **Mermaid** is the default — it renders natively on GitHub and is easy to edit in a meeting.
- **BPMN** (via Camunda Modeler or bpmn.io) for finance / approval flows where formal notation is needed for audit.

---

## Conventions

- File extensions:
  - `.mmd` — Mermaid source (also viewable embedded in `.md`).
  - `.bpmn` — BPMN XML.
- File names: `NN-short-kebab.{mmd|bpmn|md}` mirroring the journey number where applicable.
- Always include a one-paragraph description in a sibling `.md` file or at the top of the source file.

---

## Index

| File | Description |
|---|---|
| [`example-lead-to-deal.mmd`](example-lead-to-deal.mmd) | Worked example — cross-actor flow for the Lead → Won Deal journey. |
