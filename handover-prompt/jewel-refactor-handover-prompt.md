# Jewel portal refactor — handover prompt (paste this whole file as the first message of the fresh chat; §0 is current as of 7 Sep 2026)

You are continuing a staged, audit-measured refactor of my C#/Blazor WASM construction portal,
**jewel-portal** (`api/` Azure Functions, `jpms/` Blazor WebAssembly client, `contracts/`,
`worker/`, `tests/`). Fifteen rounds are done. This prompt carries everything a previous chat and I
agreed, from the original guidelines to the current state, so that you can run round 16 and onward
exactly the way rounds 12–15 ran. Read it all before touching anything, then follow **§1 Setup**,
then wait for me to say **"begin"**, **"ok next round"** or **"keep refactoring"** — each of those
starts one round. **"deliver to machine"** means run §8 for the current round. We keep going until
the code is perfect.

The doctrine lives in the repository and is authoritative — read these four files first, in this
order, before your first commit: `tools/refactor/playbook.md`, `tools/refactor/README.md`,
`tools/refactor/rules.json`, `docs/refactor/design-patterns.md`. Then `tools/refactor/baseline-report.md`
(the current report, v16). `docs/Drawing-the-Line.pdf` is the 22 August briefing the whole
programme answers to; `CLAUDE.md` at the root carries the product's house conventions
(terminology, loading states, toolbars, error reporting) that every extracted component must respect.

---

## 0. State at 7 September 2026 — read this first; where it disagrees with §1, §5, §8, §9 or §10 below, this wins

**Baseline is v21, after round 19** (`tools/refactor/baseline-report.md`, 7 Sep). Nineteen rounds
are done, not fifteen. The 2 September figures in §9 are history; the current report carries the
journey table and names round 20.

