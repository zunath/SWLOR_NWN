# Crafting revamp: balance gates of the Fabricator minigame

- Command: `SWLOR.CraftSim gates --scenario fabricator --gates rotation,styles,perks,length,materials,xp,enhancements,ladder --runs 40 --seed 1 --recipes-per-band 6 --repo . --out design/testing/crafting-revamp/fabricator-gates.md`
- Seed 1; 40 runs per case.

## Inputs

- Scenario: Fabricator (`fabricator`), Fabrication recipes, minigame Fabricator.
- Quality: the quality meter's share of its maximum when the item is made, in points (0-100). A broken or abandoned piece counts 0, so every average includes the crafts that failed.
- Crafter: the rank the scenario's cohort gives each recipe (by default the recipe's level, at most 50, so a crafter works at their own level), with no food or status bonuses and no perks unless a gate names a style or the scenario's limitations say it plays a perk loadout. Gear levels are the plan's none, middle and best tier of the skill's crafting gear:
  - no gear (Control 0, Craftsmanship 0, CP 0)
  - mid gear (Control 13, Craftsmanship 12, CP 26)
  - best gear (Control 29, Craftsmanship 30, CP 37)
  - fully modded gear (Control 119, Craftsmanship 120, CP 37)
- Recipes: 1-10: 6 of 66; 11-20: 6 of 84; 21-30: 6 of 109; 31-40: 6 of 102; 41-53: 6 of 96 recipes played.
- Adaptive player: depth 3, forecast samples per result 2, budget 4000 engine copies per decision, 0.5% of value lost per extra action, heuristic FabricatorLeafHeuristic. Each attempt's dice come from its own stream of the seed.
- Fixed-rotation solver: beam 8, cycles up to 3, 8 adaptive lines as seeds, up to 40 actions, 64 training attempts per local-search round and 150 selection attempts for each of 12 finalists, local search from the best 3 candidate(s) for up to 3 round(s) each. Gate 1 searches one sequence for each band and gear tier across the band's sampled recipes (every recipe weighing the same; an action a recipe's engine cannot take is skipped there), and also each recipe's own best sequence as a reported metric. The winner is chosen on separate attempts and then scored on a third set (hold-out) with 40 attempts per recipe.
- Legacy comparator (gates 5 and 6): the old minigame's lookahead player (`SWLOR.CraftSim baseline`), replayed here with the same seed on the same recipes, ranks and gear.
- Time: 8.0 s per action in the new window and 6.0 s in the old one, plus 0.0 s per craft in both. These are estimates; the client evidence replaces them.
- XP multiplier on the base XP of every craft, failures included, as the game server applies it (`CraftMinigameXp`): 1-10 2.40x, 11-20 1.50x, 21-30 1.25x, 31-40 1.10x, 41-53 1.06x. The new side's XP below includes it; the legacy loop's does not.
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
- Styles compared: Production Run vs Fine Tolerance (from rank 20); Production Run (full line) vs Fine Tolerance (full line) (from rank 20).

## Verdicts

| Gate | Verdict | Summary |
| --- | --- | --- |
| No fixed rotation | Failed | One fixed rotation comes within 10 points of adaptive play in 5 case(s): 1-10 / no gear; 31-40 / no gear; 41-53 / no gear; 41-53 / mid gear; 41-53 / best gear (smallest margin -34.0 in 41-53 / mid gear). The best rotation for each recipe alone, a reported metric, is -29.3 to 0.0 points behind adaptive play. |
| Style balance | Failed | 1 style comparison(s) are out of balance: Production Run vs Fine Tolerance / no gear. |
| Perks optional | Passed | An unperked rank-50 crafter in fully modded gear reaches maximum quality on at least 50% of crafts in every at-level band (lowest 57%). |
| Length | Passed | Every band and gear tier takes 10-20 actions (item craft at level) on average. |
| Economy parity | Passed | Component stacks per item are within 10% of the legacy baseline where its success rate is at least 90%, and at most 10% above it where it is lower, at no less quality, in every band and gear tier. |
| XP rate | Passed | Item crafts: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.01x in 41-53 / best gear, 2 enhancements). |
| Enhancement parity | Passed | At rank 50 in fully modded gear, the thresholds in force (50% / 85% / 100%) transfer at least 0.90 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 0.97 times today's). Thresholds that meet parity in every band for this quality: 76% / 97% / 98%. |
| Gear ladder | Passed | Every gear step raises quality, fully modded gear makes the top recipes' target, and perks do not replace gear. |

## No fixed rotation

### Average quality: adaptive play against the best single fixed rotation of each band and gear tier (quality points; a broken piece counts 0)

| Band | Gear | Recipes played | Adaptive avg quality | Best single rotation avg quality | Margin | Required | Best rotation per recipe avg quality (reported) | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 81.4 | 86.7 | -5.3 | >= 10 | 86.7 | Failed |
| 1-10 | mid gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Reported |
| 1-10 | best gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Reported |
| 1-10 | fully modded gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Reported |
| 11-20 | no gear | 6 | 98.1 | 100.0 | -1.9 | >= 10 | 100.0 | Reported |
| 11-20 | mid gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Reported |
| 11-20 | best gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Reported |
| 11-20 | fully modded gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Reported |
| 21-30 | no gear | 6 | 99.8 | 100.0 | -0.2 | >= 10 | 100.0 | Reported |
| 21-30 | mid gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Reported |
| 21-30 | best gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Reported |
| 21-30 | fully modded gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Reported |
| 31-40 | no gear | 6 | 95.1 | 100.0 | -4.9 | >= 10 | 100.0 | Failed |
| 31-40 | mid gear | 6 | 98.7 | 100.0 | -1.3 | >= 10 | 100.0 | Reported |
| 31-40 | best gear | 6 | 99.2 | 100.0 | -0.8 | >= 10 | 100.0 | Reported |
| 31-40 | fully modded gear | 6 | 99.6 | 100.0 | -0.4 | >= 10 | 100.0 | Reported |
| 41-53 | no gear | 6 | 30.7 | 52.4 | -21.7 | >= 10 | 56.0 | Failed |
| 41-53 | mid gear | 6 | 65.3 | 99.3 | -34.0 | >= 10 | 94.7 | Failed |
| 41-53 | best gear | 6 | 93.3 | 95.7 | -2.4 | >= 10 | 100.0 | Failed |
| 41-53 | fully modded gear | 6 | 99.7 | 100.0 | -0.3 | >= 10 | 100.0 | Reported |

### The single rotation found for each band and gear tier, searched across the band's sampled recipes (average quality in points)

