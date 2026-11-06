# Crafting revamp: balance gates of the Circuit minigame

- Command: `SWLOR.CraftSim gates --scenario circuit --gates rotation,styles,perks,length,materials,xp,enhancements,ladder --runs 40 --seed 1 --recipes-per-band 6 --repo . --out design/testing/crafting-revamp/circuit-gates.md`
- Seed 1; 40 runs per case.

## Inputs

- Scenario: Circuit (`circuit`), Engineering recipes, minigame Circuit.
- Quality: the quality meter's share of its maximum when the item is made, in points (0-100). A broken or abandoned piece counts 0, so every average includes the crafts that failed.
- Crafter: the rank the scenario's cohort gives each recipe (by default the recipe's level, at most 50, so a crafter works at their own level), with no food or status bonuses and no perks unless a gate names a style or the scenario's limitations say it plays a perk loadout. Gear levels are the plan's none, middle and best tier of the skill's crafting gear:
  - no gear (Control 0, Craftsmanship 0, CP 0)
  - mid gear (Control 13, Craftsmanship 12, CP 20)
  - best gear (Control 29, Craftsmanship 30, CP 37)
  - fully modded gear (Control 119, Craftsmanship 120, CP 37)
- Recipes: 1-10: 6 of 307; 11-20: 6 of 367; 21-30: 6 of 357; 31-40: 6 of 370; 41-53: 6 of 381 recipes played.
- Adaptive player: depth 3, forecast samples per result 2, budget 4000 engine copies per decision, 0.5% of value lost per extra action, heuristic DefaultLeafHeuristic. Each attempt's dice come from its own stream of the seed.
- Fixed-rotation solver: beam 8, cycles up to 3, 8 adaptive lines as seeds, up to 40 actions, 64 training attempts per local-search round and 150 selection attempts for each of 12 finalists, local search from the best 3 candidate(s) for up to 3 round(s) each. Gate 1 searches one sequence for each band and gear tier across the band's sampled recipes (every recipe weighing the same; an action a recipe's engine cannot take is skipped there), and also each recipe's own best sequence as a reported metric. The winner is chosen on separate attempts and then scored on a third set (hold-out) with 40 attempts per recipe.
- Legacy comparator (gates 5 and 6): the old minigame's lookahead player (`SWLOR.CraftSim baseline`), replayed here with the same seed on the same recipes, ranks and gear.
- Time: 8.0 s per action in the new window and 6.0 s in the old one, plus 0.0 s per craft in both. These are estimates; the client evidence replaces them.
- XP multiplier on the base XP of every craft, failures included, as the game server applies it (`CraftMinigameXp`): 1-10 2.29x, 11-20 1.54x, 21-30 1.15x, 31-40 1.00x, 41-53 1.00x. The new side's XP below includes it; the legacy loop's does not.
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
- Styles compared: Overclock (no lines) vs Safe Mode (no lines) (from rank 20); Overclock (both lines) vs Safe Mode (both lines) (from rank 20).

## Verdicts

| Gate | Verdict | Summary |
| --- | --- | --- |
| No fixed rotation | Failed | One fixed rotation comes within 10 points of adaptive play in 10 case(s): 1-10 / no gear; 1-10 / mid gear; 1-10 / best gear; 11-20 / no gear; 11-20 / mid gear; 31-40 / no gear; 31-40 / mid gear; 41-53 / no gear; 41-53 / mid gear; 41-53 / best gear (smallest margin -15.6 in 41-53 / mid gear). The best rotation for each recipe alone, a reported metric, is -21.6 to -0.0 points behind adaptive play. |
| Style balance | Failed | 1 style comparison(s) are out of balance: Overclock (no lines) vs Safe Mode (no lines) / mid gear. |
| Perks optional | Passed | An unperked rank-50 crafter in fully modded gear reaches maximum quality on at least 50% of crafts in every at-level band (lowest 79%). |
| Length | Passed | Every band and gear tier takes 10-20 actions (item craft at level) on average. |
| Economy parity | Passed | Component stacks per item are within 10% of the legacy baseline where its success rate is at least 90%, and at most 10% above it where it is lower, at no less quality, in every band and gear tier. |
| XP rate | Passed | Item crafts: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.00x in 11-20 / no gear). |
| Enhancement parity | Passed | At rank 50 in fully modded gear, the thresholds in force (50% / 85% / 100%) transfer at least 0.90 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 0.98 times today's). No thresholds meet parity in every band at this quality (see the thresholds table). |
| Gear ladder | Passed | Every gear step raises quality, fully modded gear makes the top recipes' target, and perks do not replace gear. |

## No fixed rotation

### Average quality: adaptive play against the best single fixed rotation of each band and gear tier (quality points; a broken piece counts 0)

| Band | Gear | Recipes played | Adaptive avg quality | Best single rotation avg quality | Margin | Required | Best rotation per recipe avg quality (reported) | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 46.9 | 48.5 | -1.6 | >= 10 | 51.5 | Failed |
| 1-10 | mid gear | 6 | 86.9 | 94.8 | -8.0 | >= 10 | 98.2 | Failed |
| 1-10 | best gear | 6 | 90.5 | 98.9 | -8.4 | >= 10 | 100.0 | Failed |
| 1-10 | fully modded gear | 6 | 99.9 | 100.0 | -0.1 | >= 10 | 100.0 | Reported |
| 11-20 | no gear | 6 | 77.5 | 80.5 | -3.0 | >= 10 | 82.9 | Failed |
| 11-20 | mid gear | 6 | 95.7 | 100.0 | -4.3 | >= 10 | 100.0 | Failed |
| 11-20 | best gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 11-20 | fully modded gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 21-30 | no gear | 6 | 97.9 | 99.3 | -1.4 | >= 10 | 99.4 | Reported |
| 21-30 | mid gear | 6 | 99.1 | 100.0 | -0.9 | >= 10 | 100.0 | Reported |
| 21-30 | best gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 21-30 | fully modded gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 31-40 | no gear | 6 | 86.3 | 94.7 | -8.4 | >= 10 | 99.5 | Failed |
| 31-40 | mid gear | 6 | 94.5 | 100.0 | -5.5 | >= 10 | 100.0 | Failed |
| 31-40 | best gear | 6 | 98.7 | 100.0 | -1.3 | >= 10 | 100.0 | Reported |
| 31-40 | fully modded gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 41-53 | no gear | 6 | 48.1 | 58.6 | -10.5 | >= 10 | 63.9 | Failed |
| 41-53 | mid gear | 6 | 55.5 | 71.1 | -15.6 | >= 10 | 77.1 | Failed |
| 41-53 | best gear | 6 | 76.6 | 85.9 | -9.4 | >= 10 | 89.5 | Failed |
| 41-53 | fully modded gear | 6 | 97.4 | 100.0 | -2.6 | >= 10 | 100.0 | Reported |

### The single rotation found for each band and gear tier, searched across the band's sampled recipes (average quality in points)

