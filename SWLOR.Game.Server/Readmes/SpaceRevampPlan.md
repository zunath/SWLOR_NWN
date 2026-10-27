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
24. Scanning, exploration and slicing
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
- Appendix C: The 50 ships
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
| Skills | Piloting stays the only space skill and covers every hull and every space module, for the same 50 ranks as today. Space mining and salvage move to Gathering. Engineering, Espionage and Leadership each make one space role better. |
| Stats | Piloting rank gives no stats. A ship's stats come from its hull, modules, attributes, perks and those three ground skills. |
| Gating | Hulls and modules use `RequiresSkill`. The four unlock perk lines are deleted. |
| Perks | Piloting gets 4 styles of 60 SP on the ground template, the way Devices does: Ace, Ordnance, Bulwark and Command. |
| Ships | 50 ships: 14 roles in 4 sizes, 8 manufacturers, each with a signature bonus. 37 Standard and 13 Advanced. Piloting gates 0/10/25/40, with Advanced 10 higher. |
| Looks | **Hull Designs** are skins for one specific ship. They launch from model variants and Trellis2 adds more. They're unlocked by Design Schematics and swapped by starport **Refit**. |
| Slots | High / Mid / Low. No configuration slot. |
| Modules | About 20 families, an item every 5 ranks (0-45), rank-50 named boss modules. A weapon fires at its hull's size. |
| Crafting | No new skill. Engineering builds every ship and module, as today, from components that Smithery, Agriculture and Engineering make; Fabrication builds station buildings and holding upgrades; Gathering refines. A component layer has trade-off traits, manufacturer and rare components. Research blueprints work for hulls and modules. |
| Combat | Shared ground rules: crits, one Attack and one Defense, EM/Thermal/Explosive resistances, shared status effects, threat, PvP rules. |
| Attributes | Energy weapons use AGI accuracy and PER damage; launchers use PER accuracy and MGT damage, like ranged and melee weapons on the ground. AGI evasion, PER crit, VIT hull defense, WIL shields/capacitor/repair, MGT STM for maneuvers, SOC command and trade prices. |
| Activation | Modules with a cycle are toggles that fire themselves each cycle and switch off when they can't fire. Lit icons show what's on, and a Ship HUD shows targets. |
| Crew | Stations on Large and Capital hulls, with droid crew. Gunner seats use Piloting, the Engineer seat Engineering, the Sensors seat Espionage. Ships at launch. |
| Death | Modules drop only on PvP deaths in Wild Space. Everyone aboard ejects in escape pods and takes the normal XP debt. Cargo spills on any death. |
| Enemies | Every NPC ship gets a size and a role behavior. New groups: pirates, customs patrols, Hutt enforcers, derelict defenses, convoys, aces. |
| World | 3 new small-ship zones with 3 new materials. Salvage from wrecks. Starlane fixes. Size limits on docking. |
| Trade | Cargo held as ship data, about 20 commodities, dock exchanges with moving prices, contraband and customs, pirates, freight contracts, faction standing, city tariffs. |
| Migration | Every character gets one Rebuild Token. Piloting perks are refunded. Ships and modules convert to their new equivalents. |
| Wild Space | Six persistent tier-5.5 systems reached through hidden, rotating rifts, each with an environmental effect, held by the Rift Raiders pirate coalition. |
| Exploration | A Probe Launcher reveals hidden sites (rift, combat, data, cache, gas, derelict) and jumps the party to them. |
| Slicing | Espionage's Slicing perk works in space: data cans, locked pirate caches, ace and boss wreck data cores, derelict terminals and lockboxes, lockboxes dropped by NPC ships, customs spoofing and agent data heists. Espionage XP and space reward tables. |
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
| Skills | Piloting only | Piloting only, covering every hull and module; space mining, gas and salvage in Gathering |
| Rank effects | Piloting rank adds attack, accuracy, defense and evasion | Rank only gates gear and perks; stats come from the ship and the build |
| Perks | 7 perks; 4 of them only unlock gear tiers (67 SP) | 4 styles of 18 perks (60 SP each) plus Gathering space perks; no perk unlocks gear (Appendix A) |
| Gear requirements | Perk levels stamped on examine | `RequiresSkill` rank on every hull and module |
| Hulls | 17 hulls in T1-T5 pairs, each strictly bigger, plus 10 identical capitals | 50 ships: 14 roles, 8 manufacturers, signature bonuses, Standard and Advanced (Appendix C) |
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
| Crafting | Engineering makes everything from refined ore plus electronics | Engineering still builds ships and modules, now from components (Smithery's metal parts, Agriculture's life support, its own electronics) with traits; Fabrication builds stations and holdings; research blueprints |
| Starlanes | Smuggler's Moon unreachable; Korriban return skips | New lanes; return fixed |

## 4. Skills

Piloting stays the only space skill. It covers every hull and every space module, so a pilot pays the same 50 ranks as today. Roles come from perk styles, attributes, the ship and three ground skills, never from Piloting rank.

| Skill | ID | Gates | Rank adds | Combat point category |
|---|---|---|---|---|
| Piloting | 7 (kept) | Every hull size and every space module except the Gathering and Espionage ones | No stats: rank gates gear and perks only (section 9) | Exempt (full XP from every space kill you help with, like Armor on the ground) |
| Gathering | 11 (kept) | Mining Laser, Strip Miner, Salvager, Gas Harvester | Yield | Gathering XP from mining, gas harvesting and salvage |

- **Ground skills with a space job.** None is needed to fly. Each makes one role better, so taking it is a real choice under the 400 cap.
  - **Engineering:** the Engineer seat checks Engineering rank, not Piloting, to switch on its modules (repairers, hardeners, the injector, capacitor modules, power routing). Engineering rank adds repair amount, capacitor transfer and power-routing strength for anyone using those modules, solo pilots included. A crafter with no Piloting can crew the Engineer seat.
  - **Espionage:** the Sensors seat checks Espionage rank for jammers, target painters, Ion Webs, Interdictors, the Probe Launcher and the Data Analyzer. Espionage rank adds scan strength, jam and paint strength, and the range at which cloaked ships are revealed. Its **Slicing** perk works in space (section 24): the Data Analyzer has no rank requirement, and the Slicing tier decides what a player can crack.
  - **Leadership:** Leadership rank raises the strength and range of the Command style's orders and auras, and how well crew aboard the ship work. 45% of active pilots already have it (production data, 2026-10-22).
  - Ground weapon skills (Pistol, Rifle and the rest) have no effect in space.
- **Skill attributes:** Piloting stays Utility, rank 50, active, `ContributesToSkillCap = true`. No new skill and no new combat point category.
- **Mining rank:** a Mining Laser's `RequiresSkill` follows the module rank schedule in Gathering. The asteroid tier check still applies.
- **What it costs:**
  - Casual flyer: Piloting 15 (Medium hulls and rank-15 gear).
  - Complete pilot: Piloting 50 (every hull and module), the same as today's Piloting.
  - Crew: Piloting for the pilot and gunner seats, Engineering for the Engineer seat, Espionage for the Sensors seat, up to 50 each.
  - Space miner: Piloting 10 + Gathering 40 (50 ranks) for a Mining Barge on top-tier ore, or Piloting 25 + Gathering 40 (65 ranks) to strip-mine in an Industrial. Today a top space miner needs Piloting 40, so a pure space miner pays about 10 more ranks. The same Gathering also covers ground harvesting, salvage and gas. Player text must give these numbers, never claim mining got cheaper.
  - Everything: Piloting 50 plus all four styles, 240 of a character's 410 career SP.
- **Solo target** (checked by the combat simulator, section 19): at every tier, a pilot at that tier's rank with one weapon style (60 SP) solos all normal content and most elites in any hull their rank allows. Bulwark, Command and the ground skills above are never required.
- **Why one skill:** production data (2026-10-22, the 325 characters seen in the 5 days since login dates began recording) shows active pilots spending a median 321 of their 400 ranks outside space. Under a split into three space skills, only 36% of them could afford a complete solo pilot (100 ranks) without dropping a ground skill; at Piloting 50, 90% can. Specialization comes from SP (all four styles cost 240 of 410), attributes (cap 26), hull role, fitting, one stance at a time and the ground skills above.
- **XP rules:**
  - The XP range must cover the longest weapon range. Today it's 40 m against a 55 m missile.
  - Crew earn XP in their seat's skill: Piloting for gunners, Engineering for engineers, Espionage for Sensors officers.
  - Completed freight contracts and exchange sales give Piloting XP, scaled by route length and orbit danger. Price flooding and contract limits stop back-and-forth farming.
  - PvP gives no XP (unchanged).

## 5. Attributes in space

| Attribute | Space job |
|---|---|
| AGI | Energy weapon accuracy and evasion (as today) |
| PER | Energy weapon damage, launcher accuracy, and crit for every weapon |
| VIT | Hull defense (as today) |
| WIL | Max shield, max capacitor, repair strength |
| MGT | Launcher damage, and the pilot's own STM, which pays for perk abilities and physical station actions |
| SOC | Command abilities and auras; better exchange prices; repair discount (as today) |

- **Weapons copy the ground mapping:**
  - Energy weapons (Laser, Ion and Beam Cannons, the Point-Defense Turret and the capital lasers) use the ranged mapping: PER for damage, AGI for accuracy.
  - Launchers (Missile Launcher, Torpedo Launcher, Assault Concussion Missile) use the melee mapping: MGT for damage, PER for accuracy.
  - Crits use PER against VIT for both, as on the ground.
  - Melee builds get a natural Piloting style (Ordnance) and ranged builds get theirs (Ace). Without this, a melee character can tank in space but barely kill.
- **Intuitive Piloting** (WIL in place of PER for attack) is removed and refunded, since WIL now has its own job.
- **FP** is not used in space.
- **Space ability costs:** STM or ship capacitor. Modules keep costing capacitor.
- **Regen:** STM regen keeps its ground pacing unless the Bible draft proves space abilities need their own rate. If they do, add a space-specific stat; never change ground STM.

## 6. Perks and abilities

### Template

Piloting has four styles of 60 SP each, the way Devices does. `SkillPerkStandards.md` keeps the two-style rule for weapon skills only.

- **SP schedule** (rank -> SP): 2->2, 5->2, 8->2, 10->2, 12->2, 15->4, 18->3, 20->4, 22->4, 25->4, 28->3, 30->4, 32->3, 35->2, 38->4, 40->5, 45->4, 50->6.
- **Row mix per style:** 3 ranked abilities, 2 general traits, 3 cross-skill traits, 1 stance and 1 capstone. Each style opens with a rank-2 ability and ends with a rank-50 capstone.
- **Shared rules:**
  - The ground button budget applies: 4-6 distinct active buttons per style.
  - One stance at a time.
  - The hard crowd-control rules apply.
  - Power by source runs: always-on passive < conditional passive < active/stance < capstone.

### Styles

| Style | Primary / secondary | Identity | Built from |
|---|---|---|---|
| **Ace** | PER / AGI | Energy weapons and evasion: debuffing shots, double shots, evasion buffs, a crit capstone | Pistol (Skirmisher, Gambler), Rifle |
| **Ordnance** | MGT / PER | Missiles and torpedoes as area ordnance: burning blasts, flares, concussion, fragmentation | Throwing (Ordnance) |
| **Bulwark** | VIT / WIL | Tank: a real goad, threat bursts, a threat stance, defensive buffs | Vibroblade (Bulwark), Heavy Vibroblade (Immortal), Armor's Provoke |
| **Command** | SOC / WIL | Orders and repairs: party damage orders, an accuracy debuff, a damage-reduction aura, ship repairs; Leadership rank strengthens it | Leadership, First Aid |

- **No cap on styles:** a pilot can own all four, as with Devices. All four cost 240 of a character's 410 career SP, so most pick one or two. If playtests show pilots stacking every style, the existing mutual-exclusion mechanism can cap ownership at two.
- **Gathering:** add space mining, gas harvesting and salvage perks inside its existing tree. Starship Mining I-II are refunded.
  - **Space Industry, built (W4f):** Prospector I-III, Deep Core I-II and Rapid Extraction I-II are Gathering perks (PerkTypes 883-885, trait feats 3347-3349) that set `SpaceMiningYieldBonus`, `SpaceRareMetalChance` and `SpaceMiningCycleSpeedPercent`; `ShipModuleActions` reads them in `MineAsteroid` and `StripAsteroid`, and the mining modules' cycle (their `Recast`) uses `MiningCycleSeconds`. Prospector adds to the legacy Starship Mining term, which W10 removes. Deep Core is percentage points on the rare-metal chance: a separate roll (`LootTable.GetRandomItemWithRareChance`) gives the rarest metal of the asteroid table (flagged `isRare` in `HarvestingLootTableDefinition`) with that chance, else the normal weighted pick, so the share is about base + bonus x (1 - base) (Ardanium 10% to 19% and 28%). Gas Siphoning I-II and Salvage Expert I-III followed in W6a (section 12, "Added by the salvage update"), and Rapid Extraction now shortens the gas and salvage cycles too. Trait Hunter waits for the W9 traits.
- **Draft rows:** Appendix A lists every perk for all four styles and the Gathering space perks.

### Implementation rules

- **Abilities:** space abilities use the shared ability system with `.CanBeUsedInSpace()` (`Service/AbilityService/AbilityBuilder.cs:105`). No ground ability gets that flag.
- **Source:** abilities act from the ship. Range, line of sight, threat and damage source are all the ship's.
- **Perk effects:** perks grant `StatType` values that the shared space code reads. Add space `StatType`s as needed (`Service/StatService/StatType.cs`) following the StatType rules in `CodingStandards.md`. There is no `Perk.GetPerkLevel` check in space code; today's Energy Management check at `Space.cs` ~1310 goes away.
- **Bible rows:** Appendix A is already in the Design Bible on this branch, with SP prices and a full description for every rank, all marked Dev Status **Proposed**:
  - a new tab, **Piloting (Revamp)**, with all four styles grouped the way Devices groups its four, on the Twin Pistol column layout (it needs the Cross Skill column)
  - a **Space Industry** group at the bottom of the **Gathering** tab, with its own proposed total (44 SP) so the live Total SP is unchanged
  - The live **Piloting** tab stays as it is until release, because `CombatUpgradeBibleSyncTests` requires every live perk to keep an Implemented row.
  - The new tab is not in `UpdateCombatUpgradeAudit.ps1`'s tab list; it replaces the live Piloting tab at implementation.
  - At implementation: run values through the combat simulator, get owner approval, replace the Piloting tab's rows with Piloting (Revamp)'s and delete that tab, and set rows to Implemented as they land.
