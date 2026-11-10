# Maintainability Refactor: Facade Removal

The plan for item P2-Z (remove the static facades of the converted services), from item "P2-Z plan" on 2026-11-08. Status lives in
[MaintainabilityRefactorStatus.md](MaintainabilityRefactorStatus.md) (the coordinator adds rows P2-ZA to P2-ZZ). This file goes away at cutover.

Every number comes from the Release build of the branch at `7cbed1077`, before P2-CB10: an IL scan of every assembly that references the
server (each `call`, `callvirt`, `ldftn` and `ldtoken` of a facade member, counted once per instruction and charged to the outermost type
that contains it), reflection over the facades, and a source scan for what IL cannot show (constants are inlined, `typeof`, `cref`).
`Combat` is counted as it is now: one facade in 17 files with 294 members; P2-CB10 merges the files and leaves about 291 forwarders.

## Decisions (recommended; the open ones are at the end)

- **Converted callers take the interface by constructor.** A constructor cycle takes `Lazy<IXService>` instead, from one open-generic
  registration in `Composition/`; a test allows `Lazy<>` only where the direct parameter would make a cycle.
- **Static code uses one accessor, `Composition/GameServices`,** a public static partial class with one property per removed facade
  (`public static IStatService Stat => FacadeAccessor.Resolve<IStatService>();`). Callers import it with
  `using static SWLOR.Game.Server.Composition.GameServices;`, so `Stat.GetStat(...)` keeps its text and now calls the interface. The four
  facade names that are also namespace segments (`Combat`, `Mimicry`, `Performance`, `Prospecting`) are always written
  `GameServices.<Name>.`.
- **Handlers move to the instance class, same method name.** The script registry binds them to the container's singleton. Each moved
  handler is renamed in place in `Core/ScriptHandlerOrder.txt` and gets one line in `tools/Refactor/HandlerRenames.txt`.
- **Constants move to the class that already holds the value** (the facade's initializer names it, for example `CombatRules.MaximumHitRate`
  or `Domain.Quests.FactionService.MinimumFaction`). A facade constant with a literal value moves to the instance class first.
- **One re-runnable tool does the caller edits:** `tools/Refactor/SWLOR.FacadeRemoval` (C#, because isolated agents cannot run
  `powershell.exe`), with a thin `Apply-FacadeRemoval.ps1` wrapper listed in `MechanicalScripts.txt`, and its data in
  `tools/Refactor/RemovedFacades.json`.
- **Order:** P2-ZA, then the two injection items, then the removal items by domain (up to seven at once), `Combat` last, then P2-ZZ, then
  P2-Z2 in its quiet window.

## Inventory

133 static facades: the 120 main facades of the 119 listed services and `Combat`, plus 13 further facades. They hold 2,302 members
(public or internal static methods and property getters, counted as `FacadeForwardingTests` counts them), 237 public constants, 364 handler
registrations on 339 methods (of 711 registrations in the server), and 18,375 source lines.

Call sites outside the facade itself (IL instructions):

| Caller | Sites | Types | What it is |
|---|--:|--:|---|
| Converted instance classes (`Domain.*`) | 684 | 87 | 418 inside `protected virtual` wrappers; 251 caller-to-service pairs |
| Support types of converted services (`Service.<Name>Service.*`) | 412 | 82 | static helpers, records, sessions |
| `Combat` facade to other facades | 14 | 1 | `Stat`, `StatusEffect`, `TemporaryStatModifier`; P2-CB10 moves them into `CombatService` |
| Unconverted static code in `Service/` | 159 | 9 | `Space` 99, `Droid` 33, `QuestContract` helpers 12, `PlayerShipAccess` 5, `AnimationPreviewCatalog` 5, `DroidInstructions` 4, `ServerRestart` 1 |
| `Feature/` | 4,532 | 544 | `GuiDefinition` 1,765 (96), `AbilityDefinition` 894 (180), top-level 726 (49), `StatusEffectDefinition` 239 (63), `QuestDefinition` 213 (35), the rest 695 (121) |
| `Native/` | 209 | 12 | `ResolveAttackRoll` 64, `GetDamageRoll` 61, `OnAIActionAttackObject` 39, `PairedRangedAttackCycle` 16, `WeaponAttackCycle` 10, 7 others 19 |
| `Entity/`, `Core/` | 3 | 3 | `Beast` and `IncubationJob` constructors (`Combat.CreateDefaultDefenseValues`), `ManagedEntryDepth` (`CombatRecorder.IsRecording`) |
| `ConversationData/` | 0 | 0 | the graphs are JSON |
| Other projects | 6 | 3 | `SWLOR.Admin` `DB.Load`; `SWLOR.CLI` `DB.Load`, `Delete`, `Search`, `SearchCount`; `SWLOR.CraftSim` `Guild.LoadData`. `SWLOR.CombatSim`, `SWLOR.Toolset*` call none |
| Unit test projects | 2,198 | 200 | `Game.Server.Tests` 2,045 (177), `CharacterizationTests` 126 (13), `IntegrationTests` 16 (4), `CraftSim.Tests` 10 (5), `Toolset.Tests` 1 |
| `EngineTests` | 5,108 | 193 | |
| **Total** | **13,325** | | server 6,013, other projects 6, tests 7,306 |

What IL does not show, from the source: 820 reads of facade constants (776 by simple name in 192 files; `SWLOR.CombatSim` 5,
`SWLOR.CraftSim` 2, `SWLOR.Toolset.Domain` 7 qualified reads of `Faction.MinimumFaction` and `MaximumFaction`); 316 qualified
`Service.<Facade>.` uses (server 195 in 39 files, the `Domain.*` interim style); 76 `typeof(<Facade>)` (tests 75, `SWLOR.CombatSim` 1, none in
production code); 75 `cref`s; 15 method-group conversions of a facade member in server code (view models and two definitions).

By domain (Instance = sites in converted classes; Static = every server site outside `Domain.*` plus other projects):

