# Crafting revamp: balance gates of the Circuit (Overclocker line, Overclock) minigame

- Command: `SWLOR.CraftSim gates --scenario circuit-overclocker --gates rotation,length,materials,xp,enhancements --runs 30 --seed 1 --recipes-per-band 6 --repo . --out design/testing/crafting-revamp/circuit-overclocker-gates.md`
- Seed 1; 30 runs per case.

## Inputs

- Scenario: Circuit (Overclocker line, Overclock) (`circuit-overclocker`), Engineering recipes, minigame Circuit.
- Quality: the quality meter's share of its maximum when the item is made, in points (0-100). A broken or abandoned piece counts 0, so every average includes the crafts that failed.
- Crafter: the rank the scenario's cohort gives each recipe (by default the recipe's level, at most 50, so a crafter works at their own level), with no food or status bonuses and no perks unless a gate names a style or the scenario's limitations say it plays a perk loadout. Gear levels are the plan's none, middle and best tier of the skill's crafting gear:
  - no gear (Control 0, Craftsmanship 0, CP 0)
  - mid gear (Control 13, Craftsmanship 12, CP 20)
  - best gear (Control 29, Craftsmanship 30, CP 37)
  - fully modded gear (Control 119, Craftsmanship 120, CP 37)
- Recipes: 1-10: 6 of 307; 11-20: 6 of 367; 21-30: 6 of 357; 31-40: 6 of 370; 41-53: 6 of 381 recipes played.
- Adaptive player: depth 3, forecast samples per result 2, budget 4000 engine copies per decision, 0.5% of value lost per extra action, heuristic DefaultLeafHeuristic. Each attempt's dice come from its own stream of the seed.
- Fixed-rotation solver: beam 8, cycles up to 3, 8 adaptive lines as seeds, up to 40 actions, 64 training attempts per local-search round and 150 selection attempts for each of 12 finalists, local search from the best 3 candidate(s) for up to 3 round(s) each. Gate 1 searches one sequence for each band and gear tier across the band's sampled recipes (every recipe weighing the same; an action a recipe's engine cannot take is skipped there), and also each recipe's own best sequence as a reported metric. The winner is chosen on separate attempts and then scored on a third set (hold-out) with 30 attempts per recipe.
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
- Styles: none offered, so the style-balance gate is not applicable.
- Gates run: No fixed rotation, Length, Economy parity, XP rate, Enhancement parity. The others were not run, so this report cannot show that the minigame passes.

## Verdicts

| Gate | Verdict | Summary |
| --- | --- | --- |
| No fixed rotation | Failed | One fixed rotation comes within 10 points of adaptive play in 7 case(s): 1-10 / no gear; 1-10 / mid gear; 1-10 / best gear; 11-20 / no gear; 41-53 / no gear; 41-53 / mid gear; 41-53 / best gear (smallest margin -19.7 in 41-53 / no gear). The best rotation for each recipe alone, a reported metric, is -19.7 to 0.0 points behind adaptive play. |
| Length | Passed | Every band and gear tier takes 10-20 actions (item craft at level) on average. |
| Economy parity | Passed | Component stacks per item are within 10% of the legacy baseline where its success rate is at least 90%, and at most 10% above it where it is lower, at no less quality, in every band and gear tier. |
| XP rate | Passed | Item crafts: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.03x in 1-10 / mid gear, 1 enhancement). |
| Enhancement parity | Passed | At rank 50 in fully modded gear, the thresholds in force (50% / 85% / 100%) transfer at least 0.90 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 0.99 times today's). Thresholds that meet parity in every band for this quality: 13% / 99% / 99%. |

## No fixed rotation

### Average quality: adaptive play against the best single fixed rotation of each band and gear tier (quality points; a broken piece counts 0)

| Band | Gear | Recipes played | Adaptive avg quality | Best single rotation avg quality | Margin | Required | Best rotation per recipe avg quality (reported) | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 48.7 | 49.6 | -0.9 | >= 10 | 52.7 | Failed |
| 1-10 | mid gear | 6 | 87.0 | 94.3 | -7.3 | >= 10 | 98.6 | Failed |
| 1-10 | best gear | 6 | 93.3 | 99.8 | -6.5 | >= 10 | 100.0 | Failed |
| 1-10 | fully modded gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 11-20 | no gear | 6 | 82.7 | 79.6 | 3.1 | >= 10 | 88.6 | Failed |
| 11-20 | mid gear | 6 | 97.1 | 99.9 | -2.8 | >= 10 | 100.0 | Reported |
| 11-20 | best gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 11-20 | fully modded gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 21-30 | no gear | 6 | 99.8 | 100.0 | -0.2 | >= 10 | 100.0 | Reported |
| 21-30 | mid gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 21-30 | best gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 21-30 | fully modded gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 31-40 | no gear | 6 | 97.2 | 99.1 | -1.9 | >= 10 | 100.0 | Reported |
| 31-40 | mid gear | 6 | 99.0 | 100.0 | -1.0 | >= 10 | 100.0 | Reported |
| 31-40 | best gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 31-40 | fully modded gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Reported |
| 41-53 | no gear | 6 | 66.4 | 86.2 | -19.7 | >= 10 | 86.2 | Failed |
| 41-53 | mid gear | 6 | 82.6 | 97.0 | -14.4 | >= 10 | 96.5 | Failed |
| 41-53 | best gear | 6 | 96.7 | 98.7 | -2.0 | >= 10 | 100.0 | Failed |
| 41-53 | fully modded gear | 6 | 99.6 | 100.0 | -0.4 | >= 10 | 100.0 | Reported |

### The single rotation found for each band and gear tier, searched across the band's sampled recipes (average quality in points)

