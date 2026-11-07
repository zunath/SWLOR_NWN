# Crafting revamp: the Agriculture Chef line

Command: `SWLOR.CraftSim chef-perks --runs 20 --seed 1 --recipes-per-band 4 --repo . --out design/testing/crafting-revamp/agriculture-chef-perks.md`

## Inputs

- Seed 1; 20 attempts per recipe, gear tier and build; up to 4 recipes sampled from each at-level band (`GateThresholds.RecipesPerBand`).
- Gear: no gear (Control 0, Craftsmanship 0, CP 0); mid gear (Control 13, Craftsmanship 12, CP 20); best gear (Control 29, Craftsmanship 30, CP 37); fully modded gear (Control 119, Craftsmanship 120, CP 37).
- The adaptive player (`AdaptivePolicy`, depth 3, `KitchenLeafHeuristic`) plays every build on the same dice, so a difference between two builds is theirs and not the luck of two samples.
- Every build takes its numbers from the perk definitions (`AgricultureChefPerkDefinition`) at the rank it is played at; a perk with two ranks gives the highest rank that rank allows. Seasoned Palate is a switch the cook makes for each dish: rows say whether it is on.
- CP spent is what the craft used of the CP available (gear plus 0.75 per rank, plus 31 from rank 25).

## Rank 50: each purchase alone, and the whole line

A rank-50 cook works recipes of levels 41-53 (4 played, of 88). Each row owns the one perk named (at its highest rank), or the whole line with that style. "Actions" is the mean actions per craft; the change is against the unperked cook at the same gear. A negative change is a shorter craft.

