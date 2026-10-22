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
- Adaptive player: depth 3, forecast samples per result 2, budget 4000 engine copies per decision, 0.5% of value lost per extra action, heuristic DefaultLeafHeuristic. Each attempt's dice come from its own stream of the seed.
- Fixed-rotation solver: beam 8, cycles up to 3, 8 adaptive lines as seeds, up to 40 actions, 64 training attempts per local-search round and 150 selection attempts for each of 12 finalists, local search from the best 3 candidate(s) for up to 3 round(s) each. The winner is chosen on separate attempts and then scored on a third set (hold-out) with 100 attempts.
- Legacy comparator (gates 5 and 6): the old minigame's lookahead player (`SWLOR.CraftSim baseline`), replayed here with the same seed on the same recipes, ranks and gear.
- XP multiplier: the new minigame's base XP is scaled by 1.55 (gate 6); the legacy loop's is not.
- Time: 8.0 s per action in the new window and 6.0 s in the old one, plus 0.0 s per craft in both. These are estimates; the client evidence replaces them.
- Thresholds (starting values, all in `GateThresholds`):
  - `RotationMarginPoints` = 10: gate 1: adaptive beats the best fixed rotation by at least this many quality points
  - `StyleMinimumWinShare` = 30%: gate 2: each style is the better choice on at least this share of recipes
  - `StyleMaximumMeanDifferencePoints` = 5: gate 2: neither style beats the other by more on average
  - `StyleTieMarginPoints` = 0.5: gate 2: a recipe within this many points is a tie, half a win each
  - `PerksOptionalRank` = 50: gate 3: rank of the unperked crafter
  - `PerksOptionalMinimumFullQualityShare` = 50%: gate 3: share of crafts that reach maximum quality, per at-level band
  - `PerksOptionalBands` = 41-45, 46-50, 51-53: gate 3: the at-level recipe bands
  - `MaterialTolerance` = 10%: gate 5: component stacks per item within this share of the legacy baseline
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
| No fixed rotation | Failed | A fixed rotation comes within 10 points of adaptive play in 15 case(s): 1-10 / no gear; 1-10 / mid gear; 1-10 / best gear; 11-20 / no gear; 11-20 / mid gear; 11-20 / best gear; 21-30 / no gear; 21-30 / mid gear; 21-30 / best gear; 31-40 / no gear; 31-40 / mid gear; 31-40 / best gear; 41-53 / no gear; 41-53 / mid gear; 41-53 / best gear (smallest margin -0.3 in 41-53 / best gear). |
| Style balance | NotApplicable | The scenario offers no style configurations yet, so there is nothing to compare. |
| Perks optional | Passed | An unperked rank-50 crafter in best gear reaches maximum quality on at least 50% of crafts in every at-level band (lowest 89%). |
| Length | Passed | Every band and gear tier takes 10-20 actions (item craft at level) on average. |
| Economy parity | Failed | Outside 10% of the legacy baseline, or below its quality: 41-53 / no gear (ratio 1.54); 41-53 / mid gear (ratio 0.87). |
| XP rate | Passed | Item crafts: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.00x in 1-10 / no gear). |
| Enhancement parity | Passed | At rank 50 in best gear, the thresholds in force (50% / 85% / 85%) transfer at least 1.00 times today's expected enhancements for 1, 2, 3 slot(s) in every band (lowest 1.06 times today's). Thresholds that meet parity in every band for this quality: 98% / 100% / 100%. |

## No fixed rotation

### Average quality: adaptive play against the best fixed rotation (quality points; a broken piece counts 0)

| Band | Gear | Recipes played | Adaptive avg quality | Best fixed avg quality | Margin | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 99.8 | 100.0 | -0.2 | >= 10 | Failed |
| 1-10 | mid gear | 6 | 99.9 | 100.0 | -0.1 | >= 10 | Failed |
| 1-10 | best gear | 6 | 99.8 | 100.0 | -0.1 | >= 10 | Failed |
| 11-20 | no gear | 6 | 99.9 | 100.0 | -0.1 | >= 10 | Failed |
| 11-20 | mid gear | 6 | 99.8 | 100.0 | -0.2 | >= 10 | Failed |
| 11-20 | best gear | 6 | 99.8 | 100.0 | -0.2 | >= 10 | Failed |
| 21-30 | no gear | 6 | 99.9 | 100.0 | -0.1 | >= 10 | Failed |
| 21-30 | mid gear | 6 | 99.9 | 100.0 | -0.1 | >= 10 | Failed |
| 21-30 | best gear | 6 | 99.8 | 100.0 | -0.2 | >= 10 | Failed |
| 31-40 | no gear | 6 | 99.8 | 100.0 | -0.2 | >= 10 | Failed |
| 31-40 | mid gear | 6 | 99.9 | 100.0 | -0.1 | >= 10 | Failed |
| 31-40 | best gear | 6 | 99.8 | 100.0 | -0.2 | >= 10 | Failed |
| 41-53 | no gear | 6 | 99.8 | 100.0 | -0.2 | >= 10 | Failed |
| 41-53 | mid gear | 6 | 99.8 | 100.0 | -0.2 | >= 10 | Failed |
| 41-53 | best gear | 6 | 99.7 | 100.0 | -0.3 | >= 10 | Failed |

