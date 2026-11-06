# Crafting revamp: balance gates of the Circuit (both lines, Overclock) minigame

- Command: `SWLOR.CraftSim gates --scenario circuit-both --gates rotation,length,materials,xp,enhancements --runs 30 --seed 1 --recipes-per-band 6 --repo . --out design/testing/crafting-revamp/circuit-both-gates.md`
- Seed 1; 30 runs per case.

## Inputs

- Scenario: Circuit (both lines, Overclock) (`circuit-both`), Engineering recipes, minigame Circuit.
- Quality: the quality meter's share of its maximum when the item is made, in points (0-100). A broken or abandoned piece counts 0, so every average includes the crafts that failed.
- Crafter: the rank the scenario's cohort gives each recipe (by default the recipe's level, at most 50, so a crafter works at their own level), with no food or status bonuses and no perks unless a gate names a style or the scenario's limitations say it plays a perk loadout. Gear levels are the plan's none, middle and best tier of the skill's crafting gear:
  - no gear (Control 0, Craftsmanship 0, CP 0)
  - mid gear (Control 13, Craftsmanship 12, CP 20)
  - best gear (Control 29, Craftsmanship 30, CP 37)
  - fully modded gear (Control 119, Craftsmanship 120, CP 37)
- Recipes: 1-10: 6 of 307; 11-20: 6 of 367; 21-30: 6 of 359; 31-40: 6 of 370; 41-53: 6 of 383 recipes played.
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
| No fixed rotation | Failed | One fixed rotation comes within 10 points of adaptive play in 6 case(s): 1-10 / no gear; 1-10 / mid gear; 1-10 / best gear; 11-20 / no gear; 41-53 / no gear; 41-53 / mid gear (smallest margin -24.0 in 41-53 / no gear). The best rotation for each recipe alone, a reported metric, is -19.7 to 0.0 points behind adaptive play. |
| Length | Passed | Every band and gear tier takes 10-20 actions (item craft at level) on average. |
| Economy parity | Passed | Component stacks per item are within 10% of the legacy baseline where its success rate is at least 90%, and at most 10% above it where it is lower, at no less quality, in every band and gear tier. |
| XP rate | Passed | Item crafts: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.04x in 1-10 / no gear). |
| Enhancement parity | Passed | At rank 50 in fully modded gear, the thresholds in force (50% / 85% / 100%) transfer at least 0.90 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 1.00 times today's). Thresholds that meet parity in every band for this quality: 24% / 99% / 99%. |

## No fixed rotation

### Average quality: adaptive play against the best single fixed rotation of each band and gear tier (quality points; a broken piece counts 0)

| Band | Gear | Recipes played | Adaptive avg quality | Best single rotation avg quality | Margin | Required | Best rotation per recipe avg quality (reported) | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 49.4 | 50.8 | -1.3 | >= 10 | 53.1 | Failed |
| 1-10 | mid gear | 6 | 83.8 | 99.1 | -15.3 | >= 10 | 100.0 | Failed |
| 1-10 | best gear | 6 | 93.1 | 100.0 | -6.9 | >= 10 | 100.0 | Failed |
| 1-10 | fully modded gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 11-20 | no gear | 6 | 85.7 | 84.5 | 1.2 | >= 10 | 89.3 | Failed |
| 11-20 | mid gear | 6 | 98.6 | 100.0 | -1.4 | >= 10 | 100.0 | Reported |
| 11-20 | best gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 11-20 | fully modded gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Reported |
| 21-30 | no gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 21-30 | mid gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Reported |
| 21-30 | best gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 21-30 | fully modded gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 31-40 | no gear | 6 | 97.9 | 99.4 | -1.4 | >= 10 | 100.0 | Reported |
| 31-40 | mid gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 31-40 | best gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Reported |
| 31-40 | fully modded gear | 6 | 100.0 | 100.0 | 0.0 | >= 10 | 100.0 | Reported |
| 41-53 | no gear | 6 | 72.8 | 96.8 | -24.0 | >= 10 | 92.5 | Failed |
| 41-53 | mid gear | 6 | 92.6 | 100.0 | -7.4 | >= 10 | 99.3 | Failed |
| 41-53 | best gear | 6 | 98.6 | 100.0 | -1.4 | >= 10 | 100.0 | Reported |
| 41-53 | fully modded gear | 6 | 99.7 | 100.0 | -0.3 | >= 10 | 100.0 | Reported |

### The single rotation found for each band and gear tier, searched across the band's sampled recipes (average quality in points)

| Band | Gear | Best single rotation for the band | Training | Selection | Hold-out | Adaptive on these recipes |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | wire x2 > tune x2 > discharge_calibration > wire x3 > discharge_calibration > wire x4 > discharge_calibration > wire > discharge_calibration > wire x2 > discharge_calibration > wire x2 | 50.4 | 50.4 | 50.8 | 49.4 |
| 1-10 | mid gear | wire > tune x2 > discharge_calibration > patch > tune x3 > discharge_calibration > tune > wire > tune > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire | 99.2 | 99.0 | 99.1 | 83.8 |
| 1-10 | best gear | patch > tune > patch > tune x2 > discharge_calibration > tune x3 > discharge_calibration > wire > tune x2 > discharge_calibration > wire x3 > discharge_assembly > wire x3 | 100.0 | 100.0 | 100.0 | 93.1 |
| 1-10 | fully modded gear | tune x3 > discharge_calibration > tune x3 > wire x2 > discharge_assembly > tune > wire x3 | 100.0 | 100.0 | 100.0 | 100.0 |
| 11-20 | no gear | wire x3 > tune x4 > discharge_calibration > boost > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire | 85.5 | 86.6 | 84.5 | 85.7 |
| 11-20 | mid gear | wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire | 100.0 | 100.0 | 100.0 | 98.6 |
| 11-20 | best gear | boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost | 100.0 | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | tune x6 > discharge_assembly > wire x2 > discharge_calibration > patch > wire x3 | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | tune x4 > discharge_calibration > wire > tune x2 > discharge_calibration > wire x3 > discharge_calibration > wire x4 > discharge_both > wire x2 | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | boost > tune x2 > discharge_calibration > tune > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | tune x4 > discharge_calibration > tune x4 > discharge_both > boost > tune x2 > discharge_assembly > wire x3 > discharge_both > patch > tune > micro_tune > wire x4 | 99.8 | 99.9 | 99.4 | 97.9 |
| 31-40 | mid gear | wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | tune > discharge_both > boost > tune > discharge_both > boost > tune > discharge_both > boost > tune > discharge_both > boost > tune > discharge_both > boost > tune > discharge_both > boost > tune > discharge_both > boost > tune > discharge_both > boost > tune > discharge_both > boost > tune > discharge_both > boost > tune > discharge_both > boost > tune > discharge_both > boost > tune > discharge_both > boost > tune | 100.0 | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | tune x4 > discharge_both > tune x2 > wire > discharge_both > wire x3 | 100.0 | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | wire x2 > tune x2 > discharge_calibration > tune x2 > patch > tune x2 > micro_tune > patch > discharge_calibration > wire x3 > tune > discharge_calibration > wire x4 > discharge_calibration > wire > discharge_calibration > wire | 96.8 | 96.7 | 96.8 | 72.8 |
| 41-53 | mid gear | micro_tune > tune x4 > discharge_calibration > tune > patch > tune x3 > patch > discharge_calibration > wire > tune > wire > tune > discharge_both > micro_tune > tune > wire x4 > discharge_calibration > wire x2 > boost > discharge_calibration > wire | 100.0 | 100.0 | 100.0 | 92.6 |
| 41-53 | best gear | wire > tune x3 > discharge_calibration > wire > boost > discharge_calibration > wire x2 > tune x2 > discharge_both > tune x2 > patch > tune x2 > discharge_calibration > wire x3 > tune > discharge_assembly | 100.0 | 100.0 | 100.0 | 98.6 |
| 41-53 | fully modded gear | tune x4 > discharge_calibration > boost > tune x2 > discharge_both > wire x2 > discharge_both | 100.0 | 100.0 | 100.0 | 99.7 |

