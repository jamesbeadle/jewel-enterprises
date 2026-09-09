using Jewel.JPMS.Api.Features.Subcontractors;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.RecordLinks;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiRecordTools
{
    private static IEnumerable<AiTool> DirectoryTools()
    {
        var readers = JpmsRoleSets.AllInternal;

        return new AiTool[]
        {
            new(
                "search_directory",
                "Finds company records in the subcontractor/supplier directory: search by name (or "
                + "contact name/email), optionally narrowed to a category. Returns each match's "
                + "subcontractorId — the id update_subcontractor and the procurement actions need — "
                + "with its category, trades (ids and names, exactly what update_subcontractor must "
                + "send back in full), primary contact, postal address, CIS status, payment terms, "
                + "whether it is linked to a Xero contact (and which — xeroLinks carries the Xero "
                + "ContactID and name), and whether it is still a tender-only prospect "
                + "(promote_subcontractor_to_directory makes those permanent). Call this "
                + "BEFORE update_subcontractor or add_subcontractor_to_directory — never guess an "
                + "id, and never create a record before checking it isn't already here.",
                AiToolSchema.Object(
                    ("search", "string",
                        "Text matched against company name, contact name and contact email — "
                        + "\"Sussex Tiling\", \"jo@acme\". Left out, the directory lists from the "
                        + "top (capped, so search when you know the name).", false),
                    ("category", "string",
                        "Optional filter: Subcontractor, Client, Architect, Supplier or Other.", false)),
                AiToolKind.Read,
                // Mirrors ListSubcontractorsEndpoint.InternalRolesThatMayListDirectory (the full
                // directory with contact details is internal-only; external sessions get their own
                // scoped views), plus Role.Admin named explicitly per the authorisation convention.
                RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector,
                    JpmsRoles.ProjectManager, JpmsRoles.Estimator, JpmsRoles.SiteManager,
                    JpmsRoles.HealthAndSafetyLead, JpmsRoles.OfficeComplianceCoordinator,
                    JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing, JpmsRoles.Foreman),
                async (context, input, ct) =>
                {
                    var search = AiToolSchema.Text(input, "search");
                    var categoryText = AiToolSchema.Text(input, "category");

                    var query = context.Db.Subcontractors.AsNoTracking();
                    if (!string.IsNullOrWhiteSpace(search))
                        query = query.Where(row => row.CompanyName.Contains(search)
                            || row.ContactName.Contains(search)
                            || row.ContactEmail.Contains(search));
                    if (!string.IsNullOrWhiteSpace(categoryText))
                    {
                        if (!Enum.TryParse<DirectoryCategory>(categoryText, ignoreCase: true, out var category))
                            return Fail("category must be Subcontractor, Client, Architect, Supplier or Other.");
                        query = query.Where(row => row.Category == (int)category);
                    }

                    // Capped like every listing tool — the fix for a truncated result is a better
                    // search term, not a bigger dump.
                    const int cap = 25;
                    var rows = await query.OrderBy(row => row.CompanyName)
                        .Take(cap + 1).ToListAsync(ct);
                    var truncated = rows.Count > cap;
                    if (truncated) rows = rows.Take(cap).ToList();

                    var ids = rows.Select(row => row.SubcontractorId).ToList();
                    var tradeLinks = await (
                        from link in context.Db.SubcontractorTrades.AsNoTracking()
                        join trade in context.Db.Trades.AsNoTracking() on link.TradeId equals trade.TradeId
                        where ids.Contains(link.SubcontractorId)
                        select new { link.SubcontractorId, trade.TradeId, trade.Name })
                        .ToListAsync(ct);
                    var xeroLinks = await DirectoryXeroLinks.ByRecordAsync(context.Db, ct);

                    var companies = rows.Select(row => new
                    {
                        subcontractorId = row.SubcontractorId,
                        companyName = row.CompanyName,
                        category = ((DirectoryCategory)row.Category).ToString(),
                        isProspect = row.IsProspect,
                        trades = tradeLinks.Where(link => link.SubcontractorId == row.SubcontractorId)
                            .Select(link => new { tradeId = link.TradeId, name = link.Name })
                            .OrderBy(trade => trade.name).ToList(),
                        contactName = row.ContactName,
                        contactEmail = row.ContactEmail,
                        contactPhone = row.ContactPhone,
                        mobileNumber = row.MobileNumber,
                        address = new { row.AddressLine, row.Town, row.County, row.Postcode },
                        cisStatus = row.CisStatus,
                        cisVerificationNumber = row.CisVerificationNumber,
                        cisVerifiedOn = row.CisVerifiedOn,
                        paymentTermsDays = row.PaymentTermsDays,
                        xeroLinked = xeroLinks.ContainsKey(row.SubcontractorId),
                        xeroLinks = xeroLinks.TryGetValue(row.SubcontractorId, out var links)
                            ? links.Select(link => new { link.XeroContactId, link.XeroContactName, link.LinkedAt, link.LinkedByEmail }).ToList()
                            : new()
                    }).ToList();

                    return Serialise(new
                    {
                        ok = true,
                        count = companies.Count,
                        truncated,
                        companies,
                        note = "subcontractorId is what update_subcontractor takes; send its FULL "
                               + "trades list back when updating — removing the last trade is "
                               + "refused. The address here is what the purchase order prints. "
                               + "Xero-linked records copied their address from Xero at import "
                               + "only — later Xero edits never flow back, so the directory is "
                               + "corrected here."
                               + (truncated ? " More records matched than shown — narrow the search." : "")
                    });
                }),

            new(
                "list_unlinked_directory_records",
                "Lists the directory records that are NOT linked to a Xero contact — every company "
                + "in the directory proper (tender-only prospects excluded) with no Xero link — each "
                + "with the unlinked Xero contacts whose name matches it, using the same name rule "
                + "as worker-to-company linking (normalised equality, or containment either way "
                + "when the shorter name still has two words). A match is a SUGGESTION: show the "
                + "user each pairing and take their yes, then call "
                + "link_directory_record_to_xero_contact once per confirmed pair. Records with no "
                + "suggestion are either not yet in Xero or named too differently to match — "
                + "search_directory + the Xero contact name settle those by hand. Reads Xero's "
                + "cached contact list.",
                AiToolSchema.Object(
                    ("search", "string",
                        "Optional text matched against the record's company name, to narrow to one "
                        + "company (\"JP Air\").", false),
                    ("onlyWithSuggestions", "boolean",
                        "true returns only records that have at least one matching Xero contact — "
                        + "the batch to confirm; false (default) lists every unlinked record.", false)),
                AiToolKind.Read,
                RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector,
                    JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing),
                async (context, input, ct) =>
                {
                    var search = AiToolSchema.Text(input, "search");
                    var onlyWithSuggestions = AiToolSchema.Flag(input, "onlyWithSuggestions") ?? false;

                    var xero = context.Services.GetRequiredService<IXeroClient>();
                    var snapshot = await xero.GetSuppliersAsync(force: false, ct);
                    if (!snapshot.IsConfigured) return Fail("Xero isn't connected — the Xero__ClientId / Xero__ClientSecret app settings are missing.");
                    if (snapshot.Error is not null) return Fail(snapshot.Error);

                    var linkedContactIds = (await context.Db.SubcontractorXeroLinks.AsNoTracking()
                        .Select(link => link.XeroContactId).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var unlinkedSuppliers = snapshot.Suppliers
                        .Where(supplier => !linkedContactIds.Contains(supplier.ContactId))
                        .ToList();

                    var records = await DirectoryXeroMatcher.UnlinkedRecordsAsync(context.Db, ct);
                    if (!string.IsNullOrWhiteSpace(search))
                        records = records.Where(record => record.CompanyName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

                    var unlinked = records
                        .Select(record => new
                        {
                            subcontractorId = record.SubcontractorId,
                            companyName = record.CompanyName,
                            category = ((DirectoryCategory)record.Category).ToString(),
                            suggestedXeroContacts = DirectoryXeroMatcher.SuppliersMatching(record.CompanyName, unlinkedSuppliers)
                                .Select(supplier => new { xeroContactId = supplier.ContactId, name = supplier.Name, emailAddress = supplier.EmailAddress, town = supplier.Town })
                                .ToList()
                        })
                        .Where(record => !onlyWithSuggestions || record.suggestedXeroContacts.Count > 0)
                        .ToList();

                    return Serialise(new
                    {
                        ok = true,
                        unlinkedCount = records.Count,
                        withSuggestionsCount = unlinked.Count(record => record.suggestedXeroContacts.Count > 0),
                        records = unlinked,
                        xeroContactsReadAt = snapshot.FetchedAtUtc,
                        note = "Each suggestion is a name match, not a confirmed pairing. Confirm every "
                               + "pair with the user, then link_directory_record_to_xero_contact "
                               + "(subcontractorId + xeroContactId) once per pair — one call, no import, "
                               + "no consolidation."
                               + (snapshot.Truncated ? " Xero's contact list was cut at the page cap, so a contact may be missing from the suggestions." : "")
                    });
                }),
        };
    }
}
