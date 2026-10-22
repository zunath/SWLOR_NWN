# Engineering Circuit: phase 2 tuning notes

Inputs and results of the CraftSim gates for the Circuit. The generated report is `circuit-gates.md` (and `circuit-gates.json`); this file records the values, what phase 2 changed, and the evidence for the one gate that does not pass.

Command: `SWLOR.CraftSim gates --scenario circuit --runs 100 --recipes-per-band 6 --seed 1 --out design/testing/crafting-revamp/circuit-gates.md --json design/testing/crafting-revamp/circuit-gates.json`. Seed 1, 100 runs per case, 6 recipes per band (an even sample of 307, 367, 359, 370 and 383 active Engineering recipes), crafter rank equal to recipe level (capped at 50), none, mid and best gear. Seconds per action is an estimate of 8 s for the Circuit (6 s for the old window); client timing replaces both.

## Results

| Gate | Verdict | Evidence |
| --- | --- | --- |
| 1 No fixed rotation | Failed | Margin of adaptive play over the best fixed rotation is -10.4 to 0.0 points in every band and gear tier; 10 are required. See below. |
| 2 Style balance | Not applicable | The Circuit has no styles until its perks exist. |
| 3 Perks optional | Passed | Rank 50, best gear, no perks: 95%, 99% and 97% of crafts reach maximum quality in bands 41-45, 46-50 and 51-53. |
| 4 Length | Passed | 14.0 to 18.0 actions per craft in every band and tier (target 10-20). |
| 5 Material parity | Passed | Stacks per item are 0.92 to 1.04 of the old window's, at higher quality in every case; the thinnest cell is 41-53 with mid gear (0.92). |
| 6 XP rate | Passed | XP per hour is 1.05x to 4.45x the old window's; the lowest is 1-10 with no gear. |

## Values

All in `CircuitRules`, `CircuitSetup` and `CircuitParameterProfile`. Phase 2 left the generator's bounds and base values alone, so the recipe parameters and the Bible columns are unchanged.

| Value | Phase 1 | Phase 2 | Why |
| --- | --- | --- | --- |
| Unlock ranks | Wire, Discharge (Assembly) 0; Tune, Discharge (Calibration) 5; Patch 10; Ground 15; Boost 20 | Patch 10, every other action 0 | Owner decision: every action that plays the loop is open from rank 0 and only the repair is gated. A Clean Signal is announced from rank 0 because Tune is open. |
| Output rank below the recipe | 5% per rank | 10% per rank (to 3 ranks below) | Recipes of levels 51-53 are crafted at rank 50. With this, items of those levels cost the old window's stacks per item at mid gear (0.92 at 5%, borderline at the first run). |
| XP multiplier | none | 2.4 on a craft's base XP, for a finished item and a failure alike (`CircuitRules.XpMultiplier`, declared by `CircuitRoute`) | Gate 6 at 1.0: the lowest case was 0.44x (1-10, no gear), so it needed 2.28x. The old window takes 5.6 to 8.8 actions at levels 1-10 and the Circuit takes 14 to 18 (10 is the floor), so XP per craft has to rise. 2.4 leaves the lowest case at 1.05x. |

Everything else is as phase 1: Wire 8.5% and Tune 5.5% of the meter at Load 0, Discharge 2.5% per Load, Load boost per recipe (8, 10 or 12% per point), overload 8% per point above the line costing 30% of maximum Integrity, Clean Signal 25% a turn and doubling Tune, CP = gear + 0.75 x rank (+31 from rank 25), costs 3, 4, 3 and 8 CP for Tune, Boost, Ground and Patch, and 3, 3, 3, 4 and 2 Integrity for Wire, Tune, Boost, Discharge and Ground.

### The XP multiplier needs an owner decision

