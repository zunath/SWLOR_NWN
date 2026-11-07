# Crafting revamp: balance gates of the Kitchen minigame

- Command: `SWLOR.CraftSim gates --scenario kitchen --gates length,materials,xp,enhancements,ladder --runs 40 --seed 1 --repo . --out design/testing/crafting-revamp/kitchen-gates.md`
- Seed 1; 40 runs per case.

## Inputs

- Scenario: Kitchen (`kitchen`), Agriculture recipes, minigame Kitchen.
- Quality: the quality meter's share of its maximum when the item is made, in points (0-100). A broken or abandoned piece counts 0, so every average includes the crafts that failed.
- Crafter: the rank the scenario's cohort gives each recipe (by default the recipe's level, at most 50, so a crafter works at their own level), with no food or status bonuses and no perks unless a gate names a style or the scenario's limitations say it plays a perk loadout. Gear levels are the plan's none, middle and best tier of the skill's crafting gear:
  - no gear (Control 0, Craftsmanship 0, CP 0)
  - mid gear (Control 13, Craftsmanship 12, CP 20)
  - best gear (Control 29, Craftsmanship 30, CP 37)
  - fully modded gear (Control 119, Craftsmanship 120, CP 37)
- Recipes: 1-10: 6 of 58; 11-20: 6 of 59; 21-30: 6 of 65; 31-40: 6 of 61; 41-53: 6 of 88 recipes played.
- Adaptive player: depth 3, forecast samples per result 1, budget 1500 engine copies per decision, 0.5% of value lost per extra action, heuristic KitchenLeafHeuristic. Each attempt's dice come from its own stream of the seed.
- Fixed-rotation solver: beam 8, cycles up to 3, 8 adaptive lines as seeds, up to 40 actions, 64 training attempts per local-search round and 150 selection attempts for each of 12 finalists, local search from the best 3 candidate(s) for up to 3 round(s) each. Gate 1 searches one sequence for each band and gear tier across the band's sampled recipes (every recipe weighing the same; an action a recipe's engine cannot take is skipped there), and also each recipe's own best sequence as a reported metric. The winner is chosen on separate attempts and then scored on a third set (hold-out) with 40 attempts per recipe.
- Legacy comparator (gates 5 and 6): the old minigame's lookahead player (`SWLOR.CraftSim baseline`), replayed here with the same seed on the same recipes, ranks and gear.
- Time: 8.0 s per action in the new window and 6.0 s in the old one, plus 0.0 s per craft in both. These are estimates; the client evidence replaces them.
- XP multiplier on the base XP of every craft, failures included, as the game server applies it (`CraftMinigameXp`): 1-10 2.15x, 11-20 1.15x, 21-30 1.12x, 31-40 1.20x, 41-53 1.05x. The new side's XP below includes it; the legacy loop's does not.
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
  - `EnhancementParityFullModsRatio` = 0.95: gate 7, minigames tuned against full mods: the same ratio for one and two slots in fully modded gear
  - `EnhancementParityThreeSlotFullModsRatio` = 0.9: gate 7, minigames tuned against full mods: the same ratio for three slots in fully modded gear
  - `PerksOptionalAboveRankFullModsShare` = 35%: gate 3, minigames tuned against full mods: the share in the band above the crafter's rank
  - `GearLadderBands` = 46-50, 51-53: gear ladder: the at-level recipe bands
  - `GearLadderEnhancements` = 2: gear ladder: enhancement kits played
  - `GearLadderMinimumStep` = 5: gear ladder: the least each gear step adds to average quality
  - `GearLadderSaturatedQuality` = 97: gear ladder and gate 1: an average at or above this has nothing left to earn
  - `GearLadderFullModsReach85Share` = 60%: gear ladder: share of top-band crafts in fully modded gear that reach 85%
  - `GearLadderFullModsMaxFullQualityShare` = 90%: gear ladder: most top-band crafts in fully modded gear that reach 100%
  - `GearLadderPerkAllowance` = 5: gear ladder: how far a full perk line in no gear may beat unmodded best gear
  - `UnderGearedNoGearFromLevel` = 31: gates 4, 5 and 6: no gear is only reported from this recipe level
  - `UnderGearedMidGearFromLevel` = 41: gates 4, 5 and 6: mid gear is only reported from this recipe level
  - `RecipesPerBand` = 6: recipes played per band
  - `SolverOptions` = the scenario's: effort of the fixed-rotation solver
  - `ActionLimit` = 100: actions after which an attempt counts as stalled
