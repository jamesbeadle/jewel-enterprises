# Work Order bills tab on Xero Cost Allocation — implementation plan

**Status:** Built 8 Sep 2026 on `feature/work-order-bills` (decisions taken in §13; the open questions in §11 were answered by James or by a default named there)
**Author:** Cowork (for James)
**Source:** the accountant's message of 8 Sep 2026, verified against the codebase and the live portal

---

## 1. The ask, in one paragraph

A supplier bill from a supplier who has an open work order should not wait in Unallocated. On
sync the portal matches the bill to the work order and puts it in a new **Work Order bills** tab
next to Labour, pre-filled from the order: project, cost code(s), the split across the bill's
lines, the link to the order, the order's value, invoiced to date and remaining. Nigel checks the
card and presses **Approve** once per bill. Approval allocates every line, links them to the
order, writes Sites and Cost Code tracking to Xero and approves the bill there, exactly as the
page already does for a draft once every line is allocated. Anything that does not match cleanly
stays in Unallocated with the reason on the row. Everything is audited and undoable.

## 2. What exists today (verified in the codebase)

The good news is that almost every mechanism the feature needs already exists; the feature is
mostly a new *rule* and a new *view* over existing parts.

- **The page** is `jpms/Pages/XeroAllocation.razor` (+ nine partials). Unallocated is already
  partitioned server-side into sub-views: project tabs (by persisted or suggested project) and
  the **Labour** tab, which is exactly the shape wanted here. Labour is not a status — it is
  `LabourSupplierRecognition` (`api/Features/Xero/Ledger`) computed **on every read** of the
  unallocated set and stamped onto `XeroLedgerLine` (`MatchedWorkerId`, `CoveredByTimesheets`…);
  the page partitions on those fields (`IsLabourLine`, `XeroAllocation.Tabs.cs`).
- **"Re-check matches"** is a client-side re-read of the Unallocated status
  (`XeroAllocation.Export.cs:159`) — suggestions and recognition are recomputed by the server on
  read, nothing is persisted. So anything computed the same way is automatically re-run by both
  Sync and Re-check with no extra code.
- **Allocation** is `SetXeroAllocationHandler` (one partial per concern). Allocate stamps
  project + cost centre (or `XeroCostSplits` rows for a centre split), then
  `FollowThroughToXeroAsync` calls `IXeroWriteBackService.TryWriteBackAsync`, which — once
  **every stored line of a DRAFT/SUBMITTED bill is Allocated** — stamps Sites + Cost Code
  tracking per line (physically splitting a Xero line per centre share, pro rata, invoice total
  unchanged) and sets the bill AUTHORISED (`XeroClient.Approve.cs`). Best-effort: the
  allocation saves first, Xero's answer is stamped on the lines (`WriteBackStatus`), Retry exists.
- **Work-order links** already exist: `XeroLineWorkOrderLinkEntity` (line → order → project →
  amount), written by `SetXeroLineWorkOrderLinks` from the project's WO Allocation tab
  (`ProjectWorkOrderAllocation.razor`). Order value, **invoiced to date** (sum of link amounts,
  `ListWorkOrderInvoiceSummariesHandler`) and **paid** (`WorkOrderPaidPositions`, from the linked
  bills' `AmountDue`) are all derived from these links — nothing new to store for the card's
  figures.
- **Work orders** (`WorkOrderEntity` / `WorkOrderLineEntity`): `SubcontractorId`, `Value`,
  `Status` (Draft / Released / Complete / Cancelled / Rejected), and lines each carrying a
  `CostCode` and `LineTotal` — the pro rata split for a multi-code order comes straight off these.
- **Audit trail**: `AuditTrail.WriteAsync` (`api/Features/Audit`) appends `AuditEventEntity`
  rows (actor, event type, project, `RecordType.WorkOrder`, reference, detail). Already used by
  commercial commands (`SetCostCodeBudgetHandler`). `AuditEventType` gets a new pair of values.
- **Supplier ↔ directory**: `SubcontractorXeroLinkEntity` ties a directory company to a Xero
  contact by id and name. The ledger line stores only `ContactName` (no contact id).
- **Tests**: `SetXeroAllocationHandlerTests` (in-memory DB + recording write-back fake) and
  `RecordingXero : IXeroClient` are the harness to extend.

## 3. Findings that change the spec — read before anything else

These came out of checking the message against the code and the live data. Each one either
needs a decision from Nigel or a line in the written spec.

### 3.1 Work-order numbers are per project, so "WO number on the bill" is not unique

`WorkOrderEntity.Reference` is `WO-{Number}` numbered **within a project**, and the PO document
prints exactly that (`WorkOrderPoRenderer.Header.cs`: "Purchase order # WO-0001"). Anything
Electrical today holds **WO-0001 on Ravenswood Ave and WO-0001 on Woodhouse**. A bill saying
"WO-0001" from that supplier fits two orders. Rule 1 therefore has to read *supplier + WO number*,
and even then can be ambiguous. Options for the spec:

- (a) treat supplier + number + more than one fit as the "more than one WO fits" exception; or
- (b) use the bill's Xero **Sites** tracking (already read into `XeroSite` and turned into
  `SuggestedProjectId`) as a project hint to break the tie; or
