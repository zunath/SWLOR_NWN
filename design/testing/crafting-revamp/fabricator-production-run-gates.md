# Crafting revamp: balance gates of the Fabricator (Production Run Builder) minigame

- Command: `SWLOR.CraftSim gates --scenario fabricator-production-run --gates rotation,length,materials,xp,enhancements --runs 30 --seed 1 --recipes-per-band 6 --repo . --out design/testing/crafting-revamp/fabricator-production-run-gates.md`
- Seed 1; 30 runs per case.

## Inputs

- Scenario: Fabricator (Production Run Builder) (`fabricator-production-run`), Fabrication recipes, minigame Fabricator.
- Quality: the quality meter's share of its maximum when the item is made, in points (0-100). A broken or abandoned piece counts 0, so every average includes the crafts that failed.
- Crafter: the rank the scenario's cohort gives each recipe (by default the recipe's level, at most 50, so a crafter works at their own level), with no food or status bonuses and no perks unless a gate names a style or the scenario's limitations say it plays a perk loadout. Gear levels are the plan's none, middle and best tier of the skill's crafting gear:
  - no gear (Control 0, Craftsmanship 0, CP 0)
  - mid gear (Control 13, Craftsmanship 12, CP 26)
  - best gear (Control 29, Craftsmanship 30, CP 37)
  - fully modded gear (Control 119, Craftsmanship 120, CP 37)
- Recipes: 1-10: 6 of 66; 11-20: 6 of 84; 21-30: 6 of 109; 31-40: 6 of 102; 41-53: 6 of 96 recipes played.
- Adaptive player: depth 3, forecast samples per result 2, budget 4000 engine copies per decision, 0.5% of value lost per extra action, heuristic FabricatorLeafHeuristic. Each attempt's dice come from its own stream of the seed.
- Fixed-rotation solver: beam 8, cycles up to 3, 8 adaptive lines as seeds, up to 40 actions, 64 training attempts per local-search round and 150 selection attempts for each of 12 finalists, local search from the best 3 candidate(s) for up to 3 round(s) each. Gate 1 searches one sequence for each band and gear tier across the band's sampled recipes (every recipe weighing the same; an action a recipe's engine cannot take is skipped there), and also each recipe's own best sequence as a reported metric. The winner is chosen on separate attempts and then scored on a third set (hold-out) with 30 attempts per recipe.
- Legacy comparator (gates 5 and 6): the old minigame's lookahead player (`SWLOR.CraftSim baseline`), replayed here with the same seed on the same recipes, ranks and gear.
- Time: 8.0 s per action in the new window and 6.0 s in the old one, plus 0.0 s per craft in both. These are estimates; the client evidence replaces them.
- XP multiplier on the base XP of every craft, failures included, as the game server applies it (`CraftMinigameXp`): 1-10 2.40x, 11-20 1.50x, 21-30 1.25x, 31-40 1.10x, 41-53 1.06x. The new side's XP below includes it; the legacy loop's does not.
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
- Styles: none offered, so the style-balance gate is not applicable.
- Gates run: No fixed rotation, Length, Economy parity, XP rate, Enhancement parity. The others were not run, so this report cannot show that the minigame passes.

## Verdicts

| Gate | Verdict | Summary |
| --- | --- | --- |
| No fixed rotation | Failed | One fixed rotation comes within 10 points of adaptive play in 2 case(s): 1-10 / no gear; 41-53 / no gear (smallest margin -5.7 in 1-10 / no gear). The best rotation for each recipe alone, a reported metric, is -8.7 to 0.0 points behind adaptive play. |
| Length | Passed | Every band and gear tier takes 10-20 actions (item craft at level) on average. |
| Economy parity | Passed | Component stacks per item are within 10% of the legacy baseline where its success rate is at least 90%, and at most 10% above it where it is lower, at no less quality, in every band and gear tier. |
| XP rate | Passed | Item crafts: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.02x in 1-10 / no gear). |
| Enhancement parity | Passed | At rank 50 in fully modded gear, the thresholds in force (50% / 85% / 100%) transfer at least 0.90 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 1.02 times today's). Thresholds that meet parity in every band for this quality: 100% / 100% / 100%. |

## No fixed rotation

### Average quality: adaptive play against the best single fixed rotation of each band and gear tier (quality points; a broken piece counts 0)

| Band | Gear | Recipes played | Adaptive avg quality | Best single rotation avg quality | Margin | Required | Best rotation per recipe avg quality (reported) | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 80.9 | 86.7 | -5.7 | >= 10 | 86.7 | Failed |
| 1-10 | mid gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Reported |
| 1-10 | best gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Reported |
| 1-10 | fully modded gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Reported |
| 11-20 | no gear | 6 | 99.8 | 100.0 | -0.2 | >= 10 | 100.0 | Reported |
| 11-20 | mid gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Reported |
| 11-20 | best gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Reported |
| 11-20 | fully modded gear | 6 | 99.8 | 100.0 | -0.2 | >= 10 | 100.0 | Reported |
| 21-30 | no gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Reported |
| 21-30 | mid gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Reported |
| 21-30 | best gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Reported |
| 21-30 | fully modded gear | 6 | 99.0 | 100.0 | -1.0 | >= 10 | 100.0 | Reported |
| 31-40 | no gear | 6 | 99.7 | 100.0 | -0.3 | >= 10 | 100.0 | Reported |
| 31-40 | mid gear | 6 | 98.9 | 100.0 | -1.1 | >= 10 | 100.0 | Reported |
| 31-40 | best gear | 6 | 99.1 | 100.0 | -0.9 | >= 10 | 100.0 | Reported |
| 31-40 | fully modded gear | 6 | 99.9 | 100.0 | -0.1 | >= 10 | 100.0 | Reported |
| 41-53 | no gear | 6 | 88.7 | 93.2 | -4.5 | >= 10 | 97.4 | Failed |
| 41-53 | mid gear | 6 | 98.1 | 100.0 | -1.9 | >= 10 | 100.0 | Reported |
| 41-53 | best gear | 6 | 99.1 | 100.0 | -0.9 | >= 10 | 100.0 | Reported |
| 41-53 | fully modded gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Reported |

### The single rotation found for each band and gear tier, searched across the band's sampled recipes (average quality in points)

