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
| Kitchen 46-50 | 71.6 | 81.6 | 89.7 | 97.3 | 95% | 86% |
| Kitchen 51-53 | 51.1 | 64.5 | 69.6 | 93.4 | 88% | 79% |

The Kitchen rows are seed 1. See "The Kitchen retune" for the other seeds. The 51-53 rows predate "Recipes above level 50".

Fully perked with no gear against no perks in unmodded best gear (plain crafts, average quality):

| Band | Forge | Circuit | Fabricator | Kitchen | Unmodded best gear (Forge / Circuit / Fabricator / Kitchen) |
| --- | --- | --- | --- | --- | --- |
| 46-50 | Hot 68.7, Cold 58.1 | Overclock 83.0, Safe Mode 82.8 | Run 70.0, Fine 62.2 | Slow 90.6, Flash 90.5 | 82.0 / 78.5 / 75.0 / 89.6 |
| 51-53 | Hot 64.0, Cold 63.3 | Overclock 63.5, Safe Mode 59.9 | Run 41.7, Fine 35.5 | Slow 82.2, Flash 83.6 | 59.8 / 64.3 / 37.1 / 77.8 |

The Circuit's 46-50 row depends on the seed: seed 1 passes, and on seed 2 Overclock is 87.5 and Safe Mode 83.7 against 78.6, over the limit by 3.9 and 0.1. Averaged over the two seeds, Overclock is over by 1.7 and Safe Mode is inside the limit. The Kitchen's 51-53 Flash row is over by 0.8 on seed 1 only; see "The Kitchen retune".

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
| Kitchen 46-50 | 42.9 | 53.9 | 65.3 | 73.2 |
| Kitchen 51-53 | 26.9 | 30.1 | 40.0 | 54.2 |

Two things follow:

- Gear still matters at every step for a player who has found the best sequence.
- In the Circuit and the Fabricator, one memorized sequence reaches 100% on these top recipes in fully modded gear, so "not guaranteed" holds only for players who have not solved it. Fixed rotations in those two minigames were already an open owner decision before this pass. The Forge and the Kitchen do not have this problem: the Kitchen's split passes gate 1, with a smallest margin of 22.2 in 46-50, best gear.

The recipe sample matters. The base Fabricator report's combined 41-53 band (levels 42-50, a different sample) has a mid gear rotation at 99.3, and the 46-50 sample above has 63.9. Some Fabricator recipes are much easier than others at the same level.

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

The Kitchen did not get a recipe curve. It already had a ladder, so this pass cut the Chef line, which beat unmodded best gear by far more than the limit (46-50: 94.6 and 94.4 against 87.2; 51-53: 90.9 and 89.3 against 78.6). The gear ladder gate plays each full line with Seasoned Palate off and on, because a Chef can toggle it and the gate had only tested it on.

Average quality for a full Chef line in no gear, and its gap over unmodded best gear, averaged over seeds 1, 2 and 3 at 40 runs and 6 recipes per band. The limit is +5.

| Band | Unmodded best gear | Flash | Slow | Flash, Seasoned on | Slow, Seasoned on |
| --- | --- | --- | --- | --- | --- |
| 46-50 | 90.0 | 92.3 (+2.3) | 92.2 (+2.2) | 90.7 (+0.7) | 90.8 (+0.8) |
| 51-53 | 81.4 | 84.0 (+2.6) | 83.1 (+1.7) | 80.2 (-1.2) | 80.6 (-0.7) |

The unperked ladder, by seed (no gear / mid gear / best gear / fully modded):

| Band | Seed 1 | Seed 2 | Seed 3 |
| --- | --- | --- | --- |
| 46-50 | 71.6 / 81.6 / 89.7 / 97.3 | 72.9 / 78.8 / 91.2 / 98.4 | 69.0 / 80.1 / 89.3 / 98.0 |
| 51-53 | 51.1 / 64.5 / 69.6 / 93.4 | 52.1 / 66.4 / 76.0 / 92.6 | 52.2 / 63.3 / 73.3 / 90.7 |

