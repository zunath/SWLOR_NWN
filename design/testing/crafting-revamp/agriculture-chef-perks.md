# Crafting revamp: the Agriculture Chef line

Command: `SWLOR.CraftSim chef-perks --runs 20 --seed 1 --recipes-per-band 4 --repo . --out design/testing/crafting-revamp/agriculture-chef-perks.md`

## Inputs

- Seed 1; 20 attempts per recipe, gear tier and build; up to 4 recipes sampled from each at-level band (`GateThresholds.RecipesPerBand`).
- Gear: no gear (Control 0, Craftsmanship 0, CP 0); mid gear (Control 13, Craftsmanship 12, CP 20); best gear (Control 29, Craftsmanship 30, CP 37).
- The adaptive player (`AdaptivePolicy`, depth 3, `KitchenLeafHeuristic`) plays every build on the same dice, so a difference between two builds is theirs and not the luck of two samples.
- Every build takes its numbers from the perk definitions (`AgricultureChefPerkDefinition`) at the rank it is played at; a perk with two ranks gives the highest rank that rank allows. Seasoned Palate is a switch the cook makes for each dish: rows say whether it is on.
- CP spent is what the craft used of the CP available (gear plus 0.75 per rank, plus 31 from rank 25).

## Rank 50: each purchase alone, and the whole line

A rank-50 cook works recipes of levels 41-53 (4 played, of 76). Each row owns the one perk named (at its highest rank), or the whole line with that style. "Actions" is the mean actions per craft; the change is against the unperked cook at the same gear. A negative change is a shorter craft.