| Band | Gear | Best single rotation for the band | Training | Selection | Hold-out | Adaptive on these recipes |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | run_fittings > run_shell x2 > run_frame > precision_pass x2 > run_frame > run_shell > run_frame > run_shell > precision_pass > retool_extruder > run_frame > precision_pass > retool_extruder > precision_pass > run_frame > precision_pass > retool_bonder > precision_pass x2 > run_shell x2 > precision_pass > retool_cutter > precision_pass x2 > run_frame > run_shell > run_fittings > retool_cutter > precision_pass > assemble | 86.7 | 86.7 | 86.7 | 80.9 |
| 1-10 | mid gear | run_frame > run_fittings > service > run_frame > run_shell > retool_cutter > precision_pass > retool_extruder > run_shell > run_fittings > run_frame > run_fittings > precision_pass x2 > retool_bonder > precision_pass x2 > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | run_shell > retool_cutter > run_frame > precision_pass > retool_extruder > run_shell > precision_pass x2 > retool_cutter > run_frame > run_fittings > precision_pass > retool_bonder > run_fittings > run_shell > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | retool_cutter > run_fittings > run_frame > run_shell > precision_pass > run_fittings > run_shell > retool_extruder > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 11-20 | no gear | run_frame > run_fittings > run_frame x2 > precision_pass > run_fittings x2 > precision_pass > run_frame > precision_pass > run_shell > retool_cutter > precision_pass x2 > retool_extruder > run_shell > precision_pass > retool_cutter > precision_pass > retool_extruder > precision_pass x2 > run_fittings > retool_bonder > precision_pass x3 > run_shell x2 > run_fittings > retool_cutter > precision_pass > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 99.8 |
| 11-20 | mid gear | run_shell > run_frame > precision_pass x2 > retool_bonder > run_fittings > precision_pass x2 > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_frame > run_shell > run_fittings > run_frame > run_fittings > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | run_shell > run_frame > precision_pass x2 > retool_bonder > run_fittings > precision_pass x2 > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_shell > run_frame > run_fittings > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | retool_extruder > run_fittings > run_frame > run_shell > precision_pass > service > precision_pass > run_fittings > run_frame > retool_cutter > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 | 99.8 |
| 21-30 | no gear | retool_bonder > run_shell > run_frame x3 > dual_feed_second > run_fittings > precision_pass x3 > retool_extruder > precision_pass > retool_cutter > run_shell > precision_pass > retool_extruder > precision_pass > retool_extruder > run_shell > precision_pass > retool_cutter > run_frame > precision_pass x2 > run_fittings > retool_extruder > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | dual_feed_first > retool_cutter > run_frame > run_shell > run_fittings > precision_pass > retool_extruder > run_shell > run_frame > precision_pass x2 > retool_bonder > precision_pass x2 > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | retool_bonder > run_frame > dual_feed_second > run_fittings > precision_pass x2 > retool_cutter > precision_pass x2 > retool_extruder > precision_pass > run_shell > precision_pass x2 > run_fittings > run_frame > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | retool_bonder > run_frame > run_fittings > run_shell > precision_pass > run_frame > dual_feed_second > precision_pass > retool_cutter > precision_pass x2 > retool_extruder > precision_pass > run_shell > precision_pass > run_fittings > assemble | 100.0 | 100.0 | 100.0 | 99.0 |
| 31-40 | no gear | dual_feed_first > service > precision_pass > retool_extruder > run_shell > precision_pass > service > precision_pass x2 > retool_cutter > dual_feed_second > run_fittings > precision_pass x2 > dual_feed_second > retool_bonder > precision_pass x3 > run_shell x2 > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 99.7 |
| 31-40 | mid gear | retool_extruder > dual_feed_first > run_fittings x2 > service > precision_pass x2 > retool_cutter > dual_feed_second > precision_pass > service > dual_feed_first > run_shell > run_frame > run_fittings > precision_pass > retool_bonder > precision_pass x3 > retool_cutter > precision_pass > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 98.9 |
| 31-40 | best gear | dual_feed_first > precision_pass > retool_extruder > precision_pass > dual_feed_first > precision_pass > service > precision_pass > retool_cutter > precision_pass > retool_cutter > dual_feed_first > precision_pass x2 > run_fittings > retool_bonder > precision_pass x3 > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 99.1 |
| 31-40 | fully modded gear | retool_extruder > run_fittings > service > dual_feed_first > run_shell > precision_pass x2 > run_fittings > retool_cutter > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 | 99.9 |
| 41-53 | no gear | dual_feed_second x2 > dual_feed_first > retool_extruder > precision_pass > run_frame x2 > retool_extruder > dual_feed_second > dual_feed_first > dual_feed_second > dual_feed_first > service > retool_extruder > run_frame x2 > precision_pass > run_shell > precision_pass > retool_cutter > run_frame > precision_pass > run_shell > service > precision_pass x2 > run_frame > retool_bonder > precision_pass x2 > run_frame > run_shell > precision_pass > assemble | 93.6 | 93.1 | 93.2 | 88.7 |
| 41-53 | mid gear | retool_extruder > dual_feed_first x2 > service > precision_pass > run_shell > precision_pass > run_shell > precision_pass x2 > run_fittings > precision_pass > retool_cutter > service > precision_pass > run_frame > precision_pass x2 > run_shell > service > precision_pass > retool_extruder > retool_bonder > precision_pass x2 > run_fittings > service > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 | 98.1 |
| 41-53 | best gear | dual_feed_second > retool_bonder > run_fittings > precision_pass > run_shell > precision_pass > retool_extruder > precision_pass x2 > retool_bonder > service > precision_pass > retool_cutter > run_frame > precision_pass > service > run_shell > precision_pass x2 > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 99.1 |
| 41-53 | fully modded gear | retool_extruder > run_fittings > dual_feed_first > precision_pass x2 > retool_cutter > run_fittings > precision_pass > retool_extruder > retool_cutter > retool_extruder > retool_bonder > service > precision_pass x2 > run_shell > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 100.0 |

### Reported metric, not gated: the best fixed rotation for each recipe alone, and what the band's single rotation scores on the same recipe (average quality in points)

