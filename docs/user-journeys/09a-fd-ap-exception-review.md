# Journey 09a — Finance Director: AP exception review

> Persona slice through [Workflow 09 — Accounts Payable](../workflows/09-accounts-payable.md). The single largest workflow in the audit (~80 h/month today); reducing the FD's load here is the biggest finance-side ROI of phase 1.

**Actors:** P07 Finance Director (primary). Sources: P02 Subcontractor (invoice), P03 Project & Commercial Lead (WO context). Approver: P08 Directors (above-threshold payments).
**Goal:** Clear the AP exception queue: review only the items the auto-matcher couldn't handle, fix or reject them, and release a clean payment-run draft.
**Frequency:** Daily review; weekly payment run.
**Success metric:** ≥90% of invoices auto-coded with no FD touch. Subcontractor invoice errors caught pre-payment, not post.
**Status:** Draft
**Last reviewed:** —

---

## Trigger

FD opens the AP exception queue (manually, or from the cashflow dashboard 11a step 2).

---

## Pre-conditions

- Dext has OCR'd today's captured invoices.
- JPMS work-order register is current (workflow 03).
- HMRC CIS status is current per subcontractor (workflow 08).

---

## Steps

### 1. Land on the exception queue
- Three tabs: unmatched-to-WO, CIS issue, amount variance > tolerance.
- Each row: supplier, gross, suggested WO, confidence score, reason for exception.

### 2. Resolve an unmatched invoice
- FD picks the right WO from the suggestion list, or searches.
- Coding auto-populates from the WO; FD overrides if needed.
- One click posts to Xero and clears the exception.

### 3. Resolve a CIS issue
- JPMS shows the subcontractor's current CIS status and the invoice's CIS handling.
- FD chooses: pay with adjusted CIS / hold pending verification / draft chase email to subcontractor.

### 4. Resolve an amount variance
- JPMS shows side-by-side: invoice vs WO, vs prior interim certificates.
- FD adjusts, queries the subcontractor via auto-drafted email, or approves anyway with reason.

### 5. Release the payment-run draft
- Payment-run draft already exists, filtered by due date and cashflow constraints (workflow 11).
- FD bulk-approves; above-threshold items route to a Director for approval (P08) before payment.

---

## Edge cases & exceptions

- Two invoices for the same WO arrive in the same period (duplicate detection).
- Subcontractor sends a credit note (negative invoice).
- WO has been varied since the invoice was raised — show variance against the variation, not the original WO.
- Retention release on close-out (workflow 07) flagged as a special item in the run.

---

## Data structures (referenced)

- `SupplierInvoice`, `WorkOrder`, `Subcontractor`, `CISStatus`, `PaymentRun`. See [`/docs/data-models/entity-relationship.md`](../data-models/entity-relationship.md).

---

## Permissions

| Step | Role | Can do |
|---|---|---|
| 1–4 | P07 Finance Director | Read, code, approve, reject |
| 5 | P07 Finance Director | Approve payment run up to threshold |
| 5 | P08 Directors / MD | Approve above threshold |

See [`/docs/requirements/permission-matrix.md`](../requirements/permission-matrix.md).

---

## Open questions

- [ ] Confidence threshold for auto-code — confirm with FD.
- [ ] Approval threshold value — single or per-entity?
- [ ] Retention release — fully automated on workflow-07 sign-off, or always FD-touch?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the FD
- [ ] All exception types confirmed
- [ ] CIS handling confirmed against current HMRC practice
- [ ] Threshold approval routing confirmed
- [ ] Permissions confirmed
- [ ] Signed off by: _name, role, date_
