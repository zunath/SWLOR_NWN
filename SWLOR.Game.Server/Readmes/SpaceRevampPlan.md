# Space Revamp Plan

Owner-approved design for rebuilding the space system so it matches the combat upgrade (`2fa527816`, "Feature/combat upgrade (#2277)"). The decisions came out of a design interview with the owner; every one of them is final unless this file marks it as a number to draft. The work is handed to an implementing agent. Faction, system and ship names are working names until the DM lore review (section 18).

Everything in this plan, Part I and Part II, ships in **one release**. The workstreams in section 18 organize the work, not the release.

Delete this file after release, the way the combat upgrade deleted its plan documents (`dee01b4fe`). Lasting rules move into the owning guides (section 15).

## Contents

1. Read first
2. Decisions at a glance
3. Current state
4. Skills
5. Attributes in space
6. Perks and abilities
7. Hulls
8. Modules
9. Combat rules
10. Crew stations
11. Enemies
12. Activities, zones, and materials
13. Trade
14. Crafting, rewards, and the economy
15. Migration
16. Bug list
17. Numbers to draft for approval
18. Workstreams and order
19. Verification
20. Trellis2 request list
21. Out of scope
22. Risks

Part II: Space Expansion

23. Wild Space
24. Scanning and exploration
25. Player stations
26. Planetary Holdings
27. Industrial droids
28. Derelict boarding
29. Faction agents
30. Incursions
31. Rift Runs
32. Cloaking

Appendices

- Appendix A: Perk drafts
- Appendix B: Module families
- Appendix C: The 100 ships
- Appendix D: Components and sourcing

---

## 1. Read first

Before any work, read `AGENTS.md` and these guides, plus every guide named in the section you are working on:

- `Readmes/SkillPerkStandards.md`: perk structure, mechanical contracts, balance evidence, descriptions.
- `Readmes/CodingStandards.md`: stat-driven gameplay (perks grant `StatType` values; no perk-name switches), Module JSON, TLK and 2DA.
- `Readmes/ContentStandards.md`: item, enemy, loot and ship-access standards.
- `Readmes/DesignBibleWorkbookRules.md`: the Bible is edited at the zip/XML level only, on one branch.
- `Readmes/Migrations.md`: one migration per release, server before player, saved-data item sweeps.
- `Readmes/PullRequests.md`, `Readmes/AgentEnvironment.md`, `Readmes/InEngineVisualVerification.md`, `Readmes/EngineTesting.md`.

**Standing rules for this project:**

- **The Design Bible comes first.** Every number this plan leaves open is drafted in the repo copy of `design/bible/SWLOR Design Bible.xlsx`, sent to the owner for approval, and only then coded. The owner uploads Bible changes to Google; agents never do.
- **No ground ability works in space, and no space ability works on the ground.** These are two separate systems. Ground stats are already stripped on entering space (`Space.EnterSpaceMode`); keep that.
- **No perk unlocks equipment.** Every hull and module is gated by a `RequiresSkill` item property, the same as ground gear.
- **Every new value outside an existing standard needs owner approval.** This plan sets the structure; section 17 lists the numbers still to be approved.

## 2. Decisions at a glance

| Area | Decision |
|---|---|
| Skills | Piloting (kept), Gunnery (new), Ship Systems (new). Space mining and salvage move to Gathering. All count toward the 400 skill cap. |
| Gating | Hulls and modules use `RequiresSkill`. The four unlock perk lines are deleted. |
| Perks | Each space skill gets 2 styles of 60 SP on the ground template: abilities, stances, traits, capstone. |
| Ships | 100 ships: 14 roles in 4 sizes, 8 manufacturers, each with a signature bonus. 74 Standard and 26 Advanced. Piloting gates 0/10/25/40, with Advanced 10 higher. |
| Looks | **Hull Designs** are skins for one specific ship. They launch from model variants and Trellis2 adds more. They're unlocked by Design Schematics and swapped by starport **Refit**. |
| Slots | High / Mid / Low. No configuration slot. |
| Modules | About 20 families, an item every 5 ranks (0-45), rank-50 named boss modules. A weapon fires at its hull's size. |
| Crafting | No new skill. Space crafting spreads across Smithery, Engineering, Fabrication, Agriculture and Gathering. A component layer has trade-off traits, manufacturer and rare components. Research blueprints work for hulls and modules. |
| Combat | Shared ground rules: crits, one Attack and one Defense, EM/Thermal/Explosive resistances, shared status effects, threat, PvP rules. |
| Attributes | Energy weapons use AGI accuracy and PER damage; launchers use PER accuracy and MGT damage, like ranged and melee weapons on the ground. AGI evasion, PER crit, VIT hull defense, WIL shields/capacitor/repair, MGT STM for maneuvers, SOC command and trade prices. |
| Activation | Modules with a cycle are toggles that fire themselves each cycle and switch off when they can't fire. Lit icons show what's on, and a Ship HUD shows targets. |
| Crew | Stations on Large and Capital hulls, with droid crew. Ships at launch. |
| Death | Modules drop only on PvP deaths in Wild Space. Everyone aboard ejects in escape pods and takes the normal XP debt. Cargo spills on any death. |
| Enemies | Every NPC ship gets a size and a role behavior. New groups: pirates, customs patrols, Hutt enforcers, derelict defenses, convoys, aces. |
| World | 3 new small-ship zones with 3 new materials. Salvage from wrecks. Starlane fixes. Size limits on docking. |
| Trade | Cargo held as ship data, about 20 commodities, dock exchanges with moving prices, contraband and customs, pirates, freight contracts, faction standing, city tariffs. |
| Migration | Every character gets one Rebuild Token. Piloting perks are refunded. Ships and modules convert to their new equivalents. |
| Wild Space | Six persistent tier-5.5 systems reached through hidden, rotating rifts, each with an environmental effect, held by the Rift Raiders pirate coalition. |
| Exploration | A Probe Launcher reveals hidden sites (rift, combat, data, cache, gas, derelict) and jumps the party to them. Data sites use the slicing mini-game. |
| Stations | Player cities in space, at anchor sites, with every city mechanic, plus a Shipyard (Capital hulls), Asteroid Drill and Jump Beacons. Stations can't be attacked in this release. |
| Holdings | Up to 3 planetary holdings per character, with cross-planet upgrades, production chains, droid staff, events and visitable ground sites. |
| Droids | Industrial parts and AI instructions trade combat for an Industry rating used by holdings, crew, droid bays and stations. |
| More activities | Derelict boarding (ground dungeons, some contested from space), faction agents at docks across the galaxy, weekly incursions, timed Rift Runs, and cloaking for Small hulls. |

## 3. Current state

Paths are relative to `SWLOR.Game.Server/`.

- **Skill:** `SkillType.Piloting = 7` (`Service/SkillService/SkillType.cs`), Utility, rank 50, counts toward the cap, `CombatPointCategoryType.Exempt`.
- **Perks** (`Feature/PerkDefinition/PilotingPerkDefinition.cs`):
  - Unlock lines: Starships, Defensive Modules, Offensive Modules, Mining Modules (5 levels each).
  - Others: Energy Management I-II, Starship Mining I-II, Intuitive Piloting.
- **Engine:** `Service/Space.cs` (about 2,500 lines), `Service/SpaceModuleRecast.cs`, and `Service/SpaceService/*`.
  - **Becoming the ship:** the player turns into the ship, and a pilot copy sits at `pilot_chair` in the interior.
  - **Modules on the hotbar:** modules become feats `ShipModule1-30`. High slots map to feats 1-10 and Low slots to 11-20; feats 21-30 are free.
  - **Running costs:** capacitor pays for modules, and there is a 2 s global recast.
- **Hulls** (`Feature/ShipDefinition/PlayerShipDefinition.cs`):
  - 17 player hulls in T1-T5 pairs, plus 10 capitals with identical stats.
  - Role comes from a single configuration module.
  - Enemy hulls: `NPCShipDefinition.cs` and `Feature/SpaceObjectDefinition/NPCSpaceObjectDefinition.cs`.
- **Modules:** `Feature/ShipModuleDefinition/*` (High, Low and Config slots).
  - **Requirements:** gated by perks through `ShipDetail.RequiredPerks` and `ShipModuleDetail.RequiredPerks`.
  - **Examine stamping:** `Space.ApplyShipModuleExamineDetails` and `ApplyShipExamineDetails` stamp `UseLimitationPerk` onto items when they are examined.
- **Combat:** `Combat.CalculateHitRate` and `Combat.CalculateDamage` are already shared with the ground.
  - Missing: crits, resistances, status effects and PvP checks.
  - Per-type attack and defense: separate values for EM, Thermal and Explosive.
- **Storage:** `Entity/PlayerShip.cs`.
  - `SerializedItem`: the deed.
  - `Status` (`ShipStatus`): `HighPowerModules`, `LowPowerModules` and `ConfigurationModules`, each a dictionary of `ShipStatusModule`.
  - `PlayerHotBars`: one saved hotbar per player.
- **Crafting:**
  - Recipes: `Feature/RecipeDefinition/EngineeringRecipeDefinition/*`. Module recipes use only refined ore plus electronics. Electronics come only from loot.
  - **Reserved ores:** Ochne, Croknor, Hemorgite and Bisteiss are reserved for tier 6 and later. **Do not use them.**
- **Unused hooks this plan uses:**
  - `AttachCamera` (`SWLOR.NWN.API/NWScript/CameraFunctions.cs`).
  - `VisibilityPlugin`.
  - `MarketRegionType` regions.
  - The faction service (8 factions, standing from -5000 to +5000).
  - `NWNX_ON_STORE_REQUEST_*` subscriptions with no handlers.
  - `Quest.RegisterRuntimeQuest`.
  - About 100 unused ship models in `SWLOR_Haks/sw_2da/appearance.2da` (most in `sw_cr_vehicle/`).

### Before and after

| Area | Today | After the revamp |
|---|---|---|
| Skills | Piloting only | Piloting, Gunnery, Ship Systems; space mining, gas and salvage in Gathering |
| Perks | 7 perks; 4 of them only unlock gear tiers (67 SP) | 6 styles of 18 perks (60 SP each) plus Gathering space perks; no perk unlocks gear (Appendix A) |
| Gear requirements | Perk levels stamped on examine | `RequiresSkill` rank on every hull and module |
| Hulls | 17 hulls in T1-T5 pairs, each strictly bigger, plus 10 identical capitals | 100 ships: 14 roles, 8 manufacturers, signature bonuses, Standard and Advanced (Appendix C) |
| Ship role | One configuration module | Built into the hull |
| Slots | High, Low, 1 Config | High, Mid, Low |
| Module tiers | Basic, I-IV | An item every 5 ranks (0-45) plus named rank-50 modules |
| Module families | 4 offensive, about 12 defensive, mining, 4 config, capital set | About 20 families (Appendix B) |
| Firing | Press each module; 2 s global recast | Toggle on; fires every cycle; switches off when it can't fire |
| Damage math | Separate attack and defense per damage type; no crits | One Attack and one Defense; EM, Thermal and Explosive resistances; crits |
| Effects | Ion slow hard-coded | Shared status effects, resistance families and hard crowd-control rules |
| Threat | E-War module only | Shared enmity; tank stance |
| Attributes | AGI, PER (or WIL), VIT | All six (section 5) |
| Shield regen | 1 point every N s; bigger hulls slower | Per second, growing with size and WIL |
| Speed | Same for every ship | Set per hull |
| PvP | Any ship can shoot any player ship | Ground hostility and area rules, PvP ramps |
| Ship death | 65% drop per module on every death; everyone inside killed | Modules drop only on PvP deaths in Wild Space; escape pods; cargo spills |
| Crew | Passengers only | Stations on Large and Capital hulls; droid crew |
| Mining | Piloting perks and XP | Gathering perks and XP; gas harvesting; salvage |
| Trade | None | Cargo, exchanges, contraband, pirates, freight contracts, factions |
| Zones | 13 open orbits plus the Abandoned Station | Abandoned Station becomes Small/Medium only; 3 new Small/Medium zones |
| Enemies | 7 hull types per faction, one shared AI | Sizes, 8 role behaviors, 5 new groups, aces, encounters |
| Docking | Any ship at any dock | Capitals at major ports; city starport level gates size |
| Crafting | Engineering makes everything from refined ore plus electronics | Smithery, Engineering, Fabrication, Agriculture and Gathering share the work; components with traits; research blueprints |
| Starlanes | Smuggler's Moon unreachable; Korriban return skips | New lanes; return fixed |

## 4. Skills

| Skill | ID | Gates | Rank adds | Combat point category |
|---|---|---|---|---|
| Piloting | 7 (kept) | Hull sizes; passive defenses (Shield Booster, Hull Plating, Resist Plating, Capacitor Battery); Afterburner, Thrusters, Cargo Expander, Cloaking Device | Evasion, speed | Exempt (full XP from every space kill, like Armor on the ground) |
| Gunnery | new (next free ID, 53 today) | Weapons, Targeting Computer, Damage Amplifier | Accuracy, attack, crit | New `Starship` category (split by contribution) |
| Ship Systems | new (54 today) | Active defense and support: shield and hull repairers, Shield Hardener, Hypermatter Injector, Capacitor Regulator, electronic warfare, tackle, Probe Launcher, Data Analyzer, Smuggling Compartment | Shield regen, capacitor regen, repair amount | New `Starship` category |
| Gathering | 11 (kept) | Mining Laser, Strip Miner, Salvager | Yield | Gathering XP from mining, gas harvesting and salvage |

- **Skill attributes:** Utility category, rank 50, active, `ContributesToSkillCap = true`.
- **Category value:** add `CombatPointCategoryType.Starship`. The loop in `Service/CombatPoint.cs` picks up new values automatically.
- **Mining rank:** a Mining Laser's `RequiresSkill` follows the module rank schedule in Gathering. The asteroid tier check still applies.
- **Why each skill is worth taking:**
  - **Minimum builds:**
    - Glass-cannon fighter (Small hull): Gunnery 50 (50 ranks).
    - Complete solo combat pilot: Piloting + Gunnery (100 ranks), the same as a ground melee fighter's weapon + Armor.
    - Tank or support pilot: Piloting + Ship Systems (up to 100 ranks).
    - Everything maxed: all three (150 ranks).
    - Crew member: one station skill (50 ranks).
    - Casual flyer: Piloting 10 + Gunnery 15 (25 ranks): Medium hulls and rank-15 weapons.
    - Space miner: Piloting 10 + Gathering 40 (50 ranks) for a Mining Barge on top-tier ore, or Piloting 25 + Gathering 40 (65 ranks) to strip-mine in an Industrial. Today a top space miner needs Piloting 40, so a pure space miner pays about 10 more ranks. The same Gathering also covers ground harvesting, salvage and gas. Player text must give these numbers, never claim mining got cheaper.
  - **Piloting works like the Armor skill.** It gates hulls and the passive defenses that every role wears, and it owns propulsion, cargo, cloaking, maneuvers and command. It also earns XP from trade.
  - **Ship Systems is the tank and support specialty,** like First Aid or Leadership on the ground. Nobody needs it just to survive.
- **XP rules:**
  - The XP range must cover the longest weapon range. Today it's 40 m against a 55 m missile.
  - Crew earn XP in their station's skill.
  - Completed freight contracts and exchange sales give Piloting XP, scaled by route length and orbit danger. Price flooding and contract limits stop back-and-forth farming.
  - PvP gives no XP (unchanged).

## 5. Attributes in space

