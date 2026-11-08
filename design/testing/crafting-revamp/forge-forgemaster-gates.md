# Crafting revamp: balance gates of the Plasma Forge (Forgemaster line, Hot Forging) minigame

- Command: `SWLOR.CraftSim gates --scenario forge-forgemaster --gates length,materials,xp,enhancements --runs 30 --seed 1 --recipes-per-band 6 --repo . --out design/testing/crafting-revamp/forge-forgemaster-gates.md`
- Seed 1; 30 runs per case.

## Inputs

- Scenario: Plasma Forge (Forgemaster line, Hot Forging) (`forge-forgemaster`), Smithery recipes, minigame Forge.
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
| XP rate | Passed | Item crafts: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.07x in 11-20 / no gear). |
| Enhancement parity | Passed | At rank 50 in fully modded gear, the thresholds in force (50% / 85% / 100%) transfer at least 0.90 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 0.93 times today's). Thresholds that meet parity in every band for this quality: 76% / 76% / 85%. |

## Length

### Actions per attempt of the adaptive player at level (item craft at level, perked build)

| Band | Gear | Recipes played | Mean actions per attempt | Shortest recipe | Longest recipe | Stalled attempts | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 12.1 | 11.5 | 12.9 | 0 | 8-20 | Passed |
| 1-10 | mid gear | 6 | 13.4 | 11.9 | 17.1 | 0 | 8-20 | Passed |
| 1-10 | best gear | 6 | 12.2 | 11.1 | 14.6 | 0 | 8-20 | Passed |
| 1-10 | fully modded gear | 6 | 7.5 | 6.7 | 8.6 | 0 | 8-20 | Reported |
| 11-20 | no gear | 6 | 11.3 | 8.8 | 14.4 | 0 | 8-20 | Passed |
| 11-20 | mid gear | 6 | 13.4 | 9.2 | 17.8 | 0 | 8-20 | Passed |
| 11-20 | best gear | 6 | 12.5 | 7.8 | 16.4 | 0 | 8-20 | Passed |
| 11-20 | fully modded gear | 6 | 7.9 | 5.5 | 9.5 | 0 | 8-20 | Reported |
| 21-30 | no gear | 6 | 10.4 | 8.2 | 12.7 | 0 | 8-20 | Passed |
| 21-30 | mid gear | 6 | 10.5 | 8.4 | 11.8 | 0 | 8-20 | Passed |
| 21-30 | best gear | 6 | 9.4 | 7.7 | 10.7 | 0 | 8-20 | Passed |
| 21-30 | fully modded gear | 6 | 6.5 | 5.9 | 6.9 | 0 | 8-20 | Reported |
| 31-40 | no gear | 6 | 9.6 | 7.8 | 11.8 | 0 | 8-20 | Reported |
| 31-40 | mid gear | 6 | 9.7 | 7.8 | 11.6 | 0 | 8-20 | Passed |
| 31-40 | best gear | 6 | 9.3 | 8.1 | 11.7 | 0 | 8-20 | Passed |
| 31-40 | fully modded gear | 6 | 6.7 | 5.9 | 7.6 | 0 | 8-20 | Reported |
| 41-53 | no gear | 6 | 13.6 | 9.1 | 17.4 | 0 | 8-20 | Reported |
| 41-53 | mid gear | 6 | 13.4 | 9.0 | 18.2 | 0 | 8-20 | Reported |
| 41-53 | best gear | 6 | 12.4 | 10.3 | 15.6 | 0 | 8-20 | Passed |
| 41-53 | fully modded gear | 6 | 8.1 | 6.5 | 9.2 | 0 | 8-20 | Reported |

## Economy parity

