using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface IProjectStore
{
    IReadOnlyList<Project> All();

    Project? Find(string projectId);

    event Action? OnChange;
}