Fully modded gear at 51-53 reaches 85% on 85-88% of crafts and 100% on 78-79%. No single seed is exact: the same cell moves several points between seeds, which is why candidates were judged on the three-seed average. Unmodded best gear's plain crafts at 51-53 are the clearest case, at 77.8 / 85.3 / 81.0, so the full Flash line's gap over them is +5.8 on seed 1 and under the limit on seeds 2 and 3.

Three Kitchen rules changed after the first review, each to close a gate the first retune left failing (`KitchenTuning.cs`):

- **Craftsmanship** adds 1.1% to each action's Doneness per point (was 0.8%). At 0.8% the 51-53 mid gear step was +3.2 and +2.6 on seeds 1 and 3; it is now +11.1 to +14.3, and the smallest step on any seed is +5.1 (51-53 best gear, seed 1). No gear has no Craftsmanship, so the perked no-gear rows did not move.
- **Enhancements** add 20% of their penalty to the Doneness a dish needs (was 25%). Three enhancements on a 41-45 dish in fully modded gear transferred 0.89 of today's; now 0.92 (2.70 against 2.93), and the Flash and Slow builds' lowest is 0.97 and 1.01.
- **XP at levels 21-30** uses a 1.12 multiplier (was 1.1). The perked builds with no gear and one enhancement were at 0.99x and just under 1.00x; they are now 1.04x (Flash) and 1.02x (Slow).

Two other candidates were played on the 51-53 ladder and rejected: 20% enhancements alone (the best gear step stayed under +5 on seeds 1 and 2) and that plus a larger Control spread step (the mid-to-best step fell to +1.0 on seed 2).

## Recipes above level 50

Tester feedback on the first test server build: levels 45-50 are in a good place, but the level 52 Chaos Breastplate was comfortable at 100% Shape and Finish in double-modded gear, at Control 189 / Craftsmanship 50 and at 215 / 66. The tester put nearly all mod points in Control, which the gear ladder's even split does not model. On checking, every minigame's perked builds reached 100% on nearly every level 51-53 craft. The owner asked for all of them to be fixed, with levels 50 and below left alone.

Rules that apply only to recipes above level 50 (`TopRecipeRules.cs`):

- **Control above 120 counts for a quarter** in the Forge, the Circuit and the Fabricator. Evenly split fully modded gear (119) loses nothing.
- **Forge enhancements** compare their penalty with level 50's progress (`ForgeRules.EnhancementProgressMaxLevel`). The chart jumps from 186 at level 50 to 339, 503 and 586 at levels 51-53, so two penalty 80 mods added 86% Shape at level 50 and only 32% at level 52.
- **Circuit** Calibration and Integrity difficulty are 10% higher (`CircuitRules.TopRecipeDifficultyFactor`).
- **Fabricator** Builder perks' numeric bonuses are worth a quarter (`FabricatorRules.TopRecipePerkValue`).
- **Kitchen** actions give 80% of their Doneness (`KitchenTuning.TopRecipeDonenessFactor`).

Levels 51-53, fully modded gear, two enhancements. "After" is the three-seed average of reach 100% (seeds 1-3, 40 runs, 6 recipes per band), or 3 recipes per kind in the Forge. "Before" is seed 1 unless the row says otherwise.

