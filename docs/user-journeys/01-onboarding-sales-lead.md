# Journey: Sales Lead to Won Deal

**Actors:** Sales Rep _(primary)_, Sales Manager, Finance
**Goal:** Convert an inbound lead into a paid customer with a kicked-off project.
**Frequency:** Daily
**Success metric:** Time-to-close reduced (baseline to be measured during discovery).
**Status:** Draft — worked example, **not yet validated** with the business
**Last reviewed:** —

> ⚠️ This is a **worked example** to demonstrate the journey template. The actual sales process at Jewel Enterprises must be captured during the on-site role-play session and this file rewritten or replaced.

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

- **UI:** [Live demo](./demos/01-lead-capture.html) _(to be created)_ · Figma: _tbd_
- **Screenshot:** _`assets/screenshots/01-lead-capture.png` — to be added_
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
- **Confirmed:** No — pending role-play with Sales Rep.

### 2. Qualification & Scoring

- **UI:** _tbd_
- **Fields / inputs:**
  - `budgetRange` (enum, required)
  - `timeframe` (enum, required: `0-3m`, `3-6m`, `6-12m`, `>12m`)
  - `decisionMaker` (boolean, required)
  - `score` (auto-calculated, 0–100)
- **System actions:**
  - Recalculates `score` based on a rule set (to be agreed with Sales Manager).
  - Moves `status` to `Qualified` if `score ≥ 50`, otherwise `Nurture`.
- **Decision points:**
  - `Qualified` → step 3.
  - `Nurture` → enters automated nurture cadence (separate journey).
  - `Disqualified` → archive with reason.

### 3. Opportunity Creation

- **UI:** _tbd_
- **System actions:**
  - Creates an `Opportunity` linked to the `Lead`.
  - Generates an `Opportunity` reference number.
  - Notifies Sales Manager for review.

### 4. Proposal & Quote

- **System actions:**
  - Sales Rep drafts a quote against the Opportunity, using a template.
  - Quote is generated as a PDF and stored against the Opportunity in SharePoint.

### 5. Won / Lost Decision

- **Decision points:**
  - `Won` → step 6.
  - `Lost` → capture `lostReason` (enum), archive, end journey.

### 6. Customer & Project Creation

- **System actions:**
  - Lead's company is promoted to a `Customer` record (if not already).
  - A `Project` is automatically created in `Pre-Kickoff` status, linked to the Customer and the Opportunity.
  - Handover to **Project Kickoff** journey (`11-project-kickoff.md`, to be created).
  - Finance is notified to set up billing.

---

## Edge cases & exceptions

1. **Lead already exists** — email or phone matches an existing `Lead` or `Customer`. System surfaces the match; Sales Rep chooses to merge, link, or create a duplicate (with reason).
2. **Lead with no email** — phone-only leads from older referral channels. Validation must allow this, with a warning.
3. **Disqualified mid-process** — a qualified lead can be disqualified later (e.g. funding falls through). Need a defined "regress" path.
4. **Multiple opportunities from one lead** — possible when a single contact represents several projects. Decide: one lead → many opportunities, or split the lead?
5. **Reactivation** — a `Nurture` lead becomes hot again months later. Should the original lead be reopened or a new one created?

> Mark each as confirmed/rejected during the role-play session.

---

## Data structures

References `/docs/data-models/lead.schema.json`.

```json
{
  "lead": {
    "id": "lead_01HZX...",
    "contactName": "Jane Smith",
    "companyName": "Acme Ltd",
    "email": "jane@acme.example",
    "phone": "+447700900123",
    "source": "Referral",
    "status": "Qualified",
    "score": 72,
    "ownerId": "user_abc123",
    "createdAt": "2026-05-18T10:14:00Z"
  }
}
```

---

## Permissions

| Step | Role | Can do |
|---|---|---|
| 1 | Sales Rep | Create, edit own |
| 1 | Sales Manager | View all, reassign, edit |
| 2 | Sales Rep | Edit qualification fields on own leads |
| 2 | Sales Manager | Override score, change status |
| 4 | Sales Rep | Draft quote |
| 4 | Sales Manager | Approve quote above £X (threshold tbd) |
| 5 | Sales Rep | Mark Won / Lost on own opportunities |
| 6 | Finance | View Customer + initiate billing setup |

---

## Open questions

- [ ] What is the current scoring rule set? Or is it ad-hoc today?
- [ ] Where do quotes live today (template engine, email PDFs, manual Word docs)?
- [ ] Who has authority to approve a quote? Single threshold or multiple bands?
- [ ] Does a "Lost" lead ever come back, and how?
- [ ] How do referrals get attributed for commission purposes?

---

## Confirmation checklist

- [ ] Walked through end-to-end during a role-play session
- [ ] All edge cases captured
- [ ] All field validations confirmed
- [ ] All decision points confirmed
- [ ] Permissions confirmed
- [ ] Signed off by: _name, role, date_