| Band | Gear | Best single rotation for the band | Training | Selection | Hold-out | Adaptive on these recipes |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | wire > tune x2 > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire > discharge_calibration > wire x2 | 49.4 | 49.3 | 49.6 | 48.7 |
| 1-10 | mid gear | wire > tune x2 > patch > tune > discharge_calibration > tune > wire > tune > discharge_calibration > patch > wire x3 > tune > discharge_calibration > wire x2 > tune > wire > discharge_assembly > wire | 94.0 | 93.5 | 94.3 | 87.0 |
| 1-10 | best gear | discharge_assembly > boost > discharge_calibration > tune x3 > patch > discharge_calibration > tune x3 > discharge_calibration > wire x3 > discharge_assembly > wire x3 > discharge_calibration > wire > tune > wire | 99.7 | 99.7 | 99.8 | 93.3 |
| 1-10 | fully modded gear | tune x3 > discharge_calibration > tune x3 > wire x2 > discharge_assembly > tune > wire x3 | 100.0 | 100.0 | 100.0 | 100.0 |
| 11-20 | no gear | wire > tune x2 > discharge_calibration > wire > tune x2 > discharge_calibration > wire x2 > ground > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > tune > wire > discharge_calibration > wire | 80.5 | 80.4 | 79.6 | 82.7 |
| 11-20 | mid gear | discharge_assembly > wire > tune x2 > discharge_calibration > tune x2 > patch x2 > tune x2 > discharge_calibration > boost > wire > discharge_calibration > boost > discharge_assembly > wire x2 > tune > discharge_calibration > tune > wire x2 > discharge_assembly > wire x2 > discharge_calibration > wire | 99.9 | 99.9 | 99.9 | 97.1 |
| 11-20 | best gear | tune x3 > discharge_calibration > tune x3 > discharge_calibration > tune > patch > tune > discharge_assembly > wire > tune > patch > discharge_assembly > wire x4 > discharge_calibration > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | discharge_assembly > tune x3 > discharge_calibration > tune > wire > discharge_calibration > wire > tune > wire > patch > discharge_calibration > wire x3 > discharge_both > wire > discharge_calibration > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 | 99.8 |
| 21-30 | mid gear | boost > tune x2 > discharge_calibration > tune x2 > discharge_calibration > tune x2 > wire > discharge_assembly > tune > wire x2 > discharge_assembly > tune > wire x2 | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | boost > tune x2 > discharge_calibration > tune > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | tune x4 > discharge_calibration > wire > tune x2 > discharge_both > tune > wire > tune x2 > discharge_both > boost > wire x2 > discharge_both > patch > wire x2 | 99.7 | 99.4 | 99.1 | 97.2 |
| 31-40 | mid gear | boost > tune x2 > discharge_calibration > tune x4 > discharge_both > tune x3 > wire > discharge_both > patch > wire x3 > discharge_both > wire x2 | 100.0 | 99.7 | 100.0 | 99.0 |
| 31-40 | best gear | boost > tune x2 > discharge_calibration > tune x4 > discharge_both > tune > wire > tune > discharge_both > wire x3 > discharge_both > wire | 100.0 | 99.6 | 100.0 | 100.0 |
| 31-40 | fully modded gear | boost > tune x2 > discharge_calibration > boost > tune > discharge_both > tune > wire x2 > discharge_both | 100.0 | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | boost > wire x3 > discharge_calibration > tune > wire > tune x3 > discharge_both > boost > tune x2 > patch > tune > discharge_both > discharge_calibration > tune > discharge_assembly > wire x2 | 85.6 | 85.0 | 86.2 | 66.4 |
| 41-53 | mid gear | wire x2 > boost > tune > discharge_calibration > wire > patch > boost x2 > discharge_calibration > patch > boost > tune x2 > boost > tune > discharge_calibration > tune > discharge_calibration > wire x5 > discharge_calibration > wire > discharge_calibration > wire > discharge_assembly | 98.2 | 97.6 | 97.0 | 82.6 |
| 41-53 | best gear | boost > wire > patch > tune x2 > discharge_calibration > discharge_assembly > tune x4 > discharge_both > tune x2 > patch > tune x4 > wire > discharge_both > wire > discharge_both > discharge_calibration > wire x2 > discharge_calibration > wire | 98.9 | 99.2 | 98.7 | 96.7 |
| 41-53 | fully modded gear | boost > tune x2 > discharge_calibration > tune x4 > discharge_both > boost > wire > discharge_both | 100.0 | 100.0 | 100.0 | 99.6 |

### Reported metric, not gated: the best fixed rotation for each recipe alone, and what the band's single rotation scores on the same recipe (average quality in points)

