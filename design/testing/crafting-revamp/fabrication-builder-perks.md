# Crafting revamp: the Fabrication Builder line

Command: `SWLOR.CraftSim builder-perks --runs 50 --seed 1 --recipes-per-band 6 --out design/testing/crafting-revamp/fabrication-builder-perks.md`

## Inputs

- Seed 1; 50 attempts per recipe, gear tier and build; up to 6 recipes sampled from each at-level band (`GateThresholds.RecipesPerBand`).
- Gear: no gear (Control 0, Craftsmanship 0, CP 0); mid gear (Control 13, Craftsmanship 12, CP 26); best gear (Control 29, Craftsmanship 30, CP 37).
- The adaptive player (`AdaptivePolicy`, depth 3, `FabricatorLeafHeuristic`) plays every build on the same dice, so a difference between two builds is theirs and not the luck of two samples.
- Every build takes its numbers from the perk definitions (`FabricationBuilderPerkDefinition`) at the rank it is played at; a perk with two ranks gives the highest rank that rank allows.
- CP spent is what the craft used of the CP available (gear plus 0.75 per rank, plus 31 from rank 25).

## Rank 50: each purchase alone, and the whole line

A rank-50 crafter works recipes of levels 41-53 (6 played, of 96). Each row owns the one perk named (at its highest rank), or the whole line with that style. "Actions" is the mean actions per craft; the change is against the unperked crafter at the same gear. A negative change is a shorter craft.

| Build | Gear | Mean quality | Full quality | Items made | Mean actions | Actions change | CP spent | CP available |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Unperked | no gear | 98.1 | 52% | 100% | 17.5 | - | 31.6 | 68 |
| Quick Change | no gear | 98.2 | 53% | 100% | 17.6 | +0.1 | 18.6 | 68 |
| Hardened Heads | no gear | 99.3 | 79% | 100% | 18.1 | +0.6 | 30.7 | 68 |
| Field Service | no gear | 98.1 | 52% | 100% | 17.6 | +0.0 | 30.7 | 68 |
| Production Run | no gear | 99.2 | 77% | 100% | 16.4 | -1.2 | 31.1 | 68 |
| Fine Tolerance | no gear | 99.3 | 90% | 100% | 16.6 | -1.0 | 25.7 | 68 |
| Dual Feed | no gear | 98.7 | 68% | 100% | 16.4 | -1.2 | 50.1 | 68 |
| Feedstock Reserve | no gear | 98.3 | 54% | 100% | 17.6 | +0.1 | 32.1 | 68 |
| Feedstock Contract | no gear | 98.2 | 53% | 100% | 17.6 | +0.1 | 32.8 | 68 |
| Master Assembler | no gear | 98.1 | 52% | 100% | 17.5 | +0.0 | 31.6 | 68 |
| Production Run (full line) | no gear | 100.0 | 100% | 100% | 16.2 | -1.3 | 25.1 | 68 |
| Fine Tolerance (full line) | no gear | 100.0 | 100% | 100% | 13.5 | -4.0 | 22.7 | 68 |
| Unperked | mid gear | 99.9 | 98% | 100% | 15.0 | - | 20.1 | 94 |
| Quick Change | mid gear | 99.9 | 98% | 100% | 15.0 | 0.0 | 10.5 | 94 |
| Hardened Heads | mid gear | 100.0 | 100% | 100% | 15.0 | +0.0 | 20.3 | 94 |
| Field Service | mid gear | 99.9 | 98% | 100% | 15.0 | 0.0 | 20.1 | 94 |
| Production Run | mid gear | 100.0 | 100% | 100% | 12.9 | -2.1 | 19.5 | 94 |
| Fine Tolerance | mid gear | 99.8 | 77% | 100% | 15.2 | +0.2 | 19.7 | 94 |
| Dual Feed | mid gear | 100.0 | 100% | 100% | 13.2 | -1.8 | 39.1 | 94 |
| Feedstock Reserve | mid gear | 100.0 | 99% | 100% | 15.0 | +0.0 | 19.9 | 94 |
| Feedstock Contract | mid gear | 99.9 | 97% | 100% | 15.1 | +0.1 | 21.3 | 94 |
| Master Assembler | mid gear | 99.9 | 98% | 100% | 15.0 | 0.0 | 20.1 | 94 |
| Production Run (full line) | mid gear | 100.0 | 100% | 100% | 12.6 | -2.4 | 16.3 | 94 |
| Fine Tolerance (full line) | mid gear | 100.0 | 100% | 100% | 13.3 | -1.7 | 22.4 | 94 |
| Unperked | best gear | 100.0 | 99% | 100% | 14.8 | - | 19.9 | 105 |
| Quick Change | best gear | 100.0 | 99% | 100% | 14.8 | 0.0 | 10.4 | 105 |
| Hardened Heads | best gear | 100.0 | 100% | 100% | 14.8 | +0.0 | 20.0 | 105 |
| Field Service | best gear | 100.0 | 99% | 100% | 14.8 | 0.0 | 19.9 | 105 |
| Production Run | best gear | 100.0 | 100% | 100% | 12.7 | -2.1 | 20.1 | 105 |
| Fine Tolerance | best gear | 100.0 | 100% | 100% | 14.4 | -0.4 | 19.4 | 105 |
| Dual Feed | best gear | 100.0 | 100% | 100% | 13.1 | -1.6 | 36.9 | 105 |
| Feedstock Reserve | best gear | 100.0 | 99% | 100% | 14.8 | +0.0 | 19.6 | 105 |
| Feedstock Contract | best gear | 100.0 | 100% | 100% | 13.8 | -1.0 | 17.6 | 105 |
| Master Assembler | best gear | 100.0 | 99% | 100% | 14.8 | +0.0 | 20.2 | 105 |
| Production Run (full line) | best gear | 100.0 | 100% | 100% | 11.1 | -3.7 | 14.7 | 105 |
| Fine Tolerance (full line) | best gear | 100.0 | 100% | 100% | 12.7 | -2.1 | 21.2 | 105 |