| Domain | Facades | Members | Handlers | Facade lines | Instance | Static | Static caller types | Tests |
|---|--:|--:|--:|--:|--:|--:|--:|--:|
| Abilities | 8 | 156 | 21 | 1,053 | 46 | 444 | 175 | 748 |
| Administration | 8 | 44 | 1 | 615 | 30 | 165 | 25 | 36 |
| Character | 16 | 435 | 39 | 3,230 | 259 | 1,302 | 312 | 2,151 |
| Combat | 24 | 693 | 66 | 4,427 | 186 | 898 | 198 | 2,530 |
| Companions | 3 | 60 | 27 | 469 | 4 | 84 | 24 | 50 |
| Core | 16 | 111 | 32 | 1,134 | 24 | 1,021 | 216 | 809 |
| Crafting | 12 | 115 | 13 | 1,026 | 9 | 217 | 37 | 72 |
| Creatures | 4 | 76 | 32 | 613 | 20 | 43 | 11 | 61 |
| Economy | 6 | 47 | 7 | 445 | 0 | 66 | 21 | 96 |
| Gameplay | 1 | 4 | 0 | 32 | 0 | 5 | 1 | 0 |
| Items | 8 | 104 | 15 | 1,027 | 41 | 369 | 107 | 208 |
| Quests | 5 | 80 | 17 | 803 | 2 | 85 | 23 | 73 |
| Social | 8 | 118 | 35 | 955 | 10 | 95 | 23 | 136 |
| UI | 2 | 65 | 17 | 637 | 40 | 319 | 90 | 155 |
| World | 12 | 194 | 42 | 1,909 | 13 | 222 | 45 | 181 |

Appendix A has every service.

## Access pattern after a facade goes

**What a call costs today.** Since P2-D0 a facade call is not a dictionary lookup: `FacadeCache<T>` keeps one static slot per service and
compares its container with `CompositionRoot.Current` by reference. `FacadeCacheTests.Measure_facade_call_cost` on this machine
(Release, 2026-11-08): direct instance call 3.69 ns, facade on the server container 7.73 ns, facade with a test container installed
19.41 ns (the `AsyncLocal` read). No allocation: the allocation tests pin facade reads at 0 bytes. (The "Facade cost" pitfall in
`DependencyInjection.md` still says "one dictionary lookup"; P2-ZZ corrects it.)

| Option | Cost per call | Risk | Diff and syncs |
|---|---|---|---|
| Convert the caller (constructor injection) | 3.7 ns | constructor cycles; test benches gain parameters | right for the 87 converted classes; for `Feature/` it needs container-built definitions and view models (`AbilityService` and `GuiService` build them with `Activator.CreateInstance`), 544 types, and every master edit to them becomes a hand port; impossible for `Native/` (static `UnmanagedCallersOnly` hooks) before the Phase 4 `NativeSinks` |
| `FacadeAccessor.Resolve<T>()` at the call site | 7.7 ns, as today | spreads the internal accessor to 6,000 sites; breaks the rule that only `Composition` resolves | every call site changes; every master edit to such a line conflicts |
| Keep the facade for that area | 7.7 ns, as today | none new | the forwarding layer and `FacadeForwardingTests` stay; `Domain.Combat` keeps hiding `Service.Combat` (and three more), so P2-Z2's guard cannot pass; the specs say delete them |
| Thin static accessor, `using static` (**recommended**) | 7.7 ns, as today (the same `FacadeCache` read) | a type and a property of one name in scope is error CS0229, so the property is added in the pull request that deletes the facade (checked with a probe compile); a namespace of the same name hides the property, hence the explicit form for four names | one `using static` line per caller file; call text unchanged, so master's edits merge |

**Hot paths.** The three per-swing hooks hold 164 facade call sites (`ResolveAttackRoll` 64, `GetDamageRoll` 61,
`OnAIActionAttackObject` 39). The accessor does exactly what the facade did, so the per-swing cost does not change; even if every one of
those sites ran on each swing, constructor injection would save at most about 0.7 microseconds (164 x 4 ns). Converted classes on the
attack path (`Stat`, `StatusEffect`, `Ability`, the `Combat` parts) become faster: an injected field instead of a resolve. Per-tick
handlers become delegates bound to the instance and skip one resolve per call. Nothing new allocates, except the 15 method-group
conversions: a group over an instance method allocates a delegate each time it runs, where a static group was cached. They are in view
models and definitions, not on the attack path; the item that owns each rewrites it as a lambda (cached) and the gate checks it.

**What each kind of caller uses after P2-Z:**

| Caller | Uses |
|---|---|
| Converted instance classes and parts | constructor injection; `Lazy<IXService>` for the 259 sites (67 pairs) where the direct parameter would make a cycle (161 sites to other services, 98 to `Combat`); existing `protected virtual` wrappers stay and call the field, so the test seams do not change |
| Support types, `Feature/`, `Native/`, unconverted `Space`, `Droid` and helpers, NWScript entry points (`[NWNEventHandler]` on static `Feature` classes) | `GameServices` through `using static` |
| `Entity` constructors | `CombatRules.CreateDefaultDefenseValues`, as `Player` already does (no container needed when the database layer builds entities offline) |
| `SWLOR.CLI`, `SWLOR.Admin`, `SWLOR.CraftSim` | `GameServices` after they build or install the container, as they do today; `SWLOR.Admin` and `EngineTests` already import `Service` globally and get a global `using static` |
| `SWLOR.CombatSim`, `SWLOR.Toolset.Domain` | constants from their owners (`CombatRules`, `FactionService`); `CombatSim`'s IL reader (`WeaponImpactReader`, `typeof(Ability)`) learns the new call shape |
| Tests | the same accessor; fixtures keep `FacadeContainer` (the accessor resolves from `CompositionRoot.Current`, so installed containers still work) |

**What the specs already decide.** "Replace static calls in content code with injection where the caller is itself converted, delete
facades area by area" (P2-Z bullet); handlers move to instance classes with manifest names updated in place, through `HandlerRenames.txt`
("Renamed handlers"); "Never inject `IServiceProvider`" (`DependencyInjection.md`), which the `Lazy<>` registration respects because only
`Composition/` touches the provider; "only facades may resolve from the container (through one internal accessor in `Composition/`)",
which holds in letter because `GameServices` lives in `Composition/`; `Random`, `Log` and `FrameProfiler` stay static; pure utilities
(`CombatRules`, `StatRules`, `StatusEffectRules`) stay static and callable directly. The plan's parity rule ("facades keep today's API until
the very end, so one output-locking test project compiles against both") ends here, so P2-ZA adapts the parity copy (below).

## Handlers

- **Registry (P2-ZA).** `ScriptRegistry` keeps binding static methods and also binds `[NWNEventHandler]` on public instance methods
  declared (`DeclaredOnly`) on a class the container registers, as `CreateDelegate(typeof(Action), instance)` (or the conditional delegate)
  on the singleton. The instance comes from a resolver `Composition` passes in through `ServerBootstrapper.LoadScripts`, so `Core` never
  touches the container. A handler on an unregistered type stops the boot with a message; a test double that subclasses a service in
  `EngineTests` never registers a second copy. Names keep the formula `DeclaringType.FullName + "." + Name`.
