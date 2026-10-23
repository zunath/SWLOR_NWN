# Crafting revamp: balance gates of the Kitchen minigame

- Command: `SWLOR.CraftSim gates --scenario kitchen --runs 100 --seed 1 --out design/testing/crafting-revamp/kitchen-gates.md --json C:/Users/Zunath/AppData/Local/Temp/claude-crafting-revamp-6-cooking-kitchen-tuning/final.json`
- Seed 1; 100 runs per case.

## Inputs

- Scenario: Kitchen (`kitchen`), Agriculture recipes, minigame Kitchen.
- Quality: the quality meter's share of its maximum when the item is made, in points (0-100). A broken or abandoned piece counts 0, so every average includes the crafts that failed.
- Crafter: the rank the scenario's cohort gives each recipe (by default the recipe's level, at most 50, so a crafter works at their own level), with no food or status bonuses and no perks unless a gate names a style. Gear levels are the plan's none, middle and best tier of the skill's crafting gear:
  - no gear (Control 0, Craftsmanship 0, CP 0)
  - mid gear (Control 13, Craftsmanship 12, CP 20)
  - best gear (Control 29, Craftsmanship 30, CP 37)
- Recipes: 1-10: 6 of 58; 11-20: 6 of 59; 21-30: 6 of 65; 31-40: 6 of 61; 41-53: 6 of 76 recipes played.
- Adaptive player: depth 3, forecast samples per result 1, budget 1500 engine copies per decision, 0.5% of value lost per extra action, heuristic KitchenLeafHeuristic. Each attempt's dice come from its own stream of the seed.
- Fixed-rotation solver: beam 8, cycles up to 3, 8 adaptive lines as seeds, up to 40 actions, 64 training attempts per local-search round and 150 selection attempts for each of 12 finalists, local search from the best 3 candidate(s) for up to 3 round(s) each. Gate 1 searches one sequence for each band and gear tier across the band's sampled recipes (every recipe weighing the same; an action a recipe's engine cannot take is skipped there), and also each recipe's own best sequence as a reported metric. The winner is chosen on separate attempts and then scored on a third set (hold-out) with 100 attempts per recipe.
- Legacy comparator (gates 5 and 6): the old minigame's lookahead player (`SWLOR.CraftSim baseline`), replayed here with the same seed on the same recipes, ranks and gear.
- Time: 8.0 s per action in the new window and 6.0 s in the old one, plus 0.0 s per craft in both. These are estimates; the client evidence replaces them.
- XP multiplier on the base XP of every craft, failures included, as the game server applies it (`CraftMinigameXp`): 1-10 2.15x, 11-20 1.15x, 21-30 1.10x, 31-40 1.20x, 41-53 1.05x. The new side's XP below includes it; the legacy loop's does not.
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
| No fixed rotation | Passed | Adaptive play beats the best single fixed rotation by at least 10 points in every band and gear tier (smallest margin 19.4 in 11-20 / no gear). The best rotation for each recipe alone, a reported metric, is 3.4 to 22.3 points behind adaptive play. |
| Style balance | NotApplicable | The scenario offers no style configurations yet, so there is nothing to compare. |
| Perks optional | Passed | An unperked rank-50 crafter in best gear reaches maximum quality on at least 50% of crafts in every at-level band (lowest 59%). |
| Length | Passed | Every band and gear tier takes 10-20 actions (item craft at level) on average. |
| Economy parity | Failed | Outside 10% of the legacy baseline (above it only, where its success rate is below 90%), or below its quality: 41-53 / no gear (ratio 1.21). |
| XP rate | Passed | Item crafts: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.01x in 11-20 / no gear). |
| Enhancement parity | Passed | At rank 50 in best gear, the thresholds in force (50% / 85% / 85%) transfer at least 1.00 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 1.02 times today's). Thresholds that meet parity in every band for this quality: 57% / 93% / 100%. |

## No fixed rotation

### Average quality: adaptive play against the best single fixed rotation of each band and gear tier (quality points; a broken piece counts 0)

| Band | Gear | Recipes played | Adaptive avg quality | Best single rotation avg quality | Margin | Required | Best rotation per recipe avg quality (reported) | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 85.7 | 66.0 | 19.6 | >= 10 | 82.2 | Passed |
| 1-10 | mid gear | 6 | 94.1 | 63.5 | 30.6 | >= 10 | 87.1 | Passed |
| 1-10 | best gear | 6 | 97.6 | 72.9 | 24.6 | >= 10 | 92.3 | Passed |
| 11-20 | no gear | 6 | 84.8 | 65.3 | 19.4 | >= 10 | 77.1 | Passed |
| 11-20 | mid gear | 6 | 90.9 | 63.9 | 27.0 | >= 10 | 81.5 | Passed |
| 11-20 | best gear | 6 | 94.5 | 67.5 | 27.0 | >= 10 | 90.4 | Passed |
| 21-30 | no gear | 6 | 89.6 | 56.7 | 32.9 | >= 10 | 69.4 | Passed |
| 21-30 | mid gear | 6 | 93.0 | 60.0 | 33.0 | >= 10 | 84.1 | Passed |
| 21-30 | best gear | 6 | 95.9 | 67.5 | 28.5 | >= 10 | 89.8 | Passed |
| 31-40 | no gear | 6 | 83.1 | 44.5 | 38.6 | >= 10 | 61.5 | Passed |
| 31-40 | mid gear | 6 | 84.6 | 58.1 | 26.5 | >= 10 | 64.9 | Passed |
| 31-40 | best gear | 6 | 92.8 | 61.2 | 31.6 | >= 10 | 70.5 | Passed |
| 41-53 | no gear | 6 | 84.1 | 54.7 | 29.4 | >= 10 | 63.0 | Passed |
| 41-53 | mid gear | 6 | 87.1 | 62.5 | 24.6 | >= 10 | 70.2 | Passed |
| 41-53 | best gear | 6 | 93.6 | 67.8 | 25.7 | >= 10 | 79.4 | Passed |

### The single rotation found for each band and gear tier, searched across the band's sampled recipes (average quality in points)

