# Crafting revamp: balance gates of the Kitchen (Flash Cooking Chef) minigame

- Command: `SWLOR.CraftSim gates --scenario kitchen-flash-cooking --gates materials,xp,enhancements --runs 40 --seed 1 --recipes-per-band 4 --repo . --out design/testing/crafting-revamp/kitchen-flash-cooking-gates.md`
- Seed 1; 40 runs per case.

## Inputs

- Scenario: Kitchen (Flash Cooking Chef) (`kitchen-flash-cooking`), Agriculture recipes, minigame Kitchen.
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
- Gates run: Economy parity, XP rate, Enhancement parity. The others were not run, so this report cannot show that the minigame passes.

## Verdicts

| Gate | Verdict | Summary |
| --- | --- | --- |
| Economy parity | Passed | Component stacks per item are within 10% of the legacy baseline where its success rate is at least 90%, and at most 10% above it where it is lower, at no less quality, in every band and gear tier. |
| XP rate | Passed | Item crafts: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.06x in 21-30 / no gear, 1 enhancement). |
| Enhancement parity | Passed | At rank 50 in best gear, the thresholds in force (50% / 85% / 85%) transfer at least 1.00 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 1.07 times today's). Thresholds that meet parity in every band for this quality: 99% / 100% / 100%. |

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
| 21-30 | no gear | 4 | 1.68 | 1.62 | 1.04 | 95% | 97% | 84% | 7% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | mid gear | 4 | 1.67 | 1.61 | 1.03 | 97% | 98% | 90% | 15% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | best gear | 4 | 1.60 | 1.60 | 1.00 | 100% | 99% | 96% | 30% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | no gear | 4 | 2.29 | 2.29 | 1.00 | 98% | 98% | 88% | 8% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | mid gear | 4 | 2.28 | 2.30 | 0.99 | 98% | 97% | 94% | 14% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | best gear | 4 | 2.26 | 2.26 | 1.00 | 99% | 99% | 97% | 25% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | no gear | 4 | 1.25 | 1.21 | 1.03 | 97% | 99% | 83% | 43% | yes | 0% / 25% | within 10% | Passed |
| 41-53 | mid gear | 4 | 1.33 | 1.44 | 0.93 | 100% | 79% | 95% | 56% | yes | 0% / 0% | at most +10% | Passed |
| 41-53 | best gear | 4 | 1.34 | 1.37 | 0.98 | 99% | 92% | 97% | 77% | yes | 0% / 0% | within 10% | Passed |

### Recipes the legacy policy never makes in the cell, left out of the stack and quality comparison above (they still count in the never-made check)

| Band | Gear | Recipe | Level | New success | New avg quality |
| --- | --- | --- | --- | --- | --- |
| 41-53 | no gear | DantooineCarrotCake | 52 | 95% | 90% |

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
| 21-30 | no gear | 633 | 10.5 | 36274 | 1213 | 13.3 | 41048 | 1.13 | - | Passed |
| 21-30 | no gear, 1 enhancement | 613 | 10.6 | 34849 | 1175 | 14.3 | 36951 | 1.06 | - | Passed |
| 21-30 | no gear, 2 enhancements | 621 | 11.6 | 32271 | 1160 | 14.6 | 35731 | 1.11 | - | Passed |
| 21-30 | no gear, 3 enhancements | 570 | 12.4 | 27559 | 1086 | 14.9 | 32856 | 1.19 | - | Passed |
| 21-30 | mid gear | 686 | 16.1 | 25528 | 1259 | 12.1 | 46736 | 1.83 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 668 | 15.2 | 26317 | 1244 | 13.9 | 40255 | 1.53 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 653 | 15.6 | 25201 | 1254 | 13.9 | 40492 | 1.61 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 653 | 16.2 | 24194 | 1180 | 14.6 | 36466 | 1.51 | - | Passed |
| 21-30 | best gear | 781 | 21.6 | 21681 | 1304 | 11.4 | 51656 | 2.38 | - | Passed |
| 21-30 | best gear, 1 enhancement | 750 | 20.4 | 22054 | 1279 | 12.4 | 46515 | 2.11 | - | Passed |
| 21-30 | best gear, 2 enhancements | 751 | 20.7 | 21725 | 1296 | 12.6 | 46328 | 2.13 | - | Passed |
| 21-30 | best gear, 3 enhancements | 725 | 20.8 | 20881 | 1244 | 13.8 | 40677 | 1.95 | - | Passed |
| 31-40 | no gear | 637 | 12.7 | 30125 | 1335 | 14.9 | 40438 | 1.34 | - | Passed |
| 31-40 | no gear, 1 enhancement | 626 | 14.1 | 26707 | 1287 | 15.7 | 36938 | 1.38 | - | Passed |
| 31-40 | no gear, 2 enhancements | 594 | 14.2 | 25103 | 1229 | 16.4 | 33771 | 1.35 | - | Passed |
| 31-40 | no gear, 3 enhancements | 579 | 15.2 | 22871 | 1188 | 17.0 | 31376 | 1.37 | - | Passed |
| 31-40 | mid gear | 666 | 17.5 | 22794 | 1386 | 13.0 | 47810 | 2.10 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 659 | 18.0 | 22009 | 1277 | 15.6 | 36782 | 1.67 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 657 | 18.0 | 21902 | 1342 | 14.9 | 40469 | 1.85 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 636 | 18.0 | 21155 | 1280 | 15.5 | 37162 | 1.76 | - | Passed |
| 31-40 | best gear | 747 | 24.1 | 18569 | 1415 | 11.8 | 53858 | 2.90 | - | Passed |
| 31-40 | best gear, 1 enhancement | 732 | 24.8 | 17689 | 1410 | 12.3 | 51503 | 2.91 | - | Passed |
| 31-40 | best gear, 2 enhancements | 730 | 25.8 | 17000 | 1394 | 14.0 | 44704 | 2.63 | - | Passed |
| 31-40 | best gear, 3 enhancements | 707 | 24.3 | 17478 | 1374 | 14.4 | 42797 | 2.45 | - | Passed |
| 41-53 | no gear | 675 | 16.2 | 24967 | 1241 | 15.5 | 35963 | 1.44 | - | Passed |
| 41-53 | no gear, 1 enhancement | 616 | 15.3 | 24206 | 1206 | 17.6 | 30826 | 1.27 | - | Passed |
| 41-53 | no gear, 2 enhancements | 558 | 15.2 | 21956 | 1186 | 18.3 | 29095 | 1.33 | - | Passed |
| 41-53 | no gear, 3 enhancements | 486 | 15.3 | 18994 | 1128 | 18.6 | 27338 | 1.44 | - | Passed |
| 41-53 | mid gear | 813 | 23.0 | 21214 | 1309 | 14.4 | 40758 | 1.92 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 752 | 21.6 | 20907 | 1286 | 16.1 | 35824 | 1.71 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 685 | 19.6 | 20961 | 1222 | 17.0 | 32428 | 1.55 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 622 | 18.1 | 20574 | 1237 | 18.2 | 30630 | 1.49 | - | Passed |
| 41-53 | best gear | 1014 | 27.6 | 22046 | 1315 | 13.0 | 45664 | 2.07 | - | Passed |
| 41-53 | best gear, 1 enhancement | 980 | 28.4 | 20725 | 1326 | 14.6 | 40825 | 1.97 | - | Passed |
| 41-53 | best gear, 2 enhancements | 876 | 25.2 | 20828 | 1303 | 16.1 | 36358 | 1.75 | - | Passed |
| 41-53 | best gear, 3 enhancements | 776 | 22.5 | 20658 | 1282 | 16.6 | 34784 | 1.68 | - | Passed |

