# Journey: {Journey Title}

> Copy this file to `NN-short-kebab-name.md`, fill it in, and delete this blockquote.

**Actors:** _Primary actor, secondary actors_
**Goal:** _What is the user trying to achieve?_
**Frequency:** _Daily / Weekly / Monthly / Ad-hoc_
**Success metric:** _How will we know this journey is working well?_
**Status:** Draft | In Review | Confirmed
**Last reviewed:** _date — name_

---

## Trigger

What event starts this journey? (e.g. inbound email, scheduled task, customer call, button click on a dashboard.)

---

## Pre-conditions

- _What must be true before this journey can start?_

---

## Steps

### 1. {Step name}

- **UI:** [Live demo](./demos/{slug}.html) · Figma: _link_
- **Screenshot:** `![{step}](../../assets/screenshots/{file}.png)`
- **Fields / inputs:**
  - `fieldName` (type, required?, validation)
- **System actions:** _what the platform does in response_
- **Decision points:** _branches that lead to alternate flows_
- **Confirmed:** Yes / No — _notes_

### 2. {Step name}

…

---

## Edge cases & exceptions

- _Captured live during role-play. Each edge case becomes a candidate acceptance criterion._

---

## Data structures

Referenced schemas live in `/docs/data-models/`. Inline examples below are illustrative.

```json
{
  "exampleEntity": {
    "id": "…",
    "status": "…"
  }
}
```

---

## Permissions

| Step | Role | Can do |
|---|---|---|
| 1 | Sales Rep | Create |
| 1 | Sales Manager | View, edit |

---

## Open questions

- [ ] _Things we couldn't answer in the room._

---

## Confirmation checklist

> Tick when the actor in that role has walked the journey and agreed it matches reality.

- [ ] Walked through end-to-end during a role-play session
- [ ] All edge cases captured
- [ ] All field validations confirmed
- [ ] All decision points confirmed
- [ ] Permissions confirmed
- [ ] Signed off by: _name, role, date_
