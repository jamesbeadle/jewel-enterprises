# 2026-09-09 — MCP connector coverage of the day's work

Audit of everything handed to the accountant today against the JPMS MCP connector: can the
assistant do it, not just the page?

## Coverage before this branch

| Feature (today) | Connector | Verdict |
|---|---|---|
| CIS verification (number + date + short status) | `record_cis_verification`; `search_directory` carries `cisStatus`, `cisVerificationNumber`, `cisVerifiedOn` | covered |
| Link directory record to Xero contact, pull details | `link_directory_record_to_xero_contact` (`pullDetailsFromXero`), `list_unlinked_directory_records`, `unlink_…` | covered |
| Push contacts to Xero | `push_directory_contacts_to_xero` | write covered; **no preview** (the page shows now/after) |
| Send the work order PO email direct, PDF attached | `send_work_order_po_email`, `prepare_work_order_email_draft` (shared handler) | covered |
| Compliance register / Directory compliance chips | nothing — `search_directory` had no standing, no register tool | **gap** |
| Work Order bills: card, figure per order, Approve, Undo | `list_xero_ledger_lines` dropped `WorkOrderMatch` / `WorkOrderApproval` / `XeroInvoiceId`; no approve / undo action | **gap** |
| Scanned PDFs: OCR + page image + refusal wording | `read_source`, `read_email_attachment`, `find_in_source` (shared reader) | covered |

## What this branch adds

- `list_xero_ledger_lines`: each line now carries `xeroInvoiceId`, `reference`, `workOrderBill`
  (matched order, rule, detail, `proposedSlices`, `supplierOpenOrders` with remaining value and
  cost codes, `xeroTrackingWritableForProposedSlices`), `workOrderExceptionReason` and
  `workOrderApproval` (`AiFinanceTools.LedgerLine`).
- Actions `approve_work_order_bill` (`xeroInvoiceId` + `slices[{workOrderId, net}]`) and
  `undo_work_order_bill_approval` — same handlers, same FD/Director/Admin gate, confirm-first,
  actor stamped server-side (`CommercialActions.WorkOrderBills`).
- Tool `list_compliance_register` (standing per company worst first, documents, counts; `status`
  and `search` filters) and `complianceStanding` on every `search_directory` record. The standing
  rule moved to contracts (`ComplianceDocumentExtensions.Standing`) so the page and the connector
  share it.
- Tool `preview_xero_contact_push` — the push modal's now/after for the connector.
- `AiConnectorTests.AccountantsSeptemberNinthAsks_reachTheConnector` pins the lot.

No migration.

## Not built (open, the accountant's call)

- Raising the sales invoice in Xero from the portal (coverage-audit row 41, scoped out).
- Xero MCP connector gaps he listed (tracking dropped on create, no attachment endpoints) — not
  this codebase.
