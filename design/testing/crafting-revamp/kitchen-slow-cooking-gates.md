# Crafting revamp: balance gates of the Kitchen (Slow Cooking Chef) minigame

- Command: `SWLOR.CraftSim gates --scenario kitchen-slow-cooking --gates rotation,materials,xp,enhancements --runs 40 --seed 1 --recipes-per-band 4 --repo . --out design/testing/crafting-revamp/kitchen-slow-cooking-gates.md`
- Seed 1; 40 runs per case.

## Inputs

- Scenario: Kitchen (Slow Cooking Chef) (`kitchen-slow-cooking`), Agriculture recipes, minigame Kitchen.
- Quality: the quality meter's share of its maximum when the item is made, in points (0-100). A broken or abandoned piece counts 0, so every average includes the crafts that failed.
- Crafter: the rank the scenario's cohort gives each recipe (by default the recipe's level, at most 50, so a crafter works at their own level), with no food or status bonuses and no perks unless a gate names a style or the scenario's limitations say it plays a perk loadout. Gear levels are the plan's none, middle and best tier of the skill's crafting gear:
  - no gear (Control 0, Craftsmanship 0, CP 0)
  - mid gear (Control 13, Craftsmanship 12, CP 20)
  - best gear (Control 29, Craftsmanship 30, CP 37)
- Recipes: 1-10: 4 of 58; 11-20: 4 of 59; 21-30: 4 of 65; 31-40: 4 of 61; 41-53: 4 of 76 recipes played.
- Adaptive player: depth 3, forecast samples per result 1, budget 1500 engine copies per decision, 0.5% of value lost per extra action, heuristic KitchenLeafHeuristic. Each attempt's dice come from its own stream of the seed.
- Fixed-rotation solver: beam 8, cycles up to 3, 8 adaptive lines as seeds, up to 40 actions, 64 training attempts per local-search round and 150 selection attempts for each of 12 finalists, local search from the best 3 candidate(s) for up to 3 round(s) each. Gate 1 searches one sequence for each band and gear tier across the band's sampled recipes (every recipe weighing the same; an action a recipe's engine cannot take is skipped there), and also each recipe's own best sequence as a reported metric. The winner is chosen on separate attempts and then scored on a third set (hold-out) with 40 attempts per recipe.
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
  - `RecipesPerBand` = 4: recipes played per band
  - `SolverOptions` = the scenario's: effort of the fixed-rotation solver
  - `ActionLimit` = 100: actions after which an attempt counts as stalled
- Length target: 10-20 actions (item craft at level).
- Enhancement thresholds in force (`CraftOutcomeRules`, read by gate 7): 50% / 85% / 85% for slots 1, 2 and 3. The scenario applies enhancements to its engine's progress meter.
- Styles: none offered, so the style-balance gate is not applicable.
- Gates run: No fixed rotation, Economy parity, XP rate, Enhancement parity. The others were not run, so this report cannot show that the minigame passes.

## Verdicts

| Gate | Verdict | Summary |
| --- | --- | --- |
| No fixed rotation | Failed | One fixed rotation comes within 10 points of adaptive play in 9 case(s): 21-30 / no gear; 21-30 / mid gear; 21-30 / best gear; 31-40 / no gear; 31-40 / mid gear; 31-40 / best gear; 41-53 / no gear; 41-53 / mid gear; 41-53 / best gear (smallest margin -9.3 in 41-53 / no gear). The best rotation for each recipe alone, a reported metric, is -9.3 to 11.0 points behind adaptive play. |
| Economy parity | Passed | Component stacks per item are within 10% of the legacy baseline where its success rate is at least 90%, and at most 10% above it where it is lower, at no less quality, in every band and gear tier. |
| XP rate | Passed | Item crafts: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.04x in 21-30 / no gear, 1 enhancement). |
| Enhancement parity | Passed | At rank 50 in best gear, the thresholds in force (50% / 85% / 85%) transfer at least 1.00 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 1.07 times today's). Thresholds that meet parity in every band for this quality: 99% / 100% / 100%. |

## No fixed rotation

### Average quality: adaptive play against the best single fixed rotation of each band and gear tier (quality points; a broken piece counts 0)

| Band | Gear | Recipes played | Adaptive avg quality | Best single rotation avg quality | Margin | Required | Best rotation per recipe avg quality (reported) | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 4 | 93.4 | 75.7 | 17.7 | >= 10 | 82.4 | Passed |
| 1-10 | mid gear | 4 | 95.8 | 82.4 | 13.5 | >= 10 | 85.0 | Passed |
| 1-10 | best gear | 4 | 98.5 | 87.5 | 11.0 | >= 10 | 89.7 | Passed |
| 11-20 | no gear | 4 | 92.0 | 65.2 | 26.8 | >= 10 | 84.1 | Passed |
| 11-20 | mid gear | 4 | 95.6 | 75.5 | 20.0 | >= 10 | 91.3 | Passed |
| 11-20 | best gear | 4 | 98.1 | 77.6 | 20.5 | >= 10 | 92.9 | Passed |
| 21-30 | no gear | 4 | 92.5 | 94.1 | -1.6 | >= 10 | 91.7 | Failed |
| 21-30 | mid gear | 4 | 96.2 | 94.4 | 1.7 | >= 10 | 94.9 | Failed |
| 21-30 | best gear | 4 | 97.8 | 95.5 | 2.3 | >= 10 | 97.4 | Failed |
| 31-40 | no gear | 4 | 93.0 | 98.5 | -5.5 | >= 10 | 99.4 | Failed |
| 31-40 | mid gear | 4 | 93.6 | 99.9 | -6.3 | >= 10 | 100.0 | Failed |
| 31-40 | best gear | 4 | 96.4 | 100.0 | -3.6 | >= 10 | 100.0 | Failed |
| 41-53 | no gear | 4 | 90.7 | 100.0 | -9.3 | >= 10 | 100.0 | Failed |
| 41-53 | mid gear | 4 | 96.6 | 100.0 | -3.4 | >= 10 | 100.0 | Failed |
| 41-53 | best gear | 4 | 96.3 | 100.0 | -3.7 | >= 10 | 100.0 | Failed |

