# Crafting revamp: balance gates of the Circuit (Technician line, Safe Mode) minigame

- Command: `SWLOR.CraftSim gates --scenario circuit-technician --gates rotation,length,materials,xp,enhancements --runs 30 --seed 1 --recipes-per-band 6 --repo . --out design/testing/crafting-revamp/circuit-technician-gates.md`
- Seed 1; 30 runs per case.

## Inputs

- Scenario: Circuit (Technician line, Safe Mode) (`circuit-technician`), Engineering recipes, minigame Circuit.
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
| No fixed rotation | Failed | One fixed rotation comes within 10 points of adaptive play in 6 case(s): 1-10 / no gear; 1-10 / mid gear; 1-10 / best gear; 11-20 / no gear; 41-53 / no gear; 41-53 / mid gear (smallest margin -16.3 in 41-53 / mid gear). The best rotation for each recipe alone, a reported metric, is -15.9 to 0.0 points behind adaptive play. |
| Length | Passed | Every band and gear tier takes 10-20 actions (item craft at level) on average. |
| Economy parity | Passed | Component stacks per item are within 10% of the legacy baseline where its success rate is at least 90%, and at most 10% above it where it is lower, at no less quality, in every band and gear tier. |
| XP rate | Passed | Item crafts: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.00x in 1-10 / mid gear, 1 enhancement). |
| Enhancement parity | Passed | At rank 50 in fully modded gear, the thresholds in force (50% / 85% / 100%) transfer at least 0.90 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 0.99 times today's). Thresholds that meet parity in every band for this quality: 99% / 99% / 99%. |

## No fixed rotation

### Average quality: adaptive play against the best single fixed rotation of each band and gear tier (quality points; a broken piece counts 0)

| Band | Gear | Recipes played | Adaptive avg quality | Best single rotation avg quality | Margin | Required | Best rotation per recipe avg quality (reported) | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 48.3 | 50.7 | -2.3 | >= 10 | 52.1 | Failed |
| 1-10 | mid gear | 6 | 84.1 | 99.4 | -15.3 | >= 10 | 100.0 | Failed |
| 1-10 | best gear | 6 | 91.5 | 99.8 | -8.2 | >= 10 | 99.7 | Failed |
| 1-10 | fully modded gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 11-20 | no gear | 6 | 81.6 | 85.2 | -3.6 | >= 10 | 84.8 | Failed |
| 11-20 | mid gear | 6 | 98.5 | 100.0 | -1.5 | >= 10 | 100.0 | Reported |
| 11-20 | best gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 11-20 | fully modded gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Reported |
| 21-30 | no gear | 6 | 98.7 | 99.7 | -1.0 | >= 10 | 99.5 | Reported |
| 21-30 | mid gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 21-30 | best gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 21-30 | fully modded gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 31-40 | no gear | 6 | 97.1 | 99.9 | -2.8 | >= 10 | 100.0 | Reported |
| 31-40 | mid gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 31-40 | best gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 31-40 | fully modded gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Reported |
| 41-53 | no gear | 6 | 65.5 | 63.3 | 2.2 | >= 10 | 74.2 | Failed |
| 41-53 | mid gear | 6 | 78.3 | 94.6 | -16.3 | >= 10 | 93.2 | Failed |
| 41-53 | best gear | 6 | 98.5 | 100.0 | -1.5 | >= 10 | 100.0 | Reported |
| 41-53 | fully modded gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |

### The single rotation found for each band and gear tier, searched across the band's sampled recipes (average quality in points)

| Band | Gear | Best single rotation for the band | Training | Selection | Hold-out | Adaptive on these recipes |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | wire > discharge_calibration > wire x3 > tune x2 > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire | 49.4 | 49.4 | 50.7 | 48.3 |
| 1-10 | mid gear | discharge_assembly > tune x3 > discharge_calibration > tune > wire > tune > discharge_calibration > wire > tune x2 > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire | 99.6 | 99.4 | 99.4 | 84.1 |
| 1-10 | best gear | discharge_assembly > patch > tune x3 > discharge_calibration > tune x3 > discharge_calibration > wire x2 > tune > discharge_assembly > wire x2 > tune > discharge_assembly x2 > wire > tune > wire | 99.7 | 99.6 | 99.8 | 91.5 |
| 1-10 | fully modded gear | tune x3 > discharge_calibration > tune x3 > wire x2 > discharge_assembly > tune > wire x3 | 100.0 | 100.0 | 100.0 | 100.0 |
| 11-20 | no gear | wire x4 > tune x2 > discharge_calibration > wire x4 > tune x3 > discharge_calibration > wire > discharge_calibration > wire > discharge_calibration > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire > discharge_calibration > wire | 86.1 | 86.1 | 85.2 | 81.6 |
| 11-20 | mid gear | wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire | 100.0 | 100.0 | 100.0 | 98.5 |
| 11-20 | best gear | boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost | 100.0 | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | tune x4 > discharge_calibration > tune > wire x3 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | wire x2 > boost > tune x4 > discharge_calibration > boost > tune x3 > discharge_calibration > wire x2 > discharge_calibration > wire x4 > discharge_calibration > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire | 99.7 | 99.6 | 99.7 | 98.7 |
| 21-30 | mid gear | boost > tune x4 > discharge_calibration > tune x5 > wire x4 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | boost > tune x4 > discharge_calibration > wire > tune x3 > wire x3 > discharge_assembly > tune > wire x2 | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | boost > tune x3 > discharge_calibration > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | patch > boost x2 > tune x2 > micro_tune > wire > discharge_calibration > boost x2 > wire > patch > tune x2 > discharge_calibration > wire x3 > tune > wire x2 > tune > wire x2 > discharge_calibration > boost x2 > wire > discharge_calibration > wire > discharge_calibration > wire | 99.9 | 99.9 | 99.9 | 97.1 |
| 31-40 | mid gear | boost > tune x5 > discharge_calibration > tune x7 > wire x2 > discharge_assembly > wire > tune > wire > patch > tune > wire > discharge_calibration > micro_tune > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | boost > tune x4 > discharge_calibration > boost > tune x6 > discharge_assembly > wire x3 > tune > wire > discharge_calibration > patch > tune > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | boost > tune x4 > discharge_calibration > tune x2 > wire x4 > discharge_calibration > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | boost x2 > tune x9 > micro_tune > wire x5 > micro_tune > discharge_assembly | 63.5 | 62.9 | 63.3 | 65.5 |
| 41-53 | mid gear | boost x2 > tune x4 > patch > discharge_calibration > tune x3 > patch > tune x7 > wire x5 > patch > discharge_calibration > wire x2 > discharge_calibration > wire | 95.2 | 94.9 | 94.6 | 78.3 |
| 41-53 | best gear | wire x2 > boost > tune x4 > patch > tune > discharge_calibration > wire > tune > boost > tune x5 > boost > wire x2 > discharge_calibration > wire x3 > patch > tune > wire x4 | 100.0 | 100.0 | 100.0 | 98.5 |
| 41-53 | fully modded gear | boost > tune x4 > discharge_calibration > boost > tune x4 > discharge_assembly > wire > tune > wire > tune > patch > discharge_calibration > wire | 100.0 | 100.0 | 100.0 | 100.0 |

