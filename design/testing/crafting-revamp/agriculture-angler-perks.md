# Crafting revamp: the Angler perk line

- Command: `SWLOR.CraftSim angler-perks --runs 1000 --seed 1 --repo . --out design/testing/crafting-revamp/agriculture-angler-perks.md`
- Seed 1; 1000 fights per case, each with its own dice. The adaptive player (depth 3, forecast samples per result 2, budget 4000 engine copies per decision, 0.5% of value lost per extra action, heuristic LineLeafHeuristic) stands for the skilled fight.
- Every number is the game's own perk definition (`AgricultureAnglerPerkDefinition`) played at the rank the row names: an angler owns the purchases that rank allows, at the highest level it allows. Quick Reel is today's roll at Willpower 10.
- The unperked fight is bounded to 15% above Quick Reel plus 3 points. The whole line is held to the owner's target instead: about 90% of the fish at the angler's own level at rank 50, where it is complete (judged within 5 points).

## Each purchase alone: the adaptive player's catch rate at rank 50, owning only that purchase at the level named, against a fish of the angler's own level and one 3 levels above

| Purchase (alone) | Effect read by the Line | Fish at the rank's level | Fish 3 levels above | Added points (mean of both) |
| --- | --- | --- | --- | --- |
| No perks | - | 63.5% | 33.2% | - |
| Strong Line I | cap +2 | 64.0% | 39.8% | 3.5 |
| Hard Set I | Set Hook drain +1 | 66.3% | 32.0% | 0.8 |
| Fish Sense I | two moves ahead for 1 turn(s) | 68.8% | 38.7% | 5.4 |
| Patient Angler | Give Line -1 | 64.7% | 29.9% | -1.1 |
| Aggressive Angler | Reel +1 | 66.6% | 32.7% | 1.3 |
| Strong Line II | cap +5 | 66.4% | 44.6% | 7.2 |
| Hard Set II | Set Hook drain +2 | 66.2% | 34.0% | 1.8 |
| Fish Sense II | two moves ahead for 3 turn(s) | 71.6% | 42.5% | 8.7 |
| Strong Line III | cap +8 | 70.4% | 51.1% | 12.4 |
| Hard Set III | Set Hook drain +3 | 67.3% | 34.0% | 2.3 |
| Trophy Catch | rare fish -4 rungs | 59.9% | 31.6% | -2.6 |


## The fish at the angler's own level, by rank: the adaptive player with no perks and with the whole line the rank allows

| Rank | Quick Reel | Bound (no perks) | No perks | Patient Angler line | Aggressive Angler line | Target |
| --- | --- | --- | --- | --- | --- | --- |
| 5 | 60% | 72.0% | 53.5% | 60.8% | 60.8% | - |
| 10 | 60% | 72.0% | 55.9% | 65.8% | 65.8% | - |
| 15 | 60% | 72.0% | 54.1% | 76.2% | 76.2% | - |
| 20 | 60% | 72.0% | 58.6% | 79.9% | 78.4% | - |
| 25 | 60% | 72.0% | 55.3% | 79.6% | 76.2% | - |
| 30 | 60% | 72.0% | 65.0% | 86.0% | 83.4% | - |
| 35 | 60% | 72.0% | 66.5% | 87.6% | 86.2% | - |
| 40 | 60% | 72.0% | 65.9% | 93.8% | 94.4% | - |
| 45 | 60% | 72.0% | 63.6% | 96.1% | 96.0% | - |
| 50 | 60% | 72.0% | 63.5% | 91.5% | 90.6% | 90% |


## Rank 20: catch rate and fight length by the fish's level against the rank

| Rank - fish level | Quick Reel | No perks | Patient Angler line | Aggressive Angler line | Turns, no perks | Turns, Patient | Turns, Aggressive |
| --- | --- | --- | --- | --- | --- | --- | --- |
| -6 | 0% | 0.8% | 1.0% | 1.5% | 3.7 | 4.0 | 4.0 |
| -4 | 20% | 7.2% | 16.1% | 14.5% | 4.4 | 4.6 | 4.4 |
| -3 | 30% | 17.6% | 37.3% | 39.5% | 4.8 | 4.6 | 4.4 |
| -2 | 40% | 25.3% | 49.4% | 46.0% | 5.2 | 4.9 | 4.5 |
| -1 | 50% | 44.6% | 64.2% | 61.4% | 4.7 | 4.9 | 4.6 |
| 0 | 60% | 58.6% | 79.9% | 78.4% | 5.0 | 5.3 | 4.8 |
| +1 | 70% | 67.0% | 85.8% | 82.7% | 5.2 | 5.5 | 5.0 |
| +2 | 80% | 73.2% | 88.4% | 86.5% | 5.1 | 5.2 | 4.9 |
| +4 | 95% | 89.1% | 99.7% | 98.5% | 5.4 | 3.8 | 3.6 |


## Rank 35: catch rate and fight length by the fish's level against the rank

