# Crafting revamp: the Fabrication Researcher line

Command: `SWLOR.CraftSim researcher-perks --runs 400 --seed 1 --out design/testing/crafting-revamp/fabrication-researcher-perks.md`

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

## Style balance: Methodical against Intuitive

The generic gates have no scoring rule for an experiment (it has no quality meter), so this applies gate 2's thresholds to the score above, over every case. "Alone" owns the style and nothing else; "full line" owns every other purchase the rank allows too. The length columns show what each style costs in turns, which the score does not count.

| Pair | Level kind | Gear | Cases | Methodical better | Intuitive better | Mean score gap (Methodical - Intuitive) | Methodical mean actions | Intuitive mean actions | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Methodical vs Intuitive | Fixed levels (time cut) | no gear | 6 | 75% | 25% | +2.7 | 7.7 | 5.7 | Failed |
| Methodical vs Intuitive | Fixed levels (time cut) | mid gear | 6 | 83% | 17% | +4.2 | 8.5 | 6.5 | Failed |
| Methodical vs Intuitive | Fixed levels (time cut) | best gear | 6 | 92% | 8% | +7.4 | 8.7 | 6.7 | Failed |
| Methodical vs Intuitive | Fixed levels (time cut) | fully modded gear | 6 | 100% | 0% | +3.0 | 9.3 | 6.7 | Failed |
| Methodical vs Intuitive | Evidence decides (reduction) | no gear | 6 | 33% | 67% | -0.4 | 7.7 | 5.7 | Passed |
| Methodical vs Intuitive | Evidence decides (reduction) | mid gear | 6 | 50% | 50% | +1.0 | 8.5 | 6.5 | Passed |
| Methodical vs Intuitive | Evidence decides (reduction) | best gear | 6 | 83% | 17% | +4.9 | 8.7 | 6.7 | Failed |
| Methodical vs Intuitive | Evidence decides (reduction) | fully modded gear | 6 | 92% | 8% | +3.0 | 9.3 | 6.7 | Failed |
| Methodical (full line) vs Intuitive (full line) | Fixed levels (time cut) | no gear | 6 | 42% | 58% | -0.9 | 9.0 | 7.0 | Passed |
| Methodical (full line) vs Intuitive (full line) | Fixed levels (time cut) | mid gear | 6 | 50% | 50% | +0.1 | 10.0 | 8.0 | Passed |
| Methodical (full line) vs Intuitive (full line) | Fixed levels (time cut) | best gear | 6 | 58% | 42% | +1.4 | 10.0 | 7.7 | Passed |
| Methodical (full line) vs Intuitive (full line) | Fixed levels (time cut) | fully modded gear | 6 | 58% | 42% | +0.5 | 10.7 | 8.0 | Passed |
| Methodical (full line) vs Intuitive (full line) | Evidence decides (reduction) | no gear | 6 | 33% | 67% | -1.9 | 9.0 | 7.0 | Passed |
| Methodical (full line) vs Intuitive (full line) | Evidence decides (reduction) | mid gear | 6 | 50% | 50% | -1.2 | 10.0 | 8.0 | Passed |
| Methodical (full line) vs Intuitive (full line) | Evidence decides (reduction) | best gear | 6 | 50% | 50% | +0.7 | 10.0 | 7.7 | Passed |
| Methodical (full line) vs Intuitive (full line) | Evidence decides (reduction) | fully modded gear | 6 | 58% | 42% | +0.5 | 10.7 | 8.0 | Passed |

Verdict: out of balance in 6 of 16 comparison(s): Methodical vs Intuitive / Fixed levels (time cut) / no gear; Methodical vs Intuitive / Fixed levels (time cut) / mid gear; Methodical vs Intuitive / Fixed levels (time cut) / best gear; Methodical vs Intuitive / Fixed levels (time cut) / fully modded gear; Methodical vs Intuitive / Evidence decides (reduction) / best gear; Methodical vs Intuitive / Evidence decides (reduction) / fully modded gear.

## The styles' magnitudes

Each row pairs a Methodical and an Intuitive candidate over the same 36 cases as the table above (two level kinds, three gear levels, six rank and tier combinations): the styles alone, and each style on top of every other purchase of the line (the full line). "Failed" counts the six comparisons (two level kinds by three gear levels) that miss gate 2's thresholds; the gaps are the mean score gap over all cases of the level kind (positive favors Methodical), for the styles alone.

