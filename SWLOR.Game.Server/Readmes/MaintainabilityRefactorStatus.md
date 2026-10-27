# Maintainability Refactor Status

The single source of truth for the refactor's progress and handoff. The plan and the owner's decisions are
in [MaintainabilityRefactorPlan.md](MaintainabilityRefactorPlan.md). Update this file in the same pull
request as the work it describes, and whenever a work item starts, finishes or gets blocked.

Last updated: 2026-10-26.

## Resuming (any agent, any account)

1. Read `AGENTS.md`, then this file, then the plan. The plan's owner decisions override the general rules
   where they say so (client evidence per phase, nothing merged to `master` early).
2. Work in your own worktree based on `feature/maintainability-refactor`, never in the main checkout:

   ```bash
   git -c http.sslBackend=schannel fetch origin +refs/heads/feature/maintainability-refactor:refs/remotes/origin/feature/maintainability-refactor
   git worktree add -b <your-branch> <path> origin/feature/maintainability-refactor
   ```

   Set up the sparse `SWLOR_Haks` submodule as `Readmes/AgentEnvironment.md` describes (most tests need
   `sw_2da`, `sw_tlk`, `sw_ability`).
3. Take the first open item in "Work queue" whose dependencies are done. Each item is its own branch and
   pull request into `feature/maintainability-refactor`, with the Codex review loop from
   `Readmes/PullRequests.md`. Update this file in that pull request.
4. Build and test as `AGENTS.md` says (`-p:RunPostBuildEvent=Never -v q -clp:ErrorsOnly`, then filtered
   `dotnet test --no-build`). Report the filters you ran in the pull request.
5. Before stopping, update "Work queue", "In flight" and "Next actions" so the next agent can continue
   without this conversation.

Starting prompt for a new session:

> Continue the SWLOR maintainability refactor. Read AGENTS.md, then
> SWLOR.Game.Server/Readmes/MaintainabilityRefactorStatus.md and MaintainabilityRefactorPlan.md on branch
> feature/maintainability-refactor. Resume from "In flight" and "Next actions". Use cheaper models for
> subagents where the plan's "Models and cost" table allows. Keep the status file current.

## Branch

- Integration branch: `feature/maintainability-refactor` (cut from `master` at `468f6d706`).
- Last sync with `master`: none yet (branch created 2026-10-26).
- Shared packages: `Nwn.Hosting`, `Nwn.Hosting.Contracts`, `Nwn.Hosting.Testing` `0.1.0-preview.1` on
  nuget.org, source at https://github.com/zunath/NWN.Hosting (used from Phase 4).

## Work queue

Status: `open`, `in progress`, `review` (pull request open), `done`, `blocked`.

| ID | Item | Depends on | Model | Status | Pull request | Notes |
|---|---|---|---|---|---|---|
| P0-1 | Plan and status docs on the branch | - | Opus | done | - | First commit of the branch |
| P0-2 | CI workflow: build + unit tests for the refactor branch; coverage collection with generated code excluded; baseline report | - | Sonnet | open | | |
| P0-3 | Inventory of randomness and time sources in combat, stats, perks, abilities, XP, crafting, loot | - | Haiku | open | | Input for P0-4 |
| P0-4 | Behaviour-neutral seams + output-locking test project (`SWLOR.Game.Server.CharacterizationTests`), combat first | P0-3 | Opus spec, Sonnet build | open | | Each seam has a default-equals-today test |
| P0-5 | Boot-order and script-handler-order record test | - | Sonnet | open | | |
| P0-6 | Scripted live smoke run with `tools/NwnLiveClient.py`; baseline recorded | - | Sonnet | open | | Needs a local server and client |
| P0-7 | Daily sync script and parity script (characterization tests against a throwaway `master` worktree) | P0-4 | Sonnet | open | | |
| P0-8 | Cross-domain dependency guard recording today's calls (domain map first) | - | Opus map, Sonnet build | open | | Decision 9 |
| P0-9 | Feasibility check: do app-created worktrees inherit sparse checkout? | - | Sonnet | open | | Read-only; gates the haks cutover |

Phases 1-4 and the final cutover get their own items when Phase 0 is done.

## In flight

None.

## Next actions

1. Start P0-2, P0-3 and P0-5 in parallel (disjoint files).
2. Write the P0-4 spec from the P0-3 inventory.

## Sync log

| Date | `master` commit merged | Conflicts | Parity | By |
|---|---|---|---|---|

## Port log

`master` changes to files already converted on the branch, and where each change went.

| `master` commit | File | Ported to | Pull request |
|---|---|---|---|

## Environment notes

- Git over HTTPS needs `-c http.sslBackend=schannel`; `gh` works without it.
- Container builds that download over HTTPS fail on the owner's machine because the antivirus inspects
  HTTPS. Do not work around it; report the step as blocked.
- The machine's CPU is often busy with other sessions; test timeouts may need to be generous.