| Band | Gear | Recipe | Best rotation for this recipe alone (reported) | Its hold-out | The single rotation on this recipe | Adaptive on this recipe |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | wen_cft_smth1 (level 3) | wire x5 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire | 37.5 | 33.3 | 34.3 |
| 1-10 | no gear | config_ind1 (level 5) | wire x2 > tune > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire | 44.1 | 45.0 | 45.5 |
| 1-10 | no gear | d_hd_pva1 (level 7) | wire x2 > discharge_calibration > wire x4 > tune > discharge_calibration > wire x4 > discharge_calibration > wire > discharge_calibration > wire | 53.7 | 47.1 | 46.9 |
| 1-10 | no gear | song_460 (level 8) | wire x2 > tune x2 > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire > discharge_calibration > wire | 62.2 | 59.0 | 52.0 |
| 1-10 | no gear | d_lg_pvw1 (level 10) | wire > tune x2 > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire | 57.3 | 57.3 | 54.3 |
| 1-10 | no gear | id_soulstrike1 (level 10) | wire x3 > tune x2 > discharge_calibration > wire > discharge_calibration > wire x5 > discharge_calibration > wire > discharge_calibration > wire > discharge_calibration > wire | 61.2 | 55.6 | 59.3 |
| 1-10 | mid gear | wen_cft_smth1 (level 3) | tune x5 > discharge_calibration > tune x2 > wire > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire | 100.0 | 98.4 | 87.1 |
| 1-10 | mid gear | config_ind1 (level 5) | discharge_assembly > tune x3 > discharge_calibration > wire x2 > tune > discharge_calibration > wire x2 > tune x2 > discharge_assembly > wire x2 > tune > discharge_calibration > wire x2 | 91.7 | 76.5 | 60.8 |
| 1-10 | mid gear | d_hd_pva1 (level 7) | tune x4 > discharge_calibration > wire > tune x2 > wire > tune > discharge_assembly > wire x2 > tune > wire x2 > discharge_calibration > wire | 100.0 | 100.0 | 96.6 |
| 1-10 | mid gear | song_460 (level 8) | tune x4 > discharge_calibration > tune > wire > tune x2 > ground > tune > wire x3 > discharge_calibration > wire x4 > discharge_calibration > wire | 99.7 | 99.4 | 91.7 |
| 1-10 | mid gear | d_lg_pvw1 (level 10) | wire x2 > tune > discharge_calibration > wire x2 > tune > discharge_calibration > tune > patch > tune x2 > discharge_calibration > wire > tune > patch > wire > discharge_calibration > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire x3 | 100.0 | 97.6 | 96.8 |
| 1-10 | mid gear | id_soulstrike1 (level 10) | tune x5 > discharge_calibration > wire x2 > tune > wire > tune > discharge_assembly > wire x4 > tune > discharge_calibration > wire x3 | 100.0 | 93.9 | 89.1 |
| 1-10 | best gear | wen_cft_smth1 (level 3) | tune x5 > discharge_calibration > tune x4 > wire > discharge_assembly > wire x3 > tune > discharge_assembly > wire | 100.0 | 99.2 | 86.6 |
| 1-10 | best gear | config_ind1 (level 5) | tune x3 > discharge_calibration > tune x3 > discharge_calibration > wire > tune > wire > discharge_assembly > wire > tune > wire > discharge_calibration > wire x3 | 100.0 | 100.0 | 87.0 |
| 1-10 | best gear | d_hd_pva1 (level 7) | tune x5 > discharge_calibration > wire > tune x3 > wire > discharge_assembly > wire x4 | 100.0 | 100.0 | 94.7 |
| 1-10 | best gear | song_460 (level 8) | tune x4 > discharge_calibration > tune x4 > discharge_assembly > wire > tune > wire > tune > discharge_assembly > wire x3 | 100.0 | 99.5 | 91.3 |
| 1-10 | best gear | d_lg_pvw1 (level 10) | tune x4 > discharge_calibration > tune x2 > patch > tune > discharge_calibration > tune > wire x3 > discharge_assembly > wire x3 > discharge_calibration > wire x2 | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | id_soulstrike1 (level 10) | tune x5 > discharge_calibration > tune x4 > wire > patch > discharge_assembly > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | wen_cft_smth1 (level 3) | tune x5 > discharge_calibration > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | config_ind1 (level 5) | tune x3 > discharge_calibration > tune x3 > wire x5 | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | d_hd_pva1 (level 7) | boost > tune x2 > discharge_calibration > tune x2 > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | song_460 (level 8) | tune x4 > discharge_calibration > tune x2 > wire x5 | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | d_lg_pvw1 (level 10) | tune x3 > discharge_calibration > tune x3 > wire x5 | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | id_soulstrike1 (level 10) | tune x5 > discharge_calibration > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | no gear | dhbr002 (level 13) | wire > tune x3 > discharge_calibration > wire x4 > discharge_calibration > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire | 71.4 | 67.5 | 62.7 |
| 11-20 | no gear | song_579 (level 15) | wire x2 > boost > tune x2 > discharge_calibration > tune > wire x7 > discharge_calibration > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire | 81.2 | 68.6 | 76.4 |
| 11-20 | no gear | eva_boost_1 (level 18) | boost > tune x2 > discharge_calibration > wire x5 > tune > discharge_calibration > wire x4 > discharge_calibration > wire | 84.1 | 76.8 | 82.4 |
| 11-20 | no gear | d_ar_pvs2 (level 19) | boost > wire > tune x3 > discharge_calibration > wire x6 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire x2 > discharge_calibration > wire | 95.3 | 84.4 | 90.6 |
| 11-20 | no gear | darg002 (level 20) | discharge_assembly > boost > tune x2 > discharge_calibration > wire x2 > tune > discharge_calibration > wire x3 > discharge_assembly > wire x2 > discharge_calibration > wire | 100.0 | 100.0 | 99.3 |
| 11-20 | no gear | id_pinningtoss1 (level 20) | discharge_assembly > tune x2 > discharge_calibration > tune x2 > discharge_calibration > wire > tune > discharge_assembly > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire x2 | 99.8 | 80.0 | 84.7 |
| 11-20 | mid gear | dhbr002 (level 13) | wire > tune x3 > discharge_calibration > tune x4 > patch > discharge_calibration > wire x3 > discharge_calibration > wire x4 > discharge_calibration > wire x2 | 100.0 | 100.0 | 98.4 |
| 11-20 | mid gear | song_579 (level 15) | boost > tune x4 > discharge_calibration > wire > tune x4 > wire x2 > discharge_assembly > wire x3 | 100.0 | 99.9 | 93.8 |
| 11-20 | mid gear | eva_boost_1 (level 18) | tune x5 > discharge_calibration > tune x4 > wire > discharge_assembly > tune x2 > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 96.5 |
| 11-20 | mid gear | d_ar_pvs2 (level 19) | boost > tune x3 > discharge_calibration > tune x5 > wire > discharge_assembly > tune > wire x3 > discharge_assembly > wire | 100.0 | 99.4 | 93.7 |
| 11-20 | mid gear | darg002 (level 20) | boost > tune x2 > discharge_calibration > tune x2 > discharge_calibration > tune > wire x2 > discharge_assembly > tune > wire x2 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | id_pinningtoss1 (level 20) | discharge_assembly > tune x2 > patch > discharge_calibration > tune > discharge_calibration > tune x2 > discharge_calibration > patch > tune > wire > discharge_calibration > wire x3 > discharge_assembly > wire x2 > discharge_calibration > wire x3 | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | dhbr002 (level 13) | tune x4 > discharge_calibration > tune x4 > patch > discharge_calibration > wire x8 | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | song_579 (level 15) | boost > tune x4 > discharge_calibration > tune x3 > wire x3 > discharge_assembly > wire x2 | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | eva_boost_1 (level 18) | tune x5 > discharge_calibration > tune x3 > discharge_calibration > wire x5 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | d_ar_pvs2 (level 19) | boost > tune x3 > discharge_calibration > boost > tune > discharge_calibration > tune > wire x4 > discharge_assembly > wire x2 | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | darg002 (level 20) | boost > tune x2 > discharge_calibration > tune x3 > discharge_assembly > tune x2 > wire > discharge_assembly > wire x2 | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | id_pinningtoss1 (level 20) | wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | dhbr002 (level 13) | tune x4 > discharge_calibration > tune > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | song_579 (level 15) | boost > tune x3 > discharge_calibration > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | eva_boost_1 (level 18) | tune x5 > discharge_calibration > wire x5 | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | d_ar_pvs2 (level 19) | boost > tune x3 > discharge_calibration > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | darg002 (level 20) | tune x3 > discharge_calibration > wire x4 | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | id_pinningtoss1 (level 20) | tune x2 > discharge_calibration > tune x3 > discharge_assembly > wire x2 > discharge_calibration > wire x3 | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | wen_cft_smth3 (level 23) | tune x4 > discharge_calibration > wire > tune > wire > discharge_calibration > wire x2 > discharge_calibration > wire x4 > discharge_assembly | 100.0 | 100.0 | 98.7 |
| 21-30 | no gear | ion_armor_3 (level 25) | boost > tune x2 > discharge_calibration > tune x3 > discharge_calibration > tune x2 > wire > discharge_assembly > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | d_lg_vws3 (level 27) | boost > tune x2 > discharge_calibration > tune x3 > discharge_calibration > wire x3 > discharge_assembly > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | dlbe003 (level 29) | boost > tune x2 > discharge_calibration > tune x3 > discharge_calibration > tune > wire x2 > discharge_assembly > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | dsbr003 (level 30) | boost > tune x2 > discharge_calibration > boost > tune x2 > discharge_both > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | id_linebreaker3 (level 30) | tune x4 > discharge_calibration > wire > tune x3 > discharge_both > wire x4 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | wen_cft_smth3 (level 23) | tune x4 > discharge_calibration > wire > tune x3 > discharge_assembly > wire > tune > wire > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | ion_armor_3 (level 25) | boost > tune x2 > discharge_calibration > tune x2 > discharge_calibration > tune x2 > wire > discharge_assembly > tune > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | d_lg_vws3 (level 27) | boost > tune x2 > discharge_calibration > tune x3 > discharge_both > boost > wire x2 > discharge_assembly > wire x2 | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | dlbe003 (level 29) | boost > tune x2 > discharge_calibration > tune x2 > discharge_calibration > tune > wire > tune > discharge_assembly > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | dsbr003 (level 30) | boost > tune x2 > discharge_calibration > boost > tune > discharge_both > tune > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | id_linebreaker3 (level 30) | boost > tune x2 > discharge_calibration > tune x4 > discharge_both > wire x4 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | wen_cft_smth3 (level 23) | tune x4 > discharge_calibration > tune x3 > wire > discharge_assembly > wire x2 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | ion_armor_3 (level 25) | tune x4 > discharge_calibration > tune x2 > wire > discharge_both > tune > wire x2 > discharge_both > wire > discharge_both > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | d_lg_vws3 (level 27) | boost > tune x2 > discharge_calibration > tune x3 > discharge_assembly > tune > wire x2 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | dlbe003 (level 29) | tune x4 > discharge_calibration > boost > tune x2 > discharge_both > wire > tune > wire > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | dsbr003 (level 30) | boost > tune x2 > discharge_calibration > boost > tune x2 > discharge_assembly > boost > wire > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | id_linebreaker3 (level 30) | boost > tune x2 > discharge_calibration > tune x3 > discharge_both > wire x4 > discharge_assembly | 100.0 | 100.0 | 99.9 |
| 21-30 | fully modded gear | wen_cft_smth3 (level 23) | boost > tune x2 > discharge_calibration > tune > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 99.9 |
| 21-30 | fully modded gear | ion_armor_3 (level 25) | boost > tune x2 > discharge_calibration > tune > wire x2 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | d_lg_vws3 (level 27) | tune x3 > discharge_calibration > wire x4 | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | dlbe003 (level 29) | tune x3 > discharge_calibration > wire x4 | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | dsbr003 (level 30) | boost > tune x2 > discharge_both > tune > wire x2 | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | id_linebreaker3 (level 30) | tune x4 > discharge_both > wire x3 | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | aen_cft_fab4 (level 33) | boost > tune x2 > discharge_calibration > tune x3 > discharge_calibration > tune > wire > tune > patch > discharge_both > tune > wire x2 > discharge_both > wire x3 > discharge_both | 100.0 | 96.7 | 99.8 |
| 31-40 | no gear | config_ind4 (level 35) | tune x4 > discharge_calibration > tune x4 > discharge_both > tune x2 > wire x2 > discharge_assembly > wire > discharge_both > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | d_lg_mws4 (level 37) | boost > tune x2 > discharge_calibration > boost > tune x2 > discharge_both > tune x3 > wire > discharge_both > boost > wire > discharge_assembly | 100.0 | 100.0 | 96.1 |
| 31-40 | no gear | d_hd_mwa4 (level 39) | boost > tune x2 > discharge_calibration > boost > tune x2 > discharge_both > boost > tune x2 > discharge_both > tune > wire x2 > discharge_assembly | 100.0 | 100.0 | 93.0 |
| 31-40 | no gear | dfhl004 (level 40) | boost > tune x2 > discharge_calibration > boost > tune x2 > discharge_both > tune x2 > wire > tune > discharge_both > tune > wire x2 > discharge_both | 100.0 | 100.0 | 94.9 |
| 31-40 | no gear | id_conctoss2 (level 40) | boost > tune x2 > discharge_calibration > tune > wire > tune x3 > discharge_both > tune x5 > wire > discharge_both > wire x3 | 100.0 | 98.2 | 99.7 |
| 31-40 | mid gear | aen_cft_fab4 (level 33) | tune x4 > discharge_calibration > tune x3 > discharge_both > tune > wire x2 > discharge_both > wire x3 > discharge_calibration > wire > discharge_both | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | config_ind4 (level 35) | tune x4 > discharge_calibration > tune x4 > discharge_both > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | d_lg_mws4 (level 37) | tune x4 > discharge_calibration > wire > tune x3 > discharge_both > wire > tune > wire x2 > discharge_both | 100.0 | 100.0 | 99.1 |
| 31-40 | mid gear | d_hd_mwa4 (level 39) | boost > tune x2 > discharge_calibration > tune x4 > discharge_both > tune x2 > wire x2 > discharge_both > wire x2 | 100.0 | 100.0 | 98.5 |
| 31-40 | mid gear | dfhl004 (level 40) | tune x4 > discharge_calibration > tune x4 > discharge_both > tune > wire x3 > discharge_both | 100.0 | 100.0 | 96.7 |
| 31-40 | mid gear | id_conctoss2 (level 40) | boost > tune x2 > discharge_calibration > tune x5 > discharge_both > wire > tune x3 > discharge_assembly > wire x2 > discharge_both | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | aen_cft_fab4 (level 33) | boost > tune x2 > discharge_calibration > tune x3 > discharge_both > tune x2 > wire > discharge_both > wire x3 > discharge_calibration > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | config_ind4 (level 35) | boost > tune x2 > discharge_calibration > tune x4 > discharge_both > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | d_lg_mws4 (level 37) | boost > tune x2 > discharge_calibration > tune x4 > discharge_both > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | d_hd_mwa4 (level 39) | boost > tune x2 > discharge_calibration > boost > tune x2 > discharge_both > tune x2 > wire x2 > discharge_both | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | dfhl004 (level 40) | boost > tune x2 > discharge_calibration > tune x4 > discharge_both > tune > wire x2 > discharge_assembly | 100.0 | 100.0 | 99.9 |
| 31-40 | best gear | id_conctoss2 (level 40) | tune x4 > discharge_calibration > tune x5 > discharge_both > boost > wire x2 > discharge_both > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | aen_cft_fab4 (level 33) | tune x4 > discharge_both > tune > wire > tune > discharge_both > wire x3 | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | config_ind4 (level 35) | boost > tune x2 > discharge_calibration > wire > tune > wire > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | d_lg_mws4 (level 37) | tune x4 > discharge_calibration > boost > wire > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | d_hd_mwa4 (level 39) | boost > tune x2 > discharge_calibration > boost > tune > wire > discharge_both > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | dfhl004 (level 40) | boost > tune x2 > discharge_calibration > tune x2 > wire > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | id_conctoss2 (level 40) | tune x4 > discharge_calibration > wire > tune x2 > wire > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | dhbr005 (level 43) | boost > tune x2 > discharge_calibration > wire > tune x3 > discharge_both > tune > wire > tune x2 > discharge_both > wire x3 > tune > discharge_both > discharge_assembly | 100.0 | 97.6 | 87.1 |
| 41-53 | no gear | therm_amp_4 (level 46) | wire x2 > tune x2 > wire > tune x3 > discharge_both > boost > tune x4 > boost > tune > discharge_both > wire x2 | 98.2 | 99.8 | 87.2 |
| 41-53 | no gear | wen_eva5 (level 48) | tune > wire > tune x3 > discharge_both > wire > tune x3 > patch > discharge_both > discharge_assembly > tune x3 > wire > discharge_both > wire x3 > discharge_calibration > wire | 74.1 | 73.6 | 34.3 |
| 41-53 | no gear | d_lg_mwa5 (level 49) | boost > tune x4 > discharge_both > boost > tune x4 > discharge_both > wire > tune x3 > discharge_assembly | 92.4 | 98.4 | 89.3 |
| 41-53 | no gear | id_infusion2 (level 50) | boost > tune x3 > discharge_both > tune > wire > tune x3 > discharge_both > tune > wire > tune x2 > discharge_both | 76.1 | 72.2 | 53.4 |
| 41-53 | no gear | cap_quadlas1 (level 52) | boost > wire x3 > discharge_calibration > wire > tune x4 > discharge_both > boost > tune x3 > discharge_both > tune x2 > discharge_assembly > wire x2 | 76.4 | 75.4 | 47.4 |
| 41-53 | mid gear | dhbr005 (level 43) | boost > tune x2 > discharge_calibration > tune x4 > discharge_both > tune x3 > wire > discharge_both > wire x2 > discharge_both | 100.0 | 99.9 | 97.2 |
| 41-53 | mid gear | therm_amp_4 (level 46) | tune x5 > discharge_both > wire x3 > tune x6 > discharge_both > wire > tune > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | wen_eva5 (level 48) | tune > boost x2 > discharge_calibration > wire > patch > boost x2 > discharge_calibration > patch > boost > tune > patch > tune > patch > tune > discharge_calibration > wire x5 > discharge_calibration > wire x3 > discharge_assembly | 96.8 | 87.8 | 67.8 |
| 41-53 | mid gear | d_lg_mwa5 (level 49) | boost > tune x5 > discharge_both > tune x5 > discharge_both > wire > tune > discharge_both | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | id_infusion2 (level 50) | boost > tune x3 > discharge_both > tune x5 > discharge_both > tune > wire > tune x2 > discharge_both | 92.4 | 99.5 | 90.2 |
| 41-53 | mid gear | cap_quadlas1 (level 52) | boost > tune x4 > discharge_both > wire > tune x4 > discharge_both > boost x2 > tune > discharge_both | 89.9 | 94.9 | 40.7 |
| 41-53 | best gear | dhbr005 (level 43) | boost > tune x2 > discharge_calibration > boost > tune x2 > discharge_calibration > tune x2 > wire > tune > discharge_assembly > wire x2 > discharge_both | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | therm_amp_4 (level 46) | boost > tune x2 > discharge_calibration > boost > tune x3 > discharge_both > boost > tune x2 > discharge_both > wire x2 | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | wen_eva5 (level 48) | boost > tune x2 > discharge_calibration > tune x4 > patch > discharge_both > tune x4 > discharge_both > wire x2 > discharge_both > wire > boost > discharge_both | 100.0 | 97.6 | 85.8 |
| 41-53 | best gear | d_lg_mwa5 (level 49) | boost > tune x3 > discharge_calibration > wire > tune x4 > discharge_both > wire x3 | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | id_infusion2 (level 50) | wire > ground > boost > tune x2 > discharge_calibration > boost > patch > wire > discharge_calibration > tune x4 > discharge_both > wire > tune x4 > patch > discharge_both > wire | 99.9 | 95.0 | 97.3 |
| 41-53 | best gear | cap_quadlas1 (level 52) | tune > boost > discharge_calibration > tune x5 > wire > tune x4 > discharge_calibration > wire > patch > boost > wire x2 > discharge_both | 100.0 | 99.4 | 97.2 |
| 41-53 | fully modded gear | dhbr005 (level 43) | boost > tune x2 > discharge_calibration > boost > tune > discharge_both > wire x2 > discharge_both > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | fully modded gear | therm_amp_4 (level 46) | boost > tune x2 > discharge_calibration > tune x3 > discharge_both > wire x3 | 100.0 | 100.0 | 100.0 |
| 41-53 | fully modded gear | wen_eva5 (level 48) | boost > tune x2 > discharge_calibration > tune x4 > discharge_both > wire x3 | 100.0 | 100.0 | 100.0 |
| 41-53 | fully modded gear | d_lg_mwa5 (level 49) | boost > tune x2 > discharge_calibration > tune x3 > discharge_both > wire x3 | 100.0 | 100.0 | 100.0 |
| 41-53 | fully modded gear | id_infusion2 (level 50) | tune x4 > discharge_calibration > tune x4 > discharge_assembly > wire > tune > discharge_both | 100.0 | 100.0 | 97.7 |
| 41-53 | fully modded gear | cap_quadlas1 (level 52) | boost > tune x2 > discharge_calibration > tune x5 > discharge_both > wire x2 | 100.0 | 100.0 | 100.0 |

