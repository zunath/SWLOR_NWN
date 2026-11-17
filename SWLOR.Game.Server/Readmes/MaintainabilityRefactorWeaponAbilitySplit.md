# Maintainability Refactor: WeaponActiveAbilityDefinitionBase Split (P5-16f)

Part map, not yet approved. No code changes in this commit. Base: `feature/maintainability-refactor` at `d43e40fa8`. Limits come from
[MaintainabilityRefactorPhase5.md](MaintainabilityRefactorPhase5.md): 1,000 lines and 10 services per type, 150 lines per method, no new
static service lookups, no new upward domain references, no `Part1`/`Part2` chunks. Line counts below are measured from the source
(`grep` and `sed` ranges on `Feature/AbilityDefinition/WeaponActiveAbilityDefinitionBase.cs`); estimates are rounded to about 10 and the build
report gives exact numbers.

## 1. What the type is

`Feature/AbilityDefinition/WeaponActiveAbilityDefinitionBase.cs`: one abstract class, 2,332 lines, no constructor, no handlers, no carried state.
It is the shared base of ability definitions (`IAbilityListDefinition`) and it reaches every service through `using static GameServices`.

| Fact | Number |
|---|---|
| Direct subclasses (grep `: WeaponActiveAbilityDefinitionBase`) | 120: 116 definitions and 4 intermediate bases (`HeavyVibrobladeActiveAbilityDefinitionBase`, `TwinPistolAbilityDefinitionBase`, `UnarmedAbilityDefinitionBase`, `SpearActiveAbilityDefinitionBase`) |
| Definitions below the intermediate bases | 31 (11 Heavy Vibroblade, 10 Twin Pistol, 10 Unarmed; the Spear base has none by the same grep) |
| Call sites of `ConfigureWeaponAbility` | 213 in 104 files |
| Files with `new WeaponAbilityProfile { ... }` | 69 files, 149 sites |
| Other protected statics the definitions call | `ConfigureToggle` 14, `ConfigureSelfStatus` 6, `ConfigurePartyStatus` 4, `ToggleSelfStatus` 7, `ApplyStatusToNearbyParty` 8, `ConfigureWeapon` 2, `Additional` 3, `ApplyStatusToNearbyEnemies` 2, `GetAreaStatusLocation` 2, `GetNearestHostile` 2, and one each of `ConfigureTargetStatus`, `ConfigureAreaStatus`, `ConfigureCastedTarget`, `ConfigureMultiHit`, `ConfigureInterrupt`, `PurifyAndMirror`, `FoggyMind` |
| How it is built | `AbilityCatalogService` finds every `IAbilityListDefinition` and calls `Activator.CreateInstance(type)` at `mod_cache_bef`; `BuildAbilities()` runs the subclass's static helper methods, which call the protected statics. The built `AbilityDetail` closures keep the profile. |
| Handlers | none (`ScriptHandlerOrder.txt` has no line for it) |
| Carried or runtime state | none. `StaticState.txt` counts 1: the `WeaponAbilityProfile.Empty` shared instance. Each profile holds a per-instance `ability:<guid>` modifier source. |
| Services per type (`ServicesPerType.txt`) | 21, all by lookup, none by constructor |
| Static lookup lines (`StaticLookups.txt`) | 187 |
| Over the limits | the type (2,332 lines); `ConfigureWeaponAbility` (291 lines) is its only long method |

Anatomy (line ranges in the file):

| Lines | What | Size |
|---|---|---|
| 22-283 | `WeaponAbilityProfile` data: about 185 `init` properties, two nested snapshot types | 260 |
| 284-1528 | `WeaponAbilityProfile` behavior: 41 methods that read the profile and call 20 services | 1,245 |
| 1530-1886 | `ConfigureWeaponAbility` (291 lines) and its targeting helpers | 357 |
| 1887-2060 | `ConfigureWeapon`, `ConfigureCastedTarget`, `ConfigureMultiHit`, `ConfigureInterrupt` | 175 |
| 2067-2331 | status helpers: toggle, self, target, party and area status, nearby party and enemy status, `PurifyAndMirror`, `GetNearestHostile` | 265 |

