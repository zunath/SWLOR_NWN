# Maintainability Refactor: Stat Split (P5-4 part map)

The plan for splitting `Domain/Character/StatService.cs` (3,358 lines, 22 constructor parameters, 118 `IStatService` members
including 6 script handlers). Status lives in [MaintainabilityRefactorStatus.md](MaintainabilityRefactorStatus.md) (row P5-4).
Targets and rules: [MaintainabilityRefactorPhase5.md](MaintainabilityRefactorPhase5.md) (target 2: at most 10 services per
type and under 1,000 lines; target 3: no method over 150 lines; target 4: no deferred resolution) and
[MaintainabilityRefactorDomainOrder.md](MaintainabilityRefactorDomainOrder.md) (the supply rule and the P5-4 rows). The model is
the [Ability split](MaintainabilityRefactorAbilitySplit.md). This file goes away at cutover.

Line ranges are for `StatService.cs` at `129c7e7bd` (branch `feature/maintainability-refactor`). Member line counts come from a
syntax-tree scan (a member starts at its doc comment). Dependency use counts are the references to each injected field. Server
callers are bound by `tools/Refactor/DomainOrder/EdgeExtract` (referencing members); call sites in tests and tools are counted
by regex on the receivers `Stat`, `_stat`, `stat` and, in the stat test classes, `Service`. If master changes the file first,
map the ranges through the diff.

## What the code is

`StatService` has nine jobs. The stat read is at the bottom: almost every other job calls `GetStatAdjustment`.

| Job | Lines | Size |
|---|---|---:|
| Stat read: `GetStatAdjustment`, sources, metadata, `ScaleEffect`, the equipment action registry | 140-198, 2648-2695, 2992-3017 | 127 |
| NPC stat skins and the native ability score read | 1651-1689, 2561-2573, 2841-2990, 3101-3117 | 217 |
| FP and Stamina pools | 233-598, 2802-2809 | 365 |
| Max HP, food HP, the gear-fed player records, attributes, NPC loading | 600-966, 1137-1465, 3078-3171 | 721 |
| Walk mode and the movement rate | 200-231, 968-1135 | 191 |
| Detection, Stealth, Lockpicking, Control, Craftsmanship | 2023-2133, 3019-3076 | 165 |
| Attack, Accuracy, Defense, Evasion | 1467-2021 (less 1651-1689), 2135-2186, 2575-2646, 2822-2839 | 644 |
| Deflection chances, and what a deflection triggers | 2188-2560 | 360 |
| Healing adjustments; NPC and beast regeneration | 2697-2800; 3173-3290 | 204 |

The 25 `protected virtual` seams (3292-3356) are the test double's hooks; each moves with the code that calls it. One method is
over 150 lines: `ApplyDeflectionEffectsNative` (2251-2417, 167).

### Dependencies

| Dependency | Domain | Uses | Used by | After the split |
|---|---|---:|---|---|
| `IDBService` | Foundation | 35 | pools 12, records 10, combat ratings 8, other ratings 5 | parts 3, 4, 6, 7 |
| `ICharacterRecording` | Character | 17 | pools 8, NPC regeneration 4, deflection reactions 3, healing 2 | parts 3, 9, 10, 11 |
| `IStatusEffectService` | Character | 5 | stat read 2, Control and Craftsmanship 2, food HP 1 | parts 1, 4, 6 |
| `ICompanionLookup` | Character | 4 | movement 2, pools 1, healing 1 | parts 3, 5, 10 |
| `IItemService` | Items | 4 | shield checks (`GetEquippedShield`, both forms) | part 8 (Combat, down) |
| `ISkillService` | Character | 3 | weapon skill (ratings, deflection), skill details | parts 6, 7, 8 |
| `IPowerGloveEquipmentService` | Combat | 3 | `GetEquipmentStatCopies` in three equipment reads | `PowerGloveStatCopies.For` (Foundation, the body it forwards to) |
| `ITemporaryStatModifierService` | Combat | 3 | stat read 2, deflection and healing writes 1 | moves to Character (rule 1) |
| `Lazy<ICombatAttackTimingService>` | Combat | 3 | the three `GrantNext...` deflection riders | part 9 (Combat) |
| `Lazy<ICombatDamageModifiersService>` | Combat | 3 | low HP, low FP and nearby-status attack adjustments | part 7 (Combat) |
| `Lazy<ICombatResourcesService>` | Combat | 3 | FP and Stamina restored effects 2, `TryUseStatTrigger` 1 | part 12 listener; part 9 |
| `IPlayerFeedbackService` | Foundation | 2 | `RestoreFP`, `RestoreStamina` | part 3 |
| `IMimicryService` | Abilities | 2 | trait stat bonus and sources in the stat read | part 13 behind `ILoadoutStatSource` (rule 2) |
| `ITwinPistolEquipmentService` | Combat | 2 | `GetAttackNative` | part 7 (Combat) |
| `IRecastService` | Combat | 2 | deflection recast reduction | part 9 (Combat) |
| `IResistanceService` | Combat | 2 | `DecodeItemPropertyCostTableValue` in the NPC skin reads | `ResistanceRules` moves to Character |
| `IPerkService` | Character | 2 | stat read | part 1 |
| `Lazy<ICombatQueriesService>` | Combat | 2 | weapon damage and accuracy ability types | part 7 (Combat) |
| `IEnmityService` | Combat | 1 | out-of-combat HP regeneration | part 11 (Combat) |
| `IPvpCombatService` | Combat | 1 | healing reduction | part 10 (Combat) |
| `ICombatDamageCatalogService` | Combat | 1 | `GetDefenseDamageTypes` in the record rebuild | a `DamageTableDefaults` list (Foundation) |
| `Lazy<ICombatDamageTakenService>` | Combat | 1 | `TrackDeflection` | part 9 (Combat) |

