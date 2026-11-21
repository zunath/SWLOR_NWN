# Crafting revamp: balance gates of the Circuit minigame

- Command: `SWLOR.CraftSim gates --scenario circuit --gates rotation,styles,perks,length,materials,xp,enhancements,ladder --runs 40 --seed 1 --recipes-per-band 6 --repo . --out design/testing/crafting-revamp/circuit-gates.md`
- Seed 1; 40 runs per case.

## Inputs

- Scenario: Circuit (`circuit`), Engineering recipes, minigame Circuit.
- Quality: the quality meter's share of its maximum when the item is made, in points (0-100). A broken or abandoned piece counts 0, so every average includes the crafts that failed.
- Crafter: the rank the scenario's cohort gives each recipe (by default the recipe's level, at most 50, so a crafter works at their own level), with no food or status bonuses and no perks unless a gate names a style or the scenario's limitations say it plays a perk loadout. Gear levels are the plan's none, middle and best tier of the skill's crafting gear:
  - no gear (Control 0, Craftsmanship 0, CP 0)
  - mid gear (Control 13, Craftsmanship 12, CP 20)
  - best gear (Control 29, Craftsmanship 30, CP 37)
  - fully modded gear (Control 119, Craftsmanship 120, CP 37)
- Recipes: 1-10: 6 of 442; 11-20: 6 of 453; 21-30: 6 of 441; 31-40: 6 of 443; 41-53: 6 of 417 recipes played.
- Adaptive player: depth 3, forecast samples per result 2, budget 4000 engine copies per decision, 0.5% of value lost per extra action, heuristic DefaultLeafHeuristic. Each attempt's dice come from its own stream of the seed.
- Fixed-rotation solver: beam 8, cycles up to 3, 8 adaptive lines as seeds, up to 40 actions, 64 training attempts per local-search round and 150 selection attempts for each of 12 finalists, local search from the best 3 candidate(s) for up to 3 round(s) each. Gate 1 searches one sequence for each band and gear tier across the band's sampled recipes (every recipe weighing the same; an action a recipe's engine cannot take is skipped there), and also each recipe's own best sequence as a reported metric. The winner is chosen on separate attempts and then scored on a third set (hold-out) with 40 attempts per recipe.
- Legacy comparator (gates 5 and 6): the old minigame's lookahead player (`SWLOR.CraftSim baseline`), replayed here with the same seed on the same recipes, ranks and gear.
- Time: 8.0 s per action in the new window and 6.0 s in the old one, plus 0.0 s per craft in both. These are estimates; the client evidence replaces them.
- XP multiplier on the base XP of every craft, failures included, as the game server applies it (`CraftMinigameXp`): 1-10 2.29x, 11-20 1.72x, 21-30 1.19x, 31-40 1.00x, 41-53 1.00x. The new side's XP below includes it; the legacy loop's does not.
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
- Styles compared: Overclock (no lines) vs Safe Mode (no lines) (from rank 20); Overclock (both lines) vs Safe Mode (both lines) (from rank 20).

## Verdicts

| Gate | Verdict | Summary |
| --- | --- | --- |
| No fixed rotation | Failed | One fixed rotation comes within 10 points of adaptive play in 11 case(s): 1-10 / no gear; 1-10 / mid gear; 1-10 / best gear; 11-20 / no gear; 11-20 / mid gear; 21-30 / no gear; 31-40 / no gear; 31-40 / mid gear; 41-53 / no gear; 41-53 / mid gear; 41-53 / best gear (smallest margin -16.5 in 41-53 / no gear). The best rotation for each recipe alone, a reported metric, is -16.0 to -0.0 points behind adaptive play. |
| Style balance | Failed | 1 style comparison(s) are out of balance: Overclock (no lines) vs Safe Mode (no lines) / mid gear. |
| Perks optional | Passed | An unperked rank-50 crafter in fully modded gear reaches maximum quality on at least 50% of crafts in every at-level band (lowest 75%). |
| Length | Passed | Every band and gear tier takes 10-20 actions (item craft at level) on average. |
| Economy parity | Passed | Component stacks per item are within 10% of the legacy baseline where its success rate is at least 90%, and at most 10% above it where it is lower, at no less quality, in every band and gear tier. |
| XP rate | Failed | Item crafts: XP per hour falls below 1.00x the legacy rate in 1 case(s): 31-40 / mid gear, 1 enhancement (0.97x). |
| Enhancement parity | Passed | At rank 50 in fully modded gear, the thresholds in force (50% / 85% / 100%) transfer at least 0.90 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 0.99 times today's). Thresholds that meet parity in every band for this quality: 89% / 89% / 89%. |
| Gear ladder | Failed | 46-50 / Overclock (both lines) with no gear averages 87.2 against unmodded best gear's 77.0; 46-50 / Safe Mode (both lines) with no gear averages 82.8 against unmodded best gear's 77.0; 51-53 / Overclock (both lines) with no gear averages 65.1 against unmodded best gear's 58.9. |

## No fixed rotation

### Average quality: adaptive play against the best single fixed rotation of each band and gear tier (quality points; a broken piece counts 0)

| Band | Gear | Recipes played | Adaptive avg quality | Best single rotation avg quality | Margin | Required | Best rotation per recipe avg quality (reported) | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 49.4 | 52.0 | -2.6 | >= 10 | 52.6 | Failed |
| 1-10 | mid gear | 6 | 88.0 | 96.0 | -8.0 | >= 10 | 96.6 | Failed |
| 1-10 | best gear | 6 | 92.4 | 97.1 | -4.7 | >= 10 | 100.0 | Failed |
| 1-10 | fully modded gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 11-20 | no gear | 6 | 79.0 | 84.7 | -5.7 | >= 10 | 87.3 | Failed |
| 11-20 | mid gear | 6 | 95.2 | 100.0 | -4.8 | >= 10 | 100.0 | Failed |
| 11-20 | best gear | 6 | 99.2 | 100.0 | -0.8 | >= 10 | 100.0 | Reported |
| 11-20 | fully modded gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 21-30 | no gear | 6 | 95.9 | 100.0 | -4.1 | >= 10 | 100.0 | Failed |
| 21-30 | mid gear | 6 | 98.6 | 100.0 | -1.3 | >= 10 | 100.0 | Reported |
| 21-30 | best gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 21-30 | fully modded gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 31-40 | no gear | 6 | 88.8 | 98.8 | -10.0 | >= 10 | 98.7 | Failed |
| 31-40 | mid gear | 6 | 94.1 | 100.0 | -5.9 | >= 10 | 100.0 | Failed |
| 31-40 | best gear | 6 | 99.4 | 100.0 | -0.6 | >= 10 | 100.0 | Reported |
| 31-40 | fully modded gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 41-53 | no gear | 6 | 55.1 | 71.6 | -16.5 | >= 10 | 71.1 | Failed |
| 41-53 | mid gear | 6 | 69.8 | 83.0 | -13.2 | >= 10 | 81.9 | Failed |
| 41-53 | best gear | 6 | 86.4 | 93.9 | -7.5 | >= 10 | 95.5 | Failed |
| 41-53 | fully modded gear | 6 | 98.8 | 100.0 | -1.2 | >= 10 | 100.0 | Reported |

### The single rotation found for each band and gear tier, searched across the band's sampled recipes (average quality in points)

