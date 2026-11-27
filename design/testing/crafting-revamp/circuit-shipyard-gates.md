# Crafting revamp: balance gates of the Circuit (Capital hulls at a Shipyard, best droid) minigame

- Command: `SWLOR.CraftSim gates --scenario circuit-shipyard --runs 100 --seed 1 --all-recipes --out design/testing/crafting-revamp/circuit-shipyard-gates.md`
- Seed 1; 100 runs per case.

## Inputs

- Scenario: Circuit (Capital hulls at a Shipyard, best droid) (`circuit-shipyard`), Engineering recipes, minigame Circuit.
- Quality: the quality meter's share of its maximum when the item is made, in points (0-100). A broken or abandoned piece counts 0, so every average includes the crafts that failed.
- Crafter: the rank the scenario's cohort gives each recipe (by default the recipe's level, at most 50, so a crafter works at their own level), with no food or status bonuses and no perks unless a gate names a style or the scenario's limitations say it plays a perk loadout. Gear levels are the plan's none, middle and best tier of the skill's crafting gear:
  - no gear (Control 0, Craftsmanship 0, CP 0)
  - mid gear (Control 13, Craftsmanship 12, CP 20)
  - best gear (Control 29, Craftsmanship 30, CP 37)
- Recipes: 41-53: 11 recipes played.
- Adaptive player: depth 3, forecast samples per result 2, budget 4000 engine copies per decision, 0.5% of value lost per extra action, heuristic DefaultLeafHeuristic. Each attempt's dice come from its own stream of the seed.
- Fixed-rotation solver: beam 8, cycles up to 3, 8 adaptive lines as seeds, up to 40 actions, 64 training attempts per local-search round and 150 selection attempts for each of 12 finalists, local search from the best 3 candidate(s) for up to 3 round(s) each. Gate 1 searches one sequence for each band and gear tier across the band's sampled recipes (every recipe weighing the same; an action a recipe's engine cannot take is skipped there), and also each recipe's own best sequence as a reported metric. The winner is chosen on separate attempts and then scored on a third set (hold-out) with 100 attempts per recipe.
- Legacy comparator (gates 5 and 6): the old minigame's lookahead player (`SWLOR.CraftSim baseline`), replayed here with the same seed on the same recipes, ranks and gear.
- Time: 8.0 s per action in the new window and 6.0 s in the old one, plus 0.0 s per craft in both. These are estimates; the client evidence replaces them.
- XP multiplier: none; the minigame pays today's base XP (`CraftMinigameXp`).
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
  - `RecipesPerBand` = all: recipes played per band
  - `SolverOptions` = the scenario's: effort of the fixed-rotation solver
  - `ActionLimit` = 100: actions after which an attempt counts as stalled
- Length target: 10-20 actions (item craft at level).
- Enhancement thresholds in force (`CraftOutcomeRules`, read by gate 7): 50% / 85% / 100% for slots 1, 2 and 3. The scenario applies enhancements to its engine's progress meter.
- Styles compared: Overclock (no lines) vs Safe Mode (no lines) (from rank 20); Overclock (both lines) vs Safe Mode (both lines) (from rank 20).

## Verdicts

| Gate | Verdict | Summary |
| --- | --- | --- |
| No fixed rotation | Failed | One fixed rotation comes within 10 points of adaptive play in 3 case(s): 41-53 / no gear; 41-53 / mid gear; 41-53 / best gear (smallest margin -0.0 in 41-53 / mid gear). The best rotation for each recipe alone, a reported metric, is -0.0 to -0.0 points behind adaptive play. |
| Style balance | Passed | Every style is the better choice on at least 30% of recipes and no pair differs by more than 5 points on average. |
| Perks optional | Passed | An unperked rank-50 crafter in best gear reaches maximum quality on at least 50% of crafts in every at-level band (lowest 99%). |
| Length | Passed | Every band and gear tier takes 10-20 actions (item craft at level) on average. |
| Economy parity | Passed | Component stacks per item are within 10% of the legacy baseline where its success rate is at least 90%, and at most 10% above it where it is lower, at no less quality, in every band and gear tier. |
| XP rate | Passed | Item crafts: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.26x in 41-53 / no gear, 1 enhancement). |
| Enhancement parity | Passed | At rank 50 in best gear, the thresholds in force (50% / 85% / 100%) transfer at least 1.00 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 1.10 times today's). Thresholds that meet parity in every band for this quality: 100% / 100% / 100%. |

