# Crafting revamp: balance gates of the Circuit minigame

- Command: `SWLOR.CraftSim gates --scenario circuit --runs 100 --recipes-per-band 6 --seed 1 --out design/testing/crafting-revamp/circuit-gates.md --json C:/Users/Zunath/AppData/Local/Temp/claude/C--Projects-SWLOR-NWN--claude-worktrees-lockbox-reward-pool-variety-9747f8/3c27674f-7215-4512-9b37-01d48c93d13d/scratchpad/sc/sim3/circuit-gates.json`
- Seed 1; 100 runs per case.

## Inputs

- Scenario: Circuit (`circuit`), Engineering recipes, minigame Circuit.
- Quality: the quality meter's share of its maximum when the item is made, in points (0-100). A broken or abandoned piece counts 0, so every average includes the crafts that failed.
- Crafter: the rank the scenario's cohort gives each recipe (by default the recipe's level, at most 50, so a crafter works at their own level), with no food or status bonuses and no perks unless a gate names a style or the scenario's limitations say it plays a perk loadout. Gear levels are the plan's none, middle and best tier of the skill's crafting gear:
  - no gear (Control 0, Craftsmanship 0, CP 0)
  - mid gear (Control 13, Craftsmanship 12, CP 20)
  - best gear (Control 29, Craftsmanship 30, CP 37)
- Recipes: 1-10: 6 of 442; 11-20: 6 of 453; 21-30: 6 of 443; 31-40: 6 of 443; 41-53: 6 of 419 recipes played.
- Adaptive player: depth 3, forecast samples per result 2, budget 4000 engine copies per decision, 0.5% of value lost per extra action, heuristic DefaultLeafHeuristic. Each attempt's dice come from its own stream of the seed.
- Fixed-rotation solver: beam 8, cycles up to 3, 8 adaptive lines as seeds, up to 40 actions, 64 training attempts per local-search round and 150 selection attempts for each of 12 finalists, local search from the best 3 candidate(s) for up to 3 round(s) each. Gate 1 searches one sequence for each band and gear tier across the band's sampled recipes (every recipe weighing the same; an action a recipe's engine cannot take is skipped there), and also each recipe's own best sequence as a reported metric. The winner is chosen on separate attempts and then scored on a third set (hold-out) with 100 attempts per recipe.
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
  - `RecipesPerBand` = 6: recipes played per band
  - `SolverOptions` = the scenario's: effort of the fixed-rotation solver
  - `ActionLimit` = 100: actions after which an attempt counts as stalled
- Length target: 10-20 actions (item craft at level).
- Enhancement thresholds in force (`CraftOutcomeRules`, read by gate 7): 50% / 85% / 100% for slots 1, 2 and 3. The scenario applies enhancements to its engine's progress meter.
- Styles compared: Overclock (no lines) vs Safe Mode (no lines) (from rank 20); Overclock (both lines) vs Safe Mode (both lines) (from rank 20).

## Verdicts

| Gate | Verdict | Summary |
| --- | --- | --- |
| No fixed rotation | Failed | One fixed rotation comes within 10 points of adaptive play in 15 case(s): 1-10 / no gear; 1-10 / mid gear; 1-10 / best gear; 11-20 / no gear; 11-20 / mid gear; 11-20 / best gear; 21-30 / no gear; 21-30 / mid gear; 21-30 / best gear; 31-40 / no gear; 31-40 / mid gear; 31-40 / best gear; 41-53 / no gear; 41-53 / mid gear; 41-53 / best gear (smallest margin -8.3 in 1-10 / mid gear). The best rotation for each recipe alone, a reported metric, is -10.0 to -0.0 points behind adaptive play. |
| Style balance | Passed | Every style is the better choice on at least 30% of recipes and no pair differs by more than 5 points on average. |
| Perks optional | Passed | An unperked rank-50 crafter in best gear reaches maximum quality on at least 50% of crafts in every at-level band (lowest 97%). |
| Length | Passed | Every band and gear tier takes 10-20 actions (item craft at level) on average. |
| Economy parity | Passed | Component stacks per item are within 10% of the legacy baseline where its success rate is at least 90%, and at most 10% above it where it is lower, at no less quality, in every band and gear tier. |
| XP rate | Passed | Item crafts: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.00x in 21-30 / no gear). |
| Enhancement parity | Passed | At rank 50 in best gear, the thresholds in force (50% / 85% / 100%) transfer at least 1.00 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 1.09 times today's). Thresholds that meet parity in every band for this quality: 100% / 100% / 100%. |

## No fixed rotation

### Average quality: adaptive play against the best single fixed rotation of each band and gear tier (quality points; a broken piece counts 0)

| Band | Gear | Recipes played | Adaptive avg quality | Best single rotation avg quality | Margin | Required | Best rotation per recipe avg quality (reported) | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 50.0 | 51.9 | -1.9 | >= 10 | 52.7 | Failed |
| 1-10 | mid gear | 6 | 87.3 | 95.6 | -8.3 | >= 10 | 97.3 | Failed |
| 1-10 | best gear | 6 | 94.1 | 98.2 | -4.1 | >= 10 | 100.0 | Failed |
| 11-20 | no gear | 6 | 77.6 | 84.3 | -6.7 | >= 10 | 87.3 | Failed |
| 11-20 | mid gear | 6 | 95.0 | 100.0 | -5.0 | >= 10 | 100.0 | Failed |
| 11-20 | best gear | 6 | 99.6 | 100.0 | -0.4 | >= 10 | 100.0 | Failed |
| 21-30 | no gear | 6 | 97.0 | 100.0 | -3.0 | >= 10 | 100.0 | Failed |
| 21-30 | mid gear | 6 | 98.6 | 100.0 | -1.4 | >= 10 | 100.0 | Failed |
| 21-30 | best gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Failed |
| 31-40 | no gear | 6 | 99.7 | 100.0 | -0.3 | >= 10 | 100.0 | Failed |
| 31-40 | mid gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Failed |
| 31-40 | best gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Failed |
| 41-53 | no gear | 6 | 99.9 | 100.0 | -0.1 | >= 10 | 100.0 | Failed |
| 41-53 | mid gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Failed |
| 41-53 | best gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Failed |

