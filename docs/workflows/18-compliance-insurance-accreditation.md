# Workflow 18 — Compliance, Insurance & Accreditation

**Group:** People, systems & support
**Purpose:** Track and renew company insurance policies, accreditations, and compliance documentation.
**Trigger:** Renewal dates, policy changes, tender evidence requests, annual reviews.
**Frequency:** Ongoing; concentrated around renewal dates.
**Owner (target):** Office & Compliance Coordinator; FD for sign-off.
**Current monthly hours:** ~5 h/month.
**Status:** Draft
**Last reviewed:** —

---

## Current state

1. Renewal dates tracked manually in Outlook calendar and SharePoint folders.
2. Renewal forms completed in Word, returned to the broker.
3. Accreditation reviews done ad hoc on receipt of request.
4. Evidence pack assembled from scratch for tenders.

---

## Target flow (post-automation)

1. Compliance register in JPMS with every policy, accreditation, and certificate.
2. Auto-reminders 90/60/30 days before renewal.
3. Tender evidence pack auto-assembled from the register on request.
4. Annual review workflow with sign-off recorded.

---

## JPMS functionality required

- Compliance document register.
- Renewal calendar with multi-stage reminders.
- Evidence pack generator for tenders.
- Sign-off audit trail.

---

## Integrations & adjacent systems

- **Document storage** (JPMS-native; SharePoint archive only).
- **Insurance broker portals** (TBD).
- **Tender response workflow** (workflow 02).

---

## Acceptance criteria — "done looks like"

- No policy ever lapses.
- Tender evidence packs are assembled in minutes.
- Sign-off is recorded, not assumed.

---

## Entities touched

`Compliance Policy` · `Accreditation` · `Renewal Event` · `Compliance Document`

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Office & Compliance Coordinator | Owner — register upkeep, evidence assembly |
| Finance Director | Approver — sign-off |
| Project & Commercial Lead | Read — tender evidence consumer |
| Directors | Approver — annual review |

---

## Open questions

- [ ] Broker integration — feasible, or stay portal-manual?
- [ ] Evidence pack format — PDF only, or include source files?
- [ ] Multi-entity policies — single register with entity tag?
- [ ] **Toolbox Talks (TBT) reminders** — does the compliance register host the weekly TBT topic catalogue + acknowledgement capture, or does that sit in workflow 08? Driven by spreadsheet row 41 in [`automation-task-coverage.md`](../requirements/automation-task-coverage.md).

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient
- [ ] Integrations list confirmed
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
