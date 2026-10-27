# Maintainability Refactor Plan

The plan for making the server easier to test and maintain without changing anything players see.
Progress, the work queue and the handoff procedure are in
[MaintainabilityRefactorStatus.md](MaintainabilityRefactorStatus.md); read it first when resuming.

## Goals

- Consistent unit tests and a new integration-test tier.
- Dependency injection (Microsoft.Extensions.DependencyInjection) instead of static services.
- Hot reload of game code on development and test servers.
- One repository: `SWLOR_Haks` becomes a folder of this repository (no Git LFS).
- At least 70% line coverage of non-generated code.

**Constraint:** no functional change. Players, staff and saved data see exactly what they see today.

## Owner decisions (2026-10-26)

1. Everything happens on the long-lived branch `feature/maintainability-refactor`, tests and CI included.
   Nothing reaches `master` until a **single final merge** of the finished, verified refactor.
2. Behaviour-neutral seams are allowed (for example an injectable random source); each ships with a test
   proving its default behaves exactly as today.
3. Coverage counts only non-generated code (`Feature/BeastDefinition` and `Generated/` folders excluded).
4. Refactor PRs on this branch do not need per-PR NWN client evidence. Each phase ends with one live
   smoke run instead (see "Verification"). This replaces the per-change client evidence rule in
   `AGENTS.md` for this branch only.
5. Hot reload is for development and test servers. Production keeps restarting the server.
6. The haks repository merges with its full history and without LFS. It happens in the final cutover
   window, not on the branch.
7. Data-driven definitions stay out of this plan.
8. Short, announced content freezes cover the haks cutover and the conversion of `Combat`, `Stat` and
   `Ability`.
9. No plugin projects for now. Domains get their own namespaces inside `SWLOR.Game.Server`, and an
   architecture test records today's cross-domain calls and fails any new one. Plugin projects are
   reconsidered after the dependency-injection phase.
