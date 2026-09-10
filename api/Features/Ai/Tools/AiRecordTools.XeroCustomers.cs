using Jewel.JPMS.Contracts.Xero;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The Xero contact list as a connector read (2026-09-10): the same ListXeroSuppliers query the
/// directory's Import-from-Xero modal and Project settings use, filtered to the contacts a sales
/// invoice can be raised on. The model reads the names and proposes the match for a project the
/// way a person does — "Ravenswood Ave" is "64 Ravenswood Avenue" — then, with the user's yes,
/// set_project_xero_contact stores it. Nothing here writes.
/// </summary>
internal static partial class AiRecordTools
{
    private static readonly RoleSet XeroCustomerReaders =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager);

    private static IEnumerable<AiTool> XeroCustomerTools() => new AiTool[]
    {
        new(
            "list_xero_customers",
            "The contacts Xero holds that a sales invoice can be raised on — every active contact "
            + "flagged as a customer, plus contacts Xero has not flagged either way yet (a brand-new "
            + "contact carries neither flag); supplier-only contacts are left out. Each row is the "
            + "Xero contactId and the name exactly as Xero holds it, with town and postcode where "
            + "Xero has them. This is what Project settings' Xero-contact picker reads. Use it to "
            + "find the customer a project's invoices go to: match on the address or client the "
            + "project names — spelling and abbreviations differ (\"Ravenswood Ave\" IS \"64 "
            + "Ravenswood Avenue\") — propose the match to the user, and on their yes call "
            + "set_project_xero_contact with the contactId. Pass search to narrow by name.",
            AiToolSchema.Object(
                ("search", "string", "Optional text matched against contact names, towns and postcodes (case-insensitive).", false),
                ("includeSuppliers", "boolean", "true also returns supplier-only contacts. Default false.", false)),
            AiToolKind.Read,
            XeroCustomerReaders,
            async (context, input, ct) =>
            {
                var snapshot = await context.Services
                    .GetRequiredService<IQueryHandler<ListXeroSuppliers, XeroSuppliersSnapshot>>()
                    .HandleAsync(new ListXeroSuppliers(), ct);

                if (!snapshot.IsConfigured) return Fail("Xero is not connected on this portal.");
                if (snapshot.Error is not null) return Fail($"Xero could not be read: {snapshot.Error}");

                var search = AiToolSchema.Text(input, "search")?.Trim();
                var includeSuppliers = AiToolSchema.Flag(input, "includeSuppliers") ?? false;

                var rows = snapshot.Suppliers
                    .Where(contact => includeSuppliers || contact.IsCustomer || !contact.IsSupplier)
                    .Where(contact => string.IsNullOrEmpty(search)
                        || contact.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                        || contact.Town.Contains(search, StringComparison.OrdinalIgnoreCase)
                        || contact.Postcode.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(contact => contact.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(contact => new
                    {
                        contactId = contact.ContactId,
                        name = contact.Name,
                        town = contact.Town,
                        postcode = contact.Postcode,
                        isCustomer = contact.IsCustomer,
                        isSupplier = contact.IsSupplier
                    })
                    .ToList();

                return Serialise(new
                {
                    ok = true,
                    fetchedAtUtc = snapshot.FetchedAtUtc,
                    truncated = snapshot.Truncated,
                    count = rows.Count,
                    contacts = rows,
                    note = "Match the project's client/address to a name here by reading, not by exact text; "
                           + "show the user the proposed contact (name and town) and take their yes, then "
                           + "set_project_xero_contact(projectId, xeroContactId). The portal re-reads the "
                           + "contact from Xero and stores Xero's own name."
                });
            })
    };
}