| Band | Gear | Best single rotation for the band | Training | Selection | Hold-out | Adaptive on these recipes |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | wire > tune x2 > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire > discharge_calibration > wire x2 | 48.3 | 48.2 | 48.5 | 46.9 |
| 1-10 | mid gear | discharge_assembly > tune x4 > discharge_calibration > wire x3 > patch > tune > patch > discharge_calibration > wire x2 > tune > wire > discharge_calibration > tune > patch > wire x3 > discharge_calibration > wire x2 | 94.8 | 94.5 | 94.8 | 86.9 |
| 1-10 | best gear | tune > discharge_calibration > patch > tune x2 > patch x2 > tune > discharge_calibration > tune x3 > patch > wire > discharge_calibration > wire x3 > discharge_calibration > wire x4 | 98.9 | 98.3 | 98.9 | 90.5 |
| 1-10 | fully modded gear | tune x3 > discharge_calibration > tune x3 > wire x2 > discharge_assembly > tune > wire x3 | 100.0 | 100.0 | 100.0 | 99.9 |
| 11-20 | no gear | wire x2 > tune x2 > discharge_calibration > wire > tune x3 > discharge_calibration > wire x4 > discharge_calibration > wire x3 > discharge_calibration > wire > discharge_calibration > wire x2 | 80.9 | 80.6 | 80.5 | 77.5 |
| 11-20 | mid gear | wire > discharge_calibration > tune x4 > discharge_calibration > tune x3 > patch > tune > discharge_calibration > tune x2 > wire x3 > discharge_calibration > wire x4 > discharge_calibration > wire x3 | 100.0 | 100.0 | 100.0 | 95.7 |
| 11-20 | best gear | tune x4 > discharge_calibration > tune x3 > discharge_calibration > tune > wire x3 > discharge_assembly > tune > wire x3 > discharge_calibration > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | tune x4 > discharge_calibration > tune > wire x4 > tune > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | boost > tune x4 > discharge_calibration > discharge_assembly > tune x5 > patch > tune > discharge_assembly > wire x6 > discharge_calibration > wire x4 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire | 99.1 | 99.0 | 99.3 | 97.9 |
| 21-30 | mid gear | boost > ground > tune x4 > discharge_calibration > tune x3 > wire x3 > discharge_assembly > tune > wire x2 > patch > tune x2 > discharge_calibration > wire | 100.0 | 100.0 | 100.0 | 99.1 |
| 21-30 | best gear | boost > tune x3 > discharge_calibration > tune x5 > wire > discharge_assembly > wire x4 > tune > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | boost > tune x3 > discharge_calibration > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | boost > tune x3 > discharge_calibration > discharge_assembly > wire > tune x2 > patch > tune x2 > patch > discharge_assembly > wire x2 > patch > wire x2 > tune x2 > discharge_calibration > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire | 94.9 | 94.9 | 94.7 | 86.3 |
| 31-40 | mid gear | boost > tune x3 > discharge_calibration > tune > wire > tune x4 > discharge_calibration > tune x3 > wire > tune x2 > patch > discharge_assembly > wire x4 > tune > wire x2 > tune > wire | 100.0 | 100.0 | 100.0 | 94.5 |
| 31-40 | best gear | boost > tune x4 > discharge_calibration > tune x4 > patch > tune x2 > discharge_assembly > tune > wire > tune > wire x3 > patch > discharge_calibration > tune > wire | 100.0 | 100.0 | 100.0 | 98.7 |
| 31-40 | fully modded gear | boost > tune x3 > discharge_calibration > tune x3 > wire > tune > discharge_assembly > wire x2 > tune > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | tune x6 > patch > tune > wire > discharge_calibration > wire x7 > tune > discharge_calibration > tune > discharge_calibration > wire | 60.3 | 59.4 | 58.6 | 48.1 |
| 41-53 | mid gear | wire > tune x4 > patch > tune x2 > discharge_assembly > wire x2 > tune x2 > wire > tune x3 > discharge_calibration > tune > wire | 72.5 | 71.8 | 71.1 | 55.5 |
| 41-53 | best gear | discharge_assembly > tune x6 > patch > tune > wire > discharge_calibration > tune x5 > wire > tune > discharge_assembly > wire x3 | 85.8 | 84.9 | 85.9 | 76.6 |
| 41-53 | fully modded gear | boost > tune x4 > discharge_calibration > tune x2 > wire > tune x3 > discharge_assembly > tune > wire > patch > tune > discharge_calibration > wire | 100.0 | 100.0 | 100.0 | 97.4 |

### Reported metric, not gated: the best fixed rotation for each recipe alone, and what the band's single rotation scores on the same recipe (average quality in points)

