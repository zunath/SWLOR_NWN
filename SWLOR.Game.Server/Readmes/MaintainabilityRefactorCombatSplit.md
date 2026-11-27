# Maintainability Refactor: Combat Split

The plan for converting `Service/Combat.cs` (13,179 lines, 8 handlers) and `Service/CombatService/` (32 files), from
item P2-D20 on 2026-11-07. Status lives in [MaintainabilityRefactorStatus.md](MaintainabilityRefactorStatus.md) (rows P2-CB0 to
P2-CB10). This file goes away at cutover.

Line ranges are for `Service/Combat.cs` at `7aacf4b85`, from a token scan rather than the compiler. P2-CB1 confirms them by
building and comparing sorted lines, and after P2-CB1 the part files replace them. If master changes the file before then, map
the ranges through the diff.

## Decisions

- **Order:** P2-CB0, P2-CB1, then P2-CB2 and P2-CB3 together, then P2-CB4 to P2-CB8 one at a time, then P2-CB9a and P2-CB9b
  together, then P2-CB10. Without P2-CB1 nothing can run in parallel, because the ranges interleave. After it only
  `ServiceRegistrations.cs`, `ConvertedServices.json`, `PlayerMessageAudit.json` and the `Forget*` lines conflict. The layers stay
  serial because a part injects only parts already merged.
- **Temporary guard entry (coordinator):** the guards forbid a half-converted `Combat`. A registered part must be listed
  (`FacadeForwardingTests`); listing `Combat` needs `ICombatService`, a facade equal to the interface and no static state; unlisted, it may
  not use `FacadeAccessor` (`ConvertedServicesTests`). So P2-CB0 adds an `inProgress` list to `ConvertedServices.json`. It may hold only
  `Combat`, a test says so, and P2-CB10 removes the list and that exception.
- **Callers:** every public member stays on the `Combat` facade. The one exception: `SWLOR.CombatSim` and `Entity/Player.cs:66`
  (`CreateDefaultDefenseValues`, from the constructor) call `CombatRules` directly, as with `ResistanceRules`. `SWLOR.CLI` and
  `SWLOR.CraftSim` use nothing from `Combat`; `SWLOR.Admin` uses only `CombatDamageType`.
- **Moved text stays verbatim:** moved bodies leave same-named static forwarders in the partial class, so unmoved code compiles
  unchanged, and `using static CombatRules` keeps call sites as they were. Handlers stay on the facade and forward.
- **Cycles:** this placement leaves two back edges, kept as `protected virtual` wrappers over `Service.Combat`:
  `ApplyTriggeredDamage` to `ApplyDamageTakenModifiers`, and `ApplyAreaAbilityAfterDeflectionDamageModifier` to `HasRecentDeflection`.
- **Master:** `Service/Combat.cs` had 77 commits in the five weeks to 2026-11-07. Each sync after P2-CB1 ports master's changes to the part
  that owns them.

## Static fields

`Dictionary` unless noted; (t) has a tuple key.

