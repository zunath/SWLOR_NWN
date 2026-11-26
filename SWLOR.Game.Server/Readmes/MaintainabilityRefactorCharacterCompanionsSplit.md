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

Reviewed by the coordinator; change only on a finding in review. The questions the first draft left open are settled at the end.

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
  same for the witness sweep (`CacheData` schedules it today). `ResetPerkFeatCache` is likely a no-op in production: a hot reload builds new
  instances whose flag starts false, and the flag only matters if a feat read runs between the two cache events. The PR keeps it (verbatim
  behaviour) unless P5-10b's reload check shows the instance is always new, in which case the PR deletes it and says why.
- **D6. `PerkStatBonusService` uses `StatRules.AggregateStatAdjustment` directly.** `PerkService.AggregateStatAdjustment` is a
  one-line seam over `IStatService.AggregateStatAdjustment`, which is `StatRules.AggregateStatAdjustment`
  (`StatService.cs` line 100). The part calls the static rule in the seam's body (four uses, one line changed). This is what
  lets the part sit under `StatService`, which reads it, without a `Lazy<IStatService>`.
- **D7. Upward edges that P5-10 owns are inverted where the part that needs them is built,** with Character-owned (or
  Foundation-owned) one-member ports implemented by the lower-level interface extending the port, as `IBeastCorpseInfo` and
  `IPlayerListRefresh` do. They are in "The upward references". No `Lazy<>`, `Func<>` or late lookup is added by a split, with one
  exception that moves rather than grows: the port `IPlayerListRefresh` makes `PN-3 -> PlayerListService -> PN-3` a cycle, so `PlayerListService`
  swaps its `Lazy<IPlayerNameService>` for a plain PN-1 parameter and a new `Lazy<IPlayerNameOverrideService>` (count unchanged at 2). P5-15
  removes it by making `QueueRefresh` a notice that `PlayerListService` subscribes to.
- **D8. The two long methods are cut into named steps in the same pull request that moves them.** `GiveSkillXP` (239) becomes a
  main method of about 100 lines plus `ApplyXPBonuses`, `RepayXPDebt`, `ClearXPAtRankCeiling` and `AwardRanks`, with a private
  `RankUpOutcome` carrying the locals across (the rank-up flag, XP spent on ranks, the modified and decayed skill lists, the
  running rank total). `SpawnDroid` (272) becomes a main method of about 100 lines in `DroidSpawnService` and three calls into
  `DroidRestoreService` (inventory restore, appearance restore, the closing steps). The order of every engine call, database
  write and random draw is unchanged; the ordered transcripts of P5-10b pin it.
- **D9. `GetWeaponSkill` stays on `SkillService`** (19 non-test call sites, 5 of them native). It needs the twin-pistol pair test, so
  `SkillService` takes Combat-owned `ITwinPistolEquipmentService` today. A Character-owned `ITwinPistolPairQuery` port (one member,
  `IsActivePair`) replaces it. `TwinPistolEquipmentService` and `ItemService` keep their `Lazy<ISkillService>` (the path
  `SkillService -> TwinPistolEquipmentService` stays). P5-15 records the other option (a separate weapon-skill part, about 70 lines,
  19 call-site edits) when it breaks those two `Lazy<>`.
- **D11. Order: top layer first.** A chain that pulled a middle layer out first would close a constructor cycle (the stay-part still calls
  the part that left, and the part takes the stay-part). Each chain therefore moves its top layer first, as the Combat map did, and the stay-part
  keeps the middle layer's members as public until their own PR: Skill SK-3 then SK-2, PlayerName PN-2 then PN-3, Droid DR-4 and DR-5, then
  DR-6 and DR-2, then DR-3; Perk (c, d) and Mimicry and Beast are already top-down or flat. Beast h1 runs before Perk d so
  `BeastMasteryService` never carries both `IPerkService` and `IPerkGrantedFeatService` (that would be 15 services).
- **D10. No master freeze is needed** (settled): none of the six master files changed since the last sync
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

`SpawnDroid` is 277 lines with its doc comment (272 by the guard), `LoadDroidPartItemPropertyDetails` 130. The class has a protected virtual `ReadGeneratedAmmunitionFlags` for the tests (979-992), which goes with the part
that calls it (inventory sync); there is no parameterless constructor.

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
- These are the final dependencies. The PRs move the top layer first (D11), so the stay-part holds the middle layer's members, public, until their PR.

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
  six identity seams 5 each (391). New or promoted public members: `ForgetKnownNames`, `ForgetDescriptorSetting`, `GetKnownNames`, `IsDisguiseIdentityKey` (a seventh seam, 5 lines) and the identity seams.
- **PN-2:** `SearchKnownPlayerIdsByName` 27, `ValidateKnownName` 10, `ValidateKnownNameInput` 7, `ValidateKnownNameAssignment` 17, `SetKnownName` 18, `ValidateKnownNameTarget` 10,
  `ContainsColorToken` 7, `ValidateKnownNameIsUnique` 22, `ForgetKnownName` 18, `DeleteKnownNameReferences` 36 (182).
- **PN-3:** 3 fields 5 (`AfkPlayers`, `_afkMovementCheck`, the staff marker; the tell variable stays on PN-1); `ApplyNameOverridesOnEnter` 23, `ClearAfkOnExit` 7, `IsAfk` 1, `OnCarriedStateAdopted` 18,
  `ToggleAfk` 14, `RemoveAfk` 13, `CheckAfkMovement` 18, the two suffix reads 6, `PreventAmbiguousTellTargets` 46, `GetChatDisplayName` 4,
  `SendChatMessageWithChatNameOverride` 13, `GetColoredChatDisplayName` 10, `RefreshNameOverridesForObserver` 27, `RefreshNameOverridesForPlayer` 5,
  `ApplyNameOverridesForPlayer` 38, `ApplyNameOverridesForDMObserver` 13, `ApplyTrueNameOverride` 14, `ApplyNameOverride` 21, `CanApplyChatNameOverride` 9,
  `ApplyChatNameOverride` 13, `RestoreNameOverride` 20, `ResolveChatDisplayName` 26, `CountDisplayNameMatches` 19, seams `ScheduleRepeating`, `EnsureUnknownDisplayName` 5 (430).
- **MI-1:** 7 fields 19; `CacheData` 68 (4 lines lighter), `GetResistanceBonus` 25, `GetTechniqueIcon` 8, `CarryTechniqueDescriptions` 13, `AdoptTechniqueDescriptions` 17,
  `GetTechniqueDescription` 8, `GetTechniqueDetail` 7, `IsTechnique` 7, `TechniqueCount` 4, seams 2 each (193). New: `TryGetTechnique`, `GetTechnique`, `TryGetTechniqueForNpcFeat`, `EnumerateTechniques`.
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
  moves with its only reader, `IsSkillCapContributingSkill`. `SkillCap` is already a public constant. `IsSkillCapContributingSkill` runs in a per-skill loop
  (`GetTotalContributingSkillRanks`), so it must not read `GetAllContributingSkills()` (a `ToDictionary` copy): SK-1 gets a no-copy member
  `TryIsContributingSkill(SkillType, out bool contributes)` that returns false while the table is empty, and SK-2 then uses its static fallback set, as the old code did.
- Until e2, `GetDecayCandidates` and `ShouldDecaySkillRank` (private today) are public on `SkillService` for SK-3; e2 moves both to SK-2 and SK-3 changes receiver (4 sites in `GiveSkillXP`: `GetRankCeiling`, `GetDecayCandidates` twice, `ShouldDecaySkillRank`).
- The SK-3 constructor builds `SkillXPMessageQueue` with a closure that calls `GetSkillDetails`; it reads it through `ISkillService`.
- `AddMissingSkills` (a login handler over the catalog and the database) stays on SK-1.

**PlayerName.**
- State stays on PN-1: `KnownNamesByObserverId`, `_knownNamesWriteVersion`, `ShowDescriptorsForNamedPlayersByObserverId` and `_scriptContextHooked` (the descriptor
  script-context hook is registered on first fill). Two other parts write to them: `DeleteKnownNameReferences` (PN-2) removes observer entries, and
  `RefreshNameOverridesForObserver` (PN-3) removes a descriptor setting. They call two new PN-1 members, `ForgetKnownNames(observerId)` and
  `ForgetDescriptorSetting(observerId)`, which hold the one `Remove` line each.
- PN-1 is the only holder of the two `Lazy<>`. The seven one-line seams over them (`IsDisguiseIdentityKey` below, and `GetActiveDisguise`, `GetDisplayDescriptor`, `GetIdentityKey`,
  `ShouldScrambleAccountName`, `EnsureUnknownDisplayName`, `GetUnknownDisplayNameByPlayerId`) become public on PN-1 and PN-2 and PN-3 call them there. Without that, each part
  would hold its own `Lazy<>` (a scan finds all of them needed): 2 become about 5. PN-1's private helpers PN-3 calls (`ResolveDisplayName`, `BuildDisplayNameWithDescriptor`,
  `BuildStaffDisplayName`) become public too.
- Chat display names (`GetChatDisplayName`, `GetColoredChatDisplayName`, `ResolveChatDisplayName`) go to PN-3 with the chat override code that uses them. That leaves
  `CommunicationService` (10 services today) on one part, `IPlayerNameOverrideService`, in place of `IPlayerNameService`.
- AFK stays with the overrides: the `[AFK]` suffix is read by every override and written by `ToggleAfk` and the movement check, which refresh the overrides. `AfkPlayers`
  keeps `[ReloadState(Carried)]` and gets `[ReloadCarriedFrom]` for its old class name; `OnCarriedStateAdopted` (restart the check, reapply every override) moves with it.
- `ApplyNameOverride` (private today) is public on `PlayerNameService` from f1, because `SetKnownName` and `ForgetKnownName` (PN-2) call it at lines 512, 566 and 573; it moves
  to PN-3 in f2 and PN-2 changes receiver. `CountDisplayNameMatches` moves with its only caller, the tell handler.
- `GetKnownNames(observer, createIfMissing)` (private, 648) is called from PN-2 at the search, `SetKnownName` and `ForgetKnownName`, and from PN-1's own reads: it becomes public on PN-1.
- `SearchKnownPlayerIdsByName` also calls `_disguise.Value.IsDisguiseIdentityKey` (line 393), which needs a seventh public seam on PN-1, `IsDisguiseIdentityKey`; PN-1
  stays the only holder of the two `Lazy<>`.
- `DirectTellVariable` stays on `PlayerNameService` (a public constant read by `PlayerListService.cs:241,248` and `DMPlayerSearchViewModel.cs:528,535`); the tell handler in PN-3 reads it there.

**Mimicry.**
- `_techniques` has ten keyed reads outside `CacheData` (lines 284, 511, 657, 687, 813, 988, 1072, 1080, 1189, 1216) and loops in `GrantAllTechniques` (589) and the cache. The existing
  `GetTechniqueDetail` and `IsTechnique` normalise their key through `_quickCast.GetOriginalFeat` (1249, 1257), which changes the key, so they cannot stand in for the raw reads. MI-1 gets
  `TryGetTechnique(feat, out detail)` (the raw table read, no normalisation), `GetTechnique(feat)` (an indexer-style read that throws on a missing key, as `_techniques[feat]` does in `ApplyLoadout` at line 1080, so that behaviour is identical), `TryGetTechniqueForNpcFeat(npcFeat, out technique, out detail)` (returns both the technique feat and its
  detail, so `OnCreatureAbilityUsed` does one call) and `EnumerateTechniques()` (the loops); MI-2 and MI-3 read through them. In g1, `ApplyLoadout` (MI-3) reads through `GetTechnique`, so a missing key throws exactly as the indexer does today.
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
- `ApplyStats` (private, 497) becomes public on BM-1 in h1, where `SpawnBeast` (BM-2) calls it; `GiveBeastXP` calls it in place.
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
- The protected virtual `ReadGeneratedAmmunitionFlags` (979-992) goes to DR-3; `DroidServiceTests`, `DroidServiceWorldTests` and `DroidWorld` subclass or construct `DroidService`
  today (`ItemServiceTests` only reads `DroidService.DroidResref`; Tests).
- Promoted private members (names and bodies unchanged). Until i3 they are public on `DroidService`: `AcquireDroidItem`, `IsStoredItem` and `ReturnRejectedItems` in i1
  (called by `DroidOnDisturbed`, `DespawnDroid`, `DroidOnDeath`), `CanGiveItemToDroid` and `GetDroidItemId` in i2 (called by `RestoreDroidItem` and the inventory restore); i3 moves
  all five to DR-3 and the callers change receiver. `ApplyStats` (Beast) is public on BM-1 from h1.
- `constructedDroid` is a class, so it passes by reference. `SpawnDroid` loads it once (`LoadConstructedDroid(controller)`, line 595) and hands the same object to
  `IDroidRestoreService.RestoreInventory(droid, player, controller, constructed)`, then `RestoreAppearance(droid, controller, constructed)`, then `FinishSpawn(droid, controller)`;
  each step mutates it and saves through `SaveConstructedDroid` at the places the old code did (lines 647, 737-762), so the saved data and the read-after-write order are unchanged.

## The upward references and how each goes

`UpwardEdges.txt` has four lines for these files (`PerkService.cs` Character -> Abilities 4 and Character -> Combat 3, `PlayerNameService.cs` Character -> Social 4,
`SkillService.cs` Character -> Combat 3), and `ConversationService.cs` and `PlayerFeedbackService.cs` (Foundation -> Character, 3 each) name `IPlayerNameService` for
`GetDisplayName`. Appendix C names P5-10 for Character -> Abilities (Perk), Character -> Social (PlayerName) and the Foundation-owned name lookup. All are inverted with a one-member port; `IPlayerListRefresh` (Character-owned, extended by
`IPlayerListService`, registered as an alias) already exists, and the new ports are registered the same way.

| Row | Members | Mechanism | Owner of the interface | PR |
|---|---|---|---|---|
| Character -> Abilities: Perk `IQuickCastService` (`KnowsFeat`, `RemoveFeat`, `ApplyPreferences`) | 4 | port `IFeatHotBar`; `IQuickCastService : IFeatHotBar`; registered with a forwarding lambda | Character | d |
| Character -> Combat: Perk `Lazy<ICombatService>` (`RefreshStatDrivenTrackerEffects`) | 3 | port `IStatDrivenTrackerRefresh`; `ICombatService : IStatDrivenTrackerRefresh` (the member exists; one interface line, no class change) | Character | d |
| Character -> Combat: Skill `ITwinPistolEquipmentService` (`IsActivePair`) | 3 | port `ITwinPistolPairQuery`; `ITwinPistolEquipmentService : ITwinPistolPairQuery` | Character | e2 |
| Character -> Social: PlayerName `IPlayerListService` (`QueueRefresh`) | 6 | the existing `IPlayerListRefresh` | Character | f1, f2 |
| Character -> Social: PlayerName `IChatPortraitTellsService` (`RefuseStalePortraitTell`) | 3 | port `IPortraitTellGuard`; `IChatPortraitTellsService : IPortraitTellGuard` | Character | f2 |
| Foundation -> Character: `ConversationService`, `PlayerFeedbackService` -> `IPlayerNameService.GetDisplayName` | 3 + 3 | port `IDisplayNameLookup`; `IPlayerNameService : IDisplayNameLookup`; the Foundation services take the port | Foundation | f1 |
| Character -> Companions (Perk -> Beast and Droid) | 11 | done in P5-1g (`ICompanionLookup`); P5-10 keeps the lookup | Character | none |

The two files named after the ports (`ConversationService.cs`, `PlayerFeedbackService.cs`) lose their `IPlayerNameService` lines from `UpwardEdges.txt` only if that is
the last Character type they name; the PR records what the regenerated file shows. `IBeastCorpseInfo` (Items-owned) stays on the Beast stay-part. Logic that names content
(`LogicToContent.txt`) is not removed here; it moves with the code (Baselines). No `Lazy<>`, `Func<>` or late lookup is added.

## Holders

Holders are services whose constructors take the interface, counted as the guard counts them (`GameServices` properties reached count once per service).
No holder gains a `Lazy<>` except `PlayerListService`, which swaps its one `Lazy<IPlayerNameService>` for a plain PN-1 parameter and a new `Lazy<IPlayerNameOverrideService>` in f2 (D7; count unchanged, P5-15 removes it). The numbers that matter are the services at or near 10.

