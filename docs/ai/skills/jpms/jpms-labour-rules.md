---
name: jpms-labour-rules
description: "Labour and timesheet doctrine — how hours become cost and how an approved day is corrected. Load before any timesheet, worker, absence or labour-cost work. Encodes view-code-approve order, approval closed to the approver, the MD/FD-only unapprove_worker_day / move_worker_day corrections and their downstream guards, the budget hard-block, rate confidentiality, sign-off freezing, close-and-replace mappings, the flat-rate rule for weekends and bank holidays, the NotWorked convention for unworked days, the preview-then-run Xero coding flow that recodes a worker's own bill in place, and the standard worker-month calendar card used whenever a worker's time or attendance is asked for."
---

# JPMS — Labour rules

## The order: view → code → approve

1. **view_labour_week first** — see the week's submitted days and their coding state.
2. **Code before approving** (code_worker_week): uncoded days REFUSE approval.
3. **approve_worker_week posts cost.** Approval snapshots the worker's rate effective on the
   worked date and posts the hours to Financials as actual labour cost. An approved timesheet is
   CLOSED to the approver — hours and cost code cannot be edited on it; before approval the
   correction path is reject-and-resubmit (reject_worker_day, with a reason the worker reads).
   Never promise an edit to an approved row.

## Correcting an approved day (MD/FD/Admin only, confirm-first, reason mandatory)

An approved day on the wrong project is a normal month-end event. Two actions are the way
back, and only the MD/FD/Admin hold them; everyone else asks them.
- **move_worker_day** — the day was worked on ANOTHER PROJECT: moves it as it stands (date,
  hours, cost code, status, and its approval — rate, £, approver, when — when approved) so the
  cost simply changes which project's Financials carry it. An approved day meets the
  destination's budget hard-block exactly as approval would; `budgetBlockReason` comes back
  with nothing moved, and `allowOverBudget: true` is the same MD/FD override with the same
  audit row. Prefer this over unapprove when the project is the only thing wrong.
- **unapprove_worker_day** — the day is wrong in itself (hours, code, shouldn't have been
  approved): puts it back to Submitted and withdraws its posted cost everywhere in one save;
  the approval snapshot lives on in the audit row with the reason. Then adjust/re-code and
  approve again, or reject_worker_day back to the worker.
- Both REFUSE once the month has gone downstream and name the step that undoes it: a
  signed-off week part (unapprove only — remove_labour_week_sign_off), an invoice line marked
  as covering the day (unmark the cover, or post a settlement variance instead), or a Xero
  coding run that has posted the worker's month (reset_xero_coding_outcome). Relay the
  refusal; take the named step only with the user's yes.
- Always view_labour_week first and put the day (worker, date, hours, code, £) and — for a
  move — the destination by reference and name in front of the user before confirming. The
  reason is the audit record: write what happened, not "correction".

## Money rules

- **The budget hard-block is server-enforced**: approval is refused for a cost code whose
  remaining budget the new cost would exceed, and the refusal reports the code's figures. Relay
  the refusal; never route around it.
- Only APPROVED time is cost. Submitted time is exposure; quote them separately.
- **Rates are confidential to managing roles** — worker rates (list_workers) never reach site
  surfaces or any output a site role or subcontractor will see. Rate changes apply to FUTURE
  approvals only; history keeps its snapshots.
- **One flat rate, every day, including bank holidays.** Weekends and bank holidays are paid at
  the worker's standard day rate. There is no premium, uplift or overtime rate for any day of the
  week. Never raise a weekend or bank-holiday day as a rate query; mark the bank holiday on the
  calendar card and move on. The FD (Jeremy Ferendinos) tells the agent when a rate changes; the
  agent never infers a rate change from an invoice or a message.
- Weekly sign-off freezes a worker-week before settlement; the Xero coding run refuses unmapped
  sites and codes by name — the fix is the mapping, not a guess.
- Xero mappings are effective-dated bridges: setting one CLOSES the old row and starts a new one
  (never edits), so historic reads still translate.

## Unworked days: record NotWorked, never dismiss

Day-rate operatives are not tracked for holiday entitlement, so an unworked day is not a cost
and never was. When the FD confirms a worker did not work a chase-list day, record it as an
absence of kind **NotWorked** (record_worker_absence) with a note naming who confirmed it and
when. This clears the chase list, shows the day under "Time off logged", and lets the week sign
off. Use Holiday or Sick only if the FD says so. Use dismiss_labour_chase_day only for a day
that was never expected at all (wrong engagement dates, wrong project assignment). A worker's
own invoice is a claim, not a record: if the invoice date or period does not fit the approved
days, ask the FD before recording anything.

Weeks that straddle a month end sign off **per month**: sign_off_labour_week returns
`monthStart`, and the week signs off for a month once every elapsed day inside that month is
settled. Days after the month end stay open. Weeks straddling the START of a month (e.g. Mon 27
Jul for an August close) need the previous month's days settled or recorded NotWorked if the
worker was not on the portal then.

## Access is the portal's job, not the agent's

The portal enforces role-based access control on every connector call. What the connector
returns to the signed-in user is already what that user is permitted to see, including whether
rates and money fields are present (`includesMoney` on view_worker_month). Do NOT ask the user
who else can see an output, whether rates should be hidden, or whether a role is allowed to
view something. Render what the connector returned. The rate confidentiality rule above governs
what the agent writes into outputs destined for OTHER people (emails, reports, messages), not
what it shows the signed-in user.