| Band | Gear | Best single rotation for the band | Training | Selection | Hold-out | Adaptive on these recipes |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | simmer x3 > heat.medium > simmer x2 > heat.medium > add.ingredient.1 > simmer > heat.low > simmer > heat.low > add.ingredient.1 > add.ingredient.0 > simmer > cover > simmer x4 > heat.low > simmer x3 > heat.low > simmer x5 > heat.medium > simmer | 65.0 | 65.4 | 66.0 | 85.7 |
| 1-10 | mid gear | heat.medium > add.ingredient.1 > simmer > stir > add.ingredient.1 > stir x2 > add.ingredient.0 > stir > simmer x5 > heat.low > simmer x4 | 65.3 | 62.2 | 63.5 | 94.1 |
| 1-10 | best gear | heat.medium > add.ingredient.1 > simmer > heat.medium > simmer x2 > add.ingredient.1 > heat.low > simmer > add.ingredient.0 > heat.low > add.ingredient.1 > simmer > stir x3 > cover > simmer x7 | 72.7 | 72.3 | 72.9 | 97.6 |
| 11-20 | no gear | simmer > add.ingredient.0 x2 > stir > simmer > heat.medium > simmer x5 > heat.low > cover > simmer x2 > add.ingredient.1 > simmer > heat.low > cover > add.ingredient.1 > simmer x2 > add.salt > simmer | 65.2 | 67.4 | 65.3 | 84.8 |
| 11-20 | mid gear | add.ingredient.1 > simmer x3 > add.ingredient.0 > simmer > stir > add.ingredient.0 > simmer > add.ingredient.1 > stir > simmer x2 > heat.low > simmer x2 > heat.low > simmer x4 | 69.0 | 64.6 | 63.9 | 90.9 |
| 11-20 | best gear | add.ingredient.1 > stir x2 > simmer > stir > add.ingredient.0 > stir > add.ingredient.0 > stir > add.ingredient.1 > stir x2 > simmer > cover > simmer x3 > heat.low > simmer x5 | 71.0 | 70.5 | 67.5 | 94.5 |
| 21-30 | no gear | add.ingredient.0 x2 > simmer > add.ingredient.2 > heat.medium > simmer x3 > add.ingredient.0 > simmer > add.ingredient.0 > stir x3 > add.ingredient.0 x2 > simmer > add.ingredient.0 x2 > simmer > add.ingredient.0 x2 > simmer > heat.low > cover > add.salt > simmer > add.ingredient.2 > simmer > add.ingredient.0 x2 > simmer > add.ingredient.0 x2 > simmer > add.ingredient.0 x2 > simmer > add.ingredient.0 x2 | 54.6 | 56.4 | 56.7 | 89.6 |
| 21-30 | mid gear | add.ingredient.0 x2 > stir > add.ingredient.2 > simmer > stir > add.ingredient.2 > simmer x2 > add.ingredient.0 > simmer > add.ingredient.2 > simmer x3 > stir > heat.low > add.salt > heat.low > simmer > add.ingredient.2 > add.ingredient.0 > simmer > add.ingredient.2 > add.ingredient.0 > simmer > add.ingredient.2 > add.ingredient.0 > simmer > add.ingredient.2 > add.ingredient.0 > simmer > add.ingredient.2 > add.ingredient.0 > simmer > add.ingredient.2 > add.ingredient.0 > simmer > add.ingredient.2 > add.ingredient.0 | 58.6 | 60.3 | 60.0 | 93.0 |
| 21-30 | best gear | heat.medium > simmer x3 > heat.low x2 > add.ingredient.0 > simmer > add.ingredient.2 > simmer > add.ingredient.0 > simmer x2 > add.ingredient.0 > simmer > add.ingredient.0 > stir > simmer > stir x6 > simmer x3 > add.ingredient.0 > simmer x2 | 67.1 | 68.1 | 67.5 | 95.9 |
| 31-40 | no gear | add.ingredient.2 > add.ingredient.0 x2 > stir > add.ingredient.0 > simmer x2 > stir > simmer > stir > simmer > add.ingredient.0 > simmer > cover > simmer > stir > simmer x2 > heat.low > stir > add.ingredient.0 > simmer x3 > stir > add.ingredient.0 > simmer > add.spice > add.ingredient.0 > simmer > stir > add.ingredient.0 > simmer > stir > add.ingredient.0 > simmer > stir > add.ingredient.0 | 44.2 | 43.7 | 44.5 | 83.1 |
| 31-40 | mid gear | add.ingredient.1 > simmer > add.salt > simmer x2 > add.ingredient.2 > heat.medium > simmer > add.salt > simmer > heat.low > simmer x2 > add.ingredient.2 > simmer > add.ingredient.1 > heat.low > add.ingredient.0 > simmer > add.ingredient.1 > cover > add.ingredient.1 > stir > simmer > stir > simmer > stir x2 > simmer x2 > stir > simmer x3 > heat.low > simmer x2 > heat.medium > simmer | 54.7 | 58.3 | 58.1 | 84.6 |
| 31-40 | best gear | cover > stir x2 > cover > simmer > add.ingredient.2 > cover > simmer x3 > heat.low x2 > add.ingredient.2 x2 > add.ingredient.1 > add.ingredient.0 > simmer > add.ingredient.1 > add.ingredient.2 > add.salt > simmer x2 > stir x5 > add.ingredient.1 > simmer x2 > stir > simmer > add.salt > stir x2 > simmer x2 | 62.4 | 60.5 | 61.2 | 92.8 |
| 41-53 | no gear | heat.medium > simmer > stir > simmer x2 > stir x2 > add.ingredient.3 > stir > simmer > add.ingredient.2 > simmer > add.ingredient.0 x2 > add.ingredient.3 > stir x5 > simmer x3 > heat.medium > simmer > add.ingredient.1 > add.salt > simmer > heat.low > add.ingredient.0 > heat.medium > simmer > heat.low > add.salt x2 > simmer > add.salt > simmer | 56.2 | 56.0 | 54.7 | 84.1 |
| 41-53 | mid gear | heat.low > add.ingredient.2 > heat.low > add.salt > heat.low > stir > heat.low > add.ingredient.0 > cover > simmer x2 > add.ingredient.0 > add.salt > add.ingredient.3 > stir x4 > simmer x4 > stir > heat.medium > simmer x6 > add.ingredient.1 x2 > simmer > add.salt > simmer x4 | 64.0 | 61.9 | 62.5 | 87.1 |
| 41-53 | best gear | add.ingredient.0 > simmer > add.ingredient.3 > add.ingredient.2 > simmer > add.ingredient.3 > heat.medium > add.ingredient.0 > add.ingredient.3 > stir > simmer > stir x2 > simmer x2 > stir > simmer x2 > heat.low > simmer > add.ingredient.1 x2 > add.salt > add.ingredient.1 > simmer > heat.low > add.salt > heat.low > simmer > add.salt x2 > simmer | 67.2 | 66.9 | 67.8 | 93.6 |

### Reported metric, not gated: the best fixed rotation for each recipe alone, and what the band's single rotation scores on the same recipe (average quality in points)