| Crafter | Control / Craftsmanship | Reach 100% before | Reach 100% after | Reach 85% after |
| --- | --- | --- | --- | --- |
| Forge, no perks, Shape-heavy (three seeds) | 119 / 120 | 50% | 63% | 88% |
| Forge, no perks, Finish-heavy (three seeds) | 119 / 120 | 44% | 30% | 68% |
| Forge, Artisan, Shape-heavy (three seeds) | 189 / 50 | 95% | 72% | 81% |
| Forge, Artisan, Finish-heavy (three seeds) | 189 / 50 | 98% | 88% | 97% |
| Forge, Artisan, Shape-heavy | 119 / 120 | 90% | 92% | 97% |
| Forge, Artisan, Finish-heavy | 119 / 120 | 82% | 80% | 100% |
| Circuit, no perks (three seeds) | 119 / 120 | 79% | 54% | 67% |
| Circuit, Technician (three seeds) | 189 / 50 | 97% | 87% | 100% |
| Circuit, Technician (three seeds) | 119 / 120 | 95% | 90% | 100% |
| Circuit, Overclocker (three seeds) | 119 / 120 | 97% | 82% | 100% |
| Fabricator, no perks | 119 / 120 | 72% | 73% | 90% |
| Fabricator, Fine Tolerance | 119 / 120 | 92% | 79% | 99% |
| Fabricator, Fine Tolerance | 189 / 50 | 51% | 57% | 89% |
| Fabricator, Production Run | 119 / 120 | 86% | 87% | 100% |
| Fabricator, Production Run | 189 / 50 | 93% | 68% | 90% |
| Kitchen, no perks | 119 / 120 | 79% | 71% | 80% |
| Kitchen, Slow Cooking | 50 / 189 | 94% | 89% | 98% |
| Kitchen, Flash Cooking | 50 / 189 | 93% | 89% | 95% |

- These crafts are much harder below fully modded gear. With two enhancements and no perks, Forge Shape-heavy recipes at 51-53 average 19.9 in unmodded best gear (was 52.7) and 3.4 in mid gear (was 31.3). Builder perks in unmodded best gear reach 100% on 12-24%.
- The no-perk Forge Shape-heavy row rises on a harder rule because the bot plays longer, 10 actions against 13.
- The Control rule and the Fabricator perk rule are new mechanics, added because raising difficulty did not separate perked crafters from unperked ones. In the Circuit and the Fabricator, 10-15% more difficulty and 15-25% less CP left the perked builds at 91-99% and took the no-perk crafter as low as 49% reaching 85%.
- Enhancement parity passes in every band for all four minigames (seed 1).
- Enhancements on Finish-heavy Forge recipes stay comparatively easy. Raising both Forge meters 10% above level 50 left the Control-stacked Artisan at 99% and dropped the no-perk crafter to 15%, so it was rejected.

The minigames' `*-gates.md` reports and the 51-53 rows in "What each step of gear buys" are from before these changes and were not rerun.

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
| Forge ladder | 51-53, Finish-heavy, fully modded | 9.5-9.6 actions, target 10-20 (was just under 10) | The fully modded crafter finishes at the floor |
| Forge ladder | 51-53, Shape-heavy, mid gear, two enhancements | adds +2.1 to +4.4 over no gear, target +5 | No gear scores 0, so the step has no floor to climb from; these crafts are not viable either way |
| Forge ladder | 51-53, Shape-heavy, Artisan line, Control 119 / Craftsmanship 120 | reaches 100% on 92%, limit 90% | See "Recipes above level 50" |
| Circuit ladder | 51-53, no perks, seed 1 | mid gear adds +4.5 and best gear +4.8, target +5 | Follows the level 51+ difficulty rise |
| Circuit ladder | 46-50, both lines, no gear, seed 2 only | Overclock 87.5 and Safe Mode 83.7 vs 78.6, limit +5 | Seed 1, the reported run, passes |
| Fabricator style | Production Run vs Fine Tolerance, no gear | +5.1, limit 5 | Over by 0.1 |
| Forge economy | 21-30, no gear (also Hot Forging) | ratio 1.01 | Level 24 recipe; rules below level 25 are unchanged, and master's sample differed |
| Forge XP | 11-20, no gear (also both-line builds) | 0.98x | Rules there are unchanged, and master's sample differed |
| Kitchen ladder | 51-53, Flash full line, no gear, seed 1 only | 83.6 against unmodded best gear's 77.8 (+5.8), limit +5 | Seeds 2 and 3 pass; unmodded best gear moves 77.8 / 85.3 / 81.0 between seeds, and the three-seed gap is +2.6 |
| Research style | Methodical alone against Intuitive alone: fixed levels in every gear level, and evidence-decides levels in best and fully modded gear | Methodical better on 75-100% of cases; mean gaps +2.7 to +7.4 | Owner decision: styles stay "trials against chance". Alone, Methodical leads at fixed levels in every gear level and where evidence decides in best and fully modded gear; Intuitive leads only where evidence decides in no gear (67% of cases, -0.4), and the two split mid gear. With the whole line, all 8 comparisons pass |