- Length target: 10-20 actions (item craft at level).
- Enhancement thresholds in force (`CraftOutcomeRules`, read by gate 7): 50% / 85% / 100% for slots 1, 2 and 3. The scenario applies enhancements to its engine's progress meter.
- Styles compared: Slow Cooking vs Flash Cooking (from rank 20); Slow Cooking (full line) vs Flash Cooking (full line) (from rank 20); Slow Cooking (full line, Seasoned Palate on) vs Flash Cooking (full line, Seasoned Palate on) (from rank 20).
- Gates run: Length, Economy parity, XP rate, Enhancement parity, Gear ladder. The others were not run, so this report cannot show that the minigame passes.

## Verdicts

| Gate | Verdict | Summary |
| --- | --- | --- |
| Length | Passed | Every band and gear tier takes 10-20 actions (item craft at level) on average. |
| Economy parity | Passed | Component stacks per item are within 10% of the legacy baseline where its success rate is at least 90%, and at most 10% above it where it is lower, at no less quality, in every band and gear tier. |
| XP rate | Passed | Item crafts: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.03x in 11-20 / no gear). |
| Enhancement parity | Passed | At rank 50 in fully modded gear, the thresholds in force (50% / 85% / 100%) transfer at least 0.90 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 0.92 times today's). Thresholds that meet parity in every band for this quality: 40% / 40% / 58%. |
| Gear ladder | Failed | 51-53 / Flash Cooking (full line) with no gear averages 83.6 against unmodded best gear's 77.8. |

## Length

### Actions per attempt of the adaptive player at level (item craft at level)

| Band | Gear | Recipes played | Mean actions per attempt | Shortest recipe | Longest recipe | Stalled attempts | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 12.7 | 9.9 | 16.4 | 0 | 10-20 | Passed |
| 1-10 | mid gear | 6 | 11.4 | 8.1 | 15.4 | 0 | 10-20 | Passed |
| 1-10 | best gear | 6 | 10.2 | 6.8 | 14.8 | 0 | 10-20 | Passed |
| 1-10 | fully modded gear | 6 | 8.1 | 5.4 | 10.8 | 0 | 10-20 | Reported |
| 11-20 | no gear | 6 | 13.6 | 11.6 | 17.3 | 0 | 10-20 | Passed |
| 11-20 | mid gear | 6 | 11.6 | 8.8 | 16.3 | 0 | 10-20 | Passed |
| 11-20 | best gear | 6 | 11.5 | 9.3 | 15.6 | 0 | 10-20 | Passed |
| 11-20 | fully modded gear | 6 | 8.6 | 7.2 | 12.6 | 0 | 10-20 | Reported |
| 21-30 | no gear | 6 | 14.1 | 10.1 | 19.5 | 0 | 10-20 | Passed |
| 21-30 | mid gear | 6 | 12.3 | 8.8 | 18.2 | 0 | 10-20 | Passed |
| 21-30 | best gear | 6 | 12.3 | 8.2 | 18.4 | 0 | 10-20 | Passed |
| 21-30 | fully modded gear | 6 | 9.1 | 6.1 | 12.7 | 0 | 10-20 | Reported |
| 31-40 | no gear | 6 | 17.4 | 14.6 | 20.5 | 0 | 10-20 | Reported |
| 31-40 | mid gear | 6 | 16.8 | 11.9 | 21.4 | 0 | 10-20 | Passed |
| 31-40 | best gear | 6 | 15.2 | 10.2 | 18.8 | 0 | 10-20 | Passed |
| 31-40 | fully modded gear | 6 | 11.0 | 7.3 | 14.1 | 0 | 10-20 | Reported |
| 41-53 | no gear | 6 | 18.5 | 14.8 | 21.2 | 0 | 10-20 | Reported |
| 41-53 | mid gear | 6 | 17.1 | 13.2 | 20.0 | 0 | 10-20 | Reported |
| 41-53 | best gear | 6 | 16.3 | 12.1 | 19.8 | 0 | 10-20 | Passed |
| 41-53 | fully modded gear | 6 | 11.8 | 9.3 | 14.1 | 0 | 10-20 | Reported |

