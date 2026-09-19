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
| Lockpicking | Additional trace budget for slicing lockboxes and terminals |

Perk-only adjustments such as flat Stealth rating, stealthed movement speed, stealth drain reduction, Back Attack damage/critical rate, trap capacity, trap arming speed, trap detection range, coating charges, and disguise capacity/cooldown are also represented by `StatType` values. Shared stealth, movement, damage, poison, trap, and disguise systems consume those stats; they do not special-case perk ownership.

## Starting progression

- Stealth I, Poisoncraft I, Slicing I, and Trapcraft I each cost 2 SP and are available at Espionage rank 0. Each path can train independently. Infiltration awards XP for sneaking through a hostile NPC's aggro range and leaving undetected after moving at least 4m; detection awards 15% of normal XP. Standing in stealth does not award XP, and each NPC awards infiltration XP only once per player.
- Craft Venom Coating I using 3 Kath Hound Blood and 2 Viscaran Rosemary (`kath_blood` and `herb_v`), or Snare Kit I using 3 Ruined Electronics and 2 Refined Veldite. Both are recipe level 3, so the shared three-rank crafting allowance permits crafting at rank 0. Recipe difficulty, actual required skill rank, and the training limit are shown separately.
- The Espionage Workbench is beside the market terminal in Veles Shops at (71.837, 68.257), with a map marker. Keldorn and the general-store vendor give directions. It was already present in source; deployment must use the repacked module.
- Poisoncraft tiers unlock at 0/15/28/40/48; Trapcraft tiers at 0/18/30/45, with Master Saboteur providing tier 5 at 50; Slicing tiers at 0/22/30/42/48. The `Skill` service owns these gates and training limits. Core recipes do not require dropped schematics or unrelated perks.
- Each tier's recipes, slicing successes, and hostile NPC trap disarms award XP until the next tier's unlock, exclusive, with tier 5 capped at 50. Practice XP follows the normal activity-level delta with a minimum 150 base XP while the tier still teaches. Other skills' recipes retain their existing XP curve. Concealed kit triggers award 150 XP against hostile NPCs within the kit's training range; placing, dismantling, or disarming player traps grants no XP.
- Slicing consumables are optional. The Veles General Store stocks Copper Trace Fuses, Ratchet Bypass Pins, and Continuity Samplers as single, unlimited-stock items with 50 base cost and 50 additional cost. Advanced tools remain slicing rewards and higher-tier fuses remain Engineering recipes. Supplies are selected inside the slicing window.

## Stealth and detection

- Baseline stealth uses NWN's built-in Stealth action, requires Stealth I-IV, and can only be entered out of combat. No separate Stealth ability is granted. Ghost Protocol is the sole in-combat entry window.
- Spot detection replaces the vanilla roll with one opposed check: `d20 + Detection` versus `d20 + Stealth`. The verdict is cached per observer/target pair for 30 seconds. Ties favor the stealthed target.
- A successful Spot check against a player exits that player's stealth and reveals them globally. NPC stealth retains the engine's observer-specific visibility behavior.
- Listen detection is suppressed so there is one detection model rather than separate Spot and Listen rolls.
- Cache entries for a stealthed target are cleared on stealth exit; expired entries are pruned as the cache grows.
- Stealth drains 2 STM every 6 seconds. Silent Stride reduces the drain rate by 20%, producing 2 STM every 7.5 seconds, and grants +30% Movement Speed while stealthed without removing stealth's running restriction.
- Reaching zero STM exits stealth. Activating a hostile ability or landing a damaging hit also exits stealth.
- Stealth is calculated as `(AGI x 2) + equipment + perk/status bonuses`; Stealth I-IV grant +5/+10/+15/+20 Stealth while active.
- Detection is calculated as `PER + WIL + equipment + perk/status bonuses`; Detect mode adds +5. NPC Detection keeps scaling with those stats but is capped at 50. Players, DMs, and DM-possessed creatures are not capped, so ratings above 50 require player or staff investment.
- Alertness I-III remains on the General tab as the universal Detection counter and grants +10/+15/+20 Detection.
- Stealth and Detection equipment already exists and contributes directly to the opposed ratings.

## Infiltrator behavior