## Length

### Actions per attempt of the adaptive player at level (item craft at level)

| Band | Gear | Recipes played | Mean actions per attempt | Shortest recipe | Longest recipe | Stalled attempts | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 14.4 | 12.2 | 16.2 | 0 | 10-20 | Passed |
| 1-10 | mid gear | 6 | 17.5 | 16.0 | 18.3 | 0 | 10-20 | Passed |
| 1-10 | best gear | 6 | 15.4 | 13.0 | 17.2 | 0 | 10-20 | Passed |
| 1-10 | fully modded gear | 6 | 10.0 | 8.9 | 10.3 | 0 | 10-20 | Reported |
| 11-20 | no gear | 6 | 15.7 | 14.2 | 17.4 | 0 | 10-20 | Passed |
| 11-20 | mid gear | 6 | 15.5 | 12.0 | 17.5 | 0 | 10-20 | Passed |
| 11-20 | best gear | 6 | 13.7 | 10.9 | 15.4 | 0 | 10-20 | Passed |
| 11-20 | fully modded gear | 6 | 8.8 | 7.4 | 9.7 | 0 | 10-20 | Reported |
| 21-30 | no gear | 6 | 13.9 | 11.7 | 15.1 | 0 | 10-20 | Passed |
| 21-30 | mid gear | 6 | 12.4 | 11.0 | 13.8 | 0 | 10-20 | Passed |
| 21-30 | best gear | 6 | 10.8 | 9.1 | 11.7 | 0 | 10-20 | Passed |
| 21-30 | fully modded gear | 6 | 7.2 | 6.4 | 8.4 | 0 | 10-20 | Reported |
| 31-40 | no gear | 6 | 16.5 | 15.2 | 18.6 | 0 | 10-20 | Reported |
| 31-40 | mid gear | 6 | 14.6 | 13.5 | 16.7 | 0 | 10-20 | Passed |
| 31-40 | best gear | 6 | 12.4 | 11.4 | 14.5 | 0 | 10-20 | Passed |
| 31-40 | fully modded gear | 6 | 8.0 | 7.4 | 8.6 | 0 | 10-20 | Reported |
| 41-53 | no gear | 6 | 16.9 | 16.0 | 18.6 | 0 | 10-20 | Reported |
| 41-53 | mid gear | 6 | 17.1 | 15.4 | 20.5 | 0 | 10-20 | Reported |
| 41-53 | best gear | 6 | 15.7 | 12.7 | 19.1 | 0 | 10-20 | Passed |
| 41-53 | fully modded gear | 6 | 9.2 | 7.7 | 10.9 | 0 | 10-20 | Reported |

