using System.Net;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Sales.Proposals;
using static Jewel.JPMS.Api.Features.Sales.Imagine.ImagineWording;

namespace Jewel.JPMS.Api.Features.Sales.Imagine;

public sealed partial class ImaginePublicService
{
    public async Task<ImagineView> ReviseAsync(string token, ImagineRevisionRequest request, string clientHash, CancellationToken ct)
    {
        var lead = await FindLeadAsync(token, ct) ?? throw new InvalidOperationException("This link isn't valid.");
        var feedback = (request.Feedback ?? "").Trim();
        if (feedback.Length == 0) throw new InvalidOperationException("Tell us what you'd change — a sentence is plenty.");
        if (feedback.Length > ImagineLimits.MaxBriefLength) throw new InvalidOperationException($"Please keep it under {ImagineLimits.MaxBriefLength} characters.");
        var chosen = await context.ImagineImages.AsNoTracking()
            .FirstOrDefaultAsync(row => row.ImageId == request.ImageId && row.LeadId == lead.LeadId && row.Kind == (int)ImagineImageKind.Concept, ct)
            ?? throw new InvalidOperationException("Pick one of the concepts to build on.");

        var rounds = await context.ImagineRounds.Where(row => row.LeadId == lead.LeadId).ToListAsync(ct);
        await CheckLimitsAsync(rounds, clientHash, ct);
        var first = rounds.OrderBy(row => row.Number).First();

        var now = DateTimeOffset.UtcNow;
        var round = new ImagineRoundEntity
        {
            RoundId = Guid.NewGuid().ToString("N"),
            LeadId = lead.LeadId,
            Number = rounds.Count + 1,
            Kind = (int)ImagineRoundKind.Revision,
            Brief = feedback,
            BasedOnImageId = chosen.ImageId,
            Status = (int)ImagineRoundStatus.Queued,
            RequestedAt = now,
            ProspectName = first.ProspectName,
            ProspectEmail = first.ProspectEmail,
            ClientHash = clientHash
        };
        context.ImagineRounds.Add(round);
        context.LeadActivities.Add(Activity(lead.LeadId,
            $"Imagine round {round.Number}: asked for a revision of \"{chosen.Title}\" — \"{Clip(feedback, 300)}\"."));
        await context.SaveChangesAsync(ct);

        await EnqueueAsync(round, ct);
        await NotifySalesAsync(lead,
            $"Imagine: revision requested on \"{chosen.Title}\" ({lead.DisplayReference})",
            $"<p>The prospect on {WebUtility.HtmlEncode(lead.DisplayReference)} chose <strong>{WebUtility.HtmlEncode(chosen.Title)}</strong> and asked for a revision:</p><p><em>{WebUtility.HtmlEncode(feedback)}</em></p>",
            $"Revision requested on \"{chosen.Title}\" for {lead.DisplayReference}:\n\n{feedback}", ct);

        return await ViewAsync(lead, ct);
    }
}
