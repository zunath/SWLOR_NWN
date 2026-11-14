# Maintainability Refactor: Phase 5 (Structural Debt)

Phases 0-4 met their measured goals: no static service state, dependency injection, 72% coverage and hot reload. They did
not finish the structural work the plan also asked for. Large services were to be split as they converted, but only
`Combat` and `Property` were. Content and many services still reach other services through a static lookup. And the
calls between domains were recorded instead of reduced (owner decision 9 deferred that to after Phase 2, and it was never
taken back up). Phase 5 fixes that on the branch, before the cutover. Status lives in
[MaintainabilityRefactorStatus.md](MaintainabilityRefactorStatus.md) (rows P5-0 to P5-19). This file goes away at cutover.

Numbers are from the branch at `5282fe9ef` (2026-11-14). P5-0 replaces them with a generated report that each item
shrinks.

## The debt

**Static service lookup.** `Composition/GameServices` resolves services through `FacadeAccessor`. P2-Z decision D1 made it
the bridge for static code once the facades were gone. D1 kept content code off constructor injection and left converting
it to the owner, who was never asked. 801 files use it:

| Area | Files using `GameServices` | Files |
|---|---|---|
| `Domain/` | 232, among them 75 of the 156 services | 1,263 |
| `Feature/` | 553 | 1,886 |
| `Native/`, `NativeSinks/`, `Core/`, `Composition/` | 16 | 166 |

A service that uses it has dependencies its constructor does not show, so its tests and its constructor tell only part of
the story.

**Large services.** About 45 hand-written logic types are over 1,000 lines. Some of the largest, with their constructor
parameter counts:

| Service | Lines | Constructor parameters |
|---|---|---|
| `AbilityService` | 4,573 | 31 |
| `StatService` | 3,364 | 23 |
| `StatusEffectService` | 2,802 | 15 |
| `SpaceService` | 2,763 | 16 |
| `AIService` | 1,877 | 15 |
| `CombatHitEffectsService` | 1,708 | 17 |
| `CombatRecorderService` | 1,690 | 5 |
| `CombatAbilityImpactService` | 1,687 | 13 |
| `CombatDamageTakenService` | 1,641 | 17 |
| `PropertyService` | 1,638 | 8 |
| `ItemService` | 1,611 | 10 |

On the content side: `AppearanceEditorViewModel` (3,497 lines in its main file), `WeaponActiveAbilityDefinitionBase` (2,333),
`ShipManagementViewModel` (2,082), `TintMapService` (1,961, still in `Feature/`), `UsePerkFeat` (1,688) and `DMChatCommand` (1,442).

**Long methods.** 36 methods in logic code are over 150 lines. Examples:

- `OnAIActionAttackObjectSink.AttackObject` (777 lines)
- `ResolveAttackRoll.ResolveWeaponRoll` (417)
- `EventRegistration.HookNWNXEvents` (433)
- `UsePerkFeat.ActivateAbility` (432)
- `CommunicationService.ProcessChatMessage` (326)
- `DroidService.SpawnDroid` (272)
- `SkillService.GiveSkillXP` (237)
- `StatusEffectService.ApplyStatusEffectInternal` (234)

Two GUI layouts are over 400 lines: `StablesDefinition.BuildWindow` (741) and `ShipManagementDefinition.BuildWindow` (414).

**Calls between domains.** `SWLOR.Architecture.Tests/Baseline/CrossDomainEdges.txt` holds 436 edges:

- **43 pairs of domains call each other in both directions.** The worst is `Character` and `Combat`, with 1,383 referencing
  members one way and 873 the other. Next come `Abilities` and `Combat` (181 / 346) and `Abilities` and `Character` (189 / 49).
- **41 edges run from game logic into content** (`Domain` -> `Feature`). The largest is `Combat` -> `Feature/StatusEffectDefinition`
  with 115 members.
- **Services that need each other to be built.** 42 files take 111 `Lazy<>` constructor parameters because they need
  each other: a `Lazy<>` is how the container builds two services that each need the other (P2-Z decision D3).

**Test debt.**