### The single rotation found for each band and gear tier, searched across the band's sampled recipes (average quality in points)

| Band | Gear | Best single rotation for the band | Training | Selection | Hold-out | Adaptive on these recipes |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | wire x3 > tune x2 > discharge_calibration > wire x4 > discharge_calibration > wire x3 > discharge_calibration > wire | 51.4 | 52.0 | 51.9 | 50.0 |
| 1-10 | mid gear | tune x2 > wire > tune > discharge_calibration > tune x2 > wire > tune > discharge_calibration > wire x4 > discharge_calibration > wire > tune > wire x4 | 96.6 | 95.6 | 95.6 | 87.3 |
| 1-10 | best gear | discharge_assembly > tune x3 > discharge_calibration > tune x3 > discharge_calibration > tune > wire x2 > discharge_calibration > wire x2 > tune > wire > discharge_assembly > wire x3 | 99.0 | 98.8 | 98.2 | 94.1 |
| 11-20 | no gear | wire > tune x3 > discharge_calibration > wire x2 > tune x2 > discharge_calibration > wire x4 > discharge_calibration > wire x3 > discharge_calibration > tune > wire > discharge_calibration > wire x3 | 84.1 | 84.3 | 84.3 | 77.6 |
| 11-20 | mid gear | wire > tune x3 > discharge_calibration > tune x4 > patch > discharge_calibration > wire x3 > discharge_calibration > wire x4 > discharge_calibration > wire x2 | 100.0 | 100.0 | 100.0 | 95.0 |
| 11-20 | best gear | tune x4 > discharge_calibration > tune x4 > discharge_calibration > wire x4 > discharge_assembly > wire > tune > wire x2 | 100.0 | 100.0 | 100.0 | 99.6 |
| 21-30 | no gear | tune x6 > discharge_calibration > wire > tune x3 > patch > discharge_calibration > wire x3 > discharge_calibration > wire x5 > discharge_calibration > patch > tune > wire x2 | 100.0 | 100.0 | 100.0 | 97.0 |
| 21-30 | mid gear | tune x6 > discharge_calibration > tune x3 > patch > tune > wire > discharge_assembly > wire x5 > discharge_calibration > wire > discharge_assembly > tune > patch > tune > wire | 100.0 | 100.0 | 100.0 | 98.6 |
| 21-30 | best gear | tune x5 > discharge_calibration > tune x3 > wire > discharge_calibration > wire x5 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | boost > tune x3 > discharge_calibration > tune x4 > discharge_calibration > tune x3 > wire x3 > discharge_assembly > wire x4 > tune > wire | 100.0 | 100.0 | 100.0 | 99.7 |
| 31-40 | mid gear | boost > tune x3 > discharge_calibration > tune x5 > discharge_calibration > wire x6 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | boost > tune x4 > discharge_calibration > tune x3 > wire x3 > discharge_assembly > tune > wire x2 > tune > discharge_calibration > patch > tune > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | boost > tune x4 > discharge_calibration > tune x6 > wire > discharge_assembly > tune > wire x4 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 | 99.9 |
| 41-53 | mid gear | boost > tune x4 > discharge_calibration > tune x5 > wire x2 > discharge_assembly > tune > wire x4 | 100.0 | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | boost > tune x4 > discharge_calibration > tune x3 > wire x4 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 | 100.0 |

### Reported metric, not gated: the best fixed rotation for each recipe alone, and what the band's single rotation scores on the same recipe (average quality in points)

