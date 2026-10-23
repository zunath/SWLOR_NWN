# Crafting revamp: the Researcher styles search

Command: `SWLOR.CraftSim researcher-perks --search --runs 400 --seed 1`

## Inputs

- Seed 1; 400 experiments per case and style, every style on the same dice, as `researcher-perks`.
- Methodical: +2 or +3 trials and -20 to -5 points of Test chance. Intuitive: 10 to 25 points of Test chance and -1 or -2 trials. 1024 pairs.
- A pair passes when, for the styles alone and again for each style on top of every other purchase of the line, all six comparisons (two level kinds by three gear levels, six cases each) are inside the style-balance gate's thresholds: each style the better choice on at least 30% of cases (a tie within 0.5 point counts half) and a mean score gap of at most 5 points.
- Distance from the approved numbers (Methodical +2 trials and -10 points, Intuitive -1 trial and +20 points): the points of Test chance each style moves, plus 5 points for each trial.

## Result

- Pairs that pass the styles alone: 60 of 1024.
- Pairs that pass the full line: 40 of 1024.
- Pairs that pass both: 0 of 1024.
- No pair passes both. Fewest comparisons failed: 4 of 12 (4 alone, 0 with the line), by Methodical +2 trials, -20 points against Intuitive -2 trials, +21 points (distance 16).

## The best pairs (none passes both)

| Methodical | Intuitive | Distance | Alone: failed of 6 | Full line: failed of 6 | Alone: fixed gap | Alone: evidence gap | Line: fixed gap | Line: evidence gap | Methodical actions | Intuitive actions |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Methodical +2 trials, -20 points | Intuitive -2 trials, +21 points | 16 | 4 | 0 | +12.0 | +5.1 | -0.9 | +4.6 | 9.3 | 4.7 |
| Methodical +2 trials, -20 points | Intuitive -2 trials, +22 points | 17 | 4 | 1 | +11.7 | +4.8 | -1.2 | +4.2 | 9.3 | 4.7 |
| Methodical +2 trials, -20 points | Intuitive -2 trials, +24 points | 19 | 3 | 2 | +11.2 | +4.3 | -1.8 | +3.4 | 9.3 | 4.7 |
| Methodical +2 trials, -20 points | Intuitive -2 trials, +25 points | 20 | 3 | 2 | +11.0 | +4.0 | -2.1 | +3.0 | 9.3 | 4.7 |
| Methodical +2 trials, -10 points | Intuitive -1 trial, +21 points | 1 | 3 | 3 | +6.9 | +2.9 | -1.6 | -2.4 | 9.3 | 5.7 |
| Methodical +2 trials, -11 points | Intuitive -1 trial, +20 points | 1 | 3 | 3 | +6.1 | +2.1 | -1.8 | -2.8 | 9.3 | 5.7 |
| Methodical +2 trials, -10 points | Intuitive -1 trial, +22 points | 2 | 3 | 3 | +6.4 | +2.4 | -1.9 | -3.0 | 9.3 | 5.7 |
| Methodical +2 trials, -11 points | Intuitive -1 trial, +21 points | 2 | 3 | 3 | +5.7 | +1.7 | -2.1 | -3.3 | 9.3 | 5.7 |
| Methodical +2 trials, -12 points | Intuitive -1 trial, +20 points | 2 | 3 | 3 | +4.9 | +0.9 | -2.3 | -3.7 | 9.3 | 5.7 |
| Methodical +2 trials, -7 points | Intuitive -1 trial, +20 points | 3 | 6 | 0 | +10.4 | +6.7 | -0.3 | +0.6 | 9.3 | 5.7 |
| Methodical +2 trials, -8 points | Intuitive -1 trial, +19 points | 3 | 6 | 0 | +10.0 | +6.3 | -0.4 | +0.3 | 9.3 | 6.3 |
| Methodical +2 trials, -9 points | Intuitive -1 trial, +18 points | 3 | 6 | 0 | +9.6 | +5.9 | -0.5 | -0.0 | 9.3 | 6.3 |
| Methodical +2 trials, -10 points | Intuitive -1 trial, +17 points | 3 | 6 | 0 | +9.3 | +5.5 | -0.5 | -0.3 | 9.3 | 6.3 |
| Methodical +2 trials, -10 points | Intuitive -1 trial, +23 points | 3 | 3 | 3 | +6.1 | +2.0 | -2.2 | -3.6 | 9.3 | 5.7 |
| Methodical +2 trials, -11 points | Intuitive -1 trial, +22 points | 3 | 3 | 3 | +5.3 | +1.3 | -2.4 | -3.9 | 9.3 | 5.7 |
| Methodical +2 trials, -12 points | Intuitive -1 trial, +19 points | 3 | 3 | 3 | +5.6 | +1.7 | -2.0 | -3.2 | 9.3 | 6.3 |
| Methodical +2 trials, -12 points | Intuitive -1 trial, +21 points | 3 | 3 | 3 | +4.6 | +0.6 | -2.5 | -4.2 | 9.3 | 5.7 |
| Methodical +2 trials, -7 points | Intuitive -1 trial, +21 points | 4 | 6 | 0 | +10.0 | +6.4 | -0.5 | +0.1 | 9.3 | 5.7 |
| Methodical +2 trials, -9 points | Intuitive -1 trial, +17 points | 4 | 6 | 0 | +10.2 | +6.6 | -0.2 | +0.5 | 9.3 | 6.3 |
| Methodical +2 trials, -9 points | Intuitive -1 trial, +23 points | 4 | 3 | 3 | +7.1 | +3.2 | -1.8 | -2.7 | 9.3 | 5.7 |

## Reference pairs

