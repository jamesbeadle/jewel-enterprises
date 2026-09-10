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
   the Xero contact mapped on the project and whether Xero still holds it, net, VAT reading,
   Sites tracking, reference and description, invoice date, due date, the certificate.
2. **The contact is the one MAPPED ON THE PROJECT** (Project settings → Xero contact). The raise
   never matches the client by name and never creates a contact. If the preview's blockers say no
   Xero contact is mapped (or the mapped one is not found in Xero) — STOP. Do not raise. Tell the
   user the one thing that unblocks it: set the Xero contact in Project settings, or — with their
   yes and the contact named — set it yourself with `update_project_details` (`xeroContactId` +
   `xeroContactName`, from the Xero contacts list in Project settings; echo every other field as
   it is). Then preview again.
3. **Dates come from the user.** `invoiceDate` and `dueDate` (yyyy-MM-dd) go on the preview and
   on the raise. If the user did not say, ask; the defaults are today and the certificate's issue
   date + the contract's final date for payment days (else Xero's sales default), and the preview
   shows exactly which applied. Never raise with a date the user has not seen.
4. **Numbering comes from the INVOICE.** Xero's reference is `Valuation NN` (the valuation
   invoice's number, two digits — VI-0005 → "Valuation 05") and the line reads
   `Valuation NN - Payment due as per <Month yyyy> valuation report (ex VAT)`. Read both back
   from the preview before the yes; the certificate is attached, not described.
5. Take the user's explicit yes, then `raise_valuation_invoice_in_xero` with confirm true and the
   SAME invoiceDate/dueDate the preview showed. An invoice already carrying a Xero id or number is
   refused a second raise; a wrong invoice is voided in Xero, never un-raised.
6. **A Xero invoice raised by hand is recorded, not re-raised.** If the user has already keyed
   the invoice into Xero, issue it here with `issue_valuation_invoice` and its `xeroInvoiceNumber`
   (INV-0227); for one already Issued or Paid whose Xero number is blank (VI-0002 to VI-0005
   were), `record_valuation_invoice_xero_number`. Nothing is written to Xero either way, and the
   row then reads as raised.

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
