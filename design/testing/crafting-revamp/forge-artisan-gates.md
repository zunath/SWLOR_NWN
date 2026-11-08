# Crafting revamp: balance gates of the Plasma Forge (Artisan line, Cold Working) minigame

- Command: `SWLOR.CraftSim gates --scenario forge-artisan --gates length,materials,xp,enhancements --runs 30 --seed 1 --recipes-per-band 6 --repo . --out design/testing/crafting-revamp/forge-artisan-gates.md`
- Seed 1; 30 runs per case.

## Inputs

- Scenario: Plasma Forge (Artisan line, Cold Working) (`forge-artisan`), Smithery recipes, minigame Forge.
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
| XP rate | Passed | Item crafts: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.01x in 11-20 / no gear). |
| Enhancement parity | Passed | At rank 50 in fully modded gear, the thresholds in force (50% / 85% / 100%) transfer at least 0.90 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 1.01 times today's). Thresholds that meet parity in every band for this quality: 97% / 97% / 100%. |

## Length

### Actions per attempt of the adaptive player at level (item craft at level, perked build)

| Band | Gear | Recipes played | Mean actions per attempt | Shortest recipe | Longest recipe | Stalled attempts | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 12.9 | 11.9 | 14.0 | 0 | 8-20 | Passed |
| 1-10 | mid gear | 6 | 13.7 | 12.1 | 17.3 | 0 | 8-20 | Passed |
| 1-10 | best gear | 6 | 12.3 | 11.0 | 14.3 | 0 | 8-20 | Passed |
| 1-10 | fully modded gear | 6 | 7.5 | 6.7 | 8.6 | 0 | 8-20 | Reported |
| 11-20 | no gear | 6 | 11.5 | 10.0 | 13.7 | 0 | 8-20 | Passed |
| 11-20 | mid gear | 6 | 13.5 | 9.7 | 16.1 | 0 | 8-20 | Passed |
| 11-20 | best gear | 6 | 12.6 | 8.3 | 16.1 | 0 | 8-20 | Passed |
| 11-20 | fully modded gear | 6 | 7.8 | 5.0 | 10.3 | 0 | 8-20 | Reported |
| 21-30 | no gear | 6 | 12.9 | 11.3 | 14.6 | 0 | 8-20 | Passed |
| 21-30 | mid gear | 6 | 11.9 | 11.2 | 12.8 | 0 | 8-20 | Passed |
| 21-30 | best gear | 6 | 10.9 | 10.0 | 12.3 | 0 | 8-20 | Passed |
| 21-30 | fully modded gear | 6 | 6.7 | 6.2 | 7.5 | 0 | 8-20 | Reported |
| 31-40 | no gear | 6 | 13.7 | 12.6 | 15.3 | 0 | 8-20 | Reported |
| 31-40 | mid gear | 6 | 13.6 | 12.2 | 15.2 | 0 | 8-20 | Passed |
| 31-40 | best gear | 6 | 11.9 | 9.3 | 13.9 | 0 | 8-20 | Passed |
| 31-40 | fully modded gear | 6 | 6.8 | 5.6 | 7.8 | 0 | 8-20 | Reported |
| 41-53 | no gear | 6 | 16.1 | 13.4 | 17.7 | 0 | 8-20 | Reported |
| 41-53 | mid gear | 6 | 17.2 | 15.2 | 18.6 | 0 | 8-20 | Reported |
| 41-53 | best gear | 6 | 15.7 | 12.7 | 17.4 | 0 | 8-20 | Passed |
| 41-53 | fully modded gear | 6 | 7.8 | 7.1 | 8.7 | 0 | 8-20 | Reported |

## Economy parity

