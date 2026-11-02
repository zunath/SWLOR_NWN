# Crafting revamp: balance gates of the Fabricator minigame

- Command: `SWLOR.CraftSim gates --scenario fabricator --runs 100 --seed 1 --out design/testing/crafting-revamp/fabricator-gates.md --json <output>.json`
- Seed 1; 100 runs per case.

## Inputs

- Scenario: Fabricator (`fabricator`), Fabrication recipes, minigame Fabricator.
- Quality: the quality meter's share of its maximum when the item is made, in points (0-100). A broken or abandoned piece counts 0, so every average includes the crafts that failed.
- Crafter: the rank the scenario's cohort gives each recipe (by default the recipe's level, at most 50, so a crafter works at their own level), with no food or status bonuses and no perks unless a gate names a style. Gear levels are the plan's none, middle and best tier of the skill's crafting gear:
  - no gear (Control 0, Craftsmanship 0, CP 0)
  - mid gear (Control 13, Craftsmanship 12, CP 26)
  - best gear (Control 29, Craftsmanship 30, CP 37)
- Recipes: 1-10: 6 of 66; 11-20: 6 of 84; 21-30: 6 of 108; 31-40: 6 of 102; 41-53: 6 of 96 recipes played.
- Adaptive player: depth 3, forecast samples per result 2, budget 4000 engine copies per decision, 0.5% of value lost per extra action, heuristic FabricatorLeafHeuristic. Each attempt's dice come from its own stream of the seed.
- Fixed-rotation solver: beam 8, cycles up to 3, 8 adaptive lines as seeds, up to 40 actions, 64 training attempts per local-search round and 150 selection attempts for each of 12 finalists, local search from the best 3 candidate(s) for up to 3 round(s) each. The winner is chosen on separate attempts and then scored on a third set (hold-out) with 100 attempts.
- Legacy comparator (gates 5 and 6): the old minigame's lookahead player (`SWLOR.CraftSim baseline`), replayed here with the same seed on the same recipes, ranks and gear.
- Time: 8.0 s per action in the new window and 6.0 s in the old one, plus 0.0 s per craft in both. These are estimates; the client evidence replaces them.
- XP multiplier on the base XP of every craft, failures included, as the game server applies it (`CraftMinigameXp`): 1-10 2.40x, 11-20 1.45x, 21-30 1.25x, 31-40 1.10x, 41-53 1.00x. The new side's XP below includes it; the legacy loop's does not.
- Thresholds (starting values, all in `GateThresholds`):
  - `RotationMarginPoints` = 10: gate 1: adaptive beats the best fixed rotation by at least this many quality points
  - `StyleMinimumWinShare` = 30%: gate 2: each style is the better choice on at least this share of recipes
  - `StyleMaximumMeanDifferencePoints` = 5: gate 2: neither style beats the other by more on average
  - `StyleTieMarginPoints` = 0.5: gate 2: a recipe within this many points is a tie, half a win each
  - `PerksOptionalRank` = 50: gate 3: rank of the unperked crafter
  - `PerksOptionalMinimumFullQualityShare` = 50%: gate 3: share of crafts that reach maximum quality, per at-level band
  - `PerksOptionalBands` = 41-45, 46-50, 51-53: gate 3: the at-level recipe bands
  - `MaterialTolerance` = 10%: gate 5: component stacks per item within this share of the legacy baseline
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
| No fixed rotation | Failed | A fixed rotation comes within 10 points of adaptive play in 15 case(s): 1-10 / no gear; 1-10 / mid gear; 1-10 / best gear; 11-20 / no gear; 11-20 / mid gear; 11-20 / best gear; 21-30 / no gear; 21-30 / mid gear; 21-30 / best gear; 31-40 / no gear; 31-40 / mid gear; 31-40 / best gear; 41-53 / no gear; 41-53 / mid gear; 41-53 / best gear (smallest margin -5.0 in 1-10 / no gear). |
| Style balance | NotApplicable | The scenario offers no style configurations yet, so there is nothing to compare. |
| Perks optional | Passed | An unperked rank-50 crafter in best gear reaches maximum quality on at least 50% of crafts in every at-level band (lowest 97%). |
| Length | Passed | Every band and gear tier takes 10-20 actions (item craft at level) on average. |
| Economy parity | Passed | Component stacks per item are within 10% of the legacy baseline, at no less quality, in every band and gear tier. |
| XP rate | Passed | Item crafts: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.01x in 21-30 / no gear, 1 enhancement). |
| Enhancement parity | Passed | At rank 50 in best gear, the thresholds in force (50% / 85% / 85%) transfer at least 1.00 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 1.07 times today's). Thresholds that meet parity in every band for this quality: 100% / 100% / 100%. |

