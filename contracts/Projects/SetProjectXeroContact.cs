using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Projects;

// Maps (or clears, with null) the Xero customer a project's sales invoices are raised on — the
// one field, without round-tripping the full UpdateProjectDetails payload. Only the Xero
// ContactID is given: the handler reads the contact from Xero and stores the name Xero holds,
// so a mapping can never point at a contact that does not exist or carry a made-up name.
// Raise in Xero is blocked until this is set (2026-09-10, the accountant's ask).
public sealed record SetProjectXeroContact(
    string ProjectId,
    string? XeroContactId) : ICommand<Project>;