| Band | Gear | Best single rotation for the band | Training | Selection | Hold-out | Adaptive on these recipes |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | wire x3 > tune x2 > discharge_calibration > wire x4 > discharge_calibration > wire x3 > discharge_calibration > wire | 51.4 | 52.0 | 52.0 | 49.4 |
| 1-10 | mid gear | tune x2 > wire > tune > discharge_calibration > tune x2 > wire > tune > discharge_calibration > wire x4 > discharge_calibration > wire > tune > wire x4 | 96.6 | 95.6 | 96.0 | 88.0 |
| 1-10 | best gear | discharge_assembly > tune x3 > discharge_calibration > tune x3 > discharge_calibration > tune > wire x2 > discharge_calibration > wire x2 > tune > wire > discharge_assembly > wire x3 | 99.0 | 98.8 | 97.1 | 92.4 |
| 1-10 | fully modded gear | tune x3 > discharge_calibration > tune x3 > wire x5 | 100.0 | 100.0 | 100.0 | 100.0 |
| 11-20 | no gear | wire > tune x3 > discharge_calibration > wire x2 > tune x2 > discharge_calibration > wire x4 > discharge_calibration > wire x3 > discharge_calibration > tune > wire > discharge_calibration > wire x3 | 84.1 | 84.3 | 84.7 | 79.0 |
| 11-20 | mid gear | wire > tune x3 > discharge_calibration > tune x4 > patch > discharge_calibration > wire x3 > discharge_calibration > wire x4 > discharge_calibration > wire x2 | 100.0 | 100.0 | 100.0 | 95.2 |
| 11-20 | best gear | tune x4 > discharge_calibration > tune x4 > discharge_calibration > wire x4 > discharge_assembly > wire > tune > wire x2 | 100.0 | 100.0 | 100.0 | 99.2 |
| 11-20 | fully modded gear | tune x4 > discharge_calibration > tune x2 > wire x5 | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | tune x6 > discharge_calibration > wire > tune x3 > patch > discharge_calibration > wire x3 > discharge_calibration > wire x5 > discharge_calibration > patch > tune > wire x2 | 100.0 | 100.0 | 100.0 | 95.9 |
| 21-30 | mid gear | tune x6 > discharge_calibration > tune x3 > patch > tune > wire > discharge_assembly > wire x5 > discharge_calibration > wire > discharge_assembly > tune > patch > tune > wire | 100.0 | 100.0 | 100.0 | 98.6 |
| 21-30 | best gear | tune x5 > discharge_calibration > tune x3 > wire > discharge_calibration > wire x5 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | tune x5 > discharge_calibration > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | boost > wire > tune x2 > discharge_calibration > boost > tune x2 > patch > tune > discharge_calibration > boost > patch > wire x2 > tune > discharge_calibration > wire x4 > tune x2 > wire > discharge_calibration > wire x2 > discharge_calibration > wire x2 | 98.9 | 99.1 | 98.8 | 88.8 |
| 31-40 | mid gear | wire > tune x5 > discharge_calibration > wire x4 > tune > discharge_calibration > wire > tune > patch > tune x4 > discharge_assembly > tune x4 > wire x2 > discharge_assembly > wire x3 | 100.0 | 100.0 | 100.0 | 94.1 |
| 31-40 | best gear | boost > tune x3 > discharge_calibration > tune x6 > patch > discharge_calibration > wire > tune x2 > wire x3 > discharge_assembly > wire > tune > wire | 100.0 | 100.0 | 100.0 | 99.4 |
| 31-40 | fully modded gear | boost > tune x4 > discharge_calibration > wire > tune x2 > wire x3 > discharge_calibration > tune > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | discharge_assembly > wire > boost > tune x2 > patch > tune x2 > discharge_calibration > wire x6 > tune x3 > discharge_calibration > wire | 72.4 | 70.7 | 71.6 | 55.1 |
| 41-53 | mid gear | wire x2 > tune x4 > patch > tune x3 > discharge_calibration > wire x4 > tune x5 > discharge_assembly > tune > discharge_calibration > wire | 78.5 | 80.2 | 83.0 | 69.8 |
| 41-53 | best gear | boost x2 > tune x2 > discharge_calibration > patch > discharge_calibration > discharge_assembly > boost x2 > tune > patch x2 > tune > patch x2 > discharge_calibration > boost > wire x3 > tune > wire x2 > discharge_calibration > wire | 94.4 | 94.4 | 93.9 | 86.4 |
| 41-53 | fully modded gear | boost > tune x4 > discharge_calibration > tune x5 > wire > discharge_assembly > tune > wire > patch > tune > wire | 100.0 | 100.0 | 100.0 | 98.8 |

### Reported metric, not gated: the best fixed rotation for each recipe alone, and what the band's single rotation scores on the same recipe (average quality in points)

