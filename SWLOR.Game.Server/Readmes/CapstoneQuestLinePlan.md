# Capstone Quest Line Plan

This plan tracks quest-line rules for locking level 50 capstone perks behind quest completion.

## Global Rules

- Each capstone quest line must require the matching skill at rank 50 before the player can accept any quest in the line.
- Beast capstone quest lines must require the master's `BeastMastery` skill at rank 50 before the player can accept any quest in the line. Do not gate quest acceptance on the currently active beast's level or role; beast role remains a perk-line/content identity requirement, not a quest prerequisite.
- The perk unlock must require completion of the final quest in its capstone line.
- The capstone perk unlocked by the line must be assigned to the final boss of that quest line. For passive or trait capstones, assign the granted feat and the matching `PERK_LEVEL_<perk id>` local so the existing perk-stat path activates; for active capstones, assign the granted feat or spell through the boss ability package.
- Each capstone quest line should use new enemies, items, or encounters when it asks for kills, collection, investigation, or other tasks.
- Each capstone quest line must have its own dedicated quest giver NPC. Area groups may be reused by up to three capstone lines, but the player-facing trainer/requester for each capstone line must be distinct.
- Quest giver NPCs must be placed in safe areas with no enemy spawns (settlements, landings, hubs), never in areas with ambient creature spawn tables or spawn waypoints. Quest journal "return to" text must name the quest giver's actual hub location once the giver is placed.
- Generated capstone `NPCGroupType` identifiers must use the planet prefix from the line's area group, such as `Viscara_` or `Dathomir_`, not an internal `Capstone_` prefix.
- Every generated capstone enemy must have a new first-class NPC signature ability plus a distinct support ability package and a documented resistance profile. Reusable NPC signature abilities live directly under `SWLOR.Game.Server/Feature/AbilityDefinition/NPC`, not under a capstone-specific subfolder. Humanoid enemies must use humanoid, tech, Force, command, or weapon abilities rather than creature-only body attacks; beast enemies should use beast-appropriate attacks, roars, hides, or creature movement abilities.
- Capstone-tier NPC signature abilities must use the existing `RecastGroup.Capstone` cooldown bucket. Do not add duplicate recast groups such as `CapstoneSignature` or `NPCSignature` unless a separate gameplay cooldown is explicitly designed and approved.
- Reusable generated NPC abilities must use behavior/effect-only file, class, feat, TLK, display, and icon names. Do not prefix them with the capstone line, boss, NPC, source category, combat profile, skill family, or area. For example, use `SustainBurnAbilityDefinition`, not `BeastSustainBurnAbilityDefinition`.
- New capstone NPC abilities with feat or spell icons must use a valid source icon, be listed in the gameplay icon manifest, and have matching `pr0_` through `pr5_` cooldown icon assets generated with `tools/GenerateCooldownIcons.ps1` before handoff.
- High-level capstone enemies must not be added to low-level or general public spawn tables. Keep capstone enemies in dedicated high-level encounter tables, gated areas, instances, or other content spaces appropriate for level 50 players.
- If no suitable high-level placement exists, do not mix the enemies into a low-level area as a temporary shortcut. Leave the dedicated spawn tables unwired, mark the quest line as not ready for in-game testing, and add area-builder follow-up tasks.
- Capstone spawn tables must live in the owning planet or location spawn definition and follow the existing spawn-definition style: one named private method per table, direct `_builder.Create("TABLE_ID", "Display Name")`, and explicit fluent `.AddSpawn(...).WithFrequency(...).RandomlyWalks().ReturnsHome()` rows. Do not create capstone-only spawn definition buckets or hide generated spawn rows behind helper methods, catalogs, records, or table-ID constants merely to reduce repetition. Any spawn or loot table ID constants that still exist should remain private unless another production server type truly needs direct access.
- New fixed-placement spawn tables must include matching `Module/utw` waypoint blueprints in `Module/itp/waypointpalcus.itp.json`. The waypoint blueprint `Tag` should be the spawn table ID, while `TemplateResRef` stays within resref length limits.
- Boss encounters should use the reusable quest encounter activator flow instead of ambient spawn tables.
- Quest encounter activators use `OnUsed = quest_enc`, `QUEST_ID`, `QUEST_STATE`, and `QUEST_ENCOUNTER_*` locals so the same object flow can be reused by non-capstone quest encounters.
- Quest encounter activator objects must be hidden unless the player is on the exact quest and state for that encounter.
- Quest encounter activator placeables may be area-instance-only when they are unique quest markers. Do not add one-off boss markers to the placeable blueprint palette just to make them reusable or satisfy tests; keep the world instance as the source of truth unless the area builder explicitly asks for a reusable palette blueprint.
- Quest encounter activator visibility should be refreshed from quest state changes, player area entry, and encounter lifecycle events. Do not add per-object heartbeat polling for these activators unless an engine limitation makes it unavoidable, and document the reason if that exception is ever needed.
- Quest encounter activators must enforce one active creature at a time per encounter, a 60-minute cooldown per player who starts the encounter, participant-based quest credit for eligible players fighting the creature, and idle despawn for abandoned fights.
- Progression-critical proof from kill objectives should be granted from quest credit, not corpse loot, when multiple players can receive credit for the same kill. Use quest-state advance key item grants for proof that should go to every credited player.
- Temporary proof key items must use lore-appropriate, location-specific artifact names and descriptions. Do not ship generator template labels such as `Field Report`, `Calibration Core`, `Broken Seal`, `Command Mark`, or generic `Mark` names as player-facing proof items.
- Capstone quest setup must not add or modify property structures, fabrication furniture recipes, `StructureType`, `structure_####` or `bpstructure####` UTIs, or structure/furniture loot entries unless player-placeable property rewards are explicitly requested as part of the quest line. If such rewards are requested, handle them as a separate property/fabrication change using the existing `StructureType` enum layout and `structure_####` resref convention. Ordinary furniture/placeable structures belong at the end of the non-building `StructureType` section before `// Buildings start here (5000+)`, not after city buildings or in the 5000+ building/layout range.
- Quest journals and dialogue must name the final player-facing location once placement is settled. Avoid promising an existing low-level area if the capstone encounter space still needs to be built.
- If a capstone area does not exist yet, set up quest, key item, achievement, NPC group, and perk gate data only. Do not place temporary enemies, waypoints, activators, or teleports into unrelated existing areas.
- Generated quest definitions must follow the existing readable quest-definition style: one quest or data record per block, arrays split across lines, and long builder arguments wrapped instead of compressed into single-line generated calls.
- Large capstone quest sets must be split into concrete `IQuestListDefinition` classes by the skill that gates the quest line, such as `VibrobladeCapstoneQuestDefinition`. Each concrete quest definition must have `BuildQuests()` call named private quest methods that call `QuestBuilder` directly. Do not hide quest setup behind partial classes, abstract quest bases, shared `BuildQuest(line, step)` helpers, catalog-driven quest models, or generated one-line records. Do not create a central capstone metadata/catalog class; keep line-specific quest IDs, NPC groups, proof key items, quest givers, and encounter asset constants in the owning skill quest definition file. Keep those constants private unless production server code needs cross-file access, in which case use `internal`; never make them public only for tests or handoff documentation.

## Reusable Implementation Procedure

Use this checklist when building the next capstone quest line.

1. Define the quest contract before editing implementation files.
   - Record the skill, skill rank requirement, capstone perk, final quest ID, final boss resref, dedicated quest giver, target area, and any required key item or access gate.
   - Decide whether the final boss demonstrates an existing player capstone directly or needs new humanoid/boss abilities around that capstone.

2. Wire the progression gate through existing systems.
   - Require the matching skill at rank 50 on every quest in the line.
   - For beast capstones, require `SkillType.BeastMastery` rank 50 on every quest in the line. Do not require an active beast, active beast level, or active beast role as a quest prerequisite.
   - Require the final quest ID from the capstone perk definition with `.RequirementQuest(...)`.
   - Keep the quest giver guard and the perk requirement in sync, but do not rely on dialogue alone as the gate.