- (c) print a globally unique reference on the PO (e.g. `JBB-2026-003/WO-0001`) going forward.

Recommend (a)+(b) now; (c) is a separate small change worth doing anyway.

### 3.2 The test case does not pass on the supplier-only rule — both bills are ambiguous

Live data (8 Sep): Anything Electrical (London) Ltd has **five Released work orders on four
projects** — By France WO-0026 (£97,810) and WO-0007 (£5,280, fully paid), Ravenswood WO-0001
(£14,940), Abbot Road WO-0009 (£80), Woodhouse WO-0001 (£43,755). Bills 1724 and 1725 carry no
Sites tracking (`SuggestedProjectId` is null on all four lines). So under the rule as written,
1724 and 1725 only reach the Work Order bills tab **if the bills themselves carry the WO
reference** — otherwise both are the "more than one WO fits with no reference" exception and
"Unallocated down to 9" is not reached.

**Action for James before the spec is agreed:** open 1724 and 1725 in Xero and note what is in
the *Reference* field and the line descriptions. The ledger line descriptions are just
"Electrical bill" / "Electrical works". The plan below matches the reference against
`XeroLedgerLine.Reference` first, then the description and invoice number; the spec should say
which fields count and in what form the supplier is expected to write the number.

The spec also needs a definition of **"open"**. Released only? Released with remaining value
> 0 (which would drop WO-0007 and WO-0009)? Complete orders excluded? Recommend: Released or
Complete-but-not-fully-invoiced, remaining > 0 — but that is Nigel's call.

### 3.3 Anything Electrical is not linked to a Xero contact in the directory

`search_directory` reports `xeroLinked: false` for the company. The order's supplier is a
directory `SubcontractorId`; the bill's supplier is a Xero `ContactName`. Matching therefore
needs a fallback beyond `SubcontractorXeroLink`: normalised company-name equality, the same
approach `LabourSupplierRecognition` already takes for workers. Recommend: link by
`SubcontractorXeroLink.XeroContactName` first, then exact normalised name; **never containment**
for companies (too loose for money). Optionally stamp `XeroContactId` on the ledger line at
sync (one additive column) so the link-by-id path is exact — see §7.

### 3.4 Xero cannot un-approve a bill — the undo needs a decision

The Xero Accounting API moves an invoice DRAFT → SUBMITTED → AUTHORISED → PAID/VOIDED and never
back. `IXeroClient` has no un-approve because there is nothing to call. So "an undo that
reverses … the Xero approval together in one transaction" can be done for the **portal**
allocation, the links, the audit and the Xero **tracking** (strip Sites/Cost Code back off the
lines — a small addition to the client, since `SetSiteTrackingAsync` today only writes Sites),
but the bill stays AUTHORISED (awaiting payment) in Xero. Options for the spec:

- (a) Undo reverses everything portal-side and strips the tracking; the card says plainly
  "the bill remains approved in Xero — void it there if it must not be paid"; or
- (b) Undo additionally **voids** the bill in Xero (allowed while unpaid) — but Dext's copy
  and the attachment are then on a voided bill, and re-entry means Dext re-publishing; or
- (c) Approve **holds** the Xero approval for a short window (e.g. until the next sync or
  N minutes) so an undo inside the window cancels it before it happens.

