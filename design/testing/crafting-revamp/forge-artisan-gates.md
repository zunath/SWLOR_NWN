# Crafting revamp: balance gates of the Plasma Forge (Artisan line, Cold Working) minigame

- Command: `SWLOR.CraftSim gates --scenario forge-artisan --gates rotation,length,materials,xp,enhancements --runs 30 --seed 1 --recipes-per-band 6 --out design/testing/crafting-revamp/forge-artisan-gates.md`
- Seed 1; 30 runs per case.

## Inputs

- Scenario: Plasma Forge (Artisan line, Cold Working) (`forge-artisan`), Smithery recipes, minigame Forge.
- Quality: the quality meter's share of its maximum when the item is made, in points (0-100). A broken or abandoned piece counts 0, so every average includes the crafts that failed.
- Crafter: the rank the scenario's cohort gives each recipe (by default the recipe's level, at most 50, so a crafter works at their own level), with no food or status bonuses and no perks unless a gate names a style or the scenario's limitations say it plays a perk loadout. Gear levels are the plan's none, middle and best tier of the skill's crafting gear:
  - no gear (Control 0, Craftsmanship 0, CP 0)
  - mid gear (Control 13, Craftsmanship 12, CP 23)
  - best gear (Control 29, Craftsmanship 30, CP 37)
- Recipes: 1-10: 6 of 119; 11-20: 6 of 142; 21-30: 6 of 195; 31-40: 6 of 134; 41-53: 6 of 187 recipes played.
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
- Length target: 10-20 actions (item craft at level).
- Enhancement thresholds in force (`CraftOutcomeRules`, read by gate 7): 50% / 85% / 85% for slots 1, 2 and 3. The scenario applies enhancements to its engine's progress meter.
- Styles: none offered, so the style-balance gate is not applicable.
- Gates run: No fixed rotation, Length, Economy parity, XP rate, Enhancement parity. The others were not run, so this report cannot show that the minigame passes.

## Verdicts

| Gate | Verdict | Summary |
| --- | --- | --- |
| No fixed rotation | Failed | One fixed rotation comes within 10 points of adaptive play in 3 case(s): 1-10 / best gear; 41-53 / mid gear; 41-53 / best gear (smallest margin 5.8 in 41-53 / mid gear). The best rotation for each recipe alone, a reported metric, is -5.1 to 3.6 points behind adaptive play. |
| Length | Failed | Outside 10-20 actions (item craft at level): 21-30 / best gear (9.7); 31-40 / best gear (8.7); 41-53 / best gear (9.1). |
| Economy parity | Passed | Component stacks per item are within 10% of the legacy baseline where its success rate is at least 90%, and at most 10% above it where it is lower, at no less quality, in every band and gear tier. 1 case(s) use more than 10% less material than today, which passes. |
| XP rate | Failed | Item crafts: XP per hour falls below 1.00x the legacy rate in 1 case(s): 11-20 / no gear (0.98x). |
| Enhancement parity | Passed | At rank 50 in best gear, the thresholds in force (50% / 85% / 85%) transfer at least 1.00 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 1.05 times today's). Thresholds that meet parity in every band for this quality: 100% / 100% / 100%. |

## No fixed rotation

### Average quality: adaptive play against the best single fixed rotation of each band and gear tier (quality points; a broken piece counts 0)

| Band | Gear | Recipes played | Adaptive avg quality | Best single rotation avg quality | Margin | Required | Best rotation per recipe avg quality (reported) | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 80.3 | 65.9 | 14.4 | >= 10 | 81.8 | Passed |
| 1-10 | mid gear | 6 | 97.7 | 83.0 | 14.6 | >= 10 | 97.4 | Passed |
| 1-10 | best gear | 6 | 99.9 | 91.0 | 8.9 | >= 10 | 100.0 | Failed |
| 11-20 | no gear | 6 | 69.3 | 56.4 | 13.0 | >= 10 | 72.6 | Passed |
| 11-20 | mid gear | 6 | 92.3 | 73.0 | 19.3 | >= 10 | 97.5 | Passed |
| 11-20 | best gear | 6 | 99.3 | 83.3 | 15.9 | >= 10 | 99.8 | Passed |
| 21-30 | no gear | 6 | 96.3 | 49.8 | 46.5 | >= 10 | 92.7 | Passed |
| 21-30 | mid gear | 6 | 99.8 | 55.2 | 44.6 | >= 10 | 97.5 | Passed |
| 21-30 | best gear | 6 | 99.2 | 59.0 | 40.2 | >= 10 | 98.9 | Passed |
| 31-40 | no gear | 6 | 92.6 | 80.5 | 12.1 | >= 10 | 94.9 | Passed |
| 31-40 | mid gear | 6 | 98.9 | 79.0 | 19.9 | >= 10 | 96.3 | Passed |
| 31-40 | best gear | 6 | 99.9 | 88.4 | 11.5 | >= 10 | 99.2 | Passed |
| 41-53 | no gear | 6 | 95.6 | 85.5 | 10.1 | >= 10 | 94.9 | Passed |
| 41-53 | mid gear | 6 | 99.3 | 93.5 | 5.8 | >= 10 | 98.4 | Failed |
| 41-53 | best gear | 6 | 99.9 | 91.4 | 8.5 | >= 10 | 99.1 | Failed |

### The single rotation found for each band and gear tier, searched across the band's sampled recipes (average quality in points)