3. Assign the unlocked capstone to the final boss.
   - Inspect the perk definition to identify granted feats, spells, and stat bonuses.
   - Add the granted feat or spell to the final boss blueprint.
   - If the capstone uses perk stat bonuses, set the boss `VarTable` local `PERK_LEVEL_<perk id>` to the unlocked perk level so `Perk.GetStatBonus(...)` activates the same generic stat behavior used by players.
   - Document the capstone in the boss's World NPCs ability package and add regression coverage that checks both the boss blueprint and the Bible row.

4. Build enemies, abilities, icons, and Bible rows as source-of-truth content.
   - Create new enemies, items, and proof objects for capstone objectives instead of reusing unrelated existing content.
   - Name generated `NPCGroupType` entries with the line's planet prefix, matching `line.AreaGroup.PlanetType`, and never with `Capstone_`.
   - Give every generated enemy a new first-class NPC signature ability, one ability definition per file directly under `SWLOR.Game.Server/Feature/AbilityDefinition/NPC`, plus a unique support ability package. Give humanoid enemies humanoid, tech, Force, command, or weapon abilities; avoid creature-only body attacks unless the enemy is actually a creature.
   - Use `RecastGroup.Capstone` for capstone-tier NPC signature abilities. Do not create parallel capstone/signature recast groups.
   - Name reusable generated NPC abilities for the behavior or effect only. The file, class, feat, TLK, display text, and icon resref must not include the capstone line, boss, NPC, source category, combat profile, skill family, or area.
   - Vary enemy resistance profiles through the World NPCs resistance adjustment columns. Fill every adjustment cell with a numeric value, using `0` when there is no adjustment, and keep formula-driven stat columns formula-backed.
   - Add feat or spell rows, TLK strings, gameplay icon manifest rows, source icons, semantic frames, and cooldown variants for every new signature ability. Run `tools/GenerateCooldownIcons.ps1 -Force` and `tools/UpdateGameplayIconStandards.ps1 -AuditOnly` whenever icon references change.
   - Add or update the `NPC Abilities` Bible tab for every NPC ability definition, including generated signature abilities and reusable support abilities. Fill targeting, hostile/area/target requirements, range, activation/recast, resource cost, damage/resistance, status effect, duration, notes, and source file before the ability is assigned to an NPC package.
   - Add World NPCs rows using the established formulas, dropdown-backed categories, role/difficulty/type/modifier fields, weapon delay rows, resistance adjustment columns, and setup notes.

5. Place content only where level 50 progression can work.
   - Keep level 50 capstone enemies out of low-level spawn tables and low-level public areas.
   - Use dedicated high-level areas, gated spaces, or on-demand boss arenas. If no suitable area exists, leave the spawn table or activator unwired and add an area-builder follow-up instead of mixing levels.
   - For fixed spawn tables or boss spawn positions, create matching waypoint palette blueprints. Hand-placed destination waypoints are acceptable when the area builder intentionally owns placement.

6. Use reusable encounter and access patterns.
   - Use standard `tele_obj` access objects for gated teleport entry when that pattern fits the content.
   - Use `quest_enc` activators for on-demand bosses, with `QUEST_ID`, `QUEST_STATE`, spawn resref, spawn waypoint, cooldown, active-creature guard, party credit, and idle despawn locals configured.
   - For unique quest encounter activators, prefer hand-placed area instances over adding placeable palette blueprints. The placed instance must carry the full `quest_enc` local configuration, and tests should verify the placed instance instead of requiring a palette entry.
   - Keep activators hidden outside the intended quest state and refresh visibility from quest state changes, area entry, and encounter lifecycle events.

7. Keep progression proof separate from ordinary loot.
   - Grant quest-critical proof from quest credit or quest-state advancement so every eligible participant can progress.
   - Name proof key items as local artifacts, permits, chits, relics, logs, relays, or tokens that make sense for the content package. The line name may identify the trial, but the proof noun should come from the area, faction, or enemy context.
   - Keep loot tables for ordinary rewards only unless the quest explicitly requires exclusive corpse loot.
   - Do not add player-placeable structures, furniture recipes, structure blueprint items, or structure loot as incidental capstone rewards. Treat any explicitly requested property reward work as a separate property/fabrication change and keep ordinary structure IDs below the 5000+ building/layout range.
   - Remove temporary proof key items on completion or abandonment.

8. Validate before calling the line ready for testing.
   - Parse every touched Module JSON file.
   - Refresh the Bible audit after workbook edits with `tools/UpdateCombatUpgradeAudit.ps1 -RefreshLocalBible`.
   - Run focused tests covering quest definitions, dialogue snippets, encounter activators, NPC ability packages, Bible rows, icon coverage, and spawn placement.
   - Update this plan with manual toolset steps, unresolved area-builder follow-ups, and an in-game progression pass checklist.

## Quest Giver Placements