### The single rotation found for each band and gear tier, searched across the band's sampled recipes (average quality in points)

| Band | Gear | Best single rotation for the band | Training | Selection | Hold-out | Adaptive on these recipes |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | simmer > add.ingredient.2 > simmer > heat.medium > add.ingredient.2 > add.ingredient.1 > heat.medium > add.ingredient.0 > simmer x2 > add.salt > cover > simmer x2 > add.salt x2 > heat.medium > simmer > heat.medium > simmer > heat.low > simmer > heat.low x2 > add.spice > simmer | 75.3 | 78.1 | 75.7 | 93.4 |
| 1-10 | mid gear | add.ingredient.2 > simmer > heat.low > simmer x2 > add.ingredient.2 > heat.low > add.salt > simmer > add.salt > add.ingredient.0 x2 > cover > heat.medium > simmer x2 > heat.medium > simmer x2 > stir > simmer x2 > heat.low > add.spice > simmer x3 | 80.7 | 80.8 | 82.4 | 95.8 |
| 1-10 | best gear | stir > add.ingredient.1 > simmer > heat.low > simmer > heat.low > simmer x2 > stir > add.ingredient.0 > simmer > add.spice > add.ingredient.0 > add.salt > stir > heat.medium > simmer x3 > heat.low > simmer x5 | 88.1 | 86.8 | 87.5 | 98.5 |
| 11-20 | no gear | add.ingredient.0 > stir > simmer > add.ingredient.1 > simmer > heat.medium > add.salt x2 > simmer > stir > heat.medium > simmer x4 > heat.low > simmer x5 | 66.8 | 65.3 | 65.2 | 92.0 |
| 11-20 | mid gear | simmer x5 > heat.low > add.ingredient.0 > simmer > add.ingredient.1 > simmer x3 > add.salt > stir > simmer x3 > heat.low > simmer > heat.low > simmer x3 > cover > heat.medium > cover > simmer | 75.0 | 76.1 | 75.5 | 95.6 |
| 11-20 | best gear | stir > add.ingredient.0 > add.salt > simmer > stir > simmer x3 > heat.low > simmer > add.ingredient.1 > add.salt > stir x2 > simmer > stir x2 > simmer x3 > cover > simmer x2 > heat.low > simmer x5 | 77.3 | 77.9 | 77.6 | 98.1 |
| 21-30 | no gear | add.ingredient.0 > heat.low > simmer > add.salt > add.ingredient.0 > reduce x4 > stir > simmer > stir > reduce x2 > cover > simmer > stir > cover > simmer x4 > heat.low > simmer x6 | 91.8 | 92.3 | 94.1 | 92.5 |
| 21-30 | mid gear | simmer > heat.low > simmer x4 > heat.low > simmer x2 > add.ingredient.0 > simmer > add.ingredient.0 > simmer x2 > add.sugar > add.salt > reduce x3 > simmer > reduce x2 > stir x4 > simmer x3 > cover > simmer x2 > heat.low > simmer x5 | 94.0 | 93.5 | 94.4 | 96.2 |
| 21-30 | best gear | stir > add.ingredient.0 > stir > simmer > heat.low > simmer > add.ingredient.0 > heat.low > simmer x3 > reduce > add.salt > reduce x4 > simmer x2 > reduce > simmer > reduce x2 > stir > simmer > reduce x2 > stir > reduce > stir x2 > simmer > heat.low > simmer x5 | 94.6 | 94.0 | 95.5 | 97.8 |
| 31-40 | no gear | add.ingredient.1 > simmer > heat.low > simmer > heat.low > simmer > add.ingredient.0 > heat.low > add.ingredient.0 > reduce x4 > stir x3 > simmer > stir > simmer x5 > heat.low > simmer x6 | 99.1 | 99.1 | 98.5 | 93.0 |
| 31-40 | mid gear | add.ingredient.1 > heat.low > reduce > simmer > reduce > add.ingredient.0 > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low | 99.9 | 100.0 | 99.9 | 93.6 |
| 31-40 | best gear | add.ingredient.0 > heat.low > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low | 100.0 | 100.0 | 100.0 | 96.4 |
| 41-53 | no gear | simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer | 100.0 | 100.0 | 100.0 | 90.7 |
| 41-53 | mid gear | simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer | 100.0 | 100.0 | 100.0 | 96.6 |
| 41-53 | best gear | heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low | 100.0 | 100.0 | 100.0 | 96.3 |

### Reported metric, not gated: the best fixed rotation for each recipe alone, and what the band's single rotation scores on the same recipe (average quality in points)

