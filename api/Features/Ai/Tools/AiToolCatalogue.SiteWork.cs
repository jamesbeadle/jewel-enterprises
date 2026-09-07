using Ganss.Xss;
using Jewel.JPMS.Api.Features.Labour; // SiteClock (view_labour_week's week arithmetic)
using Jewel.JPMS.Api.Features.MailboxIntake.Graph; // IIntakeMessageReader (record email reads)
using Jewel.JPMS.Api.Features.Requests; // TriageRoles (internal, same assembly)
using Jewel.JPMS.Contracts.Ai;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;


public static partial class AiToolCatalogue
{
    private static IEnumerable<AiTool> SiteWorkTools()
    {
        var readers = JpmsRoleSets.AllInternal;

        return new List<AiTool>
        {
            new(
                "list_defects",
                "Defects on a project. Status is Open, InProgress, Resolved or Verified. Looking for a "
                + "defect by what or where it is? Pass search on the FIRST call — it matches the "
                + "description and the location.",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view.", false),
                    ("status", "string", "Optional filter: Open, InProgress, Resolved or Verified.", false),
                    ("search", "string",
                        "Text matched against defect descriptions and locations — \"grout\", \"WH89 en-suite\".", false)),
                AiToolKind.Read,
                readers,
                async (context, input, ct) =>
                {
                    var project = await ResolveProjectAsync(context, AiToolSchema.Text(input, "projectId"), ct);
                    if (project is null) return NotFound("No project in scope. Ask the user which project, or call list_projects.");

                    var query = context.Db.Defects
                        .AsNoTracking()
                        .Where(row => row.ProjectId == project.ProjectId);

                    var statusText = AiToolSchema.Text(input, "status");
                    if (!string.IsNullOrWhiteSpace(statusText)
                        && Enum.TryParse<DefectStatus>(statusText, ignoreCase: true, out var status))
                    {
                        query = query.Where(row => row.Status == (int)status);
                    }

                    var defectSearch = AiToolSchema.Text(input, "search")?.Trim();
                    if (!string.IsNullOrWhiteSpace(defectSearch))
                        query = query.Where(row => row.Description.Contains(defectSearch) || row.Location.Contains(defectSearch));

                    var defectTotal = await query.CountAsync(ct);

                    var defects = await query
                        .OrderByDescending(row => row.RaisedAt)
                        .Take(100)
                        .ToListAsync(ct);

                    var supplierIds = defects.Select(row => row.SubcontractorId).Where(id => id != null).Distinct().ToList();
                    var suppliers = supplierIds.Count == 0
                        ? new Dictionary<string, Jewel.JPMS.Api.Data.Entities.SubcontractorEntity>()
                        : await context.Db.Subcontractors.AsNoTracking()
                            .Where(s => supplierIds.Contains(s.SubcontractorId))
                            .ToDictionaryAsync(s => s.SubcontractorId, ct);

                    return Serialise(new
                    {
                        ok = true,
                        project = project.Reference,
                        projectId = project.ProjectId,
                        count = defects.Count,
                        totalMatching = defectTotal,
                        note = defectTotal > defects.Count
                            ? $"Only the newest {defects.Count} of {defectTotal} matching defects are listed. "
                              + "Pass search to narrow instead of calling again blind."
                            : "read_record_emails record_type defect (with the defectId) reads a defect's tagged mail; "
                              + "list_todos aboutRecordId (the defectId) lists the to-dos about it; add_todo "
                              + "aboutRecordType Defect raises one. Sending the defect to its supplier happens "
                              + "on the defect's page (route) — no connector action yet.",
                        defects = defects.Select(row => new
                        {
                            row.DefectId,
                            reference = row.Reference,
                            status = ((DefectStatus)row.Status).ToString(),
                            description = row.Description,
                            location = row.Location,
                            supplier = row.SubcontractorId is { } supplierId && suppliers.TryGetValue(supplierId, out var supplier)
                                ? new { supplier.SubcontractorId, supplier.CompanyName, supplier.ContactEmail }
                                : null,
                            assignedTo = string.IsNullOrWhiteSpace(row.AssignedToEmail) ? null : row.AssignedToEmail,
                            sentToSupplierAt = row.SentToSupplierAt,
                            sentToSupplierBy = row.SentToSupplierByEmail,
                            raisedAt = row.RaisedAt,
                            resolvedAt = row.ResolvedAt,
                            route = $"/projects/{project.ProjectId}/defects/{row.DefectId}"
                        })
                    });
                }),
            new(
                "list_todos",
                "To-do items — company-wide by default, or one project's. Status is Open, InProgress or "
                + "Done; items are assigned to a ROLE (optionally pinned to one person). \"What is on "
                + "my list\" → status Open + the user's role from the current context. Pass search to "
                + "find an item by what it says instead of paging.",
                AiToolSchema.Object(
                    ("projectId", "string",
                        "Limit to one project. Omit for every project plus company-wide items.", false),
                    ("status", "string", "Optional filter: Open, InProgress or Done. Defaults to all.", false),
                    ("search", "string", "Text matched against item titles and notes.", false),
                    ("aboutRecordId", "string", "Only the items ABOUT one record — a defectId from list_defects, say. The record's page lists the same.", false)),
                AiToolKind.Read,
                readers,
                async (context, input, ct) =>
                {
                    var query = context.Db.TodoItems.AsNoTracking();

                    var todoProjectId = AiToolSchema.Text(input, "projectId")?.Trim();
                    if (!string.IsNullOrWhiteSpace(todoProjectId))
                        query = query.Where(row => row.ProjectId == todoProjectId);

                    var aboutRecordId = AiToolSchema.Text(input, "aboutRecordId")?.Trim();
                    if (!string.IsNullOrWhiteSpace(aboutRecordId))
                        query = query.Where(row => row.AboutRecordId == aboutRecordId);

                    var statusText = AiToolSchema.Text(input, "status")?.Trim().ToLowerInvariant();
                    query = statusText switch
                    {
                        "open" => query.Where(row => !row.IsComplete && row.StartedAt == null),
                        "inprogress" or "in_progress" => query.Where(row => !row.IsComplete && row.StartedAt != null),
                        "done" => query.Where(row => row.IsComplete),
                        _ => query
                    };

                    var todoSearch = AiToolSchema.Text(input, "search")?.Trim();
                    if (!string.IsNullOrWhiteSpace(todoSearch))
                        query = query.Where(row => row.Title.Contains(todoSearch) || row.Notes.Contains(todoSearch));

                    var todoTotal = await query.CountAsync(ct);

                    var items = await query
                        .OrderBy(row => row.IsComplete)
                        .ThenBy(row => row.DueAt == null)
                        .ThenBy(row => row.DueAt)
                        .Take(100)
                        .ToListAsync(ct);

                    var todoProjects = await ProjectReferenceMapAsync(context, items.Select(row => row.ProjectId), ct);

                    return Serialise(new
                    {
                        ok = true,
                        count = items.Count,
                        totalMatching = todoTotal,
                        note = todoTotal > items.Count
                            ? $"Only {items.Count} of {todoTotal} matching items are listed (incomplete and "
                              + "soonest-due first). Pass search or status to narrow."
                            : "read_record_emails record_type todo (with the todoItemId) reads an item's tagged "
                              + "mail. Actioning an item usually means doing the work it names, not just opening it.",
                        todos = items.Select(row => new
                        {
                            row.TodoItemId,
                            reference = row.Reference,
                            row.Title,
                            notes = string.IsNullOrWhiteSpace(row.Notes) ? null : row.Notes,
                            status = row.IsComplete ? "Done" : row.StartedAt is null ? "Open" : "InProgress",
                            assignee = row.AssigneeRole is { } assigneeRole
                                ? ((Role)assigneeRole).ToString()
                                  + (string.IsNullOrWhiteSpace(row.AssigneePersonEmail) ? "" : $" — {row.AssigneePersonEmail}")
                                : "Unassigned",
                            due = row.DueAt,
                            aboutRecord = row.AboutRecordType is { } aboutType
                                ? new { type = ((RecordType)aboutType).ToString(), recordId = row.AboutRecordId }
                                : null,
                            project = string.IsNullOrWhiteSpace(row.ProjectId)
                                ? "company-wide"
                                : todoProjects.TryGetValue(row.ProjectId, out var todoProject) ? todoProject : row.ProjectId,
                            projectId = string.IsNullOrWhiteSpace(row.ProjectId) ? null : row.ProjectId,
                            route = $"/todos/{row.TodoItemId}"
                        })
                    });
                }),
        };
    }
}
