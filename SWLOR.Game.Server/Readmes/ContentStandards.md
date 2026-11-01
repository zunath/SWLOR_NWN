# Content Standards

Read before adding or changing items, enemies, loot, balance values, player head models, or locations players reach by ship.

Sections: Balance · Item generation · Loot · Characters · Ship-access locations

## Balance

Balance follows the existing standards. A new tier, a new direction, or any value outside the standard needs the user's approval before you build it. Bring the standard you checked and the deviation you propose.

- Enemies: start from the Design Bible's `Enemy Builder Guide` tab, with `Enemy Stat Presets`, `Enemy Resistance Packages`, `Enemy Ability Packages`, `Enemy Modifiers`, `World NPC Weapon Delays`, and `World NPCs`. Creature HP follows `NpcHitPoints.md`.
- Items: `Equipment - Weapons`, `Equipment - Armor`, `Equipment - Enhancements`, `Equipment - Droids`, and `Starships` hold the stat standards per tier; the recipe tabs and `Crafting Calc` hold crafting. Player caps are on `Character Stats` (attributes cap at 26 purchased, 27 with the racial bonus).
- Match existing items of the same type and tier in stats, level, and how they are obtained (crafted, dropped, sold, rewarded). Changing whether something requires crafting is a change of direction.
- Never reason from an assumed cap or range. Evaluate formulas across the real range and cite where each number came from.
- Skills and perks also follow [SkillPerkStandards.md](SkillPerkStandards.md) for family-specific progression, cross-skill design, descriptions, and balance evidence.

## Item generation

- Unless the user says otherwise, generated equipment the appearance editor covers (armor, helmets, cloaks, and main-hand and off-hand weapons and shields) uses only appearances players can select there. The source of truth is the catalogs in `Feature/AppearanceDefinition/ItemAppearance/`: armor parts per slot (`GeneralArmorAppearanceDefinition`, `DroidArmorAppearanceDefinition`, which also hold the helmet and cloak model lists) and weapon parts per base item (`*AppearanceDefinition` for each weapon). `AppearanceEditorViewModel.IsValidItem()` refuses to edit an item with any part outside them. Anything else is off-limits.
- Every other base type, including gloves, boots, belts, rings, amulets, bracers, and non-equipment such as recipes, components, food, and furniture, reuses models and icons already on existing player items of the same base type.
- In a batch, give every item its own appearance wherever its base type offers more than one. If the available options cannot supply enough distinct appearances, stop and ask.
- The base item type must match what the item is. Name, description, Design Bible slot (`Equipment - Weapons` column D, `Equipment - Armor` column C), and `BaseItem` must agree: a cloak is never a glove. Run the `EquipmentTypeTests` filter after adding or changing wearables; it matches item names to base types and checks the workbook.
- Dyeable armor and helmet imports keep existing dye colors unchanged and take metal reflectance from the palette: no specular or roughness map over Metal 1 or Metal 2 (only the grandfathered `legacyMetalSpecular` materials keep theirs), and `SWLOR_Haks/tools/CheckDyeAppearance.py` must pass (`TintMapRendering.md`, "Keep dye colors stable").
- Item models also follow `ItemAppearanceRules.md` (reserved helmet model 223, `ModelPart1`/`xModelPart1` consistency, embedded item copies). Item blueprints follow `EconomyRestrictedItems.md` (economy flags and obtainability) and `IconStandards.md`.
- Wearable geometry, skinning and animation changes follow `GarmentModelRules.md`: preserve accepted fit/motion and verify clipping on the affected parts and robe combinations.

## Loot

- Training sabers, training saberstaves, lightsabers, and saberstaves never appear in loot tables of any kind, including rare, boss, chest, and steal tables. Their acquisition belongs to the progression and crafting systems. Validate loot by blueprint base item type, not display name.

## Characters

