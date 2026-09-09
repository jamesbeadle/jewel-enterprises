using Jewel.JPMS.Api.Features.Subcontractors.Commands;
using Jewel.JPMS.Contracts.Subcontractors;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The record page's push-to-Xero modal as a connector read (2026-09-09): what Xero holds for
/// the linked contact NOW and what it would hold AFTER push_directory_contacts_to_xero, planned
/// by the same XeroContactPushPlanner the write uses — so the connector shows the user exactly
/// what the page shows before they say yes.
/// </summary>
internal static partial class AiRecordTools
{
    private static IEnumerable<AiTool> XeroContactPushTools() => new AiTool[]
    {
        new(
            "preview_xero_contact_push",
            "What push_directory_contacts_to_xero WOULD write for one directory record: Xero's "
            + "primary person and additional persons on the linked contact now, beside the people "
            + "after (the record's primary contact → primary person, its company contacts → "
            + "additional persons, five at most; an empty side on the record leaves Xero's as it "
            + "is), plus any warnings and whether anything changes at all. Reads Xero fresh. Call "
            + "this before the push and show the user both sides.",
            AiToolSchema.Object(
                ("subcontractorId", "string", "The directory record (from search_directory).", true)),
            AiToolKind.Read,
            DirectoryXeroLinkRoles.MayLink,
            async (context, input, ct) =>
            {
                var subcontractorId = AiToolSchema.Text(input, "subcontractorId");
                if (string.IsNullOrWhiteSpace(subcontractorId)) return Fail("subcontractorId is required.");
                try
                {
                    var preview = await context.Services
                        .GetRequiredService<IQueryHandler<PreviewXeroContactPush, XeroContactPushPreview>>()
                        .HandleAsync(new PreviewXeroContactPush(subcontractorId), ct);
                    return Serialise(new
                    {
                        ok = true,
                        now = preview.Now,
                        after = preview.After,
                        warnings = preview.Warnings,
                        changesAnything = preview.ChangesAnything,
                        note = preview.ChangesAnything
                            ? "Xero's additional persons are REPLACED by the record's on push. Take the user's yes, then push_directory_contacts_to_xero."
                            : "Xero already holds these people — there is nothing to push."
                    });
                }
                catch (InvalidOperationException refusal)
                {
                    return Fail(refusal.Message);
                }
            })
    };
}
