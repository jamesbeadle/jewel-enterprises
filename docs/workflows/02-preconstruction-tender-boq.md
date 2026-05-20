# Workflow 02 — Pre-Construction: Tender & BoQ

**Group:** Project lifecycle
**Purpose:** Convert tender drawings and scope of works into a priced Bill of Quantities ready for the bid process.
**Trigger:** New tender opportunity received; or existing project re-tender required.
**Frequency:** Per tender — typically several active per month.
**Owner (target):** Project & Commercial Lead (review and judgement); JPMS for data assembly.
**Current monthly hours:** ~50 h/month.
**Status:** Draft
**Last reviewed:** —

---

## Current state

1. Estimator reviews tender drawings in Bluebeam.
2. Estimator does take-off in Bluebeam, exports quantities to Excel.
3. Estimator researches updated rates manually (web, supplier contacts).
4. Estimator reviews M&E drawings separately to create an M&E BoQ.
5. Everything is assembled in a standalone Excel BoQ that lives outside any project system.
6. Walk-rounds and on-site information are captured in notebooks / notes apps.

---

## Target flow (post-automation)

1. New project record created in JPMS with tender drawings attached.
2. Bluebeam quantities imported directly into a JPMS BoQ template (line items, units, qty).
3. Rate library held in JPMS, with last-used rates per trade and supplier; AI suggests updated rates.
4. M&E section produced through the same workflow with discipline tagging.
5. Walk-round notes/photos captured against the project on mobile.
6. Final BoQ exported from JPMS — the master record stays in JPMS, not Excel.

---

## JPMS functionality required

- Project records (pre-construction phase).
- BoQ module with hierarchical line items, units, rates.
- Rate library with version history and supplier links.
- Bluebeam import/integration for quantities.
- Mobile walk-round capture (notes, photos, voice notes).
- Re-tender comparison view (last priced vs current).

---

## Integrations & adjacent systems

- **Bluebeam** (quantities import).
- **Supplier rate data** (manual today; AI-assisted in target flow).
- **JPMS rate library** (the canonical source after rollout).

---

## Acceptance criteria — "done looks like"

- BoQ exists as a JPMS record, not a standalone Excel file.
- Re-tender of a previous project takes hours, not days.
- Walk-round notes are tied to the project record from day one.

---

## Entities touched

`Project` · `Tender` · `BoQ` · `BoQ Line Item` · `Rate` · `Drawing`

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Project & Commercial Lead | Owner — review and judgement on rates |
| Quantity Surveyor (where retained) | Contributor — take-off and pricing |
| Site Team | Contributor — walk-round capture on mobile |
| Architect (external) | Source — drawings and specs |

---

## Open questions

- [ ] Bluebeam — is direct API integration available, or do we ship via export/import file?
- [ ] Rate library — supplier list as a JPMS entity, or just a lookup against the supplier directory?
- [ ] M&E discipline tag — single field or full discipline hierarchy?
- [ ] AI rate suggestion — confidence threshold to auto-apply vs flag for review?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient
- [ ] Integrations list confirmed
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