## No fixed rotation

### Average quality: adaptive play against the best fixed rotation (quality points; a broken piece counts 0)

| Band | Gear | Recipes played | Adaptive avg quality | Best fixed avg quality | Margin | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 81.7 | 86.7 | -5.0 | >= 10 | Failed |
| 1-10 | mid gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | Failed |
| 1-10 | best gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | Failed |
| 11-20 | no gear | 6 | 98.4 | 100.0 | -1.6 | >= 10 | Failed |
| 11-20 | mid gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | Failed |
| 11-20 | best gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | Failed |
| 21-30 | no gear | 6 | 99.7 | 100.0 | -0.3 | >= 10 | Failed |
| 21-30 | mid gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | Failed |
| 21-30 | best gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | Failed |
| 31-40 | no gear | 6 | 99.5 | 100.0 | -0.5 | >= 10 | Failed |
| 31-40 | mid gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | Failed |
| 31-40 | best gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | Failed |
| 41-53 | no gear | 6 | 98.2 | 100.0 | -1.8 | >= 10 | Failed |
| 41-53 | mid gear | 6 | 99.9 | 100.0 | -0.1 | >= 10 | Failed |
| 41-53 | best gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | Failed |

### The best fixed rotation found for the first recipe played in each band and gear tier (average quality in points)

| Band | Gear | Recipe | Best fixed rotation | Training | Selection | Hold-out | Adaptive on this recipe |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | structure_0077 (level 2) | run_shell x2 > precision_pass x3 > run_frame > run_fittings > run_frame > run_fittings > run_shell > assemble | 20.0 | 20.0 | 20.0 | 20.0 |
| 1-10 | mid gear | structure_0077 (level 2) | run_shell x2 > run_frame > run_fittings > run_frame > run_fittings > precision_pass x2 > retool_cutter > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | structure_0077 (level 2) | run_shell > precision_pass > run_fittings > precision_pass > retool_bonder > run_frame > precision_pass x2 > retool_cutter > precision_pass > run_frame > run_shell x2 > run_fittings > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 11-20 | no gear | structure_0341 (level 11) | run_frame x2 > precision_pass > run_frame > run_shell > precision_pass > retool_extruder > precision_pass x3 > retool_bonder > run_fittings > run_shell > precision_pass > run_shell > precision_pass > retool_cutter > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 99.4 |
| 11-20 | mid gear | structure_0341 (level 11) | run_frame x2 > run_shell x2 > run_fittings > run_shell > precision_pass x2 > retool_extruder > precision_pass x2 > retool_bonder > run_fittings > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | structure_0341 (level 11) | run_frame > run_shell > precision_pass x2 > retool_extruder > run_fittings > precision_pass x2 > retool_bonder > run_shell > run_frame > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | structure_0147 (level 22) | run_fittings > run_frame > precision_pass > run_fittings > run_frame > run_shell > precision_pass > retool_extruder > run_shell > precision_pass x2 > run_fittings > retool_bonder > precision_pass x2 > run_fittings > precision_pass > retool_cutter > precision_pass > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 98.4 |
| 21-30 | mid gear | structure_0147 (level 22) | run_frame > run_shell x2 > precision_pass x2 > retool_extruder > precision_pass > run_frame > run_shell > run_fittings > precision_pass > retool_bonder > run_fittings > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | structure_0147 (level 22) | run_frame > run_shell > precision_pass x2 > retool_extruder > run_shell > run_fittings > run_frame > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | structure_0379 (level 31) | run_shell > precision_pass > run_shell > run_frame > precision_pass > retool_cutter > precision_pass x2 > run_fittings > retool_bonder > precision_pass x2 > run_frame > run_fittings > retool_extruder > precision_pass > retool_cutter > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | structure_0379 (level 31) | run_shell x2 > run_frame > run_fittings x2 > run_frame > precision_pass x2 > retool_cutter > precision_pass > retool_bonder > precision_pass x2 > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | structure_0379 (level 31) | run_shell x2 > run_frame > run_fittings > run_frame > run_fittings > precision_pass x2 > retool_cutter > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | structure_0048 (level 42) | run_shell > precision_pass > run_fittings > precision_pass > retool_bonder > precision_pass > run_frame > retool_cutter > precision_pass x3 > retool_bonder > precision_pass x2 > retool_extruder > run_shell > run_frame > service > run_fittings > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 98.6 |
| 41-53 | mid gear | structure_0048 (level 42) | run_shell x2 > run_frame > run_fittings > run_frame > run_fittings > precision_pass x2 > retool_cutter > precision_pass > retool_bonder > precision_pass x2 > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | structure_0048 (level 42) | run_shell > precision_pass x2 > run_fittings > retool_bonder > run_frame > precision_pass > retool_cutter > run_shell > run_frame > run_shell > run_fittings > precision_pass x2 > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 100.0 |

