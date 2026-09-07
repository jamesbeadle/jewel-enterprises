using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Closeout;

internal static class CloseoutEntityMapping
{
    /// <summary>The defect as the portal sees it. The supplier's name and contact email come from
    /// the directory record the defect names (DefectSupplierLookup resolves them per read); an
    /// unresolved id — the record deleted or consolidated away — reads as no supplier picked.</summary>
    public static Defect ToModel(this DefectEntity entity, SubcontractorEntity? supplier = null) =>
        new(entity.DefectId, entity.ProjectId, entity.Description, entity.Location, entity.AssignedToEmail,
            (DefectStatus)entity.Status, entity.RaisedAt, entity.ResolvedAt, entity.Reference,
            SubcontractorId: supplier?.SubcontractorId ?? entity.SubcontractorId,
            SubcontractorName: supplier?.CompanyName,
            SentToSupplierAt: entity.SentToSupplierAt,
            SentToSupplierByEmail: entity.SentToSupplierByEmail,
            SupplierContactEmail: supplier?.ContactEmail ?? "");

    public static SettlementRecord ToModel(this SettlementRecordEntity entity) =>
        new(entity.SettlementRecordId, entity.ProjectId, entity.FinalContractValue, entity.FinalCost, entity.FinalMargin, entity.AgreedAt, entity.IsClientSigned);

    public static VatAnalysis ToModel(this VatAnalysisEntity entity) =>
        new(entity.VatAnalysisId, entity.ProjectId, entity.ZeroRatedAmount, entity.StandardRatedAmount, entity.Notes, entity.IsClientConfirmed, entity.IsArchitectConfirmed);

    public static RetentionRelease ToModel(this RetentionReleaseEntity entity) =>
        new(entity.RetentionReleaseId, entity.ProjectId, entity.Amount, entity.ReleasedAt, entity.IsPublishedDownstream);
}