| Methodical | Intuitive | Alone: failed of 6 | Full line: failed of 6 | Fixed levels: mean gap | Evidence decides: mean gap | Full line fixed levels: mean gap | Full line evidence decides: mean gap | Methodical mean actions | Intuitive mean actions |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Methodical +1 trial, -15 points | Intuitive -1 trial, +4 points | 6 | 0 | +4.3 | +2.1 | +0.3 | -0.5 | 8.5 | 6.4 |
| Methodical +1 trial, -15 points | Intuitive -1 trial, +6 points | 3 | 1 | +3.2 | +0.9 | -0.8 | -1.7 | 8.5 | 6.3 |
| Methodical +1 trial, -15 points | Intuitive -1 trial, +20 points (the October number) | 5 | 6 | -3.4 | -6.2 | -7.0 | -9.4 | 8.5 | 5.8 |
| Methodical +1 trial, -12 points | Intuitive -1 trial, +4 points | 8 | 4 | +7.2 | +5.1 | +2.4 | +2.0 | 8.5 | 6.4 |
| Methodical +1 trial, -12 points | Intuitive -1 trial, +6 points | 7 | 2 | +6.0 | +3.8 | +1.3 | +0.8 | 8.5 | 6.3 |
| Methodical +1 trial, -12 points | Intuitive -1 trial, +20 points (the October number) | 5 | 5 | -0.6 | -3.2 | -4.9 | -6.9 | 8.5 | 5.8 |
| Methodical +1 trial, -20 points | Intuitive -1 trial, +4 points | 3 | 5 | -0.6 | -2.9 | -3.5 | -4.9 | 8.6 | 6.4 |
| Methodical +1 trial, -20 points | Intuitive -1 trial, +6 points | 4 | 6 | -1.7 | -4.1 | -4.6 | -6.2 | 8.6 | 6.3 |
| Methodical +1 trial, -20 points | Intuitive -1 trial, +20 points (the October number) | 6 | 6 | -8.4 | -11.2 | -10.8 | -13.8 | 8.6 | 5.8 |
| Methodical +2 trials, -15 points (the October number) | Intuitive -1 trial, +4 points | 8 | 7 | +10.6 | +8.3 | +4.4 | +4.4 | 9.5 | 6.4 |
| Methodical +2 trials, -15 points (the October number) | Intuitive -1 trial, +6 points | 8 | 7 | +9.5 | +7.1 | +3.4 | +3.1 | 9.5 | 6.3 |
| Methodical +2 trials, -15 points (the October number) | Intuitive -1 trial, +20 points (the October number) | 4 | 4 | +2.8 | +0.0 | -2.9 | -4.6 | 9.5 | 5.8 |
| Methodical +2 trials, -20 points | Intuitive -1 trial, +4 points | 6 | 1 | +5.8 | +3.3 | +0.9 | +0.0 | 9.6 | 6.4 |
| Methodical +2 trials, -20 points | Intuitive -1 trial, +6 points | 5 | 0 | +4.7 | +2.0 | -0.2 | -1.3 | 9.6 | 6.3 |
| Methodical +2 trials, -20 points | Intuitive -1 trial, +20 points (the October number) | 4 | 5 | -2.0 | -5.0 | -6.5 | -8.9 | 9.6 | 5.8 |
| Methodical +2 trials, -25 points | Intuitive -1 trial, +4 points | 1 | 3 | +0.6 | -2.0 | -3.3 | -4.7 | 9.6 | 6.4 |
| Methodical +2 trials, -25 points | Intuitive -1 trial, +6 points | 2 | 5 | -0.6 | -3.3 | -4.3 | -5.9 | 9.6 | 6.3 |
| Methodical +2 trials, -25 points | Intuitive -1 trial, +20 points (the October number) | 5 | 6 | -7.2 | -10.3 | -10.6 | -13.6 | 9.6 | 5.8 |

## Each purchase alone, and the whole line (rank 50, tier 5, mid gear)

Each row is a rank-50 researcher who owns the one perk named (at its highest rank), or the whole line with that style, against the unperked researcher on the same dice. "Reaches the maximum" is the share of experiments whose reduction reached today's largest; "Reaches the cap" the share that cut the full 25% of the job.

