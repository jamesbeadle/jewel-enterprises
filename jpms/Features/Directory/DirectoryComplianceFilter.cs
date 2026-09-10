using Jewel.JPMS.Features.Subcontractors;

namespace Jewel.JPMS.Features.Directory;

/// <summary>The Directory's compliance chips (2026-09-09, the accountant's ask): narrow the company
/// list to everyone whose standing — the worst status among their current documents, Missing when
/// they hold none — is Expired, Expiring soon, Current or Missing. The chips sit in
/// <see cref="ComplianceStatusExtensions.ReadingOrder"/>, the same order the register's rows read
/// in, and carry counts only once both the directory and the whole-company compliance read have
/// landed.</summary>
public static class DirectoryComplianceFilter
{
    public const string All = "all";

    /// <summary>The one chip that is not a standing (2026-09-10, the accountant's third ask): the
    /// companies whose recorded public liability cover is under the £5m Jewel's insurer requires
    /// on big jobs. A filter, not a status — a smaller policy is a fact for the person placing the
    /// work, never an expired document — so it sits after the standings and never changes a pill.
    /// An unrecorded figure is not "below": it is a gap to fill.</summary>
    public const string BelowPublicLiabilityRequirement = "BelowPublicLiabilityRequirement";

    public const string BelowPublicLiabilityRequirementLabel = "Below £5m PL";

    public const string BelowPublicLiabilityRequirementTitle =
        "Public liability cover recorded under the £5m Jewel's insurer requires of subcontractors on big jobs";

    public static IReadOnlyList<TabItem> CompanyChips(
        IReadOnlyList<Subcontractor> companies, ComplianceOverviewReadModel compliance, bool isLoaded) =>
        Chips(status => isLoaded ? companies.Count(company => StandingOf(company, compliance) == status) : null,
            () => isLoaded ? companies.Count(company => compliance.IsBelowPublicLiabilityRequirementFor(company.SubcontractorId)) : null);

    /// <summary>All, then one chip per standing in reading order, then Below £5m PL; each countFor
    /// answers null until the data has landed so no chip ever shows a zero that becomes real a
    /// second later.</summary>
    public static IReadOnlyList<TabItem> Chips(Func<ComplianceStatus, int?> countFor, Func<int?> belowRequirementCount)
    {
        var chips = new List<TabItem> { new(All, "All") };
        foreach (var status in ComplianceStatusExtensions.ReadingOrder)
            chips.Add(new TabItem(KeyFor(status), status.DisplayName(), Count: countFor(status), Title: TitleFor(status)));
        chips.Add(new TabItem(BelowPublicLiabilityRequirement, BelowPublicLiabilityRequirementLabel,
            Count: belowRequirementCount(), Title: BelowPublicLiabilityRequirementTitle));
        return chips;
    }

    public static bool Passes(Subcontractor company, string filter, ComplianceOverviewReadModel compliance) =>
        filter == BelowPublicLiabilityRequirement
            ? compliance.IsBelowPublicLiabilityRequirementFor(company.SubcontractorId)
            : Passes(StandingOf(company, compliance), filter);

    /// <summary>A standing against a standing chip. The Below £5m chip is answered by the caller
    /// from the row's figure (<see cref="Passes(ComplianceStatus, bool, string)"/>).</summary>
    public static bool Passes(ComplianceStatus status, string filter) => filter == All || KeyFor(status) == filter;

    public static bool Passes(ComplianceStatus status, bool isBelowPublicLiabilityRequirement, string filter) =>
        filter == BelowPublicLiabilityRequirement ? isBelowPublicLiabilityRequirement : Passes(status, filter);

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
