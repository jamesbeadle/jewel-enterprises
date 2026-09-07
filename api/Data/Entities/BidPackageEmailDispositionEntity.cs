using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

// A bid package's verdict on one of its tagged emails (2026-09-07; see
// Jewel.JPMS.Contracts.Procurement.BidPackageEmailDisposition). NOT a record-link: the mailbox tag
// stays the only association and is never touched here — this row says what the tender-response
// leg made of the email (Discarded = not a tender; Extracted = became QuoteId). No row = pending.
// One row per (package, email): the handler upserts by Graph id, else by the stable internet id.
public sealed class BidPackageEmailDispositionEntity
{
    [Key, MaxLength(64)] public string BidPackageEmailDispositionId { get; set; } = "";
    [MaxLength(64)]      public string BidPackageId { get; set; } = "";
    [MaxLength(512)]     public string MessageId { get; set; } = "";
    [MaxLength(512)]     public string? InternetMessageId { get; set; }

    // Maps to BidPackageEmailOutcome (1 Discarded, 2 Extracted; Pending is never stored).
    public int Outcome { get; set; }
    [MaxLength(64)]      public string? QuoteId { get; set; }
    [MaxLength(1024)]    public string Note { get; set; } = "";
    [MaxLength(256)]     public string SetByEmail { get; set; } = "";
    public DateTimeOffset SetAt { get; set; }
}
