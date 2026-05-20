# Workflow 21 — Document Management & Filing

**Group:** People, systems & support
**Purpose:** Keep SharePoint/OneDrive structures clean and searchable; tidy misplaced files; maintain folder taxonomy.
**Trigger:** New project, new documents, files saved in wrong place.
**Frequency:** Daily.
**Owner (target):** Largely eliminated by JPMS rollout; residual handled by Office Coordinator.
**Current monthly hours:** ~10 h/month.
**Status:** Draft
**Last reviewed:** —

---

## Current state

1. Office Coordinator tidies SharePoint / OneDrive folders.
2. Documents moved to correct locations when spotted in wrong place.
3. New project folders created manually from template.
4. Folder structures for Staff / HR / Brand maintained ad hoc.

---

## Target flow (post-automation)

1. JPMS owns project documentation — no SharePoint shuffling for projects.
2. Non-project documents (HR, compliance, brand) held in workflow-specific stores (16, 18, 20).
3. SharePoint reduces to archive and corporate documents only.
4. Residual filing tasks fall to near-zero post-rollout.

---

## JPMS functionality required

- Document storage per project (replaces SharePoint project folders).
- Auto-folder creation from project template.
- Search across project and corporate documents.

---

## Integrations & adjacent systems

- **SharePoint** (archive only).
- **OneDrive** (personal / draft work).

---

## Acceptance criteria — "done looks like"

- No-one is tidying project folders.
- Documents are findable by project, not by knowing where they were saved.
- SharePoint is for corporate documents only.

---

## Entities touched

`Project` · `Document` · `Folder Template`

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Office & Compliance Coordinator | Owner — residual oversight |
| Project & Commercial Lead | Read — project document custody |
| All internal roles | Read/write within their scope |

---

## Open questions

- [ ] Federated search — across JPMS + SharePoint + OneDrive, or JPMS only?
- [ ] Archive trigger — automatic at close-out (workflow 07), or manual?
- [ ] Retention policy on archived documents?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient
- [ ] Integrations list confirmed
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