| Band | Gear | Recipe | Best rotation for this recipe alone (reported) | Its hold-out | The single rotation on this recipe | Adaptive on this recipe |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | pf_coarsefish_1 (level 2) | add.ingredient.0 > simmer x2 > add.ingredient.0 > simmer > add.ingredient.0 x2 > add.salt > simmer > add.ingredient.0 > simmer > heat.low > simmer x2 > add.ingredient.1 > add.salt > simmer x4 > heat.low x2 > simmer x3 > add.salt > simmer > add.spice > simmer | 91.8 | 85.1 | 97.4 |
| 1-10 | no gear | kinrath_mball (level 5) | add.ingredient.0 > add.salt > simmer > add.salt x3 > add.ingredient.1 > simmer x6 > heat.medium > simmer > heat.low x2 > simmer x3 > add.spice > simmer x3 > heat.medium > simmer | 69.7 | 60.9 | 92.1 |
| 1-10 | no gear | cen_hpr1 (level 6) | heat.medium > add.ingredient.0 > add.spice > add.salt > simmer > add.salt > simmer x3 > add.salt > simmer x2 > heat.medium > simmer > heat.low > simmer x2 > heat.low > add.spice > simmer | 84.4 | 79.8 | 93.6 |
| 1-10 | no gear | cen_fpr1 (level 8) | add.ingredient.0 > simmer x3 > add.ingredient.0 > simmer > add.ingredient.0 > simmer > heat.low > simmer x2 > add.ingredient.0 x2 > simmer > heat.low > simmer x2 > add.ingredient.0 x2 > simmer > add.ingredient.0 x2 > simmer > heat.low > add.ingredient.0 > simmer > add.ingredient.0 x2 > simmer > add.ingredient.0 x2 > simmer x2 > add.ingredient.0 > simmer > add.ingredient.0 x2 > simmer > add.ingredient.0 x2 | 83.7 | 77.0 | 90.5 |
| 1-10 | mid gear | pf_coarsefish_1 (level 2) | heat.medium > add.ingredient.0 > stir > add.ingredient.0 > heat.medium > add.spice > simmer > stir > simmer x3 > add.salt > simmer > heat.low > simmer > add.salt > simmer x3 > add.ingredient.1 > add.spice > cover > add.ingredient.1 > add.salt > simmer | 95.8 | 87.5 | 97.0 |
| 1-10 | mid gear | kinrath_mball (level 5) | add.ingredient.0 > add.ingredient.1 x2 > add.salt x2 > add.spice > simmer > stir > simmer x7 > heat.low > simmer > add.salt > add.ingredient.2 > add.salt > cover > simmer x3 | 76.0 | 81.9 | 95.0 |
| 1-10 | mid gear | cen_hpr1 (level 6) | add.ingredient.0 > simmer > stir > heat.low > add.ingredient.0 > heat.low > simmer x2 > add.ingredient.1 > simmer > stir > heat.medium > simmer x4 > heat.low > simmer x5 > cover > simmer | 86.2 | 80.5 | 96.3 |
| 1-10 | mid gear | cen_fpr1 (level 8) | simmer > heat.low > add.ingredient.0 > add.salt > add.ingredient.0 x2 > stir > add.spice > cover > heat.medium > simmer x2 > heat.medium > simmer x2 > stir > simmer x2 > heat.low > simmer x4 | 82.0 | 79.7 | 95.2 |
| 1-10 | best gear | pf_coarsefish_1 (level 2) | add.salt > heat.low > simmer > add.ingredient.0 > add.spice > heat.low > simmer > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer > add.ingredient.0 > heat.low > simmer | 99.7 | 92.3 | 100.0 |
| 1-10 | best gear | kinrath_mball (level 5) | add.ingredient.1 > simmer > add.ingredient.0 > simmer x2 > heat.low x2 > add.salt > add.ingredient.1 > simmer x4 > cover > add.salt > add.ingredient.1 x2 > add.spice > simmer > stir > simmer x6 > heat.low > simmer x4 > heat.medium > simmer | 88.6 | 86.5 | 97.5 |
| 1-10 | best gear | cen_hpr1 (level 6) | simmer > add.ingredient.0 x2 > stir > add.ingredient.1 > heat.medium > stir > simmer x2 > heat.low > simmer x2 > heat.low > simmer x8 | 86.8 | 86.8 | 98.1 |
| 1-10 | best gear | cen_fpr1 (level 8) | stir > add.ingredient.0 > simmer x2 > stir > add.ingredient.0 > add.spice > add.ingredient.0 > stir > add.salt > stir > heat.medium > simmer x3 > heat.low > simmer x5 | 83.6 | 84.5 | 98.5 |
| 11-20 | no gear | cooked_sardine (level 12) | simmer > add.spice > simmer x5 > add.salt > simmer > add.salt x2 > add.spice > simmer x2 > add.ingredient.1 > add.salt > simmer x2 > heat.low > simmer x4 | 87.8 | 77.6 | 99.3 |
| 11-20 | no gear | cen_stm2 (level 14) | add.salt x3 > simmer > add.sugar > simmer x4 > heat.low > simmer x8 | 92.0 | 78.1 | 87.5 |
| 11-20 | no gear | cen_res_mob2 (level 16) | add.sugar > simmer > stir > add.sugar > simmer > add.sugar > simmer x3 > add.salt > simmer > heat.low > simmer x2 > add.salt > simmer x6 | 84.0 | 55.6 | 85.7 |
| 11-20 | no gear | cen_fpr2 (level 18) | simmer x7 > add.ingredient.0 x2 > heat.medium > add.salt > add.spice > heat.medium > simmer > heat.low > simmer x3 > heat.low > add.salt > heat.low > simmer x2 > add.spice > simmer > add.salt > simmer x2 > heat.medium > simmer | 72.4 | 49.6 | 95.7 |
| 11-20 | mid gear | cooked_sardine (level 12) | cover > simmer x3 > heat.medium > simmer > add.ingredient.0 > stir > add.spice > stir > add.salt > heat.medium > stir > simmer x3 > heat.low > add.ingredient.1 > simmer x3 > add.ingredient.1 > heat.low > add.salt > simmer x5 | 88.0 | 94.0 | 97.0 |
| 11-20 | mid gear | cen_stm2 (level 14) | simmer x3 > add.salt > simmer > heat.low > cover > simmer > add.sugar > add.salt > simmer x2 > stir > simmer > heat.low > simmer > heat.low > simmer x2 > heat.low > simmer > add.salt > simmer x4 > cover > simmer | 96.1 | 83.4 | 97.5 |
| 11-20 | mid gear | cen_res_mob2 (level 16) | simmer x5 > heat.low > simmer > heat.low > cover > add.sugar x2 > cover > add.salt > add.sugar > stir > add.salt > simmer x5 > heat.low > simmer x5 > heat.medium > simmer | 93.5 | 75.2 | 90.7 |
| 11-20 | mid gear | cen_fpr2 (level 18) | add.ingredient.0 > heat.low > add.ingredient.0 > add.spice > add.ingredient.0 > add.salt > cover > simmer x2 > stir > simmer x2 > stir > simmer x3 > heat.low > simmer x4 > cover > heat.medium > simmer | 87.8 | 49.5 | 97.1 |
| 11-20 | best gear | cooked_sardine (level 12) | add.ingredient.0 > add.salt x2 > add.spice > stir x2 > simmer x2 > stir > simmer x5 > heat.low > simmer > add.ingredient.1 > add.salt > simmer x2 | 96.0 | 93.8 | 97.8 |
| 11-20 | best gear | cen_stm2 (level 14) | simmer > stir x2 > simmer x4 > heat.low > simmer > add.sugar > simmer > add.salt > simmer > cover > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer x2 > add.salt > simmer | 98.9 | 83.6 | 97.5 |
| 11-20 | best gear | cen_res_mob2 (level 16) | simmer > add.salt x2 > stir > simmer x2 > heat.low > add.ingredient.1 > simmer x4 > stir > add.salt > add.sugar > simmer x2 > heat.low > simmer > stir > simmer > stir > simmer x2 > heat.low > simmer x5 | 89.9 | 81.5 | 99.9 |
| 11-20 | best gear | cen_fpr2 (level 18) | simmer x2 > add.ingredient.0 > heat.low > add.ingredient.0 > simmer x2 > add.ingredient.0 > simmer > heat.low > add.spice > stir > simmer x2 > stir > simmer > stir x2 > simmer x5 | 86.8 | 51.5 | 97.3 |
| 21-30 | no gear | pf_coarsefish_3 (level 22) | add.ingredient.1 > simmer > heat.low > simmer > add.salt > add.ingredient.1 > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > add.sugar > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 > simmer > heat.low > add.ingredient.0 | 66.9 | 77.6 | 76.1 |
| 21-30 | no gear | b_hoptoad (level 25) | heat.low > add.ingredient.0 > add.salt x2 > reduce x4 > simmer > stir x2 > reduce > cover > reduce > simmer > cover > simmer x3 > stir > simmer > heat.low > simmer x6 | 100.0 | 99.5 | 100.0 |
| 21-30 | no gear | cen_xp3 (level 26) | heat.low > add.ingredient.1 > simmer x2 > add.salt > add.ingredient.0 > reduce x4 > simmer x2 > stir > simmer x3 > heat.low > simmer x6 > reduce > simmer | 100.0 | 100.0 | 99.5 |
| 21-30 | no gear | cen_recast3 (level 28) | add.sugar > simmer > add.salt > simmer > heat.low > add.ingredient.1 > simmer x2 > reduce > add.ingredient.1 > simmer > add.ingredient.1 > reduce > simmer > add.ingredient.1 > reduce > simmer > add.ingredient.1 > reduce > simmer > add.ingredient.1 > reduce > simmer > add.ingredient.1 > reduce > simmer > add.ingredient.1 > reduce > simmer > add.ingredient.1 > reduce > simmer > add.ingredient.1 > reduce > simmer > add.ingredient.1 > reduce > simmer > add.ingredient.1 > reduce | 99.8 | 99.5 | 94.5 |
| 21-30 | mid gear | pf_coarsefish_3 (level 22) | add.ingredient.0 > heat.low > stir x2 > heat.low > add.ingredient.0 > simmer x4 > add.ingredient.0 > heat.low > simmer > add.salt > add.sugar > simmer x11 | 79.8 | 78.0 | 85.1 |
| 21-30 | mid gear | b_hoptoad (level 25) | add.ingredient.0 x2 > heat.low > reduce > simmer > reduce > simmer x10 > reduce x2 > simmer | 100.0 | 99.7 | 100.0 |
| 21-30 | mid gear | cen_xp3 (level 26) | add.ingredient.0 > add.ingredient.1 x2 > heat.low > reduce > simmer x2 > reduce > simmer x9 > reduce x2 > simmer | 99.8 | 100.0 | 99.6 |
| 21-30 | mid gear | cen_recast3 (level 28) | heat.low > reduce > add.ingredient.1 > simmer > reduce > add.ingredient.1 > simmer > reduce > add.ingredient.1 > simmer > reduce > add.ingredient.1 > simmer > reduce > add.ingredient.1 > simmer > reduce > add.ingredient.1 > simmer > reduce > add.ingredient.1 > simmer > reduce > add.ingredient.1 > simmer > reduce > add.ingredient.1 > simmer > reduce > add.ingredient.1 > simmer > reduce > add.ingredient.1 > simmer > reduce > add.ingredient.1 > simmer > reduce > add.ingredient.1 > simmer | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | pf_coarsefish_3 (level 22) | stir x2 > add.ingredient.0 > heat.low > simmer x3 > add.ingredient.0 > simmer x3 > add.salt > simmer > heat.low > add.sugar > simmer x11 | 89.7 | 81.9 | 92.3 |
| 21-30 | best gear | b_hoptoad (level 25) | add.ingredient.0 x2 > heat.low > reduce x2 > simmer x10 > reduce > simmer | 100.0 | 100.0 | 98.8 |
| 21-30 | best gear | cen_xp3 (level 26) | add.ingredient.0 > add.ingredient.1 x2 > reduce > heat.low > reduce > simmer x10 > reduce x3 > simmer | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | cen_recast3 (level 28) | heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | baked_nebimon (level 32) | add.salt > simmer > heat.low > simmer > heat.low > add.salt > simmer > heat.low > add.ingredient.0 > reduce x4 > stir x3 > simmer > stir > simmer x5 > heat.low > simmer x6 | 99.7 | 99.1 | 92.7 |
| 31-40 | no gear | dw_broth (level 34) | heat.low > add.ingredient.0 > add.salt > reduce x4 > simmer x2 > stir > reduce x3 > stir > simmer x3 > heat.low > simmer x7 | 100.0 | 100.0 | 99.3 |
| 31-40 | no gear | cen_res_mob4 (level 36) | simmer > heat.low > simmer x2 > heat.low > add.salt > simmer x3 > add.ingredient.0 > simmer x2 > add.ingredient.0 > reduce x3 > simmer > reduce x3 > stir x2 > simmer > stir > simmer x4 > heat.low > simmer x5 > reduce > simmer | 99.3 | 97.0 | 97.3 |
| 31-40 | no gear | cen_fpr4 (level 38) | add.spice > simmer > heat.low > add.spice > add.ingredient.0 > cover > simmer > add.ingredient.0 > add.spice > reduce x4 > simmer > stir x3 > simmer > stir > simmer x4 > heat.low > simmer x4 > cover > simmer x2 | 98.8 | 97.9 | 82.8 |
| 31-40 | mid gear | baked_nebimon (level 32) | add.ingredient.0 x2 > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low | 100.0 | 100.0 | 90.3 |
| 31-40 | mid gear | dw_broth (level 34) | reduce > add.ingredient.0 > add.salt > reduce > heat.low > simmer > reduce > simmer x10 > reduce x3 > simmer | 100.0 | 100.0 | 97.6 |
| 31-40 | mid gear | cen_res_mob4 (level 36) | heat.low > add.ingredient.0 x2 > reduce x3 > simmer > reduce x3 > stir x2 > simmer x2 > stir > simmer > reduce > stir > simmer > stir > simmer > heat.low > simmer x5 | 100.0 | 100.0 | 96.9 |
| 31-40 | mid gear | cen_fpr4 (level 38) | heat.low > add.salt > simmer > add.ingredient.0 > add.ingredient.1 > add.spice > reduce x5 > simmer > reduce > stir > reduce x3 > stir x3 > simmer x5 > heat.low > simmer x5 | 100.0 | 99.7 | 89.7 |
| 31-40 | best gear | baked_nebimon (level 32) | add.ingredient.0 x2 > heat.low > reduce x3 > simmer x3 > reduce > simmer x7 > reduce > simmer | 100.0 | 100.0 | 96.5 |
| 31-40 | best gear | dw_broth (level 34) | reduce > add.salt > add.ingredient.0 > reduce > heat.low > reduce > simmer x10 > reduce x2 > simmer | 100.0 | 100.0 | 95.0 |
| 31-40 | best gear | cen_res_mob4 (level 36) | simmer > heat.low > add.ingredient.1 > simmer > reduce > add.ingredient.1 > simmer > reduce > add.ingredient.1 > simmer > reduce > add.ingredient.1 > simmer > reduce > add.ingredient.1 > simmer > reduce > add.ingredient.1 > simmer > reduce > add.ingredient.1 > simmer > reduce > add.ingredient.1 > simmer > reduce > add.ingredient.1 > simmer > reduce > add.ingredient.1 > simmer > reduce > add.ingredient.1 > simmer > reduce > add.ingredient.1 > simmer > reduce > add.ingredient.1 | 100.0 | 100.0 | 97.7 |
| 31-40 | best gear | cen_fpr4 (level 38) | add.ingredient.0 > heat.low > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low | 100.0 | 100.0 | 96.4 |
| 41-53 | no gear | cen_rest5 (level 42) | simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer | 100.0 | 100.0 | 96.6 |
| 41-53 | no gear | food_nightmarch (level 45) | simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer | 100.0 | 100.0 | 94.8 |
| 41-53 | no gear | pf_tendermeat_5 (level 48) | simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer | 100.0 | 100.0 | 84.3 |
| 41-53 | no gear | dan_carrotcake (level 52) | simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer | 100.0 | 100.0 | 87.1 |
| 41-53 | mid gear | cen_rest5 (level 42) | heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low | 100.0 | 100.0 | 99.6 |
| 41-53 | mid gear | food_nightmarch (level 45) | heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low | 100.0 | 100.0 | 97.5 |
| 41-53 | mid gear | pf_tendermeat_5 (level 48) | heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low | 100.0 | 100.0 | 95.3 |
| 41-53 | mid gear | dan_carrotcake (level 52) | simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer > heat.low > reduce > simmer | 100.0 | 100.0 | 93.9 |
| 41-53 | best gear | cen_rest5 (level 42) | add.ingredient.0 > reduce > add.ingredient.1 > reduce x2 > simmer x2 > reduce > simmer x2 > reduce > simmer x2 > reduce x2 > simmer > reduce > simmer | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | food_nightmarch (level 45) | heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low > simmer > reduce > heat.low | 100.0 | 100.0 | 98.4 |
| 41-53 | best gear | pf_tendermeat_5 (level 48) | reduce x2 > add.ingredient.0 > reduce x2 > heat.low > reduce > simmer > reduce > simmer x9 > reduce > simmer | 100.0 | 100.0 | 92.5 |
| 41-53 | best gear | dan_carrotcake (level 52) | signature.sweet > reduce > add.salt > heat.low > reduce x2 > simmer x11 > reduce > simmer | 100.0 | 100.0 | 94.5 |

