# Crafting revamp: balance gates of the Plasma Forge (Forgemaster line, Hot Forging) minigame

- Command: `SWLOR.CraftSim gates --scenario forge-forgemaster --gates rotation,length,materials,xp,enhancements --runs 30 --seed 1 --recipes-per-band 6 --out design/testing/crafting-revamp/forge-forgemaster-gates.md`
- Seed 1; 30 runs per case.

## Inputs

- Scenario: Plasma Forge (Forgemaster line, Hot Forging) (`forge-forgemaster`), Smithery recipes, minigame Forge.
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
| No fixed rotation | Passed | Adaptive play beats the best single fixed rotation by at least 10 points in every band and gear tier (smallest margin 12.5 in 1-10 / best gear). The best rotation for each recipe alone, a reported metric, is -7.3 to 4.3 points behind adaptive play. |
| Length | Failed | Outside 10-20 actions (item craft at level): 21-30 / no gear (9.9); 21-30 / mid gear (10.0); 21-30 / best gear (8.8); 31-40 / no gear (9.6); 31-40 / mid gear (9.1); 31-40 / best gear (7.9); 41-53 / no gear (8.1); 41-53 / mid gear (7.5); 41-53 / best gear (7.4). |
| Economy parity | Passed | Component stacks per item are within 10% of the legacy baseline where its success rate is at least 90%, and at most 10% above it where it is lower, at no less quality, in every band and gear tier. 1 case(s) use more than 10% less material than today, which passes. |
| XP rate | Passed | Item crafts: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.16x in 11-20 / no gear). |
| Enhancement parity | Passed | At rank 50 in best gear, the thresholds in force (50% / 85% / 85%) transfer at least 1.00 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 1.05 times today's). Thresholds that meet parity in every band for this quality: 96% / 100% / 100%. |

## No fixed rotation

### Average quality: adaptive play against the best single fixed rotation of each band and gear tier (quality points; a broken piece counts 0)

| Band | Gear | Recipes played | Adaptive avg quality | Best single rotation avg quality | Margin | Required | Best rotation per recipe avg quality (reported) | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 64.0 | 49.2 | 14.8 | >= 10 | 64.2 | Passed |
| 1-10 | mid gear | 6 | 90.4 | 69.1 | 21.3 | >= 10 | 90.5 | Passed |
| 1-10 | best gear | 6 | 98.1 | 85.6 | 12.5 | >= 10 | 99.0 | Passed |
| 11-20 | no gear | 6 | 63.4 | 46.2 | 17.2 | >= 10 | 70.6 | Passed |
| 11-20 | mid gear | 6 | 92.3 | 69.3 | 23.0 | >= 10 | 94.5 | Passed |
| 11-20 | best gear | 6 | 98.0 | 76.6 | 21.3 | >= 10 | 100.0 | Passed |
| 21-30 | no gear | 6 | 82.8 | 49.4 | 33.5 | >= 10 | 79.7 | Passed |
| 21-30 | mid gear | 6 | 93.3 | 56.3 | 37.0 | >= 10 | 96.3 | Passed |
| 21-30 | best gear | 6 | 96.4 | 62.4 | 34.0 | >= 10 | 99.7 | Passed |
| 31-40 | no gear | 6 | 92.1 | 60.5 | 31.6 | >= 10 | 87.7 | Passed |
| 31-40 | mid gear | 6 | 97.0 | 65.0 | 32.0 | >= 10 | 97.7 | Passed |
| 31-40 | best gear | 6 | 98.1 | 62.0 | 36.2 | >= 10 | 98.8 | Passed |
| 41-53 | no gear | 6 | 85.0 | 63.9 | 21.1 | >= 10 | 87.2 | Passed |
| 41-53 | mid gear | 6 | 88.9 | 73.5 | 15.3 | >= 10 | 94.1 | Passed |
| 41-53 | best gear | 6 | 94.7 | 78.1 | 16.7 | >= 10 | 98.1 | Passed |

### The single rotation found for each band and gear tier, searched across the band's sampled recipes (average quality in points)

| Band | Gear | Best single rotation for the band | Training | Selection | Hold-out | Adaptive on these recipes |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | vibro_hone > hydro_press x2 > vibro_hone x4 > hydro_press > ramp_plasma > hydro_press x3 | 47.9 | 48.5 | 49.2 | 64.0 |
| 1-10 | mid gear | vibro_hone x4 > hydro_press > vibro_hone > hydro_press > vibro_hone x2 > rebond > ramp_plasma > hydro_press x2 | 71.5 | 69.1 | 69.1 | 90.4 |
| 1-10 | best gear | hydro_press > vibro_hone x5 > cryo_quench > vibro_hone > hydro_press x4 | 85.3 | 85.2 | 85.6 | 98.1 |
| 11-20 | no gear | vibro_hone > hydro_press > vibro_hone x5 > ramp_plasma > hydro_press > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press x2 > vibro_hone | 46.9 | 46.6 | 46.2 | 63.4 |
| 11-20 | mid gear | vibro_hone x6 > ramp_plasma > hydro_press x2 > cryo_quench > overdrive_press > rebond > ramp_plasma > vibro_hone x3 > hydro_press x2 > ramp_plasma > hydro_press | 67.6 | 67.9 | 69.3 | 92.3 |
| 11-20 | best gear | vibro_hone x6 > ramp_plasma > hydro_press x2 > cryo_quench > overdrive_press > rebond > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone x2 > rebond > ramp_plasma > vibro_hone x2 > hydro_press x2 | 77.8 | 77.0 | 76.6 | 98.0 |
| 21-30 | no gear | ramp_plasma > overdrive_press > cryo_quench > vibro_hone > cryo_quench > vibro_hone > overdrive_press > vibro_hone x2 > rebond > cryo_quench > hydro_press > cryo_quench > vibro_hone x4 > hydro_press > cryo_quench > vibro_hone > hydro_press | 51.5 | 51.2 | 49.4 | 82.8 |
| 21-30 | mid gear | ramp_plasma > overdrive_press > vibro_hone x2 > cryo_quench > hydro_press > vibro_hone x3 > cryo_quench > vibro_hone x3 > rebond > ramp_plasma > vibro_hone x2 > overdrive_press | 56.8 | 57.2 | 56.3 | 93.3 |
| 21-30 | best gear | ramp_plasma > vibro_hone x3 > ramp_plasma > overdrive_press > vibro_hone > cryo_quench > rebond > cryo_quench > vibro_hone > rebond > vibro_hone x4 > hydro_press > vibro_hone > hydro_press | 61.2 | 59.1 | 62.4 | 96.4 |
| 31-40 | no gear | vibro_hone > ramp_plasma > overdrive_press > cryo_quench > vibro_hone x3 > ramp_plasma > hydro_press > rebond > cryo_quench > hydro_press > cryo_quench > hydro_press > vibro_hone > ramp_plasma > hydro_press x2 > vibro_hone x2 | 57.9 | 58.6 | 60.5 | 92.1 |
| 31-40 | mid gear | vibro_hone x3 > ramp_plasma x2 > overdrive_press > cryo_quench > hydro_press > vibro_hone > cryo_quench > rebond > vibro_hone > rebond > vibro_hone x3 | 64.7 | 65.6 | 65.0 | 97.0 |
| 31-40 | best gear | vibro_hone > ramp_plasma > overdrive_press > vibro_hone > cryo_quench > hydro_press > cryo_quench > vibro_hone x3 | 62.8 | 62.5 | 62.0 | 98.1 |
| 41-53 | no gear | rebond > hydro_press > vibro_hone > cryo_quench > ramp_plasma > overdrive_press > cryo_quench > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > rebond > vibro_hone x3 > ramp_plasma > overdrive_press > hydro_press > vibro_hone x2 | 63.6 | 64.3 | 63.9 | 85.0 |
| 41-53 | mid gear | vibro_hone x3 > surge_plasma > rebond > overdrive_press > cryo_quench > hydro_press > cryo_quench > vibro_hone x3 > hydro_press > ramp_plasma > rebond > vibro_hone x2 > hydro_press x2 | 72.6 | 72.4 | 73.5 | 88.9 |
| 41-53 | best gear | rebond > vibro_hone x2 > red_line_press > ramp_plasma > overdrive_press > cryo_quench > vibro_hone x3 > ramp_plasma > vibro_hone x3 > surge_plasma > overdrive_press | 78.2 | 78.0 | 78.1 | 94.7 |

