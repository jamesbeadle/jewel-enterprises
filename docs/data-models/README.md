# Data Models

JSON Schemas (draft 2020-12) for every major business entity, plus an entity-relationship diagram that ties them together.

Once journeys are signed off, these schemas are exported as the basis of:

- OpenAPI request/response shapes
- Database table definitions (Azure SQL)
- Contract tests for the eventual API

> 📁 **`_templates/`** holds reference-only example schemas. Real schemas live in this folder root.

---

## Process for adding a real entity

1. **Identify the entity during a journey.** The journey's "Data structures" section names it.
2. **Capture the source meeting note** in the schema's `description`.
3. **Copy the shape** of [`_templates/entity-schema-example.json`](_templates/entity-schema-example.json) — never copy fields verbatim.
4. **Create** `{entity}.schema.json` in this folder (e.g. `project.schema.json`).
5. **Update** the entity-relationship diagram (see below) and the root [`README.md`](../../README.md#6-business-entities) entities table.

---

## Conventions

- One file per entity: `{entity}.schema.json` (e.g. `lead.schema.json`, `project.schema.json`).
- Use `$id`, `title`, `description` on every schema.
- Use `$ref` between schemas rather than copy-pasting nested shapes.
- Enums live alongside the schema that owns them, unless reused — then promote to `enums/`.
- A short `.md` companion file is allowed for any schema that needs narrative explanation beyond `description` fields.

---

## Entity Relationships

Once at least two real entities are confirmed, create `entity-relationship.md` in this folder with a Mermaid `erDiagram`. Use the shape of [`_templates/entity-relationship-example.md`](_templates/entity-relationship-example.md) — reference only.

---

## Entity index

| Entity | Schema | Description |
|---|---|---|
| _No real entities yet — added as journeys uncover them._ | | |
