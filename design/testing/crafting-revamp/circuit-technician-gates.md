# Crafting revamp: balance gates of the Circuit (Technician line, Safe Mode) minigame

- Command: `SWLOR.CraftSim gates --scenario circuit-technician --gates rotation,length,materials,xp,enhancements --runs 100 --recipes-per-band 6 --seed 1 --out design\testing\crafting-revamp\circuit-technician-gates.md --json design\testing\crafting-revamp\circuit-technician-gates.json`
- Seed 1; 100 runs per case.

## Inputs

- Scenario: Circuit (Technician line, Safe Mode) (`circuit-technician`), Engineering recipes, minigame Circuit.
- Quality: the quality meter's share of its maximum when the item is made, in points (0-100). A broken or abandoned piece counts 0, so every average includes the crafts that failed.
- Crafter: the rank the scenario's cohort gives each recipe (by default the recipe's level, at most 50, so a crafter works at their own level), with no food or status bonuses and no perks unless a gate names a style or the scenario's limitations say it plays a perk loadout. Gear levels are the plan's none, middle and best tier of the skill's crafting gear:
  - no gear (Control 0, Craftsmanship 0, CP 0)
  - mid gear (Control 13, Craftsmanship 12, CP 20)
  - best gear (Control 29, Craftsmanship 30, CP 37)
- Recipes: 1-10: 6 of 307; 11-20: 6 of 367; 21-30: 6 of 359; 31-40: 6 of 370; 41-53: 6 of 383 recipes played.
- Adaptive player: depth 3, forecast samples per result 2, budget 4000 engine copies per decision, 0.5% of value lost per extra action, heuristic DefaultLeafHeuristic. Each attempt's dice come from its own stream of the seed.
- Fixed-rotation solver: beam 8, cycles up to 3, 8 adaptive lines as seeds, up to 40 actions, 64 training attempts per local-search round and 150 selection attempts for each of 12 finalists, local search from the best 3 candidate(s) for up to 3 round(s) each. Gate 1 searches one sequence for each band and gear tier across the band's sampled recipes (every recipe weighing the same; an action a recipe's engine cannot take is skipped there), and also each recipe's own best sequence as a reported metric. The winner is chosen on separate attempts and then scored on a third set (hold-out) with 100 attempts per recipe.
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
- Enhancement thresholds in force (`CraftOutcomeRules`, read by gate 7): 50% / 85% / 85% for slots 1, 2 and 3. The scenario applies enhancements to its engine's progress meter.
- Styles: none offered, so the style-balance gate is not applicable.
- Gates run: No fixed rotation, Length, Economy parity, XP rate, Enhancement parity. The others were not run, so this report cannot show that the minigame passes.

## Verdicts

| Gate | Verdict | Summary |
| --- | --- | --- |
| No fixed rotation | Failed | One fixed rotation comes within 10 points of adaptive play in 15 case(s): 1-10 / no gear; 1-10 / mid gear; 1-10 / best gear; 11-20 / no gear; 11-20 / mid gear; 11-20 / best gear; 21-30 / no gear; 21-30 / mid gear; 21-30 / best gear; 31-40 / no gear; 31-40 / mid gear; 31-40 / best gear; 41-53 / no gear; 41-53 / mid gear; 41-53 / best gear (smallest margin -6.8 in 1-10 / mid gear). The best rotation for each recipe alone, a reported metric, is -7.2 to 0.2 points behind adaptive play. |
| Length | Passed | Every band and gear tier takes 10-20 actions (item craft at level) on average. |
| Economy parity | Passed | Component stacks per item are within 10% of the legacy baseline where its success rate is at least 90%, and at most 10% above it where it is lower, at no less quality, in every band and gear tier. |
| XP rate | Passed | Item crafts: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.02x in 21-30 / no gear). |
| Enhancement parity | Passed | At rank 50 in best gear, the thresholds in force (50% / 85% / 85%) transfer at least 1.00 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 1.08 times today's). Thresholds that meet parity in every band for this quality: 100% / 100% / 100%. |

## No fixed rotation

### Average quality: adaptive play against the best single fixed rotation of each band and gear tier (quality points; a broken piece counts 0)

