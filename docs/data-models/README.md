# Data Models — Domain Concepts

JSON Schemas (draft 2020-12) for every domain concept in JPMS, plus an entity-relationship diagram that ties them together. These are the shared vocabulary the business and the system both use.

Once journeys are signed off, these schemas are exported as the basis of:

- OpenAPI request/response shapes
- Database table definitions (Azure SQL)
- Contract tests for the eventual API

> 📁 **`_templates/`** holds reference-only example schemas. Real schemas live in this folder root.

---

## Process for adding a real entity

1. **Identify the entity during a workflow or journey.** The workflow's "Entities touched" section names it.
2. **Copy the shape** of [`_templates/entity-schema-example.json`](_templates/entity-schema-example.json) — never copy fields verbatim.
3. **Create** `{entity}.schema.json` in this folder (e.g. `project.schema.json`).
4. **Update** the entity-relationship diagram (see below) and the root [`README.md`](../../README.md#7-business-entities) entities table.

---

## Conventions

- One file per entity: `{entity}.schema.json` (e.g. `lead.schema.json`, `project.schema.json`).
- Use `$id`, `title`, `description` on every schema.
- Use `$ref` between schemas rather than copy-pasting nested shapes.
- Enums live alongside the schema that owns them, unless reused — then promote to `enums/`.
- A short `.md` companion file is allowed for any schema that needs narrative explanation beyond `description` fields.

---

## Entity Relationships

First-cut ERD is in [`entity-relationship.md`](entity-relationship.md). The diagram is split into three sub-diagrams (project lifecycle; subcontractor & compliance; timesheets, cashflow & settlement) for legibility. Schemas are written workflow-by-workflow as each workflow moves Draft → In Review.

---

## Entity index

Full surfaced-entity list is in [`entity-relationship.md`](entity-relationship.md). Schemas are added here as each one is written.

| Entity | Schema | Description |
|---|---|---|
| _Schemas to be written workflow-by-workflow. See [`entity-relationship.md`](entity-relationship.md) for the full list of surfaced entities._ | | |
