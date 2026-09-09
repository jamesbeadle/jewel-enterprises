using Microsoft.Extensions.Configuration;

namespace Jewel.JPMS.Api.Features.Xero;

/// <summary>
/// Configuration for outbound calls to the Xero Accounting API via a Xero "custom connection"
/// (machine-to-machine, single organisation, OAuth2 client-credentials grant — no user consent
/// redirect). The client id and secret are secrets: they are read from app settings / Key Vault only
/// (app-setting names Xero__ClientId and Xero__ClientSecret) and must never be committed to source.
/// Local development uses local.settings.json (git-ignored).
/// </summary>
public sealed class XeroOptions
{
    public const string SectionName = "Xero";

    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Optional. When unset (the default) the token request omits the scope parameter and Xero
    /// grants everything the custom connection was set up with — the safe choice, because a
    /// requested scope that doesn't exactly match the granted set fails with invalid_scope
    /// ("Client credentials scope validation failed"). Set only to deliberately narrow the token.
    /// </summary>
    public string? Scopes { get; set; }

    /// <summary>
    /// Optional. Custom connections are tied to a single organisation so Xero does not require the
    /// xero-tenant-id header; set this only if Xero starts demanding it for the app in use.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Safety cap on pagination (100 transactions per page) so a huge ledger cannot stall the
    /// endpoint. The snapshot flags itself Truncated when the cap is hit, and the UI warns.
    /// </summary>
    public int MaxPages { get; set; } = 100;

    /// <summary>Start of the reporting window — reconciliation looks back to the start of 2023.</summary>
    public DateTime FromDate { get; set; } = new(2023, 1, 1);

    /// <summary>
    /// How long a fetched snapshot is served from memory before Xero is read again. Multi-year reads
    /// cost dozens of Xero calls (rate limit: 60/min), so page navigation should not refetch; the UI's
    /// Refresh button bypasses the cache explicitly. 0 disables caching.
    /// </summary>
    public int CacheMinutes { get; set; } = 5;

    /// <summary>
    /// Nominal account-code prefixes that count as cost of sales — only lines posted to
    /// these accounts enter the allocation queue (comma-separated app setting; default "3",
    /// i.e. the 3xx cost-of-sales range). Overhead lines (IT, subscriptions, professional
    /// fees...) stay visible on the live Xero transactions page but are never queued for
    /// project allocation. Empty disables the filter.
    /// </summary>
    public IReadOnlyList<string> CostOfSalesAccountPrefixes { get; set; } = new[] { "3" };

    /// <summary>
    /// Tracking category (as named in Xero) that identifies the site on an invoice line. The
    /// organisation's category is literally named "Sites" (plural); matching ignores case and spaces.
    /// </summary>
    public string SiteTrackingCategory { get; set; } = "Sites";

    /// <summary>Tracking category (as named in Xero) that identifies the cost code on an invoice line ("Cost Code" in the org; matching ignores case and spaces).</summary>
    public string CostCodeTrackingCategory { get; set; } = "Cost Code";

    /// <summary>The revenue account a valuation invoice raised from the portal posts to (2026-09-09,
    /// the accountant's ask) — "200 Sales" in Xero's UK chart unless Xero__SalesAccountCode says otherwise.</summary>
    public string SalesAccountCode { get; set; } = "200";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);

    public static XeroOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var options = new XeroOptions
        {
            ClientId = section["ClientId"],
            ClientSecret = section["ClientSecret"],
            TenantId = section["TenantId"]
        };

        var scopes = section["Scopes"];
        if (!string.IsNullOrWhiteSpace(scopes))
            options.Scopes = scopes;

        if (int.TryParse(section["MaxPages"], out var maxPages) && maxPages is > 0 and <= 500)
            options.MaxPages = maxPages;

        if (DateTime.TryParse(section["FromDate"], out var fromDate))
            options.FromDate = fromDate;

        if (int.TryParse(section["CacheMinutes"], out var cacheMinutes) && cacheMinutes is >= 0 and <= 120)
            options.CacheMinutes = cacheMinutes;

        var prefixes = section["CostOfSalesAccountPrefixes"];
        if (prefixes is not null)
            options.CostOfSalesAccountPrefixes = prefixes
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var siteCategory = section["SiteTrackingCategory"];
        if (!string.IsNullOrWhiteSpace(siteCategory))
            options.SiteTrackingCategory = siteCategory;

        var costCodeCategory = section["CostCodeTrackingCategory"];
        if (!string.IsNullOrWhiteSpace(costCodeCategory))
            options.CostCodeTrackingCategory = costCodeCategory;

        var salesAccountCode = section["SalesAccountCode"];
        if (!string.IsNullOrWhiteSpace(salesAccountCode))
            options.SalesAccountCode = salesAccountCode;

        return options;
    }
}
