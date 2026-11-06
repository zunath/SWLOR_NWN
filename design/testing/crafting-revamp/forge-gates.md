# Crafting revamp: balance gates of the Plasma Forge minigame

- Command: `SWLOR.CraftSim gates --scenario forge --gates rotation,styles,perks,length,materials,xp,enhancements,ladder --runs 40 --seed 1 --recipes-per-band 6 --repo . --out design/testing/crafting-revamp/forge-gates.md`
- Seed 1; 40 runs per case.

## Inputs

- Scenario: Plasma Forge (`forge`), Smithery recipes, minigame Forge.
- Quality: the quality meter's share of its maximum when the item is made, in points (0-100). A broken or abandoned piece counts 0, so every average includes the crafts that failed.
- Crafter: the rank the scenario's cohort gives each recipe (by default the recipe's level, at most 50, so a crafter works at their own level), with no food or status bonuses and no perks unless a gate names a style or the scenario's limitations say it plays a perk loadout. Gear levels are the plan's none, middle and best tier of the skill's crafting gear:
  - no gear (Control 0, Craftsmanship 0, CP 0)
  - mid gear (Control 13, Craftsmanship 12, CP 23)
  - best gear (Control 29, Craftsmanship 30, CP 37)
  - fully modded gear (Control 119, Craftsmanship 120, CP 37)
- Recipes: 1-10: 6 of 120; 11-20: 6 of 144; 21-30: 6 of 196; 31-40: 6 of 135; 41-53: 6 of 193 recipes played.
- Adaptive player: depth 4, forecast samples per result 2, budget 6000 engine copies per decision, 0.5% of value lost per extra action, heuristic DefaultLeafHeuristic. Each attempt's dice come from its own stream of the seed.
- Fixed-rotation solver: beam 8, cycles up to 3, 8 adaptive lines as seeds, up to 40 actions, 64 training attempts per local-search round and 150 selection attempts for each of 12 finalists, local search from the best 3 candidate(s) for up to 3 round(s) each. Gate 1 searches one sequence for each band and gear tier across the band's sampled recipes (every recipe weighing the same; an action a recipe's engine cannot take is skipped there), and also each recipe's own best sequence as a reported metric. The winner is chosen on separate attempts and then scored on a third set (hold-out) with 40 attempts per recipe.
- Legacy comparator (gates 5 and 6): the old minigame's lookahead player (`SWLOR.CraftSim baseline`), replayed here with the same seed on the same recipes, ranks and gear.
- Time: 8.0 s per action in the new window and 6.0 s in the old one, plus 0.0 s per craft in both. These are estimates; the client evidence replaces them.
- XP multiplier on the base XP of every craft, failures included, as the game server applies it (`CraftMinigameXp`): 1-10 1.97x, 11-20 1.20x, 21-30 1.46x, 31-40 2.05x, 41-53 1.09x. The new side's XP below includes it; the legacy loop's does not.
- Thresholds (starting values, all in `GateThresholds`):
  - `RotationMarginPoints` = 10: gate 1: adaptive beats the best single fixed rotation (one sequence for a whole band) by at least this many quality points
  - `StyleMinimumWinShare` = 30%: gate 2: each style is the better choice on at least this share of recipes
  - `StyleMaximumMeanDifferencePoints` = 5: gate 2: neither style beats the other by more on average
  - `StyleTieMarginPoints` = 0.5: gate 2: a recipe within this many points is a tie, half a win each
  - `PerksOptionalRank` = 50: gate 3: rank of the unperked crafter
  - `PerksOptionalMinimumFullQualityShare` = 50%: gate 3: share of crafts that reach maximum quality, per at-level band
  - `PerksOptionalBands` = 41-45, 46-50, 51-53: gate 3: the at-level recipe bands
  - `MaterialTolerance` = 10%: gate 5: component stacks per item within this share of the legacy baseline (above it only, where the legacy success rate is below the next value)
  - `MaterialTwoSidedMinimumLegacySuccess` = 90%: gate 5: cells where the legacy policy succeeds at least this often keep the two-sided parity; below it, using less material passes
  - `MaterialExtraNeverCraftedShare` = 10%: gate 5: recipes never made may exceed the legacy share by at most this
  - `MaterialQualityShortfallPoints` = 5: gate 5: the new average quality may be at most this many points below the legacy player's
  - `XpRateMinimumRatio` = 1: gate 6: XP per hour divided by the legacy rate
  - `LegacySecondsPerAction` = 6: gate 6: seconds per action in the old craft window (unmeasured estimate)
  - `OverheadSecondsPerAttempt` = 0: gate 6: seconds per craft besides actions, both loops (unmeasured estimate)
  - `EnhancementParityRank` = 50: gate 7: rank of the enhancing crafter (best gear)
  - `EnhancementParityBands` = 41-45, 46-50, 51-53: gate 7: the at-level recipe bands
  - `EnhancementSlotCounts` = 1, 2, 3: gates 6 and 7: numbers of filled enhancement slots played
  - `EnhancementParityMinimumRatio` = 1: gate 7: expected transferred enhancements per enhanced craft divided by today's
  - `EnhancementParityFullModsRatio` = 0.95: gate 7, minigames tuned against full mods: the same ratio for one and two slots in fully modded gear
  - `EnhancementParityThreeSlotFullModsRatio` = 0.9: gate 7, minigames tuned against full mods: the same ratio for three slots in fully modded gear
  - `PerksOptionalAboveRankFullModsShare` = 35%: gate 3, minigames tuned against full mods: the share in the band above the crafter's rank
  - `GearLadderBands` = 46-50, 51-53: gear ladder: the at-level recipe bands
  - `GearLadderEnhancements` = 2: gear ladder: enhancement kits played
  - `GearLadderMinimumStep` = 5: gear ladder: the least each gear step adds to average quality
  - `GearLadderSaturatedQuality` = 97: gear ladder and gate 1: an average at or above this has nothing left to earn
  - `GearLadderFullModsReach85Share` = 60%: gear ladder: share of top-band crafts in fully modded gear that reach 85%
  - `GearLadderFullModsMaxFullQualityShare` = 90%: gear ladder: most top-band crafts in fully modded gear that reach 100%
  - `GearLadderPerkAllowance` = 5: gear ladder: how far a full perk line in no gear may beat unmodded best gear
  - `UnderGearedNoGearFromLevel` = 31: gates 4, 5 and 6: no gear is only reported from this recipe level
  - `UnderGearedMidGearFromLevel` = 41: gates 4, 5 and 6: mid gear is only reported from this recipe level
  - `RecipesPerBand` = 6: recipes played per band
  - `SolverOptions` = the scenario's: effort of the fixed-rotation solver
  - `ActionLimit` = 100: actions after which an attempt counts as stalled
- Length target: 10-20 actions (item craft at level).
- Enhancement thresholds in force (`CraftOutcomeRules`, read by gate 7): 50% / 85% / 100% for slots 1, 2 and 3. The scenario applies enhancements to its engine's progress meter.
- Styles compared: Hot Forging (no lines) vs Cold Working (no lines) (from rank 20); Hot Forging (both lines) vs Cold Working (both lines) (from rank 20).

## Verdicts

| Gate | Verdict | Summary |
| --- | --- | --- |
| No fixed rotation | Passed | Adaptive play beats the best single fixed rotation by at least 10 points in every band and gear tier (smallest margin -0.1 in 1-10 / fully modded gear). The best rotation for each recipe alone, a reported metric, is -10.3 to 3.4 points behind adaptive play. |
| Style balance | Passed | Every style is the better choice on at least 30% of recipes and no pair differs by more than 5 points on average. |
| Perks optional | Passed | An unperked rank-50 crafter in fully modded gear reaches maximum quality on at least 50% of crafts in every at-level band (lowest 41%). |
| Length | Passed | Every band and gear tier takes 10-20 actions (item craft at level) on average. |
| Economy parity | Failed | Outside 10% of the legacy baseline (above it only, where its success rate is below 90%), or below its quality: 21-30 / no gear (ratio 1.01). |
| XP rate | Failed | Item crafts: XP per hour falls below 1.00x the legacy rate in 1 case(s): 11-20 / no gear (0.98x). |
| Enhancement parity | Passed | At rank 50 in fully modded gear, the thresholds in force (50% / 85% / 100%) transfer at least 0.90 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 0.97 times today's). Thresholds that meet parity in every band for this quality: 79% / 92% / 92%. |
| Gear ladder | Failed | 51-53 / Finish-heavy / fully modded gear takes 10.0 actions. |

## No fixed rotation

### Average quality: adaptive play against the best single fixed rotation of each band and gear tier (quality points; a broken piece counts 0)

| Band | Gear | Recipes played | Adaptive avg quality | Best single rotation avg quality | Margin | Required | Best rotation per recipe avg quality (reported) | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 63.0 | 50.5 | 12.5 | >= 10 | 63.2 | Passed |
| 1-10 | mid gear | 6 | 88.9 | 74.8 | 14.1 | >= 10 | 90.7 | Passed |
| 1-10 | best gear | 6 | 97.6 | 89.6 | 8.0 | >= 10 | 98.9 | Reported |
| 1-10 | fully modded gear | 6 | 99.9 | 100.0 | -0.1 | >= 10 | 100.0 | Reported |
| 11-20 | no gear | 6 | 59.8 | 41.5 | 18.3 | >= 10 | 63.7 | Passed |
| 11-20 | mid gear | 6 | 83.5 | 56.8 | 26.7 | >= 10 | 89.3 | Passed |
| 11-20 | best gear | 6 | 95.1 | 65.2 | 29.9 | >= 10 | 97.0 | Passed |
| 11-20 | fully modded gear | 6 | 99.9 | 99.9 | 0.0 | >= 10 | 100.0 | Reported |
| 21-30 | no gear | 6 | 62.5 | 38.7 | 23.8 | >= 10 | 59.0 | Passed |
| 21-30 | mid gear | 6 | 84.7 | 49.6 | 35.1 | >= 10 | 92.7 | Passed |
| 21-30 | best gear | 6 | 93.6 | 63.9 | 29.8 | >= 10 | 94.1 | Passed |
| 21-30 | fully modded gear | 6 | 99.6 | 79.5 | 20.1 | >= 10 | 100.0 | Passed |
| 31-40 | no gear | 6 | 58.4 | 39.0 | 19.3 | >= 10 | 64.7 | Passed |
| 31-40 | mid gear | 6 | 75.1 | 49.4 | 25.7 | >= 10 | 79.1 | Passed |
| 31-40 | best gear | 6 | 83.3 | 51.8 | 31.4 | >= 10 | 88.4 | Passed |
| 31-40 | fully modded gear | 6 | 98.5 | 70.5 | 28.1 | >= 10 | 98.9 | Passed |
| 41-53 | no gear | 6 | 53.0 | 41.2 | 11.7 | >= 10 | 63.2 | Passed |
| 41-53 | mid gear | 6 | 67.8 | 52.2 | 15.5 | >= 10 | 75.8 | Passed |
| 41-53 | best gear | 6 | 80.8 | 59.8 | 21.0 | >= 10 | 82.6 | Passed |
| 41-53 | fully modded gear | 6 | 98.9 | 81.2 | 17.7 | >= 10 | 98.7 | Passed |