### Static fields and handlers

- `_outOfCombatMovementCombatState` (`Runtime`) goes to part 5. `_equipmentStatActions` (none; content fills it at module
  load) stays in part 1. `_summedEquipmentRecords` (a readonly table) goes to part 4.
- Seven handler lines in `Core/ScriptHandlerOrder.txt`: `CacheData` (54) stays; `ReapplyFoodHP` (211) goes to part 4;
  `ClearWalkModeBeforeDMPossessionChange` (448, 459), `ApplyPlayerStats` (761), `ForgetOutOfCombatMovementState` (831) and
  `RefreshOutOfCombatMovementRate` (970) go to part 5. Each is renamed in place with a `HandlerRenames.txt` line.
- `TemporaryStatModifierService._modifiers` is `Carried`; its class changes namespace, so it gets
  `[ReloadCarriedFrom("SWLOR.Game.Server.Domain.Combat.TemporaryStatModifierService")]`.

## Parts

Services are `<Domain>/<Name>` behind `I<Name>` and a `GameServices.<Name without Service>` property. "Deps" counts constructor
parameters at the end of the split. Lines are the moved members plus constructor and fields, to about 15%.

| # | Part | Domain | Lines | Deps | Runs / state |
|---|---|---|---:|---:|---|
| 1 | `StatService` (stays, `IStatService`) | Character | 180 | 4 | **`CacheData`**; every stat read; `_equipmentStatActions` |
| 2 | `NpcStatService` | Character | 235 | 0 | per NPC rating and per native roll |
| 3 | `StatPoolService` | Character | 420 | 7 | per resource change |
| 4 | `StatRecordService` | Character | 770 | 5 | **`ReapplyFoodHP`**; per equip and login |
| 5 | `StatMovementService` | Character | 225 | 2 | **4 handlers** (5 lines); `_outOfCombatMovementCombatState` |
| 6 | `StatRatingService` | Character | 200 | 5 | per Detection, Stealth and craft check |
| 7 | `CombatRatingService` | Combat | 700 | 9 | per attack roll |
| 8 | `CombatDeflectionService` | Combat | 185 | 3 | per attack roll |
| 9 | `CombatDeflectionReactionService` | Combat | 285 | 8 | per deflection |
| 10 | healing adjustments into `CombatResourcesService` | Combat | +101 (735) | 9 | per heal |
| 11 | `NpcRegenerationService` | Combat | 135 | 3 | per NPC heartbeat |
| 12 | `ResourceRestoredEffectsService` (from `CombatResourcesService`) | Combat | 50 | 2 | per FP or Stamina restore |
| 13 | `MimicryTraitStatService` (from `MimicryService`) | Abilities | 135 | 1 | per stat read of a Mimicry stat |

Nothing is over 1,000 lines. The deps per part:

