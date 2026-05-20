# Workflow 09 — Accounts Payable (Supplier Invoices → Payment)

**Group:** Finance
**Purpose:** From supplier/subcontractor invoice receipt to approved payment, with project costing and CIS handling.
**Trigger:** Invoice arrives by email, paper, or Dext capture.
**Frequency:** Daily.
**Owner (target):** Finance Director (review/approve); Finance Automation Layer for capture, matching, coding.
**Current monthly hours:** ~80 h/month — **the single largest workflow in the audit**.
**Status:** Draft
**Last reviewed:** —

---

## Current state

1. Invoice arrives in the shared accounts inbox.
2. FD captures into Dext (manually or via Dext's OCR).
3. FD codes invoice, matches to work order or project, posts to Xero.
4. Payment run reviewed weekly; FD checks cashflow, approves payments, pays via online banking.
5. Subcontractor errors (wrong amounts, missing CIS) chased manually.
6. Statement reconciliation done monthly to catch missing invoices.

---

## Target flow (post-automation)

1. Invoice arrives → Dext OCRs → JPMS auto-matches to work order using PO/WO reference, supplier, and amount.
2. Confidence-based routing: high-confidence matches coded automatically; low-confidence flagged for FD review.
3. Payment run drafted from due invoices + cashflow constraints; FD reviews and approves in bulk.
4. Subcontractor invoice errors detected automatically (CIS missing, wrong amount vs WO); error email auto-drafted for FD review.
5. Statement reconciliation runs automatically each month; gaps flagged.

---

## JPMS functionality required

- Work order register (from workflow 03) as the matching source.
- Project cost ledger updated by AP feed.
- AP exception queue for FD review.
- Payment run draft with cashflow check (consumes workflow 11).
- Subcontractor invoice validation rules (CIS, WO match).
- Statement reconciliation engine.

---

## Integrations & adjacent systems

- **Dext** (capture).
- **Xero** (posting and payment).
- **HMRC CIS** (verification + deduction).
- **Online banking** (payment execution).
- **Chaser HQ** (supplier-side disputes).

---

## Acceptance criteria — "done looks like"

- FD spends time reviewing exceptions, not coding every invoice.
- Project cost data is live, not weekly catch-up.
- Subcontractor payment errors are caught before payment, not after.

---

## Entities touched

`Work Order` · `Supplier Invoice` · `Payment Run` · `Subcontractor` · `Project` · `Inbox Message` · `Compliance Document` (CIS)

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Finance Director | Owner — review and approve |
| Office & Compliance Coordinator | Read — supplier directory upkeep |
| Project & Commercial Lead | Read — project cost visibility |
| Directors | Approver — above threshold payments |

---

## Open questions

- [ ] Confidence threshold for auto-code — what's the FD's tolerance?
- [ ] Payment run cadence — keep weekly, or move to twice-weekly with the new flow?
- [ ] Cross-entity (BB/PS/PFP) coding — how is the routing decided today?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient
- [ ] Integrations list confirmed
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