- Back Attack applies only to melee weapon attacks made from behind: +3/+5/+8% damage, with +3/+5% Critical Rate at ranks II/III.
- Slicing I-V unlock tier 1-5 portable lockboxes and shared world terminals. Mandalorian Facility quest terminals and ordinary world locks remain outside this system.
- Slicing is a turn-based NUI circuit puzzle with no timers or heartbeat work. Players rotate circuit tiles or swap adjacent tiles to connect the fixed entry node to the fixed core before exhausting trace. Runtime sessions select from a checked-in catalog of 100 pre-generated, guaranteed-solvable boards per tier; catalog generation rejects boards that begin solved or can be solved by one ordinary rotation or swap. The help view displays the stable tier-and-board ID for reproduction and bug reports.
- Boards scale from 3x3 at tier 1 to 5x5 at tier 5. Slicing III-V grant +1/+2/+3 trace. Every five points of combined Lockpicking and positive Perception modifier grants another trace, capped at +5.
- Cancelling before the first move is free. Once committed, aborting, moving away, dying, or disconnecting counts as a failure. A trace fuse grants +1 trace on the first move, while information tools provide immediate puzzle assistance and are consumed when used.
- Lockboxes preserve their catalog board number and failure count when transferred. Legacy target seeds map deterministically to a catalog board number. The first failure cannot destroy the box; later failures raise the destruction chance to 10/25/50/100%. Shared terminals reserve one player at a time, release stale reservations after three minutes, and respawn at a random valid walkmesh point after 45-75 minutes.
- Success awards experience once and one weighted reward. Direct rewards are fixed-stat items balanced by Armor skill requirements rather than Espionage rank; they do not grant raw attributes. The system has separate 50-item lockbox and 50-item terminal pools, supplemented by suitable existing items.
- Tactical Escape I/II reduce enmity by 35/60% and grant +8/+12% Evasion for 30 seconds; rank II also removes negative movement-speed effects.
- Shadow Step I/II moves the user behind one hostile target within 5m and grants +10/+15% Evasion for 30 seconds; rank II also removes negative movement-speed effects. It does not grant invisibility.
- Ghost Protocol reduces enmity by 80%, permits up to 30 seconds of stealth, and primes the next back attack within 30 seconds to critically hit and apply Exposed (-20% Defense for 30 seconds).

## Saboteur behavior

- Poisoncraft I-V unlock the five Venom Coating recipes at the Espionage Workbench. Anyone may apply a crafted coating to an eligible melee or thrown weapon; energy blades are rejected.
- A coating has 20 charges. Lasting Coatings increases this by 50%, to 30 charges.
- Venom duration is tier-based: 12/18/24/30/36 seconds. Venom Expertise I/II increases direct Venom damage by 10/20%; Master Saboteur adds another 10%. These bonuses do not extend duration or charges.
- Razor Trap I/II and Shock Trap are visible zoning abilities. They arm after 3 seconds and affect enemies in a 3m blast.
- Crafted Snare Kits place concealed traps through their inventory Activate Item action. They need no active trap ability. Placement, detection, and disarming share the same tier gate, including Master Saboteur for tier 5. A failed placement preserves the kit; dismantling a placed trap does not refund it.
- Trap kits use self activation, while ordinary and concentrated poison vials target a weapon in the user's inventory. Blueprint activation properties are unlimited-use because the scripted handler consumes exactly one item after success. Item definitions declare their activation spell so older saved instances are repaired on login or acquisition without recreating the item or resetting its stack.
- Trapcraft III/IV reduce the 3-second arming delay by 20/30%, to 2.4/2.1 seconds.
- Base concurrent-trap capacity is 1. Trap Management I/II raise it to 2/3; placing over the cap removes the oldest trap. Traps also require 3m spacing and expire after 5 minutes.
- Base concealed-trap detection range is 6m. Trap Management II adds 5m, for 11m total.
- Master Saboteur unlocks tier 5 traps and increases trap damage and weapon-poison Venom damage by 10%.

## Tradecraft behavior

- False Identities I-III increase stored disguise capacity to 2/3/4.
- Cover Story I/II reduce the delay between disguise activations by 40/70%.
- Tradecraft is part of the same mandatory Espionage review surface even though its rows are passive utility perks.

## Verification and release work

The static implementation review currently covers all 41 rows: exact price, requirements, type, resource/cast/recast values, description text, perk/feat wiring, active definitions, spell links, TLK entries, scaling declarations, and targeting metadata. Focused tests also cover disguise progression, slicing board solvability and failure escalation, reward catalogs and obtainability, coating charges, Venom duration/damage, trap ranges, and category totals.

The progression and item-activation repair requires deploying both the server assembly and a newly packed module. It does not change HAK resources. Existing inventory kits and coatings receive their missing activation property on login or acquisition. NPC trap placement remains optional world content.

## Tester retest

1. On separate Standard characters at Espionage rank 0, purchase only Poisoncraft I, Trapcraft I, or Slicing I. Confirm none requires Stealth or another profession.
2. Find the Espionage Workbench map marker in Veles Shops. With the listed starter materials, confirm the level-3 coating and snare recipes show required rank 0 and can be crafted. Verify XP still arrives past rank 7 and stops only at the next profession unlock.
3. Right-click a new and an older saved Snare Kit, choose Activate Item, and remain still. One kit should deploy at your feet and be consumed after successful placement. Moving or failing the spacing check must preserve the stack. Trigger it with a hostile NPC after arming. Also log in with legacy kits and coatings inside carried bags, and acquire a bag containing them: contained items must gain their activation without changing their stacks or adding duplicate properties.
4. Right-click ordinary and concentrated venom, choose Activate Item, and select an owned melee or thrown weapon. Verify one vial is consumed after application. Reject energy blades, someone else's weapon, and invalid targets without consumption.
5. Buy basic assistance at the Veles General Store and find a tier-1 world terminal. Complete attempts with and without assistance. Confirm tools appear inside the slicing window, XP reaches Slicing II at 22, and old tier-1 targets then stop awarding XP.
6. At higher ranks, verify each next profession perk and recipe can continue training through 50. With Master Saboteur, verify tier-5 placement, detection, and disarming use the same access rule. Player trap disarms and dismantling must not provide a repeatable XP source.
