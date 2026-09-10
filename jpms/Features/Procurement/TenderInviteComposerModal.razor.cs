using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Features.Procurement;

public partial class TenderInviteComposerModal
{
    [Parameter, EditorRequired] public string BidPackageId { get; set; } = "";

    /// <summary>The package the invite is for — its title and reference seed the defaults.</summary>
    [Parameter] public BidPackage? Package { get; set; }

    /// <summary>The line items to price: they build the default body's schedule tables, and an
    /// empty list surfaces the nothing-to-price warning.</summary>
    [Parameter] public IReadOnlyList<BidPackageLineItem> LineItems { get; set; } = Array.Empty<BidPackageLineItem>();

    /// <summary>The tender list — directory emails prefill BCC; entries without one are named
    /// in the won't-receive-it warning.</summary>
    [Parameter] public IReadOnlyList<BidPackageRecipient> Recipients { get; set; } = Array.Empty<BidPackageRecipient>();

    /// <summary>Tender documents + linked drawings travelling with the invite, for the note.</summary>
    [Parameter] public int AttachedDocumentCount { get; set; }

    /// <summary>Draft persistence is skipped for read-only viewers.</summary>
    [Parameter] public bool CanEdit { get; set; }

    /// <summary>Every send's outcome, sent or staged-in-Drafts — the host updates its package,
    /// banner and email list from it. The dialog closes itself only on a genuine send.</summary>
    [Parameter] public EventCallback<BidPackageInviteSendOutcome> OnSent { get; set; }

    private bool isOpen;
    private bool sending;
    private bool savingDraft;
    private bool editingBody;
    private string subject = "";
    private string body = "";
    private string to = "";
    private string cc = "";
    private string bcc = "";
    private DateTimeOffset? draftSavedAt;
    private string? sendError;