The 21 services: `Ability`, `AbilityDamageInput`, `AbilityHostileImpact`, `AbilityImmunity`, `AbilityImpactState`, `AbilityStatusApplication`, `CombatAbilityActivation`,
`CombatAbilityBonus`, `CombatAbilityHit`, `CombatActivity`, `CombatDamageTaken`, `CombatQueries`, `CombatRecorder`, `CombatResources`, `CombatSuppression`,
`Enmity`, `Party`, `PlayerFeedback`, `Stat`, `StatusEffect`, `TemporaryStatModifier`.

Three facts shape the design:

1. **The profile is data plus behavior.** Phase 5 says a data object takes no service and its behavior moves to the service that owns the data
   (`MaintainabilityRefactorContentInjection.md`, decision 1). The 185 properties stay as a plain class; the 41 methods move.
2. **The behavior names content.** It names seven `Feature/StatusEffectDefinition` types: `BleedStatusEffect`, `HemorrhageStatusEffect`, `SunderStatusEffect`,
   `InfectionStatusEffect`, `WardBondStatusEffect` (by type), and `ChallengeEnmityStatusEffect` and `FoggyMindStatusEffect` (by `new`). A `Domain/` service that named
   them would add `LogicToContent.txt` lines. So the new types live in `Feature/AbilityDefinition`, which `DomainMap.json` counts as the Abilities domain (content band),
   and the edges stay the ones this file has today.
3. **CombatSim reads the closure.** `SWLOR.CombatSim/Data/WeaponImpactReader.cs` finds the impact action's closure by method name (`<ConfigureWeaponAbility>`,
   `<ConfigureWeapon>`) and reads captured fields `baseDamage`, `duration`, `statusEffect`, `additionalStatusEffects`, `profile`, `skill`,
   `restoresSecondaryTargetStamina` by reflection, and it reads profile property names and a default-constructed profile (`Activator.CreateInstance(profileType, true)`).
   Moving the lambdas renames the closures, so the reader changes in the same PR (section 4).

## 2. The parts

All new types are content singletons in a new folder `Feature/AbilityDefinition/WeaponAbility/`, namespace `SWLOR.Game.Server.Feature.AbilityDefinition.WeaponAbility`.
Rules: constructor injection only, no `GameServices`, no `Func<>` deferred resolution, no interface unless a test needs a fake of the type (the services they call already have
interfaces, so unit tests build them with fakes). Method bodies move verbatim except where section 3 cuts them. Service counts include the new parts a type injects.

