# Crafting revamp: the Researcher styles search

Command: `SWLOR.CraftSim researcher-perks --search --runs 400 --seed 1`

## Inputs

- Seed 1; 400 experiments per case and style, every style on the same dice, as `researcher-perks`.
- Methodical: +2 or +3 trials and -20 to -5 points of Test chance. Intuitive: 10 to 25 points of Test chance and -1 or -2 trials. 1024 pairs.
- A pair passes when, for the styles alone and again for each style on top of every other purchase of the line, all six comparisons (two level kinds by three gear levels, six cases each) are inside the style-balance gate's thresholds: each style the better choice on at least 30% of cases (a tie within 0.5 point counts half) and a mean score gap of at most 5 points.
- Distance from the approved numbers (Methodical +2 trials and -10 points, Intuitive -1 trial and +20 points): the points of Test chance each style moves, plus 5 points for each trial.

## Result

- Pairs that pass the styles alone: 60 of 1024.
- Pairs that pass the full line: 108 of 1024.
- Pairs that pass both: 49 of 1024.
- Closest to the approved numbers: Methodical +2 trials, -14 points against Intuitive -1 trial, +20 points (distance 4).

## The closest pairs that pass both

| Methodical | Intuitive | Distance | Alone: failed of 6 | Full line: failed of 6 | Alone: fixed gap | Alone: evidence gap | Line: fixed gap | Line: evidence gap | Methodical actions | Intuitive actions |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Methodical +2 trials, -14 points | Intuitive -1 trial, +20 points | 4 | 0 | 0 | +2.5 | -1.6 | +0.0 | -0.6 | 9.3 | 5.7 |
| Methodical +2 trials, -13 points | Intuitive -1 trial, +22 points | 5 | 0 | 0 | +2.9 | -1.1 | -0.1 | -0.7 | 9.3 | 5.7 |
| Methodical +2 trials, -14 points | Intuitive -1 trial, +21 points | 5 | 0 | 0 | +2.2 | -2.0 | -0.2 | -1.1 | 9.3 | 5.7 |
| Methodical +2 trials, -15 points | Intuitive -1 trial, +20 points | 5 | 0 | 0 | +1.3 | -2.8 | -0.4 | -1.5 | 9.3 | 5.7 |
| Methodical +2 trials, -12 points | Intuitive -1 trial, +24 points | 6 | 0 | 0 | +3.4 | -0.7 | -0.2 | -0.7 | 9.3 | 5.7 |
| Methodical +2 trials, -13 points | Intuitive -1 trial, +23 points | 6 | 0 | 0 | +2.6 | -1.5 | -0.3 | -1.1 | 9.3 | 5.7 |
| Methodical +2 trials, -14 points | Intuitive -1 trial, +22 points | 6 | 0 | 0 | +1.7 | -2.4 | -0.5 | -1.6 | 9.3 | 5.7 |
| Methodical +2 trials, -15 points | Intuitive -1 trial, +19 points | 6 | 0 | 0 | +2.0 | -2.1 | -0.1 | -1.1 | 9.3 | 6.3 |
| Methodical +2 trials, -15 points | Intuitive -1 trial, +21 points | 6 | 0 | 0 | +0.9 | -3.2 | -0.7 | -2.0 | 9.3 | 5.7 |
| Methodical +2 trials, -16 points | Intuitive -1 trial, +20 points | 6 | 0 | 0 | +0.2 | -4.1 | -0.8 | -2.3 | 9.3 | 5.7 |
| Methodical +2 trials, -12 points | Intuitive -1 trial, +25 points | 7 | 0 | 0 | +3.0 | -1.1 | -0.4 | -0.9 | 9.3 | 5.7 |
| Methodical +2 trials, -13 points | Intuitive -1 trial, +24 points | 7 | 0 | 0 | +2.2 | -1.9 | -0.5 | -1.4 | 9.3 | 5.7 |
| Methodical +2 trials, -14 points | Intuitive -1 trial, +23 points | 7 | 0 | 0 | +1.4 | -2.8 | -0.8 | -2.0 | 9.3 | 5.7 |
| Methodical +2 trials, -15 points | Intuitive -1 trial, +18 points | 7 | 0 | 0 | +2.7 | -1.3 | +0.1 | -0.6 | 9.3 | 6.3 |
| Methodical +2 trials, -15 points | Intuitive -1 trial, +22 points | 7 | 0 | 0 | +0.5 | -3.6 | -1.0 | -2.5 | 9.3 | 5.7 |
| Methodical +2 trials, -16 points | Intuitive -1 trial, +19 points | 7 | 0 | 0 | +0.8 | -3.4 | -0.6 | -1.9 | 9.3 | 6.3 |
| Methodical +2 trials, -16 points | Intuitive -1 trial, +21 points | 7 | 0 | 0 | -0.2 | -4.5 | -1.1 | -2.8 | 9.3 | 5.7 |
| Methodical +2 trials, -13 points | Intuitive -1 trial, +25 points | 8 | 0 | 0 | +1.8 | -2.3 | -0.7 | -1.6 | 9.3 | 5.7 |
| Methodical +2 trials, -14 points | Intuitive -1 trial, +24 points | 8 | 0 | 0 | +1.0 | -3.2 | -1.0 | -2.2 | 9.3 | 5.7 |
| Methodical +2 trials, -15 points | Intuitive -1 trial, +23 points | 8 | 0 | 0 | +0.1 | -4.0 | -1.2 | -2.9 | 9.3 | 5.7 |

## Reference pairs

The approved numbers (Methodical -10), the implemented -15 and -12 between them, against the same Intuitive (-1 trial, +20 points).

