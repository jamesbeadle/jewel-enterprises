# Refactor audit — baseline v21, after round 19

Generated 2026-09-07 on `main` (3cde2870 plus this round's files, uncommitted), replacing the
v20 baseline adopted earlier the same day. Duplication was measured (jscpd 5.1.2); every other
check is the same heuristic as v19/v20. The v20 reading is kept below as the floor this round
ratcheted from.

## Headline

**709 of 3,614 source files are over the 100-line limit (19.6%)**; the worst file is 525 lines.
Files over the limit is the figure this programme exists to drive down, and it is now stated
first in every report the audit writes (`audit/report.py` gained a Headline section and a
"vs baseline" table this round, so a bare `audit-report.md` answers "how many files are over"
without opening `audit.json`).

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 709, totalFiles: 3614, worstFileLines: 525 |
| functionShape | limit: 30, functionsOverLimit: 728, elseBlocks: 1131, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 41, maxWords: 5, maxLength: 40 |
| duplication | clones: 459, duplicatedLines: 5611, totalLines: 230181, duplicatedPercentage: 2.44 |
| naming | bannedAbbreviationHits: 618, unprefixedBooleans: 1621 |
| comments | explanatoryCommentLines: 14501, filesWithComments: 2075, taskMarkers: 51 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 55, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2567, deeplyIndentedLines: 2815, overlongLines: 1551, measurementIsHeuristic: True |
| inventory | pages: 98, components: 145, orphanComponents: 10, averagePageLines: 209 |

## Round 19 — the four api giants, three of them under test first

The round opened by adopting the 7 September reading as **v20** — the first baseline set from
feature drift rather than earned by a round, because the v19 gate had been failing on seven
figures since the 5 September reading and nobody was standing on it (see "Where v20 came from"
below). Then, worst first among the files the cloud can build:

- **RunXeroCodingSlice 621 → 0**, the §6a coding run — the one path that writes a worker's
  month into Xero. First a characterisation file (`RunXeroCodingHandlerTests`, 21 tests over a
  recording fake Xero and the real schedule builder on an in-memory database): every gate in
  order — sign-off, run-once and the deleted-bill re-code with its preface, mapping gaps
  named, the missing settlement identity, the covered-bill / recognised-bill / two-bills
  paths — the exact draft a no-bill month stages, the exact recode a recognised bill gets
  and how the ledger rows and timesheet covers are re-pointed onto Xero's fresh line ids,
  what a dry run reads but never writes, and what a reset appends. Then the slice became a
  `Commands/XeroCoding` folder: two endpoints and the reset handler as their own types, and
  `RunXeroCodingHandler` as partials named for the concern — MonthReads, Gates, Mapping,
  FindBill, Draft, Recode, RecodedLine, Repoint, StatedMonth — with the outcome wording in
  `XeroCodingWording`. A `CodingMonth` record carries the month's shared reads and a
  `WorkerRun` record one worker's pass, so the per-worker steps take one argument instead of
  nine. Fourteen files, the largest 95 lines; the tests pass unchanged.
- **XeroClient.Writes 568 → 62**, every request that changes a bill in Xero. Pinned first by
  `XeroClientWritesTests` (18 tests over a recording `HttpMessageHandler` playing Xero): the
  JSON each write sends — a recode's pro-rated amounts and echoed `LineAmountTypes` with no
  `Status`, a draft's contact / dates / `DRAFT` / `Exclusive`, an approval's in-place tracking
  and split pieces, a site update's replaced Sites entry — which bills each refuses and with
  what words, and the three ways a staged draft settles its tax type (the contact's default,
  its last live bill, Xero's account default with a note saying so). Then one partial per
  write — Approve, Recode, DraftBill (+ ContactTaxType), SiteTracking (+ SiteTrackingLines) —
  with Bills (the summary read) and ScheduleLines (the §6a line building) shared between
  them, and `.Writes` reduced to what every write shares: the not-connected refusal (one
  constant where there were four copies), `ReadFreshAsync`, `InvoiceOrCreditNote`,
  `MissingSitesError` and `ForgetSnapshot`. Nine files, the largest 97.
- **SetXeroAllocationHandler 422 → 0**, the allocation command, whose 300-line `HandleAsync`
  was the round-18 report's named target. `SetXeroAllocationHandlerTests` (19 tests, 26
  cases) pins every action: what Allocate, bucket, ignore, reset, Set, Dispute, message and
  resolve each leave on the line; every refusal and its sentence; how a split is reconciled
  against the rows already there (kept row updated, others removed, new ones added); when
  work-order links and package slices survive (same project, new centre — and the orders are
  recoded) and when they are cleared; what the dispute thread records; and which Xero write
  follows each action. Then a `Ledger/Allocation` folder: Batch (the command as the run reads
  it), Guards, AllocateValidation, Splits, Apply (one method per action, a shared `Stamp`),
  Links and FollowThrough. Eight files, the largest 88.
- **ImaginePublicService 411 → 93**, the public imagine page's service written on 6 September
  and never audited: Submit (divided into the checks, the round, the photos and the note to
  sales), Revise, React, Proposal and Limits partials, with `ImaginePhotoDecoding` and
  `ImagineWording` as static modules. No test — it was divided by moving whole methods, and
  the api compiles.

Two figures the division nudged the wrong way were paid back before the gate was run: one
long-chain line each in MonthReads, Guards, FollowThrough and SiteTrackingLines became two
lines with a named local, and `RecodeOrdersOnKeptLinksAsync` (six words) became
`RecodeLinkedOrdersAsync`. The gate then passed on all eleven figures against v20.

**What did not happen, and why.** The two Sales pages (`SalesStrategyDetail` 525,
`SalesLeadDetail` 505) and `AdminKpis` (429) are now the worst files and were this round's
named jpms targets, but the Mac's NuGet cache — the cloud's only package source, nuget.org
being blocked — holds no `Microsoft.AspNetCore.Components.WebAssembly`, so the jpms project
cannot compile in the cloud and a Razor division there would have gone out unverified. They
wait, as `GraphMailClient` does, for a round that can build them: either the WebAssembly
package lands in the Mac cache (any `dotnet build jpms` on the Mac puts it there) or the
round runs on the Mac. The test suite likewise cannot run in the cloud (no xunit, no EF
InMemory in the cache); this round's 58 new tests were run through a stand-in harness — a
100-line shim for the xunit attributes and `Assert`, and the EF Core InMemory provider built
from its 8.0.10 source against the cached EF package — and pass 84/84 alongside the three
existing test files the harness also compiles. **They still want a run under real xunit**:
`dotnet test tests/Jewel.JPMS.Tests/Jewel.JPMS.Tests.csproj` on the Mac, or the manual Tests
workflow on GitHub.

## Against v20 (the 7 September floor)

| Ratcheted figure | v20 | **v21** | Δ |
| --- | --- | --- | --- |
| Files over 100 lines | 713 | **709** | −4 (four giants out, 35 files in, 0 of them over) |
| Worst file (lines) | 621 | **525** | −96 |
| Functions over 30 lines | 731 | **728** | −3 |
| `else` blocks | 1,133 | **1,131** | −2 |
| Duplication | 2.44% (458 clones) | **2.44% (459 clones)** | held |
| Explanatory comment lines | 14,603 | **14,501** | −102 |
| Inline hex colours | 43 | 43 | held |
| Orphan components | 10 | 10 | held |
| Long member-chain lines | 2,569 | **2,567** | −2 |
| Deeply indented lines | 2,834 | **2,815** | −19 |
| Overlong function names | 41 | 41 | held |

The files-over-100 figure fell for the first time since v1 despite the round adding 35
files: every new file is under the limit, so the four giants left the list and nothing took
their place. That is the division signature working as intended — it only ever rose before
because a 4,000-line file divides into partials of 100–190 that are still over.

## Where v20 came from

v19 (2 Sep, round 18) → v20 (7 Sep, adopted): files over 100 lines 668 → 713, worst file
475 → 621, functions over 30 lines 692 → 731, comments 13,669 → 14,603, member chains
2,365 → 2,569, deep indentation 2,656 → 2,834, orphan components 6 → 10; duplication
2.70% → 2.44% and `else` blocks 1,153 → 1,133 improved. Eleven of the twenty worst files were
new or newly grown: the KPI, contacts, Xero write-side and site P&L work of 3–4 September,
then the Sales / Imagine pages of 5–6 September (`SalesStrategyDetail` 525, `SalesLeadDetail`
505, `Imagine` 464, `SalesInbox` 457, `ImaginePublicService` 411 and fourteen more Sales
files between 101 and 331) written as whole pages against a deadline and never passed
through the audit.

The four new orphans are a finding in their own right: `TabRow`, `FilterChips`,
`ConfirmDialog` and `LoadingScreen` are shared components no page renders. The first three
were built in the Stage 1 component pass on 6 September and documented in `CLAUDE.md` as the
only way to render tabs, chips and confirmations — but the roll-out did not convert the
pages, which still carry `tab`/`chip` class strings and their own `confirming*` bools.
`LoadingScreen` is mirrored by `index.html`'s boot screen and named in the doctrine, yet
nothing composes it. Adopting the three in the Sales pages when they are divided pays the
orphan figure back.

## The journey so far

| Figure | 22 Aug (v1) | R16 (v17) | R17 (v18) | R18 (v19) | 7 Sep (v20) | **R19 (v21)** |
| --- | --- | --- | --- | --- | --- | --- |
| Files over 100 lines | 385 | 656 | 658 | 668 | 713 | **709** |
| Worst file (lines) | 4,961 | 517 | 475 | 475 | 621 | **525** |
| Average page length | 544 | 208 | 203 | 201 | 209 | **209** |
| Duplication | 4.16% | 2.87% | 2.78% | 2.70% | 2.44% | **2.44%** |
| `else` blocks | 1,087 | 1,170 | 1,156 | 1,153 | 1,133 | **1,131** |
| Functions over 30 lines | — | 697 | 694 | 692 | 731 | **728** |

## Worst files by length

| File | Lines | Note |
| --- | --- | --- |
| jpms/Pages/SalesStrategyDetail.razor | 525 | jpms — waits for a build |
| jpms/Pages/SalesLeadDetail.razor | 505 | jpms — waits for a build |
| worker/MailboxIntake/Graph/GraphMailClient.cs | 475 | worker — waits for the Mac |
| jpms/Pages/Imagine.razor | 464 | jpms |
| jpms/Pages/SalesInbox.razor | 457 | jpms |
| api/Data/JpmsContext.Model.cs | 449 | one `OnModelCreating` — a per-area partial split |
| jpms/Services/Navigation/SidebarFolders.cs | 449 | jpms |
| jpms/Pages/AdminKpis.razor | 429 | jpms |
| jpms/Components/ManualWorkOrderModal.razor.cs | 428 | jpms |
| jpms/Pages/CostCodes.razor | 415 | jpms |
| jpms/Services/HttpLabourStore.cs | 407 | jpms |
| api/Features/Commercial/Documents/ValuationReportSnapshotRenderer.Sections.cs | 402 | api — renderer partials, masked-PDF comparison |
| jpms/Pages/TriageQueue.razor | 399 | jpms |
| jpms/Features/Triage/AttachmentPicker.razor | 396 | jpms |
| jpms/Pages/ProjectVariations.razor | 396 | jpms |
| jpms/Components/ValuationReportTable.razor | 395 | jpms |
| api/Features/Ai/Sources/AiFiledDocuments.cs | 394 | api — the delivery-tools recipe |
| jpms/Pages/ProjectValuation.razor | 388 | jpms |
| api/Features/Procurement/Commands/ExtractTenderFromMessageHandler.cs | 381 | api |
| api/Features/Subcontractors/Documents/SubcontractorStatementRenderer.cs | 378 | api — renderer |

## Round 20, named

If the next round can build jpms, it is the Sales pages: **SalesStrategyDetail (525)** and
**SalesLeadDetail (505)**, then **Imagine (464)** and **SalesInbox (457)** — markup plus
concern partials per `docs/refactor/design-patterns.md` §1, the panels they hold extracted
as widgets under `jpms/Features/Sales`, and their tabs and confirmations rendered through
`TabRow` and `ConfirmDialog` so the orphans stop being orphans. If it cannot, the api list
stands: **JpmsContext.Model (449)** — one 428-line `OnModelCreating` that wants a partial per
entity area; **ValuationReportSnapshotRenderer.Sections (402)** and
**SubcontractorStatementRenderer (378)** by the renderer recipe (partials, verified by
masked-PDF comparison old against new); **AiFiledDocuments (394)** by the delivery-tools
recipe; **ExtractTenderFromMessageHandler (381)** with a characterisation test first, as
this round's three had.

Full detail, including every offender list, is in `audit.json`; the gate ratchets against
`baseline.json`, which this report accompanies.
