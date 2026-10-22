# Crafting revamp: legacy baseline of enhancement transfer

The old minigame (`CraftViewModel.cs`) played with one, two and three enhancements slotted, with today's rules: each enhancement raises the progress the
piece needs by its progress penalty, and on a success each enhancement property transfers at a chance equal to the quality percentage. It is the yardstick of
balance gate 7 (enhancement parity). No new minigame is measured here.

## Inputs

- Command: `SWLOR.CraftSim legacy-enhancements --runs 200 --seed 1 --recipes-per-band 6 --out design/testing/crafting-revamp/legacy-enhancement-baseline.md`
- Seed 1; 200 crafts per recipe level, skill, gear level, policy, enhancement penalty and slot count. Each craft's dice come from its own stream of the seed, so a run repeats exactly.
- Enhancement items: the 813 items in `Module/uti` that carry a progress penalty (item property 95) and one enhancement property for a recipe kind, with an enhancement level (104): 189 weapon, 262 armor, 12 structure, 144 food, 133 starship, 12 module, 61 droid. 5 item(s) with a penalty are left out: mtench_ink: has 2 enhancement properties, so it is not one enhancement; mtench_sac: has 2 enhancement properties, so it is not one enhancement; p_crystal_red_qs: has 3 enhancement properties, so it is not one enhancement; ssscan1: has a progress penalty but no enhancement property; ssscan2: has a progress penalty but no enhancement property.
- Representative enhancement: a recipe is played with the real item of its enhancement kind at the best level it accepts (the old window refuses an enhancement more than 5 levels above the recipe), taking the item whose progress penalty is the lower median of that level, ties by resref. The sweep plays every level the recipe accepts.
- Slots: an item with 1, 2 or 3 enhancements, every slot holding the same representative item. Recipes have 1 or 2 slots; research and blueprints add more, so every enhanceable recipe is played at each count.
- Recipes: an even sample of 6 active recipes per skill and band that have an enhancement slot, each counting equally. Smithery: 777 enhanceable of 777; Fabrication: 424 enhanceable of 456; Agriculture: 169 enhanceable of 319; Engineering: 671 enhanceable of 1786.
- Gate cohort: rank 50, the best gear, the at-level bands 41-45, 46-50, 51-53, both policies. Sweep: rank equal to the recipe's level (at most 50) in the bands 1-10, 11-20, 21-30, 31-40, 41-53, no gear, mid gear and best gear, the lookahead policy.
- Gear levels: none, the middle tier and the best tier of each crafting skill's gear, as in `legacy-crafting-baseline.md`. The meters use gear only: no food or status bonuses.
- Policies: **Dominant rotation** is the rotation players report (Waste Not, Basic Touch repeated, Master's Mend, then synthesis to finish); **Lookahead** tries each action against 8 sampled futures finished by the rotation (see `legacy-crafting-baseline.md`). The gate compares against the lookahead player, which is the stricter yardstick.
- Expected transfers: a craft that is made transfers each enhancement with probability `(int)(quality / max quality * 100) / 100` (truncated as the old window does), so it adds slots x chance; a craft that breaks the piece adds none. No transfer dice are drawn, which carries the same mean with less noise. Figures are per enhanced craft attempt.
- Extra progress: the enhancements' total penalty over the progress the recipe needs without them in the old window (rank-adjusted), averaged over the recipes.

Rules ported, each checked against the source by `LegacySourceTests`:

| Rule | Source |
| --- | --- |
| Enhancement slots | `CraftViewModel.cs:397-406` |
| Enhancement kinds and level limit | `CraftViewModel.cs:601-666` |
| Enhancement progress penalty | `CraftViewModel.cs:695-754` |
| Enhancement transfer chance | `CraftViewModel.cs:1420-1468` |
| Maximum progress modifier | `CraftViewModel.cs:460-478` |
| Recipe level chart | `RecipeLevelChart.cs:13-99` |

## Representative enhancements

Every enhancement kind and level the cohorts below use, with the spread of the real items and the one chosen. Penalties are in the old window's progress units.

| Kind | Level | Tier | Items | Penalty min / median / max | Representative item | Its penalty |
| --- | --- | --- | --- | --- | --- | --- |
| Weapon | 5 | I | 43 | 10 / 30.0 / 50 | Colicoid Leg (`colicoid_leg_f`) | 30 |
| Weapon | 15 | II | 42 | 20 / 40.0 / 70 | Improved Agrinium II (`imp_agrinium_2`) | 40 |
| Weapon | 25 | III | 34 | 30 / 50.0 / 80 | Improved Carvanium III (`imp_carvan_3`) | 50 |
| Weapon | 35 | IV | 37 | 40 / 60.0 / 80 | Improved Agrinium IV (`imp_agrinium_4`) | 60 |
| Weapon | 45 | V | 33 | 50 / 70.0 / 80 | Improved Agrinium V (`imp_agrinium_5`) | 70 |
| Armor | 5 | I | 60 | 10 / 10.0 / 40 | Damaged Blue Crystal (`p_crystal_blue`) | 10 |
| Armor | 15 | II | 53 | 10 / 20.0 / 50 | Rruchi (`rruchi`) | 20 |
| Armor | 25 | III | 48 | 30 / 40.0 / 60 | Armor Enhancement - Accuracy III (`aen_acc3`) | 40 |
| Armor | 35 | IV | 51 | 40 / 50.0 / 70 | Armor Enhancement - Defense - Force IV (`aen_def_for4`) | 50 |
| Armor | 45 | V | 50 | 50 / 60.0 / 150 | Armor Enhancement - Force Attack V (`aen_frcatk5`) | 60 |
| Structure | 5 | I | 2 | 20 / 30.0 / 40 | Structure Enhancement - Structure Bonus I (`sten_sb1`) | 20 |
| Structure | 15 | II | 3 | 30 / 40.0 / 50 | K'lor'slug Skin (`klorslug_skin2`) | 40 |
| Structure | 25 | III | 3 | 40 / 60.0 / 60 | Tench Tentacle (`mtench_tentacle`) | 60 |
| Structure | 35 | IV | 2 | 50 / 60.0 / 70 | Structure Enhancement - Structure Bonus IV (`sten_sb4`) | 50 |
| Structure | 45 | V | 2 | 60 / 70.0 / 80 | Structure Enhancement - Structure Bonus V (`sten_sb5`) | 60 |
| Food | 5 | I | 28 | 10 / 20.0 / 40 | Cooking Enhancement - Resistance - Mobility I (`cen_res_mob1`) | 20 |
| Food | 15 | II | 32 | 20 / 30.0 / 50 | Cooking Enhancement - Resistance - Trauma II (`cen_res_tra2`) | 30 |
| Food | 25 | III | 28 | 30 / 40.0 / 60 | Cooking Enhancement - Resistance - Mobility III (`cen_res_mob3`) | 40 |
| Food | 35 | IV | 28 | 40 / 50.0 / 70 | Cooking Enhancement - Resistance - Mobility IV (`cen_res_mob4`) | 50 |
| Food | 45 | V | 28 | 50 / 60.0 / 80 | Cooking Enhancement - Resistance - Mobility V (`cen_res_mob5`) | 60 |
| Starship | 5 | I | 24 | 10 / 25.0 / 40 | Starship Enhancement - Shield Recharge Rate I (`sen_shrech1`) | 20 |
| Starship | 15 | II | 33 | 20 / 40.0 / 50 | Improved Permaplas II (`imp_perma_2`) | 40 |
| Starship | 45 | V | 24 | 50 / 65.0 / 80 | Starship Enhancement - Shield Recharge Rate V (`sen_shrech5`) | 60 |
| Module | 1 | I | 1 | 10 / 10.0 / 10 | Module Enhancement - Module Bonus X (`men_mod10`) | 10 |
| Module | 5 | I | 2 | 20 / 30.0 / 40 | Module Enhancement - Module Bonus I (`men_mod1`) | 20 |
| Module | 15 | II | 3 | 30 / 40.0 / 50 | Korriban Wraid Claw (`wraid_claw2`) | 40 |
| Module | 25 | III | 2 | 40 / 50.0 / 60 | Module Enhancement - Module Bonus III (`men_mod3`) | 40 |
| Module | 35 | IV | 2 | 50 / 60.0 / 70 | Module Enhancement - Module Bonus IV (`men_mod4`) | 50 |
| Module | 45 | V | 2 | 60 / 70.0 / 80 | Module Enhancement - Module Bonus V (`men_mod5`) | 60 |
| Droid | 5 | I | 1 | 40 / 40.0 / 40 | Memory Augmentation I (`de_memaug1`) | 40 |
| Droid | 15 | II | 1 | 60 / 60.0 / 60 | Memory Augmentation II (`de_memaug2`) | 60 |
| Droid | 25 | III | 21 | 40 / 40.0 / 80 | Twin Pistol Boost I (`de_twinpist_b1`) | 40 |
| Droid | 35 | IV | 9 | 60 / 60.0 / 100 | Droid Enhancement - Resistance - Mind I (`de_res_mnd1`) | 60 |
| Droid | 45 | V | 21 | 80 / 80.0 / 120 | Twin Pistol Boost II (`de_twinpist_b2`) | 80 |
| Droid | 50 | VI | 8 | 100 / 100.0 / 100 | Droid Enhancement - Resistance - Ice II (`de_res_ice2`) | 100 |

## Gate 7 cohort: rank 50, best gear, at level

Expected enhancements transferred per enhanced craft. This is the figure the new minigames' thresholds must reach or beat. "Made" is the share of crafts that finish the item; "avg quality" and "chance" are over the crafts that were made.

### Smithery (best gear (Control 29, Craftsmanship 30, CP 37))

| Band | Recipes | Penalty each | Slots | Extra progress | Lookahead: made | avg quality | chance | enhancements transferred | Dominant: made | avg quality | chance | enhancements transferred |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 6 | 60-70 | 1 | 33% | 99% | 95% | 95% | 0.94 | 92% | 99% | 99% | 0.90 |
| 41-45 | 6 | 60-70 | 2 | 65% | 100% | 90% | 89% | 1.78 | 76% | 99% | 99% | 1.51 |
| 41-45 | 6 | 60-70 | 3 | 98% | 99% | 83% | 83% | 2.47 | 63% | 99% | 99% | 1.88 |
| 46-50 | 6 | 60 | 1 | 32% | 100% | 81% | 80% | 0.80 | 75% | 90% | 90% | 0.67 |
| 46-50 | 6 | 60 | 2 | 64% | 98% | 70% | 70% | 1.37 | 49% | 90% | 89% | 0.87 |
| 46-50 | 6 | 60 | 3 | 96% | 99% | 62% | 61% | 1.83 | 27% | 83% | 82% | 0.67 |
| 51-53 | 6 | 60-70 | 1 | 8% | 49% | 6% | 5% | 0.03 | 0% | 0% | 0% | 0.00 |
| 51-53 | 6 | 60-70 | 2 | 17% | 29% | 5% | 5% | 0.03 | 0% | 0% | 0% | 0.00 |
| 51-53 | 6 | 60-70 | 3 | 25% | 12% | 4% | 3% | 0.01 | 0% | 0% | 0% | 0.00 |

### Fabrication (best gear (Control 29, Craftsmanship 30, CP 37))

| Band | Recipes | Penalty each | Slots | Extra progress | Lookahead: made | avg quality | chance | enhancements transferred | Dominant: made | avg quality | chance | enhancements transferred |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 6 | 60 | 1 | 32% | 99% | 95% | 95% | 0.94 | 92% | 99% | 99% | 0.91 |
| 41-45 | 6 | 60 | 2 | 63% | 99% | 89% | 89% | 1.76 | 77% | 99% | 98% | 1.52 |
| 41-45 | 6 | 60 | 3 | 95% | 99% | 83% | 83% | 2.47 | 62% | 99% | 99% | 1.85 |
| 46-50 | 6 | 60 | 1 | 32% | 99% | 83% | 83% | 0.82 | 83% | 93% | 93% | 0.77 |
| 46-50 | 6 | 60 | 2 | 64% | 100% | 74% | 73% | 1.45 | 49% | 94% | 93% | 0.91 |
| 46-50 | 6 | 60 | 3 | 96% | 99% | 65% | 64% | 1.90 | 39% | 89% | 89% | 1.04 |
| 51-53 | 6 | 60 | 1 | 10% | 66% | 14% | 13% | 0.09 | 0% | 0% | 0% | 0.00 |
| 51-53 | 6 | 60 | 2 | 20% | 54% | 13% | 12% | 0.13 | 3% | 41% | 41% | 0.02 |
| 51-53 | 6 | 60 | 3 | 30% | 38% | 13% | 12% | 0.14 | 3% | 40% | 40% | 0.04 |

### Agriculture (best gear (Control 29, Craftsmanship 30, CP 37))

| Band | Recipes | Penalty each | Slots | Extra progress | Lookahead: made | avg quality | chance | enhancements transferred | Dominant: made | avg quality | chance | enhancements transferred |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 6 | 60 | 1 | 32% | 100% | 95% | 95% | 0.95 | 94% | 99% | 99% | 0.92 |
| 41-45 | 6 | 60 | 2 | 63% | 99% | 90% | 90% | 1.78 | 78% | 99% | 99% | 1.55 |
| 41-45 | 6 | 60 | 3 | 95% | 99% | 82% | 82% | 2.43 | 61% | 99% | 99% | 1.81 |
| 46-50 | 6 | 60 | 1 | 32% | 100% | 85% | 85% | 0.85 | 82% | 94% | 94% | 0.77 |
| 46-50 | 6 | 60 | 2 | 64% | 99% | 75% | 75% | 1.48 | 50% | 94% | 94% | 0.95 |
| 46-50 | 6 | 60 | 3 | 96% | 99% | 67% | 66% | 1.97 | 42% | 91% | 91% | 1.15 |
| 51-53 | 6 | 60 | 1 | 8% | 52% | 5% | 5% | 0.03 | 0% | 0% | 0% | 0.00 |
| 51-53 | 6 | 60 | 2 | 16% | 34% | 4% | 4% | 0.03 | 0% | 0% | 0% | 0.00 |
| 51-53 | 6 | 60 | 3 | 24% | 14% | 4% | 4% | 0.02 | 0% | 0% | 0% | 0.00 |

### Engineering (best gear (Control 29, Craftsmanship 30, CP 37))

| Band | Recipes | Penalty each | Slots | Extra progress | Lookahead: made | avg quality | chance | enhancements transferred | Dominant: made | avg quality | chance | enhancements transferred |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 41-45 | 6 | 60-100 | 1 | 34% | 99% | 93% | 93% | 0.92 | 89% | 98% | 98% | 0.87 |
| 41-45 | 6 | 60-100 | 2 | 69% | 99% | 86% | 85% | 1.70 | 72% | 98% | 98% | 1.41 |
| 41-45 | 6 | 60-100 | 3 | 103% | 99% | 77% | 77% | 2.27 | 54% | 98% | 98% | 1.59 |
| 46-50 | 6 | 60-100 | 1 | 39% | 99% | 80% | 80% | 0.79 | 75% | 92% | 91% | 0.69 |
| 46-50 | 6 | 60-100 | 2 | 79% | 99% | 67% | 66% | 1.31 | 40% | 91% | 90% | 0.73 |
| 46-50 | 6 | 60-100 | 3 | 118% | 99% | 57% | 56% | 1.67 | 27% | 85% | 84% | 0.68 |
| 51-53 | 6 | 60-70 | 1 | 7% | 34% | 6% | 5% | 0.02 | 0% | 0% | 0% | 0.00 |
| 51-53 | 6 | 60-70 | 2 | 15% | 23% | 5% | 4% | 0.02 | 0% | 0% | 0% | 0.00 |
| 51-53 | 6 | 60-70 | 3 | 22% | 6% | 4% | 4% | 0.01 | 0% | 0% | 0% | 0.00 |

Check against player reports: crafters report about 80% quality on triple-enhanced endgame crafts with the best gear. The model's average quality of the items made with 3 enhancements, over every skill: 41-45: 82% lookahead, 99% rotation; 46-50: 62% lookahead, 88% rotation; 51-53: 9% lookahead, 40% rotation.

## By band, enhancement tier and slot count

The lookahead player at rank equal to the recipe's level (at most 50). A tier is the enhancement level's group (5 is I, 15 is II, 25 is III, 35 is IV, 45 is V); a recipe only accepts a tier whose level is at most 5 above its own. Each cell is the expected enhancements transferred per craft, with the share of crafts made in brackets.

### Smithery, no gear

| Band | Tier (level) | Recipes | Penalty each | Extra progress at most slots | 1 slot | 2 slots | 3 slots | Avg quality at fewest / most slots |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | I (5) | 6 | 10-30 | 113% | 0.00 (78%) | 0.00 (68%) | 0.00 (45%) | 0% / 0% |
| 1-10 | II (15) | 2 | 20 | 133% | 0.00 (70%) | 0.00 (0%) | 0.00 (0%) | 1% / 0% |
| 11-20 | I (5) | 6 | 10-30 | 63% | 0.01 (83%) | 0.02 (63%) | 0.03 (39%) | 2% / 3% |
| 11-20 | II (15) | 6 | 20-40 | 110% | 0.01 (63%) | 0.02 (29%) | 0.02 (32%) | 2% / 3% |
| 11-20 | III (25) | 2 | 40 | 162% | 0.03 (90%) | 0.04 (98%) | 0.02 (97%) | 4% / 1% |
| 21-30 | I (5) | 6 | 10-30 | 59% | 0.06 (97%) | 0.12 (97%) | 0.17 (95%) | 7% / 7% |
| 21-30 | II (15) | 6 | 20-40 | 92% | 0.06 (97%) | 0.11 (96%) | 0.14 (94%) | 7% / 5% |
| 21-30 | III (25) | 6 | 40-50 | 146% | 0.05 (95%) | 0.09 (94%) | 0.08 (92%) | 6% / 3% |
| 21-30 | IV (35) | 1 | 50 | 147% | 0.06 (94%) | 0.08 (92%) | 0.09 (90%) | 7% / 4% |
| 31-40 | I (5) | 6 | 10-30 | 32% | 0.22 (97%) | 0.38 (97%) | 0.56 (97%) | 23% / 20% |
| 31-40 | II (15) | 6 | 20-40 | 57% | 0.19 (96%) | 0.38 (96%) | 0.47 (96%) | 21% / 17% |
| 31-40 | III (25) | 6 | 40-50 | 103% | 0.19 (97%) | 0.29 (96%) | 0.30 (94%) | 20% / 11% |
| 31-40 | IV (35) | 6 | 50-60 | 127% | 0.16 (95%) | 0.25 (95%) | 0.23 (92%) | 18% / 9% |
| 31-40 | V (45) | 2 | 60 | 130% | 0.34 (99%) | 0.47 (98%) | 0.45 (90%) | 35% / 17% |
| 41-53 | I (5) | 6 | 10-30 | 15% | 0.25 (66%) | 0.48 (66%) | 0.68 (66%) | 38% / 35% |
| 41-53 | II (15) | 6 | 20-40 | 28% | 0.24 (66%) | 0.41 (65%) | 0.56 (65%) | 37% / 29% |
| 41-53 | III (25) | 6 | 40-50 | 54% | 0.21 (66%) | 0.35 (65%) | 0.37 (64%) | 33% / 20% |
| 41-53 | IV (35) | 6 | 50-60 | 67% | 0.20 (66%) | 0.30 (64%) | 0.27 (62%) | 31% / 15% |
| 41-53 | V (45) | 6 | 60-70 | 81% | 0.20 (66%) | 0.25 (63%) | 0.20 (60%) | 30% / 12% |

### Smithery, mid gear (Control 13, Craftsmanship 12, CP 23)

| Band | Tier (level) | Recipes | Penalty each | Extra progress at most slots | 1 slot | 2 slots | 3 slots | Avg quality at fewest / most slots |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | I (5) | 6 | 10-30 | 113% | 0.10 (97%) | 0.18 (90%) | 0.22 (83%) | 11% / 9% |
| 1-10 | II (15) | 2 | 20 | 133% | 0.13 (98%) | 0.21 (99%) | 0.22 (98%) | 13% / 7% |
| 11-20 | I (5) | 6 | 10-30 | 63% | 0.12 (98%) | 0.20 (96%) | 0.30 (97%) | 12% / 11% |
| 11-20 | II (15) | 6 | 20-40 | 110% | 0.11 (98%) | 0.17 (96%) | 0.21 (94%) | 11% / 8% |
| 11-20 | III (25) | 2 | 40 | 162% | 0.12 (97%) | 0.18 (92%) | 0.25 (96%) | 13% / 9% |
| 21-30 | I (5) | 6 | 10-30 | 59% | 0.14 (99%) | 0.28 (99%) | 0.40 (98%) | 15% / 14% |
| 21-30 | II (15) | 6 | 20-40 | 92% | 0.14 (99%) | 0.26 (98%) | 0.38 (98%) | 15% / 13% |
| 21-30 | III (25) | 6 | 40-50 | 146% | 0.14 (98%) | 0.26 (98%) | 0.33 (97%) | 15% / 12% |
| 21-30 | IV (35) | 1 | 50 | 147% | 0.14 (97%) | 0.24 (97%) | 0.29 (96%) | 15% / 10% |
| 31-40 | I (5) | 6 | 10-30 | 32% | 0.38 (99%) | 0.68 (99%) | 1.03 (98%) | 39% / 36% |
| 31-40 | II (15) | 6 | 20-40 | 57% | 0.35 (98%) | 0.69 (99%) | 0.95 (98%) | 36% / 33% |
| 31-40 | III (25) | 6 | 40-50 | 103% | 0.34 (98%) | 0.62 (98%) | 0.75 (97%) | 36% / 26% |
| 31-40 | IV (35) | 6 | 50-60 | 127% | 0.35 (98%) | 0.60 (99%) | 0.66 (97%) | 36% / 23% |
| 31-40 | V (45) | 2 | 60 | 130% | 0.71 (100%) | 1.10 (98%) | 1.31 (98%) | 71% / 45% |
| 41-53 | I (5) | 6 | 10-30 | 15% | 0.44 (72%) | 0.87 (70%) | 1.28 (69%) | 61% / 62% |
| 41-53 | II (15) | 6 | 20-40 | 28% | 0.44 (71%) | 0.84 (69%) | 1.17 (68%) | 63% / 58% |
| 41-53 | III (25) | 6 | 40-50 | 54% | 0.42 (69%) | 0.73 (68%) | 0.90 (66%) | 61% / 46% |
| 41-53 | IV (35) | 6 | 50-60 | 67% | 0.39 (68%) | 0.69 (67%) | 0.79 (65%) | 58% / 41% |
| 41-53 | V (45) | 6 | 60-70 | 81% | 0.39 (69%) | 0.59 (66%) | 0.68 (65%) | 57% / 35% |

### Smithery, best gear (Control 29, Craftsmanship 30, CP 37)

| Band | Tier (level) | Recipes | Penalty each | Extra progress at most slots | 1 slot | 2 slots | 3 slots | Avg quality at fewest / most slots |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | I (5) | 6 | 10-30 | 113% | 0.27 (98%) | 0.47 (97%) | 0.68 (96%) | 28% / 24% |
| 1-10 | II (15) | 2 | 20 | 133% | 0.32 (99%) | 0.67 (100%) | 0.86 (99%) | 33% / 30% |
| 11-20 | I (5) | 6 | 10-30 | 63% | 0.27 (99%) | 0.54 (99%) | 0.78 (99%) | 28% / 27% |
| 11-20 | II (15) | 6 | 20-40 | 110% | 0.27 (99%) | 0.49 (99%) | 0.73 (98%) | 28% / 25% |
| 11-20 | III (25) | 2 | 40 | 162% | 0.25 (100%) | 0.48 (100%) | 0.73 (100%) | 26% / 25% |
| 21-30 | I (5) | 6 | 10-30 | 59% | 0.28 (99%) | 0.55 (100%) | 0.80 (99%) | 28% / 28% |
| 21-30 | II (15) | 6 | 20-40 | 92% | 0.27 (99%) | 0.54 (99%) | 0.77 (100%) | 28% / 26% |
| 21-30 | III (25) | 6 | 40-50 | 146% | 0.27 (99%) | 0.51 (100%) | 0.71 (99%) | 28% / 24% |
| 21-30 | IV (35) | 1 | 50 | 147% | 0.25 (100%) | 0.49 (100%) | 0.65 (100%) | 26% / 22% |
| 31-40 | I (5) | 6 | 10-30 | 32% | 0.49 (99%) | 0.98 (99%) | 1.46 (99%) | 50% / 49% |
| 31-40 | II (15) | 6 | 20-40 | 57% | 0.49 (99%) | 0.96 (100%) | 1.46 (100%) | 50% / 49% |
| 31-40 | III (25) | 6 | 40-50 | 103% | 0.49 (99%) | 0.94 (99%) | 1.31 (100%) | 49% / 44% |
| 31-40 | IV (35) | 6 | 50-60 | 127% | 0.48 (100%) | 0.91 (99%) | 1.22 (99%) | 49% / 41% |
| 31-40 | V (45) | 2 | 60 | 130% | 0.95 (98%) | 1.69 (100%) | 2.37 (100%) | 98% / 80% |
| 41-53 | I (5) | 6 | 10-30 | 15% | 0.61 (86%) | 1.18 (88%) | 1.76 (82%) | 71% / 72% |
| 41-53 | II (15) | 6 | 20-40 | 28% | 0.59 (86%) | 1.17 (81%) | 1.71 (80%) | 69% / 71% |
| 41-53 | III (25) | 6 | 40-50 | 54% | 0.59 (83%) | 1.08 (79%) | 1.43 (74%) | 71% / 65% |
| 41-53 | IV (35) | 6 | 50-60 | 67% | 0.57 (83%) | 1.01 (78%) | 1.33 (71%) | 70% / 63% |
| 41-53 | V (45) | 6 | 60-70 | 81% | 0.58 (82%) | 0.96 (75%) | 1.28 (70%) | 70% / 61% |

### Fabrication, no gear

| Band | Tier (level) | Recipes | Penalty each | Extra progress at most slots | 1 slot | 2 slots | 3 slots | Avg quality at fewest / most slots |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | I (5) | 6 | 20 | 198% | 0.00 (75%) | 0.00 (9%) | 0.00 (0%) | 0% / 0% |
| 1-10 | II (15) | 1 | 40 | 267% | 0.00 (0%) | 0.00 (0%) | 0.00 (0%) | 0% / 0% |
| 11-20 | I (5) | 6 | 20 | 102% | 0.01 (71%) | 0.01 (15%) | 0.01 (16%) | 1% / 3% |
| 11-20 | II (15) | 6 | 40 | 204% | 0.00 (15%) | 0.01 (16%) | 0.00 (16%) | 4% / 1% |
| 11-20 | III (25) | 1 | 60 | 243% | 0.02 (96%) | 0.02 (95%) | 0.01 (90%) | 3% / 1% |
| 21-30 | I (5) | 6 | 20 | 67% | 0.06 (97%) | 0.11 (96%) | 0.15 (97%) | 7% / 6% |
| 21-30 | II (15) | 6 | 40 | 134% | 0.06 (96%) | 0.10 (96%) | 0.09 (94%) | 6% / 4% |
| 21-30 | III (25) | 6 | 60 | 201% | 0.05 (96%) | 0.06 (93%) | 0.05 (90%) | 6% / 2% |
| 21-30 | IV (35) | 1 | 50 | 147% | 0.06 (96%) | 0.08 (92%) | 0.09 (90%) | 7% / 4% |
| 31-40 | I (5) | 6 | 20 | 50% | 0.14 (95%) | 0.26 (97%) | 0.33 (96%) | 15% / 12% |
| 31-40 | II (15) | 6 | 40 | 101% | 0.13 (94%) | 0.20 (94%) | 0.22 (93%) | 14% / 8% |
| 31-40 | III (25) | 6 | 60 | 151% | 0.11 (96%) | 0.14 (93%) | 0.14 (91%) | 12% / 6% |
| 31-40 | IV (35) | 6 | 50 | 126% | 0.12 (95%) | 0.18 (94%) | 0.17 (91%) | 13% / 7% |
| 31-40 | V (45) | 1 | 60 | 130% | 0.36 (99%) | 0.46 (96%) | 0.44 (94%) | 37% / 16% |
| 41-53 | I (5) | 6 | 20 | 37% | 0.37 (99%) | 0.65 (98%) | 0.87 (98%) | 38% / 30% |
| 41-53 | II (15) | 6 | 40 | 74% | 0.33 (99%) | 0.52 (96%) | 0.59 (95%) | 34% / 21% |
| 41-53 | III (25) | 6 | 60 | 110% | 0.30 (98%) | 0.40 (96%) | 0.32 (91%) | 31% / 12% |
| 41-53 | IV (35) | 6 | 50 | 92% | 0.32 (99%) | 0.46 (96%) | 0.46 (92%) | 33% / 17% |
| 41-53 | V (45) | 6 | 60 | 110% | 0.30 (98%) | 0.40 (96%) | 0.32 (91%) | 31% / 12% |

### Fabrication, mid gear (Control 13, Craftsmanship 12, CP 26)

| Band | Tier (level) | Recipes | Penalty each | Extra progress at most slots | 1 slot | 2 slots | 3 slots | Avg quality at fewest / most slots |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | I (5) | 6 | 20 | 198% | 0.06 (96%) | 0.09 (91%) | 0.09 (80%) | 7% / 4% |
| 1-10 | II (15) | 1 | 40 | 267% | 0.13 (100%) | 0.10 (98%) | 0.07 (85%) | 13% / 3% |
| 11-20 | I (5) | 6 | 20 | 102% | 0.12 (98%) | 0.19 (97%) | 0.22 (95%) | 12% / 8% |
| 11-20 | II (15) | 6 | 40 | 204% | 0.10 (97%) | 0.12 (95%) | 0.10 (85%) | 10% / 4% |
| 11-20 | III (25) | 1 | 60 | 243% | 0.12 (98%) | 0.19 (96%) | 0.19 (92%) | 13% / 7% |
| 21-30 | I (5) | 6 | 20 | 67% | 0.15 (99%) | 0.29 (99%) | 0.42 (99%) | 16% / 15% |
| 21-30 | II (15) | 6 | 40 | 134% | 0.14 (98%) | 0.26 (98%) | 0.35 (98%) | 15% / 12% |
| 21-30 | III (25) | 6 | 60 | 201% | 0.14 (99%) | 0.23 (97%) | 0.26 (96%) | 15% / 9% |
| 21-30 | IV (35) | 1 | 50 | 147% | 0.14 (98%) | 0.24 (98%) | 0.29 (96%) | 15% / 11% |
| 31-40 | I (5) | 6 | 20 | 50% | 0.25 (98%) | 0.48 (98%) | 0.71 (98%) | 26% / 25% |
| 31-40 | II (15) | 6 | 40 | 101% | 0.25 (99%) | 0.46 (98%) | 0.60 (98%) | 26% / 21% |
| 31-40 | III (25) | 6 | 60 | 151% | 0.23 (98%) | 0.39 (98%) | 0.44 (96%) | 25% / 16% |
| 31-40 | IV (35) | 6 | 50 | 126% | 0.24 (98%) | 0.42 (98%) | 0.52 (97%) | 25% / 19% |
| 31-40 | V (45) | 1 | 60 | 130% | 0.73 (98%) | 1.24 (100%) | 1.41 (99%) | 75% / 48% |
| 41-53 | I (5) | 6 | 20 | 37% | 0.68 (99%) | 1.29 (99%) | 1.85 (99%) | 69% / 63% |
| 41-53 | II (15) | 6 | 40 | 74% | 0.65 (100%) | 1.13 (99%) | 1.49 (99%) | 66% / 51% |
| 41-53 | III (25) | 6 | 60 | 110% | 0.62 (99%) | 0.99 (99%) | 1.09 (97%) | 63% / 38% |
| 41-53 | IV (35) | 6 | 50 | 92% | 0.63 (99%) | 1.05 (98%) | 1.23 (98%) | 64% / 42% |
| 41-53 | V (45) | 6 | 60 | 110% | 0.62 (99%) | 0.99 (99%) | 1.09 (97%) | 63% / 38% |

### Fabrication, best gear (Control 29, Craftsmanship 30, CP 37)

| Band | Tier (level) | Recipes | Penalty each | Extra progress at most slots | 1 slot | 2 slots | 3 slots | Avg quality at fewest / most slots |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | I (5) | 6 | 20 | 198% | 0.17 (98%) | 0.30 (96%) | 0.35 (95%) | 18% / 13% |
| 1-10 | II (15) | 1 | 40 | 267% | 0.34 (100%) | 0.51 (100%) | 0.69 (98%) | 34% / 24% |
| 11-20 | I (5) | 6 | 20 | 102% | 0.28 (99%) | 0.51 (98%) | 0.79 (99%) | 29% / 27% |
| 11-20 | II (15) | 6 | 40 | 204% | 0.26 (99%) | 0.48 (99%) | 0.58 (99%) | 26% / 20% |
| 11-20 | III (25) | 1 | 60 | 243% | 0.25 (98%) | 0.48 (99%) | 0.50 (100%) | 25% / 17% |
| 21-30 | I (5) | 6 | 20 | 67% | 0.28 (100%) | 0.55 (99%) | 0.80 (99%) | 28% / 27% |
| 21-30 | II (15) | 6 | 40 | 134% | 0.27 (99%) | 0.51 (99%) | 0.72 (99%) | 28% / 25% |
| 21-30 | III (25) | 6 | 60 | 201% | 0.26 (99%) | 0.48 (99%) | 0.61 (98%) | 27% / 21% |
| 21-30 | IV (35) | 1 | 50 | 147% | 0.26 (100%) | 0.47 (100%) | 0.65 (99%) | 27% / 23% |
| 31-40 | I (5) | 6 | 20 | 50% | 0.37 (100%) | 0.73 (99%) | 1.09 (99%) | 38% / 37% |
| 31-40 | II (15) | 6 | 40 | 101% | 0.37 (100%) | 0.70 (99%) | 0.99 (99%) | 37% / 34% |
| 31-40 | III (25) | 6 | 60 | 151% | 0.36 (99%) | 0.66 (99%) | 0.87 (98%) | 37% / 30% |
| 31-40 | IV (35) | 6 | 50 | 126% | 0.36 (99%) | 0.69 (99%) | 0.91 (99%) | 37% / 31% |
| 31-40 | V (45) | 1 | 60 | 130% | 0.97 (100%) | 1.73 (100%) | 2.39 (100%) | 98% / 81% |
| 41-53 | I (5) | 6 | 20 | 37% | 0.90 (100%) | 1.77 (100%) | 2.58 (99%) | 91% / 87% |
| 41-53 | II (15) | 6 | 40 | 74% | 0.88 (99%) | 1.65 (99%) | 2.18 (100%) | 90% / 74% |
| 41-53 | III (25) | 6 | 60 | 110% | 0.85 (100%) | 1.45 (100%) | 1.93 (99%) | 86% / 65% |
| 41-53 | IV (35) | 6 | 50 | 92% | 0.86 (99%) | 1.54 (99%) | 2.07 (99%) | 87% / 70% |
| 41-53 | V (45) | 6 | 60 | 110% | 0.85 (100%) | 1.45 (100%) | 1.93 (99%) | 86% / 65% |

### Agriculture, no gear

| Band | Tier (level) | Recipes | Penalty each | Extra progress at most slots | 1 slot | 2 slots | 3 slots | Avg quality at fewest / most slots |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | I (5) | 6 | 20 | 229% | 0.00 (80%) | 0.00 (17%) | 0.00 (0%) | 0% / 0% |
| 11-20 | I (5) | 6 | 20 | 106% | 0.00 (78%) | 0.00 (16%) | 0.00 (0%) | 0% / 0% |
| 11-20 | II (15) | 6 | 30 | 159% | 0.00 (49%) | 0.00 (0%) | 0.00 (0%) | 0% / 0% |
| 21-30 | I (5) | 6 | 20 | 69% | 0.06 (95%) | 0.12 (96%) | 0.16 (96%) | 7% / 6% |
| 21-30 | II (15) | 6 | 30 | 104% | 0.07 (94%) | 0.11 (96%) | 0.13 (95%) | 7% / 5% |
| 21-30 | III (25) | 6 | 40 | 138% | 0.06 (96%) | 0.09 (95%) | 0.10 (92%) | 7% / 4% |
| 21-30 | IV (35) | 1 | 50 | 147% | 0.06 (94%) | 0.08 (91%) | 0.09 (92%) | 7% / 4% |
| 31-40 | I (5) | 6 | 20 | 50% | 0.07 (95%) | 0.14 (94%) | 0.18 (95%) | 8% / 7% |
| 31-40 | II (15) | 6 | 30 | 76% | 0.07 (94%) | 0.12 (95%) | 0.14 (92%) | 8% / 6% |
| 31-40 | III (25) | 6 | 40 | 101% | 0.07 (95%) | 0.10 (94%) | 0.12 (93%) | 8% / 5% |
| 31-40 | IV (35) | 6 | 50 | 126% | 0.06 (95%) | 0.09 (92%) | 0.10 (92%) | 7% / 4% |
| 41-53 | I (5) | 6 | 20 | 28% | 0.25 (66%) | 0.45 (66%) | 0.63 (65%) | 38% / 33% |
| 41-53 | II (15) | 6 | 30 | 42% | 0.24 (65%) | 0.41 (65%) | 0.53 (65%) | 38% / 28% |
| 41-53 | III (25) | 6 | 40 | 55% | 0.22 (66%) | 0.36 (65%) | 0.41 (63%) | 34% / 22% |
| 41-53 | IV (35) | 6 | 50 | 69% | 0.21 (66%) | 0.32 (64%) | 0.33 (62%) | 33% / 18% |
| 41-53 | V (45) | 6 | 60 | 83% | 0.21 (65%) | 0.27 (63%) | 0.22 (61%) | 32% / 12% |

### Agriculture, mid gear (Control 13, Craftsmanship 12, CP 20)

| Band | Tier (level) | Recipes | Penalty each | Extra progress at most slots | 1 slot | 2 slots | 3 slots | Avg quality at fewest / most slots |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | I (5) | 6 | 20 | 229% | 0.03 (95%) | 0.04 (92%) | 0.03 (78%) | 4% / 2% |
| 11-20 | I (5) | 6 | 20 | 106% | 0.09 (98%) | 0.14 (95%) | 0.17 (95%) | 10% / 6% |
| 11-20 | II (15) | 6 | 30 | 159% | 0.09 (97%) | 0.11 (94%) | 0.08 (90%) | 10% / 4% |
| 21-30 | I (5) | 6 | 20 | 69% | 0.14 (99%) | 0.27 (98%) | 0.39 (98%) | 15% / 14% |
| 21-30 | II (15) | 6 | 30 | 104% | 0.14 (98%) | 0.26 (98%) | 0.36 (97%) | 15% / 13% |
| 21-30 | III (25) | 6 | 40 | 138% | 0.14 (99%) | 0.25 (98%) | 0.33 (98%) | 14% / 12% |
| 21-30 | IV (35) | 1 | 50 | 147% | 0.14 (99%) | 0.22 (96%) | 0.27 (96%) | 14% / 10% |
| 31-40 | I (5) | 6 | 20 | 50% | 0.13 (98%) | 0.26 (98%) | 0.37 (97%) | 14% / 13% |
| 31-40 | II (15) | 6 | 30 | 76% | 0.13 (98%) | 0.25 (97%) | 0.35 (98%) | 14% / 12% |
| 31-40 | III (25) | 6 | 40 | 101% | 0.13 (98%) | 0.23 (97%) | 0.31 (97%) | 14% / 11% |
| 31-40 | IV (35) | 6 | 50 | 126% | 0.12 (96%) | 0.22 (97%) | 0.26 (97%) | 13% / 10% |
| 41-53 | I (5) | 6 | 20 | 28% | 0.45 (72%) | 0.85 (70%) | 1.19 (70%) | 63% / 57% |
| 41-53 | II (15) | 6 | 30 | 42% | 0.45 (71%) | 0.80 (69%) | 1.07 (68%) | 64% / 53% |
| 41-53 | III (25) | 6 | 40 | 55% | 0.43 (71%) | 0.76 (68%) | 0.89 (67%) | 61% / 45% |
| 41-53 | IV (35) | 6 | 50 | 69% | 0.40 (68%) | 0.68 (67%) | 0.81 (65%) | 59% / 42% |
| 41-53 | V (45) | 6 | 60 | 83% | 0.40 (68%) | 0.60 (66%) | 0.66 (65%) | 59% / 35% |

### Agriculture, best gear (Control 29, Craftsmanship 30, CP 37)

| Band | Tier (level) | Recipes | Penalty each | Extra progress at most slots | 1 slot | 2 slots | 3 slots | Avg quality at fewest / most slots |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | I (5) | 6 | 20 | 229% | 0.11 (98%) | 0.19 (98%) | 0.21 (96%) | 12% / 8% |
| 11-20 | I (5) | 6 | 20 | 106% | 0.28 (99%) | 0.51 (99%) | 0.80 (99%) | 29% / 28% |
| 11-20 | II (15) | 6 | 30 | 159% | 0.28 (100%) | 0.53 (99%) | 0.69 (99%) | 29% / 24% |
| 21-30 | I (5) | 6 | 20 | 69% | 0.28 (99%) | 0.56 (99%) | 0.83 (100%) | 29% / 28% |
| 21-30 | II (15) | 6 | 30 | 104% | 0.28 (99%) | 0.55 (100%) | 0.79 (99%) | 29% / 27% |
| 21-30 | III (25) | 6 | 40 | 138% | 0.28 (100%) | 0.53 (99%) | 0.77 (99%) | 29% / 26% |
| 21-30 | IV (35) | 1 | 50 | 147% | 0.25 (98%) | 0.49 (100%) | 0.65 (99%) | 26% / 22% |
| 31-40 | I (5) | 6 | 20 | 50% | 0.25 (99%) | 0.48 (99%) | 0.71 (98%) | 26% / 25% |
| 31-40 | II (15) | 6 | 30 | 76% | 0.25 (99%) | 0.48 (99%) | 0.69 (99%) | 25% / 24% |
| 31-40 | III (25) | 6 | 40 | 101% | 0.24 (100%) | 0.47 (99%) | 0.65 (99%) | 25% / 22% |
| 31-40 | IV (35) | 6 | 50 | 126% | 0.24 (99%) | 0.45 (99%) | 0.60 (99%) | 24% / 21% |
| 41-53 | I (5) | 6 | 20 | 28% | 0.61 (88%) | 1.22 (82%) | 1.76 (83%) | 70% / 71% |
| 41-53 | II (15) | 6 | 30 | 42% | 0.60 (85%) | 1.18 (87%) | 1.70 (80%) | 72% / 71% |
| 41-53 | III (25) | 6 | 40 | 55% | 0.61 (84%) | 1.13 (81%) | 1.49 (77%) | 73% / 65% |
| 41-53 | IV (35) | 6 | 50 | 69% | 0.60 (83%) | 1.06 (80%) | 1.37 (73%) | 73% / 64% |
| 41-53 | V (45) | 6 | 60 | 83% | 0.59 (84%) | 0.99 (77%) | 1.30 (70%) | 71% / 62% |

### Engineering, no gear

| Band | Tier (level) | Recipes | Penalty each | Extra progress at most slots | 1 slot | 2 slots | 3 slots | Avg quality at fewest / most slots |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | I (1) | 2 | 10 | 120% | 0.00 (86%) | 0.00 (74%) | 0.00 (46%) | 1% / 0% |
| 1-10 | I (5) | 6 | 10-20 | 138% | 0.00 (80%) | 0.00 (45%) | 0.00 (14%) | 1% / 0% |
| 1-10 | II (15) | 3 | 20-40 | 178% | 0.00 (52%) | 0.00 (0%) | 0.00 (0%) | 1% / 0% |
| 11-20 | I (1) | 3 | 10 | 52% | 0.01 (94%) | 0.01 (78%) | 0.00 (51%) | 1% / 0% |
| 11-20 | I (5) | 6 | 10-20 | 73% | 0.01 (85%) | 0.02 (58%) | 0.03 (36%) | 2% / 3% |
| 11-20 | II (15) | 6 | 20-40 | 146% | 0.01 (59%) | 0.02 (30%) | 0.02 (33%) | 2% / 3% |
| 11-20 | III (25) | 2 | 40 | 162% | 0.03 (88%) | 0.04 (98%) | 0.02 (92%) | 4% / 1% |
| 21-30 | I (1) | 2 | 10 | 32% | 0.08 (96%) | 0.16 (95%) | 0.23 (96%) | 9% / 8% |
| 21-30 | I (5) | 6 | 10-40 | 74% | 0.07 (97%) | 0.12 (95%) | 0.15 (94%) | 8% / 6% |
| 21-30 | II (15) | 6 | 20-60 | 127% | 0.06 (96%) | 0.10 (95%) | 0.10 (94%) | 7% / 4% |
| 21-30 | III (25) | 6 | 40 | 129% | 0.06 (96%) | 0.10 (95%) | 0.10 (93%) | 7% / 4% |
| 21-30 | IV (35) | 2 | 50-60 | 162% | 0.06 (94%) | 0.08 (92%) | 0.08 (88%) | 7% / 4% |
| 31-40 | I (1) | 2 | 10 | 23% | 0.08 (94%) | 0.15 (92%) | 0.22 (95%) | 9% / 8% |
| 31-40 | I (5) | 6 | 10-40 | 54% | 0.21 (96%) | 0.35 (95%) | 0.46 (94%) | 22% / 17% |
| 31-40 | II (15) | 6 | 20-60 | 92% | 0.18 (95%) | 0.29 (95%) | 0.34 (94%) | 19% / 13% |
| 31-40 | III (25) | 6 | 40 | 93% | 0.19 (95%) | 0.30 (95%) | 0.31 (93%) | 21% / 12% |
| 31-40 | IV (35) | 6 | 50-60 | 124% | 0.17 (96%) | 0.23 (93%) | 0.24 (91%) | 18% / 9% |
| 31-40 | V (45) | 2 | 60-80 | 152% | 0.36 (98%) | 0.42 (95%) | 0.37 (90%) | 37% / 14% |
| 41-53 | I (1) | 1 | 10 | 4% | 0.00 (0%) | 0.00 (0%) | 0.00 (0%) | 0% / 0% |
| 41-53 | I (5) | 6 | 10-40 | 42% | 0.27 (82%) | 0.49 (81%) | 0.62 (80%) | 33% / 26% |
| 41-53 | II (15) | 6 | 20-60 | 67% | 0.26 (81%) | 0.37 (80%) | 0.43 (77%) | 32% / 19% |
| 41-53 | III (25) | 6 | 40 | 61% | 0.24 (82%) | 0.42 (80%) | 0.46 (80%) | 30% / 20% |
| 41-53 | IV (35) | 6 | 50-60 | 85% | 0.24 (81%) | 0.34 (80%) | 0.30 (76%) | 30% / 14% |
| 41-53 | V (45) | 6 | 60-80 | 109% | 0.22 (81%) | 0.24 (77%) | 0.18 (70%) | 27% / 9% |
| 41-53 | VI (50) | 3 | 100 | 175% | 0.21 (96%) | 0.16 (86%) | 0.10 (66%) | 23% / 5% |

### Engineering, mid gear (Control 13, Craftsmanship 12, CP 20)

| Band | Tier (level) | Recipes | Penalty each | Extra progress at most slots | 1 slot | 2 slots | 3 slots | Avg quality at fewest / most slots |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | I (1) | 2 | 10 | 120% | 0.04 (96%) | 0.04 (95%) | 0.06 (94%) | 4% / 2% |
| 1-10 | I (5) | 6 | 10-20 | 138% | 0.07 (95%) | 0.12 (93%) | 0.14 (88%) | 8% / 6% |
| 1-10 | II (15) | 3 | 20-40 | 178% | 0.10 (98%) | 0.15 (96%) | 0.13 (92%) | 11% / 5% |
| 11-20 | I (1) | 3 | 10 | 52% | 0.11 (96%) | 0.18 (98%) | 0.28 (96%) | 12% / 10% |
| 11-20 | I (5) | 6 | 10-20 | 73% | 0.10 (97%) | 0.19 (97%) | 0.25 (96%) | 11% / 9% |
| 11-20 | II (15) | 6 | 20-40 | 146% | 0.09 (97%) | 0.14 (96%) | 0.15 (88%) | 10% / 6% |
| 11-20 | III (25) | 2 | 40 | 162% | 0.11 (98%) | 0.20 (98%) | 0.23 (94%) | 12% / 8% |
| 21-30 | I (1) | 2 | 10 | 32% | 0.16 (99%) | 0.31 (98%) | 0.45 (99%) | 16% / 16% |
| 21-30 | I (5) | 6 | 10-40 | 74% | 0.14 (99%) | 0.27 (99%) | 0.39 (99%) | 15% / 14% |
| 21-30 | II (15) | 6 | 20-60 | 127% | 0.14 (99%) | 0.25 (98%) | 0.34 (98%) | 14% / 12% |
| 21-30 | III (25) | 6 | 40 | 129% | 0.14 (98%) | 0.25 (98%) | 0.35 (98%) | 15% / 12% |
| 21-30 | IV (35) | 2 | 50-60 | 162% | 0.13 (98%) | 0.23 (99%) | 0.26 (96%) | 14% / 10% |
| 31-40 | I (1) | 2 | 10 | 23% | 0.13 (98%) | 0.26 (96%) | 0.37 (96%) | 14% / 13% |
| 31-40 | I (5) | 6 | 10-40 | 54% | 0.35 (98%) | 0.66 (98%) | 0.89 (97%) | 36% / 31% |
| 31-40 | II (15) | 6 | 20-60 | 92% | 0.33 (98%) | 0.58 (97%) | 0.72 (96%) | 34% / 25% |
| 31-40 | III (25) | 6 | 40 | 93% | 0.33 (97%) | 0.61 (98%) | 0.73 (96%) | 34% / 26% |
| 31-40 | IV (35) | 6 | 50-60 | 124% | 0.33 (99%) | 0.54 (97%) | 0.61 (97%) | 34% / 22% |
| 31-40 | V (45) | 2 | 60-80 | 152% | 0.68 (100%) | 1.02 (98%) | 0.96 (96%) | 69% / 34% |
| 41-53 | I (1) | 1 | 10 | 4% | 0.00 (13%) | 0.01 (16%) | 0.01 (13%) | 2% / 2% |
| 41-53 | I (5) | 6 | 10-40 | 42% | 0.51 (84%) | 0.95 (85%) | 1.27 (83%) | 61% / 51% |
| 41-53 | II (15) | 6 | 20-60 | 67% | 0.48 (84%) | 0.82 (83%) | 1.00 (82%) | 57% / 41% |
| 41-53 | III (25) | 6 | 40 | 61% | 0.49 (84%) | 0.86 (83%) | 1.04 (82%) | 59% / 43% |
| 41-53 | IV (35) | 6 | 50-60 | 85% | 0.45 (84%) | 0.74 (83%) | 0.80 (81%) | 54% / 34% |
| 41-53 | V (45) | 6 | 60-80 | 109% | 0.44 (84%) | 0.62 (82%) | 0.62 (80%) | 53% / 27% |
| 41-53 | VI (50) | 3 | 100 | 175% | 0.46 (100%) | 0.53 (96%) | 0.42 (86%) | 47% / 17% |

### Engineering, best gear (Control 29, Craftsmanship 30, CP 37)

| Band | Tier (level) | Recipes | Penalty each | Extra progress at most slots | 1 slot | 2 slots | 3 slots | Avg quality at fewest / most slots |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | I (1) | 2 | 10 | 120% | 0.08 (98%) | 0.18 (98%) | 0.19 (98%) | 9% / 7% |
| 1-10 | I (5) | 6 | 10-20 | 138% | 0.22 (98%) | 0.43 (98%) | 0.56 (96%) | 23% / 20% |
| 1-10 | II (15) | 3 | 20-40 | 178% | 0.33 (100%) | 0.61 (100%) | 0.82 (100%) | 34% / 28% |
| 11-20 | I (1) | 3 | 10 | 52% | 0.29 (100%) | 0.55 (99%) | 0.85 (99%) | 30% / 29% |
| 11-20 | I (5) | 6 | 10-20 | 73% | 0.28 (99%) | 0.53 (99%) | 0.77 (99%) | 28% / 26% |
| 11-20 | II (15) | 6 | 20-40 | 146% | 0.27 (99%) | 0.49 (99%) | 0.65 (98%) | 28% / 23% |
| 11-20 | III (25) | 2 | 40 | 162% | 0.26 (100%) | 0.48 (100%) | 0.71 (98%) | 26% / 25% |
| 21-30 | I (1) | 2 | 10 | 32% | 0.31 (100%) | 0.60 (99%) | 0.90 (100%) | 31% / 30% |
| 21-30 | I (5) | 6 | 10-40 | 74% | 0.28 (99%) | 0.55 (99%) | 0.80 (99%) | 29% / 27% |
| 21-30 | II (15) | 6 | 20-60 | 127% | 0.27 (99%) | 0.52 (99%) | 0.73 (100%) | 28% / 25% |
| 21-30 | III (25) | 6 | 40 | 129% | 0.28 (99%) | 0.52 (99%) | 0.75 (99%) | 29% / 26% |
| 21-30 | IV (35) | 2 | 50-60 | 162% | 0.25 (99%) | 0.47 (98%) | 0.63 (100%) | 26% / 22% |
| 31-40 | I (1) | 2 | 10 | 23% | 0.25 (100%) | 0.46 (99%) | 0.68 (100%) | 25% / 23% |
| 31-40 | I (5) | 6 | 10-40 | 54% | 0.48 (99%) | 0.94 (99%) | 1.37 (99%) | 49% / 47% |
| 31-40 | II (15) | 6 | 20-60 | 92% | 0.48 (99%) | 0.90 (99%) | 1.28 (99%) | 49% / 44% |
| 31-40 | III (25) | 6 | 40 | 93% | 0.47 (99%) | 0.91 (99%) | 1.25 (99%) | 48% / 42% |
| 31-40 | IV (35) | 6 | 50-60 | 124% | 0.48 (99%) | 0.88 (99%) | 1.17 (99%) | 48% / 40% |
| 31-40 | V (45) | 2 | 60-80 | 152% | 0.94 (99%) | 1.66 (100%) | 2.10 (99%) | 96% / 71% |
| 41-53 | I (1) | 1 | 10 | 4% | 0.04 (61%) | 0.08 (66%) | 0.09 (60%) | 6% / 6% |
| 41-53 | I (5) | 6 | 10-40 | 42% | 0.73 (93%) | 1.37 (91%) | 1.91 (92%) | 79% / 70% |
| 41-53 | II (15) | 6 | 20-60 | 67% | 0.71 (91%) | 1.27 (90%) | 1.76 (88%) | 78% / 67% |
| 41-53 | III (25) | 6 | 40 | 61% | 0.72 (91%) | 1.32 (90%) | 1.74 (88%) | 79% / 66% |
| 41-53 | IV (35) | 6 | 50-60 | 85% | 0.70 (92%) | 1.19 (88%) | 1.57 (86%) | 77% / 62% |
| 41-53 | V (45) | 6 | 60-80 | 109% | 0.67 (92%) | 1.10 (89%) | 1.36 (83%) | 74% / 55% |
| 41-53 | VI (50) | 3 | 100 | 175% | 0.72 (99%) | 1.21 (99%) | 1.19 (97%) | 73% / 41% |

## Limitations

- Enhancement items differ in progress penalty within a level; the figures are for the lower-median item. A harder item transfers less, and the sweep shows how much the level matters.
- Every slot holds the same item, and every enhancement is assumed to be one the recipe accepts. Players mix enhancements, and an enhancement of the wrong kind is refused by the old window.
- The crafting policies are not real players. The lookahead is one step of policy improvement over the rotation players report, so it is the stricter yardstick; neither plays Steady Hand, Muscle Memory or Veneration the way a skilled player might in a long craft.
- Recipes are an even sample of each band, weighted equally; a recipe the sample skipped can differ. Recipe levels fix the progress, quality and durability the old window asks for.
- No attributes, food, status effects, blueprints (their bonus slots and fees), first-craft bonus or quick craft are modeled, and the client was not run.
- Failed crafts transfer nothing and also lose each enhancement at 65%; only the transfer is counted here, the loss belongs to the economy-parity gate.
