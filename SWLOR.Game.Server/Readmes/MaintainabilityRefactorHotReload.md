# Maintainability Refactor: Hot Reload (Phase 4)

The plan for Phase 4 of [MaintainabilityRefactorPlan.md](MaintainabilityRefactorPlan.md), from item P4-0 on 2026-11-10. Status
lives in [MaintainabilityRefactorStatus.md](MaintainabilityRefactorStatus.md) (the coordinator adds rows P4-1 to P4-18). P4-17
writes the working guide (`Readmes/HotReload.md`); this file goes away at cutover.

Facts come from the branch at `968e3338d`, `C:\Projects\NWN.Hosting` (packages `0.1.0-preview.1`), Xenomech's main checkout
(read only, shape only) and the server image `zunath/nwn-dotnet:8193.37.17-3`. Paths without a project are under
`SWLOR.Game.Server/`; "NH" is the NWN.Hosting repository.

## 1. Goal and limits

- Load a new build of the game code into a running development or test server without restarting the NWN process: players stay
  connected, the world (areas, objects, effects, inventories, Redis) stays as it is.
- Production keeps restarting (owner decision 5). Reload is off unless configuration turns it on, and the host refuses to turn it
  on when `SWLOR_ENVIRONMENT` is `Production`.
- No behaviour change for players. Everything that only runs on a reload is new code that production never executes. Everything
  production does execute (the host boot path, split boot handlers, moved hooks) must be shown equal to today by the existing
  output-locking, boot-order, engine and smoke checks.
- `SWLOR.Game.Server` stays a normal library for the tools and tests that reference it (`SWLOR.CLI`, `SWLOR.Admin`, the sims, every
  test project).

## 2. Findings

### Entry point and boot

- NWNX enters `SWLOR.Game.Server.Core.ServerManager.Bootstrap` (`Docker/swlor.env` lines 78-81: `NWNX_DOTNET_NEW_BOOTSTRAP=1`,
  `NWNX_DOTNET_ASSEMBLY=/nwn/home/dotnet/SWLOR.Game.Server`). `ServerManager`'s static constructor creates the bootstrapper,
  `NativeInteropManager`, `ScriptRegistry`, `ScriptExecutor` and `MainLoopProcessor` (`Core/ServerManager.cs`).
- `Core/ServerBootstrapper.cs` runs seven steps: `InitializeNWNCore` (`NWNCore.Init(_closureManager)`), `RegisterNativeHandlers`,
  `InitializeSWLORSystems` (`Log.Register`, `ScriptExecutionProvider.SetProvider`), `BuildServiceContainer`, `RegisterEventHandlers`
  (`AppDomain.UnhandledException`), `LoadEngineTestAssembly` (into the game assembly's load context), `LoadScripts`.
- `Core/NativeInteropManager.cs` registers the four NWNX callbacks (main loop, run script, closure, signal) as
  `[UnmanagedCallersOnly]` statics. The signal `ON_MODULE_LOAD_FINISH` calls `ScriptExecutor.Initialize`, which stores the native
  `CVirtualMachine`.
- The run-script return value is the script's result: `ScriptExecutor.ProcessRunScript` returns -1 (not handled), 0 (actions ran)
  or 1/0 for a conditional script, and NWNX stores it as the script's return value (`DotNETExports.cpp` in NWNX unified,
  `RegisterRunScriptHandler`). Four conditional handlers exist: `SnippetService.ConversationAppearsWhen` under `appear`, `appears`,
  `condition`, `conditions` (every conversation "appears when" check).
- `Core/ClosureManager.cs` implements `ICoreFunctionHandler`: an unbounded `Dictionary<ulong, Action>` of pending
  `DelayCommand`/`AssignCommand`/`ActionDoCommand` closures and the current `OBJECT_SELF`. The server has 465 such call sites.
- `Core/MainLoopProcessor.cs` runs every frame, outside any closure, with `OBJECT_SELF` invalid: `FrameProfiler.OnFrameStart`,
  `OnScriptContextBegin`, `NwTask.MainThreadSynchronizationContext.Update()`, `Scheduler.Process()`, `OnScriptContextEnd`.
- The container (`Composition/CompositionRoot.cs`, `ContainerHost.cs`) is built once; `GameServices` properties cache per
  container (`Composition/FacadeCache.cs`), so a new container per generation needs no cache clearing (`DependencyInjection.md`).
- Script dispatch: 713 handler lines for 232 scripts in `Core/ScriptHandlerOrder.txt`, an embedded resource, bound by
  `Core/ScriptRegistry.cs` to container singletons.

### Boot handlers

Five boot buckets hold 104 handlers: `mod_preload` 3, `mod_content_chg` 1, `mod_cache_bef` 43, `mod_cache_aft` 15, `mod_load` 42.
Two more scripts belong to boot and shutdown: `db_loaded` (1 handler, run by `DBService.Load`) and `app_shutdown` (2). After a
reload the module is loaded and the engine runs none of these again, so the new build runs the rerunnable ones itself.

Classification for reload (rule: a handler reruns when it only fills its own instance state or the game assembly's own statics,
reads the engine, Redis or resources, writes nothing persistent and creates nothing; `Scheduler` timers and static registries
live in the game assembly and die with it, so a handler that only starts a timer or registers into them reruns too):

