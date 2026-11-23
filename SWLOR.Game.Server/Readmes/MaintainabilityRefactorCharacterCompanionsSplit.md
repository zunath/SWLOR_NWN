# Maintainability Refactor: Character and Companions Split (P5-10 part map)

The plan for splitting the six Character, Abilities and Companions services over the Phase 5 limits:

| Service | File | Lines | Services used | Methods over 150 | `Lazy<>` | Event handler lines |
|---|---|---:|---:|---:|---:|---:|
| `PerkService` | `Domain/Character/` | 1,486 | 7 | 0 | 2 | 3 |
| `MimicryService` | `Domain/Abilities/` | 1,307 | 11 | 0 | 2 | 7 |
| `BeastMasteryService` | `Domain/Companions/` | 1,240 | 14 | 0 | 0 | 21 |
| `DroidService` | `Domain/Companions/` | 1,194 | 14 | 1 (`SpawnDroid`, 272) | 0 | 24 |
| `SkillService` | `Domain/Character/` | 1,182 | 6 | 1 (`GiveSkillXP`, 239) | 1 | 6 |
| `PlayerNameService` | `Domain/Character/` | 1,039 | 5 | 0 | 2 | 4 |

All six are over the line limit (target 2). `MimicryService` (11), `BeastMasteryService` (14) and `DroidService` (14) are also over
the service limit, and two methods are over the method limit (target 3). The baseline lines are in `LargeTypes.txt` (six
lines), `ServicesPerType.txt` (three), `LongMethods.txt` (two) and `DeferredResolution.txt` (seven `Lazy<>` parameters in six
lines). Status lives in [MaintainabilityRefactorStatus.md](MaintainabilityRefactorStatus.md) (row P5-10). Targets and rules:
[MaintainabilityRefactorPhase5.md](MaintainabilityRefactorPhase5.md); the order and the inversion rows:
[MaintainabilityRefactorDomainOrder.md](MaintainabilityRefactorDomainOrder.md) (Appendix C rows that name P5-10: Character ->
Abilities, Character -> Social, Character -> Companions, Foundation -> Character). The models are the
[Creatures split](MaintainabilityRefactorCreaturesSplit.md) and the
[Combat services split](MaintainabilityRefactorCombatServicesSplit.md). `DroidService` goes last of all the Phase 5 splits (owner
decision 5), so its pull requests are at the end of the order. This file goes away at cutover.

Everything here is measured at `34c3373a0` (branch `claude/mr-p5-10-map`), 2026-11-23, with a syntax scan of the six files
(member ranges with their doc comments and attributes, brace matching that skips strings and comments), a regex search of the
server, the tests and the engine tests for call sites, and a scan of the constructor graph (plain parameters only, the
`ServiceRegistrations` map and its aliases, as `LazyDependencyCycleTests` reads it). Projected file sizes add about one blank line
per member, 22 lines of header, and two lines per injected service plus 8 for the fields and constructor. Call counts are a
lower bound: a type that reaches a service through a local alias is missed. Master was fetched (`db37a6ec43`). Nothing was
built or run.

## Decisions

Proposed for the coordinator's review; change only on a finding in review. The open ones are at the end.

- **D1. One stay-part per service, layered by who calls what.** The stay-part keeps the original class and interface name and the
  members most callers use, so the fewest holders change. The other parts are new services behind new interfaces, each one
  responsibility. Rejected: chunks by line count. Layers, bottom to top, are in "Parts".
- **D2. No part is over a limit and every method is at or under 150 lines.** Every part is at or under 10 services and 1,000
  lines. The two long methods are cut (D8). No exemption is asked for. The largest part is `MimicryLoadoutService` at about 715
  lines; the widest are `MimicryWitnessService`, `BeastIncubationService` (8 services each).
- **D3. Moved code stays verbatim** except for how it reaches its state and dependencies. The text changes are the ones named
  per part: promoted private members, new read members, seams that become one-line calls to another part, the three cuts of the
  two long methods, and the receivers of calls into other parts. The proof is a Roslyn token comparison per PR (Tests).
- **D4. State that two parts share is resolved three ways:** the owner exposes a read member (`PerkService.EnumerateAllPerks`),
  a member that only one other part calls moves with that caller (`CountDisplayNameMatches`, `ApplyStats`), or a boot handler in
  the part that owns the cache rebuilds it (D5). No small core service is needed.
- **D5. Two Perk caches are rebuilt by their owner's own boot handler.** `CachePerks` calls `CacheStatBonusPerk` once per perk and
  sets `_perkFeatCacheLoaded = false`. Both are state the new parts own, and a call up from the stay-part would be a cycle.
  `PerkStatBonusService.CachePerkStatBonuses` and `PerkGrantedFeatService.ResetPerkFeatCache` become `Rerun` boot handlers on
  `OnModuleCacheBefore`, listed right after `PerkService.CacheData` in `ScriptHandlerOrder.txt` (line 41 today). Mimicry gets the
  same for the witness sweep (`CacheData` schedules it today; D6 in "State, handlers and reload").
- **D6. `PerkStatBonusService` uses `StatRules.AggregateStatAdjustment` directly.** `PerkService.AggregateStatAdjustment` is a
  one-line seam over `IStatService.AggregateStatAdjustment`, which is `StatRules.AggregateStatAdjustment`
  (`StatService.cs` line 100). The part calls the static rule in the seam's body (four uses, one line changed). This is what
  lets the part sit under `StatService`, which reads it, without a `Lazy<IStatService>`.
- **D7. Upward edges that P5-10 owns are inverted where the part that needs them is built,** with Character-owned (or
  Foundation-owned) one-member ports implemented by the lower-level interface extending the port, as `IBeastCorpseInfo` and
  `IPlayerListRefresh` do. They are in "The upward references". No `Lazy<>`, `Func<>` or late lookup is added anywhere.
- **D8. The two long methods are cut into named steps in the same pull request that moves them.** `GiveSkillXP` (239) becomes a
  main method of about 100 lines plus `ApplyXPBonuses`, `RepayXPDebt`, `ClearXPAtRankCeiling` and `AwardRanks`, with a private
  `RankUpOutcome` carrying the locals across (the rank-up flag, XP spent on ranks, the modified and decayed skill lists, the
  running rank total). `SpawnDroid` (272) becomes a main method of about 100 lines in `DroidSpawnService` and three calls into
  `DroidRestoreService` (inventory restore, appearance restore, the closing steps). The order of every engine call, database
  write and random draw is unchanged; the ordered transcripts of P5-10b pin it.
- **D9. `GetWeaponSkill` stays on `SkillService`** (19 non-test call sites, 5 of them native). It needs the twin-pistol pair test, so
  `SkillService` takes Combat-owned `ITwinPistolEquipmentService` today. A Character-owned `ITwinPistolPairQuery` port (one member,
  `IsActivePair`) replaces it. `TwinPistolEquipmentService` and `ItemService` keep their `Lazy<ISkillService>` (the path
  `SkillService -> TwinPistolEquipmentService` stays); open question 3.
- **D10. No master freeze is needed** (open question 1): none of the six master files changed since the last sync
  (`a184c9ef2`), and November has one commit each on three of them (Skill, PlayerName, Beast), three on Droid and none on Perk and Mimicry.

## What the code is

