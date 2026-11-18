# Maintainability Refactor: Remaining View Models and EquipmentStats Split (P5-16h)

Part map for owner review; no code is changed by this document. Base: `origin/feature/maintainability-refactor` at `343f6b31b`.
Limits come from [MaintainabilityRefactorPhase5.md](MaintainabilityRefactorPhase5.md): 1,000 lines and 10 services per type, 150
lines per method, no partial-class or numbered-chunk tricks. It follows the model of
[MaintainabilityRefactorAppearanceEditorSplit.md](MaintainabilityRefactorAppearanceEditorSplit.md) (P5-16e). Line counts are
measured from the code with a brace outline of each file; service counts are the distinct `GameServices` members a line range
reaches, so each is re-measured at build and may move by one or two.

## 1. Scope

| Type | Lines | Services today | Handlers and state |
|---|---:|---|---|
| `PerksViewModel` | 1,544 | 8 (lookups) | 11 click handlers, 3 refresh events |
| `CharacterSheetViewModel` | 1,516 | 21 (14 lookups + 7 `GameServices.X`), over the limit | 22 handlers, 13 refresh events, 6 static members |
| `PlayerGuideViewModel` | 1,367 | 0 | 4 handlers, static `Topics` |
| `IncubatorViewModel` | 1,190 | 8 | 13 handlers, 2 refresh events |
| `DisguiseViewModel` | 1,029 | 3 | 15 handlers, 2 refresh events |
| `DroidAssemblyViewModel` | 1,023 | 3 | 9 handlers |
| `Feature/EquipmentStats` (static) | 1,131 | 8 (120 lookups) | 5 event handlers, 1 boot handler |

Out of scope, as named: `ShipManagementViewModel` and `ShipManagementDefinition` (the space revamp rewrites them).

**What the list misses.** No other view model is over 1,000 lines or over 10 services in the Phase 5 baselines.
`AppearanceEditorViewModel` is P5-16e. Every long method in a view model (`PerksViewModel.OnClickBuyUpgrade` 162,
`CharacterFullRebuildViewModel.OnClickResetEverything` 158, `LightsaberWorkbenchViewModel.ConstructSaber` 151,
`StablesDefinition.BuildWindow` 741) is in the P5-16g branch, which also exempts `CombatStyleGuideTopics.Create` (224) as a data
catalog member; this map assumes P5-16g merges first. Two view models are near the line limit and will cross it with
any growth: `DMToolsViewModel` 924 and `ExtraHotbarViewModel` 910. They are reported, not planned.

## 2. Rules shared by every type

- **The view model keeps** its bindings, `Initialize`, one-line forwarders for the click handlers, the `Refresh` events (they are
  interface members of the view model) and any property whose setter has logic. Everything else moves.
- **Constructor injection** (Phase 5 owner decision 8). Each extracted type takes only the services it uses, through its
  constructor, with no `GameServices` use. The view model's constructor takes its parts and the services it still uses. The
  base class keeps its lookup until C3, as `ConversationViewModel` does (`MaintainabilityRefactorContentInjection.md`, Pilot).
  New parts are listed in `tools/Refactor/ContentInjection.json`, so the content tool registers them.
- **Domain and place.** The parts use Combat, Companions, Items, Economy and Abilities services, which UI may not reference
  (`MaintainabilityRefactorDomainOrder.md`: content is a band above all logic). So every part is content under `Feature/`, in
  its own folder per window: `Feature/GuiDefinition/<Window>Window/` (namespace follows the folder). None goes into `Domain/`.