| Class | Count | Handlers |
|---|--:|---|
| Rerun | 72 | `mod_cache_bef`: all 43 (the 39 cache builds, `PlanetService.CacheData` whose `SetLocalInt` per area is idempotent, `SpaceService.LoadSpaceSystem` (caches and two timers), `KatarAnimationRemap.RegisterEquipmentAnimationRestoration`, `UnarmedMastery.Register`). `mod_cache_aft`: 14 (all but `MigrationService.AfterCacheLoaded`). `mod_load`: 15 (`AchievementService.ReserveGuiIds`, `PerformanceWindowsService.ReserveGuiIds`, `AreaService.RemoveInstancesFromCache`, `FrameProfiler.Load`, `LanguageService.LoadTranslators`, `RaceService.LoadRaces`, `ResistanceService.LoadResistanceTypes`, `WalkmeshService.RetrieveWalkmeshes`, `EquipmentStats.RegisterStatActions`, `RoleplayXP.LoadOOCDetection`, `AppearanceEditorViewModel.LoadAppearances`, `MarketBuyViewModel.LoadCategories`, `CoxxionTerminalDialog.LoadDoors`, `PerformanceAudienceService.StartAudienceTick`, `PairedRangedPresentation.StartParking`) |
| Boot only | 11 | `ServerTasks.ProcessBootStart` (webhook), `WalkmeshService.LoadWalkmeshes` (writes Redis), `MigrationService.AfterCacheLoaded` (migrations, vault `.bic` upgrade), `PlayerMarketService.RemoveOldListings`, `PropertyService.OnModuleLoad`, `WeatherService.InitializeWeather`, `AreaConfiguration.ApplyAreaConfiguration`, `CZ220FlightsTerminal.SpawnHangarFlightsTerminal`, `FeatConfiguration.ConfigureFeats`, `FeedbackMessageConfiguration.ConfigureFeedbackMessages`, `TlkOverrides.OverrideTlks` (the last three set engine state that survives a reload) |
| Split | 6 | `DBService.Load`, `EventRegistration.OnModulePreload`, `ServerTasks.ProcessBootUp`, `ShuttleService.LoadShuttleFlights`, `SpaceService.LoadLandingPoints`, `StoreManagement.ProcessStores` |
| Native hook | 15 | the `Native.*.RegisterHook(s)` handlers of `mod_load` (see "Native hooks") |

`db_loaded` (`MigrationService.AfterDatabaseLoaded`) is boot only. The two `app_shutdown` handlers (`CombatRecorderService.StopOnShutdown`,
`Log.OnApplicationShutdown`) also run when a generation retires. P4-7 re-checks every line before recording it.

Hard cases:

1. `Domain/Foundation/DBService.cs` `Load` (lines 80-111): opens the Redis connection and blocks up to 10 minutes for it, registers
   entity types, syncs search indexes when a stored fingerprint differs (writes Redis), subscribes `_cachedEntities.Clear` to
   `ServerManager.OnScriptContextEnd`, then fires `db_loaded`.
2. `Feature/EventRegistration.cs` `OnModulePreload` (lines 28-64): installs the `LegacyItemProperties` hook, binds module and area
   event scripts and about 270 NWNX events by script name (these persist in the engine and route to whichever build dispatches the
   script; NWNX ignores a repeated subscription, `Plugins/Events/Events.cpp` `SubscribeEvent`), adds an `AppDomain.ProcessExit`
   lambda (line 591) and a 6-second `Scheduler` heartbeat (line 606), runs `mod_content_chg` when the module file's mtime differs
   from Redis, then runs `mod_cache_bef` and `mod_cache_aft` through `ExecuteScript`.
3. `Feature/StoreManagement.cs` `ProcessStores`: adds the price increase to every store item's base value (lines 93-94); a second
   run compounds prices. Must never rerun.
4. `Domain/Space/SpaceService.cs` `LoadLandingPoints`: replaces each dock waypoint's `STARSHIP_DOCKPOINT_ID` with a new GUID
   (lines 187-219); a rerun changes ids that ships and properties hold.
5. `Domain/Creatures/SpawnService.cs` `CacheData`: new `Guid.NewGuid()` spawn ids per area (lines 187, 213); live creatures keep
   the old ids, so area entry would spawn a second set unless the new build adopts them.
6. `Domain/World/PropertyService.cs` `OnModuleLoad`: city upkeep and elections, lease clean-up (Redis writes) and spawning every
   property from Redis. A reload needs a rebuild of the loader state from the live objects instead.
7. `Domain/World/WeatherService.cs`: `LoadData` resets the weather pattern, `InitializeWeather` rolls new weather for every area.
8. `Domain/UI/GuiService.cs` `ReserveIds` (line 733): a running counter in call order; a rerun must reserve in boot order.
9. `Domain/Foundation/CacheService.cs` `CacheData`: creates and destroys every item blueprint once; slow, so it sets the reload pause.
10. `Domain/Foundation/MigrationService.cs` `AfterCacheLoaded`: a candidate build that adds a migration must not activate (see the schema
    gate below).
11. `ShuttleService.LoadShuttleFlights`: rebuilds flight state from Redis (rerun) but also delivers offline passengers (Redis
    writes) and starts a 2-second timer.