| Band | Gear | Recipe | Best rotation for this recipe alone (reported) | Its hold-out | The single rotation on this recipe | Adaptive on this recipe |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | cen_dur1 (level 1) | add.ingredient.0 > simmer x2 > add.ingredient.0 > heat.medium > simmer > add.ingredient.0 > simmer x3 > add.ingredient.0 > heat.low x2 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer | 78.5 | 88.0 | 92.7 |
| 1-10 | no gear | cen_fp1 (level 3) | add.salt > simmer x4 > add.salt > simmer > add.salt > simmer x3 > heat.low > add.salt > simmer > heat.low > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt | 84.4 | 71.6 | 79.0 |
| 1-10 | no gear | pf_sweetfood_1 (level 5) | simmer x2 > add.ingredient.0 x2 > simmer > add.ingredient.0 x2 > simmer > add.sugar > simmer x2 > add.ingredient.0 > add.spice > add.ingredient.0 > add.spice > simmer > add.sugar > simmer > heat.low > add.sugar > add.ingredient.1 > add.sugar > simmer x2 > heat.low > simmer x3 > heat.low > simmer > heat.medium > simmer x2 | 68.9 | 23.4 | 80.2 |
| 1-10 | no gear | cen_res_psn1 (level 6) | add.ingredient.1 > add.ingredient.0 > add.ingredient.1 > simmer > add.ingredient.1 > heat.medium > simmer x5 > heat.low > simmer > add.salt > simmer | 84.3 | 87.0 | 85.1 |
| 1-10 | no gear | cen_stmr1 (level 7) | simmer x4 > heat.low > add.spice x2 > add.ingredient.0 > add.spice > simmer > add.ingredient.0 > simmer x2 > add.spice > simmer > add.sugar > simmer > add.spice > add.sugar > heat.medium > simmer x4 > add.sugar > heat.low > simmer x5 | 80.6 | 42.2 | 87.0 |
| 1-10 | no gear | cooked_bibikibo (level 9) | simmer x5 > heat.medium > add.salt > simmer > add.salt > simmer > heat.low > add.ingredient.1 > simmer > add.salt > simmer x3 > add.salt > simmer > cover > simmer > heat.low > simmer x4 > cover > simmer | 96.8 | 83.8 | 89.9 |
| 1-10 | mid gear | cen_dur1 (level 1) | add.ingredient.0 > simmer > stir > simmer > stir > simmer x3 > heat.low > simmer > heat.low > simmer x4 > cover > add.ingredient.1 > simmer | 93.7 | 87.1 | 97.9 |
| 1-10 | mid gear | cen_fp1 (level 3) | heat.medium > simmer > stir > simmer x3 > add.salt > stir > simmer > add.salt > simmer x2 > heat.low x2 > simmer x4 > add.salt > simmer > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 | 87.9 | 75.8 | 93.0 |
| 1-10 | mid gear | pf_sweetfood_1 (level 5) | cover > add.sugar x2 > add.ingredient.0 > stir > add.sugar > simmer x7 > heat.low > add.ingredient.1 > cover > simmer > add.spice > heat.low > cover > add.ingredient.1 > add.sugar > simmer > add.sugar x2 > simmer | 83.8 | 23.7 | 88.9 |
| 1-10 | mid gear | cen_res_psn1 (level 6) | stir > add.spice > cover > stir > add.ingredient.1 > stir > add.ingredient.0 > heat.medium > stir x2 > simmer x5 > heat.low > add.spice > simmer x3 | 81.9 | 83.8 | 94.8 |
| 1-10 | mid gear | cen_stmr1 (level 7) | cover > simmer > stir > simmer > add.sugar > simmer > stir > add.spice x3 > add.ingredient.0 > add.spice > add.ingredient.0 > add.spice > stir > simmer > heat.medium > simmer x2 > heat.low > simmer | 82.1 | 41.5 | 93.8 |
| 1-10 | mid gear | cooked_bibikibo (level 9) | simmer > add.salt > stir > cover > add.salt > heat.medium > cover > simmer x2 > add.salt > stir > simmer x2 > add.salt > simmer > stir > simmer > heat.low > simmer > add.ingredient.1 > simmer > heat.low > add.ingredient.1 > add.salt > simmer x3 > heat.low > simmer x2 > heat.medium > simmer | 92.9 | 68.9 | 96.4 |
| 1-10 | best gear | cen_dur1 (level 1) | stir > add.ingredient.0 > simmer > add.ingredient.0 > simmer x2 > stir > simmer > heat.low x2 > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.spice > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer | 88.8 | 95.1 | 100.0 |
| 1-10 | best gear | cen_fp1 (level 3) | heat.medium > add.ingredient.1 > add.salt > stir > simmer x4 > heat.low > add.salt > simmer x4 > stir x3 > simmer > stir > simmer x5 > heat.low > add.salt > simmer x3 | 95.2 | 88.2 | 97.3 |
| 1-10 | best gear | pf_sweetfood_1 (level 5) | simmer x2 > add.salt > heat.low > add.ingredient.1 > simmer > add.ingredient.0 x2 > add.sugar > simmer x3 > add.spice > stir x2 > simmer > stir > simmer x7 | 90.2 | 22.2 | 94.4 |
| 1-10 | best gear | cen_res_psn1 (level 6) | add.ingredient.0 > add.ingredient.1 x2 > stir > add.salt > simmer > stir > simmer x2 > stir > simmer x3 > heat.low > simmer x5 | 87.9 | 91.9 | 97.9 |
| 1-10 | best gear | cen_stmr1 (level 7) | heat.low > add.ingredient.1 > simmer > stir > add.ingredient.0 > cover > stir > add.ingredient.0 > simmer > add.ingredient.0 > add.spice > heat.medium > simmer > heat.medium > simmer > heat.low > simmer x4 > stir > simmer > heat.low > simmer > cover > heat.low > simmer x6 > add.spice > simmer x2 | 92.0 | 44.8 | 97.2 |
| 1-10 | best gear | cooked_bibikibo (level 9) | cover > simmer > stir > simmer x3 > heat.low > simmer > add.ingredient.1 > simmer x2 > heat.low x2 > simmer > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt | 99.8 | 95.5 | 98.6 |
| 11-20 | no gear | cen_dur2 (level 11) | add.salt > simmer x3 > add.ingredient.1 > simmer x2 > heat.medium > simmer > add.salt > simmer > heat.medium > add.salt > simmer > heat.low > simmer > add.salt > heat.low > simmer x5 > heat.low > simmer | 70.8 | 34.4 | 76.4 |
| 11-20 | no gear | pf_cookedfish_2 (level 13) | heat.high > sear x2 > add.salt x2 > sear > heat.low x2 > add.salt > add.ingredient.0 > cover > add.spice > cover > heat.low > add.salt > add.ingredient.1 > simmer > add.salt > simmer x4 > add.salt > heat.medium > simmer x4 > heat.low > simmer x3 | 74.5 | 60.6 | 79.4 |
| 11-20 | no gear | cooked_mackerel (level 15) | add.ingredient.0 x2 > simmer > add.spice > add.salt > simmer x6 > heat.low > simmer x7 > cover > add.ingredient.1 > add.salt > simmer | 83.6 | 73.5 | 93.4 |
| 11-20 | no gear | pf_fattyfish_2 (level 16) | add.salt > simmer > add.salt x2 > heat.medium > simmer > add.salt > simmer x2 > add.salt > stir > add.ingredient.1 > add.salt > heat.medium > add.salt > simmer x4 | 68.8 | 79.5 | 88.3 |
| 11-20 | no gear | cen_stmr2 (level 17) | simmer x2 > heat.low > cover > add.spice > simmer > add.ingredient.1 > add.ingredient.0 x2 > add.spice > simmer > cover > simmer x5 > heat.low > simmer > heat.low > simmer > add.salt > simmer x3 > heat.medium > simmer | 83.7 | 72.3 | 94.1 |
| 11-20 | no gear | cooked_siredon (level 19) | stir > add.salt x2 > cover > simmer > add.salt > stir > simmer x6 > heat.low > simmer > add.ingredient.1 > add.salt > heat.low > add.salt > simmer x2 | 81.0 | 71.8 | 77.1 |
| 11-20 | mid gear | cen_dur2 (level 11) | simmer x2 > add.salt > heat.medium > add.salt > simmer x2 > heat.low > stir > add.salt > simmer x3 > heat.low > add.salt > cover > simmer > heat.medium > add.salt > simmer x3 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 | 73.2 | 56.5 | 87.1 |
| 11-20 | mid gear | pf_cookedfish_2 (level 13) | add.ingredient.0 > simmer > add.salt > heat.medium > add.salt > cover > add.salt > cover > simmer > add.salt > simmer x5 > heat.low > simmer > add.ingredient.1 > simmer > add.spice > simmer > add.salt > simmer > cover > simmer > add.salt > simmer x2 > heat.medium > simmer | 82.6 | 60.3 | 78.4 |
| 11-20 | mid gear | cooked_mackerel (level 15) | add.ingredient.0 > add.salt > add.ingredient.0 > stir > add.spice > stir > simmer x8 > heat.low > add.ingredient.1 > add.salt > simmer > add.salt > simmer x3 | 87.0 | 77.9 | 99.4 |
| 11-20 | mid gear | pf_fattyfish_2 (level 16) | add.ingredient.0 x2 > add.sugar > stir > add.salt > stir > simmer x7 > heat.low > simmer x5 > cover > add.ingredient.1 > add.salt > simmer | 76.7 | 61.7 | 93.8 |
| 11-20 | mid gear | cen_stmr2 (level 17) | add.ingredient.0 x2 > add.ingredient.1 > add.spice x2 > add.sugar > simmer x2 > stir > simmer x2 > stir > simmer x3 > heat.low > simmer x6 | 78.1 | 79.7 | 98.0 |
| 11-20 | mid gear | cooked_siredon (level 19) | add.salt x2 > cover > simmer > heat.medium > simmer x4 > heat.low > add.ingredient.1 > simmer > heat.low > simmer > add.ingredient.1 > simmer x4 > cover > add.salt > simmer x2 > cover > add.salt > cover > simmer > add.salt > simmer | 91.3 | 47.0 | 88.7 |
| 11-20 | best gear | cen_dur2 (level 11) | add.ingredient.0 > simmer x3 > stir > simmer > heat.low > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer | 83.7 | 57.9 | 93.5 |
| 11-20 | best gear | pf_cookedfish_2 (level 13) | heat.medium > simmer x2 > heat.low > add.salt > simmer > heat.low > simmer > heat.low > simmer > add.salt > heat.low > add.ingredient.1 > simmer > heat.low > add.ingredient.0 > simmer x3 > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer | 87.5 | 62.0 | 92.0 |
| 11-20 | best gear | cooked_mackerel (level 15) | simmer > add.sugar > stir > simmer x3 > heat.low > add.salt > add.ingredient.1 > add.salt > cover > add.ingredient.0 > heat.low > cover > simmer > add.spice > cover > simmer > add.salt > stir x2 > simmer x3 > stir > simmer x3 > heat.low > simmer x4 > heat.medium > simmer | 96.3 | 80.4 | 99.3 |
| 11-20 | best gear | pf_fattyfish_2 (level 16) | add.ingredient.0 > simmer > heat.medium x2 > simmer > heat.low > add.salt > add.sugar > simmer x2 > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer | 92.0 | 69.6 | 93.8 |
| 11-20 | best gear | cen_stmr2 (level 17) | simmer > add.sugar > simmer > heat.low > simmer x3 > add.ingredient.1 > cover > add.ingredient.0 > add.sugar x2 > add.ingredient.1 > add.salt > simmer > stir > simmer > stir > simmer > stir > simmer x2 > heat.low > simmer x5 > cover > simmer x2 > cover > heat.medium > simmer | 96.5 | 81.8 | 99.3 |
| 11-20 | best gear | cooked_siredon (level 19) | add.salt > stir > add.ingredient.0 > simmer > add.salt > simmer x3 > heat.low x2 > simmer > add.ingredient.1 > simmer x3 > add.salt > add.ingredient.1 > simmer | 86.6 | 53.4 | 89.4 |
| 21-30 | no gear | cen_dur3 (level 21) | simmer x4 > add.salt > add.ingredient.1 > add.salt > heat.medium > simmer x3 > heat.low > simmer x5 | 76.4 | 63.6 | 91.9 |
| 21-30 | no gear | ramen_noodles (level 24) | simmer > stir > simmer > add.ingredient.0 > simmer > add.ingredient.0 x2 > simmer > heat.medium > simmer > heat.low > simmer > add.salt > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 | 78.6 | 72.5 | 92.2 |
| 21-30 | no gear | cen_res_fir3 (level 25) | simmer > stir > simmer > add.salt > simmer > add.salt > simmer x2 > stir > simmer x2 > add.salt > add.ingredient.1 > simmer > heat.low > simmer x2 > add.salt > simmer > add.spice > stir > simmer x2 > stir > simmer > stir > simmer x8 | 82.7 | 76.4 | 94.0 |
| 21-30 | no gear | cen_res_mob3 (level 26) | add.ingredient.1 > heat.low > add.ingredient.0 > add.salt > cover > simmer > add.salt > cover > add.salt > simmer > stir > simmer > stir > simmer > stir > simmer > stir > heat.medium > simmer > add.salt > simmer x3 | 81.4 | 68.6 | 92.8 |
| 21-30 | no gear | cen_stmr3 (level 27) | add.ingredient.1 > add.ingredient.0 x2 > add.salt > add.ingredient.1 > stir > simmer > add.sugar > simmer > stir x2 > simmer x2 > stir > simmer x6 | 51.2 | 22.6 | 90.2 |
| 21-30 | no gear | pf_sourfruit_3 (level 29) | simmer > stir > heat.medium > add.sugar > simmer x2 > add.ingredient.0 > add.ingredient.1 > simmer > heat.medium > simmer > stir > add.ingredient.1 > add.ingredient.0 > simmer > stir > add.ingredient.0 > simmer > add.ingredient.1 > add.ingredient.0 > simmer > heat.low > add.spice > add.salt > add.ingredient.1 > add.ingredient.0 > simmer > add.ingredient.1 > add.ingredient.0 > simmer > add.ingredient.1 > add.ingredient.0 > simmer > add.ingredient.1 > add.ingredient.0 > simmer > heat.medium > add.ingredient.0 > simmer > add.ingredient.1 | 46.2 | 36.5 | 76.4 |
| 21-30 | mid gear | cen_dur3 (level 21) | add.ingredient.0 > stir x2 > add.salt > simmer > add.salt > stir > simmer > stir > simmer x2 > stir > simmer x3 > heat.low > simmer x4 > heat.low > simmer x4 | 86.4 | 61.4 | 93.2 |
| 21-30 | mid gear | ramen_noodles (level 24) | add.ingredient.0 > simmer x5 > add.ingredient.0 > heat.low > simmer > add.ingredient.2 > cover > add.ingredient.0 x3 > simmer > stir x3 > simmer x2 > heat.low > add.sugar > cover > add.salt > simmer x2 > add.salt > add.ingredient.0 > simmer x3 > heat.low > simmer x2 > add.salt > simmer x3 | 90.8 | 74.9 | 94.3 |
| 21-30 | mid gear | cen_res_fir3 (level 25) | heat.medium > add.ingredient.0 > simmer > heat.low > add.ingredient.1 x2 > heat.low > simmer > add.spice > stir > add.ingredient.1 > simmer > stir x4 > simmer > heat.medium > simmer x4 > heat.low > cover > simmer | 90.9 | 83.0 | 98.2 |
| 21-30 | mid gear | cen_res_mob3 (level 26) | simmer > heat.medium > simmer > add.salt > heat.medium > add.salt > simmer > heat.medium > simmer > add.salt > heat.medium > simmer > add.salt > heat.medium x2 > add.salt > simmer > heat.medium > add.salt > simmer > heat.medium > simmer > heat.low > add.salt > heat.low > add.salt > simmer > heat.medium > add.salt > simmer > heat.medium > add.salt > simmer > heat.medium > add.salt > simmer > heat.medium > add.salt > simmer | 85.4 | 77.9 | 94.5 |
| 21-30 | mid gear | cen_stmr3 (level 27) | simmer > add.salt > add.ingredient.0 x2 > simmer x4 > heat.low > add.ingredient.1 > simmer > heat.low > simmer > add.salt > add.sugar > simmer > stir x3 > simmer x2 > stir > simmer x6 > heat.low > cover > simmer | 73.5 | 22.4 | 95.1 |
| 21-30 | mid gear | pf_sourfruit_3 (level 29) | simmer > stir > simmer x4 > heat.low x2 > add.spice > add.sugar > heat.low > add.salt > simmer > add.ingredient.1 > add.ingredient.0 > simmer x2 > add.ingredient.0 > simmer > add.ingredient.1 > add.ingredient.0 > simmer > add.ingredient.1 > add.ingredient.0 > simmer > add.ingredient.1 > add.ingredient.0 > simmer > add.ingredient.1 > add.ingredient.0 > simmer > add.ingredient.1 > add.ingredient.0 > simmer > add.ingredient.1 > add.ingredient.0 > simmer > add.ingredient.1 > add.ingredient.0 > simmer | 77.7 | 40.5 | 82.6 |
| 21-30 | best gear | cen_dur3 (level 21) | add.ingredient.0 > simmer x2 > add.salt > stir > simmer x2 > heat.low > simmer x2 > stir > simmer > add.salt > stir x2 > simmer x2 > stir x2 > simmer x6 | 95.8 | 70.9 | 97.4 |
| 21-30 | best gear | ramen_noodles (level 24) | add.ingredient.0 > simmer > stir > simmer > heat.low > add.salt > add.ingredient.0 x3 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer | 97.0 | 94.0 | 99.0 |
| 21-30 | best gear | cen_res_fir3 (level 25) | add.ingredient.1 > stir > add.ingredient.0 > simmer x2 > heat.low x2 > add.ingredient.1 > simmer > stir x2 > simmer > stir x4 > simmer x4 > heat.low > simmer | 95.8 | 89.6 | 98.4 |
| 21-30 | best gear | cen_res_mob3 (level 26) | add.salt > heat.low > simmer > heat.low > add.ingredient.0 > simmer > heat.low > simmer x2 > add.ingredient.0 > add.salt x2 > cover > stir > simmer x2 > stir x3 > simmer x4 > heat.low > simmer > cover > heat.medium > simmer | 87.0 | 82.6 | 95.5 |
| 21-30 | best gear | cen_stmr3 (level 27) | heat.medium > add.ingredient.1 > simmer x2 > add.ingredient.0 > heat.low > add.ingredient.0 > simmer x2 > add.ingredient.1 > add.salt > stir > simmer x2 > stir x7 > simmer x7 | 87.3 | 2.7 | 97.8 |
| 21-30 | best gear | pf_sourfruit_3 (level 29) | simmer > add.salt > simmer x2 > add.spice > heat.low > add.ingredient.1 > add.sugar > add.ingredient.0 > simmer x2 > add.ingredient.0 > simmer x2 > add.ingredient.0 > simmer x2 > add.ingredient.0 > simmer x2 > add.ingredient.0 > simmer x2 > add.ingredient.0 > simmer x2 > add.ingredient.0 > simmer x2 > add.ingredient.0 > simmer x2 > add.ingredient.0 > simmer x2 > add.ingredient.0 > simmer x2 > add.ingredient.0 > simmer | 75.9 | 65.0 | 87.5 |
| 31-40 | no gear | cen_dur4 (level 31) | heat.medium > add.ingredient.0 > heat.low > add.ingredient.1 > simmer > add.ingredient.1 > stir x4 > simmer x9 > heat.low > simmer x2 > cover > heat.medium > simmer | 77.4 | 65.4 | 86.2 |
| 31-40 | no gear | pf_cookedfish_4 (level 33) | add.ingredient.0 > cover > add.ingredient.0 > simmer > add.ingredient.0 > add.salt > cover > simmer > add.spice > add.salt > stir > cover > stir > simmer x4 > stir > simmer x2 > heat.low > simmer x2 > cover > add.salt > simmer > add.ingredient.1 > heat.low > add.salt > simmer | 53.1 | 41.8 | 76.6 |
| 31-40 | no gear | tusken_mball (level 35) | simmer > add.ingredient.1 > add.salt > cover > add.ingredient.0 > heat.low > cover > simmer x2 > add.salt x2 > stir > simmer x5 > heat.low > simmer x4 > add.salt > simmer > cover > heat.medium > simmer | 65.3 | 53.4 | 86.3 |
| 31-40 | no gear | sliced_cod (level 36) | heat.medium > add.ingredient.0 x2 > add.spice > simmer > add.salt > heat.medium > simmer x5 > heat.low > simmer > heat.low > simmer > add.ingredient.1 > cover > simmer > add.ingredient.1 > simmer | 69.0 | 24.8 | 79.6 |
| 31-40 | no gear | cen_res_tra4 (level 37) | add.ingredient.0 > simmer x2 > stir > heat.medium > simmer > add.ingredient.0 x2 > simmer > add.ingredient.0 x2 > simmer > add.spice > add.ingredient.0 > simmer > stir x2 > add.ingredient.0 x2 > simmer > add.ingredient.0 x2 > simmer > heat.low > add.ingredient.0 > simmer > add.sugar > add.ingredient.0 > simmer > add.ingredient.0 x2 > simmer > add.ingredient.0 x2 > simmer > add.ingredient.0 x2 > simmer > add.ingredient.0 x2 | 58.0 | 56.4 | 90.0 |
| 31-40 | no gear | dem_sandwich (level 39) | simmer x2 > stir > simmer x2 > stir > add.salt > add.ingredient.1 > simmer x2 > add.salt > simmer > heat.low > add.ingredient.2 > cover > add.ingredient.0 > cover > add.salt > add.spice > cover > simmer x3 > stir x3 > simmer > stir > add.salt > simmer x5 > heat.low > simmer > heat.medium > simmer | 46.1 | 25.3 | 80.2 |
| 31-40 | mid gear | cen_dur4 (level 31) | add.ingredient.0 > heat.low > add.ingredient.0 > simmer x3 > cover > simmer x3 > cover > simmer x5 > add.ingredient.0 > simmer > heat.medium > add.ingredient.0 > simmer > heat.medium > add.ingredient.0 > simmer > heat.medium > add.ingredient.0 > simmer > heat.medium > add.ingredient.0 > simmer > heat.medium > add.ingredient.0 > simmer > heat.medium > add.ingredient.0 > simmer > heat.medium > add.ingredient.0 > simmer > heat.medium | 77.3 | 60.1 | 90.3 |
| 31-40 | mid gear | pf_cookedfish_4 (level 33) | simmer > add.salt x2 > add.ingredient.0 > simmer x3 > heat.low > add.ingredient.1 > simmer x5 > add.ingredient.0 > simmer x2 > add.ingredient.0 > simmer x2 > add.ingredient.0 > simmer x2 > add.ingredient.0 > simmer x2 > add.ingredient.0 > simmer x2 > add.ingredient.0 > simmer x2 > add.ingredient.0 > simmer x2 > add.ingredient.0 > simmer x2 > add.ingredient.0 > simmer | 50.7 | 48.8 | 79.6 |
| 31-40 | mid gear | tusken_mball (level 35) | simmer > cover > add.ingredient.0 > simmer x2 > heat.low > add.ingredient.2 > add.ingredient.1 > simmer > heat.low > add.ingredient.0 > simmer > cover > add.ingredient.1 > stir > simmer > stir > simmer > stir x2 > simmer x2 > stir > simmer x3 > heat.low > simmer x2 > heat.medium > simmer | 69.2 | 64.4 | 84.6 |
| 31-40 | mid gear | sliced_cod (level 36) | add.spice > simmer x3 > add.salt > simmer x2 > add.salt > simmer x2 > add.ingredient.1 > heat.low > cover > simmer > add.spice > simmer x2 > add.salt > heat.low > cover > simmer > add.salt > simmer > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer | 79.9 | 62.8 | 84.4 |
| 31-40 | mid gear | cen_res_tra4 (level 37) | simmer x3 > add.ingredient.0 > simmer x2 > add.ingredient.0 > heat.low > cover > simmer x3 > add.spice > stir x2 > simmer > stir > add.sugar > simmer > stir > simmer x2 > stir > add.salt > simmer x2 > cover > simmer x2 | 77.6 | 78.8 | 93.8 |
| 31-40 | mid gear | dem_sandwich (level 39) | simmer > heat.low > add.ingredient.0 > add.ingredient.2 > add.ingredient.0 > add.ingredient.2 > stir x2 > simmer > heat.medium > stir x2 > simmer x4 > heat.low > simmer x4 | 34.5 | 33.7 | 74.7 |
| 31-40 | best gear | cen_dur4 (level 31) | add.ingredient.1 > simmer x2 > add.salt > stir > add.salt > simmer > add.salt > stir > simmer > stir x2 > simmer x2 > heat.low > stir x2 > simmer > stir > simmer x3 | 79.4 | 47.1 | 96.4 |
| 31-40 | best gear | pf_cookedfish_4 (level 33) | simmer > add.ingredient.1 > simmer x2 > add.salt > add.ingredient.1 > simmer > add.salt > add.ingredient.1 > simmer > heat.low > cover > simmer > heat.low > add.spice > simmer > cover > add.salt > simmer x2 > add.salt > add.ingredient.1 > simmer > add.salt > add.ingredient.1 > simmer > add.salt > add.ingredient.1 > simmer > add.salt > add.ingredient.1 > simmer > add.salt > add.ingredient.1 > simmer | 68.5 | 45.8 | 93.8 |
| 31-40 | best gear | tusken_mball (level 35) | add.ingredient.0 > simmer x2 > heat.low > simmer > add.ingredient.0 x2 > add.spice > heat.low > add.ingredient.0 x2 > add.salt > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer | 80.4 | 70.8 | 88.5 |
| 31-40 | best gear | sliced_cod (level 36) | add.ingredient.0 > add.salt x2 > add.spice > stir x5 > simmer > stir x2 > simmer x5 > add.ingredient.1 > add.salt > simmer x2 | 64.0 | 78.4 | 90.5 |
| 31-40 | best gear | cen_res_tra4 (level 37) | heat.medium > simmer > add.salt > heat.medium > add.ingredient.1 > simmer > heat.medium > simmer > add.spice > heat.medium > heat.low > stir x2 > add.salt > heat.medium > simmer > add.salt > stir > simmer > add.salt > heat.medium > stir > heat.medium > add.ingredient.1 > simmer > stir > simmer > heat.medium > simmer > add.salt > heat.medium > simmer > add.salt > heat.medium > simmer > add.salt > heat.medium > simmer > add.salt > heat.medium | 72.4 | 79.6 | 95.3 |
| 31-40 | best gear | dem_sandwich (level 39) | simmer > stir > simmer x2 > heat.medium > simmer > heat.low > add.ingredient.1 > add.ingredient.0 > add.ingredient.2 > simmer x3 > add.ingredient.2 > simmer > add.salt > simmer > stir x5 > add.salt > simmer > stir > simmer > add.salt > stir x2 > simmer x2 | 58.2 | 45.7 | 92.3 |
| 41-53 | no gear | bass_meuniere (level 42) | add.ingredient.0 > add.salt > add.ingredient.0 > stir > add.sugar > simmer > stir x2 > simmer > stir > simmer x6 > add.ingredient.1 > add.salt > stir > simmer x2 | 68.1 | 64.5 | 87.2 |
| 41-53 | no gear | pf_cookedmeat_5 (level 44) | heat.medium > cover > simmer x2 > add.ingredient.0 > add.salt x3 > simmer x2 > add.salt > simmer > stir > simmer x3 > heat.low > simmer x2 > heat.low > simmer > heat.low > simmer > add.ingredient.1 > add.salt > simmer x3 | 52.5 | 44.6 | 76.3 |
| 41-53 | no gear | wild_curry (level 46) | add.ingredient.0 > simmer > add.salt > add.ingredient.0 > simmer > add.ingredient.0 > stir x2 > cover > simmer > stir x2 > simmer > stir x3 > simmer x5 > heat.low > add.ingredient.1 > simmer > add.salt x2 > simmer | 72.2 | 68.6 | 89.3 |
| 41-53 | no gear | wild_bbroth (level 48) | stir > add.ingredient.2 > simmer > add.ingredient.0 > add.ingredient.2 > stir x5 > simmer > add.salt > simmer x7 > add.ingredient.1 > heat.low > simmer x2 > add.spice > simmer | 67.5 | 56.0 | 86.3 |
| 41-53 | no gear | rancid_broth (level 50) | simmer x3 > stir > simmer x4 > add.salt > stir > add.salt > simmer > cover > heat.low > cover > simmer > add.salt x2 > heat.low > simmer > add.salt > simmer x3 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt | 60.2 | 52.7 | 75.9 |
| 41-53 | no gear | forbid_gumbo (level 52) | heat.low > add.salt x2 > simmer > add.ingredient.1 > stir x2 > add.ingredient.0 x2 > simmer > heat.medium > add.ingredient.1 > simmer x3 > stir > simmer > cover > add.salt > cover > simmer > stir > simmer x2 > add.salt x2 > simmer > add.salt x2 > simmer > add.salt > heat.medium > simmer > add.salt > simmer | 57.4 | 41.7 | 89.4 |
| 41-53 | mid gear | bass_meuniere (level 42) | heat.medium > stir > simmer > stir > add.salt > simmer x3 > add.salt > simmer x2 > add.salt > simmer > add.salt > heat.low > simmer x3 > add.sugar > cover > add.salt > simmer > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt | 69.3 | 65.9 | 91.2 |
| 41-53 | mid gear | pf_cookedmeat_5 (level 44) | cover > simmer x4 > add.ingredient.1 > simmer > heat.low > cover > heat.low > add.salt > simmer x2 > cover > simmer x3 > add.salt > simmer > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer | 77.7 | 58.1 | 70.8 |
| 41-53 | mid gear | wild_curry (level 46) | add.spice > add.salt > simmer > add.salt > simmer x2 > add.ingredient.1 > simmer > add.salt > simmer > add.salt > simmer x2 > heat.low > simmer x2 > add.salt > cover > simmer x3 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 | 69.0 | 76.8 | 91.1 |
| 41-53 | mid gear | wild_bbroth (level 48) | add.ingredient.2 > simmer > heat.low > simmer x2 > heat.low > simmer > add.salt > simmer x2 > add.ingredient.0 > simmer > stir x4 > simmer > stir x2 > add.spice > simmer > stir > simmer x4 > heat.low > simmer x2 > heat.medium > simmer | 78.0 | 58.7 | 89.7 |
| 41-53 | mid gear | rancid_broth (level 50) | cover > stir > simmer x2 > add.salt > simmer x2 > add.salt > add.ingredient.1 > add.salt x2 > simmer x4 > heat.low > add.salt > simmer x3 > add.spice > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer | 57.3 | 55.9 | 82.5 |
| 41-53 | mid gear | forbid_gumbo (level 52) | heat.medium > add.ingredient.0 x3 > simmer x3 > add.ingredient.0 > heat.low > add.salt x2 > add.ingredient.0 > add.salt > simmer x2 > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > add.ingredient.0 > simmer > heat.medium > simmer > add.ingredient.0 > simmer > add.ingredient.0 | 69.5 | 59.6 | 97.2 |
| 41-53 | best gear | bass_meuniere (level 42) | add.ingredient.0 > simmer x2 > heat.low > add.salt > add.ingredient.0 > simmer > heat.low > stir > simmer x4 > stir x3 > simmer > stir > simmer x3 | 78.7 | 74.6 | 96.9 |
| 41-53 | best gear | pf_cookedmeat_5 (level 44) | add.ingredient.0 > simmer > heat.low > simmer > heat.low x2 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer | 74.4 | 69.2 | 87.9 |
| 41-53 | best gear | wild_curry (level 46) | add.ingredient.2 > simmer > heat.low x2 > add.ingredient.0 > heat.low > simmer > stir > add.ingredient.0 > stir x2 > simmer > stir x4 > simmer > stir > simmer x5 > heat.low > simmer x2 > cover > simmer | 94.0 | 75.6 | 93.0 |
| 41-53 | best gear | wild_bbroth (level 48) | heat.medium > add.ingredient.2 > add.ingredient.0 > cover > add.spice > stir > add.salt x2 > simmer > stir > simmer x3 > heat.low > simmer > add.ingredient.1 > simmer x3 > cover > add.ingredient.1 > add.spice > simmer x2 > cover > add.spice > simmer x2 | 80.3 | 54.2 | 93.7 |
| 41-53 | best gear | rancid_broth (level 50) | add.ingredient.0 > stir x3 > add.salt > stir > add.ingredient.0 > simmer > stir x4 > cover > stir > simmer > stir > simmer > stir x2 > simmer x2 > add.ingredient.1 > cover > add.salt > simmer x3 | 56.2 | 66.4 | 91.6 |
| 41-53 | best gear | forbid_gumbo (level 52) | add.ingredient.0 > heat.low > add.ingredient.0 > add.ingredient.2 > add.ingredient.0 > heat.low x2 > add.salt > simmer > heat.low > simmer > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer | 92.9 | 66.9 | 98.3 |