| Band | Gear | Best single rotation for the band | Training | Selection | Hold-out | Adaptive on these recipes |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | hydro_press x2 > vibro_hone x6 > ramp_plasma > hydro_press x5 | 66.6 | 67.8 | 65.9 | 80.3 |
| 1-10 | mid gear | vibro_hone > hydro_press > vibro_hone x4 > hydro_press > vibro_hone x2 > ramp_plasma > hydro_press x2 > rebond > hydro_press x2 > ramp_plasma > vibro_hone > hydro_press | 82.0 | 83.1 | 83.0 | 97.7 |
| 1-10 | best gear | hydro_press > vibro_hone x7 > hydro_press > ramp_plasma > hydro_press x2 | 90.9 | 91.0 | 91.0 | 99.9 |
| 11-20 | no gear | micro_hone > hydro_press x2 > micro_hone x2 > vibro_hone x5 > ramp_plasma > micro_hone > hydro_press > cryo_quench > micro_hone > vibro_hone > hydro_press x4 > vibro_hone > hydro_press x4 > vibro_hone x2 | 57.6 | 57.6 | 56.4 | 69.3 |
| 11-20 | mid gear | hydro_press > vibro_hone x6 > ramp_plasma x2 > micro_hone x2 > rebond > ramp_plasma > hydro_press x3 > vibro_hone x3 > hydro_press x2 | 72.8 | 72.6 | 73.0 | 92.3 |
| 11-20 | best gear | micro_hone > rebond > vibro_hone x6 > ramp_plasma > vibro_hone x3 > ramp_plasma > cryo_quench > hydro_press > rebond > micro_hone x2 > ramp_plasma > vibro_hone > hydro_press | 83.3 | 83.3 | 83.3 | 99.3 |
| 21-30 | no gear | vibro_hone x2 > micro_hone > vibro_hone > hydro_press > rebond > ramp_plasma > vibro_hone > hydro_press > cryo_quench x2 > rebond > vibro_hone > hydro_press > cryo_quench > micro_hone x2 > vibro_hone > hydro_press > cryo_quench > hydro_press > vibro_hone > hydro_press x2 > cryo_quench > vibro_hone | 50.8 | 49.6 | 49.8 | 96.3 |
| 21-30 | mid gear | vibro_hone x2 > micro_hone x5 > cryo_quench > micro_hone x2 > ramp_plasma > hydro_press x2 > rebond > cryo_quench > hydro_press > cryo_quench > vibro_hone x2 > hydro_press | 56.4 | 55.3 | 55.2 | 99.8 |
| 21-30 | best gear | micro_hone > vibro_hone > micro_hone > ramp_plasma > micro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > hydro_press > ramp_plasma > micro_hone > rebond > vibro_hone > ramp_plasma > micro_hone > hydro_press x2 > cryo_quench > vibro_hone | 62.0 | 61.6 | 59.0 | 99.2 |
| 31-40 | no gear | temper_hold > hydro_press > vibro_hone > hydro_press > vibro_hone > temper_hold > ramp_plasma > hydro_press > cryo_quench x2 > hydro_press > ramp_plasma > cryo_quench > hydro_press > cryo_quench > hydro_press > ramp_plasma > cryo_quench > micro_hone > rebond > micro_hone x4 > rebond > cryo_quench > hydro_press > vibro_hone > hydro_press > vibro_hone x3 | 81.3 | 79.9 | 80.5 | 92.6 |
| 31-40 | mid gear | hydro_press > vibro_hone x2 > ramp_plasma x2 > hydro_press > cryo_quench > hydro_press > temper_hold > micro_hone x2 > cryo_quench > hydro_press > rebond > ramp_plasma > micro_hone x3 > cryo_quench > rebond > micro_hone > vibro_hone > micro_hone x2 > rebond > vibro_hone | 80.0 | 78.7 | 79.0 | 98.9 |
| 31-40 | best gear | temper_hold > hydro_press x2 > temper_hold > vibro_hone > cryo_quench > ramp_plasma > micro_hone > ramp_plasma > micro_hone > temper_hold > micro_hone > hydro_press > cryo_quench > vibro_hone > ramp_plasma > temper_hold > vibro_hone x2 > ramp_plasma > hydro_press | 89.5 | 89.0 | 88.4 | 99.9 |
| 41-53 | no gear | temper_hold > hydro_press x2 > temper_hold > cryo_quench > temper_hold > vibro_hone > ramp_plasma > temper_hold > rebond > hydro_press > cryo_quench > hydro_press > temper_hold > cryo_quench > hydro_press > cryo_quench > hydro_press > micro_hone > temper_hold > micro_hone x3 > rebond > micro_hone > ramp_plasma > cryo_quench > hydro_press > cryo_quench > vibro_hone | 84.4 | 83.1 | 85.5 | 95.6 |
| 41-53 | mid gear | temper_hold > vibro_hone > hydro_press > temper_hold > vibro_hone > temper_hold > vibro_hone > ramp_plasma > hydro_press > cryo_quench > hydro_press > cryo_quench > temper_hold > micro_hone > hydro_press > cryo_quench > hydro_press > rebond x2 > micro_hone x2 > temper_hold > micro_hone x2 > cryo_quench > vibro_hone x3 | 91.7 | 92.0 | 93.5 | 99.3 |
| 41-53 | best gear | temper_hold > vibro_hone x2 > temper_hold > vibro_hone x2 > temper_hold > vibro_hone > ramp_plasma > temper_hold > hydro_press x2 > cryo_quench > hydro_press > micro_hone > cryo_quench > hydro_press > cryo_quench > hydro_press x2 > rebond > micro_hone x3 > cryo_quench > hydro_press x2 > vibro_hone | 91.4 | 90.8 | 91.4 | 99.9 |

### Reported metric, not gated: the best fixed rotation for each recipe alone, and what the band's single rotation scores on the same recipe (average quality in points)

