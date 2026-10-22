# Crafting revamp: balance gates of the Circuit minigame

- Command: `SWLOR.CraftSim gates --scenario circuit --runs 100 --recipes-per-band 6 --seed 1 --out design/testing/crafting-revamp/circuit-gates.md --json design/testing/crafting-revamp/circuit-gates.json`
- Seed 1; 100 runs per case.

## Inputs

- Scenario: Circuit (`circuit`), Engineering recipes, minigame Circuit.
- Quality: the quality meter's share of its maximum when the item is made, in points (0-100). A broken or abandoned piece counts 0, so every average includes the crafts that failed.
- Crafter: the rank the scenario's cohort gives each recipe (by default the recipe's level, at most 50, so a crafter works at their own level), with no food or status bonuses and no perks unless a gate names a style. Gear levels are the plan's none, middle and best tier of the skill's crafting gear:
  - no gear (Control 0, Craftsmanship 0, CP 0)
  - mid gear (Control 13, Craftsmanship 12, CP 20)
  - best gear (Control 29, Craftsmanship 30, CP 37)
- Recipes: 1-10: 6 of 307; 11-20: 6 of 367; 21-30: 6 of 359; 31-40: 6 of 370; 41-53: 6 of 383 recipes played.
- Adaptive player: depth 3, forecast samples per result 2, budget 4000 engine copies per decision, 0.5% of value lost per extra action, heuristic DefaultLeafHeuristic. Each attempt's dice come from its own stream of the seed.
- Fixed-rotation solver: beam 8, cycles up to 3, 8 adaptive lines as seeds, up to 40 actions, 64 training attempts per local-search round and 150 selection attempts for each of 12 finalists, local search from the best 3 candidate(s) for up to 3 round(s) each. The winner is chosen on separate attempts and then scored on a third set (hold-out) with 100 attempts.
- Legacy comparator (gates 5 and 6): the old minigame's lookahead player (`SWLOR.CraftSim baseline`), replayed here with the same seed on the same recipes, ranks and gear.
- XP multiplier: the new minigame's base XP is scaled by recipe level (gate 6): 1-10 x2.29, 11-20 x1.52, 21-30 x1.13, 31-40 x1.00, 41-53 x1.00; the legacy loop's is not.
- Time: 8.0 s per action in the new window and 6.0 s in the old one, plus 0.0 s per craft in both. These are estimates; the client evidence replaces them.
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
| No fixed rotation | Failed | A fixed rotation comes within 10 points of adaptive play in 15 case(s): 1-10 / no gear; 1-10 / mid gear; 1-10 / best gear; 11-20 / no gear; 11-20 / mid gear; 11-20 / best gear; 21-30 / no gear; 21-30 / mid gear; 21-30 / best gear; 31-40 / no gear; 31-40 / mid gear; 31-40 / best gear; 41-53 / no gear; 41-53 / mid gear; 41-53 / best gear (smallest margin -10.4 in 1-10 / mid gear). |
| Style balance | NotApplicable | The scenario offers no style configurations yet, so there is nothing to compare. |
| Perks optional | Passed | An unperked rank-50 crafter in best gear reaches maximum quality on at least 50% of crafts in every at-level band (lowest 95%). |
| Length | Passed | Every band and gear tier takes 10-20 actions (item craft at level) on average. |
| Economy parity | Passed | Component stacks per item are within 10% of the legacy baseline, at no less quality, in every band and gear tier. |
| XP rate | Passed | Item crafts: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.00x in 21-30 / no gear). |
| Enhancement parity | Passed | At rank 50 in best gear, the thresholds in force (50% / 85% / 85%) transfer at least 1.00 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 1.08 times today's). Thresholds that meet parity in every band for this quality: 100% / 100% / 100%. |

## No fixed rotation

### Average quality: adaptive play against the best fixed rotation (quality points; a broken piece counts 0)

