# Maintainability Refactor: WeaponActiveAbilityDefinitionBase Split (P5-16f)

Part map, revised after the coordinator's review (decisions D1 to D7 in section 2 are binding); not yet approved by the owner. Written against `feature/maintainability-refactor` at `d43e40fa8`
and re-measured at `8a54b0841`, after P5-5 (status effect split) and the dead-helper deletion (#3506) merged; every number below is the re-measured one. PR 1 (tests and this map) changes no production
file. Limits come from [MaintainabilityRefactorPhase5.md](MaintainabilityRefactorPhase5.md): 1,000 lines and 10 services per type,
150 lines per method, no new static service lookups, no new upward domain references, no `Part1`/`Part2` chunks. `Base.cs:N` means line N of
`Feature/AbilityDefinition/WeaponActiveAbilityDefinitionBase.cs` at `8a54b0841`. Estimates are rounded to about 10; the build report gives exact numbers.

## 1. What the type is

One abstract class, 2,041 lines (2,332 before the dead helpers went), no constructor, no handlers, no carried state. It is the shared base of ability definitions (`IAbilityListDefinition`) and reaches every service through
`using static GameServices`.

| Fact | Number |
|---|---|
| Direct subclasses | 120: 116 definitions and 4 intermediate bases (`HeavyVibrobladeActiveAbilityDefinitionBase`, `TwinPistolAbilityDefinitionBase`, `UnarmedAbilityDefinitionBase`, `SpearActiveAbilityDefinitionBase`) |
| Definitions below the bases | 31 (11 Heavy Vibroblade, 10 Twin Pistol, 10 Unarmed; the Spear base has none) |
| `ConfigureWeaponAbility` call sites | 212 in 103 production files (214 in 105 with tests) |
| `new WeaponAbilityProfile` | 149 sites in 69 production files (158 in 73 with tests) |
| Helpers with callers outside the base | `ConfigureToggle` 11, `ConfigureSelfStatus` 3, `ConfigurePartyStatus` 1, `ToggleSelfStatus` 3 (and `HeavyVibrobladeActiveAbilityDefinitionBase` declares its own), `ApplyStatusToNearbyParty` 2, `ConfigureWeapon` 1 (production; 10 with the engine tests). The `Type` overload of `ConfigurePartyStatus` (`Base.cs:1978-1982`) has no caller; its `Func` overload has the one |
| Built by | `AbilityCatalogService` calls `Activator.CreateInstance(type)` for every `IAbilityListDefinition` at `mod_cache_bef`; `BuildAbilities()` runs the subclass's static helpers, which call the protected statics. 33 test files call `BuildAbilities()` with no container. |
| Handlers | none (`ScriptHandlerOrder.txt` has no line) |
| State | none carried. `StaticState.txt` counts 1: the `WeaponAbilityProfile.Empty` shared instance |
| `ServicesPerType.txt`, `StaticLookups.txt`, `LargeTypes.txt`, `LongMethods.txt` | 24 services (all by lookup), 169 lookup lines, 2,041 lines, `ConfigureWeaponAbility` 291 lines |

Anatomy:

| Lines | What | Size |
|---|---|---|
| 22-283 | `WeaponAbilityProfile` data: 188 `init` properties, plus 4 on the nested snapshot types | 262 |
| 284-1528 | `WeaponAbilityProfile` behavior: 54 methods | 1,245 |
| 1530-1886 | `ConfigureWeaponAbility` (1530-1820, 291 lines) and its targeting helpers | 357 |
| 1887-1935 | `ConfigureWeapon` and `ApplyCombatImpactDamageAbility` | 49 |
| 1936-2040 | status helpers (`ConfigureToggle`, `ConfigureSelfStatus`, `ConfigurePartyStatus`, `ToggleSelfStatus`, `Additional`, `ApplyStatusToNearbyParty`) | 105 |

The 24 services: `Ability`, `AbilityDamageInput`, `AbilityImmunity`, `AbilityImpactState`, `AbilityStatusApplication`, `CombatAbilityActivation`, `CombatAbilityBonus`, `CombatAbilityHit`,
`CombatActivity`, `CombatDamageTaken`, `CombatDefenseMemory`, `CombatQueries`, `CombatRecorder`, `CombatResources`, `CombatSuppression`, `Enmity`, `Party`, `PlayerFeedback`, `Stat`, `StatusEffect`,
`StatusEffectApplication`, `StatusEffectCatalog`, `StatusEffectDuration`, `TemporaryStatModifier`. Against the first measurement (21): `AbilityHostileImpact` left with the dead helpers, `StatusEffectApplication`,
`StatusEffectCatalog` and `StatusEffectDuration` came with P5-5, and `CombatDefenseMemory` with P5-7. It also reaches static content and helpers: `GuardedStatusEffect` (`Base.cs:1191, 1229, 1408, 1426, 1484, 1521`),
`WardBondStatusEffect.HasRecentWardHit` (1197, 1235, 1392), `AbilityTargeting` (1357, 1403), `TemporaryHitPointEffects.ApplyFlat` (1468), `ActionInterruption.ClearActions` (516), `UsePerkFeat.InterruptAbilityActivationFromHit` (517),
`ObjectSnapshot.InShape` (2027).

Content named by the behavior is eight `Feature/StatusEffectDefinition` types: `BleedStatusEffect`, `HemorrhageStatusEffect`, `SunderStatusEffect` (snapshot `Capture` at `Base.cs:53`, spread at 1284-1308),
`InfectionStatusEffect` (379), `WardBondStatusEffect` (1197, 1235, 1392), `GuardedStatusEffect`, `DisorientedStatusEffect` (1339), and by `new`: `ChallengeEnmityStatusEffect` (1238, 1245). `FoggyMindStatusEffect` left with its dead helper.

Constraints that shape the design:

1. The builder wiring must work without a container: `FacadeCache.Get` throws without one (`Composition/FacadeCache.cs:27-37`).
2. The profile is data plus behavior. Behavior that needs a service moves to a service (`MaintainabilityRefactorContentInjection.md` decision 1 and its `WeaponAbilityProfile` entries at lines 121 and 576).
3. A `Domain/` type naming those nine content types would add `LogicToContent.txt` lines, so the parts live in `Feature/AbilityDefinition`, which `DomainMap.json` counts as the Abilities domain. Domain edges stay as they are.
4. `SWLOR.CombatSim/Data/WeaponImpactReader.cs` reads the impact closure by method name `<ConfigureWeaponAbility>` (`SharedHelpers`, `:73`) and captured names (`baseDamage`, `duration`, `statusEffect`, `additionalStatusEffects`, `profile`, `skill`, `restoresSecondaryTargetStamina`), and compares public properties with a default profile (`UnmodeledRiders()`, `:41-48`, called at `:90`). `AreaAbilityFragmentationTests.cs:191` and `GuardCounterControlTests.cs:118` read the same closures.

## 2. Decisions (coordinator, binding)

- **D1 Shape.** The builder wiring stays static and pure on the base. Only run-time behavior moves into constructor-injected content singletons. Each closure body inside `ConfigureWeaponAbility` becomes one
  call resolved at run time: a direct `GameServices.X.Method(...)` inside the lambda, not a static property (`Service_typed_members_of_content_are_private_and_named_like_the_old_property`,
  `SWLOR.Architecture.Tests/ContentInjectionRulesTests.cs:207`, rejects static or protected service properties on content). The closures stay in `<ConfigureWeaponAbility>` and capture the same names, so the CombatSim reader,
  `AreaAbilityFragmentationTests.cs:191` and `GuardCounterControlTests.cs:118` need no change, and the `<ConfigureSavageCleave>` path (`Vibroblade/SavageCleaveAbilityDefinition.cs:30-38`) is untouched. The closure
  builds a `readonly record struct` of its captured values to pass to the part (no allocation), so no part method takes 20 parameters. The temporary lookups left on the base follow the precedent of
  `MaintainabilityRefactorContentInjection.md` decision 7 and lines 543-545; P5-14 converts them in waves C5 and C6 (ability definitions and their forwarding constructors). P5-14 comes after P5-16, so nothing waits on it.
- **D2 Call text.** The 212 `ConfigureWeaponAbility` sites and the derived definitions keep their call text. The protected statics stay on the base (Phase 5 decision 2 and the P5-13 "call text stays the same" rule). Watch the shadowing:
  `HeavyVibrobladeActiveAbilityDefinitionBase.cs:18` declares its own `ToggleSelfStatus` and `:122` its own `ApplyStatusToNearbyParty`, which hide the base members. Signatures on the base do not change.
- **D3 Parts by caller.** Layers with no cycle; each part at 10 services or fewer counting part dependencies, `IContentFactory` and what P5-5 adds (section 3).
- **D4 Registration** (section 5), **D5 proof** (section 8), **D6 sequencing** (section 9), **D7 dead helpers** (section 6).

## 3. The parts

Folder `Feature/AbilityDefinition/WeaponAbility/`, namespace `SWLOR.Game.Server.Feature.AbilityDefinition.WeaponAbility`. Constructor injection, private get-only service properties named like the old
property (`Stat`, `StatusEffect`), no static lookup, no `Func<>` deferred resolution. Method bodies move verbatim except for the receiver. Counts are after P5-5, with the final interfaces of
[MaintainabilityRefactorStatusEffectSplit.md](MaintainabilityRefactorStatusEffectSplit.md): `S` = `IStatusEffectService` (reads and removal), `A` = `IStatusEffectApplicationService` (part 3, `ApplyStatusEffect`),
`D` = `IStatusEffectDurationService` (part 4, `ExtendStatusEffectDuration`), `C` = `IStatusEffectCatalogService` (part 2, `CreateStatusEffect`, `GetStatusEffectName`). "Later" is what becomes injected
after P5-12 and C8 (`AbilityTargeting`, `TemporaryHitPointEffects`, `ActionInterruption`, `UsePerkFeat`).

| Layer | Type | Responsibility and members that move | Lines | Services (with P5-5) | Part dependencies | Later |
|---|---|---|---|---|---|---|
| 0 | `WeaponAbilityTemporaryModifiers` | Writing temporary stat modifiers: `ReplaceTemporary`, `ReplaceTemporaryPayload`, `ReplaceTemporaryTarget`, `ApplyTemporaryDefeatedEnemyModifiers`; takes the profile's modifier source as an argument | ~155 | 1: `TemporaryStatModifier` | none | none |
| 0 | `WeaponAbilityStatusRiders` | Conditional statuses, spread, suppression: `GetAppliedCategories`, `ApplyConditionalStatus`, `ApplyTargetConditionalStatus`, `ApplyStatusSpread`, `SpreadStatusToNearbyHostile`, `ApplySuppressionEffects`, snapshot capture | ~210 | 7: `CombatActivity`, `CombatDefenseMemory`, `CombatQueries`, `CombatSuppression`, `S`, `A`, `C` (the first map also listed `CombatDamageTaken` and `Stat`, which no member of this part calls) | none | +`AbilityTargeting` = 8 |
| 0 | `StatusAbilityEffects` | Run-time bodies of toggle, self and party status abilities: `ToggleSelfStatus`, `ApplyStatusToNearbyParty` x2, the toggle and self-status closure bodies | ~110 | 6: `Ability`, `CombatRecorder`, `Party`, `S`, `A`, `C` (`ToggleSelfStatus` calls `GetStatusEffectName`) | none | none |
| 1 | `WeaponAbilityDamageModifiers` | Damage, percent damage, critical rate, defense ignore, idle snapshots and activation prep, with private predicates: `GetBaseDamageAdjustment`, `GetDamagePercentAdjustment`, `GetCriticalRateAdjustment`, `GetDefenseIgnorePercent`, `ResolveImpactStatus`, `CaptureActivationIdleBonusSnapshot`, `ClearActivationIdleBonusSnapshots`, `PrepareQueuedActivation`, `PrepareActivationIdleBonuses`, `IsIdle`, `IsTargetBelowThreshold`, `IsBleeding` | ~290 | 8: `CombatAbilityBonus`, `CombatActivity`, `CombatDefenseMemory`, `CombatQueries`, `CombatResources`, `PlayerFeedback`, `Stat`, `S` | none (the predicates are private; no other part uses them) | none |
| 1 | `WeaponAbilitySelfEffects` | What the caster gets: `SpendHitPoints` (uses `CombatAbilityActivation`, `Base.cs:508`), `AfterActivation`, `AfterHostileActivation`, `ApplySelfInvisibility`, `ApplyNearbyPartyStatus`, `ApplySelfImmunity`, `ApplySelfModifiers`, `ApplyLandedImpactModifiers` | ~125 | 8: `CombatAbilityActivation`, `CombatRecorder`, `CombatDamageTaken`, `CombatResources`, `AbilityImmunity`, `Enmity`, `A`, `TemporaryStatModifier` (`AddCapped`, `Base.cs:884`; `S` is not called here); plus 1 part = 9 | `WeaponAbilityTemporaryModifiers` | none |
| 1 | `WeaponAbilityHitEffects` | Per-hit resource and modifier effects: `ApplyResourceConditionalRestore`, `ApplyResourceConditionalDrain`, `IsFPAtOrAbove`, `IsStaminaAtOrAbove`, `ApplyTargetTemporaryModifiers`, `ApplyProtectedTargetHitSelfModifiers` | ~75 | 5: `CombatActivity`, `CombatResources`, `Stat`, `A`, `IContentFactory` (for `ChallengeEnmityStatusEffect`); plus 1 part = 6 | `WeaponAbilityTemporaryModifiers` | none |
| 2 | `WeaponAbilityHitRiders` | What each landed hit triggers: `AfterSuccessfulHit`, `BeforeSuccessfulImpactRiders`, `ExtendSourceStatusEffects`, `ConsumeSourceStatusEffects`, `ApplyDefenseIgnoreHitEffects`, `ApplyTargetUsingAbilityEffects`. The `ApplyRangedAbilityTargetDefenseReduction` call stays in the closure body that makes it. | ~165 | 8: `AbilityImpactState`, `CombatActivity` (`ApplyTargetUsingAbilityEffects`), `CombatResources` (`AfterSuccessfulHit`), `Stat`, `S`, `A`, `D`, `TemporaryStatModifier` (`ApplyDefenseIgnoreHitEffects`); plus 3 parts = 11 | `WeaponAbilityHitEffects`, `WeaponAbilityStatusRiders`, `WeaponAbilityDamageModifiers` (`GetDefenseIgnorePercent`, `Base.cs:637`) | +`ActionInterruption`, `UsePerkFeat` = 13. **Over 10 now; see section 11.** |
| 2 | `WeaponAbilityImpactEffects` | After the last hit: `RestoreResourcesForTargetCount`, `AfterImpact` | ~110 | 6: `CombatDefenseMemory`, `CombatResources`, `PlayerFeedback`, `Stat`, `A`, `TemporaryStatModifier`; plus 2 parts = 8 | `WeaponAbilitySelfEffects` (`ApplyLandedImpactModifiers`), `WeaponAbilityDamageModifiers` (`ClearActivationIdleBonusSnapshots`) | none |
| 2 | `WeaponAbilityFriendlyTargets` | Friendly and self-status abilities and target validation: `HasFriendlyTargetStatus`, `HasFriendlyTargetEffects`, `ValidateHostileTarget`, `ValidateFriendlyTargetStatus`, `ResolveFriendlyTarget`, `ApplyFriendlyTargetStatus`, `ApplyFriendlyTargetEffects`, `HasImmediateSelfStatusEffect`, `ApplySelfStatusEffect`, `ApplySelfStatusToGuardedTarget`, and the friendly impact body | ~155 | 4: `Ability`, `TemporaryStatModifier`, `S`, `A`; plus 1 part = 5 | `WeaponAbilitySelfEffects` (`AfterActivation`) | +`AbilityTargeting`, `TemporaryHitPointEffects` = 7 |
| 2 | `WeaponAbilityActivations` | The activation closure body | ~50 | 2: `CombatActivity`, `CombatAbilityBonus`; plus 2 parts = 4 | `WeaponAbilityDamageModifiers`, `StatusAbilityEffects` | none |
| 3 | `WeaponAbilityImpacts` | The hostile impact body (telegraphed area path and direct multi-hit path), `ResolveStatusCategories`, the pure area helpers (`ShouldUseTelegraphedCombatImpact`, `ToCombatImpactAreaShape`, `CenterAreaOnActivator`) | ~250 | 5: `Ability`, `AbilityDamageInput`, `AbilityImpactState`, `CombatAbilityHit`, `AbilityStatusApplication`; plus 4 parts = 9 | `WeaponAbilityHitRiders`, `WeaponAbilityImpactEffects`, `WeaponAbilityDamageModifiers`, `WeaponAbilitySelfEffects` | none. It needs no `A`: the one status application in the old impact body is the friendly branch (`Base.cs:1617`), which moved to `FriendlyTargets`. |
| - | `WeaponAbilityProfile` (top level, namespace `Feature.AbilityDefinition`, so the 68 files need no `using`) | Data only: 188 properties, `Empty`, the two snapshot data types. `ModifierSource` is an `internal` get-only property (a public one would show up in `UnmodeledRiders`). Keeps its parameterless constructor and property names. Becomes `public` (tests and CombatSim read it). | ~300 | 0 | none | none |
| - | `WeaponActiveAbilityDefinitionBase` (stays) | Static, pure builders and the protected statics the 150 definitions call; closure bodies are one-line calls into the parts | ~450 | about 7 by lookup: `WeaponAbilityActivations`, `WeaponAbilityImpacts`, `WeaponAbilityFriendlyTargets`, `StatusAbilityEffects`, `Ability` (the `ConfigureWeapon` family), plus what the build finds | n/a | n/a |

Layer order: 0 `TemporaryModifiers`, `StatusRiders`, `StatusAbilityEffects`; 1 `DamageModifiers`, `SelfEffects`, `HitEffects`; 2 `HitRiders`, `ImpactEffects`, `FriendlyTargets`, `Activations`; 3 `Impacts`. A part calls only lower layers.
The first map had two cycles; both are gone. Cycle A (`HitRiders` <-> `StatusRiders` through `GetAppliedCategories`): `GetAppliedCategories` lives in `StatusRiders` and `HitRiders` calls it one way. Cycle B
(`HitRiders` <-> `ImpactEffects` through `ApplyTargetUsingAbilityEffects`): that method moved to `HitRiders` with the other per-hit methods, and the per-hit self effects went to `HitEffects`. `ApplyLandedImpactModifiers` is in `SelfEffects`.
`ReplaceTemporary*` are in the layer 0 leaf, so `SelfEffects` and `HitEffects` share it without calling each other. `No_two_content_singletons_depend_on_each_other` and the tool's `RefuseCycles` check this.

Totals: about 1,700 lines of new types, about 300 of profile data and about 450 on the base, against 2,041 now. No part is over 290 lines. One part is over the service limit: `HitRiders` counts 11 (13 with the later statics),
because the first map left out `CombatResources`, `CombatActivity`, `A` and `TemporaryStatModifier` (section 11). Tightest other rows: `SelfEffects` and `Impacts` (9). If the build counts one more, move a member group to the lower neighbor that already calls it,
never a new numbered type. Check the `Stat` and `CombatQueries` counts first.

The six rider types (`DamageModifiers`, `StatusRiders`, `HitRiders`, `HitEffects`, `ImpactEffects`, `SelfEffects`) depart from "data plus a rider singleton" (`MaintainabilityRefactorContentInjection.md` lines 121 and 576). One rider
singleton would be a type of about 1,245 lines reaching 16 services, which is the debt this item removes. So the riders are split by caller into the layers above and the data stays one plain class.

Status types stay named in Feature code: `DamageModifiers` names `InfectionStatusEffect`; `StatusRiders` names `BleedStatusEffect`, `HemorrhageStatusEffect`, `SunderStatusEffect`, `DisorientedStatusEffect`, `WardBondStatusEffect`, `GuardedStatusEffect`;
`HitEffects` names `WardBondStatusEffect`, `GuardedStatusEffect`, `ChallengeEnmityStatusEffect`; `FriendlyTargets` names `WardBondStatusEffect` and `GuardedStatusEffect`. `ChallengeEnmityStatusEffect` takes a value, so `HitEffects` builds it with `_content.Create<ChallengeEnmityStatusEffect, int>(percent)` and counts `IContentFactory`.
The four `Activator.CreateInstance(type)` status factories (`Base.cs:1938, 1958, 1980, 2015`) stay as they are; C5 and C6 replace them (`MaintainabilityRefactorContentInjection.md:627`).

## 4. `ConfigureWeaponAbility` under 150 lines

By D1 alone, not by cutting steps for the metric. The 22-parameter signature (24 lines) stays. The activation, impact, status-category, hostile-validation and friendly-validation closures each shrink to one call
(`GameServices.WeaponAbilityImpacts.ApplyHostileImpact(...)` and so on), so the 291 lines lose the roughly 170-line hostile impact body, the roughly 55-line friendly branch and the activation and category bodies.
Estimate: 120 to 140 lines; the build measures it. The builder chain, kind and target-shape blocks and requirement calls stay in place. If the measured result is over 150, the three non-closure blocks (hostile, friendly and self-status settings) become
private static methods; that is the only cut permitted, decided by the measurement. Closure order, captured variables and evaluation order are unchanged. `ApplyHostileImpact` keeps `criticalRatePercentAdjustment` computed on every hit
(`Base.cs:1741`, not built once) and the separate area and direct paths (`Base.cs:1666-1702` and `1751-1752`): there is no shared `FinishImpact`, because the two paths clear the idle snapshot and call `AfterImpact` differently.

## 5. Registration and the guards that enforce counts

- Add `SWLOR.Game.Server/Feature/AbilityDefinition/WeaponAbility/` to `converted` in `tools/Refactor/ContentInjection.json` and run `pwsh -NoProfile -File tools/Refactor/Apply-ContentInjection.ps1 -Check` first (the analyzer is `tools/Refactor/SWLOR.ContentInjection/ContentAnalyzer.cs`).
- The analyzer treats a class as a singleton only if it is static, already registered, or has no instance members besides constructors (`ContentAnalyzer.cs:96-101`). The parts have instance methods, so by shape they are not candidates.
  **Unresolved:** how the first registration happens. Plan: seed one `services.AddSingleton<T>()` line per part in `Composition/ContentRegistrations.cs` once (the tool reads the registered set from that file and keeps the lines after), run `-Check`,
  and confirm with the tool's owner before the PR (question 2).
- The tool refuses a type over 10 services (`ContentAnalyzer.cs:52-53`) and rejects cycles (`RefuseCycles`). `ServicesPerType.txt` counts only types that `ServiceRegistrations` registers (`Debt/DebtMeasures.cs:153-200`), so it does not count the parts.
  Counts are enforced by the tool's refusal plus a new unit test in the PR that fails any part over 10 constructor services; `No_two_content_singletons_depend_on_each_other` enforces the layer order.
- The base reaches each entry part through a temporary `GameServices` property named like the type (`GameServices.WeaponAbilityImpacts`), in `Composition`, removed at C11 with the rest. It is the only static lookup the item adds; the base's own lookups fall from 169 lines.

## 6. Dead helpers (D7): done

The first measurement found eleven protected helpers with no caller outside the base: ten dead, one live (`Additional`, kept because `ConfigureWeapon` calls it). The owner approved deleting the ten on 2026-11-17 and they went in
their own PR, #3506 (`8a54b0841`): `ConfigureTargetStatus`, `ConfigureAreaStatus`, `ApplyStatusToNearbyEnemies`, `GetAreaStatusLocation`, `ConfigureCastedTarget`, `ConfigureMultiHit`, `ConfigureInterrupt`, `PurifyAndMirror`,
`GetNearestHostile` and `FoggyMind` (with `GetRemainingDurationSeconds`), 291 lines. The two tests that read the source text of some of them (`AbilityDamageQueueTests`, `CombatDamageTests`) were fixed in the same PR. The base fell
from 2,332 to 2,041 lines, and `StatusAbilityEffects` is the six-service part of section 3. What was PR 4 of the first table (delete the helpers, finish the `PlayerMessageAudit.json` re-key) is gone; the re-key moves into PR 3.

## 7. State, handlers and hot reload

- No handlers: `ScriptHandlerOrder.txt`, `BootHandlerClassification.txt` and `HandlerRenames.txt` do not change.
- No carried state: no `[ReloadCarried]` field exists or is added, so `ReloadCarry` is not involved. The parts are stateless singletons a reload rebuilds (`HotReload.md`). The ability cache reruns on reload and builds new `AbilityDetail` closures over new profiles, as today.
  A modifier an old profile's `ModifierSource` wrote expires on its own timer, as today.
- `WeaponAbilityProfile.Empty` stays a shared instance; its `StaticState.txt` line is re-keyed from `WeaponActiveAbilityDefinitionBase` to `WeaponAbilityProfile` when the profile becomes top level.
- `PlayerMessageAudit.json` has 11 rows keyed by `File`, `Line` and `Member` to this file. They re-key to the new files and members, and the `Call` text changes where `profile.` is added or removed. The audit test checks them.

## 8. Proof that behavior is unchanged

1. **Settings snapshot, from the base commit before any move (done in PR 1).** `WeaponAbilitySettingsTests` builds every ability definition under the base (137 definitions, 280 feats: 217 configured through `ConfigureWeaponAbility`, 63 built another way) through `BuildAbilities()`
   and compares the text with `Fixtures/WeaponAbility/WeaponAbilitySettings.txt` (332 KB): per feat, every `AbilityDetail` property that differs from a new detail and every `WeaponAbilityProfile` property that differs from the default, read by reflection as CombatSim does
   (the profile is found in the fields the closures capture). The comparison is token by token (`WeaponAbilityGolden`), not a raw text diff.
2. **Call-trace snapshot with a coverage proof (done in PR 1).** `WeaponAbilityCallTraceTests` drives each ability's categories, validation, activation and impact closures against recording doubles of the 24 services (`WeaponAbilityTraceBench` on `CombatTranscriptBench`) and records the ordered log (service, member, arguments)
   in `Fixtures/WeaponAbility/WeaponAbilityCallTrace.txt.gz` (3,942 steps, 11.6 MB of text, 322 KB compressed; `gzip -dc` shows it; a trace an earlier ability already produced in the same scenario is written as "same as" that ability). Every shipped ability runs in three scenarios.
   Because shipped abilities leave most of the 188 profile settings unused, `WeaponAbilityProbe` (a definition of the tests) also calls `ConfigureWeaponAbility` in 14 shapes (single, with status, three telegraphed shapes, direct area, friendly, self, party, the `ConfigureWeapon` helper) with a default profile, a profile with every setting, three
   variants of it, and one profile per setting (`WeaponAbilityProfileVariants`, by reflection). Measured with `dotnet-coverage` on that one test at `8a54b0841`: 1,387 of 1,390 lines and 1,416 of 1,436 branches of `Base.cs:284-2041` (the report is `coverage-proof.txt` in PR 1's evidence). The lines and branches left are named in section 11. The log also records:
   - NWScript calls: every engine write the base makes (`FloatingTextStringOnCreature`, `SendMessageToPC`, `ApplyEffectToObject` with `EffectDamage`, and so on) through the combat transcript bench's engine fake, which also answers `GetNearestCreatureToLocation`, `GetFirstObjectInShape`, `GetIsReactionTypeHostile` and hit points from the scenario;
   - static content reads: `WardBondStatusEffect.HasRecentWardHit`, `GuardedStatusEffect` and `AbilityTargeting` run for real against the doubles and the engine fake (the scenario fills their recent-hit dictionaries by reflection and empties them afterwards), so what they ask of a service is in the log. `UsePerkFeat.InterruptAbilityActivationFromHit` and
     `ActionInterruption.ClearActions` reach the engine's native library (`g_pAppManager`), which a unit test cannot load: the log shows the call stopping there, and the same settings run again without the setting that reaches it;
   - a stable per-profile token in place of the guid argument (`Base.cs:24`), numbered by first appearance in the step, so logs compare.
   The `IAbilityService` double invokes every callback passed to `ApplyCombatImpact` and `ApplyTelegraphedCombatImpact` (`beforeImpact`, `baseDamageAdjustment`, `damagePercentAdjustment`, `beforeSuccessfulImpactRiders`, `afterSuccessfulHit`, `afterImpactAction`) for the scenario's creatures, and the fixture drives the `ResolvesImpactStatusCategories` delegate.
3. **States covered.** Seventeen scenarios (`WeaponAbilityCallTraceTests.Scenarios`): every unscripted check false, every check true, every check true but whether a creature has a named status (so a status can spread), checks alternating from true or from false, one in three, two in three, two then one (so a check made twice passes then fails),
   one or three creatures hit, no damage, a caster at 1 hit point, a ward and a guarded ally that were recently hit, a creature caster (not a player) with no impact summary, a target that is gone, and no damage with no summary. That covers recent or not recent targets, guarded and ward hits, using an ability, behind or beside, bleeding, controlled, low HP, each FP and stamina threshold,
   deflection, suppression stacks, critical count, all hits landed, zero damage with targets hit, area path versus direct path, one hit versus several.
4. **CombatSim readings byte-identical** before and after (the P5-3 procedure; PR 1 changes no production file, so it has none). With D1 the reader is untouched.
5. **Moved bodies verbatim.** A script compares the normalized text of each moved method with its original (token-aware). The diff of `Module/`, `SWLOR_Haks/` and the derived definitions is empty.
6. **Existing tests** (re-measured at `8a54b0841`).
   - Nine unit test files read the base source by path (16 sites, now through `WeaponAbilitySource`, which holds the part file list in one place like `EnmitySource`, so a code PR adds a file to one list and no test names a path).
     The sites that assert exact text need rewriting by the PR that moves the text: `AbilityDamageQueueTests.cs:416-439` (the telegraphed branch between `if (isArea && ShouldUseTelegraphedCombatImpact` and `var totalDamage = 0;`),
     `CombatDamageTests.cs:558, 794`, `GeneratedWeaponPerkBehaviorTests.cs:343, 437, 468, 657, 992, 1817, 1830`. `AIModelTests.cs:702`, `FirstAidCombatUpgradeTests.cs:315`, `PerCastResourceCapTests.cs:112`,
     `PerkRefundStatusEffectCleanupTests.cs:143`, `ThrowingTossAreaImpactVisualTests.cs:25` and `WeaponDeflectionTests.cs:114` read single phrases or exclude the file.
   - Five test types derive from the bases (six with the new probe): `StaminaRecoveryEngineTests.cs:19` (calls `profile.AfterImpact` at :166, now `ImpactEffects`), `PerkTrackerRegressionEngineTests.cs:294`, `AIModelTests.cs:280`, `WeaponStatusSpreadTests.cs:11` (`snapshot.Capture` at :85-94, now `StatusRiders`) and `GuardCounterControlTests.cs:82`; PR 1 adds `WeaponAbilityProbe` (`Support/`), a sixth that calls `ConfigureWeaponAbility` the same way.
   - Also run `AreaAbilityFragmentationTests`, `CrowdControlBudgetTests`, `PlayerMessageAuditTests`, the P5 baseline guards, the content injection rule tests and the architecture tests.
7. **Engine tests.** `PerkTrackerRegressionEngineTests`, `StaminaRecoveryEngineTests` and the `AbilityBehaviors` files (`Force`, `General`, `MimicryUtility`, `MimicryTechnique`, `TwinBlade`) run before and after with identical pass sets.
8. **New unit tests.** One class per part with fake services (idle windows, recent-target windows, guarded-hit status, spread cap, resource-conditional restore and drain), and the part-service-count guard.
9. **Coverage.** The gate runs with coverage as P5-4 did; no floor is lowered.
10. **Client evidence** (`InEngineVisualVerification.md`): each code PR needs live client captures of one single-target, one telegraphed area, one multi-hit, one friendly stance and one toggle ability on the same dummy before and after, with impact numbers and status results read from the combat log.

## 9. Sequencing, PRs, baselines and conflicts

**Sequencing (D6).** Under Phase 5 order rule 4 (two items never work on the same files), P5-5 (the serial chain, under a master freeze) touched this file in P5-5c, P5-5f and P5-5g. P5-5h merged (#3473) and the dead helpers went in #3506, so the code PRs can start; this map was
re-measured at `8a54b0841` in PR 1, which changes no production file. The P5-5 deltas are in the numbers above: `CreateStatusEffect` (`Base.cs:576`) and `GetStatusEffectName` (`:2000`) to the catalog, the 19 `ApplyStatusEffect` sites (`StatusEffectApplication`), `ExtendStatusEffectDuration` (`:589`) to the duration part,
and the receiver text of every `StatusEffect.X` read that stays. The `S`, `A`, `D`, `C` counts in section 3 are the final P5-5 interfaces.

**PR order.** From the refactor branch tip, each gated before the next. Parts cannot call protected statics on the base, so lower layers land first.

| PR | Content | Base after | Baselines regenerated |
|---|---|---|---|
| 1 | This map re-measured; settings and call-trace snapshot fixtures with the coverage proof; the source-reading test helper (`WeaponAbilitySource`). Tests and docs only. | 2,041 | none |
| 2 | `StatusAbilityEffects` first (the closures of `ConfigureToggle`, `ConfigureSelfStatus`, `ConfigurePartyStatus` call it), then layers 0 and 1 (`TemporaryModifiers`, `StatusRiders`, `DamageModifiers`, `SelfEffects`, `HitEffects`). The profile methods they replace stay as thin delegates to the parts through `GameServices` until PR 3. | about 1,400 | `StaticLookups`, `ServicesPerType`, `DebtReport.md` |
| 3 | Layers 2 and 3 (`HitRiders`, `ImpactEffects`, `FriendlyTargets`, `Activations`, `Impacts`); closures become one-line calls; `WeaponAbilityProfile` top level with the profile behavior and the delegates gone; the `PlayerMessageAudit.json` re-key | about 450 | `LargeTypes`, `LongMethods`, `StaticLookups`, `ServicesPerType`, `StaticState` (re-keyed), `DebtReport.md` |

The first table had a fourth PR that deleted the dead helpers; #3506 did that (section 6), so there are three PRs.

Every code PR regenerates `LargeTypes`, `StaticLookups`, `ServicesPerType` and `DebtReport.md`; the baselines are exact-match (`Phase5DebtTests.cs:16-17`) and never hand edited. After PR 3: `LargeTypes.txt` -1 (the type leaves the list), `LongMethods.txt` -1 (`ConfigureWeaponAbility`),
`ServicesPerType.txt` 24 -> about 7 (the line leaves when the base is at 10 or fewer), `StaticLookups.txt` 169 -> a few dozen lines (one `GameServices.X` per closure plus the `ConfigureWeapon` family; the build report gives the figure), `StaticState.txt` re-keyed to `WeaponAbilityProfile`.
`CrossDomainEdges`, `UpwardEdges` and `LogicToContent` are unchanged: no `Domain/` file is added and `Feature/AbilityDefinition` is the Abilities domain.

**Conflict with P5-5.** Both edit this file's `StatusEffect.X` calls. P5-16f's code PRs start after P5-5h, so there is no concurrent edit; the PRs start from a tip where the P5-5 tool has already rewritten the receiver text. If a master sync reruns a P5-5 or content tool, it runs over the new files
like any content file; this item does not rely on that.

**Master port.** Master changed this file 4 times in the month before the first measurement (`67cb382f48`, `09bb6246d5`, `6bd07d6575`, `8051b0e016`). The profile moving to its own file ends automatic merges for the 284-1528 range, and the closures moving ends them for the impact body.
After PR 3, a master edit to those ranges is ported by hand to the owning part. Each PR records the port rule in its description and in the Status file's Port log.

## 10. Owner questions

1. **Delete the ten dead helpers** (section 6): **resolved.** The owner approved it on 2026-11-17 and #3506 landed it as its own PR.
2. **First registration of the parts** (section 5). Seed `AddSingleton<T>()` lines once, or extend the analyzer so a class with instance methods under a `converted` folder is a singleton? **Recommended: seed once**, since the analyzer's shape rule is meant for classes that are only constructors plus injected properties.
3. **Who takes `AbilityTargeting`, `TemporaryHitPointEffects`, `ActionInterruption` and `UsePerkFeat` after P5-12 and C8?** **Recommended:** `AbilityTargeting` to `StatusRiders` (spread) and `FriendlyTargets` (validation); `TemporaryHitPointEffects` to `FriendlyTargets`; `ActionInterruption` and `UsePerkFeat` to `HitRiders`. All parts stay at 9 or fewer.

## 11. Not resolved

- **`HitRiders` over 10 services: resolved** in the code that built it (section 13): `ApplyTargetUsingAbilityEffects` moved to `HitEffects`, which brings `HitRiders` to 10 (13 with the later statics, once the ActionInterruption and UsePerkFeat lookups join them, so P5-12 and C8 still need to take those two out).
- The first registration mechanism (question 2), until `-Check` runs on real code.
- Exact service counts: they come from a syntax scan of the method ranges (this re-measure scanned each method of `Base.cs:284-2040` for the 24 service names and the static types), and `Stat` and `CombatQueries` appear in several ranges, so the build report decides.
- The estimate of `ConfigureWeaponAbility` (120 to 140 lines) is not measured; section 4 gives the only permitted fallback.
- **Coverage left by the PR 1 fixture** (1,387 of 1,390 lines and 1,416 of 1,436 branches reached) : `Base.cs:1432` and `:1497` are guards that cannot be reached (`ApplyFriendlyTargetStatus` and `ApplySelfStatusEffect` run only after `HasFriendlyTargetStatus` and `HasImmediateSelfStatusEffect` have proved the factory is not null),
  `:2030` is the same kind (`ObjectSnapshot.InShape` returns only valid objects); `:318, :320, :415, :417` are the `?.` on an idle snapshot the closures always pass (only direct callers pass none); `:1354` is a duration that is never zero (its callers pass 30 at least); `:1873` and `:930` keep one outcome no shipped or probed profile reaches
  (a drawn shape the ability never uses; a duration with every amount zero). The map's gate for the parts is that the same fixture, run on the new parts, shows the moved ranges at least this covered.

## 12. PR 2 as built

Built from `43ef913f1`. The status ability effects and layers 0 and 1 are in `Feature/AbilityDefinition/WeaponAbility/`. Each part is a content singleton: it was written as a `static class` that reaches services through `GameServices`, and
`Apply-ContentInjection.ps1` made it an instance class with constructor injection (`-Check` is clean, 0 refusals, 32 singletons). The base reaches an entry part through a temporary property of the same name in `Composition/GameServices.WeaponAbility.cs`.

| Part | Lines (file) | Constructor services | What it holds |
|---|---|---|---|
| `StatusAbilityEffects` | 93 | 6: `Ability`, `CombatRecorder`, `Party`, `S`, `A`, `C` | `ToggleSelfStatus`, `ApplyToggleStatus`, `ApplySelfStatus`, `ApplyStatusToNearbyParty` (`Func` and `Type` overloads) |
| `WeaponAbilityTemporaryModifiers` | 183 | 1: `TemporaryStatModifier` | `ReplaceTemporary`, `ReplaceTemporaryTarget`, `ReplaceTemporaryPayload`, `ApplyTemporaryDefeatedEnemyModifiers` |
| `WeaponAbilityStatusRiders` | 227 | 7: `CombatActivity`, `CombatDefenseMemory`, `CombatQueries`, `CombatSuppression`, `S`, `A`, `C` | `GetAppliedCategories`, `ApplyConditionalStatus`, `ApplyTargetConditionalStatus`, `ApplyStatusSpread`, `SpreadStatusToNearbyHostile`, `ApplySuppressionEffects`, `IsBleeding`, `ReadSpreadSource` |
| `WeaponAbilityDamageModifiers` | 331 | 9: 8 services (`CombatAbilityBonus`, `CombatActivity`, `CombatDefenseMemory`, `CombatQueries`, `CombatResources`, `PlayerFeedback`, `Stat`, `S`) and `WeaponAbilityStatusRiders` | the damage, percent, critical rate and defense ignore adjustments, `ResolveImpactStatus`, the idle snapshots and activation preparation |
| `WeaponAbilitySelfEffects` | 171 | 10: 8 services (`AbilityImmunity`, `CombatAbilityActivation`, `CombatDamageTaken`, `CombatRecorder`, `CombatResources`, `Enmity`, `A`, `TemporaryStatModifier`) and the parts `StatusAbilityEffects` and `WeaponAbilityTemporaryModifiers` | `SpendHitPoints`, `AfterActivation`, `AfterHostileActivation`, `ApplyLandedImpactModifiers` and the private self effects |
| `WeaponAbilityHitEffects` | 113 | 5: `CombatActivity`, `CombatResources`, `Stat`, `A` and `WeaponAbilityTemporaryModifiers` | the resource restore and drain, the target modifiers and the protected-target self modifiers |

Base after: `WeaponActiveAbilityDefinitionBase.cs` is 500 lines (2,041 before) with 27 static lookup lines (169 before) and no longer appears in `ServicesPerType.txt` (24 before) or `LargeTypes.txt`.
`WeaponAbilityProfile.cs` is 758 lines and appears in `ServicesPerType.txt` with 15 services (all lookups) and 70 lookup lines; it still holds the bodies PR 3 moves (`HitRiders`, `ImpactEffects`, `FriendlyTargets`, `Impacts` and the activation closure).
Baselines regenerated: `StaticLookups`, `ServicesPerType`, `LargeTypes`, `StaticState` (re-keyed to `WeaponAbilityProfile`), `DebtReport.md`, `PlayerMessageAudit.json` (39 rows re-keyed). No edge key was added or removed, so `CrossDomainEdges.txt` and `EdgesIntoCoreAndPlatform.txt` are unchanged.

Departures from the map:

- **The profile became a top-level type in this PR, not PR 3.** A part takes the profile as its first parameter, and a `protected` nested type cannot be named in the public signature of a part, so `WeaponAbilityProfile` moved to `Feature/AbilityDefinition/WeaponAbilityProfile.cs` (public, same namespace, so the derived files need no `using`).
  Its two snapshot types stay nested; `ModifierSource` is an `internal` get-only property, so it is not one of the public settings the fixtures read. The base is therefore 500 lines now, not about 1,400, and PR 3 starts from a profile that is already its own file.
- **Registration needed no seed line.** The parts are static classes at the start, so the tool treats them as singleton candidates (section 5's question 2 did not arise); `Composition/ContentRegistrations.cs` gained six lines.
- **Delegates.** The 12 public profile methods the closures and tests call stay as one-line delegates to the parts (`GameServices.WeaponAbility...`). The private methods that moved have no delegate: the profile code that still calls them (`AfterSuccessfulHit`, `AfterImpact`, `BeforeSuccessfulImpactRiders`,
  `ApplyTargetUsingAbilityEffects`) calls the part directly with `this`. `StatusSpreadSnapshot.Capture` reads the two statuses through `WeaponAbilityStatusRiders.ReadSpreadSource` and keeps its cache.
- **`DamageModifiers` calls `StatusRiders`** (`IsBleeding`, public on `StatusRiders`) so the one-line predicate has one home; this adds one part dependency (9 services) and keeps the layer order (1 calls 0).
- **`HitEffects` has no `IContentFactory`.** `ChallengeEnmityStatusEffect` is not content that takes services, so `new ChallengeEnmityStatusEffect(...)` stays; the part takes 5 (4 services and `TemporaryModifiers`), not the 6 of the map.
- **`SelfEffects` is at the limit of 10** (8 services and 2 parts: it calls `StatusAbilityEffects.ApplyStatusToNearbyParty` and `TemporaryModifiers`); `StatusAbilityEffects` gained the `Type` overload `ApplyNearbyPartyStatus` needs.
- The base's closures of `ConfigureToggle`, `ConfigureSelfStatus` and `ConfigurePartyStatus` are one call each into `StatusAbilityEffects`; the protected statics `ToggleSelfStatus` and `ApplyStatusToNearbyParty` keep their signatures and forward to it.

Tests: `WeaponAbilityProfileReader` finds the profile type as `typeof(WeaponAbilityProfile)`; `WeaponAbilitySource` lists the new files; `GeneratedWeaponPerkBehaviorTests`, `CombatDamageTests` and `WeaponDeflectionTests` read the injected service text (`CombatAbilityBonus.X(...)` for `GameServices.CombatAbilityBonus.X(...)`) and one
indentation changed with the profile's move out of the base class; `PerkRefundStatusEffectCleanupTests` skips `StatusAbilityEffects.cs` like the base; `WeaponAbilityPartTests` fails a part over 10 constructor services or a part cycle. The settings and call-trace fixtures are unchanged.

### PR 2 proof beyond the fixtures

- **Moved bodies verbatim (section 8, item 5).** A token-aware comparison of the 75 methods that existed in `Base.cs` at `43ef913f1` (57 in the profile class, 18 in the base) with their text now: 72 are token-identical once `static` and the access modifiers, the
  `GameServices.` and part-name receivers, the `profile.` or `this` receiver (and the `profile` parameter and first argument) and the `WeaponAbilityProfile.` nested-type prefix are removed. The three that differ moved on purpose: the snapshot's `Capture` reads
  the two statuses through `WeaponAbilityStatusRiders.ReadSpreadSource`, and the toggle and self-status closure bodies of `ConfigureToggle` and `ConfigureSelfStatus` became calls into `StatusAbilityEffects`, whose bodies hold the same lines. The diff of `Module/`, `SWLOR_Haks/` and every derived definition against `43ef913f1` is empty.
- **Part tests (item 8).** One class per part with recording stand-ins for the services (`WeaponAbilityPartBench`): idle windows and the activation snapshot, recent-target windows, guarded-hit status, low hit points and bleeding bonuses, critical rate, the conditional and spread riders (the snapshot reads once), suppression, the resource restore and drain, target modifiers, the hit point spend,
  haste and immunities, the toggle, self status and party status. The spread cap itself and the ward and guarded-ally checks need creature state the fakes cannot give; `WeaponStatusSpreadTests` covers the cap and the call-trace fixture drives the guarded and ward paths.
- **Engine tests (item 7).** `PerkTracker`, `StaminaRecovery`, the Force, General, Mimicry utility, Mimicry technique and Twin Blade ability behavior sweeps and every `Characterization` scenario (79 tests) run in the engine container at `43ef913f1` and at the branch head: 79 of 79 pass in both with identical per-test results, and the Characterization snapshots (Ability/* and the weapon proc and attack scenarios) matched with no snapshot change.

## 13. PR 3 as built

Built from `38311bdeb`. The hit riders, impact effects, friendly targets, activations and hostile impacts are in `Feature/AbilityDefinition/WeaponAbility/`, with `WeaponAbilityImpactRequest` (a `readonly record struct` of the closure's captured values, per D1) beside the profile. As in PR 2 the parts were written as `static` classes and
`Apply-ContentInjection.ps1` made them instance classes with constructor injection (`-Check` clean, 42 singletons).

| Part | Lines (file) | Constructor services | What it holds |
|---|---|---|---|
| `WeaponAbilityHitRiders` | 189 | 10: 6 services (`CombatResources`, `Stat`, `StatusEffect`, `StatusEffectApplication`, `StatusEffectDuration`, `TemporaryStatModifier`), `IAbilityImpactState` and the parts `DamageModifiers`, `HitEffects`, `StatusRiders` | `AfterSuccessfulHit`, `BeforeSuccessfulImpactRiders`, the source status extend and consume, the defense ignore hit effect |
| `WeaponAbilityImpactEffects` | 154 | 8: 5 services (`CombatDefenseMemory`, `CombatResources`, `PlayerFeedback`, `Stat`, `StatusEffectApplication`), `TemporaryStatModifier` and the parts `DamageModifiers`, `SelfEffects` | `RestoreResourcesForTargetCount`, `AfterImpact` |
| `WeaponAbilityFriendlyTargets` | 236 | 5: `Ability`, `StatusEffect`, `StatusEffectApplication`, `TemporaryStatModifier` and the part `SelfEffects` | friendly and hostile target validation, the friendly status and effects, the self status, `ApplyFriendlyImpact` |
| `WeaponAbilityActivations` | 52 | 4: `CombatAbilityBonus`, `CombatActivity` and the parts `StatusAbilityEffects`, `DamageModifiers` | `ApplyActivation` |
| `WeaponAbilityImpacts` | 228 | 9: 4 services (`Ability`, `AbilityDamageInput`, `AbilityStatusApplication`, `CombatAbilityHit`), `IAbilityImpactState` and the parts `DamageModifiers`, `HitRiders`, `ImpactEffects`, `SelfEffects` | `ApplyHostileImpact` (direct and telegraphed paths), `ResolveStatusCategories` and the shape helpers |

`WeaponAbilityHitEffects` grew to 138 lines and 6 constructor parameters: it now holds `ApplyTargetUsingAbilityEffects` and takes `StatusRiders` (layer 0).

After: `WeaponAbilityProfile.cs` is 283 lines (758 before): data only, plus the two nested snapshot types; it left `ServicesPerType.txt` (15 services, all lookups) and its static lookup lines went from 70 to 2.
`WeaponActiveAbilityDefinitionBase.cs` is 311 lines (500 before) with 13 static lookup lines (27 before); `ConfigureWeaponAbility` left `LongMethods.txt` (291 lines). `GameServices.WeaponAbility.cs` keeps 5 temporary properties.
Baselines regenerated: `LongMethods`, `ServicesPerType`, `StaticLookups`, `DebtReport.md`, `PlayerMessageAudit.json` (15 rows re-keyed). `CrossDomainEdges.txt` and `EdgesIntoCoreAndPlatform.txt` were regenerated and reverted: they changed only in member counts, no edge key was added or removed.

**`HitRiders` resolution (section 11):** `ApplyTargetUsingAbilityEffects` moved into `HitEffects`, which already held `CombatActivity`, `Stat` and `A`. That brings `HitRiders` to 10 and `HitEffects` to 6.

Departures from the map:

- The `HasImpactAction` closure keeps an if/else of two one-line calls (`ApplyFriendlyImpact` and `ApplyHostileImpact`), since the original closure returned early from its friendly branch.
- Some helpers on `FriendlyTargets` stay `static` (`HasFriendlyTargetStatus`, `HasFriendlyTargetEffects`, `ResolveFriendlyTarget`, `HasImmediateSelfStatusEffect`): the tool leaves a method static when it reaches no service.
- Part-to-part dependencies were wired by hand where the tool had no seed for them, and the layer order holds (0 `StatusRiders`, `TemporaryModifiers`; 1 `DamageModifiers`, `HitEffects`, `SelfEffects`; 2 `HitRiders`, `ImpactEffects`, `FriendlyTargets`, `Activations`; 3 `Impacts`).
- Five temporary `GameServices` properties remain: `StatusAbilityEffects`, `WeaponAbilityActivations`, `WeaponAbilityFriendlyTargets`, `WeaponAbilityImpacts` (the static base's closures call them; D1 forbids a static property on content, so the base reaches them through `GameServices`) and `WeaponAbilityStatusRiders`
  (the status spread snapshot is a data type the container does not build, and its `Capture` reads the source statuses through it). P5-14 converts the base and the snapshot, and the five go with them.
- The profile's 12 one-line delegates from PR 2 are gone: the callers (the closures and the parts) call the parts directly.
- The `request` struct values are read back into locals at the top of `ApplyHostileImpact` so the body is the closure's text unchanged.

### PR 3 proof beyond the fixtures

- **Part tests.** One class per new part with recording stand-ins (`WeaponAbilityPartBench`, which gained `RunCalls` for "made no call" checks): `WeaponAbilityHitRidersTests`, `WeaponAbilityImpactEffectsTests`, `WeaponAbilityFriendlyTargetsTests`, `WeaponAbilityActivationsTests`, `WeaponAbilityImpactsTests`, and three tests of `ApplyTargetUsingAbilityEffects` in `WeaponAbilityHitEffectsTests`.
  The impacts tests make the ability service stand-in run the callbacks of each impact, so the closures built in the hit run. Not reachable with fakes: the heal amount (an opaque engine effect; the tests pin when it is applied), the friendly validation that asks `AbilityTargeting` about a creature, the guarded and ward lookups, and the spread cap; the call-trace fixture drives the first three against the engine fake and `WeaponStatusSpreadTests` covers the cap.
- **Moved bodies verbatim.** A token-aware comparison (`C:/Temp/mr-p5-16f4/cmp`, Roslyn tokens; removed: `static` and access modifiers, `GameServices.`, service and part receivers, `profile.` and `this.`, the profile parameter and first argument, the nested-type prefix, the request-struct alias lines) of every method of the two files at `38311bdeb` with its text now: 43 token-identical
  (37 methods: 21 moved, 16 stayed, and the 6 closure bodies: activation, status categories, the friendly and hostile halves of the impact, and two validations), 12 profile delegates removed (PR 2's one-liners), 0 different. The `ResolveStatusCategories` ternary and the closures that became methods with local aliases are token-identical once those aliases are dropped, so no rewrite remained to explain; one indentation was fixed after the move. The diff of `Module/`, `SWLOR_Haks/` and every derived definition file against `38311bdeb` is empty (9 files changed under `Feature/AbilityDefinition`: the profile, the base, `WeaponAbilityImpactRequest`, `HitEffects` and the five new parts).
- **Unit tests beyond the part tests** (`SWLOR.Game.Server.Tests`, `--no-build`, one filter of `FullyQualifiedName~` alternatives: `AreaAbilityFragmentation`, `GuardCounterControl`, `WeaponStatusSpread`, `WeaponDeflection`, `CrowdControlBudget`, `AIModel`, `WeaponAbility` (the part tests, `WeaponAbilityPartTests`, the settings and call-trace fixtures), `FirstAidCombatUpgrade`, `ThrowingTossAreaImpactVisual`, `AbilityDamageQueue`, `CombatDamage`, `GeneratedWeaponPerkBehavior`, `PerCastResourceCap`, `PerkRefundStatusEffectCleanup`, `PlayerMessageAudit`, `ConvertedServiceContainer`):
  1,394 of 1,394 pass (they read `SWLOR_Haks` data, so the run used a copy of the pinned haks' `sw_2da`, `sw_ability`, `sw_tlk`, `sw_shader`, `sw_tint_mtr` and `tools`). `SWLOR.Architecture.Tests`: 297 of 297.
- **Coverage (item 9 gate).** `dotnet-coverage` on `WeaponAbilityCallTraceTests` alone, over the 14 files that now hold the old range (base, profile, request, the 11 parts): 1,826 of 1,835 lines. Every line and branch PR 1 listed as unreachable is still the only thing left in the parts (the two friendly guards, the `ObjectSnapshot.InShape` guard, the idle snapshot `?.`, the duration that is never zero, the shape and amount outcomes no profile reaches), and the parts
  that PR 3 built (`HitRiders`, `ImpactEffects`, `Activations`, `Impacts`, `HitEffects`, the profile) are covered in every line (137, 110, 20, 179, 90 and 216 lines; 58 of 58, 82 of 82, 10 of 10, 38 of 38, 50 of 50 and 8 of 8 branches). The six lines left in the base are the two `ApplyStatusToNearbyParty` forwarders PR 2 kept for `RampartAbilityDefinition` and `SoulStormAbilityDefinition`; PR 2's closures call the part directly, so the fixture no longer reaches them (the engine tests do, through those abilities).
  The coverage is Cobertura line and condition counts, not PR 1's native counts, so the totals do not compare; the unreached sets do.
- **Engine tests (item 7).** `PerkTracker` (26), `StaminaRecovery` (7), the Force, General, Mimicry utility, Mimicry technique and Twin Blade behavior sweeps (5) and every `Characterization` scenario (41): 79 of 79 pass at `38311bdeb` and at the head, in eight runs each, with identical per-test outcomes and no snapshot difference.
- **Client captures** (NWN client, two Release server builds with a temporary event-gated driver that is not committed; same module, haks and steps; screenshots and normalized chat in `C:/Temp/mr-p5-16f4/evidence/{base,head}`): Double Shot I (hostile direct multi-hit), Blade Vortex I against three targets (telegraphed area path and the restore by target count: "Restored 4 STM"),
  Pathogen Strike I after Venom and Infection (the source status extension: "Extended 2 statuses by 4s") and Aimed Shot I (the activation snapshot path: "readies Aimed Shot I", "Aimed Shot +8 DMG" on the hit). The normalized chat lines match (numbers masked) except the hit or miss of the attack roll and the number of the dummy's swings, which are random; Aimed Shot I missed once on the head and hit three times on a repeat, with the same lines as the base.
  **Not captured: the friendly-target ability on a second player in the caster's party.** The stack tool sets up a profile only for the first client, and the second client's profile was not set up in this run. Steel Shoulder is covered by the friendly target part tests, the call-trace fixture and the PR 2 client run, whose friendly code is unchanged in text (token-identical, above).