### Component stacks consumed per item made, failures included, over the recipes the legacy policy makes at least once in the cell (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Parity rule | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 2.75 | 2.70 | 1.02 | 95% | 98% | 67% | 1% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | mid gear | 6 | 2.74 | 2.70 | 1.01 | 96% | 98% | 89% | 11% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | best gear | 6 | 2.67 | 2.67 | 1.00 | 100% | 100% | 98% | 28% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | fully modded gear | 6 | 2.67 | 2.68 | 1.00 | 100% | 99% | 100% | 79% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | no gear | 6 | 2.61 | 2.66 | 0.98 | 94% | 91% | 65% | 2% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | mid gear | 6 | 2.54 | 2.55 | 1.00 | 98% | 97% | 90% | 13% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | best gear | 6 | 2.58 | 2.52 | 1.03 | 96% | 99% | 93% | 29% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | fully modded gear | 6 | 2.50 | 2.50 | 1.00 | 100% | 100% | 100% | 93% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | no gear | 6 | 3.35 | 3.19 | 1.05 | 92% | 99% | 67% | 8% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | mid gear | 6 | 3.17 | 3.18 | 1.00 | 100% | 99% | 85% | 16% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | best gear | 6 | 3.17 | 3.18 | 1.00 | 100% | 99% | 92% | 29% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | fully modded gear | 6 | 3.17 | 3.19 | 0.99 | 100% | 99% | 100% | 90% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | no gear | 6 | 2.85 | 2.86 | 1.00 | 99% | 98% | 59% | 22% | yes | 0% / 0% | within 10% | Reported |
| 31-40 | mid gear | 6 | 2.83 | 2.87 | 0.99 | 100% | 98% | 71% | 38% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | best gear | 6 | 2.83 | 2.85 | 0.99 | 100% | 99% | 81% | 49% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | fully modded gear | 6 | 2.83 | 2.89 | 0.98 | 100% | 97% | 100% | 87% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | no gear | 6 | 3.45 | 3.04 | 1.13 | 81% | 98% | 58% | 43% | yes | 0% / 17% | within 10% | Reported |
| 41-53 | mid gear | 6 | 4.12 | 4.00 | 1.03 | 91% | 86% | 68% | 56% | yes | 0% / 0% | at most +10% | Reported |
| 41-53 | best gear | 6 | 3.85 | 3.89 | 0.99 | 99% | 94% | 82% | 75% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | fully modded gear | 6 | 3.83 | 3.83 | 1.00 | 100% | 100% | 99% | 91% | yes | 0% / 0% | within 10% | Passed |

### Recipes the legacy policy never makes in the cell, left out of the stack and quality comparison above (they still count in the never-made check)

| Band | Gear | Recipe | Level | New success | New avg quality |
| --- | --- | --- | --- | --- | --- |
| 41-53 | no gear | ImmortalBelt | 52 | 100% | 47% |

## XP rate