All 40 capstone quest givers (39 lines plus Blood Frenzy's Sera Vonn) are hand-placed, each in its **own distinct** safe, non-dungeon area on the package's planet — one giver per area, never bunched. The only exception is the three Dathomir hubs: Dathomir has just three non-dungeon areas (everything else is beast wilderness with spawn tables) for its six beast-mastery givers, so those hubs carry two each. Givers must never stand in an `IS_DUNGEON` area, a `CREATURE_SPAWN_TABLE_ID` area, or a sub-space that is part of a dungeon/restricted flow (the casino backroom, for example). Each giver keeps its unique name and a distinct `Appearance_Head`, and wears a themed outfit (Unit KX-17 is a droid). Journal delivery text (Return/Bring/Deliver/Report) names the giver's specific area. Coverage: `CapstoneQuestGiverPlacementTests` (placement + one-per-area spread + non-dungeon + uniqueness), plus `DathomirGrottoApexDenPlacementTests` and `FightClubBackroomsPlacementTests`. Positions were set from module coordinates near existing area NPCs; verify on valid walkmesh in the toolset. Placement changes need a module repack on deploy.

| Content Package | Quest Giver (resref) | Placed Area (git) |
| --- | --- | --- |
| Veles Militia Annex | Captain Tov Renn (`cq_invinc`) | Viscara - Veles - Sheriff/Clinic (`veles_sheriff`) |
| Veles Militia Annex | Mikka Varn (`cq_vitrupt`) | Viscara - Veles - Racin' Jims (`veles_cantina`) |
| Veles Militia Annex | Dalen Orso (`cq_sysshut`) | Viscara - Veles - Czerka Tower (`veles_cz_tower`) |
| Viscara Republic Engineering Bunker | Aric Jorr (`cq_killbeacon`) | Viscara - Republic Base - Entrance (`v_repubbase_1`) |
| Viscara Republic Engineering Bunker | Nella Voss (`cq_embunker`) | Viscara - Republic Base - Exterior (`v_repubbase_ext`) |
| Viscara Republic Engineering Bunker | Varen Kell (`cq_deccommand`) | Viscara - Republic Base - Combat Deck (`v_repubbase_cd`) |
| Dantooine Jedi Enclave Trial Halls | Talan Rees (`cq_sabstorm`) | Dantooine - Jedi Enclave (`dan_jedienclave`) |
| Dantooine Jedi Enclave Trial Halls | Miris Aven (`cq_guardmst`) | Dantooine - Jedi Library (`dan_jedlibrary`) |
| Dantooine Jedi Enclave Trial Halls | Jora Sel (`cq_sabcycl`) | Dantooine - Interior (`dan_interiors`) |
| Dantooine Medical Sublevel | Kavi Dorn (`cq_emcocktail`) | Dantooine - Republic Med Center (`dan_repubmed`) |
| Dantooine Medical Sublevel | Edda Maln (`cq_holdline`) | Dantooine - Republic Garrison (`dan_repgarrison`) |
| Dantooine Medical Sublevel | Tessa Quell (`cq_infconduit`) | Dantooine - Medical Interior (`dan_medinterior`) |
| Korriban Forge Caverns | Valis Korr (`cq_absdef`) | Korriban - Sith Academy (`ar_scor_kacademy`) |
| Korriban Forge Caverns | Senn Dralok (`cq_soulasc`) | Korriban - Wasteland Interiors (`scor_knwinterior`) |
| Korriban Forge Caverns | Maar Veth (`cq_forcebane`) | Korriban - Valley Temples (`ar_scor_kortemp`) |
| Korriban Sith Crypt Depths | Seris Nahl (`cq_lightstand`) | Korriban - Starport (`korribanlandingp`) |
| Korriban Sith Crypt Depths | Neth Kyr (`cq_darkhung`) | Korriban - Starport - Cantina (`ar_scor_korrcan`) |
| Korriban Sith Crypt Depths | Acolyte Varn (`cq_eclipse`) | Korriban - Wastelands Tunnels (`scor_kscaves`) |
| CZ-220 Breaker Yard | Tressa Kade (`cq_adamguard`) | CZ-220 - Offices & Labs (`nanostation015`) |
| CZ-220 Breaker Yard | Borrik Sen (`cq_scraplock`) | CZ-220 - Hangar (`czs220_hangar`) |
| CZ-220 Breaker Yard | Unit KX-17 (`cq_worldbrk`, droid) | CZ-220 - Maintenance Level (`czs220_maintlvl`) |
| Anchorhead Canyon Range | Marda Voss (`cq_unmovctr`) | Tatooine - Anchorhead - Cantina (`tat_anc_cantina`) |
| Anchorhead Canyon Range | Jek Talin (`cq_lastword`) | Tatooine - Anchorhead - Smuggler's Den (`tosche_cantina_s`) |
| Anchorhead Canyon Range | Pavo Orrel (`cq_deadhand`) | Tatooine - Anchorhead - Club d'Ash (`tochee_cantina`) |
| Czerka Arms Test Range | Ressa Vale (`cq_killbox`) | Smuggler's Moon - Czerka Arms, Store (`pw_ar_nsczgnstr`) |
| Czerka Arms Test Range | Orin Tal (`cq_oneshot`) | Smuggler's Moon - Czerka Shipyard Office (`pw_ar_czoffice`) |
| Czerka Arms Test Range | Varik Dane (`cq_rainsteel`) | Smuggler's Moon - Fabrication Facility (`pw_ar_nscrafting`) |
| Hutlar Qion Test Site | Ruk Halven (`cq_thermdet`) | Hutlar - Outpost (`hutlar_outpost`) |
| Hutlar Qion Test Site | Miri Koss (`cq_overbarr`) | Hutlar - Fort Ka'ra (`sol_mandaloriani`) |
| Hutlar Qion Test Site | Selka Vorn (`cq_perflurry`) | Hutlar - Qion Box Canyon (`sol_hutlarqcanyo`) |
| Dathomir Grotto Apex Den | Nalka Rinn (`cq_primover`) | Dathomir - Jungle Landing (`dath_landingpad`) |
| Dathomir Grotto Apex Den | Voro Thane (`cq_untinst`) | Dathomir - Czerka Base (`dath_cz_baseok`) |
| Dathomir Grotto Apex Den | Eshka Korr (`cq_forcebeast`) | Dathomir - Waterfall Ruins (`dath_waterfallru`) |
| Dathomir Tarn Jungle Preserve | Talra Venn (`cq_apexbite`) | Dathomir - Jungle Landing (`dath_landingpad`) |
| Dathomir Tarn Jungle Preserve | Oren Krast (`cq_unbrbeast`) | Dathomir - Czerka Base (`dath_cz_baseok`) |
| Dathomir Tarn Jungle Preserve | Mira Syth (`cq_alpharhy`) | Dathomir - Waterfall Ruins (`dath_waterfallru`) |
| Smuggler's Moon Fight Club Backrooms | Dax Rell (`cq_cripdef`) | Smuggler's Moon - Hyper Dive Cantina (`pw_ar_nars_canhd`) |
| Smuggler's Moon Fight Club Backrooms | Iven Brask (`cq_tempbloom`) | Smuggler's Moon - The Tilted Visor (`pw_ar_bhbar`) |
| Smuggler's Moon Fight Club Backrooms | Nyra Tane (`cq_redbloom`) | Smuggler's Moon - Casino (`pw_ar_nscasino`) |
| Viscara Sewers Depths (Blood Frenzy) | Sera Vonn (`sera_vonn`) | Viscara - Veles - Shops (`veles_shops`) |

## Remaining Capstone Quest Setup

The skill-owned capstone quest definitions define the 39 post-Blood-Frenzy capstone quest lines. Each line has five quests, rank prerequisites on every step, proof key items granted from quest credit, a final quest achievement, NPC group identifiers for every objective, deterministic enemy/waypoint/spawn/loot asset IDs, and a final quest ID used by the matching capstone perk requirement.

All 40 quest givers are placed (see the Quest Giver Placements table above); quest-giver creation and placement are not remaining work. For each of the eight unfinished packages, what remains is the dungeon/boss-arena content: two registered physical areas, gated access and internal travel, area metadata, a general spawn waypoint, and the warden/master `quest_enc` activator and spawn-waypoint instances.

The reusable setup exists now: quest definitions, dedicated quest giver UTC/DLG files, creature palette entries for those quest givers, enemy UTCs, stat skins, weapons, spawn table definitions, loot table definitions, and waypoint palette blueprints. The lines assigned to the eight unfinished packages are not ready for in-game progression until their areas, gated access objects, travel links, area metadata, spawn waypoints, and `quest_enc` boss activator instances are created.

No `Module/git` placement should be added for these lines until the target content package exists. Area builders may reuse each content package for up to three capstone lines, but must keep the level 50 content isolated from low-level or general-purpose spawn spaces.

For capstone planning, a content package is not a single physical area. Following the Blood Frenzy pattern, each content package requires two attached physical areas:

- One gated dungeon or lesson area containing the ambient level 50 capstone enemies and general spawn waypoint.
- One attached boss arena area containing the state-gated `quest_enc` activators and boss spawn waypoints.

The full 13-package scope represents 26 physical areas: 13 dungeons and 13 attached boss arenas. Five packages are wired, so the remaining construction scope is eight packages and 16 physical areas.

Current build state (2026-08-17):

- **Dathomir Grotto Apex Den** is fully placed and wired (see the dedicated section below). Its three lines await only an in-game progression pass and a position review.
- **Czerka Arms Test Range** is wired (2026-07-14). Dungeon `pw_ar_czarmrange` (`Smuggler's Moon - Czerka Weapons Testing Facility`) carries `CREATURE_SPAWN_TABLE_ID = CAPSTONE_CZERKA_ARMS_TEST_RANGE`, `IS_DUNGEON`, `MINI_MAP_DISABLED`, `MAP_KEY_ITEM_ID = 85` (Corporate District region key). The three **warden** activators + waypoints (`killbox_wd_call`/`oneshot_wd_call`/`rainsteel_wd_call`) live in the dungeon near `WP_SMUG_CZERKA_WEAPONS_TO_ARENA`; the three **master** activators + waypoints (`killbox_ms_call`/`oneshot_ms_call`/`rainsteel_ms_call`) live in the boss arena `ka_ar_czweaparen` (`Smuggler's Moon - Czerka Blast-Safe Cell`, elevated floor Z≈15.2) near `WP_SMUG_CZERKA_ARENA_TO_WEAPONS`. Dungeon↔arena uses the area builder's engine `LinkedTo` triggers (`SMUG_CZERKA_WEAPONS_TO_ARENA` / `SMUG_CZERKA_ARENA_TO_WEAPONS`, no key). The hub→dungeon `[Enter Czerka Weapons Testing Facility]` tele_obj in `pw_ar_narscorpd` (Corporate District) was mis-pointed at the hub-side exit waypoint (a no-op loop); it now targets `SMUG_CZERKA_WEAPONS_TEST_ENT` and is key-gated with `KEY_ITEM_ID = 113` + `TELEPORT_PARTY_MEMBERS = 1`. Covered by `CzerkaArmsTestRangePlacementTests`. Naming gotcha: the arena resref `ka_ar_czweaparen` collides with the `ka_*` `[Prefab]` Comms-event-area convention, so `PlayerFacingNameBroadcastTests` now matches event areas by their `[Prefab]` name rather than resref prefix alone.
- **CZ-220 Breaker Yard** is wired (2026-07-14). Note the area roles are reversed from the package names: **Breaker Bay (`cz220shipbreakin`) is the dungeon** and **Breaker Yard (`cz220shipbreaker`) is the boss arena**. The dungeon carries `CREATURE_SPAWN_TABLE_ID = CAPSTONE_CZ220_BREAKER_YARD`, `IS_DUNGEON`, `MINI_MAP_DISABLED`, `MAP_KEY_ITEM_ID = 23`. Warden mini-bosses (quest step 3) and final masters (step 5) are split by area: the three **warden** `quest_enc` activators + waypoints (`adamg_wd_call`/`scrapl_wd_call`/`wbrk_wd_call`) live in the **dungeon** at its `Shipbreak_Boss_Entrance` gate, and the three **master** activators + waypoints (`adamg_ms_call`/`scrapl_ms_call`/`wbrk_ms_call`) live in the **boss arena** at `Shipbreak_Boss_Exit`. This supersedes the older handoff guidance that placed both warden and master activators in the boss arena: mini-bosses belong in the dungeon. Each set is co-located at a validated waypoint an area builder can spread for aesthetics; only one activator is live per quest state. The `Enter the Breaker Bay` `tele_obj` in `czs220_maintlvl` targets `CZ220_MAINT_ENTRANCE_BREAKER`, requires `CapstoneCZ220BreakerYardKey` (id 111), and enables party travel; the internal `Shipbreak_Down/Up` lifts are intentionally ungated. Encounter placement is covered by `CZ220BreakerYardPlacementTests`.
- **Smuggler's Moon Fight Club Backrooms** now has its dungeon area imported and live (2026-07-13): `pw_sc_emfbackr` (`Smuggler's Moon - Fight Club Backrooms`) carries `CREATURE_SPAWN_TABLE_ID = CAPSTONE_SMUGGLERS_MOON_FIGHT_CLUB_BACKROOMS`, `IS_DUNGEON`, `MINI_MAP_DISABLED`, `MAP_KEY_ITEM_ID = 89`, `PLANET_TYPE_ID = 256`, and a `STUCK_WAYPOINT`. The `[Back Rooms]` tele_obj in `pw_ar_nsficlub` is key-gated with `KEY_ITEM_ID = 110` and `TELEPORT_PARTY_MEMBERS = 1`. The quest givers are distributed across three safe public hubs: Dax Rell (`cq_cripdef`) in the Hyper Dive Cantina (`pw_ar_nars_canhd`), Iven Brask (`cq_tempbloom`) in the Tilted Visor (`pw_ar_bhbar`), and Nyra Tane (`cq_redbloom`) in the public Casino (`pw_ar_nscasino`). Neither the Fight Club floor nor the casino backroom/restricted flow may host them, and each journal return instruction names its giver's actual hub. Giver distribution is covered by `CapstoneQuestGiverPlacementTests`; dungeon exclusion is covered by `FightClubBackroomsPlacementTests`. The three **warden** mini-boss activators + waypoints (`cripdef_wd_call`/`tempbloom_wd_call`/`redbloom_wd_call`, quests `*_breach`) are placed in the dungeon, one per backroom sub-area. The attached **Private Pit** boss arena (`pw_sc_smarena`, `Smuggler's Moon - Private Pit`) is built and holds the three **master** activators + spawn waypoints (`cripdef_ms_call`/`tempbloom_ms_call`/`redbloom_ms_call`, quests `*_mastery`), wired 2026-07-14. Dungeon↔arena is an engine door pair (`fight_club_left/right` ↔ `smugarena_left/right`, no key — internal). Both wardens and masters are covered by `FightClubBackroomsWardenPlacementTests`. Notes: the arena imported with a duplicate area tag (`pw_sc_emfbackr`), retagged to `pw_sc_smarena`; the re-import stripped SWLOR content from the dungeon and it was reapplied; the master positions are blind-anchored within the arena bounds and pending the user's placement review.
- **Dantooine Medical Sublevel** is wired (2026-08-17). The dungeon is `pw_sc_dantmedsub` (`Dantooine - Medical Sublevel`) and the attached boss arena is `pw_sc_dantprowar` (`Dantooine - Protected Ward`). Both are registered in the module and use distinct area tags. The standard `tele_obj` entrance in `dan_warehouse` is key-gated with `KEY_ITEM_ID = 117` (`CapstoneDantooineMedicalSublevelKey`) and `TELEPORT_PARTY_MEMBERS = 1`; it targets the dungeon waypoint `to_medsublevel`, and the dungeon returns to `dant_to_abandonedlabs`. The dungeon carries `CREATURE_SPAWN_TABLE_ID = CAPSTONE_DANTOOINE_MEDICAL_SUBLEVEL`, `IS_DUNGEON`, `MINI_MAP_DISABLED`, `MAP_KEY_ITEM_ID = 77`, `PLANET_TYPE_ID = 128`, a `DANTOOINE_MEDICAL_SUBLEVEL_RARES` waypoint, and a `STUCK_WAYPOINT`. The three **warden** activators + spawn waypoints (`emcocktail_wd_call`/`holdline_wd_call`/`infconduit_wd_call`) live in the dungeon. The three **master** activators + spawn waypoints (`holdline_ms_call`/`emcocktail_ms_call`/`infconduit_ms_call`) live in the Protected Ward. Dungeon↔arena travel uses the existing `dant_protect_ward` and `dant_medsublevelup` waypoint pair, and the arena has its own `STUCK_WAYPOINT`. All six activators use the complete state-gated `quest_enc` local set with 60-minute starter cooldowns and 10-minute idle despawns. Covered by `DantooineMedicalSublevelPlacementTests`. Encounter coordinates were anchored to open interior tiles from the exported layouts; review them in the toolset or in game and nudge for encounter composition as desired.
- **All 13 packages now have a Blood Frenzy-style rare loot layer** (Fight Club: 2026-07-13; the other 12 packages: 2026-07-13). Each line has two rare tables in its planet's loot definition — a lesson `_RARES` table (8 venue-themed items) and a warden `_WD_RARES` table (5 items) — wired as `LOOT_TABLE_2` at 5% on the adept/specialist/inner-circle enemies (lesson table) and the warden (warden table). Masters drop ordinary boss loot only, matching Kess Draavo. `LOOT_TABLE_1` still points at the generic lesson/boss tables the tests pin.
  - Fight Club uses `NARSHADDAA_FIGHT_CLUB_*_RARES` naming; the other 36 lines use `CAPSTONE_<LINECODE>_RARES` / `CAPSTONE_<LINECODE>_WD_RARES` (e.g. `CAPSTONE_FORCEBANE_RARES`). Items use deterministic resrefs `<linecode>_<l1..l8|w1..w5>`.
  - Weapon-skill lines drop the line's weapon type (Force lines drop lightsabers); non-weapon lines (Devices, Leadership, First Aid, Beast Mastery) drop gear/accessories only. Weapon damage is lesson-tier (23) / warden-tier (41) with normalized per-base delay; gear reuses Blood Frenzy stat templates by slot.
  - 468 new items total (36 lines × 13). They require a module repack on deploy.
- The remaining eight packages have no physical areas yet; they account for the 16-area construction backlog described above.

### Generated Reusable Content

- General lesson enemies use one spawn table per area group. Each table contains only quest steps 1, 2, and 4: adept, specialist, and inner circle enemies.
- Quest step 3 wardens and quest step 5 final masters are intentionally excluded from ambient spawn tables. They must be spawned through `quest_enc` activator instances only. Warden mini-bosses (step 3) go in the DUNGEON/lesson area; final masters (step 5) go in the attached BOSS ARENA. All five completed content packages follow this split.
- Each area group has two ordinary loot tables: `<spawn table id>_LESSON_LOOT` and `<spawn table id>_BOSS_LOOT`. Progression proof key items are not in these loot tables.
- Each capstone line has five generated enemy UTCs, five generated stat skins, and five generated weapons using deterministic resrefs:
  - Enemy: `cp_<line code>_<ad|sp|wd|ic|ms>`
  - Stat skin: `cs_<line code>_<ad|sp|wd|ic|ms>`
  - Weapon: `cw_<line code>_<ad|sp|wd|ic|ms>`
- Each generated enemy has a reusable generic signature ability, a unique support ability package, and a World NPCs row documenting its difficulty, role, creature type, modifier, resistance adjustments, signature/support abilities, existing abilities, and setup notes.
- Reusable generated assets must not be branded with the capstone line, capstone perk, boss, NPC, area name, source category, combat profile, or skill family. This includes signature/support abilities, feat labels, TLK text, icons, stat skins, weapons, and other reusable generated item display names. Use behavior, effect, or mechanical purpose only. Keep capstone/perk names only in quest progression surfaces, achievements, key/proof text where they identify the line, and the final boss's actual unlock package.
- Final boss UTCs include the matching capstone feat and `PERK_LEVEL_<perk id>` local.
- Beast capstone enemies use beast creature appearances and beast-style feat packages rather than humanoid templates.
- Area spawn waypoint blueprints are in `Module/utw` and `Module/itp/waypointpalcus.itp.json`. Their `Tag` equals the spawn table ID.
- Warden/master spawn waypoint blueprints are in `Module/utw` and `Module/itp/waypointpalcus.itp.json`. Their `Tag` is the `QUEST_ENCOUNTER_WAYPOINT` value for future `quest_enc` activator instances.
- Unique boss activator placeables are not generated and are not in the placeable palette. The future placed world instance is the source of truth for each activator.

### Required Content Packages

Each content package below requires one dungeon area and one attached boss arena area. The dungeon holds the ambient lesson enemies for quest steps 1, 2, and 4. The attached boss arena holds the on-demand warden and master encounters for quest steps 3 and 5. Do not collapse the boss arena into the dungeon for these capstone packages; the intended pattern is Blood Frenzy-style dungeon plus attached boss arena.

| Content Package | Planet | Capstone Lines | Dungeon Area Expectation | Attached Boss Arena Expectation |
| --- | --- | --- | --- | --- |
| Veles Militia Annex | Viscara | Invincible; Vital Rupture; Systemic Shutdown | Secured militia training wing attached to Veles Colony; interior barracks, sparring floor, and knife-work cells. | Isolated militia command room attached to the annex, with state-gated warden/master activators. |
| Dantooine Jedi Enclave Trial Halls | Dantooine | Saber Storm; Guardian Master; Saber Cyclone | Sealed Jedi Enclave training wing with stone/enclave interiors, crystal-channel side rooms, and saber trial corridors. | Controlled saber trial chamber attached to the halls, isolated from ambient lesson spawns. |
| Korriban Forge Caverns | Korriban | Absolute Defense; Soul Ascension; Forcebane | Ancient Sith weapon forge and cavern complex with heavy melee proving rooms and hazardous forge machinery. | Sealed champion forge chamber attached to the caverns. |
| Smuggler's Moon Fight Club Backrooms | Smuggler's Moon | Crippling Defense; Tempest Bloom; Red Bloom | Illegal fight-club service corridors and private arena backrooms, gated from public casino/fight club traffic. | Smuggler's Moon Private Pit, attached to the backrooms for on-demand bosses. |
| CZ-220 Breaker Yard | CZ-220 | Adamantine Guard; Scrapheap Lockdown; Worldbreaker | Industrial scrap and maintenance yard with tight lanes, gantries, and malfunctioning machinery. | Locked breaker bay attached to the yard for warden/master encounters. |
| Anchorhead Canyon Range | Tatooine | Unmoving Center; Last Word; Dead Man's Hand | Remote canyon firing range outside Anchorhead with open lanes, cover ridges, and dueling platforms. | Isolated canyon-pocket arena attached to the range. |
| Czerka Arms Test Range | Smuggler's Moon | Kill Box; One Shot; Rain of Steel | Czerka Arms firing and ordnance test range with interior lanes, storage, and target-control rooms. | Czerka Blast-Safe Cell, attached to the test range. |
| Hutlar Qion Test Site | Hutlar | Perfect Flurry; Thermal Detonator; Overload Barrage | Frozen Qion Valley weapons test site with snowfield approach, bunker interiors, and device hazards. | Contained overload chamber attached to the test site. |
| Korriban Sith Crypt Depths | Korriban | Last Stand of the Light; Hunger of the Dark; Eclipse of Resolve | Deep Sith crypt trial space with tomb corridors, ritual vaults, and light/dark pressure rooms. | Sealed final ritual chamber attached to the crypt depths. |
| Viscara Republic Engineering Bunker | Viscara | Killzone Beacon; Emergency Bunker; Decisive Command | Republic base engineering and command bunker with tactical consoles, shield generators, and deployable-device lanes. | Command-room boss arena attached to the bunker. |
| Dantooine Medical Sublevel | Dantooine | Hold the Line; Emergency Cocktail; Infinite Conduit | Medical facility sublevel connected to old enclave conduits, with triage rooms and conduit maintenance spaces. | Protected ward encounter space attached to the sublevel. |
| Dathomir Tarn Jungle Preserve | Dathomir | Apex Bite; Unbreakable Beast; Alpha Rhythm | Controlled high-risk jungle preserve with beast pens, elevated paths, and wild clearings. | Alpha-beast arena pocket attached to the preserve. |
| Dathomir Grotto Apex Den | Dathomir | Primal Overrun; Untouchable Instinct; Force-Bonded Beast | Grotto cavern den beneath Dathomir wilds with cave nests, ritual beast tracks, and vertical chambers. | Sealed apex-den boss room attached to the grotto. |

(2026-07-11: the Lightsaber Ward/Severance perk trees were redesigned. The Dantooine Jedi Enclave Trial Halls "Saber Storm" and "Guardian Master" mastery quests listed above are unchanged/reused — their quest IDs and definitions did not change — but they now gate the new capstones Epicenter (Severance tree) and Aegis Eternal (Ward tree) respectively, instead of the old Lightsaber Defense/Offense capstones.)

### Suggested Tilesets

Tileset per area: the resref goes in the area's `Tileset` field, and the toolset display name (`UnlocalizedName` in the `.set`) is what you pick in the New Area dropdown. The five shipped packages list their actual tilesets; the rest are recommendations chosen to fit the fiction and reuse tilesets already proven in existing SWLOR areas.

| Content Package | Dungeon Tileset | Attached Boss Arena Tileset |
| --- | --- | --- |
| Veles Militia Annex | `tjsb0` — D20 Secret Base | `tbx78` — D20 Modern Facility |
| Dantooine Jedi Enclave Trial Halls | `udp2` — D20 Office Interiors UDP | `zin01` — [CEP] City Interior 1 |
| Korriban Forge Caverns | `zde01` — [CEP] Dungeon | `ztu01` — [CEP] Underdark |
| Smuggler's Moon Fight Club Backrooms | `udp2` — D20 Office Interiors UDP (built) | `udp2` — D20 Office Interiors UDP (built) |
| CZ-220 Breaker Yard | `zsf01` — D20 SciFi Base CQ (built) | `zsf01` — D20 SciFi Base CQ (built) |
| Anchorhead Canyon Range | `ttd01` — [SW] Tatooine | `tdm01` — Mines and Caverns |
| Czerka Arms Test Range | `flow_pa` — D20 Parking Garage (built) | `dgt04` — D20 Modern Exterior (built) |
| Hutlar Qion Test Site | `zti01` — [CEP] Frozen Wastes | `tbx78` — D20 Modern Facility |
| Korriban Sith Crypt Depths | `vmr01` — D20 Alien Ruins | `zid01` — [CEP] Drow Interior |
| Viscara Republic Engineering Bunker | `tbx78` — D20 Modern Facility | `tjsb0` — D20 Secret Base |
| Dantooine Medical Sublevel | `tqq01` — Complex Labs Storage (built) | `tmi` — ModernInterior (built) |
| Dathomir Tarn Jungle Preserve | `jac01` — Jacoby's Jungle | `ttu01` — Underdark |
| Dathomir Grotto Apex Den | `ttu01` — Underdark (built) | `ttu01` — Underdark (built) |

Naming caveats: `udp2` ("D20 Office Interiors") and `zin01` ("[CEP] City Interior 1") read as urban but render as the stone enclave/temple interiors used by the game's Dantooine Jedi Enclave Library and Viscara Jedi Temple Interior. `tqq01` is stored in the hak with a typo as "Complex laps storage"; it is the Dantooine Medical/Lab interior tileset.

### Generated Dungeon Spawn Tables

These spawn tables are for the dungeon/lesson areas only. Do not place these waypoint blueprints in the attached boss arena.

| Content Package | Spawn Table ID | General Waypoint Resref |
| --- | --- | --- |
| Veles Militia Annex | `CAPSTONE_VELES_MILITIA_ANNEX` | `wp_cap_veles` |
| Dantooine Jedi Enclave Trial Halls | `CAPSTONE_DANTOOINE_JEDI_ENCLAVE_TRIAL_HALLS` | `wp_cap_dantjedi` |
| Korriban Forge Caverns | `CAPSTONE_KORRIBAN_FORGE_CAVERNS` | `wp_cap_kforge` |
| Smuggler's Moon Fight Club Backrooms | `CAPSTONE_SMUGGLERS_MOON_FIGHT_CLUB_BACKROOMS` | `wp_cap_smfight` |
| CZ-220 Breaker Yard | `CAPSTONE_CZ220_BREAKER_YARD` | `wp_cap_czbr` |
| Anchorhead Canyon Range | `CAPSTONE_ANCHORHEAD_CANYON_RANGE` | `wp_cap_tatcan` |
| Czerka Arms Test Range | `CAPSTONE_CZERKA_ARMS_TEST_RANGE` | `wp_cap_czerka` |
| Hutlar Qion Test Site | `CAPSTONE_HUTLAR_QION_TEST_SITE` | `wp_cap_hutlar` |
| Korriban Sith Crypt Depths | `CAPSTONE_KORRIBAN_SITH_CRYPT_DEPTHS` | `wp_cap_kcrypt` |
| Viscara Republic Engineering Bunker | `CAPSTONE_VISCARA_REPUBLIC_ENGINEERING_BUNKER` | `wp_cap_vrepub` |
| Dantooine Medical Sublevel | `CAPSTONE_DANTOOINE_MEDICAL_SUBLEVEL` | `wp_cap_dantmed` |
| Dathomir Tarn Jungle Preserve | `CAPSTONE_DATHOMIR_TARN_JUNGLE_PRESERVE` | `wp_cap_dtarn` |
| Dathomir Grotto Apex Den | `CAPSTONE_DATHOMIR_GROTTO_APEX_DEN` | `wp_cap_dgrot` |

### Area Builder Handoff For Each Content Package

- Create two attached physical areas: a gated dungeon/lesson area and a boss arena area reachable from that dungeon.
- Place each capstone line's dedicated quest giver NPC in a safe hub area with no enemy spawns (such as a settlement or landing pad on the package's planet). Do not collapse multiple capstone quest lines onto one shared area trainer even when the lines reuse the same content package. Update the line's journal "return to" text to name that hub.
- Add a standard access object or equivalent gate that checks the content package's capstone key item and sends the player into the dungeon/lesson area.
- In the dungeon/lesson area, place the general spawn waypoint using the generated waypoint resref above. Its tag already matches the generated spawn table ID.
- In the dungeon/lesson area, use the generated level 50 spawn table for general lesson enemies only. Do not place the table in low-level, public, or boss arena areas.
- Place `quest_enc` activator instances for on-demand bosses (state-specific visibility, 60-minute starter cooldown, one-active-creature guard, participant quest credit, idle despawn). Put the warden (step 3) activators in the DUNGEON/lesson area and the master (step 5) activators in the attached BOSS ARENA.
- For every line, place one warden activator for quest step 3 (in the dungeon) and one master activator for quest step 5 (in the boss arena). Each activator should use the generated boss UTC resref and generated boss spawn waypoint tag defined by the owning quest line and generated module assets, such as `cp_invinc_wd` with `CAPSTONE_INVINC_WD_SPAWN` or `cp_invinc_ms` with `CAPSTONE_INVINC_MS_SPAWN`. Copy literal handoff values into setup notes when an area builder needs them; do not make quest constants public for handoff tooling.
- Required `quest_enc` locals per activator:
  - `QUEST_ID`: the exact quest ID for the warden/master step.
  - `QUEST_STATE`: `1`.
  - `QUEST_ENCOUNTER_ID`: a unique encounter ID for that activator.
  - `QUEST_ENCOUNTER_RESREF`: the generated enemy resref, such as `cp_invinc_wd` or `cp_invinc_ms`.
  - `QUEST_ENCOUNTER_WAYPOINT`: the generated boss spawn waypoint tag, such as `CAPSTONE_INVINC_WD_SPAWN` or `CAPSTONE_INVINC_MS_SPAWN`.
  - `QUEST_ENCOUNTER_COOLDOWN_MINUTES`: `60`.
  - `QUEST_ENCOUNTER_IDLE_MINUTES`: use the established boss idle timeout unless the area needs a stricter value.
