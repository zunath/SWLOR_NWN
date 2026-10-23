# Crafting revamp: balance gates of the Plasma Forge minigame

- Command: `SWLOR.CraftSim gates --scenario forge --runs 100 --seed 1 --recipes-per-band 6 --out design/testing/crafting-revamp/forge-gates.md`
- Seed 1; 100 runs per case.

## Inputs

- Scenario: Plasma Forge (`forge`), Smithery recipes, minigame Forge.
- Quality: the quality meter's share of its maximum when the item is made, in points (0-100). A broken or abandoned piece counts 0, so every average includes the crafts that failed.
- Crafter: the rank the scenario's cohort gives each recipe (by default the recipe's level, at most 50, so a crafter works at their own level), with no food or status bonuses and no perks unless a gate names a style. Gear levels are the plan's none, middle and best tier of the skill's crafting gear:
  - no gear (Control 0, Craftsmanship 0, CP 0)
  - mid gear (Control 13, Craftsmanship 12, CP 23)
  - best gear (Control 29, Craftsmanship 30, CP 37)
- Recipes: 1-10: 6 of 119; 11-20: 6 of 142; 21-30: 6 of 195; 31-40: 6 of 134; 41-53: 6 of 187 recipes played.
- Adaptive player: depth 4, forecast samples per result 2, budget 6000 engine copies per decision, 0.5% of value lost per extra action, heuristic DefaultLeafHeuristic. Each attempt's dice come from its own stream of the seed.
- Fixed-rotation solver: beam 8, cycles up to 3, 8 adaptive lines as seeds, up to 40 actions, 64 training attempts per local-search round and 150 selection attempts for each of 12 finalists, local search from the best 3 candidate(s) for up to 3 round(s) each. Gate 1 searches one sequence for each band and gear tier across the band's sampled recipes (every recipe weighing the same; an action a recipe's engine cannot take is skipped there), and also each recipe's own best sequence as a reported metric. The winner is chosen on separate attempts and then scored on a third set (hold-out) with 100 attempts per recipe.
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

## Verdicts

| Gate | Verdict | Summary |
| --- | --- | --- |
| No fixed rotation | Passed | Adaptive play beats the best single fixed rotation by at least 10 points in every band and gear tier (smallest margin 12.0 in 1-10 / best gear). The best rotation for each recipe alone, a reported metric, is -5.1 to 2.1 points behind adaptive play. |
| Style balance | NotApplicable | The scenario offers no style configurations yet, so there is nothing to compare. |
| Perks optional | Passed | An unperked rank-50 crafter in best gear reaches maximum quality on at least 50% of crafts in every at-level band (lowest 57%). |
| Length | Passed | Every band and gear tier takes 10-20 actions (item craft at level) on average. |
| Economy parity | Passed | Component stacks per item are within 10% of the legacy baseline where its success rate is at least 90%, and at most 10% above it where it is lower, at no less quality, in every band and gear tier. 1 case(s) use more than 10% less material than today, which passes. |
| XP rate | Passed | Item crafts: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.00x in 41-53 / no gear, 3 enhancements). |
| Enhancement parity | Passed | At rank 50 in best gear, the thresholds in force (50% / 85% / 85%) transfer at least 1.00 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 1.05 times today's). Thresholds that meet parity in every band for this quality: 92% / 100% / 100%. |

## No fixed rotation

### Average quality: adaptive play against the best single fixed rotation of each band and gear tier (quality points; a broken piece counts 0)

| Band | Gear | Recipes played | Adaptive avg quality | Best single rotation avg quality | Margin | Required | Best rotation per recipe avg quality (reported) | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 62.0 | 48.8 | 13.2 | >= 10 | 59.8 | Passed |
| 1-10 | mid gear | 6 | 89.1 | 67.5 | 21.7 | >= 10 | 89.6 | Passed |
| 1-10 | best gear | 6 | 97.8 | 85.7 | 12.0 | >= 10 | 99.0 | Passed |
| 11-20 | no gear | 6 | 63.0 | 37.7 | 25.3 | >= 10 | 64.9 | Passed |
| 11-20 | mid gear | 6 | 89.9 | 65.9 | 24.0 | >= 10 | 93.1 | Passed |
| 11-20 | best gear | 6 | 95.7 | 75.6 | 20.2 | >= 10 | 98.1 | Passed |
| 21-30 | no gear | 6 | 82.3 | 46.9 | 35.4 | >= 10 | 80.8 | Passed |
| 21-30 | mid gear | 6 | 91.5 | 55.7 | 35.8 | >= 10 | 93.4 | Passed |
| 21-30 | best gear | 6 | 96.0 | 50.7 | 45.2 | >= 10 | 98.5 | Passed |
| 31-40 | no gear | 6 | 77.5 | 54.9 | 22.5 | >= 10 | 82.6 | Passed |
| 31-40 | mid gear | 6 | 91.3 | 64.0 | 27.3 | >= 10 | 94.1 | Passed |
| 31-40 | best gear | 6 | 96.3 | 65.1 | 31.3 | >= 10 | 95.7 | Passed |
| 41-53 | no gear | 6 | 82.4 | 65.4 | 17.0 | >= 10 | 84.7 | Passed |
| 41-53 | mid gear | 6 | 89.1 | 70.2 | 19.0 | >= 10 | 93.4 | Passed |
| 41-53 | best gear | 6 | 92.9 | 69.4 | 23.5 | >= 10 | 95.0 | Passed |

### The single rotation found for each band and gear tier, searched across the band's sampled recipes (average quality in points)

| Band | Gear | Best single rotation for the band | Training | Selection | Hold-out | Adaptive on these recipes |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | hydro_press > vibro_hone x4 > ramp_plasma > hydro_press x2 > vibro_hone > hydro_press x3 | 49.0 | 50.7 | 48.8 | 62.0 |
| 1-10 | mid gear | hydro_press x2 > vibro_hone x5 > hydro_press x2 > ramp_plasma > hydro_press > ramp_plasma > hydro_press | 66.9 | 67.3 | 67.5 | 89.1 |
| 1-10 | best gear | vibro_hone > cryo_quench > vibro_hone x3 > hydro_press > vibro_hone > hydro_press > vibro_hone > ramp_plasma > vibro_hone > hydro_press | 84.3 | 84.0 | 85.7 | 97.8 |
| 11-20 | no gear | hydro_press x2 > vibro_hone x5 > ramp_plasma > hydro_press x3 | 37.2 | 37.6 | 37.7 | 63.0 |
| 11-20 | mid gear | hydro_press > vibro_hone > cryo_quench > vibro_hone > hydro_press > vibro_hone > cryo_quench > vibro_hone > hydro_press > cryo_quench > ramp_plasma > rebond > ramp_plasma > vibro_hone > rebond > vibro_hone > rebond > vibro_hone > hydro_press > ramp_plasma > vibro_hone > hydro_press x2 > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone | 65.9 | 66.1 | 65.9 | 89.9 |
| 11-20 | best gear | vibro_hone x2 > hydro_press > vibro_hone x3 > ramp_plasma x2 > rebond > hydro_press > vibro_hone > cryo_quench > vibro_hone x2 > ramp_plasma x2 > hydro_press x2 > vibro_hone | 74.9 | 74.9 | 75.6 | 95.7 |
| 21-30 | no gear | vibro_hone x2 > ramp_plasma > hydro_press x2 > ramp_plasma > cryo_quench > hydro_press > cryo_quench > rebond > vibro_hone > rebond > vibro_hone > hydro_press > cryo_quench > hydro_press > cryo_quench > vibro_hone x4 | 47.5 | 47.2 | 46.9 | 82.3 |
| 21-30 | mid gear | rebond > vibro_hone > ramp_plasma > hydro_press > vibro_hone x2 > cryo_quench > ramp_plasma > cryo_quench > hydro_press > cryo_quench > hydro_press > vibro_hone > rebond x3 > vibro_hone > cryo_quench > rebond > vibro_hone x2 > hydro_press > vibro_hone x2 > ramp_plasma > hydro_press x2 > vibro_hone > hydro_press | 55.6 | 56.7 | 55.7 | 91.5 |
| 21-30 | best gear | rebond > vibro_hone x6 > ramp_plasma > vibro_hone > hydro_press > cryo_quench > ramp_plasma > rebond > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone > hydro_press x3 | 51.2 | 50.7 | 50.7 | 96.0 |
| 31-40 | no gear | vibro_hone x2 > ramp_plasma x2 > hydro_press x2 > cryo_quench > hydro_press > cryo_quench > hydro_press > cryo_quench > ramp_plasma > rebond > hydro_press > cryo_quench > vibro_hone x5 | 54.2 | 54.0 | 54.9 | 77.5 |
| 31-40 | mid gear | vibro_hone x2 > ramp_plasma x2 > hydro_press x2 > cryo_quench > hydro_press > cryo_quench > vibro_hone > hydro_press > ramp_plasma > cryo_quench > rebond > vibro_hone | 64.0 | 64.0 | 64.0 | 91.3 |
| 31-40 | best gear | vibro_hone x3 > ramp_plasma x2 > hydro_press > cryo_quench > ramp_plasma > hydro_press > cryo_quench > hydro_press > cryo_quench > rebond > vibro_hone x3 > rebond x2 > vibro_hone x3 | 64.4 | 65.7 | 65.1 | 96.3 |
| 41-53 | no gear | rebond > vibro_hone > ramp_plasma > hydro_press x2 > cryo_quench x2 > ramp_plasma > hydro_press > cryo_quench > hydro_press > cryo_quench > rebond > ramp_plasma > hydro_press > cryo_quench > hydro_press > vibro_hone x2 > cryo_quench > vibro_hone x3 | 66.1 | 66.0 | 65.4 | 82.4 |
| 41-53 | mid gear | hydro_press > vibro_hone > ramp_plasma > hydro_press > ramp_plasma > cryo_quench > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > rebond > vibro_hone > rebond > ramp_plasma > rebond x2 > hydro_press > cryo_quench > vibro_hone x3 > hydro_press x2 > vibro_hone x2 | 69.5 | 70.6 | 70.2 | 89.1 |
| 41-53 | best gear | vibro_hone x2 > ramp_plasma > hydro_press x2 > cryo_quench > ramp_plasma > hydro_press > cryo_quench > rebond > vibro_hone > hydro_press > vibro_hone > rebond x2 > vibro_hone x2 > cryo_quench > rebond > hydro_press > cryo_quench > vibro_hone x3 | 69.8 | 70.5 | 69.4 | 92.9 |

