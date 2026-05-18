# Entity Relationship Diagram

Add entities and relationships to the diagram below as they are confirmed during scoping.

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

> This is a **starting sketch** — refine as entities are confirmed. Treat each relationship as a question for the next role-play session.