1. `IStatusEffectService`, `IPerkService`, `ITemporaryStatModifierService` (now Character), `ILoadoutStatSource`.
2. None.
3. `IDBService`, `ICharacterRecording`, `IPlayerFeedbackService`, `ICompanionLookup`, parts 1 and 2, `IResourceRestoredListener`.
4. `IDBService`, `IStatusEffectService`, parts 1, 2 and 3.
5. `ICompanionLookup`, part 1.
6. `IDBService`, `IStatusEffectService`, `ISkillService`, parts 1 and 2.
7. `IDBService`, `ISkillService`, `ITwinPistolEquipmentService`, `ICombatQueriesService`, `ICombatDamageModifiersService`, parts 1,
   2, 3 and 8.
8. `IItemService`, `ISkillService`, part 1.
9. `ICharacterRecording`, `IRecastService`, `ICombatDamageTakenService`, `ICombatAttackTimingService`, `ICombatResourcesService`,
   `ITemporaryStatModifierService`, parts 1 and 3.
10. `CombatResourcesService` keeps its 6 and adds `IPvpCombatService`, `ICompanionLookup` and part 3.
11. `ICharacterRecording`, `IEnmityService`, part 3.
12. Part 1, `IStatusEffectService`.
13. `IDBService`; the trait tables are pushed by `MimicryService.CacheData`.

### Members by part (line counts)

- **1:** `CacheData` 10, six metadata forwarders 1-2 each, `RegisterEquipmentStatAction` 8, `TryGetEquipmentStatAction` 5,
  `EquipmentStatActionTypes` 4, `ScaleEffect` 15, `GetStatSources` 20, `CopyStatSources` 5, `GetStatAdjustment` 9,
  `GetStatAdjustmentExcludingTemporaryModifiers` 11, `GetAbilityNameShort` 26, four perk and temporary seams 1-2 each.
- **2:** `GetStatValueNative` 39, `GetNPCSkinStat` 13 (made public), `GetNPCStats` 12, `GetNPCSkinStats` 70, `GetNPCStatsNative` 66,
  `GetNPCSkinMaxHP` 17.
- **3:** `GetMaxFP` 36 and 4, `GetCurrentFP` 25, `GetAdjustedRequiredFP` 13, `GetMaxStamina` 34 and 4, `GetCurrentStamina` 25,
  `RestoreFP` 57, `ReduceFP` 41, `IsStaminaRestorationBlocked` 8, `RestoreStamina` 57, `ReduceStamina` 51, `ApplyFPRestoreAdjustment` 8.
- **4:** `ReapplyFoodHP` 13 and 17, `AdjustPlayerMaxHP` 58, `RefreshPlayerMaxHP` 28, `GetEquippedHPBonus` 19,
  `RefreshPlayerEquipmentStats` 32, `GetEquippedStatTotals` 23, `ApplyEquipmentStatTotals` 90, `ApplyCraftStatTotals` 19,
  `AdjustPlayerMaxFP` 20, `AdjustPlayerMaxSTM` 20, `ApplyPlayerStat` 15, `GetPlayerAttributeScore` 5, `RestorePlayerAttributes` 8
  and 9, `AdjustCombatReadiness` 10, `GetCombatReadinessPercent` 20, `RefreshCombatReadinessEquipment` 24,
  `GetEquippedCombatReadiness` 19, the 17 `Adjust...` record writers 10-17 each, `LoadNPCStats` 8 and 13, `SetNPCMaxHitPoints` 53.
- **5:** `ApplyPlayerStats` 10, `ApplyTemporaryPlayerStats` 10, `RestoreWalkMode` 10, `IsWalkModeEnabled` 3,
  `ClearWalkModeBeforeDMPossessionChange` 8, `ToggleWalkMode` 33, `ApplyCreatureMovementRate` 24, `RefreshOutOfCombatMovementRate`
  24, `ForgetOutOfCombatMovementState` 7, `GetMovementSpeedMultiplier` 26, `GetMovementSpeedAdjustment` 25,
  `GetBaseMovementSpeedIncrease` 10.
- **6:** `GetDetection` 36, `CalculateDetectionRating` 10, `ApplyNPCDetectionCap` 7, `GetLockpicking` 27, `GetStealth` 22,
  `CalculateStealthRating` 4, `CalculateControl` 29, `CalculateCraftsmanship` 29.
