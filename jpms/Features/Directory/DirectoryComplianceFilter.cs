using Jewel.JPMS.Features.Subcontractors;

namespace Jewel.JPMS.Features.Directory;

/// <summary>The Directory's compliance chips (2026-09-09, the accountant's ask): narrow the company
/// list to everyone whose standing — the worst status among their current documents, Missing when
/// they hold none — is Expired, Expiring soon, Current or Missing. The chips carry counts only once
/// both the directory and the whole-company compliance read have landed.</summary>
public static class DirectoryComplianceFilter
{
    public const string All = "all";

    /// <summary>The order a compliance list reads in (2026-09-10, the accountant's ask): what has
    /// lapsed, what is about to, what is in date — and only then the long tail of companies with
    /// nothing on file. Missing is the noise floor (hundreds of rows), not the headline; putting it
    /// last keeps the handful of live documents visible at the top of the All view. The chips sit
    /// in the same order as the rows so the two read as one.</summary>
    public static readonly ComplianceStatus[] ReadOrder =
    {
        ComplianceStatus.Expired, ComplianceStatus.ExpiringSoon, ComplianceStatus.Current, ComplianceStatus.Missing
    };

    public static int RankOf(ComplianceStatus status) => Array.IndexOf(ReadOrder, status);

    public static IReadOnlyList<TabItem> CompanyChips(
        IReadOnlyList<Subcontractor> companies, ComplianceOverviewReadModel compliance, bool isLoaded) =>
        Chips(status => isLoaded ? companies.Count(company => StandingOf(company, compliance) == status) : null);

    /// <summary>All, then one chip per standing in read order; countFor answers null until the data
    /// has landed so no chip ever shows a zero that becomes real a second later.</summary>
    public static IReadOnlyList<TabItem> Chips(Func<ComplianceStatus, int?> countFor)
    {
        var chips = new List<TabItem> { new(All, "All") };
        foreach (var status in ReadOrder)
            chips.Add(new TabItem(KeyFor(status), status.DisplayName(), Count: countFor(status), Title: TitleFor(status)));
        return chips;
    }

    public static bool Passes(Subcontractor company, string filter, ComplianceOverviewReadModel compliance) =>
        Passes(StandingOf(company, compliance), filter);

    public static bool Passes(ComplianceStatus status, string filter) => filter == All || KeyFor(status) == filter;

    private static ComplianceStatus StandingOf(Subcontractor company, ComplianceOverviewReadModel compliance) =>
        compliance.WorstStatusFor(company.SubcontractorId);

    private static string KeyFor(ComplianceStatus status) => status.ToString();

    private static string TitleFor(ComplianceStatus status) => status switch
    {
        ComplianceStatus.Expired      => "A current document has passed its expiry date",
        ComplianceStatus.ExpiringSoon => "A current document expires within 30 days",
        ComplianceStatus.Missing      => "No compliance documents on file",
        _                             => "Every document on file is in date"
    };
}
