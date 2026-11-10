# Crafting revamp: balance gates of the Plasma Forge (Cold Working alone) minigame

- Command: `SWLOR.CraftSim gates --scenario forge-cold --gates length,materials,xp,enhancements --runs 30 --seed 1 --recipes-per-band 6 --repo . --out design/testing/crafting-revamp/forge-cold-gates.md`
- Seed 1; 30 runs per case.

## Inputs

- Scenario: Plasma Forge (Cold Working alone) (`forge-cold`), Smithery recipes, minigame Forge.
- Quality: the quality meter's share of its maximum when the item is made, in points (0-100). A broken or abandoned piece counts 0, so every average includes the crafts that failed.
- Crafter: the rank the scenario's cohort gives each recipe (by default the recipe's level, at most 50, so a crafter works at their own level), with no food or status bonuses and no perks unless a gate names a style or the scenario's limitations say it plays a perk loadout. Gear levels are the plan's none, middle and best tier of the skill's crafting gear:
  - no gear (Control 0, Craftsmanship 0, CP 0)
  - mid gear (Control 13, Craftsmanship 12, CP 23)
  - best gear (Control 29, Craftsmanship 30, CP 37)
  - fully modded gear (Control 119, Craftsmanship 120, CP 37)
- Recipes: 1-10: 6 of 120; 11-20: 6 of 144; 21-30: 6 of 196; 31-40: 6 of 135; 41-53: 6 of 193 recipes played.
- Adaptive player: depth 4, forecast samples per result 2, budget 6000 engine copies per decision, 0.5% of value lost per extra action, heuristic DefaultLeafHeuristic. Each attempt's dice come from its own stream of the seed.
- Fixed-rotation solver: beam 8, cycles up to 3, 8 adaptive lines as seeds, up to 40 actions, 64 training attempts per local-search round and 150 selection attempts for each of 12 finalists, local search from the best 3 candidate(s) for up to 3 round(s) each. Gate 1 searches one sequence for each band and gear tier across the band's sampled recipes (every recipe weighing the same; an action a recipe's engine cannot take is skipped there), and also each recipe's own best sequence as a reported metric. The winner is chosen on separate attempts and then scored on a third set (hold-out) with 30 attempts per recipe.
- Legacy comparator (gates 5 and 6): the old minigame's lookahead player (`SWLOR.CraftSim baseline`), replayed here with the same seed on the same recipes, ranks and gear.
- Time: 8.0 s per action in the new window and 6.0 s in the old one, plus 0.0 s per craft in both. These are estimates; the client evidence replaces them.
- XP multiplier on the base XP of every craft, failures included, as the game server applies it (`CraftMinigameXp`): 1-10 1.97x, 11-20 1.20x, 21-30 1.46x, 31-40 2.05x, 41-53 1.09x. The new side's XP below includes it; the legacy loop's does not.
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
- Length target: 8-20 actions (item craft at level, perked build).
- Enhancement thresholds in force (`CraftOutcomeRules`, read by gate 7): 50% / 85% / 100% for slots 1, 2 and 3. The scenario applies enhancements to its engine's progress meter.
- Styles: none offered, so the style-balance gate is not applicable.
- Gates run: Length, Economy parity, XP rate, Enhancement parity. The others were not run, so this report cannot show that the minigame passes.

## Verdicts

| Gate | Verdict | Summary |
| --- | --- | --- |
| Length | Passed | Every band and gear tier takes 8-20 actions (item craft at level, perked build) on average. |
| Economy parity | Passed | Component stacks per item are within 10% of the legacy baseline where its success rate is at least 90%, and at most 10% above it where it is lower, at no less quality, in every band and gear tier. |
| XP rate | Passed | Item crafts: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.02x in 11-20 / no gear). |
| Enhancement parity | Passed | At rank 50 in fully modded gear, the thresholds in force (50% / 85% / 100%) transfer at least 0.90 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 0.98 times today's). Thresholds that meet parity in every band for this quality: 39% / 61% / 94%. |

## Length

### Actions per attempt of the adaptive player at level (item craft at level, perked build)

