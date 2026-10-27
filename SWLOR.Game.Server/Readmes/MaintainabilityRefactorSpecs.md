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

**Done:** the slice's tests pass; the snapshots are reviewed for plausibility (no obviously broken values, such
as every result zero, which would mean the function did not really run); the Codex loop is clean.
