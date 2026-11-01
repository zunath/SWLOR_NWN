# Capstone Dungeon Factions and Skill Trials Plan

Status: proposal, awaiting owner approval. No code yet.

Source: player feedback (November 2026) on the capstone dungeons, three points:

1. Every enemy in a dungeon should belong to one faction with names and ranks that fit the planet, not
   "[Capstone Ability] Adept/Specialist/Inner Circle" copies.
2. Every capstone quest in a dungeon should target the same enemies per tier, and prove the skill through
   its own mechanic (Twin Pistol's paired auto-attack and Cadence trials are the model), so players of
   different skills can progress together and stop clearing mobs hoping their own target spawns.
3. Enemy tiers should spawn by depth: easier near the entrance, harder further in.

Point 3 is handled separately (adepts on the spawn points nearest the entrance, specialists on the deeper
ones; inner circles already had four fixed spawns each since PR 3049). This plan covers points 1 and 2.

## Today

- 13 capstone dungeons, each hosting 3 capstone lines (39 lines). Each line has 5 quest steps:
  Foundation (kill 6 adepts), Measure (kill 5 specialists), Breach (party kills the line's warden, summoned
  at an altar), Circle (kill 4 inner circle), Mastery (party kills the line's master in the boss arena).
- Each line has its own 5 enemy blueprints (`cp_<line>_ad/_sp/_wd/_ic/_ms`, 195 in all), its own
  `NPCGroupType` per step, its own signature NPC ability package, resistance profile and `_RARES` loot.
- A dungeon therefore holds 3 adept types, 3 specialist types and 12 inner circle guards, and each player
  only gets credit for a third of them.
- Exceptions that already read as a faction: the Force lines (Crypt Marauders, Vitae Reavers, Umbral
  Raiders) and the Lightsaber and Saber Cyclone lines (training droids). Twin Pistol (2 lines) has trial
  objectives instead of kill steps, in the Anchorhead Canyon Range.

## Point 1: one faction per dungeon

Proposal: each dungeon gets one faction with a shared rank ladder. The three combat styles inside a tier
stay (they carry the signature abilities, resistance profiles and visual variety that tests require), but
they are named as roles of the faction, not after a player capstone.

| Tier | Today | Proposed |
| --- | --- | --- |
| Adept | 3 per dungeon, one per line | 3 faction roles at the faction's entry rank; any of them counts for every line |
| Specialist | 3 per dungeon, one per line | 3 faction roles at the veteran rank; any counts for every line |
| Inner circle | 4 fixed guards per line | the faction's elite guard; 12 fixed spawns, any counts for every line |
| Warden, master | one per line, on demand | stay per line (each line keeps its own boss fight), renamed as faction officers |

Mechanically this is one `NPCGroupType` per dungeon tier (for example `Tatooine_CanyonRange_Adept`) shared
by the three adept blueprints, and every line's kill objective pointing at it. The per-line groups for
wardens and masters stay.

Faction name candidates (owner picks or replaces; ranks in brackets are adept / specialist / inner circle):

| Dungeon | Planet | Faction candidate |
| --- | --- | --- |
| Veles Militia Annex | Viscara | Veles Militia Cadre (Recruit / Sergeant / Sheriff's Guard) |
| Viscara Republic Engineering Bunker | Viscara | Swamp Salvage Battalion (Scavenger / Engineer / Command Detail) |
| Tatooine Forge Caverns | Tatooine | Forgebound Sith cult (Bladesworn / Tempered / Anvil Circle) |
| Anchorhead Canyon Range | Tatooine | Canyon Range Gunhands (Drifter / Marksman / Range Marshal's Posse) |
| Smuggler's Moon Fight Club Backrooms | Smuggler's Moon | Backroom Ring (Brawler / Pit Fighter / House Champions) |
| Czerka Arms Test Range | Smuggler's Moon | Czerka Range Security (Tester / Range Officer / Black Cell) |
| Hutlar Jedi Trial Halls | Hutlar | Trial Hall Constructs (Drill / Duelist / Examiner droids, as today) |
| Hutlar Qion Test Site | Hutlar | Qion Ordnance Detachment (Technician / Demolitionist / Overload Crew) |
| Mon Cala Sith Crypt Depths | Mon Cala | Crypt raiders (today's three bands merged under one name, or kept as three bands of one expedition) |
| Mon Cala Medical Sublevel | Mon Cala | Sublevel Wardens (Orderly / Field Medic / Ward Keepers) |
| CZ-220 Breaker Yard | CZ-220 | Breaker Yard Crew (Scrapper / Rigger / Yard Bosses) |
| Dathomir Tarn Jungle Preserve | Dathomir | Preserve beasts (young / older / alpha pack, by species) |
| Dathomir Grotto Apex Den | Dathomir | Grotto beasts (young / older / apex pack, by species) |

Work: rename 195 blueprints (names, descriptions, portraits stay), add 39 shared tier groups and repoint the
kill objectives, merge the three `_RARES` tables per dungeon into one (each line's rare items stay
obtainable), rewrite the 39 x 5 journal texts and 39 giver conversations where they name enemies, update
the WorldNpcs Bible rows, the access key and proof key item descriptions that name enemies, and the tests
that pin one group per line step (`CapstoneQuestDefinitionTests`, `CapstoneEnemyAppearanceTests` and
similar).

## Point 2: shared targets, skill-specific methods

With point 1 every line in a dungeon kills the same targets per tier. The proof of skill moves from "which
enemy" to "how": a kill or hit only counts when the player used their skill's core mechanic on that target.

Two existing patterns to build on:

- Counted objective (preferred): `Feature/UnarmedMastery.cs` subscribes to `MeleeTechnique.TechniqueLanded`
  and calls `Quest.RecordCombatObjective(player, key, target)`, which counts down a persistent
  `CombatObjective` added with `QuestBuilder.AddCombatObjective`. Each capstone tier step becomes "N
  qualifying kills/hits on this tier's faction enemies".
- One-shot trial: `Service/TwinPistolMastery.cs` tracks a short sequence in memory and grants a proof key
  item when it completes (`TwinPistolTrialObjective`). Good for the inner circle step, where the trial is a
  combo rather than a count.

Proposed per-skill proof (from the skills' current abilities; each needs its hook point confirmed in code
before it is built):

| Skill (lines) | Core mechanic | Proof on faction targets |
| --- | --- | --- |
| Pistol | Gambler target switching and crits; Skirmisher disarm/interrupt | crit 3 different enemies in a row; Disarming or Interrupting Shot lands |
| Twin Pistol | Cadence build and spend, paired shots | already built (paired auto-attacks; Tempo, Lead, Closing Shot spending Cadence) |
| Rifle | One Shot after a 3 s pause; Suppression Evasion stacks | One Shot crit after a pause; N Suppression stacks on one enemy |
| Throwing | Ordnance area throws and Blind; Flurry Bleed spread | one throw hits 4+ enemies; Perfect Flurry spreads Bleed to 3 |
| Twin Blade | Bleed and Hemorrhage; Cyclone area pulses | Bleed on 3 enemies, then Red Bloom; Tempest Bloom hits 3+ |
| Vibroblade | Defense stances and enmity; Blood Frenzy cleaves | hold enmity under Shield Wall; Savage Cleave hits 3+ in Blood Frenzy |
| Heavy Vibroblade | Bastion tanking; HP-for-damage Soul abilities | tank with Bastion Stance; heal back N HP from Soul abilities |
| Vibroknife | Venom and Infection stacks consumed by Viral Cascade; Exposed | Viral Cascade consumes Venom plus 3 Infection; N hits on Exposed targets |
| Katar | Guard and Guard Counter; Daze and Hamstring | Guard Counter after a Guard; Scrapheap Lockdown dazes 5 |
| Lightsaber | Sunder; Saber Ward and Force Link | Sundering Sweep sunders 3; Force Link absorbs N damage for an ally |
| Saberstaff | Conduit above 70% FP and STM; Tempest area hits | N abilities while both pools are above 70%; area abilities hit 3+ |
| Spear | Vigor flanking; Disabler control and interrupts | interrupt an ability; Foggy Mind plus Force Disruption on 3 |
| Staff | Crusher bonus on controlled targets; Sentinel knockdown | land Charged Blows on your own controlled target; knock down 3 |
| First Aid | heals and cleanses; stims | heal N HP on allies in the dungeon; cleanse Bleed or Poison |
| Leadership | party orders and temp HP | one order buffs 3+ party members; temp HP on a low-health ally |
| Devices | Burn and Bleed payoffs, beacons, bunkers, combos | Thermal Detonator on 3 Burning/Bleeding enemies; beacon pulse hits N |
| Force | heals, DoTs, debuffs, Light/Dark capstones | Force heals on allies; DoT or debuff on N enemies |
| Beast Mastery (6 roles) | role commands and role abilities | per role: Tank holds an enemy for N s, Damage lands Apex Bite crit, and so on |

Support skills (First Aid, Leadership, some Force) prove themselves on allies, so their steps count
qualifying heals or buffs while the party fights that tier, which is exactly the "progress together"
goal.

Kill counts stay as today (6 / 5 / 4) unless the owner wants them retuned; the step becomes harder only by
requiring the mechanic.

## Decisions for the owner

1. Faction per dungeon (recommended) or per planet. Six planets host two dungeons each with very different
   themes (Forge Caverns and Canyon Range on Tatooine), so a planet-wide faction would fit one of them
   poorly.
2. Keep three combat roles per tier inside the faction (recommended: keeps signature abilities, resistance
   variety and the tests' uniqueness rules) or collapse to one enemy per tier.
3. Faction and rank names: pick from the table above or supply your own.
4. Objective style: counted qualifying kills/hits (recommended for Foundation and Measure) and one-shot
   trials (recommended for Circle), or one style for every step.
5. Rollout: pilot one dungeon first (recommended: Anchorhead Canyon Range, which already hosts the Twin
   Pistol trials), then the other 12.

## Risks and deploy notes

- Players partway through a kill step keep their quest state, but counts tied to the old per-line groups
  stop rising once objectives point at the shared groups. Needs a migration that maps old group progress
  to the new group, or an announcement that in-progress counts reset for that step.
- Rare loot must stay obtainable after the `_RARES` merge (`EconomyObtainabilityCoverageTests`).
- Signature NPC abilities are unique per blueprint today; renaming keeps them, merging blueprints would not.
- Deploy: server code, module repack (blueprints, journals, conversations), Bible update; no hak changes
  unless new portraits or appearances are chosen.
