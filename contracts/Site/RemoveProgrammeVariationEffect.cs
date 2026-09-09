using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Site;

public sealed record RemoveProgrammeVariationEffect(string ProgrammeVariationEffectId) : ICommand<Acknowledgement>;