- Keep unique boss activator instances out of the placeable palette unless the area builder explicitly asks for a reusable palette blueprint.
- Add World NPCs Bible rows for every generated enemy and boss using the established NPC guide, formulas, dropdown-backed fields, resistance overrides, weapon delay rows, and ability packages before final balance sign-off.
- Generated enemies already require reusable generic signature abilities. Create any extra boss-only abilities later as reusable behavior/effect assets, one ability definition per file directly under `SWLOR.Game.Server/Feature/AbilityDefinition/NPC`, with valid icons and generated cooldown icon variants, when a line needs more than its assigned capstone feat, generated signature ability, and baseline NPC support package.
- Run a full in-game progression pass for every line assigned to the area group after placement.

### Area Builder Quick Reference

Literal handoff values for every capstone line. All lines follow the same deterministic patterns:

- Quest IDs: `<quest id stem>_foundation`, `_measure`, `_breach` (warden), `_circle`, `_mastery` (master).
- Warden activator locals: `QUEST_ID = <stem>_breach`, `QUEST_ENCOUNTER_ID = <stem>_breach_warden`, `QUEST_ENCOUNTER_RESREF = cp_<code>_wd`, `QUEST_ENCOUNTER_WAYPOINT = CAPSTONE_<CODE>_WD_SPAWN` (waypoint blueprint `wp_<code>_wd`).
- Master activator locals: `QUEST_ID = <stem>_mastery`, `QUEST_ENCOUNTER_ID = <stem>_mastery_master`, `QUEST_ENCOUNTER_RESREF = cp_<code>_ms`, `QUEST_ENCOUNTER_WAYPOINT = CAPSTONE_<CODE>_MS_SPAWN` (waypoint blueprint `wp_<code>_ms`).
- Every activator: `QUEST_STATE = 1`, `VISIBILITY_HIDDEN_DEFAULT = 1`, a unique `VISIBILITY_OBJECT_ID`, `QUEST_ENCOUNTER_COOLDOWN_MINUTES = 60`, `QUEST_ENCOUNTER_IDLE_MINUTES = 10`, `OnUsed = quest_enc`, `LocName = ???`.
- Access gate: `tele_obj` instance with `KEY_ITEM_ID` = the package's key item ID below, `TELEPORT_PARTY_MEMBERS = 1`, and a `MISSING_KEY_ITEM_MESSAGE` naming the package location.