### Component stacks consumed per item made, failures included, over the recipes the legacy policy makes at least once in the cell (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Parity rule | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 2.76 | 2.70 | 1.02 | 94% | 98% | 74% | 1% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | mid gear | 6 | 2.72 | 2.70 | 1.01 | 97% | 98% | 93% | 11% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | best gear | 6 | 2.67 | 2.67 | 1.00 | 100% | 100% | 100% | 28% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | fully modded gear | 6 | 2.67 | 2.68 | 1.00 | 100% | 99% | 100% | 79% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | no gear | 6 | 2.74 | 2.66 | 1.03 | 88% | 91% | 64% | 2% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | mid gear | 6 | 2.65 | 2.55 | 1.04 | 92% | 97% | 83% | 13% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | best gear | 6 | 2.55 | 2.52 | 1.01 | 97% | 99% | 95% | 29% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | fully modded gear | 6 | 2.50 | 2.50 | 1.00 | 100% | 100% | 100% | 93% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | no gear | 6 | 3.39 | 3.19 | 1.06 | 91% | 99% | 80% | 8% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | mid gear | 6 | 3.19 | 3.18 | 1.00 | 99% | 99% | 97% | 16% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | best gear | 6 | 3.17 | 3.18 | 1.00 | 100% | 99% | 100% | 29% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | fully modded gear | 6 | 3.17 | 3.19 | 0.99 | 100% | 99% | 99% | 90% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | no gear | 6 | 2.91 | 2.86 | 1.01 | 96% | 98% | 78% | 22% | yes | 0% / 0% | within 10% | Reported |
| 31-40 | mid gear | 6 | 2.83 | 2.87 | 0.99 | 100% | 98% | 97% | 38% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | best gear | 6 | 2.85 | 2.85 | 1.00 | 99% | 99% | 98% | 49% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | fully modded gear | 6 | 2.83 | 2.89 | 0.98 | 100% | 97% | 100% | 87% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | no gear | 6 | 3.07 | 3.04 | 1.01 | 97% | 98% | 75% | 43% | yes | 0% / 17% | within 10% | Reported |
| 41-53 | mid gear | 6 | 3.87 | 4.00 | 0.97 | 98% | 86% | 90% | 56% | yes | 0% / 0% | at most +10% | Reported |
| 41-53 | best gear | 6 | 3.86 | 3.89 | 0.99 | 99% | 94% | 95% | 75% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | fully modded gear | 6 | 3.83 | 3.83 | 1.00 | 100% | 100% | 100% | 91% | yes | 0% / 0% | within 10% | Passed |

### Recipes the legacy policy never makes in the cell, left out of the stack and quality comparison above (they still count in the never-made check)

| Band | Gear | Recipe | Level | New success | New avg quality |
| --- | --- | --- | --- | --- | --- |
| 41-53 | no gear | ImmortalBelt | 52 | 97% | 48% |

## XP rate

