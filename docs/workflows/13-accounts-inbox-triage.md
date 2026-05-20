# Workflow 13 — Accounts Inbox Triage

**Group:** Finance
**Purpose:** Manage incoming finance correspondence across BB/PS/PFP shared inboxes: invoices, queries, statements, payment confirmations.
**Trigger:** Email arrival in shared accounts inbox.
**Frequency:** Daily — hundreds of emails/week.
**Owner (target):** Finance Automation Layer (classification); Finance Director (exceptions).
**Current monthly hours:** ~60 h/month.
**Status:** Draft
**Last reviewed:** —

---

## Current state

1. FD reads inbox throughout the day across three entities.
2. Each email classified mentally: invoice, statement, query, payment confirmation, junk.
3. Action taken or routed; some forwarded to Dext, others responded to, others filed.
4. No structured handover at end of day.

---

## Target flow (post-automation)

1. AI inbox classifier reads each email and tags it: invoice / statement / query / confirmation / spam.
2. Invoices auto-forwarded to Dext.
3. Statements routed to the reconciliation queue.
4. Queries categorised by topic (supplier dispute, CIS, payment query) and routed to the right action.
5. FD reviews the exception queue and the small handful of judgement calls.

---

## JPMS functionality required

- AI email classifier (or rules engine plus AI fallback).
- Per-class routing rules.
- Exception queue for FD.
- Audit trail of every classification decision.

---

## Integrations & adjacent systems

- **Outlook** (inbox).
- **Dext** (invoice routing).
- **Xero** (cross-check against open invoices).
- **JPMS query module**.

---

## Acceptance criteria — "done looks like"

- FD reads exceptions, not every email.
- Invoices route to Dext without human action.
- Inbox is at zero at end of day, every day.

---

## Entities touched

`Inbox Message` · `Inbox Classification` · `Supplier Invoice` · `Sales Invoice` · `Statement`

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Finance Director | Owner — exception review |
| Office & Compliance Coordinator | Read — query routing recipient where finance-adjacent |
| JPMS (system) | Classifier, router |

---

## Open questions

- [ ] Confidence threshold for auto-route — what failure rate is acceptable?
- [ ] Multi-entity inboxes — single classifier with entity tag, or one per entity?
- [ ] Spam / phishing handling — defer to M365 Defender or classify here?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient
- [ ] Integrations list confirmed
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
