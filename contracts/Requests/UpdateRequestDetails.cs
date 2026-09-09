using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

public sealed record UpdateRequestDetails(
    string RequestId,
    string Reference,
    string Title,
    string Description,
    RequestStatus Status,
    decimal? Value,
    string? ResponseText,
    string? RespondedByEmail,
    bool ImpliesVariation,
    string? DrawingRef = null,
    DateTimeOffset? ResponseDue = null,
    string? RelatedDrawingSpec = null,
    string? InternalNotes = null,
    string? ClientNotes = null,
    DateTimeOffset? RaisedAt = null,
    // EOT only: the Notice of Delay this EOT arises from. Optional — an EOT can stand alone.
    string? RelatedNodRequestId = null,
    // The date the request closed (today or earlier). Only meaningful when Status is Closed: a
    // value corrects the recorded close date; null means "not supplied" and keeps the existing one.
    DateTimeOffset? ClosedAt = null,
    // When the official document was issued to the correspondent. User-set and user-updated; a
    // value (over)writes the recorded issue date, null means "not supplied" and keeps the existing
    // one (most edit surfaces don't carry it).
    DateTimeOffset? IssuedAt = null,
    // Critical Path tag — the RFI is programme-related (its answer gates critical-path work).
    // A value (over)writes the tag; null means "not supplied" and keeps the existing one (most
    // edit surfaces don't carry it, so status changes and the like never shed the tag).
    bool? CriticalPath = null,
    // "No" on the two-week critical-path nudge. Same convention again: a value (over)writes the
    // flag, null means "not supplied" — only the nudge banner itself ever sends one, so ordinary
    // edits never resurrect (or re-dismiss) the prompt.
    bool? CriticalPathNudgeDismissed = null,
    // EOT only: the days sought / awarded. Same convention — a value (over)writes, null means "not
    // supplied" and keeps what is recorded, so the ordinary edit surfaces never shed them.
    int? EotDaysClaimed = null,
    int? EotDaysGranted = null) : ICommand<Request>;