## Economy parity

### Component stacks consumed per item made, failures included, over the recipes the legacy policy makes at least once in the cell (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Parity rule | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 1.52 | 1.54 | 0.99 | 96% | 98% | 87% | 1% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | mid gear | 6 | 1.51 | 1.54 | 0.98 | 100% | 96% | 93% | 8% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | best gear | 6 | 1.50 | 1.52 | 0.99 | 100% | 99% | 97% | 17% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | fully modded gear | 6 | 1.50 | 1.52 | 0.99 | 100% | 98% | 99% | 61% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | no gear | 6 | 1.55 | 1.59 | 0.97 | 95% | 93% | 84% | 1% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | mid gear | 6 | 1.53 | 1.52 | 1.01 | 98% | 99% | 91% | 12% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | best gear | 6 | 1.50 | 1.51 | 0.99 | 100% | 99% | 96% | 32% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | fully modded gear | 6 | 1.50 | 1.51 | 0.99 | 100% | 99% | 99% | 95% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | no gear | 6 | 1.66 | 1.64 | 1.01 | 97% | 97% | 86% | 8% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | mid gear | 6 | 1.63 | 1.65 | 0.99 | 99% | 98% | 93% | 15% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | best gear | 6 | 1.64 | 1.63 | 1.00 | 100% | 99% | 93% | 29% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | fully modded gear | 6 | 1.62 | 1.63 | 1.00 | 100% | 100% | 98% | 92% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | no gear | 6 | 2.05 | 2.03 | 1.01 | 97% | 99% | 84% | 8% | yes | 0% / 0% | within 10% | Reported |
| 31-40 | mid gear | 6 | 2.04 | 2.02 | 1.01 | 97% | 98% | 88% | 14% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | best gear | 6 | 2.03 | 2.00 | 1.01 | 99% | 100% | 93% | 25% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | fully modded gear | 6 | 2.00 | 2.00 | 1.00 | 100% | 100% | 97% | 84% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | no gear | 6 | 2.30 | 2.18 | 1.05 | 92% | 99% | 81% | 44% | yes | 0% / 17% | within 10% | Reported |
| 41-53 | mid gear | 6 | 2.07 | 2.23 | 0.93 | 95% | 86% | 85% | 59% | yes | 0% / 0% | at most +10% | Reported |
| 41-53 | best gear | 6 | 2.06 | 2.09 | 0.98 | 95% | 94% | 88% | 79% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | fully modded gear | 6 | 2.01 | 2.03 | 0.99 | 100% | 97% | 98% | 89% | yes | 0% / 0% | within 10% | Passed |

### Recipes the legacy policy never makes in the cell, left out of the stack and quality comparison above (they still count in the never-made check)

| Band | Gear | Recipe | Level | New success | New avg quality |
| --- | --- | --- | --- | --- | --- |
| 41-53 | no gear | FoodSubmissionTokenAgriculture | 52 | 78% | 45% |

## XP rate

