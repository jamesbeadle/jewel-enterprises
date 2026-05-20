# Workflow 05 — Programme & Valuations (project control)

**Group:** Project lifecycle
**Purpose:** Maintain the project programme and produce monthly valuations against contract and approved variations.
**Trigger:** Monthly valuation cycle, significant variation, or programme impact event.
**Frequency:** Monthly; updates as needed in between.
**Owner (target):** Project & Commercial Lead (review and issuance); JPMS for the calculation and assembly.
**Current monthly hours:** ~10 h/month.
**Status:** Draft
**Last reviewed:** —

---

## Current state

1. PM updates MS Project programme manually based on site reports and judgement.
2. PM updates Excel valuation sheet line-by-line: contract sum, variations to date, work in progress, percentages.
3. Reconciliation between programme and valuation is manual.
4. Issued to CA/client as PDF/Excel attachment by email.

---

## Target flow (post-automation)

1. Programme lives in JPMS with tasks/phases tied to BoQ line items.
2. Site reporting (workflow 06) updates progress percentages automatically.
3. Valuation auto-generated each **Claim Period** (the contractual cycle on the project — typically monthly, but per-contract): contract value + approved variations + current %, broken down per BoQ line item and rolled up per cost code.
4. Project Lead reviews, applies judgement, approves, and issues the **Programme Valuation Report** for the period from JPMS.
5. Client / CA receives the Programme Valuation Report through the portal or as a styled PDF, with the **Claim Value for that Claim Period** stated up front.
6. Historic valuations are retained as the time-series for each project, with diff-from-prior-period available on every line.

---

## JPMS functionality required

- Programme module with Gantt-style view.
- BoQ-to-programme linkage so progress flows through.
- **Claim Period** as a first-class concept on the project — defined at contract setup; defaults to monthly; overridable per contract.
- Valuation generator that produces a **Programme Valuation Report** for each Claim Period: contract + variations + current % per BoQ line item, with Claim Value totals (period + cumulative).
- Approval and issuance workflow for the Programme Valuation Report.
- Historic valuation series per project (one report per Claim Period), with prior-period diff.
- Sibling **Variation Orders list report** (canonically owned by workflow 04 but co-issued with the Programme Valuation Report each Claim Period).

---

## Integrations & adjacent systems

- **Site reporting** (workflow 06).
- **Variations** (workflow 04).
- **AR invoicing** (workflow 10).

---

## Acceptance criteria — "done looks like"

- Monthly valuation takes minutes to review, not hours to rebuild.
- Programme reflects site reality without manual re-entry.
- Valuation feeds directly into the sales-invoice draft (workflow 10).

---

## Entities touched

`Project` · `BoQ Line Item` · `Programme Task` · `Valuation` · `Claim Period` · `Variation` · `Site Report`

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Project & Commercial Lead | Owner — reviews, approves, issues |
| Site Team | Source — % progress per BoQ section |
| Finance Director | Read — feeds AR (workflow 10) |
| Architect / CA (external) | Recipient — receives issued valuation |

---

## Open questions

- [ ] MS Project import — migration only, or ongoing read?
- [ ] Claim Period — confirm default cadence (monthly) and the cases where it differs (per-contract).
- [ ] Retention handling — applied at valuation or at AR invoice (workflow 10)?
- [ ] Programme Valuation Report — single template across clients, or per-client styling?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient
- [ ] Integrations list confirmed
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