### Reported metric, not gated: the best fixed rotation for each recipe alone, and what the band's single rotation scores on the same recipe (average quality in points)

| Band | Gear | Recipe | Best rotation for this recipe alone (reported) | Its hold-out | The single rotation on this recipe | Adaptive on this recipe |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | wen_cft_smth1 (level 3) | wire x5 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire | 37.5 | 36.3 | 34.3 |
| 1-10 | no gear | config_ind1 (level 5) | wire x2 > tune > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire x2 > discharge_calibration > wire | 44.6 | 45.8 | 47.3 |
| 1-10 | no gear | d_hd_pva1 (level 7) | wire x2 > patch > wire x3 > tune > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire > discharge_calibration > wire | 53.0 | 47.9 | 45.2 |
| 1-10 | no gear | song_460 (level 8) | wire x3 > tune x2 > discharge_calibration > wire x4 > discharge_calibration > wire x3 > discharge_calibration > wire > discharge_calibration > wire > discharge_calibration > wire | 59.6 | 56.5 | 53.8 |
| 1-10 | no gear | d_lg_pvw1 (level 10) | wire > discharge_calibration > wire x2 > tune x2 > discharge_calibration > wire x4 > discharge_calibration > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire | 58.0 | 60.5 | 53.2 |
| 1-10 | no gear | id_soulstrike1 (level 10) | wire x3 > tune x2 > discharge_calibration > wire x5 > discharge_calibration > wire > discharge_calibration > wire > discharge_calibration > wire > discharge_calibration > wire | 59.7 | 57.0 | 56.2 |
| 1-10 | mid gear | wen_cft_smth1 (level 3) | tune x5 > discharge_calibration > tune x2 > wire > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire | 100.0 | 96.6 | 87.1 |
| 1-10 | mid gear | config_ind1 (level 5) | discharge_assembly > tune x3 > discharge_calibration > tune > wire > tune > discharge_calibration > tune > wire > tune > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire | 100.0 | 100.0 | 51.0 |
| 1-10 | mid gear | d_hd_pva1 (level 7) | tune x5 > discharge_calibration > tune x3 > wire x2 > discharge_calibration > wire x4 > discharge_assembly > wire | 100.0 | 100.0 | 94.3 |
| 1-10 | mid gear | song_460 (level 8) | boost > tune > discharge_calibration > tune x4 > discharge_calibration > tune > wire x2 > discharge_calibration > tune > wire x3 > discharge_calibration > wire x4 | 100.0 | 100.0 | 88.2 |
| 1-10 | mid gear | d_lg_pvw1 (level 10) | tune x3 > discharge_calibration > tune x3 > discharge_calibration > tune > wire > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire x3 | 100.0 | 100.0 | 95.7 |
| 1-10 | mid gear | id_soulstrike1 (level 10) | tune x5 > discharge_calibration > tune x4 > wire > discharge_calibration > wire x5 > discharge_assembly > wire x2 | 100.0 | 99.9 | 88.3 |
| 1-10 | best gear | wen_cft_smth1 (level 3) | tune x5 > discharge_calibration > tune x4 > wire > discharge_assembly > wire x3 > tune > discharge_assembly > wire | 100.0 | 99.0 | 86.6 |
| 1-10 | best gear | config_ind1 (level 5) | wire > tune x3 > discharge_calibration > tune x3 > discharge_calibration > wire x4 > discharge_calibration > wire x3 > discharge_assembly > wire | 98.2 | 99.7 | 88.3 |
| 1-10 | best gear | d_hd_pva1 (level 7) | tune x5 > discharge_calibration > tune x3 > wire x2 > discharge_assembly > wire > tune > wire x3 | 100.0 | 100.0 | 96.4 |
| 1-10 | best gear | song_460 (level 8) | tune x4 > discharge_calibration > tune x3 > discharge_calibration > tune x2 > wire x2 > discharge_assembly > wire x5 | 100.0 | 100.0 | 90.3 |
| 1-10 | best gear | d_lg_pvw1 (level 10) | discharge_assembly > tune x3 > discharge_calibration > tune x5 > patch > tune x2 > wire > discharge_assembly > wire x3 > discharge_calibration > wire x2 > discharge_assembly > wire x2 | 100.0 | 99.9 | 88.5 |
| 1-10 | best gear | id_soulstrike1 (level 10) | tune x5 > discharge_calibration > tune x3 > wire > discharge_calibration > wire x5 > discharge_assembly > wire | 100.0 | 100.0 | 99.0 |
| 1-10 | fully modded gear | wen_cft_smth1 (level 3) | tune x5 > discharge_calibration > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | config_ind1 (level 5) | tune x3 > discharge_calibration > tune x3 > wire x5 | 100.0 | 100.0 | 99.8 |
| 1-10 | fully modded gear | d_hd_pva1 (level 7) | boost > tune x2 > discharge_calibration > tune x2 > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | song_460 (level 8) | tune x4 > discharge_calibration > tune x2 > wire x5 | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | d_lg_pvw1 (level 10) | tune x3 > discharge_calibration > tune x3 > wire x5 | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | id_soulstrike1 (level 10) | tune x5 > discharge_calibration > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | no gear | dhbr002 (level 13) | discharge_assembly > wire x2 > tune x3 > discharge_calibration > wire x3 > discharge_calibration > wire x4 > discharge_calibration > wire > discharge_calibration > wire | 69.8 | 70.7 | 61.2 |
| 11-20 | no gear | song_579 (level 15) | discharge_assembly > discharge_calibration > wire x2 > boost > tune x2 > discharge_calibration > wire x3 > tune > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire > discharge_calibration > wire > discharge_calibration > wire | 75.5 | 75.1 | 75.2 |
| 11-20 | no gear | eva_boost_1 (level 18) | wire > tune x3 > discharge_calibration > wire x4 > tune > wire > discharge_calibration > wire x4 > discharge_calibration > wire > discharge_calibration > wire | 78.8 | 73.4 | 81.7 |
| 11-20 | no gear | d_ar_pvs2 (level 19) | boost > tune x3 > discharge_calibration > wire x5 > discharge_calibration > wire > discharge_calibration > wire x4 > discharge_calibration > wire | 88.2 | 94.5 | 89.5 |
| 11-20 | no gear | darg002 (level 20) | wire x3 > tune x5 > discharge_calibration > wire x4 > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire | 100.0 | 100.0 | 97.4 |
| 11-20 | no gear | id_pinningtoss1 (level 20) | wire x2 > tune x5 > discharge_calibration > wire x7 > discharge_calibration > wire > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire > discharge_calibration > wire > discharge_calibration > wire | 96.7 | 97.7 | 84.7 |
| 11-20 | mid gear | dhbr002 (level 13) | wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire | 100.0 | 100.0 | 95.4 |
| 11-20 | mid gear | song_579 (level 15) | boost > tune x3 > discharge_calibration > tune x4 > discharge_calibration > tune > wire x5 > discharge_assembly > wire x2 | 100.0 | 100.0 | 98.2 |
| 11-20 | mid gear | eva_boost_1 (level 18) | tune x5 > discharge_calibration > tune x3 > discharge_calibration > wire x3 > patch > wire > discharge_calibration > wire x4 > discharge_calibration > wire > discharge_calibration > wire | 100.0 | 100.0 | 97.8 |
| 11-20 | mid gear | d_ar_pvs2 (level 19) | tune x5 > discharge_calibration > tune x3 > discharge_calibration > wire > tune > wire x3 > discharge_assembly > wire x3 | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | darg002 (level 20) | boost > tune x6 > discharge_calibration > boost > wire x4 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | id_pinningtoss1 (level 20) | tune x7 > discharge_calibration > wire > tune x2 > wire x4 > discharge_assembly > wire > tune > wire | 100.0 | 100.0 | 99.8 |
| 11-20 | best gear | dhbr002 (level 13) | tune x4 > discharge_calibration > tune x4 > discharge_calibration > wire x4 > discharge_assembly > wire x3 | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | song_579 (level 15) | tune x6 > discharge_calibration > tune x2 > wire x4 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | eva_boost_1 (level 18) | tune x4 > discharge_calibration > tune x4 > discharge_calibration > wire x8 | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | d_ar_pvs2 (level 19) | tune x5 > discharge_calibration > tune x3 > wire x5 > tune > wire x2 | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | darg002 (level 20) | boost > tune x5 > discharge_calibration > boost > wire x4 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | id_pinningtoss1 (level 20) | tune x7 > discharge_calibration > tune > wire x5 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | dhbr002 (level 13) | tune x4 > discharge_calibration > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | song_579 (level 15) | boost > tune x3 > discharge_calibration > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | eva_boost_1 (level 18) | boost > tune > discharge_calibration > tune x4 > wire x5 | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | d_ar_pvs2 (level 19) | boost > tune x2 > discharge_calibration > tune x2 > wire x4 | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | darg002 (level 20) | boost > tune x3 > discharge_calibration > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | id_pinningtoss1 (level 20) | boost > tune x3 > discharge_calibration > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | wen_cft_smth3 (level 23) | boost x2 > tune x3 > wire > tune > discharge_calibration > wire x8 > discharge_calibration > wire | 96.9 | 98.3 | 92.5 |
| 21-30 | no gear | ion_armor_3 (level 25) | boost > tune x4 > discharge_calibration > tune x6 > wire x3 > discharge_assembly > wire > tune > wire x2 | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | d_lg_vws3 (level 27) | boost > tune x5 > discharge_calibration > tune > wire > tune > wire x4 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | dlbe003 (level 29) | boost > tune x4 > discharge_calibration > tune x5 > wire x4 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | dsbr003 (level 30) | boost > tune x5 > discharge_calibration > tune x2 > wire > tune > wire x4 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | id_linebreaker3 (level 30) | boost > tune x5 > discharge_calibration > tune x4 > wire x4 > discharge_assembly > tune > wire x2 > tune > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | wen_cft_smth3 (level 23) | tune x7 > discharge_calibration > tune x2 > wire x2 > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | ion_armor_3 (level 25) | boost > tune x4 > discharge_calibration > tune x4 > wire x4 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | d_lg_vws3 (level 27) | boost > tune x5 > discharge_calibration > tune > wire x5 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | dlbe003 (level 29) | boost > tune x5 > discharge_calibration > tune x2 > wire x5 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | dsbr003 (level 30) | boost > tune x5 > discharge_calibration > boost > tune > wire x4 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | id_linebreaker3 (level 30) | boost > tune x4 > discharge_calibration > tune x5 > wire x3 > discharge_assembly > wire > tune > wire x2 | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | wen_cft_smth3 (level 23) | tune x7 > discharge_calibration > wire > tune > wire x4 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | ion_armor_3 (level 25) | boost > tune x4 > discharge_calibration > tune x3 > wire x4 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | d_lg_vws3 (level 27) | boost > tune x5 > discharge_calibration > boost > wire x4 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | dlbe003 (level 29) | boost > tune x4 > discharge_calibration > boost > tune x2 > wire x3 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | dsbr003 (level 30) | boost > tune x5 > discharge_calibration > boost > wire x4 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | id_linebreaker3 (level 30) | boost > tune x4 > discharge_calibration > tune x4 > wire x4 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | wen_cft_smth3 (level 23) | boost > tune x3 > discharge_calibration > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | ion_armor_3 (level 25) | boost > tune x3 > discharge_calibration > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | d_lg_vws3 (level 27) | boost > tune x3 > discharge_calibration > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | dlbe003 (level 29) | boost > tune x3 > discharge_calibration > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | dsbr003 (level 30) | boost > tune x3 > discharge_calibration > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | id_linebreaker3 (level 30) | boost > tune x3 > discharge_calibration > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | aen_cft_fab4 (level 33) | boost > tune x4 > discharge_calibration > tune x6 > discharge_calibration > boost > wire x5 > discharge_assembly > tune > wire x2 | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | config_ind4 (level 35) | boost > tune x5 > discharge_calibration > boost > tune x3 > discharge_calibration > wire > tune > wire x5 > discharge_assembly > tune x2 > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | d_lg_mws4 (level 37) | boost x2 > tune x3 > micro_tune > discharge_calibration > tune > boost x2 > tune > wire > micro_tune > discharge_calibration > wire x2 > boost x2 > wire > discharge_assembly > wire x2 | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | d_hd_mwa4 (level 39) | boost > tune x6 > discharge_calibration > boost > tune x4 > wire x2 > discharge_assembly > tune > wire x2 > tune > micro_tune > wire | 100.0 | 100.0 | 93.8 |
| 31-40 | no gear | dfhl004 (level 40) | boost > tune x6 > discharge_calibration > wire > tune x6 > wire x2 > discharge_assembly > wire > tune > wire | 100.0 | 99.9 | 93.2 |
| 31-40 | no gear | id_conctoss2 (level 40) | tune > boost > tune x8 > discharge_calibration > tune x10 > wire x5 > discharge_assembly | 99.7 | 99.6 | 95.8 |
| 31-40 | mid gear | aen_cft_fab4 (level 33) | boost > tune x4 > discharge_calibration > tune x6 > wire x2 > discharge_assembly > tune > wire x3 > tune > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | config_ind4 (level 35) | boost > tune x5 > discharge_calibration > boost > tune x3 > discharge_calibration > boost > wire x5 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | d_lg_mws4 (level 37) | boost > tune x5 > discharge_calibration > boost > tune x5 > wire > discharge_assembly > tune > wire x4 | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | d_hd_mwa4 (level 39) | boost > tune x6 > discharge_calibration > boost > tune x3 > wire x2 > discharge_assembly > wire > tune > wire > tune > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | dfhl004 (level 40) | boost > tune x6 > discharge_calibration > boost > tune x3 > wire x2 > discharge_assembly > wire > tune > wire > tune > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | id_conctoss2 (level 40) | boost > tune x4 > discharge_calibration > tune x10 > patch > discharge_assembly > wire x5 > discharge_calibration > micro_tune > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | aen_cft_fab4 (level 33) | boost > tune x4 > discharge_calibration > tune x5 > wire x3 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | config_ind4 (level 35) | boost > tune x4 > discharge_calibration > tune x6 > wire x2 > discharge_assembly > tune > wire x2 | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | d_lg_mws4 (level 37) | boost > tune x4 > discharge_calibration > boost > tune x5 > discharge_assembly > tune > wire x4 | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | d_hd_mwa4 (level 39) | boost > tune x6 > discharge_calibration > tune x2 > wire x4 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | dfhl004 (level 40) | boost > tune x5 > discharge_calibration > boost > tune x4 > wire > discharge_assembly > tune > wire x3 | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | id_conctoss2 (level 40) | boost > tune x4 > discharge_calibration > tune x10 > wire > discharge_assembly > wire x3 | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | aen_cft_fab4 (level 33) | boost > tune x4 > discharge_calibration > wire x5 | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | config_ind4 (level 35) | boost > tune x4 > discharge_calibration > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | d_lg_mws4 (level 37) | boost > tune x4 > discharge_calibration > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | d_hd_mwa4 (level 39) | boost > tune x4 > discharge_calibration > tune > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | dfhl004 (level 40) | boost > tune x4 > discharge_calibration > wire > tune > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | id_conctoss2 (level 40) | boost > tune x4 > discharge_calibration > tune x3 > wire x2 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | dhbr005 (level 43) | boost > tune x2 > boost > tune > discharge_calibration > wire > boost x2 > tune x6 > micro_tune > wire x5 > discharge_calibration > wire > tune > wire | 100.0 | 77.3 | 100.0 |
| 41-53 | no gear | therm_amp_4 (level 46) | wire > boost > tune x7 > wire x3 > tune > wire > tune > wire > discharge_calibration > wire x2 | 86.6 | 74.0 | 81.6 |
| 41-53 | no gear | wen_eva5 (level 48) | boost x2 > tune x9 > micro_tune > wire x5 > micro_tune > discharge_assembly | 54.4 | 54.4 | 33.2 |
| 41-53 | no gear | d_lg_mwa5 (level 49) | boost x2 > tune x10 > wire x2 > tune > wire > micro_tune > discharge_assembly | 81.3 | 70.3 | 85.5 |
| 41-53 | no gear | id_infusion2 (level 50) | boost > tune > boost > tune x7 > wire > tune x3 > wire x3 > tune x3 > discharge_assembly | 66.2 | 54.5 | 40.6 |
| 41-53 | no gear | cap_quadlas1 (level 52) | wire x3 > boost x2 > tune x9 > wire x2 > tune > wire > tune > discharge_assembly | 56.4 | 49.6 | 52.4 |
| 41-53 | mid gear | dhbr005 (level 43) | wire x2 > boost > tune x5 > discharge_calibration > boost > tune x7 > wire x2 > discharge_assembly > tune > micro_tune > wire | 100.0 | 100.0 | 99.9 |
| 41-53 | mid gear | therm_amp_4 (level 46) | boost x3 > tune > discharge_calibration > boost > tune > patch > tune x10 > wire x3 > micro_tune > wire x2 > patch > discharge_calibration > wire | 100.0 | 100.0 | 99.9 |
| 41-53 | mid gear | wen_eva5 (level 48) | boost x2 > tune x4 > discharge_calibration > tune x3 > patch > tune x7 > wire x5 > patch > discharge_calibration > wire x2 > discharge_calibration > wire | 94.4 | 94.7 | 63.9 |
| 41-53 | mid gear | d_lg_mwa5 (level 49) | wire x2 > boost x2 > tune x8 > patch > discharge_calibration > wire > micro_tune > tune > wire x4 > discharge_calibration > patch > wire x2 | 100.0 | 100.0 | 98.1 |
| 41-53 | mid gear | id_infusion2 (level 50) | boost > tune x2 > patch > boost > tune x5 > discharge_calibration > tune > wire > tune x2 > wire > tune x2 > wire x5 > patch > discharge_calibration > wire | 94.7 | 95.0 | 53.4 |
| 41-53 | mid gear | cap_quadlas1 (level 52) | boost x2 > tune x10 > wire x3 > tune > wire > tune > discharge_assembly | 70.0 | 77.6 | 54.7 |
| 41-53 | best gear | dhbr005 (level 43) | boost > tune x4 > discharge_calibration > boost > tune x7 > wire > discharge_assembly > wire > tune x2 > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | therm_amp_4 (level 46) | boost > tune x13 > wire > discharge_assembly > patch > wire > tune > discharge_calibration > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | wen_eva5 (level 48) | wire > tune > boost > tune > micro_tune > tune x2 > patch > tune > discharge_calibration > tune x4 > micro_tune > tune x2 > micro_tune > wire > patch > wire x4 > discharge_calibration > wire x2 | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | d_lg_mwa5 (level 49) | discharge_assembly > boost > wire > tune x11 > boost > patch > discharge_calibration > micro_tune > boost > discharge_calibration > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | id_infusion2 (level 50) | tune > boost > tune > boost > tune x2 > patch > discharge_calibration > boost > wire > tune x5 > micro_tune > tune > discharge_calibration > tune > boost > patch > wire x4 > discharge_assembly | 100.0 | 100.0 | 99.9 |
| 41-53 | best gear | cap_quadlas1 (level 52) | tune > boost > tune > boost > tune x3 > patch > discharge_calibration > tune x3 > wire > boost > wire > discharge_calibration > boost > tune > boost > tune x7 > wire x4 > discharge_calibration > patch > tune > wire x3 | 100.0 | 100.0 | 91.1 |
| 41-53 | fully modded gear | dhbr005 (level 43) | boost > tune x4 > discharge_calibration > tune x2 > wire > tune > wire > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | fully modded gear | therm_amp_4 (level 46) | boost > tune x4 > discharge_calibration > tune x3 > wire > tune > wire > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | fully modded gear | wen_eva5 (level 48) | tune x9 > discharge_calibration > wire x5 | 100.0 | 100.0 | 99.9 |
| 41-53 | fully modded gear | d_lg_mwa5 (level 49) | boost > tune x5 > discharge_calibration > tune > wire > tune > wire x3 | 100.0 | 100.0 | 100.0 |
| 41-53 | fully modded gear | id_infusion2 (level 50) | boost > tune x4 > discharge_calibration > tune x6 > wire > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | fully modded gear | cap_quadlas1 (level 52) | boost > tune x5 > discharge_calibration > boost > tune x4 > wire > discharge_assembly > tune x2 > wire | 100.0 | 100.0 | 100.0 |