| Band | Gear | Recipe | Best rotation for this recipe alone (reported) | Its hold-out | The single rotation on this recipe | Adaptive on this recipe |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | wen_cft_smth1 (level 3) | wire x5 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire | 37.5 | 33.3 | 34.3 |
| 1-10 | no gear | config_ind1 (level 5) | wire x2 > tune > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire | 43.8 | 44.1 | 44.8 |
| 1-10 | no gear | d_hd_pva1 (level 7) | wire x4 > tune > discharge_calibration > wire x2 > discharge_calibration > wire x4 > discharge_calibration > wire x2 > discharge_calibration > wire | 52.0 | 45.9 | 44.3 |
| 1-10 | no gear | song_460 (level 8) | wire x2 > tune x2 > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire > discharge_calibration > wire | 60.2 | 57.7 | 52.5 |
| 1-10 | no gear | d_lg_pvw1 (level 10) | wire > tune x2 > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire | 55.8 | 55.8 | 49.8 |
| 1-10 | no gear | id_soulstrike1 (level 10) | wire x3 > tune x2 > discharge_calibration > wire > discharge_calibration > wire x5 > discharge_calibration > wire > discharge_calibration > wire > discharge_calibration > wire | 59.7 | 54.3 | 55.6 |
| 1-10 | mid gear | wen_cft_smth1 (level 3) | tune x5 > discharge_calibration > tune x2 > wire > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire | 100.0 | 99.0 | 87.1 |
| 1-10 | mid gear | config_ind1 (level 5) | discharge_assembly > tune x3 > discharge_calibration > wire x2 > tune > discharge_calibration > discharge_assembly > tune > wire > tune > discharge_assembly > tune > wire x2 > discharge_calibration > wire | 91.2 | 80.0 | 64.5 |
| 1-10 | mid gear | d_hd_pva1 (level 7) | boost > tune x2 > discharge_calibration > tune x3 > wire > tune > discharge_assembly > wire x2 > tune > wire x2 > discharge_calibration > wire x2 | 100.0 | 100.0 | 94.8 |
| 1-10 | mid gear | song_460 (level 8) | wire > tune > wire > tune > discharge_calibration > tune x4 > discharge_calibration > tune > wire x3 > discharge_calibration > discharge_assembly > wire x4 > discharge_assembly > wire | 99.3 | 99.3 | 88.0 |
| 1-10 | mid gear | d_lg_pvw1 (level 10) | wire x2 > tune x2 > discharge_calibration > tune x2 > patch > tune > discharge_calibration > discharge_assembly > wire > tune > patch > wire > discharge_calibration > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire x3 | 99.1 | 91.8 | 96.8 |
| 1-10 | mid gear | id_soulstrike1 (level 10) | wire > tune x4 > discharge_calibration > wire > tune x3 > discharge_assembly > wire x2 > tune > wire x2 > discharge_calibration > discharge_assembly > wire | 99.6 | 99.1 | 89.9 |
| 1-10 | best gear | wen_cft_smth1 (level 3) | tune x5 > discharge_calibration > tune x4 > wire > discharge_assembly > wire x3 > tune > discharge_assembly > wire | 100.0 | 99.5 | 85.0 |
| 1-10 | best gear | config_ind1 (level 5) | discharge_assembly > tune x3 > discharge_calibration > tune x3 > discharge_calibration > tune > wire x2 > discharge_calibration > wire > tune > wire > discharge_assembly > wire | 100.0 | 95.0 | 82.9 |
| 1-10 | best gear | d_hd_pva1 (level 7) | tune x5 > discharge_calibration > tune x3 > wire x2 > discharge_assembly > wire > tune > wire x3 | 100.0 | 100.0 | 91.3 |
| 1-10 | best gear | song_460 (level 8) | tune x4 > discharge_calibration > tune x3 > wire > discharge_calibration > wire x3 > discharge_calibration > wire x4 | 100.0 | 99.9 | 88.9 |
| 1-10 | best gear | d_lg_pvw1 (level 10) | tune x4 > discharge_calibration > tune > patch > tune x2 > discharge_calibration > tune > wire x2 > discharge_assembly > tune > wire x3 > discharge_assembly > tune > wire x2 | 100.0 | 99.6 | 95.0 |
| 1-10 | best gear | id_soulstrike1 (level 10) | tune x5 > discharge_calibration > tune x4 > wire > patch > discharge_assembly > tune > wire x3 > discharge_assembly > wire | 100.0 | 99.3 | 99.9 |
| 1-10 | fully modded gear | wen_cft_smth1 (level 3) | tune x5 > discharge_calibration > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | config_ind1 (level 5) | tune x3 > discharge_calibration > tune x3 > wire x5 | 100.0 | 100.0 | 99.7 |
| 1-10 | fully modded gear | d_hd_pva1 (level 7) | boost > tune x2 > discharge_calibration > tune x2 > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | song_460 (level 8) | tune x4 > discharge_calibration > tune x2 > wire x5 | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | d_lg_pvw1 (level 10) | tune x3 > discharge_calibration > tune x3 > wire x5 | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | id_soulstrike1 (level 10) | tune x5 > discharge_calibration > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | no gear | dhbr002 (level 13) | wire > tune x3 > discharge_calibration > wire x4 > discharge_calibration > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire | 69.2 | 69.2 | 60.9 |
| 11-20 | no gear | song_579 (level 15) | wire > tune > wire x2 > tune x2 > discharge_calibration > wire > discharge_calibration > wire x5 > discharge_calibration > wire > discharge_calibration > wire | 73.1 | 66.6 | 71.7 |
| 11-20 | no gear | eva_boost_1 (level 18) | wire > tune x3 > discharge_calibration > wire x3 > tune > discharge_calibration > wire > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire | 79.8 | 76.7 | 81.4 |
| 11-20 | no gear | d_ar_pvs2 (level 19) | wire > tune x4 > discharge_calibration > wire x5 > discharge_calibration > wire x4 > discharge_calibration > wire > discharge_calibration > wire | 87.8 | 83.8 | 89.9 |
| 11-20 | no gear | darg002 (level 20) | wire > tune x5 > discharge_calibration > wire x5 > discharge_calibration > wire x3 > discharge_calibration > wire > discharge_calibration > wire | 99.4 | 96.1 | 90.9 |
| 11-20 | no gear | id_pinningtoss1 (level 20) | tune x4 > discharge_calibration > wire x3 > tune > discharge_calibration > wire x3 > discharge_calibration > wire x4 > discharge_calibration > wire > discharge_calibration > wire | 87.9 | 90.6 | 70.4 |
| 11-20 | mid gear | dhbr002 (level 13) | tune > discharge_assembly > tune x4 > discharge_calibration > tune > patch > tune > discharge_calibration > wire x4 > discharge_calibration > wire x4 > discharge_calibration > wire x2 | 100.0 | 100.0 | 97.9 |
| 11-20 | mid gear | song_579 (level 15) | tune x6 > discharge_calibration > tune x4 > wire x2 > discharge_assembly > wire x5 | 100.0 | 100.0 | 86.4 |
| 11-20 | mid gear | eva_boost_1 (level 18) | tune x5 > discharge_calibration > tune x2 > wire > tune > patch > discharge_calibration > wire x5 > discharge_calibration > wire x3 > discharge_calibration > wire | 100.0 | 100.0 | 97.5 |
| 11-20 | mid gear | d_ar_pvs2 (level 19) | tune x5 > discharge_calibration > tune x3 > wire > tune > discharge_assembly > tune x2 > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 99.9 |
| 11-20 | mid gear | darg002 (level 20) | boost > tune x3 > discharge_calibration > tune x5 > wire > discharge_assembly > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 92.5 |
| 11-20 | mid gear | id_pinningtoss1 (level 20) | discharge_assembly > tune x4 > discharge_calibration > tune x3 > patch > tune > discharge_calibration > wire x5 > discharge_calibration > wire x4 > discharge_calibration > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | dhbr002 (level 13) | tune x4 > discharge_calibration > tune x4 > patch > discharge_calibration > wire x8 | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | song_579 (level 15) | boost > tune x3 > discharge_calibration > tune x5 > wire > discharge_assembly > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | eva_boost_1 (level 18) | tune x4 > discharge_calibration > tune x4 > discharge_calibration > wire x4 > discharge_assembly > wire > tune > wire x2 | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | d_ar_pvs2 (level 19) | tune x5 > discharge_calibration > tune x4 > wire > discharge_assembly > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | darg002 (level 20) | boost > tune x3 > discharge_calibration > tune x4 > wire x2 > discharge_assembly > tune > wire x3 | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | id_pinningtoss1 (level 20) | tune x4 > discharge_calibration > tune x4 > discharge_calibration > wire x4 > discharge_assembly > wire > tune > wire x2 | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | dhbr002 (level 13) | tune x4 > discharge_calibration > wire > tune > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | song_579 (level 15) | boost > tune x3 > discharge_calibration > boost > wire x2 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | eva_boost_1 (level 18) | tune x4 > discharge_calibration > tune x2 > wire x5 | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | d_ar_pvs2 (level 19) | boost > tune x2 > discharge_calibration > tune x2 > wire x4 | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | darg002 (level 20) | boost > tune x3 > discharge_calibration > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | id_pinningtoss1 (level 20) | tune x4 > discharge_calibration > tune x2 > wire x5 | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | wen_cft_smth3 (level 23) | boost > tune x4 > discharge_calibration > wire x7 > discharge_calibration > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire | 96.5 | 95.8 | 90.8 |
| 21-30 | no gear | ion_armor_3 (level 25) | boost > tune x3 > discharge_calibration > tune x6 > patch > discharge_assembly > tune x2 > wire x4 > discharge_assembly > tune > wire | 100.0 | 100.0 | 99.7 |
| 21-30 | no gear | d_lg_vws3 (level 27) | tune x5 > discharge_calibration > patch > tune x5 > wire > discharge_assembly > wire x3 > discharge_assembly > wire x2 > tune > discharge_calibration > wire | 100.0 | 100.0 | 99.8 |
| 21-30 | no gear | dlbe003 (level 29) | tune x6 > discharge_calibration > tune > wire > tune x3 > wire > discharge_assembly > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 97.3 |
| 21-30 | no gear | dsbr003 (level 30) | boost > tune x4 > discharge_calibration > tune x5 > wire x2 > discharge_assembly > wire x5 | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | id_linebreaker3 (level 30) | boost > tune x4 > discharge_calibration > tune > wire > tune x4 > wire > discharge_assembly > wire > tune > wire x4 > tune > wire | 100.0 | 100.0 | 99.8 |
| 21-30 | mid gear | wen_cft_smth3 (level 23) | boost > tune x4 > discharge_calibration > tune x4 > wire > tune > wire > discharge_assembly > wire > tune > wire x3 | 100.0 | 100.0 | 94.4 |
| 21-30 | mid gear | ion_armor_3 (level 25) | boost > tune x3 > discharge_calibration > tune x5 > wire > discharge_assembly > tune > wire x4 > tune > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | d_lg_vws3 (level 27) | boost > tune x4 > discharge_calibration > tune x3 > wire x3 > discharge_assembly > tune > wire x2 > patch > tune x2 > discharge_calibration > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | dlbe003 (level 29) | tune > wire > tune > patch > tune x3 > discharge_calibration > patch > tune x5 > wire x2 > discharge_assembly > wire x4 | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | dsbr003 (level 30) | boost > tune x4 > discharge_calibration > boost > tune x3 > wire > discharge_assembly > boost > wire x2 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | id_linebreaker3 (level 30) | boost > tune x4 > discharge_calibration > tune x4 > wire > tune > wire > discharge_assembly > wire > tune > wire x2 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | wen_cft_smth3 (level 23) | boost > tune x4 > discharge_calibration > tune x4 > wire x3 > discharge_assembly > wire x2 | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | ion_armor_3 (level 25) | boost > tune x3 > discharge_calibration > tune x5 > wire > discharge_assembly > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | d_lg_vws3 (level 27) | tune x6 > discharge_calibration > tune x2 > wire x4 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | dlbe003 (level 29) | boost > tune x3 > discharge_calibration > tune x4 > wire x2 > discharge_assembly > tune > wire x3 | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | dsbr003 (level 30) | boost > tune x4 > discharge_calibration > tune x2 > wire x4 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | id_linebreaker3 (level 30) | boost > tune x4 > discharge_calibration > tune x4 > wire x3 > discharge_assembly > wire x2 | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | wen_cft_smth3 (level 23) | boost > tune x3 > discharge_calibration > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | ion_armor_3 (level 25) | boost > tune x3 > discharge_calibration > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | d_lg_vws3 (level 27) | boost > tune x3 > discharge_calibration > boost > wire x2 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | dlbe003 (level 29) | boost > tune x3 > discharge_calibration > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | dsbr003 (level 30) | boost > tune x3 > discharge_calibration > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | id_linebreaker3 (level 30) | boost > tune x3 > discharge_calibration > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | aen_cft_fab4 (level 33) | tune x6 > discharge_calibration > tune x6 > patch > discharge_calibration > wire x6 > discharge_assembly > wire x2 | 100.0 | 99.8 | 99.5 |
| 31-40 | no gear | config_ind4 (level 35) | boost > tune x4 > discharge_calibration > tune x5 > wire > tune > discharge_assembly > tune x3 > wire x4 > discharge_calibration > wire x2 | 100.0 | 99.2 | 95.2 |
| 31-40 | no gear | d_lg_mws4 (level 37) | boost > tune x2 > discharge_calibration > tune x4 > wire > tune > patch > discharge_calibration > tune > wire > tune > wire x3 > discharge_calibration > wire x4 > discharge_calibration > wire | 100.0 | 98.2 | 84.3 |
| 31-40 | no gear | d_hd_mwa4 (level 39) | boost > tune x3 > discharge_calibration > patch > discharge_assembly > tune x3 > discharge_calibration > wire > discharge_assembly > wire x4 > tune x2 > discharge_calibration x2 > wire x4 > discharge_calibration > wire x2 | 98.6 | 97.5 | 88.6 |
| 31-40 | no gear | dfhl004 (level 40) | wire > boost > tune x2 > discharge_calibration > tune x3 > wire > patch > tune x2 > patch > discharge_calibration > wire x2 > tune > wire > tune > wire > discharge_calibration > wire x3 > discharge_calibration > wire x2 | 100.0 | 92.6 | 89.8 |
| 31-40 | no gear | id_conctoss2 (level 40) | tune x8 > patch > discharge_calibration > wire x3 > patch > wire x2 > tune x2 > wire > tune > discharge_calibration > wire x4 > discharge_calibration > wire x2 | 98.5 | 81.0 | 60.4 |
| 31-40 | mid gear | aen_cft_fab4 (level 33) | boost > tune x3 > discharge_calibration > tune x5 > discharge_calibration > tune > wire x5 > discharge_assembly > tune > wire x2 | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | config_ind4 (level 35) | boost > tune x4 > discharge_calibration > boost > tune x3 > discharge_calibration > wire x2 > tune > wire x3 > discharge_assembly > tune > wire x2 | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | d_lg_mws4 (level 37) | boost > tune x3 > discharge_calibration > tune x4 > patch > discharge_calibration > wire > tune x4 > wire > discharge_assembly > wire x4 > discharge_calibration > wire | 100.0 | 100.0 | 99.8 |
| 31-40 | mid gear | d_hd_mwa4 (level 39) | boost > tune x3 > discharge_calibration > wire > tune x5 > patch > discharge_calibration > tune x2 > wire x4 > discharge_assembly > tune x2 > wire | 100.0 | 100.0 | 98.4 |
| 31-40 | mid gear | dfhl004 (level 40) | boost > tune x3 > discharge_calibration > tune > wire > tune x4 > patch > discharge_calibration > tune x3 > wire x2 > discharge_assembly > wire x3 > tune > wire | 100.0 | 100.0 | 92.5 |
| 31-40 | mid gear | id_conctoss2 (level 40) | boost > tune x4 > discharge_calibration > tune > patch > tune x7 > discharge_assembly > tune > wire x2 > tune > wire x3 > discharge_calibration > wire | 100.0 | 100.0 | 76.3 |
| 31-40 | best gear | aen_cft_fab4 (level 33) | boost > tune x3 > discharge_calibration > tune x4 > discharge_calibration > tune x2 > wire x4 > discharge_assembly > wire x2 | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | config_ind4 (level 35) | boost > tune x4 > discharge_calibration > boost > tune x3 > discharge_calibration > boost > wire x4 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | d_lg_mws4 (level 37) | tune x6 > discharge_calibration > tune x5 > wire > discharge_assembly > wire x3 > discharge_calibration > wire x2 | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | d_hd_mwa4 (level 39) | tune x6 > discharge_calibration > tune x5 > wire > discharge_assembly > tune > wire x3 > discharge_calibration > wire | 100.0 | 100.0 | 93.3 |
| 31-40 | best gear | dfhl004 (level 40) | boost > tune x4 > discharge_calibration > tune x4 > patch > tune x2 > discharge_assembly > tune > wire > tune > wire x3 > patch > discharge_calibration > tune > wire | 100.0 | 100.0 | 99.0 |
| 31-40 | best gear | id_conctoss2 (level 40) | boost > tune x4 > discharge_calibration > tune x2 > wire > tune x5 > patch > discharge_assembly > tune x2 > wire x3 > discharge_calibration > wire | 100.0 | 100.0 | 99.9 |
| 31-40 | fully modded gear | aen_cft_fab4 (level 33) | boost > tune x3 > discharge_calibration > tune x2 > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | config_ind4 (level 35) | boost > tune x4 > discharge_calibration > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | d_lg_mws4 (level 37) | boost > tune x3 > discharge_calibration > tune x3 > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | d_hd_mwa4 (level 39) | boost > tune x3 > discharge_calibration > tune x3 > wire x2 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | dfhl004 (level 40) | boost > tune x3 > discharge_calibration > tune x4 > wire x2 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | id_conctoss2 (level 40) | boost > tune x4 > discharge_calibration > tune > wire > tune x2 > wire > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | dhbr005 (level 43) | boost x2 > tune x2 > discharge_calibration > patch > boost x2 > tune > discharge_calibration > boost x2 > wire > patch > discharge_calibration > wire x7 > discharge_calibration > wire | 86.1 | 78.4 | 74.6 |
| 41-53 | no gear | therm_amp_4 (level 46) | boost > tune x5 > patch > discharge_calibration > boost > wire x3 > tune > wire > discharge_calibration > wire x4 > discharge_calibration > wire | 77.7 | 66.6 | 50.1 |
| 41-53 | no gear | wen_eva5 (level 48) | boost > tune x3 > patch > wire > discharge_calibration > wire x2 > boost > tune > discharge_calibration > discharge_assembly > wire x3 > tune > wire x3 > discharge_calibration > wire | 56.2 | 50.8 | 40.6 |
| 41-53 | no gear | d_lg_mwa5 (level 49) | wire > boost > tune x3 > patch > wire > discharge_calibration > boost x2 > wire > tune > discharge_calibration > boost > wire x4 > discharge_calibration > wire x2 | 75.2 | 64.2 | 45.6 |
| 41-53 | no gear | id_infusion2 (level 50) | boost x2 > wire > discharge_calibration > patch > tune > patch > tune x4 > patch > discharge_assembly > wire > tune > wire > tune > wire x4 > discharge_calibration > wire x2 > discharge_calibration > wire | 47.1 | 49.7 | 41.4 |
| 41-53 | no gear | cap_quadlas1 (level 52) | boost x2 > wire > tune x2 > discharge_calibration > patch > discharge_calibration > boost x2 > wire x2 > ground > wire x3 > discharge_calibration > boost > wire x2 > discharge_calibration > wire | 40.8 | 42.0 | 36.4 |
| 41-53 | mid gear | dhbr005 (level 43) | discharge_assembly > boost > wire > tune x3 > discharge_calibration > tune > discharge_calibration > tune x3 > patch > tune x2 > patch > discharge_assembly > wire x2 > tune x2 > wire > tune > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire x2 | 99.3 | 94.5 | 85.1 |
| 41-53 | mid gear | therm_amp_4 (level 46) | discharge_assembly > boost > tune x4 > patch > tune > discharge_calibration > wire > tune x2 > wire > tune > wire > tune x2 > discharge_assembly > boost > wire > discharge_calibration > wire | 89.4 | 85.5 | 61.5 |
| 41-53 | mid gear | wen_eva5 (level 48) | tune x5 > patch > tune x2 > discharge_assembly x2 > tune > wire x2 > tune x2 > wire > tune > discharge_calibration > wire x4 | 62.2 | 61.3 | 34.8 |
| 41-53 | mid gear | d_lg_mwa5 (level 49) | wire > tune x6 > patch > wire > discharge_calibration > boost > wire x2 > tune x3 > discharge_calibration > discharge_assembly > wire | 93.1 | 81.5 | 65.4 |
| 41-53 | mid gear | id_infusion2 (level 50) | wire x2 > tune x5 > patch > tune > patch > discharge_calibration > wire > tune x3 > wire > tune x2 > discharge_assembly > wire x3 > tune > discharge_assembly | 63.2 | 55.3 | 40.6 |
| 41-53 | mid gear | cap_quadlas1 (level 52) | wire > tune x3 > patch > tune x4 > patch > discharge_assembly > boost > wire x4 > tune > discharge_calibration > boost > discharge_calibration > wire | 55.5 | 48.4 | 45.5 |
| 41-53 | best gear | dhbr005 (level 43) | boost > tune x4 > discharge_calibration > tune x7 > patch > discharge_assembly > tune x2 > wire x3 > discharge_calibration > wire x2 | 100.0 | 97.4 | 96.9 |
| 41-53 | best gear | therm_amp_4 (level 46) | boost x2 > tune x2 > discharge_calibration x2 > tune x8 > patch > discharge_assembly > wire > tune x3 > wire > tune > discharge_assembly | 99.0 | 98.7 | 78.8 |
| 41-53 | best gear | wen_eva5 (level 48) | discharge_assembly > tune x4 > patch > tune x3 > wire > discharge_calibration > tune x6 > wire > discharge_assembly > wire x3 | 82.7 | 82.5 | 66.9 |
| 41-53 | best gear | d_lg_mwa5 (level 49) | boost > tune x3 > patch > tune x2 > discharge_calibration > tune x6 > wire > tune > discharge_assembly x2 > tune > wire > tune > wire | 99.9 | 95.6 | 87.1 |
| 41-53 | best gear | id_infusion2 (level 50) | tune x6 > patch > tune > discharge_calibration > wire > tune > wire > tune x2 > wire > tune > discharge_assembly > wire > tune x2 > wire | 80.1 | 72.1 | 68.1 |
| 41-53 | best gear | cap_quadlas1 (level 52) | tune x5 > patch > tune x2 > patch > tune > patch > discharge_assembly > wire x3 > tune x2 > wire > tune x2 > discharge_calibration > wire x3 | 75.4 | 69.2 | 61.4 |
| 41-53 | fully modded gear | dhbr005 (level 43) | boost > tune x4 > discharge_calibration > tune x3 > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | fully modded gear | therm_amp_4 (level 46) | boost > tune x5 > discharge_calibration > tune x2 > wire x4 | 100.0 | 100.0 | 100.0 |
| 41-53 | fully modded gear | wen_eva5 (level 48) | boost > tune x4 > discharge_calibration > boost > tune x4 > discharge_assembly > tune > wire x2 > patch > tune > wire | 100.0 | 100.0 | 97.5 |
| 41-53 | fully modded gear | d_lg_mwa5 (level 49) | boost > tune x5 > discharge_calibration > tune x2 > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | fully modded gear | id_infusion2 (level 50) | boost > tune x4 > discharge_calibration > tune x5 > wire > discharge_assembly > tune > wire > patch > tune > wire | 100.0 | 100.0 | 93.5 |
| 41-53 | fully modded gear | cap_quadlas1 (level 52) | boost > tune x5 > discharge_calibration > boost > tune x4 > discharge_assembly > wire > boost > patch > wire > discharge_calibration > wire | 100.0 | 100.0 | 93.4 |

