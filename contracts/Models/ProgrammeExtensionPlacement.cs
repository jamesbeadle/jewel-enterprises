namespace Jewel.JPMS.Models;

// An Extension of Time as the programme sees it: the EOT request, drawn from the completion it
// extends (the baselined completion, else the current one) out by the days claimed, with the
// days granted inside it. Null bars mean the EOT carries no day count yet, or there is no
// programme to hang it on — the row still lists, it just draws nothing.
public sealed record ProgrammeExtensionRow(
    Request Eot,
    DateTimeOffset? From,
    DateTimeOffset? ClaimedTo,
    DateTimeOffset? GrantedTo)
{
    public int DaysClaimed => Eot.EotDaysClaimed ?? 0;
    public int DaysGranted => Eot.EotDaysGranted ?? 0;
    public bool HasBar => From is not null && ClaimedTo is not null;
}

// Pure placement of a project's Extensions of Time against its programme's completion. Shared by
// the Gantt and the Programme Agent. No I/O, no clock.
public static class ProgrammeExtensionPlacement
{
    public static IReadOnlyList<ProgrammeExtensionRow> Place(
        IReadOnlyList<Request> requests,
        ProgrammeMovement movement)
    {
        var from = movement.BaselineCompletion ?? movement.CurrentCompletion;
        return requests
            .Where(request => request.Kind == RequestType.ExtensionOfTime)
            .OrderBy(request => request.IssuedAt ?? request.RaisedAt)
            .Select(eot => Row(eot, from))
            .ToList()
            .AsReadOnly();
    }

    private static ProgrammeExtensionRow Row(Request eot, DateTimeOffset? from)
    {
        if (from is null || eot.EotDaysClaimed is null) return new ProgrammeExtensionRow(eot, from, null, null);
        var claimedTo = from.Value.AddDays(eot.EotDaysClaimed.Value);
        var grantedTo = eot.EotDaysGranted is { } granted ? from.Value.AddDays(granted) : (DateTimeOffset?)null;
        return new ProgrammeExtensionRow(eot, from, claimedTo, grantedTo);
    }
}
