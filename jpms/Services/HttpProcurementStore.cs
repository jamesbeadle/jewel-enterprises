using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpProcurementStore : IProcurementStore
{
    private readonly BidPackagesReadModel packagesReadModel;
    private readonly WorkOrdersReadModel workOrdersReadModel;
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    public HttpProcurementStore(BidPackagesReadModel packagesReadModel, WorkOrdersReadModel workOrdersReadModel, IQueryClient queries, ICommandSender commands)
    {
        this.packagesReadModel = packagesReadModel;
        this.workOrdersReadModel = workOrdersReadModel;
        this.queries = queries;
        this.commands = commands;
        packagesReadModel.OnChanged += () => OnChange?.Invoke();
        workOrdersReadModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public IReadOnlyList<BidPackage> PackagesFor(string projectId)
    {
        if (packagesReadModel.Current(projectId).Count == 0) _ = packagesReadModel.RefreshAsync(projectId, CancellationToken.None);
        return packagesReadModel.Current(projectId);
    }

    public BidPackage? FindPackage(string bidPackageId) =>
        queries.AskAsync(new GetBidPackageById(bidPackageId), CancellationToken.None).GetAwaiter().GetResult();

    public BidPackage Upsert(BidPackage package)
    {
        if (string.IsNullOrEmpty(package.BidPackageId))
            _ = CreatePackageAsync(package);
        else _ = UpdatePackageAsync(package);
        return package;
    }

    public IReadOnlyList<Quote> QuotesFor(string bidPackageId) =>
        queries.AskAsync(new ListQuotesForBidPackage(bidPackageId), CancellationToken.None).GetAwaiter().GetResult();

    public Quote SaveQuote(Quote quote)
    {
        if (string.IsNullOrEmpty(quote.QuoteId))
            _ = commands.SendAsync(new SubmitQuoteForBidPackage(quote.BidPackageId, quote.SubcontractorId, quote.Value, quote.Notes), CancellationToken.None);
        else
            _ = commands.SendAsync(new ReviseQuote(quote.QuoteId, quote.Value, quote.Notes), CancellationToken.None);
        return quote;
    }

    public IReadOnlyList<WorkOrder> AllWorkOrders()
    {
        if (workOrdersReadModel.Current is null) _ = workOrdersReadModel.RefreshAsync(CancellationToken.None);
        return workOrdersReadModel.Current ?? Array.Empty<WorkOrder>();
    }

    public IReadOnlyList<WorkOrder> WorkOrdersFor(string projectId) =>
        AllWorkOrders().Where(workOrder => string.Equals(workOrder.ProjectId, projectId, StringComparison.OrdinalIgnoreCase)).ToList().AsReadOnly();

    public WorkOrder? FindWorkOrder(string workOrderId) =>
        AllWorkOrders().FirstOrDefault(workOrder => string.Equals(workOrder.WorkOrderId, workOrderId, StringComparison.OrdinalIgnoreCase));

    public WorkOrder Award(WorkOrder workOrder)
    {
        _ = AwardAsync(workOrder);
        return workOrder;
    }

    private async Task CreatePackageAsync(BidPackage package)
    {
        await commands.SendAsync(new CreateBidPackage(package.ProjectId, package.Title, package.Trade, package.OwnerEmail), CancellationToken.None);
        await packagesReadModel.RefreshAsync(package.ProjectId, CancellationToken.None);
    }

    private async Task UpdatePackageAsync(BidPackage package)
    {
        await commands.SendAsync(new UpdateBidPackageScope(package.BidPackageId, package.Title, package.Trade, package.Status, package.OwnerEmail), CancellationToken.None);
        await packagesReadModel.RefreshAsync(package.ProjectId, CancellationToken.None);
    }

    private async Task AwardAsync(WorkOrder workOrder)
    {
        await commands.SendAsync(new AwardBidPackage(workOrder.BidPackageId, workOrder.ProjectId, workOrder.SubcontractorId, workOrder.Value, workOrder.Scope, workOrder.AwardedByEmail), CancellationToken.None);
        await workOrdersReadModel.RefreshAsync(CancellationToken.None);
    }
}
