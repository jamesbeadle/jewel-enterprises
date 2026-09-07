using Ganss.Xss;
using Jewel.JPMS.Api.Features.Labour; // SiteClock (view_labour_week's week arithmetic)
using Jewel.JPMS.Api.Features.MailboxIntake.Graph; // IIntakeMessageReader (record email reads)
using Jewel.JPMS.Api.Features.Requests; // TriageRoles (internal, same assembly)
using Jewel.JPMS.Contracts.Ai;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;


public static partial class AiToolCatalogue
{
    private static IEnumerable<AiTool> LookupTools()
    {
        var readers = JpmsRoleSets.AllInternal;

        return new List<AiTool>
        {
            new(
                "find_by_reference",
                "Look up a single record by the reference a person would say out loud — V72, RFI-049, REQ-0122, "
                + "NOD-003, TODO-0074, WO-0045, BPI-0003, DEF-0012, KPI-0003 (administrators only), or a project "
                + "reference like JBB-2026-002. "
                + "Searches variations, requests, to-dos, work "
                + "orders, bid packages, defects, KPIs and projects across every project. Tolerant of how people type: "
                + "rfi001, RFI-001, vo80, VOQ-0080, V80 and todo 74 all find their record, and a project-prefixed "
                + "reference (JBB-2026-001-REQ-0113) matches too. Use this before saying you cannot find "
                + "something — ONE call, not one per spelling. The one thing it cannot see: a DRAFT work "
                + "order, which has no number until approval — list_work_orders (status Draft) finds those.",
                AiToolSchema.Object(("reference", "string", "For example V72, rfi001, TODO-0074 or WO-0045 — as the user said it.", true)),
                AiToolKind.Read,
                readers,
                async (context, input, ct) =>
                {
                    var reference = AiToolSchema.Text(input, "reference")?.Trim();
                    if (string.IsNullOrWhiteSpace(reference)) return NotFound("A reference is required.");

                    // People say the same reference many ways — rfi001, RFI-001, vo80, VOQ-0080. Both
                    // sides are compared stripped of dashes and spaces, lower-cased, so the model never
                    // has to guess the house spelling (each miss used to cost a whole look-up round).
                    var cleaned = reference.Replace("-", "").Replace(" ", "").ToLowerInvariant();

                    // V72 / vo80 / VOQ-0080 — the number a user reads, which is
                    // VariationOrderEntity.Number per project, however they prefixed it.
                    var variationForm = System.Text.RegularExpressions.Regex.Match(cleaned, "^v(?:oq|o)?0*(\\d+)$");
                    if (variationForm.Success && int.TryParse(variationForm.Groups[1].Value, out var variationNumber))
                    {
                        var matches = await context.Db.VariationOrders
                            .AsNoTracking()
                            .Where(row => row.Number == variationNumber)
                            .Select(row => new
                            {
                                row.VariationOrderId, row.ProjectId, row.Number, row.Title,
                                row.Status, row.Value, row.VariationRef, row.RequestId, row.IssuedAt
                            })
                            .ToListAsync(ct);

                        if (matches.Count > 0)
                        {
                            var projectIds = matches.Select(row => row.ProjectId).ToList();
                            var projects = await context.Db.Projects
                                .AsNoTracking()
                                .Where(row => projectIds.Contains(row.ProjectId))
                                .ToDictionaryAsync(row => row.ProjectId, row => row.Reference, ct);

                            return Serialise(new
                            {
                                ok = true,
                                kind = "variation",
                                matches = matches.Select(row => new
                                {
                                    number = $"V{row.Number}",
                                    row.VariationOrderId,
                                    project = projects.TryGetValue(row.ProjectId, out var reference1) ? reference1 : row.ProjectId,
                                    projectId = row.ProjectId,
                                    row.Title,
                                    status = ((VariationOrderStatus)row.Status).ToString(),
                                    row.Value,
                                    row.RequestId,
                                    row.IssuedAt,
                                    route = $"/projects/{row.ProjectId}/variations/{row.VariationOrderId}"
                                })
                            });
                        }
                    }

                    // TODO-0074 / WO-0045 / BPI-0003 / DEF-0012 — the flat global stems (the
                    // mailbox-tag grammar: "PREFIX-{Number:0000}"; each Reference is computed, so
                    // the lookup is by Number). 2026-08-21: TODO-0074 came back "not found" and the
                    // model told the user to click the card it could not reach — a reference a
                    // person can read out loud must resolve here, whatever the record type.
                    var stemForm = System.Text.RegularExpressions.Regex.Match(cleaned, "^(todo|wo|bpi|def|kpi)0*(\\d+)$");
                    if (stemForm.Success && int.TryParse(stemForm.Groups[2].Value, out var stemNumber))
                    {
                        switch (stemForm.Groups[1].Value)
                        {
                            case "kpi":
                            {
                                // KPI-#### (2026-09-03): the administrators-only register. A
                                // non-administrator gets the same "not found" as a reference that
                                // does not exist — the register's contents are never confirmed
                                // to anyone else, not even by a miss.
                                if (!context.User.Roles.Contains(Role.Admin)) break;
                                var kpi = await context.Db.KpiEmails.AsNoTracking()
                                    .FirstOrDefaultAsync(row => row.Number == stemNumber, ct);
                                if (kpi is null) break;
                                var kpiPerson = await context.Db.KpiPeople.AsNoTracking()
                                    .FirstOrDefaultAsync(row => row.KpiPersonId == kpi.PersonId, ct);
                                return Serialise(new
                                {
                                    ok = true,
                                    kind = "kpi",
                                    matches = new[]
                                    {
                                        new
                                        {
                                            reference = kpi.Reference,
                                            kpiEmailId = kpi.KpiEmailId,
                                            personId = kpi.PersonId,
                                            person = kpiPerson?.Name ?? "(person removed)",
                                            personEmail = kpiPerson?.Email,
                                            subject = kpi.Subject,
                                            from = string.IsNullOrWhiteSpace(kpi.FromName) ? kpi.FromEmail : $"{kpi.FromName} <{kpi.FromEmail}>",
                                            receivedAt = kpi.ReceivedAt,
                                            note = string.IsNullOrWhiteSpace(kpi.Note) ? null : kpi.Note,
                                            markedBy = kpi.MarkedByEmail,
                                            markedAt = kpi.MarkedAt,
                                            messageId = kpi.MessageId,
                                            internetMessageId = kpi.InternetMessageId,
                                            route = "/admin/kpis"
                                        }
                                    },
                                    note = "Administrators only — never repeat to anyone else. Read the email with "
                                        + "get_mailbox_message(messageId, internetMessageId); the whole register is list_kpi_emails."
                                });
                            }
                            case "todo":
                            {
                                var items = await context.Db.TodoItems
                                    .AsNoTracking()
                                    .Where(row => row.Number == stemNumber)
                                    .ToListAsync(ct);
                                if (items.Count > 0)
                                {
                                    var projects = await ProjectReferenceMapAsync(context, items.Select(row => row.ProjectId), ct);
                                    return Serialise(new
                                    {
                                        ok = true,
                                        kind = "todo",
                                        matches = items.Select(row => new
                                        {
                                            reference = row.Reference,
                                            todoItemId = row.TodoItemId,
                                            row.Title,
                                            notes = string.IsNullOrWhiteSpace(row.Notes) ? null : row.Notes,
                                            status = row.IsComplete ? "Done" : row.StartedAt is null ? "Open" : "In progress",
                                            assignee = row.AssigneeRole is { } assigneeRole
                                                ? ((Role)assigneeRole).ToString()
                                                  + (string.IsNullOrWhiteSpace(row.AssigneePersonEmail) ? "" : $" — {row.AssigneePersonEmail}")
                                                : "Unassigned",
                                            due = row.DueAt,
                                            project = string.IsNullOrWhiteSpace(row.ProjectId)
                                                ? "company-wide"
                                                : projects.TryGetValue(row.ProjectId, out var todoProject) ? todoProject : row.ProjectId,
                                            projectId = string.IsNullOrWhiteSpace(row.ProjectId) ? null : row.ProjectId,
                                            route = $"/todos/{row.TodoItemId}"
                                        }),
                                        note = "Its tagged emails: read_record_emails record_type todo with this id. "
                                            + "Actioning an item usually means doing the work it names, not just opening "
                                            + "it — e.g. a \"raise this WO\" item: read its tagged emails, then open_modal "
                                            + "work_order_create with the item's projectId."
                                    });
                                }
                                break;
                            }
                            case "wo":
                            {
                                var orders = await context.Db.WorkOrders
                                    .AsNoTracking()
                                    .Where(row => row.Number == stemNumber)
                                    .ToListAsync(ct);
                                if (orders.Count > 0)
                                {
                                    var projects = await ProjectReferenceMapAsync(context, orders.Select(row => row.ProjectId), ct);
                                    return Serialise(new
                                    {
                                        ok = true,
                                        kind = "work_order",
                                        matches = orders.Select(row => new
                                        {
                                            reference = row.Reference,
                                            row.WorkOrderId,
                                            row.Title,
                                            status = ((WorkOrderStatus)row.Status).ToString(),
                                            row.Value,
                                            project = projects.TryGetValue(row.ProjectId, out var orderProject) ? orderProject : row.ProjectId,
                                            projectId = row.ProjectId,
                                            route = $"/projects/{row.ProjectId}/work-orders"
                                        }),
                                        note = "get_work_order_context reads the order's origin, lines and attachments; "
                                            + "read_record_emails record_type work_order reads its correspondence; the "
                                            + "work_order_edit dialog corrects it."
                                    });
                                }
                                break;
                            }
                            case "bpi":
                            {
                                var packages = await context.Db.BidPackages
                                    .AsNoTracking()
                                    .Where(row => row.Number == stemNumber)
                                    .ToListAsync(ct);
                                if (packages.Count > 0)
                                {
                                    var projects = await ProjectReferenceMapAsync(context, packages.Select(row => row.ProjectId), ct);
                                    return Serialise(new
                                    {
                                        ok = true,
                                        kind = "bid_package",
                                        matches = packages.Select(row => new
                                        {
                                            reference = row.Reference,
                                            row.BidPackageId,
                                            row.Title,
                                            status = ((BidPackageStatus)row.Status).ToString(),
                                            project = projects.TryGetValue(row.ProjectId, out var packageProject) ? packageProject : row.ProjectId,
                                            projectId = row.ProjectId,
                                            route = $"/projects/{row.ProjectId}/bid-package-invites/{row.BidPackageId}"
                                        }),
                                        note = "get_bid_package_context reads the package's detail; read_record_emails "
                                            + "record_type bid_package reads its tender correspondence."
                                    });
                                }
                                break;
                            }
                            case "def":
                            {
                                var defects = await context.Db.Defects
                                    .AsNoTracking()
                                    .Where(row => row.Number == stemNumber)
                                    .ToListAsync(ct);
                                if (defects.Count > 0)
                                {
                                    var projects = await ProjectReferenceMapAsync(context, defects.Select(row => row.ProjectId), ct);
                                    return Serialise(new
                                    {
                                        ok = true,
                                        kind = "defect",
                                        matches = defects.Select(row => new
                                        {
                                            reference = row.Reference,
                                            row.DefectId,
                                            row.Description,
                                            row.Location,
                                            status = ((DefectStatus)row.Status).ToString(),
                                            project = projects.TryGetValue(row.ProjectId, out var defectProject) ? defectProject : row.ProjectId,
                                            projectId = row.ProjectId,
                                            route = $"/projects/{row.ProjectId}/defects/{row.DefectId}"
                                        }),
                                        note = "read_record_emails record_type defect reads its tagged mail; list_todos aboutRecordId lists the to-dos about it."
                                    });
                                }
                                break;
                            }
                        }

                        return NotFound($"Nothing found with reference {reference}. Say so — do not guess at a similar record.");
                    }

                    // JBB-2026-002 — a PROJECT reference (2026-08-31: it came back "not found" and
                    // the model had to fall back to list_projects). Checked before the request scan
                    // because every request reference is project-PREFIXED, so a bare project
                    // reference can never collide with a request's suffix match below.
                    var projectMatches = await context.Db.Projects
                        .AsNoTracking()
                        .Where(row => row.Reference.Replace("-", "").Replace(" ", "").ToLower() == cleaned)
                        .Select(row => new { row.ProjectId, row.Reference, row.Name, row.ClientName, row.Stage })
                        .ToListAsync(ct);
                    if (projectMatches.Count > 0)
                    {
                        return Serialise(new
                        {
                            ok = true,
                            kind = "project",
                            matches = projectMatches.Select(row => new
                            {
                                row.Reference,
                                projectId = row.ProjectId,
                                row.Name,
                                client = string.IsNullOrWhiteSpace(row.ClientName) ? null : row.ClientName,
                                stage = ((ProjectStage)row.Stage).ToString(),
                                route = $"/projects/{row.ProjectId}"
                            }),
                            note = "The projectId is what every project-scoped tool and route takes; "
                                + "list_projects carries the full live list."
                        });
                    }

                    // Normalised equality first, then suffix — so "REQ-0113" also finds a stored
                    // project-prefixed "JBB-2026-001-REQ-0113". Replace/ToLower/EndsWith all
                    // translate to SQL, so this stays one indexed-table scan, not a client fetch.
                    var requests = await context.Db.Requests
                        .AsNoTracking()
                        .Where(row =>
                            row.Reference.Replace("-", "").Replace(" ", "").ToLower() == cleaned
                            || row.Reference.Replace("-", "").Replace(" ", "").ToLower().EndsWith(cleaned))
                        .Select(row => new
                        {
                            row.RequestId, row.ProjectId, row.Reference, row.Title,
                            row.Kind, row.Status, row.Value, row.ResponseDue
                        })
                        .Take(20)
                        .ToListAsync(ct);

                    if (requests.Count == 0)
                        return NotFound($"Nothing found with reference {reference}. Say so — do not guess at a similar record.");

                    return Serialise(new
                    {
                        ok = true,
                        kind = "request",
                        matches = requests.Select(row => new
                        {
                            row.Reference,
                            row.RequestId,
                            projectId = row.ProjectId,
                            row.Title,
                            kind = ((RequestType)row.Kind).ToString(),
                            status = ((RequestStatus)row.Status).ToString(),
                            row.Value,
                            row.ResponseDue,
                            route = $"/projects/{row.ProjectId}/requests/view/{row.RequestId}"
                        })
                    });
                }),
        };
    }
}
