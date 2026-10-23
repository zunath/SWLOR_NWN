# Smithery perk lines: balance gates and the length shortfall

What CraftSim says about the Forgemaster and Artisan lines and the two styles at the shipped magnitudes (`ForgePerkRules.cs`). Reports: `forge-style-gates.md` (gates 2 and 3), `forge-forgemaster-gates.md`, `forge-artisan-gates.md`, `forge-both-gates.md` and `forge-both-cold-gates.md` (gates 1, 4, 5, 6 and 7 for a crafter who bought the line or lines in full), and the unperked Plasma Forge's own report, `forge-gates.md`. All runs: seed 1, 6 recipes per band, the adaptive player, gear none, mid and best, rank equal to the recipe's level (at most 50). The style gates run 100 times per case; the perked scenarios run 30 times per case, because the adaptive player's search makes 100 runs of every perked cell take several hours on a shared machine (the unperked report is 100 runs). A perked crafter owns every purchase of the lines that their rank allows (`ForgeLoadout`, read from the real perk definitions), so a level 30 recipe is played with the perks up to Smithery 30.

Scenarios (`gates --scenario <name>`):

| Scenario | Crafter | Gates run |
| --- | --- | --- |
| `forge` | no perks; gate 2 plays Hot Forging against Cold Working with no line perks and with both lines bought in full | 2, 3 |
| `forge-forgemaster` | Forgemaster line in full, Hot Forging | 1, 4, 5, 6, 7 |
| `forge-artisan` | Artisan line in full, Cold Working | 1, 4, 5, 6, 7 |
| `forge-both` | both lines in full, Hot Forging | 1, 4, 5, 6, 7 |
| `forge-both-cold` | both lines in full, Cold Working | 1, 4, 5, 6, 7 |
| `forge-hot`, `forge-cold` | a style alone, no line perks | 6 (the rest are the unperked report's, apart from the style) |

## Results

| Gate | Unperked (`forge-gates.md`) | Forgemaster + Hot Forging | Artisan + Cold Working | Both lines + Hot Forging | Both lines + Cold Working |
| --- | --- | --- | --- | --- | --- |
| 1 No single rotation | Passed (smallest margin 12.0) | Passed (12.5) | **Failed** in 3 cells: 1-10 best gear, 41-53 mid and best gear (smallest margin 5.8) | **Failed** in 4 best-gear cells (smallest margin 1.3) | **Failed** in 5 cells: 1-10, 11-20, 31-40 and 41-53 best gear, 41-53 mid gear (smallest margin 3.2) |
| 2 Style balance | Not applicable | Passed (`forge`, 100 runs): gap -1.5 / -2.7 / -1.4 points at no / mid / best gear with no line perks, -0.6 / -0.3 / +0.1 with both lines | same comparison | same comparison | same comparison |
| 3 Perks optional | Passed (lowest 57%) | Passed, unchanged (86% / 83% / 57%): an unperked crafter plays the same engine | same | same | same |
| 4 Length | Passed (shortest cell 10.1) | **Failed** in 9 cells (lowest 7.4) | **Failed** in 3 cells (lowest 8.7) | **Failed** in 8 cells (lowest 6.1) | **Failed** in 8 cells (lowest 6.5) |
| 5 Economy parity | Passed | Passed | Passed | Passed | Passed |
| 6 XP rate | Passed (lowest 1.00x) | Passed (lowest 1.16x) | **Failed** in 1 cell: 11-20 / no gear, 0.98x | Passed (lowest 1.02x) | Passed (lowest 1.01x) |
| 7 Enhancement parity | Passed (1.05x) | Passed (1.05x) | Passed (1.05x) | Passed (1.05x) | Passed (1.05x) |

A style alone (`forge-hot`, `forge-cold`, gate 6 only, 30 runs): Hot Forging fails one cell (11-20 / no gear, 0.99x); Cold Working fails two, both "no gear, 3 enhancements" (21-30 at 0.89x and 41-53 at 0.92x). The unperked Forge is already at 1.00x in 41-53 / no gear / 3 enhancements, so any style that costs the crafter something in that cell falls under the gate.

- **Gate 2 and gate 3 pass.** The styles needed two tuning rounds (below). Gate 3 is untouched: a crafter with no perks and no style plays the same engine with the same dice (`forge-style-gates.md` reproduces the unperked figures: 86% / 83% / 57%).
- **Gates 5 and 7 pass for every build.** Perks and styles change the length and the quality of the craft, not the material it takes, and the enhancement thresholds still transfer at least today's enhancements.
- **Gate 4 is the finding**, as it is for Engineering: it passes for the unperked Forge by a small margin and fails for every build that saves actions. See below.
- **Gate 1 fails for the builds that reach full quality.** See below.

## Gate 2: how the two styles were tuned

Hot Forging is the Forgemaster's style (Shape) and Cold Working the Artisan's (Finish). Gate 2 plays them against each other on at-level crafts with no line perks and with both lines bought: each must be the better choice on at least 30% of the sampled recipes, and neither may lead by more than 5 quality points on average. The sweep used the same engine and adaptive player as the gate, at smaller samples (population runs: 20 recipes per band, 8 runs each; gate-sample runs: the gate's own 6 recipes per band, 24 runs each), and every figure below is the mean quality gap of Hot Forging minus Cold Working at no, mid and best gear.

| Round | Hot Forging | Cold Working | Gap (no / mid / best gear) | Verdict |
| --- | --- | --- | --- | --- |
| Draft | range +10, Hydro-Press +25%, Vibro-Hone -25% | range -10, Vibro-Hone +25%, Hydro-Press -25% | -2.7 / -7.6 / -6.3 | Failed: Cold Working led by more than 5 at mid and best gear |
| Round 1 | range +10, Hydro-Press +25%, Vibro-Hone -5% | range -10, Vibro-Hone +10%, Hydro-Press -25% | -1.8 / -3.3 / -1.8 (the gate, 100 runs) | Passed gate 2, but see gate 6 below |
| **Shipped** | range +5, Hydro-Press +25%, Vibro-Hone -5% | range -5, Vibro-Hone +5%, Hydro-Press -5% | -1.5 / -2.7 / -1.4 (the gate, 100 runs) | Passed; better-style shares 40-60% |

- **Why the draft failed.** A point of Finish is worth more than a point of Shape: the craft ends when Shape is full, so Finish is what the quality is made of, and a mirrored +25% / -25% gave Cold Working the better trade. Round 1 cut the two Finish numbers until the plain craft balanced.
- **Why round 1 was not shipped.** A style must not make the crafter earn less XP per hour than the legacy loop (gate 6), and enhancements make the Shape bar longer. In crafts with one to three enhancements, Cold Working's -25% Hydro-Press made the crafter earn 0.42 to 0.87 times the XP rate of a crafter with no style (mean 0.63, 18 of 18 cells under 0.9) and made the item in only 51% of those crafts on average; the CraftSim gate 6 for Cold Working alone failed 12 cells, down to 0.49x. The sweep of the style's numbers in those crafts (12 runs, 6 recipes per band, bands 21-53, no and mid gear, 1 to 3 enhancements; ratio to the no-style crafter's XP per action, mean and lowest of 18 cells):

| Cold Working | Mean | Lowest | Cells under 0.9 |
| --- | --- | --- | --- |
| range -10, Vibro-Hone +10%, Hydro-Press -25% (round 1) | 0.63 | 0.42 | 18 |
| range -10, Vibro-Hone +10%, Hydro-Press -10% | 0.89 | 0.71 | 7 |
| range -10, Vibro-Hone +5%, Hydro-Press -10% | 0.89 | 0.70 | 9 |
| range -10, Vibro-Hone +5%, Hydro-Press -5% | 0.98 | 0.76 | 2 |
| range -6, Vibro-Hone +10%, Hydro-Press -10% | 0.90 | 0.75 | 8 |
| **range -5, Vibro-Hone +5%, Hydro-Press -5% (shipped)** | 0.97 | 0.88 | 2 |

  Hot Forging in the same crafts earns 0.98 to 1.88 times the no-style rate (mean 1.26) at the shipped numbers. The Hydro-Press cost is what hurts Cold Working in enhanced crafts; its Vibro-Hone bonus changes little there.
- **The shipped numbers** keep the shape of the draft (Hot Forging is faster and rougher, Cold Working slower and cleaner) at a size where the style stays a preference and not a penalty. Both ranges move 5 Thermal where the draft moved them 10, and Cold Working's Finish and Shape numbers are small; the plan, the draft's "As implemented" section and the Bible say so.
- **With both lines bought** the styles tie: the gate 2 gaps are -0.6 / -0.3 / +0.1 with shares of 45-52%, because both reach 99 to 100 quality on at-level recipes and almost every recipe is a tie.

## Gate 4: why a build that saves actions cannot keep every cell above 10

The adaptive player's mean actions at best gear for each build (`ForgePerkLengthBudget`, an explicit test: 6 sampled recipes per band, 16 runs each, rank equal to the recipe's level, so the perks are the ones that rank has bought; the gate runs above use 30). Under 10 is below the gate's floor.

| Build | 21-30 | 31-40 | 41-53 |
| --- | --- | --- | --- |
| No perks | 10.3 | 10.4 | 10.7 |
| Hot Forging alone | 9.7 | 9.2 | 9.2 |
| Cold Working alone | 9.8 | 10.1 | 10.6 |
| Forgemaster, no style | 8.3 | 7.7 | 8.1 |
| Forgemaster + Hot Forging | 8.8 | 7.9 | 7.2 |
| Forgemaster + Cold Working | 8.8 | 7.8 | 7.9 |
| Artisan, no style | 10.2 | 8.5 | 9.4 |
| Artisan + Cold Working | 9.7 | 8.3 | 9.5 |
| Artisan + Hot Forging | 9.8 | 8.2 | 8.7 |
| Both lines + Hot Forging | 8.5 | 7.0 | 6.2 |
| Both lines + Cold Working | 8.2 | 6.9 | 6.4 |

The unperked Forge is at the floor: 10.3, 10.4 and 10.7 actions at best gear in 21-30, 31-40 and 41-53, and 10.1 to 10.6 in the gate run. Any perk that saves an action pushes the cell under 10, so the gate cannot pass for a crafter who bought one. The styles move the length a little (Hot Forging alone takes 0.6 to 1.5 off at best gear, Cold Working alone 0.5 or less). The largest single saving is **Overdrive Press** (Forgemaster 15): removing it from the strongest build adds 1.55 actions on average (up to 2.2 in a cell). Everything else the Forgemaster line does is small by comparison (below).

Every line magnitude is as the owner approved it; none was weakened to chase this gate, because there is no value that passes while the perk still saves an action.

Options for the owner, none of which is in this PR:

1. **Give the perks headroom in the base tuning.** Slow the base Forge by roughly 20% (smaller Shape per Hydro-Press, an estimate, not run), so an unperked best-gear craft is about 12.5 actions and the strongest build lands near 7.5, then re-run gates 5 and 6, which are tuned to today's lengths, and the XP multipliers. The perk magnitudes could then go back up toward the draft where they are small.
2. **Accept shorter crafts for the strongest builds.** Gate 4's floor stays 10 for an unperked crafter and becomes 6 for one who spent 25 to 50 SP on Smithery perks. The shortest cell is 6.1 actions (both lines + Hot Forging, 41-53, best gear).
3. **Weaken Overdrive Press** (a design change) or give it a real cost, such as a cooldown of several actions.

## Gate 1: why a full build cannot hold a 10-point margin

With both lines the adaptive player reaches 99 to 100 quality at best gear (`forge-both-gates.md`) while a single fixed rotation for a whole band reaches 91 to 98 in the same cells (1-10: 90.9, 11-20: 97.7, 31-40: 93.5, 41-53: 95.1), so the margin falls to 1.3 to 8.9 points. The gate asks whether one rotation wins every craft of a band; once the player's own score is capped at 100 there is little left for adaptation to add. The Forgemaster alone passes (smallest margin 12.5), and the Artisan line fails three cells (smallest margin 5.8, 41-53 mid gear). This is a saturation effect, not a rotation that works for every recipe: the single rotation is still 36 points behind in the 21-30 no-gear and mid-gear cells. The Engineering report has the same finding for its perked builds (`engineering-perk-gates.md`).

## Gate 6: the cells under 1.00x

- Artisan + Cold Working and Hot Forging alone each miss 11-20 / no gear (0.98x and 0.99x); the unperked Forge is at 1.01x there, so a style that costs anything falls under the gate, and the gap is within the noise of 30 runs. Cold Working alone misses the two "no gear, 3 enhancements" cells (0.89x at 21-30, 0.92x at 41-53): the unperked Forge is at 1.00x in 41-53, so a style whose Hydro-Press costs 5% cannot pass there. Making the Hydro-Press cost zero would pass those cells but would put Cold Working ahead in gate 2 and need Hot Forging or the Finish bonus rebalanced; the alternative is a base with headroom (gate 4's option 1).

## Style balance beyond quality

A comparison where quality does not saturate (6 sampled recipes per band, 16 runs each, the adaptive player): crafts under the recipe's rank, with no gear, and with a heavy enhancement (a 40% Shape penalty), for a crafter with no line perks and with both lines in full.

### No line perks

| Band | Condition | Mean actions (Hot / Cold) | Make the item | Quality | Reach full quality | Better quality |
| --- | --- | --- | --- | --- | --- | --- |
| 21-30 | at level, no gear | 10.1 / 11.4 | 100% / 100% | 75.8 / 87.1 | 28% / 38% | Cold Working (-11.3) |
| 21-30 | 3 ranks under, no gear | 11.2 / 11.8 | 91% / 91% | 62.4 / 70.3 | 25% / 29% | Cold Working (-7.9) |
| 21-30 | at level, no gear, heavy enhancement | 11.6 / 12.8 | 97% / 96% | 73.3 / 78.9 | 32% / 30% | Cold Working (-5.6) |
| 21-30 | 3 ranks under, mid gear, heavy enhancement | 13.9 / 13.2 | 99% / 99% | 85.2 / 75.5 | 36% / 20% | Hot Forging (+9.7) |
| 31-40 | at level, no gear | 11.2 / 11.5 | 99% / 96% | 83.6 / 76.7 | 44% / 25% | Hot Forging (+6.9) |
| 31-40 | 3 ranks under, no gear | 11.7 / 12.0 | 97% / 95% | 71.2 / 67.9 | 5% / 8% | Hot Forging (+3.3) |
| 31-40 | at level, no gear, heavy enhancement | 12.0 / 12.2 | 90% / 94% | 73.5 / 70.1 | 26% / 17% | Hot Forging (+3.4) |
| 31-40 | 3 ranks under, mid gear, heavy enhancement | 13.0 / 13.0 | 98% / 97% | 81.6 / 72.7 | 30% / 17% | Hot Forging (+8.9) |
| 41-53 | at level, no gear | 11.0 / 11.3 | 100% / 96% | 82.0 / 78.6 | 49% / 49% | Hot Forging (+3.4) |
| 41-53 | 3 ranks under, no gear | 11.4 / 11.6 | 98% / 97% | 76.1 / 71.0 | 32% / 25% | Hot Forging (+5.1) |
| 41-53 | at level, no gear, heavy enhancement | 11.5 / 11.7 | 100% / 98% | 79.1 / 79.4 | 43% / 42% | tie (-0.3) |
| 41-53 | 3 ranks under, mid gear, heavy enhancement | 12.1 / 12.7 | 100% / 98% | 87.7 / 83.3 | 52% / 42% | Hot Forging (+4.4) |

Hot Forging better in 8 rows, Cold Working in 3, ties 1; mean difference +1.7 (Hot minus Cold), range -11.3 to +9.7.

### Both lines in full

| Band | Condition | Mean actions (Hot / Cold) | Make the item | Quality | Reach full quality | Better quality |
| --- | --- | --- | --- | --- | --- | --- |
| 21-30 | at level, no gear | 10.9 / 10.4 | 100% / 100% | 92.7 / 92.4 | 55% / 67% | tie (+0.3) |
| 21-30 | 3 ranks under, no gear | 11.1 / 11.8 | 94% / 97% | 71.7 / 81.8 | 23% / 39% | Cold Working (-10.1) |
| 21-30 | at level, no gear, heavy enhancement | 11.2 / 11.9 | 96% / 96% | 84.5 / 84.5 | 45% / 49% | tie (+0.0) |
| 21-30 | 3 ranks under, mid gear, heavy enhancement | 11.5 / 12.2 | 100% / 95% | 90.2 / 84.5 | 47% / 49% | Hot Forging (+5.7) |
| 31-40 | at level, no gear | 9.8 / 9.3 | 100% / 100% | 99.6 / 99.6 | 81% / 75% | tie (+0.0) |
| 31-40 | 3 ranks under, no gear | 11.4 / 11.2 | 100% / 97% | 97.0 / 92.6 | 57% / 52% | Hot Forging (+4.4) |
| 31-40 | at level, no gear, heavy enhancement | 10.4 / 10.5 | 99% / 98% | 97.9 / 90.6 | 74% / 66% | Hot Forging (+7.3) |
| 31-40 | 3 ranks under, mid gear, heavy enhancement | 11.1 / 11.5 | 100% / 99% | 99.5 / 98.1 | 77% / 77% | Hot Forging (+1.4) |
| 41-53 | at level, no gear | 8.0 / 8.7 | 100% / 100% | 99.9 / 99.2 | 96% / 82% | Hot Forging (+0.7) |
| 41-53 | 3 ranks under, no gear | 10.4 / 10.9 | 100% / 99% | 98.5 / 96.3 | 72% / 77% | Hot Forging (+2.2) |
| 41-53 | at level, no gear, heavy enhancement | 9.1 / 9.3 | 100% / 94% | 99.8 / 89.6 | 85% / 72% | Hot Forging (+10.2) |
| 41-53 | 3 ranks under, mid gear, heavy enhancement | 10.2 / 10.8 | 100% / 99% | 99.9 / 96.2 | 92% / 78% | Hot Forging (+3.7) |

Hot Forging better in 8 rows, Cold Working in 1, ties 3; mean difference +2.2 (Hot minus Cold), range -10.1 to +10.2.

With no line perks Hot Forging has the better quality in 8 of 12 rows and Cold Working in 3, and Cold Working no longer fails the piece in the under-ranked and enhanced rows (it makes the item in 91% to 99% of them, against 65% to 95% with round 1's Hydro-Press cost). Cold Working leads in three 21-30 rows (+5.6 to +11.3 quality), Hot Forging in the 31-53 rows and in the heavy-enhancement mid-gear rows (+8.9 to +9.7 at 21-40). With both lines Hot Forging leads in 8 of 12 rows by up to 10 points (41-53, heavy enhancement) and Cold Working in one (21-30, 3 ranks under, +10.1). The styles are close enough that the choice depends on the recipe; Hot Forging is the safer pick when the piece is hard and the crafter is strong.

## Which perks do the work

Removing one perk at a time from the both-lines build with Hot Forging, at ranks 21 to 53 (the three at-level bands 21-30, 31-40 and 41-53 at no and mid gear, 16 runs each, 6 recipes per band; the change is the average over the six cells and the largest change in one cell; positive actions means the craft is longer without the perk):

| Removed perk | Actions (mean change) | Quality (mean change) | Full quality (mean change, points) | Largest change in one cell: actions / quality |
| --- | --- | --- | --- | --- |
| Plasma Efficiency | -0.08 | -0.12 | -2.7 | -0.2 / -0.4 |
| Heat Retention | -0.05 | -1.95 | -5.5 | -1.1 / -8.1 |
| Overdrive Press | +1.55 | +0.32 | +2.7 | +2.2 / +1.3 |
| Hammer Rhythm | -0.00 | -0.07 | -0.2 | -0.1 / -0.7 |
| Scorch Guard | +0.00 | +0.00 | +0.0 | +0.0 / +0.0 |
| Surge Plasma | -0.00 | -0.03 | +0.0 | -0.1 / -0.3 |
| Fast Rebond | +0.07 | -0.02 | -0.3 | +0.2 / -0.2 |
| Red Line | +0.03 | +0.00 | +0.2 | +0.1 / +0.0 |
| Steady Hands | -0.05 | -0.48 | -0.7 | -0.2 / -2.0 |
| Clean Containment | +0.50 | -0.38 | -4.3 | +0.7 / -1.1 |
| Micro-Hone | -0.27 | -2.90 | -14.5 | -1.0 / -5.5 |
| Steady Quench | +0.28 | -0.13 | -0.3 | +0.7 / -1.0 |
| Cool Hand | +0.00 | -0.02 | -0.2 | +0.0 / -0.1 |
| Temper Hold | -0.02 | +0.00 | -1.3 | -0.1 / +0.1 |
| Containment Focus | +0.08 | -0.02 | -2.3 | +0.3 / -0.1 |
| Perfect Temper | +0.38 | -0.08 | -0.3 | +1.2 / -0.6 |

- **Overdrive Press, Micro-Hone, Heat Retention, Clean Containment and Steady Quench carry the lines**: Overdrive Press carries the length (+1.55 actions without it), Micro-Hone the quality (-2.9 points and 14.5 points of full-quality share without it), Heat Retention and Clean Containment the quality and the full-quality share.
- **Plasma Efficiency, Hammer Rhythm, Scorch Guard, Surge Plasma, Fast Rebond, Red Line, Steady Hands, Cool Hand, Temper Hold and Containment Focus change a skilled craft by 0.1 actions or 0.5 quality points or less on average** at these ranks and gears (the largest change in the share of crafts that reach full quality is 2.7 points, for Plasma Efficiency; Steady Hands is the largest in quality at 0.48; Scorch Guard changes nothing because the adaptive player never presses above the range; Surge Plasma is used at most 0.2 times a craft and Red Line at most 0.1). Perfect Temper removes 0.38 actions on average (1.2 in one cell) and costs 0.08 quality points, and its quench is used in 34 to 49% of rank-50 crafts at best gear. Their value, if any, is for crafters the sampler does not model: CP-starved ones (Plasma Efficiency and Fast Rebond), crafters who press above the range (Scorch Guard) and crafters who play a rotation and not a search. The owner may want some of them stronger once the base has headroom (option 1 above).

## Limitations

- CraftSim's adaptive player is a depth-4 search on the previews (6000 engine copies per decision; 24000 gave the same results on the Artisan + Cold Working and both-line + Hot Forging builds) and plays the Forge well, so the length and style figures are for skilled play, and a perk it does not value (Scorch Guard, Surge Plasma) looks inert. Seconds per action (8) are unmeasured, so gate 6 is as soft as before.
- Gates 1 and 4 to 7 for a perked crafter play one fixed loadout (each line in full); a crafter who bought part of a line sits between the unperked and the full-line figures. The perked scenarios run 30 times per case (the style gates 100), so a cell within 1 or 2 points of a threshold (gate 6's 0.98x and 0.99x) is noise.
- The gate 2 sweep and the enhanced-craft sweep ran on smaller samples than the gates (their sizes are above); the final numbers were then run through the gates themselves.
- The tables in "Gate 4", "Style balance beyond quality" and "Which perks do the work" come from explicit tests in `SWLOR.CraftSim.Tests` (`ForgePerkLengthBudget`, `ForgeStyleStress`) and a scratch sweep over the same engine and policy, written to explain the gate results, not gates themselves. Run the first two with `dotnet test SWLOR.CraftSim.Tests --filter "FullyQualifiedName=..."`.
- No client was run.
