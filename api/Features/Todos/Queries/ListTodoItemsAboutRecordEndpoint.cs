using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

// GET the to-dos about one record: /api/records/{recordType}/{recordId}/todos — recordType is the
// RecordType enum name ("Defect") or its number. Internal-only read, like every to-do list.
public sealed class ListTodoItemsAboutRecordEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListTodoItemsAboutRecord, IReadOnlyList<TodoItem>> handler;

    public ListTodoItemsAboutRecordEndpoint(
        SignedInUserResolver users, IQueryHandler<ListTodoItemsAboutRecord, IReadOnlyList<TodoItem>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    private static readonly RoleSet RolesThatMayReadTodos = JpmsRoleSets.AllInternal;

    [Function(nameof(ListTodoItemsAboutRecord))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "records/{recordType}/{recordId}/todos")] HttpRequest request,
        string recordType, string recordId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadTodos.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (!Enum.TryParse<RecordType>(recordType, ignoreCase: true, out var type) || !Enum.IsDefined(type))
            return new BadRequestObjectResult($"Unknown record type '{recordType}'.");
        return new OkObjectResult(
            await handler.HandleAsync(new ListTodoItemsAboutRecord(type, recordId), request.HttpContext.RequestAborted));
    }
}