| Band | Gear | Recipes played | Adaptive avg quality | Best single rotation avg quality | Margin | Required | Best rotation per recipe avg quality (reported) | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 48.6 | 50.9 | -2.3 | >= 10 | 52.6 | Failed |
| 1-10 | mid gear | 6 | 92.8 | 99.6 | -6.8 | >= 10 | 100.0 | Failed |
| 1-10 | best gear | 6 | 97.5 | 99.7 | -2.2 | >= 10 | 100.0 | Failed |
| 11-20 | no gear | 6 | 79.9 | 86.7 | -6.8 | >= 10 | 86.3 | Failed |
| 11-20 | mid gear | 6 | 99.9 | 100.0 | -0.1 | >= 10 | 100.0 | Failed |
| 11-20 | best gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Failed |
| 21-30 | no gear | 6 | 98.8 | 98.9 | -0.0 | >= 10 | 98.6 | Failed |
| 21-30 | mid gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Failed |
| 21-30 | best gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Failed |
| 31-40 | no gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Failed |
| 31-40 | mid gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Failed |
| 31-40 | best gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Failed |
| 41-53 | no gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Failed |
| 41-53 | mid gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Failed |
| 41-53 | best gear | 6 | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Failed |

### The single rotation found for each band and gear tier, searched across the band's sampled recipes (average quality in points)

| Band | Gear | Best single rotation for the band | Training | Selection | Hold-out | Adaptive on these recipes |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | wire x3 > tune x2 > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire > discharge_calibration > tune > wire > discharge_calibration > wire | 50.7 | 50.6 | 50.9 | 48.6 |
| 1-10 | mid gear | tune x4 > discharge_calibration > tune > wire > tune > discharge_calibration > wire x3 > tune > discharge_calibration > wire x3 > discharge_calibration > tune > wire x3 | 99.5 | 99.7 | 99.6 | 92.8 |
| 1-10 | best gear | discharge_assembly > tune > discharge_calibration > tune x2 > patch > tune > discharge_calibration > wire > tune x2 > discharge_calibration > patch > tune > wire x3 > discharge_calibration > discharge_assembly > wire x2 | 99.7 | 99.8 | 99.7 | 97.5 |
| 11-20 | no gear | wire x3 > tune x3 > discharge_calibration > wire x3 > tune x2 > discharge_calibration > wire x3 > discharge_calibration > wire > discharge_calibration > wire > discharge_calibration > wire | 87.3 | 87.2 | 86.7 | 79.9 |
| 11-20 | mid gear | boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost | 100.0 | 100.0 | 100.0 | 99.9 |
| 11-20 | best gear | wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | micro_tune > boost > tune > wire > tune > micro_tune x2 > wire > tune x2 > discharge_calibration > tune x4 > discharge_calibration > wire > tune x2 > wire x7 > discharge_calibration > wire > discharge_calibration > wire x3 > discharge_calibration > wire | 98.9 | 98.8 | 98.9 | 98.8 |
| 21-30 | mid gear | boost > tune x5 > discharge_calibration > tune x3 > wire x5 > discharge_assembly > tune > wire > tune > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | boost > tune x5 > discharge_calibration > tune x2 > wire x5 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | boost > tune x5 > discharge_calibration > tune x4 > wire x4 > discharge_assembly > tune > wire x3 > tune > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | boost > tune x5 > discharge_calibration > tune x3 > wire x5 > discharge_assembly > tune > wire > tune > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | boost > tune x5 > discharge_calibration > tune > wire x5 > discharge_assembly > tune > wire > tune > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | boost > tune x6 > discharge_calibration > tune x2 > wire x6 > discharge_assembly > tune > wire x2 | 100.0 | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | wire > tune x2 > wire > tune x2 > wire > tune x2 > wire > tune x2 > wire > tune x2 > wire > tune x2 > wire > tune x2 > wire > tune x2 > wire > tune x2 > wire > tune x2 > wire > tune x2 > wire > tune x2 > wire > tune x2 > wire | 100.0 | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | boost > tune x5 > discharge_calibration > tune > wire x5 > discharge_assembly > tune > wire x2 | 100.0 | 100.0 | 100.0 | 100.0 |

### Reported metric, not gated: the best fixed rotation for each recipe alone, and what the band's single rotation scores on the same recipe (average quality in points)

