# 11 — Connector parity audit: the whole site against the MCP surface

*Written 2026-08-31, the day the owner starts using the portal through Claude. Every page route in
jpms/Pages (99 routes, 63 distinct pages) was traced — its reads, every command its buttons and
modals can send, and the workflow logic its layout enforces — and joined against the action
registry (245 declared actions), the 81 recorded `// Skipped:` entries, the write tools, and the
read-tool catalogue. Method per page: razor → injected stores → command contract types → `typeof`
in `Features/Ai/Tools/Actions/*.cs`. This document is the parity ledger; §5 is the fix order.*

## 1. The headline

**Write parity is in good shape.** Every command a page can send is mirrored, recorded as a
deliberate skip, or covered by a first-class write tool — with the exceptions in §2, which are
bookkeeping gaps, not design decisions. The house pattern holds everywhere it was checked: email
SENDING is never mirrored (draft-then-human-sends), and the subcontractor portal's own commands
are all skipped on purpose (the connector is internal-facing).

**Read parity is the real finding.** The gateway grew to ~245 write actions while the read
surface stayed at the chat-era tool set, so many pages are now *write-enabled but read-dark*: the
model can change data it cannot see. The worst of these are listed in §3 as blind-write pairs —
they are the priority, because a model acting on data it cannot read is how figures go wrong.

## 2. Write gaps — fit the pattern, not declared, not recorded

These have Authorisation classes DI-registered, so the standing skip rationale ("no gate class")
does not apply. Each needs either an `AiAction` declaration or a written skip note; silence is the
one state the registry convention does not allow.

| Commands | Page | Note |
|---|---|---|
| CreateWeeklyCashflowItem, UpdateWeeklyCashflowItem, ArchiveWeeklyCashflowItem, PlaceWeeklyCashflowEntry, SetWeeklyCashflowExclusion, SaveWeeklyCashflowSupplierGroup, DeleteWeeklyCashflowSupplierGroup | Weekly Cashflow | The accountant's 13-week plan, entirely absent. Declaring the writes without the plan read (§3) would be worse than useless — do them together. |
| AddInventoryItem, UpdateInventoryItem, CreateInventoryItemFromMessage | Project Inventory / Control Centre | CreateInventoryItemFromMessage is the most MCP-natural (starts from an email) and is the one clean gap on the Control Centre pathway panes. |
| AdjustTimesheet | Project Labour | The by-name labour analogs cover approve/reject/submit but not per-timesheet hour adjustment. |
| ApproveTimesheets, RejectTimesheet | Project Labour | Deliberately superseded by ApproveWorkerWeekByName / RejectWorkerDayByName — but the supersession is unrecorded at the file bottom; add skip notes. |
| SaveAiSkillReference | AI Skills | save_skill covers the skill body; a skill's REFERENCE documents cannot be written over MCP. |
| auth invite / send-reset | Admin Users | Plain auth endpoints outside the command pattern; record as skips. |

Registry drift found in passing: tool descriptions and RequestsActions notes still reference
`read_selected_email`, which is no longer registered anywhere. Scrub the references.

## 3. Read gaps — what the connector cannot see

Grouped by how much they matter. **Blind-write pairs** (a mirrored write acting on unreadable
data) are marked ⚠ — these make the model guess at current state before writing.

**Mailbox and triage (the largest single hole).** Nothing over MCP lists or searches mail that is
not already linked to a record: the Control Centre queue (untriaged / discarded / tagged), the
Document Triage queue, the three Communications registers (project roll-up, SubComms /
SupplierComms / InternalComms family tags), and any mailbox search. The Control Centre's write
actions are almost all mirrored ⚠ — a model could triage an email it has no way to list or read.
An email-intake read family (list_triage_queue, get_message, list_document_triage,
list_communications) is the precondition for the email-triage skill being usable end-to-end.