- **7:** `GetAttack` 57 and 8, `GetAttackNative` 58, `GetAccuracy` 81 and 8, `GetAccuracyNative` 67, `GetDefense` 49,
  `GetDefenseNative` 46, `GetEvasion` 45, 8 and 9, `GetEvasionNative` 52, `GetHighFPAndStaminaAttackAdjustment` 21,
  `GetDefensePercentAdjustment` 20, `ApplyAccuracyItemProperty` 15, and 13 helpers of 2 to 11 lines.
- **8:** six chance reads 8-10 each, `GetGuardChance` 4, three caps 5-7, `GetMainHandSkillTypeNative` 8 (no callers), and ten
  weapon and shield helpers 4-12 each.
- **9:** `ApplyDeflectionEffectsNative` 167, `GetDeflectionStatTypeForSource` 12, three helpers of 6.
- **10:** `ApplyOutgoingAbilityHealingAdjustment` 14, `ApplyHealingReceivedAdjustment` 14, `ApplyHealingReceivedEffects` 13,
  `ApplyHealingReceivedStaminaRestore` 24, `ApplyHealingReceivedAttackBoost` 14, three helpers of 4-7.
- **11:** `RestoreNPCStats` 7 and 53, `RestoreBeastStats` 9, `IsNaturalStaminaRegenerationAvailable` 4,
  `GetBeastNaturalRegenAmount` 8, `CanRestoreBeastStamina` 22.
- `ApplyPercentAdjustment` (10, used by parts 3, 7 and 10) becomes `StatRules.ApplyPercentAdjustment`.

### Notes on the parts

- **Why Combat holds parts 7 to 11.** Every caller of these members is in Combat or above: Combat, Abilities, Companions,
  Creatures, `Native` and content (bound callers, below). Their upward dependencies are Combat services. In Combat those become
  plain inside-Combat parameters, and no interface has to point back up. This is the "move the code up" fix of Appendix C, as the
  Ability split moved the store, geometry and magnitude down.
- **1. The stat read stays.** `GetStatAdjustment` has 739 call sites in `Domain/`, 146 in content and the zero-allocation pin. It
  keeps `IStatService` and `GameServices.Stat`, so 180 of the 442 content and native call sites keep their text.
- **Temporary stat modifiers.** `TemporaryStatModifierService` (389 lines) has one dependency, `IGuiService`, used for one
  `PublishRefreshEvent`. It moves to Character as the store the stat read reads (rule 1), taking Foundation's
  `IGuiRefreshPublisher` instead. Combat and content write and consume it; Character reads it. Its `StatAdjustmentRefreshEvent` content line in `LogicToContent.txt` moves with it.
- **13. Mimicry traits.** `MimicryService.GetStatBonus` and `GetStatSources` are called only by the stat read. They read the
  `MimicryTraitPotencyPercent` stat, so the provider needs the stat read itself. Character owns
  `ILoadoutStatSource` with `GetStatBonus(creature, stat, IStatService stats)` and `GetStatSources(creature, payloadStat, stats)`;
  `StatService` passes `this`, as P5-3b passed the stat service to the impact store. `MimicryTraitStatService` holds the trait
  table, the two reads, the scaling helpers and `GetSetBonusPotency`, and needs only `IDBService`. `MimicryService.CacheData`
  pushes its technique table into it at the same point it builds `_traitStatsByStat` today.
- **12. Restored effects.** The pools announce a restore; `ApplyFPRestoredEffects` and `ApplyStaminaRestoredEffects` (13 lines
  each, only caller `StatService`) leave `CombatResourcesService` for `ResourceRestoredEffectsService`, which implements Character's
  `IResourceRestoredListener`. It needs only the stat read and `IStatusEffectService`, so part 3 injects it plainly (rule 2).
  `CombatResourcesService` itself cannot implement it: it calls the pools.
- **10. Healing joins `CombatResourcesService`.** `ApplyHealingReceivedStaminaRestore` calls `TryUseStatTrigger`, and
  `CombatResourcesService` calls the healing adjustments in its heal methods (6 sites). A separate healing part would be a
  construction cycle with it.
- **9. The deflection reaction split.** `ApplyDeflectionEffectsNative` tracks the deflection, reads every payload (23 values and
  4 stat types), then applies the restores, modifiers and riders. It splits after the reads: the reads, verbatim, fill a readonly struct `DeflectionReaction` through an `out`
  parameter, and the applies read it back into locals of the original names. No tuple, no closure. The order of reads and
  writes does not change.
