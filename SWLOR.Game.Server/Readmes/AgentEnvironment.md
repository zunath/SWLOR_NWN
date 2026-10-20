# Agent Environment

Setup and hygiene for agents working on the maintainer's Windows machine, where several Claude and Codex sessions run at once, and for Claude Code cloud sessions (Linux).

Sections: Worktrees and the haks submodule · Git network · Shared machine · Disk hygiene · Builds and local deploy · Cloud sessions

## Worktrees and the haks submodule

Do each request in its own worktree and branch. `SWLOR_Haks` is a 27 GB submodule that a new worktree leaves uninitialized, so 2DA, TLK, icon, Bible, and toolset tests fail with missing-directory errors that have nothing to do with the change. `git submodule status` shows `-` when it is missing and `+` when it is at the wrong commit.

Most tests only need `sw_2da`, `sw_tlk`, and `sw_ability`. A sparse submodule worktree takes about 15 seconds. With `<main>` the main checkout (`C:\Projects\SWLOR_NWN`) and `<wt>` this worktree's absolute path:

One-time setup: per-worktree config needs `extensions.worktreeConfig` on the shared submodule repository. If `git -C <main>/SWLOR_Haks config --get extensions.worktreeConfig` does not print `true`, move the main checkout's `core.worktree` into its own per-worktree config first, as Git requires:

```bash
wt_main=$(git -C <main>/SWLOR_Haks config --local --get core.worktree)
git -C <main>/SWLOR_Haks config extensions.worktreeConfig true
git -C <main>/SWLOR_Haks config --worktree core.worktree "$wt_main"
git -C <main>/SWLOR_Haks config --local --unset core.worktree
git -C <main>/SWLOR_Haks rev-parse --show-toplevel   # must still print <main>/SWLOR_Haks
```

Then, from the outer worktree root:

```bash
sha=$(git rev-parse HEAD:SWLOR_Haks)
git -C <main>/SWLOR_Haks worktree add --no-checkout --detach <wt>/SWLOR_Haks $sha
git -C <wt>/SWLOR_Haks config --worktree core.worktree <wt>/SWLOR_Haks
git -C <wt>/SWLOR_Haks rev-parse --show-toplevel   # must print <wt>/SWLOR_Haks
git -C <wt>/SWLOR_Haks sparse-checkout set --cone sw_2da sw_tlk sw_ability
git -C <wt>/SWLOR_Haks checkout --detach $sha
```