### Reported metric, not gated: the best fixed rotation for each recipe alone, and what the band's single rotation scores on the same recipe (average quality in points)

| Band | Gear | Recipe | Best rotation for this recipe alone (reported) | Its hold-out | The single rotation on this recipe | Adaptive on this recipe |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | bm_bracer (level 3) | vibro_hone > hydro_press > vibro_hone x2 > hydro_press > vibro_hone > hydro_press x2 > vibro_hone > hydro_press > vibro_hone > hydro_press | 49.9 | 41.2 | 58.5 |
| 1-10 | no gear | b_twinpistol (level 6) | hydro_press x2 > vibro_hone x9 > ramp_plasma > hydro_press x2 | 66.3 | 35.2 | 61.9 |
| 1-10 | no gear | os_hidevest (level 8) | vibro_hone x2 > hydro_press > vibro_hone x2 > ramp_plasma > hydro_press x2 > ramp_plasma > hydro_press > vibro_hone > hydro_press x4 | 69.7 | 54.3 | 67.3 |
| 1-10 | no gear | sp_crestvis (level 8) | rebond > hydro_press x2 > vibro_hone x3 > hydro_press > vibro_hone > rebond > vibro_hone > ramp_plasma > hydro_press x2 | 66.8 | 62.7 | 59.7 |
| 1-10 | no gear | advent_bracer (level 10) | hydro_press > vibro_hone > hydro_press > vibro_hone > hydro_press > vibro_hone x3 > ramp_plasma > hydro_press x3 | 65.0 | 60.7 | 71.4 |
| 1-10 | no gear | chef_ring_1 (level 10) | hydro_press x2 > vibro_hone x7 > ramp_plasma > vibro_hone > hydro_press | 68.0 | 40.9 | 64.9 |
| 1-10 | mid gear | bm_bracer (level 3) | rebond > vibro_hone > hydro_press > vibro_hone > hydro_press > vibro_hone > hydro_press > vibro_hone x2 > rebond > vibro_hone > hydro_press | 85.0 | 90.0 | 91.8 |
| 1-10 | mid gear | b_twinpistol (level 6) | hydro_press x2 > vibro_hone x9 > ramp_plasma > hydro_press x2 | 74.8 | 55.6 | 70.9 |
| 1-10 | mid gear | os_hidevest (level 8) | hydro_press > vibro_hone > hydro_press > vibro_hone > hydro_press > vibro_hone x2 > hydro_press > vibro_hone > ramp_plasma > hydro_press > ramp_plasma > hydro_press | 91.2 | 16.7 | 94.6 |
| 1-10 | mid gear | sp_crestvis (level 8) | hydro_press x3 > vibro_hone x2 > hydro_press > vibro_hone x4 > hydro_press x2 | 91.9 | 90.7 | 85.4 |
| 1-10 | mid gear | advent_bracer (level 10) | hydro_press x2 > vibro_hone x4 > rebond > vibro_hone x2 > ramp_plasma > vibro_hone > hydro_press > rebond > vibro_hone > hydro_press x4 > vibro_hone > hydro_press | 100.0 | 97.8 | 100.0 |
| 1-10 | mid gear | chef_ring_1 (level 10) | hydro_press > vibro_hone x7 > ramp_plasma > hydro_press > vibro_hone x2 > rebond > vibro_hone > ramp_plasma > vibro_hone x2 > rebond > hydro_press > vibro_hone > hydro_press | 100.0 | 63.9 | 99.8 |
| 1-10 | best gear | bm_bracer (level 3) | rebond > hydro_press > vibro_hone > hydro_press > vibro_hone x5 > hydro_press > vibro_hone > ramp_plasma > hydro_press x2 | 100.0 | 98.2 | 100.0 |
| 1-10 | best gear | b_twinpistol (level 6) | hydro_press > vibro_hone x10 > ramp_plasma x2 > hydro_press | 94.1 | 56.2 | 89.2 |
| 1-10 | best gear | os_hidevest (level 8) | vibro_hone > hydro_press > vibro_hone x4 > hydro_press x2 > vibro_hone > hydro_press x3 | 100.0 | 100.0 | 100.0 |
| 1-10 | best gear | sp_crestvis (level 8) | rebond > hydro_press x2 > vibro_hone x6 > rebond > hydro_press > vibro_hone > ramp_plasma > hydro_press x2 | 100.0 | 96.2 | 99.7 |
| 1-10 | best gear | advent_bracer (level 10) | hydro_press > vibro_hone x5 > hydro_press > vibro_hone > rebond > vibro_hone > hydro_press x4 | 100.0 | 97.4 | 99.9 |
| 1-10 | best gear | chef_ring_1 (level 10) | hydro_press > vibro_hone x8 > ramp_plasma > hydro_press > vibro_hone > rebond > vibro_hone x2 > ramp_plasma > hydro_press | 100.0 | 65.5 | 99.6 |
| 11-20 | no gear | val_necklace (level 12) | hydro_press x2 > vibro_hone x3 > hydro_press > vibro_hone x2 > ramp_plasma > vibro_hone x2 > hydro_press x3 | 58.9 | 55.2 | 60.7 |
| 11-20 | no gear | viv_cap (level 14) | vibro_hone > rebond > hydro_press x2 > vibro_hone x4 > ramp_plasma x2 > hydro_press | 59.6 | 32.0 | 63.1 |
| 11-20 | no gear | vet_chn_ring (level 16) | hydro_press x2 > vibro_hone > rebond > vibro_hone > rebond > vibro_hone x2 > hydro_press > vibro_hone x6 > hydro_press x2 | 84.5 | 22.1 | 56.3 |
| 11-20 | no gear | sith_katar (level 18) | hydro_press x2 > vibro_hone x3 > ramp_plasma > vibro_hone x2 > ramp_plasma > hydro_press x5 | 42.7 | 1.5 | 29.1 |
| 11-20 | no gear | frontier_ring (level 20) | overdrive_press > hydro_press > vibro_hone x6 > cryo_quench > vibro_hone x2 | 90.8 | 71.4 | 91.6 |
| 11-20 | no gear | chef_ring_2 (level 20) | vibro_hone > overdrive_press > vibro_hone x3 > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone | 87.3 | 94.9 | 79.3 |
| 11-20 | mid gear | val_necklace (level 12) | hydro_press > vibro_hone x7 > ramp_plasma > cryo_quench > vibro_hone > ramp_plasma > rebond > vibro_hone > hydro_press x2 > vibro_hone > ramp_plasma > hydro_press x2 | 93.0 | 81.8 | 93.4 |
| 11-20 | mid gear | viv_cap (level 14) | rebond > vibro_hone x5 > rebond > vibro_hone > ramp_plasma > vibro_hone > hydro_press x2 > vibro_hone > hydro_press > rebond > hydro_press > vibro_hone > hydro_press x2 | 100.0 | 99.9 | 99.9 |
| 11-20 | mid gear | vet_chn_ring (level 16) | hydro_press > vibro_hone x11 > rebond > hydro_press > vibro_hone > ramp_plasma > rebond > vibro_hone x2 > hydro_press x2 | 100.0 | 46.9 | 98.0 |
| 11-20 | mid gear | sith_katar (level 18) | hydro_press x2 > vibro_hone x4 > ramp_plasma > vibro_hone x2 > ramp_plasma > cryo_quench > vibro_hone x2 > ramp_plasma > rebond > hydro_press x2 | 74.4 | 38.5 | 64.7 |
| 11-20 | mid gear | frontier_ring (level 20) | vibro_hone x7 > overdrive_press > cryo_quench > hydro_press > cryo_quench > rebond > vibro_hone x3 | 100.0 | 73.6 | 98.9 |
| 11-20 | mid gear | chef_ring_2 (level 20) | vibro_hone x8 > hydro_press > vibro_hone x2 > ramp_plasma > rebond > hydro_press > vibro_hone x2 > overdrive_press | 99.9 | 75.3 | 99.1 |
| 11-20 | best gear | val_necklace (level 12) | hydro_press > vibro_hone x7 > ramp_plasma > vibro_hone > ramp_plasma > hydro_press > rebond > vibro_hone x2 > ramp_plasma > vibro_hone > hydro_press | 100.0 | 100.0 | 99.7 |
| 11-20 | best gear | viv_cap (level 14) | hydro_press > vibro_hone x6 > rebond > vibro_hone > ramp_plasma > hydro_press x3 > vibro_hone > hydro_press x2 | 100.0 | 100.0 | 100.0 |
| 11-20 | best gear | vet_chn_ring (level 16) | vibro_hone > rebond > vibro_hone x7 > overdrive_press > vibro_hone x2 > rebond > vibro_hone > overdrive_press > hydro_press | 100.0 | 53.1 | 99.7 |
| 11-20 | best gear | sith_katar (level 18) | hydro_press > vibro_hone x4 > ramp_plasma > vibro_hone > ramp_plasma > vibro_hone x3 > ramp_plasma > vibro_hone > rebond > ramp_plasma > hydro_press > vibro_hone x2 > ramp_plasma > hydro_press x2 | 100.0 | 59.4 | 90.0 |
| 11-20 | best gear | frontier_ring (level 20) | vibro_hone x8 > hydro_press > vibro_hone x2 > ramp_plasma > rebond > vibro_hone > hydro_press > vibro_hone > hydro_press | 100.0 | 71.6 | 99.1 |
| 11-20 | best gear | chef_ring_2 (level 20) | hydro_press > vibro_hone x10 > ramp_plasma > rebond > vibro_hone > overdrive_press > rebond > cryo_quench > vibro_hone | 100.0 | 75.7 | 99.3 |
| 21-30 | no gear | vet_twinelec (level 22) | ramp_plasma > hydro_press > vibro_hone x9 > hydro_press x2 | 61.2 | 69.7 | 83.7 |
| 21-30 | no gear | vet_trnsabstaff (level 24) | overdrive_press > vibro_hone x9 > hydro_press | 61.0 | 21.6 | 79.8 |
| 21-30 | no gear | mb_guard (level 26) | vibro_hone x5 > hydro_press > vibro_hone x2 > ramp_plasma > vibro_hone > hydro_press > rebond > vibro_hone > ramp_plasma > hydro_press x2 | 100.0 | 81.1 | 99.3 |
| 21-30 | no gear | cnt_neck (level 27) | ramp_plasma > cryo_quench > overdrive_press > vibro_hone x3 > ramp_plasma > vibro_hone x2 > cryo_quench x2 > hydro_press > cryo_quench > ramp_plasma > rebond > vibro_hone x2 | 83.4 | 40.8 | 55.2 |
| 21-30 | no gear | tg_mantle (level 29) | ramp_plasma > overdrive_press > vibro_hone x5 > overdrive_press > cryo_quench > vibro_hone x2 > overdrive_press | 85.2 | 76.1 | 98.5 |
| 21-30 | no gear | chef_necklace_3 (level 30) | overdrive_press > vibro_hone > hydro_press > ramp_plasma > vibro_hone x2 > ramp_plasma > cryo_quench > vibro_hone x2 | 87.2 | 7.2 | 80.6 |
| 21-30 | mid gear | vet_twinelec (level 22) | ramp_plasma > hydro_press > vibro_hone x10 > ramp_plasma > rebond > vibro_hone x2 > hydro_press x2 | 91.7 | 44.6 | 91.0 |
| 21-30 | mid gear | vet_trnsabstaff (level 24) | vibro_hone x10 > ramp_plasma > hydro_press > rebond > vibro_hone x2 > cryo_quench > vibro_hone > overdrive_press > vibro_hone | 99.2 | 0.0 | 99.3 |
| 21-30 | mid gear | mb_guard (level 26) | overdrive_press > vibro_hone x5 > overdrive_press > vibro_hone x2 > ramp_plasma > rebond > vibro_hone > hydro_press > overdrive_press | 100.0 | 100.0 | 99.8 |
| 21-30 | mid gear | cnt_neck (level 27) | ramp_plasma > vibro_hone x4 > ramp_plasma > vibro_hone > overdrive_press > cryo_quench > vibro_hone x2 > rebond > ramp_plasma > vibro_hone > ramp_plasma > hydro_press > cryo_quench > vibro_hone | 87.2 | 43.3 | 74.6 |
| 21-30 | mid gear | tg_mantle (level 29) | ramp_plasma > vibro_hone > rebond > vibro_hone x7 > ramp_plasma > vibro_hone x2 > ramp_plasma > rebond > overdrive_press > hydro_press | 100.0 | 100.0 | 99.9 |
| 21-30 | mid gear | chef_necklace_3 (level 30) | vibro_hone x3 > ramp_plasma > overdrive_press > vibro_hone > ramp_plasma > cryo_quench x2 > vibro_hone > hydro_press > ramp_plasma > cryo_quench > ramp_plasma > vibro_hone | 99.9 | 49.8 | 94.9 |
| 21-30 | best gear | vet_twinelec (level 22) | ramp_plasma > vibro_hone x5 > hydro_press > vibro_hone x5 > ramp_plasma > rebond > vibro_hone x3 > hydro_press | 100.0 | 25.0 | 99.3 |
| 21-30 | best gear | vet_trnsabstaff (level 24) | vibro_hone x10 > ramp_plasma > rebond > vibro_hone x4 > ramp_plasma > overdrive_press | 100.0 | 2.6 | 99.4 |
| 21-30 | best gear | mb_guard (level 26) | overdrive_press > vibro_hone x6 > overdrive_press > vibro_hone > overdrive_press | 100.0 | 95.6 | 100.0 |
| 21-30 | best gear | cnt_neck (level 27) | ramp_plasma > vibro_hone x3 > ramp_plasma > vibro_hone x5 > ramp_plasma > hydro_press > vibro_hone x2 > rebond > ramp_plasma > vibro_hone x2 > ramp_plasma > overdrive_press | 100.0 | 82.3 | 80.6 |
| 21-30 | best gear | tg_mantle (level 29) | ramp_plasma > vibro_hone > overdrive_press > vibro_hone x7 > hydro_press > rebond > ramp_plasma > vibro_hone > hydro_press | 100.0 | 97.7 | 100.0 |
| 21-30 | best gear | chef_necklace_3 (level 30) | vibro_hone x3 > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > rebond > vibro_hone > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > hydro_press x2 | 98.3 | 71.2 | 99.1 |
| 31-40 | no gear | sh_charm (level 31) | overdrive_press > vibro_hone > hydro_press > vibro_hone x3 > ramp_plasma > vibro_hone > ramp_plasma > vibro_hone x2 > cryo_quench > rebond > vibro_hone x2 | 100.0 | 55.3 | 84.0 |
| 31-40 | no gear | prm_bul_cloak (level 33) | hydro_press > vibro_hone > ramp_plasma > overdrive_press > cryo_quench > hydro_press > cryo_quench > rebond > vibro_hone x2 | 89.7 | 79.9 | 96.3 |
| 31-40 | no gear | biolab_belt (level 34) | hydro_press x2 > vibro_hone x3 > ramp_plasma > vibro_hone > overdrive_press > cryo_quench > vibro_hone x3 > overdrive_press | 79.8 | 78.6 | 99.4 |
| 31-40 | no gear | proto_greatsword (level 38) | vibro_hone x8 > hydro_press > vibro_hone x2 > ramp_plasma > rebond > hydro_press > vibro_hone x2 > overdrive_press | 89.7 | 7.8 | 94.7 |
| 31-40 | no gear | majestic_ring001 (level 40) | overdrive_press > vibro_hone > ramp_plasma > cryo_quench > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > hydro_press > cryo_quench > vibro_hone | 92.4 | 55.2 | 87.6 |
| 31-40 | no gear | chef_cloak_4 (level 40) | vibro_hone > ramp_plasma > overdrive_press > vibro_hone x4 > ramp_plasma > hydro_press > cryo_quench > vibro_hone > ramp_plasma > hydro_press x2 > vibro_hone x2 | 74.9 | 86.4 | 90.6 |
| 31-40 | mid gear | sh_charm (level 31) | vibro_hone x6 > ramp_plasma > vibro_hone x3 > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > rebond > hydro_press > vibro_hone > cryo_quench > overdrive_press > rebond > vibro_hone x3 | 99.1 | 27.5 | 96.6 |
| 31-40 | mid gear | prm_bul_cloak (level 33) | vibro_hone x3 > ramp_plasma > vibro_hone x3 > ramp_plasma > overdrive_press > vibro_hone x2 > ramp_plasma x2 > overdrive_press | 95.8 | 94.4 | 98.8 |
| 31-40 | mid gear | biolab_belt (level 34) | overdrive_press > vibro_hone x4 > ramp_plasma > vibro_hone x4 > ramp_plasma x2 > rebond > hydro_press > vibro_hone > hydro_press x2 | 100.0 | 97.8 | 99.1 |
| 31-40 | mid gear | proto_greatsword (level 38) | vibro_hone x9 > hydro_press > vibro_hone > ramp_plasma > rebond > vibro_hone x2 > hydro_press > ramp_plasma > rebond > vibro_hone > hydro_press > vibro_hone > hydro_press | 100.0 | 24.8 | 99.4 |
| 31-40 | mid gear | majestic_ring001 (level 40) | vibro_hone x2 > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone x2 > ramp_plasma > overdrive_press > cryo_quench > ramp_plasma > hydro_press > cryo_quench > vibro_hone > rebond > vibro_hone | 92.6 | 47.6 | 91.9 |
| 31-40 | mid gear | chef_cloak_4 (level 40) | vibro_hone > hydro_press > ramp_plasma > vibro_hone x3 > ramp_plasma > overdrive_press > cryo_quench > hydro_press > cryo_quench > vibro_hone x3 | 98.4 | 98.1 | 96.4 |
| 31-40 | best gear | sh_charm (level 31) | vibro_hone x5 > ramp_plasma > vibro_hone x4 > ramp_plasma > vibro_hone > ramp_plasma > hydro_press > rebond > vibro_hone > overdrive_press | 99.9 | 51.8 | 98.6 |
| 31-40 | best gear | prm_bul_cloak (level 33) | vibro_hone x2 > ramp_plasma > vibro_hone x3 > ramp_plasma > hydro_press > vibro_hone x2 > ramp_plasma > vibro_hone > hydro_press > ramp_plasma > rebond > vibro_hone > overdrive_press | 100.0 | 99.2 | 100.0 |
| 31-40 | best gear | biolab_belt (level 34) | vibro_hone x5 > ramp_plasma > overdrive_press > vibro_hone x4 > ramp_plasma > rebond > ramp_plasma > overdrive_press > hydro_press | 100.0 | 100.0 | 99.7 |
| 31-40 | best gear | proto_greatsword (level 38) | hydro_press > vibro_hone x10 > ramp_plasma > rebond > vibro_hone x2 > overdrive_press | 100.0 | 8.3 | 96.8 |
| 31-40 | best gear | majestic_ring001 (level 40) | vibro_hone x2 > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > hydro_press > rebond > ramp_plasma > hydro_press > cryo_quench > vibro_hone x2 > cryo_quench > vibro_hone | 93.1 | 15.2 | 93.9 |
| 31-40 | best gear | chef_cloak_4 (level 40) | vibro_hone > ramp_plasma > overdrive_press > vibro_hone x2 > cryo_quench > vibro_hone > ramp_plasma > vibro_hone x3 > ramp_plasma > vibro_hone > ramp_plasma > rebond > hydro_press > vibro_hone > hydro_press | 100.0 | 97.3 | 99.8 |
| 41-53 | no gear | asc_chn_tunic (level 42) | vibro_hone x4 > ramp_plasma > vibro_hone > rebond > vibro_hone x2 > hydro_press x3 > vibro_hone > hydro_press x2 > cryo_quench > overdrive_press > rebond > vibro_hone x4 | 100.0 | 97.7 | 99.9 |
| 41-53 | no gear | sorting_harness (level 45) | vibro_hone > overdrive_press > vibro_hone x2 > ramp_plasma > hydro_press > vibro_hone x2 > overdrive_press > cryo_quench > rebond > vibro_hone | 83.1 | 60.3 | 96.4 |
| 41-53 | no gear | engi_belt_5 (level 50) | vibro_hone x3 > ramp_plasma > vibro_hone > ramp_plasma > red_line_press > vibro_hone x2 > cryo_quench > overdrive_press > cryo_quench > vibro_hone x4 | 88.3 | 94.4 | 96.9 |
| 41-53 | no gear | fang_gaunt (level 50) | hydro_press x3 > vibro_hone x3 > rebond > ramp_plasma > surge_plasma > vibro_hone x3 > ramp_plasma > hydro_press > vibro_hone > rebond > vibro_hone > hydro_press x3 | 98.7 | 99.8 | 99.9 |
| 41-53 | no gear | chi_greatsword (level 52) | overdrive_press > vibro_hone x3 > hydro_press > vibro_hone x3 > rebond > ramp_plasma > vibro_hone x2 > cryo_quench > vibro_hone x2 | 92.7 | 7.3 | 57.1 |
| 41-53 | no gear | imm_ring (level 52) | vibro_hone > ramp_plasma > overdrive_press > cryo_quench > ramp_plasma > rebond > vibro_hone x2 > hydro_press > cryo_quench > hydro_press > vibro_hone > cryo_quench > vibro_hone | 60.3 | 23.6 | 59.6 |
| 41-53 | mid gear | asc_chn_tunic (level 42) | vibro_hone x4 > ramp_plasma > vibro_hone > hydro_press > vibro_hone > overdrive_press > vibro_hone > hydro_press > overdrive_press | 100.0 | 95.9 | 100.0 |
| 41-53 | mid gear | sorting_harness (level 45) | vibro_hone x4 > ramp_plasma > overdrive_press > vibro_hone x3 > cryo_quench > ramp_plasma > rebond > vibro_hone x2 > hydro_press x2 | 100.0 | 80.7 | 99.9 |
| 41-53 | mid gear | engi_belt_5 (level 50) | vibro_hone x3 > ramp_plasma > vibro_hone > ramp_plasma > red_line_press > cryo_quench > vibro_hone x4 > ramp_plasma > hydro_press > vibro_hone x2 > hydro_press | 100.0 | 82.5 | 98.5 |
| 41-53 | mid gear | fang_gaunt (level 50) | hydro_press > vibro_hone x4 > ramp_plasma > vibro_hone > overdrive_press > vibro_hone x2 > ramp_plasma > rebond > vibro_hone > overdrive_press > hydro_press | 100.0 | 90.1 | 99.9 |
| 41-53 | mid gear | chi_greatsword (level 52) | vibro_hone x7 > ramp_plasma > vibro_hone > overdrive_press > cryo_quench > rebond > vibro_hone x2 > hydro_press > cryo_quench > vibro_hone | 90.4 | 35.4 | 68.4 |
| 41-53 | mid gear | imm_ring (level 52) | overdrive_press > cryo_quench > vibro_hone > ramp_plasma > vibro_hone x2 > ramp_plasma > hydro_press > cryo_quench > vibro_hone | 74.1 | 56.5 | 66.6 |
| 41-53 | best gear | asc_chn_tunic (level 42) | vibro_hone > overdrive_press > vibro_hone x4 > overdrive_press > vibro_hone > hydro_press > overdrive_press | 100.0 | 95.6 | 100.0 |
| 41-53 | best gear | sorting_harness (level 45) | vibro_hone x2 > ramp_plasma > overdrive_press > vibro_hone x7 > surge_plasma > overdrive_press | 100.0 | 99.1 | 100.0 |
| 41-53 | best gear | engi_belt_5 (level 50) | vibro_hone x2 > ramp_plasma > overdrive_press > cryo_quench > overdrive_press > vibro_hone > ramp_plasma > vibro_hone x4 > ramp_plasma > rebond > hydro_press > cryo_quench > hydro_press > cryo_quench > vibro_hone x2 | 100.0 | 99.6 | 100.0 |
| 41-53 | best gear | fang_gaunt (level 50) | overdrive_press > vibro_hone x4 > ramp_plasma > vibro_hone x2 > overdrive_press > vibro_hone > overdrive_press | 100.0 | 100.0 | 100.0 |
| 41-53 | best gear | chi_greatsword (level 52) | vibro_hone x7 > ramp_plasma > vibro_hone > rebond > vibro_hone x2 > overdrive_press > vibro_hone x2 > ramp_plasma > rebond > vibro_hone x2 > hydro_press x2 | 100.0 | 28.3 | 87.1 |
| 41-53 | best gear | imm_ring (level 52) | vibro_hone x2 > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone x2 > ramp_plasma > overdrive_press > cryo_quench > hydro_press > ramp_plasma > cryo_quench > vibro_hone | 88.6 | 45.8 | 81.3 |