| Owner | Fields and lines | Written |
|---|---|---|
| Catalog | `_allValidDamageTypes`:70, `_allDefenseDamageTypes`:71 (List), `_damageTypesCached`:121 (bool) | cache event, first read |
| Resources | `_statTriggerCooldowns`(t):72, `_damageDerivedHealingStates`:73, `_passiveDamageHealingBudgets`:74, `_defeatedEnemyHealingBudgets`:75, `_abilityStaminaCosts`(t):105 | proc, damage scope, kill, ability |
| Activity | `_recentDamageTargets`(t):76, `_recentDamageTaken`:77, `_lastCombatActivity`:80, `_lastHostileAbilityAttemptActivity`:81, `_lastHostileIncomingActivity`:82, `_firstCombatAttackConsumed`:83 (HashSet), `_lastAttackActivity`:85, `_lastCombatAbilityUse`:86, `_firstHostileAbilityHitCounts`:93, `_stealthOpeningWindows`:95 | attack, ability; heartbeat purges 85-86 |
| DamageTaken | `_recentGuardedHits`:78, `_recentDeflections`(t):79 | guard, deflection |
| Suppression | `_pendingSuppressionAbilityUses`(t):87, `_containmentNormalizationTargets`:102 (HashSet) | ability, re-entrancy |
| Activation | `_hostileAbilitySequenceStates`:88 | ability |
| Impact | `_sameTargetHostileAbilityHitCounts`(t):90, `_areaAbilityTargetHitSequences`(t):96 | impact |
| HitEffects | `_criticalHitSequenceStates`:89, `_autoAttackCycleCounts`:91, `_sourceStatusAutoAttackCycleCounts`(t):94, `_meleeAutoAttackCycleCounts`:103 | hit |
| DamageModifiers | `_autoAttackCycleCriticalCounts`:92, `_repeatedTargetDamageStates`:99, `_meleeRepeatedTargetDamageStates`:100, `_rangedRepeatedTargetDamageStates`:101, `_sameTargetPressureStates`:104 | attack |
| AttackTiming | `_attackSwingDebts`:97, `_attackSwingDebtsWithoutLimitedReduction`:98 | swing |
| AbilityHit | `_abilityHitResolutionOverride`:8823, `_autoAttackHitResolutionOverride`:8837 (bool?) | test setters |
| Constant arrays | `_idleSkillAbilityStatChannels`:106, `MimicryTraitProcs`:1980 | never |

`RemoveStatTriggerCooldowns` (5474-5551) clears 31 of these on death or exit. After the split only `_recentDamageTargets` crosses parts
(2907, 2915, 4426), plus the type `TargetHitSequenceState`. Helpers that fail the static-state rule: `TemporaryStatModifier._modifiers` (and 2
handlers), `WeaponImpactProfile.Profiles`, `CombatDamageTypeExtensions.DamageAttributes`. `WeaponDamage` and `WeaponAttackTiming` stay static.

## Parts

Services are `Domain/Combat/<Name>Service` behind `I<Name>Service`, except `CombatRules` (static). Handlers are in **bold**. Lines 1-172 follow their
owners. The letters in Uses name other rows.

