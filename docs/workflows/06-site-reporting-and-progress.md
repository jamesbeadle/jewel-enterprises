# Workflow 06 — Site Reporting & Progress

**Group:** Project lifecycle
**Purpose:** Capture what is happening on site — progress, photos, issues, attendance — and turn it into client reports and live project status.
**Trigger:** Daily site activity; weekly/monthly reporting cycle; CA or client request.
**Frequency:** Daily capture; weekly/monthly formal reports.
**Owner (target):** Site team (capture); JPMS (assembly); Project Lead (review/issue).
**Current monthly hours:** ~25 h/month.
**Status:** Draft
**Last reviewed:** —

---

## Current state

1. Site team photos to WhatsApp groups + their own phones.
2. PM collates photos, drafts narrative in Word/PowerPoint.
3. PM exports to PDF, emails to CA/client.
4. Subcontractor attendance tracked in a separate Excel / Dashpivot / calendar.
5. Information lives across WhatsApp, Photos, SharePoint and inboxes.

---

## Target flow (post-automation)

1. Site app captures progress (% per BoQ section), photos, notes, issues.
2. Subcontractors check in via the app (or QR/geofence) for attendance.
3. JPMS assembles weekly/monthly progress report automatically from captured data.
4. Project Lead reviews narrative, adds commentary, approves, issues from JPMS.
5. Client/CA can also view a live dashboard (selected fields) at any time.

---

## JPMS functionality required

- Site mobile app (photo, note, % capture against BoQ sections).
- Attendance check-in mechanism.
- Automated report assembly with narrative template.
- Issue / snag log capture from site.
- Client/CA dashboard (read-only, scoped).

---

## Integrations & adjacent systems

- **Mobile devices** (PWA / native).
- **WhatsApp** (one-way for legacy users initially).
- **Onetrace** where relevant.

---

## Acceptance criteria — "done looks like"

- Weekly site reports take minutes of review, not hours of assembly.
- Site photos live against the project, not in someone's WhatsApp.
- Subcontractor attendance is auditable without an Excel tracker.

---

## Entities touched

`Project` · `BoQ Line Item` · `Site Report` · `Defect` · `Subcontractor` · `Timesheet`

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Site Team | Owner of capture |
| Subcontractor | Contributor — attendance + photos |
| Project & Commercial Lead | Approver — adds narrative, issues report |
| Architect / CA (external) | Read — live dashboard, scoped |

---

## Open questions

- [ ] Offline capture — required for poor-signal sites?
- [ ] Geofence vs QR for attendance — which is acceptable to subcontractors?
- [ ] Live dashboard scoping — which fields are exposed externally?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient
- [ ] Integrations list confirmed
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
