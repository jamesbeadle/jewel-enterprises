---
name: jpms-tender-award
description: "The bid-package tender flow from scope to purchase order — what people call 'the tender'. Load before building bid packages, preparing or re-sending the invite email, handling incoming quotes, awarding, or raising the post-award work order. Encodes read-the-record-before-inviting, who the invite draft really goes to, what it really attaches, extract-never-hand-type for quotes, the human-sends rule, directory hygiene for tender-only prospects, award-mints-the-WO, and the PO email as a distinct second step."
---

# JPMS — Tender and award

A bid package is what people call "the tender" or "the enquiry": a STANDALONE record grouping
works across cost codes by trade — never a stage of the variation chain. Its tender list is the
firms being asked to price. Status on a tender-list row: `Invited` means ON THE LIST (the page
shows it as "On list"); it does not mean an email went out. `Declined` means they said no.
`Responded` means a quote is in. `Won` means awarded.

## The flow

1. **Build the package**: scope (update_bid_package_scope), line items, documents.
2. **Add to the tender list** (invite_subcontractors_to_bid_package). Say "added to the tender
   list", never "invited" — nothing emails until the invite is sent.
3. **The invite email** — see below. A person sends it; the connector prepares it.
4. **Quotes arrive by email.** NEVER hand-type a quote's figures: run extract_tender_from_message
   on the email, review what it extracted with the user, then save_extracted_quote. A typo in a
   tender figure survives into the award and the work order.
5. **Award** (award_bid_package — confirm-first): awarding mints the work order to the chosen
   subcontractor.
6. **The PO email is a distinct second step** (prepare_work_order_email_draft): a draft in the
   shared mailbox for the human to review and send — the tool never sends.

## The invite email — read first, then decide the route

Before preparing any invite:
1. `get_bid_package_context` — the tender list with each row's status.
2. `read_record_emails` (record_type bid_package) — has an invite ALREADY gone out? The sent copy
   carries the package's tag, so it shows here. If it has, tell the user who it went to and when,
   and do not prepare it again unless they say so.

Then:
- **First invite, everyone on the list should get it** → `prepare_bid_package_invite_draft`.
  Know exactly what it does: it BCCs EVERY row on the tender list that has a directory email,
  whatever the row's status — Declined rows included — and it attaches the pricing schedule,
  the company T&Cs, the package's tender documents and its linked drawings. Say this to the user
  before calling it, with the names.
- **Some of the list already had it, or someone has declined** → do NOT use
  prepare_bid_package_invite_draft (it would re-invite them all). Say which firms should and
  should not receive it, and hand the user to the package page's own Invite composer, where the
  BCC box is editable. (A recipient filter on the connector action is being added.)
- **Reporting the draft**: the action's result lists `linkedFiles` (only the files too large to
  attach, which became download links — usually empty) and does NOT list the attachments. Never
  read an empty `linkedFiles` as "no attachments". Report the attachments from what the package
  holds: "attached: the pricing schedule, T&Cs, and the 11 documents linked to the package".
- The draft sits in the shared mailbox's Drafts, tagged to the package, for a person to send
  from Outlook. Replies file themselves under the tag.

## Directory hygiene

Tender-only prospects are NOT directory members. Promote a company into the directory only from
a submitted tender or at award (promote_subcontractor_to_directory) — the directory stays a
curated list of firms Jewel actually works with. Renaming a directory company to match its Xero
supplier name is what lines its invoices up on the allocation side.

## Quoting discipline

Never disclose one bidder's figures to another, and never put subcontractor pricing in anything
client-bound. Comparisons live in internal working documents only.