| Band | Gear | Recipes played | Mean actions per attempt | Shortest recipe | Longest recipe | Stalled attempts | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 12.0 | 11.5 | 12.9 | 0 | 8-20 | Passed |
| 1-10 | mid gear | 6 | 13.5 | 11.9 | 17.4 | 0 | 8-20 | Passed |
| 1-10 | best gear | 6 | 12.2 | 11.1 | 14.8 | 0 | 8-20 | Passed |
| 1-10 | fully modded gear | 6 | 7.6 | 6.7 | 8.6 | 0 | 8-20 | Reported |
| 11-20 | no gear | 6 | 11.3 | 9.4 | 15.2 | 0 | 8-20 | Passed |
| 11-20 | mid gear | 6 | 13.8 | 10.1 | 17.0 | 0 | 8-20 | Passed |
| 11-20 | best gear | 6 | 13.0 | 8.5 | 17.9 | 0 | 8-20 | Passed |
| 11-20 | fully modded gear | 6 | 8.0 | 5.5 | 10.4 | 0 | 8-20 | Reported |
| 21-30 | no gear | 6 | 10.9 | 7.7 | 14.0 | 0 | 8-20 | Passed |
| 21-30 | mid gear | 6 | 10.9 | 8.3 | 12.8 | 0 | 8-20 | Passed |
| 21-30 | best gear | 6 | 10.4 | 9.0 | 12.7 | 0 | 8-20 | Passed |
| 21-30 | fully modded gear | 6 | 7.1 | 6.3 | 8.3 | 0 | 8-20 | Reported |
| 31-40 | no gear | 6 | 11.4 | 9.4 | 13.0 | 0 | 8-20 | Reported |
| 31-40 | mid gear | 6 | 10.8 | 8.4 | 13.4 | 0 | 8-20 | Passed |
| 31-40 | best gear | 6 | 10.5 | 9.1 | 12.2 | 0 | 8-20 | Passed |
| 31-40 | fully modded gear | 6 | 7.6 | 6.7 | 8.9 | 0 | 8-20 | Reported |
| 41-53 | no gear | 6 | 13.1 | 10.3 | 14.8 | 0 | 8-20 | Reported |
| 41-53 | mid gear | 6 | 13.2 | 11.9 | 15.1 | 0 | 8-20 | Reported |
| 41-53 | best gear | 6 | 13.0 | 12.0 | 14.3 | 0 | 8-20 | Passed |
| 41-53 | fully modded gear | 6 | 7.9 | 6.5 | 9.3 | 0 | 8-20 | Reported |

## Economy parity

### Component stacks consumed per item made, failures included, over the recipes the legacy policy makes at least once in the cell (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Parity rule | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 2.82 | 2.70 | 1.04 | 92% | 98% | 63% | 1% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | mid gear | 6 | 2.73 | 2.70 | 1.01 | 97% | 98% | 89% | 11% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | best gear | 6 | 2.67 | 2.67 | 1.00 | 100% | 100% | 98% | 28% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | fully modded gear | 6 | 2.67 | 2.68 | 1.00 | 100% | 99% | 100% | 79% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | no gear | 6 | 2.67 | 2.66 | 1.00 | 91% | 91% | 61% | 2% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | mid gear | 6 | 2.63 | 2.55 | 1.03 | 93% | 97% | 84% | 13% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | best gear | 6 | 2.52 | 2.52 | 1.00 | 99% | 99% | 95% | 29% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | fully modded gear | 6 | 2.50 | 2.50 | 1.00 | 100% | 100% | 100% | 93% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | no gear | 6 | 3.40 | 3.19 | 1.07 | 90% | 99% | 68% | 8% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | mid gear | 6 | 3.17 | 3.18 | 1.00 | 100% | 99% | 89% | 16% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | best gear | 6 | 3.17 | 3.18 | 1.00 | 100% | 99% | 93% | 29% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | fully modded gear | 6 | 3.17 | 3.19 | 0.99 | 100% | 99% | 99% | 90% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | no gear | 6 | 2.95 | 2.86 | 1.03 | 94% | 98% | 60% | 22% | yes | 0% / 0% | within 10% | Reported |
| 31-40 | mid gear | 6 | 2.86 | 2.87 | 1.00 | 98% | 98% | 72% | 38% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | best gear | 6 | 2.83 | 2.85 | 0.99 | 100% | 99% | 84% | 49% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | fully modded gear | 6 | 2.83 | 2.89 | 0.98 | 100% | 97% | 98% | 87% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | no gear | 6 | 3.01 | 3.04 | 0.99 | 99% | 98% | 60% | 43% | yes | 0% / 17% | within 10% | Reported |
| 41-53 | mid gear | 6 | 3.83 | 4.00 | 0.96 | 100% | 86% | 70% | 56% | yes | 0% / 0% | at most +10% | Reported |
| 41-53 | best gear | 6 | 3.83 | 3.89 | 0.98 | 100% | 94% | 82% | 75% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | fully modded gear | 6 | 3.83 | 3.83 | 1.00 | 100% | 100% | 98% | 91% | yes | 0% / 0% | within 10% | Passed |

