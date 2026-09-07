using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Closeout;

// One defect by id — the defect's own page (/projects/{projectId}/defects/{defectId}). Null when
// there is no such defect. Internal-only read, like the register.
public sealed record GetDefectById(string DefectId) : IQuery<Defect?>;