### The single rotation found for each band and gear tier, searched across the band's sampled recipes (average quality in points)

| Band | Gear | Best single rotation for the band | Training | Selection | Hold-out | Adaptive on these recipes |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | rebond > vibro_hone > rebond > vibro_hone x3 > rebond > vibro_hone > ramp_plasma > hydro_press x2 | 50.3 | 51.4 | 50.5 | 63.0 |
| 1-10 | mid gear | hydro_press x2 > vibro_hone x6 > rebond > ramp_plasma > hydro_press x2 > ramp_plasma x2 > hydro_press | 74.8 | 72.4 | 74.8 | 88.9 |
| 1-10 | best gear | vibro_hone x7 > cryo_quench > ramp_plasma > hydro_press x3 | 89.3 | 89.5 | 89.6 | 97.6 |
| 1-10 | fully modded gear | vibro_hone x8 > ramp_plasma > hydro_press x3 | 100.0 | 100.0 | 100.0 | 99.9 |
| 11-20 | no gear | vibro_hone > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone > cryo_quench > vibro_hone > hydro_press > vibro_hone > cryo_quench > hydro_press > cryo_quench > vibro_hone > hydro_press > rebond > hydro_press > vibro_hone > rebond > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone | 39.6 | 39.7 | 41.5 | 59.8 |
| 11-20 | mid gear | vibro_hone > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone x3 > cryo_quench x2 > vibro_hone > rebond > ramp_plasma > vibro_hone > rebond > hydro_press x2 > vibro_hone > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone | 57.7 | 57.4 | 56.8 | 83.5 |
| 11-20 | best gear | vibro_hone x3 > hydro_press > vibro_hone x4 > ramp_plasma > hydro_press > cryo_quench > ramp_plasma > rebond > ramp_plasma > cryo_quench > hydro_press > cryo_quench > ramp_plasma > vibro_hone > hydro_press x2 > vibro_hone x2 | 65.5 | 65.1 | 65.2 | 95.1 |
| 11-20 | fully modded gear | vibro_hone x6 > ramp_plasma > vibro_hone > ramp_plasma > hydro_press > cryo_quench > vibro_hone > ramp_plasma > hydro_press x2 | 99.8 | 99.8 | 99.9 | 99.9 |
| 21-30 | no gear | ramp_plasma > cryo_quench > vibro_hone > ramp_plasma > hydro_press x2 > vibro_hone > cryo_quench > hydro_press > cryo_quench > rebond > ramp_plasma > hydro_press > cryo_quench > vibro_hone | 37.4 | 37.9 | 38.7 | 62.5 |
| 21-30 | mid gear | ramp_plasma > hydro_press > vibro_hone x2 > ramp_plasma > hydro_press > cryo_quench > hydro_press > cryo_quench > hydro_press > cryo_quench > vibro_hone > hydro_press > ramp_plasma > cryo_quench > vibro_hone > rebond > vibro_hone x2 > rebond > hydro_press > vibro_hone | 49.1 | 51.2 | 49.6 | 84.7 |
| 21-30 | best gear | ramp_plasma > vibro_hone > hydro_press > vibro_hone x2 > ramp_plasma > hydro_press > cryo_quench x2 > vibro_hone > cryo_quench > hydro_press > rebond > cryo_quench > ramp_plasma x2 > hydro_press > ramp_plasma > rebond > cryo_quench > hydro_press > cryo_quench > vibro_hone > rebond > vibro_hone > rebond > vibro_hone x2 > ramp_plasma | 64.0 | 64.3 | 63.9 | 93.6 |
| 21-30 | fully modded gear | ramp_plasma > vibro_hone x4 > ramp_plasma > hydro_press > cryo_quench > vibro_hone > ramp_plasma > hydro_press > ramp_plasma > cryo_quench > vibro_hone | 80.1 | 79.6 | 79.5 | 99.6 |
| 31-40 | no gear | vibro_hone x2 > ramp_plasma > hydro_press > ramp_plasma > hydro_press > cryo_quench > hydro_press > cryo_quench x2 > ramp_plasma x2 > hydro_press > cryo_quench > hydro_press > cryo_quench > vibro_hone x2 > rebond > vibro_hone x2 | 38.9 | 39.6 | 39.0 | 58.4 |
| 31-40 | mid gear | hydro_press > vibro_hone > ramp_plasma > hydro_press > vibro_hone x2 > cryo_quench > ramp_plasma x2 > hydro_press > cryo_quench > hydro_press > cryo_quench > rebond x3 > hydro_press > cryo_quench > vibro_hone x2 > rebond > vibro_hone > rebond > hydro_press > vibro_hone x2 | 47.1 | 48.9 | 49.4 | 75.1 |
| 31-40 | best gear | rebond > vibro_hone x2 > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > hydro_press > cryo_quench > ramp_plasma > hydro_press > vibro_hone > cryo_quench > hydro_press > cryo_quench > rebond > ramp_plasma x2 > hydro_press > rebond > vibro_hone > rebond > vibro_hone | 53.7 | 55.6 | 51.8 | 83.3 |
| 31-40 | fully modded gear | hydro_press > vibro_hone > ramp_plasma > vibro_hone x2 > ramp_plasma > hydro_press > ramp_plasma > vibro_hone x2 > ramp_plasma > cryo_quench > vibro_hone x2 > cryo_quench > vibro_hone > hydro_press | 71.2 | 71.2 | 70.5 | 98.5 |
| 41-53 | no gear | rebond > hydro_press > ramp_plasma > hydro_press x2 > ramp_plasma > rebond x2 > vibro_hone > hydro_press > cryo_quench > hydro_press > ramp_plasma > cryo_quench > vibro_hone > hydro_press > cryo_quench > hydro_press > cryo_quench > vibro_hone > hydro_press x2 > vibro_hone x4 | 39.2 | 41.3 | 41.2 | 53.0 |
| 41-53 | mid gear | hydro_press > vibro_hone x2 > ramp_plasma > hydro_press x2 > cryo_quench > hydro_press > rebond > cryo_quench > ramp_plasma > hydro_press > rebond > vibro_hone > cryo_quench > ramp_plasma > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone x4 | 52.8 | 53.7 | 52.2 | 67.8 |
| 41-53 | best gear | hydro_press x2 > vibro_hone x2 > ramp_plasma > hydro_press > cryo_quench x2 > hydro_press > vibro_hone > cryo_quench > hydro_press > ramp_plasma > rebond > vibro_hone x3 > cryo_quench > ramp_plasma > hydro_press x2 > vibro_hone > rebond > vibro_hone x2 > rebond x2 > vibro_hone > rebond > vibro_hone | 58.6 | 59.5 | 59.8 | 80.8 |
| 41-53 | fully modded gear | hydro_press x2 > vibro_hone > cryo_quench > vibro_hone x2 > rebond > ramp_plasma > vibro_hone x3 > ramp_plasma > vibro_hone > cryo_quench > ramp_plasma > hydro_press > cryo_quench > vibro_hone x2 | 79.3 | 80.1 | 81.2 | 98.9 |

### Reported metric, not gated: the best fixed rotation for each recipe alone, and what the band's single rotation scores on the same recipe (average quality in points)