### Recipes the legacy policy never makes in the cell, left out of the stack and quality comparison above (they still count in the never-made check)

| Band | Gear | Recipe | Level | New success | New avg quality |
| --- | --- | --- | --- | --- | --- |
| 41-53 | no gear | ImmortalBelt | 52 | 100% | 23% |

## XP rate

### Item crafts: XP per hour against the legacy loop (6.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per craft | Legacy actions per craft | Legacy XP per hour | New XP per craft | New actions per craft | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 593 | 6.0 | 59489 | 1842 | 12.0 | 69152 | 1.16 | - | Passed |
| 1-10 | no gear, 1 enhancement | 442 | 6.9 | 38624 | 1555 | 11.6 | 60142 | 1.56 | - | Passed |
| 1-10 | no gear, 2 enhancements | 305 | 6.6 | 27851 | 1050 | 11.3 | 41913 | 1.50 | - | Passed |
| 1-10 | no gear, 3 enhancements | 209 | 6.9 | 18130 | 619 | 11.2 | 24777 | 1.37 | - | Passed |
| 1-10 | mid gear | 656 | 8.8 | 44601 | 2200 | 13.5 | 73460 | 1.65 | - | Passed |
| 1-10 | mid gear, 1 enhancement | 663 | 8.8 | 45415 | 1898 | 13.6 | 62995 | 1.39 | - | Passed |
| 1-10 | mid gear, 2 enhancements | 596 | 9.6 | 37414 | 1380 | 13.2 | 47198 | 1.26 | - | Passed |
| 1-10 | mid gear, 3 enhancements | 549 | 10.2 | 32305 | 950 | 13.1 | 32654 | 1.01 | - | Passed |
| 1-10 | best gear | 770 | 10.3 | 44808 | 2340 | 12.2 | 86015 | 1.92 | - | Passed |
| 1-10 | best gear, 1 enhancement | 740 | 10.9 | 40867 | 2213 | 13.3 | 74690 | 1.83 | - | Passed |
| 1-10 | best gear, 2 enhancements | 712 | 11.0 | 38779 | 1899 | 14.2 | 59959 | 1.55 | - | Passed |
| 1-10 | best gear, 3 enhancements | 702 | 11.2 | 37692 | 1446 | 14.8 | 44056 | 1.17 | - | Passed |
| 1-10 | fully modded gear | 1071 | 8.5 | 75652 | 2363 | 7.6 | 140519 | 1.86 | - | Passed |
| 1-10 | fully modded gear, 1 enhancement | 1049 | 8.5 | 74450 | 2364 | 8.7 | 121717 | 1.63 | - | Passed |
| 1-10 | fully modded gear, 2 enhancements | 1047 | 8.3 | 75577 | 2364 | 9.5 | 111704 | 1.48 | - | Passed |
| 1-10 | fully modded gear, 3 enhancements | 1053 | 8.8 | 71474 | 2350 | 10.6 | 100187 | 1.40 | - | Passed |
| 11-20 | no gear | 566 | 7.9 | 43163 | 1108 | 11.3 | 44147 | 1.02 | - | Passed |
| 11-20 | no gear, 1 enhancement | 409 | 7.9 | 30965 | 1028 | 12.1 | 38308 | 1.24 | - | Passed |
| 11-20 | no gear, 2 enhancements | 266 | 7.2 | 22260 | 917 | 12.3 | 33624 | 1.51 | - | Passed |
| 11-20 | no gear, 3 enhancements | 262 | 7.7 | 20334 | 676 | 12.2 | 24854 | 1.22 | - | Passed |
| 11-20 | mid gear | 664 | 11.1 | 35793 | 1285 | 13.8 | 41903 | 1.17 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 658 | 11.8 | 33330 | 1305 | 15.1 | 38900 | 1.17 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 622 | 12.4 | 30075 | 1114 | 15.8 | 31675 | 1.05 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 597 | 13.5 | 26634 | 1030 | 16.1 | 28842 | 1.08 | - | Passed |
| 11-20 | best gear | 766 | 14.4 | 31957 | 1400 | 13.0 | 48554 | 1.52 | - | Passed |
| 11-20 | best gear, 1 enhancement | 759 | 15.2 | 30033 | 1320 | 14.0 | 42451 | 1.41 | - | Passed |
| 11-20 | best gear, 2 enhancements | 744 | 16.0 | 27962 | 1280 | 15.4 | 37307 | 1.33 | - | Passed |
| 11-20 | best gear, 3 enhancements | 753 | 16.5 | 27328 | 1205 | 16.4 | 33147 | 1.21 | - | Passed |
| 11-20 | fully modded gear | 1155 | 14.2 | 48714 | 1439 | 8.0 | 80578 | 1.65 | - | Passed |
| 11-20 | fully modded gear, 1 enhancement | 1118 | 14.2 | 47354 | 1440 | 8.5 | 75916 | 1.60 | - | Passed |
| 11-20 | fully modded gear, 2 enhancements | 1135 | 14.6 | 46523 | 1439 | 9.6 | 67540 | 1.45 | - | Passed |
| 11-20 | fully modded gear, 3 enhancements | 1136 | 14.7 | 46374 | 1407 | 10.4 | 60645 | 1.31 | - | Passed |
| 21-30 | no gear | 639 | 10.2 | 37566 | 1400 | 10.9 | 57612 | 1.53 | - | Passed |
| 21-30 | no gear, 1 enhancement | 618 | 11.5 | 32333 | 1564 | 12.7 | 55532 | 1.72 | - | Passed |
| 21-30 | no gear, 2 enhancements | 592 | 12.4 | 28664 | 1294 | 13.5 | 43271 | 1.51 | - | Passed |
| 21-30 | no gear, 3 enhancements | 575 | 13.5 | 25564 | 786 | 13.7 | 25911 | 1.01 | - | Passed |
| 21-30 | mid gear | 691 | 16.5 | 25152 | 1659 | 10.9 | 68244 | 2.71 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 679 | 16.8 | 24257 | 1696 | 12.6 | 60648 | 2.50 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 665 | 17.4 | 22996 | 1644 | 14.0 | 52851 | 2.30 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 647 | 17.6 | 22047 | 1396 | 15.2 | 41223 | 1.87 | - | Passed |
| 21-30 | best gear | 770 | 21.3 | 21631 | 1694 | 10.4 | 73309 | 3.39 | - | Passed |
| 21-30 | best gear, 1 enhancement | 754 | 21.6 | 20987 | 1723 | 11.6 | 66952 | 3.19 | - | Passed |
| 21-30 | best gear, 2 enhancements | 752 | 22.0 | 20504 | 1721 | 13.2 | 58875 | 2.87 | - | Passed |
| 21-30 | best gear, 3 enhancements | 744 | 21.9 | 20368 | 1665 | 14.3 | 52568 | 2.58 | - | Passed |
| 21-30 | fully modded gear | 1133 | 20.7 | 32818 | 1744 | 7.1 | 110635 | 3.37 | - | Passed |
| 21-30 | fully modded gear, 1 enhancement | 1121 | 21.4 | 31471 | 1752 | 7.6 | 104015 | 3.31 | - | Passed |
| 21-30 | fully modded gear, 2 enhancements | 1117 | 21.3 | 31430 | 1750 | 8.4 | 94073 | 2.99 | - | Passed |
| 21-30 | fully modded gear, 3 enhancements | 1113 | 21.9 | 30542 | 1743 | 9.6 | 81502 | 2.67 | - | Passed |
| 31-40 | no gear | 724 | 14.7 | 29617 | 1906 | 11.3 | 75583 | 2.55 | - | Passed |
| 31-40 | no gear, 1 enhancement | 648 | 15.5 | 25053 | 1697 | 12.6 | 60736 | 2.42 | - | Passed |
| 31-40 | no gear, 2 enhancements | 632 | 15.2 | 25015 | 1116 | 12.9 | 38951 | 1.56 | - | Passed |
| 31-40 | no gear, 3 enhancements | 609 | 16.1 | 22695 | 706 | 13.5 | 23470 | 1.03 | - | Passed |
| 31-40 | mid gear | 819 | 19.9 | 24744 | 2095 | 10.8 | 87080 | 3.52 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 788 | 21.1 | 22421 | 2118 | 12.8 | 74731 | 3.33 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 756 | 20.5 | 22069 | 1524 | 13.6 | 50357 | 2.28 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 716 | 20.2 | 21265 | 1091 | 14.3 | 34317 | 1.61 | - | Passed |
| 31-40 | best gear | 888 | 24.8 | 21529 | 2257 | 10.5 | 96985 | 4.50 | - | Passed |
| 31-40 | best gear, 1 enhancement | 895 | 26.0 | 20640 | 2350 | 12.5 | 84312 | 4.08 | - | Passed |
| 31-40 | best gear, 2 enhancements | 860 | 26.0 | 19811 | 2182 | 13.8 | 71114 | 3.59 | - | Passed |
| 31-40 | best gear, 3 enhancements | 836 | 24.7 | 20302 | 1731 | 14.4 | 53972 | 2.66 | - | Passed |
| 31-40 | fully modded gear | 1110 | 25.0 | 26670 | 2433 | 7.6 | 144459 | 5.42 | - | Passed |
| 31-40 | fully modded gear, 1 enhancement | 1128 | 25.3 | 26798 | 2457 | 7.6 | 145392 | 5.43 | - | Passed |
| 31-40 | fully modded gear, 2 enhancements | 1129 | 25.9 | 26147 | 2458 | 9.7 | 114023 | 4.36 | - | Passed |
| 31-40 | fully modded gear, 3 enhancements | 1125 | 25.9 | 26105 | 2389 | 11.2 | 96205 | 3.69 | - | Passed |
| 41-53 | no gear | 726 | 18.1 | 24140 | 1038 | 13.1 | 35786 | 1.48 | - | Passed |
| 41-53 | no gear, 1 enhancement | 644 | 16.1 | 23941 | 911 | 12.9 | 31749 | 1.33 | - | Passed |
| 41-53 | no gear, 2 enhancements | 578 | 15.9 | 21849 | 600 | 13.3 | 20313 | 0.93 | - | Passed |
| 41-53 | no gear, 3 enhancements | 539 | 16.5 | 19605 | 400 | 13.6 | 13251 | 0.68 | - | Passed |
| 41-53 | mid gear | 871 | 24.4 | 21396 | 1152 | 13.2 | 39120 | 1.83 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 816 | 22.8 | 21507 | 1103 | 13.3 | 37260 | 1.73 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 730 | 20.6 | 21270 | 907 | 14.8 | 27556 | 1.30 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 701 | 19.9 | 21174 | 657 | 14.7 | 20168 | 0.95 | - | Passed |
| 41-53 | best gear | 1041 | 28.2 | 22172 | 1237 | 13.0 | 42675 | 1.92 | - | Passed |
| 41-53 | best gear, 1 enhancement | 1016 | 28.7 | 21237 | 1188 | 13.0 | 41132 | 1.94 | - | Passed |
| 41-53 | best gear, 2 enhancements | 901 | 26.1 | 20723 | 1156 | 14.1 | 36993 | 1.79 | - | Passed |
| 41-53 | best gear, 3 enhancements | 844 | 23.8 | 21266 | 960 | 15.1 | 28591 | 1.34 | - | Passed |
| 41-53 | fully modded gear | 1181 | 27.8 | 25509 | 1348 | 7.9 | 77163 | 3.02 | - | Passed |
| 41-53 | fully modded gear, 1 enhancement | 1164 | 27.8 | 25150 | 1356 | 9.1 | 67369 | 2.68 | - | Passed |
| 41-53 | fully modded gear, 2 enhancements | 1175 | 28.8 | 24519 | 1331 | 10.5 | 56962 | 2.32 | - | Passed |
| 41-53 | fully modded gear, 3 enhancements | 1125 | 28.6 | 23611 | 1348 | 11.3 | 53510 | 2.27 | - | Passed |