- **Per item.** `FacadeForwardingTests` already proves every facade handler is parameterless and forwards to the instance member of the
  same name, so the move is: the attribute goes from the facade method to that instance method; the manifest line keeps its position
  with the new name (`SWLOR.Game.Server.Service.Taxi.X` becomes `SWLOR.Game.Server.Domain.World.TaxiService.X`); a
  `<master name> <branch name>` line goes in `HandlerRenames.txt` (the 9 handlers already listed there, of `Craft`, `CompanionControl` and
  `TemporaryStatModifier`, get their branch side updated instead of a second line); the two handler snapshots
  (`Tests/Core/Snapshots/ScriptHandlerOrder.txt`, `.EngineTests.txt`) are regenerated; the parity run reports no handler-order difference.
  Each of Combat's 8 handlers goes on the class its facade method forwards to since P2-CB10: three on `CombatService` (death,
  exit, 6 s), five on the owning part (`CombatDamageCatalog` 2, `CombatActivity` 2, `CombatDamageTaken` 1).
- **Counts:** 364 registrations on 339 methods (19 methods carry two attributes and 2 carry four), per item in the item table and per service in
  Appendix A. Core's 32 are renamed again by P2-Z2 (`Domain.Core` to `Domain.Foundation`); its script already updates the branch side
  of existing rename lines.

## Guards and tests

- **`ConvertedServices.json`:** a per-service flag `"facadeRemoved": true` during the transition (decision D7). P2-ZZ removes the flag and
  the facade concept: extras that were further facades become plain classes of the service.
- **`ConvertedServicesTests` and `ConvertedServiceRules`:** for a removed service, "the pieces exist" requires no `Service.<Name>` type and a
  `GameServices.<Name>` property of the interface type; "handlers stay on the facade" becomes "handlers are on the instance class (or a
  listed extra's class)"; "an instance never calls its own facade" becomes "a registered class never uses `GameServices`"; service location
  allows `FacadeAccessor` only in `Composition/`. New rule fixtures in `SWLOR.Architecture.Tests/Fixtures` for each.
- **`FacadeForwardingTests`:** the forwarding cases skip removed facades (there is nothing to forward); a new case proves each `GameServices`
  property returns the installed container's instance; the handler case flips (the instance keeps the handlers the facade had). The
  container cases (one singleton, registrations equal the list) stay. P2-ZZ deletes the forwarding cases.
- **A cycle test** (`Tests/Composition`): every `Lazy<IXService>` constructor parameter would make a constructor cycle if it were direct.
- **Cross-domain guard:** callers now reference `I<Name>Service`, which `UnitClassifier` puts in the same unit as `Service.<Name>`, and
  `GameServices` is a `Composition` wiring area, so `Baseline/CrossDomainEdges.txt` must not change (a per-item check);
  `EdgesIntoCoreAndPlatform.txt` (information only) gains `Composition` edges and is regenerated. `UnitClassifier` itself needs no change
  until P2-Z2, which made every type of `Domain.<Domain>` the one unit `Domain/<Domain>` (the support namespaces it moved there hold
  no service name).
- **Allocation tests** (7 files: `Ability`, `Perk`, `Stat`, `StatusEffect`, `Combat`, `CombatHelper`, `TemporaryStatModifier`
  `FacadeAllocationTests`) compare the accessor path with the instance instead of the facade, at the same byte counts.
- **Code that reads facade source by path:** 13 files, 12 of them tests (`CombatSource`, `CombatSourceTests`, `AIModelTests`, `ClockSeamSourceGuardTests`,
  `MoveSupportNamespacesScriptTests`, `LightsaberWorkbenchTests`, `PlayerFacingNameBroadcastTests`, `EspionageSystemTests`,
  `ForceSensitiveEspionageTests`, `CraftingStyleSelectionTests`, `PerformanceAudienceCostTests`, `PlayerMessageAuditTests`, `SplitCombat.cs`).
  About 20 more find handlers on a facade by reflection; they follow the handler to the instance.
- **Manifests:** `ClockSeamFiles.txt` has 10 entries for facade files (`Combat`, `CombatParts/**`, `CombatPoint`, `StatusEffect`, `Stat`, `Perk`, `Craft`,
  `Recast`, `AbilityCooldownVisual`, `Enmity`): they stay, marked master-only, because the parity copy and the sync still rewrite master's
  versions, and `ClockSeamSources.ListFiles` stops asserting that a master-only entry exists on the branch. `PlayerMessageAudit.json`
  has 13 rows on 4 facade files (`PlayerFeedback` 5, `PvpCombat` 3, `Death` 3, `Messaging` 2).
- **Test files that call facades:** 393 types (unit 200, engine 193) with 7,306 sites; 75 `typeof(<Facade>)` (most are `.Assembly`
  anchors or constant snapshots); 482 constant reads.
- **Parity (P2-ZA):** `MasterCopy.ps1` already stubs `Support/FacadeServices.cs` in the copied characterization project. It also runs the
  tool's `-Reverse` mode on that copy: drop the `using static ... GameServices;` lines, turn `GameServices.<Name>.` back into
  `<Name>.` and map moved constants back. The four constant snapshots that reflect over a facade (`typeof(Combat)`, `Stat`, `Skill`,
  `Craft`) become explicit lists the reverse map can translate, with byte-identical output.

## Items

Rough changed lines = deleted facade lines + 4 per handler + one per static and test caller type + constant and qualified rewrites + 3 per
facade. Deleted forwarders dominate and are cheap to review.

