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