## Perks optional

### Rank 50, best gear, no perks: adaptive attempts that reach maximum quality, by recipe band

| Band | Recipes played | Attempts | Reach maximum quality | Make the item | Avg quality of items made | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 6 | 600 | 97% | 100% | 100% | >= 50% | Passed |
| 46-50 | 6 | 600 | 99% | 100% | 100% | >= 50% | Passed |
| 51-53 | 6 | 600 | 99% | 100% | 100% | >= 50% | Passed |

## Length

### Actions per attempt of the adaptive player at level (item craft at level)

| Band | Gear | Recipes played | Mean actions per attempt | Shortest recipe | Longest recipe | Stalled attempts | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 15.8 | 12.7 | 17.2 | 0 | 10-20 | Passed |
| 1-10 | mid gear | 6 | 15.4 | 15.0 | 15.8 | 0 | 10-20 | Passed |
| 1-10 | best gear | 6 | 15.1 | 14.5 | 15.3 | 0 | 10-20 | Passed |
| 11-20 | no gear | 6 | 17.6 | 16.9 | 18.4 | 0 | 10-20 | Passed |
| 11-20 | mid gear | 6 | 15.6 | 15.3 | 16.0 | 0 | 10-20 | Passed |
| 11-20 | best gear | 6 | 14.6 | 14.3 | 15.1 | 0 | 10-20 | Passed |
| 21-30 | no gear | 6 | 18.9 | 18.5 | 19.3 | 0 | 10-20 | Passed |
| 21-30 | mid gear | 6 | 15.4 | 15.0 | 15.8 | 0 | 10-20 | Passed |
| 21-30 | best gear | 6 | 14.7 | 14.0 | 15.2 | 0 | 10-20 | Passed |
| 31-40 | no gear | 6 | 19.1 | 18.7 | 19.4 | 0 | 10-20 | Passed |
| 31-40 | mid gear | 6 | 15.2 | 15.0 | 15.6 | 0 | 10-20 | Passed |
| 31-40 | best gear | 6 | 14.9 | 14.3 | 15.2 | 0 | 10-20 | Passed |
| 41-53 | no gear | 6 | 17.8 | 17.5 | 18.4 | 0 | 10-20 | Passed |
| 41-53 | mid gear | 6 | 15.5 | 15.1 | 15.8 | 0 | 10-20 | Passed |
| 41-53 | best gear | 6 | 15.0 | 14.6 | 15.3 | 0 | 10-20 | Passed |

## Economy parity

