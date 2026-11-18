# Maintainability Refactor: WeaponActiveAbilityDefinitionBase Split (P5-16f)

Part map, revised after the coordinator's review (decisions D1 to D7 in section 2 are binding); not yet approved by the owner. No code changes in this commit. Measured against
`feature/maintainability-refactor` at `d43e40fa8`. Limits come from [MaintainabilityRefactorPhase5.md](MaintainabilityRefactorPhase5.md): 1,000 lines and 10 services per type,
150 lines per method, no new static service lookups, no new upward domain references, no `Part1`/`Part2` chunks. `Base.cs:N` means line N of
`Feature/AbilityDefinition/WeaponActiveAbilityDefinitionBase.cs` at that commit. Estimates are rounded to about 10; the build report gives exact numbers.

## 1. What the type is

One abstract class, 2,332 lines, no constructor, no handlers, no carried state. It is the shared base of ability definitions (`IAbilityListDefinition`) and reaches every service through
`using static GameServices`.

| Fact | Number |
|---|---|
| Direct subclasses | 120: 116 definitions and 4 intermediate bases (`HeavyVibrobladeActiveAbilityDefinitionBase`, `TwinPistolAbilityDefinitionBase`, `UnarmedAbilityDefinitionBase`, `SpearActiveAbilityDefinitionBase`) |
| Definitions below the bases | 31 (11 Heavy Vibroblade, 10 Twin Pistol, 10 Unarmed; the Spear base has none) |
| `ConfigureWeaponAbility` call sites | 212 in 103 production files (214 in 105 with tests) |
| `new WeaponAbilityProfile` | 148 sites in 68 production files (157 in 72 with tests) |
| Helpers with callers outside the base | `ConfigureToggle` 11, `ConfigureSelfStatus` 3, `ConfigurePartyStatus` 1, `ToggleSelfStatus` 3, `ApplyStatusToNearbyParty` 2, `ConfigureWeapon` 1 |
| Built by | `AbilityCatalogService` calls `Activator.CreateInstance(type)` for every `IAbilityListDefinition` at `mod_cache_bef`; `BuildAbilities()` runs the subclass's static helpers, which call the protected statics. 33 test files call `BuildAbilities()` with no container. |
| Handlers | none (`ScriptHandlerOrder.txt` has no line) |
| State | none carried. `StaticState.txt` counts 1: the `WeaponAbilityProfile.Empty` shared instance |
| `ServicesPerType.txt`, `StaticLookups.txt`, `LargeTypes.txt`, `LongMethods.txt` | 21 services (all by lookup), 187 lookup lines, 2,332 lines, `ConfigureWeaponAbility` 291 lines |

Anatomy:

| Lines | What | Size |
|---|---|---|
| 22-283 | `WeaponAbilityProfile` data: 188 `init` properties, plus 4 on the nested snapshot types | 260 |
| 284-1528 | `WeaponAbilityProfile` behavior: 54 methods | 1,245 |
| 1530-1886 | `ConfigureWeaponAbility` (291 lines) and its targeting helpers | 357 |
| 1887-2060 | `ConfigureWeapon`, `ConfigureCastedTarget`, `ConfigureMultiHit`, `ConfigureInterrupt` | 175 |
| 2067-2331 | status helpers | 265 |

The 21 services: `Ability`, `AbilityDamageInput`, `AbilityHostileImpact`, `AbilityImmunity`, `AbilityImpactState`, `AbilityStatusApplication`, `CombatAbilityActivation`, `CombatAbilityBonus`,
`CombatAbilityHit`, `CombatActivity`, `CombatDamageTaken`, `CombatQueries`, `CombatRecorder`, `CombatResources`, `CombatSuppression`, `Enmity`, `Party`, `PlayerFeedback`, `Stat`, `StatusEffect`,
`TemporaryStatModifier`. It also reaches static content and helpers: `GuardedStatusEffect` (`Base.cs:1191, 1229, 1408, 1426, 1484, 1521`), `WardBondStatusEffect.HasRecentWardHit` (1197, 1235, 1392),
`AbilityTargeting` (1357, 1403), `TemporaryHitPointEffects.ApplyFlat` (1468), `ActionInterruption.ClearActions` (516, 2038), `UsePerkFeat.InterruptAbilityActivationFromHit` (517), `ObjectSnapshot.InShape` (2210, 2242).