| Band | Gear | Best single rotation for the band | Training | Selection | Hold-out | Adaptive on these recipes |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | run_fittings > run_shell x2 > run_frame > precision_pass x2 > run_frame > run_shell > run_frame > run_shell > precision_pass > retool_extruder > run_frame > precision_pass > retool_extruder > precision_pass > run_frame > precision_pass > retool_bonder > precision_pass x2 > run_shell x2 > precision_pass > retool_cutter > precision_pass x2 > run_frame > run_shell > run_fittings > retool_cutter > precision_pass > assemble | 86.7 | 86.7 | 86.7 | 81.4 |
| 1-10 | mid gear | run_frame > run_fittings > service > run_frame > run_shell > retool_cutter > precision_pass > retool_extruder > run_shell > run_fittings > run_frame > run_fittings > precision_pass x2 > retool_bonder > precision_pass x2 > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | run_shell > retool_cutter > run_frame > precision_pass > retool_extruder > run_shell > precision_pass x2 > retool_cutter > run_frame > run_fittings > precision_pass > retool_bonder > run_fittings > run_shell > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | retool_cutter > run_fittings > run_frame > run_shell > precision_pass > run_fittings > run_shell > retool_extruder > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 11-20 | no gear | run_frame > run_shell x2 > retool_cutter > run_fittings > precision_pass x2 > run_frame > run_fittings > precision_pass > retool_bonder > precision_pass > retool_bonder > precision_pass > run_shell > precision_pass > retool_extruder > precision_pass x3 > run_shell > run_fittings > run_frame > run_fittings > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 98.1 |
| 11-20 | mid gear | run_shell > run_frame > precision_pass x2 > retool_bonder > run_fittings > precision_pass x2 > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_frame > run_shell > run_fittings > run_frame > run_fittings > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | run_shell > run_frame > precision_pass x2 > retool_bonder > run_fittings > precision_pass x2 > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_shell > run_frame > run_fittings > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | run_shell > run_frame > precision_pass > retool_bonder > run_fittings > precision_pass > retool_cutter > precision_pass > retool_extruder > precision_pass > run_shell > run_frame > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | run_shell > precision_pass > run_fittings > run_shell > run_fittings > run_shell > precision_pass > retool_cutter > run_shell > run_frame > run_shell x2 > precision_pass > run_fittings x2 > precision_pass > run_frame > precision_pass > retool_bonder > precision_pass > retool_extruder > run_shell > precision_pass x2 > run_frame x2 > precision_pass > run_frame > run_fittings > run_frame > run_shell > retool_bonder > precision_pass > service > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 99.8 |
| 21-30 | mid gear | retool_bonder > run_fittings > run_shell > precision_pass > run_frame > precision_pass x2 > run_shell > run_frame > run_fittings > run_frame > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_shell > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | run_frame > run_fittings > run_shell > retool_bonder > precision_pass > retool_cutter > precision_pass > retool_extruder > run_frame > run_shell > run_fittings > precision_pass x2 > retool_bonder > precision_pass > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | retool_bonder > run_shell > run_fittings > precision_pass > retool_cutter > run_frame > precision_pass > retool_extruder > precision_pass > run_shell > assemble | 100.0 | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | run_frame x3 > run_shell > precision_pass > run_shell > precision_pass x2 > run_fittings > retool_extruder > precision_pass > service > precision_pass > run_shell > precision_pass > run_frame > retool_cutter > precision_pass > retool_cutter > precision_pass x2 > run_frame > service > retool_bonder > precision_pass x3 > run_shell > run_fittings > run_shell > assemble | 100.0 | 100.0 | 100.0 | 95.1 |
| 31-40 | mid gear | run_fittings > precision_pass > run_frame > run_shell > retool_extruder > precision_pass > run_fittings > service > retool_cutter > run_fittings > run_frame > retool_cutter > precision_pass x2 > retool_extruder > retool_bonder > precision_pass > run_frame > precision_pass > run_frame > retool_extruder > run_shell > service > precision_pass x2 > run_frame > retool_bonder > precision_pass > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 98.7 |
| 31-40 | best gear | run_frame > retool_bonder > run_frame > run_fittings > precision_pass x2 > run_frame x2 > retool_cutter > precision_pass x2 > service > precision_pass > run_shell > retool_extruder > precision_pass > retool_bonder > precision_pass > service > run_shell > retool_extruder > run_fittings > service > precision_pass > run_shell > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 99.2 |
| 31-40 | fully modded gear | run_fittings > precision_pass > service > retool_extruder > run_shell > run_fittings > precision_pass > retool_cutter > run_frame > precision_pass x2 > retool_extruder > run_shell > precision_pass > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 | 99.6 |
| 41-53 | no gear | run_shell > precision_pass > run_shell > retool_bonder > run_fittings x2 > run_shell > precision_pass > service > run_shell > run_fittings > precision_pass > run_frame > retool_cutter > run_frame > service > precision_pass > retool_extruder > precision_pass > run_frame > service x2 > run_shell > run_fittings > run_frame > precision_pass > run_fittings > assemble | 53.0 | 52.8 | 52.4 | 30.7 |
| 41-53 | mid gear | retool_extruder > precision_pass > run_shell > precision_pass > run_shell > precision_pass x2 > retool_cutter > service > run_frame > precision_pass > service > run_frame > retool_cutter > precision_pass > run_frame > precision_pass > run_frame > retool_bonder > run_fittings > service > run_frame > precision_pass > run_fittings > precision_pass > service > precision_pass > run_fittings > assemble | 99.4 | 99.4 | 99.3 | 65.3 |
| 41-53 | best gear | precision_pass > run_frame > retool_extruder > run_frame > run_shell > precision_pass > run_shell > precision_pass > run_shell > run_frame > service > run_frame > precision_pass > run_frame > service > precision_pass > run_shell > run_fittings > precision_pass > service > precision_pass > retool_cutter > precision_pass x2 > retool_bonder > precision_pass > service > precision_pass > run_shell > run_fittings > service > precision_pass > assemble | 95.5 | 95.5 | 95.7 | 93.3 |
| 41-53 | fully modded gear | run_fittings > run_frame > run_shell > retool_bonder > run_frame > precision_pass > retool_extruder > precision_pass > service > precision_pass > retool_cutter > precision_pass > retool_extruder > retool_bonder > precision_pass > service > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 100.0 | 99.7 |

### Reported metric, not gated: the best fixed rotation for each recipe alone, and what the band's single rotation scores on the same recipe (average quality in points)

