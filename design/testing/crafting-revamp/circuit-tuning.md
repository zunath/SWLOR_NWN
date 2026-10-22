# Engineering Circuit: phase 2 tuning notes

Inputs and results of the CraftSim gates for the Circuit. The generated report is `circuit-gates.md` (and `circuit-gates.json`); this file records the values, what phase 2 changed, and the evidence for the one gate that does not pass.

Command: `SWLOR.CraftSim gates --scenario circuit --runs 100 --recipes-per-band 6 --seed 1 --out design/testing/crafting-revamp/circuit-gates.md --json design/testing/crafting-revamp/circuit-gates.json`. Seed 1, 100 runs per case, 6 recipes per band (an even sample of 307, 367, 359, 370 and 383 active Engineering recipes), crafter rank equal to recipe level (capped at 50), none, mid and best gear, and for gate 6 also one, two and three enhancements. Seconds per action is an estimate of 8 s for the Circuit (6 s for the old window); client timing replaces both. The sample is one fixed set of recipes: a different sample can move a figure that sits at its threshold (see "XP multipliers").

## Results

| Gate | Verdict | Evidence |
| --- | --- | --- |
| 1 No fixed rotation | Failed | Gated: the margin of adaptive play over the best single rotation of a band and gear tier is -7.5 to -0.0 points in every band and tier; 10 are required. Reported: the best rotation of each recipe alone is -10.4 to -0.0 behind. See below. |
| 2 Style balance | Not applicable | The Circuit has no styles until its perks exist. |
| 3 Perks optional | Passed | Rank 50, best gear, no perks: 95%, 99% and 96% of crafts reach maximum quality in bands 41-45, 46-50 and 51-53. |
| 4 Length | Passed | 13.7 to 18.0 actions per craft on average in every band and tier (target 10-20); the shortest recipe averages 12.1 and the longest 19.8. |
| 5 Material parity | Passed | Stacks per item are 0.92 to 1.04 of the old window's, at higher quality in every case; the thinnest cell is 41-53 with mid gear (0.92). The old window succeeds on under 90% of crafts only at 41-53 with no gear and mid gear, where the one-sided rule (PR 1g) lets the new side use up to 10% more; the Circuit is at 1.04 and 0.92. |
| 6 XP rate | Passed | XP per hour is 1.00x to 4.60x the old window's in all 60 cases (5 bands x 3 gear tiers x none, 1, 2 and 3 enhancements) with the per-band multipliers below. The closest cases are 1-10, 11-20 and 21-30 with no gear (1.00x), by construction. |
| 7 Enhancement parity | Passed | At rank 50 in best gear the Circuit transfers every enhancement (100% quality): 1, 2 and 3 of 1, 2 and 3 slots. That is 1.08 to 1.80 times today's in bands 41-45 and 46-50, and far more in 51-53, where the old window rarely makes the item. |

## Values

All in `CircuitRules`, `CircuitSetup` and `CircuitParameterProfile`. Phase 2 left the generator's bounds and base values alone, so the recipe parameters and the Bible columns are unchanged.

| Value | Phase 1 | Phase 2 | Why |
| --- | --- | --- | --- |
| Unlock ranks | Wire, Discharge (Assembly) 0; Tune, Discharge (Calibration) 5; Patch 10; Ground 15; Boost 20 | Patch 10, every other action 0 | Owner decision: every action that plays the loop is open from rank 0 and only the repair is gated. A Clean Signal is announced from rank 0 because Tune is open. |
| Enhancement Assembly | the whole progress penalty added to Assembly | the penalty, capped at 30% of the Assembly the recipe itself needs (`EnhancementAssemblyPerPenalty` 1.0, `MaxEnhancementAssemblyShare` 0.3) | Gate 6 with enhancements. See "Enhancements". |
| XP multiplier | none | 2.29 for recipe levels 1-10, 1.52 for 11-20, 1.13 for 21-30, none (1.00) from 31 up, on a craft's base XP for an item and a failure alike (`CircuitRules.XpMultiplierFor`, read through `CraftMinigameXp`) | Gate 6. See "XP multipliers". |