| Content Package | Line (Skill) | Quest Giver (resref) | Line Code | Quest ID Stem | Key Item ID |
| --- | --- | --- | --- | --- | --- |
| Veles Militia Annex | Invincible (Vibroblade) | Captain Tov Renn (`cq_invinc`) | `invinc` | `invincible` | 107 |
| Veles Militia Annex | Vital Rupture (Vibroknife) | Mikka Varn (`cq_vitrupt`) | `vitrupt` | `vital_rupture` | 107 |
| Veles Militia Annex | Systemic Shutdown (Vibroknife) | Dalen Orso (`cq_sysshut`) | `sysshut` | `systemic_shutdown` | 107 |
| Dantooine Jedi Enclave Trial Halls | Saber Storm (Lightsaber) | Talan Rees (`cq_sabstorm`) | `sabstorm` | `saber_storm` | 108 |
| Dantooine Jedi Enclave Trial Halls | Guardian Master (Lightsaber) | Miris Aven (`cq_guardmst`) | `guardmst` | `guardian_master` | 108 |
| Dantooine Jedi Enclave Trial Halls | Saber Cyclone (Saberstaff) | Jora Sel (`cq_sabcycl`) | `sabcycl` | `saber_cyclone` | 108 |
| Korriban Forge Caverns | Absolute Defense (HeavyVibroblade) | Valis Korr (`cq_absdef`) | `absdef` | `absolute_defense` | 109 |
| Korriban Forge Caverns | Soul Ascension (HeavyVibroblade) | Senn Dralok (`cq_soulasc`) | `soulasc` | `soul_ascension` | 109 |
| Korriban Forge Caverns | Forcebane (Spear) | Maar Veth (`cq_forcebane`) | `forcebane` | `forcebane` | 109 |
| Smuggler's Moon Fight Club Backrooms | Crippling Defense (Spear) | Dax Rell (`cq_cripdef`) | `cripdef` | `crippling_defense` | 110 |
| Smuggler's Moon Fight Club Backrooms | Tempest Bloom (TwinBlade) | Iven Brask (`cq_tempbloom`) | `tempbloom` | `tempest_bloom` | 110 |
| Smuggler's Moon Fight Club Backrooms | Red Bloom (TwinBlade) | Nyra Tane (`cq_redbloom`) | `redbloom` | `red_bloom` | 110 |
| CZ-220 Breaker Yard | Adamantine Guard (Katar) | Tressa Kade (`cq_adamguard`) | `adamguard` | `adamantine_guard` | 111 |
| CZ-220 Breaker Yard | Scrapheap Lockdown (Katar) | Borrik Sen (`cq_scraplock`) | `scraplock` | `scrapheap_lockdown` | 111 |
| CZ-220 Breaker Yard | Worldbreaker (Staff) | Unit KX-17 (`cq_worldbrk`) | `worldbrk` | `worldbreaker` | 111 |
| Anchorhead Canyon Range | Unmoving Center (Staff) | Marda Voss (`cq_unmovctr`) | `unmovctr` | `unmoving_center` | 112 |
| Anchorhead Canyon Range | Last Word (Pistol) | Jek Talin (`cq_lastword`) | `lastword` | `last_word` | 112 |
| Anchorhead Canyon Range | Dead Man's Hand (Pistol) | Pavo Orrel (`cq_deadhand`) | `deadhand` | `dead_mans_hand` | 112 |
| Czerka Arms Test Range | Kill Box (Rifle) | Ressa Vale (`cq_killbox`) | `killbox` | `kill_box` | 113 |
| Czerka Arms Test Range | One Shot (Rifle) | Orin Tal (`cq_oneshot`) | `oneshot` | `one_shot` | 113 |
| Czerka Arms Test Range | Rain of Steel (Throwing) | Varik Dane (`cq_rainsteel`) | `rainsteel` | `rain_of_steel` | 113 |
| Hutlar Qion Test Site | Perfect Flurry (Throwing) | Selka Vorn (`cq_perflurry`) | `perflurry` | `perfect_flurry` | 114 |
| Hutlar Qion Test Site | Thermal Detonator (Devices) | Ruk Halven (`cq_thermdet`) | `thermdet` | `thermal_detonator` | 114 |
| Hutlar Qion Test Site | Overload Barrage (Devices) | Miri Koss (`cq_overbarr`) | `overbarr` | `overload_barrage` | 114 |
| Korriban Sith Crypt Depths | Last Stand of the Light (Force) | Seris Nahl (`cq_lightstand`) | `lightstand` | `last_stand_of_the_light` | 115 |
| Korriban Sith Crypt Depths | Hunger of the Dark (Force) | Neth Kyr (`cq_darkhung`) | `darkhung` | `hunger_of_the_dark` | 115 |
| Korriban Sith Crypt Depths | Eclipse of Resolve (Force) | Acolyte Varn (`cq_eclipse`) | `eclipse` | `eclipse_of_resolve` | 115 |
| Viscara Republic Engineering Bunker | Killzone Beacon (Devices) | Aric Jorr (`cq_killbeacon`) | `killbeacon` | `killzone_beacon` | 116 |
| Viscara Republic Engineering Bunker | Emergency Bunker (Devices) | Nella Voss (`cq_embunker`) | `embunker` | `emergency_bunker` | 116 |
| Viscara Republic Engineering Bunker | Decisive Command (Leadership) | Varen Kell (`cq_deccommand`) | `deccommand` | `decisive_command` | 116 |
| Dantooine Medical Sublevel | Hold the Line (Leadership) | Edda Maln (`cq_holdline`) | `holdline` | `hold_the_line` | 117 |
| Dantooine Medical Sublevel | Emergency Cocktail (FirstAid) | Kavi Dorn (`cq_emcocktail`) | `emcocktail` | `emergency_cocktail` | 117 |
| Dantooine Medical Sublevel | Infinite Conduit (Saberstaff) | Tessa Quell (`cq_infconduit`) | `infconduit` | `infinite_conduit` | 117 |
| Dathomir Tarn Jungle Preserve | Apex Bite (BeastMastery) | Talra Venn (`cq_apexbite`) | `apexbite` | `apex_bite` | 118 |
| Dathomir Tarn Jungle Preserve | Unbreakable Beast (BeastMastery) | Oren Krast (`cq_unbrbeast`) | `unbrbeast` | `unbreakable_beast` | 118 |
| Dathomir Tarn Jungle Preserve | Alpha Rhythm (BeastMastery) | Mira Syth (`cq_alpharhy`) | `alpharhy` | `alpha_rhythm` | 118 |
| Dathomir Grotto Apex Den | Primal Overrun (BeastMastery) | Nalka Rinn (`cq_primover`) | `primover` | `primal_overrun` | 119 |
| Dathomir Grotto Apex Den | Untouchable Instinct (BeastMastery) | Voro Thane (`cq_untinst`) | `untinst` | `untouchable_instinct` | 119 |
| Dathomir Grotto Apex Den | Force-Bonded Beast (BeastMastery) | Eshka Korr (`cq_forcebeast`) | `forcebeast` | `force_bonded_beast` | 119 |

