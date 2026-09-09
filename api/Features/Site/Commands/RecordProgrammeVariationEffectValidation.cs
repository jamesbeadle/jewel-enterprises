using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class RecordProgrammeVariationEffectValidation
{
    private const int MaximumNoteLength = 512;
    private const int MaximumDelayDays = 3650;

    public ValidationOutcome Check(RecordProgrammeVariationEffect command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.VariationOrderId)) errors.Add("VariationOrderId is required.");
        if (string.IsNullOrWhiteSpace(command.ProgrammeTaskId)) errors.Add("ProgrammeTaskId is required.");
        if (string.IsNullOrWhiteSpace(command.RecordedByEmail)) errors.Add("RecordedByEmail is required.");
        if (command.DelayDays <= 0) errors.Add("DelayDays must be at least one day — remove the effect instead of recording none.");
        if (command.DelayDays > MaximumDelayDays) errors.Add($"DelayDays cannot exceed {MaximumDelayDays}.");
        if (command.Note.Length > MaximumNoteLength) errors.Add($"Note cannot exceed {MaximumNoteLength} characters.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