## Perks optional

### Rank 50, best gear, no perks: adaptive attempts that reach maximum quality, by recipe band

| Band | Recipes played | Attempts | Reach maximum quality | Make the item | Avg quality of items made | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 6 | 600 | 85% | 99% | 96% | >= 50% | Passed |
| 46-50 | 6 | 600 | 80% | 98% | 94% | >= 50% | Passed |
| 51-53 | 6 | 600 | 59% | 94% | 83% | >= 50% | Passed |

## Length

### Actions per attempt of the adaptive player at level (item craft at level)

| Band | Gear | Recipes played | Mean actions per attempt | Shortest recipe | Longest recipe | Stalled attempts | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 12.7 | 10.3 | 16.5 | 0 | 10-20 | Passed |
| 1-10 | mid gear | 6 | 11.5 | 8.9 | 15.5 | 0 | 10-20 | Passed |
| 1-10 | best gear | 6 | 10.4 | 7.1 | 14.6 | 0 | 10-20 | Passed |
| 11-20 | no gear | 6 | 13.9 | 12.1 | 17.5 | 0 | 10-20 | Passed |
| 11-20 | mid gear | 6 | 12.9 | 10.1 | 17.2 | 0 | 10-20 | Passed |
| 11-20 | best gear | 6 | 11.5 | 9.6 | 15.2 | 0 | 10-20 | Passed |
| 21-30 | no gear | 6 | 14.0 | 10.0 | 19.3 | 0 | 10-20 | Passed |
| 21-30 | mid gear | 6 | 13.1 | 9.6 | 18.0 | 0 | 10-20 | Passed |
| 21-30 | best gear | 6 | 11.9 | 7.7 | 17.8 | 0 | 10-20 | Passed |
| 31-40 | no gear | 6 | 17.7 | 14.6 | 20.4 | 0 | 10-20 | Passed |
| 31-40 | mid gear | 6 | 17.3 | 12.9 | 20.8 | 0 | 10-20 | Passed |
| 31-40 | best gear | 6 | 15.1 | 10.9 | 18.2 | 0 | 10-20 | Passed |
| 41-53 | no gear | 6 | 17.0 | 15.9 | 18.3 | 0 | 10-20 | Passed |
| 41-53 | mid gear | 6 | 16.2 | 14.8 | 17.5 | 0 | 10-20 | Passed |
| 41-53 | best gear | 6 | 14.2 | 12.8 | 15.6 | 0 | 10-20 | Passed |

