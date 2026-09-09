using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero;

public sealed partial class XeroClient
{
    public async Task<XeroSuppliersSnapshot> GetSuppliersAsync(bool force, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            return XeroSuppliersSnapshot.NotConfigured();

        await _suppliersLock.WaitAsync(ct);
        try
        {
            if (!force && CachedSuppliersAreFresh)
                return _cachedSuppliers!;

            var snapshot = await FetchSuppliersAsync(ct);

            // Only successful reads replace the cache — a transient failure shouldn't evict
            // good data, but it is still returned so the user sees what went wrong.
            if (snapshot.Error is null)
            {
                _cachedSuppliers = snapshot;
                _cachedSuppliersAt = DateTimeOffset.UtcNow;
            }
            return snapshot;
        }
        finally
        {
            _suppliersLock.Release();
        }
    }

    private bool CachedSuppliersAreFresh =>
        _cachedSuppliers is not null
        && DateTimeOffset.UtcNow < _cachedSuppliersAt.AddMinutes(_options.CacheMinutes);

    private async Task<XeroSuppliersSnapshot> FetchSuppliersAsync(CancellationToken ct)
    {
        string token;
        try
        {
            token = await GetAccessTokenAsync(ct);
        }
        catch (XeroCallFailedException tokenFailure)
        {
            return XeroSuppliersSnapshot.Failed(tokenFailure.Message);
        }

        var suppliers = new List<XeroSupplier>();
        var truncated = false;
        try
        {
            // EVERY active contact, not where=IsSupplier==true: Xero only sets IsSupplier once a
            // contact has had a bill, so the filter would hide a supplier created moments ago and
            // never yet billed. The flags come back per row and the modal narrows client-side
            // (customer-only contacts hidden by default). includeArchived is deliberately not
            // sent. Paged like the invoices read.
            for (var page = 1; ; page++)
            {
                if (page > _options.MaxPages) { truncated = true; break; }

                var url = $"{ContactsUrl}?page={page}&order={Uri.EscapeDataString("Name")}";
                using var doc = await GetJsonAsync(token, url, "contacts", ct);

                if (!doc.RootElement.TryGetProperty("Contacts", out var contacts) || contacts.ValueKind != JsonValueKind.Array)
                    break;

                var pageOfSuppliers = contacts.EnumerateArray().Select(ReadSupplier).ToList();
                suppliers.AddRange(pageOfSuppliers);
                if (pageOfSuppliers.Count < PageSize) break; // Short page — no more to fetch.
            }
        }
        catch (XeroCallFailedException callFailure)
        {
            return XeroSuppliersSnapshot.Failed(callFailure.Message);
        }

        return new XeroSuppliersSnapshot(true, null, DateTimeOffset.UtcNow, truncated, suppliers);
    }

    // -- tracking categories: the Cost codes page's Xero sites / Xero cost codes tabs ---

    public async Task<XeroTrackingCategoriesSnapshot> GetTrackingCategoriesSnapshotAsync(bool force, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            return XeroTrackingCategoriesSnapshot.NotConfigured();

        await _trackingCategoriesLock.WaitAsync(ct);
        try
        {
            if (!force && CachedTrackingCategoriesAreFresh)
                return _cachedTrackingCategories!;

            var snapshot = await FetchTrackingCategoriesSnapshotAsync(ct);

            // Only successful reads replace the cache — a transient failure (429 above all)
            // shouldn't evict good data, but it is still returned so the user sees what went wrong.
            if (snapshot.Error is null)
            {
                _cachedTrackingCategories = snapshot;
                _cachedTrackingCategoriesAt = DateTimeOffset.UtcNow;
            }
            return snapshot;
        }
        finally
        {
            _trackingCategoriesLock.Release();
        }
    }

    private bool CachedTrackingCategoriesAreFresh =>
        _cachedTrackingCategories is not null
        && DateTimeOffset.UtcNow < _cachedTrackingCategoriesAt.AddMinutes(_options.CacheMinutes);

    private async Task<XeroTrackingCategoriesSnapshot> FetchTrackingCategoriesSnapshotAsync(CancellationToken ct)
    {
        string token;
        try
        {
            token = await GetAccessTokenAsync(ct);
        }
        catch (XeroCallFailedException tokenFailure)
        {
            return XeroTrackingCategoriesSnapshot.Failed(tokenFailure.Message);
        }

        try
        {
            // includeArchived: a retired option's exact name still explains historical tracking,
            // and hiding it here would make "why doesn't this match?" harder, not easier. The
            // UI flags archived rows instead. Unlike GetTrackingCategoriesAsync (the write-back's
            // lookup) this read is diagnostic: EVERY category comes back, and a missing Sites /
            // Cost Code category is the UI's message to render, not an exception.
            using var doc = await GetJsonAsync(
                token, $"{TrackingCategoriesUrl}?includeArchived=true", "tracking categories", ct);

            var categories = new List<XeroTrackingCategory>();
            if (doc.RootElement.TryGetProperty("TrackingCategories", out var trackingCategories)
                && trackingCategories.ValueKind == JsonValueKind.Array)
            {
                foreach (var category in trackingCategories.EnumerateArray())
                {
                    var name = StringOf(category, "Name");
                    var id = StringOf(category, "TrackingCategoryID");
                    if (name is null || id is null) continue;

                    var options = new List<XeroTrackingOption>();
                    if (category.TryGetProperty("Options", out var optionElements) && optionElements.ValueKind == JsonValueKind.Array)
                        foreach (var option in optionElements.EnumerateArray())
                            if (StringOf(option, "Name") is { } optionName)
                                options.Add(new XeroTrackingOption(
                                    StringOf(option, "TrackingOptionID") ?? "",
                                    optionName,
                                    StringOf(option, "Status") ?? "ACTIVE"));

                    categories.Add(new XeroTrackingCategory(
                        id,
                        name,
                        StringOf(category, "Status") ?? "ACTIVE",
                        options,
                        IsSiteCategory: Normalise(name) == Normalise(_options.SiteTrackingCategory),
                        IsCostCodeCategory: Normalise(name) == Normalise(_options.CostCodeTrackingCategory)));
                }
            }

            return new XeroTrackingCategoriesSnapshot(true, null, DateTimeOffset.UtcNow, categories);
        }
        catch (XeroCallFailedException callFailure)
        {
            return XeroTrackingCategoriesSnapshot.Failed(
                "Couldn't read Xero's tracking categories. If Xero answered 403, the custom "
                + "connection needs the accounting.settings scope; a 429 means Xero's rate "
                + "limit — wait a minute and refresh. " + callFailure.Message);
        }
    }

