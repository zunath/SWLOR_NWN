# Maintainability Refactor: StatusEffect Split (P5-5 part map)

The plan for splitting `Domain/Character/StatusEffectService.cs` (2,833 lines, 16 services, 7 script handlers, 2 carried
fields; `IStatusEffectService` has about 100 members). Status lives in
[MaintainabilityRefactorStatus.md](MaintainabilityRefactorStatus.md) (row P5-5). Targets and rules:
[MaintainabilityRefactorPhase5.md](MaintainabilityRefactorPhase5.md) (target 2: at most 10 services per type and under 1,000
lines; target 3: no method over 150 lines; target 4: no deferred resolution) and
[MaintainabilityRefactorDomainOrder.md](MaintainabilityRefactorDomainOrder.md) (the supply rule; the P5-5 rows). The models are
the [Stat split](MaintainabilityRefactorStatSplit.md) (closest), the [Ability split](MaintainabilityRefactorAbilitySplit.md) and
the [Creatures split](MaintainabilityRefactorCreaturesSplit.md) (its P5-8e section: P5-8e introduces the Combat ports after P5-5
and P5-15a, before P5-7). This file goes away at cutover.

Measured at `c153dd169` (`feature/maintainability-refactor`). Line ranges are for `StatusEffectService.cs` at that commit; member
line counts come from a scan of member declarations (a member starts at its doc comment and attributes). Part sizes add the
usings, class header, constructor and fields, so they are estimates (about 10%). Call counts are regex counts of
`StatusEffect.`, `_statusEffect.`, `statusEffect.` receivers over server, content, tests and engine tests. Master facts come from
the local `origin/master` ref (`88f02ed6a`, 2026-11-17; a `git fetch` failed on the TLS certificate in this session, so master
may have moved). If master or the branch changes the file first, map the ranges through the diff.

## Decisions

Settled by the coordinator (2026-11-17) after the review of the first draft; change only on a finding in review. The nine decisions the draft asked
for are recorded under Settled decisions (end of this guide). The owner confirmed the master freeze on 2026-11-17.

- **The split is layered by who calls what, with the stay-part at the bottom.** `StatusEffectService` stays as the trackers, the
  reads and the removal (about 800 lines, 4 services). Everything that applies, refreshes, announces, reapplies or reacts to
  combat leaves it, top-down (Order, below). The two most-called families stay together on `IStatusEffectService`: `Has`, `Get`
  and `Remove` (about 610 call sites) keep their receiver; only `Apply` (about 460) and the small families change theirs.
- **Moved code stays verbatim** except for how it reaches its state and dependencies. The one method that changes text is
  `ApplyStatusEffectInternal` (below).
- **Pure code moves down; queries that need a service use a port; notices use a listener.** The supply rule's order of preference:
  - rule 2, pure code down: concealment, the player-against-player check, the quick-cast feat read, the attack-delay suppression
    read and the icon tables (precedent: `CreatureAIRules`, `ResistanceRules`, `PowerGloveStatCopies.For`);
  - rule 2, a port: `IStatusResistance`, the one query the apply path needs that is not pure (its provider needs only the
    trackers, so plain injection builds without a cycle);
  - rule 3, notices: small Character-owned listener interfaces, one per subscriber group, each with its own list in part 1 and a boot
    registration (the P5-4e pattern of `IStatEffectListener`, which stays as it is). The `IEnumerable<...>` constructor pattern of
    `IAbilityImpactObserver` and `ICreatureRevivedListener` cannot be used here: every implementer holds part 1 or part 3 (it reads or
    applies status effects), so a part that took the implementers by constructor would sit in a cycle. Where a listener does not use a
    notice its method is empty, as `IAbilityImpactObserver` says. The server has one default interface member
    (`ICharacterMigration.MigratePlayerData`); this split adds none.
- **The carried keys stay where they can.** `_creatureEffects` stays in `StatusEffectService` (key unchanged); only
  `_loggedOutPlayerEffects` moves, with `[ReloadCarriedFrom]`.
- **No `Lazy<>`, `Func<>` or late lookup is added.** All three `Lazy<>` parameters of the service go.
- **Content references are not removed here** except the three logic-to-content or logic-to-Native seams that stay behind their
  one-line seams (State, below). The 115-member `Combat -> Feature/StatusEffectDefinition` edge is settled decision 3 (P5-7).

## What the code is

`StatusEffectService` has eight jobs. Every job reads or writes the per-creature trackers, so the trackers are the bottom.

| Job | Lines (of 2,833) | Size |
|---|---|---:|
| Trackers and reads: `_creatureEffects`, `Has...`, `Get...`, stat sources, the listener list | 527-536, 663-693, 1565-1590, 1686-1726, 1907-1918, 1969-1986, 2075-2119, 2420-2445, 2464-2489, 2753-2767 | 220 |
| Removal: the `Remove...` family, instance removal, the worn-off messages | 1341-1358, 1728-1967, 1988-2005, 2021-2065, 2135-2280, 2357-2437, 2448-2462 | 500 |
| Pure rules: Leadership reconcile, duration messages, clamps | 981-988, 1142-1201, 1228-1241, 2290-2355 | 135 |
| Definition catalog and icon tables | 47-186, 250-312, 1565-1568, 2007-2019, 2067-2073, 2121-2133, 2746-2751, 2790-2802 | 260 (157 data) |
| Application: the `ApplyStatusEffect` family, `ApplyStatusEffectInternal`, failures | 695-979, 1278-1285, 1360-1452 | 390 |
| Durations: outgoing adjustments, resistance, refresh, restart, extend | 1052-1140, 990-1050, 1203-1226, 1243-1276, 1592-1684 | 300 |
| The native effect and its scripts: build, apply, ticks, remove events | 500-661, 1287-1306, 1454-1563 | 270 |
| Session: login, logout, toggles, reload adoption, logged-out effects | 319-498, 1308-1339, 2282-2288, 2802-2832 | 235 |
| Combat events: damage and attack notices, the swing scope, limited attack effects | 2490-2743 | 245 |
| Constructor, fields, usings | 1-46, 187-248 | 150 |

The 17 seams (`protected virtual`, 2746-2787) are the test double's hooks; each moves with the code that calls it, or becomes a
listener stand-in. One method is over 150 lines: `ApplyStatusEffectInternal` (746-979, 234).

### Dependencies

| Dependency | Domain | Uses | Used by | After the split |
|---|---|---:|---|---|
| `IResistanceService` | Combat | 6 | resistance type and ticks in apply, restart, refresh | `IStatusResistance` (Character-owned port), part D |
| `IPvpCombatService` | Combat | 1 | `IsPlayerAgainstPlayer` in apply | static `PlayerControlRules`, part D |
| `IStealthService` | Combat | 2 | `IsConcealed` in the apply and worn-off announcements | static `ConcealmentRules`, parts A and core |
| `IMeleeTechniqueService` | Combat | 3 | `OnStanceChanged` x2 (removal), `PrepareDirectDamage` (damage notice) | `IStatusEffectMeleeListener` notices, parts core and CE |
| `ICharacterRecording` | Character | 6 | `RecordStatusApplied` in restart, extend, refresh | part D |
| `IMessagingService` | Foundation | 2 | application and worn-off nearby messages | parts A and core |
| `IPlayerNameService` | Character | 3 | the same two messages | parts A and core |
| `IGuiRefreshPublisher` | Foundation | 3 | received, removed, stat-adjustment refresh | parts A, D and core |
| `ICapstoneProofService` | Quests | 1 | `RecordStatusEffect` after a successful apply | `IStatusEffectAppliedListener` notice, part A |
| `IDBService` | Foundation | 4 | toggles saved and restored | part SS |
| `IQuickCastService` | Abilities | 1 | `KnowsFeat` for toggles | static `FeatKnowledgeRules`, part SS |
| `Lazy<IStatService>` | Character (inside) | 1 | `GetStatAdjustment` seam (outgoing adjustments, limited attack) | plain `IStatService` in parts D and CE |
| `IAbilityCatalog` | Character | 1 | known toggle ability search | part SS |
| `IActiveAbilityImpact` | Character | 1 | `GetActiveAbilityImpactSummary` seam | part A |
| `Lazy<ICombatAbilityImpactService>` | Combat (up) | 2 | stamina drain, hostile party buff notices | `IStatusEffectAppliedListener` notices, part A |
| `Lazy<ICombatAttackTimingService>` | Combat (up) | 1 | `IsAttackDelayReductionSuppressed` | its one-line body moves down into part CE (a stat read) |