## Length

### Actions per attempt of the adaptive player at level (item craft at level)

| Band | Gear | Recipes played | Mean actions per attempt | Shortest recipe | Longest recipe | Stalled attempts | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 12.0 | 11.4 | 12.9 | 0 | 10-20 | Passed |
| 1-10 | mid gear | 6 | 13.4 | 11.9 | 17.5 | 0 | 10-20 | Passed |
| 1-10 | best gear | 6 | 12.1 | 11.1 | 14.6 | 0 | 10-20 | Passed |
| 11-20 | no gear | 6 | 10.4 | 8.0 | 13.0 | 0 | 10-20 | Passed |
| 11-20 | mid gear | 6 | 13.5 | 10.1 | 17.7 | 0 | 10-20 | Passed |
| 11-20 | best gear | 6 | 12.4 | 8.5 | 15.7 | 0 | 10-20 | Passed |
| 21-30 | no gear | 6 | 9.9 | 6.9 | 12.8 | 0 | 10-20 | Failed |
| 21-30 | mid gear | 6 | 10.0 | 7.9 | 13.2 | 0 | 10-20 | Failed |
| 21-30 | best gear | 6 | 8.8 | 6.6 | 11.2 | 0 | 10-20 | Failed |
| 31-40 | no gear | 6 | 9.6 | 7.8 | 12.1 | 0 | 10-20 | Failed |
| 31-40 | mid gear | 6 | 9.1 | 7.5 | 11.0 | 0 | 10-20 | Failed |
| 31-40 | best gear | 6 | 7.9 | 6.2 | 9.7 | 0 | 10-20 | Failed |
| 41-53 | no gear | 6 | 8.1 | 6.8 | 9.3 | 0 | 10-20 | Failed |
| 41-53 | mid gear | 6 | 7.5 | 7.2 | 8.1 | 0 | 10-20 | Failed |
| 41-53 | best gear | 6 | 7.4 | 6.2 | 9.1 | 0 | 10-20 | Failed |

