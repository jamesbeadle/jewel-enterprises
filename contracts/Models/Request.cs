namespace Jewel.JPMS.Models;

// The "RF" family plus the JCT time/notice instruments they trigger.
// Explicit integer values are pinned so existing stored rows keep their meaning:
//   legacy Submittal(1) -> Rfa, legacy Variation(2) -> Rfc, NoticeOfDelay stays 3.
public enum RequestType
{
    Rfi = 0,            // Request for Information
    Rfa = 1,            // Request for Approval (sample / submittal)
    Rfc = 2,            // Request for Change / Comment
    NoticeOfDelay = 3,  // NOD (JCT ICD 2024 cl. 2.19)
    Rfq = 4,            // Request for Quotation
    Rfp = 5,            // Request for Proposal
    ExtensionOfTime = 6,// EOT (JCT ICD 2024 cl. 2.19/2.20)
    General = 7         // Default state: project-tagged & cost centre known, not yet promoted
}

// The request status model is deliberately small — it answers "whose court is the ball in?":
//   NeedsAction    — the ball is with us: issue the document, act on the response, re-file the record.
//   Open           — with the correspondent (architect); we are awaiting their response.
//   NeedsVariation — the response requires a variation order quote to be raised.
//   Closed         — done.
// Explicit integer values are pinned because statuses persist as ints. The 0/1 values deliberately
// keep their stored rows' new meaning: rows saved as legacy Open(0) now read NeedsAction, and rows
// saved as legacy AwaitingResponse(1) now read Open. Legacy Approved(2)/Rejected(3)/Responded(5)
// were retired and their rows migrated to Closed(4) (ConsolidateRequestStatuses migration) — never
// reuse those values.
public enum RequestStatus
{
    NeedsAction = 0,
    Open = 1,
    Closed = 4,
    NeedsVariation = 6
}

