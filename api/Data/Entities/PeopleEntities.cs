using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

public sealed class SubcontractorEntity
{
    [Key, MaxLength(64)] public string SubcontractorId { get; set; } = "";
    [MaxLength(256)]     public string CompanyName { get; set; } = "";
    [MaxLength(256)]     public string ContactName { get; set; } = "";
    [MaxLength(256)]     public string ContactEmail { get; set; } = "";
    [MaxLength(64)]      public string ContactPhone { get; set; } = "";
    [MaxLength(64)]      public string CisStatus { get; set; } = "";
    // The HMRC verification result behind CisStatus (2026-09-09): the number HMRC issued
    // ("V1415495651") and the day it was verified. Written together by RecordCisVerification.
    [MaxLength(32)]      public string CisVerificationNumber { get; set; } = "";
    public DateOnly? CisVerifiedOn { get; set; }
    public DateTimeOffset OnboardedAt { get; set; }

    // Company-directory fields. Category drives filtering (0 = Subcontractor by default). The rest
    // mirror the master-sheet columns.
    public int Category { get; set; }
    [MaxLength(64)]      public string MobileNumber { get; set; } = "";
    [MaxLength(128)]     public string Town { get; set; } = "";
    [MaxLength(128)]     public string County { get; set; } = "";
    [MaxLength(512)]     public string Website { get; set; } = "";
    [MaxLength(128)]     public string Pli { get; set; } = "";
    [MaxLength(64)]      public string PliExpiry { get; set; } = "";

    // Payment terms printed on this company's purchase orders ("30 day terms"). Every record
    // defaults to 30 days; overridable per company from the directory's Edit details dialog.
    public int PaymentTermsDays { get; set; } = 30;

    // Postal address: street line(s) + postcode join Town/County above so a purchase order can
    // print the supplier's full address letter-style in its Sub/Vendor block.
    [MaxLength(256)]     public string AddressLine { get; set; } = "";
    [MaxLength(32)]      public string Postcode { get; set; } = "";

    // True for a record minted only so a bid-package tender list could hold the company (quick-add
    // or the local web search). Hidden from the Directory until promoted — by the "Add to
    // directory" act on a submitted tender, or automatically when a package is awarded to them.
    public bool IsProspect { get; set; }
}

// A link between a directory record and a Xero contact, written when a supplier is imported from
// Xero. The link is what marks the record "linked to Xero" (the link glyph in the directory), and
// it survives consolidation: merging re-points the links to the master record, so a master built
// from any Xero-imported record stays linked — possibly to several Xero contacts at once.
// XeroContactId is unique so one Xero supplier can only ever be imported once.
public sealed class SubcontractorXeroLinkEntity
{
    [Key, MaxLength(64)] public string SubcontractorXeroLinkId { get; set; } = "";
    [MaxLength(64)]      public string SubcontractorId { get; set; } = "";
    [MaxLength(64)]      public string XeroContactId { get; set; } = "";
    // The supplier's name in Xero at import time — kept for display/troubleshooting even if the
    // portal record is later renamed or merged.
    [MaxLength(256)]     public string XeroContactName { get; set; } = "";
    public DateTimeOffset ImportedAt { get; set; }
    [MaxLength(256)]     public string ImportedByEmail { get; set; } = "";
}

// A person on a directory record beyond its single primary contact line. Consolidation keeps every
// merged record's contact details as one of these (so no email or phone number is lost), and Xero
// imports add the Xero contact persons.
public sealed class CompanyContactEntity
{
    [Key, MaxLength(64)] public string CompanyContactId { get; set; } = "";
    [MaxLength(64)]      public string SubcontractorId { get; set; } = "";
    [MaxLength(256)]     public string Name { get; set; } = "";
    [MaxLength(256)]     public string Email { get; set; } = "";
    [MaxLength(64)]      public string Phone { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

// The curated master list of trades. Directory records link to these via SubcontractorTrades, so a
// trade is added deliberately once and reused everywhere — never typed free-text per record (the old
// PrimaryTrade string allowed slash-separated compounds like "Boarding/drylining/Plastering").
public sealed class TradeEntity
{
    [Key, MaxLength(64)] public string TradeId { get; set; } = "";
    [MaxLength(64)]      public string Name { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

// Link table: one row per (subcontractor, trade). A directory record can carry several trades.
public sealed class SubcontractorTradeEntity
{
    [Key, MaxLength(64)] public string SubcontractorTradeId { get; set; } = "";
    [MaxLength(64)]      public string SubcontractorId { get; set; } = "";
    [MaxLength(64)]      public string TradeId { get; set; } = "";
}

public sealed class ComplianceDocumentEntity
{
    [Key, MaxLength(64)] public string ComplianceDocumentId { get; set; } = "";
    [MaxLength(64)]      public string SubcontractorId { get; set; } = "";
    [MaxLength(128)]     public string Kind { get; set; } = "";
    [MaxLength(256)]     public string FileName { get; set; } = "";
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset UploadedAt { get; set; }

    // File storage (empty BlobPath = metadata-only record from before files were stored).
    [MaxLength(1024)]    public string BlobPath { get; set; } = "";
    [MaxLength(256)]     public string ContentType { get; set; } = "";
    public long FileSize { get; set; }

    // Versioning: re-uploading the same Kind supersedes the previous latest rather than replacing
    // it (scoping decision, docs/06-backlog/subcontractor-crm-scope.md §4). The latest version of
    // a Kind has SupersededAt == null and drives its expiry status.
    public int Version { get; set; } = 1;
    public DateTimeOffset? SupersededAt { get; set; }

    // The public liability limit of indemnity the certificate states, in pounds (2026-09-10, the
    // accountant's ask — £5m required on big jobs). Null = not recorded, never nil cover; normal
    // on a non-insurance document. decimal(18,4) by the context's convention.
    public decimal? PublicLiabilityCover { get; set; }
}

public sealed class HsRecordEntity
{
    [Key, MaxLength(64)] public string HsRecordId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public int Kind { get; set; }
    [MaxLength(512)]     public string Summary { get; set; } = "";
    public int Severity { get; set; }
    public int Status { get; set; }
    [MaxLength(256)]     public string AssignedToEmail { get; set; } = "";
    public DateTimeOffset RaisedAt { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}

public sealed class HsRecordAttendanceEntity
{
    [Key, MaxLength(64)] public string HsRecordAttendanceId { get; set; } = "";
    [MaxLength(64)]      public string HsRecordId { get; set; } = "";
    [MaxLength(256)]     public string AttendeeName { get; set; } = "";
    [MaxLength(256)]     public string SignatureBlobRef { get; set; } = "";
    public DateTimeOffset SignedAt { get; set; }
}