| Holder | Change | After |
|---|---|---|
| `StatService` (about 10) | c: `IPerkService` -> `IPerkStatBonusService` (it calls only `GetStatSources` and `GetStatBonus`) | same |
| `CombatQueriesService` (7) | c: keeps `IPerkService` (`IsPerkInCategory`) and gains `IPerkStatBonusService` | 8 |
| `AbilityDamageStageService` (10), `CombatAbilityHitService` (13), `CombatAbilityMagnitudeService` (3) | c: `IPerkService` -> `IPerkForceAffinityService` (affinity reads only) | same |
| `AbilityActivationService` (8) | d: `IPerkService` -> `IPerkGrantedFeatService`; e: nothing; g1: `IMimicryService` -> `IMimicryLoadoutService` (`IsTechniqueEquipped`) | same |
| `BeastSpawnService` (born in h1 on `IPerkService`), `DroidService` (14) | d: the `SyncGrantedFeats` seam reads `IPerkGrantedFeatService` (the seam line and one constructor type, in each) | same |
| `BeastMasteryService` (14) | h1 runs before d: it keeps `IPerkService` (`GetPerkLevel`) and never holds `IPerkGrantedFeatService` | 10 after h1 |
| `ResistanceService` (4) | none: `GetResistanceBonus` stays on `IMimicryService` | same |
| `MimicryService` (11) | g1: loses `IGuiService`, `IStatusEffectService`; g2: loses five more (g1 and g2 follow d and e1) | 9, then 4 |
| `TrapsService` (10), `PerformanceAudienceService` (10), `EspionageInfiltrationService` (6), `SlicingSessionService` (9), `GuildTaskXpService` (3) | e1: `ISkillService` -> `ISkillXPService` (they use `GiveSkillXP` and the XP formulas only) | same |
| `CombatPointService` (7), `LanguageService` (3), `CraftService` (5) | e1: keep `ISkillService` and gain `ISkillXPService` | 8, 4, 6 |
| `CommunicationService` (9) | f2: `IPlayerNameService` -> `IPlayerNameOverrideService` (`GetColoredChatDisplayName`, `SendChatMessageWithChatNameOverride`) | same |
| `PlayerListService` (8) | f2: keeps `IPlayerNameService`, gains `Lazy<IPlayerNameOverrideService>` (`IsAfk`; the cycle `PN-3 -> IPlayerListRefresh -> PlayerListService -> PN-3`) and its PN-1 `Lazy<>` becomes plain | 9; `Lazy<>` count stays 2 (P5-15 takes it) |
| `DisguiseService` (7), `PlayerDescriptorService` (3), `PlayerIntroductionService` (3) | f1, f2: gain the known-name and override parts | +2, +2, +1 |
| `ConversationService`, `PlayerFeedbackService` (Foundation) | f1: `IPlayerNameService` -> `IDisplayNameLookup` | same |
| `IncubationFieldNoteService` | h1: `Lazy<IBeastMasteryService>` becomes plain | `Lazy<>` -1 |
| `TintMapApplyService`, `TintMapCarryService`, `TintMapDroidOverrideService`, `TintMapItemColorService` | none: `UpdateEquippedItemSnapshot` stays on `IDroidService` | same |
| `DroidEquipmentDiagnosticsService` | i2: `Lazy<IDroidService>` becomes plain (the stay-part no longer holds the diagnostics) | `Lazy<>` -1 |
| `StealthService`, `AIService` | d: `Lazy<IPerkService>` becomes plain wherever it is still lazy (P5-8e already does it for `AIService`) | `Lazy<>` -1 each |
| `ChatPortraitTellsService` | f2: `Lazy<IPlayerNameService>` becomes plain (it reads only the stay-part) | `Lazy<>` -1 |

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
| `SkillService -> Lazy<IStatService>` | gone in e1 (SK-3 plain; the stay-part no longer calls it) | `StatService` reads SK-1 only |
| `MimicryService -> Lazy<IStatService>` | stays on MI-1 | `StatService -> ... -> ResistanceService -> MI-1`; the potency read needs the stat service |
| `MimicryService -> Lazy<IStatService>` (witness) | gone (MI-2 plain) | no service in the stat closure takes MI-2 |
| `MimicryService -> Lazy<ICombatPointService>` | gone (MI-2 plain) | `AbilityActivationService` takes MI-3 only; MI-2 is reached by content |
| `PlayerNameService -> Lazy<IDisguiseService>`, `Lazy<IPlayerDescriptorService>` | stay on PN-1 | `DisguiseService` and `PlayerDescriptorService` call PN-1 |
| `StealthService`, `AIService -> Lazy<IPerkService>` | become plain in d | PK-1 reaches two classes |
| `ChatPortraitTellsService -> Lazy<IPlayerNameService>` | becomes plain in f2 (until then the stay-part holds the chat portrait service) | it reads PN-1 only; PN-1 does not reach it |
| `PlayerListService -> Lazy<IPlayerNameService>` (PN-1) | becomes plain in f2 | PN-1 does not reach it |
| `PlayerListService -> Lazy<IPlayerNameOverrideService>` (PN-3, new) | added in f2, count stays 2; P5-15 removes it (`QueueRefresh` as a notice) | `PN-3 -> IPlayerListRefresh -> PlayerListService -> PN-3` |
| `IncubationFieldNoteService -> Lazy<IBeastMasteryService>` | becomes plain in h1 | BM-1 no longer holds the field notes |
| `DroidEquipmentDiagnosticsService -> Lazy<IDroidService>` | becomes plain in i2 | DR-1 no longer holds the diagnostics |
| `TwinPistolEquipmentService`, `ItemService -> Lazy<ISkillService>` | stay | `SkillService -> ITwinPistolPairQuery -> TwinPistolEquipmentService -> ...` (D9) |

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
  and `Mimicry.CacheData`; `tools/Refactor/HandlerRenames.txt` line 114 already maps `Mimicry.CacheData` to two services, `MimicryService` and `MimicryTraitStatsService`, so `ScheduleWitnessSweep`
  is its third part): `PerkStatBonusService.CachePerkStatBonuses` and `PerkGrantedFeatService.ResetPerkFeatCache` (after line 41), `MimicryWitnessService.ScheduleWitnessSweep`
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
  equipped technique stays equipped (MI-3); a summoned droid keeps its equipment through a reload (DR-3). Run `scripts/run-hot-reload-check.sh --engine-filter HotReloadPlayers --reloads 2` on
  every PR that moves a handler, a boot registration or runtime state, keyed to the order below: c (the stat-bonus handler), d (the feat handler and reset), e1 (`_limitWarningsSent`, `_xpMessages`, the two
  session handlers), f2 (`AfkPlayers`, three handlers, the adopter), g1 (`OnMimicrySkillDecay`, `OnPlayerLogin`), g2 (the sweep handler, the two carried tables), h1 (three handlers,
  `_incubationDayOverrides`), h2 (the 16 handler lines), i1 (17 handler lines: 13 event and terminal, 4 despawn) and i3 (6 inventory handler lines). Plus the state round trips in
  `ReloadStateCarrierTests` and `ReloadDigestTests` for the three moved carried fields.
- **Attribution scopes.** `BeginSetupAttribution` is called by `SpawnBeast` (BM-2) and the droid closing steps (DR-6); P5-7j keeps it on the recorder stay-part, so the text does not change.

## Tests

- **Source readers by name** (12 test files, 31 reads of the six files by path, plus `InstanceMethod` harness reads): `PlayerNameRecognitionTests` (16 reads of `PlayerNameService.cs`),
  `PlayerIdentityAuditTests` (2 harness reads: `GetAuditName`, `GetAuditIdentity`), `MimicryTests` (2), `DMPossessionPlayerLoopTests` (harness: `OnCreatureAbilityUsed`, `GetNearbyPlayers`, both MI-2),
  `PerkRefundStatusEffectCleanupTests` (2), `SkillXPAwardTests` (2; e1 edits it, its harness names members), `PlayerMessageAuditTests` (2, the file list at lines 53-54), `DeadPlayerXPTests`, `PerksWindowTests`, `TintMapReviewTests`
  (reads `DroidService.cs`), `CrossSkillPerkInteractionSafetyTests`, `SkillRankLimitTests`, `PerkStatBonusTests`. P5-10b adds `CharacterCompanionsSource` (the source of a member wherever it lives,
  as `CreaturesSource` does) with one group per service, and points every read at it, so no later PR edits them. The `ClockSeamSourceGuardTests` list names `Domain/Character/PerkService.cs` (the clock
  reads are `UnlockPerkForPlayer`, PK-1, and the decay handler, PK-5, so d adds `PerkRefundService.cs`) and `tools/Refactor/ClockSeamFiles.txt` line 99 follows.
- **Other files that name these files:** `PlayerMessageAudit.json` has 36 rows keyed by file (Beast 11, Skill 9, Droid 7, Mimicry 6, PlayerName 2, Perk 1); each row moves with its code and
  `PlayerMessageAuditTests` gets the part files. `StaticLookups.txt` line 317 (`BeastMasteryServiceTests.cs`, 10). `tools/Refactor/HandlerRenames.txt`.
- **Benches and doubles.** `PerkBench` (255 lines) subclasses `PerkService` and answers its seams; `PerkServiceTests` (908 lines, 45 tests), `SkillServiceTests` (979, 60), `PlayerNameServiceTests`
  (1,256, 112), `MimicryServiceTests` (1,420, 80) with `MimicryTests` (1,440, 53), `BeastMasteryServiceTests` (1,808, 105), `DroidServiceTests` (321, 23) with `DroidServiceWorldTests` (939, 45), `DroidWorld`
  (409) each subclass or build one service. P5-10b gives each a part accessor (`Catalog`, `StatBonus`, `Affinity`, `Feats`, `Refund`; `Catalog`, `Rank`, `XP`; `Names`,
  `KnownNames`, `Overrides`; `Techniques`, `Witness`, `Loadout`; `Beasts`, `Spawn`, `Events`, `Incubation`; `Droids`, `Spawn`, `Inventory`, `Events`, `Despawn`, `Restore`) that is the one instance until a part moves,
  and splits each double into partial files by the part whose code calls each seam. Each PR then moves the tests of its members into a test class per part (the stay-part keeps the shared bench).