| Band | Gear | Recipe | Best rotation for this recipe alone (reported) | Its hold-out | The single rotation on this recipe | Adaptive on this recipe |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | sm_gloves (level 3) | vibro_hone > hydro_press x3 > vibro_hone x2 > hydro_press > vibro_hone x3 > hydro_press x2 | 67.5 | 38.2 | 72.8 |
| 1-10 | no gear | b_twinpistol (level 6) | hydro_press x2 > vibro_hone x8 > hydro_press x3 | 59.0 | 34.0 | 60.2 |
| 1-10 | no gear | os_hidevest (level 8) | vibro_hone x2 > hydro_press > vibro_hone x2 > ramp_plasma > hydro_press x2 > ramp_plasma > hydro_press > vibro_hone > hydro_press x4 | 67.9 | 67.3 | 68.1 |
| 1-10 | no gear | sp_crestvis (level 8) | rebond > hydro_press x2 > vibro_hone x3 > hydro_press > vibro_hone > rebond > vibro_hone > ramp_plasma > hydro_press x2 | 65.9 | 61.2 | 59.3 |
| 1-10 | no gear | advent_bracer (level 10) | hydro_press x3 > vibro_hone > hydro_press > vibro_hone > rebond > vibro_hone x3 > ramp_plasma > hydro_press x3 | 59.3 | 61.9 | 56.5 |
| 1-10 | no gear | chef_ring_1 (level 10) | hydro_press x2 > vibro_hone x6 > ramp_plasma x2 > vibro_hone > hydro_press x2 | 59.7 | 40.4 | 60.9 |
| 1-10 | mid gear | sm_gloves (level 3) | hydro_press x3 > vibro_hone > hydro_press > vibro_hone x5 > hydro_press x2 | 86.7 | 83.4 | 86.3 |
| 1-10 | mid gear | b_twinpistol (level 6) | hydro_press x2 > vibro_hone x9 > ramp_plasma > hydro_press x2 | 74.9 | 49.4 | 71.7 |
| 1-10 | mid gear | os_hidevest (level 8) | hydro_press > vibro_hone > hydro_press > vibro_hone > hydro_press > vibro_hone x2 > hydro_press > vibro_hone > ramp_plasma > hydro_press > ramp_plasma > hydro_press | 91.4 | 77.3 | 95.6 |
| 1-10 | mid gear | sp_crestvis (level 8) | hydro_press x3 > vibro_hone x2 > hydro_press > vibro_hone x4 > hydro_press x2 | 91.4 | 88.7 | 82.4 |
| 1-10 | mid gear | advent_bracer (level 10) | vibro_hone > hydro_press > vibro_hone x3 > rebond > vibro_hone > ramp_plasma > vibro_hone x2 > ramp_plasma > hydro_press x2 > vibro_hone > hydro_press x3 | 100.0 | 92.2 | 99.8 |
| 1-10 | mid gear | chef_ring_1 (level 10) | hydro_press > vibro_hone x4 > ramp_plasma > rebond > vibro_hone x2 > ramp_plasma > hydro_press > vibro_hone x5 > hydro_press | 99.4 | 57.7 | 97.6 |
| 1-10 | best gear | sm_gloves (level 3) | hydro_press x2 > vibro_hone x6 > hydro_press > vibro_hone > hydro_press x2 | 99.9 | 99.3 | 99.9 |
| 1-10 | best gear | b_twinpistol (level 6) | hydro_press > vibro_hone x10 > ramp_plasma x2 > hydro_press | 93.4 | 63.3 | 86.7 |
| 1-10 | best gear | os_hidevest (level 8) | vibro_hone > hydro_press > vibro_hone x4 > hydro_press x2 > vibro_hone > hydro_press x3 | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | sp_crestvis (level 8) | rebond > hydro_press x2 > vibro_hone x6 > rebond > hydro_press > vibro_hone > ramp_plasma > hydro_press x2 | 100.0 | 99.7 | 99.7 |
| 1-10 | best gear | advent_bracer (level 10) | vibro_hone > hydro_press > vibro_hone x4 > rebond > vibro_hone > ramp_plasma > vibro_hone > hydro_press x5 | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | chef_ring_1 (level 10) | hydro_press > vibro_hone x6 > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone > hydro_press > rebond > vibro_hone x2 > hydro_press > rebond > ramp_plasma > vibro_hone > hydro_press | 100.0 | 75.2 | 99.4 |
| 1-10 | fully modded gear | sm_gloves (level 3) | vibro_hone x2 > hydro_press > vibro_hone x3 > hydro_press x2 | 100.0 | 100.0 | 99.7 |
| 1-10 | fully modded gear | b_twinpistol (level 6) | vibro_hone x6 > hydro_press > vibro_hone x2 > hydro_press | 100.0 | 100.0 | 99.9 |
| 1-10 | fully modded gear | os_hidevest (level 8) | vibro_hone > hydro_press > vibro_hone x3 > hydro_press x3 | 100.0 | 100.0 | 99.9 |
| 1-10 | fully modded gear | sp_crestvis (level 8) | vibro_hone > hydro_press > vibro_hone x4 > hydro_press x2 | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | advent_bracer (level 10) | vibro_hone > hydro_press > vibro_hone x4 > hydro_press x3 | 100.0 | 100.0 | 99.9 |
| 1-10 | fully modded gear | chef_ring_1 (level 10) | vibro_hone x3 > hydro_press > vibro_hone x4 > ramp_plasma > hydro_press | 100.0 | 100.0 | 100.0 |
| 11-20 | no gear | sith_staff (level 12) | hydro_press > vibro_hone > hydro_press > vibro_hone x2 > hydro_press > ramp_plasma > vibro_hone > hydro_press x2 > vibro_hone > hydro_press | 32.0 | 0.0 | 26.9 |
| 11-20 | no gear | val_cap (level 14) | rebond > vibro_hone > hydro_press x3 > vibro_hone > hydro_press > vibro_hone x4 > hydro_press | 71.3 | 58.2 | 64.3 |
| 11-20 | no gear | vet_chn_ring (level 16) | hydro_press x2 > rebond > vibro_hone x7 > rebond > hydro_press > vibro_hone x2 > hydro_press x4 | 77.6 | 38.0 | 65.2 |
| 11-20 | no gear | sith_shuriken (level 18) | hydro_press x2 > vibro_hone x2 > ramp_plasma x2 > vibro_hone > hydro_press > ramp_plasma > hydro_press > vibro_hone x4 > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > hydro_press | 25.0 | 0.0 | 19.4 |
| 11-20 | no gear | frontier_ring (level 20) | vibro_hone x2 > hydro_press > vibro_hone > cryo_quench > vibro_hone > hydro_press > cryo_quench > hydro_press > vibro_hone x2 > cryo_quench > hydro_press > cryo_quench > ramp_plasma > rebond > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone | 84.2 | 75.7 | 95.5 |
| 11-20 | no gear | chef_ring_2 (level 20) | hydro_press > vibro_hone > hydro_press > vibro_hone x2 > hydro_press > vibro_hone x2 > ramp_plasma > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone x3 | 92.3 | 76.9 | 87.1 |
| 11-20 | mid gear | sith_staff (level 12) | hydro_press > vibro_hone x4 > ramp_plasma > vibro_hone x3 > ramp_plasma x2 > hydro_press > vibro_hone > ramp_plasma > hydro_press | 72.4 | 18.5 | 47.5 |
| 11-20 | mid gear | val_cap (level 14) | vibro_hone x2 > rebond > vibro_hone x3 > rebond > vibro_hone > ramp_plasma > vibro_hone > hydro_press x3 > vibro_hone > hydro_press x2 > vibro_hone > hydro_press x2 | 100.0 | 99.8 | 99.9 |
| 11-20 | mid gear | vet_chn_ring (level 16) | vibro_hone x2 > rebond > vibro_hone x6 > rebond > ramp_plasma > hydro_press > vibro_hone > rebond > vibro_hone x2 > hydro_press x2 > vibro_hone > hydro_press x2 | 99.9 | 62.6 | 99.8 |
| 11-20 | mid gear | sith_shuriken (level 18) | hydro_press > vibro_hone x3 > ramp_plasma > vibro_hone x2 > hydro_press > ramp_plasma > vibro_hone x2 > ramp_plasma > hydro_press x2 | 64.0 | 0.0 | 67.6 |
| 11-20 | mid gear | frontier_ring (level 20) | vibro_hone > hydro_press > vibro_hone x7 > hydro_press > vibro_hone > ramp_plasma > rebond > vibro_hone x3 > rebond > vibro_hone > hydro_press x3 | 100.0 | 84.6 | 99.2 |
| 11-20 | mid gear | chef_ring_2 (level 20) | hydro_press > vibro_hone x7 > ramp_plasma > rebond > vibro_hone x2 > ramp_plasma > hydro_press > vibro_hone x2 > hydro_press x2 | 99.5 | 75.2 | 86.9 |
| 11-20 | best gear | sith_staff (level 12) | hydro_press > vibro_hone x3 > ramp_plasma > vibro_hone x3 > ramp_plasma > vibro_hone x3 > ramp_plasma > rebond > ramp_plasma > vibro_hone > ramp_plasma > hydro_press > vibro_hone > hydro_press | 100.0 | 60.7 | 96.9 |
| 11-20 | best gear | val_cap (level 14) | vibro_hone x3 > rebond > vibro_hone x3 > hydro_press > vibro_hone > hydro_press x2 > vibro_hone > hydro_press x2 > ramp_plasma > hydro_press | 100.0 | 99.5 | 99.9 |
| 11-20 | best gear | vet_chn_ring (level 16) | vibro_hone x2 > rebond > vibro_hone x7 > ramp_plasma > hydro_press x2 > vibro_hone x2 > rebond > hydro_press x2 | 100.0 | 68.8 | 99.6 |
| 11-20 | best gear | sith_shuriken (level 18) | hydro_press x2 > vibro_hone x3 > ramp_plasma x2 > vibro_hone x2 > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > hydro_press x2 | 82.0 | 0.0 | 76.3 |
| 11-20 | best gear | frontier_ring (level 20) | hydro_press > vibro_hone x9 > ramp_plasma > hydro_press > rebond > vibro_hone x3 > hydro_press | 100.0 | 78.3 | 99.7 |
| 11-20 | best gear | chef_ring_2 (level 20) | hydro_press > vibro_hone x7 > ramp_plasma > hydro_press > vibro_hone x2 > ramp_plasma > rebond > vibro_hone x2 > hydro_press x2 | 100.0 | 83.8 | 97.9 |
| 11-20 | fully modded gear | sith_staff (level 12) | vibro_hone > hydro_press > vibro_hone x3 > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone > hydro_press | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | val_cap (level 14) | vibro_hone x2 > hydro_press > vibro_hone x3 > hydro_press x2 | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | vet_chn_ring (level 16) | vibro_hone > hydro_press > vibro_hone x6 > hydro_press | 100.0 | 99.8 | 99.7 |
| 11-20 | fully modded gear | sith_shuriken (level 18) | vibro_hone > hydro_press > vibro_hone x2 > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone x2 > hydro_press | 100.0 | 100.0 | 99.9 |
| 11-20 | fully modded gear | frontier_ring (level 20) | vibro_hone > hydro_press > vibro_hone x6 > hydro_press | 100.0 | 99.5 | 100.0 |
| 11-20 | fully modded gear | chef_ring_2 (level 20) | hydro_press > vibro_hone x7 > hydro_press | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | vet_twinelec (level 22) | ramp_plasma > hydro_press > vibro_hone x6 > hydro_press > vibro_hone > cryo_quench > hydro_press > cryo_quench > vibro_hone x2 | 61.5 | 30.2 | 62.0 |
| 21-30 | no gear | vet_bul_belt (level 24) | ramp_plasma > hydro_press x4 > vibro_hone > hydro_press > vibro_hone > hydro_press x2 | 0.0 | 0.0 | 0.0 |
| 21-30 | no gear | prm_skm_ring (level 26) | ramp_plasma > vibro_hone x2 > ramp_plasma > hydro_press x3 > vibro_hone > ramp_plasma > cryo_quench > vibro_hone x2 > ramp_plasma > hydro_press > cryo_quench > vibro_hone x2 | 78.3 | 44.8 | 81.2 |
| 21-30 | no gear | cnt_neck (level 27) | vibro_hone > ramp_plasma x2 > hydro_press x2 > vibro_hone x2 > cryo_quench > hydro_press > cryo_quench > vibro_hone x2 > rebond > vibro_hone x2 | 72.6 | 58.7 | 75.9 |
| 21-30 | no gear | tg_mantle (level 29) | hydro_press > vibro_hone x2 > ramp_plasma > rebond > vibro_hone x2 > ramp_plasma x2 > hydro_press x2 > cryo_quench > vibro_hone > hydro_press > cryo_quench > hydro_press > cryo_quench > vibro_hone x3 | 86.3 | 53.1 | 98.9 |
| 21-30 | no gear | chef_necklace_3 (level 30) | rebond > vibro_hone x2 > ramp_plasma x2 > hydro_press x2 > cryo_quench > hydro_press > vibro_hone > ramp_plasma > rebond > cryo_quench > vibro_hone | 55.5 | 45.4 | 56.8 |
| 21-30 | mid gear | vet_twinelec (level 22) | ramp_plasma > vibro_hone > hydro_press > vibro_hone x7 > rebond > ramp_plasma > vibro_hone x2 > rebond > hydro_press > cryo_quench > hydro_press > vibro_hone x2 | 91.4 | 20.5 | 78.9 |
| 21-30 | mid gear | vet_bul_belt (level 24) | ramp_plasma > hydro_press > vibro_hone x2 > ramp_plasma > hydro_press x2 > cryo_quench > vibro_hone > hydro_press > cryo_quench x2 > vibro_hone > ramp_plasma > hydro_press > rebond > cryo_quench > vibro_hone x3 | 91.5 | 95.5 | 85.3 |
| 21-30 | mid gear | prm_skm_ring (level 26) | ramp_plasma > vibro_hone x2 > ramp_plasma > hydro_press > vibro_hone x2 > hydro_press > ramp_plasma > vibro_hone > ramp_plasma > hydro_press > vibro_hone > cryo_quench > hydro_press x2 > rebond > vibro_hone | 88.1 | 47.0 | 80.2 |
| 21-30 | mid gear | cnt_neck (level 27) | hydro_press > ramp_plasma > vibro_hone x3 > ramp_plasma > hydro_press > vibro_hone x2 > ramp_plasma > hydro_press > vibro_hone > cryo_quench > rebond x2 > vibro_hone > hydro_press > cryo_quench > vibro_hone x2 | 94.2 | 46.1 | 87.2 |
| 21-30 | mid gear | tg_mantle (level 29) | vibro_hone x4 > ramp_plasma > rebond > vibro_hone x3 > ramp_plasma > vibro_hone > rebond > vibro_hone > hydro_press > vibro_hone > hydro_press x2 > vibro_hone > hydro_press x2 > ramp_plasma > hydro_press | 100.0 | 38.5 | 100.0 |
| 21-30 | mid gear | chef_necklace_3 (level 30) | hydro_press x2 > vibro_hone > ramp_plasma > cryo_quench > vibro_hone > ramp_plasma > vibro_hone x2 > ramp_plasma > hydro_press > ramp_plasma > cryo_quench > vibro_hone | 91.2 | 49.9 | 76.5 |
| 21-30 | best gear | vet_twinelec (level 22) | ramp_plasma > hydro_press > vibro_hone x9 > ramp_plasma > rebond > vibro_hone x3 > ramp_plasma > vibro_hone > hydro_press | 100.0 | 24.3 | 95.6 |
| 21-30 | best gear | vet_bul_belt (level 24) | ramp_plasma > hydro_press > vibro_hone x2 > ramp_plasma > hydro_press x2 > cryo_quench > vibro_hone > hydro_press > ramp_plasma > vibro_hone > cryo_quench > vibro_hone x2 > ramp_plasma > hydro_press > vibro_hone > hydro_press > vibro_hone | 93.2 | 90.8 | 99.8 |
| 21-30 | best gear | prm_skm_ring (level 26) | ramp_plasma > vibro_hone x2 > ramp_plasma > hydro_press > vibro_hone x3 > ramp_plasma > vibro_hone x2 > ramp_plasma > hydro_press > cryo_quench > hydro_press > rebond > ramp_plasma > cryo_quench > vibro_hone > cryo_quench > vibro_hone | 91.1 | 88.3 | 89.5 |
| 21-30 | best gear | cnt_neck (level 27) | vibro_hone > ramp_plasma > vibro_hone x3 > ramp_plasma > hydro_press > vibro_hone x3 > ramp_plasma > hydro_press > cryo_quench > vibro_hone > ramp_plasma > rebond > hydro_press > ramp_plasma > cryo_quench > vibro_hone | 98.6 | 65.9 | 86.1 |
| 21-30 | best gear | tg_mantle (level 29) | vibro_hone x4 > ramp_plasma > hydro_press > vibro_hone x2 > rebond > vibro_hone x2 > ramp_plasma > hydro_press > vibro_hone > hydro_press x2 > rebond > vibro_hone > hydro_press | 99.9 | 81.4 | 100.0 |
| 21-30 | best gear | chef_necklace_3 (level 30) | hydro_press > vibro_hone > ramp_plasma > hydro_press > cryo_quench > vibro_hone > ramp_plasma > vibro_hone x2 > ramp_plasma > hydro_press > ramp_plasma > vibro_hone > ramp_plasma > cryo_quench > rebond > vibro_hone | 82.1 | 32.5 | 90.9 |
| 21-30 | fully modded gear | vet_twinelec (level 22) | ramp_plasma > vibro_hone x8 > hydro_press x2 | 100.0 | 54.0 | 99.8 |
| 21-30 | fully modded gear | vet_bul_belt (level 24) | ramp_plasma > vibro_hone x2 > ramp_plasma > hydro_press > vibro_hone x4 > ramp_plasma > hydro_press x2 | 100.0 | 98.6 | 99.8 |
| 21-30 | fully modded gear | prm_skm_ring (level 26) | ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone x3 > ramp_plasma > vibro_hone x2 > ramp_plasma > hydro_press x2 | 100.0 | 84.5 | 99.9 |
| 21-30 | fully modded gear | cnt_neck (level 27) | ramp_plasma > hydro_press > vibro_hone > cryo_quench > vibro_hone > ramp_plasma > vibro_hone x4 > ramp_plasma > vibro_hone x2 > hydro_press | 100.0 | 88.5 | 99.9 |
| 21-30 | fully modded gear | tg_mantle (level 29) | vibro_hone x5 > ramp_plasma > hydro_press > vibro_hone > hydro_press x2 | 100.0 | 77.5 | 99.9 |
| 21-30 | fully modded gear | chef_necklace_3 (level 30) | vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > vibro_hone > hydro_press | 100.0 | 73.7 | 98.1 |
| 31-40 | no gear | sh_charm (level 31) | rebond > vibro_hone x4 > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > hydro_press x2 > cryo_quench > hydro_press > cryo_quench > ramp_plasma > hydro_press > rebond > hydro_press > vibro_hone x2 | 72.8 | 51.9 | 79.5 |
| 31-40 | no gear | prm_bul_cloak (level 33) | hydro_press > vibro_hone x2 > ramp_plasma > hydro_press > ramp_plasma > hydro_press x2 > cryo_quench > hydro_press > ramp_plasma > rebond > cryo_quench > hydro_press > cryo_quench > vibro_hone > rebond > vibro_hone | 68.4 | 51.3 | 63.3 |
| 31-40 | no gear | proto_staff (level 35) | hydro_press > vibro_hone x3 > ramp_plasma > hydro_press > cryo_quench > vibro_hone x3 > ramp_plasma > cryo_quench x2 > vibro_hone > hydro_press > cryo_quench > rebond > vibro_hone x2 | 87.4 | 19.7 | 63.3 |
| 31-40 | no gear | asc_skm_neck (level 37) | vibro_hone > ramp_plasma > hydro_press > ramp_plasma > hydro_press > cryo_quench > vibro_hone > ramp_plasma > rebond > hydro_press > cryo_quench > vibro_hone | 48.8 | 25.9 | 41.6 |
| 31-40 | no gear | majestic_ring001 (level 40) | rebond > hydro_press > vibro_hone > ramp_plasma x2 > hydro_press > ramp_plasma > hydro_press > vibro_hone > cryo_quench > hydro_press > cryo_quench > vibro_hone x2 | 48.7 | 26.1 | 48.6 |
| 31-40 | no gear | chef_cloak_4 (level 40) | hydro_press > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > hydro_press x2 > cryo_quench > vibro_hone > ramp_plasma > hydro_press > cryo_quench > vibro_hone x2 > rebond > vibro_hone x2 | 62.1 | 59.4 | 54.0 |
| 31-40 | mid gear | sh_charm (level 31) | vibro_hone x5 > ramp_plasma x2 > hydro_press x2 > vibro_hone > cryo_quench > vibro_hone > ramp_plasma x2 > rebond > ramp_plasma > hydro_press > cryo_quench > vibro_hone | 92.6 | 40.1 | 81.7 |
| 31-40 | mid gear | prm_bul_cloak (level 33) | hydro_press > vibro_hone x2 > ramp_plasma > cryo_quench > hydro_press > vibro_hone > ramp_plasma x2 > hydro_press > cryo_quench > hydro_press > cryo_quench > ramp_plasma > rebond > vibro_hone > ramp_plasma > hydro_press > vibro_hone x2 | 89.8 | 89.0 | 90.7 |
| 31-40 | mid gear | proto_staff (level 35) | vibro_hone x3 > ramp_plasma > cryo_quench > hydro_press > cryo_quench > vibro_hone x2 > ramp_plasma > rebond > vibro_hone x3 > ramp_plasma > vibro_hone x3 > ramp_plasma > hydro_press | 81.4 | 38.1 | 71.9 |
| 31-40 | mid gear | asc_skm_neck (level 37) | hydro_press x2 > ramp_plasma > cryo_quench > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma x2 > hydro_press > rebond > cryo_quench > vibro_hone | 73.2 | 39.2 | 67.6 |
| 31-40 | mid gear | majestic_ring001 (level 40) | hydro_press > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > hydro_press > cryo_quench > hydro_press > cryo_quench > hydro_press > vibro_hone x3 | 59.1 | 16.0 | 64.7 |
| 31-40 | mid gear | chef_cloak_4 (level 40) | hydro_press x2 > ramp_plasma > hydro_press > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > hydro_press > cryo_quench > hydro_press > cryo_quench > vibro_hone > rebond > hydro_press > vibro_hone > cryo_quench > rebond > vibro_hone x3 | 78.8 | 74.0 | 74.0 |
| 31-40 | best gear | sh_charm (level 31) | hydro_press > vibro_hone x3 > ramp_plasma > vibro_hone x4 > rebond > vibro_hone x2 > ramp_plasma x2 > vibro_hone > cryo_quench > vibro_hone > ramp_plasma x2 > hydro_press | 88.8 | 45.0 | 92.8 |
| 31-40 | best gear | prm_bul_cloak (level 33) | vibro_hone x3 > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > hydro_press > cryo_quench > hydro_press > ramp_plasma > cryo_quench > ramp_plasma x2 > hydro_press > rebond > vibro_hone x3 | 95.4 | 82.6 | 98.0 |
| 31-40 | best gear | proto_staff (level 35) | vibro_hone x4 > ramp_plasma > vibro_hone x3 > hydro_press > ramp_plasma > hydro_press > vibro_hone > cryo_quench > ramp_plasma > rebond > vibro_hone x3 > ramp_plasma > hydro_press | 96.5 | 27.5 | 75.4 |
| 31-40 | best gear | asc_skm_neck (level 37) | hydro_press x2 > ramp_plasma > cryo_quench > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > vibro_hone > ramp_plasma > vibro_hone > hydro_press > rebond > vibro_hone x2 > cryo_quench > vibro_hone | 66.9 | 39.6 | 75.8 |
| 31-40 | best gear | majestic_ring001 (level 40) | vibro_hone x2 > ramp_plasma x2 > hydro_press > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > hydro_press > ramp_plasma > cryo_quench > hydro_press > cryo_quench > vibro_hone x3 | 84.1 | 30.3 | 70.3 |
| 31-40 | best gear | chef_cloak_4 (level 40) | hydro_press > vibro_hone > ramp_plasma > vibro_hone x3 > ramp_plasma > vibro_hone > ramp_plasma > hydro_press x2 > cryo_quench > hydro_press > ramp_plasma > cryo_quench > hydro_press > cryo_quench > rebond > vibro_hone x2 > rebond > vibro_hone x4 | 98.8 | 86.1 | 87.3 |
| 31-40 | fully modded gear | sh_charm (level 31) | hydro_press > vibro_hone x3 > ramp_plasma > vibro_hone x3 > ramp_plasma > vibro_hone x2 > hydro_press x2 | 100.0 | 44.3 | 100.0 |
| 31-40 | fully modded gear | prm_bul_cloak (level 33) | hydro_press > vibro_hone > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone > hydro_press > ramp_plasma > vibro_hone > hydro_press | 100.0 | 99.0 | 100.0 |
| 31-40 | fully modded gear | proto_staff (level 35) | vibro_hone x3 > ramp_plasma > vibro_hone x4 > ramp_plasma > vibro_hone > hydro_press > vibro_hone > hydro_press | 100.0 | 26.9 | 99.3 |
| 31-40 | fully modded gear | asc_skm_neck (level 37) | hydro_press > vibro_hone > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > vibro_hone > ramp_plasma > vibro_hone > hydro_press > cryo_quench > vibro_hone | 96.1 | 83.8 | 96.0 |
| 31-40 | fully modded gear | majestic_ring001 (level 40) | hydro_press > vibro_hone > ramp_plasma > cryo_quench > vibro_hone > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > vibro_hone x2 > ramp_plasma > hydro_press | 97.4 | 69.5 | 96.9 |
| 31-40 | fully modded gear | chef_cloak_4 (level 40) | vibro_hone > ramp_plasma > hydro_press > vibro_hone x2 > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone > hydro_press x2 | 100.0 | 99.5 | 99.1 |
| 41-53 | no gear | asc_skm_tunic (level 42) | ramp_plasma > hydro_press x4 > vibro_hone x4 > cryo_quench > hydro_press > cryo_quench > hydro_press > ramp_plasma > rebond > cryo_quench > vibro_hone > rebond > vibro_hone x2 | 83.5 | 60.0 | 74.5 |
| 41-53 | no gear | warband_helm (level 45) | hydro_press x4 > vibro_hone x2 > cryo_quench > ramp_plasma > rebond > hydro_press > vibro_hone > rebond > vibro_hone > cryo_quench > rebond > hydro_press > cryo_quench > rebond > vibro_hone x5 | 78.3 | 18.5 | 65.2 |
| 41-53 | no gear | eternal_leggings (level 50) | hydro_press > vibro_hone > ramp_plasma > hydro_press x3 > vibro_hone x2 > ramp_plasma > hydro_press > cryo_quench > hydro_press > cryo_quench > rebond > vibro_hone x2 | 52.0 | 36.5 | 41.5 |
| 41-53 | no gear | pit_cestus (level 50) | hydro_press > ramp_plasma > hydro_press x3 > vibro_hone > cryo_quench > hydro_press > vibro_hone x2 > ramp_plasma > cryo_quench > hydro_press > cryo_quench > rebond > vibro_hone x2 | 65.7 | 45.6 | 54.7 |
| 41-53 | no gear | rhydelalphacr (level 50) | ramp_plasma > vibro_hone > hydro_press x2 > ramp_plasma > rebond > hydro_press > ramp_plasma > hydro_press > vibro_hone > hydro_press > vibro_hone > cryo_quench > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone x3 | 58.6 | 50.3 | 56.7 |
| 41-53 | no gear | imm_belt (level 52) | hydro_press x2 > ramp_plasma > hydro_press > ramp_plasma > hydro_press > cryo_quench > hydro_press > vibro_hone > ramp_plasma > cryo_quench > hydro_press > cryo_quench > vibro_hone x3 | 41.3 | 36.6 | 25.1 |
| 41-53 | mid gear | asc_skm_tunic (level 42) | ramp_plasma > hydro_press x3 > rebond > vibro_hone x4 > ramp_plasma > hydro_press > cryo_quench > hydro_press > vibro_hone > cryo_quench > vibro_hone x2 > rebond > vibro_hone x2 | 98.5 | 84.6 | 94.4 |
| 41-53 | mid gear | warband_helm (level 45) | hydro_press x4 > cryo_quench > hydro_press > rebond x3 > ramp_plasma > vibro_hone > rebond > vibro_hone x2 > ramp_plasma > rebond > vibro_hone x3 > cryo_quench > vibro_hone | 91.1 | 59.1 | 75.5 |
| 41-53 | mid gear | eternal_leggings (level 50) | vibro_hone x2 > ramp_plasma > hydro_press > vibro_hone > hydro_press x2 > vibro_hone > ramp_plasma > hydro_press > cryo_quench > ramp_plasma > rebond > hydro_press > cryo_quench > hydro_press > cryo_quench > vibro_hone x4 | 62.8 | 49.7 | 55.8 |
| 41-53 | mid gear | pit_cestus (level 50) | vibro_hone x2 > cryo_quench > ramp_plasma > hydro_press x2 > ramp_plasma > cryo_quench > hydro_press x2 > vibro_hone x2 > cryo_quench > hydro_press > cryo_quench > hydro_press > ramp_plasma > hydro_press > rebond > vibro_hone x3 | 76.1 | 48.3 | 69.6 |
| 41-53 | mid gear | rhydelalphacr (level 50) | hydro_press > vibro_hone > ramp_plasma > hydro_press > vibro_hone > hydro_press x2 > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > cryo_quench > rebond > hydro_press > vibro_hone x2 > cryo_quench > vibro_hone | 70.9 | 71.8 | 64.7 |
| 41-53 | mid gear | imm_belt (level 52) | vibro_hone > ramp_plasma > hydro_press > ramp_plasma > hydro_press x2 > cryo_quench > vibro_hone > hydro_press > ramp_plasma > cryo_quench > hydro_press > cryo_quench > rebond > vibro_hone x3 | 55.7 | 0.0 | 46.6 |
| 41-53 | best gear | asc_skm_tunic (level 42) | vibro_hone x3 > ramp_plasma > rebond > vibro_hone x2 > ramp_plasma > hydro_press x3 > cryo_quench > vibro_hone x2 > hydro_press > vibro_hone > cryo_quench > hydro_press > rebond > cryo_quench > hydro_press x2 > vibro_hone x4 | 100.0 | 100.0 | 99.7 |
| 41-53 | best gear | warband_helm (level 45) | hydro_press > vibro_hone x2 > rebond > vibro_hone x4 > ramp_plasma > vibro_hone > hydro_press x2 > vibro_hone x2 > rebond > ramp_plasma > hydro_press > rebond > cryo_quench > vibro_hone > hydro_press > vibro_hone > hydro_press x2 > vibro_hone x2 | 97.1 | 51.7 | 90.8 |
| 41-53 | best gear | eternal_leggings (level 50) | hydro_press > vibro_hone x2 > ramp_plasma > hydro_press x2 > vibro_hone > hydro_press > vibro_hone > ramp_plasma > cryo_quench > hydro_press > cryo_quench > rebond > vibro_hone > rebond > cryo_quench > hydro_press > vibro_hone x2 > hydro_press > vibro_hone x2 | 79.9 | 54.0 | 74.3 |
| 41-53 | best gear | pit_cestus (level 50) | hydro_press > vibro_hone x3 > ramp_plasma > rebond > ramp_plasma > cryo_quench > vibro_hone x2 > ramp_plasma > hydro_press x3 > ramp_plasma > cryo_quench > hydro_press > vibro_hone > cryo_quench > vibro_hone | 79.4 | 63.1 | 79.3 |
| 41-53 | best gear | rhydelalphacr (level 50) | hydro_press > vibro_hone > hydro_press > vibro_hone > ramp_plasma > hydro_press x2 > vibro_hone > cryo_quench > hydro_press > vibro_hone x2 > cryo_quench > rebond > vibro_hone > rebond > vibro_hone x2 > rebond x2 > vibro_hone > rebond > vibro_hone | 82.8 | 89.8 | 83.4 |
| 41-53 | best gear | imm_belt (level 52) | hydro_press > vibro_hone > ramp_plasma x2 > hydro_press x2 > cryo_quench > vibro_hone > hydro_press > ramp_plasma > cryo_quench > rebond > vibro_hone x2 > rebond > vibro_hone | 56.4 | 0.0 | 57.2 |
| 41-53 | fully modded gear | asc_skm_tunic (level 42) | vibro_hone x6 > ramp_plasma > vibro_hone > hydro_press x2 > vibro_hone > hydro_press x2 | 100.0 | 99.9 | 99.6 |
| 41-53 | fully modded gear | warband_helm (level 45) | vibro_hone > hydro_press > vibro_hone x7 > hydro_press > vibro_hone > ramp_plasma x2 > hydro_press | 100.0 | 93.6 | 100.0 |
| 41-53 | fully modded gear | eternal_leggings (level 50) | vibro_hone x3 > ramp_plasma > hydro_press > vibro_hone x4 > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > rebond > vibro_hone > cryo_quench > hydro_press x2 > vibro_hone | 99.6 | 94.3 | 99.6 |
| 41-53 | fully modded gear | pit_cestus (level 50) | hydro_press > vibro_hone x4 > ramp_plasma > vibro_hone x3 > ramp_plasma > vibro_hone > hydro_press > ramp_plasma > rebond > hydro_press > cryo_quench > vibro_hone x2 | 99.6 | 81.5 | 99.7 |
| 41-53 | fully modded gear | rhydelalphacr (level 50) | rebond > vibro_hone x6 > ramp_plasma > vibro_hone > ramp_plasma > hydro_press > vibro_hone > rebond > vibro_hone > hydro_press > vibro_hone > hydro_press | 100.0 | 95.5 | 99.5 |
| 41-53 | fully modded gear | imm_belt (level 52) | hydro_press > vibro_hone x2 > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > hydro_press > cryo_quench > hydro_press > ramp_plasma > cryo_quench > vibro_hone x2 | 93.2 | 22.5 | 95.1 |