## Economy parity

### Component stacks consumed per item made, failures included, over the recipes the legacy policy makes at least once in the cell (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Parity rule | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 4 | 1.81 | 1.82 | 0.99 | 99% | 97% | 94% | 1% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | mid gear | 4 | 1.81 | 1.81 | 1.00 | 99% | 98% | 96% | 7% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | best gear | 4 | 1.80 | 1.81 | 1.00 | 99% | 99% | 99% | 16% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | no gear | 4 | 2.05 | 2.11 | 0.97 | 96% | 92% | 92% | 1% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | mid gear | 4 | 2.01 | 2.02 | 1.00 | 99% | 99% | 96% | 11% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | best gear | 4 | 2.01 | 2.00 | 1.00 | 99% | 100% | 98% | 30% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | no gear | 4 | 1.63 | 1.62 | 1.00 | 98% | 97% | 89% | 7% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | mid gear | 4 | 1.61 | 1.61 | 1.00 | 99% | 98% | 94% | 15% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | best gear | 4 | 1.60 | 1.60 | 1.00 | 100% | 99% | 97% | 30% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | no gear | 4 | 2.28 | 2.29 | 1.00 | 98% | 98% | 93% | 8% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | mid gear | 4 | 2.26 | 2.30 | 0.98 | 99% | 97% | 94% | 14% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | best gear | 4 | 2.25 | 2.26 | 1.00 | 100% | 99% | 96% | 25% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | no gear | 4 | 1.21 | 1.21 | 1.00 | 99% | 99% | 89% | 43% | yes | 0% / 25% | within 10% | Passed |
| 41-53 | mid gear | 4 | 1.33 | 1.44 | 0.93 | 100% | 79% | 96% | 56% | yes | 0% / 0% | at most +10% | Passed |
| 41-53 | best gear | 4 | 1.34 | 1.37 | 0.98 | 99% | 92% | 95% | 77% | yes | 0% / 0% | within 10% | Passed |

