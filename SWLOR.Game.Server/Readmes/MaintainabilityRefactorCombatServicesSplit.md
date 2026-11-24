# Maintainability Refactor: Combat Services Split (P5-7 part map)

The plan for splitting the Combat services that are over the Phase 5 limits: `CombatHitEffectsService`, `CombatDamageTakenService`,
`CombatAbilityImpactService`, `CombatRecorderService` (with `CombatRecordSerializer.Write`), `CombatDamageModifiersService`,
`CombatAbilityActivationService`, `CombatAbilityBonusService`, `CombatAbilityHitService`, `CombatService`, `EnmityService` and
`DeathService`. Status lives in [MaintainabilityRefactorStatus.md](MaintainabilityRefactorStatus.md) (row P5-7). Targets and rules:
[MaintainabilityRefactorPhase5.md](MaintainabilityRefactorPhase5.md) (target 2: at most 10 services per type and under 1,000 lines;
target 3: no method over 150 lines; target 4: no deferred resolution) and
[MaintainabilityRefactorDomainOrder.md](MaintainabilityRefactorDomainOrder.md). The model is the
[StatusEffect split](MaintainabilityRefactorStatusEffectSplit.md), with the [Stat split](MaintainabilityRefactorStatSplit.md), the
[Ability split](MaintainabilityRefactorAbilitySplit.md) and the [Creatures split](MaintainabilityRefactorCreaturesSplit.md) (D1, D3 and the
P5-8e ports) beside it. This file goes away at cutover. The older [Combat split](MaintainabilityRefactorCombatSplit.md) is the P2 plan that
converted the static `Combat` class; it is finished and this file does not replace it.

Measured at `c2a30aae0` (`feature/maintainability-refactor`). Line counts are `wc -l` of the files; member line counts come from a scan
of member declarations (a member starts at its doc comment and attributes). A part's size adds the usings, class header, constructor and
promoted members to its member lines, so part sizes and service counts are estimates (about 10%); each PR re-measures. Service counts come
from a scan of which injected fields each moved member uses, plus the parts it calls. Call counts are regex counts of the receiver names
(`_damageTaken.X`, `CombatDamageTaken.X` and the like) over server, content, tests and engine tests, so they are approximate. Holder counts
(constructor services, `Lazy<>` included) come from a scan of every constructor in `ServiceRegistrations`; a scan can count one more than the
guard (for example when a constructor also takes an observer list). Master facts come from the local `origin/master` ref (`ca97d5ddc`,
2026-11-17); no `git fetch` was run, so master may have moved. Everything here is the graph at `c2a30aae0`; P5-7k re-scans the real graph after P5-8.

## Decisions

Proposed for the coordinator's review; the coordinator's review decisions are folded in. Change only on a finding in review.

- **Split by what each job does, layered by who calls what, with a stay-part per service.** The stay-part keeps the original class and
  interface name and the most-called members, so the fewest callers change. The other parts are new services behind new interfaces.
  Reject chunks: every part below is one responsibility (the effects of a critical hit, the guard memory, the retaliation damage).
- **No part is over a limit.** Every part is at or under 10 services and 1,000 lines, and every method is at or under 150 lines (the one
  method over, `CombatRecordSerializer.Write`, is cut). No exception is asked for. Five parts sit at exactly 10 services and each has a
  fallback that lowers the count (Risks). The largest part is the recorder's stay-part at about 830 lines.
- **Moved code stays verbatim** except for how it reaches its state and dependencies. The text changes are listed per part: the orchestrator
  cuts, the Start and Stop cuts of the recorder, promoted private members, the one reorder in `ModifyEnmity`, and the receivers of calls into
  other parts.
- **Pure code moves down, queries a lower domain cannot name use ports Combat owns, notices use listeners.** Combat's ports already
  exist where P5-3c and P5-15a made them (`IPartyRoster`, `IAreaLookup`, `ICityUpgradeLookup`). This split adds two one-member ports for
  the remaining upward edges (below) and the listener lists in "Cycles".
- **No `Lazy<>`, `Func<>` or late lookup is added.** Three `Lazy<>` parameters inside Combat go (DamageModifiers, DamageTaken, HitEffects).
- **Orchestrators call their steps in the original order, one public member per step.** Where one method runs a fixed sequence of effects
  that now live in several parts (`ApplyDamageDealtEffects`, `ApplyAbilityActivatedEffects`, `ApplySuccessfulAbilityImpactRiders`), the
  stay-part keeps the method and the order, and every step it calls becomes a public member of its part with the name it has today (the
  24 steps of `ApplyAbilityActivatedEffects` are 24 public members; there are no grouped runs). Random draws and recorder events keep their
  order. The ordered transcripts of P5-7b pin it (Tests).
- **Carried keys stay where they can.** `EnmityService` keeps its three carried tables under the same key. A part never caches a carried
  dictionary: adoption replaces the field, so every access goes through the owner's members. Nothing else here is carried.
- **Content references are not removed here.** They move with the code and stay behind their existing seams; P5-2 removes them (the
  `LogicToContent.txt` lines are counted in "What the code is"). The "effect keys for Combat" of the StatusEffect guide (settled
  decision 3) are therefore P5-2's, not this item's: P5-7 moves the `typeof(...StatusEffect)` text unchanged.
- **The accepted service growth from P5-5c to P5-5h is removed here.** `CombatHitEffectsService` 20 -> 8, `CombatDamageTakenService` 20 -> 8,
  `CombatAbilityImpactService` 16 -> 10, `CombatAbilityActivationService` 14 -> 7. `DeathService` stays at 10 by the guard (11
  constructor parameters with the listener list); it is one of the five parts at 10 and is flagged.

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
- `CombatAbilityImpactService` is the only one of the eleven that owns a `Rerun` boot handler today.

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
every `Domain/Combat/Combat*Service.cs` except the names in `CombatParts.OtherServices`
(`SWLOR.Game.Server.EngineTests/Framework/CombatParts.cs`) and strips the receiver of a call on an injected `ICombat*Service` field. New
parts named `Combat*Service` are therefore found with no change, except the recorder parts (below), which join `OtherServices`. Tests
that name a file by path: `EnmityService.cs` (7: `AIModelTests`, `EnmityAdjustmentTests`, `EnmityTests`, `EspionageSystemTests`,
`NPCChaseTests`, `RealEnmityTables`, `ReloadStateCarrierTests`), `DeathService.cs` (5: `AuthoredCombatAnimationTests`,
`ForceLightGuardianTests`, `PerformanceAudienceCostTests`, `PerformanceAudienceStateTests`, `StatusEffectDeliveryTests`),
`CombatRecorderService.cs` (3: `CombatParts`, `CombatSourceTests`, `DMPossessionPlayerLoopTests`), `CombatDamageModifiersService.cs`
(`MeleeRepeatedTargetStackTests`), and the snapshot and baseline files in "Baselines".

## Parts

Folder: all parts go in `Domain/Combat/` (the flat layout every Combat service uses) in namespace `SWLOR.Game.Server.Domain.Combat`, domain
Combat. New names are listed in `ConvertedServices.json` and `DomainMap.json` (Combat `services`) in the PR that adds them. Each new part
has its own interface and a `GameServices` property (derived from the interface name). Line counts are estimates of the whole class.

### CombatHitEffectsService (1,716 lines, 20 services) -> six parts

What the file does: it applies what a landed hit sets off for the attacker, in groups (the hit itself, the auto-attack riders, the
critical hit, the defeated enemy), plus the triggered-damage pipeline. The recursion `ApplyDamageDealtEffects` -> splash ->
`ApplyTriggeredDamage` -> `ApplyDamageDealtEffects` is not a runtime cycle (the splash runs in a `DelayCommand`, and a triggered hit never
reaches the splash because `deliveryType` is not `Direct`), but it is a static call cycle, so it is cut as the orchestrator cut says.

| Part | Responsibility | Members that move (by name) | Lines | Services | Callers that change |
|---|---|---|---:|---:|---|
| `CombatHitEffectsService` (stay) | The orchestrator of a direct hit: tracking call, gates, splash, the effect steps in order; the auto-attack cycle damage (it draws from the cycle tables) | stays: `ApplyDamageDealtEffects`, `ApplyAutoAttackSplashEffects`, `ApplyAutoAttackCycleDamage`, `ApplySourceStatusAutoAttackCycleDamage`, `ConsumeAutoAttackCycleDamageBonus`, `ConsumeMeleeAutoAttackCycleDamageBonus`, `ForgetHitEffectState` | ~300 | 8 | none (23 sites of `ApplyDamageDealtEffects`) |
| `CombatDealtEffectsService` | The steps a landed direct hit grants the attacker: the tracking block, stamina and HP restore, delay reduction, hamstring, force erosion, mimicry procs, predator's mark, toxic rush, bleed effects | `ApplyHeavyVibrobladeDefenseDamageRecovery`, `ApplyPredatorsMarkEffects`, `ApplyPredatorsMarkFollowUp`, `ApplyLowHPDamageDealtHPRestore`, `ApplyBleedingTargetStaminaRestore`(+`Channel`), `ApplyDamageDealtForceErosionEffect`, `ApplyDamageDealtHamstringEffect`, `ApplyDamageDealtMimicryTraitProcs`, `MimicryTraitProcs`, `ApplySideAttackDamageEffects`, `ApplyDamageDealtStaminaRestore`, `ApplyDamageDealtAttackDelayReduction`, `ApplyNextDamageDealtBleedEffect`, `ApplyFrenzySlashHasteRefresh`, `ApplyToxicRushDamageDealtEffects`, `HasPositiveDirectDamageAfterNativeMitigation`; new public members cut from `ApplyDamageDealtEffects` (the tracking block, the two HP-restore blocks) | ~500 | 9 | none (no external caller) |
| `CombatTriggeredDamageService` | Damage a trigger deals: outgoing modifiers, target adjustment, resistance, taken modifiers, queue or apply the effect | `ApplyTriggeredDamage` (+ `ApplyTriggeredDamageTargetAdjustment`) | ~110 | 8 | 23 sites (13 domain, 2 content, 5 tests, 3 engine) |
| `CombatAutoAttackEffectsService` | What an auto-attack adds: damage modifiers per attack, suppressed bonuses, master resource restore, hamstring, source status stacks, sundered-target FP restore | `ApplyAutoAttackDamageModifiers`, `ConsumeSuppressedAutoAttackDamageBonuses`, `ApplyAutoAttackMasterResourceRestore`, `ApplyAutoAttackHamstringEffect`, `ConsumeNextSkillAutoAttackDamageBonus`, `ApplySourceStatusStackEffects`, `ApplyInfectionStack`, `ApplyAutoAttackSunderedTargetFPRestore` | ~280 | 9 | 9 sites (2 native, 6 tests, 1 domain) |
| `CombatCriticalHitEffectsService` | What a critical hit sets off, and the non-critical ability riders | `ApplyCriticalHitEffects` (124 lines), `ApplyNonCriticalAbilityEffects`, the `ApplyCritical*` helpers, `CriticalHitSequenceState` and its table | ~465 | **10** | 14 sites (2 domain, 1 native, 5 tests, 6 engine) |
| `CombatDefeatedEnemyEffectsService` | What a kill sets off: spreads, nearby ally defense, cruel momentum, recent damagers | `ApplyDefeatedEnemyEffects`, `ApplyRecentDamagerDefeatedEnemyEffects`, `SpreadPoisonFromTarget` and their helpers | ~255 | 9 | 12 sites (3 domain, 3 tests, 6 engine) plus `CombatService` |

Cuts: (1) `ApplyDamageDealtEffects` keeps its text except that the tracking block (capstone proof, paired-ranged record, activity tracking)
and the two inline HP-restore blocks become calls to `CombatDealtEffectsService` (about 40 lines move; the statements and their order are
unchanged). (2) `ApplyTriggeredDamage` calls the tracking member of `CombatDealtEffectsService` where it called
`ApplyDamageDealtEffects(..., Triggered)`: for a non-direct delivery the original runs exactly that block and returns, so the call is
equal. That removes the static cycle. (3) The seam `ApplyDamageTakenModifiers` (the `Lazy<>` wrapper) is a plain call to
`CombatDamageTakenService` in the triggered part.

The critical part is at 10 services (abilityHit, attackTiming, impactState, queries, resources, stat, statusEffect, statusEffectApplication,
statusEffectDuration, temporaryStatModifier). The duration service is used only by `ApplyCriticalBleedingStatusDurationExtension`, but
`ApplyCriticalHitEffects` calls that member, so moving it to a neighbour swaps one service for one part and the count stays 10. The fallback
that lowers it moves the three members that use attackTiming and impactState (`ApplyCriticalNextAbilityDamageBonus`,
`ApplyCriticalNextAutoAttackNoDelay`, `ApplyCriticalHitLimitedHaste`, about 70 lines) to a new `CombatCriticalHitTimingEffectsService` (5
services: attackTiming, impactState, resources, stat, statusEffectApplication). The critical part then drops two services and gains one
part: 9.

### CombatDamageTakenService (1,649 lines, 20 services) -> seven parts and `CombatDamageRollService`

What the file does: the modifiers applied before damage lands (percent, leadership, redirect, share, fatal prevention), the effects
after damage (low-HP riders, melee poison, avoided attacks, deflections), the guard (reduction, retaliation, next-skill effects) and
the reflections. Pre-damage modifiers are the most called family (94 sites: `ApplyDamageTakenModifiers` 37, `ApplyDamageOverTimeTakenModifiers`
19), so they stay.

`ApplyIncomingPhysicalToForceConversion` cannot stay with `ApplyDamageTakenModifiers`. It calls `ApplyTriggeredDamage`, and the triggered part
calls `ApplyDamageTakenModifiers`, so one class holding both is a constructor cycle (Triggered -> stay -> Triggered). It goes with the
reflections. Its two callers, `AbilityDamageInputService` and `AbilityDamageStageService`, therefore take two damage taken parts (Holders).
`ApplyIncomingPhysicalToForceDefenseConversion` (stat only) stays.

| Part | Responsibility | Members that move | Lines | Services | Callers that change |
|---|---|---|---:|---:|---|
| `CombatDamageTakenService` (stay) | Damage taken modifiers: percent, typed leadership, redirect and share to a status source, damage over time, fatal prevention, the physical-to-force defense conversion, guard reduction reads | stays: `ApplyDamageTakenModifiers`, `ApplyDamageTakenPercentageModifiers`, the leadership members, `ApplyDamageTakenRedirectToStatusSource`, `ApplyDamageTakenShareToStatusSource`, `ApplyDamageOverTimeTakenModifiers`, `TryPreventFatalDamageAndGrantTemporaryHP`, `ApplyLowHPTemporaryHPBeforeFatalDamage`, `ApplyIncomingPhysicalToForceDefenseConversion`, `IsHostileAttackSource`, `GetGuardDamageReductionPercent`, constants | ~490 | 8 (7 plus the recorder damage part, for `KeepLethalHitPoints` and `RecordTemporaryHitPointsGranted`) | none |
| `CombatDefenseMemoryService` | Who was guarded or deflected recently: the two tables and their reads, no dependencies | `HasRecentGuardedHit`, `HasRecentDeflection`, new `NoteGuardedHit`, `NoteDeflection` (the table writes cut out of `TrackGuardedHit` and `TrackDeflection`), `ForgetDefenseMemory` | ~70 | 0 | 10 sites (4 content, 4 tests, 2 engine) |
| `CombatDamageTakenEffectsService` | The after-damage handler and reactions: low-HP effects, reversal cut, melee damage taken and poison, avoided attacks, attack deflection | `ApplyDamageTakenEffects` (the handler), `ApplyLowHP*`, `ApplyReversalCutReady`, `ApplyDamageTakenNextSkillAbilityDamage`, `ApplyRecentDamageTargetHitEffects`, `ApplyMeleeDamageTaken*`, `TrackAvoidedAttack` and `ApplyAvoided*`, `TrackDeflection`, `ApplyDeflectionNearbyAllyGuard`, `ApplyAbilityUsedAttackDeflection`, `ApplyAbilityGrantedAttackDeflectionEffects` | ~575 | **10** | 44 sites (6 content, 18 tests, 10 engine, 3 native, 7 domain) |
| `CombatGuardedHitService` | A guarded hit: reduction, recovery, enmity, then retaliation and next-skill effects | `ApplyGuardedHitModifiers`, `ApplyGuardedHitRecovery`, `ApplyGuardedHitEnmity` | ~110 | 9 | 3 sites |
| `CombatGuardedHitEffectsService` | The effects a guarded hit grants the guard | `TrackGuardedHit`, `ApplyGuardedHitNextSkillAbilityEffects`, `ApplyGuardedHitNextSkillAbilityStatusEffects`, `ConsumeGuardedHitNextSkillAbilityExposedDamageBonus`, and `ApplyGuardedHitNextAttackEffects` from `CombatAbilityHitService` (its only caller is `TrackGuardedHit`) | ~240 | 5 | 7 sites plus 2 |
| `CombatGuardRetaliationService` | The damage a guard returns | `ApplyGuardedHitRetaliation`, `ApplyGuardedHitRetaliationPulse`, `ResolveGuardRetaliationDamage`, `GetGuardRetaliationDamageAbility` | ~190 | 9 | none |
| `CombatDamageReflectionService` | Damage given back: reflection, force conversion, ranged deflection reflection | `ApplyDamageReflectionEffects`, `ApplyForceDamageTakenEffects`, `ApplyIncomingPhysicalToForceConversion`, `ApplyRangedDeflectionReflection`, `BuildDeflectingReturnCombatLogMessage` | ~220 | 8 | 17 sites (3 domain, 3 native, 11 tests) |
| `CombatDamageRollService` (from `CombatService`) | The random damage roll (`CalculateDamage`: the range from `CombatRules`, then `Random`) | `CalculateDamage` (31 lines, no services) | ~50 | 0 | 10 content, 2 native, 11 tests |

