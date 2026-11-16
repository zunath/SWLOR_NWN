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
stat test classes, `Service`, with the tests read at `129c7e7bd`. If master changes the file first, map the ranges through the
diff.

## What the code is

`StatService` has nine jobs. The stat read is at the bottom: almost every other job calls `GetStatAdjustment`.

| Job | Lines | Size |
|---|---|---:|
| Stat read: `GetStatAdjustment`, sources, metadata, `ScaleEffect`, the equipment action registry | 140-198, 2648-2695, 2992-3017 | 127 |
| FP and Stamina pools | 233-598, 2802-2809 | 365 |
| NPC stat skins (and the native ability score read) | 2561-2573, 2841-2990, 3101-3117 (1651-1689) | 178 (39) |
| Max HP and food HP, attributes, NPC loading | 600-738, 1137-1176, 3078-3171 | 266 |
| Gear-fed player records, Combat Readiness, the 17 `Adjust...` writers | 740-966, 1178-1465 | 505 |
| Walk mode and the movement rate | 200-231, 968-1135 | 191 |
| Detection, Stealth, Lockpicking, Control, Craftsmanship | 2023-2133, 3019-3076 | 165 |
| Attack, Accuracy, Defense, Evasion; deflection and Guard chances | 1467-2021, 2135-2249, 2426-2646, 2822-2839 | 795 |
| Deflection reactions; healing adjustments; NPC and beast regeneration | 2251-2466; 2697-2800; 3173-3290 | 413 |

The 25 `protected virtual` seams (3292-3356) are the test double's hooks; each moves with its caller. One method is over 150
lines: `ApplyDeflectionEffectsNative` (2251-2417, 167).

### Dependencies

| Dependency | Domain | Uses | Used by | After the split |
|---|---|---:|---|---|
| `IDBService` | Foundation | 35 | pools 12, records 10, ratings 8, other ratings 5 | parts 1, 2, 3, 5 |
| `ICharacterRecording` | Character | 17 | pools 8, regeneration 4, deflection reactions 3, healing 2 | parts 1, 2, 6, 7 |
| `IStatusEffectService` | Character | 5 | stat read 2, Control and Craftsmanship 2, food HP 1 | parts 1, 2, 3 |
| `ICompanionLookup` | Character | 4 | movement 2, pools 1, healing 1 | parts 1, 2, 7 |
| `IItemService` | Items | 4 | shield checks | part 5 (Combat, down) |
| `ISkillService` | Character | 3 | weapon skill (ratings, deflection), skill details | parts 3, 5 |
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
- `Core/ScriptHandlerOrder.txt` lines: `CacheData` (54) stays. `ReapplyFoodHP` (211), `ClearWalkModeBeforeDMPossessionChange` (448,
  459), `ApplyPlayerStats` (761), `ForgetOutOfCombatMovementState` (831) and `RefreshOutOfCombatMovementRate` (970) go to part 2,
  each renamed in place with a `HandlerRenames.txt` line.
- `TemporaryStatModifierService` moves namespace with its handlers `ClearExitingObject` (840, `OnModuleExit`) and
  `PurgeAllExpired` (864, `OnModuleHeartbeat`): two `HandlerRenames.txt` lines, positions unchanged. Its `Carried` field
  `_modifiers` gets `[ReloadCarriedFrom("SWLOR.Game.Server.Domain.Combat.TemporaryStatModifierService")]`.
