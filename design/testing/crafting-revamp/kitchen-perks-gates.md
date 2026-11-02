# Crafting revamp: balance gates of the Kitchen minigame

- Command: `SWLOR.CraftSim gates --scenario kitchen --gates styles,perks --runs 40 --seed 1 --repo . --out design/testing/crafting-revamp/kitchen-perks-gates.md`
- Seed 1; 40 runs per case.

## Inputs

- Scenario: Kitchen (`kitchen`), Agriculture recipes, minigame Kitchen.
- Quality: the quality meter's share of its maximum when the item is made, in points (0-100). A broken or abandoned piece counts 0, so every average includes the crafts that failed.
- Crafter: the rank the scenario's cohort gives each recipe (by default the recipe's level, at most 50, so a crafter works at their own level), with no food or status bonuses and no perks unless a gate names a style or the scenario's limitations say it plays a perk loadout. Gear levels are the plan's none, middle and best tier of the skill's crafting gear:
  - no gear (Control 0, Craftsmanship 0, CP 0)
  - mid gear (Control 13, Craftsmanship 12, CP 20)
  - best gear (Control 29, Craftsmanship 30, CP 37)
- Recipes: 1-10: 6 of 58; 11-20: 6 of 59; 21-30: 6 of 65; 31-40: 6 of 61; 41-53: 6 of 76 recipes played.
- Adaptive player: depth 3, forecast samples per result 1, budget 1500 engine copies per decision, 0.5% of value lost per extra action, heuristic KitchenLeafHeuristic. Each attempt's dice come from its own stream of the seed.
- Fixed-rotation solver: beam 8, cycles up to 3, 8 adaptive lines as seeds, up to 40 actions, 64 training attempts per local-search round and 150 selection attempts for each of 12 finalists, local search from the best 3 candidate(s) for up to 3 round(s) each. Gate 1 searches one sequence for each band and gear tier across the band's sampled recipes (every recipe weighing the same; an action a recipe's engine cannot take is skipped there), and also each recipe's own best sequence as a reported metric. The winner is chosen on separate attempts and then scored on a third set (hold-out) with 40 attempts per recipe.
- Legacy comparator (gates 5 and 6): the old minigame's lookahead player (`SWLOR.CraftSim baseline`), replayed here with the same seed on the same recipes, ranks and gear.
- Time: 8.0 s per action in the new window and 6.0 s in the old one, plus 0.0 s per craft in both. These are estimates; the client evidence replaces them.
- XP multiplier on the base XP of every craft, failures included, as the game server applies it (`CraftMinigameXp`): 1-10 2.15x, 11-20 1.15x, 21-30 1.10x, 31-40 1.20x, 41-53 1.05x. The new side's XP below includes it; the legacy loop's does not.
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
- Styles compared: Slow Cooking vs Flash Cooking (from rank 20); Slow Cooking (full line) vs Flash Cooking (full line) (from rank 20); Slow Cooking (full line, Seasoned Palate on) vs Flash Cooking (full line, Seasoned Palate on) (from rank 20).
- Gates run: Style balance, Perks optional. The others were not run, so this report cannot show that the minigame passes.

## Verdicts

| Gate | Verdict | Summary |
| --- | --- | --- |
| Style balance | Passed | Every style is the better choice on at least 30% of recipes and no pair differs by more than 5 points on average. |
| Perks optional | Passed | An unperked rank-50 crafter in best gear reaches maximum quality on at least 50% of crafts in every at-level band (lowest 58%). |

## Style balance

### Style pairs: how often each style is better, and the average quality (quality points; ties count half a win each)

