# Crafting revamp: balance gates of the Reaction minigame

- Command: `SWLOR.CraftSim gates --scenario reaction --runs 100 --seed 1 --thorough --out design/testing/crafting-revamp/reaction-gates.md --json C:\Users\Zunath\AppData\Local\Temp\claude-crafting-revamp-8-espionage-reaction-tuning\reaction-gates.json`
- Seed 1; 100 runs per case.

## Inputs

- Scenario: Reaction (`reaction`), Espionage recipes, minigame Reaction.
- Quality: the quality meter's share of its maximum when the item is made, in points (0-100). A broken or abandoned piece counts 0, so every average includes the crafts that failed.
- Crafter: the rank the scenario's cohort gives each recipe (by default the recipe's level, at most 50, so a crafter works at their own level), with no food or status bonuses and no perks unless a gate names a style. Gear levels are the plan's none, middle and best tier of the skill's crafting gear:
  - no gear (Control 0, Craftsmanship 0, CP 0)
- Recipes: 1-10: 3; 11-20: 3; 21-30: 3; 31-40: 2; 41-53: 4 recipes played.
- Adaptive player: depth 5, forecast samples per result 1, budget 6000 engine copies per decision, 0.5% of value lost per extra action, heuristic DefaultLeafHeuristic. Each attempt's dice come from its own stream of the seed.
- Fixed-rotation solver: beam 12, cycles up to 3, 12 adaptive lines as seeds, up to 40 actions, 100 training attempts per local-search round and 250 selection attempts for each of 16 finalists, local search from the best 5 candidate(s) for up to 4 round(s) each. The winner is chosen on separate attempts and then scored on a third set (hold-out) with 100 attempts.
- Legacy comparator (gates 5 and 6): the old minigame's lookahead player (`SWLOR.CraftSim baseline`), replayed here with the same seed on the same recipes, ranks and gear.
- XP multiplier: the new minigame's base XP is scaled by 3.00 (gate 6); the legacy loop's is not.
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
  - `RecipesPerBand` = 6: recipes played per band
  - `SolverOptions` = overridden: effort of the fixed-rotation solver
  - `ActionLimit` = 100: actions after which an attempt counts as stalled
- Length target: 10-20 actions (item craft at level).
- Styles: none offered, so the style-balance gate is not applicable.

## Verdicts

| Gate | Verdict | Summary |
| --- | --- | --- |
| No fixed rotation | Passed | Adaptive play beats the best fixed rotation by at least 10 points in every band and gear tier (smallest margin 17.8 in 31-40 / no gear). |
| Style balance | NotApplicable | The scenario offers no style configurations yet, so there is nothing to compare. |
| Perks optional | Passed | An unperked rank-50 crafter in no gear reaches maximum quality on at least 50% of crafts in every at-level band (lowest 100%). |
| Length | Passed | Every band and gear tier takes 10-20 actions (item craft at level) on average. |
| Economy parity | Passed | Component stacks per item are within 10% of the legacy baseline, at no less quality, in every band and gear tier. |
| XP rate | Passed | Item crafts: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.06x in 1-10 / no gear). |

## No fixed rotation

### Average quality: adaptive play against the best fixed rotation (quality points; a broken piece counts 0)

| Band | Gear | Recipes played | Adaptive avg quality | Best fixed avg quality | Margin | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 3 | 93.5 | 49.1 | 44.5 | >= 10 | Passed |
| 11-20 | no gear | 3 | 96.0 | 68.2 | 27.7 | >= 10 | Passed |
| 21-30 | no gear | 3 | 100.0 | 81.6 | 18.4 | >= 10 | Passed |
| 31-40 | no gear | 2 | 92.5 | 74.7 | 17.8 | >= 10 | Passed |
| 41-53 | no gear | 4 | 99.9 | 75.2 | 24.7 | >= 10 | Passed |

### The best fixed rotation found for the first recipe played in each band and gear tier (average quality in points)

| Band | Gear | Recipe | Best fixed rotation | Training | Selection | Hold-out | Adaptive on this recipe |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | poison_vial_1 (level 3) | refine > charge x2 > refine x2 > measure > charge > refine x2 > charge x2 > refine > charge | 55.8 | 55.0 | 58.6 | 99.9 |
| 11-20 | no gear | poison_vial_2 (level 16) | charge > refine > charge > refine > charge > refine > stabilize > refine > stabilize > refine x2 > charge > refine > charge x2 > refine > charge > refine > charge | 77.1 | 81.0 | 83.9 | 99.7 |
| 21-30 | no gear | poison_vial_3 (level 26) | refine x3 > charge > refine > stabilize > refine x2 > charge > refine > charge x2 > measure > refine > charge x2 | 98.4 | 98.4 | 98.8 | 100.0 |
| 31-40 | no gear | poison_vial_4 (level 36) | refine x4 > stabilize > charge > refine > stabilize > charge > refine > charge > measure > refine > charge | 99.1 | 99.0 | 98.9 | 100.0 |
| 41-53 | no gear | trap_kit_4 (level 45) | refine x4 > stabilize > refine > stabilize > charge x2 > refine > charge > refine > measure x3 > charge > refine x2 > charge | 97.5 | 97.6 | 96.9 | 100.0 |

