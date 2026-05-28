using Jewel.JPMS.Contracts.CommercialInputs;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpCommercialInputsStore : ICommercialInputsStore
{
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    public HttpCommercialInputsStore(IQueryClient queries, ICommandSender commands)
    {
        this.queries = queries;
        this.commands = commands;
    }

    public event Action? OnChange;

    public IReadOnlyList<Daywork> DayworksFor(string projectId) =>
        queries.AskAsync(new ListDayworksForProject(projectId), CancellationToken.None).GetAwaiter().GetResult();

    public Daywork LogDaywork(Daywork daywork)
    {
        _ = SendThenNotify(new LogDaywork(daywork.ProjectId, daywork.WorkedOn, daywork.SubcontractorReference, daywork.Description, daywork.InstructedBy, daywork.Hours, daywork.HourlyRate, daywork.LabourCost, daywork.PlantCost, daywork.MaterialsCost, daywork.UpliftPercent, daywork.ChargeableAmount));
        return daywork;
    }

    public IReadOnlyList<ContraCharge> ContraChargesFor(string projectId) =>
        queries.AskAsync(new ListContraChargesForProject(projectId), CancellationToken.None).GetAwaiter().GetResult();

    public ContraCharge RecordContraCharge(ContraCharge contraCharge)
    {
        _ = SendThenNotify(new RecordContraCharge(contraCharge.ProjectId, contraCharge.SubcontractorReference, contraCharge.RaisedOn, contraCharge.Description, contraCharge.Category, contraCharge.Amount, contraCharge.Status, contraCharge.RecoveredAmount));
        return contraCharge;
    }

    public IReadOnlyList<SubcontractorRetention> RetentionsFor(string projectId) =>
        queries.AskAsync(new ListSubcontractorRetentionsForProject(projectId), CancellationToken.None).GetAwaiter().GetResult();

    public SubcontractorRetention RecordRetention(SubcontractorRetention retention)
    {
        _ = SendThenNotify(new RecordSubcontractorRetention(retention.ProjectId, retention.SubcontractorReference, retention.CertifiedAmount, retention.RetentionPercent, retention.FirstReleasedAmount, retention.FinalReleasedAmount));
        return retention;
    }

    private async Task SendThenNotify<TResult>(Jewel.JPMS.Contracts.Cqrs.ICommand<TResult> command)
    {
        await commands.SendAsync(command, CancellationToken.None);
        OnChange?.Invoke();
    }
}
