using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Services;

public sealed class HttpValuationInvoiceStore : IValuationInvoiceStore
{
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    public HttpValuationInvoiceStore(IQueryClient queries, ICommandSender commands)
    {
        this.queries = queries;
        this.commands = commands;
    }

    public event Action? OnChange;

    public Task<IReadOnlyList<ValuationInvoice>> ListAsync(string projectId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListValuationInvoicesForProject(projectId), cancellationToken);

    public Task<ProjectValuationInvoiceSummary> GetSummaryAsync(string projectId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new GetProjectValuationInvoiceSummary(projectId), cancellationToken);

    public Task<IReadOnlyList<ValuationInvoiceEvent>> ListEventsAsync(string valuationInvoiceId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListValuationInvoiceEvents(valuationInvoiceId), cancellationToken);

    public async Task<ValuationInvoice> CreateAsync(string projectId, DateTimeOffset periodMonth, decimal amount, string? valuationClaimId, CancellationToken cancellationToken = default)
    {
        var created = await commands.SendAsync(new CreateValuationInvoice(projectId, periodMonth, amount, valuationClaimId), cancellationToken);
        OnChange?.Invoke();
        return created;
    }

    public async Task<ValuationInvoice> CreateManualAsync(string projectId, DateTimeOffset periodMonth, decimal amount, decimal? amountPaid, DateTimeOffset? issuedAt, DateTimeOffset? paidAt, string? note, CancellationToken cancellationToken = default)
    {
        var created = await commands.SendAsync(new CreateValuationInvoice(
            projectId, periodMonth, amount, ValuationClaimId: null,
            IsManual: true, AmountPaid: amountPaid, IssuedAt: issuedAt, PaidAt: paidAt, Note: note), cancellationToken);
        OnChange?.Invoke();
        return created;
    }

    public async Task<ValuationInvoice> UpdateAsync(string valuationInvoiceId, DateTimeOffset periodMonth, decimal amount, decimal? amountPaid = null, DateTimeOffset? issuedAt = null, DateTimeOffset? paidAt = null, string? note = null, CancellationToken cancellationToken = default)
    {
        var updated = await commands.SendAsync(new UpdateValuationInvoice(
            valuationInvoiceId, periodMonth, amount, amountPaid, issuedAt, paidAt, note), cancellationToken);
        OnChange?.Invoke();
        return updated;
    }

    public async Task<ValuationInvoice> SubmitAsync(string valuationInvoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await commands.SendAsync(new SubmitValuationInvoice(valuationInvoiceId), cancellationToken);
        OnChange?.Invoke();
        return invoice;
    }

    public async Task<ValuationInvoice> ApproveAsync(string valuationInvoiceId, string? note = null, CancellationToken cancellationToken = default)
    {
        var invoice = await commands.SendAsync(new ApproveValuationInvoice(valuationInvoiceId, note), cancellationToken);
        OnChange?.Invoke();
        return invoice;
    }

    public async Task<ValuationInvoice> RejectAsync(string valuationInvoiceId, string reason, CancellationToken cancellationToken = default)
    {
        var invoice = await commands.SendAsync(new RejectValuationInvoice(valuationInvoiceId, reason), cancellationToken);
        OnChange?.Invoke();
        return invoice;
    }

    public async Task<ValuationInvoice> CancelAsync(string valuationInvoiceId, string? note = null, CancellationToken cancellationToken = default)
    {
        var invoice = await commands.SendAsync(new CancelValuationInvoice(valuationInvoiceId, note), cancellationToken);
        OnChange?.Invoke();
        return invoice;
    }

    public async Task<ValuationInvoice> IssueAsync(string valuationInvoiceId, string? xeroInvoiceNumber = null, CancellationToken cancellationToken = default)
    {
        var call = await commands.SendAsync(new IssueValuationInvoice(valuationInvoiceId, xeroInvoiceNumber), cancellationToken);
        OnChange?.Invoke();
        return call;
    }

    public async Task<ValuationInvoice> RecordXeroNumberAsync(string valuationInvoiceId, string xeroInvoiceNumber, CancellationToken cancellationToken = default)
    {
        var call = await commands.SendAsync(new RecordValuationInvoiceXeroNumber(valuationInvoiceId, xeroInvoiceNumber), cancellationToken);
        OnChange?.Invoke();
        return call;
    }

    public Task<ValuationInvoiceXeroRaisePreview> PreviewXeroRaiseAsync(string valuationInvoiceId, DateTime? invoiceDate = null, DateTime? dueDate = null, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new PreviewValuationInvoiceXeroRaise(valuationInvoiceId, invoiceDate, dueDate), cancellationToken);

    public async Task<ValuationInvoiceXeroRaiseOutcome> RaiseInXeroAsync(string valuationInvoiceId, DateTime? invoiceDate = null, DateTime? dueDate = null, CancellationToken cancellationToken = default)
    {
        var outcome = await commands.SendAsync(new RaiseValuationInvoiceInXero(valuationInvoiceId, InvoiceDate: invoiceDate, DueDate: dueDate), cancellationToken);
        OnChange?.Invoke();
        return outcome;
    }

    public async Task<ValuationInvoice> RecordPaymentAsync(string valuationInvoiceId, decimal amountPaid, CancellationToken cancellationToken = default)
    {
        var call = await commands.SendAsync(new RecordValuationInvoicePayment(valuationInvoiceId, amountPaid), cancellationToken);
        OnChange?.Invoke();
        return call;
    }

    public async Task DeleteAsync(string valuationInvoiceId, CancellationToken cancellationToken = default)
    {
        await commands.SendAsync(new DeleteValuationInvoice(valuationInvoiceId), cancellationToken);
        OnChange?.Invoke();
    }
}