| Band | Gear | Recipe | Best rotation for this recipe alone (reported) | Its hold-out | The single rotation on this recipe | Adaptive on this recipe |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | bm_bracer (level 3) | vibro_hone > hydro_press > vibro_hone x2 > hydro_press > vibro_hone > hydro_press x2 > vibro_hone > hydro_press > vibro_hone > hydro_press | 49.9 | 40.7 | 58.5 |
| 1-10 | no gear | b_twinpistol (level 6) | hydro_press x2 > vibro_hone x11 > hydro_press x2 | 81.8 | 43.5 | 79.4 |
| 1-10 | no gear | os_hidevest (level 8) | hydro_press x2 > vibro_hone > hydro_press > vibro_hone > hydro_press x2 > vibro_hone x4 > ramp_plasma > hydro_press x3 | 95.3 | 94.5 | 96.1 |
| 1-10 | no gear | sp_crestvis (level 8) | rebond > hydro_press > vibro_hone x4 > hydro_press > vibro_hone x3 > ramp_plasma > hydro_press | 92.1 | 80.2 | 84.2 |
| 1-10 | no gear | advent_bracer (level 10) | hydro_press > vibro_hone > hydro_press x2 > vibro_hone x6 > ramp_plasma > hydro_press x3 > vibro_hone > hydro_press | 90.7 | 82.7 | 82.4 |
| 1-10 | no gear | chef_ring_1 (level 10) | hydro_press x2 > vibro_hone x6 > ramp_plasma > vibro_hone > hydro_press > vibro_hone > ramp_plasma > vibro_hone > hydro_press x3 | 80.9 | 54.0 | 81.2 |
| 1-10 | mid gear | bm_bracer (level 3) | rebond > vibro_hone > hydro_press > vibro_hone > hydro_press > vibro_hone > hydro_press > vibro_hone x2 > rebond > vibro_hone > hydro_press | 85.0 | 87.0 | 91.8 |
| 1-10 | mid gear | b_twinpistol (level 6) | hydro_press x2 > vibro_hone x13 > hydro_press > vibro_hone > hydro_press | 99.9 | 55.0 | 95.7 |
| 1-10 | mid gear | os_hidevest (level 8) | hydro_press > vibro_hone x6 > ramp_plasma > hydro_press x3 > vibro_hone > hydro_press x2 | 100.0 | 100.0 | 99.9 |
| 1-10 | mid gear | sp_crestvis (level 8) | hydro_press > vibro_hone x5 > hydro_press > vibro_hone x2 > ramp_plasma > hydro_press > vibro_hone > hydro_press > ramp_plasma x2 > hydro_press | 100.0 | 98.1 | 99.9 |
| 1-10 | mid gear | advent_bracer (level 10) | vibro_hone > hydro_press > vibro_hone x4 > rebond > vibro_hone > ramp_plasma > vibro_hone > hydro_press x3 > vibro_hone > hydro_press x3 | 100.0 | 99.6 | 100.0 |
| 1-10 | mid gear | chef_ring_1 (level 10) | rebond > vibro_hone x8 > ramp_plasma > hydro_press > vibro_hone x2 > ramp_plasma > hydro_press > rebond > vibro_hone > hydro_press | 99.5 | 58.5 | 98.8 |
| 1-10 | best gear | bm_bracer (level 3) | rebond > hydro_press > vibro_hone > hydro_press > vibro_hone x5 > hydro_press > vibro_hone > ramp_plasma > hydro_press x2 | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | b_twinpistol (level 6) | vibro_hone > hydro_press > vibro_hone x11 > hydro_press > vibro_hone > hydro_press | 100.0 | 65.5 | 100.0 |
| 1-10 | best gear | os_hidevest (level 8) | vibro_hone x4 > hydro_press > vibro_hone x2 > hydro_press x5 | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | sp_crestvis (level 8) | hydro_press > vibro_hone x7 > hydro_press x4 > vibro_hone > hydro_press | 99.8 | 99.8 | 99.7 |
| 1-10 | best gear | advent_bracer (level 10) | vibro_hone > hydro_press > vibro_hone x5 > hydro_press x2 > vibro_hone > hydro_press x3 | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | chef_ring_1 (level 10) | rebond > vibro_hone x5 > ramp_plasma > vibro_hone x4 > hydro_press > vibro_hone > hydro_press > ramp_plasma > rebond > vibro_hone > hydro_press | 100.0 | 80.5 | 99.7 |
| 11-20 | no gear | val_necklace (level 12) | hydro_press > vibro_hone x4 > ramp_plasma > hydro_press x2 > vibro_hone x2 > hydro_press | 52.4 | 46.6 | 45.1 |
| 11-20 | no gear | viv_cap (level 14) | hydro_press x2 > vibro_hone x6 > ramp_plasma > hydro_press x2 > vibro_hone > hydro_press x3 | 87.7 | 78.2 | 85.0 |
| 11-20 | no gear | vet_chn_ring (level 16) | vibro_hone x8 > hydro_press > vibro_hone > hydro_press > vibro_hone > hydro_press > vibro_hone > ramp_plasma > hydro_press | 75.7 | 73.3 | 70.7 |
| 11-20 | no gear | sith_katar (level 18) | hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone x2 > ramp_plasma > hydro_press > ramp_plasma > vibro_hone > hydro_press x2 > micro_hone > vibro_hone > ramp_plasma > vibro_hone > hydro_press > ramp_plasma > vibro_hone > hydro_press > ramp_plasma > vibro_hone > hydro_press > ramp_plasma > vibro_hone > hydro_press > ramp_plasma > vibro_hone > hydro_press > ramp_plasma > vibro_hone > hydro_press > ramp_plasma > vibro_hone > hydro_press > ramp_plasma > vibro_hone > hydro_press > ramp_plasma > vibro_hone | 35.2 | 0.0 | 20.9 |
| 11-20 | no gear | frontier_ring (level 20) | vibro_hone > rebond > micro_hone x2 > hydro_press > vibro_hone x7 > hydro_press > vibro_hone > ramp_plasma > vibro_hone > hydro_press > ramp_plasma > hydro_press | 85.3 | 73.1 | 99.4 |
| 11-20 | no gear | chef_ring_2 (level 20) | hydro_press > micro_hone > vibro_hone x3 > hydro_press > vibro_hone > hydro_press > cryo_quench > hydro_press > cryo_quench > micro_hone x3 > vibro_hone | 99.3 | 66.9 | 95.0 |
| 11-20 | mid gear | val_necklace (level 12) | hydro_press > vibro_hone x5 > ramp_plasma > vibro_hone x3 > ramp_plasma > vibro_hone > hydro_press > vibro_hone > ramp_plasma > hydro_press x2 | 97.3 | 59.3 | 95.2 |
| 11-20 | mid gear | viv_cap (level 14) | hydro_press > vibro_hone x5 > rebond > vibro_hone > ramp_plasma > hydro_press > vibro_hone > hydro_press x2 > vibro_hone > hydro_press x3 | 100.0 | 99.6 | 99.9 |
| 11-20 | mid gear | vet_chn_ring (level 16) | vibro_hone x2 > micro_hone > vibro_hone > rebond > vibro_hone x6 > cryo_quench > vibro_hone > hydro_press > vibro_hone > hydro_press > ramp_plasma > rebond > vibro_hone > hydro_press x2 | 100.0 | 71.1 | 99.2 |
| 11-20 | mid gear | sith_katar (level 18) | hydro_press x2 > vibro_hone x4 > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > hydro_press x2 | 87.4 | 63.7 | 59.8 |
| 11-20 | mid gear | frontier_ring (level 20) | micro_hone x3 > vibro_hone x6 > hydro_press > vibro_hone x2 > hydro_press > rebond > cryo_quench > ramp_plasma > vibro_hone x2 > hydro_press | 100.0 | 69.2 | 99.9 |
| 11-20 | mid gear | chef_ring_2 (level 20) | micro_hone > vibro_hone x6 > hydro_press > vibro_hone x2 > ramp_plasma > vibro_hone x2 > hydro_press > vibro_hone > rebond > ramp_plasma > vibro_hone > hydro_press x2 | 100.0 | 75.2 | 99.9 |
| 11-20 | best gear | val_necklace (level 12) | hydro_press > vibro_hone x5 > ramp_plasma > vibro_hone x3 > ramp_plasma > vibro_hone x2 > hydro_press x2 > ramp_plasma > rebond > ramp_plasma > vibro_hone > hydro_press | 100.0 | 100.0 | 99.7 |
| 11-20 | best gear | viv_cap (level 14) | hydro_press > vibro_hone x6 > hydro_press x3 > vibro_hone > hydro_press x2 | 100.0 | 100.0 | 99.9 |
| 11-20 | best gear | vet_chn_ring (level 16) | hydro_press > vibro_hone > micro_hone > vibro_hone x9 > hydro_press x3 > vibro_hone > hydro_press | 100.0 | 100.0 | 99.8 |
| 11-20 | best gear | sith_katar (level 18) | hydro_press > vibro_hone x5 > ramp_plasma x2 > vibro_hone x2 > micro_hone > ramp_plasma > vibro_hone > ramp_plasma x2 > hydro_press > vibro_hone > hydro_press | 98.7 | 0.0 | 96.2 |
| 11-20 | best gear | frontier_ring (level 20) | hydro_press > rebond > micro_hone x3 > vibro_hone x7 > hydro_press > vibro_hone > hydro_press x3 | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | chef_ring_2 (level 20) | micro_hone x2 > vibro_hone x7 > cryo_quench > hydro_press > ramp_plasma > vibro_hone > hydro_press > vibro_hone > hydro_press | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | vet_twinelec (level 22) | vibro_hone x2 > ramp_plasma > micro_hone > vibro_hone x2 > hydro_press > vibro_hone > hydro_press > vibro_hone x3 > cryo_quench > hydro_press > cryo_quench > vibro_hone | 87.7 | 65.8 | 85.5 |
| 21-30 | no gear | vet_trnsabstaff (level 24) | micro_hone > vibro_hone x8 > hydro_press x2 > vibro_hone > cryo_quench > vibro_hone > hydro_press | 81.6 | 36.3 | 97.4 |
| 21-30 | no gear | mb_guard (level 26) | vibro_hone x6 > ramp_plasma > hydro_press > rebond > vibro_hone > hydro_press x3 > vibro_hone > hydro_press x2 > ramp_plasma > hydro_press | 100.0 | 100.0 | 100.0 |
| 21-30 | no gear | cnt_neck (level 27) | ramp_plasma > cryo_quench > micro_hone x2 > vibro_hone x3 > ramp_plasma > vibro_hone > hydro_press > vibro_hone > ramp_plasma > hydro_press x2 > cryo_quench > hydro_press x2 > cryo_quench > vibro_hone x2 | 87.3 | 0.0 | 95.9 |
| 21-30 | no gear | tg_mantle (level 29) | micro_hone > vibro_hone x4 > ramp_plasma > vibro_hone > rebond > vibro_hone x2 > ramp_plasma > micro_hone x2 > hydro_press x4 > vibro_hone x2 > hydro_press x2 | 99.8 | 96.7 | 99.8 |
| 21-30 | no gear | chef_necklace_3 (level 30) | ramp_plasma > hydro_press x2 > vibro_hone > cryo_quench > hydro_press > cryo_quench > micro_hone x4 > rebond > micro_hone x4 > vibro_hone | 99.7 | 0.0 | 99.4 |
| 21-30 | mid gear | vet_twinelec (level 22) | hydro_press > vibro_hone > micro_hone x2 > ramp_plasma > micro_hone > vibro_hone x7 > micro_hone > ramp_plasma > micro_hone x2 > hydro_press x2 | 97.7 | 71.8 | 99.8 |
| 21-30 | mid gear | vet_trnsabstaff (level 24) | micro_hone > vibro_hone x10 > hydro_press > vibro_hone > hydro_press > vibro_hone > rebond > ramp_plasma > vibro_hone > hydro_press | 100.0 | 62.9 | 99.8 |
| 21-30 | mid gear | mb_guard (level 26) | vibro_hone x6 > ramp_plasma > hydro_press x2 > micro_hone > hydro_press x2 > vibro_hone > rebond > hydro_press x2 | 99.8 | 100.0 | 99.9 |
| 21-30 | mid gear | cnt_neck (level 27) | vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > hydro_press x2 > vibro_hone > cryo_quench > micro_hone > vibro_hone > cryo_quench > hydro_press > ramp_plasma > cryo_quench > micro_hone > rebond x2 > micro_hone > vibro_hone x2 | 100.0 | 0.0 | 99.8 |
| 21-30 | mid gear | tg_mantle (level 29) | vibro_hone x5 > ramp_plasma > vibro_hone > hydro_press > vibro_hone > rebond > vibro_hone > hydro_press x3 > vibro_hone > hydro_press x2 | 100.0 | 96.7 | 100.0 |
| 21-30 | mid gear | chef_necklace_3 (level 30) | micro_hone > vibro_hone x3 > ramp_plasma > vibro_hone > ramp_plasma > vibro_hone > ramp_plasma > hydro_press > vibro_hone > ramp_plasma x2 > hydro_press > cryo_quench > hydro_press > cryo_quench > hydro_press x2 > vibro_hone x3 | 87.4 | 0.0 | 99.7 |
| 21-30 | best gear | vet_twinelec (level 22) | ramp_plasma > micro_hone x3 > vibro_hone x8 > ramp_plasma > hydro_press > micro_hone > vibro_hone > ramp_plasma > hydro_press | 100.0 | 44.8 | 95.9 |
| 21-30 | best gear | vet_trnsabstaff (level 24) | micro_hone > vibro_hone x10 > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press | 100.0 | 44.0 | 99.9 |
| 21-30 | best gear | mb_guard (level 26) | vibro_hone x5 > hydro_press > vibro_hone > hydro_press x4 > vibro_hone > hydro_press | 96.7 | 85.1 | 100.0 |
| 21-30 | best gear | cnt_neck (level 27) | vibro_hone > ramp_plasma > micro_hone > vibro_hone x2 > ramp_plasma > micro_hone > vibro_hone x2 > hydro_press > ramp_plasma > vibro_hone x2 > hydro_press > ramp_plasma > vibro_hone > ramp_plasma > rebond > vibro_hone > hydro_press x2 | 99.9 | 80.5 | 99.8 |
| 21-30 | best gear | tg_mantle (level 29) | vibro_hone x6 > ramp_plasma > vibro_hone > hydro_press x3 > vibro_hone > hydro_press x2 | 100.0 | 22.9 | 100.0 |
| 21-30 | best gear | chef_necklace_3 (level 30) | micro_hone > hydro_press > vibro_hone > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma x2 > hydro_press > cryo_quench > hydro_press > cryo_quench > vibro_hone | 97.0 | 76.7 | 99.8 |
| 31-40 | no gear | sh_charm (level 31) | hydro_press x2 > vibro_hone > hydro_press > vibro_hone > ramp_plasma > vibro_hone x3 > ramp_plasma > vibro_hone > cryo_quench > hydro_press > cryo_quench > micro_hone > rebond > micro_hone > vibro_hone x2 | 100.0 | 92.5 | 99.8 |
| 31-40 | no gear | prm_bul_cloak (level 33) | hydro_press > vibro_hone x2 > ramp_plasma > hydro_press > cryo_quench > hydro_press > ramp_plasma > hydro_press > cryo_quench > hydro_press > ramp_plasma > micro_hone > cryo_quench > hydro_press > vibro_hone > hydro_press > vibro_hone x3 | 96.6 | 93.0 | 90.4 |
| 31-40 | no gear | biolab_belt (level 34) | hydro_press > micro_hone > vibro_hone > hydro_press x2 > ramp_plasma > hydro_press > vibro_hone > cryo_quench > ramp_plasma > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > rebond > vibro_hone | 100.0 | 100.0 | 87.1 |
| 31-40 | no gear | proto_greatsword (level 38) | vibro_hone x8 > ramp_plasma > vibro_hone x2 > hydro_press > vibro_hone > hydro_press > vibro_hone > ramp_plasma > rebond > vibro_hone x2 > hydro_press x2 | 100.0 | 28.8 | 99.7 |
| 31-40 | no gear | majestic_ring001 (level 40) | rebond > temper_hold > hydro_press x2 > ramp_plasma > hydro_press > vibro_hone x2 > ramp_plasma > cryo_quench > hydro_press > ramp_plasma > cryo_quench > micro_hone x4 > vibro_hone > micro_hone > vibro_hone x2 | 73.7 | 93.2 | 94.6 |
| 31-40 | no gear | chef_cloak_4 (level 40) | temper_hold > hydro_press > temper_hold > vibro_hone > hydro_press > ramp_plasma > micro_hone > hydro_press > ramp_plasma x2 > hydro_press > cryo_quench > temper_hold > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > cryo_quench > vibro_hone | 98.9 | 75.4 | 84.1 |
| 31-40 | mid gear | sh_charm (level 31) | vibro_hone x6 > ramp_plasma > vibro_hone > ramp_plasma > vibro_hone > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > hydro_press > rebond > vibro_hone > cryo_quench > rebond > hydro_press > cryo_quench > vibro_hone x3 | 99.6 | 79.3 | 99.7 |
| 31-40 | mid gear | prm_bul_cloak (level 33) | micro_hone > vibro_hone > ramp_plasma > micro_hone > vibro_hone > ramp_plasma > micro_hone > vibro_hone > ramp_plasma > hydro_press x2 > ramp_plasma > hydro_press > cryo_quench > vibro_hone > hydro_press x2 > vibro_hone x2 | 91.9 | 97.5 | 99.5 |
| 31-40 | mid gear | biolab_belt (level 34) | hydro_press x4 > ramp_plasma > vibro_hone x2 > cryo_quench > hydro_press > cryo_quench > micro_hone > vibro_hone | 100.0 | 90.0 | 99.9 |
| 31-40 | mid gear | proto_greatsword (level 38) | vibro_hone x9 > ramp_plasma > vibro_hone > hydro_press > vibro_hone > hydro_press > vibro_hone > ramp_plasma > hydro_press | 100.0 | 48.4 | 99.8 |
| 31-40 | mid gear | majestic_ring001 (level 40) | hydro_press x3 > temper_hold > ramp_plasma > micro_hone > vibro_hone > ramp_plasma > vibro_hone > micro_hone > temper_hold > ramp_plasma > vibro_hone > cryo_quench > hydro_press > micro_hone > cryo_quench > micro_hone > vibro_hone > micro_hone > vibro_hone | 92.8 | 62.5 | 99.2 |
| 31-40 | mid gear | chef_cloak_4 (level 40) | rebond > temper_hold > vibro_hone x4 > ramp_plasma > vibro_hone > ramp_plasma > hydro_press x2 > vibro_hone > ramp_plasma > cryo_quench > vibro_hone > ramp_plasma > hydro_press x3 | 93.6 | 96.5 | 95.6 |
| 31-40 | best gear | sh_charm (level 31) | micro_hone > vibro_hone x4 > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > micro_hone > hydro_press x2 > vibro_hone > hydro_press x2 | 100.0 | 88.2 | 99.9 |
| 31-40 | best gear | prm_bul_cloak (level 33) | hydro_press x2 > vibro_hone > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone x2 > ramp_plasma > hydro_press | 99.9 | 99.8 | 99.8 |
| 31-40 | best gear | biolab_belt (level 34) | hydro_press > vibro_hone x5 > ramp_plasma > vibro_hone > ramp_plasma > cryo_quench > vibro_hone > ramp_plasma > hydro_press x2 > vibro_hone > hydro_press x2 | 100.0 | 98.6 | 100.0 |
| 31-40 | best gear | proto_greatsword (level 38) | vibro_hone x10 > hydro_press > vibro_hone > ramp_plasma > hydro_press x3 | 100.0 | 67.8 | 99.8 |
| 31-40 | best gear | majestic_ring001 (level 40) | temper_hold > vibro_hone x4 > ramp_plasma > vibro_hone > ramp_plasma > vibro_hone > ramp_plasma > hydro_press > vibro_hone > ramp_plasma x3 > hydro_press > cryo_quench > hydro_press > cryo_quench > vibro_hone | 95.2 | 81.3 | 99.8 |
| 31-40 | best gear | chef_cloak_4 (level 40) | hydro_press > vibro_hone x2 > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone x2 > temper_hold > ramp_plasma > hydro_press x2 > vibro_hone > hydro_press > rebond > cryo_quench > vibro_hone | 100.0 | 94.4 | 100.0 |
| 41-53 | no gear | asc_chn_tunic (level 42) | vibro_hone x5 > ramp_plasma > rebond > vibro_hone > ramp_plasma > hydro_press > vibro_hone > hydro_press x4 > vibro_hone > hydro_press x2 | 100.0 | 89.3 | 99.9 |
| 41-53 | no gear | sorting_harness (level 45) | temper_hold > vibro_hone x4 > temper_hold > vibro_hone > temper_hold > vibro_hone > ramp_plasma x3 > hydro_press > temper_hold > hydro_press > vibro_hone > hydro_press | 96.7 | 99.8 | 93.0 |
| 41-53 | no gear | engi_belt_5 (level 50) | hydro_press > temper_hold > hydro_press > vibro_hone x2 > ramp_plasma > hydro_press > cryo_quench > hydro_press > cryo_quench > hydro_press > temper_hold > micro_hone > ramp_plasma > cryo_quench > hydro_press > cryo_quench > vibro_hone | 96.7 | 93.8 | 89.1 |
| 41-53 | no gear | fang_gaunt (level 50) | temper_hold > vibro_hone x3 > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > temper_hold > vibro_hone > ramp_plasma > hydro_press x2 > vibro_hone > temper_hold > hydro_press > vibro_hone > hydro_press | 86.2 | 100.0 | 99.9 |
| 41-53 | no gear | chi_greatsword (level 52) | hydro_press x2 > rebond > hydro_press > ramp_plasma > vibro_hone x2 > ramp_plasma > cryo_quench > temper_hold > hydro_press > cryo_quench > micro_hone > cryo_quench > micro_hone x4 > vibro_hone > micro_hone x2 > vibro_hone | 93.8 | 51.3 | 99.1 |
| 41-53 | no gear | imm_ring (level 52) | hydro_press > ramp_plasma > hydro_press x2 > micro_hone > ramp_plasma > cryo_quench > temper_hold > hydro_press > micro_hone > cryo_quench > micro_hone x3 > vibro_hone x3 | 96.0 | 79.1 | 92.7 |
| 41-53 | mid gear | asc_chn_tunic (level 42) | vibro_hone x4 > ramp_plasma > vibro_hone > hydro_press x4 > vibro_hone > hydro_press x3 | 100.0 | 99.5 | 99.9 |
| 41-53 | mid gear | sorting_harness (level 45) | temper_hold > vibro_hone x2 > temper_hold > vibro_hone x2 > ramp_plasma > vibro_hone x2 > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > temper_hold > hydro_press x2 > temper_hold > vibro_hone > hydro_press | 100.0 | 98.4 | 100.0 |
| 41-53 | mid gear | engi_belt_5 (level 50) | temper_hold > vibro_hone x5 > ramp_plasma x2 > cryo_quench > vibro_hone > ramp_plasma > temper_hold > vibro_hone > hydro_press x4 | 91.5 | 100.0 | 99.7 |
| 41-53 | mid gear | fang_gaunt (level 50) | temper_hold > vibro_hone x2 > temper_hold > vibro_hone x2 > temper_hold > vibro_hone > ramp_plasma > hydro_press > temper_hold > vibro_hone > hydro_press > vibro_hone > ramp_plasma > hydro_press x3 | 100.0 | 100.0 | 96.6 |
| 41-53 | mid gear | chi_greatsword (level 52) | vibro_hone x6 > ramp_plasma > vibro_hone x2 > hydro_press > ramp_plasma > hydro_press > vibro_hone x3 > ramp_plasma > rebond > cryo_quench > vibro_hone > hydro_press > cryo_quench > hydro_press x2 > vibro_hone x2 | 99.2 | 79.9 | 99.9 |
| 41-53 | mid gear | imm_ring (level 52) | hydro_press > cryo_quench > hydro_press > ramp_plasma > temper_hold > vibro_hone > hydro_press > ramp_plasma > vibro_hone > cryo_quench > temper_hold > cryo_quench > hydro_press > cryo_quench > ramp_plasma > micro_hone x4 > vibro_hone x2 | 99.6 | 83.3 | 100.0 |
| 41-53 | best gear | asc_chn_tunic (level 42) | vibro_hone x5 > ramp_plasma > vibro_hone > hydro_press x5 > vibro_hone > hydro_press | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | sorting_harness (level 45) | vibro_hone x4 > ramp_plasma > vibro_hone x2 > ramp_plasma > hydro_press x3 > vibro_hone > ramp_plasma > hydro_press > vibro_hone > hydro_press | 100.0 | 99.8 | 100.0 |
| 41-53 | best gear | engi_belt_5 (level 50) | hydro_press > vibro_hone x3 > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > hydro_press x2 > vibro_hone > ramp_plasma > hydro_press x3 | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | fang_gaunt (level 50) | vibro_hone x4 > ramp_plasma > vibro_hone > ramp_plasma > hydro_press x2 > vibro_hone > hydro_press x4 | 99.8 | 100.0 | 100.0 |
| 41-53 | best gear | chi_greatsword (level 52) | hydro_press > vibro_hone x6 > ramp_plasma > vibro_hone x3 > rebond > ramp_plasma x2 > vibro_hone > hydro_press > vibro_hone x2 > temper_hold > rebond > ramp_plasma > hydro_press x2 | 100.0 | 68.7 | 99.6 |
| 41-53 | best gear | imm_ring (level 52) | temper_hold > rebond > vibro_hone x4 > ramp_plasma > vibro_hone > cryo_quench > ramp_plasma x2 > micro_hone > ramp_plasma > hydro_press x2 > cryo_quench > ramp_plasma > hydro_press > cryo_quench > vibro_hone x3 | 95.0 | 79.7 | 100.0 |