### Reported metric, not gated: the best fixed rotation for each recipe alone, and what the band's single rotation scores on the same recipe (average quality in points)

| Band | Gear | Recipe | Best rotation for this recipe alone (reported) | Its hold-out | The single rotation on this recipe | Adaptive on this recipe |
| --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | bm_bracer (level 3) | vibro_hone > hydro_press > vibro_hone x2 > hydro_press > vibro_hone > hydro_press x2 > vibro_hone > hydro_press > vibro_hone > hydro_press | 50.1 | 35.0 | 61.6 |
| 1-10 | no gear | b_twinpistol (level 6) | hydro_press x2 > vibro_hone x8 > hydro_press x3 | 58.9 | 31.5 | 59.4 |
| 1-10 | no gear | os_hidevest (level 8) | vibro_hone x2 > hydro_press > vibro_hone x2 > ramp_plasma > hydro_press x2 > ramp_plasma > hydro_press > vibro_hone > hydro_press x4 | 66.7 | 63.8 | 66.7 |
| 1-10 | no gear | sp_crestvis (level 8) | rebond > hydro_press x2 > vibro_hone x3 > hydro_press > vibro_hone > rebond > vibro_hone > ramp_plasma > hydro_press x2 | 65.1 | 59.4 | 62.7 |
| 1-10 | no gear | advent_bracer (level 10) | hydro_press x3 > vibro_hone > hydro_press > vibro_hone > rebond > vibro_hone x3 > ramp_plasma > hydro_press x3 | 58.4 | 62.4 | 60.4 |
| 1-10 | no gear | chef_ring_1 (level 10) | hydro_press x2 > vibro_hone x6 > ramp_plasma x2 > vibro_hone > hydro_press x2 | 59.7 | 40.8 | 61.1 |
| 1-10 | mid gear | bm_bracer (level 3) | rebond > vibro_hone > hydro_press > vibro_hone > hydro_press > vibro_hone > hydro_press > vibro_hone x2 > rebond > vibro_hone > hydro_press | 81.2 | 75.3 | 88.7 |
| 1-10 | mid gear | b_twinpistol (level 6) | hydro_press x2 > vibro_hone x9 > ramp_plasma > hydro_press x2 | 75.8 | 40.8 | 73.7 |
| 1-10 | mid gear | os_hidevest (level 8) | hydro_press > vibro_hone > hydro_press > vibro_hone > hydro_press > vibro_hone x2 > hydro_press > vibro_hone > ramp_plasma > hydro_press > ramp_plasma > hydro_press | 91.0 | 90.0 | 93.9 |
| 1-10 | mid gear | sp_crestvis (level 8) | hydro_press x3 > vibro_hone x2 > hydro_press > vibro_hone x4 > hydro_press x2 | 90.2 | 72.7 | 82.6 |
| 1-10 | mid gear | advent_bracer (level 10) | vibro_hone > hydro_press > vibro_hone x3 > rebond > vibro_hone > ramp_plasma > vibro_hone x2 > ramp_plasma > hydro_press x2 > vibro_hone > hydro_press x3 | 100.0 | 77.3 | 98.8 |
| 1-10 | mid gear | chef_ring_1 (level 10) | hydro_press > vibro_hone x4 > ramp_plasma > rebond > vibro_hone x2 > ramp_plasma > hydro_press > vibro_hone x5 > hydro_press | 99.4 | 48.7 | 97.2 |
| 1-10 | best gear | bm_bracer (level 3) | rebond > hydro_press > vibro_hone > hydro_press > vibro_hone x5 > hydro_press > vibro_hone > ramp_plasma > hydro_press x2 | 100.0 | 97.4 | 99.9 |
| 1-10 | best gear | b_twinpistol (level 6) | hydro_press > vibro_hone x10 > ramp_plasma x2 > hydro_press | 93.8 | 57.5 | 87.6 |
| 1-10 | best gear | os_hidevest (level 8) | vibro_hone > hydro_press > vibro_hone x4 > hydro_press x2 > vibro_hone > hydro_press x3 | 100.0 | 96.0 | 100.0 |
| 1-10 | best gear | sp_crestvis (level 8) | rebond > hydro_press x2 > vibro_hone x6 > rebond > hydro_press > vibro_hone > ramp_plasma > hydro_press x2 | 100.0 | 99.3 | 99.8 |
| 1-10 | best gear | advent_bracer (level 10) | vibro_hone > hydro_press > vibro_hone x4 > rebond > vibro_hone > ramp_plasma > vibro_hone > hydro_press x5 | 100.0 | 89.9 | 100.0 |
| 1-10 | best gear | chef_ring_1 (level 10) | hydro_press > vibro_hone x6 > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone > hydro_press > rebond > vibro_hone x2 > hydro_press > rebond > ramp_plasma > vibro_hone > hydro_press | 100.0 | 74.3 | 99.4 |
| 11-20 | no gear | val_necklace (level 12) | hydro_press > vibro_hone x5 > ramp_plasma > hydro_press x3 | 41.5 | 43.0 | 41.8 |
| 11-20 | no gear | viv_cap (level 14) | hydro_press x3 > vibro_hone > hydro_press > vibro_hone x4 > ramp_plasma > hydro_press x3 | 60.8 | 58.7 | 59.5 |
| 11-20 | no gear | vet_chn_ring (level 16) | hydro_press x2 > rebond > vibro_hone x7 > rebond > hydro_press > vibro_hone x2 > hydro_press x4 | 77.3 | 39.1 | 69.6 |
| 11-20 | no gear | sith_katar (level 18) | hydro_press > vibro_hone > cryo_quench > vibro_hone x2 > ramp_plasma > hydro_press x2 > vibro_hone > hydro_press > vibro_hone > hydro_press | 32.6 | 3.3 | 25.2 |
| 11-20 | no gear | frontier_ring (level 20) | vibro_hone x2 > hydro_press > vibro_hone > cryo_quench > vibro_hone > hydro_press > cryo_quench > hydro_press > vibro_hone x2 > cryo_quench > hydro_press > cryo_quench > ramp_plasma > rebond > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone > cryo_quench > hydro_press > vibro_hone | 87.0 | 40.0 | 97.0 |
| 11-20 | no gear | chef_ring_2 (level 20) | hydro_press > vibro_hone > hydro_press > vibro_hone x2 > hydro_press > vibro_hone x2 > ramp_plasma > cryo_quench > vibro_hone > hydro_press > cryo_quench > vibro_hone x3 | 90.0 | 41.9 | 84.7 |
| 11-20 | mid gear | val_necklace (level 12) | hydro_press > vibro_hone x5 > ramp_plasma > vibro_hone x2 > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > rebond > vibro_hone x2 > hydro_press x2 | 93.8 | 73.6 | 90.8 |
| 11-20 | mid gear | viv_cap (level 14) | hydro_press > vibro_hone x4 > rebond > vibro_hone > ramp_plasma > hydro_press > vibro_hone x2 > ramp_plasma > hydro_press x2 > vibro_hone > hydro_press x2 | 100.0 | 99.5 | 98.3 |
| 11-20 | mid gear | vet_chn_ring (level 16) | vibro_hone x2 > rebond > vibro_hone x6 > rebond > ramp_plasma > hydro_press > vibro_hone > rebond > vibro_hone x2 > hydro_press x2 > vibro_hone > hydro_press x2 | 99.9 | 50.9 | 98.8 |
| 11-20 | mid gear | sith_katar (level 18) | hydro_press x2 > vibro_hone x3 > ramp_plasma > vibro_hone x3 > ramp_plasma > vibro_hone > ramp_plasma > hydro_press x3 | 65.4 | 0.0 | 63.2 |
| 11-20 | mid gear | frontier_ring (level 20) | vibro_hone > hydro_press > vibro_hone x7 > hydro_press > vibro_hone > ramp_plasma > rebond > vibro_hone x3 > rebond > vibro_hone > hydro_press x3 | 100.0 | 91.8 | 99.4 |
| 11-20 | mid gear | chef_ring_2 (level 20) | hydro_press > vibro_hone x7 > ramp_plasma > rebond > vibro_hone x2 > ramp_plasma > hydro_press > vibro_hone x2 > hydro_press x2 | 99.4 | 79.4 | 88.9 |
| 11-20 | best gear | val_necklace (level 12) | hydro_press > vibro_hone x5 > ramp_plasma > vibro_hone x3 > ramp_plasma x2 > hydro_press > rebond > vibro_hone x3 > ramp_plasma > hydro_press | 100.0 | 83.0 | 99.1 |
| 11-20 | best gear | viv_cap (level 14) | hydro_press > vibro_hone x5 > rebond > vibro_hone > ramp_plasma > hydro_press > vibro_hone > hydro_press x4 | 100.0 | 100.0 | 99.8 |
| 11-20 | best gear | vet_chn_ring (level 16) | vibro_hone x2 > rebond > vibro_hone x7 > ramp_plasma > hydro_press x2 > vibro_hone x2 > rebond > hydro_press x2 | 100.0 | 67.1 | 99.8 |
| 11-20 | best gear | sith_katar (level 18) | vibro_hone x5 > ramp_plasma > vibro_hone > ramp_plasma x2 > hydro_press > vibro_hone x2 > ramp_plasma > hydro_press > rebond > vibro_hone > hydro_press | 88.6 | 51.9 | 77.6 |
| 11-20 | best gear | frontier_ring (level 20) | hydro_press > vibro_hone x9 > ramp_plasma > hydro_press > rebond > vibro_hone x3 > hydro_press | 100.0 | 67.3 | 99.8 |
| 11-20 | best gear | chef_ring_2 (level 20) | hydro_press > vibro_hone x7 > ramp_plasma > hydro_press > vibro_hone x2 > ramp_plasma > rebond > vibro_hone x2 > hydro_press x2 | 100.0 | 84.1 | 98.3 |
| 21-30 | no gear | vet_twinelec (level 22) | ramp_plasma > hydro_press > vibro_hone x6 > hydro_press > vibro_hone > cryo_quench > hydro_press > cryo_quench > vibro_hone x2 | 61.7 | 18.4 | 62.2 |
| 21-30 | no gear | vet_trnsabstaff (level 24) | rebond > vibro_hone > hydro_press > vibro_hone x7 > hydro_press > vibro_hone > cryo_quench > ramp_plasma > hydro_press > rebond > vibro_hone x3 | 81.2 | 17.8 | 89.5 |
| 21-30 | no gear | mb_guard (level 26) | vibro_hone x3 > ramp_plasma > rebond > hydro_press > vibro_hone > cryo_quench > vibro_hone x3 > ramp_plasma > hydro_press x2 > vibro_hone > hydro_press x3 | 98.6 | 76.5 | 98.9 |
| 21-30 | no gear | cnt_neck (level 27) | vibro_hone > ramp_plasma x2 > hydro_press x2 > vibro_hone x2 > cryo_quench > hydro_press > cryo_quench > vibro_hone x2 > rebond > vibro_hone x2 | 73.8 | 43.0 | 77.0 |
| 21-30 | no gear | tg_mantle (level 29) | hydro_press > cryo_quench > vibro_hone > ramp_plasma > rebond > vibro_hone x2 > ramp_plasma x2 > hydro_press x2 > cryo_quench > vibro_hone > hydro_press > cryo_quench > hydro_press > cryo_quench > vibro_hone x3 | 99.9 | 89.7 | 99.6 |
| 21-30 | no gear | chef_necklace_3 (level 30) | hydro_press > vibro_hone x2 > ramp_plasma x2 > hydro_press > vibro_hone > ramp_plasma > hydro_press > cryo_quench > hydro_press > cryo_quench > vibro_hone x4 | 69.6 | 36.0 | 66.8 |
| 21-30 | mid gear | vet_twinelec (level 22) | ramp_plasma > vibro_hone > hydro_press > vibro_hone x7 > rebond > ramp_plasma > vibro_hone x2 > rebond > hydro_press > cryo_quench > hydro_press > vibro_hone x2 | 87.7 | 21.4 | 79.2 |
| 21-30 | mid gear | vet_trnsabstaff (level 24) | hydro_press x2 > vibro_hone x8 > ramp_plasma > rebond > cryo_quench > vibro_hone | 100.0 | 15.5 | 99.0 |
| 21-30 | mid gear | mb_guard (level 26) | vibro_hone x4 > ramp_plasma > rebond > vibro_hone x2 > hydro_press x2 > vibro_hone > ramp_plasma > hydro_press > vibro_hone > hydro_press x3 | 100.0 | 94.4 | 99.9 |
| 21-30 | mid gear | cnt_neck (level 27) | vibro_hone > ramp_plasma > vibro_hone x2 > ramp_plasma > hydro_press > vibro_hone x3 > ramp_plasma > hydro_press > cryo_quench > vibro_hone x2 > ramp_plasma x2 > hydro_press > vibro_hone | 81.2 | 59.4 | 89.0 |
| 21-30 | mid gear | tg_mantle (level 29) | vibro_hone x3 > ramp_plasma > hydro_press > vibro_hone x2 > rebond > vibro_hone x2 > ramp_plasma > vibro_hone > hydro_press x2 > vibro_hone > rebond > hydro_press > vibro_hone > hydro_press x2 | 100.0 | 95.2 | 99.9 |
| 21-30 | mid gear | chef_necklace_3 (level 30) | hydro_press > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > vibro_hone > hydro_press > ramp_plasma x2 > hydro_press > cryo_quench > hydro_press x2 > vibro_hone x2 | 91.5 | 48.1 | 81.9 |
| 21-30 | best gear | vet_twinelec (level 22) | ramp_plasma > hydro_press > vibro_hone x9 > ramp_plasma > rebond > vibro_hone x3 > ramp_plasma > vibro_hone > hydro_press | 100.0 | 44.9 | 95.9 |
| 21-30 | best gear | vet_trnsabstaff (level 24) | hydro_press > vibro_hone x11 > rebond > ramp_plasma > vibro_hone > hydro_press > rebond > cryo_quench > vibro_hone | 100.0 | 59.3 | 99.3 |
| 21-30 | best gear | mb_guard (level 26) | hydro_press > vibro_hone x5 > ramp_plasma > hydro_press > vibro_hone > rebond > vibro_hone > hydro_press x4 | 100.0 | 100.0 | 99.9 |
| 21-30 | best gear | cnt_neck (level 27) | vibro_hone > ramp_plasma > vibro_hone x2 > ramp_plasma > hydro_press > vibro_hone x4 > ramp_plasma > cryo_quench x2 > vibro_hone > ramp_plasma > rebond > vibro_hone x2 > ramp_plasma > hydro_press | 99.5 | 0.0 | 87.0 |
| 21-30 | best gear | tg_mantle (level 29) | hydro_press > vibro_hone x4 > ramp_plasma > vibro_hone x2 > rebond > vibro_hone > hydro_press x2 > vibro_hone > hydro_press x3 | 100.0 | 100.0 | 100.0 |
| 21-30 | best gear | chef_necklace_3 (level 30) | vibro_hone x3 > ramp_plasma > vibro_hone > ramp_plasma > vibro_hone x2 > ramp_plasma x2 > hydro_press x2 > ramp_plasma > cryo_quench > hydro_press > cryo_quench > vibro_hone > rebond > vibro_hone x2 | 91.5 | 0.0 | 93.7 |
| 31-40 | no gear | sh_charm (level 31) | hydro_press > ramp_plasma > hydro_press x2 > vibro_hone x4 > ramp_plasma > cryo_quench > vibro_hone | 81.3 | 34.8 | 85.6 |
| 31-40 | no gear | prm_bul_cloak (level 33) | vibro_hone x2 > hydro_press > ramp_plasma x2 > hydro_press x3 > ramp_plasma > cryo_quench > ramp_plasma > hydro_press > cryo_quench > vibro_hone x3 | 81.5 | 82.5 | 72.3 |
| 31-40 | no gear | biolab_belt (level 34) | hydro_press > vibro_hone > hydro_press > vibro_hone x2 > ramp_plasma > hydro_press > ramp_plasma > hydro_press > cryo_quench > hydro_press > cryo_quench > rebond > vibro_hone | 98.8 | 89.5 | 94.6 |
| 31-40 | no gear | proto_greatsword (level 38) | hydro_press x2 > vibro_hone > hydro_press > rebond > ramp_plasma > vibro_hone x7 > ramp_plasma > cryo_quench > vibro_hone | 88.1 | 23.4 | 75.3 |
| 31-40 | no gear | majestic_ring001 (level 40) | rebond > vibro_hone > ramp_plasma > hydro_press > vibro_hone > ramp_plasma x2 > hydro_press > cryo_quench > hydro_press > cryo_quench > vibro_hone x2 | 55.2 | 29.1 | 55.6 |
| 31-40 | no gear | chef_cloak_4 (level 40) | hydro_press > ramp_plasma > hydro_press x2 > vibro_hone > ramp_plasma > hydro_press > cryo_quench > hydro_press > vibro_hone > ramp_plasma x2 > cryo_quench > vibro_hone > rebond > vibro_hone | 90.7 | 70.3 | 81.5 |
| 31-40 | mid gear | sh_charm (level 31) | hydro_press > cryo_quench > vibro_hone x3 > ramp_plasma > vibro_hone x2 > rebond > ramp_plasma > vibro_hone > ramp_plasma > rebond > vibro_hone > ramp_plasma > hydro_press x2 > cryo_quench > vibro_hone > hydro_press x2 > vibro_hone | 96.7 | 33.9 | 91.6 |
| 31-40 | mid gear | prm_bul_cloak (level 33) | hydro_press > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > hydro_press > cryo_quench > hydro_press > cryo_quench > vibro_hone > rebond > vibro_hone x4 | 99.8 | 96.8 | 95.0 |
| 31-40 | mid gear | biolab_belt (level 34) | vibro_hone x2 > hydro_press > ramp_plasma > hydro_press x3 > ramp_plasma > cryo_quench > vibro_hone > hydro_press > ramp_plasma > cryo_quench > rebond x2 > vibro_hone x2 | 99.2 | 94.6 | 97.3 |
| 31-40 | mid gear | proto_greatsword (level 38) | hydro_press > vibro_hone x7 > ramp_plasma > vibro_hone > rebond > vibro_hone > ramp_plasma > hydro_press > vibro_hone x3 > rebond > cryo_quench > hydro_press > cryo_quench > vibro_hone x3 | 100.0 | 21.7 | 92.2 |
| 31-40 | mid gear | majestic_ring001 (level 40) | rebond > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > vibro_hone > ramp_plasma x2 > hydro_press x2 > cryo_quench > hydro_press > cryo_quench > hydro_press x2 > vibro_hone x2 | 73.1 | 40.9 | 75.8 |
| 31-40 | mid gear | chef_cloak_4 (level 40) | ramp_plasma > hydro_press x2 > vibro_hone x3 > ramp_plasma x2 > hydro_press > cryo_quench > vibro_hone > hydro_press > cryo_quench > ramp_plasma x2 > hydro_press > cryo_quench > rebond > vibro_hone x2 | 95.9 | 95.9 | 95.8 |
| 31-40 | best gear | sh_charm (level 31) | hydro_press > vibro_hone x3 > ramp_plasma > vibro_hone x6 > rebond > ramp_plasma x2 > vibro_hone x2 > ramp_plasma x2 > hydro_press | 97.6 | 40.1 | 98.6 |
| 31-40 | best gear | prm_bul_cloak (level 33) | hydro_press > vibro_hone x2 > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > hydro_press > ramp_plasma > hydro_press | 88.7 | 81.2 | 98.3 |
| 31-40 | best gear | biolab_belt (level 34) | hydro_press > vibro_hone x3 > ramp_plasma > vibro_hone x2 > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > rebond > vibro_hone > hydro_press x2 | 100.0 | 82.7 | 99.9 |
| 31-40 | best gear | proto_greatsword (level 38) | hydro_press > vibro_hone x7 > ramp_plasma > vibro_hone x2 > rebond > vibro_hone > ramp_plasma > hydro_press > vibro_hone x2 > hydro_press | 100.0 | 38.5 | 94.4 |
| 31-40 | best gear | majestic_ring001 (level 40) | vibro_hone > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > hydro_press > ramp_plasma > cryo_quench > hydro_press > cryo_quench > vibro_hone x2 | 93.0 | 64.2 | 87.0 |
| 31-40 | best gear | chef_cloak_4 (level 40) | vibro_hone x2 > ramp_plasma > hydro_press > vibro_hone x2 > ramp_plasma > vibro_hone x2 > ramp_plasma x2 > hydro_press x2 > cryo_quench > ramp_plasma > rebond > hydro_press > vibro_hone x2 | 94.9 | 83.5 | 99.8 |
| 41-53 | no gear | asc_chn_tunic (level 42) | rebond > hydro_press > vibro_hone x2 > hydro_press x2 > vibro_hone > ramp_plasma > rebond > vibro_hone > hydro_press > vibro_hone > ramp_plasma x2 > vibro_hone > hydro_press x4 > vibro_hone > hydro_press | 97.3 | 79.2 | 99.9 |
| 41-53 | no gear | sorting_harness (level 45) | rebond > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > hydro_press x3 > cryo_quench > vibro_hone > hydro_press > cryo_quench > hydro_press > cryo_quench > hydro_press > rebond > cryo_quench > vibro_hone x3 | 98.9 | 76.5 | 96.4 |
| 41-53 | no gear | engi_belt_5 (level 50) | hydro_press x2 > cryo_quench > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > hydro_press > cryo_quench > vibro_hone > cryo_quench > hydro_press > ramp_plasma x2 > cryo_quench > hydro_press x2 > cryo_quench > vibro_hone x3 | 95.6 | 82.9 | 76.7 |
| 41-53 | no gear | fang_gaunt (level 50) | rebond > vibro_hone > ramp_plasma > hydro_press x2 > vibro_hone > ramp_plasma > hydro_press > rebond > hydro_press > cryo_quench > hydro_press > cryo_quench > hydro_press > vibro_hone > cryo_quench > vibro_hone x3 | 92.9 | 92.7 | 96.9 |
| 41-53 | no gear | chi_greatsword (level 52) | hydro_press x2 > vibro_hone x2 > ramp_plasma > hydro_press > vibro_hone x2 > cryo_quench > vibro_hone | 68.6 | 17.9 | 62.8 |
| 41-53 | no gear | imm_ring (level 52) | hydro_press > vibro_hone > ramp_plasma > vibro_hone x2 > ramp_plasma x2 > hydro_press x2 > cryo_quench > hydro_press > cryo_quench > vibro_hone x4 | 54.9 | 43.1 | 61.6 |
| 41-53 | mid gear | asc_chn_tunic (level 42) | vibro_hone x3 > ramp_plasma > rebond > vibro_hone x2 > ramp_plasma > hydro_press > vibro_hone > hydro_press x3 > vibro_hone > hydro_press x3 | 100.0 | 90.5 | 100.0 |
| 41-53 | mid gear | sorting_harness (level 45) | vibro_hone > hydro_press > ramp_plasma > hydro_press > vibro_hone x2 > hydro_press x2 > ramp_plasma > vibro_hone > cryo_quench > hydro_press > ramp_plasma > cryo_quench > rebond > vibro_hone x3 | 97.4 | 91.3 | 98.7 |
| 41-53 | mid gear | engi_belt_5 (level 50) | vibro_hone > hydro_press > vibro_hone > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > hydro_press > cryo_quench > vibro_hone > ramp_plasma > hydro_press > cryo_quench > hydro_press > cryo_quench > rebond > vibro_hone x4 | 95.9 | 81.4 | 93.0 |
| 41-53 | mid gear | fang_gaunt (level 50) | hydro_press x2 > ramp_plasma > hydro_press x2 > cryo_quench > vibro_hone > hydro_press > ramp_plasma > cryo_quench > rebond > vibro_hone x2 | 100.0 | 96.4 | 99.7 |
| 41-53 | mid gear | chi_greatsword (level 52) | hydro_press > vibro_hone x4 > hydro_press > ramp_plasma > vibro_hone x3 > ramp_plasma > hydro_press > ramp_plasma > rebond > cryo_quench > hydro_press > vibro_hone > cryo_quench > vibro_hone x2 | 89.8 | 15.2 | 69.8 |
| 41-53 | mid gear | imm_ring (level 52) | hydro_press > vibro_hone > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma x2 > hydro_press x2 > cryo_quench > hydro_press > cryo_quench > hydro_press x2 > vibro_hone x3 | 77.1 | 46.2 | 73.5 |
| 41-53 | best gear | asc_chn_tunic (level 42) | vibro_hone x4 > ramp_plasma > hydro_press x2 > vibro_hone > hydro_press > rebond > vibro_hone > hydro_press x4 | 100.0 | 91.3 | 100.0 |
| 41-53 | best gear | sorting_harness (level 45) | vibro_hone x4 > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > vibro_hone > ramp_plasma > hydro_press > rebond > vibro_hone > ramp_plasma > vibro_hone > hydro_press x4 | 100.0 | 79.4 | 99.9 |
| 41-53 | best gear | engi_belt_5 (level 50) | hydro_press > vibro_hone x2 > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma > hydro_press > vibro_hone > ramp_plasma > hydro_press > ramp_plasma > hydro_press | 88.1 | 67.7 | 96.4 |
| 41-53 | best gear | fang_gaunt (level 50) | hydro_press > vibro_hone x2 > rebond > ramp_plasma > vibro_hone > ramp_plasma > vibro_hone > hydro_press > ramp_plasma > vibro_hone > hydro_press > rebond > hydro_press > cryo_quench > hydro_press > cryo_quench > vibro_hone x3 | 100.0 | 100.0 | 99.8 |
| 41-53 | best gear | chi_greatsword (level 52) | vibro_hone x5 > ramp_plasma > hydro_press > vibro_hone x3 > ramp_plasma > hydro_press > cryo_quench > ramp_plasma > rebond > vibro_hone x2 > hydro_press x2 | 92.2 | 36.1 | 79.3 |
| 41-53 | best gear | imm_ring (level 52) | hydro_press > vibro_hone > ramp_plasma > vibro_hone x2 > ramp_plasma > vibro_hone > ramp_plasma x2 > hydro_press > cryo_quench > vibro_hone > hydro_press > ramp_plasma > cryo_quench > hydro_press > cryo_quench > vibro_hone x2 | 89.6 | 41.7 | 81.8 |

