using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Cvr;

public sealed record RecordCvrPackageRow(
    string ProjectId,
    string PackageName,
    decimal OrderCost,
    decimal OrderValue,
    decimal VariationCost,
    decimal VariationValue) : ICommand<CvrPackageRow>;
