
namespace Jewel.JPMS.Features.Procurement;

public partial class TenderSubmissionModal
{
    [Parameter, EditorRequired] public string BidPackageId { get; set; } = "";
    [Parameter] public IReadOnlyList<BidPackageRecipient> Recipients { get; set; } = Array.Empty<BidPackageRecipient>();
    [Parameter] public IReadOnlyList<BidPackageLineItem> LineItems { get; set; } = Array.Empty<BidPackageLineItem>();
    [Parameter] public bool Busy { get; set; }
    [Parameter] public bool CanEdit { get; set; }

    /// <summary>Raised after a submission saves, so the host reloads the package's quotes.</summary>
    [Parameter] public EventCallback OnSaved { get; set; }

    private sealed class ExtractDraft
    {
        public string? BidPackageLineItemId { get; set; }
        public string Description { get; set; } = "";
        public string Unit { get; set; } = "";
        public decimal Quantity { get; set; }
        public decimal Rate { get; set; }
        public decimal Total { get; set; }
    }

    private bool isOpen;
    private bool extracting;
    private bool saving;
    private bool proposed;
    private bool complete;
    private MailboxMessage? sourceEmail;
    private List<string> issues = new();
    private string subcontractorNote = "";
    private string subcontractorId = "";
    private string notes = "";
    private List<ExtractDraft> drafts = new();
    private string? saveError;

    /// <summary>The manual path: the package's line schedule pre-filled for hand keying.</summary>
    public void OpenManual()
    {
        if (Busy || !CanEdit) return;
        Reset();
        isOpen = true;
        PrefillFromLineItems();
        StateHasChanged();
    }

    /// <summary>
    /// "Extract information" on a filed tender email: the AI reads the email (body + the returned
    /// pricing-schedule spreadsheet, extracted server-side) against the package's line schedule and
    /// pre-fills this modal with the submission it proposes plus every gap it found. However the
    /// read goes, the form falls back to the blank package schedule so the tender can always be
    /// keyed by hand.
    /// </summary>
    public async Task OpenFromEmailAsync(MailboxMessage email)
    {
        if (Busy || extracting || !CanEdit) return;
        Reset();
        isOpen = true;
        extracting = true;
        sourceEmail = email;
        StateHasChanged();
        try
        {
            var proposal = await Commands.SendAsync(
                new ExtractTenderFromMessage(BidPackageId, email.Id), CancellationToken.None);
            proposed = proposal.Proposed;
            complete = proposal.Complete;
            subcontractorId = proposal.SubcontractorId ?? "";
            subcontractorNote = proposal.SubcontractorNote;
            notes = proposal.Notes;
            issues = proposal.Issues.ToList();
            drafts = proposal.Lines
                .Select(line => new ExtractDraft
                {
                    BidPackageLineItemId = line.BidPackageLineItemId,
                    Description = line.Description,
                    Unit = line.Unit,
                    Quantity = line.Quantity,
                    Rate = line.Rate,
                    Total = line.Total
                })
                .ToList();
        }
        catch (CommandFailedException ex)
        {
            issues = new List<string> { ex.Message };
        }
        catch
        {
            issues = new List<string> { "The tender couldn't be read just now — enter the submission manually, or close and try again." };
        }
        finally
        {
            if (drafts.Count == 0) PrefillFromLineItems();
            extracting = false;
            StateHasChanged();
        }
    }

    private void Reset()
    {
        extracting = false;
        proposed = false;
        complete = false;
        sourceEmail = null;
        issues = new();
        subcontractorNote = "";
        subcontractorId = "";
        notes = "";
        drafts = new();
        saveError = null;
    }

    private void PrefillFromLineItems()
    {
        drafts = LineItems
            .Select(item => new ExtractDraft { BidPackageLineItemId = item.LineItemId, Description = item.Description, Unit = item.Unit, Quantity = item.Quantity })
            .ToList();
        if (drafts.Count == 0) drafts.Add(new ExtractDraft { Quantity = 1 });
    }

    private void Close() => isOpen = false;

    private void AddLine() => drafts.Add(new ExtractDraft { Quantity = 1 });

    private void RecalcTotal(ExtractDraft draft)
    {
        if (draft.Total == 0 && draft.Rate != 0 && draft.Quantity != 0)
            draft.Total = decimal.Round(draft.Rate * draft.Quantity, 2);
    }

    private async Task SaveAsync()
    {
        if (saving || Busy || !CanEdit || string.IsNullOrWhiteSpace(subcontractorId)) return;
        saveError = null;
        try
        {
            saving = true;
            var lines = drafts
                .Where(draft => !string.IsNullOrWhiteSpace(draft.Description))
                .Select(draft => new QuoteExtractionLine(
                    draft.BidPackageLineItemId, draft.Description.Trim(), (draft.Unit ?? "").Trim(),
                    draft.Quantity, draft.Rate, draft.Total))
                .ToList();
            await Commands.SendAsync(
                new SaveExtractedQuote(BidPackageId, subcontractorId, notes ?? "", lines,
                    // From an email: the save also records that email as Extracted → this quote.
                    SourceMessageId: sourceEmail?.Id, SourceInternetMessageId: sourceEmail?.InternetMessageId),
                CancellationToken.None);
            isOpen = false;
            await OnSaved.InvokeAsync();
        }
        catch { saveError = "Couldn't save that submission. Make sure a subcontractor is selected and every line has a description."; }
        finally { saving = false; }
    }
}
