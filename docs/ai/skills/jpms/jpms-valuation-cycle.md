---
name: jpms-valuation-cycle
description: "The monthly valuation claim and invoice cycle — the money path from % complete to cash, including raising the sales invoice in Xero. Load before any valuation, claim or valuation-invoice work: recording progress, preapproving, raising/submitting/issuing invoices, raising in Xero, payments, or presenting a statement to anyone. Encodes the claim stepper, the frozen-snapshot client rule, cumulative seeding, server-stamped retention, what certified-to-date means, and the Xero raise rule: a contact that would be created is a stop, not a go."
---

# JPMS — The valuation cycle

## The stepper (one claim, in order)

1. **Value the month**: record cumulative % complete per line (claim_progress /
   record_claim_entries). New claims ALWAYS seed from the latest claim's cumulative position —
   never start a month from zero.
2. **Lock**: preapprove_valuation_claim freezes the month's figures for claiming.
3. **Raise the invoice** (create_valuation_invoice): raising freezes a REPORT SNAPSHOT — that
   frozen statement is what the client is sent, backing this invoice. Nothing is emailed by the
   portal; a person sends the statement.
4. **Record claim sent** (submit_valuation_invoice): records that the statement went to the
   architect/client. It changes portal state only.
5. **Record approval**: record the client's approval (or rejection — a rejected invoice returns to
   draft for amend-and-resend). "Issue without approval" is legitimate only for clients with no
   formal approval loop — ask before using it.
6. **Raise in Xero & issue** (raise_valuation_invoice_in_xero): creates the AUTHORISED sales
   invoice in Xero and issues here in one press. Issuing is what moves CERTIFIED-TO-DATE. Until
   issued, the money is exposure, not certification. See the Xero raise rules below.
7. **Payment**: record it when it lands. Payment is NOT a gate for starting the next claim — the
   next month begins on its own clock.
8. **Confirm & roll over**: confirming closes the claim into history. Confirming without an
   issued invoice earns a nudge, not a block — mention it to the user.

Buttons and actions that say "Raise …" create a portal record; ones that say "Record …" record
an outside event. None sends.

## Raising in Xero — preview, then stop or go

1. Always `preview_valuation_invoice_xero_raise` first and show the user everything it returns:
   the client as Xero knows them, net, VAT reading, Sites tracking, invoice date, due date, the
   certificate.
2. **If the preview says the contact is "not in Xero" or "created with the invoice" — STOP.**
   That would create a duplicate Xero contact and lose the real contact's VAT default. Do not
   raise. Tell the user the portal could not match the client to an existing Xero contact, name
   the client as the project holds it, and give the way through: for now, raise the invoice by
   hand in Xero on the right contact, then record it here with issue_valuation_invoice ("Issue
   without raising in Xero"). A stored Xero-contact mapping on the project, with the raise
   blocked until it is set, is being built — until then this is the route, and it is not the
   user's fault.
3. **Dates**: today the raise stamps today's date and takes the due date from the certificate +
   the contract's payment days, else Xero's default. If the user needs a specific invoice date
   or due date, say so plainly, and use the by-hand route above rather than raising with the
   wrong date. (Dates on the raise are being added.)
4. **Numbering**: Xero's reference is the portal's invoice reference (VI-0005); the line
   description currently names the CLAIM number. Read the numbers back to the user from the
   preview before the yes, and do not promise a description the raise cannot yet produce.
5. Take the user's explicit yes, then raise_valuation_invoice_in_xero with confirm true. An
   invoice already carrying a Xero id is refused a second raise; a wrong invoice is voided in
   Xero, never un-raised.
6. A hand-raised Xero number cannot yet be recorded on Issue. Say so once, note the number in the
   conversation for the user, and carry on — do not write a specification instead of finishing
   the job.

## Non-negotiables

- **The client sees the FROZEN snapshot, never the live report.** The live report is a working
  copy; anything presented, emailed or quoted as "the valuation" must come from the snapshot
  behind the invoice (get_valuation_snapshot). Comparing live vs frozen is how you answer "what
  moved since we claimed".
- **Retention is stamped server-side** from the project's terms — never compute or pass it.
- **Certified-to-date = issued + paid invoices (gross of deposit credits).** Quote it from
  list_valuation_invoices' summary, never by adding numbers yourself.
- Deleting claims or invoices is recovery machinery, not tidying — user's explicit say-so, named
  by number, every time.

## Correspondence

- **The live claim is a record in its own right.** Mail about the period — what to claim, the
  QS's working, the architect's early queries — files to the claim (file_email_to_record, type
  ValuationClaim, recordId = the claim's ValuationClaimId from get_valuation_context) and reads
  back with read_record_emails (recordType valuation_claim). Its mail tag is
  JPMS/VAL-{project reference}-{claim number}.
- **A snapshot inherits its claim's mail.** Every snapshot frozen from a claim shows the claim's
  correspondence beside anything tagged to the snapshot itself (type ValuationReportSnapshot),
  so the statement carries the period's whole story; the client's reply to a sent statement
  can go on either.
- **Roll-over moves the tag on its own.** Confirm & roll over starts the next claim with the
  next number — new mail files to the new period; nothing is re-tagged.
