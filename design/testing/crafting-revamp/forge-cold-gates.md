# Crafting revamp: balance gates of the Plasma Forge (Cold Working alone) minigame

- Command: `SWLOR.CraftSim gates --scenario forge-cold --gates length,materials,xp,enhancements --runs 30 --seed 1 --recipes-per-band 6 --out design/testing/crafting-revamp/forge-cold-gates.md`
- Seed 1; 30 runs per case.

## Inputs

- Scenario: Plasma Forge (Cold Working alone) (`forge-cold`), Smithery recipes, minigame Forge.
- Quality: the quality meter's share of its maximum when the item is made, in points (0-100). A broken or abandoned piece counts 0, so every average includes the crafts that failed.
- Crafter: the rank the scenario's cohort gives each recipe (by default the recipe's level, at most 50, so a crafter works at their own level), with no food or status bonuses and no perks unless a gate names a style or the scenario's limitations say it plays a perk loadout. Gear levels are the plan's none, middle and best tier of the skill's crafting gear:
  - no gear (Control 0, Craftsmanship 0, CP 0)
  - mid gear (Control 13, Craftsmanship 12, CP 23)
  - best gear (Control 29, Craftsmanship 30, CP 37)
- Recipes: 1-10: 6 of 119; 11-20: 6 of 142; 21-30: 6 of 195; 31-40: 6 of 134; 41-53: 6 of 187 recipes played.
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
- Enhancement thresholds in force (`CraftOutcomeRules`, read by gate 7): 50% / 85% / 85% for slots 1, 2 and 3. The scenario applies enhancements to its engine's progress meter.
- Styles: none offered, so the style-balance gate is not applicable.
- Gates run: Length, Economy parity, XP rate, Enhancement parity. The others were not run, so this report cannot show that the minigame passes.

## Verdicts

| Gate | Verdict | Summary |
| --- | --- | --- |
| Length | Passed | Every band and gear tier takes 8-20 actions (item craft at level, perked build) on average. |
| Economy parity | Passed | Component stacks per item are within 10% of the legacy baseline where its success rate is at least 90%, and at most 10% above it where it is lower, at no less quality, in every band and gear tier. 1 case(s) use more than 10% less material than today, which passes. |
| XP rate | Passed | Item crafts: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.01x in 1-10 / mid gear, 3 enhancements). |
| Enhancement parity | Passed | At rank 50 in best gear, the thresholds in force (50% / 85% / 85%) transfer at least 1.00 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 1.05 times today's). Thresholds that meet parity in every band for this quality: 96% / 99% / 100%. |

## Length

### Actions per attempt of the adaptive player at level (item craft at level, perked build)

| Band | Gear | Recipes played | Mean actions per attempt | Shortest recipe | Longest recipe | Stalled attempts | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 11.9 | 11.4 | 12.9 | 0 | 8-20 | Passed |
| 1-10 | mid gear | 6 | 13.5 | 11.9 | 17.4 | 0 | 8-20 | Passed |
| 1-10 | best gear | 6 | 12.2 | 11.1 | 14.8 | 0 | 8-20 | Passed |
| 11-20 | no gear | 6 | 11.6 | 9.9 | 15.2 | 0 | 8-20 | Passed |
| 11-20 | mid gear | 6 | 14.1 | 10.1 | 17.8 | 0 | 8-20 | Passed |
| 11-20 | best gear | 6 | 13.0 | 8.5 | 16.8 | 0 | 8-20 | Passed |
| 21-30 | no gear | 6 | 11.2 | 8.1 | 13.4 | 0 | 8-20 | Passed |
| 21-30 | mid gear | 6 | 10.9 | 9.7 | 12.7 | 0 | 8-20 | Passed |
| 21-30 | best gear | 6 | 10.4 | 9.2 | 11.7 | 0 | 8-20 | Passed |
| 31-40 | no gear | 6 | 11.8 | 10.5 | 12.8 | 0 | 8-20 | Passed |
| 31-40 | mid gear | 6 | 11.2 | 9.5 | 12.4 | 0 | 8-20 | Passed |
| 31-40 | best gear | 6 | 9.7 | 8.8 | 11.1 | 0 | 8-20 | Passed |
| 41-53 | no gear | 6 | 11.3 | 6.7 | 12.9 | 0 | 8-20 | Passed |
| 41-53 | mid gear | 6 | 10.9 | 7.4 | 12.9 | 0 | 8-20 | Passed |
| 41-53 | best gear | 6 | 9.9 | 9.1 | 10.8 | 0 | 8-20 | Passed |