| Band | Gear | Recipe | Best rotation for this recipe alone (reported) | Its hold-out | The single rotation on this recipe | Adaptive on this recipe |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | structure_0077 (level 2) | run_shell x2 > precision_pass x3 > run_frame > run_fittings > run_frame > run_fittings > run_shell > assemble | 20.0 | 20.0 | 20.0 |
| 1-10 | no gear | structure_0119 (level 4) | run_shell > run_frame x2 > run_shell > run_frame > run_shell > precision_pass > retool_extruder > run_frame > precision_pass > retool_extruder > precision_pass > run_fittings > precision_pass > retool_bonder > precision_pass x2 > run_shell x2 > precision_pass > retool_cutter > precision_pass > run_frame > run_shell > run_fittings > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 97.2 |
| 1-10 | no gear | structure_0059 (level 6) | run_frame > precision_pass > run_shell > precision_pass > run_shell x2 > run_fittings x2 > precision_pass > retool_extruder > precision_pass x3 > retool_bonder > run_fittings > precision_pass > run_frame > run_shell x2 > precision_pass x2 > assemble | 100.0 | 100.0 | 85.4 |
| 1-10 | no gear | structure_0409 (level 7) | run_shell > precision_pass > run_fittings > run_frame > run_fittings > run_frame > precision_pass > run_shell > run_frame > precision_pass > retool_cutter > precision_pass x3 > retool_bonder > run_fittings > precision_pass > run_frame x2 > precision_pass x2 > assemble | 100.0 | 100.0 | 92.4 |
| 1-10 | no gear | structure_0019 (level 9) | run_frame x2 > precision_pass > run_shell > precision_pass > retool_extruder > precision_pass x2 > run_fittings > precision_pass > retool_bonder > precision_pass x3 > retool_cutter > run_frame > run_shell x2 > run_fittings > precision_pass > assemble | 100.0 | 100.0 | 96.6 |
| 1-10 | no gear | structure_0192 (level 10) | run_fittings > run_frame > precision_pass x3 > run_shell > run_frame > run_shell > run_fittings > retool_cutter > precision_pass > retool_extruder > precision_pass x3 > run_frame > retool_cutter > precision_pass x2 > assemble | 100.0 | 100.0 | 96.7 |
| 1-10 | mid gear | structure_0077 (level 2) | run_shell x2 > run_frame > run_fittings > run_frame > run_fittings > precision_pass x2 > retool_cutter > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | mid gear | structure_0119 (level 4) | run_frame > precision_pass > run_shell > precision_pass > retool_extruder > run_shell > run_fittings > run_frame > precision_pass x2 > retool_bonder > run_fittings > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | mid gear | structure_0059 (level 6) | run_frame > precision_pass x2 > run_shell > retool_extruder > run_shell > run_fittings > run_frame > run_fittings > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | mid gear | structure_0409 (level 7) | run_shell x2 > run_frame x2 > run_fittings > run_frame > precision_pass x2 > retool_cutter > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | mid gear | structure_0019 (level 9) | run_frame > run_shell > precision_pass x2 > retool_extruder > run_shell > run_fittings > precision_pass x2 > retool_bonder > run_frame > run_fittings > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | mid gear | structure_0192 (level 10) | run_fittings > precision_pass x2 > retool_cutter > run_frame > precision_pass x2 > retool_extruder > run_shell > precision_pass x2 > run_fittings > run_frame > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | structure_0077 (level 2) | run_shell > precision_pass > run_fittings > precision_pass > retool_bonder > run_frame > precision_pass x2 > retool_cutter > precision_pass > run_frame > run_shell x2 > run_fittings > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | structure_0119 (level 4) | run_frame > run_shell > precision_pass x2 > retool_extruder > run_fittings > run_shell > run_frame > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | structure_0059 (level 6) | run_frame > run_shell > precision_pass x2 > retool_extruder > run_shell > run_fittings > run_frame > run_fittings > precision_pass > retool_bonder > precision_pass x2 > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | structure_0409 (level 7) | run_shell > run_frame > run_shell > run_fittings > run_frame > precision_pass x2 > retool_cutter > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | structure_0019 (level 9) | run_frame > precision_pass > run_fittings > precision_pass > retool_bonder > run_shell > precision_pass x2 > retool_extruder > run_shell > run_frame > run_fittings > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | structure_0192 (level 10) | run_fittings > precision_pass x2 > retool_cutter > run_frame > precision_pass x2 > retool_extruder > run_shell > precision_pass x2 > run_fittings > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | structure_0077 (level 2) | run_frame > run_shell > precision_pass > run_fittings > retool_cutter > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | structure_0119 (level 4) | run_frame > run_shell > precision_pass > run_fittings > run_shell > retool_extruder > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | structure_0059 (level 6) | run_frame > run_shell > precision_pass > run_fittings > run_shell > retool_extruder > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | structure_0409 (level 7) | run_frame > run_shell > precision_pass > run_fittings > run_frame > retool_cutter > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | structure_0019 (level 9) | run_frame > run_shell > precision_pass > run_fittings > run_shell > retool_extruder > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | structure_0192 (level 10) | run_fittings > precision_pass > retool_cutter > run_frame > precision_pass > run_shell > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | no gear | structure_0341 (level 11) | run_frame x2 > precision_pass > run_frame > run_shell > precision_pass > retool_extruder > precision_pass x3 > retool_bonder > run_fittings > run_shell > precision_pass > run_shell > precision_pass > retool_cutter > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 99.4 |
| 11-20 | no gear | structure_0139 (level 13) | run_fittings > precision_pass x3 > run_frame x2 > run_shell > run_fittings > run_shell > retool_cutter > precision_pass x3 > retool_extruder > precision_pass x3 > run_frame > assemble | 100.0 | 100.0 | 97.8 |
| 11-20 | no gear | structure_0339 (level 14) | run_shell > precision_pass > run_shell > run_frame > precision_pass > retool_cutter > precision_pass > run_fittings x2 > precision_pass x2 > run_fittings > retool_bonder > precision_pass x3 > run_frame > run_fittings > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 96.9 |
| 11-20 | no gear | structure_0018 (level 16) | run_shell > precision_pass > run_fittings > precision_pass > retool_bonder > precision_pass x2 > retool_cutter > run_frame > precision_pass x3 > retool_extruder > precision_pass > run_shell > run_frame > run_fittings > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 99.5 |
| 11-20 | no gear | structure_0005 (level 18) | run_shell > run_frame > precision_pass > run_frame > run_fittings > precision_pass > retool_bonder > precision_pass > retool_bonder > precision_pass > run_shell > precision_pass > retool_extruder > precision_pass x3 > run_shell > run_fittings > run_frame > run_fittings > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 97.6 |
| 11-20 | no gear | structure_0014 (level 20) | run_shell > precision_pass > run_frame > precision_pass > retool_cutter > precision_pass x3 > retool_bonder > run_fittings > precision_pass x3 > retool_extruder > run_frame > run_shell > precision_pass > assemble | 100.0 | 100.0 | 97.6 |
| 11-20 | mid gear | structure_0341 (level 11) | run_frame x2 > run_shell x2 > run_fittings > run_shell > precision_pass x2 > retool_extruder > precision_pass x2 > retool_bonder > run_fittings > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | structure_0139 (level 13) | run_fittings > precision_pass > run_frame > precision_pass > run_frame > run_shell > run_fittings > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_shell > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | structure_0339 (level 14) | run_shell > precision_pass x2 > retool_cutter > run_frame > run_shell > run_fittings > run_shell > precision_pass x2 > retool_bonder > run_fittings > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | structure_0018 (level 16) | run_shell x2 > precision_pass x2 > retool_cutter > run_frame > precision_pass > run_frame > run_fittings > precision_pass > retool_bonder > precision_pass > run_fittings > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | structure_0005 (level 18) | run_frame > run_shell > precision_pass x2 > retool_extruder > run_fittings > precision_pass > retool_bonder > run_shell > run_fittings > run_frame > run_shell > precision_pass x2 > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | structure_0014 (level 20) | run_shell x2 > run_frame > run_fittings > run_frame > run_fittings > precision_pass x2 > retool_cutter > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | structure_0341 (level 11) | run_frame > run_shell > precision_pass x2 > retool_extruder > run_fittings > precision_pass x2 > retool_bonder > run_shell > run_frame > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | structure_0139 (level 13) | run_fittings > precision_pass x2 > run_frame > run_shell > run_fittings > run_frame > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_shell > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | structure_0339 (level 14) | run_shell > run_frame > run_fittings > run_shell > run_frame > precision_pass > retool_cutter > precision_pass x2 > retool_bonder > precision_pass x2 > run_fittings > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | structure_0018 (level 16) | run_shell > run_frame > precision_pass > retool_cutter > run_fittings > precision_pass x2 > retool_bonder > run_frame > run_shell > precision_pass x2 > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | structure_0005 (level 18) | run_frame > run_shell > precision_pass x2 > retool_extruder > precision_pass > retool_bonder > run_fittings > run_shell > precision_pass x2 > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | structure_0014 (level 20) | run_shell > run_frame > precision_pass > retool_cutter > run_fittings > run_shell > run_frame > run_fittings > precision_pass x2 > retool_bonder > precision_pass x2 > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | structure_0341 (level 11) | run_frame > run_shell > precision_pass > run_fittings > run_shell > retool_extruder > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | structure_0139 (level 13) | run_fittings > precision_pass > run_frame > retool_cutter > precision_pass > run_shell > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | structure_0339 (level 14) | run_frame > run_shell > precision_pass > run_fittings > retool_cutter > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | structure_0018 (level 16) | run_frame > run_shell > precision_pass > run_fittings > run_frame > retool_cutter > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | structure_0005 (level 18) | run_frame > run_shell > precision_pass > retool_bonder > run_shell > run_fittings > precision_pass > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | structure_0014 (level 20) | run_frame > run_shell > precision_pass > run_fittings > retool_cutter > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | structure_0147 (level 22) | run_fittings > run_frame > precision_pass > run_fittings > run_frame > run_shell > precision_pass > retool_extruder > run_shell > precision_pass x2 > run_fittings > retool_bonder > precision_pass x2 > run_fittings > precision_pass > retool_cutter > precision_pass > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 99.5 |
| 21-30 | no gear | structure_0037 (level 25) | run_shell > run_fittings > precision_pass x3 > run_frame > run_shell > run_frame > run_fittings > retool_cutter > precision_pass x3 > retool_extruder > precision_pass x3 > run_frame > assemble | 100.0 | 100.0 | 99.6 |
| 21-30 | no gear | structure_0298 (level 26) | run_fittings > precision_pass x3 > run_frame > run_shell > run_frame > run_shell > run_fittings > retool_cutter > precision_pass x3 > retool_extruder > precision_pass x3 > run_frame > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | structure_0377 (level 27) | run_shell > precision_pass x2 > run_frame > retool_cutter > precision_pass x2 > retool_bonder > run_fittings > precision_pass x3 > retool_extruder > precision_pass > run_shell > run_frame > run_shell > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | structure_0093 (level 29) | run_shell > precision_pass > run_shell > run_frame > precision_pass > retool_cutter > precision_pass x2 > retool_bonder > run_fittings > precision_pass > run_frame > precision_pass > retool_extruder > precision_pass > retool_cutter > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 99.9 |
| 21-30 | no gear | structure_0356 (level 30) | run_fittings > precision_pass > run_shell > precision_pass x2 > run_frame x2 > run_shell > run_fittings > run_frame > retool_cutter > precision_pass x3 > retool_extruder > precision_pass x3 > run_shell > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | structure_0147 (level 22) | run_frame > run_shell x2 > precision_pass x2 > retool_extruder > precision_pass > run_frame > run_shell > run_fittings > precision_pass > retool_bonder > run_fittings > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | structure_0037 (level 25) | run_fittings > precision_pass x2 > retool_cutter > run_frame > precision_pass x2 > retool_extruder > run_shell > precision_pass x2 > run_fittings > run_frame > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | structure_0298 (level 26) | run_fittings > precision_pass x2 > retool_cutter > run_frame > precision_pass x2 > retool_extruder > run_shell > precision_pass x2 > run_fittings > run_frame > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | structure_0377 (level 27) | run_shell x2 > run_frame > run_fittings > run_frame > run_fittings > precision_pass x2 > retool_cutter > precision_pass > retool_bonder > precision_pass x2 > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | structure_0093 (level 29) | run_shell x2 > run_frame > precision_pass x2 > retool_cutter > run_fittings > run_frame > precision_pass > retool_bonder > run_fittings > precision_pass x2 > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | structure_0356 (level 30) | run_fittings > run_frame > precision_pass x2 > run_shell > run_frame > run_shell > run_fittings > run_shell > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_frame > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | structure_0147 (level 22) | run_frame > run_shell > precision_pass x2 > retool_extruder > run_shell > run_fittings > run_frame > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | structure_0037 (level 25) | run_fittings > precision_pass x2 > retool_cutter > run_frame > precision_pass x2 > retool_extruder > run_shell > precision_pass x2 > run_fittings > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | structure_0298 (level 26) | run_frame > run_fittings > precision_pass x2 > run_shell > run_frame > run_shell > retool_cutter > precision_pass x2 > retool_extruder > precision_pass x2 > run_fittings > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | structure_0377 (level 27) | run_shell x2 > run_frame > run_fittings > precision_pass > retool_cutter > precision_pass x2 > retool_bonder > precision_pass x2 > run_frame > run_fittings > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | structure_0093 (level 29) | run_shell x2 > run_frame > run_fittings > run_frame > precision_pass x2 > retool_cutter > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | structure_0356 (level 30) | run_fittings > precision_pass x2 > retool_cutter > run_frame > precision_pass x2 > retool_extruder > run_shell > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | structure_0147 (level 22) | run_frame > run_shell > precision_pass > run_fittings > run_shell > retool_extruder > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | structure_0037 (level 25) | run_fittings > precision_pass > retool_cutter > run_frame > precision_pass > run_shell > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | structure_0298 (level 26) | run_fittings > precision_pass > retool_cutter > run_frame > precision_pass > run_shell > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | structure_0377 (level 27) | run_frame > run_shell > precision_pass > run_fittings > retool_cutter > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | structure_0093 (level 29) | run_frame > run_shell > precision_pass > run_fittings > retool_cutter > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | structure_0356 (level 30) | run_fittings > run_frame > run_shell > precision_pass > run_shell > retool_cutter > precision_pass > retool_extruder > precision_pass > run_frame > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | structure_0379 (level 31) | run_shell > precision_pass > run_shell > run_frame > precision_pass > retool_cutter > precision_pass x2 > run_fittings > retool_bonder > precision_pass x3 > run_frame > run_fittings > run_frame > retool_extruder > precision_pass > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | structure_0424 (level 32) | run_frame > run_shell > precision_pass x2 > run_frame > retool_cutter > precision_pass x3 > retool_bonder > run_fittings > precision_pass x3 > retool_extruder > run_frame x2 > run_shell > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | structure_0384 (level 34) | run_shell > precision_pass > run_fittings > precision_pass > retool_bonder > precision_pass > run_frame > retool_cutter > precision_pass x3 > retool_bonder > precision_pass x2 > retool_extruder > run_frame x2 > run_shell > run_fittings > service > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | structure_0108 (level 36) | run_frame > precision_pass x2 > retool_extruder > run_shell > precision_pass x2 > retool_cutter > run_shell > precision_pass > retool_extruder > precision_pass > service > run_fittings > retool_bonder > service > precision_pass > run_frame > run_fittings > run_shell > precision_pass x2 > assemble | 100.0 | 100.0 | 95.6 |
| 31-40 | no gear | structure_0325 (level 37) | run_shell > precision_pass > run_shell > run_frame > precision_pass x2 > retool_cutter > precision_pass x2 > retool_bonder > run_fittings > service > precision_pass x3 > retool_cutter > run_frame > precision_pass > assemble | 100.0 | 100.0 | 99.2 |
| 31-40 | no gear | structure_0104 (level 40) | run_fittings > precision_pass > run_frame > precision_pass x2 > run_fittings > run_shell > run_frame > run_shell > service > retool_cutter > precision_pass x3 > retool_extruder > service > precision_pass x3 > run_frame > assemble | 100.0 | 100.0 | 75.9 |
| 31-40 | mid gear | structure_0379 (level 31) | run_shell > precision_pass > run_frame > precision_pass > retool_cutter > run_shell > precision_pass > run_fittings > precision_pass > retool_bonder > run_frame > run_fittings > run_shell > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | structure_0424 (level 32) | run_shell > precision_pass > run_frame > precision_pass > run_fittings > precision_pass > retool_cutter > run_frame > precision_pass x3 > retool_bonder > run_shell > run_fittings > precision_pass > retool_bonder > precision_pass > run_frame > run_fittings > precision_pass > assemble | 100.0 | 100.0 | 98.8 |
| 31-40 | mid gear | structure_0384 (level 34) | run_shell > precision_pass > retool_cutter > run_frame > precision_pass x3 > retool_extruder > run_fittings > precision_pass > retool_bonder > run_frame > precision_pass x2 > run_shell > run_fittings > retool_extruder > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | structure_0108 (level 36) | run_frame > precision_pass > run_shell > retool_extruder > precision_pass x2 > retool_cutter > precision_pass x2 > retool_bonder > run_fittings > precision_pass > service > precision_pass x2 > retool_extruder > run_frame > run_shell > service > precision_pass > assemble | 100.0 | 100.0 | 97.9 |
| 31-40 | mid gear | structure_0325 (level 37) | run_shell > precision_pass > run_frame > precision_pass > retool_cutter > precision_pass x2 > retool_bonder > run_fittings > precision_pass x2 > retool_extruder > service > precision_pass > retool_cutter > run_frame > run_shell > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 99.5 |
| 31-40 | mid gear | structure_0104 (level 40) | run_fittings > precision_pass x2 > run_shell > precision_pass > run_frame > run_fittings > run_frame > retool_cutter > service > precision_pass x3 > retool_extruder > precision_pass > service > precision_pass x2 > run_shell > assemble | 100.0 | 100.0 | 96.3 |
| 31-40 | best gear | structure_0379 (level 31) | run_shell > run_frame > run_fittings > run_shell > run_frame > run_fittings > precision_pass x2 > retool_cutter > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | structure_0424 (level 32) | run_shell > run_frame > precision_pass x2 > retool_cutter > run_fittings > run_shell > precision_pass x2 > retool_bonder > precision_pass > run_frame > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | structure_0384 (level 34) | run_shell > run_frame > precision_pass x2 > retool_cutter > run_frame > run_fittings > run_shell > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | structure_0108 (level 36) | run_frame > run_shell x2 > precision_pass x2 > retool_extruder > run_fittings > run_shell > precision_pass > run_fittings > service > precision_pass > retool_bonder > precision_pass > run_frame > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | structure_0325 (level 37) | run_fittings x2 > run_shell x2 > precision_pass > run_fittings > precision_pass > retool_bonder > precision_pass > retool_cutter > run_frame > precision_pass x2 > retool_cutter > retool_extruder > retool_cutter > retool_extruder > retool_cutter > retool_bonder > run_fittings > service > precision_pass x2 > retool_cutter > precision_pass > retool_extruder > precision_pass > assemble | 100.0 | 100.0 | 98.0 |
| 31-40 | best gear | structure_0104 (level 40) | run_fittings > precision_pass x3 > retool_cutter > run_frame > precision_pass x2 > service > precision_pass > retool_extruder > run_shell > precision_pass x3 > service > run_fittings > assemble | 100.0 | 100.0 | 97.4 |
| 31-40 | fully modded gear | structure_0379 (level 31) | run_frame x2 > run_shell > precision_pass x2 > run_fittings > retool_cutter > run_fittings > precision_pass x2 > retool_bonder > precision_pass x2 > run_frame > assemble | 100.0 | 100.0 | 98.8 |
| 31-40 | fully modded gear | structure_0424 (level 32) | run_shell > run_fittings > precision_pass > retool_cutter > run_frame > precision_pass > retool_bonder > precision_pass x2 > retool_extruder > precision_pass > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 99.3 |
| 31-40 | fully modded gear | structure_0384 (level 34) | run_shell > run_frame > precision_pass > retool_cutter > run_fittings > precision_pass x2 > retool_bonder > run_frame > precision_pass > retool_extruder > precision_pass > retool_bonder > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | structure_0108 (level 36) | run_frame > run_shell > precision_pass x2 > retool_extruder > run_fittings > precision_pass x2 > retool_bonder > run_shell > precision_pass > service > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | structure_0325 (level 37) | run_shell > run_frame > precision_pass x2 > retool_cutter > run_fittings > precision_pass x2 > retool_bonder > run_frame > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | structure_0104 (level 40) | run_fittings > precision_pass x2 > retool_cutter > run_frame > run_shell > precision_pass x2 > retool_extruder > service > precision_pass x2 > assemble | 100.0 | 100.0 | 99.7 |
| 41-53 | no gear | structure_0048 (level 42) | run_frame > run_shell > precision_pass > run_shell > run_frame > run_fittings > run_frame > precision_pass > run_fittings > service > precision_pass > retool_cutter > retool_bonder > precision_pass > service > precision_pass > retool_cutter > service > precision_pass x3 > assemble | 92.4 | 55.7 | 93.4 |
| 41-53 | no gear | structure_0317 (level 44) | run_frame > run_fittings x2 > precision_pass > run_fittings > precision_pass > service > precision_pass > run_fittings > retool_cutter > run_frame > service x2 > run_frame > precision_pass x2 > retool_extruder > run_shell > service > precision_pass x3 > run_frame > run_fittings > assemble | 86.2 | 54.1 | 47.1 |
| 41-53 | no gear | structure_0103 (level 46) | run_shell > precision_pass x2 > service > precision_pass x2 > run_fittings > run_frame x2 > service > run_shell > run_fittings > assemble | 20.0 | 53.4 | 10.1 |
| 41-53 | no gear | structure_5015 (level 47) | run_fittings x2 > run_shell > service > precision_pass > run_shell > service > run_fittings > precision_pass > retool_extruder > precision_pass > retool_cutter > run_frame > service > precision_pass > retool_cutter > precision_pass > retool_extruder > service x2 > run_shell > run_fittings > run_frame > precision_pass > run_fittings > assemble | 65.0 | 51.7 | 14.2 |
| 41-53 | no gear | structure_0324 (level 49) | run_shell > run_fittings > service > precision_pass x4 > run_shell > retool_bonder > service > run_frame > retool_cutter > run_frame > run_fittings > assemble | 20.0 | 49.8 | 5.5 |
| 41-53 | no gear | structure_5003 (level 50) | precision_pass > run_fittings > precision_pass > retool_bonder > precision_pass x2 > retool_bonder > run_frame > retool_extruder > run_shell > service > precision_pass > run_fittings > service > precision_pass > retool_cutter > service > precision_pass > run_frame > run_fittings > assemble | 52.5 | 49.8 | 13.9 |
| 41-53 | mid gear | structure_0048 (level 42) | run_shell > precision_pass > run_shell > run_frame > precision_pass > retool_cutter > precision_pass x2 > retool_extruder > service > retool_bonder > run_fittings > precision_pass > run_frame > precision_pass > service > precision_pass > retool_extruder > precision_pass > retool_cutter > precision_pass > assemble | 100.0 | 100.0 | 96.8 |
| 41-53 | mid gear | structure_0317 (level 44) | run_fittings > precision_pass x2 > service > precision_pass > retool_cutter > run_frame > precision_pass x3 > retool_extruder > run_shell > service > precision_pass x3 > run_frame > service > run_fittings > assemble | 100.0 | 100.0 | 90.6 |
| 41-53 | mid gear | structure_0103 (level 46) | run_shell > precision_pass x3 > service > precision_pass x2 > run_shell > retool_bonder > retool_cutter > run_fittings x2 > run_frame > service > retool_cutter > precision_pass > service > precision_pass > retool_bonder > run_fittings > service > precision_pass > run_frame > precision_pass > run_frame > service > run_frame > precision_pass > assemble | 92.4 | 100.0 | 61.9 |
| 41-53 | mid gear | structure_5015 (level 47) | run_fittings > precision_pass > service > run_fittings x2 > run_shell > precision_pass > run_shell > precision_pass > retool_cutter > run_frame > run_fittings > run_shell > retool_cutter > service > precision_pass > retool_extruder > precision_pass > service > precision_pass > retool_cutter > precision_pass > retool_cutter > precision_pass > run_frame > assemble | 91.0 | 100.0 | 34.4 |
| 41-53 | mid gear | structure_0324 (level 49) | run_shell x2 > precision_pass > run_frame > run_shell > precision_pass > run_frame > run_shell > service > precision_pass > service > run_fittings > retool_cutter > run_shell > precision_pass > service > retool_cutter > precision_pass > run_frame > precision_pass > retool_bonder > precision_pass > run_fittings > service > precision_pass > run_fittings > assemble | 87.3 | 99.1 | 47.9 |
| 41-53 | mid gear | structure_5003 (level 50) | run_fittings > precision_pass > run_fittings > service > precision_pass x2 > retool_extruder > run_shell > retool_cutter > run_frame > service > precision_pass x3 > retool_extruder > run_shell > run_fittings > precision_pass > service > precision_pass x2 > run_frame > assemble | 97.3 | 96.8 | 60.5 |
| 41-53 | best gear | structure_0048 (level 42) | run_shell > precision_pass x2 > retool_cutter > run_frame > service > precision_pass x2 > retool_bonder > run_fittings > precision_pass x3 > retool_cutter > retool_extruder > run_shell > service > precision_pass > retool_cutter > precision_pass > assemble | 100.0 | 98.0 | 99.2 |
| 41-53 | best gear | structure_0317 (level 44) | run_fittings > precision_pass x2 > run_frame > precision_pass > run_shell > run_frame > service > run_shell > retool_cutter > precision_pass x2 > service > precision_pass > retool_extruder > precision_pass x2 > service > precision_pass > run_frame > assemble | 100.0 | 96.9 | 97.5 |
| 41-53 | best gear | structure_0103 (level 46) | run_shell > precision_pass x3 > retool_cutter > service > run_frame > run_shell > run_fittings > precision_pass > service > precision_pass > retool_cutter > precision_pass x2 > retool_bonder > precision_pass > service > precision_pass > run_shell > run_fittings > service > precision_pass > assemble | 100.0 | 94.9 | 96.7 |
| 41-53 | best gear | structure_5015 (level 47) | run_fittings > precision_pass > service > precision_pass x2 > run_shell > run_frame > run_fittings > run_shell > retool_cutter > service > precision_pass > service > precision_pass x2 > retool_extruder > precision_pass x2 > service > precision_pass > run_frame > assemble | 100.0 | 94.5 | 84.5 |
| 41-53 | best gear | structure_0324 (level 49) | run_fittings x2 > run_shell > service > run_shell > precision_pass x3 > run_fittings > retool_bonder > precision_pass > service > precision_pass > retool_cutter > run_frame > service > precision_pass > run_shell > precision_pass > service > precision_pass > retool_bonder > run_fittings > precision_pass > assemble | 100.0 | 94.7 | 90.7 |
| 41-53 | best gear | structure_5003 (level 50) | run_fittings > precision_pass x3 > retool_cutter > run_frame > service > precision_pass x2 > retool_extruder > run_shell > precision_pass > service > precision_pass > run_fittings > service > precision_pass > retool_cutter > precision_pass > assemble | 100.0 | 95.1 | 91.2 |
| 41-53 | fully modded gear | structure_0048 (level 42) | run_shell > run_frame > precision_pass x2 > retool_cutter > run_fittings > precision_pass x2 > retool_bonder > service > precision_pass x2 > assemble | 100.0 | 100.0 | 99.5 |
| 41-53 | fully modded gear | structure_0317 (level 44) | run_fittings > precision_pass x2 > retool_cutter > run_frame > precision_pass x2 > run_shell > retool_extruder > service > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 41-53 | fully modded gear | structure_0103 (level 46) | run_shell > run_frame > precision_pass x2 > retool_cutter > precision_pass > run_fittings > retool_bonder > service > precision_pass x2 > retool_cutter > service > precision_pass > assemble | 100.0 | 100.0 | 100.0 |
| 41-53 | fully modded gear | structure_5015 (level 47) | run_fittings > precision_pass x2 > run_shell > run_frame > retool_cutter > precision_pass > service > precision_pass > retool_extruder > service > precision_pass x2 > assemble | 100.0 | 100.0 | 100.0 |
| 41-53 | fully modded gear | structure_0324 (level 49) | run_shell > run_frame > run_fittings > precision_pass x2 > service > retool_cutter > precision_pass x2 > retool_bonder > precision_pass x2 > assemble | 100.0 | 100.0 | 99.5 |
| 41-53 | fully modded gear | structure_5003 (level 50) | run_fittings > precision_pass x2 > retool_cutter > run_frame > precision_pass x2 > service > run_shell > retool_extruder > precision_pass x2 > assemble | 100.0 | 100.0 | 99.2 |

