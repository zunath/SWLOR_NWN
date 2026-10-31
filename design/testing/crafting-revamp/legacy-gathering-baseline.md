# Crafting revamp: legacy baseline of gathering, refining and fishing

Today's harvesting, scavenging, refining and fishing-catch rules, the yardstick for the new gathering and fishing minigames' economy-parity gate.
The rules depend on Might (harvest bonus item), Perception (scavenge attempts and success) and Willpower (catch chance). Until the median attributes of
active characters are measured, the tables sweep attribute modifiers -1 to +5 (a Willpower score of 10 + 2 x modifier).

## Inputs

- Command: `SWLOR.CraftSim baseline --section gathering --seed 1 --out design/testing/crafting-revamp/legacy-gathering-baseline.md`
- Seed 1. Every table gives the exact expected value from the ported rule; harvesting and scavenging also play 20000 nodes or sites per row from the seed as a cross-check.
- XP uses the server's own `Skill.GetDeltaXP`. Item counts are loot-table rolls, not priced.

Rules ported, each checked against the source by `LegacySourceTests`:

| Rule | Source |
| --- | --- |
| Harvests per node | `ProspectingNode.cs:67` |
| Harvest XP | `GatheringLevels.cs:20-23` |
| Scavenge chances at the reference Perception modifier | `ScavengeQuickSearch.cs:23-34` |
| Scavenge second attempt | `ScavengeQuickSearch.cs:82-84` |
| Scavenge success roll and DC | `ScavengeQuickSearch.cs:86-93` |
| Scavenge XP level | `ScavengePoint.cs:93` |
| Ore tiers and XP per unit | `RefineryViewModel.cs:35-52` |
| Refinery batch size | `RefineryViewModel.cs:61` |
| Power cores per batch | `RefineryViewModel.cs:173-174` |
| Refining XP | `RefineryViewModel.cs:356-360` |
| Fishing XP per bite | `FishingFightLifecycle.cs:58-68` |

Harvesting now runs in Prospecting: a node keeps its d4 pool (`ProspectingNode`) and a unit pays `GetDeltaXP(vein level - rank)` (`GatheringLevels`). Quick Harvest plays today's loop at a Might modifier of 0. The harvest bonus item (`d100 <= 25 x Might modifier`) was in `HarvesterItemDefinition.cs` until Prospecting replaced it. It is ported here from that file at commit 64b712fc8, where it was checked against the source, and is no longer checked. Refining still pays each ore's flat XP per unit (25 to 150 by ore tier), scaled by the rank: `GetDeltaXP(ore level - rank) / GetDeltaXP(0)`, so it is the flat XP at the ore's level (`GatheringLevels.RefiningXp`).

The fishing catch roll (`40 + 10 x (rank - fish level) + 2 x Willpower`, kept from 0 to 95) was in `FinishFishing` until the Line fight replaced it. It is ported here from `Service/Fishing.cs` at commit d5f1c0466, where it was checked against the source, and is no longer checked.

The server's XP chart (`GetDeltaXP`), by the activity's level minus the rank:

| Level - rank | +7 | +6 | +5 | +4 | +3 | +2 | +1 | 0 | -1 | -2 | -3 | -4 | -5 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| XP | 1200 | 1200 | 1050 | 976 | 900 | 750 | 676 | 600 | 450 | 300 | 150 | 76 | 0 |

## Harvesting

A node holds d4 harvests, set by its first harvest. Each harvest spends one harvester charge for one loot-table item, plus a free second item at d100 <= 25 x Might modifier that does not reduce the count.

| Might modifier | Bonus item chance | Harvests (charges) per node | Items per node | Items per node, played | Items per charge |
| --- | --- | --- | --- | --- | --- |
| -1 | 0% | 2.50 | 2.50 | 2.50 | 1.00 |
| 0 | 0% | 2.50 | 2.50 | 2.51 | 1.00 |
| +1 | 25% | 2.50 | 3.12 | 3.13 | 1.25 |
| +2 | 50% | 2.50 | 3.75 | 3.76 | 1.50 |
| +3 | 75% | 2.50 | 4.38 | 4.39 | 1.75 |
| +4 | 100% | 2.50 | 5.00 | 5.00 | 2.00 |
| +5 | 100% | 2.50 | 5.00 | 5.00 | 2.00 |

