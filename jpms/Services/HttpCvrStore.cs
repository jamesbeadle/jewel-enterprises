using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Cvr;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpCvrStore : ICvrStore
{
    private readonly CvrSnapshotsReadModel snapshotsReadModel;
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    public HttpCvrStore(CvrSnapshotsReadModel snapshotsReadModel, IQueryClient queries, ICommandSender commands)
    {
        this.snapshotsReadModel = snapshotsReadModel;
        this.queries = queries;
        this.commands = commands;
        snapshotsReadModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public IReadOnlyList<CvrSnapshot> SnapshotsFor(string projectId)
    {
        if (snapshotsReadModel.Current(projectId).Count == 0) _ = snapshotsReadModel.RefreshAsync(projectId, CancellationToken.None);
        return snapshotsReadModel.Current(projectId);
    }

    public CvrSnapshot? LatestSnapshot(string projectId) =>
        SnapshotsFor(projectId).FirstOrDefault();

    public CvrSnapshot CaptureSnapshot(CvrSnapshot snapshot)
    {
        _ = CaptureSnapshotAsync(snapshot);
        return snapshot;
    }

    private async Task CaptureSnapshotAsync(CvrSnapshot snapshot)
    {
        await commands.SendAsync(
            new CaptureCvrSnapshot(snapshot.ProjectId, snapshot.TenderValue, snapshot.ForecastFinalCost, snapshot.ForecastFinalValue, snapshot.WeeksAheadOrBehind),
            CancellationToken.None);
        await snapshotsReadModel.RefreshAsync(snapshot.ProjectId, CancellationToken.None);
    }

    public IReadOnlyList<CvrPackageRow> PackagesFor(string projectId) => Array.Empty<CvrPackageRow>();

    public IReadOnlyList<ForecastComponent> ForecastComponentsFor(string projectId) =>
        queries.AskAsync(new ListForecastComponentsForProject(projectId), CancellationToken.None).GetAwaiter().GetResult();

    public IReadOnlyList<QsAccrual> AccrualsFor(string projectId) =>
        queries.AskAsync(new ListQsAccrualsForProject(projectId), CancellationToken.None).GetAwaiter().GetResult();

    public QsAccrual SaveAccrual(QsAccrual accrual)
    {
        if (string.IsNullOrEmpty(accrual.QsAccrualId))
            _ = commands.SendAsync(new RecordQsAccrual(accrual.ProjectId, accrual.Category, accrual.Description, accrual.AddAmount, accrual.OmitAmount, accrual.LiabilityAmount, accrual.SignedOffByEmail), CancellationToken.None);
        else
            _ = commands.SendAsync(new UpdateQsAccrual(accrual.QsAccrualId, accrual.Category, accrual.Description, accrual.AddAmount, accrual.OmitAmount, accrual.LiabilityAmount, accrual.SignedOffByEmail), CancellationToken.None);
        return accrual;
    }

    public IReadOnlyList<PrelimItem> PrelimsFor(string projectId) =>
        queries.AskAsync(new ListPrelimItemsForProject(projectId), CancellationToken.None).GetAwaiter().GetResult();

    public IReadOnlyList<PrelimForecastEntry> PrelimEntriesFor(string prelimItemId) =>
        queries.AskAsync(new ListPrelimEntriesForItem(prelimItemId), CancellationToken.None).GetAwaiter().GetResult();

    public IReadOnlyList<Eot> EotsFor(string projectId) =>
        queries.AskAsync(new ListEotsForProject(projectId), CancellationToken.None).GetAwaiter().GetResult();

    public Eot SaveEot(Eot eot)
    {
        if (string.IsNullOrEmpty(eot.EotId))
            _ = commands.SendAsync(new GrantEot(eot.ProjectId, eot.Reason, eot.DaysGranted, eot.CommercialRecovery), CancellationToken.None);
        else
            _ = commands.SendAsync(new UpdateEot(eot.EotId, eot.Reason, eot.DaysGranted, eot.CommercialRecovery), CancellationToken.None);
        return eot;
    }
}
