using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class ReviewProgrammeDraftLineValidation
{
    // 15 codes x (32 + a comma) stays inside the line's nvarchar(512) CostCodes column.
    private const int MaxCostCodesPerTask = 15;
    private const int MaxCostCodeLength = 32;

    public ValidationOutcome Check(ReviewProgrammeDraftLine command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProgrammeDraftLineId)) errors.Add("ProgrammeDraftLineId is required.");
        if (command.CostCodes is null) errors.Add("CostCodes is required (an empty list clears the mapping).");
        else
        {
            if (command.CostCodes.Count > MaxCostCodesPerTask) errors.Add($"A task can map to at most {MaxCostCodesPerTask} cost centres.");
            if (command.CostCodes.Any(code => code is null || code.Trim().Length > MaxCostCodeLength)) errors.Add("A cost code is missing or too long.");
        }
        if (command.ReviewedPercent is { } percent && (percent < 0m || percent > 100m)) errors.Add("Progress must be between 0 and 100.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
