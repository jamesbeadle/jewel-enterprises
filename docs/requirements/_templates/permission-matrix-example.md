> ⚠️ **REFERENCE EXAMPLE — not project content.**
> Generic permission matrix for illustration only. Roles, features and access rights here have **not** been confirmed with Jewel Enterprises. Use as scaffolding when building the real matrix.

---

# Permission Matrix (Role × Feature) _(example)_

A coarse-grained matrix of which roles can do what. Detailed per-step permissions live in each journey file; this is the at-a-glance reference.

**Legend:** `C` = create · `R` = read · `U` = update · `D` = delete · `A` = approve · `—` = no access

| Feature / Object                | Sales Rep | Sales Manager | Project Manager | Finance | Business Owner |
|---------------------------------|-----------|---------------|-----------------|---------|----------------|
| Lead                            | CRU (own) | CRUD          | R               | R       | R              |
| Opportunity                     | CRU (own) | CRUD, A       | R               | R       | R, A           |
| Quote                           | CU (own)  | RUA           | R               | R       | A (above £X)   |
| Customer                        | R         | RU            | R               | RU      | R              |
| Project                         | R         | R             | CRUD            | R       | R, A           |
| Project Task                    | —         | —             | CRUD            | —       | R              |
| Invoice                         | —         | R             | R               | CRUD, A | R, A           |
| Reports — Sales pipeline        | R (own)   | R (all)       | —               | R       | R              |
| Reports — Project profitability | —         | —             | R (own)         | R       | R              |
| Admin — Users & roles           | —         | —             | —               | —       | CRUD           |

When the real matrix is built during discovery, create `docs/requirements/permission-matrix.md` (without the `_templates/` prefix) and populate from confirmed roles only.
