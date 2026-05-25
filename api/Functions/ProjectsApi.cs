using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Functions;

public sealed class ProjectsApi
{
    private readonly JpmsContext context;

    public ProjectsApi(JpmsContext context)
    {
        this.context = context;
    }

    [Function("ListProjects")]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects")] HttpRequest request)
    {
        var projects = await context.Projects
            .OrderByDescending(project => project.CreatedAt)
            .ToListAsync();
        return new OkObjectResult(projects);
    }

    [Function("GetProject")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}")] HttpRequest request,
        string projectId)
    {
        var project = await context.Projects.FindAsync(projectId);
        if (project is null) return new NotFoundResult();
        return new OkObjectResult(project);
    }

    [Function("UpsertProject")]
    public async Task<IActionResult> Upsert(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "projects")] HttpRequest request)
    {
        var incoming = await request.ReadFromJsonAsync<ProjectEntity>();
        if (incoming is null) return new BadRequestResult();

        var existing = await context.Projects.FindAsync(incoming.ProjectId);
        if (existing is null) context.Projects.Add(incoming);
        else context.Entry(existing).CurrentValues.SetValues(incoming);

        await context.SaveChangesAsync();
        return new OkObjectResult(incoming);
    }
}