Content named by the behavior is nine `Feature/StatusEffectDefinition` types: `BleedStatusEffect`, `HemorrhageStatusEffect`, `SunderStatusEffect` (snapshot `Capture` at `Base.cs:53`, spread at 1284-1308),
`InfectionStatusEffect` (379), `WardBondStatusEffect` (1197, 1235, 1392), `GuardedStatusEffect`, `DisorientedStatusEffect` (1339), and by `new`: `ChallengeEnmityStatusEffect` (1238, 1245) and `FoggyMindStatusEffect` (2193).

Constraints that shape the design:

1. The builder wiring must work without a container: `FacadeCache.Get` throws without one (`Composition/FacadeCache.cs:27-37`).
2. The profile is data plus behavior. Behavior that needs a service moves to a service (`MaintainabilityRefactorContentInjection.md` decision 1 and lines 121 and 576).
3. A `Domain/` type naming those nine content types would add `LogicToContent.txt` lines, so the parts live in `Feature/AbilityDefinition`, which `DomainMap.json` counts as the Abilities domain. Domain edges stay as they are.
4. `SWLOR.CombatSim/Data/WeaponImpactReader.cs` reads the impact closure by method name `<ConfigureWeaponAbility>` and captured names (`baseDamage`, `duration`, `statusEffect`, `additionalStatusEffects`, `profile`, `skill`, `restoresSecondaryTargetStamina`), and compares public properties with a default profile (`UnmodeledRiders`, `Reader.cs:42-48`). `AreaAbilityFragmentationTests.cs:191` and `GuardCounterControlTests.cs:118` read the same closures.

## 2. Decisions (coordinator, binding)

