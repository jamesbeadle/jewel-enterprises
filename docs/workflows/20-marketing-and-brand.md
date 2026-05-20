# Workflow 20 — Marketing & Brand

**Group:** People, systems & support
**Purpose:** Produce and schedule social content, maintain brochures, and manage brand assets across Jewel entities.
**Trigger:** Content calendar, new project completion, brand updates.
**Frequency:** Weekly content; ad-hoc brochure / brand updates.
**Owner (target):** Brand & Content; Directors for sign-off.
**Current monthly hours:** ~20 h/month.
**Status:** Draft
**Last reviewed:** —

---

## Current state

1. Social posts created in Canva from project photos.
2. Brochure updated in Canva / Word as projects complete.
3. Brand asset folders maintained in SharePoint / OneDrive.

---

## Target flow (post-automation)

1. Content calendar held in a marketing module (lightweight, can sit outside JPMS).
2. Project completion in JPMS auto-flags eligibility for content (with consent flag).
3. Brand asset library version-controlled.
4. Director review and sign-off captured in-system before publish.

---

## JPMS functionality required

> Adjacent — mostly outside JPMS scope.

- Project "content-eligible" flag with client consent capture.
- Asset library link.

---

## Integrations & adjacent systems

- **Canva**.
- **Meta Business**, **LinkedIn**.
- **Scheduling tool** (TBD).

---

## Acceptance criteria — "done looks like"

- Director approval is recorded before posts go live.
- Content draws from current project data, not screenshot hunts.
- Brand assets aren't duplicated across folders.

---

## Entities touched

`Project` · `Content Item` · `Consent Record` · `Brand Asset`

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Brand & Content | Owner — creates and schedules |
| Directors | Approver — sign-off before publish |
| Project & Commercial Lead | Source — eligibility + consent confirmation |
| Architect / CA (external) | Consent giver where required |

---

## Open questions

- [ ] Consent capture — at handover, or per-asset?
- [ ] Where lives the brand kit — JewelBB brand-voice skill, Canva, both?
- [ ] Does the marketing tool need read-only access to JPMS project status?
- [ ] **Scheduled content distribution** — add a rule for scheduling "shareable bulletins" (industry H&S notices, "Your Home, Your Safety", etc.) to defined target groups (clients, neighbours, staff)? Driven by spreadsheet row 38 in [`automation-task-coverage.md`](../requirements/automation-task-coverage.md).

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient (adjacent only)
- [ ] Integrations list confirmed
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
