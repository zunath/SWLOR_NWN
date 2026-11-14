# Hot Reload

Replacing the game build of a running development or test server without restarting the NWN process. Production never reloads: it restarts as before, and boots through the native entry. The design and its history are in `MaintainabilityRefactorHotReload.md` (it goes away at cutover); this guide is what to know to use reload and to write code that survives it.

Sections: What it is, Turn it on, Reload, Receipts and logs, What a reload keeps and loses, Checks, Rules for new code, What enforces the rules

## What it is

- The NWN process loads `SWLOR.Game.Host` (`dotnet/`). The host loads the game (`SWLOR.Game.Server`, `dotnet/game/`) into its own load context, a **generation**. A reload loads a new build as the next generation, retires the old one, and the old build is unloaded from memory.
- The host, `SWLOR.Game.Contracts` and the native hook trampolines (`SWLOR.Game.NativeHooks`) are never reloaded. A change to them needs a restart.
- The native entry (`SWLOR.Game.Server`'s own `ServerManager.Bootstrap`, everything in `dotnet/`) is production's entry. It has no reload. `SWLOR_DOTNET_ENTRY` picks the entry; development defaults to the host and the production deployer to the native entry (`Deployment.md`).
- A reload is for trying code changes quickly. It does not replace the module, hak, TLK or 2DA files (repack or rebuild, then restart), and a build that adds a migration is refused (restart).

## Turn it on

All of these must hold, or the host logs `SWLOR_HOST reload-refused ...` and runs with reload off:

| Setting | Value |
|---|---|
| `SWLOR_DOTNET_ENTRY` | `host` (the development default in `Docker/swlor.env`). |
| `SWLOR_HOT_RELOAD` | `true`. Ships `false` in `swlor.env`; never set it on a production server. |
| `SWLOR_ENVIRONMENT` | `dev` or `test`, spelled correctly. `production`, and a missing or misspelled value (which may be production), refuse reload. |
| `SWLOR_GAME_COLLECTIBLE` | Blank (collectible everywhere outside production) or `true`. `false` loads a build that can never be replaced, so reload is refused. |
| `SWLOR_HOT_RELOAD_DIRECTORY` | Optional. Where the signal and receipts are; default `reload/` in the server home, beside `dotnet/`. |

A normal build (without `-p:RunPostBuildEvent=Never`) stages the host and the game into `debugserver/dotnet` (`SWLOR.CLI -o`), the layout reload needs; the engine-test and live-client stacks stage the same one. Build with `-p:RunPostBuildEvent=Never` when you do not mean to deploy. The server watches the signal only after the module has loaded; its log says `SWLOR_HOST reload-watching signal=... candidates=... receipts=...`. A signal written earlier is ignored.

## Reload

```bash
tools/SWLOR.CLI/RunCLI.cmd --reload                       # build Release, publish a candidate, signal, wait for the receipt
dotnet SWLOR.CLI/bin/Release/net10.0/SWLOR.CLI.dll --reload --server-home <home> --server-output <build output> --reload-timeout 900
```

`--reload` builds `SWLOR.Game.Server` in Release (or takes `--server-output`), copies the output into `<home>/dotnet/generations/<id>/`, writes `reload/signal.json` and prints the receipt. Exit code 0 only when the server applied the candidate. Options and files: `SWLOR.CLI/Readmes/README_HotReload.md`.

What the server does: the candidate loads into a new collectible context and validates (builds its service container) while the old build keeps serving players; a rejection leaves the old build untouched. Then the host holds the engine thread in one frame while the old build quiesces and is disposed and the new one activates (reruns its boot handlers, adopts the live world, restores online players). A failure after quiescence leaves no build running: the receipt says `RestartRequired` and the server must be restarted.

**The pause is about a minute** (53 to 63 seconds measured). Nearly all of it is the rerun caches (`CacheService.CacheData` alone builds 8,717 item blueprints). No packet reaches a client meanwhile, so a client with the default 60 second UDP timeout may drop; it logs back in, and the new build has already restored its state. Raise the client timeout to 300 seconds for testing (`[net.udp.window] timeout = 300000` in the client's `settings.tml`, as `tools/nwn_live_client` does). Reload one change at a time; do not reload while a test or an important event is running.

## Receipts and logs

`reload/receipts/<id>.json`, one per attempt, also logged as `SWLOR_HOST reload-receipt ...`:

| Status | Meaning |
|---|---|
| `Accepted` | The host found the candidate and started. A final receipt replaces it. |
| `Rejected` | The candidate did not activate and the old build runs on; `Detail` says why (no candidate, validation failed, the schema gate saw a newer migration). |
| `Applied` | The candidate runs. `PauseMilliseconds` is the pause. |
| `RestartRequired` | The reload failed after the old build quiesced. Restart the server. |
| `Failed` | The host could not run the reload at all; the old build is untouched. |

A candidate id is tried once, also after a restart. Other log lines: `SWLOR_HOST reload-pause frame=<n> heldMs=<ms>`, `Generation <n> activated by a reload in <ms> ms`, `SWLOR_HOST restart-required ...`. With reload on, the build also logs `SWLOR_RELOAD_DIGEST` lines (counts and hashes per service) that the check below compares.

## What a reload keeps and loses

**Kept:** the NWN process; every engine object and effect (HP, position, inventory, locals, journal, the native effects of status effects); Redis; NWNX event bindings; engine settings (feats, feedback, TLK overrides); installed hooks; the Redis connection; online players, connected and in place.

**Rebuilt by the new build:** every cache and timer (the rerun boot handlers), script dispatch, the service container. Live spawns, properties, docks, shuttle flights and weather are adopted from the world, not recreated. A cache built from the world at boot that the world can outgrow is carried instead (the area cache and the store list): area instances made since boot stay out of it, as on a server that never reloads.

**Carried** from the old build as data (fields marked `[ReloadState(ReloadStateKind.Carried)]`): status effects of every creature, parties, auras, combat times and points, enmity, several cooldowns, AFK and PvP states, temporary stat modifiers, shuttle boarding, space state, quest encounter creatures, the area cache, the store list and more. The exact list is the CARRIED STATE part of `SWLOR.Game.Server.Tests/Core/Snapshots/ReloadAdoption.txt`.

**Lost, so expect it after a reload:**

- Delayed actions the old build scheduled (`DelayCommand`, `AssignCommand`, `ActionDoCommand`): they are code of the old build and are dropped. Where one kept a state alive or was due to end one, the reload ends that state in the old build or restarts the work in the new one (a cast is interrupted, so the caster is not left busy; rest's movement check, pending ship launches and door relocks restart; section 3 of the design guide lists them). The rest are lost: an ability impact in flight, a respawn countdown or a queued ability does not happen.
- Windows that take a payload (an item, a target, a session, a callback) are closed. Windows without one reopen where they were, with their selections, scroll and typed text reset. A conversation on a registered graph resumes at its line; one built in code ends as when the player closes it.
- Activities in flight: casting, melee technique chains, twin pistol channels, quick crafts, crafting, slicing, searching, prospecting, fishing, performances, espionage infiltration, combat recordings, an open targeting cursor. Most are ended cleanly at quiescence (as at logout).
- Short-lived combat bookkeeping (swing debts, hit sequences, proc cooldowns, healing budgets, stealth rolls, telegraph shapes on the ground), AI state (boss phases, NPC ability cooldowns, pet command modes, NPC ship status in space), placed traps and Field Engineer emitters (removed at quiescence), appearance projections (the next equip change recomputes them), chat portrait history.
- The connection of a client that does not outlast the pause (above).

The reasons for each are in `MaintainabilityRefactorHotReload.md`, section 3.

## Checks

- `scripts/run-hot-reload-check.sh [--server-home <dir>] [--reloads <n>] [--skip-build] [--engine-filter <filter>]` boots the engine-test stack on the host with reload on, reloads `n` times (default 3) with `--reload`, prints the receipts and pauses, and compares the cache, adopted and world digests with the boot's. `--engine-filter HotReloadPlayers --reloads 2` also keeps a headless player online across two reloads and requires every generation's engine run to pass.
- `SWLOR.Game.Host.Tests` loads the real Release build through the host's reload path 25 times with fake engine bindings and requires zero retained contexts (`RealBuildReloadTests`).
- Engine tests run on either entry (`EngineTesting.md`): `-Entry host` (default) or `-Entry native`.

## Rules for new code

A generation must be able to leave memory and the world must come through it. Write new code so:

1. **Classify every boot handler.** Each handler of `mod_preload`, `mod_content_chg`, `mod_cache_bef`, `mod_cache_aft`, `mod_load`, `db_loaded` and `app_shutdown` carries `[BootPhase(BootReload.<class>)]`: `Rerun` when it only fills the build's own state, reads the engine, Redis or resources, writes nothing persistent and creates nothing (a `Scheduler` timer or a registration into the game assembly's own statics is fine); `BootOnly` when it changes the world, saved data or engine state that survives (give it `AdoptedBy`, a method a reload runs instead, or `NothingToAdopt` with the reason); `NativeHook` for a hook install; `Retire` for shutdown. A handler that does both is split into two handlers that run back to back, and the manifest and `tools/Refactor/HandlerRenames.txt` say so.
2. **Classify every `mod_enter` and `mod_exit` handler** with `[PlayerSession(PlayerReload.<class>, "<reason>")]`: `Restore` (rebuild the player's managed state for someone already online) or `LoginOnly` for `mod_enter`; `Quiesce` (save or end something only the old build holds) or `LogoutOnly` for `mod_exit`. A `Restore` or `Quiesce` handler is exactly a call of a public per-player method named by `Step`, so login and logout run the same code. Add the new line to the classification snapshots with the test update command.
3. **Mark state that boot or reload owns.** On a service that owns a rerun handler or adoption, mark each field that is not a plain cache: `[ReloadState(ReloadStateKind.Adopted | Runtime | Rerolled | Carried)]`. An unmarked field is cache and must hash the same after a reload as after boot. `Carried` state must be data (no delegates, no engine handles). Per-player state that must survive goes through a `Restore` step or a carried field, or it is listed as lost.
4. **No threads, timers or process-wide events outside the scheduler.** No `new Thread`, no `System.Threading.Timer`, `System.Timers.Timer` or `PeriodicTimer`, no subscription to `AppDomain`, `AssemblyLoadContext`, `Console.CancelKeyPress`, `TaskScheduler.UnobservedTaskException`. Use `Scheduler`, `NwTask` and pool work. The two process subscriptions the server keeps are in `AppDomainProcessEventSource`, behind `IProcessEventSource`, so teardown removes them. Pool work must finish by the time the build is disposed; the host waits for it, so never leave work running on a loop.
5. **No `AppDomain.GetAssemblies` or load-context listings.** Discovery scans call `GameAssemblies.GetAssemblies()`, which lists only the game build's own context, so a retired build is never scanned.
6. **Give the host text, not exceptions.** Log errors through `Log`, which a generation owns and closes. Never hand an exception object to a logger, callback or field owned by the host, the contracts or another generation (convert it with `ToString()` first), and never let one escape a sink or hook into native code: it keeps the throwing build's types, and so the build, loaded.
7. **No new static mutable state.** Services are instances behind interfaces (`DependencyInjection.md`). A static field of the game assembly dies with its generation, so it is allowed only where the reload rules above account for it (a registry a rerun handler fills); anything that points into the build from outside it (a native callback, a host static, a `NWN.Core` static, a cache in a shared package) does not die and leaks the build.
8. **Do not touch the host, contracts or trampolines for a game change.** Hook bodies live in `NativeSinks/`, the trampolines forward to them (`CoreSystems.md`, "Native hooks and sinks").
9. **Constructors do not call the engine** and do not start work: the container is built and validated off the engine thread while the old build runs.
10. **Persisted names stay simple class names** (Redis keys, index names, `EntityType`); renaming one is the same break it always was.
11. **A delayed action that keeps or ends a state the reload keeps needs a reload path.** When a `DelayCommand`, `AssignCommand` or scheduler closure clears a busy flag, removes an effect, turns off an action mode, relocks a door or reschedules itself while a carried field or engine state stays, add a `[ReloadHook(ReloadHookPhase.Quiesce)]` method that ends the state in the old build, or a `[ReloadHook(ReloadHookPhase.Resume)]` method (or `IReloadStateAdopter.OnCarriedStateAdopted`) that restarts the work in the new one. An id a carried field remembers must not restart from a per-build counter: carry the counter too.

## What enforces the rules

| Rule | Enforced by |
|---|---|
| 1 Boot handlers classified | `BootHandlerClassificationTests` (an unclassified handler fails; the snapshot `SWLOR.Game.Server.Tests/Core/Snapshots/BootHandlerClassification.txt` lists every class). Unit tests, not an architecture test. |
| 2 Player handlers classified | `PlayerSessionClassificationTests` and `Snapshots/PlayerSessionClassification.txt` (same folder). |
| 3 State marked | Partly: the reload digests (`ReloadDigestTests`, `ReloadAdoptionTests`, `ReloadStateCarrierTests`) cover the classes that own a rerun handler or adoption, and the reload check compares them live. A new field of another class is not checked; the guide and review are the check. |
| 4 No threads, timers, process events | `SWLOR.Architecture.Tests` `ProcessWideRegistrationTests` (threads, the process-wide events, and runtime timers, each with an empty allow list except the one event source). |
| 5 No assembly listings | `SWLOR.Architecture.Tests` `ConvertedServicesTests.Only_GameAssemblies_lists_the_assemblies_of_the_process`. |
| 6 Text, not exceptions | The guide only. |
| 7 No static mutable state | `ConvertedServicesTests` for the `Domain.*` types of converted services (a baseline lists the few support types that still hold static state). `Feature/`, `Native/` and `Core/` are not scanned: the guide and review. |
| 8 Host, contracts, trampolines | `HostRulesTests` (the host references nothing of the game) and `NativeHookRulesTests`. |
| 9 Constructors | Container validation at build (`ValidateOnBuild`, `ValidateScopes`); calls to the engine in a constructor are not scanned. |
| 10 Persisted names | The guide only. |
| 11 Delayed actions with a reload path | `ReloadHooksTests` cover how hooks are found and run, and each hook has a unit test. Finding a closure that needs one is review; section 3 of the design guide records the P4-15 audit. |

Also pinned: the handler manifest and its order (`ScriptHandlerOrderTests`), the entry layout of every tool that stages a server (`DotnetEntryLayoutTests`), the engine-test runners (`EngineRunnerParityTests`), and the parity tool's handling of split handlers (`ParityHandlerMappingTests`).
