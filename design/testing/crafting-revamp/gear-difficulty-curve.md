# Crafting gear and difficulty curve (November 2026)

Tester feedback: "Equipment still irrelevant compared to perks." The Forge, the Circuit and the Fabricator had flat meters across recipe level, and their perks were worth far more than gear. They were also tuned against unmodded best gear, which is not what a top crafter wears.

The target, from the feedback and the owner's decisions:

- Fully modded top-tier gear (nine pieces, both slots of the best mods) is what the top recipes are built for. It gives about Control 119, Craftsmanship 120 and 37 gear CP, for about 105 CP at rank 50. Mods add no CP.
- That crafter reaches 85% on most top crafts, and 100% is possible but not guaranteed.
- Researched gear makes those crafts safer and is not required for 100%.
- Each step of gear is worth having, armor included.
- Perks stay useful but do not replace gear.

Reports: `forge-gates.md`, `circuit-gates.md`, `fabricator-gates.md`, `kitchen-gates.md` and the per-build reports beside them (seed 1, 6 recipes per band, 40 runs for the base and style reports, 30 for the Forge, Circuit and Fabricator per-build reports). The Kitchen's Slow and Flash reports use 40 runs and 4 recipes per band.

## The curves

Levels at or below the start level play as they did. All values are constants in each minigame's rules class, and the Bible tools read them from there.

| Minigame | Starts above | Per level above the start |
| --- | --- | --- |
| Forge | 25 | Finish +1.75%, Shape +0.25%. A recipe's Finish before that factor rises to at least 1300 by level 50, so Shape-heavy armor needs gear too. |
| Circuit | 30 | Calibration +6%, Integrity costs +3.3% (every action, Ground included), Patch +1.1 CP, Tune and Micro-Tune +0.12 CP |
| Fabricator | 30 | Required quality +3%, wear +6%, Service +0.2 CP |

The Fabricator's XP multiplier for levels 41 and up went from 1.00 to 1.06, because the longer crafts put best gear just under today's XP per hour.

## What each step of gear buys

Rank 50, 2 enhancements, no perks. "Reach" is the share of crafts at or above that quality with the adaptive player.

| Minigame, band | No gear | Mid gear | Best gear | Fully modded | Modded reach 85% | Modded reach 100% |
| --- | --- | --- | --- | --- | --- | --- |
| Forge 46-50, Finish-heavy | 30.4 | 49.5 | 60.6 | 97.3 | 94% | 76% |
| Forge 46-50, Shape-heavy | 16.8 | 38.8 | 71.6 | 99.9 | 100% | 96% |
| Forge 51-53, Finish-heavy | 32.6 | 45.0 | 53.6 | 93.2 | 78% | 42% |
| Forge 51-53, Shape-heavy | 5.4 | 33.0 | 52.8 | 94.0 | 82% | 49% |
| Circuit 46-50 | 40.7 | 58.1 | 78.8 | 99.1 | 99% | 97% |
| Circuit 51-53 | 15.6 | 36.1 | 49.4 | 93.9 | 85% | 78% |
| Fabricator 46-50 | 28.3 | 72.7 | 88.3 | 99.3 | 99% | 80% |
| Fabricator 51-53 | 3.8 | 14.6 | 48.2 | 95.8 | 91% | 72% |
| Kitchen 46-50 | 70.2 | 77.4 | 83.7 | 95.2 | 92% | 84% |
| Kitchen 51-53 | 55.1 | 58.3 | 68.4 | 86.2 | 80% | 73% |

The Kitchen rows are seed 1. See "The Kitchen retune" for the other seeds.

Fully perked with no gear against no perks in unmodded best gear (plain crafts, average quality):

| Band | Forge | Circuit | Fabricator | Kitchen | Unmodded best gear (Forge / Circuit / Fabricator / Kitchen) |
| --- | --- | --- | --- | --- | --- |
| 46-50 | Hot 68.7, Cold 58.1 | Overclock 83.0, Safe Mode 82.8 | Run 70.0, Fine 62.2 | Slow 90.6, Flash 90.5 | 82.0 / 78.5 / 75.0 / 87.2 |
| 51-53 | Hot 64.0, Cold 63.3 | Overclock 63.5, Safe Mode 59.9 | Run 41.7, Fine 35.5 | Slow 82.2, Flash 83.6 | 59.8 / 64.3 / 37.1 / 78.6 |

The Circuit's 46-50 row depends on the seed: seed 1 passes, and on seed 2 Overclock is 87.5 and Safe Mode 83.7 against 78.6, over the limit by 3.9 and 0.1. Averaged over the two seeds, Overclock is over by 1.7 and Safe Mode is inside the limit.

## Does the ladder hold for a player better than the bot?

The adaptive player is a floor on skilled play, and in the Circuit and the Fabricator the fixed-rotation solver beats it. A run of gate 1 on a separate 46-50 and 51-53 split, at the rank of each recipe and with no enhancements, shows the steps survive better play. Best single rotation for the band, average quality:

