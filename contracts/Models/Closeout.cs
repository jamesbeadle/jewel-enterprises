namespace Jewel.JPMS.Models;

public enum DefectStatus
{
    Open,
    InProgress,
    Resolved,
    Verified
}

public sealed record Defect(
    string DefectId,
    string ProjectId,
    string Description,
    string Location,
    // Legacy free-typed contact (pre-2026-09-07 rows, and the Control Centre's sender suggestion).
    // A supplier picked from the directory supersedes it — see SupplierEmail.
    string AssignedToEmail,
    DefectStatus Status,
    DateTimeOffset RaisedAt,
    DateTimeOffset? ResolvedAt,
    // Sequential human reference ("DEF-0001") — also the mailbox tag stem ("JPMS/DEF-0001"), so a
    // triage email can be filed to the defect and the defect reads its mail back live by tag.
    // Defaulted last so existing construction sites keep compiling; the server always mints it.
    string Reference = "",
    // The supplier the defect is raised with — a directory record (Subcontractor / Supplier
    // category), the way a work order names its supplier. Null = not yet assigned to a company.
    // SubcontractorName is resolved from the directory at read time, never stored.
    string? SubcontractorId = null,
    string? SubcontractorName = null,
    // When (and by whom) the defect was first SENT to the supplier from the defect's page — the
    // sent email carries the defect's tag, so the supplier's replies file themselves back under
    // it. Stamped server-side by the compose pipeline; null = never sent.
    DateTimeOffset? SentToSupplierAt = null,
    string? SentToSupplierByEmail = null,
    // The picked supplier's directory contact email, resolved server-side alongside the name.
    string SupplierContactEmail = "")
{
    /// <summary>Where a "send to supplier" email goes: the directory record's contact email when
    /// a supplier is picked, else the legacy free-typed address. Empty = nowhere to send.</summary>
    public string SupplierEmail => string.IsNullOrWhiteSpace(SupplierContactEmail) ? AssignedToEmail : SupplierContactEmail;

    /// <summary>What the register shows in the Supplier column: the directory company, else the
    /// legacy address, else nothing.</summary>
    public string SupplierLabel =>
        !string.IsNullOrWhiteSpace(SubcontractorName) ? SubcontractorName!
        : AssignedToEmail;

    public bool HasBeenSentToSupplier => SentToSupplierAt is not null;
}

public static class DefectStatusExtensions
{
    // UI wording for a status — the enum's InProgress must never leak into copy.
    public static string DisplayName(this DefectStatus status) => status switch
    {
        DefectStatus.Open       => "Open",
        DefectStatus.InProgress => "In progress",
        DefectStatus.Resolved   => "Resolved",
        DefectStatus.Verified   => "Verified",
        _ => status.ToString()
    };
}

public sealed record PracticalCompletion(
    string PracticalCompletionId,
    string ProjectId,
    DateTimeOffset AchievedAt,
    string? CertificateBlobRef,
    string IssuedByEmail,
    bool IsClientSigned);

public sealed record HandoverPackItem(
    string HandoverPackItemId,
    string ProjectId,
    string Label,
    string Detail,
    bool IsReady,
    string? EvidenceBlobRef);

public sealed record SettlementRecord(
    string SettlementRecordId,
    string ProjectId,
    decimal FinalContractValue,
    decimal FinalCost,
    decimal FinalMargin,
    DateTimeOffset AgreedAt,
    bool IsClientSigned);

public sealed record VatAnalysis(
    string VatAnalysisId,
    string ProjectId,
    decimal ZeroRatedAmount,
    decimal StandardRatedAmount,
    string Notes,
    bool IsClientConfirmed,
    bool IsArchitectConfirmed);

public sealed record RetentionRelease(
    string RetentionReleaseId,
    string ProjectId,
    decimal Amount,
    DateTimeOffset ReleasedAt,
    bool IsPublishedDownstream);