### Component stacks consumed per item made, failures included (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 2.17 | 2.19 | 0.99 | 100% | 98% | 82% | 1% | yes | 0% / 0% | Passed |
| 1-10 | mid gear | 6 | 2.17 | 2.22 | 0.98 | 100% | 96% | 100% | 8% | yes | 0% / 0% | Passed |
| 1-10 | best gear | 6 | 2.17 | 2.19 | 0.99 | 100% | 98% | 100% | 18% | yes | 0% / 0% | Passed |
| 11-20 | no gear | 6 | 2.33 | 2.49 | 0.94 | 100% | 91% | 98% | 1% | yes | 0% / 0% | Passed |
| 11-20 | mid gear | 6 | 2.33 | 2.36 | 0.99 | 100% | 98% | 100% | 14% | yes | 0% / 0% | Passed |
| 11-20 | best gear | 6 | 2.33 | 2.34 | 1.00 | 100% | 99% | 100% | 30% | yes | 0% / 0% | Passed |
| 21-30 | no gear | 6 | 2.34 | 2.38 | 0.98 | 100% | 97% | 100% | 8% | yes | 0% / 0% | Passed |
| 21-30 | mid gear | 6 | 2.33 | 2.34 | 1.00 | 100% | 100% | 100% | 17% | yes | 0% / 0% | Passed |
| 21-30 | best gear | 6 | 2.33 | 2.35 | 0.99 | 100% | 99% | 100% | 30% | yes | 0% / 0% | Passed |
| 31-40 | no gear | 6 | 2.50 | 2.55 | 0.98 | 100% | 97% | 100% | 15% | yes | 0% / 0% | Passed |
| 31-40 | mid gear | 6 | 2.50 | 2.54 | 0.98 | 100% | 98% | 100% | 27% | yes | 0% / 0% | Passed |
| 31-40 | best gear | 6 | 2.50 | 2.51 | 0.99 | 100% | 99% | 100% | 38% | yes | 0% / 0% | Passed |
| 41-53 | no gear | 6 | 2.33 | 2.35 | 0.99 | 100% | 99% | 98% | 42% | yes | 0% / 0% | Passed |
| 41-53 | mid gear | 6 | 2.33 | 2.34 | 1.00 | 100% | 100% | 100% | 73% | yes | 0% / 0% | Passed |
| 41-53 | best gear | 6 | 2.33 | 2.34 | 1.00 | 100% | 100% | 100% | 91% | yes | 0% / 0% | Passed |

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
| 1-10 | mid gear, 3 enhancements | 501 | 8.6 | 34903 | 2880 | 23.0 | 56389 | 1.62 | - | Passed |
| 1-10 | best gear | 699 | 6.8 | 62111 | 2880 | 15.1 | 85866 | 1.38 | - | Passed |
| 1-10 | best gear, 1 enhancement | 694 | 7.4 | 56259 | 2880 | 16.9 | 76694 | 1.36 | - | Passed |
| 1-10 | best gear, 2 enhancements | 665 | 8.0 | 50077 | 2880 | 19.1 | 67676 | 1.35 | - | Passed |
| 1-10 | best gear, 3 enhancements | 646 | 8.2 | 47369 | 2880 | 21.5 | 60326 | 1.27 | - | Passed |
| 11-20 | no gear | 562 | 8.0 | 42142 | 1726 | 17.6 | 44038 | 1.04 | - | Passed |
| 11-20 | no gear, 1 enhancement | 174 | 6.7 | 15665 | 1727 | 21.0 | 37040 | 2.36 | - | Passed |
| 11-20 | no gear, 2 enhancements | 171 | 7.1 | 14541 | 1727 | 24.5 | 31739 | 2.18 | - | Passed |
| 11-20 | no gear, 3 enhancements | 170 | 7.5 | 13664 | 1729 | 27.9 | 27837 | 2.04 | - | Passed |
| 11-20 | mid gear | 675 | 12.1 | 33472 | 1740 | 15.6 | 50149 | 1.50 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 643 | 12.8 | 30058 | 1740 | 17.6 | 44417 | 1.48 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 608 | 13.9 | 26253 | 1740 | 20.2 | 38667 | 1.47 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 547 | 15.5 | 21187 | 1740 | 23.1 | 33896 | 1.60 | - | Passed |
| 11-20 | best gear | 779 | 14.4 | 32454 | 1740 | 14.6 | 53526 | 1.65 | - | Passed |
| 11-20 | best gear, 1 enhancement | 749 | 14.9 | 30162 | 1740 | 16.9 | 46267 | 1.53 | - | Passed |
| 11-20 | best gear, 2 enhancements | 743 | 16.2 | 27482 | 1740 | 19.1 | 40955 | 1.49 | - | Passed |
| 11-20 | best gear, 3 enhancements | 703 | 15.9 | 26489 | 1740 | 21.4 | 36612 | 1.38 | - | Passed |
| 21-30 | no gear | 635 | 11.0 | 34628 | 1496 | 18.9 | 35618 | 1.03 | - | Passed |
| 21-30 | no gear, 1 enhancement | 612 | 11.7 | 31429 | 1497 | 21.3 | 31678 | 1.01 | - | Passed |
| 21-30 | no gear, 2 enhancements | 584 | 13.0 | 26972 | 1496 | 24.2 | 27841 | 1.03 | - | Passed |
| 21-30 | no gear, 3 enhancements | 559 | 14.8 | 22666 | 1496 | 27.1 | 24808 | 1.09 | - | Passed |
| 21-30 | mid gear | 700 | 17.9 | 23471 | 1500 | 15.4 | 43936 | 1.87 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 678 | 17.5 | 23268 | 1500 | 17.5 | 38597 | 1.66 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 661 | 18.0 | 22054 | 1500 | 19.7 | 34313 | 1.56 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 630 | 18.0 | 20982 | 1500 | 22.2 | 30364 | 1.45 | - | Passed |
| 21-30 | best gear | 776 | 22.4 | 20795 | 1500 | 14.7 | 46065 | 2.22 | - | Passed |
| 21-30 | best gear, 1 enhancement | 757 | 21.5 | 21107 | 1500 | 16.5 | 40971 | 1.94 | - | Passed |
| 21-30 | best gear, 2 enhancements | 742 | 22.4 | 19894 | 1500 | 18.7 | 36016 | 1.81 | - | Passed |
| 21-30 | best gear, 3 enhancements | 716 | 21.7 | 19836 | 1500 | 20.6 | 32762 | 1.65 | - | Passed |
| 31-40 | no gear | 677 | 13.6 | 29783 | 1317 | 19.1 | 31071 | 1.04 | - | Passed |
| 31-40 | no gear, 1 enhancement | 645 | 14.9 | 26066 | 1320 | 21.0 | 28335 | 1.09 | - | Passed |
| 31-40 | no gear, 2 enhancements | 616 | 14.5 | 25455 | 1320 | 22.5 | 26402 | 1.04 | - | Passed |
| 31-40 | no gear, 3 enhancements | 596 | 15.5 | 22998 | 1320 | 24.0 | 24800 | 1.08 | - | Passed |
| 31-40 | mid gear | 748 | 19.8 | 22630 | 1320 | 15.2 | 39126 | 1.73 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 736 | 20.5 | 21573 | 1320 | 16.5 | 35924 | 1.67 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 719 | 20.5 | 21086 | 1320 | 18.1 | 32845 | 1.56 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 684 | 19.6 | 20927 | 1320 | 19.9 | 29919 | 1.43 | - | Passed |
| 31-40 | best gear | 822 | 24.7 | 19971 | 1320 | 14.9 | 39915 | 2.00 | - | Passed |
| 31-40 | best gear, 1 enhancement | 817 | 25.3 | 19347 | 1320 | 15.9 | 37351 | 1.93 | - | Passed |
| 31-40 | best gear, 2 enhancements | 802 | 25.7 | 18713 | 1320 | 17.4 | 34079 | 1.82 | - | Passed |
| 31-40 | best gear, 3 enhancements | 774 | 24.3 | 19081 | 1320 | 18.5 | 32181 | 1.69 | - | Passed |
| 41-53 | no gear | 842 | 19.1 | 26466 | 1189 | 17.8 | 30062 | 1.14 | - | Passed |
| 41-53 | no gear, 1 enhancement | 770 | 17.7 | 26166 | 1198 | 20.2 | 26744 | 1.02 | - | Passed |
| 41-53 | no gear, 2 enhancements | 705 | 17.7 | 23862 | 1200 | 21.7 | 24835 | 1.04 | - | Passed |
| 41-53 | no gear, 3 enhancements | 613 | 17.9 | 20584 | 1200 | 23.5 | 23021 | 1.12 | - | Passed |
| 41-53 | mid gear | 1036 | 26.7 | 23298 | 1199 | 15.6 | 34702 | 1.49 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 974 | 25.3 | 23068 | 1200 | 16.4 | 32852 | 1.42 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 888 | 23.2 | 22982 | 1200 | 17.6 | 30618 | 1.33 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 808 | 22.3 | 21722 | 1200 | 19.0 | 28429 | 1.31 | - | Passed |
| 41-53 | best gear | 1142 | 28.9 | 23727 | 1200 | 15.0 | 36114 | 1.52 | - | Passed |
| 41-53 | best gear, 1 enhancement | 1111 | 28.7 | 23211 | 1200 | 16.1 | 33572 | 1.45 | - | Passed |
| 41-53 | best gear, 2 enhancements | 1034 | 27.0 | 22927 | 1200 | 17.1 | 31579 | 1.38 | - | Passed |
| 41-53 | best gear, 3 enhancements | 985 | 26.2 | 22581 | 1200 | 18.2 | 29589 | 1.31 | - | Passed |

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
| 51-53 | 1 | 6 | 60 | 10% | 68% | 14% | 0.09 | 100% | 99.8% | 1.00 (+/- 0.00) | 10.77 | >= 1.00 | Passed |
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
| 51-53 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 97% |
| 51-53 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 100% |
| 51-53 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 100% |