## Style balance

### Style pairs: how often each style is better, and the average quality (quality points; ties count half a win each)

| Style pair | Gear | Recipes | First better | Second better | First avg quality | Second avg quality | Difference | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Hot Forging (no lines) vs Cold Working (no lines) | no gear | 20 | 40% | 60% | 63.0 | 64.7 | -1.6 | Passed |
| Hot Forging (no lines) vs Cold Working (no lines) | mid gear | 20 | 42% | 57% | 77.9 | 79.3 | -1.4 | Passed |
| Hot Forging (no lines) vs Cold Working (no lines) | best gear | 20 | 50% | 50% | 87.3 | 87.5 | -0.2 | Passed |
| Hot Forging (no lines) vs Cold Working (no lines) | fully modded gear | 20 | 55% | 45% | 99.0 | 98.6 | +0.4 | Passed |
| Hot Forging (both lines) vs Cold Working (both lines) | no gear | 20 | 60% | 40% | 77.3 | 76.3 | +1.1 | Passed |
| Hot Forging (both lines) vs Cold Working (both lines) | mid gear | 20 | 65% | 35% | 95.5 | 93.5 | +2.0 | Passed |
| Hot Forging (both lines) vs Cold Working (both lines) | best gear | 20 | 52% | 48% | 99.5 | 99.4 | +0.0 | Passed |
| Hot Forging (both lines) vs Cold Working (both lines) | fully modded gear | 20 | 50% | 50% | 99.9 | 99.9 | +0.0 | Passed |

