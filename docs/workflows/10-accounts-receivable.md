# Workflow 10 — Accounts Receivable (Sales Invoicing → Collection)

**Group:** Finance
**Purpose:** From valuation/milestone trigger to issued sales invoice and collected payment.
**Trigger:** Approved valuation, milestone reached, work order completed, or director request.
**Frequency:** Weekly invoicing; daily collection chasing.
**Owner (target):** Finance Director (review/approve); Finance Automation Layer for drafts and chasing.
**Current monthly hours:** ~25 h/month.
**Status:** Draft
**Last reviewed:** —

---

## Current state

1. FD instructed via WhatsApp/email/system that an invoice is due.
2. FD raises sales invoice in Xero manually.
3. Invoice emailed to client; copy to Dwellant / Vantify where required.
4. Overdue invoices chased manually via Chaser HQ + phone + email.

---

## Target flow (post-automation)

1. Trigger fires automatically in JPMS (valuation approved, WO completed, milestone reached).
2. Draft sales invoice created in Xero with project-correct line items and references.
3. FD reviews and releases (single click, in bulk).
4. Client receives via Xero + relevant portal upload (Dwellant / Vantify).
5. Chaser HQ handles the collection sequence; FD intervenes on disputed or stalled debts only.

---

## JPMS functionality required

- AR trigger events (valuation, milestone, WO completion).
- Sales invoice draft generation.
- FD bulk-release workflow.
- Portal-upload integration for external client systems.
- Collection status visibility per project.

---

## Integrations & adjacent systems

- **Xero** (invoice creation).
- **Chaser HQ** (collections).
- **Dwellant**, **Vantify** (client-side portals).

---

## Acceptance criteria — "done looks like"

- Invoices go out on the day they should, not when the FD has time to raise them.
- FD reviews drafts, doesn't type invoices.
- Project cashflow shows expected AR collection dates, not estimates.

---

## Entities touched

`Project` · `Valuation` · `Work Order` · `Sales Invoice` · `Payment Run`

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Finance Director | Owner — bulk-release and dispute handling |
| Project & Commercial Lead | Source — valuation/milestone approval |
| Directors | Read — high-value visibility |
| Architect / CA (external) | Recipient — receives invoice |

---

## Open questions

- [ ] Dwellant / Vantify — API upload available or manual?
- [ ] Disputed-debt threshold — when does FD escalate to MD?
- [ ] Multi-entity invoicing (BB/PS/PFP) — single Xero instance or per-entity?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient
- [ ] Integrations list confirmed
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