| Band | Gear | Recipe | Best rotation for this recipe alone (reported) | Its hold-out | The single rotation on this recipe | Adaptive on this recipe |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | sm_capr_00 (level 2) | wire x5 > discharge_calibration > wire > discharge_calibration > wire x4 > discharge_calibration > wire | 36.4 | 34.6 | 32.3 |
| 1-10 | no gear | dmotive_sys1 (level 5) | wire x2 > tune > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire | 48.1 | 46.5 | 46.2 |
| 1-10 | no gear | d_bd_mw1 (level 6) | wire x4 > tune > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire | 47.6 | 48.0 | 41.1 |
| 1-10 | no gear | ship_fuelcapsule (level 8) | wire > tune > wire > tune x2 > discharge_calibration > wire x4 > discharge_calibration > wire x4 > discharge_calibration > wire > discharge_calibration > wire | 58.0 | 56.4 | 56.2 |
| 1-10 | no gear | dehl001 (level 10) | wire x3 > tune x2 > discharge_calibration > wire x5 > discharge_calibration > wire x2 > discharge_calibration > wire | 64.5 | 63.4 | 59.9 |
| 1-10 | no gear | sm_resall_10 (level 10) | wire x2 > tune x2 > wire > discharge_calibration > wire x4 > discharge_calibration > wire x3 > discharge_calibration > wire > discharge_calibration > wire | 60.8 | 63.3 | 60.6 |
| 1-10 | mid gear | sm_capr_00 (level 2) | boost > tune x2 > discharge_calibration > tune x3 > wire x2 > discharge_calibration > wire x5 > discharge_calibration > wire x2 | 100.0 | 99.9 | 96.5 |
| 1-10 | mid gear | dmotive_sys1 (level 5) | wire > tune x2 > discharge_calibration > tune x3 > wire > discharge_assembly > tune > wire x2 > discharge_calibration > tune > wire x2 > discharge_calibration > wire x2 | 80.1 | 77.5 | 79.8 |
| 1-10 | mid gear | d_bd_mw1 (level 6) | tune x3 > wire > discharge_calibration > tune x2 > wire > tune > discharge_calibration > wire x4 > discharge_calibration > wire > tune > wire x4 | 99.8 | 99.9 | 92.5 |
| 1-10 | mid gear | ship_fuelcapsule (level 8) | wire > tune > wire > tune > discharge_calibration > tune x4 > discharge_calibration > discharge_assembly > wire x4 > discharge_calibration > discharge_assembly > wire > tune > wire x2 | 99.7 | 99.5 | 82.1 |
| 1-10 | mid gear | dehl001 (level 10) | tune x5 > discharge_calibration > wire > tune x3 > wire > discharge_assembly > wire x2 > tune > wire x2 > discharge_calibration > wire | 100.0 | 99.7 | 91.4 |
| 1-10 | mid gear | sm_resall_10 (level 10) | tune > wire x2 > tune x2 > discharge_calibration > tune x3 > patch > discharge_calibration > wire x2 > discharge_calibration > wire x4 > discharge_calibration > wire x3 > discharge_calibration > wire | 100.0 | 99.6 | 85.6 |
| 1-10 | best gear | sm_capr_00 (level 2) | tune x5 > discharge_calibration > tune > wire > tune x2 > wire > discharge_assembly > tune > wire x3 | 100.0 | 100.0 | 87.9 |
| 1-10 | best gear | dmotive_sys1 (level 5) | discharge_assembly > tune x3 > discharge_calibration > tune x3 > discharge_calibration > tune > wire x2 > discharge_calibration > wire > tune > wire > discharge_assembly > wire x2 | 100.0 | 82.5 | 91.5 |
| 1-10 | best gear | d_bd_mw1 (level 6) | tune x4 > discharge_calibration > tune x3 > wire > discharge_calibration > wire > tune > wire x2 > discharge_assembly > wire x3 | 100.0 | 100.0 | 88.2 |
| 1-10 | best gear | ship_fuelcapsule (level 8) | tune > wire > tune x2 > discharge_calibration > tune x3 > discharge_calibration > boost > wire > discharge_calibration > wire x2 > tune > wire > discharge_assembly > wire x3 | 100.0 | 100.0 | 86.6 |
| 1-10 | best gear | dehl001 (level 10) | tune x5 > discharge_calibration > tune x4 > wire > discharge_assembly > wire x5 | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | sm_resall_10 (level 10) | tune x5 > discharge_calibration > tune x3 > wire x2 > discharge_assembly > wire x3 > tune > wire | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | sm_capr_00 (level 2) | boost > tune x2 > discharge_calibration > tune x2 > wire x4 | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | dmotive_sys1 (level 5) | tune x3 > discharge_calibration > tune x3 > wire x5 | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | d_bd_mw1 (level 6) | tune x4 > discharge_calibration > tune > wire x4 > tune > wire | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | ship_fuelcapsule (level 8) | tune x3 > discharge_calibration > tune x3 > wire x5 | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | dehl001 (level 10) | boost > tune x2 > discharge_calibration > tune x2 > wire x4 | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | sm_resall_10 (level 10) | boost > tune x2 > discharge_calibration > tune x2 > wire x4 | 100.0 | 100.0 | 100.0 |
| 11-20 | no gear | dlbe001a (level 14) | wire > tune x3 > discharge_calibration > wire x4 > discharge_calibration > wire x4 > discharge_calibration > wire > discharge_calibration > wire | 73.2 | 71.9 | 74.4 |
| 11-20 | no gear | sd_hutthauler (level 15) | boost > wire > tune x3 > discharge_calibration > wire x2 > discharge_calibration > wire x6 > discharge_calibration > wire > discharge_calibration > wire | 76.0 | 68.4 | 74.7 |
| 11-20 | no gear | dhnk002a (level 17) | wire > tune x3 > discharge_calibration > wire x3 > tune > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire > discharge_calibration > wire | 85.9 | 83.7 | 72.6 |
| 11-20 | no gear | d_jg_cpu2_v (level 20) | wire > tune x3 > discharge_calibration > wire x2 > tune x2 > discharge_calibration > wire x4 > discharge_calibration > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire x3 | 97.3 | 97.3 | 82.9 |
| 11-20 | no gear | id_hookstri2 (level 20) | wire > tune x3 > discharge_calibration > wire x2 > tune x2 > discharge_calibration > wire x4 > discharge_calibration > wire x4 > discharge_calibration > wire > discharge_calibration > wire | 93.0 | 91.9 | 81.8 |
| 11-20 | no gear | sm_plate_20 (level 20) | wire > tune > wire > tune x3 > discharge_calibration > wire x3 > discharge_calibration > wire x5 > tune > discharge_calibration > wire x2 > discharge_calibration > wire x3 > discharge_calibration > wire | 98.5 | 95.3 | 87.7 |
| 11-20 | mid gear | dlbe001a (level 14) | wire > tune x3 > discharge_calibration > tune x4 > patch > discharge_calibration > wire x3 > discharge_calibration > wire x4 > discharge_calibration > wire x2 | 100.0 | 100.0 | 97.1 |
| 11-20 | mid gear | sd_hutthauler (level 15) | tune x6 > discharge_calibration > tune x2 > wire > tune > wire x2 > discharge_assembly > wire x3 > tune > wire | 100.0 | 100.0 | 85.5 |
| 11-20 | mid gear | dhnk002a (level 17) | tune x4 > discharge_calibration > tune x3 > discharge_calibration > tune > wire x2 > tune > discharge_assembly > wire x2 > tune > wire > discharge_assembly > wire x2 | 100.0 | 100.0 | 99.7 |
| 11-20 | mid gear | d_jg_cpu2_v (level 20) | tune x4 > discharge_calibration > tune x2 > discharge_calibration > tune x3 > wire > discharge_assembly > tune > wire x3 > discharge_assembly > tune > wire x2 | 100.0 | 100.0 | 97.5 |
| 11-20 | mid gear | id_hookstri2 (level 20) | tune x4 > discharge_calibration > tune x3 > discharge_calibration > wire > tune x2 > wire > patch > discharge_calibration > wire x8 | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | sm_plate_20 (level 20) | boost > tune x3 > discharge_calibration > tune x5 > wire > discharge_assembly > tune > wire x3 > discharge_assembly > tune > wire | 100.0 | 100.0 | 91.2 |
| 11-20 | best gear | dlbe001a (level 14) | tune x5 > discharge_calibration > patch > tune x3 > wire > discharge_calibration > wire x4 > discharge_assembly > wire > tune x2 > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | sd_hutthauler (level 15) | boost > tune x3 > discharge_calibration > tune x5 > wire > discharge_assembly > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 95.0 |
| 11-20 | best gear | dhnk002a (level 17) | boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | d_jg_cpu2_v (level 20) | tune x4 > discharge_calibration > tune x4 > discharge_calibration > wire x4 > discharge_assembly > wire x3 | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | id_hookstri2 (level 20) | tune x4 > discharge_calibration > tune x4 > discharge_calibration > wire x4 > discharge_assembly > wire > tune > wire x2 | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | sm_plate_20 (level 20) | boost > tune x3 > discharge_calibration > tune x4 > wire x2 > discharge_assembly > tune > wire x3 | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | dlbe001a (level 14) | tune x4 > discharge_calibration > tune > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | sd_hutthauler (level 15) | boost > tune x3 > discharge_calibration > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | dhnk002a (level 17) | tune x4 > discharge_calibration > tune > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | d_jg_cpu2_v (level 20) | tune x4 > discharge_calibration > tune > wire x4 | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | id_hookstri2 (level 20) | tune x4 > discharge_calibration > tune x2 > wire x5 | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | sm_plate_20 (level 20) | boost > tune x3 > discharge_calibration > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | dhhl003 (level 24) | tune x6 > discharge_calibration > wire x4 > discharge_calibration > wire x4 > discharge_calibration > wire x2 > discharge_calibration > wire | 100.0 | 100.0 | 89.7 |
| 21-30 | no gear | ship_missile_inc (level 25) | boost > tune x4 > discharge_calibration > tune x5 > wire > tune > discharge_assembly > wire x4 > discharge_assembly > wire | 100.0 | 100.0 | 94.5 |
| 21-30 | no gear | wen_atk3 (level 28) | boost > tune x3 > discharge_calibration > tune x6 > patch > discharge_calibration > wire x6 > discharge_assembly > wire x2 | 100.0 | 100.0 | 93.9 |
| 21-30 | no gear | song_26 (level 29) | boost > tune x4 > discharge_calibration > wire > tune x5 > wire > discharge_assembly > wire x5 | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | id_soulstrike3 (level 30) | tune x5 > discharge_calibration > wire > tune x4 > patch > discharge_calibration > tune > wire x4 > discharge_assembly > wire > tune > wire x3 | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | sd_sithhammerhea (level 30) | discharge_assembly > tune x5 > discharge_calibration > tune x4 > patch > tune > discharge_assembly > wire > tune > wire x3 > discharge_assembly > wire x2 > tune x2 > wire | 100.0 | 100.0 | 97.5 |
| 21-30 | mid gear | dhhl003 (level 24) | tune x6 > discharge_calibration > wire > tune x3 > wire x2 > discharge_assembly > wire x4 | 100.0 | 100.0 | 91.9 |
| 21-30 | mid gear | ship_missile_inc (level 25) | boost > tune x4 > discharge_calibration > boost > tune x4 > discharge_assembly > wire x4 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | wen_atk3 (level 28) | tune x6 > discharge_calibration > tune x4 > wire x2 > discharge_assembly > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | song_26 (level 29) | boost > tune x4 > discharge_calibration > tune x4 > wire x3 > discharge_assembly > wire x3 | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | id_soulstrike3 (level 30) | tune x5 > discharge_calibration > wire > tune x4 > patch > discharge_calibration > wire x5 > discharge_assembly > wire x2 | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | sd_sithhammerhea (level 30) | tune x5 > discharge_calibration > tune x5 > patch > discharge_assembly > wire x5 > discharge_assembly > tune > wire | 100.0 | 99.9 | 99.9 |
| 21-30 | best gear | dhhl003 (level 24) | boost > tune x3 > discharge_calibration > tune x4 > wire x2 > discharge_assembly > tune > wire x3 | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | ship_missile_inc (level 25) | boost > tune x4 > discharge_calibration > tune x2 > wire > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | wen_atk3 (level 28) | boost > tune x3 > discharge_calibration > tune x5 > wire > discharge_assembly > wire > tune > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | song_26 (level 29) | boost > tune x4 > discharge_calibration > tune x3 > wire x4 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | id_soulstrike3 (level 30) | tune x5 > discharge_calibration > tune x4 > wire > discharge_assembly > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | sd_sithhammerhea (level 30) | tune x5 > discharge_calibration > tune x4 > wire > discharge_assembly > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | dhhl003 (level 24) | boost > tune x3 > discharge_calibration > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | ship_missile_inc (level 25) | boost > tune x3 > discharge_calibration > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | wen_atk3 (level 28) | boost > tune x3 > discharge_calibration > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | song_26 (level 29) | boost > tune x3 > discharge_calibration > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | id_soulstrike3 (level 30) | tune x5 > discharge_calibration > wire x5 | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | sd_sithhammerhea (level 30) | tune x5 > discharge_calibration > wire x5 | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | aen_cft_agr4 (level 34) | tune x5 > discharge_calibration > tune x4 > patch > tune x2 > discharge_calibration > tune > wire x5 > discharge_assembly > tune > wire x2 > discharge_calibration > wire | 100.0 | 100.0 | 97.8 |
| 31-40 | no gear | sm_smug_35 (level 35) | boost > tune x5 > discharge_calibration > boost > tune x5 > discharge_assembly > wire > tune > wire x3 > discharge_assembly > tune > wire | 100.0 | 100.0 | 99.9 |
| 31-40 | no gear | aen_def_fir4 (level 38) | tune > wire > boost > tune x2 > discharge_calibration > boost x2 > tune > patch > discharge_calibration > boost > tune > wire x3 > discharge_calibration > boost > wire x4 > discharge_calibration > wire x2 | 100.0 | 99.7 | 84.8 |
| 31-40 | no gear | d_hd_mva4 (level 40) | boost > tune > wire > tune x2 > patch > tune > discharge_calibration > wire x3 > tune > discharge_calibration > boost > patch > tune x2 > wire x3 > discharge_calibration > tune > wire x3 > discharge_assembly | 100.0 | 100.0 | 94.5 |
| 31-40 | no gear | id_inteshot2 (level 40) | wire x2 > tune > wire > tune x2 > discharge_calibration > discharge_assembly > boost > tune x3 > patch > wire > discharge_calibration > boost > wire x2 > tune > discharge_calibration > wire > boost > wire x2 > discharge_calibration > wire x5 | 92.0 | 94.1 | 80.4 |
| 31-40 | no gear | sm_resem_40 (level 40) | boost > tune x5 > discharge_calibration > boost x2 > tune > patch > wire > discharge_calibration > boost > wire x5 > discharge_calibration > wire > discharge_calibration > wire | 100.0 | 99.0 | 75.6 |
| 31-40 | mid gear | aen_cft_agr4 (level 34) | boost > tune x3 > discharge_calibration > tune x5 > discharge_calibration > wire > tune x2 > wire x2 > discharge_assembly > wire x4 > tune > wire | 100.0 | 100.0 | 99.7 |
| 31-40 | mid gear | sm_smug_35 (level 35) | boost > tune x5 > discharge_calibration > boost > tune x4 > wire > discharge_assembly > wire > tune > wire x3 | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | aen_def_fir4 (level 38) | boost > tune x4 > discharge_calibration > wire > tune x6 > patch x2 > discharge_calibration > wire x6 > discharge_assembly | 100.0 | 100.0 | 99.2 |
| 31-40 | mid gear | d_hd_mva4 (level 40) | boost > tune x5 > discharge_calibration > tune x2 > wire > tune x4 > wire > discharge_assembly > tune > wire x2 > patch > tune > wire | 100.0 | 100.0 | 95.0 |
| 31-40 | mid gear | id_inteshot2 (level 40) | wire > tune x5 > discharge_calibration > wire x4 > tune > discharge_calibration > wire > tune > patch > tune x4 > discharge_assembly > tune x4 > wire x2 > discharge_assembly > wire x3 | 100.0 | 100.0 | 90.8 |
| 31-40 | mid gear | sm_resem_40 (level 40) | boost > tune x4 > discharge_calibration > tune x8 > patch > discharge_assembly > wire > tune > wire x3 > discharge_calibration > wire x2 | 100.0 | 100.0 | 79.9 |
| 31-40 | best gear | aen_cft_agr4 (level 34) | boost > tune x3 > discharge_calibration > tune x6 > discharge_calibration > wire x6 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | sm_smug_35 (level 35) | boost > tune x5 > discharge_calibration > boost > tune x3 > wire x2 > discharge_assembly > tune > wire x2 | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | aen_def_fir4 (level 38) | boost > tune x4 > discharge_calibration > tune x6 > wire > discharge_assembly > tune > wire x3 > discharge_calibration > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | d_hd_mva4 (level 40) | boost > tune x5 > discharge_calibration > boost > tune x4 > wire > discharge_assembly > tune > wire x2 | 100.0 | 100.0 | 99.5 |
| 31-40 | best gear | id_inteshot2 (level 40) | boost > tune x3 > discharge_calibration > tune x6 > patch > discharge_calibration > wire > tune x2 > wire x3 > discharge_assembly > wire > tune > wire | 100.0 | 100.0 | 97.2 |
| 31-40 | best gear | sm_resem_40 (level 40) | boost > tune x5 > discharge_calibration > tune x6 > wire x2 > discharge_assembly > tune > wire x2 | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | aen_cft_agr4 (level 34) | boost > tune x3 > discharge_calibration > tune x3 > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | sm_smug_35 (level 35) | boost > tune x4 > discharge_calibration > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | aen_def_fir4 (level 38) | boost > tune x4 > discharge_calibration > tune x2 > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | d_hd_mva4 (level 40) | boost > tune x4 > discharge_calibration > tune > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | id_inteshot2 (level 40) | tune x5 > discharge_calibration > tune x4 > wire x2 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | sm_resem_40 (level 40) | boost > tune x5 > discharge_calibration > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | sm_lbatt_40 (level 43) | discharge_assembly > boost > wire > tune x3 > patch > tune x2 > discharge_calibration > boost x2 > wire x3 > discharge_calibration > boost > wire x3 > discharge_calibration > wire | 92.9 | 87.3 | 77.9 |
| 41-53 | no gear | sm_tcomp_45 (level 45) | boost > tune x3 > patch > tune x2 > patch > tune > discharge_calibration > wire x5 > patch > wire x3 > tune > discharge_calibration > wire x4 > discharge_calibration > wire > discharge_calibration > wire x2 | 88.1 | 84.1 | 69.4 |
| 41-53 | no gear | aen_stm5 (level 47) | boost > wire > tune > patch > tune x2 > patch > tune > patch > tune > discharge_calibration > boost > wire x5 > tune > discharge_calibration > wire x3 > discharge_calibration > wire | 71.6 | 73.4 | 42.6 |
| 41-53 | no gear | song_549 (level 48) | discharge_assembly > wire > boost > tune x2 > patch > tune x2 > discharge_calibration > wire x2 > tune > wire x3 > tune x2 > discharge_calibration > wire | 72.9 | 74.5 | 54.6 |
| 41-53 | no gear | id_wristrck3 (level 50) | tune x4 > patch > tune x2 > patch > tune > discharge_calibration > wire > tune > ground > wire > ground > wire x3 > ground > wire x3 > discharge_calibration > wire x2 | 51.4 | 53.2 | 40.4 |
| 41-53 | no gear | id_palmstrike3 (level 50) | boost > wire > tune x2 > patch > wire x2 > discharge_calibration > boost x2 > tune x2 > ground > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire x4 | 49.9 | 57.0 | 45.8 |
| 41-53 | mid gear | sm_lbatt_40 (level 43) | discharge_assembly > wire > tune > boost > tune x4 > discharge_calibration > patch > wire > tune x5 > wire > tune > wire > discharge_calibration > patch > wire > discharge_calibration > wire | 100.0 | 99.4 | 93.1 |
| 41-53 | mid gear | sm_tcomp_45 (level 45) | wire x2 > boost > tune x4 > discharge_calibration > boost > tune x6 > ground > wire x2 > tune > discharge_assembly > wire x2 | 98.1 | 94.4 | 89.8 |
| 41-53 | mid gear | aen_stm5 (level 47) | tune x4 > patch > tune x5 > ground > tune x3 > wire x2 > discharge_calibration > wire | 79.6 | 85.1 | 73.7 |
| 41-53 | mid gear | song_549 (level 48) | tune x4 > patch > tune x3 > patch > discharge_assembly > boost > tune > wire > tune > wire > tune > discharge_calibration > boost > wire x2 > discharge_calibration > wire | 81.4 | 84.8 | 67.9 |
| 41-53 | mid gear | id_wristrck3 (level 50) | tune x5 > patch > tune x2 > patch > discharge_assembly > wire > tune > wire > tune x2 > wire > tune > discharge_calibration > wire x2 | 62.2 | 61.0 | 45.7 |
| 41-53 | mid gear | id_palmstrike3 (level 50) | wire > tune x2 > patch > tune x5 > patch > discharge_assembly > tune > wire > tune > wire x3 > tune x2 > discharge_calibration > wire x3 | 70.0 | 73.2 | 48.7 |
| 41-53 | best gear | sm_lbatt_40 (level 43) | boost > tune x4 > discharge_calibration > tune x8 > wire > discharge_assembly > wire > tune > patch > wire > tune > discharge_calibration > wire | 100.0 | 100.0 | 98.9 |
| 41-53 | best gear | sm_tcomp_45 (level 45) | boost > tune x6 > discharge_calibration > boost > tune x5 > wire > discharge_assembly > boost > wire > patch > tune > wire | 100.0 | 100.0 | 98.1 |
| 41-53 | best gear | aen_stm5 (level 47) | boost > tune x6 > patch > discharge_calibration > tune > wire > tune x2 > wire > tune x4 > discharge_assembly > wire x2 | 99.6 | 99.6 | 92.9 |
| 41-53 | best gear | song_549 (level 48) | tune x7 > patch > tune > discharge_calibration > wire > tune x4 > wire > tune x2 > discharge_assembly > wire x2 | 99.6 | 98.4 | 88.4 |
| 41-53 | best gear | id_wristrck3 (level 50) | tune x7 > patch > discharge_calibration > tune > wire > tune x3 > wire > tune > discharge_assembly > wire x2 > tune > wire | 81.0 | 76.6 | 69.4 |
| 41-53 | best gear | id_palmstrike3 (level 50) | boost x2 > tune x3 > discharge_calibration > discharge_assembly > boost x2 > patch > wire > tune > patch > tune > patch x2 > discharge_calibration > boost > wire > tune > wire x4 > discharge_calibration > wire | 93.0 | 88.7 | 70.6 |
| 41-53 | fully modded gear | sm_lbatt_40 (level 43) | boost > tune x5 > discharge_calibration > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | fully modded gear | sm_tcomp_45 (level 45) | boost > tune x6 > discharge_calibration > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | fully modded gear | aen_stm5 (level 47) | boost > tune x4 > discharge_calibration > boost > tune x4 > discharge_assembly > tune > wire x2 | 100.0 | 100.0 | 100.0 |
| 41-53 | fully modded gear | song_549 (level 48) | boost > tune x5 > discharge_calibration > tune x2 > wire x2 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | fully modded gear | id_wristrck3 (level 50) | boost > tune x4 > discharge_calibration > tune x5 > wire > discharge_assembly > tune > wire > patch > tune > wire | 100.0 | 100.0 | 94.3 |
| 41-53 | fully modded gear | id_palmstrike3 (level 50) | boost > tune x4 > discharge_calibration > boost > tune x5 > discharge_assembly > tune > wire x2 | 100.0 | 100.0 | 98.4 |