## Style balance

### Style pairs: how often each style is better, and the average quality (quality points; ties count half a win each)

| Style pair | Gear | Recipes | First better | Second better | First avg quality | Second avg quality | Difference | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Overclock (no lines) vs Safe Mode (no lines) | no gear | 20 | 45% | 55% | 81.6 | 81.6 | +0.1 | Passed |
| Overclock (no lines) vs Safe Mode (no lines) | mid gear | 20 | 25% | 75% | 88.3 | 93.7 | -5.3 | Failed |
| Overclock (no lines) vs Safe Mode (no lines) | best gear | 20 | 35% | 65% | 96.1 | 98.4 | -2.3 | Passed |
| Overclock (no lines) vs Safe Mode (no lines) | fully modded gear | 20 | 45% | 55% | 99.8 | 100.0 | -0.2 | Passed |
| Overclock (both lines) vs Safe Mode (both lines) | no gear | 20 | 55% | 45% | 92.5 | 93.0 | -0.5 | Passed |
| Overclock (both lines) vs Safe Mode (both lines) | mid gear | 20 | 52% | 48% | 97.8 | 94.8 | +3.0 | Passed |
| Overclock (both lines) vs Safe Mode (both lines) | best gear | 20 | 50% | 50% | 99.5 | 99.8 | -0.3 | Passed |
| Overclock (both lines) vs Safe Mode (both lines) | fully modded gear | 20 | 48% | 52% | 99.6 | 100.0 | -0.4 | Passed |