## Length

### Actions per attempt of the adaptive player at level (item craft at level)

| Band | Gear | Recipes played | Mean actions per attempt | Shortest recipe | Longest recipe | Stalled attempts | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 13.6 | 11.4 | 14.9 | 0 | 10-20 | Passed |
| 1-10 | mid gear | 6 | 13.7 | 12.1 | 16.3 | 0 | 10-20 | Passed |
| 1-10 | best gear | 6 | 12.1 | 11.1 | 14.1 | 0 | 10-20 | Passed |
| 11-20 | no gear | 6 | 12.2 | 10.5 | 14.6 | 0 | 10-20 | Passed |
| 11-20 | mid gear | 6 | 13.5 | 9.5 | 16.8 | 0 | 10-20 | Passed |
| 11-20 | best gear | 6 | 12.2 | 8.2 | 16.3 | 0 | 10-20 | Passed |
| 21-30 | no gear | 6 | 12.7 | 11.6 | 13.6 | 0 | 10-20 | Passed |
| 21-30 | mid gear | 6 | 11.2 | 10.1 | 13.0 | 0 | 10-20 | Passed |
| 21-30 | best gear | 6 | 9.7 | 7.9 | 11.3 | 0 | 10-20 | Failed |
| 31-40 | no gear | 6 | 12.1 | 11.3 | 12.9 | 0 | 10-20 | Passed |
| 31-40 | mid gear | 6 | 11.0 | 9.6 | 13.8 | 0 | 10-20 | Passed |
| 31-40 | best gear | 6 | 8.7 | 7.6 | 10.0 | 0 | 10-20 | Failed |
| 41-53 | no gear | 6 | 12.0 | 9.7 | 13.4 | 0 | 10-20 | Passed |
| 41-53 | mid gear | 6 | 11.2 | 9.2 | 12.2 | 0 | 10-20 | Passed |
| 41-53 | best gear | 6 | 9.1 | 6.4 | 10.5 | 0 | 10-20 | Failed |

