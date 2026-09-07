using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Closeout;

/// <summary>
/// The directory side of a defect: which company it is raised with. One read for a page of
/// defects (the register) or one defect (its page), and the two rules every defect write shares —
/// a picked supplier must be a real, non-prospect directory record, and a legacy free-typed
/// address that matches a directory contact email is promoted to that record so the Control
/// Centre's "sender is the subcontractor" suggestion lands on the same company the picker would.
/// </summary>
internal static class DefectSupplierLookup
{
    public static async Task<IReadOnlyDictionary<string, SubcontractorEntity>> ForAsync(
        JpmsContext context, IEnumerable<DefectEntity> defects, CancellationToken cancellationToken)
    {
        var ids = defects.Select(d => d.SubcontractorId).Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id!).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<string, SubcontractorEntity>();
        var rows = await context.Subcontractors.AsNoTracking().Where(s => ids.Contains(s.SubcontractorId)).ToListAsync(cancellationToken);
        return rows.ToDictionary(s => s.SubcontractorId, StringComparer.Ordinal);
    }

    public static async Task<SubcontractorEntity?> OneAsync(JpmsContext context, string? subcontractorId, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(subcontractorId) ? null
        : await context.Subcontractors.AsNoTracking().FirstOrDefaultAsync(s => s.SubcontractorId == subcontractorId, cancellationToken);

    /// <summary>Resolves the supplier a write names: the explicit id (checked to exist; a prospect
    /// or an unknown id is refused with the fix in the message), else a directory record whose
    /// contact email equals the free-typed address, else none.</summary>
    public static async Task<string?> ResolveAsync(
        JpmsContext context, string? subcontractorId, string assignedToEmail, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(subcontractorId))
        {
            var picked = await context.Subcontractors.AsNoTracking()
                .FirstOrDefaultAsync(s => s.SubcontractorId == subcontractorId, cancellationToken);
            if (picked is null)
                throw new InvalidOperationException("That supplier isn't in the directory — pick one from the list.");
            if (picked.IsProspect)
                throw new InvalidOperationException(
                    $"{picked.CompanyName} is a tender-list prospect, not a directory record — add it to the directory first.");
            return picked.SubcontractorId;
        }
        var email = assignedToEmail.Trim();
        if (email.Length == 0) return null;
        var match = await context.Subcontractors.AsNoTracking()
            .Where(s => !s.IsProspect && s.ContactEmail == email)
            .Select(s => s.SubcontractorId)
            .FirstOrDefaultAsync(cancellationToken);
        return match;
    }
}
