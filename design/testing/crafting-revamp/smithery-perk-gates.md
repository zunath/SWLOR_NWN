# Smithery perk lines: balance gates, the 8-action floor and XP per hour

What CraftSim says about the Forgemaster and Artisan lines and the two styles at the shipped magnitudes (`ForgePerkRules.cs`) after the tuning pass for gates 4 and 6. Reports: `forge-style-gates.md` (gates 2 and 3, 100 runs), `forge-forgemaster-gates.md`, `forge-artisan-gates.md`, `forge-both-gates.md`, `forge-both-cold-gates.md`, `forge-hot-gates.md` and `forge-cold-gates.md` (gates 4 to 7 for a crafter who bought the line or lines in full, or the style alone; 30 runs, 6 recipes per band, the adaptive player, gear none, mid and best, rank equal to the recipe's level, at most 50), and the unperked Plasma Forge's own `forge-gates.md`. A perked crafter owns every purchase of the lines that their rank allows (`ForgeLoadout`, read from the real perk definitions), so a level 30 recipe is played with the perks up to Smithery 30.

Rules this pass applied (owner decisions, October 2026; the plan's "Balance gates and tooling" has them):

- **Gate 4.** A perked build, a style included, has a floor of 8 actions in every band and gear tier (`LengthTarget.PerkedItemCraft`); the unperked Forge keeps 10. The base Forge is not changed; the perks are.
- **Gate 6.** Every build is at least 1.0x in every cell, fixed in the perk magnitudes. No perk may cost XP per hour, and the band multipliers and base XP do not move.
- **Gate 1 is open** for perked builds (see the plan). The reports of this pass do not run it and no number was tuned for it.

## Results

Before is the head of PR #2857 (the floor of 8 did not exist then, so its gate 4 column counts the cells under 8). Now is this pass.

| Build | Gate 4, before (cells under 8, lowest) | Gate 4, now | Gate 6, before | Gate 6, now |
| --- | --- | --- | --- | --- |
| Hot Forging alone | passed | passed (lowest 9.0) | 0.99x in 11-20 / no gear | passed (lowest 1.01x) |
| Cold Working alone | passed | passed (lowest 9.7) | 0.89x and 0.92x in the two no-gear, 3-enhancement cells | passed (lowest 1.01x) |
| Forgemaster + Hot Forging | 3 cells (lowest 7.4) | **1 cell: 31-40 / best gear, just under 8.0** | passed (lowest 1.16x) | passed (lowest 1.07x) |
| Artisan + Cold Working | passed (lowest 8.7) | passed (lowest 9.2) | 0.98x in 11-20 / no gear | **0.98x in 11-20 / no gear** |
| Both lines + Hot Forging | 3 cells (lowest 6.1) | **1 cell: 31-40 / best gear, 7.9** | passed (lowest 1.02x) | passed (lowest 1.01x) |
| Both lines + Cold Working | 3 cells (lowest 6.5) | passed (lowest 8.3) | passed (lowest 1.01x) | **1.00x in 11-20 / no gear, under by less than 0.005** |

Gates 5 (economy parity) and 7 (enhancement parity, lowest 1.04x to 1.05x) pass for every build. Gate 2 passes at 100 runs: Hot Forging against Cold Working with no line perks leads by -1.5 / -1.2 / -0.1 quality points at no / mid / best gear (Cold Working is the better choice on 65% / 62% / 45% of recipes) and with both lines by +0.5 / +0.5 / +0.1 (43-57% each way). Gate 3 is unchanged (86% / 83% / 57%, lowest 57%): an unperked crafter plays the same engine.

**Known gaps** (the time cap ended the tuning; each is one cell):

| Cell | Result | Why it is left | The lever |
| --- | --- | --- | --- |
| Forgemaster + Hot Forging, 31-40 / best gear, gate 4 | 8.0 shown, under 8.0 unrounded | A sampled run of the same recipes gave 8.0 to 8.1, so it is on the line | Hot Forging's Hydro-Press: removing it in the sweep took this cell from 8.0 to 9.2 |
| Both lines + Hot Forging, 31-40 / best gear, gate 4 | 7.9 | Same cell, same lever | Hot Forging's Hydro-Press, or Overdrive Press 10% to 5% |
| Artisan + Cold Working, 11-20 / no gear, gate 6 | 0.98x | The unperked Forge is at 1.01x there, and the Artisan perks below rank 20 cost XP per hour in the adaptive player's hands (see gate 6) | None found in magnitudes |
| Both lines + Cold Working, 11-20 / no gear, gate 6 | 1.00x, under by less than 0.005 | Same cell | None found in magnitudes |

## What changed

| Perk | Before | Now |
| --- | --- | --- |
| Overdrive Press (Forgemaster 15) | a Hydro-Press with 175% of the Shape | 110% |
| Hammer Rhythm (25) | +8% per press in a row, up to +24% | +4%, up to +12% |
| Steady Quench (Artisan 30) | keeps 40 points more of its bonus at the range's edge | 10 points |
| Cool Hand (35) | 20 points more at the hot edge | 10 points |
| Containment Focus (45) | +25 points to the Stable Containment bonus | +10 points |
| Perfect Temper (50) | +50% Finish at the center | +15% |
| Hot Forging (20) | range +5, Hydro-Press +25%, Vibro-Hone -5% | Hydro-Press +20% |
| Cold Working (20) | range -5, Vibro-Hone +5%, Hydro-Press -5% | range -5, Vibro-Hone +4% |

Every other value is as in PR #2857. The Bible rows (`tools/UpdateSmitheryPerkBible.py`), the perk and style descriptions and the plan's Smithery rows follow these values.

## Gate 2: the styles, third round

| Round | Hot Forging | Cold Working | Gap (no / mid / best gear) | Verdict |
| --- | --- | --- | --- | --- |
| Draft | range +10, Hydro-Press +25%, Vibro-Hone -25% | range -10, Vibro-Hone +25%, Hydro-Press -25% | -2.7 / -7.6 / -6.3 | Failed: Cold Working led by more than 5 at mid and best gear |
| Round 1 | range +10, Hydro-Press +25%, Vibro-Hone -5% | range -10, Vibro-Hone +10%, Hydro-Press -25% | -1.8 / -3.3 / -1.8 | Passed gate 2, but gate 6 failed in 12 cells of enhanced crafts (down to 0.49x) |
| Round 2 (PR #2857) | range +5, Hydro-Press +25%, Vibro-Hone -5% | range -5, Vibro-Hone +5%, Hydro-Press -5% | -1.5 / -2.7 / -1.4 | Passed; gate 6 still failed in 3 cells |
| **Now** | Hydro-Press +20% | range -5, Vibro-Hone +4% | -1.5 / -1.2 / -0.1 (the gate, 100 runs) | Passed; the better-style shares are 35-65% |

Why neither style may cost anything: in crafts with enhancements, and in the no-gear cells where the unperked Forge is at 1.00x to 1.01x, any style cost pushed the build under 1.0x. A screening of the same recipes (6 per band, 12 runs, ratio to the crafter with no style) put Cold Working's Hydro-Press cost of -5% at 0.94, 0.94 and 0.99 in the three no-gear, 3-enhancement cells of 21-53, and removing it at 1.03 or better. Cold Working's range shift of -5 helps in those enhanced crafts (the same screening with the shift at 0 put one cell at 1.00), so it stays; Hot Forging's shift of +5 cost the 11-20 / no gear cell (1.00 against 1.04 with the shift at 0), so it went. With no cost on either side the choice between them is the trade-off (only one style is active), and the numbers are the ones the gate-2 sweeps balanced.

## Gate 4: the floor of 8

The adaptive player turns any spare power into fewer actions: once the quality meter is full it stops, and it values an action at 0.5% of the score. At best gear the unperked Forge needs 10.1 to 10.7 actions for a rank-50 craft at 91.7 quality; the strongest perk lines took that to 6.1 to 6.5 for 100 quality. So the cuts had to come from the perks that add the most power.

What the one-factor sweeps showed (the both-lines build at best gear in 41-53, the cell with the fewest actions; positive is a longer craft without the perk; samples of 3 recipes and 20 runs, so differences under 0.3 are noise):

| Remove | Change in actions |
| --- | --- |
| Overdrive Press entirely | +1.8 |
| Steady Quench and Perfect Temper | +1.3 |
| Hot Forging's Hydro-Press (forgemaster build, 31-40 / best gear) | +1.2 |
| Cold Working's range shift | +0.8 |
| Heat Retention | +0.8 |
| Both styles | +0.9 |
| Clean Containment and Containment Focus | +0.4 |
| Temper Hold | +0.4 |
| Plasma Efficiency, Steady Hands, Surge Plasma, Fast Rebond, Red Line | under 0.2 |

Halving every perk magnitude only moved the strongest build from 6.3 to 7.3, because a perk's existence matters more than its size (an action that is available is used); the Overdrive Press, Steady Quench, Perfect Temper and Hot Forging cuts above are the ones that reached the floor. The mean actions of each build in the gate runs, at the cells that matter (the floor is 8):

| Build | Lowest cell | Next | Next |
| --- | --- | --- | --- |
| Hot Forging alone | 31-40 / best gear: 9.0 | 41-53 / best gear: 9.2 | 21-30 / best gear: 9.8 |
| Cold Working alone | 31-40 / best gear: 9.7 | 41-53 / best gear: 9.9 | 21-30 / best gear: 10.4 |
| Forgemaster + Hot Forging | 31-40 / best gear: 8.0 | 41-53 / best gear: 8.5 | 21-30 / best gear: 9.4 |
| Artisan + Cold Working | 31-40 / best gear: 9.2 | 21-30 / best gear: 9.9 | 41-53 / best gear: 10.2 |
| Both lines + Hot Forging | 31-40 / best gear: 7.9 | 41-53 / best gear: 8.1 | 21-30 / best gear: 8.9 |
| Both lines + Cold Working | 31-40 / best gear: 8.3 | 41-53 / best gear: 8.8 | 21-30 / best gear: 9.3 |

The unperked Forge keeps the floor of 10 (10.1 to 10.7 at best gear).

## Gate 6: no perk costs XP per hour

XP per craft is the base times (1 + quality), so an extra action costs about 8% of a 12-action craft while ten more quality points earn about 6%. The adaptive player buys quality with actions at 0.5% an action, so a perk that makes quality cheaper (Steady Hands lowers a Vibro-Hone's Integrity, Micro-Hone adds Finish at any Thermal) can lower XP per hour in the cells where the unperked Forge has no headroom: 11-20 / no gear (1.01x), 1-10 / mid gear with 3 enhancements (1.01x) and the no-gear, 3-enhancement cells of 21-53 (1.00x to 1.01x). The Forgemaster line shortens crafts, so it has headroom (lowest 1.07x); the cuts to its perks cost it some.

Isolating the Artisan perks at 11-20 / no gear (6 recipes, 20 runs, plain crafts and 1 enhancement, the ratio to a crafter with no perks):

| Artisan perks owned | Ratio |
| --- | --- |
| None (control) | 1.00 |
| Steady Hands -2 Integrity only | 0.96 |
| Steady Hands -1 only | 0.94 |
| Clean Containment 5% / 10% only | 1.02 |
| Micro-Hone 40% only | 0.96 |
| Micro-Hone 80% only | 1.01 |
| Micro-Hone 100% only | 1.04 |
| Steady Hands -2, Clean Containment 5% / 10% and Micro-Hone 40% together | 0.89 |

Six combinations of Steady Hands (1 to 3), Clean Containment (5% / 10% or 10% / 20%) and Micro-Hone (80% to 120%) over 1-10 and 11-20 left the two tight cells at 0.96 to 1.00, Steady Hands 3 at 0.85; the best, 1.00 and 1.01, needed Micro-Hone at 120%, better than the Vibro-Hone it copies. The shipped Artisan values at ranks 5 to 15 give 0.98 in 1-10 / mid gear with 3 enhancements, so they stay, and 11-20 / no gear is a known gap.

## Gate 1

Open for the Circuit, the Fabricator and perked builds (the plan has the note). These reports do not run it.

## What each perk buys

The table is produced by the explicit test `ForgePerkEffects` (rank 50, no and mid gear, an even sample of 6 recipes per band, 12 runs; run it with `dotnet test SWLOR.CraftSim.Tests --filter "FullyQualifiedName~ForgePerkEffects"`, which writes `perk-effects.md` under `%TEMP%/claude-crafting-revamp-perks-smithery-tuning/`). Its run is about 35,000 adaptive crafts at rank 50 and had not finished when this pass ended, so the table is not in this report yet.

## Limitations

- CraftSim's adaptive player is a depth-4 search on the previews (6000 engine copies per decision). It plays the Forge well and spends power as the rules above describe, so the length and XP figures are for skilled play, and a perk it does not value (Scorch Guard, Surge Plasma, Red Line) looks inert. Seconds per action (8) are unmeasured, so gate 6 is as soft as before.
- The perked scenarios run 30 times per case (the style gates 100), so a cell within 0.05 of a threshold (gate 4's 8.0 and 7.9, gate 6's 1.00x) is a coin toss; the tuning sweeps used smaller samples (3 to 6 recipes per band, 12 to 24 runs) and are noisy to about 0.3 actions and 0.03 in the XP ratio.
- Gates 4 to 7 for a perked crafter play one fixed loadout (each line in full); a crafter who bought part of a line sits between the unperked and the full-line figures.
- The perk table comes from the explicit test `ForgePerkEffects` in `SWLOR.CraftSim.Tests`; the sweep tables come from a scratch sweep over the same engine and policy (not committed); the length table is the gate runs'.
- No client was run.
