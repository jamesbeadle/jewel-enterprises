# Workflow 12 — Payroll

**Group:** Finance
**Purpose:** Process payroll across JBB and other Jewel entities, including timesheet capture and starter/leaver changes.
**Trigger:** Weekly/monthly payroll cut-off.
**Frequency:** Monthly for office; weekly for site where applicable.
**Owner (target):** Finance Director (approval); Brightpay for processing; JPMS for inputs.
**Current monthly hours:** ~10 h/month.
**Status:** Draft
**Last reviewed:** —

---

## Current state

1. FD chases timesheets from site team and managers.
2. Adjustments (starter, leaver, bonus, expenses) collected by email.
3. FD enters into Brightpay manually.
4. Reviewed and approved by directors.
5. Run, paid, payslips issued.

---

## Target flow (post-automation)

1. Timesheets captured automatically via site app (workflow 06).
2. Starter/leaver events from HR workflow (16) trigger payroll changes automatically.
3. Brightpay populated from JPMS data; FD reviews exceptions only.
4. Approval workflow within JPMS; pay-run executed in Brightpay.

---

## JPMS functionality required

- Timesheet capture (via site app and office check-in).
- Payroll input feed to Brightpay.
- Starter/leaver event handling.
- Approval routing.

---

## Integrations & adjacent systems

- **Brightpay** (payroll engine).
- **HR workflow** (16) — feeds starter/leaver events.
- **Site app** (06) — feeds timesheets.

---

## Acceptance criteria — "done looks like"

- FD doesn't chase timesheets.
- Starter/leaver changes hit payroll automatically.
- Payroll cycle takes hours, not days.

---

## Entities touched

`Timesheet` · `Onboarding Event` · `Cashflow Forecast` · `Project`

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| Finance Director | Owner — exception review and approval |
| Directors | Approver — final sign-off |
| Site Team | Source — timesheets |
| Office & Compliance Coordinator | Source — starter/leaver events |

---

## Open questions

- [ ] Timesheet correction window — how late can a site team member correct a submitted timesheet?
- [ ] Expenses — captured in JPMS, or separate?
- [ ] Pension and benefits — feed Brightpay directly or stay manual?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the named owner
- [ ] Current-state steps confirmed against actual practice
- [ ] Target-flow steps agreed
- [ ] JPMS functionality list confirmed as sufficient
- [ ] Integrations list confirmed
- [ ] Acceptance criteria signed off
- [ ] Signed off by: _name, role, date_