### The two styles: which is better, on quality and on actions

Each recipe is a tie within 0.5 quality point or 0.25 action, and a tie gives each style half a recipe. The style-balance gate (gate 2) reads the quality columns only.

| Pair | Gear | Recipes | First better quality | Second better quality | Mean quality gap | First fewer actions | Second fewer actions | Mean actions gap (first - second) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Production Run vs Fine Tolerance | no gear | 6 | 50% | 50% | -0.1 | 50% | 50% | -0.19 |
| Production Run vs Fine Tolerance | mid gear | 6 | 50% | 50% | +0.2 | 100% | 0% | -2.26 |
| Production Run vs Fine Tolerance | best gear | 6 | 50% | 50% | 0.0 | 100% | 0% | -1.67 |
| Production Run (full line) vs Fine Tolerance (full line) | no gear | 6 | 50% | 50% | 0.0 | 0% | 100% | +2.67 |
| Production Run (full line) vs Fine Tolerance (full line) | mid gear | 6 | 50% | 50% | 0.0 | 92% | 8% | -0.74 |
| Production Run (full line) vs Fine Tolerance (full line) | best gear | 6 | 50% | 50% | 0.0 | 100% | 0% | -1.62 |

## The whole line through the ranks (at-level crafts)

Every recipe is crafted at its own level's rank (the plan's item-craft cohort), so a band's crafter owns the purchases that rank allows: from rank 5 up, a style from rank 20, Dual Feed from rank 25, the capstone at 50.

