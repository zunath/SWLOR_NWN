# Espionage Implementation and Review Status

Espionage is fully included in the Design Bible review and combat-upgrade audit. It must never be filtered out as optional, deferred, or intentionally unimplemented. The authoritative design is the `Espionage` tab in `design/bible/SWLOR Design Bible - Combat Upgrade.xlsx`; the generated manifest and `CombatUpgradeBibleSyncTests` verify it against code, feat/spell data, and TLK text.

## Reviewed scope

The tab contains 41 implemented rows totaling 135 SP:

| Section | Rows | SP | Content |
|---|---:|---:|---|
| Tradecraft | 5 | 15 | False Identities I-III and Cover Story I-II |
| Infiltrator | 18 | 60 | Stealth, Back Attack, Slicing, Silent Stride, Tactical Escape, Shadow Step, and Ghost Protocol |
| Saboteur | 18 | 60 | Poisoncraft, Trapcraft, traps, Venom Expertise, Lasting Coatings, Trap Management, and Master Saboteur |
| **Total** | **41** | **135** | **All rows are required audit scope** |

Espionage is a Standard-only Utility skill with a maximum rank of 50. Poison use remains universal; the skill, crafting access, and perks are Standard-only. Espionage actives are player-only and do not grant droid instruction slots.

## Stat-driven architecture

Six player-facing equipment stats are documented on the Bible `Character Stats` tab and supported end to end by item properties, equipment aggregation, player persistence, NPC skins, and `Stat` accessors:

| Stat | Purpose |
|---|---|
| Stealth | Opposed-check strength while stealthed |
| Detection | Opposed-check strength against stealthed creatures |
| Trap Bonus | Trap effect strength |
| Disarm | Hostile-trap disarm checks |
| Poison Bonus | Venom damage potency, snapshotted from the applier |
| Lockpicking | Lockbox success checks |

Perk-only adjustments such as flat Stealth rating, stealthed movement speed, stealth drain reduction, Back Attack damage/critical rate, trap capacity, trap arming speed, trap detection range, coating charges, and disguise capacity/cooldown are also represented by `StatType` values. Shared stealth, movement, damage, poison, trap, and disguise systems consume those stats; they do not special-case perk ownership.

## Stealth and detection

- Baseline stealth uses NWN's built-in Stealth action, requires Stealth I-IV, and can only be entered out of combat. No separate Stealth ability is granted. Ghost Protocol is the sole in-combat entry window.
- Spot detection replaces the vanilla roll with one opposed check: `d20 + Detection` versus `d20 + Stealth`. The verdict is cached per observer/target pair for 30 seconds. Ties favor the stealthed target.
- A successful Spot check against a player exits that player's stealth and reveals them globally. NPC stealth retains the engine's observer-specific visibility behavior.
- Listen detection is suppressed so there is one detection model rather than separate Spot and Listen rolls.
- Cache entries for a stealthed target are cleared on stealth exit; expired entries are pruned as the cache grows.
- Stealth drains 2 STM every 6 seconds. Silent Stride reduces the drain rate by 20%, producing 2 STM every 7.5 seconds, and grants +30% Movement Speed while stealthed without removing stealth's running restriction.
- Reaching zero STM exits stealth. Activating a hostile ability or landing a damaging hit also exits stealth.
- Stealth is calculated as `(AGI x 2) + equipment + perk/status bonuses`; Stealth I-IV grant +5/+10/+15/+20 Stealth while active.
- Detection is calculated as `PER + WIL + equipment + perk/status bonuses`; Detect mode adds +5.
- Alertness I-III remains on the General tab as the universal Detection counter and grants +10/+15/+20 Detection.
- Stealth and Detection equipment already exists and contributes directly to the opposed ratings.

## Infiltrator behavior

- Back Attack applies only to melee weapon attacks made from behind: +3/+5/+8% damage, with +3/+5% Critical Rate at ranks II/III.
- For the first iteration, Slicing applies only to lockboxes. Slicing I-V unlock tier 1-5 lockboxes; Slicing III-V shorten the lockbox interaction by 20/30/40%, reducing its 2-second base delay to 1.6/1.4/1.2 seconds. It does not claim support for world locks or terminals.
- Lockbox success uses the Lockpicking stat and a tier-scaled roll. Failed attempts impose a 30-second retry lockout; the box is not destroyed.
- Tactical Escape I/II reduce enmity by 35/60% and grant +8/+12% Evasion for 30 seconds; rank II also removes negative movement-speed effects.
- Shadow Step I/II moves the user behind one hostile target within 5m and grants +10/+15% Evasion for 30 seconds; rank II also removes negative movement-speed effects. It does not grant invisibility.
- Ghost Protocol reduces enmity by 80%, permits up to 30 seconds of stealth, and primes the next back attack within 30 seconds to critically hit and apply Exposed (-20% Defense for 30 seconds).

## Saboteur behavior

- Poisoncraft I-V unlock the five Venom Coating recipes at the Espionage Workbench. Anyone may apply a crafted coating to an eligible melee or thrown weapon; energy blades are rejected.
- A coating has 20 charges. Lasting Coatings increases this by 50%, to 30 charges.
- Venom duration is tier-based: 12/18/24/30/36 seconds. Venom Expertise I/II increases direct Venom damage by 10/20%; Master Saboteur adds another 10%. These bonuses do not extend duration or charges.
- Razor Trap I/II and Shock Trap are visible zoning abilities. They arm after 3 seconds and affect enemies in a 3m blast.
- Crafted Snare Kits place concealed traps. Trapcraft controls crafting, placement, detection, and disarming by tier.
- Trapcraft III/IV reduce the 3-second arming delay by 20/30%, to 2.4/2.1 seconds.
- Base concurrent-trap capacity is 1. Trap Management I/II raise it to 2/3; placing over the cap removes the oldest trap. Traps also require 3m spacing and expire after 5 minutes.
- Base concealed-trap detection range is 6m. Trap Management II adds 5m, for 11m total.
- Master Saboteur unlocks tier 5 traps and increases trap damage and weapon-poison Venom damage by 10%.

## Tradecraft behavior

- False Identities I-III increase stored disguise capacity to 2/3/4.
- Cover Story I/II reduce the delay between disguise activations by 40/70%.
- Tradecraft is part of the same mandatory Espionage review surface even though its rows are passive utility perks.

## Verification and release work

The static implementation review currently covers all 41 rows: exact price, requirements, type, resource/cast/recast values, description text, perk/feat wiring, active definitions, spell links, TLK entries, scaling declarations, and targeting metadata. Focused tests also cover disguise progression, stealth drain timing, Slicing delay, coating charges, Venom duration/damage, trap ranges, and category totals.

Remaining release work is live playtesting and deployment packaging, including NPC trap placement where desired, module repack, and hak rebuild. These are release tasks, not reasons to exclude Espionage rows from the Bible review or mark them unimplemented.
