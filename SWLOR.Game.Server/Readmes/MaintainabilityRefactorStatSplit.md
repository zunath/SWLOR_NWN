# Maintainability Refactor: Stat Split (P5-4 part map)

The plan for splitting `Domain/Character/StatService.cs` (3,358 lines, 22 constructor parameters, 118 `IStatService` members
including 6 script handlers). The Phase 5 table says 23 parameters: it was measured at `5282fe9ef`, before P5-1g removed
`Lazy<IDroidService>`. Status lives in [MaintainabilityRefactorStatus.md](MaintainabilityRefactorStatus.md) (row P5-4).
Targets and rules: [MaintainabilityRefactorPhase5.md](MaintainabilityRefactorPhase5.md) (target 2: at most 10 services per
type and under 1,000 lines; target 3: no method over 150 lines; target 4: no deferred resolution) and
[MaintainabilityRefactorDomainOrder.md](MaintainabilityRefactorDomainOrder.md) (the supply rule and the P5-4 rows). The model is
the [Ability split](MaintainabilityRefactorAbilitySplit.md). This file goes away at cutover.

Line ranges are for `StatService.cs` at `129c7e7bd`. Member line counts come from a syntax-tree scan (a member starts at its doc
comment). Dependency use counts are references to each injected field. Server callers are bound by
`tools/Refactor/DomainOrder/EdgeExtract`; call sites are counted by regex on the receivers `Stat`, `_stat`, `stat` and, in the
stat test classes, `Service`, with the tests read at `129c7e7bd`. Services per type are the guard's own count
(`ServiceScanner` and `LookupScanner`: constructor services plus `GameServices` lookups). If master changes the file first, map
the ranges through the diff.

## Decisions

- **Parts 5 to 7 move up to Combat** (owner, 2026-11-16, "Move up to Combat"). Every caller of these members is Combat or above,
  and their upward dependencies are Combat services, which become plain inside-Combat parameters. The alternative was rule 1:
  keep them in Character and have about ten Combat services push what they read into Character-owned stores.
- **Accepted growth** (coordinator, 2026-11-16): `CombatDamageTakenService`, `CombatAbilityHitService`, `AbilityDamageInputService`
  (10 -> 11), `SpaceService` and `CharacterSheetViewModel` may gain services, and `BeastMasteryService` may go 13 -> 14 in
  P5-4e, traded for its `Lazy<>` going 3 -> 2 through the `CapstoneProofService` `ICompanionLookup` swap. The pull request that
  causes each records it and names the item that removes it (Holders, below). No holder gains a `Lazy<>`.
- The Mimicry stat bonus follows the approved rule 1 rows; healing joins `CombatResourcesService`; the content static
  `TemporaryHitPointEffects.ScheduleLimitCheck` stays behind its seam for P5-12 (as P5-3 answer 3).

## What the code is

`StatService` has nine jobs. The stat read is at the bottom: almost every other job calls `GetStatAdjustment`.

| Job | Lines | Size |
|---|---|---:|
| Stat read: `GetStatAdjustment`, sources, metadata, `ScaleEffect`, the equipment action registry | 140-198, 2648-2695, 2992-3017 | 127 |
| FP and Stamina pools | 233-598, 2802-2809 | 365 |
| NPC stat skins | 2561-2573, 2841-2990, 3101-3117 | 178 |
| Max HP and food HP, attributes, NPC loading | 600-738, 1137-1176, 3078-3171 | 266 |
| Gear-fed player records, Combat Readiness, the 17 `Adjust...` writers | 740-966, 1178-1465 | 505 |
| Walk mode and the movement rate | 200-231, 968-1135 | 191 |
| Detection, Stealth, Lockpicking, Control, Craftsmanship | 2023-2133, 3019-3076 | 165 |
| Attack, Accuracy, Defense, Evasion, the native ability score read; deflection and Guard chances | 1467-2021, 2135-2249, 2426-2646, 2822-2839 | 795 |
| Deflection reactions; healing adjustments; NPC and beast regeneration | 2251-2466; 2697-2800; 3173-3290 | 413 |

The 25 `protected virtual` seams (3292-3356) are the test double's hooks; each moves with its caller. One method is over 150
lines: `ApplyDeflectionEffectsNative` (2251-2417, 167).

### Dependencies

| Dependency | Domain | Uses | Used by | After the split |
|---|---|---:|---|---|
| `IDBService` | Foundation | 35 | pools 12, records 10, ratings 8, other ratings 5 | parts 1, 2, 3, 5 |
| `ICharacterRecording` | Character | 17 | pools 8, regeneration 4, deflection reactions 3, healing 2 | parts 1, 2, 6, 7 |
| `IStatusEffectService` | Character | 5 | stat read 2, Control and Craftsmanship 2, food HP 1 | parts 1, 2 |
| `ICompanionLookup` | Character | 4 | movement 2, pools 1, healing 1 | parts 1, 2, 7 |
| `IItemService` | Items | 4 | shield checks | part 5 (Combat, down) |
| `ISkillService` | Character | 3 | weapon skill (ratings, deflection), skill details (Control, Craftsmanship) | parts 1, 5 |
| `IPowerGloveEquipmentService` | Combat | 3 | `GetEquipmentStatCopies` in three equipment reads | `PowerGloveStatCopies.For` (Foundation, the body it forwards to) |
| `ITemporaryStatModifierService` | Combat | 3 | stat read 2, deflection and healing writes 1 | moves to Character (rule 1) |
| `Lazy<ICombatAttackTimingService>` | Combat | 3 | the three `GrantNext...` deflection riders | part 6 |
| `Lazy<ICombatDamageModifiersService>` | Combat | 3 | low HP, low FP, nearby-status attack adjustments | part 5 |
| `Lazy<ICombatResourcesService>` | Combat | 3 | FP and Stamina restored effects 2, `TryUseStatTrigger` 1 | part 8 listener; part 6 |
| `IPlayerFeedbackService` | Foundation | 2 | `RestoreFP`, `RestoreStamina` | part 1 |
| `IMimicryService` | Abilities | 2 | trait stat bonus and sources in the stat read | part 4, a Character store (rule 1) |
| `ITwinPistolEquipmentService` | Combat | 2 | `GetAttackNative` | part 5 |
| `IRecastService` | Combat | 2 | deflection recast reduction | part 6 |
| `IResistanceService` | Combat | 2 | `DecodeItemPropertyCostTableValue` in the NPC skin reads | `ResistanceRules` moves to Character |
| `IPerkService` | Character | 2 | stat read | part 1 |
| `Lazy<ICombatQueriesService>` | Combat | 2 | weapon damage and accuracy ability types | part 5 |
| `IEnmityService` | Combat | 1 | out-of-combat HP regeneration | `IEnmityTargetQuery` passed by `AIService` |
| `IPvpCombatService` | Combat | 1 | healing reduction | part 7 |
| `ICombatDamageCatalogService` | Combat | 1 | `GetDefenseDamageTypes` in the record rebuild | a `DamageTableDefaults` list (Foundation) |
| `Lazy<ICombatDamageTakenService>` | Combat | 1 | `TrackDeflection` | part 6 |