| Band | Gear | Build | Mean quality | Full quality | Mean actions | Actions change | CP spent | CP available |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | Unperked | 81.5 | 15% | 15.8 | - | 4.0 | 4 |
| 1-10 | no gear | Production Run (full line) | 81.5 | 15% | 15.8 | 0.0 | 4.0 | 4 |
| 1-10 | no gear | Fine Tolerance (full line) | 81.5 | 15% | 15.8 | 0.0 | 4.0 | 4 |
| 1-10 | mid gear | Unperked | 100.0 | 100% | 15.4 | - | 4.0 | 30 |
| 1-10 | mid gear | Production Run (full line) | 100.0 | 100% | 15.4 | 0.0 | 4.0 | 30 |
| 1-10 | mid gear | Fine Tolerance (full line) | 100.0 | 100% | 15.4 | 0.0 | 4.0 | 30 |
| 1-10 | best gear | Unperked | 100.0 | 100% | 15.0 | - | 4.2 | 41 |
| 1-10 | best gear | Production Run (full line) | 100.0 | 100% | 15.0 | 0.0 | 4.2 | 41 |
| 1-10 | best gear | Fine Tolerance (full line) | 100.0 | 100% | 15.0 | 0.0 | 4.2 | 41 |
| 11-20 | no gear | Unperked | 98.3 | 46% | 17.6 | - | 9.8 | 11 |
| 11-20 | no gear | Production Run (full line) | 99.8 | 92% | 18.7 | +1.1 | 8.5 | 11 |
| 11-20 | no gear | Fine Tolerance (full line) | 99.8 | 92% | 18.5 | +0.9 | 7.9 | 11 |
| 11-20 | mid gear | Unperked | 100.0 | 100% | 15.6 | - | 7.6 | 37 |
| 11-20 | mid gear | Production Run (full line) | 100.0 | 100% | 15.2 | -0.4 | 5.5 | 37 |
| 11-20 | mid gear | Fine Tolerance (full line) | 100.0 | 97% | 15.8 | +0.2 | 5.6 | 37 |
| 11-20 | best gear | Unperked | 100.0 | 100% | 14.6 | - | 8.1 | 48 |
| 11-20 | best gear | Production Run (full line) | 100.0 | 100% | 14.2 | -0.4 | 5.9 | 48 |
| 11-20 | best gear | Fine Tolerance (full line) | 100.0 | 100% | 14.5 | -0.1 | 5.8 | 48 |
| 21-30 | no gear | Unperked | 99.8 | 93% | 18.9 | - | 19.0 | 45 |
| 21-30 | no gear | Production Run (full line) | 100.0 | 100% | 16.9 | -2.1 | 22.3 | 45 |
| 21-30 | no gear | Fine Tolerance (full line) | 100.0 | 100% | 15.5 | -3.5 | 22.1 | 45 |
| 21-30 | mid gear | Unperked | 100.0 | 100% | 15.4 | - | 11.6 | 71 |
| 21-30 | mid gear | Production Run (full line) | 100.0 | 100% | 12.7 | -2.6 | 14.9 | 71 |
| 21-30 | mid gear | Fine Tolerance (full line) | 99.8 | 88% | 14.5 | -0.9 | 21.2 | 71 |
| 21-30 | best gear | Unperked | 100.0 | 100% | 14.6 | - | 11.8 | 82 |
| 21-30 | best gear | Production Run (full line) | 100.0 | 100% | 12.5 | -2.2 | 12.8 | 82 |
| 21-30 | best gear | Fine Tolerance (full line) | 100.0 | 100% | 13.8 | -0.8 | 19.2 | 82 |
| 31-40 | no gear | Unperked | 99.6 | 89% | 19.2 | - | 28.0 | 57 |
| 31-40 | no gear | Production Run (full line) | 100.0 | 100% | 16.4 | -2.8 | 25.4 | 57 |
| 31-40 | no gear | Fine Tolerance (full line) | 100.0 | 100% | 14.8 | -4.4 | 25.9 | 57 |
| 31-40 | mid gear | Unperked | 100.0 | 100% | 15.1 | - | 14.9 | 83 |
| 31-40 | mid gear | Production Run (full line) | 100.0 | 100% | 13.0 | -2.2 | 15.1 | 83 |
| 31-40 | mid gear | Fine Tolerance (full line) | 99.9 | 89% | 14.4 | -0.7 | 25.3 | 83 |
| 31-40 | best gear | Unperked | 100.0 | 100% | 14.9 | - | 15.8 | 94 |
| 31-40 | best gear | Production Run (full line) | 100.0 | 100% | 12.7 | -2.2 | 16.0 | 94 |
| 31-40 | best gear | Fine Tolerance (full line) | 100.0 | 100% | 13.5 | -1.4 | 23.5 | 94 |
| 41-53 | no gear | Unperked | 98.1 | 50% | 17.8 | - | 32.4 | 65 |
| 41-53 | no gear | Production Run (full line) | 100.0 | 100% | 16.5 | -1.2 | 25.7 | 65 |
| 41-53 | no gear | Fine Tolerance (full line) | 100.0 | 100% | 14.6 | -3.2 | 25.1 | 65 |
| 41-53 | mid gear | Unperked | 99.9 | 98% | 15.6 | - | 20.5 | 91 |
| 41-53 | mid gear | Production Run (full line) | 100.0 | 100% | 12.7 | -2.8 | 16.8 | 91 |
| 41-53 | mid gear | Fine Tolerance (full line) | 100.0 | 97% | 13.8 | -1.7 | 23.8 | 91 |
| 41-53 | best gear | Unperked | 100.0 | 99% | 14.9 | - | 20.2 | 102 |
| 41-53 | best gear | Production Run (full line) | 100.0 | 100% | 11.5 | -3.4 | 14.9 | 102 |
| 41-53 | best gear | Fine Tolerance (full line) | 100.0 | 100% | 13.0 | -1.9 | 22.2 | 102 |