| Build | Gear | Mean quality | Quality change | Full quality | Items made | Mean actions | Actions change | CP spent | CP available |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Unperked | no gear | 72.5 | - | 54% | 91% | 20.7 | - | 38.1 | 68 |
| Pantry Staples | no gear | 85.5 | +13.0 | 68% | 99% | 20.2 | -0.5 | 34.5 | 68 |
| Sharp Palate | no gear | 72.8 | +0.2 | 55% | 91% | 20.7 | -0.0 | 37.6 | 68 |
| Seasoned Palate | no gear | 72.5 | 0.0 | 54% | 91% | 20.7 | 0.0 | 38.1 | 68 |
| Seasoned Palate (on) | no gear | 70.9 | -1.7 | 60% | 92% | 21.3 | +0.6 | 39.4 | 68 |
| Slow Cooking | no gear | 85.0 | +12.5 | 75% | 95% | 20.0 | -0.7 | 31.4 | 68 |
| Flash Cooking | no gear | 78.6 | +6.1 | 64% | 95% | 19.7 | -1.0 | 34.0 | 68 |
| Reduce | no gear | 74.2 | +1.7 | 59% | 92% | 19.4 | -1.3 | 48.5 | 68 |
| Aromatic Kitchen | no gear | 73.0 | +0.5 | 56% | 91% | 21.4 | +0.7 | 38.8 | 68 |
| Steady Wrist | no gear | 77.8 | +5.2 | 55% | 92% | 20.6 | -0.1 | 34.8 | 68 |
| Signature Dish | no gear | 81.1 | +8.5 | 56% | 94% | 19.0 | -1.7 | 29.6 | 68 |
| Slow Cooking (full line) | no gear | 92.5 | +20.0 | 72% | 100% | 15.8 | -4.9 | 32.9 | 68 |
| Flash Cooking (full line) | no gear | 88.4 | +15.9 | 76% | 96% | 16.9 | -3.8 | 36.7 | 68 |
| Slow Cooking (full line, Seasoned Palate on) | no gear | 87.1 | +14.6 | 78% | 100% | 15.8 | -4.9 | 34.1 | 68 |
| Flash Cooking (full line, Seasoned Palate on) | no gear | 96.2 | +23.7 | 76% | 100% | 16.5 | -4.2 | 37.9 | 68 |
| Unperked | mid gear | 80.2 | - | 61% | 95% | 20.4 | - | 38.4 | 88 |
| Pantry Staples | mid gear | 78.3 | -1.9 | 62% | 94% | 20.5 | +0.1 | 40.3 | 88 |
| Sharp Palate | mid gear | 83.7 | +3.5 | 68% | 98% | 19.9 | -0.5 | 37.2 | 88 |
| Seasoned Palate | mid gear | 80.2 | 0.0 | 61% | 95% | 20.4 | 0.0 | 38.4 | 88 |
| Seasoned Palate (on) | mid gear | 78.4 | -1.8 | 65% | 90% | 20.8 | +0.3 | 36.8 | 88 |
| Slow Cooking | mid gear | 88.9 | +8.8 | 69% | 98% | 19.1 | -1.4 | 26.0 | 88 |
| Flash Cooking | mid gear | 90.0 | +9.9 | 66% | 98% | 18.6 | -1.8 | 31.6 | 88 |
| Reduce | mid gear | 88.2 | +8.1 | 71% | 98% | 18.3 | -2.1 | 55.0 | 88 |
| Aromatic Kitchen | mid gear | 81.4 | +1.3 | 60% | 96% | 20.7 | +0.2 | 38.5 | 88 |
| Steady Wrist | mid gear | 88.6 | +8.5 | 72% | 98% | 19.1 | -1.3 | 35.3 | 88 |
| Signature Dish | mid gear | 90.2 | +10.0 | 75% | 98% | 17.3 | -3.1 | 26.9 | 88 |
| Slow Cooking (full line) | mid gear | 93.0 | +12.8 | 82% | 99% | 17.1 | -3.3 | 38.9 | 88 |
| Flash Cooking (full line) | mid gear | 95.6 | +15.4 | 84% | 99% | 16.5 | -3.9 | 39.1 | 88 |
| Slow Cooking (full line, Seasoned Palate on) | mid gear | 93.0 | +12.8 | 79% | 99% | 16.1 | -4.3 | 36.2 | 88 |
| Flash Cooking (full line, Seasoned Palate on) | mid gear | 92.9 | +12.7 | 81% | 98% | 16.7 | -3.7 | 41.2 | 88 |
| Unperked | best gear | 84.8 | - | 74% | 95% | 19.1 | - | 34.8 | 105 |
| Pantry Staples | best gear | 92.2 | +7.4 | 79% | 98% | 18.9 | -0.2 | 34.3 | 105 |
| Sharp Palate | best gear | 85.4 | +0.6 | 70% | 98% | 19.4 | +0.3 | 36.6 | 105 |
| Seasoned Palate | best gear | 84.8 | 0.0 | 74% | 95% | 19.1 | 0.0 | 34.8 | 105 |
| Seasoned Palate (on) | best gear | 87.7 | +2.9 | 75% | 96% | 18.5 | -0.6 | 29.6 | 105 |
| Slow Cooking | best gear | 92.8 | +8.0 | 84% | 98% | 17.4 | -1.7 | 28.4 | 105 |
| Flash Cooking | best gear | 95.2 | +10.5 | 81% | 100% | 17.9 | -1.2 | 30.7 | 105 |
| Reduce | best gear | 94.5 | +9.7 | 74% | 99% | 17.2 | -1.9 | 53.9 | 105 |
| Aromatic Kitchen | best gear | 88.2 | +3.4 | 75% | 100% | 18.8 | -0.3 | 34.0 | 105 |
| Steady Wrist | best gear | 94.8 | +10.0 | 81% | 99% | 17.4 | -1.7 | 29.2 | 105 |
| Signature Dish | best gear | 93.5 | +8.7 | 76% | 96% | 15.4 | -3.7 | 24.6 | 105 |
| Slow Cooking (full line) | best gear | 99.9 | +15.1 | 91% | 100% | 12.8 | -6.3 | 24.5 | 105 |
| Flash Cooking (full line) | best gear | 96.0 | +11.2 | 81% | 100% | 14.0 | -5.2 | 34.0 | 105 |
| Slow Cooking (full line, Seasoned Palate on) | best gear | 94.5 | +9.7 | 79% | 100% | 14.7 | -4.4 | 32.9 | 105 |
| Flash Cooking (full line, Seasoned Palate on) | best gear | 97.3 | +12.5 | 88% | 99% | 15.5 | -3.7 | 38.6 | 105 |
| Unperked | fully modded gear | 98.4 | - | 89% | 99% | 14.5 | - | 19.4 | 105 |
| Pantry Staples | fully modded gear | 99.0 | +0.6 | 82% | 100% | 14.4 | -0.0 | 18.0 | 105 |
| Sharp Palate | fully modded gear | 99.6 | +1.2 | 90% | 100% | 13.8 | -0.7 | 17.3 | 105 |
| Seasoned Palate | fully modded gear | 98.4 | 0.0 | 89% | 99% | 14.5 | 0.0 | 19.4 | 105 |
| Seasoned Palate (on) | fully modded gear | 89.7 | -8.7 | 80% | 100% | 17.9 | +3.5 | 27.6 | 105 |
| Slow Cooking | fully modded gear | 97.3 | -1.1 | 90% | 100% | 13.8 | -0.6 | 17.7 | 105 |
| Flash Cooking | fully modded gear | 95.9 | -2.5 | 86% | 99% | 14.4 | -0.1 | 18.4 | 105 |
| Reduce | fully modded gear | 97.9 | -0.5 | 90% | 100% | 15.0 | +0.5 | 40.3 | 105 |
| Aromatic Kitchen | fully modded gear | 98.7 | +0.3 | 89% | 100% | 14.8 | +0.3 | 17.2 | 105 |
| Steady Wrist | fully modded gear | 97.4 | -1.0 | 91% | 100% | 13.8 | -0.6 | 16.0 | 105 |
| Signature Dish | fully modded gear | 95.7 | -2.7 | 78% | 100% | 12.8 | -1.6 | 14.2 | 105 |
| Slow Cooking (full line) | fully modded gear | 99.9 | +1.5 | 92% | 100% | 10.9 | -3.5 | 20.8 | 105 |
| Flash Cooking (full line) | fully modded gear | 99.6 | +1.3 | 89% | 100% | 11.7 | -2.8 | 24.1 | 105 |
| Slow Cooking (full line, Seasoned Palate on) | fully modded gear | 97.8 | -0.6 | 91% | 100% | 11.4 | -3.1 | 23.2 | 105 |
| Flash Cooking (full line, Seasoned Palate on) | fully modded gear | 98.0 | -0.4 | 88% | 100% | 11.9 | -2.6 | 28.9 | 105 |

