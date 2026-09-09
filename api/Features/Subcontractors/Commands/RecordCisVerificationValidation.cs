using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class RecordCisVerificationValidation
{
    public ValidationOutcome Check(RecordCisVerification command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.SubcontractorId)) errors.Add("SubcontractorId is required.");
        if (string.IsNullOrWhiteSpace(command.CisStatus)) errors.Add("CIS status is required.");
        if ((command.CisStatus ?? "").Trim().Length > CisVerificationLimits.StatusMaxLength)
            errors.Add($"CIS status must be {CisVerificationLimits.StatusMaxLength} characters or fewer.");
        if ((command.CisVerificationNumber ?? "").Trim().Length > CisVerificationLimits.NumberMaxLength)
            errors.Add($"Verification number must be {CisVerificationLimits.NumberMaxLength} characters or fewer.");
        if (command.CisVerifiedOn is { } verifiedOn && verifiedOn > DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            errors.Add("Verification date can't be in the future.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