- Player characters use only the normal phenotype. The large phenotype is not available to players, so do not build, fix, or verify player assets or code paths for it. Leave the existing large-body fallbacks in the tint renderers alone. `ruleset.2da`'s `CHARGEN_ENABLED_PHENOTYPES` still lists the large phenotype, but the native character-creation controls are moved off screen; players set up their body in game, where no phenotype choice exists. Do not change that setting.
- A head model players can select (`p??0_headNNN`) has a triangle cap per level of detail, counted across every mesh node in the compiled model: LOD 0 (the full model, seen up close) 30,000, LOD 1 10,000, and LOD 2 3,000. A head above 10,000 triangles must ship LOD 1 and LOD 2 models; a head with no LOD models stays within 10,000. Heads render on every character on screen, so the distant levels keep crowds within the range of the existing heads: of the 1,109 `p??0_headNNN` models on haks `master` in October 2026, the median is 840 triangles and the 90th percentile 5,078, and the 199 numbered 200 and up have a median of 4,058 and a 90th percentile of 9,405. A head above a cap needs the user's approval. The 23 existing heads above 10,000 triangles have no LOD models; they stay as they are unless a request covers them, but a replacement of one of them must meet these caps.
- Only the playable species matter: the appearances registered in `RacialAppearanceRegistry` (Human, Bothan, Chiss, Zabrak, Twi'lek, Mirialan, Echani, Kel Dor, Cyborg, Cathar, Rodian, Trandoshan, Togruta, Wookiee, Mon Calamari, Ugnaught, Droid, Nautolan, Ewok). Their bodies use the human (`p?h`), elf (`p?e`: Bothan, Rodian, Wookiee), half-orc (`p?o`: Cathar), dwarf (`p?d`: Ugnaught, Ewok) and droid (`p?z`) model families. The base NWN race appearances other than Human (elf, half-elf, half-orc, dwarf, gnome, halfling) are not playable, and no species uses the gnome (`p?g`) or halfling (`p?a`) models. Scope fixes, imports, generators, audits, guards and client evidence to playable species on the normal phenotype; do not spend work on anything else.

## Ship-access locations

A location players can only reach by player ship, such as a dungeon, needs both of these:

- **A Dock Master at the landing point**, Plot and on a neutral faction that the area's enemies do not attack. Build it on the `spc_dockhand` blueprint with `CONVERSATION=StarportDialog`, `STARPORT_TELEPORT_WAYPOINT`, `STARPORT_LANDING_WAYPOINT`, and `SHIP_BOARDING_OUT_OF_COMBAT=1`, and place a `dockhand_store` store beside it: `StarportDialog` always offers "View Shop" and opens the nearest one. The reference is the Abandoned Station: `aban_dockmaster` in `zomb_abanstation`, reached from an orbit `spc_dock` placeable with `STARPORT_LANDING_WAYPOINT_TAG`. Its landing waypoint is deliberately not tagged `STARSHIP_DOCKPOINT`, which keeps it out of the planet's docking menu.
- **Return to safety on restart.** Set the area local int `SHIP_ONLY_LOCATION=1` on every area of the location, and give each one `PLANET_TYPE_ID`. A ship left at the location returns to its most recent safe dock (the last regular starport it docked at) when the server restarts. Being stranded until the next reset is intended, since players must coordinate to retrieve the ship; the reset is their fallback.

How the return works (`Service/PropertyService/StarshipDockReturn.cs`):

- A ship's safe dock is `LastNPCDockPosition`, and its safe orbit is `LastNPCSpacePosition`. `StarportDockDialog` records both only when the landing area is not marked, so a ship-only dock never becomes the safe dock.
- On module load, `Property.CleanUpData` calls `StarshipDockReturn`. A ship docked in a marked area, or out in space after leaving one, returns to its safe dock and orbit. A ship with no usable safe dock goes to the starport of the marked area's planet. Ships at regular starports are left alone.
- Never mark a regular starport area. `StarshipDockReturnTests` fails if a planet landing area or a `STARSHIP_DOCKPOINT` area carries the marker, and checks that every marked area declares `PLANET_TYPE_ID`. The `StarshipDockReturn` engine-test category covers the dock menus and the restart.