### Item crafts: XP per hour against the legacy loop (6.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per craft | Legacy actions per craft | Legacy XP per hour | New XP per craft | New actions per craft | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 593 | 6.0 | 59489 | 2005 | 12.9 | 70122 | 1.18 | - | Passed |
| 1-10 | no gear, 1 enhancement | 442 | 6.9 | 38624 | 1539 | 12.2 | 56966 | 1.47 | - | Passed |
| 1-10 | no gear, 2 enhancements | 305 | 6.6 | 27851 | 1069 | 11.7 | 41101 | 1.48 | - | Passed |
| 1-10 | no gear, 3 enhancements | 209 | 6.9 | 18130 | 592 | 11.5 | 23082 | 1.27 | - | Passed |
| 1-10 | mid gear | 656 | 8.8 | 44601 | 2259 | 13.7 | 74097 | 1.66 | - | Passed |
| 1-10 | mid gear, 1 enhancement | 663 | 8.8 | 45415 | 1970 | 14.1 | 62708 | 1.38 | - | Passed |
| 1-10 | mid gear, 2 enhancements | 596 | 9.6 | 37414 | 1426 | 13.8 | 46672 | 1.25 | - | Passed |
| 1-10 | mid gear, 3 enhancements | 549 | 10.2 | 32305 | 1037 | 13.4 | 34727 | 1.07 | - | Passed |
| 1-10 | best gear | 770 | 10.3 | 44808 | 2361 | 12.3 | 86416 | 1.93 | - | Passed |
| 1-10 | best gear, 1 enhancement | 740 | 10.9 | 40867 | 2265 | 13.4 | 75906 | 1.86 | - | Passed |
| 1-10 | best gear, 2 enhancements | 712 | 11.0 | 38779 | 1990 | 14.4 | 62145 | 1.60 | - | Passed |
| 1-10 | best gear, 3 enhancements | 702 | 11.2 | 37692 | 1545 | 14.9 | 46509 | 1.23 | - | Passed |
| 1-10 | fully modded gear | 1071 | 8.5 | 75652 | 2363 | 7.5 | 142083 | 1.88 | - | Passed |
| 1-10 | fully modded gear, 1 enhancement | 1049 | 8.5 | 74450 | 2364 | 8.7 | 122572 | 1.65 | - | Passed |
| 1-10 | fully modded gear, 2 enhancements | 1047 | 8.3 | 75577 | 2364 | 9.5 | 111966 | 1.48 | - | Passed |
| 1-10 | fully modded gear, 3 enhancements | 1053 | 8.8 | 71474 | 2351 | 10.5 | 100778 | 1.41 | - | Passed |
| 11-20 | no gear | 566 | 7.9 | 43163 | 1105 | 11.4 | 43426 | 1.01 | - | Passed |
| 11-20 | no gear, 1 enhancement | 409 | 7.9 | 30965 | 943 | 12.0 | 35261 | 1.14 | - | Passed |
| 11-20 | no gear, 2 enhancements | 266 | 7.2 | 22260 | 838 | 12.0 | 31403 | 1.41 | - | Passed |
| 11-20 | no gear, 3 enhancements | 262 | 7.7 | 20334 | 602 | 12.1 | 22443 | 1.10 | - | Passed |
| 11-20 | mid gear | 664 | 11.1 | 35793 | 1268 | 13.5 | 42292 | 1.18 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 658 | 11.8 | 33330 | 1225 | 15.3 | 36135 | 1.08 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 622 | 12.4 | 30075 | 1128 | 15.8 | 32038 | 1.07 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 597 | 13.5 | 26634 | 1055 | 16.1 | 29501 | 1.11 | - | Passed |
| 11-20 | best gear | 766 | 14.4 | 31957 | 1387 | 12.6 | 49584 | 1.55 | - | Passed |
| 11-20 | best gear, 1 enhancement | 759 | 15.2 | 30033 | 1348 | 14.0 | 43385 | 1.44 | - | Passed |
| 11-20 | best gear, 2 enhancements | 744 | 16.0 | 27962 | 1332 | 15.5 | 38773 | 1.39 | - | Passed |
| 11-20 | best gear, 3 enhancements | 753 | 16.5 | 27328 | 1283 | 16.5 | 34956 | 1.28 | - | Passed |
| 11-20 | fully modded gear | 1155 | 14.2 | 48714 | 1440 | 7.8 | 83119 | 1.71 | - | Passed |
| 11-20 | fully modded gear, 1 enhancement | 1118 | 14.2 | 47354 | 1440 | 8.6 | 75275 | 1.59 | - | Passed |
| 11-20 | fully modded gear, 2 enhancements | 1135 | 14.6 | 46523 | 1439 | 9.6 | 67632 | 1.45 | - | Passed |
| 11-20 | fully modded gear, 3 enhancements | 1136 | 14.7 | 46374 | 1439 | 10.3 | 62824 | 1.35 | - | Passed |
| 21-30 | no gear | 639 | 10.2 | 37566 | 1508 | 12.8 | 52823 | 1.41 | - | Passed |
| 21-30 | no gear, 1 enhancement | 618 | 11.5 | 32333 | 1610 | 13.5 | 53631 | 1.66 | - | Passed |
| 21-30 | no gear, 2 enhancements | 592 | 12.4 | 28664 | 1268 | 14.2 | 40177 | 1.40 | - | Passed |
| 21-30 | no gear, 3 enhancements | 575 | 13.5 | 25564 | 815 | 14.0 | 26111 | 1.02 | - | Passed |
| 21-30 | mid gear | 691 | 16.5 | 25152 | 1719 | 11.9 | 65079 | 2.59 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 679 | 16.8 | 24257 | 1697 | 13.7 | 55629 | 2.29 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 665 | 17.4 | 22996 | 1659 | 15.0 | 49634 | 2.16 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 647 | 17.6 | 22047 | 1433 | 16.2 | 39697 | 1.80 | - | Passed |
| 21-30 | best gear | 770 | 21.3 | 21631 | 1750 | 10.9 | 71975 | 3.33 | - | Passed |
| 21-30 | best gear, 1 enhancement | 754 | 21.6 | 20987 | 1742 | 12.0 | 65205 | 3.11 | - | Passed |
| 21-30 | best gear, 2 enhancements | 752 | 22.0 | 20504 | 1740 | 13.5 | 58122 | 2.83 | - | Passed |
| 21-30 | best gear, 3 enhancements | 744 | 21.9 | 20368 | 1644 | 15.1 | 48943 | 2.40 | - | Passed |
| 21-30 | fully modded gear | 1133 | 20.7 | 32818 | 1747 | 6.7 | 116878 | 3.56 | - | Passed |
| 21-30 | fully modded gear, 1 enhancement | 1121 | 21.4 | 31471 | 1752 | 7.6 | 103718 | 3.30 | - | Passed |
| 21-30 | fully modded gear, 2 enhancements | 1117 | 21.3 | 31430 | 1752 | 8.3 | 95239 | 3.03 | - | Passed |
| 21-30 | fully modded gear, 3 enhancements | 1113 | 21.9 | 30542 | 1752 | 9.7 | 81635 | 2.67 | - | Passed |
| 31-40 | no gear | 724 | 14.7 | 29617 | 2154 | 13.7 | 70546 | 2.38 | - | Passed |
| 31-40 | no gear, 1 enhancement | 648 | 15.5 | 25053 | 1885 | 14.6 | 58162 | 2.32 | - | Passed |
| 31-40 | no gear, 2 enhancements | 632 | 15.2 | 25015 | 1329 | 14.6 | 40957 | 1.64 | - | Passed |
| 31-40 | no gear, 3 enhancements | 609 | 16.1 | 22695 | 855 | 14.5 | 26452 | 1.17 | - | Passed |
| 31-40 | mid gear | 819 | 19.9 | 24744 | 2419 | 13.6 | 79913 | 3.23 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 788 | 21.1 | 22421 | 2204 | 15.0 | 65974 | 2.94 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 756 | 20.5 | 22069 | 1743 | 15.7 | 50119 | 2.27 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 716 | 20.2 | 21265 | 1250 | 16.0 | 35078 | 1.65 | - | Passed |
| 31-40 | best gear | 888 | 24.8 | 21529 | 2427 | 11.9 | 92024 | 4.27 | - | Passed |
| 31-40 | best gear, 1 enhancement | 895 | 26.0 | 20640 | 2399 | 13.7 | 78669 | 3.81 | - | Passed |
| 31-40 | best gear, 2 enhancements | 860 | 26.0 | 19811 | 2202 | 15.2 | 65228 | 3.29 | - | Passed |
| 31-40 | best gear, 3 enhancements | 836 | 24.7 | 20302 | 1854 | 16.4 | 50918 | 2.51 | - | Passed |
| 31-40 | fully modded gear | 1110 | 25.0 | 26670 | 2456 | 6.8 | 162141 | 6.08 | - | Passed |
| 31-40 | fully modded gear, 1 enhancement | 1128 | 25.3 | 26798 | 2459 | 7.5 | 147654 | 5.51 | - | Passed |
| 31-40 | fully modded gear, 2 enhancements | 1129 | 25.9 | 26147 | 2434 | 9.6 | 114614 | 4.38 | - | Passed |
| 31-40 | fully modded gear, 3 enhancements | 1125 | 25.9 | 26105 | 2395 | 11.4 | 94764 | 3.63 | - | Passed |
| 41-53 | no gear | 726 | 18.1 | 24140 | 1134 | 16.1 | 31717 | 1.31 | - | Passed |
| 41-53 | no gear, 1 enhancement | 644 | 16.1 | 23941 | 992 | 15.4 | 28974 | 1.21 | - | Passed |
| 41-53 | no gear, 2 enhancements | 578 | 15.9 | 21849 | 725 | 14.8 | 22079 | 1.01 | - | Passed |
| 41-53 | no gear, 3 enhancements | 539 | 16.5 | 19605 | 455 | 15.0 | 13665 | 0.70 | - | Passed |
| 41-53 | mid gear | 871 | 24.4 | 21396 | 1278 | 17.2 | 33384 | 1.56 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 816 | 22.8 | 21507 | 1204 | 16.8 | 32231 | 1.50 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 730 | 20.6 | 21270 | 975 | 16.9 | 25919 | 1.22 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 701 | 19.9 | 21174 | 728 | 16.4 | 19929 | 0.94 | - | Passed |
| 41-53 | best gear | 1041 | 28.2 | 22172 | 1321 | 15.7 | 37841 | 1.71 | - | Passed |
| 41-53 | best gear, 1 enhancement | 1016 | 28.7 | 21237 | 1334 | 16.1 | 37329 | 1.76 | - | Passed |
| 41-53 | best gear, 2 enhancements | 901 | 26.1 | 20723 | 1246 | 17.4 | 32204 | 1.55 | - | Passed |
| 41-53 | best gear, 3 enhancements | 844 | 23.8 | 21266 | 1118 | 17.8 | 28202 | 1.33 | - | Passed |
| 41-53 | fully modded gear | 1181 | 27.8 | 25509 | 1362 | 7.8 | 78816 | 3.09 | - | Passed |
| 41-53 | fully modded gear, 1 enhancement | 1164 | 27.8 | 25150 | 1362 | 9.0 | 68240 | 2.71 | - | Passed |
| 41-53 | fully modded gear, 2 enhancements | 1175 | 28.8 | 24519 | 1356 | 10.7 | 57157 | 2.33 | - | Passed |
| 41-53 | fully modded gear, 3 enhancements | 1125 | 28.6 | 23611 | 1353 | 12.0 | 50777 | 2.15 | - | Passed |

