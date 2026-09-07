using System.Net;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Sales.Proposals;
using static Jewel.JPMS.Api.Features.Sales.Imagine.ImagineWording;

namespace Jewel.JPMS.Api.Features.Sales.Imagine;

public sealed partial class ImaginePublicService
{
    public async Task<ImagineView> SubmitAsync(string token, ImagineSubmission submission, string clientHash, CancellationToken ct)
    {
        var lead = await FindLeadAsync(token, ct) ?? throw new InvalidOperationException("This link isn't valid.");
        var (brief, email, name, photos) = CheckSubmission(submission);
        var decoded = photos.Select(ImaginePhotoDecoding.Decode).ToList();
        var rounds = await context.ImagineRounds.Where(row => row.LeadId == lead.LeadId).ToListAsync(ct);
        await CheckLimitsAsync(rounds, clientHash, ct);

        var now = DateTimeOffset.UtcNow;
        var round = ConceptsRound(lead, rounds.Count + 1, brief, name, email, clientHash, now);
        context.ImagineRounds.Add(round);
        await SavePhotosAsync(lead, round, decoded, now, ct);

        // What they typed fills the gaps on the lead — a letter-found lead often has an address and
        // no name; now it has both, and an email to reply to.
        if (string.IsNullOrWhiteSpace(lead.ContactName) && name.Length > 0) lead.ContactName = Clip(name, 256);
        if (string.IsNullOrWhiteSpace(lead.ContactEmail)) lead.ContactEmail = Clip(email, 256);

        var who = name.Length > 0 ? name : email;
        context.LeadActivities.Add(Activity(lead.LeadId,
            $"Imagine round {round.Number}: {who} uploaded {decoded.Count} {(decoded.Count == 1 ? "photo" : "photos")}"
            + (brief.Length > 0 ? $" — \"{Clip(brief, 300)}\"" : "") + "."));
        MoveToEngaged(lead, now);
        await context.SaveChangesAsync(ct);

        await EnqueueAsync(round, ct);
        await NotifySalesOfUploadAsync(lead, who, email, brief, decoded.Count, ct);
        return await ViewAsync(lead, ct);
    }

    private (string Brief, string Email, string Name, IReadOnlyList<ImaginePhotoUpload> Photos) CheckSubmission(ImagineSubmission submission)
    {
        var brief = (submission.Brief ?? "").Trim();
        var email = (submission.Email ?? "").Trim();
        var name = (submission.Name ?? "").Trim();
        if (!submission.Consent) throw new InvalidOperationException("Please tick the box so we can email you your concepts.");
        if (!LooksLikeEmail(email)) throw new InvalidOperationException("Please give an email address we can send the concepts to.");
        if (brief.Length > ImagineLimits.MaxBriefLength) throw new InvalidOperationException($"Please keep the description under {ImagineLimits.MaxBriefLength} characters.");
        var photos = submission.Photos ?? Array.Empty<ImaginePhotoUpload>();
        if (photos.Count == 0) throw new InvalidOperationException("Add at least one photo of your house or plot.");
        if (photos.Count > ImagineLimits.MaxPhotosPerRound) throw new InvalidOperationException($"Up to {ImagineLimits.MaxPhotosPerRound} photos per round, please.");
        if (!store.IsConfigured) throw new InvalidOperationException("We can't take uploads just now — please try again later.");
        return (brief, email, name, photos);
    }

    private static ImagineRoundEntity ConceptsRound(LeadEntity lead, int number, string brief, string name, string email, string clientHash, DateTimeOffset now) => new()
    {
        RoundId = Guid.NewGuid().ToString("N"),
        LeadId = lead.LeadId,
        Number = number,
        Kind = (int)ImagineRoundKind.Concepts,
        Brief = brief,
        Status = (int)ImagineRoundStatus.Queued,
        RequestedAt = now,
        ProspectName = Clip(name, 256),
        ProspectEmail = Clip(email, 256),
        ClientHash = clientHash
    };

    private async Task SavePhotosAsync(LeadEntity lead, ImagineRoundEntity round, List<(byte[] Bytes, string ContentType)> decoded, DateTimeOffset now, CancellationToken ct)
    {
        var order = 0;
        foreach (var (bytes, contentType) in decoded)
        {
            order++;
            var imageId = Guid.NewGuid().ToString("N");
            var blobRef = await store.SaveAsync(lead.LeadId, round.RoundId, imageId, contentType, bytes, ct);
            context.ImagineImages.Add(new ImagineImageEntity
            {
                ImageId = imageId,
                LeadId = lead.LeadId,
                RoundId = round.RoundId,
                Kind = (int)ImagineImageKind.Photo,
                Order = order,
                Title = $"Photo {order}",
                BlobRef = blobRef,
                ContentType = contentType,
                Size = bytes.LongLength,
                CreatedAt = now
            });
        }
    }
}
