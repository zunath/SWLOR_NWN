# Crafting revamp: legacy baseline of the old craft minigame

The old minigame (`CraftViewModel.cs`) played at every recipe level, gear level and policy, with today's rules. It is the yardstick the
new minigames' economy-parity gate compares against. No new minigame is measured here.

## Inputs

- Command: `SWLOR.CraftSim baseline --section crafting --runs 500 --seed 1 --out design/testing/crafting-revamp/legacy-crafting-baseline.md`
- Seed 1; 500 crafts per recipe level, skill, gear level and policy. Each craft's dice come from its own stream of the seed, so a run repeats exactly.
- Recipes: every active recipe in the game server's recipe definitions, grouped by level. Level, skill and component lines are read from the definitions; durability, progress and quality come from `RecipeLevelChart` through `Craft.GetRecipeLevelDetail`.
  Smithery 777 (levels 1-52); Fabrication 456 (levels 1-52); Agriculture 319 (levels 1-52); Engineering 1786 (levels 1-53); Espionage 15 (levels 3-50).
- Crafter rank: the recipe's level, at most 50 (the rank cap), so a crafter works at their own level. The sensitivity table also plays -3 levels and +5 levels (at least the recipe's required rank, level - 3).
- Gear levels: none, the middle tier and the best tier of each crafting skill's gear (next section). The meters use gear only: no food or status bonuses.
- Policies:
  - **Dominant rotation**: Waste Not, then Basic Touch repeated, repaired with Master's Mend, then synthesis to finish. It keeps back the durability the finish needs, counting the repairs its remaining CP can buy, and finishes with the affordable synthesis of highest expected progress while keeping one repair affordable. It never uses Steady Hand, Muscle Memory, Veneration, Standard Touch or Precise Touch.
  - **Lookahead**: at every step it tries each usable action against 8 sampled futures (the same dice for every candidate), finishes each future with the dominant rotation, and takes the action with the best mean score (0 for a failed craft, 0.5 plus half the quality fraction for a success). It is one step of policy improvement over the rotation, not an optimal player.
- Material use: a success consumes every committed component stack once, and each failure consumes each stack at 65% (`CraftViewModel.cs:1601-1641`), so stacks per success = stacks x (1 + 0.65 x (1 - p) / p) for a success chance p. A stack is one distinct component line of the recipe; enhancements are not committed. 83 of 3353 recipes make more than one item per craft, so their stacks are shared across every item a success makes.

Rules ported, each checked against the source by `LegacySourceTests`:

| Rule | Source |
| --- | --- |
| Craft action unlock ranks | `CraftViewModel.cs:39-48` |
| Craft points | `CraftViewModel.cs:446-454` |
| Durability from the level chart | `CraftViewModel.cs:456-457` |
| Maximum progress modifier | `CraftViewModel.cs:460-478` |
| Maximum quality from the level chart | `CraftViewModel.cs:481-482` |
| Recipe level chart | `RecipeLevelChart.cs:13-99` |
| Progress per synthesis | `CraftViewModel.cs:1354-1367` |
| Quality per touch | `CraftViewModel.cs:1369-1384` |
| Action resolution order | `CraftViewModel.cs:1672-1725` |
| The 11 craft actions | `CraftViewModel.cs:1727-1864` |
| Flags and meters reset between crafts | `CraftViewModel.cs:1225-1232` |
| Components and enhancements lost on failure | `CraftViewModel.cs:1601-1641` |
| Recipe rank requirement | `Craft.cs:327-331` |
| Crafting gear stats | `EquipmentStats.cs:929-1075` |

## Gear levels

A skill's gear is every item blueprint with that skill's Control (item property 88), Craftsmanship (89) or CPBonus (115) that a Smithery recipe makes. Within a body slot the recipes are ordered by level, so the Nth is tier N, and a gear level is one item of the same tier in each of armor, helmet, necklace, belt, boots, bracer and cloak plus two rings. "Mid" is tier 3 and "best" is tier 5, of five. Max CP is gear CP + 0.75 x rank (+31 from rank 25) at the shown rank.

