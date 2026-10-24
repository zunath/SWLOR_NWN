# Crafting revamp: the order of the Researcher line's shared purchases

Command: `SWLOR.CraftSim researcher-perks --order --runs 400 --seed 1`

## Inputs

- Seed 1; 400 experiments per case and build, every build on the same dice, as `researcher-perks`. The styles are Methodical +2 trials and -15 points, Intuitive -1 trial and +20 points; the effects of the shared purchases are the perk definitions' (Steady Hands -5 CP, Second Opinion +5, Peer Review, Efficient Analysis +1 then +2 points of time cut per evidence, Broader Hypotheses +1 trial, Field Notes as each setting says), and Breakthrough is the capstone at 50. Prices stay with the ranks.
- A purchase that spends CP does nothing for a researcher whose gear gives no CP until their rank alone pays for it (CP is 0.75 a rank): Steady Hands needs 25 CP, Second Opinion the CP of a Refine Method (30, or 25 with Steady Hands owned) and Peer Review 25. "Second Opinion affordable" requires only that one; "every CP purchase affordable" requires all three, which puts them at ranks 35, 40 and 45 and the five purchases that work without CP (Field Notes twice, Efficient Analysis twice, Broader Hypotheses) at 5, 10, 15, 25 and 30 in some order.
- "Failed" counts the six comparisons (two level kinds by three gear levels) of the styles on the whole line outside the style-balance gate's thresholds; the gaps are the mean score gap over the level kind (positive favors Methodical). A passing order fails none.

## Summary

| Rule | Field Notes points per purchase | Orders | Passing | Fewest failed |
| --- | --- | --- | --- | --- |
| Every CP purchase affordable | 6 | 120 | 0 | 3 |
| Every CP purchase affordable | 8 | 120 | 0 | 2 |
| Every CP purchase affordable | 10 | 120 | 24 | 0 |
| Second Opinion affordable | 6 | 3420 | 381 | 0 |
| No rule (every order) | 6 | 10080 | 1494 | 0 |
| No rule (every order) | 10 | 10080 | 1032 | 0 |

## Every CP purchase affordable, Field Notes +6 per purchase: the best orders

| Order (rank and purchase) | Every purchase useful at its rank | Failed of 6 | Fixed levels: mean gap | Evidence decides: mean gap |
| --- | --- | --- | --- | --- |
| 5 FN I, 10 EA I, 15 FN II, 25 EA II, 30 BH, 35 SH, 40 SO, 45 PR | yes | 3 | -1.4 | -2.5 |
| 5 FN I, 10 FN II, 15 EA I, 25 EA II, 30 BH, 35 SH, 40 SO, 45 PR | yes | 3 | -1.4 | -2.5 |
| 5 FN I, 10 FN II, 15 EA I, 25 BH, 30 EA II, 35 SH, 40 SO, 45 PR | yes | 3 | -1.4 | -2.5 |
| 5 EA I, 10 FN I, 15 FN II, 25 EA II, 30 BH, 35 SH, 40 SO, 45 PR | yes | 3 | -1.4 | -2.5 |
| 5 EA I, 10 FN I, 15 FN II, 25 BH, 30 EA II, 35 SH, 40 SO, 45 PR | yes | 3 | -1.4 | -2.5 |
| 5 FN I, 10 EA I, 15 FN II, 25 BH, 30 EA II, 35 SH, 40 SO, 45 PR | yes | 3 | -1.4 | -2.5 |
| 5 FN I, 10 FN II, 15 EA I, 25 EA II, 30 BH, 35 PR, 40 SO, 45 SH | yes | 3 | -1.5 | -2.7 |
| 5 FN I, 10 EA I, 15 FN II, 25 BH, 30 EA II, 35 PR, 40 SO, 45 SH | yes | 3 | -1.5 | -2.7 |
| 5 FN I, 10 FN II, 15 EA I, 25 BH, 30 EA II, 35 PR, 40 SO, 45 SH | yes | 3 | -1.5 | -2.7 |
| 5 EA I, 10 FN I, 15 FN II, 25 BH, 30 EA II, 35 PR, 40 SO, 45 SH | yes | 3 | -1.5 | -2.7 |

## Every CP purchase affordable, Field Notes +8 per purchase: the best orders

