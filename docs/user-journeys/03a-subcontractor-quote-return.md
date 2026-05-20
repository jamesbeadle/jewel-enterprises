# Journey 03a — Subcontractor: receive bid package and return a quote

> Persona slice through [Workflow 03 — Subcontractor Procurement](../workflows/03-subcontractor-procurement.md). The subcontractor-facing side: a non-Jewel user, often invited via email-with-link rather than a full JPMS account.

**Actors:** P02 Subcontractor (primary, external). P03 Project & Commercial Lead (issued the package; reviews the quote).
**Goal:** Receive a bid package, understand the scope, return a structured quote that lands directly in JPMS — without learning a new tool.
**Frequency:** Per bid invitation.
**Success metric:** ≥80% of subcontractor quotes returned through the structured flow (not as a free-text email).
**Status:** Draft
**Last reviewed:** —

---

## Trigger

Subcontractor receives a bid invitation email (or a portal notification if they're already a JPMS user).

---

## Pre-conditions

- Bid package has been assembled by the Project & Commercial Lead (workflow 03).
- Subcontractor record exists in the supplier directory with trade tag and contact.

---

## Steps

### 1. Open the package
- Email contains a single secure link. Click → land on the bid package page (no login if email-with-link; SSO if full user).

### 2. Review scope
- One-screen view: BoQ items for the trade, drawings (with current revision badge), T&Cs, deadline.

### 3. Enter prices
- Inline form per BoQ item. Subcontractor can mark items as "not bidding" with a reason.
- Subcontractor can attach their own backup (e.g. supplier quote PDF).

### 4. Add cover items / qualifications
- Free-text section for assumptions, exclusions, lead time, validity.

### 5. Submit
- Submit triggers a confirmation email and creates the `Quote` record in JPMS for the Project & Commercial Lead's comparison view.

### 6. Respond to clarifications
- If JPMS messages back ("we noticed item 14 has no price — was that intended?"), subcontractor can reply through the same link.

---

## Edge cases & exceptions

- Subcontractor declines the package entirely — one click; reason captured.
- Subcontractor requests an extension — captured, routed to Project & Commercial Lead for approval.
- Subcontractor tries to open the link after the deadline — read-only view with "deadline passed" notice.
- Subcontractor opens the link from a different device than original (no SSO) — magic link re-issue.

---

## Data structures (referenced)

- `BidPackage`, `Quote`, `Subcontractor`, `BoQLineItem`. See [`/docs/data-models/entity-relationship.md`](../data-models/entity-relationship.md).

---

## Permissions

| Step | Role | Can do |
|---|---|---|
| 1–6 | P02 Subcontractor (external) | View own package, submit own quote, message back |
| All | P03 Project & Commercial Lead | View all packages and quotes; reply |
| All | P04 Office & Compliance Coordinator | Read for directory upkeep |

See [`/docs/requirements/permission-matrix.md`](../requirements/permission-matrix.md).

---

## Open questions

- [ ] Email-with-link security — token lifetime?
- [ ] Mobile vs desktop balance — many subcontractors will open on phone.
- [ ] Currency / VAT handling at quote stage.

---

## Confirmation checklist

- [ ] Walked through with at least two real subcontractors (one tech-confident, one not)
- [ ] All form interactions confirmed
- [ ] Decline / extension flows confirmed
- [ ] Magic-link behaviour confirmed
- [ ] Permissions confirmed
- [ ] Signed off by: _name, role, date_