**Branches and commits no longer exist in this programme.** James works on `main` only and does his
own git. You do not create branches, do not commit, do not push, do not produce patch tarballs.
You change the files, verify, and write the changed files straight into his project folder
(`/Users/ybt/Documents/Claude/Projects/jewel-portal` — note the path; §8's `/Users/james/…` is
stale) with `device_commit_files`, one call of up to 50 files, `stagedPath` under
`/mnt/user-data/outputs/`. Files you delete cannot be deleted through the bridge: give him ONE
copy-paste line (`git rm -q <paths>`, no `cd` prefix) and say so. Then he commits and pushes.
The `device_bash` mount of the project folder has been dead since 6 September ("failed to
mount") — use `device_list_dir` / `device_stage_files` / `device_commit_files` by absolute path
instead, and expect the bridge to drop for minutes at a time (retry once, carry on, deliver when
it returns; put the deliverables under `/mnt/user-data/outputs/` meanwhile so nothing is lost).

**Packages (7 Sep recipe).** nuget.org is blocked from the cloud. James packs his Mac's cache with
ONE line — `tar cz -C ~/.nuget packages | split -b 60m - nuget-cache.tgz.p-` — run from the
project folder (chunks ≤ 60 MB; a 380 MB chunk times out the bridge), you stage the chunks
(`device_stage_files`, up to three per call), `cat … | tar xz -C ~/.nuget`, and restore offline
with an untracked `nuget.config` that clears every source (§1). `.gitignore` already ignores
`nuget-cache.tgz*`. His cache holds what the api, contracts and the `dotnet ef` design-time need —
**it does NOT hold `Microsoft.AspNetCore.Components.WebAssembly` (so the jpms Verify project
cannot restore), nor xunit / `Microsoft.NET.Test.Sdk` / `Microsoft.EntityFrameworkCore.InMemory`
(so `dotnet test` cannot run), nor three worker packages (`ApplicationInsights.WorkerService`,
`Functions.Worker.Extensions.Storage.Queues`, `…Extensions.Timer`).** Until one `dotnet build
jpms` and one `dotnet test` on his Mac put those in the cache, the cloud can verify `api` and
`contracts` only. Round 19 ran its 58 new tests through a stand-in harness — a ~100-line shim
for xunit's attributes and `Assert`, and the EF Core InMemory provider built from the
`dotnet/efcore` v8.0.10 source (`src/EFCore.InMemory` + `src/Shared`, one csproj referencing the
cached `Microsoft.EntityFrameworkCore` 8.0.10) — 84/84 green, including three existing test
files. Rebuild that harness if the cache is still short; say in the report that the tests want a
real xunit run (`dotnet test tests/Jewel.JPMS.Tests/Jewel.JPMS.Tests.csproj` on the Mac, or the
manual Tests workflow on GitHub).

**What this means for targets.** The worst files are now jpms pages (`SalesStrategyDetail` 525,
`SalesLeadDetail` 505, `Imagine` 464, `SalesInbox` 457, `AdminKpis` 429) and they cannot be built
in the cloud until the WebAssembly package is in the cache. Do not divide Razor you cannot
compile. Worst-first among buildable files is the rule until then; the current report's
"Round 20, named" lists both queues.

**The worker compile list (§4) still bites.** `worker/Jewel.JPMS.Worker.csproj` compiles api
sources by name; round 19 added the eight new `XeroClient.*` partials beside `.Writes` but could
not build the worker (packages above). Check it every time an api file in that list is split.

**The audit report now leads with the headline** ("N of M source files are over the 100-line
limit (x%)") and a "vs baseline" table — `tools/refactor/audit/report.py`, round 19. James asked
for files-over-100 to be stated plainly every time; keep it first in `baseline-report.md` too.

---

## 1. Setup (do this first, once)

**Get the repository.** Clone `https://github.com/jamesbeadle/jewel-portal.git` into your cloud
workspace (the previous session cloned and fetched it without any credential of its own). **Pushes
do not work** unless I have added the repository to the session's sources — try
`git push --dry-run origin main` once, and if the proxy refuses, deliver by patch tarball as in §8
(that has been the method all along). Never push or open a PR on my behalf unless I say so; I merge
each round myself. This prompt file itself (`handover-prompt.md`, wherever I keep it) is not part of
the repository — never commit it.

**Get the round-15 branch.** On 2 September `origin/main` was PR #18 (round 14, baseline v15).
Round 15 (`refactor/round-15`, tip "Baseline v16: labour, programme, and the one Apply" — `9ace0fb`
on my Mac, `a37af9d3` in the old cloud clone; `git am` re-stamps hashes, so identify commits by
title and trees by the parity hash `60d8b1516791abdfbc8a58e603c1cac7`) existed only on my Mac. If, when you clone, `origin/main` contains a commit titled
"Baseline v16 …", round 15 has been merged and you work from `main`. If not, ask me to merge it, or
pull it from my Mac through the device bridge:

```
# on my Mac (device_bash), inside the connected project folder:
git fetch -q origin && git bundle create round-15.bundle origin/main..refactor/round-15
# stage round-15.bundle into the cloud (it lands under the uploads directory), then in the clone:
git fetch /path/to/round-15.bundle refactor/round-15:refactor/round-15
```

Also recreate the earlier round branches you need for `format-patch` ranges: `refactor/round-15`
is all §8 needs for round 16 (`refactor/round-15..refactor/round-16`).

Branch chain in this refactor: `main → refactor/round-2 → … → refactor/round-15`; every round
branch is created off the previous round's tip, and each round is one PR into `main`.

**Recreate the verification project.** The real client project `jpms/Jewel.JPMS.csproj` does not
build in the cloud (Tailwind/npm static assets), so every build check uses a stand-in project that is
deliberately untracked. Create `jpms/Jewel.JPMS.Verify.csproj` with exactly this content:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>Jewel.JPMS</RootNamespace>
    <AssemblyName>Jewel.JPMS.Verify</AssemblyName>
    <OutputType>Exe</OutputType>
    <UseAppHost>false</UseAppHost>
    <BaseIntermediateOutputPath>obj-verify\</BaseIntermediateOutputPath>
    <OutputPath>bin-verify\</OutputPath>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <StaticWebAssetsEnabled>false</StaticWebAssetsEnabled>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\contracts\Jewel.JPMS.Contracts.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Content Remove="package.json;tailwind.config.js;Styles\**;node_modules\**;wwwroot\**" />
    <Compile Remove="node_modules\**;bin\**;obj\**" />
    <None Remove="node_modules\**" />
  </ItemGroup>

</Project>
```

and keep it out of git by appending to `.git/info/exclude` (never to `.gitignore`):

```
jpms/Jewel.JPMS.Verify.csproj
jpms/obj-verify/
jpms/bin-verify/
```

**Tooling.** .NET 8 SDK (`dotnet --version` → 8.0.x; it was preinstalled last time — if it is
missing, tell me before anything else), Python 3.10+, and `npm install -g jscpd` (the duplication
check silently skips without it — a report with duplication missing is wrong).

**NuGet — read this before the first restore.** `api.nuget.org` was **blocked (403)** from the
cloud workspace in the previous session, so the packages came from my Mac: `~/.nuget/packages`
on the Mac is a connected folder alongside the project, and the cloud restores offline from its
own `~/.nuget/packages` with an untracked `nuget.config` at the repository root that clears every
source (add `nuget.config` to `.git/info/exclude` too):

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
  </packageSources>
</configuration>
```

Try `curl -sI https://api.nuget.org/v3/index.json` first; if it answers 200, restore normally and
skip the rest of this paragraph. If it is 403, the 342 package folders the five projects need
(contracts, jpms Verify, api, worker, tests — about 1.0 GB) are listed in
`nuget-packages-needed.txt`, which travels with this prompt. Put that list on the Mac, then in
`device_bash` tar the folders from the Mac's `~/.nuget/packages` in three or four parts of under
350 MB each into the project folder (`tar czf …/nuget-part1.tgz -C <mac packages dir> -T part1.txt`
with the list split by `split -l 100`), stage each part into the cloud one call at a time (the
bridge has a ~50 s budget per call and a 400 MB per-file cap), extract them into
`~/.nuget/packages`, then `dotnet restore` each project. Move the tarballs to `_to_delete_*.tgz`
on the Mac afterwards (deletes are usually refused there). If the bridge is offline, ask me either
to add `api.nuget.org` to the organisation's network allowlist or to bring the Mac online — do not
start refactoring without a green build.

Then prove the loop works before any refactoring:
`dotnet build jpms/Jewel.JPMS.Verify.csproj --no-incremental` (0 errors),
`dotnet test tests/Jewel.JPMS.Tests` (**434 passed**), and the audit + gate from §3 must pass
clean against the committed `tools/refactor/baseline.json`. If the figures you get differ from the
v16 Summary in §9, stop and tell me — something in the environment differs.

---

## 2. The doctrine (the method, as originally given and as applied)

**The engine, in one sentence (playbook): shrink first, abstract second, let patterns arrive
third.** "On the backend, big files are broken into functions each with one purpose; in the UI, big
pages are broken into components each with one purpose. Abstraction is never attempted against a
large unit — it is only once the units are small that common functionality becomes visible, and only
once enough commonality has been extracted that design patterns appear. … the abstractions
themselves are discovered from the shrunken code, never imposed on the large." Name a pattern only
after it has already formed (`docs/refactor/design-patterns.md` is where they get named).

**The extraction loop (playbook Stage 4) is the round.** Worst file first, from the `fileLength`
offender list: pick the largest file over the limit; extract each section that has one nameable
purpose into a component (page markup) or a partial/class (logic); behaviour must not change — same
rendered output, same events, tests still green; re-run the audit; lock the baseline. "The loop is
deliberately boring." A page refuses to shrink only when it is two pages or the taxonomy is missing a
widget — go up a stage, never force it.

**Code is prose.** A reader looking for one concern opens one file and reads nothing else; the
filename is the concern's name (`ProjectRequestDetail.DetailEdit.cs`, `TriageQueue.Apply.cs`);
call sites read as sentences (`@Money(total)`, `@Day(Order.IssuedAt)`); line-pure rules live in a
display module and are called by name. Comments are for doctrine the code cannot carry — when a
component's name now says what a comment said, the comment comes off. Function names: at most 5
words / 40 characters.

**Simplest division.** Division of two, repeated: a file divides at its seam into two named things;
a page becomes a tab bar and its panes; a table becomes its row family. Never invent an
abstraction to make a split possible — if the split needs one, the split is wrong.

**DRY with care.** Merge a clone family only when it is the same *concept*; coincidentally similar
code stays where it is (same name, different concept is allowed). Check clone pairs with jscpd
before claiming a duplication move either way.

**Nothing is deleted unless its functionality is certainly handled elsewhere.** Before removing any
member, grep every caller across `jpms/`, `api/`, `worker/`, `tests/`; "no caller anywhere" is the
standard, and the commit message says so. A refusal path, a reset, a seeded draft, a `wasOpen`
guard — each is behaviour, and it moves with the code that owned it.

**The numbers must move every round — that is the point.** After round 11 I said: "No numbers
improved on that round… if the biggest file is triage queue it seems obvious that these fragments
become components — what am I not getting?" The answer was that "needs design" had become an
excuse for not doing the obvious extraction. Since then the rule is: if the worst file is a page,
its markup becomes components this round, with explicit `[Parameter]` interfaces however wide they
are. A wide interface is honest; a 900-line page is not. Do not defer the biggest file.

**Feature freeze.** Bug fixes I ask for, yes; net-new behaviour, never. Respect `CLAUDE.md`
(Programme not Schedule; valuation invoice; one variation with one number; `LoadGate`/`LoadState`
loading rules — never gate a control or a single line of text; `Toolbar` for in-view actions; the
`ErrorReporter` toast for failures; stale-while-revalidate stores).

---

## 3. The audit ratchet

Run from the repository root:

```
python3 -m tools.refactor.audit.run_audit . --output tools/refactor/audit-output
python3 -m tools.refactor.audit.gate tools/refactor/baseline.json tools/refactor/audit-output/audit.json
```

`audit.json` has `summaries` (per-check figures) and `details` (per-check `offenders`:
`fileLength.offenders[{file,lines}]`, `comments.offenders[{file,line}]`,
`prose.offenders{memberChains,deepIndentation,overlongLines}`,
`duplication.offenders[{file,duplicatedLineInstances}]`, …). Offender lists are capped, so a
per-file diff between two audits can miss changes — when a figure moves and you need to know why,
recompute the heuristic directly over the files you touched.

**Ratcheted figures (gate.py — any increase fails):** `fileLength.filesOverLimit`,
`fileLength.worstFileLines`, `functionShape.functionsOverLimit`, `functionShape.elseBlocks`,
`duplication.duplicatedPercentage`, `comments.explanatoryCommentLines`,
`magicValues.inlineHexColours`, `inventory.orphanComponents`, `prose.longMemberChainLines`,
`prose.deeplyIndentedLines`, `functionNames.overlongFunctionNames`.

**How the heuristics count, so you can hold them:** comments = lines starting `//` (not `///`) or
the first line of an `@* … *@` block; `else` = `^\s*}?\s*else\b`; member chains = more than three
`\w\.\w` hits on one line — a `@using static A.B.C.D` line counts, so each new one must be paid
for by breaking a chained read elsewhere; function names > 5 words or > 40 chars; duplication =
jscpd at `--min-tokens 70` (inspect a pair with
`npx jscpd <dir> --min-tokens 70 --reporters json --output /tmp/x --silent`).

**Gate discipline (what we settled on):**

- **Hold**: comments, else blocks, overlong names, member chains, deep indentation, hex colours,
  orphan components. Any regression is trimmed before the commit — a comment the component's
  filename now carries comes off; a `+1 else` becomes a ternary or an early return; a chain becomes
  a local or a typed callback (`EventCallback<LabourOverviewWorker>` rather than a tuple built
  from its fields).
- **Accept as the division signature, with an honest note in the report**: `filesOverLimit` and
  `functionsOverLimit` rising when one 700-line page becomes eight 100–200-line components, and
  `duplicatedPercentage` ticking up when sibling components carry the same `[Parameter]` block
  or the same table shape. Inspect the clone pair first and say what it is.
- **Reset the baseline once per round**, at the end, never mid-round:
  `cp tools/refactor/audit-output/audit.json tools/refactor/baseline.json`, rewrite
  `tools/refactor/baseline-report.md` (§7), commit both as the round's last commit
  ("Baseline vN: …"). `tools/refactor/audit-output/` is tracked — commit its changes with the step
  that produced them so the tip is clean.

---

## 4. The verification loop (every commit, no exceptions)

```
dotnet build jpms/Jewel.JPMS.Verify.csproj --no-incremental 2>&1 | tee /tmp/build.log | grep -E "error|Warn|Error" | tail
grep -c RZ10012 /tmp/build.log          # must be 0
dotnet test tests/Jewel.JPMS.Tests      # must be 434 passed (more if I add tests; never fewer)
```

- **0 errors and RZ10012 = 0 on a `--no-incremental` build gate the commit.** RZ10012 means a
  component tag did not resolve — Razor silently renders it as HTML and the build still "succeeds".
  The fix is a razor-side `@using` for the component's namespace on the page (or in
  `_Imports.razor` if it is shared). Never commit with RZ10012 > 0; if you already did, fix and
  `git commit --amend`.
- When `api/` is touched: `dotnet build api/JpmsApi.csproj` too, and check
  `worker/Jewel.JPMS.Worker.csproj` — the worker compiles api sources through a hand-picked
  `<Compile Include>` list, so **every api file you split or add must be added to that list beside
  its parent** (a round-4 split of `XeroClient` left two partials off the list and broke the worker
  deploy with CS0535; that is the lesson). Verify by building the worker.
- Using rules in the client: `jpms/GlobalUsings.cs` (C#) and `jpms/_Imports.razor` (razor) both
  carry the shared set (`Jewel.JPMS`, `Components`, `Cqrs`, `Models`, `Services`, the seven
  `Contracts.*` namespaces, and `@using static` for `MoneyFormats`, `FileSizeFormat`,
  `DateFormats`). Component **tag** resolution reads only the razor side. `Contracts.MailboxCompose`,
  `Contracts.Variations`, `Contracts.Subcontractors`, `Contracts.Drawings`, `Contracts.Lads`,
  `Contracts.Site` and every `Jewel.JPMS.Features.<Area>` namespace are **not** shared — add
  `@using` per page/component, and a C# `using` in any `.cs` partial that names the component type
  (`CS0246` otherwise).
- After any file split, a brace-balance sanity check and a grep for the moved members' callers.
- The `ICommandSender` shape is `Task<TResult> SendAsync<TResult>(ICommand<TResult> command,
  CancellationToken)`; commands are named intentions in `contracts/`.

---

## 5. Branches, commits, attribution

- **Create `refactor/round-N` off `refactor/round-(N-1)` before the first commit of the round.**
  (Round 13's commits once landed on round-12 because the branch was created late; it was repaired
  with `git branch -f refactor/round-12 <delivered tip>`. Don't repeat it.)
- One commit per verified step — build green, RZ10012 = 0, tests green, gate green (or a held
  regression trimmed in the same commit). Typical round: 5–10 commits.
- Commit titles are sentence-case prose stating what became what: "The labour overview's three
  dialogs become components", "The one Apply divides from the reply composer", "Comment lines the
  component names now carry come off". Bodies say what moved where and the figure: "ProjectProgramme
  750 → 78: a tab bar and four panes." The round's last commit is "Baseline vN: <the round's
  name>" with the headline figures in the body.
- End every commit message with the attribution trailers your session gives you (previous shape:
  `Co-Authored-By: Claude <model> <noreply@anthropic.com>` and `Claude-Session: <this session's
  URL>`); PR descriptions, if I ever ask for one, end with
  `🤖 Generated with [Claude Code](https://claude.com/claude-code)` and the session URL.
- Never rewrite a commit that has been delivered to my Mac; fix forward.

---

## 6. The recipes (what rounds 12–15 established — imitate these files)

**The panel recipe (pages).** Page markup → components with explicit `[Parameter]` interfaces in
`jpms/Features/<Area>/` (`@namespace Jewel.JPMS.Features.<Area>`), page keeps a tab bar and thin
dispatchers. Decide where state lives by where it parks:

- **State stays on the page** when parking, paging, tab badges or pill routing need it; bind it
  down with `@bind-X` + `EventCallback<T> XChanged` (`VariationApproveOffer @bind-ModalOpen`,
  `ApprovedFiguresPanel` bound `ReturningToQuoting`/`RejectingOrder`,
  `RequestOfficialFormPanel @bind-Editing`).
- **State moves into the component** when it parks there with nothing else: self-contained editors
  and modals own their draft, their error, their store injection, and hand back the saved record
  through `EventCallback<T> OnSaved` (`VariationDetailsCard` injects `IVariationStore`;
  `RequestOfficialFormPanel` injects `IRequestRegister`; `WorkerDetailPanel`, `ChaseListPanel`).
  Seed the draft on open (`wasOpen`/`wasEditing` in `OnParametersSet`) so a cancelled edit leaves
  nothing behind.
- **The page performs the write but the editor must stay open on refusal**:
  `Func<T, Task<bool>>` (`ApprovedFiguresPanel.ReviseValue`, `ProgrammeGanttChart.SaveTask`) or
  `Func<TCommand, Task<(bool Saved, string? Error)>>` (the three request edit dialogs' `Send`),
  never a fire-and-forget `EventCallback`.
- **Dialogs opened from several places**: `@ref` + a public `Open(...)` / `OpenAsync()` /
  `PrefillNod(...)` (`AbsenceModal.Open(workerId, name, date)`, `SettlementLineModal.Open(schedule)`,
  `WeekEntryModal.OpenAsync()`, `ProgrammeClaimsWorkbench.PrefillNod`). When a page must render the
  component before calling into it: `await InvokeAsync(StateHasChanged); await Task.Yield();` first.
- **Reset component state by identity, not by hand**: `@key` per record id or per month
  (`StagedBuildUpPanel @key by id`, `LabourOverview`'s month panes keyed by
  `MonthKey => $"{year}-{month}"`) instead of the page clearing fields in `MoveMonth`.
- **Line-pure rules** go to `Features/<Area>/<Area>Display.cs` static modules used via
  `@using static` (`XeroLedgerDisplay`, `LabourDisplay`, `CashflowDisplay`, `ProfitDisplay`,
  `TriageEmailDisplay`, `RecordLinkVocabulary`); a house-date helper reads
  `Day(DateTimeOffset?) => DateText(stamp?.LocalDateTime)`.
- **Row families** for tables: an identity cell plus one row component per status
  (`Features/Xero/Allocation/LedgerLineIdentityCell` + `QueueLineRow`/`LabourLineRow`/
  `DisputedLineRow`/`AllocatedSummaryRow`); `RenderFragment<T>` slots where a host needs a
  caller-specific patch (`CashflowGroupRow.EntryRow`, `TriageMessageDetail.AfterHeader`,
  `QueueEmailReadingPane` taking `ReplyComposerForm` as `ChildContent`).
- **Shared widgets over per-page copies**: `RecordCorrespondencePanel` (the reply widget every
  record's emails tab now renders), `TriageEmailRow`, `EmailListPager`, `NoticePanel`,
  `ApprovedSessionGate`, `LoadGate`.
- **Command-and-reload workbenches**: one
  `Task<bool> WriteAsync<TResult>(ICommand<TResult> command, string refusal)` shape that sends,
  re-reads and surfaces the refusal (`ProgrammeWorkbench.razor.cs`).
- **Fragments become components.** A `RenderFragment` in a page's `@code` (ProfitSummary's
  `MarginLine`/`Memo`, CashForecast's `CategoryRow`, WeeklyCashflow's
  `BandSection`/`LoadingFigure`) is a component that has not been written yet.

**The partial recipe (.cs and api).** A partial file is a concern and its filename is the
concern's name: `Page.razor` (markup) + `Page.razor.cs` (state, loading, panes) +
`Page.<Concern>.cs` one per concern; api giants divide per type and per API area
(`XeroClient.Reads/Writes/SitePnl/Attachments/LineItems/Http`, `AiRecordTools.Correspondence/
Contexts/Directory` with `Build() = A().Concat(B()).Concat(C())`, `JpmsContext` +
`JpmsContext.Model`, renderers into `.Sections` + `.Helpers`). Orchestrators: a plan record, a
refusal gauntlet returning `string?`, numbered step methods (`TriageQueue.Apply.cs`).

**Behaviour-preservation care that has already bitten:** editors stay open on refusal; a banner
resets its arm before invoking its callback (`MatchedLinesBanner`); a modal seeds per line via
`ReferenceEquals` (`LineCoverageModal`) or on open via `wasOpen` (`LinkDrawingsModal`);
`PanelWorkspace` keeps every once-shown pane rendered so pane state survives tab switches — keep
that when a pane becomes a component.

**Razor gotchas:** `@if ((Error ?? error) is { } shown)` — parenthesise `??` before `is`; an
attribute whose lambda contains `""` uses a single-quoted attribute
(`@oninput='e => …(e.Value?.ToString() ?? "")'`); component parameters typed
`IEnumerable<T>` where the page hands over a LINQ result (`IReadOnlyList` gives CS1503).

---

## 7. The baseline report — "redo the file I gave you"

`tools/refactor/baseline-report.md` is rewritten every round, in this exact shape, and **sent to me
as a file at the end of every round** (I read it before I look at the code):

```
# Refactor audit — baseline vN, after round N-1

Generated <date> from `refactor/round-(N-1)`, replacing the round-(N-2) (v(N-1)) baseline report. The
audit carries the prose and functionNames checks introduced at v2.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: …, totalFiles: …, worstFileLines: … |
| functionShape | limit: 30, functionsOverLimit: …, elseBlocks: …, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: …, maxWords: 5, maxLength: 40 |
| duplication | clones: …, duplicatedLines: …, totalLines: …, duplicatedPercentage: … |
| naming | bannedAbbreviationHits: …, unprefixedBooleans: … |
| comments | explanatoryCommentLines: …, filesWithComments: …, taskMarkers: … |
| magicValues | inlineHexColours: …, inlineStyleAttributes: …, repeatedStringLiterals: … |
| prose | longMemberChainLines: …, deeplyIndentedLines: …, overlongLines: …, measurementIsHeuristic: True |
| inventory | pages: …, components: …, orphanComponents: …, averagePageLines: … |

## Round N-1 — <the round's name>

Prose, then one bullet per file worked: **File before → after**: what became what, where the state
went, what was deleted and why it was safe. A **Held** bullet listing every held figure with its
before → after and what paid for it. A **Division signature** bullet for accepted drifts, naming
the clone pair or the file count honestly.

## The journey so far

| Figure | 22 Aug (v1) | R10 (v11) | … | R(N-1) (vN) |
| --- | --- | --- | --- | --- |
| Worst file (lines) | 4,961 | … | … | **…** |
| Average page length | 544 | … | … | **…** |
| Duplication | 4.16% | … | … | … † |
| `else` blocks | 1,087 | … | … | **…** |
| Overlong function names | — | … | … | **…** |
| Functions over 30 lines | — | … | … | **…** |
| Files over 100 lines | 385 | … | … | … † |

† The division signature: explicit-interface components where page markup stood. <one or two
sentences of the campaign's running story>

## Worst files by length

| File | Lines |   ← the top 15 from fileLength.offenders

## Round N, named

The next round's targets, by name and line count, with the recipe each wants (tables → row
components / panes → panels / partial-at-a-seam), and one sentence of what the worst file is now.

Full detail, including every offender list, is in `audit.json`; the gate ratchets against
`baseline.json`, which this report accompanies.
```

The Summary table is the audit's own `audit-report.md` Summary, copied exactly. Figures in the
journey table are bold when they improved or held, `†` when accepted as division signature. Numbers
carry thousands separators in prose and tables (`13,726`), never in the Summary table (`13726`,
as the audit prints them). Be honest about every figure — I check them against the gate.

---

## 8. Delivery to my Mac (end of every round, or on "deliver to machine")

I have no way to pull from your workspace, and you cannot push, so each round travels as patches.
The project on my Mac is `/Users/james/Documents/Claude/Projects/jewel-portal` (connected
through the device bridge; the same path is `$HOME/mnt/jewel-portal` inside `device_bash`).

```
# cloud
git format-patch refactor/round-(N-1)..refactor/round-N -o /tmp/rNpatches
tar czf /tmp/round-N-patches.tar.gz -C /tmp/rNpatches .
# SendUserFile /tmp/round-N-patches.tar.gz, then device_commit_files it to
#   /Users/james/Documents/Claude/Projects/jewel-portal/round-N-patches.tar.gz
# Mac (device_bash), from the project folder:
mkdir -p /tmp/rN && tar xzf round-N-patches.tar.gz -C /tmp/rN \
  && git checkout -q -b refactor/round-N refactor/round-(N-1) \
  && git am /tmp/rN/*.patch
# parity, both sides, must match:
git ls-files -s | grep -v "api/local.settings.json" | md5sum
```

Then tidy: `rm round-N-patches.tar.gz` — the Mac VM often refuses deletes on the mounted folder, in
which case `mv` it to `_to_delete_round-N-patches.tar.gz` and tell me. Report the parity hash and
the tip commit to me. I push and open the PR.

Mac quirks: the bridge is often offline — retry a failed bridge call **once**, then hand me the
tarball in chat with the apply commands and carry on; when it returns, apply and verify parity.
The desktop file watcher races git (`index.lock`/`HEAD.lock`) and can interrupt `git am` — resume
with `git am --continue`. The Mac's working tree must be clean and on `refactor/round-(N-1)`
before applying; check with `git status --short` and `git branch --show-current` first, and never
discard my uncommitted work there. Also send `tools/refactor/baseline-report.md` as a file every
round, separately from the tarball.

---

## 9. Where we are (state at handover, 2 September 2026)

**Baseline v16 (after round 15), tip of `refactor/round-15`, parity `60d8b151…`:**

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 644, totalFiles: 3333, worstFileLines: 736 |
| functionShape | limit: 30, functionsOverLimit: 698, elseBlocks: 1182, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 43, maxWords: 5, maxLength: 40 |
| duplication | clones: 488, duplicatedLines: 6207, totalLines: 216128, duplicatedPercentage: 2.87 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1474 |
| comments | explanatoryCommentLines: 13726, filesWithComments: 1811, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2398, deeplyIndentedLines: 2801, overlongLines: 1703, measurementIsHeuristic: True |
| inventory | pages: 92, components: 131, orphanComponents: 6, averagePageLines: 231 |

**The journey so far**

| Figure | 22 Aug (v1) | R10 (v11) | R11 (v12) | R12 (v13) | R13 (v14) | R14 (v15) | R15 (v16) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Worst file (lines) | 4,961 | 954 | 954 | 929 | 837 | 785 | **736** |
| Average page length | 544 | 272 | 272 | 266 | 262 | 246 | **231** |
| Duplication | 4.16% | 2.81% | 2.82% | 2.83% | 2.85% | 2.85% | 2.87% † |
| `else` blocks | 1,087 | 1,182 | 1,182 | 1,182 | 1,182 | 1,182 | **1,182** |
| Overlong function names | — | 44 | 44 | 44 | 44 | 43 | **43** |
| Functions over 30 lines | — | 700 | 704 | 703 | 702 | 700 | **698** |
| Files over 100 lines | 385 | 618 | 621 | 626 | 629 | 638 | 644 † |

Four rounds of the panel recipe took the seven worst pages 954/929/837/830/802/785/750 →
470/488/302/266/435/142/78 (TriageQueue, XeroAllocation, ProjectVariationDetail,
ProjectBidPackageInviteDetail, ProjectRequestDetail, LabourOverview, ProjectProgramme). The worst
file is under 750 for the first time; the api's tail is the AI tool catalogues and one renderer,
all 500–560. `else` blocks have held at 1,182 since round 10 — they are the next figure that wants
a deliberate round once the pages are down.

**Worst files by length (v16)**

| File | Lines |
| --- | --- |
| jpms/Pages/ProfitSummary.razor | 736 |
| jpms/Pages/CashForecast.razor | 665 |
| jpms/Pages/WeeklyCashflow.razor | 604 |
| jpms/Services/Excel/ExcelWorkbookWriter.cs | 589 |
| api/Features/Ai/Tools/AiCommercialTools.cs | 560 |
| jpms/Pages/ProjectWorkOrders.razor | 548 |
| jpms/Pages/TriageQueue.Outbox.cs | 520 |
| api/Features/Ai/Sources/AiSourceReader.cs | 518 |
| jpms/Components/ValuationReportTable.razor | 517 |
| jpms/Pages/Subcontractors.razor | 517 |
| api/Features/Commercial/Documents/CostCentreReconciliationRenderer.cs | 509 |
| jpms/Components/WorkOrderForm.razor.cs | 507 |
| api/Features/Ai/Tools/AiSourceTools.cs | 502 |
| api/Features/Ai/Tools/Actions/RequestsActions.cs | 492 |
| jpms/Pages/XeroAllocation.razor | 488 |

**Rounds so far (each a branch, each a PR):** round 2 (14 commits) … round 11 (3), round 12 (7:
TriageQueue's fragments → `Features/Triage/Queue/*`), round 13 (7: XeroAllocation → `Features/
Xero/Allocation/*` row family + modals), round 14 (10: the project detail trio — Variations,
Procurement, Requests components), round 15 (7: Labour and Site/Programme components,
`TriageQueue.Apply.cs`).

---

## 10. Round 16, named (start here on "begin")

The finance trio leads — **ProfitSummary (736)**, **CashForecast (665)**, **WeeklyCashflow
(604)** — table-heavy pages the `CashflowEntryRow`/`CashflowGroupRow` and `RunningProfitTable`
families already serve in part; the recipe is **tables into row components** and **fragments into
components** (ProfitSummary's `MarginLine`/`Memo`, CashForecast's `CategoryRow`, WeeklyCashflow's
`BandSection`/`LoadingFigure`) rather than panes; their partials
(`ProfitSummary.{Bridge,Cumulative,Export,Figures,Formats,Running}.cs`,
`CashForecast.{Assumptions,Export,Forecast,Statement}.cs`,
`WeeklyCashflow.{Export,Grid,GroupsDialog,ItemDialog,Moving}.cs`) already name the concerns the
components should sit beside. Then **ProjectWorkOrders (548, plus a 451-line `.razor.cs`)** and
**Subcontractors (517, five tables)**, the next pane-shaped pages. On the .cs side,
**ExcelWorkbookWriter (589)**, **TriageQueue.Outbox (520)** and the AI tool catalogues
(**AiCommercialTools 560**, **AiSourceTools 502**, **AiSourceReader 518**) want the
partial-at-a-seam division that Compose/Apply just had (api splits → worker compile list, §4).

A round is done when: every target named for it is under its stated figure or honestly explained,
every commit passed §4, the gate passes against the new baseline, `baseline-report.md` v17 is
written in §7's shape and sent to me as a file, and the branch is on my Mac with matching parity
(§8). Then tell me the headline numbers in two sentences and wait for "ok next round".

---

## 11. How to work with me

Start each round by reading the current report's "Round N, named" and the target files whole —
not the offender list alone. Work in the cloud; tell me what you are doing in short plain
sentences, not lists of every step. Ask me only when a choice is truly mine (which of two honest
divisions, whether a fix I did not ask for should go in); otherwise make the call, say it in the
commit body, and keep moving. If a figure regresses and you cannot hold it without inventing
something, accept it in the report with the reason — never hide it, never game the heuristic. When
I push back, take it on and change the round; the numbers are the argument.