| Band | Gear | Recipes played | Adaptive avg quality | Best fixed avg quality | Margin | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 47.4 | 51.7 | -4.3 | >= 10 | Failed |
| 1-10 | mid gear | 6 | 88.1 | 98.4 | -10.4 | >= 10 | Failed |
| 1-10 | best gear | 6 | 91.4 | 100.0 | -8.6 | >= 10 | Failed |
| 11-20 | no gear | 6 | 76.5 | 82.9 | -6.4 | >= 10 | Failed |
| 11-20 | mid gear | 6 | 96.2 | 100.0 | -3.8 | >= 10 | Failed |
| 11-20 | best gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | Failed |
| 21-30 | no gear | 6 | 98.3 | 99.3 | -0.9 | >= 10 | Failed |
| 21-30 | mid gear | 6 | 99.2 | 100.0 | -0.8 | >= 10 | Failed |
| 21-30 | best gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | Failed |
| 31-40 | no gear | 6 | 99.9 | 100.0 | -0.1 | >= 10 | Failed |
| 31-40 | mid gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | Failed |
| 31-40 | best gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | Failed |
| 41-53 | no gear | 6 | 99.7 | 100.0 | -0.3 | >= 10 | Failed |
| 41-53 | mid gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | Failed |
| 41-53 | best gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | Failed |

### The best fixed rotation found for the first recipe played in each band and gear tier (average quality in points)

| Band | Gear | Recipe | Best fixed rotation | Training | Selection | Hold-out | Adaptive on this recipe |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | wen_cft_smth1 (level 3) | wire x5 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire | 37.5 | 37.5 | 37.5 | 33.4 |
| 1-10 | mid gear | wen_cft_smth1 (level 3) | tune x5 > discharge_calibration > tune x2 > wire > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire | 100.0 | 100.0 | 100.0 | 90.6 |
| 1-10 | best gear | wen_cft_smth1 (level 3) | tune x5 > discharge_calibration > tune x4 > wire > discharge_assembly > wire x3 > tune > discharge_assembly > wire | 100.0 | 100.0 | 100.0 | 88.6 |
| 11-20 | no gear | dhbr002 (level 13) | wire > tune x3 > discharge_calibration > wire x4 > discharge_calibration > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire | 71.0 | 69.4 | 69.6 | 60.0 |
| 11-20 | mid gear | dhbr002 (level 13) | tune > discharge_assembly > tune x4 > discharge_calibration > tune > patch > tune > discharge_calibration > wire x4 > discharge_calibration > wire x4 > discharge_calibration > wire x2 | 100.0 | 100.0 | 100.0 | 98.1 |
| 11-20 | best gear | dhbr002 (level 13) | tune x4 > discharge_calibration > tune x4 > patch > discharge_calibration > wire x8 | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | wen_cft_smth3 (level 23) | boost > tune x4 > discharge_calibration > wire x7 > discharge_calibration > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire | 95.7 | 94.9 | 95.7 | 90.8 |
| 21-30 | mid gear | wen_cft_smth3 (level 23) | boost > tune x4 > discharge_calibration > tune x4 > wire > tune > wire > discharge_assembly > wire > tune > wire x3 | 100.0 | 100.0 | 100.0 | 96.1 |
| 21-30 | best gear | wen_cft_smth3 (level 23) | boost > tune x4 > discharge_calibration > tune x4 > wire x3 > discharge_assembly > wire x2 | 100.0 | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | aen_cft_fab4 (level 33) | tune x6 > discharge_calibration > tune x5 > wire > discharge_assembly > wire x6 > discharge_calibration > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | aen_cft_fab4 (level 33) | boost > tune x3 > discharge_calibration > tune x6 > discharge_assembly > tune > wire x5 > discharge_assembly | 100.0 | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | aen_cft_fab4 (level 33) | boost > tune x3 > discharge_calibration > tune x5 > wire > discharge_assembly > tune > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | dhbr005 (level 43) | boost > tune x4 > discharge_calibration > tune x5 > wire x2 > discharge_assembly > tune > wire x4 | 100.0 | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | dhbr005 (level 43) | boost > tune x4 > discharge_calibration > tune x4 > wire x3 > discharge_assembly > wire x3 | 100.0 | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | dhbr005 (level 43) | boost > tune x4 > discharge_calibration > boost > tune x2 > wire x2 > discharge_assembly > wire x3 | 100.0 | 100.0 | 100.0 | 100.0 |