| Order (rank and purchase) | Every purchase useful at its rank | Failed of 6 | Fixed levels: mean gap | Evidence decides: mean gap |
| --- | --- | --- | --- | --- |
| 5 EA I, 10 FN I, 15 FN II, 25 BH, 30 EA II, 35 SH, 40 SO, 45 PR | yes | 2 | -1.0 | -1.1 |
| 5 FN I, 10 FN II, 15 EA I, 25 BH, 30 EA II, 35 SH, 40 SO, 45 PR | yes | 2 | -1.0 | -1.1 |
| 5 FN I, 10 FN II, 15 EA I, 25 EA II, 30 BH, 35 SH, 40 SO, 45 PR | yes | 2 | -1.0 | -1.1 |
| 5 EA I, 10 FN I, 15 FN II, 25 EA II, 30 BH, 35 SH, 40 SO, 45 PR | yes | 2 | -1.0 | -1.1 |
| 5 FN I, 10 EA I, 15 FN II, 25 EA II, 30 BH, 35 SH, 40 SO, 45 PR | yes | 2 | -1.0 | -1.1 |
| 5 FN I, 10 EA I, 15 FN II, 25 BH, 30 EA II, 35 SH, 40 SO, 45 PR | yes | 2 | -1.0 | -1.1 |
| 5 FN I, 10 FN II, 15 EA I, 25 BH, 30 EA II, 35 PR, 40 SO, 45 SH | yes | 2 | -1.0 | -1.3 |
| 5 EA I, 10 FN I, 15 FN II, 25 BH, 30 EA II, 35 PR, 40 SO, 45 SH | yes | 2 | -1.0 | -1.3 |
| 5 FN I, 10 EA I, 15 FN II, 25 EA II, 30 BH, 35 PR, 40 SO, 45 SH | yes | 2 | -1.0 | -1.3 |
| 5 EA I, 10 FN I, 15 FN II, 25 EA II, 30 BH, 35 PR, 40 SO, 45 SH | yes | 2 | -1.0 | -1.3 |

## Every CP purchase affordable, Field Notes +10 per purchase: the best orders

| Order (rank and purchase) | Every purchase useful at its rank | Failed of 6 | Fixed levels: mean gap | Evidence decides: mean gap |
| --- | --- | --- | --- | --- |
| 5 FN I, 10 EA I, 15 FN II, 25 BH, 30 EA II, 35 SH, 40 PR, 45 SO | yes | 0 | -0.0 | +1.3 |
| 5 FN I, 10 EA I, 15 FN II, 25 EA II, 30 BH, 35 PR, 40 SH, 45 SO | yes | 0 | -0.0 | +1.3 |
| 5 FN I, 10 FN II, 15 EA I, 25 BH, 30 EA II, 35 SH, 40 PR, 45 SO | yes | 0 | -0.0 | +1.3 |
| 5 EA I, 10 FN I, 15 FN II, 25 EA II, 30 BH, 35 PR, 40 SH, 45 SO | yes | 0 | -0.0 | +1.3 |
| 5 EA I, 10 FN I, 15 FN II, 25 EA II, 30 BH, 35 SH, 40 PR, 45 SO | yes | 0 | -0.0 | +1.3 |
| 5 EA I, 10 FN I, 15 FN II, 25 BH, 30 EA II, 35 PR, 40 SH, 45 SO | yes | 0 | -0.0 | +1.3 |
| 5 EA I, 10 FN I, 15 FN II, 25 BH, 30 EA II, 35 SH, 40 PR, 45 SO | yes | 0 | -0.0 | +1.3 |
| 5 FN I, 10 EA I, 15 FN II, 25 EA II, 30 BH, 35 SH, 40 PR, 45 SO | yes | 0 | -0.0 | +1.3 |
| 5 FN I, 10 EA I, 15 FN II, 25 BH, 30 EA II, 35 PR, 40 SH, 45 SO | yes | 0 | -0.0 | +1.3 |
| 5 FN I, 10 FN II, 15 EA I, 25 EA II, 30 BH, 35 PR, 40 SH, 45 SO | yes | 0 | -0.0 | +1.3 |

## Second Opinion affordable, Field Notes +6 per purchase: the best orders

| Order (rank and purchase) | Every purchase useful at its rank | Failed of 6 | Fixed levels: mean gap | Evidence decides: mean gap |
| --- | --- | --- | --- | --- |
| 5 SH, 10 PR, 15 FN I, 25 EA I, 30 EA II, 35 SO, 40 FN II, 45 BH | no | 0 | -0.5 | +0.1 |
| 5 SH, 10 PR, 15 FN I, 25 EA I, 30 EA II, 35 FN II, 40 SO, 45 BH | no | 0 | -0.5 | +0.1 |
| 5 FN I, 10 SH, 15 PR, 25 EA I, 30 EA II, 35 SO, 40 FN II, 45 BH | no | 0 | -0.5 | +0.1 |
| 5 FN I, 10 SH, 15 PR, 25 EA I, 30 EA II, 35 FN II, 40 SO, 45 BH | no | 0 | -0.5 | +0.1 |
| 5 SH, 10 FN I, 15 PR, 25 EA I, 30 EA II, 35 SO, 40 FN II, 45 BH | no | 0 | -0.5 | +0.1 |
| 5 SH, 10 FN I, 15 PR, 25 EA I, 30 EA II, 35 FN II, 40 SO, 45 BH | no | 0 | -0.5 | +0.1 |
| 5 FN I, 10 PR, 15 SH, 25 EA I, 30 EA II, 35 SO, 40 FN II, 45 BH | no | 0 | -0.5 | +0.1 |
| 5 FN I, 10 PR, 15 SH, 25 EA I, 30 EA II, 35 FN II, 40 SO, 45 BH | no | 0 | -0.5 | +0.1 |
| 5 PR, 10 SH, 15 FN I, 25 EA I, 30 EA II, 35 SO, 40 FN II, 45 BH | no | 0 | -0.5 | +0.1 |
| 5 PR, 10 SH, 15 FN I, 25 EA I, 30 EA II, 35 FN II, 40 SO, 45 BH | no | 0 | -0.5 | +0.1 |

