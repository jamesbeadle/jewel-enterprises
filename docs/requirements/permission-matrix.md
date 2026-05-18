# Permission Matrix (Role × Feature)

A coarse-grained matrix of which roles can do what. Detailed per-step permissions live in each journey file; this is the at-a-glance reference.

**Legend:** `C` = create · `R` = read · `U` = update · `D` = delete · `A` = approve · `—` = no access

> Worked example — replace with the real role list as personas are confirmed.

| Feature / Object              | Sales Rep | Sales Manager | Project Manager | Finance | Business Owner |
|-------------------------------|-----------|---------------|-----------------|---------|----------------|
| Lead                          | CRU (own) | CRUD          | R               | R       | R              |
| Opportunity                   | CRU (own) | CRUD, A       | R               | R       | R, A           |
| Quote                         | CU (own)  | RUA           | R               | R       | A (above £X)   |
| Customer                      | R         | RU            | R               | RU      | R              |
| Project                       | R         | R             | CRUD            | R       | R, A           |
| Project Task                  | —         | —             | CRUD            | —       | R              |
| Invoice                       | —         | R             | R               | CRUD, A | R, A           |
| Reports — Sales pipeline      | R (own)   | R (all)       | —               | R       | R              |
| Reports — Project profitability | —       | —             | R (own)         | R       | R              |
| Admin — Users & roles         | —         | —             | —               | —       | CRUD           |

## Open questions

- [ ] Confirm approval thresholds for quotes and invoices.
- [ ] Are there read-only auditor roles needed for compliance?
- [ ] How are external collaborators (subcontractors, customers) handled?
