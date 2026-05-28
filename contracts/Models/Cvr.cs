namespace Jewel.JPMS.Models;

public sealed record CvrPackageRow(
    string ProjectId,
    string PackageName,
    decimal OrderCost,
    decimal OrderValue,
    decimal VariationCost,
    decimal VariationValue,
    decimal MovementSinceLastSnapshot)
{
    public decimal OrderProfit => OrderValue - OrderCost;
    public decimal VariationProfit => VariationValue - VariationCost;
    public decimal CombinedCost => OrderCost + VariationCost;
    public decimal CombinedValue => OrderValue + VariationValue;
    public decimal CombinedProfit => CombinedValue - CombinedCost;

    public decimal OrderProfitOnCostPercent => Commercial.CvrCalculations.ProfitOnCostPercent(OrderProfit, OrderCost);
    public decimal VariationProfitOnCostPercent => Commercial.CvrCalculations.ProfitOnCostPercent(VariationProfit, VariationCost);
    public decimal CombinedProfitOnCostPercent => Commercial.CvrCalculations.ProfitOnCostPercent(CombinedProfit, CombinedCost);
    public decimal CombinedProfitOnValuePercent => Commercial.CvrCalculations.ProfitOnValuePercent(CombinedProfit, CombinedValue);
}

public sealed record ForecastComponent(
    string ForecastComponentId,
    string ProjectId,
    string PackageName,
    decimal CostIncurred,
    decimal CostCommitted,
    decimal QsAccrualAmount,
    decimal PrelimForecast,
    decimal CostToComplete)
{
    public decimal ForecastFinalCost =>
        CostIncurred + CostCommitted + QsAccrualAmount + PrelimForecast + CostToComplete;
}

public sealed record QsAccrual(
    string QsAccrualId,
    string ProjectId,
    string Category,
    string Description,
    decimal AddAmount,
    decimal OmitAmount,
    decimal LiabilityAmount,
    string SignedOffByEmail,
    DateTimeOffset SignedOffAt)
{
    public decimal NetAmount => AddAmount - OmitAmount + LiabilityAmount;
}

public sealed record PrelimItem(
    string PrelimItemId,
    string ProjectId,
    string Description);

public sealed record PrelimForecastEntry(
    string PrelimForecastEntryId,
    string PrelimItemId,
    int WeekNumber,
    decimal TenderedAmount,
    decimal ActualAmount,
    decimal ForecastAmount)
{
    public decimal DifferenceAmount => ForecastAmount - TenderedAmount;
}

public sealed record Eot(
    string EotId,
    string ProjectId,
    string Reason,
    int DaysGranted,
    decimal CommercialRecovery,
    DateTimeOffset GrantedAt);