## Perks optional

### Rank 50, fully modded gear, no perks: adaptive attempts that reach maximum quality, by recipe band

| Band | Recipes played | Attempts | Reach maximum quality | Make the item | Avg quality of items made | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 6 | 240 | 90% | 100% | 100% | >= 50% | Passed |
| 46-50 | 6 | 240 | 68% | 100% | 95% | >= 50% | Passed |
| 51-53 | 6 | 240 | 41% | 100% | 92% | >= 35% | Passed |

## Length

### Actions per attempt of the adaptive player at level (item craft at level)

| Band | Gear | Recipes played | Mean actions per attempt | Shortest recipe | Longest recipe | Stalled attempts | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 12.0 | 11.6 | 12.9 | 0 | 10-20 | Passed |
| 1-10 | mid gear | 6 | 13.5 | 12.0 | 17.4 | 0 | 10-20 | Passed |
| 1-10 | best gear | 6 | 12.2 | 11.0 | 14.8 | 0 | 10-20 | Passed |
| 1-10 | fully modded gear | 6 | 7.6 | 6.6 | 8.7 | 0 | 10-20 | Reported |
| 11-20 | no gear | 6 | 11.4 | 9.5 | 15.1 | 0 | 10-20 | Passed |
| 11-20 | mid gear | 6 | 13.4 | 9.3 | 17.1 | 0 | 10-20 | Passed |
| 11-20 | best gear | 6 | 12.8 | 8.4 | 17.9 | 0 | 10-20 | Passed |
| 11-20 | fully modded gear | 6 | 8.1 | 6.1 | 10.4 | 0 | 10-20 | Reported |
| 21-30 | no gear | 6 | 11.2 | 9.7 | 12.9 | 0 | 10-20 | Passed |
| 21-30 | mid gear | 6 | 10.9 | 9.7 | 13.2 | 0 | 10-20 | Passed |
| 21-30 | best gear | 6 | 10.7 | 9.4 | 11.7 | 0 | 10-20 | Passed |
| 21-30 | fully modded gear | 6 | 7.3 | 5.2 | 8.9 | 0 | 10-20 | Reported |
| 31-40 | no gear | 6 | 10.9 | 9.8 | 11.9 | 0 | 10-20 | Reported |
| 31-40 | mid gear | 6 | 11.3 | 10.1 | 13.1 | 0 | 10-20 | Passed |
| 31-40 | best gear | 6 | 10.7 | 9.4 | 12.7 | 0 | 10-20 | Passed |
| 31-40 | fully modded gear | 6 | 7.6 | 6.8 | 8.4 | 0 | 10-20 | Reported |
| 41-53 | no gear | 6 | 12.7 | 11.0 | 14.2 | 0 | 10-20 | Reported |
| 41-53 | mid gear | 6 | 13.4 | 11.7 | 15.4 | 0 | 10-20 | Reported |
| 41-53 | best gear | 6 | 13.5 | 12.3 | 14.2 | 0 | 10-20 | Passed |
| 41-53 | fully modded gear | 6 | 8.7 | 6.4 | 10.6 | 0 | 10-20 | Reported |