| Band | Gear | Recipe | Best rotation for this recipe alone (reported) | Its hold-out | The single rotation on this recipe | Adaptive on this recipe |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | sm_capr_00 (level 2) | wire x5 > discharge_calibration > wire > discharge_calibration > wire x4 > discharge_calibration > wire | 36.4 | 34.6 | 32.3 |
| 1-10 | no gear | dmotive_sys1 (level 5) | wire x2 > tune > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire | 48.4 | 46.1 | 46.8 |
| 1-10 | no gear | d_bd_mw1 (level 6) | wire x4 > tune > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire | 48.0 | 47.8 | 42.6 |
| 1-10 | no gear | ship_fuelcapsule (level 8) | wire > tune > wire > tune x2 > discharge_calibration > wire x4 > discharge_calibration > wire x4 > discharge_calibration > wire > discharge_calibration > wire | 57.6 | 56.9 | 55.3 |
| 1-10 | no gear | dehl001 (level 10) | wire x3 > tune x2 > discharge_calibration > wire x5 > discharge_calibration > wire x2 > discharge_calibration > wire | 64.4 | 63.4 | 62.8 |
| 1-10 | no gear | sm_resall_10 (level 10) | wire x2 > tune x2 > wire > discharge_calibration > wire x4 > discharge_calibration > wire x3 > discharge_calibration > wire > discharge_calibration > wire | 61.6 | 62.7 | 60.2 |
| 1-10 | mid gear | sm_capr_00 (level 2) | boost > tune x2 > discharge_calibration > tune x3 > wire x2 > discharge_calibration > wire x5 > discharge_calibration > wire x2 | 100.0 | 99.9 | 94.0 |
| 1-10 | mid gear | dmotive_sys1 (level 5) | wire > tune x2 > discharge_calibration > tune x3 > wire > discharge_assembly > tune > wire x2 > discharge_calibration > tune > wire x2 > discharge_calibration > wire x2 | 84.5 | 75.0 | 78.4 |
| 1-10 | mid gear | d_bd_mw1 (level 6) | tune x3 > wire > discharge_calibration > tune x2 > wire > tune > discharge_calibration > wire x4 > discharge_calibration > wire > tune > wire x4 | 99.6 | 99.8 | 92.9 |
| 1-10 | mid gear | ship_fuelcapsule (level 8) | wire > tune > wire > tune > discharge_calibration > tune x4 > discharge_calibration > discharge_assembly > wire x4 > discharge_calibration > discharge_assembly > wire > tune > wire x2 | 99.7 | 99.4 | 83.3 |
| 1-10 | mid gear | dehl001 (level 10) | tune x5 > discharge_calibration > wire > tune x3 > wire > discharge_assembly > wire x2 > tune > wire x2 > discharge_calibration > wire | 100.0 | 99.7 | 90.4 |
| 1-10 | mid gear | sm_resall_10 (level 10) | tune > wire x2 > tune x2 > discharge_calibration > tune x3 > patch > discharge_calibration > wire x2 > discharge_calibration > wire x4 > discharge_calibration > wire x3 > discharge_calibration > wire | 100.0 | 99.7 | 85.0 |
| 1-10 | best gear | sm_capr_00 (level 2) | tune x5 > discharge_calibration > tune > wire > tune x2 > wire > discharge_assembly > tune > wire x3 | 100.0 | 100.0 | 93.0 |
| 1-10 | best gear | dmotive_sys1 (level 5) | discharge_assembly > tune x3 > discharge_calibration > tune x3 > discharge_calibration > tune > wire x2 > discharge_calibration > wire > tune > wire > discharge_assembly > wire x2 | 100.0 | 89.0 | 93.6 |
| 1-10 | best gear | d_bd_mw1 (level 6) | tune x4 > discharge_calibration > tune x3 > wire > discharge_calibration > wire > tune > wire x2 > discharge_assembly > wire x3 | 100.0 | 100.0 | 88.6 |
| 1-10 | best gear | ship_fuelcapsule (level 8) | tune > wire > tune x2 > discharge_calibration > tune x3 > discharge_calibration > boost > wire > discharge_calibration > wire x2 > tune > wire > discharge_assembly > wire x3 | 100.0 | 100.0 | 89.7 |
| 1-10 | best gear | dehl001 (level 10) | tune x5 > discharge_calibration > tune x4 > wire > discharge_assembly > wire x5 | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | sm_resall_10 (level 10) | tune x5 > discharge_calibration > tune x3 > wire x2 > discharge_assembly > wire x3 > tune > wire | 100.0 | 100.0 | 99.6 |
| 11-20 | no gear | dlbe001a (level 14) | wire > tune x3 > discharge_calibration > wire x4 > discharge_calibration > wire x4 > discharge_calibration > wire > discharge_calibration > wire | 74.0 | 72.7 | 72.4 |
| 11-20 | no gear | sd_hutthauler (level 15) | boost > wire > tune x3 > discharge_calibration > wire x2 > discharge_calibration > wire x6 > discharge_calibration > wire > discharge_calibration > wire | 75.1 | 67.5 | 70.6 |
| 11-20 | no gear | dhnk002a (level 17) | wire > tune x3 > discharge_calibration > wire x3 > tune > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire > discharge_calibration > wire | 85.8 | 83.4 | 71.6 |
| 11-20 | no gear | d_jg_cpu2_v (level 20) | wire > tune x3 > discharge_calibration > wire x2 > tune x2 > discharge_calibration > wire x4 > discharge_calibration > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire x3 | 97.2 | 97.2 | 81.8 |
| 11-20 | no gear | id_hookstri2 (level 20) | wire > tune x3 > discharge_calibration > wire x2 > tune x2 > discharge_calibration > wire x4 > discharge_calibration > wire x4 > discharge_calibration > wire > discharge_calibration > wire | 92.9 | 91.8 | 82.1 |
| 11-20 | no gear | sm_plate_20 (level 20) | wire > tune > wire > tune x3 > discharge_calibration > wire x3 > discharge_calibration > wire x5 > tune > discharge_calibration > wire x2 > discharge_calibration > wire x3 > discharge_calibration > wire | 98.6 | 93.3 | 87.3 |
| 11-20 | mid gear | dlbe001a (level 14) | wire > tune x3 > discharge_calibration > tune x4 > patch > discharge_calibration > wire x3 > discharge_calibration > wire x4 > discharge_calibration > wire x2 | 100.0 | 100.0 | 98.4 |
| 11-20 | mid gear | sd_hutthauler (level 15) | tune x6 > discharge_calibration > tune x2 > wire > tune > wire x2 > discharge_assembly > wire x3 > tune > wire | 100.0 | 100.0 | 86.3 |
| 11-20 | mid gear | dhnk002a (level 17) | tune x4 > discharge_calibration > tune x3 > discharge_calibration > tune > wire x2 > tune > discharge_assembly > wire x2 > tune > wire > discharge_assembly > wire x2 | 100.0 | 100.0 | 96.2 |
| 11-20 | mid gear | d_jg_cpu2_v (level 20) | tune x4 > discharge_calibration > tune x2 > discharge_calibration > tune x3 > wire > discharge_assembly > tune > wire x3 > discharge_assembly > tune > wire x2 | 100.0 | 100.0 | 98.5 |
| 11-20 | mid gear | id_hookstri2 (level 20) | tune x4 > discharge_calibration > tune x3 > discharge_calibration > wire > tune x2 > wire > patch > discharge_calibration > wire x8 | 100.0 | 100.0 | 98.9 |
| 11-20 | mid gear | sm_plate_20 (level 20) | boost > tune x3 > discharge_calibration > tune x5 > wire > discharge_assembly > tune > wire x3 > discharge_assembly > tune > wire | 100.0 | 100.0 | 91.9 |
| 11-20 | best gear | dlbe001a (level 14) | tune x5 > discharge_calibration > patch > tune x3 > wire > discharge_calibration > wire x4 > discharge_assembly > wire > tune x2 > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | sd_hutthauler (level 15) | boost > tune x3 > discharge_calibration > tune x5 > wire > discharge_assembly > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 97.7 |
| 11-20 | best gear | dhnk002a (level 17) | boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | d_jg_cpu2_v (level 20) | tune x4 > discharge_calibration > tune x4 > discharge_calibration > wire x4 > discharge_assembly > wire x3 | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | id_hookstri2 (level 20) | tune x4 > discharge_calibration > tune x4 > discharge_calibration > wire x4 > discharge_assembly > wire > tune > wire x2 | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | sm_plate_20 (level 20) | boost > tune x3 > discharge_calibration > tune x4 > wire x2 > discharge_assembly > tune > wire x3 | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | dhhl003 (level 24) | tune x6 > discharge_calibration > wire x4 > discharge_calibration > wire x4 > discharge_calibration > wire x2 > discharge_calibration > wire | 100.0 | 100.0 | 90.1 |
| 21-30 | no gear | ship_missile_inc (level 25) | boost > tune x4 > discharge_calibration > tune x5 > wire > tune > discharge_assembly > wire x4 > discharge_assembly > wire | 100.0 | 100.0 | 97.6 |
| 21-30 | no gear | wen_atk3 (level 28) | boost > tune x3 > discharge_calibration > tune x6 > patch > discharge_calibration > wire x6 > discharge_assembly > wire x2 | 100.0 | 100.0 | 96.9 |
| 21-30 | no gear | staff_upg3 (level 29) | boost > tune x3 > discharge_calibration > tune x6 > patch > discharge_assembly > tune > wire x4 > discharge_assembly > tune > wire > tune > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | id_soulstrike3 (level 30) | tune x5 > discharge_calibration > wire > tune x4 > patch > discharge_calibration > tune > wire x4 > discharge_assembly > wire > tune > wire x3 | 100.0 | 100.0 | 98.6 |
| 21-30 | no gear | sd_sithhammerhea (level 30) | discharge_assembly > tune x5 > discharge_calibration > tune x4 > patch > tune > discharge_assembly > wire > tune > wire x3 > discharge_assembly > wire x2 > tune x2 > wire | 100.0 | 100.0 | 98.9 |
| 21-30 | mid gear | dhhl003 (level 24) | tune x6 > discharge_calibration > wire > tune x3 > wire x2 > discharge_assembly > wire x4 | 100.0 | 100.0 | 91.8 |
| 21-30 | mid gear | ship_missile_inc (level 25) | boost > tune x4 > discharge_calibration > boost > tune x4 > discharge_assembly > wire x4 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | wen_atk3 (level 28) | tune x6 > discharge_calibration > tune x4 > wire x2 > discharge_assembly > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | staff_upg3 (level 29) | tune x6 > discharge_calibration > tune x4 > wire x2 > discharge_assembly > boost > wire x2 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | id_soulstrike3 (level 30) | tune x5 > discharge_calibration > wire > tune x4 > patch > discharge_calibration > wire x5 > discharge_assembly > wire x2 | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | sd_sithhammerhea (level 30) | tune x5 > discharge_calibration > tune x5 > patch > discharge_assembly > wire x5 > discharge_assembly > tune > wire | 100.0 | 99.8 | 100.0 |
| 21-30 | best gear | dhhl003 (level 24) | boost > tune x3 > discharge_calibration > tune x4 > wire x2 > discharge_assembly > tune > wire x3 | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | ship_missile_inc (level 25) | boost > tune x4 > discharge_calibration > tune x2 > wire > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | wen_atk3 (level 28) | boost > tune x3 > discharge_calibration > tune x5 > wire > discharge_assembly > wire > tune > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | staff_upg3 (level 29) | boost > tune x3 > discharge_calibration > tune x5 > wire > discharge_assembly > boost > wire x2 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | id_soulstrike3 (level 30) | tune x5 > discharge_calibration > tune x4 > wire > discharge_assembly > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | sd_sithhammerhea (level 30) | tune x5 > discharge_calibration > tune x4 > wire > discharge_assembly > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | aen_cft_agr4 (level 34) | boost > tune x3 > discharge_calibration > tune x4 > discharge_calibration > tune x3 > wire x3 > discharge_assembly > wire x4 > tune > wire | 100.0 | 100.0 | 99.8 |
| 31-40 | no gear | sm_smug_35 (level 35) | boost > tune x5 > discharge_calibration > tune x3 > wire x4 > discharge_assembly > tune > wire x2 | 100.0 | 100.0 | 99.0 |
| 31-40 | no gear | aen_def_fir4 (level 38) | boost > tune x4 > discharge_calibration > tune x6 > wire > discharge_assembly > boost > wire x3 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | d_hd_mva4 (level 40) | boost > tune x5 > discharge_calibration > boost > tune x2 > wire x3 > discharge_assembly > wire x2 | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | id_inteshot2 (level 40) | boost > tune x3 > discharge_calibration > tune x4 > discharge_calibration > tune > wire > tune > wire x3 > discharge_assembly > wire x3 > tune > wire | 100.0 | 100.0 | 99.8 |
| 31-40 | no gear | sm_resem_40 (level 40) | boost > tune x4 > discharge_calibration > boost > tune x2 > discharge_calibration > tune > wire x6 > discharge_assembly > wire | 100.0 | 100.0 | 99.8 |
| 31-40 | mid gear | aen_cft_agr4 (level 34) | boost > tune x3 > discharge_calibration > tune x5 > discharge_calibration > wire x6 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | sm_smug_35 (level 35) | boost > tune x5 > discharge_calibration > tune x2 > wire x5 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | aen_def_fir4 (level 38) | boost > tune x4 > discharge_calibration > tune x5 > wire x2 > discharge_assembly > wire x4 | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | d_hd_mva4 (level 40) | boost > tune x5 > discharge_calibration > tune > wire x5 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | id_inteshot2 (level 40) | boost > tune x3 > discharge_calibration > tune x6 > discharge_assembly > tune > wire x4 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | sm_resem_40 (level 40) | boost > tune x5 > discharge_calibration > tune x2 > wire x5 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | aen_cft_agr4 (level 34) | boost > tune x3 > discharge_calibration > tune x5 > wire > discharge_assembly > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | sm_smug_35 (level 35) | boost > tune x5 > discharge_calibration > wire x5 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | aen_def_fir4 (level 38) | boost > tune x4 > discharge_calibration > tune x2 > wire > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | d_hd_mva4 (level 40) | boost > tune x5 > discharge_calibration > boost > wire x4 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | id_inteshot2 (level 40) | boost > tune x3 > discharge_calibration > tune x5 > wire > discharge_assembly > tune > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | sm_resem_40 (level 40) | boost > tune x5 > discharge_calibration > wire x5 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | sm_lbatt_40 (level 43) | boost > tune x4 > discharge_calibration > boost > tune x4 > wire > discharge_assembly > wire x2 > tune > wire > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | sm_tcomp_45 (level 45) | boost > tune x6 > discharge_calibration > tune > wire x6 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | wen_stm5 (level 47) | boost > tune x4 > discharge_calibration > tune x6 > wire > discharge_assembly > wire x6 > tune > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | saber_upg5 (level 48) | boost > tune x4 > discharge_calibration > tune > wire > tune x4 > wire > discharge_assembly > wire x2 > tune > wire > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | id_wristrck3 (level 50) | boost > tune x4 > discharge_calibration > tune x6 > wire > discharge_assembly > boost > wire x3 > discharge_assembly > tune > wire | 100.0 | 100.0 | 99.6 |
| 41-53 | no gear | id_palmstrike3 (level 50) | boost > tune x4 > discharge_calibration > tune x6 > wire x2 > discharge_assembly > tune > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | sm_lbatt_40 (level 43) | boost > tune x4 > discharge_calibration > tune x4 > wire x4 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | sm_tcomp_45 (level 45) | boost > tune x6 > discharge_calibration > boost > wire x4 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | wen_stm5 (level 47) | boost > tune x4 > discharge_calibration > tune x5 > wire x2 > discharge_assembly > tune > wire x4 | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | saber_upg5 (level 48) | boost > tune x4 > discharge_calibration > tune x4 > wire x4 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | id_wristrck3 (level 50) | boost > tune x4 > discharge_calibration > boost > tune x2 > discharge_calibration > wire x6 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | id_palmstrike3 (level 50) | boost > tune x4 > discharge_calibration > tune x5 > wire x3 > discharge_assembly > wire x3 | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | sm_lbatt_40 (level 43) | boost > tune x4 > discharge_calibration > tune x3 > wire x4 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | sm_tcomp_45 (level 45) | boost > tune x4 > discharge_calibration > boost > tune x2 > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | wen_stm5 (level 47) | boost > tune x4 > discharge_calibration > tune x4 > wire x3 > discharge_assembly > wire x2 | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | saber_upg5 (level 48) | boost > tune x4 > discharge_calibration > tune > wire > tune x2 > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | id_wristrck3 (level 50) | boost > tune x4 > discharge_calibration > tune > wire > tune x2 > wire x3 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | id_palmstrike3 (level 50) | boost > tune x4 > discharge_calibration > tune x3 > wire x4 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |

## Style balance

### Style pairs: how often each style is better, and the average quality (quality points; ties count half a win each)

| Style pair | Gear | Recipes | First better | Second better | First avg quality | Second avg quality | Difference | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Overclock (no lines) vs Safe Mode (no lines) | no gear | 21 | 57% | 43% | 97.9 | 97.2 | +0.8 | Passed |
| Overclock (no lines) vs Safe Mode (no lines) | mid gear | 21 | 55% | 45% | 99.5 | 98.5 | +1.1 | Passed |
| Overclock (no lines) vs Safe Mode (no lines) | best gear | 21 | 50% | 50% | 100.0 | 100.0 | +0.0 | Passed |
| Overclock (both lines) vs Safe Mode (both lines) | no gear | 21 | 52% | 48% | 99.7 | 99.0 | +0.6 | Passed |
| Overclock (both lines) vs Safe Mode (both lines) | mid gear | 21 | 50% | 50% | 100.0 | 100.0 | -0.0 | Passed |
| Overclock (both lines) vs Safe Mode (both lines) | best gear | 21 | 50% | 50% | 100.0 | 100.0 | +0.0 | Passed |

## Perks optional

### Rank 50, best gear, no perks: adaptive attempts that reach maximum quality, by recipe band

| Band | Recipes played | Attempts | Reach maximum quality | Make the item | Avg quality of items made | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 6 | 600 | 97% | 100% | 100% | >= 50% | Passed |
| 46-50 | 6 | 600 | 98% | 100% | 100% | >= 50% | Passed |
| 51-53 | 6 | 600 | 98% | 100% | 100% | >= 50% | Passed |

