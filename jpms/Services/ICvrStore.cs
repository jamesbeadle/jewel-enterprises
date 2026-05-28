using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface ICvrStore
{
    IReadOnlyList<CvrSnapshot> SnapshotsFor(string projectId);
    CvrSnapshot? LatestSnapshot(string projectId);
    CvrSnapshot CaptureSnapshot(CvrSnapshot snapshot);

    IReadOnlyList<CvrPackageRow> PackagesFor(string projectId);
    IReadOnlyList<ForecastComponent> ForecastComponentsFor(string projectId);

    IReadOnlyList<QsAccrual> AccrualsFor(string projectId);
    QsAccrual SaveAccrual(QsAccrual accrual);

    IReadOnlyList<PrelimItem> PrelimsFor(string projectId);
    IReadOnlyList<PrelimForecastEntry> PrelimEntriesFor(string prelimItemId);

    IReadOnlyList<Eot> EotsFor(string projectId);
    Eot SaveEot(Eot eot);

    event Action? OnChange;
}