## Perks optional

### Rank 50, best gear, no perks: adaptive attempts that reach maximum quality, by recipe band

| Band | Recipes played | Attempts | Reach maximum quality | Make the item | Avg quality of items made | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 6 | 600 | 86% | 100% | 99% | >= 50% | Passed |
| 46-50 | 6 | 600 | 83% | 100% | 98% | >= 50% | Passed |
| 51-53 | 6 | 600 | 57% | 99% | 91% | >= 50% | Passed |

## Length

### Actions per attempt of the adaptive player at level (item craft at level)

| Band | Gear | Recipes played | Mean actions per attempt | Shortest recipe | Longest recipe | Stalled attempts | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 11.9 | 11.4 | 12.9 | 0 | 10-20 | Passed |
| 1-10 | mid gear | 6 | 13.5 | 11.9 | 17.4 | 0 | 10-20 | Passed |
| 1-10 | best gear | 6 | 12.2 | 10.9 | 14.9 | 0 | 10-20 | Passed |
| 11-20 | no gear | 6 | 11.7 | 10.1 | 15.1 | 0 | 10-20 | Passed |
| 11-20 | mid gear | 6 | 13.8 | 9.5 | 17.9 | 0 | 10-20 | Passed |
| 11-20 | best gear | 6 | 12.9 | 8.3 | 16.9 | 0 | 10-20 | Passed |
| 21-30 | no gear | 6 | 11.5 | 9.7 | 13.1 | 0 | 10-20 | Passed |
| 21-30 | mid gear | 6 | 11.1 | 9.7 | 12.2 | 0 | 10-20 | Passed |
| 21-30 | best gear | 6 | 10.2 | 8.9 | 11.4 | 0 | 10-20 | Passed |
| 31-40 | no gear | 6 | 11.4 | 9.5 | 12.9 | 0 | 10-20 | Passed |
| 31-40 | mid gear | 6 | 11.7 | 10.4 | 12.7 | 0 | 10-20 | Passed |
| 31-40 | best gear | 6 | 10.1 | 9.4 | 11.3 | 0 | 10-20 | Passed |
| 41-53 | no gear | 6 | 11.7 | 9.6 | 12.7 | 0 | 10-20 | Passed |
| 41-53 | mid gear | 6 | 11.7 | 9.4 | 12.7 | 0 | 10-20 | Passed |
| 41-53 | best gear | 6 | 10.6 | 8.7 | 11.8 | 0 | 10-20 | Passed |

