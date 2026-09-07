using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class SetBidPackageEmailDispositionValidation
{
    public ValidationOutcome Check(SetBidPackageEmailDisposition command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.BidPackageId)) errors.Add("BidPackageId is required.");
        if (string.IsNullOrWhiteSpace(command.MessageId)) errors.Add("MessageId is required.");
        // Extracted is only ever stamped by SaveExtractedQuote, so the verdict and the quote it
        // names can never disagree; this command records Discarded or clears (Pending).
        if (command.Outcome is not (BidPackageEmailOutcome.Pending or BidPackageEmailOutcome.Discarded))
            errors.Add("Outcome must be Discarded (not a tender) or Pending (restore).");
        if (command.Note is { Length: > 1024 }) errors.Add("Note must be 1024 characters or fewer.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
