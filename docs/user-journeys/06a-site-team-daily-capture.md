# Journey 06a — Site Team: daily progress capture on mobile

> Persona slice through [Workflow 06 — Site Reporting & Progress](../workflows/06-site-reporting-and-progress.md). The site-app side of the story: capture has to be fast, touch-friendly, and tolerant of poor signal, or it won't be used.

**Actors:** P05 Site Team (primary — site manager / foreman). P02 Subcontractor (attendance). Consumer: P03 Project & Commercial Lead.
**Goal:** End each day having captured progress, photos, attendance and any issues against the right BoQ sections, without opening a laptop.
**Frequency:** Daily on every active site.
**Success metric:** ≥90% of expected daily entries captured by end of shift. Zero photos lost to WhatsApp.
**Status:** Draft
**Last reviewed:** —

---

## Trigger

Site manager opens the JPMS site app on phone at start of shift.

---

## Pre-conditions

- Site manager assigned to project on the back-office side.
- BoQ sections published to the site app for the project.
- Current drawing revisions cached for offline view (workflow 01).

---

## Steps

### 1. Sign in / select project
- Single sign-on; default to today's primary project; quick-switch to other assigned projects.

### 2. Run the attendance check-in
- Subcontractor attendance via QR scan or geofence — see [Workflow 06](../workflows/06-site-reporting-and-progress.md) open questions.
- Manual override available for the site manager.

### 3. Update progress per BoQ section
- Today's section list shows previous %, suggested today % from yesterday's pace.
- Two-tap update: pick a section, slide the %, confirm.

### 4. Take and tag photos
- Photos taken in-app are tagged automatically with project, BoQ section, time, GPS.
- Camera roll import as fallback.

### 5. Raise a snag / issue
- Two-tap snag: photo + voice note + auto-assigned trade.
- Becomes a `Defect` candidate at close-out (workflow 07).

### 6. End-of-day push
- App holds offline if signal is poor and syncs when reconnected. Site manager sees an "X items pending sync" banner if relevant.

---

## Edge cases & exceptions

- No signal for the whole day — all entries queued, sync on return to signal.
- Wrong BoQ section tagged — back-office can re-tag without losing the photo.
- Subcontractor forgets to check in but is on site — manual override with attestation.
- Site manager off sick — deputy can capture against their projects.

---

## Data structures (referenced)

- `SiteReport`, `BoQLineItem`, `Subcontractor`, `Defect`, `DrawingRevision`. See [`/docs/data-models/entity-relationship.md`](../data-models/entity-relationship.md).

---

## Permissions

| Step | Role | Can do |
|---|---|---|
| 1–6 | P05 Site Team | Capture, tag, raise snag |
| 2 | P02 Subcontractor | Self check-in |
| All | P03 Project & Commercial Lead | Read; re-tag; convert snag to defect |

See [`/docs/requirements/permission-matrix.md`](../requirements/permission-matrix.md).

---

## Open questions

- [ ] QR vs geofence attendance — acceptable to subcontractors?
- [ ] Offline storage cap on the device — at what point do we ask the user to sync?
- [ ] Photo retention — full-resolution on server, thumbnails on device?

---

## Confirmation checklist

- [ ] Walked through end-to-end with a site manager on site
- [ ] All capture interactions confirmed
- [ ] Offline behaviour confirmed in a low-signal location
- [ ] Permissions confirmed
- [ ] Signed off by: _name, role, date_
