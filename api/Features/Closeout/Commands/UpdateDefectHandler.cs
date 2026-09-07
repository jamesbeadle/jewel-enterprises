using Jewel.JPMS.Contracts.Closeout;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

public sealed class UpdateDefectHandler : ICommandHandler<UpdateDefect, Defect>
{
    private readonly JpmsContext context;
    public UpdateDefectHandler(JpmsContext context) { this.context = context; }

    public async Task<Defect> HandleAsync(UpdateDefect command, CancellationToken cancellationToken)
    {
        var entity = await context.Defects.FindAsync(new object[] { command.DefectId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Defect {command.DefectId} not found.");
        entity.Description = command.Description;
        entity.Location = command.Location;
        entity.AssignedToEmail = command.AssignedToEmail ?? "";
        // Written as posted: an explicit id is checked against the directory; null with a
        // free-typed address that matches a directory contact promotes to that record; null with
        // nothing to match clears the supplier.
        entity.SubcontractorId = await DefectSupplierLookup.ResolveAsync(
            context, command.SubcontractorId, entity.AssignedToEmail, cancellationToken);
        var wasResolved = (DefectStatus)entity.Status == DefectStatus.Resolved || (DefectStatus)entity.Status == DefectStatus.Verified;
        entity.Status = (int)command.Status;
        if (!wasResolved && (command.Status == DefectStatus.Resolved || command.Status == DefectStatus.Verified))
            entity.ResolvedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel(await DefectSupplierLookup.OneAsync(context, entity.SubcontractorId, cancellationToken));
    }
}