- **Unit test splits by PR** (every old test name is kept; the stay-part's class keeps the shared bench):
  c: `PerkServiceTests` gives the stat-bonus tests (with `PerkStatBonusTests`) to `PerkStatBonusServiceTests` and the force-affinity tests to `PerkForceAffinityServiceTests`;
  d: the feat tests to `PerkGrantedFeatServiceTests`, the refund tests with `PerkRefundStatusEffectCleanupTests` (10) to `PerkRefundServiceTests`, and `PerkTriggerTests` stays;
  e1: the award tests of `SkillServiceTests` with `SkillXPAwardTests` (10) and `SkillXPMessageQueueTests` (4) to `SkillXPServiceTests`;
  e2: the rank-rule tests of `SkillServiceTests` and `SkillRankLimitTests` (46, mostly `GetRankLimit`, `GetRankCeiling`, `GetMaxDistributableXP`) to `SkillRankServiceTests`;
  f1: `PlayerNameServiceTests` (112) gives the validation and write tests to `PlayerKnownNameServiceTests`; f2: the override, AFK and tell tests to `PlayerNameOverrideServiceTests`;
  g1: `MimicryServiceTests` (80) and `MimicryTests` (53) give the slot, equip and loadout tests (with `MimicryLoadoutTests`, 10) to `MimicryLoadoutServiceTests`; g2: the witness and learn tests to `MimicryWitnessServiceTests`;
  h1: `BeastMasteryServiceTests` (105) gives the spawn and incubation tests to `BeastSpawnServiceTests` and `BeastIncubationServiceTests`; h2: the event tests to `BeastEventServiceTests`;
  i1 to i3: `DroidServiceTests` (23) and `DroidServiceWorldTests` (45) split in the PR order into `DroidEventServiceTests` and `DroidDespawnServiceTests` (i1), `DroidSpawnServiceTests` with `DroidRestoreService` (i2) and `DroidInventoryServiceTests` (i3); `DroidWorld` gains the part accessors.
- **Allocation pins and forwarding tests.** `PerkFacadeAllocationTests` (5 tests) and `SkillFacadeAllocationTests` (2) pin zero-byte reads through the facade; they are re-pointed by the call-text tool and the
  bytes stay.
- **New proof, written against the current code in P5-10b** (as P5-7b did): an ordered call-and-draw transcript of `GiveSkillXP` (bonuses, debt, rank-ups with decay, the cap warnings and the `Random` draws),
  of `SpawnDroid` (every engine call and database write, the order D8 must keep), a new engine test `SkillXPAwardDecayEngineTests` (the decay pick is `Random.Next` over the candidates, so the test leaves exactly one unlocked candidate) with two cases: a decayed skill that gates a purchased perk (`RemovePerkLevelOnSkillDecay` refunds it) and Mimicry as the decayed skill (`OnMimicrySkillDecay` enforces the slot budget); both assert `SWLOR_SKILL_LOST_BY_DECAY`, of `TryLearnTechniques` and `TryRecordWitness`, of `UseIncubator` and `CreateBeastEgg` (seeded draws), and of
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
  `PerkTrackerRegressionEngineTests`; d: `SkillXPAwardDecayEngineTests`, `PlayerPerkFeatRestorationEngineTests`, `AutomaticTargetingOwnershipEngineTests`, `PerformancePerkEngineTests`, `PerkTrackerRegressionEngineTests`; e1, e2:
  `SkillXPAwardDecayEngineTests` (new), `BareHandedAttackSkillEngineTests`, `TwinPistolCrossSkillEngineTests`, `WeaponAbilityCrossSkillTraitEngineTests`, `DroidWeaponSkillMigrationEngineTests`, `MimicryObservationEngineTests`; f1, f2: `AfkNameEngineTests`,
  `DMPossessedNameEngineTests`, `PlayerListEngineTests`, `PlayerListNamePrivacyEngineTests`, `DisguiseKnownNameEngineTests`, `PlayerDataCacheEngineTests`, `DMPlayerExamineEngineTests`; g1, g2: `SkillXPAwardDecayEngineTests` (g1), `MimicryLoadoutEngineTests`,
  `MimicryObservationEngineTests`, `MimicryDescriptionEngineTests`, `MimicryPotencyEngineTests`, `MimicryTraitPotencyEngineTests`, `PlayerTechniqueLearningEngineTests`; h1, h2: `PlayerBeastAbilityEngineTests`,
  `IncubatorCompleteJobEngineTests`, `CapstoneProofEngineTests`, `TankEnmityEngineTests`; i1 to i3: `DroidInventoryEngineTests`, `MigrationDroidEngineTests`, `DroidCombatWeaponEngineTests`, the five droid migration tests,
  `DroidStatusImmunityEngineTests`, `DroidWeaponPairEngineTests`, `StationReturnInventorySpaceEngineTests`, `TankEnmityEngineTests`.
- **CombatSim** (seed 1, 50 runs) byte-identical on c and e1: it reads `CombatRules`, `StatusEffectRules` and `StatRules` only, so a difference means a rule changed.
- **Client evidence** (AGENTS.md; the Combat map took none and P5-7d took it anyway). One NWN client at the base and at the head, same module, haks and steps, on every split PR (no waivers, the owner rule):
  c: the Perks window text of a Force perk (affinity multipliers) and a perk with a stat bonus on the character sheet; a Force heal and a damage ability, magnitude and hit messages. d: buy a perk (feat and hot bar),
  refund it (feats and status effects removed), relog (feats restored), and gain a rank at the skill cap so a random skill decays (the perk it supported is refunded: `RemovePerkLevelOnSkillDecay`). e1: kill an enemy for skill XP, a rank-up message, the skill-cap warning, an XP debt repayment message, and a rank gained at the skill cap (random decay, its message and refresh). e2: the Skills
  window ranks and limits, Distribute RP XP. f1: `/name` set, forget and the validation messages. f2: the AFK tag in the player list and over the head, a known name, a tell between two players (the two-client recipe). g1: the Techniques
  window equip, unequip, loadout apply, relog restore, and a skill decay at the cap that drops the Mimicry rank (`OnMimicrySkillDecay` unequips what no longer fits). g2: an NPC technique used in view, then learned after the kill (message and sound). h1: call a beast, the incubator (use, egg creation), DNA extraction. h2: beast combat
  round, death and rest, the stables terminal, an item given to a beast. i1: droid combat round, death and despawn, the assembly terminal and the appearance editor. i2: summon a droid (appearance,
  equipment, greeting). i3: give and take droid items, an equip refusal, the saved snapshot. Per phase the message kinds and counts match; random-dependent numbers are compared by range.

## Order

One pull request per row. Rows of different chains may be open at once and merge one at a time (they share `ServiceRegistrations.cs`, `GameServices.<Domain>.cs`, `ConvertedServices.json`, `RemovedFacades.json` and the
Phase 5 baselines; each later PR rebases and regenerates the baselines). Lines are member lines moved. Each chain moves its top layer first (D11); the stay-part keeps the middle layer's members public until their PR.
In flight: P5-7 (the Combat services: next `P5-7e1`, `P5-7e2`, `P5-7g`, `P5-7i1`, with `P5-7j` the recorder and `P5-7k` Enmity and Stealth later), P5-8 (Creatures: `P5-8e` to `P5-8h` are queued), P5-16 (`UsePerkFeat`,
the character sheet). Files other running items own: P5-7 `CombatAbilityHitService.cs` (e2, g), `AbilityDamageStageService.cs` (e1), `StealthService.cs` (k) and `CombatService.cs`; P5-8 `AIService.cs`,
`CompanionControlService.cs`, `StealthService.cs` (e); P5-16 `UsePerkFeat.cs`, the character sheet view models.

| PR | Contents | Lines | Other files touched | Waits for |
|---|---|---:|---|---|
| P5-10a | This guide; the Phase 5 and Status rows link it | 0 | `Readmes` | - |
| P5-10b1 | Test groundwork for Perk, Skill, PlayerName: `CharacterCompanionsSource`, bench accessors, split doubles, the readers repointed, the new transcripts and fixtures, the skill-decay engine test. No production code | 0 | tests only | P5-10a |
| P5-10b2 | The same for Mimicry, Beast, Droid | 0 | tests only | P5-10a |
| P5-10c | PK-3 `PerkForceAffinityService`, PK-2 `PerkStatBonusService` (new boot handler, `StatRules`); `PerkService` 1,486 -> about 1,130, `Lazy<IStatService>` gone | 348 | `StatService.cs`, `CombatQueriesService.cs`, `CombatAbilityHitService.cs` (field and constructor type, and the call at `TryResolveAbilityHit` line 272, which stays on the stay-part), `CombatAbilityMagnitudeService.cs`, `AbilityDamageStageService.cs`, 3 content files | b1; P5-7e1 and P5-7e2 (or the one-line rebase named under Conflicts) |
| P5-10d | PK-4 `PerkGrantedFeatService`, PK-5 `PerkRefundService`, ports `IFeatHotBar` and `IStatDrivenTrackerRefresh`; `PerkService` off `LargeTypes.txt` (about 640), `Lazy<ICombatService>` gone | 508 | `AbilityActivationService.cs`, `BeastSpawnService.cs` and `DroidService.cs` (one seam line and one constructor type each), `StealthService.cs`, `IQuickCastService.cs`, `ICombatService.cs`, `UsePerkFeat.cs`, 7 content files | c; h1 |
| P5-10e1 | SK-3 `SkillXPService`, `GiveSkillXP` cut; `GetDecayCandidates` and `ShouldDecaySkillRank` public on `SkillService`; `SkillService` 1,182 -> about 830, off `LargeTypes.txt` and `LongMethods.txt`, `Lazy<IStatService>` gone | 347 | 9 domain services, 8 static helpers, 7 content files, `SkillXPAwardTests` | b1 |
| P5-10e2 | SK-2 `SkillRankService` (no-copy `TryIsContributingSkill`), port `ITwinPistolPairQuery`; `SkillService` about 620; SK-3 takes `ISkillRankService` (4 `GiveSkillXP` sites: `GetRankCeiling`, `GetDecayCandidates` twice, `ShouldDecaySkillRank`) | 203 | `CharacterSheetAttributesTab.cs`, `CharacterSheetViewModel.cs`, `SkillsViewModel.cs`, `DistributeRPXPViewModel.cs`, `ITwinPistolEquipmentService.cs` | e1; P5-16h sheet PRs |
| P5-10f1 | PN-2 `PlayerKnownNameService`; `ApplyNameOverride`, `GetKnownNames`, the seven identity seams public on `PlayerNameService`; `PlayerNameService` 1,039 -> about 860, off `LargeTypes.txt` | 182 | `DisguiseService.cs`, `PlayerDescriptorService.cs`, `PlayerIntroductionService.cs`, `ConversationService.cs`, `PlayerFeedbackService.cs` (the `IDisplayNameLookup` port), 2 content files | b1 |
| P5-10f2 | PN-3 `PlayerNameOverrideService`, port `IPortraitTellGuard`; carried `AfkPlayers` re-keyed; PN-2 takes `IPlayerNameOverrideService`; `ChatPortraitTellsService` `Lazy<>` plain; `PlayerListService` `Lazy<>` swap | 430 | `CommunicationService.cs`, `PlayerListService.cs`, `ChatPortraitTellsService.cs`, `DisguiseService.cs`, `PlayerDescriptorService.cs`, 5 content files | f1 |
| P5-10g1 | MI-3 `MimicryLoadoutService`; `MimicryService` 1,307 -> about 650, off `LargeTypes.txt` and `ServicesPerType.txt` (11 -> 9) | 667 | `AbilityActivationService.cs`, 2 content files | b2; d (both edit `AbilityActivationService.cs`); e1 (both edit `MimicryService.cs`) |
| P5-10g2 | MI-2 `MimicryWitnessService` (new boot handler, carried tables re-keyed, on `ISkillXPService`); `Lazy<ICombatPointService>` and one `Lazy<IStatService>` gone | 392 | `UsePerkFeat.cs`, `DMPlayerChatCommand.cs` | g1; e1 |
| P5-10h1 | BM-2 `BeastSpawnService` (on `IPerkService`), BM-4 `BeastIncubationService`, `ApplyStats` public; `BeastMasteryService` 1,240 -> about 890, off `LargeTypes.txt` and `ServicesPerType.txt` (14 -> 10), `IncubationFieldNoteService` `Lazy<>` plain | 395 | 7 content files | b2 |
| P5-10h2 | BM-3 `BeastEventService` | 243 | none | h1 |
| P5-10i1 | DR-4 `DroidEventService` and DR-5 `DroidDespawnService`; `AcquireDroidItem`, `IsStoredItem`, `ReturnRejectedItems` public; `DroidService` 1,194 -> about 940, off `LargeTypes.txt`, 14 -> 11 services | 230 | none | b2; d; the other splits (owner decision 5) |
| P5-10i2 | DR-6 `DroidRestoreService` and DR-2 `DroidSpawnService`, `SpawnDroid` cut; `CanGiveItemToDroid`, `GetDroidItemId` public; `DroidService` off `ServicesPerType.txt` (11 -> 1) and `LongMethods.txt`; `DroidEquipmentDiagnosticsService` `Lazy<>` plain | 305 | 1 content file | i1 |
| P5-10i3 | DR-3 `DroidInventoryService` (the five helpers move; DR-4, DR-5, DR-6 and DR-2 change receiver); close-out: README, Status, this guide's Progress | 270 | 1 content file | i2; the other splits (owner decision 5) |

Intermediate sizes and service counts (checked for each row): after e1 `SkillService` has 3 services, SK-3 5; after f1 `PlayerNameService` has 5 services and PN-2 3; after i1 `DroidService` has 11 services (still
over 10, `ServicesPerType.txt` keeps a line), DR-4 5, DR-5 4; after i2 `DroidService` has 1 (`IItemService` only), DR-6 6 (it takes `IDroidInventoryService` in i3: 7), DR-2 7; after i3 DR-4 6, DR-5 5, DR-3 2; after h1 `BeastMasteryService` has 10 and BM-2 5, BM-4 8; after g1
`MimicryService` has 9 and MI-3 7.

**Parallel groups.**
- **Group 1 (after b1, three agents):** Perk (c, d), Skill (e1, e2) and PlayerName (f1, f2). They share no service file. c also waits for P5-7e1 and e2.
- **Group 2 (after b2, as slots free):** Beast (h1, h2), which runs before d so `BeastMasteryService` never carries both perk interfaces; Mimicry (g1, g2) after d and e1 (d and g1 both edit `AbilityActivationService.cs`;
  e1 and g1 both edit `MimicryService.cs`, and e1 hands g2 the `ISkillXPService` it is born on).
- **Group 3 (last, owner decision 5):** Droid (i1, i2, i3), after d and after the other splits. It calls `IAIService.SetAIProfile`, which stays on the AI core, so P5-8 does not conflict.
- Within a chain the rows are serial. Two rows never work on the same files at once: e2 waits for the two P5-16h character sheet PRs (or rebases onto them: scripted receiver edits); d and g2 edit `UsePerkFeat.cs` by script, as
  P5-8f, P5-7g and P5-7j do, on different lines.

### Baseline lines each PR changes

| File | c | d | e1 | e2 | f1 | f2 | g1 | g2 | h1 | h2 | i1 | i2 | i3 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `LargeTypes.txt` | `PerkService` about 1,130 | line gone | `SkillService` line gone (about 830) | - | `PlayerNameService` line gone (about 860) | - | `MimicryService` line gone (about 650) | - | `BeastMasteryService` line gone (about 890) | - | `DroidService` line gone (about 940) | - | - |
| `ServicesPerType.txt` | - | - | - | - | - | - | `MimicryService` 11 -> gone (9) | - | `BeastMasteryService` 14 -> 10, line gone | - | `DroidService` 14 -> 11 | `DroidService` line gone (11 -> 1) | - |
| `LongMethods.txt` | - | - | `GiveSkillXP` gone | - | - | - | - | - | - | - | - | `SpawnDroid` gone | - |
| `DeferredResolution.txt` | `PerkService.cs` Character line gone | `PerkService.cs` Combat line gone; `StealthService.cs` Character 5 -> 4 | `SkillService.cs` line gone | - | - | `ChatPortraitTellsService.cs` line gone; `PlayerListService.cs` Character stays 2 (one plain, one new `Lazy<>`; P5-15 takes it) | - | `MimicryService.cs` Combat line gone | `IncubationFieldNoteService.cs` line gone | - | - | `DroidEquipmentDiagnosticsService.cs` line gone | - |
| `UpwardEdges.txt` | - | `PerkService.cs` Abilities and Combat gone | - | `SkillService.cs` Combat gone | `PlayerNameService.cs` Social shrinks; `ConversationService.cs`, `PlayerFeedbackService.cs` Character shrink | `PlayerNameService.cs` Social gone | - | - | - | - | - | - | - |
| `LogicToContent.txt` | - | - | - | - | - | - | - | - | - | `BeastMasteryService.cs` two lines re-keyed to `BeastEventService.cs` | `DroidService.cs` line re-keyed to `DroidEventService.cs` | - | - |
| `StaticLookups.txt` | - | - | - | - | - | - | - | - | `BeastMasteryServiceTests.cs` follows its bench | - | - | - | - |

`DomainCycles.txt`, `StaticState.txt` and `SinkWork.txt` have no lines for these files. `CrossDomainEdges.txt` and `EdgesIntoCoreAndPlatform.txt` change only in member counts (revert them as P5-5g did). `DebtReport.md` is
regenerated with each. The sizes are projections from the member counts; the pull request records the measured ones. Handler snapshot files change as in "State, handlers and reload".
Also each PR: `ServiceRegistrations.cs`, `GameServices.<Domain>.cs`, `ConvertedServices.json`, `RemovedFacades.json`, `HandlerRenames.txt`, `ScriptHandlerOrder.txt`, `ClockSeamFiles.txt` where a part reads the clock,
`PlayerMessageAudit.json` rows.

## Conflicts

**With in-flight work.**

- **P5-7 (Combat).** No P5-7 PR edits the six files (the Combat guide says so: `RefreshStatDrivenTrackerEffects` stays on `ICombatService`, and Beast and Droid call only `BeginSetupAttribution`, which P5-7j keeps on the recorder
  stay-part; `MimicryService`'s `Lazy<ICombatPointService>` is untouched). P5-10 edits Combat-owned files: `CombatAbilityHitService.cs` in c (the field and constructor type, and the call at `TryResolveAbilityHit` line 272, which
  stays on the stay-part when P5-7g splits the file; P5-7e2 also edits the file), `AbilityDamageStageService.cs` in c (P5-7e1 edits it), `StealthService.cs` in d (one parameter; P5-7k and P5-8e also edit it),
  `ICombatService.cs` in d and `ITwinPistolEquipmentService.cs` in e2 (one inherited interface each). c waits for P5-7e1 and e2; if either slips, c names the one-line rebase for each file: the field, the
  constructor type and the call (`CombatAbilityHitService`), the field and its three uses (`AbilityDamageStageService`). d rebases the one `StealthService` parameter onto P5-7k and P5-8e whichever lands first. If P5-7j moves `BeginSetupAttribution`, P5-7j edits the single call line in `BeastSpawnService` and `DroidRestoreService`.
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
  `ShouldScrambleAccountName`, `EnsureUnknownDisplayName`, `GetUnknownDisplayNameByPlayerId`, `ApplyNameOverride`; `DroidService`'s six constants. Promoted in one PR and moved in a later one (each is public on the stay-part in between):
  `PlayerNameService.GetKnownNames` and the `IsDisguiseIdentityKey` seam (public in f1, stay on PN-1); `ApplyNameOverride` (public in f1, moves to PN-3 in f2);
  `SkillService.GetDecayCandidates` and `ShouldDecaySkillRank` (public in e1, move to SK-2 in e2); `SkillService.TryIsContributingSkill` (new in e2, stays on SK-1);
  `BeastMasteryService.ApplyStats` (public in h1, stays on BM-1); Droid `AcquireDroidItem`, `IsStoredItem` and `ReturnRejectedItems` (public in i1, move to DR-3 in i3),
  `CanGiveItemToDroid` and `GetDroidItemId` (public in i2, move to DR-3 in i3). New members: `PerkService.EnumerateAllPerks`, `GetPerkMaxLevel`,
  `GetPerksWithSkillRequirement`; `MimicryService.TryGetTechnique`, `GetTechnique`, `TryGetTechniqueForNpcFeat`, `EnumerateTechniques`; `PlayerNameService.ForgetKnownNames`, `ForgetDescriptorSetting`; `DroidService.GetPersonality`; the three boot handlers.
- **Changed bodies (a master edit to these ports by hand):** `PerkService.CachePerks` (two lines out), `MimicryService.CacheData` (four lines out), `PerkService.AggregateStatAdjustment` (one line), `SkillService.GiveSkillXP`
  (the cut) and `DroidService.SpawnDroid` (the cut): a master edit inside either body ports to the step that now holds it.
- **Nested types** `StatBonusGroup` and `TargetedStatBonusGroup` move into `PerkStatBonusService`; a master change to them ports to that file.
- `feature/space-revamp`'s `DroidService` calls port by the range table (above).

## Settled questions

The open questions of the first draft, settled by the coordinator (2026-11-23); recorded as decisions.

1. **Master freeze:** none (D10). A master edit inside a moved range is ported by hand with a Port log row.
2. **Upward edges:** inverted in P5-10, where the part that needs them is built ("The upward references").
3. **`GetWeaponSkill`:** stays on `SkillService` with `ITwinPistolPairQuery` (D9); P5-15 records the separate weapon-part option.
4. **Perk cache rebuild:** two `Rerun` boot handlers (D5).
5. **`StatRules.AggregateStatAdjustment`** used directly in `PerkStatBonusService` (D6).
6. **Droid:** six parts, moved top layer first (D11).
7. **Client evidence:** on every split PR, no waivers (owner rule); the list per PR is in "Tests".
8. **Test groundwork:** two PRs, b1 and b2.

## Risks

- **Services at 10.** `TrapsService` and `PerformanceAudienceService` are at 10 constructor parameters (the baseline lists only types over 10); each stays at 10 only because of the XP-formula placement
  (Skill). `CommunicationService` (9) and `PlayerListService` (8, 9 after f2) have room. A reviewer should confirm by regenerating `ServicesPerType.txt` in e1 and f2 that none appears.
- **Boot order.** The three new handlers must run in `Rerun`. The classification snapshot and the reload check cover a missing attribute; a wrong position in `ScriptHandlerOrder.txt` would let a later `OnModuleCacheBefore`
  handler read an empty stat-bonus table (none reads it at cache time today).
- **Handler sharing between PRs.** c and d both insert lines after `PerkService.CacheData` (41); each PR lists its lines and the second rebases.
- **`SpawnDroid` and `GiveSkillXP` cuts** are the two bodies that are not pure moves. The transcripts of P5-10b exist to prove the order; the token comparison reports them as the only intended differences.
- **Carried state keys.** A carried field that changes class needs `[ReloadCarriedFrom]` or a reload from the previous build loses it (three fields). `ReloadCarryHistoryTests` fails without it.
- **Hot-path cost.** `GetPerkLevel` and the stat-bonus reads run on every stat recalculation; the split adds no call on those paths (`StatService` takes `IPerkStatBonusService` directly). `GiveSkillXP` adds four private calls.
  The force-affinity reads add none. No `CombatLoad` run is needed.

## Progress

P5-10a: this guide (2026-11-23). P5-10b1: the Perk, Skill and PlayerName test groundwork and proof (2026-11-23, "P5-10b1 as built" below). P5-10b2: test groundwork for Mimicry, Beast and Droid (2026-11-23, "P5-10b2 as built" below). P5-10f1: `PlayerKnownNameService` built (2026-11-24, "P5-10f1 as built" below). P5-10f2: `PlayerNameOverrideService` built (2026-11-24, "P5-10f2 as built" below). P5-10h1: `BeastSpawnService` and `BeastIncubationService` built (2026-11-25, "P5-10h1 as built" below). P5-10e1: `SkillXPService` built (2026-11-25, "P5-10e1 as built" below). P5-10h2: `BeastEventService` built (2026-11-25, "P5-10h2 as built" below). Next: P5-10c, e2 and g1 can start.

### P5-10b1 as built (2026-11-23)

Tests only; no production file changed. The later Perk, Skill and PlayerName PRs run everything below unchanged except for what the "Per PR" list names (accessor types and
bodies, the part files added to `CharacterCompanionsSource`, and the harness member names of `SkillXPAwardTests`). No client evidence: nothing in game changes.

- **`CharacterCompanionsSource`** (`Tests/Support/CharacterCompanionsSource.cs`): one `ServiceSource` per service (`Perk`, `Skill`, `PlayerName`), built on `PartSourceFiles`, with `Read()`, `Paths()`, `Trees()`,
  `Method(name)`, `Member(name)`, `Members(...)`, `RelativePath(name)`, `InstanceMethod(name)`, and `CharacterCompanionsSource.Member(name)` across the three. A PR that moves a part adds its file to that service's list
  and its interface to the interface set. P5-10b2 adds its three services to the same class.
- **Readers repointed** (12 reads in 10 files): `PlayerNameRecognitionTests` (11 tests, `ExtractMethod(source, "<signature>")` became `Member("<name>")`, so a promoted modifier or a moved member changes nothing; the needles on
  `BuildDisplayNameWithDescriptor` and `BuildColoredDisplayNameWithDescriptor` lost `private`, the two become public in f1), `PlayerIdentityAuditTests` (the two harness reads), `PerkStatBonusTests`, `PerkRefundStatusEffectCleanupTests`
  (2), `PerksWindowTests`, `CrossSkillPerkInteractionSafetyTests`, `SkillRankLimitTests`, `SkillXPAwardTests` (the harness now compiles from `Skill.Trees()`; e1 still edits the member names it lists), `DeadPlayerXPTests`,
  `PlayerMessageAuditTests` (the file list and the skill cap warning test read `Skill.Paths()`; the Mimicry, Beast and Droid entries stay for b2). `ClockSeamSourceGuardTests` and `ClockSeamFiles.txt` still name
  `PerkService.cs`; d adds `PerkRefundService.cs` as the map says.
- **Accessors and doubles.** `PerkBench` gained `Catalog`, `StatBonus`, `Affinity`, `Feats`, `Refund` (all `Service` today) and `CacheData()` (the boot handlers in order; c and d add their calls). `TestPerkService` left `PerkBench.cs` and is
  `partial` files by the part that calls each seam (`.Catalog`, `.StatBonus`, `.Affinity`, `.Feats`, `.Refund`). `TestSkillService` left `SkillServiceTests` (`Catalog`, `Rank`, `XP` accessors; seams in `.Catalog` and `.XP`;
  `CreateWithDefaultSeams` for the constructor call), as did `BenchPlayerNameService` (`Names`, `KnownNames`, `Overrides`; seams in `.Names` and `.Overrides`) and `BlankIdentityPlayerNameService`; `LoggingDB` moved to `Support`. The tests of
  `PerkServiceTests`, `SkillServiceTests` and `PlayerNameServiceTests` call each member through its part's accessor, so a PR changes the accessor and the double file, not the tests. `IsDroid` is one seam on the Perk double
  until c and d give PK-2 and PK-4 their own.
- **Transcripts and fixtures** (`Tests/Fixtures/CharacterTranscripts/*.txt`, `-text` in `.gitattributes`, LF, ASCII; regenerate with `SWLOR_UPDATE_SNAPSHOTS=1`): `SkillXPAward.txt` (`SkillXPTranscriptTests`, 35 steps through the real
  container with recording leaves for stat, the GUI publisher, player feedback and the companion lookup: refusals, bonuses, the henchman penalty, debt, the rank ceiling and the once-per-login warning, rank-ups with skill and ability
  points, and a rank gained at the skill cap with the decay draw (`Random.Next(n)`, one to four candidates, candidates above their limit first, the last candidate used up), every engine write, database read and write, refresh notice,
  event and draw in order, then the return value and the record's changes); `PlayerTell.txt` (`PlayerTellTranscriptTests`, 9 steps of `PreventAmbiguousTellTargets` through the real container with the tell, rename and util plugins recorded);
  `PerkCatalog.txt` (`PerkCatalogTests`, 602 perks, about 4,100 lines, regenerated after the master sync: Focus Attention (perk 283) reads `DamageEnmityPercentAdjustment` 15/30/50 since master #3504, three rows and nothing else changed: each perk's levels, the stat bonus and `GetStatSources` per level and stat, the force affinity reads at seven affinities, and the granted and current active ability feats per
  level, built from every perk and ability list of the server). `PerkCatalogTests` also pins that no `PerkType` is in two perk lists (the D5 equivalence).
- **Engine test** (`SkillXPAwardDecayEngineTests`, two tests, constructor-injected): the player is at the cap with one unlocked candidate, so the `Random.Next` pick is not left to chance. Case 1: `Provoke` level 1 needs Armor 5; a rank in Piloting
  decays Armor to 4, the perk level is refunded (price back, Provoke1 removed). Case 2: Mimicry at the requirement of `BrutalBashTechnique` decays by one; the technique is unequipped and its feat removed while `CombatAnalyzer` keeps its level.
  Both assert `SWLOR_SKILL_LOST_BY_DECAY` once, on the player, for the decayed skill. The signal is counted without a script handler (a handler would add a script to `ScriptHandlerOrder.EngineTests.txt` that master lacks, which the parity gate refuses): `SignalSpy` wraps the events plugin around the award, inside the creature context (the override is per async flow and the engine runs that context in its own), with `EventsPluginRuntime.Override`, forwards every call, and records each `SignalEvent` with the skill id pushed before it, so a signal that fires twice or not at all fails the count.
- **Not covered, and why.** The combined XP message of a message source needs the scheduler (it runs on the real clock); `SkillServiceTests` covers it through the double. The warning "Excess XP was lost" in `GiveSkillXP` is unreachable: the loop
  stops at the top before the branch can see an empty candidate list at a total above the cap. `PerkCatalog.txt` reads stat bonuses through an NPC, so the player path of `GetStatBonusPerkLevel` (`GetPerkLevel`) is covered by `PerkServiceTests`.
- **Departures from the map.** `PlayerTellTranscriptTests` and `SkillXPTranscriptTests` use the combat transcript bench (`CombatTranscriptBench`) with their own leaves instead of a new bench. The bench `CacheData()` helper stands in for the
  stat-bonus handler call c would otherwise add to 28 tests.
- **Runs.** Unit (`SWLOR.Game.Server.Tests --no-build`, filters `PerkService`, `PerkCatalog`, `PerkFacade`, `PerkStatBonus`, `PerkRefund`, `PerksWindow`, `SkillService`, `SkillXP`, `SkillRankLimit`, `SkillFacade`, `PlayerName`, `PlayerTell`,
  `PlayerIdentityAudit`, `DeadPlayerXP`, `CrossSkillPerk`, `PlayerMessageAudit`, `ConvertedServiceContainer`): 1,174 passed. `SWLOR.Architecture.Tests`: 297 passed, no baseline changed (the first engine test used the static service lookups and
  was rewritten with constructor injection, which the Phase 5 lookup count requires). Engine tests (host entry, dev server module and haks, scratch home, `scripts/run-engine-tests.sh` with the repository root set by hand): `SkillXPAwardDecay` 2 of 2 passed three times (the third on the constructor-injected class, twice in a row after it); and on the unchanged code `PlayerPerkFeats` 4 of 4, `PlayerName` 9 of 9, `PerkTracker` 26 of 26, `BareHandedAttackSkill` 2 of 2 and `Perk` (which also selects `PerkSweep`) 61 passed, 1 skipped of 62.

### P5-10b2 as built (2026-11-23)

Tests only; no production file changed. A later PR of the Mimicry, Beast or Droid chain edits one file list per service in `MimicryBeastDroidSource`, the receiver one accessor returns
(`TestMimicry`, `TestBeastMastery`, `TestDroid`, and the one property per part on a transcript scene), and the receiver text of the tests of its own members. It edits no other bench, double or fixture.

- **Source reader** (`Tests/Support/MimicryBeastDroidSource.cs`): one file list and one part-interface set per service, read through `PartSourceFiles` as `EnmitySource` does (`Read`, `Paths`,
  `AllPaths`, `Method`, `Member`, `InstanceMethod`, `RelativePath`). **Departure:** it is not in `CharacterCompanionsSource`, which P5-10b1 builds for Perk, Skill and PlayerName; the two share no line,
  and the owner can fold them at cutover. Reads it replaced: `MimicryTests` (the wiring test reads every Mimicry file; the `RevokeTechniqueFeat` slice is now `Member`), `DMPossessionPlayerLoopTests` (two
  harness reads), `PlayerMessageAuditTests` (the Mimicry failures test reads every Mimicry file; the gameplay-outcome file list gains `AllPaths()` de-duplicated by full path, and its two literal entries
  stay so b1's edit of the same two lines does not conflict), `TintMapReviewTests` (the `DroidService.cs` read). **The map's count differs:** only these four files read the three services; `AbilitySourceTests`
  names `MimicryService.cs` as a path `IsAbilityFile` must refuse, and an engine test names it in a comment.
- **Doubles** (`Tests/Support/TestMimicry*.cs`, `TestBeastMastery*.cs`, `TestDroid*.cs`): the three nested doubles (`TestMimicry`, `TestBeastMastery`, `AmmunitionDroidService`) are top-level `partial`
  classes with one file per part for the seams its code calls and a `.Shared` file for seams two parts call (Mimicry: witness `GetNpcLevel`, `GiveSkillXP`, `GetDeltaXP`, `HasCombatPoints`,
  `ScheduleRepeating`; loadout `IsFeatRegistered`, `GetAbilityDetail`, the two status effect removals; shared `GetPerkLevel`. Beast: events the control, restore and rest seams and `ReturnItem`; shared
  `GetPerkLevel`, `SyncGrantedFeats`, `TogglePlayerWindow`. Droid: inventory `ReadGeneratedAmmunitionFlags`). **Departure:** Beast has no spawn or incubation file, since every seam of those two parts is shared.
  The accessors (`Techniques`, `Witness`, `Loadout`; `Beasts`, `Spawn`, `Events`, `Incubation`; `Droids`, `Spawn`, `Inventory`, `Events`, `Despawn`, `Restore`) are properties that return the double itself, and
  `MimicryServiceTests`, `BeastMasteryServiceTests`, `DroidServiceTests` and `DroidServiceWorldTests` call each member through the accessor of its part (the doubles' own state stays on the double).
  **Departure:** `DroidWorld` builds no service, so the Droid accessors are on `TestDroid` (a constructor over a `DroidWorld`, and one over the facade forwarders); `DroidWorld` gained an optional `OrderedLog` instead.
- **Transcripts** (`Tests/Fixtures/CompanionTranscripts/*.txt`, `-text` in `.gitattributes`, LF): each line is a call to a leaf, an engine write or a `Random` draw, in order. Mimicry and Beast run on the P5-7b bench
  (`CombatTranscriptBench`, seed 20261118) with their own leaf lists; Droid runs on `DroidWorld` with `OrderedLog` and `ObservedScriptedProxy` (the engine, the four plugins and the 14 neighbours; seed 20261123).
  - `MimicryWitnessTranscriptTests`: `MimicryWitness.txt` (`TryRecordWitness` and the analysis credit through `OnCreatureAbilityUsed`: 12 steps) and `MimicryLearn.txt` (`TryLearnTechniques` through
    `OnCreatureDeath`: 7 steps, the `D100` rolls, the learned-technique writes, messages, skill XP and sound).
  - `BeastIncubationTranscriptTests`: `BeastUseIncubator.txt` (6 steps) and `BeastCreateEgg.txt` (job eggs with mutation suppressed, impossible, and chances 0, 1000, 500, 500, with the `Next(1000)` and weighted draws,
    the field note, every purity property, the vendor bonus and the job removal; an egg by beast type). The scenes run on the whole beast catalog, and the first step names the two beasts chosen by a rule
    (most mutations a job rich in enzymes qualifies for; first with none), so a catalog edit shows in line one. The running-job message reads 29 seconds on any run under a second.
  - `DroidSpawnTranscriptTests`: `DroidSpawn.txt` (`SpawnDroid` in six summons: nothing saved; saved appearance, tint, portrait and sound set; a saved loadout with a stored container and a pending return;
    two refused items kept under a retry key and restored by the next summon; an open AI window with no custom name; the delayed heal included). The controller's property reads are left out. `DroidWorld.ItemIds`
    gives a deserialized item the droid item id it was saved with (a serialized item carries its locals) so no `Guid` reaches the text. **Not covered:** droid perks (the perk facade) and the paths that mint
    an identifier with `Guid.NewGuid` (an item that cannot be handed back, an item with no valid copy). `TheSavedItemSetIsRestoredToTheSameSlotsAndCarriedItems` is the restore test run before and after the cut with
    the same item set (slots, possessors, carried items, items handed back, the saved controller data).
  - P5-10e1 gives the witness part `ISkillXPService`, so it adds that leaf to `MimicryWitnessTranscriptTests.Leaves` and the `Skill.GiveSkillXP` lines of `MimicryLearn.txt` take the new label: the one intended fixture edit.
- **Engine tests:** none are new (the skill decay test is P5-10b1's). Run on this base through `scripts/run-engine-tests.sh` (a scratch copy with the repo root fixed and CRLF removed; no `jq`, so the report was
  read directly): the categories and names of the map's Mimicry, Beast and Droid lists (`MimicryLoadouts`, `MimicryObservation`, `MimicryDescriptions`, the Mimicry potency tests, `MimicryTraitPotency`,
  `PlayerTechniques`, the Beastmaster ability behaviors (`PlayerBeastAbilityEngineTests`), `Incubation`, `CapstoneProof`, `TankEnmity`, every `Droid` name or category, `ItemClaimSpace`): 112 selected, 110 passed,
  1 skipped (the opt-in view model transcript), 1 failed: `Migration expands blueprint alternatives inside serialized droid inventories` (`KeyNotFoundException` for `DroidLightsaberBoost1` in
  `CraftService.GetRecipe`), on the base this was built from. Master's #3486 deleted that test with the retired migrations, and the master sync to 82ebe9f00 took the deletion, so the branch no longer
  has it and a split PR's engine run expects no failure from it.
- **Unit tests** (`SWLOR.Game.Server.Tests`, filters `Mimicry`, `Beast`, `Droid`, `Incubation`, `PlayerMessageAudit`, `ConvertedServiceContainer`, `DMPossessionPlayerLoop`): 1,585 passed, 0 failed, including the
  six new tests; the transcripts are identical across repeated runs. `SWLOR.Architecture.Tests`: 297 passed, no baseline changed.
- **Client evidence:** none. No production file changed and nothing in game changes; the transcripts and the engine run are the evidence, as for P5-7b.

### P5-10f1 as built (2026-11-24)

Branch `claude/mr-p5-10f1`, base `97958db3c` (the P5-10b2 record). `PlayerNameService` 1,039 -> 869 lines (5 services, unchanged: database, player list, chat portrait tells, and the two `Lazy<>` for the disguise and the descriptors); it is off `LargeTypes.txt`.

- **Part.** `PlayerKnownNameService : IPlayerKnownNameService` (`Domain/Character`, 212 lines, 3 services: `IDBService`, `IPlayerNameService`, `IPlayerListRefresh`; 182 member lines moved): `SearchKnownPlayerIdsByName`, `ValidateKnownName`, `ValidateKnownNameInput`, `ValidateKnownNameAssignment`, `SetKnownName`, `ValidateKnownNameTarget`, `ContainsColorToken`, `ValidateKnownNameIsUnique`, `ForgetKnownName`, `DeleteKnownNameReferences`. The interface has the seven public ones. New `GameServices.PlayerKnownName`. No handler, no `[ReloadState]` field, no clock read and no player message moves (`PlayerMessageAudit.json` is unchanged; the two `PlayerNameService.cs` rows sit above the cut at lines 170 and 232), so `ScriptHandlerOrder.txt`, `HandlerRenames.txt`, `ClockSeamFiles.txt` and the reload snapshots do not change and no hot reload run was made.
- **Stay-part changes.** Public on `PlayerNameService` and `IPlayerNameService`: `ApplyNameOverride`, `GetKnownNames`, `GetIdentityKey`, the new `IsDisguiseIdentityKey` seam (over `Disguise.IsDisguiseIdentityKey`), and the new `ForgetKnownNames(observerId)` (the one `Remove` line `DeleteKnownNameReferences` needs). The other five identity seams (`GetActiveDisguise`, `GetDisplayDescriptor`, `ShouldScrambleAccountName`, `EnsureUnknownDisplayName`, `GetUnknownDisplayNameByPlayerId`) are `public virtual` on the class only; f2 puts the ones PN-3 calls on the interface. `ScheduleRepeating` and `SubscribeScriptContextEnd` stay protected. `IPlayerNameService` extends the new Foundation-owned `IDisplayNameLookup` (`GetDisplayName`), registered as an alias like `IPlayerListRefresh`; `ConversationService` and `PlayerFeedbackService` take the port.
- **Call sites.** `DisguiseService` (7 -> 8 services), `PlayerDescriptorService` (3 -> 4) and `PlayerIntroductionService` (3 -> 4) take `IPlayerKnownNameService` for the validation, set and delete calls. `CharacterSocialChatCommand` (`/name`, `/forgetname`) and `PropertyPermissionsViewModel` (the known-name search) change property name only. `RemovedFacades.json` needs no entry (the members were never facade members; `GameServices.PlayerName.X` calls became `GameServices.PlayerKnownName.X`).
- **Departures from the plan.**
  - `PlayerKnownName` is its own `ConvertedServices.json` entry and `DomainMap.json` service, not an `extras` part of `PlayerName`: `ConvertedServicesTests` requires a part's class name to start with the service name (as P5-8f found). `tools/Refactor/DomainOrder/py/upward.py` matches the new name.
  - Only the five members the part calls are on `IPlayerNameService` (the plan gave all seven seams); the rest of the seams are public on the class.
  - The part keeps the body of `ValidateKnownName` verbatim through a private constant `MaxKnownNameLength = PlayerNameService.MaxKnownNameLength`.
  - `PlayerNameService.cs` keeps its `Character -> Social` upward edge (4): its refreshes still call `IPlayerListService.QueueRefresh`; f2 removes it. The plan said it shrinks.
  - `GameServices.PlayerKnownName` collides with the entity `PlayerKnownName` in an expression (`nameof(PlayerKnownName.ObserverPlayerId)`) wherever the global static import and the entity namespace meet. Seven engine test files call `GameServices.PlayerKnownName.X(...)` and `nameof(Entity.PlayerKnownName.ObserverPlayerId)`; production code is unaffected.
  - Size: 869 lines, not about 860; nothing else is over a limit.
- **Tests.** Old test names are kept. `PlayerNameServiceTests` keeps the display name, cache, audit, override, AFK and tell tests; its world, `SetUp` and helpers moved to the abstract `PlayerNameTestBase`, and `PlayerKnownNameServiceTests` (27 test cases, 85 stay in `PlayerNameServiceTests`, 112 in all as before: the validation, the set and forget writes, the search, the cleanup) derives from it. `BenchPlayerNameService.KnownNames` and `BlankIdentityPlayerNameService.KnownNames` build a real `PlayerKnownNameService` on the instance; the double's seam overrides became `public override`. `PlayerDescriptorServiceTests`, `PlayerIntroductionServiceTests`, `DisguiseWorld` and `DisguiseServiceTests` script a second proxy for `IPlayerKnownNameService`; the three Foundation tests pass `IDisplayNameLookup`. `CharacterCompanionsSource.PlayerName` lists `PlayerKnownNameService.cs` and `IPlayerKnownNameService`. Re-pointed needles: `PlayerNameRecognitionTests`, `PlayerNameChatCommandTests`, `PlayerFacingNameBroadcastTests`; the `PlayerIdentityAuditTests` harness gained a `PlayerKnownName` stand-in. `ConvertedServiceContainerTests` and `InvertedInterfaceRegistrationTests` list `IDisplayNameLookup`. The ordered `PlayerTell.txt` transcript is unchanged (no label changed).
- **Baselines** (`SWLOR_UPDATE_SNAPSHOTS=1`): `LargeTypes.txt` loses `PlayerNameService`; `UpwardEdges.txt` loses the two `Foundation -> Character` lines (`ConversationService.cs`, `PlayerFeedbackService.cs`, 3 each); `DomainCycles.txt` loses `Character <-> Foundation`; `DebtReport.md` regenerated. `CrossDomainEdges.txt` loses `Domain/Foundation -> Domain/Character` (header 4 -> 3 edges), every other change there and all of `EdgesIntoCoreAndPlatform.txt` were member counts and are put back. `ServicesPerType.txt`, `DeferredResolution.txt`, `LongMethods.txt`, `StaticLookups.txt` and `LogicToContent.txt` do not change.
- **Proof.**
  - *Moved and changed bodies* (Roslyn token comparison, `97958db3c` against the head; modifiers and the receivers `_playerName`, `_playerKnownName`, `_disguise`, `_playerDescriptor`, `_playerList`, `PlayerName`, `PlayerKnownName` ignored): `PlayerNameService.cs` 79 members plus the moved ones, `PlayerKnownNameService.cs`, `DisguiseService.cs`, `PlayerDescriptorService.cs`, `PlayerIntroductionService.cs`, `ConversationService.cs`, `PlayerFeedbackService.cs`, `CharacterSocialChatCommand.cs`: 245 identical, 6 different, all intended: the five constructors (a new `IPlayerKnownNameService` or `IDisplayNameLookup` parameter) and `DeleteKnownNameReferences`, whose `KnownNamesByObserverId.Remove(...)` line is `ForgetKnownNames(...)`. All ten moved members and the promoted seams are identical.
  - *Unit tests*: `SWLOR.Game.Server.Tests`, filters `PlayerName`, `PlayerKnownName`, `PlayerIdentity`, `Disguise`, `PlayerDescriptor`, `PlayerIntroduction`, `Conversation`, `PlayerTell`, `PlayerFeedback`, `Handler`, `Reload`, `PlayerMessageAudit`, `ConvertedServiceContainer`, `LazyDependencyCycle`, `InvertedInterface`, `ClockSeam`, `PlayerSession`, `PlayerFacing`, `Snippet`, `PlayerList`, `CharacterCompanions`: 1,878 tests, 3 fail, all for the empty hak submodule (`IdentityLoadHints_ExplainNamesDescriptionsAndDisguises` reads `sw_2da/loadhints.2da`, `UnknownNames_AreGeneratedDuringLoginInitialization` reads `sw_2da/appearance.2da`, `Flags_ShipTheirIconsInTheUIHak` reads `sw_ui/pl_flag_rp.tga`). Whole suite, unfiltered: 19,995 tests, 246 failed, 19,746 passed, 3 skipped. The 246 are environmental and fail identically at `97958db3c` (a second full run in a scratch worktree at the base: every head failure is also a base failure, none only at the head): 224 read hak art or 2DA files from the empty `SWLOR_Haks`, and 22 are source-reading tests that resolve the repository root to the main checkout, not this worktree (their messages name `C:\Projects\SWLOR_NWN\SWLOR.Game.Server\...` files such as `EquipmentPredicates.cs`, or the Eshan palette and quest-bounty checks over module content). `SWLOR.Architecture.Tests`: 297 pass. `PlayerKnownNameService.cs` line coverage 99.2% from the part's tests (floor `Domain/Character` 85).
  - *Engine tests*, host entry, dev server module and haks, scratch home, filter `Characterization|PlayerName|PlayerList|PlayerDataCache|DMPlayerExamine|DMPossessedName|Disguise|Identity` (70 tests each: the 7 engine test files of the map plus the characterization scenarios), at `97958db3c` and at the head: base 69 pass, 0 fail, 1 skipped (the opt-in `ViewModelTranscript` "Disguise window publishes the same bindings and values as before the split"); head the same, per-test outcomes identical for all 70 names, no snapshot changed. The report was read with node (no `jq`).
  - *Hot reload*: no handler and no `[ReloadState]` field moves, so no reload run was made.
  - *Client evidence*: base and head Release builds of the same module, haks and test characters (agantarn as observer, zabb1 as second player on a second client with the observer's stored key renamed), driven by a temporary driver kept outside the repository (it only wraps the services and writes status lines; the scenes use the real chat commands and windows). Identical steps at both builds, `Server` test environment, `ooc_area`: (1) one client: `/name` with 65 characters ("Names may be no longer than 64 characters."), empty ("Please enter a name."), with a color code ("Names may not contain color codes."), `/name Seedy Fellow` on self ("Public description set..."; audit name `Agan Tarn [Seedy Fellow]`), `/forgetname` on self ("You cannot forget your own name."); (2) two clients: an unknown player is shown by the gray descriptor (`Unassuming Humanoid`) in the nameplate and the radial; `/name Zed Vorn` on the other player ("Private label saved as 'Zed Vorn'. Only you can see this label.") shows `Zed Vorn [Unassuming Humanoid]` in the service lookups and `Zed Vorn:` in the observer's chat; `/forgetname` ("Private label removed. This changes only what you see.") goes back to the descriptor; an introduction (`/introduce`, then Remember in the Introductions window) shows `Zabb the Wanderer`, in chat, the radial and the examine window title; (3) a disguise made and activated through the Disguises window (descriptor `Unassuming HumanoidMasked Courier`): the observer sees the disguise descriptor and the real name is hidden, a `/name Masked Pal` on the disguise identity shows `Masked Pal [descriptor]`, the search by name skips the disguise label (`search 'Masked'` and `'Pal'` return nothing), and `DeleteKnownNameReferences` for the disguise identity removes the label (1 reference) and restores the descriptor; (4) the display name lookups, as the driver prints them for each observer and target (`GetDisplayName`, `GetColoredDisplayName`, `GetChatDisplayName`, `GetDisplayNameByPlayerId`, `GetPlainDisplayNameByPlayerId`, `GetAuditName`) and the search by known name (`search 'Zabb'` and `'wander'` find the player id, `'nobody'` nothing; `assign` on self gives "Known names cannot target the observer."; deleting the references to a player's identity returns 1 and empties the next search). The status lines, normalized chat and audit logs read the same at base and head (0 differing lines in the four status files; the chat logs differ only by the loading-screen tip, the server restart notice and the base's two extra manual steps, one more color-code attempt and one more introduction offer). **Limits:** the `PlayerFeedbackService` lookup (`SendResourceRestored` for a companion) was not reached in game; its lookup is the same `GetDisplayName(viewer, target)` the driver prints, and its unit tests pass through the port. The driver's `validate` and `assign` lines with an empty argument throw an index error at both builds (a driver bug, not game code). A first second-client attempt with `sarkell.bic` was booted at the base by a login migration error ("Could not remove a retired item from its migration container", `MigrationItemDisposal.Remove`); neither file is in this change and it is tracked separately, so `zabb1.bic` was used at both builds. Screenshots, status lines, chat logs and the engine reports: `C:/Temp/mr-p5-10f1/evidence/{base,head}/`.

### P5-10f2 as built (2026-11-24)

Branch `claude/mr-p5-10f2`, base `d8611b8ab` (the feature branch after P5-7i2). `PlayerNameService` 869 -> 447 lines (3 services: `IDBService` and the two `Lazy<>` for the disguise and the descriptors).

- **Part.** `PlayerNameOverrideService : IPlayerNameOverrideService, IReloadStateAdopter` (`Domain/Character`, 455 lines, 3 services: `IPlayerNameService`, `IPlayerListRefresh`, `IPortraitTellGuard`; 414 member lines moved): the three handlers (`ApplyNameOverridesOnEnter`, `ClearAfkOnExit`, `PreventAmbiguousTellTargets`), `IsAfk`, `OnCarriedStateAdopted`, `ToggleAfk`, `RemoveAfk`, `CheckAfkMovement`, the two suffix reads, `GetChatDisplayName`, `GetColoredChatDisplayName`, `SendChatMessageWithChatNameOverride`, `RefreshNameOverridesForObserver` and `ForPlayer`, `ApplyNameOverridesForPlayer`, `ApplyNameOverridesForDMObserver`, `ApplyTrueNameOverride`, `ApplyNameOverride`, `CanApplyChatNameOverride`, `ApplyChatNameOverride`, `RestoreNameOverride`, `ResolveChatDisplayName`, `CountDisplayNameMatches` and the `ScheduleRepeating` seam. `AfkPlayers` moved with `[ReloadState(Carried)]` and `[ReloadCarriedFrom("SWLOR.Game.Server.Domain.Character.PlayerNameService")]`. New `GameServices.PlayerNameOverride`; the interface has the eleven public members.
- **Ports and holders.** `IPortraitTellGuard` (Character-owned, `RefuseStalePortraitTell`; `IChatPortraitTellsService : IPortraitTellGuard`, registered as an alias like `IPlayerListRefresh`). `PlayerKnownNameService` takes `IPlayerNameOverrideService` for `ApplyNameOverride` (4 services). `ChatPortraitTellsService`'s `Lazy<IPlayerNameService>` is plain (its `DeferredResolution.txt` line is gone). `PlayerListService`: `IPlayerNameService` plain and a new `Lazy<IPlayerNameOverrideService>` for `IsAfk` (the cycle `PN-3 -> IPlayerListRefresh -> PlayerListService -> PN-3`; the `Lazy<>` count stays 4 in the type and 2 in the Character line; P5-15 takes it). `CommunicationService` takes `IPlayerNameOverrideService` instead of `IPlayerNameService` (9 services). `DisguiseService` (8 -> 9) and `PlayerDescriptorService` (4 -> 5) gain the override part for `RefreshNameOverridesForPlayer`. Five content files (`CharacterSocialChatCommand`, `DMPlayerSearchViewModel`, `HoloNetViewModel`, `SettingsViewModel`, `PlayerInitialization`) and four engine test files change property name only; `RemovedFacades.json` lists the eleven members under owner `PlayerNameOverride`.
- **Stay-part changes.** Public on `PlayerNameService` and `IPlayerNameService`: `ResolveDisplayName`, `BuildDisplayNameWithDescriptor`, `BuildStaffDisplayName`, `GetActiveDisguise`, `GetDisplayDescriptor`, `ShouldScrambleAccountName`, `EnsureUnknownDisplayName`, and the new `ForgetDescriptorSetting(observerId)` (the one `Remove` line `RefreshNameOverridesForObserver` needs). `ApplyNameOverride`, `IsAfk` and the other moved members left the interface. `PlayerNameService` keeps no `Character -> Social` reference.
- **Departures from the plan.**
  - `PlayerNameOverride` is its own `ConvertedServices.json` entry and `DomainMap.json` service, as f1 found for `PlayerKnownName`; `tools/Refactor/DomainOrder/py/upward.py` matches the new name.
  - The part also owns `UnknownNamePrefix` and `UnknownNameSuffix` with the staff marker (only the override code reads them), so it has 5 fields, not 3.
  - PN-3 reaches `EnsureUnknownDisplayName` and the other identity seams through `IPlayerNameService`; its only seam is `ScheduleRepeating` (the plan also listed `EnsureUnknownDisplayName`). Only the seams PN-3 calls are on the interface, as f1 said.
  - Size: 447 and 455 lines (the plan projected 435 and 480).
  - `PlayerMessageAudit.json` is not regenerated: the two rows of `PlayerNameService.cs` (the ambiguous tell hint and "You are no longer AFK.") now belong to `PlayerNameOverrideService.cs`, so `MessageInventory_CoversEveryRuntimeCallSite` fails until the owner runs `tools/ExportPlayerMessageAudit.ps1`.
  - A new engine test, `HotReloadNamesEngineTests` (category `HotReloadPlayers`, constructor-injected, no script handler), because the plan's reload recheck needs two players that stay online. It is not a copy of `HotReloadPlayersEngineTests`, which is unchanged: a version that added the checks there raised the static service lookups of that file from 8 to 16.
- **Tests.** Old test names are kept. `PlayerNameServiceTests` keeps the display name, cache and audit tests; the override, AFK, chat override and tell tests (479 lines) are `PlayerNameOverrideServiceTests`, over the same `PlayerNameTestBase`. `BenchPlayerNameService` takes the list refresh and the tell guard and builds a real `BenchPlayerNameOverrideService` (`Overrides`, with the repeating-check seam) and `PlayerKnownNameService` over itself; `EnsureUnknownDisplayName` stays a seam of the stay-part double. `CharacterCompanionsSource.PlayerName` lists `PlayerNameOverrideService.cs` and the new interface; the `PlayerNameRecognitionTests`, `PlayerFacingNameBroadcastTests` needles, the `PlayerIdentityAuditTests` and `DMPossessionBroadcastTests` harness shims, `DisguiseWorld`, `PlayerDescriptorServiceTests`, `PlayerListServiceTests`, `ChatPortraitTellsServiceTests`, `CommunicationServiceTests`, `TestCommunicationService`, `ReloadStateCarrierTests` and `ConvertedServiceContainerTests` / `InvertedInterfaceRegistrationTests` (the new port) follow the moves. New: `Forgetting_the_descriptor_setting_makes_the_next_read_come_from_the_record`. The ordered `PlayerTell.txt` transcript is unchanged (its test reads the tell through `IPlayerNameOverrideService`; no label changed). Snapshots: `PlayerSessionClassification.txt` and `ReloadAdoption.txt` re-keyed, `ReloadCarryHistory.txt` gains `PlayerNameOverrideService.AfkPlayers` and keeps the old key; `ScriptHandlerOrder.txt` and `HandlerRenames.txt` change the three class names in place (event and position unchanged).
- **Baselines** (`SWLOR_UPDATE_SNAPSHOTS=1`): `DeferredResolution.txt` loses `ChatPortraitTellsService.cs` (Social -> Character, 1); `UpwardEdges.txt` loses `PlayerNameService.cs: Character -> Social | 4`; `DomainCycles.txt` loses `Character <-> Social`; `DebtReport.md` regenerated; `CrossDomainEdges.txt` loses `Domain/Character -> Domain/Social` (the header 3 -> 2 edges), every other change there and all of `EdgesIntoCoreAndPlatform.txt` were member counts and are put back. `LargeTypes.txt`, `ServicesPerType.txt`, `LongMethods.txt`, `StaticLookups.txt` and `LogicToContent.txt` do not change.
- **Proof.**
  - *Moved and changed bodies* (Roslyn token comparison, `d8611b8ab` against the head; modifiers and the receivers `_playerName`, `_playerKnownName`, `_playerNameOverride`, `_disguise`, `_playerDescriptor`, `_playerList`, `_playerListRefresh`, `_portraitTellGuard`, `_chatPortraitTells`, `PlayerName`, `PlayerKnownName`, `PlayerNameOverride` ignored): `PlayerNameService.cs` (72 members), `PlayerKnownNameService.cs`, `DisguiseService.cs`, `PlayerDescriptorService.cs`, `CommunicationService.cs`, `PlayerListService.cs`, `ChatPortraitTellsService.cs` and the five content files: 426 members, 409 identical, 14 different, 3 missing, every one intended. Different: the constructors of `PlayerNameService`, `PlayerKnownNameService`, `DisguiseService`, `PlayerDescriptorService`, `CommunicationService`, `PlayerListService` and `ChatPortraitTellsService` (a service added or swapped); `AfkPlayers` (the `[ReloadCarriedFrom]` attribute); `RefreshNameOverridesForObserver` (the `Remove` line is `ForgetDescriptorSetting`); `PlayerListService` field, `BuildEntries` and `ResolveName` (the `.Value` of the plain `IPlayerNameService`); `ChatPortraitTellsService` field and `GetColoredDisplayName` (the same). Missing: the fields `_playerList` and `_chatPortraitTells` of `PlayerNameService` and `_playerName` of `CommunicationService` (renamed `_playerNameOverride`). Every moved member is identical, as are the content members that changed property name.
  - *Unit tests*, `SWLOR.Game.Server.Tests --no-build`: filters `PlayerName`, `PlayerKnownName`, `PlayerIdentity`, `Afk`, `Disguise`, `PlayerDescriptor`, `PlayerIntroduction`, `Conversation`, `PlayerTell`, `ChatPortraitTell`, `PlayerList`, `Communication`, `PlayerFeedback`, `Handler`, `PlayerSession`, `ReloadStateCarrier`, `ReloadDigest`, `ReloadCarry`, `ReloadAdoption`, `PlayerMessageAudit`, `ConvertedServiceContainer`, `LazyDependencyCycle`, `InvertedInterface`, `ClockSeam`, `PlayerFacing`, `ScriptHandlerOrder`, `SplitBootHandler`, `BootHandler`, `Snippet`, `CharacterCompanions` (without `ConversationArchitecture`, which reads the module JSON for minutes and is in the full run): 2,035 tests, the three hak tests and the message inventory failed, the other 2,030 passed (the two source needles that failed were fixed after). Whole suite, unfiltered: 20,074 tests, 19,801 passed, 270 failed, 3 skipped. 246 of the 270 fail identically at the base (the same 273 test cases rerun at `d8611b8ab`: 246 failed, 27 passed): 231 read hak art or 2DA files from the empty `SWLOR_Haks`, the rest are source and content tests that resolve the repository root to the main checkout. The 24 that fail only at the head: 19 DM possession and telegraph tests whose harness named the old `PlayerName` shim for `GetChatDisplayName` (fixed, 39 DM possession, `CountHostiles` and `TargetedArea` tests pass), 5 `CountHostiles` and `TargetedArea` tests that hit a locked file while a second build ran (pass on rerun), and `MessageInventory_CoversEveryRuntimeCallSite` (the audit file, above). `SWLOR.Architecture.Tests`: 298 pass. `PlayerNameOverrideService.cs` line coverage 98.3% from the part's tests (floor `Domain/Character` 85).
  - *Engine tests*, host entry, dev server module and haks, scratch home, filter `Characterization|PlayerName|AfkName|PlayerList|PlayerDataCache|DMPlayerExamine|DMPossessedName|Disguise|Identity` (70 tests each), at `d8611b8ab` and at the head: base 69 pass, 0 fail, 1 skipped (the opt-in `ViewModelTranscript`); head 68 pass, 1 fail, 1 skipped. The failure is `Chat portrait hook cost per chat line` (a timing limit, "hook adds 19.02 us per chat line") on a machine running several stacks; rerun once alone on a quieter machine it passes at the base (5.2 s) and at the head (4.6 s), so it is load, not a difference. The other 69 outcomes are identical, `afk-name-snapshots.json` is byte-identical and `dm-examine-native-states.json` differs in one generated id. The reports were read with node.
  - *Hot reload*: `scripts/run-hot-reload-check.sh --engine-filter HotReloadPlayers --reloads 2` at the head (a scratch copy with its own project name and a longer digest wait, the machine took 2 to 4 minutes per reload pause): 2 reloads applied, every digest matched (257 cache and 86 adopted digests per generation), and both `HotReloadPlayers` tests passed in all three generations. `HotReloadNamesEngineTests` checks after each reload that the AFK player is still AFK with the tag the observer sees, that a typed tell from the observer to the player they named still goes through this build's tell handler (the observer sees the chat name while it is delivered, then the restored name), and after the last reload that moving the player clears AFK through the movement check the adopter started again. A first version also left a stale name override on the player and expected the adopter to replace it; the headless players keep it, at the base too (the same scenario against `d8611b8ab` fails the same way: the adopter's reapplication does not reach a headless player), so that check is not in the test and "the overrides are reapplied" is not shown for headless players. `ReloadStateCarrierTests` (the `CarryAll` and `AdoptAll` round trip, now on `PlayerNameOverrideService`), `ReloadCarryHistoryTests` and `ReloadDigestTests` pass.
  - *Client evidence*, base and head Release builds of the same module and haks with a temporary driver outside the repository (`Server` test environment, `ooc_area`; the driver wraps the services and writes status lines; `/afk` is typed in the client, names, tells and disguises go through the real services): client 1 Agan Tarn (observer), client 2 zabb, client 3 bothan male, the second and third on the same key renamed after each entered (the owner copied the key files). Identical steps at both builds. (1) One client: `/afk` on and off, the Player List showing `Agan Tarn [AFK]` and the tag gone, the override the engine holds for the player (`Agan Tarn [Notable Humanoid] [AFK]` and without the tag). (2) Two clients: strangers shown by descriptor, `Zed Vorn [Unassuming Humanoid]` after Agan names zabb (Player List and the lookups: `GetDisplayName`, `GetColoredDisplayName`, `GetChatDisplayName` `Zed Vorn`), `Zed Vorn [AFK]` in the list while zabb is AFK, a typed tell and a Player List tell from Agan to zabb arriving in zabb's chat under the sender's descriptor (`Notable Humanoid: [Tell]`), a disguise (the observer sees only its descriptor) and back, and forgetting the name. (3) Three clients: tells through the handler with three players online, before and after naming. The driver status lines normalized (timestamps and ids removed, the third player's lines left out where it was not yet in) are identical at base and head in all three scenes, and the Agan and zabb chat tell lines are the same. **Limits:** the tell to an ambiguous name was not reproduced live: it needs two targets with the same display name for the sender, which here means the same public descriptor or the "hide descriptors beside names" setting, and the driver could not set either without a server restart (a disguise slot limit, and a database cache the setting write did not reach); the ambiguous refusal is covered by the `PlayerTell.txt` transcript step "a name two players share is refused with a hint", `PlayerNameOverrideServiceTests` and the `AfkNameEngineTests`. Cleared-on-exit was not shown live (the exit scene lost its AFK on the second and third client, and re-entry needs a new key copy): `Leaving_the_module_clears_afk` and `Entering_the_module_clears_a_stale_afk_flag` cover it. The first run's files, which also had the three-client and disguise scenes and the engine and reload reports, were deleted by mistake in the final cleanup; the results above stand as that run's. The owner then asked for a reduced capture of the first two scenes, base and head, same module, haks, characters and steps (agantarn and zabb1, two clients on the same key, driver rebuilt for both): one client `/afk` on and off with the Player List showing `Agan Tarn [AFK]` and the override `Agan Tarn [Notable Humanoid] [AFK]` then without the tag; two clients with the stranger shown by descriptor, `Zed Vorn [Unassuming Humanoid]` after naming, `Zed Vorn [AFK]` in the list, and a typed tell and a Player List tell arriving in zabb's chat under the sender's descriptor. The driver status lines (scene 1, 6 lines; scene 2, 27 lines; timestamps and ids removed) are identical at base and head, and so are the tell and AFK chat lines of both clients. Kept in `C:/Temp/mr-p5-10f2/evidence/{base,head}/`: the screenshots (`base-*.png` and `head-*.png` for client 1, `evidence2-*` for client 2), `scene1-status.txt`, `scene2-status.txt`, `chat-client1.txt`, `chat-client2.txt`, and the token comparison in `head/`. The owner copied the key files and removed them; no copy is left.

### P5-10h1 as built (2026-11-25)

Branch `claude/mr-p5-10h1`, base `a26222f9b` (the feature branch after the master sync to 33d1a0f19). `BeastMasteryService` 1,240 -> 901 lines (14 -> 10 services: it no longer holds the field notes, the property service, the AI service or the combat recorder); it is off `LargeTypes.txt` and `ServicesPerType.txt`.

- **Parts.**
  - `BeastSpawnService : IBeastSpawnService` (`Domain/Companions`, 161 lines, 5 services: `IDBService`, `IAIService`, `ICombatRecorderService`, `IPerkService`, `IBeastMasteryService`; 107 member lines moved): `SpawnBeast`, `GetCompanionSlotValidationError`, the `SyncGrantedFeats` seam (still over `IPerkService`; P5-10d re-points it) and the private `BeastResref`. No handler and no state. New `GameServices.BeastSpawn`.
  - `BeastIncubationService : IBeastIncubationService` (299 lines, 8 services: `IDBService`, `IPropertyService`, `IIncubationFieldNoteService`, `IResistanceService`, `ICombatDamageCatalogService`, `IPerkService`, `IGuiService`, `IBeastMasteryService`; 213 member lines moved): `UseIncubator`, `DetermineMutation`, the four day-override members, both `CreateBeastEgg`, `CalculateEggVendorBonus`, `GetResistancePurity`, `OnRemoveProperty`, `UseExtractDNAObject`, `_incubationDayOverrides` (`[ReloadState(Runtime)]`) and two seams of its own (`GetPerkLevel`, `TogglePlayerWindow`). New `GameServices.BeastIncubation`. The three handlers keep their events and their positions in `ScriptHandlerOrder.txt`; `HandlerRenames.txt` edits their three lines in place. No message text, clock read or `BeginSetupAttribution` call changed (`SpawnBeast` keeps its call text).
  - `IBeastMasteryService` loses the moved members and gains `ApplyStats` (public on the class, where `SpawnBeast` reaches it and `GiveBeastXP` still calls it in place). `IsBeastEgg`, `IsIncubationCraftingItem` and `GetIncubationPercentageById` stay.
- **Callers.** Seven content files (`CallBeastAbilityDefinition`, `ReviveBeastAbilityDefinition`, `DMSpawnChatCommand`, `IncubationTestChatCommand`, `DroidControlItemDefinition`, and the two incubator window services, which take `IBeastIncubationService`), `MutationRequirementDayOfWeek` (Domain), three engine test files and the unit tests change property name or constructor only. `IncubationFieldNoteService`'s `Lazy<IBeastMasteryService>` is a plain parameter (its `DeferredResolution.txt` line is gone). `RemovedFacades.json` lists the ten members under owners `BeastSpawn` and `BeastIncubation`.
- **Departures from the plan.**
  - `BeastSpawn` and `BeastIncubation` are their own `ConvertedServices.json` entries and `DomainMap.json` services (as f1 and f2 found), and `tools/Refactor/DomainOrder/py/upward.py` matches the names.
  - `GameServices` is 1,000 lines with the two new properties, and the type limit is "over 1,000": the properties carry no `///` summary (the other properties do) because a documented pair made it 1,004 and put `GameServices` on `LargeTypes.txt`. Every later PR that adds a part adds a property, so the next one crosses the limit; the coordinator decides how (P5-15 deletes the class).
  - `BeastResref` moved private with `SpawnBeast`, and `BeastIncubationService` has a private alias `BeastEggResref = BeastMasteryService.BeastEggResref` so `CreateBeastEgg` keeps its text.
  - Size: 901 lines, not about 890.
  - A new engine test, `HotReloadIncubationEngineTests` (category `HotReloadPlayers`, constructor-injected, no script handler), because the reload check needs the three handlers and the override checked after each reload.
  - `PlayerMessageAudit.json` is not regenerated: seven rows of `BeastMasteryService.cs` (the companion-slot refusal and the missing-beast line of `SpawnBeast`, the four messages of `UseIncubator` and the DNA extractor line) now belong to `BeastSpawnService.cs` and `BeastIncubationService.cs`, so `MessageInventory_CoversEveryRuntimeCallSite` fails until the owner runs `tools/ExportPlayerMessageAudit.ps1`.
- **Tests.** Old test names are kept. `BeastMasteryServiceTests` keeps the catalog, XP, stat, event and item tests (71 test methods); its world, `SetUp` and helpers moved to the abstract `BeastTestBase`, and `BeastSpawnServiceTests` (11) and `BeastIncubationServiceTests` (23: the DNA object, incubator, egg and mutation tests) derive from it (105 in all, as before). `TestBeastMastery.Spawn` and `.Incubation` return real `TestBeastSpawnService` and `TestBeastIncubationService` doubles built over the stay-part double (the feat sync, the perk read and the window write to the one recording list); `TestBeastMastery.Shared.cs` lost the feat seam. `MimicryBeastDroidSource.Beast` lists the two part files and interfaces. `BeastIncubationTranscriptTests.Scene.Incubation` is `IBeastIncubationService` (leaf list and labels unchanged, so no transcript changed). Re-pointed: `BeastSpawnHealthTests`, `DMChatCommandTests`, `IncubationTestToolsTests`, `IncubationFieldNoteServiceTests`. New: `HotReloadIncubationEngineTests`.
- **Baselines** (`SWLOR_UPDATE_SNAPSHOTS=1`): `LargeTypes.txt` loses `BeastMasteryService`; `ServicesPerType.txt` loses `BeastMasteryService` (14); `DeferredResolution.txt` loses `IncubationFieldNoteService.cs -> Companions`; `StaticLookups.txt` follows the test split (`BeastMasteryServiceTests.cs` 10 -> 3, new `BeastIncubationServiceTests.cs` 8, `BeastSpawnServiceTests.cs` 1, `BeastTestBase.cs` 1); `DebtReport.md` regenerated. `CrossDomainEdges.txt` and `EdgesIntoCoreAndPlatform.txt` changed in member counts only (checked: no edge added or removed) and are put back. `UpwardEdges.txt`, `DomainCycles.txt`, `LongMethods.txt` and `LogicToContent.txt` do not change. `ScriptHandlerOrder.txt` and `HandlerRenames.txt` change three class names in place; the reload and session snapshots hold no line for the three handlers or the field, so none changed.
- **Proof.**
  - *Moved and changed bodies* (Roslyn token comparison, `a26222f9b` against the head; modifiers and the receivers `_beastMastery`, `_beastSpawn`, `_beastIncubation`, `BeastMastery`, `BeastSpawn`, `BeastIncubation`, `GameServices` ignored): `BeastMasteryService.cs` (110 members) against the three part files, `IncubationFieldNoteService.cs`, `MutationRequirementDayOfWeek.cs`, the five content files, and the two incubator window services: 216 members, 207 identical, 9 different, 0 missing. The nine are intended: the constructors of `BeastMasteryService`, `IncubatorJobService` and `IncubatorPanelService` (a service added or removed), and the constructor, the field and the two reads of `IncubationFieldNoteService` (the `Lazy<>` and its `.Value`). Every moved member (`SpawnBeast`, `GetCompanionSlotValidationError`, `SyncGrantedFeats`, `UseIncubator`, `DetermineMutation`, the four day-override members, both `CreateBeastEgg`, `CalculateEggVendorBonus`, `GetResistancePurity`, `OnRemoveProperty`, `UseExtractDNAObject`, `_incubationDayOverrides` with its attribute, `BeastResref`) and the promoted `ApplyStats` are identical. The report is `evidence/head/token-comparison.txt`.
  - *Unit tests*, `SWLOR.Game.Server.Tests --no-build`: filters `Beast`, `Incubat`, `Mutation`, `DNA`, `Egg`, `Stable`, `Companion`, `FieldNote`, `PlayerMessageAudit`, `ConvertedServiceContainer`, `LazyDependencyCycle`, `InvertedInterface`, `ClockSeam`, `CharacterCompanions`, `ScriptHandlerOrder`, `Reload`, `PlayerSession`, `BootHandler`, `SplitBootHandler`: 1,831 tests, 1,823 passed, 8 failed (seven read a 2DA of the empty hak submodule, and `MessageInventory_CoversEveryRuntimeCallSite`). An earlier, wider run that also had `Property` and `Handler` (2,368 tests) failed 12, none of which was listed before it ended; the whole run below names every failure. Whole suite, unfiltered: 20,177 tests, 19,921 passed, 249 failed, 7 not executed. 232 of the failures read a 2DA, shader, tileset, model or texture from the empty `SWLOR_Haks` submodule of the worktree (CI stages a sparse checkout with the 2DAs and excludes only the art tests of `scripts/ci/run-unit-tests.sh`, so this list is longer than CI's). Of the other 17, 16 fail the same at `a26222f9b` (rerun there: 16 of 17 fail, 1 passes): source and content tests that resolve the repository root to the main checkout and read files of the old layout, or the module, plus `StaticPlaceables_AreAnchoredInTheRoomTheyAreSeenFrom` and `HeadquartersFloors_AreNotLaidOverVoidFillerTiles` (empty module resources). The 17th, `MessageInventory_CoversEveryRuntimeCallSite`, fails only at the head, for the audit file above. `SWLOR.Architecture.Tests`: 298 pass. Coverage of the parts from the whole run: `BeastSpawnService.cs` 96.3% and `BeastIncubationService.cs` 97.0% (floor `Domain/Companions` 88; `BeastMasteryService.cs` 89.6%).
  - *Engine tests*, host entry, dev server module and haks, scratch homes, filter `Characterization|Incubation|CapstoneProof|TankEnmity` (54 tests each), at `a26222f9b` and at the head: base 53 pass, 1 fail; head 54 pass. The base failure is `Area taunts recover multiple enemies and replace pending approaches` (a 10 s timeout waiting for a taunted NPC to attack, on a machine running other stacks); it passes at the head, and alone with the sweep below it passes at the base too (12 s), so it is load, not a difference. Every other outcome is identical for all 54 names; no characterization snapshot changed. The `Beastmaster ability behaviors` sweep (two parts; the Call Beast, Revive Beast and Tame cases call `SpawnBeast`) was run separately at both builds, concurrently, with the taunt test: 3 of 3 pass at the base and at the head, with durations within 1 s of each other. The reports were read with node.
  - *Hot reload*: `scripts/run-hot-reload-check.sh --engine-filter HotReloadPlayers --reloads 2` at the head (a scratch copy with its own project name): 2 reloads applied (pauses 84 s and 78 s), every digest matched (259 boot lines, 86 quiescence lines per generation), and the three `HotReloadPlayers` tests passed in all three generations. `HotReloadIncubationEngineTests` sets a weekday override in each build and checks, after each reload, that the previous build's override is not carried (`_incubationDayOverrides` is `Runtime`), that exactly one `BeastIncubationService` handler is registered for each of the incubator terminal, the DNA extract object and the property deletion events and is bound to this build's instance, and that the property deletion event, signalled as the property service signals it, removes the property's jobs. The log is `evidence/head/hot-reload-check.log`.
  - *Client evidence*, base and head Release builds of the same module, haks and test character (agantarn), one client, a temporary driver kept outside the repository (it reads a command file on each module heartbeat and calls the real services by method name, so one command file drives both builds; `Server` test environment). Identical steps at both builds, in the starting area: (1) a stored beast (Aradile, level 5, Poison Breath II) is called through `SpawnBeast`: the beast Rex appears next to the player and in the party bar; its name, appearance, portrait (4279), sound set (442), hit points (140/140), ability scores, granted feats (Poison Breath II), the twelve event scripts, and every item property of its skin and claw are the same, and the stored record differs only by its creation time; (2) a second call with the beast out: "Only one companion may be active at a time." in the chat and from `GetCompanionSlotValidationError`; (3) using an incubator placeable (`incubator`, registered with a property id) without the DNA Manipulation perk: "Perk 'DNA Manipulation I' is required to use incubators."; (4) with the perk: the Incubator window opens with "Server day: Wednesday (UTC)"; (5) a day override to Friday: the window reads "Server day: Friday (test)", and clearing it returns the server day; (6) a finished job made into a beast egg through `CreateBeastEgg`: "Acquired Item: Beast Egg", the egg's name, vendor value (266) and eight item properties (DNA type, four purities, XP penalty, one resistance purity, the cast spell) are the same, and the job is deleted (1 -> 0); an egg by beast type (Bearbug) is the same; (7) a DNA extraction object: "Use a DNA Extractor on this corpse to retrieve its DNA." in the chat; (8) the property deletion event signalled as the property service signals it removes both stored jobs of the property (2 -> 0). The driver status lines (71 lines, with the service name of the call lines and ids removed) and the client chat are identical at base and head. **Limits:** the incubator is opened by the player interacting with a placeable the driver made (a real property incubator was not set up), so the stage-start and item-drop paths of the window were not exercised; `SpawnBeast` was called by the driver, not through the Call Beast ability (the engine ability sweep does call it). Screenshots, status lines and chat logs: `C:/Temp/mr-p5-10h1/evidence/{base,head}/`.

### P5-10e1 as built (2026-11-25)

Branch `claude/mr-p5-10e1`, base `a26222f9b` (the feature branch after the master sync to 33d1a0f19). `SkillService` 1,182 -> 819 lines (3 services: `IDBService`, `ITwinPistolEquipmentService`, `ICompanionLookup`); it is off `LargeTypes.txt` and `GiveSkillXP` is off `LongMethods.txt`.

- **Part.** `SkillXPService : ISkillXPService` (`Domain/Character`, 438 lines, 5 services: `IDBService`, `IGuiRefreshPublisher`, `IPlayerFeedbackService`, `Lazy<IStatService>`, `ISkillService`; about 347 member lines moved): `GiveSkillXP` (102 lines) cut into `ApplyXPBonuses`, `RepayXPDebt`, `ClearXPAtRankCeiling` and `AwardRanks` with a private `RankUpOutcome` carrying the rank-up flag, the XP spent on ranks and the modified and decayed skill lists (the running rank total and the decay candidates are arguments of `AwardRanks` and stay inside it); `ApplyAbilityPoint`, the handlers `ResetLimitWarningsOnEnter` and `ResetLimitWarningsOnExit` with `ForgetLimitWarnings`, `CalculateEspionageXP`, `GetPracticeXP`, `GetDeltaXP`, the seams `GetExperiencePercentAdjustment` and `ScheduleXPMessages`, and the fields `_limitWarningsSent` and `_xpMessages` (both `[ReloadState(Runtime)]`; the queue is built in the constructor with a closure that reads `GetSkillDetails` through `ISkillService`). The interface has four members. New `GameServices.SkillXP`. No clock read moves, so `ClockSeamFiles.txt` does not change.
- **Stay-part and ports.** `GetDecayCandidates` and `ShouldDecaySkillRank` are public on `SkillService` and on `ISkillService` for SK-3 until e2 moves them. `SkillService` lost `_gui`, `_playerFeedback`, `_stat` and the queue; `ISkillService` lost `GiveSkillXP` and the three formulas.
- **Holders and callers.** `MimicryService`, `EspionageInfiltrationService`, `SlicingSessionService`, `GuildTaskXpService`, `PerformanceAudienceService` and `TrapsService` take `ISkillXPService` in place of `ISkillService` (they used nothing else: the map named five, `PerformanceAudienceService` is the sixth); `LanguageService` (3 -> 4 services), `CombatPointService` (7 -> 8) and `CraftService` (5 -> 6) keep `ISkillService` and gain it; `XPTomeDialog` gains it. The calls through the facade changed property name only (`Skill.GiveSkillXP` -> `SkillXP.GiveSkillXP`): `GatheringLevels`, `ResearchXpRules`, `NwnCraftSessionWorld`, `NwnScavengeWorld`, `NwnFishingFightWorld`, `FishingFightLifecycle`, `DistributeRPXPViewModel`, `RefineryViewModel`, `MimicryQuestDefinition`, `ScavengePoint`, `MiningLaserModuleDefinition`, `StripMinerModuleDefinition`. `RemovedFacades.json` lists the four members under owner `SkillXP`. `SWLOR.CraftSim`'s `ServerResearchRules` builds `CraftService` with one more `null`.
- **Departures from the map.**
  - **`Lazy<IStatService>` stays, on `SkillXPService`.** With a plain `IStatService` the container never finishes: `StatService -> ... -> ResistanceService -> MimicryService -> ISkillXPService -> IStatService` closes through the alias factories (`IStatusResistance`, `IResourceRestoredListener`), which the container does not detect, so a test host that built any service of the loop hung with no CPU use (two hang dumps showed the two alias factories calling each other). The map's cycle table assumed `MimicryService` no longer reaches the XP part; it does until g2 gives the witness part `ISkillXPService` and the stay-part stops holding it. So `SkillService.cs` leaves `DeferredResolution.txt` and `SkillXPService.cs -> Character (inside) | 1` takes its place; g2 turns it plain.
  - `SkillXP` is its own `ConvertedServices.json` entry and `DomainMap.json` service, as f1 and f2 found for their parts. `tools/Refactor/DomainOrder/py/upward.py` already matches `Skill*`.
  - `SkillXPAwardTests` and `SkillXPMessageQueueTests` stay as classes: their harness is shared with `SkillRankLimitTests`; only the award tests of `SkillServiceTests` moved.
  - `GameServices` partials were at 998 lines; the new property would have put the type on `LargeTypes.txt` (1,001), so the `Skill` property's doc is one line.
  - `KillScenarios` and `SkillXPChartCharacterizationTests` name `Skill.GiveSkillXP` and `Skill.GetDeltaXP` in snapshot titles; the strings stay so no snapshot changes.
  - `PlayerMessageAudit.json` is regenerated with `tools/ExportPlayerMessageAudit.ps1` in a later commit on the branch: all 9 `SkillService.cs` rows (the queue message, the debt, rank-up, ability point and the two warnings) belong to `SkillXPService.cs` now, file and line only. Before that commit, `MessageInventory_CoversEveryRuntimeCallSite` failed at the head, as the unit test results below record.
  - A new engine test, `HotReloadSkillXPEngineTests` (category `HotReloadPlayers`, constructor-injected, no script handler), because the map's reload recheck needs a player who stays online (below).
- **Tests.** Old test names are kept. `SkillServiceTests` keeps the catalog, rank and weapon tests (38); the award tests, the XP chart formulas and the default seams (22 of the old 60) moved to `SkillXPServiceTests` (23 with a new default-seam test), both over the abstract `SkillTestBase` (the shared world, `SetUp` and trace helpers). `TestSkillService` lost its XP seams; `TestSkillXPService` carries them and is the `XP` accessor, built over the catalog double. `CharacterCompanionsSource.Skill` lists `SkillXPService.cs` and `ISkillXPService`; the `SkillXPAwardTests` harness now borrows `GiveSkillXP` and its four steps with the nested `RankUpOutcome` and the receivers of the part interface stripped; the needles of `DeadPlayerXPTests` (the first statement after the guard) and `SkillRankLimitTests` (the handler type, the confirm call) follow the move, `DecayCandidates` and `SkillRankDecay_OnlyOccursAboveTheContributingSkillCap` call the now public members. `MimicryWitnessTranscriptTests` adds the `ISkillXPService` leaf: the ten `Skill.GiveSkillXP` lines of `MimicryLearn.txt` take the label `SkillXP.GiveSkillXP` (regenerated with `SWLOR_UPDATE_SNAPSHOTS=1`; the diff is those ten lines and nothing else). `SkillXPAward.txt` and `PlayerTell.txt` are unchanged. `CraftCalc` (characterization) builds the part for `CraftService`. `PlayerSessionClassification.txt`, `ScriptHandlerOrder.txt` and `HandlerRenames.txt` change the two class names in place (event and position unchanged); the reload snapshots, `BootHandlerClassification.txt` and `ClockSeamFiles.txt` do not change.
- **Baselines** (`SWLOR_UPDATE_SNAPSHOTS=1`): `LargeTypes.txt` loses `SkillService` (1,182); `LongMethods.txt` loses `GiveSkillXP` (239); `DeferredResolution.txt` moves its one `Lazy<IStatService>` line from `SkillService.cs` to `SkillXPService.cs`; `DebtReport.md` regenerated (large types 11 -> 10, long methods 8 -> 7). `CrossDomainEdges.txt` and `EdgesIntoCoreAndPlatform.txt` changed in member counts only and are put back. `UpwardEdges.txt` (the `SkillService.cs` Combat edge stays for e2), `ServicesPerType.txt`, `StaticLookups.txt`, `LogicToContent.txt` do not change.
- **Proof.**
  - *Moved and changed bodies* (Roslyn token comparison, `a26222f9b` against the head, modifiers and the receivers `_skill`, `_skillXP`, `Skill`, `SkillXP` and `GameServices` ignored; the changed production files, 771 members): 751 identical, 20 different, every one intended. Different: the eleven constructors that gain or swap `ISkillXPService` (`SkillService`, `MimicryService`, `EspionageInfiltrationService`, `LanguageService`, `CombatPointService`, `CraftService`, `SlicingSessionService`, `GuildTaskXpService`, `PerformanceAudienceService`, `TrapsService`, `XPTomeDialog`), the six `_skill` fields whose type is now `ISkillXPService`, `RegisterAbilitiesToCharacter` (the registration line), `ServerResearchRules.Predicate` (one more `null`) and `GiveSkillXP`. Every other moved member is identical: the formulas, the handlers, `ForgetLimitWarnings`, `ApplyAbilityPoint`, both seams, both fields, `GetDecayCandidates` and `ShouldDecaySkillRank`. `GiveSkillXP` statement by statement (leaf statements and conditions): 108 at the base, 114 at the head, 97 identical; the 11 that differ are the loop locals moved into `RankUpOutcome` (`modifiedSkills`, `receivedRankUp`, `decayedSkills` and `xpSpentOnRanks` declarations and updates, and their three uses after the loop read `outcome.`), and the new statements are the four calls, the two returns of the helpers, `return outcome` and the three aliases of the lists.
  - *Unit tests*, `SWLOR.Game.Server.Tests --no-build`: the brief's filter terms (`Skill`, `XPAward`, `Decay`, `RankLimit`, `Mimicry`, `Espionage`, `Language`, `CombatPoint`, `Craft`, `Scavenge`, `Slicing`, `GuildTask`, `Performance`, `Fishing`, `Traps`, `XPTome`, `RPXP`, `RoleplayXP`, `Refinery`, `Mining`, `Handler`, `SessionClassification`, `Reload`, `PlayerMessageAudit`, `ConvertedServiceContainer`, `LazyDependencyCycle`, `InvertedInterface`, `ScriptHandlerOrder`, `DeadPlayerXP`) select 4,305 results in the unfiltered run below: 4,290 passed, 15 failed (14 read hak folders or resolve the repository root to the main checkout, and `MessageInventory_CoversEveryRuntimeCallSite`, above). Whole suite, unfiltered: 20,171 tests, 19,919 passed, 249 failed, 3 skipped (27 minutes). 248 of the 249 fail the same way at `a26222f9b` (rerun by name in a scratch worktree at the base: 248 failed, 1 passed): 228 read hak art or 2DA files from the empty `SWLOR_Haks`, and 20 are source tests that resolve the repository root to the main checkout or need the haks; 120 of the 249 are in `scripts/ci/run-unit-tests.sh`'s exclusion list (they read hak folders the CI job does not fetch) and the other 129 read `sw_2da`, `sw_tlk` or `sw_ability`, which the CI job fetches and this worktree's empty submodule lacks. The one failure only at the head is the message audit. `SWLOR.Architecture.Tests`: 298 pass. `SWLOR.Game.Server.CharacterizationTests`: 148 pass, no expectation changed. `SWLOR.Game.Server.IntegrationTests` through `scripts/ci/run-integration-tests.sh`: 146 passed, 1 skipped (the production-copy test). `SkillXPService.cs` line coverage 92.6% from `SkillXPServiceTests`, `SkillXPAwardTests`, `SkillXPMessageQueueTests`, `SkillRankLimitTests`, `SkillXPTranscriptTests` and `SkillServiceTests` (floor `Domain/Character` 85). CraftSim cites no line of `SkillService.cs`, but four of its anchors name `Skill.GetDeltaXP` and `Skill.GiveSkillXP` in files this change edited (`LegacySource.cs`: harvest, scavenge, refining and fishing XP); they read `SkillXP.` now, the whole of `SWLOR.CraftSim.Tests` passes (1,092 passed, 5 skipped; its `CraftService` construction and two test calls changed too).
  - *Engine tests*, host entry, dev server module and haks, scratch home, filter `Skill|XP|Characterization|MimicryObservation|LanguageToken|Espionage|Trap|Scavenge|Slic|Craft|Fishing|CombatPoint` (163 tests at the base), at `a26222f9b` and at the head: base 163 passed; head 163 passed and 1 skipped (the new reload scenario, which skips without hot reload), per-test outcomes identical for all 163 names, no characterization snapshot changed. The set includes `Characterization: kill awards skill XP and leaves corpse loot` (`KillScenarios`), the two `SkillXPAwardDecay` cases, `BareHandedAttackSkill`, `TwinPistolCrossSkill`, `WeaponCrossSkill`, `MimicryObservation`, `LanguageToken`, `Espionage`, the trap, scavenge, slicing, craft and fishing tests, the `SupportXP` and `RoleplayXP` categories. The reports were read with node.
  - *Hot reload*: `scripts/run-hot-reload-check.sh --engine-filter HotReloadPlayers --reloads 2` at the head (a scratch copy with its own project name and a longer digest wait; the pause was 105 and 99 seconds): 2 reloads applied, every digest matched (259 cache and 86 adopted digests per generation), and the three `HotReloadPlayers` tests passed in all three generations. `HotReloadSkillXPEngineTests` puts a player at a rank limit and checks in every generation that an award adds nothing, that the build's `_limitWarningsSent` holds one entry for the skill however many awards follow, and that the build's `_xpMessages` still combines two awards of one source into one message with the combined XP. **What it found:** `_limitWarningsSent` is `[ReloadState(Runtime)]` (as on `SkillService` at the base): a reload starts the set empty (the test records "not recorded in the new build before its first award" after each reload), so the limit warning is shown once more to a player who stays online across a reload, once per build. The map's "stays sent" is therefore not true of a reload; nothing was changed (the field's marking is the map's, and the base does the same).
  - *Client evidence*, base and head Release builds with a temporary driver kept outside the repository (it parses a command file, prepares the player record, calls the real service, spawns and kills a creature and prints the skill records; the XP, messages, warnings and the tome dialog are the game's), the same module and haks, character `agantarn.bic`, one client, `SWLOR_ENVIRONMENT=test`, GPU client (`veles_sewers`, the player on plot). Identical steps at both builds: (1) a kill: `looter_1` with Force combat points, "You earned Armor skill experience. (788)" and "You earned Force skill experience. (592)"; (2) a rank-up: Smithery at rank 2 awarded 1100 XP, "You earned Smithery skill experience. (1100)" and "Your Smithery skill level increased to rank 3!"; (3) XP debt: "Your XP debt has been cleared." and "You earned First Aid skill experience. (80)"; (4) the rank limit: two awards at the limit, "Piloting is at your limit of 5. Raise it in the Skills window." once, then the player kicked from the console and the client brought back through the tool, and the first award of the new login shows the warning again; (5) skill decay at the cap: 400 ranks with one unlocked skill, a first rank in Fabrication, Piloting 40 -> 39 and the earned and rank-up lines; (6) a non-combat source: the Minor XP Tome used from the inventory, the real conversation (Utility, Leadership, Select this skill), "You earned Leadership skill experience. (500)". The normalized chat lines (timestamps removed) and the driver's status lines (HP removed) are identical at the base and the head (0 differing lines each), and the screenshots show the same chat panel and conversation. **Limits:** the kill gives no skill an XP message through the combined queue (only Mimicry passes a message source, so the queue path is covered by the engine test and `SkillXPServiceTests`, not by this client run); decay shows in the skill records and the earned lines (the game prints no message for the decayed skill); the driver's `award` calls the service directly for scenes 2 to 5, and the tome is opened from the driver (`ActionUseItemOnObject`), not from the inventory radial. Screenshots, normalized chat, status lines, the token comparison and the engine reports: `C:/Temp/mr-p5-10e1/evidence/{base,head}/`.

### P5-10h2 as built (2026-11-25)

Branch `claude/mr-p5-10h2`, base `1496f66a5` (the feature branch after P5-7h1). `BeastMasteryService` 901 -> 646 lines (10 -> 7 services: it no longer holds the companion control, the item service or the status effects); the new part is 297 lines with 7 services.

- **Part.** `BeastEventService : IBeastEventService` (`Domain/Companions`, 297 lines, interface 84; 7 services: `IDBService`, `ICompanionControlService`, `ICreatureStatService`, `IStatusEffectApplicationService`, `IItemService`, `IGuiService`, `IBeastMasteryService`; 28 members moved): the twelve `BeastOn*` handlers, `OpenStablesMenu` (on `beast_term`), `RemoveAssociate`, `OnAcquireItem`, the `ReturnItem` seam and the twelve seams that stood under them (`ClearCompanionControl`, `InitializeCompanionControl`, `ProcessCompanionCombatRound`, `ProcessCompanionHeartbeat`, `HandleCompanionConversation`, `RegisterDefensiveThreat`, `RecordBeastDeath`, `RestoreBeastStats`, `LoadNPCStats`, `ApplyCreatureMovementRate`, `ApplyRestStatusEffect`, `TogglePlayerWindow`). No state, no clock read, no `[ReloadState]` field. New `GameServices.BeastEvent` (with a summary like its neighbours); `GameServices` stays under its `Exemptions.json` entry. The part calls `IBeastMasteryService.IsPlayerBeast` and `GetBeastId`, which stay on the stay-part (receivers `_beastMastery.` in two places, nothing else changed in a body).
- **Handlers.** 16 handler lines are renamed in place in `ScriptHandlerOrder.txt` (same event, same position) and 15 lines in `HandlerRenames.txt` (`RemoveAssociate` has one line there for its two events). `BeastMasteryService.CacheData` and `CombatPointXPDistributed` keep their lines, so `CombatPointXPDistributed` keeps its order with Mimicry's handler on `cp_xp_distribute`.
- **Callers.** None outside the handlers: no content, native or static call site names a moved member, so `RemovedFacades.json` has nothing to add. `IBeastMasteryService` loses the 15 methods. `ClockSeamFiles.txt` does not change (the part reads no clock). `ConvertedServices.json` (`BeastEvent`), `DomainMap.json` (Companions services) and `tools/Refactor/DomainOrder/py/upward.py` follow the h1 pattern.
- **Departures from the plan.**
  - `ReturnItem` is a thirteenth seam (the map counts twelve); the part is 297 lines, not about 295.
  - `BeastMasteryService` keeps `IGuiService` (the XP refresh event) and `ICreatureStatService` (`SetNPCMaxHitPoints`), so its seven services are the ones the map's BM-1 row lists.
  - A new engine test, `HotReloadBeastEventEngineTests` (category `HotReloadPlayers`, constructor-injected, no script handler), because the reload check needs the 16 handler lines checked after each reload.
  - `PlayerMessageAudit.json` is regenerated with `tools/ExportPlayerMessageAudit.ps1` in a later commit on the branch: four rows of `BeastMasteryService.cs` change (`OnAcquireItem` and `OpenStablesMenu` now belong to `BeastEventService.cs`; the two `AddXP` rows move up 9 lines), file and line only. Before that commit, `MessageInventory_CoversEveryRuntimeCallSite` failed at the head, as the unit test results below record.
- **Tests.** Old test names are kept. `BeastMasteryServiceTests` keeps the catalog, XP, stat and item tests (52 test methods); the 19 event tests moved to `BeastEventServiceTests` over `BeastTestBase`, with three new ones (22 in all): the container builds `IBeastEventService`, and two that run the handlers over a plain `BeastEventService` with `RecordingServiceProxy` stand-ins for the five services, so each seam body is seen reaching its service (the double of the other tests overrides the seams). `TestBeastMastery.Events` returns a `TestBeastEventService` (the seams and `ConversationHandled` moved from `TestBeastMastery.Events.cs`, which is gone; the window seam left `TestBeastMastery.Shared.cs`). `MimicryBeastDroidSource.Beast` lists the new file and interface.
- **Baselines** (`SWLOR_UPDATE_SNAPSHOTS=1`): `LogicToContent.txt` re-keys two lines from `BeastMasteryService.cs` to `BeastEventService.cs` (the `Feature/AbilityDefinition` and `Feature/StatusEffectDefinition` edges); `StaticLookups.txt` gains `BeastEventServiceTests.cs` (1, the container test); `DebtReport.md` regenerated (static lookup 1,081 -> 1,082 files; members both ways 3,473 -> 3,475, `Character <-> Companions` 171 -> 173 members). `CrossDomainEdges.txt` and `EdgesIntoCoreAndPlatform.txt` changed in member counts only (checked: no edge added or removed; they also carry counts from earlier merges) and are put back. `LargeTypes.txt`, `ServicesPerType.txt`, `LongMethods.txt`, `DeferredResolution.txt`, `UpwardEdges.txt` and `DomainCycles.txt` do not change. The reload and session snapshots hold no line for the moved handlers, so none changed.
- **Proof.**
  - *Moved bodies* (Roslyn token comparison, `1496f66a5` against the head; modifiers and the receivers `_beastMastery`, `_beastEvent`, `BeastMastery`, `BeastEvent`, `GameServices` ignored): `BeastMasteryService.cs` (90 members) against `BeastEventService.cs` and the stay-part: 89 identical, 0 different, 1 missing by signature. The one is the constructor of `BeastMasteryService` (ten parameters before, seven after: the companion control, the item service and the status effects left). All 28 moved members are identical. The report is `evidence/head/token-comparison.txt`.
  - *Unit tests*, `SWLOR.Game.Server.Tests --no-build`: filters `Beast`, `Companion`, `Stable`, `Associate`, `AcquireItem`, `Handler`, `Reload`, `PlayerMessageAudit`, `ConvertedServiceContainer`, `LazyDependencyCycle`, `Incubat`, `PlayerSession`, `ClockSeam`: 1,990 tests, 1,982 passed, 8 failed (seven read a 2DA or a model of the empty hak submodule, and `MessageInventory_CoversEveryRuntimeCallSite`). `BeastEventServiceTests` alone: 22 of 22. Whole suite, unfiltered, with coverage: 20,189 tests, 19,937 passed, 249 failed, 3 not executed. 228 of the failures read a 2DA, model, shader, tileset or texture from the empty `SWLOR_Haks` submodule of the worktree (CI stages a sparse checkout with the 2DAs and excludes only the art tests of `scripts/ci/run-unit-tests.sh`, so this list is longer than CI's; `BeastSoundsetsFromTheHaks_ShowNoChatText` is one of CI's own exclusions). Of the other 21, 20 fail the same at `1496f66a5` (rerun there: 20 of 21 fail, 1 passes): source and content tests that resolve the repository root to the main checkout and read files of the old layout, or the module, plus `StaticPlaceables_AreAnchoredInTheRoomTheyAreSeenFrom` and `HeadquartersFloors_AreNotLaidOverVoidFillerTiles` (empty module resources). The 21st, `MessageInventory_CoversEveryRuntimeCallSite`, fails only at the head, for the audit file above. `SWLOR.Architecture.Tests`: 298 pass. The whole run was made before the two seam tests were added (the filtered run above and `BeastEventServiceTests` were made after). Coverage of that run: `BeastEventService.cs` 73.8% (the 39 uncovered lines are the seam bodies, which the old double overrode); with the two seam tests the file is 149 of 149 lines (100%); `BeastMasteryService.cs` 95.6%. `Domain/Companions` was 87.2% in that run, against a floor of 88: the moved seam lines were uncovered in the stay-part before the move too, and the sparse checkout loses the lines the hak tests cover, so the area figure is not new; the new part has its own file figure above. The coordinator's gate run with the haks checked out, after the seam tests, puts `Domain/Companions` at 88.8%.
  - *Engine tests*, host entry, dev server module and haks, scratch homes, filter `Characterization|Beast|Incubat|CapstoneProof|TankEnmity|CombatPerkRegression|Companion` (101 tests at the base, 102 at the head with the new reload scenario), at `1496f66a5` and at the head, run together: base 98 pass, 1 fail, 2 skipped; head 99 pass, 0 fail, 3 skipped. Every outcome is identical for the 100 names the two sides share, except `Area taunts recover multiple enemies and replace pending approaches` (`TankEnmity`): it times out at the base (10 s waiting for the taunted NPC to take the taunter as target) and passes at the head. The test uses no beast code (a player taunts two NPCs); alone, it failed at the base in two reruns (14 s and 17 s) and passed at the head (11.7 s), which I could not explain from the change; the same test failed at the base in the h1 sweep and passed alone there. The skips are `HotReloadPlayers` (off outside the reload check) and the `ViewModelTranscript` incubator scenario (runs only when named). No characterization snapshot changed. The `Beastmaster ability behaviors` sweep (both parts, 43 and 42 cases) passed on both sides. The reports were read with node.
  - *Hot reload*: `scripts/run-hot-reload-check.sh --engine-filter HotReloadPlayers --reloads 2` at the head (a scratch copy with its own project name and a longer digest wait, 1,200 s, because the first boot under load did not log its digests in 300 s): 2 reloads applied (pauses 312 s and 195 s, under load), every digest matched (257 boot lines, 86 quiescence lines per generation), and the four `HotReloadPlayers` tests passed in all three generations, including `HotReloadBeastEventEngineTests`: after each reload exactly one `BeastEventService` handler is registered for each of the 16 event scripts and is bound to that build's instance, exactly one `BeastMasteryService` handler for each of `mod_cache_bef` and `cp_xp_distribute`, and a `beast_rest` run through the script dispatch on a creature puts the rest status effect on it. The log is `evidence/head/hot-reload-check.log`.
  - *Client evidence*: base and head Release builds of the same module, haks and test character (agantarn), one client, a temporary driver kept outside the repository (`SWLOR_P510H2_DRIVER=1`; it reads a command file every second and calls the real services and engine functions by command). Identical steps at both builds, in `veles_sewers`: (1) a stored Dewback beast (Rex, level 10) is called through `SpawnBeast`: Rex appears next to the player and in the party bar, and the `beast_spawn` event counter reads 2 at both builds; (2) a hostile Outlaw (`looter_1`, 78 hit points) is sent at the beast: the attacked, damaged, round end and heartbeat events fire, the beast fights back, and the chat shows the damage in both directions and "Rex killed Outlaw"; (3) `ForceRest` raises no rest event at either build, so `beast_rest` is run through the script dispatch: "Rex receives the effect of Rest"; (4) the blocked, disturbed, user-defined and spell cast events are run through the script dispatch, and the player starts a conversation with the beast: one event each, at both builds; (5) a dagger is created in the beast's inventory: "Beasts cannot hold items." then "Acquired Item: Dagger" (it goes back to the player); (6) the beast is killed with a death effect: the death event and one remove-associate event fire, the creature is gone, and the stored record goes from alive to dead; (7) the record is reset, a new beast is called and the player dismisses it with `RemoveHenchman`: a second remove-associate event, the creature is gone, the stored record stays alive; (8) the player uses a stables terminal the driver created (`bst_stables_term`): one terminal event, and the Stables window opens (the screenshots are the same; it lists two beasts because the run stored two). The driver status lines (30 each, with the hit point numbers and the fight's event counts masked, and the transient area-load lines left out) are identical at base and head, and so are the item, rest and window screenshots. The client chat holds 29 line kinds at the base and the same 29 at the head plus two more, both fight rolls (a missed Precision Shot, a critical Suppressing Shot). **Limits:** the blocked, disturbed, user-defined, spell cast and rest events were raised by the driver through the script dispatch, not by the game; the stables window was opened from a terminal the driver placed, not a placed one, and the "Only players may use this terminal" refusal needs a non-player user, so it was not captured (a unit test covers it); no chat line comes from the death or the dismissal, so those are compared by the stored record and the event counters; the head stack needed `NWNX_THREADWATCHDOG_KILL_THRESHOLD=100` (the first head start under machine load was killed by the watchdog during module load; the base start did not need it). Screenshots, the normalized status lines and the normalized chat: `C:/Temp/mr-p5-10h2/evidence/{base,head}/`.
