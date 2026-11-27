# Maintainability Refactor: Content Injection (P5-13)

How content code gets its services through the container instead of the static `Composition/GameServices` lookup, so that
`GameServices`, `FacadeAccessor` and `FacadeCache<T>` can be deleted (Phase 5 target 1). This is the P5-13 design; P5-13 also
converts the pilot, P5-12 applies the design to `Domain/`, `Native/` and `NativeSinks/`, and P5-14 converts the rest in
waves. Status lives in [MaintainabilityRefactorStatus.md](MaintainabilityRefactorStatus.md). This file goes away at cutover.

## Owner decisions

From the plan (2026-11-14): content is built through the container; injected members keep today's call text (`Ability.X`,
`Stat.Y`); `GameServices` and `FacadeAccessor` are deleted at the end; `Log`, `Clock`, `Random` and `FrameProfiler` stay
static; no change in game behaviour.

On this design (2026-11-14):

1. **Constructor injection for every type**, made content included. No method or property injection anywhere. Target 1
   keeps its wording ("gets the service through its constructor").
2. **No static accessor in tests.**

## How the numbers were measured

- Branch `feature/maintainability-refactor` at `dfc310a4a`. Its server and engine-test code equals that of `5282fe9ef`,
  which the Phase 5 plan measured.
- Three Roslyn syntax scans (`Microsoft.CodeAnalysis.CSharp` 4.14, no semantic model) over every `.cs` file outside `bin/`
  and `obj/`:
  - **Lookups.** One of the 149 `GameServices` property names used as a value, as `GameServices.X` or bare in a file that
    imports `GameServices` (`using static`, or the global import of `EngineTests` and `SWLOR.Admin`). Not counted: names
    declared by the enclosing type, a base, a parameter or a local, object-initializer member names, `nameof`.
  - **Reach.** A member reaches a service when it has a lookup or uses a member that reaches one (its own class or a base
    unqualified, or `Type.Member` of a server type), solved to a fixed point.
  - **Creation sites.** Every `new T(...)`, `new T { ... }` and target-typed `new(...)` (typed from its declaration) of a
    type that will take services in its constructor, and every `Activator.CreateInstance`.
- The lookup scan agrees with the plan within 1%: 553 `Feature/` files (plan 553), 645 server files (646), 78 static content
  classes with 45 holding handlers (78, 45), 1,768 of 4,803 `Feature/` lookups in a static context (1,765 of 4,796).
  Where the independent review counted differently, both numbers are given.

## Decisions

1. **Every type takes its services through its constructor.** Four kinds, by who builds the object:
   - *Singleton content* (one per build): the container builds it, as it does a service.
   - *Made content* (definitions, view models, menus, status effects, payloads, engine-test classes): `IContentFactory`
     builds it, with its services from the container and its values from the caller.
   - *Private collaborators* (an object a class creates for its own work, such as an emitter or a recorder scope): the
     creating class passes the services it holds.
   - *Data* (rule objects, profiles, what builders make): takes no service. Behaviour that needs one moves to the service
     that owns the data.
2. **Service calls keep their text.** Each service becomes a private get-only property named like the `GameServices`
   property (`Stat`), set by the constructor, so `Stat.GetStat(...)` does not change. A former static content class is
   injected under its class name (`InnateAbility.X(...)`). The exception is a class that already has a member of that name
   (7 types).
3. **Creation text changes where the created type takes services:** `new VenomStatusEffect(5)` becomes
   `Content.Create<VenomStatusEffect, int>(5)`, where `Content` is the creator's `IContentFactory`. The tool writes it, on
   the branch and on master's code at every sync.
4. **Handlers keep their names.** A static content class becomes an instance class of the same name.
5. **Runtime state goes to services,** as target 1 and `DependencyInjection.md` require. A content singleton holds only its
   dependencies and immutable tables.
6. **The reload carry builds an object with services through its constructor,** then fills its carried fields.
7. **No static accessor anywhere, tests included.** Fixtures read services from their own container; engine-test classes
   are made content that the runner builds with the factory it is handed. Until C11 one temporary `GameServices.Content`
   property lets code that is not converted yet create converted content with the same text.
8. **One re-runnable tool** (`tools/Refactor/SWLOR.ContentInjection`) converts, also on every sync, and refuses what needs
   a person (cycles, state, handlers on made types, more than 10 services, named arguments and initializers at creation
   sites).

## Content today

`Activator.CreateInstance` is called on 41 lines in the server: 34 in `Domain/` build content (26 in service instance
methods, 8 elsewhere), 4 in `WeaponActiveAbilityDefinitionBase`, and 3 others (database entities, the migration schema gate
in `GameGeneration`, a carry converter).

| Family | Found by | Built by | Built when | Instances |
|---|---|---|---|---|
| Abilities | `IAbilityListDefinition` | `AbilityService.CacheAbilities` | `mod_cache_bef` | one per build; closures keep it |
| Perks, items, quests, chat commands, snippets, spawns, loot, beasts, fishing | their `I*ListDefinition` | `PerkService`, `ItemService`, `QuestService`, `ChatCommandService`, `SnippetService`, `SpawnService`, `LootService`, `BeastMasteryService`, `FishingService` | `mod_cache_bef` | one per build |
| Recipes, AI profiles, ships, ship modules, space objects, property layouts | their list interfaces | `CraftService` (skill cache), `AIService`, `SpaceService`, `PropertyCatalogService` | at their cache handler | one per build |
| Migrations | `IServerMigration`, `ICharacterMigration` | `MigrationService`, `CharacterFileUpgrade`; `GameGeneration.RequiredSchemaVersion` builds each to read `Version` | `db_loaded`, the reload's schema check | one per run |
| GUI windows | `IGuiWindowDefinition` | `GuiService.LoadWindowTemplates` | `mod_cache_aft` | one per build |
| View models | the window's `T` | the `GuiWindowBuilder<T>` delegate (`Activator.CreateInstance<T>`) | one per window at `mod_cache_aft`, then per window per player at `mod_enter` | per player |
| Conversation menus | `ConversationMenuDefinition` | `ConversationMenuService.Start` | each time a menu opens | per conversation |
| Status effects | `StatusEffectBase` | `StatusEffectService` (a sample per type at `mod_cache_bef`, then per application by type), 228 `new ...StatusEffect(` sites, `Clone` | per application | per creature; carried across a reload |
| Rule objects | none | `PerkBuilder`, `QuestBuilder`, `AbilityBuilder`, `RecipeBuilder` with `new`; `QuestContractFactory` at run time | inside a definition's build | per definition |
| Static classes | `[NWNEventHandler]` methods | `ScriptRegistry` binds static methods | `LoadScripts` | none |
| Native sinks | listed | `NativeSinkFactory.Create` with `new` | before the container is built | one per build |
| Engine tests | `[EngineTest]` on public static methods | `EngineTest` runner, a static class in an optional assembly loaded after the container | per run | none |

