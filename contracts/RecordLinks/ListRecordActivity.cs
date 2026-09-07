using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.RecordLinks;

// A project's per-record communication activity, derived from the audit trail's link events
// (EmailTriaged / RecordLinked / RecordCreatedFromEmail) inside the score window. One request per
// project page view feeds every activity badge on it — the mailbox itself is never read for this:
// linked emails live in Outlook (the tag is the association), but the moment of linking leaves a
// timestamped audit row, and activity is a read of that index. Records with no events inside the
// window simply do not appear in the result — absence is the "quiet" answer. That is also why it
// is best-effort: a badge that fails to load looks exactly like a quiet record, so the transport
// retries but never raises a banner over a page that is otherwise complete.
public sealed record ListRecordActivity(string ProjectId)
    : IQuery<IReadOnlyList<RecordActivitySummary>>, IBestEffortQuery;