## Parts

Services are `Domain/<domain>/<Name>` behind `I<Name>` and a `GameServices` property. All seven are Character. "Deps" counts
constructor services at the end. Sizes are the moved members plus constructor, fields and seams.

| # | Part | Lines | Deps | Interfaces | `GameServices` | Runs / state |
|---|---|---:|---:|---|---|---|
| 1 | `StatusEffectService` (stays: trackers, reads, removal) | 800 | 4 | `IStatusEffectService` | `StatusEffect` | carried `_creatureEffects`; runtime `_statEffectListeners`, `_suppressedStatusEffectRemovalIds`; reads on every stat read |
| 2 | `StatusEffectCatalogService` | 130 | 0 | `IStatusEffectCatalogService` | `StatusEffectCatalog` | **`CacheData`** (`Rerun`); cache `_statusEffects` |
| 3 | `StatusEffectApplicationService` | 410 | 8 | `IStatusEffectApplicationService` | `StatusEffectApplication` | per application |
| 4 | `StatusEffectDurationService` | 400 | 7 | `IStatusEffectDurationService` | `StatusEffectDuration` | per application, refresh, restart |
| 5 | `StatusEffectNativeService` | 310 | 1 | `IStatusEffectNativeService` | `StatusEffectNative` | **3 effect-script handlers**; ticks |
| 6 | `StatusEffectCombatEventService` | 285 | 2 | `IStatusEffectCombatEventService` | `StatusEffectCombatEvents` | **`OnDealtDamage`**; runtime swing state |
| 7 | `StatusEffectSessionService` | 270 | 6 | `IStatusEffectSessionService`, `IReloadStateAdopter` | `StatusEffectSession` | **2 `PlayerSession` handlers**; carried `_loggedOutPlayerEffects` |
| s1 | `StatusEffectRules` (static, gains members) | 120 | - | - | - | pure; CombatSim reads it |
| s2 | `StatusEffectIconMap` (static data) | 170 | - | - | - | two `FrozenDictionary` tables and two lookups |
| s3 | `LeadershipDamageReductionRules` (static) | 80 | - | - | - | pure over a `CreatureStatusEffect` |

Services per part, named:

1. `IStatusEffectCatalogService`, `IGuiRefreshPublisher`, `IMessagingService`, `IPlayerNameService`.
2. none.
3. `IStatusEffectService`, `IStatusEffectNativeService`, `IStatusEffectDurationService`, `IStatusEffectCombatEventService`,
   `IActiveAbilityImpact`, `IGuiRefreshPublisher`, `IMessagingService`, `IPlayerNameService`.
4. `IStatusEffectService`, `IStatusEffectNativeService` (restart and refresh replace the native effect), `IStatService`,
   `IStatusResistance`, `ICharacterRecording`, `IGuiRefreshPublisher`, `IActiveAbilityImpact` (the hostile party buff after a restart).
5. `IStatusEffectService` only (verify when written: the handlers, ticks and `BuildAbilityStatEffects` read no catalog, gui or stat service).
6. `IStatusEffectService`, `IStatService` (the limited-attack suppression read).
7. `IStatusEffectService`, `IStatusEffectApplicationService`, `IStatusEffectNativeService`, `IStatusEffectCatalogService`,
   `IDBService`, `IAbilityCatalog`.

Part 3 has two services of headroom. If master adds more, `IMessagingService` and `IPlayerNameService` leave it together as a
small announcements part (the application message is lines 937-976 today); do not do it before it is needed.

### Layers and the graph

```
        7 Session ---------------------------+
        3 Application ---+--> 4 Duration ----+--> 5 Native
              |          +--> 6 CombatEvents |      |
              |          +--> 5 Native       |      |
              v                              v      v
        1 StatusEffectService (trackers, reads, removal) --> 2 Catalog
```

An arrow is a constructor parameter. Part 1 depends on the catalog only (and Foundation and Character services); no part below
calls a part above. A graph scan of the constructor graph (`LazyDependencyCycleTests`) runs in each pull request, and the expected
result is no plain path from `StatService`, `CreatureStatService`, `PerkService` or `ResistanceService` to parts 3 to 7.

### Members by part (line counts)

- **1 (stays):**
  - tracker and listeners: constructor 35, `RegisterStatEffectListener` 10, listener notices about 25, `EnsureCreatureStatusEffectTracker` 10,
    `GetCreatureStatusEffects` 6, `RemoveCreature` 15, `RemoveCreatureIfEmpty` 10, `RemoveNativeStatusEffect` 6;
  - reads: `GetStatSources` 6, `GetStatAdjustment` 7, `HasAnyActiveEffect` x2 8, `HasStatusEffect` x4 24, `GetStatusEffect` x3 21,
    `HasStatusEffectCategory` x2 15, `HasCleanseableStatusEffect` 6, `HasCleanseType` 4, `HasEffect` x2 12,
    `GetSourceOwnedStatusEffects` 12, `GetStatusEffectSourceWithStat` 13, `GetStatusEffectsFromIcon` (creature) 12,
    `GetEffectDuration` 17, `GetNativeStatusEffectDuration` 14;
  - removal: `RemoveStatusEffect` x4 public 25 and private 33+4, `RemoveStatusEffectById` 26,
    `RemoveAndProfileStatusEffectInstance` 18, `RemoveStatusEffectInstance` 45 (the worn-off message), `IsLastInstanceOfType` 12,
    `NotifyStatusEffectRemoved` 7, `RemoveStatusEffect<T>` 6, `RemoveStatusEffect(Type, uint)` 4, `RemoveStatusEffectBySourceType` 28,
    `RemoveAllStatusEffects` 15, `RemoveFirstStatusEffect` 22, `RemoveFirstStatusEffectByCategory` 15,
    `RemoveFirstBeneficialCombatStatusEffect` 14, `RemoveFirstCleanseableStatusEffect` 15, `RemoveCleanseableStatusEffects` 14,
    `RemoveStatusEffectsWithStat` 14, `RemoveStatusEffectsWithNegativeStat` 14, `ConsumeStatusEffectStat` 4,
    `SetStatusEffectStat` 6, `RemoveStatusEffectFromAllTargetsBySource` 20, `RemoveStatusEffectsFromAllTargetsBySource` 45,
    `RemoveStatusEffectsFromAllTargetsWhenSourceExits` 33, `RemoveOtherStanceStatuses` 18, `RemoveOtherCommandStatuses` 26,
    `RemoveNonPersistentStatusEffects` 7, `RemoveTrackedNWNEffects` 10, `PublishStatusEffectRemovedRefresh` 4, `ClearStatusEffectsOnDeath` 9 (it calls `RemoveAllStatusEffects` and `RemoveCreature`, both part 1, so
    it stays there and `DeathService` needs no session part).
- **2 Catalog:** `CacheData` 9, `CacheStatusEffects` 18, `CreateStatusEffect` 10, `TryCreateStatusEffect` 12,
  `HasStatusEffectDefinition` 4, `GetStatusEffectName` 6, `GetStatusEffectSourceType` 6, `GetStatusEffectsFromIcon` (icon) 7,
  `GetStatusEffectTypes` seam 6, `StatusEffectMetadata` 22 (nested struct). `GetEffectTypeFromIcon` 6 and `GetAbilityTypeBuffed` 6
  become static members of `StatusEffectIconMap` with the two tables (145 lines of data, no logic; under the line limit, so no
  exemption is needed).
- **3 Application:** `ApplyStatusEffect` x6 88, `ApplyPermanentStatusEffect` x2 12, `ApplyAndProfileStatusEffect` 26,
  `RecordCapstoneProof` 8 (becomes the listener call), `ApplyStatusEffectInternal` as seven named steps (next section) about 205,
  `SendStatusEffectFailure` 8, `HasStatAdjustment` 4, `PublishStatusEffectReceivedRefresh` 4, seam `GetActiveAbilityImpactSummary` 3.
