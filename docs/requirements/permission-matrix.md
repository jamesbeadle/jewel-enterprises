# Permission Matrix (Role × Workflow)

Coarse-grained matrix of which role is responsible for what across the twenty-one workflows. This is the at-a-glance reference. Per-step CRUD permissions live in each workflow file (and, once written, in each user-journey file).


**Status:** Draft — refined per workflow as the deep-dive sessions happen.

---

## Legend

- **O** — Owner. Accountable for the workflow end-to-end.
- **A** — Approver. Sign-off authority at a defined step.
- **C** — Contributor. Materially supplies data or carries out steps.
- **R** — Read access only.
- **—** — No access / not involved.

When two letters apply, list both (e.g. `O / A` for "owns and signs off"; `C (M)` for "contributor via mobile only").

---

## Roles

| ID | Role | Type |
|---|---|---|
| P01 | Architect | External client |
| P02 | Subcontractor | External delivery partner |
| P03 | Project & Commercial Lead | Internal |
| P04 | Office & Compliance Coordinator | Internal |
| P05 | Site Team | Internal field |
| P06 | Brand & Content | Internal |
| P07 | Finance Director (FD) | Internal executive |
| P08 | Directors / MD | Internal executive |
| P09 | Outsourced IT Helpdesk | External partner |

See [`personas.md`](personas.md) for the full card on each.

---

## Matrix

| # | Workflow | P01 Architect | P02 Subcontractor | P03 PCL | P04 OCC | P05 Site | P06 Brand | P07 FD | P08 Directors | P09 IT |
|---|---|---|---|---|---|---|---|---|---|---|
| 01 | Drawing Receipt | C (source) | R | A | — | R (M) | — | — | R | — |
| 02 | Tender & BoQ | C (source) | — | **O** | — | C (M) | — | R | A | — |
| 03 | Subcontractor Procurement | — | C (source) | **O / A** | C | — | — | R | A (high value) | — |
| 04 | Variations / RFIs / NoDs | A | C (source) | **O** | — | C | — | R | A (high value) | — |
| 05 | Programme & Valuations | R (receives) | — | **O / A** | — | C | — | R | A | — |
| 06 | Site Reporting | R (live dashboard) | C | A | — | **O** | — | — | R | — |
| 07 | Close-Out & Defects | R | C | **O / A** | — | C | — | A (retention) | R | — |
| 08 | Subcontractor Compliance | — | C (self-service) | R | **O** | — | — | R (gates pay) | R | — |
| 09 | Accounts Payable | — | C (invoice) | R | R | — | — | **O / A** | A (above threshold) | — |
| 10 | Accounts Receivable | R (recipient) | — | C (trigger) | — | — | — | **O / A** | R | — |
| 11 | Cashflow & Mgmt Reporting | — | — | R (project slice) | — | — | — | **O** | A | — |
| 12 | Payroll | — | — | — | C (starter / leaver) | C (timesheets) | — | **O / A** | A | — |
| 13 | Accounts Inbox Triage | — | — | — | C (some queries) | — | — | **O** | — | — |
| 14 | Client & Reactive Comms | C (source) | — | A (project) | **O** | — | — | — | R | — |
| 15 | Materials & Deliveries | — | — | A (threshold) | **O** | C (req + GRN) | — | R | — | — |
| 16 | HR, Onboarding, IT Access | — | — | — | **O** (admin) | — | — | A (IT access) | A (confirm) | C (provisioning) |
| 17 | IT & Systems Admin | — | — | — | R | — | — | A (governance) | R | **O** (tier-1, target) |
| 18 | Compliance / Insurance | — | — | R (tender evidence) | **O** | — | — | A | A (annual) | — |
| 19 | Fleet | — | — | — | **O** | C (driver) | — | A (insurance) | R | — |
| 20 | Marketing & Brand | C (consent) | — | C (consent) | — | — | **O** | — | A | — |
| 21 | Document Management | — | — | R | **O** (residual) | R | R | R | R | — |
| 22 | Timesheet Management (cost-code) | R (client portal) | C (day-rate entries) | **O / A** (weekly approval) | C (back-office capture) | C (site capture) | — | A (payroll + overrun) | A (overrun above threshold) | — |
| 23 | Project Completion Settlement & VAT | A (VAT agreement) | — | A (commercial sign-off) | — | — | — | **O** (settlement, VAT draft, retention trigger) | A (final VAT outcome) | — |

---

## Read across

- **P07 Finance Director** is the busiest role on the matrix — owner on five workflows and approver on most of the rest. Aligns with the audit's finding that finance workloads (AP, inbox, cashflow) dominate current monthly hours.
- **P03 Project & Commercial Lead** owns the project lifecycle group (02–05, 07) and is approver on the rest of it. The role absorbs internal QS work.
- **P04 Office & Compliance Coordinator** owns the operational glue — compliance, comms, materials, fleet, document upkeep.
- **External roles** (P01 Architect, P02 Subcontractor) are mostly source / recipient on JPMS, never owner. This shapes the external-portal scope: read what's published to them, write only their own contributions.
- **P09 Outsourced IT Helpdesk** is the only role whose JPMS surface is deliberately narrow — provisioning hooks and audit reports only.

---

## Process for refining this matrix

1. When a workflow file moves from **Draft** → **In Review**, walk the row above with the named operational owner.
2. Capture per-step CRUD permissions inside the workflow file (and inside derived user-journey files).
3. Update the relevant cell in this matrix.
4. When all rows are confirmed, root [`README.md`](../../README.md) Section 4.7 "Permission matrix populated" can be ticked.
