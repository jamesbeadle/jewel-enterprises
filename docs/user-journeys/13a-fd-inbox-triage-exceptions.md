# Journey 13a — Finance Director: inbox triage exception review

> Persona slice through [Workflow 13 — Accounts Inbox Triage](../workflows/13-accounts-inbox-triage.md). Second-largest finance workload today (~60 h/month). The goal of this journey is for the FD to never read a regular invoice email again.

**Actors:** P07 Finance Director (primary). System: JPMS classifier. Sources: external suppliers, clients, HMRC, internal staff.
**Goal:** Review the small set of inbox items the classifier wasn't confident about and the small set of judgement-calls that always need a human, then clear them.
**Frequency:** Three times a day; aim for inbox-zero by end of day.
**Success metric:** ≥85% of inbound finance email auto-classified and auto-actioned (Dext-route, statement queue, etc.). FD reads exceptions, not every email.
**Status:** Draft
**Last reviewed:** —

---

## Trigger

FD opens the inbox exception queue (or it's surfaced in the cashflow dashboard 11a step 2).

---

## Pre-conditions

- Outlook inbox feed live on the three shared finance inboxes (BB/PS/PFP).
- Classifier rules active; AI fallback live.

---

## Steps

### 1. Land on the exception queue
- One row per email needing FD attention.
- Each row: sender, subject preview, suggested class with confidence, action options.

### 2. Confirm or override the classification
- One-click confirm sends the email down its routing path (Dext / statement queue / query topic / spam).
- Override picks the right class; classifier learns from the override.

### 3. Action a judgement item
- Some classes always need human action (e.g. supplier dispute). FD takes the action inline or routes to the right person.

### 4. Track the audit trail
- Every classification (auto or human) is logged; the audit view shows classifier accuracy over time.

---

## Edge cases & exceptions

- Email contains an invoice attachment but the body is a complaint — multi-class handling.
- Phishing / spam — defer to M365 Defender; classifier doesn't try to compete.
- Sender unknown — JPMS surfaces "first contact" flag.

---

## Data structures (referenced)

- `InboxMessage`, `InboxClassification`. See [`/docs/data-models/entity-relationship.md`](../data-models/entity-relationship.md).

---

## Permissions

| Step | Role | Can do |
|---|---|---|
| 1–4 | P07 Finance Director | Confirm / override / action |
| All | P04 Office & Compliance Coordinator | Read (where query routing affects them) |

See [`/docs/requirements/permission-matrix.md`](../requirements/permission-matrix.md).

---

## Open questions

- [ ] Confidence threshold for auto-route vs surface.
- [ ] Multi-entity routing — single queue or one per entity?
- [ ] Classifier learning loop — supervised, semi-supervised, or static rules + AI fallback?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the FD
- [ ] All classification categories confirmed
- [ ] Override behaviour confirmed
- [ ] Audit trail format confirmed
- [ ] Permissions confirmed
- [ ] Signed off by: _name, role, date_
