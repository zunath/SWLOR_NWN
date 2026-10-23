# Engineering perk lines: balance gates and the length shortfall

What CraftSim says about the Overclocker and Technician lines at the shipped magnitudes (`CircuitPerkRules.cs`). Reports: `circuit-style-gates.md` (gates 2 and 3), `circuit-overclocker-gates.md`, `circuit-technician-gates.md` and `circuit-both-gates.md` (gates 1, 4, 5, 6 and 7 for a crafter who bought the line or lines in full). The unperked Circuit's own report is `circuit-gates.md`. All runs: seed 1, 100 runs per case, 6 recipes per band, the adaptive player, gear none, mid and best, rank equal to the recipe's level (at most 50). A perked crafter owns every purchase of the lines that their rank allows (`CircuitLoadout`, read from the real perk definitions), so a level 30 recipe is played with the perks up to Engineering 30.

Scenarios (`gates --scenario <name>`):

| Scenario | Crafter | Gates run |
| --- | --- | --- |
| `circuit` | no perks; gate 2 plays Overclock against Safe Mode with no line perks and with both lines bought in full | 2, 3 |
| `circuit-overclocker` | Overclocker line in full, Overclock | 1, 4, 5, 6, 7 |
| `circuit-technician` | Technician line in full, Safe Mode | 1, 4, 5, 6, 7 |
| `circuit-both` | both lines in full, Overclock | 1, 4, 5, 6, 7 |

## Results

| Gate | Unperked (`circuit-gates.md`) | Overclocker | Technician | Both lines |
| --- | --- | --- | --- | --- |
| 1 No single rotation | Failed (smallest margin -7.5) | Failed (-7.0) | Failed (-6.8) | Failed (-5.9) |
| 2 Style balance | NotApplicable | Passed in both pairs, every gear tier (see the caveat below) | | |
| 3 Perks optional | Passed (lowest 95%) | Passed (lowest 95%, unchanged: no style means no perks) | | |
| 4 Length | Passed | **Failed** in 2 cells: 31-40 best gear 9.7, 41-53 best gear 9.2 | Passed | **Failed** in 3 cells: 31-40 best gear 9.5, 41-53 mid gear 9.8, 41-53 best gear 8.5 |
| 5 Economy parity | Passed | Passed | Passed | Passed |
| 6 XP rate | Passed | Passed (lowest 1.03x) | Passed (lowest 1.02x) | Passed (lowest 1.04x) |
| 7 Enhancement parity | Passed (lowest 1.08x) | Passed (1.08x) | Passed (1.08x) | Passed (1.08x) |

- **Gate 1 fails for the unperked Circuit already** (`circuit-tuning.md`, "Gate 1"): the margin is the same kind of failure and no perked line makes it worse, so the perks neither cause nor fix it.
- **Gate 2 passes, but quality saturates.** At mid and best gear both styles reach about 100 quality on at-level recipes, so almost every recipe is a tie (half a win each) and the table reads 50% / 50%. Only "no gear" separates the styles (Overclock 62% / Safe Mode 38% with no line perks; 57% / 42% with both lines, mean gap +0.7 and +1.2 points). The gate cannot see speed, which is where they differ; see below.
- **Gate 4 is the finding.** It is the one gate that passes for the unperked Circuit and fails for a perked build. Gates 5, 6 and 7 hold for every build, and gate 1 fails for every build as it already does unperked.

## Gate 4: why the Overclocker cannot keep every cell above 10 actions

The adaptive player's mean actions at best gear for each build (`CircuitPerkLengthBudget.MeanActionsAtBestGear`, an explicit test: 6 sampled recipes per band, 16 runs each, rank equal to the recipe's level, so the perks are the ones that rank has bought; the gate runs above use 100 runs). Under 10 is below the gate's floor.

| Band | Build | Draft magnitudes | Shipped magnitudes |
| --- | --- | --- | --- |
| 21-30 | No perks | 13.9 | 13.9 |
| 21-30 | Overclock alone | 11.3 | 11.7 |
| 21-30 | Safe Mode alone | 14.4 | 14.4 |
| 21-30 | Overclocker, no style | 11.6 | 12.1 |
| 21-30 | Overclocker + Overclock | 9.9 | 10.6 |
| 21-30 | Technician, no style | 13.2 | 13.2 |
| 21-30 | Technician + Safe Mode | 13.6 | 13.6 |
| 21-30 | Both lines + Overclock | 9.6 | 10.0 |
| 21-30 | Both lines + Safe Mode | 11.9 | 12.0 |
| 31-40 | No perks | 14.2 | 14.2 |
| 31-40 | Overclock alone | 11.2 | 11.6 |
| 31-40 | Safe Mode alone | 14.5 | 14.5 |
| 31-40 | Overclocker, no style | 10.7 | 10.8 |
| 31-40 | Overclocker + Overclock | 9.1 | 10.0 |
| 31-40 | Technician, no style | 13.1 | 13.1 |
| 31-40 | Technician + Safe Mode | 13.7 | 13.7 |
| 31-40 | Both lines + Overclock | 8.6 | 9.4 |
| 31-40 | Both lines + Safe Mode | 10.3 | 10.5 |
| 41-53 | No perks | 13.5 | 13.5 |
| 41-53 | Overclock alone | 10.8 | 11.3 |
| 41-53 | Safe Mode alone | 14.1 | 14.1 |
| 41-53 | Overclocker, no style | 9.1 | 9.6 |
| 41-53 | Overclocker + Overclock | 7.9 | 8.7 |
| 41-53 | Technician, no style | 12.4 | 12.4 |
| 41-53 | Technician + Safe Mode | 12.9 | 12.9 |
| 41-53 | Both lines + Overclock | 7.2 | 8.2 |
| 41-53 | Both lines + Safe Mode | 9.5 | 10.0 |