- **D1 Shape.** The builder wiring stays static and pure on the base. Only run-time behavior moves into constructor-injected content singletons. Each closure body inside `ConfigureWeaponAbility` becomes one
  call resolved at run time: a direct `GameServices.X.Method(...)` inside the lambda, not a static property (`Service_typed_members_of_content_are_private_and_named_like_the_old_property`,
  `ContentInjectionRules.cs:141`, rejects static or protected service properties on content). The closures stay in `<ConfigureWeaponAbility>` and capture the same names, so the CombatSim reader,
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
| 0 | `WeaponAbilityTemporaryModifiers` | Writing temporary stat modifiers: `ReplaceTemporary`, `ReplaceTemporaryPayload`, `ReplaceTemporaryTarget`, `ApplyTemporaryDefeatedEnemyModifiers`; takes the profile's modifier source as an argument | ~185 | 1: `TemporaryStatModifier` | none | none |
| 0 | `WeaponAbilityStatusRiders` | Conditional statuses, spread, suppression: `GetAppliedCategories`, `ApplyConditionalStatus`, `ApplyTargetConditionalStatus`, `ApplyStatusSpread`, `SpreadStatusToNearbyHostile`, `ApplySuppressionEffects`, snapshot capture | ~230 | 8: `CombatActivity`, `CombatDamageTaken`, `CombatQueries`, `CombatSuppression`, `Stat`, `S`, `A`, `C` | none | +`AbilityTargeting` = 9 |
| 0 | `StatusAbilityEffects` | Run-time bodies of toggle, self and party status abilities: `ToggleSelfStatus`, `ApplyStatusToNearbyParty` x2, the toggle and self-status closure bodies | ~170 | 5: `Ability`, `CombatRecorder`, `Party`, `S`, `A` | none | none |
| 1 | `WeaponAbilityDamageModifiers` | Damage, percent damage, critical rate, defense ignore, idle snapshots and activation prep, with private predicates: `GetBaseDamageAdjustment`, `GetDamagePercentAdjustment`, `GetCriticalRateAdjustment`, `GetDefenseIgnorePercent`, `ResolveImpactStatus`, `CaptureActivationIdleBonusSnapshot`, `ClearActivationIdleBonusSnapshots`, `PrepareQueuedActivation`, `PrepareActivationIdleBonuses`, `IsIdle`, `IsTargetBelowThreshold`, `IsBleeding` | ~330 | 8: `CombatAbilityBonus`, `CombatActivity`, `CombatDamageTaken`, `CombatQueries`, `CombatResources`, `PlayerFeedback`, `Stat`, `S` | none (the predicates are private; no other part uses them) | none |
| 1 | `WeaponAbilitySelfEffects` | What the caster gets: `SpendHitPoints` (uses `CombatAbilityActivation`, `Base.cs:508`), `AfterActivation`, `AfterHostileActivation`, `ApplySelfInvisibility`, `ApplyNearbyPartyStatus`, `ApplySelfImmunity`, `ApplySelfModifiers`, `ApplyLandedImpactModifiers` | ~160 | 8: `CombatAbilityActivation`, `CombatRecorder`, `CombatDamageTaken`, `CombatResources`, `AbilityImmunity`, `Enmity`, `S`, `A`; plus 1 part = 9 | `WeaponAbilityTemporaryModifiers` | none |
| 1 | `WeaponAbilityHitEffects` | Per-hit resource and modifier effects: `ApplyResourceConditionalRestore`, `ApplyResourceConditionalDrain`, `IsFPAtOrAbove`, `IsStaminaAtOrAbove`, `ApplyTargetTemporaryModifiers`, `ApplyProtectedTargetHitSelfModifiers` | ~110 | 5: `CombatResources`, `Stat`, `S`, `A`, `IContentFactory` (for `ChallengeEnmityStatusEffect`); plus 1 part = 6 | `WeaponAbilityTemporaryModifiers` | none |
| 2 | `WeaponAbilityHitRiders` | What each landed hit triggers: `AfterSuccessfulHit`, `BeforeSuccessfulImpactRiders`, `ExtendSourceStatusEffects`, `ConsumeSourceStatusEffects`, `ApplyDefenseIgnoreHitEffects`, `ApplyTargetUsingAbilityEffects`. The `ApplyRangedAbilityTargetDefenseReduction` call stays in the closure body that makes it. | ~195 | 4: `AbilityImpactState`, `Stat`, `S`, `D`; plus 3 parts = 7 | `WeaponAbilityHitEffects`, `WeaponAbilityStatusRiders`, `WeaponAbilityDamageModifiers` (`GetDefenseIgnorePercent`, `Base.cs:637`) | +`ActionInterruption`, `UsePerkFeat` = 9 |
| 2 | `WeaponAbilityImpactEffects` | After the last hit: `RestoreResourcesForTargetCount`, `AfterImpact` | ~140 | 6: `CombatDamageTaken`, `CombatResources`, `PlayerFeedback`, `Stat`, `S`, `A`; plus 2 parts = 8 | `WeaponAbilitySelfEffects` (`ApplyLandedImpactModifiers`), `WeaponAbilityDamageModifiers` (`ClearActivationIdleBonusSnapshots`) | none |
| 2 | `WeaponAbilityFriendlyTargets` | Friendly and self-status abilities and target validation: `HasFriendlyTargetStatus`, `HasFriendlyTargetEffects`, `ValidateHostileTarget`, `ValidateFriendlyTargetStatus`, `ResolveFriendlyTarget`, `ApplyFriendlyTargetStatus`, `ApplyFriendlyTargetEffects`, `HasImmediateSelfStatusEffect`, `ApplySelfStatusEffect`, `ApplySelfStatusToGuardedTarget`, and the friendly impact body | ~240 | 4: `Ability`, `TemporaryStatModifier`, `S`, `A`; plus 1 part = 5 | `WeaponAbilitySelfEffects` (`AfterActivation`) | +`AbilityTargeting`, `TemporaryHitPointEffects` = 7 |
| 2 | `WeaponAbilityActivations` | The activation closure body | ~50 | 2: `CombatActivity`, `CombatAbilityBonus`; plus 2 parts = 4 | `WeaponAbilityDamageModifiers`, `StatusAbilityEffects` | none |
| 3 | `WeaponAbilityImpacts` | The hostile impact body (telegraphed area path and direct multi-hit path), `ResolveStatusCategories`, the pure area helpers (`ShouldUseTelegraphedCombatImpact`, `ToCombatImpactAreaShape`, `CenterAreaOnActivator`) | ~250 | 5: `Ability`, `AbilityDamageInput`, `AbilityImpactState`, `CombatAbilityHit`, `AbilityStatusApplication`; plus 4 parts = 9 | `WeaponAbilityHitRiders`, `WeaponAbilityImpactEffects`, `WeaponAbilityDamageModifiers`, `WeaponAbilitySelfEffects` | none. P5-5f adds `A` to it only if the body applies a status; the friendly branch that did moved to `FriendlyTargets`. |
| - | `WeaponAbilityProfile` (top level, namespace `Feature.AbilityDefinition`, so the 68 files need no `using`) | Data only: 188 properties, `Empty`, the two snapshot data types. `ModifierSource` is an `internal` get-only property (a public one would show up in `UnmodeledRiders`). Keeps its parameterless constructor and property names. Becomes `public` (tests and CombatSim read it). | ~300 | 0 | none | none |
| - | `WeaponActiveAbilityDefinitionBase` (stays) | Static, pure builders and the protected statics the 150 definitions call; closure bodies are one-line calls into the parts | ~450 (about 600 with the dead helpers kept) | about 7 by lookup: `WeaponAbilityActivations`, `WeaponAbilityImpacts`, `WeaponAbilityFriendlyTargets`, `StatusAbilityEffects`, `Ability` (the `ConfigureWeapon` family), plus what the build finds | n/a | n/a |

