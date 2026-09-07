using System.Net;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Sales.Proposals;
using static Jewel.JPMS.Api.Features.Sales.Imagine.ImagineWording;

namespace Jewel.JPMS.Api.Features.Sales.Imagine;

public sealed partial class ImaginePublicService
{
    public async Task<ImagineView> AcceptProposalAsync(string token, ProposalAcceptance acceptance, string clientHash, CancellationToken ct)
    {
        var lead = await FindLeadAsync(token, ct) ?? throw new InvalidOperationException("This link isn't valid.");
        var proposal = await context.SalesProposals
            .FirstOrDefaultAsync(row => row.ProposalId == acceptance.ProposalId && row.LeadId == lead.LeadId, ct)
            ?? throw new InvalidOperationException("That proposal isn't on this page.");
        if (proposal.Status == (int)SalesProposalStatus.Accepted) return await ViewAsync(lead, ct);
        if (proposal.Status != (int)SalesProposalStatus.Sent)
            throw new InvalidOperationException("This proposal has been replaced — please use the latest one, or reply to our email.");
        if (!acceptance.AgreedToTerms) throw new InvalidOperationException("Please confirm you've read and agree to the terms.");
        var name = (acceptance.Name ?? "").Trim();
        var email = (acceptance.Email ?? "").Trim();
        if (name.Length == 0) throw new InvalidOperationException("Please give your full name — it goes on the contract.");
        if (!LooksLikeEmail(email)) throw new InvalidOperationException("Please give a valid email address.");

        var valid = new HashSet<string>(proposal.Options().Select(option => option.OptionId), StringComparer.Ordinal);
        var chosen = (acceptance.OptionIds ?? Array.Empty<string>()).Where(valid.Contains).Distinct(StringComparer.Ordinal).ToList();
        var now = DateTimeOffset.UtcNow;
        proposal.Status = (int)SalesProposalStatus.Accepted;
        proposal.AcceptedAt = now;
        proposal.AcceptedByName = Clip(name, 256);
        proposal.AcceptedByEmail = Clip(email, 256);
        proposal.AcceptedOptionIdsJson = ProposalMapping.ToJson(chosen);
        proposal.AcceptedPrice = ProposalMapping.PriceFor(proposal, chosen);
        proposal.AcceptedClientHash = clientHash;
        proposal.UpdatedAt = now;

        var optionNames = proposal.Options().Where(option => chosen.Contains(option.OptionId)).Select(option => option.Name).ToList();
        context.LeadActivities.Add(new LeadActivityEntity
        {
            LeadActivityId = Guid.NewGuid().ToString("N"),
            LeadId = lead.LeadId,
            Kind = (int)LeadActivityKind.ProposalAccepted,
            Summary = $"Proposal v{proposal.Version} \"{proposal.Title}\" accepted by {name} ({email}) at £{proposal.AcceptedPrice:N0}"
                + (optionNames.Count > 0 ? $" with {string.Join(", ", optionNames)}" : " with no options") + ". Ready to mark Won.",
            OccurredAt = now,
            RecordedByEmail = email
        });
        await context.SaveChangesAsync(ct);

        await NotifySalesAsync(lead,
            $"ACCEPTED: {proposal.Title} — {lead.DisplayReference}",
            $"<p><strong>{WebUtility.HtmlEncode(name)}</strong> ({WebUtility.HtmlEncode(email)}) accepted proposal v{proposal.Version} <strong>{WebUtility.HtmlEncode(proposal.Title)}</strong> at <strong>£{proposal.AcceptedPrice:N0}</strong>"
            + (optionNames.Count > 0 ? $" with {WebUtility.HtmlEncode(string.Join(", ", optionNames))}" : " with no options")
            + $".</p><p>Open {WebUtility.HtmlEncode(lead.DisplayReference)} in the portal and mark it Won to create the client and the project.</p>",
            $"{name} ({email}) accepted proposal v{proposal.Version} \"{proposal.Title}\" at £{proposal.AcceptedPrice:N0}. Open {lead.DisplayReference} and mark it Won.", ct);

        return await ViewAsync(lead, ct);
    }

    public async Task<ImagineView> DeclineProposalAsync(string token, ProposalDecline decline, CancellationToken ct)
    {
        var lead = await FindLeadAsync(token, ct) ?? throw new InvalidOperationException("This link isn't valid.");
        var proposal = await context.SalesProposals
            .FirstOrDefaultAsync(row => row.ProposalId == decline.ProposalId && row.LeadId == lead.LeadId, ct)
            ?? throw new InvalidOperationException("That proposal isn't on this page.");
        if (proposal.Status != (int)SalesProposalStatus.Sent) return await ViewAsync(lead, ct);
        var reason = Clip((decline.Reason ?? "").Trim(), 1024);
        var now = DateTimeOffset.UtcNow;
        proposal.Status = (int)SalesProposalStatus.Declined;
        proposal.DeclinedAt = now;
        proposal.DeclineReason = reason.Length == 0 ? null : reason;
        proposal.UpdatedAt = now;
        context.LeadActivities.Add(Activity(lead.LeadId,
            $"Proposal v{proposal.Version} \"{proposal.Title}\" declined" + (reason.Length > 0 ? $" — \"{reason}\"" : "") + "."));
        await context.SaveChangesAsync(ct);
        await NotifySalesAsync(lead,
            $"Declined: {proposal.Title} — {lead.DisplayReference}",
            $"<p>The prospect on {WebUtility.HtmlEncode(lead.DisplayReference)} declined proposal v{proposal.Version}.</p>" + (reason.Length > 0 ? $"<p><em>{WebUtility.HtmlEncode(reason)}</em></p>" : ""),
            $"Proposal v{proposal.Version} declined on {lead.DisplayReference}.\n\n{reason}", ct);
        return await ViewAsync(lead, ct);
    }
}
