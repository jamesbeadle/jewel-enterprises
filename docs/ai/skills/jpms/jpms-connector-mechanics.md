---
name: jpms-connector-mechanics
description: "Cross-cutting mechanics of writing through the connector — the rules that stop a well-meant edit erasing data. Load with any portal write. Encodes the full-record-write rule, read-before-write, absolute figures, complete lists, draft work orders having no number, and the confirm-first protocol's spirit: confirm, then do."
---

# JPMS — Connector write mechanics

- **Full-record writes**: many update actions (update_subcontractor, update_request_details,
  update_inventory_item, update_weekly_cashflow_item, update_architect_instruction, and kin)
  replace the record's editable face WHOLE. Read the record first, change only what the user
  asked, and resend every other field exactly as read — a partial send ERASES the fields you
  omitted. When you did not read it, you may not write it.
- **Absolute figures, complete lists**: set_cost_code_budget takes absolute amounts (read
  current budgets first); set_xero_line_work_order_links and the skill/attachment savers take the
  COMPLETE new set — include everything that should remain, not just the change.
- **Draft work orders have no number** until approval — find them with list_work_orders by
  status, never by reference.
- **Confirm-first actions** (requiresConfirmation) refuse their first call by design: check for
  an existing record, show the user exactly what will happen — every value — and only send
  confirm true after their explicit yes in THIS conversation. The same spirit applies beyond the
  flag: anything financial, external-facing or irreversible gets stated first, performed second.
  **Confirm-first means confirm, then DO.** Once the user has said yes, perform the action. It
  never means handing the user a document, a markdown draft or a set of steps to do it themselves
  — a draft the portal can create in the mailbox is created by the portal.
- **Relay refusals verbatim.** Validation answers and guard messages are the portal telling you
  (and the user) what is really true — never summarise them into something softer, and never
  retry a refused call unchanged.
- **Everything is logged under the user's name.** Every call lands in Agent Activity and the
  audit trail exactly as if the user clicked it; act with that weight.