### State and handlers

- `_outOfCombatMovementCombatState` (`Runtime`) goes to part 2; `_summedEquipmentRecords` (a readonly table) to part 3;
  `_equipmentStatActions` (filled by content at module load) stays in part 1.
- Every moved handler already has a `tools/Refactor/HandlerRenames.txt` line, which the move edits in place:
  - part 2 takes `ReapplyFoodHP` (manifest line 211), `ClearWalkModeBeforeDMPossessionChange` (448, 459), `ApplyPlayerStats`
    (761), `ForgetOutOfCombatMovementState` (831) and `RefreshOutOfCombatMovementRate` (970);
  - `TemporaryStatModifierService` moves namespace with `ClearExitingObject` (840, `OnModuleExit`) and `PurgeAllExpired` (864,
    `OnModuleHeartbeat`), renames lines 90 and 91, positions unchanged; its `Carried` field `_modifiers` gets
    `[ReloadCarriedFrom("SWLOR.Game.Server.Domain.Combat.TemporaryStatModifierService")]`.
- Two handlers are added, each as a split of an existing master handler, so they stand together with it in the manifest and
  the parity handler map accepts them (renames lines take several branch names for a split, `RefactorCommon.ps1`
  `Read-HandlerRenames`):
  - `MimicryTraitStatsService.CacheData` (`OnModuleCacheAfter`, `[BootPhase(BootReload.Rerun)]`) after
    `MimicryService.CacheData` (renames line 112);
  - `CreatureStatService.RegisterStatEffectListener` (`OnModuleCacheBefore`, `[BootPhase(BootReload.Rerun)]`) after
    `StatService.CacheData` (renames line 313, manifest line 54).

## Parts

Services are `<Domain>/<Name>` behind `I<Name>` and a `GameServices` property (`CreatureStat`, `StatRecord`,
`MimicryTraitStats`, `CombatRating`, `CombatDeflectionReaction`). "Deps" counts constructor parameters at the end. Lines are the
moved members plus seams, constants, constructor and fields, to about 10%.

| # | Part | Domain | Lines | Deps | Runs / state |
|---|---|---|---:|---:|---|
| 1 | `StatService` (stays) | Character | 950 | 10 | **`CacheData`**; every stat read and resource change |
| 2 | `CreatureStatService` | Character | 600 | 5 | **6 handlers** (7 lines); `_outOfCombatMovementCombatState` |
| 3 | `StatRecordService` | Character | 520 | 2 | per equip and login |
| 4 | `MimicryTraitStatsService` | Character | 150 | 2 | **cache handler**; per read of a trait stat |
| 5 | `CombatRatingService` | Combat | 915 | 7 | per attack roll |
| 6 | `CombatDeflectionReactionService` | Combat | 285 | 7 | per deflection |
| 7 | healing adjustments into `CombatResourcesService` | Combat | +101 (735) | 8 | per heal |
| 8 | `ResourceRestoredEffectsService` (from `CombatResourcesService`) | Combat | 45 | 1 | per FP or Stamina restore |

The deps:

1. `IStatusEffectService`, `IPerkService`, `ITemporaryStatModifierService` (now Character), part 4, `IDBService`,
   `ICharacterRecording`, `IPlayerFeedbackService`, `ICompanionLookup`, `ISkillService`, `IResourceRestoredListener`.
2. `IDBService`, `ICharacterRecording`, `ICompanionLookup`, `IStatusEffectService`, part 1.
3. `IDBService`, part 1.
4. `IDBService`, `IAbilityCatalog`.
5. `IDBService`, `ISkillService`, `IItemService`, `ITwinPistolEquipmentService`, `ICombatQueriesService`,
   `ICombatDamageModifiersService`, part 1.
6. `ICharacterRecording`, `IRecastService`, `ICombatDamageTakenService`, `ICombatAttackTimingService`, `ICombatResourcesService`,
   `ITemporaryStatModifierService`, part 1.
7. `CombatResourcesService`'s 6, plus `IPvpCombatService` and `ICompanionLookup`.
8. `IStatusEffectService` (the stat read comes in as an argument).

Part 1 is at the limit (10 services) and has about 50 lines of headroom; master's additions to it are checked on each sync.

### Members by part (line counts)

- **1:** `CacheData` 10, six metadata forwarders 1-2, `RegisterEquipmentStatAction` 8, `TryGetEquipmentStatAction` 5,
  `EquipmentStatActionTypes` 4, `ScaleEffect` 15, `GetStatSources` 20, `CopyStatSources` 5, `GetStatAdjustment` 9,
  `GetStatAdjustmentExcludingTemporaryModifiers` 11, `GetAbilityNameShort` 26; `GetMaxFP` 36 and 4, `GetCurrentFP` 25,
  `GetAdjustedRequiredFP` 13, `GetMaxStamina` 34 and 4, `GetCurrentStamina` 25, `RestoreFP` 57, `ReduceFP` 41,
  `IsStaminaRestorationBlocked` 8, `RestoreStamina` 57, `ReduceStamina` 51, `ApplyFPRestoreAdjustment` 8; `GetNPCStats` 12,
  `GetNPCSkinStats` 70, `GetNPCStatsNative` 66, `GetNPCSkinStat` 13 (private), `GetNPCSkinMaxHP` 17; `GetDetection` 36,
  `CalculateDetectionRating` 10, `ApplyNPCDetectionCap` 7, `GetStealth` 22, `CalculateStealthRating` 4, `GetLockpicking` 27,
  `GetCombatReadinessPercent` 20, `CalculateControl` 29, `CalculateCraftsmanship` 29.
