# Permission Matrix (Role × Workflow)

Coarse-grained matrix of which role is responsible for what across the eleven JPMS workflows. This is the at-a-glance reference. Per-step CRUD permissions live in each workflow file (and, once written, in each user-journey file).

**Status:** Draft — refined per workflow as the deep-dive sessions happen.

---

## Legend

- **O** — Owner. Accountable for the workflow end-to-end.
- **A** — Approver. Sign-off authority at a defined step.
- **C** — Contributor. Materially supplies data or carries out steps.
- **R** — Read access only.
- **—** — No access / not involved.

When two letters apply, list both (e.g. `O / A` for "owns and signs off").

---

## Roles

| ID | Role | Type |
|---|---|---|
| P01 | Architect | External client |
| P02 | Subcontractor | External delivery partner |
| P03 | Project & Commercial Lead | Internal |
| P04 | Office & Compliance Coordinator | Internal |
| P05 | Site Team | Internal field |
| P06 | Finance Director (FD) | Internal executive |
| P07 | Directors / MD | Internal executive |

See [`personas.md`](personas.md) for the full card on each.

---

## Matrix

| # | Workflow | P01 Architect | P02 Subcontractor | P03 PCL | P04 OCC | P05 Site | P06 FD | P07 Directors |
|---|---|---|---|---|---|---|---|---|
| 01 | Drawing Receipt | C (source) | R | A | — | R (mobile) | — | R |
| 02 | Tender & BoQ | C (source) | — | **O** | — | C (mobile) | R | A |
| 03 | Subcontractor Procurement | — | C (source) | **O / A** | C | — | R | A (high value) |
| 04 | Variations / RFIs / Delays | A | C (source) | **O** | — | C | R | A (high value) |
| 05 | Programme & Valuations | R (receives) | — | **O / A** | — | C | R | A |
| 06 | Site Reporting & Progress | R (live dashboard) | C | A | — | **O** | — | R |
| 07 | Project Close-Out & Defects | R | C | **O / A** | — | C | A (retention) | R |
| 08 | Subcontractor Compliance | — | C (self-service) | R | **O** | — | R (gates pay downstream) | R |
| 09 | Timesheet Management (cost-code) | R (client portal) | C (day-rate entries) | **O / A** (weekly approval) | — | C (site capture) | A (cost-code overrun) | A (overrun above threshold) |
| 10 | Cashflow & Project Forecasting | R (scoped dashboard) | — | C (project slice) | — | — | **O** | A |
| 11 | Project Completion Settlement & VAT | A (VAT agreement) | — | A (commercial sign-off) | — | — | **O** (settlement, VAT draft, retention trigger) | A (final VAT outcome) |

---

## Read across

- **P03 Project & Commercial Lead** is the busiest internal role — owner on six workflows covering the entire project lifecycle from tender through close-out plus the timesheet approval cycle.
- **P06 Finance Director** owns the two JPMS finance outputs — cashflow forecast and project completion settlement / VAT — and is approver on cost-code overruns and retention release. Their accountancy day-job is outside JPMS.
- **P04 Office & Compliance Coordinator** owns workflow 08 only. Their wider operational role (fleet, comms, materials, HR, document filing) is out of JPMS scope.
- **External roles** (P01 Architect, P02 Subcontractor) are source / contributor on workflows where they originate data, and read-only on the portal views (programme valuation, live dashboard, timesheet totals, cashflow scoped).

---

## Process for refining this matrix

1. When a workflow file moves from **Draft** → **In Review**, walk the row above with the named operational owner.
2. Capture per-step CRUD permissions inside the workflow file (and inside derived user-journey files).
3. Update the relevant cell in this matrix.