10. The hot-reload host is the shared public package family `Nwn.Hosting`
    (https://github.com/zunath/NWN.Hosting, MIT, on nuget.org), already used by another server.
    SWLOR consumes it; it never copies its code.
11. Keep costs down: use the cheapest model that can do a task well (see "Models and cost").

## Branch model

- `feature/maintainability-refactor` was cut from `master` at `468f6d706`. Every work item is its own branch
  cut from it and its own pull request into it, with CI and the full Codex review loop
  (`Readmes/PullRequests.md`). Never commit refactor work to `master`.
- **Daily sync.** Once a day, run `tools/Refactor/Sync-MaintainabilityBranch.ps1` on the branch: it merges
  `origin/master` (merge, never rebase or force-push), re-runs the mechanical scripts, builds and runs the tests.
  The merge must reach the branch as a real merge commit (the repository's GitHub setting allows only squash merges,
  so syncs are pushed directly or merged locally with `--no-ff`). Record each sync in the status file.
- **Conflict policy.**
  - Mechanical changes (namespace moves, `using` rewrites, handler conversion, generated engine
    interface) are committed, re-runnable scripts under `tools/Refactor/`. On a conflict, take
    `master`'s version of the file and re-run the script.
  - A `master` change to a file that was already converted by hand is ported into the converted code and
    recorded in the port log (`master` commit -> where it went) in the status file.
- **Parity.** Converted services keep static facades with today's API until the very end, so one
  output-locking test project compiles against both `master` and the branch. After each sync, run it in
  a throwaway `master` worktree (never committed) to regenerate the reference results, then require the
  branch to match them exactly.
- **Hot files last.** `Combat.cs`, `Stat.cs` and `Ability.cs` (about 60 edits a month on `master`)
  convert last, in a short window close to the final merge.

## Phases

Each phase has a done check. A phase is finished only when its check passes on the branch.

### Phase 0 - Safety net

- CI (`.github/workflows`) that builds the solution and runs the unit tests for pull requests into and
  pushes to the refactor branch; engine tests nightly and on a label.
- Coverage collection (coverlet) with a baseline report of non-generated code.
- Behaviour-neutral seams for randomness and time where the locked systems need them.
- Output-locking (characterization) tests with seeded randomness: combat hit, damage and critical
  results; stats; perk and ability costs and requirements; XP; crafting; loot.
- A record of the order boot handlers and script handlers run in today; a test fails if it changes.
- A scripted live smoke run (`tools/NwnLiveClient.py`): log in, fight, use an ability, craft, bank, a
  quest step, an area transfer. Run once to record the baseline.
- The daily sync and parity scripts.
- The cross-domain dependency guard (decision 9), recording today's calls.
- A feasibility check that app-created worktrees inherit sparse checkout (needed for the haks cutover).

**Done:** CI green on the branch; coverage baseline recorded; output-locking and boot-order tests pass on
`master` code and on the branch; smoke baseline recorded; sync and parity scripts used at least once.

### Phase 1 - Engine seams

- Generate `INWScript` and NWNX plugin interfaces from the existing `SWLOR.NWN.API` signatures, with a
  pass-through implementation and test fakes (`SWLOR.NWN.API.Testing`). The global static `NWScript`
  forwards to a swappable instance, so no call site changes.
- Replace the Roslyn-recompile and reflection-reset test harnesses where the seam makes them unnecessary.

**Done:** the NWScript and NWNX seams are in with their proofs; output-locking parity holds. Tests that fake the engine
indirectly (Roslyn recompiles, reflection resets) move to the seams service by service inside Phase 2.

### Phase 2 - Dependency injection

- Before any file or namespace moves, handler order is made explicit (P0-10): today it follows the compiler's
  type order, so moving a file can reorder boot handlers (for example `DB.Load` must stay ahead of the module
  preload handler).
- A composition root builds the container at boot; `ScriptRegistry` resolves handler classes from it in
  exactly today's order (the boot-order record guards this).
- Convert services one domain at a time, leaves first: static class -> interface + class with
  constructor dependencies, static caches -> instance state, files moved into the domain namespace.
  Each keeps a static facade that forwards to the container, so content code keeps compiling.
- Split large services by responsibility as they convert (for example `Combat.cs` into hit resolution,
  damage, attack cadence, ability resources, pending bonuses, defense, combat log, damage-derived
  healing and per-creature state).
- Tests for every converted service reach the coverage floor in the same pull request.
- Facades are removed at the end, area by area, after the last conversion.

**Done:** no static service state remains; the dependency guard and architecture tests pass; parity holds.

### Phase 3 - Integration tests and coverage

- An integration tier: the real container with a fake engine and real Redis in Docker (persistence round
  trips, event chains, migrations on a production-data copy).
- Coverage ratchet in CI; per-area floors; at least 70% overall on non-generated code.

**Done:** 70%+ coverage, integration tier green in CI.

### Phase 4 - Hot reload (development and test servers)

- New `SWLOR.Game.Host` project: the NWNX entry point, built on `Nwn.Hosting`; the 32 native hooks
  register here and forward to the active game build through `SWLOR.Game.Contracts`; the Redis connection
  lives here.
- `SWLOR.Game.Server` becomes the reloadable build. Boot handlers split into rerunnable cache builds and
  one-time world setup. Definition and handler discovery scan only the current build's assemblies (today
  they scan every loaded assembly, which would find a retired build's types).
- Leak sources fixed: Newtonsoft and Serilog caches, logged exception objects, the combat recorder's
  dedicated thread, static providers in `SWLOR.NWN.API`.
- Pin the .NET runtime patch version in the server image (the package's runtime guards check it).

**Done:** many-reload unload test passes with the real SWLOR build; a connected client survives a reload on
a development server.

### Final cutover (single merge)

1. Announce the freeze; let open `master` pull requests merge; final sync and parity run.
2. Merge the haks repository into `SWLOR_Haks/` with full history (scripted), update setup, deploy and
   cloud-session scripts, archive the old repository read-only.
3. Full verification (below), then merge the branch into `master` in a scheduled window.
4. Rollback: revert the merge commit and redeploy the previous build.

## Verification

- **Every pull request:** build, unit tests, output-locking tests, architecture tests, Codex review loop.
- **Every daily sync:** the same, plus parity against a fresh `master` run.
- **End of each phase:** full unit and engine suites, parity, and one live smoke run with the procedure in
  `Readmes/InEngineVisualVerification.md`.
- **Final merge:** all of the above, production-data parity checks, and a soak on the test server.

## Target structure

Project folders stay at the repository root (moving them would break hundreds of paths for no benefit).

- `SWLOR.Game.Host/` (new): NWNX entry point, native hook registration, Redis connection, reload options.
- `SWLOR.Game.Contracts/` (new): types that cross the host/game boundary.
- `SWLOR.Game.Server/`: the reloadable game build. New `Composition/` (container, script dispatch),
  `Domain/<Domain>/` (interface + instance class per service) and `NativeSinks/` (game side of the hooks); `Service/`
  keeps the static facades until facade removal;
  `Native/` and the old `Core/` closure, scheduler and registry types removed.
- `SWLOR.NWN.API/`: generated `INWScript` seam; `SWLOR.NWN.API.Testing/` (new) with fakes.
- Tests: `SWLOR.Game.Server.Tests` (unit), `SWLOR.Game.Server.CharacterizationTests` (new, output locking),
  `SWLOR.Game.Server.IntegrationTests` (new), `SWLOR.Game.Server.EngineTests`, `SWLOR.Game.Host.Tests` (new),
  `SWLOR.Architecture.Tests` (new).
- `SWLOR_Haks/` becomes a plain folder at cutover; `.gitmodules` is removed.

## Models and cost

Use the cheapest model that does the task well; escalate only when a task needs judgment.

| Work | Model |
|---|---|
| Read-only inventories, counting call sites, summarising logs, status checks | Haiku |
| Implementing a written spec, mechanical conversions with scripts, writing tests, running suites, the Codex review loop, routine daily syncs | Sonnet |
| Writing specs, behaviour-sensitive conversions (combat, stats, abilities), hard sync conflicts, the hot-reload host split, reviewing other agents' diffs | Opus |

The coordinating session writes a spec for each work item, hands it to a subagent on the cheaper model, and
reviews the result before it merges into the branch. At most three subagents run at once.

## Estimates

About 100-130 pull requests, 1.2-2.4 billion tokens (mostly cached context; including the extra cost of
porting `master` changes into a long-lived branch), and 5-7 months of calendar time including the final
verification window. The main costs are porting `master` changes (about 175 service-touching pull requests a
month) and the size of the final merge.

## Risks

- **Divergence:** the longer the branch lives, the more `master` changes need porting. The daily sync and
  parity check keep it visible; the hot files convert last.
- **Hidden behaviour change:** guarded by output-locking parity, the boot-order record, engine tests and the
  per-phase smoke run.
- **Final merge size:** one large cutover. Mitigated by the verification list, the test-server soak and a
  one-step rollback.
