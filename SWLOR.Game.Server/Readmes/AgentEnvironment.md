# Agent Environment

Setup and hygiene for agents working on the maintainer's Windows machine, where several Claude and Codex sessions run at once.

Sections: Worktrees and the haks submodule · Git network and reviews · Shared machine · Builds and local deploy

## Worktrees and the haks submodule

Do each request in its own worktree and branch. `SWLOR_Haks` is a 27 GB submodule that a new worktree leaves uninitialized, so 2DA, TLK, icon, Bible, and toolset tests fail with missing-directory errors that have nothing to do with the change. `git submodule status` shows `-` when it is missing and `+` when it is at the wrong commit.

Most tests only need `sw_2da`, `sw_tlk`, and `sw_ability`. A sparse submodule worktree takes about 15 seconds. From the outer worktree root, with `<main>` the main checkout (`C:\Projects\SWLOR_NWN`) and `<wt>` this worktree's absolute path:

```bash
sha=$(git rev-parse HEAD:SWLOR_Haks)
git -C <main>/SWLOR_Haks worktree add --no-checkout --detach <wt>/SWLOR_Haks $sha
git -C <wt>/SWLOR_Haks config --worktree core.worktree <wt>/SWLOR_Haks
git -C <wt>/SWLOR_Haks rev-parse --show-toplevel   # must print <wt>/SWLOR_Haks
git -C <wt>/SWLOR_Haks sparse-checkout set --cone sw_2da sw_tlk sw_ability
git -C <wt>/SWLOR_Haks checkout --detach $sha
```

- The `config --worktree core.worktree` step is required. The shared submodule config carries a `core.worktree` that every linked worktree inherits; without the override, checkout writes into the wrong directory. Never run `git config core.worktree` without `--worktree`: that rewrites the shared value for every checkout.
- Add folders with `sparse-checkout add <folder>` when a task needs models, textures, or other hak content. A full checkout (`git sparse-checkout disable`) writes about 161,000 files and takes over ten minutes.
- To change hak content, create a branch in the submodule worktree (`git -C SWLOR_Haks switch -c <branch>`), commit there first, then commit the pointer bump in the outer worktree. Push with `git -C SWLOR_Haks push origin <branch>`.
- After merging or rebasing onto a commit that moves the pointer, update the submodule worktree to the new pointer before trusting hak-dependent test results.
- Do not junction `SWLOR_Haks` to the main checkout: git commands in the worktree then fail on the submodule.
- `git add -A` from the repo root can sweep sibling worktrees in as gitlinks; stage explicit paths.

## Git network and reviews

- `git fetch` and `push` over HTTPS can fail with "SSL certificate problem". Add `-c http.sslBackend=schannel`. `gh` uses a different TLS stack and works; prefer `gh` for pull requests and API calls.
- `git fetch origin <branch>` updates only `FETCH_HEAD`, not `origin/<branch>`. Before merging, fetch with `git fetch origin +refs/heads/<branch>:refs/remotes/origin/<branch>` and confirm `git rev-parse origin/<branch>`.

Review threads, then resolving one after its finding is fixed or answered:

```bash
gh api graphql -f query='query($o:String!,$r:String!,$n:Int!){repository(owner:$o,name:$r){pullRequest(number:$n){reviewThreads(first:100){nodes{id isResolved path}}}}}' -f o=zunath -f r=SWLOR_NWN -F n=<pr>
gh api graphql -f query='mutation($id:ID!){resolveReviewThread(input:{threadId:$id}){thread{isResolved}}}' -f id=<thread id>
gh api repos/zunath/SWLOR_NWN/pulls/<pr>/reviews --jq '.[] | {user: .user.login, state, body}'
```

The last command catches findings the Codex connector puts in a review body instead of a thread. Body findings cannot be resolved; answer them with a pull request comment naming the fix commit or the reason. A Codex review that reports a usage limit did not review that commit.

## Shared machine

- Other sessions may be editing the main checkout or another worktree at any moment. Never `git stash`, `git checkout --`, `git reset`, or run repo-wide generators (`-RefreshLocalBible`, icon regeneration, formatters) outside your own worktree. If files change under you, stop touching them and work elsewhere.
- Commit as soon as a step is green. Uncommitted work in a shared tree is one stash or checkout away from loss. A clean `git status` in a shared tree may mean another session stashed; check `git stash list` and `git reflog` before declaring work lost.
- Stop only processes you started, by PID. Background shells can leave child processes alive after the shell stops; check for and stop them.
- Engine-test runs and servers need unique ports and compose project names. `scripts/run-engine-tests.ps1` hard-codes the compose project `swlor-engine-tests` and runs `docker compose down --volumes`, so two concurrent runs tear each other down; check for a running `swlor-engine-tests` project first, and see `EngineTesting.md`.
- NWN clients go through `tools/VerifyNwnVisuals.py` only (see `InEngineVisualVerification.md`); it isolates the profile, port, and audio.

## Builds and local deploy

- The newest SDK on the machine is a .NET 11 preview whose persistent MSBuild server can get stuck: every build fails in a few seconds with "Build FAILED. 0 Warning(s) 0 Error(s)". Run `dotnet build-server shutdown --msbuild` and rebuild, or bypass it with `MSBUILDUSESERVER=0`.
- Building `SWLOR.Game.Server` without `-p:RunPostBuildEvent=Never` deploys to the `debugserver/` folder of the checkout being built. A worktree build therefore deploys to the worktree's own `debugserver/`, not the main checkout's, which is what the local server runs.
- Module JSON changes need `Module\PackModule.cmd`. Hak changes need `SWLOR_Haks\BuildHaks.cmd`, which needs `nwn_erf` on PATH; when it is missing, report the hak rebuild as an outstanding deploy step instead of retrying.
- When an in-game check of a hak-dependent change fails, compare the deployed `.hak` size and timestamp against the expected build before debugging code.