### Native hooks

36 native functions are hooked in 17 files of `Native/`, every one through `NWNXAPI.RequestFunctionHook` with an
`[UnmanagedCallersOnly]` static of the game assembly. Every hook calls the original; none is ever released (NWNX exports
`ReturnFunctionHook`, unused). Most guard a second install with a static field of the game assembly, which a new build would see
unset; `GetDamageRoll`, `OnAIActionAttackObject`, `ResolveAttackRoll` and `ChatPortraitSpeakers` have no guard.

| File | Installed by | Hooked functions |
|---|---|---|
| `ChatPortraitSpeakers.cs` | `RegisterHooks`, `mod_load` | 9: `CNWSMessage::SendServerToPlayerChat_` Talk, Whisper, Party, Tell, Shout, Silent_Shout, DM_Talk, DM_Whisper, DM_Silent_Shout |
| `VitalityHitPoints.cs` | `RegisterHooks`, `mod_load` | 7: `CNWSCreature::GetMaxHitPoints`, `CNWSCreatureStats::SetCONBase`, `CalcStatModifier`, its destructor, `CNWSObject::ApplyEffect`, `RemoveEffect`, `RemoveEffectById` |
| `PlayerFileCheckpoint.cs` | `RegisterHooks`, `mod_load` | 3: `CNWSCreature::SaveCreature`, `LoadCreature`, `CNWSCreatureStats::ReadStatsFromGff` |
| `StaleDriveAfterJump.cs` | `RegisterHooks`, `mod_load` | 2: `CNWSCreature::AIActionJumpToPoint`, `AddDriveAction` |
| `WeaponAttackAnimation.cs` | `RegisterHooks`, `mod_load` | 2: `CNWSMessage::ComputeUpdateRequired`, `WriteGameObjUpdate_UpdateObject` |
| `CombatRecorderEffectHooks.cs` | `Install`, lazily at the first recording (`Domain/Combat/CombatRecorderService.cs` line 124) | 2: `CNWSEffectListHandler::OnApplyDamage`, `OnApplyHeal` |
| `LegacyItemProperties.cs` | `RegisterHook`, from `EventRegistration.OnModulePreload` | `CNWSItem::LoadItem` |
| 10 files, `RegisterHook`, `mod_load` | | `ClientJsonUtf8Guard` `CNWMessage::ReadJSON`; `GetDamageRoll` `CNWSCreatureStats::GetDamageRoll`; `NeckAppearanceSerialization` `CNWSMessage::AddItemAppearanceToMessage`; `OnAIActionAttackObject` `CNWSCreature::AIActionAttackObject`; `PlayerCreation` `CServerExoAppInternal::ValidateCreateServerCharacter`; `PlayerListNamePrivacy` `CNWSMessage::SendServerToPlayerPlayerList_Add`; `PlayerSlotAssignment` `CNetLayerInternal::SetSlidingWindow`; `QuickbarFeatLoading` `CNWSCreature::LoadQuickButtons`; `ResolveAttackRoll` `CNWSCreature::ResolveAttackRoll`; `SavedPlayerInventory` `CNWSItem::CompareItem` |

The plan's "32" is out of date. The combat hooks (`GetDamageRoll`, `ResolveAttackRoll`, `OnAIActionAttackObject`) and
`CombatRecorderEffectHooks` call services through `GameServices`; the rest keep state in statics of their own files
(`VitalityPlayerState`, `StaleDriveState`, two `[ThreadStatic]` fields in `VitalityHitPoints` and `SavedPlayerInventory`).
`ComputeUpdateRequired`, `WriteGameObjUpdate_UpdateObject`, `GetMaxHitPoints` and `CalcStatModifier` run per object per frame.

### Assembly scans

23 discovery scans over `AppDomain.CurrentDomain.GetAssemblies()` in 20 files, which during an unload would also see a retired
build's types: `Core/ScriptRegistry.cs` line 75 (handlers) and the definition scans in `AbilityService` (abilities), `PerkService`,
`StatusEffectService`, `BeastMasteryService`, `ConversationMenuService`, `ConversationService` (graphs), `MigrationService`
(server migrations), `SnippetService`, `CraftService` (recipes), `AIService`, `SpawnService`, `ItemService`, `LootService`,
`QuestService`, `ChatCommandService`, `SpaceService` (3: ships, ship modules, space objects), `GuiService` (window definitions and
their view models), `FishingService`, `PropertyCatalogService` (all under `Domain/`). The engine-test runner does the same
(`SWLOR.Game.Server.EngineTests/EngineTest.cs` line 204). Six scans are already scoped to one assembly
(`RecipeParameterProfiles`, `RecipeDefinitionLoader`, `CraftMinigameRoutingService`, `DBService` entity types,
`CharacterFileUpgrade.LoadMigrations`, `ConversationService` embedded graphs).

### Static state and leak sources

- **Die with the generation (fine once nothing outside points in):** `ServerManager`, `Scheduler` (26 `Scheduler.Schedule*` sites),
  `NwTask.MainThreadSynchronizationContext`, `MainLoopProcessor` events (subscribers: `DBService.Load`, `PlayerNameService`),
  `CompositionRoot`, `FacadeCache`, `GameServices`, `ApplicationSettings`, `FrameProfiler` (strings and numbers), `Random`, `Log`'s
  loggers, every service's instance state, the `Native/` statics.
