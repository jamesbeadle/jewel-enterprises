using Jewel.JPMS.Features.Procurement;

namespace Jewel.JPMS.Pages;

public partial class ProjectBidPackageInviteDetail
{
    // ---- Record a tender submission, review, save as a quote ----
    // The modal (TenderSubmissionModal) owns the whole review flow — manual keying and the
    // AI extract-from-email path; the page only points it at the package and reloads on save.

    private TenderSubmissionModal? tenderSubmissionModal;
    private string? awardNote;

    private void OpenManualTenderModal() => tenderSubmissionModal?.OpenManual();

    private Task OpenExtractFromEmail(MailboxMessage email) =>
        tenderSubmissionModal?.OpenFromEmailAsync(email) ?? Task.CompletedTask;

    // ---- The package's verdict on a tagged email (Discard / Restore) ----
    // Shared team state held on the package (BidPackageEmailDisposition), never a mailbox tag —
    // the Emails tab is unaffected. The command answers with the full list, so no re-read.

    private IReadOnlyList<BidPackageEmailDisposition> emailDispositions = Array.Empty<BidPackageEmailDisposition>();

    private Task DiscardEmail(MailboxMessage email) => SetEmailDispositionAsync(email, BidPackageEmailOutcome.Discarded);
    private Task RestoreEmail(MailboxMessage email) => SetEmailDispositionAsync(email, BidPackageEmailOutcome.Pending);

    private async Task SetEmailDispositionAsync(MailboxMessage email, BidPackageEmailOutcome outcome)
    {
        if (busy) return;
        error = null;
        try
        {
            busy = true;
            emailDispositions = await Commands.SendAsync(
                new SetBidPackageEmailDisposition(BidPackageId, email.Id, email.InternetMessageId, outcome), CancellationToken.None);
        }
        catch (CommandFailedException ex)
        {
            error = $"Couldn't {(outcome == BidPackageEmailOutcome.Discarded ? "discard" : "restore")} that email: {ex.Message}";
        }
        catch
        {
            error = $"Couldn't {(outcome == BidPackageEmailOutcome.Discarded ? "discard" : "restore")} that email. Please try again.";
        }
        finally { busy = false; }
    }
}
