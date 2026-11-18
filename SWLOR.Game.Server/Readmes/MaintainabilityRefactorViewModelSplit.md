# Maintainability Refactor: Remaining View Models and EquipmentStats Split (P5-16h)

Part map for owner review, revised after the code review; it changes no code. Base: `origin/feature/maintainability-refactor` at
`343f6b31b`. Limits come from [MaintainabilityRefactorPhase5.md](MaintainabilityRefactorPhase5.md): 1,000 lines and 10 services
per type, 150 lines per method, no partial-class or numbered-chunk tricks. It follows
[MaintainabilityRefactorAppearanceEditorSplit.md](MaintainabilityRefactorAppearanceEditorSplit.md) (P5-16e): PR 1 of that item
(static types) for `EquipmentStats`, and PR 2 (a typed per-window host) for the view models. Line counts are measured from the
code with a brace outline of each file. Service counts are the distinct `GameServices` members a line range reaches, plus, in
brackets, the static helpers that become injected services after P5-12 and C8 (`WeaponDamage`, `EquipmentPredicates`,
`DeviceAbilityEffects`, `WeaponAttackTiming`, `TemporaryHitPointEffects`). Each is re-measured at build.

## Owner decisions applied (coordinator, after the review)

1. `EquipmentStats` is split now into static types by stat family, as P5-16e PR 1 did. It is not made a singleton here (the
   content tool would refuse the static caller `LootStatRevisionMigration.cs:25`, the static engine test
   `TwinPistolEquipmentEngineTests.cs:21` and the 21 method groups in `RegisterStatActions`). The registrations become lambdas
   written by hand; the `HandledPropertyTypes` order and the `Rerun` boot line do not change. C8 and C2 convert the statics.
2. No shared binding sink. Each window gets one typed host interface (the shape of `IAppearanceEditorHost` on P5-16e PR 2),
   implemented explicitly by the view model, so a property setter keeps its logic and a moved body changes only its receiver.
3. `CharacterSheetViewModel` is listed in `ContentInjection.json` like the other view models. Its six statics reach no service,
   so the tool accepts it; they keep their `StaticState.txt` line for C3.
4. `PlayerGuide`: one static data type per topic category, composed in today's order (not the eight P5-16c runs).

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

Out of scope, as named: `ShipManagementViewModel` and `ShipManagementDefinition`.

**What the list misses.** No other view model is over 1,000 lines or 10 services in the Phase 5 baselines; `AppearanceEditorViewModel`
is P5-16e. Every long method in a view model is in the P5-16g branch (`OnClickBuyUpgrade` 162, `OnClickResetEverything` 158,
`ConstructSaber` 151, `StablesDefinition.BuildWindow` 741), which also exempts `CombatStyleGuideTopics.Create` (224) as a data
catalog member. This map assumes P5-16g merges first. `DMToolsViewModel` (924 lines) and `ExtraHotbarViewModel` (910) are close to
the line limit; they are reported, not planned.

## 2. Rules shared by every type

- **The view model keeps** its bindings, `Initialize`, the click handlers (as forwarders), the `Refresh` events, every property
  whose setter has logic (`SelectedTabId` `CharacterSheetViewModel.cs:145-161`, `SelectedSoundSetIndex` and
  `SelectedSoundSetPageIndex` `DisguiseViewModel.cs:105-142`, `SelectedPage` `PerksViewModel.cs:62-71`, the filter and sort setters),
  and the members the host exposes. Everything else moves.
- **The host.** One interface per window (`IPerksHost`, `ICharacterSheetHost`, `IIncubatorHost`, `IDisguiseHost`,
  `IDroidAssemblyHost`), in the parts' folder. It lists what the moved code reads or writes on the view model: `Player`, the
  window's session, the bindings it sets (by property, so setters run), and callbacks the view model owns (`ShowModal`,
  `ChangePartialView`, `SelectPerkAt`, `SwitchViews`, `RefreshAllStats`, `LoadData`). The view model implements it explicitly
  by delegating to the members it already has. A part method takes the host as its first argument. Moved bodies change only
  their receiver (`Player` to `host.Player`, `Total = x` to `host.Total = x`), so "verbatim" below means receiver-only.
  Fakes implement the host in unit tests. P5-16e's files are not touched.
- **Constructor injection** (Phase 5 owner decision 8). Each part takes only the services it uses through its constructor, with no
  `GameServices` use, and is listed in `tools/Refactor/ContentInjection.json`, so the tool registers it as singleton content.
  Each view model is listed too (the base class keeps its lookup until C3, as `ConversationViewModel` does), so the tool writes
  its constructor and maintains every `new XViewModel()` in tests and `GuiFakes.FakeCharacterSheet`. A static member that reaches a
  service loses `static` as it moves, or stays on the view model and loses it there (the two Disguise portrait helpers).
- **Where.** The parts use Combat, Companions, Items and Abilities services, which UI may not reference, so they are Feature
  content in `Feature/GuiDefinition/<Window>Window/` (first folder `GuiDefinition`, which is already in `DomainMap.json`; no new
  entry). `Feature/EquipmentStatActions/` is a new first-level folder and gets a `DomainMap.json` entry (recommended domain:
  Character; `DomainMapTests` confirms).
- **Layers.** Every part table names the parts a part uses and its layer (0 uses none). A part never uses a part of its own or a
  higher layer. The host is not a part: a part reaches the view model's own callbacks through it.
- **Per-window state** lives in a plain `<Window>Session` class that the view model creates in `Initialize` (Incubator, Droid), or
  inside a private collaborator the view model creates (Disguise). Parts are stateless singletons.
- **Hot reload** (`HotReload.md`, `GuiServiceReloadTests`): a view model without a payload (Perks, PlayerGuide, Disguise, Droid) is
  reopened after a reload; one with a payload (CharacterSheet, Incubator) is closed. No window state is carried.
- **If a build reaches 11 services,** split by the sub-responsibility named in the part's row, never by line count.

## 3. Types

### 3.1 PlayerGuideViewModel (1,367 lines, data)

**What it is.** About 290 lines of logic (search, topic list, related topics, 4 handlers, `MatchesSearch`) and about 1,030 lines
of topic text: `BuildTopics` over eight run builders (`PlayerGuideViewModel.cs:303-1215`), plus `StatusEffectGuideTopics` and
`CombatStyleGuideTopics`, and the nested data classes `PlayerGuideTopic`, `ArticleBlock`, `QuestionAnswer` (about 55 lines). The
eight builders are consecutive runs of today's list, not content groups: their 30 topics belong to seven categories that
interleave (Core, Social, Core, Core, Progression, Combat, Progression, Core, Combat, ...). No service. Static state: `Topics`.

**Tests.** `PlayerGuideContentTests` reads `Topics` and `MatchesSearch` by reflection. `PlayerFacingNameBroadcastTests` (223),
`RoleplayXPOOCPenaltyTests` (456) and `WeaponDeflectionTests` (202) read the source text of `PlayerGuideViewModel.cs`; their
`NotContain` assertions would pass for the wrong reason once the text leaves the file, so they read every guide file.
`CombatStyleGuideTopics.cs:240-284` and `StatusEffectGuideTopics.cs:10-55` name the nested types and are edited.