| Item | Name | Ranges | Lines | Main members | Runs | Uses |
|---|---|---|---|---|---|---|
| CB3 | `CombatRules` (static) | 293-377, 384-438, 515-519, 926-935, 1154-1181, 1604-1616, 1653-1671, 3062-3066, 3080-3097, 4536-4554, 4880-4888, 5288-5305, 5389-5417, 6525-6535, 7236-7244, 7325-7330, 7416-7420, 8066-8124, 8606-8626, 8815-8822, 9000-9027, 9318-9325, 9331-9336, 9683-9691, 9787-9796, 10633-10637, 10696-10703, 11706-11760, 11826-11833, 11880-11889, 11908-11926, 12036-12055, 12086-12103, 12313-12326, 12554-12564, 12710-12820, 12978-13007, 13019-13061 | 845 | `CalculateHitRate`, delay formulas | attack | - |
| CB3 | `CombatDamageCatalog` | 173-292 | 120 | **`LoadDamageTypes`**, **`AddDamageTypeResistances`** | boot, enter | - |
| CB4 | `CombatFeedback` (F) | 3990-4044, 12364-12553 | 245 | `BuildCombatLogMessage` | attack | - |
| CB4 | `CombatQueries` (Q) | 378-383, 596-609, 2106-2111, 2185-2190, 2607-2643, 3010-3043, 3511-3531, 4889-4897, 5381-5388, 5418-5454, 6943-6954, 7157-7206, 7272-7281, 7674-7698, 8943-8999, 9173-9186, 9284-9290, 9326-9330, 9605-9632, 11761-11825, 11890-11907, 11927-12035, 12056-12085, 12565-12655 | 699 | `GetEquippedWeaponSkillType`, `GetCombatImpactWeaponDamage` | attack | - |
| CB4 | `CombatResources` (R) | 3044-3061, 3067-3079, 3098-3203, 4434-4478, 5455-5473, 5977-5989, 10461-10632, 10638-10695, 10704-10739, 11198-11254, 11834-11879 | 583 | `TryUseStatTrigger`, `BeginDamageDerivedHealing` | hit | Q |
| CB5 | `CombatAttackTiming` (T) | 9337-9604, 12104-12312, 12656-12709, 12821-12977, 13008-13018, 13062-13165 | 803 | `ConsumeAttacksPerSwing`, `CalculateAttackDelay` | swing | Q |
| CB5 | `CombatSuppression` (S) | 6726-6942, 9106-9172, 9187-9256, 11089-11133 | 399 | `ApplySuppressionStack` | attack | Q |
| CB5 | `CombatActivity` (A) | 1397-1412, 2803-2818, 3377-3510, 3532-3551, 3623-3629, 3754-3768, 6955-7156, 11070-11088, 11255-11450 | 625 | `TrackAttackActivity`, **`PurgeInvalidOffensiveActivity`**, **`RestoreIdleReadinessOnPlayerEnter`** | attack, heartbeat | Q R S |
| CB6 | `CombatAbilityBonus` (B) | 3552-3622, 3630-3650, 9633-9682, 9692-9786, 9797-10189, 10239-10460, 10740-10867, 12327-12347 | 1,001 | `PrepareOpeningAutoAttack`, `StoreQueuedWeaponAbilityCriticalRateBonus` | attack | Q R A |
| CB6 | `CombatDamageModifiers` (M) | 439-514, 520-595, 610-614, 1039-1153, 1182-1281, 2081-2105, 2112-2184, 3651-3753, 4479-4535, 4555-4879, 4898-5256, 5366-5380, 13166-13178 | 1,342 | `ApplyDamageDealtModifiers`, `CalculateDamageWithCriticalMitigation` | damage roll | Q R |
| CB7 | `CombatAbilityHit` (H) | 8688-8814, 8823-8942, 9028-9105, 9257-9270, 9291-9317 | 366 | `TryResolveAbilityHit` | ability | Q R S A M |
| CB7 | `CombatHitEffects` (E) | 1282-1396, 1413-1603, 1617-1652, 1672-2080, 2191-2606, 2689-2712, 2819-3009, 6660-6674, 7207-7235, 7245-7271, 7282-7294, 7369-7415, 8012-8065, 9271-9283 | 1,580 | `ApplyDamageDealtEffects`, `ApplyTriggeredDamage` | hit | Q R T S A B M H |
| CB8 | `CombatDamageTaken` (D) | 615-925, 936-1038, 2644-2688, 2713-2777, 3204-3376, 3769-3989, 4045-4433, 5257-5287, 5306-5365, 8125-8183, 11488-11553 | 1,523 | `ApplyDamageTakenModifiers`, **`ApplyDamageTakenEffects`** | damage roll, event | F Q R T A B M H E |
| CB9a | `CombatAbilityActivation` | 5553-5908, 8184-8605, 8627-8666, 10868-11069, 11134-11197, 11451-11487 | 1,121 | `ApplyAbilityActivatedEffects` | ability | Q R T A B D |
| CB9b | `CombatAbilityImpact` | 5909-5976, 5990-6524, 6536-6659, 6675-6725, 7295-7324, 7331-7368, 7421-7673, 7699-8011, 8667-8687, 11554-11705 | 1,585 | `ApplySuccessfulAbilityImpactRiders` | impact | Q R T A B E D |
| CB10 | `CombatService` | 2778-2802, 5474-5552, 10190-10238, 12348-12363 | 169 | **`ApplyDefeatedEnemyEffects`**, **`ClearStatTriggerState`**, **`ClearCombatState`** | death, exit, 6s | all |

## Items