- **4 Duration:** `ResolveApplicationTiming` (new, the extracted step, about 60), `ApplyOutgoingStatusDurationAdjustments` 24,
  `ApplyOutgoingStatusStatAdjustments` 14, `ResolveResistanceType` 19, `TryRefreshInPlace` 61, `RestartStatusEffectDuration` 57,
  `RestartInPlace` 31, `ExtendStatusEffectDuration` 32, `RefreshStatusEffectDuration` 60, seam `GetCreatureStatAdjustment` 3.
- **5 Native:** the three handlers `OnApplyStatusEffect` 8, `OnRemoveStatusEffect` 8, `OnStatusEffectInterval` 8;
  `OnApplyNWNStatusEffect` 5, `OnRemoveNWNStatusEffect` 23, `GetLastStatusEffectTag` 8, `OnNWNStatusEffectInterval` 11,
  `TickAllStatusEffects` 29, `TickStatusEffectById` 23, `TickStatusEffect` 19; `ApplyTrackedNWNEffect` 7,
  `ReplaceNativeStatusEffect` 5, `ApplyNativeStatusEffect` 16, `ScheduleTemporaryHitPointLimitCheckForVitality` 5,
  `BuildNativeStatusEffect` 25, `GetStatusEffectDurationSeconds` 15, `LinkEffect` 6, `BuildAbilityStatEffects` 21,
  `ReapplyNWNEffects` 20, seams `ScheduleTemporaryHitPointLimitCheck` and `MayHaveEffectTag` 2.
- **6 CombatEvents:** `OnDealtDamage` 20, `NotifyDamageStatusEffects` 20, `NotifyPreDamageStatusEffects` 24,
  `NotifyAttackAttemptStatusEffects` 32, `OnGuardedHit` 13, `BeginNativeAttackSwing` 10, `ForgetDestroyedNativeAttackSwing` 9,
  `EndNativeAttackSwing` 24, `TryDeferNativeAttackStatusEffect` 30 (split in two, below), `IsDeferredNativeAttackStatusEffect` 6,
  `TryGetLimitedAttackDelayReduction` 29, `TryGetLimitedAttackNoDelay` 26, seam `IsAttackDelayReductionSuppressed` (a stat read
  after the move).
- **7 Session:** `OnPlayerEnter` 26, `OnPlayerExit` 28, `OnCarriedStateAdopted` 32, `SaveTogglesForRestart` 17, `SaveToggles` 17,
  `RestoreSavedToggles` 28, `FindKnownToggleAbility` 10, `ReassignSelfSourcedEffects` 8,
  `ReconcileLoggedOutEffects` 23, `NotifyStatusEffectsRestored` 7, nested `LoggedOutStatusEffects` 14, seam `GetAllAbilityDetails` 1.
- **s1 `StatusEffectRules`** (58 lines today) gains `ShouldAnnounceNearby` 8, `BuildDurationResistanceMessage` 28,
  `FormatDurationSeconds` 7, `ClampConvertedControlDurationTicks` 7 and `GetRefreshTickAnchor` 9, which are pure; the three
  one-line forwarders (`GetOutgoingDurationPercentAdjustment`, `ClampHardCrowdControlDurationTicks`,
  `CalculateAdjustedStatusDurationSeconds`, 4-10 lines each) are deleted and their callers use the rules. CombatSim keeps the names
  it reads (`GetOutgoingDurationPercentAdjustment`, `CalculateAdjustedStatusDurationSeconds`, `ClampHardCrowdControlDurationTicks`).
- **s3 `LeadershipDamageReductionRules`:** `ReconcileLeadershipDamageReduction` 41 and `ApplyLeadershipDamageReductionContribution` 24
  take only a `CreatureStatusEffect`; part 3 (after an add) and part 1 (after a removal) both call them.

### Cutting `ApplyStatusEffectInternal`

234 lines today, in eight steps. Part 3 keeps seven as private methods under 50 lines; the timing step goes to part 4 because it
is the part of the method that reads stats and the resistance port. Each step keeps the original's order of reads and
writes; the early `return false` exits become the step's bool result. State that the steps share is a readonly struct
`StatusApplicationTiming` (duration seconds, ticks, ticks without resistance, resistance type) passed by `ref` or `in`: no
tuple, no closure (precedent: `DeflectionReaction`).

| Step | Original lines | Where | Result |
|---|---|---|---|
| 1. Defer inside a native attack swing | 756-767 | `ApplyStatusEffectInternal` (part 3), calls part 6 | returns true |
| 2. Resolve timing: outgoing adjustments, resistance, immunity, PvP and hard control clamp, budget | 769-814 | `ResolveApplicationTiming` (part 4) | struct, or false with the "resisted" message |
| 3. Replaced effects and the conversion clamp | 816-837 | `ResolveReplacedEffects` (part 3) | array, or false |
| 4. Validate: `CanApply` without the replaced effects, more powerful effects | 839-863 | `ValidateApplication` (part 3) | false with the failure message |
| 5. Refresh in place | 865-871 | `ApplyStatusEffectInternal`, calls part 4 `TryRefreshInPlace` | returns true |
| 6. Clear conflicts: replaced, command statuses, stacking, less powerful | 873-895 | `ClearConflictingEffects` (part 3) | none |
| 7. Activate: assign resistance, apply, add to the tracker, leadership, movement rate, native effect, notices, refresh | 897-935 | `ActivateEffect` (part 3) | false when flagged for removal |
| 8. Announce: duration message, application message | 937-976 | `AnnounceApplication` (part 3) | none |

The method is then about 40 lines. `TryDeferNativeAttackStatusEffect` today builds its closure at method entry, so a normal
application already allocates it (the allocation pins read the `Get` and `Has` paths, not apply). The split makes the closure in a
private method that runs only after part 6 says the application is deferred (`IsDeferred` then `Defer(creature, Action)`), which
can only lower allocation. A characterization test (`StatusEffectServiceApplyTests`, 955 lines) pins the log order of a whole
application before and after.

### Notes on the parts

- **1 stays wide on purpose.** Trackers, reads and removal are one job: removal is the other half of every read's data, and the
  hot read families (`HasStatusEffect` 276 sites, `GetStatusEffect` 162, `GetCreatureStatusEffects` 112, `HasEffect` 48,
  `RemoveStatusEffect` 117) and the stat read (`GetStatAdjustment`, 0 allocations, pinned) keep their receiver. At about 800
  lines it has 20% headroom; each sync checks master's additions to it.
- **3 and 4 are separate** so part 3 stays at 8 services. Merging them (settled decision 2) puts the merged part at exactly 10.
- **5 is the effect's script side.** The three handlers are the scripts the native effect runs (apply, remove, interval); they
  call part 1's removal (an expired or dispelled effect), so the part sits above part 1. `_suppressedStatusEffectRemovalIds` stays
  in part 1: `RemoveNativeStatusEffect` adds and removes the id around the engine call and the remove handler (part 5) only reads it
  through a public `IsRemovalSuppressed(id)`.
- **6 has one writer of state.** The swing depth and the deferred list are runtime state that only part 6 touches; part 3 asks
  `IsDeferred` and hands over the closure, so part 6 never calls the apply path (no cycle). The closure holds the apply call, so
  it runs part 3's code when the cycle ends.
- **7 owns what survives logout, restart and reload.** `OnCarriedStateAdopted` settles a status whose native effect is gone
  (`RemoveStatusEffectById`, part 1) and resumes the rest, and `OnPlayerEnter` reapplies native effects (part 5). Its
  `RemoveStatusEffectsFromAllTargetsWhenSourceExits` call at logout is part 1's.
- **Quick cast and feats.** `QuickCastService.KnowsFeat` reads only `QuickCastFeatMap` and the engine. The map (frozen tables) moves
  to `Domain/Foundation` through a `DomainOrderTypeMoves.json` entry, with a static `KnowsFeat(creature, feat)` beside it;
  `QuickCastService.KnowsFeat` keeps its signature and calls it (as `CreatureAIRules`). Its tests change where they script it.