### Reported metric, not gated: the best fixed rotation for each recipe alone, and what the band's single rotation scores on the same recipe (average quality in points)

| Band | Gear | Recipe | Best rotation for this recipe alone (reported) | Its hold-out | The single rotation on this recipe | Adaptive on this recipe |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | wen_cft_smth1 (level 3) | wire x5 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire | 37.5 | 36.8 | 34.3 |
| 1-10 | no gear | config_ind1 (level 5) | wire x2 > tune > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire x2 > discharge_calibration > wire | 44.8 | 43.8 | 47.4 |
| 1-10 | no gear | d_hd_pva1 (level 7) | discharge_assembly > wire x4 > tune > discharge_calibration > wire x4 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire > discharge_calibration > wire | 53.2 | 49.5 | 46.9 |
| 1-10 | no gear | song_460 (level 8) | wire x3 > tune x2 > discharge_calibration > wire x4 > discharge_calibration > wire x3 > discharge_calibration > wire > discharge_calibration > wire > discharge_calibration > wire | 61.5 | 59.3 | 53.8 |
| 1-10 | no gear | d_lg_pvw1 (level 10) | wire x2 > tune x2 > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire | 60.3 | 60.2 | 55.3 |
| 1-10 | no gear | id_soulstrike1 (level 10) | wire x4 > tune x2 > discharge_calibration x2 > wire x6 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire > discharge_calibration > wire | 61.0 | 55.2 | 59.0 |
| 1-10 | mid gear | wen_cft_smth1 (level 3) | tune x5 > discharge_calibration > tune x2 > wire > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire | 100.0 | 96.8 | 87.1 |
| 1-10 | mid gear | config_ind1 (level 5) | wire > tune > discharge_calibration > tune x3 > discharge_calibration > tune > wire > tune > discharge_calibration > wire x2 > tune > discharge_calibration > wire x3 > discharge_calibration > wire x4 | 100.0 | 100.0 | 53.5 |
| 1-10 | mid gear | d_hd_pva1 (level 7) | tune x5 > discharge_calibration > tune x3 > wire x2 > discharge_calibration > wire x4 > discharge_assembly > wire | 100.0 | 100.0 | 95.0 |
| 1-10 | mid gear | song_460 (level 8) | boost > tune > discharge_calibration > tune x4 > discharge_calibration > tune > wire x2 > tune > discharge_assembly > wire x2 > discharge_calibration > wire x3 > discharge_calibration > wire | 100.0 | 100.0 | 89.0 |
| 1-10 | mid gear | d_lg_pvw1 (level 10) | tune x3 > discharge_calibration > tune x3 > discharge_calibration > wire > tune x2 > discharge_assembly > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire x3 | 100.0 | 99.7 | 90.3 |
| 1-10 | mid gear | id_soulstrike1 (level 10) | tune x5 > discharge_calibration > tune x4 > wire > discharge_calibration > wire x5 > discharge_assembly > wire x2 | 100.0 | 98.3 | 88.0 |
| 1-10 | best gear | wen_cft_smth1 (level 3) | tune x5 > discharge_calibration > tune x4 > wire > discharge_assembly > wire x3 > tune > discharge_assembly > wire | 100.0 | 99.8 | 86.6 |
| 1-10 | best gear | config_ind1 (level 5) | tune x3 > discharge_calibration > tune x3 > discharge_calibration > tune x2 > wire > discharge_calibration > wire x3 > discharge_assembly > wire x3 | 100.0 | 100.0 | 86.6 |
| 1-10 | best gear | d_hd_pva1 (level 7) | boost > tune x2 > discharge_calibration > tune x5 > discharge_assembly > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 99.6 |
| 1-10 | best gear | song_460 (level 8) | tune x4 > discharge_calibration > tune x3 > discharge_calibration > tune x2 > wire x2 > discharge_assembly > wire x4 | 100.0 | 100.0 | 87.8 |
| 1-10 | best gear | d_lg_pvw1 (level 10) | wire x2 > tune > wire > discharge_assembly > tune > patch > wire > tune x2 > discharge_calibration > tune x2 > discharge_calibration > tune > patch > tune x2 > discharge_assembly > wire x4 > discharge_assembly > tune > wire x3 | 100.0 | 100.0 | 99.2 |
| 1-10 | best gear | id_soulstrike1 (level 10) | tune x5 > discharge_calibration > tune x3 > discharge_calibration > wire x5 > discharge_assembly > wire x2 | 100.0 | 100.0 | 98.6 |
| 1-10 | fully modded gear | wen_cft_smth1 (level 3) | tune x5 > discharge_calibration > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | config_ind1 (level 5) | tune x3 > discharge_calibration > tune x3 > wire x5 | 100.0 | 100.0 | 99.8 |
| 1-10 | fully modded gear | d_hd_pva1 (level 7) | boost > tune x2 > discharge_calibration > tune x2 > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | song_460 (level 8) | tune x4 > discharge_calibration > tune x2 > wire x5 | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | d_lg_pvw1 (level 10) | tune x3 > discharge_calibration > tune x3 > wire x5 | 100.0 | 100.0 | 100.0 |
| 1-10 | fully modded gear | id_soulstrike1 (level 10) | tune x5 > discharge_calibration > wire x5 | 100.0 | 100.0 | 100.0 |
| 11-20 | no gear | dhbr002 (level 13) | discharge_assembly > wire x2 > tune x3 > discharge_calibration > wire x3 > discharge_calibration > wire x4 > discharge_calibration > wire > discharge_calibration > wire | 72.8 | 63.6 | 64.0 |
| 11-20 | no gear | song_579 (level 15) | wire > boost > wire x2 > tune > discharge_calibration > wire x5 > tune > wire > tune > discharge_calibration > wire | 82.7 | 75.2 | 80.0 |
| 11-20 | no gear | eva_boost_1 (level 18) | boost > tune x3 > discharge_calibration > wire x5 > discharge_calibration > wire x2 > discharge_calibration > wire x3 > discharge_calibration > wire > discharge_calibration > wire | 83.8 | 84.7 | 85.3 |
| 11-20 | no gear | d_ar_pvs2 (level 19) | wire > discharge_calibration > boost > tune x3 > discharge_calibration > wire x6 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire x2 > discharge_calibration > wire | 96.6 | 92.9 | 91.6 |
| 11-20 | no gear | darg002 (level 20) | tune x3 > discharge_calibration > tune x2 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire x3 > discharge_assembly > wire | 100.0 | 98.5 | 100.0 |
| 11-20 | no gear | id_pinningtoss1 (level 20) | wire x2 > discharge_calibration > wire x2 > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire | 100.0 | 92.0 | 93.2 |
| 11-20 | mid gear | dhbr002 (level 13) | tune x5 > discharge_calibration > wire > tune x2 > discharge_calibration > patch > wire x4 > discharge_calibration > wire x4 > discharge_calibration > wire | 100.0 | 100.0 | 92.7 |
| 11-20 | mid gear | song_579 (level 15) | boost > tune x4 > discharge_calibration > tune x4 > wire x3 > discharge_assembly > wire x3 | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | eva_boost_1 (level 18) | tune x5 > discharge_calibration > tune x5 > discharge_assembly > tune > wire x4 > discharge_assembly > wire | 100.0 | 100.0 | 99.5 |
| 11-20 | mid gear | d_ar_pvs2 (level 19) | boost > tune x3 > discharge_calibration > tune x5 > wire > discharge_assembly > tune > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | darg002 (level 20) | tune x4 > discharge_calibration > tune x2 > wire > discharge_calibration > boost > wire x2 > discharge_assembly > tune > wire > discharge_calibration > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | id_pinningtoss1 (level 20) | tune x3 > discharge_calibration > tune x3 > discharge_calibration > tune > patch > wire > discharge_assembly > tune > wire x2 > discharge_assembly > wire x2 > discharge_calibration > wire x3 | 100.0 | 100.0 | 99.3 |
| 11-20 | best gear | dhbr002 (level 13) | wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | song_579 (level 15) | boost > tune x4 > discharge_calibration > boost > tune x2 > wire x2 > discharge_assembly > wire x3 | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | eva_boost_1 (level 18) | tune x5 > discharge_calibration > tune x4 > wire > discharge_assembly > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | d_ar_pvs2 (level 19) | boost > tune x3 > discharge_calibration > tune x4 > wire x2 > discharge_assembly > wire x3 | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | darg002 (level 20) | boost > tune x2 > discharge_calibration > tune x3 > wire > discharge_assembly > tune > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | id_pinningtoss1 (level 20) | tune x3 > discharge_calibration > tune x3 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_assembly > wire x3 | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | dhbr002 (level 13) | tune x4 > discharge_calibration > tune > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | song_579 (level 15) | boost > tune x3 > discharge_calibration > boost > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | eva_boost_1 (level 18) | boost > tune x2 > discharge_calibration > tune x2 > wire x4 | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | d_ar_pvs2 (level 19) | boost > tune x3 > discharge_calibration > wire x5 | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | darg002 (level 20) | tune x3 > discharge_calibration > wire x4 | 100.0 | 100.0 | 100.0 |
| 11-20 | fully modded gear | id_pinningtoss1 (level 20) | tune x2 > discharge_calibration > tune x3 > wire x5 | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | wen_cft_smth3 (level 23) | tune x4 > discharge_calibration > wire > tune > wire > discharge_calibration > wire x2 > discharge_calibration > wire x4 > discharge_assembly | 100.0 | 100.0 | 99.9 |
| 21-30 | no gear | ion_armor_3 (level 25) | tune x4 > discharge_calibration > tune x3 > discharge_both > tune > wire x2 > discharge_both > wire x3 > discharge_both > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | de_vblade_b1 (level 27) | tune x4 > discharge_calibration > tune x3 > discharge_both > wire x4 > discharge_assembly > tune > wire x2 | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | dhbe003 (level 29) | tune x4 > discharge_calibration > tune x3 > discharge_both > wire x4 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | dsbr003 (level 30) | tune x4 > discharge_calibration > tune x3 > wire > discharge_assembly > tune > wire x2 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | id_linebreaker3 (level 30) | boost > tune x2 > discharge_calibration > tune x4 > discharge_both > wire x4 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | wen_cft_smth3 (level 23) | tune x4 > discharge_calibration > tune x4 > discharge_assembly > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | ion_armor_3 (level 25) | boost > tune x2 > discharge_calibration > tune x2 > discharge_calibration > tune x2 > wire > discharge_assembly > wire x2 > discharge_both > wire x3 | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | de_vblade_b1 (level 27) | tune x4 > discharge_calibration > tune x4 > discharge_assembly > wire x3 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | dhbe003 (level 29) | tune x4 > discharge_calibration > boost > tune > discharge_both > wire x4 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | dsbr003 (level 30) | tune x4 > discharge_calibration > tune x2 > wire x2 > discharge_assembly > wire > discharge_both > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | id_linebreaker3 (level 30) | boost > tune x2 > discharge_calibration > tune x4 > discharge_both > wire x4 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | wen_cft_smth3 (level 23) | tune x4 > discharge_calibration > tune x3 > wire > discharge_assembly > wire x2 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | ion_armor_3 (level 25) | tune x3 > discharge_calibration > tune x2 > discharge_calibration > wire > tune > wire x2 > discharge_assembly > wire x4 | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | de_vblade_b1 (level 27) | tune x4 > discharge_calibration > tune x3 > wire > discharge_assembly > wire x2 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | dhbe003 (level 29) | tune x4 > discharge_calibration > wire > tune > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | dsbr003 (level 30) | tune x4 > discharge_calibration > tune > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | id_linebreaker3 (level 30) | boost > tune x2 > discharge_calibration > tune x3 > discharge_both > wire x4 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | wen_cft_smth3 (level 23) | tune x3 > discharge_calibration > tune > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | ion_armor_3 (level 25) | tune x3 > discharge_both > tune x2 > wire > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | de_vblade_b1 (level 27) | tune x4 > discharge_both > wire x3 | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | dhbe003 (level 29) | tune x4 > discharge_both > wire x3 | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | dsbr003 (level 30) | boost > tune x2 > discharge_both > wire > discharge_both > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | fully modded gear | id_linebreaker3 (level 30) | tune x4 > discharge_both > wire x3 | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | aen_cft_fab4 (level 33) | tune x4 > discharge_calibration > tune x3 > discharge_calibration > tune > wire x2 > discharge_both > wire x3 > discharge_assembly > tune > wire > tune > discharge_both | 100.0 | 96.7 | 100.0 |
| 31-40 | no gear | config_ind4 (level 35) | boost > tune x2 > discharge_calibration > tune x4 > discharge_both > boost > tune x2 > discharge_assembly > wire x3 > discharge_both | 100.0 | 100.0 | 96.6 |
| 31-40 | no gear | d_lg_mws4 (level 37) | boost > tune x2 > discharge_calibration > tune x4 > discharge_both > boost > tune x2 > discharge_assembly > tune > wire x2 > discharge_both | 100.0 | 100.0 | 99.0 |
| 31-40 | no gear | d_hd_mwa4 (level 39) | tune x4 > discharge_calibration > tune x4 > discharge_both > tune x2 > wire x2 > discharge_assembly > tune > discharge_both | 100.0 | 100.0 | 92.0 |
| 31-40 | no gear | dfhl004 (level 40) | boost > tune x2 > discharge_calibration > tune x4 > discharge_both > tune x4 > discharge_assembly > wire x2 > discharge_both > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | id_conctoss2 (level 40) | tune x5 > discharge_calibration > wire > tune x4 > discharge_both > tune > wire > tune > wire x2 > discharge_both | 100.0 | 99.5 | 100.0 |
| 31-40 | mid gear | aen_cft_fab4 (level 33) | tune x4 > discharge_calibration > tune x3 > discharge_both > wire x2 > discharge_both > wire x3 > discharge_both > tune x2 > discharge_both | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | config_ind4 (level 35) | boost > tune x2 > discharge_calibration > tune x4 > discharge_both > tune > wire x3 > discharge_both > wire x2 | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | d_lg_mws4 (level 37) | tune x4 > discharge_calibration > tune x4 > discharge_both > boost > wire x2 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | d_hd_mwa4 (level 39) | boost > tune x2 > discharge_calibration > tune x4 > discharge_both > tune > wire > tune > wire > discharge_both > wire x2 | 100.0 | 100.0 | 99.9 |
| 31-40 | mid gear | dfhl004 (level 40) | boost > tune x3 > discharge_calibration > tune x4 > discharge_both > boost > wire > tune > discharge_both > wire x2 > boost > discharge_both | 100.0 | 100.0 | 99.9 |
| 31-40 | mid gear | id_conctoss2 (level 40) | tune x4 > discharge_calibration > boost > tune x2 > discharge_calibration > boost > tune x3 > discharge_assembly > wire x3 > discharge_both | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | aen_cft_fab4 (level 33) | wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | config_ind4 (level 35) | tune x4 > discharge_calibration > boost > tune x2 > discharge_both > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | d_lg_mws4 (level 37) | tune x4 > discharge_calibration > boost > tune x2 > discharge_both > wire x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | d_hd_mwa4 (level 39) | tune x4 > discharge_calibration > tune x4 > discharge_assembly > tune > wire x2 > discharge_both | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | dfhl004 (level 40) | tune x4 > discharge_calibration > boost > tune x2 > discharge_both > tune > wire x2 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | id_conctoss2 (level 40) | tune x4 > discharge_calibration > tune x4 > discharge_both > tune x2 > wire x3 > discharge_both | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | aen_cft_fab4 (level 33) | tune x3 > discharge_calibration > tune x2 > wire > discharge_assembly > wire x2 | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | config_ind4 (level 35) | tune x4 > discharge_both > tune > wire > discharge_both > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | d_lg_mws4 (level 37) | boost > tune x2 > discharge_calibration > wire > tune > wire > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | d_hd_mwa4 (level 39) | tune x4 > discharge_both > tune > wire > discharge_both | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | dfhl004 (level 40) | tune x4 > discharge_calibration > boost > wire > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | fully modded gear | id_conctoss2 (level 40) | tune x4 > discharge_calibration > tune x2 > wire x2 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | dhbr005 (level 43) | wire > ground > boost > discharge_calibration > micro_tune x2 > tune x4 > discharge_calibration > boost > tune > wire > discharge_calibration > wire x3 > patch > wire > discharge_calibration > wire x4 | 100.0 | 100.0 | 93.3 |
| 41-53 | no gear | therm_amp_4 (level 46) | tune > wire > tune x4 > discharge_calibration > tune x5 > discharge_both > boost > tune > boost > wire > tune > discharge_assembly > wire | 100.0 | 100.0 | 99.4 |
| 41-53 | no gear | wen_eva5 (level 48) | boost > wire > tune x2 > discharge_calibration > patch > tune > patch > tune x3 > patch > wire > discharge_calibration > wire x3 > tune > wire > discharge_calibration > wire x3 | 89.6 | 99.4 | 52.2 |
| 41-53 | no gear | d_lg_mpv5 (level 49) | boost > tune x2 > discharge_calibration > tune x4 > patch > discharge_calibration > tune > wire x3 > discharge_calibration > wire x4 > discharge_calibration > wire x2 | 100.0 | 100.0 | 53.0 |
| 41-53 | no gear | id_infusion2 (level 50) | tune x2 > wire x2 > discharge_calibration > micro_tune > boost > tune x3 > discharge_both > discharge_assembly > tune x5 > wire > discharge_both > boost > wire > discharge_assembly > wire | 85.5 | 98.5 | 72.3 |
| 41-53 | no gear | cap_quadlas1 (level 52) | tune x11 > wire > tune > wire x5 > discharge_calibration > wire | 79.9 | 82.7 | 66.6 |
| 41-53 | mid gear | dhbr005 (level 43) | tune x4 > discharge_calibration > boost > tune x2 > discharge_calibration > tune x3 > wire > discharge_assembly > wire x3 > discharge_both | 100.0 | 100.0 | 98.5 |
| 41-53 | mid gear | therm_amp_4 (level 46) | wire > boost > tune x2 > discharge_calibration > tune x5 > discharge_both > boost > tune x3 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | wen_eva5 (level 48) | tune x4 > discharge_calibration > discharge_assembly > tune > patch > tune x3 > patch > discharge_both > wire > tune > ground > tune > discharge_both > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire x2 | 99.9 | 100.0 | 92.5 |
| 41-53 | mid gear | d_lg_mpv5 (level 49) | tune x4 > discharge_calibration > tune x4 > patch > discharge_calibration > wire > tune x3 > discharge_assembly > wire x3 > discharge_calibration > wire | 100.0 | 100.0 | 92.2 |
| 41-53 | mid gear | id_infusion2 (level 50) | wire > discharge_assembly > wire > discharge_calibration > boost > tune > patch > tune x2 > discharge_calibration > tune > boost > tune x2 > boost x2 > discharge_calibration > tune > wire > tune > discharge_both > tune x5 > wire x2 > tune > discharge_assembly > wire | 99.8 | 100.0 | 78.1 |
| 41-53 | mid gear | cap_quadlas1 (level 52) | boost x2 > tune x7 > wire > tune > wire > tune x3 > wire x2 > tune > discharge_both | 96.0 | 100.0 | 94.0 |
| 41-53 | best gear | dhbr005 (level 43) | tune x4 > discharge_calibration > tune x4 > discharge_both > tune > wire x3 > discharge_both | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | therm_amp_4 (level 46) | boost > tune x3 > discharge_calibration > boost > tune x3 > discharge_both > wire x3 > discharge_both | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | wen_eva5 (level 48) | tune x4 > discharge_calibration > tune x4 > patch > discharge_calibration > tune x3 > wire > discharge_assembly > tune > wire x2 > discharge_both | 100.0 | 100.0 | 91.8 |
| 41-53 | best gear | d_lg_mpv5 (level 49) | boost > tune x2 > discharge_calibration > tune x4 > discharge_both > tune x4 > micro_tune > discharge_assembly > patch > tune > wire > discharge_both | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | id_infusion2 (level 50) | micro_tune > tune x4 > discharge_calibration > tune > patch > wire > tune x3 > discharge_both > wire > tune x4 > discharge_both > patch > boost > discharge_both > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | cap_quadlas1 (level 52) | boost > tune x2 > discharge_calibration > boost > tune x3 > discharge_both > boost > tune x5 > wire > patch > wire > discharge_calibration > wire x2 | 100.0 | 100.0 | 99.7 |
| 41-53 | fully modded gear | dhbr005 (level 43) | tune x4 > discharge_calibration > tune > wire x2 > discharge_both > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | fully modded gear | therm_amp_4 (level 46) | tune x4 > discharge_calibration > wire > tune x2 > wire > discharge_both | 100.0 | 100.0 | 100.0 |
| 41-53 | fully modded gear | wen_eva5 (level 48) | tune x4 > discharge_calibration > tune x4 > discharge_assembly > wire > discharge_both | 100.0 | 100.0 | 99.9 |
| 41-53 | fully modded gear | d_lg_mpv5 (level 49) | tune x4 > discharge_calibration > boost > tune x2 > discharge_assembly > boost > discharge_both | 100.0 | 100.0 | 100.0 |
| 41-53 | fully modded gear | id_infusion2 (level 50) | tune x4 > discharge_calibration > boost > tune x2 > discharge_both > wire x3 | 100.0 | 100.0 | 98.0 |
| 41-53 | fully modded gear | cap_quadlas1 (level 52) | tune x4 > discharge_calibration > boost > tune x2 > discharge_both > wire x2 > discharge_both | 100.0 | 100.0 | 100.0 |

