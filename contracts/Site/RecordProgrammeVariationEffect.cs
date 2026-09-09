using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Site;

// Records (or re-states) the days a variation pushes one programme task — the Programme tab's
// "Set programme effect" on a variation's row. Writes nothing on the variation itself.
public sealed record RecordProgrammeVariationEffect(
    string ProjectId,
    string VariationOrderId,
    string ProgrammeTaskId,
    int DelayDays,
    string Note,
    string RecordedByEmail) : ICommand<ProgrammeVariationEffect>;