## Blood Frenzy

Blood Frenzy is the Vibroblade 50 capstone quest line.

### Current Implementation State

- `BloodFrenzyQuestDefinition` defines the five-quest chain and requires `Vibroblade` rank 50 on each quest.
- The Blood Frenzy perk requirement points at the final quest id, `blood_frenzy_mastery`.
- Sera Vonn exists as the quest giver and is placed in Veles.
- Accepting `blood_frenzy_blade` grants the `ViscaraSewersDepthsKey` key item, displayed to players as `Viscara Sewers Depths Key`. Abandoning that opening quest before completion removes it.
- The Sewers Depths access point uses the existing `tele_obj` teleport placeable pattern. The placed object is named `Enter Sewers Depths` and uses `OnUsed = teleport`, `KEY_ITEM_ID = ViscaraSewersDepthsKey`, `DESTINATION = VISC_SEWER_DEPTHS_INSIDE`, and `TELEPORT_PARTY_MEMBERS = 1`. This follows the existing click-object-to-boss-area pattern used elsewhere instead of introducing a special door flow, while allowing nearby party members within 8 meters of the object to enter with the key holder.
- Blood Frenzy enemies and proof key items exist as new content.
- Blood Frenzy proof items are granted as temporary key items when the relevant kill objective advances the quest. They are removed on quest completion or abandonment and are not required corpse loot.
- Dedicated Viscara Sewers Depths spawn tables exist:
  - `VISCARA_SEWERS_DEPTHS_GENERAL`