public sealed record Request(
    string RequestId,
    string ProjectId,
    RequestType Kind,
    string Reference,
    string Title,
    string Description,
    RequestStatus Status,
    decimal? Value,
    string RaisedByEmail,
    DateTimeOffset RaisedAt,          // system created-on stamp — shown only as the secondary "Created" fact on the detail page; IssuedAt is the official date lists lead with
    DateTimeOffset? RespondedAt,
    string? ResponseText = null,
    string? RespondedByEmail = null,
    bool ImpliesVariation = false,
    string? DrawingRef = null,          // drawing / detail reference the request concerns
    DateTimeOffset? ResponseDue = null, // contractual response-due date
    string? RelatedDrawingSpec = null,  // related drawing / spec issued with the response
    string? InternalNotes = null,       // notes kept internal to Jewel
    string? ClientNotes = null,         // notes shared with client / external parties
    int Number = 0,                     // sequential request number; rendered as REQ-0001
    bool HasRfq = false,                // an RFI that has spawned an RFQ (unlocks VOQ creation)
    PartyKind PartyKind = PartyKind.Client, // what kind of party PartyId points at (client or architect)
    string? PartyId = null,             // the party corresponded with — recipient source on RFI promotion
    string? OnBehalfOfClientId = null,  // when the party is an architect: the client they act for (optional)
    string? BasisOfQueries = null,          // official document: what the queries arise from (emails, drawings, site observations)
    string? ResponseActionRequired = null,  // official document: the written confirmation / instruction being asked for
    string? ImpactIfLate = null,            // official document: consequence if no response by the required-by date
    IReadOnlyList<RequestItem>? Items = null, // official document: the itemised queries, ordered by Position
    string? RelatedNodRequestId = null,     // EOT only: the Notice of Delay this EOT arises from (optional)
    string? MergedIntoRequestId = null,     // set when this General request was merged into another (the survivor's id)
    DateTimeOffset? ClosedAt = null,        // when the request was closed — user-chosen (today or prior), cleared on reopen
    DateTimeOffset? IssuedAt = null,        // the one visible request date — stamped on creation (today / backfill date), user-editable thereafter
    bool CriticalPath = false,              // Critical Path tag — the RFI is programme-related; shows in the Programme tab's "Critical Path RFIs" view
    bool CriticalPathNudgeDismissed = false, // "No" was clicked on the two-week critical-path nudge — the banner never re-asks on this RFI
    int? EotDaysClaimed = null,             // EOT only: the extension sought, in calendar days — what the Programme tab draws from completion
    int? EotDaysGranted = null)             // EOT only: the extension awarded, in calendar days — null until the architect answers
{
    // Human-readable request number / mailbox folder name (e.g. "REQ-0001"). Empty until assigned.
    public string DisplayNumber => Number > 0 ? $"REQ-{Number:0000}" : "";

    // The itemised queries, never null (Items is nullable so old payloads deserialize cleanly).
    public IReadOnlyList<RequestItem> ItemList => Items ?? Array.Empty<RequestItem>();

    // Days a request has been outstanding since it was issued. While open the clock ticks daily —
    // a recorded response (RespondedAt) does NOT stop it, since a responded-but-open request is
    // still outstanding work. At Closed the count FREEZES at the close date rather than vanishing:
    // "how long was it out?" stays a fact of the record after the fact. ClosedAt is the user-chosen
    // close date; RespondedAt is the fallback stamp for closed rows predating it, and a closed row
    // with neither shows a dash. IssuedAt is the one visible date (RaisedAt is only the internal
    // created-on stamp, kept as a fallback for rows predating the IssuedAt backfill).
    public int? DaysOutstanding =>
        Status is RequestStatus.Closed
            ? (ClosedAt ?? RespondedAt) is { } closed
                ? Math.Max(0, (int)(closed.Date - (IssuedAt ?? RaisedAt).Date).TotalDays)
                : null
            : Math.Max(0, (int)(DateTimeOffset.UtcNow.Date - (IssuedAt ?? RaisedAt).Date).TotalDays);

    // Overdue is a question about THEM, not us: the correspondent has not come back by the date we
    // asked them to. So it is measured against the request's own Response due date — the contractual
    // date carried on the issued document — and it stops the moment a response is recorded, even
    // though the request itself stays open while we act on that response. Requests with no due date
    // fall back to the default response window from the issue date, which is the rule every request
    // had before the due date became a field. Deliberately shared with the PDF's own overdue flag
    // (RequestDocumentModel) so the register, the dashboards and the issued document can never
    // disagree about what "overdue" means.
    public bool IsOverdue =>
        Status is not RequestStatus.Closed
        && RequestDates.IsOverdue(IssuedAt ?? RaisedAt, ResponseDue, RespondedAt);
}

/// <summary>
/// The one shared reading of a request's dates. Kept out of the record itself so the api-side
/// document model applies the identical rule instead of a second, drifting copy of it.
/// </summary>
public static class RequestDates
{
    /// <summary>Days allowed for a response when a request carries no explicit due date.</summary>
    public const int DefaultResponseWindowDays = 7;

    /// <summary>
    /// True when a response was due and has not arrived. <paramref name="issuedAt"/> is where the
    /// clock starts (the request's issue date); <paramref name="responseDue"/> is the date it was
    /// contractually asked for, and when absent the default window from issue applies instead.
    /// A recorded <paramref name="respondedAt"/> always stops the clock.
    /// </summary>
    public static bool IsOverdue(DateTimeOffset issuedAt, DateTimeOffset? responseDue, DateTimeOffset? respondedAt)
    {
        if (respondedAt is not null) return false;
        var due = responseDue?.Date ?? issuedAt.Date.AddDays(DefaultResponseWindowDays);
        return DateTimeOffset.UtcNow.Date > due;
    }
}