## Economy parity

### Component stacks consumed per item made, failures included (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Parity rule | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 1.54 | 1.57 | 0.99 | 96% | 96% | 84% | 1% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | mid gear | 6 | 1.51 | 1.54 | 0.98 | 98% | 96% | 93% | 8% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | best gear | 6 | 1.50 | 1.53 | 0.98 | 100% | 98% | 97% | 16% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | no gear | 6 | 1.55 | 1.59 | 0.97 | 95% | 93% | 85% | 1% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | mid gear | 6 | 1.54 | 1.52 | 1.01 | 97% | 98% | 90% | 12% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | best gear | 6 | 1.50 | 1.51 | 0.99 | 100% | 99% | 94% | 31% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | no gear | 6 | 1.67 | 1.65 | 1.01 | 97% | 97% | 86% | 8% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | mid gear | 6 | 1.65 | 1.65 | 1.00 | 98% | 98% | 90% | 15% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | best gear | 6 | 1.64 | 1.63 | 1.00 | 100% | 99% | 94% | 29% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | no gear | 6 | 2.05 | 2.05 | 1.00 | 97% | 96% | 82% | 8% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | mid gear | 6 | 2.07 | 2.01 | 1.03 | 95% | 99% | 84% | 14% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | best gear | 6 | 2.02 | 2.01 | 1.00 | 98% | 99% | 93% | 26% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | no gear | 6 | 2.09 | 1.73 | 1.21 | 96% | 82% | 82% | 37% | yes | 0% / 17% | at most +10% | Failed |
| 41-53 | mid gear | 6 | 2.11 | 2.07 | 1.02 | 96% | 87% | 83% | 60% | yes | 0% / 0% | at most +10% | Passed |
| 41-53 | best gear | 6 | 2.03 | 2.03 | 1.00 | 99% | 94% | 92% | 81% | yes | 0% / 0% | within 10% | Passed |