## Style balance

### Style pairs: how often each style is better, and the average quality (quality points; ties count half a win each)

| Style pair | Gear | Recipes | First better | Second better | First avg quality | Second avg quality | Difference | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Overclock (no lines) vs Safe Mode (no lines) | no gear | 21 | 38% | 62% | 81.3 | 82.3 | -1.1 | Passed |
| Overclock (no lines) vs Safe Mode (no lines) | mid gear | 21 | 26% | 74% | 91.4 | 96.3 | -4.9 | Failed |
| Overclock (no lines) vs Safe Mode (no lines) | best gear | 21 | 36% | 64% | 97.6 | 99.2 | -1.6 | Passed |
| Overclock (no lines) vs Safe Mode (no lines) | fully modded gear | 21 | 48% | 52% | 99.9 | 100.0 | -0.1 | Passed |
| Overclock (both lines) vs Safe Mode (both lines) | no gear | 21 | 48% | 52% | 94.8 | 94.4 | +0.4 | Passed |
| Overclock (both lines) vs Safe Mode (both lines) | mid gear | 21 | 48% | 52% | 97.9 | 98.2 | -0.3 | Passed |
| Overclock (both lines) vs Safe Mode (both lines) | best gear | 21 | 50% | 50% | 100.0 | 100.0 | -0.0 | Passed |
| Overclock (both lines) vs Safe Mode (both lines) | fully modded gear | 21 | 48% | 52% | 99.5 | 100.0 | -0.4 | Passed |