### Recipes the legacy policy never makes in the cell, left out of the stack and quality comparison above (they still count in the never-made check)

| Band | Gear | Recipe | Level | New success | New avg quality |
| --- | --- | --- | --- | --- | --- |
| 41-53 | no gear | DantooineCarrotCake | 52 | 95% | 87% |

## XP rate

### Item crafts: XP per hour against the legacy loop (6.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per craft | Legacy actions per craft | Legacy XP per hour | New XP per craft | New actions per craft | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 589 | 4.8 | 73511 | 2488 | 13.2 | 84692 | 1.15 | - | Passed |
| 1-10 | no gear, 1 enhancement | 498 | 6.1 | 48684 | 2448 | 13.2 | 83503 | 1.72 | - | Passed |
| 1-10 | no gear, 2 enhancements | 176 | 6.0 | 17606 | 2444 | 12.9 | 85187 | 4.84 | - | Passed |
| 1-10 | no gear, 3 enhancements | 90 | 6.0 | 9000 | 2368 | 13.7 | 77982 | 8.66 | - | Passed |
| 1-10 | mid gear | 644 | 6.0 | 64781 | 2513 | 12.4 | 91458 | 1.41 | - | Passed |
| 1-10 | mid gear, 1 enhancement | 602 | 5.9 | 61514 | 2513 | 12.4 | 91245 | 1.48 | - | Passed |
| 1-10 | mid gear, 2 enhancements | 587 | 6.6 | 53030 | 2514 | 12.3 | 91740 | 1.73 | - | Passed |
| 1-10 | mid gear, 3 enhancements | 519 | 7.5 | 41440 | 2516 | 12.2 | 92618 | 2.23 | - | Passed |
| 1-10 | best gear | 710 | 6.5 | 65640 | 2554 | 10.5 | 109448 | 1.67 | - | Passed |
| 1-10 | best gear, 1 enhancement | 668 | 6.3 | 63621 | 2553 | 11.2 | 103018 | 1.62 | - | Passed |
| 1-10 | best gear, 2 enhancements | 667 | 6.8 | 58475 | 2538 | 11.1 | 102618 | 1.75 | - | Passed |
| 1-10 | best gear, 3 enhancements | 646 | 6.8 | 56713 | 2577 | 10.4 | 110961 | 1.96 | - | Passed |
| 11-20 | no gear | 564 | 8.7 | 39031 | 1303 | 12.4 | 47120 | 1.21 | - | Passed |
| 11-20 | no gear, 1 enhancement | 304 | 8.9 | 20379 | 1278 | 15.6 | 36795 | 1.81 | - | Passed |
| 11-20 | no gear, 2 enhancements | 90 | 6.5 | 8308 | 1234 | 16.0 | 34774 | 4.19 | - | Passed |
| 11-20 | no gear, 3 enhancements | 90 | 6.5 | 8308 | 1223 | 16.4 | 33647 | 4.05 | - | Passed |
| 11-20 | mid gear | 661 | 10.8 | 36706 | 1346 | 11.4 | 53119 | 1.45 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 621 | 11.8 | 31594 | 1328 | 14.2 | 42134 | 1.33 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 601 | 13.2 | 27274 | 1287 | 15.9 | 36447 | 1.34 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 579 | 13.6 | 25520 | 1319 | 14.8 | 40225 | 1.58 | - | Passed |
| 11-20 | best gear | 782 | 14.4 | 32501 | 1363 | 9.5 | 64410 | 1.98 | - | Passed |
| 11-20 | best gear, 1 enhancement | 766 | 15.1 | 30378 | 1367 | 13.1 | 47055 | 1.55 | - | Passed |
| 11-20 | best gear, 2 enhancements | 750 | 16.4 | 27508 | 1337 | 12.8 | 46981 | 1.71 | - | Passed |
| 11-20 | best gear, 3 enhancements | 731 | 15.8 | 27734 | 1346 | 14.1 | 42959 | 1.55 | - | Passed |
| 21-30 | no gear | 633 | 10.5 | 36274 | 1260 | 13.4 | 42395 | 1.17 | - | Passed |
| 21-30 | no gear, 1 enhancement | 613 | 10.6 | 34849 | 1192 | 14.8 | 36177 | 1.04 | - | Passed |
| 21-30 | no gear, 2 enhancements | 621 | 11.6 | 32271 | 1153 | 15.1 | 34287 | 1.06 | - | Passed |
| 21-30 | no gear, 3 enhancements | 570 | 12.4 | 27559 | 1122 | 15.7 | 32134 | 1.17 | - | Passed |
| 21-30 | mid gear | 686 | 16.1 | 25528 | 1291 | 13.0 | 44712 | 1.75 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 668 | 15.2 | 26317 | 1238 | 14.2 | 39260 | 1.49 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 653 | 15.6 | 25201 | 1243 | 14.4 | 38942 | 1.55 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 653 | 16.2 | 24194 | 1226 | 15.3 | 36120 | 1.49 | - | Passed |
| 21-30 | best gear | 781 | 21.6 | 21681 | 1305 | 11.6 | 50445 | 2.33 | - | Passed |
| 21-30 | best gear, 1 enhancement | 750 | 20.4 | 22054 | 1300 | 12.9 | 45245 | 2.05 | - | Passed |
| 21-30 | best gear, 2 enhancements | 751 | 20.7 | 21725 | 1285 | 13.1 | 44127 | 2.03 | - | Passed |
| 21-30 | best gear, 3 enhancements | 725 | 20.8 | 20881 | 1242 | 14.3 | 39185 | 1.88 | - | Passed |
| 31-40 | no gear | 637 | 12.7 | 30125 | 1378 | 15.1 | 41140 | 1.37 | - | Passed |
| 31-40 | no gear, 1 enhancement | 626 | 14.1 | 26707 | 1264 | 16.2 | 35158 | 1.32 | - | Passed |
| 31-40 | no gear, 2 enhancements | 594 | 14.2 | 25103 | 1299 | 16.9 | 34583 | 1.38 | - | Passed |
| 31-40 | no gear, 3 enhancements | 579 | 15.2 | 22871 | 1169 | 17.6 | 29902 | 1.31 | - | Passed |
| 31-40 | mid gear | 666 | 17.5 | 22794 | 1390 | 13.4 | 46572 | 2.04 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 659 | 18.0 | 22009 | 1349 | 15.8 | 38425 | 1.75 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 657 | 18.0 | 21902 | 1347 | 15.2 | 39767 | 1.82 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 636 | 18.0 | 21155 | 1270 | 16.4 | 34767 | 1.64 | - | Passed |
| 31-40 | best gear | 747 | 24.1 | 18569 | 1414 | 12.7 | 50030 | 2.69 | - | Passed |
| 31-40 | best gear, 1 enhancement | 732 | 24.8 | 17689 | 1404 | 13.2 | 47948 | 2.71 | - | Passed |
| 31-40 | best gear, 2 enhancements | 730 | 25.8 | 17000 | 1367 | 14.4 | 42707 | 2.51 | - | Passed |
| 31-40 | best gear, 3 enhancements | 707 | 24.3 | 17478 | 1378 | 14.7 | 42099 | 2.41 | - | Passed |
| 41-53 | no gear | 675 | 16.2 | 24967 | 1263 | 15.5 | 36648 | 1.47 | - | Passed |
| 41-53 | no gear, 1 enhancement | 616 | 15.3 | 24206 | 1260 | 17.8 | 31772 | 1.31 | - | Passed |
| 41-53 | no gear, 2 enhancements | 558 | 15.2 | 21956 | 1214 | 18.8 | 29024 | 1.32 | - | Passed |
| 41-53 | no gear, 3 enhancements | 486 | 15.3 | 18994 | 1095 | 19.5 | 25221 | 1.33 | - | Passed |
| 41-53 | mid gear | 813 | 23.0 | 21214 | 1315 | 14.4 | 41161 | 1.94 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 752 | 21.6 | 20907 | 1282 | 16.6 | 34844 | 1.67 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 685 | 19.6 | 20961 | 1276 | 17.0 | 33843 | 1.61 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 622 | 18.1 | 20574 | 1213 | 18.5 | 29513 | 1.43 | - | Passed |
| 41-53 | best gear | 1014 | 27.6 | 22046 | 1310 | 13.1 | 45156 | 2.05 | - | Passed |
| 41-53 | best gear, 1 enhancement | 980 | 28.4 | 20725 | 1320 | 15.5 | 38298 | 1.85 | - | Passed |
| 41-53 | best gear, 2 enhancements | 876 | 25.2 | 20828 | 1304 | 16.6 | 35386 | 1.70 | - | Passed |
| 41-53 | best gear, 3 enhancements | 776 | 22.5 | 20658 | 1282 | 16.9 | 34111 | 1.65 | - | Passed |