### Representative enhancements: for each recipe kind the real item at the best level the recipe accepts (the old window refuses one more than 5 levels above the recipe) whose penalty is the lower median of that level

| Recipe kind | Enhancement | Level | Progress penalty | Items of that kind and level (min / median / max penalty) | Recipes |
| --- | --- | --- | --- | --- | --- |
| Structure | Structure Enhancement - Structure Bonus V (`sten_sb5`) | 45 (tier V) | 60 | 2 items: 60 / 70.0 / 80 | 18 |

## Limitations

- The recipes are an even sample of each band (or all of them, as the inputs say), weighted equally. A recipe the sample skipped can behave differently.
- The adaptive player is a bounded-depth expectimax over the previews with a generic heuristic. It is a floor on skilled play, not an optimal player; a deeper search or a scenario heuristic can only raise it.
- The fixed-rotation solver is a beam search with local search, not a proof. A fixed rotation it did not find would narrow the margin; raise the solver options to look harder. The rotation is scored on separate attempts, so its figure is not flattered by the search.
- Gate 5 compares at matched rank, gear and effort (both sides play to maximize quality) and requires the new minigame's average quality to be at least the legacy player's (within `MaterialQualityShortfallPoints`); it does not compare at an equal quality target. Enhancements are not committed in either model.
- Gate 6 uses estimated seconds per action. The legacy comparator is the old minigame's lookahead player, one step of policy improvement over the rotation players report, not an optimal player. Its enhanced cases play the cohort's recipes that have an enhancement slot (an even sample of each band, so they can differ from the plain cases' recipes) with gate 7's representative enhancement in every slot; XP does not depend on the enhancements except through the quality and failures they cause.
- Gate 7 plays every enhanceable recipe of the at-level bands with the representative real enhancement item of its kind (the best level the recipe accepts, the lower-median progress penalty of that level), the same item in every filled slot, and counts expected transferred enhancements per craft attempt, a craft that makes no item counting none. Enhancement items differ in penalty, so the figure holds for the median item. Today's side draws no transfer rolls: a craft that is made adds slots x quality chance / 100. How a scenario maps the penalty onto its progress meter is that minigame's own rule, and the client has not measured it.
- Gathering, scavenging, fishing and research are not covered here; their cohorts implement `IXpRateCohort` and their economy checks belong to their own gates.
- No attributes, food, status effects, blueprints, first-craft bonus or quick craft are modeled, and the client was not run.
- Prime Feedstock is the only chance in the Fabricator, so the adaptive player's edge over a fixed rotation comes from it and from the recipe's state, not from luck at the actions themselves.

## Notes from the author (added by hand; a rerun of the command regenerates everything above and drops this section)

### What was tuned, and why

Starting values and the values the gates settled on (`FabricatorRules`, `FabricatorParameterProfile`):

| Value | First guess | Now | Why |
| --- | --- | --- | --- |
| Run (progress per Run, share of the recipe's work, on the head's own sub-assembly) | 13% | 28% | Length (gate 4): at 13% a plain craft took 20-27 actions. At 28% and 46 quality points a Precision Pass it takes about 15 (best gear) to 19 (no gear) |
| Precision Pass (quality points) | 40 | 46 | Gate 3 at levels 51-53 (rank 50 against a level 53 recipe loses 15%): 42 left two passes short of full quality there (37% of crafts reached maximum); 46 keeps two passes enough at rank 50 with the best gear (97%+) |
| Service restores | 30 | 40 | Fewer Services are needed at the high wear of high levels |
| Wear per Run or Precision Pass | 2-7 | 2-9 | Wear is the budget: harder materials wear the heads faster. 2.0 + 1.15 per point of component hardness |
| Prime Feedstock | 12% a head, +50%, waits 2 actions | the same | The plan's +50% stays; the window of 2 actions lets a Retool catch it |
| Machine Condition | 100 | 100 for a plain piece, in proportion to the work for an enhanced one | Gate 6 with enhancements: three enhancements at levels 11-20 left a no-gear crafter 4 times short of today's XP per hour because most crafts broke. The machine now lasts the longer run |
| XP multiplier | none | 2.4 / 1.45 / 1.25 / 1.1 / 1.0 for levels 1-10 / 11-20 / 21-30 / 31-40 / 41 and up | Gate 6: the least that passes in each band at every gear tier and enhancement count (the old window takes 5-9 actions at levels 1-10) |

The adaptive player needed a leaf heuristic of the Fabricator's own (`FabricatorLeafHeuristic`): the generic one values a full progress meter as a made item, but the Fabricator's item is made by Assemble, so the generic player dithered (retooled back and forth) instead of finishing. The quality meter shows the weighted average of the three sub-assemblies and Assemble applies the cap (weakest + 20); with the cap on the meter itself, the quality sat on a plateau while the weakest sub-assembly was refined and the depth-3 player stopped early.

### Gate 1 (no fixed rotation) cannot pass with the plan's design

The best fixed rotation matches or beats the adaptive player in every band and gear tier (margins -5.0 to 0.0 against the required +10). Earlier runs with other starting values (2 recipes a band, 20 runs, seed 1) found the same:

| Values | Margins |
| --- | --- |
| Machine Condition 100, Prime Feedstock +100% at 15% a head, Run 20%, Precision 40 | -4.6 to 0.0 |
| Machine Condition 80 (a tight budget), Prime Feedstock +100% at 15% a head | -11.2 to 0.0 |
| Final values (this report) | -5.0 to 0.0 |

Why: the only chance in the Fabricator is Prime Feedstock, which only ever helps, and the solver finds the best rotation for each recipe, so a fixed rotation that ignores every announcement still reaches full quality (its average is 100.0 in 14 of 15 cells). Making the budget tighter or the prime stronger does not open a margin, because a rotation sized for the no-luck case wastes nothing, and a tight budget only makes the depth-limited player fail more often. A margin of 10 points needs a risk the player can only manage by reacting to it. The plan's rule that random events only create opportunities rules that out.

Proposed change for the owner (not made): give one action a visible, player-managed downside, as the Circuit's overload does. For example, below a recipe-specific Machine Condition line each Run and Precision Pass has a visible chance, rising per point under the line and shown beside the action, to slip: the Precision Pass adds no quality and the Run wears the machine twice. A fixed rotation then has to Service early (CP and an action) or accept slips, while the adaptive player reads the line. The alternative is to retire gate 1 for a minigame whose only chance is a telegraphed opportunity.

### Gate 7: expected enhancements transferred, the Fabricator against today

At rank 50 in the best gear and at level, thresholds in force 50% / 85% / 85% (unchanged). The Fabricator transfers every enhancement it is given (quality is 100% in nearly every craft), against today's chance roll:

| Band | Slots | Today | Fabricator | Ratio |
| --- | --- | --- | --- | --- |
| 41-45 | 1 / 2 / 3 | 0.94 / 1.75 / 2.45 | 1.00 / 2.00 / 3.00 | 1.07 / 1.14 / 1.22 |
| 46-50 | 1 / 2 / 3 | 0.82 / 1.45 / 1.91 | 1.00 / 2.00 / 3.00 | 1.22 / 1.38 / 1.57 |
| 51-53 | 1 / 2 / 3 | 0.09 / 0.14 / 0.12 | 1.00 / 2.00 / 3.00 | 10.8 / 14.0 / 24.4 |

The parity thresholds the gate computes for the quality the Fabricator makes are 100% / 100% / 100% (95% / 100% / 100% in steps of 5), far stricter than the thresholds in force, because nearly every Fabricator craft reaches 100%. The shared thresholds are not changed here.