| Attribute | Space job |
|---|---|
| AGI | Energy weapon accuracy and evasion (as today) |
| PER | Energy weapon damage, launcher accuracy, and crit for every weapon |
| VIT | Hull defense (as today) |
| WIL | Max shield, max capacitor, repair strength |
| MGT | Launcher damage, and the pilot's own STM, which pays for maneuvers and physical station actions |
| SOC | Command abilities and auras; better exchange prices; repair discount (as today) |

- **Weapons copy the ground mapping:**
  - Energy weapons (Laser, Ion and Beam Cannons, the Point-Defense Turret and the capital lasers) use the ranged mapping: PER for damage, AGI for accuracy.
  - Launchers (Missile Launcher, Torpedo Launcher, Assault Concussion Missile) use the melee mapping: MGT for damage, PER for accuracy.
  - Crits use PER against VIT for both, as on the ground.
  - Melee builds get a natural Gunnery style (Ordnance) and ranged builds get theirs (Broadside). Without this, a melee character can tank in space but barely kill.
- **Intuitive Piloting** (WIL in place of PER for attack) is removed and refunded, since WIL now has its own job.
- **FP** is not used in space.
- **Space ability costs:** STM or ship capacitor. Modules keep costing capacitor.
- **Regen:** STM regen keeps its ground pacing unless the Bible draft proves maneuvers need their own rate. If they do, add a space-specific stat; never change ground STM.

## 6. Perks and abilities

### Template

Each space skill has two styles of 60 SP, the same as a weapon skill.

- **SP schedule** (rank -> SP): 2->2, 5->2, 8->2, 10->2, 12->2, 15->4, 18->3, 20->4, 22->4, 25->4, 28->3, 30->4, 32->3, 35->2, 38->4, 40->5, 45->4, 50->6.
- **Row mix per style:** 3 ranked abilities, 2 general traits, 3 cross-skill traits, 1 stance and 1 capstone. Each style opens with a rank-2 ability and ends with a rank-50 capstone.
- **Shared rules:**
  - The ground button budget applies: 4-6 distinct active buttons per style.
  - One stance at a time.
  - The hard crowd-control rules apply.
  - Power by source runs: always-on passive < conditional passive < active/stance < capstone.

### Styles

| Skill | Style | Identity | Signature mechanic |
|---|---|---|---|
| Piloting | **Ace** | Evasion dogfighting | Maneuvers (evasive roll, burst, reverse) that turn dodges into accuracy and crit windows |
| Piloting | **Wing Commander** | Command | SOC-scaled auras and orders for nearby allied ships, a formation stance, bonuses for crew aboard |
| Gunnery | **Broadside** | Sustained energy fire; PER damage, like ranged weapons | An overcharge rhythm for lasers, ion and beams |
| Gunnery | **Ordnance** | Missiles and torpedoes; MGT damage, like melee weapons | Volleys and lock-on; burst against big hulls |
| Ship Systems | **Bulwark** | Tank | A threat stance, hardening, emergency shields |
| Ship Systems | **Operator** | Support and control | Remote repair, capacitor transfer, jamming, target painting |

- **Gathering:** add space mining, gas harvesting and salvage perks inside its existing tree. Starship Mining I-II are refunded.
- **Draft rows:** Appendix A lists every perk for all six styles and the Gathering space perks.

### Implementation rules

- **Abilities:** space abilities use the shared ability system with `.CanBeUsedInSpace()` (`Service/AbilityService/AbilityBuilder.cs:105`). No ground ability gets that flag.
- **Source:** abilities act from the ship. Range, line of sight, threat and damage source are all the ship's.
- **Perk effects:** perks grant `StatType` values that the shared space code reads. Add space `StatType`s as needed (`Service/StatService/StatType.cs`) following the StatType rules in `CodingStandards.md`. There is no `Perk.GetPerkLevel` check in space code; today's Energy Management check at `Space.cs` ~1310 goes away.
- **Bible rows:** the implementing agent moves Appendix A into new **Gunnery** and **Ship Systems** tabs, a rewritten **Piloting** tab and the **Gathering** tab, using the existing perk-table columns. Values are run through the combat simulator, and the owner approves before code.
- **IDs:** reserve feat, spell, `spells.2da` and TLK IDs following `AbilityTargeting.md`.

## 7. Hulls

### Roles

| Size | Piloting (Standard / Advanced) | Roles and built-in bonus |
|---|---|---|
| Small | 0 / 10 | **Fighter**: weapon damage, most High slots. **Interceptor**: fastest; stronger, longer-range tackle. **Bomber**: torpedo damage. **Explorer**: scan strength and cloaking. |
| Medium | 10 / 20 | **Gunship**: threat and resistances. **Logistics**: remote repair amount and range. **Mining Barge**: mining yield. **Light Freighter**: big interior, cargo, passengers. **Blockade Runner**: speed, cloaking with cargo aboard, lower customs scan chance. |
| Large | 25 / 35 | **Frigate**: heavy combat. **Industrial**: strip mining and salvage. **Heavy Freighter**: largest non-capital interior and hold. |
| Capital | 40 / 50 | **Cruiser**: line combat, capital weapons. **Command Ship**: command aura range and strength. |

### Ships: role, manufacturer and signature bonus

There are **100 player ships**, all listed in Appendix C. Each ship's stats come from one Bible formula with three layers:

1. **Role baseline:** slots, hull, shield, capacitor, speed, evasion, signature, cargo, interior, repair multiplier, crew stations and the role's built-in bonus.
2. **Manufacturer theme:** a modifier.
3. **Signature bonus:** one of its own.

Two ships in the same role are sidegrades, never strict upgrades.

| Manufacturer | Theme | Ships |
|---|---|---|
| Republic | Shields, repairs, support | 20 |
| Corellian | Speed, evasion, flexible fittings | 20 |
| Sith | Damage and critical hits | 17 |
| Czerka | Industry: mining, salvage, cargo, drills | 11 |
| Mandalorian | Hull toughness, explosive weapons | 10 |
| Hutt | Heavy hulls, big holds, contraband | 10 |
| Chiss | Sensors, accuracy, range, scanning | 8 |
| Rift Raider | Pirate refits of stolen hulls, found only in Wild Space (Advanced only) | 4 |

**Standard and Advanced:** 74 Standard ships and 26 Advanced ships. Compared with a Standard ship, an Advanced ship has:
- **Better stats:** higher base stats, plus either one extra Mid or Low slot or a stronger signature bonus, never both. No Advanced ship gets an extra High slot, because an extra weapon outweighs every other bonus.
- **A higher Piloting gate:** 10 ranks above its size's gate (Small 10, Medium 20, Large 35, Capital 50).
- **A recipe unlock (Ship Schematic)** from drops or faction stores.
- **A higher recipe level.**
- **One rare component** (Appendix D).
- **A Shipyard requirement** when it's Large or Capital. Every Capital ship, Standard or Advanced, is built at a station Shipyard (section 25).

**Every ship defines:**
- High/Mid/Low slot counts.
- Base hull, shield and capacitor.
- Speed and evasion.
- Signature (size).
- Cargo hold.
- Interior layout (structure and storage limits).
- Repair-cost multiplier.
- Crew stations.
- A model scale. Models vary in size, so each ship sets its scale through `SetObjectVisualTransform`, checked with client evidence.

**Implementation rule:** built-in, manufacturer and signature bonuses replace the configuration modules. They grant stats; there are no ship-name switches.

- **Speed:** add per-hull movement through `Stat.ApplyCreatureMovementRate` (today every ship is 1.0).
- **Shield regen:** make it per second, increasing with size and WIL. Today larger hulls regenerate slower.

### Size rules

- **Size penalties:**
  - A weapon larger than its target loses hit chance for each size step.
  - A weapon smaller than its target loses damage for each size step.
  - Bomber torpedoes ignore the size penalty.
- **Access:**
  - Some zones admit only Small and Medium hulls (section 12).
  - Docking limits by size:
    - Capital hulls dock only at Veles, Mon Cala, Anchorhead, Smuggler's Moon Station and Korriban.
    - Every NPC dock takes Large and smaller.
    - Any player-city starport takes Large hulls. Capital hulls need starport upgrade level 2, which a city with 10 citizens can reach. This gives the starport upgrade a purpose beyond its repair discount without locking capitals out of smaller cities.
    - Ships already docked somewhere too small stay there until they leave.
- **Cost:** the repair bill scales with size.

### Hull Designs (ship skins) and Refit

- **What a design is:** a **Hull Design** is a skin for one specific ship, usable only on that ship.
- **Launch designs:** every ship has its base model. Ships whose model family has variants launch with those variants as designs (Appendix C, "Launch skins").
- **More designs later:** the Trellis2 pipeline generates more designs per ship over time (section 20).
- **Unlocking:** a **Design Schematic: <Ship> - <Design>** unlocks one design for the character. Sources: aces, bosses, Wild Space, rare salvage and faction stores.
- **Refit:** a starport Refit swaps a ship to another design unlocked for it, for a fee.
- **Ship Schematics** are the recipe unlocks for Advanced ships. They replace the orphaned `recipe_aurek`, `recipe_sfight` and `recipe_basilisk`.
- **Migration:** owners keep today's look as a design of the ship they're mapped to (section 15).

### Ship names

- **In space, other players see the ship's name instead of the pilot's.** It is the ship's property name, set or changed at a starport, with the same validation as other renamed properties.
- **Examine** shows the pilot under the normal recognition rules. Route both through `PlayerName` and `PlayerDescriptor` (the system behind `/name`), following `PlayerIdentity.md`, so known-name labels and disguises keep working.
- **Leaving the ship** restores the pilot's normal name.

## 8. Modules

### Slots

- **Rows:**
  - High: hotbar feats 1-10.
  - Low: feats 11-20.
  - Mid (new): feats 21-30 (`ShipModuleFeat.cs` already defines `ShipModule21-30` and the `ife_sm21-30` icons).
- **Data model:** add `MidPowerModules` to `ShipStatus`.
- **Activity:** active modules can sit in any row.
- **Removed:** the configuration slot.
- **Fitting limits:** slot counts plus capital-only weapons; capacitor is the running cost.
- **Slot tags:** each slot carries a tag, **Helm** or a station (Gunner, Engineer, Sensors), for crew (section 10).

### Activation: toggles and auto-fire

Toggled modules are the ship's auto-attacks; perk abilities are the buttons, as on the ground.

- **The rule:** toggles do steady work every cycle. One-press modules deliver a single timed burst, with a cycle of 18 s or longer or a scarce consumable spent per use. Each family's setting is written explicitly in Appendix B ("Use"), so a later cycle-time retune never moves a family to the other category by accident.
- **Toggles:** an on/off toggle fires itself each time its cycle ends. That covers:
  - weapons other than the burst ones below (missiles while ammo lasts)
  - Mining Lasers, Salvagers and Gas Harvesters
  - repairers and capacitor transfer
  - Jammers, Target Painters, Ion Webs and Interdictors
  - Shield Hardeners and the Afterburner, which pay capacitor each cycle
- **One press:** the Torpedo Launcher, the Assault Concussion Missile, the Laser Battery, the Hypermatter Injector, the Data Analyzer, and every perk ability.
- **Targets:**
  - Offensive modules fire at the ship's current target and follow it when the pilot switches targets.
  - Support modules (repairers, capacitor transfer) lock onto the target selected when they were turned on. They keep it until it dies or they are turned off.
- **Auto-off:** a toggled module turns itself off, with one short message, when:
  - its target dies, becomes invalid or leaves range
  - capacitor can't pay its next cycle
  - its ammo runs out
  - its asteroid, gas cloud or wreck is used up
  - the ship leaves space, jumps a starlane or is destroyed
- **No global recast for toggles:** each runs on its own cycle. One-press items and perk abilities follow the ground ability rules.
- **Balance:** every fitted module now fires every cycle. Sustained damage on hulls with many slots roughly doubles compared to today, where the 2 s global recast limited how many modules a pilot could cycle. Rebalance damage, capacitor cost and cycle time for each family in the Bible draft.
- **State:** toggle state lives in `ShipStatus.ActiveModules`, which exists but is unused today. One auto-fire loop drives players and NPCs (folding in `ProcessSpaceNPCAI` firing). It runs at sub-second resolution so cycles aren't held back by the 1 s ship tick.
- **Lit icons (required):** a module that is on shows a lit variant of its icon: the same art with an "active" border, generated with the icon tools (`IconStandards.md`). It is swapped through the slot texture override that `Space.ApplyShipModuleFeat` already uses, and the cooldown sweep runs each cycle. Every module family and rank needs both variants, and client evidence must show on, off and auto-off states.
- **Ship HUD:** a NUI window built to `NuiLayoutRules.md`, `GuiWindowAuthoring.md` and `skills/swlor-gui-window-generation/SKILL.md`. It shows:
  - the current target with its hull and shield
  - every module that is on, with its target and cycle progress
  - a **Retarget** button on each support module, which locks it to the current target
  - an **All Weapons** on/off button, also available as a hotbar feat

  The HUD exists so support-module locks are never confusing: a player can always see what each module is aimed at and change it in one click.

### Families

*New* marks a new family. Existing families keep their identity and move rows as listed.