## Enhancement parity

### Rank 50, best gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 85%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 4 | 60 | 32% | 98% | 96% | 0.94 | 100% | 99.9% | 1.00 (+/- 0.00) | 1.07 | >= 1.00 | Passed |
| 41-45 | 2 | 4 | 60 | 63% | 99% | 87% | 1.73 | 100% | 98.9% | 1.98 (+/- 0.01) | 1.14 | >= 1.00 | Passed |
| 41-45 | 3 | 4 | 60 | 95% | 99% | 82% | 2.42 | 100% | 98.3% | 2.93 (+/- 0.03) | 1.21 | >= 1.00 | Passed |
| 46-50 | 1 | 4 | 60 | 32% | 99% | 85% | 0.84 | 100% | 98.4% | 0.98 (+/- 0.01) | 1.17 | >= 1.00 | Passed |
| 46-50 | 2 | 4 | 60 | 64% | 100% | 76% | 1.51 | 100% | 98.3% | 1.95 (+/- 0.02) | 1.29 | >= 1.00 | Passed |
| 46-50 | 3 | 4 | 60 | 96% | 99% | 67% | 1.98 | 98% | 96.0% | 2.79 (+/- 0.06) | 1.41 | >= 1.00 | Passed |
| 51-53 | 1 | 4 | 60 | 8% | 65% | 6% | 0.03 | 99% | 98.5% | 0.98 (+/- 0.01) | 28.44 | >= 1.00 | Passed |
| 51-53 | 2 | 4 | 60 | 16% | 35% | 4% | 0.03 | 99% | 96.9% | 1.88 (+/- 0.03) | 70.99 | >= 1.00 | Passed |
| 51-53 | 3 | 4 | 60 | 24% | 10% | 3% | 0.01 | 98% | 94.9% | 2.71 (+/- 0.06) | 360.83 | >= 1.00 | Passed |