## Enhancement parity

### Rank 50, fully modded gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 100%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 6 | 60-70 | 34% | 97% | 99% | 0.96 | 100% | 99.9% | 1.00 (+/- 0.00) | 1.04 | >= 0.95 | Passed |
| 41-45 | 2 | 6 | 60-70 | 67% | 97% | 99% | 1.93 | 100% | 100.0% | 2.00 (+/- 0.00) | 1.03 | >= 0.95 | Passed |
| 41-45 | 3 | 6 | 60-70 | 101% | 98% | 99% | 2.91 | 100% | 100.0% | 2.97 (+/- 0.01) | 1.02 | >= 0.90 | Passed |
| 46-50 | 1 | 6 | 60 | 32% | 100% | 99% | 0.99 | 100% | 100.0% | 1.00 (+/- 0.00) | 1.01 | >= 0.95 | Passed |
| 46-50 | 2 | 6 | 60 | 64% | 100% | 99% | 1.98 | 100% | 99.9% | 2.00 (+/- 0.00) | 1.01 | >= 0.95 | Passed |
| 46-50 | 3 | 6 | 60 | 96% | 97% | 99% | 2.87 | 99% | 99.9% | 2.95 (+/- 0.02) | 1.03 | >= 0.90 | Passed |
| 51-53 | 1 | 6 | 60-70 | 8% | 98% | 40% | 0.39 | 100% | 99.8% | 1.00 (+/- 0.00) | 2.56 | >= 0.95 | Passed |
| 51-53 | 2 | 6 | 60-70 | 17% | 100% | 46% | 0.91 | 100% | 99.8% | 2.00 (+/- 0.00) | 2.20 | >= 0.95 | Passed |
| 51-53 | 3 | 6 | 60-70 | 25% | 100% | 41% | 1.22 | 99% | 99.9% | 2.88 (+/- 0.03) | 2.35 | >= 0.90 | Passed |

