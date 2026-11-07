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
  - fully modded gear (Control 119, Craftsmanship 120, CP 37)
- Recipes: 1-10: 4 of 58; 11-20: 4 of 59; 21-30: 4 of 65; 31-40: 4 of 61; 41-53: 4 of 88 recipes played.
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
| XP rate | Passed | Item crafts: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.04x in 21-30 / no gear, 1 enhancement). |
| Enhancement parity | Passed | At rank 50 in fully modded gear, the thresholds in force (50% / 85% / 100%) transfer at least 0.90 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 0.97 times today's). Thresholds that meet parity in every band for this quality: 69% / 99% / 99%. |

## Economy parity

### Component stacks consumed per item made, failures included, over the recipes the legacy policy makes at least once in the cell (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Parity rule | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 4 | 1.81 | 1.82 | 0.99 | 99% | 97% | 94% | 1% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | mid gear | 4 | 1.80 | 1.81 | 0.99 | 100% | 98% | 98% | 7% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | best gear | 4 | 1.80 | 1.81 | 1.00 | 99% | 99% | 99% | 16% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | fully modded gear | 4 | 1.80 | 1.81 | 0.99 | 100% | 98% | 100% | 51% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | no gear | 4 | 2.05 | 2.11 | 0.97 | 96% | 92% | 90% | 1% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | mid gear | 4 | 2.00 | 2.02 | 0.99 | 100% | 99% | 95% | 11% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | best gear | 4 | 2.01 | 2.00 | 1.00 | 99% | 100% | 98% | 30% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | fully modded gear | 4 | 2.00 | 2.01 | 1.00 | 100% | 99% | 99% | 95% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | no gear | 4 | 1.67 | 1.62 | 1.03 | 96% | 97% | 85% | 7% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | mid gear | 4 | 1.62 | 1.61 | 1.00 | 99% | 98% | 92% | 15% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | best gear | 4 | 1.60 | 1.60 | 1.00 | 99% | 99% | 98% | 30% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | fully modded gear | 4 | 1.60 | 1.60 | 1.00 | 100% | 99% | 99% | 91% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | no gear | 4 | 2.30 | 2.29 | 1.00 | 97% | 98% | 91% | 8% | yes | 0% / 0% | within 10% | Reported |
| 31-40 | mid gear | 4 | 2.25 | 2.30 | 0.98 | 100% | 97% | 97% | 14% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | best gear | 4 | 2.26 | 2.26 | 1.00 | 99% | 99% | 96% | 25% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | fully modded gear | 4 | 2.25 | 2.26 | 1.00 | 100% | 99% | 100% | 82% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | no gear | 4 | 1.62 | 1.62 | 1.00 | 98% | 99% | 86% | 43% | yes | 0% / 25% | within 10% | Reported |
| 41-53 | mid gear | 4 | 1.83 | 1.96 | 0.94 | 100% | 79% | 95% | 55% | yes | 0% / 0% | at most +10% | Reported |
| 41-53 | best gear | 4 | 1.86 | 1.88 | 0.99 | 99% | 92% | 97% | 76% | yes | 0% / 0% | within 10% | Passed |
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
| 1-10 | no gear, 1 enhancement | 498 | 6.1 | 48684 | 2436 | 13.2 | 82967 | 1.70 | - | Passed |
| 1-10 | no gear, 2 enhancements | 176 | 6.0 | 17606 | 2437 | 12.9 | 84902 | 4.82 | - | Passed |
| 1-10 | no gear, 3 enhancements | 90 | 6.0 | 9000 | 2404 | 13.6 | 79533 | 8.84 | - | Passed |
| 1-10 | mid gear | 644 | 6.0 | 64781 | 2557 | 11.4 | 100915 | 1.56 | - | Passed |
| 1-10 | mid gear, 1 enhancement | 602 | 5.9 | 61514 | 2540 | 12.4 | 92515 | 1.50 | - | Passed |
| 1-10 | mid gear, 2 enhancements | 587 | 6.6 | 53030 | 2499 | 12.3 | 91456 | 1.72 | - | Passed |
| 1-10 | mid gear, 3 enhancements | 519 | 7.5 | 41440 | 2516 | 11.8 | 96119 | 2.32 | - | Passed |
| 1-10 | best gear | 710 | 6.5 | 65640 | 2561 | 10.4 | 110951 | 1.69 | - | Passed |
| 1-10 | best gear, 1 enhancement | 668 | 6.3 | 63621 | 2565 | 10.8 | 106389 | 1.67 | - | Passed |
| 1-10 | best gear, 2 enhancements | 667 | 6.8 | 58475 | 2540 | 11.0 | 104079 | 1.78 | - | Passed |
| 1-10 | best gear, 3 enhancements | 646 | 6.8 | 56713 | 2568 | 10.3 | 112071 | 1.98 | - | Passed |
| 1-10 | fully modded gear | 976 | 5.5 | 105745 | 2579 | 8.2 | 141852 | 1.34 | - | Passed |
| 1-10 | fully modded gear, 1 enhancement | 844 | 4.4 | 113911 | 2579 | 8.1 | 143727 | 1.26 | - | Passed |
| 1-10 | fully modded gear, 2 enhancements | 856 | 4.7 | 109518 | 2573 | 8.5 | 136116 | 1.24 | - | Passed |
| 1-10 | fully modded gear, 3 enhancements | 830 | 5.0 | 99336 | 2578 | 8.2 | 141147 | 1.42 | - | Passed |
| 11-20 | no gear | 564 | 8.7 | 39031 | 1292 | 12.9 | 45103 | 1.16 | - | Passed |
| 11-20 | no gear, 1 enhancement | 304 | 8.9 | 20379 | 1255 | 16.0 | 35257 | 1.73 | - | Passed |
| 11-20 | no gear, 2 enhancements | 90 | 6.5 | 8308 | 1209 | 16.3 | 33466 | 4.03 | - | Passed |
| 11-20 | no gear, 3 enhancements | 90 | 6.5 | 8308 | 1185 | 16.7 | 31890 | 3.84 | - | Passed |
| 11-20 | mid gear | 661 | 10.8 | 36706 | 1345 | 10.8 | 56306 | 1.53 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 621 | 11.8 | 31594 | 1336 | 14.0 | 42894 | 1.36 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 601 | 13.2 | 27274 | 1320 | 15.1 | 39386 | 1.44 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 579 | 13.6 | 25520 | 1311 | 14.9 | 39687 | 1.56 | - | Passed |
| 11-20 | best gear | 782 | 14.4 | 32501 | 1365 | 9.8 | 62668 | 1.93 | - | Passed |
| 11-20 | best gear, 1 enhancement | 766 | 15.1 | 30378 | 1366 | 12.5 | 49216 | 1.62 | - | Passed |
| 11-20 | best gear, 2 enhancements | 750 | 16.4 | 27508 | 1345 | 13.1 | 46131 | 1.68 | - | Passed |
| 11-20 | best gear, 3 enhancements | 731 | 15.8 | 27734 | 1357 | 13.3 | 46044 | 1.66 | - | Passed |
| 11-20 | fully modded gear | 1169 | 13.7 | 51313 | 1375 | 7.6 | 81270 | 1.58 | - | Passed |
| 11-20 | fully modded gear, 1 enhancement | 1139 | 13.8 | 49523 | 1370 | 10.6 | 58165 | 1.17 | - | Passed |
| 11-20 | fully modded gear, 2 enhancements | 1154 | 14.5 | 47757 | 1372 | 10.3 | 59926 | 1.25 | - | Passed |
| 11-20 | fully modded gear, 3 enhancements | 1162 | 14.3 | 48582 | 1374 | 9.6 | 64737 | 1.33 | - | Passed |
| 21-30 | no gear | 633 | 10.5 | 36274 | 1248 | 13.6 | 41411 | 1.14 | - | Passed |
| 21-30 | no gear, 1 enhancement | 613 | 10.6 | 34849 | 1169 | 14.6 | 36162 | 1.04 | - | Passed |
| 21-30 | no gear, 2 enhancements | 621 | 11.6 | 32271 | 1142 | 14.6 | 35135 | 1.09 | - | Passed |
| 21-30 | no gear, 3 enhancements | 570 | 12.4 | 27559 | 1165 | 14.1 | 37282 | 1.35 | - | Passed |
| 21-30 | mid gear | 686 | 16.1 | 25528 | 1302 | 12.0 | 48815 | 1.91 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 668 | 15.2 | 26317 | 1291 | 13.7 | 42377 | 1.61 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 653 | 15.6 | 25201 | 1264 | 14.1 | 40355 | 1.60 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 653 | 16.2 | 24194 | 1182 | 14.3 | 37164 | 1.54 | - | Passed |
| 21-30 | best gear | 781 | 21.6 | 21681 | 1327 | 11.3 | 52898 | 2.44 | - | Passed |
| 21-30 | best gear, 1 enhancement | 750 | 20.4 | 22054 | 1310 | 12.0 | 49052 | 2.22 | - | Passed |
| 21-30 | best gear, 2 enhancements | 751 | 20.7 | 21725 | 1316 | 12.4 | 47723 | 2.20 | - | Passed |
| 21-30 | best gear, 3 enhancements | 725 | 20.8 | 20881 | 1290 | 13.2 | 43818 | 2.10 | - | Passed |
| 21-30 | fully modded gear | 1155 | 20.9 | 33077 | 1337 | 8.8 | 68176 | 2.06 | - | Passed |
| 21-30 | fully modded gear, 1 enhancement | 1111 | 19.9 | 33505 | 1337 | 9.8 | 61518 | 1.84 | - | Passed |
| 21-30 | fully modded gear, 2 enhancements | 1108 | 20.0 | 33299 | 1333 | 9.7 | 61541 | 1.85 | - | Passed |
| 21-30 | fully modded gear, 3 enhancements | 1105 | 20.1 | 33033 | 1334 | 10.1 | 59677 | 1.81 | - | Passed |
| 31-40 | no gear | 637 | 12.7 | 30125 | 1358 | 14.6 | 41980 | 1.39 | - | Passed |
| 31-40 | no gear, 1 enhancement | 626 | 14.1 | 26707 | 1295 | 15.7 | 37193 | 1.39 | - | Passed |
| 31-40 | no gear, 2 enhancements | 594 | 14.2 | 25103 | 1234 | 15.7 | 35362 | 1.41 | - | Passed |
| 31-40 | no gear, 3 enhancements | 579 | 15.2 | 22871 | 1225 | 16.4 | 33705 | 1.47 | - | Passed |
| 31-40 | mid gear | 666 | 17.5 | 22794 | 1418 | 12.4 | 51420 | 2.26 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 659 | 18.0 | 22009 | 1357 | 15.2 | 40107 | 1.82 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 657 | 18.0 | 21902 | 1361 | 15.4 | 39826 | 1.82 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 636 | 18.0 | 21155 | 1329 | 15.5 | 38651 | 1.83 | - | Passed |
| 31-40 | best gear | 747 | 24.1 | 18569 | 1408 | 12.2 | 51745 | 2.79 | - | Passed |
| 31-40 | best gear, 1 enhancement | 732 | 24.8 | 17689 | 1387 | 12.8 | 48918 | 2.77 | - | Passed |
| 31-40 | best gear, 2 enhancements | 730 | 25.8 | 17000 | 1388 | 13.6 | 45996 | 2.71 | - | Passed |
| 31-40 | best gear, 3 enhancements | 707 | 24.3 | 17478 | 1400 | 14.0 | 45021 | 2.58 | - | Passed |
| 31-40 | fully modded gear | 1086 | 25.4 | 25611 | 1437 | 9.6 | 67480 | 2.63 | - | Passed |
| 31-40 | fully modded gear, 1 enhancement | 1093 | 25.8 | 25408 | 1427 | 10.7 | 59899 | 2.36 | - | Passed |
| 31-40 | fully modded gear, 2 enhancements | 1092 | 26.5 | 24713 | 1434 | 10.6 | 61046 | 2.47 | - | Passed |
| 31-40 | fully modded gear, 3 enhancements | 1067 | 26.5 | 24136 | 1434 | 10.1 | 63718 | 2.64 | - | Passed |
| 41-53 | no gear | 675 | 16.3 | 24807 | 1246 | 18.3 | 30676 | 1.24 | - | Passed |
| 41-53 | no gear, 1 enhancement | 597 | 14.9 | 23989 | 1136 | 18.3 | 27889 | 1.16 | - | Passed |
| 41-53 | no gear, 2 enhancements | 555 | 15.2 | 21993 | 1137 | 18.6 | 27455 | 1.25 | - | Passed |
| 41-53 | no gear, 3 enhancements | 502 | 15.3 | 19685 | 1026 | 19.6 | 23515 | 1.19 | - | Passed |
| 41-53 | mid gear | 810 | 23.1 | 21016 | 1298 | 17.3 | 33774 | 1.61 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 745 | 21.5 | 20764 | 1212 | 17.9 | 30397 | 1.46 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 688 | 19.6 | 21074 | 1158 | 18.6 | 28016 | 1.33 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 607 | 17.8 | 20439 | 1168 | 18.7 | 28042 | 1.37 | - | Passed |
| 41-53 | best gear | 1008 | 27.8 | 21759 | 1313 | 15.8 | 37275 | 1.71 | - | Passed |
| 41-53 | best gear, 1 enhancement | 974 | 28.2 | 20741 | 1267 | 16.4 | 34753 | 1.68 | - | Passed |
| 41-53 | best gear, 2 enhancements | 857 | 25.4 | 20238 | 1240 | 17.1 | 32663 | 1.61 | - | Passed |
| 41-53 | best gear, 3 enhancements | 765 | 22.6 | 20327 | 1203 | 17.6 | 30790 | 1.51 | - | Passed |
| 41-53 | fully modded gear | 1167 | 27.8 | 25181 | 1337 | 11.5 | 52222 | 2.07 | - | Passed |
| 41-53 | fully modded gear, 1 enhancement | 1133 | 27.4 | 24831 | 1326 | 13.1 | 45403 | 1.83 | - | Passed |
| 41-53 | fully modded gear, 2 enhancements | 1174 | 29.0 | 24296 | 1319 | 13.6 | 43652 | 1.80 | - | Passed |
| 41-53 | fully modded gear, 3 enhancements | 1134 | 29.2 | 23284 | 1309 | 13.6 | 43370 | 1.86 | - | Passed |

