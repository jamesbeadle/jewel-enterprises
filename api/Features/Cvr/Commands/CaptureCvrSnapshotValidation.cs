using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Cvr;

namespace Jewel.JPMS.Api.Features.Cvr.Commands;

public sealed class CaptureCvrSnapshotValidation
{
    public ValidationOutcome Check(CaptureCvrSnapshot command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (command.ForecastFinalCost < 0) errors.Add("Forecast final cost cannot be negative.");
        if (command.ForecastFinalValue < 0) errors.Add("Forecast final value cannot be negative.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