## Perks optional

### Rank 50, fully modded gear, no perks: adaptive attempts that reach maximum quality, by recipe band

| Band | Recipes played | Attempts | Reach maximum quality | Make the item | Avg quality of items made | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 6 | 240 | 95% | 100% | 100% | >= 50% | Passed |
| 46-50 | 6 | 240 | 93% | 100% | 100% | >= 50% | Passed |
| 51-53 | 6 | 240 | 79% | 98% | 97% | >= 35% | Passed |

## Length

### Actions per attempt of the adaptive player at level (item craft at level)

| Band | Gear | Recipes played | Mean actions per attempt | Shortest recipe | Longest recipe | Stalled attempts | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 14.5 | 12.2 | 16.2 | 0 | 10-20 | Passed |
| 1-10 | mid gear | 6 | 17.6 | 15.9 | 18.6 | 0 | 10-20 | Passed |
| 1-10 | best gear | 6 | 15.8 | 14.0 | 17.6 | 0 | 10-20 | Passed |
| 1-10 | fully modded gear | 6 | 10.1 | 9.1 | 10.5 | 0 | 10-20 | Reported |
| 11-20 | no gear | 6 | 16.6 | 14.9 | 18.4 | 0 | 10-20 | Passed |
| 11-20 | mid gear | 6 | 17.2 | 14.9 | 19.2 | 0 | 10-20 | Passed |
| 11-20 | best gear | 6 | 14.9 | 12.8 | 16.8 | 0 | 10-20 | Passed |
| 11-20 | fully modded gear | 6 | 9.6 | 8.8 | 10.3 | 0 | 10-20 | Reported |
| 21-30 | no gear | 6 | 17.8 | 15.9 | 19.2 | 0 | 10-20 | Passed |
| 21-30 | mid gear | 6 | 15.9 | 14.5 | 17.1 | 0 | 10-20 | Passed |
| 21-30 | best gear | 6 | 14.1 | 13.0 | 15.1 | 0 | 10-20 | Passed |
| 21-30 | fully modded gear | 6 | 9.1 | 8.2 | 9.7 | 0 | 10-20 | Reported |
| 31-40 | no gear | 6 | 21.5 | 21.0 | 21.9 | 0 | 10-20 | Reported |
| 31-40 | mid gear | 6 | 19.7 | 18.2 | 22.1 | 0 | 10-20 | Passed |
| 31-40 | best gear | 6 | 17.1 | 15.4 | 19.4 | 0 | 10-20 | Passed |
| 31-40 | fully modded gear | 6 | 10.8 | 10.3 | 12.0 | 0 | 10-20 | Reported |
| 41-53 | no gear | 6 | 19.7 | 18.0 | 21.4 | 0 | 10-20 | Reported |
| 41-53 | mid gear | 6 | 20.0 | 19.4 | 20.9 | 0 | 10-20 | Reported |
| 41-53 | best gear | 6 | 19.8 | 18.6 | 21.6 | 0 | 10-20 | Passed |
| 41-53 | fully modded gear | 6 | 13.0 | 11.6 | 14.5 | 0 | 10-20 | Reported |

