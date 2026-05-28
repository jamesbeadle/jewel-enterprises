using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed partial class HttpCvrStore
{
    public IReadOnlyList<ForecastComponent> ForecastComponentsFor(string projectId) =>
        queries.AskAsync(new ListForecastComponentsForProject(projectId), CancellationToken.None).GetAwaiter().GetResult();

    public ForecastComponent SaveForecastComponent(ForecastComponent component)
    {
        _ = SendThenNotify(new RecordForecastComponent(component.ProjectId, component.PackageName, component.CostIncurred, component.CostCommitted, component.QsAccrualAmount, component.PrelimForecast, component.CostToComplete));
        return component;
    }

    public IReadOnlyList<QsAccrual> AccrualsFor(string projectId) =>
        queries.AskAsync(new ListQsAccrualsForProject(projectId), CancellationToken.None).GetAwaiter().GetResult();

    public QsAccrual SaveAccrual(QsAccrual accrual)
    {
        if (string.IsNullOrEmpty(accrual.QsAccrualId))
            _ = SendThenNotify(new RecordQsAccrual(accrual.ProjectId, accrual.Category, accrual.Description, accrual.AddAmount, accrual.OmitAmount, accrual.LiabilityAmount, accrual.SignedOffByEmail));
        else
            _ = SendThenNotify(new UpdateQsAccrual(accrual.QsAccrualId, accrual.Category, accrual.Description, accrual.AddAmount, accrual.OmitAmount, accrual.LiabilityAmount, accrual.SignedOffByEmail));
        return accrual;
    }

    public IReadOnlyList<PrelimItem> PrelimsFor(string projectId) =>
        queries.AskAsync(new ListPrelimItemsForProject(projectId), CancellationToken.None).GetAwaiter().GetResult();

    public IReadOnlyList<PrelimForecastEntry> PrelimEntriesFor(string prelimItemId) =>
        queries.AskAsync(new ListPrelimEntriesForItem(prelimItemId), CancellationToken.None).GetAwaiter().GetResult();

    public PrelimForecastEntry SavePrelimForecast(string projectId, string prelimDescription, PrelimForecastEntry entry)
    {
        _ = SendThenNotify(new RecordPrelimForecastForWeek(projectId, prelimDescription, entry.WeekNumber, entry.TenderedAmount, entry.ActualAmount, entry.ForecastAmount));
        return entry;
    }

    public IReadOnlyList<Eot> EotsFor(string projectId) =>
        queries.AskAsync(new ListEotsForProject(projectId), CancellationToken.None).GetAwaiter().GetResult();

    public Eot SaveEot(Eot eot)
    {
        if (string.IsNullOrEmpty(eot.EotId))
            _ = SendThenNotify(new GrantEot(eot.ProjectId, eot.Reason, eot.DaysGranted, eot.CommercialRecovery));
        else
            _ = SendThenNotify(new UpdateEot(eot.EotId, eot.Reason, eot.DaysGranted, eot.CommercialRecovery));
        return eot;
    }
}