| Item | Scope | Facades / handlers / facade lines | Rough lines | Model | Before P2-CB10 | Depends on | Risk |
|---|---|---|--:|---|---|---|---|
| P2-ZA | Foundations: registry binding of instance handlers, `GameServices` (partial, one file per domain), `Lazy<>` registration and cycle test, the tool and data file, transitional guards, parity reverse step, clock seam master-only entries, a `DependencyInjection.md` section; pilot removes `ServerTask` (no handler) and `Taxi` (1 handler) end to end with a parity run | 2 / 1 / 80 | 2,000 | Opus | start yes, merge after | - | boot binds a handler to the wrong instance or twice |
| P2-ZB | Inject I: converted callers in `Domain/Character`, `Abilities`, `Combat` (32 classes, 81 pairs, 167 sites, 59 cycles), not calls to `Combat` | - | 500 | Opus | no | ZA, P2-CB10 | a cycle missed until `ValidateOnBuild`; hot-path wrappers |
| P2-ZC | Inject II: converted callers in every other domain (55 classes, 170 pairs, 409 sites, 102 cycles) | - | 1,000 | Sonnet | yes | ZA | test benches (174 construction sites in 117 test files, both items) |
| P2-ZD | `Stat`, `StatusEffect`, `Skill`, `Perk` | 4 / 21 / 2,136 | 2,900 | Opus | no | ZB, ZC | allocation pins; `Skill` partial files; constant snapshots |
| P2-ZE | Character rest (12 facades: `PlayerName`, `Disguise`, `Activity`, ...) | 12 / 18 / 1,094 | 1,400 | Sonnet | no | ZB, ZC | `Disguise` and `PlayerName` audit harnesses read sources |
| P2-ZF | Abilities (`Ability`, `Mimicry`, `QuickCast`, 5 small) | 8 / 21 / 1,053 | 1,450 | Opus | no | ZB, ZC | `CombatSim`'s IL reader and its byte-identical `abilities` CSV; `Mimicry` hidden by `Feature.AbilityDefinition.Mimicry` |
| P2-ZG | Combat helpers I (`TemporaryStatModifier`, `Enmity`, `CombatPoint`, `CombatRecorder`, `Recast`, `Resistance`, 5 small) | 11 / 32 / 1,432 | 1,900 | Opus | no | ZB, ZC | per-swing reads; `ManagedEntryDepth` reads `IsRecording` on every managed entry |
| P2-ZH | Combat helpers II (`Stealth`, `MeleeTechnique`, `Telegraph`, the two attack traits, the three twin pistol services, 4 small) | 12 / 26 / 1,211 | 1,500 | Sonnet build, Opus review | no | ZB, ZC | `Native` per-swing callers |
| P2-ZI | Core (`DB`, `Migration`, `Cache`, `Conversation`, `Snippet`, 11 small) | 16 / 32 / 1,134 | 1,600 | Opus | no | ZB, ZC | `DB.Load` is the first `mod_preload` handler; `CLI`, `Admin` |
| P2-ZJ | Items and UI (`Item`, `KeyItem`, `Gui`, `ExtraHotbars`, 6 small) | 10 / 32 / 1,664 | 2,250 | Sonnet | no | ZB, ZC | 125 constant reads (`Gui` colours) |
| P2-ZK | Crafting and Economy (`Craft` and 5 further facades, `Slicing`, `Scavenge`, `Prospecting`, 6 Economy) | 18 / 20 / 1,471 | 1,750 | Sonnet | no | ZB, ZC | `Prospecting` hidden name; `CraftSim` |
| P2-ZL | World (`Property`, `Music`, `HoloCom`, `Fishing`, 6 small) | 11 / 41 / 1,861 | 2,200 | Sonnet | no | ZB, ZC | handler count |
| P2-ZM | Social (`Performance` and 3 further facades, `Party`, `PlayerList`, chat) | 8 / 35 / 955 | 1,150 | Sonnet | no | ZB, ZC | `Performance` hidden name |
| P2-ZN | Quests and Administration (`Quest` and `QuestEncounter`, `QuestContractBoard`, `Faction`, `CapstoneProof`, 8 DM services) | 13 / 18 / 1,418 | 1,700 | Sonnet | no | ZB, ZC | toolset `Faction` constants |
| P2-ZO | Creatures and Companions (`AI`, `Spawn`, `CompanionControl`, `BeastMastery`, 3 small) | 7 / 59 / 1,082 | 1,450 | Sonnet | no | ZB, ZC | 59 handler moves |
| P2-ZP | `Combat`: inject `ICombatService` into its 16 converted callers (108 sites, 98 cycles), then remove the facade | 1 / 8 / 1,784 | 2,700 | Opus | no | P2-CB10, ZB, ZG, ZH | per-swing hops; 148 constant reads; `CombatSource`; constants snapshot |
| P2-ZZ | Close: drop the flag and the facade branch of the rules, delete the forwarding tests, simplify `ConvertedServices.json`, rewrite `DependencyInjection.md` (recipe without facades), `CoreSystems.md` (handlers on instances), `Service` coverage floor | - | 1,000 | Sonnet build, Opus review | no | all | stale guidance |

**Files each touches.** An injection item touches only `Domain/<its domains>/**`, `Tests/Domain/<its domains>/**` and the benches of those
domains in `Tests/Support`. A removal item deletes `Service/<Facade>.cs` (with partials), adds attributes in `Domain/<Domain>/*Service.cs`,
adds properties in `Composition/GameServices.<Domain>.cs`, and edits the shared lists (`RemovedFacades.json`, `HandlerRenames.txt`,
`ScriptHandlerOrder.txt`, the two handler snapshots, `ConvertedServices.json`, `PlayerMessageAudit.json`, `ClockSeamFiles.txt`,
`EdgesIntoCoreAndPlatform.txt`) and, through the tool, the caller files (one `using` line, constant and qualified rewrites), plus the tests
that reflect on or read its facades.

**Running seven at once.** Two removal items of one domain never run together (they share `GameServices.<Domain>.cs`). Wave 1: ZD, ZF,
ZG, ZI, ZJ, ZL, ZM. Wave 2: ZE, ZH, ZK, ZN, ZO. Then ZP alone. Conflicts in the shared lists are settled as the conversions settled
`ServiceRegistrations.cs`: keep both, sorted, or re-run the tool (it regenerates manifest names from `HandlerRenames.txt`). Two items that add
the same `using` line to one caller file merge cleanly (identical change).

**Before P2-CB10.** Only P2-ZA (it merges after P2-CB10 and settles the `inProgress` lines it shares with `ConvertedServicesTests`) and
P2-ZC (it leaves calls to `Combat` alone). P2-ZB edits the `Combat` parts and `CombatService`, and every removal item needs ZB.

**`Space` and `Droid` while blocked.** They have no facades: they are unconverted static classes and stay so. Their 141 calls into removed
facades (in `Space.cs`, `PlayerShipAccess.cs`, `Droid.cs`, `DroidInstructions.cs`) compile through one `using static` line per file, a trivial port
for `feature/space-revamp`. Converted classes keep reaching them through their `protected virtual` wrappers. When the revamp lands, P2-A9 and
the `Droid` item convert them without facades (the P2-ZZ recipe).