## Economy parity

### Component stacks consumed per item made, failures included, over the recipes the legacy policy makes at least once in the cell (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Parity rule | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 2.33 | 2.37 | 0.98 | 100% | 98% | 49% | 1% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | mid gear | 6 | 2.40 | 2.37 | 1.01 | 96% | 98% | 87% | 10% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | best gear | 6 | 2.37 | 2.35 | 1.01 | 97% | 99% | 93% | 25% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | fully modded gear | 6 | 2.33 | 2.34 | 1.00 | 100% | 99% | 100% | 75% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | no gear | 6 | 2.00 | 2.10 | 0.95 | 100% | 93% | 85% | 3% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | mid gear | 6 | 2.01 | 2.02 | 0.99 | 99% | 98% | 97% | 13% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | best gear | 6 | 2.00 | 2.03 | 0.99 | 100% | 98% | 100% | 28% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | fully modded gear | 6 | 2.00 | 2.01 | 0.99 | 100% | 99% | 100% | 91% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | no gear | 6 | 2.50 | 2.55 | 0.98 | 100% | 97% | 100% | 8% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | mid gear | 6 | 2.50 | 2.53 | 0.99 | 100% | 98% | 100% | 15% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | best gear | 6 | 2.50 | 2.51 | 1.00 | 100% | 99% | 100% | 29% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | fully modded gear | 6 | 2.50 | 2.52 | 0.99 | 100% | 99% | 100% | 93% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | no gear | 6 | 2.51 | 2.53 | 0.99 | 99% | 98% | 97% | 23% | yes | 0% / 0% | within 10% | Reported |
| 31-40 | mid gear | 6 | 2.51 | 2.53 | 0.99 | 99% | 98% | 99% | 35% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | best gear | 6 | 2.50 | 2.52 | 0.99 | 100% | 99% | 100% | 48% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | fully modded gear | 6 | 2.50 | 2.54 | 0.98 | 100% | 97% | 100% | 85% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | no gear | 6 | 2.56 | 2.42 | 1.06 | 89% | 99% | 70% | 42% | yes | 0% / 17% | within 10% | Reported |
| 41-53 | mid gear | 6 | 2.65 | 2.72 | 0.97 | 91% | 87% | 83% | 55% | yes | 0% / 0% | at most +10% | Reported |
| 41-53 | best gear | 6 | 2.52 | 2.59 | 0.98 | 98% | 94% | 97% | 76% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | fully modded gear | 6 | 2.50 | 2.54 | 0.98 | 100% | 98% | 100% | 89% | yes | 0% / 0% | within 10% | Passed |