## Style balance

### Style pairs: how often each style is better, and the average quality (quality points; ties count half a win each)

| Style pair | Gear | Recipes | First better | Second better | First avg quality | Second avg quality | Difference | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Production Run vs Fine Tolerance | no gear | 19 | 55% | 45% | 78.8 | 73.7 | +5.1 | Failed |
| Production Run vs Fine Tolerance | mid gear | 19 | 50% | 50% | 96.1 | 95.3 | +0.8 | Passed |
| Production Run vs Fine Tolerance | best gear | 19 | 37% | 63% | 97.3 | 98.9 | -1.5 | Passed |
| Production Run vs Fine Tolerance | fully modded gear | 19 | 45% | 55% | 99.4 | 99.0 | +0.4 | Passed |
| Production Run (full line) vs Fine Tolerance (full line) | no gear | 19 | 55% | 45% | 96.3 | 95.2 | +1.1 | Passed |
| Production Run (full line) vs Fine Tolerance (full line) | mid gear | 19 | 42% | 58% | 99.1 | 99.3 | -0.2 | Passed |
| Production Run (full line) vs Fine Tolerance (full line) | best gear | 19 | 47% | 53% | 99.4 | 99.0 | +0.4 | Passed |
| Production Run (full line) vs Fine Tolerance (full line) | fully modded gear | 19 | 42% | 58% | 99.6 | 99.8 | -0.2 | Passed |