| Row | Owner | Families |
|---|---|---|
| High | Gunnery | Laser Cannon, Ion Cannon, Beam Cannon, Missile Launcher, Torpedo Launcher (today's proton bomb, expanded), *Point-Defense Turret* (always fires as Light), capital weapons (Turbolaser, Quad Laser, Laser Battery, Storm Cannon, Assault Concussion Missile) |
| High | Gathering | Mining Laser, Strip Miner, *Salvager*, *Gas Harvester* |
| Mid | Ship Systems | Shield Repairer, *Shield Hardener*, Hypermatter Injector, *Ion Web*, *Interdictor* (target can't jump, dock or leave space for a while), *Jammer*, *Target Painter*, capital Bulwark, Repair Field and E-War arrays |
| Mid | Piloting | Shield Booster, Capacitor Battery, *Afterburner*, *Cloaking Device* (Small hulls and Blockade Runners; section 32) |
| Mid | Ship Systems | *Probe Launcher*, *Data Analyzer* (section 24) |
| Mid | Gunnery | Targeting Computer |
| Low | Ship Systems | Hull Repairer, *Capacitor Regulator*, *Smuggling Compartment* |
| Low | Gunnery | Damage Amplifier (per type) |
| Low | Piloting | Hull Plating, Resist Plating (one type or all types), Thrusters, *Cargo Expander* |
| Low | Ship Systems | The six capital passives |

- **Capital modules:** capital weapons and arrays fit only Capital hulls. Hull and shield repairers and the injector now work on capital ships too.
- **NPC-only modules:** variants (`npc_*`) convert to the new families with NPC-level scaling.

### Ranks and size

- **Rank steps:** every family has a distinct item every 5 ranks: 0, 5, 10 ... 45, with in-between stats. Rank N requires rank N in the owning skill.
- **Station slots:** an owner can fit a station-slot module without meeting its rank. The crew member at that station can switch it on only if their own rank in its skill meets the requirement; a droid uses its tier (section 10).
- **Rank 50:** reserved for named boss modules (section 14).
- **Capital families:** their ladders start at rank 40.
- **Size scaling:** a module's output scales with the size of the hull it's fitted to. A Laser Cannon fires as Light on a Small hull and as Heavy on a Large hull.
- **Exceptions to size scaling:**
  - Point-Defense always fires as Light, so big ships can carry anti-fighter guns.
  - Capital weapons fire as Capital.
  - Torpedoes ignore the size penalty.
- **Item count:** expect about 20 families x 10 steps, roughly 200 items with recipes, icons (`IconStandards.md`) and descriptions.

### Enhancements

- **Starship enhancements** are rebased to the new stats: one Attack, one Defense, resistances, hull, shield, capacitor, regen, accuracy, evasion and cargo.
- **Module enhancements** (`ModuleBonus`) stay.
- **Fix:** subtype 58, which is labelled "Starship - Agility" but builds an ability bonus.

## 9. Combat rules

- **Hit and damage:**
  - Hit chance uses `Combat.CalculateHitRate`, as today.
  - Damage uses `Combat.CalculateDamage` with one ship Attack and one ship Defense, replacing today's three per-type pairs. The damage attribute is PER for energy weapons and MGT for launchers (section 5).
- **Accuracy and evasion:** accuracy is the weapon's accuracy attribute x 3 (AGI for energy weapons, PER for launchers; section 5) + Gunnery rank. Evasion is AGI x 3 + Piloting rank. Both add hull and modules.
- **Crits:** on, following the ground formula. Gunnery rank stands in for weapon rank, with PER against VIT.
- **Resistances:** add `ResistanceType` Thermal, Explosive and EM, each from -100 to 100. `CombatDamageType` already has Thermal 20, Explosive 21 and EM 22.
- **Status effects:** go through `StatusEffect` with its resistance families.
  - Ion slow uses the Mobility family.
  - Hard crowd control follows the ground rules: cooldown at least 1.5x duration, 30 s maximum, 20 s immunity, no refresh, target caps.
- **Threat:** the `Enmity` service drives NPC targeting, and tank stances use the ground threat pattern.
- **PvP:**
  - A player's ship may attack another player's ship only when the ground rules allow it: like/dislike hostility through `PlayerList` plus the area's PvP setting.
  - The `PvpCombat` ramps and halved hard crowd control apply.
  - Remote repair and buffs follow the same hostility rules.
- **Death:**
  - **Escape pods:** a destroyed ship ejects everyone aboard (pilot, crew and passengers) alive to the dock the ship is towed to. Each takes the normal ground XP debt (`Death.ApplyPenalties`).
  - **Modules:** they drop only on a PvP death **in Wild Space** (section 23), at today's 65% chance per module, and the attacker can loot them. Everywhere else, including every existing orbit, modules stay installed on any death; players are used to those orbits being safe.
  - **Cargo:** spills on any death (section 13).
  - **Repairs:** the owner pays a repair bill scaled by hull size. The SOC and starport discounts stay.
- **Global recast:** none for toggled modules (section 8, Activation). One-press items and perk abilities follow the ground ability rules, and each crew member has their own.

## 10. Crew stations

Ships at launch.

- **Stations:**
  - Large hulls: Pilot, Gunner and Engineer.
  - Capital hulls: Pilot, 2 Gunners, Engineer and Sensors.
  - Small and Medium hulls are solo.
- **Slot control:**
  - Gunners toggle the High slots tagged as their turrets and pick their own target, independent of the pilot.
  - The Engineer runs active shield and hull modules, plus a new **power-routing** ability that shifts capacitor regen between weapons, shields and engines.
  - Sensors runs Mid electronic warfare and tackle.
  - The Pilot runs Helm slots, Afterburner, Thrusters and maneuvers. The pilot cannot fire station slots.
- **Gear rank on stations:** the owner fits station-slot modules without rank checks. Each crew member can switch on only the modules whose rank requirement they meet themselves; droid crew go by their tier. A pilot-only captain can still outfit their gunners.
- **Boarding a station:**
  - A crew member uses a console placeable in the interior. Their character moves into the space area and is hidden through `VisibilityPlugin`. It is ghosted (passes through things), can't be targeted, and is plot.
  - The camera attaches to the ship with `AttachCamera`, and a copy of them sits at the console.
  - Their hotbar swaps to the station's slots plus their own space abilities.
- **While crewing:**
  - The server keeps the hidden character beside the ship every second and re-attaches the camera after every area transition. The camera drops on transitions.
  - Everything a station does is fired from the ship. The crew member's own rank, attributes and perks set the strength.
- **Droid crew:**
  - A player-built droid (Engineering) can work any station at reduced effect, scaled by its Industry rating (section 27). It turns that station's modules on whenever the pilot has a valid target, and back on after an auto-off once a new valid target is picked.
  - Its tier sets which station modules it can switch on, the way a player's rank does. Its Industry rating sets how well it works them. The best industrial droid works a station at about 75% of a player of matching rank, and a tier-1 combat droid far below that, so players stay the best crew and a top-tier droid pays off.
  - **Droid limit:** a droid assigned to a ship's station, a droid bay, a holding or a station building stays at that post and doesn't count toward the one-droid limit. That limit covers only the droid that follows its owner.
  - Its logic reuses `ProcessSpaceNPCAI`.
- **Permissions and edge cases:**
  - Add a crew property permission next to `PilotShip`.
  - Handle disconnects at a station, crew leaving mid-fight, and ship death (escape pods).
- **Balance:** a ship crewed by N players must not beat N separate ships. The combat simulator checks this.
- **Wing Commander:** its crew bonuses apply to everyone aboard.

## 11. Enemies

- **Conversion:** every NPC hull gets a size and a role.
  - Size mapping: Fighter, Interceptor and Bomber -> Small; Gunship and Cargo -> Medium; Platform -> Large; capitals -> Capital.
  - Loadouts come from the new families.
- **Unchanged:** the faction ladder (T1 Septra Khanate, T2 Krakenborne, T3 Snowdrift Syndicate, T4 Clan Zel Arun, T5 Red Hand, T6 Imperial) and NPC levels.
- **Role behaviors** are shared AI in `ProcessSpaceNPCAI`:
  - Tank: holds threat and hardens.
  - Logistics: repairs allies.
  - Electronic warfare: jams or paints the top-threat player.
  - Interceptor: webs and interdicts ships that flee.
  - Bomber: hunts the biggest hull.
  - Sniper: kites at max range.
  - Carrier: launches fighter waves.
  - Minelayer.
- **Every faction tier** gets these roles.
- **New groups:**
  - **Pirate raiders**, per tier, who ambush traders (section 13).
  - **Customs patrols:** Republic, Czerka and Sith, in lawful orbits.
  - **Hutt enforcers** on the Smuggler's Moon lanes.
  - **Derelict defenses** in the debris field: droids, turret platforms (unused turret rows 2153/2154) and mines (Mine 10110).
  - **NPC trade convoys** players can raid for cargo. Raiding costs lawful standing.
- **Aces:** one named rare-spawn pilot per orbit, built with `skills/swlor-rare-spawn-generation/SKILL.md`. Each has a unique loot table of Design Schematics, named-module recipes, trophies and components.
- **Encounter types:** patrols, ambushes, convoys, mining-site defense and capital battle events.
- **Rare Sith and Mandalorian capitals** (Harrower, Kandosii and the others) spawn only in T5-T6 orbits. Their armadas also drive weekly incursions (section 30).
- **Rift Raiders:** the Wild Space pirate coalition (section 23).
- **Stats:** draft a space section in the Bible's Enemy Builder Guide, Enemy Stat Presets and related tabs for owner approval. Follow `NpcHitPoints.md` for HP limits.
- **Models:** use unused hak models. Space fauna goes on the Trellis2 list.

## 12. Activities, zones, and materials

- **Mining:**
  - Moves to Gathering: XP, perks and module gating.
  - Fix the mining lock, the strip miner tier check and the yield math (section 16).
  - Mined ore still goes to the pilot's inventory.
- **Gas harvesting:** a Gas Harvester cycles on gas-cloud placeables in the nebula.
- **Salvage:**
  - A destroyed NPC ship leaves a wreck placeable for a few minutes.
  - A Salvager cycles on it the way a Mining Laser cycles on an asteroid. Capital wrecks need a Large or Capital salvager.
  - The killing party has the wreck to itself for 60 s, then anyone can salvage it.
  - **Yields:**
    - **Ship Scrap,** which the Refinery turns into electronics of the wreck's tier. This is the first reliable electronics source.
    - **Salvaged Components,** each carrying a random trait (section 14).
    - **Rare finds** from elite and boss wrecks.
- **Zones:**
  - The 3 Abandoned Station areas (`zomb_abanstation`, `zomb_abanstatio2`, `zomb_abanstatio3`) become Small/Medium-only.
  - Build 3 new Small/Medium-only zones:
    - **Viscara Asteroid Belt** (T1-2): Durelium asteroids.
    - **Mid Rim Nebula** (T3-4): Tibanna Gas clouds.
    - **Korriban Debris Field** (T5): Aurodium salvage and derelict defenses.
  - Each pays better than open orbit.
- **New materials:**
  - **Durelium** (asteroid ore, feeds Hull Frames).
  - **Tibanna Gas** (feeds Emitters and Reactors).
  - **Aurodium** (salvaged ancient alloy; feeds the named rank-50 modules alongside Thorilide).

  Each needs item blueprints, refinery entries where needed, loot tables, icons and `EconomyRestrictedItems.md` coverage.
- **Starlanes:**
  - Add Dantooine-Korriban.
  - Give Smuggler's Moon (`narshadorbit`, unreachable through space today) dangerous lanes to Hutlar and Tatooine.
  - Make the Korriban route return the way it came.

## 13. Trade

- **Cargo:**
  - Cargo is data on `PlayerShip`, never items. It can't enter an inventory, the bank, the market or contracts, all of which are galaxy-wide today.
  - Each hull has a cargo hold in units. Freighters are largest and Small hulls hold almost nothing.
  - A Cargo Expander (Low, Piloting) trades a slot for capacity.
- **Commodities:** about 20, separate from crafting materials, plus the refined goods from holding production chains (section 26), including Station Fuel.

| Dock | Exports (cheap) | Imports (pays well) |
|---|---|---|
| Viscara | Veles honey, Viscaran crystals, kinrath silk | Purified water, medical supplies |
| CZ-220 | Reactor cores, Czerka machine parts | Grain, timber |
| Mon Cala | Seafood, purified water, hull sections | Silk, timber, neon salt |
| Hutlar | Aracia timber, byysk hides | Medical supplies, reactor cores |
| Tatooine | Jawa salvage, bantha hides | Water, grain, seafood |
| Smuggler's Moon | Moonspice (contraband), neon salt, encrypted data chips (contraband) | Sith relics (contraband), honey, weapons crates (contraband) |
| Korriban | Sith relics (contraband), abdamaryllia extract | Water, data chips (contraband) |
| Dathomir | Witch herbs, beast hides | Machine parts, medical supplies |
| Dantooine | Grain, bantha milk | Hides, timber |

- **No pair within one orbit:** no dock exports a good that a dock in the same orbit imports. Viscara and CZ-220 share an orbit, and so do Smuggler's Moon Landing and Station. Commodity drafts are checked against this rule.
- **Exchanges:**
  - A Trade Exchange NUI window sits at every NPC dock and every player-city starport. It trades with the player's ship docked there.
  - **Prices move:** selling floods a dock and lowers its price, buying raises it, and both recover over a few hours. Price state persists in a new Redis entity.
  - Longer and more dangerous routes pay more.
  - A **HoloNet price board** shows other docks' prices an hour stale.
- **SOC** improves buy and sell prices slightly. There is no trade skill.
- **Contraband and customs:**
  - Lawful docks: Republic (Viscara, Mon Cala, Dantooine), Czerka (CZ-220), Sith (Korriban). Each has its own contraband list. For example, the Republic bans spice, Sith relics and weapons crates.
  - Customs patrols in lawful orbits scan passing ships. If they find contraband, the player pays a fine and loses it, or runs: the patrol turns hostile and lawful standing drops.
  - Black markets at Smuggler's Moon, Tatooine and Hutlar buy contraband at a premium.
  - A Smuggling Compartment (Low, Ship Systems) cuts the scan chance.
- **Risk:**
  - A destroyed ship spills its cargo as containers anyone can scoop for 5 minutes, in PvE and PvP.
  - Cargo above a value threshold can trigger a pirate ambush when the ship enters an orbit or jumps. The chance and squad size scale with cargo value and orbit tier, and every squad includes an interceptor.
- **Freight contracts:**
  - NPC contracts at each exchange: haul a sealed load from A to B by a deadline for a fixed fee and standing.
  - A deposit is forfeited if the load is lost.
- **Factions:** trade switches on the faction service. Czerka (industrial), Republic Military (lawful supply), Hutt Cartel and Smugglers (contraband), Sith Military (Korriban).
  - Standing unlocks better prices and higher-paying contracts.
  - Getting caught smuggling, or raiding convoys, costs lawful standing.
- **City hubs:** a city starport exchange charges a mayor-set tariff paid into the treasury.
- **Earning targets:** measure credits per hour of space combat at each tier first (loot gold plus sold drops), then set:
  - Freight contracts: about 75% of that rate.
  - Open trading: about 100-125%.
  - Contraband: about 150-200%.
  - Price flooding, fees, deposits and cargo loss hold trade to those targets. Exchanges create credits, so this is the inflation guard.

## 14. Crafting, rewards, and the economy

### Component layer

Crafted intermediates that hull and module recipes consume. Appendix D lists every component, which skill crafts it, and which loot tables drop it.

- **Generic families:** Hull Frame, Ship Reactor, Energy Emitter, Ordnance Assembly, Shield Projector, Thruster Assembly, Sensor Suite, Extraction Head, Life Support System.
- **Manufacturer components:** one per manufacturer (for example Republic Shield Matrix, Sith Targeting Core). They are not crafted. They come from that manufacturer's faction store, its NPC ships' loot, and salvage of those wrecks.
- **Rare components** for Advanced ships: Thorilide Core, Aurodium Plating, Prototype Core, and salvaged parts carrying a rare trait.
- **Tiers:**
  - Five material tiers. Items at in-between ranks use their tier's component.
  - Today's capital parts (`capc_*`) become the top band.
  - Name the reactor "Ship Reactor"; `power_core` is already the refinery fuel item.
- **Inputs:** refined ores, metals, electronics, fiberplast, the new materials and salvage.
- **Variants:** each family has 2-3 craftable variants. Each adds a trade-off trait to the finished item, and nothing becomes a strict upgrade. Examples:
  - Energy Emitter: Focused (+accuracy), Overcharged (+damage, +capacitor cost), Efficient (-capacitor cost).
  - Hull Frame: Reinforced (+hull, -speed), Lightweight (+evasion, -hull).
  - Shield Projector: Dense (+max shield), Rapid (+regen).
  - Extraction Head: Bulk (+yield), Precision (+rare-find chance).
- **Salvaged Components:** carry a random trait, sometimes a rare one crafting can't make (for example "Sith-tuned").
- **Recipe levels:** they match the rank schedule. The rule "required rank = recipe level - 3" stays.
### Who crafts space items

There is no new crafting skill. Space work spreads across the existing crafts by theme, so a hull needs parts from several crafters and they trade with each other. Self-sufficiency costs more on purpose: no single craft builds a whole ship, the same way no ground fighter crafts their own weapon, armor and stims.

| Skill | Space items |
|---|---|
| Smithery | Weapon modules (every Gunnery High-slot weapon, capital weapons included), Hull Plating, Resist Plating, ammo (missiles, torpedoes, concussion missiles), and the metal components: Hull Frame, Ordnance Assembly, Thruster Assembly, Aurodium Plating |
| Engineering | Every other module (shields, repairers, capacitor, electronic warfare, tackle, sensors, Probe Launcher, Data Analyzer, Cloaking Device, Afterburner, Thrusters, Damage Amplifier, Targeting Computer, mining and salvage gear), Hypermatter capsules, enhancements, droids and industrial droid parts, and the electronic components: Ship Reactor, Energy Emitter, Shield Projector, Sensor Suite, Extraction Head, Thorilide Core, Prototype Core |
| Fabrication | Final assembly of all 100 ships, the Cargo Expander and Smuggling Compartment, Station Cores and station buildings (Shipyard, Asteroid Drill, Jump Beacon), holding upgrades and Processing Plants, and interior trophies and decor |
| Agriculture | Life Support System (hydroponics), a component of Light and Heavy Freighters, Cruisers and Command Ships |
| Gathering | Refining: ores, the new materials, and Ship Scrap into electronics (no recipes) |

- **Hull recipes:**
  - Each ship is one Fabrication recipe needing a Hull Frame, a Ship Reactor, a Thruster Assembly, its role's component and its manufacturer's component.
  - Advanced ships add one rare component.
  - Large and Capital Advanced ships, and every Capital ship, are assembled at a station Shipyard.
- **Recipe levels (draft):**
  - Standard: the ship's Piloting gate + 5.
  - Advanced: its Advanced gate + 5.
  - Capital Advanced recipes are level 52, the tier-5.5 recipe level, as on the ground.
- **Moving recipes:** today's ship, module and ammo recipes leave Engineering for the skills above. Recipe unlocks a player already has are remapped, never dropped (`CombatUpgradeMigration.md` rule). The free Rebuild Token covers anyone who wants to move ranks.
- **Hull quality:** starship enhancements remain the way to improve a hull.

### Research

- Extend `IsResearchableRecipe` and the Research Terminal so starship and module recipes can be researched into blueprints: bonus stats, extra enhancement slots, cheaper runs. Prototype tech from Wild Space (section 23) is an optional research input that raises blueprint bonuses.

### Rewards

- **Named rank-50 modules,** one per main family:
  - They follow the ground Chiro pattern: level-52 recipe unlocks, 2 enhancement slots.
  - Recipe unlocks drop from T6 bosses and aces. The modules need Thorilide and Aurodium.
- **Design Schematics** for Hull Designs drop from aces, bosses and rare salvage, and faction stores sell some. **Ship Schematics** for Advanced ships drop from the same sources (Appendix D).
- **Interior trophies and decor:** new `StructureType` entries such as a mounted wreck trophy or a captain's holotable.
- **Ammo variants** for each launcher at each tier:
  - Ion missiles (EM plus slow).
  - Incendiary (thermal damage over time).
  - Seeker (high accuracy).
  - Rare torpedo types from salvage.
- **Achievements:**
  - Explore the Fighter, Corvette and Basilisk interiors (missing today).
  - First capital kill.
  - Trade milestones.
  - First crewed kill.

### Stores and economy

- The dockhand store sells rank-0 modules, starter Small hulls, and Mining Lasers through rank 40 (today's Mining Lasers I-IV, mapped as in section 15). Non-crafters can still mine ore and lyase. Weapons and other modules above rank 0 are crafted only.
- Every new item source must be registered for `EconomyObtainabilityCoverageTests` and follow `EconomyRestrictedItems.md`.
- Game text uses ASCII punctuation (`CodingStandards.md`).

## 15. Migration

One release, one migration set (`Migrations.md`): server migrations first, then character migrations, then the saved-data item sweep.

- **Rebuild Token for every character**, regardless of skills or perks.
  - Use `CharacterMigration/RebuildTokenGrant.cs` in a new character migration.
  - There is no forced rebuild. Piloting ranks stay as they are; Gunnery and Ship Systems start at 0.
  - Skill rank limits ship first, in their own pull request (`SkillRankLimitsPlan.md`, zunath/SWLOR_NWN#2810). The login notice should mention them, so pilots can cap Piloting before spending the token.
- **Refund every Piloting perk** as SP: Starships, Defensive Modules, Offensive Modules, Mining Modules, Energy Management, Starship Mining, Intuitive Piloting.
  - Use `CharacterRecalculation.RefundPerk` with a login notice.
  - Remove their feats.
  - Clean `UnlockedPerks`.
- **Login notice:** must say plainly that a pilot keeps Piloting, which still covers hull sizes and the Piloting modules (passive defenses, propulsion, cargo, cloaking), but needs Gunnery or Ship Systems ranks to fit weapons or Ship Systems modules above rank 0. The free Rebuild Token is the way to move ranks.
- **Items** (inventories, storage, bank, market listings, deeds, installed modules on `PlayerShip`):
  - Old hulls map to new ships as below, and the owner gets the old model unlocked as a design of the new ship:

| Old hull | New ship |
|---|---|
| Light Freighter (`sdeed_freighter`), Striker | Striker |
| Light Escort (`sdeed_escort`), Condor | Condor |
| Hound | Republic Striker |
| Panther, Falchion | Sith Scout |
| Saber | Republic Scout |
| Mule, Consular | Republic Gunship |
| Merchant | Sith Gunship |
| Throne | Sith Infiltrator |
| Aurek, Sith Fighter, Cutlass, Basilisk | Same-named ship |
| Republic, Sith and Neutral Thranta; Hutt Corvette; Jehavey'ir; Chiss Trireme | Same-named Cruiser |
| YZ2500 Corellian Gunboat | Corellian Gunboat |
| CZC Armored Transport, Terminus Corsair, Crusader | Czerka Armored Transport, Terminus Corsair, Crusader Corvette (Command Ships) |

  - The two store starters map to Small ships so their owners can still fly them at Piloting 0.
  - Old module tiers map to the new rank steps: Basic -> 0, I -> 10, II -> 20, III -> 30, IV -> 40.
  - Configuration modules are uninstalled and refunded in credits at their value.
  - Installed modules that no longer fit return to their owner, never deleted.
  - Strip `UseLimitationPerk` and the broken `RequiresSkill(Invalid, 100)` stamps (section 16), and add `RequiresSkill`.
- **Where the item code goes:**
  - Live items: update `EquipmentRequirementMigration` / `EquipmentRequirementCompatibility`, replacing the `SpaceEquipmentLegacyRequirement` skip.
  - Saved data: the `StoredItemSweep` twin, including `PlayerShip` deeds and module dictionaries.
  - Add the legacy kinds to the `SavedDataItemSweep` engine test.
- **Ships:**
  - Rebuild `ShipStatus` for every `PlayerShip` (precedent: `RecalculateAllShipStats` in `git show 2fa527816:SWLOR.Game.Server/Feature/MigrationDefinition/ServerMigrationBase.cs`).
  - Add `MidPowerModules`, move modules to their new rows, and clear `PlayerHotBars`, since feats change.
- **Docked ships:** ships docked somewhere now too small for them stay until they leave.
- **Before migrating:** refund perks before removing their keys. The rebuild UI cannot price undefined perks.

## 16. Bug list

Fix these as part of the rewrite. Most go away with the code that replaces them.

1. Mining laser and strip miner clear the asteroid "being mined" flag right after scheduling, so the lock never holds.
2. Strip miner: no asteroid tier check, no ship-status null check after its delay, and a smaller yield than the laser despite its description.
3. Mining yield uses float division on `ModuleBonus`.
4. Capital configuration modules add and remove different `ModuleBonus` amounts. Configuration modules are removed entirely.
5. Configuration modules are ignored by death, `CanPlayerUseShip` and the unregister guard, and their descriptions disagree with the code. Removed with configs.
6. Storm Cannon is typed `IonCannon`; the `StormCannon` and `CapitalPowerDiverter` enum values are unused.
7. Turbolaser text says two seconds, but the code waits 3 s. It also has `CanTargetSelf`.
8. Quad Laser skips `CanShipFire` and the dead-target check between shots.
9. Larger hulls regenerate shields slower than small ones.
10. The kill XP range (40 m) is shorter than missile range (55 m).
11. Space weapons and damage have no PvP hostility check.
12. `EquipmentRequirementMigration` stamped examined Defensive, Offensive and Mining module items with `RequiresSkill(Invalid, 100)`, because only `PerkType.Starships` was skipped.
13. Starship enhancement subtype 58 is labelled "Starship - Agility" but builds an ability bonus.
14. `recipe_aurek`, `recipe_sfight` and `recipe_basilisk` cannot be obtained. Ship Schematics replace them (section 7).
15. Missile ammo: `Missile35` is recipe level 25, recipe type names don't match quantities, and the Bible rows are stale.
16. `ShipDetail` Accuracy, Evasion, defenses, IndustryBonus and HasDroidBay are never read. `ShipStatus.ActiveModules` is never read either; it becomes the toggle state (section 8).
17. NPC config modules are stored under negative Low-slot keys. Removed with configs.
18. The asteroid space object points at an item tag ("asteroid") that isn't a registered ship.
19. `PerkType.StarshipBlueprints` (152) is unreferenced.
20. `Readmes/Builders.md` ship builder examples use members that don't exist.
21. Capital ships can't use normal repairers or the injector (section 8 allows them).
22. Smuggler's Moon has no starlanes, and the Korriban route returns to Viscara instead of the way it came (section 12).

## 17. Numbers to draft for approval

Draft each in the Bible, check it with the combat simulator where it affects combat, and get owner approval before code.

- **Ships:** the ship formula; per-role baselines (slots, hull, shield, capacitor, speed, evasion, signature, cargo, interior limits, repair multiplier, built-in bonus); manufacturer modifiers; each ship's signature bonus size; Advanced multipliers; recipe levels; model scales.
- **Components:** recipes and quantities per ship; manufacturer component drop chances; rare component recipes.
- **Size rules:** hit-chance and damage penalty per size step.
- **Modules:** stats per family per rank step; capacitor cost, recast and range; Point-Defense and capital values; size scaling factors.
- **Components:** recipes, variant trait sizes, salvaged trait odds.
- **Perks:** every row for Ace, Wing Commander, Broadside, Ordnance, Bulwark and Operator, plus the Gathering space perks.
- **Combat:** STM costs, capacitor and shield regen formulas, crit factors.
- **Activation:** cycle time and capacitor per cycle for every family once the global recast is gone, and the auto-fire loop interval.
- **Crew:** droid crew effectiveness (the best industrial droid at about 75% of a player of matching rank, a tier-1 combat droid far below), power-routing amounts.
- **Enemies:** space NPC stat presets per role and tier, ambush odds, convoy and patrol sizes.
- **Trade:**
  - Commodity base prices, flooding and recovery rates, exchange fee.
  - Freight deposits and pay, contraband scan odds and fines, tariff cap.
  - XP per delivery, standing gains.
  - The measured combat baseline and the resulting targets.
- **Repairs:** cost per hull size.
- **Refit:** fee.
- **Dockhand store:** prices for its stock (section 14).
- **Wild Space:** environment effect sizes, rift lifetimes, mass budgets and collapse damage, Rift Raider presets, loot tables.
- **Scanning:** signal gained per cycle, spawn rates per signature type, Sensor Sweep range and cooldown, Jump to Signature spool.
- **Stations:** Station Core recipe, anchor site count per area, Asteroid Drill output, Shipyard build times, Jump Beacon fuel cost and hull caps.
- **Holdings:** price, upkeep, output, upgrade costs, Processing Plant recipes, event odds, droid boost.
- **Droids:** Industry per industrial part, stat cuts, instruction effects.
- **Activities:** agent mission pay and token prices, incursion schedule and rewards, Rift Run timers and loot, cloak speed penalty and reveal range.
- **IDs:** confirm the new skill IDs (53 and 54) were never used historically, since saved data keys skills by ID.

## 18. Workstreams and order

Everything merges to one integration branch and releases together.

- **Lore review first:** before W0, the DMs approve the Wild Space faction and system names, all 100 ship names and the faction agent setup. Liberty Fighter, Rheinland Fighter and Corsair come from another game's model labels and need Star Wars names in that review. Until then, every faction, system and ship name in this plan is a working name.
- **Branching:**
  - Create `feature/space-revamp` from up-to-date `origin/master`.
  - Each workstream slice goes on its own branch and pull request into it, with the Codex review loop (`PullRequests.md`).
  - **Bible edits happen on one branch only**, because separate branches that edit the `.xlsx` cannot be merged. Make them on the integration branch, one at a time.
  - Merge `origin/master` into the integration branch regularly.
  - The final pull request goes to `master`.

| # | Workstream | Contents | Needs |
|---|---|---|---|
| W0 | Bible drafts | Piloting, Gunnery and Ship Systems perk tabs; Starships tab rewrite (hulls, modules, components, traits); space enemy sections; trade tab (commodities, prices) | Owner approval gates W2-W9 numbers |
| W1 | Foundations | Skills, `CombatPointCategoryType.Starship`, space `StatType`s, resistance types, High/Mid/Low and slot tags in `ShipStatus`, removing configs, size x role data model, `RequiresSkill` on space items | - |
| W2 | Combat rules | Shared crit, resist, status and threat; attribute mapping; regen; toggle and auto-fire loop; Ship HUD; PvP; escape pods; repair bills | W1 |
| W3 | Ships and modules | 100 ships (Appendix C) with model scales, Hull Designs and Refit, about 20 module families x rank steps, size scaling, icons with lit variants, store, moving recipes to their new crafting skills | W1, W0 |
| W4 | Perks and abilities | Six styles, Gathering space perks, space abilities and IDs | W1, W2, W0 |
| W5 | Enemies | NPC conversion, role AI, new groups, aces, encounters, spawn tables | W2, W3 |
| W6 | World | Salvage and wrecks, gas harvesting, new materials, 3 zones (Module JSON), station flags, starlanes, docking limits | W3 |
| W7 | Trade | Cargo data, exchange NUI, pricing service, HoloNet board, contraband and customs, pirates, freight contracts, factions, city tariffs, trade XP | W3, W5, W6 |
| W8 | Crew | Stations, consoles, `AttachCamera` and hidden crew, droid crew, permissions, power routing, Wing Commander crew bonuses | W2, W3, W4 |
| W9 | Crafting and rewards | Component layer and traits, recipes, research blueprints, named modules, Design Schematics, trophies and decor, ammo variants, achievements | W3, W6 |
| W10 | Migration | Rebuild Token, perk refunds, item and ship conversion, stamp cleanup, design unlocks, notices | W1, W3, W9 |
| W12 | Wild Space and exploration | 6 Wild Space areas and environments, rifts, Rift Raiders, Probe Launcher, signatures, Jump to Signature, Sensor Sweep, data/cache/gas/combat sites, Scanner HUD tab | W2, W3, W5 |
| W13 | Stations | Station city layout, Station Core, anchor sites, deck areas, Shipyard, Asteroid Drill, Jump Beacons, Station Fuel | W3, W7 |
| W14 | Holdings and droids | Holdings terminal, types, upgrades, production chains, events, ground sites; industrial droid parts, Industry stat, AI instructions, droid bays | W7, W9 |
| W15 | Activities | Derelict boarding (including contested derelicts), faction agents and token stores, incursions, Rift Runs | W5, W7, W12 |
| W16 | Cloaking | Cloaking Device, reveal rules, stealth unblock in space | W2, W12 |
| W11 | Release | Balance pass, client evidence, docs (section 19), player release notes | All |

**Suggested order:** W0 and W1 first, then W2 and W3, then W4-W9 and W12-W16 in parallel slices, then W10, then W11.

## 19. Verification

- **Build and tests:**
  - Build once with `-p:RunPostBuildEvent=Never -v q -clp:ErrorsOnly`.
  - Run focused NUnit filters for each slice.
  - Add tests for: rank gating, size rules, slot rows, migration (refunds, token, item mapping, ship rebuild), trade pricing and flooding, cargo spill, customs, XP split and range, PvP gating, escape pods, every auto-off trigger, support-module target locks and Retarget, All Weapons.
- **Engine tests** (`EngineTesting.md`) for:
  - Entering space with Mid slots.
  - Crew boarding, camera attach and re-attach after a starlane jump.
  - Droid crew.
  - Wrecks and salvage.
  - The saved-data item sweep.
- **Combat simulator:** extend `SWLOR.CombatSim` with the ship model (the rules are shared). Report seeded results at entry, middle and endgame ranks for:
  - Damage pilot vs tank vs support.
  - Small vs Large vs Capital under the size rules.
  - A crewed capital vs the same pilots in separate ships.
  - The Advanced ships in each role: each lands within 10% of the others at that role's job.
  - A melee build (MGT, PER) on launchers vs a ranged build (PER, AGI) on energy weapons at equal ranks.
- **Client evidence** (`InEngineVisualVerification.md`) for:
  - The Mid hotbar row.
  - Lit and unlit module icons, and a module switching itself off.
  - The Ship HUD with a support module retargeted.
  - A signature scanned from 0% to 100%, then Jump to Signature.
  - A rift crossing into Wild Space, an environment effect, and a collapse ejection.
  - Founding a station at an anchor site, docking, and a Jump Beacon jump.
  - A holding's ground site, an event fight, and cargo pickup.
  - An industrial droid in a droid bay and at a crew station.
  - Boarding a derelict, a contested derelict's waves, a data site slicing game, an agent mission, a Rift Run timer, and cloak and reveal.
  - Every Hull Design at ship scale.
  - Crew camera view.
  - The exchange window.
  - The three new zones.
  - Wreck and container visuals.
  - Docking-limit messages.
- **Bible sync:** run `tools/UpdateCombatUpgradeAudit.ps1 -RefreshLocalBible` after every Bible edit. The Bible, C# descriptions, TLK and behavior must agree.
- **Docs after release:**
  - Move lasting rules into `ContentStandards.md` (hull, module and enemy standards; ship-access and docking rules) and `SkillPerkStandards.md` (space styles).
  - Fix `Builders.md`.
  - Delete this file.

## 20. Trellis2 request list

New models go through the Trellis2 pipeline (`Readmes/BlasterModelImport.md`) as a separate project. This plan ships without them.

- Space fauna: mynock swarms (latch on and drain capacitor) and a giant space slug (exogorth) in the Viscara Asteroid Belt.
- **Hull Designs for every ship:** several skins per ship, each usable only on that ship (section 7). Start with ships that have no launch skins.
- Trade containers and wreck variants, if the existing placeables look wrong at ship scale.
- Station exteriors for Small, Medium and Large stations, if scaling the existing `[mdrn]SpaceStation` placeable (placeables.2da row 1452) can't tell them apart.
- Rift Raider ships, if existing pirate models and the unused hak models (for example the Alien set, 10215-10217) don't give the coalition and its 4 refit hulls their own look.

## 21. Out of scope

- Any ground ability in space, or any space ability on the ground.
- Fuel for ordinary flight. Station Fuel is spent only by Jump Beacons (section 25).
- Capital jump drives. Jump Beacons (section 25) and rifts (section 23) are the only travel beyond starlanes.
- Tier 6 content and the reserved ores (Ochne, Croknor, Hemorgite, Bisteiss). Wild Space is tier 5.5.
- Station raids and the Defense Grid building (a later pass).
- Faction warfare, bounties on players, and racing.
- New 3D models (section 20).
- Player-posted hauling contracts. Items already teleport through the bank, so they add nothing.

## 22. Risks

- **Credits:** exchanges create credits. Measure the baseline before tuning, and watch the first weeks.
- **Crew engine edge cases:** camera loss on transitions, perception range, disconnects. These need engine tests and live client evidence.
- **Content volume:** about 200 module items, 12 hulls, 6 styles, 3 zones and new enemy groups. Slice pull requests by family so reviews stay readable.
- **Migration:** every `PlayerShip` record and every space item changes. Test the item sweep on a production-sized copy before release.
- **Balance of crewed ships against fleets,** and of Small hulls at endgame. The simulator checks both before release.
- **Expansion content volume:** 6 Wild Space systems, station deck areas, 9 holding sites, derelict templates and Rift Run pockets are all new areas. Build them from templates and reuse existing interiors wherever the plan allows.
- **More credit sources:** holdings, agents, incursions and Rift Runs all pay out. Measure them against the same combat baseline as trade (section 13) before release.

# Part II: Space Expansion

Everything in Part II ships in the same release as Part I. It builds on Part I's sizes, roles, toggles, salvage, trade, crew and droids.

## 23. Wild Space

- **Systems:** six persistent new areas that always exist, flagged as Wild Space. They are **tier 5.5**, matching the ground's tier 5.5: rank-50 gear and level-52 recipe unlocks. This is not tier 6, so the reserved ores stay unused.
- **Environments:** each system has one environmental effect, applied as `StatType` changes to every ship inside. Magnitudes are drafted in the Bible. Starting set:

| System type | Effect |
|---|---|
| Pulsar | Stronger shields, slower capacitor regen |
| Black Hole | More speed, less accuracy |
| Ion Storm | More EM damage, weaker shields |
| Magnetar | More damage, less evasion |
| Red Giant | Stronger overcharges and volleys, higher capacitor cost |
| Ship Graveyard | Richer salvage, weaker scanning |

- **Rifts:**
  - Hidden signatures (section 24) connect each Wild Space system to a random known orbit or to another Wild Space system. Each system keeps about 2 connections open, and new ones appear as old ones collapse.
  - Each rift has a lifetime of 4-24 hours and a size class (Small, Medium or Large) that caps the largest hull through it. Only Large rifts take Capital hulls.
  - Each rift has a mass budget, and every passage spends some of it by hull size.
  - On collapse, ships inside get a 2-minute warning. Then any ship still in a system with no open exit is thrown to a random known orbit with hull damage.
- **Rift Raiders:** a pirate coalition of cargo thieves and spice runners that preys on rift traffic, using all 8 role behaviors together. Its stats are drafted from the ground tier-5.5 presets. Their wrecks spill stolen cargo (section 13), some of it contraband, on top of normal salvage. Models come from existing pirate ships, unused hak ships, or Trellis2 (section 20).
- **Loot:** named rank-50 module recipe unlocks, Design Schematics, Thorilide, Aurodium, tier-5.5 Rift Keys, rare component traits, and **prototype tech** (stolen experimental parts). Prototype tech is a research input that improves blueprint bonuses (section 14).
- **Stations:** 1-2 anchor sites per system (section 25). Jump Beacons never connect into or out of Wild Space.
- **Higher stakes:** Wild Space is the only place a PvP death can drop modules (section 9).

## 24. Scanning and exploration

- **Probe Launcher** (Mid slot, Ship Systems, toggle): each cycle adds signal strength to every hidden signature in the area, system-wide.
- **Scan strength** grows with Ship Systems rank, a crew member at the Sensors station, a droid running Survey Protocol (section 27) and the system's environment.
- **Signature types:** rift, combat site, data site, cache site, gas site, derelict. They spawn at random points and stay hidden: **no object exists in the area until the signature reaches 100%**, so flying over the spot finds nothing.
- **Information grows with strength,** shown on a new Scanner tab of the Ship HUD:
  - 25%: type
  - 50%: tier
  - 75%: threat level and loot class
  - 100%: the site opens for the scanning party
- **Jump to Signature:** at 100%, anyone in the scanning party can micro-jump to the site after a 5-second spool. Damage and Interdictors interrupt the spool. Players outside the party must scan it themselves.
- **Sensor Sweep:** a pulse mode on the Probe Launcher with its own cooldown. It lists every ship within range, cloaked ships included.
- **Sites:**
  - **Combat sites:** waves and a boss at the system's tier.
  - **Data sites:** a **Data Analyzer** (Mid slot, Ship Systems) runs the existing slicing mini-game (`Service/SlicingService/`) on data cans. They yield blueprints, recipe unlocks and agent intel, which unlocks special agent missions.
  - **Cache sites:** hidden pirate caches. The Salvager (Gathering) works cache containers for prototype tech and components.
  - **Gas sites:** the Gas Harvester.
  - **Derelicts:** section 28.
- **Despawning:** sites despawn once completed or after a timer.

## 25. Player stations

A station is a **player city in space** and follows every city mechanic, in `Service/Property.cs` and the city layouts:
- a mayor, who needs City Management
- citizens, with one citizenship per player across stations and planet cities
- elections every three weeks
- taxes, weekly upkeep and loss after four weeks unpaid
- city level by citizen count
- upgrades

**Founding**
- A **Station Core**, crafted with Engineering, takes the place of the City Hall item. It is used at an **anchor site**: 2 per orbit and 1-2 per Wild Space system, one station per site.
- Founding creates two things:
  - the station exterior at the site, using the existing `[mdrn]SpaceStation` placeable scaled by city level
  - the station's own interior deck area, which is the city's land. The City Hall sits inside it.
- Add a station city layout beside `CityLayoutDefinition`, with the same building and structure limits.

**Docking and buildings**
- **Docking:** through the Starport building, as in planet cities: any starport takes Large hulls, and Capital hulls need starport upgrade level 2.
- **City buildings:** all of them: City Hall, Starport (with the Trade Exchange and its tariff), Medical Center (respawn), Bank, Cantina.
- **Station-only buildings:**
  - **Shipyard:** Capital hull recipes can only be crafted at a Shipyard. A droid assigned to it shortens builds.
  - **Asteroid Drill:** extracts the anchor site's ore over time into station storage, at the system's tier. An assigned droid raises output.
  - **Jump Beacon:** jumps a docked ship to any other beacon whose city permits it.
    - Each jump costs **Station Fuel**, a trade good.
    - Beacon level caps hull size.
    - Beacons never connect into or out of Wild Space.

**Not in this release**
- Upkeep is city upkeep in credits; there is no other upkeep.
- Stations cannot be attacked. Raids and the Defense Grid come in a later pass.

## 26. Planetary Holdings

- **Buying:** at a planet's dock, from a Holdings terminal. Up to 3 per character, with credit upkeep.
- **Types:**

| Planet | Holding types (outputs) |
|---|---|
| Viscara | Crystal Mine (Viscaran crystals), Apiary (Veles honey) |
| CZ-220 | Assembly Line (Czerka machine parts, reactor cores) |
| Mon Cala | Kelp Fishery (seafood), Desalination Plant (purified water) |
| Hutlar | Timber Camp (Aracia timber), Byysk Ranch (hides) |
| Tatooine | Moisture Farm (water), Scrap Yard (Jawa salvage) |
| Smuggler's Moon | Spice Lab (moonspice, contraband), Salt Works (neon salt) |
| Korriban | Excavation Dig (Sith relics, contraband) |
| Dathomir | Herb Grove (witch herbs) |
| Dantooine | Grain Farm (grain), Bantha Ranch (bantha milk) |

- **Output:** one batch of the holding's good per day, stored in that dock's warehouse until the owner's ship collects it as cargo.
- **Upgrades** (levels 1-3) raise output and cost goods from other planets, delivered to the holding's dock. For example, a level-3 Moisture Farm needs Czerka machine parts and reactor cores.
- **Production chains:** a holding upgraded with a **Processing Plant** turns its own output plus a delivered input into a refined good worth more than both. Examples:
  - Grain + Water -> Ration Packs
  - Reactor Cores + Tibanna Gas -> **Station Fuel**
  - Viscaran Crystals + Machine Parts -> Focusing Lenses
  - Hides + Witch Herbs -> Medical Supplies

  The full list is drafted with the commodity table.
- **Droid staff:** an assigned industrial droid raises output by its Industry rating (section 27).
- **Events,** rolled every few days per holding:
  - **Bandit raid:** a short instanced ground fight at the holding. Win to keep the stock, lose to lose it.
  - **Breakdown or sandstorm:** output pauses until parts are delivered.
  - **Boom:** double output for a day.
  - **Customs inspection** (contraband holdings only): pay a fine or lose the stock.
- **Ground sites:** each holding type has a small ground area (an instanced copy of a template), reached from its planet's dock. Owners manage the holding and fight its events there.
- **Economy:** new goods follow `EconomyRestrictedItems.md`. Holdings output is cargo (section 13), not items.

## 27. Industrial droids

- **Industrial parts:** every part slot (CPU, Head, Body, Arms, Legs) gets an industrial variant at every tier.
  - Industrial parts give a new **Industry** rating, through a new `DroidStatSubType.Industry` read in `Droid.LoadDroidItemPropertyDetails`.
  - In exchange, they have lower HP and attributes and **no combat skill bonuses**.
  - Examples: Labor Arms, Hauler Legs, Survey Head, Logistics CPU.
  - They are Engineering recipes at each Droid Assembly tier and go in the Bible's Equipment - Droids tab.
- **Industry rating:** a droid's Industry is the sum of its parts. Mixing combat and industrial parts gives a middling droid at both.
- **Industry pays off in four places:**
  - **Planetary Holdings:** an output boost.
  - **Ship droid crew** (section 10): station effectiveness comes from Industry, so a combat droid makes poor crew.
  - **Droid bays:** Mining Barge, Industrial and Logistics hulls get one, using the unused `ShipDetail.HasDroidBay`. A droid riding in the bay boosts mining, gas and salvage yield, or Logistics repair output.
  - **Station buildings:** faster Shipyard builds, more Asteroid Drill output.
- **Droid limit:** working droids don't count toward the one-droid limit (section 10).
- **New AI instructions,** which compete with combat instructions for AI slots:
  - **Efficiency Protocol:** holding, drill and droid-bay output.
  - **Damage Control Protocol:** Engineer station and Logistics bay repairs.
  - **Survey Protocol:** scan strength at the Sensors station.
  - **Prospecting Protocol:** the rare-find chance on mining and salvage.

## 28. Derelict boarding

- **Finding them:** derelicts come from scanning (section 24) and Wild Space.
- **Boarding:** a Small or Medium ship docks with the derelict and the party boards on foot.
- **Inside:** an instanced copy of a derelict template: existing starship interior layouts dressed as wrecks, plus a few new ones. It is a ground dungeon at the system's tier, with ground enemies, ground loot and ground abilities. Each mode keeps its own abilities.
- **Safety:** the docked ship can't be attacked while its crew is aboard the derelict. Leaving returns everyone to the ship.
- **Contested derelicts:** some derelicts are contested.
  - While the boarding party is inside, enemy fighters attack in waves around the derelict, and pilots who stay outside defend it. The docked ship stays safe.
  - Each wave cleared raises the boarders' loot.
  - If no defending ship is left, enemy boarders reinforce the derelict's interior.
  - It reuses combat-site waves (section 24) and the derelict dungeon, and gives a mixed space and ground group one shared goal.

## 29. Faction agents

- **Placement:** two agents per faction at different docks, with no dock hosting more than two. Each faction's first dock is the lower-tier one.

| Faction | First dock | Second dock |
|---|---|---|
| Republic Military | Veles (Viscara) | Dantooine |
| Czerka | CZ-220 | Hutlar |
| Smugglers | Mon Cala | Smuggler's Moon Station |
| Hutt Cartel | Anchorhead (Tatooine) | Smuggler's Moon Landing |
| Mandalorian | Hutlar | Dathomir |
| Sith Military | Korriban | Dathomir |

- **Missions:** kill, sealed courier, mining quota, convoy escort and scan-down, scaled to the dock's tier. Higher standing unlocks harder missions.
- **Rewards:**
  - credits
  - faction standing (the faction service)
  - **faction tokens,** one currency per faction, spent at that faction's store on Design Schematics, ammo variants, components and interior trophies
- **Implementation:** missions are runtime quests (`Quest.RegisterRuntimeQuest`).

## 30. Incursions

- **Schedule:** once a week, a Sith or Mandalorian armada invades one region (an orbit and its neighbors) for up to 48 hours.
- **What spawns:** waves, command capitals, and blockades at the region's docks. While a blockade holds, docking services there are offline.
- **Trade:** the region's prices spike while the incursion lasts.
- **Clearing it:** a region-wide reward, plus personal loot scaled by each player's tracked contribution.

## 31. Rift Runs

- **Rift Keys:** tiers 1 through 5.5, dropped by aces, Wild Space and salvage, and sold at faction stores.
- **The run:** a key opens a private pocket for 1-3 ships, built as new template areas with 3 rooms. It has a 15-minute timer, and the rooms escalate.
- **Finishing in time** pays loot scaled by the key's tier and the time left.
- **Running out of time** ejects everyone in escape pods, with the normal XP debt.

## 32. Cloaking

- **Cloaking Device:** Mid slot, Piloting. It fits Small hulls and Blockade Runners only.
  - Interceptors, Bombers and Explorers get a role bonus: a faster cloak and a smaller speed penalty.
  - Blockade Runners can cloak with cargo aboard.
- **While cloaked:**
  - The ship is hidden from other players.
  - It can't use modules or abilities, and its toggled modules switch off.
  - It moves slower.
- **Decloaking:** locks firing for 5 seconds.
- **Counters:** a Sensor Sweep, or any ship within 10 m, reveals a cloaked ship.
- **Replaces** today's blanket stealth block in space.

## Appendix A: Perk drafts

Draft rows for the Bible. Where a ground perk does the same job, its numbers are copied and the **Source** column names it. "New" rows have no ground match: their numbers are a starting point for the combat simulator. Everything here needs owner approval before code (section 17).

**Shared rules for every space style**

- **Costs and scaling:**
  - Every ability costs the pilot's (or crew member's) STM. Costs and cooldowns follow the ground template: 3/5/8/12 STM at 18 s, 4/8/10 at 24 s, 6/9 at 30 s, and the capstone at 15 STM and 90 s.
  - Stances have a 30 s cooldown.
  - Scaling uses the Bible Scaling Legend with the style's primary and secondary stat.
- **Terms:**
  - **Weapon DMG** is the DMG of the fitted weapon that fires.
  - **Energy weapons** are Laser Cannon, Ion Cannon, Beam Cannon, Point-Defense Turret and the capital lasers (Turbolaser, Quad Laser, Laser Battery, Storm Cannon).
  - **Launchers** are Missile Launcher, Torpedo Launcher and Assault Concussion Missile.
  - **Command range** is 30 m base. Ground Leadership uses 5 m, but space weapons reach 20-55 m.
- **New space statuses:**
  - **Jammed:** Accuracy down; Disruption family.
  - **Painted:** damage taken up.
  - Slows keep using the Mobility family.
  - None of these is hard crowd control.
- **Where perks apply:** a perk works wherever its condition is met, on any hull and from any station (cross-skill rule in `SkillPerkStandards.md`).

### Piloting: Ace (AGI primary, MGT secondary)

| Rank | SP | Perk | Type | STM | Cooldown | Effect | Source |
|---|---|---|---|---|---|---|---|
| 2 | 2 | Evasive Roll I | Combat | 3 | 18 s | Gain +10% Evasion for 12 s. The first attack you evade during it gives your weapons' next cycle +10% Critical Rate. | Point Blank Burst I evasion |
| 5 | 2 | Afterburner Discipline | Trait | - | - | Afterburner cycles cost 25% less capacitor. | New |
| 8 | 2 | Tight Turns I | Trait | - | - | +4% Evasion while your ship is moving. | New (Snap Roll ladder shape) |
| 10 | 2 | Strafing Run I | Combat | 4 | 24 s | Gain +20% Movement Speed for 10 s. Each weapon's next cycle on your target deals +12 DMG. | Interrupting Shot I DMG |
| 12 | 2 | Evasive Roll II | Combat | 5 | 18 s | As I, with +12% Evasion and +12% Critical Rate. | Quick Draw II crit |
| 15 | 4 | Slipstream | Trait | - | - | After you evade an attack, gain +10% Movement Speed for 10 s. Once every 10 s. | Kiting Instinct |
| 18 | 3 | Break Lock I | Combat | 6 | 30 s | Drop 50% of your threat on every enemy within 30 m and gain +15% Evasion for 10 s. | New |
| 20 | 4 | Ace Stance | Stance | - | 30 s | +15% Evasion and +10% Movement Speed; -15% max Shield. | Skirmisher Stance (penalty moved to shield) |
| 22 | 4 | Tight Turns II | Trait | - | - | +6% Evasion while moving. | New |
| 25 | 4 | Opportunist | Trait | - | - | Weapon cycles against a target that is attacking another ship gain +10% Critical Rate. | Quick Draw I crit |
| 28 | 3 | Evasive Roll III | Combat | 8 | 18 s | As I, with +15% Evasion and +15% Critical Rate. | Point Blank Burst II evasion |
| 30 | 4 | Strafing Run II | Combat | 8 | 24 s | As I, with +24 DMG. | Interrupting Shot II |
| 32 | 3 | Break Lock II | Combat | 9 | 30 s | Drop all your threat on every enemy within 30 m and gain +25% Evasion for 10 s. | New |
| 35 | 2 | Ace Reflexes | Trait | - | - | Below 30% hull, gain +25% Evasion for 15 s. Once per 3 minutes. | Unbreakable |
| 38 | 4 | Strafing Run III | Combat | 10 | 24 s | As I, with +36 DMG. | Interrupting Shot III |
| 40 | 5 | Evasive Roll IV | Combat | 12 | 18 s | As I, with +18% Evasion and +18% Critical Rate. | Quick Draw IV crit |
| 45 | 4 | Tight Turns III | Trait | - | - | +8% Evasion while moving. | New |
| 50 | 6 | Untouchable | Capstone | 15 | 90 s | For 30 s, every attack you evade resets Evasive Roll's cooldown and gives your weapons' next cycle +20% Critical Rate. | Last Word |

### Piloting: Wing Commander (SOC primary, WIL secondary)

| Rank | SP | Perk | Type | STM | Cooldown | Effect | Source |
|---|---|---|---|---|---|---|---|
| 2 | 2 | Focus Fire I | Combat | 3 | 18 s | Allied ships within command range deal +6% damage to your current target for 20 s. SOC can raise this to +8%. | Press the Attack I |
| 5 | 2 | Steady Wings | Trait | - | - | Allied ships within command range gain +30 Mobility Resistance. | Charge Order I |
| 8 | 2 | Wingmen I | Trait | - | - | Allied ships within command range gain +3% Accuracy. | Rallying Standard I |
| 10 | 2 | Rally I | Combat | 4 | 24 s | Allied ships within command range gain a shield buffer of 2.5% of their max Shield for 30 s. | Rousing Shout I |
| 12 | 2 | Focus Fire II | Combat | 5 | 18 s | As I, with +8% (SOC to +10%). | Press the Attack II |
| 15 | 4 | Crew Morale | Trait | - | - | Crew and droid crew aboard your ship work their stations at +10% effectiveness. | New |
| 18 | 3 | Break Formation I | Combat | 6 | 30 s | Enemies within 10 m of your target are Jammed for 30 s: -10% Accuracy (SOC to -12%). | Break Morale I |
| 20 | 4 | Formation Stance | Stance | - | 30 s | Allied ships within command range take 6% less damage; your Attack is -20%. | Watchful Presence II, Defensive Stance penalty |
| 22 | 4 | Wingmen II | Trait | - | - | +5% Accuracy. | Rallying Standard II |
| 25 | 4 | Mark Target | Trait | - | - | When your module or ability damages an enemy, allied ships within command range deal +8% damage to it for 30 s. | Mark Target I |
| 28 | 3 | Focus Fire III | Combat | 8 | 18 s | As I, with +10% (SOC to +12%). | Press the Attack III |
| 30 | 4 | Rally II | Combat | 8 | 24 s | As I, with 3.75% of max Shield. | Rousing Shout II |
| 32 | 3 | Break Formation II | Combat | 9 | 30 s | As I, with -15% Accuracy, plus -12% Attack (SOC to -18% and -14%). | Break Morale II |
| 35 | 2 | Command Presence | Trait | - | - | Orders last 4 s longer and command range is 10 m larger. | Command Presence II (range space-scaled) |
| 38 | 4 | Rally III | Combat | 10 | 24 s | As I, with 5% of max Shield. | Rousing Shout III |
| 40 | 5 | Focus Fire IV | Combat | 12 | 18 s | As I, with +10% damage and +5% Accuracy (SOC to +12% and +6%). | Press the Attack III |
| 45 | 4 | Wingmen III | Trait | - | - | +6% Accuracy. | New (Rallying Standard ladder) |
| 50 | 6 | Fleet Action | Capstone | 15 | 90 s | For 45 s, allied ships within command range, yours included, gain +12% damage, +6% Accuracy and +6% Critical Rate. | Decisive Command |

### Gunnery: Broadside (PER primary, AGI secondary)

| Rank | SP | Perk | Type | STM | Cooldown | Effect | Source |
|---|---|---|---|---|---|---|---|
| 2 | 2 | Overcharge I | Combat | 3 | 18 s | Your energy weapons' next cycle deals +8 DMG each and costs 50% more capacitor. | Disarming Shot I DMG |
| 5 | 2 | Capacitor Discipline | Trait | - | - | Energy weapon cycles cost 10% less capacitor. | New |
| 8 | 2 | Sustained Fire I | Trait | - | - | Each energy-weapon hit on your current target adds a stack of +2% damage, up to 5. Switching targets clears it. | New |
| 10 | 2 | Shield Breaker I | Combat | 4 | 24 s | Your energy weapons' next cycle on your target deals +50% damage to shields. | New |
| 12 | 2 | Overcharge II | Combat | 5 | 18 s | As I, with +18 DMG. | Disarming Shot II |
| 15 | 4 | Steady Barrage | Trait | - | - | While Sustained Fire has 5 stacks, gain +10% Critical Rate. | Lucky Chamber |
| 18 | 3 | Full Broadside I | Combat | 6 | 30 s | Every energy weapon fires at your target at once for weapon DMG + 14 each; their cycles restart. | Fan the Hammer I |
| 20 | 4 | Battery Stance | Stance | - | 30 s | Energy weapons cycle 15% faster but cost 25% more capacitor. | New (Gambler Stance shape) |
| 22 | 4 | Sustained Fire II | Trait | - | - | +3% damage per stack. | New |
| 25 | 4 | Hull Shredder | Trait | - | - | Energy-weapon hits on a target with no shield deal +10% damage. | New |
| 28 | 3 | Overcharge III | Combat | 8 | 18 s | As I, with +28 DMG. | Disarming Shot III |
| 30 | 4 | Shield Breaker II | Combat | 8 | 24 s | As I, with +75% damage to shields. | New |
| 32 | 3 | Full Broadside II | Combat | 9 | 30 s | As I, with weapon DMG + 26. | Fan the Hammer II |
| 35 | 2 | Heat Sinks | Trait | - | - | After Overcharge, your energy weapons' following cycle costs no capacitor. | Evasive Reload shape |
| 38 | 4 | Shield Breaker III | Combat | 10 | 24 s | As I, with +100% damage to shields. | New |
| 40 | 5 | Overcharge IV | Combat | 12 | 18 s | As I, with +38 DMG. | Disarming Shot IV |
| 45 | 4 | Sustained Fire III | Trait | - | - | +4% damage per stack. | New |
| 50 | 6 | Firestorm | Capstone | 15 | 90 s | For 30 s, your energy weapons cycle 30% faster and Sustained Fire stacks up to 10. | New (Dead Man's Hand shape) |

### Gunnery: Ordnance (MGT primary, PER secondary)

| Rank | SP | Perk | Type | STM | Cooldown | Effect | Source |
|---|---|---|---|---|---|---|---|
| 2 | 2 | Volley I | Combat | 3 | 18 s | Every launcher fires at your target at once for weapon DMG + 8 each, using ammo as normal; their cycles restart. | Disarming Shot I DMG |
| 5 | 2 | Efficient Loading | Trait | - | - | Launcher cycles have a 15% chance not to use ammo. | New |
| 8 | 2 | Lock-On I | Trait | - | - | After 5 s on the same target, your launchers gain +3% Accuracy and +3% Critical Rate against it. | New |
| 10 | 2 | Bunker Buster I | Combat | 4 | 24 s | Your next launcher hit within 10 s deals +25% damage to a Large or Capital hull. | New |
| 12 | 2 | Volley II | Combat | 5 | 18 s | As I, with +18 DMG. | Disarming Shot II |
| 15 | 4 | Proximity Warheads | Trait | - | - | Missile hits also deal 25% of their damage to other enemies within 5 m of the target. | New |
| 18 | 3 | Cluster Barrage I | Combat | 6 | 30 s | Fire at up to 3 enemies within 10 m of your target for weapon DMG + 14 each, using 1 ammo per target. | Fan the Hammer I |
| 20 | 4 | Siege Stance | Stance | - | 30 s | Launchers deal +15% damage to Large and Capital hulls; your Evasion is -15%. | New (Skirmisher Stance size) |
| 22 | 4 | Lock-On II | Trait | - | - | +5% Accuracy and +5% Critical Rate. | New |
| 25 | 4 | Shield Breach | Trait | - | - | Launcher hits on a target at 10% shield or less deal +15% damage. Stacks with the Missile Launcher's own low-shield bonus. | New |
| 28 | 3 | Volley III | Combat | 8 | 18 s | As I, with +28 DMG. | Disarming Shot III |
| 30 | 4 | Bunker Buster II | Combat | 8 | 24 s | As I, with +40%. | New |
| 32 | 3 | Cluster Barrage II | Combat | 9 | 30 s | As I, up to 4 enemies, weapon DMG + 26. | Fan the Hammer II |
| 35 | 2 | Rapid Reload | Trait | - | - | After Volley, your launchers' next cycle is 30% shorter. | New (Reload Tempo shape) |
| 38 | 4 | Bunker Buster III | Combat | 10 | 24 s | As I, with +60%. | New |
| 40 | 5 | Volley IV | Combat | 12 | 18 s | As I, with +38 DMG. | Disarming Shot IV |
| 45 | 4 | Lock-On III | Trait | - | - | +7% Accuracy and +7% Critical Rate. | New |
| 50 | 6 | Saturation Strike | Capstone | 15 | 90 s | Every launcher fires at your target three times over 6 s for weapon DMG + 30 each, without using ammo. | Dead Man's Hand DMG |

### Ship Systems: Bulwark (VIT primary, WIL secondary)

| Rank | SP | Perk | Type | STM | Cooldown | Effect | Source |
|---|---|---|---|---|---|---|---|
| 2 | 2 | Defiance I | Combat | 3 | 18 s | Your weapons' next cycle on your target deals +8 DMG each, and the target generates +25% threat toward you for 30 s. | Disarming Shot I DMG, Covering Strike threat |
| 5 | 2 | Reinforced Bulkheads | Trait | - | - | +15 Mobility Resistance and +15 Disruption Resistance. | Fortification |
| 8 | 2 | Hardened Shields I | Trait | - | - | +8% Defense while your shield is above 0. | Bulwark I |
| 10 | 2 | Bastion Pulse I | Combat | 4 | 24 s | Enemies within 15 m take 15 EM damage and generate +25% threat toward you for 30 s. | Covering Strike I |
| 12 | 2 | Defiance II | Combat | 5 | 18 s | As I, with +18 DMG. | Disarming Shot II |
| 15 | 4 | Emergency Capacitor | Trait | - | - | When your shield drops to 0, restore 15% of max capacitor. Once per 60 s. | New |
| 18 | 3 | Shield Projection I | Combat | 6 | 45 s | Channel for up to 30 s: you and allied ships within 15 m take 20% less damage. | Shield Wall I (range space-scaled) |
| 20 | 4 | Bulwark Stance | Stance | - | 30 s | +30% threat generation, +20% Defense, -20% Attack. You generate +100% threat from damage dealt. | Defensive Stance |
| 22 | 4 | Hardened Shields II | Trait | - | - | +10% Defense while your shield is above 0. | Bulwark II |
| 25 | 4 | Absorption | Trait | - | - | When an attack hits your shield, restore 4 capacitor. Once every 6 s. | Alacrity |
| 28 | 3 | Defiance III | Combat | 8 | 18 s | As I, with +28 DMG. | Disarming Shot III |
| 30 | 4 | Bastion Pulse II | Combat | 8 | 24 s | As I, with 25 EM damage. | Covering Strike II |
| 32 | 3 | Shield Projection II | Combat | 9 | 45 s | As I, with 35% less damage. | Shield Wall II |
| 35 | 2 | Last Stand | Trait | - | - | Below 25% hull, gain +40% Defense for 30 s. Once per 3 minutes. | Unbreakable |
| 38 | 4 | Bastion Pulse III | Combat | 10 | 24 s | As I, with 30 EM damage. | Covering Strike III |
| 40 | 5 | Defiance IV | Combat | 12 | 18 s | As I, with +38 DMG. | Disarming Shot IV |
| 45 | 4 | Hardened Shields III | Trait | - | - | +12% Defense while your shield is above 0. | Bulwark III |
| 50 | 6 | Unbreakable Hull | Capstone | 15 | 90 s | For 45 s, take 50% less damage and ignore slows and Interdiction. | Invincible |

### Ship Systems: Operator (WIL primary, SOC secondary)

| Rank | SP | Perk | Type | STM | Cooldown | Effect | Source |
|---|---|---|---|---|---|---|---|
| 2 | 2 | Emergency Repair I | Combat | 3 | 18 s | Repair an allied ship within 30 m for 2% of its max hull plus WIL scaling. | Med Kit I (percent part) |
| 5 | 2 | Efficient Transfer | Trait | - | - | Repairers and capacitor transfers cost 10% less capacitor. | New |
| 8 | 2 | Repair Efficiency I | Trait | - | - | Repairs from your modules and abilities are 5% stronger. | Medical Injector Rig I |
| 10 | 2 | Capacitor Transfer I | Combat | 4 | 24 s | Give 10% of your max capacitor to an allied ship within 30 m. | New |
| 12 | 2 | Emergency Repair II | Combat | 5 | 18 s | As I, with 3.5% of max hull. | Med Kit II |
| 15 | 4 | Sensor Reach | Trait | - | - | Your Jammers, Target Painters and Ion Webs reach 10 m further. | New |
| 18 | 3 | Sensor Blackout I | Combat | 6 | 30 s | Enemies within 10 m of your target are Jammed for 30 s: -10% Accuracy and -10% weapon range. | Break Morale I |
| 20 | 4 | Triage Stance | Stance | - | 30 s | Your repairs are 20% stronger and your repairers cycle 10% faster; your weapons deal 25% less damage. | New |
| 22 | 4 | Repair Efficiency II | Trait | - | - | 10% stronger. | Medical Injector Rig II |
| 25 | 4 | Target Analysis | Trait | - | - | Targets you paint take +5% damage from all sources. | New |
| 28 | 3 | Emergency Repair III | Combat | 8 | 18 s | As I, with 5% of max hull. | Med Kit III |
| 30 | 4 | Capacitor Transfer II | Combat | 8 | 24 s | As I, with 15%. | New |
| 32 | 3 | Sensor Blackout II | Combat | 9 | 30 s | As I, with -15% Accuracy and -15% weapon range. | Break Morale II |
| 35 | 2 | Overflow | Trait | - | - | Shield repairs on a target at full shield repair its hull at 50% strength. | New |
| 38 | 4 | Capacitor Transfer III | Combat | 10 | 24 s | As I, with 20%. | New |
| 40 | 5 | Emergency Repair IV | Combat | 12 | 18 s | As I, with 6% of max hull. | Med Kit IV |
| 45 | 4 | Repair Efficiency III | Trait | - | - | 15% stronger. | New (Rig ladder) |
| 50 | 6 | Lifeline Network | Capstone | 15 | 90 s | For 30 s, your repairers also repair up to 3 other allied ships within 15 m of their target, and all your repairs are 25% stronger. | New |

### Gathering: space perks (new "Space Industry" group)

Traits only, priced like the existing Gathering lines. No perk unlocks a module; Gathering rank does that through `RequiresSkill`.

| Perk | Gathering rank | SP | Effect | Source |
|---|---|---|---|---|
| Prospector I / II / III | 10 / 25 / 40 | 2 / 3 / 4 | +1 / +2 / +3 ore per asteroid cycle. | Starship Mining I-II (refunded) |
| Deep Core I / II | 20 / 40 | 3 / 4 | +10 / +20 to the rare-metal chance from asteroids. | Treasure Hunter |
| Gas Siphoning I / II | 15 / 35 | 2 / 3 | +1 / +2 Tibanna Gas per cycle. | New |
| Salvage Expert I / II / III | 10 / 25 / 40 | 2 / 3 / 4 | +1 / +2 / +3 items per salvage cycle. | New (Prospector shape) |
| Trait Hunter I / II | 20 / 40 | 3 / 4 | Salvaged components are 10% / 20% more likely to carry a rare trait. | New |
| Rapid Extraction I / II | 30 / 45 | 3 / 4 | Mining, gas and salvage cycles are 10% / 20% faster. | New |

## Appendix B: Module families

Every family has an item every 5 ranks (0-45); capital families start at rank 40. "Toggle" means it fires every cycle while on (section 8, Activation). Numbers come from the Bible draft.

| Family | Slot | Skill | Use | What it does | Today |
|---|---|---|---|---|---|
| Laser Cannon | High | Gunnery | Toggle | Thermal damage on a steady cycle. | Combat Laser |
| Ion Cannon | High | Gunnery | Toggle | EM damage, strong against shields; slows on hull hits. | Ion Cannon |
| Beam Cannon | High | Gunnery | Toggle | Thermal damage over a few ticks at short range. | Beam Cannon |
| Missile Launcher | High | Gunnery | Toggle | Explosive damage at long range; uses missiles; bonus against low shields. Ion, incendiary and seeker ammo variants. | Missile Launcher |
| Torpedo Launcher | High | Gunnery | One press | Heavy explosive damage to Large and Capital hulls; bypasses shields; ignores the size penalty; uses torpedoes. | Proton Bomb Launcher |
| Point-Defense Turret | High | Gunnery | Toggle | Always fires as Light; bonus against Small hulls. | New |
| Turbolaser, Quad Laser, Storm Cannon | High | Gunnery | Toggle | Capital weapons on short cycles. Capital hulls only. | Capital weapons |
| Laser Battery, Assault Concussion Missile | High | Gunnery | One press | Capital burst weapons: an area volley, or a heavy missile using rare ammo. Capital hulls only. | Capital weapons |
| Mining Laser | High | Gathering | Toggle | Mines asteroids up to its tier. | Mining Laser |
| Strip Miner | High | Gathering | Toggle | Large and Capital hulls; strips a whole asteroid and roots the ship while it runs. | Strip Miner |
| Salvager | High | Gathering | Toggle | Salvages wrecks for scrap, components and rare finds. | New |
| Gas Harvester | High | Gathering | Toggle | Harvests gas clouds. | New |
| Shield Booster | Mid | Piloting | Passive | + max shield. | Shield Booster (was Low) |
| Shield Repairer | Mid | Ship Systems | Toggle | Repairs the shield of its locked target, or your own. | Shield Repairer (was High) |
| Shield Hardener | Mid | Ship Systems | Toggle | + resistances while on; pays capacitor each cycle. | New |
| Capacitor Battery | Mid | Piloting | Passive | + max capacitor. | Capacitor Booster (was Low) |
| Hypermatter Injector | Mid | Ship Systems | One press | Restores capacitor; uses a capsule. | Hypermatter Injector (was High) |
| Ion Web | Mid | Ship Systems | Toggle | Slows the target. | New |
| Interdictor | Mid | Ship Systems | Toggle | The target can't jump, dock or leave space while it holds. | New |
| Jammer | Mid | Ship Systems | Toggle | Jams the target (Accuracy down). | New |
| Target Painter | Mid | Ship Systems | Toggle | Paints the target (damage taken up). | New |
| Bulwark Shield Generator, Repair Field, E-War array | Mid | Ship Systems | Toggle | Capital fleet support. Capital hulls only. | Capital support modules |
| Afterburner | Mid | Piloting | Toggle | + speed while on; pays capacitor each cycle. | New |
| Cloaking Device | Mid | Piloting | Toggle | Hides a Small hull or Blockade Runner; no module or ability use while cloaked (section 32). | New |
| Probe Launcher | Mid | Ship Systems | Toggle | Scans hidden signatures system-wide; pulse mode for a Sensor Sweep (section 24). | New |
| Data Analyzer | Mid | Ship Systems | One press | Starts the slicing mini-game on a data can (section 24). | New |
| Targeting Computer | Mid | Gunnery | Passive | + accuracy. | Targeting System (was Low) |
| Hull Plating | Low | Piloting | Passive | + max hull. | Hull Booster |
| Hull Repairer | Low | Ship Systems | Toggle | Repairs the hull of its locked target, or your own. | Hull Repairer (was High) |
| Resist Plating | Low | Piloting | Passive | + one resistance, or all resistances at a cost to evasion. | Mirrored Plating, Ion Shielding, Reactive Armor, Durasteel Plating |
| Capacitor Regulator | Low | Ship Systems | Passive | + capacitor regen. | New |
| Smuggling Compartment | Low | Ship Systems | Passive | Lowers the customs scan chance. | New |
| Damage Amplifier | Low | Gunnery | Passive | + one damage type. | EM, Thermal and Explosive amplifiers |
| Thrusters | Low | Piloting | Passive | + evasion and speed. | Evasion Booster |
| Cargo Expander | Low | Piloting | Passive | + cargo hold. | New |
| Capital passives | Low | Ship Systems | Passive | Advanced Thrusters, Redundant Shield Generator, Reinforced Plating, Targeting Array, Weapons Computer, Power Diverter. Capital hulls only. | Capital passives |

## Appendix C: The 100 ships

Model and Launch skins are `appearance.2da` rows. Launch skins are Hull Designs available at release; they are other variants from the same model family. Piloting is the required rank. Signature bonus sizes are drafts for the Bible formula.

| # | Ship | Size | Role | Maker | Tier | Piloting | Model | Launch skins | Signature bonus |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Striker | Small | Fighter | Corellian | Standard | 0 | 10057 | 10190 | Weapon cycles 5% faster. |
| 2 | Republic Striker | Small | Fighter | Republic | Standard | 0 | 10071 | 10072, 10073 | +15% shield regen. |
| 3 | Sith Striker | Small | Fighter | Sith | Standard | 0 | 10089 | 10090, 10091 | +5% Critical Rate with energy weapons. |
| 4 | Davaab | Small | Fighter | Mandalorian | Standard | 0 | 10155 | - | +10% max hull and +5% missile damage. |
| 5 | Hutt Fighter | Small | Fighter | Hutt | Standard | 0 | 10105 | - | +15% max hull and a 10-unit cargo hold. |
| 6 | Chiss Clawcraft | Small | Fighter | Chiss | Standard | 0 | 10204 | - | +10% accuracy against targets beyond 30 m. |
| 7 | Corsair | Small | Fighter | Corellian | Standard | 0 | 10193 | 10194 | +10% damage to slowed or interdicted targets. |
| 8 | Liberty Fighter | Small | Fighter | Republic | Standard | 0 | 10199 | 10200 | +10% repairs received. |
| 9 | Rheinland Fighter | Small | Fighter | Sith | Standard | 0 | 10210 | 10211 | Critical hits deal +10% damage. |
| 10 | Aurek Strikefighter | Small | Fighter | Republic | Advanced | 10 | 10074 | - | +20% shield regen; Point-Defense fires as Medium. |
| 11 | Sith Fighter | Small | Fighter | Sith | Advanced | 10 | 10092 | 2008 | +10% Critical Rate; critical hits restore 1 capacitor. |
| 12 | Cutlass | Small | Fighter | Corellian | Advanced | 10 | 10161 | - | +1 Mid slot. |
| 13 | Condor | Small | Interceptor | Corellian | Standard | 0 | 10055 | - | +10% speed. |
| 14 | Republic Scout | Small | Interceptor | Republic | Standard | 0 | 10068 | 10069, 10070 | Ion Web range +20%. |
| 15 | Sith Scout | Small | Interceptor | Sith | Standard | 0 | 10086 | 10087, 10088 | +10% damage to webbed targets. |
| 16 | Teroch | Small | Interceptor | Mandalorian | Standard | 0 | 10153 | - | +10% max hull; Interdictor cycles 20% faster. |
| 17 | Zoomer | Small | Interceptor | Corellian | Standard | 0 | 10144 | - | Afterburner costs 25% less capacitor. |
| 18 | Chiss Nighthawk | Small | Interceptor | Chiss | Standard | 0 | 10205 | - | Sensor Sweep range +25%. |
| 19 | Eclipse Interceptor | Small | Interceptor | Sith | Advanced | 10 | 2006 | - | +15% speed; webs also lower the target's evasion. |
| 20 | Arrow Interceptor | Small | Interceptor | Republic | Advanced | 10 | 2001 | 2003 | +20% evasion while the Afterburner is on. |
| 21 | Republic Bomber | Small | Bomber | Republic | Standard | 0 | 10058 | 10059, 10060 | Torpedo hits strip 10% extra shield. |
| 22 | Sith Bomber | Small | Bomber | Sith | Standard | 0 | 10077 | 10078, 10079 | +5% torpedo Critical Rate. |
| 23 | Hutt Bomber | Small | Bomber | Hutt | Standard | 0 | 10106 | - | +15% max hull. |
| 24 | Phoebos Bomber | Small | Bomber | Corellian | Standard | 0 | 10104 | - | Torpedo cycles 10% faster. |
| 25 | Ruping Bomber | Small | Bomber | Mandalorian | Standard | 0 | 10237 | - | +10% explosive damage. |
| 26 | Chela Bomber | Small | Bomber | Republic | Advanced | 10 | 10149 | 2005 | Torpedoes ignore 25% of the target's resistances. |
| 27 | Aurora Bomber | Small | Bomber | Sith | Advanced | 10 | 10241 | 2007 | +20% torpedo damage to Capital hulls. |
| 28 | Starflier | Small | Explorer | Corellian | Standard | 0 | 10114 | - | +15% scan strength. |
| 29 | Republic Infiltrator | Small | Explorer | Republic | Standard | 0 | 10064 | 10065, 10066, 10067 | Cloaks in 2 seconds; +10% scan strength. |
| 30 | Sith Infiltrator | Small | Explorer | Sith | Standard | 0 | 10083 | 10084, 10085 | Sensor Sweep reveals ships 50% farther away. |
| 31 | Chiss Hyperion | Small | Explorer | Chiss | Standard | 0 | 10230 | - | +25% scan strength; signatures show their tier from 25%. |
| 32 | Czerka Surveyor | Small | Explorer | Czerka | Standard | 0 | 10191 | 10192 | +20% loot from data and cache sites. |
| 33 | Jalshey Serendipity | Small | Explorer | Corellian | Advanced | 10 | 10221 | 10222 | Jump to Signature spools 50% faster; passes Small rifts without spending mass. |
| 34 | Raider Prowler | Small | Explorer | Rift Raider | Advanced | 10 | 10215 | - | Scans Wild Space 50% faster and ignores environment penalties. |
| 35 | Neutral Gunship | Medium | Gunship | Corellian | Standard | 10 | 10056 | - | +1 Mid slot, -1 Low slot. |
| 36 | Republic Gunship | Medium | Gunship | Republic | Standard | 10 | 10061 | 10062, 10063, 10148 | +10% shield resistances. |
| 37 | Sith Gunship | Medium | Gunship | Sith | Standard | 10 | 10080 | 10081, 10082 | +10% threat generation and +5% damage. |
| 38 | Hutt Gunship | Medium | Gunship | Hutt | Standard | 10 | 10107 | - | +20% max hull, -10% speed. |
| 39 | Brute Patrol Ship | Medium | Gunship | Mandalorian | Standard | 10 | 10156 | - | +10% hull resistances. |
| 40 | Chiss Longhorn | Medium | Gunship | Chiss | Standard | 10 | 10163 | - | +10% accuracy; Jammers cycle 15% faster. |
| 41 | Basilisk War Droid | Medium | Gunship | Mandalorian | Advanced | 20 | 10120 | - | +15% hull resistances and +10% explosive damage. |
| 42 | Raider Bruiser | Medium | Gunship | Rift Raider | Advanced | 20 | 10216 | - | 10% of damage taken returns as capacitor. |
| 43 | Republic Mender | Medium | Logistics | Republic | Standard | 10 | 10101 | - | +15% remote shield repair. |
| 44 | Czerka Repair Tender | Medium | Logistics | Czerka | Standard | 10 | 10182 | - | +15% remote hull repair. |
| 45 | Corellian Lifeline | Medium | Logistics | Corellian | Standard | 10 | 10102 | - | +20% capacitor transfer. |
| 46 | Chiss Caretaker | Medium | Logistics | Chiss | Standard | 10 | 10103 | - | Remote repair range +25%. |
| 47 | Jedi Transport | Medium | Logistics | Republic | Advanced | 20 | 10119 | 10157 | Repairs 20% stronger; repairs also remove one slow. |
| 48 | Onderon Service Ship | Medium | Logistics | Czerka | Advanced | 20 | 10226 | - | Repairers cycle 15% faster; droid bay repairs +25%. |
| 49 | Y8 Miner | Medium | Mining Barge | Czerka | Standard | 10 | 10152 | - | +15% ore yield. |
| 50 | Gas Skimmer | Medium | Mining Barge | Corellian | Standard | 10 | 10185 | - | +15% gas yield. |
| 51 | Hutt Ore Hauler | Medium | Mining Barge | Hutt | Standard | 10 | 10151 | - | +10% yield and a 30-unit cargo hold. |
| 52 | Republic Survey Barge | Medium | Mining Barge | Republic | Standard | 10 | 10201 | - | +20% rare-metal chance. |
| 53 | Deep Core Barge | Medium | Mining Barge | Czerka | Advanced | 20 | 10227 | - | Mines asteroids one tier above its laser; +20% yield. |
| 54 | XS Freighter | Medium | Light Freighter | Corellian | Standard | 10 | 10146 | - | +20% cargo. |
| 55 | Czerka Courier | Medium | Light Freighter | Czerka | Standard | 10 | 10054 | - | +15% speed while carrying cargo. |
| 56 | Hutt Hauler | Medium | Light Freighter | Hutt | Standard | 10 | 10186 | - | Comes with a built-in Smuggling Compartment. |
| 57 | Sith Transport | Medium | Light Freighter | Sith | Standard | 10 | 10240 | - | +10% max shield; Sith customs scan it 25% less. |
| 58 | Chiss Trader | Medium | Light Freighter | Chiss | Standard | 10 | 10206 | - | HoloNet price board shows live prices. |
| 59 | Lambda Shuttle | Medium | Light Freighter | Republic | Advanced | 20 | 2002 | - | +30% cargo and +15% max shield. |
| 60 | Pirate Freighter | Medium | Blockade Runner | Corellian | Standard | 10 | 10184 | - | Cloaks in 2 seconds and +10% cargo. |
| 61 | Trandoshan Runner | Medium | Blockade Runner | Hutt | Standard | 10 | 10147 | - | -25% customs scan chance. |
| 62 | Chasemaster | Medium | Blockade Runner | Corellian | Standard | 10 | 10164 | - | +15% speed, -15% cargo. |
| 63 | Doomtreader | Medium | Blockade Runner | Mandalorian | Standard | 10 | 10160 | - | +15% max hull; Interdictors hold it 50% shorter. |
| 64 | Barracuda | Medium | Blockade Runner | Corellian | Advanced | 20 | 10113 | - | Immune to Ion Webs; ships must be within 5 m to reveal it. |
| 65 | Neutral Foray | Large | Frigate | Corellian | Standard | 25 | 10133 | - | +1 High slot, -1 Mid slot. |
| 66 | Republic Foray | Large | Frigate | Republic | Standard | 25 | 10075 | - | +10% max shield. |
| 67 | Sith Foray | Large | Frigate | Sith | Standard | 25 | 10137 | - | +10% weapon damage. |
| 68 | Republic Hammerhead | Large | Frigate | Republic | Standard | 25 | 10076 | 10134 | Point-Defense deals +20% damage. |
| 69 | Sith Hammerhead | Large | Frigate | Sith | Standard | 25 | 10138 | - | +5% Critical Rate. |
| 70 | Mandalorian Raider | Large | Frigate | Mandalorian | Standard | 25 | 10121 | - | +15% max hull and +10% missile damage. |
| 71 | Interceptor IV | Large | Frigate | Chiss | Advanced | 35 | 10224 | - | +15% weapon range. |
| 72 | Raider Marauder | Large | Frigate | Rift Raider | Advanced | 35 | 10196 | 10195 | Shield regen doubles while no weapon is firing. |
| 73 | Heavy Lifter | Large | Industrial | Czerka | Standard | 25 | 10188 | - | +20% strip-mining yield. |
| 74 | Czerka Salvager | Large | Industrial | Czerka | Standard | 25 | 10197 | 10198 | +20% salvage yield. |
| 75 | Hutt Bulk Hauler | Large | Industrial | Hutt | Standard | 25 | 10132 | - | +30% cargo. |
| 76 | Sith Quartermaster | Large | Industrial | Sith | Standard | 25 | 10131 | - | The Strip Miner doesn't root the ship. |
| 77 | Czerka Refinery Ship | Large | Industrial | Czerka | Advanced | 35 | 10229 | - | Mined ore comes out already refined. |
| 78 | Freight Train | Large | Heavy Freighter | Czerka | Standard | 25 | 10189 | 2174 | +30% cargo. |
| 79 | Large Transport | Large | Heavy Freighter | Corellian | Standard | 25 | 10187 | - | +15% speed. |
| 80 | Hutt Barge | Large | Heavy Freighter | Hutt | Standard | 25 | 10212 | - | Built-in Smuggling Compartment; -20% customs scan chance. |
| 81 | Republic Quartermaster | Large | Heavy Freighter | Republic | Standard | 25 | 10130 | 10150 | +15% max shield; allied ships within 30 m take 5% less damage. |
| 82 | Hutt Grand Barge | Large | Heavy Freighter | Hutt | Advanced | 35 | 10109 | - | +40% cargo; Jump Beacon jumps cost 50% less fuel. |
| 83 | Neutral Thranta | Capital | Cruiser | Corellian | Standard | 40 | 10135 | - | +1 High slot. |
| 84 | Republic Thranta | Capital | Cruiser | Republic | Standard | 40 | 10123 | - | +10% max shield. |
| 85 | Sith Thranta | Capital | Cruiser | Sith | Standard | 40 | 10139 | - | +10% damage. |
| 86 | Hutt Corvette | Capital | Cruiser | Hutt | Standard | 40 | 10108 | - | +20% max hull and +50% cargo. |
| 87 | Jehavey'ir | Capital | Cruiser | Mandalorian | Standard | 40 | 10154 | - | +15% max hull and +10% explosive damage. |
| 88 | Chiss Trireme | Capital | Cruiser | Chiss | Standard | 40 | 10162 | - | +15% weapon range. |
| 89 | Corellian Gunboat | Capital | Cruiser | Corellian | Standard | 40 | 10145 | - | +10% speed and evasion. |
| 90 | Republic Valor | Capital | Cruiser | Republic | Advanced | 50 | 10117 | 10122, 2170 | +25% max shield; allied ships within 30 m regen shields 10% faster. |
| 91 | Sith Harrower | Capital | Cruiser | Sith | Advanced | 50 | 10126 | 10238, 10239 | +15% damage; critical hits slow the target. |
| 92 | Raider Dreadnought | Capital | Cruiser | Rift Raider | Advanced | 50 | 10217 | - | Every hit uses whichever damage type the target resists least. |
| 93 | Republic Interdictor | Capital | Command Ship | Republic | Standard | 40 | 10143 | - | Command range +10 m. |
| 94 | Sith Centurion | Capital | Command Ship | Sith | Standard | 40 | 10127 | 10141 | Focus Fire orders +5% stronger. |
| 95 | Czerka Armored Transport | Capital | Command Ship | Czerka | Standard | 40 | 10218 | - | Crew stations +10% effective; droid crew +20%. |
| 96 | Terminus Corsair | Capital | Command Ship | Corellian | Standard | 40 | 10207 | - | Allied ships within command range +5% speed. |
| 97 | Crusader Corvette | Capital | Command Ship | Mandalorian | Standard | 40 | 10260 | - | +15% max hull; allies within command range +5% explosive damage. |
| 98 | Republic Centurion | Capital | Command Ship | Republic | Advanced | 50 | 10128 | 10129, 10124, 10125 | Command range +20 m; orders last 25% longer. |
| 99 | Kyramud | Capital | Command Ship | Mandalorian | Advanced | 50 | 10116 | 10115 | Allied ships within command range +10% max hull. |
| 100 | Supremacy Assault Ship | Capital | Command Ship | Sith | Advanced | 50 | 10142 | 10093, 10118, 2169 | Allied ships within command range +8% Critical Rate. |

## Appendix D: Components and sourcing

Every ship recipe uses its generic components, its role's component, and its manufacturer's component. Advanced ships add one rare component. Quantities and drop chances are drafted in the Bible.

### Generic components

| Component | Crafted by | Main inputs | Used by ships | Also from |
|---|---|---|---|---|
| Hull Frame | Smithery | Refined ore, metals; Durelium in the T1-2 band | Every ship | Salvage |
| Ship Reactor | Engineering | Electronics, refined ore; Tibanna Gas in the T3-4 band | Every ship | Salvage |
| Thruster Assembly | Smithery | Refined ore, fiberplast | Every ship (two for Interceptors and Blockade Runners) | Salvage |
| Energy Emitter | Engineering | Electronics, crystals | Fighters, Frigates, Cruisers | Salvage |
| Ordnance Assembly | Smithery | Refined ore, metals | Bombers | Salvage |
| Shield Projector | Engineering | Electronics, crystals | Gunships, Logistics | Salvage |
| Sensor Suite | Engineering | Electronics, gems | Interceptors, Explorers, Command Ships | Salvage, data sites |
| Extraction Head | Engineering | Diamond-grade gems, metals, electronics | Mining Barges, Industrials | Salvage |
| Life Support System | Agriculture | Plants and food goods, fiberplast | Light and Heavy Freighters, Blockade Runners, Cruisers, Command Ships | None |

### Manufacturer components

These are not crafted. Each one comes from its manufacturer's side of the galaxy, so flying a maker's ships ties you to that faction.

| Component | Manufacturer | Sources |
|---|---|---|
| Republic Shield Matrix | Republic | Republic Military agent store; Republic freight contracts |
| Sith Targeting Core | Sith | Sith Military agent store; Imperial (T6) ships and Sith capitals; their wrecks |
| Mandalorian Plating | Mandalorian | Mandalorian agent store; Clan Zel Arun (T4) ships and Mandalorian capitals; their wrecks |
| Czerka Industrial Frame | Czerka | Czerka agent store; Czerka freight contracts; rare output from Assembly Line holdings |
| Hutt Cargo Coupling | Hutt | Hutt Cartel agent store; Hutt enforcers; their wrecks |
| Corellian Drive Kit | Corellian | Smugglers agent store; pirate raiders and the T1-T5 pirate factions; their wrecks |
| Chiss Sensor Array | Chiss | Data sites, aces, and the black markets at Smuggler's Moon, Tatooine and Hutlar |
| Raider Refit Hull | Rift Raider | Wild Space only: Rift Raider ships and cache sites |

### Rare components (Advanced ships)

| Component | Crafted by | Inputs | Used by |
|---|---|---|---|
| Thorilide Core | Engineering | Thorilide (capital-ship kills) + Ship Reactor | Advanced Republic, Sith, Corellian and Chiss ships |
| Aurodium Plating | Smithery | Aurodium (Korriban Debris Field, Wild Space) + Hull Frame | Advanced Mandalorian, Hutt and Czerka ships |
| Prototype Core | Engineering | Prototype tech (Wild Space) + Sensor Suite | Rift Raider ships |

A salvaged component carrying a rare trait can stand in for the rare component in any Advanced recipe. That makes elite and boss wrecks worth salvaging.

### Schematics

| Unlock | Sources |
|---|---|
| Ship Schematic (Advanced ship recipe) | That manufacturer's agent store at high standing, aces, incursion rewards; Chiss from data sites; Rift Raider from Wild Space |
| Design Schematic (one ship's skin) | Aces, bosses, rare salvage, Rift Runs, faction stores |

### Loot table changes

- **Faction NPC ships** drop their manufacturer's component: a low chance on basic ships, higher on elites and bosses. Map each NPC faction to the manufacturer its ships resemble while drafting.
- **Wrecks** (Salvager) yield Ship Scrap, salvaged generic components, the wreck's manufacturer component, and, on elites and bosses, rare-trait parts.
- **Aces** drop Ship Schematics, Design Schematics and rare components.
- **Wild Space** drops prototype tech, Raider Refit Hulls and Thorilide.
- **Incursions** reward Sith or Mandalorian Ship Schematics, matching the invader.
- **Rift Runs** drop rare-trait parts and Design Schematics.
- **Registration:** every new item source is registered for `EconomyObtainabilityCoverageTests` (`EconomyRestrictedItems.md`).
