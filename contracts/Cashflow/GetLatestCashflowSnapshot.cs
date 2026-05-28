using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Cashflow;

public sealed record GetLatestCashflowSnapshot : IQuery<CashflowSnapshot?>;