## Perks optional

### Rank 50, best gear, no perks: adaptive attempts that reach maximum quality, by recipe band

| Band | Recipes played | Attempts | Reach maximum quality | Make the item | Avg quality of items made | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 6 | 600 | 95% | 100% | 100% | >= 50% | Passed |
| 46-50 | 6 | 600 | 99% | 100% | 100% | >= 50% | Passed |
| 51-53 | 6 | 600 | 96% | 100% | 100% | >= 50% | Passed |

## Length

### Actions per attempt of the adaptive player at level (item craft at level)

| Band | Gear | Recipes played | Mean actions per attempt | Shortest recipe | Longest recipe | Stalled attempts | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 14.5 | 12.1 | 16.2 | 0 | 10-20 | Passed |
| 1-10 | mid gear | 6 | 17.7 | 16.2 | 18.6 | 0 | 10-20 | Passed |
| 1-10 | best gear | 6 | 15.8 | 14.1 | 17.4 | 0 | 10-20 | Passed |
| 11-20 | no gear | 6 | 16.6 | 14.9 | 18.6 | 0 | 10-20 | Passed |
| 11-20 | mid gear | 6 | 17.1 | 14.6 | 19.3 | 0 | 10-20 | Passed |
| 11-20 | best gear | 6 | 15.0 | 13.2 | 16.6 | 0 | 10-20 | Passed |
| 21-30 | no gear | 6 | 18.0 | 16.4 | 19.2 | 0 | 10-20 | Passed |
| 21-30 | mid gear | 6 | 16.3 | 14.4 | 17.4 | 0 | 10-20 | Passed |
| 21-30 | best gear | 6 | 14.2 | 12.8 | 15.0 | 0 | 10-20 | Passed |
| 31-40 | no gear | 6 | 17.9 | 16.5 | 19.8 | 0 | 10-20 | Passed |
| 31-40 | mid gear | 6 | 15.7 | 14.5 | 17.0 | 0 | 10-20 | Passed |
| 31-40 | best gear | 6 | 14.0 | 13.0 | 15.3 | 0 | 10-20 | Passed |
| 41-53 | no gear | 6 | 17.5 | 15.6 | 19.6 | 0 | 10-20 | Passed |
| 41-53 | mid gear | 6 | 15.5 | 13.9 | 17.0 | 0 | 10-20 | Passed |
| 41-53 | best gear | 6 | 13.7 | 12.8 | 14.7 | 0 | 10-20 | Passed |

## Economy parity

