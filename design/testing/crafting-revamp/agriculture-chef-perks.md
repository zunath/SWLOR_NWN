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
| Unperked | mid gear | 82.6 | - | 61% | 95% | 19.9 | - | 37.3 | 88 |
| Pantry Staples | mid gear | 77.0 | -5.6 | 59% | 92% | 20.2 | +0.3 | 38.5 | 88 |
| Sharp Palate | mid gear | 85.9 | +3.3 | 65% | 98% | 19.6 | -0.3 | 36.8 | 88 |
| Seasoned Palate | mid gear | 82.6 | 0.0 | 61% | 95% | 19.9 | 0.0 | 37.3 | 88 |
| Seasoned Palate (on) | mid gear | 80.7 | -1.9 | 68% | 90% | 20.8 | +0.9 | 37.9 | 88 |
| Slow Cooking | mid gear | 87.6 | +5.1 | 71% | 98% | 18.4 | -1.5 | 25.9 | 88 |
| Flash Cooking | mid gear | 88.5 | +6.0 | 68% | 98% | 18.3 | -1.6 | 30.7 | 88 |
| Reduce | mid gear | 89.8 | +7.3 | 72% | 98% | 18.2 | -1.6 | 57.1 | 88 |
| Aromatic Kitchen | mid gear | 82.4 | -0.2 | 65% | 96% | 20.6 | +0.7 | 39.0 | 88 |
| Steady Wrist | mid gear | 87.8 | +5.2 | 74% | 99% | 19.1 | -0.8 | 34.4 | 88 |
| Signature Dish | mid gear | 88.4 | +5.8 | 72% | 98% | 17.6 | -2.3 | 26.9 | 88 |
| Slow Cooking (full line) | mid gear | 93.8 | +11.2 | 80% | 99% | 16.5 | -3.4 | 37.5 | 88 |
| Flash Cooking (full line) | mid gear | 93.1 | +10.5 | 75% | 100% | 16.7 | -3.2 | 40.9 | 88 |
| Slow Cooking (full line, Seasoned Palate on) | mid gear | 92.4 | +9.8 | 78% | 99% | 16.3 | -3.6 | 38.4 | 88 |
| Flash Cooking (full line, Seasoned Palate on) | mid gear | 94.5 | +11.9 | 84% | 99% | 16.2 | -3.7 | 40.2 | 88 |
| Unperked | best gear | 94.2 | - | 80% | 100% | 17.8 | - | 31.8 | 105 |
| Pantry Staples | best gear | 92.4 | -1.9 | 76% | 100% | 18.1 | +0.3 | 32.2 | 105 |
| Sharp Palate | best gear | 94.9 | +0.7 | 80% | 100% | 18.0 | +0.2 | 33.1 | 105 |
| Seasoned Palate | best gear | 94.2 | 0.0 | 80% | 100% | 17.8 | 0.0 | 31.8 | 105 |
| Seasoned Palate (on) | best gear | 88.1 | -6.1 | 65% | 99% | 18.7 | +0.9 | 33.9 | 105 |
| Slow Cooking | best gear | 91.1 | -3.1 | 76% | 100% | 16.6 | -1.2 | 26.1 | 105 |
| Flash Cooking | best gear | 92.9 | -1.4 | 84% | 100% | 17.0 | -0.8 | 29.8 | 105 |
| Reduce | best gear | 96.8 | +2.6 | 84% | 100% | 16.5 | -1.3 | 52.2 | 105 |
| Aromatic Kitchen | best gear | 92.8 | -1.5 | 80% | 100% | 19.1 | +1.2 | 34.8 | 105 |
| Steady Wrist | best gear | 95.3 | +1.1 | 82% | 99% | 17.3 | -0.5 | 29.2 | 105 |
| Signature Dish | best gear | 97.4 | +3.1 | 84% | 100% | 14.8 | -3.1 | 20.0 | 105 |
| Slow Cooking (full line) | best gear | 99.0 | +4.8 | 82% | 100% | 12.7 | -5.1 | 25.0 | 105 |
| Flash Cooking (full line) | best gear | 99.3 | +5.1 | 82% | 100% | 13.4 | -4.4 | 32.9 | 105 |
| Slow Cooking (full line, Seasoned Palate on) | best gear | 97.5 | +3.2 | 89% | 100% | 14.1 | -3.8 | 32.1 | 105 |
| Flash Cooking (full line, Seasoned Palate on) | best gear | 97.8 | +3.6 | 89% | 100% | 13.9 | -3.9 | 34.4 | 105 |
| Unperked | fully modded gear | 98.1 | - | 91% | 100% | 12.6 | - | 14.2 | 105 |
| Pantry Staples | fully modded gear | 97.9 | -0.1 | 88% | 100% | 13.4 | +0.8 | 16.9 | 105 |
| Sharp Palate | fully modded gear | 97.8 | -0.3 | 86% | 100% | 12.9 | +0.2 | 14.2 | 105 |
| Seasoned Palate | fully modded gear | 98.1 | 0.0 | 91% | 100% | 12.6 | 0.0 | 14.2 | 105 |
| Seasoned Palate (on) | fully modded gear | 95.0 | -3.1 | 81% | 100% | 16.6 | +4.0 | 22.4 | 105 |
| Slow Cooking | fully modded gear | 99.7 | +1.7 | 91% | 100% | 12.1 | -0.6 | 11.3 | 105 |
| Flash Cooking | fully modded gear | 96.1 | -2.0 | 89% | 99% | 12.7 | +0.1 | 14.6 | 105 |
| Reduce | fully modded gear | 97.5 | -0.6 | 85% | 100% | 13.6 | +0.9 | 35.8 | 105 |
| Aromatic Kitchen | fully modded gear | 96.8 | -1.2 | 88% | 100% | 13.4 | +0.8 | 16.4 | 105 |
| Steady Wrist | fully modded gear | 97.7 | -0.4 | 92% | 100% | 12.4 | -0.2 | 14.5 | 105 |
| Signature Dish | fully modded gear | 96.1 | -1.9 | 78% | 100% | 12.1 | -0.6 | 12.9 | 105 |
| Slow Cooking (full line) | fully modded gear | 99.8 | +1.7 | 94% | 100% | 10.1 | -2.5 | 18.2 | 105 |
| Flash Cooking (full line) | fully modded gear | 99.9 | +1.9 | 91% | 100% | 10.2 | -2.4 | 18.1 | 105 |
| Slow Cooking (full line, Seasoned Palate on) | fully modded gear | 98.7 | +0.6 | 89% | 100% | 10.6 | -2.0 | 21.8 | 105 |
| Flash Cooking (full line, Seasoned Palate on) | fully modded gear | 99.0 | +0.9 | 89% | 100% | 11.3 | -1.3 | 25.8 | 105 |

