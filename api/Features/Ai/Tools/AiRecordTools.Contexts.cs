using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Contracts.RecordLinks;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiRecordTools
{
    private static IEnumerable<AiTool> ContextTools()
    {
        var readers = JpmsRoleSets.AllInternal;

        return new AiTool[]
        {
            BidPackageContextTool(readers),

            new(
                "get_work_order_context",
                "Everything held ON a work order record, in one call: reference, status, origin "
                + "(manual, tender award, variation instruction, or migrated), supplier, title, "
                + "scope, order value, the priced lines (each with its cost code and the amount "
                + "already PAID against it — a paid line can never be removed and never priced "
                + "below what is paid), programme dates and the names of its record-keeping "
                + "attachments. Call this FIRST when editing an order (work_order_edit) or "
                + "answering questions about one; the tagged emails are separate — "
                + "read_record_emails (record_type work_order) has those, and "
                + "read_email_attachment opens their files. A DRAFT has no number yet — its "
                + "workOrderId comes from list_work_orders (status Draft), never from a guessed "
                + "reference. Accepts the id, or the reference the "
                + "user actually says (\"WO-0045\") with the project resolved from the page in "
                + "view. Defaults to the work order in view.",
                AiToolSchema.Object(
                    ("workOrderId", "string",
                        "The work order's id. Defaults to the record in view when the user is on "
                        + "its PO page.", false),
                    ("reference", "string",
                        "The human reference instead — \"WO-0045\" (or just \"45\"). Resolved "
                        + "against the project in view or projectId.", false),
                    ("projectId", "string",
                        "The project a reference is resolved in. Defaults to the project in view.", false)),
                AiToolKind.Read,
                readers,
                async (context, input, ct) =>
                {
                    var workOrderId = AiToolSchema.Text(input, "workOrderId")
                        ?? (TryMapRecordType(context.Scope?.RecordType ?? "", out var scopeType)
                            && scopeType == RecordType.WorkOrder
                            ? context.Scope?.RecordId : null);

                    Data.Entities.WorkOrderEntity? order = null;
                    if (!string.IsNullOrWhiteSpace(workOrderId))
                    {
                        order = await context.Db.WorkOrders.AsNoTracking()
                            .FirstOrDefaultAsync(row => row.WorkOrderId == workOrderId, ct);
                        if (order is null) return Fail($"No work order found with id {workOrderId}.");
                    }
                    else if (AiToolSchema.Text(input, "reference") is { } reference && !string.IsNullOrWhiteSpace(reference))
                    {
                        // "WO-0045" → 45. The number is unique per project, so a reference needs one.
                        var digits = new string(reference.Where(char.IsDigit).ToArray());
                        if (digits.Length == 0 || !int.TryParse(digits, out var number))
                            return Fail($"\"{reference}\" doesn't contain an order number — say it like WO-0045.");
                        var projectId = AiToolSchema.Text(input, "projectId") ?? context.Scope?.ProjectId;
                        if (string.IsNullOrWhiteSpace(projectId))
                            return Fail("Say which project the reference belongs to: pass projectId, or have the user open a page of that project.");
                        order = await context.Db.WorkOrders.AsNoTracking()
                            .FirstOrDefaultAsync(row => row.ProjectId == projectId && row.Number == number, ct);
                        if (order is null) return Fail($"No work order numbered {number} on project {projectId}.");
                    }
                    else
                    {
                        return Fail("Say which work order: pass workOrderId or a reference like WO-0045.");
                    }

                    var lines = await context.Db.WorkOrderLines.AsNoTracking()
                        .Where(row => row.WorkOrderId == order.WorkOrderId)
                        .OrderBy(row => row.SortOrder)
                        .Select(row => new
                        {
                            row.Title,
                            row.Description,
                            row.CostCode,
                            amount = row.LineTotal,
                            paidToDate = row.PaidToDate
                        })
                        .ToListAsync(ct);

                    var supplier = await context.Db.Subcontractors.AsNoTracking()
                        .FirstOrDefaultAsync(row => row.SubcontractorId == order.SubcontractorId, ct);

                    var attachments = await context.Db.WorkOrderAttachments.AsNoTracking()
                        .Where(row => row.WorkOrderId == order.WorkOrderId)
                        .Select(row => new { row.FileName, row.ContentType })
                        .ToListAsync(ct);

                    return Serialise(new
                    {
                        ok = true,
                        workOrderId = order.WorkOrderId,
                        projectId = order.ProjectId,
                        reference = order.Reference,
                        status = ((WorkOrderStatus)order.Status).ToString(),
                        origin = order.BidPackageId is not null ? "tender award"
                            : order.VariationOrderId is not null ? "variation instruction"
                            : order.SourceReference is not null ? "migrated"
                            : "manual",
                        supplier = supplier?.CompanyName ?? order.SubcontractorId,
                        // The directory record's id, so a session can go straight to
                        // search_directory / update_subcontractor without guessing (2026-08-29:
                        // the accountant's session had the name but no id, invented one, and
                        // misread the resulting "not found" as a broken record).
                        subcontractorId = order.SubcontractorId,
                        order.Title,
                        order.Scope,
                        value = order.Value,
                        programmeStart = order.ProgrammeStart,
                        targetCompletion = order.ScheduledCompletion,
                        programmeNotes = order.ProgrammeNotes,
                        depositRequired = order.DepositRequired,
                        depositPercent = order.DepositPercent,
                        lines,
                        attachments,
                        note = "Tagged emails are separate — read_record_emails (record_type "
                               + "work_order) returns them with full bodies and attachment ids. "
                               + "Editing: open_modal work_order_edit with this workOrderId as "
                               + "record_id; a line with paidToDate ≠ 0 can't be removed or "
                               + "priced below that figure. subcontractorId is the supplier's "
                               + "directory record — search_directory reads it, "
                               + "update_subcontractor edits it (address, contact, terms)."
                    });
                }),

        };
    }
}