### The best fixed rotation found for the first recipe played in each band and gear tier (average quality in points)

| Band | Gear | Recipe | Best fixed rotation | Training | Selection | Hold-out | Adaptive on this recipe |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | bm_bracer (level 3) | vibro_hone > hydro_press > vibro_hone x8 > hydro_press x3 > vibro_hone > hydro_press x2 | 100.0 | 100.0 | 100.0 | 99.9 |
| 1-10 | mid gear | bm_bracer (level 3) | vibro_hone x2 > hydro_press > vibro_hone x7 > hydro_press x5 | 100.0 | 100.0 | 100.0 | 99.9 |
| 1-10 | best gear | bm_bracer (level 3) | vibro_hone x4 > hydro_press > vibro_hone x4 > hydro_press x4 | 100.0 | 100.0 | 100.0 | 99.9 |
| 11-20 | no gear | val_necklace (level 12) | hydro_press > vibro_hone x9 > hydro_press x4 > vibro_hone > hydro_press | 100.0 | 99.9 | 99.8 | 99.6 |
| 11-20 | mid gear | val_necklace (level 12) | vibro_hone > hydro_press > vibro_hone x7 > hydro_press > vibro_hone > hydro_press x3 | 100.0 | 100.0 | 100.0 | 99.8 |
| 11-20 | best gear | val_necklace (level 12) | vibro_hone > hydro_press > vibro_hone x7 > hydro_press x3 | 100.0 | 100.0 | 100.0 | 99.5 |
| 21-30 | no gear | vet_twinelec (level 22) | vibro_hone > hydro_press > vibro_hone x6 > ramp_plasma > vibro_hone x3 > hydro_press x3 > vibro_hone > hydro_press x3 | 100.0 | 100.0 | 100.0 | 99.8 |
| 21-30 | mid gear | vet_twinelec (level 22) | hydro_press > vibro_hone x6 > ramp_plasma > vibro_hone x2 > hydro_press x3 > vibro_hone > hydro_press | 100.0 | 99.9 | 100.0 | 99.8 |
| 21-30 | best gear | vet_twinelec (level 22) | vibro_hone x7 > ramp_plasma > hydro_press x2 > vibro_hone > hydro_press x2 | 100.0 | 100.0 | 100.0 | 99.8 |
| 31-40 | no gear | sh_charm (level 31) | hydro_press > vibro_hone x7 > ramp_plasma > vibro_hone > hydro_press x4 > cryo_quench > vibro_hone x2 | 100.0 | 99.9 | 100.0 | 99.7 |
| 31-40 | mid gear | sh_charm (level 31) | vibro_hone > hydro_press > vibro_hone x6 > ramp_plasma > hydro_press x2 > vibro_hone > hydro_press x2 | 100.0 | 100.0 | 100.0 | 99.8 |
| 31-40 | best gear | sh_charm (level 31) | hydro_press > vibro_hone x7 > ramp_plasma > hydro_press x4 | 100.0 | 100.0 | 100.0 | 99.9 |
| 41-53 | no gear | asc_chn_tunic (level 42) | vibro_hone x6 > ramp_plasma > hydro_press x8 | 100.0 | 100.0 | 100.0 | 99.9 |
| 41-53 | mid gear | asc_chn_tunic (level 42) | vibro_hone > hydro_press > vibro_hone x4 > hydro_press x2 > vibro_hone > ramp_plasma > hydro_press x5 | 100.0 | 100.0 | 100.0 | 99.9 |
| 41-53 | best gear | asc_chn_tunic (level 42) | vibro_hone > hydro_press > vibro_hone x4 > hydro_press x3 > ramp_plasma > hydro_press x3 | 100.0 | 100.0 | 100.0 | 99.7 |

## Perks optional

### Rank 50, best gear, no perks: adaptive attempts that reach maximum quality, by recipe band

| Band | Recipes played | Attempts | Reach maximum quality | Make the item | Avg quality of items made | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 6 | 600 | 92% | 100% | 100% | >= 50% | Passed |
| 46-50 | 6 | 600 | 91% | 100% | 100% | >= 50% | Passed |
| 51-53 | 6 | 600 | 89% | 100% | 100% | >= 50% | Passed |

## Length

### Actions per attempt of the adaptive player at level (item craft at level)