### Item crafts: XP per hour against the legacy loop (6.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per craft | Legacy actions per craft | Legacy XP per hour | New XP per craft | New actions per craft | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 593 | 6.0 | 59489 | 1920 | 12.1 | 71489 | 1.20 | - | Passed |
| 1-10 | no gear, 1 enhancement | 442 | 6.9 | 38624 | 1561 | 11.7 | 59983 | 1.55 | - | Passed |
| 1-10 | no gear, 2 enhancements | 305 | 6.6 | 27851 | 1069 | 11.3 | 42482 | 1.53 | - | Passed |
| 1-10 | no gear, 3 enhancements | 209 | 6.9 | 18130 | 638 | 11.3 | 25391 | 1.40 | - | Passed |
| 1-10 | mid gear | 656 | 8.8 | 44601 | 2190 | 13.4 | 73735 | 1.65 | - | Passed |
| 1-10 | mid gear, 1 enhancement | 663 | 8.8 | 45415 | 1952 | 13.7 | 64344 | 1.42 | - | Passed |
| 1-10 | mid gear, 2 enhancements | 596 | 9.6 | 37414 | 1561 | 13.6 | 51815 | 1.38 | - | Passed |
| 1-10 | mid gear, 3 enhancements | 549 | 10.2 | 32305 | 1126 | 13.3 | 38019 | 1.18 | - | Passed |
| 1-10 | best gear | 770 | 10.3 | 44808 | 2341 | 12.2 | 86342 | 1.93 | - | Passed |
| 1-10 | best gear, 1 enhancement | 740 | 10.9 | 40867 | 2212 | 13.2 | 75168 | 1.84 | - | Passed |
| 1-10 | best gear, 2 enhancements | 712 | 11.0 | 38779 | 1941 | 14.0 | 62448 | 1.61 | - | Passed |
| 1-10 | best gear, 3 enhancements | 702 | 11.2 | 37692 | 1604 | 14.8 | 48718 | 1.29 | - | Passed |
| 1-10 | fully modded gear | 1071 | 8.5 | 75652 | 2363 | 7.5 | 142731 | 1.89 | - | Passed |
| 1-10 | fully modded gear, 1 enhancement | 1049 | 8.5 | 74450 | 2364 | 8.6 | 123528 | 1.66 | - | Passed |
| 1-10 | fully modded gear, 2 enhancements | 1047 | 8.3 | 75577 | 2364 | 9.4 | 113364 | 1.50 | - | Passed |
| 1-10 | fully modded gear, 3 enhancements | 1053 | 8.8 | 71474 | 2350 | 10.4 | 101252 | 1.42 | - | Passed |
| 11-20 | no gear | 566 | 7.9 | 43163 | 1157 | 11.3 | 46062 | 1.07 | - | Passed |
| 11-20 | no gear, 1 enhancement | 409 | 7.9 | 30965 | 1027 | 11.3 | 40852 | 1.32 | - | Passed |
| 11-20 | no gear, 2 enhancements | 266 | 7.2 | 22260 | 910 | 11.4 | 35797 | 1.61 | - | Passed |
| 11-20 | no gear, 3 enhancements | 262 | 7.7 | 20334 | 683 | 11.8 | 25957 | 1.28 | - | Passed |
| 11-20 | mid gear | 664 | 11.1 | 35793 | 1351 | 13.4 | 45243 | 1.26 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 658 | 11.8 | 33330 | 1316 | 14.3 | 41477 | 1.24 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 622 | 12.4 | 30075 | 1199 | 14.8 | 36395 | 1.21 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 597 | 13.5 | 26634 | 1056 | 14.9 | 31867 | 1.20 | - | Passed |
| 11-20 | best gear | 766 | 14.4 | 31957 | 1364 | 12.5 | 49062 | 1.54 | - | Passed |
| 11-20 | best gear, 1 enhancement | 759 | 15.2 | 30033 | 1342 | 13.6 | 44341 | 1.48 | - | Passed |
| 11-20 | best gear, 2 enhancements | 744 | 16.0 | 27962 | 1336 | 15.0 | 40050 | 1.43 | - | Passed |
| 11-20 | best gear, 3 enhancements | 753 | 16.5 | 27328 | 1311 | 16.5 | 35820 | 1.31 | - | Passed |
| 11-20 | fully modded gear | 1155 | 14.2 | 48714 | 1439 | 7.8 | 82515 | 1.69 | - | Passed |
| 11-20 | fully modded gear, 1 enhancement | 1118 | 14.2 | 47354 | 1440 | 8.1 | 79865 | 1.69 | - | Passed |
| 11-20 | fully modded gear, 2 enhancements | 1135 | 14.6 | 46523 | 1439 | 9.1 | 71301 | 1.53 | - | Passed |
| 11-20 | fully modded gear, 3 enhancements | 1136 | 14.7 | 46374 | 1407 | 9.8 | 64597 | 1.39 | - | Passed |
| 21-30 | no gear | 639 | 10.2 | 37566 | 1409 | 10.4 | 61143 | 1.63 | - | Passed |
| 21-30 | no gear, 1 enhancement | 618 | 11.5 | 32333 | 1532 | 12.0 | 57287 | 1.77 | - | Passed |
| 21-30 | no gear, 2 enhancements | 592 | 12.4 | 28664 | 1372 | 12.3 | 50071 | 1.75 | - | Passed |
| 21-30 | no gear, 3 enhancements | 575 | 13.5 | 25564 | 983 | 12.8 | 34596 | 1.35 | - | Passed |
| 21-30 | mid gear | 691 | 16.5 | 25152 | 1624 | 10.5 | 69689 | 2.77 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 679 | 16.8 | 24257 | 1653 | 11.7 | 63728 | 2.63 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 665 | 17.4 | 22996 | 1610 | 12.8 | 56531 | 2.46 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 647 | 17.6 | 22047 | 1395 | 14.0 | 44915 | 2.04 | - | Passed |
| 21-30 | best gear | 770 | 21.3 | 21631 | 1680 | 9.4 | 80811 | 3.74 | - | Passed |
| 21-30 | best gear, 1 enhancement | 754 | 21.6 | 20987 | 1698 | 10.7 | 71162 | 3.39 | - | Passed |
| 21-30 | best gear, 2 enhancements | 752 | 22.0 | 20504 | 1706 | 12.0 | 64180 | 3.13 | - | Passed |
| 21-30 | best gear, 3 enhancements | 744 | 21.9 | 20368 | 1637 | 13.5 | 54586 | 2.68 | - | Passed |
| 21-30 | fully modded gear | 1133 | 20.7 | 32818 | 1751 | 6.5 | 121027 | 3.69 | - | Passed |
| 21-30 | fully modded gear, 1 enhancement | 1121 | 21.4 | 31471 | 1752 | 6.8 | 116591 | 3.70 | - | Passed |
| 21-30 | fully modded gear, 2 enhancements | 1117 | 21.3 | 31430 | 1751 | 7.3 | 107230 | 3.41 | - | Passed |
| 21-30 | fully modded gear, 3 enhancements | 1113 | 21.9 | 30542 | 1749 | 8.4 | 93331 | 3.06 | - | Passed |
| 31-40 | no gear | 724 | 14.7 | 29617 | 1943 | 9.6 | 91508 | 3.09 | - | Passed |
| 31-40 | no gear, 1 enhancement | 648 | 15.5 | 25053 | 1875 | 11.3 | 74863 | 2.99 | - | Passed |
| 31-40 | no gear, 2 enhancements | 632 | 15.2 | 25015 | 1489 | 12.3 | 54658 | 2.19 | - | Passed |
| 31-40 | no gear, 3 enhancements | 609 | 16.1 | 22695 | 818 | 12.7 | 28996 | 1.28 | - | Passed |
| 31-40 | mid gear | 819 | 19.9 | 24744 | 2102 | 9.7 | 97053 | 3.92 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 788 | 21.1 | 22421 | 2125 | 11.8 | 80957 | 3.61 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 756 | 20.5 | 22069 | 1853 | 12.8 | 65148 | 2.95 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 716 | 20.2 | 21265 | 1322 | 13.1 | 45321 | 2.13 | - | Passed |
| 31-40 | best gear | 888 | 24.8 | 21529 | 2227 | 9.3 | 107373 | 4.99 | - | Passed |
| 31-40 | best gear, 1 enhancement | 895 | 26.0 | 20640 | 2301 | 11.3 | 91726 | 4.44 | - | Passed |
| 31-40 | best gear, 2 enhancements | 860 | 26.0 | 19811 | 2251 | 12.7 | 80090 | 4.04 | - | Passed |
| 31-40 | best gear, 3 enhancements | 836 | 24.7 | 20302 | 1753 | 13.4 | 58792 | 2.90 | - | Passed |
| 31-40 | fully modded gear | 1110 | 25.0 | 26670 | 2458 | 6.7 | 164279 | 6.16 | - | Passed |
| 31-40 | fully modded gear, 1 enhancement | 1128 | 25.3 | 26798 | 2455 | 6.5 | 170105 | 6.35 | - | Passed |
| 31-40 | fully modded gear, 2 enhancements | 1129 | 25.9 | 26147 | 2459 | 8.3 | 133866 | 5.12 | - | Passed |
| 31-40 | fully modded gear, 3 enhancements | 1125 | 25.9 | 26105 | 2426 | 9.5 | 114800 | 4.40 | - | Passed |
| 41-53 | no gear | 726 | 18.1 | 24140 | 975 | 13.6 | 32253 | 1.34 | - | Passed |
| 41-53 | no gear, 1 enhancement | 644 | 16.1 | 23941 | 879 | 13.0 | 30452 | 1.27 | - | Passed |
| 41-53 | no gear, 2 enhancements | 578 | 15.9 | 21849 | 683 | 13.6 | 22667 | 1.04 | - | Passed |
| 41-53 | no gear, 3 enhancements | 539 | 16.5 | 19605 | 507 | 14.4 | 15814 | 0.81 | - | Passed |
| 41-53 | mid gear | 871 | 24.4 | 21396 | 1088 | 13.4 | 36512 | 1.71 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 816 | 22.8 | 21507 | 972 | 13.6 | 32235 | 1.50 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 730 | 20.6 | 21270 | 765 | 14.3 | 24030 | 1.13 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 701 | 19.9 | 21174 | 563 | 14.8 | 17170 | 0.81 | - | Passed |
| 41-53 | best gear | 1041 | 28.2 | 22172 | 1235 | 12.4 | 44881 | 2.02 | - | Passed |
| 41-53 | best gear, 1 enhancement | 1016 | 28.7 | 21237 | 1138 | 13.8 | 37106 | 1.75 | - | Passed |
| 41-53 | best gear, 2 enhancements | 901 | 26.1 | 20723 | 1043 | 14.5 | 32317 | 1.56 | - | Passed |
| 41-53 | best gear, 3 enhancements | 844 | 23.8 | 21266 | 794 | 15.4 | 23233 | 1.09 | - | Passed |
| 41-53 | fully modded gear | 1181 | 27.8 | 25509 | 1352 | 8.1 | 74828 | 2.93 | - | Passed |
| 41-53 | fully modded gear, 1 enhancement | 1164 | 27.8 | 25150 | 1349 | 8.5 | 71515 | 2.84 | - | Passed |
| 41-53 | fully modded gear, 2 enhancements | 1175 | 28.8 | 24519 | 1355 | 9.3 | 65706 | 2.68 | - | Passed |
| 41-53 | fully modded gear, 3 enhancements | 1125 | 28.6 | 23611 | 1354 | 10.6 | 57353 | 2.43 | - | Passed |