- **Concealment and PvP.** `StealthService.IsConcealed` and `PvpCombatService.IsPlayer`, `GetControllingPlayer` and
  `IsPlayerAgainstPlayer` read only the engine. Their bodies move into `ConcealmentRules` and `PlayerControlRules` (Character), and
  the service members stay on their interfaces with one-line bodies that call the rules, so no holder changes. `PvpCombatRules`
  (pure statics, 3 Combat users and `StatusEffectRules`, 1 member) moves to Character by type move, which also removes the
  `StatusEffectRules.cs: Character -> Combat | 1` line.
- **Attack-delay suppression.** `CombatAttackTimingService.IsAttackDelayReductionSuppressed` is
  `GetStatAdjustment(creature, StatType.AttackDelayReductionSuppressed) > 0`. Part 6 reads that through its `IStatService`; the
  Combat method stays for its own callers.

## State

Every field of `StatusEffectService`, and the new ones:

| Field | Today | After | Reload class | How a reload treats it |
|---|---|---|---|---|
| `_creatureEffects` | service | part 1 (stays) | `Carried` | carried under `...StatusEffectService._creatureEffects`; key unchanged, no `[ReloadCarriedFrom]` |
| `_loggedOutPlayerEffects` | service | part 7 | `Carried` | key becomes `...StatusEffectSessionService._loggedOutPlayerEffects`; the field takes `[ReloadCarriedFrom("SWLOR.Game.Server.Domain.Character.StatusEffectService")]`, so a reload from an older build reads it from the old class's text |
| `_statusEffects` | service | part 2 | cache (unmarked) | rebuilt by `CacheData` (`Rerun`); must hash the same after reload as after boot |
| `_statEffectListeners` | service | part 1 (renamed `_listeners`) | `Runtime` | holds service instances, rebuilt by the registration handlers (they rerun before adoption); `InvalidOperationException` when it notifies and is empty, as today |
| `_suppressedStatusEffectRemovalIds` | service | part 1 | `Runtime` | empty after a reload (held only across one engine call) |
| `_nativeAttackSwingDepth`, `_deferredNativeAttackStatusEffects` | service | part 6 | `Runtime` | emptied by a reload: a swing in flight is lost (HotReload.md, "Lost") |
| the two frozen icon tables | static readonly | `StatusEffectIconMap` | immutable data | not state |
| `StatusEffectTag`, `Interval` constants | service | part 5 and part 1 | - | - |

- **Adoption order matters and is by name.** `ReloadStateCarrier.AdoptAll` walks the carried classes in ordinal order of their full
  names and calls `OnCarriedStateAdopted` on each owner right after it reads that class's fields. Part 7's adoption reads
  `_creatureEffects`, so `StatusEffectService` must come first: `...StatusEffectService` sorts before `...StatusEffectSessionService`
  ('r' before 's'). The name is chosen for that; a unit test in P5-5e pins the order, and the reload check runs on the pull request.
- **The text of one old class feeds two new ones.** An older build wrote both fields in one JSON text under the old key; part 1
  reads its field from it (`Read` skips names it does not know) and part 7 reads its field from the same text through the
  `[ReloadCarriedFrom]` (each key is taken once). `ReloadStateCarrierTests` gets a case for it, and `ReloadCarryHistory.txt` gains
  the part 7 key (it never loses a line).
- **No reference crosses the two texts.** An effect lives in `_creatureEffects` or in `_loggedOutPlayerEffects`, never both
  (`OnPlayerExit` stores it and then calls `RemoveCreature`; `OnPlayerEnter` removes it from the held list before it adds it).
  Separate JSON texts therefore lose no shared reference. The reload check on P5-5e proves it with a player who logs out and a
  creature that holds the same effect.
- **Closures that keep a reloaded state alive** (HotReload.md rule 11) move unchanged: `DelayCommand(1f, RestoreSavedToggles)`
  (part 7), `DelayCommand(0f, ...)` in the remove handler (part 5) and `DelayCommand(0.1f, ApplyCreatureMovementRate)` in the instance
  removal (part 1). `OnCarriedStateAdopted` already covers the movement rate and the settle closure.
- **Seams that stay as they are:** `TemporaryHitPointEffects.ScheduleLimitCheck` (content, P5-12) stays behind its seam in part 5;
  `AppliedEffects.MayHaveTag` (Native, P5-16b) stays behind its seam in parts 5 and 7 (the Character -> Native edge, 1 member).

## Handlers

Seven handlers, all with a `HandlerRenames.txt` line today (320-326, edited in place by the moving PR). The manifest
(`ScriptHandlerOrder.txt`) order does not change: each handler's line is renamed where it stands. Three registration
handlers are added as splits of master's `StatusEffect.CacheData`, so they stand with it and the parity handler map accepts
them (`Read-HandlerRenames` takes several branch names for a split).

| Handler | Event | New owner | Manifest line (today) | `HandlerRenames.txt` | Class |
|---|---|---|---:|---|---|
| `CacheData` | `OnModuleCacheBefore` | `StatusEffectCatalogService.CacheData` | 56 | line 320, with the three below | `Rerun` (`BootHandlerClassification.txt` line 57) |
| (new) `RegisterStatusEffectListener` | `OnModuleCacheBefore` | `MeleeTechniqueService` | after 56 | line 320 | `Rerun` |
| (new) `RegisterStatusEffectListener` | `OnModuleCacheBefore` | `CombatAbilityImpactService` | after the one above | line 320 | `Rerun` |
| (new) `RegisterStatusEffectListener` | `OnModuleCacheBefore` | `CapstoneProofService` | after the one above | line 320 | `Rerun` |
| `CreatureStatService.RegisterStatEffectListener` | `OnModuleCacheBefore` | unchanged (the stat listener keeps its interface and handler) | 55 | line 313, unchanged | `Rerun` |
| `OnPlayerEnter` | `OnModuleEnter` | `StatusEffectSessionService.OnPlayerEnter` | 764 | line 323 | `LoginOnly` (`PlayerSessionClassification.txt` 65) |
| `OnPlayerExit` | `OnModuleExit` | `StatusEffectSessionService.OnPlayerExit` | 834 | line 324 | `LogoutOnly` (175) |
| `OnApplyStatusEffect` | `status_apply` | `StatusEffectNativeService.OnApplyStatusEffect` | 1128 | line 321 | - |
| `OnStatusEffectInterval` | `status_interval` | `StatusEffectNativeService.OnStatusEffectInterval` | 1131 | line 326 | - |
| `OnRemoveStatusEffect` | `status_remove` | `StatusEffectNativeService.OnRemoveStatusEffect` | 1134 | line 325 | - |
| `OnDealtDamage` | `swlor_damage` | `StatusEffectCombatEventService.OnDealtDamage` | 1160 (between `PoisonsService.ConsumePoisonCoating` and `StealthService.BreakStealthOnDamageDealt`) | line 322 | - |

The renames line after P5-5c and P5-5d reads:
`SWLOR.Game.Server.Service.StatusEffect.CacheData SWLOR.Game.Server.Domain.Character.StatusEffectCatalogService.CacheData
SWLOR.Game.Server.Domain.Combat.MeleeTechniqueService.RegisterStatusEffectListener
SWLOR.Game.Server.Domain.Combat.CombatAbilityImpactService.RegisterStatusEffectListener
SWLOR.Game.Server.Domain.Quests.CapstoneProofService.RegisterStatusEffectListener` (one line). The three handlers call `RegisterMeleeListener` and `RegisterAppliedListener` on `IStatusEffectService`.

- **Order inside `OnModuleCacheBefore` is not load-bearing:** the registrations only fill a list the first notice reads, and the status
  catalog reads no listener. A reload runs rerun boot handlers before adoption (`ServerBootstrapper.RebuildAfterReload`), so the
  list is full before part 7's `OnCarriedStateAdopted` notifies (it calls the movement-rate notice).
- **The three new handlers each add a manifest line and a `BootHandlerClassification.txt` line** (`Rerun`), in the same PR that adds the
  registration (P5-5d). `ScriptHandlerOrderTests`, `BootHandlerClassificationTests`, `PlayerSessionClassificationTests` and
  `ParityHandlerMappingTests` guard all of it.