## Economy parity

### Component stacks consumed per item made, failures included, over the recipes the legacy policy makes at least once in the cell (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Parity rule | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 2.81 | 2.69 | 1.04 | 92% | 98% | 63% | 1% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | mid gear | 6 | 2.73 | 2.70 | 1.01 | 97% | 98% | 89% | 12% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | best gear | 6 | 2.67 | 2.69 | 0.99 | 100% | 98% | 98% | 28% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | fully modded gear | 6 | 2.67 | 2.68 | 0.99 | 100% | 99% | 100% | 79% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | no gear | 6 | 2.68 | 2.64 | 1.02 | 90% | 92% | 60% | 2% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | mid gear | 6 | 2.59 | 2.54 | 1.02 | 95% | 98% | 83% | 13% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | best gear | 6 | 2.53 | 2.51 | 1.01 | 98% | 99% | 95% | 29% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | fully modded gear | 6 | 2.50 | 2.51 | 1.00 | 100% | 100% | 100% | 92% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | no gear | 6 | 3.21 | 3.19 | 1.01 | 83% | 99% | 62% | 8% | yes | 17% / 0% | within 10% | Failed |
| 21-30 | mid gear | 6 | 3.18 | 3.18 | 1.00 | 100% | 100% | 85% | 16% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | best gear | 6 | 3.17 | 3.17 | 1.00 | 100% | 100% | 94% | 29% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | fully modded gear | 6 | 3.17 | 3.18 | 0.99 | 100% | 99% | 100% | 90% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | no gear | 6 | 2.90 | 2.86 | 1.02 | 96% | 99% | 58% | 23% | yes | 0% / 0% | within 10% | Reported |
| 31-40 | mid gear | 6 | 2.84 | 2.87 | 0.99 | 100% | 98% | 75% | 38% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | best gear | 6 | 2.83 | 2.85 | 0.99 | 100% | 99% | 83% | 49% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | fully modded gear | 6 | 2.83 | 2.87 | 0.99 | 100% | 98% | 99% | 88% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | no gear | 6 | 3.00 | 3.03 | 0.99 | 100% | 98% | 59% | 43% | yes | 0% / 17% | within 10% | Reported |
| 41-53 | mid gear | 6 | 3.84 | 3.99 | 0.96 | 100% | 87% | 68% | 56% | yes | 0% / 0% | at most +10% | Reported |
| 41-53 | best gear | 6 | 3.84 | 3.89 | 0.99 | 100% | 95% | 81% | 75% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | fully modded gear | 6 | 3.83 | 3.83 | 1.00 | 100% | 100% | 99% | 91% | yes | 0% / 0% | within 10% | Passed |

### Recipes the legacy policy never makes in the cell, left out of the stack and quality comparison above (they still count in the never-made check)

| Band | Gear | Recipe | Level | New success | New avg quality |
| --- | --- | --- | --- | --- | --- |
| 41-53 | no gear | ImmortalBelt | 52 | 95% | 25% |

## XP rate