- **2:** `ApplyPlayerStats` 10, `ApplyTemporaryPlayerStats` 10, `RestoreWalkMode` 10, `IsWalkModeEnabled` 3,
  `ClearWalkModeBeforeDMPossessionChange` 8, `ToggleWalkMode` 33, `ApplyCreatureMovementRate` 24,
  `RefreshOutOfCombatMovementRate` 24, `ForgetOutOfCombatMovementState` 7, `GetMovementSpeedMultiplier` 26,
  `GetMovementSpeedAdjustment` 25, `GetBaseMovementSpeedIncrease` 10; `ReapplyFoodHP` 13 and 17, `AdjustPlayerMaxHP` 58,
  `RefreshPlayerMaxHP` 28, `GetEquippedHPBonus` 19, `ApplyPlayerStat` 15, `GetPlayerAttributeScore` 5, `RestorePlayerAttributes` 8
  and 9; `LoadNPCStats` 8 and 13, `SetNPCMaxHitPoints` 53; `RestoreNPCStats` 7 and 53, `RestoreBeastStats` 9,
  `IsNaturalStaminaRegenerationAvailable` 4, `GetBeastNaturalRegenAmount` 8, `CanRestoreBeastStamina` 22.
- **3:** `RefreshPlayerEquipmentStats` 32, `GetEquippedStatTotals` 23, `ApplyEquipmentStatTotals` 90, `ApplyCraftStatTotals` 19,
  `AdjustPlayerMaxFP` 20, `AdjustPlayerMaxSTM` 20, `AdjustCombatReadiness` 10, `RefreshCombatReadinessEquipment` 24,
  `GetEquippedCombatReadiness` 19, the 17 `Adjust...` writers 10-17 each.
- **5:** `GetAttack` 57 and 8, `GetAttackNative` 58, `GetAccuracy` 81 and 8, `GetAccuracyNative` 67, `GetDefense` 49,
  `GetDefenseNative` 46, `GetEvasion` 45, 8 and 9, `GetEvasionNative` 52, `GetStatValueNative` 39,
  `GetHighFPAndStaminaAttackAdjustment` 21, `GetDefensePercentAdjustment` 20, `ApplyAccuracyItemProperty` 15, 13 rating helpers of
  2-11; six deflection chance reads 8-10, `GetGuardChance` 4, three caps 5-7, `GetMainHandSkillTypeNative` 8 (no callers), ten
  weapon and shield helpers 4-12.
- **6:** `ApplyDeflectionEffectsNative` 167, `GetDeflectionStatTypeForSource` 12, three helpers of 6.
- **7:** `ApplyOutgoingAbilityHealingAdjustment` 14, `ApplyHealingReceivedAdjustment` 14, `ApplyHealingReceivedEffects` 13,
  `ApplyHealingReceivedStaminaRestore` 24, `ApplyHealingReceivedAttackBoost` 14, three helpers of 4-7.
- `ApplyPercentAdjustment` (10, used by parts 1, 5 and 7) becomes `StatRules.ApplyPercentAdjustment`.

### Notes on the parts

- **1 stays wide on purpose.** The split is cut by who calls what. Part 1 keeps every member its holders read together with
  `GetStatAdjustment`: the pools, the NPC skin reads, and the Detection, Stealth, Lockpicking, Combat Readiness, Control and
  Craftsmanship reads. `ForgeViewModel` (10 services by lookup today) reads `CalculateCraftsmanship`, `CalculateControl` and
  `GetStatAdjustment`; with the four reads in part 3 it would enter `ServicesPerType.txt`. Part 1 keeps `IStatService`,
  `GameServices.Stat` and the zero-allocation pin.
- **2. Movement, HP, attributes, NPC loading and regeneration** are what `AIService`, `DroidService`, `SpawnService` and
  `BeastMasteryService` call, so they are one part. The out-of-combat HP block of `RestoreNPCStats` (3253-3263) reads
  `IEnmityService.GetHighestEnmityTarget`; only `AIService.cs:132` passes `outOfCombatRegen: true`. The block stays in place: it
  runs inside the `Regeneration` attribution scope (3221), and `CombatRecorderService.RecordHealingApplied` takes the heal's
  source from that scope. Character owns `IEnmityTargetQuery`, `IEnmityService` extends it, and `RestoreNPCStats` takes it as an
  argument: `AIService` passes its own `_enmity.Value`, the other callers pass `false` and `null`.
- **2. `StatusEffectService`'s notices.** It calls `ReapplyFoodHP` after restoring a player's effects (329) and
  `ApplyCreatureMovementRate` when a stat-adjusting effect is added, removed or resumed (362 in `OnCarriedStateAdopted`, 910,
  1291, 2237). Part 2 needs `IStatusEffectService` for the food check, so it cannot be injected back (rule 3, notices only):
  - Character owns `IStatEffectListener` (`OnStatusEffectsRestored`, `OnStatAdjustingEffectsChanged`);
  - `CreatureStatService.RegisterStatEffectListener`, `[BootPhase(BootReload.Rerun)]` on `OnModuleCacheBefore`, calls
    `IStatusEffectService.RegisterStatEffectListener(this)`; a reload runs rerun boot handlers before adoption
    (`ServerBootstrapper.RebuildAfterReload`), so the list is full when `OnCarriedStateAdopted` runs;
  - the list `StatusEffectService._statEffectListeners` is `[ReloadState(ReloadStateKind.Runtime)]` (HotReload.md rule 3: it
    holds service instances, which the cache digest cannot compare);
  - `StatusEffectService` throws `InvalidOperationException` when it notifies and the list is empty, so a missing registration
    fails at once; its unit-test bench registers a stand-in;
  - its `Lazy<IStatService>` then serves only `GetStatAdjustment` (3 sites). P5-5 folds the listener into its effect lifecycle
    events.