## Perks optional

### Rank 50, fully modded gear, no perks: adaptive attempts that reach maximum quality, by recipe band

| Band | Recipes played | Attempts | Reach maximum quality | Make the item | Avg quality of items made | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 6 | 240 | 98% | 100% | 100% | >= 50% | Passed |
| 46-50 | 6 | 240 | 94% | 100% | 100% | >= 50% | Passed |
| 51-53 | 6 | 240 | 75% | 100% | 96% | >= 35% | Passed |

## Length

### Actions per attempt of the adaptive player at level (item craft at level)

| Band | Gear | Recipes played | Mean actions per attempt | Shortest recipe | Longest recipe | Stalled attempts | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 14.0 | 12.0 | 15.3 | 0 | 10-20 | Passed |
| 1-10 | mid gear | 6 | 17.0 | 15.7 | 18.6 | 0 | 10-20 | Passed |
| 1-10 | best gear | 6 | 15.2 | 14.3 | 16.3 | 0 | 10-20 | Passed |
| 1-10 | fully modded gear | 6 | 9.5 | 9.2 | 10.0 | 0 | 10-20 | Reported |
| 11-20 | no gear | 6 | 17.0 | 15.8 | 20.5 | 0 | 10-20 | Passed |
| 11-20 | mid gear | 6 | 17.2 | 16.1 | 18.8 | 0 | 10-20 | Passed |
| 11-20 | best gear | 6 | 14.9 | 14.0 | 16.2 | 0 | 10-20 | Passed |
| 11-20 | fully modded gear | 6 | 9.5 | 9.1 | 10.2 | 0 | 10-20 | Reported |
| 21-30 | no gear | 6 | 18.5 | 16.6 | 20.9 | 0 | 10-20 | Passed |
| 21-30 | mid gear | 6 | 16.8 | 15.2 | 18.9 | 0 | 10-20 | Passed |
| 21-30 | best gear | 6 | 14.4 | 12.8 | 16.1 | 0 | 10-20 | Passed |
| 21-30 | fully modded gear | 6 | 9.3 | 8.5 | 9.9 | 0 | 10-20 | Reported |
| 31-40 | no gear | 6 | 20.9 | 18.4 | 23.4 | 0 | 10-20 | Reported |
| 31-40 | mid gear | 6 | 19.6 | 16.9 | 22.3 | 0 | 10-20 | Passed |
| 31-40 | best gear | 6 | 16.9 | 14.8 | 20.6 | 0 | 10-20 | Passed |
| 31-40 | fully modded gear | 6 | 11.0 | 10.1 | 12.2 | 0 | 10-20 | Reported |
| 41-53 | no gear | 6 | 19.5 | 18.1 | 20.2 | 0 | 10-20 | Reported |
| 41-53 | mid gear | 6 | 20.1 | 19.4 | 20.7 | 0 | 10-20 | Reported |
| 41-53 | best gear | 6 | 19.5 | 17.6 | 21.2 | 0 | 10-20 | Passed |
| 41-53 | fully modded gear | 6 | 12.5 | 10.8 | 14.8 | 0 | 10-20 | Reported |