The effects part is at 10 services (attackTiming, combatActivity, recorder, resources, stat, statusEffect, statusEffectApplication,
temporaryStatModifier, triggered damage, defense memory). The fallback that lowers it moves `ApplyMeleeDamageTakenEffects` and
`ApplyMeleeDamageTakenPoisonDamage` (29 + 24 lines; recorder, triggered damage, resources, stat, temporaryStatModifier) to
`CombatDamageReflectionService`: nothing in the effects part calls them (they are called from the native hook), so the effects part drops
the triggered-damage service (9), and the reflection part gains temporaryStatModifier (9).

Handler: `ApplyDamageTakenEffects` moves to the effects part (`ScriptHandlerOrder.txt` line 344, `HandlerRenames.txt` line 146, renamed in place).

### CombatAbilityImpactService (1,720 lines, 16 services) -> six parts

What the file does: the effects of an ability hit (riders), the area impacts, the damage bonus reads, and the status-applied listener.

| Part | Responsibility | Members that move | Lines | Services | Callers that change |
|---|---|---|---:|---:|---|
| `CombatAbilityImpactService` (stay) | The orchestrator of a successful ability hit: the step order, status riders dispatch, and the attacking-ally check that `AbilityHostileImpactService` calls | stays: `ApplySuccessfulAbilityImpactRiders`, `ApplyAbilityStatusRiders`, `IsTargetAttackingAllyForStatusAppliedRiders`, `IsAttackingAllyOf` | ~200 | **10** | 6 sites (4 tests, 2 domain) |
| `CombatAbilityDamageBonusService` | Damage bonus reads before the hit | `GetAbilityImpactBaseDamageBonus`, `GetHighResourceAbilityDamageBonus`, `GetCostlyAbilityDamageBonus` | ~145 | 7 | 16 sites (2 domain, 9 tests, 5 engine) |
| `CombatAbilityHitRidersService` | Hit-time effects of a hostile ability: costly, near-target, same-target, next auto-attack delay, and the impact effects entry with its area, spear, throwing and reward helpers | `ApplyCostlyAbilityHitEffects`, `ApplyRangedAbilityHitNearTargetEffects`, `ApplySameTargetHostileAbilityHitEffects`, `ApplyHostileAbilityHitNextAutoAttackNoDelay`, `ApplyAbilityImpactEffects`, `ApplyAreaAbilityImpactEffects`, `ApplySpearAbilityImpactEffects`, `ApplyThrowingAreaAbilityImpactEffects`, `ApplyAreaHitRewards`, `ApplyAreaHitHaste`, `ApplyStackingAttackBoost` and the same-target table | ~380 | 6 | 12 sites (2 domain, 2 content, 3 tests, 5 engine) |
| `CombatAbilityAreaImpactService` | Area abilities and the damage riders: pulse, fragmentation, ricochet, cluster storm, splash, saturation toss, bleed refresh and spread, the area hit sequence | `ApplyAbilityDamageRiders`, `ApplyFoggyMindResourceDrain`, `ApplyAreaAbilityPulse`, `ApplyAreaAbilityFragmentation`, `ApplyRicochetDamage`, `ApplyClusterStormDamage`, `ApplyBleedingTargetAbilitySplash`, `ApplySaturationToss`, `ApplyBleedingTargetAbilityBleedRefresh`, `ApplyBleedingTargetAbilityBleedSpread`, `ApplyAreaAbilityTargetHitSequenceEffects` and the sequence table | ~410 | 9 | none |
| `CombatAbilityWeaponRidersService` | Weapon-family impact riders (heavy vibroblade, force dark, lightsaber, pistol, rifle, saberstaff, spear, staff, throwing, twin blade, vibroknife, katar venom) | the `Apply*ImpactRiders` family, `ApplyAbilityUsedPerkCategoryTargetEnmityToSourceStatus` (922; it uses enmity, which only this part has), `HasSunderPenaltyAtLeast`, `ApplySpearDisablerSuppressionRiders`, `ApplyGuardedHitNextSkillAbilityExposedStatus` | ~440 | 8 | 1 site |
| `CombatAbilityStatusAppliedService` | The status-applied listener, target status effects and the hostile-debuff party buff | `RegisterStatusEffectListener` (the boot handler), `OnStatusApplied`, `OnHostileDebuffApplied`, `OnApplicationCompleted`, `ApplyHostileAbilityPartyBuff`, `ApplyStatusApplied*`, `ApplyAbilityTargetStatusEffects`, `ReplaceTemporaryStat` (with its callers) | ~320 | 8 | 6 sites (5 tests, 1 content) |

The stay-part is at 10 services (combatActivity, enmity, hitEffects, pairedRangedTraits, party, stat and the four parts). The fallback that
lowers it moves `IsTargetAttackingAllyForStatusAppliedRiders` and `IsAttackingAllyOf` (29 lines; enmity, party, stat) to
`CombatAbilityWeaponRidersService` (which already has enmity and stat and gains party: 9). The stay-part is then 7. The cost is that
`AbilityHostileImpactService` takes the weapon riders part for that one call (9 -> 10, Holders). `IsAttackingAllyOf` is not a candidate for
the hit-riders part.

`RegisterStatusEffectListener` keeps its event and position; it is renamed in place (`ScriptHandlerOrder.txt` line 58, `HandlerRenames.txt`
line 320 with the branch name, `BootHandlerClassification.txt`).

### CombatRecorderService (1,691 lines, 6 services) -> four parts, bottom-up; `CombatRecordSerializer.Write` cut

What the file does: the recording session (start, stop, status, snapshots, the event queue and participant ids), the attribution stack, and
47 `Record*`, `Begin*` and `Expect*` event methods. Lower domains reach it through `ICombatRecordingState`, `ICombatRecording` and
`ICharacterRecording` (21 holders); the stay-part keeps implementing those, and `ICombatRecorderService` and `IShutdownStep`. The members
those interfaces name (attribution, healing reaction, resource and item records, status records) stay.

The stay-part and a damage part cannot be split by member ranges alone: they share state and private members (about 98 calls from the
damage and ability members into 14 private members of the stay-part, among them `Enqueue` 18, `GetParticipantId` 29, `IsRecorded` 11,
`IsInRecordedArea` 10, `CurrentFrame` 6 and the attribution resolvers). The structure is therefore layered from the bottom:

| Layer | Part | Responsibility | Members | Lines | Services |
|---|---|---|---|---:|---:|
| 0 | `CombatRecordSessionCore` | State and queue, no services: event queue, held events, the damage and heal application scopes, participants, recorded area, pending damage table, `IsRecording`, the clock and log seams | `Enqueue`, `Write`, `WriteHeldEvents`, `GetParticipantId`, `IsRecorded`, `IsInRecordedArea`, `AnyInRecordedArea`, `WithTargetLocation`, `BeginDamageApplication`, `EndDamageApplication`, `BeginHealApplication`, `EndHealApplication`, `FindDamageApplication`, `KeepHitPointsLeftByHit`, `ReleaseAwaitingHeal`, `AddPendingDamage`, `CurrentFrame`, `UnixTimeMilliseconds`, `UtcNow`, `WriteLog`, `LastWriter`, new `Reset` and `Flush` (the clears and the held-event flush cut out of `Start` and `Stop`) | ~340 | 0 |
| 1 | `CombatRecorderService` (stay) | Session control (`Start`, `Stop`, `GetStatus`, `StopOnShutdown`, snapshots, `RegisterPlayersInArea`), the attribution stack and the resolvers, the healing reaction, the resource, item and status records, the pending-damage entry members and the three small entry members lower domains call | stays: `Start`, `Stop`, `GetStatus`, `StopOnShutdown`, `TakeSnapshots`, every `Begin*Attribution`, `Capture*`, `Push*`, `Resolve*`, `GetFeatCode`, `BeginHealingReaction`, `EndHealingReaction`, `RecordResourceChange`, `RecordResourceReduction`, `RecordItem*`, `RecordStatusApplied`, `RecordStatusRemoved`, `ExpectDeferredDamage`, `ExpectHitPointCost`, `ExpectWeaponDamage`, `HasPendingDamage`, `RecordPvpHealingReduced`, `RecordAttack`, `RecordRecurringAbilityImpact`; `IsRecording` is one line that returns the core's | ~830 | 7 (core, dmPresence, db, catalog, impactState, `Lazy<IStatService>`, `Lazy<ICombatQueriesService>`) |
| 2 | `CombatRecordAbilityService` | Ability records: cast, denied, interrupted, queued, impact, cost, toggle notes | `RecordAbilityCast`, `RecordAbilityDenied`, `RecordAbilityInterrupted`, `RecordAbilityQueued`, `RecordAbilityImpact`, `BeginAbilityCost`, `CompleteAbilityCost`, `NoteToggleDeactivated`, `TakeToggleDeactivation` | ~230 | 3 (core, the stay-part, `IStatService` plain) |
| 2 | `CombatRecordDamageService` | Damage, healing, temporary hit point and death records, with the death handlers | `RecordDamageApplied`, `RecordDamageEvents`, `ResolveDamageAttribution`, `RecordHealingApplied`, the temporary hit point records, `RecordCreatureDeath` and `RecordPlayerDeath` (handlers), `RecordRespawn`, `RecordResurrection`, `RecordSubdual`, `KeepLethalHitPoints`, `RecordDeath` and its helpers, and the tables `_temporaryHitPointGrants`, `_lastDamageSources`, `_deathFrames` | ~480 | 3 (core, the stay-part, impactState) |

- **`Enqueue` and the other core members are not on any interface the 21 holders see.** The core has its own interface
  `ICombatRecordSession`, held only by the stay-part and the two layer 2 parts. `ICombatRecordingState` (held by Platform) is registered to the
  core instance.
- **The ability part needs `IStatService` plain.** It reads FP and stamina (`GetCurrentFP`, `GetCurrentStamina`) for the ability cost records.
  Nothing it reaches depends back on it (`StatService` holds the stay-part, never the ability part), so plain is safe; the build scan
  confirms it. The stay-part keeps its `Lazy<IStatService>` and `Lazy<ICombatQueriesService>` (Cycles).
- **The one forwarding member** is the stay-part's `IsRecording` (`ICombatRecording` inherits it, so the stay-part must implement it). It is a
  one-line getter over the core's field, on a hot path (88 call sites), and `CombatLoad` and the allocation pins measure it.