## Length

### Actions per attempt of the adaptive player at level (item craft at level)

| Band | Gear | Recipes played | Mean actions per attempt | Shortest recipe | Longest recipe | Stalled attempts | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 14.5 | 12.1 | 16.3 | 0 | 10-20 | Passed |
| 1-10 | mid gear | 6 | 16.9 | 15.7 | 18.0 | 0 | 10-20 | Passed |
| 1-10 | best gear | 6 | 15.6 | 13.6 | 17.3 | 0 | 10-20 | Passed |
| 1-10 | fully modded gear | 6 | 10.0 | 9.1 | 10.4 | 0 | 10-20 | Reported |
| 11-20 | no gear | 6 | 16.4 | 15.0 | 18.6 | 0 | 10-20 | Passed |
| 11-20 | mid gear | 6 | 16.3 | 14.1 | 18.3 | 0 | 10-20 | Passed |
| 11-20 | best gear | 6 | 14.3 | 12.1 | 15.6 | 0 | 10-20 | Passed |
| 11-20 | fully modded gear | 6 | 9.1 | 7.7 | 9.9 | 0 | 10-20 | Reported |
| 21-30 | no gear | 6 | 16.0 | 14.2 | 17.0 | 0 | 10-20 | Passed |
| 21-30 | mid gear | 6 | 14.2 | 12.5 | 15.0 | 0 | 10-20 | Passed |
| 21-30 | best gear | 6 | 12.7 | 10.7 | 14.0 | 0 | 10-20 | Passed |
| 21-30 | fully modded gear | 6 | 8.2 | 7.5 | 8.9 | 0 | 10-20 | Reported |
| 31-40 | no gear | 6 | 19.4 | 17.4 | 21.6 | 0 | 10-20 | Reported |
| 31-40 | mid gear | 6 | 17.6 | 15.7 | 20.7 | 0 | 10-20 | Passed |
| 31-40 | best gear | 6 | 15.4 | 14.1 | 17.5 | 0 | 10-20 | Passed |
| 31-40 | fully modded gear | 6 | 9.9 | 9.1 | 10.4 | 0 | 10-20 | Reported |
| 41-53 | no gear | 6 | 18.4 | 17.5 | 19.9 | 0 | 10-20 | Reported |
| 41-53 | mid gear | 6 | 19.1 | 16.9 | 20.9 | 0 | 10-20 | Reported |
| 41-53 | best gear | 6 | 18.3 | 15.6 | 22.4 | 0 | 10-20 | Passed |
| 41-53 | fully modded gear | 6 | 11.3 | 9.9 | 12.6 | 0 | 10-20 | Reported |