## XP rate

### Item crafts: XP per hour against the legacy loop (6.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per craft | Legacy actions per craft | Legacy XP per hour | New XP per craft | New actions per craft | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 587 | 4.5 | 79033 | 2347 | 12.7 | 83338 | 1.05 | - | Passed |
| 1-10 | no gear, 1 enhancement | 496 | 5.8 | 51400 | 2367 | 12.0 | 88780 | 1.73 | - | Passed |
| 1-10 | no gear, 2 enhancements | 176 | 6.0 | 17585 | 2364 | 12.0 | 88584 | 5.04 | - | Passed |
| 1-10 | no gear, 3 enhancements | 90 | 6.0 | 9000 | 2285 | 12.2 | 84256 | 9.36 | - | Passed |
| 1-10 | mid gear | 621 | 5.3 | 70987 | 2488 | 11.5 | 97367 | 1.37 | - | Passed |
| 1-10 | mid gear, 1 enhancement | 591 | 5.4 | 65551 | 2487 | 11.1 | 100404 | 1.53 | - | Passed |
| 1-10 | mid gear, 2 enhancements | 574 | 6.2 | 55612 | 2484 | 10.9 | 102368 | 1.84 | - | Passed |
| 1-10 | mid gear, 3 enhancements | 490 | 6.9 | 42515 | 2430 | 11.5 | 95472 | 2.25 | - | Passed |
| 1-10 | best gear | 682 | 5.4 | 75384 | 2547 | 10.4 | 110653 | 1.47 | - | Passed |
| 1-10 | best gear, 1 enhancement | 658 | 5.4 | 72768 | 2552 | 9.5 | 120975 | 1.66 | - | Passed |
| 1-10 | best gear, 2 enhancements | 649 | 6.0 | 64536 | 2541 | 9.5 | 120122 | 1.86 | - | Passed |
| 1-10 | best gear, 3 enhancements | 624 | 6.1 | 61201 | 2546 | 9.7 | 118321 | 1.93 | - | Passed |
| 11-20 | no gear | 570 | 8.6 | 39740 | 1247 | 13.9 | 40220 | 1.01 | - | Passed |
| 11-20 | no gear, 1 enhancement | 342 | 9.2 | 22208 | 1157 | 15.1 | 34410 | 1.55 | - | Passed |
| 11-20 | no gear, 2 enhancements | 90 | 6.5 | 8308 | 1174 | 15.3 | 34594 | 4.16 | - | Passed |
| 11-20 | no gear, 3 enhancements | 90 | 6.5 | 8308 | 1196 | 14.7 | 36643 | 4.41 | - | Passed |
| 11-20 | mid gear | 660 | 10.7 | 37075 | 1301 | 12.9 | 45526 | 1.23 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 634 | 11.5 | 33071 | 1283 | 13.6 | 42567 | 1.29 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 600 | 13.0 | 27720 | 1294 | 13.5 | 43010 | 1.55 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 566 | 13.6 | 24960 | 1250 | 13.9 | 40547 | 1.62 | - | Passed |
| 11-20 | best gear | 780 | 14.5 | 32192 | 1340 | 11.5 | 52295 | 1.62 | - | Passed |
| 11-20 | best gear, 1 enhancement | 768 | 14.8 | 31150 | 1349 | 11.8 | 51442 | 1.65 | - | Passed |
| 11-20 | best gear, 2 enhancements | 759 | 16.0 | 28471 | 1335 | 11.9 | 50594 | 1.78 | - | Passed |
| 11-20 | best gear, 3 enhancements | 736 | 15.9 | 27795 | 1321 | 13.0 | 45588 | 1.64 | - | Passed |
| 21-30 | no gear | 632 | 9.9 | 38246 | 1235 | 14.0 | 39734 | 1.04 | - | Passed |
| 21-30 | no gear, 1 enhancement | 612 | 11.3 | 32486 | 1181 | 15.8 | 33572 | 1.03 | - | Passed |
| 21-30 | no gear, 2 enhancements | 606 | 12.3 | 29487 | 1133 | 16.3 | 31311 | 1.06 | - | Passed |
| 21-30 | no gear, 3 enhancements | 587 | 13.0 | 27166 | 1077 | 17.0 | 28461 | 1.05 | - | Passed |
| 21-30 | mid gear | 678 | 15.4 | 26409 | 1263 | 13.1 | 43276 | 1.64 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 678 | 16.0 | 25445 | 1243 | 14.5 | 38514 | 1.51 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 665 | 16.4 | 24388 | 1233 | 15.1 | 36839 | 1.51 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 652 | 16.6 | 23519 | 1203 | 15.4 | 35048 | 1.49 | - | Passed |
| 21-30 | best gear | 773 | 21.0 | 22040 | 1290 | 11.9 | 48688 | 2.21 | - | Passed |
| 21-30 | best gear, 1 enhancement | 766 | 21.2 | 21642 | 1271 | 13.1 | 43648 | 2.02 | - | Passed |
| 21-30 | best gear, 2 enhancements | 755 | 21.8 | 20753 | 1259 | 14.1 | 40049 | 1.93 | - | Passed |
| 21-30 | best gear, 3 enhancements | 750 | 22.0 | 20437 | 1253 | 14.5 | 38848 | 1.90 | - | Passed |
| 31-40 | no gear | 631 | 12.0 | 31525 | 1298 | 17.7 | 32941 | 1.04 | - | Passed |
| 31-40 | no gear, 1 enhancement | 621 | 13.9 | 26822 | 1176 | 17.8 | 29706 | 1.11 | - | Passed |
| 31-40 | no gear, 2 enhancements | 593 | 14.3 | 24810 | 1084 | 18.5 | 26394 | 1.06 | - | Passed |
| 31-40 | no gear, 3 enhancements | 579 | 15.3 | 22660 | 1047 | 19.2 | 24517 | 1.08 | - | Passed |
| 31-40 | mid gear | 678 | 16.6 | 24487 | 1298 | 17.3 | 33861 | 1.38 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 660 | 17.9 | 22095 | 1243 | 17.6 | 31709 | 1.44 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 659 | 18.5 | 21385 | 1198 | 17.9 | 30031 | 1.40 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 638 | 18.1 | 21133 | 1157 | 18.2 | 28646 | 1.36 | - | Passed |
| 31-40 | best gear | 748 | 23.7 | 18926 | 1378 | 15.1 | 41092 | 2.17 | - | Passed |
| 31-40 | best gear, 1 enhancement | 740 | 24.7 | 17975 | 1321 | 16.2 | 36721 | 2.04 | - | Passed |
| 31-40 | best gear, 2 enhancements | 731 | 25.5 | 17211 | 1288 | 16.9 | 34342 | 2.00 | - | Passed |
| 31-40 | best gear, 3 enhancements | 718 | 24.0 | 17922 | 1229 | 17.6 | 31455 | 1.76 | - | Passed |
| 41-53 | no gear | 723 | 17.3 | 25077 | 1185 | 17.0 | 31427 | 1.25 | - | Passed |
| 41-53 | no gear, 1 enhancement | 559 | 14.5 | 23118 | 1093 | 19.3 | 25419 | 1.10 | - | Passed |
| 41-53 | no gear, 2 enhancements | 506 | 14.5 | 20996 | 1010 | 20.5 | 22209 | 1.06 | - | Passed |
| 41-53 | no gear, 3 enhancements | 453 | 14.5 | 18752 | 976 | 20.6 | 21321 | 1.14 | - | Passed |
| 41-53 | mid gear | 883 | 23.7 | 22400 | 1207 | 16.2 | 33426 | 1.49 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 690 | 21.0 | 19702 | 1177 | 19.0 | 27895 | 1.42 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 620 | 18.2 | 20454 | 1130 | 19.3 | 26310 | 1.29 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 567 | 17.0 | 20004 | 1013 | 20.5 | 22223 | 1.11 | - | Passed |
| 41-53 | best gear | 1051 | 28.2 | 22386 | 1267 | 14.2 | 40170 | 1.79 | - | Passed |
| 41-53 | best gear, 1 enhancement | 933 | 27.8 | 20145 | 1242 | 17.7 | 31587 | 1.57 | - | Passed |
| 41-53 | best gear, 2 enhancements | 810 | 25.0 | 19445 | 1197 | 18.5 | 29117 | 1.50 | - | Passed |
| 41-53 | best gear, 3 enhancements | 735 | 22.2 | 19841 | 1193 | 18.6 | 28821 | 1.45 | - | Passed |