### Thresholds, first / second / third slot, that would meet parity for the quality measured (the strictest set that keeps every case at least at today's, never falling from one slot to the next)

| Scope | Thresholds that meet parity | In steps of 5 | Thresholds in force |
| --- | --- | --- | --- |
| Every band together (the shared thresholds) | 99% / 100% / 100% | 95% / 100% / 100% | 50% / 85% / 85% |
| Band 41-45 alone | 99% / 100% / 100% | 95% / 100% / 100% | 50% / 85% / 85% |
| Band 46-50 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 85% |
| Band 51-53 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 85% |

### Rank 50, best gear: quality of the items the new minigame made, in whole percent, by band and slots filled

| Band | Slots filled | Items made | 10th percentile | 25th percentile | Median | 75th percentile | 90th percentile | Made at 100% |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 91% |
| 41-45 | 2 | 100% | 99% | 100% | 100% | 100% | 100% | 90% |
| 41-45 | 3 | 100% | 99% | 100% | 100% | 100% | 100% | 88% |
| 46-50 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 94% |
| 46-50 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 93% |
| 46-50 | 3 | 98% | 99% | 100% | 100% | 100% | 100% | 87% |
| 51-53 | 1 | 99% | 100% | 100% | 100% | 100% | 100% | 91% |
| 51-53 | 2 | 99% | 99% | 100% | 100% | 100% | 100% | 84% |
| 51-53 | 3 | 98% | 82% | 100% | 100% | 100% | 100% | 81% |

### Representative enhancements: for each recipe kind the real item at the best level the recipe accepts (the old window refuses one more than 5 levels above the recipe) whose penalty is the lower median of that level

| Recipe kind | Enhancement | Level | Progress penalty | Items of that kind and level (min / median / max penalty) | Recipes |
| --- | --- | --- | --- | --- | --- |
| Food | Cooking Enhancement - Resistance - Mobility V (`cen_res_mob5`) | 45 (tier V) | 60 | 28 items: 50 / 60.0 / 80 | 12 |

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
- The unperked scenario plays no perks except in the style-balance gate, which compares the two Chef styles. A Chef is played with the numbers of the game's own perk definitions at the rank the recipe is played at.
- Seasoned Palate is a switch the cook makes for each dish. The Chef scenarios play it off; the Seasoned Palate scenarios play it on for every dish. A real cook would choose per dish.
- Seconds per action is an estimate (8 s) until the client evidence measures it.
- Ingredient names are the resrefs here; the window shows item names.
- Every cook plays as a Slow Cooking (full line) Chef: the style from rank 20 and every other purchase of the line the rank allows. The unperked rank-50 result for gate 3 is in the `kitchen` scenario.