| Band | Gear | Recipe | Best rotation for this recipe alone (reported) | Its hold-out | The single rotation on this recipe | Adaptive on this recipe |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | wen_cft_smth1 (level 3) | wire x5 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire | 37.5 | 33.3 | 33.4 |
| 1-10 | no gear | config_ind1 (level 5) | wire x2 > tune > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire x2 > discharge_calibration > wire | 45.5 | 45.3 | 45.5 |
| 1-10 | no gear | d_hd_pva1 (level 7) | wire x2 > patch > wire x3 > tune > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire > discharge_calibration > wire | 52.2 | 48.1 | 44.6 |
| 1-10 | no gear | song_460 (level 8) | wire x3 > tune x2 > discharge_calibration > tune > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire | 59.7 | 59.2 | 54.1 |
| 1-10 | no gear | d_lg_pvw1 (level 10) | wire x2 > tune x2 > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire | 59.5 | 59.9 | 56.0 |
| 1-10 | no gear | id_soulstrike1 (level 10) | wire x3 > tune x2 > discharge_calibration > wire x5 > discharge_calibration > wire > discharge_calibration > wire > discharge_calibration > wire > discharge_calibration > wire | 61.2 | 59.8 | 58.2 |
| 1-10 | mid gear | wen_cft_smth1 (level 3) | tune x5 > discharge_calibration > tune x2 > wire > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire | 100.0 | 100.0 | 90.6 |
| 1-10 | mid gear | config_ind1 (level 5) | wire > tune > discharge_calibration > wire x2 > tune > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire | 100.0 | 99.6 | 76.6 |
| 1-10 | mid gear | d_hd_pva1 (level 7) | wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire | 100.0 | 100.0 | 99.3 |
| 1-10 | mid gear | song_460 (level 8) | tune x4 > discharge_calibration > tune x4 > discharge_calibration > wire x4 > discharge_calibration > wire x5 | 100.0 | 100.0 | 93.8 |
| 1-10 | mid gear | d_lg_pvw1 (level 10) | wire > tune > discharge_calibration > tune x4 > discharge_calibration > tune x4 > discharge_calibration > wire x3 > discharge_assembly > wire x4 > discharge_calibration > wire x3 | 100.0 | 98.0 | 97.1 |
| 1-10 | mid gear | id_soulstrike1 (level 10) | tune x5 > discharge_calibration > tune x3 > discharge_calibration > wire > tune > wire x3 > discharge_assembly > wire > discharge_calibration > wire x3 | 100.0 | 100.0 | 99.2 |
| 1-10 | best gear | wen_cft_smth1 (level 3) | tune x5 > discharge_calibration > tune x4 > wire > discharge_assembly > wire x3 > tune > discharge_assembly > wire | 100.0 | 99.4 | 88.6 |
| 1-10 | best gear | config_ind1 (level 5) | wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire | 100.0 | 99.5 | 98.6 |
| 1-10 | best gear | d_hd_pva1 (level 7) | tune x5 > discharge_calibration > tune x4 > wire x7 | 100.0 | 100.0 | 99.3 |
| 1-10 | best gear | song_460 (level 8) | tune x5 > discharge_calibration > tune x3 > wire > discharge_calibration > wire x3 > discharge_assembly > wire x3 > tune > discharge_calibration > wire | 100.0 | 99.8 | 100.0 |
| 1-10 | best gear | d_lg_pvw1 (level 10) | wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire | 100.0 | 99.5 | 98.8 |
| 1-10 | best gear | id_soulstrike1 (level 10) | tune x5 > discharge_calibration > tune x4 > wire > discharge_assembly > wire x5 | 100.0 | 100.0 | 100.0 |
| 11-20 | no gear | dhbr002 (level 13) | wire x3 > tune x3 > discharge_calibration > wire x2 > discharge_calibration > wire > discharge_calibration > wire x4 > discharge_calibration > wire | 74.7 | 74.0 | 62.4 |
| 11-20 | no gear | song_579 (level 15) | wire x4 > tune x3 > discharge_calibration > wire > discharge_calibration > wire x5 > discharge_calibration > wire x2 > discharge_calibration > wire x2 > discharge_calibration > wire | 78.5 | 78.5 | 74.8 |
| 11-20 | no gear | eva_boost_1 (level 18) | wire > tune x5 > discharge_calibration > wire x4 > discharge_calibration > wire x5 > discharge_calibration > wire > discharge_calibration > wire | 79.9 | 82.4 | 83.0 |
| 11-20 | no gear | d_ar_pvs2 (level 19) | boost > tune x3 > discharge_calibration > wire x4 > discharge_calibration > wire > discharge_calibration > wire x5 > discharge_calibration > wire x2 > discharge_calibration > wire | 91.8 | 94.0 | 85.7 |
| 11-20 | no gear | darg002 (level 20) | wire x2 > tune > wire > tune x5 > discharge_calibration > wire x4 > discharge_calibration > wire x3 > discharge_calibration > wire x3 > discharge_calibration > wire > discharge_calibration > wire | 100.0 | 98.9 | 94.1 |
| 11-20 | no gear | id_pinningtoss1 (level 20) | wire > tune x5 > discharge_calibration > wire x6 > discharge_calibration > wire > discharge_calibration > wire x3 > discharge_calibration > wire x2 > discharge_calibration > wire | 93.0 | 92.2 | 79.3 |
| 11-20 | mid gear | dhbr002 (level 13) | wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire > discharge_calibration > boost > wire | 100.0 | 100.0 | 99.4 |
| 11-20 | mid gear | song_579 (level 15) | tune x6 > discharge_calibration > wire > tune x3 > wire x2 > discharge_assembly > wire x4 | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | eva_boost_1 (level 18) | wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire > discharge_calibration > tune > wire | 100.0 | 100.0 | 99.9 |
| 11-20 | mid gear | d_ar_pvs2 (level 19) | tune x5 > discharge_calibration > tune x4 > wire x2 > discharge_assembly > tune > wire x4 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | darg002 (level 20) | boost > tune x5 > discharge_calibration > tune x2 > wire x5 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | mid gear | id_pinningtoss1 (level 20) | tune x6 > discharge_calibration > tune x4 > wire x2 > discharge_assembly > wire x4 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | dhbr002 (level 13) | wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire > tune > discharge_calibration > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | song_579 (level 15) | tune x6 > discharge_calibration > tune x2 > wire x4 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | eva_boost_1 (level 18) | tune x4 > discharge_calibration > tune x5 > wire x2 > discharge_assembly > wire > tune > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | d_ar_pvs2 (level 19) | tune x5 > discharge_calibration > tune x3 > wire x3 > discharge_assembly > tune > wire x4 | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | darg002 (level 20) | boost > tune x5 > discharge_calibration > boost > wire x4 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | id_pinningtoss1 (level 20) | tune x6 > discharge_calibration > tune > wire > tune > wire x3 > discharge_assembly > wire x2 > tune > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | wen_cft_smth3 (level 23) | boost x2 > tune x3 > discharge_calibration > wire x9 > discharge_calibration > wire > discharge_calibration > wire | 91.6 | 93.2 | 93.1 |
| 21-30 | no gear | ion_armor_3 (level 25) | boost > tune x4 > discharge_calibration > tune x6 > wire x2 > discharge_assembly > wire > tune > wire x2 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | de_vblade_b1 (level 27) | tune x7 > discharge_calibration > tune x4 > wire x4 > discharge_assembly > tune > wire x4 | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | dhbe003 (level 29) | boost > tune x5 > discharge_calibration > tune x4 > wire x4 > discharge_assembly > wire x3 | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | dsbr003 (level 30) | boost > tune x6 > discharge_calibration > tune > wire x6 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | id_linebreaker3 (level 30) | tune x8 > discharge_calibration > tune x2 > wire x6 > discharge_assembly > wire > tune > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | wen_cft_smth3 (level 23) | tune x8 > discharge_calibration > tune > wire x6 > discharge_assembly > wire > tune > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | ion_armor_3 (level 25) | boost > tune x5 > discharge_calibration > tune x3 > wire x5 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | de_vblade_b1 (level 27) | tune x8 > discharge_calibration > wire > tune > wire x5 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | dhbe003 (level 29) | boost > tune x5 > discharge_calibration > tune x2 > wire x5 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | dsbr003 (level 30) | boost > tune x5 > discharge_calibration > tune x2 > wire x5 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | mid gear | id_linebreaker3 (level 30) | tune x7 > discharge_calibration > wire > tune x2 > wire x4 > discharge_assembly > wire > tune > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | wen_cft_smth3 (level 23) | boost > tune x4 > discharge_calibration > tune x4 > wire x4 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | ion_armor_3 (level 25) | boost > tune x5 > discharge_calibration > tune x2 > wire x5 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | de_vblade_b1 (level 27) | tune x8 > discharge_calibration > wire x6 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | dhbe003 (level 29) | boost > tune x5 > discharge_calibration > tune > wire x5 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | dsbr003 (level 30) | boost > tune x5 > discharge_calibration > boost > wire x4 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | id_linebreaker3 (level 30) | tune x7 > discharge_calibration > tune > wire x5 > discharge_assembly > wire > tune > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | aen_cft_fab4 (level 33) | tune x7 > discharge_calibration > tune x3 > wire x5 > discharge_assembly > tune > wire x2 > tune > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | config_ind4 (level 35) | boost > tune x5 > discharge_calibration > tune x4 > wire x4 > discharge_assembly > tune > wire x3 | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | d_lg_mws4 (level 37) | boost > tune x5 > discharge_calibration > tune x3 > wire x5 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | d_hd_mwa4 (level 39) | boost > tune x5 > discharge_calibration > tune > wire > tune > wire x4 > discharge_assembly > tune > wire x2 | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | dfhl004 (level 40) | boost > tune x5 > discharge_calibration > tune x2 > wire x5 > discharge_assembly > tune > wire x2 | 100.0 | 100.0 | 100.0 |
| 31-40 | no gear | id_conctoss2 (level 40) | tune x8 > discharge_calibration > tune x2 > wire x6 > discharge_assembly > wire > tune > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | aen_cft_fab4 (level 33) | boost > tune x4 > discharge_calibration > tune x5 > wire x3 > discharge_assembly > wire x3 > tune > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | config_ind4 (level 35) | boost > tune x5 > discharge_calibration > tune x2 > wire x4 > discharge_assembly > wire x3 > tune > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | d_lg_mws4 (level 37) | boost > tune x5 > discharge_calibration > tune x2 > wire x5 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | d_hd_mwa4 (level 39) | boost > tune x5 > discharge_calibration > wire x2 > tune > wire x3 > discharge_assembly > wire x2 | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | dfhl004 (level 40) | boost > tune x5 > discharge_calibration > wire x2 > tune > wire x4 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 31-40 | mid gear | id_conctoss2 (level 40) | tune x7 > discharge_calibration > wire > tune x2 > wire x5 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | aen_cft_fab4 (level 33) | tune x7 > discharge_calibration > wire > tune > wire x4 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | config_ind4 (level 35) | boost > tune x5 > discharge_calibration > tune x2 > wire x5 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | d_lg_mws4 (level 37) | boost > tune x5 > discharge_calibration > tune > wire x5 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | d_hd_mwa4 (level 39) | boost > tune x5 > discharge_calibration > boost > wire x4 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | dfhl004 (level 40) | boost > tune x5 > discharge_calibration > boost > wire x4 > discharge_assembly | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | id_conctoss2 (level 40) | tune x7 > discharge_calibration > tune > wire x5 > discharge_assembly > wire > tune > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | dhbr005 (level 43) | boost > tune x5 > discharge_calibration > tune x3 > wire x5 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | therm_amp_4 (level 46) | boost > tune x6 > discharge_calibration > wire x6 > discharge_assembly > tune > wire x2 | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | wen_eva5 (level 48) | boost > tune x6 > discharge_calibration > tune x2 > wire x6 > discharge_assembly > wire > tune > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | d_lg_mpv5 (level 49) | boost > tune x6 > discharge_calibration > boost > wire x5 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | id_infusion2 (level 50) | boost > tune x6 > discharge_calibration > wire > tune x2 > wire x3 > discharge_assembly > wire x5 | 100.0 | 100.0 | 100.0 |
| 41-53 | no gear | cap_quadlas1 (level 52) | boost > tune x6 > discharge_calibration > tune x2 > wire x6 > discharge_assembly > tune > wire x2 | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | dhbr005 (level 43) | boost > tune x5 > discharge_calibration > tune x2 > wire x5 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | therm_amp_4 (level 46) | boost > tune x6 > discharge_calibration > wire x6 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | wen_eva5 (level 48) | boost > tune x6 > discharge_calibration > tune > wire x6 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | d_lg_mpv5 (level 49) | boost > tune x6 > discharge_calibration > wire x6 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | id_infusion2 (level 50) | boost > tune x6 > discharge_calibration > tune > wire x4 > discharge_assembly > wire x4 | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | cap_quadlas1 (level 52) | boost > tune x5 > discharge_calibration > tune x2 > wire x6 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | dhbr005 (level 43) | boost > tune x5 > discharge_calibration > boost > wire x4 > discharge_assembly > tune > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | therm_amp_4 (level 46) | boost > tune x5 > discharge_calibration > boost > wire x4 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | wen_eva5 (level 48) | tune x6 > discharge_calibration > tune x2 > wire x5 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | d_lg_mpv5 (level 49) | boost > tune x5 > discharge_calibration > boost > wire x4 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | id_infusion2 (level 50) | boost > tune x5 > discharge_calibration > tune > wire x5 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | cap_quadlas1 (level 52) | boost > tune x6 > discharge_calibration > wire x6 > discharge_assembly > wire | 100.0 | 100.0 | 100.0 |

