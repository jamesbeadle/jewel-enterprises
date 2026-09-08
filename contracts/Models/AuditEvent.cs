namespace Jewel.JPMS.Models;

// What happened, on the record of client-facing interactions. Scope decision 2026-07-22: the audit
// trail records CLIENT-FACING events only — triage decisions on client-pathway threads, client
// records created or linked from email, drafted client correspondence, wall refusals, snapshots.
// Subcontractor/internal event values are reserved (declared, never written yet) so widening the
// scope later is a filter change, not a schema change.
//
// First widening, 2026-07-28: CostCentreRecoded — the finance reconciliation trail. Moving a
// valuation report line between cost centres recodes where money sits in the cost-centre master,
// so each move is recorded (who, when, which line, from → to, the value carried). Not a
// client-facing event: Pathway is "", and the register's client filters simply never match it.
//
// Second widening, 2026-08-04: the link events (EmailTriaged / RecordLinked) are now written for
// EVERY pathway, not just Client — the record activity indicator derives each record's
// recent-communications score from these rows (ListRecordActivity), so links to bid packages,
// work orders and pathway-neutral records must leave a trace too. Pathway carries the thread's
// side ("Subcontractor", "Internal", or "" for neutral links); client-facing views keep filtering
// on it, which is the filter-change-not-schema-change this enum's reserved values planned for.
public enum AuditEventType
{
    EmailTriaged = 0,           // a thread was filed under a pathway via its first link/create
    RecordLinked = 1,           // an email was linked to an existing client record
    RecordCreatedFromEmail = 2, // a client record was created from an email at triage
    TagRemoved = 3,             // a record tag was removed from a client-pathway email
    Discarded = 4,              // a client-pathway thread was discarded
    Restored = 5,               // a discarded thread returned to the queue
    WallRejected = 6,           // an action that would cross the client wall was refused
    DraftCreated = 7,           // the portal drafted client correspondence (request doc / reply)
    SnapshotTaken = 8,          // a valuation report snapshot was frozen (invoice raise)
    BackfillStamped = 9,        // the backfill stamped a pathway onto an existing thread
    // Reserved for the wider scope — declared so persisted ints never shift:
    CrossPathwayOverride = 10,  // a deliberate Subcontractor↔Internal dual filing
    ThreadSwept = 11,           // the queue sweep propagated tags to a late reply
    // Finance reconciliation (written since 2026-07-28):
    CostCentreRecoded = 12,     // a valuation report line moved to a different cost centre
    // Outbound mail (written since the triage compose work, 2026-08-04). EmailSent is always
    // material whatever the pathway — the portal put words in front of a correspondent — so it is
    // written for every send, with Pathway carrying the thread's side ("" when none was chosen).
    EmailSent = 13,             // the portal sent an email from the projects mailbox
    EmailSendFailed = 14,       // a send attempt failed after the draft was staged (draft kept)
    // Project lifecycle (written since the settings danger-zone work, 2026-08-05). The project's
    // own rows are gone by the time this is written, so RecordReference carries the deleted
    // project's reference and the detail its name — the event is the surviving record.
    ProjectDeleted = 15,        // a project and its records were permanently deleted from settings
    // Document Control (written since the attachment-triage split, 2026-08-12):
    SentToDocumentControl = 16, // an email attachment was copied into the Document Control queue
    DocumentFiled = 17,         // a Document Control item was filed to its destination record
    DocumentDiscarded = 18,     // a Document Control item was discarded (restorable, never deleted)
    // Procurement guardrail (written since 2026-08-17): raising a work order whose cost centre
    // has no priced valuation report line commits cost with no matching sale — the raise dialog
    // warns, and this row records the user's deliberate decision to raise anyway. Not a
    // client-facing event: Pathway is "", like CostCentreRecoded.
    WorkOrderSaleWarningOverridden = 19, // a work order was raised against uncovered cost centre(s)
    // Draft deletion (written since 2026-08-24): a draft work order removed outright — raised in
    // error or duplicated — leaves no Rejected row behind, so this event is the surviving record
    // (mirroring ProjectDeleted). Not a client-facing event: Pathway is "", like CostCentreRecoded.
    DraftWorkOrderDeleted = 20, // a draft work order was permanently deleted before any decision
    // Tender enquiries (written since 2026-08-25): an architect's invitation to tender logged from
    // its email (or by hand), and every move of its status — accepted, PQQ submitted, shortlisted,
    // tender submitted, won/lost. Client-facing: Pathway is "Client", like the request events.
    TenderEnquiryLogged = 21,        // a tender enquiry was logged (its Lead project created when new)
    TenderEnquiryStatusChanged = 22, // a tender enquiry moved to a new status
    // AI connector writes (since 2026-08-27): actions taken from a team member's own AI tool over
    // MCP, under their portal identity. Not pathway-specific: Pathway is "".
    NotePosted = 23,                 // a message was posted on a request's conversation
    TodoCreated = 24,                // a to-do item was added
    TodoCompleted = 25,              // a to-do item was completed or reopened
    // Labour budget override (written since 2026-08-29): approving a timesheet past the cost
    // code's budget hard-block is allowed only for the MD/FD/Admin, with a typed reason — this
    // row records each overridden timesheet (who, the block it overrode, the reason given).
    // Not a client-facing event: Pathway is "", like CostCentreRecoded.
    LabourBudgetOverridden = 26,     // a timesheet was approved past the budget hard-block
    // Work-order lifecycle (written since 2026-08-29): the decisions that move an order between
    // states, so a WO's timeline reads complete from its record-scoped history. Approval also
    // stamps AwardedAt/AwardedByEmail on the order itself; reject and cancel stamp nothing on the
    // entity, so these rows are the only dated record of those decisions. Not client-facing:
    // Pathway is "", like CostCentreRecoded.
    WorkOrderApproved = 27,          // a draft work order was approved — number minted, order released
    WorkOrderRejected = 28,          // a draft work order was rejected — terminal, never issued
    WorkOrderCancelled = 29,         // a released work order was cancelled — voided, keeps its number
    // Mailbox draft withdrawal (written since 2026-08-29): an unsent draft staged in the shared
    // mailbox was deleted before sending — the inverse of DraftCreated. The draft itself is gone
    // (recoverable from Outlook's Deleted Items for a while), so this row is the surviving record
    // of what was staged and withdrawn, mirroring DraftWorkOrderDeleted. Not pathway-specific:
    // Pathway is "".
    MailboxDraftDeleted = 30,        // an unsent mailbox draft was deleted before sending
    // Cost code budget writes (written since 2026-08-29): the Financials tab's allocated/spent
    // figures for a cost code moved — from the portal's Financials tab or the connector's
    // set_cost_code_budget (confirm-first). The row records before → after, so a raised
    // allocation that quietly papers over an overspend is a matter of record, mirroring
    // LabourBudgetOverridden. Not a client-facing event: Pathway is "", like CostCentreRecoded.
    CostCodeBudgetSet = 31,          // a cost code's budget row was created or changed
    // Worker ↔ directory linking and chase dismissals (written since 2026-08-31): settlement is
    // gated on the worker's counterparty, so a link being created — by the Xero import's
    // auto-match, the reconcile sweep, the allocation page's inline fix, or the connector — is a
    // money-routing decision worth a dated row; a chase dismissal is the FD/PM deciding a
    // worker-day needs no timesheet, and the reason given is the point. Not client-facing:
    // Pathway is "", like CostCentreRecoded.
    WorkerLinkedToDirectory = 32,    // a worker was linked to (or unlinked from) a directory company / flagged sole trader
    LabourChaseDayDismissed = 33,    // a chase-list day was dismissed with a reason
    // Drawing data extraction (written since 2026-08-31): a drawing revision's structured data —
    // Bluebeam Studio markups plus the PDF's own text layer — was extracted into the portal's data
    // view. Written by the worker after the pipeline succeeds; the register's Metadata badge is the
    // per-revision status, this row is the when/who record. Not client-facing: Pathway is "".
    DrawingDataExtracted = 34,       // a drawing revision's markups + text layer were extracted
    // Archive extraction in Document Triage (written since 2026-08-31): a zip queue item was split
    // into one queue item per contained file so each can be filed individually. The original
    // resolves as ArchiveExtracted; this row records who split it and how many files came out.
    // Not client-facing: Pathway is "".
    DocumentArchiveExtracted = 35,   // a Document Triage zip was split into per-file queue items
    // Bluebeam connection (written since 2026-08-31): an admin connected (or reconnected) the
    // portal's shared Bluebeam Studio account from Admin → Integrations. One connection serves the
    // whole portal, so the record of who signed it in matters. Not client-facing: Pathway is "".
    BluebeamConnected = 36,          // the shared Bluebeam Studio account was connected
    // Variation conversation (written since 2026-08-31): a message was posted on a variation
    // order's in-app conversation — the VO twin of NotePosted above. Posts from the client
    // portal carry Pathway "Client"; internal posts leave Pathway "".
    VariationNotePosted = 37,        // a message was posted on a variation order's conversation
    // KPI emails (written since 2026-09-03): an administrator marked an email as a KPI against a
    // portal user, or took the mark off. The register itself is administrators-only, so these
    // rows carry the KPI reference and nothing else — no user, no subject, no message id — and
    // the audit endpoint refuses a non-administrator's read narrowed to them. Pathway is "".
    KpiEmailMarked = 38,             // an email was marked as a KPI (KPI-####)
    KpiEmailRemoved = 39,            // a KPI mark was taken off an email
    // Labour corrections (written since 2026-09-07, the accountant's ask): an approved day on the
    // wrong project is a normal month-end event, and until now the only remedies were a
    // settlement variance or a request to the developer. Both corrections are MD/FD/Admin only
    // with a mandatory reason, and these rows are the record — LabourApprovalReversed carries
    // the approval snapshot the day lost (rate, £, who approved, when), LabourDayMoved the
    // projects either side; a move of an approved day past the destination's budget also writes
    // LabourBudgetOverridden, exactly as an over-budget approval does. Not client-facing:
    // Pathway is "", like CostCentreRecoded.
    LabourApprovalReversed = 40,     // an approved timesheet was put back to Submitted, its posted cost withdrawn
    LabourDayMoved = 41,             // a timesheet was moved to another project (status, hours and snapshot kept)
    // Directory ↔ Xero linking (written since 2026-09-08, the accountant's ask): an existing
    // directory record was linked to a Xero contact — or a link was taken off — without importing
    // a second record. The link decides which Xero supplier a company's bills reconcile against,
    // so who made it is a matter of record, exactly like WorkerLinkedToDirectory. Not
    // client-facing: Pathway is "".
    DirectoryRecordXeroLinkChanged = 42, // a directory record was linked to (or unlinked from) a Xero contact
    // Work Order bills (written since 2026-09-08, the accountant's ask): a supplier bill matched
    // to an open work order was approved from the allocation page's Work Order bills tab — every
    // line allocated from the order, linked to it, and the bill approved in Xero — or that
    // approval was undone. The detail names the bill, the order, the rule that matched and the
    // value. Not client-facing: Pathway is "", like CostCentreRecoded.
    WorkOrderBillApproved = 43,      // a Work Order bill was approved: allocated, linked, confirmed to Xero
    WorkOrderBillApprovalUndone = 44 // a Work Order bill approval was reversed (allocation, links, tracking)
}

// One append-only audit event. WebLink (when present) opens the email or draft in Outlook on the
// web — the audit register doubles as the index for finding portal-drafted mail in Outlook.
public sealed record AuditEvent(
    string AuditEventId,
    DateTimeOffset OccurredAt,
    string ActorEmail,
    AuditEventType EventType,
    // Short pathway label ("Client", "Subcontractor", "Internal") or "" when not pathway-specific.
    string Pathway,
    string? ProjectId,
    RecordType? RecordType,
    string? RecordId,
    // Denormalised display reference (e.g. "RFI-012", "REQ-0113", "VI-0004") so rows render without joins.
    string RecordReference,
    string? ConversationId,
    string? EmailMessageId,
    string? InternetMessageId,
    string? WebLink,
    // One human sentence: "Linked to RFI-012", "RFI-012 document drafted to …", etc.
    string Detail);

// One page of the audit register, newest first. Cursor is a plain offset.
public sealed record AuditEventsPage(
    IReadOnlyList<AuditEvent> Items,
    string? NextCursor,
    int Total);