## Length

### Actions per attempt of the adaptive player at level (item craft at level)

| Band | Gear | Recipes played | Mean actions per attempt | Shortest recipe | Longest recipe | Stalled attempts | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 14.1 | 12.0 | 15.1 | 0 | 10-20 | Passed |
| 1-10 | mid gear | 6 | 16.9 | 15.6 | 18.6 | 0 | 10-20 | Passed |
| 1-10 | best gear | 6 | 15.2 | 14.5 | 16.1 | 0 | 10-20 | Passed |
| 11-20 | no gear | 6 | 16.9 | 15.6 | 20.2 | 0 | 10-20 | Passed |
| 11-20 | mid gear | 6 | 17.2 | 16.0 | 18.9 | 0 | 10-20 | Passed |
| 11-20 | best gear | 6 | 15.0 | 14.2 | 16.5 | 0 | 10-20 | Passed |
| 21-30 | no gear | 6 | 18.9 | 16.5 | 21.1 | 0 | 10-20 | Passed |
| 21-30 | mid gear | 6 | 16.9 | 15.1 | 18.4 | 0 | 10-20 | Passed |
| 21-30 | best gear | 6 | 14.6 | 13.2 | 15.8 | 0 | 10-20 | Passed |
| 31-40 | no gear | 6 | 17.2 | 14.2 | 19.7 | 0 | 10-20 | Passed |
| 31-40 | mid gear | 6 | 15.5 | 13.1 | 17.7 | 0 | 10-20 | Passed |
| 31-40 | best gear | 6 | 13.7 | 12.2 | 15.3 | 0 | 10-20 | Passed |
| 41-53 | no gear | 6 | 17.1 | 15.0 | 18.9 | 0 | 10-20 | Passed |
| 41-53 | mid gear | 6 | 15.2 | 13.5 | 17.2 | 0 | 10-20 | Passed |
| 41-53 | best gear | 6 | 13.5 | 12.2 | 14.7 | 0 | 10-20 | Passed |