## Economy parity

### Component stacks consumed per item made, failures included, over the recipes the legacy policy makes at least once in the cell (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Parity rule | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 2.79 | 2.70 | 1.03 | 93% | 98% | 64% | 1% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | mid gear | 6 | 2.71 | 2.70 | 1.00 | 98% | 98% | 90% | 11% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | best gear | 6 | 2.67 | 2.67 | 1.00 | 100% | 100% | 98% | 28% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | no gear | 6 | 2.56 | 2.66 | 0.96 | 97% | 91% | 63% | 2% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | mid gear | 6 | 2.53 | 2.55 | 0.99 | 98% | 97% | 92% | 13% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | best gear | 6 | 2.52 | 2.52 | 1.00 | 99% | 99% | 98% | 29% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | no gear | 6 | 3.33 | 3.36 | 0.99 | 100% | 99% | 83% | 8% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | mid gear | 6 | 3.33 | 3.34 | 1.00 | 100% | 99% | 93% | 16% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | best gear | 6 | 3.33 | 3.34 | 1.00 | 100% | 99% | 96% | 29% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | no gear | 6 | 3.00 | 3.05 | 0.98 | 100% | 98% | 92% | 22% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | mid gear | 6 | 3.00 | 3.01 | 1.00 | 100% | 99% | 97% | 38% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | best gear | 6 | 3.00 | 3.02 | 0.99 | 100% | 99% | 98% | 49% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | no gear | 6 | 3.00 | 3.03 | 0.99 | 100% | 98% | 98% | 44% | yes | 0% / 33% | within 10% | Passed |
| 41-53 | mid gear | 6 | 4.67 | 5.38 | 0.87 | 100% | 72% | 89% | 46% | yes | 0% / 0% | at most +10%; uses 13% less, passes | Passed |
| 41-53 | best gear | 6 | 4.67 | 4.90 | 0.95 | 100% | 89% | 95% | 62% | yes | 0% / 0% | at most +10% | Passed |