## Economy parity

### Component stacks consumed per item made, failures included, over the recipes the legacy policy makes at least once in the cell (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Parity rule | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 2.78 | 2.70 | 1.03 | 94% | 98% | 80% | 1% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | mid gear | 6 | 2.68 | 2.70 | 0.99 | 99% | 98% | 98% | 11% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | best gear | 6 | 2.67 | 2.67 | 1.00 | 100% | 100% | 100% | 28% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | no gear | 6 | 2.74 | 2.66 | 1.03 | 88% | 91% | 69% | 2% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | mid gear | 6 | 2.57 | 2.55 | 1.01 | 96% | 97% | 92% | 13% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | best gear | 6 | 2.51 | 2.52 | 1.00 | 99% | 99% | 99% | 29% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | no gear | 6 | 3.34 | 3.36 | 1.00 | 99% | 99% | 96% | 8% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | mid gear | 6 | 3.33 | 3.34 | 1.00 | 100% | 99% | 100% | 16% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | best gear | 6 | 3.33 | 3.34 | 1.00 | 100% | 99% | 99% | 29% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | no gear | 6 | 3.08 | 3.05 | 1.01 | 96% | 98% | 93% | 22% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | mid gear | 6 | 3.01 | 3.01 | 1.00 | 99% | 99% | 99% | 38% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | best gear | 6 | 3.00 | 3.02 | 0.99 | 100% | 99% | 100% | 49% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | no gear | 6 | 3.07 | 3.03 | 1.01 | 97% | 98% | 95% | 44% | yes | 0% / 33% | within 10% | Passed |
| 41-53 | mid gear | 6 | 4.69 | 5.38 | 0.87 | 99% | 72% | 99% | 46% | yes | 0% / 0% | at most +10%; uses 13% less, passes | Passed |
| 41-53 | best gear | 6 | 4.67 | 4.90 | 0.95 | 100% | 89% | 100% | 62% | yes | 0% / 0% | at most +10% | Passed |

