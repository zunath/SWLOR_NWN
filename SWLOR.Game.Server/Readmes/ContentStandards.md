# Content Standards

Read before adding or changing items, enemies, loot, balance values, or locations players reach by ship.

Sections: Balance · Item generation · Loot · Characters · Ship-access locations · Space content

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
- Item models also follow `ItemAppearanceRules.md` (reserved helmet model 223, `ModelPart1`/`xModelPart1` consistency, embedded item copies). Item blueprints follow `EconomyRestrictedItems.md` (economy flags and obtainability) and `IconStandards.md`.

## Loot

- Training sabers, training saberstaves, lightsabers, and saberstaves never appear in loot tables of any kind, including rare, boss, chest, and steal tables. Their acquisition belongs to the progression and crafting systems. Validate loot by blueprint base item type, not display name.

## Characters

- Player characters use only the normal phenotype. The large phenotype is not available to players, so do not build, fix, or verify player assets or code paths for it. Leave the existing large-body fallbacks in the tint renderers alone. `ruleset.2da`'s `CHARGEN_ENABLED_PHENOTYPES` still lists the large phenotype, but the native character-creation controls are moved off screen; players set up their body in game, where no phenotype choice exists. Do not change that setting.
- Only the playable species matter: the appearances registered in `RacialAppearanceRegistry` (Human, Bothan, Chiss, Zabrak, Twi'lek, Mirialan, Echani, Kel Dor, Cyborg, Cathar, Rodian, Trandoshan, Togruta, Wookiee, Mon Calamari, Ugnaught, Droid, Nautolan, Ewok). Their bodies use the human (`p?h`), elf (`p?e`: Bothan, Rodian, Wookiee), half-orc (`p?o`: Cathar), dwarf (`p?d`: Ugnaught, Ewok) and droid (`p?z`) model families. The base NWN race appearances other than Human (elf, half-elf, half-orc, dwarf, gnome, halfling) are not playable, and no species uses the gnome (`p?g`) or halfling (`p?a`) models. Scope fixes, imports, generators, audits, guards and client evidence to playable species on the normal phenotype; do not spend work on anything else.

## Ship-access locations

A location players can only reach by player ship, such as a dungeon, needs both of these:

- **A Dock Master at the landing point**, Plot and on a neutral faction that the area's enemies do not attack. Build it on the `spc_dockhand` blueprint with `CONVERSATION=StarportDialog`, `STARPORT_TELEPORT_WAYPOINT`, `STARPORT_LANDING_WAYPOINT`, and `SHIP_BOARDING_OUT_OF_COMBAT=1`, and place a `dockhand_store` store beside it: `StarportDialog` always offers "View Shop" and opens the nearest one. The reference is the Abandoned Station: `aban_dockmaster` in `zomb_abanstation`, reached from an orbit `spc_dock` placeable with `STARPORT_LANDING_WAYPOINT_TAG`. Its landing waypoint is deliberately not tagged `STARSHIP_DOCKPOINT`, which keeps it out of the planet's docking menu.
- **Return to safety on restart.** Set the area local int `SHIP_ONLY_LOCATION=1` on every area of the location, and give each one `PLANET_TYPE_ID`. A ship left at the location returns to its most recent safe dock (the last regular starport it docked at) when the server restarts. Being stranded until the next reset is intended, since players must coordinate to retrieve the ship; the reset is their fallback.

How the return works (`Service/PropertyService/StarshipDockReturn.cs`):

- A ship's safe dock is `LastNPCDockPosition`, and its safe orbit is `LastNPCSpacePosition`. `StarportDockDialog` records both only when the landing area is not marked, so a ship-only dock never becomes the safe dock.
- On module load, `Property.CleanUpData` calls `StarshipDockReturn`. A ship docked in a marked area, or out in space after leaving one, returns to its safe dock and orbit. A ship with no usable safe dock goes to the starport of the marked area's planet. Ships at regular starports are left alone.
- Never mark a regular starport area. `StarshipDockReturnTests` fails if a planet landing area or a `STARSHIP_DOCKPOINT` area carries the marker, and checks that every marked area declares `PLANET_TYPE_ID`. The `StarshipDockReturn` engine-test category covers the dock menus and the restart.

## Space content

Rules future space content (ships, modules, enemies, trade, activities, droids) must follow. The numbers live in the Design Bible's Revamp tabs and `tools/SpaceRevampBalance/`; change a generator and regenerate, never hand-edit generated tuning.

- **Ships:** a ship is a role (14 roles in 4 sizes) plus a manufacturer theme plus one signature bonus, from one Bible formula. Two ships of a role are sidegrades, never strict upgrades. Piloting gates are Small 0, Medium 10, Large 25, Capital 40; an Advanced ship is 10 higher, needs a Ship Schematic and one rare component, and adds one Mid or Low slot or a stronger signature, never both and never a High slot. Every Capital ship is built at a station Shipyard. Gating is a `RequiresSkill` item property on the hull or module; no perk unlocks equipment. Bonuses grant `StatType` values, never ship-name switches.
- **Size rules:** a weapon larger than its target loses hit chance per size step; a smaller one loses damage per step; torpedoes ignore both. A module scales with the hull it is fitted to; Point-Defense always fires Light and capital weapons fire Capital. Repair bills scale Small 1.0, Medium 1.5, Large 2.5, Capital 4.0. Capital hulls dock only at Veles, Mon Cala, Anchorhead, Smuggler's Moon Station and Korriban, every NPC dock takes Large and smaller, and a player-city starport takes Large (Capital needs starport upgrade level 2). A ship already docked somewhere too small stays until it leaves.
- **Modules:** three rows (High, Mid, Low) with Helm or station slot tags; no configuration slot. Every family has an item every 5 ranks (0-45), rank N needs rank N in the owning skill, capital ladders start at 40, and rank 50 is only for named boss modules. Each step carries its own Attack, Accuracy, Defense and Evasion, because Piloting rank adds no stats.
- **Crew:** Large hulls have Pilot, Gunner and Engineer; Capital hulls Pilot, 2 Gunners, Engineer and Sensors; Small and Medium are solo. A station slot works only while its seat is crewed. A seat works at no more than 105% (after Crew Morale and the captain's Leadership), so a ship crewed by N players never beats N separate ships. A droid works Engineer or Sensors seats at up to 75% and Gunner seats at up to 50% (Industry 100), and any combat droid at 25%. Crew count as party members for kill XP, loot, wreck claims, agent missions and incursions; cargo and trade profit stay with the owner.
- **Droids:** industrial parts cost about half the materials of combat parts, have lower HP and attributes and no combat skill bonuses, and give an Industry rating (4 x tier per part, summed). A droid posted to a ship seat, bay, holding or station building does not count toward the one-droid follow limit. A new Industry consumer needs a draft number in `tools/SpaceRevampBalance/droids.py`.
- **Enemies:** every NPC ship has a `RevampNpcShipCatalog` preset with a size (Fighter, Interceptor, Bomber Small; Gunship, Cargo Medium; Platform Large; capitals Capital) and a role behavior from `ProcessSpaceNPCAI`. The faction ladder is fixed: T1 Septra Khanate, T2 Krakenborne, T3 Snowdrift Syndicate, T4 Clan Zel Arun, T5 Red Hand, T6 Imperial. Stats come from the Bible's enemy tabs and `NpcHitPoints.md`. Aces and other named pilots follow `skills/swlor-rare-spawn-generation/SKILL.md`.
- **Income bands** (credits per hour as a share of the tier's space combat, the baseline): active activities 100-140%; open trade in legal goods 100-125%; contraband after customs and pirates 150-200%; freight contracts about 75%; mining and strip mining without perks about 90-100% and at most 140% with them; exploring with store-valued schematics at most 120%; derelict salvage teams 60-70% of boarding; passive income (holdings, drills) at most 90 minutes of combat a day per character. Check a new activity with its generator's `--sim` and `bible_tabs.py --income` before proposing numbers.
- **Trade:** unit price = base x factor x exp(-pressure / depth), flood multiplier within 0.35-2.5, a 6% exchange spread each way, pressure halving every hour; buying and selling back at one exchange always loses. Base prices come from comparable item Costs. Exchanges create credits, so flooding, fees, deposits and cargo loss are the inflation guard and a trade change must keep the bands above.
- **Rewards:** space sites, slicing and agent missions pay through the generated reward tables; faction tokens are a currency (not items) worth about 100 credits each. A new reward source needs its payout in the income table.
- **Ship-only locations** also follow "Ship-access locations" above.