## Length

### Actions per attempt of the adaptive player at level (item craft at level)

| Band | Gear | Recipes played | Mean actions per attempt | Shortest recipe | Longest recipe | Stalled attempts | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 14.4 | 12.1 | 16.3 | 0 | 10-20 | Passed |
| 1-10 | mid gear | 6 | 17.1 | 16.0 | 17.8 | 0 | 10-20 | Passed |
| 1-10 | best gear | 6 | 15.6 | 14.1 | 16.4 | 0 | 10-20 | Passed |
| 11-20 | no gear | 6 | 16.7 | 15.1 | 18.3 | 0 | 10-20 | Passed |
| 11-20 | mid gear | 6 | 16.5 | 15.0 | 18.4 | 0 | 10-20 | Passed |
| 11-20 | best gear | 6 | 14.5 | 13.5 | 16.3 | 0 | 10-20 | Passed |
| 21-30 | no gear | 6 | 18.1 | 16.1 | 19.4 | 0 | 10-20 | Passed |
| 21-30 | mid gear | 6 | 16.4 | 14.8 | 17.6 | 0 | 10-20 | Passed |
| 21-30 | best gear | 6 | 14.4 | 12.4 | 15.5 | 0 | 10-20 | Passed |
| 31-40 | no gear | 6 | 17.2 | 15.7 | 18.6 | 0 | 10-20 | Passed |
| 31-40 | mid gear | 6 | 15.5 | 14.2 | 16.7 | 0 | 10-20 | Passed |
| 31-40 | best gear | 6 | 13.7 | 12.8 | 14.8 | 0 | 10-20 | Passed |
| 41-53 | no gear | 6 | 16.7 | 14.2 | 18.1 | 0 | 10-20 | Passed |
| 41-53 | mid gear | 6 | 14.9 | 12.6 | 16.5 | 0 | 10-20 | Passed |
| 41-53 | best gear | 6 | 13.2 | 11.6 | 14.3 | 0 | 10-20 | Passed |

