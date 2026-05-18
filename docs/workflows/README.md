# Workflows

Process diagrams that complement user journeys. Where a journey describes **what one actor experiences**, a workflow describes **what happens across actors and systems** for a given process.

> 📁 **`_templates/`** holds reference-only example diagrams. Real workflows live in this folder root.

---

## Notation

- **Mermaid** is the default — it renders natively on GitHub and is easy to edit in a meeting.
- **BPMN** (via Camunda Modeler or bpmn.io) for finance / approval flows where formal notation is needed for audit.

---

## Process for adding a real workflow

1. A workflow is usually identified while writing or reviewing a journey. Capture which meeting it came from.
2. Look at [`_templates/workflow-example.mmd`](_templates/workflow-example.mmd) for the shape of a Mermaid flowchart — reference only.
3. Create the new file in this folder using the same number prefix as the journey it supports (e.g. `01-{slug}.mmd` for the journey `01-{slug}.md`).
4. Add a one-paragraph description in a sibling `.md` file or at the top of the source file.

---

## Conventions

- File extensions:
  - `.mmd` — Mermaid source (also viewable embedded in `.md`).
  - `.bpmn` — BPMN XML.
- File names: `NN-short-kebab.{mmd|bpmn|md}` mirroring the journey number where applicable.

---

## Index

| File | Description |
|---|---|
| _No real workflows yet — created alongside journeys during discovery._ | |
