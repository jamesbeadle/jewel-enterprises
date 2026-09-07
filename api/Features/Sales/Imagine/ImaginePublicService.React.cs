using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Sales.Proposals;
using static Jewel.JPMS.Api.Features.Sales.Imagine.ImagineWording;

namespace Jewel.JPMS.Api.Features.Sales.Imagine;

public sealed partial class ImaginePublicService
{
    public async Task<ImagineView> ReactAsync(string token, ImagineReaction reaction, CancellationToken ct)
    {
        var lead = await FindLeadAsync(token, ct) ?? throw new InvalidOperationException("This link isn't valid.");
        var image = await context.ImagineImages
            .FirstOrDefaultAsync(row => row.ImageId == reaction.ImageId && row.LeadId == lead.LeadId && row.Kind == (int)ImagineImageKind.Concept, ct)
            ?? throw new InvalidOperationException("That concept isn't on this page.");
        var comment = (reaction.Comment ?? "").Trim();
        if (comment.Length > 2000) throw new InvalidOperationException("Please keep the comment under 2000 characters.");
        var changed = image.Liked != reaction.Liked || image.Comment != comment;
        image.Liked = reaction.Liked;
        image.Comment = comment;
        if (changed)
        {
            context.LeadActivities.Add(Activity(lead.LeadId,
                (reaction.Liked ? $"Liked \"{image.Title}\"" : $"Un-liked \"{image.Title}\"")
                + (comment.Length > 0 ? $" — \"{Clip(comment, 300)}\"" : "") + "."));
        }
        await context.SaveChangesAsync(ct);
        return await ViewAsync(lead, ct);
    }
}