Recommend (a) with a clear notice, and (b) offered as a second, `Danger`-confirmed action.
Either way, the portal side is one EF transaction.

### 3.5 Links today only exist on whole-line allocations — a multi-code order breaks that

`SetXeroLineWorkOrderLinksHandler` refuses to link a line split across cost centres, and
`SetXeroAllocationHandler.Links.cs` clears links when a line is re-cut as a split.
`GetProjectFinancialSummaryHandler` (line ~108) filters linked slices to lines with a single
`CostCenterCode`. Point 3 (pro rata split across the order's cost codes) means a bill line
carrying `XeroCostSplits` rows **and** a link. The invariant has to be relaxed to: *links may
exist on a line whose shares all sit on the order's project* (a centre split, never a project
split). Consumers to update: `GetProjectFinancialSummaryHandler` (re-attribute a linked split
line by its split rows), `KeepOrClearLinksAsync` (keep links through a same-project split), and
the guard in `SetXeroLineWorkOrderLinksHandler`. `WorkOrderPaidPositions`,
`ListWorkOrderInvoiceSummaries` and `PackageReconciliationCalculator` only read `Amount` and
need nothing.

### 3.6 The order drives the invoice here — the reverse of the existing rule

Today the **invoice drives the order's coding**: linking recodes every line of the order to the
invoice's centre (`WorkOrderInvoiceRecoding`). For a Work Order bill the coding *comes from* the
order, so the approve command must not call the recode. A later hand re-allocation of one of
these lines on the Allocated tab would still recode the order (existing behaviour) — worth a
sentence in the spec saying that is acceptable, or a guard that WO-approved lines are re-cut
through undo rather than re-allocated.

### 3.7 The CIS split is safe by construction

The write-back never touches account codes: it stamps tracking on each existing Xero line, and
when a line is centre-split it clones that line per share with the same account code
(`XeroClient.Approve.cs`). 1724's 321 £6,000 / 322 £4,000 lines stay exactly as Dext published
them. No work needed; the acceptance test should still assert it.

### 3.8 "Unallocated down to 9" checks out

Live Unallocated holds 33 lines: 19 are labour-recognised (Jewel Property Serve, Pranas
Jancauskas, James Everitt, Adam Midgley, Lawrence Downey, Jack Easty — all on the worker
registry, so they sit on Labour today), 1 is on the Woodhouse project tab (Toolstation), 13 sit
on the plain Unallocated tab: Grant & Stone ×5, Anything Electrical ×4, Euroloos ×2, Faction
H&S ×1, Safeguard ×1. Move the four AE lines and the tab reads **9**. The number is the plain
tab's count, not the whole status.

## 4. User stories

1. As the FD, I want a bill from a supplier with an open work order to arrive already coded
   from that order, so nobody re-decides what was decided at approval.
2. As the FD, I want to see, per bill, the order it matched, why it matched, the order's value,
   invoiced to date and remaining, and the coding the portal proposes per line, so I can check
   it in one glance.
3. As the FD, I want to adjust the proposed cost-code split before approving, so an order that
   spans several codes can be corrected when the bill is not pro rata.
4. As the FD, I want one Approve per bill that allocates every line, links the order, writes
   tracking and approves the bill in Xero, so the bill becomes payable with no hand coding.
5. As the FD, I want a bill that does not match cleanly to stay in Unallocated with the reason
   on the row, so I deal with exceptions and nothing is guessed.
6. As the FD, I want to see who approved each Work Order bill and which rule matched it, and to
   undo an approval in one step, so a mistake is reversible and traceable.
7. As anyone on the page, I want Sync and Re-check to re-run the matching, so a bill that missed
   because an order was approved late lands in the right tab next time.

## 5. UI

All within `/finance/allocation`; nothing new in the site map. Components follow the jpms rules
in `CLAUDE.md` (Pill for status, Notice for facts, Toolbar for actions, LoadGate for waits).

- **Tab bar** (`AllocationTabBar`): a **Work Order bills (n)** chip after Labour, rendered when
  n > 0 or the tab is open; n = bills (cards), not lines — the actionable number. Persisted in
  the last-tab memory like Labour (`WorkOrderBillsTabToken`).