- **Point from outside into the generation today:** the four NWNX callbacks and 36 hook pointers (native code); `NWNCore.Init`
  stores the `ClosureManager` in an `NWN.Core` static (NWN.Core is always shared); `ScriptExecutionProvider.SetProvider` stores a
  game type in a `SWLOR.NWN.API` static; `AppDomain.CurrentDomain.UnhandledException` (`Core/ServerBootstrapper.cs` line 120) and
  `ProcessExit` (`Feature/EventRegistration.cs` line 591) are never removed; `NReJSONSerializer.SerializerProxy` (set in the
  `DBService` constructor) is a static of NReJSON.
- **Threads:** one dedicated thread, `Domain/Combat/CombatRecordWriter.cs` line 38, started per recording and
  stopped at `app_shutdown`. No `System.Threading.Timer`, `Task.Run`, `LongRunning` or `HttpClient` in the server; webhooks and
  metrics go through the Redis stream that `SWLOR.BackgroundServices` reads. `Parallel.For` runs only inside migrations.
- **Redis:** `DBService._multiplexer` (StackExchange.Redis 3.0.17, never disposed) owns sockets and timers for the life of the
  process; NRediSearch and NReJSON wrap it.
- **Logging:** `Domain/Foundation/Log.cs` builds one Serilog logger per group (file sinks, plus console for errors) in static fields and
  disposes them on `app_shutdown`. `Log.WriteError` passes the `Exception` object to Serilog (lines 124-130); 22 files call it.
- **JSON:** Newtonsoft with default settings in 43 files (entity persistence through `Domain/Foundation/DBJsonSerializer.cs`,
  migrations); no `TypeNameHandling`. System.Text.Json only as `Utf8JsonWriter`, `JsonDocument` and `JsonNode` (no type metadata
  cached), so the package's `JsonSerializerOptions.Default` leak does not apply.
- **Other:** no `Expression.Compile`, `ConditionalWeakTable`, `ThreadLocal` or `MemoryCache`; `PropertyCityGovernanceService` line 99
  continues on pool threads (`ConfigureAwait(false)`).

### Persisted type names

Only simple class names, never namespaces or assembly names: Redis keys `{Type.Name}:{id}`, index names, `Index:{Type.Name}:` and
`IndexDefinition:{Type.Name}` keys (`Domain/Foundation/DBService.cs`), `EntityBase.EntityType = GetType().Name` (`Entity/EntityBase.cs`
line 21) queried by `DBQuery` (`Domain/Foundation/DBQuery.cs` line 156), status effect toggles restored after a restart
(`Domain/Character/StatusEffectService.cs` line 350) and engine effect tags (`Domain/Character/StatusEffectBase.cs` line
328). Moving types between assemblies or loading them per generation changes nothing; renaming one is the same break it is today.

### Deployment and runtime

- The game builds to `SWLOR.Game.Server/bin/<cfg>/net10.0` (`EnableDynamicLoading` already on). Local deploy: the post-build event
  runs `SWLOR.CLI -o` (`SWLOR.CLI/DeployBuild.cs`), which copies the output to `debugserver/dotnet`; compose mounts it read only at
  `/nwn/home/dotnet` (`Docker/docker-compose.yml`). Production: `scripts/deployment/swlor-deploy.sh` copies the Release output into
  `$SERVER_DOTNET_ROOT` and checks for `SWLOR.Game.Server.dll` and its `runtimeconfig.json` (lines 1405, 1541-1546). Engine tests
  stage the game and test outputs together (`scripts/run-engine-tests.ps1` and `.sh`, `Docker/docker-compose.enginetests.yml`);
  the live client stack takes `--dotnet <dir>` (`tools/nwn_live_client/stack.py`).
- Image: `Docker/Dockerfile` (`FROM nwnxee/unified:3d4c4e1`, Debian 12) installs `dotnet-runtime-10.0` from Microsoft's apt feed
  without a version, published as `zunath/nwn-dotnet:8193.37.17-3` (`scripts/deployment/server-image.txt`; the publish workflow
  runs only for the owner). The local copy of that tag carries `Microsoft.NETCore.App 10.0.14`. SDK: `global.json` 10.0.100,
  `latestFeature`.
- Packages: `NWN.Core` and `NWN.Native` 8193.37.4, `NWNX.NET` 1.0.3 (transitive), `Microsoft.Extensions.DependencyInjection` 9.0.9,
  `StackExchange.Redis` 3.0.17 (pulls `Microsoft.Extensions.Logging.Abstractions` 10.0.5, `RESPite`, `System.IO.Hashing`).

### The package and Xenomech