| Type | Responsibility | Members that move (from) | Lines | Injected |
|---|---|---|---|---|
| `WeaponActiveAbilityDefinitionBase` (stays) | Entry points for derived definitions | Interim (section 4): the protected static methods as one-line forwarders. Final (P5-14): a constructor taking the two configurators as protected properties | interim ~200, final ~40 | interim 0 (2 lookups), final 2 |
| `WeaponAbilityProfile` (top level, namespace `Feature.AbilityDefinition`) | Data: what an ability adds on top of the base impact | all properties; `Empty`; `ModifierSource` (was the private guid field, now a public get-only property); the two snapshot types as plain data (`StatusSpreadSnapshot` keeps the counter and the captured dictionary; its `Capture` lookup moves to `WeaponAbilityStatusRiders`) | ~300 | 0 |
| `WeaponAbilityDamageModifiers` | Extra damage, percent damage, critical rate, defense ignore, idle snapshots | `GetBaseDamageAdjustment`, `GetDamagePercentAdjustment`, `GetCriticalRateAdjustment`, `GetDefenseIgnorePercent`, `ResolveImpactStatus`, `CaptureActivationIdleBonusSnapshot`, `ClearActivationIdleBonusSnapshots`, `IsIdle`, `IsTargetBelowThreshold`, `IsBleeding` | ~300 | 8: `CombatAbilityBonus`, `CombatActivity`, `CombatDamageTaken`, `CombatQueries`, `CombatResources`, `CombatAbilityActivation`, `PlayerFeedback`, `StatusEffect` (`Stat` if the build shows it) |
| `WeaponAbilityStatusRiders` | Conditional statuses, status spread, suppression | `ApplyConditionalStatus`, `ApplyTargetConditionalStatus`, `ApplyStatusSpread`, `ApplySuppressionEffects`, `SpreadStatusToNearbyHostile`, snapshot `Capture` | ~240 | 6: `CombatActivity`, `CombatDamageTaken`, `CombatQueries`, `CombatSuppression`, `Stat`, `StatusEffect` |
| `WeaponAbilityHitRiders` | What each landed hit does | `AfterSuccessfulHit`, `BeforeSuccessfulImpactRiders`, `GetAppliedCategories`, `ExtendSourceStatusEffects`, `ConsumeSourceStatusEffects`, `ApplyDefenseIgnoreHitEffects`, and the `ApplyRangedAbilityTargetDefenseReduction` call that follows `AfterSuccessfulHit` in both impact paths | ~180 | 9: `AbilityImpactState`, `CombatAbilityHit`, `CombatResources`, `Stat`, `StatusEffect`, `TemporaryStatModifier`, `CombatAbilityBonus`, `CombatActivity`, `WeaponAbilityStatusRiders` |
| `WeaponAbilityImpactEffects` | What the impact does to caster and target after the last hit | `SpendHitPoints`, `RestoreResourcesForTargetCount`, `AfterImpact`, `ApplyLandedImpactModifiers`, `ApplyResourceConditionalRestore`, `ApplyResourceConditionalDrain`, `ApplyTargetTemporaryModifiers`, `ApplyProtectedTargetHitSelfModifiers`, `ApplyTargetUsingAbilityEffects`, `IsFPAtOrAbove`, `IsStaminaAtOrAbove` | ~330 | 9: `CombatDamageTaken`, `CombatResources`, `CombatActivity`, `CombatQueries`, `CombatRecorder`, `PlayerFeedback`, `Stat`, `StatusEffect`, `TemporaryStatModifier` |
| `WeaponAbilitySelfEffects` | Activation steps and self buffs | `PrepareQueuedActivation`, `PrepareActivationIdleBonuses`, `AfterActivation`, `AfterHostileActivation`, `ApplySelfInvisibility`, `ApplyNearbyPartyStatus`, `ApplySelfImmunity`, `ApplySelfModifiers`, `ReplaceTemporary*`, `ApplyTemporaryDefeatedEnemyModifiers` | ~330 | 8: `CombatAbilityBonus`, `PlayerFeedback`, `AbilityImmunity`, `CombatDamageTaken`, `CombatResources`, `Enmity`, `StatusEffect`, `TemporaryStatModifier` |
| `WeaponAbilityFriendlyTargets` | Friendly and self-status abilities, hostile target validation | `HasFriendlyTargetStatus`, `HasFriendlyTargetEffects`, `ValidateHostileTarget`, `ValidateFriendlyTargetStatus`, `ResolveFriendlyTarget`, `ApplyFriendlyTargetStatus`, `ApplyFriendlyTargetEffects`, `HasImmediateSelfStatusEffect`, `ApplySelfStatusEffect`, `ApplySelfStatusToGuardedTarget` | ~190 | 3: `Ability`, `StatusEffect`, `TemporaryStatModifier` |
| `WeaponAbilityImpacts` | The impact action: hostile path (telegraphed area and direct multi-hit) and friendly path | the impact lambda body of `ConfigureWeaponAbility` as `ApplyHostileImpact`, `ApplyFriendlyImpact` and their steps (section 3) | ~300 | 9: `Ability`, `AbilityDamageInput`, `AbilityImpactState`, `StatusEffect`, `WeaponAbilityDamageModifiers`, `WeaponAbilityHitRiders`, `WeaponAbilityImpactEffects`, `WeaponAbilitySelfEffects`, `WeaponAbilityFriendlyTargets` |
| `WeaponAbilityConfigurator` | Builds the `AbilityBuilder` for weapon abilities | `ConfigureWeaponAbility` (as named steps), `ShouldUseTelegraphedCombatImpact`, `ToCombatImpactAreaShape`, `CenterAreaOnActivator`, `ApplyTargetingMetadata`, `ApplyClientTargeting`, `ConfigureWeapon`, `ConfigureCastedTarget`, `ConfigureMultiHit`, `ConfigureInterrupt`, `ApplyCombatImpactDamageAbility`, `Additional` | ~480 | 8: `Ability`, `AbilityStatusApplication`, `CombatAbilityBonus`, `CombatActivity`, `WeaponAbilityDamageModifiers`, `WeaponAbilitySelfEffects`, `WeaponAbilityFriendlyTargets`, `WeaponAbilityImpacts` |
| `StatusAbilityConfigurator` | Toggle and status abilities | `ConfigureToggle` x2, `ConfigureSelfStatus` x2, `ConfigureTargetStatus`, `ConfigurePartyStatus` x2, `ConfigureAreaStatus`, `ToggleSelfStatus`, `FoggyMind`, `ApplyStatusToNearbyParty` x2, `ApplyStatusToNearbyEnemies`, `GetAreaStatusLocation`, `PurifyAndMirror`, `GetRemainingDurationSeconds`, `GetNearestHostile` | ~290 | 6: `Ability`, `AbilityHostileImpact`, `CombatRecorder`, `Party`, `Stat`, `StatusEffect` (plus `IContentFactory` for `FoggyMind`, so 7) |