| Build | Gear | Mean quality | Quality change | Full quality | Items made | Mean actions | Actions change | CP spent | CP available |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Unperked | no gear | 77.1 | - | 54% | 95% | 17.7 | - | 34.6 | 68 |
| Pantry Staples | no gear | 77.4 | +0.3 | 59% | 91% | 17.9 | +0.1 | 33.8 | 68 |
| Sharp Palate | no gear | 75.4 | -1.7 | 50% | 96% | 17.5 | -0.2 | 34.4 | 68 |
| Seasoned Palate | no gear | 77.1 | 0.0 | 54% | 95% | 17.7 | 0.0 | 34.6 | 68 |
| Seasoned Palate (on) | no gear | 73.4 | -3.7 | 50% | 95% | 18.8 | +1.1 | 35.6 | 68 |
| Slow Cooking | no gear | 80.3 | +3.2 | 59% | 96% | 17.2 | -0.6 | 29.8 | 68 |
| Flash Cooking | no gear | 78.7 | +1.6 | 56% | 91% | 17.2 | -0.5 | 30.9 | 68 |
| Reduce | no gear | 82.1 | +5.0 | 64% | 96% | 15.8 | -2.0 | 43.2 | 68 |
| Aromatic Kitchen | no gear | 73.4 | -3.7 | 56% | 91% | 18.0 | +0.2 | 35.0 | 68 |
| Steady Wrist | no gear | 75.0 | -2.1 | 54% | 94% | 17.5 | -0.2 | 30.7 | 68 |
| Signature Dish | no gear | 81.0 | +3.9 | 68% | 92% | 16.3 | -1.4 | 28.7 | 68 |
| Slow Cooking (full line) | no gear | 94.4 | +17.3 | 86% | 99% | 13.8 | -3.9 | 28.1 | 68 |
| Flash Cooking (full line) | no gear | 96.3 | +19.2 | 85% | 100% | 13.3 | -4.5 | 26.7 | 68 |
| Slow Cooking (full line, Seasoned Palate on) | no gear | 94.1 | +17.0 | 90% | 96% | 13.0 | -4.8 | 24.9 | 68 |
| Flash Cooking (full line, Seasoned Palate on) | no gear | 96.1 | +19.0 | 82% | 100% | 13.4 | -4.4 | 27.1 | 68 |
| Unperked | mid gear | 84.8 | - | 72% | 92% | 16.8 | - | 31.0 | 88 |
| Pantry Staples | mid gear | 77.5 | -7.3 | 59% | 91% | 17.6 | +0.8 | 34.5 | 88 |
| Sharp Palate | mid gear | 82.4 | -2.4 | 69% | 94% | 17.1 | +0.3 | 30.1 | 88 |
| Seasoned Palate | mid gear | 84.8 | 0.0 | 72% | 92% | 16.8 | 0.0 | 31.0 | 88 |
| Seasoned Palate (on) | mid gear | 77.6 | -7.2 | 52% | 96% | 18.0 | +1.2 | 35.0 | 88 |
| Slow Cooking | mid gear | 87.2 | +2.4 | 70% | 99% | 17.1 | +0.3 | 25.4 | 88 |
| Flash Cooking | mid gear | 87.5 | +2.6 | 74% | 98% | 16.0 | -0.8 | 28.9 | 88 |
| Reduce | mid gear | 87.3 | +2.4 | 75% | 99% | 15.5 | -1.3 | 44.9 | 88 |
| Aromatic Kitchen | mid gear | 85.2 | +0.4 | 71% | 100% | 16.4 | -0.4 | 28.3 | 88 |
| Steady Wrist | mid gear | 83.5 | -1.3 | 69% | 98% | 16.2 | -0.6 | 26.9 | 88 |
| Signature Dish | mid gear | 87.2 | +2.3 | 74% | 99% | 13.7 | -3.1 | 23.6 | 88 |
| Slow Cooking (full line) | mid gear | 97.4 | +12.6 | 88% | 100% | 13.1 | -3.7 | 26.4 | 88 |
| Flash Cooking (full line) | mid gear | 97.2 | +12.4 | 82% | 100% | 13.4 | -3.4 | 25.9 | 88 |
| Slow Cooking (full line, Seasoned Palate on) | mid gear | 95.7 | +10.8 | 88% | 100% | 13.1 | -3.7 | 26.9 | 88 |
| Flash Cooking (full line, Seasoned Palate on) | mid gear | 97.7 | +12.8 | 88% | 100% | 13.3 | -3.5 | 27.7 | 88 |
| Unperked | best gear | 85.5 | - | 70% | 96% | 16.6 | - | 30.3 | 105 |
| Pantry Staples | best gear | 83.4 | -2.2 | 68% | 95% | 16.5 | -0.1 | 33.7 | 105 |
| Sharp Palate | best gear | 88.9 | +3.4 | 70% | 96% | 16.0 | -0.7 | 29.9 | 105 |
| Seasoned Palate | best gear | 85.5 | 0.0 | 70% | 96% | 16.6 | 0.0 | 30.3 | 105 |
| Seasoned Palate (on) | best gear | 72.0 | -13.6 | 56% | 92% | 18.8 | +2.1 | 39.2 | 105 |
| Slow Cooking | best gear | 89.9 | +4.3 | 75% | 99% | 15.6 | -1.0 | 23.2 | 105 |
| Flash Cooking | best gear | 89.7 | +4.1 | 78% | 98% | 15.4 | -1.3 | 27.7 | 105 |
| Reduce | best gear | 92.2 | +6.7 | 80% | 98% | 14.2 | -2.5 | 39.1 | 105 |
| Aromatic Kitchen | best gear | 84.2 | -1.3 | 71% | 95% | 16.0 | -0.7 | 29.3 | 105 |
| Steady Wrist | best gear | 88.3 | +2.8 | 75% | 99% | 15.7 | -0.9 | 24.2 | 105 |
| Signature Dish | best gear | 92.2 | +6.7 | 79% | 100% | 13.9 | -2.8 | 21.4 | 105 |
| Slow Cooking (full line) | best gear | 98.0 | +12.4 | 90% | 99% | 12.6 | -4.0 | 24.2 | 105 |
| Flash Cooking (full line) | best gear | 98.2 | +12.7 | 90% | 100% | 12.2 | -4.5 | 23.3 | 105 |
| Slow Cooking (full line, Seasoned Palate on) | best gear | 99.9 | +14.4 | 98% | 100% | 12.4 | -4.2 | 21.4 | 105 |
| Flash Cooking (full line, Seasoned Palate on) | best gear | 98.0 | +12.5 | 85% | 100% | 13.0 | -3.6 | 27.4 | 105 |

### The two styles: which is better, on quality and on actions

Each recipe is a tie within 0.5 quality point or 0.25 action, and a tie gives each style half a recipe. The style-balance gate (gate 2) reads the quality columns only.

