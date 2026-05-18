# Data Models

JSON Schemas (draft 2020-12) for every major business entity, plus an entity-relationship diagram that ties them together.

Once journeys are signed off, these schemas are exported as the basis of:

- OpenAPI request/response shapes
- Database table definitions (Azure SQL)
- Contract tests for the eventual API

---

## Conventions

- One file per entity: `{entity}.schema.json` (e.g. `lead.schema.json`, `project.schema.json`).
- Use `$id`, `title`, `description` on every schema.
- Use `$ref` between schemas rather than copy-pasting nested shapes.
- Enums live alongside the schema that owns them, unless reused — then promote to `enums/`.
- A short `.md` companion file is allowed for any schema that needs narrative explanation beyond `description` fields.

---

## Entity index

| Entity | Schema | Description |
|---|---|---|
| Lead | [`lead.schema.json`](lead.schema.json) | A potential customer enquiry before qualification. |

---

## Entity Relationships

The ERD lives in [`entity-relationship.md`](entity-relationship.md) as a Mermaid `erDiagram`. Update it whenever a new entity is added.
