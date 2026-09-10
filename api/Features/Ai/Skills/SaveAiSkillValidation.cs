using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

public sealed class SaveAiSkillValidation
{
    /// <summary>Ninety pages of markdown is ~250k characters; double it for headroom. The point is
    /// a ceiling on what an unbounded field can push into every prompt, not a real limit on prose.</summary>
    private const int MaxBodyLength = 500_000;

    public ValidationOutcome Check(SaveAiSkill command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.SkillKey))
            errors.Add("A skill key is required — lowercase, hyphenated, e.g. nigel-commercial-doctrine.");
        else if (command.SkillKey.Trim().Length > 128)
            errors.Add("That skill key is too long (128 characters max).");
        else if (command.SkillKey.Contains(' '))
            errors.Add("Skill keys are hyphenated, not spaced — e.g. commercial-director.");

        if (string.IsNullOrWhiteSpace(command.DisplayName))
            errors.Add("A display name is required.");

        if (string.IsNullOrWhiteSpace(command.Description))
            errors.Add("A description is required — it is what the assistant routes on, so say when "
                       + "this skill applies.");
        else if (command.Description.Length > 4000)
            errors.Add("That description is too long (4000 characters max).");

        if (string.IsNullOrWhiteSpace(command.Body))
            errors.Add("The skill body is empty.");
        else if (command.Body.Length > MaxBodyLength)
            errors.Add($"That body is too long ({MaxBodyLength:N0} characters max). Move the bulk "
                       + "into reference documents and keep the body to the method.");

        // The discipline must be one the store knows — a typo here would file the skill under a
        // heading nothing lists, silently.
        var agentKey = command.AgentKey?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(agentKey))
            errors.Add("A discipline is required — pick one, or \"shared\" for house-wide knowledge.");
        else if (!SkillDisciplines.IsKnown(agentKey))
            errors.Add($"No discipline named \"{agentKey}\" exists. Valid keys: "
                       + string.Join(", ", SkillDisciplines.All.Select(discipline => discipline.Key)) + ".");

        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
