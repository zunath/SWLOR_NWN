# Maintainability Refactor: Combat Split (P5-7 part map)

The plan for splitting the Combat services that are over the Phase 5 limits: `CombatHitEffectsService`, `CombatDamageTakenService`,
`CombatAbilityImpactService`, `CombatRecorderService` (with `CombatRecordSerializer.Write`), `CombatDamageModifiersService`,
`CombatAbilityActivationService`, `CombatAbilityBonusService`, `CombatAbilityHitService`, `CombatService`, `EnmityService` and
`DeathService`. Status lives in [MaintainabilityRefactorStatus.md](MaintainabilityRefactorStatus.md) (row P5-7). Targets and rules:
[MaintainabilityRefactorPhase5.md](MaintainabilityRefactorPhase5.md) (target 2: at most 10 services per type and under 1,000 lines;
target 3: no method over 150 lines; target 4: no deferred resolution) and
[MaintainabilityRefactorDomainOrder.md](MaintainabilityRefactorDomainOrder.md). The model is the
[StatusEffect split](MaintainabilityRefactorStatusEffectSplit.md), with the [Stat split](MaintainabilityRefactorStatSplit.md), the
[Ability split](MaintainabilityRefactorAbilitySplit.md) and the [Creatures split](MaintainabilityRefactorCreaturesSplit.md) (D1, D3 and the
P5-8e ports) beside it. This file goes away at cutover.

This file replaces the P2 plan for converting the static `Combat` class (item P2-D20, 2026-11-07, git `33cf7825f`). That conversion is
done and its facade is gone. Code comments and old status rows that link here for "the Combat split" mean that conversion; its text is
in git history.

Measured at `c2a30aae0` (`feature/maintainability-refactor`). Line counts are `wc -l` of the files; member line counts come from a scan
of member declarations (a member starts at its doc comment and attributes). A part's size adds the usings, class header, constructor and
promoted members to its member lines, so part sizes and service counts are estimates (about 10%); each PR re-measures. Service counts come
from a scan of which injected fields each moved member uses, plus the parts it calls. Call counts are regex counts of the receiver names
(`_damageTaken.X`, `CombatDamageTaken.X` and the like) over server, content, tests and engine tests, so they are approximate. Master facts
come from the local `origin/master` ref (`ca97d5ddc`, 2026-11-17); no `git fetch` was run, so master may have moved.

## Decisions

Proposed here for the coordinator's review. Change only on a finding in review.

- **Split by what each job does, layered by who calls what, with a stay-part per service.** The stay-part keeps the original class and
  interface name and the most-called members, so the fewest callers change. The other parts are new services behind new interfaces.
  Reject chunks: every part below is one responsibility (the effects of a critical hit, the guard memory, the retaliation damage).
- **No part is over a limit.** Every part is at or under 10 services and 1,000 lines, and every method is at or under 150 lines (the one
  method over, `CombatRecordSerializer.Write`, is cut). No exception is asked for. Three parts sit at 10 services and the recorder's
  stay-part sits near 88% of the line limit; they are named in "Risks".
- **Moved code stays verbatim** except for how it reaches its state and dependencies. The text changes are listed per part: the orchestrator
  cuts (below), promoted private members, and the receivers of calls into other parts.
- **Pure code moves down, queries a lower domain cannot name use ports Combat owns, notices use listeners.** Combat's ports already
  exist where P5-3c and P5-15a made them (`IPartyRoster`, `IAreaLookup`, `ICityUpgradeLookup`). This split adds two one-member ports for
  the remaining upward edges (below) and the listener lists in "Cycles".
- **No `Lazy<>`, `Func<>` or late lookup is added.** Three `Lazy<>` parameters inside Combat go (DamageModifiers, DamageTaken, HitEffects).
- **Orchestrators call their steps in the original order.** Where one method runs a fixed sequence of effects that now live in several
  parts (`ApplyDamageDealtEffects`, `ApplyAbilityActivatedEffects`, `ApplySuccessfulAbilityImpactRiders`), the stay-part keeps the method
  and the order; each run of consecutive steps that belong to one part becomes one public method on that part (the body is the original
  statements). Random draws and recorder events keep their order. A characterization test per orchestrator pins it (Tests).
- **Carried keys stay where they can.** `EnmityService` keeps its three carried tables under the same key. Nothing else here is carried.
- **Content references are not removed here.** They move with the code and stay behind their existing seams; P5-2 removes them (the
  `LogicToContent.txt` lines are counted in "What the code is"). The "effect keys for Combat" of the StatusEffect guide (settled
  decision 3) are therefore P5-2's, not this item's: P5-7 moves the `typeof(...StatusEffect)` text unchanged.
- **The accepted service growth from P5-5c to P5-5h is removed here.** `CombatHitEffectsService` 20 -> 8, `CombatDamageTakenService` 20 -> 7,
  `CombatAbilityImpactService` 16 -> 9, `CombatAbilityActivationService` 14 -> 7 and `DeathService` 10 -> 9 (its guard count; 11
  constructor parameters with the listener list).

## What the code is

Eleven files, 12,729 lines (13,026 with the serializer), 5 `Lazy<>` parameters, 22 script handler lines (15 of them move), 3 carried fields,
no static state (the only `static` members are constants and `DeathService.IsWalkable`).

| Service | Lines | Services | Injected (distinct services) | `Lazy<>` | Handlers (`ScriptHandlerOrder.txt`) | Longest methods |
|---|---:|---:|---|---|---|---|
| `CombatHitEffectsService` | 1,716 | 20 | queries, resources, attackTiming, suppression, combatActivity, damageModifiers, abilityHit, stat, statusEffect, statusEffectApplication, statusEffectCombatEvents, statusEffectDuration, temporaryStatModifier, impactState, pairedRangedTraits, combatRecorder, capstoneProof, resistance, party | damageTaken | none | `ApplyCriticalHitEffects` 124, `ApplyAutoAttackDamageModifiers` 80, `ApplyNonCriticalAbilityEffects` 79 |
| `CombatDamageTakenService` | 1,649 | 20 | feedback, queries, resources, attackTiming, combatActivity, damageModifiers, abilityHit, hitEffects, stat, combatRating, statusEffect, statusEffectApplication, statusEffectCombatEvents, temporaryStatModifier, combatRecorder, pvpCombat, enmity, messaging, playerName | combat | `ApplyDamageTakenEffects` (`OnCreatureDamagedAfter`, line 344) | `ApplyDamageTakenModifiers` 72, `TryPreventFatalDamage...` 63, `ApplyIncomingPhysicalToForceConversion` 59 |
| `CombatAbilityImpactService` | 1,720 | 16 | queries, resources, attackTiming, combatActivity, hitEffects, damageTaken, stat, statusEffect, statusEffectApplication, statusEffectDuration, temporaryStatModifier, impactState, enmity, party, combatRecorder, pairedRangedTraits | none | `RegisterStatusEffectListener` (boot, `OnModuleCacheBefore`, line 58) | `GetAbilityImpactBaseDamageBonus` 61, `ApplyAreaAbilityImpactEffects` 63 |
| `CombatRecorderService` | 1,691 | 6 | dmPresence, db, catalog, impactState | stat, queries | `StopOnShutdown` (149), `RecordCreatureDeath` (369), `RecordPlayerDeath` (713) | `Start` 75, `ExpectDeferredDamage` 50 |
| `CombatDamageModifiersService` | 1,436 | 9 | queries, resources, stat, statusEffect, statusEffectApplication, temporaryStatModifier, impactState, playerFeedback | damageTaken | none | `ApplyTargetStatusDamageModifiers` 138, `ApplyDamageDealtModifiers` 90 |
| `CombatAbilityActivationService` | 1,203 | 14 | queries, resources, attackTiming, combatActivity, damageTaken, stat, statusEffect, statusEffectApplication, temporaryStatModifier, impactState, perk, recast, enmity, combatRecorder | none | none | `ApplyAbilityActivatedEffects` 46 |
| `CombatAbilityBonusService` | 1,061 | 9 | queries, resources, combatActivity, attackTiming, stat, statusEffect, temporaryStatModifier, enmity, db | none | none | `ConsumeNextSkillAbilityBonuses` 50 |
| `CombatAbilityHitService` | 426 | 12 | queries, resources, suppression, combatActivity, damageModifiers, stat, combatRating, statusEffect, temporaryStatModifier, impactState, perk, pairedRangedTraits | none | none | `TryResolveAbilityHit` 59 |
| `CombatService` | 198 | 12 | resources, suppression, combatActivity, attackTiming, damageModifiers, hitEffects, damageTaken, abilityActivation, abilityImpact, stat, statusEffect, temporaryStatModifier | none | `ApplyDefeatedEnemyEffects` (368), `ClearCombatState` (600), `ClearStatTriggerState` (816) | `RefreshStatDrivenTrackerEffects` 48 |
| `EnmityService` | 1,244 | 12 | ai, activity, party, holoCom, companions, stat, stealth, companionControl, statusEffect, combatActivity, attackTiming, queries | none | ten lines: `CreatureDamaged`, `HealingStarted`, `HealingCompleted`, `CreatureAttacked`, `CreatureDeath`, `CreatureDestroyed`, `PlayerDeath`, `PlayerExit` (twice, `OnModuleExit` and `OnAreaExit`), `CreatureLimbo` | `ModifyEnmity` 73, `IssueAttackCommand` 46 |
| `DeathService` | 385 | 10 (11 parameters) | db, namedAnimation, messaging, playerName, property (`ICityUpgradeLookup`), area (`IAreaLookup`), statusEffect, statusEffectApplication, combatRecorder, damageTaken, plus the revived-listener list | none | `OnPlayerDying`, `OnPlayerDeath`, `OnPlayerRespawn`, `InitializeRespawnPoint` (`LoginOnly`) | `OnPlayerDeath` 53 |
| `CombatRecordSerializer` | 297 | - | none (static) | - | none | `Write` **198** (the one method over 150) |