- **2 and the food check.** `FoodStatusEffect` implements a Character capability `IFoodStatusEffect : IStatusEffect`, and the
  check reads `GetStatusEffect<IFoodStatusEffect>`. `GetStatusEffect<T>` is constrained to `class, IStatusEffect`
  (`StatusEffectService.cs:2426`) and `CreatureStatusEffect.GetEffect<T>` is `OfType<T>`, so the answer is the same.
- **2 and `BeastMasteryService`, without a `Lazy<>`.** Part 2 depends on `IStatusEffectService`, and today
  `StatusEffectService -> ICapstoneProofService -> CapstoneProofService -> IBeastMasteryService` is a plain path, so part 2 in
  `BeastMasteryService` would need a fourth `Lazy<>` (`DeferredResolution.txt:42`). `CapstoneProofService` uses the beast service
  for `IsPlayerBeast` only (143), and Character's `ICompanionLookup.IsPlayerBeast` (`CompanionLookup`, no constructor
  dependencies) has the same body over the same `BEAST_TYPE` variable. P5-4e switches `CapstoneProofService` to
  `ICompanionLookup`; a scan of the constructor graph then finds no plain path from parts 1 and 2 to `BeastMasteryService`, which
  takes both as plain parameters, and its stat `Lazy<>` goes (3 -> 2). `LazyDependencyCycleTests` confirms it in the pull request.
- **4. Mimicry traits, a Character store (rule 1), named as a service** (P5-4b, below), as the approved rows say (domain order 361, 365, 385, 491-492; Ability split
  245). `MimicryService.GetStatBonus` and `GetStatSources` are called only by the stat read. The store holds the trait tables,
  `GetStatBonus`, `GetStatSources` and `GetSetBonusPotency`, and reads `Player.EquippedTechniques` and `AbilityDetail`, both at
  or below Character. Its reads take the stat reader as an argument (`StatService` passes `this`), so they use the live
  `MimicryTraitPotencyPercent` in the same order. `ScaleTraitBonus` becomes `MimicryTraitRules.ScaleTraitBonus`, which the store
  and `MimicryService.GetResistanceBonus` (297) call. Pushing the tables would add the store to `MimicryService` (11 -> 12), so the
  store builds them from `IAbilityCatalog` in its own cache handler; `MimicryService` stays at 11.
- **Temporary stat modifiers.** `TemporaryStatModifierService` (389 lines) has one dependency, `IGuiService`, for one
  `PublishRefreshEvent`; it moves to Character with Foundation's `IGuiRefreshPublisher`. Combat and content write and consume it;
  the stat read reads it (rule 1). Its `StatAdjustmentRefreshEvent` is already `Domain/Foundation` (the
  `Feature.GuiDefinition.RefreshEvent` using is stale), so `LogicToContent.txt` has no line for it and gains none.
- **5. Ratings and deflection chances are one part.** `CombatDamageTakenService` reads `GetAttack`, `GetDefense` and
  `GetGuardChance`; one part keeps it at one new parameter. Part 6 calls `TrackDeflection` on it, so the reactions are separate.
- **6. The deflection reaction split.** `ApplyDeflectionEffectsNative` tracks the deflection, reads every payload (23 values and 4
  stat types), then applies. The reads, verbatim, fill a readonly struct `DeflectionReaction` through an `out` parameter, and the
  applies read it into locals of the original names: no tuple, no closure, the order of reads and writes unchanged.
- **7. Healing joins `CombatResourcesService`.** `ApplyHealingReceivedStaminaRestore` calls `TryUseStatTrigger`, and
  `CombatResourcesService` calls the healing adjustments (6 sites), so a separate part would be a construction cycle.
- **8. Restored effects.** `ApplyFPRestoredEffects` and `ApplyStaminaRestoredEffects` (13 lines each, only caller the pools) leave
  `CombatResourcesService` for part 8, which implements Character's `IResourceRestoredListener`. Its methods take the stat reader
  as an argument, so it needs only `IStatusEffectService`, which part 1 already takes (rule 2). Their content types
  (`ForceLensForceAttackStatusEffect`, `ForceLensAttackStatusEffect`) move with them: `CombatResourcesService.cs ->
  Feature/StatusEffectDefinition` goes 3 -> 1 and `ResourceRestoredEffectsService.cs` gets a line with 2 (moved, not added).
- **Small moves down.** `ResistanceRules` (63 lines, Foundation dependencies only) moves to Character. `DamageTableDefaults` gains
  the defense damage type list (enum order, `IsDefenseDamageType`), which is what `GetDefenseDamageTypes` returns. The power glove
  seam calls `PowerGloveStatCopies.For`.

## The upward references and how each goes

`UpwardEdges.txt` lists `StatService.cs` with Character -> Combat (40 members), -> Abilities (4) and -> Items (4).
`DeferredResolution.txt` has its 5 up `Lazy<>`. `LogicToContent.txt` has 2 lines.

| Reference | Fix | PR |
|---|---|---|
| `ITemporaryStatModifierService` | the store moves to Character (rule 1) | b |
| `IMimicryService` | part 4, Character store (rule 1) | b |
| `IPowerGloveEquipmentService`, `IResistanceService`, `ICombatDamageCatalogService` | Foundation and Character statics | b |
| `IEnmityService` | `IEnmityTargetQuery` argument from `AIService` | b |
| `FoodStatusEffect` (content) | `IFoodStatusEffect : IStatusEffect` in Character | b |
| `ITwinPistolEquipmentService`, `IItemService`, `CombatRules`, `Lazy<ICombatQueriesService>`, `Lazy<ICombatDamageModifiersService>` | part 5 | c |
| `IRecastService`, `IPvpCombatService`, `PvpCombatRules`, `Lazy<ICombatAttackTimingService>`, `Lazy<ICombatDamageTakenService>`, `Lazy<ICombatResourcesService>` | parts 6, 7, 8 | d |
| `TemporaryHitPointEffects` (content static) | stays behind its seam in part 2 (P5-12) | e (re-keyed) |