## Enhancement parity

### Rank 50, best gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 85%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 4 | 60 | 32% | 98% | 96% | 0.94 | 100% | 99.6% | 1.00 (+/- 0.00) | 1.07 | >= 1.00 | Passed |
| 41-45 | 2 | 4 | 60 | 63% | 99% | 87% | 1.73 | 100% | 98.5% | 1.96 (+/- 0.02) | 1.14 | >= 1.00 | Passed |
| 41-45 | 3 | 4 | 60 | 95% | 99% | 82% | 2.42 | 100% | 99.0% | 2.96 (+/- 0.03) | 1.23 | >= 1.00 | Passed |
| 46-50 | 1 | 4 | 60 | 32% | 99% | 85% | 0.84 | 100% | 98.9% | 0.99 (+/- 0.01) | 1.18 | >= 1.00 | Passed |
| 46-50 | 2 | 4 | 60 | 64% | 100% | 76% | 1.51 | 100% | 98.0% | 1.94 (+/- 0.02) | 1.29 | >= 1.00 | Passed |
| 46-50 | 3 | 4 | 60 | 96% | 99% | 67% | 1.98 | 100% | 98.0% | 2.91 (+/- 0.04) | 1.47 | >= 1.00 | Passed |
| 51-53 | 1 | 4 | 60 | 8% | 65% | 6% | 0.03 | 99% | 98.1% | 0.98 (+/- 0.01) | 28.44 | >= 1.00 | Passed |
| 51-53 | 2 | 4 | 60 | 16% | 35% | 4% | 0.03 | 98% | 97.5% | 1.90 (+/- 0.03) | 71.70 | >= 1.00 | Passed |
| 51-53 | 3 | 4 | 60 | 24% | 10% | 3% | 0.01 | 99% | 94.9% | 2.69 (+/- 0.07) | 358.33 | >= 1.00 | Passed |

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
| 41-45 | 1 | 100% | 99% | 100% | 100% | 100% | 100% | 87% |
| 41-45 | 2 | 100% | 99% | 100% | 100% | 100% | 100% | 84% |
| 41-45 | 3 | 100% | 99% | 100% | 100% | 100% | 100% | 86% |
| 46-50 | 1 | 100% | 99% | 100% | 100% | 100% | 100% | 90% |
| 46-50 | 2 | 100% | 99% | 100% | 100% | 100% | 100% | 90% |
| 46-50 | 3 | 100% | 99% | 100% | 100% | 100% | 100% | 89% |
| 51-53 | 1 | 99% | 99% | 100% | 100% | 100% | 100% | 89% |
| 51-53 | 2 | 98% | 99% | 100% | 100% | 100% | 100% | 86% |
| 51-53 | 3 | 99% | 76% | 100% | 100% | 100% | 100% | 79% |

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
- Every cook plays as a Flash Cooking (full line) Chef: the style from rank 20 and every other purchase of the line the rank allows. The unperked rank-50 result for gate 3 is in the `kitchen` scenario.