### Component stacks consumed per item made, failures included (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 2.35 | 2.40 | 0.98 | 99% | 96% | 47% | 1% | yes | 0% / 0% | Passed |
| 1-10 | mid gear | 6 | 2.37 | 2.39 | 0.99 | 98% | 97% | 88% | 10% | yes | 0% / 0% | Passed |
| 1-10 | best gear | 6 | 2.43 | 2.37 | 1.02 | 94% | 98% | 91% | 24% | yes | 0% / 0% | Passed |
| 11-20 | no gear | 6 | 2.01 | 2.10 | 0.96 | 99% | 93% | 78% | 3% | yes | 0% / 0% | Passed |
| 11-20 | mid gear | 6 | 2.04 | 2.03 | 1.00 | 98% | 97% | 96% | 12% | yes | 0% / 0% | Passed |
| 11-20 | best gear | 6 | 2.00 | 2.01 | 0.99 | 100% | 99% | 100% | 28% | yes | 0% / 0% | Passed |
| 21-30 | no gear | 6 | 2.51 | 2.56 | 0.98 | 100% | 97% | 98% | 8% | yes | 0% / 0% | Passed |
| 21-30 | mid gear | 6 | 2.51 | 2.52 | 0.99 | 100% | 99% | 99% | 15% | yes | 0% / 0% | Passed |
| 21-30 | best gear | 6 | 2.50 | 2.52 | 0.99 | 100% | 99% | 100% | 29% | yes | 0% / 0% | Passed |
| 31-40 | no gear | 6 | 2.50 | 2.54 | 0.98 | 100% | 98% | 100% | 23% | yes | 0% / 0% | Passed |
| 31-40 | mid gear | 6 | 2.50 | 2.52 | 0.99 | 100% | 99% | 100% | 36% | yes | 0% / 0% | Passed |
| 31-40 | best gear | 6 | 2.50 | 2.52 | 0.99 | 100% | 99% | 100% | 49% | yes | 0% / 0% | Passed |
| 41-53 | no gear | 6 | 2.50 | 2.41 | 1.04 | 100% | 82% | 100% | 35% | yes | 0% / 17% | Passed |
| 41-53 | mid gear | 6 | 2.50 | 2.72 | 0.92 | 100% | 87% | 100% | 55% | yes | 0% / 0% | Passed |
| 41-53 | best gear | 6 | 2.50 | 2.58 | 0.97 | 100% | 94% | 100% | 75% | yes | 0% / 0% | Passed |

## XP rate