## Economy parity

### Component stacks consumed per item made, failures included, over the recipes the legacy policy makes at least once in the cell (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Parity rule | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 2.85 | 2.70 | 1.06 | 90% | 98% | 60% | 1% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | mid gear | 6 | 2.71 | 2.70 | 1.00 | 98% | 98% | 90% | 11% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | best gear | 6 | 2.67 | 2.67 | 1.00 | 100% | 100% | 98% | 28% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | no gear | 6 | 2.62 | 2.66 | 0.99 | 93% | 91% | 64% | 2% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | mid gear | 6 | 2.52 | 2.55 | 0.99 | 99% | 97% | 92% | 13% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | best gear | 6 | 2.53 | 2.52 | 1.01 | 98% | 99% | 96% | 29% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | no gear | 6 | 3.33 | 3.36 | 0.99 | 100% | 99% | 86% | 8% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | mid gear | 6 | 3.33 | 3.34 | 1.00 | 100% | 99% | 94% | 16% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | best gear | 6 | 3.33 | 3.34 | 1.00 | 100% | 99% | 95% | 29% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | no gear | 6 | 3.04 | 3.05 | 1.00 | 98% | 98% | 85% | 22% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | mid gear | 6 | 3.03 | 3.01 | 1.01 | 98% | 99% | 93% | 38% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | best gear | 6 | 3.01 | 3.02 | 1.00 | 99% | 99% | 97% | 49% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | no gear | 6 | 3.02 | 3.03 | 0.99 | 99% | 98% | 96% | 44% | yes | 0% / 33% | within 10% | Passed |
| 41-53 | mid gear | 6 | 4.69 | 5.38 | 0.87 | 99% | 72% | 90% | 46% | yes | 0% / 0% | at most +10%; uses 13% less, passes | Passed |
| 41-53 | best gear | 6 | 4.67 | 4.90 | 0.95 | 100% | 89% | 94% | 62% | yes | 0% / 0% | at most +10% | Passed |

### Recipes the legacy policy never makes in the cell, left out of the stack and quality comparison above (they still count in the never-made check)

| Band | Gear | Recipe | Level | New success | New avg quality |
| --- | --- | --- | --- | --- | --- |
| 41-53 | no gear | ChiroGreatSword | 52 | 100% | 56% |
| 41-53 | no gear | ImmortalRing | 52 | 100% | 57% |

## XP rate

