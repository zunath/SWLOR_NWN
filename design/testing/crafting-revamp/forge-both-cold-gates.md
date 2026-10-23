# Crafting revamp: balance gates of the Plasma Forge (both lines, Cold Working) minigame

- Command: `SWLOR.CraftSim gates --scenario forge-both-cold --gates rotation,length,materials,xp,enhancements --runs 30 --seed 1 --recipes-per-band 6 --out design/testing/crafting-revamp/forge-both-cold-gates.md`
- Seed 1; 30 runs per case.

## Inputs

- Scenario: Plasma Forge (both lines, Cold Working) (`forge-both-cold`), Smithery recipes, minigame Forge.
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
| No fixed rotation | Failed | One fixed rotation comes within 10 points of adaptive play in 5 case(s): 1-10 / best gear; 11-20 / best gear; 31-40 / best gear; 41-53 / mid gear; 41-53 / best gear (smallest margin 3.2 in 11-20 / best gear). The best rotation for each recipe alone, a reported metric, is -3.9 to 1.8 points behind adaptive play. |
| Length | Failed | Outside 10-20 actions (item craft at level): 21-30 / mid gear (9.6); 21-30 / best gear (8.1); 31-40 / no gear (9.5); 31-40 / mid gear (8.1); 31-40 / best gear (6.9); 41-53 / no gear (8.7); 41-53 / mid gear (7.7); 41-53 / best gear (6.5). |
| Economy parity | Passed | Component stacks per item are within 10% of the legacy baseline where its success rate is at least 90%, and at most 10% above it where it is lower, at no less quality, in every band and gear tier. 1 case(s) use more than 10% less material than today, which passes. |
| XP rate | Passed | Item crafts: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.01x in 11-20 / no gear). |
| Enhancement parity | Passed | At rank 50 in best gear, the thresholds in force (50% / 85% / 85%) transfer at least 1.00 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 1.05 times today's). Thresholds that meet parity in every band for this quality: 100% / 100% / 100%. |

## No fixed rotation

### Average quality: adaptive play against the best single fixed rotation of each band and gear tier (quality points; a broken piece counts 0)

| Band | Gear | Recipes played | Adaptive avg quality | Best single rotation avg quality | Margin | Required | Best rotation per recipe avg quality (reported) | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 84.9 | 62.7 | 22.2 | >= 10 | 84.9 | Passed |
| 1-10 | mid gear | 6 | 97.8 | 79.3 | 18.5 | >= 10 | 97.5 | Passed |
| 1-10 | best gear | 6 | 99.8 | 90.9 | 8.9 | >= 10 | 100.0 | Failed |
| 11-20 | no gear | 6 | 77.3 | 65.4 | 11.9 | >= 10 | 80.8 | Passed |
| 11-20 | mid gear | 6 | 95.3 | 79.8 | 15.4 | >= 10 | 99.2 | Passed |
| 11-20 | best gear | 6 | 99.0 | 95.8 | 3.2 | >= 10 | 100.0 | Failed |
| 21-30 | no gear | 6 | 91.7 | 60.7 | 31.0 | >= 10 | 94.8 | Passed |
| 21-30 | mid gear | 6 | 99.4 | 68.2 | 31.2 | >= 10 | 98.5 | Passed |
| 21-30 | best gear | 6 | 99.2 | 75.1 | 24.1 | >= 10 | 100.0 | Passed |
| 31-40 | no gear | 6 | 99.7 | 73.0 | 26.7 | >= 10 | 97.9 | Passed |
| 31-40 | mid gear | 6 | 99.8 | 86.0 | 13.9 | >= 10 | 99.9 | Passed |
| 31-40 | best gear | 6 | 100.0 | 94.5 | 5.4 | >= 10 | 100.0 | Failed |
| 41-53 | no gear | 6 | 98.8 | 85.1 | 13.8 | >= 10 | 100.0 | Passed |
| 41-53 | mid gear | 6 | 100.0 | 91.6 | 8.4 | >= 10 | 99.7 | Failed |
| 41-53 | best gear | 6 | 100.0 | 96.2 | 3.8 | >= 10 | 100.0 | Failed |

### The single rotation found for each band and gear tier, searched across the band's sampled recipes (average quality in points)

