# Maintainability Refactor: Ability Split (P5-3 part map)

The plan for splitting `Domain/Abilities/AbilityService.cs` (4,572 lines, 31 constructor parameters, 78 `IAbilityService`
members including 9 script handlers, and one reload hook). Status lives in [MaintainabilityRefactorStatus.md](MaintainabilityRefactorStatus.md) (row P5-3).
Targets and rules: [MaintainabilityRefactorPhase5.md](MaintainabilityRefactorPhase5.md) (target 2: at most 10 services per
type and under 1,000 lines; target 3: no method over 150 lines; target 4: no deferred resolution) and
[MaintainabilityRefactorDomainOrder.md](MaintainabilityRefactorDomainOrder.md) (the supply rule and the P5-3 rows). This file
goes away at cutover.

Line ranges are for `Domain/Abilities/AbilityService.cs` at `1f052cb3c` (branch `feature/maintainability-refactor`), from a
token scan and the compiler-free tallies below, not from the compiler. A range starts at the doc comment of its first member.
The first pull request confirms them by building and comparing sorted lines, as the Combat split did. Dependency counts are
the distinct services each range reaches, through its fields and its `protected virtual` seams (lines 4081-4272); the part's
own new dependencies are added on top. If master changes the file first, map the ranges through the diff.

## What the code is

`AbilityService` is one file with seven jobs and no clean seams between them:

| Job | Lines | Size |
|---|---|---:|
| Catalog of ability definitions | 161-223, 4081-4087 | 70 |
| Impact tracking: the tracker class, begin, capture, end, abort, summaries, reads | 225-650, 699-713, 811-870, 4273-4572 | about 1,000 |
| Activation checks (`CanUseAbility`, 170 lines) and the line-of-sight and shape helpers | 871-1133 | 263 |
| Auras and the Leadership combat point (8 handlers) | 1134-1803 | 670 |
| Applying impacts: `ApplyCombatImpact`, telegraphed, hostile, damage, status effects, messages, Dark Force | 1804-3910 | 2,107 |
| Temporary immunities | 3911-4036 | 126 |
| Activation and queued-attack state, the reload quiesce hook | 4037-4080 | 44 |

The `protected virtual` seams (4081-4272, about 100 one-line methods) are the test doubles' hooks. They move with the code that
calls them. The 31 constructor parameters are 22 plain services and 9 `Lazy<>` (8 Combat services and `ISpaceService`).

Three methods are over 150 lines and must be split in the move: `ApplyTelegraphedCombatImpact` (1931-2133, 197),
`CanUseAbility` (899-1069, 170) and `BuildTelegraphedCombatImpactAction` (2237-2388, 151). No other method in the file is over
150.

## Decisions

The coordinator answered the four open questions of this map on 2026-11-15:

1. **`TwinPistolAbilityService` moves from Combat to Abilities,** in P5-3f.
2. **`IPartyRoster` is owned by Combat,** so P5-7 reuses it for Combat's own party reads.
3. **`PistolAnimationRemap`, `TwinPistolEquipmentValidation` and `UsePerkFeat` stay** behind their seams for P5-12 and P5-16.
4. **The deviation from the domain-order row is accepted:** the activation and queued-attack state stays in Abilities,
   because nothing lower reads it.

The decisions of the map:

- **The content-facing name stays.** `AbilityService` and `IAbilityService` stay as the part that holds `ApplyCombatImpact`
  (72 content call sites), `ApplyTelegraphedCombatImpact` (63) and `PlaySuccessfulImpactVisualEffect` (81), and with four smaller ones 238 of the 380
  server content and native call sites. Content text for them does not change, `GameServices.Ability` stays, and
  `WeaponImpactReader` in CombatSim (which looks for `GameServices.get_Ability` and `IAbilityService.ApplyCombatImpact` in IL)
  needs no change for them.
- **No forwarding layer.** A moved member leaves `IAbilityService` and its callers change in the same pull request. The
  call text of a moved member changes only in the property name (`Ability.GetAbilityDetail` becomes
  `AbilityCatalog.GetAbilityDetail`), which a script does and `tools/Refactor/MasterCopy.ps1` and
  `Apply-FacadeRemoval.ps1 -Reverse` map back (see Callers).
- **Combat gets a store and three small services, not callbacks.** The state Combat reads is pushed into a Combat-owned
  class (rule 1) and the code Combat shares with Abilities (shape geometry, magnitude scaling) moves down into Combat (rule
  2). Nothing in Combat injects an Abilities type afterwards, except what other items own (`RecastService` ->
  `IAbilityCooldownVisualService`, `ResistanceService` -> `IMimicryService`, and the `AbilityRequirementStamina` type in
  `CombatResourcesService`).
- **The activation state stays in Abilities.** The domain-order row says activation and queued-attack state go to a
  Combat-owned store. At `1f052cb3c` nothing in `Domain/` or `Native/` reads them: only `UsePerkFeat` and other content
  (32 sites), tests and engine tests do. They stay in one small Abilities part. If P5-16 moves `UsePerkFeat` into logic, the
  part moves with it.
- **`TwinPistolAbilityService` moves from Combat to Abilities** (328 lines). It is the one Combat service that applies ability
  impacts (`ApplyCombatImpact`, `CaptureRepeatedAbilityImpact`, `PlayAbilityImpactAnimation`), so it is an Abilities
  service sitting below the service it calls. Without the move Combat keeps 6 members pointing up (coordinator answer 1).
- **Moved code stays verbatim** except for how it reaches its dependencies and the three method splits above. The
  `protected virtual` seams move with their callers, so each part gets its own test double.
- **A hit-roll and a damage-stage split is needed** because `ApplyHostileCombatImpact` and the damage formulas reach 11 to 15
  Combat services as one unit. They split along the stages the code already has (see Parts).

## Static fields

`[ReloadState]` is the hot reload contract (`ReloadCarryContractResolver`). Each field keeps its kind and moves with its owner.

| Field | Lines | Kind | Owner |
|---|---|---|---|
| `_abilities` | 142 | none (rebuilt by `CacheData`; `ReloadWiring.cs:28` reads it for `AbilityDetailReloadConverter`) | Catalog |
| `_trackedAbilityImpacts` | 146 | Runtime | Impact state |
| `_lastCompletedImpactSummaries` | 589 | Runtime | Impact state |
| `_playerAuras` | 143 | Carried | Aura |
| `_lastActivationDenial` | 871 | Runtime | Activation |
| `_activeAbilityActivations`, `_queuedWeaponTraits`, `_queuedWeaponAttacks` | 67-72 | Runtime | Activation state |
| `HardCrowdControlImmunityTypes` (static readonly `FrozenSet`) | 3911 | none | Immunity |

