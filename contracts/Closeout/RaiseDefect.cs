using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Closeout;

// Raise a defect on a project. The supplier it is raised with is a DIRECTORY record
// (SubcontractorId — Subcontractor / Supplier category), the way a work order names its supplier;
// AssignedToEmail is the older free-typed contact, still accepted (the Control Centre suggests the
// sender's address) and resolved to a directory record server-side when it matches one.
public sealed record RaiseDefect(
    string ProjectId,
    string Description,
    string Location,
    string AssignedToEmail,
    string? SubcontractorId = null) : ICommand<Defect>;
