using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Subcontractors;

/// <summary>
/// Removes one Xero link from a directory record — the undo of
/// <see cref="LinkDirectoryRecordToXeroContact"/> (and of a mistaken import's link). Only the link
/// row goes: the record, its work orders, contacts and everything else stay. Refused when the
/// record holds no link to that contact. Audited (DirectoryRecordXeroLinkChanged).
/// </summary>
public sealed record UnlinkDirectoryRecordFromXeroContact(string SubcontractorId, string XeroContactId) : ICommand<Subcontractor>;
