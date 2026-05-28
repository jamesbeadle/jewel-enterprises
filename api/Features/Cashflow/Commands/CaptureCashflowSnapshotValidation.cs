using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Cashflow;

namespace Jewel.JPMS.Api.Features.Cashflow.Commands;

public sealed class CaptureCashflowSnapshotValidation
{
    public ValidationOutcome Check(CaptureCashflowSnapshot command)
    {
        var errors = new List<string>();
        if (command.ExpectedIncome13Week < 0) errors.Add("Expected income cannot be negative.");
        if (command.CommittedSpend13Week < 0) errors.Add("Committed spend cannot be negative.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