### Item crafts: XP per hour against the legacy loop (6.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per craft | Legacy actions per craft | Legacy XP per hour | New XP per craft | New actions per craft | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 586 | 5.6 | 62435 | 2015 | 14.5 | 62648 | 1.00 | - | Passed |
| 1-10 | no gear, 1 enhancement | 424 | 6.5 | 39443 | 1696 | 18.2 | 41926 | 1.06 | - | Passed |
| 1-10 | no gear, 2 enhancements | 185 | 6.0 | 18520 | 1669 | 18.1 | 41508 | 2.24 | - | Passed |
| 1-10 | no gear, 3 enhancements | 90 | 6.0 | 9000 | 1684 | 18.3 | 41385 | 4.60 | - | Passed |
| 1-10 | mid gear | 645 | 7.6 | 51038 | 2554 | 17.7 | 65072 | 1.27 | - | Passed |
| 1-10 | mid gear, 1 enhancement | 612 | 7.8 | 46899 | 2023 | 18.9 | 48265 | 1.03 | - | Passed |
| 1-10 | mid gear, 2 enhancements | 589 | 8.4 | 41861 | 2050 | 19.0 | 48585 | 1.16 | - | Passed |
| 1-10 | mid gear, 3 enhancements | 535 | 8.8 | 36480 | 2005 | 19.0 | 47460 | 1.30 | - | Passed |
| 1-10 | best gear | 733 | 8.8 | 49866 | 2563 | 15.8 | 73060 | 1.47 | - | Passed |
| 1-10 | best gear, 1 enhancement | 722 | 9.1 | 47421 | 2298 | 18.3 | 56533 | 1.19 | - | Passed |
| 1-10 | best gear, 2 enhancements | 716 | 9.6 | 44932 | 2324 | 18.2 | 57363 | 1.28 | - | Passed |
| 1-10 | best gear, 3 enhancements | 689 | 10.0 | 41440 | 2365 | 18.3 | 58200 | 1.40 | - | Passed |
| 11-20 | no gear | 577 | 8.0 | 43466 | 1604 | 16.6 | 43506 | 1.00 | - | Passed |
| 11-20 | no gear, 1 enhancement | 390 | 8.7 | 27067 | 1655 | 19.2 | 38829 | 1.43 | - | Passed |
| 11-20 | no gear, 2 enhancements | 262 | 7.3 | 21603 | 1636 | 19.1 | 38483 | 1.78 | - | Passed |
| 11-20 | no gear, 3 enhancements | 251 | 8.0 | 18827 | 1642 | 19.2 | 38539 | 2.05 | - | Passed |
| 11-20 | mid gear | 656 | 10.4 | 38034 | 1775 | 17.1 | 46583 | 1.22 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 639 | 11.6 | 32975 | 1748 | 18.7 | 42025 | 1.27 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 621 | 12.8 | 29212 | 1760 | 18.8 | 42027 | 1.44 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 568 | 13.9 | 24621 | 1746 | 18.8 | 41808 | 1.70 | - | Passed |
| 11-20 | best gear | 766 | 14.6 | 31378 | 1824 | 15.0 | 54772 | 1.75 | - | Passed |
| 11-20 | best gear, 1 enhancement | 753 | 15.3 | 29481 | 1821 | 16.4 | 50118 | 1.70 | - | Passed |
| 11-20 | best gear, 2 enhancements | 739 | 16.1 | 27545 | 1824 | 16.3 | 50241 | 1.82 | - | Passed |
| 11-20 | best gear, 3 enhancements | 718 | 16.6 | 25960 | 1821 | 16.4 | 50097 | 1.93 | - | Passed |
| 21-30 | no gear | 630 | 11.3 | 33568 | 1342 | 18.0 | 33588 | 1.00 | - | Passed |
| 21-30 | no gear, 1 enhancement | 618 | 12.3 | 30162 | 1333 | 19.1 | 31382 | 1.04 | - | Passed |
| 21-30 | no gear, 2 enhancements | 605 | 13.1 | 27717 | 1334 | 19.3 | 31105 | 1.12 | - | Passed |
| 21-30 | no gear, 3 enhancements | 575 | 13.8 | 24961 | 1326 | 19.3 | 30943 | 1.24 | - | Passed |
| 21-30 | mid gear | 684 | 16.8 | 24373 | 1349 | 16.3 | 37230 | 1.53 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 676 | 17.4 | 23323 | 1350 | 16.8 | 36256 | 1.55 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 657 | 17.7 | 22315 | 1351 | 16.8 | 36170 | 1.62 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 655 | 17.9 | 21904 | 1351 | 17.0 | 35772 | 1.63 | - | Passed |
| 21-30 | best gear | 766 | 22.5 | 20425 | 1356 | 14.2 | 42825 | 2.10 | - | Passed |
| 21-30 | best gear, 1 enhancement | 761 | 22.7 | 20084 | 1356 | 14.6 | 41846 | 2.08 | - | Passed |
| 21-30 | best gear, 2 enhancements | 752 | 23.3 | 19334 | 1356 | 14.6 | 41770 | 2.16 | - | Passed |
| 21-30 | best gear, 3 enhancements | 741 | 23.1 | 19223 | 1356 | 14.6 | 41718 | 2.17 | - | Passed |
| 31-40 | no gear | 723 | 14.5 | 29893 | 1199 | 17.9 | 30198 | 1.01 | - | Passed |
| 31-40 | no gear, 1 enhancement | 680 | 14.5 | 28109 | 1196 | 18.7 | 28804 | 1.02 | - | Passed |
| 31-40 | no gear, 2 enhancements | 621 | 15.2 | 24578 | 1195 | 18.8 | 28656 | 1.17 | - | Passed |
| 31-40 | no gear, 3 enhancements | 588 | 16.0 | 22065 | 1199 | 18.9 | 28619 | 1.30 | - | Passed |
| 31-40 | mid gear | 808 | 18.9 | 25624 | 1200 | 15.7 | 34368 | 1.34 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 784 | 19.5 | 24177 | 1200 | 16.5 | 32743 | 1.35 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 734 | 19.1 | 23010 | 1200 | 16.4 | 32996 | 1.43 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 683 | 18.4 | 22225 | 1200 | 16.5 | 32677 | 1.47 | - | Passed |
| 31-40 | best gear | 888 | 24.6 | 21677 | 1200 | 14.0 | 38648 | 1.78 | - | Passed |
| 31-40 | best gear, 1 enhancement | 881 | 25.3 | 20886 | 1200 | 14.4 | 37534 | 1.80 | - | Passed |
| 31-40 | best gear, 2 enhancements | 854 | 25.3 | 20213 | 1200 | 14.2 | 37987 | 1.88 | - | Passed |
| 31-40 | best gear, 3 enhancements | 815 | 23.8 | 20539 | 1200 | 14.4 | 37374 | 1.82 | - | Passed |
| 41-53 | no gear | 723 | 17.5 | 24786 | 1248 | 17.5 | 32159 | 1.30 | - | Passed |
| 41-53 | no gear, 1 enhancement | 633 | 16.0 | 23699 | 1250 | 18.0 | 31187 | 1.32 | - | Passed |
| 41-53 | no gear, 2 enhancements | 537 | 16.6 | 19443 | 1250 | 18.1 | 31066 | 1.60 | - | Passed |
| 41-53 | no gear, 3 enhancements | 455 | 17.4 | 15739 | 1250 | 18.2 | 30944 | 1.97 | - | Passed |
| 41-53 | mid gear | 871 | 23.9 | 21886 | 1250 | 15.5 | 36298 | 1.66 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 777 | 22.1 | 21084 | 1250 | 15.8 | 35492 | 1.68 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 679 | 20.0 | 20347 | 1250 | 16.0 | 35112 | 1.73 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 587 | 19.3 | 18273 | 1250 | 16.2 | 34786 | 1.90 | - | Passed |
| 41-53 | best gear | 1042 | 28.5 | 21964 | 1250 | 13.7 | 41018 | 1.87 | - | Passed |
| 41-53 | best gear, 1 enhancement | 961 | 27.5 | 20940 | 1250 | 13.9 | 40584 | 1.94 | - | Passed |
| 41-53 | best gear, 2 enhancements | 880 | 25.9 | 20354 | 1250 | 14.0 | 40298 | 1.98 | - | Passed |
| 41-53 | best gear, 3 enhancements | 775 | 23.3 | 19959 | 1250 | 14.1 | 39908 | 2.00 | - | Passed |