### The two styles: which is better, on quality and on actions

Each recipe is a tie within 0.5 quality point or 0.25 action, and a tie gives each style half a recipe. The style-balance gate (gate 2) reads the quality columns only.

| Pair | Gear | Recipes | First better quality | Second better quality | Mean quality gap | First fewer actions | Second fewer actions | Mean actions gap (first - second) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Slow Cooking vs Flash Cooking | no gear | 4 | 75% | 25% | +6.4 | 50% | 50% | +0.30 |
| Slow Cooking vs Flash Cooking | mid gear | 4 | 38% | 62% | -0.9 | 50% | 50% | +0.06 |
| Slow Cooking vs Flash Cooking | best gear | 4 | 25% | 75% | -1.7 | 62% | 38% | -0.41 |
| Slow Cooking vs Flash Cooking | fully modded gear | 4 | 75% | 25% | +3.7 | 75% | 25% | -0.67 |
| Slow Cooking (full line) vs Flash Cooking (full line) | no gear | 4 | 62% | 38% | +4.1 | 75% | 25% | -1.09 |
| Slow Cooking (full line) vs Flash Cooking (full line) | mid gear | 4 | 62% | 38% | +0.7 | 75% | 25% | -0.20 |
| Slow Cooking (full line) vs Flash Cooking (full line) | best gear | 4 | 50% | 50% | -0.3 | 75% | 25% | -0.66 |
| Slow Cooking (full line) vs Flash Cooking (full line) | fully modded gear | 4 | 38% | 62% | -0.1 | 62% | 38% | -0.13 |
| Slow Cooking (full line, Seasoned Palate on) vs Flash Cooking (full line, Seasoned Palate on) | no gear | 4 | 25% | 75% | -9.1 | 75% | 25% | -0.71 |
| Slow Cooking (full line, Seasoned Palate on) vs Flash Cooking (full line, Seasoned Palate on) | mid gear | 4 | 25% | 75% | -2.1 | 38% | 62% | +0.10 |
| Slow Cooking (full line, Seasoned Palate on) vs Flash Cooking (full line, Seasoned Palate on) | best gear | 4 | 38% | 62% | -0.3 | 25% | 75% | +0.20 |
| Slow Cooking (full line, Seasoned Palate on) vs Flash Cooking (full line, Seasoned Palate on) | fully modded gear | 4 | 50% | 50% | -0.3 | 75% | 25% | -0.71 |