## No rule (every order), Field Notes +6 per purchase: the best orders

| Order (rank and purchase) | Every purchase useful at its rank | Failed of 6 | Fixed levels: mean gap | Evidence decides: mean gap |
| --- | --- | --- | --- | --- |
| 5 SH, 10 FN I, 15 PR, 25 SO, 30 EA I, 35 EA II, 40 FN II, 45 BH | no | 0 | -0.2 | +0.2 |
| 5 SH, 10 FN I, 15 PR, 25 SO, 30 EA I, 35 FN II, 40 EA II, 45 BH | no | 0 | -0.2 | +0.2 |
| 5 SH, 10 FN I, 15 PR, 25 EA I, 30 SO, 35 EA II, 40 FN II, 45 BH | no | 0 | -0.2 | +0.2 |
| 5 SH, 10 FN I, 15 PR, 25 EA I, 30 SO, 35 FN II, 40 EA II, 45 BH | no | 0 | -0.2 | +0.2 |
| 5 PR, 10 FN I, 15 SH, 25 SO, 30 EA I, 35 EA II, 40 FN II, 45 BH | no | 0 | -0.2 | +0.2 |
| 5 PR, 10 FN I, 15 SH, 25 SO, 30 EA I, 35 FN II, 40 EA II, 45 BH | no | 0 | -0.2 | +0.2 |
| 5 PR, 10 FN I, 15 SH, 25 EA I, 30 SO, 35 EA II, 40 FN II, 45 BH | no | 0 | -0.2 | +0.2 |
| 5 PR, 10 FN I, 15 SH, 25 EA I, 30 SO, 35 FN II, 40 EA II, 45 BH | no | 0 | -0.2 | +0.2 |
| 5 PR, 10 SH, 15 FN I, 25 SO, 30 EA I, 35 EA II, 40 FN II, 45 BH | no | 0 | -0.2 | +0.2 |
| 5 PR, 10 SH, 15 FN I, 25 SO, 30 EA I, 35 FN II, 40 EA II, 45 BH | no | 0 | -0.2 | +0.2 |

## No rule (every order), Field Notes +10 per purchase: the best orders

| Order (rank and purchase) | Every purchase useful at its rank | Failed of 6 | Fixed levels: mean gap | Evidence decides: mean gap |
| --- | --- | --- | --- | --- |
| 5 PR, 10 SO, 15 FN I, 25 BH, 30 FN II, 35 EA I, 40 EA II, 45 SH | no | 0 | +0.2 | +0.1 |
| 5 PR, 10 SO, 15 FN I, 25 FN II, 30 BH, 35 EA I, 40 EA II, 45 SH | no | 0 | +0.2 | +0.1 |
| 5 FN I, 10 PR, 15 SO, 25 FN II, 30 BH, 35 EA I, 40 EA II, 45 SH | no | 0 | +0.2 | +0.1 |
| 5 PR, 10 FN I, 15 SO, 25 BH, 30 FN II, 35 EA I, 40 EA II, 45 SH | no | 0 | +0.2 | +0.1 |
| 5 PR, 10 FN I, 15 SO, 25 FN II, 30 BH, 35 EA I, 40 EA II, 45 SH | no | 0 | +0.2 | +0.1 |
| 5 SO, 10 PR, 15 FN I, 25 BH, 30 FN II, 35 EA I, 40 EA II, 45 SH | no | 0 | +0.2 | +0.1 |
| 5 SO, 10 PR, 15 FN I, 25 FN II, 30 BH, 35 EA I, 40 EA II, 45 SH | no | 0 | +0.2 | +0.1 |
| 5 SO, 10 FN I, 15 PR, 25 BH, 30 FN II, 35 EA I, 40 EA II, 45 SH | no | 0 | +0.2 | +0.1 |
| 5 SO, 10 FN I, 15 PR, 25 FN II, 30 BH, 35 EA I, 40 EA II, 45 SH | no | 0 | +0.2 | +0.1 |
| 5 FN I, 10 PR, 15 SO, 25 BH, 30 FN II, 35 EA I, 40 EA II, 45 SH | no | 0 | +0.2 | +0.1 |