- **Work Order bills tab**: one **card per Xero bill** (`WorkOrderBillCard`, new component under
  `Features/Xero/Allocation`), newest first. Card header: supplier, invoice number, date, bill
  net, "Open document" (existing invoice viewer), a Pill for the match rule ("Matched by WO
  reference" / "Matched by supplier"). Card body: the order — reference as a link to the
  project's work-orders page, title, project name, **order value / invoiced to date /
  remaining after this bill** as three `StatTile`s (remaining goes `Tone.Negative` if this bill
  would take the order over value — and Approve is disabled, see §8). Then a `data-table` of
  the bill's lines: description, account (321/322), net, and the proposed coding — a single cost
  code cell for a one-code order, or the pro rata split with editable amounts per code for a
  multi-code order (reuse `SplitEditorForm`, project fixed). Footer: the one `btn-primary`
  **Approve**, disabled while the split does not sum to the line net.
- **Unallocated tab**: an exception row gets a muted one-liner under the identity cell
  (`LedgerLineIdentityCell` gains an optional `Reason`): "Not matched to a work order: two open
  orders for this supplier and no reference on the bill", "…would take WO-0026 over its value by
  £1,240", "…supplier is on the labour registry — settle through the Labour tab". A line with
  no candidate order at all shows nothing (that is the normal case, not an exception).
- **Allocated tab** (`AllocatedSummaryRow`): lines approved this way show a "WO-0026 · approved
  by Nigel, 8 Sep" line and the existing row menu gains **Undo work-order approval** (whole bill,
  `ConfirmDialog`, `Danger`), which replaces the per-line Undo for those lines.
- **Page footnote** gains one sentence about the new tab; the header intro too.

## 6. Data

Two additions, both additive; migration + apply commands ship with the code per `CLAUDE.md`.

- **`WorkOrderBillApprovalEntity`** — the audit and undo record, one per approved bill:
  `WorkOrderBillApprovalId`, `XeroInvoiceId`, `WorkOrderId`, `ProjectId`, `MatchRule` (int enum:
  `ByReference` / `BySupplier`), `MatchDetail` (the text shown on the card, ≤512),
  `ApprovedByEmail`, `ApprovedAtUtc`, `UndoneByEmail?`, `UndoneAtUtc?`. Kept after undo so the
  history reads "approved, then undone"; a re-approval adds a new row. Also written to
  `AuditTrail` as two new `AuditEventType`s (`WorkOrderBillApproved`,
  `WorkOrderBillApprovalUndone`, `RecordType.WorkOrder`).
- **`XeroLedgerLineEntity.XeroContactId`** (nullable, ≤64) stamped by sync from the Xero
  contact — makes the supplier match exact where the directory link exists. Optional; §3.3.

Deliberately **not** stored: the match itself. It is computed on read like labour recognition
(§2), so Sync and Re-check re-run it for free, an order approved late is picked up next read, and
there is no stale match table to maintain. The proposed split is derived from the order on read
and edited in the browser; the Approve command carries the final split.

## 7. Backend — commands and queries

### 7.1 Matching (a rule class, not a handler)

`WorkOrderBillRecognition` in `api/Features/Xero/Ledger`, built by `XeroLedgerReads` beside
`LabourSupplierRecognition` only when unallocated lines are being read. Per **bill** (lines
grouped by `XeroInvoiceId`), evaluated in order:

1. **Labour first.** If any line is labour-recognised, no match, reason "labour registry" —
   the cover route wins (point 4).
2. **Supplier.** Resolve the Xero contact to a directory company: `SubcontractorXeroLink` by
   contact id (if stored) or contact name, else exact normalised company-name equality. No
   company → no match, no reason (ordinary bill).
3. **Open orders** for that company: status per the spec's definition of open (§3.2), across
   all projects.
4. **By reference.** Find a `WO-nnnn` (or bare number the spec allows) in `Reference`, then
   `Description`, then `InvoiceNumber`. Exactly one open order of that supplier with that
   number → match `ByReference`. Several (per-project numbering, §3.1) → break the tie with the
   bill's Sites-tracking project if present, else exception "more than one WO fits".
5. **By supplier.** No reference: exactly one open order → match `BySupplier`; several →
   exception; none → no match, no reason.