- **8 and 7.** Part 7 calls part 8 for `HasShieldEquipped` (the shield physical defense bonus). `CombatDamageTakenService` calls
  `GetGuardChance` and the ratings, and part 9 calls `TrackDeflection` on it, so the chances (8) and the reactions (9) are two parts.
- **4. Records hold NPC loading.** `LoadNPCStats` and `SetNPCMaxHitPoints` sit with the player max HP code, so the one content
  reference left (`TemporaryHitPointEffects.ScheduleLimitCheck`) is in one file, and part 2 has no dependency.
- **Small moves down.** `ResistanceRules` (63 lines, Foundation dependencies only) moves to Character for the NPC skin decode.
  `DamageTableDefaults` gains the defense damage type list (enum order, `IsDefenseDamageType`), which is what
  `CombatDamageCatalogService.GetDefenseDamageTypes` returns. The power glove seam calls `PowerGloveStatCopies.For`, the body the
  Combat interface forwards to. `FoodStatusEffect` implements a Character capability `IFoodStatusEffect`, and the food check
  reads `GetStatusEffect<IFoodStatusEffect>` (`CreatureStatusEffect.GetEffect<T>` is `OfType<T>`, so the answer is the same).

## The upward references and how each goes

`UpwardEdges.txt` lists `StatService.cs` with Character -> Combat (40 members), -> Abilities (4) and -> Items (4).
`DeferredResolution.txt` has its 5 up `Lazy<>`. `LogicToContent.txt` has 2 lines.

| Reference | Rule | Fix | PR |
|---|---|---|---|
| `ITemporaryStatModifierService` | 1 | the store moves to Character | b |
| `IMimicryService` (2 reads) | 2 | `ILoadoutStatSource`, part 13, reader as an argument | b |
| `IPowerGloveEquipmentService`, `IResistanceService`, `ICombatDamageCatalogService` | move down | Foundation and Character statics | b |
| `FoodStatusEffect` (content) | capability | `IFoodStatusEffect` in Character | b |
| `ITwinPistolEquipmentService`, `Lazy<ICombatQueriesService>`, `Lazy<ICombatDamageModifiersService>`, `IItemService`, `CombatRules` | move up | parts 7 and 8 in Combat | c |
| `IRecastService`, `Lazy<ICombatAttackTimingService>`, `Lazy<ICombatDamageTakenService>`, `IPvpCombatService`, `IEnmityService`, `PvpCombatRules` | move up | parts 9, 10, 11 in Combat | d |
| `Lazy<ICombatResourcesService>` | 2 | `IResourceRestoredListener`, part 12 | f |
| `TemporaryHitPointEffects` (content static) | - | stays behind its seam for P5-12 (as P5-3 answer 3) | - |

No `Lazy<>`, `Func<>` or late lookup is added. The 11 `Lazy<IStatService>` elsewhere (`PerkService`, `SkillService`,
`StatusEffectService`, `MimicryService`, `EnmityService`, `MeleeTechniqueService`, `ResistanceService`, `StealthService`,
`CombatRecorderService`, `BeastMasteryService`, `AIService`) become plain parameters of the narrow parts wherever
`LazyDependencyCycleTests` shows the cycle gone.

## Callers

Bound referencing members by caller (server): Combat 352 (298 on part 1, 31 pools, 10 healing, 5 combat ratings), content 310,
Abilities 22, `Native` 20, Companions 14, Character 13, World 9, Space 8, Administration 4, Crafting 4, Creatures 4, Social 3,
Items 2. No caller below Combat reads parts 7 to 11.

Call sites by destination (`Domain` includes the parts themselves; `Content` is `Feature/` and `Native/`):

| Destination | Domain | Content | Tests | Engine | Tools |
|---|---:|---:|---:|---:|---:|
| `IStatService` (stays) | 779 | 180 | 284 | 141 | 23 |
| `IStatPoolService` | 43 | 115 | 61 | 247 | 6 |
| `IStatRecordService` | 9 | 73 | 107 | 110 | 43 |
| `INpcStatService` | 17 | 11 | 5 | 9 | 0 |
| `IStatMovementService` | 7 | 4 | 55 | 19 | 0 |
| `IStatRatingService` | 3 | 15 | 47 | 4 | 6 |
| `ICombatRatingService` | 13 | 20 | 93 | 57 | 10 |
| `ICombatDeflectionService` | 1 | 7 | 28 | 2 | 0 |
| `ICombatDeflectionReactionService` | 0 | 1 | 14 | 0 | 0 |
| `ICombatResourcesService` (healing) | 10 | 16 | 33 | 8 | 4 |
| `INpcRegenerationService` | 3 | 0 | 16 | 3 | 4 |