## No fixed rotation

### Average quality: adaptive play against the best single fixed rotation of each band and gear tier (quality points; a broken piece counts 0)

| Band | Gear | Recipes played | Adaptive avg quality | Best single rotation avg quality | Margin | Required | Best rotation per recipe avg quality (reported) | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-53 | no gear | 11 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Failed |
| 41-53 | mid gear | 11 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Failed |
| 41-53 | best gear | 11 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Failed |

### The single rotation found for each band and gear tier, searched across the band's sampled recipes (average quality in points)

| Band | Gear | Best single rotation for the band | Training | Selection | Hold-out | Adaptive on these recipes |
| --- | --- | --- | --- | --- | --- | --- |
| 41-53 | no gear | boost > tune x6 > discharge_calibration > tune > wire x4 > discharge_assembly > tune > wire > discharge_assembly > tune > patch > tune x4 > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | boost > tune x5 > discharge_calibration > tune x2 > wire x4 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | boost > tune x5 > discharge_calibration > tune > wire x4 > discharge_assembly | 100.0 | 100.0 | 100.0 | 100.0 |

### Reported metric, not gated: the best fixed rotation for each recipe alone, and what the band's single rotation scores on the same recipe (average quality in points)

| Band | Gear | Recipe | Best rotation for this recipe alone (reported) | Its hold-out | The single rotation on this recipe | Adaptive on this recipe |
| --- | --- | --- | --- | --- | --- | --- |
| 41-53 | no gear | sd_neutralthrant (level 45) | boost > tune x6 > discharge_calibration > wire x5 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | sd_republicthran (level 45) | boost > tune x6 > discharge_calibration > wire x2 > tune > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | sd_siththranta (level 45) | boost > tune x6 > discharge_calibration > tune > boost > wire x3 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | sd_huttcorvette (level 45) | boost > tune x6 > discharge_calibration > tune > wire x5 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | sd_jehaveyir (level 45) | boost > tune x4 > discharge_calibration > boost > tune x3 > discharge_calibration > wire x6 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | sd_chisstrireme (level 45) | boost > tune x6 > discharge_calibration > tune > wire x5 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | sd_corelliangunb (level 45) | boost > tune x4 > discharge_calibration > boost > tune x2 > discharge_calibration > tune > wire x5 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | sd_czerkaarmored (level 45) | boost > tune x6 > discharge_calibration > wire x2 > tune > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | sd_terminuscorsa (level 45) | boost > tune x4 > discharge_calibration > boost > tune x3 > discharge_calibration > wire x5 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | sd_crusadercorve (level 45) | boost > tune x6 > discharge_calibration > tune > wire x5 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | sd_republiccentu (level 50) | boost > tune x6 > discharge_calibration > wire x2 > tune > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | sd_neutralthrant (level 45) | boost > tune x6 > discharge_calibration > boost > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | sd_republicthran (level 45) | boost > tune x6 > discharge_calibration > wire x5 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | sd_siththranta (level 45) | boost > tune x4 > discharge_calibration > wire > tune x4 > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | sd_huttcorvette (level 45) | boost > tune x6 > discharge_calibration > boost > wire x4 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | sd_jehaveyir (level 45) | boost > tune x4 > discharge_calibration > tune x4 > wire x3 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | sd_chisstrireme (level 45) | boost > tune x6 > discharge_calibration > boost > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | sd_corelliangunb (level 45) | boost > tune x5 > discharge_calibration > tune x2 > wire x4 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | sd_czerkaarmored (level 45) | boost > tune x6 > discharge_calibration > wire x5 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | sd_terminuscorsa (level 45) | boost > tune x4 > discharge_calibration > tune x5 > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | sd_crusadercorve (level 45) | boost > tune x6 > discharge_calibration > boost > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | sd_republiccentu (level 50) | boost > tune x6 > discharge_calibration > wire x5 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | sd_neutralthrant (level 45) | boost > tune x5 > discharge_calibration > boost > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | sd_republicthran (level 45) | boost > tune x5 > discharge_calibration > tune > wire x4 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | sd_siththranta (level 45) | boost > tune x5 > discharge_calibration > tune > wire x4 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | sd_huttcorvette (level 45) | boost > tune x4 > discharge_calibration > tune > wire > tune x2 > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | sd_jehaveyir (level 45) | boost > tune x4 > discharge_calibration > tune x2 > wire > tune > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | sd_chisstrireme (level 45) | boost > tune x5 > discharge_calibration > boost > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | sd_corelliangunb (level 45) | boost > tune x5 > discharge_calibration > wire > boost > wire x2 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | sd_czerkaarmored (level 45) | boost > tune x5 > discharge_calibration > tune > wire x4 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | sd_terminuscorsa (level 45) | boost > tune x4 > discharge_calibration > tune x2 > wire > tune > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | sd_crusadercorve (level 45) | boost > tune x5 > discharge_calibration > boost > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | sd_republiccentu (level 50) | boost > tune x5 > discharge_calibration > tune > wire x4 > discharge_assembly | 100.0 | 100.0 | 100.0 |