## Enhancement parity

### Rank 50, fully modded gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 100%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 4 | 60 | 32% | 99% | 100% | 0.98 | 100% | 100.0% | 1.00 (+/- 0.00) | 1.02 | >= 0.95 | Passed |
| 41-45 | 2 | 4 | 60 | 63% | 98% | 99% | 1.93 | 100% | 99.8% | 1.99 (+/- 0.01) | 1.03 | >= 0.95 | Passed |
| 41-45 | 3 | 4 | 60 | 95% | 99% | 99% | 2.94 | 100% | 99.9% | 2.96 (+/- 0.02) | 1.00 | >= 0.90 | Passed |
| 46-50 | 1 | 4 | 60 | 32% | 98% | 100% | 0.97 | 100% | 99.6% | 1.00 (+/- 0.00) | 1.03 | >= 0.95 | Passed |
| 46-50 | 2 | 4 | 60 | 63% | 99% | 100% | 1.97 | 100% | 99.7% | 1.99 (+/- 0.01) | 1.01 | >= 0.95 | Passed |
| 46-50 | 3 | 4 | 60 | 95% | 99% | 99% | 2.97 | 100% | 99.4% | 2.88 (+/- 0.03) | 0.97 | >= 0.90 | Passed |
| 51-53 | 1 | 4 | 60 | 8% | 100% | 40% | 0.40 | 100% | 99.8% | 1.00 (+/- 0.00) | 2.50 | >= 0.95 | Passed |
| 51-53 | 2 | 4 | 60 | 16% | 100% | 49% | 0.97 | 100% | 99.5% | 1.99 (+/- 0.01) | 2.06 | >= 0.95 | Passed |
| 51-53 | 3 | 4 | 60 | 24% | 98% | 37% | 1.08 | 100% | 99.5% | 2.90 (+/- 0.03) | 2.69 | >= 0.90 | Passed |