## Length

### Actions per attempt of the adaptive player at level (item craft at level)

| Band | Gear | Recipes played | Mean actions per attempt | Shortest recipe | Longest recipe | Stalled attempts | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 14.4 | 12.2 | 16.1 | 0 | 10-20 | Passed |
| 1-10 | mid gear | 6 | 16.6 | 15.0 | 17.3 | 0 | 10-20 | Passed |
| 1-10 | best gear | 6 | 15.1 | 13.0 | 16.7 | 0 | 10-20 | Passed |
| 1-10 | fully modded gear | 6 | 10.0 | 8.9 | 10.3 | 0 | 10-20 | Reported |
| 11-20 | no gear | 6 | 15.6 | 14.3 | 17.1 | 0 | 10-20 | Passed |
| 11-20 | mid gear | 6 | 15.1 | 12.0 | 16.9 | 0 | 10-20 | Passed |
| 11-20 | best gear | 6 | 13.2 | 10.5 | 15.2 | 0 | 10-20 | Passed |
| 11-20 | fully modded gear | 6 | 8.5 | 7.2 | 9.4 | 0 | 10-20 | Reported |
| 21-30 | no gear | 6 | 14.3 | 12.6 | 15.9 | 0 | 10-20 | Passed |
| 21-30 | mid gear | 6 | 12.3 | 11.3 | 12.9 | 0 | 10-20 | Passed |
| 21-30 | best gear | 6 | 11.1 | 9.6 | 12.4 | 0 | 10-20 | Passed |
| 21-30 | fully modded gear | 6 | 6.9 | 6.0 | 7.7 | 0 | 10-20 | Reported |
| 31-40 | no gear | 6 | 15.7 | 15.0 | 17.7 | 0 | 10-20 | Reported |
| 31-40 | mid gear | 6 | 14.2 | 13.2 | 15.9 | 0 | 10-20 | Passed |
| 31-40 | best gear | 6 | 12.3 | 11.2 | 13.9 | 0 | 10-20 | Passed |
| 31-40 | fully modded gear | 6 | 7.6 | 6.9 | 8.0 | 0 | 10-20 | Reported |
| 41-53 | no gear | 6 | 18.2 | 16.5 | 20.4 | 0 | 10-20 | Reported |
| 41-53 | mid gear | 6 | 17.6 | 14.7 | 20.3 | 0 | 10-20 | Reported |
| 41-53 | best gear | 6 | 15.5 | 13.1 | 18.4 | 0 | 10-20 | Passed |
| 41-53 | fully modded gear | 6 | 9.0 | 7.7 | 10.1 | 0 | 10-20 | Reported |