## Economy parity

### Component stacks consumed per item made, failures included, over the recipes the legacy policy makes at least once in the cell (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Parity rule | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 2.67 | 2.70 | 0.99 | 100% | 98% | 49% | 1% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | mid gear | 6 | 2.73 | 2.68 | 1.02 | 96% | 99% | 88% | 11% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | best gear | 6 | 2.75 | 2.70 | 1.02 | 95% | 98% | 92% | 24% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | fully modded gear | 6 | 2.67 | 2.69 | 0.99 | 100% | 99% | 100% | 76% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | no gear | 6 | 3.34 | 3.50 | 0.95 | 100% | 93% | 79% | 3% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | mid gear | 6 | 3.40 | 3.33 | 1.02 | 97% | 100% | 95% | 13% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | best gear | 6 | 3.34 | 3.36 | 0.99 | 99% | 99% | 99% | 28% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | fully modded gear | 6 | 3.33 | 3.33 | 1.00 | 100% | 100% | 100% | 90% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | no gear | 6 | 0.66 | 0.69 | 0.96 | 98% | 98% | 95% | 9% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | mid gear | 6 | 0.63 | 0.67 | 0.94 | 99% | 98% | 100% | 16% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | best gear | 6 | 0.63 | 0.63 | 1.00 | 100% | 100% | 100% | 33% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | fully modded gear | 6 | 0.63 | 0.63 | 1.00 | 100% | 99% | 100% | 98% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | no gear | 6 | 2.55 | 2.52 | 1.01 | 97% | 99% | 89% | 30% | yes | 0% / 0% | within 10% | Reported |
| 31-40 | mid gear | 6 | 2.57 | 2.56 | 1.00 | 96% | 96% | 94% | 45% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | best gear | 6 | 2.51 | 2.52 | 0.99 | 100% | 99% | 99% | 61% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | fully modded gear | 6 | 2.50 | 2.56 | 0.98 | 100% | 97% | 100% | 87% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | no gear | 6 | 2.77 | 2.70 | 1.03 | 94% | 98% | 55% | 42% | yes | 0% / 0% | within 10% | Reported |
| 41-53 | mid gear | 6 | 2.80 | 2.68 | 1.05 | 93% | 99% | 70% | 65% | yes | 0% / 0% | within 10% | Reported |
| 41-53 | best gear | 6 | 2.74 | 2.68 | 1.02 | 96% | 99% | 86% | 88% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | fully modded gear | 6 | 2.67 | 2.70 | 0.99 | 100% | 98% | 99% | 98% | yes | 0% / 0% | within 10% | Passed |

## XP rate

