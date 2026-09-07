using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Linkable-record provider for defects. Wraps the Defects table so a triage email can be linked to
// a defect and the defect can read its mail back live by tag (RecordEmailReader) — the same
// mechanism the Bid Package and Work Order families use, with no changes to the link/read layer or
// triage UI.
//
// Filed under JPMS/Subcontractor whichever pane raises it: TriageCategories.BucketFor maps the
// type, and the type is one type — the SAME DEF-#### record is raised from the Subcontractor pane
// (a trade's workmanship) and, since 2026-09-07, from the Supplier pane (a merchant's faulty or
// short-delivered goods). So a supplier-side defect files its thread under Subcontractor rather
// than Supplier. Deliberate for now and the one wart in that arrangement: the pathway is decided
// by record TYPE, and splitting it per raising pane would mean carrying the pane down through the
// staged picks. The defect, its DEF-#### tag and its correspondence are unaffected.
public sealed class DefectLinkProvider : ILinkableRecordProvider, ITagResolvingProvider
{
    private readonly JpmsContext context;

    public DefectLinkProvider(JpmsContext context) { this.context = context; }

    public RecordType Type => RecordType.Defect;

    // Defects own the "DEF" reference namespace.
    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[] { "DEF" };

    public async Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct)
    {
        var entities = await context.Defects.AsNoTracking()
            .Where(d => d.ProjectId == projectId)
            .OrderByDescending(d => d.Number)
            .ToListAsync(ct);
        return entities.Select(ToLinkable).ToList().AsReadOnly();
    }

    public async Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct)
    {
        var entity = await context.Defects.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DefectId == recordId, ct);
        return entity is null ? null : ToLinkable(entity);
    }

    // "DEF-0004" -> the defect numbered 4 (global sequence, same flat-tag-space rule as to-dos).
    public async Task<LinkableRecord?> FindByTagAsync(string tagReference, CancellationToken ct)
    {
        if (!TagReferenceParsing.TryParseNumber(tagReference, "DEF", out var number)) return null;
        var entity = await context.Defects.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Number == number, ct);
        return entity is null ? null : ToLinkable(entity);
    }

    private static LinkableRecord ToLinkable(DefectEntity entity)
    {
        // The defect's sequential DEF-0001 reference is the tag stem, so a triage email tagged to
        // it ("JPMS/DEF-0001") surfaces under the defect on the project's Defects tab.
        var reference = entity.Reference;

        // A defect has no title of its own: the location is what a triager reads first ("which
        // defect is the bathroom one?"), with the description as the fallback so a row is never
        // blank.
        var title = string.IsNullOrWhiteSpace(entity.Location)
            ? RecordSummaries.Clip(entity.Description) ?? reference
            : entity.Location;

        return new LinkableRecord(
            Type:         RecordType.Defect,
            RecordId:     entity.DefectId,
            ProjectId:    entity.ProjectId,
            Reference:    reference,
            TagReference: reference,
            Title:        title,
            StatusLabel:  ((DefectStatus)entity.Status).DisplayName(),
            Summary:      RecordSummaries.Clip(entity.Description),
            // Verified is the defect's closed-out state; everything before it is still being chased.
            IsActive:     entity.Status != (int)DefectStatus.Verified);
    }
}
