# Crafting revamp: balance gates of the Fabricator (Fine Tolerance Builder) minigame

- Command: `SWLOR.CraftSim gates --scenario fabricator-fine-tolerance --runs 100 --seed 1 --recipes-per-band 6 --out design/testing/crafting-revamp/fabricator-fine-tolerance-gates.md
- Seed 1; 100 runs per case.

## Inputs

- Scenario: Fabricator (Fine Tolerance Builder) (`fabricator-fine-tolerance`), Fabrication recipes, minigame Fabricator.
- Quality: the quality meter's share of its maximum when the item is made, in points (0-100). A broken or abandoned piece counts 0, so every average includes the crafts that failed.
- Crafter: the rank the scenario's cohort gives each recipe (by default the recipe's level, at most 50, so a crafter works at their own level), with no food or status bonuses and no perks unless a gate names a style. Gear levels are the plan's none, middle and best tier of the skill's crafting gear:
  - no gear (Control 0, Craftsmanship 0, CP 0)
  - mid gear (Control 13, Craftsmanship 12, CP 26)
  - best gear (Control 29, Craftsmanship 30, CP 37)
- Recipes: 1-10: 6 of 66; 11-20: 6 of 84; 21-30: 6 of 108; 31-40: 6 of 102; 41-53: 6 of 96 recipes played.
- Adaptive player: depth 3, forecast samples per result 2, budget 4000 engine copies per decision, 0.5% of value lost per extra action, heuristic FabricatorLeafHeuristic. Each attempt's dice come from its own stream of the seed.
- Fixed-rotation solver: beam 8, cycles up to 3, 8 adaptive lines as seeds, up to 40 actions, 64 training attempts per local-search round and 150 selection attempts for each of 12 finalists, local search from the best 3 candidate(s) for up to 3 round(s) each. Gate 1 searches one sequence for each band and gear tier across the band's sampled recipes (every recipe weighing the same; an action a recipe's engine cannot take is skipped there), and also each recipe's own best sequence as a reported metric. The winner is chosen on separate attempts and then scored on a third set (hold-out) with 100 attempts per recipe.
- Legacy comparator (gates 5 and 6): the old minigame's lookahead player (`SWLOR.CraftSim baseline`), replayed here with the same seed on the same recipes, ranks and gear.
- Time: 8.0 s per action in the new window and 6.0 s in the old one, plus 0.0 s per craft in both. These are estimates; the client evidence replaces them.
- XP multiplier on the base XP of every craft, failures included, as the game server applies it (`CraftMinigameXp`): 1-10 2.40x, 11-20 1.45x, 21-30 1.25x, 31-40 1.10x, 41-53 1.00x. The new side's XP below includes it; the legacy loop's does not.
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
- Enhancement thresholds in force (`CraftOutcomeRules`, read by gate 7): 50% / 85% / 85% for slots 1, 2 and 3. The scenario applies enhancements to its engine's progress meter.
- Styles: none offered, so the style-balance gate is not applicable.

## Verdicts

| Gate | Verdict | Summary |
| --- | --- | --- |
| No fixed rotation | Failed | One fixed rotation comes within 10 points of adaptive play in 15 case(s): 1-10 / no gear; 1-10 / mid gear; 1-10 / best gear; 11-20 / no gear; 11-20 / mid gear; 11-20 / best gear; 21-30 / no gear; 21-30 / mid gear; 21-30 / best gear; 31-40 / no gear; 31-40 / mid gear; 31-40 / best gear; 41-53 / no gear; 41-53 / mid gear; 41-53 / best gear (smallest margin -5.0 in 1-10 / no gear). The best rotation for each recipe alone, a reported metric, is -5.0 to 0.3 points behind adaptive play. |
| Style balance | NotApplicable | The scenario offers no style configurations yet, so there is nothing to compare. |
| Perks optional | Passed | An unperked rank-50 crafter in best gear reaches maximum quality on at least 50% of crafts in every at-level band (lowest 100%). |
| Length | Passed | Every band and gear tier takes 10-20 actions (item craft at level) on average. |
| Economy parity | Passed | Component stacks per item are within 10% of the legacy baseline where its success rate is at least 90%, and at most 10% above it where it is lower, at no less quality, in every band and gear tier. |
| XP rate | Passed | Item crafts: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.00x in 11-20 / no gear). |
| Enhancement parity | Passed | At rank 50 in best gear, the thresholds in force (50% / 85% / 85%) transfer at least 1.00 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 1.07 times today's). Thresholds that meet parity in every band for this quality: 100% / 100% / 100%. |

## No fixed rotation

### Average quality: adaptive play against the best single fixed rotation of each band and gear tier (quality points; a broken piece counts 0)

| Band | Gear | Recipes played | Adaptive avg quality | Best single rotation avg quality | Margin | Required | Best rotation per recipe avg quality (reported) | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 81.7 | 86.7 | -5.0 | >= 10 | 86.7 | Failed |
| 1-10 | mid gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Failed |
| 1-10 | best gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Failed |
| 11-20 | no gear | 6 | 99.8 | 100.0 | -0.2 | >= 10 | 100.0 | Failed |
| 11-20 | mid gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Failed |
| 11-20 | best gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Failed |
| 21-30 | no gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Failed |
| 21-30 | mid gear | 6 | 99.9 | 100.0 | -0.1 | >= 10 | 100.0 | Failed |
| 21-30 | best gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Failed |
| 31-40 | no gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Failed |
| 31-40 | mid gear | 6 | 99.9 | 100.0 | -0.1 | >= 10 | 100.0 | Failed |
| 31-40 | best gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 99.7 | Failed |
| 41-53 | no gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Failed |
| 41-53 | mid gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Failed |
| 41-53 | best gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Failed |

### The single rotation found for each band and gear tier, searched across the band's sampled recipes (average quality in points)

| Band | Gear | Best single rotation for the band | Training | Selection | Hold-out | Adaptive on these recipes |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | run_fittings > run_shell x2 > precision_pass > run_frame > precision_pass > run_frame x2 > precision_pass > run_shell > run_frame > run_shell > precision_pass > retool_extruder > run_frame > precision_pass > retool_extruder > precision_pass > run_fittings > precision_pass > retool_bonder > precision_pass x2 > run_shell x2 > precision_pass > retool_cutter > precision_pass x2 > run_frame > run_shell > run_fittings > retool_cutter > precision_pass > assemble | 86.7 | 86.7 | 86.7 | 81.7 |
| 1-10 | mid gear | run_frame x2 > run_shell > retool_cutter > precision_pass > retool_extruder > precision_pass > run_fittings > retool_bonder > run_shell > precision_pass x2 > run_frame > run_fittings > run_shell > retool_cutter > precision_pass > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | run_shell > retool_cutter > run_frame > precision_pass > retool_extruder > run_shell > precision_pass x2 > retool_cutter > run_frame > run_fittings > precision_pass > retool_bonder > run_fittings > run_shell > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 11-20 | no gear | retool_extruder > run_frame > run_fittings > run_shell > precision_pass > run_shell > run_frame x2 > run_shell x2 > precision_pass x2 > run_frame > retool_cutter > precision_pass > run_fittings > precision_pass > run_fittings > precision_pass > retool_bonder > precision_pass x2 > run_frame > run_shell > run_frame > run_fittings > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 99.8 |
| 11-20 | mid gear | run_shell > run_frame > precision_pass x2 > retool_bonder > run_fittings > precision_pass x2 > retool_cutter > precision_pass x2 > retool_extruder > precision_pass > run_shell > precision_pass > run_frame > run_fittings > run_frame > run_shell > run_fittings > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | run_fittings > run_frame > retool_extruder > run_fittings > run_shell x2 > precision_pass > retool_cutter > run_frame > precision_pass x2 > run_fittings x2 > retool_bonder > precision_pass x2 > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | retool_bonder > run_frame x2 > dual_feed_second > run_fittings > precision_pass > dual_feed_first > dual_feed_second > run_frame > precision_pass x2 > run_frame > run_shell > run_fittings > run_shell x2 > run_fittings > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_shell > run_frame > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | retool_bonder > run_frame x2 > dual_feed_second > run_fittings > precision_pass > run_frame > precision_pass x2 > dual_feed_first > run_shell > run_fittings > run_shell > run_frame > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_shell > run_fittings > assemble | 100.0 | 100.0 | 100.0 | 99.9 |
| 21-30 | best gear | dual_feed_second > dual_feed_first > retool_cutter > run_frame > precision_pass > retool_extruder > run_shell > precision_pass x2 > retool_cutter > run_fittings > precision_pass > retool_bonder > run_fittings > run_frame x2 > run_shell > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | retool_extruder > run_shell > run_frame > service > precision_pass x2 > retool_cutter > precision_pass > dual_feed_second > precision_pass > retool_bonder > precision_pass x2 > dual_feed_first > run_fittings > run_frame > run_fittings > run_frame > retool_extruder > run_shell x2 > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | dual_feed_first > dual_feed_second > precision_pass > dual_feed_first > service > retool_cutter > precision_pass > dual_feed_second > run_fittings > run_frame > run_shell > service > run_shell > precision_pass > service > retool_bonder > precision_pass x2 > retool_extruder > precision_pass > run_frame > run_shell > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 99.9 |
| 31-40 | best gear | dual_feed_first > precision_pass > retool_cutter > precision_pass > retool_extruder > dual_feed_second > run_fittings > retool_bonder > precision_pass > retool_extruder > precision_pass > dual_feed_first > run_shell > run_frame > precision_pass > run_fittings > retool_cutter > precision_pass > dual_feed_first > retool_bonder > precision_pass > run_frame > dual_feed_second > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | precision_pass > dual_feed_first > service > precision_pass > dual_feed_second > dual_feed_first > run_fittings > precision_pass > dual_feed_second > run_frame > retool_cutter > precision_pass > retool_bonder > retool_cutter > dual_feed_first > run_shell > run_frame > run_shell > retool_extruder > precision_pass > retool_bonder > precision_pass x2 > retool_cutter > precision_pass > retool_extruder > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | dual_feed_second > precision_pass > dual_feed_first > retool_bonder > run_shell > precision_pass x2 > dual_feed_first > run_shell > run_fittings > run_frame > run_shell > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_frame > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | dual_feed_second > dual_feed_first > retool_bonder > precision_pass x2 > dual_feed_second > run_frame x2 > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_shell > assemble | 100.0 | 100.0 | 100.0 | 100.0 |

### Reported metric, not gated: the best fixed rotation for each recipe alone, and what the band's single rotation scores on the same recipe (average quality in points)

| Band | Gear | Recipe | Best rotation for this recipe alone (reported) | Its hold-out | The single rotation on this recipe | Adaptive on this recipe |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | structure_0077 (level 2) | run_shell x2 > precision_pass x3 > run_frame > run_fittings > run_frame > run_fittings > run_shell > assemble | 20.0 | 20.0 | 20.0 |
| 1-10 | no gear | structure_0119 (level 4) | run_shell > run_frame x2 > run_shell > run_frame > run_shell > precision_pass > retool_extruder > run_frame > precision_pass > retool_extruder > precision_pass > run_fittings > precision_pass > retool_bonder > precision_pass x2 > run_shell x2 > precision_pass > retool_cutter > precision_pass > run_frame > run_shell > run_fittings > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 97.3 |
| 1-10 | no gear | structure_0059 (level 6) | run_frame > precision_pass > run_shell > precision_pass > run_shell x2 > run_fittings x2 > precision_pass > retool_extruder > precision_pass x3 > retool_bonder > run_fittings > precision_pass > run_frame > run_shell x2 > precision_pass x2 > assemble | 100.0 | 100.0 | 87.2 |
| 1-10 | no gear | structure_0409 (level 7) | run_shell > precision_pass > run_frame > precision_pass > run_shell > run_fittings > precision_pass > retool_bonder > precision_pass x3 > run_frame > retool_cutter > precision_pass > run_frame > run_fittings > precision_pass x2 > assemble | 100.0 | 100.0 | 91.6 |
| 1-10 | no gear | structure_0019 (level 9) | run_frame x2 > precision_pass > run_shell > precision_pass > retool_extruder > precision_pass x2 > run_fittings > precision_pass > retool_bonder > precision_pass x3 > retool_cutter > run_frame > run_shell x2 > run_fittings > precision_pass > assemble | 100.0 | 100.0 | 97.2 |
| 1-10 | no gear | structure_0192 (level 10) | run_fittings > run_frame > precision_pass x3 > run_shell > run_frame > run_shell > run_fittings > retool_cutter > precision_pass > retool_extruder > precision_pass x3 > run_frame > retool_cutter > precision_pass x2 > assemble | 100.0 | 100.0 | 96.9 |
| 1-10 | mid gear | structure_0077 (level 2) | run_shell x2 > run_frame > run_fittings > run_frame > run_fittings > precision_pass x2 > retool_cutter > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | mid gear | structure_0119 (level 4) | run_frame > precision_pass > run_shell > precision_pass > retool_extruder > run_shell > run_fittings > run_frame > precision_pass x2 > retool_bonder > run_fittings > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | mid gear | structure_0059 (level 6) | run_frame > precision_pass x2 > run_shell > retool_extruder > run_shell > run_fittings > run_frame > run_fittings > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | mid gear | structure_0409 (level 7) | run_shell x2 > run_frame > run_fittings > run_frame > run_fittings > precision_pass x2 > retool_cutter > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | mid gear | structure_0019 (level 9) | run_frame > run_shell > precision_pass x2 > retool_extruder > run_shell > run_fittings > precision_pass x2 > retool_bonder > run_frame > run_fittings > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | mid gear | structure_0192 (level 10) | run_fittings > precision_pass x2 > retool_cutter > run_frame > precision_pass x2 > retool_extruder > run_shell > precision_pass x2 > run_fittings > run_frame > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | structure_0077 (level 2) | run_shell > precision_pass > run_fittings > precision_pass > retool_bonder > run_frame > precision_pass x2 > retool_cutter > precision_pass > run_frame > run_shell x2 > run_fittings > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | structure_0119 (level 4) | run_frame > run_shell > precision_pass x2 > retool_extruder > run_fittings > run_shell > run_frame > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | structure_0059 (level 6) | run_frame > run_shell > precision_pass x2 > retool_extruder > run_shell > run_fittings > run_frame > run_fittings > precision_pass > retool_bonder > precision_pass x2 > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | structure_0409 (level 7) | run_shell > precision_pass x2 > retool_cutter > run_frame > run_fittings > precision_pass x2 > retool_bonder > run_shell > precision_pass > run_fittings > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | structure_0019 (level 9) | run_frame > precision_pass > run_fittings > precision_pass > retool_bonder > run_shell > precision_pass x2 > retool_extruder > run_shell > run_frame > run_fittings > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | structure_0192 (level 10) | run_fittings > precision_pass x2 > retool_cutter > run_frame > precision_pass x2 > retool_extruder > run_shell > precision_pass x2 > run_fittings > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | no gear | structure_0341 (level 11) | run_frame x2 > precision_pass > run_frame > run_shell > precision_pass > retool_extruder > precision_pass x3 > retool_bonder > run_fittings > run_shell > precision_pass > run_shell > precision_pass > retool_cutter > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 99.4 |
| 11-20 | no gear | structure_0139 (level 13) | run_fittings > precision_pass x3 > run_frame x2 > run_shell > run_fittings > run_shell > retool_cutter > precision_pass x3 > retool_extruder > precision_pass x3 > run_frame > assemble | 100.0 | 100.0 | 99.6 |
| 11-20 | no gear | structure_0339 (level 14) | run_shell > precision_pass > run_shell > precision_pass > run_frame > retool_cutter > precision_pass x2 > run_fittings > retool_bonder > precision_pass x3 > run_frame x2 > run_fittings > retool_extruder > precision_pass > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | no gear | structure_0018 (level 16) | run_shell > precision_pass > run_fittings > precision_pass > retool_bonder > precision_pass x2 > retool_cutter > run_frame > precision_pass x3 > retool_extruder > precision_pass > run_shell > run_frame > run_fittings > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | no gear | structure_0005 (level 18) | run_frame > precision_pass > run_shell > precision_pass > retool_extruder > precision_pass > retool_bonder > run_fittings > precision_pass x3 > retool_extruder > run_shell > run_frame > run_fittings > precision_pass x2 > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 99.9 |
| 11-20 | no gear | structure_0014 (level 20) | run_shell x2 > run_frame > precision_pass x2 > retool_cutter > run_frame > run_fittings > precision_pass x2 > retool_bonder > run_fittings > run_shell > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | structure_0341 (level 11) | run_frame x2 > run_shell x2 > run_fittings > run_shell > precision_pass x2 > retool_extruder > precision_pass x2 > retool_bonder > run_fittings > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | structure_0139 (level 13) | run_fittings > precision_pass > run_frame > precision_pass > run_frame > run_shell > run_fittings > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_shell > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | structure_0339 (level 14) | run_shell > precision_pass x2 > retool_cutter > run_frame > run_shell > run_fittings > precision_pass x2 > retool_bonder > run_shell > run_fittings > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | structure_0018 (level 16) | run_shell x2 > precision_pass x2 > retool_cutter > run_frame > precision_pass > run_frame > run_fittings > precision_pass > retool_bonder > precision_pass > run_fittings > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | structure_0005 (level 18) | run_frame > run_shell > precision_pass x2 > retool_extruder > run_fittings > precision_pass > retool_bonder > run_shell > run_fittings > run_frame > run_shell > precision_pass x2 > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | structure_0014 (level 20) | run_shell x2 > run_frame x2 > run_fittings > precision_pass x2 > retool_cutter > precision_pass x2 > retool_bonder > run_fittings > run_shell > run_frame > precision_pass x2 > assemble | 100.0 | 100.0 | 99.8 |
| 11-20 | best gear | structure_0341 (level 11) | run_frame > run_shell > precision_pass x2 > retool_extruder > run_fittings > precision_pass x2 > retool_bonder > run_shell > run_frame > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | structure_0139 (level 13) | run_fittings > precision_pass x2 > run_frame > run_shell > run_fittings > run_frame > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_shell > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | structure_0339 (level 14) | run_shell > precision_pass x2 > retool_cutter > run_frame > run_shell > run_fittings > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | structure_0018 (level 16) | run_shell > run_frame > precision_pass > retool_cutter > run_fittings > precision_pass x2 > retool_bonder > run_frame > run_shell > precision_pass x2 > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | structure_0005 (level 18) | run_frame > run_shell > precision_pass x2 > retool_extruder > precision_pass > retool_bonder > run_fittings > run_shell > precision_pass x2 > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | structure_0014 (level 20) | run_shell x2 > precision_pass > retool_cutter > run_frame > precision_pass x2 > run_fittings x2 > retool_bonder > precision_pass x2 > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | structure_0147 (level 22) | run_frame > precision_pass > retool_extruder > run_shell > precision_pass x2 > retool_cutter > run_frame > run_fittings > precision_pass > retool_bonder > run_shell x2 > run_fittings > run_frame > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | structure_0037 (level 25) | dual_feed_first > precision_pass x2 > dual_feed_second > dual_feed_first > dual_feed_second > run_frame > run_shell > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_shell > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | structure_0298 (level 26) | dual_feed_first > precision_pass x2 > dual_feed_first > dual_feed_second x2 > run_frame > run_shell > run_frame > run_shell > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | structure_0377 (level 27) | dual_feed_first > precision_pass > retool_cutter > precision_pass x2 > retool_bonder > dual_feed_second > precision_pass x2 > retool_extruder > dual_feed_first > run_shell > run_fittings > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | structure_0093 (level 29) | dual_feed_second > dual_feed_first > precision_pass x2 > retool_cutter > precision_pass > run_fittings > precision_pass > retool_bonder > precision_pass x2 > retool_extruder > dual_feed_first > run_frame > run_shell > dual_feed_second > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | structure_0356 (level 30) | dual_feed_first > precision_pass x2 > dual_feed_second x2 > run_frame x2 > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_shell > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | structure_0147 (level 22) | run_frame x2 > precision_pass > retool_extruder > run_shell > precision_pass x2 > run_shell > run_fittings x2 > retool_cutter > precision_pass > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 99.6 |
| 21-30 | mid gear | structure_0037 (level 25) | dual_feed_second > precision_pass x2 > dual_feed_first > dual_feed_second > run_frame x2 > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > assemble | 100.0 | 100.0 | 99.9 |
| 21-30 | mid gear | structure_0298 (level 26) | dual_feed_second > precision_pass x2 > dual_feed_second > dual_feed_first > run_frame x2 > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | structure_0377 (level 27) | dual_feed_second > dual_feed_first > precision_pass x2 > retool_cutter > precision_pass > run_fittings > precision_pass > retool_bonder > precision_pass > run_shell x2 > run_frame > run_shell > precision_pass > dual_feed_second > assemble | 100.0 | 100.0 | 99.9 |
| 21-30 | mid gear | structure_0093 (level 29) | run_shell > dual_feed_first > precision_pass x2 > retool_cutter > dual_feed_second > precision_pass > retool_bonder > dual_feed_second > precision_pass x2 > retool_cutter > run_shell > precision_pass > run_shell x2 > dual_feed_second > assemble | 100.0 | 100.0 | 99.9 |
| 21-30 | mid gear | structure_0356 (level 30) | dual_feed_first > precision_pass x2 > dual_feed_second x2 > run_frame x2 > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_shell > assemble | 100.0 | 100.0 | 99.9 |
| 21-30 | best gear | structure_0147 (level 22) | run_frame x2 > run_shell > precision_pass x2 > retool_extruder > run_shell > run_fittings > precision_pass x2 > retool_bonder > run_fittings > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | structure_0037 (level 25) | dual_feed_first > precision_pass > dual_feed_second > precision_pass > dual_feed_first > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_shell > run_frame x2 > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | structure_0298 (level 26) | dual_feed_second > precision_pass x2 > dual_feed_first x2 > run_shell > run_frame > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | structure_0377 (level 27) | dual_feed_first > precision_pass x2 > retool_cutter > run_shell > dual_feed_first > run_fittings > precision_pass x2 > retool_bonder > precision_pass x2 > dual_feed_second > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | structure_0093 (level 29) | run_shell > dual_feed_first > precision_pass x2 > retool_cutter > precision_pass > run_fittings > precision_pass > retool_bonder > run_frame > dual_feed_second > run_shell x2 > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | structure_0356 (level 30) | dual_feed_first > precision_pass x2 > dual_feed_second x2 > run_frame x2 > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_shell > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | structure_0379 (level 31) | dual_feed_first > precision_pass x2 > retool_cutter > precision_pass > retool_bonder > dual_feed_second > precision_pass x2 > retool_cutter > run_shell x3 > precision_pass > dual_feed_second > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | structure_0424 (level 32) | dual_feed_first > precision_pass x2 > retool_cutter > precision_pass > dual_feed_second > precision_pass > retool_bonder > precision_pass x2 > dual_feed_first > run_frame > retool_extruder > run_shell x2 > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | structure_0384 (level 34) | run_fittings x2 > dual_feed_second > dual_feed_first > precision_pass x2 > dual_feed_first x2 > precision_pass x2 > retool_cutter > precision_pass x2 > retool_bonder > dual_feed_first > precision_pass x2 > retool_extruder > dual_feed_first x2 > dual_feed_second > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | structure_0108 (level 36) | dual_feed_first x2 > precision_pass x2 > retool_extruder > precision_pass > dual_feed_second > precision_pass > retool_bonder > precision_pass x2 > dual_feed_first > retool_cutter > run_shell > dual_feed_first > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | structure_0325 (level 37) | dual_feed_first > precision_pass > retool_cutter > precision_pass x2 > retool_bonder > dual_feed_first > precision_pass x2 > dual_feed_second > retool_extruder > run_frame > run_shell > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | structure_0104 (level 40) | dual_feed_second > precision_pass > dual_feed_first > precision_pass > dual_feed_first > dual_feed_second > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_frame > run_shell > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | structure_0379 (level 31) | run_shell > dual_feed_first > precision_pass x2 > retool_cutter > precision_pass > dual_feed_second > retool_bonder > precision_pass x2 > dual_feed_second > retool_cutter > run_shell x2 > precision_pass > assemble | 100.0 | 100.0 | 99.9 |
| 31-40 | mid gear | structure_0424 (level 32) | dual_feed_first > precision_pass x2 > retool_cutter > precision_pass x2 > retool_bonder > dual_feed_second > precision_pass x2 > retool_extruder > precision_pass > run_frame > dual_feed_first > dual_feed_second > run_fittings > assemble | 100.0 | 100.0 | 99.9 |
| 31-40 | mid gear | structure_0384 (level 34) | dual_feed_first > dual_feed_second > dual_feed_first > precision_pass x2 > retool_cutter > precision_pass > run_fittings > precision_pass > retool_bonder > precision_pass > run_frame > precision_pass > dual_feed_second > run_frame > retool_cutter > dual_feed_first > assemble | 100.0 | 100.0 | 99.9 |
| 31-40 | mid gear | structure_0108 (level 36) | dual_feed_first x2 > precision_pass x2 > retool_extruder > precision_pass > dual_feed_second > precision_pass > retool_bonder > precision_pass x2 > dual_feed_first > retool_extruder > run_frame > run_shell > assemble | 100.0 | 100.0 | 99.8 |
| 31-40 | mid gear | structure_0325 (level 37) | run_shell > dual_feed_first > precision_pass > retool_cutter > precision_pass > dual_feed_second > precision_pass > retool_bonder > precision_pass x2 > retool_extruder > dual_feed_second > run_frame > run_shell > precision_pass > assemble | 100.0 | 100.0 | 99.9 |
| 31-40 | mid gear | structure_0104 (level 40) | dual_feed_second > precision_pass x2 > dual_feed_first x2 > run_shell > dual_feed_first > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | structure_0379 (level 31) | dual_feed_second > dual_feed_first > precision_pass x2 > dual_feed_second > retool_cutter > precision_pass x2 > retool_bonder > precision_pass > run_frame > run_shell > precision_pass > dual_feed_second > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | structure_0424 (level 32) | dual_feed_second > precision_pass x2 > dual_feed_first > dual_feed_second > retool_cutter > precision_pass x2 > retool_bonder > run_frame > precision_pass > run_frame > precision_pass > dual_feed_second > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | structure_0384 (level 34) | dual_feed_second > precision_pass > retool_cutter > dual_feed_first > precision_pass x2 > retool_bonder > precision_pass x2 > retool_extruder > run_frame x2 > precision_pass > dual_feed_second > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | structure_0108 (level 36) | precision_pass > dual_feed_second > precision_pass x2 > dual_feed_first > retool_bonder > precision_pass x2 > retool_extruder > precision_pass x2 > retool_bonder > run_shell > precision_pass > run_frame > precision_pass > dual_feed_second > assemble | 98.0 | 100.0 | 100.0 |
| 31-40 | best gear | structure_0325 (level 37) | dual_feed_first > precision_pass x2 > retool_cutter > precision_pass x2 > run_fittings > retool_bonder > dual_feed_second > run_frame > run_shell > run_frame > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | structure_0104 (level 40) | dual_feed_first > precision_pass x2 > dual_feed_second > dual_feed_first > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_shell > assemble | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | structure_0048 (level 42) | dual_feed_first > precision_pass x2 > retool_cutter > precision_pass > retool_bonder > dual_feed_second > precision_pass x2 > retool_cutter > run_shell x3 > precision_pass > dual_feed_second > assemble | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | structure_0317 (level 44) | dual_feed_first > precision_pass > dual_feed_second > precision_pass > dual_feed_first > run_frame > run_shell > run_frame > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_shell > assemble | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | structure_0103 (level 46) | dual_feed_second x2 > dual_feed_first x2 > precision_pass x2 > retool_cutter > precision_pass > run_fittings > precision_pass > retool_bonder > precision_pass x2 > run_frame > dual_feed_second x2 > assemble | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | structure_5015 (level 47) | dual_feed_first > dual_feed_second > precision_pass x2 > dual_feed_first > dual_feed_second > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_shell > assemble | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | structure_0324 (level 49) | dual_feed_first > dual_feed_second x2 > precision_pass x2 > retool_cutter > precision_pass x2 > retool_bonder > run_frame > run_shell > run_frame > run_shell > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | structure_5003 (level 50) | dual_feed_first > precision_pass > dual_feed_second > precision_pass > dual_feed_first > dual_feed_second > run_frame > run_shell > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_shell > assemble | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | structure_0048 (level 42) | dual_feed_second > precision_pass > dual_feed_first x2 > precision_pass x2 > retool_cutter > precision_pass > retool_bonder > dual_feed_second > run_fittings > precision_pass x2 > dual_feed_second > retool_cutter > precision_pass > dual_feed_first > assemble | 100.0 | 100.0 | 99.9 |
| 41-53 | mid gear | structure_0317 (level 44) | dual_feed_first > precision_pass x2 > dual_feed_second x2 > run_frame x3 > run_shell > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > assemble | 100.0 | 100.0 | 99.9 |
| 41-53 | mid gear | structure_0103 (level 46) | dual_feed_first > dual_feed_second > precision_pass x2 > run_fittings > retool_cutter > precision_pass x2 > retool_bonder > run_frame > run_shell x2 > run_frame > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | structure_5015 (level 47) | dual_feed_second > precision_pass x2 > dual_feed_second > dual_feed_first x2 > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_frame > assemble | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | structure_0324 (level 49) | dual_feed_first > run_shell x2 > precision_pass > dual_feed_first > precision_pass > retool_cutter > precision_pass x2 > dual_feed_second > retool_bonder > precision_pass > retool_bonder > precision_pass > dual_feed_first > assemble | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | structure_5003 (level 50) | dual_feed_second > precision_pass x2 > dual_feed_first > dual_feed_second > run_frame x2 > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | structure_0048 (level 42) | dual_feed_second > dual_feed_first > run_shell > service > precision_pass x2 > retool_bonder > precision_pass > service > dual_feed_first > retool_cutter > precision_pass > retool_cutter > precision_pass > run_shell > dual_feed_first > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | structure_0317 (level 44) | dual_feed_first > precision_pass x2 > dual_feed_first > run_shell x2 > retool_cutter > precision_pass x2 > dual_feed_second > retool_extruder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | structure_0103 (level 46) | dual_feed_second x2 > precision_pass x2 > retool_cutter > dual_feed_first > retool_cutter > precision_pass x2 > retool_bonder > precision_pass x2 > run_frame > run_shell > assemble | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | structure_5015 (level 47) | dual_feed_second > precision_pass > dual_feed_first > precision_pass > dual_feed_second > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_frame > assemble | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | structure_0324 (level 49) | dual_feed_second > precision_pass x2 > dual_feed_first > retool_cutter > precision_pass x2 > retool_bonder > precision_pass > run_frame > precision_pass > dual_feed_second > assemble | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | structure_5003 (level 50) | dual_feed_first > precision_pass x2 > run_shell > dual_feed_first > run_shell > dual_feed_first > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |

## Perks optional

### Rank 50, best gear, no perks: adaptive attempts that reach maximum quality, by recipe band

| Band | Recipes played | Attempts | Reach maximum quality | Make the item | Avg quality of items made | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 6 | 600 | 100% | 100% | 100% | >= 50% | Passed |
| 46-50 | 6 | 600 | 100% | 100% | 100% | >= 50% | Passed |
| 51-53 | 6 | 600 | 100% | 100% | 100% | >= 50% | Passed |

## Length

### Actions per attempt of the adaptive player at level (item craft at level)

| Band | Gear | Recipes played | Mean actions per attempt | Shortest recipe | Longest recipe | Stalled attempts | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 15.8 | 12.7 | 17.2 | 0 | 10-20 | Passed |
| 1-10 | mid gear | 6 | 15.4 | 15.0 | 15.8 | 0 | 10-20 | Passed |
| 1-10 | best gear | 6 | 15.1 | 14.5 | 15.3 | 0 | 10-20 | Passed |
| 11-20 | no gear | 6 | 18.5 | 17.0 | 19.2 | 0 | 10-20 | Passed |
| 11-20 | mid gear | 6 | 15.8 | 15.3 | 16.4 | 0 | 10-20 | Passed |
| 11-20 | best gear | 6 | 14.5 | 14.3 | 15.0 | 0 | 10-20 | Passed |
| 21-30 | no gear | 6 | 15.5 | 14.5 | 17.2 | 0 | 10-20 | Passed |
| 21-30 | mid gear | 6 | 14.5 | 13.7 | 15.3 | 0 | 10-20 | Passed |
| 21-30 | best gear | 6 | 13.7 | 13.3 | 14.9 | 0 | 10-20 | Passed |
| 31-40 | no gear | 6 | 14.9 | 14.5 | 15.3 | 0 | 10-20 | Passed |
| 31-40 | mid gear | 6 | 14.4 | 14.2 | 14.7 | 0 | 10-20 | Passed |
| 31-40 | best gear | 6 | 13.5 | 13.3 | 13.5 | 0 | 10-20 | Passed |
| 41-53 | no gear | 6 | 14.6 | 14.1 | 15.1 | 0 | 10-20 | Passed |
| 41-53 | mid gear | 6 | 13.8 | 13.4 | 14.6 | 0 | 10-20 | Passed |
| 41-53 | best gear | 6 | 13.1 | 12.8 | 13.5 | 0 | 10-20 | Passed |

## Economy parity

### Component stacks consumed per item made, failures included (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Parity rule | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 2.17 | 2.19 | 0.99 | 100% | 98% | 82% | 1% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | mid gear | 6 | 2.17 | 2.22 | 0.98 | 100% | 96% | 100% | 8% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | best gear | 6 | 2.17 | 2.19 | 0.99 | 100% | 98% | 100% | 18% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | no gear | 6 | 2.33 | 2.49 | 0.94 | 100% | 91% | 100% | 1% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | mid gear | 6 | 2.33 | 2.36 | 0.99 | 100% | 98% | 100% | 14% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | best gear | 6 | 2.33 | 2.34 | 1.00 | 100% | 99% | 100% | 30% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | no gear | 6 | 2.33 | 2.38 | 0.98 | 100% | 97% | 100% | 8% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | mid gear | 6 | 2.33 | 2.34 | 1.00 | 100% | 100% | 100% | 17% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | best gear | 6 | 2.33 | 2.35 | 0.99 | 100% | 99% | 100% | 30% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | no gear | 6 | 2.50 | 2.55 | 0.98 | 100% | 97% | 100% | 15% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | mid gear | 6 | 2.50 | 2.54 | 0.98 | 100% | 98% | 100% | 27% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | best gear | 6 | 2.50 | 2.51 | 0.99 | 100% | 99% | 100% | 38% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | no gear | 6 | 2.33 | 2.35 | 0.99 | 100% | 99% | 100% | 42% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | mid gear | 6 | 2.33 | 2.34 | 1.00 | 100% | 100% | 100% | 73% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | best gear | 6 | 2.33 | 2.34 | 1.00 | 100% | 100% | 100% | 91% | yes | 0% / 0% | within 10% | Passed |

## XP rate

### Item crafts: XP per hour against the legacy loop (6.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per craft | Legacy actions per craft | Legacy XP per hour | New XP per craft | New actions per craft | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 595 | 5.0 | 71616 | 2616 | 15.8 | 74356 | 1.04 | - | Passed |
| 1-10 | no gear, 1 enhancement | 422 | 6.1 | 41840 | 2618 | 19.8 | 59559 | 1.42 | - | Passed |
| 1-10 | no gear, 2 enhancements | 134 | 6.0 | 13420 | 2624 | 23.8 | 49519 | 3.69 | - | Passed |
| 1-10 | no gear, 3 enhancements | 90 | 6.0 | 9000 | 2619 | 27.6 | 42630 | 4.74 | - | Passed |
| 1-10 | mid gear | 629 | 6.6 | 57334 | 2880 | 15.4 | 84156 | 1.47 | - | Passed |
| 1-10 | mid gear, 1 enhancement | 614 | 7.4 | 49914 | 2880 | 18.0 | 71913 | 1.44 | - | Passed |
| 1-10 | mid gear, 2 enhancements | 575 | 8.0 | 43258 | 2880 | 20.3 | 63780 | 1.47 | - | Passed |
| 1-10 | mid gear, 3 enhancements | 501 | 8.6 | 34903 | 2880 | 23.0 | 56385 | 1.62 | - | Passed |
| 1-10 | best gear | 699 | 6.8 | 62111 | 2880 | 15.1 | 85866 | 1.38 | - | Passed |
| 1-10 | best gear, 1 enhancement | 694 | 7.4 | 56259 | 2880 | 16.9 | 76694 | 1.36 | - | Passed |
| 1-10 | best gear, 2 enhancements | 665 | 8.0 | 50077 | 2880 | 19.1 | 67676 | 1.35 | - | Passed |
| 1-10 | best gear, 3 enhancements | 646 | 8.2 | 47369 | 2880 | 21.5 | 60326 | 1.27 | - | Passed |
| 11-20 | no gear | 562 | 8.0 | 42142 | 1738 | 18.5 | 42329 | 1.00 | - | Passed |
| 11-20 | no gear, 1 enhancement | 174 | 6.7 | 15665 | 1737 | 21.4 | 36463 | 2.33 | - | Passed |
| 11-20 | no gear, 2 enhancements | 171 | 7.1 | 14541 | 1739 | 25.0 | 31317 | 2.15 | - | Passed |
| 11-20 | no gear, 3 enhancements | 170 | 7.5 | 13664 | 1739 | 28.1 | 27863 | 2.04 | - | Passed |
| 11-20 | mid gear | 675 | 12.1 | 33472 | 1740 | 15.8 | 49673 | 1.48 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 643 | 12.8 | 30058 | 1740 | 18.0 | 43568 | 1.45 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 608 | 13.9 | 26253 | 1740 | 20.7 | 37743 | 1.44 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 547 | 15.5 | 21187 | 1740 | 23.9 | 32823 | 1.55 | - | Passed |
| 11-20 | best gear | 779 | 14.4 | 32454 | 1740 | 14.5 | 53944 | 1.66 | - | Passed |
| 11-20 | best gear, 1 enhancement | 749 | 14.9 | 30162 | 1740 | 17.1 | 45656 | 1.51 | - | Passed |
| 11-20 | best gear, 2 enhancements | 743 | 16.2 | 27482 | 1740 | 19.5 | 40181 | 1.46 | - | Passed |
| 11-20 | best gear, 3 enhancements | 703 | 15.9 | 26489 | 1740 | 22.0 | 35534 | 1.34 | - | Passed |
| 21-30 | no gear | 635 | 11.0 | 34628 | 1500 | 15.5 | 43638 | 1.26 | - | Passed |
| 21-30 | no gear, 1 enhancement | 612 | 11.7 | 31429 | 1500 | 18.5 | 36397 | 1.16 | - | Passed |
| 21-30 | no gear, 2 enhancements | 584 | 13.0 | 26972 | 1500 | 22.0 | 30728 | 1.14 | - | Passed |
| 21-30 | no gear, 3 enhancements | 559 | 14.8 | 22666 | 1500 | 25.6 | 26345 | 1.16 | - | Passed |
| 21-30 | mid gear | 700 | 17.9 | 23471 | 1499 | 14.5 | 46629 | 1.99 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 678 | 17.5 | 23268 | 1500 | 17.8 | 37930 | 1.63 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 661 | 18.0 | 22054 | 1500 | 20.6 | 32725 | 1.48 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 630 | 18.0 | 20982 | 1500 | 23.5 | 28752 | 1.37 | - | Passed |
| 21-30 | best gear | 776 | 22.4 | 20795 | 1500 | 13.7 | 49103 | 2.36 | - | Passed |
| 21-30 | best gear, 1 enhancement | 757 | 21.5 | 21107 | 1500 | 16.3 | 41398 | 1.96 | - | Passed |
| 21-30 | best gear, 2 enhancements | 742 | 22.4 | 19894 | 1500 | 18.9 | 35730 | 1.80 | - | Passed |
| 21-30 | best gear, 3 enhancements | 716 | 21.7 | 19836 | 1500 | 21.3 | 31717 | 1.60 | - | Passed |
| 31-40 | no gear | 677 | 13.6 | 29783 | 1320 | 14.9 | 39978 | 1.34 | - | Passed |
| 31-40 | no gear, 1 enhancement | 645 | 14.9 | 26066 | 1320 | 16.8 | 35442 | 1.36 | - | Passed |
| 31-40 | no gear, 2 enhancements | 616 | 14.5 | 25455 | 1320 | 18.6 | 31873 | 1.25 | - | Passed |
| 31-40 | no gear, 3 enhancements | 596 | 15.5 | 22998 | 1320 | 20.6 | 28787 | 1.25 | - | Passed |
| 31-40 | mid gear | 748 | 19.8 | 22630 | 1319 | 14.4 | 41202 | 1.82 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 736 | 20.5 | 21573 | 1319 | 16.1 | 36971 | 1.71 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 719 | 20.5 | 21086 | 1320 | 17.8 | 33401 | 1.58 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 684 | 19.6 | 20927 | 1320 | 19.3 | 30739 | 1.47 | - | Passed |
| 31-40 | best gear | 822 | 24.7 | 19971 | 1320 | 13.5 | 44120 | 2.21 | - | Passed |
| 31-40 | best gear, 1 enhancement | 817 | 25.3 | 19347 | 1320 | 14.7 | 40303 | 2.08 | - | Passed |
| 31-40 | best gear, 2 enhancements | 802 | 25.7 | 18713 | 1320 | 16.2 | 36576 | 1.95 | - | Passed |
| 31-40 | best gear, 3 enhancements | 774 | 24.3 | 19081 | 1320 | 17.8 | 33418 | 1.75 | - | Passed |
| 41-53 | no gear | 842 | 19.1 | 26466 | 1200 | 14.6 | 37054 | 1.40 | - | Passed |
| 41-53 | no gear, 1 enhancement | 770 | 17.7 | 26166 | 1200 | 16.3 | 33085 | 1.26 | - | Passed |
| 41-53 | no gear, 2 enhancements | 705 | 17.7 | 23862 | 1200 | 18.0 | 30033 | 1.26 | - | Passed |
| 41-53 | no gear, 3 enhancements | 613 | 17.9 | 20584 | 1200 | 19.5 | 27626 | 1.34 | - | Passed |
| 41-53 | mid gear | 1036 | 26.7 | 23298 | 1200 | 13.8 | 38997 | 1.67 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 974 | 25.3 | 23068 | 1200 | 15.7 | 34330 | 1.49 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 888 | 23.2 | 22982 | 1200 | 17.2 | 31352 | 1.36 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 808 | 22.3 | 21722 | 1200 | 18.7 | 28879 | 1.33 | - | Passed |
| 41-53 | best gear | 1142 | 28.9 | 23727 | 1200 | 13.1 | 41117 | 1.73 | - | Passed |
| 41-53 | best gear, 1 enhancement | 1111 | 28.7 | 23211 | 1200 | 14.8 | 36466 | 1.57 | - | Passed |
| 41-53 | best gear, 2 enhancements | 1034 | 27.0 | 22927 | 1200 | 16.1 | 33600 | 1.47 | - | Passed |
| 41-53 | best gear, 3 enhancements | 985 | 26.2 | 22581 | 1200 | 17.4 | 31103 | 1.38 | - | Passed |

## Enhancement parity

### Rank 50, best gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 85%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 6 | 60 | 32% | 99% | 95% | 0.94 | 100% | 100.0% | 1.00 (+/- 0.00) | 1.07 | >= 1.00 | Passed |
| 41-45 | 2 | 6 | 60 | 63% | 99% | 89% | 1.75 | 100% | 100.0% | 2.00 (+/- 0.00) | 1.14 | >= 1.00 | Passed |
| 41-45 | 3 | 6 | 60 | 95% | 99% | 83% | 2.45 | 100% | 100.0% | 3.00 (+/- 0.00) | 1.22 | >= 1.00 | Passed |
| 46-50 | 1 | 6 | 60 | 32% | 100% | 83% | 0.82 | 100% | 100.0% | 1.00 (+/- 0.00) | 1.22 | >= 1.00 | Passed |
| 46-50 | 2 | 6 | 60 | 64% | 99% | 73% | 1.45 | 100% | 100.0% | 2.00 (+/- 0.00) | 1.38 | >= 1.00 | Passed |
| 46-50 | 3 | 6 | 60 | 96% | 99% | 65% | 1.91 | 100% | 100.0% | 3.00 (+/- 0.00) | 1.57 | >= 1.00 | Passed |
| 51-53 | 1 | 6 | 60 | 10% | 68% | 14% | 0.09 | 100% | 100.0% | 1.00 (+/- 0.00) | 10.77 | >= 1.00 | Passed |
| 51-53 | 2 | 6 | 60 | 20% | 58% | 13% | 0.14 | 100% | 100.0% | 2.00 (+/- 0.00) | 14.02 | >= 1.00 | Passed |
| 51-53 | 3 | 6 | 60 | 30% | 34% | 13% | 0.12 | 100% | 100.0% | 3.00 (+/- 0.00) | 24.41 | >= 1.00 | Passed |

### Thresholds, first / second / third slot, that would meet parity for the quality measured (the strictest set that keeps every case at least at today's, never falling from one slot to the next)

| Scope | Thresholds that meet parity | In steps of 5 | Thresholds in force |
| --- | --- | --- | --- |
| Every band together (the shared thresholds) | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 85% |
| Band 41-45 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 85% |
| Band 46-50 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 85% |
| Band 51-53 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 85% |

### Rank 50, best gear: quality of the items the new minigame made, in whole percent, by band and slots filled

| Band | Slots filled | Items made | 10th percentile | 25th percentile | Median | 75th percentile | 90th percentile | Made at 100% |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 100% |
| 41-45 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 100% |
| 41-45 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 100% |
| 46-50 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 100% |
| 46-50 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 100% |
| 46-50 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 100% |
| 51-53 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 100% |
| 51-53 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 100% |
| 51-53 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 100% |

### Representative enhancements: for each recipe kind the real item at the best level the recipe accepts (the old window refuses one more than 5 levels above the recipe) whose penalty is the lower median of that level

| Recipe kind | Enhancement | Level | Progress penalty | Items of that kind and level (min / median / max penalty) | Recipes |
| --- | --- | --- | --- | --- | --- |
| Structure | Structure Enhancement - Structure Bonus V (`sten_sb5`) | 45 (tier V) | 60 | 2 items: 60 / 70.0 / 80 | 18 |

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
- Prime Feedstock is the only chance in the Fabricator, so the adaptive player's edge over a fixed rotation comes from it and from the recipe's state, not from luck at the actions themselves.
- A Builder is played with the numbers of the game's own perk definitions at the rank the recipe is played at. The unperked scenario plays no perks except in the style-balance gate, which compares the styles.
- Every crafter plays as a Fine Tolerance (full line) Builder: the style from rank 20 and every other purchase of the line the rank allows. The unperked rank-50 result for gate 3 is in the `fabricator` scenario.