| Minigame, band | No gear | Mid gear | Best gear | Fully modded |
| --- | --- | --- | --- | --- |
| Forge 46-50 | 48.8 | 57.7 | 70.8 | 83.6 |
| Forge 51-53 | 25.2 | 36.2 | 41.3 | 55.6 |
| Circuit 46-50 | 59.2 | 68.7 | 93.9 | 100.0 |
| Circuit 51-53 | 47.0 | 69.4 | 81.4 | 100.0 |
| Fabricator 46-50 | 29.8 | 63.9 | 82.3 | 100.0 |
| Fabricator 51-53 | 27.5 | 50.3 | 60.9 | 100.0 |

Two things follow:

- Gear still matters at every step for a player who has found the best sequence.
- In the Circuit and the Fabricator, one memorized sequence reaches 100% on these top recipes in fully modded gear, so "not guaranteed" holds only for players who have not solved it. Fixed rotations in those two minigames were already an open owner decision before this pass. The Forge does not have this problem.

The recipe sample matters. The base Fabricator report's combined 41-53 band (levels 42-50, a different sample) has a mid gear rotation at 99.3, and the 46-50 sample above has 63.9. Some Fabricator recipes are much easier than others at the same level.

The Kitchen is not in this table: see "Limits" for why its rotation search did not run in this pass.

## Perk values

Perks were the other half of "equipment irrelevant". Every value here was tuned against the ladder.

| Forge perk | Before | After |
| --- | --- | --- |
| Plasma Efficiency CP reduction | 2 | 1 |
| Heat Retention | cooling -1 / -2 | cooling -1 / cooling -1 and Ramp -1 CP |
| Hammer Rhythm per stack | 4% | 2% |
| Scorch Guard | 25% | 12% |
| Fast Rebond | Thermal -6, CP -3 | Thermal -3, CP -1 |
| Steady Hands Integrity reduction | 2 | 1 |
| Clean Containment | 10 / 20 | 4 / 8 |
| Steady Quench, Cool Hand edges | 10% | 5% |
| Containment Focus | 10% | 5% |
| Hot Forging press | 20% | 10% |
| Cold Working hone | 4% | 5% |

| Circuit perk | Before | After |
| --- | --- | --- |
| Conductive Wiring | Load +1 / +2 | Load +1 / Load +1 and Discharge +2% |
| Capacitor Bank Discharge | 5% | 1% |
| Shock Absorbers | 33% | 15% |
| Insulated Traces | Integrity -1 / -2 on Wire, Tune, Boost and Discharge | Integrity -1 on Wire and Boost / also Patch +3 |
| Fine Calibration | 10% / 25% | 2% / 4% |
| Signal Lock chance | 10% | 4% |
| Precision Patch | Restore +10, CP -2 | Restore +2, CP -1 |
| Overclock Load boost | +1% | removed; Wire and Tune still add 1 extra Load |
| Safe Mode | Grace 2, Load -3% | Grace 3, Load -1% |
| Micro-Tune share | 70% | 60% |

| Fabricator perk | Before | After |
| --- | --- | --- |
| Production Run | Run +60%, Precision -3% | Run +35%, Precision -3% |
| Fine Tolerance | Precision +25%, Run -25% | Precision +28%, Run -15% |
| Field Service | Restore +10 / +20, CP -2 / -4 | Restore +5 / +10, CP -1 / -2 |

| Chef perk | Before | After |
| --- | --- | --- |
| Sharp Palate base spread | 45% | 47% (no perk: 50%) |
| Seasoned Palate II | 25% less random | 15% less random |
| Slow Cooking Low heat Simmer | +80% (9 Doneness) | +60% (8 Doneness); Medium heat drift -25% unchanged |
| Reduce I | 20% at 12 CP | 15% at 12 CP |
| Reduce II | 35% at 9 CP | 20% at 11 CP |
| Aromatic Kitchen Peak Aroma chance | 35% | 30% (no perk: 25%) |
| Steady Wrist Stir | 8 CP | 10 CP (no perk: 12); half a Simmer's Doneness unchanged |

Pantry Staples, Flash Cooking and Signature Dish did not change.

## The Kitchen retune

The Kitchen did not get a recipe curve. It already had a ladder, so this pass only cut the Chef line, which beat unmodded best gear by far more than the limit (46-50: 94.6 and 94.4 against 87.2; 51-53: 90.9 and 89.3 against 78.6). The gear ladder gate plays each full line with Seasoned Palate off and on, because a Chef can toggle it and the gate had only tested it on.

Average quality for a full Chef line in no gear, and its gap over unmodded best gear, averaged over seeds 1, 2 and 3 at 40 runs and 6 recipes per band. The limit is +5.

| Band | Unmodded best gear | Flash | Slow | Flash, Seasoned on | Slow, Seasoned on |
| --- | --- | --- | --- | --- | --- |
| 46-50 | 87.3 | 92.3 (+5.0) | 92.2 (+4.9) | 90.7 (+3.4) | 90.8 (+3.5) |
| 51-53 | 80.8 | 84.0 (+3.2) | 83.1 (+2.3) | 80.2 (-0.6) | 80.6 (-0.1) |

The unperked ladder, by seed (no gear / mid gear / best gear / fully modded):

