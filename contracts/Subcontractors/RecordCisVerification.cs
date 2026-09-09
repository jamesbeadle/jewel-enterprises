using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Subcontractors;

/// <summary>Records the HMRC CIS verification result on a directory record — the status as
/// read from HMRC ("Verified 20% standard", "Gross payment", "Unverified 30%"), the verification
/// number HMRC issued and the day it was verified. The three are one fact and are written
/// together; blank number and no date are allowed for a result HMRC issued no number for.</summary>
public sealed record RecordCisVerification(
    string SubcontractorId,
    string CisStatus,
    string CisVerificationNumber,
    DateOnly? CisVerifiedOn) : ICommand<Subcontractor>;

/// <summary>The column widths the CIS fields are stored at — shared so the validation and the
/// form agree, and a too-long value is refused with a message rather than failing the save.</summary>
public static class CisVerificationLimits
{
    public const int StatusMaxLength = 64;
    public const int NumberMaxLength = 32;
}
