using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class RecordCisVerificationHandler
    : ICommandHandler<RecordCisVerification, Subcontractor>
{
    private readonly JpmsContext context;

    public RecordCisVerificationHandler(JpmsContext context) { this.context = context; }

    public async Task<Subcontractor> HandleAsync(RecordCisVerification command, CancellationToken cancellationToken)
    {
        var entity = await context.Subcontractors.FindAsync(new object[] { command.SubcontractorId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Subcontractor {command.SubcontractorId} not found.");

        entity.CisStatus = command.CisStatus.Trim();
        entity.CisVerificationNumber = (command.CisVerificationNumber ?? "").Trim().ToUpperInvariant();
        entity.CisVerifiedOn = command.CisVerifiedOn;
        await context.SaveChangesAsync(cancellationToken);

        return entity.ToModel(await context.TradesForAsync(entity.SubcontractorId, cancellationToken));
    }
}