- Viscara Sewers Depths loot tables exist for each Blood Frenzy creature and contain ordinary rewards only, not progression-critical proof:
  - `VISCARA_SEWERS_DEPTHS_SCAVENGER`
  - `VISCARA_SEWERS_DEPTHS_PULSE_DROID`
  - `VISCARA_SEWERS_DEPTHS_BUTCHER`
  - `VISCARA_SEWERS_DEPTHS_DUELIST`
  - `VISCARA_SEWERS_DEPTHS_KING`
- The Sewers Depths inside teleport waypoint is intentionally hand-placed with tag `VISC_SEWER_DEPTHS_INSIDE`; it does not need a palette blueprint.
- Sewers Depths waypoint palette templates exist for the ambient spawn table and optional boss spawn points:
  - `bf_sd_general` -> `VISCARA_SEWERS_DEPTHS_GENERAL`
  - `bf_butch_spawn` -> `BF_BUTCHER_SPAWN`
  - `bf_kess_spawn` -> `BF_KESS_SPAWN`
- The normal `VISCARA_VELES_SEWERS` table intentionally remains low-level and must not contain Blood Frenzy enemies.
- `bf_butch_call` is the Blood Frenzy Butcher on-demand quest encounter activator instance. It is configured for `blood_frenzy_glass` state 1, uses `OnUsed = quest_enc`, spawns `bf_butcher` at `BF_BUTCHER_SPAWN` when that waypoint exists, has a 60-minute starter cooldown, and hides itself outside the intended quest state. Its placed `LocName` is intentionally `???`, and it must remain a world instance instead of being re-added to the placeable blueprint palette.
- `bf_kess_call` is the Kess Draavo on-demand quest encounter activator instance. It is configured for `blood_frenzy_mastery` state 1, uses `OnUsed = quest_enc`, spawns `bf_kess` at `BF_KESS_SPAWN` when that waypoint exists, has a 60-minute starter cooldown, and hides itself outside the intended quest state. Its placed `LocName` is intentionally `???`, and it must remain a world instance instead of being re-added to the placeable blueprint palette.
- The on-demand humanoid bosses use humanoid Blood Frenzy ability packages, not creature-only attacks:
  - Blood Frenzy Butcher: `Rending Carve`, `Stim Canister`, `Blood Frenzy Flurry`, `Brutal Bash`
  - Kess Draavo: `Blood Frenzy`, `Blood Frenzy Flurry`, `Concussive Challenge`, `Stim Canister`, `Serrated Slash`, `Brutal Bash`, `Tactical Mark`