## The styles' magnitudes

Each candidate gives Production Run a Run bonus and a Precision Pass penalty, and Fine Tolerance a Precision Pass bonus and a Run penalty (a mirror uses the same sizes both ways), the style alone, at-level crafts of levels 20 to 53 (the rank equal to the level), the style-balance gate's recipes. The "better" columns and the verdict are gate 2's rule (a tie within 0.5 point gives each style half a recipe; each style must lead on at least 30% and the mean gap must not pass 5 points). "Actions" is the change against the unperked crafter at the same gear; a negative change is a shorter craft. Gate 2 reads quality only, which every candidate keeps near the maximum, so the actions columns are where the styles differ.

| Candidate | Gear | Production Run quality | Production Run actions | Fine Tolerance quality | Fine Tolerance actions | Fine Tolerance - Production Run actions | Production Run better | Fine Tolerance better | Mean quality gap | Gate 2 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Production Run +60% / -3%, Fine Tolerance +25% / -25% | no gear | 99.3 | -1.1 | 99.7 | -1.5 | -0.4 | 37% | 63% | -0.5 | Passes |
| Production Run +60% / -4%, Fine Tolerance +25% / -25% | no gear | 99.3 | -1.3 | 99.7 | -1.5 | -0.2 | 37% | 63% | -0.4 | Passes |
| Production Run +60% / -5%, Fine Tolerance +25% / -25% | no gear | 99.3 | -1.3 | 99.7 | -1.5 | -0.2 | 37% | 63% | -0.4 | Passes |
| Production Run +60% / -10%, Fine Tolerance +25% / -25% | no gear | 99.5 | -1.5 | 99.7 | -1.5 | -0.0 | 42% | 58% | -0.2 | Passes |
| Production Run +80% / -3%, Fine Tolerance +25% / -25% | no gear | 99.6 | -1.6 | 99.7 | -1.5 | +0.1 | 45% | 55% | -0.2 | Passes |
| Production Run +40% / -3%, Fine Tolerance +25% / -25% | no gear | 98.6 | -1.0 | 99.7 | -1.5 | -0.5 | 26% | 74% | -1.1 | Fails |
| Production Run +100% / -25%, Fine Tolerance +25% / -25% | no gear | 99.8 | -2.0 | 99.7 | -1.5 | +0.5 | 53% | 47% | +0.1 | Passes |
| Mirror +25% / -25% (the plan's original numbers) | no gear | 99.7 | +0.3 | 99.7 | -1.5 | -1.8 | 45% | 55% | -0.1 | Passes |
| Production Run +60% / -3%, Fine Tolerance +25% / -25% | mid gear | 100.0 | -1.9 | 99.8 | +0.7 | +2.5 | 50% | 50% | +0.2 | Passes |
| Production Run +60% / -4%, Fine Tolerance +25% / -25% | mid gear | 100.0 | -1.9 | 99.8 | +0.7 | +2.6 | 50% | 50% | +0.2 | Passes |
| Production Run +60% / -5%, Fine Tolerance +25% / -25% | mid gear | 98.5 | -1.7 | 99.8 | +0.7 | +2.3 | 0% | 100% | -1.3 | Fails |
| Production Run +60% / -10%, Fine Tolerance +25% / -25% | mid gear | 99.3 | +1.2 | 99.8 | +0.7 | -0.6 | 24% | 76% | -0.4 | Fails |
| Production Run +80% / -3%, Fine Tolerance +25% / -25% | mid gear | 100.0 | -2.4 | 99.8 | +0.7 | +3.1 | 50% | 50% | +0.2 | Passes |
| Production Run +40% / -3%, Fine Tolerance +25% / -25% | mid gear | 99.9 | -1.2 | 99.8 | +0.7 | +1.8 | 50% | 50% | +0.2 | Passes |
| Production Run +100% / -25%, Fine Tolerance +25% / -25% | mid gear | 99.4 | +0.9 | 99.8 | +0.7 | -0.2 | 45% | 55% | -0.4 | Passes |
| Mirror +25% / -25% (the plan's original numbers) | mid gear | 99.3 | +2.7 | 99.8 | +0.7 | -2.0 | 34% | 66% | -0.5 | Passes |
| Production Run +60% / -3%, Fine Tolerance +25% / -25% | best gear | 100.0 | -1.9 | 100.0 | +0.1 | +2.0 | 50% | 50% | 0.0 | Passes |
| Production Run +60% / -4%, Fine Tolerance +25% / -25% | best gear | 100.0 | -1.9 | 100.0 | +0.1 | +2.0 | 50% | 50% | 0.0 | Passes |
| Production Run +60% / -5%, Fine Tolerance +25% / -25% | best gear | 100.0 | -1.9 | 100.0 | +0.1 | +2.0 | 50% | 50% | 0.0 | Passes |
| Production Run +60% / -10%, Fine Tolerance +25% / -25% | best gear | 100.0 | -1.9 | 100.0 | +0.1 | +2.1 | 50% | 50% | 0.0 | Passes |
| Production Run +80% / -3%, Fine Tolerance +25% / -25% | best gear | 100.0 | -2.0 | 100.0 | +0.1 | +2.1 | 50% | 50% | 0.0 | Passes |
| Production Run +40% / -3%, Fine Tolerance +25% / -25% | best gear | 100.0 | -1.3 | 100.0 | +0.1 | +1.5 | 50% | 50% | 0.0 | Passes |
| Production Run +100% / -25%, Fine Tolerance +25% / -25% | best gear | 99.9 | +1.6 | 100.0 | +0.1 | -1.5 | 45% | 55% | -0.1 | Passes |
| Mirror +25% / -25% (the plan's original numbers) | best gear | 99.5 | +2.5 | 100.0 | +0.1 | -2.4 | 37% | 63% | -0.5 | Passes |

## Limitations

- Every figure is the adaptive player's, a bounded-depth search over the previews that does not plan the whole craft; a better player would use fewer actions with any build.
- Rank 50 plays recipes of levels 41-53 at rank 50 with every perk the rank allows. The through-the-ranks table plays one recipe level at the rank equal to it.
- Quality is the quality meter's share at the end, a broken or abandoned piece counting 0. Most crafts at mid and best gear reach full quality, so a style's cost shows in actions and CP.
- No client run; seconds per action are the simulator's estimate.