- **IDs:** reserve feat, spell, `spells.2da` and TLK IDs following `AbilityTargeting.md`.
- **Engine work the styles need** (Appendix A shared rules): ground statuses on ships, evade events from space hit rolls, damage and heal enmity in space, hull-based low-hull triggers, auras rebuilt for space (they're torn down on space entry today), crew effectiveness, Leadership rank scaling, and space area and command radii.

- **Engine built for the styles** (in code; styles call these and add no space-specific shared code):
  - **Pilot stats:** `Space.GetShipAccuracy/Evasion/Attack/Defense` and `RollWeaponShot` read the ground flat and percent stats (Accuracy, Evasion and Ranged Evasion %, Attack, Defense, Critical Rate %, Critical Damage %, Damage Dealt %, Single-Target and Area Ability Damage %, Damage Taken %, typed Leadership "other" reduction), in the ground's order (flat joins the rating, then the percent scales it, minimum 1). The pure arithmetic is in `SpaceCombatMath`.
  - **Statuses:** `Space.ApplyStatusToShip(source, ship, statusOrType, seconds)`. Damage-over-time ticks go through `StatusEffect.ApplyDamageOverTime`, which sends a ship's tick to its shield and hull. `Space.IsShipIncapacitated` (Dazed and the other casting blockers) stops toggle cycles, module presses and abilities. Blind makes 50% of an attacker's hits miss. Statuses and space auras end when the pilot leaves space.
  - **Threat:** weapon threat uses `Enmity.CalculateDamageEnmity`. `Space.ApplyRepairEnmity(source, repairedShip, restored)` adds half the restored points; module repairs call it. `Space.Goad` and `Space.GoadArea` wrap `Enmity.TryTaunt`.
  - **Events:** a missed shot calls `Combat.TrackAvoidedAttack` for the target, a landed critical calls `Combat.ApplyCriticalHitEffects`, every shot calls `Combat.TrackAttackActivity`. Space-only triggers subscribe to `SpaceCombatEvents` (`ShotEvaded`, `ShotLanded`, `HullDamaged` with `CrossedBelow(percent)`, `HostileAbilityUsed`).
  - **Abilities:** `SpaceAbilities.FireWeapon(activator, target, filter, bonusDmg, options)` and `FireWeaponArea(activator, center, radius, maxTargets, filter, bonusDmg, options, primaryTarget)` fire the highest-ranked usable module of the family, one impact each; `GetFireError` is for custom validation. `SpaceAbilities.ArmRider` stores an "on your next weapon hit" rider on the ship status (bonus DMG, an extra damage instance, extra threat, or a handler registered with `RegisterRiderHandler`); the next landed weapon cycle consumes it. A space ability has no activation delay, does not stop the ship, takes its target from the cursor or the ship's selected target, has no line-of-sight check, and reaches as far as the ship's longest usable weapon unless it sets `HasMaxRange`. It cannot be used outside a ship.
  - **Auras and radii:** `SpaceAuras.ApplyAura/RemoveAura` (refreshed every second, party ships within command range, the commander's ship included). `SpaceRadii`: ground radii x 4 (a 5 m ground burst is 20 m), command range 30 m plus the ground command-radius bonus x 4 plus bonuses registered with `SpaceAuras.RegisterRangeBonus`.
  - **Crew:** `SpaceCrew` holds the seats and the station fire context (see "Added by the crew update" below).
  - **Added by the Ace style** (shared with the other styles):
    - `PilotingAbilityConfiguration` (`Feature/AbilityDefinition/Piloting`) sets up a targeted shot or a self-centered burst that fires through `SpaceAbilities`. The ability file sets its own name, level and `HasRecastDelay`.
    - `SpaceHostileAbilityRewards` grants a temporary Evasion or Movement Speed bonus after a hostile space ability (`SpaceHostileAbilityUsedEvasionPercentAdjustment` and `SpaceHostileAbilityUsedMovementSpeedPercentAdjustment` with their durations). The ground stats for these triggers are skill-selector stats that cannot coexist with a pilot's ground perks, so space has its own.
    - `SpaceSustainedFire` and `SpaceRepeatedHits` count consecutive energy weapon hits against one target and add their DMG to the same shot's roll (`SpaceEnergyRepeatedTarget*` stats).
    - `ILimitedWeaponCycleStatusEffect` and `SpaceWeaponCycles` give "your next N weapon cycles" effects: while one has cycles left, a weapon toggle's next cycle runs after 0.25 s, and the weapon critical rate stat applies to the weapon's own cycles but not to ability shots.
    - Evasive Reload and High Noon reuse the ground stats (`AvoidedAttackNextSkillAbility*` with the skill set to Piloting, and the High HP critical damage pair, which space reads against hull).
  - **Added by the Ordnance style** (shared with the other styles):
    - `PilotingAbilityConfiguration.ConfigureTargetedBurst` sets up an area ability aimed at a ship (a cursor on the enemy ship, a blast of `SpaceRadii.NearTarget` around it, `LauncherRange` as its declared range). Volley uses it.
    - `SpaceOrdnance` (`Service`) holds what the launcher traits add: the Proximity Warheads splash on the launcher's own cycles (`ShotLanded`), and, for an area launcher ability that calls `OnAreaAbilityResolved` with its shots, fragmentation, the Saturation Fire Exposed count (`SpaceHitSequences`), the Cluster Warheads hit and Bombardier's Rhythm. Saturation Strike's zones are temporary `SpaceLauncherAreaFragmentation*` stats that add to Shrapnel Warheads', as the ground's Rain of Steel adds to Shrapnel Casing.
    - The traits use space-only stats (`SpaceLauncher*`), because the ground Throwing stats pick their skill through selector stats (or add up) and would mix with a pilot's ground perks. `Space.RollWeaponShot` adds the launcher shape stats (`SpaceLauncherSingleTarget/AreaAbilityDamagePercentAdjustment`) to the shot-shape percent for launcher shots; the Ordnance Stance sets them.
  - **Added by the Bulwark style** (shared with the other styles):
    - `SpaceBulwark` (`Service`) arms the Shield Ram and Defiance riders (`SpaceAbilities.ArmRider`, the Defiance handler registered with `RegisterRiderHandler`): a new one replaces the other, as the ground queues one ability. Defiance adds the damage dealt as threat through the pilot's damage enmity stat, the Fortress Strike status, and the Anger Strike threat and next-hit rider (`SpaceDefiance*` stats, the window counted from when Defiance was used). It also holds Critical Ward and Unbreakable.
    - `Space.RollWeaponShot` asks `SpaceBulwark.TryWardCriticalHit` when a shot critically hits: a warded ship takes a normal hit that rolls minimum damage, with a cooldown scaled by the pilot's MGT (`SpaceIncomingCriticalHitDowngrade*` stats, because the ground pair adds up with the Heavy Vibroblade's). Unbreakable listens to `SpaceCombatEvents.HullDamaged` and `CrossedBelow` (`SpaceLowHullDefense*` stats).
    - Ship Defense and damage taken read `SpaceDefensePercentAdjustment` and `SpaceDamageTakenPercentAdjustment` as well as the general stats. The ground Defensive Stance, Fortress Strike, Unbreakable and Invincible statuses are typed Physical, which a ship never reads, so they set the space stats too and the abilities reuse them. The Bulwark trait uses the general Defense % stat.
    - `PilotingAbilityConfiguration` gains `ConfigureRider` (no target, no cursor, turned away when the ship has no weapon), `ConfigureGoad` and `ConfigureAreaGoad` (a hostile cast that fires no weapon and costs no STM, range `GoadRange` = the Provoke's 15 m x 4). Hail uses them with `Space.Goad` and `Space.GoadArea`.
    - Invincible's Knockdown and Dazed immunities end when the pilot leaves space (`Ability.RemoveTemporaryImmunity`).
  - **Added by the Command style** (shared with the other styles):
    - `SpaceCommand` (`Service`) holds the party ships and enemy ships within command range, `ApplyOrder` (a party status for 30 s, or 45 s for the capstone, lengthened by Command Presence), Mark Target and the Command Presence range and duration bonuses. Mark Target listens to `SpaceCombatEvents.HostileAbilityUsed` for damage; Break Formation calls it for its debuff. The traits set space-only stats (`SpaceMarkTarget*`, `SpaceCommand*`, `SpaceRepairEfficiency*`, `SpaceCrewEffectiveness*`), because the ground Leadership and First Aid stats add up with a pilot's ground perks. The Command Presence range registers with `SpaceAuras.RegisterRangeBonus`; the ground's +4 m x 4 is +16 m.
    - The orders reuse the ground statuses: Focus Fire the Press the Attack statuses, Fleet Action Decisive Command's, Break Formation Flash and Weakened, Formation the Watchful Presence II aura (kept on party ships by `SpaceAuras`, switched off by using it again). A ship rolls Accuracy rather than ability hit chance and takes Leadership's "other" damage, so those statuses also set `SpaceAccuracyPercentAdjustment` and `SpaceLeadershipDamageTakenPercentAdjustment`, which the ship Accuracy and shot roll read (the stronger of the Leadership reduction stats applies).
    - `PilotingAbilityConfiguration` gains `ConfigureAllyRepair` (a support cast at a party ship in range, with `CommandRangeCeiling` as its declared range), `ConfigureOrder` (a self-centered ally area whose marker follows the pilot's command range) and `ConfigureCommandDebuff` (a hostile self-centered area, turned away when no enemy ship is in range).
    - Emergency Repair restores hull through `ShipModuleActions.RepairHull`, so it generates repair threat and Repair Efficiency applies. Its flat amounts are the Med Kit's doubled (22 / 84 / 160 / 216): in the balance model a ship's hull plus shield is about 2x a ground Normal HP and a repair restores hull only. `ShipModuleActions.ApplyRepairEfficiency` applies Repair Efficiency to shield and hull repairs (modules and abilities), the Repair Field arrays and the legacy Repair Field Generator.
    - `SpaceCrew.GetCrewEffectiveness` adds Crew Morale and the captain's Leadership crew bonus. `SpaceCommand.GetLeadershipBonusPercent` feeds command range, Mark Target's strength and the Social-scaled Command statuses (see "Added by the crew update").
  - **Added by the crew update (W8a, player crew):**
    - **Seats:** `ShipCrewSeat` and `ShipCrewSeats` (`Service/SpaceService`). Large hulls seat the Pilot, a Gunner and the Engineer; Capital hulls add a Second Gunner and Sensors; Small and Medium hulls are solo. A seat works the slots the hull's W1 tags give it: Helm slots are the Pilot's, Gunner slots the Gunner's (a Capital hull gives the first half of them, rounded up, to the first Gunner), Engineer slots (the Mid and Low station slots) the Engineer's, and the last Mid station slot of a Capital hull is Sensors'. Seat skills: Piloting for the Pilot and Gunners, Engineering for the Engineer, Espionage for Sensors.
    - **Seat registry and station fire:** `SpaceCrew.AssignSeat`, `ReleaseSeat`, `GetSeatOccupant`, `IsSeatCrewed(ship, status, slot)`. While a crew member fires a station module, `SpaceCrew.BeginStationFire` names them: `GetStatSource` (their attributes and stats), `GetFireEffectiveness`, `GetActingSeat` and `GetCreditedSkill` (their seat skill) answer for that ship until the scope ends. `Space.RollWeaponShot` reads the stat source for Accuracy, Attack, the damage attribute, crits and damage stats and scales the shot's DMG by the effectiveness; the result carries the crew member and skill for the combat point (`ShipShotResult.CreditedPlayer/CreditedSkill`). `ShipModuleActions.ApplyRepairEfficiency` and `ApplySupport` scale repairs and capacitor injections by the effectiveness and use the crew member's Repair Efficiency; support combat points go to the crew member in their seat skill (`ShipModuleActions.AddSupportCombatPoint`). The ship stays the attacker for range, threat and damage.
    - **Slot control:** `Space.ActivateShipModule` sends a crew member's press to `ActivateShipModuleCore(ship, feat, false, crewMember, seat)`: the slot must be their seat's, the module's rank must not exceed their rank in the seat skill (`ShipCrewSeats.MeetsRank`), they fire at their own target (their `SPACE_TARGET`, picked by clicking as a pilot does), and they have their own global recast. The pilot cannot fire station slots, and the pilot's abilities fire Helm weapons only (`SpaceCrew.CanActingSeatFire`). The toggle loop fires a station toggle under its crew member's context at their target and switches it off when the seat empties. Slot icons refresh for the pilot and the crew member.
    - **Station slots need their seat:** `ShipStatCalculator.Calculate(..., stationEffectiveness)` counts a station slot's passives only at its seat's effectiveness (`Space.GetStationSlotEffectiveness`: the crew multiplier while a crew member at the module's rank works the seat, otherwise 0), scaling hull, shield, capacitor and capacitor regen as the balance model does; best-of Defense, Evasion and Accuracy apply in full while crewed. `Space.RecalculateShipStats` takes the same argument, and the ship is recalculated whenever a seat fills or empties. A player crew member at the module's rank works it at 100% (Starships (Revamp)), times Crew Morale and the Leadership crew bonus, capped at 105% (`ShipCrewSeats.MaximumSeatEffectiveness`, applied by `SpaceCrew.GetCrewEffectiveness` and `ShipCrewSeats.SeatEffectiveness` to every seat, droid seats included).
    - **Fitting:** the owner fits station slots with no rank or perk check (`ShipManagementViewModel.ValidateModuleEquip` with the slot), and the pilot's launch checks skip station-slot modules (`Space.GetPilotModules`).
    - **Boarding:** `SpaceCrewStations.cs` (a `Space` partial). The ship's computer opens `CrewStationDialog` while someone else flies the ship, and so does the new `crew_console` placeable (Module/utp, palette "Crew Console"; it still has to be placed in the Large and Capital interiors). Taking a seat needs the new `PropertyPermissionType.CrewStation` (17, listed for starships next to `PilotShip`; the module-load permission refresh gives it to owners and denies it to everyone else, so no migration) and membership of the captain's party. The crew member's ground statuses and effects are removed as a pilot's are, their hotbar is saved (`Player.SerializedHotBar`) and replaced with their seat's active modules (saved per seat in `PlayerShip.PlayerHotBars` under `"<playerId>:<seat>"`), a copy of them stands at the console (seated at a `crew_chair` within 3 m when one exists), and their character jumps beside the ship. On arrival it is plot, ghosted, hidden from everyone through `VisibilityPlugin` and its camera is attached to the ship (`AttachCamera`). Every second `ProcessCrewStations` jumps it back beside the ship (more than 1.5 m away or in another area); each arrival in the ship's area hides it and attaches the camera again. Abilities and items are refused at a station. `Player.ActiveCrewShipId` marks the station in the database.
    - **Viewports:** the same dialog offers "Watch from a viewport" to anyone aboard: the hidden character and camera with no controls, no party or permission check.
    - **Leaving:** `/exit` leaves a station. Crew return to their console when the pilot leaves the controls, docks, logs out or the ship leaves space, when they leave the captain's party (checked every second), or when they arrive in an area that is neither the ship's, the console's nor another space area. A crew member who disconnects frees the seat at once; at their next login `RestoreCrewMemberAfterLogin` restores their hotbar, feats, visibility, plot flag and camera, and the space login flow returns them to the console (or their saved location after a restart). When the ship is destroyed, everyone at its stations and viewports leaves the station and is ejected alive in an escape pod with the pilot and the passengers (section 9, W2d).
    - **Rewards:** crew are party members within 40 m in the ship's area, so `CombatPoint` gives them kill XP for the combat points their station earns, in their seat skill. Loot from space kills is not owned by anyone today, so crew need no change for it.
    - **Leadership:** `SpaceCommand.LeadershipBonusPercent(rank)` = +0.5% per rank, +25% at Leadership 50, for command range, Mark Target and the Social-scaled Command statuses (Focus Fire, Formation, Fleet Action: `SocialScalingStatusEffectBase` multiplies its value by it when the source is a ship in space), and Break Formation's penalties (`SpaceCommand.ApplyLeadershipScaling`). Derivation: the ground scales Leadership by SOC only, and SOC 26 raises its orders by about a quarter over SOC 10 (Press the Attack II 8% to 10%, Decisive Command 12% to 15%, Mark Target 8% to 10%, the ground's 25% direct-effect stat bonus), so Leadership 50 adds the same quarter again. Crew effectiveness takes a smaller share, `LeadershipCrewBonusPercent(rank)` = +1% per 10 ranks, +5% at 50: with the balance model's player crew at 100%, a crewed Cruiser already matches five Fighters against a T5 Boss capital (94 s against 91 s) and at 105% is level with them (90 s); at 110% it kills in 84 s and at 125% in 74 s. So crew effectiveness is capped at 105% after Crew Morale and Leadership. Focus Fire and Mark Target also land on the crew aboard every ship they reach, since the crew fire with their own stats.
    - **Not built here:** a crew member's own Piloting abilities from a station (the ability pipeline treats its activator as the ship: validation, range, self statuses and Command orders would all need a ship proxy), droid crew and power routing (built by W8b, below), ship-to-ship docking (W8c), and the Engineering and Espionage rank bonuses of section 4 (no numbers yet).
  - **Added by the HUD (W2c, Ship HUD):**
    - **Window:** `GuiWindowType.ShipHud` (78), `ShipHudDefinition` and `ShipHudViewModel` (`Feature/GuiDefinition`), built on `AddStandardLayout` with a Ship tab (target, All Weapons, modules that are on) and a Stats tab. It opens on `OnSpaceEnter` and closes on `OnSpaceExit` (`SpaceHud`, `Service`), and `/shiphud` (or `/hud`) opens or closes it while piloting.
    - **Refresh:** open windows register with `SpaceHud`, whose 0.5 s loop publishes `ShipHudRefreshEvent` to them only. The view model binds a value or list column only when it changed (`Set` pushes on every call), and progress is rounded to 5% steps. The Stats tab refreshes only while it is selected.
    - **Pure logic:** `ShipHudRules` (`Service/SpaceService`) builds the module rows (slot label, name, target, cycle progress from the module's `RecastStartedAt` and `RecastTime`, Retarget availability), the Stats rows and the All Weapons state. `ShipHudStats.FromStatus` copies the pools, resistances (with a Shield Hardener's bonus and a Bulwark shield bonus while they last), damage bonuses, speed and fitted counts from the `ShipStatus`. `SpaceHud.CollectStats` adds the pilot-dependent values from the getters combat uses: `Space.GetShipAttack`, `GetShipAccuracy`, `GetShipDefense`, `GetShipEvasion`, and the new `GetShipShieldRegenPerSecond`, `GetShipCapacitorRegenPerSecond` (the recovery tick now calls both) and `GetShipCriticalRate` (against a target of equal Vitality at weapon rank 0). Attack and Accuracy are shown for energy and launcher weapons without a module's own Attack or Accuracy. Cargo is the hull's `Cargo`; slot counts are fitted against the hull's slots.
    - **Retarget:** a Locked-mode module (the shield and hull repairers) keeps its target in `ShipActiveModule.TargetId`. `Space.RetargetShipToggle(player, slot)` (`SpaceToggleControls.cs`, a `Space` partial) locks it to an allied ship, or to the ship itself when nothing is selected, and refuses an enemy, a wrong target type or a target out of range with a message. Other modules get no usable Retarget button: weapons follow the ship's target and area modules need none.
    - **All Weapons:** feat `FeatType.ShipAllWeapons` (3350, no spell row, `TARGETSELF=1` in `feat.2da` like the module feats), TLK 192884-192885, icon `ife_allweap`. `Space.ToggleAllWeapons` switches on every toggled offensive module of the pilot's seat (`ShipToggleRules.IsOffensive`; one-press weapons and miners are left out), or switches them all off when all are on. Refusals from `ActivateShipModuleCore` are collected and sent once. The feat is added in `EnterSpaceMode` before the hot bar loads and removed in `ExitSpaceMode`; `SpaceHud.EnsureAllWeaponsOnHotBar` puts it in the first empty quickbar slot when the ship's bar lacks it.
    - **Seats:** `ShipHudRules.IsSlotInSeat(status, slot, seat)` (built on `ShipCrewSeats.GetSeatForSlot`) is the one filter for the module list and for All Weapons. The window passes `ShipHudRules.PilotSeat`; a crew station's HUD would pass its own seat and the ship's status.
  - **Added by the droid crew update (W8b, droid crew and power routing):**
    - **Posting:** the captain (the owner, or anyone with the crew permission) posts one of their own player-built droids at an empty Gunner, Engineer or Sensors seat, and recalls it, from `CrewStationDialog` ("Droid crew"). The crew console offers it docked or in flight; a docked ship's computer opens the dialog first (its first choice takes the controls) only on a hull with stations, for a captain who carries a droid or has droids posted, so everyone else still launches with one click (`Space.TakeShipControls`, split out of `UseShipComputer`). The droid's owner may also recall it.
    - **Saved on the ship:** `PlayerShip.DroidCrew` (`ShipDroidCrew`: seat, owner, droid id, name, tier, posted time). A new list that loads empty, so no migration. The controller item stays with its owner, cursed (it cannot be dropped, sold, given or stored), with its ship in the `DROID_POSTED_SHIP` local and its own id in `DROID_CREW_ID`. `DroidCrewRules.GetActivationError` refuses to activate a posted droid, and reprogramming is refused while posted. A posted droid is never spawned, so the one-droid follow limit (the active henchman) never counts it.
    - **A seat holds a player or a droid:** `SpaceCrew.AssignSeat` and `AssignDroidSeat` refuse a worked seat; `IsSeatCrewed` and `IsSeatWorked` count both. A player who takes a droid's station bumps the droid back to its owner (`DroidCrewRules.PlayerBumpsDroid`), so a droid whose owner is offline never keeps players off a seat; a droid cannot be posted at a seat a player works.
    - **Effectiveness (W0 balance):** base = 25% + (best - 25%) x (tier - 1) / 4, best 75% at the Engineer and Sensors seats and 50% at Gunner seats: Gunner 25 / 31.25 / 37.5 / 43.75 / 50%, Engineer and Sensors 25 / 37.5 / 50 / 62.5 / 75% for tiers 1-5. Crew Morale and the Leadership crew bonus multiply it (`SpaceCrew.GetCrewMultiplier`), and the 105% cap applies (`DroidCrewRules.SeatEffectiveness`, `SpaceCrew.GetDroidEffectiveness`). `SpaceCrew.GetDroidIndustryBonus` is the Industry hook (section 27, W14) and returns 0. Station passives count at the droid's effectiveness (`Space.GetStationSlotEffectiveness`), and the launch recalculation already counts the droid crew, so a ship does not launch below its full hull.
    - **Module rank:** a droid switches on modules up to its level, tier x 10 - 5 (5, 15, 25, 35, 45; `Droid.CacheDroidLevels`), two rank bands per tier: a tier-5 droid works rank-45 gear, and the named rank-50 modules stay with players (`DroidCrewRules.MaximumModuleRank`, `MeetsRank`).
    - **Shot strength:** a droid has no creature at its post. Its fire context (`SpaceCrewFireContext.Droid`) makes `SpaceCrew.GetStatSource` OBJECT_INVALID, so no perks or statuses apply, and `Space.GetPilotScore` reads every attribute as the balance model's pilot at the droid's level (`DroidCrewRules.Attribute`: 16 + 0.2 x level, 17-25), with no NPC level term. The shot's DMG and repairs scale by the droid's effectiveness as a crew member's do. A droid's combat points go to the pilot in Piloting.
    - **Behavior:** `ProcessDroidCrew` (every second) runs the NPC ship routines, now shared (`Space.UpdateRepairToggles` and `TurnOnWeaponToggles`, which `UpdateNpcRepairToggles` and `TurnOnNpcWeaponToggles` call): repairers, shield generators and hardeners switch on once 25% of the pool is missing (`DroidCrewRules.RepairThreshold`, the NPC threshold) and off when it is full, at the ship itself; toggled weapons switch on at the pilot's hostile target in range, once per pick of target (`SpaceCrew.RecordDroidEngagement`; picking a target, the same one included, forgets them), so a weapon that auto-offs comes back with a new pick. `ActivateShipModuleCore(..., droid)` fires them with the droid's tier gate, the pilot's target and no messages. EW, the Data Analyzer, miners and one-press modules are left alone (`DroidCrewRules.GetTask`).
    - **Lifecycle:** droids take their seats when someone launches the ship (`SeatDroidCrew`, which ends postings whose seat the hull lost, whose owner is gone or whose online owner no longer carries the controller) and stop when it docks, its pilot logs out or it is destroyed (`UnloadDroidCrew`); postings stay. A droid keeps working while its owner is offline, as long as someone flies the ship. The ship's destruction puts the players aboard in escape pods (section 9, W2d) and returns every droid to its owner (`ReturnDroidCrew`, called from `Space.ApplyDeath` on every death). A controller that leaves its owner (`OnModuleUnacquire`) or that an online owner no longer carries (checked every 6 seconds) frees the seat; at login, a controller whose posting ended while its owner was away is uncursed (`ReturnEndedDroidPostings`).
    - **Power routing:** feats 3351-3354 (`FeatType.ShipPowerBalanced`, `ShipPowerWeapons`, `ShipPowerShields`, `ShipPowerEngines`; TARGETSELF, no spell row; TLK 192886-192893; icons `ife_pwrbal`, `ife_pwrwep`, `ife_pwrshd`, `ife_pwreng`) on the Engineer station's hotbar only (`LoadCrewStationHotBar`, `EnsurePowerRoutingOnHotBar`); droids do not route. `Space.SetPowerRouting` saves `ShipStatus.PowerRouting` (every 6 seconds at most). Weapons: weapon modules use 25% less capacitor, shield regen -10%, speed -10%. Shields: shield regen +15%, weapon damage -5%, speed -10%. Engines: speed +20%, weapon damage -5%, shield regen -10% (`PowerRoutingRules`). Capacitor cost (`GetModuleCapacitorCost`), `GetShipShieldRegenPerSecond`, the shot's damage bonus and the pilot's speed (`ApplyShipSpeed`) read it. A ship launches Balanced and returns to Balanced once no player works the Engineer seat (`RecalculateCrewedShip`). The Ship HUD's Stats tab shows Power Routing and the routed speed.
    - **Balance (checks.py section L):** no mode raises weapon damage, so no mode kills faster than Balanced and a crewed Frigate or Cruiser stays at or below N separate ships in every mode (Cruiser 94 s Balanced at 100% crew, 90 s at the 105% cap, against 91 s for 5 Fighters). Job score against Balanced: Weapons 92-96% and Engines 83-87% (their gains, capacitor and speed, are not in the fights), Shields 95-97% against bosses and 103-104% in the long fight against 3 Elites.
    - **Not built here:** the Industry rating (W14), Engineering rank adding power-routing strength (section 4, no numbers yet), droids at a Sensors seat running EW or the Data Analyzer, and the droid bay, holdings and station buildings (sections 26-27).
  - **Added by the docking update (W8c, ship-to-ship docking):**
    - **Permission:** `PropertyPermissionType.Dock` (18, listed for starships after `CrewStation`). The module-load permission refresh gives it to owners and denies it to everyone else, so no migration. A ship whose owner also owns the host docks whoever flies it.
    - **Docking:** the visiting pilot targets the host and types `/dock` (`Space.DockWithTarget`, `SpaceDocking.cs`, a `Space` partial); a chat command, like `/exit` and `/shiphud`, because the pilot's creature is the ship and has no menu in space. `ShipDockingRules.GetDockRefusal` (`Service/SpaceService`) checks, in order: a different ship; the host is Large or Capital and in flight with a pilot; the visitor is Small or Medium; the pilot holds the host's Dock permission; within `ShipDockingRules.DockRange` = `SpaceRadii.CommandRange` (30 m, inside weapon range); neither ship in combat; the host has room (Large 1, Capital 2). In combat means an enemy holds threat on the ship (`Enmity.HasEnmity`, the starport dock and `/exit` rule) or it dealt or took shield or hull damage in the last 10 s (`ShipDockingRules.CombatWindowSeconds`, recorded by `ApplyShipDamage` and `ApplyHullDamage`, which also covers PvP).
    - **On docking:** the visiting ship loses its current position and its pilot leaves space mode, so the ship leaves the space area (no hidden copy beside the host: a docked ship needs no position updates and travels with the host). The pilot, its viewport watchers and anyone in its interior walk to the host's interior entrance. `PlayerShip.DockedToShipId` (indexed, so hosts find their ships) and `PlayerShip.DockedCrewPlayerIds` (the docking pilot first) save it; both load empty on older records, so no migration. Docking was planned to reuse the derelict flow (section 28), which is not built; derelict boarding (W15) should reuse this one instead.
    - **Undocking:** a pilot of the docked ship (Pilot Ship permission on it) uses the host's computer or a crew console ("Undock <ship>" in `CrewStationDialog`; the docked host's computer opens the dialog for them) or types `/undock` aboard (`Space.UndockShip`). With the host in space (flown, unpiloted after a logout, or its saved position), the pilot launches through the normal launch flow to 10 m beside the host (`ShipDockingRules.GetUndockOffset`: the first ship to the host's right, the second to its left), their copy seated in their own interior; the ship leaves the host only when they arrive (`PendingShipLaunch.UndockFromShipId`), and the rest of its crew still aboard the host go back to its interior. With the host at a starport, the ship goes into that dock as the starport dock puts it there (dock and orbit, the safe dock at an NPC dock, player-starport registration; `Space.GetStarportRefusal`, section 12 "Added by the lanes and docking-limits update") and the pilot stays aboard the host to walk out. Revoking Dock stops new docking only.
    - **Host destroyed:** `Space.DetachDockedShips(host)` runs in `ApplyDeath` before the host's stations, viewports and interior are handled. Each docked ship is put in space beside the wreck (an unpiloted copy, as a logged-out ship), and the players aboard who belong to it (`ShipDockingRules.AssignReturningPlayers`: the ship they came aboard with, otherwise one they own) leave any host station and go back to it: the first who can fly it (`ChooseDetachPilot`: the docking pilot, then its crew, then its owner) takes its controls, the rest go to its interior. It returns the players it sent back, and the escape pods (`SpaceDeath.cs`, W2d) leave them out of the manifest: no XP debt and no jump to the dock. A docked ship whose interior is not loaded returns to its last dock instead, and its players share the host's fate; a host loads its docked ships' interiors when it launches (`Property.RequestInstanceLoad`), so this needs a host that launched before they loaded.
    - **Edge cases:** a docked ship stays docked while its owner is offline, while the host docks at a starport, flies starlanes or is unpiloted, and across restarts (the host returns to its dock at boot; the docked ship's pilots may board the host from Ship Management, which shows the ship as "Docked aboard <host>" and does not let it be boarded, repaired or unregistered). Its own computer refuses the controls. A host that is unregistered first puts its docked ships in its starport (`Space.UndockAllToStarport`); ship sales and transfers (section 7) do not exist yet and must call it. A ship whose host record is gone is undocked where it last docked itself. A player who logs out in the host's interior after docking returns there at login while that interior exists (`RestoreDockedPassengerAfterLogin`); arriving anywhere else forgets it.
    - **Not built here:** hostile boarding (section 21) and derelict boarding (W15). Docking limits by size at starports are built in W6b (section 12), which replaced the `FitsStarport` placeholder.

## 7. Hulls

### Roles

| Size | Piloting (Standard / Advanced) | Roles and built-in bonus |
|---|---|---|
| Small | 0 / 10 | **Fighter**: weapon damage, most High slots. **Interceptor**: fastest; stronger, longer-range tackle. **Bomber**: torpedo damage. **Explorer**: scan strength and cloaking. |
| Medium | 10 / 20 | **Gunship**: threat and resistances. **Logistics**: remote repair amount and range. **Mining Barge**: mining yield. **Light Freighter**: big interior, cargo, passengers. **Blockade Runner**: speed, cloaking with cargo aboard, lower customs scan chance. |
| Large | 25 / 35 | **Frigate**: heavy combat. **Industrial**: strip mining and salvage. **Heavy Freighter**: largest non-capital interior and hold. |
| Capital | 40 / 50 | **Cruiser**: line combat, capital weapons. **Command Ship**: command aura range and strength. |

### Ships: role, manufacturer and signature bonus

There are **50 player ships**, all listed in Appendix C. Each ship's stats come from one Bible formula with three layers:

1. **Role baseline:** slots, hull, shield, capacitor, speed, Attack, Accuracy, Defense, Evasion, signature, cargo, interior, repair multiplier, crew stations and the role's built-in bonus.
2. **Manufacturer theme:** a modifier.
3. **Signature bonus:** one of its own.

Two ships in the same role are sidegrades, never strict upgrades.

| Manufacturer | Theme | Ships |
|---|---|---|
| Republic | Shields, repairs, support | 12 |
| Corellian | Speed, evasion, flexible fittings | 10 |
| Sith | Damage and critical hits | 8 |
| Czerka | Industry: mining, salvage, cargo, drills | 6 |
| Mandalorian | Hull toughness, explosive weapons | 5 |
| Hutt | Heavy hulls, big holds, contraband | 4 |
| Chiss | Sensors, accuracy, range, scanning | 3 |
| Rift Raider | Pirate refits of stolen hulls, found only in Wild Space (Advanced only) | 2 |

**Standard and Advanced:** 37 Standard ships and 13 Advanced ships. Compared with a Standard ship, an Advanced ship has:
- **Better stats:** higher base stats, plus either one extra Mid or Low slot or a stronger signature bonus, never both. No Advanced ship gets an extra High slot, because an extra weapon outweighs every other bonus.
- **A higher Piloting gate:** 10 ranks above its size's gate (Small 10, Medium 20, Large 35, Capital 50).
- **A recipe unlock (Ship Schematic)** from drops or faction stores.
- **A higher recipe level.**
- **One rare component** (Appendix D).
- **A Shipyard requirement** when it's Large or Capital. Every Capital ship, Standard or Advanced, is built at a station Shipyard (section 25).

**Every ship defines:**
- High/Mid/Low slot counts.
- Base hull, shield and capacitor.
- Speed, and base Attack, Accuracy, Defense and Evasion. These replace today's Piloting rank bonus (section 9).
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
- **Cost:** the repair bill scales with size: Small x1.0, Medium x1.5, Large x2.5, Capital x4.0 (the Repair x column of Starships (Revamp)), before the starport and Social discounts. Built in W2d (section 9).

### Hull Designs (ship skins) and Refit

- **What a design is:** a **Hull Design** is a skin for one specific ship, usable only on that ship.
- **Launch designs:** every ship has its base model. Ships whose model family has variants launch with those variants as designs (Appendix C, "Launch skins").
- **More designs later:** the Trellis2 pipeline generates more designs per ship over time (section 20).
- **Unlocking:** a **Design Schematic: <Ship> - <Design>** unlocks one design for the character. Sources: aces, bosses, Wild Space, rare salvage and faction stores.
- **Refit:** a starport Refit swaps a ship to another design unlocked for it, for a fee.
- **Exterior only:** designs change the outside of the ship. Interior designs are a later pass, because there are few interior layouts (section 21).
- **Ship Schematics** are the recipe unlocks for Advanced ships. They replace the orphaned `recipe_aurek`, `recipe_sfight` and `recipe_basilisk`.
- **Migration:** owners keep today's look as a design of the ship they're mapped to (section 15).

### Ship names

- **In space, other players see the ship's name instead of the pilot's.** It is the ship's property name, set or changed at a starport, with the same validation as other renamed properties.
- **Examine** shows the pilot under the normal recognition rules. Route both through `PlayerName` and `PlayerDescriptor` (the system behind `/name`), following `PlayerIdentity.md`, so known-name labels and disguises keep working.
- **Leaving the ship** restores the pilot's normal name.

### Ship transfers and sales

Today the only way to hand a ship over is unregistering it, which turns it back into a deed and destroys every structure inside (`ShipManagementViewModel.OnClickUnregisterShip`). Add two ways that keep the ship whole.

- **What a transfer is:** the same ship changes owner. Nothing moves between interiors. One step re-owns the `PlayerShip`, its interior `WorldProperty` and every child structure; structures copy their owner's ID when placed, so each one must be updated. The layout, furniture positions, stored items and installed modules stay as they are.
- **Blocking conditions** (checked when offered and again at the moment it completes):
  - Nobody is aboard, and the ship isn't flying.
  - The recipient is under `Space.MaxRegisteredShips` (10).
  - Droids assigned to the ship belong to the old owner and go back to them first.
  - Any item aboard that can't normally be traded blocks the transfer until it's removed, so a transfer never passes on bound items.
- **Rules for the new owner:**
  - They can own a ship above their Piloting rank but can't fly it until they reach it.
  - Hull Designs don't change hands. The ship keeps its current look, and the new owner can only refit to designs they've unlocked. The old owner keeps every unlock.
  - Permissions reset. The old owner and everyone they invited lose access, and the new owner starts with full rights.
- **Direct transfer (gifts):** a Transfer button in Ship Management. Both players are at the starport where the ship is docked. The recipient sees a summary (ship, design, modules, furniture, stored items) and accepts. It costs a transfer fee scaled by size (section 17).
- **Sales through the player market:** the existing `PlayerMarket` already has a `Starship` category for ship deeds. Extend it so an owner can list a registered ship from Ship Management at an asking price.
  - The listing is a `MarketItem` that points at the ship instead of holding a serialized item, and it shows the ship's summary and where it's docked.
  - While listed, the ship is locked: it can't fly, be furnished or be transferred.
  - Buying uses the market's existing flow: the same-CD-key block, the price check, and the seller's proceeds minus the market tax going to their market till. Then it runs the transfer above instead of handing over an item. The market tax replaces the transfer fee.
  - The buyer doesn't need to be at the starport; the ship stays where it's docked.
  - When a listing expires after the usual 14 days, the ship simply unlocks.

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
  - a **Stats** tab with every stat of the player's ship: hull, shield and capacitor with their regen, Attack and Defense, resistances, accuracy, evasion, critical rate, speed, cargo and slot counts. Today players see only hull, shield and capacitor (Ship Management); `/shipstats` is a DM command that only the test server opens to everyone.

  The HUD exists so support-module locks are never confusing: a player can always see what each module is aimed at and change it in one click.

### Families

*New* marks a new family. Existing families keep their identity and move rows as listed.

| Row | Skill (crew seat) | Families |
|---|---|---|
| High | Piloting (Gunner) | Laser Cannon, Ion Cannon, Beam Cannon, Missile Launcher, Torpedo Launcher (today's proton bomb, expanded), *Point-Defense Turret* (always fires as Light), capital weapons (Turbolaser, Quad Laser, Laser Battery, Storm Cannon, Assault Concussion Missile) |
| High | Gathering (Helm) | Mining Laser, Strip Miner, *Salvager*, *Gas Harvester* |
| Mid | Piloting (Engineer: Engineering) | Shield Repairer, *Shield Hardener*, Hypermatter Injector, capital Bulwark and Repair Field arrays |
| Mid | Piloting (Sensors: Espionage) | *Ion Web*, *Interdictor* (target can't jump, dock or leave space for a while), *Jammer*, *Target Painter*, capital E-War array, *Probe Launcher* (section 24) |
| Mid | Piloting (Helm) | Shield Booster, Capacitor Battery, *Afterburner*, *Cloaking Device* (Small hulls and Blockade Runners; section 32), Targeting Computer |
| Mid | Espionage (Sensors) | *Data Analyzer*: one module with no rank ladder and no rank requirement. The Slicing perk gates its targets (section 24). |
| Low | Piloting (Engineer: Engineering) | Hull Repairer, *Capacitor Regulator* |
| Low | Piloting (Helm) | Hull Plating, Resist Plating (one type or all types), Thrusters, *Cargo Expander*, Damage Amplifier (per type), *Smuggling Compartment*, the six capital passives |

- **Who is checked:** the skill named first gates the module for a pilot fitting it on their own ship. At a crew seat the seat's skill is checked instead (section 10).
- **Capital modules:** capital weapons and arrays fit only Capital hulls. Hull and shield repairers and the injector now work on capital ships too.
- **NPC-only modules:** variants (`npc_*`) convert to the new families with NPC-level scaling.

### Ranks and size

- **Rank steps:** every family has a distinct item every 5 ranks: 0, 5, 10 ... 45, with in-between stats. Rank N requires rank N in the owning skill.
- **Station slots:** an owner can fit a station-slot module without meeting its rank. The crew member at that station can switch it on only if their own rank in the seat's skill meets the requirement; a droid uses its tier (section 10).
- **Stats on gear:** Piloting rank adds no stats (section 9), so each weapon step carries its own Attack and Accuracy, and hull and defensive steps carry Defense and Evasion. Higher-rank gear is how a pilot grows stronger.
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
- **Accuracy, evasion, attack and defense:** Piloting rank adds nothing, because every space player maxes it and a bonus everyone has isn't a choice. A ship's values come from its hull (role baseline, manufacturer, signature), its modules (each weapon step's Attack and Accuracy, Targeting Computers, Thrusters, plating), attributes (energy weapon accuracy AGI x 3, launcher accuracy PER x 3, evasion AGI x 3, defense VIT; section 5) and perks. NPC ships keep their level in these formulas through the space enemy presets (section 11).
- **Crits:** on, following the ground formula. The firing module's rank stands in for weapon rank (rank / 10, and the damage formula's delta cap), with PER against VIT. The ground's 5-50% range applies (`Combat.MinimumCriticalRate` and `MaximumCriticalRate`). The ground formula gives at most 13% before perks, and every perk draft is checked so its crit bonuses fit under the cap.
- **Resistances:** add `ResistanceType` Thermal, Explosive and EM, each from -100 to 100. `CombatDamageType` already has Thermal 20, Explosive 21 and EM 22.
- **Status effects:** go through `StatusEffect` with its resistance families.
  - Ion slow uses the Mobility family.
  - Hard crowd control follows the ground rules: cooldown at least 1.5x duration, 30 s maximum, 20 s immunity, no refresh, target caps.
- **Threat:** the `Enmity` service drives NPC targeting, and tank stances use the ground threat pattern.
- **PvP:**
  - A player's ship may attack another player's ship only when the ground rules allow it: the ground hostility check `GetIsReactionTypeHostile(target, attacker)`, which the engine bases on reputation, the area and server PvP setting and the players' Like/Dislike marks (the player list).
  - The `PvpCombat` ramps and halved hard crowd control apply.
  - Remote repair and buffs follow the same hostility rules.
- **Death:**
  - **Escape pods:** a destroyed ship ejects everyone aboard (pilot, crew and passengers) alive to the dock the ship is towed to. Each takes the normal ground XP debt (`Death.ApplyPenalties`). Players `Space.DetachDockedShips` returns to a docked ship are skipped (section 6, "Added by the docking update").
  - **Modules:** they drop only on a PvP death **in Wild Space** (section 23), at today's 65% chance per module, and the attacker can loot them. Everywhere else, including every existing orbit, modules stay installed on any death; players are used to those orbits being safe.
  - **Cargo:** spills on any death (section 13).
  - **Repairs:** the owner pays a repair bill scaled by hull size. The SOC and starport discounts stay.
- **Global recast:** none for toggled modules (section 8, Activation). One-press items and perk abilities follow the ground ability rules, and each crew member has their own.
- **Added by the death and PvP update (W2d):**
  - **Hostility check:** `Space.GetPvpDecision(attacker, target)` (`SpacePvp.cs`, a `Space` partial) answers for two player ships with the pure `SpacePvpRules.CanAttack(isPlayerVersusPlayer, groundHostile)`, where `groundHostile` is exactly the ground ability check `GetIsReactionTypeHostile(target, attacker)`. The ground never reads the area or server PvP setting itself (no `GetPVPSetting` or `PvpSetting` call in `SWLOR.Game.Server`; its PvP tests only set the area to Full PvP and mark the target hostile), because the engine's check already depends on "the reputation, PVP setting and (if both are PCs) the source's Like/Dislike setting for the target". Space defers to it the same way, so it cannot drift from the ground, and it does not decide what No PvP or Party PvP mean. Ships against NPC ships are never stopped (`NotPlayerVersusPlayer`). `Space.IsShipEnemy(source, target)` is the attack test (PvP decision between players, `GetIsEnemy` otherwise) and `Space.IsShipHostile(source, target)` the support test (an enemy, or a player either side marked hostile). Fixes bug 11.
  - **Where it applies:** `ApplyShipDamage` and `ApplyHullDamage` refuse a forbidden hit (so area weapons, splash, status damage-over-time and every later damage source are covered), the module activation refuses an offensive module aimed at a player it may not hit (with the reason: not marked hostile, no PvP in this area, own party), a running toggle switches itself off when its player target becomes forbidden (`ShipToggleAutoOffReason.PvpForbidden`), and every enemy finder uses `IsShipEnemy` (`GetEnemyShipsNear`, `SpaceAbilities.FireWeapon`/`FireWeaponArea` via `GetFireError`, the Volley, the Laser Battery, Goad, the area-ally lookup of `ShipModuleActions`). Remote repair and capacitor transfer (`ApplySupport`), Retarget, the Locked-mode lock target, Command orders and Command auras skip a hostile player.
  - **Ramp and crowd control:** `ApplyPvpDamageRamp` calls `PvpCombat.RecordHit` and raises the damage by the target's ramp (`SpacePvpRules.ApplyRamp` over `PvpCombatRules.IncreaseDamage`) for ship-versus-ship damage only, ahead of the shield and hull pools. Ship statuses already go through `StatusEffect`, whose `PvpCombat.IsPlayerAgainstPlayer` check halves hard crowd control, because the pilot is the ship's creature.
  - **Escape pods:** `Space.ApplyDeath` first calls `DetachDockedShips`, whose returned players stay out of the manifest. `Death.OnPlayerDeath` calls `Space.ApplyDeath(player)` first and stops when it returns true (it replaces the old `OnModuleDeath` handler on `Space`, so the order against the death panel is fixed). A destroyed ship's manifest (`EscapePodRules.BuildManifest`) is the pilot, then the station crew and viewport watchers, then the players standing in the interior area (found by area, not from `PropertyInstance.Players`: `Property.EnterPropertyInstance` read `GetExitingObject()` in an area-enter handler, so that list was filled wrongly; it now reads `GetEnteringObject()`), each once; NPCs are not touched. Everyone is jumped to the dock the ship is towed to (`PropertyLocationType.DockPosition`; if that dock is a player starport that is not loaded, the last NPC dock), the pilot is resurrected at half hit points (as after a respawn), and each takes `Death.ApplyPenalties` (now public) and a red message naming the dock. The flight ends as it does when a pilot leaves space: auras, orders, sustained-fire and ordnance state and statuses are cleared (`StatusEffect.ClearStatusEffectsOnDeath`, which the skipped ground flow would have run). DMs ride along with no debt. With no reachable dock the pilot takes the ground death flow and the others are killed, as before pods.
  - **Modules:** they drop only when `SpaceDeathRules.ModulesDrop(Space.IsWildSpace(area), killerIsPlayer)`: the killer is a player (the recorded killing blow, or the last hostile actor) and the area carries the local int `WILD_SPACE` = 1 (`Space.WildSpaceVariable`; no area has it until W12 builds Wild Space). Each module then drops at the wreck with the old 65% chance (`SpaceDeathRules.ModuleDropChancePercent`) and the rest are lost. On every other death the modules stay installed, the hot bar is kept, and the ship is left at 1 hull and no shield.
  - **Repair bill:** `ShipRepairBill` prices missing shield at 50 and hull at 100 per point, takes off the starport discount (5% per level) and the Social discount (2% per point over 10, at most 20%; 90% in all), then multiplies by the hull's `RepairMultiplier` from the Starships (Revamp) tab: Small 1.0, Medium 1.5, Large 2.5, Capital 4.0. A ship with no revamp hull keeps the old flat x5 for capitals. `ShipManagementViewModel.CalculateRepairBill` uses it.
  - **Cargo spill:** built with the cargo bay in W7a (section 13, "Added by the trade core update"): `ApplyDeath` spills the cargo into pods anyone can scoop for five minutes, on every death.
  - **Not verified in a client:** the engine tests (`ShipDeathEngineTests`) build but were not run, and no client evidence was captured.

## 10. Crew stations

Ships at launch.

- **Stations:**
  - Large hulls: Pilot, Gunner and Engineer.
  - Capital hulls: Pilot, 2 Gunners, Engineer and Sensors.
  - Small and Medium hulls are solo.
- **Slot control:**
  - Gunners toggle the High slots tagged as their turrets and pick their own target, independent of the pilot.
  - The Engineer runs active shield and hull modules, plus a new **power-routing** ability that shifts capacitor regen between weapons, shields and engines.
  - Sensors runs Mid electronic warfare and tackle, and the Data Analyzer. The crew member slices with their own Slicing perk, so an Espionage character can serve as a ship's slicer with no space skill.
  - The Pilot runs Helm slots, Afterburner, Thrusters and their own perk abilities. The pilot cannot fire station slots.
- **Seat skills:** each seat checks one skill. Gunner seats check Piloting, the Engineer seat Engineering, the Sensors seat Espionage. A crafter with Engineering and no Piloting can crew the Engineer seat, and a slicer with Espionage the Sensors seat. Section 4 lists what each skill's rank adds.
- **Gear rank on stations:** the owner fits station-slot modules without rank checks. Each crew member can switch on only the modules whose rank requirement they meet in their seat's skill; droid crew go by their tier. A captain can still outfit every seat.
- **Boarding a station:**
  - A crew member uses a console placeable in the interior. Their character moves into the space area and is hidden through `VisibilityPlugin`. It is ghosted (passes through things), can't be targeted, and is plot.
  - The camera attaches to the ship with `AttachCamera`, and a copy of them sits at the console.
  - Their hotbar swaps to the station's slots plus their own space abilities.
- **While crewing:**
  - The server keeps the hidden character beside the ship every second and re-attaches the camera after every area transition. The camera drops on transitions.
  - Everything a station does is fired from the ship. The crew member's own rank, attributes and perks set the strength.
  - **Crew rewards:** taking a station requires being in the captain's party. Crew count as party members for every reward: kill XP, loot rights, wreck claims, agent mission credit and incursion contribution. Cargo and trade profit stay with the ship's owner.
  - **Viewports:** a viewport console in the interior lets any passenger watch space from the ship with no controls. It reuses the station camera (`AttachCamera` and the hidden character) without firing anything.
- **Droid crew:**
  - A player-built droid (Engineering) can work any station at reduced effect, scaled by its Industry rating (section 27). It turns that station's modules on whenever the pilot has a valid target, and back on after an auto-off once a new valid target is picked.
  - Its tier sets which station modules it can switch on, the way a player's rank does. Its Industry rating sets how well it works them. The best industrial droid works a station at about 75% of a player of matching rank, and a tier-1 combat droid far below that, so players stay the best crew and a top-tier droid pays off.
  - **Droid limit:** a droid assigned to a ship's station, a droid bay, a holding or a station building stays at that post and doesn't count toward the one-droid limit. That limit covers only the droid that follows its owner.
  - Its logic reuses `ProcessSpaceNPCAI`.
- **Permissions and edge cases:**
  - Add a crew property permission next to `PilotShip`.
  - Handle disconnects at a station, crew leaving mid-fight, and ship death (escape pods).
- **Balance:** a ship crewed by N players must not beat N separate ships. The combat simulator checks this.
- **Station slots need their seat (W0 balance):** a Large or Capital role tags some slots in each row to a crew seat (Starships (Revamp), Station H/M/L). A station slot works only while its seat is crewed, passives included, at the crew's effectiveness. A solo pilot runs only the Helm slots. This keeps a solo Large or Capital hull a slower, tougher sidegrade to a Fighter while a fully crewed one matches the same number of separate ships.
- **Droid seat effectiveness (W0 balance):** the best droid works an Engineer or Sensors seat at 75% and a Gunner seat at 50%; a tier-1 combat droid works any seat at 25%. At 75% on Gunner seats, one pilot with droids beat a Fighter by about 2x.
- **Command:** the Command style's crew bonuses apply to everyone aboard, and the captain's Leadership rank adds to them.
- **Crew effectiveness cap (W8a):** a seat works at no more than 105%, after Crew Morale (+10%) and the captain's Leadership (+1% per 10 ranks, up to +5%), for players and droids alike. A crewed Cruiser at 105% is level with five Fighters against a T5 Boss capital (90 s against 91 s), so the cap keeps a ship crewed by N players from beating N separate ships. It binds only on player crews, who start at 100%: Crew Morale and Leadership pay off mostly for droid crew (W8b), who start at 50-75%.
- **Ship-to-ship docking:** a Small or Medium ship can dock with a player's Large or Capital ship when the host's owner grants a new **Dock** permission (next to `PilotShip`).
  - The visiting ship leaves space while docked. Its pilot and passengers walk into the host's interior.
  - Undocking returns them to their own ship at the host's position, or puts the ship into the starport where the host is docked.
  - Docked ships travel with the host. If the host is destroyed, docked ships detach where it was and their crews return to them.
  - Limits (W8c): within 30 m, neither ship in combat, a Large host carries 1 ship and a Capital host 2. Built in W8c ("Added by the docking update", section 6); derelict boarding (section 28) reuses this flow. Docking is friendly only; hostile boarding is out of scope (section 21).

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
- **DM tools:** the DM windows have no space tools today. Add a Space tab to the DM tools (`Feature/GuiDefinition/DMToolsDefinition.cs`):
  - Spawn enemy squads by faction, role and tier, using the new presets.
  - Start an incursion or a trade convoy.
  - Place a derelict, data site or cache site.
- **Models:** use unused hak models. Space fauna goes on the Trellis2 list.
- **Added by the NPC roles update (W5b):**
  - **Roles in code:** `ShipNpcRole` (parsed from the preset's Role by `ShipNpcRoleRules.ParseRole`), the decisions in `ShipNpcRoleRules` (pure) and every number in `ShipNpcRoleTuning`, with its source. `SpaceNpcRoles.cs` (a `Space` partial) runs them from `ProcessSpaceNPCAI` on the one second tick: `RunNpcRole` may swap the threat target for the role's own and set a sniper's hold distance. The roles act through the shared paths (module toggles, `ApplyStatusToShip`, `RollWeaponShot`/`CompleteWeaponShot`, enmity) and add no player-facing mechanic.
  - **Gunship = Tank** (the W0 role "holds threat and hardens"; no separate Tank hull): every 12 s (Provoke II recast) it targets the enemy with the most threat summed over its squad (non-player ships within 30 m and itself), and switches its Shield Hardener on below half hull until the hull is whole. Players cannot be goaded (`Enmity.TryTaunt` refuses them, as Provoke does), so holding threat is targeting. Gunship presets fit the Shield Hardener of their rank step and give up a quarter of its resist (`npc_catalog.HARDENER_SHARE`: the hardener runs over the last quarter of hull + shield), so the fight-length checks hold.
  - **Logistics:** dedicated `tNlogistics` ships fit a Hull and a Shield Repairer at their rank step; each idle repairer locks onto the ally (non-player ships in its 30 m range, itself included) missing the largest share of that pool once a quarter is missing (the NPC repairer threshold), through the module, so repair threat applies. `UpdateNpcRepairToggles` now measures a repairer's locked target, so it switches off when that ally is whole. Cargo haulers keep the Logistics stat row with no repairers (convoy material, W7).
  - **Electronic warfare:** every 18 s, at its weapon range, jams (Flash 8%, the ground default) or, if already jammed, paints (Exposed -10% Defense; the ground's -15% cost a matched Fighter too many fights against a pair) its threat target for 12 s. Flash sets `SpaceAccuracyPercentAdjustment` and Exposed `DefensePercentAdjustment`, which `GetShipAccuracy` and `GetShipDefense` read.
  - **Interceptor:** an enemy on its threat table that moved at least 1 m away since the last tick while below half hull or beyond its weapon range is fleeing; it chases the nearest such ship and webs it (Hamstring, -20% speed, Mobility) for 6 s every 9 s once in range. Interdiction waits for the Interdictor module (it has no mechanic yet).
  - **Bomber:** targets the biggest hull (size, then max hull) within its longest weapon range, else its threat target. **Sniper:** holds at 90% of its longest weapon's range (`ActionMoveAwayFromObject`/`ActionMoveToObject`).
  - **Carrier** (Boss, Large; boss loot like the tier's capital): launches 1 of its tier's Normal Fighters every 40 s, 4 waves, at most 2 alive. Launched fighters drop no loot and are destroyed when the carrier dies, despawns or resets on its leash. An Elite carrier broke the rule that a Fighter solos Elites: one Fighter beats an Elite carrier alone (97-100%) but only 27-53% with one wing fighter out and never with two (the Elite Fighter baseline is 87-100%, checks B), at any wave size. As a Boss, three Fighters beat it alone in 87-100% of fights and with a wing fighter out the whole fight in 73-97% (the Boss Fighter baseline is 96-100%); the W0 draft's 2 every 30 s dropped that to 20-52%. Carriers are group content and spawn at 480 (capitals 960).
  - **Minelayer** (Small, the W0 row with HP x1.0 instead of x0.9, which kept the T4 spread under 1.3x): drops a `spacemine` creature (Mine 10110, plot, no loot) every 20 s, 3 at most, each for 60 s. Twice a second a mine detonates on the nearest enemy ship within 5 m as the minelayer's shot: the Torpedo step's flags (bypasses shields, ignores size) with `ShipNpcEntry.MineDmg` (the Bomber torpedo DMG of its level) and the minelayer's launcher Attack, Accuracy and rank. Mines go with their minelayer.
  - **Role ships for every tier** (T1-T6, Module/utc, copied from the tier's closest ship with an Appendix C Hull Design appearance, an existing tier skin for the level and the source's loot table): `tNewar` (Jammer, Small, interceptor skin), `tNsniper` (Sniper, Small, bomber skin), `tNminelayer` (Minelayer, Small, bomber skin), `tNlogistics` (Repair Tender, Medium, heavy skin, gunship loot), `tNcarrier` (Carrier, Boss Large, heavy skin, the tier capital's elite and boss loot tables). They are registered in `NPCSpaceObjectDefinition.RoleShips` on the tier's legacy ship items, in the creature palette, and in every orbit of their tier at modest weights (EW, sniper, minelayer 1920 each, logistics 1440, carrier 480: about 11% of spawns together).
  - **DM Space tab:** `DMToolsDefinition` is now a standard-layout window with Placeables and Space tabs. Space spawns 1-6 ships of a faction tier and role (`SpaceSquadCatalog`) on a 12 m ring around the DM in a space area. Start Incursion (W15), Start Convoy (W7) and Place Derelict (W6/W15) are shown disabled.
  - **Balance:** checks A (now every Normal role) and K (`tools/SpaceRevampBalance/checks.py`: the hardened Gunship, a Fighter + Logistics pair repairing, a Fighter + EW pair jammed or painted, minelayers with every mine hitting, and the Elite and Boss Carrier with a wing fighter).
  - **Not built here:** pirates and convoys and customs and Hutt enforcers (W7), derelict defenses (W6/W15), aces (W9 loot), incursions and capital battle events (W15), interdiction (the Interdictor module).

## 12. Activities, zones, and materials

- **Mining:**
  - Moves to Gathering: XP, perks and module gating.
  - Fix the mining lock, the strip miner tier check and the yield math (section 16).
  - Mining, gas and salvage yields go into the ship's interior storage while it has room, and into the pilot's inventory only when storage is full or the ship has none. Today every cycle creates the item on the pilot (`MiningLaserModuleDefinition.cs:134`), so miners keep flying home over carry weight.
- **Gas harvesting:** a Gas Harvester cycles on gas-cloud placeables in the nebula.
- **Salvage:**
  - A destroyed NPC ship leaves a wreck placeable for a few minutes.
  - A Salvager cycles on it the way a Mining Laser cycles on an asteroid. Capital wrecks need a Large or Capital salvager.
  - The killing party has the wreck to itself for 60 s, then anyone can salvage it.
  - **Yields:**
    - **Ship Scrap,** which the Refinery turns into electronics of the wreck's tier. This is the first reliable electronics source.
    - **Salvaged Components,** each carrying a random trait (section 14).
    - **Rare finds** from elite and boss wrecks.
  - **Data cores:** elite, ace and boss wrecks also carry a data core. Slicing it (section 24) has better odds of Design Schematics, recipe unlocks and Ship Schematics than salvage. The killing party's 60 s first claim covers the core too.
- **Lockboxes:** destroyed NPC ships sometimes drop a space lockbox, a new set of lockbox items beside today's `lockbox_t1`-`lockbox_t5` (`LockboxItemDefinition`). The owner slices it later, out of combat. It holds components, ammo, credits and parts with rare traits, from the space slicing tables (section 24). It never holds cargo, because cargo isn't an item.
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
  - No Dantooine-Korriban lane: a direct link between the Jedi enclave world and the Sith homeworld clashes with the lore.
  - Give Smuggler's Moon (`narshadorbit`, unreachable through space today) dangerous lanes to Hutlar and Tatooine.
  - Make the Korriban route return the way it came.
- **Added by the lanes and docking-limits update (W6b):**
  - **Rules in code:** `ShipAccessRules` (pure, in `Service/SpaceService`) holds every size decision: `GetStarportRefusal(StarportDockRequest)` for starports and `AreaAdmits(shipSize, areaMax)` for areas. `Space` (`SpaceAccess.cs`) reads the area locals and the ship for them. The `FitsStarport` placeholder is gone.
  - **Area locals** (Module `git` `VarTable`, like `WILD_SPACE`):
    - `MAX_SHIP_SIZE` (int, a `ShipSize` value; 2 = Medium, so Small and Medium only; unset = no limit). On a space area it gates entry; on a ship-only location it gates docking there. Set on `zomb_abanstation`, `zomb_abanstatio2` and `zomb_abanstatio3`. The three new Small/Medium zones (W6 zones) set it to 2 on their areas.
    - `CAPITAL_DOCK` (int, 1) on the landing areas of the five major ports, the only NPC docks that take Capital hulls: `velesinterior` (Veles), `moncaladaccityex` (Mon Cala), `tat_anc_astropor` (Anchorhead), `spacenarshaddung` (Smuggler's Moon Station) and `korribanlandingp` (Korriban). These flags are the "station flags" of the W6 row.
  - **Docking limits:** Small, Medium and Large dock anywhere. A Capital hull needs a `CAPITAL_DOCK` area at an NPC dock, or a player starport whose city's effective starport level is 2 or more (`Property.GetEffectiveUpgradeLevel`, the level the city can reach with 10 citizens). A `MAX_SHIP_SIZE` on the dock's area also applies, so the Abandoned Station refuses Large and Capital hulls.
  - **Where it is enforced:** the starport dock dialog (`StarportDockDialog`, including the Abandoned Station's own dock), undocking a ship into its host's starport (`UndockShip`), and `UndockAllToStarport` (a host being unregistered). A refusal tells the pilot why in red text: the Capital message names the five ports, the player-starport message names the level 2 upgrade, and the area message names the hulls the place admits.
  - **Entering a limited area:** a starlane jump (`PlaceableScripts.UseTeleportDevice`, for the pilot and, when the lane carries party teleport, each party member), a launch from a dock (`Space.TakeShipControls`), and undocking beside a host in space (`UndockShip`) all call `Space.CanShipEnterArea`. A ship that cannot enter stays where it is. The escape from a destroyed host is exempt.
  - **Ships already too big stay:** nothing ejects a ship docked at a dock, or flying in an area, that its size no longer fits; the rule applies only to docking and entering.
  - **Starlanes (Module JSON):**
    - Smuggler's Moon (`narshadorbit`) has lanes to Hutlar and Tatooine, and both have lanes back (landings `STARLANE_LANDING_SMUGGLERS_1/2`, `STARLANE_LANDING_HUTLAR_3`, `STARLANE_LANDING_TATOOINE_3`). Lanes are `tele_obj` placeables with a `DESTINATION` landing waypoint; no concept of a dangerous lane exists yet, so these are ordinary lanes until the Hutt enforcers (section 11) are placed on them.
    - Mid Rim 2's lane toward Viscara now lands at `STARLANE_LANDING_MIDRIM_1_2` in Mid Rim 1 (the waypoint existed with no lane pointing at it), so the Korriban route returns Korriban, Mid Rim 2, Mid Rim 1, Viscara (bug 22).
    - There is no Dantooine-Korriban lane (Dantooine links only to Mon Cala).
  - **Tests:** `ShipAccessRulesTests` covers the dock matrix, the area gate, the wiring, the Module flags and the lane graph (every lane has a lane back that lands beside it, Smuggler's Moon is reachable, the Korriban route returns through each system, no Dantooine-Korriban lane).
  - **Not built here:** the 3 new zones and their flags, the Hutt enforcers, dangerous-lane marking, and refusing Capital hulls at a Shipyard purchase (no Shipyard yet; a ship bought into a dock that cannot take it is "already too big" and stays).
- **Added by the salvage update (W6a):**
  - **Ship's hold:** `ShipHold.DeliverYield` takes every mining, strip-mining, gas and salvage yield. It fills a "Ship Hold" storage category of the ship's property (created for the owner on the first yield, with the owner's category permissions), tops up existing stacks first, then opens new stacks while the ship has storage entries left (`WorldProperty.ItemStorageCount`), and gives the pilot only the rest (`ShipYieldRouting`, pure). A pilot who is not the owner uses the hold only with storage access to it; otherwise yields go to the pilot as before. The pilot sees "Stored in the ship hold: ..." each cycle and a hold-full warning at most once a minute. Cycles run on the ship (the pilot's creature), so a crew member's or droid's cycle reaches the same hold. The asteroid's bonus lyase loot (`Loot.SpawnLoot`) still goes to the pilot.
  - **Section 16 mining bugs:** bug 1 fixed (the extraction hold is a local object, `BEING_MINED_BY`, set when a cycle starts and cleared when it ends, so another ship is refused while the ship's own toggle can start its next cycle). Bug 2: the revamp strip miner already passed its tier; the legacy strip miner now checks tier 5 instead of none, the ship status is checked after the delay, and the yield is what a mining laser with the same per-cycle ore would pull over the asteroid's remaining units (`SpaceCombatMath.StripMinerYield`), with the laser's quarter of the Industrial bonus instead of a sixth. Bug 3 fixed: `ExtractionBaseAmount` uses whole-number division throughout.
  - **Gas harvesting:** the Gas Harvester family (`sm_gas_00`-`sm_gas_45`, High, Gathering, toggle, 6 s, 3 capacitor, 15 m, tier rank / 10 + 1, as the Bible's Ship Modules row) cycles on gas clouds (`spc_gascloud_t3`/`_t4`, tag `spc_gascloud`, appearance 6120 "[mdrn]Cloud", a mesh cloud; the colored fog VFX rows are emitters a click cannot pick). Each cycle yields Tibanna Gas: 1 + Industrial / 4 + module bonus / 6 + Gas Siphoning; a cloud holds 3-6 units and uses 1 + gas / 2 per cycle, like an asteroid. Clouds spawn in the Mid Rim nebula on the Viscara starlane (`space_midrim1`, new table `SPACE_RESOURCES_MIDRIM_NEBULA` = Korriban's asteroids plus tier 3 and 4 clouds, 10 each) until the Mid Rim Nebula zone is built.
  - **Salvage:** a destroyed preset NPC ship (`Space.SpawnWreckOnShipDeath`, OnCreatureDeathBefore; carrier-launched fighters excluded) leaves a wreck (`spc_wreck`, appearance 20583 "[mdrn]Wreck, ship/container"; capitals `spc_wreck_cap`, 20494 "[mdrn]Wreck Corvette front") for 300 s. The killing party (the killing blow's party, else the top-threat player's) has it for 60 s (`ShipSalvageRules.CanSalvage`). The Salvager family (`sm_salv_00`-`sm_salv_45`, same numbers as the gas harvester) needs a Large or Capital hull for a capital wreck and a tier of min(wreck tier, 5). Draft numbers, tune in testing: a wreck holds 3 / 4 / 6 / 10 cycles (Small / Medium / Large / Capital), +2 Elite, +4 Boss; each cycle yields 1 + Industrial / 4 + module bonus / 6 + Salvage Expert Ship Scrap of the wreck's tier (T6 gives tier 5); elite wrecks add a rare find from the tier's `SPACE_ELITE_Tn` table 10% of cycles and boss wrecks from `SPACE_BOSS_Tn` 20%; tier 5 and 6 wrecks add one Aurodium 4% of cycles, or the area local `SALVAGE_AURODIUM_PERCENT` (the hook for the Korriban Debris Field zone). Gas and salvage cycles give Gathering XP (target tier x 10 against Gathering rank); mining XP stays Piloting until its own move. Salvaged Components with traits (W9) plug into `ShipSalvageActions.RollSalvagedComponents`; data cores and space lockboxes wait for slicing (W12).
  - **Materials:** `ore_durelium` (Durelium Ore, in the tier 1 and 2 asteroid tables at weight 4 beside the ore's 30, 12 in the strip tables), `gas_tibanna` (Tibanna Gas, table `GAS_TIBANNA`), `aurodium` (Aurodium), and `ship_scrap_1`-`_5` (Ship Scrap, Tier 1-5), all ESS9 stackable items (base 535, models 222-225) with new illustrated icons. They stay raw: nothing refines Durelium, Tibanna or Aurodium, and what they feed (Hull Frames, Emitters, Reactors, Aurodium Plating) is the W9 component layer, since no Engineering recipe takes materials per family yet. The Refinery turns Ship Scrap of tier N into the tier's electronics the module recipes use (ruined, flawed, good, imperfect, high quality), at Refining N.
  - **Perks:** Gas Siphoning I-II (PerkType 886, `SpaceGasYieldBonus` 1218, trait feat 3355) and Salvage Expert I-III (887, `SpaceSalvageYieldBonus` 1219, feat 3356), at the Bible's ranks and prices; TLK 192894-192897; the feats reuse the Refinery Management and Scavenging icons, as W4f's did.
  - **Recipes:** the two families' 20 recipes are `RecipeType` 7328-7347, numbered after the hulls by `catalog.py` (`LATE_FAMILIES`) so no earlier value moved.
  - **Not built here:** zone placement beyond the Mid Rim nebula (the three new zones), Salvaged Components and Trait Hunter (W9), data cores and space lockboxes (W12), DM "Place Derelict" (W6/W15), and mining XP moving to Gathering.

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
  - **Spoofing:** a slicer aboard (the pilot or the Sensors crew member) can instead try the slicing mini-game at the patrol's tier. Success returns a clean scan. Failure plays out as being caught: fine, confiscation and standing loss.
  - Black markets at Smuggler's Moon, Tatooine and Hutlar buy contraband at a premium.
  - A Smuggling Compartment (Low, Piloting) cuts the scan chance.
- **Risk:**
  - A destroyed ship spills its cargo as containers anyone can scoop for 5 minutes, in PvE and PvP (built in W7a, "Added by the trade core update" below).
  - Cargo above a value threshold can trigger a pirate ambush when the ship enters an orbit or jumps. The chance and squad size scale with cargo value and orbit tier, and every squad includes an interceptor.
- **Freight contracts:**
  - NPC contracts at each exchange: haul a sealed load from A to B by a deadline for a fixed fee and standing.
  - A deposit is forfeited if the load is lost.
- **Factions:** trade switches on the faction service. Czerka (industrial), Republic Military (lawful supply), Hutt Cartel and Smugglers (contraband), Sith Military (Korriban).
  - Standing unlocks better prices and higher-paying contracts.
  - Getting caught smuggling, or raiding convoys, costs lawful standing.
- **City hubs:** a city starport exchange charges a mayor-set tariff paid into the treasury.
- **Earning targets:** measure credits per hour of space combat at each tier first (loot gold plus sold drops), and hold it to the ground's rate at the same tier (reward parity, section 19). Then set:
  - Freight contracts: about 75% of that rate.
  - Open trading: about 100-125%.
  - Contraband: about 150-200%.
  - Price flooding, fees, deposits and cargo loss hold trade to those targets. Exchanges create credits, so this is the inflation guard.
- **Added by the trade core update (W7a):**
  - **Cargo bay:** commodity units on `PlayerShip.Cargo` (`CommodityType` to a `CargoLot` of units and the credits they cost), never items. Older records load empty, so no migration. Capacity is the hull's `Cargo` (`Trade.GetCargoCapacity`, the value the HUD already showed); a ship without a revamp hull has none. The Cargo Expander module family was never built (W3 left it out), so nothing else adds capacity yet. Each unit fills its commodity's volume (1-6). Player text calls it the "cargo bay", apart from W6a's item "Ship Hold" storage. Unregistering a ship with cargo aboard is refused.
  - **Commodities:** 23 (`CommodityType` 1-23, `CommodityDetail`), in the generated `TradeCatalog` (`Feature/TradeDefinition`) and `TradeTuning` from `tools/SpaceRevampBalance/trade.py`, which holds every trade number and its source. Legality comes from `BannedBy` (`TradeAuthority` Republic, Czerka, Sith): Moonspice is contraband (all three ban it); Encrypted Data Chips (Republic, Czerka), Sith Relics and Weapons Crates (Republic) are restricted. Departures from the dock table: Mon Cala also exports Medical Supplies (kolto) and CZ-220 Weapons Crates, since both were imports with no exporter; Smuggler's Moon Landing and Station share one exchange. The orbit rule holds (`trade.py` checks it).
  - **Exchanges:** 9 (`TradeExchangeType`, one per NPC planet, `TradeLookup.GetExchangeForPlanet`). The Dock Master's menu (`StarportDialog`) offers "Trade Exchange" at NPC starports only: player-city starports get theirs with tariffs (W7c), and ship-only locations (the Abandoned Station) have none. The window (`GuiWindowType.TradeExchange` 79, `TradeExchangeDefinition` and view model) trades with one of the player's own ships docked at that dock (its `DockPosition` area is the Dock Master's landing area), in units per order: buy takes what fits and what the player can afford. A lawful exchange trades a good its authority bans only when it makes or needs it; open ports (Hutlar, Tatooine, Smuggler's Moon, Dathomir) trade everything.
  - **Pricing** (`TradePricing`, mirrored in `trade.py`): unit mid price = base x factor x exp(-pressure / depth), the flood multiplier kept within 0.35-2.5. Factors: exporter 0.85; neutral 0.90 + 0.04 per starlane hop from the nearest exporter (at most 1.10); importer 1.05 + 0.04 per hop (at most 1.40); restricted goods x1.10 and contraband x1.20 off the exporter. The exchange sells at mid + 6% and buys at mid - 6%; Social improves both by 0.1% per point over 10 (at most 1.5%). Depth in units = 250,000 / base price. Each unit of an order is priced halfway through it, so splitting an order changes nothing. Selling adds its units to the market's pressure and buying removes them; pressure halves every hour (`TradeExchangeState`, one record per exchange, recovered lazily when read).
  - **Base prices:** the Cost of a comparable item blueprint (`trade.py` `SOURCE`; NPC stores buy items at 40% of Cost); bulk foods and water are crates of 5.
  - **Balance** (`python tools/SpaceRevampBalance/trade.py --sim`, one greedy trader on fresh markets, 3 minutes per starlane hop and 2 per stop, assumed): legal goods only, an XS Freighter (480) earns 12,900 credits in its first hour and 9,100 an hour over four; a Republic Quartermaster (1,200) 19,100 and 10,100; a Barracuda (200) 6,700; a fighter (10) 800. With restricted and contraband goods the freighters reach about 18,000 an hour. Space combat today, from the loot tables at 60 normal kills an hour with drops sold at 40%: T1 4,100, T2 6,300, T3 8,500, T4 7,600, T5 9,200, T6 22,200. Over four hours legal trade sits at 107-119% of T3 combat (the plan's 100-125%); contraband at about 210% before customs, which W7b must bring to 150-200%. Holds above about 500 add little, because flooding, not the hold, caps a trip; big holds matter for freight contracts and holdings output. Buying and selling back at one exchange always loses, and repeated round trips flood the pair until they stop paying (`TradeTests`).
  - **Trade XP:** a sale gives Piloting XP equal to its profit (sale credits minus what the units cost), at most 1,200 (two same-level kills) per sale. Longer routes pay more, so they give more. Scooped cargo counts at its base price.
  - **Cargo spill:** `Space.ApplyDeath` calls `Trade.SpillCargo` on every death, PvE or PvP. The bay empties into cargo pods (`spc_cargo_pod`, Module/utp, appearance 1313 "[mdrn]Cargo Container1"; needs a module repack) placed 4 m around the wreck: one commodity per pod, largest first, at most 6 pods (`CargoSpillRules`). A pod holds units in locals (`CARGO_POD_UNITS_<commodity>`), is named for what it holds, and lasts 300 s; its OnUsed (`cargo_pod_used`, set when it spawns) lets any pilot in space scoop what fits into their cargo bay (`Trade.ScoopPod`), and an empty pod is removed. Pods are not saved, so a restart clears them.
  - **Ship HUD:** the Stats tab has a CARGO section: "Cargo" shows the volume used against the hull's Cargo, then one row per commodity aboard.
  - **Tests:** `TradeTests` (catalog, cargo bay, buy and sell, flooding and recovery, Social, the arbitrage bound, trade XP, spill and scoop, wiring), `ShipHudRulesTests`; engine test `CargoSpillEngineTests` (category CargoSpill) built, not run.
  - **Not built here:** the HoloNet board, contraband and customs scans, pirate ambushes, NPC convoy cargo, freight contracts, faction standing, city tariffs and player-city exchanges, smuggling compartments, black markets (W7b and W7c), the Cargo Expander, and the Trade (Revamp) Bible tab (`trade.trade_tab`, not yet written to the workbook). No client evidence: the exchange window and the pods are unverified in a client.

## 14. Crafting, rewards, and the economy

### Component layer

Crafted intermediates that hull and module recipes consume. Appendix D lists every component, which skill crafts it, and which loot tables drop it.

- **Generic families:** Hull Frame, Ship Reactor, Energy Emitter, Ordnance Assembly, Shield Projector, Thruster Assembly, Sensor Suite, Extraction Head, Life Support System.
- **Manufacturer components:** one per manufacturer (for example Republic Shield Matrix, Sith Targeting Core). They are not crafted. They come from that manufacturer's faction store, its NPC ships' loot and salvage of those wrecks, and more slowly from data sites and aces. They're tradeable, so no character needs faction standing to build a ship.
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

There is no new crafting skill. Engineering keeps building every ship and module, as it does today: the 2026-10-22 crafting logs show 37 characters crafting ships, weapons, ammo or plating in a month, 25 of them at Engineering 40-50 and almost none with Smithery or Fabrication. Crafters still trade, because every hull needs metal components from a smith, and the big hulls need life support from a farmer. Hull Frames, Thruster Assemblies and Ordnance Assemblies also come from salvage, so a solo engineer can still build a ship.

| Skill | Space items |
|---|---|
| Engineering | Final assembly of all 50 ships; every module (weapons and capital weapons, Hull Plating, Resist Plating, shields, repairers, capacitor, electronic warfare, tackle, sensors, Probe Launcher, Data Analyzer, Cloaking Device, Afterburner, Thrusters, Damage Amplifier, Targeting Computer, Cargo Expander, Smuggling Compartment, mining and salvage gear); ammo (missiles, torpedoes, concussion missiles); Hypermatter capsules; enhancements; droids and industrial droid parts; and the electronic components: Ship Reactor, Energy Emitter, Shield Projector, Sensor Suite, Extraction Head, Thorilide Core, Prototype Core |
| Smithery | The metal components: Hull Frame, Ordnance Assembly, Thruster Assembly, Aurodium Plating |
| Agriculture | Life Support System (hydroponics), a component of Light and Heavy Freighters, Cruisers and Command Ships |
| Fabrication | Station Cores and station buildings (Shipyard, Asteroid Drill, Jump Beacon), holding upgrades and Processing Plants, and interior trophies and decor |
| Gathering | Refining: ores, the new materials, and Ship Scrap into electronics (no recipes) |

- **Hull recipes:**
  - Each ship is one Engineering recipe needing a Hull Frame, a Ship Reactor, a Thruster Assembly, its role's component and its manufacturer's component.
  - Advanced ships add one rare component.
  - Large and Capital Advanced ships, and every Capital ship, are assembled at a station Shipyard.
- **Recipe levels (draft):**
  - Standard: the ship's Piloting gate + 5.
  - Advanced: its Advanced gate + 5.
  - Capital Advanced recipes are level 52, the tier-5.5 recipe level, as on the ground.
- **Existing recipes:** today's ship, module and ammo recipes stay in Engineering. Recipe unlocks a player already has are remapped to the new items, never dropped (`CombatUpgradeMigration.md` rule).
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

- The dockhand store sells rank-0 modules, starter Small hulls, Mining Lasers through rank 40 (today's Mining Lasers I-IV, mapped as in section 15), and the lowest-tier missile. Non-crafters can still mine ore and lyase, and Ordnance pilots (launchers are the MGT damage path, section 5) are never stranded without a crafter; today the dockhand sells a launcher but no missiles. Weapons and other modules above rank 0, torpedoes, higher-tier missiles and ammo variants are crafted only.
- Every new item source must be registered for `EconomyObtainabilityCoverageTests` and follow `EconomyRestrictedItems.md`.
- Game text uses ASCII punctuation (`CodingStandards.md`).

## 15. Migration

One release, one migration set (`Migrations.md`): server migrations first, then character migrations, then the saved-data item sweep.

- **Rebuild Token for every character**, regardless of skills or perks.
  - Use `CharacterMigration/RebuildTokenGrant.cs` in a new character migration.
  - There is no forced rebuild. Piloting ranks stay as they are and keep their meaning.
  - Skill rank limits ship first, in their own pull request (`SkillRankLimitsPlan.md`, zunath/SWLOR_NWN#2810). The login notice mentions them, so players can set limits before spending the token.
- **Refund every Piloting perk** as SP: Starships, Defensive Modules, Offensive Modules, Mining Modules, Energy Management, Starship Mining, Intuitive Piloting.
  - Use `CharacterRecalculation.RefundPerk` with a login notice.
  - Remove their feats.
  - Clean `UnlockedPerks`.
- **Login notice:** must say plainly that Piloting now covers every space hull and module, so every piece of gear a pilot can fit today stays fittable; that the old Piloting perks are refunded as SP to spend on the four new styles; that space mining, gas and salvage move to Gathering; and that every character has a free Rebuild Token to move ranks.
- **Items** (inventories, storage, bank, market listings, deeds, installed modules on `PlayerShip`):
  - Converted ships keep their interior: the property keeps the layout it already has (`WorldProperty.Layout`), so no furniture moves or breaks. Only ships built after the release use the new ship's layout.
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
11. Space weapons and damage have no PvP hostility check. Fixed in W2d (section 9).
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
23. The `t3capital` NPC has no `npc_cap3` configuration (`Feature/SpaceObjectDefinition/NPCSpaceObjectDefinition.cs`, `t3capital`), so its defense is 70 against 157 for T2 and 383 for T4.
24. `npc_cap2` has explosive attack 20 and explosive defense 5 (`ShipConfigurationModuleDefinition.cs`), which look swapped: every other boss configuration has explosive defense at half its thermal defense.

## 17. Numbers to draft for approval

Draft each in the Bible, check it with the combat simulator where it affects combat, and get owner approval before code.

- **Ships:** the ship formula; per-role baselines (slots, hull, shield, capacitor, speed, evasion, signature, cargo, interior limits, repair multiplier, built-in bonus); manufacturer modifiers; each ship's signature bonus size; Advanced multipliers; recipe levels; model scales.
- **Components:** recipes and quantities per ship; manufacturer component drop chances; rare component recipes.
- **Size rules:** hit-chance and damage penalty per size step.
- **Modules:** stats per family per rank step; capacitor cost, recast and range; Point-Defense and capital values; size scaling factors.
- **Components:** recipes, variant trait sizes, salvaged trait odds.
- **Perks:** every row for Ace, Ordnance, Bulwark and Command, plus the Gathering space perks.
- **Ship stats that replace rank:** hull Attack, Accuracy, Defense and Evasion per role and size; Attack and Accuracy per weapon step; Defense and Evasion per defensive step.
- **Ground-skill bonuses per rank:** Engineering (repair amount, capacitor transfer, power routing), Espionage (scan strength, jam and paint strength, reveal range) and Leadership (order and aura strength, command range, crew effectiveness).
- **Combat:** STM costs, capacitor and shield regen formulas, crit factors.
- **Activation:** cycle time and capacitor per cycle for every family once the global recast is gone, and the auto-fire loop interval.
- **Crew:** droid crew effectiveness (the best industrial droid at about 75% of a player of matching rank, a tier-1 combat droid far below), industrial part recipes (about half the materials of combat parts), power-routing amounts.
- **Enemies:** space NPC stat presets per role and tier, ambush odds, convoy and patrol sizes.
- **Trade:**
  - Commodity base prices, flooding and recovery rates, exchange fee.
  - Freight deposits and pay, contraband scan odds and fines, tariff cap.
  - XP per delivery, standing gains.
  - The measured combat baseline and the resulting targets.
- **Repairs:** cost per hull size.
- **Refit:** fee.
- **Ship transfer:** fee by hull size.
- **Dockhand store:** prices for its stock (section 14).
- **Wild Space:** environment effect sizes, rift lifetimes, mass budgets and collapse damage, Rift Raider presets, loot tables.
- **Scanning:** signal gained per cycle, spawn rates per signature type, Sensor Sweep range and cooldown, Jump to Signature spool.
- **Stations:** Station Core recipe, Shipyard fee cap, anchor site count per area, Asteroid Drill output, Shipyard build times, Jump Beacon fuel cost and hull caps.
- **Holdings:** price, upkeep, output, upgrade costs, Processing Plant recipes, event odds, droid boost.
- **Droids:** Industry per industrial part, stat cuts, instruction effects, derelict salvage team timer and yield.
- **Slicing:** Data Analyzer range, slicing tiers per zone for data cans, locked caches, data cores and space lockboxes, space lockbox drop rates, customs spoof tier per patrol, space slicing XP and reward odds.
- **Activities:** agent mission pay and token prices, incursion schedule and rewards, Rift Run timers and loot, cloak speed penalty and reveal range.

## 18. Workstreams and order

Everything merges to one integration branch and releases together.

- **Lore review first:** before W0, the DMs approve the Wild Space faction and system names, all 50 ship names and the faction agent setup. Until then, every faction, system and ship name in this plan is a working name.
- **Branching:**
  - Create `feature/space-revamp` from up-to-date `origin/master`.
  - Each workstream slice goes on its own branch and pull request into it, with the Codex review loop (`PullRequests.md`).
  - **Bible edits happen on one branch only**, because separate branches that edit the `.xlsx` cannot be merged. Make them on the integration branch, one at a time.
  - The plan branch (`claude/festive-edison-bbnnxi`) already edits the Bible (the proposed space perk tabs). Start the integration branch from it, or merge it first. If master's Bible changes in the meantime, redo the tabs on the new base with the same rows instead of merging the binary.
  - Merge `origin/master` into the integration branch regularly.
  - The final pull request goes to `master`.

| # | Workstream | Contents | Needs |
|---|---|---|---|
| W0 | Bible drafts | Piloting (Revamp) perk tab with four styles (drafted as Proposed on the plan branch); Starships tab rewrite (hulls, modules, components, traits); space enemy sections; trade tab (commodities, prices) | Owner approval gates W2-W9 numbers |
| W1 | Foundations | Piloting gating and seat skills, space `StatType`s, resistance types, High/Mid/Low and slot tags in `ShipStatus`, removing configs, size x role data model, `RequiresSkill` on space items | - |
| W2 | Combat rules | Shared crit, resist, status and threat; attribute mapping; regen; toggle and auto-fire loop; Ship HUD; PvP; escape pods; repair bills | W1 |
| W3 | Ships and modules | 50 ships (Appendix C) with model scales, Hull Designs and Refit, about 20 module families x rank steps, size scaling, icons with lit variants, store, component inputs for the existing Engineering recipes, ship transfers and market sales | W1, W0 |
| W4 | Perks and abilities | Four Piloting styles, Gathering space perks, space abilities and IDs | W1, W2, W0 |
| W5 | Enemies | NPC conversion, role AI, new groups, aces, encounters, spawn tables, DM Space tab | W2, W3 |
| W6 | World | Salvage and wrecks, gas harvesting, new materials, 3 zones (Module JSON), station flags, starlanes, docking limits | W3 |
| W7 | Trade | Cargo data, exchange NUI, pricing service, HoloNet board, contraband and customs, pirates, freight contracts, factions, city tariffs, trade XP | W3, W5, W6 |
| W8 | Crew | Stations, consoles, `AttachCamera` and hidden crew, droid crew, permissions, power routing, Command crew bonuses, Engineering, Espionage and Leadership seat bonuses, ship-to-ship docking and the Dock permission | W2, W3, W4 |
| W9 | Crafting and rewards | Component layer and traits, recipes, research blueprints, named modules, Design Schematics, trophies and decor, ammo variants, achievements | W3, W6 |
| W10 | Migration | Rebuild Token, perk refunds, item and ship conversion, stamp cleanup, design unlocks, notices | W1, W3, W9 |
| W12 | Wild Space and exploration | 6 Wild Space areas and environments, rifts, Rift Raiders, Probe Launcher, signatures, Jump to Signature, Sensor Sweep, data/cache/gas/combat sites, Scanner HUD tab, space slicing (Data Analyzer, new slicing sources, space reward tables, customs spoofing) | W2, W3, W5 |
| W13 | Stations | Station city layout, Station Core, anchor sites, deck areas, Shipyard, Asteroid Drill, Jump Beacons, Station Fuel, passenger shuttle route | W3, W7 |
| W14 | Holdings and droids | Holdings terminal, types, upgrades, production chains, events, ground sites; industrial droid parts, Industry stat, AI instructions, droid bays | W7, W9 |
| W15 | Activities | Derelict boarding (including contested derelicts and droid salvage teams), faction agents and token stores, incursions, Rift Runs | W5, W7, W12 |
| W16 | Cloaking | Cloaking Device, reveal rules, stealth unblock in space | W2, W12 |
| W11 | Release | Balance pass, client evidence, docs (section 19), player release notes | All |

**Suggested order:** W0 and W1 first, then W2 and W3, then W4-W9 and W12-W16 in parallel slices, then W10, then W11.

**W0 status:** the combat numbers (hulls, the 50 ships, modules, size rules, space enemies) are in the Bible's Starships, Ship Modules, Space Enemies and Space Balance (Revamp) tabs, generated and simulator-checked by `tools/SpaceRevampBalance`. Trade, component and economy numbers are drafted with their own workstreams (W7, W9), since nothing else depends on them.

## 19. Verification

- **Build and tests:**
  - Build once with `-p:RunPostBuildEvent=Never -v q -clp:ErrorsOnly`.
  - Run focused NUnit filters for each slice.
  - Add tests for: rank gating, size rules, slot rows, migration (refunds, item mapping, ship rebuild), seat skill checks (Piloting, Engineering, Espionage) and the Engineering, Espionage and Leadership bonuses, Piloting rank changing no combat stat, trade pricing and flooding, cargo spill, customs, XP split and range, PvP gating, escape pods, every auto-off trigger, support-module target locks and Retarget, All Weapons, space slicing (tier gate, Sensors crew slicing, Espionage XP, customs spoof outcomes), mining yield into ship storage (and the overflow to inventory), Shipyard access and fee, ship transfers and market sales (every structure re-owned, blocking conditions, designs not shared, permissions reset, migrated interiors kept).
- **Engine tests** (`EngineTesting.md`) for:
  - Entering space with Mid slots.
  - Crew boarding, camera attach and re-attach after a starlane jump.
  - Droid crew.
  - Wrecks and salvage.
  - The saved-data item sweep.
- **Reward parity:** before release, credits per hour and loot value from space content at each tier must land within about 15% of ground content at the same tier. Measure both with the same method and record the results.
- **Combat simulator:** extend `SWLOR.CombatSim` with the ship model (the rules are shared). Report seeded results at entry, middle and endgame ranks for:
  - Damage pilot vs tank vs support.
  - Small vs Large vs Capital under the size rules.
  - A crewed capital vs the same pilots in separate ships.
  - The solo target at every tier: a pilot at that tier's rank with one weapon style (60 SP) solos all normal content and most elites in any hull their rank allows, sustained by shield regen, rank-0 repairers and consumables. Bulwark, Command and the seat skills are never required.
  - The Advanced ships in each role: each lands within 10% of the others at that role's job.
  - A melee build (MGT, PER) on launchers vs a ranged build (PER, AGI) on energy weapons at equal ranks.
  - **Fight length:** a matched solo pilot (same rank and attributes as the ground run) kills every normal space role at each tier within the ground's normal-enemy time at that tier, measured with the same simulator settings. No normal role takes more than 1.5x as long as the fastest one; on the ground, Melee, Ranged and Controller normals land within about 10% of each other. Capitals are compared with ground elites and bosses at the same tier.
- **Fight-length baseline (today, measured 2026-10-22):** ground from `SWLOR.CombatSim` (a longsword autoattacking a same-level Normal Melee enemy from the Bible's Enemy Stat Presets, attributes 16 + 0.2 x rank, 300 runs); space from a model of today's rules (the shared hit and damage formulas, the 2 s global recast, per-module recasts, shield before hull, halved ion overflow, NPC shield regen and repairers, real player ships, modules and configs, real NPC pilots and loadouts, perfect play, the NPC not fighting back). Medians:

| Tier (rank) | Ground, autoattacks | Ground, with abilities (rank 10-50) | Space fighter / interceptor | Space gunship |
|---|---|---|---|---|
| T1 (5) | 18 s | 19 s | 22 / 26 s | 175 s |
| T2 (15) | 34 s | 25 s | 28 / 30 s | 85 s |
| T3 (25) | 47 s | 25 s | 32 / 34 s | 75 s |
| T4 (35) | 47 s | 25 s | 28 / 30 s | 59 s |
| T5 (45) | 51 s | 29 s | 36 / 38 s | 83 s |

- A T5 platform takes 90 s and a T5 capital about 340 s solo. Fighters and interceptors are 85% of space spawns; gunships, cargo haulers and platforms about 13%.
- Today's slow fights come from two rules this plan replaces: the 2 s global recast caps a ship at one shot per 2 s however many weapons it carries, and gunships, platforms and cargo haulers regain 1 shield per second (fighters 1 per 6 s), some with repairers on top.
- **Client evidence** (`InEngineVisualVerification.md`) for:
  - The Mid hotbar row.
  - Lit and unlit module icons, and a module switching itself off.
  - The Ship HUD with a support module retargeted, and its Stats tab.
  - A signature scanned from 0% to 100%, then Jump to Signature.
  - A rift crossing into Wild Space, an environment effect, and a collapse ejection.
  - Founding a station at an anchor site, docking, and a Jump Beacon jump.
  - A holding's ground site, an event fight, and cargo pickup.
  - An industrial droid in a droid bay and at a crew station.
  - A ship transfer and a market ship sale, with the interior intact.
  - Boarding a derelict, a contested derelict's waves, a droid salvage team, a data site slicing game, a customs spoof, a wreck data core, a space lockbox, a DM-spawned squad, an agent mission, a Rift Run timer, and cloak and reveal.
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
- Rift Raider ships, if existing pirate models and the unused hak models (for example the Alien set, 10215-10217) don't give the coalition and its 2 refit hulls their own look.

## 21. Out of scope

- Any ground ability in space, or any space ability on the ground.
- Fuel for ordinary flight. Station Fuel is spent only by Jump Beacons (section 25).
- Capital jump drives, and jumping to hyperspace from anywhere (hyperdrive cooldowns, emergency or random jumps). Jump Beacons (section 25) and rifts (section 23) are the only travel beyond starlanes, so interdiction, pirate ambushes and trade risk keep working.
- Hostile boarding of player ships (a later pass). Ship-to-ship docking is friendly only (section 10).
- Interior Hull Designs (a later pass; section 7).
- Tier 6 content and the reserved ores (Ochne, Croknor, Hemorgite, Bisteiss). Wild Space is tier 5.5.
- Station raids and the Defense Grid building (a later pass).
- A Capital Industrial role (a later pass). The proposal: convert the Czerka Armored Transport and the Hutt Corvette into capital industrials that refine ore aboard, carry a large ore hold and droid bays, and boost nearby miners' yield. A passive that mines several asteroids at once was also suggested.
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

## 24. Scanning, exploration and slicing

- **Probe Launcher** (Mid slot, Piloting, or Espionage at the Sensors seat; toggle): each cycle adds signal strength to every hidden signature in the area, system-wide.
- **Scan strength** grows with the Espionage rank of whoever runs the launcher (the pilot or the Sensors officer), a droid running Survey Protocol (section 27) and the system's environment.
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
  - **Data sites:** the **Data Analyzer** runs the existing slicing mini-game (`Service/SlicingService/`) on data cans. They yield blueprints, recipe unlocks and agent intel, which unlocks special agent missions.
  - **Cache sites:** hidden pirate caches. The Salvager (Gathering) works the open containers for prototype tech and components. Locked caches hold the site's best loot and need slicing.
  - **Gas sites:** the Gas Harvester.
  - **Derelicts:** section 28.
- **Despawning:** sites despawn once completed or after a timer.

### Slicing in space

Espionage's Slicing perk works in space exactly as on the ground.

- **Data Analyzer:** a Mid-slot Espionage module (Engineering crafts it). Any ship can fit it, with no rank requirement. It is a one-press module that starts the mini-game on a target in range; range is a number to draft (section 17) and replaces the 5 m terminal range for space targets.
- **Who slices:** the pilot, or the crew member at the Sensors station with their own Slicing perk (section 10).
- **Rules kept from the ground:** `SlicingSession.ValidateStart` still requires a Slicing tier at or above the target's tier, and no slicing starts in combat. Wild Space targets are tier 5.
- **New sources:** add `SlicingSourceType` values for data cans, locked caches, wreck data cores and customs scans.
- **Targets:**
  - Data cans at data sites.
  - Locked caches at cache sites.
  - Data cores in elite, ace and boss wrecks (section 12).
  - Terminals and lockboxes inside derelicts (section 28).
  - Customs scans (section 13).
  - Data-heist missions for faction agents (section 29).
  - Space lockboxes dropped by NPC ships (section 12).
- **XP:** Espionage XP through `Skill.CalculateEspionageXP(PerkType.Slicing, tier, rank)`, never Piloting XP.
- **Rewards:** new space reward tables in `SlicingRewardCatalog` for each new source: Design Schematics, recipe unlocks, Ship Schematics, agent intel and components with rare traits. Ground lockbox and terminal rewards stay as they are. New items follow `EconomyRestrictedItems.md`.

## 25. Player stations

A station is a **player city in space** and follows every city mechanic, in `Service/Property.cs` and the city layouts:
- a mayor, who needs City Management
- citizens, with one citizenship per player across stations and planet cities
- elections every three weeks
- taxes, weekly upkeep and loss after four weeks unpaid
- city level by citizen count
- upgrades

**Founding**
- A **Station Core**, crafted with Fabrication, takes the place of the City Hall item. It is used at an **anchor site**: 2 per orbit and 1-2 per Wild Space system, one station per site.
- Founding creates two things:
  - the station exterior at the site, using the existing `[mdrn]SpaceStation` placeable scaled by city level
  - the station's own interior deck area, which is the city's land. The City Hall sits inside it.
- Add a station city layout beside `CityLayoutDefinition`, with the same building and structure limits.

**Docking and buildings**
- **Docking:** through the Starport building, as in planet cities: any starport takes Large hulls, and Capital hulls need starport upgrade level 2.
- **Passenger shuttle:** every station's Starport has a flights terminal on the existing shuttle network, with a route to its orbit's NPC dock and the city's transport tax as in planet cities. Today `StarportFlightsDialog` and `Shuttle.TryPurchaseTicket` route between `PlanetType` values, so stations need their own origin and destination entries. Non-pilots can visit, live and roleplay on a station, so stations never become pilot-only hubs.
- **City buildings:** all of them: City Hall, Starport (with the Trade Exchange and its tariff), Medical Center (respawn), Bank, Cantina.
- **Station-only buildings:**
  - **Shipyard:** Capital hull recipes can only be crafted at a Shipyard. A droid assigned to it shortens builds.
    - **Access:** the mayor decides who can build there: citizens only (the default) or anyone. The mayor can also set an optional Shipyard fee paid into the treasury, added as a new `PropertyTaxType` beside Citizenship and Transportation and edited with the existing `EditTaxes` permission. Several stations can each have a Shipyard, so the first one can't lock out everyone else.
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
- **Cost:** industrial parts cost about half the materials of combat parts at the same tier, so a crew droid is affordable while a top-tier one is still an investment.
- **Industry rating:** a droid's Industry is the sum of its parts. Mixing combat and industrial parts gives a middling droid at both.
- **Industry pays off in five places:**
  - **Planetary Holdings:** an output boost.
  - **Ship droid crew** (section 10): station effectiveness comes from Industry, so a combat droid makes poor crew.
  - **Droid bays:** Mining Barge, Industrial and Logistics hulls get one, using the unused `ShipDetail.HasDroidBay`. A droid riding in the bay boosts mining, gas and salvage yield, or Logistics repair output.
  - **Station buildings:** faster Shipyard builds, more Asteroid Drill output.
  - **Derelict salvage teams** (section 28): a faster timer and a better yield.
- **Droid limit:** working droids don't count toward the one-droid limit (section 10).
- **New AI instructions,** which compete with combat instructions for AI slots:
  - **Efficiency Protocol:** holding, drill and droid-bay output.
  - **Damage Control Protocol:** Engineer station and Logistics bay repairs.
  - **Survey Protocol:** scan strength at the Sensors station.
  - **Prospecting Protocol:** the rare-find chance on mining and salvage.

## 28. Derelict boarding

- **Finding them:** derelicts come from scanning (section 24) and Wild Space.
- **Boarding:** a Small or Medium ship docks with the derelict and the party boards on foot, reusing the ship-to-ship docking flow (section 6, "Added by the docking update").
- **Inside:** an instanced copy of a derelict template: existing starship interior layouts dressed as wrecks, plus a few new ones. It is a ground dungeon at the system's tier, with ground enemies, ground loot, slicing terminals and lockboxes at the system's tier, and ground abilities. Each mode keeps its own abilities.
- **Safety:** the docked ship can't be attacked while its crew is aboard the derelict. Leaving returns everyone to the ship.
- **Contested derelicts:** some derelicts are contested.
  - While the boarding party is inside, enemy fighters attack in waves around the derelict, and pilots who stay outside defend it. The docked ship stays safe.
  - Each wave cleared raises the boarders' loot.
  - If no defending ship is left, enemy boarders reinforce the derelict's interior.
  - It reuses combat-site waves (section 24) and the derelict dungeon, and gives a mixed space and ground group one shared goal.
- **Droid salvage teams:** instead of boarding, a docked ship can send its working droids (station crew or droid-bay droids, section 27) to strip the derelict on a timer while the pilot holds off waves outside, using the contested-derelict waves. The droids' Industry rating sets the timer and the yield. It pays less than boarding on foot and gives solo pilots and small groups a way in.

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

- **Missions:** kill, sealed courier, mining quota, convoy escort, scan-down and data heist (slice a target data can inside an enemy-held site), scaled to the dock's tier. Higher standing unlocks harder missions.
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

- **Ported, not invented:** every row ports an existing ground perk (Source column) with its numbers, durations, STM and cooldown. Only the targeting changes: abilities fire your ship's weapons, and allies are party ships. Unlock ranks follow the Piloting schedule, so a row can unlock at a different rank than its source. A row marked **New** has no ground match.
- **Durations:** buffs and debuffs last 30 s and capstones 45 s, as on the ground.
- **Activation:** every space ability has a 0 s activation delay, because movement cancels a cast (`UsePerkFeat.cs`). No channels.
- **One impact:** an ability fires one weapon unless it says otherwise, so its damage never scales with how many weapons a ship carries. **Weapon DMG** is the DMG of the fitted weapon it fires.
- **Areas:** "near your ship" and "near the target" use the source's radius scaled to space distances; radii are drafted in the Bible (section 17). **Command range** is 30 m base (ground Leadership uses 5 m).
- **Allies:** party ships. Crew must be in the captain's party, so they count.
- **Threat follows the ground enmity rules** (`Service/Enmity.cs`):
  - Space damage goes through `Enmity.CalculateDamageEnmity`, so `DamageEnmityPercentAdjustment` (the tank stance's +100%) applies. Today modules call `ModifyEnmity` with raw damage and skip it.
  - Repairs generate heal enmity like ground healing: half the repair, to enemies holding the repaired ship. Today repairs generate none.
  - Goads use `Enmity.TryTaunt`: threat catches up to the highest visible rival plus a bonus, once, with no target lock.
  - Flat bursts use `ModifyEnmity`. Percent modifiers (stances, statuses) stack to the existing +50% clamp.
- **Statuses:** ground statuses apply to ships through the shared status system: Burn, Blind, Dazed, Exposed, Disarming Shot, Weakened and fragmentation. The hard crowd-control rules apply to Dazed.
- **Stats:** the space formulas must read the existing Attack, Accuracy, Defense and Evasion percent stats, which `Space.cs` ignores today.
- **Where perks apply:** a perk works wherever its condition is met, on any hull and from any station (cross-skill rule in `SkillPerkStandards.md`).

### Piloting: Ace (PER primary, AGI secondary)

Energy weapons and evasion, ported from Pistol (Skirmisher and Gambler) and Rifle.

| Rank | SP | Perk | Type | STM | Cooldown | Effect | Source |
|---|---|---|---|---|---|---|---|
| 2 | 2 | Disabling Shot I | Combat | 3 | 18 s | Fire one energy weapon at your target for weapon DMG + 8 and inflict Disarming Shot for 30 s, reducing its Attack by 10%. | Disarming Shot I (Pistol) |
| 5 | 2 | Mobile Footwork | Trait | - | - | After using a hostile ability, gain +8% Evasion for 30 s. | Mobile Footwork (Pistol) |
| 8 | 2 | Sustained Fire I | Trait | - | - | Each consecutive energy-weapon hit against the same target grants +1 DMG, up to five stacks. Stacks expire after 30 s. | Sustained Fire I (Rifle) |
| 10 | 2 | Double Shot I | Combat | 4 | 24 s | Fire one energy weapon twice at your target for weapon DMG + 6 each. If either shot critically hits, restore 2 STM. | Double Shot I (Pistol) |
| 12 | 2 | Disabling Shot II | Combat | 5 | 18 s | Fire one energy weapon at your target for weapon DMG + 18 and inflict Disarming Shot for 30 s, reducing its Attack by 12%. | Disarming Shot II (Pistol) |
| 15 | 4 | Kiting Instinct | Trait | - | - | When you move after using a hostile ability, gain +10% Movement Speed for 30 s. | Kiting Instinct (Pistol) |
| 18 | 3 | Point Blank Burst I | Combat | 6 | 30 s | Deal weapon DMG + 16 to enemies near your ship and gain +10% Evasion for 30 s. | Point Blank Burst I (Pistol) |
| 20 | 4 | Ace Stance | Stance | - | 30 s | While active, gain +15% Evasion and +10% Movement Speed, but your Attack is reduced by 10%. | Skirmisher Stance (Pistol) |
| 22 | 4 | Sustained Fire II | Trait | - | - | Each consecutive energy-weapon hit against the same target grants +2 DMG, up to five stacks. Stacks expire after 30 s. | Sustained Fire II (Rifle) |
| 25 | 4 | High Noon | Trait | - | - | Critical hits against targets above 70% hull deal +15% damage. | High Noon (Pistol) |
| 28 | 3 | Disabling Shot III | Combat | 8 | 18 s | Fire one energy weapon at your target for weapon DMG + 28 and inflict Disarming Shot for 30 s, reducing its Attack by 15%. | Disarming Shot III (Pistol) |
| 30 | 4 | Double Shot II | Combat | 8 | 24 s | Fire one energy weapon twice at your target for weapon DMG + 13 each. If either shot critically hits, restore 3 STM. | Double Shot II (Pistol) |
| 32 | 3 | Point Blank Burst II | Combat | 9 | 30 s | Deal weapon DMG + 30 to enemies near your ship and gain +15% Evasion for 30 s. | Point Blank Burst II (Pistol) |
| 35 | 2 | Evasive Reload | Trait | - | - | After you evade an attack, your next hostile ability within 30 s costs 3 less STM. | Evasive Reload (Pistol) |
| 38 | 4 | Double Shot III | Combat | 10 | 24 s | Fire one energy weapon twice at your target for weapon DMG + 20 each. If either shot critically hits, restore 4 STM. | Double Shot III (Pistol) |
| 40 | 5 | Disabling Shot IV | Combat | 12 | 18 s | Fire one energy weapon at your target for weapon DMG + 38 and inflict Disarming Shot for 30 s, reducing its Attack by 18%. | Disarming Shot IV (Pistol) |
| 45 | 4 | Sustained Fire III | Trait | - | - | Each consecutive energy-weapon hit against the same target grants +3 DMG, up to five stacks. Stacks expire after 30 s. | Sustained Fire III (Rifle) |
| 50 | 6 | Ace in the Hole | Capstone | 15 | 90 s | Fire one energy weapon at your target for weapon DMG + 30. If it critically hits, your next three weapon cycles have +20% Critical Rate and no extra delay. | Dead Man's Hand (Pistol) |

### Piloting: Ordnance (MGT primary, PER secondary)

Missiles and torpedoes as area ordnance, ported from Throwing (Ordnance).

| Rank | SP | Perk | Type | STM | Cooldown | Effect | Source |
|---|---|---|---|---|---|---|---|
| 2 | 2 | Volley I | Combat | 3 | 18 s | Fire a missile at your target that deals weapon DMG + 7 to enemies near it and inflicts Burn for 30 s. | Explosive Toss I (Throwing) |
| 5 | 2 | Proximity Warheads | Trait | - | - | Launcher hits have a 15% chance to deal 8 damage to the target and up to four other enemies near it. This splash cannot trigger other on-hit effects. | Payload Pouch (Throwing) |
| 8 | 2 | Shrapnel Warheads I | Trait | - | - | Area launcher abilities inflict fragmentation for 3 damage every 3 s for 30 s. | Shrapnel Casing I (Throwing) |
| 10 | 2 | Flare Missile I | Combat | 4 | 24 s | Fire a launcher at your target for weapon DMG + 12 and inflict Blind for 15 s. | Flash Toss I (Throwing) |
| 12 | 2 | Volley II | Combat | 5 | 18 s | Fire a missile at your target that deals weapon DMG + 15 to enemies near it and inflicts Burn for 30 s. | Explosive Toss II (Throwing) |
| 15 | 4 | Cluster Warheads | Trait | - | - | Area launcher abilities hit one additional enemy near the target for +10 DMG. | Cluster Pouch (Throwing) |
| 18 | 3 | Concussion Barrage I | Combat | 6 | 30 s | Deal weapon DMG + 16 to up to five enemies near your ship and inflict Dazed for 15 s. | Concussive Toss I (Throwing) |
| 20 | 4 | Ordnance Stance | Stance | - | 30 s | While active, area launcher abilities deal +10% damage, but single-target launcher damage is reduced by 10%. | Ordnance Stance (Throwing) |
| 22 | 4 | Shrapnel Warheads II | Trait | - | - | Area launcher abilities inflict fragmentation for 5 damage every 3 s for 30 s. | Shrapnel Casing II (Throwing) |
| 25 | 4 | Saturation Fire | Trait | - | - | Targets hit by two or more area launcher abilities within 30 s suffer Exposed for 30 s. | Saturation Toss (Throwing) |
| 28 | 3 | Volley III | Combat | 8 | 18 s | Fire a missile at your target that deals weapon DMG + 24 to enemies near it and inflicts Burn for 30 s. | Explosive Toss III (Throwing) |
| 30 | 4 | Flare Missile II | Combat | 8 | 24 s | Fire a launcher at your target for weapon DMG + 24 and inflict Blind for 15 s. | Flash Toss II (Throwing) |
| 32 | 3 | Concussion Barrage II | Combat | 9 | 30 s | Deal weapon DMG + 30 to up to five enemies near your ship and inflict Dazed for 15 s. | Concussive Toss II (Throwing) |
| 35 | 2 | Bombardier's Rhythm | Trait | - | - | After a launcher ability hits two or more enemies, restore 4 STM. | Bombardier's Rhythm (Throwing) |
| 38 | 4 | Flare Missile III | Combat | 10 | 24 s | Fire a launcher at your target for weapon DMG + 36 and inflict Blind for 15 s. | Flash Toss III (Throwing) |
| 40 | 5 | Volley IV | Combat | 12 | 18 s | Fire a missile at your target that deals weapon DMG + 32 to enemies near it and inflicts Burn for 30 s. | Explosive Toss IV (Throwing) |
| 45 | 4 | Shrapnel Warheads III | Trait | - | - | Area launcher abilities inflict fragmentation for 7 damage every 3 s for 30 s. | Shrapnel Casing III (Throwing) |
| 50 | 6 | Saturation Strike | Capstone | 15 | 90 s | Deal weapon DMG + 20 to enemies near your ship. For 45 s, area launcher abilities leave fragmentation zones that deal 8 damage every 6 s. | Rain of Steel (Throwing) |

### Piloting: Bulwark (VIT primary, WIL secondary)

Tank, ported from Vibroblade (Bulwark), Heavy Vibroblade (Immortal) and Armor's Provoke.

| Rank | SP | Perk | Type | STM | Cooldown | Effect | Source |
|---|---|---|---|---|---|---|---|
| 2 | 2 | Shield Ram I | Combat | 3 | 20 s | On your next weapon hit, deal weapon DMG and an additional instance of damage equal to 4% of your Defense. | Shield Bash I (Vibroblade) |
| 5 | 2 | Anger Strike | Trait | - | - | Defiance hits generate +150 enmity, and your next attack within 30 s after using Defiance deals +12 DMG. | Anger Strike (Heavy Vibroblade) |
| 8 | 2 | Bulwark I | Trait | - | - | Gain +8% Defense. | Bulwark I (Vibroblade; its Shield Deflection has no space equivalent) |
| 10 | 2 | Defiance I | Combat | 4 | 18 s | Your next weapon hit deals weapon DMG + 10 and generates +350 enmity plus the damage dealt as enmity. You gain +10% Defense for 30 s. | Fortress Strike I (Heavy Vibroblade; unlocks at 2 there) |
| 12 | 2 | Shield Ram II | Combat | 5 | 20 s | On your next weapon hit, deal weapon DMG and an additional instance of damage equal to 6% of your Defense. | Shield Bash II (Vibroblade) |
| 15 | 4 | Fortification | Trait | - | - | Gain +15 Mind, Trauma and Mobility Resistance. | Fortification (Vibroblade) |
| 18 | 3 | Hail I | Combat | - | 6 s | Goad your target into attacking you. Enmity generated increases by 1% per VIT. On a valid hit, your threat catches up to the highest visible rival plus the VIT-scaled bonus. Repeated goads while already ahead add nothing. No forced target lock. | Provoke I (Armor; unlocks at 5 there) |
| 20 | 4 | Bulwark Stance | Stance | - | 30 s | While active, gain +30% enmity generation and +20% Defense, but your Attack is reduced by 20%. You generate +100% threat from damage dealt, before general enmity modifiers. | Defensive Stance (Vibroblade) |
| 22 | 4 | Bulwark II | Trait | - | - | Gain +10% Defense. | Bulwark II (Vibroblade) |
| 25 | 4 | Critical Ward | Trait | - | - | If you would receive a critical hit, it becomes a normal hit dealing minimum damage. Once every 16 s, reduced by 0.5 s per MGT to a minimum of 12 s. | Critical Ward (Heavy Vibroblade; unlocks at 35 there) |
| 28 | 3 | Shield Ram III | Combat | 6 | 20 s | On your next weapon hit, deal weapon DMG and an additional instance of damage equal to 8% of your Defense. | Shield Bash III (Vibroblade) |
| 30 | 4 | Defiance II | Combat | 7 | 18 s | Your next weapon hit deals weapon DMG + 20 and generates +450 enmity plus the damage dealt as enmity. You gain +20% Defense for 30 s. | Fortress Strike II (Heavy Vibroblade; unlocks at 12 there) |
| 32 | 3 | Hail II | Combat | - | 12 s | Goad your target and all other enemies near it into attacking you, with the same catch-up rule as Hail I. No forced target lock. | Provoke II (Armor; unlocks at 15 there) |
| 35 | 2 | Unbreakable | Trait | - | - | When reduced below 25% hull, gain +40% Defense for 30 s. Once per 3 minutes. | Unbreakable (Vibroblade) |
| 38 | 4 | Defiance III | Combat | 10 | 18 s | Your next weapon hit deals weapon DMG + 30 and generates +550 enmity plus the damage dealt as enmity. You gain +30% Defense for 30 s. | Fortress Strike III (Heavy Vibroblade; unlocks at 30 there) |
| 40 | 5 | Shield Ram IV | Combat | 8 | 20 s | On your next weapon hit, deal weapon DMG and an additional instance of damage equal to 10% of your Defense. | Shield Bash IV (Vibroblade) |
| 45 | 4 | Bulwark III | Trait | - | - | Gain +12% Defense. | Bulwark III (Vibroblade) |
| 50 | 6 | Invincible | Capstone | 15 | 90 s | For 45 s, you take 50% less damage and are immune to Knockdown and Dazed. | Invincible (Vibroblade) |

### Piloting: Command (SOC primary, WIL secondary)

Orders and repairs, ported from Leadership (Vanguard Command and Field Steward) and First Aid. Leadership rank also raises the strength and range of Command's orders and auras (section 10); that scaling is new, since the ground scales Leadership only by SOC.

| Rank | SP | Perk | Type | STM | Cooldown | Effect | Source |
|---|---|---|---|---|---|---|---|
| 2 | 2 | Emergency Repair I | Combat | 4 | 10.5 s | Repair an allied ship within command range for a flat amount plus 2% of its max hull plus WIL scaling. The flat amount is drafted for ship hull in the Bible. | Med Kit I (First Aid) |
| 5 | 2 | Mark Target I | Trait | - | - | When your hostile ability damages or debuffs an enemy, party ships within command range gain +8% damage for 30 s. SOC scaling can raise this to +10%. Once per ability use, at most once every 45 s. | Mark Target I (Leadership) |
| 8 | 2 | Repair Efficiency I | Trait | - | - | Repairs from your modules and abilities are 5% stronger. | Medical Injector Rig I (First Aid) |
| 10 | 2 | Focus Fire I | Combat | 4 | 24 s | Party ships within command range deal +6% damage for 30 s. SOC scaling can raise this to +8%. | Press the Attack I (Leadership) |
| 12 | 2 | Emergency Repair II | Combat | 6 | 10.5 s | Repair an allied ship within command range for a flat amount plus 3.5% of its max hull plus WIL scaling. | Med Kit II (First Aid) |
| 15 | 4 | Crew Morale | Trait | - | - | Crew and droid crew aboard your ship work their stations at +10% effectiveness. | New (crew stations exist only in space) |
| 18 | 3 | Break Formation I | Combat | 7 | 45 s | Enemies within command range suffer -10% Accuracy for 30 s. SOC scaling can raise the penalty to 12%. | Break Morale I (Leadership); Accuracy as Smoke Bomb, because ship weapons roll Accuracy, not ability hit chance |
| 20 | 4 | Formation | Aura | - | 30 s | Aura: party ships within command range take 6% less damage. SOC scaling can raise this to 7%. An aura, not a stance, so it doesn't block a stance. | Watchful Presence II (Leadership) |
| 22 | 4 | Repair Efficiency II | Trait | - | - | Repairs from your modules and abilities are 10% stronger. | Medical Injector Rig II (First Aid) |
| 25 | 4 | Mark Target II | Trait | - | - | When your hostile ability damages or debuffs an enemy, party ships within command range gain +12% damage and +10% Accuracy for 30 s. SOC scaling can raise these to +15% and +12%. Once per ability use, at most once every 45 s. | Mark Target II (Leadership) |
| 28 | 3 | Emergency Repair III | Combat | 7 | 10.5 s | Repair an allied ship within command range for a flat amount plus 5% of its max hull plus WIL scaling. | Med Kit III (First Aid) |
| 30 | 4 | Focus Fire II | Combat | 6 | 24 s | Party ships within command range deal +8% damage for 30 s. SOC scaling can raise this to +10%. | Press the Attack II (Leadership) |
| 32 | 3 | Break Formation II | Combat | 9 | 45 s | Enemies within command range suffer -15% Accuracy and Weakened (-12% Attack) for 30 s. SOC scaling can raise these to 18% and 15%. | Break Morale II (Leadership) |
| 35 | 2 | Command Presence | Trait | - | - | Your command auras and area orders reach further, and non-capstone orders last 4 s longer. SOC scaling can raise the duration bonus to +5 s. | Command Presence II (Leadership); the range bonus is drafted for space |
| 38 | 4 | Focus Fire III | Combat | 9 | 30 s | Party ships within command range deal +10% damage and gain +5% Accuracy for 30 s. SOC scaling can raise these to +12% and +7%. | Press the Attack III (Leadership) |
| 40 | 5 | Emergency Repair IV | Combat | 8 | 10.5 s | Repair an allied ship within command range for a flat amount plus 6% of its max hull plus WIL scaling. | Med Kit IV (First Aid) |
| 45 | 4 | Repair Efficiency III | Trait | - | - | Repairs from your modules and abilities are 15% stronger. | New (the Medical Injector Rig ladder has two ranks) |
| 50 | 6 | Fleet Action | Capstone | 15 | 90 s | For 45 s, party ships within command range, yours included, gain +12% damage, +6% Accuracy and +6% Critical Rate, and restore 1 STM every 3 s. SOC scaling can raise the bonuses to +15%, +8% and +8%. | Decisive Command (Leadership) |

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

Every family has an item every 5 ranks (0-45); capital families start at rank 40. The Data Analyzer is the exception: one item, no rank. "Toggle" means it fires every cycle while on (section 8, Activation). Numbers come from the Bible draft.

| Family | Slot | Skill | Use | What it does | Today |
|---|---|---|---|---|---|
| Laser Cannon | High | Piloting | Toggle | Thermal damage on a steady cycle. | Combat Laser |
| Ion Cannon | High | Piloting | Toggle | EM damage, strong against shields; slows on hull hits. | Ion Cannon |
| Beam Cannon | High | Piloting | Toggle | Thermal damage over a few ticks at short range. | Beam Cannon |
| Missile Launcher | High | Piloting | Toggle | Explosive damage at long range; uses missiles; bonus against low shields. Ion, incendiary and seeker ammo variants. | Missile Launcher |
| Torpedo Launcher | High | Piloting | One press | Heavy explosive damage to Large and Capital hulls; bypasses shields; ignores the size penalty; uses torpedoes. | Proton Bomb Launcher |
| Point-Defense Turret | High | Piloting | Toggle | Always fires as Light; bonus against Small hulls. | New |
| Turbolaser, Quad Laser, Storm Cannon | High | Piloting | Toggle | Capital weapons on short cycles. Capital hulls only. | Capital weapons |
| Laser Battery, Assault Concussion Missile | High | Piloting | One press | Capital burst weapons: an area volley, or a heavy missile using rare ammo. Capital hulls only. | Capital weapons |
| Mining Laser | High | Gathering | Toggle | Mines asteroids up to its tier. | Mining Laser |
| Strip Miner | High | Gathering | Toggle | Large and Capital hulls; strips a whole asteroid and roots the ship while it runs. | Strip Miner |
| Salvager | High | Gathering | Toggle | Salvages wrecks for scrap, components and rare finds. | New |
| Gas Harvester | High | Gathering | Toggle | Harvests gas clouds. | New |
| Shield Booster | Mid | Piloting | Passive | + max shield. | Shield Booster (was Low) |
| Shield Repairer | Mid | Piloting | Toggle | Repairs the shield of its locked target, or your own. | Shield Repairer (was High) |
| Shield Hardener | Mid | Piloting | Toggle | + resistances while on; pays capacitor each cycle. | New |
| Capacitor Battery | Mid | Piloting | Passive | + max capacitor. | Capacitor Booster (was Low) |
| Hypermatter Injector | Mid | Piloting | One press | Restores capacitor; uses a capsule. | Hypermatter Injector (was High) |
| Ion Web | Mid | Piloting | Toggle | Slows the target. | New |
| Interdictor | Mid | Piloting | Toggle | The target can't jump, dock or leave space while it holds. | New |
| Jammer | Mid | Piloting | Toggle | Jams the target (Accuracy down). | New |
| Target Painter | Mid | Piloting | Toggle | Paints the target (damage taken up). | New |
| Bulwark Shield Generator, Repair Field, E-War array | Mid | Piloting | Toggle | Capital fleet support. Capital hulls only. | Capital support modules |
| Afterburner | Mid | Piloting | Toggle | + speed while on; pays capacitor each cycle. | New |
| Cloaking Device | Mid | Piloting | Toggle | Hides a Small hull or Blockade Runner; no module or ability use while cloaked (section 32). | New |
| Probe Launcher | Mid | Piloting | Toggle | Scans hidden signatures system-wide; pulse mode for a Sensor Sweep (section 24). | New |
| Data Analyzer | Mid | Espionage (no rank; the Slicing perk gates targets) | One press | Starts the slicing mini-game on a data can, locked cache or wreck data core (section 24). | New |
| Targeting Computer | Mid | Piloting | Passive | + accuracy. | Targeting System (was Low) |
| Hull Plating | Low | Piloting | Passive | + max hull. | Hull Booster |
| Hull Repairer | Low | Piloting | Toggle | Repairs the hull of its locked target, or your own. | Hull Repairer (was High) |
| Resist Plating | Low | Piloting | Passive | + one resistance, or all resistances at a cost to evasion. | Mirrored Plating, Ion Shielding, Reactive Armor, Durasteel Plating |
| Capacitor Regulator | Low | Piloting | Passive | + capacitor regen. | New |
| Smuggling Compartment | Low | Piloting | Passive | Lowers the customs scan chance. | New |
| Damage Amplifier | Low | Piloting | Passive | + one damage type. | EM, Thermal and Explosive amplifiers |
| Thrusters | Low | Piloting | Passive | + evasion and speed. | Evasion Booster |
| Cargo Expander | Low | Piloting | Passive | + cargo hold. | New |
| Capital passives | Low | Piloting | Passive | Advanced Thrusters, Redundant Shield Generator, Reinforced Plating, Targeting Array, Weapons Computer, Power Diverter. Capital hulls only. | Capital passives |

## Appendix C: The 50 ships

Model and Launch skins are `appearance.2da` rows. Launch skins are Hull Designs available at release: other variants from the same model family, plus the models of the ships cut when the roster went from 100 to 50 (each went to a kept ship of the same size and role). Piloting is the required rank. Signature bonus sizes are drafts for the Bible formula.

| # | Ship | Size | Role | Maker | Tier | Piloting | Model | Launch skins | Signature bonus |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Striker | Small | Fighter | Corellian | Standard | 0 | 10057 | 10190, 10193, 10194 | Weapon cycles 5% faster. |
| 2 | Republic Striker | Small | Fighter | Republic | Standard | 0 | 10071 | 10072, 10073, 10199, 10200 | +15% shield regen. |
| 3 | Davaab | Small | Fighter | Mandalorian | Standard | 0 | 10155 | 10105 | +10% max hull and +5% missile damage. |
| 4 | Aurek Strikefighter | Small | Fighter | Republic | Advanced | 10 | 10074 | - | +20% max shield and +20% shield regen; Point-Defense fires as Medium. |
| 5 | Sith Fighter | Small | Fighter | Sith | Advanced | 10 | 10092 | 2008, 10089, 10090, 10091, 10210, 10211 | +10% Critical Rate and +10% weapon damage; critical hits restore 1 capacitor. |
| 6 | Cutlass | Small | Fighter | Corellian | Advanced | 10 | 10161 | 10204 | +1 Mid slot. |
| 7 | Condor | Small | Interceptor | Corellian | Standard | 0 | 10055 | 10153, 10144 | +10% speed. |
| 8 | Republic Scout | Small | Interceptor | Republic | Standard | 0 | 10068 | 10069, 10070, 10205, 2001, 2003 | Ion Web range +20%. |
| 9 | Sith Scout | Small | Interceptor | Sith | Standard | 0 | 10086 | 10087, 10088 | +10% damage to webbed targets. |
| 10 | Eclipse Interceptor | Small | Interceptor | Sith | Advanced | 10 | 2006 | - | +15% speed; webs also lower the target's evasion by 10. |
| 11 | Republic Bomber | Small | Bomber | Republic | Standard | 0 | 10058 | 10059, 10060, 10104 | Torpedo hits strip 10% extra shield. |
| 12 | Sith Bomber | Small | Bomber | Sith | Standard | 0 | 10077 | 10078, 10079, 10106, 10241, 2007 | +10% torpedo damage and +5% torpedo Critical Rate. |
| 13 | Chela Bomber | Small | Bomber | Republic | Advanced | 10 | 10149 | 2005, 10237 | Torpedoes ignore 25% of the target's resistances. |
| 14 | Sith Infiltrator | Small | Explorer | Sith | Standard | 0 | 10083 | 10084, 10085, 10064, 10065, 10066, 10067 | Sensor Sweep reveals ships 50% farther away. |
| 15 | Chiss Hyperion | Small | Explorer | Chiss | Standard | 0 | 10230 | 10191, 10192 | +25% scan strength; signatures show their tier from 25%. |
| 16 | Raider Prowler | Small | Explorer | Rift Raider | Advanced | 10 | 10215 | 10114, 10221, 10222 | Scans Wild Space 50% faster and ignores environment penalties. |
| 17 | Republic Gunship | Medium | Gunship | Republic | Standard | 10 | 10061 | 10062, 10063, 10148, 10056 | +10% shield resistances. |
| 18 | Sith Gunship | Medium | Gunship | Sith | Standard | 10 | 10080 | 10081, 10082, 10163 | +10% threat generation and +5% damage. |
| 19 | Hutt Gunship | Medium | Gunship | Hutt | Standard | 10 | 10107 | 10216 | +20% max hull, -10% speed. |
| 20 | Basilisk War Droid | Medium | Gunship | Mandalorian | Advanced | 20 | 10120 | 10156 | +15 hull resistances and +10% explosive damage. |
| 21 | Czerka Repair Tender | Medium | Logistics | Czerka | Standard | 10 | 10182 | 10226 | +15% remote hull repair. |
| 22 | Chiss Caretaker | Medium | Logistics | Chiss | Standard | 10 | 10103 | 10102 | Remote repair range +25%. |
| 23 | Jedi Transport | Medium | Logistics | Republic | Advanced | 20 | 10119 | 10157, 10101 | Repairs 30% stronger; repairs also remove one slow. |
| 24 | Y8 Miner | Medium | Mining Barge | Czerka | Standard | 10 | 10152 | 10151, 10227 | +15% ore yield. |
| 25 | Gas Skimmer | Medium | Mining Barge | Corellian | Standard | 10 | 10185 | 10201 | +15% gas yield. |
| 26 | XS Freighter | Medium | Light Freighter | Corellian | Standard | 10 | 10146 | 10054, 10206 | +20% cargo. |
| 27 | Hutt Hauler | Medium | Light Freighter | Hutt | Standard | 10 | 10186 | - | Comes with a built-in Smuggling Compartment. |
| 28 | Lambda Shuttle | Medium | Light Freighter | Republic | Advanced | 20 | 2002 | 10240 | +30% cargo and +15% max shield. |
| 29 | Pirate Freighter | Medium | Blockade Runner | Corellian | Standard | 10 | 10184 | 10147, 10160 | Cloaks in 2 seconds and +10% cargo. |
| 30 | Barracuda | Medium | Blockade Runner | Corellian | Advanced | 20 | 10113 | 10164 | Immune to Ion Webs; ships must be within 5 m to reveal it. |
| 31 | Republic Hammerhead | Large | Frigate | Republic | Standard | 25 | 10076 | 10134, 10075 | Point-Defense deals +20% damage. |
| 32 | Sith Hammerhead | Large | Frigate | Sith | Standard | 25 | 10138 | 10137 | +5% Critical Rate. |
| 33 | Mandalorian Raider | Large | Frigate | Mandalorian | Standard | 25 | 10121 | 10224 | +15% max hull and +10% missile damage. |
| 34 | Raider Marauder | Large | Frigate | Rift Raider | Advanced | 35 | 10196 | 10195, 10133 | Shield regen doubles while no weapon is firing; +10% weapon damage. |
| 35 | Heavy Lifter | Large | Industrial | Czerka | Standard | 25 | 10188 | 10132, 10229 | +20% strip-mining yield. |
| 36 | Czerka Salvager | Large | Industrial | Czerka | Standard | 25 | 10197 | 10198, 10131 | +20% salvage yield. |
| 37 | Freight Train | Large | Heavy Freighter | Czerka | Standard | 25 | 10189 | 2174, 10187 | +30% cargo. |
| 38 | Republic Quartermaster | Large | Heavy Freighter | Republic | Standard | 25 | 10130 | 10150 | +15% max shield; allied ships within 30 m take 5% less damage. |
| 39 | Hutt Grand Barge | Large | Heavy Freighter | Hutt | Advanced | 35 | 10109 | 10212 | +40% cargo; Jump Beacon jumps cost 50% less fuel. |
| 40 | Neutral Thranta | Capital | Cruiser | Corellian | Standard | 40 | 10135 | 10217 | +1 High slot (Gunner); -10% max hull and shield. |
| 41 | Republic Thranta | Capital | Cruiser | Republic | Standard | 40 | 10123 | 10117, 10122, 2170 | +10% max shield. |
| 42 | Sith Thranta | Capital | Cruiser | Sith | Standard | 40 | 10139 | 10126, 10238, 10239 | +10% damage. |
| 43 | Hutt Corvette | Capital | Cruiser | Hutt | Standard | 40 | 10108 | - | +20% max hull and +50% cargo. |
| 44 | Jehavey'ir | Capital | Cruiser | Mandalorian | Standard | 40 | 10154 | - | +15% max hull and +10% explosive damage. |
| 45 | Chiss Trireme | Capital | Cruiser | Chiss | Standard | 40 | 10162 | - | +15% weapon range. |
| 46 | Corellian Gunboat | Capital | Cruiser | Corellian | Standard | 40 | 10145 | - | +10% speed and +4 evasion. |
| 47 | Czerka Armored Transport | Capital | Command Ship | Czerka | Standard | 40 | 10218 | - | Crew stations +10% effective; droid crew +20%. |
| 48 | Terminus Corsair | Capital | Command Ship | Corellian | Standard | 40 | 10207 | 10142, 10093, 10118, 2169 | Allied ships within command range +5% speed. |
| 49 | Crusader Corvette | Capital | Command Ship | Mandalorian | Standard | 40 | 10260 | 10116, 10115 | +15% max hull; allies within command range +5% explosive damage. |
| 50 | Republic Centurion | Capital | Command Ship | Republic | Advanced | 50 | 10128 | 10129, 10124, 10125, 10143, 10127, 10141 | Command range +20 m; orders last 25% longer. |

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

These are not crafted, and they're tradeable. Each one is easiest to get from its manufacturer's side of the galaxy, so flying a maker's ships pulls you toward that faction. Every one also drops, more slowly, from data sites and aces, so no character needs standing with a faction to build its ships.

| Component | Manufacturer | Sources |
|---|---|---|
| Republic Shield Matrix | Republic | Republic Military agent store; Republic freight contracts; data sites and aces (slower) |
| Sith Targeting Core | Sith | Sith Military agent store; Imperial (T6) ships and Sith capitals; their wrecks; data sites and aces (slower) |
| Mandalorian Plating | Mandalorian | Mandalorian agent store; Clan Zel Arun (T4) ships and Mandalorian capitals; their wrecks; data sites and aces (slower) |
| Czerka Industrial Frame | Czerka | Czerka agent store; Czerka freight contracts; rare output from Assembly Line holdings; data sites and aces (slower) |
| Hutt Cargo Coupling | Hutt | Hutt Cartel agent store; Hutt enforcers; their wrecks; data sites and aces (slower) |
| Corellian Drive Kit | Corellian | Smugglers agent store; pirate raiders and the T1-T5 pirate factions; their wrecks; data sites and aces (slower) |
| Chiss Sensor Array | Chiss | Data sites, aces, and the black markets at Smuggler's Moon, Tatooine and Hutlar |
| Raider Refit Hull | Rift Raider | Wild Space only: Rift Raider ships and cache sites; Wild Space data sites and aces (slower) |

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
- **NPC ships** sometimes drop space lockboxes (section 12), at every tier.
- **Wild Space** drops prototype tech, Raider Refit Hulls and Thorilide.
- **Incursions** reward Sith or Mandalorian Ship Schematics, matching the invader.
- **Rift Runs** drop rare-trait parts and Design Schematics.
- **Registration:** every new item source is registered for `EconomyObtainabilityCoverageTests` (`EconomyRestrictedItems.md`).
