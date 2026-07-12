# Capstone Quest Line Plan

This plan tracks quest-line rules for locking level 50 capstone perks behind quest completion.

## Global Rules

- Each capstone quest line must require the matching skill at rank 50 before the player can accept any quest in the line.
- Beast capstone quest lines must require the master's `BeastMastery` skill at rank 50 before the player can accept any quest in the line. Do not gate quest acceptance on the currently active beast's level or role; beast role remains a perk-line/content identity requirement, not a quest prerequisite.
- The perk unlock must require completion of the final quest in its capstone line.
- The capstone perk unlocked by the line must be assigned to the final boss of that quest line. For passive or trait capstones, assign the granted feat and the matching `PERK_LEVEL_<perk id>` local so the existing perk-stat path activates; for active capstones, assign the granted feat or spell through the boss ability package.
- Each capstone quest line should use new enemies, items, or encounters when it asks for kills, collection, investigation, or other tasks.
- Each capstone quest line must have its own dedicated quest giver NPC. Area groups may be reused by up to three capstone lines, but the player-facing trainer/requester for each capstone line must be distinct.
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

## Remaining Capstone Quest Setup

The skill-owned capstone quest definitions define the 39 remaining capstone quest lines after Blood Frenzy. Each line has five quests, rank prerequisites on every step, proof key items granted from quest credit, a final quest achievement, NPC group identifiers for every objective, deterministic enemy/waypoint/spawn/loot asset IDs, and a final quest ID used by the matching capstone perk requirement.

The reusable setup exists now: quest definitions, dedicated quest giver UTC/DLG files, creature palette entries for those quest givers, enemy UTCs, stat skins, weapons, spawn table definitions, loot table definitions, and waypoint palette blueprints. These lines are not ready for in-game progression testing until their areas, gated access objects, placed quest givers, placed spawn waypoints, and `quest_enc` boss activator instances are created.

No `Module/git` placement should be added for these lines until the target content package exists. Area builders may reuse each content package for up to three capstone lines, but must keep the level 50 content isolated from low-level or general-purpose spawn spaces.

For capstone planning, a content package is not a single physical area. Following the Blood Frenzy pattern, each content package requires two attached physical areas:

- One gated dungeon or lesson area containing the ambient level 50 capstone enemies and general spawn waypoint.
- One attached boss arena area containing the state-gated `quest_enc` activators and boss spawn waypoints.

The 13 content packages below therefore represent 26 physical areas to build: 13 dungeons and 13 attached boss arenas.

### Generated Reusable Content

- General lesson enemies use one spawn table per area group. Each table contains only quest steps 1, 2, and 4: adept, specialist, and inner circle enemies.
- Quest step 3 wardens and quest step 5 final masters are intentionally excluded from ambient spawn tables. They must be spawned through `quest_enc` activator instances only.
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
- Place each capstone line's dedicated quest giver NPC. Do not collapse multiple capstone quest lines onto one shared area trainer even when the lines reuse the same content package.
- Add a standard access object or equivalent gate that checks the content package's capstone key item and sends the player into the dungeon/lesson area.
- In the dungeon/lesson area, place the general spawn waypoint using the generated waypoint resref above. Its tag already matches the generated spawn table ID.
- In the dungeon/lesson area, use the generated level 50 spawn table for general lesson enemies only. Do not place the table in low-level, public, or boss arena areas.
- In the attached boss arena area, place `quest_enc` activator instances for on-demand bosses with state-specific visibility, 60-minute starter cooldown, one-active-creature guard, participant quest credit, and idle despawn.
- For every line, place one warden activator for quest step 3 and one master activator for quest step 5. Each activator should use the generated boss UTC resref and generated boss spawn waypoint tag defined by the owning quest line and generated module assets, such as `cp_invinc_wd` with `CAPSTONE_INVINC_WD_SPAWN` or `cp_invinc_ms` with `CAPSTONE_INVINC_MS_SPAWN`. Copy literal handoff values into setup notes when an area builder needs them; do not make quest constants public for handoff tooling.
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

Do not consider the Blood Frenzy line fully validated until the in-game progression pass below is complete.

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
- Run an in-game progression pass from quest 1 through Blood Frenzy purchase after the final area placement is in place.