| Build | Trials | Test chance | Reductions: mean actions | Mean reduction size | No evidence | Reaches the maximum | Fixed: mean actions | Mean time cut | Reaches the cap |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Unperked | 6 | 51% | 8.0 | 5.19% | 0.8% | 3% | 8.0 | 14.3% | 6% |
| Field Notes | 6 | 53% | 8.0 | 5.41% | 0.5% | 4% | 8.0 | 14.8% | 6% |
| Steady Hands | 6 | 51% | 8.0 | 5.19% | 0.8% | 3% | 8.0 | 14.3% | 6% |
| Broader Hypotheses | 7 | 51% | 9.0 | 5.99% | 0.2% | 8% | 9.0 | 16.6% | 16% |
| Methodical | 7 | 36% | 9.0 | 4.43% | 2.0% | 2% | 9.0 | 12.4% | 3% |
| Intuitive | 5 | 55% | 7.0 | 4.70% | 0.8% | 0% | 7.0 | 12.7% | 0% |
| Peer Review | 6 | 51% | 8.0 | 5.19% | 0.8% | 3% | 8.0 | 14.3% | 6% |
| Efficient Analysis | 6 | 51% | 8.0 | 5.19% | 0.8% | 3% | 8.0 | 15.3% | 6% |
| Second Opinion | 6 | 51% | 8.0 | 5.37% | 0.8% | 4% | 8.0 | 14.8% | 6% |
| Breakthrough | 6 | 51% | 9.0 | 6.00% | 0.0% | 5% | 9.0 | 16.5% | 10% |
| Methodical (full line) | 8 | 38% | 11.0 | 6.45% | 0.0% | 14% | 11.0 | 19.3% | 26% |
| Intuitive (full line) | 6 | 57% | 9.0 | 6.72% | 0.0% | 10% | 9.0 | 19.6% | 16% |

## Peer Review for a player who has not decided

The focused player never has evidence behind the leader, so Peer Review has nothing to move for them. A player who tests both reductions first and then backs the leader does: Peer Review pools the evidence behind the leader into it. Rank 50, tier 5, where evidence decides. The perk costs 25 CP, so it competes with Refine Method for CP: a no-gear researcher has 37 CP and cannot afford both.

| Build | Gear | Mean actions | Mean reduction size | No evidence | Reaches the maximum |
| --- | --- | --- | --- | --- | --- |
| Unperked | no gear | 7.0 | 4.00% | 2.8% | 0% |
| Peer Review | no gear | 6.3 | 4.34% | 3.2% | 0% |
| Unperked | mid gear | 8.0 | 4.30% | 0.8% | 0% |
| Peer Review | mid gear | 7.4 | 4.94% | 0.8% | 2% |
| Unperked | best gear | 8.0 | 4.93% | 0.0% | 0% |
| Peer Review | best gear | 7.5 | 5.73% | 0.0% | 4% |
| Unperked | fully modded gear | 9.0 | 8.18% | 0.0% | 37% |
| Peer Review | fully modded gear | 8.8 | 9.15% | 0.0% | 68% |

## Fabrication XP per real hour with the line

Rows follow the research report: the researcher is at the tier's first rank and researches the highest-level recipe of the tier, in mid gear, every job slot kept busy, and research level 1 (the highest-paying level: the job length grows with the square of the level while the XP grows by 10% per level). The experiment's XP is the share of its trials used, so a researcher with more trials who plays them all earns the same XP: perks reach research XP only through the job's length, which the time cut shortens by at most 25% whatever the perks, and through the active time per job, which only matters when it exceeds the job's length. "At the cap" recomputes the row with the full 25% cut and the shortest experiment, the most any build can reach.

| Rank | Tier | Recipe level | Jobs | Unperked XP per hour | Methodical (full line) | Intuitive (full line) | Most any build can reach (the cap) | At-level Fabricator crafting XP per hour (0% quality) | Most / crafting |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 10 | 1 | 9 | 1 | 9977 | 10044 | 10044 | 11016 | 43200 | 26% |
| 20 | 2 | 19 | 1 | 2163 | 2153 | 2119 | 2400 | 27000 | 9% |
| 30 | 3 | 29 | 2 | 1762 | 1805 | 1820 | 1965 | 22500 | 9% |
| 40 | 4 | 39 | 2 | 920 | 950 | 959 | 1044 | 19800 | 5% |
| 50 | 5 | 52 | 3 | 1399 | 1485 | 1492 | 1599 | 19080 | 8% |

The highest unperked row is 9977 XP per hour; the highest with the line is 10044 (1.007x), and no build can exceed 11016 (1.10x the unperked peak) because the time cut's cap does not move. The crafting column is a Fabricator craft of 15 actions (the middle of the 10-20 target) at 8.0 s each with the Fabricator's XP multiplier for the rank's level band (`CraftMinigameXp`), no blueprint, not a first craft, at 0% quality, the lowest crafting rate; chart XP at level is 600.

## Limitations

- The focused and undecided players are stated strategies, not searches for the best play. A real player who mixes reductions or skips Refine Method lands between the rows.
- Seconds per action and the per-job overhead are estimates; the client evidence replaces them. XP per hour scales directly with them.
- The score weighs a reduction's size and a time cut against their maximums; it does not value a time cut against a credit or time reduction, or a shorter sitting against a longer one. Each Methodical turn is about six seconds, so a style's length costs far less than a shortened job saves.
- The recipe cost modifier (0.2 on some food) and blueprint time reductions are not modeled, as in the research report.
- No client run: the window is untested in the client.
