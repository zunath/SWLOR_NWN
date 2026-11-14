# Maintainability Refactor: Phase 5 (Structural Debt)

Phases 0-4 met their measured goals: no static service state, dependency injection, 72% coverage and hot reload. They did
not finish the structural work the plan also asked for:

- Large services were to be split as they converted, but only `Combat` and `Property` were.
- Content and static helpers still reach services through a static lookup.
- The calls between domains were recorded (owner decision 9), and no item has reduced them since.

Phase 5 fixes that on the branch, before the cutover. Status lives in
[MaintainabilityRefactorStatus.md](MaintainabilityRefactorStatus.md) (rows P5-0 to P5-19). This file goes away at cutover.

Numbers are from the branch at `5282fe9ef` (2026-11-14), measured with a syntax scan (types summed over their partial files,
lookups counted in code, not comments). P5-0 replaces them with a generated report that each item shrinks.

## The debt

**Static service lookup.** `Composition/GameServices` resolves services through `FacadeAccessor`. P2-Z decision D1 made it the
bridge for static code once the facades were gone, kept content code off constructor injection, and left converting it to
the owner, who was never asked.

- **Services:** none use it. `ConvertedServicesTests.No_class_the_container_builds_uses_GameServices` forbids it.
- **Server code: 646 files use it.**
  - `Feature/`: 553 files.
  - `Domain/`: 79 files. These are 29 static helper classes, plus types that content builds with `new`: ability and perk
    requirements, quest objectives, `StatusEffectBase`, `GuiViewModelBase`.
  - `Native/` 7, `NativeSinks/` 6, `Core/` 1.
- **Outside the server: about 290 files.** `SWLOR.Game.Server.Tests` has 184, `EngineTests` 77, `CharacterizationTests` 10,
  CraftSim and its tests 8, `IntegrationTests` 4, Toolset 3, and Admin, CLI and CombatSim a few each.
- **The shape of the content code:**
  - Of the 557 content types using the lookup, 78 are static classes (45 of them hold script handlers).
  - 208 are instance classes that call it from static members (158 only from static members).
  - 1,765 of the 4,796 call sites are in a static context.
- **How tests and tools build content:**
  - `new`: 827 sites in `SWLOR.Game.Server.Tests` and 71 in `EngineTests`.
  - `Activator.CreateInstance`: 178 sites in `SWLOR.Game.Server.Tests`, 13 in `EngineTests`, 7 in `CharacterizationTests` and 8 in
    CombatSim.
  - `SWLOR.CombatSim/Data/WeaponImpactReader.cs` decodes ability code and looks for a call to `GameServices.get_Ability`.

Code that uses the lookup does not show its dependencies, and a test of it has to set up the whole container.

**Other static entry points and static state.**

- Static entry points:
  - `Random`, a shared generator that keeps its state (static by P2-C5);
  - `Clock`, the time seam;
  - `Log`, in 140 files (static by P2-C1);
  - `Scheduler`;
  - `FrameProfiler`, with 10 mutable static fields;
  - `NativeHookAccess`.
- Static state in content: 42 content files hold runtime state in 68 static mutable collections, for example
  `EmbattledStatusEffect.Attackers`, the `GuardedStatusEffect` fields and `StoreManagement._stores`.

**Large types.** About 46 hand-written logic types are over 1,000 lines. The largest services, with their constructor parameter
counts:

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

On the content side:

- `AppearanceEditorViewModel`: 6,180 lines over six partial files.
- `WeaponActiveAbilityDefinitionBase` (2,333), `ShipManagementViewModel` (2,082), `TintMapService` (1,961, still in `Feature/`),
  `UsePerkFeat` (1,688) and `DMChatCommand` (1,442).

Counting the lookups as dependencies too, some content and native types already use more than 10 services:

- `UsePerkFeat` 28;
- `GetDamageRoll` 21;
- `ResolveAttackRoll` 20;
- `CharacterSheetViewModel` 19;
- `CharacterChatCommand` and `DMChatCommand` 16 each.