No `Lazy<>`, `Func<>` or late lookup is added.

## Holders of `IStatService`

48 types take `IStatService` in a constructor (47 services and one content type, 11 of them as `Lazy<>`); the table also lists
`CharacterSheetViewModel`, which reads it by lookup, so its rows cover 49 types. No holder gains a `Lazy<>`, and every service
count that grows is one the coordinator accepted (or below the limit), recorded in its pull request.

| Holders (services today) | Parts | Outcome |
|---|---|---|
| 37 types: every other holder, among them the `Lazy<>` holders `PerkService`, `SkillService`, `MimicryService`, `ResistanceService`, `StealthService`, `EnmityService`, `MeleeTechniqueService` and `CombatRecorderService`, and `CombatAbilityMagnitudeService` and `SlicingSessionService` | 1 | unchanged |
| `StatusEffectService` (16, 1 inside `Lazy<>`) | 1 | unchanged; the two notices go through `IStatEffectListener` (e) |
| `AIService` (15) | 2 | `Lazy<IStatService>` narrowed to `Lazy<ICreatureStatService>` (e), same counts; `StatusEffectService -> IStealthService -> IAIService` is plain, so it stays lazy |
| `DroidService` (14), `SpawnService` | 2 | plain `ICreatureStatService` (e), same counts |
| `CombatResourcesService` (6) | 1, 7 | absorbs part 7: 6 -> 8 (d), under the limit |
| `CharacterLoginRestoration` (content, 4) | 2, 3 | 4 -> 5 (e), under the limit |
| `CombatDamageTakenService` (17), `CombatAbilityHitService` (11) | 1, 5 | +1 each, accepted; recorded in c; removed by P5-7 |
| `AbilityDamageInputService` (10) | 1, 5 | 10 -> 11, a new line, accepted; recorded in c; removed by P5-16 |
| `SpaceService` (16) | 1, 2 | +1, accepted; recorded in e; removed by P5-6 |
| `CharacterSheetViewModel` (19, lookups) | 1, 2, 5 | 19 -> 21, accepted; recorded in c and e; removed by P5-14 and P5-16 |
| `BeastMasteryService` (13, 3 down `Lazy<>`) | 1, 2 | both plain once `CapstoneProofService` takes `ICompanionLookup` (e): `Lazy<>` 3 -> 2, services 13 -> 14, accepted; recorded in e; removed by P5-10 |

## Callers

Bound referencing members by caller: Combat 352, content 310, Abilities 22, `Native` 20, Companions 14, Character 13, World 9,
Space 8, Administration 4, Crafting 4, Creatures 4, Social 3, Items 2. No caller below Combat reads parts 5 to 7.

| Destination | Domain | Content | Tests | Engine | Tools |
|---|---:|---:|---:|---:|---:|
| `IStatService` (stays) | 843 | 319 | 403 | 401 | 35 |
| `ICreatureStatService` | 18 | 15 | 106 | 120 | 4 |
| `IStatRecordService` | 0 | 61 | 66 | 12 | 43 |
| `ICombatRatingService` | 14 | 30 | 121 | 59 | 10 |
| `ICombatDeflectionReactionService` | 0 | 1 | 14 | 0 | 0 |
| `ICombatResourcesService` (healing) | 10 | 16 | 33 | 8 | 4 |

**Call text.** 123 of the 442 content and native sites change the property name only (`Stat.SetNPCMaxHitPoints` becomes
`CreatureStat.SetNPCMaxHitPoints`). `RemovedFacades.json` lists the members by owner, `Apply-FacadeRemoval.ps1` rewrites them,
and `-Reverse` maps master's text back for the parity build, as in P5-3. Public constants move with their part; the
characterization tests that name `StatService.<constant>` change the type name only (`nameof` keeps the snapshot name).

## Tests (P5-4a)

- **`StatSource`** (`Tests/Support`) is the source of `StatService` wherever it lives (`PartFiles`, `InterfaceFiles`,
  `PartInterfaces`; `Read`, `ReadInterfaces`, `Method`, `Member`, `Members`, `Nodes`, `Paths`). A call through a field of a part
  interface loses its receiver; nothing else is rewritten. `StatSourceTests` proves it. The 16 test files that read
  `StatService.cs` or `IStatService.cs` by path read it, directly or through `StatHarnessSource` (substring searches between two
  members became `Method` reads).
- **`StatHarnessSource`** reads the methods and constants the Roslyn harnesses compile from `StatSource`.
- **`StatBench`** and `StatServiceTestBase` give one accessor per part (`Service`, `CreatureStat`, `Record`, `CombatRating`,
  `DeflectionReaction`, `Healing`); the 363 calls in the 7 stat test classes use them. Until a part moves its accessor is the one
  instance. The seam answers are properties of the bench (`StatBench.Seams.cs`).
- **`TestStatService`** is split into partial files by the part whose code calls each seam (`.Stat`, `.Creature`, `.Record`,
  `.CombatRating`, `.DeflectionReaction`; `.Shared` holds the stat trigger and the temporary modifier, called by parts 6 and 7).
- **The allocation pins** (`StatFacadeAllocationTests`) are grouped by part, still through `Stat.`; the same 48 pins.

## Order

Part 1 is the bottom of the graph, so the split runs top-down: a part leaves only when nothing still in `StatService` calls it,
and a moved part calls the members still in `StatService` through `IStatService`.

| PR | Contents | `StatService` services after | Up `Lazy<>` left |
|---|---|---:|---:|
| P5-4a | This guide and the test groundwork | 22 | 5 |
| P5-4b | Part 4 and `MimicryTraitRules`; `TemporaryStatModifierService` and `ResistanceRules` to Character; the Foundation lists; `IFoodStatusEffect`; `IEnmityTargetQuery`; the Mimicry callers repointed | 18 | 5 |
| P5-4c | Part 5; `Native` and content call sites; the accepted growth recorded | 14 | 3 |
| P5-4d | Parts 6 (with its split), 7 and 8 | 10 | 0 |
| P5-4e | Parts 2 and 3; `IStatEffectListener`; `CapstoneProofService` on `ICompanionLookup`; `CraftSim` needles; every baseline regenerated | 10 | 0 |