- Six unit tests are excluded because they fail on master (`scripts/ci/run-unit-tests.sh`, "Failing at the base commit").
- The engine sweep has 39 known failures (Reported defects).
- The 10:00 UTC daily restart can end an engine run.
- The smoke run's craft step is stale.

## Targets (owner approval needed)

Phase 5 is done when:

1. **Every service gets its dependencies through its constructor.** `GameServices` and `FacadeAccessor` are deleted, and an
   architecture test keeps any static lookup out.
2. **No logic type is over 1,000 lines** (all partial files of a type together), **and no constructor takes more than 10
   dependencies.**
3. **No logic method is over 150 lines,** and no GUI layout method is over 400.
4. **Game logic never references content** (no `Domain` -> `Feature` edge), and **no two domains depend on each other.** The
   architecture tests change from "no new edge" to these rules. No service takes a `Lazy<>` dependency to get around a
   cycle.
5. **The unit suite runs with no master-failure exclusions.** Every engine failure is fixed, or the test is removed because
   it checks behaviour that no longer exists, with the reason recorded.
6. **No behaviour change:** parity stays identical to master, the characterization snapshots stay unchanged, and coverage
   stays at or above 72% with every area floor.

**Exempt from targets 2 and 3:**

- generated code;
- migrations (history, never edited);
- data tables (attribute-row enums such as `StatType`, `RecipeType`, `StructureType`, `KeyItemType`, `PerkType`, `RecastGroup`);
- content data definitions such as recipes, loot tables, quest and ship tiers, which stay out of this plan under owner
  decision 7;
- fluent builders made of short methods (`AbilityBuilder`).

GUI window definitions count as layout, so only the 400-line layout rule applies to them.

## How a split works

- **One responsibility per service.** For example, `AbilityService` becomes services for activation checks, resource
  costs, recast and cooldowns, telegraphed impacts, and the definition catalog. Each new service has its own interface and
  injects only what it uses. The parts are chosen from the code, the way the Combat split did it
  ([MaintainabilityRefactorCombatSplit.md](MaintainabilityRefactorCombatSplit.md)): a written part map first, then moves.
- **Callers move to the narrow interfaces.** No forwarding layer is kept.
- **Moved code stays verbatim** except for how it reaches its dependencies, so each diff reads as a move.
- **Tests and checks per split:** each new service reaches the coverage floor in the same pull request; the CombatSim
  readings and allocation pins stay unchanged; and handler order and parity are checked as for every item.
- **Content freezes:** short announced freezes on `master` cover the `Ability`, `Stat` and `StatusEffect` splits
  (precedent: owner decision 8).

## Content definitions without the lookup

Content (ability, perk, status effect, GUI, chat command and quest definitions) is discovered by reflection and built with
`Activator`. P5-13 changes discovery to build definitions through the container, so a definition takes services by
constructor like any other class. Static definition classes that hold behaviour become instance classes. Pure data
definitions need no change. P5-13 writes the design and converts one definition family as a pilot. P5-14 converts the
rest in waves with a script (one injected field per service a class uses), then deletes `GameServices` and
`FacadeAccessor`.

## Breaking the domain cycles

P5-1 classifies every two-way pair and every `Domain` -> `Feature` edge by what is referenced:

- **Shared vocabulary** (enums, records, constants that several domains use): moves to the domain that owns it or to
  `Foundation`. This is cheap, and it is where most of `Character` <-> `Combat` is expected to be.
- **Calls:** invert with an interface or an event that the lower domain owns.

It then proposes a layered domain order for the owner, for example
`Foundation` < `Character` < `Combat` < `Abilities` < content. It records that order in `DomainMap.json`, so the guard can
reject an edge that points up a layer. P5-2 removes the `Domain` -> `Feature` edges. P5-15 breaks the remaining cycles once
the splits have made the call edges smaller.

## Items

