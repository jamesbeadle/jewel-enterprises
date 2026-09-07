using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

/// <summary>
/// The §6a automation (docs/Labour-Overview-Forecast-and-Xero-Mapping-Scope.md): for each fully
/// signed-off worker-month, write the settlement schedule's coding into Xero.
///
/// Rebuilt 2026-09-03 on the accountant's "the coding run must settle a worker who already has a
/// bill": the cover route (worker invoices → Dext lands it → accountant authorises → marks it as
/// settlement) is the sole trader's NORMAL path, so the run's default is to FIND the worker's
/// existing bill — covered, or recognised by contact + period, draft or AUTHORISED — and recode
/// that bill's lines to the schedule's split, keeping its total, VAT treatment, status and cover.
/// Staging a draft is the exception, for the worker whose invoice hasn't arrived. A bill that
/// cannot be recoded (paid, credited, voided) skips with its status named; a second bill is
/// never staged beside one — a silent duplicate is the worst outcome because it looks like it
/// worked. Mapping gaps and unsigned weeks skip-and-report; the run never guesses a code and
/// never writes from unsigned data. Every outcome is recorded; a dry run records nothing.
/// </summary>
public sealed class RunXeroCodingEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RunXeroCodingHandler handler;
    public RunXeroCodingEndpoint(SignedInUserResolver users, RunXeroCodingHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(RunXeroCoding))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/xero-coding/run")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ManageSettlement.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var command = await request.ReadFromJsonAsync<RunXeroCoding>();
        if (command is null) return new BadRequestResult();
        if (command.Year < 2020 || command.Year > 2100 || command.Month < 1 || command.Month > 12)
            return new BadRequestResult();
        return new OkObjectResult(await handler.HandleAsync(command, signedInUser.Email, request.HttpContext.RequestAborted));
    }
}
