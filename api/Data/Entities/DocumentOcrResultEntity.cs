using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// What OCR read off one scanned document, kept by the SHA-256 of the file's bytes so the same
/// certificate is never sent to the OCR service twice (2026-09-09, the accountant's ask). The
/// pages are JSON — one array of lines per page — and Confidence the mean word confidence
/// across them. A different scan of the same paper is a different hash and a fresh read.
/// </summary>
public sealed class DocumentOcrResultEntity
{
    [Key, MaxLength(64)] public string ContentSha256 { get; set; } = "";
    public int PageCount { get; set; }
    public string PagesJson { get; set; } = "";
    public double Confidence { get; set; }
    [MaxLength(64)]      public string Provider { get; set; } = "";
    public DateTimeOffset CreatedAtUtc { get; set; }
}