Every other method in these files is at or under 138 lines (`LongMethods.txt` lists only `Write`).

**State.** No static fields.
- **Carried:** `EnmityService._enemyEnmityTables`, `_creatureToEnemies`, `_proximityEnmityAmounts` (`ReloadAdoption.txt`, `ReloadCarryHistory.txt`).
- **Runtime:** HitEffects `_criticalHitSequenceStates`, `_autoAttackCycleCounts`, `_sourceStatusAutoAttackCycleCounts`, `_meleeAutoAttackCycleCounts`;
  DamageTaken `_recentGuardedHits`, `_recentDeflections`; Impact `_sameTargetHostileAbilityHitCounts`, `_areaAbilityTargetHitSequences`;
  DamageModifiers `_autoAttackCycleCriticalCounts` and four repeated-target and pressure tables; Activation `_hostileAbilitySequenceStates`;
  Enmity `_attackCommandTimes`, `_attackCommandTargets`, `_healingHitPoints`; the recorder's attribution stack, participants, pending damage,
  heal and damage applications, held events and writer (a reload ends a recording cleanly at quiescence, `HotReload.md`).
- `CombatAbilityImpactService` is the only one of the eleven that owns a `Rerun` boot handler.

**Content, Native and tests that reach them.** Counts are call sites by receiver name (approximate).

| Service | Content | Domain | Native and sinks | Tests | Engine tests |
|---|---:|---:|---:|---:|---:|
| `CombatHitEffects` | 3 | 21 | 5 | 26 | 30 |
| `CombatDamageTaken` | 37 | 19 | 11 | 65 | 19 |
| `CombatAbilityImpact` | 3 | 7 | 0 | 23 | 10 |
| `CombatRecorder` | 71 | 88 | 18 | 27 | 102 |
| `CombatDamageModifiers` | 5 | 29 | 16 | 61 | 21 |
| `CombatAbilityActivation` | 5 | 1 | 0 | 13 | 6 |
| `CombatAbilityBonus` | 26 | 20 | 9 | 76 | 48 |
| `CombatAbilityHit` | 3 | 4 | 3 | 18 | 330 (the two resolution overrides, 326 sites) |
| `Combat` | 10 | 0 | 2 | 21 | 1 |
| `Enmity` | 51 | 34 | 0 | 306 | 147 |
| `Death` | 0 | 0 | 0 | 34 | 6 |

CombatSim (`SWLOR.CombatSim`) reads `CombatRules`, `StatusEffectRules` and `StatRules`, and reflects `GameServices.Ability` and the
`<ConfigureWeaponAbility>` closure in `WeaponImpactReader`. It names none of the eleven services, so the readings test that nothing else
changed; they cannot drift from a move inside these files. `SWLOR.CLI` and `SWLOR.CraftSim` use none of them.

**Holders (constructor injection) that change a type.** The Abilities parts `AbilityDamageDeliveryService`, `AbilityDamageInputService`,
`AbilityDamageStageService`, `AbilityHitRollService`, `AbilityHostileImpactService`, `AbilityImpactLifecycleService`,
`AbilityStatusApplicationService`, `AbilityTargetImpactService`; `CombatRatingService`, `CombatDeflectionReactionService`,
`CombatPointService`, `CombatResourcesService`, `PvpCombatService`, `MeleeTechniqueService`, `WeatherService`, `DeathService`,
`QuestService`, `QuestEncounterService`, `SpaceService`, `EspionageInfiltrationService`, `StealthService`, `AIService`, the
`StatusEffect*` parts (recorder), `BeastMasteryService`, `DroidService`, `ItemService`, `TrapsService`, `CreatureStatService`,
`AbilityDarkForceService`, `AbilityImpactMessageService` (recorder), and `ServerTeardown` (the shutdown step). Which of them change depends on
the members they call; the PR that moves a member lists its holders.

**Logic-to-content lines that move with the code** (P5-2 removes them): HitEffects `Feature/StatusEffectDefinition` 17, DamageTaken 5 plus
`UsePerkFeat` 1 and `AbilityDefinition` 1, Impact 28 plus 2, Activation 12 plus 1, DamageModifiers 14, Bonus 7, Hit 2, `CombatService` 3,
Death 5 (`StatusEffectDefinition` 2, `PlayerTemporaryEffects`, `UsePerkFeat`, `AbilityDefinition`), Recorder `AbilityDefinition` 1.

**Upward edges** (`UpwardEdges.txt`) this item closes: HitEffects -> Quests 3 (`ICapstoneProofService`) and -> Social 3 (`IPartyService`),
Impact -> Social 3, Enmity -> Social 3 and -> World 3 (`IHoloComService`). Enmity -> Creatures 13 goes in P5-8e. HitEffects, Impact and
Enmity switch `IPartyService` to `IPartyRoster` (every member they call is on it: `GetAllPartyMembersWithinRange`, `IsInParty`). The two new
ports are `IBeastDamageRecording` (`RecordBeastDamage`, one member, implemented by `CapstoneProofService`) and `IHologramQuery`
(`IsHologram`, one member, implemented by `HoloComService`). `CombatAbilityActivationService -> Abilities` (2 members, vocabulary types)
stays for P5-15.

**Source-reading tests.** 31 test files read combat source through `CombatSource` (`Tests/Support/CombatSource.cs`), which combines
every `Domain/Combat/Combat*Service.cs` except the names in `CombatParts.OtherServices` and strips the receiver of a call on an injected
`ICombat*Service` field. New parts named `Combat*Service` are therefore found with no change, except the two recorder parts (below), which
join `OtherServices`. Tests that name a file by path: `EnmityService.cs` (7: `AIModelTests`, `EnmityAdjustmentTests`, `EnmityTests`,
`EspionageSystemTests`, `NPCChaseTests`, `RealEnmityTables`, `ReloadStateCarrierTests`), `DeathService.cs` (5: `AuthoredCombatAnimationTests`,
`ForceLightGuardianTests`, `PerformanceAudienceCostTests`, `PerformanceAudienceStateTests`, `StatusEffectDeliveryTests`),
`CombatRecorderService.cs` (3: `CombatParts`, `CombatSourceTests`, `DMPossessionPlayerLoopTests`), `CombatDamageModifiersService.cs`
(`MeleeRepeatedTargetStackTests`), and the snapshot and baseline files in "Baselines".

## Parts

Folder: all parts go in `Domain/Combat/` (the flat layout every Combat service uses) in namespace `SWLOR.Game.Server.Domain.Combat`, domain Combat. New
names are listed in `ConvertedServices.json` and `DomainMap.json` (Combat `services`) in the PR that adds them. Each new part has its own
interface and a `GameServices` property (derived from the interface name). Line counts are estimates of the whole class.

### CombatHitEffectsService (1,716 lines, 20 services) -> six parts

What the file does: it applies what a landed hit sets off for the attacker, in five groups (the hit itself, the auto-attack riders, the
critical hit, the defeated enemy), plus the triggered-damage pipeline. The recursion `ApplyDamageDealtEffects` -> splash ->
`ApplyTriggeredDamage` -> `ApplyDamageDealtEffects` is not a runtime cycle (the splash runs in a `DelayCommand`, and a triggered hit never
reaches the splash because `deliveryType` is not `Direct`), but it is a static call cycle, so it is cut as the orchestrator cut says.