| Band | Gear | Best single rotation for the band | Training | Selection | Hold-out | Adaptive on these recipes |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | hydro_press > vibro_hone > hydro_press > vibro_hone x5 > ramp_plasma > hydro_press x3 | 65.6 | 66.5 | 62.7 | 84.9 |
| 1-10 | mid gear | hydro_press > vibro_hone x3 > hydro_press > vibro_hone x3 > rebond > hydro_press > ramp_plasma > vibro_hone > hydro_press > rebond x2 > hydro_press > ramp_plasma > hydro_press | 79.2 | 78.8 | 79.3 | 97.8 |
| 1-10 | best gear | hydro_press > vibro_hone x6 > cryo_quench > vibro_hone > hydro_press > ramp_plasma > hydro_press x3 | 90.8 | 90.9 | 90.9 | 99.8 |
| 11-20 | no gear | hydro_press x2 > rebond > vibro_hone x5 > ramp_plasma > vibro_hone > hydro_press > vibro_hone > cryo_quench > hydro_press > cryo_quench > hydro_press x3 > vibro_hone | 66.2 | 66.1 | 65.4 | 77.3 |
| 11-20 | mid gear | hydro_press > vibro_hone x6 > ramp_plasma > hydro_press > micro_hone > vibro_hone > ramp_plasma > cryo_quench > ramp_plasma > rebond > micro_hone > hydro_press > micro_hone > hydro_press > overdrive_press > rebond > hydro_press > vibro_hone > hydro_press x2 > vibro_hone | 79.9 | 79.6 | 79.8 | 95.3 |
| 11-20 | best gear | vibro_hone x6 > ramp_plasma > micro_hone > hydro_press > micro_hone > ramp_plasma > micro_hone > ramp_plasma > micro_hone > overdrive_press > rebond x2 > hydro_press > vibro_hone x2 > rebond > vibro_hone x4 > ramp_plasma > hydro_press x2 | 96.5 | 95.9 | 95.8 | 99.0 |
| 21-30 | no gear | vibro_hone x4 > ramp_plasma > hydro_press > cryo_quench > micro_hone > vibro_hone > ramp_plasma x2 > rebond > vibro_hone > hydro_press > overdrive_press > cryo_quench > vibro_hone > hydro_press x2 > vibro_hone > cryo_quench > hydro_press > cryo_quench > vibro_hone x2 | 60.9 | 60.6 | 60.7 | 91.7 |
| 21-30 | mid gear | rebond > vibro_hone x2 > micro_hone > ramp_plasma x2 > micro_hone x2 > hydro_press > micro_hone x2 > ramp_plasma > cryo_quench > micro_hone > rebond x2 > hydro_press > cryo_quench > hydro_press > cryo_quench > ramp_plasma > hydro_press x2 > vibro_hone x2 | 67.3 | 67.1 | 68.2 | 99.4 |
| 21-30 | best gear | rebond > ramp_plasma > cryo_quench > micro_hone x5 > ramp_plasma > hydro_press > micro_hone x2 > vibro_hone > micro_hone > cryo_quench > ramp_plasma > cryo_quench > vibro_hone > ramp_plasma > overdrive_press > hydro_press x2 | 76.5 | 75.3 | 75.1 | 99.2 |
| 31-40 | no gear | overdrive_press > hydro_press > cryo_quench > hydro_press > micro_hone > cryo_quench > hydro_press > cryo_quench > ramp_plasma > cryo_quench > hydro_press > cryo_quench > micro_hone > surge_plasma > rebond > micro_hone > cryo_quench > micro_hone x3 > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press | 77.6 | 77.2 | 73.0 | 99.7 |
| 31-40 | mid gear | overdrive_press > temper_hold > vibro_hone x2 > cryo_quench > ramp_plasma > micro_hone > cryo_quench > micro_hone x2 > surge_plasma > ramp_plasma > micro_hone x2 > hydro_press > cryo_quench > hydro_press > cryo_quench > overdrive_press > ramp_plasma > temper_hold > rebond > vibro_hone x2 > hydro_press > vibro_hone > micro_hone > vibro_hone | 88.1 | 85.8 | 86.0 | 99.8 |
| 31-40 | best gear | overdrive_press > micro_hone > temper_hold > micro_hone > vibro_hone > ramp_plasma > temper_hold > micro_hone x2 > ramp_plasma > micro_hone > temper_hold > micro_hone x2 > hydro_press > cryo_quench > vibro_hone > ramp_plasma > rebond > overdrive_press > cryo_quench > micro_hone x2 > hydro_press > micro_hone > hydro_press > micro_hone > cryo_quench > vibro_hone > hydro_press x2 > vibro_hone x2 | 94.7 | 94.5 | 94.5 | 100.0 |
| 41-53 | no gear | temper_hold > hydro_press > vibro_hone > hydro_press > vibro_hone > temper_hold > micro_hone > cryo_quench > ramp_plasma > hydro_press > cryo_quench > temper_hold > hydro_press > micro_hone > cryo_quench > rebond > temper_hold > vibro_hone x2 > hydro_press x3 > temper_hold > vibro_hone > hydro_press > temper_hold > vibro_hone > hydro_press > temper_hold > vibro_hone > hydro_press > temper_hold > vibro_hone > hydro_press > temper_hold > vibro_hone > hydro_press > temper_hold > vibro_hone > hydro_press | 81.7 | 83.1 | 85.1 | 98.8 |
| 41-53 | mid gear | vibro_hone x2 > micro_hone > vibro_hone > surge_plasma > micro_hone x2 > cryo_quench > micro_hone > ramp_plasma > micro_hone > rebond > ramp_plasma > micro_hone > ramp_plasma > red_line_press > hydro_press > rebond > ramp_plasma > hydro_press > micro_hone > hydro_press x2 | 91.6 | 92.0 | 91.6 | 100.0 |
| 41-53 | best gear | micro_hone > vibro_hone x3 > surge_plasma > micro_hone x3 > cryo_quench > red_line_press > vibro_hone > ramp_plasma > cryo_quench > ramp_plasma > cryo_quench > rebond > micro_hone > temper_hold > vibro_hone > hydro_press x2 | 93.1 | 94.7 | 96.2 | 100.0 |

### Reported metric, not gated: the best fixed rotation for each recipe alone, and what the band's single rotation scores on the same recipe (average quality in points)

