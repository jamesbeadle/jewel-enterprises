using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class SetComplianceDocumentDetailsValidation
{
    public ValidationOutcome Check(SetComplianceDocumentDetails command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.SubcontractorId)) errors.Add("SubcontractorId is required.");
        if (string.IsNullOrWhiteSpace(command.ComplianceDocumentId)) errors.Add("ComplianceDocumentId is required.");
        if (PublicLiabilityCoverField.Check(command.PublicLiabilityCover) is { } coverProblem) errors.Add(coverProblem);
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