The `[PlayerSession]` reasons on `ApplyAuraAOE` and `ClearAurasOnExit` name `AbilityService._playerAuras` in their text and change
with the move. The one `[ReloadHook(Quiesce)]` (`QuiesceForReload`, 4051) moves to the activation state part.

## Parts

Services are `Domain/Abilities/<Name>` behind `I<Name>`, unless the owner column says otherwise. "Deps" counts the part's
constructor parameters after the move (at most 10, target 2). Lines are the moved ranges plus the part's seams and constructor,
so they are approximate to about 15%. Handlers are in **bold**.

| # | Part | Interface owner | Ranges | Lines | Deps | Runs / state |
|---|---|---|---|---:|---:|---|
| 1 | `AbilityCatalogService` | `IAbilityCatalog` in Character | 161-223, 4081-4087 | 85 | 1 | **`CacheData`** (`OnModuleCacheBefore`, boot rerun); `_abilities` |
| 2 | `AbilityImpactState` | `IAbilityImpactState` in Combat; its read half `IActiveAbilityImpact` in Character | 358-366, 388-418, 588-589, 598-617, 631-649, 699-713, 811-870, 4273-4500, 4549-4572 | 410 | 0 | per hit; `_trackedAbilityImpacts`, `_lastCompletedImpactSummaries` |
| 3 | `CombatImpactGeometryService` | Combat | 1070-1075, 1110-1133, 2480-2700 | 255 | 1 | per activation and per impact |
| 4 | `CombatAbilityMagnitudeService` | Combat | 714-794 | 90 | 3 | per heal and per impact |
| 5 | `AbilityImpactLifecycleService` | Abilities | 225-357, 420-547, 548-587, 590-597, 618-630 | 340 | 10 | begin and end of every activation |
| 6 | `AbilityActivationService` | Abilities | 871-956, 965-990 | 120 | 8 | per activation attempt; `_lastActivationDenial` |
| 7 | `AbilityTargetCheckService` | Abilities | 957-964, 991-1064, 1070-1109 | 140 | 4 | per activation attempt |
| 8 | `AbilityAuraService` | Abilities | 1134-1803 | 690 | 4 | **8 handlers**; `_playerAuras` |
| 9 | `AbilityService` (stays) | `IAbilityService` | 651-698, 1804-2479, 2701-2777 | 880 | 10 | per impact |
| 10 | `AbilityHitRollService` | Abilities | 2998-3043 | 70 | 8 | per target |
| 11 | `AbilityTargetImpactService` | Abilities | 2970-2997, 3044-3084 | 90 | 8 | per target |
| 12 | `AbilityHostileImpactService` | Abilities | 795-810, 2778-2811, 2850-2969, 3085-3127 | 235 | 10 | per target |
| 13 | `AbilityDamageDeliveryService` | Abilities | 2812-2849, 4501-4548 | 105 | 9 | per target; per end of impact |
| 14 | `AbilityDarkForceService` | Abilities | 3212-3309 | 110 | 4 | per damaging hit |
| 15 | `AbilityImpactMessageService` | Abilities | 3170-3211 | 55 | 3 | per target |
| 16 | `AbilityDamageInputService` | Abilities | first half of 3310-3766 (3310-3351, the inputs of 3352-3492, 3493-3567, the inputs of 3568-3718, 3719-3766) | 250 | 8 | per target |
| 17 | `AbilityDamageStageService` | Abilities | second half of 3310-3766 (the stages of 3352-3492 and 3568-3718) | 230 | 9 | per target |
| 18 | `AbilityStatusApplicationService` | Abilities | 3128-3169, 3767-3910 | 200 | 4 | per target |
| 19 | `AbilityImmunityService` | Abilities | 3911-4036 | 140 | 1 | per status effect |
| 20 | `AbilityActivationStateService` | Abilities | 4037-4080 | 60 | 0 | **`QuiesceForReload`** (reload hook); the three activation dictionaries |

Nothing is over 1,000 lines. The largest are the aura part (690) and `AbilityService` (880 with the three long methods split).
The dependency counts for parts 5 to 7, 10 to 14 and 16 to 18 come from tallying the fields and seams in the ranges with a
script when the map was written; parts 16 and 17 are the least certain, see the notes.

Moves outside `AbilityService.cs`:

- `TwinPistolAbilityService` and `ITwinPistolAbilityService` go from `Domain/Combat` to `Domain/Abilities` (coordinator answer 1).
- `AbilityImpactSequence`, `CombatImpactAreaShape` and `CombatImpactShapeGeometry` go from `Domain/Abilities` to `Domain/Combat`
  (Combat reads the sequence; the shape types belong to the geometry part). `AbilityImpactVisualEffects` and
  `AbilityQueuedWeaponAttack` stay.
- `AbilityEffectScaling` and the target-query half of `AbilityTargeting` (lines 87-335) move from `Feature/AbilityDefinition` to
  `Domain/Combat` as static classes with the namespace changed only (the domain-order guide gives "the ability helpers" to P5-3).
  Content call text is the same; the files need the new `using` (178 content files, 25 `Domain/` files, tests). Their cache
  half (`CacheData`, `RefreshClientTargeting` and its 3 handlers, lines 18-85) moves into `AbilityTargetingCacheService`,
  which already owns the cache. `AbilityEffectScaling` calls `Ability.ApplyActive...` and `ApplyCombatReadiness...`, which
  become `CombatAbilityMagnitudeService` calls.
- `PartyService` loses its roster (see the edges), `SpaceService.cs` gains a tiny query class, and `MusicService` and
  `CapstoneProofService` gain one interface each.

### Notes on the parts

- **1. Catalog.** The body of `AbilityTargeting.CacheData` becomes `AbilityTargetingCacheService.CacheData`, which the catalog
  calls after loading; that removes the catalog's reference to `Feature/AbilityDefinition`. `AbilityAnimationBinding.Apply` stays
  in the catalog. The 3 `Lazy<IAbilityService>` inside Abilities (`MimicryService`, `QuickCastService`,
  `AbilityCooldownVisualService`) and the down ones in `AIService`, `CompanionControlService`, `CapstoneProofService` and
  `PartyService` read only the catalog (and `CanUseAbility`), and become plain parameters once the up half is gone.