| Pair | Gear | Recipes | First better quality | Second better quality | Mean quality gap | First fewer actions | Second fewer actions | Mean actions gap (first - second) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Slow Cooking vs Flash Cooking | no gear | 4 | 50% | 50% | +1.5 | 62% | 38% | -0.08 |
| Slow Cooking vs Flash Cooking | mid gear | 4 | 38% | 62% | -0.3 | 12% | 88% | +1.11 |
| Slow Cooking vs Flash Cooking | best gear | 4 | 62% | 38% | +0.2 | 38% | 62% | +0.24 |
| Slow Cooking (full line) vs Flash Cooking (full line) | no gear | 4 | 12% | 88% | -1.9 | 12% | 88% | +0.56 |
| Slow Cooking (full line) vs Flash Cooking (full line) | mid gear | 4 | 62% | 38% | +0.2 | 50% | 50% | -0.25 |
| Slow Cooking (full line) vs Flash Cooking (full line) | best gear | 4 | 62% | 38% | -0.2 | 25% | 75% | +0.46 |
| Slow Cooking (full line, Seasoned Palate on) vs Flash Cooking (full line, Seasoned Palate on) | no gear | 4 | 38% | 62% | -2.0 | 62% | 38% | -0.40 |
| Slow Cooking (full line, Seasoned Palate on) vs Flash Cooking (full line, Seasoned Palate on) | mid gear | 4 | 38% | 62% | -2.0 | 62% | 38% | -0.19 |
| Slow Cooking (full line, Seasoned Palate on) vs Flash Cooking (full line, Seasoned Palate on) | best gear | 4 | 88% | 12% | +1.9 | 62% | 38% | -0.61 |

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
| 11-20 | no gear | Unperked | 89.8 | 76% | 13.0 | - | 5.7 | 11 |
| 11-20 | no gear | Slow Cooking (full line) | 96.2 | 91% | 12.0 | -1.0 | 5.3 | 11 |
| 11-20 | no gear | Flash Cooking (full line) | 96.2 | 91% | 12.0 | -1.0 | 5.3 | 11 |
| 11-20 | mid gear | Unperked | 94.2 | 91% | 11.2 | - | 9.2 | 31 |
| 11-20 | mid gear | Slow Cooking (full line) | 97.7 | 92% | 10.9 | -0.2 | 9.7 | 31 |
| 11-20 | mid gear | Flash Cooking (full line) | 97.7 | 92% | 10.9 | -0.2 | 9.7 | 31 |
| 11-20 | best gear | Unperked | 95.9 | 89% | 10.7 | - | 12.0 | 48 |
| 11-20 | best gear | Slow Cooking (full line) | 97.4 | 94% | 9.7 | -1.0 | 9.0 | 48 |
| 11-20 | best gear | Flash Cooking (full line) | 97.4 | 94% | 9.7 | -1.0 | 9.0 | 48 |
| 21-30 | no gear | Unperked | 89.5 | 78% | 14.0 | - | 15.8 | 42 |
| 21-30 | no gear | Slow Cooking (full line) | 91.1 | 79% | 13.4 | -0.6 | 17.5 | 42 |
| 21-30 | no gear | Flash Cooking (full line) | 94.1 | 85% | 13.4 | -0.6 | 20.9 | 42 |
| 21-30 | mid gear | Unperked | 91.6 | 78% | 13.3 | - | 17.8 | 62 |
| 21-30 | mid gear | Slow Cooking (full line) | 96.3 | 89% | 12.7 | -0.6 | 21.7 | 62 |
| 21-30 | mid gear | Flash Cooking (full line) | 93.8 | 90% | 12.0 | -1.3 | 18.9 | 62 |
| 21-30 | best gear | Unperked | 96.7 | 90% | 12.6 | - | 16.1 | 79 |
| 21-30 | best gear | Slow Cooking (full line) | 96.8 | 94% | 11.8 | -0.7 | 17.9 | 79 |
| 21-30 | best gear | Flash Cooking (full line) | 96.5 | 92% | 11.0 | -1.6 | 17.7 | 79 |
| 31-40 | no gear | Unperked | 87.8 | 66% | 15.9 | - | 26.7 | 57 |
| 31-40 | no gear | Slow Cooking (full line) | 94.1 | 78% | 14.5 | -1.4 | 32.5 | 57 |
| 31-40 | no gear | Flash Cooking (full line) | 87.8 | 64% | 14.8 | -1.1 | 33.6 | 57 |
| 31-40 | mid gear | Unperked | 91.1 | 81% | 14.6 | - | 22.5 | 77 |
| 31-40 | mid gear | Slow Cooking (full line) | 94.5 | 86% | 13.2 | -1.4 | 29.6 | 77 |
| 31-40 | mid gear | Flash Cooking (full line) | 93.4 | 86% | 13.1 | -1.4 | 27.2 | 77 |
| 31-40 | best gear | Unperked | 92.5 | 78% | 13.6 | - | 24.5 | 94 |
| 31-40 | best gear | Slow Cooking (full line) | 97.1 | 92% | 12.0 | -1.6 | 27.6 | 94 |
| 31-40 | best gear | Flash Cooking (full line) | 99.2 | 91% | 10.8 | -2.8 | 22.6 | 94 |
| 41-53 | no gear | Unperked | 69.1 | 52% | 18.6 | - | 37.0 | 65 |
| 41-53 | no gear | Slow Cooking (full line) | 92.2 | 76% | 15.3 | -3.3 | 36.0 | 65 |
| 41-53 | no gear | Flash Cooking (full line) | 91.1 | 78% | 15.4 | -3.3 | 36.9 | 65 |
| 41-53 | mid gear | Unperked | 79.8 | 64% | 17.8 | - | 29.8 | 85 |
| 41-53 | mid gear | Slow Cooking (full line) | 96.0 | 88% | 14.5 | -3.3 | 35.4 | 85 |
| 41-53 | mid gear | Flash Cooking (full line) | 95.3 | 78% | 14.7 | -3.1 | 37.6 | 85 |
| 41-53 | best gear | Unperked | 80.8 | 62% | 17.3 | - | 30.9 | 102 |
| 41-53 | best gear | Slow Cooking (full line) | 94.6 | 88% | 13.7 | -3.5 | 32.2 | 102 |
| 41-53 | best gear | Flash Cooking (full line) | 97.9 | 92% | 13.3 | -3.9 | 33.4 | 102 |

