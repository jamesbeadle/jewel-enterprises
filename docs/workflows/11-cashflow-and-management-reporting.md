# Workflow 11 — Cashflow & Management Reporting

**Group:** Finance
**Purpose:** Maintain a live, accurate view of short-term cashflow and cross-entity cost allocation.
**Trigger:** Continuous; weekly review cycle.
**Frequency:** Weekly review; live dashboard.
**Owner (target):** Finance Director (judgement and intervention); Finance Automation Layer for data.
**Current monthly hours:** ~25 h/month.
**Status:** Draft
**Last reviewed:** —

> **Primary pain-point anchor.** This workflow contains the cashflow-forecast journey identified as the platform's primary driver in the [2026-05-18 domain-discovery meeting](../meetings/2026-05-18-domain-discovery.md). Every other workflow that feeds it (09 AP, 10 AR, 12 payroll, 05 valuations) should be reviewed against the question: *does this make the FD's cashflow forecast more accurate?*

---

## Current state

1. FD rebuilds cashflow tracker in Excel weekly: upcoming payments, payroll, expected receipts.
2. Cross-charges between entities tracked manually in a separate sheet.
3. Management view exists only in the FD's head until the Excel is updated.

---

## Target flow (post-automation)

1. Live cashflow dashboard pulling from Xero AP, AR, payroll, plus JPMS forward commitments.
2. Cross-entity charges captured at source through JPMS work-order/project flags.
3. Directors can see cashflow position any time without asking the FD.
4. FD's role becomes interpretation and intervention, not data assembly.

---

## JPMS functionality required

- Cashflow dashboard (consumer of finance + project data).
- Forward commitment register (work orders due to pay).
- Cross-entity flag on every transaction.
- Director-level view with appropriate scoping.

---

## Integrations & adjacent systems

- **Xero** (AP, AR balances).
- **Brightpay** (payroll commitments).
- **JPMS work orders** (forward outflows).
- **JPMS AR pipeline** (forward inflows from workflow 10).

---

## Acceptance criteria — "done looks like"

- Cashflow is a dashboard, not a weekly Excel rebuild.
- Cross-charges are accurate without month-end correction.
- Directors view cash position independently.

---

## Entities touched

`Cashflow Forecast` · `Supplier Invoice` · `Sales Invoice` · `Payment Run` · `Work Order` · `Project` · `Valuation` · `Timesheet`

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Finance Director | Owner — judgement, intervention |
| Directors / MD | Approver — strategic decisions |
| Project & Commercial Lead | Read — project-level forecast slice |

---

## Open questions

- [ ] Cross-entity reporting — separate dashboards per entity, or one consolidated view with filters?
- [ ] Forecast horizon — 13-week rolling, or different per audience?
- [ ] How much of the cross-charge logic lives in JPMS vs Xero?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient
- [ ] Integrations list confirmed
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