### Item crafts: XP per hour against the legacy loop (6.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per craft | Legacy actions per craft | Legacy XP per hour | New XP per craft | New actions per craft | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 593 | 6.0 | 59489 | 1792 | 11.9 | 67748 | 1.14 | - | Passed |
| 1-10 | no gear, 1 enhancement | 442 | 6.9 | 38624 | 1555 | 11.6 | 60142 | 1.56 | - | Passed |
| 1-10 | no gear, 2 enhancements | 305 | 6.6 | 27851 | 1050 | 11.3 | 41913 | 1.50 | - | Passed |
| 1-10 | no gear, 3 enhancements | 209 | 6.9 | 18130 | 619 | 11.2 | 24777 | 1.37 | - | Passed |
| 1-10 | mid gear | 656 | 8.8 | 44601 | 2224 | 13.5 | 74354 | 1.67 | - | Passed |
| 1-10 | mid gear, 1 enhancement | 663 | 8.8 | 45415 | 1898 | 13.6 | 62995 | 1.39 | - | Passed |
| 1-10 | mid gear, 2 enhancements | 596 | 9.6 | 37414 | 1380 | 13.2 | 47198 | 1.26 | - | Passed |
| 1-10 | mid gear, 3 enhancements | 549 | 10.2 | 32305 | 950 | 13.1 | 32654 | 1.01 | - | Passed |
| 1-10 | best gear | 770 | 10.3 | 44808 | 2341 | 12.2 | 86575 | 1.93 | - | Passed |
| 1-10 | best gear, 1 enhancement | 740 | 10.9 | 40867 | 2213 | 13.3 | 74690 | 1.83 | - | Passed |
| 1-10 | best gear, 2 enhancements | 712 | 11.0 | 38779 | 1899 | 14.2 | 59959 | 1.55 | - | Passed |
| 1-10 | best gear, 3 enhancements | 702 | 11.2 | 37692 | 1446 | 14.8 | 44056 | 1.17 | - | Passed |
| 11-20 | no gear | 566 | 7.9 | 43163 | 1142 | 11.6 | 44181 | 1.02 | - | Passed |
| 11-20 | no gear, 1 enhancement | 409 | 7.9 | 30965 | 1061 | 12.1 | 39607 | 1.28 | - | Passed |
| 11-20 | no gear, 2 enhancements | 266 | 7.2 | 22260 | 967 | 12.3 | 35508 | 1.60 | - | Passed |
| 11-20 | no gear, 3 enhancements | 262 | 7.7 | 20334 | 676 | 12.4 | 24543 | 1.21 | - | Passed |
| 11-20 | mid gear | 664 | 11.1 | 35793 | 1376 | 14.1 | 43983 | 1.23 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 658 | 11.8 | 33330 | 1309 | 15.1 | 39039 | 1.17 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 622 | 12.4 | 30075 | 1117 | 15.8 | 31736 | 1.06 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 597 | 13.5 | 26634 | 1014 | 16.1 | 28411 | 1.07 | - | Passed |
| 11-20 | best gear | 766 | 14.4 | 31957 | 1401 | 13.0 | 48419 | 1.52 | - | Passed |
| 11-20 | best gear, 1 enhancement | 759 | 15.2 | 30033 | 1380 | 14.3 | 43484 | 1.45 | - | Passed |
| 11-20 | best gear, 2 enhancements | 744 | 16.0 | 27962 | 1316 | 15.7 | 37699 | 1.35 | - | Passed |
| 11-20 | best gear, 3 enhancements | 753 | 16.5 | 27328 | 1252 | 16.6 | 33912 | 1.24 | - | Passed |
| 21-30 | no gear | 639 | 10.2 | 37566 | 1630 | 11.2 | 65244 | 1.74 | - | Passed |
| 21-30 | no gear, 1 enhancement | 618 | 11.5 | 32333 | 1570 | 12.5 | 56464 | 1.75 | - | Passed |
| 21-30 | no gear, 2 enhancements | 592 | 12.4 | 28664 | 1315 | 13.5 | 43801 | 1.53 | - | Passed |
| 21-30 | no gear, 3 enhancements | 575 | 13.5 | 25564 | 797 | 13.6 | 26299 | 1.03 | - | Passed |
| 21-30 | mid gear | 691 | 16.5 | 25152 | 1696 | 10.9 | 70046 | 2.78 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 679 | 16.8 | 24257 | 1703 | 12.5 | 61256 | 2.53 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 665 | 17.4 | 22996 | 1655 | 13.9 | 53539 | 2.33 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 647 | 17.6 | 22047 | 1415 | 15.1 | 42095 | 1.91 | - | Passed |
| 21-30 | best gear | 770 | 21.3 | 21631 | 1712 | 10.4 | 74342 | 3.44 | - | Passed |
| 21-30 | best gear, 1 enhancement | 754 | 21.6 | 20987 | 1729 | 11.4 | 68063 | 3.24 | - | Passed |
| 21-30 | best gear, 2 enhancements | 752 | 22.0 | 20504 | 1729 | 13.0 | 59672 | 2.91 | - | Passed |
| 21-30 | best gear, 3 enhancements | 744 | 21.9 | 20368 | 1678 | 14.2 | 53012 | 2.60 | - | Passed |
| 31-40 | no gear | 722 | 15.3 | 28279 | 2247 | 11.8 | 85898 | 3.04 | - | Passed |
| 31-40 | no gear, 1 enhancement | 648 | 15.5 | 25053 | 1923 | 12.4 | 69796 | 2.79 | - | Passed |
| 31-40 | no gear, 2 enhancements | 632 | 15.2 | 25015 | 1240 | 12.8 | 43436 | 1.74 | - | Passed |
| 31-40 | no gear, 3 enhancements | 609 | 16.1 | 22695 | 787 | 13.5 | 26209 | 1.15 | - | Passed |
| 31-40 | mid gear | 827 | 20.5 | 24162 | 2359 | 11.2 | 94877 | 3.93 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 788 | 21.1 | 22421 | 2237 | 12.5 | 80389 | 3.59 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 756 | 20.5 | 22069 | 1784 | 13.6 | 58928 | 2.67 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 716 | 20.2 | 21265 | 1277 | 14.3 | 40092 | 1.89 | - | Passed |
| 31-40 | best gear | 889 | 25.4 | 21025 | 2415 | 9.7 | 111710 | 5.31 | - | Passed |
| 31-40 | best gear, 1 enhancement | 895 | 26.0 | 20640 | 2423 | 11.9 | 91552 | 4.44 | - | Passed |
| 31-40 | best gear, 2 enhancements | 860 | 26.0 | 19811 | 2343 | 13.1 | 80383 | 4.06 | - | Passed |
| 31-40 | best gear, 3 enhancements | 836 | 24.7 | 20302 | 1898 | 14.1 | 60372 | 2.97 | - | Passed |
| 41-53 | no gear | 608 | 15.9 | 22911 | 1279 | 11.3 | 50890 | 2.22 | - | Passed |
| 41-53 | no gear, 1 enhancement | 543 | 14.4 | 22557 | 1196 | 12.2 | 43970 | 1.95 | - | Passed |
| 41-53 | no gear, 2 enhancements | 487 | 14.3 | 20434 | 849 | 12.9 | 29568 | 1.45 | - | Passed |
| 41-53 | no gear, 3 enhancements | 455 | 14.7 | 18538 | 542 | 13.0 | 18808 | 1.01 | - | Passed |
| 41-53 | mid gear | 746 | 23.0 | 19424 | 1336 | 10.9 | 55287 | 2.85 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 695 | 21.5 | 19414 | 1316 | 11.5 | 51340 | 2.64 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 620 | 18.8 | 19811 | 1278 | 13.3 | 43384 | 2.19 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 586 | 17.4 | 20185 | 996 | 14.3 | 31309 | 1.55 | - | Passed |
| 41-53 | best gear | 951 | 27.5 | 20773 | 1367 | 9.9 | 61974 | 2.98 | - | Passed |
| 41-53 | best gear, 1 enhancement | 890 | 27.1 | 19687 | 1352 | 11.2 | 54382 | 2.76 | - | Passed |
| 41-53 | best gear, 2 enhancements | 769 | 23.8 | 19375 | 1346 | 12.0 | 50432 | 2.60 | - | Passed |
| 41-53 | best gear, 3 enhancements | 711 | 21.4 | 19960 | 1257 | 13.3 | 42682 | 2.14 | - | Passed |

