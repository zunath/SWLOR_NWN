# Maintainability Refactor Specs

Task specs for work items in [MaintainabilityRefactorStatus.md](MaintainabilityRefactorStatus.md). Each spec
is given to a subagent together with [MaintainabilityRefactorAgentBrief.md](MaintainabilityRefactorAgentBrief.md).
Items whose spec fits in the status table have no section here.

## Findings that shape Phase 0 (from P0-3, 2026-10-26)

- Game randomness in combat, stats, abilities, skills, crafting and loot goes through the static
  `Service/Random.cs` (`System.Random` wrapper with `D2`-`D100`, `Next`, `NextFloat`,
  `GetRandomWeightedIndex`). It already has `SetSeed(int)` / `ResetSeed()`, used by the engine tests
  (`SWLOR.Game.Server.EngineTests/Framework/EngineTestContext.cs`). It is global: tests that seed it must not run
  in parallel.
- Not covered by the seed: `System.Random.Shared` in `Native/WeaponAttackAnimation.cs` (animation variant), the
  NWScript dice (`d2()`...`Random(n)`, used by weather, traps, HoloCom, a few status effects and the harvester,
  none in the locked systems), `Guid.NewGuid` and `GetOrAssignRandom` (identities, not outcomes).
- Wall-clock time is read directly: about 44 `DateTime.UtcNow` in `Combat.cs`, 15 in status effects, 7 in stats,
  perks, abilities and crafting, plus `Environment.TickCount64` in weapon repositioning and paired ranged
  presentation, and engine timers (`DelayCommand`). Many `CombatService` state types already take `DateTime now`.
  Locking stateful combat paths needs a clock seam (Tier B).
- Existing partial overrides: `Combat.SetAbilityHitResolutionOverride` and `SetAutoAttackHitResolutionOverride`
  (hit roll only).
- `SWLOR.CombatSim` is a balance simulator that re-implements parts of the math and calls a few pure `Combat`
  functions; it is not a characterization harness.

## P0-4 - Output-locking tests, Tier A (unit level)

**Goal:** lock today's outputs of the game's pure calculations so later refactoring cannot change them silently.
Tier A covers code that runs without the engine. Tier B (engine scenarios with a clock seam) is a later item.

**Project:** new NUnit test project `SWLOR.Game.Server.CharacterizationTests` (same test packages and versions as
`SWLOR.Game.Server.Tests`), added to `SWLOR.Game.Server.sln`. If `.github/workflows/maintainability-ci.yml` exists
on the base when you start, add the project to it.

**Rules:**
- No production code changes in Tier A. Use only public APIs that exist on `master` today, so the same project
  can run against `master` for parity (the parity script copies it into a `master` worktree).
- Every test that uses randomness calls `Random.SetSeed(<fixed seed>)` first and `Random.ResetSeed()` after;
  fixtures that seed are `[NonParallelizable]`.
- A function qualifies only if it runs without the engine. If calling it throws because NWN.Core is not
  initialised, leave it out and list it in the project's README section of your pull request description as a
  Tier B candidate.
- Inputs are deterministic grids covering normal values, boundaries and edge cases (0, negative where the
  signature allows, caps, very large values). Keep each snapshot small enough to review: prefer coarse grids
  plus the boundary values.
- Snapshots are JSON files checked into the project (`Snapshots/<Area>/<Function>.json`), written with stable
  ordering and formatting. A mismatch fails with a readable diff and the regenerate command
  (`SWLOR_UPDATE_SNAPSHOTS=1`, the same variable the boot-order test uses). Never regenerate a snapshot to make a
  failing test pass unless the behaviour change is intended and approved.
- Each snapshot records, beside the outputs, the inputs and the seed, so a reviewer can read it.

**Slices (one pull request each, in this order):**
1. **P0-4a Combat:** `Combat.CalculateHitRate`, `CalculateCriticalRate`, `CalculateDamage` (seeded, recording the
   sequence of results over many rolls), `CalculateDamageRange`, the damage-with-critical-mitigation path if it
   is reachable without the engine, `CalculateAbilityCriticalRating` / `GetAbilityCriticalRate` /
   `CalculateAbilityCriticalChance`, `StandardCriticalRating`, the attack delay functions that take numbers
   (`CalculateAttackDelayMilliseconds`, `CalculateEffectiveAttackDelay`, `CalculateAttackSwingDelay`,
   `CalculateAttacksPerSwing`), `CalculateAutoAttackProcDamage` (with a recorded roll function), healing budget
   and stamina-restore calculations (`CalculateMaxHPHealingBudget`, `CalculateDamageEligibleForHealing`,
   `CalculateCappedDamageDerivedHealingAmount`, `CalculateHitPointsSpentThroughTemporaryHitPoints`,
   `CalculateHitPointSpendStaminaRestore`, `CalculateAbilityHitStaminaRestore`), and any other public static
   numeric helper in `Combat.cs` that qualifies.
