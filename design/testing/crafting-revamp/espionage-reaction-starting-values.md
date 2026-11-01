# Espionage Reaction: tuned values

Workstream 8 of the crafting revamp. Phase 1 chose starting values; phase 2 tuned them with the CraftSim gates (`reaction-gates.md`, scenario `reaction`). Every number lives in `Service/CraftService/Minigame/Reaction/ReactionRules.cs`. The Reaction is a pure rules engine (`ReactionEngine`) with its window in `Feature/GuiDefinition/ReactionDefinition.cs`.

## Inputs

Espionage has no crafting gear, so its Craftsmanship, Control and CP equivalents come from Espionage rank alone. Every gear tier the simulator offers is the same rank-only setup (the harness lists only "no gear" for Espionage). All arithmetic is whole-number.

| Input | Formula | Rank 25 | Rank 50 |
| --- | --- | --- | --- |
| Craftsmanship (powers Potency actions) | `rank * 3 / 5` | 15 | 30 |
| Control (powers Subtlety actions) | `rank * 3 / 5` | 15 | 30 |
| Max CP | `rank * 3 / 4`; the rank-25 passive was tuned from 31 to 0 | 18 | 37 |
| Potency factor | `100 + 5 * (rank - recipe level)` percent, from 50 to 125 | | |
| Subtlety factor | `100 + 5 * min(rank - recipe level, 0)` percent, from 50 to 100 | | |

A gain is `base * (100 + stat) * factor / 10000`, rounded down, at least 1.

## Unlock ranks (owner decision, October 2026)

Stabilize unlocks at Espionage rank 10. Measure, Charge, Refine and Dampen are available from rank 0. Nothing else is rank-gated; rank changes power and CP. Dampen and Stabilize are also limited by CP.

## Meters and actions

Potency (progress) and Subtlety (quality) run 0 to 100; Stability (integrity) runs 0 to 200. Stability 0 breaks the piece.

| Action | Cost | Effect |
| --- | --- | --- |
| Measure | `wear` Stability | Potency +4 in any phase |
| Charge | `wear`, plus 36 in Rising or Settling, plus `36 + 8 * intensity` at Peak. Calm adds nothing | Potency +14, doubled at Peak |
| Refine | `wear` Stability | Subtlety +11, doubled in Settling, halved (rounded down, at least 1) at Peak |
| Dampen | 3 CP per 20 recipe levels (at least 2); no Stability wear | Stability +8 (never past the maximum), and the cycle does not advance at the end of the turn |
| Stabilize | 3 CP per 5 recipe levels (at least 4) | Stability +40. Refused below rank 10 and while Stability is full |

