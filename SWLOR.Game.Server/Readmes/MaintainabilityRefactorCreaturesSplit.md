# Maintainability Refactor: Creatures Split (P5-8 part map)

The plan for splitting the three Creatures services over the Phase 5 limits (`Domain/Creatures/`):

| Service | Lines | Services used | Methods over 150 | `Lazy<>` |
|---|---:|---:|---:|---:|
| `AIService` | 1,874 | 15 | 0 | 8 (Abilities 1, Character 4, Combat 2, Creatures 1) |
| `SpawnService` | 1,208 | 3 | 0 | 0 |
| `CompanionControlService` | 1,090 | 10 | 0 | 0 |

`AIService` is the only one over the service limit; all three are over the line limit (target 2). Status lives in
[MaintainabilityRefactorStatus.md](MaintainabilityRefactorStatus.md) (row P5-8). Targets and rules:
[MaintainabilityRefactorPhase5.md](MaintainabilityRefactorPhase5.md); the order and the inversion rows:
[MaintainabilityRefactorDomainOrder.md](MaintainabilityRefactorDomainOrder.md) (Appendix C rows marked P5-8, and the notes on
who owns each interface). The model is the [Stat split](MaintainabilityRefactorStatSplit.md). This file goes away at cutover.

Line ranges and member sizes are for the three files at `81aed3ad2` (a member starts at its doc comment; blank lines and
`using` lines are not in a member's count). Projected file sizes add about one blank line per member, 22 lines of header, and
two lines per injected service plus 8 for the fields and constructor. Dependency use counts are references to each injected
field. Call counts come from a regex search of the server, the tests and the engine tests at the same commit. Cycle claims
come from a scan of the constructor graph (plain parameters only, `ServiceRegistrations` for the interface-to-class map): it
lists the plain path behind each `Lazy<>`; `LazyDependencyCycleTests` is the gate in each pull request. If master changes a
file first, map the ranges through the diff.

## Decisions

Settled by the coordinator (2026-11-17), as recommended:

1. **D1. The Enmity and leash cycle.** `EnmityService` calls `TryStartCombatLeashEvade` at six sites in four methods
   (`ModifyEnmity` 391, `TryTaunt` 449, `AttackTargetIfNeeded` 884, `IssueAttackCommand` 936, 945 and 959), and the leash code
   calls back into Enmity (`ClearEnmityTable`, `GetEnmityTable`, `RemoveEnmity`,
   `AttackHighestEnmityTarget`). An interface owned by Combat does not break that: the container still has to build Enmity
   before the leash and the leash before Enmity. It is the only plain path left behind the last `Lazy<IEnmityService>` and
   `Lazy<IEspionageInfiltrationService>` (the second one reaches `AIService` only through Enmity). **Decision:** P5-8
   leaves `AIService.Lazy<IEnmityService>` (and the Espionage one until g) in place, and records them as the last two. P5-7's
   Enmity part map then puts the table reads and writes (`GetEnmityTable`, `ClearEnmityTable`, `RemoveEnmity`) in a part that
   never calls the leash, and turns the leash's one call back up (`AttackHighestEnmityTarget` in `TryKeepFightingWithinLeash`)
   into a Combat-owned notice that Enmity subscribes to at boot (rule 3). P5-7 or P5-15 then deletes the two `Lazy<>`.
2. **D2. `UsePerkFeat` call text.** The Phase 5 order holds content types named in P5-16 untouched. `UsePerkFeat` calls three
   members that leave `ICompanionControlService` in P5-8f: `IsRegisteredCompanion` (5 sites, lines 50, 103, 282, 298, 845),
   `PeekAuthorizedTarget` (2) and `IsHostileAbilityTargetAuthorized` (2). Keeping them on `ICompanionControlService` would keep a
   forwarding layer over the authority part, which the split rules forbid. **Decision:** the 9 call sites change
   by `Apply-FacadeRemoval.ps1` (property name only), as P5-3 and P5-4 did for the same file; no logic changes.
3. **D3. The window for P5-8e.** It edits `EnmityService.cs` (P5-7), `StealthService.cs` (P5-7's file; P5-5 only changes its
   `IStatusEffectService` parameters and its `IsConcealed` body, see below) and `EspionageInfiltrationService.cs` (P5-15a, which owns that file's Combat edges). P5-8e runs
   when none of the three is running on its file: after P5-5 has merged, before P5-7 starts on Enmity, and after P5-15a's
   Espionage change. **Decision:** P5-8e, not P5-7, introduces the Combat-owned ports (the domain order names P5-7 as the
   introducer, but the code behind every port is P5-8's, and one PR that edits Enmity and Stealth once keeps P5-7's Enmity split
   on stable ports). If P5-7 gets to Enmity first, P5-7 makes the same call-site edits on the ports this guide names, and P5-8e
   shrinks to the Creatures side.
   *Amended 2026-11-17:* the Stealth-subscription clause is gone because P5-5's part map (MaintainabilityRefactorStatusEffectSplit.md) makes Stealth's two
   status effect reads pure engine reads (`ConcealmentRules`), so Stealth subscribes to nothing.

Settled here (change only on a finding in review):

- **`AIService` stays as the core plus the leash** (7 services, about 730 lines after P5-8h). The core is what everyone below
  reads: the profile tables, the per-creature state and flags, the ally cache and the leash. The events, the aggro and the
  decision engine move above it. This keeps `IAIService` for `SpawnService`, `BeastMasteryService` and `DroidService`
  (`SetAIProfile`, `SetAIFlag`), so P5-10's files are never edited.
- **`CompanionControlService` stays as the control part.** It keeps what `BeastMasteryService`, `DroidService`,
  `CharacterChatCommand` and `StablesViewModel` call, so none of them is edited (P5-10 and P5-16 files).
- **`SpawnService` stays as the runtime part;** its public members (`ScheduleResourceDespawn`, `DespawnAndQueueRespawn`, the
  three constants) keep their callers (`ScavengePoint`, `SlicingSessionService`, `SpawnTable`).
- **Pure rules move down, queries that need services use ports.** Four members of `IAIService` read only engine state:
  `IsLeashEvading`, `IsInAggroRange`, `IsCreatureAIEnabled` and (private today) `IsOutsideHomeRadius`. They become static
  members of `Domain/Foundation/CreatureAIRules` (precedent: `ResistanceRules`, `PowerGloveStatCopies.For` in P5-4b), so
  Combat, Character, Space and two Feature scripts use them without an interface.
- **Moved code stays verbatim** except for how it reaches its state and dependencies. The new members that carry state out of a
  class are listed per part (Notes on the parts).
- **No `Lazy<>`, `Func<>` or late lookup is added.** Where a part leaves a class that holds a `Lazy<>`, the parameter goes to
  the part that uses it and is plain there, unless the scan finds a plain path back (only Enmity, D1).
- **Content references are not removed here.** `UsePerkFeat`, `RestStatusEffect` and `AppliedEffects` stay behind their
  `protected virtual` seams in the part that calls them; the `LogicToContent.txt` lines move with the code. P5-2 removes them
  (it follows P5-7).
- **Accepted growth:** the coordinator accepted `AIService` 15 -> 16 services in
  P5-8f, until P5-8g removes four. No other holder grows (Holders, below).

## What the code is

### `AIService` (1,874 lines)

Two static classes of master (`AI`, 970 lines; `NPCAI`, 770 lines) became one service in P2-B6. It has five jobs.

| Job | Lines | Size |
|---|---|---:|
| Profile tables, creature state and flags, ally cache | 29-30, 58-63, 99-105, 137-149, 403-411, 537-633, 1049-1149, 1232-1253, 1756-1778, 1797-1812 | 284 |
| Creature events | 107-135, 151-313, 436-463, 866-872, 1838-1842 | 218 |
| Home, return-home and combat leash | 33-35, 37-40, 465-535, 635-922 (minus 701-705 and 909-914), 1015-1025 | 330 |
| Aggro: the aura handlers, proximity enmity, stealth-detection aggro | 36, 315-384, 413-434, 924-1013 | 177 |
| Decision engine: trigger, selection and execution | 386-401, 1038-1047, 1151-1230, 1255-1754, 1780-1795, 1850-1872 | 618 |

The 59 lines from 1814 to 1872 are 26 seams, one `protected virtual` method per outside call: Enmity (12, and
`RestoreNPCStats` passes the Enmity service as an argument), the stat service (3), the Espionage service (2), Stealth (1), the
companion service (3), Perk (1), ability activation (1), `AppliedEffects` (1), `UsePerkFeat` (1) and `AIContext` (1). Each moves
with its caller. No method is over 150 lines (the largest is `ProcessProfileTrigger`, 80).

### `SpawnService` (1,208 lines)

| Job | Lines | Size |
|---|---|---:|
| The registry: tables, stored spawns, reload carry, spawn counts | 43-171, 174-381, 383-446 | 386 |
| The runtime: queues, area spawn and despawn, respawn, testing hooks | 448-495, 527-565, 567-598, 600-860 (minus 497-525), 951-957, 1130-1206 | 456 |
| The object factory: placing a spawn, script and stat adjustment, DM spawns | 497-525, 861-949, 959-1128 | 283 |

`StoreSpawns` (130 lines) is the longest method. The registry state is seven `Adopted` fields, one `Rerolled` field and one
digest flag; the reload carry (`CarrySpawns`, `AdoptSpawns`) serialises it by property name, so the five nested state classes
become types of their own without changing the carried JSON.

### `CompanionControlService` (1,090 lines)

| Job | Lines | Size |
|---|---|---:|
| Per-companion state and the registered or controlled checks | 35-54, 91-105, 128-136, 543-555 | 54 |
| Mode position, follow action, resets, responses | 938-975, 993-1035 | 76 |
| The authorized target: choice, validation, progress, reach | 333-492, 641-907 (minus 909-936) | 412 |
| Control: lifecycle, conversation commands, rounds, owner events, handlers | 107-331, 494-639, 909-936, 977-991 | 365 |

The seams (1038-1088, 22 members) call Activity, status effects, `PlayerName`, `CombatActivity`, `CombatQueries`, `Perk`, the
catalog, ability activation and two `UsePerkFeat` statics.

### Dependencies

`AIService` (15, all constructor parameters, 8 as `Lazy<>`):

| Dependency | Domain | Uses | Used by | After the split |
|---|---|---:|---|---|
| `IEnmityService` (Lazy) | Combat | 13 | seams for events, leash, aggro, decision, action | events, aggro, decision, action plain; leash (core) `Lazy` until D1 |
| `IAbilityCatalog` | Character | 7 | validation 1, targets and abilities 6 | core 1, action 1 (the catalog is read through the same interface) |
| `ICompanionControlService` (Lazy) | Creatures | 3 | `IsControlledCompanion`, `AreAbilitiesEnabled`, `ResolveHostileAbilityTarget` | action: store and authority, plain (P5-8f) |
| `ICreatureStatService` (Lazy) | Character | 3 | events 2, leash 1 | events plain, core plain |
| `IConversationService`, `IConversationMenuService` | Foundation | 2, 1 | `CreatureConversation` | events |
| `IActivityService` | Character | 2 | `CreatureCombatRoundEnd`, `ProcessProfileTrigger` | events, decision |
| `IStatusEffectService` (Lazy) | Character | 2 | `RemoveEnemySourcedStatusEffects` | core, plain |
| `ICompanionLookup` | Character | 2 | `GetProfileType` | core |
| `IEspionageInfiltrationService` (Lazy) | Character | 2 | aggro handlers | aggro, plain (reaches AI only through Enmity, D1) |
| `IPartyService` | Social | 1 | `BuildAllies` | core, through a Creatures-owned port |
| `IDMPresenceService` | Administration | 1 | `ShouldUseCombatLeash` | core |
| `IAbilityActivationService` (Lazy) | Abilities | 1 | `CanActivateAbility` seam | action, plain |
| `IPerkService` (Lazy) | Character | 1 | `GetPerkLevel` seam | action, plain |
| `IStealthService` (Lazy) | Combat | 1 | `CanAcquireAggro` seam | aggro, plain |

`SpawnService` (3): `IAIService` (3 uses: `SetAIFlag` x2, `SetAIProfile`), `ICreatureStatService` (2: `LoadNPCStats`),
`IWalkmeshService` (3). `CompanionControlService` (10): `IAIService` (3: `ClearAIState`, `ProcessTrigger`,
`TryUseBestTargetedSupportAbility`), `ICompanionLookup` (2), `IActivityService` (3), `IStatusEffectService` (2),
`IPlayerNameService` (1), `IAbilityActivationService` (1), `IAbilityCatalog` (2), `IPerkService` (1), `ICombatActivityService`
(1), `ICombatQueriesService` (1).

### Why eight `Lazy<>` exist, and which go

The scan of plain paths from each `Lazy<>` target back to `AIService`:

- Five run through `StealthService -> IAIService`: ability activation, Perk, the stat service and the status effect service
  reach Stealth through `MessagingService` or `StatusEffectService`. The Stealth notice (P5-8e) makes all five needless.
  `LazyDependencyCycleTests` fails on a needless `Lazy<>`, so P5-8e turns them plain in the same pull request.
- `ICompanionControlService`: direct (`CompanionControlService -> IAIService`). It goes in P5-8f, when the authority part (no
  dependency on AI) takes over what `AIService` reads.
- `IEnmityService`: direct (`EnmityService -> IAIService`). It stays: D1.
- `IEspionageInfiltrationService`: `-> EnmityService -> IAIService`. It stays until D1; P5-8e cuts Espionage's own edge, and the
  parameter moves to the aggro part in P5-8g (plain there: nothing in the path reaches the aggro part).

A graph check of the final layering (the new classes below with their dependencies; Enmity on the ports; the leash on Enmity)
finds one cycle: `AIService` and `EnmityService`. Without the Enmity-to-leash edge it finds none.

## Parts

New services are `<Name>Service` behind `I<Name>Service`, registered in `ServiceRegistrations` and given a `GameServices`
property of the same name (the `ConvertedServices.json` rule for a service made of parts: each part is listed under its
service's `extras`).

| # | Part | Domain | Lines | Services | Runs / state |
|---|---|---|---:|---:|---|
| AI-1 | `AIService` (stays) | Creatures | 730 | 7 | **3 handlers**; `_creatureAllies`, `_states` (both `Runtime`) |
| AI-2 | `AICreatureEventService` | Creatures | 290 | 7 | **13 handlers**, per creature event |
| AI-3 | `AIAggroService` | Creatures | 260 | 5 | **2 handlers** and a boot handler; per aggro-aura crossing |
| AI-4 | `AIDecisionService` | Creatures | 295 | 4 | per creature trigger (`NpcAI.Decide` perf scope) |
| AI-5 | `AIActionService` | Creatures | 450 | 7 | per decision (`NpcAI.Execute` perf scope) |
| AI-6 | `CreatureAIRules` (static) and `AIActionEvaluation` (type) | Foundation, Creatures | 50, 20 | 0 | none |
| CC-1 | `CompanionControlService` (stays) | Creatures | 470 | 9 | **6 handlers** |
| CC-2 | `CompanionControlStoreService` and `CompanionControlState` | Creatures | 110, 35 | 1 | `_states` |
| CC-3 | `CompanionPositioningService` | Creatures | 125 | 2 | per mode change and follow |
| CC-4 | `CompanionTargetAuthorityService` | Creatures | 495 | 7 | per combat round and per attack command |
| SP-1 | `SpawnService` (stays) | Creatures | 515 | 2 | **5 handlers** (6 lines) |
| SP-2 | `SpawnRegistryService` and five state types | Creatures | 440, 100 | 0 | **2 handlers**; seven `Adopted` fields |
| SP-3 | `SpawnObjectService` | Creatures | 330 | 4 | **1 handler**; per spawn |

The deps:

- **AI-1:** `ICompanionLookup`, `IAbilityCatalog`, `IPartyRoster` (Combat-owned, from P5-3c, implemented by `PartyRosterService`),
  `IDMPresenceService`, `ICreatureStatService`, `IStatusEffectService`, `IEnmityService` (`Lazy<>` until D1).
- **AI-2:** `IConversationService`, `IConversationMenuService`, `IActivityService`, `ICreatureStatService`, `IEnmityService`,
  `IAIDecisionService`, `IAIService`.
- **AI-3:** `IAIDecisionService`, `IAIService`, `IEnmityService`, `IStealthService`, `IEspionageInfiltrationService`.
- **AI-4:** `IAIService`, `IActivityService`, `IEnmityService`, `IAIActionService`.
- **AI-5:** `IAIService`, `IAbilityCatalog`, `ICompanionControlStoreService`, `ICompanionTargetAuthorityService`, `IPerkService`,
  `IAbilityActivationService`, `IEnmityService`.
- **CC-1:** `ICompanionControlStoreService`, `ICompanionPositioningService`, `ICompanionTargetAuthorityService`,
  `IAIDecisionService`, `IAIActionService`, `IAIService`, `IActivityService`, `IStatusEffectService`, `IPlayerNameService`.
- **CC-2:** `ICompanionLookup`. **CC-3:** `ICompanionControlStoreService`, `IActivityService`. **CC-4:** the store, the
  positioning part, `IAbilityCatalog`, `IPerkService`, `IAbilityActivationService`, `ICombatActivityService`,
  `ICombatQueriesService`.
- **SP-1:** the registry, the object factory. **SP-3:** the registry, `IAIService`, `ICreatureStatService`,
  `IWalkmeshService`. **SP-2:** none (it uses `GameAssemblies`, `Log` and `ReloadCarry`, the guarded statics).

The layering, bottom to top: AI-1 (core and leash) < AI-5 < AI-4 < AI-2 and AI-3; CC-2 < CC-3 < CC-4 < CC-1; CC-4 < AI-5 <
CC-1 (so CC-1 calls AI and AI never calls CC-1). A scan of the final constructor graph finds the one cycle named in D1 and no
other.

### Members by part (line counts)

- **AI-1:** `ProfileLocalVariable` 1, `ProfileIdLocalVariable` 1, `_creatureAllies` 2, `_profiles` 1, `_states` 2,
  `_defaultTargetSelectors` 1, `CacheAIData` 7, `CreaturePerception` 13, `SetAIProfile` 4, `GetAIProfile` 4,
  `ProcessCreatureAllies` 32, `RemoveFromAlliesCache` 22, `SetAIFlag` 10, `GetAIFlag` 10, `BuildAllies` 19, `Profiles` 1,
  `RegisterDefaultTargetSelector` 7, `TryGetDefaultTargetSelector` 4, `CacheProfiles` 24, `ValidateProfiles` 27, `SetProfile` 5,
  `GetProfileType` 22, `ClearAIState` 4, `GetState` 22, `ValidateActions` 23, `GetProfileDefinitionTypes` 16 (284); the leash:
  three radius and rate constants 3, four variable names 4 (`LeashEvadeActiveVariable` is also read by `CreatureAIRules`),
  `ProcessFlags` 71, `ShouldStartCombatLeashEvade` 4, `ShouldLeashCombatTarget` 7, `IsTargetBeyondLeash` 22,
  `ShouldUseCombatLeash` 17, `GetCombatLeashRadius` 4, `GetHitDistance` 6, `StartLeashEvade` 23, `ContinueLeashEvadeReturn` 21,
  `TryEndLeashEvadeAtHome` 11, `CompleteLeashEvadeReturn` 19, `EndLeashEvade` 13, `TryStartCombatLeashEvade` 4,
  `TryStartLeashEvade` 17, `TryKeepFightingWithinLeash` 22, `IsEngagedWithinLeash` 7, `TryReturnHomeAfterCombat` 12,
  `ApplyLeashEvadeMovementRate` 8, `RestoreLeashEvadeMovementRate` 11, `RemoveEnemySourcedStatusEffects` 13,
  `StopCombatAfterProximityLoss` 11 (330). Seams it keeps: the leash's Enmity calls (5) and `ApplyCreatureMovementRate`.
- **AI-2:** `DMToggleAIAfter` 15, `CreatureHeartbeat` 13, `CreatureCombatRoundEnd` 22, `CreatureConversation` 17,
  `CreaturePhysicalAttacked` 19, `CreatureDamaged` 19, `CreatureDeath` 9, `CreatureDisturbed` 13, `CreatureSpawn` 16,
  `CreatureRested` 7, `CreatureSpellCastAt` 7, `CreatureUserDefined` 7, `CreatureBlocked` 17, `LoadAggroEffect` 9, `DoVFX` 18,
  `GetHighestOrEventTarget` 7, and the seams `RestoreNPCStats`, `LoadNPCStats`, `ApplyCreatureMovementRate` (218).
- **AI-3:** `CreatureAggroEnter` 44, `CreatureAggroExit` 25, `GetHasEffect` 22, `TryAcquireAggroAfterDetection` 19,
  `AcquireAggroFromNearbyEnemies` 8, `TryAcquireAggro` 18, `AddNearbyAllyProximityEnmity` 15, `TryAddProximityEnmity` 8,
  `RemoveProximityEnmity` 17, `ProximityEnmityAmount` 1 (177), plus the Espionage and Stealth seams (3) and the listener
  boot handler (about 14).
- **AI-4:** `ProcessTrigger` 16, `ProcessProfileTrigger` 80, `ShouldThrottle` 10, `UpdateActivePhase` 21, `RefreshActionCache` 21,
  `FilterKnownActions` 13, `GetKnownFeats` 18, `ScheduleBossTimer` 35, `HasEffectType` 16, and the seams `MayHaveScriptType` and
  `UseAttackableEnmityTarget` (237).
- **AI-5:** `SelectAction` 30, `EvaluateAction` 33, `ResolveTarget` 34, `CanExecuteAction` 32, `IsHostileIntent` 10, `CanUseAbility` 40,
  `ExecuteAction` 70, `ExecuteAbility` 32, `TryUseBestTargetedSupportAbility` 32, `IsOnCooldown` 8, `ApplyCooldown` 7,
  `HasCompletedOncePerPhaseAction` 7, `MarkOncePerPhaseAction` 7, `GetOncePerPhaseKey` 4, `CallAllies` 16, the nested
  `ActionEvaluation` 10 (a type of its own), and the seams for the companion service (3), `GetPerkLevel`, `CanActivateAbility`
  and `TryUseAbility` (381).
- **AI-6 (`CreatureAIRules`):** `AggroRadius`, `IsLeashEvading` 5, `IsOutsideHomeRadius` 6, `IsInAggroRange` 7,
  `IsCreatureAIEnabled` 4, `IsAIEnabled` 5 (28). It takes the radius, `ReturnHomeRadius` default and the leash-active variable name.
- **CC-1:** `Initialize` 14, `Clear` 5, `HandleConversation` 53, `RegisterDefensiveThreat` 21, `RegisterOwnerAttack` 14,
  `BeginExplicitOrder` 9, `CancelExplicitOrder` 5, `ProcessCombatRound` 35, `ProcessHeartbeat` 19,
  `ResumeFromRestIfMasterIsActive` 13, `ResumeModePosition` 5, `TryProcessPendingDefensiveReaction` 11,
  `ResetOwnerCompanionToFollow` 9, the six handlers 5-9 each (`RenameToggleAbilitiesCommand`, `OnOwnerAttacked`,
  `OnOwnerDamaged`, `OnHostileSpellCastAtOwner`, `OnOwnerCombatRound`, `OnOwnerAreaExit`), `SetMode` 13, `AttackNearest` 19,
  `HealMaster` 29, `ToggleAbilities` 13, `RegisterOwnerThreat` 5, `FindNearestVisibleEnemy` 28, `IsExplicitOrderInProgress` 15
  (365), the clock, activity, rest-effect and display-name seams, and `AssociateAbilitiesLabelStrRef`.
- **CC-2:** `CompanionControlState` 18 (a type of its own), `_states` 1, `IsRegisteredCompanion` 4, `IsControlledCompanion` 10,
  `GetMode` 4, `AreAbilitiesEnabled` 4, `GetState` 13 (54), and the `IsDroid` seam.
- **CC-3:** `ResetToFollow` 14, `ClearEngagements` 8, `ResetProgress` 7, `InterruptAndClear` 6, `MaintainModePosition` 18,
  `IssueFollowAction` 17, `SendResponse` 6 (76), the follow-distance constants, and the `IsBusy` and
  `InterruptAbilityActivation` seams.
- **CC-4:** `AdvanceAuthorizedTarget` 49, `PeekAuthorizedTarget` 27, `ResolveHostileAbilityTarget` 56,
  `IsHostileAbilityTargetAuthorized` 12, `CanIssueAttackCommand` 12, `GetDefensiveTarget` 14, `SelectDefensiveTarget` 41,
  `RemoveDefensiveThreat` 6, `ValidateAuthorizedTarget` 22, `TrackProgress` 40, `CompleteAttackNearest` 10, `StartTracking` 7,
  `CanEngageWithoutMoving` 5, `CanUseHostileAbilityWithoutMoving` 58, `GetCachedHostileAbilities` 39, `IsWithinWeaponRange` 6,
  `IsValidHostileTarget` 8 (412), the weapon-range, perk, feat, ability-detail, ability-use and last-offensive seams, and the
  tolerance and nearest-check constants.
- **SP-1:** `SpawnArea` 30, `QueueDespawnArea` 17, `ScheduleResourceDespawn` 39, `CreateQueuedSpawn` 21, `RemoveQueuedSpawn` 10,
  `QueueRespawn` 23, `DespawnAndQueueRespawn` 26, `DestroySpawnObject` 18, `GetRespawnTime` 8, `ProcessSpawnSystem` 11,
  `ProcessQueuedSpawns` 51, `ProcessDespawnAreas` 63, both `ProcessResourceDespawns` 8 and 44, `RemoveActiveSpawn` 7, the clock seam,
  the three constants and the six testing hooks 72 (456).
- **SP-2:** the five state classes 36, the eight fields 20, `_spawnTablesLoaded` 3, `SpawnCarryKey` 2, the two seams 19,
  `LoadSpawnTables` 38, `StoreSpawns` 130, `CarrySpawns` 29, `AdoptSpawns` 47, `CalculateResourceSpawnCount` 35,
  `CalculateCreatureSpawnCount` 28 (386).
- **SP-3:** `QueueResourceDespawn` 29, `AdjustScripts` 44, `AdjustStats` 44, `GetActivePlaceableSpawnPositions` 17,
  `HasActiveRareSpawn` 20, `DMSpawnCreature` 15, `PrepareDMSpawnedCreature` 10, `SpawnObject` 104 (283).

### Notes on the parts

- **AI-1 holds the leash on purpose.** The leash is the only part of the old class that Enmity calls and that calls Enmity (D1),
  and `ProcessFlags` (the heartbeat body), `StopCombatAfterProximityLoss` and `TryReturnHomeAfterCombat` use it with the state
  and the home location. Moving it out would add a class without removing a cycle or a limit. `DMPossessionPlayerLoopTests`
  borrows `ShouldUseCombatLeash` from `AIService.cs`; it stays there.
- **AI-1 keeps three handlers.** `CacheAIData` (`OnModuleCacheAfter`, `Rerun`), `CreaturePerception` (it only feeds the ally
  cache) and `RemoveFromAlliesCache` (`OnObjectDestroyed`). None is renamed.
- **New members on AI-1** (state that the other parts read; signatures new, bodies verbatim): `TryGetAllies(creature, out
  allies)` for the aggro handlers and `AddNearbyAllyProximityEnmity` (they read `_creatureAllies` today); `GetState`,
  `GetProfileType` and `TryGetProfile` become interface members (they were private); `ProcessFlags` and
  `ProcessCreatureAllies` stay private to the class that calls them (`CreatureHeartbeat` calls `ProcessFlags` through
  `IAIService`).
- **AI-2 and AI-3 are leaves.** Nothing injects them; the container resolves them as handler owners. That is why they leave first
  (P5-8g) and take plain parameters: no plain path leads back to them.
- **AI-3 and the Stealth notice.** `StealthService` calls `AcquireAggroFromNearbyEnemies` (843) and `TryAcquireAggroAfterDetection`
  (861). Neither returns a value Stealth uses, and AI-3 reaches `AIDecisionService`, `AIActionService`, ability activation and so
  Stealth again, so an injected port would recreate the cycle. Rule 3 applies: Combat owns `IStealthAggroListener`;
  `StealthService` keeps a `[ReloadState(ReloadStateKind.Runtime)]` list and `RegisterAggroListener`; the aggro part registers
  in `RegisterStealthAggroListener` (`[BootPhase(BootReload.Rerun)]` on `OnModuleCacheAfter`), which stands right after
  `CacheAIData` in the manifest (a split of master's `AI.CacheAIData`, as P5-4e did for the stat listener). A reload runs rerun
  handlers before adoption, so the list is full. `StealthService` throws `InvalidOperationException` when it notifies and the list
  is empty (a missing registration fails at once); its test bench registers a stand-in. Until P5-8g the registration is on
  `AIService`.
- **AI-4 and AI-5 split the engine** so that neither has more than 10 services and each has one reason to change: AI-4 decides
  when and about whom (throttle, phase, boss timer, action cache); AI-5 scores, resolves targets, checks and executes. AI-4 passes
  `AIActionEvaluation` (a nested private class today) to AI-5. `ProcessProfileTrigger` keeps its perf scope and calls
  `SelectAction` and `ExecuteAction` through `IAIActionService`: two interface calls per decision that were self calls. Hence the
  paired `CombatLoad` run in P5-8h.
- **`CreatureAIRules`** replaces five members: Enmity (4 sites), Espionage (3), Space (4) and `CivilianPatrol` (40) and
  `EchaniTempleMonks` (208) call it; `AIService` calls it for the same checks. Their unit tests that script `IsLeashEvading` or
  `IsInAggroRange` on an `IAIService` stand-in (`EnmityServiceTests` 130-132) set up the engine fake instead.
- **CC-2 is a store.** `CompanionControlState` becomes a public type in its own file. New members (the dictionary is private to
  the store): `Register(companion)` (the body of `Initialize`'s state line), `Remove(companion)` (`Clear`), `TryGetState`
  (`CancelExplicitOrder`, `TryProcessPendingDefensiveReaction`) and `GetState` (public). `IsRegisteredCompanion` is the store's
  check; the authority exposes the same answer for the Combat port (below) with a one-line body, because one port interface
  needs one implementer.
- **CC-4 has no path to AI.** It depends on no AI service (catalog, Perk, ability activation, the two Combat reads, the store and
  the positioning part), so `AIActionService`, `EnmityService` and `UsePerkFeat` can read it without a cycle: the graph check finds no
  plain path from `CombatActivityService`, `CombatQueriesService`, `PerkService` or `AbilityActivationService` back to Enmity.
  Its new public members (they were private to the class that calls them): `AdvanceAuthorizedTarget`, `StartTracking`,
  `IsValidHostileTarget`.
- **CC-1 keeps the six handlers** (`RenameToggleAbilitiesCommand`, `OnOwnerAttacked`, `OnOwnerDamaged`,
  `OnHostileSpellCastAtOwner`, `OnOwnerCombatRound`, `OnOwnerAreaExit`), so `HandlerRenames.txt` lines 13-18 and the manifest
  entries do not change. `RenameToggleAbilitiesCommand` keeps its `[PlayerSession(PlayerReload.LoginOnly, ...)]`.
- **SP-1 keeps the public face.** Its constants, `ScheduleResourceDespawn`, `DespawnAndQueueRespawn`, `ProcessQueuedSpawns`, the
  four area and heartbeat handlers and `QueueRespawn` (two handler lines: `OnCreatureDeathAfter` and `OnPlaceableDeath`) stay on
  `ISpawnService`, so `ScavengePoint`, `SlicingSessionService` and `SpawnTable` are not edited.
- **SP-2 and the reload.** The registry owns every `Adopted` field with its carry and adopt methods, so the reload digests and
  `ReloadAdoption.txt` re-key from `SpawnService` to `SpawnRegistryService`; the carry key (`"spawns"`) and the carried JSON
  (property names of the five state types) do not change, so a reload from a build before the split still adopts. `StoreSpawns`
  stays boot-only with `AdoptedBy` and `CarriedBy`; `LoadSpawnTables` stays `Rerun`. Both are `OnModuleCacheBefore` and keep
  their places. `SpawnRegistryService` needs no service, so it sits at the bottom.
- **SP-3 and the DM edge.** `DMSpawnCreature` (`OnDMSpawnObjectAfter`) and `PrepareDMSpawnedCreature` call `AdjustScripts` and
  `AdjustStats`; `DMBlueprintCatalogService` calls the latter through an Administration-owned port (below). `QueueResourceDespawn`
  goes with its only caller, `SpawnObject`, and appends to the registry's list.
- **No attribution scopes.** None of the three services opens a combat attribution scope (`CombatRecorder` reads none of
  them), so nothing carries an attribution through a split. The perf scopes `NpcAI.Decide` and `NpcAI.Execute` keep their names
  and methods.

## The upward references and how each goes

`UpwardEdges.txt` has seven P5-8 lines: `EnmityService.cs` Combat -> Creatures (13 members), `StealthService.cs` (4),
`EspionageInfiltrationService.cs` Character -> Creatures (4), `DMBlueprintCatalogService.cs` and `DMObjectDetailsService.cs`
Administration -> Creatures (3 each), `AIService.cs` Creatures -> Social (3) and `WalkmeshService.cs` Foundation -> Creatures
(2). `Native/OnAIActionAttackObject.cs` (Platform -> Creatures, 1) is P5-16b's and stays.

| Row | Members | Mechanism | Owner of the interface | PR |
|---|---|---|---|---|
| Combat -> Creatures: Enmity `IsLeashEvading` x3, `IsInAggroRange` x1 | 4 | static `CreatureAIRules` (rule 2: move the pure code down) | Foundation | e |
| Combat -> Creatures: Enmity `TryStartCombatLeashEvade` x6, `StopCombatAfterProximityLoss` x1 | 7 | port `ICombatLeash`; `IAIService : ICombatLeash`; registered with a forwarding lambda like `IResourceRestoredListener`. The cycle stays (D1) | Combat | e |
| Combat -> Creatures: Enmity `IsRegisteredCompanion`, `PeekAuthorizedTarget`, `CanIssueAttackCommand` | 3 (5 counted) | port `ICompanionTargetQuery`; `ICompanionControlService : ICompanionTargetQuery` in e, implemented by the authority from f (rule 2: the authority has no path back to Enmity) | Combat | e, then f moves the implementation |
| Combat -> Creatures: Stealth `AcquireAggroFromNearbyEnemies`, `TryAcquireAggroAfterDetection` | 4 | notice `IStealthAggroListener`, registered at boot (rule 3) | Combat | e, then g moves the implementation |
| Character -> Creatures: Espionage `IsInAggroRange` x2, `IsCreatureAIEnabled` | 4 | static `CreatureAIRules` | Foundation | e |
| Administration -> Creatures: `DMBlueprintCatalogService` `PrepareDMSpawnedCreature` | 3 | Administration-owned `IDMSpawnPreparation` (P5-15a), implemented directly by `SpawnObjectService`; the forwarding adapter `DMSpawnPreparation` is deleted | Administration | d |
| Administration -> Creatures: `DMObjectDetailsService` `GetAIProfile` | 3 | Administration-owned `IAIProfileDescription` (P5-15a), implemented directly by `AIService` (`IAIService : IAIProfileDescription`); the forwarding adapter `AIProfileDescription` is deleted | Administration | d |
| Creatures -> Social: `AIService` `GetAllPartyMembers` | 3 | no new port: `AIService` takes Combat's `IPartyRoster` (P5-3c; Creatures -> Combat is downward) | Creatures | d |
| Foundation -> Creatures: `WalkmeshService` -> `SpawnClearanceGrid` | 2 | the type moves to `Domain/Foundation` (`DomainOrderTypeMoves.json`; `SWLOR.TypeMoves` fixes the usings) | Foundation | c |
| Creatures -> Companions (3 + 3 + 3) | 9 | done in P5-1g (`ICompanionLookup`); P5-8 edits neither `BeastMasteryService` nor `DroidService` | Character | none |
| Creatures -> Native (`AppliedEffects`), Native -> Creatures (`OnAIActionAttackObject`) | 1 + 1 | hook logic moves in P5-16b; `AppliedEffects.MayHaveScriptType` stays behind its seam in AI-4; the hook keeps `AI.TryStartCombatLeashEvade`, which `IAIService` still declares by inheritance | - | P5-16b |

**Shared with P5-7.** Rows 1 to 4 appear in P5-7's list (Combat -> Creatures). P5-8's half is everything on the Creatures
side: the static rules, the ports' implementations, the registrations and the boot handler, plus the call-site edits in
`EnmityService.cs` and `StealthService.cs` (about 25 lines: constructor parameter types, the six seams, one listener list).
P5-7's half is nothing in P5-8e; afterwards P5-7 splits Enmity on top of the ports and, with D1, removes the last two
`Lazy<>`. The domain order's rule that the introducer adds the implementation is kept: P5-8e is the introducer and the
implementer. Combat's party port `IPartyRoster` already exists (P5-3c, implemented by `PartyRosterService`; `PartyService` only forwards to it), so `AIService` uses it and no Social port is added.

No `Lazy<>`, `Func<>` or late lookup is added.

## Holders

Services counted as the guard counts them. No holder gains a `Lazy<>`.

| Holder (services today) | Change | After |
|---|---|---|
| `AIService` (15, 8 `Lazy<>`) | e: five `Lazy<>` plain; f: `ICompanionControlService` replaced by store and authority (+1, accepted); g: loses Stealth, Espionage, both conversation services; h: loses the engine's | e 15, f 16, g 12, h 7; `Lazy<>`: e 3, f 2, g 1 |
| `CompanionControlService` (10) | f: the store, positioning and authority replace seven | 9 |
| `SpawnService` (3) | c: gains the registry (+1); d: loses AI, stat and Walkmesh, gains the object factory | c 4, d 2 |
| `EnmityService` (12) | e: `IAIService` -> `ICombatLeash`, `ICompanionControlService` -> `ICompanionTargetQuery` | 12 |
| `StealthService` | e: `IAIService` leaves (a listener list is state, not a service) | -1 |
| `EspionageInfiltrationService` | e: `IAIService` leaves | -1 |
| `SpaceService` (18) | e: `IAIService` -> `ICombatLeash`; four `IsLeashEvading` calls -> `CreatureAIRules` | 18 |
| `DMObjectDetailsService`, `DMBlueprintCatalogService` | d: `IAIService` -> `IAIProfileDescription`; `ISpawnService` -> `IDMSpawnPreparation` (both from P5-15a) | same |
| `BeastMasteryService` (14), `DroidService` (14), `SlicingSessionService` | unchanged (`IAIService`, `ICompanionControlService`, `ISpawnService` keep their members) | same |
| `CharacterChatCommand`, `StablesViewModel`, `ScavengePoint` | unchanged | same |

## Callers

Members that change home; every other member stays on its interface.

| Member | Moves to | Callers (server) |
|---|---|---|
| `IAIService.IsLeashEvading`, `IsInAggroRange`, `IsCreatureAIEnabled` | `CreatureAIRules` | Enmity 4, Espionage 3, Space 4, `CivilianPatrol`, `EchaniTempleMonks`, the AI classes |
| `IAIService.ProcessTrigger` | `IAIDecisionService` | `CompanionControlService` 1 |
| `IAIService.TryUseBestTargetedSupportAbility` | `IAIActionService` | `CompanionControlService` 1 |
| `IAIService.TryAcquireAggroAfterDetection`, `AcquireAggroFromNearbyEnemies` | `IAIAggroService` (`IStealthAggroListener`) | Stealth 2 (becomes a notice) |
| 15 event handlers | AI-2 (13), AI-3 (2) | the event system |
| `ICompanionControlService.IsRegisteredCompanion`, `PeekAuthorizedTarget`, `CanIssueAttackCommand`, `IsHostileAbilityTargetAuthorized`, `ResolveHostileAbilityTarget` | `ICompanionTargetAuthorityService` | Enmity (port), `AIContext` 4, `UsePerkFeat` 9, AI-5 |
| `ICompanionControlService.IsControlledCompanion`, `AreAbilitiesEnabled`, `GetMode` | `ICompanionControlStoreService` | AI-5, tests |
| `ISpawnService.LoadSpawnTables`, `StoreSpawns` | `ISpawnRegistryService` | handlers, tests |
| `ISpawnService.DMSpawnCreature`, `PrepareDMSpawnedCreature` | `ISpawnObjectService` | `DMBlueprintCatalogService` (port), tests |

The rest of `IAIService` (`SetAIProfile`, `GetAIProfile`, `SetAIFlag`, `GetAIFlag`, `ClearAIState`, `RemoveFromAlliesCache`,
`CacheAIData`, `TryStartCombatLeashEvade`, `StopCombatAfterProximityLoss`) and of `ICompanionControlService` (`Initialize`,
`Clear`, `HandleConversation`, `RegisterDefensiveThreat`, `RegisterOwnerAttack`, `BeginExplicitOrder`, `CancelExplicitOrder`,
`ProcessCombatRound`, `ProcessHeartbeat`, `ResumeModePosition`, `TryProcessPendingDefensiveReaction`,
`ResetOwnerCompanionToFollow`, the handlers) stays.

**Call text.** The content and native sites that change change property name only (`AI.ProcessTrigger` is not called by content;
`CompanionControl.PeekAuthorizedTarget` becomes `CompanionTargetAuthority.PeekAuthorizedTarget`). `RemovedFacades.json` lists the
members by owner; `Apply-FacadeRemoval.ps1` rewrites them and `-Reverse` maps master's text back for the parity build.
`CreatureAIRules` members are static, so those sites change to `CreatureAIRules.IsInAggroRange(...)` and the like; master's
`AI.IsInAggroRange(...)` text is ported the same way (below). New `GameServices.Creatures` properties: `AIAction`,
`AIAggro`, `AICreatureEvent`, `AIDecision`, `CompanionControlStore`, `CompanionPositioning`, `CompanionTargetAuthority`,
`SpawnObject`, `SpawnRegistry`.

## State, handlers and reload

- **Handlers.** `HandlerRenames.txt` lines 13-18 (CC), 19-36 (AI) and 38-43 (Spawn) are edited in place by each move; positions
  in `ScriptHandlerOrder.txt` do not change. Moves: AI-2 takes 13 lines (`CreatureHeartbeat` 376, `CreatureCombatRoundEnd` 393,
  `CreatureConversation` 333, `CreaturePhysicalAttacked` 321, `CreatureDamaged` 339, `CreatureDeath` 349, `CreatureDisturbed` 373,
  `CreatureSpawn` 396, `CreatureRested` 390, `CreatureSpellCastAt` 408, `CreatureUserDefined` 411, `CreatureBlocked` 330,
  `DMToggleAIAfter` 427); AI-3 takes `CreatureAggroEnter` 315 and `CreatureAggroExit` 318; SP-2 takes `LoadSpawnTables` 52 and
  `StoreSpawns` 53 (master's one `Spawn.CacheData` already splits into them, line 38); SP-3 takes `DMSpawnCreature` 470. Added
  handler: `RegisterStealthAggroListener` after `CacheAIData` (69), with `SplitBootHandlerTests`' `ServiceSplitMasterHandlers` naming
  master's `AI.CacheAIData`. `BootHandlerClassification.txt` lines 53, 54 and 70 and the reload snapshots follow.
- **`[ReloadState]`.** `_creatureAllies` and `_states` (`Runtime`) stay on `AIService`; the CC store's `_states` is unmarked
  today and stays unmarked; the seven SP-2 `Adopted` fields, the `Rerolled` `_spawnTables` and the `_spawnTablesLoaded` cache
  digest move to `SpawnRegistryService` (`ReloadAdoption.txt` 23, 60-66, 88; `ReloadDigestTests` 180-189, 240, 252;
  `ReloadBootstrapTests` 229, 237: `AdoptSpawns` and `CarrySpawns` change owner). `StealthService`'s listener list is `Runtime`.
  Nothing is `Carried`, so `ReloadCarryHistory.txt` gains no line.
- **Attribution scopes.** None (Notes on the parts).

## Tests

- **Source readers by name** (three test files, nine reads): `AIModelTests` reads `AIService.cs` once (593, `CreatureSpawn`:
  AI-2) and `CompanionControlService.cs` six times (1114, 1138, 1158, 1205, 1228, 1244: CC-1 for `RegisterDefensiveThreat`,
  `ProcessCombatRound`, `ResumeFromRestIfMasterIsActive`; CC-4 for `PeekAuthorizedTarget`, `GetDefensiveTarget`,
  `SelectDefensiveTarget`, `RemoveDefensiveThreat`, `CanUseHostileAbilityWithoutMoving`, `ResolveHostileAbilityTarget`);
  `NPCChaseTests` 23 borrows `CreatureBlocked` (AI-2); `DMPossessionPlayerLoopTests` 79 borrows `ShouldUseCombatLeash` (stays).
  P5-8b adds `CreaturesSource` (the source of a Creatures member wherever it lives, as `StatSource` does) and points the nine
  reads at it, so no later PR edits them.
- **Other files that name the files:** `tools/Refactor/ClockSeamFiles.txt` line 152 (`AIService.cs`: the clock reads at 1197,
  1210, 1263 go to AI-4; 1671, 1679 to AI-5, so h replaces the line with the two files); `PlayerMessageAudit.json` rows 948
  (`AIService.cs`, `ExecuteAction`: AI-5 in h) and 955 (`CompanionControlService.cs`, `SendResponse`: CC-3 in f), keyed by file;
  `StaticLookups.txt` lines for `AIContext.cs` (8), `AIServiceDecisionTests.cs` (3) and `CompanionControlServiceTests.cs` (11).
- **Benches and doubles.** `AIBench` (381 lines), `TestAIService` (220), `DefaultSeamsAIService` (42), `AIServiceTestBase` (79),
  `SpawnBench` (299), `TestSpawnService` (41) and the nested `TestCompanionControl` (`CompanionControlServiceTests.cs` 1261)
  each subclass one service and answer its seams. P5-8b gives each a part accessor (`Service`, `Events`, `Aggro`, `Decision`,
  `Action`; `Control`, `Store`, `Positioning`, `Authority`; `Runtime`, `Registry`, `Objects`) that is the one instance until
  a part moves, and splits each double into partial files by the part whose code calls each seam. `AIServiceSeamTests` 117
  builds `new AIService(...)` with the `Lazy<>` factories and changes with every constructor.
- **Engine tests.** `AIInternals` (`Invoke("ResolveTarget", ...)` in three tests, `Profiles` in three) and `SpawnInternals`
  (`Field("_activeSpawnsByArea")`, `"_queuedSpawnsByArea"`, `"_queuedSpawns"` in `AbandonedStationDockMasterEngineTests`,
  `Invoke("AdjustScripts")` in `CreatureHeartbeatEngineTests`) reflect into the instance class by name. P5-8b makes both pick the
  part that declares the member, so c, d and h change one lookup each.
- **Counts.** `SWLOR.Game.Server.Tests/Domain/Creatures` has 340 tests in 11 files (`AIService*` 204, `CompanionControlServiceTests`
  74, `SpawnServiceTests` 56, `NPCGroupServiceTests` 6).
- **Each new service reaches the coverage floor in its own pull request** (`Domain/Creatures` 93, `Domain/Foundation` 80,
  `CoverageFloors.json`). The new `CreatureAIRules` is covered by the tests that moved with its members.

## Order

One pull request per row, serial (they share `ServiceRegistrations`, `GameServices.Creatures`, `ConvertedServices.json` and
`AIService.cs`). Files other running items own: P5-5 `StatusEffectService.cs` (and constructor parameters in `StealthService.cs`);
P5-7 the Combat services named in the Phase 5 guide (here `EnmityService.cs`); P5-15a the services no split covers (here
`EspionageInfiltrationService.cs`). Only P5-8e touches any of them, and P5-8f touches `UsePerkFeat.cs` (D2).

| PR | Contents | Touches outside Creatures | `AIService` lines / services / `Lazy<>` after |
|---|---|---|---|
| P5-8a | This guide; the Phase 5 table links it | `Readmes` | 1,874 / 15 / 8 |
| P5-8b | Test groundwork: `CreaturesSource`, bench accessors, split doubles, part-aware `AIInternals` and `SpawnInternals`, the nine readers repointed. No production code | tests only | same |
| P5-8c | `SpawnClearanceGrid` to Foundation; `SpawnRegistryService` leaves `SpawnService` (1,208 -> about 830, `LargeTypes.txt` line gone); handler renames, reload snapshots and digests re-keyed | `WalkmeshService.cs` (usings), reload tests | same |
| P5-8d | `SpawnObjectService` leaves (`SpawnService` about 515); `AIService` implements `IAIProfileDescription` and `SpawnObjectService` implements `IDMSpawnPreparation` directly (the two P5-15a adapters are deleted); `AIService` takes Combat's `IPartyRoster` (P5-3c) instead of `IPartyService` | `DMBlueprintCatalogService.cs`, `DMObjectDetailsService.cs`, `PartyService.cs` | 1,874 / 15 / 8 |
| P5-8e | **Gated (D3).** `CreatureAIRules`; ports `ICombatLeash`, `ICompanionTargetQuery`, `IStealthAggroListener` and their registrations; Enmity, Stealth, Espionage, Space and the two Feature call sites; five `Lazy<>` plain | `EnmityService.cs`, `StealthService.cs`, `EspionageInfiltrationService.cs`, `SpaceService.cs`, `CivilianPatrol.cs`, `EchaniTempleMonks.cs` | about 1,850 / 15 / 3 |
| P5-8f | `CompanionControlStoreService`, `CompanionPositioningService`, `CompanionTargetAuthorityService` leave `CompanionControlService` (1,090 -> about 470); `AIContext` and `UsePerkFeat` call text (D2); `AIService` takes store and authority plain | `UsePerkFeat.cs` (9 sites, scripted) | about 1,850 / 16 / 2 |
| P5-8g | `AICreatureEventService` and `AIAggroService` leave (15 handler lines re-pointed; the Stealth registration handler moves); `Lazy<IEspionageInfiltrationService>` leaves with the aggro code | none | about 1,330 / 12 / 1 |
| P5-8h | `AIDecisionService` and `AIActionService` leave; `AIActionEvaluation`; `ClockSeamFiles.txt`, audit and test readers follow; paired `CombatLoad` run | none | about 730 / 7 / 1 (D1) |

P5-8c and P5-8d need nothing from P5-5 or P5-7 and can run while P5-8e waits. P5-8f, g and h run after e. Each pull request
runs the filters in the Test plan and regenerates the Phase 5 baselines.

### Baseline lines each PR changes

| File | c | d | e | f | g | h |
|---|---|---|---|---|---|---|
| `LargeTypes.txt` | `SpawnService` line gone (about 830) | - | `AIService` about 1,850 | `CompanionControlService` line gone; `AIService` about 1,850 | `AIService` about 1,330 | `AIService` line gone (about 730) |
| `ServicesPerType.txt` | - | - | - | `AIService` 15 -> 16 (accepted) | 16 -> 12 | line gone (7) |
| `DeferredResolution.txt` | - | - | `AIService.cs`: Abilities line gone, Character 4 -> 1, Combat 2 -> 1 (8 -> 3 parameters, 4 -> 3 lines) | Creatures line gone (2 parameters) | Character line gone: Espionage goes plain in `AIAggroService` (1 parameter) | - (the last one waits for D1) |
| `UpwardEdges.txt` | `WalkmeshService.cs` Foundation -> Creatures gone | `DMBlueprintCatalogService.cs`, `DMObjectDetailsService.cs`, `AIService.cs` Creatures -> Social gone | `EnmityService.cs` Combat -> Creatures, `StealthService.cs`, `EspionageInfiltrationService.cs` Character -> Creatures gone | - | - | - |
| `LogicToContent.txt` | - | - | - | `CompanionControlService.cs` keeps `UsePerkFeat` and `StatusEffectDefinition`; `CompanionPositioningService.cs` gains `UsePerkFeat` (moved, not new) | - | `AIService.cs` -> `AIActionService.cs` (`UsePerkFeat`) |
| `StaticLookups.txt` | `SpawnServiceTests` follows its bench | - | tests that stubbed `IAIService` | `CompanionControlServiceTests`, `AIContext.cs` | `AIServiceDecisionTests` | - |
| `CrossDomainEdges.txt` | Foundation -> Creatures gone | Administration -> Creatures, Creatures -> Social gone | Character -> Creatures, Combat -> Creatures, two Gameplay lines gone; Space -> Creatures shrinks | - | - | - |

`DomainCycles.txt` and `StaticState.txt` have no Creatures lines. The Enmity, Stealth and Espionage `LargeTypes.txt` and
`ServicesPerType.txt` lines belong to P5-7 and P5-15a and only change by the counts above. `DebtReport.md` is regenerated with
each. The `AIService` figures after g and h are projections from the member counts; the pull request records the measured ones.

## Master porting

Master changed the four source files 12 times since 2026-10-10: `Service/AI.cs` (970 lines) 5, `Service/AIService/NPCAI.cs`
(770) 2, `Service/Spawn.cs` (1,062) 4, `Service/CompanionControlService/CompanionControl.cs` (999) 1. From P5-8c each sync ports
master's edits by the range table (member -> part) in this guide; `AI.cs` and `NPCAI.cs` map by member name to AI-1 to AI-5,
`Spawn.cs` to SP-1 to SP-3, `CompanionControl.cs` to CC-1 to CC-4. Parts with little headroom: AI-1 (about 270 lines under 1,000),
CC-4 (about 500). No part is near the limit.

Members whose signature or call text changes:

- **`AI.IsLeashEvading(creature)`, `AI.IsInAggroRange(creature, target)`, `AI.IsCreatureAIEnabled(creature)`** become
  `CreatureAIRules.<same name>(...)` with the same arguments. Master's `Space.cs`, `Enmity.cs`, `EspionageInfiltration` and the
  two Feature scripts call them as `AI.<name>`; the sync rewrites them by hand or through `MasterCopy.ps1`'s rename table.
- **`CompanionControl.IsRegisteredCompanion`, `PeekAuthorizedTarget`, `ResolveHostileAbilityTarget`,
  `IsHostileAbilityTargetAuthorized`, `CanIssueAttackCommand`** change property name only (`CompanionTargetAuthority`);
  `IsControlledCompanion`, `AreAbilitiesEnabled`, `GetMode` change to `CompanionControlStore`. `RemovedFacades.json` has the
  owners; `-Reverse` maps master's text back.
- **`AI.ProcessTrigger`** (`AIDecision`), **`AI.TryUseBestTargetedSupportAbility`** (`AIAction`),
  **`AI.TryAcquireAggroAfterDetection`**, **`AI.AcquireAggroFromNearbyEnemies`** (`AIAggro`): property name only. Master calls the
  last two from `Stealth.cs`, which becomes a notice on the branch; a master edit to those two lines is ported by hand to the
  listener call.
- **`Spawn.LoadSpawnTables`/`StoreSpawns`** (`SpawnRegistry`) and **`Spawn.PrepareDMSpawnedCreature`**/**`DMSpawnCreature`**
  (`SpawnObject`): property name only; master's one `Spawn.CacheData` already maps to the two registry handlers.
- **Private members that become public** (names and bodies unchanged): `AI.GetState`, `GetProfileType`, `ProcessFlags`;
  `CompanionControl.AdvanceAuthorizedTarget`, `StartTracking`, `IsValidHostileTarget`, `GetState`. New members: `TryGetAllies`,
  the store's `Register`, `Remove`, `TryGetState`, `RegisterAggroListener` on Stealth. A master change inside one of these bodies
  ports by the range table.
- **Nested types** `ActionEvaluation`, `CompanionControlState`, `SpawnDetail`, `ActiveSpawn`, `SpawnResult`, `QueuedSpawn`,
  `ResourceDespawn` become types of their own; their properties keep their names (the carried JSON depends on them). A master
  change to a nested type ports to its file.
- **The `Lazy<IAbilityActivationService>` note** at `MaintainabilityRefactorStatSplit.md` line 342 (`CompanionControlService`
  keeps a `Lazy<IAbilityActivationService>`) is stale: `CompanionControlService` takes `IAbilityActivationService` plainly today
  and has no `Lazy<>`; it was the `AIService` parameter. The AI-5 plain parameter closes it.
- `feature/space-revamp`'s `SpaceService` calls `_ai.IsLeashEvading` and `_ai.TryStartCombatLeashEvade`: the first changes to the
  static, the second to the `ICombatLeash` parameter.

## Test plan

Each pull request builds the solution; runs `SWLOR.Architecture.Tests` and regenerates the Phase 5 baselines with
`SWLOR_UPDATE_SNAPSHOTS=1`, explaining every moved line; runs `--filter "FullyQualifiedName~Creatures"`,
`FullyQualifiedName~AIModel`, `~NPCChase`, `~CompanionControl`, `~Spawn`, `~Enmity` and `~Stealth` (e), `LazyDependencyCycleTests`,
the converted-service container tests, the handler snapshot tests (`ScriptHandlerOrder`, `BootHandlerClassification`,
`ReloadAdoption`, `ReloadCarryHistory`) and `PlayerMessageAuditTests`; reaches the coverage floor for each new service; and
keeps the characterization snapshots unchanged. Parity is the coordinator's gate.

- **P5-8c** runs the reload checks (`ReloadDigestTests`, `ReloadBootstrapTests`) and an engine reload with live spawns: the
  registry adopts and no area spawns twice.
- **P5-8e** runs the stealth engine tests (`StealthedPlayerAreaAbilityEngineTests`, the Espionage and leash tests) and a reload
  with a stealthed player, so the aggro listener is registered when the first detection runs.
- **P5-8h** runs a paired `CombatLoad` run (base and branch side by side, then the homes swapped): the decision path gains two
  interface calls per decision and `ExecuteAction` one per action; CombatSim reports (seed 1, 50 runs) must be byte-identical;
  and the filtered engine runs for AI profiles, companions and leash (`SpaceNPCShipLeashEngineTests`, `TankEnmityEngineTests`,
  `UnnervingRoarEngineTests`, `LoneTargetAreaAbilityEngineTests`).

## Risks

- **D1 stays open until P5-7.** `AIService` keeps one `Lazy<IEnmityService>` until the Enmity part map is written; P5-8 does not
  close the Phase 5 target 4 on its own. D1 is the least change that closes it.
- **Hot path.** `ProcessTrigger` runs on every creature heartbeat and combat round. The split adds interface calls
  (decision to action, events to decision, aggro to decision) and removes `Lazy<>.Value` reads. The measured pull request is h.
- **Stealth notice.** A missing registration would silently stop detection aggro; the empty-list `InvalidOperationException` makes
  it loud, and the engine run in e covers it.
- **Gating.** If P5-5 or P5-7 slips, P5-8e slips and with it f, g and h. c and d are independent, so the Spawn half and the
  Administration and Social ports still land. If the wait is long, f can be reordered before e only by keeping the three
  `ICompanionControlService` members on the interface (a forwarding layer); not recommended.
- **Same-file overlap.** `EspionageInfiltrationService.cs` may be rewritten by P5-15a (its Combat edges); `UsePerkFeat.cs` by P5-16
  (D2). Each is a small, scripted edit that rebases cleanly.
- **Text readers outside tests.** `tools/Refactor/ClockSeamFiles.txt` (h), `PlayerMessageAudit.json` (f, h), `HandlerRenames.txt`
  and `DomainOrderTypeMoves.json` (c).

## Progress

P5-8a: this guide (2026-11-17). P5-8e: built (2026-11-23, "P5-8e as built" below). P5-8f: built (2026-11-23, "P5-8f as built" below). Next: P5-8g.

### P5-8e as built (2026-11-23)

Branch `claude/mr-p5-8e`, base `34c3373a0` (the comment label sweep). D3's window was open: P5-5 and P5-15a are merged and P5-7 has not touched `EnmityService.cs`.

- **`CreatureAIRules`** (`Domain/Foundation`, 44 lines, static, no service): `IsLeashEvading`, `IsOutsideHomeRadius`, `IsInAggroRange`, `IsCreatureAIEnabled`, and the three constants those members read (`AggroRadius`, `ReturnHomeRadius`, `LeashEvadeActiveVariable`, public so `AIService` has one copy of each). The four bodies are the old ones; `IsCreatureAIEnabled` is the old private `IsAIEnabled` body, so `AIService` now calls it at the 14 places that called `IsAIEnabled`, and the public `IsCreatureAIEnabled` forwarder is gone. `IAIService` loses `IsLeashEvading`, `IsInAggroRange` and `IsCreatureAIEnabled`; Enmity (4 sites), Espionage (3), Space (4), `CivilianPatrol` and `EchaniTempleMonks` call the static.
- **Three Combat-owned ports** (`Domain/Combat`, one file each): `ICombatLeash` (`TryStartCombatLeashEvade`, `StopCombatAfterProximityLoss`), `ICompanionTargetQuery` (`IsRegisteredCompanion`, `PeekAuthorizedTarget`, `CanIssueAttackCommand`) and `IStealthAggroListener` (`TryAcquireAggroAfterDetection`, `AcquireAggroFromNearbyEnemies`). `IAIService : ICombatLeash` and `ICompanionControlService : ICompanionTargetQuery` (the members are declared once, on the port); both are registered with a forwarding lambda, so the container hands Enmity and Space the same instance. `AIService` implements `IStealthAggroListener` on the class, not on `IAIService`.
- **The Stealth notice.** `StealthService` drops `IAIService`. It keeps a `[ReloadState(ReloadStateKind.Runtime)]` listener list and `RegisterAggroListener` (on `IStealthService`; a second registration of the same listener keeps one entry). The two seams that called AI keep their names (`protected virtual`, so the stealth tests still override them); their bodies call every listener and throw `InvalidOperationException` when the list is empty. `AIService.RegisterStealthAggroListener` (`OnModuleCacheAfter`, `Rerun`) stands right after `CacheAIData` in `ScriptHandlerOrder.txt`; `HandlerRenames.txt` maps master's `AI.CacheAIData` to both, and `SplitBootHandlerTests` lists it as a split master handler. A reload reruns it before adoption, so the list is full at the first detection.
- **Call sites.** Enmity takes `ICombatLeash` and `ICompanionTargetQuery` (the fields are `_leash` and `_companionControl`; the seams `IsRegisteredCompanion`, `PeekAuthorizedTarget` and `CanIssueAttackCommand` keep their text); Space takes `ICombatLeash` (parameter `leash`); Espionage loses the `IAIService` parameter; the two Feature scripts gain a `Domain.Foundation` using.
- **Five `Lazy<>` plain** in `AIService`: ability activation, Perk, the stat service, the status effect service and Stealth. `LazyDependencyCycleTests` passes with the other three (companion control, Enmity, Espionage) left, as the guide says. `AIService` is 1,878 -> 1,854 lines; services stay 15; `Lazy<>` 8 -> 3. Enmity 1,244 -> 1,243, Stealth 860 -> 881, Espionage 466 -> 463.
- **Departures from the plan.**
  - `UpwardEdges.txt` and `CrossDomainEdges.txt` also lose the `Native/OnAIActionAttackObject` Platform -> Creatures line (P5-16b's): the hook calls `AI.TryStartCombatLeashEvade`, which now resolves to the member `ICombatLeash` declares, so the scan no longer sees a Creatures member. The call text is unchanged. `DomainCycles.txt` loses three lines (`Character <-> Creatures`, `Combat <-> Creatures`, `Creatures <-> Platform`), not only the two the plan expected.
  - `EnmityService`'s `using` of `Domain.Creatures` is removed (no other Creatures type is named there); Stealth and Espionage drop it too.
- **Tests.** Old test names are kept. Re-pointed: the AI benches and `AIServiceSeamTests` (five parameters plain), `AIServiceAggroTests` and `AIServiceLeashTests` (the rules are static), `EspionageInfiltrationServiceTests` (the engine fake answers the aggro range through `GetDistanceBetween`, line of sight and `GetAILevel`), `EnmityServiceTests` (the leash flag is `GetLocalBool`; the aggro range comes from the distance, and a new `_beyondAggroRange` set replaces the old default of "not in range"), `EnmityTranscriptTests`, `NPCChaseTests` (the compiled harness gains a `CreatureAIRules` stand-in), `AIModelTests` and `ForceSensitiveEspionageTests` (source readers), `StealthServiceTests`, the two Space benches, `InvertedInterfaceRegistrationTests` and `ConvertedServiceContainerTests` (the two forwarded ports), the boot handler snapshot and `SplitBootHandlerTests`. New: `StealthAggroListenerTests` (4: every listener in order, the visible notice, one entry per listener, a loud failure when none is registered), `AIServiceStealthListenerTests` (1), `CreatureAIRulesTests` (3), and the engine tests `StealthAggroListenerEngineTests` (2: an enemy beside a hidden player acquires aggro when the player is revealed and not before; an enemy out of range does not). `Fixtures/CombatTranscripts/Enmity.txt` loses 40 lines (38 `AI.IsLeashEvading`, 2 `AI.IsInAggroRange`) and nothing else; the transcript test answers those two from the engine now.
- **Baselines** (`SWLOR_UPDATE_SNAPSHOTS=1`, then the member counts of the two edge files put back): `UpwardEdges.txt` loses `EnmityService.cs` (13), `StealthService.cs` (4), `EspionageInfiltrationService.cs` (4) and the Native line; `CrossDomainEdges.txt` loses Character -> Creatures, Combat -> Creatures, Platform -> Creatures and Space -> Creatures, and Gameplay -> Creatures goes from 4 to 2 edges (`CivilianPatrol` and `EchaniTempleMonks` leave); `DeferredResolution.txt`: the Abilities line goes, Character 4 -> 1, Combat 2 -> 1; `DomainCycles.txt` the three lines above; `LargeTypes.txt` `AIService` 1,878 -> 1,854 and `EnmityService` 1,244 -> 1,243; `StaticLookups.txt` a few engine and Feature counts down by one to three, and the new engine test file at 4; `DebtReport.md` regenerated. `ServicesPerType.txt` does not change.
- **Proof.**
  - *Moved and changed bodies* (Roslyn token comparison against `34c3373a0`; modifiers, `Lazy<>.Value` and the receivers `CreatureAIRules`, `_ai`, `_leash` and `_companionControl` ignored): 541 members identical (`AIService` 131, Enmity 102, Stealth 82, Espionage 49, Space 132, `CivilianPatrol` 16, `EchaniTempleMonks` 29) and 27 different, all intended: 13 `AIService` members whose `IsAIEnabled` call is `IsCreatureAIEnabled`; the five `AIService` fields and its constructor (plain parameters); the Enmity constructor and its companion field (`ICompanionTargetQuery`); the Stealth constructor and the two listener seams; the Espionage and Space constructors. The four members that moved to `CreatureAIRules` are the old bodies.
  - *Unit tests*: `SWLOR.Game.Server.Tests` with the filters `Enmity`, `Stealth`, `Espionage`, `AI`, `Spawn`, `Companion`, `LazyDependencyCycle`, `PlayerMessageAudit` and `ConvertedServiceContainer`: 2,444 tests, 2,443 pass; the one failure (`ForceSensitiveEspionageTests`, a source reader that named `_ai.IsInAggroRange`) is fixed, and the filters `Espionage`, `StealthAggroListener`, `CreatureAIRules`, `AIServiceStealthListener`, `InvertedInterface`, `SplitBootHandler`, `NPCChase` and `ScriptHandlerOrder` pass again (300 tests). The spawn walkmesh tests need hak folders the checkout lacks (`sw_2da`, `sw_t_office`, `sw_t_alienruin`, `sw_t_garage`, `sw_t_modint2`, `sw_t_scifibase`); they were copied in read-only for the run and removed. `SWLOR.Architecture.Tests`: 297 pass after the regeneration.
  - *Engine tests*, host entry, filter `Characterization|StealthRetarget|StealthAreaAbility|StealthAggro|GhostProtocolStealth|ForceEspionage|TankEnmity|DMLimbo|SpaceNPCAI|SpawnReachability|HotReloadSpawns|TwinPistolCompanionArrival`, at `34c3373a0` and at the head (the new engine test file copied into the base tree): 92 tests each, 89 pass, 2 fail and 1 is skipped at both with the same messages (a capstone spawn distance, area taunt recovery, and the hot reload test, which needs the reload run), identical per-test outcomes, no snapshot changed.
  - *Client evidence*: one NWN client, the same module, haks, character and steps at the base and the head, in `veles_sewers` against a native Outlaw (`looter_1`), driven by a temporary driver kept outside the repository. Four scenes, the driver's status lines and the screenshots compared: (A) hidden beside an enemy, then revealed: both builds show the enemy idle with no enmity while hidden (`hiddenFromNpc=True`) and attacking the player 4 s after the reveal, so `AcquireAggroFromNearbyEnemies` reached the AI through the notice; (B) Ghost Protocol in combat: the enemy drops the player (`attackTarget=none`, `hiddenFromNpc=True`), the enmity stays at 1, and after the reveal it attacks again (chat "Outlaw attacks Agan Tarn"); at the head the status read `attackTarget=none` for 20 s after the reveal while the chat log showed the Outlaw attacking and `action=AttackObject` in both, an engine read quirk; (C) stealth stat 0: the enemy detects the hidden player (about 50 s at the base, 65 s at the head, a 30 s check cadence), stealth breaks and the enemy attacks, so `TryAcquireAggroAfterDetection` reached the AI; (D) Espionage: a stealthed player inside the aura at 4 m for 20 s, then out at 14 m: Espionage XP 0 -> 845 at both (the base run of this scene came from a second base stack). The first Espionage try (6 m for 8 s) paid at the base and not at the head, because a Spot roll is needed inside the aura and the roll interval is 30 s beyond 5 m.

### P5-8f as built (2026-11-23)

Branch `claude/mr-p5-8f`, base `3d05db052` (the P5-8e record). `CompanionControlService` 1,090 -> 477 lines (7 services: AI, activity, player name, status effect, and the three parts).

- **Parts.**
  - `CompanionControlStoreService` (`Domain/Creatures`, 85 lines, 1 service: `ICompanionLookup`): `_states`, `IsRegisteredCompanion`, `IsControlledCompanion`, `GetMode`, `AreAbilitiesEnabled`, `GetState` (public), the `IsDroid` seam and the new `Register`, `Remove`, `TryGetState`. `CompanionControlState` is a public type of its own (29 lines, property names unchanged).
  - `CompanionPositioningService` (111 lines, 2 services: the store and activity): `ClearEngagements`, `ResetProgress`, `IsExplicitOrderInProgress`, `MaintainModePosition`, `IssueFollowAction`, `SendResponse`, the follow-distance constants and the clock and busy seams.
  - `CompanionTargetAuthorityService : ICompanionTargetAuthorityService : ICompanionTargetQuery` (514 lines, 7 services: store, positioning, catalog, Perk, ability activation, combat activity, combat queries): the authorized target (`AdvanceAuthorizedTarget`, `StartTracking` and `IsValidHostileTarget` are public now), the defensive threats, tracking, the hostile ability checks, and a one-line `IsRegisteredCompanion` that asks the store. It depends on no AI service.
  - `ICompanionControlService` no longer extends `ICompanionTargetQuery` and lost `IsControlledCompanion`, `GetMode`, `AreAbilitiesEnabled`, `ResolveHostileAbilityTarget` and `IsHostileAbilityTargetAuthorized`. The Combat port is registered with a forwarding lambda from `ICompanionTargetAuthorityService`, so Enmity gets the authority instance. New `GameServices` properties: `CompanionControlStore`, `CompanionPositioning`, `CompanionTargetAuthority`.
- **Call sites.** `AIService` takes the store and the authority plain (`Lazy<ICompanionControlService>` gone: 16 services, `Lazy<>` 3 -> 2; its three seams call the store and the authority). `UsePerkFeat` (9 sites) and `AIContext` (4) call `CompanionTargetAuthority` by property name only. `RemovedFacades.json` lists the owners (`CompanionControlStore`: `AreAbilitiesEnabled`, `GetMode`, `IsControlledCompanion`; `CompanionTargetAuthority`: `CanIssueAttackCommand`, `IsHostileAbilityTargetAuthorized`, `IsRegisteredCompanion`, `PeekAuthorizedTarget`, `ResolveHostileAbilityTarget`). Two engine tests (`DroidCombatWeaponEngineTests`, `DroidInventoryEngineTests`) change property name the same way. The call edits were made with `sed` on the exact call text because `Apply-FacadeRemoval.ps1` is not run by agents; the owner runs its `-Check`.
- **Departures from the plan.**
  - `ResetToFollow` and `InterruptAndClear` stay in `CompanionControlService`, not the positioning part. Both call `CancelExplicitOrder` and `InterruptAbilityActivation`, which stay on the control interface, and only control calls them; moving them would make positioning (below control) call up. Positioning therefore has no `UsePerkFeat` reference and `LogicToContent.txt` does not change.
  - `IsExplicitOrderInProgress` moves to positioning (the plan had it in control): `MaintainModePosition` calls it. Positioning gets its own clock seam; the authority has one too (3 `UtcNow` seams, one per part that reads the clock).
  - `CompanionPositioningService` and `CompanionTargetAuthorityService` are their own `ConvertedServices.json` entries (and `DomainMap.json` services), not `extras` of `CompanionControl`: `ConvertedServicesTests` requires a part's class name to start with the service name. `CompanionControlStore` is an extra. `tools/Refactor/DomainOrder/py/upward.py` matches the two new names.
  - `UsePerkFeat` services-per-type 31 -> 32 (it now looks up the authority as well as control, because `TryProcessPendingDefensiveReaction` and `ResumeModePosition` stay on control). The plan did not list it.
  - `Initialize`, `Clear`, `CancelExplicitOrder` and `TryProcessPendingDefensiveReaction` call the store's `Register`, `Remove` and `TryGetState` where they touched `_states`.
  - The Combat golden transcript `Fixtures/CombatTranscripts/Enmity.txt` changes the call label of the scripted leaf from `CompanionControl.` to `CompanionTargetAuthority.` on 87 lines and nothing else; the leaf is now the authority interface.
- **Tests.** Old test names are kept. `TestCompanionControl` builds the three part doubles (`TestCompanionControlStoreService`, `TestCompanionPositioningService`, `TestCompanionTargetAuthorityService`) on one `CompanionTestState`; its accessors `Store`, `Positioning` and `Authority` return the real part. Re-pointed: the AI benches and `AIServiceSeamTests` (two plain parameters), `CreaturesSource` (the four files and four interfaces), `AbilityActivationPathTests`, `AbilityAttackResumeTests` (the compiled harness names `CompanionTargetAuthority`), `EnmityTranscriptTests`, `InvertedInterfaceRegistrationTests` and the `PlayerMessageAudit.json` row (`SendResponse`, now `CompanionPositioningService.cs` line 115). New: `CompanionControlPartsTests` (6: register, remove, try-get, the authority answering the Combat port, the two positioning resets).
- **Baselines** (`SWLOR_UPDATE_SNAPSHOTS=1`; the two edge files put back because only member counts moved): `LargeTypes.txt` loses `CompanionControlService` and `AIService` goes 1,854 -> 1,857; `ServicesPerType.txt` `AIService` 15 -> 16 (accepted) and `UsePerkFeat` 31 -> 32; `DeferredResolution.txt` loses the `AIService` Creatures line (`Lazy` 3 -> 2); `DebtReport.md` regenerated. `CrossDomainEdges.txt`, `EdgesIntoCoreAndPlatform.txt`, `UpwardEdges.txt`, `LogicToContent.txt` and `StaticLookups.txt` do not change.
- **Proof.**
  - *Moved and changed bodies* (Roslyn token comparison against `3d05db052`; modifiers and the receivers `_store`, `_positioning`, `_authority` ignored): `CompanionControlService.cs` 90 members and the nested state class: 85 identical (every moved member of the three parts and the six handlers), 5 different, all intended: the constructor, `Initialize`, `Clear`, `CancelExplicitOrder` and `TryProcessPendingDefensiveReaction` (the `_states` calls). `AIService` 142 identical, 5 different (the field, the constructor and the three seams, whose only change is the receiver); `AIContext` 30 and `UsePerkFeat` 62 identical with the `CompanionControl`/`CompanionTargetAuthority` receiver ignored.
  - *Unit tests*: `SWLOR.Game.Server.Tests`, filters `Companion`, `AI`, `Beast`, `Droid`, `Pet`, `Handler`, `Container`, `PlayerMessageAudit`, `ConvertedServiceContainer`, `LazyDependencyCycle`, `CreaturesSource`, `Enmity`, `InvertedInterface`, `AbilityActivationPath`, `AbilityAttackResume`, `Lockboxes`, `Reload`: 2,559 tests, all pass (the first run, 2,427 tests, had 21 failures: the golden Enmity call label, the PlayerMessageAudit row and 19 tests that need hak folders, all fixed or supplied and rerun); `CompanionControl`, `LazyDependencyCycle`, `PlayerMessageAudit` and `ConvertedServiceContainer` again after the new tests: 873 pass. Hak-reading tests needed `sw_2da`, `sw_tlk`, `sw_ability`, `sw_cr_creature`, `sw_item` and the four `sw_plc*` folders, copied in read-only and removed afterwards. `SWLOR.Architecture.Tests`: 297 pass after the regeneration.
  - *Engine tests*, host entry, filter `Characterization|TankEnmity|DroidInventory|DroidCombat|Droid|TwinPistolCompanionArrival|HotReloadSpawns|AbilityAttackResume|HostileAbilityDelay|BeastShield|LoneTargetAreaAbility|StealthRetarget`, at `3d05db052` and at the head (138 tests each): base 135 pass, 1 fails (`Migration expands blueprint alternatives inside serialized droid inventories`, a missing `DroidLightsaberBoost1` key) and 2 skip (`HotReloadSpawns` needs the reload run, `ViewModelTranscript` needs its own filter); head 134 pass with the same three outcomes plus `HostileAbilityDelay` Rib Breaker and Slam, a half-second timing check that failed under load at the head (the live stack ran beside it) and, rerun alone, failed again at the head and at the base (a busy machine), so the per-test outcomes are identical apart from that timing-only test; no snapshot changed
  - *Hot reload*: no handler and no `[ReloadState]` field moves (the six handlers stay on `CompanionControlService`; the store's dictionary was and is unmarked), so no reload snapshot changes (`Reload` unit filter above) and no reload run was made.
  - *Client evidence*: one NWN client per build, same module, haks, character and steps, driven by a temporary driver kept outside the repository, in `veles_sewers` at `SEWERS_GATE`, player set plot, native `looter_1` Outlaws spawned (immortal, paralysis applied). The driver calls the same code the radial commands reach (`SetMode`, `AttackNearest` by reflection, since the radial command needs a conversation event) and the `/ordercompanion` logic. A beast (Klorslug, level 20) and a droid (controller with 20s, 200 HP). Scenes, base and head with the same status lines: follow (2.0 m behind a walking player), guard (3.0 m), stand ground (holds its spot, 11 m from the player), told to follow (back to 2.0 m), attack nearest (attacks the Outlaw, `authorized` and `attackTarget` name it, then Follow again after it dies), an attack order on the farther of two Outlaws (the beast engages it), and the owner attacked (the beast defends and returns). Droid: follow, guard, stand, return and attack nearest match. The droid's order and owner-attacked scenes were not captured: the Outlaws kill the 200 HP droid within 30 s on both builds. Chat logs show the same "Probe damages Outlaw" lines and the mode replies. Screenshots and status lines: `C:/Temp/mr-p5-8f/evidence/{base,head}/`.