P5-4b repoints every caller of the moved Mimicry members to `GameServices.MimicryTraitStats`: the engine tests
`MimicryTraitPotencyEngineTests.cs` (59-81), `AbilityBehaviorExecutor.cs` (212-217) and `MimicryTraitAbilityBehaviors.cs` (18,
41), and the unit tests `MimicryTests.cs` (616, 1206-1216) and `MimicryServiceTests`.

## Progress

### P5-4b (the trait stats, the moves down and the inversions)

Part 4 moved, `TemporaryStatModifierService` and `ResistanceRules` moved to Character, and the four services that only fed
helpers or one argument left `StatService`: 22 -> 18 services (`IPowerGloveEquipmentService`, `IResistanceService`,
`ICombatDamageCatalogService` and `IEnmityService` gone; `IMimicryService` became `IMimicryTraitStatsService`), 3,358 -> 3,348
lines, the 5 up `Lazy<>` unchanged. `MimicryService` lost the trait stat reads (1,434 -> 1,307 lines) and keeps 11 services.
Where the pull request differs from the map:

- **Part 4 is `MimicryTraitStatsService` behind `IMimicryTraitStatsService`,** a converted service of Character
  (`ConvertedServices.json`, `DomainMap.json`) with the `GameServices` property `MimicryTraitStats`, not a store named
  `MimicryTraitStatStore`. The architecture rules count every type of `Domain.Character` as part of the domain's converted
  services, and their handlers must be on a `<Name>Service` instance class
  (`Event_handlers_of_converted_services_are_on_their_instance_classes`); part 4 has a cache handler. It holds the technique
  and trait stat tables, `GetStatBonus`, `GetStatSources` and `GetSetBonusPotency`, verbatim, with the stat reader as their last
  argument (`statReader`; `StatService` passes `this`). Its handler is a split of master's `Mimicry.CacheData` (renames line 112)
  and stands right after `MimicryService.CacheData` in the manifest.
- **The defense damage types are an `ImmutableArray`.** `DamageTableDefaults.GetDefenseDamageTypes()` returns the array it already
  held, and the `GetDefenseDamageTypes` seam returns `ImmutableArray<CombatDamageType>`: a static `IReadOnlyList` field is state
  the converted-service rule refuses, and boxing the array on each read would allocate. The record rebuild's `foreach` no longer
  boxes the list's enumerator, so it allocates one object less per rebuild; the list and its order are the catalog's (a test).
- **`IEnmityTargetQuery`** declares `GetHighestEnmityTarget`, which left `IEnmityService` (it extends the query).
  `RestoreNPCStats(bool, IEnmityTargetQuery)`: `AIService`'s seam passes `_enmity.Value`, `DroidService` and the beast
  regeneration pass `null` (the query is read only when `outOfCombatRegen` is true). The edges
  `Feature.CivilianPatrol -> Domain/Combat` and `Feature.EchaniTempleMonks -> Domain/Combat` went with it: that call was their
  only Combat member.
- **Seven down and inside `Lazy<>` went.** With `IEnmityService` gone from `StatService`, `LazyDependencyCycleTests` found
  needless: `EnmityService`'s four (`IStatService`, `ICombatActivityService`, `ICombatAttackTimingService`,
  `ICombatQueriesService`) and `CompanionControlService`'s three (`IAbilityActivationService`, `ICombatActivityService`,
  `ICombatQueriesService`); each cycle ran `... -> StatService -> EnmityService -> CompanionControlService`. They are plain
  parameters now (a Lazy is built once and its `Value` read per call, so a call is the same interface call without the read).
  `EnmityService` leaves the `Lazy<>` holders of the holder table (37 types, 10 of them lazy), and the P5-3d note that
  `CompanionControlService` keeps its `Lazy<IAbilityActivationService>` until P5-7 and P5-8 no longer holds: the stat half of its
  cycle was the enmity parameter.
- **Part 8 stays in P5-4d,** as the order table says. In b it would add `IResourceRestoredListener` while
  `Lazy<ICombatResourcesService>` still serves `TryUseStatTrigger` (19 services, not 18).
- **The type moves** are three `DomainOrderTypeMoves.json` entries (`SWLOR.TypeMoves` moved the files, added the usings and
  renamed handler lines 90 and 91, `ScriptHandlerOrder.txt`, `ClockSeamFiles.txt` and the reload snapshots in place).
  `ReloadCarryHistory.txt` joined the tool's `textExcludes`: its keys name what earlier builds carried, so a move must not
  rewrite them. The carried `_modifiers` names its former owner, and the moved service publishes through `IGuiRefreshPublisher`,
  so `Combat <-> UI` is no longer a domain cycle. Its two test files moved to `Tests/Domain/Character`.
- **Tests:** `StatBench` lost the power glove, resistance and enmity stand-ins: the NPC skin read decodes resistances with
  `ResistanceRules` (the test reads 30 and 104, a vulnerability of 4), and the regeneration tests pass `StatBench.Enmity`.
  `MimicryServiceTests` builds the new service over the same ability details and a stat reader that answers Trait Potency. The
  engine tests read the trait stats through a `TraitStatBonus` helper per file, so the stat reader argument adds no static lookup.
- **Baselines:** `ServicesPerType.txt` (`StatService` 22 -> 18), `LargeTypes.txt` (`StatService` 3,348, `MimicryService` 1,307),
  `UpwardEdges.txt` (`StatService.cs` Character -> Combat 40 -> 25 members, Character -> Abilities gone), `LogicToContent.txt`
  (`StatService.cs -> Feature/StatusEffectDefinition` gone), `DomainCycles.txt` (`Combat <-> UI` gone), `StaticLookups.txt`
  (`MimicryTraitPotencyEngineTests.cs` 32 -> 30, the moved allocation test re-keyed), `DeferredResolution.txt` 51 -> 47 lines
  (76 -> 69 parameters: the `EnmityService` and `CompanionControlService` lines; `StatService.cs -> Combat (up) | 5` unchanged);
  `CrossDomainEdges.txt` loses `Domain/Combat -> Domain/UI` and the two Gameplay edges (counts kept). No accepted growth: no
  holder gained a service or a `Lazy<>`.