Totals: 2,332 lines become about 2,700 across eleven types (the growth is constructors, fields and `using` lines, about 30 lines per new type). No type is over 480
lines and none is over 9 services. If the build shows a type over 10, move a member group to the neighbor that already calls it, never a new numbered type. The `Stat` and
`CombatQueries` counts are the ones to check first, because the scan found them in several ranges.

Content status types stay named in Feature code: `WeaponAbilityStatusRiders` names `BleedStatusEffect`, `HemorrhageStatusEffect`, `SunderStatusEffect`;
`WeaponAbilityDamageModifiers` names `InfectionStatusEffect` and `SunderStatusEffect` (the line 53 `HasStatusEffect<SunderStatusEffect>` and the line 379 use);
`WeaponAbilityImpactEffects` and `WeaponAbilityFriendlyTargets` name `WardBondStatusEffect`. `ChallengeEnmityStatusEffect` (two `new` sites with a value, in
`WeaponAbilityImpactEffects`) and `FoggyMindStatusEffect` are built with `IContentFactory` (`Content.Create<ChallengeEnmityStatusEffect, int>(percent)`), so those two types take `IContentFactory`
and count it (`WeaponAbilityImpactEffects` 9 -> 10, the limit; if the build adds one more, `ApplyProtectedTargetHitSelfModifiers` moves to `WeaponAbilitySelfEffects`, which has room).
The four `Activator.CreateInstance(type)` status factories in the status helpers become `StatusEffect.CreateStatusEffect(type)` (the status effect catalog after P5-5c), as the content injection guide lists.
No parameter list on the 213 call sites changes.

## 3. Getting `ConfigureWeaponAbility` under 150 lines

The signature (22 parameters) stays so the 213 call sites change only their receiver. Inside, the arguments are packed once into a private `WeaponAbilitySpec` record
and every step takes the spec and the builder:

| Step | What it does | Lines |
|---|---|---|
| `RequireValidProfile` | the guarded-hit status check that throws `InvalidOperationException` | ~10 |
| `ApplyBaseSettings` | damage ability, activation delay, skill, impact animation, `BreaksStealth` | ~15 |
| `ResolveStatusCategories` | the `ResolvesImpactStatusCategories` lambda | ~15 |
| `BuildActivationAction` | the `HasActivationAction` lambda (idle bonuses, queued activation, toggle) | ~25 |
| `BuildImpactAction` | `HasImpactAction`: calls `WeaponAbilityImpacts.ApplyFriendlyImpact` or `ApplyHostileImpact` | ~15 |
| `ApplyAbilityKind` | `IsWeaponAbility` or `IsCastedAbility` | ~6 |
| `ApplyHostileSettings`, `ApplyFriendlySettings`, `ApplySelfStatusSettings` | targeting, range, custom validation, perk refund removal | ~20 each |
| `ApplyRequirements` | stamina and FP | ~6 |