### Recipes the legacy policy never makes in the cell, left out of the stack and quality comparison above (they still count in the never-made check)

| Band | Gear | Recipe | Level | New success | New avg quality |
| --- | --- | --- | --- | --- | --- |
| 41-53 | no gear | ChiroGreatSword | 52 | 100% | 57% |
| 41-53 | no gear | ImmortalRing | 52 | 100% | 60% |

## XP rate

### Item crafts: XP per hour against the legacy loop (6.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per craft | Legacy actions per craft | Legacy XP per hour | New XP per craft | New actions per craft | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 593 | 6.0 | 59489 | 1871 | 12.0 | 70112 | 1.18 | - | Passed |
| 1-10 | no gear, 1 enhancement | 442 | 6.9 | 38624 | 1561 | 11.7 | 59983 | 1.55 | - | Passed |
| 1-10 | no gear, 2 enhancements | 305 | 6.6 | 27851 | 1069 | 11.3 | 42482 | 1.53 | - | Passed |
| 1-10 | no gear, 3 enhancements | 209 | 6.9 | 18130 | 638 | 11.3 | 25391 | 1.40 | - | Passed |
| 1-10 | mid gear | 656 | 8.8 | 44601 | 2228 | 13.4 | 74732 | 1.68 | - | Passed |
| 1-10 | mid gear, 1 enhancement | 663 | 8.8 | 45415 | 2010 | 14.1 | 64112 | 1.41 | - | Passed |
| 1-10 | mid gear, 2 enhancements | 596 | 9.6 | 37414 | 1628 | 14.3 | 51239 | 1.37 | - | Passed |
| 1-10 | mid gear, 3 enhancements | 549 | 10.2 | 32305 | 1207 | 14.0 | 38851 | 1.20 | - | Passed |
| 1-10 | best gear | 770 | 10.3 | 44808 | 2341 | 12.1 | 86906 | 1.94 | - | Passed |
| 1-10 | best gear, 1 enhancement | 740 | 10.9 | 40867 | 2212 | 13.2 | 75168 | 1.84 | - | Passed |
| 1-10 | best gear, 2 enhancements | 712 | 11.0 | 38779 | 1941 | 14.0 | 62448 | 1.61 | - | Passed |
| 1-10 | best gear, 3 enhancements | 702 | 11.2 | 37692 | 1604 | 14.8 | 48736 | 1.29 | - | Passed |
| 11-20 | no gear | 566 | 7.9 | 43163 | 1155 | 10.4 | 50019 | 1.16 | - | Passed |
| 11-20 | no gear, 1 enhancement | 409 | 7.9 | 30965 | 1038 | 10.5 | 44453 | 1.44 | - | Passed |
| 11-20 | no gear, 2 enhancements | 266 | 7.2 | 22260 | 930 | 10.7 | 39135 | 1.76 | - | Passed |
| 11-20 | no gear, 3 enhancements | 262 | 7.7 | 20334 | 660 | 10.9 | 27176 | 1.34 | - | Passed |
| 11-20 | mid gear | 664 | 11.1 | 35793 | 1374 | 13.5 | 45830 | 1.28 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 658 | 11.8 | 33330 | 1251 | 13.2 | 42780 | 1.28 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 622 | 12.4 | 30075 | 1224 | 13.1 | 41980 | 1.40 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 597 | 13.5 | 26634 | 1051 | 13.5 | 34935 | 1.31 | - | Passed |
| 11-20 | best gear | 766 | 14.4 | 31957 | 1418 | 12.4 | 51564 | 1.61 | - | Passed |
| 11-20 | best gear, 1 enhancement | 759 | 15.2 | 30033 | 1381 | 13.1 | 47383 | 1.58 | - | Passed |
| 11-20 | best gear, 2 enhancements | 744 | 16.0 | 27962 | 1362 | 13.6 | 45017 | 1.61 | - | Passed |
| 11-20 | best gear, 3 enhancements | 753 | 16.5 | 27328 | 1312 | 14.5 | 40854 | 1.49 | - | Passed |
| 21-30 | no gear | 639 | 10.2 | 37566 | 1601 | 9.9 | 72462 | 1.93 | - | Passed |
| 21-30 | no gear, 1 enhancement | 618 | 11.5 | 32333 | 1541 | 10.0 | 69162 | 2.14 | - | Passed |
| 21-30 | no gear, 2 enhancements | 592 | 12.4 | 28664 | 1451 | 10.7 | 61140 | 2.13 | - | Passed |
| 21-30 | no gear, 3 enhancements | 575 | 13.5 | 25564 | 1298 | 11.4 | 51231 | 2.00 | - | Passed |
| 21-30 | mid gear | 691 | 16.5 | 25152 | 1693 | 10.0 | 76342 | 3.04 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 679 | 16.8 | 24257 | 1645 | 9.7 | 76048 | 3.14 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 665 | 17.4 | 22996 | 1634 | 10.7 | 68777 | 2.99 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 647 | 17.6 | 22047 | 1544 | 11.2 | 61813 | 2.80 | - | Passed |
| 21-30 | best gear | 770 | 21.3 | 21631 | 1720 | 8.8 | 87640 | 4.05 | - | Passed |
| 21-30 | best gear, 1 enhancement | 754 | 21.6 | 20987 | 1711 | 9.4 | 81679 | 3.89 | - | Passed |
| 21-30 | best gear, 2 enhancements | 752 | 22.0 | 20504 | 1708 | 10.0 | 77067 | 3.76 | - | Passed |
| 21-30 | best gear, 3 enhancements | 744 | 21.9 | 20368 | 1698 | 10.5 | 72830 | 3.58 | - | Passed |
| 31-40 | no gear | 722 | 15.3 | 28279 | 2362 | 9.6 | 110994 | 3.92 | - | Passed |
| 31-40 | no gear, 1 enhancement | 648 | 15.5 | 25053 | 2226 | 10.2 | 98059 | 3.91 | - | Passed |
| 31-40 | no gear, 2 enhancements | 632 | 15.2 | 25015 | 1943 | 10.0 | 87089 | 3.48 | - | Passed |
| 31-40 | no gear, 3 enhancements | 609 | 16.1 | 22695 | 1668 | 10.8 | 69431 | 3.06 | - | Passed |
| 31-40 | mid gear | 827 | 20.5 | 24162 | 2423 | 9.1 | 119763 | 4.96 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 788 | 21.1 | 22421 | 2389 | 9.9 | 108654 | 4.85 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 756 | 20.5 | 22069 | 2316 | 10.8 | 96842 | 4.39 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 716 | 20.2 | 21265 | 2209 | 11.4 | 86917 | 4.09 | - | Passed |
| 31-40 | best gear | 889 | 25.4 | 21025 | 2437 | 7.9 | 139217 | 6.62 | - | Passed |
| 31-40 | best gear, 1 enhancement | 895 | 26.0 | 20640 | 2448 | 8.7 | 127196 | 6.16 | - | Passed |
| 31-40 | best gear, 2 enhancements | 860 | 26.0 | 19811 | 2429 | 10.0 | 109528 | 5.53 | - | Passed |
| 31-40 | best gear, 3 enhancements | 836 | 24.7 | 20302 | 2358 | 10.8 | 97888 | 4.82 | - | Passed |
| 41-53 | no gear | 608 | 15.9 | 22911 | 1296 | 8.1 | 72400 | 3.16 | - | Passed |
| 41-53 | no gear, 1 enhancement | 543 | 14.4 | 22557 | 1277 | 9.4 | 60857 | 2.70 | - | Passed |
| 41-53 | no gear, 2 enhancements | 487 | 14.3 | 20434 | 1145 | 10.5 | 48991 | 2.40 | - | Passed |
| 41-53 | no gear, 3 enhancements | 455 | 14.7 | 18538 | 827 | 11.7 | 31810 | 1.72 | - | Passed |
| 41-53 | mid gear | 746 | 23.0 | 19424 | 1327 | 7.5 | 79365 | 4.09 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 695 | 21.5 | 19414 | 1329 | 8.7 | 69052 | 3.56 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 620 | 18.8 | 19811 | 1335 | 9.9 | 60597 | 3.06 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 586 | 17.4 | 20185 | 1285 | 10.9 | 52905 | 2.62 | - | Passed |
| 41-53 | best gear | 951 | 27.5 | 20773 | 1374 | 7.4 | 83314 | 4.01 | - | Passed |
| 41-53 | best gear, 1 enhancement | 890 | 27.1 | 19687 | 1377 | 8.1 | 76898 | 3.91 | - | Passed |
| 41-53 | best gear, 2 enhancements | 769 | 23.8 | 19375 | 1358 | 8.9 | 68344 | 3.53 | - | Passed |
| 41-53 | best gear, 3 enhancements | 711 | 21.4 | 19960 | 1362 | 10.1 | 60699 | 3.04 | - | Passed |