## Economy parity

### Component stacks consumed per item made, failures included, over the recipes the legacy policy makes at least once in the cell (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Parity rule | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 2.33 | 2.37 | 0.98 | 100% | 98% | 48% | 1% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | mid gear | 6 | 2.46 | 2.37 | 1.04 | 93% | 98% | 84% | 10% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | best gear | 6 | 2.42 | 2.35 | 1.03 | 94% | 99% | 92% | 25% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | fully modded gear | 6 | 2.33 | 2.34 | 1.00 | 100% | 99% | 100% | 75% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | no gear | 6 | 2.00 | 2.10 | 0.95 | 100% | 93% | 84% | 3% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | mid gear | 6 | 2.00 | 2.02 | 0.99 | 100% | 98% | 99% | 13% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | best gear | 6 | 2.00 | 2.03 | 0.99 | 100% | 98% | 100% | 28% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | fully modded gear | 6 | 2.00 | 2.01 | 0.99 | 100% | 99% | 100% | 91% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | no gear | 6 | 2.50 | 2.55 | 0.98 | 100% | 97% | 99% | 8% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | mid gear | 6 | 2.50 | 2.53 | 0.99 | 100% | 98% | 100% | 15% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | best gear | 6 | 2.50 | 2.51 | 1.00 | 100% | 99% | 100% | 29% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | fully modded gear | 6 | 2.50 | 2.52 | 0.99 | 100% | 99% | 100% | 93% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | no gear | 6 | 2.53 | 2.53 | 1.00 | 98% | 98% | 97% | 23% | yes | 0% / 0% | within 10% | Reported |
| 31-40 | mid gear | 6 | 2.50 | 2.53 | 0.99 | 100% | 98% | 100% | 35% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | best gear | 6 | 2.50 | 2.52 | 0.99 | 100% | 99% | 100% | 48% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | fully modded gear | 6 | 2.50 | 2.54 | 0.98 | 100% | 97% | 100% | 85% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | no gear | 6 | 2.64 | 2.42 | 1.09 | 85% | 99% | 68% | 42% | yes | 0% / 17% | within 10% | Reported |
| 41-53 | mid gear | 6 | 2.69 | 2.72 | 0.99 | 88% | 87% | 78% | 55% | yes | 0% / 0% | at most +10% | Reported |
| 41-53 | best gear | 6 | 2.50 | 2.59 | 0.97 | 100% | 94% | 98% | 76% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | fully modded gear | 6 | 2.50 | 2.54 | 0.98 | 100% | 98% | 100% | 89% | yes | 0% / 0% | within 10% | Passed |