The hostile impact (about 170 lines of the 291) is cut in `WeaponAbilityImpacts` into `BeginHostileImpact` (spend hit points, resolve status, create the spread snapshot, capture
the idle snapshot, add the defense ignore), `RunTelegraphedAreaImpact`, `RunDirectImpact` (the hit loop) and `FinishImpact`. The two impact calls repeat the same six callbacks
(base damage, percent damage, after hit, before riders, hit chance, critical rate); they are built once as a private `WeaponImpactCallbacks` record. Evaluation order and
captured arguments stay the same. The longest method after the cut is `ApplyTemporaryDefeatedEnemyModifiers` (129 lines, moved whole).

## 4. How the derived definitions reach the parts

**Today** every call is a protected static: `ConfigureWeaponAbility(builder, ...)`, called from private static helpers of the subclass. P5-14 converts the definitions to
made content (instances with constructors) and its tool rewrites static helpers to instance methods. P5-16 must land first (Phase 5 rule 4), so the order is:

- **P5-16f (this item): interim bridge.** The base keeps its protected static methods, now one-line forwarders (about 24, with overloads) to the two entry types, `WeaponAbilityConfigurator`
  and `StatusAbilityConfigurator`, through one private static property each that reads a temporary `GameServices` accessor (`GameServices.WeaponAbilityConfigurator`,
  `GameServices.StatusAbilityConfigurator`, in `Composition`, removed at C11 with the rest). The base's lookups go from 187 lines and 21 services to 2 and 2. The 150 other
  definition types and the generator `tools/GenerateWeaponArchetypeImplementation.py` are untouched. New code has no lookup; the two accessors are the only bridge.
- **P5-14 (content wave for abilities).** The tool turns the forwarders into constructor-injected protected properties named `WeaponAbilities` and `StatusAbilities` and rewrites the call
  text (`ConfigureWeaponAbility(` becomes `WeaponAbilities.ConfigureWeaponAbility(`), forwards them through the four intermediate bases, and the base ends at about 40 lines and 2 services.

Registration: the new singletons are written to `Composition/ContentRegistrations.cs` by `tools/Refactor/Apply-ContentInjection.ps1` (the file says do not edit by hand); the build
confirms every new type is registered and builds at `BuildServiceContainer` (constructors only store dependencies).

**CombatSim.** The reader keeps its method-name match for the shared helpers; the impact action becomes a method on a small `WeaponAbilityImpactAction` object (public properties
`BaseDamage`, `Duration`, `StatusEffect`, `AdditionalStatusEffects`, `Profile`, `Skill`, `RestoresSecondaryTargetStamina`) built in `BuildImpactAction`, and `WeaponImpactReader` reads them
as typed properties from `detail.ImpactAction.Target` instead of by closure field name. The profile reader keeps reading property names and the default profile, so `WeaponAbilityProfile`
keeps its property names and a parameterless constructor. `ConfigureWeapon` gets the same object. Both changes are in the PR that moves the lambdas.

## 5. State, handlers and hot reload

- No handlers: `ScriptHandlerOrder.txt`, `BootHandlerClassification.txt` and `HandlerRenames.txt` do not change.
- No carried state: no `[ReloadCarried]` field exists on the type and none is added. The new types are stateless singletons that a reload rebuilds (`HotReload.md`). The ability cache reruns
  on reload and builds new `AbilityDetail` closures over new profiles, as today. A temporary stat modifier an old profile's `ModifierSource` wrote expires on its own timer, as today.
  `ReloadCarryHistory` is unaffected because no carried field moves.
- `WeaponAbilityProfile.Empty` stays a shared instance. It is the one `StaticState.txt` line today and stays unless the build shows the guard counts the top-level class differently.
  A profile-less ability never reaches a rider method that writes a modifier (every rider is gated on a non-default property); a test pins that.
- `PlayerMessageAudit.json` has 11 rows keyed by `File`, `Line` and `Member` to this file. They are re-keyed to the new files and members in the same PR; the audit test checks them.

## 6. Proof that behavior is unchanged

