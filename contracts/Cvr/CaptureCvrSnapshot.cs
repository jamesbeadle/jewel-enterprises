using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Cvr;

public sealed record CaptureCvrSnapshot(
    string ProjectId,
    decimal TenderValue,
    decimal ForecastFinalCost,
    decimal ForecastFinalValue,
    int WeeksAheadOrBehind) : ICommand<CvrSnapshot>;