## Enhancement parity

### Rank 50, best gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 85%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 6 | 60-70 | 33% | 100% | 96% | 0.96 | 100% | 99.6% | 1.00 (+/- 0.00) | 1.05 | >= 1.00 | Passed |
| 41-45 | 2 | 6 | 60-70 | 65% | 100% | 89% | 1.78 | 100% | 99.6% | 2.00 (+/- 0.00) | 1.13 | >= 1.00 | Passed |
| 41-45 | 3 | 6 | 60-70 | 98% | 100% | 83% | 2.47 | 100% | 99.6% | 3.00 (+/- 0.00) | 1.21 | >= 1.00 | Passed |
| 46-50 | 1 | 6 | 60 | 32% | 100% | 83% | 0.83 | 100% | 99.1% | 1.00 (+/- 0.00) | 1.21 | >= 1.00 | Passed |
| 46-50 | 2 | 6 | 60 | 64% | 97% | 72% | 1.38 | 100% | 99.2% | 1.99 (+/- 0.01) | 1.44 | >= 1.00 | Passed |
| 46-50 | 3 | 6 | 60 | 96% | 100% | 60% | 1.79 | 100% | 99.3% | 2.99 (+/- 0.01) | 1.67 | >= 1.00 | Passed |
| 51-53 | 1 | 6 | 60-70 | 8% | 49% | 7% | 0.03 | 100% | 93.4% | 1.00 (+/- 0.00) | 30.30 | >= 1.00 | Passed |
| 51-53 | 2 | 6 | 60-70 | 17% | 22% | 4% | 0.02 | 100% | 95.5% | 1.83 (+/- 0.03) | 113.01 | >= 1.00 | Passed |
| 51-53 | 3 | 6 | 60-70 | 25% | 9% | 4% | 0.01 | 100% | 93.8% | 2.60 (+/- 0.06) | 288.89 | >= 1.00 | Passed |