### Item crafts: XP per hour against the legacy loop (6.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per craft | Legacy actions per craft | Legacy XP per hour | New XP per craft | New actions per craft | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 594 | 4.4 | 80547 | 2365 | 12.7 | 83785 | 1.04 | - | Passed |
| 1-10 | no gear, 1 enhancement | 490 | 5.8 | 50602 | 2333 | 12.1 | 87070 | 1.72 | - | Passed |
| 1-10 | no gear, 2 enhancements | 169 | 6.0 | 16862 | 2314 | 12.3 | 84942 | 5.04 | - | Passed |
| 1-10 | no gear, 3 enhancements | 90 | 6.0 | 9000 | 2314 | 11.9 | 87317 | 9.70 | - | Passed |
| 1-10 | mid gear | 622 | 5.3 | 70958 | 2500 | 11.4 | 98969 | 1.39 | - | Passed |
| 1-10 | mid gear, 1 enhancement | 588 | 5.4 | 64779 | 2512 | 10.7 | 105243 | 1.62 | - | Passed |
| 1-10 | mid gear, 2 enhancements | 569 | 6.1 | 55620 | 2480 | 10.9 | 102704 | 1.85 | - | Passed |
| 1-10 | mid gear, 3 enhancements | 495 | 7.0 | 42624 | 2476 | 11.1 | 100085 | 2.35 | - | Passed |
| 1-10 | best gear | 690 | 5.4 | 76106 | 2548 | 10.2 | 112173 | 1.47 | - | Passed |
| 1-10 | best gear, 1 enhancement | 658 | 5.5 | 72296 | 2561 | 9.4 | 122423 | 1.69 | - | Passed |
| 1-10 | best gear, 2 enhancements | 653 | 6.0 | 64899 | 2546 | 9.6 | 118921 | 1.83 | - | Passed |
| 1-10 | best gear, 3 enhancements | 628 | 6.1 | 61844 | 2568 | 9.7 | 119619 | 1.93 | - | Passed |
| 1-10 | fully modded gear | 921 | 4.8 | 116191 | 2570 | 8.1 | 142854 | 1.23 | - | Passed |
| 1-10 | fully modded gear, 1 enhancement | 826 | 4.0 | 123228 | 2577 | 7.5 | 153693 | 1.25 | - | Passed |
| 1-10 | fully modded gear, 2 enhancements | 848 | 4.2 | 120973 | 2568 | 7.3 | 157645 | 1.30 | - | Passed |
| 1-10 | fully modded gear, 3 enhancements | 805 | 4.4 | 108780 | 2566 | 7.7 | 149374 | 1.37 | - | Passed |
| 11-20 | no gear | 573 | 8.6 | 39900 | 1251 | 13.6 | 41281 | 1.03 | - | Passed |
| 11-20 | no gear, 1 enhancement | 345 | 9.3 | 22368 | 1155 | 15.2 | 34119 | 1.53 | - | Passed |
| 11-20 | no gear, 2 enhancements | 90 | 6.5 | 8308 | 1182 | 14.9 | 35594 | 4.28 | - | Passed |
| 11-20 | no gear, 3 enhancements | 90 | 6.5 | 8308 | 1165 | 14.9 | 35079 | 4.22 | - | Passed |
| 11-20 | mid gear | 665 | 10.7 | 37367 | 1316 | 11.6 | 51057 | 1.37 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 634 | 11.5 | 33124 | 1292 | 13.5 | 43208 | 1.30 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 586 | 13.0 | 26982 | 1323 | 12.8 | 46333 | 1.72 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 581 | 13.5 | 25866 | 1272 | 13.6 | 41993 | 1.62 | - | Passed |
| 11-20 | best gear | 784 | 14.6 | 32210 | 1358 | 11.5 | 53332 | 1.66 | - | Passed |
| 11-20 | best gear, 1 enhancement | 769 | 14.8 | 31202 | 1354 | 11.6 | 52586 | 1.69 | - | Passed |
| 11-20 | best gear, 2 enhancements | 760 | 16.1 | 28412 | 1330 | 11.7 | 50962 | 1.79 | - | Passed |
| 11-20 | best gear, 3 enhancements | 732 | 15.9 | 27622 | 1342 | 12.3 | 49059 | 1.78 | - | Passed |
| 11-20 | fully modded gear | 1161 | 13.6 | 51363 | 1376 | 8.6 | 71833 | 1.40 | - | Passed |
| 11-20 | fully modded gear, 1 enhancement | 1157 | 13.4 | 51971 | 1377 | 9.4 | 65940 | 1.27 | - | Passed |
| 11-20 | fully modded gear, 2 enhancements | 1164 | 14.3 | 49013 | 1365 | 9.3 | 66249 | 1.35 | - | Passed |
| 11-20 | fully modded gear, 3 enhancements | 1169 | 14.2 | 49401 | 1374 | 8.9 | 69355 | 1.40 | - | Passed |
| 21-30 | no gear | 632 | 9.9 | 38173 | 1256 | 14.1 | 39969 | 1.05 | - | Passed |
| 21-30 | no gear, 1 enhancement | 618 | 11.3 | 32873 | 1214 | 15.7 | 34796 | 1.06 | - | Passed |
| 21-30 | no gear, 2 enhancements | 600 | 12.4 | 29120 | 1185 | 16.1 | 33084 | 1.14 | - | Passed |
| 21-30 | no gear, 3 enhancements | 590 | 13.0 | 27232 | 1175 | 16.2 | 32687 | 1.20 | - | Passed |
| 21-30 | mid gear | 680 | 15.3 | 26591 | 1305 | 12.3 | 47751 | 1.80 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 678 | 16.0 | 25491 | 1278 | 14.3 | 40242 | 1.58 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 663 | 16.3 | 24429 | 1259 | 15.2 | 37314 | 1.53 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 646 | 16.5 | 23534 | 1249 | 15.5 | 36287 | 1.54 | - | Passed |
| 21-30 | best gear | 772 | 21.0 | 22011 | 1311 | 12.3 | 48129 | 2.19 | - | Passed |
| 21-30 | best gear, 1 enhancement | 767 | 21.3 | 21584 | 1303 | 13.2 | 44534 | 2.06 | - | Passed |
| 21-30 | best gear, 2 enhancements | 751 | 21.9 | 20554 | 1298 | 13.1 | 44432 | 2.16 | - | Passed |
| 21-30 | best gear, 3 enhancements | 745 | 21.8 | 20463 | 1302 | 13.4 | 43650 | 2.13 | - | Passed |
| 21-30 | fully modded gear | 1145 | 20.4 | 33732 | 1335 | 9.1 | 65899 | 1.95 | - | Passed |
| 21-30 | fully modded gear, 1 enhancement | 1135 | 20.9 | 32602 | 1339 | 9.8 | 61676 | 1.89 | - | Passed |
| 21-30 | fully modded gear, 2 enhancements | 1121 | 20.8 | 32334 | 1337 | 9.4 | 63883 | 1.98 | - | Passed |
| 21-30 | fully modded gear, 3 enhancements | 1128 | 21.0 | 32163 | 1333 | 10.1 | 59345 | 1.85 | - | Passed |
| 31-40 | no gear | 644 | 12.0 | 32200 | 1314 | 17.4 | 33993 | 1.06 | - | Passed |
| 31-40 | no gear, 1 enhancement | 610 | 13.9 | 26362 | 1197 | 17.8 | 30243 | 1.15 | - | Passed |
| 31-40 | no gear, 2 enhancements | 596 | 14.4 | 24754 | 1193 | 18.1 | 29696 | 1.20 | - | Passed |
| 31-40 | no gear, 3 enhancements | 576 | 15.4 | 22484 | 1108 | 18.4 | 27059 | 1.20 | - | Passed |
| 31-40 | mid gear | 675 | 16.6 | 24429 | 1342 | 16.8 | 36001 | 1.47 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 658 | 18.1 | 21812 | 1234 | 17.3 | 32125 | 1.47 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 658 | 18.4 | 21497 | 1251 | 17.9 | 31461 | 1.46 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 637 | 18.3 | 20926 | 1257 | 17.6 | 32102 | 1.53 | - | Passed |
| 31-40 | best gear | 749 | 23.5 | 19123 | 1388 | 15.2 | 41090 | 2.15 | - | Passed |
| 31-40 | best gear, 1 enhancement | 735 | 24.8 | 17801 | 1323 | 16.3 | 36556 | 2.05 | - | Passed |
| 31-40 | best gear, 2 enhancements | 732 | 25.6 | 17194 | 1319 | 15.8 | 37482 | 2.18 | - | Passed |
| 31-40 | best gear, 3 enhancements | 719 | 24.2 | 17840 | 1305 | 16.6 | 35301 | 1.98 | - | Passed |
| 31-40 | fully modded gear | 1095 | 25.7 | 25572 | 1424 | 11.0 | 58222 | 2.28 | - | Passed |
| 31-40 | fully modded gear, 1 enhancement | 1086 | 25.8 | 25287 | 1419 | 11.7 | 54594 | 2.16 | - | Passed |
| 31-40 | fully modded gear, 2 enhancements | 1089 | 26.4 | 24782 | 1412 | 12.5 | 50698 | 2.05 | - | Passed |
| 31-40 | fully modded gear, 3 enhancements | 1068 | 26.5 | 24221 | 1416 | 12.3 | 51640 | 2.13 | - | Passed |
| 41-53 | no gear | 726 | 17.3 | 25168 | 1070 | 18.5 | 26063 | 1.04 | - | Passed |
| 41-53 | no gear, 1 enhancement | 556 | 14.5 | 22930 | 1136 | 19.9 | 25670 | 1.12 | - | Passed |
| 41-53 | no gear, 2 enhancements | 512 | 14.4 | 21412 | 1123 | 20.7 | 24470 | 1.14 | - | Passed |
| 41-53 | no gear, 3 enhancements | 450 | 14.3 | 18909 | 1093 | 20.6 | 23837 | 1.26 | - | Passed |
| 41-53 | mid gear | 872 | 23.7 | 22098 | 1164 | 17.1 | 30589 | 1.38 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 683 | 20.6 | 19869 | 1205 | 18.8 | 28922 | 1.46 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 634 | 18.9 | 20092 | 1196 | 19.7 | 27292 | 1.36 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 576 | 17.1 | 20191 | 1161 | 20.6 | 25325 | 1.25 | - | Passed |
| 41-53 | best gear | 1043 | 27.9 | 22429 | 1189 | 16.3 | 32872 | 1.47 | - | Passed |
| 41-53 | best gear, 1 enhancement | 939 | 28.1 | 20090 | 1261 | 17.6 | 32292 | 1.61 | - | Passed |
| 41-53 | best gear, 2 enhancements | 812 | 24.5 | 19868 | 1217 | 18.2 | 30093 | 1.51 | - | Passed |
| 41-53 | best gear, 3 enhancements | 722 | 21.3 | 20330 | 1224 | 18.4 | 29969 | 1.47 | - | Passed |
| 41-53 | fully modded gear | 1145 | 27.3 | 25121 | 1297 | 11.8 | 49372 | 1.97 | - | Passed |
| 41-53 | fully modded gear, 1 enhancement | 1145 | 27.8 | 24716 | 1335 | 13.6 | 44053 | 1.78 | - | Passed |
| 41-53 | fully modded gear, 2 enhancements | 1156 | 29.5 | 23523 | 1350 | 13.3 | 45640 | 1.94 | - | Passed |
| 41-53 | fully modded gear, 3 enhancements | 1119 | 29.1 | 23056 | 1352 | 13.5 | 44949 | 1.95 | - | Passed |