**Long methods.** 36 methods in logic code are over 150 lines. Examples:

- `OnAIActionAttackObjectSink.AttackObject` (777 lines)
- `ResolveAttackRoll.ResolveWeaponRoll` (417)
- `EventRegistration.HookNWNXEvents` (433)
- `UsePerkFeat.ActivateAbility` (432)
- `EmoteChatCommand.BuildChatCommands` (412)
- `CommunicationService.ProcessChatMessage` (326)
- `DroidService.SpawnDroid` (272)
- `SkillService.GiveSkillXP` (237)
- `StatusEffectService.ApplyStatusEffectInternal` (234)
- `PropertyCityGovernanceService.ProcessCityElections` (215)
- `ServiceRegistrations.Register` (204)
- `CombatRecordSerializer.Write` (198)
- `GuiWindowBuilder.Build` (198)

Two GUI layouts are over 400 lines: `StablesDefinition.BuildWindow` (741) and `ShipManagementDefinition.BuildWindow` (414).

**Calls between domains.** The guard (`SWLOR.Architecture.Tests`) counts a domain as its `Domain/` code plus its content
folders (`DomainMap.json`).

- **63 pairs of domains depend on each other in both directions.**
  - The worst is `Character` and `Combat`, with 1,383 referencing members one way and 873 the other (code folders).
  - Next come `Abilities` and `Combat` (181 / 346) and `Abilities` and `Character` (189 / 49).
  - `Foundation` depends on 10 of the domains that depend on it. Those edges are recorded only in `EdgesIntoCoreAndPlatform.txt`.
- **Game logic references content code:**
  - 43 edges from `Domain/` into another domain's `Feature/` folder. The largest is `Combat` -> `Feature/StatusEffectDefinition`
    with 115 members.
  - 7 edges from `Platform/Native` into content.
  - 17 `Domain/` files that use their own domain's content namespace, for example `AbilityService` -> `Feature.AbilityDefinition`.
    The guard does not record these.
- **Services that need each other to be built.** 42 files take 111 `Lazy<>` constructor parameters. A `Lazy<>` is how the
  container builds two services that each need the other (P2-Z decision D3).

**Test debt.**

