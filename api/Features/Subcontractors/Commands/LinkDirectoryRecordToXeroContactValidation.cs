using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class LinkDirectoryRecordToXeroContactValidation
{
    public ValidationOutcome Check(LinkDirectoryRecordToXeroContact command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.SubcontractorId))
            errors.Add("A directory record id is required.");
        if (string.IsNullOrWhiteSpace(command.XeroContactId))
            errors.Add("A Xero contact id is required.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class UnlinkDirectoryRecordFromXeroContactValidation
{
    public ValidationOutcome Check(UnlinkDirectoryRecordFromXeroContact command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.SubcontractorId))
            errors.Add("A directory record id is required.");
        if (string.IsNullOrWhiteSpace(command.XeroContactId))
            errors.Add("A Xero contact id is required.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
