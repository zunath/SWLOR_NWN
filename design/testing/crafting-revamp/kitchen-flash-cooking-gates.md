# Crafting revamp: balance gates of the Kitchen (Flash Cooking Chef) minigame

- Command: `SWLOR.CraftSim gates --scenario kitchen-flash-cooking --gates materials,xp,enhancements --runs 40 --seed 1 --recipes-per-band 4 --repo . --out design/testing/crafting-revamp/kitchen-flash-cooking-gates.md
- Seed 1; 40 runs per case.

## Inputs

- Scenario: Kitchen (Flash Cooking Chef) (`kitchen-flash-cooking`), Agriculture recipes, minigame Kitchen.
- Quality: the quality meter's share of its maximum when the item is made, in points (0-100). A broken or abandoned piece counts 0, so every average includes the crafts that failed.
- Crafter: the rank the scenario's cohort gives each recipe (by default the recipe's level, at most 50, so a crafter works at their own level), with no food or status bonuses and no perks unless a gate names a style or the scenario's limitations say it plays a perk loadout. Gear levels are the plan's none, middle and best tier of the skill's crafting gear:
  - no gear (Control 0, Craftsmanship 0, CP 0)
  - mid gear (Control 13, Craftsmanship 12, CP 20)
  - best gear (Control 29, Craftsmanship 30, CP 37)
  - fully modded gear (Control 119, Craftsmanship 120, CP 37)
- Recipes: 1-10: 4 of 58; 11-20: 4 of 59; 21-30: 4 of 65; 31-40: 4 of 61; 41-53: 4 of 88 recipes played.
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
  - `RecipesPerBand` = 4: recipes played per band
  - `SolverOptions` = the scenario's: effort of the fixed-rotation solver
  - `ActionLimit` = 100: actions after which an attempt counts as stalled
- Length target: 10-20 actions (item craft at level).
- Enhancement thresholds in force (`CraftOutcomeRules`, read by gate 7): 50% / 85% / 100% for slots 1, 2 and 3. The scenario applies enhancements to its engine's progress meter.
- Styles: none offered, so the style-balance gate is not applicable.
- Gates run: Economy parity, XP rate, Enhancement parity. The others were not run, so this report cannot show that the minigame passes.

## Verdicts

| Gate | Verdict | Summary |
| --- | --- | --- |
| Economy parity | Passed | Component stacks per item are within 10% of the legacy baseline where its success rate is at least 90%, and at most 10% above it where it is lower, at no less quality, in every band and gear tier. |
| XP rate | Failed | Item crafts: XP per hour falls below 1.00x the legacy rate in 1 case(s): 21-30 / no gear, 1 enhancement (0.99x). |
| Enhancement parity | Passed | At rank 50 in fully modded gear, the thresholds in force (50% / 85% / 100%) transfer at least 0.90 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 0.97 times today's). Thresholds that meet parity in every band for this quality: 76% / 97% / 97%. |

## Economy parity

### Component stacks consumed per item made, failures included, over the recipes the legacy policy makes at least once in the cell (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Parity rule | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 4 | 1.81 | 1.82 | 0.99 | 99% | 97% | 94% | 1% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | mid gear | 4 | 1.81 | 1.81 | 1.00 | 99% | 98% | 96% | 7% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | best gear | 4 | 1.80 | 1.81 | 1.00 | 99% | 99% | 99% | 16% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | fully modded gear | 4 | 1.80 | 1.81 | 0.99 | 100% | 98% | 100% | 51% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | no gear | 4 | 2.05 | 2.11 | 0.97 | 96% | 92% | 90% | 1% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | mid gear | 4 | 2.01 | 2.02 | 1.00 | 99% | 99% | 96% | 11% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | best gear | 4 | 2.01 | 2.00 | 1.00 | 99% | 100% | 98% | 30% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | fully modded gear | 4 | 2.00 | 2.01 | 1.00 | 100% | 99% | 99% | 95% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | no gear | 4 | 1.67 | 1.62 | 1.03 | 96% | 97% | 85% | 7% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | mid gear | 4 | 1.67 | 1.61 | 1.04 | 96% | 98% | 88% | 15% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | best gear | 4 | 1.60 | 1.60 | 1.00 | 99% | 99% | 98% | 30% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | fully modded gear | 4 | 1.60 | 1.60 | 1.00 | 100% | 99% | 100% | 91% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | no gear | 4 | 2.30 | 2.29 | 1.00 | 97% | 98% | 91% | 8% | yes | 0% / 0% | within 10% | Reported |
| 31-40 | mid gear | 4 | 2.27 | 2.30 | 0.99 | 99% | 97% | 95% | 14% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | best gear | 4 | 2.26 | 2.26 | 1.00 | 99% | 99% | 96% | 25% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | fully modded gear | 4 | 2.25 | 2.26 | 1.00 | 100% | 99% | 99% | 82% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | no gear | 4 | 1.62 | 1.62 | 1.00 | 98% | 99% | 86% | 43% | yes | 0% / 25% | within 10% | Reported |
| 41-53 | mid gear | 4 | 1.84 | 1.96 | 0.94 | 98% | 79% | 94% | 55% | yes | 0% / 0% | at most +10% | Reported |
| 41-53 | best gear | 4 | 1.83 | 1.88 | 0.98 | 100% | 92% | 96% | 76% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | fully modded gear | 4 | 1.83 | 1.85 | 0.99 | 100% | 99% | 99% | 90% | yes | 0% / 0% | within 10% | Passed |

### Recipes the legacy policy never makes in the cell, left out of the stack and quality comparison above (they still count in the never-made check)

| Band | Gear | Recipe | Level | New success | New avg quality |
| --- | --- | --- | --- | --- | --- |
| 41-53 | no gear | DantooineFlapJack | 52 | 98% | 87% |

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
| 1-10 | fully modded gear | 976 | 5.5 | 105745 | 2579 | 8.8 | 131597 | 1.24 | - | Passed |
| 1-10 | fully modded gear, 1 enhancement | 844 | 4.4 | 113911 | 2578 | 8.6 | 135296 | 1.19 | - | Passed |
| 1-10 | fully modded gear, 2 enhancements | 856 | 4.7 | 109518 | 2571 | 8.7 | 132811 | 1.21 | - | Passed |
| 1-10 | fully modded gear, 3 enhancements | 830 | 5.0 | 99336 | 2579 | 8.6 | 135359 | 1.36 | - | Passed |
| 11-20 | no gear | 564 | 8.7 | 39031 | 1292 | 12.9 | 45103 | 1.16 | - | Passed |
| 11-20 | no gear, 1 enhancement | 304 | 8.9 | 20379 | 1257 | 16.0 | 35329 | 1.73 | - | Passed |
| 11-20 | no gear, 2 enhancements | 90 | 6.5 | 8308 | 1207 | 16.4 | 33199 | 4.00 | - | Passed |
| 11-20 | no gear, 3 enhancements | 90 | 6.5 | 8308 | 1173 | 16.7 | 31533 | 3.80 | - | Passed |
| 11-20 | mid gear | 661 | 10.8 | 36706 | 1347 | 11.5 | 52802 | 1.44 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 621 | 11.8 | 31594 | 1329 | 14.3 | 41835 | 1.32 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 601 | 13.2 | 27274 | 1281 | 15.8 | 36454 | 1.34 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 579 | 13.6 | 25520 | 1321 | 14.5 | 41081 | 1.61 | - | Passed |
| 11-20 | best gear | 782 | 14.4 | 32501 | 1361 | 9.8 | 62160 | 1.91 | - | Passed |
| 11-20 | best gear, 1 enhancement | 766 | 15.1 | 30378 | 1350 | 12.9 | 47258 | 1.56 | - | Passed |
| 11-20 | best gear, 2 enhancements | 750 | 16.4 | 27508 | 1317 | 13.1 | 45332 | 1.65 | - | Passed |
| 11-20 | best gear, 3 enhancements | 731 | 15.8 | 27734 | 1335 | 14.3 | 42147 | 1.52 | - | Passed |
| 11-20 | fully modded gear | 1169 | 13.7 | 51313 | 1372 | 8.4 | 73278 | 1.43 | - | Passed |
| 11-20 | fully modded gear, 1 enhancement | 1139 | 13.8 | 49523 | 1380 | 10.3 | 60426 | 1.22 | - | Passed |
| 11-20 | fully modded gear, 2 enhancements | 1154 | 14.5 | 47757 | 1375 | 10.7 | 57707 | 1.21 | - | Passed |
| 11-20 | fully modded gear, 3 enhancements | 1162 | 14.3 | 48582 | 1373 | 10.9 | 56562 | 1.16 | - | Passed |
| 21-30 | no gear | 633 | 10.5 | 36274 | 1226 | 13.6 | 40673 | 1.12 | - | Passed |
| 21-30 | no gear, 1 enhancement | 613 | 10.6 | 34849 | 1124 | 14.6 | 34644 | 0.99 | 1.01x | Failed |
| 21-30 | no gear, 2 enhancements | 621 | 11.6 | 32271 | 1117 | 14.5 | 34566 | 1.07 | - | Passed |
| 21-30 | no gear, 3 enhancements | 570 | 12.4 | 27559 | 1094 | 14.9 | 33023 | 1.20 | - | Passed |
| 21-30 | mid gear | 686 | 16.1 | 25528 | 1242 | 12.5 | 44610 | 1.75 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 668 | 15.2 | 26317 | 1256 | 13.9 | 40779 | 1.55 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 653 | 15.6 | 25201 | 1233 | 14.4 | 38504 | 1.53 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 653 | 16.2 | 24194 | 1181 | 14.4 | 36902 | 1.53 | - | Passed |
| 21-30 | best gear | 781 | 21.6 | 21681 | 1304 | 11.4 | 51656 | 2.38 | - | Passed |
| 21-30 | best gear, 1 enhancement | 750 | 20.4 | 22054 | 1280 | 12.3 | 46922 | 2.13 | - | Passed |
| 21-30 | best gear, 2 enhancements | 751 | 20.7 | 21725 | 1292 | 13.3 | 43836 | 2.02 | - | Passed |
| 21-30 | best gear, 3 enhancements | 725 | 20.8 | 20881 | 1240 | 14.0 | 39767 | 1.90 | - | Passed |
| 21-30 | fully modded gear | 1155 | 20.9 | 33077 | 1318 | 8.9 | 66622 | 2.01 | - | Passed |
| 21-30 | fully modded gear, 1 enhancement | 1111 | 19.9 | 33505 | 1309 | 10.3 | 57218 | 1.71 | - | Passed |
| 21-30 | fully modded gear, 2 enhancements | 1108 | 20.0 | 33299 | 1309 | 10.1 | 58524 | 1.76 | - | Passed |
| 21-30 | fully modded gear, 3 enhancements | 1105 | 20.1 | 33033 | 1307 | 11.1 | 53217 | 1.61 | - | Passed |
| 31-40 | no gear | 637 | 12.7 | 30125 | 1358 | 14.6 | 41980 | 1.39 | - | Passed |
| 31-40 | no gear, 1 enhancement | 626 | 14.1 | 26707 | 1269 | 16.0 | 35785 | 1.34 | - | Passed |
| 31-40 | no gear, 2 enhancements | 594 | 14.2 | 25103 | 1181 | 16.5 | 32128 | 1.28 | - | Passed |
| 31-40 | no gear, 3 enhancements | 579 | 15.2 | 22871 | 1188 | 17.0 | 31469 | 1.38 | - | Passed |
| 31-40 | mid gear | 666 | 17.5 | 22794 | 1395 | 13.1 | 47797 | 2.10 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 659 | 18.0 | 22009 | 1330 | 15.4 | 38752 | 1.76 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 657 | 18.0 | 21902 | 1326 | 15.3 | 38899 | 1.78 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 636 | 18.0 | 21155 | 1231 | 16.1 | 34405 | 1.63 | - | Passed |
| 31-40 | best gear | 747 | 24.1 | 18569 | 1408 | 12.2 | 51824 | 2.79 | - | Passed |
| 31-40 | best gear, 1 enhancement | 732 | 24.8 | 17689 | 1383 | 12.9 | 48120 | 2.72 | - | Passed |
| 31-40 | best gear, 2 enhancements | 730 | 25.8 | 17000 | 1370 | 14.2 | 43285 | 2.55 | - | Passed |
| 31-40 | best gear, 3 enhancements | 707 | 24.3 | 17478 | 1352 | 14.7 | 41457 | 2.37 | - | Passed |
| 31-40 | fully modded gear | 1086 | 25.4 | 25611 | 1433 | 10.4 | 61703 | 2.41 | - | Passed |
| 31-40 | fully modded gear, 1 enhancement | 1093 | 25.8 | 25408 | 1433 | 10.7 | 60070 | 2.36 | - | Passed |
| 31-40 | fully modded gear, 2 enhancements | 1092 | 26.5 | 24713 | 1422 | 12.2 | 52465 | 2.12 | - | Passed |
| 31-40 | fully modded gear, 3 enhancements | 1067 | 26.5 | 24136 | 1436 | 11.4 | 56580 | 2.34 | - | Passed |
| 41-53 | no gear | 675 | 16.3 | 24807 | 1246 | 18.3 | 30676 | 1.24 | - | Passed |
| 41-53 | no gear, 1 enhancement | 597 | 14.9 | 23989 | 1101 | 18.5 | 26730 | 1.11 | - | Passed |
| 41-53 | no gear, 2 enhancements | 555 | 15.2 | 21993 | 1029 | 19.2 | 24153 | 1.10 | - | Passed |
| 41-53 | no gear, 3 enhancements | 502 | 15.3 | 19685 | 961 | 19.4 | 22341 | 1.13 | - | Passed |
| 41-53 | mid gear | 810 | 23.1 | 21016 | 1283 | 17.5 | 32956 | 1.57 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 745 | 21.5 | 20764 | 1199 | 18.4 | 29392 | 1.42 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 688 | 19.6 | 21074 | 1104 | 18.5 | 26810 | 1.27 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 607 | 17.8 | 20439 | 1025 | 19.4 | 23786 | 1.16 | - | Passed |
| 41-53 | best gear | 1008 | 27.8 | 21759 | 1313 | 16.0 | 36933 | 1.70 | - | Passed |
| 41-53 | best gear, 1 enhancement | 974 | 28.2 | 20741 | 1224 | 16.7 | 32898 | 1.59 | - | Passed |
| 41-53 | best gear, 2 enhancements | 857 | 25.4 | 20238 | 1173 | 17.7 | 29832 | 1.47 | - | Passed |
| 41-53 | best gear, 3 enhancements | 765 | 22.6 | 20327 | 1094 | 18.6 | 26391 | 1.30 | - | Passed |
| 41-53 | fully modded gear | 1167 | 27.8 | 25181 | 1334 | 12.6 | 47793 | 1.90 | - | Passed |
| 41-53 | fully modded gear, 1 enhancement | 1133 | 27.4 | 24831 | 1322 | 13.6 | 43791 | 1.76 | - | Passed |
| 41-53 | fully modded gear, 2 enhancements | 1174 | 29.0 | 24296 | 1300 | 14.5 | 40320 | 1.66 | - | Passed |
| 41-53 | fully modded gear, 3 enhancements | 1134 | 29.2 | 23284 | 1294 | 14.9 | 39001 | 1.67 | - | Passed |

## Enhancement parity

### Rank 50, fully modded gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 100%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 4 | 60 | 32% | 99% | 100% | 0.98 | 100% | 99.6% | 0.99 (+/- 0.01) | 1.01 | >= 0.95 | Passed |
| 41-45 | 2 | 4 | 60 | 63% | 98% | 99% | 1.93 | 100% | 99.4% | 1.98 (+/- 0.01) | 1.03 | >= 0.95 | Passed |
| 41-45 | 3 | 4 | 60 | 95% | 99% | 99% | 2.94 | 100% | 99.6% | 2.96 (+/- 0.02) | 1.01 | >= 0.90 | Passed |
| 46-50 | 1 | 4 | 60 | 32% | 98% | 100% | 0.97 | 100% | 99.8% | 1.00 (+/- 0.00) | 1.03 | >= 0.95 | Passed |
| 46-50 | 2 | 4 | 60 | 63% | 99% | 100% | 1.97 | 100% | 99.2% | 1.98 (+/- 0.01) | 1.01 | >= 0.95 | Passed |
| 46-50 | 3 | 4 | 60 | 95% | 99% | 99% | 2.97 | 100% | 99.3% | 2.87 (+/- 0.03) | 0.97 | >= 0.90 | Passed |
| 51-53 | 1 | 4 | 60 | 8% | 100% | 40% | 0.40 | 100% | 98.3% | 0.98 (+/- 0.01) | 2.46 | >= 0.95 | Passed |
| 51-53 | 2 | 4 | 60 | 16% | 100% | 49% | 0.97 | 99% | 98.3% | 1.94 (+/- 0.02) | 2.01 | >= 0.95 | Passed |
| 51-53 | 3 | 4 | 60 | 24% | 98% | 37% | 1.08 | 100% | 98.8% | 2.86 (+/- 0.03) | 2.66 | >= 0.90 | Passed |

### Thresholds, first / second / third slot, that would meet parity for the quality measured (the strictest set that keeps every case at least at today's, never falling from one slot to the next)

| Scope | Thresholds that meet parity | In steps of 5 | Thresholds in force |
| --- | --- | --- | --- |
| Every band together (the shared thresholds) | 76% / 97% / 97% | 75% / 95% / 95% | 50% / 85% / 100% |
| Band 41-45 alone | 99% / 99% / 100% | 95% / 95% / 100% | 50% / 85% / 100% |
| Band 46-50 alone | 76% / 97% / 97% | 75% / 95% / 95% | 50% / 85% / 100% |
| Band 51-53 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 100% |

### Rank 50, fully modded gear: quality of the items the new minigame made, in whole percent, by band and slots filled

| Band | Slots filled | Items made | 10th percentile | 25th percentile | Median | 75th percentile | 90th percentile | Made at 100% |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 41-45 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 41-45 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 46-50 | 1 | 100% | 99% | 100% | 100% | 100% | 100% | 90% |
| 46-50 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 91% |
| 46-50 | 3 | 100% | 99% | 100% | 100% | 100% | 100% | 89% |
| 51-53 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 92% |
| 51-53 | 2 | 99% | 99% | 100% | 100% | 100% | 100% | 88% |
| 51-53 | 3 | 100% | 99% | 100% | 100% | 100% | 100% | 90% |

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