| Part | Responsibility | Members that move (by name) | Lines | Services | Callers that change |
|---|---|---|---:|---:|---|
| `CombatHitEffectsService` (stay) | The orchestrator of a direct hit: tracking call, gates, splash, the effect steps in order; the auto-attack cycle damage (it draws from the cycle tables) | stays: `ApplyDamageDealtEffects`, `ApplyAutoAttackSplashEffects`, `ApplyAutoAttackCycleDamage`, `ApplySourceStatusAutoAttackCycleDamage`, `ConsumeAutoAttackCycleDamageBonus`, `ConsumeMeleeAutoAttackCycleDamageBonus`, `ForgetHitEffectState` | ~300 | 8 | none (23 sites of `ApplyDamageDealtEffects`) |
| `CombatDealtEffectsService` | The steps a landed direct hit grants the attacker: the tracking block, stamina and HP restore, delay reduction, hamstring, force erosion, mimicry procs, predator's mark, toxic rush, bleed effects | `ApplyHeavyVibrobladeDefenseDamageRecovery`, `ApplyPredatorsMarkEffects`, `ApplyPredatorsMarkFollowUp`, `ApplyLowHPDamageDealtHPRestore`, `ApplyBleedingTargetStaminaRestore`(+`Channel`), `ApplyDamageDealtForceErosionEffect`, `ApplyDamageDealtHamstringEffect`, `ApplyDamageDealtMimicryTraitProcs`, `MimicryTraitProcs`, `ApplySideAttackDamageEffects`, `ApplyDamageDealtStaminaRestore`, `ApplyDamageDealtAttackDelayReduction`, `ApplyNextDamageDealtBleedEffect`, `ApplyFrenzySlashHasteRefresh`, `ApplyToxicRushDamageDealtEffects`, `HasPositiveDirectDamageAfterNativeMitigation`; new public step methods cut from `ApplyDamageDealtEffects` (tracking block, the two HP-restore blocks) | ~500 | 9 | none (no external caller) |
| `CombatTriggeredDamageService` | Damage a trigger deals: outgoing modifiers, target adjustment, resistance, taken modifiers, queue or apply the effect | `ApplyTriggeredDamage` (+ `ApplyTriggeredDamageTargetAdjustment`) | ~110 | 8 | 23 sites (13 domain, 2 content, 5 tests, 3 engine) |
| `CombatAutoAttackEffectsService` | What an auto-attack adds: damage modifiers per attack, suppressed bonuses, master resource restore, hamstring, source status stacks, sundered-target FP restore | `ApplyAutoAttackDamageModifiers`, `ConsumeSuppressedAutoAttackDamageBonuses`, `ApplyAutoAttackMasterResourceRestore`, `ApplyAutoAttackHamstringEffect`, `ConsumeNextSkillAutoAttackDamageBonus`, `ApplySourceStatusStackEffects`, `ApplyInfectionStack`, `ApplyAutoAttackSunderedTargetFPRestore` | ~280 | 9 | 9 sites (2 native, 6 tests, 1 domain) |
| `CombatCriticalHitEffectsService` | What a critical hit sets off, and the non-critical ability riders | `ApplyCriticalHitEffects` (124 lines), `ApplyNonCriticalAbilityEffects`, the `ApplyCritical*` helpers, `CriticalHitSequenceState` and its table | ~465 | 10 | 14 sites (2 domain, 1 native, 5 tests, 6 engine) |
| `CombatDefeatedEnemyEffectsService` | What a kill sets off: spreads, nearby ally defense, cruel momentum, recent damagers | `ApplyDefeatedEnemyEffects`, `ApplyRecentDamagerDefeatedEnemyEffects`, `SpreadPoisonFromTarget` and their helpers | ~255 | 9 | 12 sites (3 domain, 3 tests, 6 engine) plus `CombatService` |

Cuts: (1) `ApplyDamageDealtEffects` keeps its text except that the tracking block (capstone proof, paired-ranged record, activity tracking)
and the two inline HP-restore blocks become calls to `CombatDealtEffectsService` (about 40 lines move; the statements and their order are
unchanged). (2) `ApplyTriggeredDamage` calls the tracking step of `CombatDealtEffectsService` where it called
`ApplyDamageDealtEffects(..., Triggered)`: for a non-direct delivery the original runs exactly that block and returns, so the call is
equal. That removes the static cycle. (3) The seam `ApplyDamageTakenModifiers` (the `Lazy<>` wrapper) is a plain call to
`CombatDamageTakenService` in the triggered part.

### CombatDamageTakenService (1,649 lines, 20 services) -> seven parts and `CombatDamageRollService`

What the file does: the modifiers applied before damage lands (percent, leadership, redirect, share, fatal prevention), the effects
after damage (low-HP riders, melee poison, avoided attacks, deflections), the guard (reduction, retaliation, next-skill effects) and
the reflections. Pre-damage modifiers are the most called family (94 sites: `ApplyDamageTakenModifiers` 37, `ApplyDamageOverTimeTakenModifiers`
19), so they stay.

| Part | Responsibility | Members that move | Lines | Services | Callers that change |
|---|---|---|---:|---:|---|
| `CombatDamageTakenService` (stay) | Damage taken modifiers: percent, typed leadership, redirect and share to a status source, damage over time, fatal prevention, the physical-to-force defense conversion, guard reduction reads | stays: `ApplyDamageTakenModifiers`, `ApplyDamageTakenPercentageModifiers`, the leadership members, `ApplyDamageTakenRedirectToStatusSource`, `ApplyDamageTakenShareToStatusSource`, `ApplyDamageOverTimeTakenModifiers`, `TryPreventFatalDamageAndGrantTemporaryHP`, `ApplyLowHPTemporaryHPBeforeFatalDamage`, `ApplyIncomingPhysicalToForceDefenseConversion`, `IsHostileAttackSource`, `GetGuardDamageReductionPercent`, constants | ~490 | 7 | none |
| `CombatDefenseMemoryService` | Who was guarded or deflected recently: the two tables and their reads, no dependencies | `HasRecentGuardedHit`, `HasRecentDeflection`, new `NoteGuardedHit`, `NoteDeflection` (the table writes cut out of `TrackGuardedHit` and `TrackDeflection`), `ForgetDefenseMemory` | ~70 | 0 | 10 sites (4 content, 4 tests, 2 engine) |
| `CombatDamageTakenEffectsService` | The after-damage handler and reactions: low-HP effects, reversal cut, melee damage taken and poison, avoided attacks, attack deflection | `ApplyDamageTakenEffects` (the handler), `ApplyLowHP*`, `ApplyReversalCutReady`, `ApplyDamageTakenNextSkillAbilityDamage`, `ApplyRecentDamageTargetHitEffects`, `ApplyMeleeDamageTaken*`, `TrackAvoidedAttack` and `ApplyAvoided*`, `TrackDeflection`, `ApplyDeflectionNearbyAllyGuard`, `ApplyAbilityUsedAttackDeflection`, `ApplyAbilityGrantedAttackDeflectionEffects` | ~575 | 10 | 44 sites (6 content, 18 tests, 10 engine, 3 native, 7 domain) |
| `CombatGuardedHitService` | A guarded hit: reduction, recovery, enmity, then retaliation and next-skill effects | `ApplyGuardedHitModifiers`, `ApplyGuardedHitRecovery`, `ApplyGuardedHitEnmity` | ~110 | 9 | 3 sites |
| `CombatGuardedHitEffectsService` | The effects a guarded hit grants the guard | `TrackGuardedHit`, `ApplyGuardedHitNextSkillAbilityEffects`, `ApplyGuardedHitNextSkillAbilityStatusEffects`, `ConsumeGuardedHitNextSkillAbilityExposedDamageBonus`, and `ApplyGuardedHitNextAttackEffects` from `CombatAbilityHitService` (its only caller is `TrackGuardedHit`) | ~240 | 5 | 7 sites plus 2 |
| `CombatGuardRetaliationService` | The damage a guard returns | `ApplyGuardedHitRetaliation`, `ApplyGuardedHitRetaliationPulse`, `ResolveGuardRetaliationDamage`, `GetGuardRetaliationDamageAbility` | ~190 | 9 | none |
| `CombatDamageReflectionService` | Damage given back: reflection, force conversion, ranged deflection reflection | `ApplyDamageReflectionEffects`, `ApplyForceDamageTakenEffects`, `ApplyIncomingPhysicalToForceConversion`, `ApplyRangedDeflectionReflection`, `BuildDeflectingReturnCombatLogMessage` | ~220 | 8 | 17 sites (3 domain, 3 native, 11 tests) |
| `CombatDamageRollService` (from `CombatService`) | The random damage roll (`CalculateDamage`: the range from `CombatRules`, then `Random`) | `CalculateDamage` (31 lines, no services) | ~50 | 0 | 10 content, 2 native, 11 tests |

`CombatDamageTakenEffectsService` is at 10 services (attackTiming, combatActivity, recorder, resources, stat, statusEffect,
statusEffectApplication, temporaryStatModifier, triggered damage, defense memory). If the build counts one more, `ApplyMeleeDamageTaken*`
(29 + 24 lines, recorder, triggered damage) moves to `CombatDamageReflectionService`, which has room.

Handler: `ApplyDamageTakenEffects` moves to the effects part (`ScriptHandlerOrder.txt` line 344, `HandlerRenames.txt` line 146, renamed in place).

### CombatAbilityImpactService (1,720 lines, 16 services) -> six parts

What the file does: the effects of an ability hit (riders), the area impacts, the damage bonus reads, and the status-applied listener.