| Skill | Gear level | Tier | Control | Craftsmanship | Gear CP | Max CP at rank 25 | Max CP at rank 50 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Smithery | no gear | - | 0 | 0 | 0 | 49 | 68 |
| Smithery | mid gear | 3 | 13 | 12 | 23 | 72 | 91 |
| Smithery | best gear | 5 | 29 | 30 | 37 | 86 | 105 |
| Engineering | no gear | - | 0 | 0 | 0 | 49 | 68 |
| Engineering | mid gear | 3 | 13 | 12 | 20 | 69 | 88 |
| Engineering | best gear | 5 | 29 | 30 | 37 | 86 | 105 |
| Fabrication | no gear | - | 0 | 0 | 0 | 49 | 68 |
| Fabrication | mid gear | 3 | 13 | 12 | 26 | 75 | 94 |
| Fabrication | best gear | 5 | 29 | 30 | 37 | 86 | 105 |
| Agriculture | no gear | - | 0 | 0 | 0 | 49 | 68 |
| Agriculture | mid gear | 3 | 13 | 12 | 20 | 69 | 88 |
| Agriculture | best gear | 5 | 29 | 30 | 37 | 86 | 105 |
| Espionage | no gear | - | 0 | 0 | 0 | 49 | 68 |

## Summary across the four gear skills

Smithery, Engineering, Fabrication and Agriculture pooled, each skill's recipes counted once. The columns are the ones of the per-skill tables below.

| Levels | Gear | Dominant rotation: success | quality | full | actions | stacks | Lookahead: success | quality | full | actions | stacks |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | no gear | 94% | 1% | 0% | 5.4 | 2.6 | 97% | 1% | 0% | 5.6 | 2.5 |
| 1-10 | mid gear | 97% | 11% | 0% | 7.7 | 2.5 | 97% | 11% | 0% | 7.8 | 2.5 |
| 1-10 | best gear | 99% | 25% | 0% | 8.6 | 2.5 | 97% | 25% | 0% | 9.0 | 2.5 |
| 11-20 | no gear | 91% | 1% | 0% | 8.1 | 2.6 | 94% | 2% | 0% | 8.0 | 2.6 |
| 11-20 | mid gear | 99% | 12% | 0% | 12.4 | 2.5 | 98% | 13% | 0% | 10.7 | 2.5 |
| 11-20 | best gear | 100% | 30% | 0% | 15.2 | 2.5 | 99% | 30% | 0% | 14.6 | 2.5 |
| 21-30 | no gear | 99% | 5% | 0% | 15.3 | 2.4 | 96% | 8% | 0% | 10.9 | 2.4 |
| 21-30 | mid gear | 98% | 16% | 0% | 20.8 | 2.4 | 99% | 16% | 0% | 16.7 | 2.4 |
| 21-30 | best gear | 98% | 31% | 0% | 23.4 | 2.4 | 99% | 29% | 0% | 22.1 | 2.4 |
| 31-40 | no gear | 93% | 20% | 0% | 19.7 | 2.7 | 96% | 21% | 0% | 14.3 | 2.6 |
| 31-40 | mid gear | 94% | 36% | 2% | 23.6 | 2.7 | 98% | 34% | 1% | 19.2 | 2.6 |
| 31-40 | best gear | 100% | 47% | 27% | 27.3 | 2.6 | 99% | 46% | 22% | 24.7 | 2.6 |
| 41-53 | no gear | 51% | 51% | 0% | 18.7 | 3.2 (16% never) | 84% | 42% | 0% | 17.5 | 2.3 (16% never) |
| 41-53 | mid gear | 78% | 77% | 1% | 25.5 | 2.4 (16% never) | 86% | 66% | 0% | 24.1 | 2.8 (1% never) |
| 41-53 | best gear | 79% | 96% | 51% | 28.2 | 2.4 (16% never) | 93% | 82% | 23% | 28.2 | 2.8 |