### Thresholds, first / second / third slot, that would meet parity for the quality measured (the strictest set that keeps every case at least at today's, never falling from one slot to the next)

| Scope | Thresholds that meet parity | In steps of 5 | Thresholds in force |
| --- | --- | --- | --- |
| Every band together (the shared thresholds) | 97% / 97% / 100% | 95% / 95% / 100% | 50% / 85% / 100% |
| Band 41-45 alone | 99% / 100% / 100% | 95% / 100% / 100% | 50% / 85% / 100% |
| Band 46-50 alone | 97% / 97% / 100% | 95% / 95% / 100% | 50% / 85% / 100% |
| Band 51-53 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 100% |

### Rank 50, fully modded gear: quality of the items the new minigame made, in whole percent, by band and slots filled

| Band | Slots filled | Items made | 10th percentile | 25th percentile | Median | 75th percentile | 90th percentile | Made at 100% |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 94% |
| 41-45 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 99% |
| 41-45 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 97% |
| 46-50 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 96% |
| 46-50 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 95% |
| 46-50 | 3 | 99% | 100% | 100% | 100% | 100% | 100% | 96% |
| 51-53 | 1 | 100% | 99% | 100% | 100% | 100% | 100% | 82% |
| 51-53 | 2 | 100% | 99% | 100% | 100% | 100% | 100% | 87% |
| 51-53 | 3 | 99% | 99% | 100% | 100% | 100% | 100% | 89% |

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
- Every craft in this scenario is played by a crafter who bought the Artisan line in full (every purchase their Smithery rank allows) and plays Cold Working, so no gate here is an unperked result.