| Part | Responsibility | Members that move | Lines | Services | Callers that change |
|---|---|---|---:|---:|---|
| `CombatAbilityImpactService` (stay) | The orchestrator of a successful ability hit: the step order, status riders dispatch, target status effects | stays: `ApplySuccessfulAbilityImpactRiders`, `ApplyAbilityStatusRiders`, `ApplyAbilityTargetStatusEffects` | ~200 | 9 | none |
| `CombatAbilityDamageBonusService` | Damage bonus reads before the hit | `GetAbilityImpactBaseDamageBonus`, `GetHighResourceAbilityDamageBonus`, `GetCostlyAbilityDamageBonus` | ~145 | 7 | 16 sites (2 domain, 9 tests, 5 engine) |
| `CombatAbilityHitRidersService` | Hit-time effects of a hostile ability: costly, near-target, same-target, next auto-attack delay | `ApplyCostlyAbilityHitEffects`, `ApplyRangedAbilityHitNearTargetEffects`, `ApplySameTargetHostileAbilityHitEffects`, `ApplyHostileAbilityHitNextAutoAttackNoDelay` and the table | ~210 | 4 | none |
| `CombatAbilityAreaImpactService` | Area abilities: pulse, fragmentation, ricochet, cluster storm, area hit rewards, area hit sequence, the impact effects entry | `ApplyAbilityDamageRiders`, `ApplyAreaAbility*`, `ApplyRicochetDamage`, `ApplyClusterStormDamage`, `ApplyBleedingTargetAbilitySplash`, `ApplySaturationToss`, `ApplyAbilityImpactEffects`, `ApplyAreaHitHaste`, `ApplyAreaHitRewards`, `ApplyStackingAttackBoost` and the sequence table | ~500 | 9 | 12 sites (2 domain, 2 content, 3 tests, 5 engine) |
| `CombatAbilityWeaponRidersService` | Weapon-family impact riders (heavy vibroblade, force dark, lightsaber, pistol, rifle, saberstaff, spear, staff, throwing, twin blade, vibroknife, katar venom) | the `Apply*ImpactRiders` family, `ApplyBleedingTargetAbilityBleedRefresh`, `ApplyBleedingTargetAbilityBleedSpread`, `HasSunderPenaltyAtLeast`, `ApplySpearDisablerSuppressionRiders`, `ApplyGuardedHitNextSkillAbilityExposedStatus` | ~515 | 9 | 1 site |
| `CombatAbilityStatusAppliedService` | The status-applied listener and the hostile-debuff party buff | `RegisterStatusEffectListener` (the boot handler), `OnStatusApplied`, `OnHostileDebuffApplied`, `OnApplicationCompleted`, `ApplyHostileAbilityPartyBuff`, `ApplyStatusApplied*`, `IsAttackingAllyOf`, `IsTargetAttackingAllyForStatusAppliedRiders`, `ReplaceTemporaryStat` | ~330 | 10 | 9 sites (7 tests) |

At 10 services the status-applied part uses `IPartyRoster` (not `IPartyService`). If the build counts one more, `IsAttackingAllyOf` (enmity, party)
moves to the hit-riders part. `RegisterStatusEffectListener` keeps its event and position; it is renamed in place (`ScriptHandlerOrder.txt`
line 58, `HandlerRenames.txt` line 320 with the branch name, `BootHandlerClassification.txt`).

### CombatRecorderService (1,691 lines, 6 services) -> three parts; `CombatRecordSerializer.Write` cut

What the file does: it is the recording session (start, stop, status, snapshots, the event queue and participant ids), the attribution
stack, and 47 `Record*`, `Begin*` and `Expect*` event methods. Lower domains reach it through `ICombatRecordingState`, `ICombatRecording` and
`ICharacterRecording`, which the stay-part keeps implementing (one instance behind four registrations, plus `IShutdownStep`). The
members those interfaces name (attribution, healing reaction, resource and item records, status records) stay.

| Part | Responsibility | Members that move | Lines | Services | Callers that change |
|---|---|---|---:|---:|---|
| `CombatRecorderService` (stay) | Session, event queue, participants, snapshots, attribution, healing reaction, resource, item and status records | stays: everything the narrow interfaces name, `Start`, `Stop`, `GetStatus`, `StopOnShutdown`, `Enqueue` and the participant reads (promoted to public) | ~880 | 6 | none |
| `CombatRecordDamageService` | Damage, healing, temporary hit point and death records, with the pending-damage matching and the death handlers | `ExpectDeferredDamage`, `ExpectHitPointCost`, `ExpectWeaponDamage`, `HasPendingDamage`, `BeginDamageApplication`, `EndDamageApplication`, `RecordDamageApplied`, `RecordDamageEvents`, the heal application members, `RecordPvpHealingReduced`, `RecordHealingApplied`, the temporary hit point records, `RecordCreatureDeath` and `RecordPlayerDeath` (handlers), `RecordRespawn`, `RecordResurrection`, `RecordSubdual`, `KeepLethalHitPoints` and their tables | ~660 | 2 | 46 sites (14 content, 7 native, 6 domain, 17 tests, 2 engine) |
| `CombatRecordAbilityService` | Ability records: cast, denied, interrupted, queued, impact, recurring impact, cost, attack, toggle notes | `RecordAbilityCast`, `RecordAbilityDenied`, `RecordAbilityInterrupted`, `RecordAbilityQueued`, `RecordAbilityImpact`, `RecordRecurringAbilityImpact`, `BeginAbilityCost`, `CompleteAbilityCost`, `RecordAttack`, `NoteToggleDeactivated`, `TakeToggleDeactivation` | ~290 | 2 | 20 sites (15 content, 4 domain, 1 native) |

New state ownership: the damage part owns `_pendingDamage`, `_damageApplications`, `_healApplications`, `_lastDamageSources`, `_deathFrames`,
`_temporaryHitPointGrants`. `Start` and `Stop` used to clear them, so the stay-part holds a list of `ICombatRecordingSessionListener`
(`OnRecordingStarted`, `OnRecordingStopped`), the damage part registers from a `Rerun` boot handler (the P5-5d pattern), and a notice with an
empty list throws `InvalidOperationException`. The two new part names join `CombatParts.OtherServices` so `CombatSource` does not absorb them.
If the stay-part measures over about 950 lines after the move, the status, resource and item records (about 150 lines) join the damage
part. The recorder keeps its two `Lazy<>` parameters (Cycles).

`CombatRecordSerializer.Write` (198 lines): one `switch` over 22 event types. It becomes a dispatcher of about 30 lines that calls four
static methods, each holding the original `case` bodies verbatim: `WriteLifecycleEvent` (started, stopped, participant), `WriteAbilityEvent`
(cast, queued, denied, interrupted, impact, cost), `WriteDamageEvent` (attack, damage, temporary hit points, healing) and `WriteStateEvent`
(resource, status, death, resurrection, subdual, snapshot); each stays under 70 lines. `CombatRecordSerializerTests` (exact JSON lines)
proves it.

### CombatDamageModifiersService (1,436 lines, 9 services) -> three parts

| Part | Responsibility | Members that move | Lines | Services | Callers that change |
|---|---|---|---:|---:|---|
| `CombatDamageModifiersService` (stay) | Damage dealt modifiers: the orchestrator, ability shape, status source, positional (side and back attack), attack adjustments | stays: `ApplyDamageDealtModifiers`, `ApplyOrdinaryAttackDamageMultiplier`, the `Apply*ShapeDamageModifier` family, `ApplyTargetStatusDamageModifiers` (138 lines), `ApplyDamageTypeDealtModifiers`, `ApplySideAttack*`, `ApplyBackAttack*`, `GetLow*AttackAdjustment`, `ApplyStatusSource*Modifiers`, `ApplyAreaAbilityAfterDeflectionDamageModifier` | ~780 | 9 | none |
| `CombatCriticalModifiersService` | Critical rate and critical damage modifiers and the auto-attack cycle critical tracker | `CalculateDamageWithCriticalMitigation`, `TryUseIncomingCriticalHitDowngrade`, `ApplyCriticalDamageModifier`, `Get*CriticalRate*Adjustment`, `PrepareAutoAttackCycleCriticalRate`, `ClearAutoAttackCycleCriticalRateTracker` and its table | ~285 | 7 | 32 sites (7 domain, 8 native, 16 tests, 1 content) |
| `CombatRepeatedTargetService` | Repeated-target and same-target pressure state and effects | `ApplyRepeatedTargetDamageModifier`, the melee and ranged variants, `ClearRangedRepeatedTargetDamageTracker`, `ApplySameTargetPressureDamageEffects`, the `SameTargetPressure*` members, the four state tables, `ForgetDamageModifierState` | ~410 | 3 | 10 sites (5 domain, 5 tests) |

The stay-part's `Lazy<ICombatDamageTakenService>` (only for `HasRecentDeflection`) becomes a plain `ICombatDefenseMemoryService` parameter.

### CombatAbilityActivationService (1,203 lines, 14 services) -> five parts

`ApplyAbilityActivatedEffects` runs 24 steps. The orchestrator stays and calls nine runs in the original order: prepare (5 steps), defense
(4), offense (1), defense (4), offense (3), defense (3), offense (1), weapon riders, offense (2).

| Part | Responsibility | Members that move | Lines | Services | Callers that change |
|---|---|---|---:|---:|---|
| `CombatAbilityActivationService` (stay) | The step order, queued weapon ability tracking | stays: `ApplyAbilityActivatedEffects`, `TrackQueuedWeaponAbilityUse`, `ResolveActivatedAbilitySkillType` | ~115 | 7 | none (19 sites) |
| `CombatAbilityCostEffectsService` | Effects tied to what an ability costs or primes: recast reduction, next-skill bonuses, hit point spend, force FP cost | `ApplyHitPointSpendAbilityEffects` (public), `ApplyAbilityUsedRecastReduction`, `ApplyAbilityUsedNextSkill*`, `ApplyAbilityUsedMasterAbilityHitChance`, `ApplyForceFPCostActivatedEffects`, `ApplyHitPointSpend*` | ~220 | 8 | 4 sites (2 content, 2 tests) |
| `CombatAbilityDefenseEffectsService` | Evasion, deflection and ally-defense effects | `ApplyAbilityUsedSkillEvasion`, `ApplyHostileAbilityUsedEvasion`, `ApplyCostlyAbilityUsedEvasion`, `ApplyAreaAbilityUsedEvasion`, `ApplyAbilityUsed*Deflection`, `ApplyAbilityUsedNearbyAllyDefense`, `ApplyAbilityUsedPerkCategory*`, `ApplyAbilityUsedEvasion` | ~400 | 7 | none |
| `CombatAbilityOffenseEffectsService` | Hostile ability effects: movement speed, area rewards, force attack, attack adjustment, sequence and resource restore | `ApplyAbilityUsedMovementSpeed`, `ApplyAreaAbilityUsedRewards`, `ApplyHostileAbility*`, `ApplyNextAutoAttackDamageBonus`, `ForgetActivationState`, the sequence table | ~285 | 8 | 2 sites |
| `CombatAbilityWeaponActivationService` | Weapon-family activation riders | `ApplyAbilityActivatedRiders`, `ApplyHeavyVibrobladeActivatedEffects`, `ApplyGuardiansResolve`, `ApplyBeastBalancedAbilityRecovery`, `ApplyVibroknifeShadowActivatedEffects`, `ApplyPistolSkirmisherActivatedEffects`, the three lightsaber members | ~345 | 9 | none |

