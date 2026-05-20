# Workflow 01 — Drawing Receipt & Distribution

**Group:** Project lifecycle
**Purpose:** Receive new or revised drawings from architects/CAs, version-control them, distribute to relevant project members and site team.
**Trigger:** Drawing issued by architect, CA, or engineer (email or portal).
**Frequency:** As received — multiple times per week per active project.
**Owner (target):** JPMS (automated) with Project & Commercial Lead oversight on supersedure decisions.
**Current monthly hours:** ~15 h/month.
**Status:** Draft
**Last reviewed:** —

---

## Current state

1. PM receives drawing by email (Outlook).
2. PM saves PDF to SharePoint project folder, manually renamed for version.
3. PM uploads same drawing to Buildertrend.
4. PM prints copies for site and distributes by email to relevant subcontractors and site managers.
5. Old drawings are manually marked as superseded if remembered.

---

## Target flow (post-automation)

1. Drawing arrives in a monitored inbox or shared upload portal.
2. JPMS captures the drawing and auto-extracts revision/title from filename or metadata.
3. JPMS supersedes the previous version automatically; the old version is flagged as archive.
4. JPMS notifies the project team and assigned subcontractors with the new revision.
5. PM is alerted only if the system cannot confidently determine revision or supersedure.
6. The site app shows the current revision automatically; no email distribution needed.

---

## JPMS functionality required

- Drawing register per project with revision history.
- Auto-supersede logic with PM override.
- Email-inbox monitor and/or upload portal.
- Subscriber / notification model — project members opted-in by role.
- Mobile-accessible viewer for site (PDF, mark-up).
- Full audit trail of who has viewed which revision.

---

## Integrations & adjacent systems

- **SharePoint** (archive only after rollout).
- **Outlook** (inbound monitoring for the drawings inbox).
- **Bluebeam** (handoff to QS mark-up flow — see workflow 02).

See [`/docs/requirements/integrations.md`](../requirements/integrations.md) for direction (read/write) and target status (keep / replace / archive).

---

## Acceptance criteria — "done looks like"

- PM no longer manually saves, renames or distributes drawings.
- Site team always sees the current revision in one place.
- Zero instances of work done from a superseded drawing.

---

## Entities touched

`Project` · `Drawing` · `Drawing Revision` · `Subcontractor` · `Inbox Message`

See [`/docs/data-models/entity-relationship.md`](../data-models/entity-relationship.md).

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Project & Commercial Lead | Owner of supersedure-override decisions |
| Site Team | Read current revision on mobile |
| Subcontractor | Read assigned drawings; notified on revision |
| Architect / CA (external) | Source — issues the drawing |
| JPMS (system) | Capture, supersede, notify |

See [`/docs/requirements/permission-matrix.md`](../requirements/permission-matrix.md).

---

## Open questions

- [ ] Single drawings inbox or one per project?
- [ ] Tolerance for mis-detected revision — what's the fall-back when auto-extract fails?
- [ ] Retention policy on superseded revisions (forever, project lifetime, or N years post close-out)?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient
- [ ] Integrations list confirmed (additions or removals)
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