## Perks optional

### Rank 50, fully modded gear, no perks: adaptive attempts that reach maximum quality, by recipe band

| Band | Recipes played | Attempts | Reach maximum quality | Make the item | Avg quality of items made | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 6 | 240 | 94% | 99% | 100% | >= 50% | Passed |
| 46-50 | 6 | 240 | 66% | 100% | 98% | >= 50% | Passed |
| 51-53 | 6 | 240 | 57% | 100% | 92% | >= 35% | Passed |

## Length

### Actions per attempt of the adaptive player at level (item craft at level)

| Band | Gear | Recipes played | Mean actions per attempt | Shortest recipe | Longest recipe | Stalled attempts | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 15.8 | 12.5 | 17.0 | 0 | 10-20 | Passed |
| 1-10 | mid gear | 6 | 15.3 | 14.7 | 16.0 | 0 | 10-20 | Passed |
| 1-10 | best gear | 6 | 15.0 | 14.2 | 15.4 | 0 | 10-20 | Passed |
| 1-10 | fully modded gear | 6 | 9.7 | 9.0 | 10.0 | 0 | 10-20 | Reported |
| 11-20 | no gear | 6 | 17.4 | 16.8 | 18.4 | 0 | 10-20 | Passed |
| 11-20 | mid gear | 6 | 15.6 | 15.3 | 16.0 | 0 | 10-20 | Passed |
| 11-20 | best gear | 6 | 14.8 | 14.4 | 15.1 | 0 | 10-20 | Passed |
| 11-20 | fully modded gear | 6 | 9.5 | 9.0 | 10.0 | 0 | 10-20 | Reported |
| 21-30 | no gear | 6 | 18.9 | 18.2 | 19.6 | 0 | 10-20 | Passed |
| 21-30 | mid gear | 6 | 15.3 | 14.9 | 15.7 | 0 | 10-20 | Passed |
| 21-30 | best gear | 6 | 14.6 | 14.0 | 15.2 | 0 | 10-20 | Passed |
| 21-30 | fully modded gear | 6 | 9.7 | 9.0 | 10.8 | 0 | 10-20 | Reported |
| 31-40 | no gear | 6 | 19.5 | 18.5 | 21.1 | 0 | 10-20 | Reported |
| 31-40 | mid gear | 6 | 18.2 | 15.3 | 20.6 | 0 | 10-20 | Passed |
| 31-40 | best gear | 6 | 15.8 | 14.2 | 18.6 | 0 | 10-20 | Passed |
| 31-40 | fully modded gear | 6 | 12.4 | 10.5 | 13.3 | 0 | 10-20 | Reported |
| 41-53 | no gear | 6 | 16.4 | 14.8 | 19.2 | 0 | 10-20 | Reported |
| 41-53 | mid gear | 6 | 19.9 | 18.9 | 21.1 | 0 | 10-20 | Reported |
| 41-53 | best gear | 6 | 19.9 | 19.3 | 20.4 | 0 | 10-20 | Passed |
| 41-53 | fully modded gear | 6 | 13.6 | 11.9 | 15.2 | 0 | 10-20 | Reported |