### The two styles: which is better, on quality and on actions

Each recipe is a tie within 0.5 quality point or 0.25 action, and a tie gives each style half a recipe. The style-balance gate (gate 2) reads the quality columns only.

| Pair | Gear | Recipes | First better quality | Second better quality | Mean quality gap | First fewer actions | Second fewer actions | Mean actions gap (first - second) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Slow Cooking vs Flash Cooking | no gear | 4 | 75% | 25% | +6.4 | 50% | 50% | +0.30 |
| Slow Cooking vs Flash Cooking | mid gear | 4 | 38% | 62% | -1.1 | 25% | 75% | +0.49 |
| Slow Cooking vs Flash Cooking | best gear | 4 | 50% | 50% | -2.4 | 75% | 25% | -0.53 |
| Slow Cooking vs Flash Cooking | fully modded gear | 4 | 62% | 38% | +1.4 | 50% | 50% | -0.54 |
| Slow Cooking (full line) vs Flash Cooking (full line) | no gear | 4 | 62% | 38% | +4.1 | 75% | 25% | -1.09 |
| Slow Cooking (full line) vs Flash Cooking (full line) | mid gear | 4 | 50% | 50% | -2.6 | 25% | 75% | +0.59 |
| Slow Cooking (full line) vs Flash Cooking (full line) | best gear | 4 | 88% | 12% | +4.0 | 100% | 0% | -1.13 |
| Slow Cooking (full line) vs Flash Cooking (full line) | fully modded gear | 4 | 62% | 38% | +0.2 | 62% | 38% | -0.78 |
| Slow Cooking (full line, Seasoned Palate on) vs Flash Cooking (full line, Seasoned Palate on) | no gear | 4 | 25% | 75% | -9.1 | 75% | 25% | -0.71 |
| Slow Cooking (full line, Seasoned Palate on) vs Flash Cooking (full line, Seasoned Palate on) | mid gear | 4 | 62% | 38% | +0.1 | 50% | 50% | -0.61 |
| Slow Cooking (full line, Seasoned Palate on) vs Flash Cooking (full line, Seasoned Palate on) | best gear | 4 | 12% | 88% | -2.8 | 75% | 25% | -0.75 |
| Slow Cooking (full line, Seasoned Palate on) vs Flash Cooking (full line, Seasoned Palate on) | fully modded gear | 4 | 50% | 50% | -0.2 | 50% | 50% | -0.54 |