## Results by skill

Crafter at the recipe's level. Success is the share of crafts that finish the item. Quality is the mean final quality of successful crafts (maximum quality is 100%); Full is the share of those that reached it. Actions counts every craft, failed or not. Stacks is component stacks consumed per item made (a success makes the recipe's quantity) when the band's recipes are attempted equally often; recipes that never succeeded in these runs are left out of it, and the percentage after it is their share of the band.

### Smithery

| Levels | Recipes | Gear | Dominant rotation: success | quality | full | actions | stacks | Lookahead: success | quality | full | actions | stacks |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | 119 | no gear | 94% | 1% | 0% | 5.6 | 2.8 | 97% | 1% | 0% | 5.9 | 2.7 |
| 1-10 | 119 | mid gear | 97% | 13% | 0% | 8.8 | 2.7 | 97% | 12% | 0% | 8.9 | 2.7 |
| 1-10 | 119 | best gear | 99% | 30% | 0% | 9.9 | 2.7 | 98% | 29% | 0% | 10.5 | 2.7 |
| 11-20 | 142 | no gear | 90% | 1% | 0% | 7.9 | 2.9 | 94% | 2% | 0% | 8.1 | 2.8 |
| 11-20 | 142 | mid gear | 98% | 13% | 0% | 13.0 | 2.7 | 98% | 13% | 0% | 11.0 | 2.7 |
| 11-20 | 142 | best gear | 100% | 30% | 0% | 15.1 | 2.7 | 99% | 30% | 0% | 14.6 | 2.7 |
| 21-30 | 195 | no gear | 99% | 5% | 0% | 14.5 | 2.9 | 97% | 7% | 0% | 10.4 | 2.9 |
| 21-30 | 195 | mid gear | 98% | 16% | 0% | 20.5 | 2.9 | 99% | 15% | 0% | 16.5 | 2.9 |
| 21-30 | 195 | best gear | 98% | 30% | 0% | 22.6 | 2.9 | 99% | 29% | 0% | 21.4 | 2.9 |
| 31-40 | 134 | no gear | 94% | 19% | 0% | 19.6 | 2.9 | 97% | 21% | 0% | 14.4 | 2.8 |
| 31-40 | 134 | mid gear | 97% | 35% | 5% | 24.1 | 2.8 | 99% | 34% | 1% | 19.9 | 2.8 |
| 31-40 | 134 | best gear | 100% | 46% | 26% | 27.0 | 2.8 | 99% | 45% | 21% | 25.0 | 2.8 |
| 41-53 | 187 | no gear | 44% | 52% | 0% | 18.1 | 3.9 (25% never) | 74% | 43% | 0% | 16.7 | 2.7 (25% never) |
| 41-53 | 187 | mid gear | 69% | 76% | 1% | 24.8 | 2.9 (25% never) | 79% | 64% | 0% | 23.7 | 4.3 |
| 41-53 | 187 | best gear | 71% | 94% | 40% | 27.8 | 2.8 (25% never) | 90% | 75% | 19% | 28.0 | 4.1 |

### Fabrication