- **Readings:** CombatSim's team-fight, twin-pistol-barrage, twin-pistol-crossfire and solo-boss reports (seed 1, 50 runs) and its
  ability table are byte-identical to the base's (`10862bf90`). The filtered engine run (`Stat|Mimicry|Temporary|Modifier|Regen|Food`,
  134 tests, the same packed module and haks on both sides) failed 3 on the branch, all failing on the base too (Piercing Toss
  Bleed ticks, Toxin and Shock damage reduction, the Foundry loot migration); the base also failed the companion stamina delay
  test, which passed on the branch.

### P5-4c (the ratings move up to Combat)

Part 5 moved verbatim: `CombatRatingService` behind `ICombatRatingService` (Combat, `GameServices.CombatRating`; 950 lines, 7
services: `IDBService`, `ISkillService`, `IItemService`, `ITwinPistolEquipmentService`, `ICombatQueriesService`,
`ICombatDamageModifiersService`, `IStatService`) holds the scripted and native Attack, Accuracy, Defense and Evasion reads,
`GetStatValueNative`, the attack and defense percent adjustments, the six deflection chance reads, `GetGuardChance`, the caps, the
weapon and shield helpers, the five deflection and Guard constants and its 8 seams. Its 40 calls into part 1 (28 of them
`GetStatAdjustment`, the rest the NPC stat reads and the pools) go through `IStatService`, and its two Combat seams lost their
`Lazy<>.Value`. `StatService` went 18 -> 14 services (`ITwinPistolEquipmentService`, `IItemService`, `Lazy<ICombatQueriesService>`
and `Lazy<ICombatDamageModifiersService>` gone), 3,348 -> 2,430 lines, up `Lazy<>` 5 -> 3; `ApplyPercentAdjustment` is
`StatRules.ApplyPercentAdjustment`, which the pools and the healing adjustments call too. No handler, no `[ReloadState]` field and
no `[ReloadCarriedFrom]` moved: part 5 holds no state. Where the pull request differs from the map or adds to it:

- **A converted service of Combat,** listed on its own in `ConvertedServices.json` and `DomainMap.json`, not as an `extras` part of
  `Stat`'s entry: a part lives in its service's domain.
- **The `Stat` facade is split in `RemovedFacades.json`.** Its `members` list the 19 part 5 names under `CombatRating` and the 89 names
  left on `IStatService` under `Stat`, as the `Ability` entry does: the tool rewrites a call only for a listed name, so without the
  `Stat` list a master file that calls only `Stat` members would not get its `GameServices` import on a sync. The five deflection and
  Guard constants name `CombatRatingService`. `FacadeRemovalDataTests` checks the `Stat` entry against the two interfaces. P5-4d and
  P5-4e move their parts' names out of the `Stat` list in the same way.
- **Callers.** `Apply-FacadeRemoval.ps1` rewrote 131 call sites in 29 files: content (`CharacterSheetViewModel` 18,
  `ShieldBashAbilityDefinition` 1), `Native` (`AttackRollDefense` 4, `DamageRollCreature` 3, `GetDamageRoll` 2, `AttackRollAccuracy`
  1, `DamageRollWeapon` 1), the static helpers `BeastCombatStatCalculator` (4) and `WeaponShotProfile` (1), 12 engine test files (59),
  7 unit test files and the characterization test (5); a second run changes nothing. `CombatDamageTakenService` (`GetGuardChance`,
  `GetAttack`, `GetDefense` twice), `CombatAbilityHitService` (`GetEvasion`, `GetAccuracy`) and `AbilityDamageInputService` (its
  `GetAttack` and `GetDefense` seams) take `ICombatRatingService`. `PlayerGuideViewModel` and the tests read the moved constants from
  `CombatRatingService`; the characterization test keeps its `nameof` names, so its snapshot is unchanged.
- **Accepted growth, recorded here as the holder table says:** `CombatDamageTakenService` 17 -> 18 services (and 1,640 -> 1,643
  lines for the field, parameter and assignment) and `CombatAbilityHitService` 11 -> 12, both removed by P5-7;
  `AbilityDamageInputService` 10 -> 11, a new `ServicesPerType.txt` line, removed by P5-16; `CharacterSheetViewModel` 19 -> 20 lookups
  (`CombatRating` beside `Stat`; P5-4e takes it to 21), removed by P5-14 and P5-16. No holder gained a `Lazy<>`.
- **Tests:** `TestStatService.CombatRating.cs` became `TestCombatRatingService`, the part's double, over the bench's database, item
  and twin pistol stand-ins, with the stat double as its `IStatService`, so the rating tests see the same answers and the same log;
  the bench's and the test base's `CombatRating` accessors return it. `AllocationCombatRatingService` answers the rating seams without
  allocating and is registered beside `AllocationStatService` in `StatFacadeAllocationTests` and `CombatFacadeAllocationTests`; the
  48 pins are unchanged and the rating group reads through `GameServices.CombatRating`. `StatServiceDefaultSeamsTests` runs the
  part's seams on a `CombatRatingService` subclass, and the `CombatAbilityHitService` and `CombatDamageTakenService` tests answer the
  ratings through an `ICombatRatingService` stand-in. `StatSource` adds the part's file, its interface and `ICombatRatingService`;
  the tests that read the call sites by text follow the new receivers.
- **Coverage:** the native reads (167 lines: `GetAttackNative`, `GetAccuracyNative`, `GetEvasionNative`, `GetDefenseNative`,
  `GetStatValueNative` and the native chance and shield helpers) run only in the engine, as they did in `StatService`, so the unit
  suite covers about 57% of the part. Measured against P5-4a's gate coverage, `Domain/Combat` goes from 97.8% to about 96.8% (floor
  95) and `Domain/Character` from 83.7% to about 85.0% (floor 83).