The Overclock style by itself takes about 15-20% off an unperked best-gear craft, because its extra Load per Wire and Tune puts the player at a high Load sooner, and every Overclocker perk (Conductive Wiring, Capacitor Bank, Heat Sinks, Cascade Discharge, Overdrive Boost) does the same job for a few percent more. At best gear the unperked craft is 13-14 actions, so there are about 3 actions between it and the length gate's floor of 10, and the line plus its style uses more than that.

What was changed: three magnitudes came down from the draft (Conductive Wiring II +3 to +2, Capacitor Bank 10% to 5%, Overclock's boost per Load point +3 to +1). That took the strongest build's top-band cell from 7.2 to 8.2 actions in the experiment (7.0 to 8.5 in the gate runs) and left every ranked perk's second rank larger than its first. Every other value is as drafted.

What cannot be changed inside the approved rows: with Overclock's +1 Load per Wire and Tune and every other perk cut to its smallest integer, a build with both lines and Overclock still only reached 9.9 actions in the top band (a scratch run at fixed rank 50 that also left Heat Sinks, Conductive Wiring and Insulated Traces with equal ranks), and a ranked perk whose second rank equals its first is a purchase that does nothing. The Technician line needs nothing: it passes at its drafted magnitudes, because its perks mostly buy Integrity and information instead of output.

Options for the owner, none of which is in this PR:

1. **Give the perks headroom in the base tuning.** Lower `WirePercent`, `TunePercent` and `DischargePercentPerLoad` in `CircuitRules.cs` by roughly 20% (an estimate, not run: the unperked best-gear craft would go from 13-14 to about 16-17 actions, and the strongest build, at 8.2 above, would land near 10), then re-run gates 5 and 6 and the XP multipliers, which are tuned to today's lengths. The perk magnitudes could then go back up toward the draft.
2. **Accept shorter crafts for the strongest builds.** Gate 4's floor stays 10 for an unperked crafter and becomes 8 for one who spent 25 to 50 SP on Engineering perks. The shortest cell is 8.5 actions.
3. **Change the Overclock style** (a design change): a real cost, such as a higher overload chance per point, or its extra Load on Wire only.

## Style balance beyond quality

A comparison where quality does not saturate (6 sampled recipes per band, 16 runs each, the adaptive player). Rows: Overclock / Safe Mode, no line perks.

| Condition | Mean actions | Quality | Reach full quality |
| --- | --- | --- | --- |
| Band 21-30, at level, no gear | 15.4 / 18.4 | 99.5 / 98.5 | 95% / 96% |
| Band 21-30, 3 ranks under the recipe, no gear | 18.7 / 20.8 | 94.4 / 92.6 | 74% / 52% |
| Band 21-30, at level, no gear, heavy enhancement | 18.0 / 21.6 | 96.3 / 98.8 | 92% / 97% |
| Band 41-53, at level, no gear | 14.2 / 17.7 | 100.0 / 99.9 | 100% / 90% |
| Band 41-53, 3 ranks under, no gear | 16.3 / 19.9 | 99.9 / 99.9 | 97% / 93% |

Overclock is 10-20% faster in every row and has the better or equal quality in four of the five. Safe Mode is the better choice in one: the heavy enhancement at level 21-30 with no gear, where it is 2.5 points ahead and makes the full-quality item more often. With both lines bought it wins none of the rows measured (the closest, a heavy enhancement 3 ranks under in band 21-30 with mid gear, is a tie; at 3 ranks under without gear Overclock reaches full quality on 97% of crafts against 79%), so Safe Mode's grace is not buying more than Overclock's extra Load costs. If the owner wants Safe Mode to be a real choice, it needs something Overclock lacks, or Overclock needs a cost (option 3 above).

## Limitations

- CraftSim's adaptive player is a depth-3 search on the previews and plays the Circuit very well, so the length and style figures are for skilled play. Seconds per action (8 s) are unmeasured, so gate 6 is as soft as before.
- Gates 4 to 7 for a perked crafter play one fixed loadout (each line in full); a crafter who bought part of a line sits between the unperked and the full-line figures.
- The `circuit-both` report was run after CodeRabbit's finding that Surge's Discharge skipped Failsafe (fixed in `db275fd77`); the `circuit-overclocker` and `circuit-technician` reports have only one of the two capstones, so the fix does not change them.
- The tables in "Gate 4" and "Style balance beyond quality" come from two explicit tests, `CircuitPerkLengthBudget` and `CircuitStyleStress.Compare` in `SWLOR.CraftSim.Tests` (run them with `dotnet test SWLOR.CraftSim.Tests --filter "FullyQualifiedName=..."`). They are the same engine and policy as the gates on a smaller sample, written to explain the gate results, not gates themselves.
- No client was run.