- **2. Impact state.** `TrackedAbilityImpact` becomes a Combat class (`AbilityImpactTracker`); the store holds the two
  dictionaries and answers the reads: `GetActiveAbilityImpactSummary`, `GetAbilityImpactSequence`,
  `GetWeaponTraitAccuracy`, `GetRemainingDamageTargetHP`, `IsAreaImpact` (both), `IsSingleTargetImpact`,
  `TryTriggerAreaAbilityPulse`, `TryQueueTrackedDamageEffect`, the last-completed summary pair and
  `AddActiveAbilityDefenseIgnorePercentAdjustment`. It has no service parameters. Three tracker methods reached back into the
  service, and each moves to the caller: the stat-source cache takes the `IStatService` as an argument
  (`GetAbilityImpactStatSources(activator, payload, stat)`; both Combat callers already hold it), the recorder scope for
  `QueueDamageEffect` is passed in by the caller (`CombatHitEffectsService` and the delivery part hold the recorder), and
  `ConsumeStatusAppliedNextAttackDamageBonus` and `FlushDamageEffects` (4501-4548) move to the hostile and delivery parts.
  `RecordSuccessfulWeaponAbilityHit` (367-378) moves to `PairedRangedTraitsService`, which already owns what it calls.
  `TakeWeaponTraitDamageBonus` (379-390) goes to the damage input part.
- **3. Geometry.** `ResolveCombatImpactGeometry` needs `ITelegraphService` (Combat). `GetHostileTargetsInSphere` is here, so
  `MeleeTechniqueService` and the activation checks use it without going through Abilities.
- **4. Magnitude.** The six `ApplyActive...`, `GetActive...` and `ApplyCombatReadiness...` members read the store and
  `IPerkService` and `IStatService`. Both are below Combat, so the part sits in Combat and `AbilityEffectScaling` can use it.
  The damage stage part uses it for `ApplyForceAffinityMagnitude` instead of taking `IPerkService`.
- **5. Lifecycle.** Writes the store. Deps: the store, `ICombatAbilityBonusService`, `IMeleeTechniqueService`,
  `ICombatQueriesService`, `ICombatAbilityImpactService`, `ICombatRecorderService`, `ICombatHitEffectsService`,
  `ICombatResourcesService`, `IStatusEffectService` and the delivery part (for the flush at the end).
- **6 and 7. Activation checks.** `CanUseAbility` is one ordered chain of 23 denial checks. It splits into ordered check
  methods that return the denial text (or empty), and `CanUseAbility` in part 6 calls them in the same order, so the first
  failing check and its message do not change. State checks (space, prerequisites, replacement, Mimicry loadout, dead,
  commandable; then busy, weapon, equipment, Force disabled) are in part 6. Target checks (line of sight, target, location,
  range, hostility; then technique, requirements, custom validation, hostile-area line of sight, recast) are in part 7.
  Part 6 calls part 7's checks between its own, in the original order. `IsForcePower(creature, ability)` (878-881) moves to
  `ICombatQueriesService` and the bool overload to `CombatRules`, because Combat calls them. The `TwinPistolEquipmentValidation`
  call stays a seam (a content reference left for P5-12; coordinator answer 3).
- **8. Aura.** Everything that touches `_playerAuras`, with the Leadership combat point (1134-1158, handler `OnItemHit`).
  It injects `IStatusEffectService`, `IPartyRoster`, `IStatService` and `ICombatPointService`. The one content reference
  (`AuraStatusEffectBase.AnnouncesApplication`, 1186) becomes a capability interface in Character next to `IStatusEffect`;
  that is a one-line edit to P5-5's area, noted in its handoff.
- **9. `AbilityService`.** Keeps the public entry points content calls most. Its deps: the store, geometry,
  `ITelegraphService`, `ICombatResourcesService`, `ICombatAbilityImpactService`, `ICombatAbilityBonusService`,
  `INamedAnimationService`, `IStealthService`, the hostile part and the message part. The two long telegraphed methods split into
  private helpers in the same file (geometry call, activation-marker suppression and the delayed action each become a method).
  `PistolAnimationRemap` (impact animation, 2738-2773) stays a content reference until P5-12.
- **10 and 11. Per-target resolution.** The private `ApplyHostileCombatImpact` (2970-3084, 25 parameters) is one method
  that rolls the hit, then computes damage, then applies it. It splits into the hit roll (part 10: impact validity, activity
  tracking, battle theme, `TryResolveAbilityHit`, the miss branch) and the rest (part 11: base damage, the damage calls,
  then the public apply). Part 11 keeps the `BeginDamageDerivedHealing` scope (2997) around both, so its lifetime is unchanged.
- **12. Hostile impact.** The public `ApplyHostileCombatImpact` (143 lines) with its enmity (795-810), weapon damage type
  (2921-2969) and helpers. Music and capstone notices go through `IEnumerable<IAbilityImpactObserver>` (one dependency). The
  deps: the store, the observers, `ICombatPointService`, `ICombatResourcesService`, `ICombatAbilityImpactService`,
  `ICombatHitEffectsService`, `ICombatQueriesService`, `IEnmityService`, the delivery part and the status part.
- **13 and 14. Damage delivery and Dark Force.** The block of the public apply that queues or applies direct damage and tells
  the status effects (2812-2849), the end-of-impact flush of the queued damage (4501-4548), and the Dark Force conversion and
  restoration (3212-3309, 3 services of its own).
- **16 and 17. Damage formulas.** `CalculateCombatImpactDamage` (141 lines) and `CalculateNPCCombatImpactDamage` (151 lines,
  over the method limit) are linear and run the same stages: the damage inputs (weapon damage, bonuses, attack, defense,
  defense ignore) and the damage stages (critical rating and mitigation, force affinity, dealt and taken modifiers, typed
  Leadership, resistance, critical effects). Each pipeline splits at that seam, so part 16 holds the inputs of both pipelines
  and part 17 their stages. The whole range reaches 11 services plus the store, which is why it needs two parts. The pull
  request that moves them checks the real counts against these tallies; if one part lands at 11, the remaining reads move to
  the other.
- **19. Immunity** has one service call (`_statusEffect`, 4 sites) and a static table.
- **20. Activation state.** `InterruptActivationForReload` (4059) is a seam over `UsePerkFeat.InterruptAbilityActivation`
  (content reference left for P5-16).