## Economy parity

### Component stacks consumed per item made, failures included, over the recipes the legacy policy makes at least once in the cell (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Parity rule | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 2.82 | 2.70 | 1.05 | 92% | 98% | 62% | 1% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | mid gear | 6 | 2.73 | 2.73 | 1.00 | 97% | 96% | 89% | 11% | yes | 0% / 0% | within 10% | Passed |
| 1-10 | best gear | 6 | 2.67 | 2.70 | 0.99 | 100% | 98% | 98% | 28% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | no gear | 6 | 2.66 | 2.70 | 0.98 | 92% | 89% | 63% | 2% | yes | 0% / 0% | at most +10% | Passed |
| 11-20 | mid gear | 6 | 2.54 | 2.53 | 1.00 | 98% | 98% | 90% | 13% | yes | 0% / 0% | within 10% | Passed |
| 11-20 | best gear | 6 | 2.54 | 2.51 | 1.01 | 98% | 100% | 96% | 29% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | no gear | 6 | 3.35 | 3.37 | 0.99 | 99% | 98% | 82% | 7% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | mid gear | 6 | 3.33 | 3.35 | 1.00 | 100% | 99% | 92% | 15% | yes | 0% / 0% | within 10% | Passed |
| 21-30 | best gear | 6 | 3.33 | 3.35 | 0.99 | 100% | 99% | 96% | 28% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | no gear | 6 | 3.06 | 3.07 | 1.00 | 97% | 97% | 77% | 22% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | mid gear | 6 | 3.02 | 3.01 | 1.00 | 99% | 99% | 91% | 38% | yes | 0% / 0% | within 10% | Passed |
| 31-40 | best gear | 6 | 3.00 | 3.02 | 0.99 | 100% | 99% | 96% | 50% | yes | 0% / 0% | within 10% | Passed |
| 41-53 | no gear | 6 | 3.01 | 3.03 | 1.00 | 99% | 98% | 93% | 43% | yes | 0% / 33% | within 10% | Passed |
| 41-53 | mid gear | 6 | 4.70 | 5.35 | 0.88 | 99% | 73% | 89% | 46% | yes | 0% / 0% | at most +10%; uses 12% less, passes | Passed |
| 41-53 | best gear | 6 | 4.67 | 4.90 | 0.95 | 100% | 89% | 93% | 61% | yes | 0% / 0% | at most +10% | Passed |