XP per item gathered, by node level and Gathering rank (`GetDeltaXP(10 x (node level - 1) + 5 - rank)`). A harvest pays this once per item gathered:

| Node level (vein level) | Rank 0 | Rank 5 | Rank 10 | Rank 15 | Rank 20 | Rank 25 | Rank 30 | Rank 35 | Rank 40 | Rank 45 | Rank 50 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 (5) | 1050 | 600 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 2 (15) | 1200 | 1200 | 1050 | 600 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 3 (25) | 1200 | 1200 | 1200 | 1200 | 1050 | 600 | 0 | 0 | 0 | 0 | 0 |
| 4 (35) | 1200 | 1200 | 1200 | 1200 | 1200 | 1200 | 1050 | 600 | 0 | 0 | 0 |
| 5 (45) | 1200 | 1200 | 1200 | 1200 | 1200 | 1200 | 1200 | 1200 | 1050 | 600 | 0 |

## Scavenging

One attempt, plus a second at Hard Look x 10 + Perception modifier x 5 percent. An attempt succeeds on d20 + Perception modifier against a DC of 6, then 6 + d3 for the second attempt. A site can be searched once.

Successful rolls per site (exact), by Perception modifier and Hard Look level:

| Perception modifier | Hard Look 0 | Hard Look 1 | Hard Look 2 | Hard Look 3 | Hard Look 4 | Hard Look 5 |
| --- | --- | --- | --- | --- | --- | --- |
| -1 | 0.70 | 0.73 | 0.79 | 0.85 | 0.91 | 0.97 |
| 0 | 0.75 | 0.81 | 0.88 | 0.95 | 1.01 | 1.07 |
| +1 | 0.84 | 0.91 | 0.98 | 1.04 | 1.12 | 1.19 |
| +2 | 0.93 | 1.00 | 1.07 | 1.15 | 1.23 | 1.30 |
| +3 | 1.02 | 1.10 | 1.18 | 1.26 | 1.34 | 1.42 |
| +4 | 1.12 | 1.21 | 1.29 | 1.38 | 1.46 | 1.54 |
| +5 | 1.23 | 1.31 | 1.41 | 1.50 | 1.58 | 1.68 |

Attempts per site (exact):

| Perception modifier | Hard Look 0 | Hard Look 1 | Hard Look 2 | Hard Look 3 | Hard Look 4 | Hard Look 5 |
| --- | --- | --- | --- | --- | --- | --- |
| -1 | 1.00 | 1.05 | 1.15 | 1.25 | 1.35 | 1.45 |
| 0 | 1.00 | 1.10 | 1.20 | 1.30 | 1.40 | 1.50 |
| +1 | 1.05 | 1.15 | 1.25 | 1.35 | 1.45 | 1.55 |
| +2 | 1.10 | 1.20 | 1.30 | 1.40 | 1.50 | 1.60 |
| +3 | 1.15 | 1.25 | 1.35 | 1.45 | 1.55 | 1.65 |
| +4 | 1.20 | 1.30 | 1.40 | 1.50 | 1.60 | 1.70 |
| +5 | 1.25 | 1.35 | 1.45 | 1.55 | 1.65 | 1.75 |

Cross-check: 20000 played sites per cell differ from the exact successful rolls by at most 0.010.

XP per site at Hard Look 0 and Perception modifier 0 (`GetDeltaXP(10 x site level - rank)` per success, a quarter per failure), by site level and Gathering rank:

| Site level | Rank 0 | Rank 5 | Rank 10 | Rank 15 | Rank 20 | Rank 25 | Rank 30 | Rank 35 | Rank 40 | Rank 45 | Rank 50 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 975.0 | 853.0 | 487.5 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 |
| 2 | 975.0 | 975.0 | 975.0 | 853.0 | 487.5 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 |
| 3 | 975.0 | 975.0 | 975.0 | 975.0 | 975.0 | 853.0 | 487.5 | 0.0 | 0.0 | 0.0 | 0.0 |
| 4 | 975.0 | 975.0 | 975.0 | 975.0 | 975.0 | 975.0 | 975.0 | 853.0 | 487.5 | 0.0 | 0.0 |
| 5 | 975.0 | 975.0 | 975.0 | 975.0 | 975.0 | 975.0 | 975.0 | 975.0 | 975.0 | 853.0 | 487.5 |

## Refining

Each power core refines 3 + Refinery Management units. Every unit grants a flat XP set by its ore, to a refiner whose rank is the ore's level (5, 15, 25, 35 and 45 by ore tier); the rank scales it by the curve harvesting uses, `GetDeltaXP(ore level - rank) / GetDeltaXP(0)`, which the harvesting study tabulates. The ores the game does not have (Ochne, Croknor, Hemorgite, Bisteiss) are left out.

| Refinery Management | Units per core | Cores for 100 units | Cores for 9 units |
| --- | --- | --- | --- |
| 0 | 3 | 34 | 3 |
| 1 | 4 | 25 | 3 |
| 2 | 5 | 20 | 2 |
| 3 | 6 | 17 | 2 |
| 4 | 7 | 15 | 2 |
| 5 | 8 | 13 | 2 |
| 6 | 9 | 12 | 1 |

XP per refined unit at the ore's level, against what harvesting a node of the same tier pays per item at the tier's unlock rank (10 x (tier - 1)), five ranks later and ten ranks later:

| Ore | Ore tier | Refining XP per unit | XP per core at 3 units | XP per core at 9 units | Harvest XP per item at unlock rank | +5 ranks | +10 ranks |
| --- | --- | --- | --- | --- | --- | --- | --- |
| ore_tilarium | 1 | 25 | 75 | 225 | 1050 | 600 | 0 |
| raw_veldite | 1 | 25 | 75 | 225 | 1050 | 600 | 0 |
| ore_currian | 2 | 50 | 150 | 450 | 1050 | 600 | 0 |
| raw_scordspar | 2 | 50 | 150 | 450 | 1050 | 600 | 0 |
| ore_idailia | 3 | 75 | 225 | 675 | 1050 | 600 | 0 |
| raw_plagionite | 3 | 75 | 225 | 675 | 1050 | 600 | 0 |
| ore_barinium | 4 | 100 | 300 | 900 | 1050 | 600 | 0 |
| raw_keromber | 4 | 100 | 300 | 900 | 1050 | 600 | 0 |
| ore_gostian | 5 | 125 | 375 | 1125 | 1050 | 600 | 0 |
| raw_jasioclase | 5 | 125 | 375 | 1125 | 1050 | 600 | 0 |
| ore_arda | 5 | 150 | 450 | 1350 | 1050 | 600 | 0 |
| raw_arkoxit | 5 | 150 | 450 | 1350 | 1050 | 600 | 0 |

## Fishing

A bite spends one bait, then lands the fish at `40 + 10 x (Agriculture rank - fish level) + 2 x Willpower score` percent, kept between 0 and 95. XP is paid on every bite, caught or not.

Catch chance by rank above the fish's level and Willpower modifier (score in brackets):

| Rank - fish level | WIL -1 (8) | WIL 0 (10) | WIL +1 (12) | WIL +2 (14) | WIL +3 (16) | WIL +4 (18) | WIL +5 (20) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| -5 | 6% | 10% | 14% | 18% | 22% | 26% | 30% |
| -4 | 16% | 20% | 24% | 28% | 32% | 36% | 40% |
| -3 | 26% | 30% | 34% | 38% | 42% | 46% | 50% |
| -2 | 36% | 40% | 44% | 48% | 52% | 56% | 60% |
| -1 | 46% | 50% | 54% | 58% | 62% | 66% | 70% |
| 0 | 56% | 60% | 64% | 68% | 72% | 76% | 80% |
| +1 | 66% | 70% | 74% | 78% | 82% | 86% | 90% |
| +2 | 76% | 80% | 84% | 88% | 92% | 95% | 95% |
| +3 | 86% | 90% | 94% | 95% | 95% | 95% | 95% |
| +4 | 95% | 95% | 95% | 95% | 95% | 95% | 95% |
| +5 | 95% | 95% | 95% | 95% | 95% | 95% | 95% |
| +6 | 95% | 95% | 95% | 95% | 95% | 95% | 95% |