The approved numbers, and the numbers in force before this search (Methodical -15), against the same Intuitive.

| Methodical | Intuitive | Distance | Alone: failed of 6 | Full line: failed of 6 | Alone: fixed gap | Alone: evidence gap | Line: fixed gap | Line: evidence gap | Methodical actions | Intuitive actions |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Methodical +2 trials, -15 points | Intuitive -1 trial, +20 points | 5 | 0 | 6 | +1.3 | -2.8 | -3.7 | -6.8 | 9.3 | 5.7 |
| Methodical +2 trials, -12 points | Intuitive -1 trial, +20 points | 2 | 3 | 3 | +4.9 | +0.9 | -2.3 | -3.7 | 9.3 | 5.7 |
| Methodical +2 trials, -10 points | Intuitive -1 trial, +20 points | 0 | 4 | 3 | +7.2 | +3.2 | -1.3 | -1.9 | 9.3 | 5.7 |

## The implemented pair, case by case

Methodical +2 trials, -15 points against Intuitive -1 trial, +20 points, at mid gear. A score is out of 100 (the time cut as a share of the cap at a fixed level, the reduction won as a share of today's largest where evidence decides); a case within 0.5 point is a tie. "Line" is each style on top of every other purchase the rank allows.

| Level kind | Tier | Rank | Alone: Methodical | Alone: Intuitive | Alone: leads | Line: Methodical | Line: Intuitive | Line: leads |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Fixed levels (time cut) | 2 | 20 | 63.6 | 66.1 | Intuitive | 75.2 | 84.0 | Intuitive |
| Fixed levels (time cut) | 3 | 30 | 66.0 | 67.7 | Intuitive | 84.9 | 94.0 | Intuitive |
| Fixed levels (time cut) | 4 | 40 | 65.9 | 66.8 | Intuitive | 91.0 | 95.3 | Intuitive |
| Fixed levels (time cut) | 5 | 50 | 68.3 | 70.5 | Intuitive | 99.9 | 100.0 | tie |
| Fixed levels (time cut) | 1 | 50 | 84.5 | 75.8 | Methodical | 100.0 | 100.0 | tie |
| Fixed levels (time cut) | 3 | 50 | 83.8 | 76.6 | Methodical | 100.0 | 100.0 | tie |
| Evidence decides (reduction) | 2 | 20 | 58.7 | 65.3 | Intuitive | 70.3 | 82.0 | Intuitive |
| Evidence decides (reduction) | 3 | 30 | 56.7 | 64.8 | Intuitive | 68.0 | 81.6 | Intuitive |
| Evidence decides (reduction) | 4 | 40 | 60.6 | 67.0 | Intuitive | 79.3 | 88.5 | Intuitive |
| Evidence decides (reduction) | 5 | 50 | 61.5 | 67.0 | Intuitive | 83.5 | 89.6 | Intuitive |
| Evidence decides (reduction) | 1 | 50 | 78.9 | 74.9 | Methodical | 95.4 | 95.2 | tie |
| Evidence decides (reduction) | 3 | 50 | 78.7 | 74.4 | Methodical | 95.2 | 94.5 | Methodical |

## The leading pair, case by case

Methodical +2 trials, -20 points against Intuitive -2 trials, +21 points, at mid gear. A score is out of 100 (the time cut as a share of the cap at a fixed level, the reduction won as a share of today's largest where evidence decides); a case within 0.5 point is a tie. "Line" is each style on top of every other purchase the rank allows.

| Level kind | Tier | Rank | Alone: Methodical | Alone: Intuitive | Alone: leads | Line: Methodical | Line: Intuitive | Line: leads |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Fixed levels (time cut) | 2 | 20 | 57.2 | 50.6 | Methodical | 67.8 | 68.3 | tie |
| Fixed levels (time cut) | 3 | 30 | 58.5 | 52.0 | Methodical | 78.9 | 83.9 | Intuitive |
| Fixed levels (time cut) | 4 | 40 | 59.7 | 51.2 | Methodical | 86.7 | 86.5 | tie |
| Fixed levels (time cut) | 5 | 50 | 62.1 | 54.9 | Methodical | 99.7 | 100.0 | tie |
| Fixed levels (time cut) | 1 | 50 | 78.8 | 56.7 | Methodical | 100.0 | 100.0 | tie |
| Fixed levels (time cut) | 3 | 50 | 78.0 | 57.5 | Methodical | 100.0 | 100.0 | tie |
| Evidence decides (reduction) | 2 | 20 | 51.9 | 52.9 | Intuitive | 63.1 | 67.4 | Intuitive |
| Evidence decides (reduction) | 3 | 30 | 50.2 | 52.1 | Intuitive | 60.8 | 67.3 | Intuitive |
| Evidence decides (reduction) | 4 | 40 | 53.6 | 53.9 | tie | 73.0 | 72.0 | Methodical |
| Evidence decides (reduction) | 5 | 50 | 55.2 | 54.1 | Methodical | 79.0 | 73.2 | Methodical |
| Evidence decides (reduction) | 1 | 50 | 73.3 | 57.9 | Methodical | 92.4 | 76.1 | Methodical |
| Evidence decides (reduction) | 3 | 50 | 73.4 | 57.5 | Methodical | 91.9 | 75.8 | Methodical |

## Limitations

- The same score analog and thresholds as `researcher-perks`: the generic gates have no scoring rule for an experiment, and each comparison has only six cases, so a share is a multiple of one twelfth and one case decides a comparison. A pair that passes here at a few hundred experiments per case can miss at another seed; run `researcher-perks` on the chosen numbers.
- Length is reported (mean actions of each style alone) and not ranked: Methodical +3 trials gives experiments longer than the owner's one-or-two-turns allowance.