### Recipes the legacy policy never makes in the cell, left out of the stack and quality comparison above (they still count in the never-made check)

| Band | Gear | Recipe | Level | New success | New avg quality |
| --- | --- | --- | --- | --- | --- |
| 41-53 | no gear | QuadLaserCannon1 | 52 | 90% | 47% |

## XP rate

### Item crafts: XP per hour against the legacy loop (6.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per craft | Legacy actions per craft | Legacy XP per hour | New XP per craft | New actions per craft | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 595 | 5.8 | 61824 | 2043 | 14.4 | 64029 | 1.04 | - | Passed |
| 1-10 | no gear, 1 enhancement | 429 | 6.4 | 40203 | 1806 | 18.3 | 44455 | 1.11 | - | Passed |
| 1-10 | no gear, 2 enhancements | 184 | 6.0 | 18350 | 1766 | 18.0 | 44106 | 2.40 | - | Passed |
| 1-10 | no gear, 3 enhancements | 90 | 6.0 | 9000 | 1780 | 18.2 | 43925 | 4.88 | - | Passed |
| 1-10 | mid gear | 649 | 7.5 | 51840 | 2524 | 17.5 | 64981 | 1.25 | - | Passed |
| 1-10 | mid gear, 1 enhancement | 603 | 7.9 | 45828 | 1912 | 18.2 | 47145 | 1.03 | - | Passed |
| 1-10 | mid gear, 2 enhancements | 578 | 8.4 | 41136 | 1982 | 18.5 | 48114 | 1.17 | - | Passed |
| 1-10 | mid gear, 3 enhancements | 529 | 8.9 | 35636 | 1915 | 18.5 | 46662 | 1.31 | - | Passed |
| 1-10 | best gear | 744 | 8.8 | 50836 | 2623 | 15.4 | 76837 | 1.51 | - | Passed |
| 1-10 | best gear, 1 enhancement | 722 | 9.2 | 47316 | 2275 | 18.0 | 56835 | 1.20 | - | Passed |
| 1-10 | best gear, 2 enhancements | 719 | 9.6 | 44992 | 2344 | 18.1 | 58304 | 1.30 | - | Passed |
| 1-10 | best gear, 3 enhancements | 687 | 10.0 | 41012 | 2313 | 18.1 | 57496 | 1.40 | - | Passed |
| 1-10 | fully modded gear | 1047 | 7.7 | 82045 | 2748 | 10.0 | 123861 | 1.51 | - | Passed |
| 1-10 | fully modded gear, 1 enhancement | 1031 | 7.7 | 80482 | 2748 | 10.8 | 114324 | 1.42 | - | Passed |
| 1-10 | fully modded gear, 2 enhancements | 1019 | 8.0 | 76508 | 2745 | 10.8 | 113867 | 1.49 | - | Passed |
| 1-10 | fully modded gear, 3 enhancements | 1018 | 8.5 | 71761 | 2732 | 11.0 | 112172 | 1.56 | - | Passed |
| 11-20 | no gear | 579 | 7.9 | 44148 | 1688 | 15.7 | 48512 | 1.10 | - | Passed |
| 11-20 | no gear, 1 enhancement | 389 | 8.6 | 27294 | 1765 | 17.7 | 44755 | 1.64 | - | Passed |
| 11-20 | no gear, 2 enhancements | 265 | 7.3 | 21917 | 1734 | 17.8 | 43956 | 2.01 | - | Passed |
| 11-20 | no gear, 3 enhancements | 252 | 8.1 | 18738 | 1744 | 17.6 | 44669 | 2.38 | - | Passed |
| 11-20 | mid gear | 667 | 10.2 | 39192 | 1816 | 15.5 | 52735 | 1.35 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 635 | 11.6 | 32743 | 1783 | 16.5 | 48561 | 1.48 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 616 | 12.8 | 28933 | 1805 | 16.7 | 48516 | 1.68 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 537 | 13.9 | 23243 | 1798 | 16.9 | 47850 | 2.06 | - | Passed |
| 11-20 | best gear | 761 | 14.5 | 31427 | 1848 | 13.7 | 60649 | 1.93 | - | Passed |
| 11-20 | best gear, 1 enhancement | 749 | 15.4 | 29203 | 1848 | 14.6 | 56979 | 1.95 | - | Passed |
| 11-20 | best gear, 2 enhancements | 740 | 16.0 | 27777 | 1845 | 14.6 | 56724 | 2.04 | - | Passed |
| 11-20 | best gear, 3 enhancements | 717 | 16.5 | 26050 | 1845 | 14.2 | 58466 | 2.24 | - | Passed |
| 11-20 | fully modded gear | 1146 | 14.4 | 47678 | 1848 | 8.8 | 94499 | 1.98 | - | Passed |
| 11-20 | fully modded gear, 1 enhancement | 1147 | 14.5 | 47531 | 1848 | 9.1 | 91718 | 1.93 | - | Passed |
| 11-20 | fully modded gear, 2 enhancements | 1133 | 14.6 | 46455 | 1848 | 9.2 | 90065 | 1.94 | - | Passed |
| 11-20 | fully modded gear, 3 enhancements | 1134 | 14.9 | 45749 | 1848 | 9.2 | 90065 | 1.97 | - | Passed |
| 21-30 | no gear | 633 | 11.3 | 33571 | 1379 | 13.9 | 44468 | 1.32 | - | Passed |
| 21-30 | no gear, 1 enhancement | 617 | 12.2 | 30271 | 1379 | 15.3 | 40643 | 1.34 | - | Passed |
| 21-30 | no gear, 2 enhancements | 605 | 12.9 | 28088 | 1377 | 15.6 | 39806 | 1.42 | - | Passed |
| 21-30 | no gear, 3 enhancements | 559 | 14.1 | 23889 | 1378 | 15.6 | 39663 | 1.66 | - | Passed |
| 21-30 | mid gear | 683 | 16.8 | 24372 | 1380 | 12.4 | 49899 | 2.05 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 677 | 17.5 | 23193 | 1380 | 13.3 | 46594 | 2.01 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 671 | 17.5 | 23035 | 1379 | 13.2 | 46870 | 2.03 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 652 | 18.0 | 21772 | 1378 | 13.6 | 45677 | 2.10 | - | Passed |
| 21-30 | best gear | 773 | 22.6 | 20498 | 1380 | 10.8 | 57347 | 2.80 | - | Passed |
| 21-30 | best gear, 1 enhancement | 763 | 22.7 | 20201 | 1380 | 11.5 | 54025 | 2.67 | - | Passed |
| 21-30 | best gear, 2 enhancements | 755 | 23.2 | 19561 | 1380 | 11.7 | 53101 | 2.71 | - | Passed |
| 21-30 | best gear, 3 enhancements | 748 | 23.3 | 19290 | 1380 | 11.4 | 54367 | 2.82 | - | Passed |
| 21-30 | fully modded gear | 1151 | 22.1 | 31253 | 1380 | 7.2 | 85914 | 2.75 | - | Passed |
| 21-30 | fully modded gear, 1 enhancement | 1109 | 22.6 | 29432 | 1380 | 7.5 | 82616 | 2.81 | - | Passed |
| 21-30 | fully modded gear, 2 enhancements | 1129 | 22.7 | 29785 | 1380 | 7.4 | 84108 | 2.82 | - | Passed |
| 21-30 | fully modded gear, 3 enhancements | 1121 | 22.7 | 29589 | 1380 | 7.4 | 83793 | 2.83 | - | Passed |
| 31-40 | no gear | 726 | 14.8 | 29537 | 1181 | 16.5 | 32165 | 1.09 | - | Passed |
| 31-40 | no gear, 1 enhancement | 680 | 14.5 | 28186 | 1178 | 16.9 | 31293 | 1.11 | - | Passed |
| 31-40 | no gear, 2 enhancements | 631 | 15.1 | 25051 | 1167 | 16.9 | 31115 | 1.24 | - | Passed |
| 31-40 | no gear, 3 enhancements | 583 | 16.0 | 21889 | 1175 | 16.8 | 31487 | 1.44 | - | Passed |
| 31-40 | mid gear | 799 | 18.8 | 25485 | 1191 | 14.6 | 36680 | 1.44 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 783 | 19.4 | 24165 | 1199 | 14.7 | 36584 | 1.51 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 743 | 19.2 | 23152 | 1198 | 14.5 | 37055 | 1.60 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 674 | 18.4 | 21987 | 1198 | 14.6 | 36986 | 1.68 | - | Passed |
| 31-40 | best gear | 884 | 24.6 | 21596 | 1200 | 12.4 | 43448 | 2.01 | - | Passed |
| 31-40 | best gear, 1 enhancement | 887 | 25.3 | 21027 | 1200 | 12.8 | 42209 | 2.01 | - | Passed |
| 31-40 | best gear, 2 enhancements | 859 | 25.0 | 20580 | 1194 | 13.0 | 41424 | 2.01 | - | Passed |
| 31-40 | best gear, 3 enhancements | 817 | 23.7 | 20695 | 1200 | 12.8 | 42103 | 2.03 | - | Passed |
| 31-40 | fully modded gear | 1093 | 25.3 | 25966 | 1200 | 8.0 | 67688 | 2.61 | - | Passed |
| 31-40 | fully modded gear, 1 enhancement | 1109 | 25.6 | 25990 | 1200 | 7.9 | 68211 | 2.62 | - | Passed |
| 31-40 | fully modded gear, 2 enhancements | 1115 | 26.4 | 25368 | 1200 | 8.0 | 67498 | 2.66 | - | Passed |
| 31-40 | fully modded gear, 3 enhancements | 1107 | 26.6 | 24967 | 1200 | 8.1 | 67034 | 2.68 | - | Passed |
| 41-53 | no gear | 725 | 17.5 | 24801 | 979 | 16.9 | 26023 | 1.05 | - | Passed |
| 41-53 | no gear, 1 enhancement | 638 | 16.0 | 23876 | 585 | 17.3 | 15190 | 0.64 | - | Passed |
| 41-53 | no gear, 2 enhancements | 510 | 16.4 | 18665 | 642 | 17.4 | 16589 | 0.89 | - | Passed |
| 41-53 | no gear, 3 enhancements | 447 | 17.4 | 15438 | 572 | 17.5 | 14719 | 0.95 | - | Passed |
| 41-53 | mid gear | 868 | 23.9 | 21755 | 1073 | 17.1 | 28212 | 1.30 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 788 | 22.6 | 20965 | 1025 | 18.4 | 25029 | 1.19 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 684 | 20.0 | 20552 | 1077 | 18.4 | 26413 | 1.29 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 596 | 19.2 | 18602 | 976 | 18.6 | 23620 | 1.27 | - | Passed |
| 41-53 | best gear | 1044 | 28.5 | 22007 | 1221 | 15.7 | 34898 | 1.59 | - | Passed |
| 41-53 | best gear, 1 enhancement | 970 | 27.8 | 20982 | 1234 | 16.3 | 33988 | 1.62 | - | Passed |
| 41-53 | best gear, 2 enhancements | 885 | 26.0 | 20448 | 1236 | 16.3 | 34049 | 1.67 | - | Passed |
| 41-53 | best gear, 3 enhancements | 776 | 23.3 | 19995 | 1200 | 16.1 | 33464 | 1.67 | - | Passed |
| 41-53 | fully modded gear | 1157 | 27.7 | 25051 | 1248 | 9.2 | 61210 | 2.44 | - | Passed |
| 41-53 | fully modded gear, 1 enhancement | 1159 | 28.2 | 24692 | 1250 | 9.4 | 59590 | 2.41 | - | Passed |
| 41-53 | fully modded gear, 2 enhancements | 1163 | 28.9 | 24113 | 1250 | 9.5 | 59106 | 2.45 | - | Passed |
| 41-53 | fully modded gear, 3 enhancements | 1159 | 29.1 | 23858 | 1250 | 9.7 | 57988 | 2.43 | - | Passed |