Catch chance per bite by Agriculture rank, averaged over every rod and bait pool at every fishing location (699 pools: each location, rod and bait, daytime and nighttime counted separately; pools weighted equally, fish within a pool by frequency). The game-wide average mixes pools no single angler fishes, so use it to compare rules, not as a rank's experience:

| Agriculture rank | WIL -1 | WIL 0 | WIL +1 | WIL +2 | WIL +3 | WIL +4 | WIL +5 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | 7.5% | 8.3% | 9.2% | 10.1% | 11.1% | 12.1% | 13.0% |
| 5 | 19.0% | 19.7% | 20.4% | 21.2% | 21.9% | 22.6% | 23.3% |
| 10 | 27.1% | 27.6% | 28.2% | 28.7% | 29.2% | 29.9% | 30.5% |
| 15 | 35.1% | 35.7% | 36.6% | 37.5% | 38.4% | 39.3% | 40.2% |
| 20 | 45.8% | 46.6% | 47.5% | 48.2% | 49.0% | 49.7% | 50.4% |
| 25 | 55.1% | 55.6% | 56.3% | 56.9% | 57.6% | 58.3% | 58.9% |
| 30 | 63.6% | 64.3% | 65.1% | 65.8% | 66.6% | 67.3% | 68.0% |
| 35 | 72.7% | 73.3% | 74.0% | 74.6% | 75.2% | 75.8% | 76.4% |
| 40 | 80.2% | 80.7% | 81.1% | 81.6% | 82.1% | 82.6% | 83.1% |
| 45 | 86.3% | 86.8% | 87.3% | 87.8% | 88.3% | 88.8% | 89.2% |
| 50 | 92.0% | 92.4% | 92.8% | 93.1% | 93.4% | 93.7% | 93.9% |

57 fish, levels 0 to 52. XP per bite is `GetDeltaXP(fish level - rank)`, caught or not:

| Fish level | Rank 0 | Rank 5 | Rank 10 | Rank 15 | Rank 20 | Rank 25 | Rank 30 | Rank 35 | Rank 40 | Rank 45 | Rank 50 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | 600 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 10 | 1200 | 1050 | 600 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 20 | 1200 | 1200 | 1200 | 1050 | 600 | 0 | 0 | 0 | 0 | 0 | 0 |
| 30 | 1200 | 1200 | 1200 | 1200 | 1200 | 1050 | 600 | 0 | 0 | 0 | 0 |
| 40 | 1200 | 1200 | 1200 | 1200 | 1200 | 1200 | 1200 | 1050 | 600 | 0 | 0 |
| 50 | 1200 | 1200 | 1200 | 1200 | 1200 | 1200 | 1200 | 1200 | 1200 | 1050 | 600 |

## Limitations

- The attribute modifiers are a sweep, not measured medians. A Willpower score of 10 + 2 x modifier assumes the engine's ability modifier, floor((score - 10) / 2).
- Harvesting counts items, not their value: the loot-table roll behind each item, and the extra `LOOT_TABLE_` rolls some nodes add through `Loot.SpawnLoot`, are not played. Scavenging counts successful rolls, not the loot they return, and ignores Treasure Hunter and Creditfinder.
- Respawn, node density and travel time are not modelled, so these are yields per node or site, not per hour.
- The fishing pool average ignores that most pools are unreachable for a given rod, and the 80% chance of no bite when only the default fish is available. Real anglers choose their location, rod and bait.
- Refining XP is shown at the ore's level; how it falls as the rank passes the level is in the harvesting study. The progression-pace baseline still counts the flat XP at every rank.
