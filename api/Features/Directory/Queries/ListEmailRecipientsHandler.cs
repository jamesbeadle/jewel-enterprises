using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Api.Features.Directory.Queries;

/// <summary>
/// Flattens every email address the directory knows into one address book for the composers'
/// recipient pickers. Sources, in priority order (the first source to claim an address keeps it —
/// a person who is both a portal user and a client contact appears once, as staff):
///   1. Portal users (staff) — DirectoryUsers, revoked excluded.
///   2. Party contacts — the people on client accounts and architect practices (PartyContacts),
///      captioned with the party's name; the legacy Client.PrimaryContactEmail /
///      Architect.ContactEmail lines fill in for parties that predate party contacts.
///   3. Project contact sheets — ad-hoc rows only (PartyContactId null); linked rows are the party
///      contact again under a per-project routing.
///   4. Company directory — each record's primary contact line plus its CompanyContacts.
///   5. Labour workers with a contact email.
/// Read-only and fetched whole: the client filters as the user types.
/// </summary>
public sealed class ListEmailRecipientsHandler : IQueryHandler<ListEmailRecipients, IReadOnlyList<EmailRecipient>>
{
    private readonly JpmsContext context;
    public ListEmailRecipientsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<EmailRecipient>> HandleAsync(ListEmailRecipients query, CancellationToken cancellationToken)
    {
        var book = new Dictionary<string, EmailRecipient>(StringComparer.OrdinalIgnoreCase);

        void Add(string? name, string? email, string? organisation, EmailRecipientKind kind, string? detail = null)
        {
            var address = (email ?? "").Trim();
            if (address.Length == 0 || !address.Contains('@')) return;
            if (book.ContainsKey(address)) return;
            var display = string.IsNullOrWhiteSpace(name) ? address : name.Trim();
            book[address] = new EmailRecipient(
                display, address,
                string.IsNullOrWhiteSpace(organisation) ? null : organisation.Trim(),
                kind,
                string.IsNullOrWhiteSpace(detail) ? null : detail.Trim());
        }

        // 1. Staff.
        var staff = await context.DirectoryUsers.AsNoTracking()
            .Where(user => user.RevokedAt == null)
            .Select(user => new { user.Email, user.DisplayName })
            .ToListAsync(cancellationToken);
        foreach (var user in staff) Add(user.DisplayName, user.Email, "Jewel Bespoke Build", EmailRecipientKind.Staff);

        // 2. Party contacts (clients + architects), captioned with the party's name.
        var clients = await context.Clients.AsNoTracking()
            .Select(client => new { client.ClientId, client.Name, client.PrimaryContactName, client.PrimaryContactEmail })
            .ToListAsync(cancellationToken);
        var architects = await context.Architects.AsNoTracking()
            .Select(architect => new { architect.ArchitectId, architect.Name, architect.ContactName, architect.ContactEmail })
            .ToListAsync(cancellationToken);
        var clientNames = clients.ToDictionary(client => client.ClientId, client => client.Name);
        var architectNames = architects.ToDictionary(architect => architect.ArchitectId, architect => architect.Name);

        var partyContacts = await context.PartyContacts.AsNoTracking()
            .OrderByDescending(contact => contact.IsPrimary).ThenBy(contact => contact.Name)
            .Select(contact => new { contact.PartyKind, contact.PartyId, contact.Name, contact.Email, contact.JobTitle })
            .ToListAsync(cancellationToken);
        foreach (var contact in partyContacts)
        {
            var isClient = contact.PartyKind == (int)PartyKind.Client;
            var organisation = isClient
                ? clientNames.GetValueOrDefault(contact.PartyId)
                : architectNames.GetValueOrDefault(contact.PartyId);
            Add(contact.Name, contact.Email, organisation,
                isClient ? EmailRecipientKind.Client : EmailRecipientKind.Architect, contact.JobTitle);
        }
        foreach (var client in clients)
            Add(client.PrimaryContactName, client.PrimaryContactEmail, client.Name, EmailRecipientKind.Client);
        foreach (var architect in architects)
            Add(architect.ContactName, architect.ContactEmail, architect.Name, EmailRecipientKind.Architect);

        // 3. Ad-hoc project contacts.
        var projectContacts = await context.ProjectContacts.AsNoTracking()
            .Where(contact => contact.PartyContactId == null)
            .OrderBy(contact => contact.Name)
            .Select(contact => new { contact.Name, contact.Email, contact.Organisation, contact.Role })
            .ToListAsync(cancellationToken);
        foreach (var contact in projectContacts)
            Add(contact.Name, contact.Email, contact.Organisation, EmailRecipientKind.ProjectContact,
                ((ProjectContactRole)contact.Role).DisplayName());

        // 4. Company directory — primary line, then the extra contacts.
        var companies = await context.Subcontractors.AsNoTracking()
            .OrderBy(company => company.CompanyName)
            .Select(company => new { company.SubcontractorId, company.CompanyName, company.ContactName, company.ContactEmail, company.Category })
            .ToListAsync(cancellationToken);
        var companyNames = companies.ToDictionary(company => company.SubcontractorId, company => company.CompanyName);
        foreach (var company in companies)
            Add(company.ContactName, company.ContactEmail, company.CompanyName, EmailRecipientKind.Company,
                ((DirectoryCategory)company.Category).ToString());

        var companyContacts = await context.CompanyContacts.AsNoTracking()
            .OrderBy(contact => contact.Name)
            .Select(contact => new { contact.SubcontractorId, contact.Name, contact.Email })
            .ToListAsync(cancellationToken);
        foreach (var contact in companyContacts)
            Add(contact.Name, contact.Email, companyNames.GetValueOrDefault(contact.SubcontractorId),
                EmailRecipientKind.Company);

        // 5. Workers.
        var workers = await context.Workers.AsNoTracking()
            .Where(worker => worker.IsActive && worker.ContactEmail != "")
            .OrderBy(worker => worker.Name)
            .Select(worker => new { worker.Name, worker.ContactEmail, worker.SubcontractorId })
            .ToListAsync(cancellationToken);
        foreach (var worker in workers)
            Add(worker.Name, worker.ContactEmail,
                worker.SubcontractorId is null ? null : companyNames.GetValueOrDefault(worker.SubcontractorId),
                EmailRecipientKind.Worker, "Worker");

        return book.Values
            .OrderBy(recipient => recipient.Kind == EmailRecipientKind.Staff ? 0 : 1)
            .ThenBy(recipient => recipient.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(recipient => recipient.Email, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }
}