### Recipes the legacy policy never makes in the cell, left out of the stack and quality comparison above (they still count in the never-made check)

| Band | Gear | Recipe | Level | New success | New avg quality |
| --- | --- | --- | --- | --- | --- |
| 41-53 | no gear | ChiroGreatSword | 52 | 100% | 99% |
| 41-53 | no gear | ImmortalRing | 52 | 97% | 93% |

## XP rate

### Item crafts: XP per hour against the legacy loop (6.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per craft | Legacy actions per craft | Legacy XP per hour | New XP per craft | New actions per craft | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 593 | 6.0 | 59489 | 2069 | 13.6 | 68355 | 1.15 | - | Passed |
| 1-10 | no gear, 1 enhancement | 442 | 6.9 | 38624 | 1658 | 12.9 | 57678 | 1.49 | - | Passed |
| 1-10 | no gear, 2 enhancements | 305 | 6.6 | 27851 | 1133 | 12.2 | 41901 | 1.50 | - | Passed |
| 1-10 | no gear, 3 enhancements | 209 | 6.9 | 18130 | 630 | 12.0 | 23621 | 1.30 | - | Passed |
| 1-10 | mid gear | 656 | 8.8 | 44601 | 2331 | 13.7 | 76465 | 1.71 | - | Passed |
| 1-10 | mid gear, 1 enhancement | 663 | 8.8 | 45415 | 2082 | 14.5 | 64473 | 1.42 | - | Passed |
| 1-10 | mid gear, 2 enhancements | 596 | 9.6 | 37414 | 1405 | 14.4 | 44007 | 1.18 | - | Passed |
| 1-10 | mid gear, 3 enhancements | 549 | 10.2 | 32305 | 1073 | 13.8 | 34946 | 1.08 | - | Passed |
| 1-10 | best gear | 770 | 10.3 | 44808 | 2363 | 12.1 | 87869 | 1.96 | - | Passed |
| 1-10 | best gear, 1 enhancement | 740 | 10.9 | 40867 | 2302 | 13.4 | 77480 | 1.90 | - | Passed |
| 1-10 | best gear, 2 enhancements | 712 | 11.0 | 38779 | 2059 | 14.6 | 63578 | 1.64 | - | Passed |
| 1-10 | best gear, 3 enhancements | 702 | 11.2 | 37692 | 1646 | 15.2 | 48622 | 1.29 | - | Passed |
| 11-20 | no gear | 566 | 7.9 | 43163 | 1144 | 12.2 | 42276 | 0.98 | 1.02x | Failed |
| 11-20 | no gear, 1 enhancement | 409 | 7.9 | 30965 | 993 | 12.5 | 35625 | 1.15 | - | Passed |
| 11-20 | no gear, 2 enhancements | 266 | 7.2 | 22260 | 874 | 12.3 | 32006 | 1.44 | - | Passed |
| 11-20 | no gear, 3 enhancements | 262 | 7.7 | 20334 | 598 | 12.6 | 21405 | 1.05 | - | Passed |
| 11-20 | mid gear | 664 | 11.1 | 35793 | 1361 | 13.4 | 45529 | 1.27 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 658 | 11.8 | 33330 | 1221 | 15.3 | 35865 | 1.08 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 622 | 12.4 | 30075 | 1129 | 16.2 | 31350 | 1.04 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 597 | 13.5 | 26634 | 1049 | 16.6 | 28437 | 1.07 | - | Passed |
| 11-20 | best gear | 766 | 14.4 | 31957 | 1431 | 12.2 | 52718 | 1.65 | - | Passed |
| 11-20 | best gear, 1 enhancement | 759 | 15.2 | 30033 | 1397 | 13.5 | 46694 | 1.55 | - | Passed |
| 11-20 | best gear, 2 enhancements | 744 | 16.0 | 27962 | 1381 | 15.4 | 40405 | 1.44 | - | Passed |
| 11-20 | best gear, 3 enhancements | 753 | 16.5 | 27328 | 1283 | 16.9 | 34205 | 1.25 | - | Passed |
| 21-30 | no gear | 639 | 10.2 | 37566 | 1716 | 12.7 | 61000 | 1.62 | - | Passed |
| 21-30 | no gear, 1 enhancement | 618 | 11.5 | 32333 | 1631 | 13.6 | 54154 | 1.67 | - | Passed |
| 21-30 | no gear, 2 enhancements | 592 | 12.4 | 28664 | 1370 | 14.4 | 42941 | 1.50 | - | Passed |
| 21-30 | no gear, 3 enhancements | 575 | 13.5 | 25564 | 843 | 14.6 | 26031 | 1.02 | - | Passed |
| 21-30 | mid gear | 691 | 16.5 | 25152 | 1750 | 11.2 | 70013 | 2.78 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 679 | 16.8 | 24257 | 1703 | 13.1 | 58334 | 2.40 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 665 | 17.4 | 22996 | 1704 | 14.8 | 51743 | 2.25 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 647 | 17.6 | 22047 | 1420 | 16.0 | 39834 | 1.81 | - | Passed |
| 21-30 | best gear | 770 | 21.3 | 21631 | 1745 | 9.7 | 81334 | 3.76 | - | Passed |
| 21-30 | best gear, 1 enhancement | 754 | 21.6 | 20987 | 1751 | 11.6 | 68070 | 3.24 | - | Passed |
| 21-30 | best gear, 2 enhancements | 752 | 22.0 | 20504 | 1702 | 13.5 | 56583 | 2.76 | - | Passed |
| 21-30 | best gear, 3 enhancements | 744 | 21.9 | 20368 | 1642 | 14.9 | 49447 | 2.43 | - | Passed |
| 31-40 | no gear | 722 | 15.3 | 28279 | 2323 | 12.1 | 86426 | 3.06 | - | Passed |
| 31-40 | no gear, 1 enhancement | 648 | 15.5 | 25053 | 1975 | 13.8 | 64544 | 2.58 | - | Passed |
| 31-40 | no gear, 2 enhancements | 632 | 15.2 | 25015 | 1517 | 14.5 | 47101 | 1.88 | - | Passed |
| 31-40 | no gear, 3 enhancements | 609 | 16.1 | 22695 | 1015 | 14.8 | 30957 | 1.36 | - | Passed |
| 31-40 | mid gear | 827 | 20.5 | 24162 | 2441 | 11.0 | 99852 | 4.13 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 788 | 21.1 | 22421 | 2313 | 13.1 | 79393 | 3.54 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 756 | 20.5 | 22069 | 1834 | 14.9 | 55277 | 2.50 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 716 | 20.2 | 21265 | 1376 | 15.6 | 39650 | 1.86 | - | Passed |
| 31-40 | best gear | 889 | 25.4 | 21025 | 2459 | 8.7 | 127823 | 6.08 | - | Passed |
| 31-40 | best gear, 1 enhancement | 895 | 26.0 | 20640 | 2404 | 11.4 | 94882 | 4.60 | - | Passed |
| 31-40 | best gear, 2 enhancements | 860 | 26.0 | 19811 | 2173 | 14.0 | 69904 | 3.53 | - | Passed |
| 31-40 | best gear, 3 enhancements | 836 | 24.7 | 20302 | 1969 | 15.5 | 57233 | 2.82 | - | Passed |
| 41-53 | no gear | 608 | 15.9 | 22911 | 1370 | 12.0 | 51453 | 2.25 | - | Passed |
| 41-53 | no gear, 1 enhancement | 543 | 14.4 | 22557 | 1283 | 13.1 | 44068 | 1.95 | - | Passed |
| 41-53 | no gear, 2 enhancements | 487 | 14.3 | 20434 | 1062 | 14.1 | 33961 | 1.66 | - | Passed |
| 41-53 | no gear, 3 enhancements | 455 | 14.7 | 18538 | 774 | 14.7 | 23718 | 1.28 | - | Passed |
| 41-53 | mid gear | 746 | 23.0 | 19424 | 1410 | 11.2 | 56422 | 2.90 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 695 | 21.5 | 19414 | 1377 | 12.4 | 49935 | 2.57 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 620 | 18.8 | 19811 | 1313 | 13.8 | 42832 | 2.16 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 586 | 17.4 | 20185 | 1064 | 15.0 | 32013 | 1.59 | - | Passed |
| 41-53 | best gear | 951 | 27.5 | 20773 | 1417 | 9.1 | 70011 | 3.37 | - | Passed |
| 41-53 | best gear, 1 enhancement | 890 | 27.1 | 19687 | 1410 | 11.3 | 56065 | 2.85 | - | Passed |
| 41-53 | best gear, 2 enhancements | 769 | 23.8 | 19375 | 1395 | 12.5 | 50304 | 2.60 | - | Passed |
| 41-53 | best gear, 3 enhancements | 711 | 21.4 | 19960 | 1337 | 14.0 | 42938 | 2.15 | - | Passed |