| ID | Item | Depends on | Model |
|---|---|---|---|
| P5-0 | Debt guards with shrinking baselines (static lookup per file, type and method size, constructor dependencies, `Lazy<>` cycle parameters, `Domain` -> `Feature` edges, two-way domain pairs) and a generated debt report | - | Opus |
| P5-1 | Edge classification, shared-vocabulary moves, proposed domain layering (owner review) | P5-0 | Opus |
| P5-2 | Remove the 41 `Domain` -> `Feature` edges | P5-1 | Sonnet build, Opus review |
| P5-3 | Split `AbilityService` (freeze) | P5-0 | Opus |
| P5-4 | Split `StatService` (freeze) | P5-3 | Opus |
| P5-5 | Split `StatusEffectService` (freeze) | P5-4 | Opus |
| P5-6 | Split `SpaceService` | P5-0 | Sonnet build, Opus review |
| P5-7 | Combat services over the limits: `CombatHitEffects`, `CombatDamageTaken`, `CombatAbilityImpact`, `CombatRecorder`, `CombatDamageModifiers`, `CombatAbilityActivation`, `CombatAbilityBonus`, `CombatAbilityHit`, `CombatService`, `Enmity` | P5-5 | Opus |
| P5-8 | Creatures: `AIService`, `SpawnService`, `CompanionControlService` | P5-0 | Sonnet build, Opus review |
| P5-9 | Items and World: `ItemService`, `PropertyService`, `PropertyLoaderService`, `ShuttleService`, `CraftService`, `BlueprintBonuses` | P5-0 | Sonnet build, Opus review |
| P5-10 | Character and Companions: `PerkService`, `SkillService`, `PlayerNameService`, `MimicryService`, `DroidService`, `BeastMasteryService` | P5-5 | Sonnet build, Opus review |
| P5-11 | Social, UI and Foundation: `CommunicationService`, `PerformanceAudienceService`, `PerformanceService`, `ExtraHotbarsService`; `TintMapService` into `Domain` | P5-0 | Sonnet build, Opus review |
| P5-12 | Static lookup out of the rest of `Domain`, `Native`, `NativeSinks`, `Core` and `Composition` | P5-3 to P5-11 | Sonnet (scripted) |
| P5-13 | Definitions built by the container: design and pilot family | P5-0 | Opus |
| P5-14 | Content waves off the static lookup; delete `GameServices` and `FacadeAccessor` | P5-12, P5-13 | Sonnet (scripted), Opus review |
| P5-15 | Break the remaining domain cycles and the `Lazy<>` service cycles; guard: no two-way pair, layering enforced | P5-2, P5-14 | Opus |
| P5-16 | Long methods and large content-side types (`Native`, `NativeSinks`, `EventRegistration`, `UsePerkFeat`, `WeaponActiveAbilityDefinitionBase`, the view models over the limit, `DMChatCommand`, `EquipmentStats`, the two layouts, player guide topics, `RaceService.LoadRaces`) | P5-0 | Sonnet build, Opus review |
| P5-17 | Test debt: the six master-failing unit tests, the 39 known engine failures, engine runs surviving the daily restart, the smoke craft step | - | Sonnet, Opus for triage |
| P5-18 | Reload follow-ups (optional, owner call): the 1-2 minute reload pause, the host entry's non-collectible frame rate | - | Opus |
| P5-19 | End-of-phase verification (gate, engine sweeps on both entries, reload checks, smoke run, live reload) and an updated cutover handoff | all | Opus |

**Order:**

- P5-3, P5-4, P5-5 and P5-7 run one after another, because those services call each other heavily.
- The other splits, P5-13, P5-16 and P5-17 run alongside them, at most three agents at once, never two on the same files.
- Daily master syncs continue throughout. Port log rows map master's edits into the split services.

**Estimate:** about 25-35 pull requests and two to three weeks of calendar time, mostly set by the serial
Ability-Stat-StatusEffect-Combat chain and the porting of master's edits into the services being split. Token use is
likely 25-40% of a week's usage limit. That is a rough figure, to be checked after P5-3.

## Owner decisions needed

1. The targets and exemptions above.
2. Content definitions built by the container, and `GameServices` deleted (P5-13, P5-14).
3. Short `master` freezes for the `Ability`, `Stat` and `StatusEffect` splits.
4. Removing an engine test only when it checks behaviour that no longer exists, with the reason in the pull request.
5. Whether P5-18 (reload pause, host frame rate) is in Phase 5 or left for later.
