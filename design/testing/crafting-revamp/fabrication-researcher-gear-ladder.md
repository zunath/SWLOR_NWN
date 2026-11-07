# Crafting revamp: the Fabrication Researcher gear ladder

Command: `SWLOR.CraftSim researcher-perks --ladder --runs 400 --seed 1 --out design/testing/crafting-revamp/fabrication-researcher-gear-ladder.md`

## Inputs

- Seed 1; 400 experiments per case and build, every build on the same dice (the build is left out of each experiment's stream), so a difference between two builds is theirs and not the luck of two samples.
- Every build takes its numbers from the perk definitions (`FabricationResearcherPerkDefinition`) at the researcher's rank; a perk with two ranks gives the highest rank that rank allows. Base values (`ExperimentRules`): 6 trials, Test chance 60% plus up to 15 points for rank, Refine Method 15 points for 30 CP, Analyze 5% per piece of spare evidence up to 25%, Peer Review 25 CP. Gear: one point of Test chance per 2 Fabrication Control, one more trial per 120 Craftsmanship; the base chance drops 5 points for each research tier above tier 2.
- Gear: no gear = Control 0, Craftsmanship 0, 0 CP; mid gear = Control 13, Craftsmanship 12, 26 CP; best gear = Control 29, Craftsmanship 30, 37 CP; fully modded gear = Control 119, Craftsmanship 120, 37 CP. CP is gear CP plus 0.75 per rank; mods add no CP.
- Cases: research level 1 stands for the fixed levels and licensed-run rolls (every Test banks evidence and the last trial Analyzes it); level 3 stands for the levels where evidence decides between a credit and a time reduction. Researchers at ranks 20, 30, 40 and 50 research the tier their rank opens, at that tier's first rank, and rank 50 also researches tiers 1 and 3 (up to 15 more points of Test chance).
- Focused player: refines before every Test it can afford, puts every Test on one reduction (credit), Analyzes with the last trial at a fixed level, and with Breakthrough spends it first. Undecided player (Peer Review only): tests both reductions for half the trials, then backs the leader and, with Peer Review, pools the evidence behind it into the leader, keeping the CP it costs back from Refine Method.
- Score (0-100): where evidence decides, the reduction won as a share of today's largest (10%), a run with no evidence counting 0; at a fixed level, the time cut as a share of the cap (25%). Style balance uses the style-balance gate's thresholds: each style the better choice on at least 30% of cases, a mean gap of at most 5 points, ties within 0.5 point count half.

## Gear ladder

The crafting gear ladder's thresholds, on the score above, at the top research tiers (each at its first rank, focused player): each gear step adds at least 5 points unless the step below averages 97; fully modded gear reaches 85 on at least 60% of experiments and the maximum on at most 90%; a full line in no gear beats the unperked researcher in unmodded best gear by at most 5 points.

| Tier | Level kind | Gear | Mean score | Reaches 85 | Reaches the maximum | Mean actions | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 4 | Evidence decides (reduction) | no gear | 49.7 | 2% | 2% | 7.0 | Passed |
| 4 | Evidence decides (reduction) | mid gear | 56.7 | 4% | 4% | 7.0 | Passed |
| 4 | Evidence decides (reduction) | best gear | 64.1 | 10% | 10% | 8.0 | Passed |
| 4 | Evidence decides (reduction) | fully modded gear | 96.3 | 84% | 84% | 9.0 | Passed |
| 4 | Fixed levels (time cut) | no gear | 53.9 | 5% | 5% | 7.0 | Passed |
| 4 | Fixed levels (time cut) | mid gear | 59.5 | 7% | 7% | 7.0 | Passed |
| 4 | Fixed levels (time cut) | best gear | 70.5 | 16% | 16% | 8.0 | Passed |
| 4 | Fixed levels (time cut) | fully modded gear | 97.3 | 88% | 88% | 9.0 | Passed |
| 5 | Evidence decides (reduction) | no gear | 45.9 | 1% | 1% | 7.0 | Passed |
| 5 | Evidence decides (reduction) | mid gear | 51.9 | 3% | 3% | 8.0 | Passed |
| 5 | Evidence decides (reduction) | best gear | 59.6 | 5% | 5% | 8.0 | Passed |
| 5 | Evidence decides (reduction) | fully modded gear | 92.9 | 72% | 72% | 9.0 | Passed |
| 5 | Fixed levels (time cut) | no gear | 47.6 | 2% | 2% | 7.0 | Passed |
| 5 | Fixed levels (time cut) | mid gear | 57.0 | 6% | 6% | 8.0 | Passed |
| 5 | Fixed levels (time cut) | best gear | 65.4 | 11% | 11% | 8.0 | Passed |
| 5 | Fixed levels (time cut) | fully modded gear | 95.5 | 82% | 82% | 9.0 | Passed |


| Tier | Level kind | Build, no gear | Score | Unperked, unmodded best gear | Verdict |
| --- | --- | --- | --- | --- | --- |
| 4 | Evidence decides (reduction) | Methodical (full line) | 50.4 | 64.1 | Passed |
| 4 | Evidence decides (reduction) | Intuitive (full line) | 55.5 | 64.1 | Passed |
| 4 | Fixed levels (time cut) | Methodical (full line) | 58.6 | 70.5 | Passed |
| 4 | Fixed levels (time cut) | Intuitive (full line) | 63.8 | 70.5 | Passed |
| 5 | Evidence decides (reduction) | Methodical (full line) | 55.8 | 59.6 | Passed |
| 5 | Evidence decides (reduction) | Intuitive (full line) | 59.2 | 59.6 | Passed |
| 5 | Fixed levels (time cut) | Methodical (full line) | 66.7 | 65.4 | Passed |
| 5 | Fixed levels (time cut) | Intuitive (full line) | 69.2 | 65.4 | Passed |


Verdict: passed.