- The `config --worktree core.worktree` step is required. The shared submodule config can carry a `core.worktree` (this machine's still does) that every linked worktree inherits; without the override, checkout writes into the wrong directory. Never run `git config core.worktree` without `--worktree`: that rewrites the shared value for every checkout.
- Add folders with `sparse-checkout add <folder>` when a task needs models, textures, or other hak content. A full checkout (`git sparse-checkout disable`) writes about 161,000 files and takes over ten minutes.
- To change hak content, create a branch in the submodule worktree (`git -C SWLOR_Haks switch -c <branch>`), commit there first, then commit the pointer bump in the outer worktree. Push with `git -C SWLOR_Haks push origin <branch>`.
- After merging or rebasing onto a commit that moves the pointer, update the submodule worktree to the new pointer before trusting hak-dependent test results.
- Before calling a test failure pre-existing, reproduce it on the base commit in an equally initialized worktree.
- Do not junction `SWLOR_Haks` to the main checkout: git commands in the worktree then fail on the submodule.
- `git add -A` from the repo root can sweep sibling worktrees in as gitlinks; stage explicit paths.

## Git network

- `git fetch` and `push` over HTTPS can fail with "SSL certificate problem". Add `-c http.sslBackend=schannel`. `gh` uses a different TLS stack and works; prefer `gh` for pull requests and API calls.
- `git fetch origin <branch>` updates only `FETCH_HEAD`, not `origin/<branch>`. Before merging, fetch with `git fetch origin +refs/heads/<branch>:refs/remotes/origin/<branch>` and confirm `git rev-parse origin/<branch>`.

## Shared machine

- Other sessions may be editing the main checkout or another worktree at any moment. Never `git stash`, `git checkout --`, `git reset`, or run repo-wide generators (`-RefreshLocalBible`, icon regeneration, formatters) outside your own worktree. If files change under you, stop touching them and work elsewhere.
- Commit as soon as a step is green. Uncommitted work in a shared tree is one stash or checkout away from loss. A clean `git status` in a shared tree may mean another session stashed; check `git stash list` and `git reflog` before declaring work lost.
- Use bounded foreground commands. Start background jobs, watchers, servers, or GUI apps only when the task requires one, track the PID, and stop it before handoff.
- Stop only processes you started, by PID; never kill `nwmain`, `nwserver`, `dotnet`, or `docker` by name. Background shells can leave child processes alive after the shell stops; check for and stop them.
- Never start a search at `/` or a drive root; on Windows it can run for hours. Scope searches to the repository.
- Engine-test runs and servers need unique ports and compose project names. `scripts/run-engine-tests.ps1` hard-codes the compose project `swlor-engine-tests` and runs `docker compose down --volumes`, so two concurrent runs tear each other down; check for a running `swlor-engine-tests` project first, and see `EngineTesting.md`.
- Run NWN clients only through the procedures in `InEngineVisualVerification.md`; they isolate the profile, port, and audio.

## Disk hygiene

Graphics and model work (renders, Blender files, exported meshes and textures, GR2/MDL intermediates, HAK and module copies, client profiles, capture sets) fills the drive fast. Clean up as you go, without being asked.

- Keep everything a task generates in one task-owned directory outside the repository (your scratchpad, or `C:\Temp\<task>\`) so nothing is scattered.
- Delete each intermediate as soon as no later step needs it: superseded renders and exports, rejected variants, unpacked archives, copied HAKs and modules after the run that used them.
- At handoff, delete everything else the task generated except the evidence the user needs (final screenshots and reports), and state what was kept and its size.
- Remove Docker containers, volumes, and images you created (`docker compose -p <your project> down --volumes --rmi local`, plus `docker image rm` for any image you built and tagged yourself), never another session's.

## Builds and local deploy

- The newest SDK on the machine is a .NET 11 preview whose persistent MSBuild server can get stuck: every build fails in a few seconds with "Build FAILED. 0 Warning(s) 0 Error(s)". Run `dotnet build-server shutdown --msbuild` and rebuild, or bypass it with `MSBUILDUSESERVER=0`.
- Always build with `-p:RunPostBuildEvent=Never` unless you mean to deploy. - Building `SWLOR.Game.Server` without `-p:RunPostBuildEvent=Never` deploys to the `debugserver/` folder of the checkout being built, and `dotnet run --project SWLOR.Runner` runs that same checkout's `debugserver/`. A worktree build and Runner therefore use the worktree's own `debugserver/`; a server already running from the main checkout uses the main checkout's. Deploy to the one you are testing.
- Runner always uses the compose project `debugserver` (container `debugserver-swlor-server-1`), so Runner servers from different checkouts collide. Check that none is running before starting one, or use your own compose project and port for parallel work.
- Module JSON changes need `Module\PackModule.cmd`. Hak changes need `SWLOR_Haks\BuildHaks.cmd`, which needs `nwn_erf` on PATH; when it is missing, report the hak rebuild as an outstanding deploy step instead of retrying.
- When an in-game check of a hak-dependent change fails, compare the deployed `.hak` size and timestamp against the expected build before debugging code.

## Cloud sessions

Claude Code cloud sessions run on Linux with a limited disk. SessionStart hooks in `.claude/settings.json` prepare them and do nothing on local machines:

- `session-start.sh` prepares the native NWN tools, installs Wine, PowerShell 7, Xvfb with Mesa's software OpenGL, the Python packages, and the NuGet packages, and exports the variables below. The session waits for it (about a minute on a fresh machine); after `/clear` or compaction it only refreshes the environment.
- `session-start-haks.sh` checks out `SWLOR_Haks` in the background (about 10 minutes on a fresh machine) and installs CleanModels. Before building haks or running tests that read `SWLOR_Haks`, wait for `/tmp/swlor-haks-checkout.done`; progress is in `/tmp/swlor-haks-checkout.log`. It deletes the checked-out blobs from `.git` to fit the disk, and git downloads a blob again when it needs one. The first `git commit` in the submodule downloads all of them again (about 5 GB, several minutes); for a small commit, `git write-tree --missing-ok`, `git commit-tree`, and `git update-ref` avoid that. Set `SWLOR_HAKS_SPARSE` to a folder list for a partial checkout. On later session starts it moves a clean, detached submodule to the pinned commit and leaves a branch or local changes alone.
- `session-start-game-data.sh` copies the dedicated-server game data out of the pinned server image (`scripts/deployment/server-image.txt`) to `/opt/nwn` in the background; `/tmp/swlor-game-data.done` marks success. It holds the base game's 2DAs, scripts, walkmeshes, tilesets, palettes, and `dialog.tlk`, but its models and textures are zero-byte stubs.

Working there:

- `SWLOR.CLI` and the toolset use the native Linux tools (`nwn_erf`, `nwn_gff`, `nwn_tlk`, `nwn_script_comp`), which the hook puts in the same verified cache as `./Setup.sh` and on PATH. Wine, through a binfmt handler, runs the Windows-only tools: CleanModels, and the bundled `.exe` tools that some tests launch directly. The hooks mark the `.exe` files executable and turn `core.fileMode` off so git does not report the mode change. Wine tools fail with "Invalid handle" when their output goes to `/dev/null`; pipe it instead.
- Use the `.sh` shortcuts, not the `.cmd` ones. Run `./Setup.sh` only after `/tmp/swlor-haks-checkout.done` exists: it starts with `git submodule update`, which would otherwise begin a full haks clone that does not fit. To pack the module, build `SWLOR.CLI` and run `dotnet ../SWLOR.CLI/bin/Debug/net10.0/SWLOR.CLI.dll -p "./Star Wars LOR v2.mod" --no-prompt` from `Module/` (about 2 minutes).
- `NWN_INSTALL_PATH` and `NWN_ROOT` point at `/opt/nwn`, `NWN_HOME` at an empty user directory (the script compiler requires one), and `DISPLAY` at the Xvfb display. `NWN_INSTALL_ROOT` stays unset, so the licensed-corpus tests skip.
- PowerShell 7 answers to `pwsh`, `powershell`, and `powershell.exe`. Scripts that need Windows PowerShell 5.1 or Windows-only APIs still fail.
- Linux file names are case-sensitive and many HAK files use mixed case; resolve resource names case-insensitively, as the game does. Windows share modes are not enforced, so tests that rely on them skip (`SWLOR.Toolset.Tests/Support/WindowsFileSharing.cs`). Golden animation bakes differ from the Windows C runtime in the last bits and compare with a tolerance (`GoldenBake.cs`).
- Engine tests: `scripts/prepare-engine-test-home.sh`, then `scripts/run-engine-tests.sh` (`EngineTesting.md`). The prep script starts Docker when it is not running. Docker Hub can rate-limit anonymous pulls.
- Not available: the NWN:EE client and its art. Tests that read base-game models, textures, sounds, or portraits fail or skip, and client evidence needs a Windows machine.