### Item crafts: XP per hour against the legacy loop (6.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per craft | Legacy actions per craft | Legacy XP per hour | New XP per craft | New actions per craft | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 596 | 6.0 | 60026 | 1846 | 12.0 | 69379 | 1.16 | - | Passed |
| 1-10 | no gear, 1 enhancement | 452 | 6.8 | 39681 | 1554 | 11.6 | 60124 | 1.52 | - | Passed |
| 1-10 | no gear, 2 enhancements | 307 | 6.5 | 28117 | 1049 | 11.3 | 41840 | 1.49 | - | Passed |
| 1-10 | no gear, 3 enhancements | 224 | 6.9 | 19432 | 637 | 11.2 | 25506 | 1.31 | - | Passed |
| 1-10 | mid gear | 658 | 8.9 | 44484 | 2199 | 13.4 | 73570 | 1.65 | - | Passed |
| 1-10 | mid gear, 1 enhancement | 653 | 8.9 | 44187 | 1890 | 13.5 | 62893 | 1.42 | - | Passed |
| 1-10 | mid gear, 2 enhancements | 598 | 9.6 | 37479 | 1345 | 13.2 | 45975 | 1.23 | - | Passed |
| 1-10 | mid gear, 3 enhancements | 549 | 10.1 | 32620 | 944 | 13.1 | 32412 | 0.99 | - | Passed |
| 1-10 | best gear | 759 | 10.3 | 44051 | 2332 | 12.2 | 85793 | 1.95 | - | Passed |
| 1-10 | best gear, 1 enhancement | 747 | 10.9 | 41318 | 2196 | 13.3 | 74028 | 1.79 | - | Passed |
| 1-10 | best gear, 2 enhancements | 723 | 11.0 | 39355 | 1910 | 14.3 | 60199 | 1.53 | - | Passed |
| 1-10 | best gear, 3 enhancements | 699 | 11.2 | 37569 | 1456 | 14.8 | 44300 | 1.18 | - | Passed |
| 1-10 | fully modded gear | 1069 | 8.4 | 76333 | 2363 | 7.6 | 140666 | 1.84 | - | Passed |
| 1-10 | fully modded gear, 1 enhancement | 1056 | 8.5 | 74415 | 2364 | 8.7 | 122027 | 1.64 | - | Passed |
| 1-10 | fully modded gear, 2 enhancements | 1045 | 8.3 | 75408 | 2364 | 9.5 | 111571 | 1.48 | - | Passed |
| 1-10 | fully modded gear, 3 enhancements | 1052 | 8.8 | 71599 | 2353 | 10.5 | 100429 | 1.40 | - | Passed |
| 11-20 | no gear | 571 | 7.8 | 43682 | 1091 | 11.4 | 43011 | 0.98 | 1.02x | Failed |
| 11-20 | no gear, 1 enhancement | 403 | 7.9 | 30622 | 1009 | 12.0 | 37908 | 1.24 | - | Passed |
| 11-20 | no gear, 2 enhancements | 266 | 7.2 | 22226 | 866 | 12.0 | 32400 | 1.46 | - | Passed |
| 11-20 | no gear, 3 enhancements | 262 | 7.7 | 20475 | 632 | 12.1 | 23611 | 1.15 | - | Passed |
| 11-20 | mid gear | 666 | 11.1 | 35937 | 1290 | 13.4 | 43315 | 1.21 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 653 | 11.9 | 32969 | 1298 | 14.9 | 39266 | 1.19 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 629 | 12.5 | 30090 | 1109 | 15.6 | 32024 | 1.06 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 602 | 13.5 | 26788 | 1016 | 15.9 | 28734 | 1.07 | - | Passed |
| 11-20 | best gear | 767 | 14.5 | 31782 | 1394 | 12.8 | 48839 | 1.54 | - | Passed |
| 11-20 | best gear, 1 enhancement | 758 | 15.1 | 30167 | 1312 | 14.1 | 41926 | 1.39 | - | Passed |
| 11-20 | best gear, 2 enhancements | 746 | 16.0 | 27937 | 1280 | 15.6 | 37032 | 1.33 | - | Passed |
| 11-20 | best gear, 3 enhancements | 753 | 16.5 | 27369 | 1208 | 16.5 | 32901 | 1.20 | - | Passed |
| 11-20 | fully modded gear | 1150 | 14.2 | 48540 | 1439 | 8.1 | 79684 | 1.64 | - | Passed |
| 11-20 | fully modded gear, 1 enhancement | 1122 | 14.1 | 47622 | 1440 | 8.6 | 75256 | 1.58 | - | Passed |
| 11-20 | fully modded gear, 2 enhancements | 1131 | 14.7 | 46302 | 1439 | 9.2 | 70494 | 1.52 | - | Passed |
| 11-20 | fully modded gear, 3 enhancements | 1132 | 14.7 | 46116 | 1392 | 10.8 | 58269 | 1.26 | - | Passed |
| 21-30 | no gear | 638 | 10.2 | 37540 | 1296 | 11.2 | 52249 | 1.39 | - | Passed |
| 21-30 | no gear, 1 enhancement | 618 | 11.5 | 32252 | 1493 | 12.1 | 55631 | 1.72 | - | Passed |
| 21-30 | no gear, 2 enhancements | 593 | 12.5 | 28522 | 1240 | 12.9 | 43145 | 1.51 | - | Passed |
| 21-30 | no gear, 3 enhancements | 577 | 13.4 | 25795 | 756 | 13.3 | 25508 | 0.99 | - | Passed |
| 21-30 | mid gear | 692 | 16.4 | 25252 | 1614 | 10.9 | 66600 | 2.64 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 675 | 16.8 | 24166 | 1656 | 12.8 | 58321 | 2.41 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 667 | 17.4 | 23051 | 1586 | 14.3 | 49894 | 2.16 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 647 | 17.5 | 22142 | 1346 | 15.5 | 39002 | 1.76 | - | Passed |
| 21-30 | best gear | 770 | 21.3 | 21698 | 1696 | 10.7 | 71573 | 3.30 | - | Passed |
| 21-30 | best gear, 1 enhancement | 755 | 21.5 | 21058 | 1710 | 11.9 | 64466 | 3.06 | - | Passed |
| 21-30 | best gear, 2 enhancements | 748 | 22.0 | 20371 | 1685 | 13.3 | 57121 | 2.80 | - | Passed |
| 21-30 | best gear, 3 enhancements | 745 | 22.1 | 20262 | 1637 | 14.6 | 50535 | 2.49 | - | Passed |
| 21-30 | fully modded gear | 1136 | 20.7 | 32908 | 1748 | 7.3 | 108125 | 3.29 | - | Passed |
| 21-30 | fully modded gear, 1 enhancement | 1123 | 21.4 | 31520 | 1751 | 7.5 | 105558 | 3.35 | - | Passed |
| 21-30 | fully modded gear, 2 enhancements | 1122 | 21.4 | 31513 | 1751 | 8.6 | 92165 | 2.92 | - | Passed |
| 21-30 | fully modded gear, 3 enhancements | 1113 | 21.9 | 30515 | 1751 | 9.5 | 82928 | 2.72 | - | Passed |
| 31-40 | no gear | 728 | 14.6 | 29901 | 1908 | 10.9 | 78833 | 2.64 | - | Passed |
| 31-40 | no gear, 1 enhancement | 651 | 15.5 | 25231 | 1762 | 12.5 | 63324 | 2.51 | - | Passed |
| 31-40 | no gear, 2 enhancements | 638 | 15.1 | 25402 | 1075 | 12.9 | 37624 | 1.48 | - | Passed |
| 31-40 | no gear, 3 enhancements | 611 | 16.0 | 22902 | 663 | 13.8 | 21544 | 0.94 | - | Passed |
| 31-40 | mid gear | 818 | 19.9 | 24672 | 2149 | 11.3 | 85675 | 3.47 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 791 | 21.1 | 22479 | 2037 | 13.2 | 69415 | 3.09 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 755 | 20.6 | 22046 | 1494 | 13.8 | 48882 | 2.22 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 711 | 20.2 | 21120 | 1036 | 14.2 | 32810 | 1.55 | - | Passed |
| 31-40 | best gear | 892 | 24.7 | 21679 | 2254 | 10.7 | 95119 | 4.39 | - | Passed |
| 31-40 | best gear, 1 enhancement | 886 | 25.9 | 20494 | 2282 | 12.7 | 80843 | 3.94 | - | Passed |
| 31-40 | best gear, 2 enhancements | 863 | 26.1 | 19809 | 2131 | 14.0 | 68660 | 3.47 | - | Passed |
| 31-40 | best gear, 3 enhancements | 837 | 24.7 | 20325 | 1619 | 14.7 | 49684 | 2.44 | - | Passed |
| 31-40 | fully modded gear | 1118 | 25.1 | 26768 | 2442 | 7.6 | 143964 | 5.38 | - | Passed |
| 31-40 | fully modded gear, 1 enhancement | 1119 | 25.3 | 26536 | 2443 | 8.2 | 133339 | 5.02 | - | Passed |
| 31-40 | fully modded gear, 2 enhancements | 1130 | 25.9 | 26130 | 2445 | 10.0 | 110403 | 4.23 | - | Passed |
| 31-40 | fully modded gear, 3 enhancements | 1123 | 25.9 | 25998 | 2412 | 11.3 | 96278 | 3.70 | - | Passed |
| 41-53 | no gear | 728 | 18.1 | 24054 | 1028 | 12.7 | 36422 | 1.51 | - | Passed |
| 41-53 | no gear, 1 enhancement | 650 | 16.2 | 24132 | 901 | 13.0 | 31282 | 1.30 | - | Passed |
| 41-53 | no gear, 2 enhancements | 590 | 15.9 | 22201 | 638 | 13.2 | 21805 | 0.98 | - | Passed |
| 41-53 | no gear, 3 enhancements | 537 | 16.4 | 19614 | 387 | 13.6 | 12848 | 0.66 | - | Passed |
| 41-53 | mid gear | 878 | 24.5 | 21468 | 1134 | 13.4 | 38055 | 1.77 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 788 | 22.4 | 21074 | 1088 | 13.8 | 35443 | 1.68 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 725 | 20.6 | 21161 | 864 | 14.6 | 26581 | 1.26 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 696 | 19.8 | 21121 | 596 | 14.6 | 18302 | 0.87 | - | Passed |
| 41-53 | best gear | 1041 | 28.1 | 22232 | 1222 | 13.5 | 40771 | 1.83 | - | Passed |
| 41-53 | best gear, 1 enhancement | 1022 | 28.7 | 21364 | 1176 | 13.3 | 39874 | 1.87 | - | Passed |
| 41-53 | best gear, 2 enhancements | 894 | 25.8 | 20819 | 1122 | 14.5 | 34737 | 1.67 | - | Passed |
| 41-53 | best gear, 3 enhancements | 841 | 23.9 | 21142 | 905 | 14.9 | 27247 | 1.29 | - | Passed |
| 41-53 | fully modded gear | 1181 | 27.8 | 25481 | 1354 | 8.7 | 70252 | 2.76 | - | Passed |
| 41-53 | fully modded gear, 1 enhancement | 1167 | 27.7 | 25303 | 1334 | 9.2 | 65003 | 2.57 | - | Passed |
| 41-53 | fully modded gear, 2 enhancements | 1163 | 28.8 | 24183 | 1331 | 10.8 | 55206 | 2.28 | - | Passed |
| 41-53 | fully modded gear, 3 enhancements | 1132 | 28.6 | 23774 | 1311 | 11.2 | 52468 | 2.21 | - | Passed |