## Enhancement parity

### Rank 50, fully modded gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 100%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 6 | 60 | 32% | 99% | 100% | 0.99 | 100% | 99.4% | 0.99 (+/- 0.01) | 1.00 | >= 0.95 | Passed |
| 41-45 | 2 | 6 | 60 | 63% | 98% | 99% | 1.93 | 100% | 98.8% | 1.98 (+/- 0.01) | 1.02 | >= 0.95 | Passed |
| 41-45 | 3 | 6 | 60 | 95% | 98% | 99% | 2.93 | 100% | 96.9% | 2.70 (+/- 0.04) | 0.92 | >= 0.90 | Passed |
| 46-50 | 1 | 6 | 60 | 32% | 97% | 99% | 0.96 | 100% | 98.8% | 0.98 (+/- 0.01) | 1.02 | >= 0.95 | Passed |
| 46-50 | 2 | 6 | 60 | 64% | 99% | 99% | 1.96 | 99% | 98.1% | 1.93 (+/- 0.02) | 0.98 | >= 0.95 | Passed |
| 46-50 | 3 | 6 | 60 | 96% | 100% | 99% | 2.96 | 99% | 98.4% | 2.79 (+/- 0.04) | 0.94 | >= 0.90 | Passed |
| 51-53 | 1 | 6 | 60 | 8% | 100% | 40% | 0.40 | 100% | 93.6% | 0.94 (+/- 0.02) | 2.35 | >= 0.95 | Passed |
| 51-53 | 2 | 6 | 60 | 16% | 100% | 49% | 0.96 | 100% | 93.8% | 1.82 (+/- 0.03) | 1.89 | >= 0.95 | Passed |
| 51-53 | 3 | 6 | 60 | 24% | 98% | 37% | 1.08 | 99% | 94.8% | 2.65 (+/- 0.05) | 2.46 | >= 0.90 | Passed |

