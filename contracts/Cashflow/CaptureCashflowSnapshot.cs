using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Cashflow;

public sealed record CaptureCashflowSnapshot(
    decimal ExpectedIncome13Week,
    decimal CommittedSpend13Week) : ICommand<CashflowSnapshot>;