### Recipes the legacy policy never makes in the cell, left out of the stack and quality comparison above (they still count in the never-made check)

| Band | Gear | Recipe | Level | New success | New avg quality |
| --- | --- | --- | --- | --- | --- |
| 41-53 | no gear | QuadLaserCannon1 | 52 | 97% | 52% |

## XP rate

### Item crafts: XP per hour against the legacy loop (6.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per craft | Legacy actions per craft | Legacy XP per hour | New XP per craft | New actions per craft | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 595 | 5.8 | 61824 | 2038 | 14.5 | 63161 | 1.02 | - | Passed |
| 1-10 | no gear, 1 enhancement | 429 | 6.4 | 40203 | 2149 | 18.9 | 51126 | 1.27 | - | Passed |
| 1-10 | no gear, 2 enhancements | 184 | 6.0 | 18350 | 2116 | 18.8 | 50653 | 2.76 | - | Passed |
| 1-10 | no gear, 3 enhancements | 90 | 6.0 | 9000 | 2158 | 18.8 | 51664 | 5.74 | - | Passed |
| 1-10 | mid gear | 649 | 7.5 | 51840 | 2445 | 16.9 | 64978 | 1.25 | - | Passed |
| 1-10 | mid gear, 1 enhancement | 603 | 7.9 | 45828 | 2015 | 19.8 | 45917 | 1.00 | - | Passed |
| 1-10 | mid gear, 2 enhancements | 578 | 8.4 | 41136 | 2127 | 20.0 | 47775 | 1.16 | - | Passed |
| 1-10 | mid gear, 3 enhancements | 529 | 8.9 | 35636 | 2160 | 20.2 | 48045 | 1.35 | - | Passed |
| 1-10 | best gear | 744 | 8.8 | 50836 | 2567 | 15.6 | 74257 | 1.46 | - | Passed |
| 1-10 | best gear, 1 enhancement | 722 | 9.2 | 47316 | 2343 | 18.1 | 58090 | 1.23 | - | Passed |
| 1-10 | best gear, 2 enhancements | 719 | 9.6 | 44992 | 2298 | 18.2 | 56893 | 1.26 | - | Passed |
| 1-10 | best gear, 3 enhancements | 687 | 10.0 | 41012 | 2329 | 18.5 | 56521 | 1.38 | - | Passed |
| 1-10 | fully modded gear | 1047 | 7.7 | 82045 | 2747 | 10.0 | 123223 | 1.50 | - | Passed |
| 1-10 | fully modded gear, 1 enhancement | 1031 | 7.7 | 80482 | 2748 | 11.0 | 112874 | 1.40 | - | Passed |
| 1-10 | fully modded gear, 2 enhancements | 1019 | 8.0 | 76508 | 2743 | 10.9 | 112966 | 1.48 | - | Passed |
| 1-10 | fully modded gear, 3 enhancements | 1018 | 8.5 | 71761 | 2734 | 11.2 | 110051 | 1.53 | - | Passed |
| 11-20 | no gear | 579 | 7.9 | 44148 | 1678 | 16.4 | 45926 | 1.04 | - | Passed |
| 11-20 | no gear, 1 enhancement | 389 | 8.6 | 27294 | 1712 | 18.4 | 41953 | 1.54 | - | Passed |
| 11-20 | no gear, 2 enhancements | 265 | 7.3 | 21917 | 1720 | 18.5 | 41859 | 1.91 | - | Passed |
| 11-20 | no gear, 3 enhancements | 252 | 8.1 | 18738 | 1728 | 18.5 | 42146 | 2.25 | - | Passed |
| 11-20 | mid gear | 667 | 10.2 | 39192 | 1834 | 16.3 | 50763 | 1.30 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 635 | 11.6 | 32743 | 1828 | 17.2 | 47819 | 1.46 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 616 | 12.8 | 28933 | 1820 | 17.7 | 46345 | 1.60 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 537 | 13.9 | 23243 | 1826 | 17.8 | 46052 | 1.98 | - | Passed |
| 11-20 | best gear | 761 | 14.5 | 31427 | 1848 | 14.3 | 58062 | 1.85 | - | Passed |
| 11-20 | best gear, 1 enhancement | 749 | 15.4 | 29203 | 1848 | 15.5 | 53689 | 1.84 | - | Passed |
| 11-20 | best gear, 2 enhancements | 740 | 16.0 | 27777 | 1844 | 15.5 | 53548 | 1.93 | - | Passed |
| 11-20 | best gear, 3 enhancements | 717 | 16.5 | 26050 | 1846 | 15.4 | 53891 | 2.07 | - | Passed |
| 11-20 | fully modded gear | 1146 | 14.4 | 47678 | 1848 | 9.1 | 91051 | 1.91 | - | Passed |
| 11-20 | fully modded gear, 1 enhancement | 1147 | 14.5 | 47531 | 1848 | 9.7 | 85974 | 1.81 | - | Passed |
| 11-20 | fully modded gear, 2 enhancements | 1133 | 14.6 | 46455 | 1848 | 9.7 | 85487 | 1.84 | - | Passed |
| 11-20 | fully modded gear, 3 enhancements | 1134 | 14.9 | 45749 | 1848 | 9.8 | 85192 | 1.86 | - | Passed |
| 21-30 | no gear | 633 | 11.3 | 33571 | 1371 | 16.0 | 38528 | 1.15 | - | Passed |
| 21-30 | no gear, 1 enhancement | 617 | 12.2 | 30271 | 1380 | 16.3 | 37990 | 1.25 | - | Passed |
| 21-30 | no gear, 2 enhancements | 605 | 12.9 | 28088 | 1380 | 16.4 | 37960 | 1.35 | - | Passed |
| 21-30 | no gear, 3 enhancements | 559 | 14.1 | 23889 | 1380 | 16.4 | 37925 | 1.59 | - | Passed |
| 21-30 | mid gear | 683 | 16.8 | 24372 | 1380 | 14.2 | 43730 | 1.79 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 677 | 17.5 | 23193 | 1380 | 14.6 | 42648 | 1.84 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 671 | 17.5 | 23035 | 1380 | 14.4 | 43105 | 1.87 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 652 | 18.0 | 21772 | 1380 | 14.6 | 42466 | 1.95 | - | Passed |
| 21-30 | best gear | 773 | 22.6 | 20498 | 1380 | 12.7 | 48873 | 2.38 | - | Passed |
| 21-30 | best gear, 1 enhancement | 763 | 22.7 | 20201 | 1380 | 12.8 | 48684 | 2.41 | - | Passed |
| 21-30 | best gear, 2 enhancements | 755 | 23.2 | 19561 | 1380 | 12.7 | 49024 | 2.51 | - | Passed |
| 21-30 | best gear, 3 enhancements | 748 | 23.3 | 19290 | 1380 | 12.8 | 48472 | 2.51 | - | Passed |
| 21-30 | fully modded gear | 1151 | 22.1 | 31253 | 1380 | 8.2 | 75626 | 2.42 | - | Passed |
| 21-30 | fully modded gear, 1 enhancement | 1109 | 22.6 | 29432 | 1380 | 8.4 | 73728 | 2.51 | - | Passed |
| 21-30 | fully modded gear, 2 enhancements | 1129 | 22.7 | 29785 | 1380 | 8.3 | 75167 | 2.52 | - | Passed |
| 21-30 | fully modded gear, 3 enhancements | 1121 | 22.7 | 29589 | 1380 | 8.4 | 73779 | 2.49 | - | Passed |
| 31-40 | no gear | 726 | 14.8 | 29537 | 1174 | 19.4 | 27168 | 0.92 | - | Passed |
| 31-40 | no gear, 1 enhancement | 680 | 14.5 | 28186 | 1043 | 19.9 | 23561 | 0.84 | - | Passed |
| 31-40 | no gear, 2 enhancements | 631 | 15.1 | 25051 | 1044 | 19.9 | 23576 | 0.94 | - | Passed |
| 31-40 | no gear, 3 enhancements | 583 | 16.0 | 21889 | 1012 | 20.0 | 22760 | 1.04 | - | Passed |
| 31-40 | mid gear | 799 | 18.8 | 25485 | 1200 | 17.6 | 30651 | 1.20 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 783 | 19.4 | 24165 | 1193 | 18.4 | 29116 | 1.20 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 743 | 19.2 | 23152 | 1200 | 18.6 | 29002 | 1.25 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 674 | 18.4 | 21987 | 1187 | 18.6 | 28689 | 1.30 | - | Passed |
| 31-40 | best gear | 884 | 24.6 | 21596 | 1200 | 15.4 | 35076 | 1.62 | - | Passed |
| 31-40 | best gear, 1 enhancement | 887 | 25.3 | 21027 | 1200 | 15.4 | 34999 | 1.66 | - | Passed |
| 31-40 | best gear, 2 enhancements | 859 | 25.0 | 20580 | 1200 | 15.6 | 34598 | 1.68 | - | Passed |
| 31-40 | best gear, 3 enhancements | 817 | 23.7 | 20695 | 1200 | 15.6 | 34640 | 1.67 | - | Passed |
| 31-40 | fully modded gear | 1093 | 25.3 | 25966 | 1200 | 9.9 | 54761 | 2.11 | - | Passed |
| 31-40 | fully modded gear, 1 enhancement | 1109 | 25.6 | 25990 | 1200 | 9.9 | 54483 | 2.10 | - | Passed |
| 31-40 | fully modded gear, 2 enhancements | 1115 | 26.4 | 25368 | 1200 | 10.0 | 54119 | 2.13 | - | Passed |
| 31-40 | fully modded gear, 3 enhancements | 1107 | 26.6 | 24967 | 1200 | 9.9 | 54606 | 2.19 | - | Passed |
| 41-53 | no gear | 725 | 17.5 | 24801 | 962 | 18.4 | 23533 | 0.95 | - | Passed |
| 41-53 | no gear, 1 enhancement | 638 | 16.0 | 23876 | 959 | 19.9 | 21643 | 0.91 | - | Passed |
| 41-53 | no gear, 2 enhancements | 510 | 16.4 | 18665 | 912 | 19.9 | 20644 | 1.11 | - | Passed |
| 41-53 | no gear, 3 enhancements | 447 | 17.4 | 15438 | 910 | 20.0 | 20486 | 1.33 | - | Passed |
| 41-53 | mid gear | 868 | 23.9 | 21755 | 1044 | 19.1 | 24600 | 1.13 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 788 | 22.6 | 20965 | 1025 | 20.8 | 22142 | 1.06 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 684 | 20.0 | 20552 | 1048 | 20.8 | 22726 | 1.11 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 596 | 19.2 | 18602 | 1031 | 21.1 | 21967 | 1.18 | - | Passed |
| 41-53 | best gear | 1044 | 28.5 | 22007 | 1239 | 18.3 | 30458 | 1.38 | - | Passed |
| 41-53 | best gear, 1 enhancement | 970 | 27.8 | 20982 | 1188 | 18.9 | 28280 | 1.35 | - | Passed |
| 41-53 | best gear, 2 enhancements | 885 | 26.0 | 20448 | 1206 | 19.0 | 28493 | 1.39 | - | Passed |
| 41-53 | best gear, 3 enhancements | 776 | 23.3 | 19995 | 1146 | 19.1 | 26996 | 1.35 | - | Passed |
| 41-53 | fully modded gear | 1157 | 27.7 | 25051 | 1250 | 11.3 | 49653 | 1.98 | - | Passed |
| 41-53 | fully modded gear, 1 enhancement | 1159 | 28.2 | 24692 | 1250 | 11.8 | 47846 | 1.94 | - | Passed |
| 41-53 | fully modded gear, 2 enhancements | 1163 | 28.9 | 24113 | 1250 | 11.8 | 47860 | 1.98 | - | Passed |
| 41-53 | fully modded gear, 3 enhancements | 1159 | 29.1 | 23858 | 1250 | 11.8 | 47639 | 2.00 | - | Passed |