## Economy parity

### Component stacks consumed per item made, failures included, over the recipes the legacy policy makes at least once in the cell (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Parity rule | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 2.35 | 2.38 | 0.99 | 99% | 97% | 47% | 1% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | mid gear | 6 | 2.37 | 2.38 | 1.00 | 98% | 97% | 87% | 10% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | best gear | 6 | 2.44 | 2.37 | 1.03 | 93% | 98% | 90% | 24% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | fully modded gear | 6 | 2.33 | 2.34 | 1.00 | 100% | 100% | 100% | 75% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | no gear | 6 | 2.01 | 2.09 | 0.96 | 99% | 94% | 79% | 3% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | mid gear | 6 | 2.04 | 2.01 | 1.01 | 98% | 99% | 95% | 13% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | best gear | 6 | 2.00 | 2.02 | 0.99 | 100% | 98% | 100% | 28% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | fully modded gear | 6 | 2.00 | 2.01 | 1.00 | 100% | 99% | 100% | 91% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | no gear | 6 | 2.51 | 2.55 | 0.98 | 99% | 97% | 98% | 8% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | mid gear | 6 | 2.51 | 2.54 | 0.99 | 100% | 98% | 99% | 15% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | best gear | 6 | 2.50 | 2.51 | 0.99 | 100% | 99% | 100% | 29% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | fully modded gear | 6 | 2.50 | 2.51 | 1.00 | 100% | 99% | 100% | 94% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | no gear | 6 | 2.60 | 2.52 | 1.03 | 94% | 99% | 86% | 23% | yes | 0% / 0% | within 10% | Reported |
| 31-40 | mid gear | 6 | 2.56 | 2.53 | 1.01 | 96% | 98% | 94% | 35% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | best gear | 6 | 2.52 | 2.52 | 1.00 | 99% | 99% | 99% | 49% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | fully modded gear | 6 | 2.50 | 2.53 | 0.99 | 100% | 98% | 100% | 85% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | no gear | 6 | 2.62 | 2.41 | 1.09 | 88% | 99% | 50% | 42% | yes | 0% / 17% | within 10% | Reported |
| 41-53 | mid gear | 6 | 2.78 | 2.73 | 1.02 | 85% | 86% | 55% | 55% | yes | 0% / 0% | at most +10% | Reported |
| 41-53 | best gear | 6 | 2.69 | 2.58 | 1.04 | 89% | 95% | 77% | 76% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | fully modded gear | 6 | 2.52 | 2.54 | 0.99 | 98% | 98% | 97% | 88% | yes | 0% / 0% | within 10% | Passed |

### Recipes the legacy policy never makes in the cell, left out of the stack and quality comparison above (they still count in the never-made check)

| Band | Gear | Recipe | Level | New success | New avg quality |
| --- | --- | --- | --- | --- | --- |
| 41-53 | no gear | QuadLaserCannon1 | 52 | 92% | 36% |

## XP rate