The 554 `Feature/` types that use the lookup:

| Kind | Types | Lookups | Of them in a static context |
|---|--:|--:|--:|
| Made: definitions (lists, windows, menus, migrations) | 289 | 1,351 | 465 |
| Made: view models | 94 | 1,858 | 53 |
| Made: status effects | 63 | 240 | 32 |
| Made, collaborators or data: other | 14 | 240 | 104 |
| Singleton: static classes | 78 | 1,032 | 1,032 |
| Singleton: classes with only static members, never built | 16 | 82 | 82 |

Sixteen more `Feature/` static classes reach a service only through other static classes (`NPCSignatureAbility`,
`NeckAppearance`, `ItemIconMigration`, ...): 94 in all (the review counted 93). With the 16 static-only classes, `Native/`
(7), `Domain/` helpers (30) and `Core/` (1), 148 classes become singletons or services.

**Which made types take services.** Every non-static server type with an instance member that reaches a service, plus
every subclass of such a base, less what this design makes service-free (below): 795 types. 434 are ability definitions
(most through the weapon, heavy vibroblade, unarmed, twin pistol and spear bases), 102 GUI types, 69 status effects, 34 quest,
24 item, 22 migration, 18 ship module, 17 menu, 12 chat command, 10 perk and 8 snippet types, and 45 others. 742 of them
have no constructor with parameters today; the rest take 1 to 6 values, except `FieldEngineerPulseEmitter` (18), a private
collaborator.

**Made service-free instead** (each saves a constructor change in every subclass, or every creation site):

- `StatusEffectBase`: its 10 `CombatRecorder` calls wrap the 8 lifecycle calls (`ApplyEffect`, `RemoveEffect`, `TickEffect`,
  `OnHitEffect`, ...), all made by `StatusEffectService`, which already holds the recorder. They move there (C7). The 321
  status effects that use no service themselves keep `new X(...)` unchanged.