## Enhancement parity

### Rank 50, best gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 85%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 6 | 60-70 | 33% | 100% | 96% | 0.96 | 100% | 99.5% | 1.00 (+/- 0.00) | 1.05 | >= 1.00 | Passed |
| 41-45 | 2 | 6 | 60-70 | 65% | 100% | 89% | 1.78 | 100% | 98.8% | 1.98 (+/- 0.01) | 1.12 | >= 1.00 | Passed |
| 41-45 | 3 | 6 | 60-70 | 98% | 100% | 83% | 2.47 | 99% | 98.0% | 2.85 (+/- 0.04) | 1.15 | >= 1.00 | Passed |
| 46-50 | 1 | 6 | 60 | 32% | 100% | 83% | 0.83 | 99% | 98.3% | 0.99 (+/- 0.01) | 1.19 | >= 1.00 | Passed |
| 46-50 | 2 | 6 | 60 | 64% | 97% | 72% | 1.38 | 98% | 97.0% | 1.89 (+/- 0.03) | 1.37 | >= 1.00 | Passed |
| 46-50 | 3 | 6 | 60 | 96% | 100% | 60% | 1.79 | 96% | 92.3% | 2.52 (+/- 0.08) | 1.41 | >= 1.00 | Passed |
| 51-53 | 1 | 6 | 60-70 | 8% | 49% | 7% | 0.03 | 99% | 90.3% | 0.99 (+/- 0.01) | 29.97 | >= 1.00 | Passed |
| 51-53 | 2 | 6 | 60-70 | 17% | 22% | 4% | 0.02 | 99% | 92.1% | 1.77 (+/- 0.03) | 109.25 | >= 1.00 | Passed |
| 51-53 | 3 | 6 | 60-70 | 25% | 9% | 4% | 0.01 | 98% | 86.9% | 2.21 (+/- 0.08) | 245.68 | >= 1.00 | Passed |

