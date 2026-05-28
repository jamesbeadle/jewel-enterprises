using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Cashflow;
using Jewel.JPMS.Features.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpCommercialStore : ICommercialStore
{
    private static readonly CashflowSnapshot NoCashflowYet = new("", DateTimeOffset.MinValue, 0m, 0m, 0m);

    private readonly ValuationsReadModel valuationsReadModel;
    private readonly CostCodeBudgetsReadModel budgetsReadModel;
    private readonly TimesheetsReadModel timesheetsReadModel;
    private readonly CashflowReadModel cashflowReadModel;
    private readonly ICommandSender commands;

    public HttpCommercialStore(ValuationsReadModel valuationsReadModel, CostCodeBudgetsReadModel budgetsReadModel, TimesheetsReadModel timesheetsReadModel, CashflowReadModel cashflowReadModel, ICommandSender commands)
    {
        this.valuationsReadModel = valuationsReadModel;
        this.budgetsReadModel = budgetsReadModel;
        this.timesheetsReadModel = timesheetsReadModel;
        this.cashflowReadModel = cashflowReadModel;
        this.commands = commands;
        valuationsReadModel.OnChanged += () => OnChange?.Invoke();
        budgetsReadModel.OnChanged += () => OnChange?.Invoke();
        timesheetsReadModel.OnChanged += () => OnChange?.Invoke();
        cashflowReadModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public IReadOnlyList<ClaimPeriod> ClaimPeriodsFor(string projectId) => Array.Empty<ClaimPeriod>();

    public IReadOnlyList<Valuation> ValuationsFor(string projectId)
    {
        if (valuationsReadModel.Current(projectId).Count == 0) _ = valuationsReadModel.RefreshAsync(projectId, CancellationToken.None);
        return valuationsReadModel.Current(projectId);
    }

    public Valuation SaveValuation(Valuation valuation)
    {
        if (string.IsNullOrEmpty(valuation.ValuationId))
            _ = DraftAsync(valuation);
        else if (valuation.IsIssued)
            _ = IssueAsync(valuation);
        else
            _ = ReviseAsync(valuation);
        return valuation;
    }

    public IReadOnlyList<CostCodeBudget> BudgetsFor(string projectId)
    {
        if (budgetsReadModel.Current(projectId).Count == 0) _ = budgetsReadModel.RefreshAsync(projectId, CancellationToken.None);
        return budgetsReadModel.Current(projectId);
    }

    public IReadOnlyList<Timesheet> TimesheetsFor(string projectId)
    {
        if (timesheetsReadModel.Current(projectId).Count == 0) _ = timesheetsReadModel.RefreshAsync(projectId, CancellationToken.None);
        return timesheetsReadModel.Current(projectId);
    }

    public Timesheet SaveTimesheet(Timesheet timesheet)
    {
        if (string.IsNullOrEmpty(timesheet.TimesheetId))
            _ = SubmitAsync(timesheet);
        return timesheet;
    }

    public Timesheet ApproveTimesheet(string timesheetId)
    {
        _ = commands.SendAsync(new ApproveTimesheet(timesheetId), CancellationToken.None);
        return new Timesheet(timesheetId, "", "", DateTimeOffset.UtcNow, 0, "", true);
    }

    public CashflowSnapshot LatestCashflow()
    {
        if (!cashflowReadModel.HasLoaded) _ = cashflowReadModel.RefreshAsync(CancellationToken.None);
        return cashflowReadModel.Current ?? NoCashflowYet;
    }

    private async Task DraftAsync(Valuation valuation)
    {
        await commands.SendAsync(new DraftValuation(valuation.ProjectId, valuation.ClaimPeriodId, valuation.GrossValue, valuation.RetentionPercent), CancellationToken.None);
        await valuationsReadModel.RefreshAsync(valuation.ProjectId, CancellationToken.None);
    }

    private async Task IssueAsync(Valuation valuation)
    {
        await commands.SendAsync(new IssueValuation(valuation.ValuationId), CancellationToken.None);
        await valuationsReadModel.RefreshAsync(valuation.ProjectId, CancellationToken.None);
    }

    private async Task ReviseAsync(Valuation valuation)
    {
        await commands.SendAsync(new ReviseValuation(valuation.ValuationId, valuation.GrossValue, valuation.RetentionPercent), CancellationToken.None);
        await valuationsReadModel.RefreshAsync(valuation.ProjectId, CancellationToken.None);
    }

    private async Task SubmitAsync(Timesheet timesheet)
    {
        await commands.SendAsync(new SubmitTimesheet(timesheet.ProjectId, timesheet.PersonEmail, timesheet.WorkedOn, timesheet.Hours, timesheet.CostCode), CancellationToken.None);
        await timesheetsReadModel.RefreshAsync(timesheet.ProjectId, CancellationToken.None);
    }
}