- Six exclusion entries, covering more than 25 tests, hide tests that fail on master (`scripts/ci/run-unit-tests.sh`, "Failing
  at the base commit").
- The engine sweep has 39 known failures, plus tests that fail only under load.
- The 10:00 UTC daily restart can end an engine run.
- Every engine run uses the same compose project name.
- The smoke run's craft step is stale.
- `SWLOR.Toolset.Tests` fails outside CI.

## Targets (owner-approved 2026-11-14)

Phase 5 is done when:

1. **No code reaches a service through a static lookup.** `GameServices` and `FacadeAccessor` are deleted. Every type that uses
   a service is built by the container or by a factory the container provides, and gets the service through its
   constructor. The static entry points that stay are named and guarded (owner decision 3: `Log`, `Clock`, `Random`,
   `FrameProfiler`). P5-0 classifies the rest (`Scheduler`, `NativeHookAccess` and any others it finds), each kept with a
   reason the owner approves or removed. Runtime state held in static content fields moves into services.
2. **No logic type is over 1,000 lines** (all partial files of a type together), **and no type uses more than 10 services.**
   Services count whether they come in through the constructor or through a lookup that remains. Value constructors,
   such as records and settings, do not count.
3. **No logic method is over 150 lines,** and no GUI layout method is over 400.
4. **Domains follow an order:**
   - No two domains depend on each other.
   - Every edge follows the order the owner approves in P5-1, with `Foundation` and `Platform` at the bottom, depending on
     no domain.
   - No `Domain/` or `Native*/` code references a `Feature/` type, its own domain's included.
   - No service uses deferred resolution (`Lazy<>`, `Func<>`, `IServiceProvider` or a factory that resolves later) to get
     around a construction cycle.
5. **No test is excluded or failing because master is broken.**
   - A fix that only corrects a test lands on the branch.
   - A fix that changes game behaviour lands on `master` as a normal bug fix and reaches the branch through the sync, so
     target 6 holds.
   - Every engine failure is fixed, made stable under load, or removed because it checks behaviour that no longer exists,
     with the reason recorded.
6. **No behaviour change:** parity stays identical to master, the characterization snapshots stay unchanged, and coverage
   stays at or above 72% with every area floor.

**Exempt from targets 2 and 3** (owner decision 1, 2026-11-14; plan decision 7 only keeps data-driven definitions out):

- generated code, including `Feature/BeastDefinition` (174 generated classes of about 1,540 lines, which decision 3 already
  treats as generated);
- migrations (history, never edited);
- attribute-row enums (`StatType`, `RecipeType`, `StructureType`, `KeyItemType`, `PerkType`, `RecastGroup`);
- content data definitions (recipes, loot tables, quest and ship tiers);
- data catalog members that only list data (`MusicSongCatalog.Songs`, `ActiveAbilityAnimationCatalog.Entries`);
- fluent builders made of short methods (`AbilityBuilder`).

GUI window definitions count as layout, so only the 400-line layout rule applies to them.

## How a split works

- **One responsibility per service.** For example, `AbilityService` becomes services for activation checks, resource costs,
  recast and cooldowns, telegraphed impacts and the definition catalog. Each new service has its own interface and injects
  only what it uses.
- **A written part map comes first, then the moves,** as the Combat split did
  ([MaintainabilityRefactorCombatSplit.md](MaintainabilityRefactorCombatSplit.md)). Part maps follow the domain order P5-1
  approves.
- **Each split also removes its service's references to content** (target 4).
- **Callers move to the narrow interfaces.** No forwarding layer is kept.
- **Moved code stays verbatim** except for how it reaches its dependencies, so each diff reads as a move.
- **Tests and tools follow the moves.** Tests that read source files by name must change with them: `AbilityService.cs` is
  read by 21 test files, `StatusEffectService.cs` by 16, `StatService.cs` by 15 and `UsePerkFeat.cs` by 14.
- **Each new service reaches the coverage floor in the same pull request.** The CombatSim readings and allocation pins
  stay unchanged, and handler order and parity are checked as for every item.
- **Content freezes:** short announced freezes on `master` cover the `Ability`, `Stat` and `StatusEffect` splits
  (precedent: owner decision 8).

## Content without the lookup (P5-13 design, P5-14 waves)

Today content is discovered by reflection and built with `Activator.CreateInstance`. P5-13 designs how content gets its
services instead. The design must cover:

- **Static classes and static members:** the 78 static classes (45 with script handlers) and the 1,765 call sites in static
  context. Static classes that hold behaviour become instance classes, with their handlers bound like the services'
  handlers.
- **Objects made per use:** status effects are created per application and view models per window
  (`StatusEffectService`, `CombatQueriesService`, `GuiWindowBuilder`). They are built through a cached factory at today's
  call sites, at today's time, with allocation pins unchanged. Hot reload rule 9 validates the container off the engine
  thread, so registration must not build them early.
- **Hot reload carry:** live status effects are carried across a reload. `_creatureEffects` is `[ReloadState(Carried)]`, and
  `ReloadCarryContractResolver` rebuilds them with `GetUninitializedObject`, which runs no constructor. Injected fields must
  stay out of the carry and be supplied again on adoption, with a reload check that proves it.
- **Tests and tools:** the `new` and `Activator` construction sites in tests and tools get a test factory. The CombatSim
  ability reader changes with the call form.
- **Call text stays the same:** injected members are named like today's `GameServices` properties (`Ability`, `Stat`, ...),
  so a content call keeps its text. Master's edits to content then keep merging without hand work, and
  `tools/Refactor/MasterCopy.ps1` and the parity master build (`Apply-FacadeRemoval.ps1 -Reverse`) are updated for the new
  form.

P5-13 converts one content family as a pilot. P5-14 converts the rest in waves sized from the counts above, updates the
tests and tools outside the server, and deletes `GameServices` and `FacadeAccessor`.

## Domain order and cycles

P5-1 classifies every two-way pair and every logic-to-content reference by what is referenced:

- **Shared vocabulary** (enums, records, constants that several domains use) moves to the domain that owns it or to
  `Foundation`. This is cheap, and it is where most of `Character` <-> `Combat` is expected to be.
- **Interfaces content implements** (for example `IAbilityListDefinition`) move into the domain that consumes them.
- **Calls** are inverted with an interface or an event that the lower domain owns.

It then proposes a domain order for the owner, with `Foundation` and `Platform` at the bottom and content above the code it
uses. The order is recorded in `DomainMap.json`, so the guard can reject an edge that points up. P5-2 removes what the splits
leave. P5-15 breaks the remaining cycles, including the `Lazy<>` construction cycles, once the splits have made the call edges
smaller.

## Risks

- **Master porting:** since the sync of 2026-11-09, master's edits to converted services merge into the instance classes
  without hand work. Each split ends that for its files, so master's edits to them must be ported by hand (Port log rows).
  The call-text rule keeps content merging.
- **`feature/space-revamp`:** this branch has 847 commits not on master, and it must be ported onto `SpaceService`,
  `DroidService` and `ItemService` (Next actions, item 2). P5-6, P5-9 and P5-10 split exactly those services.
- **Hot reload:** the content change touches carried state (see above). Every item that changes a carried class runs the
  reload checks.
- **Scale:** the content waves touch about 840 files, among them about 290 test and tool files.

## Items

| ID | Item | Depends on | Model |
|---|---|---|---|
| P5-0 | Debt guards with shrinking baselines and a generated debt report: lookups per file, type and method size, services per type (constructor plus lookups), deferred-resolution parameters, logic-to-content references (same-domain included), two-way domain pairs (Foundation and Platform included), static entry points and static content state | - | Opus |
| P5-1 | Edge classification, shared-vocabulary and interface moves, proposed domain order (owner review) | P5-0 | Opus |
| P5-2 | Remove the logic-to-content references the splits leave | P5-7 | Sonnet build, Opus review |
| P5-3 | Split `AbilityService` (master freeze) | P5-1 | Opus |
| P5-4 | Split `StatService` (master freeze) | P5-3 | Opus |
| P5-5 | Split `StatusEffectService` (master freeze) | P5-4 | Opus |
| P5-6 | Split `SpaceService` | the other splits (owner decision 5) | Sonnet build, Opus review |
| P5-7 | Combat services over the limits: `CombatHitEffects`, `CombatDamageTaken`, `CombatAbilityImpact`, `CombatRecorder` (and `CombatRecordSerializer.Write`), `CombatDamageModifiers`, `CombatAbilityActivation`, `CombatAbilityBonus`, `CombatAbilityHit`, `CombatService`, `Enmity` | P5-5 | Opus |
| P5-8 | Creatures: `AIService`, `SpawnService`, `CompanionControlService` | P5-1 | Sonnet build, Opus review |
| P5-9 | Items and World: `ItemService`, `PropertyService`, `PropertyLoaderService`, `PropertyCityGovernanceService`, `ShuttleService`, `CraftService`, `BlueprintBonuses` | P5-1; `ItemService` after the other splits (owner decision 5) | Sonnet build, Opus review |
| P5-10 | Character and Companions: `PerkService`, `SkillService`, `PlayerNameService`, `MimicryService`, `DroidService`, `BeastMasteryService` | P5-5; `DroidService` after the other splits (owner decision 5) | Sonnet build, Opus review |
| P5-11 | Social, UI and Foundation: `CommunicationService`, `PerformanceAudienceService`, `PerformanceService`, `ExtraHotbarsService`, `GuiWindowBuilder`; `TintMapService` into `Domain` | P5-1 | Sonnet build, Opus review |
| P5-12 | Static helpers and `new`-built logic types off the lookup (79 `Domain` files, `Native`, `NativeSinks`, `Core`), using the P5-13 design | P5-13, P5-16 | Opus design, Sonnet build |
| P5-13 | Content without the lookup: design (above) and pilot family | P5-0 | Opus |
| P5-14 | Content waves, tests and tools; delete `GameServices` and `FacadeAccessor` | P5-12, P5-13, P5-16 | Sonnet (scripted), Opus review |
| P5-15 | Break the remaining domain cycles and the construction cycles; guards: no two-way pair, the domain order, no deferred resolution | P5-2, P5-14 | Opus |
| P5-16 | Every type and method over the limits that no split covers. Named so far: `Native` (`ResolveAttackRoll`, `GetDamageRoll`), `NativeSinks.AttackObject`, `EventRegistration`, `ServiceRegistrations.Register`, `UsePerkFeat`, `WeaponActiveAbilityDefinitionBase`, the view models over the limit, `DMChatCommand`, `CharacterChatCommand`, `EmoteChatCommand`, `EquipmentStats`, the two layouts, the player guide topics, `RaceService.LoadRaces`; static content state into services | P5-0 | Sonnet build, Opus review |
| P5-17 | Test debt: the six exclusion entries, the 39 known engine failures and the load-dependent ones, engine runs surviving the daily restart, a compose project name per run, the smoke craft step, `SWLOR.Toolset.Tests` outside CI | - | Sonnet, Opus for triage |
| P5-18 | Dropped from Phase 5 (owner decision 7): the reload pause and the host entry's non-collectible frame rate stay in Reported defects | - | - |
| P5-19 | End-of-phase verification (gate, engine sweeps on both entries, reload checks, smoke run, live reload) and an updated cutover handoff | all | Opus |

**Order:**

1. P5-0, then P5-1. The owner approves the domain order before any split starts.
2. The serial chain, one item at a time: P5-3, P5-4, P5-5, P5-7, then P5-2.
3. Alongside the chain, after P5-1: P5-8, P5-9, P5-11, and P5-10 once P5-5 is merged. The `SpaceService`, `ItemService` and
   `DroidService` splits come after every other split (owner decision 5). After P5-0: P5-13 and P5-16. At any
   time: P5-17.
4. At most three agents run at once, and two items never work on the same files. Content types named in P5-16 stay
   untouched by other items until P5-16 merges.
5. Last: P5-12, P5-14, P5-15, then P5-19.

Daily master syncs continue throughout.

**Estimate:** about 60-90 pull requests. For comparison, the Combat split took 11 pull requests for 13 services and facade
removal took 17 items. Phase 5 splits about 33 services and 14 content types and converts about 840 files off the lookup.

Measured costs so far (status file): a conversion costs about 1-3 points of weekly usage and a sync about 2-4. On that basis
Phase 5 needs roughly 120-400 points, which is two to four weeks of the weekly limit, and about five to eight weeks of
calendar time with the serial chain and the 95% stop. P5-3 re-measures this.

## Owner decisions (2026-11-14)

The owner approved all seven as recommended:

1. The targets, and the exemption list as a new decision.
2. Content is built through the container with the call text kept, and `GameServices` is deleted (P5-13, P5-14).
3. The static entry points that stay are `Log`, `Clock`, `Random` and `FrameProfiler`. P5-0 classifies the rest.
4. Short announced `master` freezes cover the `Ability`, `Stat` and `StatusEffect` splits.
5. The `SpaceService`, `ItemService` and `DroidService` splits come last. If `feature/space-revamp` has not landed on master
   by then, they go ahead, and the pull request records where each piece of those services went, for the port.
6. A test fix that changes game behaviour lands on `master` as a normal bug fix. An engine test is removed only when it
   checks behaviour that no longer exists, with the reason in the pull request.
7. P5-18 (reload pause, host frame rate) is not part of Phase 5. Both stay in Reported defects.