| Band | Gear | Recipe | Best rotation for this recipe alone (reported) | Its hold-out | The single rotation on this recipe | Adaptive on this recipe |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | bm_bracer (level 3) | vibro_hone > hydro_press > vibro_hone x2 > hydro_press > vibro_hone > hydro_press x2 > vibro_hone > hydro_press > vibro_hone > hydro_press | 49.9 | 27.2 | 58.5 |
| 1-10 | no gear | b_twinpistol (level 6) | hydro_press > vibro_hone x12 > ramp_plasma > hydro_press x2 | 88.6 | 43.4 | 85.2 |
| 1-10 | no gear | os_hidevest (level 8) | hydro_press x2 > vibro_hone > hydro_press > vibro_hone > hydro_press x2 > vibro_hone x4 > ramp_plasma > hydro_press x3 | 95.3 | 95.1 | 96.1 |
| 1-10 | no gear | sp_crestvis (level 8) | rebond > hydro_press > vibro_hone x4 > hydro_press > vibro_hone x3 > ramp_plasma > hydro_press | 92.1 | 78.5 | 84.2 |
| 1-10 | no gear | advent_bracer (level 10) | hydro_press > vibro_hone x2 > hydro_press x2 > vibro_hone x5 > ramp_plasma x2 > hydro_press x3 | 95.2 | 79.6 | 97.8 |
| 1-10 | no gear | chef_ring_1 (level 10) | vibro_hone x8 > hydro_press > vibro_hone > ramp_plasma > hydro_press > vibro_hone x2 > hydro_press | 88.2 | 52.5 | 87.9 |
| 1-10 | mid gear | bm_bracer (level 3) | rebond > vibro_hone > hydro_press > vibro_hone > hydro_press > vibro_hone > hydro_press > vibro_hone x2 > rebond > vibro_hone > hydro_press | 85.0 | 60.3 | 91.8 |
| 1-10 | mid gear | b_twinpistol (level 6) | hydro_press x2 > vibro_hone x13 > hydro_press > vibro_hone > hydro_press | 99.9 | 48.6 | 95.7 |
| 1-10 | mid gear | os_hidevest (level 8) | hydro_press > vibro_hone x6 > ramp_plasma > hydro_press x3 > vibro_hone > hydro_press x2 | 100.0 | 100.0 | 99.9 |
| 1-10 | mid gear | sp_crestvis (level 8) | hydro_press > vibro_hone x5 > hydro_press > vibro_hone x2 > ramp_plasma > hydro_press > vibro_hone > hydro_press > ramp_plasma x2 > hydro_press | 100.0 | 97.2 | 99.9 |
| 1-10 | mid gear | advent_bracer (level 10) | vibro_hone > hydro_press > vibro_hone x5 > hydro_press > vibro_hone > rebond > hydro_press > vibro_hone > hydro_press x4 | 100.0 | 99.5 | 99.8 |
| 1-10 | mid gear | chef_ring_1 (level 10) | hydro_press > vibro_hone x9 > ramp_plasma > vibro_hone > hydro_press > vibro_hone > hydro_press > rebond > ramp_plasma > vibro_hone > hydro_press | 100.0 | 70.2 | 99.9 |
| 1-10 | best gear | bm_bracer (level 3) | rebond > hydro_press > vibro_hone > hydro_press > vibro_hone x5 > hydro_press > vibro_hone > ramp_plasma > hydro_press x2 | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | b_twinpistol (level 6) | vibro_hone > hydro_press > vibro_hone x11 > hydro_press > vibro_hone > hydro_press | 100.0 | 65.5 | 100.0 |
| 1-10 | best gear | os_hidevest (level 8) | vibro_hone x4 > hydro_press > vibro_hone x2 > hydro_press x5 | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | sp_crestvis (level 8) | hydro_press > vibro_hone x7 > hydro_press x4 > vibro_hone > hydro_press | 99.8 | 99.8 | 99.7 |
| 1-10 | best gear | advent_bracer (level 10) | hydro_press > vibro_hone x6 > hydro_press x2 > vibro_hone > hydro_press x3 | 100.0 | 100.0 | 99.7 |
| 1-10 | best gear | chef_ring_1 (level 10) | rebond > vibro_hone x9 > ramp_plasma > hydro_press > vibro_hone > hydro_press > vibro_hone > hydro_press | 100.0 | 80.1 | 99.5 |
| 11-20 | no gear | val_necklace (level 12) | hydro_press x2 > vibro_hone x6 > ramp_plasma > vibro_hone > hydro_press > vibro_hone > hydro_press x2 | 70.5 | 58.3 | 69.2 |
| 11-20 | no gear | viv_cap (level 14) | hydro_press x2 > vibro_hone > hydro_press > vibro_hone x4 > rebond > vibro_hone x2 > hydro_press > ramp_plasma > hydro_press x2 | 90.0 | 83.8 | 91.9 |
| 11-20 | no gear | vet_chn_ring (level 16) | vibro_hone x7 > rebond > vibro_hone x2 > hydro_press > vibro_hone x2 > hydro_press x2 > vibro_hone x3 > hydro_press | 100.0 | 63.7 | 80.9 |
| 11-20 | no gear | sith_katar (level 18) | hydro_press x2 > vibro_hone x3 > ramp_plasma > vibro_hone > ramp_plasma > vibro_hone > hydro_press x3 > vibro_hone > micro_hone > hydro_press > vibro_hone x2 > micro_hone > hydro_press | 45.0 | 0.0 | 22.3 |
| 11-20 | no gear | frontier_ring (level 20) | vibro_hone > rebond > micro_hone x2 > hydro_press > vibro_hone x7 > hydro_press > vibro_hone > ramp_plasma > vibro_hone > hydro_press > ramp_plasma > hydro_press | 85.3 | 94.1 | 99.6 |
| 11-20 | no gear | chef_ring_2 (level 20) | hydro_press x2 > micro_hone > vibro_hone x7 > hydro_press > cryo_quench > vibro_hone > hydro_press x2 > vibro_hone x2 > hydro_press | 94.2 | 92.4 | 99.7 |
| 11-20 | mid gear | val_necklace (level 12) | hydro_press > vibro_hone x6 > ramp_plasma > vibro_hone x3 > ramp_plasma > hydro_press > vibro_hone x2 > ramp_plasma > rebond > vibro_hone > hydro_press x2 | 99.9 | 68.8 | 99.3 |
| 11-20 | mid gear | viv_cap (level 14) | hydro_press > vibro_hone x6 > hydro_press x2 > vibro_hone > ramp_plasma > hydro_press > vibro_hone > rebond > hydro_press > vibro_hone > hydro_press | 100.0 | 96.6 | 99.8 |
| 11-20 | mid gear | vet_chn_ring (level 16) | micro_hone > vibro_hone x10 > hydro_press > vibro_hone > hydro_press > vibro_hone > rebond > overdrive_press > hydro_press | 100.0 | 74.1 | 98.4 |
| 11-20 | mid gear | sith_katar (level 18) | hydro_press > vibro_hone x5 > ramp_plasma > vibro_hone x2 > ramp_plasma > cryo_quench > vibro_hone > cryo_quench > vibro_hone > hydro_press > ramp_plasma > vibro_hone > hydro_press > overdrive_press | 95.3 | 79.8 | 74.5 |
| 11-20 | mid gear | frontier_ring (level 20) | micro_hone > vibro_hone x9 > overdrive_press > vibro_hone x2 > ramp_plasma > rebond > vibro_hone > hydro_press x2 | 100.0 | 81.5 | 99.9 |
| 11-20 | mid gear | chef_ring_2 (level 20) | micro_hone x2 > vibro_hone x8 > hydro_press x2 > vibro_hone x2 > ramp_plasma > rebond > hydro_press > vibro_hone > hydro_press x2 | 100.0 | 78.1 | 99.6 |
| 11-20 | best gear | val_necklace (level 12) | vibro_hone x6 > ramp_plasma > vibro_hone x4 > hydro_press > ramp_plasma > hydro_press > ramp_plasma > rebond > vibro_hone > hydro_press | 100.0 | 100.0 | 99.7 |
| 11-20 | best gear | viv_cap (level 14) | vibro_hone > hydro_press > vibro_hone x6 > hydro_press x3 > vibro_hone > hydro_press x2 | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | vet_chn_ring (level 16) | micro_hone > vibro_hone x9 > hydro_press > vibro_hone > hydro_press > rebond > vibro_hone > overdrive_press | 100.0 | 92.8 | 99.7 |
| 11-20 | best gear | sith_katar (level 18) | vibro_hone x6 > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone > overdrive_press > vibro_hone > ramp_plasma > vibro_hone > overdrive_press | 100.0 | 96.1 | 95.1 |
| 11-20 | best gear | frontier_ring (level 20) | micro_hone x3 > vibro_hone x7 > overdrive_press > vibro_hone > hydro_press > rebond > vibro_hone > hydro_press | 100.0 | 92.7 | 99.8 |
| 11-20 | best gear | chef_ring_2 (level 20) | micro_hone x3 > vibro_hone x6 > overdrive_press > vibro_hone > ramp_plasma > vibro_hone > overdrive_press | 100.0 | 93.4 | 99.9 |
| 21-30 | no gear | vet_twinelec (level 22) | hydro_press > micro_hone > vibro_hone x2 > ramp_plasma > micro_hone > vibro_hone x5 > hydro_press > cryo_quench > vibro_hone x3 > hydro_press x2 | 88.8 | 74.0 | 60.3 |
| 21-30 | no gear | vet_trnsabstaff (level 24) | micro_hone > vibro_hone x10 > hydro_press > vibro_hone x2 > hydro_press > overdrive_press | 93.3 | 37.4 | 99.3 |
| 21-30 | no gear | mb_guard (level 26) | hydro_press > vibro_hone x4 > ramp_plasma > micro_hone > rebond > vibro_hone x2 > overdrive_press > hydro_press x2 > vibro_hone > hydro_press x4 | 100.0 | 70.8 | 99.9 |
| 21-30 | no gear | cnt_neck (level 27) | ramp_plasma > cryo_quench > micro_hone x2 > overdrive_press > vibro_hone > hydro_press > vibro_hone x2 > ramp_plasma > vibro_hone > cryo_quench > hydro_press > overdrive_press > cryo_quench > hydro_press x3 > vibro_hone x3 | 94.9 | 60.1 | 93.4 |
| 21-30 | no gear | tg_mantle (level 29) | vibro_hone x4 > ramp_plasma > vibro_hone > overdrive_press > vibro_hone x3 > hydro_press x2 > vibro_hone > rebond > vibro_hone > hydro_press x3 | 100.0 | 66.5 | 99.1 |
| 21-30 | no gear | chef_necklace_3 (level 30) | micro_hone > vibro_hone x3 > ramp_plasma > overdrive_press > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > hydro_press > cryo_quench > hydro_press > cryo_quench > vibro_hone x2 | 92.1 | 55.2 | 98.0 |
| 21-30 | mid gear | vet_twinelec (level 22) | vibro_hone x4 > ramp_plasma > micro_hone > hydro_press > micro_hone > vibro_hone x8 > overdrive_press > hydro_press | 100.0 | 64.5 | 96.8 |
| 21-30 | mid gear | vet_trnsabstaff (level 24) | micro_hone > vibro_hone x10 > overdrive_press > cryo_quench > vibro_hone x2 > overdrive_press | 99.9 | 56.9 | 99.9 |
| 21-30 | mid gear | mb_guard (level 26) | vibro_hone x5 > ramp_plasma > micro_hone > overdrive_press > micro_hone > overdrive_press > vibro_hone > ramp_plasma > overdrive_press | 100.0 | 56.6 | 100.0 |
| 21-30 | mid gear | cnt_neck (level 27) | vibro_hone > ramp_plasma > overdrive_press > micro_hone > cryo_quench > micro_hone > vibro_hone > micro_hone > ramp_plasma > micro_hone > vibro_hone x4 > ramp_plasma > micro_hone > overdrive_press | 99.9 | 87.3 | 99.8 |
| 21-30 | mid gear | tg_mantle (level 29) | vibro_hone x5 > ramp_plasma > vibro_hone > overdrive_press > vibro_hone x2 > hydro_press > vibro_hone > hydro_press > overdrive_press | 100.0 | 60.3 | 100.0 |
| 21-30 | mid gear | chef_necklace_3 (level 30) | micro_hone x2 > vibro_hone > ramp_plasma > hydro_press > vibro_hone x3 > ramp_plasma > vibro_hone > cryo_quench > vibro_hone > ramp_plasma > vibro_hone > ramp_plasma > hydro_press x2 | 91.5 | 83.2 | 99.7 |
| 21-30 | best gear | vet_twinelec (level 22) | hydro_press > vibro_hone x3 > micro_hone > vibro_hone > ramp_plasma > micro_hone x4 > vibro_hone x5 > overdrive_press > hydro_press | 100.0 | 75.6 | 95.8 |
| 21-30 | best gear | vet_trnsabstaff (level 24) | overdrive_press > vibro_hone > cryo_quench > micro_hone x8 > vibro_hone x2 | 100.0 | 51.3 | 100.0 |
| 21-30 | best gear | mb_guard (level 26) | vibro_hone x5 > overdrive_press > vibro_hone > overdrive_press > hydro_press > vibro_hone > overdrive_press | 100.0 | 53.3 | 100.0 |
| 21-30 | best gear | cnt_neck (level 27) | vibro_hone > ramp_plasma > micro_hone > overdrive_press > cryo_quench > micro_hone x2 > vibro_hone > hydro_press > micro_hone > ramp_plasma > cryo_quench > vibro_hone x2 > micro_hone > ramp_plasma > hydro_press > rebond > vibro_hone x4 > hydro_press | 100.0 | 97.7 | 99.8 |
| 21-30 | best gear | tg_mantle (level 29) | vibro_hone > hydro_press > vibro_hone x6 > ramp_plasma > vibro_hone > overdrive_press > hydro_press x2 > vibro_hone > hydro_press | 100.0 | 76.6 | 100.0 |
| 21-30 | best gear | chef_necklace_3 (level 30) | micro_hone > cryo_quench > overdrive_press > micro_hone > ramp_plasma > micro_hone > vibro_hone x2 > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone x2 > overdrive_press | 100.0 | 96.2 | 99.8 |
| 31-40 | no gear | sh_charm (level 31) | micro_hone > vibro_hone x5 > ramp_plasma > overdrive_press > vibro_hone x3 > hydro_press > vibro_hone > ramp_plasma > overdrive_press | 92.4 | 76.2 | 99.5 |
| 31-40 | no gear | prm_bul_cloak (level 33) | overdrive_press > hydro_press x2 > vibro_hone > cryo_quench > vibro_hone > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > cryo_quench > vibro_hone | 99.9 | 89.6 | 100.0 |
| 31-40 | no gear | biolab_belt (level 34) | overdrive_press > hydro_press > vibro_hone > hydro_press > cryo_quench > vibro_hone > ramp_plasma > vibro_hone > cryo_quench > hydro_press > cryo_quench > vibro_hone > rebond > micro_hone > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press | 100.0 | 100.0 | 99.9 |
| 31-40 | no gear | proto_greatsword (level 38) | vibro_hone x10 > hydro_press > vibro_hone > overdrive_press > ramp_plasma > rebond > vibro_hone x3 > overdrive_press | 100.0 | 34.7 | 99.8 |
| 31-40 | no gear | majestic_ring001 (level 40) | temper_hold > vibro_hone x3 > surge_plasma > hydro_press > cryo_quench > micro_hone > hydro_press > vibro_hone > cryo_quench > vibro_hone > hydro_press > cryo_quench > rebond > hydro_press x3 > cryo_quench > hydro_press > temper_hold > cryo_quench > hydro_press > vibro_hone x2 | 95.2 | 38.3 | 99.0 |
| 31-40 | no gear | chef_cloak_4 (level 40) | overdrive_press > temper_hold > vibro_hone > ramp_plasma > vibro_hone > temper_hold > vibro_hone > overdrive_press > cryo_quench > temper_hold > hydro_press > cryo_quench > vibro_hone | 100.0 | 99.0 | 99.9 |
| 31-40 | mid gear | sh_charm (level 31) | hydro_press > micro_hone > vibro_hone x5 > ramp_plasma > vibro_hone x3 > rebond > ramp_plasma > overdrive_press > vibro_hone x2 > ramp_plasma > rebond > hydro_press x2 | 100.0 | 100.0 | 99.7 |
| 31-40 | mid gear | prm_bul_cloak (level 33) | micro_hone > vibro_hone x4 > ramp_plasma > vibro_hone > overdrive_press > vibro_hone > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > hydro_press > overdrive_press | 99.7 | 91.0 | 100.0 |
| 31-40 | mid gear | biolab_belt (level 34) | vibro_hone x5 > ramp_plasma > vibro_hone > overdrive_press > vibro_hone > hydro_press > vibro_hone > hydro_press > ramp_plasma > rebond > vibro_hone > overdrive_press > hydro_press | 99.9 | 96.8 | 99.9 |
| 31-40 | mid gear | proto_greatsword (level 38) | vibro_hone x10 > hydro_press > vibro_hone > hydro_press > ramp_plasma > rebond > vibro_hone > hydro_press | 100.0 | 59.8 | 99.8 |
| 31-40 | mid gear | majestic_ring001 (level 40) | vibro_hone x2 > surge_plasma > micro_hone x2 > overdrive_press > vibro_hone > cryo_quench > hydro_press > cryo_quench > micro_hone > vibro_hone > rebond > vibro_hone | 100.0 | 100.0 | 99.7 |
| 31-40 | mid gear | chef_cloak_4 (level 40) | vibro_hone x4 > ramp_plasma > vibro_hone > surge_plasma > overdrive_press > micro_hone > rebond > vibro_hone x2 > rebond > hydro_press > cryo_quench > hydro_press x2 > vibro_hone | 99.6 | 68.3 | 99.9 |
| 31-40 | best gear | sh_charm (level 31) | micro_hone > vibro_hone x6 > ramp_plasma > vibro_hone x2 > overdrive_press > vibro_hone > hydro_press > overdrive_press | 100.0 | 90.4 | 100.0 |
| 31-40 | best gear | prm_bul_cloak (level 33) | micro_hone > vibro_hone x3 > ramp_plasma > vibro_hone x2 > ramp_plasma > micro_hone > overdrive_press > vibro_hone > overdrive_press > ramp_plasma > hydro_press | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | biolab_belt (level 34) | vibro_hone x6 > ramp_plasma > overdrive_press > vibro_hone x2 > overdrive_press > vibro_hone > overdrive_press | 100.0 | 100.0 | 100.0 |
| 31-40 | best gear | proto_greatsword (level 38) | hydro_press > vibro_hone x11 > overdrive_press > hydro_press > vibro_hone > hydro_press | 100.0 | 76.8 | 99.9 |
| 31-40 | best gear | majestic_ring001 (level 40) | vibro_hone x2 > surge_plasma > micro_hone x3 > overdrive_press > cryo_quench > vibro_hone > ramp_plasma > vibro_hone x3 > ramp_plasma > vibro_hone x2 > overdrive_press | 100.0 | 100.0 | 99.8 |
| 31-40 | best gear | chef_cloak_4 (level 40) | vibro_hone x3 > ramp_plasma > vibro_hone x3 > ramp_plasma > vibro_hone > overdrive_press > hydro_press > vibro_hone > ramp_plasma > overdrive_press | 100.0 | 99.9 | 100.0 |
| 41-53 | no gear | asc_chn_tunic (level 42) | hydro_press > vibro_hone x5 > ramp_plasma > overdrive_press > rebond > hydro_press > vibro_hone > hydro_press > vibro_hone > hydro_press x3 > rebond > cryo_quench > vibro_hone | 100.0 | 92.7 | 100.0 |
| 41-53 | no gear | sorting_harness (level 45) | vibro_hone > overdrive_press > vibro_hone x2 > ramp_plasma > vibro_hone x2 > overdrive_press > cryo_quench > rebond > vibro_hone > hydro_press > cryo_quench > hydro_press > cryo_quench > vibro_hone x4 | 100.0 | 95.4 | 100.0 |
| 41-53 | no gear | engi_belt_5 (level 50) | vibro_hone x3 > hydro_press > surge_plasma > micro_hone x3 > red_line_press > vibro_hone x3 > ramp_plasma > vibro_hone > hydro_press x2 | 100.0 | 98.8 | 100.0 |
| 41-53 | no gear | fang_gaunt (level 50) | vibro_hone x4 > surge_plasma > micro_hone x4 > red_line_press > vibro_hone > hydro_press x3 | 100.0 | 94.6 | 100.0 |
| 41-53 | no gear | chi_greatsword (level 52) | hydro_press > vibro_hone x9 > temper_hold > vibro_hone x2 > rebond > ramp_plasma > vibro_hone x2 > rebond > hydro_press > vibro_hone > hydro_press > overdrive_press | 100.0 | 54.5 | 99.6 |
| 41-53 | no gear | imm_ring (level 52) | vibro_hone x3 > surge_plasma > micro_hone > red_line_press > cryo_quench > hydro_press > cryo_quench > micro_hone x5 > vibro_hone x3 | 100.0 | 74.4 | 93.5 |
| 41-53 | mid gear | asc_chn_tunic (level 42) | vibro_hone x4 > overdrive_press > vibro_hone > ramp_plasma > overdrive_press > vibro_hone > hydro_press x3 | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | sorting_harness (level 45) | vibro_hone x4 > ramp_plasma > vibro_hone x2 > overdrive_press > vibro_hone > hydro_press x2 > vibro_hone > ramp_plasma > rebond > vibro_hone > overdrive_press > hydro_press | 100.0 | 99.4 | 100.0 |
| 41-53 | mid gear | engi_belt_5 (level 50) | temper_hold > vibro_hone x4 > micro_hone > surge_plasma > micro_hone > red_line_press > micro_hone x2 > red_line_press > vibro_hone > overdrive_press > vibro_hone > overdrive_press | 99.9 | 98.5 | 100.0 |
| 41-53 | mid gear | fang_gaunt (level 50) | temper_hold > vibro_hone x5 > surge_plasma > micro_hone > overdrive_press > micro_hone x2 > red_line_press > vibro_hone > overdrive_press | 100.0 | 100.0 | 100.0 |
| 41-53 | mid gear | chi_greatsword (level 52) | vibro_hone x9 > hydro_press > overdrive_press > vibro_hone > ramp_plasma > vibro_hone > rebond > vibro_hone x2 > hydro_press x2 | 100.0 | 70.9 | 99.8 |
| 41-53 | mid gear | imm_ring (level 52) | temper_hold > vibro_hone x5 > ramp_plasma > surge_plasma > micro_hone x2 > cryo_quench > red_line_press > micro_hone > cryo_quench > temper_hold > hydro_press > vibro_hone x2 > overdrive_press > hydro_press | 98.3 | 80.6 | 99.9 |
| 41-53 | best gear | asc_chn_tunic (level 42) | vibro_hone > temper_hold > hydro_press > vibro_hone > temper_hold > hydro_press > vibro_hone > temper_hold > hydro_press > vibro_hone > temper_hold > hydro_press > vibro_hone > temper_hold > hydro_press > vibro_hone > temper_hold > hydro_press > vibro_hone > temper_hold > hydro_press > vibro_hone > temper_hold > hydro_press > vibro_hone > temper_hold > hydro_press > vibro_hone > temper_hold > hydro_press > vibro_hone > temper_hold > hydro_press > vibro_hone > temper_hold > hydro_press > vibro_hone > temper_hold > hydro_press > vibro_hone | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | sorting_harness (level 45) | hydro_press > temper_hold > vibro_hone x5 > ramp_plasma > vibro_hone > overdrive_press > vibro_hone > overdrive_press > hydro_press x2 | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | engi_belt_5 (level 50) | vibro_hone x4 > ramp_plasma > vibro_hone x2 > surge_plasma > micro_hone > red_line_press > micro_hone > overdrive_press | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | fang_gaunt (level 50) | hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | chi_greatsword (level 52) | vibro_hone x9 > hydro_press > vibro_hone > overdrive_press > rebond > micro_hone > ramp_plasma > vibro_hone > overdrive_press | 100.0 | 78.8 | 100.0 |
| 41-53 | best gear | imm_ring (level 52) | overdrive_press > hydro_press > rebond > ramp_plasma > cryo_quench > micro_hone x2 > vibro_hone | 100.0 | 98.6 | 100.0 |