/// <summary>
/// One itemised query on a request's official document — a numbered row of the RFI sheet
/// (Item / Drawing Ref / Member-Area / Query / Response). The rendered item number is the 1-based
/// <paramref name="Position"/>.
/// </summary>
public sealed record RequestItem(
    string RequestItemId,
    string RequestId,
    int Position,
    string DrawingRef,
    string MemberArea,
    string Query,
    string? Response = null);

public static class RequestStatusExtensions
{
    // The one shared status wording — every pill, picker, export and document label goes through
    // here so the register, detail page, dashboards and PDFs can never drift apart.
    public static string DisplayName(this RequestStatus status) => status switch
    {
        RequestStatus.NeedsAction    => "Needs action",
        // Named for both halves of what it means: the request is open, and the ball is with the
        // correspondent. Reading "Open" alone left people unsure whether it meant "still ours".
        RequestStatus.Open           => "Open / Awaiting response",
        RequestStatus.NeedsVariation => "Needs variation",
        RequestStatus.Closed         => "Closed",
        _ => status.ToString()
    };

    // The tooltip/hint wording that accompanies the label wherever a surface shows one.
    public static string? Hint(this RequestStatus status) => status switch
    {
        RequestStatus.NeedsAction    => "The ball is with us — something needs doing (issue the document, act on the response).",
        RequestStatus.Open           => "With the correspondent — awaiting their response.",
        RequestStatus.NeedsVariation => "The response requires a variation order quote to be raised.",
        _ => null
    };
}

public static class RequestTypeExtensions
{
    // EMAIL POLICY — which request kinds may ever produce an email draft of their official
    // document. Only the official instruments are emailed: the RFI and the JCT time notices
    // (NOD / EOT). A General container, RFA, RFC, RFQ and RFP are NEVER emailed: an RFQ reaches
    // subcontractors as a bid-package invite (its own draft flow), and VOQ / VO financial
    // documents are not request emails. Server handlers and UI both consult this single gate.
    public static bool IsEmailable(this RequestType kind) =>
        kind is RequestType.Rfi or RequestType.NoticeOfDelay or RequestType.ExtensionOfTime;

    // RAISE POLICY — the status a newly raised request starts at, which is only ever a question of
    // whose court the ball is in the moment the record exists. Raising an official instrument (an
    // RFI, a JCT notice, an RFA/RFC/RFQ/RFP) is us asking someone else something: the ball is with
    // the correspondent from the off, so it starts Open / Awaiting response. Needs action is
    // reserved for what is genuinely ours to do — a General container (a mailbox-raised email
    // sitting in triage, or a holding record we have not asked anything of anyone with yet), a
    // recorded response waiting to be acted on, and a reopened request.
    // The raise handler and the Raise request dialog both read this, so the default can never
    // differ between what the dialog shows and what the server stores.
    public static RequestStatus DefaultStatusOnRaise(this RequestType kind) =>
        kind is RequestType.General ? RequestStatus.NeedsAction : RequestStatus.Open;

    public static string DisplayName(this RequestType kind) => kind switch
    {
        RequestType.Rfi             => "RFI",
        RequestType.Rfq             => "RFQ",
        RequestType.Rfp             => "RFP",
        RequestType.Rfc             => "RFC",
        RequestType.Rfa             => "RFA",
        RequestType.NoticeOfDelay   => "NOD",
        RequestType.ExtensionOfTime => "EOT",
        RequestType.General         => "General",
        _ => kind.ToString()
    };

    public static string LongName(this RequestType kind) => kind switch
    {
        RequestType.Rfi             => "Request for Information",
        RequestType.Rfq             => "Request for Quotation",
        RequestType.Rfp             => "Request for Proposal",
        RequestType.Rfc             => "Request for Change",
        RequestType.Rfa             => "Request for Approval",
        RequestType.NoticeOfDelay   => "Notice of Delay",
        RequestType.ExtensionOfTime => "Extension of Time",
        RequestType.General         => "General Request",
        _ => kind.ToString()
    };
}