### Thresholds, first / second / third slot, that would meet parity for the quality measured (the strictest set that keeps every case at least at today's, never falling from one slot to the next)

| Scope | Thresholds that meet parity | In steps of 5 | Thresholds in force |
| --- | --- | --- | --- |
| Every band together (the shared thresholds) | 96% / 100% / 100% | 95% / 100% / 100% | 50% / 85% / 85% |
| Band 41-45 alone | 96% / 100% / 100% | 95% / 100% / 100% | 50% / 85% / 85% |
| Band 46-50 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 85% |
| Band 51-53 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 85% |

### Rank 50, best gear: quality of the items the new minigame made, in whole percent, by band and slots filled

| Band | Slots filled | Items made | 10th percentile | 25th percentile | Median | 75th percentile | 90th percentile | Made at 100% |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 92% |
| 41-45 | 2 | 100% | 99% | 100% | 100% | 100% | 100% | 88% |
| 41-45 | 3 | 100% | 99% | 100% | 100% | 100% | 100% | 89% |
| 46-50 | 1 | 100% | 99% | 100% | 100% | 100% | 100% | 90% |
| 46-50 | 2 | 100% | 98% | 100% | 100% | 100% | 100% | 90% |
| 46-50 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 93% |
| 51-53 | 1 | 100% | 78% | 87% | 100% | 100% | 100% | 57% |
| 51-53 | 2 | 100% | 79% | 97% | 100% | 100% | 100% | 68% |
| 51-53 | 3 | 100% | 72% | 97% | 100% | 100% | 100% | 69% |

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
- Every craft in this scenario is played by a crafter who bought the Forgemaster line in full (every purchase their Smithery rank allows) and plays Hot Forging, so no gate here is an unperked result.
