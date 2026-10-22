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
| Dampen | `wear` Stability, 6 CP | The cycle does not advance at the end of the turn |
| Stabilize | 3 CP per 5 recipe levels (at least 4) | Stability +40. Refused below rank 10 and while Stability is full |

`wear` is the Stability every action but Stabilize costs, by Peak intensity (the recipe's tier) 1 to 5: 7, 9, 9, 10, 12. A more intense mixture wears the piece faster, which keeps the budget as tight in the high tiers, where CP is plentiful, as in the low ones. (Potency and Subtlety amounts are the base values before the stat and factor above.)

- The cycle advances one turn after every performed action that leaves the attempt going.
- The attempt succeeds the moment Potency is full, with Subtlety locked in. If one action both fills Potency and empties Stability, the item is finished.
- Nothing is random once the attempt starts. The one random choice is the starting position, rolled anywhere in the cycle and shown from the first turn.
- The engine announces no opportunity (`NextOpportunity` is always null): the plan gives the Reaction none, and its visible cycle is the telegraph.
- Stabilize costs 3 CP for every 5 recipe levels, so a crafter at the recipe's level (or at its minimum rank, level - 3) can afford one use and not two. Dampen costs 6 CP, so at the recipe's level a crafter can afford Stabilize or Dampen, and only in the top two tiers both.

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
| XP multiplier | none | 3.0 | Gate 6, see below |

## Gate results

`reaction-gates.md`: seed 1, 100 runs per case, the long fixed-rotation search (`--thorough`), all 15 recipes. Seeds 2 and 3 at the default search also passed every gate.

| Gate | Result | Phase 1 values | Tuned values |
| --- | --- | --- | --- |
| 1 No fixed rotation | Passed | margin -0.2 to 0.0 points | smallest margin 17.8 (31-40), 18.4 (21-30), 24.7 (41-53), 27.7 (11-20), 44.5 (1-10) |
| 2 Style balance | Not applicable | | Espionage gets no perk lines |
| 3 Perks optional | Passed | 100% | 100% at maximum quality in 41-45 and 46-50 |
| 4 Length | Passed | 11.2 to 14.0 actions | 12.4 to 14.0 actions |
| 5 Material parity | Passed | ratios 0.95 to 1.00 | ratios 0.90 to 1.00, success 92% to 100%, average quality 97% to 100% |
| 6 XP rate | Passed with the multiplier | 0.34x in band 1-10 | 1.06x in 1-10, 3.4x to 4.9x in the other bands |

Seconds per action: 8 s for the Reaction window against 6 s for the old one, both estimates until the client evidence measures them. Espionage XP is practice XP (at least 150 below the recipe's `SkillUpToRank` limit) on both sides.

## XP multiplier (owner decision)

The old minigame is trivial at levels 1-10 with no gear: 3.2 actions a craft at 0% quality, which is 112,000 XP per hour at 6 s an action. Ten to twenty actions at 8 s cannot match that without more XP per craft, and the gate asks for at least today's rate. `ReactionRules.XpMultiplier` (3.0) scales a Reaction craft's base XP, applied in `CraftMinigameXp.BaseXp` for both a finished craft and a failed one's 15%. Band 1-10 needed 2.83x. The Bible records it, with the Stabilize unlock, in a row of the System Changes & Migrations tab (`python tools/UpdateReactionBible.py`, which reads the constants from the source; a test fails when the two drift).

One multiplier cannot fit every band: the same 3.0 pays 3.4x to 4.9x the old XP per hour in bands 11-53, where the old loop was slower. Espionage levels about three times faster than today. If the owner prefers, the multiplier could apply only to the lowest tier, or gate 6 could be accepted below 1.0 in band 1-10; either is a decision, not a tuning.

## Does Dampen earn its CP?

Same recipes, ranks and dice (seed 7, 200 runs), with and without Dampen (`gates --scenario reaction-nodampen`, the same engine with Dampen removed):

| Band | Actions with / without | Success with / without | Average quality with / without |
| --- | --- | --- | --- |
| 1-10 (no CP) | 13.2 / 13.2 | 100% / 100% | 98% / 98% |
| 11-20 | 12.5 / 13.1 | 100% / 100% | 99% / 98% |
| 21-30 | 14.0 / 13.5 | 100% / 100% | 100% / 100% |
| 31-40 | 13.1 / 12.4 | 92% / 100% | 97% / 99% |
| 41-53 | 12.4 / 11.8 | 100% / 100% | 100% / 100% |
| Rank 50, 46-50 | | 100% / 100% | share at maximum quality 100% / 92% |

Mostly no. Dampen only shifts the schedule by one turn, so it cannot add a productive turn; it pays when that shift lines up the last Charge with a Peak or the last Refine with a Settling. It helps in tier 2 (0.6 fewer actions) and at rank 50 in tier 5 (maximum quality on 100% of crafts instead of 92%), costs half an action in tiers 3 and 5 at level, and in tier 4 the simulated player loses 8 points of success by spending CP on it that Stabilize needed. A cheaper Dampen (4 CP) and a Stability-free Dampen changed none of this. The Reaction passes every gate either way, so Dampen stays as the plan words it; making it consistently useful needs a second effect, which is a design change for the owner.

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
```

`python tools/UpdateReactionBible.py` writes the System Changes row and changes nothing on a second run. The recipe Bible columns come from `espionage-reaction-bible-columns.json` (`python tools/UpdateRecipeParameterBible.py --data design/testing/crafting-revamp/espionage-reaction-bible-columns.json`) and only change when the recipe parameters do.