## The whole line through the ranks (at-level crafts)

Every recipe is crafted at its own level's rank (the plan's item-craft cohort), so a band's cook owns the purchases that rank allows: from rank 5 up, a style from rank 20, Reduce from rank 25, the capstone at 50. Seasoned Palate is off.

| Band | Gear | Build | Mean quality | Full quality | Mean actions | Actions change | CP spent | CP available |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | Unperked | 93.7 | 78% | 13.0 | - | 1.2 | 4 |
| 1-10 | no gear | Slow Cooking (full line) | 94.9 | 76% | 13.0 | -0.1 | 1.1 | 4 |
| 1-10 | no gear | Flash Cooking (full line) | 94.9 | 76% | 13.0 | -0.1 | 1.1 | 4 |
| 1-10 | mid gear | Unperked | 98.9 | 91% | 11.6 | - | 8.2 | 24 |
| 1-10 | mid gear | Slow Cooking (full line) | 99.8 | 91% | 10.8 | -0.8 | 7.1 | 24 |
| 1-10 | mid gear | Flash Cooking (full line) | 99.8 | 91% | 10.8 | -0.8 | 7.1 | 24 |
| 1-10 | best gear | Unperked | 98.2 | 95% | 10.5 | - | 9.8 | 40 |
| 1-10 | best gear | Slow Cooking (full line) | 98.5 | 95% | 10.6 | +0.1 | 10.5 | 40 |
| 1-10 | best gear | Flash Cooking (full line) | 98.5 | 95% | 10.6 | +0.1 | 10.5 | 40 |
| 1-10 | fully modded gear | Unperked | 100.0 | 98% | 8.1 | - | 5.4 | 40 |
| 1-10 | fully modded gear | Slow Cooking (full line) | 99.9 | 94% | 8.3 | +0.2 | 5.2 | 40 |
| 1-10 | fully modded gear | Flash Cooking (full line) | 99.9 | 94% | 8.3 | +0.2 | 5.2 | 40 |
| 11-20 | no gear | Unperked | 89.8 | 76% | 13.0 | - | 5.7 | 11 |
| 11-20 | no gear | Slow Cooking (full line) | 93.7 | 86% | 12.6 | -0.4 | 5.8 | 11 |
| 11-20 | no gear | Flash Cooking (full line) | 93.7 | 86% | 12.6 | -0.4 | 5.8 | 11 |
| 11-20 | mid gear | Unperked | 93.8 | 91% | 10.4 | - | 9.4 | 31 |
| 11-20 | mid gear | Slow Cooking (full line) | 94.5 | 90% | 10.4 | 0.0 | 9.2 | 31 |
| 11-20 | mid gear | Flash Cooking (full line) | 94.5 | 90% | 10.4 | 0.0 | 9.2 | 31 |
| 11-20 | best gear | Unperked | 98.0 | 91% | 10.7 | - | 10.9 | 48 |
| 11-20 | best gear | Slow Cooking (full line) | 97.3 | 92% | 10.4 | -0.3 | 10.9 | 48 |
| 11-20 | best gear | Flash Cooking (full line) | 97.3 | 92% | 10.4 | -0.3 | 10.9 | 48 |
| 11-20 | fully modded gear | Unperked | 99.7 | 99% | 8.0 | - | 7.5 | 48 |
| 11-20 | fully modded gear | Slow Cooking (full line) | 100.0 | 100% | 7.3 | -0.6 | 6.0 | 48 |
| 11-20 | fully modded gear | Flash Cooking (full line) | 100.0 | 100% | 7.3 | -0.6 | 6.0 | 48 |
| 21-30 | no gear | Unperked | 89.5 | 78% | 14.0 | - | 15.8 | 42 |
| 21-30 | no gear | Slow Cooking (full line) | 91.9 | 81% | 14.0 | +0.0 | 17.4 | 42 |
| 21-30 | no gear | Flash Cooking (full line) | 92.4 | 79% | 13.9 | -0.1 | 18.9 | 42 |
| 21-30 | mid gear | Unperked | 95.8 | 85% | 12.0 | - | 14.1 | 62 |
| 21-30 | mid gear | Slow Cooking (full line) | 97.1 | 91% | 12.3 | +0.3 | 18.8 | 62 |
| 21-30 | mid gear | Flash Cooking (full line) | 94.7 | 84% | 12.1 | +0.1 | 19.8 | 62 |
| 21-30 | best gear | Unperked | 95.2 | 89% | 12.9 | - | 16.5 | 79 |
| 21-30 | best gear | Slow Cooking (full line) | 99.1 | 95% | 11.8 | -1.2 | 18.4 | 79 |
| 21-30 | best gear | Flash Cooking (full line) | 97.6 | 92% | 11.0 | -1.9 | 17.3 | 79 |
| 21-30 | fully modded gear | Unperked | 99.8 | 91% | 8.4 | - | 5.0 | 79 |
| 21-30 | fully modded gear | Slow Cooking (full line) | 99.3 | 95% | 8.6 | +0.2 | 10.8 | 79 |
| 21-30 | fully modded gear | Flash Cooking (full line) | 98.6 | 96% | 8.6 | +0.2 | 12.5 | 79 |
| 31-40 | no gear | Unperked | 87.8 | 66% | 15.9 | - | 26.7 | 57 |
| 31-40 | no gear | Slow Cooking (full line) | 91.1 | 71% | 15.1 | -0.8 | 33.1 | 57 |
| 31-40 | no gear | Flash Cooking (full line) | 92.5 | 78% | 13.5 | -2.4 | 29.5 | 57 |
| 31-40 | mid gear | Unperked | 92.3 | 82% | 14.7 | - | 24.0 | 77 |
| 31-40 | mid gear | Slow Cooking (full line) | 93.4 | 85% | 13.0 | -1.7 | 27.6 | 77 |
| 31-40 | mid gear | Flash Cooking (full line) | 96.5 | 90% | 12.2 | -2.6 | 25.7 | 77 |
| 31-40 | best gear | Unperked | 94.4 | 79% | 13.8 | - | 24.4 | 94 |
| 31-40 | best gear | Slow Cooking (full line) | 96.6 | 89% | 12.6 | -1.2 | 26.8 | 94 |
| 31-40 | best gear | Flash Cooking (full line) | 97.3 | 88% | 11.5 | -2.3 | 24.3 | 94 |
| 31-40 | fully modded gear | Unperked | 97.7 | 86% | 11.1 | - | 16.4 | 94 |
| 31-40 | fully modded gear | Slow Cooking (full line) | 98.6 | 91% | 9.7 | -1.4 | 19.6 | 94 |
| 31-40 | fully modded gear | Flash Cooking (full line) | 99.5 | 94% | 9.2 | -1.9 | 15.7 | 94 |
| 41-53 | no gear | Unperked | 73.4 | 56% | 20.3 | - | 37.3 | 66 |
| 41-53 | no gear | Slow Cooking (full line) | 88.1 | 69% | 18.8 | -1.5 | 42.0 | 66 |
| 41-53 | no gear | Flash Cooking (full line) | 87.1 | 74% | 17.8 | -2.6 | 42.6 | 66 |
| 41-53 | mid gear | Unperked | 81.2 | 62% | 20.8 | - | 44.0 | 86 |
| 41-53 | mid gear | Slow Cooking (full line) | 93.1 | 84% | 17.5 | -3.3 | 46.6 | 86 |
| 41-53 | mid gear | Flash Cooking (full line) | 93.8 | 75% | 17.2 | -3.6 | 48.7 | 86 |
| 41-53 | best gear | Unperked | 86.9 | 72% | 19.0 | - | 33.7 | 102 |
| 41-53 | best gear | Slow Cooking (full line) | 96.6 | 82% | 15.6 | -3.4 | 35.1 | 102 |
| 41-53 | best gear | Flash Cooking (full line) | 97.8 | 88% | 15.4 | -3.6 | 41.8 | 102 |
| 41-53 | fully modded gear | Unperked | 98.0 | 88% | 13.8 | - | 16.1 | 102 |
| 41-53 | fully modded gear | Slow Cooking (full line) | 98.8 | 94% | 11.9 | -1.9 | 30.6 | 102 |
| 41-53 | fully modded gear | Flash Cooking (full line) | 99.5 | 85% | 11.5 | -2.3 | 30.9 | 102 |