| Band | Gear | Recipes played | Mean actions per attempt | Shortest recipe | Longest recipe | Stalled attempts | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 13.7 | 12.9 | 14.8 | 0 | 10-20 | Passed |
| 1-10 | mid gear | 6 | 12.4 | 11.6 | 13.3 | 0 | 10-20 | Passed |
| 1-10 | best gear | 6 | 10.8 | 10.1 | 11.9 | 0 | 10-20 | Passed |
| 11-20 | no gear | 6 | 13.7 | 13.0 | 14.2 | 0 | 10-20 | Passed |
| 11-20 | mid gear | 6 | 12.2 | 11.6 | 12.7 | 0 | 10-20 | Passed |
| 11-20 | best gear | 6 | 10.6 | 10.0 | 11.2 | 0 | 10-20 | Passed |
| 21-30 | no gear | 6 | 13.7 | 13.1 | 14.7 | 0 | 10-20 | Passed |
| 21-30 | mid gear | 6 | 12.1 | 11.6 | 13.1 | 0 | 10-20 | Passed |
| 21-30 | best gear | 6 | 10.7 | 10.2 | 11.5 | 0 | 10-20 | Passed |
| 31-40 | no gear | 6 | 14.3 | 13.4 | 15.2 | 0 | 10-20 | Passed |
| 31-40 | mid gear | 6 | 12.8 | 12.2 | 13.6 | 0 | 10-20 | Passed |
| 31-40 | best gear | 6 | 11.3 | 10.9 | 11.8 | 0 | 10-20 | Passed |
| 41-53 | no gear | 6 | 15.7 | 13.6 | 17.6 | 0 | 10-20 | Passed |
| 41-53 | mid gear | 6 | 14.0 | 12.4 | 15.7 | 0 | 10-20 | Passed |
| 41-53 | best gear | 6 | 12.2 | 10.7 | 13.8 | 0 | 10-20 | Passed |

## Economy parity

### Component stacks consumed per item made, failures included (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 6 | 2.67 | 2.70 | 0.99 | 100% | 98% | 100% | 1% | yes | 0% / 0% | Passed |
| 1-10 | mid gear | 6 | 2.67 | 2.73 | 0.98 | 100% | 96% | 100% | 11% | yes | 0% / 0% | Passed |
| 1-10 | best gear | 6 | 2.67 | 2.70 | 0.99 | 100% | 98% | 100% | 28% | yes | 0% / 0% | Passed |
| 11-20 | no gear | 6 | 2.50 | 2.70 | 0.92 | 100% | 89% | 100% | 2% | yes | 0% / 0% | Passed |
| 11-20 | mid gear | 6 | 2.50 | 2.53 | 0.99 | 100% | 98% | 100% | 13% | yes | 0% / 0% | Passed |
| 11-20 | best gear | 6 | 2.50 | 2.51 | 1.00 | 100% | 100% | 100% | 29% | yes | 0% / 0% | Passed |
| 21-30 | no gear | 6 | 3.33 | 3.37 | 0.99 | 100% | 98% | 100% | 7% | yes | 0% / 0% | Passed |
| 21-30 | mid gear | 6 | 3.33 | 3.35 | 1.00 | 100% | 99% | 100% | 15% | yes | 0% / 0% | Passed |
| 21-30 | best gear | 6 | 3.33 | 3.35 | 0.99 | 100% | 99% | 100% | 28% | yes | 0% / 0% | Passed |
| 31-40 | no gear | 6 | 3.00 | 3.07 | 0.98 | 100% | 97% | 100% | 22% | yes | 0% / 0% | Passed |
| 31-40 | mid gear | 6 | 3.00 | 3.01 | 1.00 | 100% | 99% | 100% | 38% | yes | 0% / 0% | Passed |
| 31-40 | best gear | 6 | 3.00 | 3.02 | 0.99 | 100% | 99% | 100% | 50% | yes | 0% / 0% | Passed |
| 41-53 | no gear | 6 | 4.67 | 3.03 | 1.54 | 100% | 66% | 100% | 29% | yes | 0% / 33% | Failed |
| 41-53 | mid gear | 6 | 4.67 | 5.35 | 0.87 | 100% | 73% | 100% | 46% | yes | 0% / 0% | Failed |
| 41-53 | best gear | 6 | 4.67 | 4.90 | 0.95 | 100% | 89% | 100% | 61% | yes | 0% / 0% | Passed |

## XP rate

