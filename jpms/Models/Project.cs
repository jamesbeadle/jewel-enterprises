namespace Jewel.JPMS.Models;

public sealed record Project(
    string ProjectId,
    string Reference,
    string Name,
    string ClientName,
    Organisation Organisation,
    ProjectStage Stage,
    string ProjectManagerEmail,
    DateTimeOffset CreatedAt);