## The whole line through the ranks (at-level crafts)

Every recipe is crafted at its own level's rank (the plan's item-craft cohort), so a band's cook owns the purchases that rank allows: from rank 5 up, a style from rank 20, Reduce from rank 25, the capstone at 50. Seasoned Palate is off.

| Band | Gear | Build | Mean quality | Full quality | Mean actions | Actions change | CP spent | CP available |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | Unperked | 93.7 | 78% | 13.0 | - | 1.2 | 4 |
| 1-10 | no gear | Slow Cooking (full line) | 94.9 | 76% | 13.0 | -0.1 | 1.1 | 4 |
| 1-10 | no gear | Flash Cooking (full line) | 94.9 | 76% | 13.0 | -0.1 | 1.1 | 4 |
| 1-10 | mid gear | Unperked | 97.3 | 88% | 12.2 | - | 9.3 | 24 |
| 1-10 | mid gear | Slow Cooking (full line) | 98.4 | 90% | 11.6 | -0.7 | 8.8 | 24 |
| 1-10 | mid gear | Flash Cooking (full line) | 98.4 | 90% | 11.6 | -0.7 | 8.8 | 24 |
| 1-10 | best gear | Unperked | 97.3 | 94% | 10.7 | - | 10.0 | 40 |
| 1-10 | best gear | Slow Cooking (full line) | 97.8 | 95% | 10.7 | -0.0 | 10.5 | 40 |
| 1-10 | best gear | Flash Cooking (full line) | 97.8 | 95% | 10.7 | -0.0 | 10.5 | 40 |
| 1-10 | fully modded gear | Unperked | 100.0 | 96% | 8.8 | - | 6.1 | 40 |
| 1-10 | fully modded gear | Slow Cooking (full line) | 99.9 | 96% | 9.2 | +0.3 | 6.0 | 40 |
| 1-10 | fully modded gear | Flash Cooking (full line) | 99.9 | 96% | 9.2 | +0.3 | 6.0 | 40 |
| 11-20 | no gear | Unperked | 89.8 | 76% | 13.0 | - | 5.7 | 11 |
| 11-20 | no gear | Slow Cooking (full line) | 93.7 | 86% | 12.6 | -0.4 | 5.8 | 11 |
| 11-20 | no gear | Flash Cooking (full line) | 93.7 | 86% | 12.6 | -0.4 | 5.8 | 11 |
| 11-20 | mid gear | Unperked | 94.2 | 91% | 11.2 | - | 9.2 | 31 |
| 11-20 | mid gear | Slow Cooking (full line) | 97.4 | 91% | 11.0 | -0.2 | 10.3 | 31 |
| 11-20 | mid gear | Flash Cooking (full line) | 97.4 | 91% | 11.0 | -0.2 | 10.3 | 31 |
| 11-20 | best gear | Unperked | 95.9 | 89% | 10.7 | - | 12.0 | 48 |
| 11-20 | best gear | Slow Cooking (full line) | 97.1 | 91% | 10.4 | -0.3 | 11.4 | 48 |
| 11-20 | best gear | Flash Cooking (full line) | 97.1 | 91% | 10.4 | -0.3 | 11.4 | 48 |
| 11-20 | fully modded gear | Unperked | 98.6 | 96% | 8.7 | - | 8.6 | 48 |
| 11-20 | fully modded gear | Slow Cooking (full line) | 98.1 | 96% | 8.5 | -0.2 | 8.0 | 48 |
| 11-20 | fully modded gear | Flash Cooking (full line) | 98.1 | 96% | 8.5 | -0.2 | 8.0 | 48 |
| 21-30 | no gear | Unperked | 89.5 | 78% | 14.0 | - | 15.8 | 42 |
| 21-30 | no gear | Slow Cooking (full line) | 91.9 | 81% | 14.0 | +0.0 | 17.4 | 42 |
| 21-30 | no gear | Flash Cooking (full line) | 92.4 | 79% | 13.9 | -0.1 | 18.9 | 42 |
| 21-30 | mid gear | Unperked | 91.6 | 78% | 13.3 | - | 17.8 | 62 |
| 21-30 | mid gear | Slow Cooking (full line) | 93.8 | 86% | 13.3 | 0.0 | 23.9 | 62 |
| 21-30 | mid gear | Flash Cooking (full line) | 92.2 | 84% | 12.7 | -0.6 | 21.3 | 62 |
| 21-30 | best gear | Unperked | 96.7 | 90% | 12.6 | - | 16.1 | 79 |
| 21-30 | best gear | Slow Cooking (full line) | 97.2 | 88% | 12.2 | -0.4 | 20.1 | 79 |
| 21-30 | best gear | Flash Cooking (full line) | 97.6 | 91% | 11.1 | -1.5 | 18.1 | 79 |
| 21-30 | fully modded gear | Unperked | 98.3 | 92% | 9.2 | - | 8.6 | 79 |
| 21-30 | fully modded gear | Slow Cooking (full line) | 98.7 | 92% | 9.2 | -0.0 | 11.7 | 79 |
| 21-30 | fully modded gear | Flash Cooking (full line) | 100.0 | 99% | 8.5 | -0.7 | 10.9 | 79 |
| 31-40 | no gear | Unperked | 87.8 | 66% | 15.9 | - | 26.7 | 57 |
| 31-40 | no gear | Slow Cooking (full line) | 91.1 | 71% | 15.1 | -0.8 | 33.1 | 57 |
| 31-40 | no gear | Flash Cooking (full line) | 92.5 | 78% | 13.5 | -2.4 | 29.5 | 57 |
| 31-40 | mid gear | Unperked | 91.1 | 81% | 14.6 | - | 22.5 | 77 |
| 31-40 | mid gear | Slow Cooking (full line) | 93.1 | 80% | 13.7 | -0.8 | 31.1 | 77 |
| 31-40 | mid gear | Flash Cooking (full line) | 95.8 | 86% | 12.7 | -1.9 | 25.6 | 77 |
| 31-40 | best gear | Unperked | 92.5 | 78% | 13.6 | - | 24.5 | 94 |
| 31-40 | best gear | Slow Cooking (full line) | 95.7 | 84% | 12.5 | -1.2 | 26.1 | 94 |
| 31-40 | best gear | Flash Cooking (full line) | 97.3 | 88% | 11.4 | -2.2 | 24.5 | 94 |
| 31-40 | fully modded gear | Unperked | 98.7 | 86% | 11.4 | - | 14.9 | 94 |
| 31-40 | fully modded gear | Slow Cooking (full line) | 99.5 | 94% | 10.3 | -1.1 | 21.3 | 94 |
| 31-40 | fully modded gear | Flash Cooking (full line) | 99.7 | 91% | 10.1 | -1.4 | 18.1 | 94 |
| 41-53 | no gear | Unperked | 73.4 | 56% | 20.3 | - | 37.3 | 66 |
| 41-53 | no gear | Slow Cooking (full line) | 88.1 | 69% | 18.8 | -1.5 | 42.0 | 66 |
| 41-53 | no gear | Flash Cooking (full line) | 87.1 | 74% | 17.8 | -2.6 | 42.6 | 66 |
| 41-53 | mid gear | Unperked | 73.2 | 54% | 21.8 | - | 46.1 | 86 |
| 41-53 | mid gear | Slow Cooking (full line) | 94.3 | 86% | 17.9 | -3.9 | 45.6 | 86 |
| 41-53 | mid gear | Flash Cooking (full line) | 93.4 | 78% | 17.7 | -4.1 | 48.4 | 86 |
| 41-53 | best gear | Unperked | 82.4 | 69% | 19.7 | - | 37.1 | 102 |
| 41-53 | best gear | Slow Cooking (full line) | 96.3 | 86% | 15.8 | -3.8 | 36.9 | 102 |
| 41-53 | best gear | Flash Cooking (full line) | 95.9 | 86% | 15.8 | -3.9 | 44.4 | 102 |
| 41-53 | fully modded gear | Unperked | 96.7 | 85% | 15.2 | - | 21.7 | 102 |
| 41-53 | fully modded gear | Slow Cooking (full line) | 99.3 | 89% | 13.0 | -2.2 | 32.0 | 102 |
| 41-53 | fully modded gear | Flash Cooking (full line) | 99.1 | 85% | 12.7 | -2.5 | 34.7 | 102 |