### Thresholds, first / second / third slot, that would meet parity for the quality measured (the strictest set that keeps every case at least at today's, never falling from one slot to the next)

| Scope | Thresholds that meet parity | In steps of 5 | Thresholds in force |
| --- | --- | --- | --- |
| Every band together (the shared thresholds) | 96% / 99% / 100% | 95% / 95% / 100% | 50% / 85% / 85% |
| Band 41-45 alone | 96% / 99% / 100% | 95% / 95% / 100% | 50% / 85% / 85% |
| Band 46-50 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 85% |
| Band 51-53 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 85% |

### Rank 50, best gear: quality of the items the new minigame made, in whole percent, by band and slots filled

| Band | Slots filled | Items made | 10th percentile | 25th percentile | Median | 75th percentile | 90th percentile | Made at 100% |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 100% | 99% | 100% | 100% | 100% | 100% | 89% |
| 41-45 | 2 | 100% | 97% | 100% | 100% | 100% | 100% | 83% |
| 41-45 | 3 | 99% | 99% | 100% | 100% | 100% | 100% | 87% |
| 46-50 | 1 | 99% | 97% | 100% | 100% | 100% | 100% | 86% |
| 46-50 | 2 | 98% | 88% | 100% | 100% | 100% | 100% | 80% |
| 46-50 | 3 | 96% | 68% | 100% | 100% | 100% | 100% | 73% |
| 51-53 | 1 | 99% | 73% | 80% | 100% | 100% | 100% | 52% |
| 51-53 | 2 | 99% | 70% | 87% | 100% | 100% | 100% | 52% |
| 51-53 | 3 | 98% | 66% | 79% | 95% | 100% | 100% | 43% |

### Representative enhancements: for each recipe kind the real item at the best level the recipe accepts (the old window refuses one more than 5 levels above the recipe) whose penalty is the lower median of that level

| Recipe kind | Enhancement | Level | Progress penalty | Items of that kind and level (min / median / max penalty) | Recipes |
| --- | --- | --- | --- | --- | --- |
| Weapon | Improved Agrinium V (`imp_agrinium_5`) | 45 (tier V) | 70 | 33 items: 50 / 70.0 / 80 | 3 |
| Armor | Armor Enhancement - Force Attack V (`aen_frcatk5`) | 45 (tier V) | 60 | 50 items: 50 / 60.0 / 150 | 15 |

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