| Style pair | Gear | Recipes | First better | Second better | First avg quality | Second avg quality | Difference | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Slow Cooking vs Flash Cooking | no gear | 18 | 50% | 50% | 89.3 | 89.8 | -0.5 | Passed |
| Slow Cooking vs Flash Cooking | mid gear | 18 | 47% | 53% | 91.3 | 91.3 | -0.0 | Passed |
| Slow Cooking vs Flash Cooking | best gear | 18 | 33% | 67% | 95.0 | 95.5 | -0.4 | Passed |
| Slow Cooking (full line) vs Flash Cooking (full line) | no gear | 18 | 56% | 44% | 92.9 | 92.3 | +0.6 | Passed |
| Slow Cooking (full line) vs Flash Cooking (full line) | mid gear | 18 | 50% | 50% | 95.1 | 94.7 | +0.4 | Passed |
| Slow Cooking (full line) vs Flash Cooking (full line) | best gear | 18 | 53% | 47% | 97.4 | 97.0 | +0.5 | Passed |
| Slow Cooking (full line, Seasoned Palate on) vs Flash Cooking (full line, Seasoned Palate on) | no gear | 18 | 39% | 61% | 87.6 | 89.2 | -1.6 | Passed |
| Slow Cooking (full line, Seasoned Palate on) vs Flash Cooking (full line, Seasoned Palate on) | mid gear | 18 | 42% | 58% | 93.2 | 94.4 | -1.2 | Passed |
| Slow Cooking (full line, Seasoned Palate on) vs Flash Cooking (full line, Seasoned Palate on) | best gear | 18 | 64% | 36% | 96.7 | 96.5 | +0.2 | Passed |

## Perks optional

### Rank 50, best gear, no perks: adaptive attempts that reach maximum quality, by recipe band

| Band | Recipes played | Attempts | Reach maximum quality | Make the item | Avg quality of items made | Required | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 6 | 240 | 88% | 99% | 97% | >= 50% | Passed |
| 46-50 | 6 | 240 | 79% | 98% | 94% | >= 50% | Passed |
| 51-53 | 6 | 240 | 58% | 93% | 85% | >= 50% | Passed |

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
- The unperked scenario plays no perks except in the style-balance gate, which compares the two Chef styles. A Chef is played with the numbers of the game's own perk definitions at the rank the recipe is played at.
- Seasoned Palate is a switch the cook makes for each dish. The Chef scenarios play it off; the Seasoned Palate scenarios play it on for every dish. A real cook would choose per dish.
- Seconds per action is an estimate (8 s) until the client evidence measures it.
- Ingredient names are the resrefs here; the window shows item names.

## The other gates for a perked Chef

Gates 2 and 3 above are the style and perks-optional gates at the gate's own sample (6 recipes per band, 40 runs, seed 1). The other gates are played by the Chef scenarios (`kitchen-slow-cooking`, `kitchen-flash-cooking`: the style from rank 20 and every other purchase the rank allows, Seasoned Palate off), and their reports are `kitchen-slow-cooking-gates.md` and `kitchen-flash-cooking-gates.md`. Gates 1, 5, 6 and 7 are sampled (4 recipes per band, 40 runs, seed 1); gate 4 is the full sample (6 recipes per band, 100 runs).

| Gate | Slow Cooking line | Flash Cooking line |
| --- | --- | --- |
| 1: no fixed rotation | Open (see below) | Open (see below) |
| 4: length (perked floor 8 actions, ceiling 20) | Passed: every cell 10.1 to 16.1 actions | Passed: every cell 10.1 to 15.8 actions |
| 5: economy and material parity | Passed | Passed |
| 6: XP rate (every cell at least 1.0x) | Passed: lowest 1.04x (21-30, no gear, 1 enhancement) | Passed: lowest 1.06x (21-30, no gear, 1 enhancement) |
| 7: enhancement parity | Passed: lowest 1.07x | Passed: lowest 1.07x |

Gates 4 and 6 hold without changing the Kitchen's base tuning or its XP multipliers (`KitchenTuning` is unchanged).

### Gate 1

Gate 1 is open for perked Chef builds, as for the Circuit, Fabricator and perked Forge (owner, October 2026; see the plan's gate 1 note). Not retuned here; judged from player feedback.

For the record, the Slow Cooking line at the sample above (the Flash Cooking line was not run for this gate): adaptive play beats the best single fixed rotation by 11.0 to 26.8 points in bands 1-10 and 11-20 (the perks hardly apply below rank 20), and by -9.3 to +2.3 points in bands 21-30, 31-40 and 41-53, where Reduce (rank 25 and up) and a Low heat dish that no longer drifts let one rotation of "add, simmer at Low heat, Reduce" match adaptive play. No cell reaches the saturation mark (adaptive mean quality of 98 or more). The unperked Kitchen passes gate 1 in every cell (`kitchen-gates.md`).