| Levels | Recipes | Gear | Dominant rotation: success | quality | full | actions | stacks | Lookahead: success | quality | full | actions | stacks |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | 66 | no gear | 94% | 1% | 0% | 4.8 | 2.3 | 97% | 1% | 0% | 5.1 | 2.2 |
| 1-10 | 66 | mid gear | 98% | 10% | 0% | 7.0 | 2.2 | 97% | 10% | 0% | 7.2 | 2.2 |
| 1-10 | 66 | best gear | 99% | 21% | 0% | 7.2 | 2.2 | 97% | 21% | 0% | 7.6 | 2.2 |
| 11-20 | 84 | no gear | 86% | 1% | 0% | 7.9 | 2.4 | 94% | 1% | 0% | 8.3 | 2.3 |
| 11-20 | 84 | mid gear | 100% | 14% | 0% | 13.9 | 2.2 | 98% | 14% | 0% | 12.1 | 2.2 |
| 11-20 | 84 | best gear | 100% | 32% | 0% | 14.9 | 2.2 | 99% | 31% | 0% | 14.4 | 2.2 |
| 21-30 | 108 | no gear | 99% | 5% | 0% | 14.8 | 2.3 | 97% | 8% | 0% | 10.6 | 2.4 |
| 21-30 | 108 | mid gear | 99% | 16% | 0% | 21.5 | 2.3 | 99% | 17% | 0% | 17.3 | 2.3 |
| 21-30 | 108 | best gear | 99% | 31% | 0% | 23.0 | 2.3 | 99% | 30% | 0% | 21.8 | 2.3 |
| 31-40 | 102 | no gear | 95% | 10% | 0% | 19.6 | 2.4 | 95% | 13% | 0% | 13.1 | 2.4 |
| 31-40 | 102 | mid gear | 98% | 21% | 4% | 25.1 | 2.3 | 98% | 22% | 1% | 18.9 | 2.3 |
| 31-40 | 102 | best gear | 100% | 33% | 9% | 27.4 | 2.3 | 99% | 32% | 7% | 24.3 | 2.3 |
| 41-53 | 96 | no gear | 60% | 52% | 0% | 19.5 | 3.1 (4% never) | 95% | 42% | 0% | 18.6 | 2.3 (4% never) |
| 41-53 | 96 | mid gear | 85% | 83% | 5% | 27.0 | 2.5 (4% never) | 96% | 71% | 1% | 26.1 | 2.5 |
| 41-53 | 96 | best gear | 89% | 97% | 62% | 28.5 | 2.3 (6% never) | 98% | 88% | 30% | 28.6 | 2.5 |

### Agriculture

| Levels | Recipes | Gear | Dominant rotation: success | quality | full | actions | stacks | Lookahead: success | quality | full | actions | stacks |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | 58 | no gear | 95% | 1% | 0% | 4.4 | 1.7 | 97% | 1% | 0% | 4.6 | 1.7 |
| 1-10 | 58 | mid gear | 98% | 7% | 0% | 5.5 | 1.7 | 97% | 7% | 0% | 5.5 | 1.7 |
| 1-10 | 58 | best gear | 99% | 17% | 0% | 5.6 | 1.7 | 98% | 17% | 0% | 5.9 | 1.7 |
| 11-20 | 59 | no gear | 88% | 1% | 0% | 8.1 | 1.8 | 93% | 1% | 0% | 8.4 | 1.7 |
| 11-20 | 59 | mid gear | 98% | 12% | 0% | 12.0 | 1.7 | 98% | 12% | 0% | 10.7 | 1.7 |
| 11-20 | 59 | best gear | 100% | 31% | 0% | 15.0 | 1.7 | 99% | 31% | 0% | 14.5 | 1.7 |
| 21-30 | 65 | no gear | 99% | 5% | 0% | 13.6 | 1.8 | 96% | 8% | 0% | 9.9 | 1.8 |
| 21-30 | 65 | mid gear | 98% | 15% | 0% | 19.0 | 1.8 | 99% | 15% | 0% | 15.4 | 1.8 |
| 21-30 | 65 | best gear | 99% | 30% | 0% | 21.9 | 1.8 | 99% | 30% | 0% | 20.9 | 1.8 |
| 31-40 | 61 | no gear | 95% | 8% | 0% | 19.6 | 1.8 | 96% | 11% | 0% | 12.8 | 1.8 |
| 31-40 | 61 | mid gear | 92% | 17% | 0% | 22.0 | 1.8 | 98% | 18% | 0% | 17.4 | 1.8 |
| 31-40 | 61 | best gear | 100% | 30% | 5% | 27.6 | 1.7 | 99% | 29% | 4% | 24.0 | 1.7 |
| 41-53 | 76 | no gear | 53% | 52% | 0% | 18.0 | 2.2 (22% never) | 77% | 43% | 0% | 16.5 | 1.7 (22% never) |
| 41-53 | 76 | mid gear | 73% | 80% | 0% | 25.0 | 1.7 (22% never) | 81% | 66% | 0% | 23.3 | 2.0 |
| 41-53 | 76 | best gear | 74% | 98% | 72% | 27.7 | 1.7 (22% never) | 92% | 79% | 30% | 28.0 | 1.9 |

