using Jewel.JPMS.Contracts.Projects;

namespace Jewel.JPMS.Api.Features.Projects.Commands;

public sealed class UpdateProjectDetailsHandler
    : ICommandHandler<UpdateProjectDetails, Project>
{
    private readonly JpmsContext context;

    public UpdateProjectDetailsHandler(JpmsContext context) { this.context = context; }

    public async Task<Project> HandleAsync(UpdateProjectDetails command, CancellationToken cancellationToken)
    {
        var entity = await context.Projects.FindAsync(new object[] { command.ProjectId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        entity.Reference = command.Reference;
        entity.Name = command.Name;
        entity.ClientName = command.ClientName;
        entity.Organisation = (int)command.Organisation;
        entity.Stage = (int)command.Stage;
        entity.ProjectManagerEmail = command.ProjectManagerEmail;
        entity.AddressLine = (command.AddressLine ?? "").Trim();
        entity.Town = (command.Town ?? "").Trim();
        entity.Postcode = (command.Postcode ?? "").Trim();
        entity.XeroSiteName = string.IsNullOrWhiteSpace(command.XeroSiteName) ? null : command.XeroSiteName.Trim();
        // The Xero contact mapping travels as a pair: no id, no mapping (a name without an id is
        // exactly the by-name matching the raise no longer does).
        entity.XeroContactId = string.IsNullOrWhiteSpace(command.XeroContactId) ? null : command.XeroContactId.Trim();
        entity.XeroContactName = entity.XeroContactId is null || string.IsNullOrWhiteSpace(command.XeroContactName)
            ? null
            : command.XeroContactName.Trim();

        // The party this project corresponds with (client directly, or architect on a client's
        // behalf). A null/empty PartyId clears the assignment.
        if (string.IsNullOrWhiteSpace(command.PartyId))
        {
            entity.PartyKind = (int)PartyKind.Client;
            entity.PartyId = null;
            entity.OnBehalfOfClientId = null;
        }
        else
        {
            entity.PartyKind = (int)command.PartyKind;
            entity.PartyId = command.PartyId;
            entity.OnBehalfOfClientId = command.PartyKind == PartyKind.Architect
                ? (string.IsNullOrWhiteSpace(command.OnBehalfOfClientId) ? null : command.OnBehalfOfClientId)
                : null;
        }

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