## Enhancement parity

### Rank 50, best gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 85%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 6 | 60-70 | 33% | 100% | 96% | 0.96 | 100% | 100.0% | 1.00 (+/- 0.00) | 1.05 | >= 1.00 | Passed |
| 41-45 | 2 | 6 | 60-70 | 65% | 100% | 89% | 1.78 | 99% | 100.0% | 1.99 (+/- 0.01) | 1.12 | >= 1.00 | Passed |
| 41-45 | 3 | 6 | 60-70 | 98% | 100% | 83% | 2.47 | 97% | 99.8% | 2.89 (+/- 0.04) | 1.17 | >= 1.00 | Passed |
| 46-50 | 1 | 6 | 60 | 32% | 100% | 83% | 0.83 | 99% | 100.0% | 0.99 (+/- 0.01) | 1.20 | >= 1.00 | Passed |
| 46-50 | 2 | 6 | 60 | 64% | 97% | 72% | 1.38 | 99% | 99.5% | 1.97 (+/- 0.02) | 1.43 | >= 1.00 | Passed |
| 46-50 | 3 | 6 | 60 | 96% | 100% | 60% | 1.79 | 93% | 98.5% | 2.71 (+/- 0.06) | 1.51 | >= 1.00 | Passed |
| 51-53 | 1 | 6 | 60-70 | 8% | 49% | 7% | 0.03 | 99% | 99.8% | 0.99 (+/- 0.01) | 29.97 | >= 1.00 | Passed |
| 51-53 | 2 | 6 | 60-70 | 17% | 22% | 4% | 0.02 | 98% | 99.9% | 1.96 (+/- 0.02) | 120.55 | >= 1.00 | Passed |
| 51-53 | 3 | 6 | 60-70 | 25% | 9% | 4% | 0.01 | 98% | 99.9% | 2.93 (+/- 0.03) | 325.93 | >= 1.00 | Passed |

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
| 41-45 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 41-45 | 2 | 99% | 100% | 100% | 100% | 100% | 100% | 97% |
| 41-45 | 3 | 97% | 100% | 100% | 100% | 100% | 100% | 95% |
| 46-50 | 1 | 99% | 100% | 100% | 100% | 100% | 100% | 97% |
| 46-50 | 2 | 99% | 100% | 100% | 100% | 100% | 100% | 96% |
| 46-50 | 3 | 93% | 100% | 100% | 100% | 100% | 100% | 84% |
| 51-53 | 1 | 99% | 99% | 100% | 100% | 100% | 100% | 86% |
| 51-53 | 2 | 98% | 100% | 100% | 100% | 100% | 100% | 92% |
| 51-53 | 3 | 98% | 100% | 100% | 100% | 100% | 100% | 92% |

### Representative enhancements: for each recipe kind the real item at the best level the recipe accepts (the old window refuses one more than 5 levels above the recipe) whose penalty is the lower median of that level

| Recipe kind | Enhancement | Level | Progress penalty | Items of that kind and level (min / median / max penalty) | Recipes |
| --- | --- | --- | --- | --- | --- |
| Weapon | Improved Agrinium V (`imp_agrinium_5`) | 45 (tier V) | 70 | 33 items: 50 / 70.0 / 80 | 3 |
| Armor | Armor Enhancement - Force Attack V (`aen_frcatk5`) | 45 (tier V) | 60 | 50 items: 50 / 60.0 / 150 | 15 |

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