## Research

Before this pass the research Experiment read no gear stat besides CP: tier 5 at rank 50 scored 6.1% reduction in no gear and in best gear alike, while the full Researcher line gave 9.5% and the 25% time-cut cap. It is now tuned like the crafts (report `fabrication-researcher-gear-ladder.md`, seed 1, 400 runs; the score is the reduction as a share of 10%, or the time cut as a share of the 25% cap).

- **Control** adds a point of Test chance for each 2 Control up to 30 (best unmodded gear), then one for each 4. Fully modded gear gets +37.
- **Craftsmanship** adds a trial for each 120, so fully modded gear gets one more trial. Craftsmanship first added to Analyze's time cut instead, but that took fully modded gear to the maximum every time.
- **Research tier.** The base Test chance (60%) drops 5 points for each tier above tier 2, so tier 5 starts at 45%.
- **Researcher line** cut so a full line in no gear stays within 5 points of unperked best gear: Field Notes +1/+2 (was +10/+20), Efficient Analysis 5.3%/5.5% per evidence (was 6%/7%), Methodical +1 trial and -15 (was +2 trials), Intuitive -1 trial and +4 (was +20), and Breakthrough only makes the next Test succeed (it no longer doubles the time cut). Broader Hypotheses, Steady Hands, Peer Review and Second Opinion are unchanged. The time cut per evidence is now counted in tenths of a percent.

Unperked mean score (no gear / mid gear / best gear / fully modded), and how often fully modded gear reaches 85:

| Tier, level kind | Ladder | Fully modded reaches 85 |
| --- | --- | --- |
| 4, evidence decides | 49.7 / 56.7 / 64.1 / 96.3 | 84% |
| 4, fixed levels | 53.9 / 59.5 / 70.5 / 97.3 | 88% |
| 5, evidence decides | 45.9 / 51.9 / 59.6 / 92.9 | 72% |
| 5, fixed levels | 47.6 / 57.0 / 65.4 / 95.5 | 82% |

A full line in no gear against unperked best gear: tier 4 is 6.9 to 13.6 points under it; tier 5 is -3.8 to +3.8 (Intuitive at fixed levels, 69.2 against 65.4).

**Espionage** needs no change: its Reaction minigame reads Espionage rank only, with no crafting gear and no crafting perks.

## Limits

- The simulator does not model attributes, food, status effects, blueprints or quick craft. Researched gear above the modded tier was not played.
- The base reports use 40 runs per case, not 100, so differences under about 2 points are noise.
- The adaptive player reads only shared meters and previews. It is a floor, as the rotation table shows.
- Seconds per action is an estimate, so XP per hour and the length limits are estimates too.
- The Kitchen reports leave out gate 1. The search takes many CPU hours, and the run over every band was stopped after 3 hours. Its last full report (100 runs) passed with a smallest margin of 19.4, and the 46-50 and 51-53 split above passed with 22.2. Both ran before the three Kitchen rule changes in "The Kitchen retune" and were not rerun; those changes give geared crafts more Doneness and lower the enhancement cost, so the rotation margins are not expected to shrink, but that is not measured. Gate 1 for perked Chef builds was already failing, was not rerun, and stays open by owner decision.
- The Kitchen's style and perks-optional gates are in `kitchen-perks-gates.md`, not `kitchen-gates.md`, which holds the length, economy, XP, enhancement and ladder gates.
