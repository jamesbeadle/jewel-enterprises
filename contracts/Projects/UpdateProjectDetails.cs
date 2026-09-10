using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Projects;

public sealed record UpdateProjectDetails(
    string ProjectId,
    string Reference,
    string Name,
    string ClientName,
    Organisation Organisation,
    ProjectStage Stage,
    string ProjectManagerEmail,
    // The party this project corresponds with (null PartyId clears the assignment): a client
    // account directly, or an architect acting on a client's behalf — where project emails
    // (RFIs and other request documents) are addressed.
    PartyKind PartyKind = PartyKind.Client,
    string? PartyId = null,
    string? OnBehalfOfClientId = null,
    // Site address — Town + Postcode drive the "find local subcontractors" search.
    string AddressLine = "",
    string Town = "",
    string Postcode = "",
    // The project's option in Xero's "Sites" tracking category (exact name). Null/blank clears it.
    string? XeroSiteName = null,
    // The Xero customer the project's sales invoices are raised on — Xero's ContactID and the
    // name as Xero holds it (2026-09-10). Same full-record convention as XeroSiteName: null/blank
    // clears the mapping, so carry the current values forward when they should not change.
    string? XeroContactId = null,
    string? XeroContactName = null) : ICommand<Project>;