## Gate per item and proof of no functional change

- Build: `MSBUILDUSESERVER=0 dotnet build SWLOR.Game.Server.sln -c Release -p:RunPostBuildEvent=Never -nr:false -v q -clp:ErrorsOnly`, 0 errors.
- `SWLOR.Architecture.Tests` all pass, `Baseline/CrossDomainEdges.txt` unchanged.
- `SWLOR.Game.Server.CharacterizationTests` pass with `Snapshots/` unchanged (never regenerated).
- Handler order: `--filter "FullyQualifiedName~ScriptHandler"`; the manifest diff has the same line count and order and only names change,
  each listed in `HandlerRenames.txt`; a parity run (`Test-MaintainabilityParity.ps1`) reports no handler-order or snapshot difference.
- The tool's `-Check` is clean after `-Apply` (idempotent); the clock seam check changes nothing.
- The area's unit filters, `SWLOR.CombatSim.Tests` (ZF, ZP), `SWLOR.CraftSim.Tests` (ZK), `SWLOR.CLI.Tests` (ZI), and
  `bash scripts/ci/run-integration-tests.sh` (part of every landing gate). Items that change more than about 50 test files run
  `bash scripts/ci/run-unit-tests.sh --out <dir>` with coverage; a floor that drops (deleted forwarders were covered by `FacadeForwardingTests`)
  is a by-hand edit with the measured number.
- Hot items (ZB, ZD, ZF, ZG, ZI, ZP): the allocation tests at their old byte counts, and the engine characterization scenarios
  (`scripts/run-engine-tests.ps1 -Filter Characterization`).
- End of P2-Z: the full unit and engine suites, parity, and the live smoke run (end of phase, owner decision 4).

**The coordinator checks at review:** instance diffs are attribute lines, constructor parameters, fields and `Stat.` to `_stat.` in
existing wrappers, with no logic line; the deleted facades were only forwarders, constants and handlers (`FacadeForwardingTests` passed on
the base); every facade constant is in the constant map; no `Service.<removed>` text remains; no registered class uses `GameServices`
and every `Lazy<>` is a real cycle; the parity and engine runs above, which isolated agents cannot start (`powershell.exe`), so the
coordinator runs them.

## Master syncs

- **A master change to a facade file** (logic on master; deleted on the branch) is a modify/delete conflict, ported by hand as today: logic
  into the instance class; a new public member into the interface and the instance (static callers reach it through `GameServices`, converted
  callers inject); a new constant into its owner plus a line in the constant map; a new handler onto the instance method, its manifest line at
  master's position and a `HandlerRenames.txt` line (the sync stops with exit 3 for the manual manifest edit, as for any unplaced handler).
  Each port goes in the port log.
- **A master change to a caller** (`Feature/`, `Native/`, tests) merges, because call text does not change (except at the 390 explicit
  `GameServices.<Name>.` sites, see "Hidden names"). New master code that calls a
  removed facade compiles after the sync re-runs the tool (it is in `MechanicalScripts.txt`), which adds the `using` and rewrites constants.
  The tool owns no whole files, so it lists none for "take master's version"; a conflict on a `using` line is settled by hand and the tool
  re-runs.
- **A master change in converted logic that calls another service** is ported with an injected field (or a wrapper); the architecture rule
  catches a port that uses `GameServices` from a registered class.

## Order with P2-Z2 and P3-3

- **P2-Z2 after P2-ZZ.** Its guard fails while any of `Combat`, `Mimicry`, `Performance` or `Prospecting` is still a facade type.
- **Hidden names.** Once support code moves into `Domain.<Domain>`, `Combat` names the namespace `Domain.Combat`, so a bare `Combat.X` that
  the accessor served would break. Writing `GameServices.Combat.` (and the other three) everywhere avoids it. That changes the text of 390
  static call sites (`Combat` 313, `Performance` 47, `Mimicry` 21, `Prospecting` 9), so a master edit to one of those lines conflicts:
  take master's line and re-run the tool. P2-Z2's
  `Move-SupportNamespaces.ps1` derives its hidden-facade list from the files of namespace `Service`, so after P2-Z it no longer rewrites
  `Combat`; it still handles `Space`. It runs after `Apply-FacadeRemoval.ps1` in `MechanicalScripts.txt`, because the removal tool finds
  master's facade calls by master's `Service.<Name>Service` namespaces, and `RemovedFacades.json` keeps master's facade names (the script
  never rewrites that file).