Sizes are member lines (a member starts at its doc comment; blank lines and `using` lines are not in a member's count), summed per job from
the member scan, so a job's size is the sum of its members in "Members by part".

### `PerkService` (1,486 lines)

The definition caches, a creature's perk levels, the stat bonuses and granted feats that follow, and refunds.

| Job | Size |
|---|---:|
| Catalog and levels: tables, `CachePerks`, trigger tables, category and detail reads, `GetPerkLevel` and the player, droid and beast level reads, `UnlockPerkForPlayer` | 597 |
| Stat bonuses: the two group types and tables, `CacheStatBonusPerk`, the three reads and helpers | 252 |
| Force affinity | 96 |
| Granted feats: the feat caches, `RestorePlayerFeats`, `SyncGrantedFeats`, the active-ability reads | 385 |
| Refunds: refund cleanup, source-owned refunds, the skill decay handler | 123 |

`CachePerks` (119 lines) is the longest method. The seams (7 members at the end of the file) call `GameAssemblies`, the stat service, the
companion lookup, the ability catalog and `ICombatService`.

### `SkillService` (1,182 lines)

| Job | Size |
|---|---:|
| Catalog: tables, `CacheData`, the category and skill reads, the item-to-skill mapping, `GetWeaponSkill`, the XP chart reads, `GetCreatureSkillRank`, `AddMissingSkills`, the point caps | 579 |
| Rank rules: caps, limits, decay candidates, `IsBlockedBySkillCap`, `GetMaxDistributableXP` | 203 |
| XP award: `GiveSkillXP`, `ApplyAbilityPoint`, limit warnings, the message queue, the XP formulas | 347 |

`GiveSkillXP` is 252 lines with its doc comment (239 by the guard's count). `CacheData` (85) and `LoadItemToSkillMapping` (88) are
the next longest.

### `PlayerNameService` (1,039 lines)

| Job | Size |
|---|---:|
| Display names and known-name reads: the caches, `GetDisplayName` family, `ResolveDisplayName`, descriptors, `TryGetKnownName`, `SanitizeKnownName`, the identity seams | 391 |
| Known-name writes: validation, `SetKnownName`, `ForgetKnownName`, `DeleteKnownNameReferences`, `SearchKnownPlayerIdsByName` | 182 |
| Engine name overrides, tells and AFK: `ApplyNameOverride*`, refreshes, the tell handler, chat name overrides, AFK state and check | 430 |

No method is over 150 (the longest is `PreventAmbiguousTellTargets`, 46). The class also implements `IReloadStateAdopter`.

### `MimicryService` (1,307 lines)

| Job | Size |
|---|---:|
| Technique catalog and trait reads: tables, `CacheData`, `GetResistanceBonus`, icon and description reads, the description carry and adopt | 193 |
| Witness and learn: nearby players, analysis XP, witness tracker, the three handlers, `TryLearnTechniques`, `GrantAllTechniques` | 392 |
| Loadout: slots, equip and unequip, budget enforcement, hotbar helpers, saved loadouts, login restore | 667 |

No method is over 100 (the longest is `TryLearnTechniques`, 69). `MimicryService` has the two largest field-state sets of the six:
two `Carried` tables (`_witnesses`, `_analysisObservers`) and one `Adopted` table (`_techniqueDescriptions`).

### `BeastMasteryService` (1,240 lines)

| Job | Size |
|---|---:|
| Catalog and XP: beast, role and food tables, the two XP tables (135 lines), IDs, `GiveBeastXP`, RP XP, `AddXP`, `ApplyStats`, `CombatPointXPDistributed` | 598 |
| Spawn: `SpawnBeast`, `GetCompanionSlotValidationError`, the feat seam | 109 |
| Events: the twelve beast event handlers, acquire and associate handlers, the stables menu, the companion-control seams | 243 |
| Incubation: incubator use, mutation, day overrides, eggs, vendor bonus, DNA extraction, property removal | 218 |

No method is over 100 (the longest is `SpawnBeast`, 82).

### `DroidService` (1,194 lines)

| Job | Size |
|---|---:|
| Controller data and details: constants, personalities, `IsDroid`, `GetControllerItem`, the constructed-droid load and save, the two property-detail loaders, `UpdateEquippedItemSnapshot`, instruction validation | 319 |
| Spawn and restore: `SpawnDroid` and `RestoreDroidItem` | 305 |
| Inventory sync: acquire, lose, equip, unequip handlers, snapshot, returned items, generated ammunition | 270 |
| Events and terminal: the twelve droid event handlers (one of them death), the assembly terminal, the appearance edit | 145 |
| Despawn: `DespawnDroid`, `ClearTemporaryData`, `RemoveAssociate`, `OnPlayerExit`, `DroidOnDeath` | 85 |

`SpawnDroid` is 277 lines with its doc comment (272 by the guard), `LoadDroidPartItemPropertyDetails` 130. The class has a protected
parameterless constructor (979-992) and a protected virtual `ReadGeneratedAmmunitionFlags` for the tests; both go with the part
that calls them (inventory sync).

## Dependencies

All are constructor parameters, as the guard counts them (the six files name `GameServices` only in a doc comment).

| Service | Services (constructor parameters) |
|---|---|
| `PerkService` (7, 2 `Lazy<>`) | `IDBService`, `IQuickCastService`, `ICompanionLookup`, `IStatusEffectService`, `IAbilityCatalog`; `Lazy<IStatService>`, `Lazy<ICombatService>` |
| `SkillService` (6, 1) | `IDBService`, `IGuiRefreshPublisher`, `IPlayerFeedbackService`, `ITwinPistolEquipmentService`, `ICompanionLookup`; `Lazy<IStatService>` |
| `PlayerNameService` (5, 2) | `IDBService`, `IPlayerListService`, `IChatPortraitTellsService`; `Lazy<IDisguiseService>`, `Lazy<IPlayerDescriptorService>` |
| `MimicryService` (11, 2) | `IDBService`, `IQuickCastService`, `IDMPresenceService`, `IPlayerFeedbackService`, `IGuiService`, `IAbilityCatalog`, `IPerkService`, `ISkillService`, `IStatusEffectService`; `Lazy<IStatService>`, `Lazy<ICombatPointService>` |
| `BeastMasteryService` (14) | `IDBService`, `IIncubationFieldNoteService`, `IPropertyService`, `IAIService`, `ICombatRecorderService`, `ICompanionControlService`, `IGuiService`, `IItemService`, `IPerkService`, `IResistanceService`, `IStatService`, `ICreatureStatService`, `IStatusEffectApplicationService`, `ICombatDamageCatalogService` |
| `DroidService` (14) | `IAIService`, `ICombatRecorderService`, `ICompanionControlService`, `IDroidEquipmentDiagnosticsService`, `IGuiService`, `IItemService`, `IPerkService`, `IPowerGloveEquipmentService`, `IRaceService`, `IRecastService`, `IResistanceService`, `ICreatureStatService`, `IStatusEffectApplicationService`, `ITwinPistolEquipmentService` |

The scan of plain paths from each `Lazy<>` target back to its owner (the container's view, `StatService` is the stay-part of P5-4):

- `Perk -> Lazy<IStatService>`: direct (`StatService -> PerkService`). `Perk -> Lazy<ICombatService>`: through `CombatResourcesService`,
  `CombatQueriesService`, `StatService`. Both go in the split (D6; the refund part takes a port, below).
- `Skill -> Lazy<IStatService>`: direct (`StatService -> SkillService`, for `GetSkillDetails`). It goes when the XP part, which `StatService`
  does not reach, takes it.
- `PlayerName -> Lazy<IDisguiseService>` and `Lazy<IPlayerDescriptorService>`: `DisguiseService` and `PlayerDescriptorService` both call
  the stay-part (`SanitizeKnownName`, the known-name reads). They stay, on the stay-part only.
- `Mimicry -> Lazy<IStatService>`: `StatService -> ... -> ResistanceService -> MimicryService` (`GetResistanceBonus` needs the
  creature's potency stat). It stays on the stay-part. `Lazy<ICombatPointService>` goes: only the stay-part and the loadout part are
  reached by other services (`ResistanceService`, `AbilityActivationService`), and the witness part is reached by content only.

## Parts

New services are `<Name>Service` behind `I<Name>Service`, registered in `ServiceRegistrations`, given a `GameServices` property of
the same name, and listed in `ConvertedServices.json` under the stay-part's `extras`. Domains: Character (Perk, Skill, PlayerName),
Abilities (Mimicry), Companions (Beast, Droid). Handler counts are lines in `ScriptHandlerOrder.txt`.

| # | Part | Lines | Services | Runs / state |
|---|---|---:|---:|---|
| PK-1 | `PerkService` (stay) | 640 | 2 | **1 handler** (`CacheData`); the catalog tables |
| PK-2 | `PerkStatBonusService` | 290 | 2 | **1 new boot handler**; read on every stat recalculation |
| PK-3 | `PerkForceAffinityService` | 140 | 2 | per ability hit chance and magnitude |
| PK-4 | `PerkGrantedFeatService` | 430 | 5 | **1 handler** (`CachePerkFeatData`) and **1 new boot handler**; per login and perk change |
| PK-5 | `PerkRefundService` | 170 | 7 | **1 handler** (`RemovePerkLevelOnSkillDecay`) |
| SK-1 | `SkillService` (stay) | 620 | 3 | **4 handlers**; the catalog tables, the item map |
| SK-2 | `SkillRankService` | 245 | 2 | none; per XP award and window open |
| SK-3 | `SkillXPService` | 420 | 6 | **2 handlers**; `_limitWarningsSent`, `_xpMessages` (both `Runtime`); per XP award |
| PN-1 | `PlayerNameService` (stay) | 435 | 3 (2 `Lazy<>`) | **1 handler**; the known-name and descriptor caches |
| PN-2 | `PlayerKnownNameService` | 230 | 4 | none; per `/name` |
| PN-3 | `PlayerNameOverrideService` | 480 | 3 | **3 handlers** and `IReloadStateAdopter`; `AfkPlayers` (`Carried`) |
| MI-1 | `MimicryService` (stay) | 235 | 4 (1 `Lazy<>`) | **1 handler** (`CacheData`, adopted and carried description table) |
| MI-2 | `MimicryWitnessService` | 445 | 8 | **3 handlers** (4 lines) and **1 new boot handler**; `_witnesses`, `_analysisObservers` (`Carried`) |
| MI-3 | `MimicryLoadoutService` | 715 | 7 | **2 handlers**; per equip and login |
| BM-1 | `BeastMasteryService` (stay) | 650 | 7 | **2 handlers** (`CacheData`, `CombatPointXPDistributed`) |
| BM-2 | `BeastSpawnService` | 155 | 5 | none; per beast call |
| BM-3 | `BeastEventService` | 295 | 7 | **15 methods, 16 lines**; per beast event |
| BM-4 | `BeastIncubationService` | 270 | 8 | **3 handlers**; `_incubationDayOverrides` (`Runtime`) |
| DR-1 | `DroidService` (stay) | 360 | 1 | **1 handler** (`CacheData`) |
| DR-2 | `DroidSpawnService` | 190 | 7 | per summon |
| DR-3 | `DroidInventoryService` | 310 | 2 | **5 methods, 6 lines**; per item move |
| DR-4 | `DroidEventService` | 195 | 6 | **13 handlers**; per droid event |
| DR-5 | `DroidDespawnService` | 130 | 5 | **3 methods, 4 lines**; per despawn and death |
| DR-6 | `DroidRestoreService` | 260 | 7 | per summon |

The deps, with `Lazy<>` marked:

- **PK-1:** `IDBService`, `ICompanionLookup`. **PK-2:** `IPerkService`, `ICompanionLookup`. **PK-3:** `IPerkService`, `IStatService`.
  **PK-4:** `IDBService`, `IFeatHotBar` (port), `ICompanionLookup`, `IAbilityCatalog`, `IPerkService`. **PK-5:** `IDBService`,
  `IFeatHotBar`, `IStatusEffectService`, `IAbilityCatalog`, `IPerkService`, `IPerkGrantedFeatService`, `IStatDrivenTrackerRefresh` (port).
- **SK-1:** `IDBService`, `ICompanionLookup`, `ITwinPistolPairQuery` (port). **SK-2:** `ISkillService`, `IDBService`. **SK-3:** `IDBService`,
  `IGuiRefreshPublisher`, `IPlayerFeedbackService`, `IStatService`, `ISkillService`, `ISkillRankService`.
- **PN-1:** `IDBService`, `Lazy<IDisguiseService>`, `Lazy<IPlayerDescriptorService>`. **PN-3:** `IPlayerNameService`, `IPlayerListRefresh`
  (exists), `IPortraitTellGuard` (port). **PN-2:** `IDBService`, `IPlayerNameService`, `IPlayerNameOverrideService`, `IPlayerListRefresh`.
- **MI-1:** `IAbilityCatalog`, `IDBService`, `IQuickCastService`, `Lazy<IStatService>`. **MI-2:** `IDBService`, `IDMPresenceService`,
  `IPlayerFeedbackService`, `IPerkService`, `IStatService`, `ISkillXPService`, `ICombatPointService`, `IMimicryService`. **MI-3:** `IDBService`,
  `IQuickCastService`, `IGuiService`, `IPerkService`, `IStatusEffectService`, `IMimicryService`, `IAbilityCatalog`.
- **BM-1:** `IDBService`, `IPerkService`, `IStatService`, `IGuiService`, `ICreatureStatService`, `IResistanceService`,
  `ICombatDamageCatalogService`. **BM-2:** `IDBService`, `IAIService`, `ICombatRecorderService`, `IPerkGrantedFeatService`,
  `IBeastMasteryService`. **BM-3:** `IDBService`, `ICompanionControlService`, `ICreatureStatService`, `IStatusEffectApplicationService`,
  `IItemService`, `IGuiService`, `IBeastMasteryService`. **BM-4:** `IDBService`, `IPropertyService`, `IIncubationFieldNoteService`,
  `IResistanceService`, `ICombatDamageCatalogService`, `IPerkService`, `IGuiService`, `IBeastMasteryService`.
- **DR-1:** `IItemService`. **DR-3:** `IItemService`, `IDroidService`. **DR-6:** `IRaceService`, `IPowerGloveEquipmentService`,
  `ITwinPistolEquipmentService`, `IDroidEquipmentDiagnosticsService`, `ICombatRecorderService`, `IDroidService`, `IDroidInventoryService`.
  **DR-2:** `IGuiService`, `ICreatureStatService`, `IResistanceService`, `IPerkGrantedFeatService`, `IAIService`, `IDroidService`,
  `IDroidRestoreService`. **DR-5:** `ICompanionControlService`, `IRecastService`, `IGuiService`, `IDroidService`, `IDroidInventoryService`.
  **DR-4:** `ICompanionControlService`, `ICreatureStatService`, `IStatusEffectApplicationService`, `IGuiService`, `IDroidService`,
  `IDroidInventoryService`.

Layers, bottom to top (a scan of the final constructor graph with these parts finds no plain cycle):

- Perk: PK-1 < PK-2, PK-3, PK-4 < PK-5. Skill: SK-1 < SK-2 < SK-3. PlayerName: PN-1 < PN-3 < PN-2. Mimicry: MI-1 < MI-2, MI-3.
- Beast: BM-1 < BM-2, BM-3, BM-4. Droid: DR-1 < DR-3 < DR-6 < DR-2; DR-3 < DR-5, DR-4.
- Between services: PK-4 < BM-2 and DR-2; SK-3 < MI-2; PK-1 < MI-2, MI-3, BM-1, BM-4.

### Members by part (line counts)

Fields are summed (`N tables, L lines`); sizes are member lines.

- **PK-1:** 15 tables 21; `CacheData` 10, `CachePerks` 122, `CacheCharacterTypes` 14, `CacheTriggers` 42, four `GetAll*Triggers` 8 each, `GetAllPerks` 8,
  `GetAllActivePerks` 9, `GetAllPerkCategories` 8, `GetAllActivePerkCategories` 9, `GetActivePerksInCategory` 11, `GetPerkDetails` 9,
  `TryGetPerkDetails` 4, `GetPerkCategoryType` 6, `IsPerkInCategory` 11, `GetPerkCategoryDetails` 9, `GetCharacterType` 9, `GetPerkLevelTier` 15,
  `GetPerkLevel` 43, `GetPlayerPerkLevel` 12, `GetPlayerEffectivePerkLevel` 19 and 47, `GetBeastPerkLevel` 49, `UnlockPerkForPlayer` 19, seams `GetPerkListDefinitionTypes` 7,
  `IsDroid` 4, `GetDroidPerkLevel` 4 (597). New: `EnumerateAllPerks`, `GetPerkMaxLevel`, `GetPerksWithSkillRequirement` (one-line reads of the tables).
- **PK-2:** 2 tables 2, the two group types 13 and 10; `CacheStatBonusPerk` 40, `GetOrCreateStatBonusGroup` 19, `GetOrCreateTargetedStatBonusGroup` 18,
  `GetStatSources` 32, `GetStatBonus` 28, `GetTargetedStatBonus` 30, `CalculateStatBonuses` 16, `GetStatBonusPerkLevel` 14, `IsTargetedPerk` 5,
  `GetPerkTypeFromStat` 6, seam `AggregateStatAdjustment` 4 (252). New: the boot handler `CachePerkStatBonuses`, the seam `IsDroid`.
- **PK-3:** 3 constants 4; `GetForceAffinity` 10, `TryGetForceSideAffinity` 26, the magnitude, hit chance and restorative reads 6, 6 and 4, the three
  `ApplyForceAffinity*` 4, 5 and 7, `ApplyMagnitudeMultiplier` 7, seam `GetCreatureStatAdjustment` 4 (96).
- **PK-4:** 9 tables and flag 9; `CachePerkFeatData` 6, `CachePerkFeatLookups` 51, `BuildActiveAbilityFeatsByLevel` 25, `FindActiveAbilityRecastGroup` 18,
  `IsActiveAbilityFeatForPerk` 9, `CachePerkLevelFeatLookups` 44, `FindCurrentActiveAbilityFeats` 12, five public feat reads 5 to 7, `RestorePlayerFeats` 23,
  `SyncGrantedFeats` 33, `ShouldEnforceActiveAbilityFeatReplacement` 13, `IsCurrentActiveAbilityFeat` 9, the two set reads 5, `GetCachedFeatList` and
  `GetCachedFeatSet` 14, `NormalizePerkFeatCacheLevel` 12, `EnsurePerkFeatCacheLoaded` 10, `IsReplacingActiveAbilityFeat` 5, `AddDistinct` 7, seam
  `GetAllAbilityDetails` 4 (385). New: `ResetPerkFeatCache`, the seam `IsDroid`.
- **PK-5:** `RemoveStatusEffectsOnPerkRefund` 35, `GetSourceOwnedStatusEffectRefunds` 14, `RemovePerkLevelOnSkillDecay` 66, seam `RefreshStatDrivenTrackerEffects` 4 (123);
  one more `GetAllAbilityDetails` seam.
- **SK-1:** 18 fields 31 (the 15 catalog tables, `_itemToSkillMapping`, constants); `APCap` 4, `GetTotalSkillPoints` 11, `AddMissingSkills` 23,
  `GetCreatureSkillRank` 20, `CacheData` 85, `OrganizeSkillsForDisplay` 27, 14 table reads 8 or 9, `GetSkillDetails` 9, `GetSkillCategoryDetails` 9, `LoadMappings` 9,
  `LoadItemToSkillMapping` 88, `GetSkillTypeByBaseItem` 13, `GetAttackSkillTypeByBaseItem` 12, `GetWeaponSkill` 14, `CacheXPChartData` 10, `GetRequiredXP` 4,
  `GetTotalRequiredXP` 9, `GetLevelByTotalXP` 9, the two espionage rank reads 7, seams `IsDroid`, `GetDroidSkillRank` 5 (579).
- **SK-2:** the cap set 4; `GetTotalContributingSkillRanks` 13, `IsSkillCapContributingSkill` 6, `ShouldDecaySkillRank` 4, `GetRankLimit` 14, `GetRankCeiling` 10,
  `GetDecayCandidates` 31, `GetDecayCapacity` 25, `IsBlockedBySkillCap` 13, `GetMaxDistributableXP` 17 and 55 (203).
- **SK-3:** 2 fields 8; `GiveSkillXP` 252 (cut, D8), `ApplyAbilityPoint` 19, `ResetLimitWarningsOnEnter` 12, `ResetLimitWarningsOnExit` 12, `ForgetLimitWarnings` 4,
  `CalculateEspionageXP` 4, `GetPracticeXP` 7, `GetDeltaXP` 7, seams `GetExperiencePercentAdjustment` 5, `ScheduleXPMessages` 5 (347).
- **PN-1:** 8 fields 9; `ReleaseKnownNamesOnExit` 9, `GetDisplayName` 4, `GetAuditName` 9, `GetAuditIdentity` 12, `GetDisplayNameByPlayerId` 27, `GetPlainDisplayNameByPlayerId` 35,
  `GetKnownNameOrFallbackByPlayerId` 25, `GetColoredDisplayName` 22, `SanitizeKnownName` 25, `GetKnownNames` 18, `FindKnownNames` 20, `BuildStaffDisplayName` 7,
  `ResolveDisplayName` 30, two `Build*WithDescriptor` 4, `CanUseKnownName` 8, `ShouldShowDescriptorForNamedPlayers` 28, `ShouldShowOwnDescriptor` 17, two `TryGetKnownName` 5 and 9,
  six identity seams 5 each (391). New public members: `ForgetKnownNames`, `ForgetDescriptorSetting`, and the identity seams made public.
- **PN-2:** `SearchKnownPlayerIdsByName` 27, `ValidateKnownName` 10, `ValidateKnownNameInput` 7, `ValidateKnownNameAssignment` 17, `SetKnownName` 18, `ValidateKnownNameTarget` 10,
  `ContainsColorToken` 7, `ValidateKnownNameIsUnique` 22, `ForgetKnownName` 18, `DeleteKnownNameReferences` 36 (182).
- **PN-3:** 4 fields 6 (`AfkPlayers`, `_afkMovementCheck`, the staff marker, the tell variable); `ApplyNameOverridesOnEnter` 23, `ClearAfkOnExit` 7, `IsAfk` 1, `OnCarriedStateAdopted` 18,
  `ToggleAfk` 14, `RemoveAfk` 13, `CheckAfkMovement` 18, the two suffix reads 6, `PreventAmbiguousTellTargets` 46, `GetChatDisplayName` 4,
  `SendChatMessageWithChatNameOverride` 13, `GetColoredChatDisplayName` 10, `RefreshNameOverridesForObserver` 27, `RefreshNameOverridesForPlayer` 5,
  `ApplyNameOverridesForPlayer` 38, `ApplyNameOverridesForDMObserver` 13, `ApplyTrueNameOverride` 14, `ApplyNameOverride` 21, `CanApplyChatNameOverride` 9,
  `ApplyChatNameOverride` 13, `RestoreNameOverride` 20, `ResolveChatDisplayName` 26, `CountDisplayNameMatches` 19, seams `ScheduleRepeating`, `EnsureUnknownDisplayName` 5 (430).
- **MI-1:** 7 fields 19; `CacheData` 68 (4 lines lighter), `GetResistanceBonus` 25, `GetTechniqueIcon` 8, `CarryTechniqueDescriptions` 13, `AdoptTechniqueDescriptions` 17,
  `GetTechniqueDescription` 8, `GetTechniqueDetail` 7, `IsTechnique` 7, `TechniqueCount` 4, seams 2 each (193). New: `TryGetTechniqueForNpcFeat`, `EnumerateTechniques`.
- **MI-2:** 17 fields and constants 38; `OnCreatureAbilityUsed` 28, `GetNearbyPlayers` 12, `TryRecordAnalysis` 20, `CombatPointXPDistributed` 27, `CalculateAnalysisXP` 12,
  `ClearAnalysisOnPlayerExit` 18, `TryRecordWitness` 45, `OnCreatureDeath` 17, `SweepStaleWitnesses` 28, `TryLearnTechniques` 69, `CalculateLearnChance` 19,
  `GrantAllTechniques` 29, six seams 2 to 4 (392). New: the boot handler `ScheduleWitnessSweep`.
- **MI-3:** 5 constants 6; `GetMaxSlots` 12 and 19, `GetUsedSlots` 8 and 17, `CanEquip` 8 and 54, `EquipTechnique` 23, `UnequipTechnique` 27, `EnforceSlotBudget` 13 and 58,
  `OnMimicrySkillDecay` 14, `UnequipAllTechniques` 30, `OnPlayerLogin` 17, `RestorePlayerTechniques` 21, `GrantTechniqueFeat` 36, `ValidateLoadout` 34, `CaptureLoadoutHotbar` 14,
  `ApplyLoadout` 60, `RevokeTechniqueFeat` 32, `IsFeatOnHotBar` 13, `AddFeatToHotBar` 20, `RemoveFeatFromHotBar` 13, two `GetLearnedTechniques` 8 and 17, two
  `GetEquippedTechniques` 8 and 17, two `IsTechniqueEquipped` 11 and 8, five seams (667).
- **BM-1:** 19 fields 135 (the two XP tables are 106 and about 25); `CacheData` 10 and the five `Load*` 4 to 17, the catalog reads 4, `IsPlayerBeast` 11, `GiveBeastXP` 31, `GiveBeastRPXP` 33,
  `GetBeastLevelCap` 7, `GetMaxDistributableXP` 20, `AddXP` 36, `GetRequiredXP` 4, `ApplyStats` 58, `CombatPointXPDistributed` 27, `GetIncubationPercentageById` 11,
  `IsIncubationCraftingItem` 13, `IsBeastEgg` 9, six seams (598).
- **BM-2:** `SpawnBeast` 82, `GetCompanionSlotValidationError` 16, seam `SyncGrantedFeats` 8 (109).
- **BM-3:** the twelve `BeastOn*` handlers 5 to 21, `RemoveAssociate` 12, `OnAcquireItem` 26, `OpenStablesMenu` 13, twelve seams 5 to 8 (243).
- **BM-4:** `_incubationDayOverrides` 2; `UseIncubator` 46, `DetermineMutation` 23, four day-override members 4 to 6, two `CreateBeastEgg` 49 and 11, `CalculateEggVendorBonus` 24,
  `GetResistancePurity` 7, `OnRemoveProperty` 16, `UseExtractDNAObject` 9 (218).
- **DR-1:** 12 fields and constants 13; `GetInstructionDiscValidationError` 16, `CacheData` 9, `CachePersonalities` 9, `IsDroid` 9, `IsBloodlessMachine` 14, `IsSpawning` 4,
  `GetControllerItem` 10, `UpdateEquippedItemSnapshot` 25, `LoadDroidItemPropertyDetails` 6, `LoadDroidPartItemPropertyDetails` 130, `GetDroid` 12, `GetPendingItemRestoreKey` 1,
  `LoadConstructedDroid` 7, `SaveConstructedDroid` 10, `SaveInstructions` 14 (319). New: `GetPersonality`; six private constants become public.
- **DR-2:** `SpawnDroid` (about 100 after the cut). **DR-6:** `RestoreDroidItem` 27 and the three pieces of `SpawnDroid` (about 175).
- **DR-3:** `CanGiveItemToDroid` 14, `OnAcquireItem` 9, `AcquireDroidItem` 37, `OnLostItem` 15, `OnEquipItem` 17, `OnUnequipItem` 17, `OnEquipmentChanged` 9, two `GetDroidItemId` 9 and 10,
  `IsStoredItem` 12, `ReturnRejectedItems` 28, `GetGeneratedAmmunition` 19, `ReadGeneratedAmmunitionFlags` 14, `SaveItemSnapshot` 46 (270).
- **DR-4:** `UseDroidAssemblyTerminal` 12, `EditDroidAppearance` 33, eleven `DroidOn*` 5 to 21 (145). **DR-5:** `OnPlayerExit` 11, `ClearTemporaryData` 9, `DespawnDroid` 25,
  `CloseAppearanceEditor` 5, `RemoveAssociate` 10, `DroidOnDeath` 19 (85).

### Notes on the parts

**Perk.**
- `PerkService` is left with two services (`IDBService`, `ICompanionLookup`) and no `Lazy<>`; it reaches nothing else, which is why
  `StealthService`'s and `AIService`'s `Lazy<IPerkService>` become needless (Cycles).
- Tables other parts read: `_allPerks` (the two builds, `TryGetForceSideAffinity`, `RestorePlayerFeats`), `_perkMaxLevels` (the feat cache
  and its level clamp), `_perksWithSkillRequirement` (the decay handler). They are read through `EnumerateAllPerks()` (no copy: `GetAllPerks()` copies
  today, and `PerkFacadeAllocationTests` pins the feat reads at zero bytes), `GetPerkMaxLevel` and `GetPerksWithSkillRequirement`.
- `CachePerks` loses its `CacheStatBonusPerk` call and the `_perkFeatCacheLoaded = false` line (D5). The original called `CacheStatBonusPerk` once per
  built perk in definition order; the new handler walks the table in insertion order, which is the same unless one `PerkType` is built twice (the table
  keeps the last, the original called twice). P5-10b adds a test that no `PerkType` appears in two definitions and a fixture of every perk's
  `GetStatSources` on the full catalog, captured on base and compared at the head.
- Each of PK-1, PK-2 and PK-4 keeps its own one-line `IsDroid` seam over `ICompanionLookup`, as each part carries its seams.
- `GetPerkLevel` stays on PK-1: 55 files use it. `RemovePerkLevelOnSkillDecay` reads `GetPlayerEffectivePerkLevel` and `GetPerkDetails` (PK-1), and
  calls `SyncGrantedFeats` (PK-4), so PK-5 is the top.

**Skill.**
- The three XP formulas (`GetDeltaXP`, `GetPracticeXP`, `CalculateEspionageXP`) move with `GiveSkillXP`. Five domain holders use only those and `GiveSkillXP`
  (`TrapsService`, `EspionageInfiltrationService`, `SlicingSessionService`, `GuildTaskXpService`, `MimicryService`), so with the formulas in SK-3 none gains a
  service; `TrapsService` is at 10 services today (by a source scan) and would have been the one to cross the limit. `CombatPointService`, `LanguageService` and
  `CraftService` also read the catalog and gain one. The chart reads that the rank rules need (`GetRequiredXP`) stay on SK-1.
- `GiveSkillXP` calls `GetRankCeiling`, `GetDecayCandidates` and `ShouldDecaySkillRank` (SK-2) and `GetRequiredXP`, `GetSkillDetails` (SK-1); SK-2 calls
  `GetSkillDetails` and `GetRequiredXP` (SK-1) and reads `_allSkillsContributingToCap` through `GetAllContributingSkills`. The cap set `_skillCapContributingSkillTypes`
  moves with its only reader, `IsSkillCapContributingSkill`. `SkillCap` is already a public constant.
- The SK-3 constructor builds `SkillXPMessageQueue` with a closure that calls `GetSkillDetails`; it reads it through `ISkillService`.
- `AddMissingSkills` (a login handler over the catalog and the database) stays on SK-1.

**PlayerName.**
- State stays on PN-1: `KnownNamesByObserverId`, `_knownNamesWriteVersion`, `ShowDescriptorsForNamedPlayersByObserverId` and `_scriptContextHooked` (the descriptor
  script-context hook is registered on first fill). Two other parts write to them: `DeleteKnownNameReferences` (PN-2) removes observer entries, and
  `RefreshNameOverridesForObserver` (PN-3) removes a descriptor setting. They call two new PN-1 members, `ForgetKnownNames(observerId)` and
  `ForgetDescriptorSetting(observerId)`, which hold the one `Remove` line each.
- PN-1 is the only holder of the two `Lazy<>`. The six one-line seams over them (`GetActiveDisguise`, `GetDisplayDescriptor`, `GetIdentityKey`,
  `ShouldScrambleAccountName`, `EnsureUnknownDisplayName`, `GetUnknownDisplayNameByPlayerId`) become public on PN-1 and PN-2 and PN-3 call them there. Without that, each part
  would hold its own `Lazy<>` (a scan finds all of them needed): 2 become about 5. PN-1's private helpers PN-3 calls (`ResolveDisplayName`, `BuildDisplayNameWithDescriptor`,
  `BuildStaffDisplayName`) become public too.
- Chat display names (`GetChatDisplayName`, `GetColoredChatDisplayName`, `ResolveChatDisplayName`) go to PN-3 with the chat override code that uses them. That leaves
  `CommunicationService` (10 services today) on one part, `IPlayerNameOverrideService`, in place of `IPlayerNameService`.
- AFK stays with the overrides: the `[AFK]` suffix is read by every override and written by `ToggleAfk` and the movement check, which refresh the overrides. `AfkPlayers`
  keeps `[ReloadState(Carried)]` and gets `[ReloadCarriedFrom]` for its old class name; `OnCarriedStateAdopted` (restart the check, reapply every override) moves with it.
- `ApplyNameOverride` becomes public on PN-3 (PN-2 calls it after `SetKnownName` and `ForgetKnownName`). `CountDisplayNameMatches` moves with its only caller, the tell handler.

**Mimicry.**
- The technique tables stay on MI-1 and the two other parts read them through two new members, `TryGetTechniqueForNpcFeat(npcFeat, out technique)` (one table lookup
  in `OnCreatureAbilityUsed`) and `EnumerateTechniques()` (the loops in `GrantAllTechniques` and the loadout reads); `GetTechniqueDetail` and `IsTechnique` are already there.
- `CacheData` schedules the witness sweep (`_witnessSweepScheduled`, `ScheduleRepeating(SweepStaleWitnesses, 5 min)`); that is witness state. The four lines become
  a `Rerun` boot handler `ScheduleWitnessSweep` on `OnModuleCacheAfter` right after `MimicryService.CacheData` (D5). `_witnessSweepScheduled` is marked `Runtime` (the part
  owns a rerun handler). The first sweep is five minutes after boot, so the handler order against other handlers does not matter.
- `GetNearbyPlayers` is called by `OnCreatureAbilityUsed` and `OnCreatureDeath`, both witness code; it stays there. `GrantAllTechniques` (a DM tool that writes learned
  techniques) goes with learning. `GrantTechniqueFeat`, `RevokeTechniqueFeat`, the three hotbar helpers and `RestorePlayerTechniques` are called only from loadout code.
- Both `Carried` tables move to MI-2 with `[ReloadCarriedFrom]` for the old class name; `ClearAnalysisOnPlayerExit` (clears `_analysisObservers`) and `SweepStaleWitnesses`
  move with them.

**Beast.**
- `ApplyStats` is called by `GiveBeastXP` (level-up re-applies stats) and by `SpawnBeast`. With spawn above the catalog and XP, `ApplyStats` and its seam stay on
  the stay-part (+3 services: resistance, damage catalog, creature stat), so spawn calls down and there is no cycle. This is why BM-2 is a small part.
- The two XP tables and `CombatPointXPDistributed` stay with the XP code, so no new boot handler is needed (`LoadHighestDelta` stays in `CacheData`).
- `IBeastMasteryService : IBeastCorpseInfo` (Items-owned, `GetBeastName`) stays on the stay-part, with the registration alias `AddSingleton<IBeastCorpseInfo>(...)`, so
  `LootService` does not change.
- `UseIncubator` and `OpenStablesMenu` both open a window (`TogglePlayerWindow`, a one-line seam over `IGuiService`); each part keeps its own seam.
- The incubation part no longer needs the stay-part to hold `IIncubationFieldNoteService`, so that service's `Lazy<IBeastMasteryService>` becomes plain.

**Droid.**
- A single `DroidSpawnService` would use 14 services (the whole of today's list), so the spawn code is cut across three services by what it does (D8): DR-2 keeps the
  stats, skin, perks, AI profile and scripts (7 services); DR-6 restores the saved equipment, inventory, appearance, portrait and sound set, and runs the closing steps
  (the pair rebuild, `RememberHands`, `SaveItemSnapshot`, the delayed heal under setup attribution; 7 services); DR-5 despawns and handles death.
- Constants shared by the parts (`DroidObjectVariable`, `DroidControlItemVariable`, `DroidIsSpawning`, `DroidItemId`, the two pending-item prefixes, `RecastDelaySeconds`) are
  private on `DroidService` today (`DroidResref` and `DroidControlItemResref` are already public); the six become public constants on DR-1. `_droidPersonalities` is read by
  spawn (greeting) and despawn (farewell) through a new DR-1 member `GetPersonality`.
- `UpdateEquippedItemSnapshot` stays on DR-1 (it reads only the controller item and the constructed droid and has seven domain callers, the tint-map services).
- `DroidOnDeath` goes with despawn: it repeats `DespawnDroid`'s cleanup steps (`ClearTemporaryData`, `CloseAppearanceEditor`, `ReturnRejectedItems`, `SaveItemSnapshot`).
- The protected parameterless constructor (979-992) and `ReadGeneratedAmmunitionFlags` go to DR-3; `DroidServiceTests`, `DroidServiceWorldTests` and `ItemServiceTests`
  subclass or construct `DroidService` today (Tests).

## The upward references and how each goes

`UpwardEdges.txt` has four lines for these files (`PerkService.cs` Character -> Abilities 4 and Character -> Combat 3, `PlayerNameService.cs` Character -> Social 4,
`SkillService.cs` Character -> Combat 3), and `ConversationService.cs` and `PlayerFeedbackService.cs` (Foundation -> Character, 3 each) name `IPlayerNameService` for
`GetDisplayName`. Appendix C names P5-10 for Character -> Abilities (Perk), Character -> Social (PlayerName) and the Foundation-owned name lookup. All are inverted with a one-member port; `IPlayerListRefresh` (Character-owned, extended by
`IPlayerListService`, registered as an alias) already exists, and the new ports are registered the same way.

| Row | Members | Mechanism | Owner of the interface | PR |
|---|---|---|---|---|
| Character -> Abilities: Perk `IQuickCastService` (`KnowsFeat`, `RemoveFeat`, `ApplyPreferences`) | 4 | port `IFeatHotBar`; `IQuickCastService : IFeatHotBar`; registered with a forwarding lambda | Character | d |
| Character -> Combat: Perk `Lazy<ICombatService>` (`RefreshStatDrivenTrackerEffects`) | 3 | port `IStatDrivenTrackerRefresh`; `ICombatService : IStatDrivenTrackerRefresh` (the member exists; one interface line, no class change) | Character | d |
| Character -> Combat: Skill `ITwinPistolEquipmentService` (`IsActivePair`) | 3 | port `ITwinPistolPairQuery`; `ITwinPistolEquipmentService : ITwinPistolPairQuery` | Character | e1 |
| Character -> Social: PlayerName `IPlayerListService` (`QueueRefresh`) | 6 | the existing `IPlayerListRefresh` | Character | f1, f2 |
| Character -> Social: PlayerName `IChatPortraitTellsService` (`RefuseStalePortraitTell`) | 3 | port `IPortraitTellGuard`; `IChatPortraitTellsService : IPortraitTellGuard` | Character | f1 |
| Foundation -> Character: `ConversationService`, `PlayerFeedbackService` -> `IPlayerNameService.GetDisplayName` | 3 + 3 | port `IDisplayNameLookup`; `IPlayerNameService : IDisplayNameLookup`; the Foundation services take the port | Foundation | f1 |
| Character -> Companions (Perk -> Beast and Droid) | 11 | done in P5-1g (`ICompanionLookup`); P5-10 keeps the lookup | Character | none |

The two files named after the ports (`ConversationService.cs`, `PlayerFeedbackService.cs`) lose their `IPlayerNameService` lines from `UpwardEdges.txt` only if that is
the last Character type they name; the PR records what the regenerated file shows. `IBeastCorpseInfo` (Items-owned) stays on the Beast stay-part. Logic that names content
(`LogicToContent.txt`) is not removed here; it moves with the code (Baselines). No `Lazy<>`, `Func<>` or late lookup is added.

## Holders

Holders are services whose constructors take the interface, counted as the guard counts them (`GameServices` properties reached count once per service).
No holder gains a `Lazy<>`. The numbers that matter are the services at or near 10.

| Holder | Change | After |
|---|---|---|
| `StatService` (about 10) | c: `IPerkService` -> `IPerkStatBonusService` (it calls only `GetStatSources` and `GetStatBonus`) | same |
| `CombatQueriesService` (7) | c: keeps `IPerkService` (`IsPerkInCategory`) and gains `IPerkStatBonusService` | 8 |
| `AbilityDamageStageService` (10), `CombatAbilityHitService` (13), `CombatAbilityMagnitudeService` (3) | c: `IPerkService` -> `IPerkForceAffinityService` (affinity reads only) | same |
| `AbilityActivationService` (8) | d: `IPerkService` -> `IPerkGrantedFeatService`; e: nothing; g1: `IMimicryService` -> `IMimicryLoadoutService` (`IsTechniqueEquipped`) | same |
| `BeastMasteryService` (14), `DroidService` (14) | d: the `SyncGrantedFeats` seam reads `IPerkGrantedFeatService` (the seam line and one constructor type) | split later |
| `ResistanceService` (4) | none: `GetResistanceBonus` stays on `IMimicryService` | same |
| `MimicryService` (11) | g1: loses `IGuiService`, `IStatusEffectService`; g2: loses five more | 9, then 4 |
| `TrapsService` (10), `PerformanceAudienceService` (10), `EspionageInfiltrationService` (6), `SlicingSessionService` (9), `GuildTaskXpService` (4) | e2: `ISkillService` -> `ISkillXPService` (they use `GiveSkillXP` and the XP formulas only) | same |
| `CombatPointService` (8), `LanguageService` (3), `CraftService` (6) | e2: keep `ISkillService` and gain `ISkillXPService` | +1 each |
| `CommunicationService` (10) | f1: `IPlayerNameService` -> `IPlayerNameOverrideService` (`GetColoredChatDisplayName`, `SendChatMessageWithChatNameOverride`) | same |
| `PlayerListService` (9) | f1: keeps `IPlayerNameService`, gains `IPlayerNameOverrideService` (`IsAfk`) | 10 |
| `DisguiseService` (7), `PlayerDescriptorService` (3), `PlayerIntroductionService` (3) | f1, f2: gain the override and known-name parts | +2, +2, +1 |
| `ConversationService`, `PlayerFeedbackService` (Foundation) | f1: `IPlayerNameService` -> `IDisplayNameLookup` | same |
| `IncubationFieldNoteService` | h1: `Lazy<IBeastMasteryService>` becomes plain | `Lazy<>` -1 |
| `TintMapApplyService`, `TintMapCarryService`, `TintMapDroidOverrideService`, `TintMapItemColorService` | none: `UpdateEquippedItemSnapshot` stays on `IDroidService` | same |
| `DroidEquipmentDiagnosticsService` | i2: `Lazy<IDroidService>` becomes plain (the stay-part no longer holds the diagnostics) | `Lazy<>` -1 |
| `StealthService`, `AIService` | d: `Lazy<IPerkService>` becomes plain wherever it is still lazy (P5-8e already does it for `AIService`) | `Lazy<>` -1 each |
| `ChatPortraitTellsService` | f1: `Lazy<IPlayerNameService>` becomes plain (it reads only the stay-part) | `Lazy<>` -1 |

Interfaces that keep their holders on the stay-part:

- `IPerkService` keeps 20 of its 42 members, among them `GetPerkLevel` (60 non-test call sites in 55 files, 26 of the sites in domain code), `GetPerkDetails`, `TryGetPerkDetails` and the category reads; the
  25 domain and 12 content holders that name it change only if they also use a moved member.
- `ISkillService` keeps 31 of 40 (catalog, chart reads, `GetWeaponSkill`, `GetCreatureSkillRank`; `StatService`, the Combat services, `ItemService`, the Craft services and the other domain holders).
- `IPlayerNameService` keeps 12 of 29 (non-test call sites: `GetDisplayName` 45, `GetColoredDisplayName` 24, `SanitizeKnownName` 12, `GetAuditName` 11).
- `IMimicryService` keeps 7 of 29, among them `GetResistanceBonus` (`ResistanceService`).
- `IBeastMasteryService` keeps 19 of 44 (`GetBeastDetail` 24 non-test sites, `IsPlayerBeast` 31, XP, IDs) and `IBeastCorpseInfo`.
- `IDroidService` keeps 14 of 37 (`IsDroid` 25 non-test sites, `LoadConstructedDroid`, `SaveConstructedDroid`, `GetControllerItem`, `UpdateEquippedItemSnapshot`; the tint-map services).

## Callers

Members that change home; every other member stays on its interface. Counts are call sites (files) from a regex search at `34c3373a0`; native is zero for every row.

| Members | Moves to | Domain | Content | Tests | Engine tests |
|---|---|---|---|---|---|
| `GetStatSources`, `GetStatBonus`, `GetTargetedStatBonus` | `IPerkStatBonusService` | 3 (2: `StatService`, `CombatQueriesService`) | 0 | 6 (2) | 4 (2) |
| seven force-affinity members | `IPerkForceAffinityService` | 7 (3) | 12 (3: `CharacterSheetUtilityRows`, `PerkDetailText`, `PerkListBuilder`) | 12 (3) | 0 |
| nine feat members (`SyncGrantedFeats`, `RestorePlayerFeats`, `IsCurrentActiveAbilityFeat`, ...) | `IPerkGrantedFeatService` | 5 (3) | 15 (6, among them `UsePerkFeat`) | 12 (3) | 17 (5) |
| `RemoveStatusEffectsOnPerkRefund`, `RemovePerkLevelOnSkillDecay`, `GetSourceOwnedStatusEffectRefunds` | `IPerkRefundService` | 0 | 3 (2) | 3 (1) | 0 (and `PerkRefundChainTests`, integration) |
| `GetRankLimit`, `GetRankCeiling`, `GetTotalContributingSkillRanks`, `IsBlockedBySkillCap`, `GetMaxDistributableXP` | `ISkillRankService` | 0 | 9 (4) | 36 (2) | 0 (and 13 sites in 1 characterization file) |
| `GiveSkillXP`, `GetDeltaXP`, `GetPracticeXP`, `CalculateEspionageXP` | `ISkillXPService` | 35 (17: 9 services, 8 static helpers and world classes) | 10 (7) | 31 (14) | 2 (2) |
| `ApplyNameOverridesOnEnter`, `ClearAfkOnExit`, `IsAfk`, `ToggleAfk`, `PreventAmbiguousTellTargets`, the two chat display names, `SendChatMessageWithChatNameOverride`, `RefreshNameOverridesFor*` | `IPlayerNameOverrideService` | 7 (4) | 7 (5) | 6 (3) | 29 (4) |
| `SearchKnownPlayerIdsByName`, `ValidateKnownName*`, `SetKnownName`, `ForgetKnownName`, `DeleteKnownNameReferences` | `IPlayerKnownNameService` | 6 (3) | 6 (2) | 8 (3) | 16 (7) |
| `OnCreatureAbilityUsed`, `CombatPointXPDistributed`, `CalculateAnalysisXP`, `CalculateLearnChance`, `GrantAllTechniques`, `OnCreatureDeath` | `IMimicryWitnessService` | 0 | 3 (2: `UsePerkFeat`, `DMPlayerChatCommand`) | 6 (1) | 7 (2) |
| slot, equip, loadout, learned and equipped reads, login, skill decay | `IMimicryLoadoutService` | 1 (1: `AbilityActivationService`) | 10 (2: `TechniquesViewModel`, `MimicryPerkDefinition`) | 16 (2) | 22 (5) |
| `SpawnBeast`, `GetCompanionSlotValidationError` | `IBeastSpawnService` | 0 | 5 (3) | 1 (1) | 2 (2) |
| incubator members, `CreateBeastEgg`, day overrides, DNA, property removal | `IBeastIncubationService` | 1 (1) | 11 (4) | 6 (2) | 2 (1) |
| the 15 beast event and terminal handlers | `IBeastEventService` | 0 | 0 | 0 | 0 |
| `OnAcquireItem`, `OnLostItem`, `OnEquipItem`, `OnUnequipItem`, `OnEquipmentChanged`, `SaveItemSnapshot` | `IDroidInventoryService` | 0 | 2 (1) | 0 | 0 |
| `SpawnDroid` | `IDroidSpawnService` | 0 | 1 (1: `DroidControlItemDefinition`) | 0 | 5 (3) |
| `RemoveAssociate`, `OnPlayerExit`, `DroidOnDeath` | `IDroidDespawnService` | 0 | 0 | 0 | 3 (1) |
| the 13 droid event, terminal and appearance handlers | `IDroidEventService` | 0 | 0 | 0 | 0 |

**Call text.** The content and native sites that change change the property name only (`Perk.SyncGrantedFeats` becomes `PerkGrantedFeat.SyncGrantedFeats`); `RemovedFacades.json` lists the
members by owner, `Apply-FacadeRemoval.ps1` rewrites the sites and `-Reverse` maps master's text back for the parity build. New `GameServices` properties: Character `PerkStatBonus`,
`PerkForceAffinity`, `PerkGrantedFeat`, `PerkRefund`, `SkillRank`, `SkillXP`, `PlayerKnownName`, `PlayerNameOverride`; Abilities `MimicryWitness`, `MimicryLoadout`; Companions `BeastSpawn`,
`BeastEvent`, `BeastIncubation`, `DroidInventory`, `DroidSpawn`, `DroidRestore`, `DroidDespawn`, `DroidEvent`. Content types named in P5-16 that change by that scripted edit:
`UsePerkFeat` (the feat calls in d, `OnCreatureAbilityUsed` in g2) and the character sheet files (`CharacterSheetAttributesTab`, `CharacterSheetViewModel`, `PerkListBuilder`; e1, c).

## Cycles and `Lazy<>`

The hypothetical final graph (the six services replaced by the parts above, each holder given the parts it uses) has no plain construction cycle. For each existing `Lazy<>` the
question is whether the target still reaches the owner through plain parameters:

| `Lazy<>` | After | Why |
|---|---|---|
| `PerkService -> Lazy<IStatService>` | gone (PK-3 plain, PK-2 uses `StatRules`) | `StatService` reads PK-2 and PK-1 but not PK-3 |
| `PerkService -> Lazy<ICombatService>` | gone (PK-5 takes `IStatDrivenTrackerRefresh`, plain) | nothing in `CombatService`'s closure reaches PK-5 |
| `SkillService -> Lazy<IStatService>` | gone (SK-3 plain) | `StatService` reads SK-1 only |
| `MimicryService -> Lazy<IStatService>` | stays on MI-1 | `StatService -> ... -> ResistanceService -> MI-1`; the potency read needs the stat service |
| `MimicryService -> Lazy<IStatService>` (witness) | gone (MI-2 plain) | no service in the stat closure takes MI-2 |
| `MimicryService -> Lazy<ICombatPointService>` | gone (MI-2 plain) | `AbilityActivationService` takes MI-3 only; MI-2 is reached by content |
| `PlayerNameService -> Lazy<IDisguiseService>`, `Lazy<IPlayerDescriptorService>` | stay on PN-1 | `DisguiseService` and `PlayerDescriptorService` call PN-1 |
| `StealthService`, `AIService -> Lazy<IPerkService>` | become plain in d | PK-1 reaches two classes |
| `ChatPortraitTellsService -> Lazy<IPlayerNameService>` | becomes plain in f1 | it reads PN-1 only; PN-1 does not reach it |
| `PlayerListService -> Lazy<>` for PN-1 | becomes plain in f1; the one for PN-3 stays | `PN-3 -> IPlayerListRefresh -> PlayerListService -> PN-3` |
| `IncubationFieldNoteService -> Lazy<IBeastMasteryService>` | becomes plain in h1 | BM-1 no longer holds the field notes |
| `DroidEquipmentDiagnosticsService -> Lazy<IDroidService>` | becomes plain in i2 | DR-1 no longer holds the diagnostics |
| `TwinPistolEquipmentService`, `ItemService -> Lazy<ISkillService>` | stay | `SkillService -> ITwinPistolPairQuery -> TwinPistolEquipmentService -> ...` (D9; open question 3) |

`LazyDependencyCycleTests` fails on a needless `Lazy<>`, so each PR turns the ones its change makes needless into plain parameters in the same PR, even in a file another item owns
(`StealthService.cs`, `PlayerListService.cs`). The six services' own `DeferredResolution.txt` lines go from seven parameters to three (Mimicry stay 1, PlayerName stay 2). Target 4 is met
by Perk, Skill, Beast and Droid; Mimicry (the potency read) and PlayerName (the disguise and descriptor paths) keep the three that P5-15 takes.

## State, handlers and reload

- **Handler moves** (`ScriptHandlerOrder.txt`, `HandlerRenames.txt`; the line is renamed in place, event and position unchanged, as for P5-8):

| Service | Methods that move | To |
|---|---|---|
| Perk | `CachePerkFeatData` (line 81) | PK-4 |
| Perk | `RemovePerkLevelOnSkillDecay` (1190) | PK-5 |
| Skill | `ResetLimitWarningsOnEnter` (768), `ResetLimitWarningsOnExit` (841) | SK-3 |
| PlayerName | `ApplyNameOverridesOnEnter` (761), `ClearAfkOnExit` (838), `PreventAmbiguousTellTargets` (941) | PN-3 |
| Mimicry | `CombatPointXPDistributed` (311), `ClearAnalysisOnPlayerExit` (195, 831), `OnCreatureDeath` (359) | MI-2 |
| Mimicry | `OnMimicrySkillDecay` (1189), `OnPlayerLogin` (756) | MI-3 |
| Beast | 15 methods, 16 lines (the twelve `BeastOn*`, `RemoveAssociate` x2, `OnAcquireItem`, `OpenStablesMenu`) | BM-3 |
| Beast | `UseIncubator`, `OnRemoveProperty`, `UseExtractDNAObject` | BM-4 |
| Droid | `OnAcquireItem`, `OnLostItem`, `OnEquipItem`, `OnUnequipItem`, `OnEquipmentChanged` x2 (6 lines) | DR-3 |
| Droid | `OnPlayerExit`, `RemoveAssociate` x2, `DroidOnDeath` (4 lines) | DR-5 |
| Droid | `UseDroidAssemblyTerminal`, `EditDroidAppearance`, eleven `DroidOn*` (13 lines) | DR-4 |

  `HandlerRenames.txt` lines to edit in place: Perk 2 of 3, Skill 2 of 6, PlayerName 3 of 4, Mimicry 5 of 6, Beast 18 of 20, Droid 21 of 22. Handlers that stay: `PerkService.CacheData`
  (41), `SkillService.CacheData`, `LoadMappings`, `CacheXPChartData`, `AddMissingSkills` (47-49, 767), `PlayerNameService.ReleaseKnownNamesOnExit` (837), `MimicryService.CacheData` (79),
  `BeastMasteryService.CacheData` (24) and `CombatPointXPDistributed` (310), `DroidService.CacheData`. Two handlers on `OnCombatPointXPDistribute` (Beast 310, Mimicry 311) keep their relative order.
- **Added handlers** (all `Rerun` boot handlers, listed directly after the handler they were cut from, `SplitBootHandlerTests`' `ServiceSplitMasterHandlers` naming master's `Perk.CacheData`
  and `Mimicry.CacheData`): `PerkStatBonusService.CachePerkStatBonuses` and `PerkGrantedFeatService.ResetPerkFeatCache` (after line 41), `MimicryWitnessService.ScheduleWitnessSweep`
  (after line 79). `BootHandlerClassification.txt` gains three lines and re-keys `PerkService.CachePerkFeatData`. A missing `Rerun` on `CachePerkStatBonuses` would leave every perk stat
  bonus empty after a hot reload, so the classification test and an engine reload check cover it.
- **Session classification** (`PlayerSessionClassification.txt`): `SkillService.ResetLimitWarningsOnEnter` and `ResetLimitWarningsOnExit`, `PlayerNameService.ApplyNameOverridesOnEnter` and
  `ClearAfkOnExit`, `MimicryService.OnPlayerLogin` and `ClearAnalysisOnPlayerExit`, `DroidService.OnPlayerExit` are re-keyed to their parts with their reasons unchanged.
- **`[ReloadState]`.** Carried: `_witnesses` and `_analysisObservers` move to MI-2, `AfkPlayers` to PN-3; each gets `[ReloadCarriedFrom("<old class>")]` so a reload from the previous build still adopts
  (`ReloadCarryHistory.txt` gains the three new keys and loses none; `ReloadAdoption.txt` lines 98, 99 and 104 re-key). Adopted: `_techniqueDescriptions` stays on MI-1 (the carry key is a string, not a class
  name). Runtime: `_limitWarningsSent` and `_xpMessages` move to SK-3, `_incubationDayOverrides` to BM-4, and `_witnessSweepScheduled` (MI-2, new marking: the part owns a rerun handler) is marked.
  `IReloadStateAdopter` moves to PN-3 with `AfkPlayers`; `OnCarriedStateAdopted` restarts the AFK check and reapplies every override, so it needs the override and AFK code together. Nothing is
  newly `Carried`.
- **What a hot reload must recheck.** After a reload: a player's perk stat bonuses and force affinity equal the values before (the two Rerun handlers); feat sync and the hot bar after a perk purchase; an AFK
  player stays AFK with the tag, and a tell between two named players still resolves (PN-3); a witnessed technique can still be learned and the analysis observers still block combat-point credit (MI-2); an
  equipped technique stays equipped (MI-3); a summoned droid keeps its equipment through a reload (DR-3). Run `scripts/run-hot-reload-check.sh --engine-filter HotReloadPlayers --reloads 2` on c, d, f1, g2
  and i3, plus the state round trips in `ReloadStateCarrierTests` and `ReloadDigestTests` for the three moved carried fields.
- **Attribution scopes.** `BeginSetupAttribution` is called by `SpawnBeast` (BM-2) and the droid closing steps (DR-6); P5-7j keeps it on the recorder stay-part, so the text does not change.

## Tests

- **Source readers by name** (12 test files, 31 reads of the six files by path, plus `InstanceMethod` harness reads): `PlayerNameRecognitionTests` (16 reads of `PlayerNameService.cs`),
  `PlayerIdentityAuditTests` (2 harness reads: `GetAuditName`, `GetAuditIdentity`), `MimicryTests` (2), `DMPossessionPlayerLoopTests` (harness: `OnCreatureAbilityUsed`, `GetNearbyPlayers`, both MI-2),
  `PerkRefundStatusEffectCleanupTests` (2), `SkillXPAwardTests` (2), `PlayerMessageAuditTests` (2, the file list at lines 53-54), `DeadPlayerXPTests`, `PerksWindowTests`, `TintMapReviewTests`
  (reads `DroidService.cs`), `CrossSkillPerkInteractionSafetyTests`, `SkillRankLimitTests`, `PerkStatBonusTests`. P5-10b adds `CharacterCompanionsSource` (the source of a member wherever it lives,
  as `CreaturesSource` does) with one group per service, and points every read at it, so no later PR edits them. The `ClockSeamSourceGuardTests` list names `Domain/Character/PerkService.cs` (the clock
  reads are `UnlockPerkForPlayer`, PK-1, and the decay handler, PK-5, so d adds `PerkRefundService.cs`) and `tools/Refactor/ClockSeamFiles.txt` line 99 follows.
- **Other files that name these files:** `PlayerMessageAudit.json` has 36 rows keyed by file (Beast 11, Skill 9, Droid 7, Mimicry 6, PlayerName 2, Perk 1); each row moves with its code and
  `PlayerMessageAuditTests` gets the part files. `StaticLookups.txt` line 317 (`BeastMasteryServiceTests.cs`, 10). `tools/Refactor/HandlerRenames.txt`.
- **Benches and doubles.** `PerkBench` (255 lines) subclasses `PerkService` and answers its seams; `PerkServiceTests` (908 lines, 45 tests), `SkillServiceTests` (979, 60), `PlayerNameServiceTests`
  (1,256, 112), `MimicryServiceTests` (1,420, 80) with `MimicryTests` (1,440, 53), `BeastMasteryServiceTests` (1,808, 105), `DroidServiceTests` (321, 23) with `DroidServiceWorldTests` (939, 45), `DroidWorld`
  (409) and `ItemServiceTests` (it builds a `DroidService`) each subclass one service. P5-10b gives each a part accessor (`Catalog`, `StatBonus`, `Affinity`, `Feats`, `Refund`; `Catalog`, `Rank`, `XP`; `Names`,
  `KnownNames`, `Overrides`; `Techniques`, `Witness`, `Loadout`; `Beasts`, `Spawn`, `Events`, `Incubation`; `Droids`, `Spawn`, `Inventory`, `Events`, `Despawn`, `Restore`) that is the one instance until a part moves,
  and splits each double into partial files by the part whose code calls each seam. Each PR then moves the tests of its members into a test class per part (the stay-part keeps the shared bench).
- **Unit test splits by PR** (every old test name is kept; the stay-part's class keeps the shared bench):
  c: `PerkServiceTests` gives the stat-bonus tests (with `PerkStatBonusTests`) to `PerkStatBonusServiceTests` and the force-affinity tests to `PerkForceAffinityServiceTests`;
  d: the feat tests to `PerkGrantedFeatServiceTests`, the refund tests with `PerkRefundStatusEffectCleanupTests` (10) to `PerkRefundServiceTests`, and `PerkTriggerTests` stays;
  e1: the rank-rule tests of `SkillServiceTests` and `SkillRankLimitTests` (46, mostly `GetRankLimit`, `GetRankCeiling`, `GetMaxDistributableXP`) to `SkillRankServiceTests`;
  e2: the award tests of `SkillServiceTests` with `SkillXPAwardTests` (10) and `SkillXPMessageQueueTests` (4) to `SkillXPServiceTests`;
  f1: `PlayerNameServiceTests` (112) gives the override, AFK and tell tests to `PlayerNameOverrideServiceTests`; f2: the validation and write tests to `PlayerKnownNameServiceTests`;
  g1: `MimicryServiceTests` (80) and `MimicryTests` (53) give the slot, equip and loadout tests (with `MimicryLoadoutTests`, 10) to `MimicryLoadoutServiceTests`; g2: the witness and learn tests to `MimicryWitnessServiceTests`;
  h1: `BeastMasteryServiceTests` (105) gives the spawn and incubation tests to `BeastSpawnServiceTests` and `BeastIncubationServiceTests`; h2: the event tests to `BeastEventServiceTests`;
  i1 to i3: `DroidServiceTests` (23) and `DroidServiceWorldTests` (45) split into `DroidInventoryServiceTests`, `DroidSpawnServiceTests` (with `DroidRestoreService`), `DroidDespawnServiceTests` and `DroidEventServiceTests`, and `DroidWorld` gains the part accessors.
- **Allocation pins and forwarding tests.** `PerkFacadeAllocationTests` (5 tests) and `SkillFacadeAllocationTests` (2) pin zero-byte reads through the facade; they are re-pointed by the call-text tool and the
  bytes stay.
- **New proof, written against the current code in P5-10b** (as P5-7b did): an ordered call-and-draw transcript of `GiveSkillXP` (bonuses, debt, rank-ups with decay, the cap warnings and the `Random` draws),
  of `SpawnDroid` (every engine call and database write, the order D8 must keep), of `TryLearnTechniques` and `TryRecordWitness`, of `UseIncubator` and `CreateBeastEgg` (seeded draws), and of
  `PreventAmbiguousTellTargets`; a fixture of every perk's `GetStatSources`, `GetForceAffinity*` and `GetCurrentActiveAbilityFeats` on the full catalog (the D5 equivalence); a catalog test that no `PerkType`
  appears in two definitions; the `DroidWorld` restore test run before and after the cut with the same item set.
- **Counts.** The tests that name the six services are about 630 `[Test]` and `[TestCase]` attributes in 18 files (above). The characterization tests that read skill and perk rules
  (`SkillRankRuleCharacterizationTests`, `SkillXPChartCharacterizationTests`, `SkillDefinitionCharacterizationTests`, `PerkDefinitionCharacterizationTests`, `CombatSkillRuleCharacterizationTests`) keep their
  expectations.
- **Each new service reaches the coverage floor in its own pull request** (`Domain/Character` 85, `Domain/Abilities` 95, `Domain/Companions` 88, `CoverageFloors.json`); the moved code keeps its tests.

**Per PR** (build once with `-p:RunPostBuildEvent=Never -v q -clp:ErrorsOnly`, then `dotnet test <project> --no-build --filter`):

- **Unit filters:** `FullyQualifiedName~` the service the PR moves (`PerkService`, `PerkStatBonus`, `SkillService`, `SkillXP`, `PlayerName`, `Mimicry`, `BeastMastery`, `Droid`) with `PerkFacadeAllocation`, `SkillFacadeAllocation`,
  `ConvertedServiceContainer`, `LazyDependencyCycle`, `PlayerMessageAudit`, `ClockSeamSourceGuard`, `SplitBootHandler`, `BootHandlerClassification`, `PlayerSessionClassification`, `ReloadAdoption`, `ReloadCarryHistory`,
  `ReloadStateCarrier`, `ScriptHandlerOrder`; `SWLOR.Architecture.Tests` with the baselines regenerated each PR (`SWLOR_UPDATE_SNAPSHOTS=1`, every moved line explained); `SWLOR.Game.Server.CharacterizationTests` with no
  expectation changed; `SWLOR.Game.Server.IntegrationTests` (`PerkRefundChainTests`, `SkillRankUpTests`) on c, d and e.
- **Roslyn token comparison** of every moved member of the old class against the part, with modifiers, receivers, the namespace and the named seams ignored (the P5-7 tool, `tools/Refactor/CompareCombatSplit.sh` is
  its text-level ancestor); the report lists each member that differs, and each difference must be one of the changes this guide names (D3, D6, D8, the promoted members, the seams).
- **Engine tests, base against head** (host entry, same module and haks; per-test outcomes must match): c: `PerkLevelBehaviorEngineTests`, `PerkSweepEngineTests`, `CombatPerkRegressionEngineTests`,
  `PerkTrackerRegressionEngineTests`; d: `PlayerPerkFeatRestorationEngineTests`, `AutomaticTargetingOwnershipEngineTests`, `PerformancePerkEngineTests`, `PerkTrackerRegressionEngineTests`; e1, e2:
  `BareHandedAttackSkillEngineTests`, `TwinPistolCrossSkillEngineTests`, `WeaponAbilityCrossSkillTraitEngineTests`, `DroidWeaponSkillMigrationEngineTests`, `MimicryObservationEngineTests`; f1, f2: `AfkNameEngineTests`,
  `DMPossessedNameEngineTests`, `PlayerListEngineTests`, `PlayerListNamePrivacyEngineTests`, `DisguiseKnownNameEngineTests`, `PlayerDataCacheEngineTests`, `DMPlayerExamineEngineTests`; g1, g2: `MimicryLoadoutEngineTests`,
  `MimicryObservationEngineTests`, `MimicryDescriptionEngineTests`, `MimicryPotencyEngineTests`, `MimicryTraitPotencyEngineTests`, `PlayerTechniqueLearningEngineTests`; h1, h2: `PlayerBeastAbilityEngineTests`,
  `IncubatorCompleteJobEngineTests`, `CapstoneProofEngineTests`, `TankEnmityEngineTests`; i1 to i3: `DroidInventoryEngineTests`, `MigrationDroidEngineTests`, `DroidCombatWeaponEngineTests`, the five droid migration tests,
  `DroidStatusImmunityEngineTests`, `DroidWeaponPairEngineTests`, `StationReturnInventorySpaceEngineTests`, `TankEnmityEngineTests`.
- **CombatSim** (seed 1, 50 runs) byte-identical on c and e2: it reads `CombatRules`, `StatusEffectRules` and `StatRules` only, so a difference means a rule changed.
- **Client evidence** (AGENTS.md; the Combat map took none and P5-7d took it anyway). One NWN client at the base and at the head, same module, haks and steps, per PR:
  c: the Perks window text of a Force perk (affinity multipliers) and a perk with a stat bonus on the character sheet; a Force heal and a damage ability, magnitude and hit messages. d: buy a perk (feat and hot bar),
  refund it (feats and status effects removed), relog (feats restored). e1: the Skills window ranks and limits, Distribute RP XP. e2: kill an enemy for skill XP, a rank-up message, the skill-cap warning, an XP debt
  repayment message. f1: the AFK tag in the player list and over the head, a known name, a tell between two players (the two-client recipe). f2: `/name` set, forget and the validation messages. g1: the Techniques
  window equip, unequip, loadout apply, relog restore. g2: an NPC technique used in view, then learned after the kill (message and sound). h1: call a beast, the incubator (use, egg creation), DNA extraction. h2: beast combat
  round, death and rest, the stables terminal, an item given to a beast. i1: give and take droid items, an equip refusal, the saved snapshot. i2: summon a droid (appearance, equipment, greeting). i3: droid death and despawn,
  the assembly terminal and the appearance editor. Per phase the message kinds and counts match; random-dependent numbers are compared by range.

## Order

One pull request per row. Rows of different chains may be open at once and merge one at a time (they share `ServiceRegistrations.cs`, `GameServices.<Domain>.cs`, `ConvertedServices.json`, `RemovedFacades.json` and the
Phase 5 baselines; each later PR rebases and regenerates the baselines). Lines are member lines moved. In flight: P5-7 (the Combat services: next `P5-7e1`, `P5-7g`, `P5-7i1`, with `P5-7j` the recorder), P5-8 (Creatures: `P5-8e`
to `P5-8h` are queued), P5-16 (`UsePerkFeat`, the character sheet). Files other running items own: P5-7 `CombatAbilityHitService.cs` (split in P5-7g) and `CombatService.cs`; P5-8 `AIService.cs`,
`CompanionControlService.cs`, `StealthService.cs` (P5-8e); P5-16 `UsePerkFeat.cs`, the character sheet view models.

| PR | Contents | Lines | Other files touched | Waits for |
|---|---|---:|---|---|
| P5-10a | This guide; the Phase 5 and Status rows link it | 0 | `Readmes` | - |
| P5-10b1 | Test groundwork for Perk, Skill, PlayerName: `CharacterCompanionsSource`, bench accessors, split doubles, the readers repointed, the new transcripts and fixtures. No production code | 0 | tests only | P5-10a |
| P5-10b2 | The same for Mimicry, Beast, Droid | 0 | tests only | P5-10a |
| P5-10c | PK-3 `PerkForceAffinityService`, PK-2 `PerkStatBonusService` (new boot handler, `StatRules`); `PerkService` 1,486 -> about 1,130, `Lazy<IStatService>` gone | 348 | `StatService.cs`, `CombatQueriesService.cs`, `CombatAbilityHitService.cs`, `CombatAbilityMagnitudeService.cs`, `AbilityDamageStageService.cs`, 3 content files | b1; P5-7g for `CombatAbilityHitService.cs` |
| P5-10d | PK-4 `PerkGrantedFeatService`, PK-5 `PerkRefundService`, ports `IFeatHotBar` and `IStatDrivenTrackerRefresh`; `PerkService` off `LargeTypes.txt` (about 640), `Lazy<ICombatService>` gone | 508 | `AbilityActivationService.cs`, `BeastMasteryService.cs`, `DroidService.cs` (one seam line each), `StealthService.cs`, `IQuickCastService.cs`, `ICombatService.cs`, `UsePerkFeat.cs`, 7 content files | c |
| P5-10e1 | SK-2 `SkillRankService`, port `ITwinPistolPairQuery` | 203 | `CharacterSheetAttributesTab.cs`, `CharacterSheetViewModel.cs`, `SkillsViewModel.cs`, `DistributeRPXPViewModel.cs`, `ITwinPistolEquipmentService.cs` | b1; P5-16h sheet PRs |
| P5-10e2 | SK-3 `SkillXPService`, `GiveSkillXP` cut; `SkillService` off `LargeTypes.txt` and `LongMethods.txt`, `Lazy<IStatService>` gone | 347 | 9 domain services, 8 static helpers, 7 content files | e1 |
| P5-10f1 | PN-3 `PlayerNameOverrideService`, ports `IPortraitTellGuard` and `IDisplayNameLookup`; `PlayerNameService` off `LargeTypes.txt`; carried `AfkPlayers` re-keyed | 430 | `CommunicationService.cs`, `PlayerListService.cs`, `ChatPortraitTellsService.cs`, `ConversationService.cs`, `PlayerFeedbackService.cs`, `DisguiseService.cs`, `PlayerDescriptorService.cs`, 5 content files | b1 |
| P5-10f2 | PN-2 `PlayerKnownNameService` | 182 | `DisguiseService.cs`, `PlayerDescriptorService.cs`, `PlayerIntroductionService.cs`, 2 content files | f1 |
| P5-10g1 | MI-3 `MimicryLoadoutService`; `MimicryService` off `LargeTypes.txt` and `ServicesPerType.txt` (11 -> 9) | 667 | `AbilityActivationService.cs`, 2 content files | b2 |
| P5-10g2 | MI-2 `MimicryWitnessService` (new boot handler, carried tables re-keyed); `Lazy<ICombatPointService>` and one `Lazy<IStatService>` gone | 392 | `UsePerkFeat.cs`, `DMPlayerChatCommand.cs` | g1; e2 (so the part is born on `ISkillXPService`) |
| P5-10h1 | BM-2 `BeastSpawnService`, BM-4 `BeastIncubationService`; `BeastMasteryService` off `LargeTypes.txt` and `ServicesPerType.txt` (14 -> 10), `IncubationFieldNoteService` `Lazy<>` plain | 395 | 7 content files | b2; d |
| P5-10h2 | BM-3 `BeastEventService` | 243 | none | h1 |
| P5-10i1 | DR-3 `DroidInventoryService` | 270 | 1 content file | b2; the other splits (owner decision 5); d |
| P5-10i2 | DR-6 `DroidRestoreService`, DR-2 `DroidSpawnService`, `SpawnDroid` cut; `DroidService` off `ServicesPerType.txt` (14 -> 6; 1 after i3) and `LongMethods.txt`; `DroidEquipmentDiagnosticsService` `Lazy<>` plain | 305 | 1 content file | i1 |
| P5-10i3 | DR-5 `DroidDespawnService`, DR-4 `DroidEventService`; close-out: README, Status, this guide's Progress | 230 | none | i2 |

**Parallel groups.** Chains Perk (c, d), Skill (e1, e2) and PlayerName (f1, f2) touch no common service file and can run together (three agents, the Phase 5 limit); Mimicry (g1, g2) and Beast (h1, h2) can start when b2 is
merged and a slot is free. g2 waits for e2 (both edit `MimicryService.cs`'s `GiveSkillXP` line), h1 for d (both edit `BeastMasteryService.cs`'s `SyncGrantedFeats` seam). Droid (i1 to i3) is last by owner decision 5 and
after d. It calls `IAIService.SetAIProfile`, which stays on the AI core, so P5-8 does not conflict. Two rows never work on the same files at once: c waits for P5-7g
(or, if P5-7g slips, edits `CombatAbilityHitService.cs` in two lines and P5-7g rebases); e1 waits for the two P5-16h character sheet PRs (or rebases onto them: scripted receiver edits); d and g2 edit `UsePerkFeat.cs` by script, as
P5-8f, P5-7g and P5-7j do, on different lines.

### Baseline lines each PR changes

| File | c | d | e1 | e2 | f1 | f2 | g1 | g2 | h1 | h2 | i1 | i2 | i3 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `LargeTypes.txt` | `PerkService` about 1,130 | line gone | `SkillService` about 985 (line gone if measured at 1,000 or less) | `SkillService` line gone if e1 left it over 1,000 (e1 projects about 985; e2 about 620) | `PlayerNameService` line gone | - | `MimicryService` line gone (about 650) | - | `BeastMasteryService` line gone (about 890) | - | `DroidService` line gone (about 920) | - | - |
| `ServicesPerType.txt` | - | - | - | - | - | - | `MimicryService` 11 -> gone (9) | - | `BeastMasteryService` 14 -> 10, line gone | - | - | `DroidService` 14 -> 6, line gone | - |
| `LongMethods.txt` | - | - | - | `GiveSkillXP` gone | - | - | - | - | - | - | - | `SpawnDroid` gone | - |
| `DeferredResolution.txt` | `PerkService.cs` Character line gone | `PerkService.cs` Combat line gone; `StealthService.cs` Character 5 -> 4 | - | `SkillService.cs` line gone | `ChatPortraitTellsService.cs` line gone; `PlayerListService.cs` Character 2 -> 1 | - | - | `MimicryService.cs` Combat line gone | `IncubationFieldNoteService.cs` line gone | - | - | `DroidEquipmentDiagnosticsService.cs` line gone | - |
| `UpwardEdges.txt` | - | `PerkService.cs` Abilities and Combat gone | `SkillService.cs` Combat gone | - | `PlayerNameService.cs` Social 4 -> 2; `ConversationService.cs`, `PlayerFeedbackService.cs` Character shrink | `PlayerNameService.cs` Social gone | - | - | - | - | - | - | - |
| `LogicToContent.txt` | - | - | - | - | - | - | - | - | - | `BeastMasteryService.cs` two lines re-keyed to `BeastEventService.cs` | - | - | `DroidService.cs` line re-keyed to `DroidEventService.cs` |
| `StaticLookups.txt` | - | - | - | - | - | - | - | - | `BeastMasteryServiceTests.cs` follows its bench | - | - | - | - |

`DomainCycles.txt`, `StaticState.txt` and `SinkWork.txt` have no lines for these files. `CrossDomainEdges.txt` and `EdgesIntoCoreAndPlatform.txt` change only in member counts (revert them as P5-5g did). `DebtReport.md` is
regenerated with each. The sizes after the first PR of a chain are projections from the member counts; the pull request records the measured ones. Handler snapshot files change as in "State, handlers and reload".
Also each PR: `ServiceRegistrations.cs`, `GameServices.<Domain>.cs`, `ConvertedServices.json`, `RemovedFacades.json`, `HandlerRenames.txt`, `ScriptHandlerOrder.txt`, `ClockSeamFiles.txt` where a part reads the clock,
`PlayerMessageAudit.json` rows.

## Conflicts

**With in-flight work.**

- **P5-7 (Combat).** No P5-7 PR edits the six files (the Combat guide says so: `RefreshStatDrivenTrackerEffects` stays on `ICombatService`, and Beast and Droid call only `BeginSetupAttribution`, which P5-7j keeps on the recorder
  stay-part; `MimicryService`'s `Lazy<ICombatPointService>` is untouched). P5-10 edits three Combat-owned files: `CombatAbilityHitService.cs` in c (one call and one constructor type; P5-7g moves that call into
  `CombatAbilityHitAdjustmentService`, so c either follows P5-7g and edits the new part, or goes first and P5-7g rebases), `ICombatService.cs` in d (one inherited interface), and `ITwinPistolEquipmentService.cs`
  in e1 (one inherited interface). If P5-7j moves `BeginSetupAttribution`, P5-7j edits the single call line in `BeastSpawnService` and `DroidRestoreService`.
- **P5-8 (Creatures).** `AIService`, `CompanionControlService`, `SpawnService` call only `IPerkService.GetPerkLevel` (stay-part) and `IBeastMasteryService`/`IDroidService` through `ICompanionLookup`, so none of their
  parts changes. `StealthService.cs` (P5-8e edits it) loses a `Lazy<IPerkService>` in d, one parameter. `UsePerkFeat.cs` (D2 of the Creatures guide, nine sites by script) is edited by d and g2 by script on other lines.
- **P5-16.** Content types named there that P5-10 touches by scripted receiver edits only: `UsePerkFeat` (d, g2), `CharacterSheetAttributesTab`, `CharacterSheetViewModel`, `PerkListBuilder` (c, e1), `DistributeRPXPViewModel` (e1, e2).
  Native types named there (`ResolveAttackRoll`, `GetDamageRoll`) read `GetWeaponSkill`, which does not move.
- **P5-9.** `ItemService` reads `GetPerkDetails` and `GetCreatureSkillRank` (stay-parts) and holds `Lazy<ISkillService>` (kept). No overlap.
- **`feature/space-revamp`.** Its port onto `DroidService` (lists in #3329 and #3338) lands on the stay-part and, for spawn and inventory code, on `DroidSpawnService`, `DroidRestoreService`, `DroidInventoryService`; d
  edits one seam line and one constructor type in `DroidService.cs`. The split PRs record where each piece went, as owner decision 5 requires if the branch has not landed.

**With master.** Commits to the six pre-move files (the `Service/` statics; `Perk`, `Skill`, `PlayerName`, `Mimicry`, `BeastMastery`, `Droid`, one `.cs` each), `origin/master` at `db37a6ec43`:

| Master file (lines) | Since 2026-10-10 | Since 2026-11-01 | Last |
|---|---:|---:|---|
| `Service/Perk.cs` (1,420) | 3 | 0 | 2026-10-14, `f926b6b2bd`, apply persistent targeting preferences to normal ability buttons |
| `Service/Skill.cs` (602, with `Service/SkillService/`) | 6 | 1 | 2026-11-19, `3fd4bdd4ef`, warn once per login when a skill is held at its rank limit (#3499) |
| `Service/PlayerName.cs` (932) | 14 | 1 | 2026-11-07, `1d2c10a6ff`, stop reusing connection slots right away |
| `Service/Mimicry.cs` (1,306) | 8 | 0 | 2026-10-17, `f113778438`, fix Overclocked Analyzer potency for passive techniques |
| `Service/BeastMastery.cs` (1,002) | 7 | 1 | 2026-11-03, `60ab60f377`, beast revamp: incubator rebuild, field notes |
| `Service/Droid.cs` (1,296) | 21 | 3 | 2026-11-06, `3336192446`, remove saber skills from droid parts (#3253) |

None of them has a commit after the last sync (`a184c9ef2`); the 18 master commits since it touch none of the six (#3511 changed `SkillsViewModel.cs` and a skill test). The commit counts are commit-file pairs; one
commit can touch several files. The pre-move `Service/` paths are the ones to diff; the `Domain/` files have no master history. `DroidService` has the busiest history (21), then `PlayerNameService` (14).

## Master porting

From the first PR of each chain, each sync ports master's edits to a moved member by the range tables of this guide (member -> part): the old statics map by member name (`Perk.cs`, `PerkService/`, `Skill.cs`, `PlayerName.cs`,
`Mimicry.cs`, `BeastMastery.cs`, `Droid.cs`). Parts with little headroom: none; the largest is `MimicryLoadoutService` at about 715 (285 under 1,000).

Members whose signature or call text changes:

- **Property name only:** every row of "Callers". `RemovedFacades.json` lists the owners; `-Reverse` maps master's text back.
- **Private members that become public** (names and bodies unchanged): `PlayerNameService.ResolveDisplayName`, `BuildDisplayNameWithDescriptor`, `BuildStaffDisplayName`, `GetActiveDisguise`, `GetDisplayDescriptor`, `GetIdentityKey`,
  `ShouldScrambleAccountName`, `EnsureUnknownDisplayName`, `GetUnknownDisplayNameByPlayerId`, `ApplyNameOverride`; `DroidService`'s six constants. New members: `PerkService.EnumerateAllPerks`, `GetPerkMaxLevel`,
  `GetPerksWithSkillRequirement`; `MimicryService.TryGetTechniqueForNpcFeat`, `EnumerateTechniques`; `PlayerNameService.ForgetKnownNames`, `ForgetDescriptorSetting`; `DroidService.GetPersonality`; the three boot handlers.
- **Changed bodies (a master edit to these ports by hand):** `PerkService.CachePerks` (two lines out), `MimicryService.CacheData` (four lines out), `PerkService.AggregateStatAdjustment` (one line), `SkillService.GiveSkillXP`
  (the cut) and `DroidService.SpawnDroid` (the cut): a master edit inside either body ports to the step that now holds it.
- **Nested types** `StatBonusGroup` and `TargetedStatBonusGroup` move into `PerkStatBonusService`; a master change to them ports to that file.
- `feature/space-revamp`'s `DroidService` calls port by the range table (above).

## Open questions

Each needs the coordinator's or owner's decision; the recommendation is the guide's text.

1. **Master freeze?** None (recommended; D10). Three of the six files have one November commit each, `Droid.cs` three, and none has a commit since the sync; per-file windows as in P5-7 would cost more than the edits they prevent. A master edit
   inside a moved range is ported by hand with a Port log row.
2. **Do the upward edges go in P5-10?** Yes (recommended): Appendix C names P5-10 for Character -> Abilities, Character -> Social and the display-name lookup, and the other two (Perk -> Combat, Skill -> Combat) cost one interface
   line each. Alternative: leave all six rows for P5-15 and keep the `UpwardEdges.txt` lines (about 14 members).
3. **`GetWeaponSkill`: stay on `SkillService` with the port (D9, recommended) or its own part?** Its own part (about 70 lines, `ISkillWeaponService`) would remove the `Lazy<ISkillService>` from `TwinPistolEquipmentService` and
   `ItemService`, at the cost of 19 call-site edits (5 native, among them the P5-16 attack roll types). Recommendation: stay now, take the two `Lazy<>` in P5-15.
4. **Perk cache rebuild:** two new `Rerun` boot handlers (D5, recommended) or a state core service under `PerkService` that holds the stat-bonus tables and the feat-cache flag and is filled by `CachePerks`. The core keeps
   one handler and adds one service and one interface; the handlers cost a handler-order line and the equivalence fixture. Recommendation: handlers.
5. **`StatRules.AggregateStatAdjustment` in `PerkStatBonusService` (D6)?** Recommended: a one-line change to the seam body (the pure rule `IStatService` already forwards to). Alternative: keep `IStatService` and a `Lazy<>` on the part.
6. **Droid in six parts.** Recommended: the split in "Parts" (every part at or under 7 services, `SpawnDroid` cut across `DroidSpawnService` and `DroidRestoreService`). Alternative: ask an exemption for a 14-service `DroidSpawnService`
   (against target 2). Order stays last (owner decision 5); if `feature/space-revamp` has not landed by then, the PRs go ahead and record where each piece went.
7. **Client evidence for code-move PRs.** Required by AGENTS.md for any change with an in-game surface unless the owner waives it; the list per PR is in "Tests" (precedent: P5-7d took it although its map said none). Recommendation: take it, one
   session per PR from the list, and report counts per phase. A waiver for the Beast events and Droid events PRs (h2, i3), which only re-home handlers whose behaviour the engine tests already pin, is the cheapest to give.
8. **One test-groundwork PR or two?** Two (recommended, b1 and b2): the Character three and the Abilities and Companions three share no test file except `ItemServiceTests`, and b1 unblocks the first three chains.

## Risks

- **Services at 10.** `TrapsService`, `CommunicationService` and `PerformanceAudienceService` are at 10 by a source scan (the baseline lists only types over 10); each stays at 10 only because of two placements: the XP formulas with `GiveSkillXP` (Skill) and the chat display names with the overrides (PlayerName). A reviewer should confirm by regenerating `ServicesPerType.txt` in e2 and f1 that none appears.
- **Boot order.** The three new handlers must run in `Rerun`. The classification snapshot and the reload check cover a missing attribute; a wrong position in `ScriptHandlerOrder.txt` would let a later `OnModuleCacheBefore`
  handler read an empty stat-bonus table (none reads it at cache time today).
- **Handler sharing between PRs.** c and d both insert lines after `PerkService.CacheData` (41); each PR lists its lines and the second rebases.
- **`SpawnDroid` and `GiveSkillXP` cuts** are the two bodies that are not pure moves. The transcripts of P5-10b exist to prove the order; the token comparison reports them as the only intended differences.
- **Carried state keys.** A carried field that changes class needs `[ReloadCarriedFrom]` or a reload from the previous build loses it (three fields). `ReloadCarryHistoryTests` fails without it.
- **Hot-path cost.** `GetPerkLevel` and the stat-bonus reads run on every stat recalculation; the split adds no call on those paths (`StatService` takes `IPerkStatBonusService` directly). `GiveSkillXP` adds four private calls.
  The force-affinity reads add none. No `CombatLoad` run is needed.

## Progress

P5-10a: this guide (2026-11-23). Next: P5-10b1 and P5-10b2.