### CombatAbilityBonusService (1,061 lines, 9 services) -> two parts

The service is stateless (the bonuses live in `ITemporaryStatModifierService`), so the split is by what the bonus is for.

| Part | Responsibility | Members that move | Lines | Services | Callers that change |
|---|---|---|---:|---:|---|
| `CombatAbilityBonusService` (stay) | Next-ability and next-skill bonuses and the ability adjustment reads | stays: `ConsumeNextSkillAbilityBonuses`, `GrantNext*`, `ConsumeNext*`, `GetNext*`, persistent critical rate, guarded-hit and status-applied bonuses, `GetAbility*Adjustment`, `ApplyAbilityRecastDelayModifiers`, `GetAbilityDamageBonus` | ~540 | 9 | none |
| `CombatQueuedAbilityBonusService` | Opening auto-attack, queued weapon ability and idle activation bonuses | `PrepareOpeningAutoAttack`, `PrepareQueuedWeaponAbilityOpeningAttackAtActivation`, `Store*`, `Clear*`, `Has*`, `ConsumeQueuedWeaponAbilityBonuses`, `GetQueuedWeaponAbilityActivationHitChanceAdjustment`, the idle snapshot reads | ~455 | 6 | 67 sites (22 content, 28 tests, 12 engine, 5 native) |

### CombatAbilityHitService (426 lines, 12 services) -> two parts

| Part | Responsibility | Members that move | Lines | Services | Callers that change |
|---|---|---|---:|---:|---|
| `CombatAbilityHitService` (stay) | Resolving an ability hit and the two resolution overrides the engine tests set | stays: `TryResolveAbilityHit`, `GetAbilityAccuracy`, `GetIncomingAbilityHitChanceAdjustment`, `Set*HitResolutionOverride`, `GetAutoAttackHitResolutionOverride` | ~180 | 9 | none (340 sites, 326 of them the overrides) |
| `CombatAbilityHitAdjustmentService` | Critical rate and hit adjustments, evaded effects | `CalculateAbilityCriticalRating`, `GetAbilityCriticalRate`, `GetAbilityHitOrCriticalAdjustment`, the sundered target reads, `ApplyRangedAbilityTargetDefenseReduction`, `ApplyForceAbilityEvadedEffects` | ~225 | 7 | 16 sites (2 domain, 3 content, 2 native, 7 tests, 2 engine) |

`ApplyGuardedHitNextAttackEffects` (58 lines) leaves for `CombatGuardedHitEffectsService` (above).

### CombatService (198 lines, 12 services) -> stay-part and `CombatDamageRollService`

It keeps its three handlers, `RefreshStatDrivenTrackerEffects` and the cleanup, and loses `CalculateDamage`. The cleanup
(`RemoveStatTriggerCooldowns`) calls twelve forgets in a fixed order; after the splits it would need about fifteen services. It takes
`IEnumerable<ICombatCreatureStateOwner>` instead (one method, `ForgetCreatureCombatState(uint)`), implemented by every part that holds a
per-creature table (resources, activity, suppression, attack timing, the defense memory, the activation offense part, the hit effects
stay-part and critical part, the impact hit-riders and area parts, the two damage modifier parts, `TemporaryStatModifierService`) and
registered in `ServiceRegistrations` in the old order. This is the constructor pattern the StatusEffect guide rejected for part 1 because its
implementers held the receiver; here nothing holds `CombatService` (after the `Lazy<>` goes, only `PerkService`'s `Lazy<>` does, and that one
stays), so it is plain. `ForgetOpeningActivityState` and `ForgetAbilityStaminaCostState` fold into their owner's single method; each
forget removes entries keyed by the creature from its own tables, and a characterization test (a populated bench cleared by the old
sequence and the new list, every table compared) proves the order does not matter.
`CombatService` after: stay-part ~200 lines, 8 services (the defeated-enemy part, the owner list, suppression, combatActivity, stat,
statusEffect, temporaryStatModifier, the two damage modifier parts). `RefreshStatDrivenTrackerEffects` stays on `ICombatService`, so
`PerkService` (P5-10's file) is not edited.

### EnmityService (1,244 lines, 12 services) -> three parts

This is the D1 split of the Creatures guide: the part the AI core calls (`GetEnmityTable`, `ClearEnmityTable`, `RemoveEnmity`) must never
call the leash, so the table and the changes that call the leash are separate parts, and the one call from the leash back to
`AttackHighestEnmityTarget` becomes a notice. It builds on the P5-8e ports (`ICombatLeash`, `ICompanionTargetQuery`); service counts below
are after them (the counts do not change: `IAIService` becomes `ICombatLeash`, `ICompanionControlService` becomes `ICompanionTargetQuery`).

| Part | Responsibility | Members that move | Lines | Services | Callers that change |
|---|---|---|---:|---:|---|
| `EnmityService` (stay) | The tables and their reads: the three carried tables, the attack-command memory, reads, threat checks, removal and clearing | stays: `GetEnmityTable`, `GetEnmityTowardsAllEnemies`, `GetHighestEnmityTarget`, `HasEnmity`, `HasProximityEnmity`, `HasNonProximityEnmity*`, `HasOnlyProximityEnmity`, `IsThreatSecured`, `IsThreatLeadSecure`, `RemoveEnmity`, `RemoveProximityEnmity`, `ClearEnmityTable`, `ClearEnmityTables`, the raw reads, the tracking removals; new public accessors for the attack-command memory (`TryGetAttackCommand`, `RecordAttackCommand`, `ForgetAttackCommand`) and for the three tables | ~350 | 0 | none (about 250 sites) |
| `EnmityChangeService` | Changes to enmity and the handlers that cause them | `CreatureDamaged`, `CalculateDamageEnmity`, `HealingStarted`, `HealingCompleted`, `CalculateHealingEnmity`, `CreatureAttacked`, `ModifyEnmity` (both), `CalculateAdjustedEnmity`, `TryTaunt`, `CalculateTauntEnmity`, `ModifyEnmityOnAll`, `ReduceEnmity`, `ReduceEnmityOnAll`, `CalculateReducedEnmity`, `ModifyProximityEnmity`, `RemoveCreatureEnmity`, `CreatureDeath`, `CreatureDestroyed`, `PlayerDeath`, `PlayerExit`, `CreatureLimbo`, the healing table | ~460 | 8 | about 250 sites (`ModifyEnmity` 138: 16 content, 63 tests, 50 engine, 9 domain) |
| `EnmityAttackService` | Choosing and commanding the attack target | `GetHighestEnmityAttackTarget`, `IsEligibleAttackTarget`, `ReevaluateEnemyAttackTargets`, `AttackHighestEnmityTarget`, `ResumeAttackAfterActionsCleared`, `IssueAttackCommand` and the stale-attack, hold and move-range helpers | ~385 | 8 | 54 sites (7 content, 41 tests, 4 engine, 2 domain) |

The ten handlers move to `EnmityChangeService` (the eight events and `PlayerExit` twice), one `PlayerSession` classification line
(`PlayerExit`, `LogoutOnly`) moves with them. The carried keys do not change, because the tables stay in `EnmityService`; no
`[ReloadCarriedFrom]` and no `ReloadAdoption.txt` change. The three new notices are in Cycles. Hot path: `ModifyEnmity` runs per damage
event; it gains two interface calls (the table and the attack part), which `CombatLoad` measures.

### DeathService (385 lines, 10 services) -> two parts

| Part | Responsibility | Members that move | Lines | Services | Callers that change |
|---|---|---|---:|---:|---|
| `DeathService` (stay) | Dying, death and the respawn handler | stays: `OnPlayerDying`, `OnPlayerDeath`, `IsSubdualDeath`, `RecordDeathDebtWaiver`, `OnPlayerRespawn` | ~250 | 9 | none |
| `DeathRespawnService` | Respawn points, locations and penalties | `InitializeRespawnPoint` (the `OnModuleEnter` handler, `LoginOnly`), `GetRespawnLocation`, `HasRespawnArea`, `IsWalkable`, `SendToHomePoint`, `ApplyPenalties`, `WriteAudit` | ~170 | 4 (db, area, property, playerName) | 20 sites (`GetRespawnLocation` 10, `InitializeRespawnPoint` 5, `HasRespawnArea` 5; 16 tests, 4 engine) |

`DeathService` also swaps `ICombatDamageTakenService` for the same interface (`TryPreventFatalDamageAndGrantTemporaryHP` stays on the
stay-part). Guard count after: 9.

### Totals

| Service | Lines before | Services before | Parts (lines, services) |
|---|---:|---:|---|
| `CombatHitEffectsService` | 1,716 | 20 | stay 300/8, Dealt 500/9, Triggered 110/8, AutoAttack 280/9, Critical 465/10, Defeated 255/9 |
| `CombatDamageTakenService` | 1,649 | 20 | stay 490/7, Effects 575/10, Memory 70/0, GuardedHit 110/9, GuardedHitEffects 240/5, Retaliation 190/9, Reflection 220/8 |
| `CombatAbilityImpactService` | 1,720 | 16 | stay 200/9, DamageBonus 145/7, HitRiders 210/4, AreaImpact 500/9, WeaponRiders 515/9, StatusApplied 330/10 |
| `CombatRecorderService` | 1,691 | 6 | stay 880/6, Damage 660/2, Ability 290/2 |
| `CombatDamageModifiersService` | 1,436 | 9 | stay 780/9, Critical 285/7, RepeatedTarget 410/3 |
| `CombatAbilityActivationService` | 1,203 | 14 | stay 115/7, Cost 220/8, Defense 400/7, Offense 285/8, Weapon 345/9 |
| `CombatAbilityBonusService` | 1,061 | 9 | stay 540/9, Queued 455/6 |
| `CombatAbilityHitService` | 426 | 12 | stay 180/9, Adjustment 225/7 |
| `CombatService` | 198 | 12 | stay 200/8, DamageRoll 50/0 |
| `EnmityService` | 1,244 | 12 | Tables (stay) 350/0, Change 460/8, Attack 385/8 |
| `DeathService` | 385 | 10 | stay 250/9, Respawn 170/4 |
| `CombatRecordSerializer.Write` | 198 | - | dispatcher 30 and four methods under 70 |

New services: 30 (HitEffects 5, DamageTaken 6, `CombatDamageRollService` 1, Impact 5, Recorder 2, DamageModifiers 2, Activation 4, Bonus 1,
Hit 1, Enmity 2, Death 1). Each has an interface, a `ServiceRegistrations` line and a `GameServices` property. By comparison the StatusEffect
split added six. The count is driven by the 10-service limit (20 injected services in each of two files), not by the line limit.

**Risks in the sizes.** Parts at 10 services: `CombatDamageTakenEffectsService`, `CombatCriticalHitEffectsService`,
`CombatAbilityStatusAppliedService`. Each has a named fallback: the first and third above, and for the critical part
`ApplyCriticalBleedingStatusDurationExtension` (20 lines, the only user of the duration service there) moves to `CombatAutoAttackEffectsService`,
which already holds that service. The recorder's stay-part is the largest part left (about 880
lines, 88% of the limit; fallback above). The first PR of each service re-measures; a part that measures over a limit moves the member group
named in its row to its lower neighbour, never a new numbered type.