### Item crafts: XP per hour against the legacy loop (6.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per craft | Legacy actions per craft | Legacy XP per hour | New XP per craft | New actions per craft | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 595 | 5.7 | 62152 | 2052 | 14.0 | 65861 | 1.06 | - | Passed |
| 1-10 | no gear, 1 enhancement | 454 | 6.1 | 44601 | 2077 | 18.1 | 51501 | 1.15 | - | Passed |
| 1-10 | no gear, 2 enhancements | 279 | 6.3 | 26496 | 2058 | 18.1 | 51217 | 1.93 | - | Passed |
| 1-10 | no gear, 3 enhancements | 166 | 6.5 | 15369 | 2000 | 17.8 | 50530 | 3.29 | - | Passed |
| 1-10 | mid gear | 657 | 7.5 | 52269 | 2539 | 17.0 | 67308 | 1.29 | - | Passed |
| 1-10 | mid gear, 1 enhancement | 623 | 7.4 | 50255 | 2201 | 18.4 | 53846 | 1.07 | - | Passed |
| 1-10 | mid gear, 2 enhancements | 597 | 8.1 | 44228 | 2244 | 18.5 | 54507 | 1.23 | - | Passed |
| 1-10 | mid gear, 3 enhancements | 540 | 8.7 | 37063 | 2148 | 18.4 | 52511 | 1.42 | - | Passed |
| 1-10 | best gear | 737 | 8.8 | 50324 | 2589 | 15.2 | 76787 | 1.53 | - | Passed |
| 1-10 | best gear, 1 enhancement | 703 | 8.7 | 48380 | 2451 | 17.4 | 63547 | 1.31 | - | Passed |
| 1-10 | best gear, 2 enhancements | 700 | 9.2 | 45742 | 2458 | 17.1 | 64673 | 1.41 | - | Passed |
| 1-10 | best gear, 3 enhancements | 686 | 9.5 | 43489 | 2490 | 17.6 | 63790 | 1.47 | - | Passed |
| 1-10 | fully modded gear | 1048 | 7.6 | 82822 | 2748 | 9.5 | 129764 | 1.57 | - | Passed |
| 1-10 | fully modded gear, 1 enhancement | 970 | 6.8 | 86107 | 2744 | 10.5 | 117800 | 1.37 | - | Passed |
| 1-10 | fully modded gear, 2 enhancements | 943 | 7.0 | 80759 | 2734 | 10.7 | 115478 | 1.43 | - | Passed |
| 1-10 | fully modded gear, 3 enhancements | 947 | 7.2 | 78944 | 2746 | 10.7 | 115117 | 1.46 | - | Passed |
| 11-20 | no gear | 581 | 7.2 | 48214 | 1843 | 17.0 | 48697 | 1.01 | - | Passed |
| 11-20 | no gear, 1 enhancement | 469 | 8.2 | 34520 | 1891 | 20.1 | 42248 | 1.22 | - | Passed |
| 11-20 | no gear, 2 enhancements | 353 | 7.5 | 28228 | 1913 | 20.1 | 42801 | 1.52 | - | Passed |
| 11-20 | no gear, 3 enhancements | 336 | 8.6 | 23435 | 1919 | 20.0 | 43089 | 1.84 | - | Passed |
| 11-20 | mid gear | 676 | 10.0 | 40393 | 1985 | 17.2 | 51954 | 1.29 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 638 | 11.3 | 33828 | 2003 | 19.2 | 46899 | 1.39 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 635 | 12.7 | 30119 | 1997 | 19.1 | 46940 | 1.56 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 579 | 13.4 | 25873 | 2001 | 19.2 | 46904 | 1.81 | - | Passed |
| 11-20 | best gear | 761 | 14.6 | 31340 | 2048 | 14.9 | 61647 | 1.97 | - | Passed |
| 11-20 | best gear, 1 enhancement | 754 | 14.9 | 30327 | 2064 | 16.3 | 56923 | 1.88 | - | Passed |
| 11-20 | best gear, 2 enhancements | 744 | 16.0 | 27827 | 2064 | 16.7 | 55589 | 2.00 | - | Passed |
| 11-20 | best gear, 3 enhancements | 726 | 16.6 | 26151 | 2063 | 16.9 | 55033 | 2.10 | - | Passed |
| 11-20 | fully modded gear | 1141 | 14.4 | 47556 | 2064 | 9.5 | 98151 | 2.06 | - | Passed |
| 11-20 | fully modded gear, 1 enhancement | 1135 | 14.6 | 46630 | 2064 | 10.2 | 91019 | 1.95 | - | Passed |
| 11-20 | fully modded gear, 2 enhancements | 1128 | 14.6 | 46203 | 2064 | 10.5 | 88453 | 1.91 | - | Passed |
| 11-20 | fully modded gear, 3 enhancements | 1133 | 14.8 | 46043 | 2064 | 10.4 | 89590 | 1.95 | - | Passed |
| 21-30 | no gear | 634 | 11.5 | 33052 | 1384 | 18.5 | 33584 | 1.02 | - | Passed |
| 21-30 | no gear, 1 enhancement | 622 | 13.1 | 28407 | 1357 | 21.1 | 29005 | 1.02 | - | Passed |
| 21-30 | no gear, 2 enhancements | 614 | 13.7 | 26830 | 1392 | 20.8 | 30178 | 1.12 | - | Passed |
| 21-30 | no gear, 3 enhancements | 567 | 14.5 | 23514 | 1398 | 20.8 | 30308 | 1.29 | - | Passed |
| 21-30 | mid gear | 681 | 17.0 | 24045 | 1413 | 16.8 | 37928 | 1.58 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 684 | 18.6 | 22019 | 1428 | 18.4 | 34876 | 1.58 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 675 | 18.5 | 21928 | 1428 | 18.5 | 34774 | 1.59 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 657 | 18.5 | 21282 | 1428 | 18.3 | 35147 | 1.65 | - | Passed |
| 21-30 | best gear | 771 | 22.9 | 20204 | 1428 | 14.4 | 44637 | 2.21 | - | Passed |
| 21-30 | best gear, 1 enhancement | 773 | 24.0 | 19351 | 1428 | 15.8 | 40766 | 2.11 | - | Passed |
| 21-30 | best gear, 2 enhancements | 761 | 24.4 | 18689 | 1428 | 15.8 | 40681 | 2.18 | - | Passed |
| 21-30 | best gear, 3 enhancements | 743 | 24.0 | 18578 | 1428 | 15.5 | 41369 | 2.23 | - | Passed |
| 21-30 | fully modded gear | 1152 | 22.3 | 31014 | 1428 | 9.3 | 68849 | 2.22 | - | Passed |
| 21-30 | fully modded gear, 1 enhancement | 1154 | 23.8 | 29109 | 1428 | 10.1 | 63623 | 2.19 | - | Passed |
| 21-30 | fully modded gear, 2 enhancements | 1143 | 24.0 | 28616 | 1428 | 10.0 | 64099 | 2.24 | - | Passed |
| 21-30 | fully modded gear, 3 enhancements | 1140 | 23.9 | 28645 | 1428 | 10.2 | 62794 | 2.19 | - | Passed |
| 31-40 | no gear | 772 | 16.1 | 28723 | 1116 | 20.9 | 24072 | 0.84 | - | Passed |
| 31-40 | no gear, 1 enhancement | 676 | 14.3 | 28347 | 1083 | 23.1 | 21118 | 0.74 | - | Passed |
| 31-40 | no gear, 2 enhancements | 614 | 15.1 | 24362 | 1072 | 23.2 | 20758 | 0.85 | - | Passed |
| 31-40 | no gear, 3 enhancements | 591 | 15.9 | 22288 | 1049 | 23.0 | 20531 | 0.92 | - | Passed |
| 31-40 | mid gear | 853 | 20.5 | 24985 | 1145 | 19.6 | 26281 | 1.05 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 780 | 18.8 | 24951 | 1131 | 21.1 | 24176 | 0.97 | 1.03x | Failed |
| 31-40 | mid gear, 2 enhancements | 727 | 18.8 | 23159 | 1133 | 21.0 | 24269 | 1.05 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 684 | 18.3 | 22470 | 1137 | 21.3 | 24017 | 1.07 | - | Passed |
| 31-40 | best gear | 958 | 25.1 | 22889 | 1194 | 16.9 | 31844 | 1.39 | - | Passed |
| 31-40 | best gear, 1 enhancement | 881 | 24.8 | 21290 | 1192 | 18.1 | 29674 | 1.39 | - | Passed |
| 31-40 | best gear, 2 enhancements | 859 | 24.8 | 20806 | 1188 | 18.1 | 29482 | 1.42 | - | Passed |
| 31-40 | best gear, 3 enhancements | 818 | 23.4 | 20996 | 1189 | 17.8 | 29998 | 1.43 | - | Passed |
| 31-40 | fully modded gear | 1107 | 24.8 | 26785 | 1200 | 11.0 | 49052 | 1.83 | - | Passed |
| 31-40 | fully modded gear, 1 enhancement | 1097 | 25.7 | 25619 | 1200 | 11.2 | 48268 | 1.88 | - | Passed |
| 31-40 | fully modded gear, 2 enhancements | 1108 | 26.4 | 25212 | 1200 | 11.1 | 48539 | 1.93 | - | Passed |
| 31-40 | fully modded gear, 3 enhancements | 1102 | 26.5 | 24938 | 1200 | 11.2 | 48376 | 1.94 | - | Passed |
| 41-53 | no gear | 844 | 19.7 | 25663 | 901 | 19.5 | 20768 | 0.81 | - | Passed |
| 41-53 | no gear, 1 enhancement | 647 | 15.8 | 24601 | 754 | 20.4 | 16632 | 0.68 | - | Passed |
| 41-53 | no gear, 2 enhancements | 561 | 16.4 | 20527 | 707 | 20.3 | 15666 | 0.76 | - | Passed |
| 41-53 | no gear, 3 enhancements | 485 | 17.1 | 17057 | 708 | 20.1 | 15877 | 0.93 | - | Passed |
| 41-53 | mid gear | 985 | 25.4 | 23314 | 980 | 20.1 | 21926 | 0.94 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 799 | 22.0 | 21736 | 863 | 21.3 | 18262 | 0.84 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 697 | 19.5 | 21467 | 833 | 21.3 | 17595 | 0.82 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 623 | 19.2 | 19466 | 878 | 21.6 | 18281 | 0.94 | - | Passed |
| 41-53 | best gear | 1126 | 29.1 | 23180 | 1097 | 19.5 | 25322 | 1.09 | - | Passed |
| 41-53 | best gear, 1 enhancement | 965 | 27.4 | 21104 | 1018 | 20.7 | 22077 | 1.05 | - | Passed |
| 41-53 | best gear, 2 enhancements | 860 | 25.0 | 20635 | 1019 | 20.6 | 22268 | 1.08 | - | Passed |
| 41-53 | best gear, 3 enhancements | 792 | 23.2 | 20521 | 1042 | 20.8 | 22507 | 1.10 | - | Passed |
| 41-53 | fully modded gear | 1178 | 26.9 | 26246 | 1191 | 12.5 | 42744 | 1.63 | - | Passed |
| 41-53 | fully modded gear, 1 enhancement | 1165 | 27.6 | 25309 | 1239 | 13.0 | 42824 | 1.69 | - | Passed |
| 41-53 | fully modded gear, 2 enhancements | 1144 | 28.4 | 24205 | 1245 | 13.3 | 42112 | 1.74 | - | Passed |
| 41-53 | fully modded gear, 3 enhancements | 1167 | 29.3 | 23878 | 1240 | 13.4 | 41767 | 1.75 | - | Passed |

