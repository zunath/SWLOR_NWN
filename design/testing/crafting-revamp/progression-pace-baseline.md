# Crafting revamp: progression pace baseline (workstream 11)

How long a new crafter takes today to reach rank 50 in each crafting skill and in Gathering, with the component gathering that supplies the crafts, and the proposals this gives the owner. **Nothing in the game's XP changes in this PR**; the numbers below are today's pace, measured with the legacy craft model (`design/testing/crafting-revamp/legacy-crafting-baseline.md`). The XP-rate gate (gate 6) measures new minigames against today's XP first; any change proposed here applies on top of that result.

## Hours to rank 50 today

A new crafter (rank 0, nothing handed in to a guild) who always crafts recipes of their own level with the legacy lookahead player. Hours are craft window time (6.0 s per action plus 30 s per craft) plus the time to gather and refine the components (node travel is an unmeasured assumption, below). Mid gear is the headline; the band-by-band tables give all three gear levels.

| Skill | Crafts to 50 (mid gear) | Craft window h | Gathering h | Total h: no gear | Total h: mid gear | Total h: best gear | XP per hour (mid gear) | Component units no node supplies (mid gear) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Smithery | 536 | 20.1 | 87.9 | 116.6 | 108.0 | 100.1 | 3,808 | 1020 |
| Engineering | 539 | 19.8 | 43.0 | 65.9 | 62.8 | 59.7 | 6,545 | 1078 |
| Fabrication | 530 | 20.4 | 35.5 | 58.2 | 55.9 | 53.5 | 7,355 | 582 |
| Agriculture | 539 | 19.7 | 16.9 | 36.4 | 36.6 | 36.4 | 11,218 | 1839 |
| Gathering (the skill) | - | - | 3.1 | 3.1 | 3.1 | 3.1 | 131,722 | - |

Rank 50 costs 411,125 XP in every skill (`Skill.GetRequiredXP`, ranks 0-49). RP XP pays 3,000 XP per hour with no materials (1,500 base XP every 30 minutes; the public mechanism in `RoleplayXP.cs`), which is 137 hours for the same XP. Gathering takes 4.4 h by harvesting and refining alone, 3.5 h by scavenging alone and 3.1 h taking the better of the two at each rank.

What the numbers say:

- **Crafts, not just components.** Mid gear needs about 536 at-level crafts to reach rank 50, not the 340-420 in the plan's research: that range assumes near-full quality, and the lookahead player's quality at mid gear is 30% on average (quality adds up to 100% to a craft's XP).
- **Pace.** The craft loop pays 3,808-11,218 XP per hour with its gathering, 1.3-3.7 times what RP XP pays. Players who bypass crafting with RP XP are buying out the materials and gathering, not a faster rate.
- **Gathering dominates Smithery.** 81% of its hours are gathering. The three components that cost the most are `lth_good` (16.7 h), `ruby` (15.1 h), `lth_high` (10.5 h); each drops on a small share of rolls (gems 2-9%, crate goods 15%), so one unit costs 3.4-7.4 minutes.
- **Gathering is quick for the skill itself.** Each harvested item pays up to 1,200 XP and refining pays 25-150 XP per unit whatever the rank, so Gathering rank 50 takes hours, not days, if nodes are free. Node respawns and contention are not modelled.
- **Agriculture has a faster path than the one modelled here.** Fishing pays the same chart per bite (600-1,200 XP); see the catch task table in the guild section.

## Inputs and assumptions

- Command: `SWLOR.CraftSim progression --runs 500 --seed 1 --out design/testing/crafting-revamp/progression-pace-baseline.md`
- Seed 1; 500 lookahead crafts per recipe level, skill and gear level (8 sampled futures per action), each from its own stream of the seed, so a run repeats exactly. The hours themselves are expected values, not sampled.
- Recipes: every active recipe in the server's definitions (Smithery 777; Engineering 1786; Fabrication 456; Agriculture 319). The crafter's rank steps from 0 to 49 and each step crafts recipes of level max(1, rank): the `Skill.GetDeltaXP` price for the level equals the rank is 600 XP (676 at rank 0). Each of a level's recipes counts equally, as in the gates' cohort.
- Gear: none, tier 3 (mid) and tier 5 (best) of the skill's own crafting gear (`legacy-crafting-baseline.md`, "Gear levels"). Food and status bonuses are not played.
- Craft player and XP: the legacy lookahead (`RolloutPolicy`) at the recipe's level and rank. A success pays the base XP plus the quality share; a failure pays 15% of the base (`CraftOutcomeRules`). No blueprint, no enhancements, no XP bonuses from Social or food, and no first-craft bonus unless the variety row below says so.
- Time in the craft window: 6.0 s per action (the legacy window's estimate) plus **30 s per craft** to open the station, pick the recipe, commit the components and collect the item. The overhead is an unmeasured estimate; the gates' own overhead defaults to 0. The sensitivity table shows 15 s and 60 s.
- Components: a craft commits every component stack of the recipe and a failure loses each stack on 65% of rolls (`CraftViewModel.cs:1601-1641`), so a craft consumes success + 0.65 x failure of the committed units. A unit is priced at the cheapest loot table in `HarvestingLootTableDefinition` that drops it, worked the way the game works that table. Ore veins and trees are harvested (`res_used` on the placeable): each harvester use takes 5.0 s (the item's `Delay(5f)`) plus its share of 30 s node travel over the node's 2.5 average harvests, and rolls the table once (plus the Might bonus roll; Might modifier 0). Bushes, herbs, vegetable patches and supply crates are scavenge sites (`scav_opened`, `SCAVENGE_POINT_LOOT_TABLE_NAME`): a visit costs the 30 s walk plus 2.0 s per search, the site can be searched once, and each successful search (`d20 + Perception >= 6`, then 6 + d3 on the second attempt that Hard Look gives) is one roll of the table, giving 1 to the item's maximum quantity. Hard Look is taken at the site's level, the lowest Gathering tier that can search it; the Perception modifier is 0.
- Refined ore (`ref_*`) costs the raw ore plus refining: 6 s per operation (`RefiningDelaySeconds`) over a batch of 99 units (a full 99-unit stack; the refinery accepts several stacks per operation, so this is cautious). The sensitivity table also prices a batch of one power core (4 units). Power cores cost credits, not time.
- Asteroid ores (`ore_*`, the Engineering recipes' `ref_tilarium` and the like) are priced like a ground node of the same tier. Space mining is not modelled.
- Node travel: **30 s** between nodes, unmeasured. Respawn waits and other players at the node are not modelled, so gathering hours are a lower bound. Gathering rank is not a gate: the crafter is assumed to reach every node tier the recipe needs.
- Components no node supplies (enemy and crate drops of leather and electronics, meat, vendor goods, parts another recipe makes) are left out of the hours and counted in the summary column. The sensitivity table prices them at 20 s a unit, about a harvested unit.
- Gathering the skill: each rank uses the better of harvesting and refining the best ground vein the tier allows (`CraftRankMilestones.TierForRank`) and searching the best scavenge site, at 30 s between nodes or sites and 2.0 s per scavenge attempt. Perception modifier 0.

Rules copied from the server (the model calls everything else directly), each checked against its source by `ProgressionSourceTests` or `LegacySourceTests`:

| Rule | Source |
| --- | --- |
| RP XP per hour | `RoleplayXP.cs:16-93` |
| Guild points per hand-in | `Guild.cs:62-80` |
| Ten guild tasks per tier on the board | `Guild.cs:124-170` |
| Seconds per harvester use | `HarvesterItemDefinition.cs:50-60` |
| Seconds per scavenge attempt | `ScavengePoint.cs:55-70` |
| Seconds per refining operation | `RefineryViewModel.cs:58-64` |
| Research job length and credits | `Craft.cs:1145-1192` |
| A first research job creates the blueprint | `ResearchViewModel.cs:387-410` |
| Components and enhancements lost on failure | `CraftViewModel.cs:1601-1641` |
| Recipe rank requirement | `Craft.cs:327-331` |
| Harvests per node | `HarvesterItemDefinition.cs:102-109` |
| Harvest bonus item | `HarvesterItemDefinition.cs:113-124` |
| Harvest XP | `HarvesterItemDefinition.cs:144-153` |
| Scavenge second attempt | `ScavengePoint.cs:51-57` |
| Scavenge success roll and DC | `ScavengePoint.cs:63-105` |
| Ore tiers and XP per unit | `RefineryViewModel.cs:30-53` |
| Refinery batch size | `RefineryViewModel.cs:58` |
| Power cores per batch | `RefineryViewModel.cs:169-171` |
| Refining XP | `RefineryViewModel.cs:337-352` |

## Hours per 10-rank band

A band is the hours to climb from its first rank to the next band's first rank (0-10 is rank 0 to rank 10). Crafts, craft window and gathering hours are mid gear; the three total columns give each gear level. "RP XP h" is the hours RP XP needs for the band's XP.

### Smithery

| Ranks | XP | Crafts (mid) | Craft window h | Gathering h | Total h: no gear | Total h: mid gear | Total h: best gear | XP per hour (mid) | RP XP h |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0-10 | 17,545 | 28 | 0.5 | 1.3 | 1.8 | 1.8 | 1.6 | 9,929 | 5.8 |
| 10-20 | 50,960 | 77 | 2.1 | 5.2 | 8.0 | 7.3 | 6.5 | 7,012 | 17.0 |
| 20-30 | 89,340 | 130 | 4.4 | 16.1 | 21.0 | 20.5 | 19.2 | 4,359 | 29.8 |
| 30-40 | 107,950 | 159 | 6.0 | 34.7 | 42.5 | 40.7 | 38.5 | 2,652 | 36.0 |
| 40-50 | 145,330 | 142 | 7.1 | 30.6 | 43.4 | 37.7 | 34.2 | 3,852 | 48.4 |
| 0-50 | 411,125 | 536 | 20.1 | 87.9 | 116.6 | 108.0 | 100.1 | 3,808 | 137.0 |

### Engineering

| Ranks | XP | Crafts (mid) | Craft window h | Gathering h | Total h: no gear | Total h: mid gear | Total h: best gear | XP per hour (mid) | RP XP h |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0-10 | 17,545 | 28 | 0.5 | 0.5 | 1.0 | 1.0 | 0.9 | 18,395 | 5.8 |
| 10-20 | 50,960 | 77 | 2.0 | 2.2 | 4.5 | 4.2 | 4.0 | 12,148 | 17.0 |
| 20-30 | 89,340 | 131 | 4.2 | 12.7 | 17.2 | 16.9 | 16.1 | 5,275 | 29.8 |
| 30-40 | 107,950 | 160 | 5.9 | 13.3 | 19.3 | 19.2 | 18.9 | 5,630 | 36.0 |
| 40-50 | 145,330 | 143 | 7.1 | 14.4 | 23.9 | 21.6 | 19.8 | 6,741 | 48.4 |
| 0-50 | 411,125 | 539 | 19.8 | 43.0 | 65.9 | 62.8 | 59.7 | 6,545 | 137.0 |

### Fabrication

| Ranks | XP | Crafts (mid) | Craft window h | Gathering h | Total h: no gear | Total h: mid gear | Total h: best gear | XP per hour (mid) | RP XP h |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0-10 | 17,545 | 28 | 0.5 | 0.8 | 1.3 | 1.3 | 1.2 | 13,570 | 5.8 |
| 10-20 | 50,960 | 76 | 2.2 | 2.6 | 5.0 | 4.8 | 4.4 | 10,718 | 17.0 |
| 20-30 | 89,340 | 130 | 4.4 | 11.9 | 16.4 | 16.4 | 15.5 | 5,462 | 29.8 |
| 30-40 | 107,950 | 158 | 6.1 | 8.6 | 14.3 | 14.7 | 14.8 | 7,368 | 36.0 |
| 40-50 | 145,330 | 139 | 7.2 | 11.6 | 21.1 | 18.8 | 17.6 | 7,715 | 48.4 |
| 0-50 | 411,125 | 530 | 20.4 | 35.5 | 58.2 | 55.9 | 53.5 | 7,355 | 137.0 |

### Agriculture

| Ranks | XP | Crafts (mid) | Craft window h | Gathering h | Total h: no gear | Total h: mid gear | Total h: best gear | XP per hour (mid) | RP XP h |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0-10 | 17,545 | 28 | 0.5 | 0.8 | 1.4 | 1.3 | 1.2 | 13,209 | 5.8 |
| 10-20 | 50,960 | 77 | 2.0 | 2.1 | 4.4 | 4.1 | 3.9 | 12,387 | 17.0 |
| 20-30 | 89,340 | 131 | 4.2 | 2.8 | 6.4 | 7.1 | 7.3 | 12,658 | 29.8 |
| 30-40 | 107,950 | 160 | 5.9 | 6.6 | 12.0 | 12.4 | 12.9 | 8,672 | 36.0 |
| 40-50 | 145,330 | 143 | 7.1 | 4.6 | 12.2 | 11.7 | 11.1 | 12,421 | 48.4 |
| 0-50 | 411,125 | 539 | 19.7 | 16.9 | 36.4 | 36.6 | 36.4 | 11,218 | 137.0 |

## Gathering rank 50

Hours per band for a new gatherer. Harvesting pays `GetDeltaXP(vein level - rank)` for every item, and a vein two tiers below the rank pays nothing, so the gatherer always works the top vein the tier allows; refining pays a flat 25-150 XP per raw unit.

| Ranks | XP | Harvest and refine h | Scavenge h | Better of the two h | XP per hour (better) | RP XP h |
| --- | --- | --- | --- | --- | --- | --- |
| 0-10 | 17,545 | 0.31 | 0.19 | 0.17 | 101,025 | 5.8 |
| 10-20 | 50,960 | 0.69 | 0.50 | 0.44 | 116,714 | 17.0 |
| 20-30 | 89,340 | 1.02 | 0.80 | 0.70 | 127,665 | 29.8 |
| 30-40 | 107,950 | 1.10 | 0.90 | 0.80 | 135,612 | 36.0 |
| 40-50 | 145,330 | 1.28 | 1.15 | 1.02 | 143,178 | 48.4 |
| 0-50 | 411,125 | 4.40 | 3.54 | 3.12 | 131,722 | 137.0 |

The plan's quick gathering and the minigame sessions that replace the harvester and the search, for the same climb. **Quick Harvest** resolves a node at today's expected units and XP, one harvest delay per unit, and **Quick Search** resolves a site at today's expected rolls and XP, taking as long as today's attempts, so each is today's loop by construction and neither can train Gathering faster than the rows below. A played session may exceed it by the gate's starting value of 15%; the minigames are not built yet, so that row is the cap the parity gate enforces, not a measurement.

| Cohort | Gathering XP per hour (rank 0 to 50) | Hours to rank 50 | Ceiling the plan sets |
| --- | --- | --- | --- |
| Quick Harvest (today's harvesting and refining) | 93,472 | 4.40 | today's rate, never above it |
| Quick Search (today's scavenging) | 116,148 | 3.54 | today's rate, never above it |
| Played Prospecting session at the parity cap | 107,493 | 3.82 | at most 15% above today's |
| Played Search session at the parity cap | 133,571 | 3.08 | at most 15% above today's |

Sensitivity of the better-of-two total to the node travel assumption, the only unmeasured input that moves it:

| Seconds between nodes | Harvest and refine h | Scavenge h | Better of the two h |
| --- | --- | --- | --- |
| 15 | 2.9 | 1.9 | 1.8 |
| 30 | 4.4 | 3.5 | 3.1 |
| 60 | 7.5 | 6.8 | 5.6 |
| 120 | 13.7 | 13.3 | 10.7 |
| 300 | 32.2 | 32.7 | 25.8 |

## What supplies the components

Share of component units the at-level recipes commit, by where the model says they come from, over the whole climb. A unit only counts as gathered when a gathering loot table in `HarvestingLootTableDefinition` drops it (or its raw ore does).

| Skill | Units per climb (mid gear) | Refined ore | Harvested items | Made by another recipe | Other (drops, vendor, quest) |
| --- | --- | --- | --- | --- | --- |
| Smithery | 4027 | 28% | 46% | 0% | 25% |
| Engineering | 2861 | 40% | 22% | 21% | 17% |
| Fabrication | 3297 | 32% | 50% | 3% | 15% |
| Agriculture | 2468 | 0% | 25% | 0% | 75% |

Gathering hours of the mid gear climb by kind of node. Each component is priced at the cheapest table that drops it. Ore veins and trees are harvested; bushes, herbs, vegetable patches and supply crates are scavenge sites; asteroid tables are mined from a ship and priced like a harvest node:

| Skill | asteroids | bushes | herbs | ore veins | supply crates | trees | vegetable patches |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Smithery | 0.0 | 8.4 | 0.0 | 36.3 | 41.8 | 1.4 | 0.0 |
| Engineering | 6.8 | 0.6 | 0.0 | 12.7 | 23.0 | 0.0 | 0.0 |
| Fabrication | 0.1 | 6.2 | 0.0 | 5.2 | 18.4 | 5.6 | 0.0 |
| Agriculture | 0.0 | 0.0 | 2.9 | 0.0 | 0.0 | 0.0 | 14.0 |

The five components that cost the most gathering hours in each skill's mid gear climb:

| Skill | Component | Gathering h | Share of the skill's gathering |
| --- | --- | --- | --- |
| Smithery | `lth_good` | 16.7 | 19% |
| Smithery | `ruby` | 15.1 | 17% |
| Smithery | `lth_high` | 10.5 | 12% |
| Smithery | `citrine` | 8.6 | 10% |
| Smithery | `elec_high` | 5.8 | 7% |
| Engineering | `elec_good` | 12.6 | 29% |
| Engineering | `elec_high` | 7.4 | 17% |
| Engineering | `ruby` | 5.0 | 12% |
| Engineering | `citrine` | 2.9 | 7% |
| Engineering | `emerald` | 1.6 | 4% |
| Fabrication | `lth_good` | 5.9 | 17% |
| Fabrication | `elec_good` | 5.2 | 15% |
| Fabrication | `elec_high` | 3.9 | 11% |
| Fabrication | `lth_high` | 3.4 | 10% |
| Fabrication | `fiberp_high` | 2.4 | 7% |
| Agriculture | `tomato` | 2.2 | 13% |
| Agriculture | `melon` | 1.8 | 11% |
| Agriculture | `plant_butter` | 1.3 | 8% |
| Agriculture | `mushroom` | 1.2 | 7% |
| Agriculture | `ginger` | 1.0 | 6% |

## Sensitivity

Total hours to rank 50 at mid gear when one unmeasured input or play choice changes. Every other row is the baseline above.

| Scenario | Smithery | Engineering | Fabrication | Agriculture |
| --- | --- | --- | --- | --- |
| Baseline | 108.0 | 62.8 | 55.9 | 36.6 |
| Overhead 15 s per craft | 105.7 | 60.6 | 53.7 | 34.4 |
| Overhead 60 s per craft | 112.4 | 67.3 | 60.3 | 41.1 |
| Node travel 15 s | 71.7 | 45.2 | 40.8 | 28.9 |
| Node travel 60 s | 180.5 | 98.1 | 86.1 | 52.1 |
| Node travel 120 s | 325.7 | 168.8 | 146.5 | 83.1 |
| Might modifier +2 (half the harvests drop a second item) | 95.4 | 56.3 | 52.3 | 36.6 |
| Refining one power core (4 units) per operation | 108.4 | 63.3 | 56.3 | 36.6 |
| Components no node supplies cost 20 s a unit | 113.6 | 68.8 | 59.1 | 46.9 |
| Crafts the cheapest quarter of each level's recipes | 44.5 | 27.6 | 29.9 | 19.7 |
| Rotates through each level's recipes (20% first-craft bonus on each) | 92.9 | 52.4 | 48.3 | 33.2 |

## Guild task cohort

With the task cap lifted, a completed task goes back on the board at once for the Smithery, Engineering, Fabrication and Agriculture guilds, so hand-ins are a repeatable activity. A task wants one item (four raw fish for a catch task) and pays credits and guild points by its tier, which is the guild rank that unlocks it. Credits, guild points and the rank thresholds are unchanged.

| Guild | Tasks | Crafted | Caught | Other | Tier 0-4 recipe or fish levels | Credits per task by tier | Base guild points by tier |
| --- | --- | --- | --- | --- | --- | --- | --- |
| SmitheryGuild | 175 | 175 | 0 | 0 | 1-9, 11-19, 21-29, 31-39, 41-49 | 1,000 / 2,500 / 4,000 / 5,500 / 7,200 | 21 / 81 / 117 / 156 / 195 |
| EngineeringGuild | 80 | 80 | 0 | 0 | 1-10, 11-20, 21-30, 31-40, 41-50 | 1,000 / 2,500 / 4,000 / 5,500 / 7,200 | 21 / 81 / 117 / 156 / 195 |
| FabricationGuild | 211 | 211 | 0 | 0 | 1-10, 11-20, 21-30, 31-40, 41-50 | 1,000 / 2,500 / 4,000 / 5,500 / 7,200 | 21 / 81 / 117 / 156 / 195 |
| AgricultureGuild | 185 | 134 | 51 | 0 | 0-10, 10-20, 20-30, 30-40, 40-52 | 1,000 / 2,500 / 4,000 / 5,500 / 7,200 | 21 / 81 / 117 / 156 / 195 |

Every crafted task asks for a player-produced item, which includes quick-crafted ones (`NwnCraftSessionWorld.CreateCraftedItem` stamps both). Tier k holds the recipes of levels 10k+1 to 10k+10. Guild rank 5 needs 45,000 GP, about 238 hand-ins when each is handed in at the tier of the rank it climbs from.

### Cohort: hand in what you craft

The cohort crafts at level as before and, whenever the board has a task for that recipe level and the guild rank allows it, hands the item in. The board is the catch: `Guild.RefreshGuildTasks` keeps only 10 random tasks of a tier at each server start (all of them when the tier has 10 or fewer), the same for every player, and the cap change does not touch that. A level's task is therefore on the board with a chance, and the model weights every hand-in, its XP, points and credits by it (the chance that at least one of the level's tasks is among the ten drawn; Agriculture's catch tasks take board places too). A crafter who finds no task for their level on this boot's board hands nothing in, so a climb over many server starts gets the expected share. A hand-in asks for one exact recipe (the quest collector matches the item's resref), so when the board holds the level's task the crafter makes that recipe, not an arbitrary one of the level: the craft's components are the level's task recipes' mean, not the level's average over all recipes (a crafter who picks recipes by cost can do so with or without a guild; see the sensitivity table). Each hand-in costs 10 s (unmeasured). When the guild rank is what stands in the way, the crafter quick-crafts the preferred task item of their current tier that is on the board and that quick craft allows (the cheapest to gather; rank at least level + 5, after one craft by hand; 2 s per item, components gathered as above) and hands it in until the rank rises; the cost is the expected cost over which tasks the board holds, given that it holds one. A board with no eligible item makes the crafter wait for the next server start; that is calendar time, not play time, and it is not charged, so the table gives the lowest chance of such a board in any catch-up. Those items pay no XP. The award is a share of the craft's base XP (`GetDeltaXP(task level - rank)`, the same cutoff as crafting), so a hand-in five or more levels below the rank pays nothing and quick-crafted items never do.

| Skill | Award share | Total h (mid gear) | Change vs no guild | Change vs the 0% award | At-level hand-ins | Catch-up hand-ins | Catch-up h | Lowest chance a board held a catch-up item | Guild rank at rank 50 | Hand-in XP | Share of the climb's XP | Credits earned |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Smithery | no guild | 108.0 | - | - | - | - | - | - | - | - | - | - |
| Smithery | 0% (hand-ins for credits and points only) | 87.8 | -19% | - | 360 | 28 | 0.6 | 100% | 5 | 0 | 0% | 1,795,664 |
| Smithery | 25% | 78.9 | -27% | -10% | 314 | 31 | 0.6 | 100% | 5 | 47,071 | 11% | 1,579,669 |
| Smithery | 50% | 72.1 | -33% | -18% | 278 | 33 | 0.7 | 100% | 5 | 83,431 | 20% | 1,413,623 |
| Smithery | 100% | 62.4 | -42% | -29% | 227 | 48 | 1.0 | 100% | 5 | 136,136 | 33% | 1,205,097 |
| Engineering | no guild | 62.8 | - | - | - | - | - | - | - | - | - | - |
| Engineering | 0% (hand-ins for credits and points only) | 64.7 | 3% | - | 413 | 24 | 0.3 | 100% | 5 | 0 | 0% | 2,021,724 |
| Engineering | 25% | 56.5 | -10% | -13% | 357 | 27 | 0.3 | 100% | 5 | 53,493 | 13% | 1,762,643 |
| Engineering | 50% | 50.2 | -20% | -22% | 314 | 30 | 0.3 | 100% | 5 | 94,222 | 23% | 1,565,738 |
| Engineering | 100% | 41.3 | -34% | -36% | 254 | 37 | 0.4 | 100% | 5 | 152,445 | 37% | 1,291,900 |
| Fabrication | no guild | 55.9 | - | - | - | - | - | - | - | - | - | - |
| Fabrication | 0% (hand-ins for credits and points only) | 52.4 | -6% | - | 361 | 26 | 0.5 | 100% | 5 | 0 | 0% | 1,758,221 |
| Fabrication | 25% | 46.3 | -17% | -12% | 318 | 29 | 0.5 | 100% | 5 | 47,669 | 12% | 1,563,006 |
| Fabrication | 50% | 41.6 | -26% | -21% | 284 | 31 | 0.6 | 100% | 5 | 85,170 | 21% | 1,408,823 |
| Fabrication | 100% | 34.7 | -38% | -34% | 234 | 39 | 0.7 | 100% | 5 | 140,566 | 34% | 1,193,105 |
| Agriculture | no guild | 36.6 | - | - | - | - | - | - | - | - | - | - |
| Agriculture | 0% (hand-ins for credits and points only) | 39.2 | 7% | - | 295 | 31 | 0.1 | 100% | 5 | 0 | 0% | 1,446,686 |
| Agriculture | 25% | 35.3 | -4% | -10% | 264 | 33 | 0.1 | 100% | 5 | 39,664 | 10% | 1,308,392 |
| Agriculture | 50% | 32.2 | -12% | -18% | 240 | 38 | 0.2 | 100% | 5 | 71,950 | 18% | 1,206,128 |
| Agriculture | 100% | 27.5 | -25% | -30% | 203 | 53 | 0.3 | 100% | 5 | 121,585 | 30% | 1,076,540 |

Read the award's effect against the 0% row, not against no guild: handing in the task recipes already changes the hours at a 0% award, because their components differ from the level's average recipe. Hand-in XP's share of the climb is the figure to read for the award: the proposal should land it between a fifth and a third, so it neither dominates nor vanishes.

The same cohort by rank band at the recommended 50% award. The task tier a crafter can hand in at is capped by the guild rank, which the catch-up hand-ins raise; one item per task, taken from the crafter's own craft or, for the catch-up, from quick craft.

| Skill | Ranks | Task tier at level | Items per task | At-level hand-ins | Quick-crafted catch-up hand-ins | Hand-in hours (turn-in and catch-up) | Hand-in XP | Share of the band's XP |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Smithery | 0-10 | 0 | 1 | 15 | 0 | 0.04 | 4,420 | 25% |
| Smithery | 10-20 | 1 | 1 | 39 | 33 | 0.76 | 11,754 | 23% |
| Smithery | 20-30 | 2 | 1 | 67 | 0 | 0.23 | 19,960 | 22% |
| Smithery | 30-40 | 3 | 1 | 79 | 0 | 0.22 | 23,691 | 22% |
| Smithery | 40-50 | 4 | 1 | 79 | 0 | 0.22 | 23,605 | 16% |
| Engineering | 0-10 | 0 | 1 | 15 | 0 | 0.04 | 4,617 | 26% |
| Engineering | 10-20 | 1 | 1 | 44 | 30 | 0.46 | 13,087 | 26% |
| Engineering | 20-30 | 2 | 1 | 75 | 0 | 0.21 | 22,530 | 25% |
| Engineering | 30-40 | 3 | 1 | 90 | 0 | 0.25 | 27,138 | 25% |
| Engineering | 40-50 | 4 | 1 | 89 | 0 | 0.25 | 26,849 | 18% |
| Fabrication | 0-10 | 0 | 1 | 14 | 0 | 0.04 | 4,264 | 24% |
| Fabrication | 10-20 | 1 | 1 | 39 | 31 | 0.69 | 11,846 | 23% |
| Fabrication | 20-30 | 2 | 1 | 68 | 0 | 0.19 | 20,412 | 23% |
| Fabrication | 30-40 | 3 | 1 | 82 | 0 | 0.23 | 24,646 | 23% |
| Fabrication | 40-50 | 4 | 1 | 80 | 0 | 0.22 | 24,002 | 17% |
| Agriculture | 0-10 | 0 | 1 | 12 | 0 | 0.03 | 3,547 | 20% |
| Agriculture | 10-20 | 1 | 1 | 33 | 34 | 0.23 | 10,042 | 20% |
| Agriculture | 20-30 | 2 | 1 | 59 | 4 | 0.20 | 17,649 | 20% |
| Agriculture | 30-40 | 3 | 1 | 71 | 0 | 0.20 | 21,243 | 20% |
| Agriculture | 40-50 | 4 | 1 | 65 | 0 | 0.18 | 19,470 | 13% |

### The guild rank chore

What raising a guild from rank 0 to 5 costs a rank-50 crafter who quick-crafts the cheapest eligible task item of each tier that is on the board and hands it in (a board with none makes them wait for the next server start, which is not charged). Before the change the board held ten tasks a tier per server start, so rank 5 took at least 14-21 days.

| Guild | Hand-ins | Hours | Credits earned | Credits per hour | Lowest chance a board holds an eligible item |
| --- | --- | --- | --- | --- | --- |
| SmitheryGuild | 238 | 5.1 | 954,321 | 186,743 | 100% |
| EngineeringGuild | 238 | 8.2 | 954,321 | 115,856 | 100% |
| FabricationGuild | 238 | 3.2 | 954,321 | 295,343 | 100% |
| AgricultureGuild | 238 | 0.9 | 954,321 | 1,066,797 | 100% |

| Guild | Tier | Item | Item level | Guild points each | Hand-ins | Hours | Chance a board holds an eligible item |
| --- | --- | --- | --- | --- | --- | --- | --- |
| SmitheryGuild | 0 | `b_knife` | 1 | 21 | 48 | 0.89 | 100% |
| SmitheryGuild | 1 | `tit_knife` | 11 | 101 | 40 | 0.75 | 100% |
| SmitheryGuild | 2 | `del_knife` | 21 | 175 | 57 | 1.35 | 100% |
| SmitheryGuild | 3 | `proto_knife` | 31 | 273 | 55 | 1.09 | 100% |
| SmitheryGuild | 4 | `oph_knife` | 41 | 390 | 38 | 1.03 | 100% |
| EngineeringGuild | 0 | `min_laser_b` | 1 | 21 | 48 | 0.50 | 100% |
| EngineeringGuild | 1 | `min_laser_1` | 11 | 101 | 40 | 0.42 | 100% |
| EngineeringGuild | 2 | `min_laser_2` | 21 | 175 | 57 | 4.19 | 100% |
| EngineeringGuild | 3 | `min_laser_3` | 31 | 273 | 55 | 0.58 | 100% |
| EngineeringGuild | 4 | `min_laser_4` | 41 | 390 | 38 | 2.55 | 100% |
| FabricationGuild | 0 | `structure_0132` | 1 | 21 | 48 | 0.86 | 100% |
| FabricationGuild | 1 | `structure_0026` | 11 | 101 | 40 | 0.52 | 100% |
| FabricationGuild | 2 | `structure_0004` | 21 | 175 | 57 | 0.74 | 100% |
| FabricationGuild | 3 | `structure_0155` | 31 | 273 | 55 | 0.61 | 100% |
| FabricationGuild | 4 | `structure_0163` | 41 | 390 | 38 | 0.51 | 100% |
| AgricultureGuild | 0 | `mynock_broth` | 3 | 21 | 48 | 0.16 | 100% |
| AgricultureGuild | 1 | `raivor_broth` | 13 | 101 | 40 | 0.13 | 100% |
| AgricultureGuild | 2 | `ara_broth` | 23 | 175 | 57 | 0.19 | 100% |
| AgricultureGuild | 3 | `womp_broth` | 33 | 273 | 55 | 0.18 | 100% |
| AgricultureGuild | 4 | `wild_broth` | 43 | 390 | 38 | 0.23 | 100% |

### Catch tasks (Agriculture)

A catch task wants four raw fish of one kind (for example `agr_tsk_017`, four moat carp). The XP basis is the fish's `FishType` level against the Agriculture rank. Fishing already pays `GetDeltaXP(fish level - rank)` on every bite, landed or not, at an unmeasured 30 s per bite and the catch chance `40 + 10 x (rank - level) + 2 x Willpower` (Willpower score 10). The table sets the rank equal to the tier's median fish level.

| Tier | Fish level | Fish per task | Catch chance | Bites per task | Bite XP each | Seconds per task | Bite XP per hour | Hand-in XP at 50% per task | Hand-in XP at 50% per fish | Share of the task's XP: per task | Share: per fish |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | 5 | 4 | 60% | 6.7 | 600 | 210 | 68,571 | 300 | 1200 | 7% | 23% |
| 1 | 15 | 4 | 60% | 6.7 | 600 | 210 | 68,571 | 300 | 1200 | 7% | 23% |
| 2 | 25 | 4 | 60% | 6.7 | 600 | 210 | 68,571 | 300 | 1200 | 7% | 23% |
| 3 | 35 | 4 | 60% | 6.7 | 600 | 210 | 68,571 | 300 | 1200 | 7% | 23% |
| 4 | 45 | 4 | 60% | 6.7 | 600 | 210 | 68,571 | 300 | 1200 | 7% | 23% |

Fishing pays several times what the cooking loop does per hour at this bite time, so Agriculture's pace is set by fishing for anyone who fishes. The table assumes a fish of the tier's level is available to the angler; location, rod and bait pools are not modelled.

### What limits the hand-ins

Every hand-in consumes one real player-crafted item (or four caught fish), which already limits the rate: the components. Nothing else limits it once the cap is lifted: the board's ten tasks per tier no longer bound it, because a completed task returns at once and one task can be repeated. Quick craft makes a base item in 2 s, so for a high-rank crafter the real bounds are the components and the turn-in time. Credits are the lever that matters: a task pays 1,000 to 7,200 credits plus the Guild Relations and Social bonuses, and the Smithery rank climb alone pays 954,321 credits for 5.1 hours of work. Nothing was added in this PR; the owner decides whether credits need a limit (see the proposals).

## Research cohort

Research becomes a Fabrication XP source in the revamp (an experiment plus job completion), with the amount not yet set. This cohort is a Fabrication researcher at the tier their rank opens, doing the job loop that pays the most XP per hour. A job's length in seconds and its credit cost are one formula, `200 x tier^2.2 x level^2` (tier = recipe level / 10 + 1, at most 5; `Craft.CalculateResearchCost`), where level is the blueprint's new research level 1-10. A recipe's blueprint is first acquired by starting its level-1 job with no blueprint, so "blueprint acquisition" is that job. The tier gate is Fabrication rank / 10 and the job slots are 1, 2 from rank 25 and 3 from rank 50 (`CraftRankMilestones`).

Time at the terminal per job: an experiment of 6 trials at 6 s, plus 30 s to start the job and 30 s to collect it (96 s; unmeasured). The recipe researched is the researchable one (weapons, armor, food) whose level pays the most against the rank within the tier. "Fast loop" keeps every slot on level-1 jobs for new blueprints; "climb" runs each slot up one blueprint from level 1 to 10. Base XP is the XP chart's price for the recipe level against the rank, the unit the award would be a share of.

| Fabrication rank | Tier | Recipe level | Base XP | Job slots | Level-1 job | Level-10 job | Level-1 job credits | Fast loop jobs per h | Climb jobs per h | Crafting XP per h (incl. gathering) | Research XP per h at a 100% award |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 10 | 1 | 9 | 450 | 1 | 3.3 min | 5.6 h | 200 | 18.0 | 0.47 | 9,854 | 8,100 |
| 15 | 1 | 9 | 0 | 1 | 3.3 min | 5.6 h | 200 | 18.0 | 0.47 | 11,689 | 0 |
| 20 | 2 | 19 | 450 | 1 | 15.3 min | 25.5 h | 918 | 3.9 | 0.10 | 9,309 | 1,765 |
| 25 | 2 | 19 | 0 | 2 | 15.3 min | 25.5 h | 918 | 7.8 | 0.20 | 6,710 | 0 |
| 30 | 3 | 29 | 450 | 2 | 37.4 min | 62.3 h | 2,242 | 3.2 | 0.08 | 3,947 | 1,445 |
| 35 | 3 | 29 | 0 | 2 | 37.4 min | 62.3 h | 2,242 | 3.2 | 0.08 | 8,816 | 0 |
| 40 | 4 | 39 | 450 | 2 | 1.2 h | 117.3 h | 4,222 | 1.7 | 0.04 | 12,864 | 767 |
| 45 | 4 | 39 | 0 | 2 | 1.2 h | 117.3 h | 4,222 | 1.7 | 0.04 | 10,764 | 0 |
| 49 | 4 | 39 | 0 | 2 | 1.2 h | 117.3 h | 4,222 | 1.7 | 0.04 | 5,640 | 0 |

The tier gate lags the rank, so the best recipe a researcher may start is between 1 and 10 levels below their rank, and the XP chart pays nothing for a recipe five or more levels below. 24 of the 40 Fabrication ranks 10-49 have no researchable recipe that pays XP at all if the award follows `GetDeltaXP`; research XP would arrive in bursts in the first four ranks of each tier. The third job slot (Fabrication 50) arrives when the skill stops paying XP, so it matters for blueprints only.

Ceilings: the largest award, as a share of base XP per job, that keeps research XP per hour under at-level Fabrication crafting XP per hour at that rank (the whole loop, gathering included), by two yardsticks.

| Yardstick | Lowest ceiling over the paying ranks | At Fabrication rank |
| --- | --- | --- |
| Per elapsed hour with every job slot busy (research runs while the player does other things) | 122% | 10 |
| Per hour of the player's own time at the terminal | 23% | 30 |

## Proposals for the owner

These are options with their measured effect, not code. All of them apply to the four crafting skills and their guilds only: combat and Piloting keep `GetRequiredXP` and `GetDeltaXP` as they are, and Espionage is excluded (`Craft.GetBaseRecipeXP`'s practice XP, `EspionageInfiltration.CalculateXp` and slicing's `Skill.CalculateEspionageXP` are untouched and pinned by tests). None moves the XP chart's cutoff (a craft five or more levels below the rank pays nothing), so quick craft stays at rank >= recipe level + 5 and never costs a player XP they could still earn.

### XP target

Today the four skills take Smithery 108 h, Engineering 63 h, Fabrication 56 h, Agriculture 37 h at mid gear with the model's unmeasured timings: a median of 59 h and a slowest of 108 h, against 137 h of RP XP. **Proposed target: the median crafting skill reaches rank 50 in 45 focused hours or fewer** (33% of the RP XP time), with the 50% guild award below. The target is set on the median skill because the slowest (Smithery) is slow from gathering, not from XP: 81% of its hours are spent gathering. Hours scale with 1 / multiplier, because the components and the craft time scale with the crafts, so one crafting-only multiplier moves every skill together and cannot close that gap.

What each target needs on top of the guild award. A multiplier of x1.00 means the award alone meets it:

| Median skill target (h) | Multiplier needed | Rank cost cap needed (XP) | Smithery h at that multiplier | Engineering h at that multiplier | Fabrication h at that multiplier | Agriculture h at that multiplier |
| --- | --- | --- | --- | --- | --- | --- |
| 45 | x1.02 | 14,039 | 70.7 | 49.3 | 40.7 | 31.6 |
| 35 | x1.32 | 8,225 | 55.1 | 38.2 | 31.8 | 24.5 |
| 25 | x1.97 | 5,305 | 38.5 | 27.5 | 22.5 | 16.8 |

| Option | Change | Smithery h | Engineering h | Fabrication h | Agriculture h | Median skill | Slowest skill | Migration of stored XP |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Today | nothing | 108.0 | 62.8 | 55.9 | 36.6 | 59.4 | 108.0 | no |
| 0 | hand in the guild's task recipes for credits and points, no XP award (isolates the recipe choice) | 87.8 | 64.7 | 52.4 | 39.2 | 58.5 | 87.8 | no |
| A | guild task XP only | 72.1 | 50.2 | 41.6 | 32.2 | 45.9 | 72.1 | no |
| B | A, and crafting XP x1.05 (crafts and hand-ins) | 68.7 | 47.9 | 39.6 | 30.7 | 43.7 | 68.7 | no |
| C | A, and no rank costs more than 12,700 XP | 68.6 | 47.5 | 39.9 | 30.7 | 43.7 | 68.6 | yes |

- **Row 0 is not an option.** It shows that handing in the guild's task recipes, with no award at all, already changes the hours, because the task recipes' components differ from the level's average recipe; the award's own effect is the difference between rows 0 and A.
- **A: guild task XP only.** No craft XP change; the smallest change, with no stored-data conversion. It takes the median skill to 46 h, which is 1 h short of the 45 h target.
- **B: A plus one crafting-only multiplier, x1.05.** The smallest round multiplier that brings the median skill to the target (44 h). One parameter on what crafts and hand-ins pay, retuned from the client timings later. It changes gains only, so stored XP and ranks need no conversion.
- **C: A plus a cap of 12,700 XP on any rank's cost.** The same median as B by a different route: it cuts the climb from 411,125 to 391,825 XP and spares the late ranks that cost up to 16,000 XP and need 15-25 crafts each, while leaving the early ranks alone. It changes required XP per rank for the crafting skills, so it needs a crafting-specific table and the plan's migration (convert stored XP and ranks so nobody loses a rank).

**Recommendation: B.** The guild award alone leaves the median skill at 46 h, so the target needs a crafting-only multiplier of about x1.05 on top; B reaches it with no migration. Treat the multiplier as a starting point: the model's hours move more with the node travel time (Smithery runs from 72 h at 15 s to 326 h at 120 s) than any option moves them, so measure it, re-run `progression`, and retune. Smithery stays far above the median under every option here because its components are scarce drops (gems and crate goods); that is a supply question for the gathering workstream, not an XP one.

### Guild task XP

Each crafting-guild hand-in awards XP in the guild's skill. **Formula:** award = share x `Skill.GetDeltaXP(level - rank)` x item quantity, with `level` the recipe level of a crafted task and the fish's `FishType` level of a catch task (such as `agr_tsk_017`), and `rank` the player's rank in the guild's skill. The chart's cutoff makes a hand-in five or more levels below the rank worth nothing, so the quick-crafted items that raise the guild rank pay no XP, and a task never pays more than crafting the same recipe would.

| Share of base XP | Smithery h | Smithery XP share | Engineering h | Engineering XP share | Fabrication h | Fabrication XP share | Agriculture h | Agriculture XP share |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| none | 108.0 | - | 62.8 | - | 55.9 | - | 36.6 | - |
| 25% | 78.9 | 11% | 56.5 | 13% | 46.3 | 12% | 35.3 | 10% |
| 50% | 72.1 | 20% | 50.2 | 23% | 41.6 | 21% | 32.2 | 18% |
| 100% | 62.4 | 33% | 41.3 | 37% | 34.7 | 34% | 27.5 | 30% |

- **Option 1, 25%.** Hand-ins supply 11% of the climb's XP: a small bonus that is easy to ignore.
- **Option 2, 50% (recommended).** Hand-ins supply 20%: a real part of the pace that does not replace crafting. Quantity matters for catch tasks: at this share a four-fish task's hand-in XP is 23% of the task's XP when the award is per fish and 7% when it is per task, so the award is per item.
- **Option 3, 100%.** Hand-ins supply 33% of the climb, and a crafter who hands in every craft effectively doubles their XP per craft. That is the largest change and should be a deliberate choice.

The credit faucet is the risk to weigh with this: every hand-in pays 1,000-7,200 credits and quick craft makes the items in seconds. If the owner wants a limit, the least invasive option is a per-day cap on guild task credits (not points or XP) per character; nothing here adds one.

### Research XP ceiling

Award per job (experiment and completion together) = share x `GetDeltaXP(recipe level - Fabrication rank)` of the recipe researched. The lowest ceiling over the paying ranks is 23%, from research XP per hour of the player's own time against the whole crafting loop. **Proposal: a share of 20% or less**, which keeps research XP per hour under at-level crafting XP per hour by both yardsticks at every rank, and a rule that a research award never exceeds the base XP of a craft of the same recipe level. Credits give a second bound: the first job on a new blueprint costs 200-4,200 credits and the level-10 job of a tier-4 recipe takes about five days. Because the chart pays nothing for recipes five or more levels below the rank, the owner may want a floor for research (like `GetPracticeXP`'s 150) so researchers are not paid only in the first four ranks of each tier; a floor must sit under the same ceiling.

## Limitations

- Seconds per action, craft overhead, node travel, turn-in time, research visits and seconds per fishing bite are unmeasured estimates. The client evidence replaces them; the command re-runs with the new values.
- Respawns, node contention, inventory trips, selling, restocking power cores and every other hour of real play are not modelled. These hours are a floor on a focused crafter's time, not a forecast of calendar days.
- Components no node supplies, and parts another recipe makes, are not timed; the sensitivity row prices them at a flat 20 s. Gems are priced as if self-gathered, although players trade them.
- One player policy (the legacy lookahead) at three gear sets. A better player or a different recipe choice changes quality, hence XP per craft. Stretch play (recipes up to three levels above the rank) and blueprints are not played.
- The crafter's Gathering rank is not a gate, XP bonuses (Social, food, DM) are not applied, and the total skill-rank cap and XP debt are ignored.
- Guild points follow the server's formula without Guild Relations or Social, and the cohort's catch-up uses one task item per tier. Credits are base rewards without the Guild Relations and Social bonuses.
- Fishing is shown for catch tasks only; Agriculture's fishing path is not part of the hours above, and fish availability by location is not modelled.
- The research cohort assumes the award follows `GetDeltaXP`; the Research workstream sets the real amount, and the ceiling is the bound on it.