## Economy parity

### Component stacks consumed per item made, failures included (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Parity rule | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 2.33 | 2.40 | 0.97 | 100% | 96% | 49% | 1% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | mid gear | 6 | 2.37 | 2.39 | 0.99 | 98% | 97% | 93% | 10% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | best gear | 6 | 2.36 | 2.37 | 0.99 | 98% | 98% | 98% | 24% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | no gear | 6 | 2.00 | 2.10 | 0.95 | 100% | 93% | 82% | 3% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | mid gear | 6 | 2.00 | 2.03 | 0.98 | 100% | 97% | 100% | 12% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | best gear | 6 | 2.00 | 2.01 | 0.99 | 100% | 99% | 100% | 28% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | no gear | 6 | 2.50 | 2.56 | 0.98 | 100% | 97% | 99% | 8% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | mid gear | 6 | 2.50 | 2.52 | 0.99 | 100% | 99% | 100% | 15% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | best gear | 6 | 2.50 | 2.52 | 0.99 | 100% | 99% | 100% | 29% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | no gear | 6 | 2.50 | 2.54 | 0.98 | 100% | 98% | 100% | 23% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | mid gear | 6 | 2.50 | 2.52 | 0.99 | 100% | 99% | 100% | 36% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | best gear | 6 | 2.50 | 2.52 | 0.99 | 100% | 99% | 100% | 49% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | no gear | 6 | 2.50 | 2.41 | 1.04 | 100% | 82% | 100% | 35% | yes | 0% / 17% | at most +10% | Passed |
| 41-53 | mid gear | 6 | 2.50 | 2.72 | 0.92 | 100% | 87% | 100% | 55% | yes | 0% / 0% | at most +10% | Passed |
| 41-53 | best gear | 6 | 2.50 | 2.58 | 0.97 | 100% | 94% | 100% | 75% | yes | 0% / 0% | within 10% | Passed |

