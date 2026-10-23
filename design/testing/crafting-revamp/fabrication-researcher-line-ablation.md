# Crafting revamp: which shared Researcher purchase moves the style balance

Command: `SWLOR.CraftSim researcher-perks --ablate --runs 400 --seed 1`

## Inputs

- Seed 1; 400 experiments per case and build, every build on the same dice, as `researcher-perks`. Both styles come from the perk definitions.
- A row is Methodical against Intuitive when both own the shared purchases named. "Failed" counts the six comparisons (two level kinds by three gear levels, six cases each) outside the style-balance gate's thresholds; the gaps are the mean score gap over the level kind (positive favors Methodical).
- The shared purchases: Field Notes, Steady Hands, Broader Hypotheses, Peer Review, Efficient Analysis, Second Opinion, Breakthrough.
- The line under test, read from the perk definitions: Broader Hypotheses (+1 trial) from rank 15, Second Opinion from rank 35, Field Notes +3 from rank 5 then +6 from rank 40. The candidate rows and the grid change these shared purchases and nothing else; a rank below a purchase's own rank repeats the definition's.

## The whole line, one purchase taken out

A purchase whose removal moves the gap toward zero, or the failures toward none, is one that favors a style.

| Build | Failed of 6 | Fixed levels: mean gap | Evidence decides: mean gap | Methodical mean actions | Intuitive mean actions |
| --- | --- | --- | --- | --- | --- |
| Styles alone | 0 | +1.3 | -2.8 | 9.3 | 5.7 |
| Whole line | 6 | -3.7 | -6.8 | 10.9 | 7.4 |
| Whole line without Field Notes | 6 | -4.9 | -9.5 | 10.9 | 7.4 |
| Whole line without Steady Hands | 6 | -3.8 | -6.8 | 10.8 | 7.2 |
| Whole line without Broader Hypotheses | 0 | -0.9 | +4.1 | 9.9 | 6.4 |
| Whole line without Peer Review | 6 | -3.7 | -6.8 | 10.9 | 7.4 |
| Whole line without Efficient Analysis | 6 | -4.0 | -6.8 | 10.9 | 7.4 |
| Whole line without Second Opinion | 6 | -3.9 | -7.4 | 10.9 | 7.4 |
| Whole line without Breakthrough | 6 | -4.9 | -8.0 | 10.4 | 6.9 |

## The styles alone, one purchase added

| Build | Failed of 6 | Fixed levels: mean gap | Evidence decides: mean gap | Methodical mean actions | Intuitive mean actions |
| --- | --- | --- | --- | --- | --- |
| Styles alone | 0 | +1.3 | -2.8 | 9.3 | 5.7 |
| Styles with Field Notes | 3 | +5.3 | +1.4 | 9.3 | 5.7 |
| Styles with Steady Hands | 0 | +1.2 | -2.8 | 9.4 | 5.9 |
| Styles with Broader Hypotheses | 6 | -8.8 | -12.0 | 10.3 | 6.7 |
| Styles with Peer Review | 0 | +1.3 | -2.8 | 9.3 | 5.7 |
| Styles with Efficient Analysis | 3 | -4.5 | -2.8 | 9.3 | 5.7 |
| Styles with Second Opinion | 0 | +2.2 | -2.0 | 9.3 | 5.7 |
| Styles with Breakthrough | 3 | -1.3 | -0.3 | 9.8 | 6.2 |

## Candidate rebalances of the line

Each row changes only a shared purchase (the styles keep their numbers) and compares the styles on the whole line, as the table above. "Failed" counts the six comparisons outside the thresholds.

| Build | Failed of 6 | Fixed levels: mean gap | Evidence decides: mean gap | Methodical mean actions | Intuitive mean actions |
| --- | --- | --- | --- | --- | --- |
| The line as it is | 6 | -3.7 | -6.8 | 10.9 | 7.4 |
| Broader Hypotheses from rank 35 (not 15) | 3 | -1.6 | -4.0 | 10.6 | 7.1 |
| Broader Hypotheses from rank 45 (not 15) | 2 | -0.8 | -2.4 | 10.4 | 6.9 |
| No Broader Hypotheses | 0 | -0.9 | +4.1 | 9.9 | 6.4 |
| Field Notes doubled | 4 | -2.9 | -4.7 | 10.9 | 7.4 |
| Broader Hypotheses from rank 40 (not 15) | 3 | -1.6 | -4.0 | 10.6 | 7.1 |
| Swap Broader Hypotheses (15) and Field Notes II (40) | 3 | -0.9 | -3.4 | 10.6 | 7.1 |
| Field Notes doubled, Broader Hypotheses from rank 35 | 0 | -0.6 | -1.7 | 10.6 | 7.1 |

## Grid: where Broader Hypotheses sits and how large Field Notes is

Failed comparisons of six, the styles on the whole line. Rows: the Fabrication rank Broader Hypotheses first applies at ("never" takes it out). Columns: Field Notes' Test chance as a multiple of today's (3 points, then 6).

| Broader Hypotheses from | Field Notes x1.0 | Field Notes x1.5 | Field Notes x2.0 | Field Notes x3.0 |
| --- | --- | --- | --- | --- |
| rank 15 | 6 (-3.7 / -6.8) | 6 (-3.2 / -5.5) | 4 (-2.9 / -4.7) | 3 (-2.4 / -3.0) |
| rank 25 | 3 (-2.2 / -5.4) | 0 (-1.7 / -4.0) | 0 (-1.3 / -3.2) | 0 (-0.6 / -1.4) |
| rank 35 | 3 (-1.6 / -4.0) | 0 (-1.0 / -2.6) | 0 (-0.6 / -1.7) | 0 (+0.0 / +0.2) |
| rank 45 | 2 (-0.8 / -2.4) | 0 (-0.3 / -0.8) | 0 (+0.1 / +0.1) | 1 (+0.8 / +2.3) |
| never | 0 (-0.9 / +4.1) | 3 (-0.3 / +6.0) | 3 (+0.1 / +7.4) | 3 (+0.8 / +10.3) |

