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
| Fish Sense I | two moves ahead for 2 turn(s) | 72.9% | 38.6% | 7.4 |
| Patient Angler | Give Line -1 | 64.7% | 29.9% | -1.1 |
| Aggressive Angler | Reel +1 | 66.6% | 32.7% | 1.3 |
| Strong Line II | cap +5 | 66.4% | 44.6% | 7.2 |
| Hard Set II | Set Hook drain +2 | 66.2% | 34.0% | 1.8 |
| Fish Sense II | two moves ahead for 4 turn(s) | 73.6% | 41.8% | 9.3 |
| Strong Line III | cap +8 | 70.4% | 51.1% | 12.4 |
| Hard Set III | Set Hook drain +3 | 67.3% | 34.0% | 2.3 |
| Trophy Catch | rare fish -4 rungs | 59.9% | 31.6% | -2.6 |


## The fish at the angler's own level, by rank: the adaptive player with no perks and with the whole line the rank allows

| Rank | Quick Reel | Bound (no perks) | No perks | Patient Angler line | Aggressive Angler line | Target |
| --- | --- | --- | --- | --- | --- | --- |
| 5 | 60% | 72.0% | 53.5% | 60.8% | 60.8% | - |
| 10 | 60% | 72.0% | 55.9% | 65.8% | 65.8% | - |
| 15 | 60% | 72.0% | 54.1% | 76.9% | 76.9% | - |
| 20 | 60% | 72.0% | 58.6% | 82.3% | 75.0% | - |
| 25 | 60% | 72.0% | 55.3% | 82.7% | 78.6% | - |
| 30 | 60% | 72.0% | 65.0% | 88.0% | 85.0% | - |
| 35 | 60% | 72.0% | 66.5% | 89.4% | 87.5% | - |
| 40 | 60% | 72.0% | 65.9% | 95.3% | 95.9% | - |
| 45 | 60% | 72.0% | 63.6% | 97.4% | 96.8% | - |
| 50 | 60% | 72.0% | 63.5% | 91.5% | 88.6% | 90% |


## Rank 20: catch rate and fight length by the fish's level against the rank

| Rank - fish level | Quick Reel | No perks | Patient Angler line | Aggressive Angler line | Turns, no perks | Turns, Patient | Turns, Aggressive |
| --- | --- | --- | --- | --- | --- | --- | --- |
| -6 | 0% | 0.8% | 1.4% | 1.4% | 3.7 | 3.9 | 3.7 |
| -4 | 20% | 7.2% | 15.2% | 17.4% | 4.4 | 4.8 | 4.5 |
| -3 | 30% | 17.6% | 42.7% | 42.1% | 4.8 | 4.7 | 4.3 |
| -2 | 40% | 25.3% | 52.6% | 51.1% | 5.2 | 4.8 | 4.5 |
| -1 | 50% | 44.6% | 68.6% | 65.8% | 4.7 | 4.8 | 4.6 |
| 0 | 60% | 58.6% | 82.3% | 75.0% | 5.0 | 5.3 | 4.8 |
| +1 | 70% | 67.0% | 87.5% | 84.3% | 5.2 | 5.2 | 4.9 |
| +2 | 80% | 73.2% | 89.2% | 87.8% | 5.1 | 5.2 | 4.7 |
| +4 | 95% | 89.1% | 100.0% | 99.9% | 5.4 | 3.8 | 3.4 |


## Rank 35: catch rate and fight length by the fish's level against the rank

| Rank - fish level | Quick Reel | No perks | Patient Angler line | Aggressive Angler line | Turns, no perks | Turns, Patient | Turns, Aggressive |
| --- | --- | --- | --- | --- | --- | --- | --- |
| -6 | 0% | 0.5% | 4.2% | 4.0% | 3.8 | 4.3 | 4.1 |
| -4 | 20% | 4.9% | 41.9% | 41.0% | 4.9 | 4.7 | 4.6 |
| -3 | 30% | 19.4% | 52.8% | 53.6% | 5.2 | 5.1 | 4.5 |
| -2 | 40% | 41.8% | 68.5% | 66.8% | 4.9 | 5.2 | 5.0 |
| -1 | 50% | 56.1% | 82.2% | 83.3% | 5.5 | 5.2 | 4.8 |
| 0 | 60% | 66.5% | 89.4% | 87.5% | 5.2 | 5.5 | 5.0 |
| +1 | 70% | 72.8% | 95.2% | 95.3% | 5.7 | 4.9 | 4.7 |
| +2 | 80% | 84.0% | 100.0% | 99.9% | 5.7 | 3.6 | 3.5 |
| +4 | 95% | 98.3% | 100.0% | 100.0% | 4.2 | 3.0 | 2.9 |