## Style balance

### Style pairs: how often each style is better, and the average quality (quality points; ties count half a win each)

| Style pair | Gear | Recipes | First better | Second better | First avg quality | Second avg quality | Difference | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Overclock (no lines) vs Safe Mode (no lines) | no gear | 11 | 50% | 50% | 100.0 | 100.0 | +0.0 | Passed |
| Overclock (no lines) vs Safe Mode (no lines) | mid gear | 11 | 50% | 50% | 100.0 | 100.0 | +0.0 | Passed |
| Overclock (no lines) vs Safe Mode (no lines) | best gear | 11 | 50% | 50% | 100.0 | 100.0 | +0.0 | Passed |
| Overclock (both lines) vs Safe Mode (both lines) | no gear | 11 | 50% | 50% | 100.0 | 100.0 | +0.0 | Passed |
| Overclock (both lines) vs Safe Mode (both lines) | mid gear | 11 | 50% | 50% | 100.0 | 100.0 | +0.0 | Passed |
| Overclock (both lines) vs Safe Mode (both lines) | best gear | 11 | 50% | 50% | 100.0 | 100.0 | +0.0 | Passed |

## Perks optional

### Rank 50, best gear, no perks: adaptive attempts that reach maximum quality, by recipe band

| Band | Recipes played | Attempts | Reach maximum quality | Make the item | Avg quality of items made | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 10 | 1000 | 99% | 100% | 100% | >= 50% | Passed |
| 46-50 | 1 | 100 | 99% | 100% | 100% | >= 50% | Passed |

## Length

### Actions per attempt of the adaptive player at level (item craft at level)

| Band | Gear | Recipes played | Mean actions per attempt | Shortest recipe | Longest recipe | Stalled attempts | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-53 | no gear | 11 | 14.4 | 14.0 | 15.0 | 0 | 10-20 | Passed |
| 41-53 | mid gear | 11 | 13.0 | 12.4 | 13.6 | 0 | 10-20 | Passed |
| 41-53 | best gear | 11 | 11.8 | 11.3 | 12.2 | 0 | 10-20 | Passed |

## Economy parity

### Component stacks consumed per item made, failures included, over the recipes the legacy policy makes at least once in the cell (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Parity rule | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-53 | no gear | 11 | 5.91 | 5.92 | 1.00 | 100% | 100% | 100% | 42% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | mid gear | 11 | 5.91 | 5.94 | 0.99 | 100% | 99% | 100% | 68% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | best gear | 11 | 5.91 | 5.94 | 0.99 | 100% | 99% | 100% | 92% | yes | 0% / 0% | within 10% | Passed |

## XP rate