1. **Settings snapshot, taken before any move.** A throwaway generator on the base commit builds every ability definition (the 147 types that derive from the base, directly or
   through the four intermediate bases) through `BuildAbilities()` and writes `WeaponAbilitySettings.txt`: per feat, every `AbilityDetail` property (name, level, skill, flags, delays, range,
   requirements, targeting shape and flags, status removals) and every `WeaponAbilityProfile` property that differs from the default, read by reflection as CombatSim does. It is committed as a fixture. After
   each PR a test rebuilds the text and compares it byte for byte.
2. **Call-trace snapshot.** A second fixture drives each ability's activation action, impact action and validation against recording doubles for the 21 services (fixed return values, a
   fixed set of target states: idle, bleeding, controlled, behind, low HP, guarded, in party) and writes the ordered call log: service, member, arguments. It is generated from the base commit before
   any move and compared after each PR. It proves the moved bodies and the cut steps call the same things in the same order.
3. **CombatSim readings byte-identical.** Run the weapon simulation before and after (the P5-3 procedure) and compare the output files.
4. **Moved bodies verbatim.** A script compares the normalized text of each moved method against its original. The diff of `Module/`, `SWLOR_Haks/` and the derived definitions is empty (interim bridge).
5. **Existing tests.** Nine unit test files read this file by path (17 sites), so each PR re-points them to the owning new file: `AIModelTests`, `AbilityDamageQueueTests`, `ThrowingTossAreaImpactVisualTests`,
   `FirstAidCombatUpgradeTests`, `GeneratedWeaponPerkBehaviorTests`, `PerCastResourceCapTests`, `CombatDamageTests`, `PerkRefundStatusEffectCleanupTests`, `WeaponDeflectionTests`. Two derive from the base:
   `WeaponStatusSpreadTests` (uses the nested snapshot type, which gets a `using` and its new name) and `GuardCounterControlTests.ConfigureProbe`. Also run `AreaAbilityFragmentationTests`,
   `CrowdControlBudgetTests`, `PlayerMessageAuditTests`, the P5 baseline guards and the architecture tests.
6. **Engine tests.** `PerkTrackerRegressionEngineTests`, `StaminaRecoveryEngineTests` and the `AbilityBehaviors` files (`Force`, `General`, `MimicryUtility`, `TwinBlade`) run before and
   after with identical pass sets.
7. **New unit tests.** One class per new type with fake services for its rules (idle bonus windows, recent-target windows, guarded-hit status, spread cap, resource-conditional restore); a guard that fails the
   base over 1,000 lines or 10 services and any new type over 10 services.
8. **Coverage.** The gate runs with coverage as P5-4 did. Moved lines keep their area floors because the call-trace fixture executes every moved method. No floor is lowered.
9. **Client evidence** (`InEngineVisualVerification.md`): the change has an in-game surface (ability behavior), so each PR needs live client captures: one single-target, one telegraphed area, one multi-hit, one friendly
   stance and one toggle ability used on the same dummy before and after, with the impact numbers and status result read from the combat log and compared.

## 7. PR order, baselines and conflicts

Four PRs, each from the refactor branch tip, each gated before the next:

| PR | Content | Base type after | Baselines |
|---|---|---|---|
| 1 | The two snapshot fixtures and the source-reading test helper (tests only, from the base commit) | 2,332 | none |
| 2 | `WeaponAbilityProfile` top level and its behavior moved out: `WeaponAbilityDamageModifiers`, `WeaponAbilityStatusRiders`, `WeaponAbilityHitRiders`, `WeaponAbilityImpactEffects`, `WeaponAbilitySelfEffects`, `WeaponAbilityFriendlyTargets`; the profile methods are replaced by calls through temporary private statics on the base | ~1,000 | none yet (the base still names services through lookups) |
| 3 | `WeaponAbilityImpacts`, `WeaponAbilityConfigurator`, the `WeaponImpactReader` change, the `ConfigureWeaponAbility` cut | ~330 | `LongMethods` -1 |
| 4 | `StatusAbilityConfigurator`; base reduced to forwarders and the two accessors; `PlayerMessageAudit.json` re-keyed; baselines regenerated; `DebtReport.md` | ~200 | below |