## Enhancement parity

### Rank 50, fully modded gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 100%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 6 | 60-100 | 34% | 99% | 100% | 0.98 | 100% | 100.0% | 1.00 (+/- 0.00) | 1.02 | >= 0.95 | Passed |
| 41-45 | 2 | 6 | 60-100 | 69% | 95% | 99% | 1.89 | 100% | 100.0% | 2.00 (+/- 0.00) | 1.06 | >= 0.95 | Passed |
| 41-45 | 3 | 6 | 60-100 | 103% | 98% | 99% | 2.92 | 100% | 100.0% | 2.99 (+/- 0.01) | 1.02 | >= 0.90 | Passed |
| 46-50 | 1 | 6 | 60-100 | 39% | 99% | 100% | 0.98 | 100% | 100.0% | 1.00 (+/- 0.00) | 1.02 | >= 0.95 | Passed |
| 46-50 | 2 | 6 | 60-100 | 79% | 99% | 100% | 1.98 | 100% | 100.0% | 2.00 (+/- 0.00) | 1.01 | >= 0.95 | Passed |
| 46-50 | 3 | 6 | 60-100 | 118% | 100% | 99% | 2.98 | 100% | 100.0% | 2.97 (+/- 0.01) | 0.99 | >= 0.90 | Passed |
| 51-53 | 1 | 6 | 60-70 | 7% | 97% | 38% | 0.36 | 100% | 100.0% | 1.00 (+/- 0.00) | 2.75 | >= 0.95 | Passed |
| 51-53 | 2 | 6 | 60-70 | 15% | 99% | 39% | 0.77 | 100% | 99.9% | 2.00 (+/- 0.00) | 2.61 | >= 0.95 | Passed |
| 51-53 | 3 | 6 | 60-70 | 22% | 98% | 33% | 0.97 | 100% | 100.0% | 2.97 (+/- 0.01) | 3.08 | >= 0.90 | Passed |