- Kess Draavo's blueprint has `BloodFrenzyTrait` and `PERK_LEVEL_322 = 1` so the final boss uses the same generic Blood Frenzy defeated-enemy stat behavior the player unlocks.
- `Concussive Challenge` is a player-facing boss threat, not an enmity-only move: it deals heavy Sonic damage and inflicts Dazed in a caster-centered area.

### Placement Status

The current module content includes the level 50 Viscara Sewers Depths area and a separate Kess arena area.

- Sera Vonn is hand-placed in Veles Shops.
- `Enter Sewers Depths` is a standard `tele_obj` instance in Veles Sewers and points to the hand-placed `VISC_SEWER_DEPTHS_INSIDE` waypoint.
- The general Sewers Depths spawn table is wired through `VISCARA_SEWERS_DEPTHS_GENERAL`.
- `bf_butch_call` is placed in Viscara Sewers Depths with `LocName = ???` and spawns the Butcher at `BF_BUTCHER_SPAWN`.
- `bf_kess_call` is placed in the Kess arena with `LocName = ???` and spawns Kess at `BF_KESS_SPAWN`.
- Neither `bf_butch_call` nor `bf_kess_call` should exist in the placeable blueprint palette or as `Module/utp` files. Their placed area instances are intentional.
- The Sewers Depths area has a teleport to the Kess arena, and the arena has a return teleport to `VISC_SEWERS_ARENA_EXIT`.

The full in-game progression pass for Blood Frenzy was completed on 2026-07-11. The line is fully validated.

### Area Builder Follow-Ups

- Keep the Sewers Depths access point as a standard `tele_obj` instance instead of a Blood Frenzy-specific access blueprint. Hand-place the inside destination waypoint in the target space with tag `VISC_SEWER_DEPTHS_INSIDE`, or update `DESTINATION` if the inside waypoint tag changes.
- Keep the Sewers Depths spawn waypoint from the custom waypoint palette: `bf_sd_general` for general-purpose enemies. Do not add this directly to the low-level Veles sewer area.
- Ensure normal low-level Veles sewer routes do not path through these encounters.
- Add access gating, transition placement, or encounter boundaries so lower-level players do not wander into capstone enemies by accident.
- Keep the area-instance-only `bf_butch_call` in the final stim lab encounter space with its intentional `???` name and keep `bf_butch_spawn` where the Blood Frenzy Butcher should appear. Do not add `bf_butch_call` back to the placeable blueprint palette.
- Keep the area-instance-only `bf_kess_call` in the final Blood Frenzy circle encounter space with its intentional `???` name and keep `bf_kess_spawn` where Kess should appear. Do not add `bf_kess_call` back to the placeable blueprint palette. Use the same `quest_enc`/`QUEST_ENCOUNTER_*` pattern for future on-demand quest encounters.
- Confirm there are enough spawn points for:
  - Red Vein Scavengers
  - Pulse-Frame Training Droids
  - Blood Frenzy Duelists
- Confirm the on-demand spawn positions for:
  - Blood Frenzy Butcher
  - Kess Draavo, the Blood Frenzy King
- Keep Sera Vonn's dialogue, the Blood Frenzy journal text, and the access key item naming aligned on `Viscara Sewers Depths`.
- The in-game progression pass from quest 1 through Blood Frenzy purchase is complete (2026-07-11). No open follow-ups remain unless the area layout changes.

## Dathomir Grotto Apex Den

The Dathomir Grotto Apex Den content package hosts the Primal Overrun, Untouchable Instinct, and Force-Bonded Beast capstone lines (BeastMastery). Placement was completed on 2026-07-11 and is covered by `DathomirGrottoApexDenPlacementTests`.

### Placement Status

- The dungeon is `pw_sc_dath_apexd` (`Dathomir - Grotto Apex Den`, tag `pw_sc_dath_apexden`). The attached boss arena is `pw_sc_dath_sden` (`Dathomir - Sealed Apex Den`, tag `pw_sc_dath_sden`; the tag was corrected from a duplicated `pw_sc_dath_apexden`). The two areas are door-linked via `dath_to_apexsealed`/`dath_sealedden`.
- The three quest givers are distributed across Dathomir's safe non-dungeon hubs: Nalka Rinn (`cq_primover`) at Jungle Landing (`dath_landingpad`), Voro Thane (`cq_untinst`) at the Czerka Base (`dath_cz_baseok`), and Eshka Korr (`cq_forcebeast`) at the Waterfall Ruins (`dath_waterfallru`). Each quest journal return instruction names its giver's actual hub. Quest givers must not stand in the Grotto Caverns, which have ambient enemy spawns.
- The access point is a standard `tele_obj` instance named `Enter the Grotto Apex Den` at the fire-pit camp inside `Dathomir - Grotto Caverns`' anti-spawn zone (`dathgrottocavern`), using `KEY_ITEM_ID = 119` (`CapstoneDathomirGrottoApexDenKey`), `DESTINATION = DATH_APEX_DEN_INSIDE`, and `TELEPORT_PARTY_MEMBERS = 1`. A matching `Exit the Apex Den` `tele_obj` inside the dungeon returns players to the hand-placed `DATH_APEX_DEN_OUTSIDE` waypoint at the camp.
- The dungeon uses the area-local spawn wiring pattern (`CREATURE_SPAWN_TABLE_ID = CAPSTONE_DATHOMIR_GROTTO_APEX_DEN`, `IS_DUNGEON = 1`, `MINI_MAP_DISABLED = 1`, `MAP_KEY_ITEM_ID = 65`), so only adept/specialist/inner-circle lesson enemies spawn ambiently. The generated `wp_cap_dgrot` waypoint blueprint remains available if fixed spawn points are preferred later.
- Six `quest_enc` activator instances are split by quest stage: the three wardens (`primov_wd_call`, `untinst_wd_call`, `fbeast_wd_call`) and their spawn waypoints are in the Grotto Apex Den dungeon, while the three masters (`primov_ms_call`, `untinst_ms_call`, `fbeast_ms_call`) and their spawn waypoints are in the Sealed Apex Den arena. Each uses the full `QUEST_ID`/`QUEST_STATE`/`QUEST_ENCOUNTER_*`/`VISIBILITY_*` local set, `LocName = ???`, a 60-minute starter cooldown, and a 10-minute idle despawn. None of the activators exist as palette blueprints or `Module/utp` files; the placed instances are the source of truth.
- `STUCK_WAYPOINT` instances exist in both new areas.
- Journal text, dialogue, and the access key item name the `Dathomir Grotto Apex Den`; journal return instructions name each giver's actual safe hub. The keyed dungeon entrance remains at the fire-pit camp in the Grotto Caverns, but no quest giver stands in that spawn-bearing area.

### Area Builder Follow-Ups

- Placements were made from module JSON coordinates, not in the toolset. Verify in the toolset or in game that the three giver placements at Jungle Landing, the Czerka Base, and the Waterfall Ruins; the gate camp in the Grotto Caverns; the dungeon entry and warden placements; and the arena master placements sit on valid walkmesh, and nudge as needed.
- The decorative `dath_apexentrance` door in the dungeon is unlinked by design; entry and exit flow through the `tele_obj` pair. Remove or relocate the door only alongside a layout pass.
- Run the full in-game progression pass for all three lines (quest 1 through capstone perk purchase) before calling the package validated.