## The styles' magnitudes

Each candidate gives Slow Cooking a Low heat Simmer bonus and a Medium heat drift reduction, and Flash Cooking a High heat drift reduction, alone and with the rest of the line the rank allows (Seasoned Palate off), at-level crafts of levels 20 to 53 (the rank equal to the level), the style-balance gate's recipes. The "better" columns and the verdict are gate 2's rule (a tie within 0.5 point gives each style half a recipe; each style must lead on at least 30% and the mean gap must not pass 5 points). "Actions" is the change against the unperked cook at the same gear; a negative change is a shorter craft.

| Candidate | With | Gear | Slow Cooking quality | Slow Cooking actions | Flash Cooking quality | Flash Cooking actions | Slow better | Flash better | Mean quality gap (Slow - Flash) | Gate 2 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Slow +60% Low / -25% Medium drift, Flash -15% High drift | alone | no gear | 85.4 | -0.6 | 86.6 | -0.3 | 46% | 54% | -1.2 | Passes |
| Slow +60% Low / -25% Medium drift, Flash -15% High drift | full line | no gear | 90.4 | -0.8 | 90.7 | -1.7 | 54% | 46% | -0.3 | Passes |
| Slow +60% Low / -25% Medium drift, Flash -15% High drift | alone | mid gear | 89.7 | +0.1 | 91.5 | -1.2 | 29% | 71% | -1.8 | Fails |
| Slow +60% Low / -25% Medium drift, Flash -15% High drift | full line | mid gear | 94.5 | -1.6 | 95.0 | -2.0 | 50% | 50% | -0.5 | Passes |
| Slow +60% Low / -25% Medium drift, Flash -15% High drift | alone | best gear | 94.6 | -1.1 | 93.6 | -1.0 | 71% | 29% | +1.0 | Fails |
| Slow +60% Low / -25% Medium drift, Flash -15% High drift | full line | best gear | 97.4 | -1.9 | 97.5 | -2.6 | 50% | 50% | -0.1 | Passes |
| Slow +60% Low / -25% Medium drift, Flash -15% High drift | alone | fully modded gear | 99.4 | -0.7 | 98.0 | -0.1 | 58% | 42% | +1.4 | Passes |
| Slow +60% Low / -25% Medium drift, Flash -15% High drift | full line | fully modded gear | 98.9 | -1.0 | 99.2 | -1.3 | 46% | 54% | -0.3 | Passes |

## Limitations

- Every figure is the adaptive player's, a bounded-depth search over the previews that does not plan the whole dish; a better player would use fewer actions with any build.
- Rank 50 plays recipes of levels 41-53 at rank 50 with every perk the rank allows. The through-the-ranks table plays one recipe level at the rank equal to it.
- Quality is the quality meter's share at the end, a ruined dish counting 0.
- Seasoned Palate is a choice the cook makes for each dish; the study plays it on for every dish or off for every dish, never choosing per dish.
- No client run; seconds per action are the simulator's estimate.