## Perks optional

### Rank 50, no gear, no perks: adaptive attempts that reach maximum quality, by recipe band

| Band | Recipes played | Attempts | Reach maximum quality | Make the item | Avg quality of items made | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 1 | 100 | 100% | 100% | 100% | >= 50% | Passed |
| 46-50 | 3 | 300 | 100% | 100% | 100% | >= 50% | Passed |

## Length

### Actions per attempt of the adaptive player at level (item craft at level)

| Band | Gear | Recipes played | Mean actions per attempt | Shortest recipe | Longest recipe | Stalled attempts | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 3 | 13.1 | 12.4 | 13.9 | 0 | 10-20 | Passed |
| 11-20 | no gear | 3 | 12.5 | 11.7 | 13.9 | 0 | 10-20 | Passed |
| 21-30 | no gear | 3 | 14.0 | 12.5 | 16.2 | 0 | 10-20 | Passed |
| 31-40 | no gear | 2 | 13.0 | 12.3 | 13.7 | 0 | 10-20 | Passed |
| 41-53 | no gear | 4 | 12.4 | 11.1 | 13.5 | 0 | 10-20 | Passed |

## Economy parity

### Component stacks consumed per item made, failures included (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)

| Band | Gear | Recipes played | New stacks per item | Legacy stacks per item | Ratio | New success | Legacy success | New avg quality | Legacy avg quality | Quality matched | Never made (new / legacy) | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 3 | 0.55 | 0.55 | 1.00 | 100% | 99% | 97% | 0% | yes | 0% / 0% | Passed |
| 11-20 | no gear | 3 | 0.55 | 0.60 | 0.90 | 100% | 86% | 99% | 1% | yes | 0% / 0% | Passed |
| 21-30 | no gear | 3 | 0.55 | 0.55 | 1.00 | 100% | 100% | 100% | 9% | yes | 0% / 0% | Passed |
| 31-40 | no gear | 2 | 0.67 | 0.68 | 0.97 | 92% | 96% | 98% | 9% | yes | 0% / 0% | Passed |
| 41-53 | no gear | 4 | 0.50 | 0.50 | 0.99 | 100% | 99% | 100% | 41% | yes | 0% / 0% | Passed |

## XP rate

### Item crafts: XP per hour against the legacy loop (6.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per craft | Legacy actions per craft | Legacy XP per hour | New XP per craft | New actions per craft | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 600 | 3.2 | 112720 | 3484 | 13.1 | 119759 | 1.06 | - | Passed |
| 11-20 | no gear | 536 | 9.2 | 35155 | 3527 | 12.5 | 126914 | 3.61 | - | Passed |
| 21-30 | no gear | 656 | 11.9 | 33140 | 3600 | 14.0 | 115549 | 3.49 | - | Passed |
| 31-40 | no gear | 636 | 11.2 | 33898 | 3350 | 13.0 | 116015 | 3.42 | - | Passed |
| 41-53 | no gear | 635 | 19.2 | 19860 | 2698 | 12.4 | 97645 | 4.92 | - | Passed |

## Limitations

- The recipes are an even sample of each band (or all of them, as the inputs say), weighted equally. A recipe the sample skipped can behave differently.
- The adaptive player is a bounded-depth expectimax over the previews with a generic heuristic. It is a floor on skilled play, not an optimal player; a deeper search or a scenario heuristic can only raise it.
- The fixed-rotation solver is a beam search with local search, not a proof. A fixed rotation it did not find would narrow the margin; raise the solver options to look harder. The rotation is scored on separate attempts, so its figure is not flattered by the search.
- Gate 5 compares at matched rank, gear and effort (both sides play to maximize quality) and requires the new minigame's average quality to be at least the legacy player's (within `MaterialQualityShortfallPoints`); it does not compare at an equal quality target. Enhancements are not committed in either model.
- Gate 6 uses estimated seconds per action. The legacy comparator is the old minigame's lookahead player, one step of policy improvement over the rotation players report, not an optimal player.
- Gathering, scavenging, fishing and research are not covered here; their cohorts implement `IXpRateCohort` and their economy checks belong to their own gates.
- No attributes, food, status effects, blueprints, first-craft bonus or quick craft are modeled, and the client was not run.
- Espionage has no crafting gear, so the Reaction reads Espionage rank only (Craftsmanship, Control and CP equivalents). The gear tier is ignored and every gear tier is the same rank-only setup; the harness offers only "no gear" for Espionage.
- The Reaction has no random event after the attempt starts. The only chance is the starting position, rolled anywhere in the cycle, so every attempt of a recipe differs only by its start.
- Espionage XP is practice XP (at least 150 below the recipe's SkillUpToRank limit), taken from `Craft.GetBaseRecipeXP` for both the legacy loop and the Reaction; the recipes are the 15 Espionage recipes, so each band has only a few and the sample is the whole band.
- The Reaction offers no styles (Espionage gets no perk lines), so the style-balance gate is not applicable. Gate 3 has recipes only in the 41-45 and 46-50 bands; the game has no Espionage recipe above level 50.
