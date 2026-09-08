using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class DraftProgrammeFromValuationValidation
{
    public ValidationOutcome Check(DraftProgrammeFromValuation command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.ValuationClaimId)) errors.Add("Pick the valuation claim to draft from.");
        if (string.IsNullOrWhiteSpace(command.RequestedByEmail)) errors.Add("RequestedByEmail is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
