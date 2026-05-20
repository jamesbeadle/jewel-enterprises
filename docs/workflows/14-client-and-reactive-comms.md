# Workflow 14 — Client & Reactive Comms (front-of-house)

**Group:** Operations & comms
**Purpose:** Handle incoming client calls and emails, route them appropriately, and provide updates on access/progress/issues.
**Trigger:** Inbound call or email from client, neighbour, or other external party.
**Frequency:** Daily, continuous through office hours.
**Owner (target):** Office & Compliance Coordinator (primary); Project Lead for project-specific judgement.
**Current monthly hours:** ~20 h/month.
**Status:** Draft
**Last reviewed:** —

---

## Current state

1. Calls and emails come into shared lines/inboxes.
2. Coordinator routes to the right person based on subject and project.
3. Updates to clients drafted from scratch by checking SharePoint, Outlook, asking the PM.
4. Neighbour letters drafted in Word from a template each time.

---

## Target flow (post-automation)

1. Caller/sender identified from CRM lookup; project context auto-surfaced for the coordinator.
2. Routing rules guide most enquiries automatically; coordinator handles judgement cases.
3. Client update template populated from JPMS project status (access dates, progress, issues).
4. Neighbour letters generated from project address and works data.

---

## JPMS functionality required

- Lightweight CRM / contact directory.
- Project context lookup by caller.
- Templated client update generator from project data.
- Neighbour letter generator.
- Call / email log against client and project.

---

## Integrations & adjacent systems

- **Outlook**.
- **Phone system** (caller-ID lookup).
- **JPMS project module**.

---

## Acceptance criteria — "done looks like"

- Coordinator answers clients with full context in front of them.
- Client updates take minutes to send, not chase.
- Every interaction is logged against the project.

---

## Entities touched

`Project` · `Contact` · `Inbox Message` · `Communication Log`

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Office & Compliance Coordinator | Owner — front line |
| Project & Commercial Lead | Approver — project-specific judgement |
| Architect / CA (external) | Source — inbound enquiries |
| Directors | Read — escalation visibility |

---

## Open questions

- [ ] Phone-system caller-ID — which platform (3CX, Microsoft Teams, other)?
- [ ] CRM scope — full CRM, or just a contact directory with project links?
- [ ] Out-of-hours flow — voicemail to ticket, or external answering service?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient
- [ ] Integrations list confirmed
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