## Enhancement parity

### Rank 50, fully modded gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 100%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 6 | 60-100 | 34% | 99% | 100% | 0.98 | 100% | 100.0% | 1.00 (+/- 0.00) | 1.02 | >= 0.95 | Passed |
| 41-45 | 2 | 6 | 60-100 | 68% | 95% | 100% | 1.89 | 100% | 100.0% | 2.00 (+/- 0.00) | 1.06 | >= 0.95 | Passed |
| 41-45 | 3 | 6 | 60-100 | 102% | 100% | 99% | 2.97 | 100% | 100.0% | 2.99 (+/- 0.01) | 1.01 | >= 0.90 | Passed |
| 46-50 | 1 | 6 | 60-100 | 43% | 99% | 99% | 0.99 | 99% | 100.0% | 0.99 (+/- 0.01) | 1.01 | >= 0.95 | Passed |
| 46-50 | 2 | 6 | 60-100 | 86% | 98% | 99% | 1.95 | 100% | 99.9% | 1.99 (+/- 0.01) | 1.02 | >= 0.95 | Passed |
| 46-50 | 3 | 6 | 60-100 | 128% | 100% | 99% | 2.97 | 99% | 99.9% | 2.95 (+/- 0.02) | 0.99 | >= 0.90 | Passed |
| 51-53 | 1 | 6 | 60 | 8% | 98% | 42% | 0.41 | 100% | 95.8% | 1.00 (+/- 0.00) | 2.45 | >= 0.95 | Passed |
| 51-53 | 2 | 6 | 60 | 16% | 100% | 48% | 0.95 | 99% | 94.2% | 1.80 (+/- 0.03) | 1.89 | >= 0.95 | Passed |
| 51-53 | 3 | 6 | 60 | 24% | 100% | 37% | 1.10 | 98% | 93.5% | 2.48 (+/- 0.06) | 2.26 | >= 0.90 | Passed |

### Thresholds, first / second / third slot, that would meet parity for the quality measured (the strictest set that keeps every case at least at today's, never falling from one slot to the next)

| Scope | Thresholds that meet parity | In steps of 5 | Thresholds in force |
| --- | --- | --- | --- |
| Every band together (the shared thresholds) | 89% / 89% / 89% | 85% / 85% / 85% | 50% / 85% / 100% |
| Band 41-45 alone | 99% / 100% / 100% | 95% / 100% / 100% | 50% / 85% / 100% |
| Band 46-50 alone | 89% / 89% / 89% | 85% / 85% / 85% | 50% / 85% / 100% |
| Band 51-53 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 100% |

### Rank 50, fully modded gear: quality of the items the new minigame made, in whole percent, by band and slots filled

| Band | Slots filled | Items made | 10th percentile | 25th percentile | Median | 75th percentile | 90th percentile | Made at 100% |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 41-45 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 41-45 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 99% |
| 46-50 | 1 | 99% | 100% | 100% | 100% | 100% | 100% | 96% |
| 46-50 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 97% |
| 46-50 | 3 | 99% | 100% | 100% | 100% | 100% | 100% | 96% |
| 51-53 | 1 | 100% | 80% | 100% | 100% | 100% | 100% | 75% |
| 51-53 | 2 | 99% | 71% | 100% | 100% | 100% | 100% | 75% |
| 51-53 | 3 | 98% | 71% | 99% | 100% | 100% | 100% | 72% |

### Representative enhancements: for each recipe kind the real item at the best level the recipe accepts (the old window refuses one more than 5 levels above the recipe) whose penalty is the lower median of that level

| Recipe kind | Enhancement | Level | Progress penalty | Items of that kind and level (min / median / max penalty) | Recipes |
| --- | --- | --- | --- | --- | --- |
| Armor | Armor Enhancement - Force Attack V (`aen_frcatk5`) | 45 (tier V) | 60 | 50 items: 50 / 60.0 / 150 | 4 |
| Starship | Starship Enhancement - Shield Recharge Rate V (`sen_shrech5`) | 45 (tier V) | 60 | 24 items: 50 / 65.0 / 80 | 1 |
| Module | Module Enhancement - Module Bonus V (`men_mod5`) | 45 (tier V) | 60 | 2 items: 60 / 70.0 / 80 | 9 |
| Droid | Droid Enhancement - Resistance - Ice II (`de_res_ice2`) | 50 (tier VI) | 100 | 8 items: 100 / 100.0 / 100 | 4 |

## Gear ladder

### Rank 50, 2 kits, no perks: what each step of gear buys

| Band | Recipes | Kind | Gear | Avg quality | Reach 85% | Reach 100% | Actions | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 46-50 | 6 | all | no gear | 44.8 | 2% | 0% | 19.8 | Passed |
| 46-50 | 6 | all | mid gear | 61.8 | 10% | 6% | 20.2 | Passed |
| 46-50 | 6 | all | best gear | 76.0 | 63% | 52% | 19.8 | Passed |
| 46-50 | 6 | all | fully modded gear | 99.5 | 100% | 97% | 12.4 | Passed |
| 51-53 | 6 | all | no gear | 21.1 | 0% | 0% | 19.6 | Passed |
| 51-53 | 6 | all | mid gear | 30.4 | 0% | 0% | 20.6 | Passed |
| 51-53 | 6 | all | best gear | 48.6 | 2% | 0% | 21.0 | Passed |
| 51-53 | 6 | all | fully modded gear | 93.5 | 82% | 75% | 15.5 | Passed |

### Perks do not replace gear: plain crafts, average quality

| Band | Style | Fully perked, no gear | No perks, unmodded best gear | Allowance | Verdict |
| --- | --- | --- | --- | --- | --- |
| 46-50 | Overclock (both lines) | 87.2 | 77.0 | +5 | Failed |
| 46-50 | Safe Mode (both lines) | 82.8 | 77.0 | +5 | Failed |
| 51-53 | Overclock (both lines) | 65.1 | 58.9 | +5 | Failed |
| 51-53 | Safe Mode (both lines) | 60.8 | 58.9 | +5 | Passed |

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
- An enhancement's difficulty is the old window's progress penalty added to the Assembly the piece needs, capped at 30% of the Assembly the recipe itself needs; the Circuit's Wire, Tune and Discharge outputs do not change with it.
- Without a style the crafter plays without perks, which is what the perks-optional gate and every gate of the plain scenario play; the style-balance gate plays Overclock and Safe Mode with no line perks and with both lines bought in full (every purchase the crafter's rank allows).
- Seconds per action is an estimate of 8 s until the client evidence measures it.