## Economy parity

### Component stacks consumed per item made, failures included, over the recipes the legacy policy makes at least once in the cell (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Parity rule | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 2.67 | 2.71 | 0.98 | 100% | 97% | 50% | 1% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | mid gear | 6 | 2.76 | 2.73 | 1.01 | 95% | 96% | 87% | 10% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | best gear | 6 | 2.72 | 2.70 | 1.01 | 97% | 98% | 94% | 24% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | no gear | 6 | 3.34 | 3.50 | 0.96 | 100% | 93% | 78% | 3% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | mid gear | 6 | 3.39 | 3.34 | 1.01 | 97% | 99% | 95% | 12% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | best gear | 6 | 3.34 | 3.35 | 1.00 | 100% | 99% | 100% | 29% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | no gear | 6 | 0.68 | 0.71 | 0.95 | 99% | 98% | 97% | 9% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | mid gear | 6 | 0.67 | 0.69 | 0.96 | 99% | 98% | 100% | 16% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | best gear | 6 | 0.67 | 0.67 | 1.00 | 100% | 99% | 100% | 32% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | no gear | 6 | 2.50 | 2.54 | 0.98 | 100% | 97% | 100% | 30% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | mid gear | 6 | 2.50 | 2.54 | 0.98 | 100% | 98% | 100% | 47% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | best gear | 6 | 2.50 | 2.52 | 0.99 | 100% | 99% | 100% | 61% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | no gear | 6 | 2.83 | 2.86 | 0.99 | 100% | 99% | 100% | 41% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | mid gear | 6 | 2.83 | 2.84 | 1.00 | 100% | 100% | 100% | 66% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | best gear | 6 | 2.83 | 2.84 | 1.00 | 100% | 100% | 100% | 89% | yes | 0% / 0% | within 10% | Passed |

## XP rate