## Enhancement parity

### Rank 50, fully modded gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 100%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 6 | 60-100 | 34% | 99% | 100% | 0.98 | 100% | 98.7% | 0.98 (+/- 0.01) | 1.00 | >= 0.95 | Passed |
| 41-45 | 2 | 6 | 60-100 | 69% | 95% | 99% | 1.89 | 100% | 99.2% | 1.98 (+/- 0.02) | 1.05 | >= 0.95 | Passed |
| 41-45 | 3 | 6 | 60-100 | 103% | 98% | 99% | 2.92 | 100% | 99.3% | 2.94 (+/- 0.03) | 1.01 | >= 0.90 | Passed |
| 46-50 | 1 | 6 | 60-100 | 39% | 99% | 100% | 0.98 | 100% | 98.6% | 0.98 (+/- 0.01) | 1.00 | >= 0.95 | Passed |
| 46-50 | 2 | 6 | 60-100 | 79% | 99% | 100% | 1.98 | 100% | 98.7% | 1.97 (+/- 0.02) | 0.99 | >= 0.95 | Passed |
| 46-50 | 3 | 6 | 60-100 | 118% | 100% | 99% | 2.98 | 100% | 99.5% | 2.96 (+/- 0.02) | 0.99 | >= 0.90 | Passed |
| 51-53 | 1 | 6 | 60-70 | 7% | 97% | 38% | 0.36 | 100% | 100.0% | 1.00 (+/- 0.00) | 2.75 | >= 0.95 | Passed |
| 51-53 | 2 | 6 | 60-70 | 15% | 99% | 39% | 0.77 | 100% | 100.0% | 2.00 (+/- 0.00) | 2.61 | >= 0.95 | Passed |
| 51-53 | 3 | 6 | 60-70 | 22% | 98% | 33% | 0.97 | 100% | 100.0% | 2.98 (+/- 0.01) | 3.09 | >= 0.90 | Passed |

