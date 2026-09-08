---
name: jpms-xero-allocation
description: "How Xero and the portal reconcile — allocation doctrine and why the ledgers read the way they do. Load before reading or discussing Xero costs, aged payables/receivables, cost-of-sales spend, work-order invoice links, or anything built on allocated lines. Encodes drafts-are-deliberate, allocation-moves-money-views, the complete-slice-list rule, and labour-settlement bills."
---

# JPMS — Xero allocation doctrine

## Why the portal's numbers beat Xero's reports

- **Draft bills are deliberate.** The coding procedure holds purchase bills in DRAFT until they
  are allocated through the portal, so Xero's own aged-payables report UNDERCOUNTS what is owed.
  Quote get_aged_payables / get_aged_receivables (drafts included), never Xero's report, for what
  we owe or are owed.
- **A cost only reaches a project when its line is ALLOCATED** to a project + master cost centre
  (or split across several). Unallocated lines are real money not yet in any project's spend —
  when project cost figures look light, check the Unallocated queue first
  (list_xero_ledger_lines).

## Allocation rules (the page's arming rules, as doctrine)

- A line goes to ONE of: project + cost centre, or a bucket (no project, no cost centre) —
  when both are somehow set, the bucket was the later deliberate act and wins.
- Half-allocated states exist on purpose: a line can carry its project (moving it to that
  project's tab and writing the Xero site) without being allocated to a centre yet.
- Disputed lines are a conversation, not an error state — the thread survives resolution.
- **Labour-registry suppliers' bills are timesheet SETTLEMENT, not costs** — they bypass the
  allocation queue entirely; labour cost enters projects through approved timesheets.
- **A bill from a supplier with an open work order is a Work Order bill** (2026-09-08): its
  project and cost code(s) were decided when the order was approved, so it is never coded by
  hand. It sits on the allocation page's Work Order bills tab as ONE card per bill, pre-filled
  from the order (project, cost code split pro rata to the order's lines, order value, invoiced
  to date, remaining), and one Approve allocates every line, links each to the order for its
  net, writes Sites + Cost Code tracking to Xero and approves the bill there. Matching runs on
  every unallocated read (Sync and Re-check re-run it): a WO number on the bill wins (supplier
  + number — numbers are per project, the bill's own Xero site breaks a tie); else a supplier
  with exactly one open order; "open" is Released with value still left to invoice. Not
  matched, with the reason on the row: the bill would take the order over its value, several
  orders fit and the bill carries no reference, or the supplier is on the labour registry (the
  cover route wins). The approval is audited (who, which rule) and undone as a bill: allocation,
  links and Xero tracking reverse together — but Xero never un-approves, so an approved bill
  stays awaiting payment there.

## Work-order invoice links

set_xero_line_work_order_links takes the line's COMPLETE slice list every time — read the
current links first (list_xero_ledger_lines with the projectId), modify, resend the whole set.
Sending a partial list silently drops the missing allocations.