| Rank - fish level | Quick Reel | No perks | Patient Angler line | Aggressive Angler line | Turns, no perks | Turns, Patient | Turns, Aggressive |
| --- | --- | --- | --- | --- | --- | --- | --- |
| -6 | 0% | 0.5% | 5.3% | 4.3% | 3.8 | 4.4 | 4.1 |
| -4 | 20% | 4.9% | 39.5% | 39.1% | 4.9 | 4.8 | 4.6 |
| -3 | 30% | 19.4% | 53.8% | 51.9% | 5.2 | 5.0 | 4.7 |
| -2 | 40% | 41.8% | 65.8% | 65.4% | 4.9 | 5.3 | 5.0 |
| -1 | 50% | 56.1% | 81.9% | 80.0% | 5.5 | 5.4 | 5.0 |
| 0 | 60% | 66.5% | 87.6% | 86.2% | 5.2 | 5.3 | 5.1 |
| +1 | 70% | 72.8% | 95.0% | 93.7% | 5.7 | 5.0 | 4.9 |
| +2 | 80% | 84.0% | 99.9% | 99.9% | 5.7 | 3.6 | 3.5 |
| +4 | 95% | 98.3% | 100.0% | 100.0% | 4.2 | 3.0 | 2.9 |


## Rank 50: catch rate and fight length by the fish's level against the rank

| Rank - fish level | Quick Reel | No perks | Patient Angler line | Aggressive Angler line | Turns, no perks | Turns, Patient | Turns, Aggressive |
| --- | --- | --- | --- | --- | --- | --- | --- |
| -6 | 0% | 0.7% | 15.1% | 14.9% | 3.9 | 4.4 | 3.9 |
| -4 | 20% | 12.5% | 43.5% | 46.4% | 4.8 | 4.9 | 4.4 |
| -3 | 30% | 33.2% | 59.6% | 59.5% | 4.4 | 4.8 | 4.6 |
| -2 | 40% | 41.0% | 64.4% | 65.2% | 4.7 | 5.0 | 4.6 |
| -1 | 50% | 58.0% | 83.7% | 82.9% | 4.6 | 5.2 | 5.0 |
| 0 | 60% | 63.5% | 91.5% | 90.6% | 4.9 | 5.0 | 4.8 |
| +1 | 70% | 71.6% | 99.1% | 98.9% | 5.1 | 3.8 | 3.5 |
| +2 | 80% | 86.0% | 100.0% | 100.0% | 5.3 | 3.0 | 2.9 |
| +4 | 95% | 97.6% | 100.0% | 100.0% | 4.2 | 3.0 | 2.9 |


## The two styles over the full line

| Measure | Value |
| --- | --- |
| Patient Angler is the better style | 52% |
| Aggressive Angler is the better style | 48% |
| Mean gap, Patient less Aggressive (points) | 0.8 |
| Rank 20, -4 | 16% / 14% (Tie) |
| Rank 20, -3 | 37% / 40% (Aggressive) |
| Rank 20, -2 | 49% / 46% (Patient) |
| Rank 20, -1 | 64% / 61% (Patient) |
| Rank 20, 0 | 80% / 78% (Tie) |
| Rank 20, +1 | 86% / 83% (Patient) |
| Rank 20, +2 | 88% / 86% (Tie) |
| Rank 35, -4 | 40% / 39% (Tie) |
| Rank 35, -3 | 54% / 52% (Tie) |
| Rank 35, -2 | 66% / 65% (Tie) |
| Rank 35, -1 | 82% / 80% (Tie) |
| Rank 35, 0 | 88% / 86% (Tie) |
| Rank 35, +1 | 95% / 94% (Tie) |
| Rank 35, +2 | 100% / 100% (Tie) |
| Rank 50, -4 | 44% / 46% (Aggressive) |
| Rank 50, -3 | 60% / 60% (Tie) |
| Rank 50, -2 | 64% / 65% (Tie) |
| Rank 50, -1 | 84% / 83% (Tie) |
| Rank 50, 0 | 92% / 91% (Tie) |
| Rank 50, +1 | 99% / 99% (Tie) |
| Rank 50, +2 | 100% / 100% (Tie) |


## The six special fish: a rare fish takes the fight perks at half strength, rounded down, and Trophy Catch at full strength (1000 fights per case)

| Case (rank 50, tier 4 rod, level 52) | Rare fish landed | Common fish of the same level | Bound | Within the bound |
| --- | --- | --- | --- | --- |
| No perks | 39% | 41% | 49.0% | Yes |
| Patient Angler and the line below the capstone (half strength) | 51% | 68% | 49.0% | Not judged (fight perks at half strength) |
| Aggressive Angler and the line below the capstone (half strength) | 51% | 66% | 49.0% | Not judged (fight perks at half strength) |
| Trophy Catch alone | 46% | 41% | 49.0% | Yes |
| Patient Angler, the whole line with Trophy Catch | 49% | 64% | 49.0% | Not judged (fight perks at half strength) |
| Aggressive Angler, the whole line with Trophy Catch | 52% | 65% | 49.0% | Not judged (fight perks at half strength) |
| Strong Line I alone | 42% | - | 49.0% | - |
| Hard Set I alone | 36% | - | 49.0% | - |
| Fish Sense I alone | 37% | - | 49.0% | - |
| Patient Angler alone | 40% | - | 49.0% | - |
| Aggressive Angler alone | 37% | - | 49.0% | - |
| Strong Line II alone | 44% | - | 49.0% | - |
| Hard Set II alone | 38% | - | 49.0% | - |
| Fish Sense II alone | 37% | - | 49.0% | - |
| Strong Line III alone | 48% | - | 49.0% | - |
| Hard Set III alone | 37% | - | 49.0% | - |
| Trophy Catch alone | 46% | - | 49.0% | - |


