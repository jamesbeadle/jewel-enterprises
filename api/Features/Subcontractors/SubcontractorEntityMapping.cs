using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Subcontractors;

internal static class SubcontractorEntityMapping
{
    // xeroLinked defaults to false so callers that don't show the Xero link mark — the portal's
    // own-record read, command handlers returning the record just written — keep working unchanged;
    // ListSubcontractors passes the real value from the SubcontractorXeroLinks table, and the
    // links themselves (xeroLinks) so a record page can say WHICH Xero contact it settles through.
    public static Subcontractor ToModel(this SubcontractorEntity entity, IReadOnlyList<Trade> trades, bool xeroLinked = false,
        IReadOnlyList<DirectoryXeroLink>? xeroLinks = null) =>
        new(entity.SubcontractorId, entity.CompanyName, trades, entity.ContactName, entity.ContactEmail, entity.ContactPhone, entity.CisStatus, entity.OnboardedAt,
            (DirectoryCategory)entity.Category, entity.MobileNumber, entity.Town, entity.County, entity.Website, entity.Pli, entity.PliExpiry,
            entity.PaymentTermsDays, xeroLinked, entity.AddressLine, entity.Postcode, entity.IsProspect, xeroLinks,
            entity.CisVerificationNumber, entity.CisVerifiedOn);

    public static Trade ToModel(this TradeEntity entity) => new(entity.TradeId, entity.Name);

    public static ComplianceDocument ToModel(this ComplianceDocumentEntity entity) =>
        new(entity.ComplianceDocumentId, entity.SubcontractorId, entity.Kind, entity.FileName, entity.ExpiresAt, entity.UploadedAt,
            entity.Version, entity.SupersededAt, HasFile: !string.IsNullOrEmpty(entity.BlobPath), entity.FileSize);

    public static CompanyContact ToModel(this CompanyContactEntity entity) =>
        new(entity.CompanyContactId, entity.SubcontractorId, entity.Name, entity.Email, entity.Phone, entity.CreatedAt);
}