## Enhancement parity

### Rank 50, best gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 85%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 6 | 60 | 32% | 99% | 96% | 0.95 | 99% | 95.6% | 0.96 (+/- 0.01) | 1.02 | >= 1.00 | Passed |
| 41-45 | 2 | 6 | 60 | 63% | 99% | 90% | 1.77 | 98% | 93.2% | 1.80 (+/- 0.02) | 1.02 | >= 1.00 | Passed |
| 41-45 | 3 | 6 | 60 | 95% | 100% | 82% | 2.44 | 97% | 93.5% | 2.60 (+/- 0.04) | 1.07 | >= 1.00 | Passed |
| 46-50 | 1 | 6 | 60 | 32% | 100% | 85% | 0.84 | 98% | 92.6% | 0.91 (+/- 0.01) | 1.08 | >= 1.00 | Passed |
| 46-50 | 2 | 6 | 60 | 64% | 99% | 75% | 1.48 | 96% | 89.7% | 1.65 (+/- 0.03) | 1.12 | >= 1.00 | Passed |
| 46-50 | 3 | 6 | 60 | 96% | 99% | 67% | 1.99 | 91% | 87.8% | 2.20 (+/- 0.05) | 1.11 | >= 1.00 | Passed |
| 51-53 | 1 | 6 | 60 | 8% | 63% | 6% | 0.03 | 89% | 82.4% | 0.76 (+/- 0.02) | 23.15 | >= 1.00 | Passed |
| 51-53 | 2 | 6 | 60 | 16% | 35% | 5% | 0.03 | 89% | 76.6% | 1.25 (+/- 0.04) | 40.12 | >= 1.00 | Passed |
| 51-53 | 3 | 6 | 60 | 24% | 18% | 4% | 0.02 | 86% | 72.5% | 1.64 (+/- 0.06) | 88.26 | >= 1.00 | Passed |