### Engineering

| Levels | Recipes | Gear | Dominant rotation: success | quality | full | actions | stacks | Lookahead: success | quality | full | actions | stacks |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | 307 | no gear | 93% | 1% | 0% | 5.7 | 2.8 | 97% | 1% | 0% | 5.9 | 2.7 |
| 1-10 | 307 | mid gear | 97% | 12% | 0% | 7.9 | 2.7 | 97% | 11% | 0% | 7.9 | 2.7 |
| 1-10 | 307 | best gear | 98% | 26% | 0% | 9.0 | 2.7 | 97% | 26% | 0% | 9.3 | 2.7 |
| 11-20 | 367 | no gear | 94% | 1% | 0% | 8.2 | 2.8 | 95% | 3% | 0% | 7.8 | 2.7 |
| 11-20 | 367 | mid gear | 99% | 11% | 0% | 11.9 | 2.7 | 98% | 12% | 0% | 10.2 | 2.7 |
| 11-20 | 367 | best gear | 100% | 29% | 0% | 15.3 | 2.6 | 99% | 29% | 0% | 14.7 | 2.7 |
| 21-30 | 359 | no gear | 99% | 5% | 0% | 16.2 | 2.3 | 96% | 8% | 0% | 11.4 | 2.4 |
| 21-30 | 359 | mid gear | 97% | 16% | 0% | 21.0 | 2.4 | 99% | 16% | 0% | 16.9 | 2.3 |
| 21-30 | 359 | best gear | 98% | 31% | 0% | 24.2 | 2.3 | 99% | 29% | 0% | 22.8 | 2.3 |
| 31-40 | 370 | no gear | 92% | 25% | 0% | 19.8 | 2.9 | 97% | 25% | 0% | 14.8 | 2.8 |
| 31-40 | 370 | mid gear | 93% | 43% | 0% | 23.3 | 2.9 | 98% | 40% | 1% | 19.4 | 2.8 |
| 31-40 | 370 | best gear | 100% | 54% | 36% | 27.3 | 2.7 | 99% | 52% | 29% | 24.8 | 2.7 |
| 41-53 | 383 | no gear | 52% | 51% | 0% | 19.0 | 3.2 (13% never) | 87% | 41% | 0% | 17.8 | 2.3 (13% never) |
| 41-53 | 383 | mid gear | 82% | 76% | 0% | 25.6 | 2.4 (13% never) | 89% | 66% | 0% | 24.0 | 2.5 (3% never) |
| 41-53 | 383 | best gear | 82% | 96% | 49% | 28.5 | 2.4 (13% never) | 93% | 84% | 22% | 28.2 | 2.4 |

### Espionage

| Levels | Recipes | Gear | Dominant rotation: success | quality | full | actions | stacks | Lookahead: success | quality | full | actions | stacks |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1-10 | 3 | no gear | 98% | 1% | 0% | 3.2 | 0.5 | 98% | 1% | 0% | 3.3 | 0.5 |
| 11-20 | 3 | no gear | 97% | 0% | 0% | 9.1 | 0.6 | 92% | 1% | 0% | 9.0 | 0.6 |
| 21-30 | 3 | no gear | 100% | 6% | 0% | 16.6 | 0.5 | 97% | 9% | 0% | 11.9 | 0.6 |
| 31-40 | 2 | no gear | 99% | 5% | 0% | 19.4 | 0.7 | 95% | 10% | 0% | 11.2 | 0.7 |
| 41-53 | 4 | no gear | 56% | 52% | 0% | 19.8 | 0.7 | 100% | 41% | 0% | 18.8 | 0.5 |