After PR 4: `LargeTypes.txt` -1 line (`WeaponActiveAbilityDefinitionBase`, 2,332); `LongMethods.txt` -1 (`ConfigureWeaponAbility`); `ServicesPerType.txt` the type 21 -> 2 (the line leaves in P5-14);
`StaticLookups.txt` the file's line 187 -> about 4; `StaticState.txt` unchanged, or -1 if the guard no longer counts `Empty`;
`CrossDomainEdges` and `UpwardEdges` unchanged (Feature/AbilityDefinition is the Abilities domain, so the edges are the old ones, and no new `Domain/` file is added); `LogicToContent.txt` unchanged (no `Domain/` file
names content). The build report is the authority; the PR regenerates baselines, never hand edits them.

**Handlers and reload:** none to change (section 5).

**Conflict with P5-5 (StatusEffectService split).** Both touch this file's `StatusEffect.X(...)` calls: `ApplyStatusEffect` 22 sites (P5-5f moves it to `IStatusEffectApplicationService`, about 460 sites by the tool),
`ExtendStatusEffectDuration` and `CreateStatusEffect`, `GetStatusEffectName` (P5-5c catalog, P5-5g duration). The reads (`HasStatusEffect`, `GetStatusEffect`, `RemoveStatusEffect`, `HasCleanseType`,
`GetCreatureStatusEffects`) stay on `IStatusEffectService`. The new types take `IStatusEffectService`; the P5-5 tool rewrites receiver text in every file, new files included, so whichever
lands second reruns the tool and nobody hand-merges. Two cautions. (a) After P5-5f the types that apply statuses (`WeaponAbilityStatusRiders`, `WeaponAbilityHitRiders`, `WeaponAbilityImpactEffects`, `WeaponAbilitySelfEffects`,
`WeaponAbilityFriendlyTargets`, `StatusAbilityConfigurator`) each gain `IStatusEffectApplicationService`, and `WeaponAbilityHitRiders` also the duration part, which takes it to 10 and `WeaponAbilityImpactEffects` to 11. If that
happens, move `ApplyProtectedTargetHitSelfModifiers` to `WeaponAbilitySelfEffects` and the target modifier methods to `WeaponAbilityStatusRiders`; the PR that causes it records the move. (b) PR 2 of this item and P5-5f must not
both be open against the same base: P5-5f edits `WeaponActiveAbilityDefinitionBase.cs` (22 lines) while PR 2 empties it. Recommended order: P5-16f PR 2 and PR 3 first (they empty the file), then P5-5f runs its tool over the new files.
The two items share no other file.

## 8. Open questions

1. **Interim bridge or wait for P5-14.** May the base keep static forwarders that read two temporary `GameServices` accessors until the ability wave of P5-14 converts the 147 definitions? **Recommended: yes.** The accessors live in `Composition`
   and go at C11. The alternative is converting the 147 definitions and four bases inside P5-16f, which is P5-14's tool run and would make this item touch about 150 files.
2. **New types in `Feature/` as content singletons, not `Domain/`.** **Recommended: yes.** A `Domain/Abilities` home would turn seven status types into a seam interface or new `LogicToContent.txt` lines. If the owner prefers `Domain/`,
   the same parts move there with a status-type interface supplied by content (supply rule 2); the part map does not change, only the folder and the registration line.
3. **Receiver rewrite versus forwarders after P5-14.** Derived definitions call `WeaponAbilities.ConfigureWeaponAbility(...)` (the tool rewrites about 290 sites) or keep the bare protected method names on the base as instance forwarders.
   **Recommended: receiver rewrite.** Forwarders would keep the base at about 200 lines for no benefit and are the forwarding layer the Ability split avoided.
4. **`WeaponAbilityProfile` visibility.** It is `protected sealed` nested today; the top-level type must be `public` (tests, CombatSim and the new types read it). **Recommended: public, with the same property names**, because CombatSim reads names by reflection.
5. **`Empty` shared instance.** **Recommended: keep it.** A new profile per ability would change the shared modifier-source guid, which no rider reads for a default profile, but gains nothing.

## 9. Blockers

None for the map. The split cannot start before the snapshot fixtures of PR 1 exist, and PR 2 needs the P5-5 order in section 7 settled with the coordinator so that no two PRs are open on this file.
