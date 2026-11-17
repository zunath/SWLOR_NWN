# Domain Order (P5-1)

The domain order of the maintainability refactor, approved by the owner on 2026-11-14 with the conditions below. Phase 5
items follow it: their part maps place code by this order, and the guard (P5-1b, then P5-15) rejects an edge that points
up. Measured on `feature/maintainability-refactor` at `369c318be` (2026-11-14), the tree of the P5-0 baselines.

## Summary

- **Order, bottom to top:** Platform, Foundation, Character, UI, Gameplay, Items, Administration, Combat, Abilities,
  Creatures, Crafting, World, Companions, Quests, Space, Economy, Social. All content (`Feature/`) is a band above all
  logic.
- **Vocabulary alone does not break the cycles.** Vocabulary is 61% of the referencing members in the 94 pairs (37%
  without the generated beast classes), but only 9 of the 188 directions are vocabulary only. Moving 99 types and
  extracting the vocabulary that cannot move whole takes the pairs from 94 to 86.
- **Most of the remaining pairs come from how domains were counted.** The recount alone (content band, persistence
  with Foundation, `NativeSinks` as wiring) takes today's 94 pairs to 69. After the moves it takes 84 to 58. Moving the
  hook logic into Combat gives 54, and moving five misplaced classes gives 44.
- **The rest are real calls.** After the moves, 619 member references in 65 directions point up, plus 63 in the hook
  code. Appendix C gives each direction its fix and item. About two in three are inversions; the rest are code moves
  and content fixes.
- **Inversions must not recreate construction cycles.** 28 of the 111 `Lazy<>` parameters point up in this order, 53
  down and 30 stay inside one domain. The supply rule below says how a lower service gets what a higher one provides
  without a `Lazy<>`.
- **Character, Combat and Abilities stay three domains.** Combat uses Character 6.7 times as much as the reverse, and
  Abilities uses Combat 2.0 times as much. Merging all three would leave 36 pairs instead of 54 and cut the upward
  references from 609 to 431, but it would remove no `Lazy<>` and would make one domain of 48 services and 36% of the
  logic lines.

## Owner decisions (2026-11-14)

All four were approved as recommended, with the reviewer's conditions.

1. **Content is a band above all logic.** Logic never names content (target 4). Content may use any logic. Content that
   uses other content follows the order; 3 such pairs remain, and P5-2 removes them. The recount alone takes today's 94
   pairs to 69; after the moves, the band removes 25 pairs (84 to 59). It makes legal about 1,180 member references from
   a lower domain's content into higher logic. Under the band, "Foundation depends on no domain" holds for Foundation's
   logic only: its content (`MigrationDefinition`, `SnippetDefinition`, `DialogDefinition`) may use any logic.
2. **`Entity` and `Enumeration` count as Foundation.** The vocabulary they store moves into `Domain.Foundation`, and
   `Log` and `FrameProfiler` move into `Core` with their support types.
3. **`NativeSinks` counts as wiring, under a "sink does no work" guard.** Today the sinks are not wiring: they are
   2,373 lines, `OnAIActionAttackObjectSink.cs` alone is 801, and four more are 166 to 248 lines. They name 13 Combat
   types and 3 content units. The guard (P5-1b) works like the trampoline rule: a sink only forwards. It starts as a
   shrinking baseline. Sinks stay under the no-content rule (`LogicToContent.txt` keeps counting them). The hook logic in
   `Native/` moves into Combat services after P5-7's part map gives it a home (P5-16b). It builds on P5-16a, which is
   already splitting `AttackObject`, `ResolveWeaponRoll` and `ResolveWeaponDamage` into static helpers in their own
   namespaces. Making `NativeSinks` wiring removes one more pair (59 to 58).
4. **Character, Combat and Abilities stay three domains, and the `AbilityDetail` family moves into Character without
   the AI types.** The order holds by members, by distinct targets and by type pairs. `AbilityDetail` belongs low
   because Character reads it (`IStatusEffect.OriginatingAbility` is an `AbilityDetail`, and perk refunds read it). A
   merge would save about 180 upward references but no `Lazy<>`, and it would need one namespace root for 48 services.

## How it was measured

- **Binding.** A Roslyn tool compiles the server from source (3,375 files, 0 errors) and binds every name the way the
  compiler does, with accessibility respected. It records one line per source member, referenced type and kind of use
  (type, call, member access, constant, enum member, `nameof`, inferred generic argument): 323,522 records. Units and
  domains follow `UnitClassifier` and `DomainMap.json`, and edges out of `Composition` are left out.
- **Check against the guard.** Leaving out what the compiler inlines (constants, enum members, `nameof`), the tool
  finds all 773 unit edges of `CrossDomainEdges.txt` and `EdgesIntoCoreAndPlatform.txt` plus one more
  (`Feature.ServerTasks -> Platform/Root`), and exactly the 94 pairs of `DomainCycles.txt`. Counting constants too
  adds 26 edges but no pair. An independent review re-ran the tool and reproduced the pair steps and the upward counts.
- **Members.** A member is a source member: a method, property, field, constructor or enum member, with its lambdas
  and local functions. The IL baseline counts getters and setters apart and counts attribute default arguments, so its
  numbers differ. For example, 1,210 of the 1,383 IL members of `Domain/Character -> Domain/Combat` are the fields of
  `StatType`, whose attribute constructor takes a `CombatDamageType` by default: vocabulary. The generated
  `Feature/BeastDefinition` classes are included and inflate the vocabulary counts.
- **Classes.** Each reference is classed by what it names:
  - *vocabulary:* enums and enum members, constants, attributes, delegates, records and structs, classes that only hold
    data (persisted entities included), and GUI refresh events and payloads;
  - *definition:* an interface or base class that content implements, or a data-only definition record (`*Detail`,
    `*Builder`, `*Definition`);
  - *service call:* an `I*Service` member, a static helper, or `GameServices`;
  - *other:* a class with behaviour, a status effect type named as a key, a view model, a non-service interface.

  A member counts once per direction, under the hardest class it uses there (service call, other, definition,
  vocabulary, in that order), because removing its vocabulary does not remove the edge if it also calls a service.
- **Moves.** The moves are computed for the approved order. A vocabulary or data-only definition type moves to the
  lowest domain that uses it, unless that user is itself misplaced (it has a call up into the type's home, which has to
  be fixed anyway). The explicit families (the `AbilityDetail` family, `Log` and `FrameProfiler`, `CombatDamageType`)
  are moved whole first. A file moves whole: no file is split between targets.
- **Order.** For a fixed set of moves, a dynamic program over every order of the 15 game domains (Platform and
  Foundation fixed at the bottom) finds the order with the fewest member references pointing up. The moves depend on
  the order, so this is not a global optimum. With the approved moves, the minimum is 598 references (UI after
  Creatures) and the approved order costs 609, 1.8% more. UI stays low by choice: once `ExtraHotbarsService` moves up,
  the minimum (517) keeps UI low and the approved order costs 528. Above Combat the order is weakly held; several orders
  are within 2%.
- **Reproducing.** The tool (a C# extractor and Python analysis scripts) is in `tools/Refactor/DomainOrder` (P5-1b; its
  README names the commands), so the order can be re-measured after each split and in P5-15. Run on `369c318be` it
  reproduces every figure here except the record count: the review taught it the types a `var` infers (type arguments
  and array element types), which adds 356 records (323,878) and changes no pair or upward count. The other fixes made
  to the tool in that review (references into a wiring area count, `Native/` is never a move destination, a move conflict
  is checked where the target ends up) change none of the figures here. On `bd12924d9`, P5-1b's base, only the hook
  figures differ: P5-16a split the native attack code into more members, so the hook code holds 81 upward member
  references instead of 63 (P5-16 and P5-16b: 91 instead of 73). The guard measures are in `SWLOR.Architecture.Tests`
  with the other debt measures (section 5, "Record the order").

## 1. What the two-way pairs are made of

Over the 188 directions of the 94 pairs (Appendix A has every pair and direction):

| Class | Members | Share | Without BeastDefinition |
|---|---:|---:|---:|
| Vocabulary | 13,724 | 61% | 5,024 |
| Service call | 7,690 | 34% | 7,690 |
| Other | 628 | 3% | 628 |
| Definition | 333 | 1% | 333 |
| Total | 22,375 | | 13,675 |

Only 9 directions consist of vocabulary alone, so vocabulary moves can break at most 9 pairs.

`Character <-> Combat` has 1,223 members: 629 service calls, 542 vocabulary, 36 other, 16 definition. Character calls
Combat in 146 members and Combat calls Character in 483, so the pair stays after any vocabulary move. The phase plan
expected most of this pair to be vocabulary; by members it is 44%.

The 10 largest referenced types per class (members in the 94 pairs that name the type):

- **Vocabulary:** `CombatDamageType` 9,175, `ResistanceType` 9,041, `SkillType` 1,732, `Entity.Player` 713,
  `Core.ScriptName` 670, `Core.NWNEventHandler` 645, `StatType` 636, `PerkType` 615, `LogGroup` 243,
  `Entity.EntityBase` 231.
- **Definition:** `AbilityDetail` 311, `IStatusEffect` 112, `PerkDetail` 39, `RecipeDetail` 39, `QuestDetail` 18,
  `BlueprintDetail` 15, `BeastDetail` 12, `StatusEffectBase` 11, `DroidItemPropertyDetails` 9, `AbilityTargetingDetail` 8.
- **Service call:** `GameServices` 2,176, `IDBService` 847, `IStatService` 559, `IStatusEffectService` 412, `Log` 250,
  `ColorToken` 188, `IGuiService` 172, `IItemService` 159, `IAbilityService` 143, `IPerkService` 126.
- **Other:** `DBQuery` 126, `BleedStatusEffect` 40, `ApplicationSettings` 37, `CreatureStatusEffect` 33,
  `MinigameEngineBase` 33, `StatAdjustmentSource` 32, `KnockdownStatusEffect` 30, `DazedStatusEffect` 29,
  `GuiComboEntry` 29, `ExposedStatusEffect` 23.

## 2. Logic that names content

Every `LogicToContent.txt` entry, classed the same way (Appendix D lists each entry):

| Group | Entries | Members | Vocabulary | Definition | Service call | Other |
|---|---:|---:|---:|---:|---:|---:|
| `Domain/` to another domain's content | 46 edges | 245 | 41 | 0 | 63 | 141 |
| `Native/`, `NativeSinks/` to content | 8 edges | 14 | 1 | 0 | 12 | 1 |
| `Domain/` to its own domain's content | 17 files | 160 | 65 | 4 | 5 | 86 |

What they are, and the fix:

- **Status effect types used as keys** (186 member references, almost all from Combat, for example
  `HasStatusEffect<BleedStatusEffect>`; 7 also read a field such as `Stacks`). P5-5 gives logic a name for each effect it
  must recognise that Character owns: a key, or a capability interface like the existing `IPreDamageStatusEffect`.
  P5-7 and P5-2 then switch the callers.
- **Logic that lives in content** (67 references from `Domain/`, 14 from `Native/`): `AbilityTargeting`,
  `AbilityEffectScaling`, `TemporaryHitPointEffects`, `UsePerkFeat`, `PistolAnimationRemap`,
  `TwinPistolEquipmentValidation`, the TintMap model renderers, `SaberRecalibration`. They move into their domain's
  logic: `UsePerkFeat` in P5-16, the ability helpers in P5-3, the rest in P5-2.
