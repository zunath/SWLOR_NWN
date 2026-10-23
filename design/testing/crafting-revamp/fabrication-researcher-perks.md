# Crafting revamp: the Fabrication Researcher line

Command: `SWLOR.CraftSim researcher-perks --runs 400 --seed 1 --out design/testing/crafting-revamp/fabrication-researcher-perks.md`

## Inputs

- Seed 1; 400 experiments per case and build, every build on the same dice (the build is left out of each experiment's stream), so a difference between two builds is theirs and not the luck of two samples.
- Every build takes its numbers from the perk definitions (`FabricationResearcherPerkDefinition`) at the researcher's rank; a perk with two ranks gives the highest rank that rank allows. Base values (`ExperimentRules`): 6 trials, Test chance 60% plus up to 15 points for rank, Refine Method 15 points for 30 CP, Analyze 5% per piece of spare evidence up to 25%, Peer Review 25 CP.
- Gear: no gear = 0 CP; mid gear = 26 CP; best gear = 37 CP. CP is gear CP plus 0.75 per rank.
- Cases: research level 1 stands for the fixed levels and licensed-run rolls (every Test banks evidence and the last trial Analyzes it); level 3 stands for the levels where evidence decides between a credit and a time reduction. Researchers at ranks 20, 30, 40 and 50 research the tier their rank opens, at that tier's first rank, and rank 50 also researches tiers 1 and 3 (up to 15 more points of Test chance).
- Focused player: refines before every Test it can afford, puts every Test on one reduction (credit), Analyzes with the last trial at a fixed level, and with Breakthrough spends it first. Undecided player (Peer Review only): tests both reductions for half the trials, then backs the leader and, with Peer Review, pools the evidence behind it into the leader, keeping the CP it costs back from Refine Method.
- Score (0-100): where evidence decides, the reduction won as a share of today's largest (10%), a run with no evidence counting 0; at a fixed level, the time cut as a share of the cap (25%). Style balance uses the style-balance gate's thresholds: each style the better choice on at least 30% of cases, a mean gap of at most 5 points, ties within 0.5 point count half.

## Style balance: Methodical against Intuitive

The generic gates have no scoring rule for an experiment (it has no quality meter), so this applies gate 2's thresholds to the score above, over every case. "Alone" owns the style and nothing else; "full line" owns every other purchase the rank allows too. The length columns show what each style costs in turns, which the score does not count.

| Pair | Level kind | Gear | Cases | Methodical better | Intuitive better | Mean score gap (Methodical - Intuitive) | Methodical mean actions | Intuitive mean actions | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Methodical vs Intuitive | Fixed levels (time cut) | no gear | 6 | 42% | 58% | +1.3 | 8.7 | 5.3 | Passed |
| Methodical vs Intuitive | Fixed levels (time cut) | mid gear | 6 | 33% | 67% | +1.5 | 9.5 | 5.8 | Passed |
| Methodical vs Intuitive | Fixed levels (time cut) | best gear | 6 | 33% | 67% | +1.2 | 9.7 | 6.0 | Passed |
| Methodical vs Intuitive | Evidence decides (reduction) | no gear | 6 | 33% | 67% | -2.6 | 8.7 | 5.3 | Passed |
| Methodical vs Intuitive | Evidence decides (reduction) | mid gear | 6 | 33% | 67% | -3.0 | 9.5 | 5.8 | Passed |
| Methodical vs Intuitive | Evidence decides (reduction) | best gear | 6 | 33% | 67% | -2.8 | 9.7 | 6.0 | Passed |
| Methodical (full line) vs Intuitive (full line) | Fixed levels (time cut) | no gear | 6 | 25% | 75% | -3.7 | 10.2 | 6.8 | Failed |
| Methodical (full line) vs Intuitive (full line) | Fixed levels (time cut) | mid gear | 6 | 25% | 75% | -3.7 | 11.2 | 7.5 | Failed |
| Methodical (full line) vs Intuitive (full line) | Fixed levels (time cut) | best gear | 6 | 25% | 75% | -3.8 | 11.5 | 7.8 | Failed |
| Methodical (full line) vs Intuitive (full line) | Evidence decides (reduction) | no gear | 6 | 17% | 83% | -7.0 | 10.2 | 6.8 | Failed |
| Methodical (full line) vs Intuitive (full line) | Evidence decides (reduction) | mid gear | 6 | 25% | 75% | -6.6 | 11.2 | 7.5 | Failed |
| Methodical (full line) vs Intuitive (full line) | Evidence decides (reduction) | best gear | 6 | 25% | 75% | -6.6 | 11.5 | 7.8 | Failed |

Verdict: out of balance in 6 of 12 comparison(s): Methodical (full line) vs Intuitive (full line) / Fixed levels (time cut) / no gear; Methodical (full line) vs Intuitive (full line) / Fixed levels (time cut) / mid gear; Methodical (full line) vs Intuitive (full line) / Fixed levels (time cut) / best gear; Methodical (full line) vs Intuitive (full line) / Evidence decides (reduction) / no gear; Methodical (full line) vs Intuitive (full line) / Evidence decides (reduction) / mid gear; Methodical (full line) vs Intuitive (full line) / Evidence decides (reduction) / best gear.

## The styles' magnitudes

Each row pairs a Methodical and an Intuitive candidate over the same 36 cases as the table above (two level kinds, three gear levels, six rank and tier combinations): the styles alone, and each style on top of every other purchase of the line (the full line). "Failed" counts the six comparisons (two level kinds by three gear levels) that miss gate 2's thresholds; the gaps are the mean score gap over all cases of the level kind (positive favors Methodical), for the styles alone.

| Methodical | Intuitive | Alone: failed of 6 | Full line: failed of 6 | Fixed levels: mean gap | Evidence decides: mean gap | Full line fixed levels: mean gap | Full line evidence decides: mean gap | Methodical mean actions | Intuitive mean actions |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Methodical +2 trials, -15 points | Intuitive -1 trial, +20 points | 0 | 6 | +1.3 | -2.8 | -3.7 | -6.8 | 9.3 | 5.7 |
| Methodical +2 trials, -15 points | Intuitive -1 trial, +15 points | 3 | 6 | +4.7 | +1.0 | -2.3 | -3.9 | 9.3 | 6.3 |
| Methodical +2 trials, -15 points | Intuitive -1 trial, +25 points | 0 | 6 | -0.6 | -4.9 | -5.1 | -9.5 | 9.3 | 5.7 |
| Methodical +2 trials, -14 points | Intuitive -1 trial, +20 points | 0 | 6 | +2.5 | -1.6 | -3.2 | -5.8 | 9.3 | 5.7 |
| Methodical +2 trials, -14 points | Intuitive -1 trial, +15 points | 4 | 4 | +6.0 | +2.2 | -1.8 | -2.9 | 9.3 | 6.3 |
| Methodical +2 trials, -14 points | Intuitive -1 trial, +25 points | 0 | 6 | +0.6 | -3.6 | -4.6 | -8.5 | 9.3 | 5.7 |
| Methodical +2 trials, -13 points | Intuitive -1 trial, +20 points | 3 | 4 | +3.7 | -0.3 | -2.7 | -4.7 | 9.3 | 5.7 |
| Methodical +2 trials, -13 points | Intuitive -1 trial, +15 points | 5 | 4 | +7.2 | +3.5 | -1.3 | -1.9 | 9.3 | 6.3 |
| Methodical +2 trials, -13 points | Intuitive -1 trial, +25 points | 0 | 6 | +1.8 | -2.3 | -4.1 | -7.5 | 9.3 | 5.7 |
| Methodical +2 trials, -12 points | Intuitive -1 trial, +20 points | 3 | 3 | +4.9 | +0.9 | -2.3 | -3.7 | 9.3 | 5.7 |
| Methodical +2 trials, -12 points | Intuitive -1 trial, +15 points | 6 | 2 | +8.4 | +4.7 | -0.8 | -0.8 | 9.3 | 6.3 |
| Methodical +2 trials, -12 points | Intuitive -1 trial, +25 points | 0 | 6 | +3.0 | -1.1 | -3.7 | -6.4 | 9.3 | 5.7 |
| Methodical +2 trials, -11 points | Intuitive -1 trial, +20 points | 3 | 3 | +6.1 | +2.1 | -1.8 | -2.8 | 9.3 | 5.7 |
| Methodical +2 trials, -11 points | Intuitive -1 trial, +15 points | 6 | 0 | +9.5 | +5.9 | -0.4 | +0.1 | 9.3 | 6.3 |
| Methodical +2 trials, -11 points | Intuitive -1 trial, +25 points | 3 | 6 | +4.2 | +0.0 | -3.2 | -5.5 | 9.3 | 5.7 |
| Methodical +2 trials, -10 points (the plan's number) | Intuitive -1 trial, +20 points | 4 | 3 | +7.2 | +3.2 | -1.3 | -1.9 | 9.3 | 5.7 |
| Methodical +2 trials, -10 points (the plan's number) | Intuitive -1 trial, +15 points | 6 | 1 | +10.7 | +7.0 | +0.1 | +0.9 | 9.3 | 6.3 |
| Methodical +2 trials, -10 points (the plan's number) | Intuitive -1 trial, +25 points | 3 | 4 | +5.3 | +1.2 | -2.7 | -4.7 | 9.3 | 5.7 |
| Methodical +2 trials, -5 points | Intuitive -1 trial, +20 points | 6 | 3 | +12.4 | +9.1 | +0.4 | +2.0 | 9.3 | 5.7 |
| Methodical +2 trials, -5 points | Intuitive -1 trial, +15 points | 6 | 5 | +15.8 | +12.9 | +1.9 | +4.8 | 9.3 | 6.3 |
| Methodical +2 trials, -5 points | Intuitive -1 trial, +25 points | 6 | 2 | +10.4 | +7.0 | -1.0 | -0.7 | 9.3 | 5.7 |
| Methodical +3 trials, -10 points | Intuitive -1 trial, +20 points | 6 | 3 | +13.7 | +10.2 | +0.6 | +2.2 | 10.3 | 5.7 |
| Methodical +3 trials, -10 points | Intuitive -1 trial, +15 points | 6 | 5 | +17.2 | +14.0 | +2.1 | +5.1 | 10.3 | 6.3 |
| Methodical +3 trials, -10 points | Intuitive -1 trial, +25 points | 6 | 1 | +11.8 | +8.1 | -0.8 | -0.5 | 10.3 | 5.7 |

## Each purchase alone, and the whole line (rank 50, tier 5, mid gear)

Each row is a rank-50 researcher who owns the one perk named (at its highest rank), or the whole line with that style, against the unperked researcher on the same dice. "Reaches the maximum" is the share of experiments whose reduction reached today's largest; "Reaches the cap" the share that cut the full 25% of the job.

| Build | Trials | Test chance | Reductions: mean actions | Mean reduction size | No evidence | Reaches the maximum | Fixed: mean actions | Mean time cut | Reaches the cap |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Unperked | 6 | 60% | 8.0 | 6.15% | 0.2% | 9% | 8.0 | 16.6% | 10% |
| Field Notes | 6 | 66% | 8.0 | 6.72% | 0.0% | 12% | 8.0 | 18.0% | 18% |
| Steady Hands | 6 | 60% | 8.0 | 6.15% | 0.2% | 9% | 8.0 | 16.6% | 10% |
| Broader Hypotheses | 7 | 60% | 9.0 | 7.11% | 0.0% | 22% | 9.0 | 19.3% | 29% |
| Methodical | 8 | 45% | 10.0 | 6.15% | 0.5% | 11% | 10.0 | 17.1% | 21% |
| Intuitive | 5 | 80% | 7.0 | 6.70% | 0.0% | 0% | 7.0 | 17.6% | 0% |
| Peer Review | 6 | 60% | 8.0 | 6.15% | 0.2% | 9% | 8.0 | 16.6% | 10% |
| Efficient Analysis | 6 | 60% | 8.0 | 6.15% | 0.2% | 9% | 8.0 | 21.1% | 46% |
| Second Opinion | 6 | 60% | 8.0 | 6.29% | 0.0% | 9% | 8.0 | 17.1% | 12% |
| Breakthrough | 6 | 60% | 9.0 | 6.83% | 0.0% | 13% | 9.0 | 24.3% | 89% |
| Methodical (full line) | 9 | 51% | 12.0 | 8.35% | 0.0% | 47% | 12.0 | 25.0% | 100% |
| Intuitive (full line) | 6 | 86% | 9.0 | 8.96% | 0.0% | 57% | 9.0 | 25.0% | 100% |

## Peer Review for a player who has not decided

The focused player never has evidence behind the leader, so Peer Review has nothing to move for them. A player who tests both reductions first and then backs the leader does: Peer Review pools the evidence behind the leader into it. Rank 50, tier 5, where evidence decides. The perk costs 25 CP, so it competes with Refine Method for CP: a no-gear researcher has 37 CP and cannot afford both.

| Build | Gear | Mean actions | Mean reduction size | No evidence | Reaches the maximum |
| --- | --- | --- | --- | --- | --- |
| Unperked | no gear | 7.0 | 5.08% | 0.0% | 0% |
| Peer Review | no gear | 6.5 | 5.75% | 0.2% | 5% |
| Unperked | mid gear | 8.0 | 5.04% | 0.2% | 0% |
| Peer Review | mid gear | 7.5 | 5.88% | 0.2% | 7% |
| Unperked | best gear | 8.0 | 5.00% | 0.0% | 0% |
| Peer Review | best gear | 7.5 | 5.82% | 0.0% | 5% |

## Fabrication XP per real hour with the line

Rows follow the research report: the researcher is at the tier's first rank and researches the highest-level recipe of the tier, in mid gear, every job slot kept busy, and research level 1 (the highest-paying level: the job length grows with the square of the level while the XP grows by 10% per level). The experiment's XP is the share of its trials used, so a researcher with more trials who plays them all earns the same XP: perks reach research XP only through the job's length, which the time cut shortens by at most 25% whatever the perks, and through the active time per job, which only matters when it exceeds the job's length. "At the cap" recomputes the row with the full 25% cut and the shortest experiment, the most any build can reach.

| Rank | Tier | Recipe level | Jobs | Unperked XP per hour | Methodical (full line) | Intuitive (full line) | Most any build can reach (the cap) | At-level Fabricator crafting XP per hour (0% quality) | Most / crafting |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 10 | 1 | 9 | 1 | 9818 | 9901 | 9901 | 11016 | 43200 | 26% |
| 20 | 2 | 19 | 1 | 2122 | 2217 | 2278 | 2400 | 27000 | 9% |
| 30 | 3 | 29 | 2 | 1755 | 1871 | 1927 | 1965 | 22500 | 9% |
| 40 | 4 | 39 | 2 | 931 | 1013 | 1028 | 1044 | 19800 | 5% |
| 50 | 5 | 52 | 3 | 1438 | 1598 | 1599 | 1599 | 18000 | 9% |

The highest unperked row is 9818 XP per hour; the highest with the line is 9901 (1.008x), and no build can exceed 11016 (1.12x the unperked peak) because the time cut's cap does not move. The crafting column is a Fabricator craft of 15 actions (the middle of the 10-20 target) at 8.0 s each with the Fabricator's XP multiplier for the rank's level band (`CraftMinigameXp`), no blueprint, not a first craft, at 0% quality, the lowest crafting rate; chart XP at level is 600.

## Limitations

- The focused and undecided players are stated strategies, not searches for the best play. A real player who mixes reductions or skips Refine Method lands between the rows.
- Seconds per action and the per-job overhead are estimates; the client evidence replaces them. XP per hour scales directly with them.
- The score weighs a reduction's size and a time cut against their maximums; it does not value a time cut against a credit or time reduction, or a shorter sitting against a longer one. Each Methodical turn is about six seconds, so a style's length costs far less than a shortened job saves.
- The recipe cost modifier (0.2 on some food) and blueprint time reductions are not modeled, as in the research report.
- No client run: the window is untested in the client.