- `Nwn.Hosting` 0.1.0-preview.1 (net10.0) needs `NWN.Core` 8193.37.7, `NWN.Native` 8193.37.8, `NWNX.NET` 1.0.3.
  `NativeHost.Initialize` (NH `src/Nwn.Hosting/Bootstrap/NativeHost.cs`) registers the four NWNX callbacks, calls `NWNCore.Init` with
  its own `CoreFunctionHandler`, installs `ClosureStackRelease`, then each `INativeHostExtension.Install` and the consumer's
  `IHostMode`. `ReloadHost` (`src/Nwn.Hosting/Reload/ReloadHost.cs`) loads a build from bytes into a collectible context, finds the
  one `IGameplayGeneration`, validates it off the engine thread, runs an optional schema gate, quiesces and drains the old
  generation, disposes and unloads it, activates the new one on the engine thread and probes the old context's collection.
  Shared with every generation: `NWN.Core`, `NWN.Native`, `NWNX.NET`, `Nwn.Hosting.Contracts`, plus `GenerationLoadOptions`
  names. Development trigger: `GameReloadSignalWatcher` and `GameDevelopmentReloadHandler` (signal file, generation folders,
  receipts). `ThreadPoolKeepAlive.Require` demands `System.Threading.ThreadPool.ThreadsToKeepAlive=-1` in the host's
  runtimeconfig (NH `docs/threads-outlive-generations.md`: generation code runs only on the engine thread and the pool).
  `VirtualDispatchCacheSentinel` seals a runtime defect on 10.0.0 to 10.0.12 (NH `docs/generic-virtual-dispatch.md`); on 10.0.14 it
  does nothing. `Nwn.Hosting.Testing` has fixture bindings and `RequireNoRetainedContexts`; NH
  `tests/Nwn.Hosting.Tests/Reload/ReloadHostLifecycleTests.cs` reloads 50 fixture generations.
- Xenomech: a host project (entry `NativeBootstrap.Initialize`, three native extensions, the database store, root logger and
  `ReloadHost`), a game project with one generation class that builds its container in `ActivateAsync`, a host contracts assembly
  shared by name, the host published outside the volume and the game into a `game/` folder, and a 25-cycle unload test of the real
  game assembly.

### Package gaps for SWLOR

As published, the package cannot host SWLOR without changing what players see:

1. `NativeHost.OnRunScript` returns 0 for every handled script and `IHostMode.OnRunScript` returns only `bool`, so every conversation
   "appears when" check would be false. Xenomech's mode discards the conditional result too.
2. `CoreFunctionHandler` throws once 64 closures are pending (`MaximumPendingClosures`); SWLOR's closure dictionary is unbounded and
   a live server keeps many more pending.
3. Pending closures hold delegates of the generation that scheduled them, so an old build's `ActionDoCommand` that never fires keeps
   it loaded, and one that fires runs retired code after its container is disposed. The handler can only cancel everything by
   disposing itself (`CloseAndCancel`).
