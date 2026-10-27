# Maintainability Refactor Agent Brief

Every subagent working on the maintainability refactor follows this brief plus its task. A coordinator writes the
task, reviews the result and merges it into `feature/maintainability-refactor`.

## Read first

- `C:\Projects\SWLOR_NWN\AGENTS.md` (repository rules), and the guides it names for your area. Always read
  `SWLOR.Game.Server/Readmes/PullRequests.md` and the "Worktrees and the haks submodule", "Git network",
  "Shared machine" and "Builds and local deploy" sections of `SWLOR.Game.Server/Readmes/AgentEnvironment.md`.
- `SWLOR.Game.Server/Readmes/MaintainabilityRefactorPlan.md` on branch `feature/maintainability-refactor`
  (the owner's decisions override general rules where they say so: no client evidence per pull request,
  nothing goes to `master`).

## Where to work

- Fetch: `git -C C:\Projects\SWLOR_NWN -c http.sslBackend=schannel fetch origin +refs/heads/feature/maintainability-refactor:refs/remotes/origin/feature/maintainability-refactor`
- Your worktree: `git -C C:\Projects\SWLOR_NWN worktree add -b claude/mr-<item id lowercase> C:\Projects\SWLOR_NWN\.claude\worktrees\mr-<item id lowercase> origin/feature/maintainability-refactor`
- Set up the sparse `SWLOR_Haks` submodule exactly as AgentEnvironment.md describes if your tests need hak
  data (most unit tests need `sw_2da`, `sw_tlk`, `sw_ability`).
- Never touch `C:\Projects\SWLOR_NWN` itself or any other worktree. Never stash, reset or check out in a
  shared checkout. Stop only processes you started, by PID. Scope every search to the repository.
- Builds: `-p:RunPostBuildEvent=Never -v q -clp:ErrorsOnly`. If a build fails in seconds with
  "0 Warning(s) 0 Error(s)", set `MSBUILDUSESERVER=0`.

## Rules for this refactor

- No change in behaviour. Production code changes are allowed only where your task says so, and each must
  be provably behaviour-neutral (a test shows the default behaves exactly as before).
- Follow the coding standards (one type per file, namespace = folder, plain ASCII punctuation in text).
- Do NOT edit `MaintainabilityRefactorStatus.md`; the coordinator keeps it current.
- Stage explicit paths; keep the tree to your task's changes.

## Pull request

- Push your branch (`git -c http.sslBackend=schannel push -u origin <branch>`) and open a pull request
  with base `feature/maintainability-refactor` (never `master`) using `gh pr create --base feature/maintainability-refactor`.
- The description holds what changed, why, and validation (exact build and test commands and results).
  End it with: `🤖 Generated with [Claude Code](https://claude.com/claude-code)`. Commit messages end with
  `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Run the Codex review loop from PullRequests.md (`tools/RequestCodexReview.ps1 -PullRequest <pr>`) until a
  review of the current head is clean; fix or answer every finding and resolve every thread; check
  mergeability. Do NOT merge the pull request.
- If Codex is rate-limited or never answers, follow PullRequests.md and say so in your report.

## Finish

- Remove your worktree only if the coordinator should not need it; otherwise leave it and say where it is.
  Leave no running processes or containers.
- Report (under 400 words): pull request number and URL, head commit, what changed, build and test results
  with the exact filters, the Codex review outcome, anything skipped or blocked and why.
