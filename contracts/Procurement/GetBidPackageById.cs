using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

public sealed record GetBidPackageById(string BidPackageId) : IQuery<BidPackage?>;