### Item crafts: XP per hour against the legacy loop (6.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per craft | Legacy actions per craft | Legacy XP per hour | New XP per craft | New actions per craft | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 596 | 5.8 | 61154 | 1858 | 13.7 | 61212 | 1.00 | - | Passed |
| 1-10 | no gear, 1 enhancement | 458 | 6.9 | 39901 | 1859 | 15.6 | 53520 | 1.34 | - | Passed |
| 1-10 | no gear, 2 enhancements | 305 | 6.6 | 27880 | 1858 | 17.6 | 47601 | 1.71 | - | Passed |
| 1-10 | no gear, 3 enhancements | 237 | 6.9 | 20583 | 1817 | 19.2 | 42594 | 2.07 | - | Passed |
| 1-10 | mid gear | 649 | 8.9 | 43928 | 1859 | 12.4 | 67681 | 1.54 | - | Passed |
| 1-10 | mid gear, 1 enhancement | 646 | 8.8 | 44075 | 1859 | 14.3 | 58550 | 1.33 | - | Passed |
| 1-10 | mid gear, 2 enhancements | 596 | 9.6 | 37325 | 1858 | 16.1 | 51897 | 1.39 | - | Passed |
| 1-10 | mid gear, 3 enhancements | 548 | 10.1 | 32667 | 1850 | 18.3 | 45537 | 1.39 | - | Passed |
| 1-10 | best gear | 758 | 10.3 | 44072 | 1858 | 10.8 | 77519 | 1.76 | - | Passed |
| 1-10 | best gear, 1 enhancement | 745 | 10.9 | 41214 | 1859 | 12.5 | 66942 | 1.62 | - | Passed |
| 1-10 | best gear, 2 enhancements | 733 | 11.0 | 39984 | 1859 | 14.2 | 58769 | 1.47 | - | Passed |
| 1-10 | best gear, 3 enhancements | 703 | 11.1 | 37811 | 1859 | 15.9 | 52462 | 1.39 | - | Passed |
| 11-20 | no gear | 556 | 7.8 | 42813 | 1859 | 13.7 | 61242 | 1.43 | - | Passed |
| 11-20 | no gear, 1 enhancement | 415 | 8.1 | 30917 | 1798 | 16.5 | 49022 | 1.59 | - | Passed |
| 11-20 | no gear, 2 enhancements | 260 | 7.1 | 21850 | 1694 | 19.3 | 39581 | 1.81 | - | Passed |
| 11-20 | no gear, 3 enhancements | 261 | 7.7 | 20290 | 1665 | 20.8 | 35972 | 1.77 | - | Passed |
| 11-20 | mid gear | 668 | 11.0 | 36437 | 1858 | 12.2 | 68432 | 1.88 | - | Passed |
| 11-20 | mid gear, 1 enhancement | 651 | 11.8 | 32992 | 1859 | 14.6 | 57362 | 1.74 | - | Passed |
| 11-20 | mid gear, 2 enhancements | 626 | 12.6 | 29817 | 1859 | 16.9 | 49547 | 1.66 | - | Passed |
| 11-20 | mid gear, 3 enhancements | 611 | 13.5 | 27091 | 1842 | 19.8 | 41934 | 1.55 | - | Passed |
| 11-20 | best gear | 771 | 14.5 | 31867 | 1858 | 10.6 | 78561 | 2.47 | - | Passed |
| 11-20 | best gear, 1 enhancement | 761 | 15.1 | 30267 | 1859 | 12.6 | 66410 | 2.19 | - | Passed |
| 11-20 | best gear, 2 enhancements | 743 | 16.0 | 27845 | 1859 | 14.5 | 57514 | 2.07 | - | Passed |
| 11-20 | best gear, 3 enhancements | 744 | 16.5 | 27046 | 1859 | 16.8 | 49942 | 1.85 | - | Passed |
| 21-30 | no gear | 636 | 10.2 | 37567 | 1859 | 13.7 | 61202 | 1.63 | - | Passed |
| 21-30 | no gear, 1 enhancement | 614 | 11.5 | 32122 | 1832 | 16.8 | 48983 | 1.52 | - | Passed |
| 21-30 | no gear, 2 enhancements | 601 | 12.5 | 28780 | 1816 | 20.1 | 40609 | 1.41 | - | Passed |
| 21-30 | no gear, 3 enhancements | 575 | 13.4 | 25748 | 1784 | 23.1 | 34766 | 1.35 | - | Passed |
| 21-30 | mid gear | 688 | 16.4 | 25238 | 1859 | 12.1 | 68891 | 2.73 | - | Passed |
| 21-30 | mid gear, 1 enhancement | 676 | 16.7 | 24276 | 1859 | 14.7 | 56980 | 2.35 | - | Passed |
| 21-30 | mid gear, 2 enhancements | 666 | 17.3 | 23121 | 1859 | 17.8 | 47080 | 2.04 | - | Passed |
| 21-30 | mid gear, 3 enhancements | 645 | 17.5 | 22081 | 1856 | 21.0 | 39680 | 1.80 | - | Passed |
| 21-30 | best gear | 766 | 21.3 | 21616 | 1858 | 10.7 | 78440 | 3.63 | - | Passed |
| 21-30 | best gear, 1 enhancement | 757 | 21.6 | 21048 | 1859 | 12.8 | 65247 | 3.10 | - | Passed |
| 21-30 | best gear, 2 enhancements | 748 | 22.0 | 20379 | 1859 | 15.0 | 55838 | 2.74 | - | Passed |
| 21-30 | best gear, 3 enhancements | 743 | 22.1 | 20179 | 1859 | 17.9 | 46862 | 2.32 | - | Passed |
| 31-40 | no gear | 717 | 15.1 | 28426 | 1858 | 14.3 | 58427 | 2.06 | - | Passed |
| 31-40 | no gear, 1 enhancement | 667 | 15.3 | 26143 | 1858 | 17.4 | 48110 | 1.84 | - | Passed |
| 31-40 | no gear, 2 enhancements | 641 | 14.8 | 25935 | 1856 | 21.1 | 39576 | 1.53 | - | Passed |
| 31-40 | no gear, 3 enhancements | 602 | 16.0 | 22641 | 1829 | 24.8 | 33145 | 1.46 | - | Passed |
| 31-40 | mid gear | 824 | 20.7 | 23873 | 1859 | 12.8 | 65217 | 2.73 | - | Passed |
| 31-40 | mid gear, 1 enhancement | 792 | 21.0 | 22644 | 1859 | 15.4 | 54435 | 2.40 | - | Passed |
| 31-40 | mid gear, 2 enhancements | 750 | 20.5 | 21918 | 1859 | 18.5 | 45111 | 2.06 | - | Passed |
| 31-40 | mid gear, 3 enhancements | 711 | 20.1 | 21265 | 1859 | 21.5 | 38880 | 1.83 | - | Passed |
| 31-40 | best gear | 892 | 25.4 | 21100 | 1858 | 11.3 | 73959 | 3.51 | - | Passed |
| 31-40 | best gear, 1 enhancement | 887 | 26.0 | 20442 | 1859 | 13.2 | 63221 | 3.09 | - | Passed |
| 31-40 | best gear, 2 enhancements | 862 | 26.1 | 19776 | 1859 | 15.6 | 53678 | 2.71 | - | Passed |
| 31-40 | best gear, 3 enhancements | 831 | 24.8 | 20137 | 1859 | 18.5 | 45134 | 2.24 | - | Passed |
| 41-53 | no gear | 605 | 15.8 | 22974 | 2013 | 15.7 | 57769 | 2.51 | - | Passed |
| 41-53 | no gear, 1 enhancement | 553 | 14.4 | 23054 | 2014 | 18.0 | 50410 | 2.19 | - | Passed |
| 41-53 | no gear, 2 enhancements | 495 | 14.5 | 20431 | 2005 | 20.4 | 44180 | 2.16 | - | Passed |
| 41-53 | no gear, 3 enhancements | 445 | 14.8 | 17963 | 1987 | 23.1 | 38677 | 2.15 | - | Passed |
| 41-53 | mid gear | 757 | 23.0 | 19709 | 2013 | 14.0 | 64719 | 3.28 | - | Passed |
| 41-53 | mid gear, 1 enhancement | 686 | 21.2 | 19431 | 2014 | 15.8 | 57481 | 2.96 | - | Passed |
| 41-53 | mid gear, 2 enhancements | 611 | 18.3 | 19987 | 2013 | 17.9 | 50642 | 2.53 | - | Passed |
| 41-53 | mid gear, 3 enhancements | 568 | 17.3 | 19735 | 2013 | 20.2 | 44789 | 2.27 | - | Passed |
| 41-53 | best gear | 947 | 27.5 | 20638 | 2013 | 12.2 | 74416 | 3.61 | - | Passed |
| 41-53 | best gear, 1 enhancement | 900 | 27.4 | 19733 | 2014 | 13.7 | 66177 | 3.35 | - | Passed |
| 41-53 | best gear, 2 enhancements | 776 | 24.2 | 19276 | 2014 | 15.4 | 58867 | 3.05 | - | Passed |
| 41-53 | best gear, 3 enhancements | 715 | 21.7 | 19802 | 2014 | 17.2 | 52554 | 2.65 | - | Passed |