### Thresholds, first / second / third slot, that would meet parity for the quality measured (the strictest set that keeps every case at least at today's, never falling from one slot to the next)

| Scope | Thresholds that meet parity | In steps of 5 | Thresholds in force |
| --- | --- | --- | --- |
| Every band together (the shared thresholds) | 57% / 93% / 100% | 55% / 90% / 100% | 50% / 85% / 85% |
| Band 41-45 alone | 57% / 93% / 100% | 55% / 90% / 100% | 50% / 85% / 85% |
| Band 46-50 alone | 77% / 98% / 99% | 75% / 95% / 100% | 50% / 85% / 85% |
| Band 51-53 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 85% |

### Rank 50, best gear: quality of the items the new minigame made, in whole percent, by band and slots filled

| Band | Slots filled | Items made | 10th percentile | 25th percentile | Median | 75th percentile | 90th percentile | Made at 100% |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 99% | 93% | 100% | 100% | 100% | 100% | 76% |
| 41-45 | 2 | 98% | 74% | 100% | 100% | 100% | 100% | 75% |
| 41-45 | 3 | 97% | 72% | 99% | 100% | 100% | 100% | 72% |
| 46-50 | 1 | 98% | 67% | 100% | 100% | 100% | 100% | 75% |
| 46-50 | 2 | 96% | 52% | 99% | 100% | 100% | 100% | 70% |
| 46-50 | 3 | 91% | 53% | 87% | 100% | 100% | 100% | 61% |
| 51-53 | 1 | 89% | 26% | 74% | 100% | 100% | 100% | 53% |
| 51-53 | 2 | 89% | 8% | 57% | 100% | 100% | 100% | 46% |
| 51-53 | 3 | 86% | 3% | 45% | 99% | 100% | 100% | 40% |

### Representative enhancements: for each recipe kind the real item at the best level the recipe accepts (the old window refuses one more than 5 levels above the recipe) whose penalty is the lower median of that level

| Recipe kind | Enhancement | Level | Progress penalty | Items of that kind and level (min / median / max penalty) | Recipes |
| --- | --- | --- | --- | --- | --- |
| Food | Cooking Enhancement - Resistance - Mobility V (`cen_res_mob5`) | 45 (tier V) | 60 | 28 items: 50 / 60.0 / 80 | 18 |

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
- The Kitchen has no perks yet: the Chef line is a draft, so gate 2 does not apply and gates run without styles.
- Seconds per action is an estimate (8 s) until the client evidence measures it.
- Ingredient names are the resrefs here; the window shows item names.

## Notes added after the run

These are written by hand after the generated report above, so a rerun of the command replaces the report and not these notes.

- **Gate 5 (economy parity), the one failing gate: 41-53 / no gear, ratio 1.21.** It is a comparison over different recipe sets, not a Kitchen cost. In this sample the old minigame never makes Forbidden Gumbo (level 52, 4 component stacks) without gear, so its stacks per item cover the other five recipes and the Kitchen's cover all six. Probing the same cell per recipe (a scratch build at an earlier tuning iteration; the old minigame's side is identical) gave Forbidden Gumbo 100% for the Kitchen against 0% for today, and over the five recipes both make a ratio of 1.04 (1.79 against 1.72 stacks per item). The mid and best gear cells of that band show the same recipe (20% and 67% for today) and pass at 1.02 and 1.00 on their own. Every other cell is within 10%, and the Kitchen's average quality is above today's in every cell. Passing this cell as defined would mean making a dish the old window cannot make fail on purpose, so no value was changed for it. Proposed harness change for the owner or the harness workstream: compute stacks per item over the recipes both sides make (the never-made share is already a separate check).
- **Gate 1 under both definitions.** Under the definition in force (one rotation per band and tier, 10 points) the Kitchen passes every cell, with a smallest margin of 19.4 points. Under the previous per-recipe definition, at an earlier tuning iteration (3 recipes a band, 20 runs) it failed 3 of 15 cells by 1 to 5 points (1-10 no gear 6.8, 1-10 best gear 5.1, 11-20 best gear 9.0); no randomness or effect was added for it.
- **Tuning that moved a gate** (every value is a starting value in `KitchenTuning` or `KitchenParameterProfile`): the Doneness a dish needs 100 -> 120 and Craftsmanship 0.015 -> 0.008 per point (length: crafts with gear were under 10 actions); enhancement Doneness 0.5 -> 0.25 of the progress penalty and recipe Freshness slack 105% -> 112% (gate 7, and gate 3's 51-53 band, which sat at 52%); push spread 40% -> 50% and Stir 8 -> 12 CP (gate 1, one rotation per band: at 40% and 8 CP the best single rotation came within 10 points in 4 of 15 cells). Raising the drift from 8 to 10 as well made gate 1 worse, because it hurt adaptive play as much, and was dropped.
- **XP multipliers** are the least in each recipe-level band that kept gate 6 passing at every gear tier and enhancement count in the sampled runs (1-10 2.15x, 11-20 1.15x, 21-30 1.10x, 31-40 1.20x, 41-53 1.05x), rounded up to the next 0.05. The base's 1.05 crafting pace multiplier is layered on top in the game and is not part of the gate.
- **Gate 7** passes at the shared thresholds (50% / 85% / 85%), unchanged. The Kitchen's expected enhancements transferred are 1.02 to 1.12 times today's in the 41-45 and 46-50 bands and many times today's in 51-53, where today's crafter rarely makes the item. Thresholds that would meet parity for this quality: 57% / 93% / 100%.
- The run happened on the branch before the base's latest merge (progression, Prospecting, Line, Reaction tuning); that merge changed no gate, player or crafting-legacy file.