## Enhancement parity

### Rank 50, best gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 85%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 6 | 60-100 | 34% | 99% | 94% | 0.92 | 100% | 100.0% | 1.00 (+/- 0.00) | 1.08 | >= 1.00 | Passed |
| 41-45 | 2 | 6 | 60-100 | 69% | 99% | 86% | 1.69 | 100% | 100.0% | 2.00 (+/- 0.00) | 1.18 | >= 1.00 | Passed |
| 41-45 | 3 | 6 | 60-100 | 103% | 99% | 77% | 2.28 | 100% | 100.0% | 3.00 (+/- 0.00) | 1.31 | >= 1.00 | Passed |
| 46-50 | 1 | 6 | 60-100 | 39% | 99% | 81% | 0.79 | 100% | 100.0% | 1.00 (+/- 0.00) | 1.26 | >= 1.00 | Passed |
| 46-50 | 2 | 6 | 60-100 | 79% | 99% | 67% | 1.31 | 100% | 100.0% | 2.00 (+/- 0.00) | 1.52 | >= 1.00 | Passed |
| 46-50 | 3 | 6 | 60-100 | 118% | 99% | 57% | 1.67 | 100% | 100.0% | 3.00 (+/- 0.00) | 1.80 | >= 1.00 | Passed |
| 51-53 | 1 | 6 | 60-70 | 7% | 32% | 6% | 0.02 | 100% | 100.0% | 1.00 (+/- 0.00) | 57.75 | >= 1.00 | Passed |
| 51-53 | 2 | 6 | 60-70 | 15% | 22% | 5% | 0.02 | 100% | 100.0% | 2.00 (+/- 0.00) | 99.17 | >= 1.00 | Passed |
| 51-53 | 3 | 6 | 60-70 | 22% | 5% | 5% | 0.01 | 100% | 100.0% | 3.00 (+/- 0.00) | 434.78 | >= 1.00 | Passed |

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
| 41-45 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 99% |
| 41-45 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 99% |
| 41-45 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 100% |
| 46-50 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 99% |
| 46-50 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 46-50 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 99% |
| 51-53 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 51-53 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 51-53 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |

### Representative enhancements: for each recipe kind the real item at the best level the recipe accepts (the old window refuses one more than 5 levels above the recipe) whose penalty is the lower median of that level

| Recipe kind | Enhancement | Level | Progress penalty | Items of that kind and level (min / median / max penalty) | Recipes |
| --- | --- | --- | --- | --- | --- |
| Weapon | Improved Agrinium V (`imp_agrinium_5`) | 45 (tier V) | 70 | 33 items: 50 / 70.0 / 80 | 1 |
| Armor | Armor Enhancement - Force Attack V (`aen_frcatk5`) | 45 (tier V) | 60 | 50 items: 50 / 60.0 / 150 | 4 |
| Starship | Starship Enhancement - Shield Recharge Rate V (`sen_shrech5`) | 45 (tier V) | 60 | 24 items: 50 / 65.0 / 80 | 2 |
| Module | Module Enhancement - Module Bonus V (`men_mod5`) | 45 (tier V) | 60 | 2 items: 60 / 70.0 / 80 | 8 |
| Droid | Droid Enhancement - Resistance - Ice II (`de_res_ice2`) | 50 (tier VI) | 100 | 8 items: 100 / 100.0 / 100 | 3 |

## Limitations

- The recipes are an even sample of each band (or all of them, as the inputs say), weighted equally. A recipe the sample skipped can behave differently.
- The adaptive player is a bounded-depth expectimax over the previews with a generic heuristic. It is a floor on skilled play, not an optimal player; a deeper search or a scenario heuristic can only raise it.
- The fixed-rotation solver is a beam search with local search, not a proof. A fixed rotation it did not find would narrow the margin; raise the solver options to look harder. The rotation is scored on separate attempts, so its figure is not flattered by the search.
- Gate 5 compares at matched rank, gear and effort (both sides play to maximize quality) and requires the new minigame's average quality to be at least the legacy player's (within `MaterialQualityShortfallPoints`); it does not compare at an equal quality target. Enhancements are not committed in either model.
- Gate 6 uses estimated seconds per action. The legacy comparator is the old minigame's lookahead player, one step of policy improvement over the rotation players report, not an optimal player. Its enhanced cases play the cohort's recipes that have an enhancement slot (an even sample of each band, so they can differ from the plain cases' recipes) with gate 7's representative enhancement in every slot; XP does not depend on the enhancements except through the quality and failures they cause.
- Gate 7 plays every enhanceable recipe of the at-level bands with the representative real enhancement item of its kind (the best level the recipe accepts, the lower-median progress penalty of that level), the same item in every filled slot, and counts expected transferred enhancements per craft attempt, a craft that makes no item counting none. Enhancement items differ in penalty, so the figure holds for the median item. Today's side draws no transfer rolls: a craft that is made adds slots x quality chance / 100. How a scenario maps the penalty onto its progress meter is that minigame's own rule, and the client has not measured it.
- Gathering, scavenging, fishing and research are not covered here; their cohorts implement `IXpRateCohort` and their economy checks belong to their own gates.
- No attributes, food, status effects, blueprints, first-craft bonus or quick craft are modeled, and the client was not run.
- An enhancement's difficulty is the old window's progress penalty added to the Assembly the piece needs, capped at 30% of the Assembly the recipe itself needs; the Circuit's Wire, Tune and Discharge outputs do not change with it.
- The Circuit has no styles or perks yet, so the style-balance gate is not applicable and the perks-optional gate plays the unperked crafter.
- Seconds per action is an estimate of 8 s until the client evidence measures it.