### Item crafts: XP per hour against the legacy loop (6.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per craft | Legacy actions per craft | Legacy XP per hour | New XP per craft | New actions per craft | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 591 | 5.8 | 61669 | 2003 | 14.5 | 62334 | 1.01 | - | Passed |
| 1-10 | no gear, 1 enhancement | 425 | 6.4 | 39684 | 1711 | 18.3 | 42143 | 1.06 | - | Passed |
| 1-10 | no gear, 2 enhancements | 181 | 6.0 | 18138 | 1664 | 17.9 | 41716 | 2.30 | - | Passed |
| 1-10 | no gear, 3 enhancements | 90 | 6.0 | 9000 | 1688 | 18.3 | 41411 | 4.60 | - | Passed |
| 1-10 | mid gear | 647 | 7.5 | 51461 | 2538 | 17.6 | 65046 | 1.26 | - | Passed |
| 1-10 | mid gear, 1 enhancement | 610 | 7.9 | 46202 | 1975 | 18.8 | 47336 | 1.02 | - | Passed |
| 1-10 | mid gear, 2 enhancements | 583 | 8.4 | 41565 | 2020 | 18.8 | 48300 | 1.16 | - | Passed |
| 1-10 | mid gear, 3 enhancements | 532 | 8.9 | 35784 | 1893 | 18.8 | 45425 | 1.27 | - | Passed |
| 1-10 | best gear | 733 | 8.8 | 50091 | 2540 | 15.8 | 72176 | 1.44 | - | Passed |
| 1-10 | best gear, 1 enhancement | 723 | 9.2 | 47336 | 2266 | 18.3 | 55642 | 1.18 | - | Passed |
| 1-10 | best gear, 2 enhancements | 719 | 9.6 | 45095 | 2377 | 18.4 | 58279 | 1.29 | - | Passed |
| 1-10 | best gear, 3 enhancements | 685 | 10.0 | 40977 | 2336 | 18.3 | 57419 | 1.40 | - | Passed |
| 1-10 | fully modded gear | 1049 | 7.6 | 83082 | 2747 | 10.1 | 122961 | 1.48 | - | Passed |
| 1-10 | fully modded gear, 1 enhancement | 1030 | 7.8 | 79730 | 2705 | 11.0 | 110374 | 1.38 | - | Passed |
| 1-10 | fully modded gear, 2 enhancements | 1024 | 7.9 | 77666 | 2735 | 11.0 | 112099 | 1.44 | - | Passed |
| 1-10 | fully modded gear, 3 enhancements | 1018 | 8.5 | 71869 | 2715 | 11.1 | 109784 | 1.53 | - | Passed |
| 11-20 | no gear | 582 | 7.9 | 44282 | 1633 | 16.6 | 44325 | 1.00 | - | Passed |
| 11-20 | no gear, 1 enhancement | 404 | 8.7 | 27936 | 1709 | 19.4 | 39720 | 1.42 | - | Passed |
| 11-20 | no gear, 2 enhancements | 265 | 7.3 | 21871 | 1673 | 19.2 | 39130 | 1.79 | - | Passed |
| 11-20 | no gear, 3 enhancements | 254 | 8.0 | 19022 | 1673 | 19.3 | 38957 | 2.05 | - | Passed |
| 11-20 | mid gear | 667 | 10.3 | 38960 | 1795 | 17.2 | 47027 | 1.21 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 641 | 11.6 | 33075 | 1799 | 18.7 | 43293 | 1.31 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 618 | 12.8 | 29056 | 1790 | 18.8 | 42931 | 1.48 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 552 | 13.9 | 23844 | 1783 | 18.7 | 42850 | 1.80 | - | Passed |
| 11-20 | best gear | 761 | 14.6 | 31276 | 1848 | 14.9 | 55825 | 1.78 | - | Passed |
| 11-20 | best gear, 1 enhancement | 753 | 15.3 | 29430 | 1841 | 16.3 | 50898 | 1.73 | - | Passed |
| 11-20 | best gear, 2 enhancements | 743 | 16.1 | 27754 | 1848 | 16.5 | 50271 | 1.81 | - | Passed |
| 11-20 | best gear, 3 enhancements | 718 | 16.6 | 25943 | 1848 | 16.3 | 50945 | 1.96 | - | Passed |
| 11-20 | fully modded gear | 1147 | 14.5 | 47545 | 1848 | 9.6 | 86769 | 1.83 | - | Passed |
| 11-20 | fully modded gear, 1 enhancement | 1150 | 14.6 | 47335 | 1848 | 10.2 | 81861 | 1.73 | - | Passed |
| 11-20 | fully modded gear, 2 enhancements | 1134 | 14.7 | 46248 | 1848 | 10.1 | 82609 | 1.79 | - | Passed |
| 11-20 | fully modded gear, 3 enhancements | 1134 | 14.9 | 45771 | 1848 | 10.3 | 81031 | 1.77 | - | Passed |
| 21-30 | no gear | 632 | 11.3 | 33506 | 1361 | 17.8 | 34479 | 1.03 | - | Passed |
| 21-30 | no gear, 1 enhancement | 617 | 12.3 | 30083 | 1361 | 19.0 | 32222 | 1.07 | - | Passed |
| 21-30 | no gear, 2 enhancements | 610 | 13.1 | 27854 | 1354 | 19.3 | 31620 | 1.14 | - | Passed |
| 21-30 | no gear, 3 enhancements | 560 | 14.0 | 24021 | 1361 | 19.3 | 31785 | 1.32 | - | Passed |
| 21-30 | mid gear | 681 | 16.8 | 24313 | 1371 | 15.9 | 38895 | 1.60 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 680 | 17.5 | 23353 | 1380 | 16.7 | 37086 | 1.59 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 671 | 17.6 | 22839 | 1375 | 16.6 | 37377 | 1.64 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 651 | 17.9 | 21793 | 1372 | 17.1 | 36197 | 1.66 | - | Passed |
| 21-30 | best gear | 772 | 22.6 | 20472 | 1380 | 14.1 | 43962 | 2.15 | - | Passed |
| 21-30 | best gear, 1 enhancement | 762 | 22.7 | 20179 | 1380 | 14.6 | 42545 | 2.11 | - | Passed |
| 21-30 | best gear, 2 enhancements | 753 | 23.3 | 19422 | 1380 | 14.6 | 42631 | 2.20 | - | Passed |
| 21-30 | best gear, 3 enhancements | 742 | 23.2 | 19198 | 1380 | 14.6 | 42582 | 2.22 | - | Passed |
| 21-30 | fully modded gear | 1157 | 22.2 | 31305 | 1380 | 9.1 | 68429 | 2.19 | - | Passed |
| 21-30 | fully modded gear, 1 enhancement | 1120 | 22.6 | 29717 | 1380 | 9.6 | 64912 | 2.18 | - | Passed |
| 21-30 | fully modded gear, 2 enhancements | 1129 | 22.7 | 29823 | 1380 | 9.7 | 64291 | 2.16 | - | Passed |
| 21-30 | fully modded gear, 3 enhancements | 1119 | 22.7 | 29536 | 1380 | 9.6 | 64547 | 2.19 | - | Passed |
| 31-40 | no gear | 730 | 14.6 | 29917 | 1088 | 21.5 | 22801 | 0.76 | - | Passed |
| 31-40 | no gear, 1 enhancement | 680 | 14.5 | 28234 | 1112 | 23.0 | 21802 | 0.77 | - | Passed |
| 31-40 | no gear, 2 enhancements | 621 | 15.1 | 24664 | 1095 | 22.7 | 21744 | 0.88 | - | Passed |
| 31-40 | no gear, 3 enhancements | 587 | 16.1 | 21896 | 1080 | 22.7 | 21406 | 0.98 | - | Passed |
| 31-40 | mid gear | 799 | 18.8 | 25458 | 1148 | 19.7 | 26189 | 1.03 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 785 | 19.5 | 24189 | 1168 | 20.4 | 25727 | 1.06 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 736 | 19.2 | 23044 | 1178 | 20.7 | 25636 | 1.11 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 675 | 18.3 | 22081 | 1161 | 20.7 | 25215 | 1.14 | - | Passed |
| 31-40 | best gear | 885 | 24.4 | 21781 | 1188 | 17.1 | 31331 | 1.44 | - | Passed |
| 31-40 | best gear, 1 enhancement | 883 | 25.3 | 20967 | 1199 | 17.7 | 30430 | 1.45 | - | Passed |
| 31-40 | best gear, 2 enhancements | 850 | 25.3 | 20199 | 1198 | 17.9 | 30147 | 1.49 | - | Passed |
| 31-40 | best gear, 3 enhancements | 818 | 23.7 | 20734 | 1180 | 17.7 | 29923 | 1.44 | - | Passed |
| 31-40 | fully modded gear | 1102 | 25.3 | 26092 | 1200 | 10.8 | 49978 | 1.92 | - | Passed |
| 31-40 | fully modded gear, 1 enhancement | 1113 | 25.5 | 26178 | 1200 | 11.0 | 49033 | 1.87 | - | Passed |
| 31-40 | fully modded gear, 2 enhancements | 1114 | 26.3 | 25444 | 1200 | 11.1 | 48630 | 1.91 | - | Passed |
| 31-40 | fully modded gear, 3 enhancements | 1110 | 26.6 | 24991 | 1200 | 11.1 | 48484 | 1.94 | - | Passed |
| 41-53 | no gear | 725 | 17.6 | 24733 | 863 | 19.7 | 19705 | 0.80 | - | Passed |
| 41-53 | no gear, 1 enhancement | 636 | 16.1 | 23754 | 706 | 19.8 | 16031 | 0.67 | - | Passed |
| 41-53 | no gear, 2 enhancements | 526 | 16.5 | 19097 | 696 | 20.0 | 15676 | 0.82 | - | Passed |
| 41-53 | no gear, 3 enhancements | 455 | 17.4 | 15666 | 686 | 19.8 | 15571 | 0.99 | - | Passed |
| 41-53 | mid gear | 865 | 24.0 | 21652 | 890 | 20.0 | 19990 | 0.92 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 785 | 22.4 | 21033 | 907 | 21.1 | 19300 | 0.92 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 681 | 19.9 | 20502 | 912 | 20.9 | 19679 | 0.96 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 587 | 19.2 | 18315 | 854 | 20.8 | 18435 | 1.01 | - | Passed |
| 41-53 | best gear | 1045 | 28.3 | 22142 | 1043 | 19.8 | 23651 | 1.07 | - | Passed |
| 41-53 | best gear, 1 enhancement | 968 | 27.8 | 20857 | 987 | 20.5 | 21681 | 1.04 | - | Passed |
| 41-53 | best gear, 2 enhancements | 886 | 26.2 | 20271 | 1054 | 20.8 | 22830 | 1.13 | - | Passed |
| 41-53 | best gear, 3 enhancements | 777 | 23.3 | 20017 | 994 | 20.8 | 21447 | 1.07 | - | Passed |
| 41-53 | fully modded gear | 1153 | 27.7 | 24958 | 1224 | 13.1 | 42194 | 1.69 | - | Passed |
| 41-53 | fully modded gear, 1 enhancement | 1163 | 28.1 | 24796 | 1232 | 13.2 | 41923 | 1.69 | - | Passed |
| 41-53 | fully modded gear, 2 enhancements | 1153 | 28.9 | 23896 | 1238 | 13.2 | 42168 | 1.76 | - | Passed |
| 41-53 | fully modded gear, 3 enhancements | 1162 | 29.1 | 23967 | 1239 | 13.3 | 42012 | 1.75 | - | Passed |

