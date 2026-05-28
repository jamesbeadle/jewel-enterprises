using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Cvr;

internal static class CvrEntityMapping
{
    public static CvrSnapshot ToModel(this CvrSnapshotEntity entity) =>
        new(entity.CvrSnapshotId, entity.ProjectId, entity.SnapshotAt, entity.TenderValue, entity.ForecastFinalCost, entity.ForecastFinalValue, entity.MarginPounds, entity.MarginPercent, entity.WeeksAheadOrBehind);

    public static ForecastComponent ToModel(this ForecastComponentEntity entity) =>
        new(entity.ForecastComponentId, entity.ProjectId, entity.PackageName, entity.CostIncurred, entity.CostCommitted, entity.QsAccrualAmount, entity.PrelimForecast, entity.CostToComplete);

    public static QsAccrual ToModel(this QsAccrualEntity entity) =>
        new(entity.QsAccrualId, entity.ProjectId, entity.Category, entity.Description, entity.AddAmount, entity.OmitAmount, entity.LiabilityAmount, entity.SignedOffByEmail, entity.SignedOffAt);

    public static PrelimItem ToModel(this PrelimItemEntity entity) =>
        new(entity.PrelimItemId, entity.ProjectId, entity.Description);

    public static PrelimForecastEntry ToModel(this PrelimForecastEntryEntity entity) =>
        new(entity.PrelimForecastEntryId, entity.PrelimItemId, entity.WeekNumber, entity.TenderedAmount, entity.ActualAmount, entity.ForecastAmount);

    public static Eot ToModel(this EotEntity entity) =>
        new(entity.EotId, entity.ProjectId, entity.Reason, entity.DaysGranted, entity.CommercialRecovery, entity.GrantedAt);

    public static CvrPackageRow ToModel(this CvrPackageRowEntity entity) =>
        new(entity.ProjectId, entity.PackageName, entity.OrderCost, entity.OrderValue, entity.VariationCost, entity.VariationValue, entity.MovementSinceLastSnapshot);
}