Layer order: 0 `TemporaryModifiers`, `StatusRiders`, `StatusAbilityEffects`; 1 `DamageModifiers`, `SelfEffects`, `HitEffects`; 2 `HitRiders`, `ImpactEffects`, `FriendlyTargets`, `Activations`; 3 `Impacts`. A part calls only lower layers.
The first map had two cycles; both are gone. Cycle A (`HitRiders` <-> `StatusRiders` through `GetAppliedCategories`): `GetAppliedCategories` lives in `StatusRiders` and `HitRiders` calls it one way. Cycle B
(`HitRiders` <-> `ImpactEffects` through `ApplyTargetUsingAbilityEffects`): that method moved to `HitRiders` with the other per-hit methods, and the per-hit self effects went to `HitEffects`. `ApplyLandedImpactModifiers` is in `SelfEffects`.
`ReplaceTemporary*` are in the layer 0 leaf, so `SelfEffects` and `HitEffects` share it without calling each other. `No_two_content_singletons_depend_on_each_other` and the tool's `RefuseCycles` check this.

Totals: about 2,060 lines of new types, about 300 of profile data and about 450 to 600 on the base, against 2,332 now. No part is over 330 lines or 9 services. Tightest rows: `StatusRiders` and `HitRiders` (9 with the later statics),
`SelfEffects` and `Impacts` (9). If the build counts one more, move a member group to the lower neighbor that already calls it, never a new numbered type. Check the `Stat` and `CombatQueries` counts first.

The six rider types (`DamageModifiers`, `StatusRiders`, `HitRiders`, `HitEffects`, `ImpactEffects`, `SelfEffects`) depart from "data plus a rider singleton" (`MaintainabilityRefactorContentInjection.md` lines 121 and 576). One rider
singleton would be a type of about 1,245 lines reaching 16 services, which is the debt this item removes. So the riders are split by caller into the layers above and the data stays one plain class.

