# Draft programme update from the certified valuation — design spec

**Status:** Built 2026-09-08 (awaiting build verification and the migration)
**Author:** Cowork (for Nigel Reilly)
**Decisions (Nigel, 2026-09-08):** open the draft on approval *and* by hand · match by rules
first, Claude second, the reviewer confirms and the confirmation is saved per task · the draft
proposes progress percentages only (planned dates untouched)

---

## 1. What it does

When the architect certifies a valuation — in the portal, **Record approval** on the claim card
(the valuation invoice goes Submitted → Approved) — the claim's line percentages are final. At
that moment the API opens a **draft programme update**: every programme task with the progress
the valuation says it has reached, held for a person on the Programme tab to review. Nothing on
the live programme changes until the reviewer applies the draft.

The valuation is priced by **cost centre** (MEC-PLM Plumber, 11 lines) while the programme is
planned by **place and trade** ("First Floor — Plumbing 2nd Fix"), so the two never map exactly.
The draft bridges them in three passes, and every line shows which pass mapped it:

1. **Saved** — the task's confirmed cost centres from an earlier draft (kept on the task when a
   draft is applied; `ProgrammeTaskCostCentres`).
2. **Rule** — the trade-word rulebook (`ProgrammeCostCentreRules`, contracts): a task title is
   "where — what"; the *what* is a word the rulebook knows ("plumbing" → MEC-PLM, "window" → the
   WDR family). A rule names the whole family a word could mean; the claim narrows it to the codes
   that actually carry priced lines, so "Window Installation" lands on WDR-ALU alone when
   aluminium is the only window line priced. A longer phrase beats any shorter one it contains
   ("carpentry 2nd fix" silences "carpentry").
3. **Claude** — asked once, automatically, when the review pane first opens a draft with tasks
   still unmatched (and again from the toolbar on demand). It sees the task titles and each cost
   centre's code, name, completion and a few of its line descriptions, and answers strict JSON.
   Unconfigured / failed / unparsable degrades to a sentence on the draft, never an error.

The reviewer can remap any task (tick the centres in the inline editor — **Person**), type their
own percentage, untick a task, discard the draft, or apply it.

## 2. The proposal

A task's proposed progress is the £-weighted completion of the valuation lines on its cost
centres (`ProgrammeProgressProposal`, contracts — pure, unit-tested):

```
Proposed %  = Σ cumulative claimed on the claim ÷ Σ line amount   × 100     (one decimal, 0–100)
              over every Priced / Provisional-Sum line with a positive amount on the task's codes
```

Omits (negative), Declined and TBC lines never take part. Variation additions do — they carry a
cost code like any line. A line the claim holds no entry for is 0 claimed (the report's own
rule). Tasks sharing a cost centre are offered the same figure; the floor-by-floor split is the
reviewer's call (the evidence text on each line names the centre, its line count and £ claimed
of £ amount so the figure can be checked against the report in one glance).

## 3. Records

| Table | Row |
|---|---|
| `ProgrammeDrafts` | One per opening: project, claim (and its display name at the time), status Open / Applied / Discarded / Superseded, who opened it and when, who closed it and when, when Claude was asked and what it said |
| `ProgrammeDraftLines` | One per programme task at opening: title (copied), progress then, the proposal, the mapping (`CostCodes`, comma-separated), its source (None / Saved / Rule / Claude / Person), the evidence text, the tick, the reviewer's figure |
| `ProgrammeTaskCostCentres` | The saved mapping: one row per task per cost code, replaced when a draft is applied |

Only the newest draft on a project is ever Open — opening another supersedes it (never deleted:
it is the record of what an earlier claim proposed). Migration `20260908150000_AddProgrammeDrafts`.

## 4. Flow

```
Valuation tab: Record approval ──▶ ApproveValuationInvoiceHandler
                                     └─ commits the approval, THEN opens the draft in its own unit
                                        of work (best effort: no tasks → no draft; a failure is
                                        logged, never refuses the approval)
Programme tab ──▶ "Draft from valuation…" door ──▶ DraftProgrammeFromValuation (any locked claim)

Programme tab, Notice "Draft programme update from <claim> awaits review" ──▶ Review draft
   ProgrammeDraftReview (jpms/Features/Site/Programme):
     · asks Claude once for the unmatched tasks (SuggestProgrammeDraftMappings)
     · tick / figure / mapping per line (ReviewProgrammeDraftLine)
     · Apply N changes (ApplyProgrammeDraft) — writes ProgressPercent on the ticked tasks, saves
       every mapped line's cost centres on its task, closes the draft
     · Discard draft (DiscardProgrammeDraft) — closes it, programme untouched
```

Roles: reading the draft is any internal role (as the programme); every draft command is the
programme-editing roles (Director, Project Manager). The MCP connector's `get_programme` now
also returns each task's saved mapping and the open draft, so the assistant can say what the
valuation proposes and what still needs a hand.

## 5. Code

- **contracts** — `Models/ProgrammeDraft.cs` (records, enums), `Models/ProgrammeProgressProposal.cs`
  (the maths), `Models/ProgrammeCostCentreRules.cs` (the rulebook), `Site/ProgrammeDrafts.cs`
  (query + five commands); `ProgrammeDetail` gains `CostCentres`.
- **api** — `Features/Site/Drafts/*` (capture, reader, claim centres, line proposal, Claude prompt),
  `Features/Site/Commands/*ProgrammeDraft*`, `Features/Site/Queries/GetOpenProgrammeDraft*`, the
  hook in `ApproveValuationInvoiceHandler`, entities in `CommercialEntities.cs`, DbSets, indexes,
  the migration and its snapshot entries; `RemoveProgrammeTask` and `DeleteProject` cascade.
- **jpms** — `Features/Site/Programme/ProgrammeDraftReview.razor(.cs)` (the review pane),
  `ProgrammeWorkbench` (the notice, the door, the hand-off), routes in `SiteRouteRegistration`,
  tones in `StatusTones`, the post-approval notice on `ProjectValuation`.
- **tests** — `ProgrammeCostCentreRulesTests`, `ProgrammeProgressProposalTests`,
  `ProgrammeDraftTests` (in-memory capture → review → apply / discard / supersede).

## 6. Not in this cut

- Re-forecasting planned end dates from lagging progress (decision: progress only).
- Sequence-aware spreading of one centre's completion across the tasks that share it (earlier
  tasks fill first) — the reviewer does this by hand today; worth adding if drafts keep needing
  the same edits.
- Location-aware matching (reading "Ground Floor" against line descriptions such as "to GF").