### Recipes the legacy policy never makes in the cell, left out of the stack and quality comparison above (they still count in the never-made check)

| Band | Gear | Recipe | Level | New success | New avg quality |
| --- | --- | --- | --- | --- | --- |
| 41-53 | no gear | ChiroGreatSword | 52 | 100% | 63% |
| 41-53 | no gear | ImmortalRing | 52 | 96% | 62% |

## XP rate

### Item crafts: XP per hour against the legacy loop (6.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per craft | Legacy actions per craft | Legacy XP per hour | New XP per craft | New actions per craft | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 596 | 5.8 | 61154 | 1830 | 11.9 | 68989 | 1.13 | - | Passed |
| 1-10 | no gear, 1 enhancement | 458 | 6.9 | 39901 | 1549 | 11.6 | 60248 | 1.51 | - | Passed |
| 1-10 | no gear, 2 enhancements | 305 | 6.6 | 27880 | 1010 | 11.3 | 40179 | 1.44 | - | Passed |
| 1-10 | no gear, 3 enhancements | 237 | 6.9 | 20583 | 650 | 11.3 | 25912 | 1.26 | - | Passed |
| 1-10 | mid gear | 649 | 8.9 | 43928 | 2202 | 13.5 | 73568 | 1.67 | - | Passed |
| 1-10 | mid gear, 1 enhancement | 646 | 8.8 | 44075 | 1891 | 13.5 | 62896 | 1.43 | - | Passed |
| 1-10 | mid gear, 2 enhancements | 596 | 9.6 | 37325 | 1366 | 13.2 | 46504 | 1.25 | - | Passed |
| 1-10 | mid gear, 3 enhancements | 548 | 10.1 | 32667 | 965 | 13.1 | 33051 | 1.01 | - | Passed |
| 1-10 | best gear | 758 | 10.3 | 44072 | 2334 | 12.2 | 86198 | 1.96 | - | Passed |
| 1-10 | best gear, 1 enhancement | 745 | 10.9 | 41214 | 2183 | 13.4 | 73156 | 1.78 | - | Passed |
| 1-10 | best gear, 2 enhancements | 733 | 11.0 | 39984 | 1925 | 14.2 | 60896 | 1.52 | - | Passed |
| 1-10 | best gear, 3 enhancements | 703 | 11.1 | 37811 | 1461 | 14.7 | 44792 | 1.18 | - | Passed |
| 11-20 | no gear | 556 | 7.8 | 42813 | 1122 | 11.7 | 43030 | 1.01 | - | Passed |
| 11-20 | no gear, 1 enhancement | 415 | 8.1 | 30917 | 1039 | 11.9 | 39314 | 1.27 | - | Passed |
| 11-20 | no gear, 2 enhancements | 260 | 7.1 | 21850 | 889 | 12.0 | 33405 | 1.53 | - | Passed |
| 11-20 | no gear, 3 enhancements | 261 | 7.7 | 20290 | 633 | 12.2 | 23254 | 1.15 | - | Passed |
| 11-20 | mid gear | 668 | 11.0 | 36437 | 1354 | 13.8 | 44013 | 1.21 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 651 | 11.8 | 32992 | 1296 | 15.0 | 39012 | 1.18 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 626 | 12.6 | 29817 | 1139 | 15.6 | 32864 | 1.10 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 611 | 13.5 | 27091 | 1018 | 15.9 | 28900 | 1.07 | - | Passed |
| 11-20 | best gear | 771 | 14.5 | 31867 | 1395 | 12.9 | 48801 | 1.53 | - | Passed |
| 11-20 | best gear, 1 enhancement | 761 | 15.1 | 30267 | 1371 | 14.3 | 43085 | 1.42 | - | Passed |
| 11-20 | best gear, 2 enhancements | 743 | 16.0 | 27845 | 1330 | 15.8 | 37796 | 1.36 | - | Passed |
| 11-20 | best gear, 3 enhancements | 744 | 16.5 | 27046 | 1253 | 16.9 | 33379 | 1.23 | - | Passed |
| 21-30 | no gear | 636 | 10.2 | 37567 | 1592 | 11.5 | 62178 | 1.66 | - | Passed |
| 21-30 | no gear, 1 enhancement | 614 | 11.5 | 32122 | 1507 | 12.1 | 55839 | 1.74 | - | Passed |
| 21-30 | no gear, 2 enhancements | 601 | 12.5 | 28780 | 1266 | 12.9 | 44253 | 1.54 | - | Passed |
| 21-30 | no gear, 3 enhancements | 575 | 13.4 | 25748 | 767 | 13.2 | 26058 | 1.01 | - | Passed |
| 21-30 | mid gear | 688 | 16.4 | 25238 | 1677 | 11.1 | 68267 | 2.70 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 676 | 16.7 | 24276 | 1655 | 12.7 | 58557 | 2.41 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 666 | 17.3 | 23121 | 1613 | 13.8 | 52599 | 2.27 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 645 | 17.5 | 22081 | 1401 | 15.5 | 40786 | 1.85 | - | Passed |
| 21-30 | best gear | 766 | 21.3 | 21616 | 1716 | 10.2 | 75487 | 3.49 | - | Passed |
| 21-30 | best gear, 1 enhancement | 757 | 21.6 | 21048 | 1719 | 11.8 | 65624 | 3.12 | - | Passed |
| 21-30 | best gear, 2 enhancements | 748 | 22.0 | 20379 | 1699 | 13.0 | 58786 | 2.88 | - | Passed |
| 21-30 | best gear, 3 enhancements | 743 | 22.1 | 20179 | 1653 | 14.4 | 51631 | 2.56 | - | Passed |
| 31-40 | no gear | 717 | 15.1 | 28426 | 2153 | 11.4 | 84744 | 2.98 | - | Passed |
| 31-40 | no gear, 1 enhancement | 667 | 15.3 | 26143 | 1900 | 12.5 | 68646 | 2.63 | - | Passed |
| 31-40 | no gear, 2 enhancements | 641 | 14.8 | 25935 | 1196 | 12.8 | 42025 | 1.62 | - | Passed |
| 31-40 | no gear, 3 enhancements | 602 | 16.0 | 22641 | 701 | 13.8 | 22812 | 1.01 | - | Passed |
| 31-40 | mid gear | 824 | 20.7 | 23873 | 2342 | 11.7 | 89999 | 3.77 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 792 | 21.0 | 22644 | 2261 | 13.1 | 77632 | 3.43 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 750 | 20.5 | 21918 | 1676 | 13.7 | 55125 | 2.52 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 711 | 20.1 | 21265 | 1136 | 14.2 | 35975 | 1.69 | - | Passed |
| 31-40 | best gear | 892 | 25.4 | 21100 | 2413 | 10.1 | 107757 | 5.11 | - | Passed |
| 31-40 | best gear, 1 enhancement | 887 | 26.0 | 20442 | 2334 | 12.4 | 84455 | 4.13 | - | Passed |
| 31-40 | best gear, 2 enhancements | 862 | 26.1 | 19776 | 2310 | 13.6 | 76173 | 3.85 | - | Passed |
| 31-40 | best gear, 3 enhancements | 831 | 24.8 | 20137 | 1826 | 14.7 | 55959 | 2.78 | - | Passed |
| 41-53 | no gear | 605 | 15.8 | 22974 | 1274 | 11.7 | 49106 | 2.14 | - | Passed |
| 41-53 | no gear, 1 enhancement | 553 | 14.4 | 23054 | 1211 | 12.5 | 43725 | 1.90 | - | Passed |
| 41-53 | no gear, 2 enhancements | 495 | 14.5 | 20431 | 862 | 13.1 | 29646 | 1.45 | - | Passed |
| 41-53 | no gear, 3 enhancements | 445 | 14.8 | 17963 | 522 | 13.1 | 17965 | 1.00 | - | Passed |
| 41-53 | mid gear | 757 | 23.0 | 19709 | 1324 | 11.7 | 50883 | 2.58 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 686 | 21.2 | 19431 | 1315 | 12.3 | 47997 | 2.47 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 611 | 18.3 | 19987 | 1208 | 13.8 | 39518 | 1.98 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 568 | 17.3 | 19735 | 911 | 14.4 | 28411 | 1.44 | - | Passed |
| 41-53 | best gear | 947 | 27.5 | 20638 | 1359 | 10.6 | 57702 | 2.80 | - | Passed |
| 41-53 | best gear, 1 enhancement | 900 | 27.4 | 19733 | 1371 | 11.8 | 52406 | 2.66 | - | Passed |
| 41-53 | best gear, 2 enhancements | 776 | 24.2 | 19276 | 1362 | 12.7 | 48191 | 2.50 | - | Passed |
| 41-53 | best gear, 3 enhancements | 715 | 21.7 | 19802 | 1231 | 13.9 | 39829 | 2.01 | - | Passed |