| Band | Gear | Recipe | Best rotation for this recipe alone (reported) | Its hold-out | The single rotation on this recipe | Adaptive on this recipe |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | structure_0077 (level 2) | run_shell x2 > precision_pass x3 > run_frame > run_fittings > run_frame > run_fittings > run_shell > assemble | 20.0 | 20.0 | 20.0 |
| 1-10 | no gear | structure_0119 (level 4) | run_shell > run_frame x2 > run_shell > run_frame > run_shell > precision_pass > retool_extruder > run_frame > precision_pass > retool_extruder > precision_pass > run_fittings > precision_pass > retool_bonder > precision_pass x2 > run_shell x2 > precision_pass > retool_cutter > precision_pass > run_frame > run_shell > run_fittings > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 97.3 |
| 1-10 | no gear | structure_0059 (level 6) | run_frame > precision_pass > run_shell > precision_pass > run_shell x2 > run_fittings x2 > precision_pass > retool_extruder > precision_pass x3 > retool_bonder > run_fittings > precision_pass > run_frame > run_shell x2 > precision_pass x2 > assemble | 100.0 | 100.0 | 82.8 |
| 1-10 | no gear | structure_0409 (level 7) | run_shell > precision_pass > run_fittings > run_frame > run_fittings > run_frame > precision_pass > run_shell > run_frame > precision_pass > retool_cutter > precision_pass x3 > retool_bonder > run_fittings > precision_pass > run_frame x2 > precision_pass x2 > assemble | 100.0 | 100.0 | 92.8 |
| 1-10 | no gear | structure_0019 (level 9) | run_frame x2 > precision_pass > run_shell > precision_pass > retool_extruder > precision_pass x2 > run_fittings > precision_pass > retool_bonder > precision_pass x3 > retool_cutter > run_frame > run_shell x2 > run_fittings > precision_pass > assemble | 100.0 | 100.0 | 96.2 |
| 1-10 | no gear | structure_0192 (level 10) | run_fittings > run_frame > precision_pass x3 > run_shell > run_frame > run_shell > run_fittings > retool_cutter > precision_pass > retool_extruder > precision_pass x3 > run_frame > retool_cutter > precision_pass x2 > assemble | 100.0 | 100.0 | 96.6 |
| 1-10 | mid gear | structure_0077 (level 2) | run_shell x2 > run_frame > run_fittings > run_frame > run_fittings > precision_pass x2 > retool_cutter > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | mid gear | structure_0119 (level 4) | run_frame > precision_pass > run_shell > precision_pass > retool_extruder > run_shell > run_fittings > run_frame > precision_pass x2 > retool_bonder > run_fittings > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | mid gear | structure_0059 (level 6) | run_frame > precision_pass x2 > run_shell > retool_extruder > run_shell > run_fittings > run_frame > run_fittings > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | mid gear | structure_0409 (level 7) | run_shell x2 > run_frame x2 > run_fittings > run_frame > precision_pass x2 > retool_cutter > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | mid gear | structure_0019 (level 9) | run_frame > run_shell > precision_pass x2 > retool_extruder > run_shell > run_fittings > precision_pass x2 > retool_bonder > run_frame > run_fittings > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | mid gear | structure_0192 (level 10) | run_fittings > precision_pass x2 > retool_cutter > run_frame > precision_pass x2 > retool_extruder > run_shell > precision_pass x2 > run_fittings > run_frame > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | structure_0077 (level 2) | run_shell > precision_pass > run_fittings > precision_pass > retool_bonder > run_frame > precision_pass x2 > retool_cutter > precision_pass > run_frame > run_shell x2 > run_fittings > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | structure_0119 (level 4) | run_frame > run_shell > precision_pass x2 > retool_extruder > run_fittings > run_shell > run_frame > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | structure_0059 (level 6) | run_frame > run_shell > precision_pass x2 > retool_extruder > run_shell > run_fittings > run_frame > run_fittings > precision_pass > retool_bonder > precision_pass x2 > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | structure_0409 (level 7) | run_shell > run_frame > run_shell > run_fittings > run_frame > precision_pass x2 > retool_cutter > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | structure_0019 (level 9) | run_frame > precision_pass > run_fittings > precision_pass > retool_bonder > run_shell > precision_pass x2 > retool_extruder > run_shell > run_frame > run_fittings > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | structure_0192 (level 10) | run_fittings > precision_pass x2 > retool_cutter > run_frame > precision_pass x2 > retool_extruder > run_shell > precision_pass x2 > run_fittings > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | structure_0077 (level 2) | run_frame > run_shell > precision_pass > run_fittings > retool_cutter > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | structure_0119 (level 4) | run_frame > run_shell > precision_pass > run_fittings > run_shell > retool_extruder > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | structure_0059 (level 6) | run_frame > run_shell > precision_pass > run_fittings > run_shell > retool_extruder > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | structure_0409 (level 7) | run_frame > run_shell > precision_pass > run_fittings > run_frame > retool_cutter > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | structure_0019 (level 9) | run_frame > run_shell > precision_pass > run_fittings > run_shell > retool_extruder > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | structure_0192 (level 10) | run_fittings > precision_pass > retool_cutter > run_frame > precision_pass > run_shell > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | no gear | structure_0341 (level 11) | run_frame x2 > precision_pass > run_frame > run_shell > precision_pass > retool_extruder > precision_pass x3 > retool_bonder > run_fittings > run_shell > precision_pass > run_shell > precision_pass > retool_cutter > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 99.5 |
| 11-20 | no gear | structure_0139 (level 13) | run_fittings > precision_pass x3 > run_frame x2 > run_shell > run_fittings > run_shell > retool_cutter > precision_pass x3 > retool_extruder > precision_pass x3 > run_frame > assemble | 100.0 | 100.0 | 99.6 |
| 11-20 | no gear | structure_0339 (level 14) | run_shell > precision_pass > run_shell > precision_pass > run_frame > retool_cutter > precision_pass x2 > run_fittings > retool_bonder > precision_pass x2 > run_frame > run_fittings > retool_extruder > precision_pass > retool_cutter > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 99.9 |
| 11-20 | no gear | structure_0018 (level 16) | run_shell > precision_pass > run_fittings > precision_pass > retool_bonder > precision_pass x2 > retool_cutter > run_frame > precision_pass x3 > retool_extruder > precision_pass > run_shell > run_frame > run_fittings > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | no gear | structure_0005 (level 18) | run_frame > precision_pass > run_shell > precision_pass > retool_extruder > precision_pass > retool_bonder > run_fittings > precision_pass x3 > retool_extruder > run_shell > run_frame > run_fittings > precision_pass x2 > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 99.8 |
| 11-20 | no gear | structure_0014 (level 20) | run_shell > precision_pass > run_frame > precision_pass > retool_cutter > precision_pass x3 > retool_bonder > run_fittings > precision_pass x3 > run_frame > run_shell > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | structure_0341 (level 11) | run_frame x2 > run_shell x2 > run_fittings > run_shell > precision_pass x2 > retool_extruder > precision_pass x2 > retool_bonder > run_fittings > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | structure_0139 (level 13) | run_fittings > precision_pass > run_frame > precision_pass > run_frame > run_shell > run_fittings > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_shell > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | structure_0339 (level 14) | run_shell > precision_pass x2 > retool_cutter > run_frame > run_shell > run_fittings > run_shell > precision_pass x2 > retool_bonder > run_fittings > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | structure_0018 (level 16) | run_shell x2 > precision_pass x2 > retool_cutter > run_frame > precision_pass > run_frame > run_fittings > precision_pass > retool_bonder > precision_pass > run_fittings > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | structure_0005 (level 18) | run_frame > run_shell > precision_pass x2 > retool_extruder > run_fittings > precision_pass > retool_bonder > run_shell > run_fittings > run_frame > run_shell > precision_pass x2 > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | structure_0014 (level 20) | run_shell > run_frame > run_fittings > run_shell > run_frame > run_fittings > precision_pass x2 > retool_cutter > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | structure_0341 (level 11) | run_frame > run_shell > precision_pass x2 > retool_extruder > run_fittings > precision_pass x2 > retool_bonder > run_shell > run_frame > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | structure_0139 (level 13) | run_fittings > precision_pass x2 > run_frame > run_shell > run_fittings > run_frame > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_shell > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | structure_0339 (level 14) | run_shell > run_frame > run_fittings > run_shell > run_frame > precision_pass > retool_cutter > precision_pass x2 > retool_bonder > precision_pass x2 > run_fittings > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | structure_0018 (level 16) | run_shell > run_frame > precision_pass > retool_cutter > run_fittings > precision_pass x2 > retool_bonder > run_frame > run_shell > precision_pass x2 > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | structure_0005 (level 18) | run_frame > run_shell > precision_pass x2 > retool_extruder > precision_pass > retool_bonder > run_fittings > run_shell > precision_pass x2 > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | structure_0014 (level 20) | run_shell > run_frame > run_fittings > run_shell > run_frame > precision_pass x2 > retool_cutter > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | structure_0341 (level 11) | run_frame > run_shell > precision_pass > run_fittings > run_shell > retool_extruder > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | structure_0139 (level 13) | run_fittings > precision_pass > run_frame > retool_cutter > precision_pass > run_shell > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | structure_0339 (level 14) | run_frame > run_shell > precision_pass > run_fittings > retool_cutter > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | structure_0018 (level 16) | run_frame > run_shell > precision_pass > run_fittings > run_frame > retool_cutter > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | structure_0005 (level 18) | run_frame > run_shell > precision_pass > retool_bonder > run_shell > run_fittings > precision_pass > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | structure_0014 (level 20) | run_shell > service > run_fittings > precision_pass x2 > retool_cutter > run_frame > precision_pass > retool_cutter > precision_pass > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 99.0 |
| 21-30 | no gear | structure_0147 (level 22) | run_frame > run_fittings > precision_pass x2 > retool_extruder > run_shell > run_frame > precision_pass x2 > retool_bonder > precision_pass > run_shell > precision_pass > retool_cutter > precision_pass > retool_extruder > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | structure_0037 (level 25) | dual_feed_first > dual_feed_second > precision_pass x3 > run_frame > retool_cutter > precision_pass x3 > retool_extruder > precision_pass x3 > run_shell > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | structure_0298 (level 26) | dual_feed_first > precision_pass > dual_feed_second > precision_pass x2 > run_frame > retool_cutter > precision_pass x3 > retool_extruder > precision_pass x3 > run_shell > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | structure_0377 (level 27) | dual_feed_second > precision_pass x2 > retool_bonder > precision_pass x2 > run_frame > retool_cutter > precision_pass x3 > retool_extruder > precision_pass > run_frame > run_shell > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | structure_0093 (level 29) | run_frame > service > run_frame > dual_feed_second > precision_pass x2 > retool_bonder > precision_pass x3 > retool_cutter > run_frame > precision_pass x3 > retool_extruder > run_shell > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | structure_0356 (level 30) | dual_feed_first > precision_pass > dual_feed_second > precision_pass x2 > run_frame > retool_cutter > precision_pass x3 > retool_extruder > precision_pass x3 > run_shell > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | structure_0147 (level 22) | run_frame > run_shell > run_fittings > precision_pass x2 > retool_extruder > run_shell > run_frame > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | structure_0037 (level 25) | dual_feed_first > dual_feed_second > precision_pass x2 > run_frame > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_shell > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | structure_0298 (level 26) | dual_feed_second > dual_feed_first > precision_pass x2 > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > dual_feed_first > run_shell > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | structure_0377 (level 27) | dual_feed_first > precision_pass x2 > retool_cutter > run_shell > run_fittings > precision_pass x2 > retool_bonder > run_shell > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | structure_0093 (level 29) | dual_feed_first > precision_pass x2 > retool_cutter > precision_pass x2 > retool_bonder > dual_feed_second > run_shell > run_frame > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | structure_0356 (level 30) | dual_feed_first > precision_pass > dual_feed_second > precision_pass > run_frame > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_shell > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | structure_0147 (level 22) | run_frame > run_shell > precision_pass x2 > retool_extruder > run_fittings > run_shell > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | structure_0037 (level 25) | dual_feed_second > dual_feed_first > precision_pass x2 > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_frame > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | structure_0298 (level 26) | dual_feed_second > precision_pass x2 > dual_feed_first > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_frame > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | structure_0377 (level 27) | dual_feed_first > precision_pass x2 > retool_cutter > run_shell > run_fittings > precision_pass x2 > retool_bonder > run_shell > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | structure_0093 (level 29) | dual_feed_first > precision_pass x2 > retool_cutter > run_shell > run_fittings > precision_pass x2 > retool_bonder > run_shell > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | structure_0356 (level 30) | dual_feed_first > precision_pass > dual_feed_second > precision_pass > run_frame > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_shell > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | structure_0147 (level 22) | run_fittings > run_frame > run_shell > service > precision_pass > service > precision_pass > service > retool_extruder > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 98.9 |
| 21-30 | fully modded gear | structure_0037 (level 25) | dual_feed_first > run_shell > precision_pass x2 > retool_cutter > precision_pass > retool_extruder > precision_pass x2 > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 99.0 |
| 21-30 | fully modded gear | structure_0298 (level 26) | dual_feed_first > precision_pass > run_shell > precision_pass > retool_cutter > precision_pass > retool_extruder > precision_pass x2 > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 98.9 |
| 21-30 | fully modded gear | structure_0377 (level 27) | dual_feed_first > precision_pass x2 > retool_cutter > precision_pass x2 > retool_bonder > dual_feed_second > precision_pass x2 > assemble | 100.0 | 100.0 | 99.2 |
| 21-30 | fully modded gear | structure_0093 (level 29) | dual_feed_first > precision_pass x2 > dual_feed_second > retool_cutter > precision_pass > service > precision_pass > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 99.1 |
| 21-30 | fully modded gear | structure_0356 (level 30) | dual_feed_first > precision_pass > run_shell > precision_pass > retool_cutter > precision_pass > retool_extruder > precision_pass x2 > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 98.9 |
| 31-40 | no gear | structure_0379 (level 31) | dual_feed_second > dual_feed_first > precision_pass x3 > retool_cutter > precision_pass x2 > retool_bonder > precision_pass > run_frame > run_shell > precision_pass x2 > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | structure_0424 (level 32) | dual_feed_first > precision_pass x2 > retool_cutter > precision_pass x3 > dual_feed_second > retool_bonder > precision_pass x3 > retool_extruder > run_shell > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | structure_0384 (level 34) | dual_feed_first > precision_pass x2 > retool_cutter > precision_pass > dual_feed_second > run_shell > precision_pass > retool_bonder > precision_pass x2 > retool_extruder > precision_pass > retool_cutter > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | structure_0108 (level 36) | dual_feed_first > precision_pass x2 > retool_extruder > precision_pass x3 > dual_feed_second > retool_bonder > precision_pass x2 > retool_cutter > run_frame > service > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 99.5 |
| 31-40 | no gear | structure_0325 (level 37) | dual_feed_first > precision_pass x2 > retool_cutter > precision_pass x3 > retool_extruder > precision_pass > retool_bonder > dual_feed_second > precision_pass > run_frame > run_shell > precision_pass > service > precision_pass > assemble | 100.0 | 100.0 | 98.8 |
| 31-40 | no gear | structure_0104 (level 40) | dual_feed_second > precision_pass > dual_feed_first > precision_pass x2 > dual_feed_first > retool_cutter > precision_pass x3 > retool_extruder > service > precision_pass x3 > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | structure_0379 (level 31) | dual_feed_first > precision_pass > run_shell > service > precision_pass x2 > retool_cutter > precision_pass x2 > dual_feed_second > precision_pass > retool_bonder > precision_pass x2 > run_shell x2 > precision_pass > assemble | 100.0 | 100.0 | 98.4 |
| 31-40 | mid gear | structure_0424 (level 32) | dual_feed_first > service > precision_pass x2 > retool_cutter > precision_pass > service > dual_feed_first > run_fittings > precision_pass > retool_bonder > precision_pass x3 > retool_cutter > precision_pass > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 98.2 |
| 31-40 | mid gear | structure_0384 (level 34) | dual_feed_first > precision_pass x2 > retool_cutter > precision_pass x2 > retool_bonder > run_fittings > precision_pass x2 > run_frame > run_shell > retool_extruder > precision_pass > retool_cutter > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | structure_0108 (level 36) | run_frame > precision_pass > run_shell > precision_pass > retool_extruder > precision_pass x3 > dual_feed_second > retool_bonder > precision_pass x2 > service > retool_cutter > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 97.8 |
| 31-40 | mid gear | structure_0325 (level 37) | dual_feed_first > precision_pass x2 > retool_cutter > precision_pass x3 > dual_feed_second > retool_bonder > precision_pass > service > precision_pass x2 > run_shell > retool_extruder > precision_pass > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 99.7 |
| 31-40 | mid gear | structure_0104 (level 40) | dual_feed_second > precision_pass x2 > dual_feed_first > precision_pass > retool_cutter > precision_pass x3 > retool_extruder > precision_pass > service > precision_pass x2 > run_frame > assemble | 100.0 | 100.0 | 99.1 |
| 31-40 | best gear | structure_0379 (level 31) | dual_feed_first > precision_pass x2 > retool_cutter > precision_pass > dual_feed_second > precision_pass > retool_bonder > precision_pass > run_shell > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | structure_0424 (level 32) | dual_feed_first > precision_pass x2 > retool_cutter > precision_pass > retool_bonder > dual_feed_second > run_frame > precision_pass x2 > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | structure_0384 (level 34) | dual_feed_first > precision_pass x2 > retool_cutter > precision_pass > run_shell > run_frame > run_fittings > precision_pass > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | structure_0108 (level 36) | dual_feed_second > service > precision_pass > run_shell > service > precision_pass > dual_feed_first > precision_pass > retool_bonder > precision_pass > retool_extruder > precision_pass > retool_extruder > precision_pass x2 > retool_bonder > precision_pass > run_shell > precision_pass x2 > assemble | 100.0 | 100.0 | 98.5 |
| 31-40 | best gear | structure_0325 (level 37) | dual_feed_first > precision_pass x2 > retool_cutter > dual_feed_first > precision_pass > retool_cutter > precision_pass > dual_feed_first > precision_pass > run_fittings > retool_bonder > precision_pass > service > precision_pass x2 > retool_extruder > precision_pass > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 98.6 |
| 31-40 | best gear | structure_0104 (level 40) | dual_feed_second > dual_feed_first > precision_pass x3 > retool_cutter > precision_pass x3 > retool_extruder > precision_pass > service > precision_pass x2 > run_frame > assemble | 100.0 | 100.0 | 97.3 |
| 31-40 | fully modded gear | structure_0379 (level 31) | dual_feed_second > precision_pass > dual_feed_first > precision_pass > retool_cutter > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 99.3 |
| 31-40 | fully modded gear | structure_0424 (level 32) | dual_feed_first > precision_pass x2 > retool_cutter > run_fittings > precision_pass > retool_bonder > precision_pass > retool_cutter > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | structure_0384 (level 34) | dual_feed_first > precision_pass x2 > retool_cutter > run_fittings > precision_pass > retool_bonder > precision_pass > retool_cutter > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | structure_0108 (level 36) | dual_feed_first > precision_pass x2 > retool_extruder > run_fittings > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | structure_0325 (level 37) | dual_feed_first > precision_pass x2 > retool_cutter > run_fittings > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | structure_0104 (level 40) | dual_feed_first > precision_pass x2 > run_shell > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | structure_0048 (level 42) | dual_feed_second > precision_pass x4 > service > precision_pass x4 > dual_feed_first > retool_bonder > precision_pass x2 > retool_cutter > precision_pass x2 > service > dual_feed_first > precision_pass x2 > retool_bonder > run_shell > run_frame > precision_pass x2 > assemble | 100.0 | 99.0 | 99.1 |
| 41-53 | no gear | structure_0317 (level 44) | dual_feed_first > precision_pass > dual_feed_second > precision_pass x3 > run_frame > retool_cutter > precision_pass > service > precision_pass x2 > retool_extruder > precision_pass x2 > service > precision_pass x2 > retool_cutter > precision_pass > run_shell > assemble | 100.0 | 95.9 | 97.1 |
| 41-53 | no gear | structure_0103 (level 46) | dual_feed_second > dual_feed_first x2 > precision_pass x2 > dual_feed_first > retool_cutter > precision_pass > run_shell > service > precision_pass > dual_feed_second > service > precision_pass > run_fittings > retool_bonder > precision_pass > run_fittings > retool_bonder > service > precision_pass > retool_extruder > service > dual_feed_second > precision_pass > retool_bonder > dual_feed_first > precision_pass > run_shell > assemble | 94.5 | 92.3 | 73.2 |
| 41-53 | no gear | structure_5015 (level 47) | dual_feed_first > precision_pass x2 > dual_feed_second x2 > precision_pass > retool_cutter > retool_extruder > dual_feed_second > service > precision_pass > run_frame x2 > dual_feed_second > retool_cutter > precision_pass > service > precision_pass x2 > retool_extruder > precision_pass > run_frame > precision_pass > assemble | 94.0 | 92.7 | 85.5 |
| 41-53 | no gear | structure_0324 (level 49) | dual_feed_second > dual_feed_first x2 > precision_pass > dual_feed_second > service > precision_pass x3 > dual_feed_first > run_frame > retool_cutter > precision_pass > service > precision_pass > service > precision_pass x2 > service > retool_bonder > precision_pass x2 > run_frame > run_shell > precision_pass > assemble | 98.7 | 88.4 | 90.5 |
| 41-53 | no gear | structure_5003 (level 50) | dual_feed_second > precision_pass x4 > dual_feed_first > precision_pass x2 > dual_feed_first > precision_pass > run_fittings > retool_extruder > precision_pass > service x2 > precision_pass > dual_feed_first > precision_pass > retool_cutter > service > precision_pass x4 > dual_feed_first > run_shell > assemble | 97.0 | 90.7 | 86.6 |
| 41-53 | mid gear | structure_0048 (level 42) | dual_feed_first > precision_pass x3 > retool_cutter > precision_pass > run_fittings > precision_pass > service > retool_bonder > precision_pass > run_shell x2 > precision_pass x2 > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 99.7 |
| 41-53 | mid gear | structure_0317 (level 44) | dual_feed_first > precision_pass x3 > dual_feed_second > retool_cutter > precision_pass x3 > retool_extruder > service > precision_pass x3 > run_frame > service > run_shell > assemble | 100.0 | 100.0 | 98.8 |
| 41-53 | mid gear | structure_0103 (level 46) | run_shell > precision_pass x2 > run_fittings > precision_pass > retool_cutter > service > run_frame > precision_pass x2 > run_shell > service > precision_pass > retool_bonder > precision_pass > run_fittings > precision_pass > service > precision_pass > assemble | 100.0 | 100.0 | 99.9 |
| 41-53 | mid gear | structure_5015 (level 47) | dual_feed_first > precision_pass x3 > dual_feed_second > precision_pass > retool_cutter > service > precision_pass > retool_extruder > precision_pass x2 > service > precision_pass > retool_cutter > precision_pass x3 > retool_extruder > service > precision_pass > run_shell > assemble | 100.0 | 100.0 | 95.9 |
| 41-53 | mid gear | structure_0324 (level 49) | dual_feed_first > service > precision_pass x3 > retool_cutter > dual_feed_first > precision_pass > dual_feed_first > retool_cutter > precision_pass x2 > service > precision_pass > run_fittings > retool_bonder > precision_pass > service > precision_pass x3 > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 95.9 |
| 41-53 | mid gear | structure_5003 (level 50) | dual_feed_first > precision_pass x2 > dual_feed_second > precision_pass x2 > run_frame > retool_cutter > service > precision_pass x2 > retool_extruder > precision_pass > service > precision_pass x3 > retool_cutter > precision_pass > service > precision_pass > run_shell > assemble | 100.0 | 100.0 | 98.1 |
| 41-53 | best gear | structure_0048 (level 42) | dual_feed_first > precision_pass x2 > retool_cutter > precision_pass x3 > retool_extruder > dual_feed_second > retool_bonder > service > precision_pass x2 > retool_extruder > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 99.9 |
| 41-53 | best gear | structure_0317 (level 44) | dual_feed_second > dual_feed_first > precision_pass x3 > retool_cutter > precision_pass x2 > retool_extruder > precision_pass > service > precision_pass x2 > retool_cutter > precision_pass > run_frame > assemble | 100.0 | 100.0 | 99.5 |
| 41-53 | best gear | structure_0103 (level 46) | dual_feed_first > service > dual_feed_second > service > precision_pass x3 > run_frame > retool_cutter > precision_pass x2 > service > precision_pass > retool_bonder > precision_pass > service > run_shell > run_frame > precision_pass x2 > assemble | 100.0 | 100.0 | 99.3 |
| 41-53 | best gear | structure_5015 (level 47) | dual_feed_second > precision_pass > dual_feed_first > precision_pass x2 > retool_cutter > service > precision_pass x3 > retool_extruder > precision_pass > service > precision_pass x2 > assemble | 100.0 | 100.0 | 97.2 |
| 41-53 | best gear | structure_0324 (level 49) | dual_feed_first > precision_pass x2 > retool_cutter > precision_pass x2 > retool_extruder > precision_pass > retool_cutter > service > run_fittings > retool_bonder > precision_pass x2 > retool_cutter > precision_pass > service > dual_feed_first > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 99.8 |
| 41-53 | best gear | structure_5003 (level 50) | dual_feed_second > precision_pass x3 > dual_feed_first > retool_cutter > precision_pass > service > precision_pass x2 > retool_extruder > precision_pass x2 > service > precision_pass > run_frame > assemble | 100.0 | 100.0 | 98.7 |
| 41-53 | fully modded gear | structure_0048 (level 42) | dual_feed_first > precision_pass x2 > retool_cutter > run_fittings > precision_pass x2 > retool_bonder > precision_pass > run_shell > service > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 41-53 | fully modded gear | structure_0317 (level 44) | dual_feed_first > precision_pass x2 > retool_cutter > precision_pass x2 > run_shell > retool_extruder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 41-53 | fully modded gear | structure_0103 (level 46) | dual_feed_first > precision_pass x2 > retool_cutter > run_fittings > precision_pass > retool_extruder > retool_cutter > retool_extruder > retool_bonder > service > precision_pass x2 > run_shell > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 41-53 | fully modded gear | structure_5015 (level 47) | dual_feed_second > precision_pass > run_frame > precision_pass > retool_cutter > precision_pass x2 > retool_extruder > service > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 41-53 | fully modded gear | structure_0324 (level 49) | run_shell > run_frame > run_fittings > precision_pass x2 > retool_cutter > precision_pass x2 > service > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 41-53 | fully modded gear | structure_5003 (level 50) | dual_feed_first > precision_pass x2 > run_shell > retool_cutter > precision_pass x2 > retool_extruder > service > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |

## Length

### Actions per attempt of the adaptive player at level (item craft at level)

| Band | Gear | Recipes played | Mean actions per attempt | Shortest recipe | Longest recipe | Stalled attempts | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 15.8 | 12.5 | 17.1 | 0 | 10-20 | Passed |
| 1-10 | mid gear | 6 | 15.3 | 14.6 | 16.1 | 0 | 10-20 | Passed |
| 1-10 | best gear | 6 | 15.0 | 14.2 | 15.5 | 0 | 10-20 | Passed |
| 1-10 | fully modded gear | 6 | 9.7 | 9.0 | 10.0 | 0 | 10-20 | Reported |
| 11-20 | no gear | 6 | 18.6 | 18.2 | 19.2 | 0 | 10-20 | Passed |
| 11-20 | mid gear | 6 | 15.4 | 14.6 | 16.0 | 0 | 10-20 | Passed |
| 11-20 | best gear | 6 | 14.6 | 14.2 | 15.0 | 0 | 10-20 | Passed |
| 11-20 | fully modded gear | 6 | 9.6 | 9.0 | 10.0 | 0 | 10-20 | Reported |
| 21-30 | no gear | 6 | 16.9 | 16.4 | 18.1 | 0 | 10-20 | Passed |
| 21-30 | mid gear | 6 | 13.0 | 12.5 | 14.1 | 0 | 10-20 | Passed |
| 21-30 | best gear | 6 | 12.9 | 12.7 | 13.3 | 0 | 10-20 | Passed |
| 21-30 | fully modded gear | 6 | 9.4 | 9.0 | 10.1 | 0 | 10-20 | Reported |
| 31-40 | no gear | 6 | 16.5 | 15.6 | 17.8 | 0 | 10-20 | Reported |
| 31-40 | mid gear | 6 | 15.6 | 13.5 | 17.4 | 0 | 10-20 | Passed |
| 31-40 | best gear | 6 | 13.6 | 12.5 | 15.9 | 0 | 10-20 | Passed |
| 31-40 | fully modded gear | 6 | 11.1 | 10.1 | 12.4 | 0 | 10-20 | Reported |
| 41-53 | no gear | 6 | 18.8 | 17.3 | 19.5 | 0 | 10-20 | Reported |
| 41-53 | mid gear | 6 | 18.9 | 17.8 | 19.7 | 0 | 10-20 | Reported |
| 41-53 | best gear | 6 | 16.9 | 16.3 | 18.0 | 0 | 10-20 | Passed |
| 41-53 | fully modded gear | 6 | 12.1 | 10.7 | 13.6 | 0 | 10-20 | Reported |

