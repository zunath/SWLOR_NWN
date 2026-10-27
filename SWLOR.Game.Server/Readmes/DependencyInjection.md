# Dependency Injection

How the server's services move from static classes to instances built by a dependency-injection container, and how to convert one. The decisions are in `MaintainabilityRefactorSpecs.md` (Phase 2); this guide is the working recipe, with the pilot (`Achievement`, `Faction`, `Taxi`) as the example. Read it before converting a service.

Sections: The pattern · The container · Converting a service · Dependencies · Tests · Rules the architecture tests enforce · Pitfalls

## The pattern

A converted service is three pieces. Callers, content definitions and event handlers keep using the static class, so nothing outside the service changes.

| Piece | Where | What it is |
|---|---|---|
| Facade | `Service/<Name>.cs`, namespace `SWLOR.Game.Server.Service` | The static class every caller already uses. Same public members, same signatures. Each member forwards to the instance. It keeps the `[NWNEventHandler]` methods and the `const` fields. |
| Interface | `Domain/<Domain>/I<Name>Service.cs` | One member per public member of the old static class, with the doc comments. |
| Instance class | `Domain/<Domain>/<Name>Service.cs` | The old logic moved verbatim. Static state becomes instance fields. Dependencies come through the constructor. |

`<Domain>` is the domain the service is in `SWLOR.Architecture.Tests/DomainMap.json` (Core, UI, Combat, Abilities, Character, Items, Crafting, Economy, Quests, World, Space, Creatures, Companions, Social, Administration, Gameplay). The namespace is `SWLOR.Game.Server.Domain.<Domain>`, never `SWLOR.Game.Server.<Domain>`: a namespace named like a service (`Combat`, `Space`) or like an existing folder (`Core`) would capture simple names such as `Combat.CalculateHitRate()` in every file below `SWLOR.Game.Server`. Inside `Domain.*` code, a name that is also a domain (`Combat`, `Space`) still means the facade only when spelled `Service.Combat`.

The support types of the service (the enum, the attribute, records) stay where they are, in `Service/<Name>Service/`. Moving them would change the `using` of every caller; they move when the facades are removed (Phase 2, P2-Z).

The facade, as converted (`Service/Achievement.cs`):

```csharp
public static class Achievement
{
    private static IAchievementService Instance => FacadeAccessor.Resolve<IAchievementService>();

    [NWNEventHandler(ScriptName.OnModuleLoad)]
    public static void ReserveGuiIds()
    {
        Instance.ReserveGuiIds();
    }

    public static void GiveAchievement(uint player, AchievementType achievementType)
    {
        Instance.GiveAchievement(player, achievementType);
    }
    // ... one forwarding member per member of the interface
}
```

A `const` cannot forward, so the facade repeats it from the instance class: `public const int MinimumFaction = Domain.Quests.FactionService.MinimumFaction;` (spell the namespace: `FactionService` alone means the namespace `Service.FactionService` inside `namespace SWLOR.Game.Server.Service`). Compile-time constants are inlined, so callers (the toolset included) are unaffected.

## The container

Code lives in `Composition/`.

- `ContainerHost` builds a container once, with `ValidateOnBuild` and `ValidateScopes`, and creates every singleton during the build. A missing dependency, a lifetime mistake or a constructor that throws stops the boot, not the first handler that needs the service. A second build, or asking before the build, throws `ContainerNotBuiltException`.
- `CompositionRoot.ServerHost` is the server's container. `ServiceRegistrations` lists the services, one line each, grouped by domain. Every game service is a singleton (process lifetime; there are no scoped services in this phase).
- `FacadeAccessor.Resolve<T>()` is the one internal accessor facades use. Nothing else may resolve from the container (see the rules below).
- `ServerBootstrapper` runs a named step list (`Steps`). `BuildServiceContainer` comes after the internal systems (logging, the script execution provider) and before `RegisterEventHandlers`, `LoadEngineTestAssembly` and `LoadScripts`. Handlers exist only after `LoadScripts` registers them, and the engine cannot call back into the server before `Bootstrap` returns, so the container exists before any handler, `mod_preload` included. `LoadScripts` refuses to run without the container, and `ServerBootstrapperOrderTests` pins the order.
- A constructor must not call the engine or read other services' state: singletons are created while the server boots, before the module loads. Do engine-dependent setup in a handler (the facade forwards it), as the services always did.