## Enhancement parity

### Rank 50, best gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 85%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 6 | 60-70 | 33% | 100% | 95% | 0.94 | 100% | 98.8% | 0.99 (+/- 0.00) | 1.05 | >= 1.00 | Passed |
| 41-45 | 2 | 6 | 60-70 | 65% | 100% | 89% | 1.78 | 100% | 98.7% | 1.96 (+/- 0.01) | 1.10 | >= 1.00 | Passed |
| 41-45 | 3 | 6 | 60-70 | 98% | 100% | 83% | 2.48 | 97% | 97.3% | 2.77 (+/- 0.03) | 1.12 | >= 1.00 | Passed |
| 46-50 | 1 | 6 | 60 | 32% | 100% | 81% | 0.81 | 99% | 97.6% | 0.98 (+/- 0.01) | 1.22 | >= 1.00 | Passed |
| 46-50 | 2 | 6 | 60 | 64% | 98% | 70% | 1.36 | 99% | 95.2% | 1.84 (+/- 0.02) | 1.35 | >= 1.00 | Passed |
| 46-50 | 3 | 6 | 60 | 96% | 99% | 62% | 1.82 | 93% | 87.3% | 2.25 (+/- 0.05) | 1.24 | >= 1.00 | Passed |
| 51-53 | 1 | 6 | 60-70 | 8% | 55% | 6% | 0.03 | 98% | 89.3% | 0.98 (+/- 0.01) | 29.62 | >= 1.00 | Passed |
| 51-53 | 2 | 6 | 60-70 | 17% | 29% | 5% | 0.03 | 98% | 88.4% | 1.62 (+/- 0.02) | 60.30 | >= 1.00 | Passed |
| 51-53 | 3 | 6 | 60-70 | 25% | 12% | 4% | 0.01 | 99% | 88.1% | 2.31 (+/- 0.04) | 168.37 | >= 1.00 | Passed |