### Thresholds, first / second / third slot, that would meet parity for the quality measured (the strictest set that keeps every case at least at today's, never falling from one slot to the next)

| Scope | Thresholds that meet parity | In steps of 5 | Thresholds in force |
| --- | --- | --- | --- |
| Every band together (the shared thresholds) | 13% / 99% / 99% | 10% / 95% / 95% | 50% / 85% / 100% |
| Band 41-45 alone | 31% / 100% / 100% | 30% / 100% / 100% | 50% / 85% / 100% |
| Band 46-50 alone | 13% / 99% / 99% | 10% / 95% / 95% | 50% / 85% / 100% |
| Band 51-53 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 100% |

### Rank 50, fully modded gear: quality of the items the new minigame made, in whole percent, by band and slots filled

| Band | Slots filled | Items made | 10th percentile | 25th percentile | Median | 75th percentile | 90th percentile | Made at 100% |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 96% |
| 41-45 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 96% |
| 41-45 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 96% |
| 46-50 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 95% |
| 46-50 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 97% |
| 46-50 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 97% |
| 51-53 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 96% |
| 51-53 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 51-53 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |

### Representative enhancements: for each recipe kind the real item at the best level the recipe accepts (the old window refuses one more than 5 levels above the recipe) whose penalty is the lower median of that level

| Recipe kind | Enhancement | Level | Progress penalty | Items of that kind and level (min / median / max penalty) | Recipes |
| --- | --- | --- | --- | --- | --- |
| Weapon | Improved Agrinium V (`imp_agrinium_5`) | 45 (tier V) | 70 | 33 items: 50 / 70.0 / 80 | 1 |
| Armor | Armor Enhancement - Force Attack V (`aen_frcatk5`) | 45 (tier V) | 60 | 50 items: 50 / 60.0 / 150 | 4 |
| Starship | Starship Enhancement - Shield Recharge Rate V (`sen_shrech5`) | 45 (tier V) | 60 | 24 items: 50 / 65.0 / 80 | 2 |
| Module | Module Enhancement - Module Bonus V (`men_mod5`) | 45 (tier V) | 60 | 2 items: 60 / 70.0 / 80 | 8 |
| Droid | Droid Enhancement - Resistance - Ice II (`de_res_ice2`) | 50 (tier VI) | 100 | 8 items: 100 / 100.0 / 100 | 3 |

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
- Every craft in this scenario is played by a crafter who bought the Overclocker line in full (every purchase their Engineering rank allows) and plays Overclock, so no gate here is an unperked result.