Status types stay named in Feature code: `DamageModifiers` names `InfectionStatusEffect`; `StatusRiders` names `BleedStatusEffect`, `HemorrhageStatusEffect`, `SunderStatusEffect`, `DisorientedStatusEffect`, `WardBondStatusEffect`, `GuardedStatusEffect`;
`HitEffects` names `WardBondStatusEffect`, `GuardedStatusEffect`, `ChallengeEnmityStatusEffect`; `FriendlyTargets` names `WardBondStatusEffect` and `GuardedStatusEffect`. `FoggyMindStatusEffect` is named only in the dead `FoggyMind` helper (section 6).
`ChallengeEnmityStatusEffect` takes a value, so `HitEffects` builds it with `_content.Create<ChallengeEnmityStatusEffect, int>(percent)` and counts `IContentFactory`.
The four `Activator.CreateInstance(type)` status factories (`Base.cs:2069, 2089, 2130, 2198`) stay as they are; C5 and C6 replace them (`MaintainabilityRefactorContentInjection.md:627`).

## 4. `ConfigureWeaponAbility` under 150 lines

By D1 alone, not by cutting steps for the metric. The 22-parameter signature (24 lines) stays. The activation, impact, status-category, hostile-validation and friendly-validation closures each shrink to one call
(`GameServices.WeaponAbilityImpacts.ApplyHostileImpact(...)` and so on), so the 291 lines lose the roughly 170-line hostile impact body, the roughly 55-line friendly branch and the activation and category bodies.
Estimate: 120 to 140 lines; the build measures it. The builder chain, kind and target-shape blocks and requirement calls stay in place. If the measured result is over 150, the three non-closure blocks (hostile, friendly and self-status settings) become
private static methods; that is the only cut permitted, decided by the measurement. Closure order, captured variables and evaluation order are unchanged. `ApplyHostileImpact` keeps `criticalRatePercentAdjustment` computed on every hit
(`Base.cs:1741`, not built once) and the separate area and direct paths (`Base.cs:1666-1702` and `1751-1752`): there is no shared `FinishImpact`, because the two paths clear the idle snapshot and call `AfterImpact` differently.

## 5. Registration and the guards that enforce counts

- Add `SWLOR.Game.Server/Feature/AbilityDefinition/WeaponAbility/` to `converted` in `tools/Refactor/ContentInjection.json` and run `pwsh -NoProfile -File tools/Refactor/Apply-ContentInjection.ps1 -Check` first.
- The analyzer treats a class as a singleton only if it is static, already registered, or has no instance members besides constructors (`ContentAnalyzer.cs:98-101`). The parts have instance methods, so by shape they are not candidates.
  **Unresolved:** how the first registration happens. Plan: seed one `services.AddSingleton<T>()` line per part in `Composition/ContentRegistrations.cs` once (the tool reads the registered set from that file and keeps the lines after), run `-Check`,
  and confirm with the tool's owner before the PR (question 2).
- The tool refuses a type over 10 services (`ContentAnalyzer.cs:53`) and rejects cycles (`RefuseCycles`). `ServicesPerType.txt` counts only types that `ServiceRegistrations` registers (`Debt/DebtMeasures.cs:197-200`), so it does not count the parts.
  Counts are enforced by the tool's refusal plus a new unit test in the PR that fails any part over 10 constructor services; `No_two_content_singletons_depend_on_each_other` enforces the layer order.
- The base reaches each entry part through a temporary `GameServices` property named like the type (`GameServices.WeaponAbilityImpacts`), in `Composition`, removed at C11 with the rest. It is the only static lookup the item adds; the base's own lookups fall from 187 lines.

## 6. Dead helpers (D7)

Eleven protected helpers have no caller outside the base. Checked against the base itself (grep over `Feature/` and tests, then lines inside the base): ten are dead, one is live.