## Enhancement parity

### Rank 50, best gear, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force (50% / 85% / 85%)

| Band | Slots filled | Recipes | Penalty each | Extra progress | Today: items made | Today: avg quality | Today: enhancements transferred | New: items made | New: avg quality | New: enhancements transferred | New / today | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 6 | 60-70 | 33% | 100% | 95% | 0.94 | 100% | 99.8% | 1.00 (+/- 0.00) | 1.06 | >= 1.00 | Passed |
| 41-45 | 2 | 6 | 60-70 | 65% | 100% | 89% | 1.78 | 100% | 99.8% | 2.00 (+/- 0.00) | 1.13 | >= 1.00 | Passed |
| 41-45 | 3 | 6 | 60-70 | 98% | 100% | 83% | 2.48 | 100% | 99.8% | 3.00 (+/- 0.00) | 1.21 | >= 1.00 | Passed |
| 46-50 | 1 | 6 | 60 | 32% | 100% | 81% | 0.81 | 100% | 99.9% | 1.00 (+/- 0.00) | 1.24 | >= 1.00 | Passed |
| 46-50 | 2 | 6 | 60 | 64% | 98% | 70% | 1.36 | 100% | 99.8% | 2.00 (+/- 0.00) | 1.47 | >= 1.00 | Passed |
| 46-50 | 3 | 6 | 60 | 96% | 99% | 62% | 1.82 | 100% | 99.8% | 3.00 (+/- 0.00) | 1.65 | >= 1.00 | Passed |
| 51-53 | 1 | 6 | 60-70 | 8% | 55% | 6% | 0.03 | 100% | 99.7% | 1.00 (+/- 0.00) | 30.27 | >= 1.00 | Passed |
| 51-53 | 2 | 6 | 60-70 | 17% | 29% | 5% | 0.03 | 100% | 99.7% | 2.00 (+/- 0.00) | 74.44 | >= 1.00 | Passed |
| 51-53 | 3 | 6 | 60-70 | 25% | 12% | 4% | 0.01 | 100% | 99.7% | 3.00 (+/- 0.00) | 218.98 | >= 1.00 | Passed |

