namespace Jewel.JPMS.Contracts.Cqrs;

public interface IQuery<TResult>
{
}

/// <summary>
/// A query whose failure is not news to the user. Ordinarily every failed read is reported, because
/// the page it feeds would otherwise sit there looking empty with nothing to explain why. A few
/// queries feed decoration instead — an activity badge, a count in a tab — where the contract itself
/// says absence is the quiet answer: the page is complete without them, and a red banner over a
/// perfectly good page is worse than a missing dot. Marking the query, rather than the call site,
/// keeps that fact with the contract that makes it true; the transport still retries the same way,
/// it just doesn't tell the user when the last attempt fails.
/// </summary>
public interface IBestEffortQuery
{
}
