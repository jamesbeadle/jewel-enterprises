# Journey 16a — Office & Compliance Coordinator: day-one starter onboarding

> Persona slice through [Workflow 16 — HR, Onboarding & IT Access](../workflows/16-hr-onboarding-and-it-access.md). The orchestration view — the Coordinator's screen during a new starter's first day.

**Actors:** P04 Office & Compliance Coordinator (primary). Approvers: P07 Finance Director (IT access gate), P08 Directors (confirm starter). Contributor: P09 Outsourced IT Helpdesk (provisioning execution).
**Goal:** New starter walks in to a fully equipped day — contract signed, M365 account live, JPMS account live, system accounts in place — without three people coordinating by Teams chat.
**Frequency:** Several events per quarter.
**Success metric:** Starter day-one readiness checklist 100% green by 09:00 of the start date.
**Status:** Draft
**Last reviewed:** —

---

## Trigger

A Director (P08) confirms the starter; the onboarding event is raised in JPMS.

---

## Pre-conditions

- Role template configured (contract, induction pack, system accounts list).
- Approval flows configured (FD approves IT access).
- E-signature provider configured.

---

## Steps

### 1. Open the starter record
- Coordinator sees the orchestration view: role template, checklist (contract / induction / M365 / JPMS / finance systems / compliance), approval status per item.

### 2. Generate and send the contract
- One click: contract drafted from the role template, sent for e-signature, status moves to "awaiting signature".

### 3. Trigger IT provisioning
- Coordinator hits "request IT access" → routes to FD for approval (P07).
- On FD approval, JPMS calls M365 Admin / Entra / Intune APIs (executed by P09 Outsourced IT Helpdesk where the integration requires it).

### 4. Track day-one readiness
- Real-time checklist updates as each item completes: signed / provisioned / first-login / equipment-issued.

### 5. Handover on day one
- Induction pack auto-generated from role template + starter details, ready to print or email.

---

## Edge cases & exceptions

- Starter delays start date — checklist re-anchors; reminders re-schedule.
- FD rejects IT access — Coordinator sees the reason, can re-route after fix.
- Provisioning API fails — checklist item shows failure with retry, routes to IT helpdesk.
- Leaver process — same screen in reverse; offboarding checklist with same approval pattern.

---

## Data structures (referenced)

- `OnboardingEvent`, `Person`, `Role`, `SystemAccount`, `Contract`. See [`/docs/data-models/entity-relationship.md`](../data-models/entity-relationship.md).

---

## Permissions

| Step | Role | Can do |
|---|---|---|
| 1–5 | P04 Office & Compliance Coordinator | Owner — orchestrate, generate, track |
| 3 | P07 Finance Director | Approve IT access |
| 1 | P08 Directors | Confirm starter; sign off the event |
| 3 | P09 Outsourced IT Helpdesk | Execute provisioning items routed to them |

See [`/docs/requirements/permission-matrix.md`](../requirements/permission-matrix.md).

---

## Open questions

- [ ] FD-as-approval-gate — keep or delegate?
- [ ] Contractor (non-employee) flow — same screen with lighter checklist?
- [ ] What does "100% green" actually mean per role template (configurable per role)?

---

## Confirmation checklist

- [ ] Walked through with the Coordinator
- [ ] All checklist items confirmed against the real starter pack
- [ ] Approval flows confirmed with FD
- [ ] Leaver mirror-flow confirmed
- [ ] Permissions confirmed
- [ ] Signed off by: _name, role, date_
