namespace Jewel.JPMS.Services;

/// <summary>
/// The screen's one loading mark, cascaded down the render tree so "one jewel at a time" holds by
/// construction rather than by every page remembering it.
///
/// A <c>LoadGate</c> that is showing the mark cascades a claimed scope; every gate inside it — a
/// panel, a table, a list two components down — sees the claim and stays silent, holding its space
/// without adding a second spinner to the same wait. When the outer gate lifts, the claim lifts
/// with it and an inner gate refreshing on its own shows its own mark again.
///
/// Siblings cannot see each other, so the rule they answer to is the call site's: a screen loading
/// for the first time gates ONCE, around the region the reader is waiting for.
/// </summary>
public sealed class LoadScope
{
    /// <summary>Nothing above is loading — a gate here shows its own mark.</summary>
    public static readonly LoadScope Open = new(false);

    /// <summary>An ancestor is already showing the mark for this wait.</summary>
    public static readonly LoadScope Claimed = new(true);

    private LoadScope(bool isClaimed) => IsClaimed = isClaimed;

    public bool IsClaimed { get; }

    public static LoadScope For(bool isClaimed) => isClaimed ? Claimed : Open;
}