- **P2-CB0, tests only.** A `CombatSource` helper (reads the part files first, restores `static`, maps injected calls back to what the tests
  expect) and reflection helpers; `FacadeServices` in the six characterization fixtures; `FacadeContainer` in the 34 test files that call `Combat.`; the
  `inProgress` list and its test. Risk: normalising hides real text changes.
- **P2-CB1, mechanical.** `static partial class Combat`, one `Service/CombatParts/Combat.<Part>.cs` per row with its fields; `RemoveStatTriggerCooldowns`
  becomes `Forget*` calls; compare sorted lines against the old file. No logic change. Risk: order; 5511 and 5545-5550 touch status effects, so Activity and Resources
  need two `Forget*` calls each.
- **P2-CB2.** `TemporaryStatModifierService` behind facade `Service.TemporaryStatModifier` (a further facade name must start with `Combat`, so it is its own service). Risk: manifest
  829 and 853, `HandlerRenames.txt`, baseline edges, usings in 62 files.
- **P2-CB3.** `CombatRules`, `CombatDamageCatalog`. Risk: `Defenses` insertion order; snapshots.
- **P2-CB4.** F, Q, R. `WeaponImpactProfile` state joins Q; `DamageAttributes` becomes a `FrozenDictionary` of a sealed record. Risk: `DamageDerivedHealingScope` must capture the part.
- **P2-CB5.** T, S, A. Risk: per-swing allocation; idle `DelayCommand` closures.
- **P2-CB6.** B, M. Risk: default parameters; extend `CombatFacadeAllocationTests`.
- **P2-CB7.** H, E. Risk: `Random` draw order; recursion depth.
- **P2-CB8.** D. Risk: message audit rows.
- **P2-CB9a, P2-CB9b.** Activation, Impact. Risk: impact ordering.
- **P2-CB10.** `CombatService`, about 291 forwarders, list `Combat` with its extras, merge the partials, remove `inProgress`. Risk: `SampleValue` nulls; three hops per call.

## What breaks when code moves

- **Tests reading source text:** 27 test files, 89 sites (`CombatDamageTests` 26, `GeneratedWeaponPerkBehaviorTests` 15, `CrossSkillPerkInteractionSafetyTests` 11, whose line 214 excludes the path); up to
  262 needles contain `private static`-style signatures.
- **Roslyn readers:** `DamageOverTimeStatusEffectTests:242`, `PerkRefundStatusEffectCleanupTests:208`, `PlayerMessageAuditTests:81,261`.
- **Manifests:** `ClockSeamFiles.txt:5`, `PlayerMessageAudit.json` (33 rows).
- **Reflection:** `CombatAttackDelayTests:472,495,591,616,658`, `LeadershipOffensivePartyBuffTests:35,79,82`, `LeadershipCombatUpgradeTests:123,628,661`, `GeneratedWeaponPerkBehaviorTests:46-47`,
  `AreaAbilityFragmentationTests:117,197`, `StatAdjustmentSourceTests:224,236`, `CrossSkillPerkInteractionSafetyTests:425`, `RifleIdleReadinessEngineTests:19,21`.
- **Snapshot:** `CombatHitAndCriticalCharacterizationTests:106` snapshots the 26 public constants.

## Offline members `SWLOR.CombatSim` uses

All pure, all go to `CombatRules`: `CalculateHitRate`, `CalculateCriticalRate`, `CalculateDamageRange`, `CalculateAbilityCriticalChance`, `CalculateAbilityHitStaminaRestore`, `CalculateAttackDelayMilliseconds`,
`CalculateEffectiveAttackDelay`, `CalculateAttackSwingDelay`, `CalculateAttacksPerSwing`, `ApplyPercentDamageAdjustment`, `ApplyOrdinaryAttackDamageMultiplier(int,int)`, `ApplyTargetHPDamageAdjustment`,
`ClampCriticalDamagePercentAdjustment`, `CapOutgoingDamageBonus`, `IsWeaponSkillType`, `StandardCriticalRating`, `MaximumNormalDamageReductionPercent`.