| Helper | Called by | Verdict |
|---|---|---|
| `ConfigureTargetStatus` | nothing | dead |
| `ConfigureAreaStatus` | nothing; it alone calls `ApplyStatusToNearbyEnemies` (`Base.cs:2157`) | dead |
| `ApplyStatusToNearbyEnemies` | `ConfigureAreaStatus` only; it alone calls `GetAreaStatusLocation` (2235) | dead with `ConfigureAreaStatus` |
| `GetAreaStatusLocation` | `ApplyStatusToNearbyEnemies` only | dead with it |
| `ConfigureCastedTarget`, `ConfigureMultiHit`, `ConfigureInterrupt` | nothing in `Feature/`; two tests read their source text (`AbilityDamageQueueTests.cs:220-223`, `CombatDamageTests.cs:969-971`) | dead (those tests need rewriting if deleted) |
| `PurifyAndMirror` | nothing; it alone calls `GetNearestHostile` (2298) and `GetRemainingDurationSeconds` | dead |
| `GetNearestHostile` | `PurifyAndMirror` only | dead with it |
| `FoggyMind` | nothing (the engine test at `MimicryTechniqueAbilityBehaviors.cs:686-691` names the status type, not this helper) | dead |
| `Additional` | `ConfigureWeapon` (1915) and `ConfigureMultiHit` (2009); `ConfigureWeapon` has an outside caller | live through `ConfigureWeapon` |

So ten helpers are dead (about 330 lines with `GetRemainingDurationSeconds`). `StatusAbilityEffects` needs 5 services without them (`Ability`, `CombatRecorder`, `Party`, `S`, `A`) and 7 with them (add `AbilityHostileImpact` and `Stat`, and the static `ObjectSnapshot`;
about 280 lines instead of 170). The base is about 450 lines without them and about 600 with them. The map builds either way. **Owner question 1.**

## 7. State, handlers and hot reload

- No handlers: `ScriptHandlerOrder.txt`, `BootHandlerClassification.txt` and `HandlerRenames.txt` do not change.
- No carried state: no `[ReloadCarried]` field exists or is added, so `ReloadCarry` is not involved. The parts are stateless singletons a reload rebuilds (`HotReload.md`). The ability cache reruns on reload and builds new `AbilityDetail` closures over new profiles, as today.
  A modifier an old profile's `ModifierSource` wrote expires on its own timer, as today.
- `WeaponAbilityProfile.Empty` stays a shared instance; its `StaticState.txt` line is re-keyed from `WeaponActiveAbilityDefinitionBase` to `WeaponAbilityProfile` when the profile becomes top level.
- `PlayerMessageAudit.json` has 11 rows keyed by `File`, `Line` and `Member` to this file. They re-key to the new files and members, and the `Call` text changes where `profile.` is added or removed. The audit test checks them.

## 8. Proof that behavior is unchanged

1. **Settings snapshot, from the base commit before any move.** A throwaway generator builds every ability definition (the 147 types under the base) through `BuildAbilities()` and writes `WeaponAbilitySettings.txt`: per feat, every `AbilityDetail` property and every `WeaponAbilityProfile` property that
   differs from the default, read by reflection as CombatSim does. The test compares token by token (not a raw text diff).
2. **Call-trace snapshot with a coverage proof.** The fixture drives each ability's activation, impact and validation closures against recording doubles of the 21 services and records the ordered log (service, member, arguments). At the base commit, coverage must show every line of `Base.cs:284-2331`
   reached and both sides of each branch; the report is attached to PR 1. The log also records:
   - NWScript calls: `FloatingTextStringOnCreature`, `SendMessageToPC`, `EffectDamage`, `EffectHeal`, `EffectInvisibility`, `GetNearestHostile`, `ObjectSnapshot.InShape`, `AbilityTargeting.GetHostileTargetsNearLocation`;
   - static content reads: `WardBondStatusEffect.HasRecentWardHit`, the `GuardedStatusEffect` calls, and `UsePerkFeat.InterruptAbilityActivationFromHit`;
   - a stable per-profile token in place of the guid argument (`Base.cs:934`), so logs compare.
   The `IAbilityService` double invokes every callback passed to `ApplyCombatImpact` and `ApplyTelegraphedCombatImpact` for a given target list and summary, and the fixture drives the `ResolvesImpactStatusCategories` delegate.
