# Content Standards

Read before adding or changing items, enemies, loot, balance values, or locations players reach by ship.

Sections: Balance · Item generation · Loot · Characters · Ship-access locations

## Balance

Balance follows the existing standards. A new tier, a new direction, or any value outside the standard needs the user's approval before you build it. Bring the standard you checked and the deviation you propose.

- Enemies: start from the Design Bible's `Enemy Builder Guide` tab, with `Enemy Stat Presets`, `Enemy Resistance Packages`, `Enemy Ability Packages`, `Enemy Modifiers`, `World NPC Weapon Delays`, and `World NPCs`. Creature HP follows `NpcHitPoints.md`.
- Items: `Equipment - Weapons`, `Equipment - Armor`, `Equipment - Enhancements`, `Equipment - Droids`, and `Starships` hold the stat standards per tier; the recipe tabs and `Crafting Calc` hold crafting. Player caps are on `Character Stats` (attributes cap at 26 purchased, 27 with the racial bonus).
- Match existing items of the same type and tier in stats, level, and how they are obtained (crafted, dropped, sold, rewarded). Changing whether something requires crafting is a change of direction.
- Never reason from an assumed cap or range. Evaluate formulas across the real range and cite where each number came from.

## Item generation

- Unless the user says otherwise, generated equipment the appearance editor covers (armor, helmets, cloaks, and main-hand and off-hand weapons and shields) uses only appearances players can select there. The source of truth is the catalogs in `Feature/AppearanceDefinition/ItemAppearance/`: armor parts per slot (`GeneralArmorAppearanceDefinition`, `DroidArmorAppearanceDefinition`, which also hold the helmet and cloak model lists) and weapon parts per base item (`*AppearanceDefinition` for each weapon). `AppearanceEditorViewModel.IsValidItem()` refuses to edit an item with any part outside them. Anything else is off-limits.
- Every other base type, including gloves, boots, belts, rings, amulets, bracers, and non-equipment such as recipes, components, food, and furniture, reuses models and icons already on existing player items of the same base type.
- In a batch, give every item its own appearance wherever its base type offers more than one. If the available options cannot supply enough distinct appearances, stop and ask.
- The base item type must match what the item is. Name, description, Design Bible slot (`Equipment - Weapons` column D, `Equipment - Armor` column C), and `BaseItem` must agree: a cloak is never a glove. Run the `EquipmentTypeTests` filter after adding or changing wearables; it matches item names to base types and checks the workbook.
- Item models also follow `ItemAppearanceRules.md` (reserved helmet model 223, `ModelPart1`/`xModelPart1` consistency, embedded item copies). Item blueprints follow `EconomyRestrictedItems.md` (economy flags and obtainability) and `IconStandards.md`.

## Loot

- Training sabers, training saberstaves, lightsabers, and saberstaves never appear in loot tables of any kind, including rare, boss, chest, and steal tables. Their acquisition belongs to the progression and crafting systems. Validate loot by blueprint base item type, not display name.

## Characters

- Player characters use only the normal phenotype. The large phenotype is not available to players, so do not build, fix, or verify player assets or code paths for it. Leave the existing large-body fallbacks in the tint renderers alone.

## Ship-access locations

A location players can only reach by player ship, such as a dungeon, needs both of these:

- **A Dock Master at the landing point**, Plot and on a neutral faction that the area's enemies do not attack. Build it on the `spc_dockhand` blueprint with `CONVERSATION=StarportDialog`, `STARPORT_TELEPORT_WAYPOINT`, `STARPORT_LANDING_WAYPOINT`, and `SHIP_BOARDING_OUT_OF_COMBAT=1`, and place a `dockhand_store` store beside it: `StarportDialog` always offers "View Shop" and opens the nearest one. The reference is the Abandoned Station: `aban_dockmaster` in `zomb_abanstation`, reached from an orbit `spc_dock` placeable with `STARPORT_LANDING_WAYPOINT_TAG`. Its landing waypoint is deliberately not tagged `STARSHIP_DOCKPOINT`, which keeps it out of the planet's docking menu.
- **Return to safety on restart.** A ship left at the location returns to its most recent safe dock (the last regular starport it docked at) when the server restarts. Being stranded until the next reset is intended, since players must coordinate to retrieve the ship; the reset is their fallback.

The restart return is not implemented yet. `Property.CleanUpData` only clears `CurrentPosition` on module load, and `StarportDockDialog` treats a dungeon dock as an NPC dock, so `DockPosition` and `LastNPCDockPosition` both stay in the dungeon. Flag this when adding a ship-access location until it is fixed, and update this section when it is.