### Thresholds, first / second / third slot, that would meet parity for the quality measured (the strictest set that keeps every case at least at today's, never falling from one slot to the next)

| Scope | Thresholds that meet parity | In steps of 5 | Thresholds in force |
| --- | --- | --- | --- |
| Every band together (the shared thresholds) | 99% / 99% / 99% | 95% / 95% / 95% | 50% / 85% / 100% |
| Band 41-45 alone | 99% / 100% / 100% | 95% / 100% / 100% | 50% / 85% / 100% |
| Band 46-50 alone | 99% / 99% / 99% | 95% / 95% / 95% | 50% / 85% / 100% |
| Band 51-53 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 100% |

### Rank 50, fully modded gear: quality of the items the new minigame made, in whole percent, by band and slots filled

| Band | Slots filled | Items made | 10th percentile | 25th percentile | Median | 75th percentile | 90th percentile | Made at 100% |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 41-45 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 100% |
| 41-45 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 99% |
| 46-50 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 96% |
| 46-50 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 95% |
| 46-50 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 97% |
| 51-53 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 95% |
| 51-53 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 94% |
| 51-53 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 97% |

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
- Every craft in this scenario is played by a crafter who bought the Technician line in full (every purchase their Engineering rank allows) and plays Safe Mode, so no gate here is an unperked result.
