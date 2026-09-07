using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Closeout;

// Every field is written as posted — carry forward what should not change. SubcontractorId null
// clears the picked supplier (the legacy AssignedToEmail then stands alone).
public sealed record UpdateDefect(
    string DefectId,
    string Description,
    string Location,
    string AssignedToEmail,
    DefectStatus Status,
    string? SubcontractorId = null) : ICommand<Defect>;
