# 07a — CVR engine readiness (Phase 3, pre-implementation)

The CVR is the first commercial output to build. Per the delivery rule, every Forecast Final
Cost must be the explicit sum of its components and no number may be invented. This note
separates what workflow 07 **pins down** (safe to implement) from what it **leaves open**
(needs a decision before code), and lists the **input entities that don't yet exist**.

## Pinned — safe to implement as-is

- **Forecast Final Cost** = Cost Incurred + Cost Committed + QS Accruals + Prelim Forecast +
  Cost to Complete. A transparent sum of five stored components; drillable. (US-07-23)
- **Combined per package** = Order + Variation, for each of Cost, Value, Profit £. (US-07-33)
- **Profit £** = Value − Cost (per package, for Order / Variation / Combined).
- **Movement** = current value − prior CVR snapshot value, per line. (US-07-26)
- **CVR snapshot** retained per Claim Period. (US-07-27)
- **Cost Committed** = open work orders not yet invoiced; an approved Variation feeds Committed
  and only moves to Incurred when invoiced. (US-07-25)
- **Time control header** fields and **Weeks Ahead/Behind** = Anticipated Completion − Contract
  Completion (in weeks). (US-07-29)

## Open — needs your decision before implementation

1. **Profit % basis** — Profit % = Profit ÷ Value, or Profit ÷ Cost? (Affects every package row.)
2. **Weekly prelim run rate** for time-related overspend (US-07-30) — derived automatically from
   the Prelim Forecast week×item grid (mean weekly tendered prelim), or set per project? (Open
   question already flagged in workflow 07.)
3. **Cost to Complete** — "remaining contract scope × current rate-card position": is "remaining
   scope" = (1 − site-reported %) × tendered cost per package, or BoQ-quantity-remaining × current
   rate? The two diverge once rates move.
4. **Order Cost vs Order Value per package** — Value = tendered sell price for the package;
   Cost = ? (tendered cost / budget for the package, i.e. sell − tender margin). Confirm the
   source field for package Cost.
5. **Cost-code overrun policy** (US-07-20/21) — hard-block vs soft-warn-with-FD-sign-off, and the
   two threshold values (FD vs Director).
6. **Completion-% model** (US-07-43) — site-reported %, timesheet burn rate, or blended; if
   blended, the weighting.

## Input entities missing from the schema (block "Cost Incurred")

`Cost Incurred` = WO invoices applied + approved day-rate timesheets + **dayworks + contras +
retention movements**. Present in `JpmsContext`: `WorkOrders`, `Timesheets`. **Absent:**
`Daywork`, `ContraCharge`, `SubcontractorRetention`, and any "work-order invoice / payment
application" entity. These must be modelled (Phase-3 schema step) before Cost Incurred is real.

## Proposed build order once decisions are in

1. Add the missing input entities + their capture commands (daywork, contra, retention movement,
   WO invoice application).
2. CVR calculation engine as pure, unit-tested functions over those inputs (one worked example
   per output, hand-checkable).
3. `CaptureCvrSnapshot` command persisting a `CvrSnapshot` + its `ForecastComponent`/`MarginTrace`
   rows; `Movement` computed against the prior snapshot.
4. Wire `/projects/{id}/commercial` to the engine, replacing the (now-removed) seed snapshots.

Nothing in steps 1–4 is coded until the six decisions above are answered, because each one
changes a number the QS will check by hand.
