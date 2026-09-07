using System.Security.Cryptography;
using System.Text;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Sales.Proposals;
using Jewel.JPMS.Api.Storage;

namespace Jewel.JPMS.Api.Features.Sales.Imagine;

/// <summary>
/// Everything the public imagine page can do, keyed by the lead's token and nothing else. The
/// token IS the authorisation: it was printed on one letter to one household, so whoever holds
/// it is that prospect (or someone they handed it to). Refusals throw InvalidOperationException
/// with a sentence the page shows as-is. Abuse limits are deliberately simple and all here: so
/// many rounds per lead, one render at a time per lead, so many submissions per connection per
/// hour, so many across the site per hour — a public upload endpoint with no login needs a
/// ceiling on what it can cost.
/// One partial per thing the page can do — Submit, Revise, React, Proposal — with the abuse
/// limits and the notes to sales in Limits, photo decoding in ImaginePhotoDecoding and the
/// wording helpers in ImagineWording.
/// </summary>
public sealed partial class ImaginePublicService
{
    private const int MaxRoundsPerClientPerHour = 6;
    private const int MaxRoundsSiteWidePerHour = 60;

    private readonly JpmsContext context;
    private readonly IImagineImageStore store;
    private readonly IImagineRenderQueue queue;
    private readonly IImagineNotifier notifier;
    private readonly ILogger<ImaginePublicService> logger;

    public ImaginePublicService(
        JpmsContext context, IImagineImageStore store, IImagineRenderQueue queue,
        IImagineNotifier notifier, ILogger<ImaginePublicService> logger)
    {
        this.context = context;
        this.store = store;
        this.queue = queue;
        this.notifier = notifier;
        this.logger = logger;
    }

    /// <summary>SHA-256 of the caller's address — the per-connection throttle key.</summary>
    public static string ClientHash(HttpRequest request)
    {
        var forwarded = request.Headers["X-Forwarded-For"].ToString();
        var address = string.IsNullOrWhiteSpace(forwarded)
            ? request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
            : forwarded.Split(',')[0].Trim();
        // The SWA edge appends the port to the forwarded address.
        var colon = address.LastIndexOf(':');
        if (colon > 0 && address.Count(c => c == ':') == 1) address = address[..colon];
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(address))).ToLowerInvariant();
    }

    public async Task<ImagineView?> GetAsync(string token, CancellationToken ct)
    {
        var lead = await FindLeadAsync(token, ct);
        return lead is null ? null : await ViewAsync(lead, ct);
    }

    public async Task<StoredBlob?> OpenImageAsync(string token, string imageId, CancellationToken ct)
    {
        var lead = await FindLeadAsync(token, ct);
        if (lead is null) return null;
        var image = await context.ImagineImages.AsNoTracking()
            .FirstOrDefaultAsync(row => row.ImageId == imageId && row.LeadId == lead.LeadId, ct);
        return image is null ? null : await store.OpenAsync(image.BlobRef, ct);
    }

    // ---- helpers ----------------------------------------------------------------------------

    private Task<LeadEntity?> FindLeadAsync(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 64) return Task.FromResult<LeadEntity?>(null);
        return context.Leads.FirstOrDefaultAsync(row => row.ImagineToken == token, ct);
    }

    private async Task<ImagineView> ViewAsync(LeadEntity lead, CancellationToken ct)
    {
        var rounds = await ImagineMapping.RoundsForLeadAsync(context, lead.LeadId, ct);
        var proposals = await context.SalesProposals.AsNoTracking().Where(row => row.LeadId == lead.LeadId).ToListAsync(ct);
        var live = ProposalMapping.Live(proposals);
        var firstName = ImagineWording.FirstName(lead.ContactName);
        return new ImagineView(
            firstName,
            lead.ContactEmail,
            rounds,
            Math.Max(0, ImagineLimits.MaxRoundsPerLead - rounds.Count),
            ImagineLimits.MaxPhotosPerRound,
            live?.ToView());
    }
}