## Enhancement parity

### Rank 50, fully modded gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 100%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 6 | 60-70 | 34% | 97% | 99% | 0.96 | 100% | 99.8% | 1.00 (+/- 0.00) | 1.04 | >= 0.95 | Passed |
| 41-45 | 2 | 6 | 60-70 | 67% | 97% | 99% | 1.93 | 100% | 100.0% | 2.00 (+/- 0.00) | 1.03 | >= 0.95 | Passed |
| 41-45 | 3 | 6 | 60-70 | 101% | 98% | 99% | 2.91 | 100% | 99.9% | 2.97 (+/- 0.01) | 1.02 | >= 0.90 | Passed |
| 46-50 | 1 | 6 | 60 | 32% | 100% | 99% | 0.99 | 100% | 98.8% | 1.00 (+/- 0.00) | 1.01 | >= 0.95 | Passed |
| 46-50 | 2 | 6 | 60 | 64% | 100% | 99% | 1.98 | 100% | 98.0% | 1.94 (+/- 0.02) | 0.98 | >= 0.95 | Passed |
| 46-50 | 3 | 6 | 60 | 96% | 97% | 99% | 2.87 | 99% | 98.7% | 2.82 (+/- 0.04) | 0.98 | >= 0.90 | Passed |
| 51-53 | 1 | 6 | 60-70 | 8% | 98% | 40% | 0.39 | 100% | 91.7% | 0.99 (+/- 0.01) | 2.54 | >= 0.95 | Passed |
| 51-53 | 2 | 6 | 60-70 | 17% | 100% | 46% | 0.91 | 100% | 96.0% | 1.92 (+/- 0.02) | 2.12 | >= 0.95 | Passed |
| 51-53 | 3 | 6 | 60-70 | 25% | 100% | 41% | 1.22 | 100% | 94.7% | 2.41 (+/- 0.05) | 1.97 | >= 0.90 | Passed |

