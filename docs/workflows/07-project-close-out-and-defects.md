# Workflow 07 — Project Close-Out & Defects

**Group:** Project lifecycle
**Purpose:** Manage the snagging, defect resolution, and formal close-out process at the end of a project.
**Trigger:** Practical completion or formal handover.
**Frequency:** Per project — typically several active close-outs in parallel.
**Owner (target):** Project & Commercial Lead (sign-off); JPMS for defect register and workflow.
**Current monthly hours:** ~5 h/month (likely under-captured).
**Status:** Draft
**Last reviewed:** —

---

## Current state

1. Defect schedule created in Excel from site inspection.
2. Items emailed to relevant subcontractors for resolution.
3. Status chased manually by email/phone.
4. Final sign-off captured by email or in a notebook.

---

## Target flow (post-automation)

1. Defect register raised in JPMS against the project, with photos and BoQ references.
2. Each defect auto-assigned to the responsible subcontractor with a deadline.
3. Subcontractor updates status via JPMS portal or app; photos as evidence.
4. Project Lead signs off each defect; final sign-off triggers retention release.
5. Close-out pack (warranties, O&Ms, defect record) assembled by JPMS.

---

## JPMS functionality required

- Defect register module per project.
- Subcontractor assignment with deadline tracking.
- Photo evidence and resolution audit trail.
- Close-out pack generator (warranties, O&Ms, sign-offs).
- Retention release trigger → finance.

---

## Integrations & adjacent systems

- **Subcontractor portal** (workflow 03 — same portal).
- **AP / retention release** (workflow 09).
- **Document library** (close-out pack output).

---

## Acceptance criteria — "done looks like"

- Defects are tracked to closure with photo evidence.
- Close-out pack is assembled from JPMS, not built by hand.
- Retention release happens on close-out sign-off, not by separate request.

---

## Entities touched

`Project` · `Defect` · `Subcontractor` · `Work Order` · `BoQ Line Item` · `Compliance Document`

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Project & Commercial Lead | Owner — sign-off on each defect and the close-out pack |
| Subcontractor (external) | Contributor — updates status + photo evidence |
| Finance Director | Approver — retention release |
| Architect / CA (external) | Read — close-out pack delivery |

---

## Open questions

- [ ] Retention percentage — global or per work-order?
- [ ] Warranties — where stored after close-out (JPMS vs SharePoint archive)?
- [ ] Defect-period clock — starts at PC, or at handover?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient
- [ ] Integrations list confirmed
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