### Item crafts: XP per hour against the legacy loop (6.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per craft | Legacy actions per craft | Legacy XP per hour | New XP per craft | New actions per craft | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 591 | 5.6 | 62931 | 2060 | 14.1 | 65892 | 1.05 | - | Passed |
| 1-10 | no gear, 1 enhancement | 453 | 6.1 | 44392 | 2070 | 18.0 | 51641 | 1.16 | - | Passed |
| 1-10 | no gear, 2 enhancements | 279 | 6.3 | 26424 | 2049 | 18.1 | 51054 | 1.93 | - | Passed |
| 1-10 | no gear, 3 enhancements | 164 | 6.5 | 15134 | 2027 | 17.9 | 50969 | 3.37 | - | Passed |
| 1-10 | mid gear | 643 | 7.6 | 50946 | 2517 | 16.9 | 66998 | 1.32 | - | Passed |
| 1-10 | mid gear, 1 enhancement | 619 | 7.4 | 50208 | 2232 | 18.5 | 54330 | 1.08 | - | Passed |
| 1-10 | mid gear, 2 enhancements | 604 | 8.1 | 44579 | 2218 | 18.4 | 54096 | 1.21 | - | Passed |
| 1-10 | mid gear, 3 enhancements | 544 | 8.6 | 37714 | 2155 | 18.3 | 52880 | 1.40 | - | Passed |
| 1-10 | best gear | 734 | 8.8 | 50163 | 2632 | 15.2 | 77780 | 1.55 | - | Passed |
| 1-10 | best gear, 1 enhancement | 706 | 8.7 | 48876 | 2460 | 17.3 | 63916 | 1.31 | - | Passed |
| 1-10 | best gear, 2 enhancements | 699 | 9.1 | 45854 | 2492 | 17.3 | 64693 | 1.41 | - | Passed |
| 1-10 | best gear, 3 enhancements | 685 | 9.5 | 43467 | 2451 | 17.4 | 63201 | 1.45 | - | Passed |
| 11-20 | no gear | 582 | 7.2 | 48420 | 1828 | 16.9 | 48644 | 1.00 | - | Passed |
| 11-20 | no gear, 1 enhancement | 437 | 8.1 | 32413 | 1906 | 20.2 | 42524 | 1.31 | - | Passed |
| 11-20 | no gear, 2 enhancements | 348 | 7.5 | 27837 | 1922 | 20.1 | 42998 | 1.54 | - | Passed |
| 11-20 | no gear, 3 enhancements | 331 | 8.6 | 23185 | 1923 | 20.1 | 43008 | 1.85 | - | Passed |
| 11-20 | mid gear | 670 | 10.1 | 39728 | 1986 | 17.2 | 51993 | 1.31 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 646 | 11.3 | 34431 | 1977 | 19.0 | 46714 | 1.36 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 639 | 12.6 | 30484 | 1996 | 19.2 | 46800 | 1.54 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 588 | 13.2 | 26629 | 2009 | 19.1 | 47309 | 1.78 | - | Passed |
| 11-20 | best gear | 767 | 14.6 | 31540 | 2057 | 15.0 | 61718 | 1.96 | - | Passed |
| 11-20 | best gear, 1 enhancement | 758 | 14.9 | 30506 | 2061 | 16.4 | 56626 | 1.86 | - | Passed |
| 11-20 | best gear, 2 enhancements | 740 | 16.2 | 27478 | 2064 | 16.5 | 56404 | 2.05 | - | Passed |
| 11-20 | best gear, 3 enhancements | 723 | 16.6 | 26131 | 2063 | 16.6 | 55819 | 2.14 | - | Passed |
| 21-30 | no gear | 634 | 11.5 | 33221 | 1399 | 18.9 | 33289 | 1.00 | - | Passed |
| 21-30 | no gear, 1 enhancement | 622 | 13.2 | 28252 | 1376 | 21.0 | 29512 | 1.04 | - | Passed |
| 21-30 | no gear, 2 enhancements | 608 | 13.7 | 26557 | 1399 | 20.7 | 30480 | 1.15 | - | Passed |
| 21-30 | no gear, 3 enhancements | 579 | 14.3 | 24276 | 1401 | 20.9 | 30202 | 1.24 | - | Passed |
| 21-30 | mid gear | 678 | 16.9 | 24038 | 1411 | 16.9 | 37557 | 1.56 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 682 | 18.3 | 22303 | 1428 | 18.6 | 34588 | 1.55 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 669 | 18.5 | 21702 | 1428 | 18.4 | 34997 | 1.61 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 660 | 18.6 | 21304 | 1428 | 18.2 | 35389 | 1.66 | - | Passed |
| 21-30 | best gear | 765 | 22.8 | 20117 | 1428 | 14.6 | 43881 | 2.18 | - | Passed |
| 21-30 | best gear, 1 enhancement | 768 | 24.0 | 19179 | 1428 | 15.8 | 40790 | 2.13 | - | Passed |
| 21-30 | best gear, 2 enhancements | 756 | 24.5 | 18517 | 1428 | 15.8 | 40734 | 2.20 | - | Passed |
| 21-30 | best gear, 3 enhancements | 743 | 24.0 | 18565 | 1428 | 15.8 | 40636 | 2.19 | - | Passed |
| 31-40 | no gear | 766 | 15.9 | 28890 | 1198 | 17.2 | 31373 | 1.09 | - | Passed |
| 31-40 | no gear, 1 enhancement | 676 | 14.4 | 28140 | 1197 | 18.8 | 28654 | 1.02 | - | Passed |
| 31-40 | no gear, 2 enhancements | 615 | 15.2 | 24249 | 1196 | 18.9 | 28486 | 1.17 | - | Passed |
| 31-40 | no gear, 3 enhancements | 604 | 16.0 | 22690 | 1194 | 18.6 | 28886 | 1.27 | - | Passed |
| 31-40 | mid gear | 868 | 20.6 | 25284 | 1200 | 15.5 | 34935 | 1.38 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 782 | 18.8 | 24966 | 1200 | 16.7 | 32347 | 1.30 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 735 | 18.9 | 23343 | 1198 | 16.6 | 32430 | 1.39 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 695 | 18.3 | 22797 | 1200 | 16.6 | 32431 | 1.42 | - | Passed |
| 31-40 | best gear | 958 | 25.1 | 22894 | 1200 | 13.7 | 39554 | 1.73 | - | Passed |
| 31-40 | best gear, 1 enhancement | 881 | 25.0 | 21176 | 1200 | 14.5 | 37232 | 1.76 | - | Passed |
| 31-40 | best gear, 2 enhancements | 858 | 24.8 | 20757 | 1200 | 14.5 | 37301 | 1.80 | - | Passed |
| 31-40 | best gear, 3 enhancements | 823 | 23.5 | 20984 | 1200 | 14.6 | 36968 | 1.76 | - | Passed |
| 41-53 | no gear | 840 | 19.5 | 25889 | 1200 | 17.1 | 31621 | 1.22 | - | Passed |
| 41-53 | no gear, 1 enhancement | 646 | 15.8 | 24524 | 1248 | 18.3 | 30603 | 1.25 | - | Passed |
| 41-53 | no gear, 2 enhancements | 565 | 16.4 | 20663 | 1247 | 18.6 | 30111 | 1.46 | - | Passed |
| 41-53 | no gear, 3 enhancements | 480 | 16.9 | 17080 | 1245 | 18.8 | 29770 | 1.74 | - | Passed |
| 41-53 | mid gear | 994 | 25.4 | 23484 | 1200 | 15.2 | 35420 | 1.51 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 799 | 22.2 | 21612 | 1250 | 16.1 | 34872 | 1.61 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 699 | 19.6 | 21344 | 1250 | 16.5 | 34046 | 1.60 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 621 | 19.2 | 19433 | 1250 | 16.3 | 34565 | 1.78 | - | Passed |
| 41-53 | best gear | 1129 | 29.1 | 23311 | 1200 | 13.5 | 40024 | 1.72 | - | Passed |
| 41-53 | best gear, 1 enhancement | 974 | 27.3 | 21435 | 1250 | 14.2 | 39627 | 1.85 | - | Passed |
| 41-53 | best gear, 2 enhancements | 866 | 24.8 | 20934 | 1250 | 14.2 | 39733 | 1.90 | - | Passed |
| 41-53 | best gear, 3 enhancements | 794 | 23.2 | 20530 | 1250 | 14.3 | 39258 | 1.91 | - | Passed |