6. **Value gate.** Bill net (sum of its lines, credit notes negative) must not exceed the
   order's remaining (`Value` − Σ existing link amounts). Over → exception "takes WO over its
   value by £x". Credit notes always fit.

Output per line (new fields on `XeroLedgerLine`, null when nothing applies):
`MatchedWorkOrderId`, `MatchedWorkOrderReference`, `MatchedWorkOrderTitle`,
`WorkOrderMatchRule`, `WorkOrderMatchDetail`, `WorkOrderValue`, `WorkOrderInvoicedToDate`,
`WorkOrderExceptionReason`, and `ProposedSplits : IReadOnlyList<XeroCostSplit>?` (pro rata per
line over the order's lines by `LineTotal`, `XeroSplitMaths.ProportionalShares`; a one-code order
yields a single share).

### 7.2 `ApproveWorkOrderBill` (command, `contracts/Xero`)

`ApproveWorkOrderBill(XeroInvoiceId, WorkOrderId, IReadOnlyList<WorkOrderBillLineCoding> Lines)`
where a line coding is `(XeroLedgerLineId, IReadOnlyList<XeroCostSplit> Splits)`.
Gates in the endpoint, in order: authorisation (`Role.Admin`, `Director`, `FinanceDirector`
only — narrower than `XeroLedgerRoles.AllowedToAllocate`, this is the FD's button), validation
(ids present, every line of the bill present exactly once, each line's splits sum to its net,
every code active, every split on the order's project), then the handler's guards: every line
still Unallocated, the bill still DRAFT/SUBMITTED, the order still open, the recognition
rule **re-run server-side** and agreeing with the requested order (so the client cannot approve
a bill against an order the rule would not give it), value gate re-checked.

Handler, one EF transaction: stamp every line Allocated (whole-line coding or `XeroCostSplits`
rows, `AllocatedBy` = signed-in user, `Note` = "Work order WO-0026"), add one
`XeroLineWorkOrderLink` per line for its full net, **do not** call `WorkOrderInvoiceRecoding`
(§3.6), add the `WorkOrderBillApproval` row, save. After the save: `TryWriteBackAsync([invoice])`
— the existing service does the tracking + AUTHORISED write and stamps the outcome; then
`AuditTrail.WriteAsync`. Returns the write-back outcome so the card can say "approved in Xero" or
"allocated; Xero said: … — Retry".

### 7.3 `UndoWorkOrderBillApproval` (command)

`UndoWorkOrderBillApproval(XeroInvoiceId, VoidInXero = false)` (the flag only if option 3.4(b)
is chosen). Guards: an un-undone approval row exists; no line of the bill has since been
re-allocated by hand; the bill is not PAID. One transaction: reset every line (`Reset`
semantics), delete the bill's `XeroCostSplits`, `XeroLineWorkOrderLinks` and any
`ReconciliationPackageCostLines`, stamp the approval row undone, save. After the save:
`IXeroWriteBackService.TryClearTrackingAsync(lineIds)` (new: strips Sites and Cost Code from the
lines — `XeroClient` gains the symmetric write), then the audit event. The bill's Xero status is
reported honestly on the toast (§3.4).

### 7.4 Reads

`ListXeroLedgerLines(Unallocated)` already returns everything the tab needs once the new fields
are on the line; the page groups by `XeroInvoiceId` into cards, the same way it partitions
Labour. `ListXeroLedgerLines(Allocated)` carries `WorkOrderApproval` (reference, approver, date)
for the Allocated-tab row. No new query.

## 8. Rules the card enforces (client) and the handler re-checks (server)

- One Approve applies to every line of the bill; there is no per-line approve.
- Approve is disabled until each line's split sums to the line net to the penny, and while the
  bill would take the order over its remaining value (server re-checks against fresh links).
- The proposed split is pro rata by the order's line totals; the user may move money between
  the order's codes only — adding a code the order does not carry is not offered (that is a
  hand allocation, and the bill can be sent to Unallocated for it via "Not a work-order bill",
  a this-visit escape like `notLabourIds`).
- Approved lines carry `Note = "Work order WO-0026"` and the approval row; the Allocated tab
  shows it.

## 9. Files

**contracts/Xero** — `XeroLedger.cs`: new fields on `XeroLedgerLine` (keep the record under 100
lines by moving the labour + work-order fields into a `XeroLedgerLineMatches` record if it
tips over); new `WorkOrderBills.cs`: `ApproveWorkOrderBill`, `UndoWorkOrderBillApproval`,
`WorkOrderBillLineCoding`, `WorkOrderMatchRule`.
**contracts/Models/AuditEvent.cs** — two event types.
**api/Data/Entities** — `WorkOrderBillApprovalEntity.cs`; `XeroLedgerLineEntity.XeroContactId`;
`JpmsContext` set; migration `AddWorkOrderBillApprovals`.
**api/Features/Xero/Ledger** — `WorkOrderBillRecognition.cs` (+ `.Supplier.cs`, `.Reference.cs`
partials to stay under 100 lines), `XeroLedgerReads.ToModel` gains the recognition argument,
`ListXeroLedgerLinesHandler` builds it; `WorkOrderBills/ApproveWorkOrderBillHandler.cs`,
`…Authorisation.cs`, `…Validation.cs`, `…Endpoint.cs`, `UndoWorkOrderBillApprovalHandler.cs`
(+ its trio); `XeroWriteBackService` + `IXeroWriteBackService`: `TryClearTrackingAsync`;
`SyncXeroLedgerHandler`: stamp `XeroContactId` (needs `XeroTransaction.ContactId` from
`XeroClient.Reads`).
**api/Features/Xero** — `IXeroClient` + `XeroClient.SiteTracking.cs`: clear-tracking write;
`NullXeroClient`; tests' `RecordingXero`.
**api/Features/Commercial** — `GetProjectFinancialSummaryHandler` split-aware link
attribution; `SetXeroLineWorkOrderLinksHandler` guard relaxed to "same project"; and
**api/Features/Xero/Ledger/Allocation/SetXeroAllocationHandler.Links.cs** keeps links through a
same-project split (§3.5).
**jpms** — `Features/Xero/Allocation/WorkOrderBillCard.razor` (+ `WorkOrderBillLinesTable.razor`,
`WorkOrderFigures.razor`), `AllocationTabBar` chip, `LedgerLineIdentityCell` reason,
`AllocatedSummaryRow` approval line + undo item, `Pages/XeroAllocation.WorkOrderBills.cs` (tab
state, grouping, approve/undo calls), `Services/IXeroLedgerStore` + implementation: `ApproveWorkOrderBillAsync`,
`UndoWorkOrderBillApprovalAsync`, `AllocationTabStorage` token; `StatusTones.cs` for the match
rule Pill; `PageContext` unchanged (same route).
**docs** — `docs/00-business-context/glossary.md` ("Work Order bill"), `CLAUDE.md` working note
("a Work Order bill is coded from its order — the one place the order drives the invoice"),
`docs/ai/skills/jpms/…` page guide for the allocation page.

## 10. Tests and acceptance

Unit (in-memory DB, recording fakes, `tests/Jewel.JPMS.Tests`):

- `WorkOrderBillRecognitionTests`: reference beats supplier; per-project duplicate numbers →
  Sites tie-break → exception; supplier with one open order matches, with two does not; labour
  registry excludes; value gate; credit notes; company name match is exact, not containment;
  pro rata split sums to the line net to the penny.
- `ApproveWorkOrderBillHandlerTests`: every line allocated + one link per line + approval row +
  one `WriteBack:inv` call; refuses a stale bill, a re-allocated line, a mismatched order, a
  split that does not sum, a split with a foreign code; never recodes the order.
- `UndoWorkOrderBillApprovalHandlerTests`: reverses lines, splits, links, package slices in
  one save; asks for the tracking clear; refuses a paid bill and a bill re-allocated by hand.
- `SetXeroAllocationHandlerTests`: links survive a same-project centre split (new invariant).
- `GetProjectFinancialSummaryHandler` characterisation for a linked split line.

Acceptance (the accountant's test case, to run on prod after the spec confirms 3.2):
Anything Electrical 1724 (By France, £10,000: 321 £6,000 + 322 £4,000) and 1725 (Ravenswood,
£5,976: 321 £3,000 + 322 £2,976) both appear on Work Order bills pre-filled from WO-0026 and
Ravenswood WO-0001 (`ELE-STD`, one code each, so no split editor); Approve once each; all four
lines Allocated with `ELE-STD`, four `XeroLineWorkOrderLinks` (£6,000 + £4,000 on WO-0026;
£3,000 + £2,976 on WO-0001); both bills AUTHORISED in Xero with Sites = the project's mapped
site and Cost Code = ELE-STD on every line; 321/322 account codes and CIS unchanged; invoiced to
date on the two orders up by £10,000 and £5,976; plain Unallocated count 9; two audit rows naming
the approver and the rule; Undo on 1725 returns its two lines, removes its two links, clears the
tracking, and the Allocated count drops by two.

## 11. Open questions for the written spec

1. **What is on 1724 and 1725** that identifies the order — Reference field, description, or
   nothing? (§3.2) If nothing, the test case needs a project hint or the rule needs widening.
2. **Definition of "open"** work order — status set and whether remaining value > 0 is
   required. (§3.2)
3. **Per-project WO numbering** — accept the supplier + number + Sites tie-break, or change the
   printed reference? (§3.1)
4. **Supplier matching without a Xero link** — exact company-name match acceptable, or should
   Nigel link the directory records to Xero contacts first (a one-off tidy-up)? (§3.3)
5. **Undo and Xero** — reverse portal + tracking only, or void the bill too? (§3.4)
6. **Multi-code orders** — confirm the relaxed link invariant and that the pro rata split may be
   edited only between the order's own codes. (§3.5, §8)
7. **Who may approve** — FD and Directors only, or the whole allocation audience?
8. **Bills that pay several orders at once** (a subcontractor invoicing a main order plus a
   variation order together) — always an exception, or a card with a per-order split later?
9. **Later hand re-allocation** of a WO-approved line — allowed (and it recodes the order, as
   today), or must go through Undo first? (§3.6)

## 12. Sequencing and touch-points with the dialog-bug session

The other session has uncommitted work in `RunXeroCodingHandler.*`, `IXeroClient`,
`NullXeroClient`, `SettlementScheduleBuilder`, `LabourEntities` and the model snapshot. This
feature touches `IXeroClient` / `NullXeroClient` / `RecordingXero` (the clear-tracking write)
and adds a migration (snapshot). Wait for that work to be committed, then branch; add our
migration after theirs so the scoped script (`CLAUDE.md` → Database migrations) applies cleanly.
Everything else here is new files or the Xero ledger / commercial handlers, which that session
does not touch.

Suggested build order once the spec lands: (1) recognition + fields + tests (read-only, safe to
ship alone — the tab appears, Approve not yet wired); (2) approve command + card; (3) undo +
clear-tracking write; (4) financial-summary and link-invariant changes; (5) docs and CLAUDE.md
note; (6) acceptance run on 1724/1725 with Nigel.

## 13. Decisions taken at build (8 Sep 2026)

1. **Undo** reverses the portal side and clears the Xero tracking; it never voids. The toast
   and the audit row say the bill stays awaiting payment in Xero when it was approved there.
   James: "if he has the edit function he doesn't need delete."
2. **Open** = Released with remaining value (order value − linked invoiced to date) > 0.
3. **Reference matching** reads `WO`/`PO` + number from the bill's Reference, then the line
   descriptions, then the invoice number — first field that names any order decides; two
   different numbers in it is an exception. Supplier + number; the bill's own Sites hint breaks
   a tie across projects, and also narrows the supplier-only rule.
4. **Supplier matching** uses the directory's own rule (`DirectoryXeroMatcher`, the house
   "does this name mean that company" rule) over the record's company name and its linked
   Xero contact name — so Anything Electrical matches without a Xero link. No new contact-id
   column was needed.
5. **Multi-code orders** relax the link invariant to same-project centre splits; the shares
   may only move between the order's own codes.
6. **Roles**: Approve and Undo are Admin / Director / Finance Director.
7. **A bill paying several orders at once** is an exception ("more than one work order") —
   linked by hand on the WO Allocation tab as today.
8. **Later hand re-allocation** of a WO-approved line is allowed and behaves as today; Undo
   then refuses (it would throw that decision away) and says to reset on the Allocated tab.
9. The **sweep** (Allocate all matched, nightly worker) skips matched bills, like labour.