## Economy parity

### Component stacks consumed per item made, failures included, over the recipes the legacy policy makes at least once in the cell (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Parity rule | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 2.17 | 2.18 | 1.00 | 100% | 99% | 81% | 1% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | mid gear | 6 | 2.17 | 2.20 | 0.98 | 100% | 97% | 100% | 8% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | best gear | 6 | 2.17 | 2.18 | 0.99 | 100% | 99% | 100% | 19% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | fully modded gear | 6 | 2.17 | 2.19 | 0.99 | 100% | 99% | 100% | 58% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | no gear | 6 | 2.33 | 2.45 | 0.95 | 100% | 93% | 98% | 1% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | mid gear | 6 | 2.33 | 2.35 | 0.99 | 100% | 99% | 100% | 14% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | best gear | 6 | 2.33 | 2.35 | 0.99 | 100% | 99% | 100% | 31% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | fully modded gear | 6 | 2.33 | 2.35 | 0.99 | 100% | 99% | 100% | 94% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | no gear | 6 | 2.33 | 2.37 | 0.98 | 100% | 98% | 100% | 8% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | mid gear | 6 | 2.33 | 2.33 | 1.00 | 100% | 100% | 100% | 17% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | best gear | 6 | 2.33 | 2.34 | 1.00 | 100% | 100% | 100% | 30% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | fully modded gear | 6 | 2.33 | 2.35 | 0.99 | 100% | 99% | 100% | 94% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | no gear | 6 | 2.58 | 2.52 | 1.02 | 96% | 99% | 95% | 15% | yes | 0% / 0% | within 10% | Reported |
| 31-40 | mid gear | 6 | 2.50 | 2.54 | 0.98 | 100% | 98% | 99% | 27% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | best gear | 6 | 2.50 | 2.51 | 0.99 | 100% | 99% | 99% | 38% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | fully modded gear | 6 | 2.50 | 2.52 | 0.99 | 100% | 99% | 100% | 86% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | no gear | 6 | 3.40 | 2.35 | 1.45 | 58% | 99% | 31% | 42% | no | 0% / 0% | within 10% | Reported |
| 41-53 | mid gear | 6 | 2.81 | 2.33 | 1.20 | 76% | 100% | 65% | 73% | no | 0% / 0% | within 10% | Reported |
| 41-53 | best gear | 6 | 2.38 | 2.35 | 1.01 | 97% | 99% | 93% | 91% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | fully modded gear | 6 | 2.33 | 2.40 | 0.97 | 100% | 96% | 100% | 95% | yes | 0% / 0% | within 10% | Passed |

## XP rate