- **Event order inside a slot stays.** `OnDealtDamage` keeps its place after `PoisonsService.ConsumePoisonCoating` in `swlor_damage`;
  the three effect-script handlers keep their one-per-event slots; the enter and exit handlers keep their positions among the
  `Restore`/`Quiesce` steps.

## The upward references and how each goes

`UpwardEdges.txt` lists `StatusEffectService.cs` with Character -> Combat (17 members), -> Abilities (3) and -> Quests (3), and
`StatusEffectRules.cs` Character -> Combat (1). `DeferredResolution.txt` has its 2 up `Lazy<>` and 1 inside `Lazy<>`.
`LogicToContent.txt` has 1 line (`Feature/AbilityDefinition`: `TemporaryHitPointEffects`).

| Reference | Fix | PR |
|---|---|---|
| `IResistanceService` (6 uses) | port `IStatusResistance` (Character): `IsValidResistanceType`, `HasImmunity`, `CalculateResistedTicks`; `IResistanceService : IStatusResistance`, registered with `AddSingleton<IStatusResistance>(p => p.GetRequiredService<IResistanceService>())` as `IResourceRestoredListener` is. Plain in part 4. The provider needs only `IDBService`, `IMimicryService`, the stat reader and the trackers, so no path leads back to parts 3 to 7 (rule 2) | c, g |
| `IPvpCombatService.IsPlayerAgainstPlayer` | static `PlayerControlRules` (rule 2); `PvpCombatRules` to Character | c |
| `IStealthService.IsConcealed` (2) | static `ConcealmentRules` (rule 2) | c |
| `IQuickCastService.KnowsFeat` | static beside `QuickCastFeatMap` in Foundation (rule 2) | c |
| `Lazy<ICombatAttackTimingService>` | body down: a stat read in part 6 (rule 2); the up `Lazy<>` goes | c |
| `IMeleeTechniqueService` (3) | `IStatusEffectMeleeListener.OnStanceChanged(creature)` and `OnBeforeDirectDamage(attacker, defender, deliveryType)`; `MeleeTechniqueService` implements and registers (rule 3) | d |
| `Lazy<ICombatAbilityImpactService>` (2) | `IStatusEffectAppliedListener.OnStatusApplied(source, creature, categories)` (the stamina drain) and `OnHostileDebuffApplied(source, ability)` (the party buff); `CombatAbilityImpactService` implements and registers (rule 3); the second up `Lazy<>` goes | d |
| `ICapstoneProofService` | `IStatusEffectAppliedListener.OnApplicationCompleted(source, creature, type, ability)`, fired where `RecordCapstoneProof` runs; `CapstoneProofService` implements it (the other two notices empty) and registers (rule 3); it gains `IStatusEffectService` (3 -> 4 services) | d |
| `IStatEffectListener` (Stat split) | unchanged; its list sits beside the two new lists in part 1 | d |
| `Lazy<IStatService>` (inside Character) | plain `IStatService` in parts 4 and 6: `StatService` depends on part 1 only, so no plain path leads back | g |
| `AppliedEffects` (Native) | stays behind its seam (1 member); P5-16b | - |
| `TemporaryHitPointEffects` (content) | stays behind its seam in part 5; P5-12 (the `LogicToContent.txt` line is re-keyed to `StatusEffectNativeService.cs`) | h |
| `Feature.GuiDefinition.RefreshEvent` using | stale (the three events are `Domain/Foundation`); the using goes with the moved code | c-h |
| `Combat -> Feature/StatusEffectDefinition` (115 members in Combat, 186 references in all) | **kept**, P5-7 designs the keys (settled decision 3) | - |

After P5-5: `StatusEffectService.cs` has no Combat, Abilities or Quests reference (23 members removed from `UpwardEdges.txt`, plus the
`StatusEffectRules.cs` line); the new files are Character-only except the Native seam. No `Lazy<>`, `Func<>` or late lookup is added.

### Combat and Abilities holders that lose a `Lazy<>`

