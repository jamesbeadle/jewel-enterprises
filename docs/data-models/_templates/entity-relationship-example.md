> ⚠️ **REFERENCE EXAMPLE — not project content.**
> Generic ERD for illustration only. Entities, attributes and relationships here have **not** been confirmed with Jewel Enterprises. Use as scaffolding when drawing the real ERD.

---

# Entity Relationship Diagram _(example)_

```mermaid
erDiagram
    LEAD ||--o| OPPORTUNITY : "qualifies into"
    OPPORTUNITY ||--o| PROJECT : "becomes (on won)"
    PROJECT ||--o{ INVOICE : "raises"
    PROJECT ||--o{ TASK : "contains"
    CUSTOMER ||--o{ PROJECT : "owns"
    CUSTOMER ||--o{ LEAD : "originated as"

    LEAD {
        string id PK
        string source
        string status
        datetime createdAt
    }
    PROJECT {
        string id PK
        string customerId FK
        string status
        decimal budget
    }
    INVOICE {
        string id PK
        string projectId FK
        decimal amount
        string status
    }
```

When real entities are confirmed, create `docs/data-models/entity-relationship.md` (without the `_templates/` prefix) and build the diagram from confirmed entities only.