- **Three shapes, chosen by what the part holds:**
  - *Stateless singleton service* with per-window state in a plain `<Window>Session` class the view model creates in
    `Initialize` (P5-16e's pattern). Used by Incubator, Droid.
  - *Private collaborator* the view model creates in `Initialize` and passes its services (content design, decision 1). Used
    where one small piece owns its own state, Disguise's sound-set picker.
  - *Result records.* A part that fills a table or a block of text returns a record and the view model assigns it, in the old
    order, so the binding order does not change. Used by the character sheet tabs and the perk list.
- **Writing bindings.** Where a part must publish many bindings, it calls `IGuiBindingSink` (`Publish<T>(string name, T value)`,
  `Read<T>(string name)`), which the view model implements with `Set(value, name)` and `Get<T>(name)` (the same code path as a
  declared property, confirmed in P5-16e). **Proposal:** one shared interface in `Domain/UI`, and P5-16e PR 2 uses it instead of
  `IAppearanceBindingSink`. Question 2.
- **A moved method is moved whole.** `Player`, the view model property, becomes a `player` parameter, and nothing else in the body
  changes. Source-reading tests that quote call text with `Player` are rewritten for the parameter name in the same PR.
- **Hot reload** (`HotReload.md`, `GuiServiceReloadTests`): a view model without a payload (Perks, PlayerGuide, Disguise, Droid)
  is reopened after a reload; one with a payload (CharacterSheet, Incubator) is closed. No window state is carried, so sessions
  and collaborators need no carrier. The parts are stateless singletons that a reload rebuilds.
- **No cycles.** Every dependency below goes one way (leaf parts first). If a build shows a part at 11 services, split it by
  the sub-responsibility named in its row, never by line count.

## 3. Types

### 3.1 PlayerGuideViewModel (1,367 lines, data)

**What it is.** About 290 lines of logic (search, topic list, related topics, 4 handlers, `MatchesSearch`) and about 1,030 lines
of topic text: `BuildTopics` (17) over eight section builders of 98 to 137 lines (P5-16c) plus `StatusEffectGuideTopics` and
`CombatStyleGuideTopics`, then the three nested data classes `PlayerGuideTopic`, `ArticleBlock`, `QuestionAnswer` (about 55
lines). It reaches no service. Static state: `Topics` (a `StaticState.txt` entry). Largest methods: the section builders.

**Tests.** `PlayerGuideContentTests` reads `Topics` and `MatchesSearch` by reflection. Three tests read the source text of
`PlayerGuideViewModel.cs`: `PlayerFacingNameBroadcastTests` (line 223), `RoleplayXPOOCPenaltyTests` (456) and
`WeaponDeflectionTests` (202). Their `NotContain` assertions would pass for the wrong reason once the text leaves the file, so
they must read every guide file (the view model, the section types, the two existing topic types), not one.

| Part | Kind, folder | Responsibility | Moves | Lines | Services |
|---|---|---|---|---:|---|
| `PlayerGuideTopicCatalog` | singleton, `Feature/GuiDefinition/PlayerGuideWindow/` | owns the topic list, built once from the sections | `Topics`, `BuildTopics` | ~40 | 0 |
| `PlayerGuideBasicsTopics`, `PlayerGuideProgressionTopics`, `PlayerGuidePerkAndRebuildTopics`, `PlayerGuideCombatTopics`, `PlayerGuideEspionageAndCraftingTopics`, `PlayerGuideGatheringAndCompanionTopics`, `PlayerGuideQuestAndSocialTopics`, `PlayerGuideTravelAndWindowTopics` | static data types, same folder | one guide section each, named for its content (the eight P5-16c methods, one for one) | the eight `Build*Topics` methods | 110 to 150 each | 0 |
| `PlayerGuideTopic`, `ArticleBlock`, `QuestionAnswer` | data, same folder | one type per file | the nested classes | ~20 each | 0 |
| `PlayerGuideViewModel` (stays) | view model | bindings, search, selection, handlers | the rest | ~290 | catalog (1) |

Each section builder stays under 150 lines, so no exemption is needed. `MatchesSearch` stays on the view model (tests reach it).
`Topics` leaves `StaticState.txt` because the catalog is a singleton that holds data.

### 3.2 PerksViewModel (1,544 lines)

**What it is.** 37 bindings (202 lines), 11 click handlers (336), 3 refresh events and about 1,000 lines of methods, using
8 services by lookup (`Perk` 20 calls, `DB` 17, `QuickCast` 10, `AbilityCatalog` 5, `Currency` 4, `Recast` 2, `Gui` 2, `Skill` 1).
No static state, no boot handlers. Carried state: `_pages`, `_selectedPerkIndex`, `_filteredPerks`, `_initialLoadDone`.
Largest methods: `OnClickBuyUpgrade` 162 (P5-16g splits it into three), `LoadPerks` 124, `OnClickRefund` 102, `SelectPerkAt` 74.

**Tests.** `PerksWindowTests` (five source reads and reflection on rank, requirement and detail-text methods),
`ForceLightConsularTests` (invokes the detail text method on `new PerksViewModel()`), `ForceLightGuardianTests` (536),
`PlayerSkillPointAccountingTests` (126), `PerkRefundStatusEffectCleanupTests` (185), `DMChatCommandTests` (136). Also the Bible
tool `tools/UpdateCraftingMinigamesBible.py` reads the refund hours and text from `PerksViewModel.cs` (lines 597 and 598); it is
pointed at the refund part.

| Part | Kind | Responsibility | Moves | Lines | Services |
|---|---|---|---|---:|---|
| `PerkListBuilder` | singleton | which perks show, their rows, order and requirements | `LoadCategories`, `LoadDetails`, `LoadForceAffinityDetails`, `LoadPerks`, `SortPerks`, `GetPerkRowStatus`, rank and sort helpers, `BuildRequirements` | ~380 | `Perk`, `DB`, `Skill`, `Currency` |
| `PerkDetailText` | singleton | the text of the selected perk | `BuildSelectedPerkDetailText`, `BuildAbilityCostText`, `BuildStatusEffectPerkDetailText`, `BuildForceAffinityPerkDetailText`, `BuildRecastGroupText`, `FormatForceAffinity` | ~170 | `AbilityCatalog`, `Perk`, `Recast` |
| `PerkHotBarService` | singleton | feats and mode toggles on the player's hot bar after a purchase or refund | `GrantFeats`, `SyncHotBarActiveAbilityFeats`, the add, remove and is-on-bar helpers | ~210 | `AbilityCatalog`, `Perk`, `QuickCast` (hot bar calls are NWScript) |
| `PerkPurchaseService` | singleton | buying and refunding a perk, with its rules and triggers | the buy and refund bodies (after P5-16g: `ConfirmBuyUpgrade`, `TryGetPerkRank`), `ApplyPurchasePerkTriggers`, `GetCurrentPerkRank` | ~330 | `DB`, `Perk`, `Currency`, `QuickCast`, `Gui`, `PerkHotBarService` |
| `PerksViewModel` (stays) | view model | bindings, page and filter setters, `SelectPerkAt`, handler forwarders, refresh events | the rest | ~620 | 4 parts + `DB`, `Perk`, `Currency` = 7 |

Dependencies: purchase uses the hot bar service and the list builder is used by the view model only. The view model keeps
`SelectPerkAt` (it sets many bindings from `Perk`, `Currency`, `DB`); the three setters with logic (`SelectedPage`,
`SearchText`, the combo setters) stay as they are.

### 3.3 CharacterSheetViewModel (1,516 lines, 21 services)

**What it is.** 59 bindings (315 lines) over four tabs (attributes, stats, resistances, crafting) and a header. 22 handlers
(15 open another window, 6 upgrade an attribute), 13 refresh events, a live hit-point loop on `Scheduler`, three nested row
classes (41 lines), and 6 static members (`Tabs`, `TopToggles`, `BottomToggles`, `StatsTable`, `ResistancesTable`,
`CraftingTable`; `StaticState.txt` 6). Services by range: weapon summary `BeastMastery`, `CombatDamageModifiers`,
`CombatQueries`, `CombatRating`, `DB`, `Item`, `PairedRangedTraits`, `Skill`, `Stat` (9); attack delay `CombatAttackTiming`,
`CombatQueries`, `Item`, `StatusEffect`, `TwinPistolEquipment` (5); stat rows 10; stat numbers `CombatAbilityHit`,
`CombatDamageModifiers`, `DB`, `Stat`. Largest methods: `RefreshEquipmentStats` 140 (under the limit, so no method is over 150),
`UpgradeAttribute` 64, `RefreshCharacterStatsList` 59, `RefreshStats` 52. Carried state: `_target`, `_hitPointRefreshGeneration`.
Public statics other code uses: `AttackDescription`, `FormatOffHandTooltip`, `FormatPairedDelay`, `IsViewingTarget`. It
is used for DM and beast targets as well as the player.

**Tests, the heaviest of the seven.** `CharacterSheetCombatUpgradeTests` (about ten source reads of `RefreshEquipmentStats` and
others, reflection on four methods, and `new CharacterSheetViewModel()`), `CharacterSheetStatCoverageTests` (154),
`PlayerNameRecognitionTests` (743), `PlayerSkillPointAccountingTests` (113), `DevicesFieldSupportAndAssaultGadgetsTests` (381),
`PlayerAttributeRestorationTests` (90), `WeaponDeflectionTests` (200), `GuiFakes.FakeCharacterSheet` (subclass, reads `_target` by
reflection) and `DualWieldEngineTests` (987 to 989: `new` plus reflection on the delay method). The constructor change touches
every `new CharacterSheetViewModel()` and the fake's base call.

| Part | Kind | Responsibility | Moves | Lines | Services |
|---|---|---|---|---:|---|
| `CharacterSheetAttackDelay` | singleton, leaf | which weapon is off hand, delay text, damage ability | `GetOffhandAttackWeapon`, `FormatOffHandTooltip`, `FormatPairedDelay`, `SelectForceAccuracyWeapon`, `GetEquipmentDamageAbility`, `GetAttackDelayInfo` | ~110 | `CombatAttackTiming`, `CombatQueries`, `Item`, `StatusEffect`, `TwinPistolEquipment` |
| `CharacterSheetWeaponSummary` | singleton | damage estimate per hand shown with the sheet | the body of `RefreshEquipmentStats`, `GetPrimaryCombatProfile`; returns a record | ~190 | the nine above + `CharacterSheetAttackDelay` = 10 |
| `CharacterSheetOffenseValues` | singleton, leaf | numbers for offense rows: crit rates, haste, ability bonuses, high-resource damage | `GetCriticalRate`, `GetAssaultGadgetCriticalRate`, `GetSkillRank`, `AddHighResourceAbilityDamageStats`, small readers for the haste and bonus rows | ~150 | `Stat`, `DB`, `CombatAbilityHit`, `CombatDamageModifiers`, `CombatAbilityBonus`, `CombatAttackTiming`, `CombatResources` |
| `CharacterSheetDefenseValues` | singleton, leaf | numbers for defense rows: regen, damage taken, evasion, status durations | `GetHPRegenValue`, `GetFPRegenValue`, `GetStaminaRegenValue`, `GetDamageTakenPercent`, `ApplyDamageTakenPercentAdjustment`, `GetForceEvasion`, `GetEffectStateLabel`, `GetStatusDurationLabel`, formatters, guard, movement and affinity readers | ~150 | `Stat`, `CombatDamageTaken`, `CreatureStat`, `Perk` |
| `CharacterSheetTableTabs` | singleton | the row lists of the stats, resistances and crafting tabs | `RefreshCharacterStatsList`, `RefreshResistances`, `RefreshCraftingStats`; returns `List<StatEntry>` and the others; the view model calls `StatsTable.Refresh(this, rows)` | ~240 | `Stat`, `CombatRating`, `Resistance`, `Skill`, `BeastMastery`, + the delay and two value parts = 8 |
| `CharacterSheetAttributesTab` | singleton | name, HP, FP, STM, ability scores, portrait, AP and SP labels, attribute upgrade rules (not the modal) | `RefreshStats`, `RefreshAttributes`, `RefreshPortrait`, `GetPurchasedAttributeScore`, `IsAttributeUpgradeAvailable`, the validation and write inside `UpgradeAttribute`, `RefreshHitPoints`, `RefreshLiveHitPoints` | ~260 | `DB`, `Gui`, `PlayerName`, `Stat`, `BeastMastery`, `CreatureStat`, `Skill` |
| `StatEntry`, `ResistanceEntry`, `CraftEntry` | data, own files | table rows | the nested classes | ~15 each | 0 |
| `CharacterSheetViewModel` (stays) | view model | bindings, tabs, `ShowModal` prompt, window-opening handlers, refresh events, `Initialize`, `LoadData` | the rest | ~780 | 4 parts + `Gui`, `Perk`, `BeastMastery`, `Skill`, `DB` = 9 |

The prompt and `ShowModal` stay on the view model (the base class owns the modal); the attribute part validates and applies
once the player confirms. The 6 static members stay with the view model (Question 3).

### 3.4 IncubatorViewModel (1,190 lines)

**What it is.** 54 one-line bindings (54 lines), 13 handlers (227), and about 780 lines of methods. Services by lookup: `Item`
17, `BeastMastery` 10, `DB` 5, `Perk` 4, `Gui` 4, `Targeting` 2, `IncubationFieldNote` 2, `KeyItem` 2. Carried state: 12 fields
(four item serials, three enzyme colors, `_dnaType`, `_incubatorPropertyId`, `_showResistances`, `_baseStats`, `_stageStats`).
No static state or boot handler; it has a payload and two refresh events. Largest methods: `RefreshRecipes` 63, `StartJob` 58,
`OnClickEnzyme` 57, `RefreshStageTracker` 54, `RemoveDNA` 53.

**Tests.** `IncubationPerkRegressionTests` (four source reads of `CalculateIncubationSeconds`, `GetErraticGeniusBonus`, `StartJob`,
`GetProjectedMutationChance`, `ValidateCreateJob`), `BeastMutationRecipeTests` (reflects the static `NumberOfStages`),
`IncubatorCompleteJobEngineTests` and `StationReturnInventorySpaceEngineTests` (open, reopen and drive it).

| Part | Kind | Responsibility | Moves | Lines | Services |
|---|---|---|---|---:|---|
| `IncubatorSession` | per-window data, `Feature/GuiDefinition/IncubatorWindow/` | the 12 state fields | fields | ~70 | 0 |
| `IncubatorPanelService` | singleton | what the screen shows for a job: header, stage tracker, enzyme totals, purities, mutation chance, recipes, outcomes | the `Refresh*` panels, projected percent and mutation helpers, `DescribeServerDay`, `DescribeColorCounts`, `GetErraticGeniusBonus` | ~380 | `BeastMastery`, `IncubationFieldNote`, `KeyItem`, `Perk` |
| `IncubatorItemService` | singleton | items placed in the DNA and enzyme slots, held and returned items, their stats | `RemoveDNA`, `ReturnItemsHeldElsewhere`, `RestoreHeldItems`, `LoadDNAStats`, `LoadJobStats`, `Add`, `Subtract` and `ApplyItemStats`, `DescribeEnzyme`, `EnzymeTitle`, `ClearStats`, `ResetPendingEnzymes`, the DNA and enzyme click bodies | ~400 | `Item`, `Targeting` |
| `IncubatorJobService` | singleton | the job: validate, start, continue, cancel, complete | `GetJob`, `IsJobComplete`, `ValidateCreateJob`, `StartJob`, `GetStageTotal`, `CalculateIncubationSeconds`, the job click bodies | ~230 | `BeastMastery`, `DB`, `Gui`, `Perk` |
| `IncubatorViewModel` (stays) | view model | bindings, `SwitchViews`, `Initialize`, forwarders, refresh events | the rest | ~330 | 3 parts + `Gui` = 4 |

### 3.5 DisguiseViewModel (1,029 lines)

**What it is.** 35 bindings (215 lines), 15 handlers (218), 2 refresh events, about 450 lines of methods. Services: `Disguise`
10, `Cache` 4, `DB` 4. Carried state: ten fields (disguise ids, sound set ids, indexes, option pages, page index, selection,
suppress flag, active portrait). No static state or handlers. It is 29 lines over the limit, and one responsibility is clearly
separable: the sound-set picker, about 220 lines (`LoadSoundSetOptions` through `RefreshSoundSetBindings`, the previous, next
and preview handlers, and the portrait resref helper), which owns six of the ten fields.

**Tests.** `EspionageDisguisePerkTests` (two source reads: activation-delay text and perk refresh methods, neither in the picker),
`PlayerNameRecognitionTests` (718, 976 to 1049: constants on the view model and the definition's source), `GameTextEncodingTests`
(50), `DisguiseKnownNameEngineTests`.

| Part | Kind | Responsibility | Moves | Lines | Services |
|---|---|---|---|---:|---|
| `DisguiseSoundSetPicker` | private collaborator, `Feature/GuiDefinition/DisguiseWindow/` | the sound-set combo, its paging and preview, and the portrait resref | the nine sound-set methods, the paging fields, `ResolvePortraitResref`, `GetMaxPortraitCount` | ~250 | `Cache` (passed by the view model) |
| `DisguiseViewModel` (stays) | view model | bindings, list, selection, edit and activate handlers, layout partials | the rest | ~830 | `Disguise`, `DB`, `Cache` = 3 |

The view model has about 170 lines of room under the limit. Moving the activate, retire and save flows too is not needed and is
not proposed.

### 3.6 DroidAssemblyViewModel (1,023 lines)

**What it is.** 43 bindings (215 lines), 9 handlers (370), about 310 lines of methods. Services: `Droid` 13, `Item` 12,
`Targeting` 5. Carried state: five part serials and 32 integer tallies (tier, level, AI slots, HP, stamina, 6 abilities,
12 weapon skills, armor, 8 resistances). Largest methods: `OnClickConstruct` 123, `AddPart` 87, `Reset` 60, `RemovePart` 55.
Handlers `OnClickHead`, `Body`, `Arms` and `Legs` are 43 lines each and differ only by slot; they move as they are.

**Tests.** Only `StationReturnInventorySpaceEngineTests` (opens it, reads the view model).

| Part | Kind | Responsibility | Moves | Lines | Services |
|---|---|---|---|---:|---|
| `DroidAssemblySession` | per-window data, `Feature/GuiDefinition/DroidAssemblyWindow/` | part serials and tallies | the 37 fields | ~60 | 0 |
| `DroidAssemblyStatsService` | singleton | the tally and its display | `RefreshStats`, `AdjustResistances`, `AddPart`, `RemovePart` | ~240 | `Item` |
| `DroidAssemblyActionService` | singleton | installing, returning, resetting and constructing | the five part-click bodies, `Reset`, `ReturnParts`, `ReturnPart`, `OnClickConstruct`, new droid | ~480 | `Droid`, `Item`, `Targeting`, `DroidAssemblyStatsService` |
| `DroidAssemblyViewModel` (stays) | view model | bindings, `Initialize`, forwarders | the rest | ~380 | 2 parts |

`DroidService` is split last (owner decision 5), so the `Droid.` calls in the new parts are re-pointed then.

### 3.7 EquipmentStats (1,131 lines, static)

**What it is.** A static class with 120 `GameServices` lookups and 8 services (`DB` 40, `StatRecord` 38, `Stat` 24,
`Resistance` 6, `CreatureStat` 4, `Gui` 3, `PowerGloveEquipment` 2, `Item` 2). It has one boot handler
(`RegisterStatActions`, `Rerun`) that registers 21 item property actions with `Stat`, five event handlers
(`ApplyStats`, `RemoveStats` and three refresh handlers that run after an equipment change), the public `ApplyItemStats`,
`ApplyStatAmount` and `HandledPropertyTypes`, and 21 private `Apply*` actions of 10 to 66 lines each. The 21 actions differ
by stat family: each has a player branch (adjust the record, `DB.Set`) and an NPC branch (rewrite a skin item property, through
`ReapplyNPCStat` for 15 of them). No method is over 150 lines. Callers outside the class: `LootStatRevisionMigration`
(`ApplyStatAmount`) and doc references in `Stat` and `SavedCharacter`. The gear-fed rule (`CodingStandards.md`) applies: a
record that sums worn gear is rebuilt from the slots by `Stat.RefreshPlayerEquipmentStats`, which this item does not touch.

**Tests.** `EquipmentStatActionTests` (36 tests with the fake world), `EquipmentStatsTests` (10), `StatRulesTests` (103 to 108: the
registration order of `HandledPropertyTypes`), `EquipmentRecordReconciliationTests`, `MaxHitPointsReconciliationTests`,
`CombatReadinessEquipmentTests` and `AccuracyItemPropertyTests` (the last three parse `EquipmentStats.cs` with Roslyn or read it,
and read handler attributes by reflection), `TwinPistolEquipmentEngineTests`, `EquipmentRecordReconciliationEngineTests`,
the Power Glove engine tests. `BootHandlerClassification.txt` has the `RegisterStatActions` line.

| Part | Kind, folder | Responsibility | Moves | Lines | Services |
|---|---|---|---|---:|---|
| `EquipmentVitalStatActions` | singleton, `Feature/EquipmentStatActions/` | HP, FP, FP regen, STM, STM regen, Combat Readiness | six `Apply*` | ~255 | `StatRecord`, `DB`, `CreatureStat` |
| `EquipmentRatingStatActions` | singleton | Attack, Accuracy, Force Attack, Defense, Resistance, Evasion | six `Apply*` | ~320 | `StatRecord`, `DB`, `Item`, `Resistance` |
| `EquipmentFieldStatActions` | singleton | Stealth, Detection, trap bonus, disarm, poison, lockpicking | six `Apply*` | ~240 | `StatRecord`, `DB` |
| `EquipmentCraftStatActions` | singleton | Control, Craftsmanship, CP Bonus | three `Apply*` | ~180 | `StatRecord`, `DB` |
| `NpcSkinStatWriter` | static helper, no service | rewrite one skin property for an NPC | `ReapplyNPCStat` | ~45 | 0 (NWScript only) |
| `EquipmentStats` (stays) | singleton (see below) | registration, equip and unequip handlers, the three after-change refreshes, `ApplyItemStats`, `ApplyStatAmount`, `HandledPropertyTypes` | the rest | ~270 | `Stat`, `Item`, `PowerGloveEquipment`, `StatRecord`, `CreatureStat`, `Gui` + 4 parts = 10 |

`RegisterStatActions` stays on `EquipmentStats`, with the same name and `Rerun` phase, and registers the parts' methods in the
same order, so `HandledPropertyTypes` order and the boot line do not change. A static class cannot take parts through a
constructor, so `EquipmentStats` becomes a singleton in PR 1 (the content tool converts it with its test call sites; wave C8
loses this file). Fallback if the build counts 11: move the three after-change refresh handlers into their own singleton.
Question 1.

## 4. Proof that nothing changes

### 4.1 View models (all but PlayerGuide)

1. **Transcript groundwork (PR 0, tests only).** A reusable recorder, copied from `AppearanceEditorBindingIdentityEngineTests`
   (the appearance test is not touched, because P5-16e still edits it): it records every `PropertyChanged` binding name, its
   order and its JSON from `GuiPropertyConverter`, plus the opening state. It adds, per step, the saved record JSON
   (`Player`, `Beast`, `Disguise` and the like, dates masked) and every message sent to the player, because moved code
   mostly writes data and messages, not bindings. Transcripts are captured on `343f6b31b` before any move and committed as
   embedded fixtures.
2. **Scripts per window** (each a fixed run of the window's interactions):
   - Perks: open; select an unowned, an owned and a maxed perk; buy; refund; next and previous page; the four filters; search;
     category and sort change; beast mode; a perk that adds a hot bar feat.
   - Character sheet: open on self, on an NPC target (DM mode) and on a beast; the four tabs; an attribute upgrade through the
     modal; equip and unequip refresh; dual wield (the engine test setup); skill XP and stat adjustment refresh.
   - Incubator: new job; place DNA and three enzymes; toggles; start; in progress; stage complete; continue; cancel; complete;
     reopen with items held elsewhere.
   - Disguise: open; new; save; edit; activate; deactivate; retire; unretire; portrait steps; sound-set paging and preview; empty
     state.
   - Droid assembly: open; install CPU, head, body, arms, legs; remove each; error text; construct; reset; new.
3. **PlayerGuide** is static data, so the proof is a unit test: a text dump of every topic (name, category, summary, rail
   summary, blocks, questions, related topics), captured before the move and compared byte for byte after it; plus the
   existing `PlayerGuideContentTests`.
4. **Moved bodies.** A normalized-body comparison of old and new text per moved method, run as a script and attached to the
   PR (the P5-16e approach), allowing only the `Player` to `player` parameter change and the call prefixes.
5. **Tests** (build once with `-p:RunPostBuildEvent=Never -v q -clp:ErrorsOnly`, then filters): the classes named in each
   section, `Phase5DebtTests`, `ConvertedServicesTests`, `BootHandlerClassification`, `GuiServiceReloadTests`; the engine
   classes above, with identical pass sets before and after.
6. **Client evidence** (`InEngineVisualVerification.md`), captured before the first PR and after each PR for that window, on
   one character (and one DM possession for the sheet): Perks (my perks list, a selected perk, beast tab, after buy, after
   refund); sheet (the four tabs, the upgrade prompt, an NPC target, a dual-wield character); guide (first topic, search
   results, a topic with questions and related topics); incubator (new job, items placed, in progress, stage complete);
   disguise (list, edit form with a sound-set page, retired, empty); droid (empty, all five parts, error). The captures are
   compared and shown in each PR.

### 4.2 EquipmentStats

1. **Gear-fed results transcript (PR 0).** A unit test on the fake world of `EquipmentStatActionTests` that wears a fixed set of
   gear and writes the saved player record and the NPC skin properties after each step:
   a chest with Defense (physical and Force), Resistance, Evasion, Control, Craftsmanship and CP Bonus; a helmet with HP, FP,
   FP regen, STM, STM regen, Stealth and Detection; boots with trap, disarm, poison and lockpicking; a rifle with Accuracy and
   Attack; a Combat Readiness item; a Power Glove pair (two copies per hand); a companion pistol (skipped); a DM and a
   DM-possessed wearer (skipped); an NPC skin that already has values, to hit the remove-at-zero branch. Each is worn,
   removed, swapped through `ApplyStats` and `RemoveStats`, and followed by the three after-change handlers and
   `Stat.RefreshPlayerEquipmentStats`. The text is captured on the base commit and compared after each PR. The list of
   registered types and their order is part of it.
2. The existing classes above run before and after with identical results, including the Roslyn-based source tests, re-pointed to
   the files that now hold the moved text, and the engine reconciliation and Twin Pistol tests.
3. **Client evidence.** One character wears a fixed outfit; the character sheet stats, resistances and crafting tabs and the
   status bars are captured before and after each PR, and again after unequipping one piece.
4. `git diff --stat` shows no change to `Stat`, `StatRecord`, `EquipmentStatTotals`, `Module/`, 2DA or `SWLOR_Haks`.

## 5. PR plan and baseline changes

Each PR starts from up-to-date `origin/feature/maintainability-refactor`, moves at most about 1,000 lines, gets a Codex review
of its head, and runs the gate with coverage. "Moved" counts the lines taken out of the stay-type.

| PR | Content | Moved | Stay-type after |
|---|---|---:|---|
| 0 | Test groundwork: recorder, the five transcripts, the guide dump, the gear transcript; no production code | 0 | unchanged |
| 1 | `EquipmentStats` to a singleton (tool run, `LootStatRevisionMigration` and the four test files updated); vitals and craft parts, `NpcSkinStatWriter` | ~600 | ~560 lines |
| 2 | Rating and field parts | ~530 | ~270 lines, 10 services |
| 3 | `PlayerGuide`: catalog, eight section types, three data types | ~1,085 | ~290 lines |
| 4 | `Incubator`: session and three parts | ~1,000 | ~330 lines |
| 5 | `Disguise` (sound-set picker) and `DroidAssembly` (session and two parts) | ~220 + ~600 | ~830 and ~380 lines |
| 6 | `Perks`: four parts (after P5-16g merges) | ~1,000 | ~620 lines |
| 7 | `CharacterSheet`: delay, weapon summary, two value parts, table tabs, row records | ~590 | ~960 lines, still 14 lookups |
| 8 | `CharacterSheet`: attributes tab | ~220 | ~780 lines, 9 services |

After PR 7 the sheet is under the line limit but not the service limit; PR 8 finishes it.

**Baseline changes** (each PR regenerates the Phase 5 snapshots with `SWLOR_UPDATE_SNAPSHOTS=1` and reviews the diff):
- `LargeTypes.txt`: the seven lines go (`EquipmentStats` in PR 2; the view models in PRs 3, 4, 5 (two), 6 and 7).
- `ServicesPerType.txt`: the `CharacterSheetViewModel` line goes in PR 8.
- `StaticLookups.txt`: each file's count drops as its lookups become constructor services (`EquipmentStats` 120 to 0 in PR 1);
  new files have none.
- `StaticState.txt`: the `PlayerGuideViewModel` line goes in PR 3. `CharacterSheetViewModel` stays at 6 (Question 3).
- `UpwardEdges.txt`: the three `CharacterSheetViewModel` rows move to the new files that hold the calls; counts do not change.
- `LongMethods.txt`: none (P5-16g). `DebtReport.md` is regenerated.
- `ContentInjection.json` and `ContentRegistrations.cs`: new parts, `EquipmentStats` and each view model that is not the sheet.
- `BootHandlerClassification.txt`, `HandlerRenames.txt`, `ScriptHandlerOrder.txt`: no change expected (names, phases and order of
  the handlers are kept); the guard tests confirm it.
- Source-reading tests listed in section 3 are re-pointed in the PR that moves their text.

## 6. Conflicts and order

| With | Files in common | What to do |
|---|---|---|
| P5-16g | `PerksViewModel` (it splits `OnClickBuyUpgrade`), `CombatStyleGuideTopics` (exempt) | PR 6 starts after P5-16g merges. PRs 3 to 5 do not touch its files. |
| P5-5c to P5-5h (master frozen) | `CharacterSheetViewModel` has one call, `StatusEffect.TryGetLimitedAttackDelayReduction`, that P5-5g's part 6 moves; no other file in this map | PR 7 starts after P5-5g merges, so the call is rewritten once. Everything else runs beside P5-5. |
| P5-7 (Combat services) | `CharacterSheetViewModel` reaches `CombatAbilityBonus`, `CombatAbilityHit`, `CombatAttackTiming`, `CombatDamageModifiers`, `CombatDamageTaken`, `CombatResources` | PRs 7 and 8 merge before P5-7 starts, so P5-7 edits the small new files. |
| P5-10 (`PerkService`, `SkillService`, `PlayerNameService`, `BeastMasteryService`, `DroidService`) | Perk 20 calls in Perks, `BeastMastery` in Incubator and the sheet, `Droid` in Droid | PRs 4 to 8 merge before P5-10 starts. It then rewrites receivers in the new parts, which already name their services in one place each. |
| P5-9 (`ItemService`, last) | `Item` in Incubator (17), Droid (12), the sheet, `EquipmentStats` | Same as P5-10: P5-16h goes first. |
| P5-16e | `Domain/UI` (the sink), the engine test recorder | Question 2. The appearance binding test is not touched. |
| P5-14 C3, C4, C8 | view model constructors, `EquipmentStats` conversion | P5-14 starts after P5-16 by plan. C8 loses `EquipmentStats` and C3 and C4 find six view models already constructor-injected (the sheet's statics remain). |

Test files shared between PRs: `PlayerNameRecognitionTests` (Disguise, sheet), `PlayerSkillPointAccountingTests` (Perks, sheet),
`WeaponDeflectionTests` (guide, sheet), `StationReturnInventorySpaceEngineTests` (Droid, Incubator). Two PRs that share one never
run at the same time. **Recommended order**, at most three lanes (the coordinator counts P5-5 and P5-16e/f/g agents too):

1. PR 0.
2. Lane A: PR 1, PR 2, then PR 3. Lane B: PR 4, then PR 5. Lane C: PR 6 once P5-16g has merged.
3. PRs 7 and 8 last, after PRs 3, 5 and 6 and after P5-5g, and before P5-7 and P5-10.

## 7. Open questions

1. **Convert `EquipmentStats` to a singleton now?** Splitting needs it: a static class cannot take parts. The cost is a
   mechanical edit of `LootStatRevisionMigration` (migrations are never edited otherwise) and about 60 test call sites, which the
   content tool writes. **Recommended: yes**, since wave C8 would do it anyway. The alternative (parts register themselves from
   their own boot handlers) changes the handler list and the registration order that `StatRulesTests` pins.
2. **One shared `IGuiBindingSink` in `Domain/UI`**, adopted by P5-16e PR 2 in place of `IAppearanceBindingSink` (not written
   yet). **Recommended: yes**; it costs one rename in a branch with no code for it, and six windows share one seam.
3. **The sheet's six static members** (`Tabs`, `TopToggles`, `BottomToggles`, three `GuiTableSource` fields) are in
   `StaticState.txt`, and the content tool refuses to convert a made type with static state. **Recommended:** leave them on the
   view model, do not list the sheet in `ContentInjection.json`, give it its parts through a hand-written constructor (as P5-16e
   PR 2 will for the editor), and let C3 classify the six with the owner.
4. **PlayerGuide in eight section types, one PR of about 1,085 moved lines** (the guide data is a verbatim move, proved by a byte
   dump). **Recommended: yes, one PR**; the other choice, grouping to four types of about 250 lines, loses the one-to-one match
   with the P5-16c methods. Say if you want two PRs of about 540.
