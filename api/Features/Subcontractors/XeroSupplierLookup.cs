using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Subcontractors;

/// <summary>
/// Finds one Xero contact by id for the directory's link and import commands. The cached snapshot
/// is normally fresh (the modal just listed it); one forced re-read covers a supplier created in
/// Xero moments ago. Not configured / Xero said no / not found are answered as guards the endpoint
/// turns into a message, never a 500.
/// </summary>
public sealed class XeroSupplierLookup
{
    private readonly IXeroClient xero;

    public XeroSupplierLookup(IXeroClient xero) { this.xero = xero; }

    public async Task<XeroSupplier> FindAsync(string xeroContactId, CancellationToken cancellationToken)
    {
        var snapshot = await xero.GetSuppliersAsync(force: false, cancellationToken);
        var supplier = Match(snapshot, xeroContactId);
        if (supplier is null && snapshot.IsConfigured && snapshot.Error is null)
        {
            snapshot = await xero.GetSuppliersAsync(force: true, cancellationToken);
            supplier = Match(snapshot, xeroContactId);
        }

        if (!snapshot.IsConfigured)
            throw new InvalidOperationException("Xero isn't connected — add the Xero__ClientId / Xero__ClientSecret app settings.");
        if (snapshot.Error is not null)
            throw new InvalidOperationException(snapshot.Error);
        return supplier
            ?? throw new InvalidOperationException("That contact wasn't found in Xero. Refresh the list and try again.");
    }

    private static XeroSupplier? Match(XeroSuppliersSnapshot snapshot, string xeroContactId) =>
        snapshot.Suppliers.FirstOrDefault(supplier =>
            string.Equals(supplier.ContactId, xeroContactId, StringComparison.OrdinalIgnoreCase));
}