## Economy parity

### Component stacks consumed per item made, failures included, over the recipes the legacy policy makes at least once in the cell (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Parity rule | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 2.17 | 2.17 | 1.00 | 100% | 99% | 81% | 1% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | mid gear | 6 | 2.17 | 2.19 | 0.99 | 100% | 98% | 100% | 8% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | best gear | 6 | 2.17 | 2.17 | 1.00 | 100% | 99% | 100% | 19% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | fully modded gear | 6 | 2.17 | 2.18 | 0.99 | 100% | 99% | 100% | 58% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | no gear | 6 | 2.33 | 2.46 | 0.95 | 100% | 93% | 100% | 1% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | mid gear | 6 | 2.33 | 2.34 | 1.00 | 100% | 99% | 100% | 14% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | best gear | 6 | 2.33 | 2.34 | 1.00 | 100% | 99% | 100% | 31% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | fully modded gear | 6 | 2.33 | 2.35 | 0.99 | 100% | 99% | 100% | 94% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | no gear | 6 | 2.33 | 2.37 | 0.99 | 100% | 98% | 100% | 8% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | mid gear | 6 | 2.33 | 2.33 | 1.00 | 100% | 100% | 100% | 18% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | best gear | 6 | 2.33 | 2.34 | 1.00 | 100% | 99% | 100% | 30% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | fully modded gear | 6 | 2.33 | 2.35 | 0.99 | 100% | 99% | 99% | 94% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | no gear | 6 | 2.50 | 2.51 | 1.00 | 100% | 99% | 100% | 15% | yes | 0% / 0% | within 10% | Reported |
| 31-40 | mid gear | 6 | 2.50 | 2.53 | 0.99 | 100% | 98% | 99% | 27% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | best gear | 6 | 2.50 | 2.51 | 1.00 | 100% | 99% | 99% | 37% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | fully modded gear | 6 | 2.50 | 2.53 | 0.99 | 100% | 98% | 100% | 86% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | no gear | 6 | 2.43 | 2.35 | 1.03 | 94% | 99% | 89% | 42% | yes | 0% / 0% | within 10% | Reported |
| 41-53 | mid gear | 6 | 2.33 | 2.33 | 1.00 | 100% | 100% | 98% | 73% | yes | 0% / 0% | within 10% | Reported |
| 41-53 | best gear | 6 | 2.33 | 2.35 | 0.99 | 100% | 99% | 99% | 91% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | fully modded gear | 6 | 2.33 | 2.38 | 0.98 | 100% | 97% | 100% | 96% | yes | 0% / 0% | within 10% | Passed |