    private static XeroSupplier ReadSupplier(JsonElement contact) => new(
        ContactId: StringOf(contact, "ContactID") ?? Guid.NewGuid().ToString(),
        Name: StringOf(contact, "Name") ?? "",
        EmailAddress: StringOf(contact, "EmailAddress") ?? "",
        Phone: PhoneOf(contact, "DEFAULT") ?? PhoneOf(contact, "DDI") ?? "",
        Mobile: PhoneOf(contact, "MOBILE") ?? "",
        Town: AddressPartOf(contact, "City"),
        County: AddressPartOf(contact, "Region"),
        AddressLine: StreetOf(contact),
        Postcode: AddressPartOf(contact, "PostalCode"),
        ContactPersons: ReadContactPersons(contact),
        IsSupplier: BoolOf(contact, "IsSupplier"),
        IsCustomer: BoolOf(contact, "IsCustomer"),
        PrimaryPersonName: FullNameOf(contact));

    /// <summary>The street line(s) from the contact's first address that carries any — Xero's
    /// AddressLine1–4 joined onto one line for the directory record's AddressLine field.</summary>
    private static string StreetOf(JsonElement contact)
    {
        if (!contact.TryGetProperty("Addresses", out var addresses) || addresses.ValueKind != JsonValueKind.Array)
            return "";
        foreach (var address in addresses.EnumerateArray())
        {
            var street = string.Join(", ",
                new[] { StringOf(address, "AddressLine1"), StringOf(address, "AddressLine2"),
                        StringOf(address, "AddressLine3"), StringOf(address, "AddressLine4") }
                    .Where(part => !string.IsNullOrWhiteSpace(part)));
            if (!string.IsNullOrWhiteSpace(street)) return street;
        }
        return "";
    }

    /// <summary>One phone line by Xero PhoneType, assembled country + area + number; null when empty.</summary>
    private static string? PhoneOf(JsonElement contact, string phoneType)
    {
        if (!contact.TryGetProperty("Phones", out var phones) || phones.ValueKind != JsonValueKind.Array)
            return null;
        foreach (var phone in phones.EnumerateArray())
        {
            if (!string.Equals(StringOf(phone, "PhoneType"), phoneType, StringComparison.OrdinalIgnoreCase))
                continue;
            var number = string.Join(" ",
                new[] { StringOf(phone, "PhoneCountryCode"), StringOf(phone, "PhoneAreaCode"), StringOf(phone, "PhoneNumber") }
                    .Where(part => !string.IsNullOrWhiteSpace(part)));
            if (!string.IsNullOrWhiteSpace(number)) return number;
        }
        return null;
    }

    /// <summary>One field ("City"/"Region") from the contact's first address that carries it.</summary>
    private static string AddressPartOf(JsonElement contact, string part)
    {
        if (!contact.TryGetProperty("Addresses", out var addresses) || addresses.ValueKind != JsonValueKind.Array)
            return "";
        foreach (var address in addresses.EnumerateArray())
        {
            var value = StringOf(address, part);
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }
        return "";
    }

    private static IReadOnlyList<XeroContactPerson> ReadContactPersons(JsonElement contact)
    {
        if (!contact.TryGetProperty("ContactPersons", out var persons) || persons.ValueKind != JsonValueKind.Array)
            return Array.Empty<XeroContactPerson>();
        return persons.EnumerateArray()
            .Select(person => new XeroContactPerson(FullNameOf(person), StringOf(person, "EmailAddress") ?? ""))
            .Where(person => !string.IsNullOrWhiteSpace(person.Name) || !string.IsNullOrWhiteSpace(person.EmailAddress))
            .ToList();
    }

    /// <summary>FirstName + LastName as one name — the shape a person has on the contact itself and on each ContactPerson.</summary>
    private static string FullNameOf(JsonElement personOrContact) =>
        string.Join(" ",
            new[] { StringOf(personOrContact, "FirstName"), StringOf(personOrContact, "LastName") }
                .Where(part => !string.IsNullOrWhiteSpace(part)));
}