## The styles' magnitudes

Each candidate gives Slow Cooking a Low heat Simmer bonus and a Medium heat drift reduction, and Flash Cooking a High heat drift reduction, alone and with the rest of the line the rank allows (Seasoned Palate off), at-level crafts of levels 20 to 53 (the rank equal to the level), the style-balance gate's recipes. The "better" columns and the verdict are gate 2's rule (a tie within 0.5 point gives each style half a recipe; each style must lead on at least 30% and the mean gap must not pass 5 points). "Actions" is the change against the unperked cook at the same gear; a negative change is a shorter craft.

| Candidate | With | Gear | Slow Cooking quality | Slow Cooking actions | Flash Cooking quality | Flash Cooking actions | Slow better | Flash better | Mean quality gap (Slow - Flash) | Gate 2 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Slow +60% Low / -25% Medium drift, Flash -15% High drift | alone | no gear | 85.4 | -0.6 | 86.6 | -0.3 | 46% | 54% | -1.2 | Passes |
| Slow +60% Low / -25% Medium drift, Flash -15% High drift | full line | no gear | 90.4 | -0.8 | 90.7 | -1.7 | 54% | 46% | -0.3 | Passes |
| Slow +60% Low / -25% Medium drift, Flash -15% High drift | alone | mid gear | 89.5 | -0.4 | 90.2 | -0.9 | 46% | 54% | -0.7 | Passes |
| Slow +60% Low / -25% Medium drift, Flash -15% High drift | full line | mid gear | 93.7 | -1.6 | 93.8 | -2.2 | 50% | 50% | -0.0 | Passes |
| Slow +60% Low / -25% Medium drift, Flash -15% High drift | alone | best gear | 95.1 | -1.0 | 94.2 | -0.9 | 58% | 42% | +1.0 | Passes |
| Slow +60% Low / -25% Medium drift, Flash -15% High drift | full line | best gear | 96.4 | -1.8 | 96.9 | -2.5 | 33% | 67% | -0.5 | Passes |
| Slow +60% Low / -25% Medium drift, Flash -15% High drift | alone | fully modded gear | 98.0 | -0.4 | 97.4 | -0.3 | 54% | 46% | +0.7 | Passes |
| Slow +60% Low / -25% Medium drift, Flash -15% High drift | full line | fully modded gear | 99.2 | -1.1 | 99.6 | -1.5 | 50% | 50% | -0.4 | Passes |

## Limitations

- Every figure is the adaptive player's, a bounded-depth search over the previews that does not plan the whole dish; a better player would use fewer actions with any build.
- Rank 50 plays recipes of levels 41-53 at rank 50 with every perk the rank allows. The through-the-ranks table plays one recipe level at the rank equal to it.
- Quality is the quality meter's share at the end, a ruined dish counting 0.
- Seasoned Palate is a choice the cook makes for each dish; the study plays it on for every dish or off for every dish, never choosing per dish.
- No client run; seconds per action are the simulator's estimate.