A flat 2.4 is set by one cell. At levels 21 and above the Circuit already pays more XP per hour than the old window at 1.0, because a Circuit craft takes 14 to 18 actions where the old window takes 20 to 28 with gear. With 2.4, bands 21-53 run 2.1x to 4.5x the old rate. A multiplier that falls with level (about 2.4 up to level 10, falling to 1.0 by level 30) would keep gate 6 passing and avoid that. The plan asks for one named multiplier, so this is what ships; the plan's XP-pace workstream and the owner decide whether to shape it. The Bible record is not written: the workbook cannot merge across the parallel minigame PRs, so one "crafting XP multipliers" entry on the System Changes & Migrations tab should cover every minigame once they have landed.

## Gate 1: why tuning alone does not pass

The best fixed rotation scores 98 to 100 at every level above 10 with mid or best gear, and the adaptive player scores the same, so those cells sit at a margin of 0.0 however the rest is tuned: a margin of 10 needs the fixed rotation at 90 or less. Where quality is not capped, the adaptive player is below the fixed rotation, not above it.

Why the fixed rotation is strong: the Circuit's only chance is the Clean Signal and the overload dice. Load, CP, Integrity and Assembly are deterministic, so one sequence of up to 40 actions finds the whole optimum: build Load with Tunes up to the line, bank it into Calibration or Assembly, wire to finish. A rotation skips an unavailable action, which gives it free conditional behavior (it does not Patch at full Integrity or Discharge at zero Load). It can also run longer than the adaptive player's 14 to 18 actions: Patch trades CP for Integrity, so extra Tunes cost only CP.

Why the adaptive player is weaker where quality binds: the harness's adaptive player is a depth-3 expectimax whose heuristic reads only the shared meters and the previews. The Circuit needs plans it cannot see: keep enough Integrity to finish (a Discharge preview promises a burst that is gone once the Load is spent), wire on turns with no signal and Tune on turns with one, and spend CP only where it pays. In the runs it overspends Integrity on Tunes and cannot finish (breaks), or spends CP on Patch and Boost early.

The ceiling on what a perfect player gains from the signal is 2 / (1 + p) times what the fixed rotation gets: the fixed rotation's Tunes meet a signal by chance a share p of the time, the adaptive player puts every Tune on one. At p = 0.25 that is 1.6, and the quality has to be binding for it to show. Reaching a margin of 10 everywhere therefore needs all of: quality that deterministic play cannot saturate at every tier, an action budget that stops a 40-action rotation from simply doing more Tunes, and an adaptive player that actually plays that way.

### What was tried (not committed; temporary overrides of the values above, 30 runs and 2 recipes per band)

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

No variation produced a margin of 10 in any cell (the best is +1.4). A heuristic that finishes safely and values leftover CP helps by 3 to 8 points and is not enough.

### Proposals (none is implemented)

1. **Let a scenario bring its own player.** A scenario hook for the adaptive player, or a leaf heuristic that can read the engine's own meters (Load), would let the Circuit be played by something that waits for signals and keeps a finish reserve. That is a harness change, and it should come first: without it a tuning pass cannot tell a game that is too easy from a player that is too weak.
2. **Bind the action budget.** Make Patch a once-per-craft repair, or strong enough that crafts end near 16 actions however much CP a crafter has, so a 40-action rotation cannot out-Tune the adaptive player. That is a rules change to an action the plan describes.
3. **Make quality scarce, and reactions more valuable.** Lower Tune and Discharge output until deterministic play tops out near 80 at best gear and 50 with no gear, and raise the Clean Signal's share to about 40%. Adding a second telegraphed opportunity (for example a Stable Window: the overload line is 2 higher next turn, or the next Discharge is 50% stronger) gives a reacting player a second thing a fixed rotation cannot use. A new opportunity type is a structural change and needs the owner's approval.
4. **Change what gate 1 compares.** The solver may use up to 40 actions while the adaptive player averages 14 to 18. Capping the rotation at the adaptive player's mean length plus a few actions would compare like with like. That changes the harness's gate for every minigame.

## Not covered

- The plan's enhancement parity and enhancement-count XP cohorts are not in the harness yet; this run slots no enhancements (an enhancement's progress penalty adds to the Assembly the craft needs).
- Style balance is not applicable until the perks exist.
- Seconds per action (8 s for the Circuit, 6 s for the old window) are estimates.
