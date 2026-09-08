using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// The gates the approve_labour_bill action and the endpoint share (2026-09-08): who may
// approve, and what a well-formed ask looks like. The rule itself — every worker on the bill
// reads Matches — lives in the handler, decided on the schedules as they stand.

public sealed class ApproveLabourBillAuthorisation
{
    public bool Allows(SignedInUser user, ApproveLabourBill command) =>
        LabourRoleSets.ManageSettlement.IncludesAny(user.Roles);
}

public sealed class ApproveLabourBillValidation
{
    public ValidationOutcome Check(ApproveLabourBill command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.XeroInvoiceId))
            errors.Add("xeroInvoiceId is required — the bill as view_settlement_month's coveredBill names it.");
        if (command.Year < 2020 || command.Year > 2100)
            errors.Add("Year must be between 2020 and 2100.");
        if (command.Month < 1 || command.Month > 12)
            errors.Add("Month must be between 1 and 12.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