- `CreatureStatusEffect`: its `Stat` calls forward to the pure `StatRules`, which it calls instead.
- Rule objects (33 types: perk, ability and recipe requirements, quest objectives, prerequisites and rewards, mutation
  requirements, and `QuestDetail`'s behaviour): data; their checks move to a checker in the owning service (P5-12).
  Otherwise every builder would need the factory and 611 builder creations in content would change (`AbilityBuilder` 453,
  `RecipeBuilder` 73, `PerkBuilder` 47, `QuestBuilder` 38).
- `GuiWindowBuilder` (built at 96 window-definition sites) registers element events with `Gui` while it builds; it records
  them on the constructed window instead and `GuiService` registers them when it caches the window (P5-11, which splits
  `Build`). `SpaceObjectBuilder` (2 sites) likewise leaves its two `Space` checks to `SpaceService`.
- `WeaponAbilityProfile` (148 sites, 14 services): data plus a rider singleton the weapon base takes (P5-16).

## Singleton content

**What becomes one:** the 94 static `Feature/` classes and 16 static-only classes that reach a service, the 7 `Native/`
static classes and the native sinks (P5-12).

**The conversion** (the tool does it): `static` comes off the class and off every member that reaches a service; constants,
immutable tables and members that reach none stay static. Each service becomes a private get-only property set by the
constructor. The `GameServices` import goes, `GameServices.X.` becomes `X.` (the four names P2-Z had to spell out return to
master's text), and a self-qualified call to a member that becomes instance loses the qualifier (none today). The class is
added to `Composition/ContentRegistrations.cs`, which the tool writes.

Before:

```csharp
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature
{
    public static class PlaceableScripts
    {
        [NWNEventHandler(ScriptName.OnPlaceableBuyRebuild)]
        public static void PurchaseRebuild()
        {
            PurchaseRebuild(GetPCSpeaker());
        }

        public static bool PurchaseRebuild(uint player)
        {
            // ...
            Currency.TakeCurrency(player, CurrencyType.RebuildToken, 1);
            // ...
        }
    }
}
```

After:

```csharp
namespace SWLOR.Game.Server.Feature
{
    public class PlaceableScripts
    {
        private IConversationService Conversation { get; }
        private IConversationMenuService ConversationMenu { get; }
        private ICurrencyService Currency { get; }
        // ... Enmity, Gui, KeyItem, Party, PlayerName

        public PlaceableScripts(IConversationService conversation, IConversationMenuService conversationMenu,
            ICurrencyService currency /* , ... */)
        {
            Conversation = conversation;
            ConversationMenu = conversationMenu;
            Currency = currency;
            // ...
        }

        [NWNEventHandler(ScriptName.OnPlaceableBuyRebuild)]
        public void PurchaseRebuild()
        {
            PurchaseRebuild(GetPCSpeaker());
        }

        public bool PurchaseRebuild(uint player)
        {
            // ...
            Currency.TakeCurrency(player, CurrencyType.RebuildToken, 1);
            // ...
        }
    }
}
```

**Handlers.** `ScriptRegistry` binds a public parameterless instance handler to the singleton
`ContainerHost.ResolveHandlerOwner` gives and names it `DeclaringType.FullName + "." + Name`, so the manifest line does not
change. Of the 235 static handler methods (259 registrations) on `Feature/` types that use the lookup, 214 are on singleton
content and keep their names.

**Static members other code calls.** A caller takes the singleton under its class name, typed as the class
(`private InnateAbility InnateAbility { get; }`); "Color Color" resolution keeps `InnateAbility.Apply(...)` and its remaining
static members. A static class converts in the first wave in which all its server callers can take it.

**State.** Runtime state of a static content class moves to a service (P5-16, or the wave for anything P5-16 missed). A
static field initializer that reads a service moves to first use, which is when the type initializer runs today. None
exists (the last one, `SerializedItemWeaponDamageTypeMigration.WeaponDamageScales`, went with that migration in master's
#3486): an instance initializer could not read the property, and the constructor would run it at container build, off the
engine thread.

**Cycles.** The container cannot build two singletons that need each other, and target 4 forbids `Lazy<>`. Three groups,
each with a hand step before the wave that converts it:

| Group | Loop | Step | When |
|---|---|---|---|
| `HeadgearSlotMigration`, `LootStatRevisionMigration` | `LootStatRevisionMigration.MigrateObject` calls `HeadgearSlotMigration.ConvertItem` (`:73`); `HeadgearSlotMigration.MigrateObject` only forwards to `LootStatRevisionMigration.MigrateHeadgear` (`:40`) | Remove the forwarder; its 9 callers (two engine-test files) call `MigrateHeadgear` | C2 |
| `EquipmentPredicates`, `WeaponDamage` (`Domain/`) | `HasSingleWeapon` calls `WeaponDamage.IsSingleWeaponType`; `GetEffectiveDMG` calls `HasSingleWeapon` | Move `IsSingleWeaponType` into `EquipmentPredicates` | P5-12 |
| 12 appearance classes: `TintMapService`, `TintMapModelResolver`, `EquippedItemAppearance`, `ArmorPartAppearance`, `NeckAppearance`, `RobeAppearance`, the four model renderers, `HornFit`, `PowerGloveFit` | for example `RobeModelRenderer.Apply` -> `EquippedItemAppearance.Refresh` -> `NeckAppearance.RemoveUnavailableNeck` -> `RobeModelRenderer.GetBasePhenotype` | Split into a lower part (the queries) and an upper part (apply, refresh, remove unavailable) | P5-11, which moves `TintMapService` into `Domain`; target 4 forbids it calling the other 11 in `Feature/`, so they move with it |

The tool computes the groups on every run and refuses to convert a class in one. 41 call sites in 18 `Domain/` types call
service-reaching members of `Feature/` singletons today; P5-2 removes them before P5-14, so no service depends on content.

## Made content

**The form.** The constructor takes the services first, then the values it takes today:

```csharp
public sealed class VenomStatusEffect : StatusEffectBase
{
    private ICombatDamageModifiersService CombatDamageModifiers { get; }
    private ICombatDamageTakenService CombatDamageTaken { get; }
    private IResistanceService Resistance { get; }
    private IStatService Stat { get; }

    public VenomStatusEffect(ICombatDamageModifiersService combatDamageModifiers, ICombatDamageTakenService combatDamageTaken,
        IResistanceService resistance, IStatService stat)
        : this(combatDamageModifiers, combatDamageTaken, resistance, stat, 0)
    {
    }

    public VenomStatusEffect(ICombatDamageModifiersService combatDamageModifiers, ICombatDamageTakenService combatDamageTaken,
        IResistanceService resistance, IStatService stat, int damageBonusPercent, int baseDamagePerTick = DefaultDamagePerTick)
    {
        CombatDamageModifiers = combatDamageModifiers;
        CombatDamageTaken = combatDamageTaken;
        Resistance = resistance;
        Stat = stat;
        UpdatePotency(baseDamagePerTick, damageBonusPercent);
    }

    public override IStatusEffect Clone() =>
        new VenomStatusEffect(CombatDamageModifiers, CombatDamageTaken, Resistance, Stat, _damageBonusPercent, _baseDamagePerTick);

    protected override void Tick(uint creature)
    {
        // ... service calls unchanged
        damageAmount = Resistance.ApplyResistanceToDamage(creature, ResistanceType, damageAmount);
    }
}
```

The creator, before and after:

```csharp
// PoisonsService today
ApplyStatusEffect(attacker, defender, new VenomStatusEffect(potency, damagePerTick), durationSeconds);
// after
ApplyStatusEffect(attacker, defender, _content.Create<VenomStatusEffect, int, int>(potency, damagePerTick), durationSeconds);
```

A content creator writes `Content.Create<...>(...)`; a service writes `_content`, as it names every dependency.

- **Bases** take their own services through their constructor, and subclasses pass them on; the tool writes the forwarding
  constructors. `GuiViewModelBase` takes `IGuiService` (98 view models forward it); the weapon ability bases take what P5-16
  leaves them. The compiler checks every hierarchy.
- **A class copying itself** (33 server sites, 22 of them status effect `Clone` methods) passes its own services, with no
  factory.
- **Constructors may use their services** after assigning them, at today's time: `ExamineItemPayload` reads `Item`,
  `DefaultAIProfileDefinition` reads `Ability.GetAllAbilityDetails()`. `SentinelGuardStatusEffect` calls the pure
  `StatRules.GetGrantedDeflectionStatType`, to which its `Stat` call only forwards, so its values-free constructor needs no
  service.

**The factory.**

```csharp
namespace SWLOR.Game.Server.Core
{
    /// <summary>Builds made content with its services from the container. Never builds or returns a registered type.</summary>
    public interface IContentFactory
    {
        T Create<T>() where T : class;
        T Create<T>(Type type) where T : class;
        T Create<T, A1>(A1 a1) where T : class;
        T Create<T, A1, A2>(A1 a1, A2 a2) where T : class;
        // ... up to six values, the most any made type takes
    }
}
```

- **How it builds.** For each type and argument signature, on first use, `Composition/ContentFactory` picks the public
  constructor whose leading parameters are registered types and whose next parameters are exactly `A1..An` (the rest
  optional, filled with their declared defaults; the one with fewest parameters wins, as C# picks). It resolves the services
  once and compiles a `Func<A1, ..., T>` with them as constants. Each `Create` signature keeps a slot holding the factory it
  belongs to and the delegate, checked by reference, as `FacadeCache<T>` does today (about 4 ns over a direct call,
  measured by `FacadeCacheTests`); a second factory (a test) falls back to a dictionary. The slots are statics of the game
  build, so they unload with it.
- **What it costs.** The object and nothing else: no argument array, no boxing, no lookup per service. The object is 8
  bytes larger per service it holds (`VenomStatusEffect` +32 bytes per application). No existing allocation pin measures a
  creation path (the pins in `PoisonsServiceTests`, `StatusEffectFacadeAllocationTests` and the others are on reads, at 0
  bytes, and stay so); `ContentFactoryAllocationTests` pins that `Create` allocates exactly what the constructor allocates.
- **Why not `ActivatorUtilities`.** `CreateInstance(provider, type, values)` matches constructors by reflection on every
  call; `CreateFactory` caches that but takes an `object[]` and boxes each value, about 32 to 80 extra bytes per creation on
  the status effect paths.
- **Hot paths.** Per-swing code (`Native/`, the attack sinks) creates no made content. Combat services create made content
  at 14 sites: 10 status effects (`InfectionStatusEffect`, `VenomStatusEffect`, ...) through the factory, and 3 recorder
  scopes and 1 impact profile as private collaborators. Each factory creation costs one slot check and one delegate call
  more than today.
- **Who may hold it.** Services that build or own content (the 26 discovery lines, `StatusEffectService`, `GuiService`,
  `ConversationMenuService` and the 22 `Domain/` files that create made content), content that creates made content (about
  20 `Feature/` files today: ability definitions and helpers that create status effects, view models that create payloads,
  an item, a migration), the engine-test runner and test classes, and tools at their entry. It counts as one service toward
  the limit of 10.
- **The guard.** `Create` throws for a registered service or content singleton, for an abstract or non-game type, and for a
  type outside the content namespaces and the test assembly, so it never hands out a service. An architecture rule keeps
  the service parameters of made content private and forbids `new` and `Activator` of a type whose constructor takes a
  registered type, except a class copying itself and a creator passing its own services to a private collaborator.

**Who builds what:**

| Site | Change |
|---|---|
| The 26 discovery lines in service instance methods (`AbilityService`, `PerkService`, `ConversationMenuService`, `StatusEffectService` 6, `SpaceService` 3, ...) | `Activator.CreateInstance(type)` becomes `_content.Create<T>(type)` |
| View models | `GuiConstructedWindow` gains `ViewModelType` (set by `GuiWindowBuilder<T>`); `GuiService` builds each with `_content.Create<IGuiViewModel>(type)` |
| `WeaponActiveAbilityDefinitionBase`'s 4 status factories | `StatusEffect.CreateStatusEffect(type)` |
| `StatusEffectBase.Clone`'s `Activator.CreateInstance(GetType())` | each type with services overrides `Clone`; the base default stays for the 321 without |
| `CombatQueriesService.StatusEffectTypeHasCategory` (builds an effect to read `Categories`) | reads the categories the status effect catalog caches per type |
| `CharacterFileUpgrade.LoadMigrations`, `RecipeDefinitionLoader.LoadGame` (static) | take `IContentFactory` from their callers (`MigrationService`; tests and CraftSim) |
| `CraftMinigameRoutingService.DiscoverRoutes`, `RecipeParameterProfiles` | unchanged: routes and profiles reach no service |
| `GameGeneration.RequiredSchemaVersion` | builds each migration with `GetUninitializedObject` and reads `Version`, which is `public int Version => N;` in all 25 (a test keeps it so) |
| `ConversationMenuService.Start` | `_content.Create<ConversationMenuDefinition>(type)` |

**Static members and handlers on made types.** 21 static handler methods on 15 made types (8 view models, 2 status effects,
3 definitions, `DMChatCommand`, `CoxxionTerminalDialog`): one that reaches a service moves with its state to a service, or
without state to a singleton, renamed in place in the manifest with one `HandlerRenames.txt` line; one that reaches none
stays static. Static members of made types called from other files (16 server and 12 test sites in `Feature/`, 37 and 24 in
`Domain/`) move to a singleton or service, mostly with the state P5-16 moves. Static members only the class itself calls
just lose `static`.

## Creation sites that change

For the 795 types above:

| Where | Sites | Of them | Who converts |
|---|--:|---|---|
| Server, status effects | 52 | 30 from other code (10 in combat services), 22 self copies in `Clone` | tool |
| Server, other `Feature/` | 15 | `ExamineItemPayload` 5, `FieldEngineerPulseEmitter` 2, `TerminalItem` 2, migrations 5 (3 self copies), 1 other | tool; `TerminalItem` (target-typed, static list) by hand |
| Server, `Domain/` (P5-12) | 26 | recorder scopes, impact profiles, craft and fishing lifecycles and worlds, `AIContext` (8 self copies) | by hand, as collaborators |
| Server, by type | 32 lines | 26 discovery lines, 4 status factories, 2 view-model delegates | by hand (one line each) |
| `SWLOR.Game.Server.Tests` | 603 | ability definitions 245, status effects 146, GUI 60, `Domain/` types 59, perks 22, quests 19, chat commands 17, items 11, recipes 10, AI 10, menus 4; 366 in about 80 files with a container, 237 in about 70 without | tool |
| `EngineTests` | 87 in 42 files | status effects 32, GUI 24, ability definitions 11, chat commands 8, `AIContext` 8, items 2, ship modules 2 | tool |
| CraftSim, its tests, CombatSim tests, characterization | 14 | mostly perk and quest definitions | tool |
| `Activator` in tests and tools | up to 178, 13, 7 and 8 (plan: `Tests`, `EngineTests`, characterization, CombatSim) | definitions and effects built by type | tool where the type is a literal, else by hand |

Not counted, because the design makes them service-free or container-built: 96 `GuiWindowBuilder` and 2 `SpaceObjectBuilder`
sites, 611 builder creations, the 22 saved-character file objects P5-12 makes data, and the native sinks. The plan's 827 and
71 test sites counted every type that used the lookup; this table counts the types that will take services after those
moves.

The tool cannot convert, and lists: named arguments at a creation site (server 4, tests 7), object initializers (server 7,
all in `Domain/`; tests 26) and target-typed `new()` whose type it cannot resolve. It writes positional arguments exactly
as they were.

## Build order at boot and reload

1. `BuildServiceContainer`: every service, every content singleton (constructors only store dependencies, `HotReload.md`
   rule 9) and the content factory. No made content.
2. `LoadEngineTestAssembly` (test builds only): the bootstrapper builds the engine-test handler owners with the factory.
3. `LoadScripts`: handlers bind to services, content singletons and those owners.
4. Cache handlers: catalogs build definitions through the factory at the line that calls `Activator` today.
5. `mod_enter` and play: view models per window, menus per conversation, status effects per application, at today's lines.

**Reload validation** builds the candidate's container off the engine thread, now with the content singletons, and runs a
content check: each made type's constructors take registered types before their values, and each `Create` signature the
tool recorded resolves to exactly one constructor. It builds no made content.

**Native sinks** are bound after `BuildServiceContainer` and before `LoadScripts` on both entries and resolved from the
container (P5-12; `ServerBootstrapperOrderTests` pins it).

**`CompositionRoot.ServerHost`** and `IsBuilt` are removed in C11: `ServerManager` and `GameGeneration` create the host they
hand to `ServerBootstrapper`, each tool builds its own, and the 20 engine-test sites in 17 files that read
`ServerHost.Provider` take the service through their constructor and cast it.

## Hot reload carry

`StatusEffectService._creatureEffects` and `_loggedOutPlayerEffects` are `[ReloadState(Carried)]`; they hold
`CreatureStatusEffect` objects (service-free after the change above) and status effects. The 69 status effects that take
services are the only carried objects that do (`PlayerAura` and the rest of the carried state hold types and values).

**The design: a carry creator.** `ReloadCarryContractResolver` creates a game object with `GetUninitializedObject` today.
For a type whose constructor takes services, `ReloadWiring.CreateStateCarrier` gives it a creator instead: the content factory
builds the object through its values-free constructor (`Create<T>(type)`) with the new build's services, and the resolver
then writes every carried field from the text, as it does now. Service-typed fields are never written or read: the carrier
gets a predicate from the container's registrations ("a registered service or content singleton"), which the resolver and
`ReloadStateCarrier.Problems` use. The digests already treat a non-collection interface field as a dependency
(`ReloadDigestHasher.IsDependency`); the predicate adds class-typed singletons, which `ReloadDigests.DescribeFields` reaches
through the hasher.

**Why it keeps behaviour.** What the constructors of the 69 do today, measured: 22 calls in all (`Math.Max`, `Math.Abs`,
`Math.Clamp`, `AddCraftBonus` and `UpdatePotency`, which fill `StatGroup`, and `Stat.GetGrantedDeflectionStatType`, a pure
rule called directly after this change); no `throw`; each has a values-free constructor. `StatusEffectBase`'s constructor
makes a new `Id`, `StatGroup` and two lists. All of those fields are in the carried text and are overwritten, so after the
read the object equals the old build's field for field, as with `GetUninitializedObject`, and its services are the new
build's. One difference: a field the new build added, which the old build never wrote, starts at the constructor's value
instead of zero; `HotReload.md` records it.

The rejected alternative, carrying state and rebuilding each object through a restore method, would need a second
construction path in each of the 69 types.

**The checks:**

- `ReloadStateCarrierTests`: an effect with services round-trips; the text has no service field; every other field equals
  the original; the services are the new container's; an effect held in two sets is one object again.
- `ReloadStateCarrierTests.EveryCarriedTypeWithServicesBuildsWithoutEffects` (new): each carried type with services is built
  through its values-free constructor under the engine fake, records no engine call and does not throw.
- `EveryCarriedFieldHasACarriableType` keeps failing for a service field the predicate misses.
- `ReloadDigestTests`, and the live check (`scripts/run-hot-reload-check.sh --engine-filter HotReloadPlayers --reloads 2`),
  which gains a step: the headless player carries a `ForceDamageOverTimeStatusEffectBase` effect whose `Tick` calls services,
  and it ticks after each reload. Carried and adopted digests stay equal.

## Call text and visible dependencies

- **Naming.** Content reads services through private get-only properties named like the old `GameServices` property (or the
  class, for a content singleton). Services keep their `_camelCase` fields. The factory is `Content` in content and tests,
  and `_content` in services.
- **When the class already has the name.** 18 explicit `GameServices.X` sites in 7 types (the review counted 20):
  `FactionStandingReward.Faction` and `FactionPointsReward.Faction` (`QuestRewards.cs:159-196`), `GPReward.Guild`,
  `BankLayoutDefinition.Bank`, `EspionagePerkDefinition.Stealth`, `KeyItemDefinition.KeyItem`,
  `PropertyPermissionsViewModel.PlayerName`. There the property is `<Name>Service` (`FactionService.GetFactionDetail(Faction)`),
  and only these sites change text. The guard allows it only where the class or a base declares `<Name>`.
- **Shadowing.** A class member is found before a namespace or an imported member. The only `GameServices` names that are
  also type names in the scanned projects are `Slicing` and a test's nested `Area`.
- **No base class that exposes every service:** it would be `GameServices` again, hide each type's dependencies from P5-0
  and the domain guard, and need the whole container in every test.
- **More than 10 services (target 2):** counted per class as constructor service parameters, the factory and injected
  singletons counting one each. 649 of 656 server types that use the lookup reach 10 or fewer; the 7 over (`UsePerkFeat` 28,
  `GetDamageRoll` 21, `ResolveAttackRoll` 20, `CharacterSheetViewModel` 19, `DMChatCommand` 16, `CharacterChatCommand` 16,
  `WeaponAbilityProfile` 14) are split by P5-16 first; the tool refuses a type that would get more than 10.

## Tests and tools

**Measured.** Files using the lookup: `Tests` 180 (1,993 sites), `EngineTests` 236 by bare name through the global import
(5,300 sites; 78 name `GameServices`, the plan's 77), `CharacterizationTests` 10 (82), CraftSim 3 and its tests 5,
`IntegrationTests` 4, `SWLOR.Admin` 2, `SWLOR.Toolset` 2, `SWLOR.Toolset.Tests` 1, `SWLOR.CLI` 1, `SWLOR.CombatSim` 1.
`FacadeContainer` is used by 269 test files. Calls into service-reaching members of singletons: `EngineTests` 519 sites in
90 files on 38 `Feature/` classes (the review: 514, 86, 35), plus 37 sites in 16 files on `Native/`; `Tests` 234 sites in 32
files.

**The test API** is added to `FacadeContainer` in the pilot, and the class is renamed `TestContainer` in C10:

```csharp
public T Get<T>() where T : class;             // a service or content singleton of this container
public IContentFactory Content { get; }        // builds made content with this container's services
```

A fixture declares what its own code calls as properties of the same names, so test text matches content text:

```csharp
private FacadeContainer _container = null!;
private IStatService Stat => _container.Get<IStatService>();
private IContentFactory Content => _container.Content;

[Test]
public void A_flight_page_lists_the_ride()
{
    var dialog = Content.Create<StarportFlightsDialog>();
    var venom = Content.Create<VenomStatusEffect, int, int>(1, 1);
    // ...
}
```

**Tests without a container** (237 sites in about 70 files) never reach a service today, or they would fail. Their fixtures
get `private IContentFactory Content { get; } = TestContent.WithoutServices;`, a test-only factory that passes null for every
service parameter: the object behaves as it does today, and a service call fails as it does today. No container is built
for them.

**Engine tests.** The runner (`EngineTest.cs`) becomes made content taking `IContentFactory`. Its assembly loads after the
container, so `LoadEngineTestAssembly` builds each class there that declares an instance handler with the factory, and the
resolver `LoadScripts` hands `ScriptRegistry` answers those classes after the container's. `IsValidTestMethod` also accepts
public instance methods, whose class the runner builds with `Create` for each run; static tests run as before. A test file
converts (instance methods, constructor services) in the first wave that converts a singleton or made type it uses, or
earlier.

**A test-only `GameServices` copy is not an option:** besides the owner's decision, it could not serve a converted static
class under its own name, because an imported static property beside a type of the same name is error CS0229
(`MaintainabilityRefactorFacadeRemoval.md`, "Access pattern").

**Cost per wave.** The tool rewrites the test creation sites of the wave's types; a person handles named arguments,
initializers and multi-container fixtures (about 40 sites in all). The biggest test loads are C5 and C6 (256 ability
definition sites) and C7 (179 status effect sites).

**Characterization tests and the parity master build.** `MasterCopy.ps1` copies the characterization project into master's
tree, stubs `Support/FacadeServices.cs`, and runs `Apply-FacadeRemoval.ps1 -Reverse`. The new tool's `-Reverse` runs first on
that copy. The 7 `Activator` sites and the 1 `new` site of the definition sources use a support helper
(`Support/ContentServices.Create`), which `MasterCopy` stubs to `Activator.CreateInstance` and `new`.

**The master sync.**

- Order in `MechanicalScripts.txt`: `Apply-FacadeRemoval.ps1`, then `Apply-ContentInjection.ps1`, then the namespace and type
  moves. Master keeps writing `new X(...)`, `Activator.CreateInstance` and facade calls in content; the forward run converts
  them at every sync: a new service use becomes a property and a constructor parameter, a new `new X(values)` of a type with
  services becomes `Content.Create<X, ...>(values)` (and the creator gets `Content`), a new content file is converted whole.
  In code not converted yet, `Content` is the temporary `GameServices.Content`, so the text is the same before and after.
- The tool maps a name to its interface from `SWLOR.Architecture.Tests/ConvertedServices.json` plus
  `ContentRegistrations.cs`. After `GameServices` is deleted, the facade tool recognizes the branch by `Core/IContentFactory.cs`.
  The forward runs of both tools do nothing on a tree without it. `-Reverse` never checks for it: `MasterCopy.ps1` runs it on
  the characterization project copied into master's tree, which has no `Core/IContentFactory.cs`, and it must still turn
  the converted text back into master's form there.
- Mixed files (a master edit merged into a converted file) are converted in place; rewriter tests cover a new static method
  that reaches a service, a new service use, a new call to a converted singleton, a new creation of a converted type, a new
  handler and a new file.
- A conflicted file is taken from master and the pipeline rerun only when, at that moment, the branch's version equals the
  whole `MechanicalScripts.txt` pipeline's output on the merge base's version. The sync checks this on its master copy for
  each conflicted file instead of trusting a list.

**The round trip.** The parity copy compiles master's own content, so only the characterization project is reversed. For
each of its files that exists on master, `Reverse(Forward(master text)) == master text`: forward only adds fixture properties
and rewrites `new X(args)` to `Content.Create<X, A...>(args)` with the arguments as written, and reverse removes the
properties and turns `Content.Create<X, A...>(args)` back into `new X(args)`. Forms the tool refuses (named arguments,
initializers) are never rewritten, so they never need reversing. A rewriter test asserts the round trip on fixtures with
each creation form, and the sync gate runs `Forward` then `Reverse` over master's whole characterization project and
requires identical text.

**CombatSim:** builds its own container, takes `IContentFactory` once at its entry and calls `Create` at its 7 content
`Activator` sites. `WeaponImpactReader.ReadDirectCombatImpact` accepts both IL shapes during the waves (a static impact
reading `GameServices.get_Ability`, or an instance impact whose receiver is `ldarg.0` plus the definition's `get_Ability`,
with argument positions by `MethodInfo.IsStatic`); `WeaponLineTests` and the CombatSim readings pin it.

**CraftSim, CLI, Admin, IntegrationTests, Toolset:** build their own container, resolve at their entry point, and use the
container API in their tests. A tool entry point resolving its dependencies is a composition root; the service-location
guard lists those entries.

## Domain code (P5-12)

- **30 static helper classes** become services (or fold into one) under `DependencyInjection.md`, after the
  `EquipmentPredicates` and `WeaponDamage` loop is broken. Until C11 a temporary `GameServices` property of the old class name
  keeps unconverted callers compiling.
- **Rule objects (33)** become data; their checks move to a checker in the owning service.
- **Collaborators** (26 creation sites: recorder scopes, impact profiles, lifecycles, worlds, `AIContext`) take what their
  creating service passes; the 22 saved-character file objects become data.
- **`Native/`** static classes become singletons; native sinks are resolved from the container. The 16 engine-test files
  that call `Native/` singletons convert in P5-12.

## Guards

- `SWLOR.Architecture.Tests`, new `ContentInjectionRules`:
  - no method or property sets a service-typed member: constructors only;
  - a service or content singleton is never built with `new` or `Activator` outside `Composition/`;
  - a type whose constructor takes a registered type is built only by the factory, by a copy of itself, or as a private
    collaborator of the class that creates it;
  - service-typed members of content are private and named like the old property or class (or `<Name>Service`);
  - no two content singletons depend on each other, directly or through others;
  - no static field initializer reads a service;
  - nothing outside the entry points references a static `ContainerHost`;
  - `ContentRegistrations.cs` equals the content singletons (one instance per build) whose constructor takes registered
    types. Made content is never registered: the factory creates it, and it rejects any registered type.
- `ConvertedServicesTests.No_class_the_container_builds_uses_GameServices` covers content singletons too.
- P5-0's debt report counts lookups per file and services per type; each wave shrinks the baseline.

## Changes this design asks of other items

- **P5-2:** remove the 41 call sites in 18 `Domain/` types that call `Feature/` singletons.
- **P5-5 or C7:** move `StatusEffectBase`'s recorder calls into `StatusEffectService`.
- **P5-11:** move the appearance group with `TintMapService` and break its loop; `GuiWindowBuilder` records its event
  registrations for `GuiService`.
- **P5-12:** break the `EquipmentPredicates` and `WeaponDamage` loop; rule objects and `QuestDetail` to data plus checkers;
  collaborators; the 16 `Native/` engine-test files.
- **P5-16:** `WeaponAbilityProfile` as data plus a rider singleton (approved by the coordinator); leave the weapon bases few
  constructor dependencies; skip `CoxxionTerminalDialog`, which the pilot does.

## Pilot (P5-13)

**Conversations:** `Feature/DialogDefinition` (17 menus), `ConversationViewModel`, `ExamineItemPayload`,
`Feature/PlaceableScripts.cs`, `Feature/SnippetDefinition/ConversationStateSnippetDefinition.cs`, and the engine test
`StarshipDockReturnEngineTests`.

- **Made content by type, per use:** `ConversationMenuService.Start` builds a menu per conversation with
  `_content.Create<ConversationMenuDefinition>(type)`; the 17 menus take 1 to 6 services.
- **Made content with values:** `ExamineItemPayload(IItemService item, uint itemObject, bool trackLiveItem = false)`, created
  at 5 sites in 4 view models that are not converted yet: `Content.Create<ExamineItemPayload, uint>(item)` reaches the
  temporary `GameServices.Content`, and the factory fills the default. The one named-argument site is converted by hand.
- **View models through the factory:** `GuiService` builds every view model with `Create` (`GuiConstructedWindow.ViewModelType`);
  `ConversationViewModel` takes `Conversation` and `PlayerName` while its base keeps the lookup until C3.
- **Singleton with handlers:** `PlaceableScripts` (6 handlers, 8 services); its manifest lines stay.
- **A former static member called from another family:** `ConversationStateSnippetDefinition` takes `PlaceableScripts`;
  `SnippetService` builds it with `Create`.
- **A static handler and state on a made type:** `CoxxionTerminalDialog.LoadDoors` and its static `_areaDoors` move to a new
  service `Domain/World/CoxxionBaseDoorsService`, renamed in place with one `HandlerRenames.txt` line.
- **Engine tests:** the runner change, and `StarshipDockReturnEngineTests` converted.
- **The carry creator and predicate** with a test-only carried hierarchy (status effects wait for C7, because P5-5 works on
  their service).
- **Clear of the splits:** `ConversationMenuService`, `SnippetService` and `GuiService` are in no split item; P5-11 splits
  `GuiWindowBuilder.Build`, and the pilot only adds `ViewModelType` beside it.

The pilot also lands `IContentFactory`, `ContentFactory`, `TestContent.WithoutServices`, `ContentRegistrations.cs`, the content
check in validation, the temporary `GameServices.Content`, the container API on `FacadeContainer`, the tool (`-Check`,
`-Reverse`, ownership, cycles) with its rewriter tests, the `MechanicalScripts.txt` line, the `MasterCopy.ps1` step, the
guards, and the guide updates (`DependencyInjection.md`, `HotReload.md`, `CodingStandards.md`, `EngineTesting.md`).

Gate: the build; the test files that build the pilot types; `ContentFactoryAllocationTests` and a cost measurement of
`Create` against `new`; `ReloadStateCarrierTests`; `SWLOR.Architecture.Tests`; `CharacterizationTests` with snapshots
unchanged; the handler filters (`ScriptHandler`), with only the `LoadDoors` rename; a parity run; the tool's `-Check` clean
after `-Apply`, and a sync rehearsal on a master edit to a converted menu and a new `new ExamineItemPayload(...)`;
`RealBuildReloadTests` (compiled delegates in a collectible build); the engine tests that open menus and the examine window;
and client evidence of a converted menu, the conversation window, the examine window and a placeable script
(`InEngineVisualVerification.md`). The pilot records the cost per file and per creation site for the wave estimate.

## Waves (P5-14)

P5-14 starts when P5-2, P5-11, P5-12, P5-16 and every split have merged. A static class converts in the first wave in which
all its server callers can take it; a test file converts in the first wave that converts a type it uses. The creation sites
of a type change in the type's wave, in every file (unconverted creators use the temporary `GameServices.Content`).

| Wave | Content | Content files | Creation sites (server, tests) | Test and tool files | Hand work |
|---|---|--:|---|--:|---|
| C1 | Items, ship modules, snippets, chat commands, perks, recipes, property layouts, traps, AI, spawns | 72 | 0, about 90 | about 60 | 2 handler moves |
| C2 | Quests and migrations, with their static helpers | 67 | 5, about 25 | about 73 | the headgear loop; `WeaponDamageScales` |
| C3, C4 | GUI windows and view models (forwarding `IGuiService`), in two halves | 102 | 2, 84 | about 127 | 12 handler moves; `TerminalItem` |
| C5, C6 | Ability definitions and their forwarding constructors, in two halves | about 165 | 0, 256 | about 115 | the 4 status factories; the CombatSim reader |
| C7 | Status effects (69 with services), `StatusEffectBase`'s recorder move, `StatusEffectService` by-type creation, carry on | 69 | 52, 179 | about 133 | 4 handler moves; `Clone` overrides; the live reload step |
| C8 | Remaining singletons: top-level `Feature/` files, the 4 appearance classes outside P5-11's group, the 13 ability helpers | about 70 | 2, few | about 200 | `FieldEngineerPulseEmitter` as a collaborator |
| C9 | Engine-test files not yet converted | - | the rest of 87 | about 125 | `Framework/` helpers |
| C10 | Remaining unit tests, characterization, CombatSim, CraftSim, CLI, Admin, IntegrationTests, Toolset; rename `FacadeContainer` | - | the rest | about 330 | multi-container fixtures |
| C11 | Delete `GameServices` (with `Content`), `FacadeAccessor`, `FacadeCache<T>`, `CompositionRoot.Current`, `InstallForTests`, `ServerHost`, `IsBuilt`; flip the guards | - | - | - | the facade tool's branch detection |

Eleven pull requests after the pilot. The creation-site work adds about a quarter to the conversion cost the plan measured
(1 to 3 points of weekly usage per conversion); C5, C6 and C7 are at the top of that range. The pilot measures the real
cost. Each wave is Sonnet running the tool and doing the hand work, with an Opus review, the pilot's gate, and the engine
characterization scenarios for C5 to C8.

**Master freeze.** None is needed: the tool converts content files again at every sync, and owned files merge by rerunning
it. C7 changes the constructors of 69 status effects and creation sites in 30 files that master edits often, so it lands
directly after a daily sync, and its pull request lists the files master changed while it was open.

**What the tool does:** classify each class; find singleton groups that loop; add properties, constructors and forwarding
constructors; remove `static` where a service is reached; remove the `GameServices` import, prefix and self-qualification;
add injected singletons and `Content` to callers and creators; rewrite creation sites; regenerate `ContentRegistrations.cs`;
convert test fixtures and engine-test classes; reverse the characterization project; report what it refused.

**What it cannot do (it exits with the list):** break a cycle; move a handler, static member or state off a made type or
into a service; make a type data; split a type over 10 services; creation sites with named arguments, initializers or a
target type it cannot resolve; method groups over members that become instance members; review the 228 lambdas that use a
service and captured nothing, which capture `this` after the conversion (almost all are built once in a `Build*` method;
one evaluated per use allocates a delegate per call, so hot ones get a pin); multi-container fixtures; `Domain/` owner
lines.

## Risks

- *Larger objects.* A made object grows by 8 bytes per service it holds; status effects with services are created per
  application (`VenomStatusEffect` +32 bytes). No existing pin covers it; the new pins record the sizes.
- *Creation text.* Every creation of the 795 types becomes `Content.Create<T, A...>(...)` with the value types spelled out.
  Master's new code keeps compiling only because the tool rewrites it at every sync; a sync that skips the tool fails to
  build.
- *Tests without a container* get null services. A test that starts reaching a service fails with a null reference instead
  of `ContainerNotBuiltException`.
- *Scope added to other items* (the section above), mostly P5-11 and P5-12.
- *Compiled delegates in a collectible build* must not keep a retired build loaded; `RealBuildReloadTests` (25 reloads,
  zero retained contexts) proves it in the pilot.