## Cycles

`LazyDependencyCycleTests` fails on a needless `Lazy<>`, and a plain cycle deadlocks the container build (P5-5g). The scan (a reading of
every constructor in `ServiceRegistrations` with `Lazy<>` edges kept apart) finds one strongly connected group of 68 services (with the `Lazy<>` edges counted) that holds
every Combat `Lazy<>` below. Plain paths were checked for each `Lazy<>` this split removes.

**Broken by this split (three `Lazy<>` leave `DeferredResolution.txt`):**

| Today | Why it exists | How it goes |
|---|---|---|
| `CombatDamageModifiersService -> Lazy<ICombatDamageTakenService>` | one read, `HasRecentDeflection` | the read moves to `CombatDefenseMemoryService`, which takes no service; plain |
| `CombatHitEffectsService -> Lazy<ICombatDamageTakenService>` | `ApplyTriggeredDamage` calls `ApplyDamageTakenModifiers`, and damage taken calls hit effects back (reflection, retaliation, poison) | the damage taken stay-part keeps only the pre-damage modifiers, whose seven dependencies (recorder, pvpCombat, queries, resources, stat, enmity, statusEffect) reach no hit effects part by plain edges; the triggered part takes it plain, and every part that calls hit effects is above it |
| `CombatDamageTakenService -> Lazy<ICombatService>` | one call, `CalculateDamage` (guard retaliation) | `CalculateDamage` moves to `CombatDamageRollService` (no services) |

**Broken in the Enmity PR (P5-7k), with P5-8e's ports in place:**
- `AIService -> Lazy<IEnmityService>` (the D1 cycle). The leash reads and removes through `EnmityService` (the table part, which never calls the
  leash) and its call to `AttackHighestEnmityTarget` becomes a Combat-owned notice `ILeashRetargetListener`, with the list in `AIService`'s core
  and the registration from `EnmityAttackService` in a `Rerun` boot handler; an empty list throws. `AIService` loses the `Lazy<>`; the
  Creatures guide expects this (D1, risks).
- `StealthService -> Lazy<IEnmityService>`. Stealth makes two retarget calls (`ReevaluateEnemyAttackTargets`, `AttackHighestEnmityTarget`) and one
  table read (`GetEnmityTable(...).Count`, plain once the table part has no path to stealth). The two calls become a Combat-owned notice
  `IStealthRetargetListener`, with the list in `StealthService` and the registration from `EnmityAttackService` (the same boot handler).
  The edit to `StealthService.cs` is about 15 lines (P5-8e also edits it, so P5-7k runs after it).

**Not broken here, routed:**

| Cycle | Why it stays | Goes to |
|---|---|---|
| `CombatRecorderService -> Lazy<IStatService>` | `StatService` takes the recorder (10 uses) and the recorder reads FP and stamina for snapshots and costs | P5-15: Foundation-owned reader for the four resource reads |
| `CombatRecorderService -> Lazy<ICombatQueriesService>` | `CombatQueriesService` reaches the recorder by plain edges; the recorder reads the equipped weapon skill (via `SkillService`) | P5-15: the same Foundation reader or a Character-owned weapon skill read |
| `PerkService -> Lazy<ICombatService>` (up) | one call, `RefreshStatDrivenTrackerEffects`; the refresh's dependencies (stat, activity, suppression) reach `PerkService` by plain edges | P5-10 or P5-15: a Character-owned perk-change notice that Combat registers with; P5-7 leaves the method on `ICombatService` so the file is not edited |
| `StealthService -> Lazy<IPerkService, IStatService, IStatusEffectService, IStatusEffectApplicationService, IEspionageInfiltrationService>` | `StatusEffectService -> MessagingService -> StealthService`, and `MessagingService` takes `IStealthService` only for `IsConcealed` | P5-15: `MessagingService` takes `IConcealmentQuery` from a no-dependency provider; then P5-8e's and P5-5h's notes shrink |
| `AIService -> Lazy<IStatusEffectService>` and the other P5-8 ones | same Messaging path (the Creatures guide lists them) | P5-8e, P5-8f, P5-8g, P5-15 as that guide says |
| `StatusEffectDurationService` and `StatusEffectCombatEventService -> Lazy<IStatService>` | `StatService -> ResourceRestoredEffectsService -> StatusEffectApplicationService -> the part` | P5-15: a listener inversion at `ResourceRestoredEffectsService` |
| `ItemService -> Lazy<ITwinPistolEquipmentService>` | unchanged | P5-9 / P5-15 |

**New edges and why they are plain.** The layering, bottom to top: defense memory, damage roll and the damage taken stay-part; dealt effects;
triggered damage; hit effects stay-part; auto-attack, critical and defeated parts and the guard, reflection, taken-effects parts; the
ability impact and activation parts; `CombatService`. The impact and activation parts call hit effects and damage taken, never the reverse.
The recorder parts call the stay-part (plain) and the stay-part reaches them only through the listener list, so there is no cycle. Each PR
runs `LazyDependencyCycleTests`, the container validation (`ValidateOnBuild`) and, for every constructor change, the engine start (a plain
cycle hangs the build; a hung run means a new cycle, dump with `dotnet-dump collect`).

## Hot reload and handlers

- **Handlers that move** (`ScriptHandlerOrder.txt`, `HandlerRenames.txt`, renamed in place, event and position unchanged): `ApplyDamageTakenEffects` to
  the effects part; `RegisterStatusEffectListener` to the status-applied part; the ten Enmity lines to `EnmityChangeService`;
  `InitializeRespawnPoint` to `DeathRespawnService`; `RecordCreatureDeath` and `RecordPlayerDeath` to `CombatRecordDamageService`. 15 lines in all.
  `StopOnShutdown`, the three `CombatService` handlers and the other Death handlers stay.
- **Classification files:** `BootHandlerClassification.txt` (the impact registration moves; new `Rerun` registrations for the recorder damage part,
  the recorder ability part if its toggle notes reset at `Start`, and the enmity attack part), `PlayerSessionClassification.txt` (`PlayerExit` of Enmity, `InitializeRespawnPoint`).
- **Carried state:** `EnmityService` keeps all three carried keys. The attack-command memory and healing table are runtime and rebuilt empty,
  as today. `ReloadAdoption.txt` and `ReloadCarryHistory.txt` do not change.
