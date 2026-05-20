# Workflow 17 — IT & Systems Administration

**Group:** People, systems & support
**Purpose:** Day-to-day IT support, M365 administration, SharePoint/Teams permissions, and security/licensing oversight.
**Trigger:** Staff issue raised; system change; security/renewal cycle.
**Frequency:** Daily (support); periodic (admin).
**Owner (target):** Outsourced IT helpdesk (recommended); FD for governance only.
**Current monthly hours:** ~50 h/month.
**Status:** Draft
**Last reviewed:** —

---

## Current state

1. FD handles all day-to-day IT support across the group.
2. FD also manages M365 admin, SharePoint/Teams permissions, Entra, Intune, Defender.
3. Security reviews and licensing checks done by FD periodically.
4. Systems map and architecture documentation maintained in Excel.

---

## Target flow (post-automation)

1. Outsourced IT helpdesk (3rd party) handles all tier-1 support.
2. M365 admin tasks routed via helpdesk for routine items; FD retains governance.
3. Onboarding/offboarding triggered via workflow 16, not FD ad hoc.
4. Security and licensing on a calendar with auto-reminders.
5. Architecture docs live in a wiki, updated as changes are made (not Excel).

---

## JPMS functionality required

> Limited JPMS scope — mostly outside the system.

- Provisioning hooks from workflow 16.
- Reminder calendar for renewals and reviews.
- Access audit reports.

---

## Integrations & adjacent systems

- **M365 Admin**, **Entra**, **Intune**, **Defender**.
- **1Password**.
- **3rd-party helpdesk** (TBD provider).

---

## Acceptance criteria — "done looks like"

- FD is not the helpdesk.
- Routine IT tasks are not in the FD's inbox.
- Security and licensing reviews happen on schedule, not when remembered.

---

## Entities touched

`Person` · `System Account` · `Compliance Policy` · `Renewal Event`

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Outsourced IT Helpdesk | Owner — tier-1 |
| Finance Director | Approver — governance only |
| Office & Compliance Coordinator | Read — routing |
| Directors | Read — security/audit visibility |

---

## Open questions

- [ ] Helpdesk vendor — selected or still in evaluation?
- [ ] What FD-only governance items remain (vs delegable to helpdesk)?
- [ ] Wiki platform — SharePoint pages, Notion, GitHub wiki?
- [ ] **Policy review cadence** — add an annual review reminder for each governance policy (IT, AI, cybersecurity, GDPR) with sign-off capture? Driven by spreadsheet row 45 in [`automation-task-coverage.md`](../requirements/automation-task-coverage.md).

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient (note: minimal scope)
- [ ] Integrations list confirmed
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