The most moved members: `GetCurrentStamina` (150 engine sites), `SetNPCMaxHitPoints` (86 engine), `GetMaxStamina`, `GetCurrentFP`,
`RestoreStamina`, `GetAccuracy`, `GetAttack`.

**Call text.** 262 content and native sites change only the property name (`Stat.RestoreFP` becomes `StatPool.RestoreFP`).
`RemovedFacades.json` lists the members by owner and `Apply-FacadeRemoval.ps1` rewrites them, and `-Reverse` maps master's text
back for the parity build, as in P5-3. Public constants move with their part; content and the characterization tests that name
`StatService.<constant>` change the type name only (`nameof` gives the same snapshot name).

## Tests (P5-4a)

P5-4a prepares the tests so each later pull request changes the helpers, not the test text:

- **`StatSource`** (`Tests/Support`) is the source of `StatService` wherever it lives: `PartFiles`, `InterfaceFiles`,
  `PartInterfaces`, and `Read`, `ReadInterfaces`, `Method`, `Member`, `Members`, `Nodes`, `Paths`. A call through a field of a
  part interface loses its receiver; nothing else is rewritten. `StatSourceTests` proves the rewrite. The 16 test files that read
  `StatService.cs` or `IStatService.cs` by path now read it, directly or through `StatHarnessSource` (substring searches between
  two members became `Method` reads).
- **`StatHarnessSource`** reads the methods and the constants the Roslyn harnesses compile from `StatSource`, so
  `DMPossessedCombatTargetTests`, `ResourceRestorationTests`, `WalkModeTests`, `CombatReadinessEquipmentTests` and
  `MaxHitPointsReconciliationTests` find a member in any part file.
- **`StatBench`** and `StatServiceTestBase` give one accessor per part (`Service`, `NpcStats`, `Pool`, `Record`, `Movement`,
  `Rating`, `CombatRating`, `Deflection`, `DeflectionReaction`, `Healing`, `Regeneration`). The 363 calls in the 7 stat test
  classes go through the accessor of their member's part. Until a part moves its accessor is the one instance.
- **The seam answers** (`StatBench.Seams.cs`) are properties of the bench, not of the double.
- **`TestStatService`** moved out of `StatBench.cs` into partial files by the part whose code calls each seam
  (`TestStatService.<Part>.cs`; `.Shared.cs` holds the weapon skill, the stat trigger and the temporary modifier). When a part
  moves, its file becomes the part's double.
- **The allocation pins** (`StatFacadeAllocationTests`) are grouped by part, still through `Stat.` (a local property per part would
  add `GameServices` lookups to `StaticLookups.txt`). The same 48 pins.

## Order

The part that stays is the bottom of the graph, so the split runs top-down, unlike the Ability split: a part leaves only when
nothing still in `StatService` calls it, and a moved part calls the members still in `StatService` through `IStatService`. Six
pull requests, serial; the master freeze covers b to f.

| PR | Contents | `StatService` services after |
|---|---|---:|
| P5-4a | This guide; `StatSource`, the harness reads, the bench accessors, the double's partial files, the pins grouped | 22 |
| P5-4b | `TemporaryStatModifierService` and `ResistanceRules` to Character; `ILoadoutStatSource` and part 13; the Foundation lists; `IFoodStatusEffect` | 19 |
| P5-4c | Parts 7 and 8 (the attack-roll reads); `Native` and content call sites | 15 |
| P5-4d | Parts 9 (with its split), 10 and 11 | 10 |
| P5-4e | Parts 4, 5 and 6 (6 handler lines renamed; `CraftSim` needles) | 9 |
| P5-4f | Parts 2 and 3, part 12 and the listener; moved parts switch to the narrow interfaces; every baseline regenerated | 4 |

The last up `Lazy<>` (`ICombatResourcesService`) goes in P5-4f: part 12 needs the stat read, so it can be injected only once the
pools have left `StatService`.

## Test plan

Each pull request:

