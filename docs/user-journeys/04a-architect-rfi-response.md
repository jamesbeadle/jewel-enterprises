# Journey 04a — Architect / CA: respond to an RFI

> Persona slice through [Workflow 04 — Variations, RFIs & Delays](../workflows/04-variations-rfis-delays.md). The architect/CA-facing slice — an external user who may or may not be a full JPMS user.

**Actors:** P01 Architect (primary, external). Sources: P02 Subcontractor (raises RFI on site). Consumer: P03 Project & Commercial Lead.
**Goal:** Architect receives an RFI, sees the right context (drawing, BoQ item, photo), replies in-flow, and the reply attaches to the project automatically.
**Frequency:** Per RFI — multiple per active project per week.
**Success metric:** Median RFI response time falls; zero RFI replies stuck in inboxes.
**Status:** Draft
**Last reviewed:** —

---

## Trigger

JPMS emails the architect with an RFI link (or notifies them in-portal if they're a JPMS user).

---

## Pre-conditions

- RFI raised by site team / subcontractor via workflow 04 / 06.
- Architect contact exists on the project.
- Relevant drawing revision and BoQ item linked to the RFI.

---

## Steps

### 1. Open the RFI
- Single secure link → RFI page with the question, attached photo(s), and a "context" panel showing the drawing extract, BoQ section, and project name.

### 2. Reply
- Free-text reply with optional file attachment (revised drawing extract).
- Toggle: "This reply implies a variation" — flags for the Project & Commercial Lead to draft a VO.

### 3. Submit
- Reply attaches automatically to the RFI record in JPMS; Project & Commercial Lead and Subcontractor are notified.

### 4. Track open RFIs
- Optional "open RFIs across my projects" view for architects who handle several Jewel projects.

---

## Edge cases & exceptions

- Architect forwards the link to a colleague — magic link tied to the architect's email, not the recipient; forwarded recipient gets a "request your own link" view.
- Architect's reply contains a contradiction with current drawing revision — JPMS surfaces the conflict to the Project & Commercial Lead.
- Architect goes on leave — out-of-office bounce flips RFI to a "stuck" state and notifies the Project & Commercial Lead.

---

## Data structures (referenced)

- `RFI`, `DrawingRevision`, `BoQLineItem`, `Variation`. See [`/docs/data-models/entity-relationship.md`](../data-models/entity-relationship.md).

---

## Permissions

| Step | Role | Can do |
|---|---|---|
| 1–4 | P01 Architect | View RFIs on own projects; reply; flag for VO |
| All | P03 Project & Commercial Lead | Read all; draft VO from flagged reply |
| All | P02 Subcontractor | Read the resolution on their RFI |

See [`/docs/requirements/permission-matrix.md`](../requirements/permission-matrix.md).

---

## Open questions

- [ ] Magic-link token lifetime for architects.
- [ ] Open-RFI dashboard for architects — opt-in or default?
- [ ] How are architect-side approval workflows handled (e.g. their internal PM needs to sign off the reply)?

---

## Confirmation checklist

- [ ] Walked through with an external architect
- [ ] Context panel content confirmed as sufficient
- [ ] Out-of-office handling confirmed
- [ ] "Implies a variation" flow confirmed
- [ ] Permissions confirmed
- [ ] Signed off by: _name, role, date_