    // Invited but unreachable — no email address in the directory.
    private IReadOnlyList<string> MissingEmail =>
        Recipients
            .Select(r => Subs.Find(r.SubcontractorId) is { } s
                ? (Name: s.CompanyName, HasEmail: !string.IsNullOrWhiteSpace(s.ContactEmail))
                : (Name: r.SubcontractorId, HasEmail: false))
            .Where(x => !x.HasEmail)
            .Select(x => x.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    // The tender list's directory emails — the BCC the composer opens with. Only the rows still
    // in the running: a firm that has Declined, or already Won, is never re-invited by default
    // (the same rule as BidPackageInviteMailAssembler.DefaultBccAsync on the server).
    private IReadOnlyList<string> TenderListEmails =>
        Recipients
            .Where(r => r.Status != BidPackageRecipientStatus.Declined && r.Status != BidPackageRecipientStatus.Won)
            .Select(r => Subs.Find(r.SubcontractorId))
            .Where(s => s is not null && !string.IsNullOrWhiteSpace(s.ContactEmail))
            .Select(s => s!.ContactEmail.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static int CountAddresses(string raw) =>
        raw.Split(new[] { ';', ',' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Count(address => address.Contains('@'));

    private int RecipientCount => CountAddresses(to) + CountAddresses(cc) + CountAddresses(bcc);

    /// <summary>Opens the composer, the persisted draft winning over the defaults — a
    /// half-written invite picked up where it was left, by whoever picks it up.</summary>
    public async Task OpenAsync()
    {
        if (Package is null) return;
        editingBody = false;
        sendError = null;
        draftSavedAt = null;

        BidPackageInviteComposerDraft? saved = null;
        try { saved = await Queries.AskAsync(new GetBidPackageInviteComposerDraft(BidPackageId), CancellationToken.None); }
        catch { /* no draft is a fine answer; the defaults stand */ }

        if (saved is not null)
        {
            subject = saved.Subject;
            body = saved.Body;
            to = saved.To;
            cc = saved.Cc;
            bcc = saved.Bcc;
            draftSavedAt = saved.SavedAt;
        }
        else
        {
            subject = $"Invitation to tender — {Package.Title} ({Package.Reference})";
            body = DefaultInviteBody();
            to = "";
            cc = "";
            bcc = string.Join("; ", TenderListEmails);
        }
        isOpen = true;
        StateHasChanged();
    }

    private async Task SaveDraftAsync()
    {
        if (savingDraft || Package is null || !CanEdit) return;
        try
        {
            savingDraft = true;
            await Commands.SendAsync(
                new SaveBidPackageInviteComposerDraft(BidPackageId, subject.Trim(), body,
                    to.Trim(), cc.Trim(), bcc.Trim()), CancellationToken.None);
            draftSavedAt = DateTimeOffset.Now;
        }
        catch { sendError = "Couldn't save the invite draft. Please try again."; }
        finally { savingDraft = false; }
    }

    private Task CloseAsync()
    {
        if (sending) return Task.CompletedTask;
        isOpen = false;
        // Closing keeps the work: the draft persists on the package quietly, best-effort — the
        // user asked for exactly this ("it will be useful later"). A failed save costs a re-type,
        // never an error dialog on the way out.
        if (CanEdit && Package is not null)
            _ = Commands.SendAsync(
                new SaveBidPackageInviteComposerDraft(BidPackageId, subject.Trim(), body,
                    to.Trim(), cc.Trim(), bcc.Trim()), CancellationToken.None);
        return Task.CompletedTask;
    }

    // The pre-filled HTML invite: scope summary plus the line items to price, grouped by trade.
    private string DefaultInviteBody()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<p>Hello,</p>");
        if (LineItems.Count > 0)
            sb.AppendLine($"<p>Jewel Bespoke Build invites you to tender for the <strong>{Package!.Title}</strong> package (ref {Package.Reference}). Please complete and return the attached pricing schedule — the Rate and Total columns are left for you — and reply to this email with your exclusions and lead times, quoting the reference.</p>");
        else
            sb.AppendLine($"<p>Jewel Bespoke Build invites you to tender for the <strong>{Package!.Title}</strong> package (ref {Package.Reference}). Please price the works described in the attached documents and reply to this email with your rates, exclusions and lead times, quoting the reference in your reply.</p>");
        foreach (var group in LineItems.GroupBy(item => item.Trade).OrderBy(g => g.Key))
        {
            var heading = string.IsNullOrWhiteSpace(group.Key) ? "General" : group.Key;
            sb.AppendLine($"<p><strong>{heading}</strong></p>");
            sb.AppendLine("<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse\">");
            sb.AppendLine("<tr><th align=\"left\">Description</th><th align=\"left\">Qty</th><th align=\"left\">Unit</th><th align=\"left\">Rate</th><th align=\"left\">Total</th></tr>");
            foreach (var item in group.OrderBy(i => i.SortOrder))
                sb.AppendLine($"<tr><td>{item.Description}</td><td>{item.Quantity}</td><td>{item.Unit}</td><td></td><td></td></tr>");
            sb.AppendLine("</table>");
        }
        sb.AppendLine("<p><strong>Please include the following with your tender:</strong></p>");
        sb.AppendLine("<ul>");
        if (Package!.MaterialsApplicable)
            sb.AppendLine("<li><strong>Materials</strong> — please state whether you will be supplying your own materials for this work. If your rates are supply-and-fit, itemise the materials included; if they are labour-only, state what you expect us to supply.</li>");
        sb.AppendLine("<li><strong>Deposit</strong> — our preference is not to pay a deposit upfront. If one is required for you to take on the work, please state the amount and terms in your tender.</li>");
        sb.AppendLine("<li><strong>Duration</strong> — how long the work will take.</li>");
        sb.AppendLine("<li><strong>Availability</strong> — if selected, when you would be able to start.</li>");
        sb.AppendLine("<li><strong>Insurances</strong> — confirmation that you hold all insurances required for this work.</li>");
        sb.AppendLine("<li><strong>RAMS</strong> — confirmation that you will provide the required RAMS documentation.</li>");
        sb.AppendLine("<li><strong>Portfolio</strong> — examples of prior comparable work.</li>");
        sb.AppendLine("</ul>");
        sb.AppendLine("<p>Our tender terms and conditions are attached; your tender will be taken as made on that basis unless it states otherwise.</p>");
        sb.AppendLine("<p>Please confirm receipt and let us know if you need any further information or drawings.</p>");
        sb.AppendLine("<p>Kind regards,<br/>Jewel Bespoke Build</p>");
        return sb.ToString();
    }

    private async Task ConfirmSendAsync()
    {
        if (sending || Package is null || !CanEdit || RecipientCount == 0) return;
        sendError = null;
        try
        {
            sending = true;
            var outcome = await Commands.SendAsync(
                new SendBidPackageInvite(BidPackageId, subject.Trim(), body,
                    to.Trim(), cc.Trim(), bcc.Trim()), CancellationToken.None);
            if (outcome.Sent)
            {
                isOpen = false;
                draftSavedAt = null;
            }
            // Sent or staged-in-Drafts, the host hears about it either way.
            await OnSent.InvokeAsync(outcome);
        }
        catch (CommandFailedException ex) { sendError = $"Couldn't send the invite: {ex.Message}"; }
        catch { sendError = "Couldn't send the invite. Check the recipients and the mailbox connection, then try again."; }
        finally { sending = false; }
    }
}