## Economy parity

### Component stacks consumed per item made, failures included, over the recipes the legacy policy makes at least once in the cell (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Parity rule | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 2.33 | 2.37 | 0.98 | 100% | 98% | 49% | 1% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | mid gear | 6 | 2.47 | 2.37 | 1.04 | 92% | 98% | 84% | 10% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | best gear | 6 | 2.40 | 2.35 | 1.02 | 96% | 99% | 93% | 25% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | fully modded gear | 6 | 2.33 | 2.34 | 1.00 | 100% | 99% | 100% | 75% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | no gear | 6 | 2.00 | 2.10 | 0.95 | 99% | 93% | 88% | 3% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | mid gear | 6 | 2.01 | 2.02 | 0.99 | 99% | 98% | 99% | 13% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | best gear | 6 | 2.00 | 2.03 | 0.99 | 100% | 98% | 100% | 28% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | fully modded gear | 6 | 2.00 | 2.01 | 0.99 | 100% | 99% | 100% | 91% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | no gear | 6 | 2.50 | 2.55 | 0.98 | 100% | 97% | 100% | 8% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | mid gear | 6 | 2.50 | 2.53 | 0.99 | 100% | 98% | 100% | 15% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | best gear | 6 | 2.50 | 2.51 | 1.00 | 100% | 99% | 100% | 29% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | fully modded gear | 6 | 2.50 | 2.52 | 0.99 | 100% | 99% | 100% | 93% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | no gear | 6 | 2.53 | 2.53 | 1.00 | 98% | 98% | 98% | 23% | yes | 0% / 0% | within 10% | Reported |
| 31-40 | mid gear | 6 | 2.50 | 2.53 | 0.99 | 100% | 98% | 100% | 35% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | best gear | 6 | 2.50 | 2.52 | 0.99 | 100% | 99% | 100% | 48% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | fully modded gear | 6 | 2.50 | 2.54 | 0.98 | 100% | 97% | 100% | 85% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | no gear | 6 | 2.72 | 2.42 | 1.12 | 83% | 99% | 74% | 42% | yes | 0% / 17% | within 10% | Reported |
| 41-53 | mid gear | 6 | 2.56 | 2.72 | 0.94 | 96% | 87% | 93% | 55% | yes | 0% / 0% | at most +10% | Reported |
| 41-53 | best gear | 6 | 2.52 | 2.59 | 0.97 | 99% | 94% | 99% | 76% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | fully modded gear | 6 | 2.50 | 2.54 | 0.98 | 100% | 98% | 100% | 89% | yes | 0% / 0% | within 10% | Passed |

