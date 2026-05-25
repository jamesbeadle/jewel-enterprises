using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class InMemoryProjectStore : IProjectStore
{
    private const string NigelEmail = "Nigel.Reilly@jewelenterprises.co.uk";

    private readonly List<Project> projects = new()
    {
        new Project(
            "PRJ-001",
            "JBB-2026-001",
            "The Coppice",
            "Mr & Mrs Wright",
            Organisation.JewelBespokeBuild,
            ProjectStage.LiveDelivery,
            NigelEmail,
            DateTimeOffset.UtcNow.AddMonths(-3)),
        new Project(
            "PRJ-002",
            "JBB-2026-002",
            "Hampton Manor",
            "Hampton Family Trust",
            Organisation.JewelBespokeBuild,
            ProjectStage.Mobilisation,
            NigelEmail,
            DateTimeOffset.UtcNow.AddMonths(-1)),
        new Project(
            "PRJ-003",
            "JBB-2026-003",
            "Oakwood Lodge",
            "Mrs Patel",
            Organisation.JewelBespokeBuild,
            ProjectStage.PreConstruction,
            NigelEmail,
            DateTimeOffset.UtcNow.AddDays(-10))
    };

    public event Action? OnChange;

    public IReadOnlyList<Project> All() => projects.AsReadOnly();

    public Project? Find(string projectId) =>
        projects.FirstOrDefault(project =>
            string.Equals(project.ProjectId, projectId, StringComparison.OrdinalIgnoreCase));
}