### Thresholds, first / second / third slot, that would meet parity for the quality measured (the strictest set that keeps every case at least at today's, never falling from one slot to the next)

| Scope | Thresholds that meet parity | In steps of 5 | Thresholds in force |
| --- | --- | --- | --- |
| Every band together (the shared thresholds) | 69% / 99% / 99% | 65% / 95% / 95% | 50% / 85% / 100% |
| Band 41-45 alone | 99% / 99% / 99% | 95% / 95% / 100% | 50% / 85% / 100% |
| Band 46-50 alone | 69% / 99% / 99% | 65% / 95% / 95% | 50% / 85% / 100% |
| Band 51-53 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 100% |

### Rank 50, fully modded gear: quality of the items the new minigame made, in whole percent, by band and slots filled

| Band | Slots filled | Items made | 10th percentile | 25th percentile | Median | 75th percentile | 90th percentile | Made at 100% |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 41-45 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 99% |
| 41-45 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 96% |
| 46-50 | 1 | 100% | 99% | 100% | 100% | 100% | 100% | 89% |
| 46-50 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 92% |
| 46-50 | 3 | 100% | 99% | 100% | 100% | 100% | 100% | 90% |
| 51-53 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 96% |
| 51-53 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 94% |
| 51-53 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 91% |

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
