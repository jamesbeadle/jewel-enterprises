# 2026-09-09 — Work Order bills across several orders; contacts pushed to Xero

Branch `feature/work-order-bill-splits-and-xero-contacts` off `main` (`f8ba5f8`). Built after
Jeremy's 12:14 and 12:48 answers. Not compiled — no dotnet in either shell, NuGet denied for
Claude — so the tests below are written, not run.

## Jeremy's asks (12:14, decided 12:48)

- One bill spanning two orders (Sussex Tiling Lees Green-001: £1,748 to WO-0055, £1,344 to
  WO-0056) could not use the Work Order bills tab. Decision: propose the per-line split where
  each bill line names its WO, shown on the confirm and editable before Approve; the hand split
  still available where lines do not name an order.
- Push the record's contacts to the linked Xero contact (12:18 conditions): person-pressed,
  never automatic; the confirm shows Xero's people now and after; an empty portal list never
  clears Xero's people; primary contact → Xero's primary person; names split on the first
  space; five is Xero's limit. Scope: `accounting.contacts` is already on the Cost Integration
  app; Jeremy is reconnecting the integration today and will confirm.

## Built

### Work Order bills across orders (`5156d81`)
- `contracts/Xero/WorkOrderBills.cs`: `WorkOrderBillShare` (order + code + net) replaces
  `XeroCostSplit` in a line's coding; `WorkOrderBillMatch` carries `ProposedShares` and
  `SupplierOrders` (`WorkOrderBillOrderOption`, the supplier's open orders with figures and
  codes); `ApproveWorkOrderBill` loses its single `WorkOrderId`; the approval stamp lists
  `Orders`; `WorkOrderMatchRule.ByLineReference` is new.
- Recognition: `…ByLine.cs` (new) proposes per line when descriptions name ≥2 orders; the
  whole-bill rule sends a bill naming several orders to the card on the first, gated against
  their combined remaining value (`Assignment.Pool`); otherwise the value gate is per order.
- Approve: refuses an order not the supplier's; per-order value gate; one link per share with
  `XeroLineWorkOrderLinkEntity.CostCenterCode` (new nullable column; unique index widened to
  line + order + code — migration `20260909150000_AddXeroLineWorkOrderLinkCostCenterCode`,
  additive); cost splits grouped by project + code; one approval row per order with its slice.
  Undo marks every order's row. `WorkOrderLinkSlices` uses a link's own centre when set.
- jpms: `WorkOrderBillShareDraft`; the card lists the orders paid and shows figures per order;
  the share editor has an order column, a code select for multi-code orders, remove, and
  "Add a share on…" over the supplier's open orders; Allocated row shows `OrdersLabel`.
- Tests updated to the new contract; added: per-line proposal, the two-order hand split
  reaching the card, the decided-project tie-break, cross-order approve (links, approval rows,
  stamp), foreign-order refusal, per-order over-value refusal.

### Xero contact push and primary person (`4c593b9`, docs `3934858`)
- `IXeroClient.GetContactPeopleAsync` / `SetContactPeopleAsync` (`XeroClient.ContactPeople.cs`;
  Null and Recording clients updated). `XeroSupplier.PrimaryPersonName` read from the contact's
  own FirstName/LastName.
- `api/Features/Subcontractors/XeroContacts`: `XeroContactPushPlanner` (the one rule),
  `XeroContactPushContext` (record + link + contacts + Xero now), preview query
  `PreviewXeroContactPush`, command `PushDirectoryContactsToXeroContact` with gates and
  endpoints (`GET`/`POST /api/subcontractors/{id}/xero-contact-push`), audit
  `DirectoryContactsPushedToXero = 45`; `XeroDetailsPull` for the opt-in pull on link.
- `LinkDirectoryRecordToXeroContact.PullDetailsFromXero` (default false); import seeds the
  primary contact from Xero's primary person.
- jpms: `XeroContactPushModal` + `XeroContactPeopleList` (now / after, warnings); "Push contacts
  to Xero…" on a linked record; "Also pull the contact's details from Xero" checkbox on the link
  modal; store + routes. Connector: `push_directory_contacts_to_xero` (confirmation required);
  `link_directory_record_to_xero_contact` documents `pullDetailsFromXero`.
- Tests: `XeroContactPushPlannerTests` (replace, empty side untouched, five cut, name split).

## Apply
```
cd api
dotnet ef migrations script 20260909110000_DropCompanyContactPurpose --idempotent -o migrate.sql
sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -i migrate.sql -b
```
Additive — before or with the deploy.

## Open
- Build and run the tests (James).
- Jeremy to confirm the Xero reconnect; until then the push returns Xero's 403 message.
- Not done: nothing else from the morning is outstanding.

## Addendum, 15:10 — Jeremy 14:53: figures off the bill total, not the Xero lines

Branch `feature/work-order-bill-slices-per-order` (`07ee06f`). The order split and the CIS
line split are different axes; the first version tied them together and its write-back would
have replaced a split Xero line with one line per share — the recoding he refused.

- `WorkOrderBillOrderSlice` (order + net) replaces the per-line shares in the contract;
  `WorkOrderBillMatch.ProposedSlices` is bill-level, identical on every line; `ApproveWorkOrderBill`
  takes `Slices`. `WorkOrderBillLineCoding` / `WorkOrderBillShare` are gone.
- `WorkOrderBillSliceSpread` (api) spreads each slice over the lines pro rata, penny-safe per
  line, drift settled on the largest line, then over the order's codes; `Allocate` unchanged.
- `XeroWriteBackService.WriteBackInvoiceAsync(keepLinesWhole)`: a Work Order bill's Xero line is
  stamped whole with the centre carrying most of it — never split.
- jpms: `WorkOrderBillOrderSlices` (figure per open order, tally against the bill) replaces the
  share editor and order figures; `WorkOrderBillLinesTable` is read-only.
- Tests: fixture and approve/recognition tests moved to slices; `WorkOrderBillSliceSpreadTests`
  pins Lees Green (720/2372 vs 1748/1344), the penny settlement and the code pro rata.