## Rank sensitivity

The dominant rotation with the crafter's rank at the recipe's level plus the offset shown (kept between the recipe's required rank and 50). Each level below the recipe raises the progress needed by 25% and cuts progress and quality per action by 5%. Each level above raises progress per action by 5% and, in the old code, also raises the progress needed by 5%, up to 25%.

| Skill | Levels | Gear | -3: success | quality | actions | 0: success | quality | actions | +5: success | quality | actions |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Smithery | 1-10 | no gear | 14% | 0% | 5.8 | 94% | 1% | 5.6 | 97% | 2% | 7.1 |
| Smithery | 1-10 | mid gear | 80% | 0% | 5.2 | 97% | 13% | 8.8 | 97% | 21% | 11.6 |
| Smithery | 1-10 | best gear | 97% | 4% | 4.4 | 99% | 30% | 9.9 | 99% | 48% | 13.8 |
| Smithery | 11-20 | no gear | 0% | 0% | 8.9 | 90% | 1% | 7.9 | 99% | 4% | 9.9 |
| Smithery | 11-20 | mid gear | 74% | 5% | 11.9 | 98% | 13% | 13.0 | 99% | 17% | 15.3 |
| Smithery | 11-20 | best gear | 92% | 20% | 15.5 | 100% | 30% | 15.1 | 100% | 36% | 17.6 |
| Smithery | 21-30 | no gear | 64% | 2% | 13.1 | 99% | 5% | 14.5 | 96% | 7% | 18.0 |
| Smithery | 21-30 | mid gear | 88% | 9% | 18.1 | 98% | 16% | 20.5 | 99% | 19% | 23.7 |
| Smithery | 21-30 | best gear | 93% | 20% | 21.1 | 98% | 30% | 22.6 | 99% | 36% | 26.1 |
| Smithery | 31-40 | no gear | 51% | 3% | 17.8 | 94% | 19% | 19.6 | 92% | 38% | 19.9 |
| Smithery | 31-40 | mid gear | 49% | 10% | 24.2 | 97% | 35% | 24.1 | 97% | 62% | 26.0 |
| Smithery | 31-40 | best gear | 71% | 20% | 27.2 | 100% | 46% | 27.0 | 100% | 70% | 26.6 |
| Smithery | 41-53 | no gear | 19% | 21% | 16.3 | 44% | 52% | 18.1 | 38% | 54% | 18.2 |
| Smithery | 41-53 | mid gear | 22% | 42% | 22.3 | 69% | 76% | 24.8 | 70% | 77% | 25.0 |
| Smithery | 41-53 | best gear | 24% | 61% | 26.4 | 71% | 94% | 27.8 | 71% | 94% | 27.8 |
| Fabrication | 1-10 | no gear | 28% | 0% | 5.6 | 94% | 1% | 4.8 | 97% | 3% | 6.5 |
| Fabrication | 1-10 | mid gear | 88% | 0% | 4.6 | 98% | 10% | 7.0 | 99% | 23% | 11.5 |
| Fabrication | 1-10 | best gear | 99% | 2% | 3.6 | 99% | 21% | 7.2 | 99% | 50% | 12.8 |
| Fabrication | 11-20 | no gear | 0% | 0% | 7.9 | 86% | 1% | 7.9 | 99% | 3% | 8.3 |
| Fabrication | 11-20 | mid gear | 62% | 6% | 11.6 | 100% | 14% | 13.9 | 99% | 18% | 14.6 |
| Fabrication | 11-20 | best gear | 94% | 21% | 15.0 | 100% | 32% | 14.9 | 100% | 34% | 15.9 |
| Fabrication | 21-30 | no gear | 64% | 2% | 13.1 | 99% | 5% | 14.8 | 96% | 7% | 18.0 |
| Fabrication | 21-30 | mid gear | 90% | 9% | 18.2 | 99% | 16% | 21.5 | 100% | 19% | 23.9 |
| Fabrication | 21-30 | best gear | 93% | 19% | 20.9 | 99% | 31% | 23.0 | 99% | 36% | 25.9 |
| Fabrication | 31-40 | no gear | 48% | 3% | 17.8 | 95% | 10% | 19.6 | 91% | 37% | 20.0 |
| Fabrication | 31-40 | mid gear | 48% | 10% | 24.1 | 98% | 21% | 25.1 | 97% | 60% | 25.9 |
| Fabrication | 31-40 | best gear | 77% | 20% | 27.0 | 100% | 33% | 27.4 | 100% | 68% | 26.2 |
| Fabrication | 41-53 | no gear | 20% | 23% | 17.4 | 60% | 52% | 19.5 | 49% | 55% | 19.8 |
| Fabrication | 41-53 | mid gear | 22% | 47% | 23.8 | 85% | 83% | 27.0 | 85% | 87% | 28.4 |
| Fabrication | 41-53 | best gear | 33% | 64% | 28.0 | 89% | 97% | 28.5 | 88% | 97% | 28.3 |
| Agriculture | 1-10 | no gear | 34% | 0% | 5.5 | 95% | 1% | 4.4 | 98% | 3% | 6.1 |
| Agriculture | 1-10 | mid gear | 92% | 0% | 4.4 | 98% | 7% | 5.5 | 98% | 22% | 10.0 |
| Agriculture | 1-10 | best gear | 99% | 1% | 3.2 | 99% | 17% | 5.6 | 100% | 52% | 12.5 |
| Agriculture | 11-20 | no gear | 0% | 0% | 8.0 | 88% | 1% | 8.1 | 99% | 3% | 8.1 |
| Agriculture | 11-20 | mid gear | 80% | 4% | 11.5 | 98% | 12% | 12.0 | 98% | 15% | 12.7 |
| Agriculture | 11-20 | best gear | 94% | 21% | 15.2 | 100% | 31% | 15.0 | 100% | 34% | 15.6 |
| Agriculture | 21-30 | no gear | 68% | 1% | 12.2 | 99% | 5% | 13.6 | 98% | 7% | 17.7 |
| Agriculture | 21-30 | mid gear | 84% | 7% | 16.0 | 98% | 15% | 19.0 | 98% | 19% | 22.8 |
| Agriculture | 21-30 | best gear | 94% | 19% | 19.9 | 99% | 30% | 21.9 | 99% | 37% | 25.6 |
| Agriculture | 31-40 | no gear | 47% | 3% | 17.9 | 95% | 8% | 19.6 | 92% | 40% | 20.0 |
| Agriculture | 31-40 | mid gear | 34% | 9% | 21.1 | 92% | 17% | 22.0 | 98% | 61% | 24.0 |
| Agriculture | 31-40 | best gear | 77% | 20% | 26.9 | 100% | 30% | 27.6 | 100% | 71% | 25.9 |
| Agriculture | 41-53 | no gear | 15% | 22% | 16.5 | 53% | 52% | 18.0 | 42% | 56% | 18.3 |
| Agriculture | 41-53 | mid gear | 29% | 41% | 21.9 | 73% | 80% | 25.0 | 71% | 82% | 25.3 |
| Agriculture | 41-53 | best gear | 28% | 64% | 27.1 | 74% | 98% | 27.7 | 73% | 98% | 27.6 |
| Engineering | 1-10 | no gear | 12% | 0% | 5.8 | 93% | 1% | 5.7 | 97% | 2% | 7.0 |
| Engineering | 1-10 | mid gear | 79% | 0% | 5.1 | 97% | 12% | 7.9 | 98% | 20% | 10.7 |
| Engineering | 1-10 | best gear | 98% | 3% | 4.1 | 98% | 26% | 9.0 | 99% | 48% | 13.7 |
| Engineering | 11-20 | no gear | 0% | 0% | 9.6 | 94% | 1% | 8.2 | 99% | 5% | 10.8 |
| Engineering | 11-20 | mid gear | 85% | 4% | 12.2 | 99% | 11% | 11.9 | 99% | 18% | 15.7 |
| Engineering | 11-20 | best gear | 90% | 20% | 15.9 | 100% | 29% | 15.3 | 100% | 36% | 18.9 |
| Engineering | 21-30 | no gear | 60% | 2% | 14.2 | 99% | 5% | 16.2 | 95% | 7% | 18.5 |
| Engineering | 21-30 | mid gear | 84% | 8% | 18.5 | 97% | 16% | 21.0 | 98% | 17% | 23.4 |
| Engineering | 21-30 | best gear | 92% | 20% | 22.4 | 98% | 31% | 24.2 | 99% | 34% | 26.5 |
| Engineering | 31-40 | no gear | 55% | 3% | 17.6 | 92% | 25% | 19.8 | 92% | 53% | 19.7 |
| Engineering | 31-40 | mid gear | 41% | 8% | 22.3 | 93% | 43% | 23.3 | 99% | 82% | 25.3 |
| Engineering | 31-40 | best gear | 59% | 19% | 27.2 | 100% | 54% | 27.3 | 100% | 89% | 25.5 |
| Engineering | 41-53 | no gear | 21% | 24% | 17.1 | 52% | 51% | 19.0 | 43% | 55% | 19.3 |
| Engineering | 41-53 | mid gear | 29% | 44% | 22.7 | 82% | 76% | 25.6 | 81% | 77% | 25.8 |
| Engineering | 41-53 | best gear | 30% | 67% | 27.6 | 82% | 96% | 28.5 | 82% | 95% | 28.3 |
| Espionage | 1-10 | no gear | 59% | 0% | 5.6 | 98% | 1% | 3.2 | 96% | 4% | 5.7 |
| Espionage | 11-20 | no gear | 0% | 0% | 8.3 | 97% | 0% | 9.1 | 99% | 3% | 7.7 |
| Espionage | 21-30 | no gear | 81% | 2% | 13.6 | 100% | 6% | 16.6 | 97% | 7% | 18.0 |
| Espionage | 31-40 | no gear | 42% | 3% | 17.4 | 99% | 5% | 19.4 | 89% | 66% | 19.7 |
| Espionage | 41-53 | no gear | 20% | 28% | 17.9 | 56% | 52% | 19.8 | 50% | 56% | 20.2 |

## Limitations

- Only the two policies above are played. Neither is optimal, so a result is evidence of what these plays reach, not a ceiling. The lookahead samples 8 futures per action, so its choices carry sampling noise, and its score weighs quality as well as success, so it can accept a slightly lower success rate for more quality. Espionage has no crafting gear, so it plays without any.
- A craft is played at one rank. Real crafters pass through ranks, and rank thresholds (5, 8, 10, 15, 20, 25, 30, 35, 40) change the actions available.
- Gear is the sets derived above. Mixed or partial sets, food and other status bonuses to Control, Craftsmanship and CP are not played.
- Components are counted as one stack per component line. The real count depends on how the player's inventory splits each line (`AggregateComponents`), and a stack can be partly committed. Enhancements, blueprints and licensed runs are not played, and the 65% loss on failure also applies to selected enhancements.
- Quality is the final quality fraction. The old minigame uses it for enhancement transfer chance, food duration, XP and vendor value, none of which are valued here.
- Recipes at one level share a result, because the old minigame reads only the recipe's level and skill. Components only weight the material columns.
- Max progress is computed as the old code does, including its sign errors (the 5% bonus above the recipe raises the progress needed), because the baseline must match today's play.
