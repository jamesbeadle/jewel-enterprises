namespace Jewel.JPMS.Models;

public sealed record Project(
    string ProjectId,
    string Reference,
    string Name,
    string ClientName,
    Organisation Organisation,
    ProjectStage Stage,
    string ProjectManagerEmail,
    DateTimeOffset CreatedAt,
    // The party this project corresponds with: a client account directly, or an architect acting
    // on a client's behalf (OnBehalfOfClientId optionally records that client). Null PartyId until
    // assigned; ClientName above stays the free-text display name shown on documents.
    PartyKind PartyKind = PartyKind.Client,
    string? PartyId = null,
    string? OnBehalfOfClientId = null,
    // Site address — used to find local subcontractors near the project (and anywhere else the
    // site's location matters). Free-text; Town + Postcode are what the local search needs.
    string AddressLine = "",
    string Town = "",
    string Postcode = "",
    // The project's option in Xero's "Sites" tracking category, exactly as named in Xero.
    // Explicit mapping: the Xero write-back (cost-code confirmation + invoice approval)
    // stamps it on every allocated line and refuses to run while it's unset.
    string? XeroSiteName = null,
    // When the next valuation is expected. Set manually via SetNextValuationDate; informational.
    DateTimeOffset? NextExpectedValuationDate = null,
    // The FD's per-project forecast assumption (2026-08-13): roughly how much the architect is
    // expected to certify per valuation month. Null means "no view" — the Cash Forecast spreads
    // left-to-claim evenly to practical completion; set, it claims at this rate until the money
    // runs out. Set via SetExpectedMonthlyValuation (the Cash Forecast page's inline editor);
    // forecasting only — it never touches valuations or invoices.
    decimal? ExpectedMonthlyValuation = null,
    // The Xero customer the project's sales invoices are raised on (2026-09-10, the accountant's
    // ask: the raise matched the client by NAME and created a duplicate contact on a miss). An
    // explicit mapping like XeroSiteName — Xero's ContactID and the name as Xero holds it — set
    // from the Xero contacts list in Project settings; Raise in Xero is blocked until it is set.
    string? XeroContactId = null,
    string? XeroContactName = null);