### Thresholds, first / second / third slot, that would meet parity for the quality measured (the strictest set that keeps every case at least at today's, never falling from one slot to the next)

| Scope | Thresholds that meet parity | In steps of 5 | Thresholds in force |
| --- | --- | --- | --- |
| Every band together (the shared thresholds) | 98% / 100% / 100% | 95% / 100% / 100% | 50% / 85% / 85% |
| Band 41-45 alone | 98% / 100% / 100% | 95% / 100% / 100% | 50% / 85% / 85% |
| Band 46-50 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 85% |
| Band 51-53 alone | 100% / 100% / 100% | 100% / 100% / 100% | 50% / 85% / 85% |

### Rank 50, best gear: quality of the items the new minigame made, in whole percent, by band and slots filled

| Band | Slots filled | Items made | 10th percentile | 25th percentile | Median | 75th percentile | 90th percentile | Made at 100% |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 93% |
| 41-45 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 92% |
| 41-45 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 93% |
| 46-50 | 1 | 100% | 100% | 100% | 100% | 100% | 100% | 95% |
| 46-50 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 92% |
| 46-50 | 3 | 100% | 100% | 100% | 100% | 100% | 100% | 94% |
| 51-53 | 1 | 100% | 99% | 100% | 100% | 100% | 100% | 90% |
| 51-53 | 2 | 100% | 100% | 100% | 100% | 100% | 100% | 90% |
| 51-53 | 3 | 100% | 99% | 100% | 100% | 100% | 100% | 89% |

### Representative enhancements: for each recipe kind the real item at the best level the recipe accepts (the old window refuses one more than 5 levels above the recipe) whose penalty is the lower median of that level

| Recipe kind | Enhancement | Level | Progress penalty | Items of that kind and level (min / median / max penalty) | Recipes |
| --- | --- | --- | --- | --- | --- |
| Weapon | Improved Agrinium V (`imp_agrinium_5`) | 45 (tier V) | 70 | 33 items: 50 / 70.0 / 80 | 3 |
| Armor | Armor Enhancement - Force Attack V (`aen_frcatk5`) | 45 (tier V) | 60 | 50 items: 50 / 60.0 / 150 | 15 |

## Limitations

- The recipes are an even sample of each band (or all of them, as the inputs say), weighted equally. A recipe the sample skipped can behave differently.
- The adaptive player is a bounded-depth expectimax over the previews with a generic heuristic. It is a floor on skilled play, not an optimal player; a deeper search or a scenario heuristic can only raise it.
- The fixed-rotation solver is a beam search with local search, not a proof. A fixed rotation it did not find would narrow the margin; raise the solver options to look harder. The rotation is scored on separate attempts, so its figure is not flattered by the search.
- Gate 5 compares at matched rank, gear and effort (both sides play to maximize quality) and requires the new minigame's average quality to be at least the legacy player's (within `MaterialQualityShortfallPoints`); it does not compare at an equal quality target. Enhancements are not committed in either model.
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