## Enhancement parity

### Rank 50, fully modded gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 100%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 6 | 60-70 | 34% | 97% | 99% | 0.96 | 100% | 99.7% | 1.00 (+/- 0.00) | 1.04 | >= 0.95 | Passed |
| 41-45 | 2 | 6 | 60-70 | 67% | 97% | 99% | 1.93 | 100% | 99.9% | 2.00 (+/- 0.00) | 1.03 | >= 0.95 | Passed |
| 41-45 | 3 | 6 | 60-70 | 101% | 98% | 99% | 2.91 | 100% | 99.9% | 2.97 (+/- 0.01) | 1.02 | >= 0.90 | Passed |
| 46-50 | 1 | 6 | 60 | 32% | 100% | 99% | 0.99 | 100% | 98.6% | 1.00 (+/- 0.00) | 1.01 | >= 0.95 | Passed |
| 46-50 | 2 | 6 | 60 | 64% | 100% | 99% | 1.98 | 100% | 98.1% | 1.97 (+/- 0.01) | 0.99 | >= 0.95 | Passed |
| 46-50 | 3 | 6 | 60 | 96% | 97% | 99% | 2.87 | 100% | 97.0% | 2.67 (+/- 0.04) | 0.93 | >= 0.90 | Passed |
| 51-53 | 1 | 6 | 60-70 | 8% | 98% | 40% | 0.39 | 100% | 93.1% | 0.99 (+/- 0.01) | 2.55 | >= 0.95 | Passed |
| 51-53 | 2 | 6 | 60-70 | 17% | 100% | 46% | 0.91 | 100% | 90.8% | 1.78 (+/- 0.03) | 1.96 | >= 0.95 | Passed |
| 51-53 | 3 | 6 | 60-70 | 25% | 100% | 41% | 1.22 | 100% | 92.4% | 2.18 (+/- 0.06) | 1.78 | >= 0.90 | Passed |