- **Baselines:** `ServicesPerType.txt` 19 -> 20 lines (302 -> 312 services: `StatService` 18 -> 14 and the growth above),
  `LargeTypes.txt` (`StatService` 2,430, `CombatDamageTakenService` 1,643; 65,931 -> 65,016 lines), `DeferredResolution.txt`
  (`StatService.cs -> Combat (up)` 5 -> 3; 69 -> 67 parameters), `UpwardEdges.txt` 166 -> 164 lines and 470 -> 456 members
  (`StatService.cs` Character -> Combat 25 -> 15 and Character -> Items gone; the `Native` rating reads name Combat instead of
  Character: `AttackRollDefense` Character 3 -> 2 and Combat 2 -> 3, `DamageRollWeapon` Combat 2 -> 3, `DamageRollCreature`'s
  Character line gone). The domain-pair report's members both ways grow 5,271 -> 5,294, because the moved code references Character
  from Combat now; no pair or cycle is added. `CrossDomainEdges.txt` and `EdgesIntoCoreAndPlatform.txt` gain and lose no edge
  (counts kept).
- **`CombatParts.OtherServices` names `CombatRatingService`:** it is a `Combat*Service` in `Domain/Combat` but not a part of the
  `Combat` facade, so `CombatSource` and `CombatMembers` leave it out, as they do `CombatPointService` and `CombatRecorderService`.
- **Readings:** CombatSim's team-fight, twin-pistol-barrage, twin-pistol-crossfire and solo-boss reports (seed 1, 50 runs) and its
  ability table are byte-identical to the base's (`5ae2b671d`). The filtered engine run (ratings, deflection, Guard, attack, dual
  wield, Twin Pistol, Power Glove, trap damage and the rotation balance tests, 301 tests, the same module and haks on both sides, run
  side by side) failed 8 on the branch and 11 on the base. Seven fail on both (the crafting station floor, the dual wield shared
  cadence, the Foundry loot migration, Warden Wall Stance, the Single Weapon rating capture, Piercing Toss Bleed ticks, Toxin and
  Shock damage reduction); the branch's eighth, the Power Glove and Katar parity test, timed out under the paired load and passed
  on a rerun of its category (9 of 9); the base also failed the companion stamina delay, the dual wield animation sequence, the
  Hobble refresh and a Twin Pistol timeout, which passed on the branch. The paired `CombatLoad` runs shared the machine with two
  other sessions' live stacks, so both sides ran saturated (9 to 15 fps): players 874 against 871 managed ms/s (base against
  branch) and NPCs 698 against 711 in the first round, players 844 against 854 and NPCs 771 against 768 with the homes swapped.
  Averaged, the sides are within 1% (players 859 against 863, NPCs 735 against 740), less than one side moved between rounds, and
  the stat read's own time stayed at 86 to 89 ms/s in the player fight on both sides.

## Test plan

Each pull request builds the solution; runs `SWLOR.Architecture.Tests` and regenerates the Phase 5 baselines with
`SWLOR_UPDATE_SNAPSHOTS=1`, explaining every moved line; runs `--filter "FullyQualifiedName~Stat"` and the classes it touches,
`LazyDependencyCycleTests`, the converted-service container tests, `ReloadCarryHistoryTests` and the handler snapshot tests;
adds its files to `StatSource`, points the bench accessors and the allocation group at the part and copies the seams it calls
into the part's double; reaches the coverage floor for each new service; and keeps the characterization snapshots unchanged.
Parity is the coordinator's gate.

P5-4c runs a paired `CombatLoad` (FrameProfiler) run, base and branch side by side, then the homes swapped. P5-4c and P5-4d run
CombatSim's team-fight and solo-boss reports (seed 1, 50 runs), which must be byte-identical, and the filtered engine runs for
ratings, deflection, healing, regeneration and walk mode. P5-4e runs the reload checks with a stat-adjusting effect active, so
`OnCarriedStateAdopted` reaches the listener.

## Risks

- **Hot path.** `GetStatAdjustment` keeps its body and its zero pin; the Mimicry read becomes a store call instead of an
  `IMimicryService` call, so a read makes the same number of virtual calls. Part 5 has 40 calls into part 1 that were self calls
  (28 of them `GetStatAdjustment`). Per read: `GetAttackNative` 4 (1 more for Force, 4 more when the high FP and Stamina bonus is
  set, 1 for an NPC); `GetAccuracyNative` 2 (+1 NPC); `GetEvasionNative` 3 (+1 NPC); `GetDefenseNative` 5 (+1 for Physical with a
  shield, +1 NPC); the melee and ranged deflection chances 2 each, the shield chance and `GetGuardChance` 1. Its three Combat
  seams lose their `Lazy<>.Value`. Hence the paired `CombatLoad` run in P5-4c.
- **Reload.** One carried field changes namespace (`[ReloadCarriedFrom]`). The store's tables and the listener list are rebuilt by
  rerun boot handlers before adoption. Run the reload checks in b and e.
- **Handler order.** Eight moved handler lines keep their places (`RefreshOutOfCombatMovementRate` on every player heartbeat,
  `PurgeAllExpired` on the module heartbeat); the two added handlers stand right after the master handler they split.
- **Text readers outside tests.** `SWLOR.CraftSim/Legacy/LegacySource.cs` looks for `Stat.AdjustControl(...)`,
  `Stat.AdjustCraftsmanship(...)` and `Stat.AdjustCPBonus(...)` in content (e). `PlayerMessageAudit.json`'s two `StatService.cs`
  rows stay keyed there (the pools stay). `ClockSeamFiles.txt` gains `CreatureStatService.cs` (e), which reads `Clock.UtcNow`.
- **Master porting.** Master changed `Service/Stat.cs` and `StatService.cs` 46 times since 2026-10-10; from P5-4b each sync ports
  master's edits by the range table. Part 1 has about 50 lines of headroom under 1,000. `feature/space-revamp`'s `SpaceService`
  calls change property name only. Master's `Mimicry.GetStatBonus(creature, stat)` and `Mimicry.GetStatSources(creature, stat)` calls (its engine tests have several) are ported by hand to `GameServices.MimicryTraitStats.GetStatBonus(creature, stat, GameServices.Stat)` and `GetStatSources(...)` with the same added argument, because the stat reader is now an argument; `GetSetBonusPotency` kept its signature and is listed in `RemovedFacades.json`.