2. **P0-4b Stats, abilities, perks:** pure stat aggregation and formulas in `Stat.cs` / `StatService`; ability
   definition data (costs, recast delays, requirements, targeting) and perk definition data (levels, prices,
   requirements) read from the built definitions, as sorted JSON.
3. **P0-4c XP, crafting, loot:** the skill XP chart and award calculations; crafting calculations and
   `BlueprintBonuses.PickBonus` (seeded); loot tables (`LootTable.GetRandomItemWithRareBonus` seeded over many
   draws, quantity rolls) and loot table definition data.

**Done (P0-4):** the slice's tests pass; the snapshots are reviewed for plausibility (no obviously broken values, such
as every result zero, which would mean the function did not really run); the Codex loop is clean.

## P0-6 - Scripted live smoke run

**Goal:** one repeatable command that boots the branch's server locally, logs a client in and walks the core
player loop, so every phase can end with the same smoke run (owner decision 4 replaces per-PR client evidence
with it).

**Use what exists:** `tools/NwnLiveClient.py` and the live-server procedure in
`Readmes/InEngineVisualVerification.md` (unique ports and compose project names; never take the owner's login;
the client's CD key handling stays exactly as that guide says). Read that guide fully before starting.

**Scenario (each step records a pass/fail line and a screenshot):**
1. Server boots from this branch's build; the client reaches the module.
2. Log in an existing test character (or create one through the normal flow if the guide's procedure does).
3. Melee and ranged attacks against a spawned training target; damage numbers appear.
4. Use one active ability with a cooldown.
5. Open the bank (or another storage container) and move an item in and out.
6. Craft one item at a crafting station.
7. Advance one quest step (accept a simple quest).
8. Transfer to another area and back.
9. Log out cleanly.

Where a step needs content placement (target, station, quest giver), prefer DM/console commands the guide
already uses; do not change module content. A step that cannot be automated reliably is recorded as manual with
the reason.

**Deliverables:** the script under `tools/` (for example `tools/RunSmokeTest.ps1` driving `NwnLiveClient.py`),
a short section in `Readmes/InEngineVisualVerification.md` describing the smoke run and its output, and a
baseline run on this branch: results file plus screenshots kept outside the repository (path in the report).
Clean up every container, client profile and server you start.

**Done:** the script runs end to end twice in a row with the same results; the baseline is recorded.

## P0-10 - Explicit handler order

**Problem (from P0-5):** `ScriptRegistry` runs each script's handlers in the order reflection returns them,
which follows the compiler's type emission order (namespace depth-first, then sorted file paths). Moving a file
or namespace, or adding an unrelated file, can reorder handlers. Some order is load-bearing (`DB.Load` must run
before `EventRegistration.OnModulePreload` in `mod_preload`). The dependency-injection phase moves many files,
so the order must stop depending on file layout first.

**Design (decided by the coordinator, 2026-10-26):** an ordering manifest.
- A production resource lists, per script, the handlers in today's order (`Namespace.Type.Method`, kind). Generate
  it from today's discovery order; it is the same content as the P0-5 snapshot body, so keep one format and one
  generator (the snapshot test then compares the registry's order to the manifest plus discovery of unknown
  handlers, and the manifest becomes the single source of order).
- `ScriptRegistry.LoadHandlersFromAssemblies` orders each script's handlers by their manifest position. Handlers
  missing from the manifest (new ones) run after the listed ones, in discovery order, and are logged once at boot
  as unlisted so they get added. Manifest entries whose method no longer exists are ignored and logged.
- Behaviour-neutral proof: with the generated manifest, the registry's order equals today's discovery order for
  every script (test against `LegacyHandlerDiscovery` from P0-5), and it stays equal after a test that reorders
  discovery (for example by passing types in reverse) - proving file layout no longer matters.
- After the final merge this changes how content authors add handlers on `master`: a new handler runs after the
  existing ones for its script until it is placed in the manifest. Document this in `Readmes/CoreSystems.md` in the
  same pull request.
- Daily sync: `master` may add handlers; the sync regenerates the manifest entries for new handlers from `master`'s
  discovery order so parity holds (record this in the P0-7 scripts).

**Done:** manifest in place, order tests and seam proof pass, CoreSystems.md updated, Codex loop clean.

## P0-8 - Cross-domain dependency guard

**Goal (owner decision 9):** record which domains call which today and fail any new cross-domain dependency, so
the code stops tangling further while the refactor runs. Later phases move each domain into its own namespace;
this guard works before and after those moves because it maps types to domains, not files.

**Domain map (coordinator's first cut, 2026-10-26; refine as conversions happen).** A "service" is the static
class `SWLOR.Game.Server.Service.<Name>` plus every type in `SWLOR.Game.Server.Service.<Name>Service.*`.

| Domain | Services | Feature folders |
|---|---|---|
| Core | DB, Log, Cache, Random, Time, GameMath, ColorToken, Messaging, Communication, BackgroundJob, FrameProfiler, Migration, Authorization, Menu, Conversation, ConversationMenu, Snippet, PlayerFeedback, ObjectVisibility, Walkmesh, TileMagic, AnimationPlayer, NamedAnimation, NamedAnimationPlayback, Animation (folder), Attribute (folder) | MigrationDefinition, SnippetDefinition, DialogDefinition |
| UI | Gui | GuiDefinition |
| Combat | Combat, CombatPoint, CombatRecorder, Enmity, PvpCombat, Resistance, Recast, RecastTimestamp, Telegraph, Targeting, QueuedAttackAnimation, WeaponAttackEligibility, WeaponAttackPhase, WeaponDelay, OrdinaryAttackTraits, PairedRangedTraits, MeleeTechnique, TwinPistolAbility, TwinPistolEquipment, TwinPistolMastery, PowerGloveEquipment, Stealth, Death, Poisons, BattleGrid | - |
| Abilities | Ability, AbilityCooldownVisual, QuickCast, Mimicry, MimicryLoadouts | AbilityDefinition |
| Character | Stat, Skill, Perk, StatusEffect, Race, Achievement, Activity, PlayerActivityEffectTag, PlayerDescriptor, PlayerName, PlayerIntroduction, Disguise, EspionageInfiltration, Language, RoleplayXP (folder) | PerkDefinition, StatusEffectDefinition, AppearanceDefinition |
| Items | Item, ItemTrash, EquipmentUnequip, KeyItem, Loot, LightsaberWorkbench, OverflowStorage | ItemDefinition, LootTableDefinition |
| Crafting | Craft, Slicing (folder) | RecipeDefinition |
| Economy | Bank, Currency, PlayerMarket, Guild | - |
| Quests | Quest, QuestContractBoard, QuestContract (folder), Faction | QuestDefinition |
| World | Area, Planet, Weather, Music, Shuttle, Taxi, Property, Fishing, HoloCom, Traps, Notes | FishingLocationDefinition, PropertyLayoutDefinition, TrapDefinition |
| Space | Space, SpaceModuleRecast | ShipDefinition, ShipModuleDefinition, SpaceObjectDefinition |
| Creatures | AI, Spawn, NPCGroup, NPC (folder), CompanionControl (folder) | AIDefinition, SpawnDefinition |
| Companions | BeastMastery, IncubationFieldNote, Droid, DroidEquipmentDiagnostics | BeastDefinition |
| Social | ChatCommand, ChatPortraitTells, Party, PlayerList | ChatCommandDefinition |
| Administration | DMBlueprintCatalog, DMCreatorFavorite, DMCreatureManagerFavorite, DMObjectDetails, DMObjectSearch, DMPlayerDirectory, DMPreference, DMPresence, DMTool (folder) | - |
| Gameplay | (catch-all) | every top-level `Feature/*.cs` file and any `Feature` folder not listed above |

Types outside `Service` and `Feature` (`Core/`, `Native/`, `Entity/`, `Enumeration/`, `Extension/`) belong to
domain `Platform`; record their edges too but do not fail on edges into `Platform` or `Core`.

**Design:**
- New NUnit project `SWLOR.Architecture.Tests` (same test packages as the other test projects), added to the
  solution and, if it exists on the base, to `.github/workflows/maintainability-ci.yml`.
- Read dependencies from the compiled `SWLOR.Game.Server.dll` IL (System.Reflection.Metadata or Mono.Cecil): for
  every method body (including compiler-generated closures and lambdas, attributed to their declaring user type)
  record references to methods, fields, properties and types of other services. This is exact and ignores
  comments and strings.
- The domain map lives in a checked-in JSON file in the test project. A service or `Feature` folder that is in
  no domain fails the test with "add it to the domain map" (new services from `master` get mapped during the sync).
- Baseline: a checked-in, sorted list of today's cross-domain edges at service level (`Combat -> Stat`), with the
  count of distinct referencing members as information only. The test fails when (a) a new cross-domain service
  edge appears, or (b) a baseline edge no longer exists (keep the baseline tight). Edges into `Core` and
  `Platform` are recorded but never fail. Regenerate with `SWLOR_UPDATE_SNAPSHOTS=1` (reviewed like any snapshot).
- Report totals in the test output: edges per domain pair.

**Done:** guard passes on the branch; a deliberately added cross-domain call fails it with a clear message; the
baseline and map are reviewed for sense; Codex loop clean.

## P0-4d - Output locking Tier B: clock seam and engine scenarios

Two pull requests, in order.

### P0-4d1 - Clock seam (behaviour-neutral)

- Add `SWLOR.Game.Server.Core.Clock`: `UtcNow` (returns `DateTime.UtcNow` by default) and `TickCount64`
  (returns `Environment.TickCount64` by default), plus a test-only override that returns an `IDisposable` restoring
  the default. No other behaviour.
- A committed, re-runnable script under `tools/Refactor/` (PowerShell or Python, no new dependencies) rewrites
  `DateTime.UtcNow` -> `Clock.UtcNow` and `Environment.TickCount64` -> `Clock.TickCount64` in exactly the files of the
  locked systems and their direct helpers: `Service/Combat.cs`, `Service/CombatService/**`, `Service/CombatPoint.cs`,
  `Service/OrdinaryAttackTraits.cs`, `Service/PairedRangedTraits.cs`, `Service/StatusEffect.cs`,
  `Service/StatusEffectService/**`, `Service/Stat.cs`, `Service/Perk.cs`, `Service/AbilityService/**`,
  `Service/Craft.cs`, `Native/PairedRangedPresentation.cs`, `Native/OnAIActionAttackObject.cs`. It adds the `using`
  only where needed, is idempotent, and prints what it changed. The daily sync and the parity script re-run it.
- Tests: the default `Clock` equals the system clock (tolerance), the override restores itself, and a source guard
  fails if any listed file reads `DateTime.UtcNow`/`Environment.TickCount64` directly (so new `master` code is caught
  at sync time). Running the script twice changes nothing.
- No other edits to those files; the diff must be the mechanical rewrite only.

### P0-4d2 - Engine scenarios

- In `SWLOR.Game.Server.EngineTests`, a `Characterization/` folder with scenario tests that seed `Random`
  (`EngineTestContext.SeedRandom`) and freeze `Clock` to a fixed instant (advancing it explicitly where a scenario
  needs time to pass). Read `Readmes/EngineTesting.md` first (attribute contract, determinism, fixtures, timing).
- Scenarios (fixed creatures, stats and gear from existing fixtures): weapon attacks of each weapon class against a
  target (hit, crit, damage, procs); a representative set of abilities per skill (outcome, damage, status effects
  with durations, resource changes); status effect application and ticking over frozen-then-advanced time; XP awarded
  for a kill; loot spawned from a table; one crafting attempt.
- Each scenario records its outcomes into a snapshot (same JSON format as the CharacterizationTests helper; reuse
  the format, not the assembly) and compares it; regenerate with `SWLOR_UPDATE_SNAPSHOTS=1` through the existing
  engine-test runner environment.
- Run with `scripts/run-engine-tests.ps1 -Filter Characterization`; check that no other `swlor-engine-tests` compose
  project is running first (AgentEnvironment.md). Run twice; results must be identical.

**Done:** both pull requests merged; the scenarios pass twice in a row; a deliberate formula change fails them.

## P0-7 - Daily sync and parity scripts

Two PowerShell scripts under `tools/Refactor/` (they also run under PowerShell 7 on Linux), each printing a short
summary and exiting non-zero on failure. Neither pushes; the agent running them pushes after reviewing.

### `Sync-MaintainabilityBranch.ps1`

1. Requires a clean worktree on `feature/maintainability-refactor` (or a branch from it); fetches `origin/master`
   (`-c http.sslBackend=schannel`) and merges it (merge commit, never rebase).
2. On conflicts: for each conflicted file that a mechanical script owns (listed in a small manifest the script
   reads, starting with the P0-4d1 clock seam files), take `master`'s version and re-run that script; leave every
   other conflict for the agent, listing the files and the `master` commits that touched them (for the port log).
3. Re-runs every mechanical script (clock seam, and later ones as they are added to the manifest), then builds and
   runs the unit, characterization and architecture tests with the standard filters.
4. Prints what to record in the status file's sync log: date, `master` commit merged, conflicts, test results.

### `Test-MaintainabilityParity.ps1`

1. Creates a throwaway worktree of `origin/master` (detached, outside the repository's tracked paths; sparse haks as
   AgentEnvironment.md describes), never committed and always removed at the end, even on failure.
2. Applies the branch's behaviour-neutral seams there: re-runs the mechanical scripts, and cherry-picks the seam
   commits listed in the manifest (P0-5's `LoadHandlersFromAssemblies`; later seams as added).
3. Copies `SWLOR.Game.Server.CharacterizationTests` and the boot-order tests from the branch into it, runs them with
   `SWLOR_SNAPSHOT_DIRECTORY` (and `SWLOR_UPDATE_SNAPSHOTS=1`) pointed at a temporary folder, then compares that
   reference output with the branch's snapshots file by file.
4. Exit 0 only when they are identical; otherwise prints each differing snapshot and row.

**Done:** both scripts run on this machine; one real sync and one parity run are recorded in the status file;
documented briefly at the top of each script.

## P1-1 - Engine seam for NWScript (Phase 1)

**Goal:** unit tests can run game code that calls the engine, by swapping the engine for a fake, without changing
any of the thousands of call sites (`GlobalUsings.cs` imports `SWLOR.NWN.API.NWScript.NWScript` statically).

**Today:** `SWLOR.NWN.API/NWScript/*.cs` (54 files, about 1,127 public static members) are hand-written wrappers;
most forward to `global::NWN.Core.NWScript`, some contain extra logic (and see `NWScriptCustom/`).

**Design (coordinator, 2026-10-26):**
- A committed generator tool (a small console project under `tools/Refactor/`, Roslyn syntax only, deterministic,
  re-runnable; never hand-edit its output) reads the wrapper sources and writes, into `SWLOR.NWN.API/Generated/`:
  - `INWScript` - one interface member per public static wrapper member, same name, parameters, defaults and
    return type;
  - `NwnCoreNWScript : INWScript` - the original method bodies moved verbatim (instance methods);
  - `NWScriptRuntime` - holds the current `INWScript` (default `NwnCoreNWScript`) with a test-only override scope
    (`IDisposable`; `AsyncLocal` so parallel tests do not leak into each other);
  - and rewrites each static wrapper body to forward to `NWScriptRuntime.Current`.
  The rewrite keeps every static signature byte-for-byte, so no call site changes.
- A second output, in a new project `SWLOR.NWN.API.Testing`: `FakeNWScript : INWScript` that records every call
  (name and arguments) and returns configurable values (per member, with defaults: zero, empty string,
  `OBJECT_INVALID` for object ids). Tests can assert on the recorded calls - this is also how later output-locking
  tests record what game code asked the engine to do.
- Members that cannot be expressed on an interface (for example generic or `ref` oddities) stay static; list them.

**Behaviour-neutral proof:**
- A test reflects over the static `NWScript` class and `INWScript`: every static member has a matching interface
  member and forwards to it (call each through a recording fake and assert exactly one matching call with the same
  arguments).
- A test that the default runtime is `NwnCoreNWScript` and that the override scope restores it.
- The generator's output is checked in; a test (or CI step) re-runs the generator and fails if the checked-in output
  differs. The moved bodies must be textually identical to the originals (the generator verifies this while running).
- CharacterizationTests snapshots stay byte-identical; the architecture tests keep passing.

**Sync:** add the generator to the mechanical-script manifest for P0-7 (on a conflict in `SWLOR.NWN.API/NWScript`,
take `master`'s files and re-run the generator).

**Done:** generator and outputs merged; proofs pass; an example unit test runs a small engine-calling game function
against `FakeNWScript`; Codex loop clean.

## P1-2 - Engine seam for NWNX plugins

Same approach and proofs as P1-1 for the static plugin wrappers in `SWLOR.NWN.API/NWNX/*Plugin.cs`: one interface per
plugin (`IChatPlugin`, ...), a pass-through implementation holding the moved bodies, a runtime with an override
scope, forwarding statics, and generated fakes in `SWLOR.NWN.API.Testing`. Reuse the P1-1 generator.

## Phase 2 - Dependency injection (conversion recipe and order)

**Decisions (coordinator, 2026-10-27):**
- Container: `Microsoft.Extensions.DependencyInjection` in `SWLOR.Game.Server`, built once at boot in a new
  `Composition/` folder, with `ValidateOnBuild` and `ValidateScopes`. Every game service is a singleton (process
  lifetime; per reload generation in Phase 4). No scoped lifetimes in this phase.
- **Facades stay until the end.** A converted service keeps its static class (`Service/<Name>.cs`) with the same public
  members, each forwarding to the container's instance. Content code, definitions and handlers keep calling the static
  API, so nothing outside the service changes. Only facades may resolve from the container (through one internal
  accessor in `Composition/`); an architecture test forbids service location anywhere else.
- **Handlers stay on the facades.** `[NWNEventHandler]` methods remain static methods of the facade type and forward
  to the instance, so the handler-order manifest (P0-10) keeps its names. Moving handlers off the facades is part of
  facade removal at the end of the phase.
- **Engine calls stay bare.** Converted code keeps calling NWScript and NWNX through the global static imports; tests
  swap the engine with `NWScriptRuntime.Override` and the P1-2 plugin runtimes. Do not inject `INWScript`.
- Instance class: `<Name>Service` (or the existing folder name's convention) implementing `I<Name>Service`, in the
  service's domain namespace (the P0-8 domain map). Static mutable state becomes instance fields. Dependencies on other
  converted services are constructor-injected interfaces; calls to not-yet-converted services keep using their static
  facades (they convert later).
- Each conversion pull request also replaces that service's Roslyn-recompile or reflection-reset test harnesses (the
  rest of Phase 1) with ordinary tests using the seams, and brings the service's own lines to at least 70% coverage.

**Recipe for one service (one pull request; Sonnet builds, Opus reviews behaviour-sensitive ones):**
1. Read the service, its folder, its callers (P0-8 baseline edges), its handlers (manifest) and its tests.
2. Create the interface and instance class; move logic and state verbatim into instance members; the facade forwards.
3. Register it in `Composition/` (one line); add it to the converted-services list that the architecture test uses to
   forbid static mutable fields in converted services.
4. Move files into the domain namespace if the map says so. File moves change reflection order: the manifest keeps the
   handler order; if `ScriptHandlerManifestBaselineTests` fails for that reason, delete it in this pull request as its
   comment says (the layout-independence tests remain).
5. Tests: unit tests against the instance with fakes (engine runtimes, fake dependencies); coverage of the service's
   lines at least 70%; CharacterizationTests unchanged; Architecture.Tests unchanged except the converted-services list;
   handler-order tests unchanged.
6. Record in the status file: service, pull request, coverage before and after.

**Order (waves; within a wave, up to three services in parallel when their files do not overlap):**
- **P2-0 pilot:** the composition root, the facade accessor, the architecture rules, `Readmes/DependencyInjection.md`,
  and three small services with handlers: `Achievement` (Character), `Faction` (Quests), `Taxi` (World). The pilot
  settles the pattern; later items follow it.
- **Wave A, peripheral domains:** World, Economy, Social, Administration, Quests, Space (services with few callers first).
- **Wave B:** Items, Crafting, Companions, Creatures.
- **Wave C, shared infrastructure:** Core services other than `DB` (Log, Cache, Messaging, ...), then `DB` alone with
  extra review (persistence; data format must not change).
- **Wave D, the hot core (last, short freeze each):** Character (`StatusEffect`, `Skill`, `Perk`, then `Stat`),
  Abilities (`Ability`), Combat (`Combat`, split by responsibility as it converts).
- **P2-Z facade removal:** move handlers to instance classes (update manifest names in place), replace static calls in
  content code with injection where the caller is itself converted, delete facades area by area.