Everything else is as phase 1: Wire 8.5% and Tune 5.5% of the meter at Load 0, Discharge 2.5% per Load, Load boost per recipe (8, 10 or 12% per point), overload 8% per point above the line costing 30% of maximum Integrity, Clean Signal 25% a turn and doubling Tune, CP = gear + 0.75 x rank (+31 from rank 25), costs 3, 4, 3 and 8 CP for Tune, Boost, Ground and Patch, and 3, 3, 3, 4 and 2 Integrity for Wire, Tune, Boost, Discharge and Ground. A crafter below the recipe's level loses 5% of output per rank (to 3 ranks below). The help text now reads the slot thresholds from `CraftOutcomeRules.GetEnhancementThresholds` (a test checks it), so a change to the shared thresholds reaches the window.

## Enhancements (gates 6 and 7)

`CircuitSetup` takes the enhancements' total progress penalty (in the old window's progress units) and adds `CircuitRules.EnhancementAssembly(total, assemblySize)` to the Assembly the piece needs; the window and `CircuitScenario` share it. Wire, Tune and Discharge output do not shrink, so enhancements cost actions and Integrity, not power.

The cap is needed at low levels. The level chart gives a recipe of level 1-10 only 19 to 45 progress and 60 Integrity, while one enhancement item adds 10 to 40 penalty, so the penalty alone adds 120% to 178% at the most slots (the legacy baseline's figures) and, at three slots, more than doubles the Assembly a piece needs. A Wire costs 3 Integrity, so the Circuit breaks the piece before it can finish. The old window's lookahead player finishes the same pieces in 6 to 10 actions and, at mid and best gear, its XP barely falls with the enhancements. Measured on the gate's sample, with the XP multiplier at 1 so the figures are the multiplier a case needs in absolute terms (the uncapped row is the first run's figures at its 2.4 multiplier, scaled to 1):

| Enhancement Assembly | Highest multiplier any enhanced case at levels 1-10 needs | The plain crafts at levels 1-10 need |
| --- | --- | --- |
| the whole penalty, no cap | 6.6 (mid gear, 3 enhancements; 2.7 to 4.8 for the others) | 2.28 |
| capped at 60% | 2.77 (mid gear, 2 enhancements) | 2.28 |
| capped at 30% | 2.23 (mid gear, 1 enhancement) | 2.28 |

At 30% no enhanced case in any band needs more than the plain crafts of its band, so the multipliers below are set by plain play and enhancing costs nothing extra in XP. A cap that low leaves the enhancement penalty binding only at levels where it is large against the recipe: at 41-45 one slot is +34% in the legacy baseline and three slots +103%, and the Circuit adds at most +30%. The Circuit's quality does not need the help: at rank 50 and the best gear it makes 100% quality whatever the slot count (gate 7), so the cap does not change gate 7's outcome. If the owner wants enhancements to remain a real difficulty at high level, the lever is the cap (`MaxEnhancementAssemblyShare`), at a cost in XP multiplier at levels 1-10 (the 60% row).

### Gate 7: the Circuit's expected enhancements and the parity thresholds

Rank 50, best gear, at level, the thresholds in force (50% / 85% / 85%). Today is the old window's chance roll (quality percentage per enhancement) with the representative enhancement slotted.

| Band | Slots | Today | Circuit | Circuit / today |
| --- | --- | --- | --- | --- |
| 41-45 | 1 / 2 / 3 | 0.92 / 1.69 / 2.28 | 1.00 / 2.00 / 3.00 | 1.08 / 1.18 / 1.31 |
| 46-50 | 1 / 2 / 3 | 0.79 / 1.31 / 1.67 | 1.00 / 2.00 / 3.00 | 1.26 / 1.52 / 1.80 |
| 51-53 | 1 / 2 / 3 | 0.02 / 0.02 / 0.01 | 1.00 / 2.00 / 3.00 | 58 / 99 / 435 |

The Circuit's quality is 100% in 98% to 100% of crafts at rank 50 with the best gear, so the thresholds that keep every case at today's are 100% / 100% / 100% (in every band together and alone): any set at or below that passes. The shared thresholds were not touched; the coordinator sets them from this and the other minigames' figures.

## XP multipliers

A Circuit craft takes 14 to 18 actions where the old window takes 5.6 to 10 at levels 1-10 and 11 to 28 above, so XP per craft has to rise at low levels and hardly at all at high ones. Gate 6 gives the multiplier each band needs; the one shipped is the smallest to two decimals that keeps every case of the band (three gear tiers, none to three enhancements) at 1.00x or more. Measured with the multiplier at 1 on the sample above:

| Recipe levels | Needed (exact) | Set by | Shipped | Lowest case at the shipped value |
| --- | --- | --- | --- | --- |
| 1-10 | 2.283 | 1-10, no gear, no enhancement | 2.29 | 1.00x |
| 11-20 | 1.519 | 11-20, no gear, no enhancement | 1.52 | 1.00x |
| 21-30 | 1.129 | 21-30, no gear, no enhancement | 1.13 | 1.00x |
| 31-40 | 0.990 | none needed | 1.00 | 1.01x |
| 41-53 | 0.771 | none needed | 1.00 | 1.30x |

The multiplier is one value per band of recipe levels, as the plan asks, recorded in code (`CircuitRules.XpMultiplierLevels1To10` and the others). It is a step function; a smooth curve through the same points would avoid the 2.29 to 1.52 jump between levels 10 and 11, and the owner and the plan's XP-pace workstream may prefer one. The Bible records it as System Changes & Migrations entry 13, written from `CircuitRules.cs` by `tools/UpdateCircuitBible.py`; rerun the script after a change to the multipliers or the cap. A second sample (10 recipes per band, seed 2, the same multipliers) leaves 2 of 60 cases at 0.99x (11-20 and 21-30 with no gear): the multipliers are the minimum on the committed sample, so another sample can sit a point under.

## Gate 1

### Per recipe (reported, no longer gated)

The best fixed rotation of each recipe scores 98 to 100 at every level above 10 with mid or best gear, and the adaptive player scores the same, so those cells sit at a margin of 0.0 however the rest is tuned: a margin of 10 needs the fixed rotation at 90 or less. Where quality is not capped, the adaptive player is below the fixed rotation, not above it.

Why the fixed rotation is strong: the Circuit's only chance is the Clean Signal and the overload dice. Load, CP, Integrity and Assembly are deterministic, so one sequence of up to 40 actions finds the whole optimum: build Load with Tunes up to the line, bank it into Calibration or Assembly, wire to finish. A rotation skips an unavailable action, which gives it free conditional behavior (it does not Patch at full Integrity or Discharge at zero Load). It can also run longer than the adaptive player's 14 to 18 actions: Patch trades CP for Integrity, so extra Tunes cost only CP.

Why the adaptive player is weaker where quality binds: the harness's adaptive player is a depth-3 expectimax whose heuristic reads only the shared meters and the previews. The Circuit needs plans it cannot see: keep enough Integrity to finish (a Discharge preview promises a burst that is gone once the Load is spent), wire on turns with no signal and Tune on turns with one, and spend CP only where it pays. In the runs it overspends Integrity on Tunes and cannot finish (breaks), or spends CP on Patch and Boost early.

The ceiling on what a perfect player gains from the signal is 2 / (1 + p) times what the fixed rotation gets: the fixed rotation's Tunes meet a signal by chance a share p of the time, the adaptive player puts every Tune on one. At p = 0.25 that is 1.6, and the quality has to be binding for it to show.

### One rotation per band and tier (the owner's definition, in the harness since PR 1g and gated)

`FixedRotationSolver.SolveShared` searches one sequence for every sampled recipe of a band at each gear tier, and the adaptive player must beat it by 10 points. The per-recipe best rotations are reported in the last column and in a second table of `circuit-gates.md`.

| Band | Gear | Adaptive avg quality | Best single rotation | Margin | Required | Best rotation per recipe (reported) | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 47.4 | 48.4 | -1.0 | >= 10 | 51.7 | Failed |
| 1-10 | mid gear | 88.1 | 95.2 | -7.2 | >= 10 | 98.4 | Failed |
| 1-10 | best gear | 91.4 | 98.9 | -7.5 | >= 10 | 100.0 | Failed |
| 11-20 | no gear | 76.5 | 80.4 | -3.9 | >= 10 | 82.9 | Failed |
| 11-20 | mid gear | 96.2 | 100.0 | -3.8 | >= 10 | 100.0 | Failed |
| 11-20 | best gear | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Failed |
| 21-30 | no gear | 98.3 | 99.0 | -0.6 | >= 10 | 99.3 | Failed |
| 21-30 | mid gear | 99.2 | 100.0 | -0.8 | >= 10 | 100.0 | Failed |
| 21-30 | best gear | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Failed |
| 31-40 | no gear | 99.9 | 100.0 | -0.1 | >= 10 | 100.0 | Failed |
| 31-40 | mid gear | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Failed |
| 31-40 | best gear | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Failed |
| 41-53 | no gear | 99.7 | 100.0 | -0.3 | >= 10 | 100.0 | Failed |
| 41-53 | mid gear | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Failed |
| 41-53 | best gear | 100.0 | 100.0 | -0.0 | >= 10 | 100.0 | Failed |