### Item crafts: XP per hour against the legacy loop (6.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per craft | Legacy actions per craft | Legacy XP per hour | New XP per craft | New actions per craft | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 600 | 5.0 | 72158 | 2612 | 15.8 | 74522 | 1.03 | - | Passed |
| 1-10 | no gear, 1 enhancement | 411 | 6.1 | 40636 | 2624 | 19.8 | 59713 | 1.47 | - | Passed |
| 1-10 | no gear, 2 enhancements | 141 | 6.0 | 14100 | 2626 | 23.9 | 49372 | 3.50 | - | Passed |
| 1-10 | no gear, 3 enhancements | 90 | 6.0 | 9000 | 2638 | 27.9 | 42610 | 4.73 | - | Passed |
| 1-10 | mid gear | 633 | 6.6 | 57467 | 2880 | 15.3 | 84591 | 1.47 | - | Passed |
| 1-10 | mid gear, 1 enhancement | 618 | 7.3 | 50487 | 2880 | 18.0 | 71933 | 1.42 | - | Passed |
| 1-10 | mid gear, 2 enhancements | 581 | 8.0 | 43366 | 2880 | 20.3 | 63816 | 1.47 | - | Passed |
| 1-10 | mid gear, 3 enhancements | 509 | 8.6 | 35455 | 2880 | 23.0 | 56368 | 1.59 | - | Passed |
| 1-10 | best gear | 705 | 6.8 | 62319 | 2880 | 15.0 | 86617 | 1.39 | - | Passed |
| 1-10 | best gear, 1 enhancement | 697 | 7.4 | 56329 | 2880 | 16.9 | 76724 | 1.36 | - | Passed |
| 1-10 | best gear, 2 enhancements | 672 | 7.9 | 50802 | 2880 | 19.0 | 68121 | 1.34 | - | Passed |
| 1-10 | best gear, 3 enhancements | 642 | 8.3 | 46533 | 2880 | 21.6 | 60104 | 1.29 | - | Passed |
| 1-10 | fully modded gear | 942 | 5.8 | 97355 | 2880 | 9.7 | 133608 | 1.37 | - | Passed |
| 1-10 | fully modded gear, 1 enhancement | 942 | 5.8 | 98257 | 2880 | 11.5 | 113105 | 1.15 | - | Passed |
| 1-10 | fully modded gear, 2 enhancements | 924 | 6.1 | 90616 | 2880 | 12.4 | 104376 | 1.15 | - | Passed |
| 1-10 | fully modded gear, 3 enhancements | 899 | 6.3 | 85365 | 2880 | 14.0 | 92848 | 1.09 | - | Passed |
| 11-20 | no gear | 572 | 8.1 | 42636 | 1783 | 17.4 | 46027 | 1.08 | - | Passed |
| 11-20 | no gear, 1 enhancement | 178 | 6.7 | 15987 | 1785 | 21.0 | 38310 | 2.40 | - | Passed |
| 11-20 | no gear, 2 enhancements | 170 | 7.1 | 14382 | 1786 | 24.6 | 32606 | 2.27 | - | Passed |
| 11-20 | no gear, 3 enhancements | 171 | 7.5 | 13763 | 1787 | 27.8 | 28920 | 2.10 | - | Passed |
| 11-20 | mid gear | 679 | 12.2 | 33512 | 1800 | 15.6 | 51993 | 1.55 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 644 | 12.8 | 30161 | 1800 | 17.6 | 46154 | 1.53 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 610 | 13.9 | 26361 | 1800 | 20.4 | 39730 | 1.51 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 556 | 15.6 | 21453 | 1800 | 23.0 | 35179 | 1.64 | - | Passed |
| 11-20 | best gear | 780 | 14.4 | 32417 | 1800 | 14.8 | 54900 | 1.69 | - | Passed |
| 11-20 | best gear, 1 enhancement | 752 | 14.7 | 30657 | 1800 | 16.9 | 47858 | 1.56 | - | Passed |
| 11-20 | best gear, 2 enhancements | 746 | 16.2 | 27667 | 1800 | 19.2 | 42288 | 1.53 | - | Passed |
| 11-20 | best gear, 3 enhancements | 701 | 16.0 | 26295 | 1800 | 21.4 | 37784 | 1.44 | - | Passed |
| 11-20 | fully modded gear | 1161 | 13.6 | 51168 | 1800 | 9.5 | 85563 | 1.67 | - | Passed |
| 11-20 | fully modded gear, 1 enhancement | 1159 | 13.9 | 49871 | 1800 | 11.0 | 73664 | 1.48 | - | Passed |
| 11-20 | fully modded gear, 2 enhancements | 1146 | 14.3 | 48200 | 1800 | 12.6 | 64499 | 1.34 | - | Passed |
| 11-20 | fully modded gear, 3 enhancements | 1143 | 14.5 | 47305 | 1800 | 13.8 | 58731 | 1.24 | - | Passed |
| 21-30 | no gear | 636 | 11.1 | 34553 | 1499 | 18.9 | 35684 | 1.03 | - | Passed |
| 21-30 | no gear, 1 enhancement | 606 | 11.8 | 30902 | 1497 | 21.2 | 31746 | 1.03 | - | Passed |
| 21-30 | no gear, 2 enhancements | 572 | 13.0 | 26387 | 1498 | 24.3 | 27751 | 1.05 | - | Passed |
| 21-30 | no gear, 3 enhancements | 566 | 14.7 | 23089 | 1495 | 27.0 | 24926 | 1.08 | - | Passed |
| 21-30 | mid gear | 704 | 17.8 | 23755 | 1500 | 15.3 | 44046 | 1.85 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 674 | 17.5 | 23118 | 1500 | 17.5 | 38627 | 1.67 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 667 | 17.9 | 22325 | 1500 | 19.6 | 34351 | 1.54 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 627 | 18.1 | 20761 | 1500 | 22.2 | 30463 | 1.47 | - | Passed |
| 21-30 | best gear | 779 | 22.4 | 20851 | 1500 | 14.7 | 46075 | 2.21 | - | Passed |
| 21-30 | best gear, 1 enhancement | 763 | 21.5 | 21292 | 1500 | 16.5 | 40930 | 1.92 | - | Passed |
| 21-30 | best gear, 2 enhancements | 741 | 22.5 | 19776 | 1500 | 18.7 | 36161 | 1.83 | - | Passed |
| 21-30 | best gear, 3 enhancements | 720 | 21.7 | 19940 | 1500 | 20.6 | 32840 | 1.65 | - | Passed |
| 21-30 | fully modded gear | 1159 | 21.8 | 31857 | 1500 | 9.7 | 69379 | 2.18 | - | Passed |
| 21-30 | fully modded gear, 1 enhancement | 1121 | 21.3 | 31555 | 1500 | 10.9 | 61950 | 1.96 | - | Passed |
| 21-30 | fully modded gear, 2 enhancements | 1117 | 21.4 | 31303 | 1500 | 12.1 | 55862 | 1.78 | - | Passed |
| 21-30 | fully modded gear, 3 enhancements | 1104 | 22.0 | 30104 | 1500 | 13.5 | 50062 | 1.66 | - | Passed |
| 31-40 | no gear | 686 | 13.7 | 30092 | 1264 | 19.5 | 29114 | 0.97 | - | Passed |
| 31-40 | no gear, 1 enhancement | 646 | 14.9 | 25972 | 1301 | 21.3 | 27515 | 1.06 | - | Passed |
| 31-40 | no gear, 2 enhancements | 612 | 14.6 | 25178 | 1319 | 22.7 | 26165 | 1.04 | - | Passed |
| 31-40 | no gear, 3 enhancements | 594 | 15.6 | 22856 | 1319 | 24.1 | 24670 | 1.08 | - | Passed |
| 31-40 | mid gear | 747 | 19.8 | 22704 | 1312 | 18.2 | 32369 | 1.43 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 737 | 20.6 | 21414 | 1312 | 19.2 | 30746 | 1.44 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 714 | 20.5 | 20903 | 1318 | 20.5 | 28924 | 1.38 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 680 | 19.6 | 20817 | 1319 | 22.3 | 26627 | 1.28 | - | Passed |
| 31-40 | best gear | 821 | 24.6 | 20008 | 1315 | 15.8 | 37517 | 1.88 | - | Passed |
| 31-40 | best gear, 1 enhancement | 816 | 25.3 | 19342 | 1316 | 16.4 | 36073 | 1.87 | - | Passed |
| 31-40 | best gear, 2 enhancements | 803 | 25.5 | 18914 | 1318 | 18.1 | 32747 | 1.73 | - | Passed |
| 31-40 | best gear, 3 enhancements | 778 | 24.1 | 19403 | 1319 | 19.2 | 30909 | 1.59 | - | Passed |
| 31-40 | fully modded gear | 1109 | 25.2 | 26365 | 1317 | 12.4 | 47988 | 1.82 | - | Passed |
| 31-40 | fully modded gear, 1 enhancement | 1119 | 25.5 | 26363 | 1318 | 13.0 | 45693 | 1.73 | - | Passed |
| 31-40 | fully modded gear, 2 enhancements | 1109 | 26.2 | 25367 | 1318 | 13.7 | 43319 | 1.71 | - | Passed |
| 31-40 | fully modded gear, 3 enhancements | 1093 | 26.1 | 25111 | 1318 | 14.8 | 40204 | 1.60 | - | Passed |
| 41-53 | no gear | 847 | 19.1 | 26585 | 606 | 16.4 | 16591 | 0.62 | - | Passed |
| 41-53 | no gear, 1 enhancement | 763 | 17.5 | 26201 | 447 | 17.9 | 11235 | 0.43 | - | Passed |
| 41-53 | no gear, 2 enhancements | 708 | 17.8 | 23830 | 373 | 20.1 | 8343 | 0.35 | - | Passed |
| 41-53 | no gear, 3 enhancements | 620 | 17.8 | 20904 | 316 | 22.4 | 6335 | 0.30 | - | Passed |
| 41-53 | mid gear | 1037 | 26.6 | 23404 | 923 | 19.9 | 20894 | 0.89 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 971 | 25.2 | 23146 | 880 | 21.0 | 18846 | 0.81 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 892 | 23.1 | 23142 | 956 | 22.8 | 18888 | 0.82 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 800 | 22.3 | 21510 | 1024 | 24.2 | 19006 | 0.88 | - | Passed |
| 41-53 | best gear | 1139 | 28.8 | 23732 | 1213 | 19.9 | 27460 | 1.16 | - | Passed |
| 41-53 | best gear, 1 enhancement | 1115 | 28.8 | 23267 | 1187 | 20.7 | 25845 | 1.11 | - | Passed |
| 41-53 | best gear, 2 enhancements | 1038 | 27.2 | 22945 | 1132 | 22.1 | 23074 | 1.01 | - | Passed |
| 41-53 | best gear, 3 enhancements | 983 | 26.3 | 22463 | 1209 | 23.4 | 23288 | 1.04 | - | Passed |
| 41-53 | fully modded gear | 1150 | 26.5 | 26062 | 1270 | 13.6 | 41947 | 1.61 | - | Passed |
| 41-53 | fully modded gear, 1 enhancement | 1168 | 26.7 | 26228 | 1265 | 14.9 | 38104 | 1.45 | - | Passed |
| 41-53 | fully modded gear, 2 enhancements | 1181 | 27.6 | 25642 | 1271 | 15.0 | 38041 | 1.48 | - | Passed |
| 41-53 | fully modded gear, 3 enhancements | 1179 | 28.2 | 25091 | 1271 | 15.4 | 37032 | 1.48 | - | Passed |