## Length

### Actions per attempt of the adaptive player at level (item craft at level)

| Band | Gear | Recipes played | Mean actions per attempt | Shortest recipe | Longest recipe | Stalled attempts | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 13.8 | 11.4 | 15.8 | 0 | 10-20 | Passed |
| 1-10 | mid gear | 6 | 13.5 | 12.1 | 15.6 | 0 | 10-20 | Passed |
| 1-10 | best gear | 6 | 12.0 | 11.1 | 14.1 | 0 | 10-20 | Passed |
| 11-20 | no gear | 6 | 12.5 | 10.5 | 14.9 | 0 | 10-20 | Passed |
| 11-20 | mid gear | 6 | 12.9 | 9.5 | 16.6 | 0 | 10-20 | Passed |
| 11-20 | best gear | 6 | 11.6 | 8.1 | 14.4 | 0 | 10-20 | Passed |
| 21-30 | no gear | 6 | 10.2 | 8.2 | 11.9 | 0 | 10-20 | Passed |
| 21-30 | mid gear | 6 | 9.6 | 7.2 | 13.0 | 0 | 10-20 | Failed |
| 21-30 | best gear | 6 | 8.1 | 6.2 | 10.2 | 0 | 10-20 | Failed |
| 31-40 | no gear | 6 | 9.5 | 8.0 | 10.8 | 0 | 10-20 | Failed |
| 31-40 | mid gear | 6 | 8.1 | 7.4 | 9.1 | 0 | 10-20 | Failed |
| 31-40 | best gear | 6 | 6.9 | 6.0 | 7.8 | 0 | 10-20 | Failed |
| 41-53 | no gear | 6 | 8.7 | 7.6 | 9.6 | 0 | 10-20 | Failed |
| 41-53 | mid gear | 6 | 7.7 | 7.0 | 9.2 | 0 | 10-20 | Failed |
| 41-53 | best gear | 6 | 6.5 | 5.9 | 7.4 | 0 | 10-20 | Failed |