## XP rate

### Item crafts: XP per hour against the legacy loop (6.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per craft | Legacy actions per craft | Legacy XP per hour | New XP per craft | New actions per craft | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 586 | 5.6 | 62435 | 2041 | 14.4 | 63587 | 1.02 | - | Passed |
| 1-10 | no gear, 1 enhancement | 424 | 6.5 | 39443 | 2185 | 18.9 | 52066 | 1.32 | - | Passed |
| 1-10 | no gear, 2 enhancements | 185 | 6.0 | 18520 | 2172 | 18.8 | 51898 | 2.80 | - | Passed |
| 1-10 | no gear, 3 enhancements | 90 | 6.0 | 9000 | 2186 | 18.9 | 52173 | 5.80 | - | Passed |
| 1-10 | mid gear | 645 | 7.6 | 51038 | 2623 | 17.1 | 69032 | 1.35 | - | Passed |
| 1-10 | mid gear, 1 enhancement | 612 | 7.8 | 46899 | 2362 | 19.7 | 54025 | 1.15 | - | Passed |
| 1-10 | mid gear, 2 enhancements | 589 | 8.4 | 41861 | 2316 | 19.9 | 52461 | 1.25 | - | Passed |
| 1-10 | mid gear, 3 enhancements | 535 | 8.8 | 36480 | 2359 | 19.7 | 53914 | 1.48 | - | Passed |
| 1-10 | best gear | 733 | 8.8 | 49866 | 2693 | 15.6 | 77565 | 1.56 | - | Passed |
| 1-10 | best gear, 1 enhancement | 722 | 9.1 | 47421 | 2497 | 17.9 | 62809 | 1.32 | - | Passed |
| 1-10 | best gear, 2 enhancements | 716 | 9.6 | 44932 | 2464 | 17.6 | 62904 | 1.40 | - | Passed |
| 1-10 | best gear, 3 enhancements | 689 | 10.0 | 41440 | 2486 | 17.8 | 62843 | 1.52 | - | Passed |
| 11-20 | no gear | 577 | 8.0 | 43466 | 1662 | 16.7 | 44694 | 1.03 | - | Passed |
| 11-20 | no gear, 1 enhancement | 390 | 8.7 | 27067 | 1751 | 19.1 | 41327 | 1.53 | - | Passed |
| 11-20 | no gear, 2 enhancements | 262 | 7.3 | 21603 | 1738 | 19.1 | 40885 | 1.89 | - | Passed |
| 11-20 | no gear, 3 enhancements | 251 | 8.0 | 18827 | 1749 | 19.1 | 41214 | 2.19 | - | Passed |
| 11-20 | mid gear | 656 | 10.4 | 38034 | 1847 | 16.5 | 50373 | 1.32 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 639 | 11.6 | 32975 | 1845 | 17.8 | 46532 | 1.41 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 621 | 12.8 | 29212 | 1845 | 17.8 | 46756 | 1.60 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 568 | 13.9 | 24621 | 1844 | 17.8 | 46706 | 1.90 | - | Passed |
| 11-20 | best gear | 766 | 14.6 | 31378 | 1848 | 14.5 | 57429 | 1.83 | - | Passed |
| 11-20 | best gear, 1 enhancement | 753 | 15.3 | 29481 | 1848 | 15.7 | 52979 | 1.80 | - | Passed |
| 11-20 | best gear, 2 enhancements | 739 | 16.1 | 27545 | 1848 | 15.8 | 52671 | 1.91 | - | Passed |
| 11-20 | best gear, 3 enhancements | 718 | 16.6 | 25960 | 1848 | 15.8 | 52788 | 2.03 | - | Passed |
| 21-30 | no gear | 630 | 11.3 | 33568 | 1372 | 18.1 | 34124 | 1.02 | - | Passed |
| 21-30 | no gear, 1 enhancement | 618 | 12.3 | 30162 | 1373 | 19.1 | 32304 | 1.07 | - | Passed |
| 21-30 | no gear, 2 enhancements | 605 | 13.1 | 27717 | 1373 | 19.1 | 32266 | 1.16 | - | Passed |
| 21-30 | no gear, 3 enhancements | 575 | 13.8 | 24961 | 1375 | 19.2 | 32297 | 1.29 | - | Passed |
| 21-30 | mid gear | 684 | 16.8 | 24373 | 1380 | 16.4 | 37850 | 1.55 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 676 | 17.4 | 23323 | 1380 | 17.3 | 35829 | 1.54 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 657 | 17.7 | 22315 | 1380 | 17.2 | 36150 | 1.62 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 655 | 17.9 | 21904 | 1380 | 17.4 | 35737 | 1.63 | - | Passed |
| 21-30 | best gear | 766 | 22.5 | 20425 | 1380 | 14.4 | 43019 | 2.11 | - | Passed |
| 21-30 | best gear, 1 enhancement | 761 | 22.7 | 20084 | 1380 | 15.0 | 41265 | 2.05 | - | Passed |
| 21-30 | best gear, 2 enhancements | 752 | 23.3 | 19334 | 1380 | 14.9 | 41551 | 2.15 | - | Passed |
| 21-30 | best gear, 3 enhancements | 741 | 23.1 | 19223 | 1380 | 15.0 | 41385 | 2.15 | - | Passed |
| 31-40 | no gear | 723 | 14.5 | 29893 | 1200 | 17.2 | 31355 | 1.05 | - | Passed |
| 31-40 | no gear, 1 enhancement | 680 | 14.5 | 28109 | 1200 | 18.2 | 29701 | 1.06 | - | Passed |
| 31-40 | no gear, 2 enhancements | 621 | 15.2 | 24578 | 1200 | 18.1 | 29883 | 1.22 | - | Passed |
| 31-40 | no gear, 3 enhancements | 588 | 16.0 | 22065 | 1200 | 18.2 | 29636 | 1.34 | - | Passed |
| 31-40 | mid gear | 808 | 18.9 | 25624 | 1200 | 15.5 | 34943 | 1.36 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 784 | 19.5 | 24177 | 1200 | 16.1 | 33521 | 1.39 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 734 | 19.1 | 23010 | 1200 | 16.2 | 33393 | 1.45 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 683 | 18.4 | 22225 | 1200 | 16.1 | 33490 | 1.51 | - | Passed |
| 31-40 | best gear | 888 | 24.6 | 21677 | 1200 | 13.7 | 39456 | 1.82 | - | Passed |
| 31-40 | best gear, 1 enhancement | 881 | 25.3 | 20886 | 1200 | 14.3 | 37848 | 1.81 | - | Passed |
| 31-40 | best gear, 2 enhancements | 854 | 25.3 | 20213 | 1200 | 14.2 | 37972 | 1.88 | - | Passed |
| 31-40 | best gear, 3 enhancements | 815 | 23.8 | 20539 | 1200 | 14.1 | 38341 | 1.87 | - | Passed |
| 41-53 | no gear | 723 | 17.5 | 24786 | 1250 | 16.7 | 33752 | 1.36 | - | Passed |
| 41-53 | no gear, 1 enhancement | 633 | 16.0 | 23699 | 1250 | 16.7 | 33705 | 1.42 | - | Passed |
| 41-53 | no gear, 2 enhancements | 537 | 16.6 | 19443 | 1250 | 16.8 | 33500 | 1.72 | - | Passed |
| 41-53 | no gear, 3 enhancements | 455 | 17.4 | 15739 | 1250 | 16.8 | 33384 | 2.12 | - | Passed |
| 41-53 | mid gear | 871 | 23.9 | 21886 | 1250 | 14.9 | 37658 | 1.72 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 777 | 22.1 | 21084 | 1250 | 15.0 | 37481 | 1.78 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 679 | 20.0 | 20347 | 1250 | 15.2 | 36892 | 1.81 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 587 | 19.3 | 18273 | 1250 | 15.2 | 36892 | 2.02 | - | Passed |
| 41-53 | best gear | 1042 | 28.5 | 21964 | 1250 | 13.2 | 42769 | 1.95 | - | Passed |
| 41-53 | best gear, 1 enhancement | 961 | 27.5 | 20940 | 1250 | 13.2 | 42758 | 2.04 | - | Passed |
| 41-53 | best gear, 2 enhancements | 880 | 25.9 | 20354 | 1250 | 13.2 | 42505 | 2.09 | - | Passed |
| 41-53 | best gear, 3 enhancements | 775 | 23.3 | 19959 | 1250 | 13.4 | 42055 | 2.11 | - | Passed |