| Gate | Result | Where it stands |
| --- | --- | --- |
| 1 No fixed rotation | Failed in all 15 cases (margins -0.3 to -0.1) | Needs a structural change; see "Gate 1" below. |
| 2 Style balance | Not applicable | No styles until the perk pull request. |
| 3 Perks optional | Passed (89% to 92% reach maximum quality at rank 50 with best gear) | |
| 4 Length | Passed (10.6 to 15.7 actions at level in every band and gear tier) | |
| 5 Material parity | Failed in 41-53 / no gear (1.54) and 41-53 / mid gear (0.87) only | Both are properties of the comparison, not of the Forge; see "Gate 5" below. |
| 6 XP rate | Passed in every band, gear tier and enhancement count (0 to 3; lowest 1.00x in 1-10 / no gear) with `ForgeRules.XpMultiplier` = 1.55 | The multiplier is an owner decision; see "Gate 6" below. |
| 7 Enhancement parity | Passed (1.06 to 218 times today's expected enhancements; the Forge transfers 1.00 / 2.00 / 3.00 for 1 / 2 / 3 slots) | The shared thresholds are untouched; see "Gate 7" below. |

### What changed from the phase 1 values

Phase 1 values were played first (40 runs, 4 recipes per band, the generic adaptive heuristic): gate 1 failed in 15 of 15 cases, gate 3 passed, gate 4 failed in 2 cases (20.9 and 21.1 actions), gate 5 failed in 3 cases (the adaptive player broke 75% of 1-10 / no gear crafts), and gate 6 failed in 7 cases (lowest 0.13x). The cause of most of it was the player and the profile, not the engine: at ranks 8-24 a crafter has 6-18 CP, so a recipe whose cooling needed two or three Ramp Plasmas could not be finished.

| Value | Phase 1 | Tuned | Why |
| --- | --- | --- | --- |
| Unlock ranks | Vibro-Hone 5, Ramp Plasma 10, Rebond 15, Cryo-Quench 20 | Rebond 10, Cryo-Quench 20, the rest 0 | Owner decision for every minigame. |
| Cooling rate (profile) | 2 + 4 x (level - 1) / 49, rounded | 1 + 0.1 x (level - 1), rounded | A level 10 recipe cooled 4 points per action against 7 CP at rank 10 and could not be finished without a Ramp Plasma. |
| Ramp Plasma CP | 8 | 5 | One Ramp Plasma from rank 7 (CP is 0.75 per rank). |
| Rebond CP | 10 | 8 | Affordable from rank 11, a rank after it unlocks. |
| Hydro-Press base Shape | 130 | 170 | Shorter crafts: gates 4 and 6 (about 21% less work). |
| Vibro-Hone base Finish | 105 | 140 | Same. |
| Cryo-Quench base Finish | 300 | 360 | Same. |
| Post-quench Vibro-Hone | 63 Finish, 55 Shape | 84 Finish, 72 Shape | Scaled with the above, so it stays 0.6 of a best-case hone. |
| Ramp Plasma +25, Rebond +25 Integrity and -12 Thermal, Integrity costs, Stable Containment 35% and +50%, the 80% quench threshold | | unchanged | Raising the opportunity's frequency or strength did not help gate 1 (below). |
| `ForgeRules.XpMultiplier` | none | 1.55 | Gate 6, at its minimum (1-10 / no gear, no enhancements needs 1.54). |
| Adaptive heuristic (scenario) | generic | success bonus 0.85, safety margin 3.0 | The generic one honed to full Finish and then lacked the Integrity to finish Shape. |

### Gate 1: why tuning magnitudes and frequency cannot pass it

Reduced solver (beam 6, 32/80 training/selection attempts), 3 recipes per band, 30 runs, seed 1, the tuned values above. Margins are adaptive minus best fixed rotation, in quality points, over the 15 band and gear cases.

| Stable Containment | Adaptive average | Best fixed average | Margin range |
| --- | --- | --- | --- |
| none (1% announced, +0%) | 98.0 to 100 | 100 | -2.0 to 0.0 |
| the plan's 35%, +50% | 99.5 to 100 | 100 | -0.5 to 0.0 |
| 50%, +100% | 99.6 to 100 | 99.4 to 100 | -0.4 to +0.4 |
| 80%, +300% (extreme) | 99.5 to 100 | 98.8 to 100 | -0.4 to +0.7 |

Both players reach 100% in every case. The Forge is deterministic except for that one opportunity, so a fixed rotation reproduces the best plan every time and a rotation made mostly of Hydro-Press and Vibro-Hone catches the opportunity as often as an adaptive player can use it. Making crafts tighter does not change that: in exploratory runs with the Finish per hone cut to a quarter, the gear scale doubled and Integrity costs raised (1 recipe per band, 16 runs, earlier base values), adaptive play fell 0 to 41 points below the best fixed rotation without any opportunity, and stayed 0 to 29 below at 50% and +100% (one depth-4 search reached +5 in two cells and -29 in another). The adaptive player is a depth-3 expectimax over the previews; against a deterministic game a search over whole rotations plans better than it does. The harness's own test bench passes gate 1 because its Stoke action is chance-dependent.

Proposed structural change (not made; needs the owner's approval). Give the Forge a second, visible, upside-only chance that decides how long to keep honing, so that a rotation that cannot see results must either overspend Integrity or stop short:

1. Vibro-Hone and Hydro-Press each show two results with their odds, a normal one and a Resonant one (for example 70% and 30%, Resonant doubling the gain), with the base lowered so the average stays where it is. The engine and window already support multi-result actions, previews and chances. The normal result is today's value, so nothing punishes; it only adds an upside.
2. Raise the Integrity costs of presses and hones by one, so quality is bound by Integrity and the decision to stop honing and finish Shape matters.
3. Optionally add two more telegraphed opportunity types that reward different actions (for example Plasma Surge: the next Ramp Plasma gives +50% Thermal; Quench Window: the next Cryo-Quench gives +50% Finish), so reacting means choosing the action, not only the timing.

This is reasoned from the results above and has not been simulated for the Forge.

### Gate 5: the failing cells compare different recipe sets

The comparison drops recipes the old minigame never makes from its stack figure and keeps them in the new one, and in 41-53 the old minigame never makes 2 of the 6 sampled recipes at no gear. On the recipes the old minigame does make (100 runs per recipe, seed 1):

| Band / gear | New stacks per item | Legacy stacks per item | Ratio |
| --- | --- | --- | --- |
| 41-53 / no gear | 3.00 | 3.03 | 0.99 |
| 41-53 / mid gear | 4.67 | 5.35 | 0.87 |
| 41-53 / best gear | 4.67 | 4.90 | 0.95 |

The mid gear cell is a real difference with the right sign: the old minigame fails 27% of these recipes at mid gear (each failure loses stacks at 65%) and the Forge fails none, so it uses 13% fewer stacks per item. Matching the old waste would mean making the Forge fail more often, which the outcome contract does not want. A one-sided check (the new minigame uses at most 10% more than the old one) would pass every cell; that change to the gate belongs to the harness owner.

### Gate 6: the XP multiplier

The multiplier is `CraftMinigameXp.MultiplierFor(CraftMinigameType.Forge)`, which reads `ForgeRules.XpMultiplier`. It scales the base XP of every Smithery craft, failures included, in the game and in the simulator. At multiplier 1.0, with the enhancement cases the gate now plays (0 to 3 enhancements, `--gates xp`, seed 1, 100 runs, 6 recipes per band), the multiplier each band needs is:

| Band | Needed at 1.0 | Binding case |
| --- | --- | --- |
| 1-10 | 1.54 | no gear, no enhancement: the old minigame spends 5.8 actions per craft (6 s each), the Forge 13.7 (8 s) |
| 11-20 | 1.09 | no gear |
| 21-30 | 1.15 | no gear, 3 enhancements |
| 31-40 | 1.07 | no gear, 3 enhancements |
| 41-53 | none (the Forge already pays 1.39x or more) | |

The single multiplier is 1.55 (the gate's own "needed" figure for the worst case, 1.55x). It passes every case at 1.00x or more; the lowest is 1-10 / no gear with no enhancement at exactly 1.00x, so a small change in the estimated seconds per action will move it. Enhancements make a Forge craft longer (Shape needs more) but also make the old craft longer, so they never need more than 1.17x. The cost of a single multiplier: Smithery XP per hour is 1.00x to 3.6x the old rate (3.4x to 3.6x at best gear from level 21, where the old minigame's crafts take 21-28 actions and the Forge's 11-18). A band-by-band multiplier (1.54, 1.09, 1.15, 1.07, 1.0 above) would pay 1.0x to 2.7x instead, but `ICraftScenario.XpMultiplier` and `CraftMinigameXp.MultiplierFor` take no recipe level; adding one is a harness and route change for the owner to decide. The seconds-per-action figures (8 and 6) are estimates; client timing will move every number in this gate.

### Gate 7: enhancement parity (reported for the shared thresholds; `CraftOutcomeRules` is not changed)

Enhancements raise the Shape the recipe needs by `ForgeRules.EnhancementShapePercent(penalty, level)` (the penalty compared with the old window's progress for the recipe's level: 33% to 98% in 41-45, 32% to 96% in 46-50, 8% to 25% in 51-53). At rank 50 in best gear and at level, expected enhancements transferred per enhanced craft (seed 1, 100 runs, 6 recipes per band):

| Band | Slots | Today (chance roll) | Forge under 50% / 85% / 85% | Forge / today |
| --- | --- | --- | --- | --- |
| 41-45 | 1 / 2 / 3 | 0.94 / 1.78 / 2.48 | 1.00 / 2.00 / 3.00 | 1.06 / 1.13 / 1.21 |
| 46-50 | 1 / 2 / 3 | 0.81 / 1.36 / 1.82 | 1.00 / 2.00 / 3.00 | 1.24 / 1.47 / 1.65 |
| 51-53 | 1 / 2 / 3 | 0.03 / 0.03 / 0.01 | 1.00 / 2.00 / 3.00 | 30 / 74 / 219 |

The Forge's quality at rank 50 in best gear is at or near 100% on every craft that is made (average 99.7% to 99.9%, 89% to 95% made at exactly 100%, every craft made), so every threshold of 85% or lower transfers every enhancement. The thresholds gate 7 computes for the Forge, the strictest set that keeps every band at least at today's expected enhancements and never falls from one slot to the next, are 98% / 100% / 100% (95% / 100% / 100% in steps of 5); band 41-45 alone gives the same, bands 46-50 and 51-53 alone 100% / 100% / 100%. Today's 51-53 figures are near zero because the old minigame fails or makes poor items there at rank 50, so parity is trivial in that band.