## Economy parity

### Component stacks consumed per item made, failures included, over the recipes the legacy policy makes at least once in the cell (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Parity rule | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 2.74 | 2.70 | 1.01 | 96% | 98% | 85% | 1% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | mid gear | 6 | 2.68 | 2.70 | 0.99 | 99% | 98% | 98% | 11% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | best gear | 6 | 2.67 | 2.67 | 1.00 | 100% | 100% | 100% | 28% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | no gear | 6 | 2.68 | 2.66 | 1.01 | 91% | 91% | 77% | 2% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | mid gear | 6 | 2.53 | 2.55 | 0.99 | 98% | 97% | 95% | 13% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | best gear | 6 | 2.51 | 2.52 | 1.00 | 99% | 99% | 99% | 29% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | no gear | 6 | 3.33 | 3.36 | 0.99 | 100% | 99% | 92% | 8% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | mid gear | 6 | 3.33 | 3.34 | 1.00 | 100% | 99% | 99% | 16% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | best gear | 6 | 3.33 | 3.34 | 1.00 | 100% | 99% | 99% | 29% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | no gear | 6 | 3.00 | 3.05 | 0.98 | 100% | 98% | 100% | 22% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | mid gear | 6 | 3.00 | 3.01 | 1.00 | 100% | 99% | 100% | 38% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | best gear | 6 | 3.00 | 3.02 | 0.99 | 100% | 99% | 100% | 49% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | no gear | 6 | 3.00 | 3.03 | 0.99 | 100% | 98% | 100% | 44% | yes | 0% / 33% | within 10% | Passed |
| 41-53 | mid gear | 6 | 4.67 | 5.38 | 0.87 | 100% | 72% | 100% | 46% | yes | 0% / 0% | at most +10%; uses 13% less, passes | Passed |
| 41-53 | best gear | 6 | 4.67 | 4.90 | 0.95 | 100% | 89% | 100% | 62% | yes | 0% / 0% | at most +10% | Passed |