## Rank 50: catch rate and fight length by the fish's level against the rank

| Rank - fish level | Quick Reel | No perks | Patient Angler line | Aggressive Angler line | Turns, no perks | Turns, Patient | Turns, Aggressive |
| --- | --- | --- | --- | --- | --- | --- | --- |
| -6 | 0% | 0.7% | 15.6% | 14.6% | 3.9 | 4.2 | 3.9 |
| -4 | 20% | 12.5% | 48.8% | 47.0% | 4.8 | 4.5 | 4.4 |
| -3 | 30% | 33.2% | 61.2% | 57.9% | 4.4 | 4.8 | 4.7 |
| -2 | 40% | 41.0% | 68.1% | 65.8% | 4.7 | 5.0 | 4.7 |
| -1 | 50% | 58.0% | 83.9% | 82.3% | 4.6 | 5.3 | 4.9 |
| 0 | 60% | 63.5% | 91.5% | 88.6% | 4.9 | 4.9 | 4.7 |
| +1 | 70% | 71.6% | 99.5% | 99.0% | 5.1 | 3.8 | 3.6 |
| +2 | 80% | 86.0% | 100.0% | 100.0% | 5.3 | 3.0 | 2.9 |
| +4 | 95% | 97.6% | 100.0% | 100.0% | 4.2 | 3.0 | 2.9 |


## The two styles over the full line

| Measure | Value |
| --- | --- |
| Patient Angler is the better style | 62% |
| Aggressive Angler is the better style | 38% |
| Mean gap, Patient less Aggressive (points) | 1.4 |
| Rank 20, -4 | 15% / 17% (Aggressive) |
| Rank 20, -3 | 43% / 42% (Tie) |
| Rank 20, -2 | 53% / 51% (Tie) |
| Rank 20, -1 | 69% / 66% (Patient) |
| Rank 20, 0 | 82% / 75% (Patient) |
| Rank 20, +1 | 88% / 84% (Patient) |
| Rank 20, +2 | 89% / 88% (Tie) |
| Rank 35, -4 | 42% / 41% (Tie) |
| Rank 35, -3 | 53% / 54% (Tie) |
| Rank 35, -2 | 68% / 67% (Tie) |
| Rank 35, -1 | 82% / 83% (Tie) |
| Rank 35, 0 | 89% / 88% (Tie) |
| Rank 35, +1 | 95% / 95% (Tie) |
| Rank 35, +2 | 100% / 100% (Tie) |
| Rank 50, -4 | 49% / 47% (Tie) |
| Rank 50, -3 | 61% / 58% (Patient) |
| Rank 50, -2 | 68% / 66% (Patient) |
| Rank 50, -1 | 84% / 82% (Tie) |
| Rank 50, 0 | 92% / 89% (Patient) |
| Rank 50, +1 | 100% / 99% (Tie) |
| Rank 50, +2 | 100% / 100% (Tie) |


## The six special fish: a rare fish takes the fight perks at half strength, rounded down, and Trophy Catch at full strength (1000 fights per case)

| Case (rank 50, tier 4 rod, level 52) | Rare fish landed | Common fish of the same level | Bound | Within the bound |
| --- | --- | --- | --- | --- |
| No perks | 39% | 41% | 49.0% | Yes |
| Patient Angler and the line below the capstone (half strength) | 51% | 69% | 49.0% | Not judged (fight perks at half strength) |
| Aggressive Angler and the line below the capstone (half strength) | 47% | 68% | 49.0% | Not judged (fight perks at half strength) |
| Trophy Catch alone | 46% | 41% | 49.0% | Yes |
| Patient Angler, the whole line with Trophy Catch | 52% | 68% | 49.0% | Not judged (fight perks at half strength) |
| Aggressive Angler, the whole line with Trophy Catch | 48% | 66% | 49.0% | Not judged (fight perks at half strength) |
| Strong Line I alone | 42% | - | 49.0% | - |
| Hard Set I alone | 36% | - | 49.0% | - |
| Fish Sense I alone | 38% | - | 49.0% | - |
| Patient Angler alone | 40% | - | 49.0% | - |
| Aggressive Angler alone | 37% | - | 49.0% | - |
| Strong Line II alone | 44% | - | 49.0% | - |
| Hard Set II alone | 38% | - | 49.0% | - |
| Fish Sense II alone | 37% | - | 49.0% | - |
| Strong Line III alone | 48% | - | 49.0% | - |
| Hard Set III alone | 37% | - | 49.0% | - |
| Trophy Catch alone | 46% | - | 49.0% | - |