### Thresholds, first / second / third slot, that would meet parity for the quality measured (the strictest set that keeps every case at least at today's, never falling from one slot to the next)

| Scope | Thresholds that meet parity | In steps of 5 | Thresholds in force |
| --- | --- | --- | --- |
| Every band together (the shared thresholds) | 92% / 100% / 100% | 90% / 100% / 100% | 50% / 85% / 85% |
| Band 41-45 alone | 92% / 100% / 100% | 90% / 100% / 100% | 50% / 85% / 85% |
| Band 46-50 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 85% |
| Band 51-53 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 85% |

### Rank 50, best gear: quality of the items the new minigame made, in whole percent, by band and slots filled

| Band | Slots filled | Items made | 10th percentile | 25th percentile | Median | 75th percentile | 90th percentile | Made at 100% |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 100% | 99% | 100% | 100% | 100% | 100% | 88% |
| 41-45 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 90% |
| 41-45 | 3 | 97% | 98% | 100% | 100% | 100% | 100% | 86% |
| 46-50 | 1 | 99% | 95% | 100% | 100% | 100% | 100% | 85% |
| 46-50 | 2 | 99% | 89% | 100% | 100% | 100% | 100% | 82% |
| 46-50 | 3 | 93% | 43% | 90% | 100% | 100% | 100% | 66% |
| 51-53 | 1 | 98% | 69% | 78% | 100% | 100% | 100% | 53% |
| 51-53 | 2 | 98% | 64% | 77% | 100% | 100% | 100% | 50% |
| 51-53 | 3 | 99% | 66% | 79% | 99% | 100% | 100% | 48% |

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
- No perks or styles exist yet; Hot Forging and Cold Working are drafts, so the style-balance gate is not applicable.
- The seconds per action (8) is an estimate for a window nobody has played; client timing replaces it.
- The adaptive player reads only the shared meters and previews, so it plans Thermal worse than a person who reads the range; gate 1 compares it with a solver that searches whole rotations.

## Appendix: tuning evidence (hand-written; the command above rewrites everything before this heading)

### Result by gate

Seed 1, 100 runs per case, 6 recipes per band, the default fixed-rotation search, 8 s per action in the Forge and 6 s in the old window (both unmeasured estimates).