### Item crafts: XP per hour against the legacy loop (6.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per craft | Legacy actions per craft | Legacy XP per hour | New XP per craft | New actions per craft | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-53 | no gear | 848 | 18.4 | 27656 | 1200 | 14.4 | 37533 | 1.36 | - | Passed |
| 41-53 | no gear, 1 enhancement | 787 | 17.2 | 27494 | 1199 | 15.6 | 34642 | 1.26 | - | Passed |
| 41-53 | no gear, 2 enhancements | 712 | 18.1 | 23618 | 1200 | 15.8 | 34124 | 1.44 | - | Passed |
| 41-53 | no gear, 3 enhancements | 635 | 18.1 | 21046 | 1200 | 15.8 | 34244 | 1.63 | - | Passed |
| 41-53 | mid gear | 1001 | 24.6 | 24411 | 1200 | 13.0 | 41578 | 1.70 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 970 | 24.5 | 23730 | 1200 | 14.2 | 38107 | 1.61 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 872 | 21.5 | 24349 | 1200 | 14.2 | 38034 | 1.56 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 785 | 20.9 | 22486 | 1200 | 14.2 | 38051 | 1.69 | - | Passed |
| 41-53 | best gear | 1148 | 28.9 | 23828 | 1200 | 11.8 | 45631 | 1.92 | - | Passed |
| 41-53 | best gear, 1 enhancement | 1134 | 28.9 | 23579 | 1200 | 12.6 | 42798 | 1.82 | - | Passed |
| 41-53 | best gear, 2 enhancements | 1010 | 26.4 | 22996 | 1200 | 12.8 | 42316 | 1.84 | - | Passed |
| 41-53 | best gear, 3 enhancements | 981 | 26.2 | 22477 | 1200 | 12.8 | 42274 | 1.88 | - | Passed |

## Enhancement parity

### Rank 50, best gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 100%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 10 | 60 | 30% | 99% | 92% | 0.91 | 100% | 100.0% | 1.00 (+/- 0.00) | 1.10 | >= 1.00 | Passed |
| 41-45 | 2 | 10 | 60 | 61% | 99% | 83% | 1.64 | 100% | 100.0% | 2.00 (+/- 0.00) | 1.22 | >= 1.00 | Passed |
| 41-45 | 3 | 10 | 60 | 91% | 99% | 78% | 2.31 | 100% | 100.0% | 2.98 (+/- 0.00) | 1.29 | >= 1.00 | Passed |
| 46-50 | 1 | 1 | 60 | 32% | 99% | 81% | 0.79 | 100% | 100.0% | 1.00 (+/- 0.00) | 1.26 | >= 1.00 | Passed |
| 46-50 | 2 | 1 | 60 | 65% | 98% | 67% | 1.29 | 100% | 100.0% | 2.00 (+/- 0.00) | 1.55 | >= 1.00 | Passed |
| 46-50 | 3 | 1 | 60 | 97% | 100% | 62% | 1.83 | 100% | 100.0% | 3.00 (+/- 0.00) | 1.64 | >= 1.00 | Passed |

### Thresholds, first / second / third slot, that would meet parity for the quality measured (the strictest set that keeps every case at least at today's, never falling from one slot to the next)

| Scope | Thresholds that meet parity | In steps of 5 | Thresholds in force |
| --- | --- | --- | --- |
| Every band together (the shared thresholds) | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 100% |
| Band 41-45 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 100% |
| Band 46-50 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 100% |

### Rank 50, best gear: quality of the items the new minigame made, in whole percent, by band and slots filled

| Band | Slots filled | Items made | 10th percentile | 25th percentile | Median | 75th percentile | 90th percentile | Made at 100% |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 99% |
| 41-45 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 41-45 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 46-50 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 99% |
| 46-50 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 99% |
| 46-50 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 100% |

### Representative enhancements: for each recipe kind the real item at the best level the recipe accepts (the old window refuses one more than 5 levels above the recipe) whose penalty is the lower median of that level

| Recipe kind | Enhancement | Level | Progress penalty | Items of that kind and level (min / median / max penalty) | Recipes |
| --- | --- | --- | --- | --- | --- |
| Starship | Starship Enhancement - Shield Recharge Rate V (`sen_shrech5`) | 45 (tier V) | 60 | 24 items: 50 / 65.0 / 80 | 11 |

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
