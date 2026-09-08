using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class DiscardProgrammeDraftValidation
{
    public ValidationOutcome Check(DiscardProgrammeDraft command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProgrammeDraftId)) errors.Add("ProgrammeDraftId is required.");
        if (string.IsNullOrWhiteSpace(command.DiscardedByEmail)) errors.Add("DiscardedByEmail is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
