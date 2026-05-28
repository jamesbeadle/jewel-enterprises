using Jewel.JPMS.Contracts.Cashflow;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Cashflow;

public sealed class CashflowReadModel
{
    private readonly IQueryClient queries;

    public CashflowReadModel(IQueryClient queries) { this.queries = queries; }

    public CashflowSnapshot? Current { get; private set; }

    public bool HasLoaded { get; private set; }

    public event Action? OnChanged;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new GetLatestCashflowSnapshot(), cancellationToken);
        HasLoaded = true;
        OnChanged?.Invoke();
    }
}