| Band | Seed 1 | Seed 2 | Seed 3 |
| --- | --- | --- | --- |
| 46-50 | 70.2 / 77.4 / 83.7 / 95.2 | 70.4 / 76.9 / 84.3 / 95.9 | 66.7 / 76.7 / 84.6 / 96.4 |
| 51-53 | 55.1 / 58.3 / 68.4 / 86.2 | 46.9 / 61.3 / 68.3 / 86.0 | 52.4 / 55.0 / 69.3 / 85.5 |

Fully modded gear at 51-53 reaches 85% on about 80% of crafts and 100% on 70-73%. No single seed is exact: the same cell moves several points between seeds, which is why candidates were judged on the three-seed average. The 51-53 mid gear step is the clearest case, at +3.2 / +14.4 / +2.6 for an average of +6.8.

## Gate changes

A top recipe that is meant to be hard cannot pass gates written for a flat minigame, so these four scenarios (`ICraftScenario.TunedAgainstFullMods`) are judged differently. Each loosening is a decision for the owner to check.

- **New gate, Gear ladder.** At 46-50 and 51-53: each gear step adds at least 5 points unless the step below averages 97; at 51-53 fully modded gear reaches 85% on at least 60% of crafts and 100% on at most 90%, within the length target; a full perk line in no gear beats unmodded best gear by at most 5 points.
- **Under-geared cells are reported, not failed.** The length, economy and XP gates only report no gear from level 31 and mid gear from level 41, plus fully modded length outside the ladder's top-band check. Poor, slow crafts there are the point.
- **Saturated rotation cells are reported.** Gate 1 only reports a cell where adaptive play already averages 97 or more.
- **Perks optional plays fully modded gear** and asks for 50%, or 35% at 51-53 (was 50% in unmodded best gear).
- **Enhancement parity plays fully modded gear** and asks for 0.95 of today's at one and two slots and 0.90 at three (was 1.00). Today's transfer is near-certain; the third slot needs a full-quality craft, which is meant not to be guaranteed.

## Gates still failing

Tuning stopped after seven rounds; further rounds traded one miss for another.

| Gate | Case | Result | Note |
| --- | --- | --- | --- |
| Circuit rotation | 10 cases (6 to 7 in each perked build) | smallest margin -19.7 | Open by owner decision before this pass; master already failed 15 |
| Fabricator rotation | 5 cases (2 in each perked build) | smallest margin -34.0 | Open by owner decision before this pass |
| Circuit style | Overclock vs Safe Mode, no lines, mid gear | Overclock wins 25/75, -5.3 | Win share under 30% and the average gap just over the limit of 5 |
| Forge ladder | 51-53, Finish-heavy, fully modded | just under 10 actions (rounds to 10.0), target 10-20 | The fully modded crafter finishes at the floor, not below it in any meaningful way |
| Circuit ladder | 46-50, both lines, no gear, seed 2 only | Overclock 87.5 and Safe Mode 83.7 vs 78.6, limit +5 | Seed 1, the reported run, passes |
| Fabricator style | Production Run vs Fine Tolerance, no gear | +5.1, limit 5 | Over by 0.1 |
| Forge economy | 21-30, no gear (also Hot Forging) | ratio 1.01 | Level 24 recipe; rules below level 25 are unchanged, and master's sample differed |
| Forge XP | 11-20, no gear (also both-line builds) | 0.98x | Rules there are unchanged, and master's sample differed |
| Kitchen ladder | 51-53, mid gear step, seeds 1 and 3 | +3.2 and +2.6, limit +5 | Sample noise: seed 2 is +14.4 and the three-seed average is +6.8 |
| Kitchen ladder | 46-50, Flash and Slow full lines, single seeds | over +5 on some seeds | Seed 1 passes at +3.3 and +3.4; the three-seed averages are +5.0 and +4.9 |
| Kitchen enhancement | 41-45, 3 slots, no perks, fully modded | 0.894 (2.62 against 2.93), shown as 0.90, limit 0.90 | The Kitchen's rules did not change in this pass; the gate now plays fully modded gear |
| Kitchen XP | 21-30, no gear, 1 enhancement, perked builds | Flash 0.99x, Slow just under 1.00x | 4 recipes per band; unperked passes (lowest 1.03x) |

## Not retuned

- **Research and Espionage.** Out of scope.

## Limits

- The simulator does not model attributes, food, status effects, blueprints or quick craft. Researched gear above the modded tier was not played.
- The base reports use 40 runs per case, not 100, so differences under about 2 points are noise.
- The adaptive player reads only shared meters and previews. It is a floor, as the rotation table shows.
- Seconds per action is an estimate, so XP per hour and the length limits are estimates too.
- The Kitchen reports leave out gate 1. The search takes many CPU hours, and the full run was stopped after 3 hours. The unperked Kitchen's rules did not change in this pass, and its last full report (100 runs) passed with a smallest margin of 19.4. Gate 1 for perked Chef builds was already failing and stays open by owner decision.
- The Kitchen's style and perks-optional gates are in `kitchen-perks-gates.md`, not `kitchen-gates.md`, which holds the length, economy, XP, enhancement and ladder gates.