The guide's example parts map like this: activation checks are 6 and 7; "resource costs" and "recast and cooldowns" are not in
this service (`CombatResourcesService` and `RecastService` own them in Combat; `CanUseAbility` only asks the recast service at
1060); telegraphed impacts are in part 9 and the geometry part; the definition catalog is part 1.

## The 6 upward `Lazy<>` and the edges

The six up `Lazy<>` that `DeferredResolution.txt` lists for P5-3, and what replaces each. Every replacement is a plain
constructor parameter on an interface whose implementation needs nothing above the interface's owner, so the container builds
it without a cycle (rule 1 or rule 2 of the supply rule). No `Lazy<>`, `Func<>` or late lookup is added.

| # | Service (domain) | `Lazy<>` | What it reads | Replacement | Rule |
|---|---|---|---|---|---|
| 1 | `PerkService` (Character) | `Lazy<IAbilityService>` | `GetAllAbilityDetails` | `IAbilityCatalog`, owned by Character; `AbilityCatalogService` has one dependency (`IAbilityTargetingCacheService`, none of its own) | 2 |
| 2 | `StatusEffectService` (Character) | `Lazy<IAbilityService>` | `GetAllAbilityDetails`, `GetActiveAbilityImpactSummary` | `IAbilityCatalog` and `IActiveAbilityImpact`, both owned by Character; the second is implemented by `AbilityImpactState`, which has no dependencies | 2 |
| 3 | `CombatRecorderService` (Combat) | `Lazy<IAbilityService>` | catalog (3 members), `GetActiveAbilityImpactSummary` | `IAbilityCatalog` and `IAbilityImpactState` (Combat-owned store) | 1, 2 |
| 4 | `MeleeTechniqueService` (Combat) | `Lazy<IAbilityService>` | `GetHostileTargetsInSphere` | `ICombatImpactGeometryService` (Combat, one dependency: `ITelegraphService`) | 2 |
| 5 | `PairedRangedTraitsService` (Combat) | `Lazy<IAbilityService>` | `GetActiveAbilityImpactSummary` | `IAbilityImpactState` | 1 |
| 6 | `AbilityService` (Abilities) | `Lazy<ISpaceService>` | `IsPlayerInSpaceMode` | `ISpaceModeQuery`, owned by Abilities, implemented by a new `SpaceModeQuery` in Space that needs only `IDBService` (it reads `Player.ActiveShipId`, the same three lines as `SpaceService.IsPlayerInSpaceMode`, 950-958). `AbilityActivationService` takes it | 2 |

The edges the guide's P5-3 rows name, with their members at `1f052cb3c` (`UpwardEdges.txt`):

- **Combat -> Abilities (65 members).** The tracker reads move to `IAbilityImpactState`, so Combat pushes nothing and reads the
  store the Abilities lifecycle writes (rule 1). The catalog reads use `IAbilityCatalog`. The rest:

  | Combat service | Reads today | Reads after |
  |---|---|---|
  | `CombatAbilityActivationService` | `IsAreaImpact`, `IsForcePower` | store, `ICombatQueriesService` |
  | `CombatAbilityHitService` | `GetWeaponTraitAccuracy` | store |
  | `CombatAbilityImpactService` | sequence, stat sources, `IsAreaImpact`, `RecordSuccessfulWeaponAbilityHit`, pulse | store, `IPairedRangedTraitsService` |
  | `CombatActivityService`, `CombatDamageModifiersService`, `CombatResourcesService`, `PairedRangedTraitsService` | state reads | store |
  | `CombatHitEffectsService` | `GetActiveAbilityImpactSummary`, `TryQueueTrackedDamageEffect` | store |
  | `CombatRecorderService` | catalog, summary | `IAbilityCatalog`, store |
  | `MeleeTechniqueService` | `GetHostileTargetsInSphere` | geometry |
  | `DeathService` | `ReapplyAuraEffectsForCreature` (subdual revive, 114) | `IEnumerable<ICreatureRevivedListener>`, owned by Combat, implemented by `AbilityAuraService` (rule 2: the aura part does not need `DeathService`) |
  | `TwinPistolAbilityService` | 4 impact members | moves to Abilities (coordinator answer 1) |

  The 8 `Lazy<ICombat...>` in `AbilityService` are down edges. Each part takes the Combat services it uses as plain
  parameters, and the last one disappears when `DeathService` and `TwinPistolAbilityService` stop injecting `IAbilityService`.
- **Character -> Abilities (32 members).** `PerkService` and `StatusEffectService` read only the catalog and the active summary
  (rows 1 and 2). Their `IQuickCastService` reads (`PerkService` 4) and `StatService`'s `IMimicryService` reads (4) are P5-10
  and P5-4 (the Mimicry stat bonus is pushed into the Character stat store there, rule 1), not this item.
- **Abilities -> Social, Quests, Space, World (19 members).**
  - **Party (10 members, all `IsInParty`).** `PartyService` keeps `_parties`, `_creatureToParty` and `_partyLeaders` and calls the
    aura part (`EnrollPartyMemberInAuraRanges`, `RefreshPartyAuraEligibility`), so the aura part cannot take `IPartyService`
    without a cycle. The roster state and its three reads (`GetAllPartyMembers`, `GetAllPartyMembersWithinRange`, `IsInParty`,
    lines 204-253 and the dictionaries, 26-30) move into a new `PartyRosterService : IPartyRoster` in Social with no
    dependencies. `PartyService` injects it and the aura part (both down), and its `Lazy<IAbilityService>` becomes a plain
    `IAbilityAuraService`. `IPartyService` keeps its public members, implemented over the roster. The aura part takes
    `IPartyRoster`, which Combat owns (coordinator answer 2).
  - **Capstone (3) and music (3).** One Abilities-owned interface, `IAbilityImpactObserver`, with two one-way notices
    (`OnTargetEngaged(activator, target)` for the battle theme at 2804 and 3007, `OnAbilityImpact(activator, target, ability)`
    for the capstone proof at 2879). `MusicService` and `CapstoneProofService` implement it (the other notice is empty). The
    hostile parts take `IEnumerable<IAbilityImpactObserver>` and copy it to an array once, so the per-hit call allocates
    nothing. `CapstoneProofService`'s `Lazy<IAbilityService>` becomes `IAbilityCatalog`.
  - **Space (3).** `ISpaceModeQuery` (row 6). `SpaceService` injects `IAbilityAuraService` for `ReapplyPlayerAuraAOE`.
  - **World (`TrapsService` -> `CaptureRepeatedAbilityImpact`, `GetSuccessfulImpactVisualEffect`)** is a down edge from World
    and stays as a plain call to the lifecycle part and `IAbilityService`.