### Thresholds, first / second / third slot, that would meet parity for the quality measured (the strictest set that keeps every case at least at today's, never falling from one slot to the next)

| Scope | Thresholds that meet parity | In steps of 5 | Thresholds in force |
| --- | --- | --- | --- |
| Every band together (the shared thresholds) | 39% / 61% / 94% | 35% / 60% / 90% | 50% / 85% / 100% |
| Band 41-45 alone | 98% / 100% / 100% | 95% / 100% / 100% | 50% / 85% / 100% |
| Band 46-50 alone | 39% / 61% / 94% | 35% / 60% / 90% | 50% / 85% / 100% |
| Band 51-53 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 100% |

### Rank 50, fully modded gear: quality of the items the new minigame made, in whole percent, by band and slots filled

| Band | Slots filled | Items made | 10th percentile | 25th percentile | Median | 75th percentile | 90th percentile | Made at 100% |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 93% |
| 41-45 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 41-45 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 97% |
| 46-50 | 1 | 100% | 96% | 99% | 100% | 100% | 100% | 74% |
| 46-50 | 2 | 100% | 96% | 100% | 100% | 100% | 100% | 81% |
| 46-50 | 3 | 99% | 98% | 100% | 100% | 100% | 100% | 86% |
| 51-53 | 1 | 100% | 80% | 86% | 96% | 100% | 100% | 43% |
| 51-53 | 2 | 100% | 85% | 94% | 100% | 100% | 100% | 60% |
| 51-53 | 3 | 100% | 82% | 94% | 100% | 100% | 100% | 52% |

