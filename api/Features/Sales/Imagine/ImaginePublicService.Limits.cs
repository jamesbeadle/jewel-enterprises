using System.Net;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Sales.Proposals;
using Jewel.JPMS.Api.Features.Sales.Research;
using static Jewel.JPMS.Api.Features.Sales.Imagine.ImagineWording;

namespace Jewel.JPMS.Api.Features.Sales.Imagine;

public sealed partial class ImaginePublicService
{
    private async Task CheckLimitsAsync(List<ImagineRoundEntity> rounds, string clientHash, CancellationToken ct)
    {
        if (rounds.Count >= ImagineLimits.MaxRoundsPerLead)
            throw new InvalidOperationException("You've used all the rounds on this page — reply to our email and we'll carry on together.");
        if (rounds.Any(row => ((ImagineRoundStatus)row.Status).IsInProgress()))
            throw new InvalidOperationException("Your last round is still rendering — give it a few minutes.");
        if (!queue.IsConfigured)
            throw new InvalidOperationException("We can't render just now — please try again later.");
        var hourAgo = DateTimeOffset.UtcNow.AddHours(-1);
        var fromClient = await context.ImagineRounds.CountAsync(row => row.ClientHash == clientHash && row.RequestedAt > hourAgo, ct);
        if (fromClient >= MaxRoundsPerClientPerHour)
            throw new InvalidOperationException("That's a lot of uploads in an hour — please try again a little later.");
        var siteWide = await context.ImagineRounds.CountAsync(row => row.RequestedAt > hourAgo, ct);
        if (siteWide >= MaxRoundsSiteWidePerHour)
            throw new InvalidOperationException("We're busy rendering just now — please try again in an hour.");
    }

    private async Task EnqueueAsync(ImagineRoundEntity round, CancellationToken ct)
    {
        try
        {
            await queue.EnqueueAsync(new ImagineRenderMessage(round.RoundId), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The submission is saved and the lead page can retry it; the prospect sees "failed".
            logger.LogError(ex, "Imagine: could not queue round {RoundId}.", round.RoundId);
            round.Status = (int)ImagineRoundStatus.Failed;
            round.Error = Clip("Couldn't queue the render: " + ex.Message, 2000);
            await context.SaveChangesAsync(CancellationToken.None);
        }
    }

    private async Task NotifySalesAsync(LeadEntity lead, string subject, string html, string text, CancellationToken ct)
    {
        if (!notifier.IsConfigured) return;
        try { await notifier.SendToSalesAsync(subject, html, text, ct); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Imagine: the note to the sales mailbox about {Lead} failed.", lead.DisplayReference);
        }
    }

    private static void MoveToEngaged(LeadEntity lead, DateTimeOffset now)
    {
        var stage = (LeadStage)lead.Stage;
        if (!stage.IsOpen() || stage >= LeadStage.Engaged) return;
        lead.Stage = (int)LeadStage.Engaged;
        lead.StageChangedAt = now;
        lead.LostReason = null;
    }

    private Task NotifySalesOfUploadAsync(LeadEntity lead, string who, string email, string brief, int photoCount, CancellationToken ct) =>
        NotifySalesAsync(lead,
            $"Imagine: {who} uploaded photos ({lead.DisplayReference})",
            $"<p><strong>{WebUtility.HtmlEncode(who)}</strong> ({WebUtility.HtmlEncode(email)}) has uploaded {photoCount} photo(s) on the imagine page for {WebUtility.HtmlEncode(lead.DisplayReference)}"
            + (string.IsNullOrWhiteSpace(lead.SiteAddress) ? "" : $" — {WebUtility.HtmlEncode(lead.SiteAddress)}") + ".</p>"
            + (brief.Length > 0 ? $"<p>They wrote: <em>{WebUtility.HtmlEncode(brief)}</em></p>" : "")
            + "<p>The concepts are rendering; they'll be emailed the link when they're ready.</p>",
            $"{who} ({email}) uploaded {photoCount} photo(s) on the imagine page for {lead.DisplayReference}.\n\n{brief}", ct);
}
