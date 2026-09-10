using Jewel.JPMS.Api.Features.Subcontractors.Commands;
using Jewel.JPMS.Contracts.DocumentControl;

namespace Jewel.JPMS.Api.Features.DocumentControl.Commands;

public sealed class FileDocumentToSubcontractorValidation
{
    public ValidationOutcome Check(FileDocumentToSubcontractor command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.DocumentControlItemId)) errors.Add("DocumentControlItemId is required.");
        if (string.IsNullOrWhiteSpace(command.SubcontractorId)) errors.Add("SubcontractorId is required.");
        if (string.IsNullOrWhiteSpace(command.Kind)) errors.Add("Document kind is required.");
        else if (command.Kind.Trim().Length > 128) errors.Add("Document kind must be 128 characters or fewer.");
        if (PublicLiabilityCoverField.Check(command.PublicLiabilityCover) is { } coverProblem) errors.Add(coverProblem);
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