### Recipes the legacy policy never makes in the cell, left out of the stack and quality comparison above (they still count in the never-made check)

| Band | Gear | Recipe | Level | New success | New avg quality |
| --- | --- | --- | --- | --- | --- |
| 41-53 | no gear | QuadLaserCannon1 | 52 | 100% | 67% |

## XP rate

### Item crafts: XP per hour against the legacy loop (6.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per craft | Legacy actions per craft | Legacy XP per hour | New XP per craft | New actions per craft | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 595 | 5.8 | 61824 | 2053 | 14.4 | 64125 | 1.04 | - | Passed |
| 1-10 | no gear, 1 enhancement | 429 | 6.4 | 40203 | 2190 | 18.6 | 52840 | 1.31 | - | Passed |
| 1-10 | no gear, 2 enhancements | 184 | 6.0 | 18350 | 2140 | 18.3 | 52555 | 2.86 | - | Passed |
| 1-10 | no gear, 3 enhancements | 90 | 6.0 | 9000 | 2148 | 18.3 | 52695 | 5.85 | - | Passed |
| 1-10 | mid gear | 649 | 7.5 | 51840 | 2435 | 16.6 | 66131 | 1.28 | - | Passed |
| 1-10 | mid gear, 1 enhancement | 603 | 7.9 | 45828 | 2094 | 19.5 | 48399 | 1.06 | - | Passed |
| 1-10 | mid gear, 2 enhancements | 578 | 8.4 | 41136 | 2125 | 19.8 | 48352 | 1.18 | - | Passed |
| 1-10 | mid gear, 3 enhancements | 529 | 8.9 | 35636 | 2135 | 19.8 | 48463 | 1.36 | - | Passed |
| 1-10 | best gear | 744 | 8.8 | 50836 | 2601 | 15.1 | 77536 | 1.53 | - | Passed |
| 1-10 | best gear, 1 enhancement | 722 | 9.2 | 47316 | 2379 | 17.9 | 59838 | 1.26 | - | Passed |
| 1-10 | best gear, 2 enhancements | 719 | 9.6 | 44992 | 2323 | 17.9 | 58535 | 1.30 | - | Passed |
| 1-10 | best gear, 3 enhancements | 687 | 10.0 | 41012 | 2380 | 18.1 | 59077 | 1.44 | - | Passed |
| 1-10 | fully modded gear | 1047 | 7.7 | 82045 | 2747 | 9.9 | 124259 | 1.51 | - | Passed |
| 1-10 | fully modded gear, 1 enhancement | 1031 | 7.7 | 80482 | 2748 | 10.7 | 115390 | 1.43 | - | Passed |
| 1-10 | fully modded gear, 2 enhancements | 1019 | 8.0 | 76508 | 2743 | 10.8 | 113837 | 1.49 | - | Passed |
| 1-10 | fully modded gear, 3 enhancements | 1018 | 8.5 | 71761 | 2748 | 11.0 | 112576 | 1.57 | - | Passed |
| 11-20 | no gear | 579 | 7.9 | 44148 | 1711 | 15.6 | 49286 | 1.12 | - | Passed |
| 11-20 | no gear, 1 enhancement | 389 | 8.6 | 27294 | 1775 | 16.9 | 47144 | 1.73 | - | Passed |
| 11-20 | no gear, 2 enhancements | 265 | 7.3 | 21917 | 1760 | 17.2 | 46118 | 2.10 | - | Passed |
| 11-20 | no gear, 3 enhancements | 252 | 8.1 | 18738 | 1747 | 17.1 | 45849 | 2.45 | - | Passed |
| 11-20 | mid gear | 667 | 10.2 | 39192 | 1831 | 15.1 | 54513 | 1.39 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 635 | 11.6 | 32743 | 1797 | 15.8 | 51355 | 1.57 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 616 | 12.8 | 28933 | 1835 | 15.7 | 52438 | 1.81 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 537 | 13.9 | 23243 | 1839 | 15.8 | 52222 | 2.25 | - | Passed |
| 11-20 | best gear | 761 | 14.5 | 31427 | 1848 | 13.2 | 63156 | 2.01 | - | Passed |
| 11-20 | best gear, 1 enhancement | 749 | 15.4 | 29203 | 1848 | 14.2 | 58723 | 2.01 | - | Passed |
| 11-20 | best gear, 2 enhancements | 740 | 16.0 | 27777 | 1846 | 14.1 | 58947 | 2.12 | - | Passed |
| 11-20 | best gear, 3 enhancements | 717 | 16.5 | 26050 | 1847 | 14.0 | 59569 | 2.29 | - | Passed |
| 11-20 | fully modded gear | 1146 | 14.4 | 47678 | 1848 | 8.4 | 98414 | 2.06 | - | Passed |
| 11-20 | fully modded gear, 1 enhancement | 1147 | 14.5 | 47531 | 1848 | 8.9 | 93147 | 1.96 | - | Passed |
| 11-20 | fully modded gear, 2 enhancements | 1133 | 14.6 | 46455 | 1848 | 8.8 | 94378 | 2.03 | - | Passed |
| 11-20 | fully modded gear, 3 enhancements | 1134 | 14.9 | 45749 | 1848 | 9.0 | 92801 | 2.03 | - | Passed |
| 21-30 | no gear | 633 | 11.3 | 33571 | 1380 | 14.3 | 43371 | 1.29 | - | Passed |
| 21-30 | no gear, 1 enhancement | 617 | 12.2 | 30271 | 1380 | 15.0 | 41464 | 1.37 | - | Passed |
| 21-30 | no gear, 2 enhancements | 605 | 12.9 | 28088 | 1372 | 14.9 | 41482 | 1.48 | - | Passed |
| 21-30 | no gear, 3 enhancements | 559 | 14.1 | 23889 | 1380 | 14.8 | 41896 | 1.75 | - | Passed |
| 21-30 | mid gear | 683 | 16.8 | 24372 | 1380 | 12.3 | 50351 | 2.07 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 677 | 17.5 | 23193 | 1380 | 13.0 | 47789 | 2.06 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 671 | 17.5 | 23035 | 1380 | 13.1 | 47303 | 2.05 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 652 | 18.0 | 21772 | 1380 | 13.2 | 47065 | 2.16 | - | Passed |
| 21-30 | best gear | 773 | 22.6 | 20498 | 1380 | 11.1 | 55750 | 2.72 | - | Passed |
| 21-30 | best gear, 1 enhancement | 763 | 22.7 | 20201 | 1380 | 11.3 | 54767 | 2.71 | - | Passed |
| 21-30 | best gear, 2 enhancements | 755 | 23.2 | 19561 | 1380 | 11.2 | 55200 | 2.82 | - | Passed |
| 21-30 | best gear, 3 enhancements | 748 | 23.3 | 19290 | 1380 | 11.4 | 54526 | 2.83 | - | Passed |
| 21-30 | fully modded gear | 1151 | 22.1 | 31253 | 1380 | 6.9 | 89853 | 2.88 | - | Passed |
| 21-30 | fully modded gear, 1 enhancement | 1109 | 22.6 | 29432 | 1380 | 7.3 | 85263 | 2.90 | - | Passed |
| 21-30 | fully modded gear, 2 enhancements | 1129 | 22.7 | 29785 | 1380 | 7.2 | 85655 | 2.88 | - | Passed |
| 21-30 | fully modded gear, 3 enhancements | 1121 | 22.7 | 29589 | 1380 | 7.3 | 85002 | 2.87 | - | Passed |
| 31-40 | no gear | 726 | 14.8 | 29537 | 1179 | 15.7 | 33712 | 1.14 | - | Passed |
| 31-40 | no gear, 1 enhancement | 680 | 14.5 | 28186 | 1169 | 16.8 | 31394 | 1.11 | - | Passed |
| 31-40 | no gear, 2 enhancements | 631 | 15.1 | 25051 | 1193 | 16.4 | 32721 | 1.31 | - | Passed |
| 31-40 | no gear, 3 enhancements | 583 | 16.0 | 21889 | 1200 | 17.0 | 31829 | 1.45 | - | Passed |
| 31-40 | mid gear | 799 | 18.8 | 25485 | 1200 | 14.2 | 37889 | 1.49 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 783 | 19.4 | 24165 | 1200 | 14.4 | 37513 | 1.55 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 743 | 19.2 | 23152 | 1194 | 14.6 | 36906 | 1.59 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 674 | 18.4 | 21987 | 1194 | 14.5 | 37120 | 1.69 | - | Passed |
| 31-40 | best gear | 884 | 24.6 | 21596 | 1200 | 12.3 | 43919 | 2.03 | - | Passed |
| 31-40 | best gear, 1 enhancement | 887 | 25.3 | 21027 | 1200 | 12.3 | 43882 | 2.09 | - | Passed |
| 31-40 | best gear, 2 enhancements | 859 | 25.0 | 20580 | 1200 | 12.4 | 43528 | 2.12 | - | Passed |
| 31-40 | best gear, 3 enhancements | 817 | 23.7 | 20695 | 1200 | 12.3 | 43783 | 2.12 | - | Passed |
| 31-40 | fully modded gear | 1093 | 25.3 | 25966 | 1200 | 7.6 | 70949 | 2.73 | - | Passed |
| 31-40 | fully modded gear, 1 enhancement | 1109 | 25.6 | 25990 | 1200 | 7.9 | 68495 | 2.64 | - | Passed |
| 31-40 | fully modded gear, 2 enhancements | 1115 | 26.4 | 25368 | 1200 | 7.9 | 68207 | 2.69 | - | Passed |
| 31-40 | fully modded gear, 3 enhancements | 1107 | 26.6 | 24967 | 1200 | 7.8 | 68977 | 2.76 | - | Passed |
| 41-53 | no gear | 725 | 17.5 | 24801 | 1004 | 18.2 | 24768 | 1.00 | - | Passed |
| 41-53 | no gear, 1 enhancement | 638 | 16.0 | 23876 | 1157 | 19.0 | 27412 | 1.15 | - | Passed |
| 41-53 | no gear, 2 enhancements | 510 | 16.4 | 18665 | 1055 | 19.0 | 24946 | 1.34 | - | Passed |
| 41-53 | no gear, 3 enhancements | 447 | 17.4 | 15438 | 1142 | 19.0 | 27013 | 1.75 | - | Passed |
| 41-53 | mid gear | 868 | 23.9 | 21755 | 1184 | 17.6 | 30221 | 1.39 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 788 | 22.6 | 20965 | 1079 | 17.2 | 28235 | 1.35 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 684 | 20.0 | 20552 | 1154 | 17.2 | 30180 | 1.47 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 596 | 19.2 | 18602 | 1093 | 17.3 | 28438 | 1.53 | - | Passed |
| 41-53 | best gear | 1044 | 28.5 | 22007 | 1236 | 15.5 | 35925 | 1.63 | - | Passed |
| 41-53 | best gear, 1 enhancement | 970 | 27.8 | 20982 | 1241 | 15.3 | 36395 | 1.73 | - | Passed |
| 41-53 | best gear, 2 enhancements | 885 | 26.0 | 20448 | 1232 | 15.5 | 35678 | 1.74 | - | Passed |
| 41-53 | best gear, 3 enhancements | 776 | 23.3 | 19995 | 1204 | 15.6 | 34804 | 1.74 | - | Passed |
| 41-53 | fully modded gear | 1157 | 27.7 | 25051 | 1248 | 9.0 | 62548 | 2.50 | - | Passed |
| 41-53 | fully modded gear, 1 enhancement | 1159 | 28.2 | 24692 | 1250 | 9.3 | 60299 | 2.44 | - | Passed |
| 41-53 | fully modded gear, 2 enhancements | 1163 | 28.9 | 24113 | 1250 | 9.4 | 59837 | 2.48 | - | Passed |
| 41-53 | fully modded gear, 3 enhancements | 1159 | 29.1 | 23858 | 1250 | 9.5 | 59310 | 2.49 | - | Passed |