## Enhancement parity

### Rank 50, best gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 85%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 6 | 60-100 | 34% | 99% | 94% | 0.92 | 100% | 100.0% | 1.00 (+/- 0.00) | 1.08 | >= 1.00 | Passed |
| 41-45 | 2 | 6 | 60-100 | 69% | 99% | 86% | 1.69 | 100% | 100.0% | 2.00 (+/- 0.00) | 1.18 | >= 1.00 | Passed |
| 41-45 | 3 | 6 | 60-100 | 103% | 99% | 77% | 2.28 | 100% | 100.0% | 3.00 (+/- 0.00) | 1.31 | >= 1.00 | Passed |
| 46-50 | 1 | 6 | 60-100 | 39% | 99% | 81% | 0.79 | 100% | 100.0% | 1.00 (+/- 0.00) | 1.26 | >= 1.00 | Passed |
| 46-50 | 2 | 6 | 60-100 | 79% | 99% | 67% | 1.31 | 100% | 100.0% | 2.00 (+/- 0.00) | 1.52 | >= 1.00 | Passed |
| 46-50 | 3 | 6 | 60-100 | 118% | 99% | 57% | 1.67 | 100% | 100.0% | 3.00 (+/- 0.00) | 1.80 | >= 1.00 | Passed |
| 51-53 | 1 | 6 | 60-70 | 7% | 32% | 6% | 0.02 | 100% | 100.0% | 1.00 (+/- 0.00) | 57.75 | >= 1.00 | Passed |
| 51-53 | 2 | 6 | 60-70 | 15% | 22% | 5% | 0.02 | 100% | 100.0% | 2.00 (+/- 0.00) | 99.17 | >= 1.00 | Passed |
| 51-53 | 3 | 6 | 60-70 | 22% | 5% | 5% | 0.01 | 100% | 100.0% | 3.00 (+/- 0.00) | 434.78 | >= 1.00 | Passed |

### Thresholds, first / second / third slot, that would meet parity for the quality measured (the strictest set that keeps every case at least at today's, never falling from one slot to the next)

| Scope | Thresholds that meet parity | In steps of 5 | Thresholds in force |
| --- | --- | --- | --- |
| Every band together (the shared thresholds) | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 85% |
| Band 41-45 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 85% |
| Band 46-50 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 85% |
| Band 51-53 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 85% |

### Rank 50, best gear: quality of the items the new minigame made, in whole percent, by band and slots filled

| Band | Slots filled | Items made | 10th percentile | 25th percentile | Median | 75th percentile | 90th percentile | Made at 100% |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 99% |
| 41-45 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 41-45 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 46-50 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 46-50 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 97% |
| 46-50 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 97% |
| 51-53 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 51-53 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 97% |
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