Tests that need their own container build one with `ContainerHost.Create(services)` and make the facades use it for the current async flow with `CompositionRoot.InstallForTests(provider)`. Production code never calls it (`CompositionSourceGuardTests` fails if it does).

## Converting a service

One service per pull request. Each pull request also brings the service's own lines to at least 70% coverage.

1. Read the service, its folder, its callers (the service's edges in `SWLOR.Architecture.Tests/Baseline/CrossDomainEdges.txt`), its handlers (`Core/ScriptHandlerOrder.txt`) and its tests. Look for tests that recompile its source with Roslyn or reset its static state by reflection; they are replaced in step 7.
2. Create the interface and the instance class in `Domain/<Domain>/`. Move the logic and state verbatim: statics become instance fields, `static` goes off the methods (private helpers included), nothing else changes. A `const` the logic uses moves with it (the facade repeats a public one); a public `const` the logic does not use (`PlayerMarket.MaxListingsPerMarket`) stays on the facade. A public static property becomes an interface property, and the facade repeats it as a getter-only static property (`public static int MaxRank => Instance.MaxRank;`), which has no backing field. Keep strings that name the service (a GUI id reservation made under `nameof(Achievement)` stays the literal `"Achievement"`; do not reference the facade from the instance).
3. Turn the old file into the facade: keep the class, the handlers and the constants, forward every public member to `Instance`. Keep the doc comments.
4. Register the interface in `Composition/ServiceRegistrations.cs` (one line) and add the service, sorted by name, to `SWLOR.Architecture.Tests/ConvertedServices.json`.
5. Handlers stay on the facade. The instance class has no `[NWNEventHandler]`: the script registry binds handlers as static methods, and the handler order manifest keeps its names (`SWLOR.Game.Server.Service.Achievement.LoadAchievements`). Do not move a facade to another namespace or file path; a move can reorder reflection (`ScriptHandlerManifestBaselineTests` fails; delete that temporary test in the first pull request that legitimately reorders it, as its comment says).
6. If the old file made player messages, `Readmes/PlayerMessageAudit.json` lists them by file: change `File` (and `Line`) of those entries to the instance file. If `PlayerMessageAuditTests` names the old file in its list of gameplay-outcome files, name the instance file.
7. Write the unit tests (below) and measure coverage.
8. Build and run: the service's tests, `SWLOR.Architecture.Tests`, `SWLOR.Game.Server.CharacterizationTests` (snapshots must not change), the handler order tests (`ScriptHandlerManifestOrderTests`, `ScriptHandlerSeamTests`, `ScriptHandlerManifestBaselineTests`), and `scripts/ci/run-unit-tests.sh --out <dir>`. For a service with in-game behaviour also run the engine characterization scenarios (`scripts/run-engine-tests.ps1 -Filter Characterization`, `EngineTesting.md`).
9. Record the service, the pull request and the coverage before and after in `MaintainabilityRefactorStatus.md` (the coordinating session does this when it merges).

## Dependencies

| The service uses | In the instance class |
|---|---|
| Another converted service | Constructor-injected interface. |
| A service not converted yet (`Gui`, `Random`, `Messaging`, ...) | The static facade, as before. It converts later and keeps working. |
| `DB` | `IDBService` (`Domain/Core`), constructor-injected. Until `DB` itself converts it is implemented by `Composition/StaticDBAdapter`, which forwards to the static `DB` one to one. `IDBService` has only the members converted services have needed (`Get<T>`, `Set<T>`, `Delete<T>`, `Exists<T>`, `Search<T>`, `SearchCount<T>`); add a member with the same signature as on `DB` when you need it, to the adapter and to `InMemoryDBService`. When `DB` converts, `IDBService` becomes its interface and the adapter is deleted. |
| The engine (NWScript, NWNX) | Bare calls through the global static imports, as everywhere. Tests swap the engine with `NWScriptRuntime.Override` and the plugin runtimes (`PlayerPluginRuntime.Override`, ...). Do not inject `INWScript`. |
| Native engine objects (`NWNXLib.g_pAppManager`, `CNWSPlayer`, ...) | Not seamed yet. Put each read of native data in a small `protected virtual` method of the instance class whose body is the original code, and let a test subclass answer it. Return a managed value (a record struct, an `int?`) instead of the native object when the rest of the logic needs only a few fields (`ChatPortraitTellsClient`). Keep the native read to the smallest piece that cannot run without a server; the logic around it stays testable. Do not add an interface and a registration for it: `Every_domain_service_in_the_container_is_listed_as_converted` counts every registered `Domain.*` type as a service. The same holds for a call into a service that is not converted yet whose static code reads the database or the native engine for a real player (`Perk.GetPerkLevel`, `StatusEffect.ApplyStatusEffect`): wrap just that call in a `protected virtual` method of the instance class, with the original call as its body (`TrapsService.CanUseTrapTier`, `TrapsService.ApplyBleed`, `MusicService.HasKeyItem`, `WeatherService.GetProtectedDamage`, `FishingService.GiveAgricultureXp`), and let a test subclass answer it. Leave the wrapper when the dependency converts. |
| Time, randomness | `Clock` and `Random` as before (`Clock` only in the files the clock seam script owns). A service that compares against `DateTime.UtcNow` and registers `Scheduler` tasks (`ShuttleService`) keeps those calls but puts each behind a one-line `protected virtual` method (`UtcNow()`, `Schedule`, `ScheduleRepeating`) whose body is the original call, so a test can set the time and run the repeating tick and the delayed tasks itself (`ClockedShuttleService` in `Tests/Support/ShuttleBench.cs`). A test of rolls seeds the shared generator (`Random.SetSeed`, `Random.ResetSeed` in `TearDown`) and replays the draws with `System.Random` in the order the service makes them, which also pins their order and number (`FishingServiceTests`). |

Never inject `IServiceProvider` or take the container in a constructor. Dependencies that form a cycle between two converted services cannot be injected both ways: break it by moving the shared part into a third service, or leave one direction on the static facade and say so in the pull request.

## Tests

- **The instance**: `Tests/Domain/<Domain>/<Name>ServiceTests.cs`. Construct the instance with fakes: `InMemoryDBService` for `IDBService` (entities by id, records every `Set`), `PlayerEngine` (a `FakeNWScript` where the target is a player with a UUID; `Messages()` returns what was sent to the player), `FakePlayerPlugin` and the other plugin fakes for NWNX calls. Assert the saved entities, the messages and the engine calls. `InMemoryDBService` cannot evaluate a query: `Search` and `SearchCount` answer with every stored entity of the type and record the query in `Queries`, so store only what the query would match and assert on the recorded query text. Code that runs several searches over one entity type (the contract board's boot sweep) uses `QueryableDBService` (`Tests/Support`), which wraps an `InMemoryDBService` as `Store` and answers a search with the entities whose indexed properties match the query (exact values, lists of numbers and numeric ranges). A call into a service that is not converted yet (`Item`, `Perk`, `Quest`, ...) runs its real static code, so it works only where that code needs nothing but the engine fakes; code that needs the native engine (`Item.FitsInInventory`) or the database (`Perk.GetPerkLevel` on a player) is left to the in-engine tests, and a DM answer from the engine fake stands in for a player with no perks. The pilot and Economy test classes are the examples.
- **The facade**: nothing to write. `FacadeForwardingTests` reads `ConvertedServices.json` and proves for every listed service that the facade and the interface have the same members, that each facade member (and each static property getter) makes exactly one call on the instance with the same arguments and returns its result (`RecordingProxy` answers `true`, `4242`, `4.5f` or the second value of an enum for value types, so a facade that returns a default fails), that the container holds one instance of the instance class, that the instance has no handlers, and that `ConvertedServices.json` lists exactly the services `ServiceRegistrations` registers (apart from `IDBService`). A facade member the test checks is a public or `internal` static method (native code calls an `internal` one, such as `ChatPortraitTells.RecordChatLine`) or a public static property getter.
- **Local variables**: the engine fake only records `SetLocalInt` and friends. `Tests/Support/LocalVariables` gives a `FakeNWScript` real local variables (what the code sets is what the matching `GetLocal...` answers until a `DeleteLocal...`); clear the fake's recorded calls through its `ClearCalls`. `Tests/Support/LoadedPlanets` builds a planet instance with its cache filled by the module handler, for tests of the area-to-planet rules.
- **Code that reaches a facade without a server**: a unit test of other code (a quest definition that names a guild, a migration that names a market category) calls the static facade, which needs a container. Give the fixture a `FacadeContainer` (`Tests/Support`): build it in `[OneTimeSetUp]` (call a cache handler such as `Guild.LoadData()` inside `using (container.Install())`), install it in `[SetUp]` and dispose that scope in `[TearDown]`. Without one the call throws `ContainerNotBuiltException`. This includes the tests of another converted service that calls the facade of this one (the `PlayerList` tests reach `HoloCom.GetCallState`), and a fixture of an instance that calls a facade of a service that converts later (the `HoloCom` and `Traps` fixtures install one for `DMPresence`).
- **Roslyn harnesses that borrow a converted service**: a test of code that is not converted yet (the chat routing, the DM toolbar handlers) can compile the service's methods into its own stand-in static class, with `Method("Service/<Name>.cs", ...)`. Once the service is converted that method is a forwarder, so point the harness at the instance class: `InstanceMethod("Domain/<Domain>/<Name>Service.cs", ...)` and `InstanceField(...)` (`DMPossessionHarness`) return the member as a static one, and the stand-in declares static fields for what the instance injects (`_db`, `_presence`) with one-line shims to its own stand-ins. A test whose subject is the converted service itself becomes an ordinary instance test instead: `DMPossessionEngine` (`Tests/Support`) is the harness's model of DM possession behind the engine seams, and `FakeJsonEngine` gives a `FakeNWScript` working `Json` values and templates.
- **Coverage**: at least 70% of the service's lines (the facade, the interface and the instance class). Measure with the repository's settings, which exclude generated code:

  ```bash
  dotnet test SWLOR.Game.Server.Tests --no-build -c Release --collect "XPlat Code Coverage" --settings coverlet.runsettings --results-directory <dir>
  ```

  then read `line-rate` per class in the `coverage.cobertura.xml` it writes (or run all projects with `scripts/ci/run-unit-tests.sh --out <dir>`, which merges them). Report the number before the conversion and after it.
- **Behaviour must not change**: `SWLOR.Game.Server.CharacterizationTests` snapshots stay byte for byte; do not regenerate them.

## Rules the architecture tests enforce

`SWLOR.Architecture.Tests` (`ConvertedServicesTests`) reads the compiled server and fails with a message naming the type and member:

- **The pieces exist.** A listed service has the static class `Service.<Name>`, the interface `Domain.<Domain>.I<Name>Service` and a class `Domain.<Domain>.<Name>Service` that implements it, and the domain agrees with `DomainMap.json`.
- **No static state.** No type of a converted service (the facade, the interface, the instance class, the types in `Service/<Name>Service/`) holds a static field. Only `const` is allowed; `static readonly` is not, because it still shares the collection it holds.
- **Handlers stay on the facade.** A `[NWNEventHandler]` on any other type of a converted service fails.
- **The instance never calls its own facade.**
- **No service location.** Nothing outside `Composition/` uses `IServiceProvider` or any `Microsoft.Extensions.DependencyInjection` type, and nothing outside `Composition/` and the facades of converted services uses `FacadeAccessor`.
- **Domain types name their service.** A type in `Domain.<Domain>` is classified as the service of that domain whose name starts its name (`IAchievementService`, `AchievementService` and `AchievementNotification` are all the service `Achievement`), so the cross-domain baseline (`CrossDomainEdges.txt`) is the same before and after a conversion. A type that names no service of its domain shows up as a new `Platform/Domain.<Domain>` edge.

The composition root (`Composition/`) names every service it registers, so its outgoing edges are left out of the cross-domain guard (`wiringPlatformAreas` in `DomainMap.json`).

## Pitfalls

- **Name clash.** `Service.<Name>Service` is a namespace (the support types) and `<Name>Service` is also the instance class. Inside `namespace SWLOR.Game.Server.Service` the simple name means the namespace; the facade refers to the interface only.
- **Package.** `Microsoft.Extensions.DependencyInjection` 9.0.9 is the version the toolset already uses (`SWLOR.Toolset.csproj`); `SWLOR.Game.Server/packages.lock.json` and the lock files of the toolset projects that reference the server record it. Change the version everywhere at once, then `dotnet restore <project> --force-evaluate` (add `-p:Configuration=Release` for the `packages.Release.lock.json` files) for the server and each toolset project.
- **Test projects see `Domain` as a namespace.** A test namespace `...Tests.Domain.<Domain>` is fine; spell the production types with a `using SWLOR.Game.Server.Domain.<Domain>;`.
- **Engine structures.** Production code that makes a real engine structure (`new Location(IntPtr.Zero)`) crashes a unit-test process when the garbage collector finalizes it, because the finalizer calls the native library. Fakes avoid this (`EngineStructures.Create`); for a structure the service makes itself, put the construction in a `protected virtual` method and override it in the test (`ChatCommandService.CreateEmptyLocation`).
- **Shared statics the instance reaches.** A converted instance still calls services that are not converted. Their static state is shared between tests: `Scheduler.Schedule` leaves an item that another test's `Scheduler.Process()` runs against whatever engine is installed then (drain it with `Scheduler.Process()` in `TearDown` while your fake is installed), and `Authorization.GetAuthorizationLevel` and `Disguise`/`PlayerName` lookups of a real player read the static `DB`. A test that must pass them uses an object the fake engine reports as a non-player, or `SWLOR_SUPER_ADMIN_CD_KEY` set for the test to the CD key the fake returns.
- **Engine tests that called private statics by reflection** (`typeof(Party).GetMethod("AddToParty", ...)`) reach the instance through `EngineTests/Framework/PartyCaches.cs`, which reads `CompositionRoot.ServerHost.Provider`. Tests outside the server assembly may do that; the architecture rule is about the server assembly.
- **Offline callers.** A pure method of a converted service (`PlayerMarket.GetBaseItemMarketCategory`, which an offline migration calls) still goes through the facade and so needs the container. The server's boot builds it before any handler; tests install a `FacadeContainer`.
- **Private constants and tests that read them.** A test that reflected on a private member of the old static class (`Bank.StorageId`) follows it to the instance class; a test that read the old file's text (`LeadershipCivicPerkRegressionTests` reading `Guild.cs`) follows the code to `Domain/<Domain>/`.
- **Members the unit tests cannot reach.** A call into a not-yet-converted service that needs the native engine or the static database cannot run offline: `Gui.TogglePlayerWindow` (its window table), `Space.IsPlayerInSpaceMode` (`DB.Get`), the `NWNXLib` structures that `DMPresence.GetIsManifested` reads, and the player list `Area` fills in an event handler. The instance tests leave those lines to the in-engine tests, and the window logic around them stays covered by the harness tests of the code that shows the windows (the DM toolbar, the emotes window) through `InstanceMethod`.
- **Out parameters and engine structures on a facade.** `RecordingProxy` writes a default into an `out` value-type parameter (an `out bool` otherwise fails the proxy's write-back) and suppresses the finalizer of an engine structure it returns (`Json`, `Location`), which would crash a process with no engine.
- **A support type with handlers or static state in the service folder.** The architecture rules apply to every type that `UnitClassifier` puts in the service, and `Service/<Name>Service/*` is the service `<Name>` by namespace. A static class there that keeps `[NWNEventHandler]` methods or static fields (`Service/QuestService/QuestEncounter.cs`, a static class with five handlers and two static dictionaries) fails `Event_handlers_of_converted_services_stay_on_the_facade` and `Converted_services_hold_no_static_mutable_state` as soon as its service is listed, and its handler names are pinned by the manifest. Decide how such a type is converted before converting its service (the `Quest` conversion is blocked on this).
- **One support folder for several services.** The eight DM services share `Service/DMToolService/` (the unit `DMTool` in `DomainMap.json`): its pure helpers (`DMSearchText`, `DMFavoriteList`, ...) are not services and stay static; the instance classes call them directly.
- **A facade that was not a static class.** `Area` was a non-static class with only static members; the facade must be a static class (`ConvertedServiceRules`), which no caller notices. A `Dictionary` the old class handed out by reference (`Area.GetAreas()`) is still the instance's own dictionary.
- **A nested public type or a static catalog in the old class.** `Music` was a class with a nested `Song`; the facade is a static class and the instance must not name it, so `Song` moved to `Service/MusicService/Song.cs` (nothing named `Music.Song`; callers only used `var`). `MusicSongCatalog.Songs` was a static auto-property, which the no-static-state rule counts (its backing field), so the catalog became a class that the service builds once (`new MusicSongCatalog().Songs`, kept in an instance field). Code outside the service asks the facade: `KeyItemIcon` calls `Music.TryGetCollectibleSong`, so `KeyItemIconTests` installs a `FacadeContainer`.
- **Facade cost.** Every facade call resolves the instance from the container (one dictionary lookup). Fine for services that run per event; a hot-path service can cache the instance once the container's lifetime per reload generation is settled (Phase 4).