### Thresholds, first / second / third slot, that would meet parity for the quality measured (the strictest set that keeps every case at least at today's, never falling from one slot to the next)

| Scope | Thresholds that meet parity | In steps of 5 | Thresholds in force |
| --- | --- | --- | --- |
| Every band together (the shared thresholds) | 40% / 40% / 58% | 40% / 40% / 55% | 50% / 85% / 100% |
| Band 41-45 alone | 44% / 52% / 52% | 45% / 45% / 50% | 50% / 85% / 100% |
| Band 46-50 alone | 40% / 40% / 58% | 40% / 40% / 55% | 50% / 85% / 100% |
| Band 51-53 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 100% |

### Rank 50, fully modded gear: quality of the items the new minigame made, in whole percent, by band and slots filled

| Band | Slots filled | Items made | 10th percentile | 25th percentile | Median | 75th percentile | 90th percentile | Made at 100% |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 100% | 99% | 100% | 100% | 100% | 100% | 88% |
| 41-45 | 2 | 100% | 99% | 100% | 100% | 100% | 100% | 83% |
| 41-45 | 3 | 100% | 99% | 100% | 100% | 100% | 100% | 80% |
| 46-50 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 91% |
| 46-50 | 2 | 99% | 99% | 100% | 100% | 100% | 100% | 86% |
| 46-50 | 3 | 99% | 99% | 100% | 100% | 100% | 100% | 85% |
| 51-53 | 1 | 100% | 81% | 100% | 100% | 100% | 100% | 82% |
| 51-53 | 2 | 100% | 81% | 100% | 100% | 100% | 100% | 79% |
| 51-53 | 3 | 99% | 82% | 100% | 100% | 100% | 100% | 81% |