- **Reload checks:** run `scripts/run-hot-reload-check.sh --engine-filter HotReloadPlayers --reloads 2` on the PRs that move handlers or
  registrations (P5-7h2, j1, j2, k, l). The Enmity PR also runs the carried-table round trip in `ReloadStateCarrierTests` and an engine run
  that reloads with an enemy holding enmity.
- **Registration order:** the three new listener lists load in `OnModuleCacheBefore` after the data caches, before the first notice, as the
  P5-5d lists do. An adoption-order test is not needed (nothing new is carried).

## Tests and the proof plan

**Groundwork (PR P5-7b, no production code), as P5-5b did for status effects.**
- `CombatSource`: `CombatParts.OtherServices` gains `CombatRecordDamageService` and `CombatRecordAbilityService`; a reader for the files that
  do not start with `Combat` (`EnmitySource`, `DeathSource`, like `StatusEffectSource`), so the 12 path-reading tests (Enmity 7, Death 5) read
  through one place. `CombatSourceTests` proves each new reader (needles still found, receivers stripped).
- Unit doubles: each test class subclasses its service to override the `protected virtual` seams (`CombatHitEffectsServiceTests` 1,860 lines,
  `CombatDamageTakenServiceTests` 2,130, `CombatAbilityImpactServiceTests` 2,008, `CombatAbilityActivationServiceTests` 1,639,
  `CombatRecorderServiceTests` 1,342, `CombatDamageModifiersServiceTests` 1,168, `EnmityServiceTests` 1,124, `CombatAbilityBonusServiceTests` 730,
  `DeathServiceTests` 596, `CombatAbilityHitServiceTests` 568, `CombatServiceTests` 377; 14,000 lines in all). The seam wrappers travel with the part
  that calls them; each double becomes partial files by part, and each test is pointed at the part that owns its member.
- `CombatPartForwardingTests.Parts()` and `CombatFacadeAllocationTests` (the allocation pins; 307 static lookup lines) gain the new
  interfaces; the zero-allocation pins stay on the stay-parts that hold the hot reads.
- Reflection users and snapshot files: `ReloadStateCarrierTests` (the Enmity field names are unchanged), the `Start`/`Stop` reset check in
  `CombatRecorderServiceTests` (follows the session listener), the `CombatRecordSerializerTests` JSON lines (unchanged).

**Per PR** (build once with `-p:RunPostBuildEvent=Never -v q -clp:ErrorsOnly`, then `dotnet test <project> --no-build --filter`):
- **CombatSim readings byte-identical** to the base: `team-fight`, `twin-pistol-barrage`, `twin-pistol-crossfire`, `solo-boss`; seed 1, 50 runs, Markdown
  and JSON, plus the ability table. CombatSim reads `CombatRules`, `StatusEffectRules` and `StatRules` only, so a difference means a rule changed, not a move.
- **Characterization and parity:** `SWLOR.Game.Server.CharacterizationTests` (148 tests; the `Combat*CharacterizationTests` files: hit and critical,
  damage, auto-attack proc, attack delay, resource, skill rules) unchanged; `ScriptHandlerOrderTests`, `ParityHandlerMappingTests`,
  `BootHandlerClassificationTests`, `PlayerSessionClassificationTests`, `ReloadStateCarrierTests`, `ReloadCarryHistoryTests`, `ReloadDigestTests`,
  `ClockSeamSourceGuardTests`, `LazyDependencyCycleTests`, `ConvertedServiceRulesTests`, `PlayerMessageAuditTests`.
- **New tests:** an order characterization for each orchestrator (the 24 activation steps, the damage dealt steps, the impact steps) that records the
  call and the random draws against the old text and the new; `CombatStateOwnerTests` (every table-holding part is registered, in the old order,
  an empty list throws, and the cleanup equals the old sequence); the three listener-empty tests; `CombatRecordSerializerTests` for the four write
  methods; `CombatDefenseMemoryServiceTests` (window and purge behaviour copied from the existing damage taken tests).
- **Unit filters** per PR, by service: `FullyQualifiedName~CombatHitEffects|CombatDamageTaken|CombatAbilityImpact|CombatRecorder|CombatDamageModifiers|CombatAbilityActivation|CombatAbilityBonus|CombatAbilityHit|CombatService|EnmityService|DeathService`
  (the one the PR moves), with `CombatPartForwarding`, `CombatSource` and `CombatFacadeAllocation`; `Phase5DebtTests`, `DomainMapTests` and the
  rest of `SWLOR.Architecture.Tests` with the baselines regenerated each PR.
- **Engine tests** (filter by area, host entry): `CombatRecorderEngineTests` (recorder), `TankEnmityEngineTests` and `DMLimboEnmityEngineTests` (Enmity),
  `DeathRespawnPointEngineTests` (Death), `MarkedForDeathEngineTests`, and `Characterization/` scenarios for hit, damage and ability impact; the 326
  override sites in engine tests exercise `CombatAbilityHitService`.
- **Coverage.** `Domain/Combat` is at 96 in `CoverageFloors.json` (overall 73). Each new part ships with its tests in its own PR and the gate runs with
  coverage; the moved code keeps its existing tests, so the floor holds. Native reads (`CombatRecorderEffectHooks`) run only in the engine.
- **Client evidence:** none. The split changes no in-game surface (code moves only); the engine runs and the characterization and parity gates are the
  evidence, as for P5-5.

