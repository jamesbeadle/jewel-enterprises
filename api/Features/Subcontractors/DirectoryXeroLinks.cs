using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Subcontractors;

/// <summary>The SubcontractorXeroLinks rows of a directory record as the model carries them.</summary>
internal static class DirectoryXeroLinks
{
    public static DirectoryXeroLink ToModel(this SubcontractorXeroLinkEntity link) =>
        new(link.XeroContactId, link.XeroContactName, link.ImportedAt, link.ImportedByEmail);

    public static async Task<IReadOnlyList<DirectoryXeroLink>> ForRecordAsync(
        JpmsContext context, string subcontractorId, CancellationToken cancellationToken) =>
        (await context.SubcontractorXeroLinks.AsNoTracking()
            .Where(link => link.SubcontractorId == subcontractorId)
            .OrderBy(link => link.ImportedAt)
            .ToListAsync(cancellationToken))
        .Select(ToModel)
        .ToList();

    /// <summary>Every record's links in one read, for the directory listing.</summary>
    public static async Task<Dictionary<string, IReadOnlyList<DirectoryXeroLink>>> ByRecordAsync(
        JpmsContext context, CancellationToken cancellationToken) =>
        (await context.SubcontractorXeroLinks.AsNoTracking()
            .OrderBy(link => link.ImportedAt)
            .ToListAsync(cancellationToken))
        .GroupBy(link => link.SubcontractorId, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(
            group => group.Key,
            group => (IReadOnlyList<DirectoryXeroLink>)group.Select(ToModel).ToList(),
            StringComparer.OrdinalIgnoreCase);
}