If the container build test shows that an implementer above reaches an Abilities part (for example `MusicService` through
`IPerformanceAudienceService`), that notice moves to a boot-time registration (rule 3, one list per observer interface) and
P5-15's guard checks it; the interface does not change.

The baselines that shrink: `ServicesPerType.txt` (31 to 10 or less), `LargeTypes.txt` and `LongMethods.txt` (3 entries),
`DeferredResolution.txt` (the 6 up lines, the 8 down `AbilityService` lines and the down lines of `PartyService`,
`CapstoneProofService`, `MimicryService`, `QuickCastService`, `AbilityCooldownVisualService`, `AIService` and
`CompanionControlService` as each cycle is proven gone), `UpwardEdges.txt` (the three Abilities-to-Social, Quests, Space and World
rows, the Combat rows, `PerkService` and `StatusEffectService`) and `LogicToContent.txt` (`AbilityService` loses
`Feature/AbilityDefinition` and `Feature/StatusEffectDefinition`; `PistolAnimationRemap`, `TwinPistolEquipmentValidation` and
`UsePerkFeat` stay for P5-12 and P5-16, coordinator answer 3).

## Callers

Counts are call sites of `<ability service>.<member>` at `1f052cb3c` in code (comments excluded), by regex on the field and
property names `Ability`, `_ability`, `ability` and `_service` (with and without `.Value`). They are exact for the server and
the tools and a lower bound for tests: the seven `AbilityService*Tests` classes call a `TestAbilityService` through a local
property `Service` (589 calls) and are not in the "Tests" column. Columns: `Domain` is `SWLOR.Game.Server/Domain`
(including Abilities' own services), `Content` is `Feature/` plus `Native/`, `Tests` is `SWLOR.Game.Server.Tests`, `Engine` is
`SWLOR.Game.Server.EngineTests`, `Tools` is CombatSim (CLI, CraftSim, Admin and Toolset use none).

By destination:

| Destination | Domain | Content | Tests | Engine | Tools |
|---|---:|---:|---:|---:|---:|
| `IAbilityService` (stays) | 3 | 238 | 12 | 7 | 2 |
| `IAbilityCatalog` | 27 | 21 | 132 | 131 | 0 |
| `IAbilityImpactState` / `IActiveAbilityImpact` | 28 | 9 | 27 | 112 | 0 |
| `IAbilityImpactLifecycleService` | 2 | 11 | 9 | 122 | 0 |
| `IAbilityActivationService` | 2 | 5 | 4 | 81 | 0 |
| `IAbilityAuraService` | 5 | 16 | 0 | 17 | 0 |
| `IAbilityImmunityService` | 0 | 22 | 7 | 4 | 1 |
| `IAbilityActivationStateService` | 0 | 32 | 62 | 2 | 0 |
| `ICombatAbilityMagnitudeService` | 0 | 14 | 9 | 1 | 0 |
| `IAbilityHostileImpactService`, `IAbilityDarkForceService`, `IAbilityStatusApplicationService` | 0 | 11 | 8 | 16 | 0 |
| `ICombatImpactGeometryService` | 1 | 1 | 0 | 0 | 0 |
| `ICombatQueriesService` and `CombatRules` (`IsForcePower`) | 1 | 0 | 8 | 0 | 0 |
| `IPairedRangedTraitsService` | 1 | 0 | 0 | 0 | 0 |

Of the 380 server content and native sites, 238 keep their text and 142 change the property name. Tests, engine tests and
the Combat and Abilities services change with their member in the same pull request; the engine tests mostly move through
three members (`GetAbilityDetail` 124, `GetLastActivationDenialReason` 69 and `BeginAbilityImpact`/`EndAbilityImpact` 59 each).
The tests that read `AbilityService.cs` by name are 27 files (`CombatDamageTests`, `AbilityDamageQueueTests`,
`CrossSkillPerkInteractionSafetyTests` and the `PlayerMessageAudit` rows among them); a source helper that reads the part files
(as `CombatSource` did) is the first test change.

### Call text

- **Kept.** Content that calls `ApplyCombatImpact`, `ApplyTelegraphedCombatImpact`, `PlaySuccessfulImpactVisualEffect`,
  `CaptureSuccessfulImpactVisualEffect`, `ShowVisualEffectToPlayer`, `GetSuccessfulImpactVisualEffect` and
  `PlayAbilityImpactAnimation` keeps `Ability.<member>(...)`. The P5-13 design names an injected member like today's
  `GameServices` property, so these 238 sites convert in P5-14 with no text change.
- **Changed by property name only.** The other 142 sites keep the member name and arguments. The new `GameServices`
  properties are `AbilityCatalog`, `AbilityImpactState`, `AbilityImpactLifecycle`, `AbilityActivation`, `AbilityAura`,
  `AbilityImmunity`, `AbilityActivationState`, `CombatAbilityMagnitude`, `AbilityHostileImpact`, `AbilityDarkForce`,
  `AbilityStatusApplication` and `CombatImpactGeometry`. A script does the rename from the table below.
- **Master.** Master's new content lines still say `Ability.GetAbilityDetail(...)`. `tools/Refactor/MasterCopy.ps1` gets a rename
  table (member -> property) applied to `Feature/` and `Native/` copies, and `Apply-FacadeRemoval.ps1 -Reverse` (the parity
  master build) applies the table backwards, so the parity build still compiles master text. Master's edits to
  `AbilityService.cs` itself are ported by hand with the range table above (Port log rows).
- **CombatSim.** `WeaponImpactReader` needs no change for `ApplyCombatImpact` (it stays on `IAbilityService` behind
  `GameServices.Ability`). The one `HasHardCrowdControlImmunity` call in CombatSim follows to `IAbilityImmunityService`.

### Every member

| Member | Moves to | Domain | Content | Tests | Engine | Tools |
|---|---|---:|---:|---:|---:|---:|
| `CacheData` | IAbilityCatalog (handler `OnModuleCacheBefore`) | 1 | 0 | 97 | 1 | 0 |
| `GetAllAbilityDetails` | IAbilityCatalog | 8 | 2 | 13 | 3 | 0 |
| `IsFeatRegistered` | IAbilityCatalog | 9 | 9 | 6 | 3 | 0 |
| `GetAbilityDetail` | IAbilityCatalog | 9 | 10 | 16 | 124 | 0 |
| `GetAbilityImpactSequence` | IAbilityImpactState | 4 | 1 | 1 | 2 | 0 |
| `GetWeaponTraitAccuracy` | IAbilityImpactState | 2 | 0 | 1 | 0 | 0 |
| `GetRemainingDamageTargetHP` | IAbilityImpactState | 2 | 0 | 0 | 6 | 0 |
| `GetAbilityImpactStatSources` | IAbilityImpactState | 2 | 0 | 2 | 2 | 0 |
| `TryTriggerAreaAbilityPulse` | IAbilityImpactState | 1 | 0 | 1 | 0 | 0 |
| `TryQueueTrackedDamageEffect` | IAbilityImpactState | 1 | 0 | 1 | 0 | 0 |
| `GetActiveAbilityImpactSummary` | IAbilityImpactState and Character's IActiveAbilityImpact | 6 | 7 | 3 | 5 | 0 |
| `GetLastCompletedAbilityImpactSummary` | IAbilityImpactState | 0 | 0 | 2 | 61 | 0 |
| `ClearLastCompletedAbilityImpactSummary` | IAbilityImpactState | 0 | 0 | 0 | 33 | 0 |
| `IsAreaImpact` | IAbilityImpactState (both overloads) | 7 | 0 | 7 | 2 | 0 |
| `IsSingleTargetImpact` | IAbilityImpactState | 3 | 0 | 7 | 1 | 0 |
| `AddActiveAbilityDefenseIgnorePercentAdjustment` | IAbilityImpactState | 0 | 1 | 2 | 0 | 0 |
| `BeginAbilityImpact` | IAbilityImpactLifecycleService | 0 | 2 | 3 | 59 | 0 |
| `CaptureRepeatedAbilityImpact` | IAbilityImpactLifecycleService (two overloads) | 2 | 3 | 2 | 3 | 0 |
| `CaptureRepeatedAbilityImpactBatch` | IAbilityImpactLifecycleService | 0 | 1 | 1 | 0 | 0 |
| `EndAbilityImpact` | IAbilityImpactLifecycleService | 0 | 3 | 1 | 59 | 0 |
| `AbortAbilityImpact` | IAbilityImpactLifecycleService | 0 | 2 | 2 | 1 | 0 |
| `RecordSuccessfulWeaponAbilityHit` | IPairedRangedTraitsService (Combat) | 1 | 0 | 0 | 0 | 0 |
| `CanUseAbility` | IAbilityActivationService | 2 | 3 | 2 | 12 | 0 |
| `GetLastActivationDenialReason` | IAbilityActivationService | 0 | 2 | 2 | 69 | 0 |
| `IsForcePower` | ICombatQueriesService (instance) and CombatRules (the bool overload) | 1 | 0 | 8 | 0 | 0 |
| `ApplyAura` | IAbilityAuraService | 0 | 1 | 0 | 1 | 0 |
| `GetSelfAndPartyAuraRecipients` | IAbilityAuraService | 0 | 6 | 0 | 6 | 0 |
| `RemoveAura` | IAbilityAuraService | 0 | 1 | 0 | 6 | 0 |
| `ToggleAura` | IAbilityAuraService | 0 | 1 | 0 | 0 | 0 |
| `ClearAllPlayerAuras` | IAbilityAuraService | 0 | 1 | 0 | 0 | 0 |
| `RemoveCreatureFromAllAuraRanges` | IAbilityAuraService | 0 | 0 | 0 | 0 | 0 |
| `RefreshPartyAuraEligibility` | IAbilityAuraService | 2 | 0 | 0 | 0 | 0 |
| `EnrollPartyMemberInAuraRanges` | IAbilityAuraService | 1 | 0 | 0 | 0 | 0 |
| `ReapplyAuraEffectsForCreature` | IAbilityAuraService | 1 | 2 | 0 | 0 | 0 |
| `ReapplyPlayerAuraAOE` | IAbilityAuraService | 1 | 1 | 0 | 4 | 0 |
| `RecordLeadershipWeaponHit` | IAbilityAuraService | 0 | 3 | 0 | 0 | 0 |
| `AddLeadershipCombatPoint` | IAbilityAuraService (handler `OnItemHit`) | 0 | 0 | 0 | 0 | 0 |
| `ApplyAuraAOE` | IAbilityAuraService (handler) | 0 | 0 | 0 | 0 | 0 |
| `AuraEnter` | IAbilityAuraService (handler) | 0 | 0 | 0 | 0 | 0 |
| `AuraExit` | IAbilityAuraService (handler) | 0 | 0 | 0 | 0 | 0 |
| `ClearAurasOnExit` | IAbilityAuraService (handler) | 0 | 0 | 0 | 0 | 0 |
| `ClearAurasOnDeath` | IAbilityAuraService (handler) | 0 | 0 | 0 | 0 | 0 |
| `ReapplyAuraOnRespawn` | IAbilityAuraService (handler) | 0 | 0 | 0 | 0 | 0 |
| `ClearAurasOnSpaceEntry` | IAbilityAuraService (handler) | 0 | 0 | 0 | 0 | 0 |
| `ApplyCombatImpact` | IAbilityService (stays) | 1 | 72 | 6 | 7 | 2 |
| `ApplyTelegraphedCombatImpact` | IAbilityService (stays) | 0 | 63 | 4 | 0 | 0 |
| `PlaySuccessfulImpactVisualEffect` | IAbilityService (stays) | 0 | 81 | 0 | 0 | 0 |
| `CaptureSuccessfulImpactVisualEffect` | IAbilityService (stays) | 0 | 7 | 0 | 0 | 0 |
| `ShowVisualEffectToPlayer` | IAbilityService (stays) | 0 | 6 | 1 | 0 | 0 |
| `GetSuccessfulImpactVisualEffect` | IAbilityService (stays) | 1 | 3 | 1 | 0 | 0 |
| `PlayAbilityImpactAnimation` | IAbilityService (stays) | 1 | 6 | 0 | 0 | 0 |
| `GetHostileTargetsInSphere` | ICombatImpactGeometryService | 1 | 1 | 0 | 0 | 0 |
| `ApplyHostileCombatImpact` | IAbilityHostileImpactService | 0 | 1 | 1 | 16 | 0 |
| `GetWeaponAbilityDamageType` | IAbilityHostileImpactService | 0 | 4 | 1 | 0 | 0 |
| `ApplyCapturedAbilityDamageBonus` | IAbilityHostileImpactService | 0 | 1 | 1 | 0 | 0 |
| `ApplyDarkForceDamageRestoration` | IAbilityDarkForceService | 0 | 1 | 2 | 0 | 0 |
| `ApplyHostileAbilityEnmity` | IAbilityHostileImpactService | 0 | 3 | 0 | 0 | 0 |
| `CalculateHostileAbilityEnmity` | IAbilityHostileImpactService | 0 | 0 | 3 | 0 | 0 |
| `GetCombatImpactStatusCategories` | IAbilityStatusApplicationService | 0 | 1 | 0 | 0 | 0 |
| `ApplyActiveForceAffinityMagnitude` | ICombatAbilityMagnitudeService (two overloads) | 0 | 2 | 2 | 0 | 0 |
| `ApplyActiveForceAffinityRestorativeMagnitude` | ICombatAbilityMagnitudeService | 0 | 1 | 1 | 0 | 0 |
| `GetActiveForceAffinityMagnitudeMultiplier` | ICombatAbilityMagnitudeService | 0 | 0 | 1 | 0 | 0 |
| `GetActiveForceAffinityRestorativeMagnitudeMultiplier` | ICombatAbilityMagnitudeService | 0 | 1 | 2 | 0 | 0 |
| `ApplyCombatReadinessMagnitude` | ICombatAbilityMagnitudeService | 0 | 4 | 1 | 1 | 0 |
| `ApplyCombatReadinessToActivatedAbilityMagnitude` | ICombatAbilityMagnitudeService | 0 | 6 | 2 | 0 | 0 |
| `ApplyTemporaryImmunity` | IAbilityImmunityService | 0 | 8 | 1 | 0 | 0 |
| `ApplyPostControlImmunity` | IAbilityImmunityService | 0 | 7 | 2 | 0 | 0 |
| `HasTemporaryImmunity` | IAbilityImmunityService | 0 | 0 | 0 | 3 | 0 |
| `HasHardCrowdControlImmunity` | IAbilityImmunityService | 0 | 7 | 4 | 1 | 1 |
| `TryGetActiveAbilityActivation` | IAbilityActivationStateService | 0 | 9 | 14 | 0 | 0 |
| `SetActiveAbilityActivation` | IAbilityActivationStateService | 0 | 1 | 5 | 0 | 0 |
| `RemoveActiveAbilityActivation` | IAbilityActivationStateService | 0 | 2 | 2 | 0 | 0 |
| `TryGetQueuedWeaponTraits` | IAbilityActivationStateService | 0 | 2 | 5 | 2 | 0 |
| `SetQueuedWeaponTraits` | IAbilityActivationStateService | 0 | 1 | 7 | 0 | 0 |
| `RemoveQueuedWeaponTraits` | IAbilityActivationStateService | 0 | 4 | 3 | 0 | 0 |
| `TryGetQueuedWeaponAttack` | IAbilityActivationStateService | 0 | 6 | 10 | 0 | 0 |
| `TryAddQueuedWeaponAttack` | IAbilityActivationStateService | 0 | 1 | 11 | 0 | 0 |
| `RemoveQueuedWeaponAttack` | IAbilityActivationStateService | 0 | 6 | 5 | 0 | 0 |

The aura handlers (`ApplyAuraAOE`, `AuraEnter`, `AuraExit`, `ClearAurasOnExit`, `ClearAurasOnDeath`, `ReapplyAuraOnRespawn`,
`ClearAurasOnSpaceEntry`) have no callers in code; the engine reaches them by event name.

### Tests (P5-3a)

P5-3a prepared the tests so each later pull request changes only the helpers, not the test text:

- **`AbilitySource`** (`Tests/Support`) is the source of `AbilityService` wherever it lives. `PartFiles` lists the files in
  reading order and `PartInterfaces` the part interfaces; a call through a field of a part interface loses its receiver, the
  one rewrite. The 26 test files that read `AbilityService.cs` by path read it (`Read`, `Method`, `Members`, `Nodes`), and
  `PlayerMessageAuditTests` takes its ability files from `Paths()`. Each pull request adds its part files and interfaces.
- **`AbilityBench`** gives one accessor per part with public members (`Catalog`, `ImpactState`, `Geometry`, `Magnitude`,
  `Lifecycle`, `Activation`, `Aura`, `HostileImpact`, `DarkForce`, `StatusApplication`, `Immunity`, `ActivationState`, and
  `Service` for part 9), which `AbilityServiceTestBase` repeats. The tests call each member through its part's accessor
  (the "Every member" table); until a part moves, its accessor is the one instance.
- **The seam answers** (`AbilityBench.Seams.cs`) are properties of the bench, not of the double, so a seam that several
  parts call answers the same in each part's double.
- **`TestAbilityService`** is split into partial files by the part whose code calls each seam
  (`TestAbilityService.<Part>.cs`; `.Shared.cs` holds the seams several parts call). When a part moves, its file becomes
  the part's double.
- **The allocation pins** (`AbilityFacadeAllocationTests`) are grouped by the interface each member goes to and still read
  through `Ability.` (a local accessor per part would add `GameServices` lookups to `StaticLookups.txt`); the pull request
  that moves a part points its group at the new property and registers the part's bench instance in the container.

## Order

Six pull requests, serial (a part injects only parts already merged, and they all edit `AbilityService.cs`,
`ServiceRegistrations.cs`, `GameServices.Abilities.cs` and `ConvertedServices.json`). The master freeze starts before the
first and ends after the last, because the pull requests rename 142 content call sites and move the file's text.

| PR | Contents | Up `Lazy<>` removed |
|---|---|---|
| P5-3a | Tests only: a `CombatSource`-style helper for the 27 test files that read `AbilityService.cs`; split `AbilityBench` and `TestAbilityService` (933 lines, 84 overrides) so each part gets its own double; the allocation pins cover the new interfaces' reads | 0 |
| P5-3b | Parts 1, 2, 3, 4; the moves of `AbilityImpactSequence` and the shape types; `AbilityEffectScaling` and the target queries into Combat; `IsForcePower` and `RecordSuccessfulWeaponAbilityHit` to their Combat owners; all Combat and Character reads switch | 5 (rows 1 to 5) |
| P5-3c | Parts 8, 19, 20 and the leaf edges: `PartyRosterService`, `ICreatureRevivedListener` (`DeathService`), the `AuraStatusEffectBase` capability, the 8 aura handlers (`ScriptHandlerOrder.txt`, `HandlerRenames.txt`) | 0 |
| P5-3d | Parts 6 and 7; `ISpaceModeQuery` and `SpaceModeQuery`; `AIService` and `CompanionControlService` take the activation interface | 1 (row 6) |
| P5-3e | The damage side bottom-up: parts 15, 14, 13, 18, 16 and 17 | 0 |
| P5-3f | Parts 10, 11, 12, 5, the observers on `MusicService` and `CapstoneProofService`, `TwinPistolAbilityService` to Abilities, `AbilityService` trimmed to part 9, the 8 down `Lazy<ICombat...>` gone, every baseline regenerated | 0 |

P5-3b comes first because it carries 5 of the 6 `Lazy<>` and unblocks P5-4 (`StatService` reads the active impact through the
Character-owned interface) and P5-5 (`StatusEffectService`). P5-3c and P5-3d touch different ranges and could overlap with a
rebase, but the freeze makes serial cheaper than the rebases. P5-3a can run before the freeze starts. P5-4 can start after
P5-3b merges if it needs only the catalog and the active-impact interface; the owner decides, because the plan chains P5-3 to
P5-4 whole.

## Risks

- **Hot paths.** `ApplyCombatImpact` runs per target per ability and the weapon abilities ride every landed swing, so the chain
  from part 9 through parts 11, 10, 16, 17, 12, 13 and 18 adds about 8 interface calls per target. They are plain
  virtual calls, but check them with a paired `CombatLoad` run (FrameProfiler) before and after P5-3e and P5-3f. The reads
  Combat makes per swing (`IsAreaImpact`, `GetWeaponTraitAccuracy`, `GetActiveAbilityImpactSummary`) keep one hop: they go
  straight to the store.
- **Allocation pins.** `AbilityFacadeAllocationTests` pins zero allocation for the catalog reads, the impact reads and the
  last denial reason; `GetAbilityImpactStatSources` gains an argument but no closure. The observers are copied to an array
  once, never enumerated through `IEnumerable<T>` per hit. Splitting `ApplyHostileCombatImpact` must not add a closure or a
  tuple on the per-target path. The 25 optional parameters stay positional.
- **`[ReloadState]`.** Five fields change class (table above). The carried `_playerAuras` needs the reload carry test for the
  new class, and the two dictionaries in the Combat store need it in Combat. `ReloadWiring.cs:28` and
  `AbilityDetailReloadConverter` read the catalog instead of `IAbilityService`. The `PlayerSession` reasons name
  `AbilityService._playerAuras`. Run the reload checks on each pull request that moves a state field (3b, 3c, 3d).
- **Handler order.** Nine handlers move and rename in `ScriptHandlerOrder.txt` (lines 21, 214, 217, 637, 705, 734, 807, 876,
  1107). `CacheData` sits at line 21, early in `OnModuleCacheBefore`; the regenerated manifest must differ only by the class
  name. `PerkService` and others read the catalog in their own cache handlers, so the order must hold.
- **CombatSim readings.** `WeaponImpactReader` decodes the IL of a static impact (Rending Strike) for exactly one
  `GameServices.get_Ability` and one `IAbilityService.ApplyCombatImpact` call. It stays valid only while `ApplyCombatImpact` stays
  on `IAbilityService` behind the `Ability` property; renaming either breaks every weapon-ability reading silently (the
  reader returns null). The decision above keeps both. Run CombatSim's tests and one reading before P5-3b and P5-3f.
- **Message audit.** `PlayerMessageAudit.json` has 5 rows keyed by `AbilityService.cs` (the denial and impact messages);
  they re-key to the part files, and `PlayerMessageAuditTests` reads source by Roslyn.
- **Method splits.** `CanUseAbility`, the telegraphed pair and the private `ApplyHostileCombatImpact` change text, so
  each needs the characterization tests in `AbilityServiceActivationTests` (310 lines), `AbilityServiceTelegraphTests` (420) and
  `AbilityServiceCombatImpactTests` (910) green before and after, plus the engine activation tests (`PlayerGhostEngineTests`,
  `QuickCastEngineTests`, `TwinPistolEngineTests`).
- **Master porting.** `AbilityService.cs` is read by 27 test files and edited often on master (54 commits to `Service/Ability.cs` since 2026-10-10;
  the Combat file had 77 in five weeks). From P5-3b each sync ports master's edits to the part that owns the range; the range table is the map. The
  142 renamed call sites merge cleanly only if `MasterCopy.ps1` applies the rename table. `feature/space-revamp` edits
  `SpaceService`, which this item touches only by adding `SpaceModeQuery` and swapping one injected interface.
- **Part count.** 20 parts is more than the plan's example of five and more than the Combat split's 13 services. The
  alternative is fewer parts with more than 10 dependencies each, which target 2 forbids. The driver is the Combat coupling of
  the impact code (11 to 15 Combat and Character services per range), not the number of jobs.

## Questions (answered)

The questions as the map asked them; the answers are under Decisions.

1. **Move `TwinPistolAbilityService` (328 lines) from Combat to Abilities?** It applies ability impacts, so it sits above the
   service it calls. Recommended: yes, in P5-3f. Without it, Combat keeps `ApplyCombatImpact`,
   `CaptureRepeatedAbilityImpact`, `PlayAbilityImpactAnimation` and `GetActiveAbilityImpactSummary` pointing up (6 members) and
   P5-7 would have to invert them.
2. **Who owns the party roster interface, `IPartyRoster`?** Combat also reads the party through `IPartyService` (several services,
   18 members) and P5-7 plans a Combat-owned interface for it. Recommended: P5-3c declares it in Combat and `PartyRosterService`
   implements it, so Abilities and P5-7 share one interface. The domain-order table says "interfaces Abilities owns"; a second,
   Abilities-owned copy is the alternative.
3. **Leave three content references until their items?** After this split `Domain/Abilities` still names `PistolAnimationRemap`
   (impact animation), `TwinPistolEquipmentValidation` (equipment denial) and `UsePerkFeat` (reload interrupt), all statics
   with handlers that P5-12 and P5-16 convert. Removing them now means converting those statics inside P5-3. Recommended: leave
   them behind their existing seams, and remove the other two (`AbilityTargeting`, `AuraStatusEffectBase`) here.
