
namespace Jewel.JPMS.Features.Projects;

// ONE reading of a project's next expected valuation date — overdue, due within the week, later,
// or not set — shared by the dashboard tile, the Upcoming valuations panel and the portfolio
// table, so "3 overdue" on the tile is the same three rows everywhere the reader lands.
// A completed project has nothing left to value and is never overdue.
public static class ValuationDue
{
    public enum Status { Overdue, DueSoon, Later, NotSet }

    private const int DueSoonWindowDays = 7;

    public static Status Of(Project project) =>
        project.Stage == ProjectStage.Completed ? Status.NotSet : Of(project.NextExpectedValuationDate);

    public static Status Of(DateTimeOffset? date)
    {
        if (date is null) return Status.NotSet;
        var today = DateTime.Today;
        if (date.Value.Date < today) return Status.Overdue;
        if (date.Value.Date <= today.AddDays(DueSoonWindowDays)) return Status.DueSoon;
        return Status.Later;
    }

    public static bool IsOverdue(Project project) => Of(project) == Status.Overdue;

    // The portfolio page filtered to the overdue rows — where the dashboard tile lands.
    public const string OverdueFilterQuery = "valuations=overdue";
    public const string OverdueFilterRoute = "/projects?" + OverdueFilterQuery;

    // Where a valuation is chased: the project's live Valuation Report, where the claim is raised.
    // A list reached BECAUSE valuations are due (the overdue-filtered portfolio, the dashboard's
    // Upcoming valuations panel) lands its rows here, never on the role's default project tab —
    // the FD clicking an overdue project wants the claim, not the RFI register (2026-09-10).
    public static string ProjectRoute(string projectId) => $"/projects/{projectId}/valuation";
}