4. Frames run inside a closure assigned to the module (`OBJECT_SELF` is the module, run during the AI master's event pass) and only
   after `ON_MODULE_LOAD_FINISH`; SWLOR runs its scheduler at the start of the main loop with `OBJECT_SELF` invalid. Whether this
   changes anything players see is checked in P4-10; if it does, the package needs a direct frame option.
5. `IGenerationExecution.TryEnter` takes a lock and allocates per call, too costly for the per-object hooks; engine-thread hooks
   need a cheaper guard (SWLOR's side, see the design).

## 3. Target design

### Projects and shared assemblies

- `SWLOR.Game.Host` (new, the NWNX entry in every environment): `Bootstrap.NativeBootstrap.Initialize` calls `NativeHost.Initialize`
  with SWLOR's host mode and one `INativeHostExtension` per hook file. It owns the `ReloadHost`, the Redis connection, the reload
  options and the console log. csproj: `EnableDynamicLoading`, `GenerateRuntimeConfigurationFiles`, `linux-x64`,
  `ThreadsToKeepAlive=-1`. Nothing in it references a game type.
- `SWLOR.Game.Contracts` (new, shared by name): the sink interfaces the hooks forward to, `IScriptContext` (enter `OBJECT_SELF`, read
  the VM, built on the package's `CoreFunctionHandler`), `IRedisConnectionSource`, `IHostLifecycle` (module loaded, environment,
  reload on). It references only `Nwn.Hosting.Contracts` and `StackExchange.Redis`; sink parameters are native pointers and numbers.
- `SWLOR.Game.Server` (the reloadable build): `Composition/GameGeneration` implements `IGameplayGeneration`; `NativeSinks/` holds the
  managed bodies of the hooks; `Core/` loses `NativeInteropManager`, `ClosureManager` and the native steps of the bootstrapper.
- Shared names: `SWLOR.Game.Contracts`, `StackExchange.Redis` and its dependency closure. Per generation: everything else, including
  `SWLOR.NWN.API`, `Newtonsoft.Json`, `NRediSearch`, `NReJSON`, `Serilog`, `Microsoft.Extensions.DependencyInjection`, so their
  static caches go with the build.

### A generation

- `ValidateAsync` (off the engine thread): builds and discards the container with `ValidateOnBuild` (constructors already may not
  call the engine).
- `ActivateAsync` (engine thread): `Log.Register`, `ScriptExecutionProvider.SetProvider`, builds the container, binds the native
  sinks, registers script handlers, exposes `Scripts` and `MainLoop`. Generation 1 then waits for the engine to run the boot
  scripts as today. A later generation runs the reload bootstrap: the rerun boot handlers in manifest order, then the reload-only
  adoption steps, then re-attaches online players.
- `BeginQuiescenceAsync` (engine thread): saves online players' managed state through the logout paths, closes NUI windows and
  conversations, clears the `Scheduler` and `NwTask` queues, stops the combat recorder. `DrainAsync`: waits for pool work.
  `DisposeAsync`: unbinds sinks, runs the `app_shutdown` handlers' clean-up, disposes the container and loggers, clears
  `ScriptExecutionProvider`.
- `RequiredSchemaVersion` is the newest server migration version in the build; the host's schema gate compares it with
  `SWLOR_CONFIG`'s `MigrationVersion` and rejects a candidate that would need a migration (restart instead).
- `GameServices`, `CompositionRoot`, `Scheduler`, `Log` and `FrameProfiler` stay static in the game assembly, so each generation has
  its own copies. This settles the Phase 2 deferrals: `Log`, `FrameProfiler` and `Random` stay where they are, per generation;
  the host logs to the console only. Generations never overlap (the old one is disposed before the new one activates), so log
  files are closed before they are reopened.

### Reload sequence

1. The developer publishes a candidate into `dotnet/generations/<id>/` and writes the signal file (P4-13 tool).
2. The host validates it while the server keeps running; a rejection leaves the old build untouched and writes a receipt.
3. From quiescence to activation the host mode keeps the engine thread inside one frame, pumping the reload's engine-thread work,
   so no engine frame runs without an active build (no unhandled scripts, no hooks without a sink). This pause includes the
   rerun caches; P4-14 measures it against the NWNX thread watchdog and the client timeout.
4. A failure after quiescence leaves the host in `RestartRequired`; the server is restarted.

### Native hooks

Each hook file becomes a host extension that resolves its symbols at `Install` but requests the hook only when the game asks
(`IHookControl.Enable(id)`, idempotent for the process), from the same handler and moment as today, so hook timing does not change.
The trampoline reads the bound sink (a `volatile` field) and calls it, or the original when none is bound. Sinks are bound at
activation and unbound at dispose, both on the engine thread inside the reload pause, so engine-thread hooks need no lease;
allocation tests pin the hot paths. The `[ThreadStatic]` re-entrancy guards move with the trampolines (the engine thread never
exits).

### What a reload keeps and loses

Kept: the NWN process and connections, engine objects and effects, positions, inventories, Redis, NWNX event bindings, engine
settings (feats, feedback, TLK overrides), installed hooks, the Redis connection. Rebuilt: every cache and timer, script dispatch,
per-player managed state (through logout and login persistence). Lost: closures the old build scheduled (dropped, never run), open
NUI windows and conversations (closed), in-memory state no persistence path covers (P4-15 lists it). Reload-only adoption replaces
the boot-only steps: live spawns, properties, dock points and weather are adopted, not recreated.

## 4. Items

| ID | Item | Model | Depends on |
|---|---|---|---|
| P4-1 | Pin the server runtime | Sonnet | - |
| P4-2 | Nwn.Hosting changes SWLOR needs (upstream) | Opus | owner Q1 |
| P4-3 | Align NWN.Core and NWN.Native with the package | Sonnet | P4-2 |
| P4-4 | Scope discovery scans to the game build | Sonnet | - |
| P4-5 | Removable process-wide registrations and a teardown path | Sonnet | - |
| P4-6 | Combat record writer on the thread pool | Sonnet build, Opus review | - |
| P4-7 | Classify boot handlers | Sonnet | - |
| P4-8 | Split the six mixed boot handlers | Opus | P4-7 |
| P4-9 | Contracts project and native sinks | Sonnet build, Opus review | P4-3 |
| P4-10 | `SWLOR.Game.Host`, one generation, reload off | Opus | P4-1, P4-3, P4-9, owner Q2 |
| P4-11 | Native hooks into the host | Opus | P4-10 |
| P4-12 | Many-reload unload test with the real build | Opus | P4-4, P4-5, P4-6, P4-11 |
| P4-13 | Reload switch, development trigger and single-pause reload | Sonnet build, Opus review | P4-12 |
| P4-14 | Reload bootstrap, adoption and schema gate | Opus | P4-8, P4-13 |
| P4-15 | Online players across a reload | Opus | P4-14, owner Q3 |
| P4-16 | Engine tests, parity and CI under the host | Sonnet build, Opus review | P4-15 |
| P4-17 | Guides and architecture rules | Sonnet | P4-16 |
| P4-18 | End-of-phase verification and live smoke | Opus | all |

Every item follows the brief and the gate of `MaintainabilityRefactorFacadeRemoval.md` ("Gate per item"): build, architecture tests,
characterization snapshots unchanged, handler-order tests, the area's unit filters. Items that change what production executes
(P4-5, P4-6, P4-8, P4-9, P4-10, P4-11) also run the engine characterization scenarios and allocation tests; the coordinator runs
the PowerShell parity and engine scripts that isolated agents cannot.

**P4-1 Pin the server runtime.** Scope: install `dotnet-runtime-10.0` and `dotnet-apphost-pack-10.0` at the exact version the
current image carries (10.0.14) in `Docker/Dockerfile`, bump the tag (`8193.37.17-4`) in `scripts/deployment/server-image.txt`, both
compose files, `swlor-deploy.conf.example`, `Readmes/Deployment.md`; a test that fails on an unpinned runtime line. Done: the image
builds locally and reports 10.0.14; the owner publishes it (the workflow is owner-only). Risk: the apt feed drops the version
(record the feed and keep the old tag).

**P4-2 Nwn.Hosting changes (repository `zunath/NWN.Hosting`).** Scope: a run-script result that carries the conditional value;
configurable closure capacity; dropping a retired generation's pending closures without disposing the handler (stale events
ignored); a direct frame option only if P4-10 shows the module-closure frame changes behaviour; release `0.1.0-preview.2`. Files:
NH `Bootstrap/NativeHost.cs`, `IHostMode.cs`, `CoreFunctionHandler.cs`, tests, docs. Done: package tests, release on nuget.org.
Risk: Xenomech consumes the same API; keep changes additive.

**P4-3 Align engine bindings.** Scope: `NWN.Core` and `NWN.Native` in every SWLOR project to the versions the released package needs
(today 8193.37.7 and 8193.37.8), lock files. Done: full unit suite, characterization, engine characterization scenarios. Risk:
binding changes between 8193.37.4 and the new versions; read their release notes and list them in the PR.

**P4-4 Scope discovery scans.** Scope: one helper (`Core/GameAssemblies`) that keeps `AppDomain.CurrentDomain.GetAssemblies()` order
but only the assemblies of the game build's own load context (the game and, when loaded, the engine-test assembly); the 23 scans
and `EngineTest.DiscoverTests` use it; an architecture rule forbids new `GetAssemblies()`. Done: a test shows each scan finds the
same types in the same order as before; handler-order tests. Risk: a scan that relied on another assembly (none found).

**P4-5 Process-wide registrations and teardown.** Scope: `UnhandledException` and `ProcessExit` subscriptions kept in removable
form; a `ServerTeardown` routine (stop timers, clear the `Scheduler` and `NwTask` queues, run the `app_shutdown` clean-up, clear
`ScriptExecutionProvider`) that `app_shutdown` uses today and generations use later; architecture rules: no `AppDomain` or other
process-wide event subscription and no `new Thread` outside an allow list. Done: shutdown behaviour tests unchanged. Risk: shutdown
order (combat recorder files, log flush).

**P4-6 Combat record writer on the pool.** Scope: `Domain/Combat/CombatRecordWriter.cs` runs its loop as pool work
with the same queue, ordering and flush; stop and drain on shutdown and quiescence. Done: recorder tests and a byte-identical record
file for a recorded fixture. Risk: ordering under load; recordings are developer-only.

**P4-7 Classify boot handlers.** Scope: an attribute on every handler of the boot scripts (`Rerun`, `BootOnly`, `Split`,
`NativeHook`), the 104 + 3 classifications above re-checked, a snapshot test that fails an unclassified boot handler; no behaviour
change. Done: the snapshot and handler-order tests. Risk: a handler classified rerun that writes state (review each against the rule).

**P4-8 Split mixed handlers.** Scope: `DBService.Load`, `EventRegistration.OnModulePreload`, `ServerTasks.ProcessBootUp`,
`ShuttleService.LoadShuttleFlights`, `SpaceService.LoadLandingPoints`, `StoreManagement.ProcessStores` each become a rerun part and a
boot-only part that run back to back at the old manifest position (manifest lines added in place, listed for the parity tool).
Done: boot order unchanged in effect, characterization and engine scenarios, the Redis connection behind `IRedisConnectionSource`
(default opens it as today). Risk: these are boot-critical (`DBService.Load` stays first).

**P4-9 Contracts and native sinks.** Scope: `SWLOR.Game.Contracts`; each hook file's managed body moves behind its sink interface in
`NativeSinks/` while the game still installs the hook and calls the sink. Done: allocation tests at their byte counts, engine
characterization (combat, vitality), unit filters for the 17 files. Risk: hot-path cost of the interface call.

**P4-10 Host, one generation.** Scope: `SWLOR.Game.Host` (entry, host mode, script dispatch with conditional results, frame,
`IScriptContext` from the package's `CoreFunctionHandler`, Redis connection, options), `GameGeneration`, the bootstrapper without
native steps; deployment layout (host in `dotnet/`, game in `dotnet/game/`) in `SWLOR.CLI` `DeployBuild`, `swlor-deploy.sh`, both
compose files and `swlor.env`, the engine-test scripts and the live client stack. Reload stays off. Done: unit, characterization,
full engine suite and `RunSmokeTest.ps1 -ServerOnly` on the host; a frame-timing comparison (scheduler ticks, `OBJECT_SELF`) against
the old entry. Risk: package gap 4; pool-thread NWScript calls that the package now refuses (`PropertyCityGovernanceService`).

**P4-11 Hooks into the host.** Scope: 17 host extensions, trampolines forwarding to sinks, `IHookControl.Enable` called from the
existing `RegisterHook(s)` handlers, `CombatRecorderEffectHooks` and `LegacyItemProperties` at their current moments. Done: allocation
tests, engine characterization, a test that every hook falls back to the original with no sink. Risk: hook order values and install
timing; vitality hooks running without a sink.

**P4-12 Many-reload unload test.** Scope: `SWLOR.Game.Host.Tests` loads the real Release build (copied, not referenced) through
`ReloadHost` 25 times with `Nwn.Hosting.Testing` bindings and the `SWLOR.NWN.API.Testing` fakes inside each generation, dispatches
every script name and binds every sink, then requires zero retained contexts and records the pause; added to
`scripts/ci/run-unit-tests.sh`. Done: the test passes; each leak it finds is fixed in its own PR first. Risk: fake-engine coverage of
activation; a leak in a dependency (shared closure of StackExchange.Redis).

**P4-13 Reload switch, trigger and single-pause reload.** Scope: `SWLOR_HOT_RELOAD` (off by default, refused in `Production`), signal
and receipt paths in a directory mount, `GameReloadSignalWatcher` and `GameDevelopmentReloadHandler`, a C# tool (isolated agents
cannot run PowerShell) that builds, publishes a candidate and signals, receipts in the server log; the host mode holds the engine
thread from quiescence to activation ("Reload sequence" step 3). Generation 2+ only rebuilds the container and dispatch here. Done:
tests for the switch and the production refusal; a local reload on `debugserver` with the pause logged. Risk: a switch that leaks
into production configuration.

**P4-14 Reload bootstrap and adoption.** Scope: activation of generation 2+ runs the rerun handlers in manifest order and the
reload-only steps: adopt live spawns, rebuild property loader state from live objects, read dock ids without rewriting them, keep
the weather state, recover shuttle and store state without writes, reserve GUI ids in boot order; the schema gate from migration
versions. Done: an engine run that reloads several times and compares cache digests (counts and hashes per service) with boot;
pause measured. Risk: adoption misses an object kind; pause length.

**P4-15 Online players across a reload.** Scope: classify the `mod_enter` (57) and `mod_exit` (45) handlers; quiescence saves managed
state through the logout persistence paths without engine-side logout effects, activation restores it through the login paths;
close NUI windows and conversations; list what is lost. Done: an engine run with a player fixture that keeps HP, effects, status
effects, cooldowns that persist and position across reloads. Risk: handlers that mix persistence with world effects.

**P4-16 Engine tests, parity and CI.** Scope: the engine-test assembly loaded into each generation from bytes, runner scripts and
compose for the new layout, `Update-EngineCharacterization.ps1`, the parity tool's handling of split handlers, a `CombatLoad`
comparison with the old entry, the engine-test workflow. Done: full engine suite and parity on the host. Risk: CI cannot run on
GitHub (billing lock), so local runs are the evidence.

**P4-17 Guides and rules.** Scope: `Readmes/HotReload.md` (how to reload, what is lost, the rules: classify boot handlers, no
threads or process events, no `GetAssemblies`, no exceptions to host loggers), updates to `DependencyInjection.md`,
`CoreSystems.md`, `EngineTesting.md`, `AgentEnvironment.md`, `Deployment.md`, `AGENTS.md` pointer. Done: Codex clean. Risk: stale
guidance.

**P4-18 Verification.** Scope: full unit and engine suites, parity, `RunSmokeTest.ps1 -Repeat 2` on the host, and the reload smoke: a
client connected through `tools/NwnLiveClient.py` (`Readmes/InEngineVisualVerification.md`), a candidate published and signalled,
the client still in the area with the same HP, inventory and effects, then smoke steps 3 to 8 again in the new generation. Done:
both runs pass with evidence. Risk: the busy-machine watchdog traps in the guide's "Smoke run" section.

## 5. Out of scope

- Reloading in production; reloading the host, contracts, hook trampolines or shared assemblies; builds that need a migration
  (rejected, restart); module, hak, TLK or 2DA changes (repack and restart as today).
- Carrying in-memory state that no persistence path covers; data-driven definitions (decision 7); plugin projects (decision 9);
  `SWLOR.Admin`, `SWLOR.BackgroundServices`, the toolset and the CLI.

## 6. Open questions for the owner

| # | Question | Recommendation | Owner answer (2026-11-10) |
|---|---|---|---|
| Q1 | May the package be changed for SWLOR's needs (conditional results, closure capacity, dropping a retired build's closures, a direct frame option if needed) and released as `0.1.0-preview.2`? Without the first two, conversations and delayed actions break on the host. | Yes: an agent opens the PRs in `zunath/NWN.Hosting` with the same review loop, additive so Xenomech keeps working; SWLOR pins the release (P4-2). | Yes. The owner pushes the packages to nuget.org with their own API key (`docs/releasing.md` in the package repository). |
| Q2 | Does production also boot through `SWLOR.Game.Host` with reload off, or keep today's entry? | Host everywhere: one boot path and one home for hooks. Gate it on parity, the `CombatLoad` comparison (the game then runs in a collectible context) and the test-server soak; if the collectible context costs frame time, add a package option to load production's single build non-collectible. | Yes, if it costs no performance. P4-10 compares `CombatLoad` and frame time on today's entry and on the host; production moves to the host only if it is no slower. P4-2 adds the option to load production's single build non-collectible for this. |
| Q3 | Is it acceptable that a reload on development and test servers drops the old build's pending delayed actions and closes open NUI windows and conversations, while players stay connected with world state intact? | Yes for development and test servers; P4-15 lists anything else that is lost, and production never reloads. | Yes, but only where it is required or too complicated technically to avoid. P4-15 keeps delayed actions, windows and conversations where it can and lists each loss with its reason. |