- builds the solution, runs `SWLOR.Architecture.Tests` and regenerates the Phase 5 baselines with `SWLOR_UPDATE_SNAPSHOTS=1`,
  reviewing that only the expected lines move (`UpwardEdges.txt`, `DeferredResolution.txt`, `ServicesPerType.txt`,
  `LargeTypes.txt`, `LongMethods.txt` in d, `LogicToContent.txt`, `CrossDomainEdges.txt`);
- runs `--filter "FullyQualifiedName~Stat"` and the classes it touches, plus `LazyDependencyCycleTests`, the converted-service
  container tests, `ReloadCarryHistoryTests` and the script handler snapshot tests;
- adds its part files to `StatSource`, points the bench accessors and the allocation group at the part, and copies the seams the
  part calls into its double; `StatServiceDefaultSeamsTests` runs the defaults of each part's seams;
- reaches the coverage floor for each new service, keeps the characterization snapshots unchanged, and leaves parity to the
  coordinator.

P5-4c and P5-4d also run CombatSim's team-fight and solo-boss reports (seed 1, 50 runs), which must stay byte-identical, and the
filtered engine runs for stats, deflection, healing, regeneration and walk mode.

## Risks

- **Hot reads.** `GetStatAdjustment` keeps its body and its pin at zero; the Mimicry read becomes an `ILoadoutStatSource` call
  instead of an `IMimicryService` call, so the number of virtual calls per read does not change. The real temporary read still
  allocates its 32-byte closure. The attack roll reads (`GetAttackNative`, `GetAccuracyNative`, `GetEvasionNative`,
  `GetDefenseNative`, the deflection chances) are one interface call from the hook as today, but call parts 1, 2 and 3 through
  interfaces where they called themselves: about 3 to 6 more virtual calls per rating. A paired `CombatLoad` run checks c.
- **Reload.** One carried field changes namespace (`[ReloadCarriedFrom]`). The trait tables are rebuilt by the Mimicry cache
  rerun. Run the reload checks in b and e.
- **Handler order.** Six handler lines are renamed; the regenerated manifest must differ only by class name.
  `RefreshOutOfCombatMovementRate` runs on every player heartbeat and must keep its place.
- **Text readers outside tests.** `SWLOR.CraftSim/Legacy/LegacySource.cs` looks for `Stat.AdjustControl(...)`,
  `Stat.AdjustCraftsmanship(...)` and `Stat.AdjustCPBonus(...)` in content (P5-4e). `PlayerMessageAudit.json` has two rows keyed by
  `StatService.cs` (`RestoreFP`, `RestoreStamina`) that re-key in P5-4f. `ClockSeamFiles.txt` gains the regeneration (d) and pool
  (f) files, which read `Clock.UtcNow`.
- **Master porting.** Master changed `Service/Stat.cs` and `StatService.cs` 46 times since 2026-10-10. From P5-4b each sync ports
  master's edits by the range table above. `feature/space-revamp` calls `ApplyCreatureMovementRate` and `GetNPCStats` from
  `SpaceService`; those lines change property name only.
- **Construction cycles.** The order above keeps every new parameter plain. If the container test finds a cycle through a service
  outside the split (for example part 11 through `IEnmityService` and `IAIService`), the caller keeps a narrowed existing `Lazy<>`
  and the pull request records it, as P5-3c and P5-3d did.

## Questions

Recommendations first; the coordinator or owner answers before P5-4b.

1. **Move parts 7 to 11 up to Combat instead of inverting them?** Recommended: yes. Their callers are all Combat or above, and
   inverting them would need about ten Character interfaces implemented by Combat services that call the stat read back.
2. **Move `TemporaryStatModifierService` (389 lines) from Combat to Character?** Recommended: yes. It is the store the domain-order
   guide names, Combat only writes it, and it needs nothing above Character.
3. **Mimicry by rule 2 with the reader as an argument, not rule 1?** The domain-order row says the Mimicry bonus is pushed. The
   bonus is scaled by the live `MimicryTraitPotencyPercent` stat, so a pushed value would change when it is computed.
   Recommended: rule 2.
4. **Healing adjustments into `CombatResourcesService`?** The alternative is a stat trigger store split out of
   `CombatResourcesService`, which adds a parameter to six Combat services (20 call sites). Recommended: join.
5. **Leave `TemporaryHitPointEffects.ScheduleLimitCheck` behind its seam for P5-12?** Recommended: yes, as P5-3 answer 3.