3. **States covered.** Target: recent or not recent, a recent guarded hit, a ward hit, using an ability, PC versus NPC, behind or beside, bleeding, controlled, low HP. Resources: each FP and stamina threshold. Hits: deflection, suppression stacks, critical count, all hits landed,
   zero damage with targets hit, area path versus direct path, one hit versus several.
4. **CombatSim readings byte-identical** before and after (the P5-3 procedure). With D1 the reader is untouched.
5. **Moved bodies verbatim.** A script compares the normalized text of each moved method with its original (token-aware). The diff of `Module/`, `SWLOR_Haks/` and the derived definitions is empty.
6. **Existing tests.**
   - Nine unit test files read the base source by path (17 sites). Those that assert exact text need rewriting, not re-pointing: `AbilityDamageQueueTests.cs:450-465`, `CombatDamageTests.cs:573`, `GeneratedWeaponPerkBehaviorTests.cs:346, 1851, 1875`. The others are re-pointed to the owning part.
   - Five test types derive from the bases: `StaminaRecoveryEngineTests.cs:19` (calls `profile.AfterImpact` at :166, now `ImpactEffects`), `PerkTrackerRegressionEngineTests.cs:294`, `AIModelTests.cs:280`, `WeaponStatusSpreadTests.cs:85-94` (`snapshot.Capture`, now `StatusRiders`) and `GuardCounterControlTests.cs:82`.
   - Also run `AreaAbilityFragmentationTests`, `CrowdControlBudgetTests`, `PlayerMessageAuditTests`, the P5 baseline guards, the content injection rule tests and the architecture tests.
7. **Engine tests.** `PerkTrackerRegressionEngineTests`, `StaminaRecoveryEngineTests` and the `AbilityBehaviors` files (`Force`, `General`, `MimicryUtility`, `MimicryTechnique`, `TwinBlade`) run before and after with identical pass sets.
8. **New unit tests.** One class per part with fake services (idle windows, recent-target windows, guarded-hit status, spread cap, resource-conditional restore and drain), and the part-service-count guard.
9. **Coverage.** The gate runs with coverage as P5-4 did; no floor is lowered.
10. **Client evidence** (`InEngineVisualVerification.md`): each code PR needs live client captures of one single-target, one telegraphed area, one multi-hit, one friendly stance and one toggle ability on the same dummy before and after, with impact numbers and status results read from the combat log.

## 9. Sequencing, PRs, baselines and conflicts

**Sequencing (D6).** Under Phase 5 order rule 4 (two items never work on the same files), P5-5 (the serial chain, under a master freeze) touches this file in P5-5c, P5-5f and P5-5g. P5-16f's code PRs start after P5-5h merges, and this map is re-measured then.
PR 1 changes no production file and can run earlier. P5-5 deltas the re-measure must pick up: `CreateStatusEffect` (`Base.cs:576`, catalog), `GetStatusEffectName` (2178, catalog), the 22 `ApplyStatusEffect` sites (P5-5f, application),
`ExtendStatusEffectDuration` (589, duration), and the receiver text of every `StatusEffect.X` read that stays. The `S`, `A`, `D`, `C` counts in section 3 are the post-P5-5 shape; if P5-5's final interfaces change, the counts change.

**PR order.** From the refactor branch tip, each gated before the next. Parts cannot call protected statics on the base, so lower layers land first.