### Thresholds, first / second / third slot, that would meet parity for the quality measured (the strictest set that keeps every case at least at today's, never falling from one slot to the next)

| Scope | Thresholds that meet parity | In steps of 5 | Thresholds in force |
| --- | --- | --- | --- |
| Every band together (the shared thresholds) | 76% / 76% / 85% | 75% / 80% / 80% | 50% / 85% / 100% |
| Band 41-45 alone | 97% / 100% / 100% | 95% / 100% / 100% | 50% / 85% / 100% |
| Band 46-50 alone | 76% / 76% / 85% | 75% / 80% / 80% | 50% / 85% / 100% |
| Band 51-53 alone | 90% / 98% / 100% | 90% / 95% / 100% | 50% / 85% / 100% |

### Rank 50, fully modded gear: quality of the items the new minigame made, in whole percent, by band and slots filled

| Band | Slots filled | Items made | 10th percentile | 25th percentile | Median | 75th percentile | 90th percentile | Made at 100% |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 100% | 99% | 100% | 100% | 100% | 100% | 90% |
| 41-45 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 97% |
| 41-45 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 97% |
| 46-50 | 1 | 100% | 96% | 100% | 100% | 100% | 100% | 79% |
| 46-50 | 2 | 100% | 94% | 99% | 100% | 100% | 100% | 73% |
| 46-50 | 3 | 100% | 87% | 98% | 100% | 100% | 100% | 73% |
| 51-53 | 1 | 100% | 75% | 87% | 100% | 100% | 100% | 58% |
| 51-53 | 2 | 100% | 77% | 85% | 91% | 100% | 100% | 29% |
| 51-53 | 3 | 100% | 78% | 84% | 98% | 100% | 100% | 43% |

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
- Every craft in this scenario is played by a crafter who bought the Forgemaster line in full (every purchase their Smithery rank allows) and plays Hot Forging, so no gate here is an unperked result.