## Enhancement parity

### Rank 50, fully modded gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 100%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 6 | 60-100 | 34% | 99% | 100% | 0.98 | 100% | 99.6% | 0.99 (+/- 0.01) | 1.01 | >= 0.95 | Passed |
| 41-45 | 2 | 6 | 60-100 | 69% | 95% | 99% | 1.89 | 100% | 99.6% | 1.99 (+/- 0.01) | 1.05 | >= 0.95 | Passed |
| 41-45 | 3 | 6 | 60-100 | 103% | 98% | 99% | 2.92 | 100% | 100.0% | 2.98 (+/- 0.01) | 1.02 | >= 0.90 | Passed |
| 46-50 | 1 | 6 | 60-100 | 39% | 99% | 100% | 0.98 | 100% | 98.6% | 0.98 (+/- 0.01) | 1.00 | >= 0.95 | Passed |
| 46-50 | 2 | 6 | 60-100 | 79% | 99% | 100% | 1.98 | 100% | 99.6% | 1.99 (+/- 0.01) | 1.00 | >= 0.95 | Passed |
| 46-50 | 3 | 6 | 60-100 | 118% | 100% | 99% | 2.98 | 100% | 100.0% | 2.98 (+/- 0.01) | 1.00 | >= 0.90 | Passed |
| 51-53 | 1 | 6 | 60-70 | 7% | 97% | 38% | 0.36 | 100% | 100.0% | 1.00 (+/- 0.00) | 2.75 | >= 0.95 | Passed |
| 51-53 | 2 | 6 | 60-70 | 15% | 99% | 39% | 0.77 | 100% | 100.0% | 2.00 (+/- 0.00) | 2.61 | >= 0.95 | Passed |
| 51-53 | 3 | 6 | 60-70 | 22% | 98% | 33% | 0.97 | 100% | 100.0% | 2.99 (+/- 0.01) | 3.10 | >= 0.90 | Passed |

### Thresholds, first / second / third slot, that would meet parity for the quality measured (the strictest set that keeps every case at least at today's, never falling from one slot to the next)

| Scope | Thresholds that meet parity | In steps of 5 | Thresholds in force |
| --- | --- | --- | --- |
| Every band together (the shared thresholds) | 24% / 99% / 99% | 20% / 95% / 95% | 50% / 85% / 100% |
| Band 41-45 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 100% |
| Band 46-50 alone | 24% / 99% / 99% | 20% / 95% / 95% | 50% / 85% / 100% |
| Band 51-53 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 100% |

### Rank 50, fully modded gear: quality of the items the new minigame made, in whole percent, by band and slots filled

| Band | Slots filled | Items made | 10th percentile | 25th percentile | Median | 75th percentile | 90th percentile | Made at 100% |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 99% |
| 41-45 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 97% |
| 41-45 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 46-50 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 46-50 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 99% |
| 46-50 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 51-53 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 100% |
| 51-53 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 99% |
| 51-53 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 99% |

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
- Every craft in this scenario is played by a crafter who bought both perk lines in full (every purchase their Engineering rank allows) and plays Overclock, so no gate here is an unperked result.