| Part | Layer | Uses | Moves (topics, in today's order) | Lines | Services | Kind |
|---|---|---|---|---:|---|---|
| `PlayerGuideCoreTopics` | 0 | - | Common Questions, Skills, Skill Decay, XP Debt, Death & Recovery | ~185 | 0 | verbatim |
| `PlayerGuideSocialTopics` | 0 | - | Communication, Disguises | ~105 | 0 | verbatim |
| `PlayerGuideProgressionTopics` | 0 | - | Perks, Perk Refunds, Training Store | ~105 | 0 | verbatim |
| `PlayerGuideCombatTopics` | 0 | - | Force Affinity, Abilities, Mimicry & Techniques, PvP Combat, Combat Basics, Stat Caps | ~195 | 0 | verbatim |
| `PlayerGuideCharacterTopics` | 0 | - | Attributes, Rebuilds | ~75 | 0 | verbatim |
| `PlayerGuideActivitiesTopics` | 0 | - | Espionage, Crafting, Gathering & Fishing, Beasts & Stables, Droids, Quests & Key Items, Quest Contracts, Guilds & Citizenship, Housing & Markets, Travel & Navigation, Ships & Space | ~370 | 0 | verbatim |
| `PlayerGuideInterfaceTopics` | 0 | - | Useful Windows | ~50 | 0 | verbatim |
| `PlayerGuideTopic`, `ArticleBlock`, `QuestionAnswer` | 0 | - | the nested classes, one file each | ~20 each | 0 | verbatim |
| `PlayerGuideTopicCatalog` (static) | 1 | the seven category types, `StatusEffectGuideTopics`, `CombatStyleGuideTopics` | `Topics`, `BuildTopics`: lists the 30 topic methods in today's order, then the two existing types | ~70 | 0 | rewrite |
| `PlayerGuideViewModel` (stays) | - | the catalog | the rest | ~290 | 0 | - |

Each category type has one static method per topic (24 to 50 lines each), so no method nears 150 and no type nears 1,000 (the
largest, Activities, is about 370; the fallback of one data type with a `typeSize` exemption is not needed). A catalog with no
service cannot be a registered content singleton, so `Topics` stays a static field, now on `PlayerGuideTopicCatalog`; its
`StaticState.txt` line is renamed to the new type and kept for classification. `MatchesSearch` stays on the view model.

### 3.2 PerksViewModel (1,544 lines)

**What it is.** 37 bindings (202 lines), 11 click handlers (336), 3 refresh events, about 1,000 lines of methods. Services by lookup:
`Perk` 20 calls, `DB` 17, `QuickCast` 10, `AbilityCatalog` 5, `Currency` 4, `Recast` 2, `Gui` 2, `Skill` 1. No static state, no handlers.
Carried state: `_pages`, `_selectedPerkIndex` (a private property with logic, 158-170), `_filteredPerks`, `_initialLoadDone`. Largest methods:
`LoadPerks` 124, `OnClickRefund` 102, `ConfirmBuyUpgrade` 86 and `SelectPerkAt` 74 (P5-16g splits `OnClickBuyUpgrade` into
`OnClickBuyUpgrade`, `ConfirmBuyUpgrade`, `TryGetPerkRank` and `RefreshAfterUpgrade`). `.Where(CanAddFeatToHotBar)`
(`:997`, `:1002`) is a method group over a static; it moves with the hot bar service and stays a static in that type, or becomes a lambda.

**Tests and tools.** `PerksWindowTests` (five source reads and reflection on rank, requirement and detail-text methods),
`ForceLightConsularTests` (invokes the detail text method on `new PerksViewModel()`), `ForceLightGuardianTests` (536),
`PlayerSkillPointAccountingTests` (126), `PerkRefundStatusEffectCleanupTests` (185), `DMChatCommandTests` (136). `tools/UpdateCraftingMinigamesBible.py`
(597-598) reads the refund hours, `DateTime.UtcNow.AddHours(n)`, and the refund text from `PerksViewModel.cs`: PR 0 changes its
pattern to `Clock.UtcNow.AddHours(n)` (the clock seam, section 4.1), PR 6 points its path at `PerkPurchaseService.cs`.

| Part | Layer | Uses | Moves | Lines | Services | Kind |
|---|---|---|---|---:|---|---|
| `PerkListBuilder` | 0 | - | `LoadCategories`, `LoadDetails`, `LoadForceAffinityDetails`, `LoadPerks` (calls `host.SelectPerkAt(0)`), `SortPerks`, `GetPerkRowStatus`, `GetCurrentPerkRank`, `GetDisplayPerkRank`, the required-level helpers, `BuildRequirements` | ~390 | `Perk`, `DB`, `Skill`, `Currency` (4) | verbatim |
| `PerkDetailText` | 0 | - | `BuildSelectedPerkDetailText`, `BuildAbilityCostText`, `BuildStatusEffectPerkDetailText`, `BuildForceAffinityPerkDetailText`, `BuildRecastGroupText`, `FormatForceAffinity`, `IsForcePerkCategory` | ~175 | `AbilityCatalog`, `Perk`, `Recast` (3) | verbatim |
| `PerkHotBarService` | 0 | - | `GrantFeats`, `SyncHotBarActiveAbilityFeats`, `CanAddFeatToHotBar`, the add, remove and is-on-bar helpers | ~215 | `AbilityCatalog`, `Perk`, `QuickCast` (3) | verbatim |
| `PerkPurchaseService` | 1 | `PerkListBuilder`, `PerkDetailText`, `PerkHotBarService` | `ConfirmBuyUpgrade`, `TryGetPerkRank`, `RefreshAfterUpgrade`, the refund body (it ends in `LoadDetails` and `LoadPerks`), `ApplyPurchasePerkTriggers` | ~310 | `DB`, `Perk`, `Currency`, `QuickCast`, `Gui` + 3 parts (8) | verbatim |
| `PerksViewModel` (stays) | - | all four | bindings, setters, `SelectPerkAt`, `OnClickBuyUpgrade` (its `ShowModal` call), handler forwarders, refresh events, `Initialize`, the host | ~620 | the four parts + `DB`, `Perk`, `Currency` (7) | - |

The refresh after a purchase moves with the purchase (it uses the builder and the detail text, which Purchase declares), so there
is no cycle. `OnClickBuyUpgrade` stays as the 17-line view model handler that opens the modal.

### 3.3 CharacterSheetViewModel (1,516 lines, 21 services)

**What it is.** 59 bindings (315 lines) over four tabs and a header, 22 handlers (15 open another window, 6 upgrade an attribute),
13 refresh events, a live hit-point loop on `Scheduler` (`:1395-1404`), three nested row classes (41 lines), and 6 static members
(`Tabs`, `TopToggles`, `BottomToggles`, `StatsTable`, `ResistancesTable`, `CraftingTable`). `RefreshEquipmentStats` is 140 lines
(`:781-920`) with a local function. The public statics `FormatOffHandTooltip`, `FormatPairedDelay` and the const
`AttackDescription` are used by tests; `IsViewingTarget` (`:140`) is used by `GuiService.cs:573` (an existing logic-to-content
reference, `LogicToContent.txt`) and stays. The static `GetEquipmentDamageAbility` (`:959`) reaches `CombatQueries`.
It is also the DM and beast target view.

**Tests (heaviest).** `CharacterSheetCombatUpgradeTests` (about ten source reads of `RefreshEquipmentStats` and others, reflection on
four methods, `new CharacterSheetViewModel()`), `CharacterSheetStatCoverageTests` (154), `PlayerNameRecognitionTests` (743),
`PlayerSkillPointAccountingTests` (113), `DevicesFieldSupportAndAssaultGadgetsTests` (381), `PlayerAttributeRestorationTests` (90),
`WeaponDeflectionTests` (200), `GuiFakes.FakeCharacterSheet` (subclass, reads `_target`), `DualWieldEngineTests` (987-989).

Rows are cut into four builders by real reach, in the order of today's list; `RefreshCharacterStatsList` becomes a short orchestrator.
The code that calls `GetPrimaryCombatProfile` (`:1018`) is the orchestrator, so the profile moves with it.

| Part | Layer | Uses | Moves | Lines | Services now (later) | Kind |
|---|---|---|---|---:|---|---|
| `CharacterSheetFormat` (static, no service) | 0 | - | `FormatPercent`, `FormatMultiplier`, `GetStatusDurationLabel`, `FormatOffHandTooltip`, `FormatPairedDelay` | ~55 | 0 | verbatim |
| `CharacterSheetWeaponTiming` | 0 | - | `GetOffhandAttackWeapon`, `SelectForceAccuracyWeapon`, `GetAttackDelayInfo` | ~90 | `CombatAttackTiming`, `Item`, `StatusEffect`, `TwinPistolEquipment` (4) (+ `EquipmentPredicates`, `WeaponAttackTiming` = 6) | verbatim |
| `CharacterSheetDamageEstimate` | 0 | - | `GetEquipmentDamageAbility` (loses `static`), the local function `GetCombatInfo` lifted to a method with a `target` parameter | ~80 | `CombatDamageModifiers`, `CombatQueries`, `CombatRating`, `DB`, `Item`, `PairedRangedTraits`, `Skill`, `Stat` (8) (+ `WeaponDamage` = 9) | rewrite (local function to method) |
| `CharacterSheetWeaponRatings` | 1 | `WeaponTiming`, `DamageEstimate` | `RefreshEquipmentStats` without the local function and without its last three calls | ~110 | `BeastMastery`, `CombatRating`, `Item`, `PairedRangedTraits`, `Skill` + 2 parts (7) | verbatim |
| `CharacterSheetAttributesTab` | 0 | - | `RefreshStats` (calls `host.RefreshHitPoints()`), `RefreshAttributes`, `GetPurchasedAttributeScore`, `IsAttributeUpgradeAvailable`, `UpgradeAttribute` (uses `host.ShowModal` and `host.LoadData`) | ~200 | `BeastMastery`, `CreatureStat`, `DB`, `PlayerName`, `Skill`, `Stat` (6) | verbatim |
| `CharacterSheetSurvivalRows` | 0 | - | the rows `HP Regen` to `Force DEF %` (`:1020-1032`), `IsPlayerCharacterTarget`, `GetPlayerEntity`, the three regen readers, `GetDamageTakenPercent`, `ApplyDamageTakenPercentAdjustment` | ~105 | `CombatDamageTaken`, `CombatRating`, `DB`, `Stat` (4) (the `NaturalRegeneration` constant is a Gameplay content edge) | verbatim |
| `CharacterSheetAccuracyRows` | 1 | `WeaponTiming` | the rows `Ability Accuracy` to `Critical Damage` (`:1033-1045`), `GetCriticalRate`, `GetAssaultGadgetCriticalRate`, `GetSkillRank`, its own `IsPlayerCharacterTarget` and `GetPlayerEntity` | ~85 | `BeastMastery`, `CombatAbilityBonus`, `CombatAbilityHit`, `CombatDamageModifiers`, `CombatRating`, `DB`, `Skill`, `Stat` (8) (+ `DeviceAbilityEffects` = 9, + the timing part = 10) | verbatim |
| `CharacterSheetDamageRows` | 0 | - | the rows `Damage Dealt` to `Single Weapon DMG` (`:1046-1056`), `AddHighResourceAbilityDamageStats` | ~60 | `CombatAttackTiming`, `CombatResources`, `Stat` (3) (+ `EquipmentPredicates`, `WeaponDamage` = 5) | verbatim |
| `CharacterSheetUtilityRows` | 0 | - | the rows `Ranged Evasion` to `Experience` (`:1057-1065`), `GetEffectStateLabel`, `GetForceEvasion` | ~55 | `CreatureStat`, `Perk`, `Stat` (3) | verbatim |
| `CharacterSheetTableTabs` | 2 | the four row parts, `DamageEstimate` | `RefreshCharacterStatsList` as the orchestrator (list, `AddStat`, calls the four builders in order, `GetPrimaryCombatProfile`), `RefreshResistances`, `RefreshCraftingStats` | ~110 | `BeastMastery`, `Resistance`, `Skill`, `Stat` + 5 parts (9) | rewrite (orchestrator) |
| `StatEntry`, `ResistanceEntry`, `CraftEntry` | 0 | - | the nested classes | ~15 each | 0 | verbatim |
| `CharacterSheetViewModel` (stays) | - | `WeaponRatings`, `AttributesTab`, `TableTabs` | bindings, tabs, nav handlers, upgrade handlers (forwarders), refresh events, `Initialize`, `LoadData`, `RefreshHitPoints` and the live loop (stay), `RefreshEquipmentStats` as a 5-line method (the part, then the three table refreshes), the host | ~880 | 3 parts + `Gui`, `Perk`, `BeastMastery`, `Skill`, `DB` (8) (+ `TemporaryHitPointEffects` = 9) | rewrite (`RefreshEquipmentStats`) |

`UpwardEdges.txt:57-58` has two rows today (UI content to Abilities content 2, to Gameplay content 1). They become three rows in three
files: the view model (`TemporaryHitPointEffects`), the accuracy rows (`DeviceAbilityEffects`) and the survival rows (`NaturalRegeneration`).
Each part is at 10 or fewer even after P5-12 and C8; `CharacterSheetAccuracyRows` is at 10. Its fallback is to split the critical
rows (`Critical Rate`, `GetCriticalRate`, `GetSkillRank`, `GetAssaultGadgetCriticalRate`) from the accuracy rows.

### 3.4 IncubatorViewModel (1,190 lines)

**What it is.** 54 one-line bindings, 13 handlers (227 lines), about 780 lines of methods. Services: `Item` 17, `BeastMastery` 10, `DB` 5,
`Perk` 4, `Gui` 4, `Targeting` 2, `IncubationFieldNote` 2, `KeyItem` 2. Carried state: 12 fields. It has a payload, no static state or
handlers. It reads the clock at `:129`, `:254`, `:1038` (and 888, 1017, 1158). Largest methods: `RefreshRecipes` 63, `StartJob` 58, `OnClickEnzyme` 57,
`RefreshStageTracker` 54, `RemoveDNA` 53.

**Tests.** `IncubationPerkRegressionTests` (source reads of `CalculateIncubationSeconds`, `GetErraticGeniusBonus`, `StartJob`,
`GetProjectedMutationChance`, `ValidateCreateJob`), `BeastMutationRecipeTests` (reflects `NumberOfStages`, which moves to the calculator),
`IncubatorCompleteJobEngineTests` and `StationReturnInventorySpaceEngineTests`.

| Part | Layer | Uses | Moves | Lines | Services | Kind |
|---|---|---|---|---:|---|---|
| `IncubatorSession` | 0 | - | the 12 state fields | ~70 | 0 | verbatim |
| `IncubatorJobCalculator` | 0 | - | `GetJob`, `IsJobComplete`, `CalculateIncubationSeconds`, `GetErraticGeniusBonus`, `GetProjectedMutationChance`, `GetStageTotal`, `NumberOfStages` | ~85 | `DB`, `Perk` (2) | verbatim |
| `IncubatorPanelService` | 1 | calculator | `RefreshHeader`, `DescribeServerDay`, `RefreshStageTracker`, `RefreshEnzymeTotals`, `DescribeColorCounts`, `RefreshPurities`, `GetProjectedPercent`, `RefreshMutation`, `RefreshRecipes`, `RefreshOutcomes`, `RefreshIncubationTime` | ~330 | `BeastMastery`, `IncubationFieldNote`, `KeyItem` + calculator (4) | verbatim |
| `IncubatorItemService` | 1 | calculator | `RemoveDNA`, `ReturnItemsHeldElsewhere`, `RestoreHeldItems`, `ClearStats`, `ResetPendingEnzymes`, `LoadJobStats`, the DNA click body, `LoadDNAStats`, the item-stat helpers, `DescribeEnzyme`, `EnzymeTitle`, `OnClickEnzyme` | ~370 | `Item`, `Targeting` + calculator (3) | verbatim |
| `IncubatorJobService` | 1 | calculator | `ValidateCreateJob`, `StartJob`, the start, continue, cancel and complete click bodies | ~190 | `BeastMastery`, `DB`, `Gui`, `Perk` + calculator (5) | verbatim |
| `IncubatorViewModel` (stays) | - | all four | bindings, `SwitchViews`, `RefreshAllStats` (stay, reached through the host), the enzyme forwarders, toggles, `Initialize`, refresh events, the host | ~410 | the four parts + `Gui` (5) | - |

The click bodies call `host.ShowModal`, `host.SwitchViews` and `host.RefreshAllStats`; the refresh tails stay in the view model
and no part uses the panel service except through the host.

### 3.5 DisguiseViewModel (1,029 lines)

**What it is.** 35 bindings (215 lines), 15 handlers (218), 2 refresh events, about 450 lines of methods. Services: `Disguise` 10, `Cache` 4,
`DB` 4. Carried state: ten fields. No static state or handlers. It is 29 lines over the limit. One responsibility is separable:
the sound-set picker, `LoadSoundSetOptions` to `RefreshSoundSetBindings` (`:847-1010`, 164 lines), the preview body (14) and the
paging fields; it owns six of the ten fields. The portrait helpers (`:1012-1027`) are not part of it and stay.

**Tests.** `EspionageDisguisePerkTests` (source reads of the activation-delay text and perk refresh methods, neither in the picker),
`PlayerNameRecognitionTests` (718, 976-1049), `GameTextEncodingTests` (50), `DisguiseKnownNameEngineTests`.

| Part | Layer | Uses | Moves | Lines | Services | Kind |
|---|---|---|---|---:|---|---|
| `DisguiseSoundSetPicker` (private collaborator the view model creates in `Initialize`) | 0 | - | `LoadSoundSetOptions`, `SelectSoundSet`, `GetDefaultSoundSetId`, `SanitizeSoundSetIndex`, `SanitizeSoundSetPageIndex`, `GetCurrentSoundSetPageSize`, `GetSoundSetPageCount`, `LoadSoundSetPageNumbers`, both `LoadSoundSetPageOptions`, `GetSelectedSoundSetIndexOnCurrentPage`, `SetSelectedSoundSetFromPageIndex`, `RefreshSoundSetBindings`, the preview body; the six paging fields | ~215 | `Cache` (1, passed by the view model) | verbatim |
| `DisguiseViewModel` (stays) | - | the picker | bindings and the two setters (they call the picker), list, selection, edit and activate handlers, layout partials, the portrait helpers (`GetMaxPortraitCount` and `ResolvePortraitResref`, which lose `static` because they reach `Cache`), the host | ~890 | `Disguise`, `DB`, `Cache` (3) | rewrite (the two setter bodies call the picker) |

About 110 lines of room remain under the limit. Moving the activate, retire and save flows too is not proposed.

### 3.6 DroidAssemblyViewModel (1,023 lines)

**What it is.** 43 bindings (215 lines), 9 handlers (370), about 310 lines of methods. Services: `Droid` 13, `Item` 12, `Targeting` 5. Carried
state: five part serials and 32 integer tallies. Largest methods: `OnClickConstruct` 123, `AddPart` 87, `Reset` 60, `RemovePart` 55. The four
slot handlers (43 lines each) differ by slot and move as they are. Tests: only `StationReturnInventorySpaceEngineTests`. `DroidService` is
split last (owner decision 5); the `Droid` calls in the new parts are re-pointed then.

| Part | Layer | Uses | Moves | Lines | Services | Kind |
|---|---|---|---|---:|---|---|
| `DroidAssemblySession` | 0 | - | the 37 fields | ~60 | 0 | verbatim |
| `DroidAssemblyStatsService` | 0 | - | `RefreshStats`, `AdjustResistances`, `AddPart`, `RemovePart` | ~240 | `Item` (1) | verbatim |
| `DroidAssemblyActionService` | 1 | stats service | the five part-click bodies, `Reset`, `ReturnParts`, `ReturnPart`, `ShowError`, `ClearError`, new droid, `OnClickConstruct` | ~480 | `Droid`, `Item`, `Targeting` + stats (4) | verbatim |
| `DroidAssemblyViewModel` (stays) | - | both | bindings, `Initialize`, forwarders, `OnWindowClosed`, the host | ~420 | 2 parts | - |

### 3.7 EquipmentStats (1,131 lines, static)

**What it is.** A static class with 120 `GameServices` lookups and 8 services (`DB` 40, `StatRecord` 38, `Stat` 24, `Resistance` 6, `CreatureStat` 4, `Gui` 3,
`PowerGloveEquipment` 2, `Item` 2). It has one boot handler (`RegisterStatActions`, `Rerun`, `:30-53`: 21 method groups), five event handlers
(`ApplyStats`, `RemoveStats` and three refresh handlers after an equipment change), `ApplyItemStats`, `ApplyStatAmount`,
`HandledPropertyTypes`, and 21 private actions of 10 to 66 lines. Each action has a player branch (adjust the record, `DB.Set`); 15 also have an
NPC branch through `ReapplyNPCStat`, Defense and Resistance their own; the three craft actions have a player branch only. No method is over
150 lines. The gear-fed rule (`CodingStandards.md`) applies: records summed from worn gear are rebuilt by `StatRecord.RefreshPlayerEquipmentStats`,
which this item does not touch. The displaced-record constant `EQUIPMENT_DISPLACED_RECORD_STATS` is written in `ApplyStats` (`:125`) and
read in the refresh handler (`:224`); the three refresh handlers sit in `ScriptHandlerOrder.txt:624-626` and `647-649`.

**Callers and tests.** `LootStatRevisionMigration.cs:25` calls `ApplyStatAmount`, which stays public static, so it is not edited.
`EquipmentStatActionTests` (36), `EquipmentStatsTests` (10), `StatRulesTests` (103-108, registration order), `EquipmentRecordReconciliationTests`,
`MaxHitPointsReconciliationTests`, `CombatReadinessEquipmentTests` and `AccuracyItemPropertyTests` (the last three parse `EquipmentStats.cs`;
the stay-type keeps the handlers they parse), `TwinPistolEquipmentEngineTests`, the reconciliation and Power Glove engine tests.
`SWLOR.CraftSim/Legacy/LegacySource.cs:84-87` (`GearStats`) cites `EquipmentStats` lines 930-1126 and four anchors for Control, Craftsmanship and
CP Bonus; PR 1 re-cites it to `EquipmentCraftStatActions.cs` with new lines, checked by `LegacySourceTests.cs:27-45`.

| Part | Layer | Uses | Moves | Lines | Services | Kind |
|---|---|---|---|---:|---|---|
| `NpcSkinStatWriter` (static) | 0 | - | `ReapplyNPCStat` | ~45 | 0 (NWScript only) | verbatim |
| `EquipmentVitalStatActions` (static) | 1 | writer | HP, FP, FP regen, STM, STM regen, Combat Readiness | ~255 | `StatRecord`, `DB`, `CreatureStat` (3) | verbatim |
| `EquipmentRatingStatActions` (static) | 1 | writer | Attack, Accuracy, Force Attack, Defense, Resistance, Evasion | ~320 | `StatRecord`, `DB`, `Item`, `Resistance` (4) | verbatim |
| `EquipmentFieldStatActions` (static) | 1 | writer | Stealth, Detection, trap bonus, disarm, poison, lockpicking | ~240 | `StatRecord`, `DB` (2) | verbatim |
| `EquipmentCraftStatActions` (static) | 0 | - | Control, Craftsmanship, CP Bonus | ~180 | `StatRecord`, `DB` (2) | verbatim |
| `EquipmentStats` (stays, static) | - | the four action types | registration, equip and unequip handlers, the three refreshes with their constant, `ApplyItemStats`, `ApplyStatAmount`, `HandledPropertyTypes` | ~270 | `Stat`, `Item`, `PowerGloveEquipment`, `StatRecord`, `CreatureStat`, `Gui` (6) | rewrite (`RegisterStatActions` lambdas) |

`RegisterStatActions` keeps its name, phase and order and registers a lambda per action, for example
`(creature, item, ip, adding) => EquipmentVitalStatActions.ApplyHPBonus(creature, item, ip, adding)`, so `HandledPropertyTypes` and the boot line do
not change. The new static types still use the lookup, so the total of `StaticLookups.txt` is unchanged (the count moves from one file to five;
C8 converts them). If a later item splits the refresh handlers, the writer and the reader of the displaced-record constant stay in one type and the
three handlers keep their names.

## 4. Proof that nothing changes

### 4.1 View models (all but PlayerGuide)

1. **Groundwork (PR 0).** A recorder, copied from `AppearanceEditorBindingIdentityEngineTests` (that test is not touched), that is attached to the
   view model before `Bind`, so the opening order is recorded: every `PropertyChanged` name, order and JSON from `GuiPropertyConverter`. It also records
   every partial-view swap (`SwitchViews`, `RefreshLayoutPartials`, `Tabs.Select`), `WatchOnClient` registration, `Gui.PublishRefreshEvent`,
   `EventsPlugin.SignalEvent`, `ExportSingleCharacter`, feats and quick-bar slots, items created, destroyed and returned, key items and field notes,
   `CreatureStat.ApplyPlayerStat`, sounds played and targeting entry. Service calls are recorded by spy decorators over the real services
   (`IGuiService`, `ITargetingService`, `IKeyItemService`, `IIncubationFieldNoteService`, `ICreatureStatService`); engine state is dumped after every step.
2. **A real player record.** The scripts run on the `Player` entity that the engine fixture really saves, filled to production shape (skills at ranks,
   perks, SP and AP, a beast, disguises, held incubator items), not a stub.
3. **A fixed clock.** The time-dependent files read time through the existing clock seam: PR 0 adds `IncubatorViewModel.cs` and `PerksViewModel.cs` to
   `tools/Refactor/ClockSeamFiles.txt` and rewrites `DateTime.UtcNow` to `Clock.UtcNow` mechanically (`IncubatorViewModel.cs:129, 254, 888, 1017, 1038, 1158`;
   `PerksViewModel.cs:432, 962, 1360-1362, 1412`), with `FrozenClock` in the scripts. No text is masked. This is the one production edit in PR 0.
   **Capture environment (PR 0, built).** The six transcripts in `SWLOR.Game.Server.EngineTests/Definitions/Fixtures/ViewModelTranscript/` were captured on the full engine hak set: a throwaway checkout outside every worktree, `scripts/ci/fetch-engine-test-haks.sh`, the staging step of `prepare-engine-test-home.sh` and `SWLOR.CLI --hak`, host entry. The debugserver hak set lacks robe and forearm models and is not the comparison environment. The first line of each fixture names this, and later PRs compare on a home built the same way. The recorder must not create a SHA-1 inside the server (it hangs) and writes the JSON a call publishes from the value as it is before the call; the clock is fixed only while a scripted phase runs. The six transcripts are opt-in: they run only when `SWLOR_ENGINE_TEST_FILTER` names `ViewModelTranscript`, because a default sweep runs on a different hak set. The Perks script buys a perk that grants a quick bar feat (Dance1), shows the slot assignment, then refunds it and shows the slot cleared, which is the `PerkHotBarService` path of PR 6.
4. **Scripts per window.** Perks: open; select an unowned, an owned and a maxed perk; buy; refund; pages; four filters; search; category and sort; beast
   mode; a perk that adds a hot bar feat. Sheet: open on self, an NPC target (DM mode) and a beast; the four tabs; an attribute upgrade through the modal;
   equip, unequip and refresh events; dual wield; skill XP and stat adjustment. Incubator: new job; DNA and three enzymes; toggles; start; in progress;
   stage complete; continue; cancel; complete; reopen with held items. Disguise: open; new; save; edit; activate; deactivate; retire; unretire; portrait;
   sound-set paging and preview; empty state. Droid: open; install the five parts; remove each; error; construct; reset; new.
5. **Rewrites get their own proof.** `RefreshEquipmentStats` and the stat row orchestrator (sheet), the picker's setter calls (Disguise): a dump of every binding
   `RefreshEquipmentStats` writes and every row of the stats table (name, value, tooltip, order) for six fixtures (player with a one-hand weapon, dual wield,
   Twin Pistol, a beast, an NPC target, high-resource stats), captured on the base commit and compared after the PR. The catalog composition (PlayerGuide)
   and the `RegisterStatActions` lambdas (EquipmentStats) have their own checks below.
6. **PlayerGuide** is static data: a text dump of every topic (name, category, summary, rail summary, blocks, questions, related topics) in list order,
   captured before the move and compared byte for byte afterwards, plus `PlayerGuideContentTests`.
7. **Moved bodies.** A normalized-body comparison of old and new text per moved member, run as a script and attached to the PR, allowing only the receiver
   changes of the host and the `static` removal.
8. **Tests.** Build once with `-p:RunPostBuildEvent=Never -v q -clp:ErrorsOnly`, then filters for the classes named per type, `Phase5DebtTests`,
   `ConvertedServicesTests`, `BootHandlerClassification`, `GuiServiceReloadTests`, `PlayerMessageAuditTests`, `DomainMapTests`, `ClockSeamSourceGuardTests`;
   the engine classes with identical pass sets.
9. **Client evidence** (`InEngineVisualVerification.md`), before the first PR and after each PR for that window, one character (and one DM possession for the
   sheet): Perks (my perks, a selected perk, beast tab, after buy, after refund); sheet (the four tabs, upgrade prompt, an NPC target, dual wield); guide (first
   topic, search results, a topic with questions and related topics); incubator (new job, items placed, in progress, stage complete); disguise (list, edit form
   with a sound-set page, retired, empty); droid (empty, five parts, error).

### 4.2 EquipmentStats

1. **Gear-fed results transcript (PR 0),** a unit test on the fake world of `EquipmentStatActionTests` that writes the saved player record and the NPC skin
   properties after each step of a fixed gear set: a chest (Defense physical and Force, Resistance, Evasion, Control, Craftsmanship, CP Bonus), a helmet
   (HP, FP, FP regen, STM, STM regen, Stealth, Detection), boots (trap, disarm, poison, lockpicking), a rifle (Accuracy, Attack), a Combat Readiness item,
   a Power Glove pair, a companion pistol, DM and DM-possessed wearers, and an NPC skin that already holds values. Each is worn, removed and swapped through
   `ApplyStats` and `RemoveStats`, followed by the three refresh handlers and `StatRecord.RefreshPlayerEquipmentStats`. The registered types in order are in it.
2. **Coverage proves reach.** The transcript test must cover every line of every stat action, both sides of each branch, measured by the coverage run on
   the five new files and on `EquipmentStats` (no uncovered line without a listed reason). It must therefore include: Force Attack, the weapon early return in
   `ApplyAccuracy` and the `CreatureItem` case, all four craft subtypes and the unknown-subtype throw in Control, Craftsmanship and CP Bonus, a Defense
   subtype that is not a defense type, an invalid Resistance type, the NPC removal rules (Defense value `<= 0`, Resistance value `== 0`), the HP NPC branch,
   `ApplyStatAmount` (including zero and negative amounts and the weapon skip), and both the add and remove side of every player and NPC branch.
3. The existing classes run before and after with identical results, with the Roslyn-based source tests re-pointed to the files that now hold their text.
4. **Client evidence:** one character in a fixed outfit; the sheet stats, resistances and crafting tabs and the status bars before and after each PR, and
   after unequipping one piece.
5. `git diff --stat` shows no change to `Stat`, `StatRecord`, `EquipmentStatTotals`, `LootStatRevisionMigration`, `Module/`, 2DA or `SWLOR_Haks`.

## 5. PR plan, baselines and shared files

Each PR starts from up-to-date `origin/feature/maintainability-refactor`, moves at most about 1,000 lines, gets a Codex review of its head, and runs the
gate with coverage. "Moved" counts the lines taken out of the stay-type.

| PR | Content | Moved | Stay-type after |
|---|---|---:|---|
| 0 | Groundwork: recorder, the transcripts, guide dump, gear transcript, clock seam for Incubator and Perks, the Bible tool pattern | 0 | unchanged |
| 1 | `EquipmentStats`: writer, vitals and craft types; `RegisterStatActions` lambdas for them; `LegacySource` re-cited; `DomainMap.json` entry | ~600 | ~560 lines |
| 2 | `EquipmentStats`: rating and field types | ~530 | ~270 lines, 6 services |
| 3 | PlayerGuide: seven category types, three data types, catalog; edits to the two existing topic types | ~1,085 | ~290 lines |
| 4 | Incubator: host, session, calculator and three parts | ~850 | ~410 lines |
| 5 | Disguise (picker) and DroidAssembly (host, session, two parts) | ~180 + ~670 | ~890 and ~420 lines |
| 6 | Perks: host and four parts (after P5-16g merges); Bible tool path | ~990 | ~620 lines |
| 7 | CharacterSheet: host, format statics, timing, damage estimate, ratings, attributes tab, entry records | ~455 | ~1,100 lines |
| 8 | CharacterSheet: four row builders and the table tabs | ~270 | ~880 lines, 8 services |

**Baseline and record changes** (each PR regenerates the Phase 5 snapshots with `SWLOR_UPDATE_SNAPSHOTS=1` and reviews the diff):
- `LargeTypes.txt`: `EquipmentStats` goes in PR 2; the view models in PRs 3, 4, 5 (two), 6 and 8. `ServicesPerType.txt`: the sheet line goes in PR 8.
- `StaticLookups.txt`: counts drop on every moved line and the new static types get lines (`EquipmentStats` 120 to about 25, then four new files). View models
  listed in `ContentInjection.json` drop to the base class lookups only.
- `StaticState.txt`: the `Topics` line moves from `PlayerGuideViewModel` to `PlayerGuideTopicCatalog` (kept); the sheet keeps its 6.
- `UpwardEdges.txt`: the two sheet rows become three (section 3.3). `LongMethods.txt`: none. `DebtReport.md` is regenerated.
- `PlayerMessageAudit.json`: 36 rows are re-keyed to the new files and lines (Incubator 11, Disguise 10, Perks 8, sheet 6, Droid 1).
- `DomainMap.json`: `EquipmentStatActions`. `ContentInjection.json` and `ContentRegistrations.cs`: the new parts and the six view models.
- `ClockSeamFiles.txt`: the two files (PR 0). `LegacySource.cs`: the `GearStats` reference (PR 1). `BootHandlerClassification.txt`, `HandlerRenames.txt`,
  `ScriptHandlerOrder.txt`: no change expected, since no handler is renamed or moved; the guard tests confirm it.
- Source-reading tests in section 3 are re-pointed in the PR that moves their text.

**Shared files edited by other branches:** `ContentInjection.json`, `ContentRegistrations.cs` (generated: rerun the tool, never merge by hand),
`DomainMap.json`, `PlayerMessageAudit.json`, `ClockSeamFiles.txt` and the Phase 5 baselines (`LargeTypes`, `ServicesPerType`, `StaticLookups`, `StaticState`,
`UpwardEdges`, `LongMethods`, `DebtReport.md`). After merging the base, regenerate them rather than merging text.

**Master edits and the sync.** Since 2026-10-01 master changed `PlayerGuideViewModel.cs` 21 times, `CharacterSheetViewModel.cs` 17, `EquipmentStats.cs` 16,
`IncubatorViewModel.cs` 6, `DroidAssemblyViewModel.cs` 6, `PerksViewModel.cs` 5 and `DisguiseViewModel.cs` 0; the freeze ends with P5-5h.
- **Port table.** Section 3's "Moves" column is the member-to-part table. A master edit to a member that has moved is ported into the part that
  now holds it (for PlayerGuide, into the category type named for the topic's category in the table); the daily sync's conflict on the old file is resolved
  by hand that way, with the receiver rule of section 2.
- **Fixtures after a sync.** When a sync changes text a fixture covers, a transcript or dump is not edited by hand. Keep one temporary branch, `p5-16h-presplit`, that holds the pre-split base plus every master change already synced to the old files, in order. Apply master's new commit to the old file on that branch (never on a branch made from the pre-split base alone, which would drop the changes of the earlier syncs and show their reversal in the diff), recapture with the same script, and compare the new fixture with the fixture in the PR: the diff must be explained by the new commit alone. Commit the regenerated fixture in the PR with that explanation, rerun the PR's comparison, and leave the branch at the new commit for the next sync.

## 6. Conflicts and order

Rule 4 (Phase 5): no PR of this item is open on a file while a P5-5, P5-7, P5-10, P5-16e or P5-16g PR is open on it.

| With | Files in common | What to do |
|---|---|---|
| P5-16g | `PerksViewModel.cs` (it splits `OnClickBuyUpgrade`) | PR 6 opens after P5-16g merges. |
| P5-5c to P5-5h (master frozen) | `CharacterSheetViewModel.cs` has one call, `StatusEffect.TryGetLimitedAttackDelayReduction`, that P5-5g's part 6 moves; `PlayerMessageAudit.json` (P5-5c edits it) | PRs 7 and 8 do not open while P5-5g is open. The audit file is regenerated after the other merges. |
| P5-7 (Combat services, after P5-5 in the serial chain) | `CharacterSheetViewModel.cs` reaches `CombatAbilityBonus`, `CombatAbilityHit`, `CombatAttackTiming`, `CombatDamageModifiers`, `CombatDamageTaken`, `CombatResources` | PRs 7 and 8 must be merged before P5-7 starts, so it edits the small new files. |
| P5-10 (`PerkService`, `SkillService`, `PlayerNameService`, `BeastMasteryService`, `DroidService`) | `Perk` 20 calls in Perks, `BeastMastery` in Incubator and the sheet, `Droid` in Droid | PRs 4 to 8 merge before P5-10 starts; it then rewrites receivers in the new parts, which name each service once. |
| P5-9 (`ItemService`, last) | `Item` in Incubator, Droid, the sheet, `EquipmentStats` | P5-16h goes first. |
| P5-16e | none of its files; shared `DomainMap.json`, `ContentInjection.json`, baselines | regenerate after the other merges. The host pattern is its own; `Feature/AppearanceEditing` is not touched. |
| P5-14 C2, C3, C4, C8 | `EquipmentStats` statics, `LootStatRevisionMigration`, six view models | P5-14 starts after P5-16 by plan. C8 and C2 convert the statics; C3 and C4 find six view models already injected. |

Test files shared between PRs: `PlayerNameRecognitionTests` (Disguise, sheet), `PlayerSkillPointAccountingTests` (Perks, sheet), `WeaponDeflectionTests`
(guide, sheet), `StationReturnInventorySpaceEngineTests` (Droid, Incubator). Two PRs that share one are never open together.

**Order**, at most three lanes (the coordinator counts the P5-5, P5-16e and P5-16g agents too):

1. PR 0.
2. Lane A: PR 1, PR 2, then PR 3. Lane B: PR 4, then PR 5. Lane C: PR 6, once P5-16g has merged.
3. PRs 7 and 8 last: after PRs 3, 5 and 6 and after P5-5g has merged, and both merged before P5-7 starts.

## 7. Open questions

None needs the owner now. Three choices are recorded so they are not lost:
1. **`EquipmentStatActions` in `DomainMap.json`** goes under Character (the rules write Character records); `DomainMapTests` and the upward-edge guard confirm it.
   Recommended: Character; Gameplay only if the guard flags an edge.
2. **`CharacterSheetAccuracyRows` sits at 10 services after P5-12.** Recommended: accept; the fallback in section 3.3 splits the critical rows if the build
   or a later item adds an eleventh.
3. **Disguise keeps about 110 lines of room.** Recommended: accept; if master grows it past 1,000 before PR 5 merges, the next responsibility to move is the
   list and slot capacity code (`LoadList`, `RefreshSlotCapacity`), not a numbered piece.

## 8. PR 1 as built (EquipmentStats: writer, vital and craft actions)

- New static types in `Feature/EquipmentStatActions/` (namespace `SWLOR.Game.Server.Feature.EquipmentStatActions`), bodies moved verbatim with only the
  receiver of `ReapplyNPCStat` changed to `NpcSkinStatWriter`: `NpcSkinStatWriter` (47 lines, no lookups), `EquipmentVitalStatActions` (249 lines: HP, FP, FP regen,
  STM, STM regen, Combat Readiness; `StatRecord`, `DB`, `CreatureStat`) and `EquipmentCraftStatActions` (174 lines: Control, Craftsmanship, CP Bonus; `StatRecord`, `DB`).
  The action methods are `public static` so the registration lambdas can reach them. `EquipmentStats` went from 1,131 to 714 lines (417 lines moved, not the ~600 the
  table estimated: the rating and field actions stay for PR 2).
- `RegisterStatActions` registers a hand-written lambda for the nine moved actions, in the same order; `HandledPropertyTypes`, the `Rerun` boot line, the three
  refresh handler names and the writer and reader of `EQUIPMENT_DISPLACED_RECORD_STATS` (both in `EquipmentStats`) are unchanged. The still-private rating and field
  actions call `NpcSkinStatWriter.ReapplyNPCStat`.
- `DomainMap.json` lists `EquipmentStatActions` under Gameplay (the coordinator's decision overrides the Character recommendation of open question 1); no upward edge appears.
- `LegacySource.GearStats` is re-cited to `EquipmentCraftStatActions.cs:25-172` (the old 930-1126 range was stale); `GearItemReader` and `CraftingReport` follow.
- `CombatReadinessEquipmentTests` and `MaxHitPointsReconciliationTests` read the moved methods from `EquipmentVitalStatActions.cs` as well as `EquipmentStats.cs`.
- Baselines: `LargeTypes.txt` drops `EquipmentStats`; `StaticLookups.txt` goes from 120 on one file to 87 + 22 + 13 (two more `using static` lines, no new use in code);
  the two edge records gain the `Feature/EquipmentStatActions` rows and lower the `Feature.EquipmentStats` counts. `PlayerMessageAudit.json` is unchanged (no message moved).

## 9. PR 2 as built (EquipmentStats: rating and field actions)

- New static types in `Feature/EquipmentStatActions/`, bodies moved verbatim (only `private` became `public`): `EquipmentRatingStatActions` (314 lines: Attack, Accuracy, Force Attack,
  Defense, Resistance, Evasion; 30 lookups) and `EquipmentFieldStatActions` (235 lines: Stealth, Detection, trap bonus, disarm, poison, lockpicking; 25 lookups). No new lookups
  in code: the 87 `EquipmentStats` lookups of PR 1 became 34 (stay-type) + 30 + 25, with two more `using static` lines.
- `EquipmentStats` went from 714 to 203 lines (511 moved). The map estimated ~270 and ~530: the stay-type is smaller because `ApplyItemStats`, `ApplyStatAmount` and the three refresh
  handlers are shorter than estimated. All 21 registrations are now hand-written lambdas in the original order; `HandledPropertyTypes`, the `Rerun` boot line, the refresh handler
  names and the writer and reader of `EQUIPMENT_DISPLACED_RECORD_STATS` are unchanged.
- Proof: `EquipmentStatsWornGearTranscriptTests` passes unchanged against its committed fixture (no recapture). `AccuracyItemPropertyTests` now expects the lambda registration text.
  No `LegacySource` or `PlayerMessageAudit.json` change (no craft code or message moved).
- Baselines: `StaticLookups.txt` as above; the two edge records gain `Feature/EquipmentStatActions -> Domain/Combat` and `-> Domain/Items` and lose
  `Feature.EquipmentStats -> Platform/Entity`, with the `Gameplay -> Foundation` header 60 to 59; other member-count churn from regeneration was reverted.

## 10. PR 3 as built (PlayerGuide: topics by category)

- New internal types in `Feature/GuiDefinition/ViewModel/`, topic bodies moved verbatim (each topic is one `Create<Name>()` method, the only edits are a four-space dedent and `new(` after `return`): `PlayerGuideCoreTopics` (188 lines, 5 topics), `PlayerGuideSocialTopics` (101, 2), `PlayerGuideProgressionTopics` (105, 3), `PlayerGuideCombatTopics` (206, 6), `PlayerGuideCharacterTopics` (72, 2), `PlayerGuideActivitiesTopics` (397, 11) and `PlayerGuideInterfaceTopics` (47, 1); `PlayerGuideTopic` (33), `ArticleBlock` (14) and `QuestionAnswer` (14) in their own files; `PlayerGuideTopicCatalog` (53, static) lists the 30 topic methods in today's order, then `StatusEffectGuideTopics` and `CombatStyleGuideTopics`. The longest method is 42 lines; no `typeSize` exemption is needed.
- `PlayerGuideViewModel` went from 1,367 to 283 lines (1,084 moved) and reads `PlayerGuideTopicCatalog.Topics`. `CombatStyleGuideTopics.cs` and `StatusEffectGuideTopics.cs` now name the top-level data types.
- Static state: `Topics` stays a static `IReadOnlyList` on `PlayerGuideTopicCatalog` (its `StaticState.txt` line is kept for classification, not made deeply immutable).
- Proof: `PlayerGuideTopicDumpTests` passes unchanged against its fixture; `PlayerGuideSource` discovers every new file by name (no change), and `PlayerFacingNameBroadcastTests`, `RoleplayXPOOCPenaltyTests` and `WeaponDeflectionTests` pass. `PlayerGuideContentTests` and `ViewModelBindingTranscriptEngineTests` read `Topics` through the catalog type by reflection (the only test edits). All six `ViewModelTranscript` transcripts, PlayerGuide included, are identical on the full engine hak set.
- Baselines: `LargeTypes.txt` drops `PlayerGuideViewModel`; `StaticState.txt` renames its line to `PlayerGuideTopicCatalog`; `DebtReport.md` regenerated. `StaticLookups.txt`, both edge records and `PlayerMessageAudit.json` are unchanged (no edge appeared or disappeared, no message moved).

## 11. PR 4 as built (Incubator: host, session, calculator and three parts)

- New types in `Feature/GuiDefinition/IncubatorWindow/` (namespace `SWLOR.Game.Server.Feature.GuiDefinition.IncubatorWindow`), bodies moved with only their receiver changed (`Player` to `host.Player`, a field to `host.Session.X`, a binding to `host.X`, a moved call to its part): `IIncubatorHost` (72 lines, 46 binding properties, `Session`, `Player`, `ShowModal`, `SwitchViews`, `RefreshAllStats`), `IncubatorSession` (48 lines, 0 services; the 12 state fields plus `Blank`), `IncubatorJobCalculator` (91 lines, 2 services: `DB`, `Perk`; `GetJob`, `IsJobComplete`, `CalculateIncubationSeconds`, `GetErraticGeniusBonus`, `GetProjectedMutationChance`, `GetStageTotal`, `NumberOfStages` and the stage constants), `IncubatorPanelService` (348 lines, 4: `BeastMastery`, `IncubationFieldNote`, `KeyItem`, the calculator), `IncubatorItemService` (371 lines, 3: `Item`, `Targeting`, the calculator) and `IncubatorJobService` (206 lines, 5: `BeastMastery`, `DB`, `Gui`, `Perk`, the calculator). The parts are content singletons (`ContentInjection.json`, `ContentRegistrations.cs`); the content tool's `-Check` is clean.
- `IncubatorViewModel` went from 1,190 to 368 lines and from 8 services (`Item` 17, `BeastMastery` 10, `DB` 5, `Perk` 4, `Gui` 4, `Targeting` 2, `IncubationFieldNote` 2, `KeyItem` 2 uses) to 6: `DB` (the mutation toggle saves the job), `Gui` and the four parts. It keeps the bindings, `SwitchViews`, `RefreshAllStats`, `RefreshButtons`, `LoadPlayerStats`, `Initialize`, the toggles, the click forwarders, the refresh events and the explicit host. The enzyme forwarders keep their setter lambdas (they write the session and the bindings, as before) and call `IncubatorItemService.OnClickEnzyme`. The longest moved method is 63 lines.
- Two departures from the part table. `ClearStats` and `ResetPendingEnzymes` live on `IncubatorSession` (they touch only its state), not on the item service: `StartJob` calls `ResetPendingEnzymes`, and the job service and item service are both layer 1 and may not use each other. `RefreshButtons` and `LoadPlayerStats` stay on the view model (the table did not list them).
- Proof: all six `ViewModelTranscript` tests pass on the full engine hak set and the Incubator transcript is identical to its fixture (7,708 lines, no fixture change); `IncubatorCompleteJobEngineTests` (2) and the `ItemClaimSpace` tests (13, which include `StationReturnInventorySpaceEngineTests`) pass. The recorder changed in two places so it sees the singletons' service calls: `SpyProvider` builds its own copy of each concrete content singleton and its own `ContentFactory` over itself, and `Open` builds the view model with that factory (the window service's own factory holds the real container). Without it the targeting and key item calls of the parts bypassed the spies (the transcript differed at line 577).
- Re-pointed tests: `IncubationPerkRegressionTests` read `CalculateIncubationSeconds`, `GetErraticGeniusBonus`, `GetProjectedMutationChance` (calculator), `StartJob` and `ValidateCreateJob` (job service) with the new signatures and receivers; `BeastMutationRecipeTests` reads `NumberOfStages` from the calculator (now public); `StationReturnInventorySpaceEngineTests` and the transcript test read the session through the host and call `RestoreHeldItems` on the item service.
- `PlayerMessageAudit.json`: the 11 Incubator rows are re-keyed (File, Line, Member); the `Call` text of 9 of them also changes because it names the receiver (`Player` to `host.Player`). Baselines: `LargeTypes.txt` and `StaticLookups.txt` drop `IncubatorViewModel` (no lookup is left in any part: the content tool converted them all); `DebtReport.md` regenerated; both edge records are unchanged (no edge appeared or disappeared; the regenerated member counts were reverted).
- Client evidence (live stack, one character, a temporary uncommitted driver that opens the window and seeds the held items or a stored job; base and branch on the same module, haks and character): the new-job, items-placed, in-progress, stage-complete and job-complete views are byte-identical PNGs on base and branch, except the in-progress view, where only the live countdown seconds differ (56S on base, 54S on branch).
