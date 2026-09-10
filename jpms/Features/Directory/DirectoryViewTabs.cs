namespace Jewel.JPMS.Features.Directory;

/// <summary>The company directory's sibling views, reached by navigating: the companies list and
/// the compliance register (every company's documents in one list). Both pages render the same row
/// so moving between them reads as switching tabs.</summary>
public static class DirectoryViewTabs
{
    public const string Companies = "companies";
    public const string ComplianceRegister = "compliance";

    public static readonly IReadOnlyList<TabItem> Items = new[]
    {
        new TabItem(Companies, "Companies", "/directory"),
        new TabItem(ComplianceRegister, "Compliance register", "/directory/compliance",
            Title: "Every company's compliance documents in one list — expired and expiring first, companies with nothing on file last")
    };
}
