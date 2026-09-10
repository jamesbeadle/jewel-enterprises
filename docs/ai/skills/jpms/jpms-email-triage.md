---
name: jpms-email-triage
description: "How Jewel triages the projects mailbox and replies to it — the Control Centre's procedure translated for the connector. Load before working the triage queue: listing untriaged mail, filing emails to records, raising records from emails, discarding, or drafting and sending any reply. Encodes the apply ordering (file everything before any reply), the one-create-per-pass rule, attachments-before-body, the pane-choice-is-the-decision cross-filing rule, when a thread counts as handled, and the reply rule: answer every question asked before asking your own."
---

# JPMS — Email triage

The Control Centre stages a whole email's decisions and lands them in one Apply. Over the
connector you perform the same decisions as individual actions — so the ORDER the page enforced
by machinery, you must enforce by discipline.

## The order of work on one email

1. **Read the thread, not just the message** (list_mailbox_conversation). Later replies often say
   how the earlier messages should be triaged. If the thread already carries record tags
   (threadTags), the decision is usually to file to the same record — one step, not a re-triage.
2. **Attachments before body** (doc-first rule). If the email carries drawings, certificates or
   compliance documents, send them to Document Triage (send_attachments_to_document_control)
   before acting on the words.
3. **To-dos next** — anything the email demands of the team (create_todo_items_from_message).
4. **File to records** (file_email_to_record) — every record the email genuinely concerns. An
   email can feed a request AND a cost centre AND the programme at once; multiple filings are
   normal, not a smell.
   A valuation period is a record too: mail about the month's claim — the site-meeting notes
   that settle what is claimed, the QS's working, the architect's early queries — files to the
   LIVE claim (type ValuationClaim; the recordId is the claim's ValuationClaimId from
   get_valuation_context). The frozen statement (type ValuationReportSnapshot, ids from
   list_valuation_snapshots) is for the client's response to what was actually sent; a snapshot
   reads its claim's mail as well as its own, so filing to the claim is never the wrong choice.
5. **Create at most ONE new record per email per pass** (create_request_from_message,
   create_work_order_from_message, create_defect_from_message,
   create_inventory_item_from_message, …). Creating mints the record's tag onto the email, so the
   next filing decision sees it. If an email seems to need two new records, do the second on a
   second pass, after the first exists.
6. **Discard only what needs nothing** (discard_mailbox_message) — circulars, pure
   acknowledgements. Discard is restorable; when in doubt, file rather than discard.
7. **Replies LAST.** Everything above must be filed before any reply goes, so a failed send
   loses nothing already filed. send_mailbox_email IS the Control Centre's Reply box: read the
   email with get_mailbox_message, take its replyAll envelope (to, cc, subject — the same prefill
   the page shows), write the body as plain text, then show the user the recipients, subject and
   body and get their explicit yes — the action is confirm-first, and the email leaves the
   projects mailbox the moment the confirmed call succeeds. A sent reply tags the thread
   JPMS/Replied and it leaves the queue. saveAsDraftOnly true stages the reviewed draft in the
   mailbox's Drafts folder instead, for a person to send from Outlook; the prepare_*_draft actions
   do the same for record-anchored emails (a purchase order, a request's official document).

## Writing a reply — answer first

Most replies are to suppliers, subcontractors, consultants and architects asking something
ordinary: which option, what date, is it a firm or budget quote, where is the boundary. The
reply exists to answer them.

1. **List the questions the email asks** — every one, in the sender's words — before writing a
   line. Read the whole thread; an earlier message often holds the question the latest one is
   chasing.
2. **Answer each one, or park it explicitly** ("we will confirm the boundary by Friday"). Where
   the answer is the user's decision, ask the USER for it before drafting — do not send them a
   draft that leaves the sender's question unanswered.
3. **Only then ask your own** — at most one or two, and only if they are needed to proceed.
4. **The first paragraph is the answer.** Keep the sender's terms; do not rename what they
   called something.
5. Plain UK English, short, no reservation-of-rights language, no evidence held back — that is
   Nigel's doctrine for his contractual correspondence, not this.
6. Show the user the envelope and the body, get the yes, then send or stage as they choose.

## Decisions, not defaults

- **The pathway choice IS the decision.** Filing a Subcontractor-pathway thread onto a client
  record (or the reverse) is allowed — but it is a cross-filing the user must confirm. When an
  action answers that the thread is already filed under another pathway, ask the user; never
  silently pass allowCrossPathway true on your own judgment.
- **Project match is a guess until confirmed.** The queue's project hint comes from the email;
  say which project you are filing under and let the user correct it.
- **A thread is "handled" when its business is filed**, not when it has been read. Do not chase
  an inbox-zero count; chase every email's business landing on the right record.

## What you cannot do (by design)

Attach a file from the user's own computer to an email (attachments go by reference only — a
drawing revision, a progress photo, the replied-to email's own attachments, a record's official
PDF), stage decisions into the page's pending Apply (each connector action lands at once), and
bulk retagging are portal-only. Say so rather than improvising.