### Representative enhancements: for each recipe kind the real item at the best level the recipe accepts (the old window refuses one more than 5 levels above the recipe) whose penalty is the lower median of that level

| Recipe kind | Enhancement | Level | Progress penalty | Items of that kind and level (min / median / max penalty) | Recipes |
| --- | --- | --- | --- | --- | --- |
| Weapon | Improved Agrinium V (`imp_agrinium_5`) | 45 (tier V) | 70 | 33 items: 50 / 70.0 / 80 | 4 |
| Armor | Armor Enhancement - Force Attack V (`aen_frcatk5`) | 45 (tier V) | 60 | 50 items: 50 / 60.0 / 150 | 14 |

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
- Enhancements raise the Shape the recipe needs by their progress penalty through ForgeRules.EnhancementShapePercent; their other effects are not played.
- Without a style the crafter plays without perks, which is what the perks-optional gate and every gate of the plain scenario play; the style-balance gate plays Hot Forging and Cold Working with no line perks and with both lines bought in full (every purchase the crafter's rank allows).
- The seconds per action (8) is an estimate for a window nobody has played; client timing replaces it.
- The adaptive player reads only the shared meters and previews, so it plans Thermal worse than a person who reads the range; gate 1 compares it with a solver that searches whole rotations.
- Every craft in this scenario is played by a crafter who bought no perk line and plays Cold Working, so no gate here is an unperked result.