- **GUI refresh events and payloads** (37): vocabulary; they move into `Domain.UI` (P5-1f).
- **Same-domain data and catalogs:** `StoredObjectData` and its `Node` and `Field` (used by Foundation's saved-character
  code), the TintMap catalogs and `TintMapLayerType` (used by Character's appearance render state),
  `FieldEngineerPulseEmitter` (Abilities), the extra hotbar view model and events (UI). They move into their domain's
  `Domain/` folder (P5-1f; the TintMap ones with P5-11's `TintMapService` move).

## 3. Vocabulary and definition moves

**Rule:** a type that is only vocabulary or a data-only definition stays with the domain that owns it unless a lower
domain uses it; then it moves to the lowest domain that uses it. Persisted entities count as Foundation (decision 2),
so everything an entity stores ends up in Foundation. A file moves whole.

**What Character may own.** Its own services and their vocabulary; vocabulary its logic reads, wherever it was declared;
and the `AbilityDetail` family, because Character logic reads ability definitions. Character never owns another domain's
behaviour, nor a member that only a higher domain reads: such a member moves off to that domain (as the AI settings move
off `AbilityDetail`).

The named types:

| Type | Today | Owner | Why |
|---|---|---|---|
| `CombatDamageType`, `ResistanceType` (with `CombatDamageAttribute`, `CombatDamageCategoryType`, `CombatDamageTypeExtensions`, `CombatDamageDetails`) | Combat | Foundation | stored by `Player`, `Beast` and `IncubationJob`; used by 11 domains |
| `StatType` | Character | stays | every domain that uses it is above Character |
| `PerkType` | Character | Foundation | stored by `Player`; used by 15 domains |
| `SkillType` | Character | Foundation | stored by `Player`; named by `Enumeration.GuildType` |
| `AbilityType` | `SWLOR.NWN.API` | stays | the NWScript ability score enum, outside the server; `AbilityDetail.CombatImpactDamageAbility` names it, which is a downward reference |
| The `AbilityDetail` family (15 types, below) | Abilities | Character | read by Combat (207 members) and by Perk and StatusEffect (20) |
| `AITargetSelector`, `AIScoreCalculation` and the other `AIDelegates.cs` delegates | Creatures | stay | the AI settings move off `AbilityDetail` (P5-1d) |
| `IAbilityListDefinition`, `IPerkListDefinition` | Abilities, Character | stay | only their own service reads them |
| `StatusEffectBase`, `IStatusEffect` | Character | stay | their other readers (Combat, Abilities) are above Character |
| `Log`, `LogGroup`, `LogGroupAttribute`, `FrameProfiler`, `FrameMetric`, `FrameCounters`, `FrameCaptureSummary` | Foundation | Platform (`Core`, beside `Clock`) | `Core`, `Native` and `NativeSinks` log and profile |

**The `AbilityDetail` family:** `AbilityDetail` and the five delegates in its file (`AbilityActivationAction`,
`AbilityActivationDelayAction`, `AbilityCustomValidationAction`, `AbilityImpactAction`, `AbilityRecastDelayAction`),
`AbilityActivationType`, `AbilityTargetingDetail` with `AbilityTargetingSizeResolver`, `AbilityTargetingFlags`,
`AbilityTargetingShapeType`, `IAbilityActivationRequirement`, `MeleeTechniqueDetail`, `MeleeTechniqueLine` and
`MeleeTechniqueStage`. `RecastGroup` and the damage enums it names go to Foundation. The requirement classes that
implement `IAbilityActivationRequirement` stay in Abilities. Content builds them with `new`, so this is an inversion with
no construction cycle.

**The AI settings move off `AbilityDetail`.** `AITargetSelector`, `AIScore` and `AIHitPointCostPercent` are written only
by `AbilityBuilder.HasAITarget`, `HasAIScore` and `HasAIHitPointCostPercent` (called from 17 content files), and read
only by `Creatures.AIScore` and `DefaultAIProfileDefinition`. The three builder methods become extension methods
declared in Creatures that write a Creatures-owned table keyed by feat, and `AIScore` reads that table. The call text in
content stays the same; the 17 files get a `using` of `Domain.Creatures`. This keeps `AIContext` out of Character; an
interface for it would have needed `Profile`, `State` and about ten more members.

**In all,** 99 types in 75 files move: 66 to Foundation, 20 to Character, 8 to `Core`, 3 to Combat, and one each to
Crafting and World. 19 of the files hold more than one type, and no moved name collides with a name in its target
namespace. Appendix B lists each type with the unit edges and pairs it removes when moved alone (21 unit edges and 2 pairs
in all, because the same pairs also hold service calls). Measured together, the pairs go from 94 to 88 (the rule's
moves) and 87 (the families, with the AI settings moved off).

**What cannot move whole:** 114 vocabulary references to 53 holders that are not vocabulary types: constants inside
behaviour classes, nested types, and content constants. The largest are the capstone quest IDs that perk definitions
read from quest definitions (46), the TintMap layer type and colours (19), the `CombatRecord*` scope types that Character
uses (16), and `GuiWindowType` (6). Extracting them takes the pairs from 87 to 86 (P5-1f). The entity defaults
`CombatRules.CreateDefaultDefenseValues` and `ResistanceRules.CreateDefaultResistanceValues` move with the enums.

## 4. The order

### What counts

- **Content is a band above all logic** (decision 1).
- **`Entity` and `Enumeration` count as Foundation** (decision 2): persisted data sits at the bottom with the vocabulary
  it stores.
- **`NativeSinks` counts as wiring,** like `Composition`, under the sink guard (decision 3). The hook logic in `Native/`
  is attack resolution and moves into Combat services.

### Tiers

| Tier | Domain | Depends on (members of its logic and content, after the moves) |
|---:|---|---|
| 0 | Platform | - |
| 1 | Foundation | Platform 42 |
| 2 | Character | Foundation 123, Platform 41 |
| 3 | UI | Character 64, Platform 48, Foundation 21 |
| 3 | Gameplay | Character 7, Platform 4, Foundation 3 |
| 4 | Items | Character 32, Foundation 29, Platform 11, UI 6 |
| 5 | Administration | Foundation 29, Character 12, Platform 9, UI 6, Items 5 |
| 6 | Combat | Character 659, Foundation 149, Platform 112, Items 22 |
| 7 | Abilities | Character 698, Combat 129, Foundation 49 |
| 8 | Creatures | Character 48, Combat 29, Platform 18, Foundation 16, Abilities 13 |
| 9 | Crafting | Foundation 79, Character 65, Items 27, UI 17 |
| 10 | World | Foundation 124, Platform 56, Character 36, Combat 15 |
| 11 | Companions | Foundation 40, Character 33, Combat 26, Creatures 24, Items 16 |
| 11 | Space | Foundation 33, Character 18, Platform 17, World 14 |
| 12 | Quests | Foundation 78, Character 19, Platform 17, Items 16 |
| 12 | Social | Character 54, Foundation 29, World 20, Platform 17, Administration 13 |
| 13 | Economy | Foundation 30, Platform 12, Quests 11, UI 10 |

A tier is the longest chain of dependencies below a domain. Domains in the same tier, and Social and Economy, do not
depend on each other, so the order in the summary is one valid reading. `Gameplay` is the `ServerTask` service plus the
scripts directly in `Feature`: its logic is low and its scripts are content.

### Pairs at each step

| Step | Pairs |
|---|---:|
| Today | 94 |
| The rule's vocabulary moves | 88 |
| The families moved, the AI settings off `AbilityDetail` | 87 |
| The remaining vocabulary extracted | 86 |
| `Entity` and `Enumeration` count as Foundation | 84 |
| Content counted as a band above logic | 59 |
| `NativeSinks` counted as wiring | 58 |
| `Native` hook logic moved into Combat (P5-16b) | 54 |
| Five misplaced classes moved (the first four rows of the misplaced code table) | 44 |
| Every remaining upward edge fixed (Appendix C) | 0 |

### Remaining upward edges, by item

After the moves, in the approved order (Appendix C has each direction with its main sources and targets). P5-1b
records them as a shrinking baseline, `UpwardEdges.txt`, so every item shrinks it and P5-15 is left with the cycles inside
domains and the final guards.

The baseline counts the tree as it is, before the moves, and counts every reference, vocabulary included, so the type
moves of P5-1e and P5-1f shrink it too. On `bd12924d9` it held 1,319 member references in 76 directions (931 of them
service, other or definition references, in 65 directions); 124 come from the hook code in `Native/`, which is Platform
until it moves. P5-1d (the AI settings off `AbilityDetail`) and P5-1c's `RecipeType` pilot, merged before P5-1b, took it
to 1,311 in 75 directions. The table below counts only what the moves leave. When P5-16b moves hook code into Combat, the references into it
from domains below Combat (the `Character -> Native` and `Foundation -> Native` rows of Appendix C) start to point up, so
P5-16b fixes them in the same pull request.

| Item | Directions | Members |
|---|---:|---:|
| P5-1g shared low interfaces | 9 | 106 |
| P5-1h misplaced code | 10 | 53 |
| P5-3 Ability | 6 | 81 |
| P5-4 Stat | 4 | 56 |
| P5-5 StatusEffect | 3 | 25 |
| P5-7 Combat | 7 | 45 |
| P5-2 content left over | 8 | 57 |
| P5-6 Space | 2 | 9 |
| P5-8 Creatures | 5 | 19 |
| P5-9 Items and World | 6 | 20 |
| P5-10 Character and Companions | 7 | 38 |
| P5-11 Social, UI and Foundation | 11 | 61 |
| P5-15a services no split covers | 15 | 61 |
| P5-16 and P5-16b hook logic | 12 | 73 |

A direction can need more than one item, so the directions add up to more than 73.

### Character, Combat and Abilities

Hard references (service, other and definition) after the moves:

| From | To | Members |
|---|---|---:|
| Combat | Character | 659 |
| Character | Combat | 98 |
| Abilities | Combat | 129 |
| Combat | Abilities | 65 |
| Abilities | Character | 698 |
| Character | Abilities | 32 |

Upward references for the whole order with each alternative: approved (Character, Combat, Abilities) 609; Abilities
below Combat 673; Character above Combat 1,244; Abilities below Character 1,340. Merges: Character and Combat leave 44
pairs and 528 upward references; all three leave 36 pairs and 431 references. All three together are 39% of the
`Domain/` lines and 36% of the logic lines (`Domain/` and Platform, generated content excluded), with 48 services.

- **Character and Combat are two domains.** Combat's use of Character is mostly `IStatService` (346 members, 270 of them
  `GetStatAdjustment`) and `IStatusEffectService` (190). The 98 back references are concentrated in `StatService` and
  `StatusEffectService`, which pull inputs from Combat services: temporary stat modifiers, power glove and twin pistol
  equipment, damage modifiers, resistances, the combat recorder.
- **Abilities sits above Combat.** `AbilityService` calls 20 Combat services to apply impacts. Combat calls back 19
  members of `IAbilityService` (impact queries such as `IsAreaImpact` and `GetActiveAbilityImpactSummary`, the catalog,
  activation state) and reads `AbilityDetail` (207 members). Moving the Combat ability services (`CombatAbilityActivation`,
  `Impact`, `Bonus`, `Hit`, `MeleeTechnique`, `TwinPistolAbility`) into Abilities was also measured: it only takes Combat
  to Abilities from 253 to 136 members, because `AbilityDetail` is read across Combat. Moving the definition record down
  works better.

### Misplaced code

| Class | Today | Moves to | Upward members it removes | Item |
|---|---|---|---:|---|
| `CommunicationService` (chat) | Foundation | Social | 31 | P5-11 |
| `ExtraHotbarsService` | UI | Abilities (or above) | 27 | P5-11 |
| `EquipmentPredicates` | Character | Items | 20 | P5-1h |
| `AbilityAnimationBinding`, `AnimationPreviewCatalog` | Foundation | Abilities | 12 | P5-1h |
| Migration steps in `MigrationService` that call Stat, Perk and OverflowStorage | Foundation | `MigrationDefinition` content | 11 | P5-1h |
| `SavedCharacter`, `SavedCharacterDataStore`, `SavedEquipmentStats` | Foundation | invert, or move up with the character file code | 10 | P5-1h |
| `GuiRectangle`, `DMFavoriteBlueprint`, `SpawnClearanceGrid` | UI, Administration, Creatures | down to their lowest user | 6 | P5-1h, P5-8 |

### Inversions and how the lower domain gets the implementation

An inversion means the lower domain owns an interface or event and the higher domain provides it. Injecting the
implementation into the lower service's constructor recreates the construction cycle that a `Lazy<>` hides today, and
target 4 forbids deferred resolution (`Lazy<>`, `Func<>`, `IServiceProvider`, a resolving factory). **Supply rule**, in
order of preference:

1. **Push instead of pull.** The higher service writes what the lower one needs into a store the lower domain owns. For
   example, Combat's temporary stat modifiers and equipment bonuses become entries in a Character-owned stat source
   store that `StatService` reads. No interface points back up, and the container builds each service in order.
2. **Split so the provider does not need the owner.** The part that answers the lower domain depends only on things
   below that domain. Plain constructor injection (for example `IEnumerable<IStatContributor>`) then builds without a
   cycle.
3. **Notifications register at boot.** This is only for events: the lower domain announces and never asks for a value.
   Subscribers register with the publisher in a boot handler, and the publisher keeps a list. It is the only late binding
   allowed, and P5-15's guard checks that every registration happens at boot.

A query that needs a value from above uses rule 1 or 2, never rule 3. A down `Lazy<>` exists because the other half of
its cycle points up. It becomes a plain parameter in the item that removes the up half. The 30 `Lazy<>` inside a domain
(Combat 11, Character 6, Abilities 3, Social 3, others 1 or 2) are broken by that domain's part map or by P5-15. A merge
of domains removes none: the cycles are between services, not domains.

The main inversions (every direction is in Appendix C):

| Edge | Mechanism | Members | Up `Lazy<>` removed | Item |
|---|---|---:|---:|---|
| Character, Items, Platform -> Combat (CombatRecorder) | recording interface owned by Foundation, implemented by the recorder (rule 3 for notices, rule 1 for the record data); `ServerTeardown` joins Core's `IServerTeardown` | 29 | 0 | P5-1g |
| Character, Items, Creatures, Combat -> Companions | companion lookup (is this a beast or droid, its owner) owned by Character, implemented by BeastMastery and Droid (rule 2) | 57 | 9 (`Lazy<IDroidService>`) | P5-1g |
| Character, Foundation -> UI | window refresh publisher owned by Foundation, implemented by Gui (rule 3) | 24 | 0 | P5-1g |
| Character -> Combat, Abilities, Items (StatService) | stat sources: Combat, Mimicry and Item push contributions into a Character-owned store (rule 1) | 47 | 5 | P5-4 |
| Character -> Combat (StatusEffectService) | effect lifecycle events owned by Character; Resistance, MeleeTechnique, AbilityImpact and Stealth subscribe (rule 3) | 18 | 2 | P5-5 |
| Combat -> Character content (status effect types) | effect keys or capability interfaces owned by Character | 186 references | 0 | P5-5, then P5-7 |
| Combat -> Abilities | impact and activation state pushed into a Combat-owned store (rule 1), the catalog read through Character (`AbilityDetail`) | 65 | 3 | P5-3 |
| Character -> Abilities (Perk, StatusEffect, Stat) | quick cast and ability state through interfaces Character owns (rule 2); Mimicry stat bonus pushed (rule 1) | 32 | 2 | P5-3, P5-10 |
| Abilities -> Social, Quests, Space, World | party, capstone, space and music through interfaces Abilities owns | 19 | 1 | P5-3 |
| Combat -> Social, Creatures, World, Quests, Space | party members, AI and companion notices, area lookups, capstone progress through interfaces Combat owns | 52 | 1 | P5-7, P5-8, P5-15a |
| Others (World, Administration, Items, Quests, Social) | interfaces owned by the lower domain | | 5 | P5-6, P5-9, P5-10, P5-11, P5-15a |

The 28 up `Lazy<>` by item: P5-1g 9, P5-3 6, P5-4 5, P5-5 2, P5-6 2, P5-7 1, P5-9 1, P5-10 1, P5-11 1. Inversions keep
the call shape the services use today (an interface call or a store read), so they need not add allocations. An event
that allocates per raise is not acceptable on the attack path.

### Who adds each implementation

The item that introduces an interface also adds its implementation in the implementing class, in the same pull request.
If the implementer's own item is running at the time, it waits. The implementer's later split moves the implementation
with the rest.

| Interface (owner) | Introduced by | Implemented in (whose files) |
|---|---|---|
| Recording (Foundation) | P5-1g | `CombatRecorderService` (P5-7's, later) |
| Companion lookup (Character) | P5-1g | `BeastMasteryService`, `DroidService` (P5-10's; P5-1g runs before P5-8 and P5-10, so P5-8 never edits them) |
| Window refresh publisher (Foundation) | P5-1g | `GuiService` (P5-11's) |
| Stat source store (Character) | P5-4 | Combat services (P5-7's, later), `MimicryService` (P5-10's, later), `ItemService` (P5-9's, which runs last by owner decision 5) |
| Effect lifecycle events (Character) | P5-5 | `ResistanceService`, `MeleeTechniqueService`, `CombatAbilityImpactService`, `StealthService` (Combat, P5-7's, later) |
| Ability state store and catalog (Combat, Character) | P5-3 | the Ability parts (P5-3's own) |
| Combat's queries up (party, AI, area, capstone, space) | P5-7 | `PartyService`, `AIService` and `CompanionControlService` (P5-8's; P5-7 waits if P5-8 is running), `AreaService`, `CapstoneProofService`, `SpaceService` (P5-6's, which runs last) |

## 5. Moves and master syncs

- **The type move tool, extended.** `Move-SupportNamespaces.ps1` renames whole namespaces and folders. A vocabulary move
  takes one file out of a namespace that keeps its other types, which is what `tools/Refactor/SWLOR.TypeMoves`
  (`Move-ServiceTypes.ps1`, P2-Z3) does. It moves the file, rewrites qualified names, adds a `using` where a simple name
  needs it, proves each name binds as before, rewrites names in other text, and keeps a stand-in of each moved type in
  the old namespace so master code merged later is moved the same way.
- **What it cannot do yet.**
  - It has one `from` namespace, assumed in about 26 places.
  - `TypeMoveData.cs:75` requires the target below the parent of `from`. That refuses `Feature.*` to `Domain.*`
    (`HealAmount`, `TemporaryHitPointEffectKey`, the 37 GUI events, `StoredObjectData`, the TintMap types) and
    `Domain.Foundation` to `Core` (`Log`, `FrameProfiler`). `TypeMove.RelativeTarget` assumes the same shared parent
    when it respells a qualifier.

  P5-1c gives each entry its own `from`, allows targets across roots, and adds tests. It is about the size of the
  P2-Z3 tool item, one Opus item.
- **Order of the mechanical scripts.** Master's code reaches these types through `Service.<Name>Service`.
  `Move-SupportNamespaces.ps1` first turns that into `Domain.<Domain>`. Only then can the new moves send
  `Domain.Character.PerkType` to `Domain.Foundation`. So the Phase 5 moves run after `Move-ServiceTypes.ps1` in
  `MechanicalScripts.txt`, which also puts them in the parity copy of master.
- **New `using` lines.**
  - 1,256 of the 1,447 server files that name a type going to Foundation have neither a `using` nor the namespace of
    `Domain.Foundation`.
  - For Character it is 289 of 756 files, and for `Core` 78 of 175.
  - Tests and tools add more.

  Where master edits a file's usings, the merge conflicts. The existing rule applies: take master's block and run the
  script again. P5-1e runs right after a daily sync, as one pull request, in a short announced quiet window.
- **Master edits these files often.** In the last three months master changed `RecipeType.cs` 28 times, `KeyItemType.cs`
  22, `AbilityDetail.cs` 18, `PerkType.cs` 10 and `SkillType.cs` 7. Git follows the rename on merge, as it does for the
  P2-Z2 moves. **Pilot:** P5-1c moves `RecipeType.cs` alone and takes it through one daily sync. The check is that
  master's edits land in the moved file, new master code that names `RecipeType` gets its `using`, and the parity copy
  builds. P5-1e starts only after the pilot passes.
- **No topic named like a domain.** The namespace guard fails a leaf segment under two parents, so the moved types go
  directly into `Domain.Foundation`, `Domain.Character` and `Core`.
- **Prefer type moves to member moves.** Moving a constant or a nested type out of a class changes the call text, which
  master's code then needs rewritten (`Apply-FacadeRemoval.ps1` can respell moved constants, as upkeep until the cutover).
  P5-1f keeps such moves to the 114 references listed.
- **Record the order.** P5-1b recorded the order and the band rule in `DomainMap.json`:
  - `order` (bottom to top) and `contentBand: aboveAllLogic`;
  - `platformAreaDomains` puts `Entity` and `Enumeration` in Foundation for every guard, so `CrossDomainEdges.txt` and
    `EdgesIntoCoreAndPlatform.txt` now list their edges under Foundation;
  - `wiringPlatformAreas` adds `NativeSinks`, so its 7 edges left `CrossDomainEdges.txt`;
  - `contentRuleExemptAreas` lets only `Composition` name content, so the sinks stay in `LogicToContent.txt`.

  The order measures bind the server's source the way the tool does (`SourceReferenceScanner`), so their counts do not
  depend on the build configuration, and they match the tool's `guard_figures.py`. On `bd12924d9`, P5-1b's base:
  - `DomainCycles.txt` counts pairs by band: 69, 63 between logic and 6 between content (the recount of section 4). P5-1d
    removed `Abilities <-> Creatures`, so the merged baseline holds 68.
  - `UpwardEdges.txt` has one line per file, node of the file's code and higher node (`<file>: Character -> Combat`), with
    its referencing members: 1,319, then 1,311 after P5-1c and P5-1d (see "Remaining upward edges, by item").
  - `DeferredResolution.txt` gives each `Lazy<>` its direction (`<file> -> Combat (up)`): 28 up, 53 down and 30 inside one
    domain. The direction went into the existing baseline rather than a second one, so deferred resolution keeps one source.
  - `SinkWork.txt` counts the lines of the sinks' members that do more than forward: 2,096 lines in 15 of the 17 sinks,
    879 of them in `OnAIActionAttackObjectSink` (decision 3's 2,373 lines count whole files at `369c318be`).

  P5-15 turns them into failing guards; P5-16b does it for `SinkWork.txt`.

## 6. Items

P5-1b onward replace the single P5-1b of the earlier draft. At most three agents run at once, and two items never touch
the same files.

| Item | Work | Depends on | Model |
|---|---|---|---|
| P5-1b | Guard and measures: order, band, Foundation areas, `NativeSinks` wiring with the sink-work baseline, pairs by band, `UpwardEdges.txt`, `Lazy<>` by direction; commit the measuring tool under `tools/Refactor/DomainOrder` | P5-1 | Opus |
| P5-1c | Type move tool: a `from` per entry, targets across roots, tests; pilot `RecipeType.cs` through one daily sync | P5-1 | Opus |
| P5-1d | AI settings off `AbilityDetail` into Creatures extension methods and a Creatures-owned table; AI scoring unchanged | P5-1 | Sonnet build, Opus review |
| P5-1e | The 99 type moves of Appendix B, including the `AbilityDetail`, `Log` and `CombatDamageType` families and the entity defaults | P5-1b, P5-1c, P5-1d | Sonnet (scripted), Opus review |
| P5-1f | Extractions (114 references, 53 holders) and content vocabulary into `Domain/` (GUI events, `StoredObjectData`, TintMap types, `FieldEngineerPulseEmitter`, `HealAmount`, capstone quest IDs) | P5-1c, P5-1e | Sonnet build, Opus review |
| P5-1g | Shared low interfaces with their implementations: recording, companion lookup, window refresh publisher | P5-1e | Opus |
| P5-1h | Misplaced code (table above, except the P5-11 rows) | P5-1b | Sonnet build, Opus review |
| P5-15a | Inversions in services that no split covers (Appendix C rows marked P5-15a) | P5-1b | Sonnet build, Opus review |
| P5-16b | Hook logic into Combat services | P5-7, P5-16a | Opus |

Sequencing:

- P5-1b, P5-1c and P5-1d run first, in parallel.
- P5-1e runs alone: it touches about 1,600 server files' usings.
- P5-3 starts after P5-1e, because the Ability split needs `AbilityDetail` in Character.
- P5-1g runs before P5-4, because P5-4 and P5-5 need the recording interface before P5-7.
- P5-1h runs before P5-4 (`EquipmentPredicates` feeds `StatService`).
- P5-1f and P5-15a run alongside the chain on files the chain is not touching.
- P5-15 keeps the `Lazy<>` cycles inside domains, the final guards (no two-way pair, the order, no deferred resolution,
  sinks do no work) and whatever the shrinking baselines still hold.

## 7. Effects on the splits

**P5-3 Ability.**
- Combat (below) reads ability state through 19 `IAbilityService` members, and Character reads quick cast and ability
  state.
- Each part that a lower domain reads gets its supply from the rule:
  - the definition catalog returns the Character-owned `AbilityDetail`, and its interface sits in Character;
  - impact queries and activation and queued attack state are pushed into a Combat-owned store.
- The parts that call Combat (applying combat and telegraphed impacts, impact visuals, healing adjustments) stay in
  Abilities.
- It inverts `AbilityService`'s calls to Social, Quests, Space and World (19) and removes 6 up `Lazy<>`.

**P5-4 Stat.**
- The stat read path that Combat uses (`GetStatAdjustment`, 270 members) stays a Character interface and stays
  allocation free.
- `StatService` pulls inputs from Combat, Mimicry and Items today. The part map needs a stat sources part that reads a
  Character-owned store, which the contributing services fill (rule 1).
- Its recorder calls use P5-1g's recording interface. It removes the 5 `Lazy<ICombat...>` in `StatService.cs`.

**P5-5 StatusEffect.**
- `StatusEffectService` calls Combat (21), UI (5), Abilities and Quests (6); `StatusEffectBase` calls the recorder (6).
- The part map needs lifecycle events owned by Character.
- It must give logic a Character-owned name for the effects that Combat recognises today by their content type (186
  references), so P5-7 can drop every `Feature` name.
- It removes 2 up `Lazy<>`.

**P5-7 Combat.**
- Its calls up go to Social (18), Creatures (17, mostly `EnmityService` to AI and companion control), World (8), Quests
  (6) and Space (3); each becomes an interface Combat owns.
- Its calls to Abilities and Companions are already gone by then (P5-3, P5-1g).
- The recorder part implements P5-1g's recording interface.
- The part map gives P5-16b a home for the hook logic; P5-16a's static helpers are the starting point.

## Side finding (fixed in P5-1b)

P5-0's `SourceSnapshot` builds its compilations with `MetadataImportOptions.All` and binds with
`ignoreAccessibility: true`. With the server's `global using System;`, the name `Currency` then binds to both
`GameServices.Currency` and the internal `System.Currency` of `System.Private.CoreLib`, so `GetSymbolInfo` returns no
symbol. `LookupScanner` therefore misses the `Currency` lookups (8 server files, for example the three in
`Feature/PlaceableScripts.cs`), so `StaticLookups.txt` undercounts them. Binding with accessibility respected fixes it.
`Currency` is the only `GameServices` name that matches an internal type of `System.Private.CoreLib`; other assemblies
were not checked.

P5-1b binds every measure and the content injection rules through one call (`SourceSnapshot.Bind`) with accessibility
respected. `StaticLookups.txt` gained the 32 `Currency` uses it missed: 26 in those 8 server files and 6 in two engine
test files (`LightsaberWorkbenchEngineTests`, `ServiceContainerEngineTests`). The 10 files were already listed for other
lookups, so only their counts grew. No other baseline changed.

## Appendix A: Every two-way pair today

Members by the hardest class they use in that direction (section 1). Unit edges as in `DomainCycles.txt`.

| Pair | Direction | Unit edges | Members | Vocabulary | Definition | Service call | Other |
|---|---|---:|---:|---:|---:|---:|---:|
| Abilities <-> Character | Abilities -> Character | 4 | 1,109 | 484 | 29 | 245 | 351 |
| Abilities <-> Character | Character -> Abilities | 6 | 91 | 22 | 17 | 52 | 0 |
| Abilities <-> Combat | Abilities -> Combat | 2 | 555 | 313 | 0 | 229 | 13 |
| Abilities <-> Combat | Combat -> Abilities | 2 | 332 | 60 | 187 | 84 | 1 |
| Abilities <-> Companions | Abilities -> Companions | 1 | 11 | 0 | 0 | 11 | 0 |
| Abilities <-> Companions | Companions -> Abilities | 2 | 3 | 2 | 0 | 1 | 0 |
| Abilities <-> Creatures | Abilities -> Creatures | 2 | 35 | 4 | 0 | 28 | 3 |
| Abilities <-> Creatures | Creatures -> Abilities | 2 | 30 | 1 | 15 | 14 | 0 |
| Abilities <-> Foundation | Abilities -> Foundation | 2 | 92 | 17 | 0 | 75 | 0 |
| Abilities <-> Foundation | Foundation -> Abilities | 2 | 15 | 0 | 10 | 5 | 0 |
| Abilities <-> Gameplay | Abilities -> Gameplay | 5 | 9 | 0 | 0 | 7 | 2 |
| Abilities <-> Gameplay | Gameplay -> Abilities | 6 | 36 | 1 | 8 | 26 | 1 |
| Abilities <-> Items | Abilities -> Items | 2 | 3 | 0 | 0 | 3 | 0 |
| Abilities <-> Items | Items -> Abilities | 3 | 13 | 10 | 0 | 3 | 0 |
| Abilities <-> Platform | Abilities -> Platform | 8 | 523 | 81 | 0 | 442 | 0 |
| Abilities <-> Platform | Platform -> Abilities | 4 | 8 | 2 | 2 | 4 | 0 |
| Abilities <-> Quests | Abilities -> Quests | 2 | 4 | 0 | 0 | 4 | 0 |
| Abilities <-> Quests | Quests -> Abilities | 2 | 16 | 0 | 12 | 4 | 0 |
| Abilities <-> Social | Abilities -> Social | 2 | 21 | 0 | 0 | 21 | 0 |
| Abilities <-> Social | Social -> Abilities | 3 | 10 | 1 | 0 | 9 | 0 |
| Abilities <-> Space | Abilities -> Space | 1 | 3 | 0 | 0 | 3 | 0 |
| Abilities <-> Space | Space -> Abilities | 1 | 8 | 3 | 0 | 5 | 0 |
| Abilities <-> UI | Abilities -> UI | 2 | 12 | 0 | 0 | 12 | 0 |
| Abilities <-> UI | UI -> Abilities | 3 | 52 | 6 | 5 | 41 | 0 |
| Abilities <-> World | Abilities -> World | 2 | 5 | 0 | 0 | 5 | 0 |
| Abilities <-> World | World -> Abilities | 2 | 5 | 0 | 0 | 5 | 0 |
| Administration <-> Character | Administration -> Character | 1 | 13 | 1 | 0 | 12 | 0 |
| Administration <-> Character | Character -> Administration | 1 | 3 | 0 | 0 | 3 | 0 |
| Administration <-> Combat | Administration -> Combat | 1 | 1 | 1 | 0 | 0 | 0 |
| Administration <-> Combat | Combat -> Administration | 1 | 9 | 0 | 0 | 9 | 0 |
| Administration <-> Creatures | Administration -> Creatures | 1 | 6 | 0 | 0 | 6 | 0 |
| Administration <-> Creatures | Creatures -> Administration | 1 | 3 | 0 | 0 | 3 | 0 |
| Administration <-> Foundation | Administration -> Foundation | 1 | 38 | 0 | 0 | 36 | 2 |
| Administration <-> Foundation | Foundation -> Administration | 1 | 7 | 0 | 0 | 7 | 0 |
| Administration <-> Gameplay | Administration -> Gameplay | 1 | 1 | 0 | 0 | 0 | 1 |
| Administration <-> Gameplay | Gameplay -> Administration | 6 | 13 | 0 | 0 | 13 | 0 |
| Administration <-> Items | Administration -> Items | 1 | 5 | 0 | 0 | 5 | 0 |
| Administration <-> Items | Items -> Administration | 2 | 4 | 0 | 0 | 4 | 0 |
| Administration <-> Platform | Administration -> Platform | 2 | 23 | 23 | 0 | 0 | 0 |
| Administration <-> Platform | Platform -> Administration | 1 | 4 | 2 | 0 | 0 | 2 |
| Administration <-> UI | Administration -> UI | 1 | 12 | 6 | 1 | 5 | 0 |
| Administration <-> UI | UI -> Administration | 2 | 119 | 23 | 0 | 86 | 10 |
| Administration <-> World | Administration -> World | 1 | 7 | 0 | 0 | 7 | 0 |
| Administration <-> World | World -> Administration | 1 | 11 | 0 | 0 | 11 | 0 |
| Character <-> Combat | Character -> Combat | 4 | 348 | 192 | 0 | 146 | 10 |
| Character <-> Combat | Combat -> Character | 3 | 875 | 350 | 16 | 483 | 26 |
| Character <-> Companions | Character -> Companions | 4 | 86 | 50 | 0 | 36 | 0 |
| Character <-> Companions | Companions -> Character | 3 | 47 | 12 | 0 | 35 | 0 |
| Character <-> Crafting | Character -> Crafting | 2 | 57 | 39 | 0 | 18 | 0 |
| Character <-> Crafting | Crafting -> Character | 3 | 409 | 338 | 5 | 57 | 9 |
| Character <-> Creatures | Character -> Creatures | 1 | 4 | 0 | 0 | 4 | 0 |
| Character <-> Creatures | Creatures -> Character | 2 | 38 | 6 | 0 | 32 | 0 |
| Character <-> Economy | Character -> Economy | 1 | 1 | 1 | 0 | 0 | 0 |
| Character <-> Economy | Economy -> Character | 1 | 15 | 8 | 0 | 7 | 0 |
| Character <-> Foundation | Character -> Foundation | 4 | 150 | 2 | 0 | 148 | 0 |
| Character <-> Foundation | Foundation -> Character | 5 | 86 | 31 | 1 | 52 | 2 |
| Character <-> Items | Character -> Items | 1 | 22 | 0 | 0 | 22 | 0 |
| Character <-> Items | Items -> Character | 4 | 50 | 8 | 0 | 42 | 0 |
| Character <-> Platform | Character -> Platform | 15 | 614 | 376 | 0 | 236 | 2 |
| Character <-> Platform | Platform -> Character | 7 | 50 | 25 | 0 | 25 | 0 |
| Character <-> Quests | Character -> Quests | 2 | 49 | 44 | 0 | 5 | 0 |
| Character <-> Quests | Quests -> Character | 3 | 221 | 166 | 0 | 54 | 1 |
| Character <-> Social | Character -> Social | 3 | 14 | 1 | 0 | 13 | 0 |
| Character <-> Social | Social -> Character | 5 | 102 | 34 | 0 | 63 | 5 |
| Character <-> UI | Character -> UI | 4 | 46 | 1 | 0 | 18 | 27 |
| Character <-> UI | UI -> Character | 3 | 270 | 74 | 7 | 182 | 7 |
| Combat <-> Companions | Combat -> Companions | 1 | 15 | 0 | 0 | 15 | 0 |
| Combat <-> Companions | Companions -> Combat | 2 | 8,748 | 8,718 | 0 | 30 | 0 |
| Combat <-> Creatures | Combat -> Creatures | 1 | 17 | 0 | 0 | 17 | 0 |
| Combat <-> Creatures | Creatures -> Combat | 1 | 29 | 0 | 0 | 29 | 0 |
| Combat <-> Foundation | Combat -> Foundation | 2 | 150 | 7 | 0 | 137 | 6 |
| Combat <-> Foundation | Foundation -> Combat | 4 | 57 | 18 | 0 | 39 | 0 |
| Combat <-> Gameplay | Combat -> Gameplay | 3 | 9 | 0 | 0 | 8 | 1 |
| Combat <-> Gameplay | Gameplay -> Combat | 18 | 88 | 3 | 0 | 83 | 2 |
| Combat <-> Items | Combat -> Items | 1 | 24 | 2 | 0 | 21 | 1 |
| Combat <-> Items | Items -> Combat | 2 | 13 | 1 | 0 | 12 | 0 |
| Combat <-> Platform | Combat -> Platform | 6 | 233 | 107 | 0 | 124 | 2 |
| Combat <-> Platform | Platform -> Combat | 4 | 66 | 30 | 0 | 36 | 0 |
| Combat <-> Quests | Combat -> Quests | 2 | 9 | 3 | 0 | 6 | 0 |
| Combat <-> Quests | Quests -> Combat | 2 | 9 | 0 | 0 | 9 | 0 |
| Combat <-> Social | Combat -> Social | 1 | 18 | 0 | 0 | 18 | 0 |
| Combat <-> Social | Social -> Combat | 2 | 11 | 0 | 0 | 11 | 0 |
| Combat <-> Space | Combat -> Space | 1 | 3 | 0 | 0 | 3 | 0 |
| Combat <-> Space | Space -> Combat | 2 | 24 | 0 | 0 | 24 | 0 |
| Combat <-> UI | Combat -> UI | 2 | 3 | 0 | 0 | 3 | 0 |
| Combat <-> UI | UI -> Combat | 2 | 73 | 17 | 0 | 56 | 0 |
| Combat <-> World | Combat -> World | 1 | 8 | 0 | 0 | 8 | 0 |
| Combat <-> World | World -> Combat | 1 | 19 | 3 | 0 | 16 | 0 |
| Companions <-> Creatures | Companions -> Creatures | 1 | 24 | 0 | 0 | 24 | 0 |
| Companions <-> Creatures | Creatures -> Companions | 1 | 9 | 0 | 0 | 9 | 0 |
| Companions <-> Foundation | Companions -> Foundation | 1 | 44 | 6 | 0 | 38 | 0 |
| Companions <-> Foundation | Foundation -> Companions | 1 | 35 | 25 | 0 | 10 | 0 |
| Companions <-> Gameplay | Companions -> Gameplay | 1 | 1 | 0 | 0 | 1 | 0 |
| Companions <-> Gameplay | Gameplay -> Companions | 2 | 3 | 0 | 0 | 3 | 0 |
| Companions <-> Items | Companions -> Items | 1 | 23 | 7 | 0 | 16 | 0 |
| Companions <-> Items | Items -> Companions | 3 | 23 | 9 | 1 | 13 | 0 |
| Companions <-> Platform | Companions -> Platform | 4 | 100 | 79 | 0 | 21 | 0 |
| Companions <-> Platform | Platform -> Companions | 1 | 8 | 8 | 0 | 0 | 0 |
| Companions <-> Quests | Companions -> Quests | 1 | 3 | 0 | 0 | 0 | 3 |
| Companions <-> Quests | Quests -> Companions | 1 | 3 | 0 | 0 | 3 | 0 |
| Companions <-> UI | Companions -> UI | 2 | 11 | 2 | 0 | 9 | 0 |
| Companions <-> UI | UI -> Companions | 1 | 71 | 28 | 2 | 36 | 5 |
| Crafting <-> Foundation | Crafting -> Foundation | 1 | 85 | 0 | 0 | 85 | 0 |
| Crafting <-> Foundation | Foundation -> Crafting | 2 | 19 | 17 | 0 | 2 | 0 |
| Crafting <-> Gameplay | Crafting -> Gameplay | 1 | 1 | 0 | 0 | 0 | 1 |
| Crafting <-> Gameplay | Gameplay -> Crafting | 5 | 13 | 2 | 0 | 10 | 1 |
| Crafting <-> Items | Crafting -> Items | 3 | 30 | 2 | 0 | 13 | 15 |
| Crafting <-> Items | Items -> Crafting | 2 | 8 | 1 | 0 | 7 | 0 |
| Crafting <-> Platform | Crafting -> Platform | 5 | 177 | 98 | 0 | 79 | 0 |
| Crafting <-> Platform | Platform -> Crafting | 2 | 10 | 10 | 0 | 0 | 0 |
| Crafting <-> UI | Crafting -> UI | 2 | 32 | 15 | 0 | 17 | 0 |
| Crafting <-> UI | UI -> Crafting | 2 | 249 | 83 | 7 | 131 | 28 |
| Crafting <-> World | Crafting -> World | 1 | 7 | 4 | 0 | 3 | 0 |
| Crafting <-> World | World -> Crafting | 1 | 12 | 3 | 0 | 2 | 7 |
| Creatures <-> Foundation | Creatures -> Foundation | 2 | 31 | 2 | 0 | 25 | 4 |
| Creatures <-> Foundation | Foundation -> Creatures | 1 | 2 | 0 | 0 | 0 | 2 |
| Creatures <-> Gameplay | Creatures -> Gameplay | 1 | 3 | 0 | 0 | 3 | 0 |
| Creatures <-> Gameplay | Gameplay -> Creatures | 4 | 8 | 0 | 0 | 8 | 0 |
| Creatures <-> Platform | Creatures -> Platform | 6 | 68 | 45 | 0 | 23 | 0 |
| Creatures <-> Platform | Platform -> Creatures | 2 | 2 | 1 | 0 | 1 | 0 |
| Creatures <-> Social | Creatures -> Social | 1 | 3 | 0 | 0 | 3 | 0 |
| Creatures <-> Social | Social -> Creatures | 1 | 1 | 0 | 0 | 1 | 0 |
| Economy <-> Foundation | Economy -> Foundation | 1 | 30 | 0 | 0 | 28 | 2 |
| Economy <-> Foundation | Foundation -> Economy | 2 | 11 | 6 | 0 | 5 | 0 |
| Economy <-> Items | Economy -> Items | 1 | 9 | 0 | 0 | 9 | 0 |
| Economy <-> Items | Items -> Economy | 2 | 7 | 0 | 0 | 7 | 0 |
| Economy <-> Platform | Economy -> Platform | 4 | 59 | 50 | 0 | 9 | 0 |
| Economy <-> Platform | Platform -> Economy | 1 | 4 | 4 | 0 | 0 | 0 |
| Economy <-> Quests | Economy -> Quests | 1 | 11 | 0 | 7 | 4 | 0 |
| Economy <-> Quests | Quests -> Economy | 1 | 4 | 0 | 0 | 4 | 0 |
| Economy <-> UI | Economy -> UI | 2 | 11 | 1 | 0 | 7 | 3 |
| Economy <-> UI | UI -> Economy | 1 | 39 | 11 | 0 | 28 | 0 |
| Economy <-> World | Economy -> World | 1 | 6 | 1 | 0 | 5 | 0 |
| Economy <-> World | World -> Economy | 1 | 2 | 0 | 0 | 2 | 0 |
| Foundation <-> Gameplay | Foundation -> Gameplay | 5 | 11 | 0 | 0 | 11 | 0 |
| Foundation <-> Gameplay | Gameplay -> Foundation | 39 | 100 | 3 | 0 | 96 | 1 |
| Foundation <-> Items | Foundation -> Items | 4 | 34 | 0 | 0 | 34 | 0 |
| Foundation <-> Items | Items -> Foundation | 4 | 56 | 1 | 0 | 54 | 1 |
| Foundation <-> Platform | Foundation -> Platform | 17 | 376 | 215 | 0 | 153 | 8 |
| Foundation <-> Platform | Platform -> Foundation | 4 | 62 | 1 | 0 | 61 | 0 |
| Foundation <-> Quests | Foundation -> Quests | 3 | 12 | 3 | 0 | 9 | 0 |
| Foundation <-> Quests | Quests -> Foundation | 3 | 123 | 0 | 0 | 121 | 2 |
| Foundation <-> Social | Foundation -> Social | 1 | 3 | 0 | 0 | 3 | 0 |
| Foundation <-> Social | Social -> Foundation | 2 | 89 | 13 | 0 | 76 | 0 |
| Foundation <-> Space | Foundation -> Space | 3 | 12 | 7 | 0 | 5 | 0 |
| Foundation <-> Space | Space -> Foundation | 2 | 59 | 0 | 0 | 57 | 2 |
| Foundation <-> UI | Foundation -> UI | 6 | 15 | 4 | 0 | 9 | 2 |
| Foundation <-> UI | UI -> Foundation | 3 | 368 | 7 | 0 | 349 | 12 |
| Foundation <-> World | Foundation -> World | 3 | 29 | 6 | 0 | 23 | 0 |
| Foundation <-> World | World -> Foundation | 5 | 138 | 3 | 0 | 135 | 0 |
| Gameplay <-> Items | Gameplay -> Items | 14 | 23 | 0 | 0 | 23 | 0 |
| Gameplay <-> Items | Items -> Gameplay | 1 | 1 | 0 | 0 | 1 | 0 |
| Gameplay <-> Platform | Gameplay -> Platform | 146 | 341 | 95 | 0 | 241 | 5 |
| Gameplay <-> Platform | Platform -> Gameplay | 4 | 9 | 1 | 0 | 8 | 0 |
| Gameplay <-> Social | Gameplay -> Social | 3 | 3 | 0 | 0 | 3 | 0 |
| Gameplay <-> Social | Social -> Gameplay | 4 | 3 | 0 | 0 | 2 | 1 |
| Gameplay <-> UI | Gameplay -> UI | 18 | 30 | 4 | 0 | 26 | 0 |
| Gameplay <-> UI | UI -> Gameplay | 7 | 10 | 1 | 0 | 8 | 1 |
| Items <-> Platform | Items -> Platform | 10 | 84 | 45 | 0 | 39 | 0 |
| Items <-> Platform | Platform -> Items | 2 | 6 | 2 | 0 | 4 | 0 |
| Items <-> Space | Items -> Space | 1 | 1 | 0 | 0 | 1 | 0 |
| Items <-> Space | Space -> Items | 1 | 2 | 0 | 0 | 2 | 0 |
| Items <-> UI | Items -> UI | 4 | 13 | 3 | 0 | 10 | 0 |
| Items <-> UI | UI -> Items | 2 | 83 | 22 | 0 | 61 | 0 |
| Items <-> World | Items -> World | 2 | 5 | 1 | 0 | 4 | 0 |
| Items <-> World | World -> Items | 1 | 13 | 8 | 0 | 5 | 0 |
| Platform <-> Quests | Platform -> Quests | 1 | 8 | 8 | 0 | 0 | 0 |
| Platform <-> Quests | Quests -> Platform | 8 | 285 | 91 | 0 | 193 | 1 |
| Platform <-> Social | Platform -> Social | 2 | 5 | 4 | 0 | 1 | 0 |
| Platform <-> Social | Social -> Platform | 10 | 170 | 87 | 0 | 78 | 5 |
| Platform <-> Space | Platform -> Space | 1 | 1 | 1 | 0 | 0 | 0 |
| Platform <-> Space | Space -> Platform | 7 | 85 | 49 | 0 | 35 | 1 |
| Platform <-> UI | Platform -> UI | 1 | 4 | 2 | 0 | 0 | 2 |
| Platform <-> UI | UI -> Platform | 12 | 1,139 | 368 | 0 | 765 | 6 |
| Platform <-> World | Platform -> World | 1 | 16 | 16 | 0 | 0 | 0 |
| Platform <-> World | World -> Platform | 12 | 292 | 244 | 0 | 45 | 3 |
| Quests <-> UI | Quests -> UI | 2 | 9 | 0 | 0 | 9 | 0 |
| Quests <-> UI | UI -> Quests | 1 | 43 | 11 | 0 | 31 | 1 |
| Social <-> UI | Social -> UI | 4 | 64 | 27 | 0 | 36 | 1 |
| Social <-> UI | UI -> Social | 2 | 116 | 42 | 1 | 62 | 11 |
| Social <-> World | Social -> World | 2 | 24 | 1 | 0 | 23 | 0 |
| Social <-> World | World -> Social | 1 | 11 | 2 | 0 | 9 | 0 |
| Space <-> UI | Space -> UI | 1 | 4 | 0 | 0 | 4 | 0 |
| Space <-> UI | UI -> Space | 1 | 30 | 11 | 0 | 19 | 0 |
| Space <-> World | Space -> World | 2 | 25 | 11 | 0 | 14 | 0 |
| Space <-> World | World -> Space | 2 | 7 | 0 | 0 | 7 | 0 |
| UI <-> World | UI -> World | 1 | 135 | 45 | 0 | 86 | 4 |
| UI <-> World | World -> UI | 2 | 16 | 9 | 0 | 7 | 0 |

## Appendix B: Type moves

Each type that moves, with the unit edges and pairs it removes when moved alone (measured with today's guard). Together they take the pairs from 94 to 87; most single moves remove none because the same pairs also hold service calls. A target of Platform means `Core`. Family types move with their file or family whatever their own shape.

| Type | From | To | Kind | Domains using it | Unit edges removed alone | Pairs removed alone |
|---|---|---|---|---:|---:|---:|
| `AbilityDetail` | Domain/Abilities | Character | definition | 8 | 1 | 0 |
| `AbilityActivationType` | Domain/Abilities | Character | vocabulary | 4 | 0 | 0 |
| `AbilityImpactSummary` | Domain/Abilities | Character | vocabulary | 3 | 0 | 0 |
| `AbilityTargetingDetail` | Domain/Abilities | Character | family (moves with its file or family) | 3 | 0 | 0 |
| `DeflectionSource` | Domain/Combat | Character | vocabulary | 3 | 0 | 0 |
| `AbilityRecastDelayAction` | Domain/Abilities | Character | vocabulary | 2 | 0 | 0 |
| `AbilityTargetingFlags` | Domain/Abilities | Character | vocabulary | 2 | 0 | 0 |
| `AbilityTargetingShapeType` | Domain/Abilities | Character | vocabulary | 2 | 0 | 0 |
| `MeleeTechniqueDetail` | Domain/Abilities | Character | family (moves with its file or family) | 2 | 0 | 0 |
| `MeleeTechniqueLine` | Domain/Abilities | Character | vocabulary | 2 | 0 | 0 |
| `MeleeTechniqueStage` | Domain/Abilities | Character | vocabulary | 2 | 0 | 0 |
| `CombatDamageDeliveryType` | Domain/Combat | Character | vocabulary | 2 | 0 | 0 |
| `BeastRoleAttribute` | Domain/Companions | Character | vocabulary | 2 | 0 | 0 |
| `AbilityActivationAction` | Domain/Abilities | Character | vocabulary | 1 | 0 | 0 |
| `AbilityActivationDelayAction` | Domain/Abilities | Character | vocabulary | 1 | 0 | 0 |
| `AbilityImpactAction` | Domain/Abilities | Character | vocabulary | 1 | 0 | 0 |
| `IAbilityActivationRequirement` | Domain/Abilities | Character | definition | 1 | 0 | 0 |
| `BeastRoleType` | Domain/Companions | Character | vocabulary | 1 | 1 | 0 |
| `AbilityCustomValidationAction` | Domain/Abilities | Character | vocabulary | 0 | 0 | 0 |
| `AbilityTargetingSizeResolver` | Domain/Abilities | Character | vocabulary | 0 | 0 | 0 |
| `MeleeTechniqueProfile` | Domain/Abilities | Combat | vocabulary | 2 | 0 | 0 |
| `HealAmount` | Feature/AbilityDefinition | Combat | vocabulary | 2 | 0 | 0 |
| `TemporaryHitPointEffectKey` | Feature/AbilityDefinition | Combat | vocabulary | 2 | 0 | 0 |
| `FishRarity` | Domain/World | Crafting | vocabulary | 1 | 0 | 0 |
| `PerkType` | Domain/Character | Foundation | vocabulary | 15 | 1 | 0 |
| `SkillType` | Domain/Character | Foundation | vocabulary | 15 | 4 | 0 |
| `CombatDamageType` | Domain/Combat | Foundation | vocabulary | 11 | 0 | 0 |
| `ResistanceType` | Domain/Combat | Foundation | vocabulary | 11 | 0 | 0 |
| `CombatDamageTypeExtensions` | Domain/Combat | Foundation | family (moves with its file or family) | 9 | 0 | 0 |
| `RecastGroup` | Domain/Abilities | Foundation | vocabulary | 8 | 4 | 0 |
| `KeyItemType` | Domain/Items | Foundation | vocabulary | 8 | 1 | 0 |
| `SkillAttribute` | Domain/Character | Foundation | vocabulary | 6 | 0 | 0 |
| `RecipeType` | Domain/Crafting | Foundation | vocabulary | 6 | 0 | 0 |
| `CurrencyType` | Domain/Economy | Foundation | vocabulary | 6 | 2 | 1 |
| `BeastType` | Domain/Companions | Foundation | vocabulary | 5 | 0 | 0 |
| `PropertyType` | Domain/World | Foundation | vocabulary | 5 | 0 | 0 |
| `PropertyUpgradeType` | Domain/World | Foundation | vocabulary | 5 | 0 | 0 |
| `StructureType` | Domain/World | Foundation | vocabulary | 5 | 0 | 0 |
| `AchievementType` | Domain/Character | Foundation | vocabulary | 4 | 0 | 0 |
| `KeyItemAttribute` | Domain/Items | Foundation | vocabulary | 4 | 0 | 0 |
| `ShipStatus` | Domain/Space | Foundation | vocabulary | 4 | 2 | 1 |
| `PropertyLayoutType` | Domain/World | Foundation | vocabulary | 4 | 1 | 0 |
| `PropertyLocation` | Domain/World | Foundation | vocabulary | 4 | 0 | 0 |
| `PropertyLocationType` | Domain/World | Foundation | vocabulary | 4 | 0 | 0 |
| `SkillCategoryType` | Domain/Character | Foundation | vocabulary | 3 | 0 | 0 |
| `CraftMinigameType` | Domain/Crafting | Foundation | vocabulary | 3 | 0 | 0 |
| `MarketCategoryType` | Domain/Economy | Foundation | vocabulary | 3 | 0 | 0 |
| `FactionType` | Domain/Quests | Foundation | vocabulary | 3 | 0 | 0 |
| `QuestContractItem` | Domain/Quests | Foundation | vocabulary | 3 | 1 | 0 |
| `PropertyChildType` | Domain/World | Foundation | vocabulary | 3 | 0 | 0 |
| `PropertyPermissionType` | Domain/World | Foundation | vocabulary | 3 | 0 | 0 |
| `PropertyTaxType` | Domain/World | Foundation | vocabulary | 3 | 0 | 0 |
| `DMSavedCreatureFavorite` | Domain/Administration | Foundation | vocabulary | 2 | 0 | 0 |
| `SkillCategoryAttribute` | Domain/Character | Foundation | vocabulary | 2 | 0 | 0 |
| `BeastFoodType` | Domain/Companions | Foundation | vocabulary | 2 | 0 | 0 |
| `EnzymeColorType` | Domain/Companions | Foundation | vocabulary | 2 | 0 | 0 |
| `LineActionIds` | Domain/Crafting | Foundation | vocabulary | 2 | 0 | 0 |
| `ProspectingActionIds` | Domain/Crafting | Foundation | vocabulary | 2 | 0 | 0 |
| `ResearchCandidate` | Domain/Crafting | Foundation | vocabulary | 2 | 0 | 0 |
| `NPCGroupType` | Domain/Creatures | Foundation | vocabulary | 2 | 1 | 0 |
| `QuestContractObjective` | Domain/Quests | Foundation | vocabulary | 2 | 0 | 0 |
| `QuestContractStatus` | Domain/Quests | Foundation | vocabulary | 2 | 0 | 0 |
| `PlayerListFlagType` | Domain/Social | Foundation | vocabulary | 2 | 1 | 0 |
| `ElectionStageType` | Domain/World | Foundation | vocabulary | 2 | 0 | 0 |
| `PropertyDateType` | Domain/World | Foundation | vocabulary | 2 | 0 | 0 |
| `ShuttleRideStatus` | Domain/World | Foundation | vocabulary | 2 | 0 | 0 |
| `StructureAttribute` | Domain/World | Foundation | vocabulary | 2 | 0 | 0 |
| `TaxiDestinationType` | Domain/World | Foundation | vocabulary | 2 | 0 | 0 |
| `RecastGroupAttribute` | Domain/Abilities | Foundation | vocabulary | 1 | 0 | 0 |
| `AchievementAttribute` | Domain/Character | Foundation | vocabulary | 1 | 0 | 0 |
| `CombatPointCategoryType` | Domain/Character | Foundation | vocabulary | 1 | 0 | 0 |
| `NPCGroupAttribute` | Domain/Creatures | Foundation | vocabulary | 1 | 0 | 0 |
| `CurrencyAttribute` | Domain/Economy | Foundation | vocabulary | 1 | 0 | 0 |
| `MarketCategoryAttribute` | Domain/Economy | Foundation | vocabulary | 1 | 0 | 0 |
| `KeyItemCategoryAttribute` | Domain/Items | Foundation | vocabulary | 1 | 0 | 0 |
| `FactionAttribute` | Domain/Quests | Foundation | vocabulary | 1 | 0 | 0 |
| `PlayerListFlagAttribute` | Domain/Social | Foundation | vocabulary | 1 | 0 | 0 |
| `ExtraHotbarLayout` | Domain/UI | Foundation | vocabulary | 1 | 0 | 0 |
| `ExtraHotbarSlotType` | Domain/UI | Foundation | vocabulary | 1 | 0 | 0 |
| `PropertyPermissionAttribute` | Domain/World | Foundation | vocabulary | 1 | 0 | 0 |
| `PropertyPublicType` | Domain/World | Foundation | vocabulary | 1 | 0 | 0 |
| `PropertyTypeAttribute` | Domain/World | Foundation | vocabulary | 1 | 0 | 0 |
| `TaxiDestinationAttribute` | Domain/World | Foundation | vocabulary | 1 | 0 | 0 |
| `CombatDamageAttribute` | Domain/Combat | Foundation | vocabulary | 0 | 0 | 0 |
| `CombatDamageCategoryType` | Domain/Combat | Foundation | vocabulary | 0 | 0 | 0 |
| `CombatDamageDetails` | Domain/Combat | Foundation | family (moves with its file or family) | 0 | 0 | 0 |
| `KeyItemCategoryType` | Domain/Items | Foundation | vocabulary | 0 | 0 | 0 |
| `PropertyLoadType` | Domain/World | Foundation | vocabulary | 0 | 0 | 0 |
| `PropertySpawnType` | Domain/World | Foundation | vocabulary | 0 | 0 | 0 |
| `StructureCategoryType` | Domain/World | Foundation | vocabulary | 0 | 0 | 0 |
| `Log` | Domain/Foundation | Platform | family (moves with its file or family) | 16 | 0 | 0 |
| `LogGroup` | Domain/Foundation | Platform | vocabulary | 16 | 0 | 0 |
| `FrameMetric` | Domain/Foundation | Platform | vocabulary | 5 | 0 | 0 |
| `FrameProfiler` | Domain/Foundation | Platform | family (moves with its file or family) | 5 | 0 | 0 |
| `ServerEnvironmentType` | Platform/Enumeration | Platform | vocabulary | 3 | 1 | 0 |
| `FrameCaptureSummary` | Domain/Foundation | Platform | family (moves with its file or family) | 0 | 0 | 0 |
| `FrameCounters` | Domain/Foundation | Platform | family (moves with its file or family) | 0 | 0 | 0 |
| `LogGroupAttribute` | Domain/Foundation | Platform | vocabulary | 0 | 0 | 0 |
| `PlayerListCallState` | Domain/Social | World | vocabulary | 2 | 0 | 0 |

## Appendix C: Upward edges after the moves

Every direction that still points up in the approved order, logic and content bands, after the moves in Appendix B and the extraction of the remaining vocabulary. `hook:` rows are `Native` code, which P5-16b moves into Combat services. Members are source members; the fix and items listed cover most of them.

| Edge | Unit edges | Members | Main sources -> targets (members) | Fix | Item |
|---|---:|---:|---|---|---|
| Character -> Combat | 1 | 98 | StatService -> ICombatRecorderService (10), StatusEffectBase -> ICombatRecorderService (6), StatusEffectService -> IResistanceService (6) | invert: interface or event owned by Character; invert: recording interface owned by Foundation, implemented by the recorder | P5-4, P5-5, P5-1g |
| Combat -> Abilities | 1 | 65 | CombatAbilityImpactService -> IAbilityService (10), CombatResourcesService -> IAbilityService (7), CombatRecorderService -> IAbilityService (6) | invert: interface or event owned by Combat; move AbilityImpactSequence down | P5-3, P5-15a, P5-10 |
| Foundation -> Character | 1 | 36 | AbilityAnimationBinding -> AbilityDetail (9), MigrationService -> IStatService (6), CommunicationService -> ILanguageService (4) | invert: interface or event owned by Foundation; move the code up to Abilities | P5-1h, P5-11, P5-10 |
| Character -> Companions | 1 | 23 | PerkService -> IBeastMasteryService (6), PerkService -> IDroidService (5), StatService -> IBeastMasteryService (5) | invert: companion lookup interface owned by Character, implemented by BeastMastery and Droid; move DroidItemPropertyDetails down | P5-1g, P5-10 |
| Character -> Items | 1 | 22 | EquipmentPredicates -> IItemService (17), StatService -> IItemService (4), EquipmentStatTotals -> IItemService (1) | move the code up to Items; invert: interface or event owned by Character | P5-1h, P5-4, P5-9 |
| Combat -> Social | 1 | 18 | CombatAbilityImpactService -> IPartyService (3), CombatHitEffectsService -> IPartyService (3), CombatPointService -> IPartyService (3) | invert: interface or event owned by Combat | P5-7, P5-15a |
| Combat -> Creatures | 1 | 17 | EnmityService -> IAIService (9), EnmityService -> ICompanionControlService (5), StealthService -> IAIService (4) | invert: interface or event owned by Combat | P5-7, P5-8 |
| Character -> UI | 1 | 16 | StatusEffectService -> IGuiService (5), AchievementService -> IGuiService (4), DisguiseService -> IGuiService (4) | invert: window refresh publisher owned by Foundation, implemented by Gui | P5-1g |
| Character -> Abilities | 1 | 16 | PerkService -> IQuickCastService (4), StatService -> IMimicryService (4), StatusEffectService -> IAbilityService (4) | invert: interface or event owned by Character | P5-10, P5-4, P5-3 |
| Character content -> Abilities content | 2 | 16 | LastBastionBarrierStatusEffect -> TemporaryHitPointEffects (2), DevicesGrenadierPerkDefinition -> AbilityTargeting (1), LeadershipVanguardCommandPerkDefinition -> AbilityTargeting (1) | content -> content: call through Abilities's logic, or move the helper into Abilities logic | P5-2 |
| Combat -> Companions | 1 | 15 | CombatPointService -> IBeastMasteryService (3), EnmityService -> IBeastMasteryService (3), EnmityService -> IDroidService (3) | invert: companion lookup interface owned by Character, implemented by BeastMastery and Droid | P5-1g |
| Foundation content -> Character content | 1 | 12 | PelvisAppearanceMigration -> ArmorColorIndexCalculator (4), PelvisAppearanceMigration -> TintMapVariable (4), ItemAppearanceCatalogMigration -> EquippedItemAppearance (2) | content -> content: call through Character's logic, or move the helper into Character logic; content -> content: move CatharRacialAppearanceDefinition into logic or invert | P5-2 |
| Foundation -> Combat | 2 | 11 | MessagingService -> IStealthService (3), SavedCharacter -> IPowerGloveEquipmentService (3), SavedCharacterDataStore -> ICombatDamageCatalogService (1) | invert: interface or event owned by Foundation; move the code up to Combat | P5-15a, P5-1h, P5-7 |
| UI -> Abilities | 1 | 11 | ExtraHotbarsService -> IQuickCastService (7), ExtraHotbarsService -> IAbilityService (5) | invert: interface or event owned by UI | P5-11 |
| Abilities -> Social | 1 | 10 | AbilityService -> IPartyService (10) | invert: interface or event owned by Abilities | P5-3 |
| Items -> Companions | 1 | 10 | ItemService -> IDroidService (4), ItemService -> IBeastMasteryService (3), LootService -> IBeastMasteryService (3) | invert: companion lookup interface owned by Character, implemented by BeastMastery and Droid; move BeastDetail down | P5-1g, P5-1h, P5-15a |
| Foundation content -> Gameplay content | 4 | 10 | PistolBaseItemMigration -> PistolBaseItemCompatibility (3), SavedCombatUpgradeItemMigrations -> PistolBaseItemCompatibility (2), SavedCharacterLoad -> PistolBaseItemCompatibility (1) | content -> content: call through Gameplay's logic, or move the helper into Gameplay logic | P5-2 |
| Creatures -> Companions | 1 | 9 | AIService -> IBeastMasteryService (3), AIService -> IDroidService (3), CompanionControlService -> IBeastMasteryService (3) | invert: companion lookup interface owned by Character, implemented by BeastMastery and Droid | P5-1g |
| Foundation -> Administration | 2 | 9 | CommunicationService -> IDMPreferenceService (5), CommunicationService -> IDMPresenceService (4), DMCreatorFavorites -> DMFavoriteBlueprint (2) | move the code up to Social; move DMFavoriteBlueprint down | P5-11, P5-1h |
| Foundation -> World | 1 | 9 | CommunicationService -> IPlanetService (4), CommunicationService -> IPropertyService (4), CommunicationService -> IAreaService (3) | move the code up to Social | P5-11 |
| Items -> Combat | 1 | 9 | ItemService -> ITwinPistolEquipmentService (5), ItemService -> ICombatRecorderService (4), ItemService -> IRecastService (3) | invert: interface or event owned by Items; invert: recording interface owned by Foundation, implemented by the recorder | P5-9, P5-1g |
| World -> Social | 1 | 9 | HoloComService -> IPlayerListService (6), MusicService -> IPerformanceAudienceService (3) | invert: interface or event owned by World | P5-15a, P5-11 |
| Character -> Social | 1 | 8 | PlayerNameService -> IPlayerListService (6), PlayerNameService -> IChatPortraitTellsService (3) | invert: interface or event owned by Character | P5-10 |
| Combat -> World | 1 | 8 | DeathService -> IAreaService (3), DeathService -> IPropertyService (3), EnmityService -> IHoloComService (3) | invert: interface or event owned by Combat | P5-15a, P5-9, P5-7 |
| Foundation -> Items | 1 | 8 | CacheService -> IItemService (3), MigrationService -> IOverflowStorageService (3), SavedCharacterDataStore -> IItemService (1) | move the code up to Items; invert: interface or event owned by Foundation | P5-1h, P5-15a |
| Foundation -> UI | 2 | 8 | ConversationService -> IGuiService (6), Player -> GuiRectangle (2), ConversationService -> GuiPayloadBase (1) | invert: window refresh publisher owned by Foundation, implemented by Gui; move GuiRectangle down | P5-1g, P5-1h |
| UI content -> Gameplay content | 5 | 8 | AppearanceEditorViewModel -> ArmorDisplay (3), DMSettingsViewModel -> ArmorDisplay (2), CharacterFullRebuildViewModel -> PlayerInitialization (1) | content -> content: call through Gameplay's logic, or move the helper into Gameplay logic; content -> content: move PlayerInitialization into logic or invert | P5-2 |
| Administration -> World | 1 | 7 | DMPlayerDirectoryService -> IAreaService (4), DMObjectDetailsService -> IAreaService (3) | invert: interface or event owned by Administration | P5-15a |
| Combat -> Native | 1 | 7 | CombatRecorderService -> CombatRecorderEffectHooks (3), StealthService -> AppliedEffects (1), TwinPistolAbilityService -> PairedRangedAttackCycle (1) | same domain once the hook logic moves into Combat | P5-16b |
| Administration -> Creatures | 1 | 6 | DMBlueprintCatalogService -> ISpawnService (3), DMObjectDetailsService -> IAIService (3) | invert: interface or event owned by Administration | P5-8 |
| Combat -> Quests | 1 | 6 | CombatHitEffectsService -> ICapstoneProofService (3), TwinPistolMasteryService -> IQuestService (3) | invert: interface or event owned by Combat | P5-7, P5-15a |
| UI -> Combat | 1 | 6 | ExtraHotbarsService -> ITwinPistolEquipmentService (4), ExtraHotbarsService -> ITargetingService (3) | invert: interface or event owned by UI | P5-11 |
| World -> Space | 1 | 6 | HoloComService -> ISpaceService (5), StructureChangedAction -> ISpaceService (1) | invert: interface or event owned by World | P5-6 |
| Character -> Quests | 1 | 5 | StatusEffectService -> ICapstoneProofService (3), PerkRequirementQuest -> IQuestService (2), PerkRequirementQuest -> QuestDetail (2) | invert: interface or event owned by Character; invert: interface owned by Character | P5-10, P5-5 |
| UI -> Administration | 1 | 5 | GuiService -> IDMPresenceService (5) | invert: interface or event owned by UI | P5-15a |
| Platform -> Combat | 1 | 4 | ServerTeardown -> ICombatRecorderService (3), ManagedEntryDepth -> ICombatRecorderService (1) | invert: the recorder joins Core's IServerTeardown participants | P5-1g |
| Character -> Creatures | 1 | 4 | EspionageInfiltrationService -> IAIService (4) | invert: interface or event owned by Character | P5-8 |
| Quests -> Economy | 1 | 4 | GuildTaskXpReward -> IGuildTaskXpService (2), GPReward -> IGuildService (2) | invert: interface or event owned by Quests | P5-15a |
| UI -> Items | 1 | 4 | ExtraHotbarsService -> IItemService (4) | invert: interface or event owned by UI | P5-11 |
| UI content -> Abilities content | 1 | 4 | CharacterSheetViewModel -> DeviceAbilityEffects (1), CharacterSheetViewModel -> TemporaryHitPointEffects (1), DMPlayerExamineViewModel -> TemporaryHitPointEffects (1) | content -> content: call through Abilities's logic, or move the helper into Abilities logic | P5-2 |
| Abilities -> Quests | 1 | 3 | AbilityService -> ICapstoneProofService (3) | invert: interface or event owned by Abilities | P5-3 |
| Abilities -> Space | 1 | 3 | AbilityService -> ISpaceService (3) | invert: interface or event owned by Abilities | P5-3 |
| Abilities -> World | 1 | 3 | AbilityService -> IMusicService (3) | invert: interface or event owned by Abilities | P5-3 |
| Administration -> Space | 1 | 3 | DMPresenceService -> ISpaceService (3) | invert: interface or event owned by Administration | P5-6 |
| Character -> Administration | 1 | 3 | DisguiseService -> IDMPresenceService (3) | invert: interface or event owned by Character | P5-15a |
| Combat -> Space | 1 | 3 | CombatPointService -> ISpaceService (3) | invert: interface or event owned by Combat | P5-7 |
| Crafting -> World | 1 | 3 | CraftService -> IPropertyService (3) | invert: interface or event owned by Crafting | P5-9 |
| Creatures -> Social | 1 | 3 | AIService -> IPartyService (3) | invert: interface or event owned by Creatures | P5-8 |
| Foundation -> Social | 1 | 3 | CommunicationService -> IPartyService (3) | move the code up to Social | P5-11 |
| Foundation -> Space | 1 | 3 | CommunicationService -> ISpaceService (3) | move the code up to Social | P5-11 |
| Items -> Crafting | 1 | 3 | ItemService -> ICraftMinigameRoutingService (3) | invert: interface or event owned by Items | P5-9 |
| Items -> Administration | 1 | 3 | ItemTrashService -> IDMPresenceService (3) | invert: interface or event owned by Items | P5-9 |
| Items -> Economy | 1 | 3 | LightsaberWorkbenchService -> ICurrencyService (3) | invert: interface or event owned by Items | P5-15a |
| UI -> Crafting | 1 | 3 | ExtraHotbarsService -> ICraftMinigameRoutingService (3) | invert: interface or event owned by UI | P5-11 |
| UI -> Social | 1 | 3 | ExtraHotbarsService -> IChatCommandService (3) | invert: interface or event owned by UI | P5-11 |
| Items content -> Abilities content | 1 | 3 | FaultlineCapacitorItemDefinition -> TemporaryHitPointEffects (1), StormcoreMatrixItemDefinition -> TemporaryHitPointEffects (1), TidecallBeaconItemDefinition -> TemporaryHitPointEffects (1) | content -> content: call through Abilities's logic, or move the helper into Abilities logic | P5-2 |
| Foundation -> Creatures | 1 | 2 | WalkmeshService -> SpawnClearanceGrid (2) | move SpawnClearanceGrid down | P5-8 |
| Gameplay content -> Abilities content | 2 | 2 | NaturalRegeneration -> TemporaryHitPointEffects (1), UsePerkFeat -> ActionInterruption (1) | content -> content: call through Abilities's logic, or move the helper into Abilities logic | P5-2 |
| Foundation content -> UI content | 1 | 2 | ConversationStateSnippetDefinition -> CharacterStatRebuildViewModel (1), ConversationStateSnippetDefinition -> TrainingStoreViewModel (1) | content -> content: move CharacterStatRebuildViewModel into logic or invert; content -> content: move TrainingStoreViewModel into logic or invert | P5-2 |
| Platform -> Character | 1 | 1 | ApplicationSettings -> OOCAllowance (1) | move the OOCAllowance setting value down to Platform | P5-1h |
| Character -> Native | 1 | 1 | StatusEffectService -> AppliedEffects (1) | move the native helper (AppliedEffects) down, or invert | P5-16b |
| Creatures -> Native | 1 | 1 | AIService -> AppliedEffects (1) | move the native helper (AppliedEffects) down, or invert | P5-16b |
| Foundation -> Abilities | 1 | 1 | AnimationPreviewCatalog -> IAbilityService (1) | move the code up to Abilities | P5-1h |
| Foundation -> Native | 1 | 1 | SavedCharacter -> VitalityHitPoints (1) | move the native helper (VitalityHitPoints) down, or invert | P5-16b |
| Items -> World | 1 | 1 | KeyItemIcon -> IMusicService (1) | invert: interface or event owned by Items | P5-15a |
| hook: Native -> Combat | 1 | 25 | GetDamageRoll -> ICombatHitEffectsService (4), PairedRangedPresentation -> ITwinPistolEquipmentService (4), GetDamageRoll -> ICombatDamageTakenService (3) | move the hook logic into Combat services | P5-16b |
| hook: Native -> Character | 1 | 22 | GetDamageRoll -> IStatService (6), ResolveAttackRoll -> IStatService (5), PairedRangedAttackCycle -> IStatusEffectService (4) | move the hook logic into Combat services | P5-16b |
| hook: Native -> Gameplay content | 1 | 7 | ResolveAttackRoll -> UsePerkFeat (3), GetDamageRoll -> UsePerkFeat (2), PairedRangedAttackCycle -> UsePerkFeat (1) | hook names content: move the helper (UsePerkFeat and others) into logic | P5-16 |
| hook: Native -> Items | 1 | 4 | GetDamageRoll -> IItemService (1), PairedRangedAttackCycle -> ILootService (1), ResolveAttackRoll -> IItemService (1) | move the hook logic into Combat services | P5-16b |
| hook: Native -> Abilities | 1 | 2 | PairedRangedAttackCycle -> IAbilityService (1), WeaponAttackCycle -> IAbilityService (1) | move the hook logic into Combat services | P5-16b |
| hook: Native -> Abilities content | 1 | 1 | CombatRecorderEffectHooks -> TemporaryHitPointEffects (1) | hook names content: move the helper (UsePerkFeat and others) into logic | P5-16 |
| hook: Native -> Creatures | 1 | 1 | OnAIActionAttackObject -> IAIService (1) | move the hook logic into Combat services | P5-16b |
| hook: Native -> Character content | 1 | 1 | ResolveAttackRoll -> EmbattledStatusEffect (1) | hook names content: move the helper (UsePerkFeat and others) into logic | P5-16 |

### P5-15b: rows closed by the second half of P5-15a

Appendix C above is the measurement before the inversions; these rows are closed on the branch since, with the fix used. Edges
still open for P5-5, P5-6, P5-7, P5-8e to h, P5-9, P5-10, P5-12, P5-14 and P5-16b stay as the rows say.

| Row (source -> target) | Fix used |
|---|---|
| Character -> Combat: `EspionageInfiltrationService` -> `IStealthService` and `IEnmityService` | Character owns `IEspionageStealthQuery` (with the combat entry window variable) and `IEspionageEnmityQuery`; `IStealthService` and `IEnmityService` extend them and the container hands Espionage the same instances (`IAIService` stays for P5-8e) |
| Character -> Combat: `PlayerIntroductionService` -> `IRecastService` | Character owns `IRecastDelays` (the two calls the introduction makes); `IRecastService` extends it |
| Character -> UI: `AchievementService` -> `IGuiService` | Character owns `IAchievementNotification`; `AchievementNotificationWindow` in UI draws it (the drawing moved unchanged) |
| Items -> Companions: `LootService` -> `IBeastMasteryService`, `BeastDetail` | Items owns `IBeastCorpseInfo` (the corpse resref and variables, `GetBeastName`); `IBeastMasteryService` extends it; `BeastMasteryService` and `CompanionLookup` alias the constants |
| World -> Social: `MusicService` -> `IPerformanceWatchingService` (the construction cycle and its `Lazy<>`) | supply rule 3: World owns the notice `IMusicReplacedListener`; `MusicService` keeps the list (`RegisterMusicReplacedListener`) and `PerformanceWatchingService` registers itself in a boot handler (`OnModuleLoad`, rerun on reload) like `CreatureStatService` does with the status effects. The music no longer names a Social service, so the container builds it first and the `Lazy<>` is plain gone |
| Foundation -> UI: `Entity.Player` -> `GuiRectangle` | `GuiRectangle` moved down to Foundation (`DomainOrderTypeMoves.json`) |
| Foundation -> Administration: `DMCreatorFavorites` -> `DMFavoriteBlueprint` | `DMFavoriteBlueprint`, `DMBlueprintCategory` and `DMBlueprintSource` moved down to Foundation; the reads that name a `DMBlueprint` (`ToFavorite`, `Matches`) became extension methods in `DMFavoriteBlueprints` (Administration) |

Left open from the unlisted Character, Foundation and Items files: `ConversationService` and `PlayerFeedbackService` -> `IPlayerNameService`
(P5-10 splits `PlayerNameService`; the Foundation-owned name lookup belongs with its parts), `ConversationService` -> `IGuiService`
and `GuiPayloadBase` (needs a design decision: a window host owned by Foundation plus moving the payload type, and the
conversation view model is content), `EquipmentStatTotals`, `EquipmentPredicates`, `PerkRequirementBeastRole`, `PerkRequirementQuest`
and `KeyItemIcon` (static service lookups, P5-12 and P5-14), `SavedCharacter` -> `VitalityHitPoints` (P5-16b).

## Appendix D: Logic that names content, per entry

Every `LogicToContent.txt` entry, members by hardest class (section 2). Same-domain entries are per file.

**Cross-domain (46 entries)**

| Entry | Members | Vocabulary | Definition | Service call | Other | Main content types |
|---|---:|---:|---:|---:|---:|---|
| Domain/Combat -> Feature/StatusEffectDefinition | 114 | 0 | 0 | 0 | 114 | SuppressionStatusEffect, SunderStatusEffect, OrdinaryAttackHasteStatusEffect |
| Domain/Combat -> Feature/AbilityDefinition | 24 | 0 | 0 | 24 | 0 | AbilityTargeting, TemporaryHitPointEffects, AbilityEffectScaling |
| Domain/Abilities -> Feature/GuiDefinition | 8 | 8 | 0 | 0 | 0 | TechniqueChangedRefreshEvent, ExtraHotbarCooldownRefreshEvent |
| Domain/Social -> Feature/StatusEffectDefinition | 8 | 0 | 0 | 0 | 8 | InspiredStatusEffect |
| Domain/Quests -> Feature/GuiDefinition | 7 | 7 | 0 | 0 | 0 | QuestAbandonedRefreshEvent, QuestCompletedRefreshEvent, QuestContractPublishedRefreshEvent |
| Domain/Character -> Feature/GuiDefinition | 6 | 6 | 0 | 0 | 0 | RPXPRefreshEvent, DisguiseChangedRefreshEvent, SkillXPRefreshEvent |
| Domain/Combat -> Feature.UsePerkFeat | 6 | 0 | 0 | 6 | 0 | UsePerkFeat |
| Domain/Crafting -> Feature/GuiDefinition | 4 | 4 | 0 | 0 | 0 | RecipesPayload, ResearchPayload, ProspectingPayload |
| Domain/Combat -> Feature/AppearanceDefinition | 3 | 0 | 0 | 3 | 0 | EquippedItemAppearance, PowerGloveFit |
| Domain/Combat -> Feature/QuestDefinition | 3 | 3 | 0 | 0 | 0 | TwinPistolCapstoneQuestDefinition |
| Domain/Companions -> Feature/QuestDefinition | 3 | 0 | 0 | 0 | 3 | BeastStableQuestDefinition |
| Domain/Companions -> Feature/AppearanceDefinition | 3 | 0 | 0 | 3 | 0 | HelmetModelRenderer, TintMapService |
| Domain/Crafting -> Feature/StatusEffectDefinition | 3 | 0 | 0 | 0 | 3 | OverclockStatusEffect, SafeModeStatusEffect, ColdWorkingStatusEffect |
| Domain/Creatures -> Feature.UsePerkFeat | 3 | 0 | 0 | 3 | 0 | UsePerkFeat |
| Domain/Economy -> Feature/GuiDefinition | 3 | 3 | 0 | 0 | 0 | CurrencyRefreshEvent, CasinoSlotPayload |
| Domain/Foundation -> Feature/GuiDefinition | 3 | 2 | 0 | 0 | 1 | ConversationViewModel, ConversationPayload |
| Domain/Items -> Feature/AppearanceDefinition | 3 | 0 | 0 | 3 | 0 | BaseArmorAppearanceDefinition, EquippedItemAppearance, ArmorAppearanceCatalog |
| Domain/World -> Feature/AbilityDefinition | 3 | 0 | 0 | 3 | 0 | CombatAreaPulses, DeviceAbilityEffects |
| Domain/Abilities -> Feature.UsePerkFeat | 2 | 0 | 0 | 2 | 0 | UsePerkFeat |
| Domain/Character -> Feature/AbilityDefinition | 2 | 0 | 0 | 2 | 0 | TemporaryHitPointEffects |
| Domain/Combat -> Feature/MigrationDefinition | 2 | 0 | 0 | 2 | 0 | PlayerEquipmentStorage |
| Domain/Combat -> Feature.LightsaberAudio | 2 | 0 | 0 | 2 | 0 | LightsaberAudio |
| Domain/Companions -> Feature/StatusEffectDefinition | 2 | 0 | 0 | 0 | 2 | RestStatusEffect |
| Domain/Companions -> Feature/GuiDefinition | 2 | 2 | 0 | 0 | 0 | BeastGainXPRefreshEvent, IncubatorPayload |
| Domain/Creatures -> Feature/StatusEffectDefinition | 2 | 0 | 0 | 0 | 2 | RestStatusEffect |
| Domain/Social -> Feature/AbilityDefinition | 2 | 0 | 0 | 2 | 0 | AbilityEffectScaling |
| Domain/Social -> Feature/GuiDefinition | 2 | 1 | 0 | 0 | 1 | PerformanceWatchingViewModel, PlayerListRefreshEvent |
| Domain/World -> Feature/StatusEffectDefinition | 2 | 0 | 0 | 0 | 2 | BleedStatusEffect |
| Domain/Abilities -> Feature/StatusEffectDefinition | 1 | 0 | 0 | 0 | 1 | AuraStatusEffectBase |
| Domain/Abilities -> Feature.TwinPistolEquipmentValidation | 1 | 0 | 0 | 1 | 0 | TwinPistolEquipmentValidation |
| Domain/Abilities -> Feature.PistolAnimationRemap | 1 | 0 | 0 | 1 | 0 | PistolAnimationRemap |
| Domain/Administration -> Feature.DMActions | 1 | 0 | 0 | 0 | 1 | DMActions |
| Domain/Combat -> Feature.PlayerTemporaryEffects | 1 | 0 | 0 | 0 | 1 | PlayerTemporaryEffects |
| Domain/Combat -> Feature/GuiDefinition | 1 | 1 | 0 | 0 | 0 | StatAdjustmentRefreshEvent |
| Domain/Companions -> Feature/AbilityDefinition | 1 | 0 | 0 | 1 | 0 | ReviveRules |
| Domain/Crafting -> Feature.PlayerTemporaryEffects | 1 | 0 | 0 | 0 | 1 | PlayerTemporaryEffects |
| Domain/Crafting -> Feature/ItemDefinition | 1 | 0 | 0 | 0 | 1 | HarvesterItemDefinition |
| Domain/Foundation -> Feature.PistolAnimationRemap | 1 | 0 | 0 | 1 | 0 | PistolAnimationRemap |
| Domain/Items -> Feature/GuiDefinition | 1 | 1 | 0 | 0 | 0 | KeyItemReceivedRefreshEvent |
| Domain/Quests -> Feature/DialogDefinition | 1 | 1 | 0 | 0 | 0 | QuestRewardSelectionDialog |
| Domain/Social -> Feature/AppearanceDefinition | 1 | 0 | 0 | 1 | 0 | HornFit |
| Domain/Space -> Feature/AppearanceDefinition | 1 | 0 | 0 | 1 | 0 | HelmetModelRenderer, TintMapService |
| Domain/UI -> Feature/MigrationDefinition | 1 | 0 | 0 | 1 | 0 | PlayerEquipmentStorage |
| Domain/UI -> Feature.UsePerkFeat | 1 | 0 | 0 | 1 | 0 | UsePerkFeat |
| Domain/World -> Feature/GuiDefinition | 1 | 1 | 0 | 0 | 0 | LinePayload |
| Domain/World -> Feature/DialogDefinition | 1 | 1 | 0 | 0 | 0 | PlaceCityHallDialog |

**Native (8 entries)**

| Entry | Members | Vocabulary | Definition | Service call | Other | Main content types |
|---|---:|---:|---:|---:|---:|---|
| Platform/Native -> Feature.UsePerkFeat | 7 | 0 | 0 | 7 | 0 | UsePerkFeat |
| Platform/Native -> Feature/AbilityDefinition | 1 | 0 | 0 | 1 | 0 | TemporaryHitPointEffects |
| Platform/Native -> Feature/MigrationDefinition | 1 | 0 | 0 | 1 | 0 | SaberRecalibration |
| Platform/Native -> Feature.PistolAnimationRemap | 1 | 1 | 0 | 0 | 0 | PistolAnimationRemap |
| Platform/Native -> Feature/StatusEffectDefinition | 1 | 0 | 0 | 0 | 1 | EmbattledStatusEffect |
| Platform/NativeSinks -> Feature/AppearanceDefinition | 1 | 0 | 0 | 1 | 0 | PartSkinModelRenderer, NeckModelRenderer, ShoulderModelRenderer |
| Platform/NativeSinks -> Feature.TwinPistolEquipmentValidation | 1 | 0 | 0 | 1 | 0 | TwinPistolEquipmentValidation |
| Platform/NativeSinks -> Feature.UsePerkFeat | 1 | 0 | 0 | 1 | 0 | UsePerkFeat |

**Same domain (17 entries)**

| Entry | Members | Vocabulary | Definition | Service call | Other | Main content types |
|---|---:|---:|---:|---:|---:|---|
| Domain/Foundation/SavedCharacter.cs -> Feature/MigrationDefinition | 47 | 20 | 0 | 0 | 27 | Node, Field, StoredObjectData |
| Domain/Foundation/SavedItem.cs -> Feature/MigrationDefinition | 43 | 16 | 0 | 0 | 27 | Field, Node, StoredObjectData |
| Domain/Character/AppearanceRenderStateService.cs -> Feature/AppearanceDefinition | 12 | 0 | 0 | 0 | 12 | NeckModelCatalog, PartSkinModelCatalog, RobeModelCatalog |
| Domain/Character/TintMapStateService.cs -> Feature/AppearanceDefinition | 10 | 3 | 2 | 0 | 5 | TintMapLayerType, TintMapEquipmentMaterialIndex, TintMapMaterialDefinition |
| Domain/Character/IAppearanceRenderStateService.cs -> Feature/AppearanceDefinition | 8 | 0 | 0 | 0 | 8 | NeckModelCatalog, PartSkinModelCatalog, RobeModelCatalog |
| Domain/Character/ITintMapStateService.cs -> Feature/AppearanceDefinition | 8 | 6 | 2 | 0 | 0 | TintMapLayerType, TintMapMaterialDefinition |
| Domain/UI/ExtraHotbarsService.cs -> Feature/GuiDefinition | 8 | 4 | 0 | 0 | 4 | ExtraHotbarViewModel, ExtraHotbarRefreshEvent, ExtraHotbarStatusRefreshEvent |
| Domain/Abilities/FieldEngineerPulseEmittersService.cs -> Feature/AbilityDefinition | 7 | 7 | 0 | 0 | 0 | FieldEngineerPulseEmitter |
| Domain/Abilities/IFieldEngineerPulseEmittersService.cs -> Feature/AbilityDefinition | 5 | 5 | 0 | 0 | 0 | FieldEngineerPulseEmitter |
| Domain/UI/GuiService.cs -> Feature/GuiDefinition | 4 | 2 | 0 | 0 | 2 | CharacterSheetViewModel, EquipItemRefreshEvent, UnequipItemRefreshEvent |
| Domain/Foundation/SavedCharacterDataStore.cs -> Feature/MigrationDefinition | 2 | 0 | 0 | 2 | 0 | SavedDroidBoostItemMigration, SavedItemResistanceMigration, DungeonSaberMigration |
| Domain/Abilities/AbilityService.cs -> Feature/AbilityDefinition | 1 | 0 | 0 | 1 | 0 | AbilityTargeting |
| Domain/Abilities/QuickCastService.cs -> Feature/AbilityDefinition | 1 | 0 | 0 | 1 | 0 | ActionInterruption |
| Domain/Character/StatService.cs -> Feature/StatusEffectDefinition | 1 | 0 | 0 | 0 | 1 | FoodStatusEffect |
| Domain/Character/TintMapStateRevisionScope.cs -> Feature/AppearanceDefinition | 1 | 1 | 0 | 0 | 0 | TintMapLayerType |
| Domain/Foundation/CharacterFileUpgrade.cs -> Feature/MigrationDefinition | 1 | 0 | 0 | 1 | 0 | SavedCharacterLoad |
| Domain/Quests/CapstoneProofService.cs -> Feature/QuestDefinition | 1 | 1 | 0 | 0 | 0 | CapstoneProofDefinitions |