| Gate | Result | Where it stands |
| --- | --- | --- |
| 1 No single rotation | Passed in all 15 band and gear cases (margins 12.0 to 45.3 points against the best single rotation, 10 required) | The smallest is 12.0 in 1-10 / best gear; see "Gate 1" below for how far to trust it. |
| 2 Style balance | Not applicable | No styles until the perk pull request. |
| 3 Perks optional | Passed (57% to 86% reach maximum quality at rank 50 with best gear, 50% required) | The 51-53 recipes are the tightest. |
| 4 Length | Passed (10.1 to 13.8 actions at level in every band and gear tier) | Best-gear crafts in 21-30 and 31-40 sit at 10.1 to 10.2. |
| 5 Material parity | Passed in all 15 cases (ratios 0.88 to 1.05) | One case uses more than 10% less material than today, which passes. |
| 6 XP rate | Passed in every band, gear tier and enhancement count (0 to 3), lowest 1.00x, with per-band multipliers 1.97 / 1.20 / 1.46 / 2.05 / 1.09 | The multipliers are an owner decision; see "Gate 6" below. |
| 7 Enhancement parity | Passed (1.05 to 168 times today's expected enhancements) | The shared thresholds are untouched; see "Gate 7" below. |

### What changed since the first tuning

The first tuning (pull request 2815 and the earlier commits of this one) passed gates 3 to 7 but failed gate 1: with every recipe of a band playing alike, one sequence of actions scored 99.9 to 100.0 on every band, 0.0 to 0.3 points ahead of adaptive play. The cause was generosity and similarity, not randomness: Integrity was cheap, leaving the working range cost little, Craft points were plentiful at every level, and the recipes of one band had nearly the same range, cooling and start. The values changed this way (the engine values are in `ForgeRules`, the recipe spread in `ForgeParameterProfile`):

| Value | Before | Now | Why |
| --- | --- | --- | --- |
| Hone Integrity | 4 | 8 | Integrity is the budget every sequence must fit; with it tight, a sequence tuned for one recipe breaks or under-finishes another. |
| Press Integrity in range / below / above | 5 / 7 / 10 | 10 / 12 / 20 | Same, and leaving the range costs more. |
| Ramp Plasma Integrity, Cryo-Quench Integrity | 2, 4 | 3, 6 | Same. |
| Press Shape below the range, Hone Finish outside it | 0.25, 0.35 | 0.10, 0.10 | A sequence that loses the range at the wrong action gets almost nothing. |
| Hydro-Press base Shape | 170 | 190 | Keeps crafts at 10 to 14 actions with the dearer Integrity. |
| Vibro-Hone base Finish | 140 | 90 | Finish needs more honing, so Integrity decides how much of it a craft can afford. |
| Cryo-Quench base Finish, edge of the range | 360, 0.5 | 600, 0.3 | The quench carries most of the Finish, and only a centered quench earns it: when to quench is a recipe-specific decision. |
| Post-quench Hone Shape | 72 | 48 | Lengthens the trim after the quench (gate 4). |
| Ramp Plasma Thermal | +25 | +22 | A ramp no longer overshoots a narrow range as easily. |
| Ramp Plasma CP | 5 | 3 + 0.35 per recipe level | CP stays scarce at every level, including above rank 25 where the passive adds 31. |
| Rebond CP | 8 | 6 + 0.4 per recipe level | Same. |
| Recipe spread | one formula by level and category | curves by level band, plus a repeatable offset from each recipe's name | See the profile. Range width, cooling (light goods cool faster than heavy ones), starting Thermal and range center differ within a band, the Shape share runs 25 to 75, and some recipes start outside their range. |
| Adaptive player (scenario) | depth 3, success bonus 0.85, margin 3.0 | depth 4, 6000-copy budget, success bonus 0.75, margin 2.5 | Tight Integrity needs a player that plans a few actions ahead. |
| XP multipliers | 1.55 / 1.09 / 1.15 / 1.07 / 1.0 | 1.97 / 1.20 / 1.46 / 2.05 / 1.09 | Re-derived for the new crafts (gate 6). |
| Unchanged | | | Stable Containment (35%, +50%), the unlock ranks (Rebond 10, Cryo-Quench 20, the rest 0), the 80% quench threshold, Rebond (+25 Integrity, -12 Thermal), the post-quench Finish (84). |

### Gate 1: how it passes and how far to trust it

Single-rotation margins (adaptive average quality minus the best single rotation's, points; none / mid / best gear):

| Band | Margin | Adaptive | Best single rotation |
| --- | --- | --- | --- |
| 1-10 | 13.2 / 21.6 / 12.1 | 62.0 / 89.1 / 97.8 | 48.8 / 67.5 / 85.7 |
| 11-20 | 25.3 / 24.0 / 20.1 | 63.0 / 89.9 / 95.7 | 37.7 / 65.9 / 75.6 |
| 21-30 | 35.4 / 35.8 / 45.3 | 82.3 / 91.5 / 96.0 | 46.9 / 55.7 / 50.7 |
| 31-40 | 22.6 / 27.3 / 31.2 | 77.5 / 91.3 / 96.3 | 54.9 / 64.0 / 65.1 |
| 41-53 | 17.0 / 18.9 / 23.5 | 82.4 / 89.1 / 92.9 | 65.4 / 70.2 / 69.4 |

What to keep in mind:

- The rotation per recipe alone is still at most 2.1 points ahead of adaptive play and as much as 5.1 behind it (a reported metric, not gated): the Forge is deterministic apart from one telegraphed opportunity, so each recipe still has a sequence that matches a skilled player. What the gate asks, and what now fails, is that one sequence serves every recipe of a band.
- Quality without gear is low in the first two bands (62% and 63% on average): a crafter with 0 to 15 CP cannot ramp or rebond. The old minigame gives 1% to 28% there.
- The values were found by a search against this report's sample (6 recipes per band), with a smaller solver and fewer runs than the report. The report is the check, with the committed solver. The same profile and engine values were also played on other samples (4, 5, 7 and 8 recipes per band, the small solver, 30 runs); see "Other samples" below. The recipes the report plays are the evenly spaced ones of each band; recipes added later change the sample and can need the curves re-tuned.
- The adaptive player is a depth-4 search over the previews with a generic leaf heuristic. A stronger player can only widen the margins.

#### Other samples

The same engine and profile values on four other samples of each band (4, 5, 7 and 8 recipes per band; the small solver the search used, 30 runs; the lengths and the rank-50 perks check in the same runs):

| Recipes per band | Smallest margin (points) | Cells under 10 | Shortest length | Lowest rank-50 full-quality share |
| --- | --- | --- | --- | --- |
| 4 | 9.8 (1-10 / best gear) | 1 | 10.4 (21-30 / best gear) | 72% |
| 5 | 9.1 (1-10 / best gear) | 1 | 10.7 (41-53 / no gear) | 53% |
| 7 | 6.8 (1-10 / no gear) | 1 | 10.3 (21-30 and 41-53 / best gear) | 42% (51-53) |
| 8 | 7.3 (1-10 / mid gear) | 1 | 10.3 (21-30 / best gear) | 50% (51-53) |

Bands 11-20 to 41-53 keep margins of 11 points or more on every sample. Band 1-10 does not: with the recipes of a sample that differ less, one of its three gear tiers falls to 7 to 10 points. The 51-53 recipes (rank 50 against level 51 to 53) are the other tight spot: with seven recipes the full-quality share is 42%, under the 50% gate 3 asks for. Both depend on which recipes a sample picks, and the committed report's sample (6 per band) passes. Making the 1-10 band hold on every sample is the next tuning step if the owner wants it. A candidate for the 51-53 recipes (range width 30, cooling 4 and starting Thermal +12 at the level 53 end of the curves) lifts that sub-band's full-quality share by 7 to 13 points on the samples with 6, 7 and 8 recipes per band (65%, 49% and 63% at 6, 7 and 8 recipes per band) and keeps the margins at 19 points or more; it is not applied because gates 3 to 7 were verified on the committed values.

### Gate 5: matched recipes only

The comparison now covers the recipes the old minigame makes at least once in the cell (`MaterialParityGate`), and lists the ones it never makes in a second table. Before it, 41-53 / no gear compared 4.67 stacks per item for the Forge against 3.03 for the old minigame because two of the six recipes were counted on the Forge's side only; over the recipes both make, the ratio was 0.99. All 15 cases now pass; 41-53 / mid gear uses 12% less material than today, which the one-sided rule allows.

### Gate 6: the XP multipliers

At multiplier 1.0 the multiplier each band needs, over every gear tier and 0 to 3 enhancements, is 1.96 (1-10), 1.19 (11-20), 1.45 (21-30), 2.04 (31-40) and 1.09 (41-53). The binding cases are enhanced crafts without gear: the Forge turns an enhancement's progress penalty into extra Shape and so extra actions, where the old minigame's enhanced crafts stay short. Set to 1.97 / 1.20 / 1.46 / 2.05 / 1.09 (one hundredth above the figure, for rounding), every case passes at 1.00x or more. The cost: Smithery XP per hour is 1.00x to 5.1x the old rate, because the same multiplier also applies to crafts the old minigame makes slowly (best gear, no enhancements: 3.5x in 21-30 and 5.1x in 31-40). A multiplier by gear tier or enhancement count would be fairer; the shared mechanism takes the recipe level only. The seconds-per-action figures (8 and 6) are estimates; client timing will move every number in this gate.

### Gate 7: enhancement parity (reported for the shared thresholds; `CraftOutcomeRules` is not changed)

Rank 50, best gear, at level, expected enhancements transferred per enhanced craft (1 / 2 / 3 slots):

| Band | Today | Forge under 50% / 85% / 85% | Forge / today |
| --- | --- | --- | --- |
| 41-45 | 0.94 / 1.78 / 2.48 | 0.99 / 1.96 / 2.77 | 1.05 / 1.10 / 1.12 |
| 46-50 | 0.81 / 1.36 / 1.82 | 0.98 / 1.84 / 2.25 | 1.22 / 1.35 / 1.24 |
| 51-53 | 0.03 / 0.03 / 0.01 | 0.98 / 1.62 / 2.31 | 30 / 60 / 168 |

The thresholds gate 7 computes for the Forge, the strictest that keep every band at least at today's expected enhancements and never fall from one slot to the next, are 92% / 100% / 100% (90 / 100 / 100 in steps of 5). The thresholds in force pass the gate.
