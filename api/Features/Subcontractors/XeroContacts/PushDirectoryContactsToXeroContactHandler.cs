using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.XeroContacts;

/// <summary>
/// Writes the record's contacts onto its linked Xero contact — planned by the same rule the
/// preview showed, read fresh from Xero at the moment of the write so nothing is overwritten
/// blind. Xero's refusal (a missing accounting.contacts scope above all) comes back as the
/// answer, not a 500. Audited with who pushed and what was written.
/// </summary>
public sealed class PushDirectoryContactsToXeroContactHandler : ICommandHandler<PushDirectoryContactsToXeroContact, XeroContactPushOutcome>
{
    private readonly JpmsContext context;
    private readonly IXeroClient xero;
    private readonly AuditTrail audit;

    public PushDirectoryContactsToXeroContactHandler(JpmsContext context, IXeroClient xero, AuditTrail audit)
    {
        this.context = context; this.xero = xero; this.audit = audit;
    }

    public async Task<XeroContactPushOutcome> HandleAsync(PushDirectoryContactsToXeroContact command, CancellationToken cancellationToken)
    {
        var push = await XeroContactPushContext.LoadAsync(context, xero, command.SubcontractorId, cancellationToken);
        var (after, _) = XeroContactPushPlanner.Plan(push.Record, push.Contacts, push.Now);

        var written = await xero.SetContactPeopleAsync(after, cancellationToken);
        if (!written.Succeeded)
            throw new InvalidOperationException(written.Error ?? "Xero didn't accept the contact's people.");

        await audit.WriteAsync(
            AuditEventType.DirectoryContactsPushedToXero,
            $"{push.Record.CompanyName}: contacts pushed to Xero contact {push.Link.XeroContactName} — "
            + $"primary {after.PrimaryPerson?.Name ?? "(unchanged)"}, additional {Describe(after)}.",
            cancellationToken: cancellationToken);

        return new XeroContactPushOutcome(push.Link.XeroContactName, after);
    }

    private static string Describe(Contracts.Xero.XeroContactPeople people) =>
        people.AdditionalPersons.Count == 0 ? "none" : string.Join(", ", people.AdditionalPersons.Select(person => person.Name));
}