`wear` is the Stability every action but Dampen and Stabilize costs, by Peak intensity (the recipe's tier) 1 to 5: 7, 9, 9, 10, 12. A more intense mixture wears the piece faster, which keeps the budget as tight in the high tiers, where CP is plentiful, as in the low ones. (Potency and Subtlety amounts are the base values before the stat and factor above.)

- The cycle advances one turn after every performed action that leaves the attempt going.
- The attempt succeeds the moment Potency is full, with Subtlety locked in. If one action both fills Potency and empties Stability, the item is finished.
- Nothing is random once the attempt starts. The one random choice is the starting position, rolled anywhere in the cycle and shown from the first turn.
- The engine announces no opportunity (`NextOpportunity` is always null): the plan gives the Reaction none, and its visible cycle is the telegraph.
- Stabilize costs 3 CP for every 5 recipe levels, so a crafter at the recipe's level (or at its minimum rank, level - 3) can afford one use and not two. Dampen costs 3 CP for every 20 recipe levels (at least 2), a quarter of Stabilize's CP, so from recipe level 11 a crafter at the recipe's level can afford one Stabilize and one Dampen, and a crafter below rank 10, who has not learned Stabilize, can afford a few Dampens. At the minimum rank the CP covers one Stabilize and no Dampen beside it.

## What phase 2 changed, and why

With the phase 1 values, a slow safe rotation always worked: while Stability was slack, "Refine x8, Charge x2, Refine, Charge x4" reached 100% quality from every start, so the best fixed rotation matched adaptive play (gate 1 margin -0.2 to 0.0 in every band). The gates need the budget tight enough that only phase-aligned play fits, without making adaptive play fail.

| Value | Phase 1 | Tuned | Why |
| --- | --- | --- | --- |
| Starting position | Calm or Rising turns | Anywhere in the cycle | A start the rotation was not written for is what breaks it; Calm and Rising only left it too few starts |
| Stability maximum | 100 | 200 | Finer steps for the costs below |
| Stability per action | 2 | 7, 9, 9, 10, 12 by tier | Makes the budget bind, tier by tier, against the CP that grows with rank |
| Charge outside Calm | 10 (Peak: 10 + 4 per intensity) | 36 (Peak: 36 + 8 per intensity) | A Charge off the Calm turn must cost real Stability, or a fixed rotation can Charge at any phase |
| Measure | 5 | 4 | Measure-only Potency was a safe slow route |
| Stabilize restore | 30 | 40 | Keeps one use worth taking after the costs above |
| Stabilize CP | 10 | 3 per 5 recipe levels, at least 4 | One use at the recipe's level in every tier; a flat 10 allowed 3 to 6 uses at high rank and let fixed rotations repair their way through |
| Stabilize unlock | none | rank 10 | Owner decision |
| Rank-25 CP passive | +31 | 0 | The passive handed rank-25+ crafters several Stabilizes |
| XP multiplier | none | 3.0 for recipes up to level 10, none above | Gate 6, see below |
| Dampen | Holds the phase, 6 CP, wears like any action | Holds the phase and restores 8 Stability; no wear; 3 CP per 20 recipe levels (at least 2) | Owner decision (October 2026), tuned with gates 1 and 6, see below |

## Gate results

`reaction-gates.md`: seed 1, 100 runs per case, the long fixed-rotation search (`--thorough`), all 15 recipes, on the base harness after #2826 (gate 1 is one fixed sequence per band, applied to every recipe of the band) and #2825 (the shared per-band XP mechanism). Seeds 2 and 3 (default search) also pass every applicable gate: gate 1 smallest margins 20.6 and 26.8 (both 21-30), gate 6 lowest 1.04x and 1.07x (both 11-20). Gate 6 on seeds 2 and 3 also passed (lowest 1.04x and 1.07x, both in 11-20 with no multiplier).

| Gate | Result | Phase 1 values | Tuned values |
| --- | --- | --- | --- |
| 1 No single rotation | Passed | margin -0.2 to 0.0 points | best single rotation per band: margins 17.1 (21-30), 29.3 (31-40), 30.9 (11-20), 31.9 (41-53), 35.4 (1-10); the best rotation for each recipe alone (reported only) is 15.0 to 39.7 behind |
| 2 Style balance | Not applicable | | Espionage gets no perk lines |
| 3 Perks optional | Passed | 100% | 100% at maximum quality in 41-45 and 46-50 |
| 4 Length | Passed | 11.2 to 14.0 actions | 12.2 to 14.0 actions |
| 5 Material parity | Passed | ratios 0.95 to 1.00 | ratios 0.90 to 1.00, success 100% in every band, average quality 100% (the base's one-sided rule applies in 11-20, where the legacy player succeeds 86%) |
| 6 XP rate | Passed with the 1-10 multiplier | 0.34x in band 1-10 | 1.03x in 1-10 (with 3.0), then 1.15x, 1.19x, 1.25x and 1.67x in 11-20, 21-30, 31-40 and 41-53 (no multiplier) |
| 7 Enhancement parity | Not applicable | | Espionage recipes have no enhancement slots |

Seconds per action: 8 s for the Reaction window against 6 s for the old one, both estimates until the client evidence measures them. Espionage XP is practice XP (at least 150 below the recipe's `SkillUpToRank` limit) on both sides.

## XP multiplier (owner decision, reviewed)

The old minigame is trivial at levels 1-10 with no gear: 3.2 actions a craft at 0% quality, which is 112,000 XP per hour at 6 s an action. Ten to twenty actions at 8 s cannot match that without more XP per craft, and gate 6 asks for at least today's rate. Gate 6 is a floor, not a target, and Espionage is excluded from the progression pace change, so the multiplier is the smallest one that passes in each recipe-level band:

| Recipe level | Multiplier | Why | XP per hour against the old window (seed 1) |
| --- | --- | --- | --- |
| 1-10 | 3.0 (`ReactionRules.LowLevelXpMultiplier`) | 2.93x is the least that passes with Dampen in play (2.9 gave 0.99x) | 1.03x |
| 11 and above | 1.0 | The old loop is already slow there, so the Reaction passes unscaled | 1.15x (11-20), 1.19x (21-30), 1.25x (31-40), 1.67x (41-53) |

The multiplier lives in one place, `ReactionRules.XpMultiplier(recipeLevel)` (with `LowLevelXpMultiplier` and `XpMultiplierLastLevel`), is registered in the shared `CraftMinigameXp.MultiplierFor`, and is applied in `CraftMinigameXp.BaseXp` for both a finished craft and a failed one's 15%. A flat 3.0 over every band would have paid 3.4x to 4.9x the old XP per hour in 11-53. The Bible records the per-band rule, with the Stabilize unlock and Dampen, in a row of the System Changes & Migrations tab (`python tools/UpdateReactionBible.py`, which reads the constants from the source; a test fails when the two drift).

## Does Dampen earn its CP?

Owner decision, October 2026: Dampen keeps its hold and also restores a little Stability. The starting value was 10 Stability at the original flat 6 CP. The gates chose 8 Stability and a CP cost of 3 per 20 recipe levels (at least 2).

How the values were chosen (seed 7, 200 runs, the non-thorough solver; the model player spends CP like a prudent crafter, see the scenario notes below):

| Dampen | Gate 1 smallest margin (21-30 band), per-recipe rotations, before #2826 | What changed for the model player at level |
| --- | --- | --- |
| 6 CP flat, +10 Stability | 19.2 | Used only from level 31 (below that the CP is the one Stabilize); quality 99% to 100%, maximum quality at rank 50 in 46-50 from 92% to 100%, about 0.5 more actions |
| 3 per 20 levels, +10 | 7.1, fails | Fixed rotations repair their way through (best fixed rotation 92.9 against 79.9) |
| 3 per 20 levels, +8 (chosen) | 13.1 here, 15.0 in the thorough report (per-recipe rotations); 17.1 as a single rotation per band after the merge | Used in every band; quality 98% to 100% |
| 3 per 20 levels, +6 | 14.3 | The same, a little less |

The chosen values, with and without Dampen (`reaction` against `reaction-nodampen`, the same engine with Dampen removed; seed 7, 200 runs):

| Band (at level) | Actions with / without | Success with / without | Average quality with / without |
| --- | --- | --- | --- |
| 1-10 | 13.9 / 13.2 | 100% / 100% | 100% / 98% |
| 11-20 | 13.2 / 13.1 | 100% / 100% | 100% / 98% |
| 21-30 | 13.7 / 13.5 | 100% / 100% | 100% / 100% |
| 31-40 | 12.9 / 12.4 | 100% / 100% | 100% / 99% |
| 41-53 | 12.2 / 11.8 | 100% / 100% | 100% / 100% |
| Rank 50, 46-50 | | 100% / 100% | share at maximum quality 100% / 92% |

At the minimum rank (`reaction-minrank` against `reaction-minrank-nodampen`, three ranks below the level) bands 21-30 to 41-53 are identical, because the CP covers one Stabilize and no Dampen beside it. In band 1-10 (ranks 0 to 7, before Stabilize) the model player does worse with Dampen, 72% success against 86%: it plans around a repair it then spends badly. A real crafter cannot do worse with one more option, and the perfect-play search agrees:

| Perfect play (beam search over every recipe and start position) | Dampen improves | Dampen worsens |
| --- | --- | --- |
| At level, all five tiers | no case | no case |
| Minimum rank | 4 of 20 tier-1 starts and 1 of 14 tier-5 starts save about half an action (the search is a beam, so one other tier-5 start came out an action longer) | none reaches a lower quality |

Verdict: Dampen earns its CP modestly at most tiers, not decisively. For the model player it lifts average quality from 98% to 100% in bands 1-20 and 31-40 and makes the rank-50 top tier reliable, at 0.1 to 0.7 more actions. It is the only repair before rank 10. It is not a dominant rotation: the best fixed rotation stays at least 15 points behind adaptive play in every band, and the perfect-play search almost never needs it. Making it decisive would take a bigger restore or a cheaper cost, and with 10 Stability a per-recipe fixed rotation already won the 21-30 band (the gate then searched one rotation per recipe; the choice of 8 was made under that rule and the single-rotation gate passes with more room).

## Scenarios

- `reaction`: the plan's cohort, every recipe at its own level.
- `reaction-nodampen`: the same with Dampen removed, to measure what it is worth.
- `reaction-minrank`, `reaction-minrank-nodampen`: the same recipes three ranks below their levels, the lowest rank that can craft them.
- The model player in all four uses Dampen as a prudent crafter does (`ReactionPlayerEngine`): it never spends the CP its next Stabilize needs on Dampen, and never Dampens while the Stability it restores is not missing.

## Per-recipe parameters

Unchanged by phase 2, so the Bible columns are unchanged: `ReactionParameterProfile` and the 15 recipes' cycle pattern, length and Peak intensity from phase 1. Patterns: Brew (venom coatings), Surge (concentrated venoms), Trigger (trap kits); length 4 to 8; intensity is the tier, 1 to 5.

| Pattern | Extra turns go to, in order | Length 6 | Length 8 |
| --- | --- | --- | --- |
| Brew | Calm, Settling, Calm, Rising | Calm 2, Rising 1, Peak 1, Settling 2 | Calm 3, Rising 2, Peak 1, Settling 2 |
| Surge | Peak, Rising, Peak, Calm | Calm 1, Rising 2, Peak 2, Settling 1 | Calm 2, Rising 2, Peak 3, Settling 1 |
| Trigger | Rising, Calm, Rising, Settling | Calm 2, Rising 2, Peak 1, Settling 1 | Calm 2, Rising 3, Peak 1, Settling 2 |

## Re-running

```powershell
dotnet build SWLOR.CraftSim -p:RunPostBuildEvent=Never -v q -clp:ErrorsOnly
dotnet SWLOR.CraftSim/bin/Debug/net10.0/SWLOR.CraftSim.dll gates --scenario reaction --runs 100 --seed 1 --thorough --out design/testing/crafting-revamp/reaction-gates.md
dotnet SWLOR.CraftSim/bin/Debug/net10.0/SWLOR.CraftSim.dll gates --scenario reaction-nodampen --runs 200 --seed 7 --gates length,materials,perks
dotnet SWLOR.CraftSim/bin/Debug/net10.0/SWLOR.CraftSim.dll gates --scenario reaction-minrank --runs 200 --seed 7 --gates length,materials,perks
```

`python tools/UpdateReactionBible.py` writes the System Changes row and changes nothing on a second run. The recipe Bible columns come from `espionage-reaction-bible-columns.json` (`python tools/UpdateRecipeParameterBible.py --data design/testing/crafting-revamp/espionage-reaction-bible-columns.json`) and only change when the recipe parameters do.
