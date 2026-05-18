> ⚠️ **REFERENCE EXAMPLE — not project content.**
> Generic illustration of a filled-in journey. The business processes described here are **invented** and have **not** been confirmed with Jewel Enterprises. Use this only to see what a completed journey file looks like. Never copy this content verbatim into a real journey.

---

# Journey: Sales Lead to Won Deal _(example)_

**Actors:** Sales Rep _(primary)_, Sales Manager, Finance
**Goal:** Convert an inbound lead into a paid customer with a kicked-off project.
**Frequency:** Daily
**Success metric:** Time-to-close reduced (baseline to be measured during discovery).
**Status:** Example only — never marked Confirmed.
**Last reviewed:** —
**Sourced from:** N/A — this is illustrative scaffolding.

---

## Trigger

A new lead is created in one of three ways:

1. Inbound web form submission (future state).
2. Sales Rep manually adds a lead after a phone enquiry.
3. Lead imported from an event / external list.

---

## Pre-conditions

- Sales Rep has an active platform account with the `Sales` role.
- Customer does not yet exist in the system — or, if they do, the merge flow (see Edge Cases) applies.

---

## Steps

### 1. Lead Capture

- **UI:** [Live demo](./demos/01-lead-capture.html) _(would be created alongside)_ · Figma: _tbd_
- **Screenshot:** _`assets/screenshots/01-lead-capture.png`_
- **Fields / inputs:**
  - `contactName` (string, required, 2–80 chars)
  - `companyName` (string, required, 2–120 chars)
  - `email` (string, required, RFC 5322 format)
  - `phone` (string, optional, E.164 format)
  - `source` (enum, required: `Web`, `Phone`, `Referral`, `Event`, `Other`)
  - `notes` (string, optional, ≤ 2000 chars)
- **System actions:**
  - Creates a `Lead` record with `status = New`.
  - Assigns the lead to the Sales Rep who created it (or to the round-robin pool for web form leads).
  - Posts to a Teams channel `#sales-leads` for visibility.
- **Decision points:**
  - If `email` matches an existing customer → **Merge flow** (edge case 1).

### 2. Qualification & Scoring

- **Fields / inputs:**
  - `budgetRange` (enum, required)
  - `timeframe` (enum, required: `0-3m`, `3-6m`, `6-12m`, `>12m`)
  - `decisionMaker` (boolean, required)
  - `score` (auto-calculated, 0–100)
- **System actions:**
  - Recalculates `score` based on a rule set (to be agreed with Sales Manager).
  - Moves `status` to `Qualified` if `score ≥ 50`, otherwise `Nurture`.

### 3. Opportunity Creation

- Creates an `Opportunity` linked to the `Lead`.
- Generates an `Opportunity` reference number.
- Notifies Sales Manager for review.

### 4. Proposal & Quote

- Sales Rep drafts a quote against the Opportunity, using a template.
- Quote is generated as a PDF and stored against the Opportunity in SharePoint.

### 5. Won / Lost Decision

- `Won` → step 6.
- `Lost` → capture `lostReason` (enum), archive, end journey.

### 6. Customer & Project Creation

- Lead's company is promoted to a `Customer` record (if not already).
- A `Project` is automatically created in `Pre-Kickoff` status.
- Handover to a Project Kickoff journey.
- Finance is notified to set up billing.

---

## Edge cases & exceptions

1. **Lead already exists** — system surfaces the match; Sales Rep chooses to merge, link, or create a duplicate.
2. **Lead with no email** — phone-only leads. Validation must allow this with a warning.
3. **Disqualified mid-process** — a qualified lead can be disqualified later. Need a defined "regress" path.
4. **Multiple opportunities from one lead** — one lead → many opportunities, or split?
5. **Reactivation** — a `Nurture` lead becomes hot months later. Reopen or new lead?

---

## Data structures

```json
{
  "lead": {
    "id": "lead_01HZX...",
    "contactName": "Jane Smith",
    "companyName": "Acme Ltd",
    "email": "jane@acme.example",
    "source": "Referral",
    "status": "Qualified",
    "score": 72
  }
}
```

---

## Permissions

| Step | Role | Can do |
|---|---|---|
| 1 | Sales Rep | Create, edit own |
| 1 | Sales Manager | View all, reassign, edit |
| 2 | Sales Manager | Override score, change status |
| 4 | Sales Manager | Approve quote above threshold |
| 5 | Sales Rep | Mark Won / Lost on own opportunities |
| 6 | Finance | View Customer + initiate billing setup |

---

## Open questions

- [ ] Sample list — real journeys will have real open questions captured during the session.

---

## Confirmation checklist

> A real journey is only "Confirmed" once this checklist is ticked. An example like this one is **never** confirmed.

- [ ] Walked through end-to-end during a role-play session
- [ ] All edge cases captured
- [ ] All field validations confirmed
- [ ] All decision points confirmed
- [ ] Permissions confirmed
- [ ] Signed off by: _name, role, date_
