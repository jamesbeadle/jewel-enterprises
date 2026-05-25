using System.Net.Http.Json;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpProjectStore : IProjectStore
{
    private const string ProjectsEndpoint = "/api/projects";

    private readonly HttpClient httpClient;
    private IReadOnlyList<Project> cachedProjects = Array.Empty<Project>();
    private bool hasLoaded;

    public HttpProjectStore(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public event Action? OnChange;

    public IReadOnlyList<Project> All()
    {
        if (!hasLoaded) _ = LoadAsync();
        return cachedProjects;
    }

    public Project? Find(string projectId) =>
        cachedProjects.FirstOrDefault(project =>
            string.Equals(project.ProjectId, projectId, StringComparison.OrdinalIgnoreCase));

    public Project Upsert(Project project)
    {
        _ = UpsertAsync(project);
        return project;
    }

    private async Task LoadAsync()
    {
        hasLoaded = true;
        try
        {
            var response = await httpClient.GetFromJsonAsync<List<Project>>(ProjectsEndpoint);
            cachedProjects = response?.AsReadOnly() ?? (IReadOnlyList<Project>)Array.Empty<Project>();
            OnChange?.Invoke();
        }
        catch
        {
            cachedProjects = Array.Empty<Project>();
        }
    }

    private async Task UpsertAsync(Project project)
    {
        var response = await httpClient.PostAsJsonAsync(ProjectsEndpoint, project);
        if (response.IsSuccessStatusCode) await LoadAsync();
    }
}
