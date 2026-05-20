# Workflow 08 — Subcontractor Compliance & Onboarding

**Group:** Supplier & subcontractor management
**Purpose:** Maintain up-to-date insurance, certifications, tickets, CIS status, and RAMS for every subcontractor working on a project.
**Trigger:** New subcontractor added; expiry approaching; project requiring RAMS; CIS check needed.
**Frequency:** Continuous; ~6 tasks/month per active subcontractor tracking cycle.
**Owner (target):** Office & Compliance Coordinator (oversight); JPMS for tracking and reminders.
**Current monthly hours:** ~10 h/month.
**Status:** Draft
**Last reviewed:** —

---

## Current state

1. Subcontractor details kept in Monday.com, SharePoint folders, and an Excel tracker.
2. Insurance / certificate expiry chased manually 30 days before expiry.
3. RAMS drafted in RAMsApp per project, sent to client for approval.
4. CIS verified manually through the HMRC portal.

---

## Target flow (post-automation)

1. Single subcontractor record in JPMS with all compliance docs attached.
2. Expiry dates tracked centrally; automated reminders 60/30/7 days before.
3. Subcontractor has a portal to upload new docs themselves.
4. RAMS auto-drafted from project + subcontractor data, reviewed and issued from JPMS.
5. CIS verification integrated with HMRC; status held against the record.

---

## JPMS functionality required

- Subcontractor master record.
- Document register with expiry tracking and reminders.
- Subcontractor self-service portal.
- RAMS template engine populated from project + subcontractor data.
- HMRC CIS integration — or status field with audit if no integration.

---

## Integrations & adjacent systems

- **HMRC CIS service**.
- **RAMsApp** (legacy migration only).
- **Client portals** where required (Dwellant / Vantify).

---

## Acceptance criteria — "done looks like"

- No subcontractor works on a project with expired documents.
- RAMS produced in minutes from existing data, not redrafted each time.
- CIS status is current and visible at the point of payment.

---

## Entities touched

`Subcontractor` · `Compliance Document` · `RAMS` · `Project` · `Work Order`

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Office & Compliance Coordinator | Owner — oversight, gating |
| Subcontractor (external) | Contributor — self-service uploads |
| Project & Commercial Lead | Read — checks status before award |
| Finance Director | Read — gates payment |

---

## Open questions

- [ ] HMRC CIS — API available, or scraping/manual?
- [ ] Document expiry grace — soft block (warn) or hard block (no work) at expiry?
- [ ] RAMS approval — client-side approval needed before subcontractor mobilises?
- [ ] **Toolbox Talks (TBT)** — should a weekly TBT distribution + acknowledgement capture sit here or in workflow 18? Driven by spreadsheet row 41 in [`automation-task-coverage.md`](../requirements/automation-task-coverage.md).
- [ ] **Broad subcontractor notifications** (uniform / PPE / one-off bulletins) — add a generic "notify all active subcontractors on this project" channel? Driven by spreadsheet row 42 in [`automation-task-coverage.md`](../requirements/automation-task-coverage.md).

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient
- [ ] Integrations list confirmed
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