**Finance.** No read tool for: the cash forecast/statement family (valuation-invoice payment
status, live retention schedule, unpaid-bill remainders, drawdowns — per project or company); the
weekly cashflow plan ⚠-to-be; aged payables/receivables (and the drafts-included convention that
makes Xero's own report wrong); the Xero allocation ledger, transactions feed and site P&L;
payment certificates; package reconciliation rows ⚠ (SaveReconciliationPackage / lock are
mirrored); Xero line→work-order link slices ⚠ (SetXeroLineWorkOrderLinks is mirrored, and links
are sent as a complete slice list — writing blind silently drops allocations); the reconciliation
audit trail (the model can cause CostCentreRecoded events it cannot read back).

**Commercial records.** The valuation-invoice register ⚠ (all nine invoice lifecycle writes are
mirrored; the register, statuses and certified-to-date are unreadable); valuation snapshots
(frozen statements — the client-safe artefact — unreadable); the lead list (capture/won/lost are
mirrored writes ⚠, and Estimating Queue / Sales Analytics / Nurture are all lead reads); the rate
library ⚠ (add_rate/revise_rate mirrored, current rates unreadable, staleness invisible); tender
enquiry LISTING (context tool needs an id; find_by_reference does not resolve TEQ numbers);
architect instructions (whole feature dark, see §4); the client and architect registers (their
create/update writes are mirrored ⚠; search_directory reads the subcontractor table); internal
staff list; compliance standing per subcontractor; portal-raised variation requests (the
accept/reject queue on Project Variations).

**Delivery and back office.** Programme detail, baselines and LAD/EOT claims ⚠ (nine programme
writes mirrored); the progress report/update register ⚠; building control case + inspection
ladder ⚠; project calendar ⚠ (writes mirrored, no event listing); drawings folder tree and
revision history (list_sources shows current revisions only, max 60, no approval trail); the
labour monthly overview, worker roster ⚠ (add_worker mirrored) and Xero mappings; company
registers (insurances/subscriptions/vans — "what lapses this month" is exactly an assistant
question); policies and their sign-off ledger; useful-information notes (internal wall — any
future read tool must keep the internal-only gate); to-do activity trails and linked-todo graph;
the admin user/role directory; the audit trail (possibly deliberate — decide and record).

## 4. Skips worth revisiting (and skips that should stand)

Unlockable by adding gate classes (the AddWorker 2026-08-28 pattern): the plain-JSON architect
instruction commands (Update, ImportFromMessage — the multipart ones stay skipped), the triage
queue's Discard/Restore/RemoveTag and CreateRequestFromMessage (inline TriageRoles),
SetXeroAllocation and friends (inline XeroLedgerRoles — only worth it once the ledger is
readable), the labour/registers/policies clusters (inline role sets throughout), and
UpdateManualWorkOrder (server-derived flag). Standing on purpose: every multipart upload and the
whole subcontractor portal. (SendMailboxEmail stood here too — "drafts only, the human presses
send" — until 2026-09-04; see §7. The stance had already been crossed by send_work_order_po_email,
and the connector's own doctrine telling every AI tool "the connector never sends email" was
sending users back to Nigel with "your MCP can't do what the website does".)

## 5. Recommended fix order

1. **Record the §2 gaps** — declare or skip-note every one; cheap, and restores the "silence is
   not allowed" invariant. Include the read_selected_email reference scrub.
2. **Valuation invoices + snapshots read tool** — the deepest blind-write pair, on the flagship
   money path, and Nigel's first use case.
3. **Email-intake read family** — unlocks the Control Centre and Document Triage skills
   end-to-end; without it the triage doctrine has nothing to act on.
4. **Finance reads** (forecast/statement, weekly cashflow plan + its writes together, aged
   payables/receivables with the drafts convention baked into the description).
5. **Small-register reads batch** — leads, rates, tender-enquiry list, calendar, building
   control, programme, architect instructions, registers, worker roster: one modest list/get tool
   each, most of them read-only pages today.
6. **Unlock skips** per §4 as the reads land.

## 6. The skills the pages imply

The audit confirmed the working theory: a page's server calls are actions; the procedure its
layout enforces is a skill. The doctrine below was extracted from the pages themselves and is
ready to be written into the skill store and attached (area level) on the AI Actions page.

- **jpms-email-triage** (→ Requests & RFIs, Correspondence): the Control Centre's apply order —
  verify inherited tag stems first, then doc-triage copies → to-dos → record links → staged
  creates → system actions → discards → outbox replies → the anchor reply LAST so a send failure
  loses nothing; one create per pass; attachments before body; cross-pathway filing is allowed —
  the pane choice IS the decision; replies stamp the thread handled only when nothing else filed
  it.
- **jpms-document-filing** (→ Document control, Drawings): revision inherits code/title from the
  target drawing; a new drawing resolves its folder first; certificate dates are date-only UTC;
  discard is restorable, never delete; current revision = approved else newest.
- **jpms-valuation-cycle** (→ Valuation invoices, Commercial): the claim stepper — value & lock →
  raise & send (freezes the snapshot) → approval → issue (moves certified-to-date) → payment →
  confirm & roll over; payment never gates the next claim; new claims seed cumulative % from the
  latest; retention stamped server-side; the client sees the FROZEN snapshot, never the working
  copy.
- **jpms-variation-lifecycle** (→ Variations): one document, one number, status says where it is;
  stage the build-up (total becomes the estimate, approve modal pre-seeds) and let the USER press
  approve; approval mints the V-ref and mirrors lines onto the valuation; work-order issue from a
  variation is UI-only — fall back to create_manual_work_order.
- **jpms-tender-award** (→ Procurement, Tender enquiries): scope + lines + drawings → invite
  (send is human) → extract quotes from email, never hand-type figures → award mints the WO →
  PO email as the distinct second step; tender-only prospects stay out of the directory until
  award; bid packages are not a variation stage.
- **jpms-xero-allocation** (→ Cost centres, and the ledger tools when they exist): bucket beats
  project+centre when both set; "Set" is the half-step that writes the Xero site without
  allocating; dropdown picks alone move nothing; labour-registry suppliers' bills are settlement,
  not costs; drafts are deliberate — Xero's own aged reports undercount.
- **jpms-cash-forecast** (→ Cashflow): amounts authoritative, months indicative; no honest date →
  Undated, untouched balance; the FD's two knobs change timing never amounts; phased months must
  tie to the statement to the penny; weekly grid moves change WHEN not HOW MUCH.
- **jpms-labour-rules** (→ Labour): view week → code → approve, in that order (approved rows
  snapshot their rate); only approved time becomes cost; sign-off freezes the week before
  settlement; rates never reach site surfaces; mappings close-and-replace, never edit.
- **Full-record write rule** (belongs in jbb-second-brain): UpdateSubcontractor,
  UpdateRequestDetails and their kin round-trip the ENTIRE record — read first, resend every
  field, or data is erased. The same brain gains: draft WOs have no number (list by status, not
  reference); set_cost_code_budget and Xero WO links take absolute/complete values.

Attachment mechanics are docs/ai/10 §2d; the live area mapping is the AI Actions page (the seed scripts were removed 2026-09-10 — the database is the source of truth, docs/ai/skills its mirror).

## 7. Resolution — 2026-08-31, same day

The fix order was worked the same day the audit was written. Shipped:

- **§2 write gaps — all recorded.** WeeklyCashflowAndInventoryActions.cs declares the weekly
  cashflow suite (7) and inventory suite (3); AdjustTimesheet and the timesheet-supersession
  notes are written skips there; save_skill_reference joins the write tools; the AdminUsers auth
  endpoints are recorded as skips (SubcontractorsAndLeadsActions); every stale
  read_selected_email reference now names get_mailbox_message.
- **§3 read gaps — the surface shipped as five tool files**: AiValuationInvoiceTools (invoices,
  snapshots, frozen snapshot lines), AiMailboxTools (triage queue, message, conversation, search,
  document triage, project communications), AiFinanceTools (weekly cashflow plan, aged
  payables/receivables with the drafts doctrine, payment certificates, allocation ledger + counts),
  AiRegisterTools (leads, rates, tender enquiries, clients, architects, workers, company
  registers, portal users, cross-project RFIs, useful information), AiDeliveryTools (calendar,
  building control, programme + LAD claims, architect instructions, progress, drawings + revision
  history, package reconciliation). Every tool wraps the query handler its endpoint composes and
  mirrors that endpoint's role gate.
- **§4 unlocks**: gate classes + declarations for the five plain-JSON architect-instruction
  commands (delete is confirm-first) and the triage quartet (create_request_from_message,
  discard/restore/remove-tag). Endpoints keep their inline checks deliberately — both sides read
  the same RoleSet constant, so there is one source of truth (rationale in the gates files).
- **§6 skills**: nine written to docs/ai/skills/jpms/ (the eight extracted plus
  jpms-connector-mechanics carrying the full-record-write rule) and attached to their areas on
  the AI Actions page.

**Resolved 2026-08-31 evening (the accountant's month-end ask)**: the LABOUR half of the
write clusters is no longer open. Sign-off (sign_off_labour_week / remove_labour_week_sign_off,
by-name wrappers over the sign-off handlers), the §6a coding run (run_xero_coding, by-name,
per-worker outcomes like approval's), reconciliation (set_xero_line_timesheet_cover,
add_labour_settlement_variance — both with CreatedByEmail stamps) and the effective-dated
mappings (set_site_xero_mapping / set_cost_code_xero_mapping, gate classes in
SettlementCommandGates.cs) are all declared, confirm-first where they write to Xero or post
money. Reads to match: view_settlement_month, view_worker_month (cross-project),
get_xero_mappings (AiLabourMonthEndTools.cs). find_by_reference now resolves project
references (JBB-2026-002 → kind "project").

**2026-09-03 (the accountant's "coding run must settle a worker who already has a bill")**: the
coding run's normal path is now to find and recode the worker's EXISTING bill (draft or
authorised, by cover or by contact + period) and re-point the cover itself; staging is the
exception. Two actions joined the cluster: preview_xero_coding (the dry run — read-shaped, no
confirmation, the list the run is confirmed against) and reset_xero_coding_outcome
(confirm-first, ResetByEmail stamp, reason mandatory) — by-name wrappers over
RunXeroCodingHandler(DryRun) and the new ResetXeroCodingOutcome endpoint
(POST labour/xero-coding/reset).

**2026-09-04 (the accountant's "the export must read like the page")**: the weekly cashflow's
COMPUTATION is now server-readable. get_weekly_cashflow_grid (AiWeeklyCashflowGridTool.cs,
WeeklyCashflowRoles) seeds the aged payables/receivables through WeeklyCashflowSeeding, applies
the plan's placements and exclusions with WeeklyCashflowMaths, and folds the result with
WeeklyCashflowExportBands — the same contracts code the page and its redesigned Excel export use
— into one line per supplier (a supplier group is one line), with only the cells that hold money,
net per week, and the closing balance for directors only (the cash-summary gate mirrored).
get_weekly_cashflow_plan stays the raw overlay. The page guide for /finance/weekly-cashflow
(FinancePageGuides) arrived with it — the page had shipped 2026-08-27 without one.

**2026-09-04 (Nigel: "the MCP server can do everything the website can")**: an AI tool asked
to send a thanks-only reply from the Control Centre went round the catalogue twice and reported,
correctly, that no reply-and-send action existed — SendMailboxEmail was on the skip list, and
the jpms-email-triage skill said "the connector never sends email". The skip note's reasons had
gone stale: POST mailbox/compose takes plain JSON when nothing is uploaded, and
SendMailboxEmailHandler was already registered as ICommandHandler<SendMailboxEmail,
ComposeOutcome>; the only missing piece was a gate class. Now declared as **send_mailbox_email**
(RequestsActions.Mailbox.cs; SendMailboxEmailAuthorisation mirrors the endpoint's
JpmsRoleSets.AllInternal check; SenderEmail stamped; confirm-first, because a sent email has no
undo) — the Reply box, Compose pane and Outbox in one: reply-all in the thread, forward, or a
brand-new email; markThreadHandled tags the thread JPMS/Replied; linkRecordType/linkRecordId
and alsoRaiseRequest file it in the same act; saveAsDraftOnly keeps the old draft-then-Outlook
path as a choice. Source=Upload attachments stay page-only (no bytes travel with a connector
call; the handler refuses them with a message). get_mailbox_message now returns
replyTo, mailboxAddress and a ready-made **replyAll** envelope
(contracts ReplyAllEnvelope — the composer's prefill rule, so a model replies to addresses it
has read, never ones it constructed). Doctrine caught up in the same delivery: the
/control-centre page guide rewritten for the connector's verbs (it still told the model to
use select_email / read_selected_email / stage_triage_* / open_modal reply_email — all removed
2026-08-27), jpms-email-triage step 7 and its "cannot do" section rewritten (docs +
seed script), and scripts/update-jpms-email-triage-skill-send-reply.sql moves the LIVE skill
on with a revision row, replacing only the stale passages.

**2026-09-04 (Nigel: "why isn't the August valuation report an option for tagging")**: the
Client pane's snapshot section lists only FROZEN statements, and a Draft claim has none until its
invoice is raised — so the month's correspondence had nowhere to file until the money moved.
The live claim is now a record of its own: **RecordType.ValuationClaim** (ValuationClaimLinkProvider,
tag JPMS/VAL-{project}-{claim number}, Client pathway, drift-check reach via
get_valuation_context — whose claims list already carried each ValuationClaimId). The connector
needed no new tool: file_email_to_record's type enum is generated from RecordType, so
ValuationClaim is accepted the moment the api deploys, and read_record_emails takes
recordType valuation_claim (both help texts say so, as does the /control-centre page guide and
get_valuation_context's description). On the site, the valuation page gained a Correspondence
section for the selected claim and every snapshot merges its claim's mail with its own, so the
statement carries the period's whole story and Confirm & roll over moves the tag on by itself.
Doctrine: jpms-email-triage step 4 and a new jpms-valuation-cycle "Correspondence" section (docs +
seed script); scripts/update-jpms-skills-valuation-claims.sql moves the two LIVE skills on with
revision rows.

**Still open, with reasons**: the cash forecast/statement COMPUTATION (the phasing runs
client-side over several stores; the inputs are now all readable — a server-side statement tool
is a real build, not a wrapper); profit summary / Xero site P&L and Xero transactions reads;
SetXeroAllocation and the registers/policies write clusters (inline gates — unlock on
demand as reads prove out); UpdateManualWorkOrder (server-derived flag); the audit-trail read
(left unmirrored on purpose for now — decide deliberately, it is the client-pathway wall);
drawings' ambiguous-revision queue; to-do activity trails; Dashboard aggregates.

## 8. Per-page ledger (condensed)

| Page | Writes | Reads | Skill |
|---|---|---|---|
| Control Centre | mirrored — send_mailbox_email 2026-09-04 (uploads page-only) | queue readable 2026-08-31 | email-triage |
| Document Triage | filing mirrored; discard/restore skipped | ⚠ queue unreadable | document-filing |
| Requests / Request detail | full parity (attachments skipped) | covered | chain doctrine (page guides) |
| Communications ×4 | send-only (skipped by design) | registers unreadable | — |
| Estimating / RFI dash / Sales / Nurture | read-only pages | leads + cross-project RFI listing missing | — |
| Valuation (+snapshots) | full parity (9 invoice writes) | ⚠ invoices + snapshots unreadable | valuation-cycle |
| Variations (+detail) | full parity; request accept/reject + WO-issue skipped | request queue + instruction links unreadable | variation-lifecycle |
| Work orders (+PO, allocation) | full parity; manual-order edit skipped | ⚠ ledger link slices unreadable; audit history unreadable | tender-award (PO step) |
| Bid packages (+detail) | full parity; invite SEND skipped | covered | tender-award |
| Tender enquiries (+detail) | full parity | listing missing | — |
| Cost codes / Rates | mirrored | ⚠ rate library unreadable; Xero comparison minor | — |
| Directory (+detail) / Clients / Architects | mirrored; portal invite skipped | ⚠ clients/architects/staff/compliance unreadable | full-record rule |
| Finance: forecast/aged/allocation/certificates/profit/weekly/Xero | forecast knobs mirrored; allocation + weekly-cashflow dark (skips + GAPs) | the whole finance read surface missing | xero-allocation, cash-forecast |
| Project financials (+recon audit) | full parity | ⚠ recon packages + audit trail unreadable | — |
| Inventory | GAP ×3 | register unreadable | — |
| Programme / Progress / Drawings / Building control / Defects / Calendar | mirrored (multipart uploads skipped) | ⚠ all but defects unreadable | document-filing, conventions |
| Labour ×4 | by-name actions mirrored; rest skipped; AdjustTimesheet GAP | ⚠ overview/roster/mappings unreadable | labour-rules |
| To-dos (+detail) / My Day | write tools + mirrored move/delete | activity trail thin | — |
| Settings / Setup | full parity (doc uploads skipped) | covered | — |
| Useful info / Policies / Registers | notes mirrored; policies/registers skipped | all unreadable | — |
| Admin (users/trades/system/AI pages) | mirrored; invite/reset GAP-adjacent | user directory unreadable | — |
| Portal (subcontractor) | skipped by design | out of audience | — |
| Audit / Agent activity | read-only | unreadable (decide if deliberate) | — |