The new definition does not rescue gate 1 for the Circuit: the margin is -7.5 to -0.0 in every band and gear tier (per recipe: -10.4 to -0.0). The recipes of a band share one shape (the level chart scales the meters and the recipe's parameters move the Load boost and the Assembly share by a few points), so one rotation fits them nearly as well as a rotation per recipe: the single rotations score within 0 to 3 points of the per-recipe ones, and 95 to 100 at mid and best gear from level 11. The cause is the same as per recipe: Load, CP, Integrity and Assembly are deterministic, and the only chance (the Clean Signal and the overload dice) is too small for the harness's adaptive player to gain 10 points by reacting to it. The margin needs the harness change (proposal 1) or a rules change (proposal 2), not a tuning pass. No randomness or new effect was added to pass it, per the owner's decision.

### What was tried for the per-recipe gate (not committed; temporary overrides of the values above, 30 runs and 2 recipes per band)

| Variation | Margin range (points) |
| --- | --- |
| Shipped values, default heuristic | -10.4 to 0.0 (100 runs, 6 recipes) |
| Tune 1 CP unit, signal 40%, Tune 4.0%, Discharge into Calibration at half | -15.1 to +1.4 |
| CP costs scaled by recipe level, signal 40%, Tune 4.2% | -10.6 to -0.2 |
| The same with the adaptive player at depth 4, 8,000 engine copies | -12.9 to 0.0 |
| Integrity costs 5, 5, 5, 6, 3, Patch +15, signal 40%, no time preference | -24.1 to -0.1 |
| Patch +10 for 8 CP units, signal 50%, Tune 6.0%, Discharge into Calibration at 30% | -59.4 to -0.5 |
| The same with Tune 3.5% | -33.9 to -3.7 |
| Patch +10 for 20 CP units, signal 50%, Tune 2.8% | -45.2 to -2.6 |
| Shipped values with a Circuit-specific leaf heuristic (finish safety from Wire only) and no time preference | -29.4 to 0.0 (default heuristic, same settings: -16.5 to 0.0) |
| Signal 50%, Tune 2.2%, Discharge into Calibration at 25%, Circuit heuristic | -18.5 to -1.3 |
| Patch unaffordable, Tune 3.2%, signal 40%, Circuit heuristic | -32.0 to -0.1 |

No variation produced a margin of 10 in any cell (the best is +1.4). A heuristic that finishes safely and values leftover CP helps by 3 to 8 points and is not enough. The owner has ruled out new randomness and new effects to pass the per-recipe gate, so the proposals that needed them (a second telegraphed opportunity, a larger Clean Signal share) are dropped.

### Proposals that remain

1. **Let a scenario bring its own player.** A scenario hook for the adaptive player, or a leaf heuristic that can read the engine's own meters (Load), would let the Circuit be played by something that waits for signals and keeps a finish reserve. That is a harness change; without it a tuning pass cannot tell a game that is too easy from a player that is too weak.
2. **Bind the action budget.** Make Patch a once-per-craft repair, or strong enough that crafts end near 16 actions however much CP a crafter has, so a 40-action rotation cannot out-Tune the adaptive player. That is a rules change to an action the plan describes, and needs the owner's approval.

## Not covered

- Style balance is not applicable until the perks exist.
- Seconds per action (8 s for the Circuit, 6 s for the old window) are estimates.
- The multipliers and the cap are set on one sample of recipes (6 per band) and one seed; a second sample is 0.99x in two cases (see "XP multipliers").
- Client evidence: the owner deferred it to the crafting revamp closeout pass; the window, the Needs line with an enhancement and the XP of a craft were not seen in the NWN client.