## Enhancement parity

### Rank 50, fully modded gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 100%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 6 | 60 | 32% | 98% | 99% | 0.97 | 100% | 100.0% | 1.00 (+/- 0.00) | 1.03 | >= 0.95 | Passed |
| 41-45 | 2 | 6 | 60 | 63% | 98% | 99% | 1.95 | 100% | 100.0% | 2.00 (+/- 0.00) | 1.03 | >= 0.95 | Passed |
| 41-45 | 3 | 6 | 60 | 95% | 95% | 99% | 2.84 | 100% | 100.0% | 3.00 (+/- 0.00) | 1.06 | >= 0.90 | Passed |
| 46-50 | 1 | 6 | 60 | 32% | 99% | 99% | 0.98 | 100% | 99.7% | 1.00 (+/- 0.00) | 1.02 | >= 0.95 | Passed |
| 46-50 | 2 | 6 | 60 | 64% | 99% | 99% | 1.97 | 100% | 99.3% | 1.99 (+/- 0.01) | 1.01 | >= 0.95 | Passed |
| 46-50 | 3 | 6 | 60 | 96% | 99% | 99% | 2.93 | 100% | 99.6% | 2.83 (+/- 0.03) | 0.97 | >= 0.90 | Passed |
| 51-53 | 1 | 6 | 60 | 10% | 100% | 53% | 0.53 | 95% | 92.4% | 0.94 (+/- 0.02) | 1.77 | >= 0.95 | Passed |
| 51-53 | 2 | 6 | 60 | 20% | 98% | 57% | 1.12 | 100% | 96.2% | 1.90 (+/- 0.02) | 1.69 | >= 0.95 | Passed |
| 51-53 | 3 | 6 | 60 | 30% | 100% | 52% | 1.53 | 100% | 96.6% | 2.67 (+/- 0.04) | 1.74 | >= 0.90 | Passed |

### Thresholds, first / second / third slot, that would meet parity for the quality measured (the strictest set that keeps every case at least at today's, never falling from one slot to the next)

| Scope | Thresholds that meet parity | In steps of 5 | Thresholds in force |
| --- | --- | --- | --- |
| Every band together (the shared thresholds) | 76% / 97% / 98% | 85% / 85% / 95% | 50% / 85% / 100% |
| Band 41-45 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 100% |
| Band 46-50 alone | 76% / 97% / 98% | 85% / 85% / 95% | 50% / 85% / 100% |
| Band 51-53 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 100% |

### Rank 50, fully modded gear: quality of the items the new minigame made, in whole percent, by band and slots filled

| Band | Slots filled | Items made | 10th percentile | 25th percentile | Median | 75th percentile | 90th percentile | Made at 100% |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 100% |
| 41-45 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 100% |
| 41-45 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 100% |
| 46-50 | 1 | 100% | 99% | 100% | 100% | 100% | 100% | 85% |
| 46-50 | 2 | 100% | 99% | 100% | 100% | 100% | 100% | 80% |
| 46-50 | 3 | 100% | 99% | 100% | 100% | 100% | 100% | 83% |
| 51-53 | 1 | 95% | 75% | 93% | 100% | 100% | 100% | 56% |
| 51-53 | 2 | 100% | 92% | 97% | 100% | 100% | 100% | 72% |
| 51-53 | 3 | 100% | 75% | 100% | 100% | 100% | 100% | 77% |

### Representative enhancements: for each recipe kind the real item at the best level the recipe accepts (the old window refuses one more than 5 levels above the recipe) whose penalty is the lower median of that level

| Recipe kind | Enhancement | Level | Progress penalty | Items of that kind and level (min / median / max penalty) | Recipes |
| --- | --- | --- | --- | --- | --- |
| Structure | Structure Enhancement - Structure Bonus V (`sten_sb5`) | 45 (tier V) | 60 | 2 items: 60 / 70.0 / 80 | 18 |

## Gear ladder

### Rank 50, 2 kits, no perks: what each step of gear buys

| Band | Recipes | Kind | Gear | Avg quality | Reach 85% | Reach 100% | Actions | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 46-50 | 6 | all | no gear | 28.3 | 25% | 10% | 20.4 | Passed |
| 46-50 | 6 | all | mid gear | 72.7 | 72% | 44% | 21.9 | Passed |
| 46-50 | 6 | all | best gear | 88.3 | 87% | 44% | 20.9 | Passed |
| 46-50 | 6 | all | fully modded gear | 99.3 | 99% | 80% | 15.2 | Passed |
| 51-53 | 6 | all | no gear | 3.8 | 0% | 0% | 15.3 | Passed |
| 51-53 | 6 | all | mid gear | 14.6 | 4% | 0% | 18.5 | Passed |
| 51-53 | 6 | all | best gear | 48.2 | 39% | 18% | 19.8 | Passed |
| 51-53 | 6 | all | fully modded gear | 95.8 | 91% | 72% | 16.8 | Passed |

### Perks do not replace gear: plain crafts, average quality

| Band | Style | Fully perked, no gear | No perks, unmodded best gear | Allowance | Verdict |
| --- | --- | --- | --- | --- | --- |
| 46-50 | Production Run (full line) | 70.0 | 75.0 | +5 | Passed |
| 46-50 | Fine Tolerance (full line) | 62.2 | 75.0 | +5 | Passed |
| 51-53 | Production Run (full line) | 41.7 | 37.1 | +5 | Passed |
| 51-53 | Fine Tolerance (full line) | 35.5 | 37.1 | +5 | Passed |

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
- Prime Feedstock is the only chance in the Fabricator, so the adaptive player's edge over a fixed rotation comes from it and from the recipe's state, not from luck at the actions themselves.
- A Builder is played with the numbers of the game's own perk definitions at the rank the recipe is played at. The unperked scenario plays no perks except in the style-balance gate, which compares the styles.
