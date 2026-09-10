namespace Jewel.JPMS.Models;

// The kind of company a directory record is. Used for filtering — e.g. only Subcontractor (and
// Supplier) records are offered when inviting to a bid package, never Clients or Architects.
// Extensible: add values as more company types are tracked. Subcontractor is the default.
public enum DirectoryCategory
{
    Subcontractor = 0,
    Client = 1,
    Architect = 2,
    Supplier = 3,
    Other = 4
}

public enum ComplianceStatus
{
    Current,
    ExpiringSoon,
    Expired,
    Missing
}

// A trade from the curated master list (e.g. "Bricklayer"). Directory records carry a set of these
// rather than a free-text string, so RFI/bid-package trade filters group reliably.
public sealed record Trade(string TradeId, string Name);

// A company directory record. Originally subcontractor-only; now any company type (see Category).
// The id/field names keep the "Subcontractor" prefix for back-compat with existing references
// (bid-package recipients, compliance docs) while the directory is unified by Category.
public sealed record Subcontractor(
    string SubcontractorId,
    string CompanyName,
    IReadOnlyList<Trade> Trades,
    string ContactName,
    string ContactEmail,
    string ContactPhone,
    string CisStatus,
    DateTimeOffset OnboardedAt,
    DirectoryCategory Category = DirectoryCategory.Subcontractor,
    string MobileNumber = "",
    string Town = "",
    string County = "",
    string Website = "",
    string Pli = "",
    string PliExpiry = "",
    // Payment terms printed on this company's purchase orders ("30 day terms"): every company
    // defaults to 30 days, overridable per record from the directory's Edit details dialog.
    int PaymentTermsDays = 30,
    // True when the record holds at least one Xero link — it was imported from Xero, or a
    // Xero-imported record is among those consolidated into it. Shown as the link mark in the
    // directory list and on the record's page.
    bool XeroLinked = false,
    // Street line(s) and postcode of the company's postal address (with Town/County above they
    // complete the letter block printed at the top of its purchase orders).
    string AddressLine = "",
    string Postcode = "",
    // True for a record minted only so a bid-package tender list could hold the company (quick-add
    // or the local web search) — a prospect, not a vetted directory entry. Prospects are hidden
    // from the Directory and its pickers until promoted ("Add to directory" on a submitted tender,
    // or automatically when a package is awarded to them), so the directory stays a curated list
    // of companies judged worth working with rather than everyone ever invited to price a job.
    bool IsProspect = false,
    // The Xero contacts this record is linked to (normally one; several only when Xero-imported
    // records were consolidated together). Null from callers that don't show the link — treat
    // through XeroLinks, which never answers null.
    IReadOnlyList<DirectoryXeroLink>? XeroLinks = null,
    // The HMRC CIS verification result (2026-09-09): the verification number HMRC issued
    // ("V1415495651") and the day it was verified. CisStatus above stays the short reading of the
    // result ("Verified 20% standard"). All three are written together by RecordCisVerification.
    string CisVerificationNumber = "",
    DateOnly? CisVerifiedOn = null)
{
    public IReadOnlyList<DirectoryXeroLink> XeroLinks { get; init; } = XeroLinks ?? Array.Empty<DirectoryXeroLink>();

    public bool HasCisVerification =>
        !string.IsNullOrWhiteSpace(CisStatus) || !string.IsNullOrWhiteSpace(CisVerificationNumber) || CisVerifiedOn is not null;

    // The letter-style address block for the purchase order's Sub/Vendor panel: street line(s),
    // town, county, postcode — blanks skipped.
    public IReadOnlyList<string> AddressLines =>
        new[] { AddressLine, Town, County, Postcode }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToList();

    // Display helper: the trade names joined for one-line contexts (tables, subtitles).
    public string TradesLabel => string.Join(" · ", Trades.Select(trade => trade.Name));

    public bool HasTrade(string tradeId) =>
        Trades.Any(trade => string.Equals(trade.TradeId, tradeId, StringComparison.OrdinalIgnoreCase));
}

// One link between a directory record and a Xero contact: written by Import from Xero or by
// "Link to Xero contact" on an existing record, moved to the master by consolidation. XeroContactName
// is the contact's name in Xero when the link was made — kept so the record can say which Xero
// contact it settles through even after either side is renamed.
public sealed record DirectoryXeroLink(
    string XeroContactId,
    string XeroContactName,
    DateTimeOffset LinkedAt,
    string LinkedByEmail);

// A person on a company directory record, beyond the record's single primary contact line. A
// consolidated master record keeps every merged email/phone as one of these.
public sealed record CompanyContact(
    string CompanyContactId,
    string SubcontractorId,
    string Name,
    string Email,
    string Phone,
    DateTimeOffset CreatedAt);

public sealed record ComplianceDocument(
    string ComplianceDocumentId,
    string SubcontractorId,
    string Kind,
    string FileName,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset UploadedAt,
    int Version = 1,
    DateTimeOffset? SupersededAt = null,
    bool HasFile = false,
    long FileSize = 0)
{
    /// <summary>The live version of its Kind. Superseded versions are audit history and should
    /// not drive expiry banners or status pills.</summary>
    public bool IsCurrentVersion => SupersededAt is null;
}

public static class ComplianceDocumentExtensions
{
    public static ComplianceStatus Status(this ComplianceDocument document)
    {
        if (document.ExpiresAt is null) return ComplianceStatus.Current;
        var daysToExpiry = (document.ExpiresAt.Value - DateTimeOffset.UtcNow).TotalDays;
        if (daysToExpiry < 0) return ComplianceStatus.Expired;
        if (daysToExpiry < 30) return ComplianceStatus.ExpiringSoon;
        return ComplianceStatus.Current;
    }

    /// <summary>A company's standing: the worst status among its current documents, Missing when
    /// it holds none. The one reading the Directory's compliance chips, the register and the
    /// connector share (2026-09-09).</summary>
    public static ComplianceStatus Standing(this IEnumerable<ComplianceDocument> documents)
    {
        var current = documents.Where(document => document.IsCurrentVersion).ToList();
        if (current.Count == 0) return ComplianceStatus.Missing;
        return current.Select(document => document.Status()).Max();
    }
}

public static class ComplianceStatusExtensions
{
    public static string DisplayName(this ComplianceStatus status) => status switch
    {
        ComplianceStatus.Current      => "Current",
        ComplianceStatus.ExpiringSoon => "Expiring soon",
        ComplianceStatus.Expired      => "Expired",
        ComplianceStatus.Missing      => "Missing",
        _ => status.ToString()
    };

    /// <summary>The order every compliance list, chip row and connector read shares (2026-09-10,
    /// the accountant's ask): what is lapsing, then what is in date, then the companies with
    /// nothing on file. Missing is a standing but not a document — most of the directory has
    /// never filed one, so ranking it above Current buried the handful of companies whose
    /// documents actually matter under hundreds of blank rows. Expired stays first: an expired
    /// document is the worst thing on file.</summary>
    public static readonly ComplianceStatus[] ReadingOrder =
    {
        ComplianceStatus.Expired, ComplianceStatus.ExpiringSoon, ComplianceStatus.Current, ComplianceStatus.Missing
    };

    public static int ReadingRank(this ComplianceStatus status) => Array.IndexOf(ReadingOrder, status);
}