## Enhancement parity

### Rank 50, fully modded gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 100%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 6 | 60-70 | 34% | 98% | 99% | 0.97 | 100% | 99.8% | 1.00 (+/- 0.00) | 1.03 | >= 0.95 | Passed |
| 41-45 | 2 | 6 | 60-70 | 67% | 98% | 99% | 1.94 | 100% | 99.9% | 2.00 (+/- 0.00) | 1.03 | >= 0.95 | Passed |
| 41-45 | 3 | 6 | 60-70 | 101% | 97% | 99% | 2.88 | 100% | 99.8% | 2.90 (+/- 0.02) | 1.01 | >= 0.90 | Passed |
| 46-50 | 1 | 6 | 60 | 32% | 100% | 99% | 0.99 | 100% | 98.8% | 1.00 (+/- 0.00) | 1.01 | >= 0.95 | Passed |
| 46-50 | 2 | 6 | 60 | 64% | 98% | 99% | 1.94 | 100% | 99.4% | 1.98 (+/- 0.01) | 1.02 | >= 0.95 | Passed |
| 46-50 | 3 | 6 | 60 | 96% | 98% | 99% | 2.91 | 100% | 98.7% | 2.82 (+/- 0.03) | 0.97 | >= 0.90 | Passed |
| 51-53 | 1 | 6 | 60-70 | 8% | 98% | 41% | 0.40 | 100% | 94.3% | 1.00 (+/- 0.00) | 2.52 | >= 0.95 | Passed |
| 51-53 | 2 | 6 | 60-70 | 17% | 100% | 46% | 0.90 | 100% | 93.7% | 1.80 (+/- 0.03) | 1.99 | >= 0.95 | Passed |
| 51-53 | 3 | 6 | 60-70 | 25% | 98% | 41% | 1.19 | 100% | 92.4% | 2.25 (+/- 0.05) | 1.90 | >= 0.90 | Passed |

### Thresholds, first / second / third slot, that would meet parity for the quality measured (the strictest set that keeps every case at least at today's, never falling from one slot to the next)

| Scope | Thresholds that meet parity | In steps of 5 | Thresholds in force |
| --- | --- | --- | --- |
| Every band together (the shared thresholds) | 79% / 92% / 92% | 75% / 90% / 90% | 50% / 85% / 100% |
| Band 41-45 alone | 98% / 98% / 98% | 95% / 95% / 100% | 50% / 85% / 100% |
| Band 46-50 alone | 79% / 92% / 92% | 75% / 90% / 90% | 50% / 85% / 100% |
| Band 51-53 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 100% |

### Rank 50, fully modded gear: quality of the items the new minigame made, in whole percent, by band and slots filled

| Band | Slots filled | Items made | 10th percentile | 25th percentile | Median | 75th percentile | 90th percentile | Made at 100% |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 92% |
| 41-45 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 97% |
| 41-45 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 91% |
| 46-50 | 1 | 100% | 97% | 100% | 100% | 100% | 100% | 84% |
| 46-50 | 2 | 100% | 99% | 100% | 100% | 100% | 100% | 90% |
| 46-50 | 3 | 100% | 96% | 100% | 100% | 100% | 100% | 83% |
| 51-53 | 1 | 100% | 79% | 93% | 100% | 100% | 100% | 52% |
| 51-53 | 2 | 100% | 77% | 90% | 98% | 100% | 100% | 46% |
| 51-53 | 3 | 100% | 78% | 88% | 97% | 100% | 100% | 45% |

### Representative enhancements: for each recipe kind the real item at the best level the recipe accepts (the old window refuses one more than 5 levels above the recipe) whose penalty is the lower median of that level

| Recipe kind | Enhancement | Level | Progress penalty | Items of that kind and level (min / median / max penalty) | Recipes |
| --- | --- | --- | --- | --- | --- |
| Weapon | Improved Agrinium V (`imp_agrinium_5`) | 45 (tier V) | 70 | 33 items: 50 / 70.0 / 80 | 4 |
| Armor | Armor Enhancement - Force Attack V (`aen_frcatk5`) | 45 (tier V) | 60 | 50 items: 50 / 60.0 / 150 | 14 |

## Gear ladder

### Rank 50, 2 kits, no perks: what each step of gear buys

| Band | Recipes | Kind | Gear | Avg quality | Reach 85% | Reach 100% | Actions | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 46-50 | 2 | Finish-heavy | no gear | 30.4 | 0% | 0% | 11.1 | Passed |
| 46-50 | 2 | Finish-heavy | mid gear | 49.5 | 0% | 0% | 12.1 | Passed |
| 46-50 | 2 | Finish-heavy | best gear | 60.6 | 0% | 0% | 12.3 | Passed |
| 46-50 | 2 | Finish-heavy | fully modded gear | 97.3 | 94% | 76% | 10.2 | Passed |
| 46-50 | 4 | Shape-heavy | no gear | 16.8 | 0% | 0% | 15.1 | Passed |
| 46-50 | 4 | Shape-heavy | mid gear | 38.8 | 4% | 1% | 15.6 | Passed |
| 46-50 | 4 | Shape-heavy | best gear | 71.6 | 22% | 9% | 15.0 | Passed |
| 46-50 | 4 | Shape-heavy | fully modded gear | 99.9 | 100% | 96% | 12.3 | Passed |
| 51-53 | 3 | Finish-heavy | no gear | 32.6 | 0% | 0% | 9.5 | Passed |
| 51-53 | 3 | Finish-heavy | mid gear | 45.0 | 0% | 0% | 10.2 | Passed |
| 51-53 | 3 | Finish-heavy | best gear | 53.6 | 0% | 0% | 9.6 | Passed |
| 51-53 | 3 | Finish-heavy | fully modded gear | 93.2 | 78% | 42% | 10.0 | Failed |
| 51-53 | 3 | Shape-heavy | no gear | 5.4 | 0% | 0% | 11.2 | Passed |
| 51-53 | 3 | Shape-heavy | mid gear | 33.0 | 0% | 0% | 13.4 | Passed |
| 51-53 | 3 | Shape-heavy | best gear | 52.8 | 0% | 0% | 13.2 | Passed |
| 51-53 | 3 | Shape-heavy | fully modded gear | 94.0 | 82% | 49% | 10.1 | Passed |

### Perks do not replace gear: plain crafts, average quality

| Band | Style | Fully perked, no gear | No perks, unmodded best gear | Allowance | Verdict |
| --- | --- | --- | --- | --- | --- |
| 46-50 | Hot Forging (both lines) | 68.7 | 82.0 | +5 | Passed |
| 46-50 | Cold Working (both lines) | 58.1 | 82.0 | +5 | Passed |
| 51-53 | Hot Forging (both lines) | 64.0 | 59.8 | +5 | Passed |
| 51-53 | Cold Working (both lines) | 63.3 | 59.8 | +5 | Passed |

## Limitations

- The recipes are an even sample of each band (or all of them, as the inputs say), weighted equally. A recipe the sample skipped can behave differently.
- The adaptive player is a bounded-depth expectimax over the previews with a generic heuristic. It is a floor on skilled play, not an optimal player; a deeper search or a scenario heuristic can only raise it.
- Gate 1 asks whether one rotation wins every craft of a band (owner decision, October 2026), not whether each recipe has a rotation of its own: in a minigame with full state and almost no randomness a rotation per recipe always exists, so that figure is reported and not gated. The band's recipes are an even sample of at most `RecipesPerBand`, so a rotation that wins the unsampled recipes is not tried.
- The fixed-rotation solver is a beam search with local search, not a proof. A fixed rotation it did not find would narrow the margin; raise the solver options to look harder. The single rotation starts from every recipe's own best sequence and the adaptive lines, and is scored on separate attempts, so its figure is not flattered by the search.
- Gate 5 compares at matched rank, gear and effort (both sides play to maximize quality) and requires the new minigame's average quality to be at least the legacy player's (within `MaterialQualityShortfallPoints`); it does not compare at an equal quality target. Where the legacy policy succeeds in fewer than `MaterialTwoSidedMinimumLegacySuccess` of the crafts, its failures waste material, and lower use than today passes (owner decision, October 2026). Enhancements are not committed in either model.
- Gate 6 uses estimated seconds per action. The legacy comparator is the old minigame's lookahead player, one step of policy improvement over the rotation players report, not an optimal player. Its enhanced cases play the cohort's recipes that have an enhancement slot (an even sample of each band, so they can differ from the plain cases' recipes) with gate 7's representative enhancement in every slot; XP does not depend on the enhancements except through the quality and failures they cause.
- Gate 7 plays every enhanceable recipe of the at-level bands with the representative real enhancement item of its kind (the best level the recipe accepts, the lower-median progress penalty of that level), the same item in every filled slot, and counts expected transferred enhancements per craft attempt, a craft that makes no item counting none. Enhancement items differ in penalty, so the figure holds for the median item. Today's side draws no transfer rolls: a craft that is made adds slots x quality chance / 100. How a scenario maps the penalty onto its progress meter is that minigame's own rule, and the client has not measured it.
- Gathering, scavenging, fishing and research are not covered here; their cohorts implement `IXpRateCohort` and their economy checks belong to their own gates.
- No attributes, food, status effects, blueprints, first-craft bonus or quick craft are modeled, and the client was not run.
- Enhancements raise the Shape the recipe needs by their progress penalty through ForgeRules.EnhancementShapePercent; their other effects are not played.
- Without a style the crafter plays without perks, which is what the perks-optional gate and every gate of the plain scenario play; the style-balance gate plays Hot Forging and Cold Working with no line perks and with both lines bought in full (every purchase the crafter's rank allows).
- The seconds per action (8) is an estimate for a window nobody has played; client timing replaces it.
- The adaptive player reads only the shared meters and previews, so it plans Thermal worse than a person who reads the range; gate 1 compares it with a solver that searches whole rotations.