| Methodical | Intuitive | Distance | Alone: failed of 6 | Full line: failed of 6 | Alone: fixed gap | Alone: evidence gap | Line: fixed gap | Line: evidence gap | Methodical actions | Intuitive actions |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Methodical +2 trials, -15 points | Intuitive -1 trial, +20 points | 5 | 0 | 0 | +1.3 | -2.8 | -0.4 | -1.5 | 9.3 | 5.7 |
| Methodical +2 trials, -12 points | Intuitive -1 trial, +20 points | 2 | 3 | 0 | +4.9 | +0.9 | +0.9 | +1.0 | 9.3 | 5.7 |
| Methodical +2 trials, -10 points | Intuitive -1 trial, +20 points | 0 | 4 | 2 | +7.2 | +3.2 | +1.7 | +2.3 | 9.3 | 5.7 |

## The implemented pair, case by case

Methodical +2 trials, -15 points against Intuitive -1 trial, +20 points, at mid gear. A score is out of 100 (the time cut as a share of the cap at a fixed level, the reduction won as a share of today's largest where evidence decides); a case within 0.5 point is a tie. "Line" is each style on top of every other purchase the rank allows.

| Level kind | Tier | Rank | Alone: Methodical | Alone: Intuitive | Alone: leads | Line: Methodical | Line: Intuitive | Line: leads |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Fixed levels (time cut) | 2 | 20 | 63.6 | 66.1 | Intuitive | 72.0 | 69.5 | Methodical |
| Fixed levels (time cut) | 3 | 30 | 66.0 | 67.7 | Intuitive | 83.5 | 85.1 | Intuitive |
| Fixed levels (time cut) | 4 | 40 | 65.9 | 66.8 | Intuitive | 94.9 | 97.4 | Intuitive |
| Fixed levels (time cut) | 5 | 50 | 68.3 | 70.5 | Intuitive | 100.0 | 100.0 | tie |
| Fixed levels (time cut) | 1 | 50 | 84.5 | 75.8 | Methodical | 100.0 | 100.0 | tie |
| Fixed levels (time cut) | 3 | 50 | 83.8 | 76.6 | Methodical | 100.0 | 100.0 | tie |
| Evidence decides (reduction) | 2 | 20 | 58.7 | 65.3 | Intuitive | 67.4 | 69.0 | Intuitive |
| Evidence decides (reduction) | 3 | 30 | 56.7 | 64.8 | Intuitive | 65.3 | 68.3 | Intuitive |
| Evidence decides (reduction) | 4 | 40 | 60.6 | 67.0 | Intuitive | 86.6 | 92.7 | Intuitive |
| Evidence decides (reduction) | 5 | 50 | 61.5 | 67.0 | Intuitive | 89.6 | 93.1 | Intuitive |
| Evidence decides (reduction) | 1 | 50 | 78.9 | 74.9 | Methodical | 98.4 | 95.2 | Methodical |
| Evidence decides (reduction) | 3 | 50 | 78.7 | 74.4 | Methodical | 97.4 | 94.5 | Methodical |

## The leading pair, case by case

Methodical +2 trials, -14 points against Intuitive -1 trial, +20 points, at mid gear. A score is out of 100 (the time cut as a share of the cap at a fixed level, the reduction won as a share of today's largest where evidence decides); a case within 0.5 point is a tie. "Line" is each style on top of every other purchase the rank allows.

| Level kind | Tier | Rank | Alone: Methodical | Alone: Intuitive | Alone: leads | Line: Methodical | Line: Intuitive | Line: leads |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Fixed levels (time cut) | 2 | 20 | 64.7 | 66.1 | Intuitive | 73.0 | 69.5 | Methodical |
| Fixed levels (time cut) | 3 | 30 | 67.2 | 67.7 | Intuitive | 84.3 | 85.1 | Intuitive |
| Fixed levels (time cut) | 4 | 40 | 67.0 | 66.8 | tie | 95.3 | 97.4 | Intuitive |
| Fixed levels (time cut) | 5 | 50 | 69.5 | 70.5 | Intuitive | 100.0 | 100.0 | tie |
| Fixed levels (time cut) | 1 | 50 | 85.2 | 75.8 | Methodical | 100.0 | 100.0 | tie |
| Fixed levels (time cut) | 3 | 50 | 85.0 | 76.6 | Methodical | 100.0 | 100.0 | tie |
| Evidence decides (reduction) | 2 | 20 | 59.7 | 65.3 | Intuitive | 68.3 | 69.0 | Intuitive |
| Evidence decides (reduction) | 3 | 30 | 58.1 | 64.8 | Intuitive | 66.6 | 68.3 | Intuitive |
| Evidence decides (reduction) | 4 | 40 | 62.0 | 67.0 | Intuitive | 87.7 | 92.7 | Intuitive |
| Evidence decides (reduction) | 5 | 50 | 62.6 | 67.0 | Intuitive | 90.4 | 93.1 | Intuitive |
| Evidence decides (reduction) | 1 | 50 | 79.7 | 74.9 | Methodical | 98.6 | 95.2 | Methodical |
| Evidence decides (reduction) | 3 | 50 | 80.0 | 74.4 | Methodical | 97.9 | 94.5 | Methodical |

## Limitations

- The same score analog and thresholds as `researcher-perks`: the generic gates have no scoring rule for an experiment, and each comparison has only six cases, so a share is a multiple of one twelfth and one case decides a comparison. A pair that passes here at a few hundred experiments per case can miss at another seed; run `researcher-perks` on the chosen numbers.
- Length is reported (mean actions of each style alone) and not ranked: Methodical +3 trials gives experiments longer than the owner's one-or-two-turns allowance.