## Enhancement parity

### Rank 50, best gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 100%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 6 | 60-100 | 34% | 99% | 93% | 0.92 | 100% | 100.0% | 1.00 (+/- 0.00) | 1.09 | >= 1.00 | Passed |
| 41-45 | 2 | 6 | 60-100 | 68% | 99% | 84% | 1.66 | 100% | 100.0% | 2.00 (+/- 0.00) | 1.21 | >= 1.00 | Passed |
| 41-45 | 3 | 6 | 60-100 | 102% | 99% | 76% | 2.23 | 100% | 100.0% | 2.99 (+/- 0.00) | 1.34 | >= 1.00 | Passed |
| 46-50 | 1 | 6 | 60-100 | 43% | 99% | 81% | 0.80 | 100% | 100.0% | 1.00 (+/- 0.00) | 1.25 | >= 1.00 | Passed |
| 46-50 | 2 | 6 | 60-100 | 86% | 99% | 65% | 1.28 | 100% | 100.0% | 2.00 (+/- 0.00) | 1.56 | >= 1.00 | Passed |
| 46-50 | 3 | 6 | 60-100 | 128% | 99% | 56% | 1.65 | 100% | 100.0% | 2.98 (+/- 0.01) | 1.81 | >= 1.00 | Passed |
| 51-53 | 1 | 6 | 60 | 8% | 51% | 6% | 0.03 | 100% | 100.0% | 1.00 (+/- 0.00) | 35.46 | >= 1.00 | Passed |
| 51-53 | 2 | 6 | 60 | 16% | 40% | 5% | 0.04 | 100% | 100.0% | 2.00 (+/- 0.00) | 55.87 | >= 1.00 | Passed |
| 51-53 | 3 | 6 | 60 | 24% | 9% | 5% | 0.01 | 100% | 100.0% | 2.98 (+/- 0.01) | 260.96 | >= 1.00 | Passed |

### Thresholds, first / second / third slot, that would meet parity for the quality measured (the strictest set that keeps every case at least at today's, never falling from one slot to the next)

| Scope | Thresholds that meet parity | In steps of 5 | Thresholds in force |
| --- | --- | --- | --- |
| Every band together (the shared thresholds) | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 100% |
| Band 41-45 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 100% |
| Band 46-50 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 100% |
| Band 51-53 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 100% |

### Rank 50, best gear: quality of the items the new minigame made, in whole percent, by band and slots filled

| Band | Slots filled | Items made | 10th percentile | 25th percentile | Median | 75th percentile | 90th percentile | Made at 100% |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 99% |
| 41-45 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 99% |
| 41-45 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 99% |
| 46-50 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 46-50 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 46-50 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 51-53 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 51-53 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 51-53 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |

### Representative enhancements: for each recipe kind the real item at the best level the recipe accepts (the old window refuses one more than 5 levels above the recipe) whose penalty is the lower median of that level

| Recipe kind | Enhancement | Level | Progress penalty | Items of that kind and level (min / median / max penalty) | Recipes |
| --- | --- | --- | --- | --- | --- |
| Armor | Armor Enhancement - Force Attack V (`aen_frcatk5`) | 45 (tier V) | 60 | 50 items: 50 / 60.0 / 150 | 4 |
| Starship | Starship Enhancement - Shield Recharge Rate V (`sen_shrech5`) | 45 (tier V) | 60 | 24 items: 50 / 65.0 / 80 | 1 |
| Module | Module Enhancement - Module Bonus V (`men_mod5`) | 45 (tier V) | 60 | 2 items: 60 / 70.0 / 80 | 9 |
| Droid | Droid Enhancement - Resistance - Ice II (`de_res_ice2`) | 50 (tier VI) | 100 | 8 items: 100 / 100.0 / 100 | 4 |

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