## Enhancement parity

### Rank 50, fully modded gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 100%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 6 | 60-100 | 34% | 99% | 100% | 0.99 | 100% | 100.0% | 1.00 (+/- 0.00) | 1.01 | >= 0.95 | Passed |
| 41-45 | 2 | 6 | 60-100 | 69% | 95% | 100% | 1.89 | 100% | 100.0% | 2.00 (+/- 0.00) | 1.06 | >= 0.95 | Passed |
| 41-45 | 3 | 6 | 60-100 | 103% | 99% | 99% | 2.93 | 100% | 100.0% | 2.99 (+/- 0.01) | 1.02 | >= 0.90 | Passed |
| 46-50 | 1 | 6 | 60-100 | 39% | 99% | 99% | 0.99 | 100% | 99.8% | 1.00 (+/- 0.00) | 1.01 | >= 0.95 | Passed |
| 46-50 | 2 | 6 | 60-100 | 79% | 98% | 99% | 1.95 | 99% | 99.9% | 1.98 (+/- 0.01) | 1.01 | >= 0.95 | Passed |
| 46-50 | 3 | 6 | 60-100 | 118% | 100% | 99% | 2.98 | 99% | 99.9% | 2.93 (+/- 0.02) | 0.98 | >= 0.90 | Passed |
| 51-53 | 1 | 6 | 60-70 | 7% | 98% | 38% | 0.37 | 100% | 96.0% | 1.00 (+/- 0.00) | 2.70 | >= 0.95 | Passed |
| 51-53 | 2 | 6 | 60-70 | 15% | 99% | 39% | 0.77 | 98% | 95.8% | 1.83 (+/- 0.03) | 2.40 | >= 0.95 | Passed |
| 51-53 | 3 | 6 | 60-70 | 22% | 98% | 34% | 0.98 | 99% | 94.5% | 2.57 (+/- 0.05) | 2.62 | >= 0.90 | Passed |

### Thresholds, first / second / third slot, that would meet parity for the quality measured (the strictest set that keeps every case at least at today's, never falling from one slot to the next)

| Scope | Thresholds that meet parity | In steps of 5 | Thresholds in force |
| --- | --- | --- | --- |
| Every band together (the shared thresholds) | none | none | 50% / 85% / 100% |
| Band 41-45 alone | 99% / 100% / 100% | 95% / 100% / 100% | 50% / 85% / 100% |
| Band 46-50 alone | none | none | 50% / 85% / 100% |
| Band 51-53 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 100% |

### Rank 50, fully modded gear: quality of the items the new minigame made, in whole percent, by band and slots filled

| Band | Slots filled | Items made | 10th percentile | 25th percentile | Median | 75th percentile | 90th percentile | Made at 100% |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 41-45 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 99% |
| 41-45 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 99% |
| 46-50 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 97% |
| 46-50 | 2 | 99% | 100% | 100% | 100% | 100% | 100% | 97% |
| 46-50 | 3 | 99% | 100% | 100% | 100% | 100% | 100% | 95% |
| 51-53 | 1 | 100% | 80% | 100% | 100% | 100% | 100% | 79% |
| 51-53 | 2 | 98% | 79% | 100% | 100% | 100% | 100% | 78% |
| 51-53 | 3 | 99% | 73% | 100% | 100% | 100% | 100% | 75% |

### Representative enhancements: for each recipe kind the real item at the best level the recipe accepts (the old window refuses one more than 5 levels above the recipe) whose penalty is the lower median of that level

| Recipe kind | Enhancement | Level | Progress penalty | Items of that kind and level (min / median / max penalty) | Recipes |
| --- | --- | --- | --- | --- | --- |
| Weapon | Improved Agrinium V (`imp_agrinium_5`) | 45 (tier V) | 70 | 33 items: 50 / 70.0 / 80 | 1 |
| Armor | Armor Enhancement - Force Attack V (`aen_frcatk5`) | 45 (tier V) | 60 | 50 items: 50 / 60.0 / 150 | 4 |
| Starship | Starship Enhancement - Shield Recharge Rate V (`sen_shrech5`) | 45 (tier V) | 60 | 24 items: 50 / 65.0 / 80 | 2 |
| Module | Module Enhancement - Module Bonus V (`men_mod5`) | 45 (tier V) | 60 | 2 items: 60 / 70.0 / 80 | 8 |
| Droid | Droid Enhancement - Resistance - Ice II (`de_res_ice2`) | 50 (tier VI) | 100 | 8 items: 100 / 100.0 / 100 | 3 |

## Gear ladder

### Rank 50, 2 kits, no perks: what each step of gear buys

| Band | Recipes | Kind | Gear | Avg quality | Reach 85% | Reach 100% | Actions | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 46-50 | 6 | all | no gear | 40.7 | 0% | 0% | 20.0 | Passed |
| 46-50 | 6 | all | mid gear | 58.1 | 8% | 0% | 20.2 | Passed |
| 46-50 | 6 | all | best gear | 78.8 | 64% | 46% | 20.4 | Passed |
| 46-50 | 6 | all | fully modded gear | 99.1 | 99% | 97% | 13.1 | Passed |
| 51-53 | 6 | all | no gear | 15.6 | 0% | 0% | 18.7 | Passed |
| 51-53 | 6 | all | mid gear | 36.1 | 0% | 0% | 20.5 | Passed |
| 51-53 | 6 | all | best gear | 49.4 | 2% | 0% | 20.4 | Passed |
| 51-53 | 6 | all | fully modded gear | 93.9 | 85% | 78% | 15.5 | Passed |

### Perks do not replace gear: plain crafts, average quality

| Band | Style | Fully perked, no gear | No perks, unmodded best gear | Allowance | Verdict |
| --- | --- | --- | --- | --- | --- |
| 46-50 | Overclock (both lines) | 83.0 | 78.5 | +5 | Passed |
| 46-50 | Safe Mode (both lines) | 82.8 | 78.5 | +5 | Passed |
| 51-53 | Overclock (both lines) | 63.5 | 64.3 | +5 | Passed |
| 51-53 | Safe Mode (both lines) | 59.9 | 64.3 | +5 | Passed |

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