| PR | Content | Base after | Baselines regenerated |
|---|---|---|---|
| 1 | Settings and call-trace snapshot fixtures with the coverage proof; source-reading test helper. Tests only. | 2,332 | none |
| 2 | `StatusAbilityEffects` first (the closures of `ConfigureToggle`, `ConfigureSelfStatus`, `ConfigurePartyStatus` call it), then layers 0 and 1 (`TemporaryModifiers`, `StatusRiders`, `DamageModifiers`, `SelfEffects`, `HitEffects`). The profile methods they replace stay as thin delegates to the parts through `GameServices` until PR 3. | ~2,000 | `StaticLookups`, `ServicesPerType`, `DebtReport.md` |
| 3 | Layers 2 and 3 (`HitRiders`, `ImpactEffects`, `FriendlyTargets`, `Activations`, `Impacts`); closures become one-line calls; `WeaponAbilityProfile` top level with the profile behavior and the delegates gone | ~450 to 600 | `LargeTypes`, `LongMethods`, `StaticLookups`, `ServicesPerType`, `StaticState` (re-keyed), `DebtReport.md` |
| 4 | Delete the dead helpers if the owner approves (question 1) and finish the `PlayerMessageAudit.json` re-key | ~450 | `StaticLookups`, `DebtReport.md` |

Every code PR regenerates `LargeTypes`, `StaticLookups`, `ServicesPerType` and `DebtReport.md`; the baselines are exact-match (`Phase5DebtTests.cs:16-17`) and never hand edited. After PR 3: `LargeTypes.txt` -1 (the type leaves the list), `LongMethods.txt` -1 (`ConfigureWeaponAbility`),
`ServicesPerType.txt` 21 -> about 7 (the line leaves when the base is at 10 or fewer), `StaticLookups.txt` 187 -> a few dozen lines (one `GameServices.X` per closure plus the `ConfigureWeapon` family; the build report gives the figure), `StaticState.txt` re-keyed to `WeaponAbilityProfile`.
`CrossDomainEdges`, `UpwardEdges` and `LogicToContent` are unchanged: no `Domain/` file is added and `Feature/AbilityDefinition` is the Abilities domain.

**Conflict with P5-5.** Both edit this file's `StatusEffect.X` calls. P5-16f's code PRs start after P5-5h, so there is no concurrent edit; the PRs start from a tip where the P5-5 tool has already rewritten the receiver text. If a master sync reruns a P5-5 or content tool, it runs over the new files
like any content file; this item does not rely on that.

**Master port.** Master changed this file 4 times in the last month (`67cb382f48`, `09bb6246d5`, `6bd07d6575`, `8051b0e016`). The profile moving to its own file ends automatic merges for the 284-1528 range, and the closures moving ends them for the impact body.
After PR 3, a master edit to those ranges is ported by hand to the owning part. Each PR records the port rule in its description and in the Status file's Port log.

## 10. Owner questions

1. **Delete the ten dead helpers** (section 6) in their own PR (PR 4)? **Recommended: yes.** They have no caller in `Feature/`, cost about 330 lines and 2 services, and two tests only read their source text. If not, they stay on the base unmoved (base about 600 lines, `StatusAbilityEffects` 7 services).
2. **First registration of the parts** (section 5). Seed `AddSingleton<T>()` lines once, or extend the analyzer so a class with instance methods under a `converted` folder is a singleton? **Recommended: seed once**, since the analyzer's shape rule is meant for classes that are only constructors plus injected properties.
3. **Who takes `AbilityTargeting`, `TemporaryHitPointEffects`, `ActionInterruption` and `UsePerkFeat` after P5-12 and C8?** **Recommended:** `AbilityTargeting` to `StatusRiders` (spread) and `FriendlyTargets` (validation); `TemporaryHitPointEffects` to `FriendlyTargets`; `ActionInterruption` and `UsePerkFeat` to `HitRiders`. All parts stay at 9 or fewer.

## 11. Not resolved

- The first registration mechanism (question 2), until `-Check` runs on real code.
- Exact service counts: they come from a syntax scan of the original ranges, and `Stat` and `CombatQueries` appear in several ranges, so the build report decides.
- P5-5's final interfaces may change before P5-5h; the map is re-measured then (section 9).
- The estimate of `ConfigureWeaponAbility` (120 to 140 lines) is not measured; section 4 gives the only permitted fallback.