- **Master's names after P2-Z2.** The parity copy runs the move too, so its handler names and namespaces are the moved ones while
  `HandlerRenames.txt` keeps master's own names on the left. `Read-HandlerRenames` (`RefactorCommon.ps1`) also lists each left name
  under its moved name, and `Convert-HandlerOrderNames` (`MasterCopy.ps1`) moves a master name no line lists through the same
  namespace map (`Read-NamespaceMoves`: `DomainMap.json` with `retiredServiceFolders` and `namespaceRenames` of `NamespaceMoves.json`),
  so the sync (master's names before the move) and the parity run (after it) both reach the branch's names. `-Reverse` keeps the
  import of a branch-only namespace that the copy's server sources declare.
- **Handler renames stay one line per handler** (decision D4): the P2-Z2 script already updates the branch side of existing lines; a new
  type-level format would need changes to that script, `RefactorCommon.ps1` and the sync.
- **`Space`.** P2-Z cannot clear `Service.Space` (a class) against `Domain.Space` (the namespace P2-Z2 creates); the owner's D8 ruling: convert `Space` and `Droid` from master's code before P2-Z2 if the revamp has not landed.
- **P3-3.** Can run alongside: tests it adds that call `Stat.X` keep compiling once the tool adds the `using`. Removing the facades deletes
  lines that `FacadeForwardingTests` covers, which lowers the overall percentage and the `Service` area; re-measure for P3-3 after P2-ZZ.

## Open decisions

| # | Decision | Recommendation | Who |
|---|---|---|---|
| D1 | What static code (`Feature/`, `Native/`, support types, tools, tests) uses after a facade goes | `GameServices` with `using static`; content code is not converted to injection in this refactor. If "delete facades" is read as "no static access to services at all", that converts 544 `Feature` types, cannot cover `Native/` before Phase 4, and changes the plan's size: the owner would have to confirm that reading | coordinator (owner only for the other reading) |
| D2 | Accessor name | `GameServices` (`Services` collides with the namespaces `SWLOR.Admin.Services` and `SWLOR.Toolset.Services`) | coordinator |
| D3 | Constructor cycles (259 sites, 67 pairs) | `Lazy<IXService>` from an open-generic `Composition` registration, guarded by the cycle test; not wrappers that call `GameServices` (service location inside instances) | coordinator |
| D4 | `HandlerRenames.txt` shape | one line per handler method (339: 330 new, 9 updated), the format every script reads today | coordinator |
| D5 | Constants | move reads to the owner the facade's initializer names; literal facade constants move to the instance class; no constants on interfaces | coordinator |
| D6 | `Combat`'s pure members | static callers of the rule constants call `CombatRules` (required); rule methods too, which removes two hops from per-swing reads (`CombatSim` and `Player` already do it); anything ambiguous stays on `GameServices.Combat` | coordinator |
| D7 | Transition marker | `"facadeRemoved": true` per entry in `ConvertedServices.json`, removed by P2-ZZ | coordinator |
| D8 | `Service.Space` against `Domain.Space` if `feature/space-revamp` has not landed when P2-Z2 is due | Decided by the owner 2026-11-08: if the revamp is not on master when P2-Z2 is due, convert `Space` and `Droid` from master's code (no facades, the P2-ZZ recipe) before P2-Z2; no exception | owner |
| D9 | Parity tool | land the blocked `SkillCacheScope.cs` overlay fix before P2-ZA's pilot: every item that moves a handler needs a parity run | coordinator |

## Appendix A: per service

F = facades (main and further). Lines = facade source lines. Caller columns are IL sites: Inst = converted instance classes, Supp = support types
of converted services (and `Combat` to other facades), Static = unconverted code in `Service/`, Feat = `Feature/`, Native = `Native/`,
Other = `Entity`, `Core` and other projects, Unit = unit test projects, Engine = `EngineTests`.

| Service | Domain | F | Members | Handlers | Lines | Inst | Supp | Static | Feat | Native | Other | Unit | Engine |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Ability | Abilities | 1 | 82 | 9 | 594 | 41 | 0 | 2 | 376 | 3 | 0 | 115 | 489 |
| AbilityCooldownVisual | Abilities | 1 | 10 | 3 | 79 | 0 | 0 | 1 | 3 | 0 | 0 | 10 | 8 |
| AbilityTargetingCache | Abilities | 1 | 3 | 0 | 22 | 0 | 0 | 0 | 5 | 0 | 0 | 0 | 0 |
| FieldEngineerPulseEmitters | Abilities | 1 | 5 | 0 | 25 | 0 | 0 | 0 | 6 | 0 | 0 | 0 | 0 |
| ForceControlCooldowns | Abilities | 1 | 2 | 0 | 20 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 0 |
| Mimicry | Abilities | 1 | 39 | 7 | 249 | 0 | 0 | 0 | 21 | 0 | 0 | 32 | 44 |
| QuickCast | Abilities | 1 | 11 | 2 | 42 | 5 | 0 | 0 | 19 | 1 | 0 | 13 | 37 |
| ReviveHistory | Abilities | 1 | 4 | 0 | 22 | 0 | 0 | 0 | 5 | 0 | 0 | 0 | 0 |
| DMBlueprintCatalog | Administration | 1 | 8 | 1 | 100 | 0 | 0 | 0 | 7 | 0 | 0 | 0 | 9 |
| DMCreatorFavorite | Administration | 1 | 2 | 0 | 38 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 0 |
| DMCreatureManagerFavorite | Administration | 1 | 3 | 0 | 51 | 0 | 0 | 0 | 3 | 0 | 0 | 0 | 0 |
| DMObjectDetails | Administration | 1 | 6 | 0 | 76 | 0 | 0 | 0 | 6 | 0 | 0 | 0 | 4 |
| DMObjectSearch | Administration | 1 | 2 | 0 | 55 | 1 | 0 | 0 | 1 | 0 | 0 | 0 | 4 |
| DMPlayerDirectory | Administration | 1 | 3 | 0 | 54 | 0 | 0 | 0 | 8 | 0 | 0 | 0 | 6 |
| DMPreference | Administration | 1 | 2 | 0 | 36 | 0 | 0 | 0 | 7 | 0 | 0 | 0 | 0 |
| DMPresence | Administration | 1 | 18 | 0 | 205 | 29 | 0 | 0 | 131 | 0 | 0 | 0 | 13 |
| Achievement | Character | 1 | 6 | 2 | 71 | 0 | 0 | 0 | 88 | 0 | 0 | 0 | 1 |
| Activity | Character | 1 | 6 | 2 | 74 | 26 | 8 | 0 | 10 | 0 | 0 | 5 | 80 |
| AppearanceRenderState | Character | 1 | 26 | 0 | 98 | 0 | 0 | 0 | 32 | 0 | 0 | 0 | 0 |
| Disguise | Character | 1 | 24 | 1 | 168 | 16 | 0 | 0 | 19 | 0 | 0 | 37 | 19 |
| EspionageInfiltration | Character | 1 | 14 | 5 | 127 | 5 | 0 | 0 | 1 | 0 | 0 | 29 | 0 |
| Language | Character | 1 | 10 | 1 | 89 | 5 | 0 | 0 | 9 | 0 | 0 | 11 | 7 |
| Perk | Character | 1 | 43 | 3 | 346 | 30 | 13 | 7 | 72 | 0 | 0 | 48 | 38 |
| PlayerDescriptor | Character | 1 | 6 | 0 | 50 | 6 | 0 | 0 | 4 | 0 | 0 | 0 | 11 |
| PlayerIntroduction | Character | 1 | 6 | 1 | 51 | 0 | 0 | 0 | 5 | 0 | 0 | 0 | 0 |
| PlayerName | Character | 1 | 29 | 4 | 188 | 16 | 0 | 4 | 63 | 4 | 0 | 0 | 64 |
| Race | Character | 1 | 5 | 2 | 61 | 0 | 0 | 2 | 5 | 0 | 0 | 1 | 0 |
| RoleplayXPDetection | Character | 1 | 2 | 0 | 25 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 0 |
| Skill | Character | 1 | 41 | 4 | 474 | 34 | 17 | 0 | 93 | 6 | 0 | 164 | 21 |
| Stat | Character | 1 | 118 | 7 | 803 | 77 | 44 | 12 | 403 | 23 | 0 | 377 | 600 |
| StatusEffect | Character | 1 | 88 | 7 | 513 | 44 | 25 | 3 | 296 | 16 | 0 | 99 | 539 |
| TintMapState | Character | 1 | 11 | 0 | 92 | 0 | 0 | 0 | 16 | 0 | 0 | 0 | 0 |
| BattleGrid | Combat | 1 | 12 | 0 | 103 | 0 | 0 | 0 | 12 | 0 | 0 | 7 | 16 |
| Combat | Combat | 1 | 294 | 8 | 1784 | 108 | 35 | 1 | 185 | 90 | 2 | 687 | 638 |
| CombatPoint | Combat | 1 | 17 | 5 | 161 | 2 | 0 | 0 | 52 | 4 | 0 | 2 | 26 |
| CombatRecorder | Combat | 1 | 61 | 3 | 488 | 17 | 13 | 1 | 71 | 17 | 1 | 17 | 102 |
| Death | Combat | 1 | 6 | 4 | 58 | 0 | 0 | 0 | 0 | 0 | 0 | 3 | 5 |
| Enmity | Combat | 1 | 41 | 10 | 299 | 22 | 6 | 3 | 49 | 0 | 0 | 54 | 147 |
| MeleeTechnique | Combat | 1 | 35 | 10 | 120 | 0 | 1 | 0 | 8 | 4 | 0 | 2 | 123 |
| OrdinaryAttackTraits | Combat | 1 | 7 | 0 | 28 | 0 | 0 | 0 | 11 | 4 | 0 | 9 | 13 |
| PairedRangedTraits | Combat | 1 | 26 | 0 | 99 | 0 | 2 | 0 | 13 | 7 | 0 | 2 | 49 |
| Poisons | Combat | 1 | 3 | 1 | 29 | 0 | 0 | 0 | 0 | 0 | 0 | 6 | 0 |
| PowerGloveEquipment | Combat | 1 | 24 | 0 | 108 | 0 | 5 | 1 | 54 | 3 | 0 | 4 | 40 |
| PvpCombat | Combat | 1 | 15 | 3 | 76 | 1 | 0 | 0 | 4 | 0 | 0 | 0 | 54 |
| QueuedAttackAnimation | Combat | 1 | 5 | 0 | 88 | 0 | 0 | 0 | 6 | 0 | 0 | 9 | 2 |
| Recast | Combat | 1 | 8 | 1 | 68 | 4 | 0 | 3 | 11 | 0 | 0 | 8 | 40 |
| Resistance | Combat | 1 | 18 | 1 | 75 | 7 | 12 | 1 | 32 | 1 | 0 | 36 | 11 |
| Stealth | Combat | 1 | 31 | 11 | 202 | 6 | 2 | 0 | 17 | 0 | 0 | 13 | 16 |
| Targeting | Combat | 1 | 2 | 1 | 39 | 2 | 0 | 0 | 29 | 0 | 0 | 1 | 0 |
| Telegraph | Combat | 1 | 20 | 5 | 246 | 2 | 0 | 0 | 8 | 0 | 0 | 22 | 16 |
| TemporaryStatModifier | Combat | 1 | 17 | 2 | 81 | 3 | 4 | 0 | 24 | 1 | 0 | 12 | 230 |
| TwinPistolAbility | Combat | 1 | 8 | 0 | 45 | 0 | 0 | 0 | 9 | 0 | 0 | 0 | 28 |
| TwinPistolEquipment | Combat | 1 | 30 | 0 | 124 | 12 | 1 | 1 | 53 | 8 | 0 | 11 | 43 |
| TwinPistolMastery | Combat | 1 | 4 | 0 | 25 | 0 | 0 | 0 | 6 | 2 | 0 | 1 | 1 |
| WeaponAttackEligibility | Combat | 1 | 3 | 0 | 23 | 0 | 0 | 0 | 2 | 3 | 0 | 15 | 0 |
| WeaponDelay | Combat | 1 | 6 | 1 | 58 | 0 | 0 | 0 | 3 | 0 | 0 | 8 | 1 |
| BeastMastery | Companions | 1 | 45 | 21 | 322 | 4 | 4 | 0 | 76 | 0 | 0 | 16 | 10 |
| DroidEquipmentDiagnostics | Companions | 1 | 9 | 5 | 77 | 0 | 0 | 1 | 0 | 0 | 0 | 4 | 7 |
| IncubationFieldNote | Companions | 1 | 6 | 1 | 70 | 0 | 1 | 0 | 2 | 0 | 0 | 13 | 0 |
| AnimationPlayer | Core | 1 | 1 | 0 | 17 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 |
| Authorization | Core | 1 | 1 | 0 | 25 | 4 | 0 | 1 | 5 | 0 | 0 | 0 | 0 |
| BackgroundJob | Core | 1 | 2 | 0 | 44 | 1 | 0 | 0 | 4 | 0 | 0 | 0 | 0 |
| Cache | Core | 1 | 12 | 1 | 126 | 7 | 5 | 0 | 74 | 0 | 0 | 1 | 1 |
| Communication | Core | 1 | 10 | 7 | 86 | 0 | 0 | 0 | 15 | 0 | 0 | 0 | 0 |
| Conversation | Core | 1 | 13 | 4 | 109 | 3 | 0 | 0 | 4 | 0 | 0 | 0 | 6 |
| ConversationMenu | Core | 1 | 6 | 3 | 51 | 2 | 1 | 0 | 3 | 0 | 0 | 0 | 1 |
| DB | Core | 1 | 15 | 1 | 133 | 0 | 60 | 67 | 676 | 3 | 5 | 0 | 792 |
| Messaging | Core | 1 | 5 | 0 | 61 | 1 | 1 | 1 | 24 | 0 | 0 | 0 | 0 |
| Migration | Core | 1 | 11 | 3 | 106 | 0 | 12 | 0 | 5 | 0 | 0 | 0 | 1 |
| NamedAnimation | Core | 1 | 9 | 2 | 73 | 2 | 5 | 2 | 12 | 0 | 0 | 0 | 2 |
| ObjectVisibility | Core | 1 | 4 | 2 | 57 | 1 | 0 | 0 | 19 | 0 | 0 | 0 | 0 |
| PlayerFeedback | Core | 1 | 10 | 0 | 78 | 0 | 0 | 0 | 8 | 0 | 0 | 0 | 5 |
| Snippet | Core | 1 | 6 | 7 | 85 | 0 | 0 | 0 | 7 | 0 | 0 | 0 | 0 |
| TileMagic | Core | 1 | 3 | 0 | 36 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 |
| Walkmesh | Core | 1 | 3 | 2 | 47 | 3 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Craft | Crafting | 6 | 72 | 6 | 676 | 9 | 28 | 0 | 145 | 0 | 0 | 42 | 30 |
| Prospecting | Crafting | 2 | 12 | 2 | 99 | 0 | 1 | 0 | 8 | 0 | 0 | 0 | 0 |
| Scavenge | Crafting | 2 | 14 | 2 | 110 | 0 | 1 | 0 | 13 | 0 | 0 | 0 | 0 |
| Slicing | Crafting | 2 | 17 | 3 | 141 | 0 | 0 | 0 | 21 | 0 | 0 | 0 | 0 |
| AI | Creatures | 1 | 32 | 18 | 271 | 7 | 0 | 8 | 1 | 1 | 0 | 0 | 8 |
| CompanionControl | Creatures | 1 | 26 | 6 | 153 | 12 | 4 | 10 | 15 | 0 | 0 | 10 | 5 |
| NPCGroup | Creatures | 1 | 2 | 1 | 36 | 0 | 2 | 0 | 1 | 0 | 0 | 6 | 0 |
| Spawn | Creatures | 1 | 16 | 7 | 153 | 1 | 0 | 0 | 1 | 0 | 0 | 0 | 32 |
| Bank | Economy | 1 | 11 | 0 | 78 | 0 | 0 | 0 | 11 | 0 | 0 | 2 | 35 |
| CasinoSlot | Economy | 1 | 5 | 1 | 37 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 0 |
| Currency | Economy | 1 | 5 | 1 | 70 | 0 | 0 | 0 | 26 | 0 | 0 | 0 | 6 |
| Guild | Economy | 1 | 11 | 2 | 105 | 0 | 3 | 0 | 8 | 0 | 1 | 14 | 4 |
| GuildTaskXp | Economy | 1 | 8 | 0 | 62 | 0 | 2 | 0 | 2 | 0 | 0 | 28 | 0 |
| PlayerMarket | Economy | 1 | 7 | 3 | 93 | 0 | 0 | 0 | 11 | 0 | 0 | 1 | 6 |
| ServerTask | Gameplay | 1 | 4 | 0 | 32 | 0 | 0 | 0 | 5 | 0 | 0 | 0 | 0 |
| EquipmentUnequip | Items | 1 | 3 | 1 | 31 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 1 |
| Item | Items | 1 | 55 | 4 | 464 | 38 | 33 | 4 | 152 | 4 | 0 | 97 | 62 |
| ItemTrash | Items | 1 | 1 | 0 | 26 | 0 | 0 | 0 | 3 | 0 | 0 | 0 | 0 |
| KeyItem | Items | 1 | 12 | 2 | 145 | 3 | 7 | 0 | 122 | 0 | 0 | 6 | 6 |
| LightsaberWorkbench | Items | 1 | 8 | 1 | 119 | 0 | 0 | 0 | 6 | 0 | 0 | 20 | 3 |
| Loot | Items | 1 | 11 | 7 | 121 | 0 | 5 | 0 | 8 | 3 | 0 | 0 | 2 |
| OverflowStorage | Items | 1 | 3 | 0 | 31 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 |
| Wardrobe | Items | 1 | 11 | 0 | 90 | 0 | 0 | 0 | 19 | 0 | 0 | 0 | 11 |
| CapstoneProof | Quests | 1 | 9 | 0 | 74 | 0 | 2 | 0 | 1 | 0 | 0 | 0 | 9 |
| Faction | Quests | 1 | 5 | 1 | 74 | 0 | 4 | 0 | 7 | 0 | 0 | 0 | 3 |
| Quest | Quests | 2 | 39 | 14 | 370 | 2 | 13 | 0 | 25 | 0 | 0 | 1 | 43 |
| QuestContractBoard | Quests | 1 | 27 | 2 | 285 | 0 | 0 | 5 | 28 | 0 | 0 | 0 | 17 |
| ChatCommand | Social | 1 | 12 | 2 | 65 | 0 | 0 | 0 | 10 | 0 | 0 | 0 | 0 |
| ChatPortraitTells | Social | 1 | 8 | 3 | 79 | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 20 |
| Party | Social | 1 | 12 | 7 | 135 | 0 | 0 | 0 | 15 | 0 | 0 | 0 | 30 |
| Performance | Social | 4 | 68 | 15 | 516 | 10 | 0 | 0 | 47 | 0 | 0 | 1 | 28 |
| PlayerList | Social | 1 | 18 | 8 | 160 | 0 | 0 | 0 | 22 | 0 | 0 | 0 | 57 |
| ExtraHotbars | UI | 1 | 40 | 8 | 288 | 0 | 0 | 0 | 41 | 0 | 0 | 18 | 2 |
| Gui | UI | 1 | 25 | 9 | 349 | 40 | 35 | 10 | 233 | 0 | 0 | 10 | 125 |
| Area | World | 1 | 9 | 4 | 95 | 3 | 1 | 1 | 20 | 0 | 0 | 2 | 22 |
| Fishing | World | 2 | 20 | 4 | 164 | 3 | 0 | 0 | 14 | 0 | 0 | 1 | 0 |
| HoloCom | World | 1 | 27 | 4 | 190 | 2 | 0 | 0 | 11 | 0 | 0 | 0 | 8 |
| Music | World | 1 | 30 | 8 | 290 | 1 | 1 | 0 | 23 | 0 | 0 | 0 | 37 |
| Notes | World | 1 | 12 | 0 | 100 | 0 | 0 | 0 | 14 | 0 | 0 | 31 | 1 |
| Planet | World | 1 | 5 | 1 | 70 | 0 | 0 | 2 | 10 | 0 | 0 | 4 | 9 |
| Property | World | 1 | 54 | 11 | 624 | 4 | 7 | 5 | 98 | 0 | 0 | 0 | 29 |
| Shuttle | World | 1 | 10 | 4 | 137 | 0 | 0 | 0 | 9 | 0 | 0 | 0 | 12 |
| Taxi | World | 1 | 3 | 1 | 48 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 2 |
| Traps | World | 1 | 6 | 1 | 80 | 0 | 0 | 0 | 4 | 0 | 0 | 0 | 11 |
| Weather | World | 1 | 18 | 4 | 111 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 12 |
| **Total** | | 133 | 2302 | 364 | 18375 | 684 | 426 | 159 | 4532 | 209 | 9 | 2198 | 5108 |