**Baselines each PR changes** (regenerate with `SWLOR_UPDATE_SNAPSHOTS=1`, review the diff): `ServicesPerType.txt` (the seven lines leave as each service
reaches 10: HitEffects 20, DamageTaken 20, Impact 16, Activation 14, AbilityHit 12, `CombatService` 12, Enmity 12), `LargeTypes.txt` (eight lines:
Impact 1,720, HitEffects 1,716, Recorder 1,691, DamageTaken 1,649, DamageModifiers 1,436, Enmity 1,244, Activation 1,203, Bonus 1,061),
`LongMethods.txt` (`CombatRecordSerializer.Write`), `DeferredResolution.txt` (the three `Combat` lines, plus Enmity's and Stealth's in the Enmity PR),
`UpwardEdges.txt` (Quests 3, Social 3 + 3 + 3, World 3), `LogicToContent.txt` (lines re-keyed to the part files), `DebtReport.md`;
`ConvertedServices.json`, `DomainMap.json`, `RemovedFacades.json` (the facade tool's names per owner), `ServiceRegistrations.cs`,
`GameServices.Combat.cs`, `HandlerRenames.txt`, `ScriptHandlerOrder.txt`, `ClockSeamFiles.txt` (eight entries, plus each new part that reads the
clock), `PlayerMessageAudit.json` (18 rows keyed by these files: DamageModifiers 6, DamageTaken 4, Death 4, HitEffects 2, Hit 1, Impact 1).
`CrossDomainEdges.txt` and `EdgesIntoCoreAndPlatform.txt` change only in member counts; revert them as P5-5g did.

## Order

Bottom-up where a part needs a lower one, otherwise independent. A moved part calls the members still on its old service through the old
interface, promoted from private to public for that PR; the next PR moves them. Moved lines are member lines (about 90% of the part sizes);
callers are rewritten by `Apply-FacadeRemoval.ps1` (receiver name only), and the 15 or so instance call sites in holders by hand.

| PR | Contents | Moved lines | Files touched (beyond tests and baselines) | Needs |
|---|---|---:|---|---|
| P5-7a | This guide | 0 | the guide, README, Phase 5 link | - |
| P5-7b | Test groundwork (above); no production code | 0 | `Tests/Support/*`, source-reading tests, `CombatParts.cs`, the 11 `*Tests` doubles | P5-7a |
| P5-7c | Leaves and cuts: `CombatDamageRollService` (from `CombatService`), `CombatDefenseMemoryService` with the two `Lazy<>` (damage modifiers, damage taken) gone, the serializer `Write` cut | ~90 moved, ~200 re-cut | `CombatService`, `CombatDamageTakenService`, `CombatDamageModifiersService`, `CombatRecordSerializer`, 10 content and 2 native call sites of `Combat.CalculateDamage`, `ServiceRegistrations`, `GameServices.Combat.cs` | P5-7b |
| P5-7d | `CombatCriticalModifiersService`, `CombatRepeatedTargetService` | ~600 | `CombatDamageModifiersService`, 8 holders, 40 call sites | P5-16h PRs 7 and 8; P5-7c |
| P5-7e1 | `CombatDamageTakenEffectsService` (handler renamed), `CombatDamageReflectionService` | ~680 | `CombatDamageTakenService`, `ScriptHandlerOrder.txt`, `HandlerRenames.txt`, `CombatDeflectionReactionService`, `CombatAbilityActivationService`, 60 call sites | P5-7c; P5-16f first (recommended) |
| P5-7e2 | `CombatGuardedHitService`, `CombatGuardedHitEffectsService` (with `ApplyGuardedHitNextAttackEffects` from `CombatAbilityHitService`), `CombatGuardRetaliationService`; the damage taken stay-part is now the modifiers only | ~380 | `CombatDamageTakenService`, `CombatAbilityHitService`, 12 call sites | P5-7e1 |
| P5-7f1 | `CombatDealtEffectsService`, `CombatTriggeredDamageService` with the orchestrator cuts; `Lazy<ICombatDamageTakenService>` plain; `IBeastDamageRecording` and `IPartyRoster` swaps | ~510 | `CombatHitEffectsService`, `CapstoneProofService`, 25 call sites (13 domain) | P5-7e2 |
| P5-7f2 | `CombatAutoAttackEffectsService`, `CombatCriticalHitEffectsService`, `CombatDefeatedEnemyEffectsService` | ~845 | `CombatHitEffectsService`, `CombatService`, 35 call sites | P5-7f1 |
| P5-7g | `CombatQueuedAbilityBonusService`, `CombatAbilityHitAdjustmentService` | ~650 | `CombatAbilityBonusService`, `CombatAbilityHitService`, 70 call sites (22 content) | P5-7e2 |
| P5-7h1 | `CombatAbilityDamageBonusService`, `CombatAbilityHitRidersService`, `CombatAbilityAreaImpactService` | ~710 | `CombatAbilityImpactService`, 25 call sites | P5-7f2 |
| P5-7h2 | `CombatAbilityWeaponRidersService`, `CombatAbilityStatusAppliedService` (listener registration, `IPartyRoster`) | ~756 | `CombatAbilityImpactService`, handler lines, `BootHandlerClassification.txt` | P5-7h1 |
| P5-7i1 | `CombatAbilityDefenseEffectsService`, `CombatAbilityCostEffectsService` | ~520 | `CombatAbilityActivationService`, 6 call sites | P5-7e1 |
| P5-7i2 | `CombatAbilityOffenseEffectsService`, `CombatAbilityWeaponActivationService` | ~530 | `CombatAbilityActivationService` | P5-7i1 |
| P5-7j1 | `CombatRecordAbilityService`; `ICombatRecordingSessionListener` and its registration | ~230 + 40 | `CombatRecorderService`, `CombatParts.cs`, 20 call sites | P5-7b |
| P5-7j2 | `CombatRecordDamageService` (handlers renamed) | ~640 | `CombatRecorderService`, handler lines, 46 call sites | P5-7j1 |
| P5-7k | Enmity: `EnmityChangeService`, `EnmityAttackService`; `ILeashRetargetListener`, `IStealthRetargetListener`, `IHologramQuery`; two `Lazy<>` go | ~740 | `EnmityService`, `StealthService`, `AIService` (the one call and the list), `HoloComService`, 10 handler lines, 300 call sites (about 250 of them tests) | P5-8e, P5-8's leash part, P5-7f2, P5-7h2 |
| P5-7l | `DeathRespawnService`; `CombatService` takes the state owner list; close-out: README, Status, the guide's Progress | ~140 + 80 | `DeathService`, `CombatService`, every part (one interface), `ServiceRegistrations`, `PlayerSessionClassification.txt` | all |

P5-7 ends with the 11 services at or under 10 services and 1,000 lines, no method over 150 lines, three fewer `Lazy<>` in Combat, the `Lazy<IEnmityService>` of `AIService` and
`StealthService` gone, and the Combat -> Social, Quests and World edges named above closed. 16 pull requests after this guide; about 7,800 lines
move in total. Each PR is one branch from the refactor branch's tip, gets a Codex review of the current head, and runs the full gate with
coverage. Parallelism (rule 4: no two items on the same files): c and j1 can run together; d, e1, g, i1 after c; h and f follow the
chain above. At most three agents.

## Conflicts

- **P5-8e (`AIService`, `CompanionControlService`, the Combat ports).** It edits `EnmityService.cs` (parameter types, six leash call sites) and
  `StealthService.cs`. P5-7k waits for it (the Creatures guide's D3 already says P5-8e runs before P5-7 starts on Enmity). P5-7a to P5-7j
  do not touch either file. P5-7k edits `AIService.cs` for D1 (about 15 lines: replace one call with the notice list), so it also waits
  until no P5-8 PR is open on `AIService.cs`.
- **P5-10 (`PerkService`, `SkillService`, `MimicryService`, `DroidService`, `BeastMasteryService`).** No P5-7 PR edits these files: `RefreshStatDrivenTrackerEffects`
  stays on `ICombatService`, and `BeastMasteryService` and `DroidService` call only `BeginSetupAttribution` (stay-part). `MimicryService` holds a `Lazy<>`
  for `ICombatPointService`, which this split does not touch.
- **P5-16f (`WeaponActiveAbilityDefinitionBase`).** Its parts call `CombatAbilityActivation`, `CombatAbilityBonus`, `CombatAbilityHit`, `CombatDamageTaken`,
  `CombatRecorder` and `Enmity` through `GameServices`. P5-7e1 and later change receiver names there. Rule 4 keeps hand edits out of P5-16 types, but the
  facade tool already rewrote this file in P5-5f and P5-5g (22 -> 24 lookups), so a scripted receiver rewrite is the accepted route. Recommendation:
  P5-16f lands first (its code starts now that P5-5h is merged), so P5-7 rewrites the new parts; if P5-16f slips, P5-7 rewrites the base and P5-16f
  re-measures. Either way the lookup counts of that type grow by the number of new parts it names (recorded in the PR).
- **P5-16h PRs 7 and 8 (the character sheet).** `CharacterSheetViewModel` reaches `CombatAbilityBonus`, `CombatAbilityHit`, `CombatAttackTiming`,
  `CombatDamageModifiers`, `CombatDamageTaken` and `CombatResources`; the view model guide says both PRs merge before P5-7 starts. They must be
  merged before P5-7d (the first PR that changes a receiver the sheet uses); P5-7b and P5-7c touch no sheet file. Today the sheet's
  `CharacterSheetDamageEstimate`, `SurvivalRows` and `AccuracyRows` parts are the files P5-7 would edit.
- **P5-2 (follows P5-7).** It removes the `LogicToContent.txt` lines this guide leaves (counts in "What the code is"), now keyed to the part files.
- **P5-16b (hook logic out of `NativeSinks`).** The homes after P5-7: the attack roll reads `CombatAbilityHitService` and its adjustment part, and
  the stat adjustments in `CombatDamageModifiersService` and `CombatCriticalModifiersService`; the damage roll reads `CombatDamageModifiersService`,
  `CombatDamageTakenService` (pre-damage), `CombatDamageTakenEffectsService`, `CombatDamageReflectionService` and the hit effects parts; the
  deflection hooks read the damage taken effects part. About 60 call sites in `Native/` and `NativeSinks/` change receivers by the tool.
- **Domain order.** All new parts are Combat. The edges they add point down (Character, Foundation) or to ports Combat owns.

**Master.** Master still holds this logic in `Service/Combat.cs` (13,240 lines), `Service/CombatRecorder.cs` (1,641), `Service/Enmity.cs` (1,142) and
`Service/Death.cs` (340). Commits to them since 2026-10-01 (`origin/master` at `ca97d5ddc`): `Combat.cs` 75, `CombatRecorder.cs` 25, `Enmity.cs` 11, `Death.cs` 13;
112 distinct commits in all. They came in a burst: 99 in the weeks of 5 to 26 October, 12 in November before the 10th, 1 later (one commit not yet
on the branch: `cb465ec15`, "Show the respawn panel after a lethal PvP death", on `Death.cs`). Master has not touched `Combat.cs` since 2026-11-10. Tests
that read these files by path on master: 27 for `Combat.cs`, 5 for `Enmity.cs`, 5 for `Death.cs`, 1 for `CombatRecorder.cs`. The per-service share of master's
commits cannot be read from the diffs (master's hunks name call sites in other files), so only the totals are given.
For comparison the StatusEffect freeze covered one file with 27 commits since 2026-10-10. Recommendation (open question 1): a short announced freeze like
P5-3 to P5-5, scoped to those four master files and the 38 tests above, from P5-7d to P5-7l, because every master edit to a moved range must be
ported by hand to whichever part now owns it, and the ranges change with each PR. If the owner declines, each sync ports by hand with a Port log row,
about 1 to 2 commits a week at the November rate and many more if October's tuning pass repeats.

## Open questions

1. **Master freeze for P5-7d to P5-7l?** Recommend yes, scoped to the four files, as above. The window is about nine PRs. It is the owner's call
   because it holds combat balance edits on master.
2. **Enmity part names and which part keeps `IEnmityService`.** Recommend `EnmityService` stays the table part (carried keys unchanged, no
   `[ReloadCarriedFrom]`, the AI core's reads stay), and the writes (`ModifyEnmity`, 138 sites) move to `EnmityChangeService`, which changes about 250
   mostly test call sites. The alternative (writes stay, tables move) costs about the same but moves the carried keys.
3. **The Stealth notice.** Recommend a separate `IStealthRetargetListener` in P5-7k (above) rather than widening P5-8e's
   `IStealthAggroListener`, so P5-8e is not reopened. If P5-8e has not landed when P5-7k starts, P5-7k adds both notices.
4. **The recorder's two `Lazy<>`.** Recommend leaving them to P5-15 (they need a Foundation-owned reader for FP, stamina and the weapon skill, which
   is a design decision of its own).
5. **Cleanup by owner list.** Recommend `IEnumerable<ICombatCreatureStateOwner>` for `CombatService` over a larger direct list, with the old order kept in
   `ServiceRegistrations` and proved order-independent by the characterization test. If the owner wants the exact order forever, the owner interface
   takes a phase number and the list is walked twice.

## Progress

P5-7a: this guide (2026-11-18). Next: P5-7b (test groundwork), after P5-7a is reviewed.