Six types hold `Lazy<IStatusEffectService>` because the service used to point at them: `MimicryService`, `MeleeTechniqueService`,
`PvpCombatService`, `ResistanceService`, `StealthService` and `AIService`. Once part 1 depends only on the catalog and Foundation
services and parts 3 to 7 depend on no one of them, the expected result is that each takes the interfaces it uses plainly (the Stat
split removed seven `Lazy<>` the same way). `ResistanceService`'s `Lazy<IStatService>` follows if `StatService` has no plain path to it.
The pull request that makes a `Lazy<>` plain runs `LazyDependencyCycleTests`; a `Lazy<>` the scan still needs stays and is recorded.
Count of up `Lazy<>` in this item: 2 (the service's own). The six holder `Lazy<>` are down, so they are the "30 inside or down" the
domain order says P5-15 breaks if this item does not (P5-5h tries first).

### How it fits P5-8e, P5-7 and P5-10

- **P5-8e (Combat ports, after P5-5 and P5-15a, before P5-7).** P5-5 runs first and edits `MeleeTechniqueService`, `StealthService`,
  `PvpCombatService`, `ResistanceService`, `AIService` and `CompanionControlService` only for their `IStatusEffectService`
  parameters and the three registrations (rechecked at `c153dd169`, after P5-8d merged as #3446: `AIService` already takes Combat's
  `IPartyRoster`, `SpawnObjectService` exists, and `AIService` (15) and `CompanionControlService` use only part 1, so P5-5h only turns their
  `Lazy<IStatusEffectService>` plain, in whichever P5-8 part file holds it then); P5-8e then edits `StealthService.cs` and `EnmityService.cs` on top. **This map does not
  need the "effect lifecycle subscription in `StealthService`" that the Creatures split's D3 and the domain order name:** Stealth's
  two uses are pure engine reads, so Stealth gets `ConcealmentRules` and a one-line body, not a listener (D3 drops that clause in the
  same commit as this amendment). The listener lists live in part 1 (not in a Combat service), so P5-8e's own `IStealthAggroListener`
  list is independent. P5-8e runs after P5-5 has merged.
- **P5-7.** `CombatAbilityImpactService`, `CombatHitEffectsService`, `CombatDamageTakenService` and `CombatAbilityActivationService` are
  P5-7's files and holders (below). P5-5 edits each for fields, parameters and call receivers only; P5-7 then splits them. The
  three Combat registrations add a method and a handler to `MeleeTechniqueService` and `CombatAbilityImpactService`, about 12 lines each.
  Neither P5-7 nor P5-8e should start on a file P5-5 is changing; the order below says when each file is busy.
- **P5-10.** `PerkService` (2 remove calls), `MimicryService` (2) and `BeastMasteryService`/`DroidService` (1 apply each) only change
  their receiver; none changes services.

## Holders of `IStatusEffectService`

41 files in `Domain/` take `IStatusEffectService` (tests not counted). 19 of them use members of more than one new part and gain one to
three services; the other 22 keep one. Counts are the guard's (distinct constructor services plus `GameServices` lookups), measured from the
constructors at `c153dd169`; the method reproduces the baseline's 13, 14, 17 and 18. Coordinator decision (2026-11-17): growth is accepted only
for the four P5-7-owned Combat holders that are already over the limit; each is recorded in the PR that causes it, and P5-7 removes it. Every other
holder stays at 10 or fewer. No holder gains a `Lazy<>`.

| Holder | Parts used | Services now | After | Rule |
|---|---|---:|---:|---|
| `CombatHitEffectsService` | 1, 3, 4, 6 | 17 | 20 | accepted, P5-7 removes |
| `CombatDamageTakenService` | 1, 3, 6 | 18 | 20 | accepted, P5-7 removes |
| `CombatAbilityImpactService` | 1, 3, 4 | 14 | 16 | accepted, P5-7 removes |
| `CombatAbilityActivationService` | 1, 3 | 13 | 14 | accepted, P5-7 removes |
| `CombatActivityService` | 1, 3 | 9 | 10 | at the limit |
| `CombatDamageModifiersService` | 1, 3 | 8 | 9 | |
| `CombatAttackTimingService` | 1, 6 | 7 | 8 | |
| `PairedRangedTraitsService` | 1, 3, 4 | 6 | 8 | |
| `MeleeTechniqueService` | 1, 3 | 6 | 7 | |
| `StealthService` | 1, 3 | 6 | 7 | |
| `AbilityAuraService` | 1, 2, 3 | 4 | 6 | |
| `AbilityStatusApplicationService` | 1, 3 | 4 | 5 | |
| `CombatSuppressionService` | 1, 3 | 4 | 5 | |
| `OrdinaryAttackTraitsService` | 1, 3, 4 | 2 | 4 | |
| `PoisonsService` | 1, 3, 4 | 2 | 4 | |
| `PerformanceInspiredService` | 1, 3, 4 | 1 | 3 | |
| `PvpCombatService` | 1, 3 | 2 | 3 | |
| `CraftStyleEffectsService` | 1, 3 | 2 | 3 | |
| `DeathService` | 1, 3 | 10 | 11 | accepted, P5-7 removes (decision 10) |

Other changes: `CapstoneProofService` 3 -> 4 (it takes `IStatusEffectService` to register). `AIService` (15) uses only part 1 and stays at 15.
`StatService`, `CreatureStatService`, `ResistanceService`, `PerkService`, `CompanionControlService` and the Stat, Mimicry and Space holders use
part 1 only and are unchanged except that their `Lazy<>` may go plain. What was done to keep holders at 10 or fewer: `ClearStatusEffectsOnDeath`
stays in part 1 (it was going to the session part), so `DeathService` needs two parts, not three.

**`DeathService` (Combat) crosses 10 (10 -> 11), accepted (decision 10).** It calls `GetCreatureStatusEffects` and `ClearStatusEffectsOnDeath`
(part 1) and `ApplyStatusEffect` for `KnockdownStatusEffect` and `SubdualPenaltyStatusEffect` (part 3, lines 114-115). No member can move to a part it
already holds, because it holds no other status effect part, and putting `ApplyStatusEffect` on `IStatusEffectService` would be a forwarding layer. The
ways out are to drop one of its other ten services in a pull request that may touch `DeathService` (none is related to this item), or to accept 11 until
the Combat split reaches it. 
## Callers

Counts are call sites (server, content, tests, engine tests together) by destination.

| Destination | Calls | Notes |
|---|---:|---|
| `IStatusEffectService` (stays) | about 610 | `HasStatusEffect` 276, `GetStatusEffect` 162, `RemoveStatusEffect` 117, `GetCreatureStatusEffects` 112, `HasEffect` 48, and the rest of the reads and removals; no receiver changes |
| `IStatusEffectApplicationService` | about 460 | `ApplyStatusEffect` 441, `ApplyPermanentStatusEffect` 17 (engine 126, tests 90, server 240) |
| `IStatusEffectCombatEventService` | about 100 | notices 40, swing scope 34, limited attack 17, guarded hit 5 |
| `IStatusEffectDurationService` | about 52 | refresh 23, restart 15, extend 14 |
| `IStatusEffectCatalogService` | about 27 and 14 test `CacheData` calls | create, definition, name, source type, icon search |
| `IStatusEffectSessionService` | about 7 | `SaveTogglesForRestart`, enter and exit in tests |
| `IStatusEffectNativeService` | about 25 | mostly tests (`OnNWNStatusEffectInterval` 14, `OnRemoveNWNStatusEffect` 7) |
| static rules (`StatusEffectRules`, `StatusEffectIconMap`) | about 61 | pure duration members, the two icon lookups; tests and CombatSim |

`RemovedFacades.json` lists the members by owner and `Apply-FacadeRemoval.ps1` rewrites them (property name only), with `-Reverse`
for the parity build and `MasterCopy.ps1`, as P5-3 and P5-4 did. A call text change is a receiver change: `StatusEffect.ApplyStatusEffect`
becomes `StatusEffectApplication.ApplyStatusEffect`. Properties are named so no class shares a name (`StatusEffectApplication` and the
others are not class names in the server). The seven new interfaces and services get entries in `ConvertedServices.json` and
`DomainMap.json` (Character), and a registration line each in `ServiceRegistrations`.

## Tests

The same layout as the Stat split's P5-4a: the test suite learns where each member lives before any code moves.

- **`StatusEffectSource`** (`Tests/Support`) is the source of `StatusEffectService` wherever it lives: `PartFiles`, `InterfaceFiles`, `Read`,
  `Method`, `Member`, `Members`, `Nodes`, `Paths`; a call through a field of a part interface loses its receiver. `StatusEffectSourceTests`
  proves it. Source readers, all read through it from P5-5b: `StatusEffectDeliveryTests` (5 reads), `PlayerMessageAuditTests` (2),
  `BlazingSpikesStatusEffectTests`, `CharacterSheetStatCoverageTests`, `CombatUpgradeMigrationCoverageTests` (asserts on
  `_loggedOutPlayerEffects[playerId] = new LoggedOutStatusEffects(...)`), `DestroyedOwnerCleanupTests`, `NativeControlStatusEffectTests`,
  `PlayerFacingNameBroadcastTests`, `ForceSensitiveEspionageTests`, `GeneratedWeaponPerkBehaviorTests`,
  `LeadershipOffensivePartyBuffTests`, `RifleSuppressionFeedbackTests`, `CombatDamageTests`, `CraftingStyleStatusEffectTests`,
  `PerformanceAudienceCostTests`, `PerkRefundStatusEffectCleanupTests` and `ClockSeamSourceGuardTests` (18 files in all; the Phase 5 guide
  says 16: two were added since).
- **`StatusEffectBench`** gets one accessor per part (`Service`, `Catalog`, `Application`, `Duration`, `Native`, `CombatEvents`, `Session`);
  until a part moves its accessor is the one `TestStatusEffectService`. The listener stand-ins register with the instance, so the existing
  log lines (`ReapplyFoodHP`, `ApplyCreatureMovementRate`, `StaminaDrain`, `HostileBuff`, `OnStanceChanged`, `PrepareDirectDamage`,
  `CapstoneProof`) keep their text and order. The 12 constructor arguments become the per-part lists above.
- **`TestStatusEffectService`** (`StatusEffectBench.cs:190`) splits into partial files by the part whose code calls each seam
  (`.Core`, `.Catalog`, `.Application`, `.Duration`, `.Native`, `.CombatEvents`, `.Session`); `StatusEffectServiceDefaultSeamsTests` (196
  lines) runs each part's seams on a subclass of that part.
- **`StatusEffectServiceTestBase`** (86 lines) gives each class the accessor it needs.
- **Reflection users** (the complete list; `SWLOR.Game.Server.EngineTests` has none):
  - `Support/StatusEffectInternals.cs`: `CreatureEffects` (part 1), `NativeAttackSwingDepth` and `DeferredNativeAttackStatusEffects` (part 6),
    `Invoke<float>("GetStatusEffectDurationSeconds")` (part 5, 2 callers); it resolves the instance through `FacadeAccessor`, so it
    gets one resolver per part type. Its callers: `AIModelTests` (4), `StatusEffectServiceDefaultSeamsTests` (3),
    `DamageOverTimeStatusEffectTests` (2), `CombatFacadeAllocationTests`, `StanceStatusEffectTests`, `AreaAbilityFragmentationTests`,
    `GeneratedWeaponPerkBehaviorTests` (4), `LeadershipCombatUpgradeTests`, `LeadershipOffensivePartyBuffTests`,
    `SaberstaffAreaEffectsTests`, `WeaponStatusSpreadTests`, `CombatAttackDelayTests`, `StatAdjustmentSourceTests`;
  - `Core/ReloadStateCarrierTests.cs` (lines 42, 95-112): `FieldsOf(typeof(StatusEffectService))` with `_creatureEffects` and the private
    nested `LoggedOutStatusEffects` of `_loggedOutPlayerEffects`; it follows the field to part 7;
  - `Composition/ReloadWiringTests.cs:28` (`.Contain("_creatureEffects")`, part 1, unchanged);
  - `Core/ReloadDigestTests.cs:255` (`_creatureEffects`, unchanged) and the four snapshot files (`ReloadAdoption.txt`,
    `ReloadCarryHistory.txt`, `BootHandlerClassification.txt`, `PlayerSessionClassification.txt`).
- **The allocation pins** (`StatusEffectFacadeAllocationTests`, 7 pins on `GetStatAdjustment`, `GetStatSources` and `HasAnyActiveEffect`) stay
  on part 1, whose reads keep their zero allocation; the set-up calls `OnApplyNWNStatusEffect` (part 5) and `RemoveCreature` (part 1).
  `CombatFacadeAllocationTests` and `GameServicesAllocationTests` add the new properties.
- **Guard tests per PR** (build once with `-p:RunPostBuildEvent=Never -v q -clp:ErrorsOnly`, then `dotnet test <project> --no-build --filter`):
  - characterization: `StatusEffectServiceApplyTests` (955, part 3 and 4), `...DurationTests` (375, parts 4 and s1), `...RemovalTests` (427, part 1),
    `...LifecycleTests` (586, parts 5 and 7), `...CombatTests` (235, part 6), `...CatalogTests` (146, part 2), `...RestartTests` (140, part 7),
    `...ReloadTests` (55, part 7), `StatusEffectServiceDefaultSeamsTests`, `StatusEffectFacadeAllocationTests`;
  - `StatusEffectDeliveryTests`, `PlayerMessageAuditTests`, `ScriptHandlerOrderTests`, `BootHandlerClassificationTests`,
    `PlayerSessionClassificationTests`, `ParityHandlerMappingTests`, `ReloadStateCarrierTests`, `ReloadCarryHistoryTests`,
    `ReloadDigestTests`, `ClockSeamSourceGuardTests`, `LazyDependencyCycleTests`, `ConvertedServiceRulesTests`, `PvpCombatRulesTests`
    (moves with the type), `PvpCombatServiceTests`, `StealthServiceTests`, `ResistanceServiceTests`, `MeleeTechniqueServiceTests`,
    `QuickCast...` tests, and `Phase5DebtTests`/`DomainMapTests` in `SWLOR.Architecture.Tests` (baselines regenerated each PR);
  - new: `StatusEffectSourceTests`, an adoption-order test (part 1 before part 7), a carry test with one text feeding two classes,
    a listener-empty test (each list throws when it notifies empty, as the stat list does), the apply-order characterization for the eight steps, and `IStatusResistanceTests` (the port answers
    what `ResistanceService` answers);
  - engine tests: `StatusEffectEngineTests`, `StatusEffectRefreshInPlaceEngineTests`, `StatusEffectTickStatEngineTests`,
    `Characterization/StatusEffectScenarios` and `HotReloadPlayersEngineTests` (a player with effects through a reload and a logout), filtered
    with `StatusEffect|HotReloadPlayers`; the hot-reload check (`scripts/run-hot-reload-check.sh --engine-filter HotReloadPlayers --reloads 2`)
    runs on P5-5e and the last PR.
- **CombatSim readings** (team-fight, twin-pistol-barrage, twin-pistol-crossfire, solo-boss; seed 1, 50 runs) must be byte-identical:
  CombatSim reads `StatusEffectRules` and `PvpCombatRules`, so the type move and the new members are checked there.
- **Coverage.** Each new part reaches the floor in its own PR (`CoverageFloors.json`). The native reads (parts 5 and 7:
  `HasEffectByTag`, `RemoveEffectByTag`, the adoption loop) run only in the engine; the unit suite covers part 5 at about 60% and part 7
  at about 75%, so P5-5b measures the current floor for `Domain/Character` and the gate runs with coverage.

## Order

Top-down: a part leaves only when nothing still in `StatusEffectService` calls it, and a moved part calls the members still there through
`IStatusEffectService` (promoted from private to public for that PR; the next PR moves them to the new interface). Estimates are for
the diff in the PR, not the tree.

| PR | Contents | Moved lines | `StatusEffectService` services after | Up `Lazy<>` left | Files |
|---|---|---:|---:|---:|---|
| P5-5a | This guide | 0 | 16 | 2 | the guide, README, Phase 5 link |
| P5-5b | Test groundwork: `StatusEffectSource`, bench accessors, double partials, seam tests, allocation pins grouped, reflection users re-pointed; no production code | 0 | 16 | 2 | `Tests/Support/*` (6), 18 source-reading tests, 10 `Domain/Character/StatusEffect*Tests`, `StatSourceTests` guard line |
| P5-5c | Leaves and statics: catalog (`CacheData` handler moves), `StatusEffectIconMap`, `LeadershipDamageReductionRules`, pure members into `StatusEffectRules`, `ConcealmentRules`, `PlayerControlRules`, `PvpCombatRules` and `QuickCastFeatMap` type moves, `IStatusResistance`, the suppression read down; renames line 320 | 390 | 13 | 1 | `StatusEffectService.cs`, `StatusEffectRules.cs`, 4 new Character files, `StealthService.cs`, `PvpCombatService.cs`, `QuickCastService.cs`, `ResistanceService.cs` and its interface, `DomainOrderTypeMoves.json`, `ServiceRegistrations.cs`, `GameServices.Character.cs`, `ScriptHandlerOrder.txt`, `HandlerRenames.txt`, `ClockSeamFiles.txt`, `ConvertedServices.json`, `DomainMap.json`, `RemovedFacades.json`, call sites (about 90) |
| P5-5d | `IStatusEffectMeleeListener` and `IStatusEffectAppliedListener`, their lists and notices in part 1, and the three registrations on `MeleeTechniqueService`, `CombatAbilityImpactService`, `CapstoneProofService` | +40 | 10 | 0 | `StatusEffectService.cs`, the two new listener interfaces, the three implementers, `ScriptHandlerOrder.txt`, `HandlerRenames.txt` (320), `BootHandlerClassification.txt`, test bench |
| P5-5e | Part 7 `StatusEffectSessionService`; `[ReloadCarriedFrom]`; adoption-order test; 12 callers | 270 | 8 | 0 | `StatusEffectService.cs`, new part and interface, `ScriptHandlerOrder.txt`, `HandlerRenames.txt` (323, 324), `PlayerSessionClassification.txt`, `ReloadAdoption.txt`, `ReloadCarryHistory.txt`, `ClockSeamFiles.txt`, `PlayerMessageAudit.json` (none), tests |
| P5-5f | Part 3 `StatusEffectApplicationService` with the `ApplyStatusEffectInternal` cut; the apply timing and refresh still in part 1 behind promoted members; about 460 callers; the 19 holders gain the `StatusEffectApplication` parameter | 410 | 7 | 0 | `StatusEffectService.cs`, new part, `ServiceRegistrations.cs`, `GameServices.Character.cs`, `RemovedFacades.json`, `PlayerMessageAudit.json` (7 rows re-keyed), `LongMethods.txt`, 19 holders, about 460 call sites in content and tests |
| P5-5g | Parts 4 and 6; `IStatService` plain in both; call sites for refresh, restart, extend and the notices | 685 | 4 | 0 | `StatusEffectService.cs`, two new parts, holders `CombatHitEffectsService`, `CombatAbilityImpactService`, `CombatDamageTakenService`, `PoisonsService`, `OrdinaryAttackTraitsService`, `PairedRangedTraitsService`, `PerformanceInspiredService`, `AbilityDamageDeliveryService`, `AbilityImpactLifecycleService`, `CombatAttackTimingService`, about 150 call sites |
| P5-5h | Part 5 `StatusEffectNativeService`; the three effect-script handlers; the holders' `Lazy<IStatusEffectService>` plain where the scan allows; baselines regenerated; `DebtReport.md` | 310 | 4 | 0 | `StatusEffectService.cs`, new part, `ScriptHandlerOrder.txt`, `HandlerRenames.txt` (321, 325, 326), `LogicToContent.txt`, `DeferredResolution.txt`, `ServicesPerType.txt`, `LargeTypes.txt`, `LongMethods.txt`, `UpwardEdges.txt`, the six holders |

P5-5 ends at `StatusEffectService` about 800 lines, 4 services, no method over 150 lines (the longest is `RemoveStatusEffectsFromAllTargetsBySource`
at 45), no `Lazy<>`, no Combat, Abilities or Quests reference. Seven pull requests after the guide. Each PR is one branch from up-to-date
`origin/master`'s successor (the refactor branch), gets a Codex review of the current head, and runs the full gate with coverage as P5-4's did.

Estimates per PR (lines moved and written, services on the stay-part): see the table; the moves are verbatim except the apply cut (P5-5f)
and the two small seams (P5-5c). P5-5f is the largest diff by call sites (about 460) but the smallest risk per line: the call rewrite is the
tool's. P5-5c is the one that edits other items' files (Stealth, Pvp, QuickCast, Resistance); each edit is under 15 lines.

## Master freeze

P5-5c to P5-5h run under a master freeze that the owner confirmed on 2026-11-17. P5-5b is tests only and needed no freeze. The freeze covers the work from P5-5c,
the first PR that moves code out of the service file, to the merge of P5-5h. The master files whose edits would need hand porting
(master `88f02ed6a`, local ref):

- `SWLOR.Game.Server/Service/StatusEffect.cs` (2,695 lines, 27 commits since 2026-10-10): master edits to it port by the range table of
  this guide's "Members by part" to the part that owns the range. Since P5-4, master edits to converted services merge into the instance classes
  without hand work; each split ends that for its files.
- 17 master tests that read it by path (hand port if master changes the string they assert on): `Feature/BlazingSpikesStatusEffectTests`,
  `Feature/CharacterSheetStatCoverageTests`, `Feature/CombatUpgradeMigrationCoverageTests`, `Feature/DestroyedOwnerCleanupTests`,
  `Feature/NativeControlStatusEffectTests`, `Feature/PlayerFacingNameBroadcastTests`, `Perks/CombatUpgradeBibleSyncTests`,
  `Perks/ForceSensitiveEspionageTests`, `Perks/GeneratedWeaponPerkBehaviorTests`, `Perks/LeadershipOffensivePartyBuffTests`,
  `Perks/RifleSuppressionFeedbackTests`, `Service/CombatDamageTests`, `Service/CraftingStyleStatusEffectTests`,
  `Service/PerformanceAudienceCostTests`, `Service/PerkRefundStatusEffectCleanupTests`, `Service/PlayerMessageAuditTests`,
  `Service/StatusEffectDeliveryTests`.
- `SWLOR.Game.Server/Readmes/PlayerMessageAudit.json` (12 rows keyed by the service file: 770 and 790 timing, 926-958 announcements, 1122 and
  1645-1654 refresh and restart, 1268 failures, 2215-2217 worn off).

That is 19 files (1 + 17 + 1). Content edits that only call the service merge without hand work (call text is rewritten by
`Apply-FacadeRemoval.ps1` and its `-Reverse`). Not in the freeze: the status effect types
(`Service/StatusEffectService/*`, `Feature/StatusEffectDefinition/*`), which this split does not move. Master's later syncs run the mechanical
scripts first and port the service range by hand with a Port log row.

Open master PR at the freeze start: #3448 (status extension adds exactly the requested time) changes one line of
`ExtendStatusEffectDuration` in `Service/StatusEffect.cs` (part 4, duration) plus the status effect types. If it merges during the freeze,
the sync ports that line by hand to whichever file holds `ExtendStatusEffectDuration` at the time.

## Settled decisions (coordinator, 2026-11-17)

The coordinator reviewed the first draft and settled its nine decisions; the guide above is written to them.

1. **Holder growth:** accepted only for the four P5-7-owned Combat holders already over the limit (`CombatHitEffectsService`, `CombatDamageTakenService`,
   `CombatAbilityImpactService`, `CombatAbilityActivationService`); each is recorded in the PR that causes it and P5-7 removes it. Every other holder stays
   at 10 or fewer (Holders table; `DeathService` is decision 10).
2. **Duration and Application stay separate parts.**
3. **Effect keys for Combat belong to P5-7.** P5-5 keeps the `Type` APIs it moves unchanged.
4. **Listeners:** small interfaces registered as lists, the existing pattern (`IStatEffectListener` stays; `IStatusEffectMeleeListener` and
   `IStatusEffectAppliedListener` are added; a notice an implementer does not use is empty). No default interface members. The server has one
   (`ICharacterMigration.MigratePlayerData`), so they are not unknown, but nothing here needs them.
5. **Resistance is the port `IStatusResistance`,** not a push.
6. **Statics:** `PvpCombatRules`, `PlayerControlRules` and `ConcealmentRules` in Character beside `StatusEffectRules`; `QuickCastFeatMap` in Foundation.
7. **P5-8e's D3:** no Stealth subscription is needed; D3 in the Creatures split drops that clause (done in this commit).
8. **The freeze** was confirmed by the owner on 2026-11-17 (Master freeze above).
9. **P5-8:** P5-8d merged (`737210cdd`, #3446); the `AIService` and `CompanionControlService` rows are rechecked against the feature tip (Combat ports
   section); P5-8e runs after P5-5.

10. **`DeathService` (Combat) 10 -> 11 is accepted in P5-5f,** the PR that gives it `IStatusEffectApplicationService`, like the four P5-7 holders: recorded
   in that PR with P5-7 named as the remover (P5-7's item list now names `DeathService`). The `ClearStatusEffectsOnDeath` cut change (it stays in part 1)
   is approved.

## Open decisions

None. The freeze start was confirmed by the owner on 2026-11-17 (Master freeze above).

## Progress

### P5-5c (leaves and statics)

`StatusEffectService` 2,833 -> 2,397 lines, 16 -> 13 services, up `Lazy<>` 2 -> 1 (`ICombatAttackTimingService` gone; `ICombatAbilityImpactService` stays for P5-5d).

- **Part 2** `StatusEffectCatalogService` behind `IStatusEffectCatalogService` (Character, `GameServices.StatusEffectCatalog`; 146 lines, 0 services) holds
  `CacheData` (the handler, `HandlerRenames.txt` line 320, `ScriptHandlerOrder.txt` and `BootHandlerClassification.txt` renamed in place), the cache, creation,
  the definition reads and the `GetStatusEffectTypes` seam. Two members the part-1 reads need are added: `FindDefinitionByName(name)` (the saved-toggle search over
  the cache) and `TryGetCategories(type, out categories)` (`HasStatusEffectCategory(Type, ...)` stays in part 1 and reads it). `TryCreateStatusEffect` is public.
  `GetStatusEffectsFromIcon(EffectIconType)` moved to the catalog; the creature overload stays in part 1, so `RemovedFacades.json` lists the other five names only.
- **Statics:** `StatusEffectIconMap` (176 lines), `LeadershipDamageReductionRules` (79), five pure members and `FormatDurationSeconds` into `StatusEffectRules` (123);
  the three forwarders are gone and callers use the rules. `AbilityAuraService` gains `IStatusEffectCatalogService` (4 -> 5 services).
- **Down:** `IStatusResistance` (Character, 3 members) is implemented by `IResistanceService` and registered through the same instance; `PlayerControlRules`
  (`IsPlayer`, `GetControllingPlayer`, `IsPlayerAgainstPlayer`) replaces `IPvpCombatService`; `ConcealmentRules` replaces `IStealthService`;
  `QuickCastFeatMap.KnowsFeat` (the map moved to Foundation) replaces `IQuickCastService`; `IsAttackDelayReductionSuppressed` reads the stat itself.
- **Departures from the map:** `StealthService.IsConcealed` and `IsInvisible` keep their bodies, because the invisibility read has the native `AppliedEffects` pre-check
  behind a test seam that a static cannot keep; `ConcealmentRules` holds the engine iteration (`HasEffect`, shared with `StealthService.HasEffect`) and its
  own `IsConcealed` without the pre-check (cold path: application and worn-off messages). `PvpCombatRampStep` moves with `PvpCombatRules` (the rules' public tables
  name it). `PvpCombatService` takes `IStatusEffectService` plainly (its `Lazy<>` became needless when the service lost `IPvpCombatService`; `LazyDependencyCycleTests`).
- **Tests:** `TestStatusEffectCatalogService` is the part 2 double; the bench scripts the engine for concealment, player control and known feats instead of three
  proxies; `StatusEffectSource.PartFiles` gains the catalog and the three statics (`ClockSeamFiles.txt` lists them) and four source needles name `StatusEffectRules.`
  and `ConcealmentRules.` receivers.