### Recipes the legacy policy never makes in the cell, left out of the stack and quality comparison above (they still count in the never-made check)

| Band | Gear | Recipe | Level | New success | New avg quality |
| --- | --- | --- | --- | --- | --- |
| 41-53 | no gear | ChiroGreatSword | 52 | 100% | 100% |
| 41-53 | no gear | ImmortalRing | 52 | 100% | 93% |

## XP rate

### Item crafts: XP per hour against the legacy loop (6.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per craft | Legacy actions per craft | Legacy XP per hour | New XP per craft | New actions per craft | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 593 | 6.0 | 59489 | 2147 | 13.8 | 69773 | 1.17 | - | Passed |
| 1-10 | no gear, 1 enhancement | 442 | 6.9 | 38624 | 1644 | 13.1 | 56616 | 1.47 | - | Passed |
| 1-10 | no gear, 2 enhancements | 305 | 6.6 | 27851 | 1089 | 12.3 | 39978 | 1.44 | - | Passed |
| 1-10 | no gear, 3 enhancements | 209 | 6.9 | 18130 | 730 | 12.0 | 27334 | 1.51 | - | Passed |
| 1-10 | mid gear | 656 | 8.8 | 44601 | 2333 | 13.5 | 77657 | 1.74 | - | Passed |
| 1-10 | mid gear, 1 enhancement | 663 | 8.8 | 45415 | 2149 | 14.4 | 67142 | 1.48 | - | Passed |
| 1-10 | mid gear, 2 enhancements | 596 | 9.6 | 37414 | 1663 | 15.1 | 49721 | 1.33 | - | Passed |
| 1-10 | mid gear, 3 enhancements | 549 | 10.2 | 32305 | 1281 | 14.9 | 38715 | 1.20 | - | Passed |
| 1-10 | best gear | 770 | 10.3 | 44808 | 2362 | 12.0 | 88273 | 1.97 | - | Passed |
| 1-10 | best gear, 1 enhancement | 740 | 10.9 | 40867 | 2303 | 13.3 | 78180 | 1.91 | - | Passed |
| 1-10 | best gear, 2 enhancements | 712 | 11.0 | 38779 | 2060 | 14.3 | 64829 | 1.67 | - | Passed |
| 1-10 | best gear, 3 enhancements | 702 | 11.2 | 37692 | 1701 | 15.0 | 50994 | 1.35 | - | Passed |
| 11-20 | no gear | 566 | 7.9 | 43163 | 1218 | 12.5 | 43763 | 1.01 | - | Passed |
| 11-20 | no gear, 1 enhancement | 409 | 7.9 | 30965 | 1150 | 12.3 | 42010 | 1.36 | - | Passed |
| 11-20 | no gear, 2 enhancements | 266 | 7.2 | 22260 | 959 | 12.1 | 35702 | 1.60 | - | Passed |
| 11-20 | no gear, 3 enhancements | 262 | 7.7 | 20334 | 679 | 12.1 | 25192 | 1.24 | - | Passed |
| 11-20 | mid gear | 664 | 11.1 | 35793 | 1396 | 12.9 | 48748 | 1.36 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 658 | 11.8 | 33330 | 1364 | 14.2 | 43321 | 1.30 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 622 | 12.4 | 30075 | 1248 | 14.6 | 38418 | 1.28 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 597 | 13.5 | 26634 | 1080 | 14.7 | 33136 | 1.24 | - | Passed |
| 11-20 | best gear | 766 | 14.4 | 31957 | 1429 | 11.6 | 55269 | 1.73 | - | Passed |
| 11-20 | best gear, 1 enhancement | 759 | 15.2 | 30033 | 1378 | 12.6 | 49050 | 1.63 | - | Passed |
| 11-20 | best gear, 2 enhancements | 744 | 16.0 | 27962 | 1377 | 13.6 | 45616 | 1.63 | - | Passed |
| 11-20 | best gear, 3 enhancements | 753 | 16.5 | 27328 | 1349 | 15.0 | 40541 | 1.48 | - | Passed |
| 21-30 | no gear | 639 | 10.2 | 37566 | 1679 | 10.2 | 73739 | 1.96 | - | Passed |
| 21-30 | no gear, 1 enhancement | 618 | 11.5 | 32333 | 1645 | 11.8 | 62906 | 1.95 | - | Passed |
| 21-30 | no gear, 2 enhancements | 592 | 12.4 | 28664 | 1366 | 12.5 | 49263 | 1.72 | - | Passed |
| 21-30 | no gear, 3 enhancements | 575 | 13.5 | 25564 | 1069 | 12.7 | 37837 | 1.48 | - | Passed |
| 21-30 | mid gear | 691 | 16.5 | 25152 | 1746 | 9.6 | 81956 | 3.26 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 679 | 16.8 | 24257 | 1737 | 10.8 | 72117 | 2.97 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 665 | 17.4 | 22996 | 1661 | 12.2 | 61054 | 2.65 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 647 | 17.6 | 22047 | 1445 | 13.3 | 49011 | 2.22 | - | Passed |
| 21-30 | best gear | 770 | 21.3 | 21631 | 1745 | 8.1 | 96887 | 4.48 | - | Passed |
| 21-30 | best gear, 1 enhancement | 754 | 21.6 | 20987 | 1748 | 9.4 | 83684 | 3.99 | - | Passed |
| 21-30 | best gear, 2 enhancements | 752 | 22.0 | 20504 | 1744 | 10.4 | 75094 | 3.66 | - | Passed |
| 21-30 | best gear, 3 enhancements | 744 | 21.9 | 20368 | 1670 | 12.1 | 62311 | 3.06 | - | Passed |
| 31-40 | no gear | 722 | 15.3 | 28279 | 2456 | 9.5 | 116623 | 4.12 | - | Passed |
| 31-40 | no gear, 1 enhancement | 648 | 15.5 | 25053 | 2217 | 10.7 | 93363 | 3.73 | - | Passed |
| 31-40 | no gear, 2 enhancements | 632 | 15.2 | 25015 | 1798 | 11.7 | 69270 | 2.77 | - | Passed |
| 31-40 | no gear, 3 enhancements | 609 | 16.1 | 22695 | 1397 | 12.3 | 51025 | 2.25 | - | Passed |
| 31-40 | mid gear | 827 | 20.5 | 24162 | 2458 | 8.1 | 135815 | 5.62 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 788 | 21.1 | 22421 | 2459 | 9.9 | 112209 | 5.00 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 756 | 20.5 | 22069 | 2251 | 11.7 | 86591 | 3.92 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 716 | 20.2 | 21265 | 1862 | 12.9 | 65040 | 3.06 | - | Passed |
| 31-40 | best gear | 889 | 25.4 | 21025 | 2459 | 6.9 | 160395 | 7.63 | - | Passed |
| 31-40 | best gear, 1 enhancement | 895 | 26.0 | 20640 | 2459 | 8.1 | 136618 | 6.62 | - | Passed |
| 31-40 | best gear, 2 enhancements | 860 | 26.0 | 19811 | 2454 | 9.7 | 113602 | 5.73 | - | Passed |
| 31-40 | best gear, 3 enhancements | 836 | 24.7 | 20302 | 2123 | 11.5 | 83190 | 4.10 | - | Passed |
| 41-53 | no gear | 608 | 15.9 | 22911 | 1408 | 8.7 | 72587 | 3.17 | - | Passed |
| 41-53 | no gear, 1 enhancement | 543 | 14.4 | 22557 | 1284 | 9.8 | 59052 | 2.62 | - | Passed |
| 41-53 | no gear, 2 enhancements | 487 | 14.3 | 20434 | 1176 | 11.5 | 46026 | 2.25 | - | Passed |
| 41-53 | no gear, 3 enhancements | 455 | 14.7 | 18538 | 948 | 12.9 | 33098 | 1.79 | - | Passed |
| 41-53 | mid gear | 746 | 23.0 | 19424 | 1417 | 7.7 | 83353 | 4.29 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 695 | 21.5 | 19414 | 1379 | 9.2 | 67754 | 3.49 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 620 | 18.8 | 19811 | 1294 | 10.2 | 57192 | 2.89 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 586 | 17.4 | 20185 | 1147 | 11.4 | 45144 | 2.24 | - | Passed |
| 41-53 | best gear | 951 | 27.5 | 20773 | 1417 | 6.5 | 98034 | 4.72 | - | Passed |
| 41-53 | best gear, 1 enhancement | 890 | 27.1 | 19687 | 1417 | 7.6 | 83491 | 4.24 | - | Passed |
| 41-53 | best gear, 2 enhancements | 769 | 23.8 | 19375 | 1389 | 8.9 | 69942 | 3.61 | - | Passed |
| 41-53 | best gear, 3 enhancements | 711 | 21.4 | 19960 | 1330 | 10.2 | 58443 | 2.93 | - | Passed |

