# HotReload

## Overview
`--reload` replaces the game build (`SWLOR.Game.Server`) of a running development or test server without restarting the NWN
process. It builds the game in Release, publishes the output as a new candidate into the server home, signals the server
and prints the server's receipt. Production never reloads.

## Command
```bash
# From the repository (the tool finds it from the current folder); the default server home is ../debugserver.
tools/SWLOR.CLI/RunCLI.cmd --reload

# Another server home, a build that is already there, a longer wait:
dotnet SWLOR.CLI/bin/Release/net10.0/SWLOR.CLI.dll --reload --server-home <home> --server-output SWLOR.Game.Server/bin/Release/net10.0 --reload-timeout 900
```

| Option | Meaning |
|--------|---------|
| `--reload` | Build, publish, signal, wait for the receipt. Exit code 0 only when the server applied the candidate. |
| `--server-home <dir>` | The server home (`dotnet/`, `reload/`). Default: the repository's `debugserver/`. |
| `--server-output <dir>` | Publish this SWLOR.Game.Server build output as is, without building. Default: build `SWLOR.Game.Server` in Release (`-p:RunPostBuildEvent=Never`) and publish `SWLOR.Game.Server/bin/Release/net10.0/`. |
| `--reload-timeout <s>` | How long to wait for the final receipt (default 600 s). The pause includes the new build's first compilation: the runtime never tiers code of a collectible build, so it is compiled fully optimized. |

## What the server needs
- The host entry: `SWLOR_DOTNET_ENTRY=host` (`SWLOR.Game.Host.dll` in `dotnet/`, the game in `dotnet/game/`). The tool refuses a home without it.
- `SWLOR_HOT_RELOAD=true` in its environment (`swlor.env` ships it `false`). The host refuses the switch, logs
  `SWLOR_HOST reload-refused ...` and keeps running with reload off when `SWLOR_ENVIRONMENT` is production, when it is not
  `dev` or `test` (a missing or misspelled value may be production), and when `SWLOR_GAME_COLLECTIBLE` is `false`.
- The server watches the signal only once the module has loaded: its log says
  `SWLOR_HOST reload-watching signal=... candidates=... receipts=...`. A signal written before that is never acted on.

## Files
All paths are in the server home; the server mounts `dotnet/` read only and writes only to `reload/receipts/`.

| Path | Written by | Content |
|------|-----------|---------|
| `dotnet/generations/<id>/` | the tool | The candidate: a full copy of the build output, published once into its own folder (through a `.partial` folder renamed into place). The id is a new GUID per publish, written without dashes. |
| `reload/signal.json` | the tool | The hosting package's `DevelopmentReloadRequest` (`{"FormatVersion":1,"GenerationId":"<id>"}`), moved over the old signal whole. |
| `reload/receipts/<id>.json` | the server | The receipt of that attempt: `Accepted` while the reload runs, then `Applied` (with `ActiveGeneration`, `ValidationMilliseconds`, `PauseMilliseconds`, `PausedFrame`), `Rejected` (the reason in `Detail`), `RestartRequired` or `Failed`. Each receipt is also a `SWLOR_HOST reload-receipt ...` line in the server log, with the full detail. |

A candidate id is attempted at most once, also after a server restart (its receipt is on disk). After an applied reload the
tool removes candidate folders beyond the newest three.

The server (UID 1000 in the image) creates `reload/` and `reload/receipts/` when it starts watching; the developer writes
`reload/signal.json` and `dotnet/generations/`. On Docker Desktop that needs nothing; on a Linux host whose user is not
UID 1000, make `reload/` writable by both (the local check below does).

## What happens on the server
1. The candidate loads into a new collectible context and validates (builds its service container) off the engine
   thread while the running build keeps serving players. A rejection (no candidate, validation failure) leaves the running
   build untouched.
2. From the running build's quiescence to the candidate's activation the engine thread stays inside one frame: it runs the
   old build's quiescence and dispose and the new build's activation, so no engine frame runs without an active build
   (no script reaches the game unhandled, no native hook fires without a sink). The log says
   `SWLOR_HOST reload-pause frame=<n> heldMs=<ms>` and the new build logs
   `Generation <n> activated by a reload in <ms> ms: ...`.
3. A failure after quiescence leaves no game build running: the log says `SWLOR_HOST restart-required ...`, the receipt
   says `RestartRequired`, and the server must be restarted.

## Limits
- The new build reruns the rerunnable boot handlers, adopts the live world and restores online players, and the pause
  is about a minute (a client may drop). What a reload keeps and loses, and the rules for code that must survive it, are
  in `SWLOR.Game.Server/Readmes/HotReload.md`.
- The old build's pending delayed actions (`DelayCommand`, `AssignCommand`, `ActionDoCommand`) are dropped.
- Module, hak, TLK and 2DA changes still need a repack or hak rebuild and a restart; a build that needs a migration needs
  a restart.

## Local check
`scripts/run-hot-reload-check.sh [--server-home <dir>] [--reloads <n>] [--skip-build]` builds the game, the host and the
CLI, stages the host entry into the server home (without the engine-test assembly), boots the engine-test Compose stack
(its own project, a throwaway Redis) with `SWLOR_HOT_RELOAD=true`, waits for `reload-watching`, runs `--reload` the given
number of times (default 3), prints the receipts and the logged pauses, and tears the stack down. It exits 0 only when
every reload applied and the server is still running.