## The styles' magnitudes

Each candidate gives Slow Cooking a Low heat Simmer bonus and a Medium heat drift reduction, and Flash Cooking a High heat drift reduction, alone and with the rest of the line the rank allows (Seasoned Palate off), at-level crafts of levels 20 to 53 (the rank equal to the level), the style-balance gate's recipes. The "better" columns and the verdict are gate 2's rule (a tie within 0.5 point gives each style half a recipe; each style must lead on at least 30% and the mean gap must not pass 5 points). "Actions" is the change against the unperked cook at the same gear; a negative change is a shorter craft.

| Candidate | With | Gear | Slow Cooking quality | Slow Cooking actions | Flash Cooking quality | Flash Cooking actions | Slow better | Flash better | Mean quality gap (Slow - Flash) | Gate 2 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Slow +80% Low / -25% Medium drift, Flash -15% High drift | alone | no gear | 85.2 | -0.4 | 85.7 | -0.7 | 58% | 42% | -0.5 | Passes |
| Slow +80% Low / -25% Medium drift, Flash -15% High drift | full line | no gear | 92.4 | -1.7 | 91.0 | -1.6 | 71% | 29% | +1.5 | Fails |
| Slow +80% Low / -25% Medium drift, Flash -15% High drift | alone | mid gear | 89.2 | -0.4 | 89.9 | -0.5 | 58% | 42% | -0.6 | Passes |
| Slow +80% Low / -25% Medium drift, Flash -15% High drift | full line | mid gear | 95.6 | -1.8 | 94.2 | -1.9 | 58% | 42% | +1.4 | Passes |
| Slow +80% Low / -25% Medium drift, Flash -15% High drift | alone | best gear | 94.8 | -0.8 | 92.4 | -0.7 | 58% | 42% | +2.4 | Passes |
| Slow +80% Low / -25% Medium drift, Flash -15% High drift | full line | best gear | 96.1 | -2.0 | 97.8 | -2.8 | 42% | 58% | -1.7 | Passes |

## Limitations

- Every figure is the adaptive player's, a bounded-depth search over the previews that does not plan the whole dish; a better player would use fewer actions with any build.
- Rank 50 plays recipes of levels 41-53 at rank 50 with every perk the rank allows. The through-the-ranks table plays one recipe level at the rank equal to it.
- Quality is the quality meter's share at the end, a ruined dish counting 0.
- Seasoned Palate is a choice the cook makes for each dish; the study plays it on for every dish or off for every dish, never choosing per dish.
- No client run; seconds per action are the simulator's estimate.
