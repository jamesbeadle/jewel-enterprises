# Workflow 19 — Fleet Administration

**Group:** People, systems & support
**Purpose:** Manage company van MOTs, services, insurance, driver changes, and fines.
**Trigger:** Renewal date, new van, driver change, fine received.
**Frequency:** Continuous; concentrated around renewal cycles.
**Owner (target):** Office & Compliance Coordinator; FD on insurance/payment.
**Current monthly hours:** ~3 h/month.
**Status:** Draft
**Last reviewed:** —

---

## Current state

1. Renewals tracked across Outlook calendar, SharePoint, Monday.com.
2. Fines received by post/email; chased to driver; appealed or paid via TfL/council portal.
3. Driver changes communicated by email.

---

## Target flow (post-automation)

1. Fleet register in JPMS with every vehicle, driver, and renewal date.
2. Auto-reminders for MOT, service, insurance.
3. Fine workflow: receipt → driver attribution → appeal or payment route → record.
4. Driver-change updates linked to HR workflow (16).

---

## JPMS functionality required

- Fleet register.
- Renewal calendar.
- Fine workflow with driver attribution.
- Driver assignment with HR link.

---

## Integrations & adjacent systems

- **TfL / council portals**.
- **Insurance broker**.
- **HR** (workflow 16).

---

## Acceptance criteria — "done looks like"

- Renewals never missed.
- Fines attributed and resolved within deadline.
- Driver-to-vehicle mapping always current.

---

## Entities touched

`Vehicle` · `Driver Assignment` · `Renewal Event` · `Fine` · `Person`

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Office & Compliance Coordinator | Owner |
| Finance Director | Approver — insurance and fine payments |
| Site Team | Source — driver of record |
| Directors | Read — visibility |

---

## Open questions

- [ ] Telematics — in scope, or out?
- [ ] Driver licence checking — manual, or via licence-check service?
- [ ] Pool vehicles vs assigned vehicles — same register or two?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient
- [ ] Integrations list confirmed
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