### Representative enhancements: for each recipe kind the real item at the best level the recipe accepts (the old window refuses one more than 5 levels above the recipe) whose penalty is the lower median of that level

| Recipe kind | Enhancement | Level | Progress penalty | Items of that kind and level (min / median / max penalty) | Recipes |
| --- | --- | --- | --- | --- | --- |
| Food | Cooking Enhancement - Resistance - Mobility V (`cen_res_mob5`) | 45 (tier V) | 60 | 28 items: 50 / 60.0 / 80 | 18 |

## Gear ladder

### Rank 50, 2 kits, no perks: what each step of gear buys

| Band | Recipes | Kind | Gear | Avg quality | Reach 85% | Reach 100% | Actions | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 46-50 | 6 | all | no gear | 71.6 | 56% | 48% | 21.1 | Passed |
| 46-50 | 6 | all | mid gear | 81.6 | 73% | 63% | 19.4 | Passed |
| 46-50 | 6 | all | best gear | 89.7 | 82% | 71% | 18.3 | Passed |
| 46-50 | 6 | all | fully modded gear | 97.3 | 95% | 86% | 13.2 | Passed |
| 51-53 | 6 | all | no gear | 51.1 | 37% | 30% | 22.5 | Passed |
| 51-53 | 6 | all | mid gear | 64.5 | 52% | 45% | 21.2 | Passed |
| 51-53 | 6 | all | best gear | 69.6 | 58% | 49% | 21.1 | Passed |
| 51-53 | 6 | all | fully modded gear | 93.4 | 88% | 79% | 15.9 | Passed |

### Perks do not replace gear: plain crafts, average quality

| Band | Style | Fully perked, no gear | No perks, unmodded best gear | Allowance | Verdict |
| --- | --- | --- | --- | --- | --- |
| 46-50 | Slow Cooking (full line) | 90.6 | 89.6 | +5 | Passed |
| 46-50 | Flash Cooking (full line) | 90.5 | 89.6 | +5 | Passed |
| 46-50 | Slow Cooking (full line, Seasoned Palate on) | 88.3 | 89.6 | +5 | Passed |
| 46-50 | Flash Cooking (full line, Seasoned Palate on) | 90.1 | 89.6 | +5 | Passed |
| 51-53 | Slow Cooking (full line) | 82.2 | 77.8 | +5 | Passed |
| 51-53 | Flash Cooking (full line) | 83.6 | 77.8 | +5 | Failed |
| 51-53 | Slow Cooking (full line, Seasoned Palate on) | 82.1 | 77.8 | +5 | Passed |
| 51-53 | Flash Cooking (full line, Seasoned Palate on) | 79.7 | 77.8 | +5 | Passed |

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
