# 2026-09-09 — The sales invoice raised in Xero from the claim card

The accountant's ask (his Cert 15 job, 9 Sep): the portal knew the certified figure, held the
certificate PDF and the project's Xero site, and he still raised the sales invoice in Xero by
hand, tracked it by hand and attached the PDF by hand. Coverage-audit row 41 had scoped this out
("Xero raises the invoice"); he said build it.

## What it does

- The claim card's Issue step is now **Raise in Xero & issue…** (`ValuationInvoiceXeroRaiseModal`,
  also in the invoices section's menu). It shows the plan first — client as Xero knows them, net,
  VAT reading, Sites option, invoice/due dates, the certificate to attach, every blocker — then one
  press raises the AUTHORISED ACCREC invoice in Xero, attaches the certificate, stamps Xero's id
  and number on the valuation invoice and issues it (the existing issue handler runs last).
- "Issue without raising in Xero" stays inside the modal for an invoice raised by hand.
- Issued rows show `Xero INV-0123`; the audit trail gains a "Raised in Xero" event with the total
  and the VAT reading.
- Connector: `preview_valuation_invoice_xero_raise` (read) and `raise_valuation_invoice_in_xero`
  (confirm-first, actor stamped); `list_valuation_invoices` carries the Xero id/number/date.
- `issue_valuation_invoice` is now described as the without-Xero route.

## Rules that decide the figures

- Contact: Client account's name (else project `ClientName`) → a Client-category directory
  record's Xero link → Xero contact by exact name → created with the invoice.
- Net = the valuation invoice's cash `Amount` (the certified net for payment). The amount must
  already be right — `update_valuation_invoice` / Edit fixes it before raising.
- VAT: contact's default sales tax type → their last sales invoice → Xero's account default. Said
  every time, never assumed (new-build work is zero-rated, refurbishment is standard — the
  contact's default in Xero is the rule).
- Sales account: `Xero__SalesAccountCode`, default 200.
- Due date: certificate issue date + contract `FinalDateForPaymentDays` when both exist, else
  Xero's sales default (DueDate omitted).
- Tracking: Sites only.

## Migration

`20260910110000_AddValuationInvoiceXeroRaise` — three nullable columns on `ValuationInvoices`.
Additive; apply before or with the deploy.

## Needs from Jeremy

The Cost Integration app's `accounting.attachments` scope (Xero developer portal) — without it the
invoice is raised and the attachment step reports the 403 in the outcome.

## Not compiled here

Written in the cloud sandbox without NuGet; the build is the Mac's / CI's.