## XP rate

### Item crafts: XP per hour against the legacy loop (6.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per craft | Legacy actions per craft | Legacy XP per hour | New XP per craft | New actions per craft | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 602 | 5.0 | 72709 | 2605 | 15.8 | 74280 | 1.02 | - | Passed |
| 1-10 | no gear, 1 enhancement | 419 | 6.1 | 41369 | 2632 | 19.8 | 59793 | 1.45 | - | Passed |
| 1-10 | no gear, 2 enhancements | 141 | 6.0 | 14100 | 2631 | 23.9 | 49502 | 3.51 | - | Passed |
| 1-10 | no gear, 3 enhancements | 90 | 6.0 | 9000 | 2642 | 27.9 | 42642 | 4.74 | - | Passed |
| 1-10 | mid gear | 636 | 6.6 | 58041 | 2880 | 15.3 | 84737 | 1.46 | - | Passed |
| 1-10 | mid gear, 1 enhancement | 617 | 7.3 | 50596 | 2880 | 18.1 | 71624 | 1.42 | - | Passed |
| 1-10 | mid gear, 2 enhancements | 582 | 8.1 | 43402 | 2880 | 20.3 | 63738 | 1.47 | - | Passed |
| 1-10 | mid gear, 3 enhancements | 513 | 8.6 | 35819 | 2880 | 23.0 | 56334 | 1.57 | - | Passed |
| 1-10 | best gear | 712 | 6.8 | 62752 | 2880 | 15.0 | 86592 | 1.38 | - | Passed |
| 1-10 | best gear, 1 enhancement | 692 | 7.4 | 56262 | 2880 | 16.9 | 76561 | 1.36 | - | Passed |
| 1-10 | best gear, 2 enhancements | 677 | 8.0 | 50959 | 2880 | 19.0 | 68131 | 1.34 | - | Passed |
| 1-10 | best gear, 3 enhancements | 643 | 8.3 | 46616 | 2880 | 21.6 | 60139 | 1.29 | - | Passed |
| 1-10 | fully modded gear | 942 | 5.8 | 96659 | 2880 | 9.7 | 133915 | 1.39 | - | Passed |
| 1-10 | fully modded gear, 1 enhancement | 936 | 5.7 | 98631 | 2880 | 11.4 | 113574 | 1.15 | - | Passed |
| 1-10 | fully modded gear, 2 enhancements | 927 | 6.1 | 90570 | 2880 | 12.4 | 104423 | 1.15 | - | Passed |
| 1-10 | fully modded gear, 3 enhancements | 899 | 6.3 | 85256 | 2880 | 13.9 | 93275 | 1.09 | - | Passed |
| 11-20 | no gear | 571 | 8.1 | 42410 | 1798 | 18.6 | 43438 | 1.02 | - | Passed |
| 11-20 | no gear, 1 enhancement | 178 | 6.7 | 15963 | 1796 | 21.3 | 38020 | 2.38 | - | Passed |
| 11-20 | no gear, 2 enhancements | 170 | 7.1 | 14456 | 1796 | 24.4 | 33102 | 2.29 | - | Passed |
| 11-20 | no gear, 3 enhancements | 170 | 7.5 | 13647 | 1796 | 27.2 | 29678 | 2.17 | - | Passed |
| 11-20 | mid gear | 682 | 12.1 | 33794 | 1800 | 15.4 | 52484 | 1.55 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 647 | 12.8 | 30366 | 1800 | 17.4 | 46492 | 1.53 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 611 | 13.9 | 26363 | 1800 | 19.9 | 40658 | 1.54 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 570 | 15.5 | 22055 | 1800 | 22.6 | 35823 | 1.62 | - | Passed |
| 11-20 | best gear | 783 | 14.4 | 32668 | 1800 | 14.6 | 55479 | 1.70 | - | Passed |
| 11-20 | best gear, 1 enhancement | 751 | 14.7 | 30676 | 1800 | 16.6 | 48861 | 1.59 | - | Passed |
| 11-20 | best gear, 2 enhancements | 747 | 16.2 | 27567 | 1800 | 18.8 | 43060 | 1.56 | - | Passed |
| 11-20 | best gear, 3 enhancements | 697 | 16.1 | 25944 | 1800 | 21.1 | 38460 | 1.48 | - | Passed |
| 11-20 | fully modded gear | 1158 | 13.6 | 51172 | 1798 | 9.6 | 84354 | 1.65 | - | Passed |
| 11-20 | fully modded gear, 1 enhancement | 1161 | 13.9 | 50070 | 1798 | 11.2 | 72511 | 1.45 | - | Passed |
| 11-20 | fully modded gear, 2 enhancements | 1141 | 14.3 | 47964 | 1798 | 12.4 | 65091 | 1.36 | - | Passed |
| 11-20 | fully modded gear, 3 enhancements | 1144 | 14.5 | 47234 | 1798 | 13.7 | 59006 | 1.25 | - | Passed |
| 21-30 | no gear | 638 | 11.1 | 34655 | 1500 | 16.9 | 39980 | 1.15 | - | Passed |
| 21-30 | no gear, 1 enhancement | 601 | 11.8 | 30664 | 1500 | 18.5 | 36535 | 1.19 | - | Passed |
| 21-30 | no gear, 2 enhancements | 567 | 13.0 | 26196 | 1500 | 20.2 | 33416 | 1.28 | - | Passed |
| 21-30 | no gear, 3 enhancements | 561 | 14.7 | 22915 | 1500 | 22.1 | 30563 | 1.33 | - | Passed |
| 21-30 | mid gear | 705 | 17.8 | 23814 | 1500 | 13.0 | 52034 | 2.19 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 676 | 17.4 | 23363 | 1500 | 14.5 | 46427 | 1.99 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 674 | 17.8 | 22686 | 1500 | 16.2 | 41581 | 1.83 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 626 | 18.1 | 20753 | 1500 | 17.9 | 37733 | 1.82 | - | Passed |
| 21-30 | best gear | 779 | 22.4 | 20833 | 1500 | 12.9 | 52303 | 2.51 | - | Passed |
| 21-30 | best gear, 1 enhancement | 762 | 21.5 | 21239 | 1500 | 13.8 | 48736 | 2.29 | - | Passed |
| 21-30 | best gear, 2 enhancements | 738 | 22.5 | 19708 | 1500 | 15.5 | 43502 | 2.21 | - | Passed |
| 21-30 | best gear, 3 enhancements | 720 | 21.7 | 19944 | 1500 | 16.9 | 39928 | 2.00 | - | Passed |
| 21-30 | fully modded gear | 1156 | 21.8 | 31807 | 1492 | 9.4 | 71298 | 2.24 | - | Passed |
| 21-30 | fully modded gear, 1 enhancement | 1122 | 21.3 | 31573 | 1491 | 10.2 | 65587 | 2.08 | - | Passed |
| 21-30 | fully modded gear, 2 enhancements | 1112 | 21.4 | 31226 | 1490 | 10.6 | 63571 | 2.04 | - | Passed |
| 21-30 | fully modded gear, 3 enhancements | 1107 | 22.1 | 30100 | 1490 | 11.3 | 59534 | 1.98 | - | Passed |
| 31-40 | no gear | 689 | 13.7 | 30240 | 1318 | 16.5 | 35875 | 1.19 | - | Passed |
| 31-40 | no gear, 1 enhancement | 638 | 15.0 | 25554 | 1320 | 17.1 | 34693 | 1.36 | - | Passed |
| 31-40 | no gear, 2 enhancements | 617 | 14.7 | 25221 | 1320 | 18.1 | 32845 | 1.30 | - | Passed |
| 31-40 | no gear, 3 enhancements | 595 | 15.6 | 22929 | 1320 | 19.2 | 31007 | 1.35 | - | Passed |
| 31-40 | mid gear | 750 | 19.7 | 22806 | 1312 | 15.6 | 37977 | 1.67 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 739 | 20.6 | 21566 | 1316 | 16.1 | 36873 | 1.71 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 713 | 20.5 | 20839 | 1317 | 17.1 | 34649 | 1.66 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 684 | 19.7 | 20806 | 1317 | 17.7 | 33486 | 1.61 | - | Passed |
| 31-40 | best gear | 821 | 24.6 | 20052 | 1314 | 13.6 | 43606 | 2.17 | - | Passed |
| 31-40 | best gear, 1 enhancement | 819 | 25.4 | 19364 | 1317 | 14.1 | 42083 | 2.17 | - | Passed |
| 31-40 | best gear, 2 enhancements | 804 | 25.7 | 18746 | 1317 | 14.8 | 40067 | 2.14 | - | Passed |
| 31-40 | best gear, 3 enhancements | 783 | 23.9 | 19650 | 1317 | 15.5 | 38236 | 1.95 | - | Passed |
| 31-40 | fully modded gear | 1104 | 25.2 | 26317 | 1319 | 11.1 | 53586 | 2.04 | - | Passed |
| 31-40 | fully modded gear, 1 enhancement | 1116 | 25.5 | 26258 | 1320 | 11.6 | 51400 | 1.96 | - | Passed |
| 31-40 | fully modded gear, 2 enhancements | 1104 | 26.2 | 25270 | 1320 | 11.8 | 50193 | 1.99 | - | Passed |
| 31-40 | fully modded gear, 3 enhancements | 1096 | 26.2 | 25114 | 1320 | 12.3 | 48464 | 1.93 | - | Passed |
| 41-53 | no gear | 847 | 19.0 | 26766 | 1170 | 18.8 | 28010 | 1.05 | - | Passed |
| 41-53 | no gear, 1 enhancement | 763 | 17.4 | 26335 | 1060 | 19.4 | 24626 | 0.94 | - | Passed |
| 41-53 | no gear, 2 enhancements | 716 | 17.7 | 24258 | 1132 | 20.6 | 24729 | 1.02 | - | Passed |
| 41-53 | no gear, 3 enhancements | 623 | 17.9 | 20900 | 1084 | 21.7 | 22500 | 1.08 | - | Passed |
| 41-53 | mid gear | 1038 | 26.6 | 23389 | 1259 | 18.9 | 29967 | 1.28 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 969 | 25.1 | 23157 | 1254 | 18.5 | 30525 | 1.32 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 890 | 23.2 | 23018 | 1265 | 19.4 | 29355 | 1.28 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 811 | 22.4 | 21682 | 1266 | 19.3 | 29527 | 1.36 | - | Passed |
| 41-53 | best gear | 1138 | 28.9 | 23636 | 1266 | 16.9 | 33785 | 1.43 | - | Passed |
| 41-53 | best gear, 1 enhancement | 1116 | 28.8 | 23278 | 1261 | 17.2 | 33073 | 1.42 | - | Passed |
| 41-53 | best gear, 2 enhancements | 1036 | 27.1 | 22940 | 1268 | 17.4 | 32860 | 1.43 | - | Passed |
| 41-53 | best gear, 3 enhancements | 983 | 26.3 | 22415 | 1271 | 17.4 | 32924 | 1.47 | - | Passed |
| 41-53 | fully modded gear | 1159 | 26.4 | 26299 | 1272 | 12.1 | 47197 | 1.79 | - | Passed |
| 41-53 | fully modded gear, 1 enhancement | 1171 | 26.8 | 26196 | 1272 | 11.7 | 48853 | 1.86 | - | Passed |
| 41-53 | fully modded gear, 2 enhancements | 1188 | 27.6 | 25827 | 1272 | 11.7 | 48830 | 1.89 | - | Passed |
| 41-53 | fully modded gear, 3 enhancements | 1186 | 28.1 | 25279 | 1272 | 12.2 | 47090 | 1.86 | - | Passed |

