using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface ICommercialInputsStore
{
    IReadOnlyList<Daywork> DayworksFor(string projectId);
    Daywork LogDaywork(Daywork daywork);

    IReadOnlyList<ContraCharge> ContraChargesFor(string projectId);
    ContraCharge RecordContraCharge(ContraCharge contraCharge);

    IReadOnlyList<SubcontractorRetention> RetentionsFor(string projectId);
    SubcontractorRetention RecordRetention(SubcontractorRetention retention);

    event Action? OnChange;
}