## Enhancement parity

### Rank 50, best gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 85%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 6 | 60-70 | 33% | 100% | 96% | 0.96 | 100% | 100.0% | 1.00 (+/- 0.00) | 1.05 | >= 1.00 | Passed |
| 41-45 | 2 | 6 | 60-70 | 65% | 100% | 89% | 1.78 | 100% | 100.0% | 2.00 (+/- 0.00) | 1.13 | >= 1.00 | Passed |
| 41-45 | 3 | 6 | 60-70 | 98% | 100% | 83% | 2.47 | 100% | 100.0% | 3.00 (+/- 0.00) | 1.21 | >= 1.00 | Passed |
| 46-50 | 1 | 6 | 60 | 32% | 100% | 83% | 0.83 | 100% | 100.0% | 1.00 (+/- 0.00) | 1.21 | >= 1.00 | Passed |
| 46-50 | 2 | 6 | 60 | 64% | 97% | 72% | 1.38 | 100% | 99.9% | 2.00 (+/- 0.00) | 1.45 | >= 1.00 | Passed |
| 46-50 | 3 | 6 | 60 | 96% | 100% | 60% | 1.79 | 99% | 99.9% | 2.98 (+/- 0.02) | 1.66 | >= 1.00 | Passed |
| 51-53 | 1 | 6 | 60-70 | 8% | 49% | 7% | 0.03 | 100% | 100.0% | 1.00 (+/- 0.00) | 30.30 | >= 1.00 | Passed |
| 51-53 | 2 | 6 | 60-70 | 17% | 22% | 4% | 0.02 | 100% | 100.0% | 2.00 (+/- 0.00) | 123.29 | >= 1.00 | Passed |
| 51-53 | 3 | 6 | 60-70 | 25% | 9% | 4% | 0.01 | 100% | 99.9% | 3.00 (+/- 0.00) | 333.33 | >= 1.00 | Passed |

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
| 41-45 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 99% |
| 41-45 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 98% |
| 46-50 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 99% |
| 46-50 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 96% |
| 46-50 | 3 | 99% | 100% | 100% | 100% | 100% | 100% | 97% |
| 51-53 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 96% |
| 51-53 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 97% |
| 51-53 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 94% |

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
- Every craft in this scenario is played by a crafter who bought both perk lines in full (every purchase their Smithery rank allows) and plays Cold Working, so no gate here is an unperked result.