- **`ExpectHitPointCost`, `RecordPvpHealingReduced`, `RecordAttack` and `RecordRecurringAbilityImpact` stay in the stay-part** (with the
  `Expect*` family and `HasPendingDamage`, which use the core's pending table). They are the members `AbilityDarkForceService`,
  `PvpCombatService`, `AbilityImpactMessageService` and `AbilityImpactLifecycleService` call, so those four holders keep one recorder service.
- **Start and Stop cut.** `Start` (75 lines) and `Stop` (36) keep their text except that the clears of the core's tables become `Reset()` and
  `Flush()` calls on the core, and the clears of the damage part's three tables become a notice (below). The statement order is unchanged.
- **Notice.** The stay-part holds a list of `ICombatRecordingSessionListener` (`OnRecordingStarted`, `OnRecordingStopped`). `OnRecordingStarted`
  fires where `Start` clears its tables (after `_recording++`, before the writer is created and before `TakeSnapshots` reaches
  `RegisterPlayersInArea`); `OnRecordingStopped` fires where `Stop` clears them (after `IsRecording = false` and the message, as today). The
  damage part registers from a `Rerun` boot handler in `OnModuleCacheBefore`; a notice with an empty list throws `InvalidOperationException`
  (the P5-5d pattern). The ability part's toggle notes are not cleared by `Start` or `Stop` today, so it registers nothing.
- **Callers that change:** core 17 sites (4 native, 13 tests: the scope members), ability part 16 (15 content, 1 domain), damage part 17 (12
  content, 3 domain, 2 native). The two layer 2 parts join `CombatParts.OtherServices` so `CombatSource` does not absorb them. The stay-part
  ends near 830 lines (83%); if it measures over about 950, the resource, item and status records move to a new layer 2 part.

`CombatRecordSerializer.Write` (198 lines): one `switch` over 23 case labels. It becomes a dispatcher of about 30 lines that calls four
static methods, each holding the original `case` bodies verbatim: `WriteLifecycleEvent` (started, stopped, participant), `WriteAbilityEvent`
(cast, queued, denied, interrupted, impact, cost), `WriteDamageEvent` (attack, damage, temporary hit points, healing) and `WriteStateEvent`
(resource, status applied, refreshed and removed, death, resurrection, subdual, snapshot); each stays under 70 lines. One golden JSON line
per label proves it (Tests).

### CombatDamageModifiersService (1,436 lines, 9 services) -> three parts

Regrouped so the two common holders need one part: the critical damage side stays with `ApplyDamageDealtModifiers` (`AbilityDamageStageService`
calls `ApplyDamageDealtModifiers`, `ApplyCriticalDamageModifier` and `CalculateDamageWithCriticalMitigation`), and the rate reads that the
character sheet and `AbilityDamageInputService` call (`GetSkillCriticalRatePercentAdjustment`, `GetLowHPAttackAdjustment`,
`GetNearbyStatusTargetAttackAdjustment`, `ApplyStatusSourceDefenseModifiers`) stay too.

| Part | Responsibility | Members that move | Lines | Services | Callers that change |
|---|---|---|---:|---:|---|
| `CombatDamageModifiersService` (stay) | Damage dealt modifiers: the orchestrator, critical damage, ability shape, status source, attack adjustments | stays: `ApplyDamageDealtModifiers`, `ApplyOrdinaryAttackDamageMultiplier`, `CalculateDamageWithCriticalMitigation`, `TryUseIncomingCriticalHitDowngrade`, `ApplyCriticalDamageModifier`, `GetSkillCriticalRatePercentAdjustment`, the `Apply*ShapeDamageModifier` family, `ApplyTargetStatusDamageModifiers` (138 lines), `ApplyDamageTypeDealtModifiers`, `GetLow*AttackAdjustment`, `GetNearbyStatusTargetAttackAdjustment`, `ApplyStatusSource*Modifiers`, `ApplyTargetStatusAttackModifiers`, `ApplyAreaAbilityAfterDeflectionDamageModifier` | ~840 | **10** | none |
| `CombatRateModifiersService` | Critical rate and positional rate modifiers: auto-attack and status critical rate, the auto-attack cycle critical tracker, side and back attack | `GetAutoAttackCriticalRateAdjustment`, `GetTargetStatusCriticalRateAdjustment`, `PrepareAutoAttackCycleCriticalRate`, `ClearAutoAttackCycleCriticalRateTracker` and its table, `ApplySideAttack*`, `GetSideAttack*Adjustment`, `ApplyBackAttack*`, `GetBackAttackCriticalRateAdjustment` | ~210 | 6 | 25 sites (6 native, 13 tests, 6 domain) |
| `CombatRepeatedTargetService` | Repeated-target and same-target pressure state and effects | `ApplyRepeatedTargetDamageModifier`, the melee and ranged variants, `ClearRangedRepeatedTargetDamageTracker`, `ApplySameTargetPressureDamageEffects`, the `SameTargetPressure*` members, the four state tables, `ForgetDamageModifierState` | ~410 | 3 | 10 sites (5 domain, 5 tests) |

The stay-part's `Lazy<ICombatDamageTakenService>` (only for `HasRecentDeflection`) becomes a plain `ICombatDefenseMemoryService` parameter. The
stay-part is at 10 services (damage memory, impactState, playerFeedback, queries, resources, stat, statusEffect, temporaryStatModifier and
the two parts). The fallback that lowers it moves `GetHighHPTargetCriticalDamageAdjustment` (24 lines; the only user of `IPlayerFeedbackService`
in the stay-part) to `CombatRateModifiersService`, which already holds playerFeedback: the stay-part drops it (9).

### CombatAbilityActivationService (1,203 lines, 14 services) -> five parts

`ApplyAbilityActivatedEffects` runs 24 steps. The orchestrator stays and calls each step as a public member of the part that owns it, in the
original order. The parts are split by what the effect is for, not by position in the sequence.

| Part | Responsibility | Members that move | Lines | Services | Callers that change |
|---|---|---|---:|---:|---|
| `CombatAbilityActivationService` (stay) | The step order, queued weapon ability tracking | stays: `ApplyAbilityActivatedEffects`, `TrackQueuedWeaponAbilityUse`, `ResolveActivatedAbilitySkillType` | ~115 | 7 | none (19 sites) |
| `CombatAbilityCostEffectsService` | Effects tied to what an ability costs or primes: recast reduction, next-skill bonuses, hit point spend, force FP cost | `ApplyHitPointSpendAbilityEffects` (public), `ApplyAbilityUsedRecastReduction`, `ApplyAbilityUsedNextSkill*`, `ApplyAbilityUsedMasterAbilityHitChance`, `ApplyForceFPCostActivatedEffects`, `ApplyHitPointSpend*` | ~220 | 8 | 4 sites (2 content, 2 tests) |
| `CombatAbilityDefenseEffectsService` | Evasion, deflection and ally-defense effects | `ApplyAbilityUsedSkillEvasion`, `ApplyHostileAbilityUsedEvasion`, `ApplyCostlyAbilityUsedEvasion`, `ApplyAreaAbilityUsedEvasion`, `ApplyAbilityUsed*Deflection`, `ApplyAbilityUsedNearbyAllyDefense`, `ApplyAbilityUsedPerkCategory*`, `ApplyAbilityUsedEvasion` | ~400 | 7 | none |
| `CombatAbilityOffenseEffectsService` | Hostile ability effects: movement speed, area rewards, force attack, attack adjustment, sequence and resource restore | `ApplyAbilityUsedMovementSpeed`, `ApplyAreaAbilityUsedRewards`, `ApplyHostileAbility*`, `ApplyNextAutoAttackDamageBonus`, `ForgetActivationState`, the sequence table | ~285 | 7 | 2 sites |
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
(`RemoveStatTriggerCooldowns`) calls twelve forgets in a fixed order; after the splits it would need about fifteen services. The order
matters: the forgets have side effects that other forgets observe (`CombatSuppressionService.cs:447-461` removes the overwatch status,
`CombatDamageModifiersService.cs:1423` clears the ranged repeated-target tracker and its status, `TemporaryStatModifierService.cs:311-316`
publishes refresh notices), and `CombatActivity` and `CombatResources` each appear twice in it (`CombatService.cs:84-98`).

So the cleanup takes `IEnumerable<ICombatCreatureStateOwner>` and walks a phase list that is today's order exactly:
`ForgetCreatureState(creature, CombatForgetPhase phase)`, with `CombatForgetPhase` (its own enum file) numbering 1 resource triggers, 2 activity,
3 defense memory, 4 suppression, 5 activation, 6 hit effects, 7 ability impact, 8 opening activity, 9 ability stamina cost, 10 attack timing,
11 damage modifiers. Each owner handles the phases it owns and ignores the rest; resources owns 1 and 9, activity owns 2 and 8, the new parts
own one phase each (the hit effects stay-part and the critical part both own 6, the hit riders and area parts both own 7, the repeated-target
and rate parts both own 11, adjacent in registration order as the forgets are adjacent today). `CombatService` loops phases 1 to 11 over the
owners and then calls `_temporaryStatModifier.Clear(creature)` directly as the last call: `TemporaryStatModifierService` is Character, and
implementing a Combat interface would be an upward edge. Nothing holds `CombatService` plainly after the `Lazy<>` goes (only
`PerkService`'s `Lazy<>`, which stays), so the owner list is plain. P5-7b writes the order test against the current code (Tests).

`CombatService` after: stay-part ~200 lines, 8 services (the defeated-enemy part, suppression, combatActivity, stat, statusEffect,
temporaryStatModifier, the rate part and the repeated-target part, the last two for the two tracker clears in
`RefreshStatDrivenTrackerEffects`; the owner list is a list, not a service). `RefreshStatDrivenTrackerEffects` stays on `ICombatService`, so
`PerkService` (P5-10's file) is not edited.

### EnmityService (1,244 lines, 12 services) -> three parts

This is the D1 split of the Creatures guide: the part the AI core calls (the table reads and removals) must never call the leash, so the
table and the changes that call the leash are separate parts, and the AI core's call to `AttackHighestEnmityTarget` becomes a notice. It
builds on the P5-8e ports (`ICombatLeash`, `ICompanionTargetQuery`); service counts below are after them (the counts do not change:
`IAIService` becomes `ICombatLeash`, `ICompanionControlService` becomes `ICompanionTargetQuery`).

| Part | Responsibility | Members that move | Lines | Services | Callers that change |
|---|---|---|---:|---:|---|
| `EnmityService` (stay) | The tables and their reads: the three carried tables, the attack-command memory, reads, threat checks, removal and clearing; implements `IEnmityTargetQuery` and `IEspionageEnmityQuery` | stays: `GetEnmityTable`, `GetEnmityTowardsAllEnemies`, `GetHighestEnmityTarget`, `HasEnmity`, `HasProximityEnmity`, `HasNonProximityEnmity*`, `HasOnlyProximityEnmity`, `IsThreatSecured`, `IsThreatLeadSecure`, `RemoveEnmity`, `RemoveProximityEnmity`, `ClearEnmityTable`, `ClearEnmityTables`, the raw reads, the tracking removals; new mutation members (below) | ~350 | 0 | none (about 240 sites) |
| `EnmityChangeService` | Changes to enmity and the handlers that cause them | `CreatureDamaged`, `CalculateDamageEnmity`, `HealingStarted`, `HealingCompleted`, `CalculateHealingEnmity`, `CreatureAttacked`, `ModifyEnmity` (both), `CalculateAdjustedEnmity`, `TryTaunt`, `CalculateTauntEnmity`, `ModifyEnmityOnAll`, `ReduceEnmity`, `ReduceEnmityOnAll`, `CalculateReducedEnmity`, `ModifyProximityEnmity`, `RemoveCreatureEnmity`, `CreatureDeath`, `CreatureDestroyed`, `PlayerDeath`, `PlayerExit`, `CreatureLimbo`, the healing table | ~465 | 8 | about 260 sites (`ModifyEnmity` 138: 16 content, 63 tests, 50 engine, 9 domain) |
| `EnmityAttackService` | Choosing and commanding the attack target | `GetHighestEnmityAttackTarget`, `IsEligibleAttackTarget`, `ReevaluateEnemyAttackTargets`, `AttackHighestEnmityTarget`, `ResumeAttackAfterActionsCleared`, `IssueAttackCommand` and the stale-attack, hold and move-range helpers | ~375 | 8 | 54 sites (7 content, 41 tests, 4 engine, 2 domain) |

**The table part exposes mutation members, not dictionaries.** The change part never touches the tables directly:
- `AddEnmity(creature, enemy, amount)`: the block of `ModifyEnmity` from "retrieve the creature's list of associated enemies" to "update this
  creature's list of enemies". One reorder: `ModifyEnmity` calls `CalculateAdjustedEnmity(amount, CalculateEnmityAdjustment(...))` after the
  table entries are created and before the clamp; the adjustment reads stats and status effects, never the tables, so the change part
  computes it first and passes the adjusted amount. A transcript test proves the result and the call order are the same.
- `ReduceEnmityEntry(creature, enemy, percent)` (the table and proximity update of `ReduceEnmity`, returning whether it changed),
  `AddProximityEnmity(creature, enemy, appliedAmount)` (the tail of `ModifyProximityEnmity`), `GetEnemiesOf(creature)` (a copy, as every
  caller already used `ToArray()`), `RemoveEnmityTableEntry` and `RemoveProximityEnmityTracking` (promoted), and
  `TryGetAttackCommand`, `RecordAttackCommand`, `ForgetAttackCommand` for the attack-command memory, which lives in the table part because
  `ClearEnmityTables` and `RemoveCreatureEnmity` clear it.
- `GetEnmityTable` already hands out the live dictionary and is called 96 times; it stays as it is. No part stores the result of a table
  member in a field: adoption replaces the carried fields, so a cached reference would point at the old build's table.

The ten handlers move to `EnmityChangeService` (the eight events and `PlayerExit` twice); one `PlayerSession` classification line
(`PlayerExit`, `LogoutOnly`) moves with them. The carried keys do not change, because the tables stay in `EnmityService`; no
`[ReloadCarriedFrom]` and no `ReloadAdoption.txt` change. Hot path: `ModifyEnmity` runs per damage event; it gains two interface calls
(the table and the attack part), which `CombatLoad` measures.

**The 13 seams of `AIService` on `_enmity.Value` (`Creatures/AIService.cs:1818-1842`).** After P5-8h each seam moves with its caller into
the AI part that uses it; this is where each Enmity member lands.

| # | Seam | Used by (AI part after P5-8; line today) | Enmity part | After P5-7k |
|---|---|---|---|---|
| 1 | `GetHighestEnmityTarget` | core (480), events (163, 872), aggro (340, 1009), action (1749) | table | plain |
| 2 | `GetHighestEnmityAttackTarget` | action (1627) | attack | plain (action is above Enmity) |
| 3 | `GetEnmityTable` | core (833) | table | plain |
| 4 | `AttackHighestEnmityTarget` | core (510, 845); events (133, 170, 208, 228, 252); aggro (1012); decision (1222); action (1548) | attack | core: the retarget notice; the others plain |
| 5 | `ResumeAttackAfterActionsCleared` | events (311) | attack | plain |
| 6 | `IssueAttackCommand` | action (1633) | attack | plain |
| 7 | `ClearEnmityTable` | core (723) | table | plain |
| 8 | `RemoveEnmity` | core (842) | table | plain |
| 9 | `ModifyEnmity` | decision (1720) | change | plain |
| 10 | `HasProximityEnmity` | aggro (994) | table | plain |
| 11 | `ModifyProximityEnmity` | aggro (997) | change | plain |
| 12 | `RemoveTrackedProximityEnmity` (`RemoveProximityEnmity`) | aggro (1006) | table | plain |
| 13 | `RestoreNPCStats(outOfCombatRegen, _enmity.Value)` | events (131) | table | passes the table part as `IEnmityTargetQuery` (one member, `GetHighestEnmityTarget`) |

Six of the 13 reach members that call the leash back (`EnmityService.cs:369, 391, 448-449, 848, 862, 884, 936, 945, 959, 1044`): seams 2, 4,
5, 6 (the attack part) and 9, 11 (the change part). After P5-8h the AI core (the leash) uses only seams 1, 3, 7 and 8, which are table reads
and removals that never call the leash, and the one retarget call of seam 4 (two sites, 510 and 845), which becomes `ILeashRetargetListener`.
That is why P5-7k waits for P5-8h (Order). P5-7k runs the constructor scan on the real graph after P5-8 before any `Lazy<>` is made plain.

### DeathService (385 lines, 10 services) -> two parts

| Part | Responsibility | Members that move | Lines | Services | Callers that change |
|---|---|---|---:|---:|---|
| `DeathService` (stay) | Dying, death and the respawn handler | stays: `OnPlayerDying`, `OnPlayerDeath`, `IsSubdualDeath`, `RecordDeathDebtWaiver`, `OnPlayerRespawn` | ~250 | **10** (db, namedAnimation, messaging, playerName, statusEffect, statusEffectApplication, recorder, recorder damage part, damage taken, respawn) | none |
| `DeathRespawnService` | Respawn points, locations and penalties | `InitializeRespawnPoint` (the `OnModuleEnter` handler, `LoginOnly`), `GetRespawnLocation`, `HasRespawnArea`, `IsWalkable`, `SendToHomePoint`, `ApplyPenalties`, `WriteAudit` | ~170 | 4 (db, area, property, playerName) | 20 sites (`GetRespawnLocation` 10, `InitializeRespawnPoint` 5, `HasRespawnArea` 5; 16 tests, 4 engine) |

The stay-part is 10, not 9: it holds the recorder stay-part and the recorder damage part (`RecordSubdual`, `RecordRespawn`). Flagged; the
fallback that lowers it moves the `OnPlayerDying` handler (9 lines, the only use of damage taken) to `DeathRespawnService`, which takes damage
taken (5) and becomes the dying-to-respawn part: the stay-part is then 9. It costs one more handler rename (`OnModuleDying`).

### Totals

| Service | Lines before | Services before | Parts (lines, services) |
|---|---:|---:|---|
| `CombatHitEffectsService` | 1,716 | 20 | stay 300/8, Dealt 500/9, Triggered 110/8, AutoAttack 280/9, Critical 465/**10**, Defeated 255/9 |
| `CombatDamageTakenService` | 1,649 | 20 | stay 490/8, Effects 575/**10**, Memory 70/0, GuardedHit 110/9, GuardedHitEffects 240/5, Retaliation 190/9, Reflection 220/8 |
| `CombatAbilityImpactService` | 1,720 | 16 | stay 200/**10**, DamageBonus 145/7, HitRiders 380/6, AreaImpact 410/9, WeaponRiders 440/8, StatusApplied 320/8 |
| `CombatRecorderService` | 1,691 | 6 | core 340/0, stay 830/7, Ability 230/3, Damage 480/3 |
| `CombatDamageModifiersService` | 1,436 | 9 | stay 840/**10**, Rate 210/6, RepeatedTarget 410/3 |
| `CombatAbilityActivationService` | 1,203 | 14 | stay 115/7, Cost 220/8, Defense 400/7, Offense 285/7, Weapon 345/9 |
| `CombatAbilityBonusService` | 1,061 | 9 | stay 540/9, Queued 455/6 |
| `CombatAbilityHitService` | 426 | 12 | stay 180/9, Adjustment 225/7 |
| `CombatService` | 198 | 12 | stay 200/8, DamageRoll 50/0 |
| `EnmityService` | 1,244 | 12 | Tables (stay) 350/0, Change 465/8, Attack 375/8 |
| `DeathService` | 385 | 10 | stay 250/**10**, Respawn 170/4 |
| `CombatRecordSerializer.Write` | 198 | - | dispatcher 30 and four methods under 70 |

New services: 31 (HitEffects 5, DamageTaken 6, `CombatDamageRollService` 1, Impact 5, Recorder 3, DamageModifiers 2, Activation 4, Bonus 1,
Hit 1, Enmity 2, Death 1). Each has an interface, a `ServiceRegistrations` line and a `GameServices` property. By comparison the StatusEffect
split added six. The count is driven by the 10-service limit (20 injected services in each of two files), not by the line limit.

**Parts at exactly 10 services, and the fallback that lowers each** (each fallback is a real reduction, not a swap): `CombatCriticalHitEffectsService`
(the timing members to `CombatCriticalHitTimingEffectsService`: 9), `CombatDamageTakenEffectsService` (melee members to the reflection part:
9), `CombatAbilityImpactService` (the attacking-ally members to the weapon riders part: 7), `CombatDamageModifiersService`
(`GetHighHPTargetCriticalDamageAdjustment` to the rate part: 9), `DeathService` (`OnPlayerDying` to `DeathRespawnService`: 9). The recorder's
stay-part is the largest part left (about 830 lines, 83% of the limit). The first PR of each service re-measures; a part that measures over a
limit moves the member group named in its row to its lower neighbour, never a new numbered type.

## Holders

The services that hold a split service, before and after, as the StatusEffect guide does. "Before" is the constructor scan; "after" replaces
each split service the holder holds by the parts of it that the holder's calls reach (calls resolved by member name against the tables above).
Holders that hold a split service and do not change: the Abilities parts `AbilityDamageDeliveryService` 8, `AbilityHitRollService` 10,
`AbilityImpactLifecycleService` 10, `AbilityImpactMessageService` 3, `AbilityDarkForceService` 4, `AbilityService` 10,
`AbilityStatusApplicationService` 5, `AbilityTargetImpactService` 9; `PvpCombatService` 3, `MeleeTechniqueService` 7, `CombatRatingService` 7,
`CombatDeflectionReactionService` 7, `CombatPointService` 7, `CombatResourcesService` 8, `PerkService` 7, `StatService` 10, `CreatureStatService` 5,
`StatusEffectDurationService` 6, `ItemService` 9, `TrapsService` 10, `WeatherService` 7, `EspionageInfiltrationService` 6, `QuestService` 8,
`BeastMasteryService` 14, `DroidService` 14 (each reaches one part of each split service it holds, because the regrouping above put the
members they call in the stay-part: the recorder's four small entry members, the critical damage side, the rate reads).

| Holder (owner) | Before | After | Change | Why, and the remover of any growth |
|---|---:|---:|---:|---|
| `AbilityDamageInputService` (P5-3) | 11 (guard) | 13 | +2 | damage taken: stay and reflection (the force conversion); damage modifiers: stay and repeated target (`GetSameTargetPressureWeaponAbilityDamageBonus`, whose state is in the repeated part). Already over by accepted growth (Stat split, "removed by P5-16"); the entry grows from 11 to 13 and **P5-16 removes it** |
| `AbilityDamageStageService` (P5-3) | 10 | 11 | +1 | damage taken: stay and reflection (the force conversion). A new breach; **P5-16 removes it** (open question 4) |
| `AbilityHostileImpactService` (P5-3) | 9 | 9, or 10 with the impact fallback | 0 | calls the impact stay-part only (`IsTargetAttackingAllyForStatusAppliedRiders` is on it); 10 only if the fallback moves that member |
| `SpaceService` (P5-6) | 18 | 19 | +1 | enmity: table (`GetHighestEnmityTarget`) and change (`RemoveCreatureEnmity`). **P5-6 removes it** (it splits `SpaceService` last; its map must count this part) |
| `QuestEncounterService` | 4 | 5 | +1 | enmity: table and change. Within the limit |
| `StealthService` | 7 | 7 | 0 | enmity: the table part only once the two retarget calls become the notice |
| `DeathService` (this item) | 10 | 10 | 0 | see its section |
| `AIService` core after P5-8h | 7 | 7 | 0 | enmity: the table part plain; the retarget notice has its list in the core and takes no service |
| AI events part after P5-8g (`AICreatureEventService`) | 7 | 8 | +1 | enmity: table and attack |
| AI aggro part after P5-8g (`AIAggroService`) | 5 | 7 | +2 | enmity: table, attack (`AttackHighestEnmityTarget` 1012) and change (`ModifyProximityEnmity`) |
| AI decision part after P5-8h (`AIDecisionService`) | 4 | 6 | +2 | enmity: table, attack, change (`ModifyEnmity` 1720) |
| AI action part after P5-8h (`AIActionService`) | 7 | 8 | +1 | enmity: attack and table |

The AI parts take their counts from the Creatures guide; P5-7k re-measures each on the post-P5-8h code (the decision and action parts may share
`ModifyEnmity`). All stay at or under 10.

**Content and the other split maps** (injected counts grow as P5-13 and P5-14 convert the lookups; the lookup counts of today are in
`StaticLookups.txt`). From a scan of the current text:
- `Feature/UsePerkFeat.cs`: 5 Combat services now, 7 after (recorder: stay and ability part, 13 sites each; ability bonus: stay and queued, 2 and 6
  sites). P5-16 splits it and removes the growth.
- `WeaponActiveAbilityDefinitionBase.cs` (the P5-16f map: 11 parts, none over 9 services): 6 now, 8 after for the whole file (damage taken: defense
  memory 4 sites and effects 2; recorder: stay 3 and ability part 1). Per part the growth is at most +1, so no part passes 10; the parts that
  hold the guarded-hit and deflection checks (`WeaponAbilityDamageModifiers`, `WeaponAbilityStatusRiders`) and the part that records ability
  riders recount at the P5-7c and P5-7j2 PRs. `HeavyVibrobladeActiveAbilityDefinitionBase` 3 -> 4 (recorder ability part).
- `CharacterSheetViewModel` (the P5-16h sheet parts): 4 -> 4 after the regrouping (the rate reads and the damage reads are on the damage modifiers
  stay-part; `GetAbilityCriticalRate` is on the hit adjustment part, which the sheet reaches instead of the hit stay-part). Without the
  regrouping it would be 5. The sheet parts `CharacterSheetDamageEstimate`, `SurvivalRows` and `AccuracyRows` keep their counts (9, 4 and 8).

## Cycles

`LazyDependencyCycleTests` fails on a needless `Lazy<>`, and a plain cycle deadlocks the container build (P5-5g). The scan (a reading of
every constructor in `ServiceRegistrations` with `Lazy<>` edges kept apart) finds one strongly connected group of 68 services (with the
`Lazy<>` edges counted) that holds every Combat `Lazy<>` below. Plain paths were checked for each `Lazy<>` this split removes, on this
graph; each PR re-scans the real one.

**Broken by this split (three `Lazy<>` leave `DeferredResolution.txt`):**

| Today | Why it exists | How it goes |
|---|---|---|
| `CombatDamageModifiersService -> Lazy<ICombatDamageTakenService>` | one read, `HasRecentDeflection` | the read moves to `CombatDefenseMemoryService`, which takes no service; plain |
| `CombatHitEffectsService -> Lazy<ICombatDamageTakenService>` | `ApplyTriggeredDamage` calls `ApplyDamageTakenModifiers`, and damage taken calls hit effects back (reflection, retaliation, poison) | the damage taken stay-part keeps only the pre-damage modifiers, whose seven dependencies (recorder, pvpCombat, queries, resources, stat, enmity, statusEffect) reach no hit effects part by plain edges; the triggered part takes it plain, and every part that calls hit effects is above it |
| `CombatDamageTakenService -> Lazy<ICombatService>` | one call, `CalculateDamage` (guard retaliation) | `CalculateDamage` moves to `CombatDamageRollService` (no services) |

**Broken in the Enmity PR (P5-7k), on the real graph after P5-8h:**
- `AIService -> Lazy<IEnmityService>` (the D1 cycle). The core reads and removes through `EnmityService` (the table part, which never calls the
  leash) and its call to `AttackHighestEnmityTarget` (seam 4, sites 510 and 845) becomes the Combat-owned notice `ILeashRetargetListener`, with
  the list in the AI core and the registration from `EnmityAttackService` in a `Rerun` boot handler; an empty list throws. The core loses the
  `Lazy<>` (the Creatures guide expects this: D1, risks).
- `StealthService -> Lazy<IEnmityService>`. Stealth makes two retarget calls (`ReevaluateEnemyAttackTargets`, `AttackHighestEnmityTarget`) and one
  table read (`GetEnmityTable(...).Count`, plain once the table part has no path to stealth: `IsEligibleAttackTarget` and its `IsHiddenFrom` call
  live in the attack part). The two calls become a Combat-owned notice `IStealthRetargetListener`, with the list in `StealthService` and the
  registration from `EnmityAttackService` (the same boot handler). The edit to `StealthService.cs` is about 15 lines.

**Not broken here, routed:**

| Cycle | Why it stays | Goes to |
|---|---|---|
| `CombatRecorderService -> Lazy<IStatService>` | `StatService` takes the recorder (10 uses) and the stay-part reads FP and stamina for snapshots | P5-15: Foundation-owned reader for the resource reads |
| `CombatRecorderService -> Lazy<ICombatQueriesService>` | `CombatQueriesService` reaches the recorder by plain edges; the attribution resolver reads the equipped weapon skill (via `SkillService`) | P5-15: the same reader or a Character-owned weapon skill read |
| `PerkService -> Lazy<ICombatService>` (up) | one call, `RefreshStatDrivenTrackerEffects`; the refresh's dependencies (stat, activity, suppression) reach `PerkService` by plain edges | P5-10 or P5-15: a Character-owned perk-change notice that Combat registers with; P5-7 leaves the method on `ICombatService` so the file is not edited |
| `StealthService -> Lazy<IPerkService, IStatService, IStatusEffectService, IStatusEffectApplicationService, IEspionageInfiltrationService>` | `StatusEffectService -> MessagingService -> StealthService`, and `MessagingService` takes `IStealthService` only for `IsConcealed` | P5-15: `MessagingService` takes `IConcealmentQuery` from a no-dependency provider; then P5-8e's and P5-5h's notes shrink |
| `MessagingService -> StealthService` | as above | P5-15 |
| `AIService -> Lazy<IStatusEffectService>` and the other P5-8 ones | same Messaging path (the Creatures guide lists them) | P5-8e, P5-8f, P5-8g, P5-15 as that guide says |
| `StatusEffectDurationService` and `StatusEffectCombatEventService -> Lazy<IStatService>` | `StatService -> ResourceRestoredEffectsService -> StatusEffectApplicationService -> the part` | P5-15: a listener inversion at `ResourceRestoredEffectsService` |
| `ItemService -> Lazy<ITwinPistolEquipmentService>` | unchanged | P5-9 / P5-15 |

**New edges and why they are plain.** The layering, bottom to top: recorder core, defense memory, damage roll and the damage taken stay-part;
dealt effects; triggered damage; hit effects stay-part; the auto-attack, critical and defeated parts and the guard, reflection and taken-effects
parts; the recorder stay-part sits above the core and below its two layer 2 parts; the ability impact and activation parts; `CombatService`.
The impact and activation parts call hit effects and damage taken, never the reverse. The recorder stay-part reaches its damage part only
through the listener list, so there is no cycle. Each PR runs `LazyDependencyCycleTests`, the container validation (`ValidateOnBuild`) and, for
every constructor change, the engine start (a plain cycle hangs the build; a hung run means a new cycle, dump with `dotnet-dump collect`).

## Hot reload and handlers

- **Handlers that move** (`ScriptHandlerOrder.txt`, `HandlerRenames.txt`, renamed in place, event and position unchanged): `ApplyDamageTakenEffects` to
  the effects part; `RegisterStatusEffectListener` to the status-applied part; the ten Enmity lines to `EnmityChangeService`;
  `InitializeRespawnPoint` to `DeathRespawnService`; `RecordCreatureDeath` and `RecordPlayerDeath` to `CombatRecordDamageService`. 15 lines in all.
  `StopOnShutdown`, the three `CombatService` handlers and the other Death handlers stay.
- **Classification files:** `BootHandlerClassification.txt` (the impact registration moves; new `Rerun` registrations for the recorder damage
  part and the enmity attack part), `PlayerSessionClassification.txt` (`PlayerExit` of Enmity, `InitializeRespawnPoint`).
- **Rule 3 (mark state that boot or reload owns)**, applied where the new code owns a `Rerun` handler or a list:
  - Each new listener list field (`AIService`'s retarget list, `StealthService`'s retarget list, the recorder stay-part's session list) is
    `[ReloadState(ReloadStateKind.Runtime)]`, a repeat registration keeps one entry, and the lists are filled by the `Rerun` registration
    before the first notice (the P5-5d lists are the model; a reload reruns the handlers before it adopts carried state).
  - `CombatAbilityStatusAppliedService` and `CombatRecordDamageService` own a `Rerun` handler, so every field of theirs that is not a plain cache
    is marked: the recorder damage part's three tables are `Runtime`; the status-applied part has no field. `EnmityAttackService` owns the
    registration handler and has no field of its own (the attack-command memory is in the table part).
  - `EnmityService` owns the carried tables (adoption), so its other fields are marked: the attack-command memory is `Runtime` (today unmarked;
    the change is the attribute only). Its carried keys do not change.
  - The AI core's list is part of the AI parts' marking in P5-8; P5-7k adds the one attribute.
- **Carried state:** none new. `ReloadAdoption.txt` and `ReloadCarryHistory.txt` do not change. `ReloadStateCarrierTests` keeps the Enmity names.
- **Reload checks:** run `scripts/run-hot-reload-check.sh --engine-filter HotReloadPlayers --reloads 2` on the PRs that move handlers or
  registrations (P5-7h2, j3, k, l). The Enmity PR also runs the carried-table round trip in `ReloadStateCarrierTests` and an engine run
  that reloads with an enemy holding enmity.

## Tests and the proof plan

**P5-7b (tests only, before any code moves).** It writes the proof against the current code, through the public interfaces of the real
container with recording fakes at the leaves, and every later PR runs it unchanged except for receivers (`GameServices.X.` and `_x.` text that
the facade tool rewrites):
- **An ordered call-and-draw transcript per orchestrator and handler**: `ApplyDamageDealtEffects`, `ApplyTriggeredDamage`,
  `ApplyCriticalHitEffects`, `ApplyAutoAttackDamageModifiers`, `ApplyDefeatedEnemyEffects`, `ApplyDamageTakenModifiers`,
  `ApplyDamageTakenEffects` (the handler), `ApplyGuardedHitModifiers`, `ApplyAbilityActivatedEffects` (24 steps),
  `ApplySuccessfulAbilityImpactRiders`, `ApplyAbilityImpactEffects`, the status-applied listener notices, `ApplyDamageDealtModifiers`,
  `TryResolveAbilityHit`, `DeathService`'s three handlers, and each `CombatService` handler. A transcript records every call into a leaf
  service (stat, status effects, temporary modifiers, resources, recorder, enmity, activity), its arguments, and every `Random` draw.
- **The cleanup order test**: a populated bench cleared by the current `RemoveStatTriggerCooldowns`, recording not only the tables but every
  status removal and refresh notice the forgets cause (the overwatch removal at `CombatSuppressionService.cs:447-461`, the repeated-target
  clear at `CombatDamageModifiersService.cs:1423`, the refresh notices at `TemporaryStatModifierService.cs:311-316`), in order. P5-7l's phase
  list must produce the same transcript.
- **A recorder transcript for a scripted fight**: a `Start`, a fight (ability cast and impact, damage with temporary hit points, a heal with a
  nested heal, a status applied and removed, a death, a respawn, a resurrection, a `Stop`), with every `CombatRecordEvent` and the file lines
  the writer receives, byte for byte. It is the proof for the three recorder PRs.
- **An enmity table transcript**: a scripted sequence of damage, healing, taunt, reduce, proximity, removal, limbo and exit events, with the
  tables, `_creatureToEnemies` and the attack commands compared after each step, and a reload round trip through the carried keys.
- **One golden JSON line per serializer event type.** `CombatRecordSerializer.Write` has 23 case labels. `CombatRecordSerializerTests` builds 19
  event types (`AbilityQueued`, `AbilityCost`, `Attack` and `StatusRefreshed` never appear) and mostly asserts parsed fields, so P5-7b adds a
  whole-line golden for all 23.
- **An engine test that fires the stealth retarget notice.** None exists: no engine test mentions `ReevaluateEnemyAttackTargets`, and
  `GhostProtocolStealthEngineTests`, `StealthedPlayerAreaAbilityEngineTests` and `ForceEspionageEngineTests` enter and leave stealth without
  asserting the enemy's attack target. P5-7b adds `StealthRetargetEngineTests`: an enemy attacking a player who stealths, and again when the
  player is revealed, with the attack target and the enmity table asserted. P5-7k runs it for the notice.
- **Groundwork as in P5-5b:** `CombatParts.OtherServices` gains the recorder parts; `EnmitySource` and `DeathSource` (like `StatusEffectSource`)
  so the 12 path-reading tests (Enmity 7, Death 5) read through one place; each of the 11 unit test classes (`CombatHitEffectsServiceTests` 1,860
  lines, `CombatDamageTakenServiceTests` 2,130, `CombatAbilityImpactServiceTests` 2,008, `CombatAbilityActivationServiceTests` 1,639,
  `CombatRecorderServiceTests` 1,342, `CombatDamageModifiersServiceTests` 1,168, `EnmityServiceTests` 1,124, `CombatAbilityBonusServiceTests` 730,
  `DeathServiceTests` 596, `CombatAbilityHitServiceTests` 568, `CombatServiceTests` 377; about 14,000 lines) subclasses its service to override
  the `protected virtual` seams, so each double becomes partial files by part, and each test is pointed at the part that owns its member;
  `CombatPartForwardingTests.Parts()` and `CombatFacadeAllocationTests` (the allocation pins) gain the new interfaces.

**Per PR** (build once with `-p:RunPostBuildEvent=Never -v q -clp:ErrorsOnly`, then `dotnet test <project> --no-build --filter`):
- **CombatSim readings byte-identical** to the base: `team-fight`, `twin-pistol-barrage`, `twin-pistol-crossfire`, `solo-boss`; seed 1, 50 runs, Markdown
  and JSON, plus the ability table. CombatSim reads `CombatRules`, `StatusEffectRules` and `StatRules` only, so a difference means a rule changed, not a move.
- **Characterization and parity.** `SWLOR.Game.Server.CharacterizationTests` (148 tests; the `Combat*CharacterizationTests` files) run with no
  expectation changed. They do change in text at P5-7c: `CombatDamageCharacterizationTests`, `CombatResourceCharacterizationTests` and
  `SeededRandom` call `GameServices.Combat.CalculateDamage`, which becomes `GameServices.CombatDamageRoll.CalculateDamage`; the facade tool makes
  that edit and the snapshots stay as they are. `ScriptHandlerOrderTests`, `ParityHandlerMappingTests`, `BootHandlerClassificationTests`,
  `PlayerSessionClassificationTests`, `ReloadStateCarrierTests`, `ReloadCarryHistoryTests`, `ReloadDigestTests`, `ClockSeamSourceGuardTests`,
  `LazyDependencyCycleTests`, `ConvertedServiceRulesTests`, `PlayerMessageAuditTests` also run.
- **New tests per PR:** `CombatDefenseMemoryServiceTests` (window and purge behaviour copied from the damage taken tests), the listener-empty
  tests, `CombatStateOwnerTests` (every table-holding part is registered, the phase list equals the old order, an empty list throws), and the
  tests of each new part's seams.
- **Unit filters** per PR: `FullyQualifiedName~` the service the PR moves (`CombatHitEffects`, `CombatDamageTaken`, `CombatAbilityImpact`,
  `CombatRecorder`, `CombatDamageModifiers`, `CombatAbilityActivation`, `CombatAbilityBonus`, `CombatAbilityHit`, `CombatService`, `EnmityService`,
  `DeathService`) with `CombatPartForwarding`, `CombatSource`, `CombatFacadeAllocation` and the P5-7b transcripts; `Phase5DebtTests`, `DomainMapTests`
  and the rest of `SWLOR.Architecture.Tests` with the baselines regenerated each PR.
- **Engine tests** (filter by area, host entry): `CombatRecorderEngineTests` (recorder), `TankEnmityEngineTests` and `DMLimboEnmityEngineTests` (Enmity),
  `DeathRespawnPointEngineTests` (Death), `MarkedForDeathEngineTests`, `StealthRetargetEngineTests` (new), and `Characterization/` scenarios for hit,
  damage and ability impact; the 326 override sites in engine tests exercise `CombatAbilityHitService`.
- **Coverage.** `Domain/Combat` is at 96 in `CoverageFloors.json` (overall 73). Each new part ships with its tests in its own PR and the gate runs with
  coverage; the moved code keeps its existing tests, so the floor holds. Native reads (`CombatRecorderEffectHooks`) run only in the engine.
- **Client evidence:** none. The split changes no in-game surface (code moves only); the engine runs and the transcripts are the evidence, as for P5-5.

**Baselines each PR changes** (regenerate with `SWLOR_UPDATE_SNAPSHOTS=1`, review the diff): `ServicesPerType.txt` (the seven lines leave as each service
reaches 10: HitEffects 20, DamageTaken 20, Impact 16, Activation 14, AbilityHit 12, `CombatService` 12, Enmity 12; `AbilityDamageStageService` may gain
a line and `AbilityDamageInputService` changes from 11 to 13 until P5-16), `LargeTypes.txt` (eight lines: Impact 1,720, HitEffects 1,716, Recorder
1,691, DamageTaken 1,649, DamageModifiers 1,436, Enmity 1,244, Activation 1,203, Bonus 1,061), `LongMethods.txt` (`CombatRecordSerializer.Write`),
`DeferredResolution.txt` (the three `Combat` lines, plus Enmity's holders in the Enmity PR), `UpwardEdges.txt` (Quests 3, Social 3 + 3 + 3, World 3),
`LogicToContent.txt` (lines re-keyed to the part files), `DebtReport.md`; `ConvertedServices.json`, `DomainMap.json`, `RemovedFacades.json` (the facade
tool's names per owner), `ServiceRegistrations.cs`, `GameServices.Combat.cs`, `HandlerRenames.txt`, `ScriptHandlerOrder.txt`, `ClockSeamFiles.txt`
(eight entries, plus each new part that reads the clock), `PlayerMessageAudit.json` (18 rows keyed by these files: DamageModifiers 6, DamageTaken 4,
Death 4, HitEffects 2, Hit 1, Impact 1). `CrossDomainEdges.txt` and `EdgesIntoCoreAndPlatform.txt` change only in member counts; revert them as P5-5g did.

## Order

Bottom-up where a part needs a lower one, otherwise independent. A moved part calls the members still on its old service through the old
interface, promoted from private to public for that PR; the next PR moves them. Moved lines are member lines (about 90% of the part sizes);
callers are rewritten by `Apply-FacadeRemoval.ps1` (receiver name only), and the 15 or so instance call sites in holders by hand.

| PR | Contents | Moved lines | Files touched (beyond tests and baselines) | Needs |
|---|---|---:|---|---|
| P5-7a | This guide | 0 | the guide, README, Phase 5 link | - |
| P5-7b | The proof of "Tests and the proof plan"; no production code | 0 | `Tests/Support/*`, source-reading tests, `CombatParts.cs`, the 11 `*Tests` doubles, the new transcript and engine tests | P5-7a |
| P5-7c | `CombatDamageRollService` (from `CombatService`), `CombatDefenseMemoryService` with the two `Lazy<>` (damage modifiers, damage taken) gone, the serializer `Write` cut | ~90 moved, ~200 re-cut | `CombatService`, `CombatDamageTakenService`, `CombatDamageModifiersService`, `CombatRecordSerializer`, 10 content and 2 native call sites of `Combat.CalculateDamage`, the 10 external defense memory sites (below), `ServiceRegistrations`, `GameServices.Combat.cs` | P5-7b |
| P5-7d | `CombatRateModifiersService`, `CombatRepeatedTargetService` | ~530 | `CombatDamageModifiersService`, 8 holders, 35 call sites | P5-16h PRs 7 and 8; P5-7c |
| P5-7e1 | `CombatDamageTakenEffectsService` (handler renamed), `CombatDamageReflectionService` | ~680 | `CombatDamageTakenService`, `ScriptHandlerOrder.txt`, `HandlerRenames.txt`, `CombatDeflectionReactionService`, `CombatAbilityActivationService`, `AbilityDamageInputService`, `AbilityDamageStageService`, 60 call sites | P5-7c; P5-16f first (recommended) |
| P5-7e2 | `CombatGuardedHitService`, `CombatGuardedHitEffectsService` (with `ApplyGuardedHitNextAttackEffects` from `CombatAbilityHitService`), `CombatGuardRetaliationService`; the damage taken stay-part is now the modifiers only | ~380 | `CombatDamageTakenService`, `CombatAbilityHitService`, 12 call sites | P5-7e1 |
| P5-7f1 | `CombatDealtEffectsService`, `CombatTriggeredDamageService` with the orchestrator cuts; `Lazy<ICombatDamageTakenService>` plain; `IBeastDamageRecording` and `IPartyRoster` swaps | ~510 | `CombatHitEffectsService`, `CapstoneProofService`, 25 call sites (13 domain) | P5-7e2 |
| P5-7f2 | `CombatAutoAttackEffectsService`, `CombatCriticalHitEffectsService`, `CombatDefeatedEnemyEffectsService` | ~845 | `CombatHitEffectsService`, `CombatService`, 35 call sites | P5-7f1 |
| P5-7g | `CombatQueuedAbilityBonusService`, `CombatAbilityHitAdjustmentService` | ~650 | `CombatAbilityBonusService`, `CombatAbilityHitService`, `Feature/UsePerkFeat.cs` (8 sites), 70 call sites (22 content) | P5-7e2 |
| P5-7h1 | `CombatAbilityDamageBonusService`, `CombatAbilityHitRidersService`, `CombatAbilityAreaImpactService` | ~785 | `CombatAbilityImpactService`, `Feature/UsePerkFeat.cs` (2 sites), 25 call sites | P5-7f2 |
| P5-7h2 | `CombatAbilityWeaponRidersService`, `CombatAbilityStatusAppliedService` (listener registration, `IPartyRoster`) | ~670 | `CombatAbilityImpactService`, handler lines, `BootHandlerClassification.txt` | P5-7h1 |
| P5-7i1 | `CombatAbilityDefenseEffectsService`, `CombatAbilityCostEffectsService` | ~520 | `CombatAbilityActivationService`, 6 call sites | P5-7e1 |
| P5-7i2 | `CombatAbilityOffenseEffectsService`, `CombatAbilityWeaponActivationService` | ~530 | `CombatAbilityActivationService` | P5-7i1 |
| P5-7j1 | `CombatRecordSessionCore`; the `Start` and `Stop` cuts; the `IsRecording` getter | ~250 | `CombatRecorderService`, `ServiceRegistrations` (the four registrations point at the core or the stay-part), `ServerTeardown` unchanged | P5-7b |
| P5-7j2 | `CombatRecordAbilityService` (`IStatService` plain) | ~170 | `CombatRecorderService`, `CombatParts.cs`, `Feature/UsePerkFeat.cs` (13 sites), 16 call sites | P5-7j1 |
| P5-7j3 | `CombatRecordDamageService` (handlers renamed), the session notice | ~480 | `CombatRecorderService`, handler lines, `Feature/ChatCommandDefinition/DMWorldChatCommand.cs` (`RecordResurrection`), 17 call sites | P5-7j2 |
| P5-7k | Enmity: `EnmityChangeService`, `EnmityAttackService`; `ILeashRetargetListener`, `IStealthRetargetListener`, `IHologramQuery`; two `Lazy<>` go; runs the constructor scan on the real graph before making any `Lazy<>` plain | ~720 | `EnmityService`, `StealthService`, `AIService` (the one call and the list), `HoloComService`, `Feature/UsePerkFeat.cs` (4 sites), 10 handler lines, 300 call sites (about 250 of them tests) | **P5-8h merged** (the AI core left with the table reads and the one retarget call), P5-8e, P5-7f2, P5-7h2 |
| P5-7l | `DeathRespawnService`; `CombatService` takes the state owner list with the phase order; close-out: README, Status, the guide's Progress | ~140 + 80 | `DeathService`, `CombatService`, every part that holds a table (one interface), `ServiceRegistrations`, `PlayerSessionClassification.txt` | all |

**P5-7c's defense memory call sites** (the 10 sites outside the damage taken service that change from `CombatDamageTaken` to
`CombatDefenseMemory`, plus the one `Lazy<>` use): `WeaponActiveAbilityDefinitionBase.cs` 386, 487, 714 and 1154 (`HasRecentGuardedHit`,
`HasRecentDeflection`); `CombatPerkRegressionEngineTests.cs:202`; `TwinPistolEngineTests.cs:563`; `CombatFacadeAllocationTests.cs:993` and `994`;
`CombatSourceTests.cs:399` and `404`; and `CombatDamageModifiersService.cs:62` (the `Lazy<>`).

P5-7 ends with the 11 services at or under 10 services and 1,000 lines, no method over 150 lines, three fewer `Lazy<>` in Combat, the
`Lazy<IEnmityService>` of `AIService` and `StealthService` gone, and the Combat -> Social, Quests and World edges named above closed. 17 pull
requests after this guide; about 8,000 lines move in total. Each PR is one branch from the refactor branch's tip, gets a Codex review of the
current head, and runs the full gate with coverage. Parallelism (rule 4: no two items on the same files): c and j1 can run together; d, e1, g, i1
after c; j2 and j3 follow j1; h and f follow the chain above. At most three agents.

## Conflicts

- **P5-8e (`AIService`, `CompanionControlService`, the Combat ports).** It edits `EnmityService.cs` (parameter types, six leash call sites) and
  `StealthService.cs`. P5-7k waits for it (the Creatures guide's D3 already says P5-8e runs before P5-7 starts on Enmity). P5-7a to P5-7j
  do not touch either file.
- **P5-8h (the AI core).** P5-7k depends on the P5-8 PR that leaves the AI core with only the table reads and the one retarget call, which the
  Creatures guide puts at P5-8h (its dependency table, line 127, and the order table, line 466). Until then the core still holds all 13 seams.
  P5-7k edits `AIService.cs` (the one call and the list, about 15 lines), so it also waits until no P5-8 PR is open on that file.
- **P5-10 (`PerkService`, `SkillService`, `MimicryService`, `DroidService`, `BeastMasteryService`).** No P5-7 PR edits these files:
  `RefreshStatDrivenTrackerEffects` stays on `ICombatService`, and `BeastMasteryService` and `DroidService` call only `BeginSetupAttribution`
  (the recorder stay-part). `MimicryService` holds a `Lazy<>` for `ICombatPointService`, which this split does not touch.
- **P5-16f (`WeaponActiveAbilityDefinitionBase`).** Its parts call `CombatAbilityActivation`, `CombatAbilityBonus`, `CombatAbilityHit`,
  `CombatDamageTaken`, `CombatRecorder` and `Enmity` through `GameServices`. The overlap starts at P5-7c, not at P5-7e1: `HasRecentGuardedHit`
  and `HasRecentDeflection` at `WeaponActiveAbilityDefinitionBase.cs` 386, 487, 714 and 1154 change receiver in P5-7c (listed in the P5-7c row).
  Later PRs change the others (`CombatQueuedAbilityBonus` 16 sites at P5-7g, the ability recorder part at P5-7j2). Rule 4 keeps hand edits out
  of P5-16 types, but the facade tool already rewrote this file in P5-5f and P5-5g (22 -> 24 lookups), so a scripted receiver rewrite is the
  accepted route. Recommendation: P5-16f lands first (its code starts now that P5-5h is merged), so P5-7 rewrites the new parts; if P5-16f slips,
  P5-7 rewrites the base and P5-16f re-measures. Either way the lookup counts of that type grow by the number of new parts it names (Holders).
- **`Feature/UsePerkFeat.cs`** (a P5-16 type that P5-8f also edits, 9 scripted sites, decision D2). P5-7g (queued ability bonus, 8 sites), P5-7h1
  (`ApplyAbilityImpactEffects`, 2 sites), P5-7j2 (ability records, 13 sites) and P5-7k (attack part, 4 sites) change receiver text in it. All four
  are scripted receiver edits, like P5-8f's; none is a hand edit, and each merges with the others because they touch different lines. The PRs
  that touch it name it in their file list.
- **`Feature/ChatCommandDefinition/DMWorldChatCommand.cs`** calls `CombatRecorder.RecordResurrection` (line 124); it changes receiver in P5-7j3.
- **P5-16h PRs 7 and 8 (the character sheet).** `CharacterSheetViewModel` reaches `CombatAbilityBonus`, `CombatAbilityHit`, `CombatAttackTiming`,
  `CombatDamageModifiers`, `CombatDamageTaken` and `CombatResources`; the view model guide says both PRs merge before P5-7 starts. They must be
  merged before P5-7d (the first PR that changes a receiver the sheet uses); P5-7b and P5-7c touch no sheet file. The sheet's
  `CharacterSheetDamageEstimate`, `SurvivalRows` and `AccuracyRows` parts are the files P5-7 would edit.
- **P5-2 (follows P5-7).** It removes the `LogicToContent.txt` lines this guide leaves (counts in "What the code is"), now keyed to the part files.
- **P5-16b (hook logic out of `NativeSinks`).** The homes after P5-7: the attack roll reads `CombatAbilityHitService` and its adjustment part, and
  the stat adjustments in `CombatDamageModifiersService` and `CombatRateModifiersService`; the damage roll reads `CombatDamageModifiersService`,
  `CombatDamageTakenService` (pre-damage), `CombatDamageTakenEffectsService`, `CombatDamageReflectionService` and the hit effects parts; the
  deflection hooks read the damage taken effects part. About 60 call sites in `Native/` and `NativeSinks/` change receivers by the tool.
- **Domain order.** All new parts are Combat. The edges they add point down (Character, Foundation) or to ports Combat owns.

**Master.** Master still holds this logic in `Service/Combat.cs` (13,240 lines), `Service/CombatRecorder.cs` (1,641), `Service/Enmity.cs` (1,142) and
`Service/Death.cs` (340). Commits to them since 2026-10-01 (`origin/master` at `ca97d5ddc`), 112 distinct in all, by period and file:

| File | 1 to 31 October | 1 to 17 November (of which since 10 November) |
|---|---:|---:|
| `Service/Combat.cs` | 66 | 9 (0) |
| `Service/CombatRecorder.cs` | 25 | 0 (0) |
| `Service/Enmity.cs` | 10 | 1 (0) |
| `Service/Death.cs` | 11 | 2 (1) |
| distinct commits | 101 | 11 (2) |

November's rate is 11 commits in 17 days, about 4 a week, but front-loaded: 9 in the first nine days and 2 in the last eight. One of the
latest (`cb465ec15`, "Show the respawn panel after a lethal PvP death") is not yet on the branch, and touches `Death.cs`. Tests that read these files by
path on master: 27 for `Combat.cs`, 5 for `Enmity.cs`, 5 for `Death.cs`, 1 for `CombatRecorder.cs`. The per-service share of master's commits cannot
be read from the diffs (master's hunks name call sites in other files), so only the totals are given. For comparison the StatusEffect freeze
covered one file with 27 commits since 2026-10-10.

**Freeze windows, per file** (owner decision, 2026-11-18: "Yes, per-file windows"; open question 1). A master edit to a moved range must be ported by hand to whichever part owns it
and the ranges change with each PR, so each file is frozen only while its code moves:
- `Service/Combat.cs`: P5-7c to P5-7i2 (the Combat parts d to i; P5-7l's `CombatService` edit is small and ported by hand if master touched
  the handler or cleanup ranges). 9 November commits and 66 in October make this the file that matters.
- `Service/CombatRecorder.cs`: P5-7j1 to P5-7j3 (no November commits).
- `Service/Enmity.cs`: P5-7k only (one PR).
- `Service/Death.cs`: P5-7l only (one PR; `cb465ec15` must be on the branch first).
The 38 tests that read these files by path are in the same windows. A master edit inside a window waits for the window to close, or is
ported by hand with a Port log row.

## Open questions

All six are closed. Question 1 is the owner's decision; question 4 is the coordinator's decision (2026-11-18); the other four are
coordinator decisions that take this guide's recommendation.

1. **Master freeze, per file?** Closed, owner decision (2026-11-18): "Yes, per-file windows". `Service/Combat.cs` is frozen from P5-7c to
   P5-7i2, `Service/CombatRecorder.cs` from P5-7j1 to P5-7j3, `Service/Enmity.cs` for P5-7k, and `Service/Death.cs` for P5-7l (`cb465ec15`
   synced first). The 38 tests that read these files by path follow the same windows.
2. **Enmity part names and which part keeps `IEnmityService`.** Closed, coordinator decision: `EnmityService` stays the table part (carried
   keys unchanged, no `[ReloadCarriedFrom]`, the AI core's reads stay), and the writes (`ModifyEnmity`, 138 sites) move to
   `EnmityChangeService`.
3. **The Stealth notice.** Closed, coordinator decision: a separate `IStealthRetargetListener` in P5-7k, not a widening of P5-8e's
   `IStealthAggroListener`. If P5-8e has not landed when P5-7k starts, P5-7k waits (it depends on P5-8h anyway).
4. **Holder growth that regrouping cannot remove.** Closed, coordinator decision (2026-11-18): accepted with the named removers.
   `AbilityDamageInputService` goes to 13 and `AbilityDamageStageService` to 11, both removed by P5-16; `SpaceService` goes to 19, removed by
   P5-6. Stage is a new `ServicesPerType.txt` line until then.
5. **The recorder's two `Lazy<>`.** Closed, coordinator decision: left to P5-15 (they need a Foundation-owned reader for FP, stamina and the
   weapon skill, which is a design decision of its own).
6. **Cleanup by owner list with phases.** Closed, coordinator decision: the phase-numbered owner list for `CombatService`, as above; it keeps
   today's order exactly, including the second `CombatActivity` and `CombatResources` calls, and the direct last call to
   `TemporaryStatModifierService`.

## Progress

P5-7a: this guide (2026-11-18), revised after the coordinator's review. P5-7b: the proof (2026-11-18, "P5-7b as built" below). P5-7c: the damage roll, the defense memory and the serializer cut (2026-11-18, "P5-7c as built" below). P5-7d: the rate and repeated-target parts (2026-11-21, "P5-7d as built" below). P5-7e1: the damage taken effects and reflection parts (2026-11-23, "P5-7e1 as built" below). P5-7i1: the ability activation defense and cost parts (2026-11-23, "P5-7i1 as built" below). Next: P5-7e2, P5-7g and P5-7i2 can start.

### P5-7b as built (2026-11-18)

Tests only; no production file changed. Later PRs run every test below unchanged except for receivers (`bench.Get<I...>()` types and the casts to a
concrete class where a handler is not on an interface).

- **The bench** (`Tests/Support/CombatTranscriptBench.cs`, `TranscriptLeaf.cs`, `ForwardingRecorder.cs`, `CombatTableState.cs`): the real container
  (`FacadeContainer`) with recording stand-ins at its leaves (`CombatTranscriptBench.DefaultLeaves`: stat, the four status effect services, temporary
  stat modifiers, resources, the recorder and its three interfaces, enmity, activity, attack timing, suppression, paired ranged traits, capstone proof,
  resistance, party, rating), an engine fake whose writes are recorded in order (`DelayCommand` and `ActionDoCommand` run after the step's own calls),
  an in-memory database with two players, the `Clock` held still, and `Random` replaced by a seeded generator that records every draw. A leaf answers
  from a table or with the empty value of its return type; a step that throws fails the test, except at the native library (the transcript ends there
  with a marker). Every other Combat service is real, so a transcript crosses the seams the split moves. The recorder's `get_IsRecording` reads are
  left out (the split changes how they are served). Fixtures are in `Tests/Fixtures/CombatTranscripts/*.txt`, `-text` in `.gitattributes`, LF.
- **Transcripts** (each with every stat at zero and again at 5, so the gates and every rider are pinned; weapon-family riders once per weapon skill):
  `CombatHitEffectsTranscriptTests` (`ApplyDamageDealtEffects`, `ApplyTriggeredDamage`, `ApplyCriticalHitEffects`, `ApplyNonCriticalAbilityEffects`,
  `ApplyAutoAttackDamageModifiers`, the cycle bonus, `ApplyDefeatedEnemyEffects`), `CombatDamageTakenTranscriptTests` (`ApplyDamageTakenModifiers`,
  the damage-over-time and fatal-damage members, `ApplyGuardedHitModifiers`, the `ApplyDamageTakenEffects` handler, reflection and force conversion),
  `CombatAbilityTranscriptTests` (`ApplyAbilityActivatedEffects` for 15 skills, `ApplySuccessfulAbilityImpactRiders` for 15 skills,
  `ApplyAbilityImpactEffects`, the status-applied listener notices through the registered listener, `ApplyDamageDealtModifiers` for 15 skills,
  `TryResolveAbilityHit` with the overrides, the bonus reads), `CombatDeathTranscriptTests` (the three `DeathService` handlers, the login handler,
  `CombatService`'s three handlers and the tracker refresh).
- **Cleanup order** (`CombatCleanupOrderTests`): ten real parts decorated to record their forgets and the temporary modifier clear, a bench with 34 tables populated for
  four creatures, the exit and the death path. The transcript shows the tables before and after, the forgets in order including the second
  `CombatActivity` and `CombatResources` calls, the two overwatch removals of the suppression forget, the `SpottersRhythm` removal of the damage
  modifiers forget and the refresh notice of the final `TemporaryStatModifier.Clear`. Two tables are not cleared today (`_containmentNormalizationTargets`,
  `_damageDerivedHealingStates`); the transcript pins that.
- **Recorder** (`CombatRecorderTranscriptTests`): a scripted fight of 20 steps through the real writer, 42 event lines compared byte for byte. The
  double `Probe` names the recorder's constructor; P5-7j1 updates it. The summary file is not compared (the writer stamps `DateTime.UtcNow`, no seam).
  **Serializer** (`CombatRecordSerializerGoldenTests`): one golden line for each of the 23 event types plus 13 variants of the other branch.
- **Enmity** (`EnmityTranscriptTests`): 31 steps (damage, attack, heals with a nested heal, taunts, reduces, proximity, removal, limbo, death, destroy,
  player death and exit, the attack command, the hidden-target hold, the stale proximity target, the leash starting and already running), the six
  tables dumped after each, and a reload round trip of the carried keys through `ReloadStateCarrier`.
- **Engine test** (`StealthRetargetEngineTests`, two tests): an enemy attacking a player who enters stealth (through Ghost Protocol, the engine refuses
  stealth in combat otherwise) drops the player and keeps the enmity, and attacks again when the player is revealed; two enemies do the same. It passed
  on a full engine hak set built in a throwaway worktree.
- **Clock seams (behaviour-neutral, the P5-16h PR 0 precedent).** `TemporaryHitPointEffects` stamped the pool expiry
  `TEMPORARY_HP_POOLS_ACTIVE_UNTIL` with `DateTimeOffset.UtcNow`, and `CombatRecordWriter` stamped the summary file with `DateTime.UtcNow`. Both read
  `Clock.UtcNow` now (`new DateTimeOffset(Clock.UtcNow)` by hand, because `Apply-ClockSeam.ps1` does not rewrite `DateTimeOffset`; the writer's
  `Clock.UtcNow` is the tool's own form, written by hand), and both files are in `ClockSeamFiles.txt` (`-Check` stays clean). That put the raised-stat hit point spend back into the
  activation transcript and the summary file into the recorder transcript. Only `AbilityActivation.txt` and `Recorder.txt` changed.
- **Not covered, and why.** Paths that reach the native library (`ActionInterruption`, the rifle pacification rider) end their transcript at a
  marker in two impact steps.
- **Groundwork (the "Groundwork as in P5-5b" bullet).** `Tests/Support/PartSourceFiles.cs` is the reading half of `StatusEffectSource`; `EnmitySource` and
  `DeathSource` hold one file list and one part-interface set each, so a PR that moves a part edits one list. The 12 path-reading tests read through
  them (Enmity: `AIModelTests`, `EnmityAdjustmentTests`, `EnmityTests`, `EspionageSystemTests`, `NPCChaseTests` through
  `DMPossessionHarness.InstanceMethod(roots, name)`; Death: `AuthoredCombatAnimationTests`, `ForceLightGuardianTests`, `PerformanceAudienceCostTests`,
  `PerformanceAudienceStateTests`, `StatusEffectDeliveryTests`); `RealEnmityTables` and `ReloadStateCarrierTests` read no path (the tables stay in
  `EnmityService`). `CombatParts.OtherServices` names `CombatRecordAbilityService` and `CombatRecordDamageService`
  (the session core is not named `Combat*Service`); `CombatSourceTests` lets a name without a file yet stand if it starts with `CombatRecord`. The
  doubles are `partial` files in `Tests/Support/`: `TestHitEffects`, `TestDamageTaken`, `TestDamageModifiers`, `TestEnmity`, `TestDeath` (with
  `TestDeathWaiver` and `DeathScope`, which the test and the double share), `TestRecorder` (`PlainRecorder`, `RecorderProbe`, `ScheduledTask`); a PR
  that splits a service adds one partial file per part. **The map was wrong that all 11 unit test classes subclass their service:** six do
  (eight doubles); `CombatAbilityImpactServiceTests`, `CombatAbilityActivationServiceTests`, `CombatAbilityBonusServiceTests`,
  `CombatAbilityHitServiceTests` and `CombatServiceTests` construct the real class with scripted neighbours, so they have no double to move; the PR
  that splits each still re-points its constructor calls. The path-reading tests of `CombatRecorderService.cs` (`CombatSourceTests`,
  `DMPossessionPlayerLoopTests`) and `CombatDamageModifiersService.cs` (`MeleeRepeatedTargetStackTests`) were not re-pointed here: the coordinator's list for this PR was the Enmity and Death readers, and the
  recorder and damage modifiers readers move with their PRs (P5-7j1, P5-7d).

### P5-7c as built (2026-11-18)

- **`CombatDamageRollService`** (`ICombatDamageRollService`, 45 lines, 0 services): `CalculateDamage` verbatim, with its default `deltaCap = 0`.
  `ICombatService` loses the member; `CombatService` loses the `CombatRules` and `Random` usings and 36 lines (198 to 162).
- **`CombatDefenseMemoryService`** (`ICombatDefenseMemoryService`, 67 lines, 0 services): the two tables `_recentGuardedHits` and `_recentDeflections`,
  `HasRecentGuardedHit` and `HasRecentDeflection` verbatim, `NoteGuardedHit` and `NoteDeflection` (the one table write cut out of `TrackGuardedHit`
  and `TrackDeflection`), and `ForgetDefenseMemory` (the body of `ForgetDamageTakenState`, which is gone). It reads the clock, so it is in `ClockSeamFiles.txt`.
- **`Lazy<>` parameters removed (2):** `CombatDamageModifiersService -> Lazy<ICombatDamageTakenService>` (plain `ICombatDefenseMemoryService`) and
  `CombatDamageTakenService -> Lazy<ICombatService>` (plain `ICombatDamageRollService`). The third of the three (`CombatHitEffectsService ->
  Lazy<ICombatDamageTakenService>`) is P5-7f1's, as the map says.
- **Departure: `CombatDamageTakenService` is at 21 services, not 20.** It swaps the `Lazy<ICombatService>` for the roll part and also takes the
  defense memory (the two writes of `TrackGuardedHit` and `TrackDeflection`). The `ServicesPerType.txt` line goes from 20 to 21; the guarded-hit
  effects part (P5-7e2) and the effects part (P5-7e1) take the two parts and the count falls with those moves. `CombatService` stays at 12
  (the defense memory replaces damage taken, which it held only for the forget; the roll was never a field of it).
- **Cleanup order unchanged.** `CombatService` calls `_defenseMemory.ForgetDefenseMemory` where it called `_damageTaken.ForgetDamageTakenState` (third).
  `CombatCleanupOrderTests` decorates the defense memory part instead of damage taken and lists it in the same table slot; its fixture changes in the three
  lines that name the call (`DefenseMemory.ForgetDefenseMemory`), every other line is byte-identical.
- **`CombatRecordSerializer.Write` cut:** a dispatcher of 44 lines (the three header writes, then one `switch` that sends each of the 23 labels to a
  method) and four private static methods, each holding the original `case` bodies verbatim: `WriteLifecycleEvent` (3 labels, 24 lines), `WriteAbilityEvent` (6, 54),
  `WriteDamageEvent` (6, 69, with the temporary hit point and healing events) and `WriteStateEvent` (8, 62). The dispatcher is 44 lines, not the
  map's 30, because it lists the 23 labels. The 23 goldens and the 13 variants pass unchanged.
- **Call sites.** By hand, with `sed` on the exact text (the facade tool only rewrites the removed facade's own `Combat.X` text, not the
  `GameServices.Combat.X` that earlier PRs wrote; `RemovedFacades.json` is updated and `-Check` is clean): `GameServices.Combat.CalculateDamage` to
  `GameServices.CombatDamageRoll.CalculateDamage` in 10 ship module definitions, `Native/GetDamageRoll.cs` (2) and the two characterization test
  files (text only, snapshots untouched); `GameServices.CombatDamageTaken.HasRecent*` to `GameServices.CombatDefenseMemory.HasRecent*` in
  `WeaponActiveAbilityDefinitionBase.cs` (4), `CombatPerkRegressionEngineTests`, `TwinPistolEngineTests`, `CombatSourceTests` (2) and
  `CombatFacadeAllocationTests` (2). Holders: `CombatDamageModifiersService` (9 services, unchanged: the `Lazy<>` became the plain part),
  `CombatService` (12, unchanged), `CombatDamageTakenService` (20 to 21).
- **Tests.** `CombatDamageRollServiceTests` and `CombatDefenseMemoryServiceTests` are new (the window, purge and forget tests moved out of
  `CombatDamageTakenServiceTests` and `CombatServiceTests`); the doubles `TestDamageTaken` and `TestDamageModifiers` take the new parts; the
  allocation pins keep their bytes (`ForgetDefenseMemory` 184); `CombatPartForwardingTests` lists the two new interfaces.

### P5-7d as built (2026-11-21)

Branch `claude/mr-p5-7d`, base `38311bdeb` (P5-16f PR 2).

- **`CombatRateModifiersService`** (`ICombatRateModifiersService`, 220 lines, 6 services: queries, stat, statusEffect, statusEffectApplication,
  temporaryStatModifier, playerFeedback): `GetAutoAttackCriticalRateAdjustment`, `GetTargetStatusCriticalRateAdjustment`,
  `PrepareAutoAttackCycleCriticalRate`, `ClearAutoAttackCycleCriticalRateTracker`, the side attack members (`ApplySideAttackDamageModifier`,
  `GetSideAttackHitChanceAdjustment`, `GetSideAttackCriticalRateAdjustment`, `ApplySideAttackEvasionIgnore`), the back attack members and the
  `_autoAttackCycleCriticalCounts` table. One new member, `ForgetAutoAttackCycleCriticalRateCount` (the one table line cut out of
  `ForgetDamageModifierState`).
- **`CombatRepeatedTargetService`** (`ICombatRepeatedTargetService`, 427 lines, 4 services: stat, statusEffect, statusEffectApplication and
  `ICombatRateModifiersService`): the repeated-target, melee, ranged and same-target pressure members, the four state tables, their two nested
  state classes and `ForgetDamageModifierState`. It reads the clock, so it is in `ClockSeamFiles.txt`.
- **`CombatDamageModifiersService`** (stay, 875 lines, 1,436 before; 10 services): the two parts are plain parameters; it keeps
  `GetHighHPTargetCriticalDamageAdjustment` and `IPlayerFeedbackService`, so the map's fallback (which would move it) was not needed.
  `ICombatDamageModifiersService` loses the moved members. `LargeTypes.txt` loses its line (20 to 19 types over 1,000 lines).
- **Departures from the plan.** `CombatRepeatedTargetService` is at 4 services, not 3, because `ForgetDamageModifierState` calls the rate part
  first. Four holders grow by one in `ServicesPerType.txt`: `CombatHitEffectsService` 20 to 21, `CombatAbilityHitService` 12 to 13,
  `CombatService` 12 to 13 and `AbilityDamageInputService` 11 to 12 (each takes the part its calls moved to; `CombatService` takes both and drops
  the damage modifiers). The later splits of those services lower the counts again. `CombatHitEffectsService` is 3 lines longer (1,716 to 1,719).
  `LogicToContent.txt` re-keys the `StatusEffectDefinition` lines (stay 14 to 9, rate 2, repeated target 3).
- **Cleanup order unchanged.** `ForgetDamageModifierState` clears the auto-attack cycle table first, then the repeated-target tables and the
  pressure states, in the old order. `CombatCleanupOrderTests` decorates the repeated-target part; its fixture changes only in the three lines that
  name the call (`RepeatedTarget.ForgetDamageModifierState`).
- **Call sites** by hand with `sed` on the exact text, as in P5-7c: the native attack roll (`AttackRollAccuracy`, `AttackRollCritical`,
  `AttackRollDefense`, `ResolveAttackRoll`), `AbilityDamageDeliveryService`, `AbilityDamageInputService`, `CombatAbilityHitService`,
  `CombatHitEffectsService`, `CombatService`, `RemovedFacades.json` (and the registrations and `GameServices.Combat.cs` for the two parts). The player
  message audit rows moved with the code (6 rows: stay 1, rate 1, repeated target 4).
- **Tests.** `CombatDamageModifiersServiceTests` splits into `CombatModifierTestBase` (the shared bench, 231 lines) and three classes: the stay-part
  24 tests, `CombatRateModifiersServiceTests` 6 and `CombatRepeatedTargetServiceTests` 8. All 37 old test names are kept (the four helper
  methods moved to the base) and `ForgettingTheCycleCountRestartsTheCycleWithoutTouchingOtherCreatures` is new. The doubles `TestDamageModifiers` and
  `TestHitEffects`, `CombatAbilityTranscriptTests`, the allocation pins (`CombatFacadeAllocationTests` re-pointed, bytes unchanged) and
  `CombatPartForwardingTests` take the new interfaces. The transcript fixtures are unchanged except `CleanupOrder.txt`.
- **Proof.**
  - *Moved bodies:* a Roslyn token comparison (modifiers, receivers and the namespace ignored) of every member of the old class against the three
    parts: 80 members identical, the two nested state classes identical, one different. The difference is `ForgetDamageModifierState`, whose first
    statement `_autoAttackCycleCriticalCounts.Remove(creature)` is now `_rateModifiers.ForgetAutoAttackCycleCriticalRateCount(creature)`.
  - *Unit tests* (`SWLOR.Game.Server.Tests`, filters `Combat`, `DamageModifiers`, `RepeatedTarget`, `RateModifiers`, `PlayerMessageAudit`,
    `ConvertedServiceContainer`, `AbilityDamage`, `MeleeRepeated`, `AttackRoll`): 3,196 tests, 3,195 pass and 1 skipped (the walkmesh test needs the
    `sw_t_office` hak folder, which a checkout without the submodule lacks; it passes with the folder). `SWLOR.Architecture.Tests`: 297 pass.
  - *Engine tests* at `38311bdeb` and at the head, host entry, filter `Characterization|RepeatedTargetDamage|StealthRetarget|TwinPistolCompanionArrival|CombatPerkRegression|PerkTracker|TwinPistolPvpCombat|TwinPistolConversion`:
    104 tests each, 104 pass each, identical per-test outcomes, no snapshot file changed.
  - *Client evidence* (map "Tests and the proof plan" said none; taken anyway): one NWN client, the same module, haks and steps at the base and the head,
    a Vibroknife and a rifle against a hostile Outlaw in `veles_sewers`: plain melee, auto-attack cycle critical rate, side attack, back attack, melee
    repeated target, plain rifle, ranged repeated target (Sustained Fire), same-target pressure (Spotter's Rhythm) and rifle cycle critical. Per phase the
    message kinds and counts, and the damage ranges, match (side and back attacks double the plain hit, Repeated Target 1/5 to 5/5 adds +10 per stack,
    Sustained Fire stacks 1/5 to 4/5, the critical rate notice appears 6 and 4 times in both); attack rolls differ by at most two per phase
    from timing. A second run drove the pressure bonus on a weapon ability (`ConsumeSameTargetPressureWeaponAbilityDamageBonus`), same setup at
    the base and the head: with the ready notice up, Aimed Shot I hit with "Spotter's Rhythm (+25 DMG)" (29 and 26 damage), and Crippling Shot I about
    8 s later hit with no bonus (15 and 16 damage, then Hamstring) on both. Near the second cast the base showed a fresh plain "Spotter's Rhythm" ready
    notice, because the pressure had built again since the first hit, and the head did not yet; neither carries a bonus. Takes where a cast missed were
    dropped on both sides, since a miss leaves the bonus in place.

### P5-7e1 as built (2026-11-23)

Branch `claude/mr-p5-7e1`, base `34c3373a0` (the P5-19 record).

- **`CombatDamageTakenEffectsService`** (`ICombatDamageTakenEffectsService`, 599 lines, **10 services**: resources, attackTiming, combatActivity,
  hitEffects, stat, statusEffect, statusEffectApplication, temporaryStatModifier, recorder, defenseMemory): 24 methods, verbatim. The handler
  `ApplyDamageTakenEffects` (both overloads), `ApplyDamageTakenNextSkillAbilityDamage`, `ApplyReversalCutReady`, `ApplyRecentDamageTargetHitEffects`, the low HP
  members (`ApplyLowHPDamageTakenEffects`, the physical defense, evasion, next-ability, temporary HP, no-save temporary HP and guard effects,
  `ApplyLowHPGuardEffectFromProtectedTarget`; `ApplyLowHPTemporaryHPBeforeFatalDamage` stays, as the map's stay list says), `TrackAvoidedAttack` with its three
  helpers, `ApplyMeleeDamageTakenEffects` with the poison, `TrackDeflection` with `ApplyDeflectionNearbyAllyGuard`, `ApplyAbilityUsedAttackDeflection` and
  `ApplyAbilityGrantedAttackDeflectionEffects`. It reads the clock, so it is in `ClockSeamFiles.txt`. The map's fallback (the melee members to the reflection part) was
  not needed.
- **`CombatDamageReflectionService`** (`ICombatDamageReflectionService`, 222 lines, **8 services**: queries, resources, hitEffects, stat, statusEffectApplication,
  recorder, messaging, playerName): 5 methods, verbatim: `ApplyDamageReflectionEffects`, `ApplyForceDamageTakenEffects`, `ApplyIncomingPhysicalToForceConversion`,
  `ApplyRangedDeflectionReflection`, `BuildDeflectingReturnCombatLogMessage`.
- **`CombatDamageTakenService`** (stay, 885 lines, 1,609 before; **17 services**, 21 before): it drops the four services only the moved members used (combatActivity,
  statusEffectApplication, messaging, playerName). 28 methods stay. `LargeTypes.txt` loses its line (19 to 18 types over 1,000 lines). The `ICombatDamageTakenService`
  summary and the two `<see cref>` that named moved members are re-pointed.
- **Handler.** `ApplyDamageTakenEffects` is renamed in place: `ScriptHandlerOrder.txt` line 345 and `HandlerRenames.txt` line 146 now name
  `CombatDamageTakenEffectsService` (event `OnCreatureDamagedAfter` and position unchanged). The map's "line 344" is 345 on this base. No classification file names the class.
  `RemovedFacades.json` re-points the 11 moved member names to the new owners (`CombatDamageTakenEffects` 7, `CombatDamageReflection` 4).
- **Departures from the map.**
  - `CombatAbilityImpactService` goes from 16 to 17 services: it reads both the stay-part (`ConsumeGuardedHitNextSkillAbilityExposedDamageBonus`) and the effects part
    (`ApplyAbilityGrantedAttackDeflectionEffects`). The map's holder table does not list it. The impact split (P5-7h1 and P5-7h2) takes it to 10.
  - `Native.ResolveAttackRoll` is a new line in `ServicesPerType.txt` at 11 (lookup): it called `CombatDamageTaken` twice (`TrackAvoidedAttack`,
    `ApplyRangedDeflectionReflection`) and now calls the effects part and the reflection part. P5-16b (hook logic out of the native sinks) removes it.
  - `AbilityDamageInputService` is 13 and `AbilityDamageStageService` 11, as the map's holder table says (the reflection part for the force conversion); P5-16 removes both.
  - Swaps with no change in count: `AbilityDamageDeliveryService` (8, the reflection part replaces damage taken), `CombatAbilityActivationService` (14) and
    `CombatDeflectionReactionService` (7) and `WeaponAbilitySelfEffects` (the effects part replaces damage taken).
  - `PlayerMessageAudit.json`: 4 rows keyed by the old file are re-keyed by hand (Guardian Reflexes to the effects part, Deflecting Return to the reflection part, the two
    guard rows keep the stay-part with their new lines) and sorted into place; `ExportPlayerMessageAudit.ps1 -Check` was not run by the agent.
- **Call sites** by hand with a literal receiver rewrite (`CombatDamageTaken.X` to `CombatDamageTakenEffects.X` or `CombatDamageReflection.X`, 43 in production, engine and
  unit test code) and constructor edits: `AbilityDamageDeliveryService`, `AbilityDamageInputService`, `AbilityDamageStageService`, `CombatAbilityActivationService`,
  `CombatAbilityImpactService`, `CombatDeflectionReactionService`, `WeaponAbilitySelfEffects`, the native hooks (`AttackRollSettlement`, `DamageRollCreature`,
  `DamageRollMitigation`, `GetDamageRoll`, `ResolveAttackRoll`), `PlayerStatusWindow`, `GuardedStatusEffect`, `HeavyVibrobladeActiveAbilityDefinitionBase`.
  Registrations and `GameServices.Combat.cs` for the two parts; `ConvertedServices.json` lists them (22 parts).
- **Tests.** `CombatDamageTakenServiceTests` splits into `CombatDamageTakenTestBase` (the shared bench: tables, fakes, the three services, 434 lines) and three classes:
  the stay-part (57 test methods), `CombatDamageTakenEffectsServiceTests` (27) and `CombatDamageReflectionServiceTests` (9). All 93 old test names are kept (the fixture
  methods moved to the base), and `Private` gains a target for the one private member the effects tests reach. `TestDamageTaken` loses the four parameters. The ability,
  deflection and stat doubles take the new parts; `CombatPartForwardingTests` lists the two interfaces and re-points the moved defaults, the activity-table and handler checks;
  `CombatFacadeAllocationTests` re-points the part side (every pinned byte count is unchanged); `CombatDamageTakenTranscriptTests` calls the effects and reflection parts and its
  fixture is unchanged.
- **Proof.**
  - *Moved bodies:* a Roslyn token comparison (modifiers, receivers and the namespace ignored) of every method of the old class at `34c3373a0` against the three classes: 57
    methods, 57 identical, 0 different, 0 missing (24 in the effects part, 5 in the reflection part, 28 in the stay-part).
  - *Unit tests* (`SWLOR.Game.Server.Tests`, one filter on `CombatDamageTaken`, `CombatDamageReflection`, `CombatDeflection`, `CombatPartForwarding`, `CombatFacadeAllocation`,
    `CombatSource`, `PlayerMessageAudit`, `ConvertedServiceContainer`, `AbilityDamage`, `Combat`, `ScriptHandlerOrder`, `Handler`, `Reflection`, `Deflection`,
    `AbilityServiceDefaultSeams` and `StatServiceDefaultSeams`): 3,420 tests, 3,417 pass, 2 fail and 1 skipped after the hak data the tests read was copied read-only from the main
    checkout (`sw_2da`, `sw_tlk`, `sw_ability`, `sw_shader`, `sw_ui`, `sw_tint_mtr`, `sw_item`, `sw_weapon`, `sw_t_office`). The two failures read hak data that is not the pin's:
    the two `CombatUpgradeBibleSyncTests` (Beast Mastery stance and Focus Attention TLK text against the Bible) fail on the main checkout's newer hak data, not on code this item
    touches (`FightClubArena_HasAContinuousCombatFloorThroughTheFormerPit` needs `sw_t_office` and passes with it). A second run of the handler, reload, classification,
    cycle, audit and cleanup order guards (`ParityHandlerMapping`, `BootHandlerClassification`, `PlayerSessionClassification`, `ReloadStateCarrier`, `ReloadCarryHistory`,
    `ReloadDigest`, `ClockSeamSourceGuard`, `LazyDependencyCycle`, `PlayerMessageAudit`, `ScriptHandlerOrder`, `CombatCleanupOrder`, `FightClubArena`): 143 tests, 143 pass.
    `SWLOR.Architecture.Tests`: 297 tests, 3 baseline tests failed before the regeneration (`LargeTypes`, `ServicesPerType`, `LogicToContent`) and every test passes after it
    (`ConvertedServiceRulesTests` and the edge files included; `CrossDomainEdges.txt` and `EdgesIntoCoreAndPlatform.txt` did not change).
  - *Baselines changed:* `LargeTypes.txt` (`CombatDamageTakenService` 1,609 leaves; `CombatAbilityImpactService` 1,720 to 1,723), `ServicesPerType.txt` (`CombatDamageTakenService`
    21 to 17, `CombatAbilityImpactService` 16 to 17, `AbilityDamageInputService` 12 to 13, new `AbilityDamageStageService` 11 and `Native.ResolveAttackRoll` 11), `LogicToContent.txt`
    (106 to 108 lines: the stay-part loses its `Feature.UsePerkFeat` and `Feature/StatusEffectDefinition` lines; the effects part gains `Feature.UsePerkFeat` 1,
    `Feature/AbilityDefinition` 1 and `Feature/StatusEffectDefinition` 4; the reflection part `Feature/StatusEffectDefinition` 2), `DebtReport.md`.
  - *Engine tests* at `34c3373a0` and at the head, host entry, filter
    `Characterization|CombatPerkRegression|PerkTracker|ReviveRules|UnarmedMastery|DualWield|SaberWard|TwinPistol|StealthRetarget|Deflection|Reflection`: 187 tests each. Head: 187 pass.
    Base: 186 pass and 1 fails, `A nonhostile Twin stance starts during the hostile delay and keeps its normal cast time` (a timing assertion, 614 ms into a delay it expects to be
    inside, in a run that shared the machine with the head run, and the hot reload check later); the category (`TwinPistolHostileDelay`, 4 tests) rerun alone at the base passed 3 of 4 the first time (another test of it hit the same timing assertion) and 4 of 4 the second time, so every per-test outcome is identical once the load is off. No characterization snapshot was written on either side.
  - *Hot reload:* `run-hot-reload-check.sh --engine-filter HotReloadPlayers --reloads 2` at the head (the handler moved): both reloads applied, the engine run of every generation passed
    (`A player online across hot reloads keeps HP, effects, status effects, cooldowns and position`), 253 cache digests matched the boot and 86 adopted and world digests matched the
    build each reload replaced, for each reload. Not run at the base.
  - *Client evidence* (map "Tests and the proof plan" said none; taken anyway because the parts drive combat messages and effects): one NWN client, the same module, haks, build
    flags, character and driver steps at the base (a Release build of `34c3373a0`) and the head, in `veles_sewers` at `SEWERS_GATE` after a jump from `ooc_area`, with spawned hostile
    Outlaws (`looter_1`, ranged rifle attackers) and Bandits (`nw_bandit001` with a katar, melee). A temporary driver built outside the repository (`CustomAfterMicrosoftCommonTargets`)
    gave the player a katar and, per step, temporary stats for 36 s; it healed the player between steps except in the low HP step. Eight steps of 40 s: damage taken riders (attack
    percent, Reversal Cut), low HP (physical defense, temporary hit points, evasion at 60 percent), melee damage taken (poison back, stamina restore), avoided attacks (evasion),
    melee deflection, physical damage reflection (60 percent), the physical-to-Force conversion (50 percent) and ranged deflection with Deflecting Return. The client chat log is split
    by step and compared by message kind (digits removed). The kinds match step for step; the only differences are two critical-hit lines (one on each side). Counts differ by the dice, as in
    the earlier captures: low HP shows "Unbreakable" 10 and 7 times and temporary-hit-point absorption 18 and 14 (base and head); melee damage taken shows the poison damage to the
    Bandit 15 and 17 times; melee deflection negates 4 and 8 attacks; physical reflection damages the Outlaw 13 and 13 times; the conversion adds Force damage lines 19 and 16 times;
    ranged deflection negates 4 and 3 attacks and Deflecting Return reflects Force damage to the Outlaw 3 and 2 times (the same line on both sides). No step differs in kind. The damage-taken handler runs for a player through `PlayerStatusWindow` (a player's damage-taken effects come from that window's
    hook), so the driver's own damage counters read 0 for the player on both sides; the effects were read from the chat. The screenshots show a two-line chat panel; the
    normalized chat is the evidence for counts. Screenshots and the normalized chat are in `C:/Temp/mr-p5-7e1/evidence/{base,head}/` (kept outside the repository).
  - *Not run:* the `-Check` scripts (content, facade, clock) and `ExportPlayerMessageAudit.ps1 -Check`, which the agent rules leave to the owner; `CombatSim` (this change touches no rule).

### P5-7i1 as built (2026-11-23)

Branch `claude/mr-p5-7i1`, base `766f0475d` (the P5-7e1 record).

- **`CombatAbilityDefenseEffectsService`** (`ICombatAbilityDefenseEffectsService`, 403 lines, 7 services: queries, resources, damageTakenEffects, stat,
  statusEffectApplication, temporaryStatModifier, perk): 13 methods, verbatim. The 11 steps the orchestrator calls are public: `ApplyAbilityUsedSkillEvasion`,
  `ApplyHostileAbilityUsedEvasion`, `ApplyCostlyAbilityUsedEvasion`, `ApplyAreaAbilityUsedEvasion`, `ApplyAbilityUsedSkillRangedDeflection`,
  `ApplyAbilityUsedSkillAttackDeflection`, `ApplyAbilityUsedPerkCategoryAttackDeflection`, `ApplySingleTargetAbilityUsedAttackDeflection`,
  `ApplyAbilityUsedNearbyAllyDefense`, `ApplyAbilityUsedPerkCategoryNearbyAllyAttackDeflection` and `ApplyAbilityUsedPerkCategorySelfDefense`. The two helpers
  (`ApplyAbilityUsedSkillEvasionChannel`, `ApplyAbilityUsedEvasion`) stay private. It holds no state and reads no clock.
- **`CombatAbilityCostEffectsService`** (`ICombatAbilityCostEffectsService`, 222 lines, 8 services: queries, resources, attackTiming, stat, statusEffectApplication,
  temporaryStatModifier, recast, combatRecorder): 9 methods, verbatim. Public: `ApplyHitPointSpendAbilityEffects`, `ApplyAbilityUsedRecastReduction` (the
  `AbilityDetail` overload; the `RecastGroup` overload is private), `ApplyAbilityUsedNextSkillAutoAttackDamageBonus`, `ApplyAbilityUsedNextSkillFPCostAdjustment`,
  `ApplyAbilityUsedMasterAbilityHitChance` and `ApplyForceFPCostActivatedEffects`. Private: the HP spend stamina restore and temporary hit points. It holds no state and
  reads no clock.
- **`CombatAbilityActivationService`** (stay, 675 lines, 1,203 before; **14 services**, 14 before): the orchestrator `ApplyAbilityActivatedEffects` keeps its 24 steps in
  the original order and calls 5 of them on the cost part and 11 on the defense part; the other 8 members still run here until P5-7i2. It drops the two services only
  the moved members used (perk, recast) and takes the two parts, so the count does not move; the map's 7 is reached when P5-7i2 moves the rest. `LargeTypes.txt` loses
  its line (18 to 17 types over 1,000 lines). `ICombatAbilityActivationService` loses `ApplyHitPointSpendAbilityEffects` and its summary is re-pointed.
- **Handlers and state.** None moved: no `[NWNEventHandler]`, no `[ReloadState]` field and no carried key is in either part, so `ScriptHandlerOrder.txt`,
  `HandlerRenames.txt`, `BootHandlerClassification.txt`, `PlayerSessionClassification.txt`, `ClockSeamFiles.txt` (neither part reads the clock) and the reload files do
  not change, and the hot reload check was not run.
- **Departures from the map.** The stay-part stays at 14 services (see above). The map's "6 call sites" are the two `ApplyHitPointSpendAbilityEffects` callers
  (`HeavyVibrobladeActiveAbilityDefinitionBase`, `WeaponAbilitySelfEffects`, which now injects the cost part instead of the activation part) and four test sites;
  `ApplyAbilityActivatedEffects` and `TrackQueuedWeaponAbilityUse` callers (`UsePerkFeat`, engine tests) do not change. `RemovedFacades.json` gains the owner
  `CombatAbilityCostEffects` (`ApplyHitPointSpendAbilityEffects`) and loses that name from `CombatAbilityActivation`. No holder grows.
- **Tests.** `CombatAbilityActivationServiceTests` splits into `CombatAbilityActivationTestBase` (the shared bench, 327 lines; the two parts are real and built
  from the same scripted leaves, so the order test still goes through them) and three classes: the stay-part 26 tests, `CombatAbilityCostEffectsServiceTests`
  9 plus a construction test, `CombatAbilityDefenseEffectsServiceTests` 9 plus a construction test. All 44 old test names are kept (`Private` takes a target). Both new
  interfaces are in `CombatPartForwardingTests.Parts()` (the HP spend default moved to the cost part), `WeaponAbilityTraceBench.Leaves`, and `CombatAbilityTranscriptTests` and
  `CombatFacadeAllocationTests` call the cost part for the HP spend (every pinned byte count is unchanged). `WeaponAbilityCallTrace.txt.gz` is regenerated: 208 lines
  change, all the label `AbilityActivation.ApplyHitPointSpendAbilityEffects` to `AbilityCostEffects.ApplyHitPointSpendAbilityEffects` (arguments and line count identical).
  Source readers that searched for `private static void <member>` (`GeneratedWeaponPerkBehaviorTests`, `CrossSkillPerkInteractionSafetyTests`,
  `ForceSensitiveEspionageTests`) search `static void <member>`, because the moved steps are public and `CombatSource` restores `static` only. `ConvertedServicesTests`,
  `ConvertedServices.json` and `ConvertedServiceContainerTests` list 24 parts (the container test is renamed from twenty_two to twenty_four).
- **Proof.**
  - *Moved bodies:* a Roslyn token comparison (modifiers, receivers and the namespace ignored) of every method of the old class at `766f0475d` against the three
    classes: 45 methods, 45 identical, 0 different, 0 missing (13 in the defense part, 9 in the cost part, 23 in the stay-part; the orchestrator is identical because only its receivers changed).
  - *Unit tests:* `SWLOR.Game.Server.Tests`, one filter on `Combat`, `AbilityActivation`, `Ability`, `Evasion`, `Deflection`, `CallTrace`, `Handler`,
    `PlayerMessageAudit`, `ConvertedServiceContainer`, `ScriptHandlerOrder` and `Reflection` (it takes in `CombatPartForwarding`, `CombatFacadeAllocation`, `CombatSource`, the
    transcripts and `CombatCleanupOrder`): 4,292 tests, 4,287 pass, 1 skipped and 4 failed on the first run. The four were this change's own reader edits and one data gap:
    `ConvertedServiceContainerTests` (22 parts, now 24), `WeaponAbilityCallTraceTests` (the labels above), `GeneratedWeaponPerkBehaviorTests.SnapRoll_...` (it searched for
    `private static void`) and `AbilityImpactVisualEffectTests` (needs `sw_vfx`). After the edits and the hak data, those classes and the other source readers
    (`CrossSkillPerkInteractionSafetyTests`, `ForceSensitiveEspionageTests`, `LeadershipOffensivePartyBuffTests`) pass: 841 tests. The unfiltered run of the whole project (19,957
    tests, 21 minutes) has 19,887 passing, 2 skipped and 68 failing. Sixty of the 68 read hak folders that were not in the nine copied for the first runs (`sw_t_*`, `sw_pt_*`,
    `sw_cr_creature`, `sw_plc*`, `sw_tint0`, `SWLOR_Haks/tools`) and pass once the main checkout's whole hak set is copied read-only (a different, newer haks commit than the pin;
    the folders were copied for the run and removed). The other 8 fail with that data and fail identically at `766f0475d`, run in a scratch worktree with the same data:
    `EshanBlueprints_AreDiscoverableInTheirToolsetPalettes("uti","itempalcus")` (`esh_captain_o` is not in the palette), `KillRewards_FollowExistingBountyRatesRatherThanIntroducingAHigherTier`
    (12 quests expected, 32 found), `EveryTileModelShipsAWalkmesh("sw_t_duct","nac01")`, three `LargeBodyRobeModelsRemainAvailable` cases (this hak set has fewer than 1,241
    phenotype-22 robe models) and two `SeatsOpeningTowardModelPlusY_SeatSittersFacingTheSeatFront` cases (`AshmarkWorkStool`, `CommandlineWorkStool`: no use node in the walkmesh).
    None of the 68 touches the moved code.
  - *Architecture:* `SWLOR.Architecture.Tests`: 297 tests; `LargeTypes`, `LogicToContent` and `UpwardEdges` failed before the regeneration with `SWLOR_UPDATE_SNAPSHOTS=1` and every test
    passes after it. Baselines changed: `LargeTypes.txt` (`CombatAbilityActivationService` 1,203 leaves; 18 to 17 types), `LogicToContent.txt` (107 to 110 lines: the stay-part
    `Feature/StatusEffectDefinition` 12 to 5; the cost part `Feature/AbilityDefinition` 1 and `Feature/StatusEffectDefinition` 3; the defense part `Feature/StatusEffectDefinition` 4),
    `UpwardEdges.txt` (115 to 116: the stay-part `Combat -> Abilities` 2 to 1, the cost part 1) and `DebtReport.md` (the same figures). `ServicesPerType.txt` does not change (the
    stay-part is at 14 before and after; the new parts are at 7 and 8). The regeneration also rewrote member counts in `CrossDomainEdges.txt` and `EdgesIntoCoreAndPlatform.txt`
    (every changed line is an unchanged edge with a new count; no edge appears or goes); both are reverted, and the two `DebtReport.md` figures that come from them are kept at the committed values.
  - *Engine tests:* host entry, filter `Characterization|CombatPerkRegression|PerkTracker|RifleIdleReadiness|AbilityAttackResume|ReviveRules|SaberWard|TwinPistol|StealthRetarget|Deflection|Evasion|HitPoint|Stance|UnarmedMastery|DualWield|Reflection`
    (it takes in the ability activation riders through `PerkTracker`, `Stance` (the Vigor and Tempest Stance riders), `HitPoint` (HP-cost abilities), `RifleIdleReadiness` and the
    characterization scenarios), run from scratch copies of the runner with a scratch server home, at `766f0475d` (a scratch worktree) and at the head: 240 tests each, 239 pass and 1 fails
    on each side. The failures differ: the head failed `Dual wield commanded attacks resolve both hands on one shared cadence` (the off-hand hit does not follow the main-hand swing inside the
    delay cycle, a timing assertion) and the base timed out `Twin Pistol stance damage costs and Haste reach native saber and rifle attacks without paired handling` (180 s) while another
    session's stack was running. The two tests rerun alone on each side: Twin Pistol passes on both, Dual wield fails on both with the same message (it passed in the base's first run), so
    the per-test outcomes are identical and the Dual wield failure is the test's timing on this machine, not a difference. No characterization snapshot changed on either side. A wider
    first run (the substring `Ability`, which takes in every ability behavior case) was stopped after 319 passes and no failure, because it would have taken about 80 minutes a side. The hot
    reload check was not run (no handler or reload state moved).
  - *Client evidence:* one NWN client, the same module (`Star Wars LOR v2.mod` of the main checkout's deploy home), haks, build flags, character (Agan Tarn) and driver steps at the base (a Release
    build of `766f0475d`) and the head, in `veles_sewers` at `SEWERS_GATE` after a jump, with the player set plot. A temporary driver built outside the repository
    (`CustomAfterMicrosoftCommonTargets`; the base copy differs only in the receiver of the HP spend call) takes steps from a command file. The test character owns none of the perks that
    set these riders up, so the driver sets the stats those perks set (temporary stat modifiers for 90 s) and calls the activation entry `UsePerkFeat` calls, `ApplyAbilityActivatedEffects`,
    with the catalog abilities Aimed Shot I (rifle, hostile, single target, stamina cost) and Force Spark I (a Force power with an FP cost), and `ApplyHitPointSpendAbilityEffects`; it
    reads the stats and status effects back into the chat. Hostile Outlaws (`looter_1`, ranged) and Bandits (`nw_bandit001`, melee) attack the player (the enemies are plot flagged), and one
    Bandit joins the party as the ally. Steps: D1 evasion (hostile, costly and skill evasion), D2 deflection (attack, single-target, ranged with its icon, perk category), D3 nearby ally defense,
    self guard and nearby ally deflection, C1 recast reduction, C2 next-skill auto-attack bonus, C3 the Force FP cost riders, C4 the HP spend riders. Of the 19 driver readouts, 13 are identical
    on both sides, among them every one taken right after an activation: evasion 0 to 50 (Evasive Footwork), melee deflection 0 to 120 with the Ranged Deflection effect, the ally's physical
    defense 0 to 25 and Force defense 0 to 15 with Guarding Step and Sentinel Guard, the recast of 1 minute 58 seconds at 22 seconds after two 40-second reductions, Danger Sense and Force
    Convergence (evasion 25, ability hit chance 5), and the HP spend (hit points 190 to 210, the 50 percent temporary hit points of 40 spent; stamina 0 to 13; Soul Sacrifice). The six that differ
    (`D2_end`, `C1_before`, `C1_after`, `C4_before`, `C4_after`, `C4_end`) differ only in which effects of the previous step had expired (the steps were fed with different pauses) and in the recast
    countdown. `C1_after` was read before the activation ran (the driver runs it in a module script context), so the recast reduction shows in `C2_after_attacks`, which is identical.
    The combat lines match by kind step for step apart from dice: Outlaw and Bandit misses, hits and critical hits, "ranged deflect negates" (2 and 6 over the run), the five effect icons,
    "worn off" lines (20 and 23); the screenshots show the same five status icons on both sides. Not covered in the client: the master hit chance rider (it needs a companion activator) and the
    area evasion step (no area ability was driven); both have unit tests. The driver source, the steps, the readouts, the normalized chat and the screenshots are in
    `C:/Temp/mr-p5-7i1/evidence/{base,head,driver,proof}/` (kept outside the repository).
  - *Not run:* the `-Check` scripts (content, facade, clock) and `ExportPlayerMessageAudit.ps1 -Check`, which the agent rules leave to the owner; `CombatSim`
    (this change touches no rule); the hot reload check (no handler or reload state moved). `PlayerMessageAudit.json` has no row for the activation files and the moved code sends no player message, so it is unchanged.
