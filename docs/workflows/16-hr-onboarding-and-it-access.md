# Workflow 16 — HR, Onboarding & IT Access

**Group:** People, systems & support
**Purpose:** From new starter confirmation through paperwork, induction, IT account setup, and access provisioning.
**Trigger:** New hire confirmed, contractor engaged, or leaver notified.
**Frequency:** Ad hoc — several events per quarter.
**Owner (target):** Office & Compliance Coordinator (admin); FD (IT access); JPMS for orchestration.
**Current monthly hours:** ~10 h/month.
**Status:** Draft
**Last reviewed:** —

---

## Current state

1. Starter confirmed by directors.
2. Contract drafted in Word from template, sent for signature.
3. Induction paperwork issued separately.
4. FD creates M365 account, sets up Entra / Intune, adds to Teams / SharePoint.
5. Monday / Dashpivot / RAMsApp logins arranged separately.

---

## Target flow (post-automation)

1. Starter event raised in JPMS triggers the full onboarding sequence.
2. Contract auto-drafted from role template; e-signature flow.
3. M365 / IT account provisioned automatically based on role (with FD approval gate).
4. All system accounts (JPMS, finance, compliance) created in one orchestrated flow.
5. Leaver process is the same in reverse, with offboarding checklist.

---

## JPMS functionality required

- Onboarding/offboarding workflow with checklist.
- Role-based template library (contracts, induction packs).
- E-signature integration.
- IT provisioning trigger (calls M365/Entra APIs).
- Cross-system account audit.

---

## Integrations & adjacent systems

- **M365 Admin**, **Entra**, **Intune**.
- **1Password**.
- **Brightpay** (links to workflow 12).
- **JPMS itself** (account creation).

---

## Acceptance criteria — "done looks like"

- Starter is fully equipped on day one without three people coordinating.
- Leaver access is removed the same day, every time.
- Audit shows who has access to what across all systems.

---

## Entities touched

`Onboarding Event` · `Person` · `Role` · `System Account` · `Contract`

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Office & Compliance Coordinator | Owner — admin and orchestration |
| Finance Director | Approver — IT access gate |
| Directors | Approver — confirm starter/leaver |
| Outsourced IT Helpdesk | Contributor — provisioning execution |

---

## Open questions

- [ ] E-signature provider — DocuSign, Adobe Sign, M365 native?
- [ ] FD-as-approval-gate — keep, or delegate to Coordinator?
- [ ] Contractor (non-employee) flow — same checklist or lighter?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient
- [ ] Integrations list confirmed
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