- Part 4 builds its tables in its own `OnModuleCacheAfter` handler next to `MimicryService.CacheData`: one
  `HandlerRenames.txt` split line (master's `MimicryService.CacheData` -> both handlers, in order).

## Parts

Services are `<Domain>/<Name>` behind `I<Name>` and a `GameServices` property (`CreatureStat`, `StatRecord`,
`MimicryTraitStats`, `CombatRating`, `CombatDeflectionReaction`). "Deps" counts constructor parameters at the end. Lines are the
moved members plus constructor and fields, to about 15%.

| # | Part | Domain | Lines | Deps | Runs / state |
|---|---|---|---:|---:|---|
| 1 | `StatService` (stays) | Character | 845 | 9 | **`CacheData`**; every stat read and resource change |
| 2 | `CreatureStatService` | Character | 600 | 5 | **5 handlers** (6 lines); `_outOfCombatMovementCombatState` |
| 3 | `StatRecordService` | Character | 625 | 4 | per equip and login |
| 4 | `MimicryTraitStatStore` (`IMimicryTraitStats`) | Character | 150 | 2 | **cache handler**; per read of a trait stat |
| 5 | `CombatRatingService` | Combat | 880 | 7 | per attack roll |
| 6 | `CombatDeflectionReactionService` | Combat | 285 | 7 | per deflection |
| 7 | healing adjustments into `CombatResourcesService` | Combat | +101 (735) | 8 | per heal |
| 8 | `ResourceRestoredEffectsService` (from `CombatResourcesService`) | Combat | 45 | 1 | per FP or Stamina restore |

The deps:

1. `IStatusEffectService`, `IPerkService`, `ITemporaryStatModifierService` (now Character), part 4, `IDBService`,
   `ICharacterRecording`, `IPlayerFeedbackService`, `ICompanionLookup`, `IResourceRestoredListener`.
2. `IDBService`, `ICharacterRecording`, `ICompanionLookup`, `IStatusEffectService`, part 1.
3. `IDBService`, `IStatusEffectService`, `ISkillService`, part 1.
4. `IDBService`, `IAbilityCatalog`.
5. `IDBService`, `ISkillService`, `IItemService`, `ITwinPistolEquipmentService`, `ICombatQueriesService`,
   `ICombatDamageModifiersService`, part 1.
6. `ICharacterRecording`, `IRecastService`, `ICombatDamageTakenService`, `ICombatAttackTimingService`, `ICombatResourcesService`,
   `ITemporaryStatModifierService`, part 1.
7. `CombatResourcesService`'s 6, plus `IPvpCombatService` and `ICompanionLookup`.
8. `IStatusEffectService` (the stat read comes in as an argument).

### Members by part (line counts)

- **1:** `CacheData` 10, six metadata forwarders 1-2, `RegisterEquipmentStatAction` 8, `TryGetEquipmentStatAction` 5,
  `EquipmentStatActionTypes` 4, `ScaleEffect` 15, `GetStatSources` 20, `CopyStatSources` 5, `GetStatAdjustment` 9,
  `GetStatAdjustmentExcludingTemporaryModifiers` 11, `GetAbilityNameShort` 26; `GetMaxFP` 36 and 4, `GetCurrentFP` 25,
  `GetAdjustedRequiredFP` 13, `GetMaxStamina` 34 and 4, `GetCurrentStamina` 25, `RestoreFP` 57, `ReduceFP` 41,
  `IsStaminaRestorationBlocked` 8, `RestoreStamina` 57, `ReduceStamina` 51, `ApplyFPRestoreAdjustment` 8; `GetNPCStats` 12,
  `GetNPCSkinStats` 70, `GetNPCStatsNative` 66, `GetNPCSkinStat` 13, `GetNPCSkinMaxHP` 17; `GetDetection` 36,
  `CalculateDetectionRating` 10, `ApplyNPCDetectionCap` 7, `GetStealth` 22, `CalculateStealthRating` 4.
- **2:** `ApplyPlayerStats` 10, `ApplyTemporaryPlayerStats` 10, `RestoreWalkMode` 10, `IsWalkModeEnabled` 3,
  `ClearWalkModeBeforeDMPossessionChange` 8, `ToggleWalkMode` 33, `ApplyCreatureMovementRate` 24,
  `RefreshOutOfCombatMovementRate` 24, `ForgetOutOfCombatMovementState` 7, `GetMovementSpeedMultiplier` 26,
  `GetMovementSpeedAdjustment` 25, `GetBaseMovementSpeedIncrease` 10; `ReapplyFoodHP` 13 and 17, `AdjustPlayerMaxHP` 58,
  `RefreshPlayerMaxHP` 28, `GetEquippedHPBonus` 19, `ApplyPlayerStat` 15, `GetPlayerAttributeScore` 5, `RestorePlayerAttributes` 8
  and 9; `LoadNPCStats` 8 and 13, `SetNPCMaxHitPoints` 53; `RestoreNPCStats` 7 and 53, `RestoreBeastStats` 9,
  `IsNaturalStaminaRegenerationAvailable` 4, `GetBeastNaturalRegenAmount` 8, `CanRestoreBeastStamina` 22.
- **3:** `RefreshPlayerEquipmentStats` 32, `GetEquippedStatTotals` 23, `ApplyEquipmentStatTotals` 90, `ApplyCraftStatTotals` 19,
  `AdjustPlayerMaxFP` 20, `AdjustPlayerMaxSTM` 20, `AdjustCombatReadiness` 10, `GetCombatReadinessPercent` 20,
  `RefreshCombatReadinessEquipment` 24, `GetEquippedCombatReadiness` 19, the 17 `Adjust...` writers 10-17 each, `GetLockpicking`
  27, `CalculateControl` 29, `CalculateCraftsmanship` 29.
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

- **1 stays wide on purpose.** The split is cut by who calls what. Part 1 keeps every member that the holders of `IStatService`
  read together with `GetStatAdjustment` (the pools, the NPC skin reads, Detection and Stealth), so most holders keep one
  parameter (Holders, below). It keeps `IStatService`, `GameServices.Stat` and the zero-allocation pin.
- **2. Movement, HP, attributes, NPC loading and regeneration** are what `AIService`, `DroidService`, `SpawnService` and
  `StatusEffectService` call, so they are one part. The out-of-combat HP block of `RestoreNPCStats` (3253-3263) reads
  `IEnmityService.GetHighestEnmityTarget`. Only `AIService.cs:132` passes `outOfCombatRegen: true` (`DroidService.cs:1146` and the
  beast path pass false). The block cannot move into `AIService`: it runs inside the `Regeneration` attribution scope (3221), and
  `CombatRecorderService.RecordHealingApplied` resolves the heal's source from that scope, so outside it the recorded source
  would change. Instead Character owns `IEnmityTargetQuery` (`GetHighestEnmityTarget`), `IEnmityService` extends it, and
  `RestoreNPCStats` takes it as an argument: `AIService` passes its own `_enmity.Value`, the others pass `null` with `false`. The
  block stays where it is, nothing is injected, and part 2 has no Combat dependency.
- **2. `StatusEffectService`'s two notices.** It calls `ReapplyFoodHP` after restoring a player's effects (329) and
  `ApplyCreatureMovementRate` when a stat-adjusting effect is added, removed or resumed (362, 910, 1291, 2237). Part 2 needs
  `IStatusEffectService` for the food check, so injecting part 2 back would be a cycle. Character owns
  `IStatEffectListener` (`OnStatusEffectsRestored`, `OnStatAdjustingEffectsChanged`); part 2 registers with
  `StatusEffectService` in a boot handler (rule 3: notices only), and `StatusEffectService` calls the list at the same five
  points. Its `Lazy<IStatService>` then serves only `GetStatAdjustment` (3 sites). P5-5 folds the listener into its effect
  lifecycle events.
- **2 and the food check.** `FoodStatusEffect` implements a Character capability `IFoodStatusEffect : IStatusEffect`, and the
  check reads `GetStatusEffect<IFoodStatusEffect>`. `GetStatusEffect<T>` is constrained to `class, IStatusEffect`
  (`StatusEffectService.cs:2426`) and `CreatureStatusEffect.GetEffect<T>` is `OfType<T>`, so the answer is the same.
- **4. Mimicry traits, a Character store (rule 1).** As the approved rows say (domain order 361, 365, 385, 491-492; Ability split
  245). `MimicryService.GetStatBonus` and `GetStatSources` are called only by the stat read. The store holds the trait tables
  (`_traitStatsByStat` and the technique details it needs), `GetStatBonus`, `GetStatSources` and `GetSetBonusPotency`, and reads
  `Player.EquippedTechniques` and `AbilityDetail`, both at or below Character. It evaluates on read, with the live
  `MimicryTraitPotencyPercent`: its reads take the stat reader as an argument (`StatService` passes `this`), so the order of
  evaluation is unchanged. `ScaleTraitBonus` becomes `MimicryTraitRules.ScaleTraitBonus` (Character), which the store and
  `MimicryService.GetResistanceBonus` (297) both call. `MimicryService` pushing its tables would add the store to its
  constructor (11 -> 12, `ServicesPerType.txt`), so the store builds the same tables from `IAbilityCatalog` in its own cache
  handler. `MimicryService` keeps `_techniques` for its other reads and stays at 11.
- **Temporary stat modifiers.** `TemporaryStatModifierService` (389 lines) has one dependency, `IGuiService`, for one
  `PublishRefreshEvent`; it moves to Character with Foundation's `IGuiRefreshPublisher`. Combat and content write and consume it;
  the stat read reads it (rule 1, the domain-order example). Its `StatAdjustmentRefreshEvent` content line moves with it.
- **5. Ratings and deflection chances are one part.** `CombatDamageTakenService` reads `GetAttack`, `GetDefense` and
  `GetGuardChance`; one part keeps it at one new parameter. Part 6 calls `TrackDeflection` on it, so the reactions are separate.
- **6. The deflection reaction split.** `ApplyDeflectionEffectsNative` tracks the deflection, reads every payload (23 values and 4
  stat types), then applies. The reads, verbatim, fill a readonly struct `DeflectionReaction` through an `out` parameter, and the
  applies read it into locals of the original names: no tuple, no closure, the order of reads and writes unchanged.
- **7. Healing joins `CombatResourcesService`.** `ApplyHealingReceivedStaminaRestore` calls `TryUseStatTrigger`, and
  `CombatResourcesService` calls the healing adjustments (6 sites), so a separate part would be a construction cycle.
- **8. Restored effects.** `ApplyFPRestoredEffects` and `ApplyStaminaRestoredEffects` (13 lines each, only caller the pools) leave
  `CombatResourcesService` for part 8, which implements Character's `IResourceRestoredListener`. Its methods take the stat reader
  as an argument, so it needs only `IStatusEffectService`, which part 1 already takes: plain injection, no cycle (rule 2).
- **Small moves down.** `ResistanceRules` (63 lines, Foundation dependencies only) moves to Character. `DamageTableDefaults` gains
  the defense damage type list (enum order, `IsDefenseDamageType`), which is what `GetDefenseDamageTypes` returns. The power glove
  seam calls `PowerGloveStatCopies.For`. `TemporaryHitPointEffects.ScheduleLimitCheck` (content) stays behind its seam in part 2
  for P5-12, as P5-3 answer 3 did.

### Parts 5 to 7 in Combat, or a Character store (owner decision)

**Move up (recommended).** Every caller of these members is Combat or above (Combat, Abilities, Companions, `Native`, content),
and their upward dependencies are Combat services: twin pistol, queries, damage modifiers, item, recast, attack timing, damage
taken, resources, PvP. In Combat those become plain parameters, and no interface points back up.

**Rule 1 instead.** Keep them in Character and have each Combat service push what they read into Character-owned stores (pair
state, weapon ability types, the three attack adjustments, recast and attack-timing writes, stat-trigger cooldowns, PvP healing
reduction), or answer through Character interfaces with the stat reader as an argument. That is about ten stores or interfaces,
and writes on every change they depend on (current HP for the low-HP adjustment). The ratings are still a separate service from
part 1 (part 1 plus 880 lines is over 1,000), so the holders below grow the same way.

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
| `TemporaryHitPointEffects` (content static) | stays behind its seam (P5-12) | - |

No `Lazy<>`, `Func<>` or late lookup is added.

## Holders of `IStatService`

48 types take `IStatService` in a constructor (47 services and one content type); 11 as `Lazy<>`. Rule for every pull request: no holder grows
`DeferredResolution.txt` or `ServicesPerType.txt`. Counts are services (constructor plus lookups) at `129c7e7bd`.

| Holders | Parts they use | Outcome |
|---|---|---|
| 35 types: every other holder, among them the `Lazy<>` holders `PerkService`, `SkillService`, `MimicryService`, `ResistanceService`, `StealthService`, `EnmityService`, `MeleeTechniqueService` and `CombatRecorderService` | 1 | unchanged; `Lazy<>` kinds and counts unchanged |
| `StatusEffectService` (16, 1 inside `Lazy<>`) | 1, 2 | 1 only, through `IStatEffectListener` (e); unchanged |
| `AIService` (15, `Lazy<IStatService>`) | 2 | `Lazy<ICreatureStatService>` (e), same counts; passes `_enmity.Value` (b) |
| `DroidService` (14), `SpawnService` | 2 | plain `ICreatureStatService` (e), same counts |
| `CombatAbilityMagnitudeService`, `SlicingSessionService` | 3 | plain `IStatRecordService` (e), same counts |
| `CombatResourcesService` (6) | 1, 7 | absorbs part 7: 6 -> 8 (d), not listed |
| `CharacterLoginRestoration` (content, 4 constructor services) | 2, 3 | 4 -> 5 (e), not listed |
| `CombatDamageTakenService` (17), `CombatAbilityHitService` (11), `AbilityDamageInputService` (10) | 1, 5 | **+1 each (c)** |
| `BeastMasteryService` (13, 3 down `Lazy<>`) | 1, 2 | **+1 (e)**, and +1 `Lazy<>` if part 2 cannot be plain |
| `SpaceService` (16) | 1, 2 | **+1 (e)** |
| `CharacterSheetViewModel` (content, 19, lookups) | 1, 2, 3, 5 | **+3** across c and e |

The bold rows break the rule; see Open problem.

## Callers

Bound referencing members by caller: Combat 352, content 310, Abilities 22, `Native` 20, Companions 14, Character 13, World 9,
Space 8, Administration 4, Crafting 4, Creatures 4, Social 3, Items 2. No caller below Combat reads parts 5 to 7.

| Destination | Domain | Content | Tests | Engine | Tools |
|---|---:|---:|---:|---:|---:|
| `IStatService` (stays) | 841 | 305 | 383 | 399 | 35 |
| `ICreatureStatService` | 18 | 15 | 106 | 120 | 4 |
| `IStatRecordService` | 2 | 75 | 86 | 14 | 43 |
| `ICombatRatingService` | 14 | 30 | 121 | 59 | 10 |
| `ICombatDeflectionReactionService` | 0 | 1 | 14 | 0 | 0 |
| `ICombatResourcesService` (healing) | 10 | 16 | 33 | 8 | 4 |

**Call text.** 137 of the 442 content and native sites change the property name only (`Stat.SetNPCMaxHitPoints` becomes
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
| P5-4b | Part 4 and `MimicryTraitRules` (1 split handler line); `TemporaryStatModifierService` (2 handler renames) and `ResistanceRules` to Character; the Foundation lists; `IFoodStatusEffect`; `IEnmityTargetQuery` | 18 | 5 |
| P5-4c | Part 5; `Native` and content call sites | 14 | 3 |
| P5-4d | Parts 6 (with its split), 7 and 8 | 10 | 0 |
| P5-4e | Parts 2 and 3 (6 handler renames, 1 added registration handler; `IStatEffectListener`; `CraftSim` needles); every baseline regenerated | 9 | 0 |

## Test plan

Each pull request builds the solution; runs `SWLOR.Architecture.Tests` and regenerates the Phase 5 baselines with
`SWLOR_UPDATE_SNAPSHOTS=1`, explaining every moved line; runs `--filter "FullyQualifiedName~Stat"` and the classes it touches,
`LazyDependencyCycleTests`, the converted-service container tests, `ReloadCarryHistoryTests` and the handler snapshot tests;
adds its files to `StatSource`, points the bench accessors and the allocation group at the part and copies the seams it calls
into the part's double; reaches the coverage floor for each new service; and keeps the characterization snapshots unchanged.
Parity is the coordinator's gate.

P5-4c runs a paired `CombatLoad` (FrameProfiler) run, base and branch side by side, then the homes swapped. P5-4c and P5-4d run
CombatSim's team-fight and solo-boss reports (seed 1, 50 runs), which must be byte-identical, and the filtered engine runs for
ratings, deflection, healing, regeneration and walk mode.

## Risks

- **Hot path.** `GetStatAdjustment` keeps its body and its zero pin; the Mimicry read becomes a store call instead of an
  `IMimicryService` call, so a read makes the same number of virtual calls. Part 5 has 40 calls into part 1 that were self calls
  (28 of them `GetStatAdjustment`). Per read: `GetAttackNative` 4 (1 more for Force, 4 more when the high FP and Stamina bonus is
  set, 1 for an NPC); `GetAccuracyNative` 2 (+1 NPC); `GetEvasionNative` 3 (+1 NPC); `GetDefenseNative` 5 (+1 for Physical with a
  shield, +1 NPC); the melee and ranged deflection chances 2 each, the shield chance and `GetGuardChance` 1. Its three Combat seams lose their
  `Lazy<>.Value`. Hence the paired `CombatLoad` run in P5-4c.
- **Reload.** One carried field changes namespace (`[ReloadCarriedFrom]`). The store's tables and the listener registration are
  rebuilt by boot handlers that rerun on reload. Run the reload checks in b and e.
- **Handler order.** Ten handler lines: six move to part 2, two move with `TemporaryStatModifierService`, and two are added (the
  store's cache handler, as a split of `MimicryService.CacheData`, and part 2's listener registration, which has no master
  counterpart; check that the parity handler map accepts a branch-only boot handler before P5-4e). The regenerated manifest must
  differ only by class name and those two. `RefreshOutOfCombatMovementRate` (every player heartbeat) and `PurgeAllExpired` (module heartbeat) keep
  their places.
- **Text readers outside tests.** `SWLOR.CraftSim/Legacy/LegacySource.cs` looks for `Stat.AdjustControl(...)`,
  `Stat.AdjustCraftsmanship(...)` and `Stat.AdjustCPBonus(...)` in content (e). `PlayerMessageAudit.json`'s two `StatService.cs`
  rows stay keyed there (the pools stay). `ClockSeamFiles.txt` gains `CreatureStatService.cs` (e), which reads `Clock.UtcNow`.
- **Master porting.** Master changed `Service/Stat.cs` and `StatService.cs` 46 times since 2026-10-10; from P5-4b each sync ports
  master's edits by the range table. `feature/space-revamp`'s `SpaceService` calls change property name only.

## Open problem

The holder rule cannot hold for every holder with any split under 1,000 lines. The ratings must leave part 1 (upward
dependencies, and part 1 plus 880 lines is over 1,000), so the three Combat types that read both `GetStatAdjustment` and a rating
gain one parameter whichever side holds the ratings; `AbilityDamageInputService` reaches 11 and enters `ServicesPerType.txt`.
`BeastMasteryService` and `SpaceService` read part 1 and `ApplyCreatureMovementRate` (part 2); putting movement, NPC loading and
regeneration into part 1 as well would take it over 1,000 lines. `CharacterSheetViewModel` reads four parts through lookups. The
options are to accept these six recorded growths (P5-7 splits the two Combat types, P5-10 `BeastMasteryService`, P5-6
`SpaceService`, P5-14 and P5-16 the view model), or to find a notice or a moved call for each before P5-4c. The coordinator
decides.