## The month-end chain, in order

1. Clear every chase-list day for the worker (timesheet, NotWorked, or dismissal).
2. sign_off_labour_week for each of the worker's weeks (confirm-first action).
3. Settle the bill with the coding run — **preview first, always**:
   - **preview_xero_coding** (dry run, writes nothing) reports per worker exactly what the run
     would do. Show the user that per-worker list verbatim; it IS the confirmation list.
   - **run_xero_coding (confirm-first, WRITES TO XERO)** — since the 3 Sep 2026 deploy this is
     the default settlement route. It finds the worker's existing bill for the month (the
     covered bill, or by contact + period, DRAFT or AUTHORISED) and recodes its lines to the
     settlement schedule's split, keeping the bill's status, total, VAT treatment and
     attachment, and moving the timesheet cover onto the new lines in the same transaction.
     Only where NO bill exists does it stage a DRAFT — VAT from the contact's Xero default or
     their last bill, never assumed, and the outcome names which it used. Paid, part-paid,
     credited or voided bills skip with the reason named; it never stages a second bill beside
     an existing one, and running a month twice produces the same end state. Already-coded
     months skip by design unless the bill they were coded to has since been deleted or voided
     (then the run takes them again); reset_xero_coding_outcome (with a reason, kept in the
     history) is the deliberate way to re-run one.
   - **CAUTION — mixed bills.** A recode spreads the bill's WHOLE total, including any
     materials or travel lines, across the labour lines in schedule proportions — non-labour
     money moves onto the CIS labour account (321) and CIS changes (seen on Lawrence Downey Aug
     2026: a £19.10 materials line on 322 was absorbed, CIS +£3.82). For a bill with a
     non-labour line that matters, settle by the manual cover route instead
     (set_xero_line_timesheet_cover on the labour line only, leaving the other lines for the
     Allocation page), or correct the lines in the Xero UI after the recode.
   - The manual cover route remains valid where the run cannot act: the FD authorises the
     worker's own bill in Xero, then set_xero_line_timesheet_cover marks the labour line(s)
     against the worker-month. A covered line needs NO allocation on the Allocation page — the
     approved timesheets carry the site cost and covered lines are excluded from cost of sales.
4. add_labour_settlement_variance posts any accepted difference between the bill total and the
   settlement schedule.

Before any Xero authorisation, read get_aged_payables for duplicate drafts per worker and check
the bill's account split: labour must sit on the CIS labour account (321) so CIS is deducted;
a labour amount coded to a materials account (322) under-deducts CIS and must be corrected
before authorisation.

## Presenting a worker's month (view_worker_month)

Whenever a person asks to see a worker's time, attendance, days, sites or hours for a month,
do not answer with a list or a table of days. Render ONE calendar card in this shape, then keep
commentary to a few lines beneath it:

1. **Header**: worker name and initials; entity; "Labour month · <Month Year>"; CIS rate.
   A status pill top-right: "<n> of <m> weeks signed off".
2. **Four metric tiles**: Days approved · Hours · Gross cost · Days missing. Money tiles appear
   whenever the connector returned money (`includesMoney: true`); omit them if it did not.
3. **A Monday-to-Sunday calendar for the whole month.** Each worked day is filled in the colour
   of its SITE and shows: date, site name, hours, cost code. Days on the labour chase list
   (view_labour_chase) with no timesheet and no absence are filled amber and labelled
   "No timesheet". Recorded absences are filled grey with the absence type. Days with no
   expectation are blank.
   **Bank holidays are always marked in the cell**, worked or not, because some workers work
   them and some do not, and the distinction matters at settlement. Use England and Wales
   dates. A worked bank holiday shows the site fill plus the label "Bank holiday"; an unworked
   one shows the label on a blank cell so it is not mistaken for a missing day.
4. **Under the calendar, one tile per site** with day count and gross, plus a "Missing" tile.
   Site colours are fixed so they read the same across workers and months. The colour for each
   project is held in the site name map (`jbb-site-name-map`, column "Card colour"); a new
   project takes the next unused ramp and is added there, not here.
5. **Footer line**: cost-code split (e.g. "All 16 days coded PRELIMS-LAB"), then
   Gross · CIS · Net · settlement verdict from view_settlement_month.

**Data sources, always all three for the same month**: view_worker_month (days, status, weeks),
view_labour_chase (missing days), view_settlement_month (gross, CIS, net, verdict, sign-off).
Never infer a missing day from a gap in the calendar; only the chase list says a day was expected.

What the card must make obvious without reading: which days block sign-off (amber), which site
each day was charged to, whether any day fell on a bank holiday, and whether the month has a
bill against it.

Same card, filtered to one worker-week, when a person asks about a single week. For "show me
everyone" requests, render one card per worker, not one combined grid.

## Presenting the month's outstanding actions

When asked what is outstanding, needs actioning, or where the month stands, render one
action-board card: a row per worker with a status pill for each step of the month-end chain
(Chase days · Signed off · Coded · Bill in Xero · Matched), coloured done / blocked / waiting,
and a short "next action" cell naming the single next step and who does it (agent or
accountant). Keep prose beneath it to the confirmations needed. Re-render the board after every
batch of labour writes so the person always sees the current state, not a list.

## Before advising any change to doctrine

Read the stored skills and the connector's current settings first (list_skills, load_skill,
what_can_you_do or get_current_context as relevant). Do not propose a rule, ask a question or
raise a risk that an existing skill or a server-enforced setting already answers.