## Enhancement parity

### Rank 50, fully modded gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 100%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 6 | 60 | 32% | 98% | 99% | 0.98 | 100% | 99.9% | 1.00 (+/- 0.00) | 1.02 | >= 0.95 | Passed |
| 41-45 | 2 | 6 | 60 | 63% | 99% | 99% | 1.96 | 100% | 100.0% | 2.00 (+/- 0.00) | 1.02 | >= 0.95 | Passed |
| 41-45 | 3 | 6 | 60 | 95% | 96% | 99% | 2.84 | 100% | 100.0% | 3.00 (+/- 0.00) | 1.05 | >= 0.90 | Passed |
| 46-50 | 1 | 6 | 60 | 32% | 99% | 99% | 0.98 | 100% | 100.0% | 1.00 (+/- 0.00) | 1.02 | >= 0.95 | Passed |
| 46-50 | 2 | 6 | 60 | 64% | 99% | 99% | 1.96 | 100% | 100.0% | 2.00 (+/- 0.00) | 1.02 | >= 0.95 | Passed |
| 46-50 | 3 | 6 | 60 | 96% | 99% | 99% | 2.93 | 100% | 100.0% | 3.00 (+/- 0.00) | 1.02 | >= 0.90 | Passed |
| 51-53 | 1 | 6 | 60 | 10% | 100% | 53% | 0.53 | 100% | 99.5% | 1.00 (+/- 0.00) | 1.89 | >= 0.95 | Passed |
| 51-53 | 2 | 6 | 60 | 20% | 98% | 58% | 1.13 | 100% | 99.6% | 2.00 (+/- 0.00) | 1.77 | >= 0.95 | Passed |
| 51-53 | 3 | 6 | 60 | 30% | 100% | 52% | 1.54 | 100% | 99.8% | 2.94 (+/- 0.02) | 1.92 | >= 0.90 | Passed |

### Thresholds, first / second / third slot, that would meet parity for the quality measured (the strictest set that keeps every case at least at today's, never falling from one slot to the next)

| Scope | Thresholds that meet parity | In steps of 5 | Thresholds in force |
| --- | --- | --- | --- |
| Every band together (the shared thresholds) | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 100% |
| Band 41-45 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 100% |
| Band 46-50 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 100% |
| Band 51-53 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 100% |

### Rank 50, fully modded gear: quality of the items the new minigame made, in whole percent, by band and slots filled

| Band | Slots filled | Items made | 10th percentile | 25th percentile | Median | 75th percentile | 90th percentile | Made at 100% |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 41-45 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 100% |
| 41-45 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 100% |
| 46-50 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 100% |
| 46-50 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 100% |
| 46-50 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 100% |
| 51-53 | 1 | 100% | 97% | 100% | 100% | 100% | 100% | 81% |
| 51-53 | 2 | 100% | 98% | 100% | 100% | 100% | 100% | 86% |
| 51-53 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 94% |

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
- Every crafter plays as a Production Run (full line) Builder: the style from rank 20 and every other purchase of the line the rank allows. The unperked rank-50 result for gate 3 is in the `fabricator` scenario.
