using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Projects;

internal static class ProjectEntityMapping
{
    public static Project ToModel(this ProjectEntity entity) => new(
        ProjectId: entity.ProjectId,
        Reference: entity.Reference,
        Name: entity.Name,
        ClientName: entity.ClientName,
        Organisation: (Organisation)entity.Organisation,
        Stage: (ProjectStage)entity.Stage,
        ProjectManagerEmail: entity.ProjectManagerEmail,
        CreatedAt: entity.CreatedAt,
        PartyKind: (PartyKind)entity.PartyKind,
        PartyId: entity.PartyId,
        OnBehalfOfClientId: entity.OnBehalfOfClientId,
        AddressLine: entity.AddressLine,
        Town: entity.Town,
        Postcode: entity.Postcode,
        XeroSiteName: entity.XeroSiteName,
        NextExpectedValuationDate: entity.NextExpectedValuationDate,
        ExpectedMonthlyValuation: entity.ExpectedMonthlyValuation,
        XeroContactId: entity.XeroContactId,
        XeroContactName: entity.XeroContactName);
}
