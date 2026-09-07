---
name: jpms-labour-rules
description: "Labour and timesheet doctrine — how hours become cost and what is immutable. Load before any timesheet, worker, absence or labour-cost work. Encodes view-code-approve order, approval immutability, the budget hard-block, rate confidentiality, sign-off freezing, and close-and-replace mappings."
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

## Correcting an approved day (2026-09-07 — MD/FD/Admin only, confirm-first, reason mandatory)

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
- Weekly sign-off freezes a worker-week before settlement; the Xero coding run refuses unmapped
  sites and codes by name — the fix is the mapping, not a guess.
- Xero mappings are effective-dated bridges: setting one CLOSES the old row and starts a new one
  (never edits), so historic reads still translate.

## The month-end chain (2026-08-31 — runs whole from the connector)

1. **view_worker_month / view_labour_week** — find what stands in the way: Submitted days,
   uncoded days, weeks not signed off.
2. **code_worker_week → approve_worker_week** per project (the money rules above apply).
3. **sign_off_labour_week** per worker-week (confirm-first). The server re-checks the signable
   rule: every elapsed day approved, rejected or a recorded absence. A refusal names the days —
   fix them, never work around them.
   - **A week that straddles the month end signs off per month** (2026-09-02, the accountant's
     ask: August must close on the 1st). view_worker_month marks such a week with `monthPart`;
     pass `monthStart` (any date in the month you are closing) to sign that month's days only —
     31 Aug signs August's part of the week of 31 Aug, 1 Sep signs September's. The new month's
     days never hold the old month's settlement up, and a whole week inside one month is
     unchanged (one marker).
4. **view_settlement_month** — who will code, who will skip and why (FullySignedOff, verdict,
   lastCodingOutcome — `Reset` means a person reopened the month for the run).
5. **preview_xero_coding** — the dry run. Writes nothing; reports per worker what the run WOULD
   do (WouldRecodeBill naming the bill, WouldStageDraft, or Skipped with the reason). Show the
   list verbatim; it is the confirmation list for step 6.
6. **run_xero_coding** (confirm-first) — since 2026-09-03 the run's NORMAL path is the cover
   route: it finds the worker's existing bill for the month (covered, or recognised by contact +
   period; draft OR AUTHORISED — our sole traders' bills are authorised before the run sees
   them), recodes its lines to the schedule's split keeping the bill's total, VAT treatment,
   status and attachment, and moves the timesheet cover onto the new lines in the same
   transaction. It stages a DRAFT only where no bill exists at all — VAT from the contact's
   default (or their last bill), never assumed — and approving that draft stays human. A bill it
   cannot recode (paid, credited, voided) skips naming the bill and its status; it never stages
   a second bill beside an existing one. Skips report their fix: a mapping gap →
   get_xero_mappings, then set_site_xero_mapping / set_cost_code_xero_mapping; not signed off →
   step 3; two candidate bills → set_xero_line_timesheet_cover on the right one. Already-coded
   months skip by design (run-once) unless their bill has since been deleted or voided in Xero;
   **reset_xero_coding_outcome** (confirm-first, reason mandatory) is the deliberate way to
   reopen one. Running twice gives the same end state, never two bills.
7. When the bill's total will not tie to the schedule and the difference is accepted,
   **add_labour_settlement_variance** (confirm-first) posts it visibly — never absorb a
   difference silently. **set_xero_line_timesheet_cover** still marks (or unmarks) a line by hand
   when recognition picked nothing or the wrong bill.

## Settlement identity (2026-08-31)

- Every worker settles through a COUNTERPARTY: their linked directory company, or themselves when
  flagged a sole trader (they bill Dext/Xero under their own name). The company link always wins.
- A "no settlement identity" refusal is fixed with **link_worker_to_company** or
  **set_worker_sole_trader** — NEVER by inventing a directory company for a sole trader; fake
  companies pollute compliance and tendering.
- **import_xero_supplier** auto-links workers whose names match the imported supplier;
  **reconcile_worker_directory_links** (confirm-first; run apply:false first) backfills the rest
  and reports the ambiguous/unmatched remainder for a human decision.

## The chase list (2026-08-31)

- A day is chased only when it was EXPECTED: inside the worker's engagement window AND the worker
  is contracted, assigned to a project, or holds an open sign-in. Signed-off weeks, settled
  worker-months and dismissed days never chase, and the unconfirmed-cost figures always agree
  with the list.
- **view_labour_chase** reads the list; a wrong item is answered with a timesheet
  (submit_worker_week), an absence (record_worker_absence), or **dismiss_labour_chase_day** with
  a REAL reason (audited; restore_labour_chase_day undoes). A worker chased every day needs the
  real fix — contracted days, an assignment, or engagement dates — not day-by-day dismissals.
