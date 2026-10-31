# Crafting revamp: progression pace baseline (workstream 11)

How long a new crafter takes today to reach rank 50 in each crafting skill and in Gathering, with the component gathering that supplies the crafts, measured with the legacy craft model (`design/testing/crafting-revamp/legacy-crafting-baseline.md`), and what the owner's October 2026 decisions change. The tables below the decisions are today's pace; the decisions are applied in code in workstream 11b and their resulting hours are in "Decisions applied". The XP-rate gate (gate 6) measures new minigames against today's XP first; the decisions apply on top of that result.

## Hours to rank 50 today

A new crafter (rank 0, nothing handed in to a guild) who always crafts recipes of their own level with the legacy lookahead player. Hours are craft window time (6.0 s per action plus 30 s per craft) plus the time to gather and refine the components (node travel is an unmeasured assumption, below). Mid gear is the headline; the band-by-band tables give all three gear levels.

| Skill | Crafts to 50 (mid gear) | Craft window h | Gathering h | Total h: no gear | Total h: mid gear | Total h: best gear | XP per hour (mid gear) | Component units no node supplies (mid gear) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Smithery | 536 | 20.0 | 77.1 | 104.2 | 97.1 | 90.4 | 4,234 | 873 |
| Engineering | 539 | 19.8 | 69.8 | 96.6 | 89.6 | 83.5 | 4,588 | 988 |
| Fabrication | 530 | 20.4 | 32.8 | 54.7 | 53.2 | 51.0 | 7,731 | 471 |
| Agriculture | 539 | 19.7 | 16.6 | 36.1 | 36.3 | 36.2 | 11,310 | 1818 |
| Gathering (the skill) | - | - | 3.1 | 3.1 | 3.1 | 3.1 | 133,429 | - |

Rank 50 costs 411,125 XP in every skill (`Skill.GetRequiredXP`, ranks 0-49). RP XP pays 3,000 XP per hour with no materials (1,500 base XP every 30 minutes; the public mechanism in `RoleplayXP.cs`), which is 137 hours for the same XP. Gathering takes 4.4 h by harvesting and refining alone, 3.5 h by scavenging alone and 3.1 h taking the better of the two at each rank.

What the numbers say:

- **Crafts, not just components.** Mid gear needs about 536 at-level crafts to reach rank 50, not the 340-420 in the plan's research: that range assumes near-full quality, and the lookahead player's quality at mid gear is 30% on average (quality adds up to 100% to a craft's XP).
- **Pace.** The craft loop pays 4,234-11,310 XP per hour with its gathering, 1.4-3.8 times what RP XP pays. Players who bypass crafting with RP XP are buying out the materials and gathering, not a faster rate.
- **Gathering dominates Smithery.** 79% of its hours are gathering. The three components that cost the most are `lth_good` (18.0 h), `ruby` (10.4 h), `lth_high` (8.6 h); each drops on a small share of rolls (gems 2-9%, crate goods 15%), so one unit costs 3.2-7.4 minutes.
- **Gathering is quick for the skill itself.** Each harvested item pays up to 1,200 XP and refining pays 25-150 XP per unit at the ore's level (the model counts that at every rank), so Gathering rank 50 takes hours, not days, if nodes are free. Node respawns and contention are not modelled.
- **Agriculture has a faster path than the one modelled here.** Fishing pays the same chart per bite (600-1,200 XP); see the catch task table in the guild section.

## Decisions applied (owner, October 2026)

1. **Guild hand-ins give XP.** The Smithery, Engineering, Fabrication and Agriculture guilds pay 50% of `Skill.GetDeltaXP(task level - rank)` for every item handed in, in the guild's skill (`GuildTaskXp`), with the task level the recipe level for a crafted item and the `FishType` level for a catch task. It goes through the crafting XP path, so Social and food bonuses apply, and the chart's cutoff stays: a hand-in five or more levels below the rank pays nothing, as crafting and quick craft (rank at least level + 5) do. The Hunter's Guild gives none.
2. **Crafting XP x1.05.** `CraftMinigameXp.CraftingPaceMultiplier` multiplies the base XP of crafts in Smithery, Engineering, Fabrication and Agriculture's cooking, on top of each minigame's gate-6 band multiplier. It does not apply to Gathering, Espionage, fishing, guild hand-ins, research, combat or Piloting. Gate 6 is still measured against today's XP, before this multiplier.
3. **Research XP scaled.** `ResearchXpRules` now pays 62% (experiment) and 31% (completion) of the chart XP, +10% per research level, down from 100% and 50%, so research XP per real hour stays under at-level Fabrication crafting XP per hour (see the research cohort).

Hours to rank 50 at mid gear, today against with decisions 1 and 2 (hand-ins at the recommended award, crafts at the pace multiplier; the same unmeasured timings as the rest of this report):

| Skill | Today h | With the decisions h | Change | Crafts to 50 today | Crafts to 50 with the decisions | Hand-in XP share of the climb | At-level hand-ins | Catch-up hand-ins (quick craft) | Catch-up h (in the total) | Guild rank at rank 50 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Smithery | 97.1 | 66.0 | -32% | 536 | 407 | 20% | 269 | 35 | 0.7 | 5 |
| Engineering | 89.6 | 75.5 | -16% | 539 | 501 | 2% | 31 | 178 | 2.1 | 4 |
| Fabrication | 53.2 | 39.1 | -26% | 530 | 401 | 20% | 273 | 32 | 0.6 | 5 |
| Agriculture | 36.3 | 30.3 | -17% | 539 | 424 | 17% | 230 | 40 | 0.2 | 5 |
| Median skill | 71.4 | 52.6 | -26% | - | - | - | - | - | - | - |
| Slowest skill | 97.1 | 75.5 | -22% | - | - | - | - | - | - | - |

The proposed target was the median skill at 45 h or fewer. With the decisions it is 52.6 h, 7.6 h over it. An at-level hand-in is one of the crafter's own crafts of a task recipe, which the model makes whenever the ten-task board holds one, so it adds no craft. A catch-up hand-in is a quick-crafted item that raises the guild rank when the rank stands in the way; its quick-craft and turn-in time is in the hours above and its XP is not paid (quick-crafted items sit five or more levels below the rank).

Research under the new values: the highest research XP per elapsed hour against at-level crafting XP per hour (with the x1.05) is 99% at Fabrication 10, under the plan's ceiling at every rank. Before the change the starting values (100% and 50%) paid 167% at Fabrication 10 in the baseline.

## Inputs and assumptions

- Command: `SWLOR.CraftSim progression --runs 500 --seed 1 --out design/testing/crafting-revamp/progression-pace-baseline.md`
- Seed 1; 500 lookahead crafts per recipe level, skill and gear level (8 sampled futures per action), each from its own stream of the seed, so a run repeats exactly. The hours themselves are expected values, not sampled.
- Recipes: the active recipes in the server's definitions that a new crafter can make without learning them first, of all the active ones (Smithery 579 of 831; Engineering 2085 of 2200; Fabrication 318 of 464; Agriculture 290 of 324). The crafter's rank steps from 0 to 49 and each step crafts recipes of level max(1, rank): the `Skill.GetDeltaXP` price for the level equals the rank is 600 XP (676 at rank 0). Each of a level's recipes counts equally, as in the gates' cohort.
- Gear: none, tier 3 (mid) and tier 5 (best) of the skill's own crafting gear (`legacy-crafting-baseline.md`, "Gear levels"). Food and status bonuses are not played.
- Craft player and XP: the legacy lookahead (`RolloutPolicy`) at the recipe's level and rank. A success pays the base XP plus the quality share; a failure pays 15% of the base (`CraftOutcomeRules`). No blueprint, no enhancements, no XP bonuses from Social or food, and no first-craft bonus unless the variety row below says so.
- Time in the craft window: 6.0 s per action (the legacy window's estimate) plus **30 s per craft** to open the station, pick the recipe, commit the components and collect the item. The overhead is an unmeasured estimate; the gates' own overhead defaults to 0. The sensitivity table shows 15 s and 60 s.
- Components: a craft commits every component stack of the recipe and a failure loses each stack on 65% of rolls (`CraftViewModel.cs:1601-1641`), so a craft consumes success + 0.65 x failure of the committed units. A unit is priced at the cheapest loot table in `HarvestingLootTableDefinition` that drops it, worked the way the game works that table. Ore veins and trees are harvested (`res_used` on the placeable): each harvester use takes 5.0 s (the item's `Delay(5f)`) plus its share of 30 s node travel over the node's 2.5 average harvests, and rolls the table once (plus the Might bonus roll; Might modifier 0). Bushes, herbs, vegetable patches and supply crates are scavenge sites (`scav_opened`, `SCAVENGE_POINT_LOOT_TABLE_NAME`): a visit costs the 30 s walk plus 2.0 s for the one search animation of the visit (a second attempt adds no time), the site can be searched once, and each successful search (`d20 + Perception >= 6`, then 6 + d3 on the second attempt that Hard Look gives) is one roll of the table, giving 1 to the item's maximum quantity. Hard Look follows the gatherer's Gathering tier (`CraftRankMilestones.GetHardLookLevel`), which the model sets to the tier of the crafter's own crafting rank at each step (tier 1 below rank 10, one more every 10 ranks) and never below the site's level, so a higher-rank crafter searches low-level sites twice more often; the Perception modifier is 0. A roll yields one item of the table, so a recipe that needs two items of one table is priced at the expected visits until both counts are met (the expected maximum of the items' independent arrival times, checked against an exact multinomial calculation in the tests), which lies between the scarcer item's time and the sum of both.
- Refined ore (`ref_*`) costs the raw ore plus refining: 6 s per operation (`RefiningDelaySeconds`) over a batch of 99 units (a full 99-unit stack; the refinery accepts several stacks per operation, so this is cautious). The sensitivity table also prices a batch of one power core (4 units). Power cores cost credits, not time.
- Asteroid ores (`ore_*`, the Engineering recipes' `ref_tilarium` and the like) are priced like a ground node of the same tier. Space mining is not modelled.
- Node travel: **30 s** between nodes, unmeasured. Respawn waits and other players at the node are not modelled, so gathering hours are a lower bound. Gathering rank is not a gate: the crafter is assumed to reach every node tier the recipe needs.
- Components no node supplies (enemy and crate drops of leather and electronics, meat, vendor goods, parts another recipe makes) are left out of the hours and counted in the summary column. The sensitivity table prices them at 20 s a unit, about a harvested unit.
- Gathering the skill: each rank uses the better of harvesting and refining the best ground vein the tier allows (`CraftRankMilestones.TierForRank`) and searching the best scavenge site, at 30 s between nodes or sites and 2.0 s of search animation a visit. Perception modifier 0.

Rules copied from the server (the model calls everything else directly), each checked against its source by `ProgressionSourceTests` or `LegacySourceTests`, except the loss rule of the deleted `CraftViewModel.cs`, which is frozen at commit `ded6cfaf45b66b0a5c4ffe66447bf9c3e24a2ff7`:

| Rule | Source |
| --- | --- |
| RP XP per hour | `RoleplayXP.cs:16-93` |
| Guild points per hand-in | `Guild.cs:62-80` |
| Ten guild tasks per tier on the board | `Guild.cs:124-170` |
| Seconds per harvester use | `ProspectingRules.cs:71` |
| Seconds of the scavenge animation | `SearchRules.cs:34` |
| Seconds per refining operation | `RefineryViewModel.cs:58-64` |
| Research job length and credits | `Craft.cs:1145-1192` |
| A first research job creates the blueprint | `ResearchViewModel.cs:517-530` |
| Components and enhancements lost on failure | `CraftViewModel.cs:1601-1641` |
| Recipe rank requirement | `Craft.cs:327-331` |
| Harvests per node | `ProspectingNode.cs:67` |
| Harvest XP | `GatheringLevels.cs:20-23` |
| Scavenge chances at the reference Perception modifier | `ScavengeQuickSearch.cs:38-49` |
| Scavenge second attempt | `ScavengeQuickSearch.cs:97-99` |
| Scavenge success roll and DC | `ScavengeQuickSearch.cs:101-108` |
| Scavenge XP level | `ScavengePoint.cs:93` |
| Ore tiers and XP per unit | `RefineryViewModel.cs:35-52` |
| Refinery batch size | `RefineryViewModel.cs:61` |
| Power cores per batch | `RefineryViewModel.cs:172-177` |
| Refining XP | `RefineryViewModel.cs:356-360` |

## Hours per 10-rank band

A band is the hours to climb from its first rank to the next band's first rank (0-10 is rank 0 to rank 10). Crafts, craft window and gathering hours are mid gear; the three total columns give each gear level. "RP XP h" is the hours RP XP needs for the band's XP.

### Smithery

| Ranks | XP | Crafts (mid) | Craft window h | Gathering h | Total h: no gear | Total h: mid gear | Total h: best gear | XP per hour (mid) | RP XP h |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0-10 | 17,545 | 28 | 0.5 | 1.1 | 1.7 | 1.6 | 1.5 | 10,872 | 5.8 |
| 10-20 | 50,960 | 77 | 2.1 | 4.3 | 6.9 | 6.4 | 5.8 | 8,008 | 17.0 |
| 20-30 | 89,340 | 130 | 4.4 | 19.9 | 25.1 | 24.3 | 22.6 | 3,679 | 29.8 |
| 30-40 | 107,950 | 159 | 6.0 | 27.9 | 35.2 | 33.9 | 32.3 | 3,185 | 36.0 |
| 40-50 | 145,330 | 142 | 7.1 | 23.8 | 35.3 | 31.0 | 28.2 | 4,694 | 48.4 |
| 0-50 | 411,125 | 536 | 20.0 | 77.1 | 104.2 | 97.1 | 90.4 | 4,234 | 137.0 |

### Engineering

| Ranks | XP | Crafts (mid) | Craft window h | Gathering h | Total h: no gear | Total h: mid gear | Total h: best gear | XP per hour (mid) | RP XP h |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0-10 | 17,545 | 28 | 0.5 | 0.5 | 1.1 | 1.0 | 1.0 | 17,168 | 5.8 |
| 10-20 | 50,960 | 77 | 2.0 | 2.4 | 4.8 | 4.4 | 4.2 | 11,506 | 17.0 |
| 20-30 | 89,340 | 131 | 4.2 | 14.9 | 19.6 | 19.1 | 18.1 | 4,669 | 29.8 |
| 30-40 | 107,950 | 160 | 5.9 | 21.9 | 28.6 | 27.9 | 26.8 | 3,875 | 36.0 |
| 40-50 | 145,330 | 143 | 7.1 | 30.1 | 42.5 | 37.2 | 33.5 | 3,911 | 48.4 |
| 0-50 | 411,125 | 539 | 19.8 | 69.8 | 96.6 | 89.6 | 83.5 | 4,588 | 137.0 |

### Fabrication

| Ranks | XP | Crafts (mid) | Craft window h | Gathering h | Total h: no gear | Total h: mid gear | Total h: best gear | XP per hour (mid) | RP XP h |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0-10 | 17,545 | 28 | 0.5 | 0.8 | 1.3 | 1.3 | 1.2 | 13,565 | 5.8 |
| 10-20 | 50,960 | 76 | 2.2 | 2.5 | 4.9 | 4.6 | 4.3 | 10,970 | 17.0 |
| 20-30 | 89,340 | 130 | 4.4 | 13.8 | 18.5 | 18.3 | 17.2 | 4,889 | 29.8 |
| 30-40 | 107,950 | 158 | 6.1 | 7.7 | 13.3 | 13.7 | 14.0 | 7,859 | 36.0 |
| 40-50 | 145,330 | 139 | 7.2 | 8.0 | 16.6 | 15.2 | 14.3 | 9,540 | 48.4 |
| 0-50 | 411,125 | 530 | 20.4 | 32.8 | 54.7 | 53.2 | 51.0 | 7,731 | 137.0 |

### Agriculture

| Ranks | XP | Crafts (mid) | Craft window h | Gathering h | Total h: no gear | Total h: mid gear | Total h: best gear | XP per hour (mid) | RP XP h |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0-10 | 17,545 | 28 | 0.5 | 0.8 | 1.4 | 1.3 | 1.2 | 13,103 | 5.8 |
| 10-20 | 50,960 | 77 | 2.0 | 2.1 | 4.4 | 4.1 | 3.9 | 12,397 | 17.0 |
| 20-30 | 89,340 | 131 | 4.2 | 2.8 | 6.4 | 7.0 | 7.3 | 12,726 | 29.8 |
| 30-40 | 107,950 | 160 | 5.9 | 6.5 | 11.9 | 12.4 | 12.8 | 8,728 | 36.0 |
| 40-50 | 145,330 | 143 | 7.1 | 4.4 | 12.0 | 11.5 | 10.9 | 12,626 | 48.4 |
| 0-50 | 411,125 | 539 | 19.7 | 16.6 | 36.1 | 36.3 | 36.2 | 11,310 | 137.0 |

## Gathering rank 50

Hours per band for a new gatherer. Harvesting pays `GetDeltaXP(vein level - rank)` for every item, and a vein two tiers below the rank pays nothing, so the gatherer always works the top vein the tier allows; refining pays 25-150 XP per raw unit, counted at every rank.

| Ranks | XP | Harvest and refine h | Scavenge h | Better of the two h | XP per hour (better) | RP XP h |
| --- | --- | --- | --- | --- | --- | --- |
| 0-10 | 17,545 | 0.31 | 0.19 | 0.17 | 101,407 | 5.8 |
| 10-20 | 50,960 | 0.69 | 0.49 | 0.43 | 117,560 | 17.0 |
| 20-30 | 89,340 | 1.02 | 0.78 | 0.69 | 129,082 | 29.8 |
| 30-40 | 107,950 | 1.10 | 0.88 | 0.78 | 137,556 | 36.0 |
| 40-50 | 145,330 | 1.28 | 1.11 | 1.00 | 145,645 | 48.4 |
| 0-50 | 411,125 | 4.40 | 3.46 | 3.08 | 133,429 | 137.0 |

The plan's quick gathering and the minigame sessions that replace the harvester and the search, for the same climb. **Quick Harvest** resolves a node at today's expected units and XP, one harvest delay per unit, and **Quick Search** resolves a site at today's expected rolls and XP, taking as long as today's attempts, so each is today's loop by construction and neither can train Gathering faster than the rows below. A played session may exceed it by the gate's starting value of 15%; the minigames are not built yet, so that row is the cap the parity gate enforces, not a measurement.

| Cohort | Gathering XP per hour (rank 0 to 50) | Hours to rank 50 | Ceiling the plan sets |
| --- | --- | --- | --- |
| Quick Harvest (today's harvesting and refining) | 93,472 | 4.40 | today's rate, never above it |
| Quick Search (today's scavenging) | 118,795 | 3.46 | today's rate, never above it |
| Played Prospecting session at the parity cap | 107,493 | 3.82 | at most 15% above today's |
| Played Search session at the parity cap | 136,614 | 3.01 | at most 15% above today's |

Sensitivity of the better-of-two total to the node travel assumption, the only unmeasured input that moves it:

| Seconds between nodes | Harvest and refine h | Scavenge h | Better of the two h |
| --- | --- | --- | --- |
| 15 | 2.9 | 1.8 | 1.8 |
| 30 | 4.4 | 3.5 | 3.1 |
| 60 | 7.5 | 6.7 | 5.6 |
| 120 | 13.7 | 13.2 | 10.7 |
| 300 | 32.2 | 32.7 | 25.8 |

## What supplies the components

Share of component units the at-level recipes commit, by where the model says they come from, over the whole climb. A unit only counts as gathered when a gathering loot table in `HarvestingLootTableDefinition` drops it (or its raw ore does).

| Skill | Units per climb (mid gear) | Refined ore | Harvested items | Made by another recipe | Other (drops, vendor, quest) |
| --- | --- | --- | --- | --- | --- |
| Smithery | 4048 | 33% | 46% | 0% | 21% |
| Engineering | 2798 | 43% | 22% | 19% | 16% |
| Fabrication | 3022 | 39% | 46% | 5% | 10% |
| Agriculture | 2438 | 0% | 25% | 0% | 75% |

Gathering hours of the mid gear climb by kind of node. Each component is priced at the cheapest table that drops it. Ore veins and trees are harvested; bushes, herbs, vegetable patches and supply crates are scavenge sites; asteroid tables are mined from a ship and priced like a harvest node:

| Skill | GAS_TIBANNA | asteroids | bushes | herbs | ore veins | supply crates | trees | vegetable patches |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Smithery | 0.0 | 1.3 | 8.1 | 0.0 | 29.1 | 36.8 | 1.9 | 0.0 |
| Engineering | 0.0 | 35.7 | 0.6 | 0.0 | 11.3 | 22.2 | 0.0 | 0.0 |
| Fabrication | 0.0 | 0.1 | 3.2 | 0.0 | 5.7 | 18.3 | 5.4 | 0.0 |
| Agriculture | 0.0 | 0.0 | 0.1 | 2.5 | 0.0 | 0.0 | 0.0 | 14.0 |

The five components that cost the most gathering hours in each skill's mid gear climb:

| Skill | Component | Gathering h | Share of the skill's gathering |
| --- | --- | --- | --- |
| Smithery | `lth_good` | 18.0 | 23% |
| Smithery | `ruby` | 10.4 | 14% |
| Smithery | `lth_high` | 8.6 | 11% |
| Smithery | `citrine` | 7.3 | 9% |
| Smithery | `elec_good` | 5.7 | 7% |
| Engineering | `elec_good` | 13.0 | 19% |
| Engineering | `diamond` | 9.3 | 13% |
| Engineering | `katrium` | 9.0 | 13% |
| Engineering | `elec_high` | 7.6 | 11% |
| Engineering | `zinsiam` | 4.9 | 7% |
| Fabrication | `lth_good` | 7.2 | 22% |
| Fabrication | `elec_good` | 6.3 | 19% |
| Fabrication | `elec_high` | 3.9 | 12% |
| Fabrication | `aracia_wood` | 1.9 | 6% |
| Fabrication | `ref_keromber` | 1.7 | 5% |
| Agriculture | `tomato` | 2.1 | 13% |
| Agriculture | `melon` | 1.8 | 11% |
| Agriculture | `plant_butter` | 1.4 | 8% |
| Agriculture | `mushroom` | 1.2 | 7% |
| Agriculture | `ginger` | 1.2 | 7% |

## Sensitivity

Total hours to rank 50 at mid gear when one unmeasured input or play choice changes. Every other row is the baseline above.

| Scenario | Smithery | Engineering | Fabrication | Agriculture |
| --- | --- | --- | --- | --- |
| Baseline | 97.1 | 89.6 | 53.2 | 36.3 |
| Overhead 15 s per craft | 94.9 | 87.4 | 51.0 | 34.1 |
| Overhead 60 s per craft | 101.6 | 94.1 | 57.6 | 40.8 |
| Node travel 15 s | 64.7 | 62.3 | 39.1 | 28.6 |
| Node travel 60 s | 161.9 | 144.2 | 81.3 | 51.9 |
| Node travel 120 s | 291.4 | 253.3 | 137.5 | 83.1 |
| Might modifier +2 (half the harvests drop a second item) | 86.4 | 73.9 | 49.4 | 36.3 |
| Refining one power core (4 units) per operation | 97.6 | 90.1 | 53.6 | 36.3 |
| Components no node supplies cost 20 s a unit | 102.0 | 95.1 | 55.8 | 46.5 |
| Crafts the cheapest quarter of each level's recipes | 44.7 | 27.0 | 31.7 | 19.7 |
| Rotates through each level's recipes (20% first-craft bonus on each) | 85.6 | 75.3 | 47.8 | 33.1 |

## Guild task cohort

With the task cap lifted, a completed task goes back on the board at once for the Smithery, Engineering, Fabrication and Agriculture guilds, so hand-ins are a repeatable activity. A task wants one item (four raw fish for a catch task) and pays credits and guild points by its tier, which is the guild rank that unlocks it. Credits, guild points and the rank thresholds are unchanged.

| Guild | Tasks | Crafted | Caught | Other | Tier 0-4 recipe or fish levels | Credits per task by tier | Base guild points by tier |
| --- | --- | --- | --- | --- | --- | --- | --- |
| SmitheryGuild | 185 | 185 | 0 | 0 | 1-9, 11-19, 21-29, 31-39, 41-49 | 1,000 / 2,500 / 4,000 / 5,500 / 7,200 | 21 / 81 / 117 / 156 / 195 |
| EngineeringGuild | 80 | 80 | 0 | 0 | 2-5, 5-10, 5-20, 15-30, 5-40 | 1,000 / 2,500 / 4,000 / 5,500 / 7,200 | 21 / 81 / 117 / 156 / 195 |
| FabricationGuild | 211 | 211 | 0 | 0 | 1-10, 11-20, 21-30, 31-40, 41-50 | 1,000 / 2,500 / 4,000 / 5,500 / 7,200 | 21 / 81 / 117 / 156 / 195 |
| AgricultureGuild | 185 | 132 | 51 | 2 | 0-10, 10-20, 20-30, 30-40, 40-50 | 1,000 / 2,500 / 4,000 / 5,500 / 7,200 | 21 / 81 / 117 / 156 / 195 |

Every crafted task asks for a player-produced item, which includes quick-crafted ones (`NwnCraftSessionWorld.CreateCraftedItem` stamps both). Tier k holds the recipes of levels 10k+1 to 10k+10. Guild rank 5 needs 45,000 GP, about 238 hand-ins when each is handed in at the tier of the rank it climbs from.

### Cohort: hand in what you craft

The cohort crafts at level as before and, whenever the board has a task for that recipe level and the guild rank allows it, hands the item in. The board is the catch: `Guild.RefreshGuildTasks` keeps only 10 random tasks of a tier at each server start (all of them when the tier has 10 or fewer), the same for every player, and the cap change does not touch that. A level's task is therefore on the board with a chance, and the model weights every hand-in, its XP, points and credits by it (the chance that at least one of the level's tasks is among the ten drawn; Agriculture's catch tasks take board places too). A crafter who finds no task for their level on this boot's board hands nothing in, so a climb over many server starts gets the expected share. A hand-in asks for one exact recipe (the quest collector matches the item's resref), so when the board holds the level's task the crafter makes that recipe, not an arbitrary one of the level: the craft's components are the level's task recipes' mean, not the level's average over all recipes (a crafter who picks recipes by cost can do so with or without a guild; see the sensitivity table). Each hand-in costs 10 s (unmeasured). When the guild rank is what stands in the way, the crafter quick-crafts the preferred task item of their current tier that is on the board and that quick craft allows (the cheapest to gather; rank at least level + 5, after one craft by hand; 2 s per item, components gathered as above) and hands it in until the rank rises; the cost is the expected cost over which tasks the board holds, given that it holds one. A board with no eligible item makes the crafter wait for the next server start. The wait is not play time and is not charged, but the crafts made meanwhile cannot be handed in, so they are made without the award for the expected number of missed boards (4 play hours per server start, unmeasured); the table gives the lowest chance of such a board and the expected server starts waited. Those items pay no XP. The award is a share of the craft's base XP (`GetDeltaXP(task level - rank)`, the same cutoff as crafting), so a hand-in five or more levels below the rank pays nothing and quick-crafted items never do.

| Skill | Award share | Total h (mid gear) | Change vs no guild | Change vs the 0% award | At-level hand-ins | Catch-up hand-ins | Catch-up h | Lowest chance a board held a catch-up item | Server starts waited | Guild rank at rank 50 | Hand-in XP | Share of the climb's XP | Credits earned |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Smithery | no guild | 97.1 | - | - | - | - | - | - | - | - | - | - | - |
| Smithery | 0% (hand-ins for credits and points only) | 83.8 | -14% | - | 361 | 28 | 0.6 | 100% | 0.00 | 5 | 0 | 0% | 1,800,437 |
| Smithery | 25% | 75.3 | -23% | -10% | 315 | 31 | 0.6 | 100% | 0.00 | 5 | 47,235 | 11% | 1,584,993 |
| Smithery | 50% | 68.7 | -29% | -18% | 279 | 33 | 0.7 | 100% | 0.00 | 5 | 83,758 | 20% | 1,418,561 |
| Smithery | 100% | 59.3 | -39% | -29% | 228 | 47 | 1.0 | 100% | 0.00 | 5 | 136,721 | 33% | 1,208,647 |
| Engineering | no guild | 89.6 | - | - | - | - | - | - | - | - | - | - | - |
| Engineering | 0% (hand-ins for credits and points only) | 79.7 | -11% | - | 46 | 167 | 2.0 | 88% | 0.14 | 4 | 0 | 0% | 754,127 |
| Engineering | 25% | 79.4 | -11% | -0% | 38 | 173 | 2.0 | 88% | 0.14 | 4 | 5,717 | 1% | 744,875 |
| Engineering | 50% | 79.1 | -12% | -1% | 33 | 177 | 2.1 | 88% | 0.14 | 4 | 9,828 | 2% | 737,608 |
| Engineering | 100% | 78.8 | -12% | -1% | 26 | 182 | 2.1 | 88% | 0.14 | 4 | 15,369 | 4% | 726,933 |
| Fabrication | no guild | 53.2 | - | - | - | - | - | - | - | - | - | - | - |
| Fabrication | 0% (hand-ins for credits and points only) | 51.3 | -4% | - | 361 | 26 | 0.5 | 100% | 0.00 | 5 | 0 | 0% | 1,758,220 |
| Fabrication | 25% | 45.3 | -15% | -12% | 318 | 29 | 0.6 | 100% | 0.00 | 5 | 47,669 | 12% | 1,563,004 |
| Fabrication | 50% | 40.6 | -24% | -21% | 284 | 31 | 0.6 | 100% | 0.00 | 5 | 85,170 | 21% | 1,408,821 |
| Fabrication | 100% | 33.9 | -36% | -34% | 234 | 39 | 0.8 | 100% | 0.00 | 5 | 140,565 | 34% | 1,193,099 |
| Agriculture | no guild | 36.3 | - | - | - | - | - | - | - | - | - | - | - |
| Agriculture | 0% (hand-ins for credits and points only) | 38.4 | 6% | - | 295 | 31 | 0.1 | 100% | 0.00 | 5 | 0 | 0% | 1,446,620 |
| Agriculture | 25% | 34.6 | -5% | -10% | 264 | 33 | 0.1 | 100% | 0.00 | 5 | 39,661 | 10% | 1,308,335 |
| Agriculture | 50% | 31.6 | -13% | -18% | 240 | 38 | 0.2 | 100% | 0.00 | 5 | 71,942 | 17% | 1,206,094 |
| Agriculture | 100% | 27.0 | -26% | -30% | 203 | 53 | 0.3 | 100% | 0.00 | 5 | 121,566 | 30% | 1,076,501 |

Read the award's effect against the 0% row, not against no guild: handing in the task recipes already changes the hours at a 0% award, because their components differ from the level's average recipe. Hand-in XP's share of the climb is the figure to read for the award: the proposal should land it between a fifth and a third, so it neither dominates nor vanishes.

The same cohort by rank band at the recommended 50% award. The task tier a crafter can hand in at is capped by the guild rank, which the catch-up hand-ins raise; one item per task, taken from the crafter's own craft or, for the catch-up, from quick craft.

| Skill | Ranks | Task tier at level | Items per task | At-level hand-ins | Quick-crafted catch-up hand-ins | Hand-in hours (turn-in and catch-up) | Hand-in XP | Share of the band's XP |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Smithery | 0-10 | 0 | 1 | 15 | 0 | 0.04 | 4,439 | 25% |
| Smithery | 10-20 | 1 | 1 | 39 | 33 | 0.76 | 11,802 | 23% |
| Smithery | 20-30 | 2 | 1 | 67 | 0 | 0.23 | 20,036 | 22% |
| Smithery | 30-40 | 3 | 1 | 79 | 0 | 0.22 | 23,785 | 22% |
| Smithery | 40-50 | 4 | 1 | 79 | 0 | 0.22 | 23,697 | 16% |
| Engineering | 0-10 | 0 | 1 | 3 | 0 | 0.01 | 903 | 5% |
| Engineering | 10-20 | 1 | 1 | 4 | 138 | 1.71 | 1,300 | 3% |
| Engineering | 20-30 | 2 | 1 | 7 | 0 | 0.02 | 2,055 | 2% |
| Engineering | 30-40 | 3 | 1 | 10 | 0 | 0.03 | 3,030 | 3% |
| Engineering | 40-50 | 4 | 1 | 8 | 39 | 0.39 | 2,540 | 2% |
| Fabrication | 0-10 | 0 | 1 | 14 | 0 | 0.04 | 4,264 | 24% |
| Fabrication | 10-20 | 1 | 1 | 39 | 31 | 0.70 | 11,846 | 23% |
| Fabrication | 20-30 | 2 | 1 | 68 | 0 | 0.19 | 20,412 | 23% |
| Fabrication | 30-40 | 3 | 1 | 82 | 0 | 0.23 | 24,646 | 23% |
| Fabrication | 40-50 | 4 | 1 | 80 | 0 | 0.22 | 24,002 | 17% |
| Agriculture | 0-10 | 0 | 1 | 12 | 0 | 0.03 | 3,547 | 20% |
| Agriculture | 10-20 | 1 | 1 | 33 | 34 | 0.24 | 10,036 | 20% |
| Agriculture | 20-30 | 2 | 1 | 59 | 4 | 0.20 | 17,647 | 20% |
| Agriculture | 30-40 | 3 | 1 | 71 | 0 | 0.20 | 21,243 | 20% |
| Agriculture | 40-50 | 4 | 1 | 65 | 0 | 0.18 | 19,470 | 13% |

### The guild rank chore

What raising a guild from rank 0 to 5 costs a rank-50 crafter who quick-crafts the cheapest eligible task item of each tier that is on the board and hands it in (a board with none makes them wait for the next server start, which is not charged). Before the change the board held ten tasks a tier per server start, so rank 5 took at least 14-21 days.

| Guild | Hand-ins | Hours | Credits earned | Credits per hour | Lowest chance a board holds an eligible item | Server starts waited (not charged) |
| --- | --- | --- | --- | --- | --- | --- |
| SmitheryGuild | 238 | 5.2 | 954,321 | 184,304 | 100% | 0.00 |
| EngineeringGuild | 238 | 2.2 | 954,321 | 427,602 | 100% | 0.00 |
| FabricationGuild | 238 | 3.4 | 954,321 | 284,549 | 100% | 0.00 |
| AgricultureGuild | 238 | 1.1 | 954,321 | 873,641 | 100% | 0.00 |

| Guild | Tier | Item | Item level | Guild points each | Hand-ins | Hours | Chance a board holds an eligible item |
| --- | --- | --- | --- | --- | --- | --- | --- |
| SmitheryGuild | 0 | `b_knife` | 1 | 21 | 48 | 0.89 | 100% |
| SmitheryGuild | 1 | `tit_knife` | 11 | 101 | 40 | 0.78 | 100% |
| SmitheryGuild | 2 | `del_knife` | 21 | 175 | 57 | 1.34 | 100% |
| SmitheryGuild | 3 | `proto_knife` | 31 | 273 | 55 | 1.13 | 100% |
| SmitheryGuild | 4 | `oph_knife` | 41 | 390 | 38 | 1.04 | 100% |
| EngineeringGuild | 0 | `sm_capb_00` | 2 | 21 | 48 | 0.46 | 100% |
| EngineeringGuild | 1 | `sm_capb_10` | 10 | 101 | 40 | 0.39 | 100% |
| EngineeringGuild | 2 | `sm_capb_20` | 20 | 175 | 57 | 0.51 | 100% |
| EngineeringGuild | 3 | `sm_capb_30` | 30 | 273 | 55 | 0.50 | 100% |
| EngineeringGuild | 4 | `sm_capb_40` | 40 | 390 | 38 | 0.37 | 100% |
| FabricationGuild | 0 | `structure_0132` | 1 | 21 | 48 | 0.76 | 100% |
| FabricationGuild | 1 | `structure_0026` | 11 | 101 | 40 | 0.56 | 100% |
| FabricationGuild | 2 | `structure_0004` | 21 | 175 | 57 | 0.80 | 100% |
| FabricationGuild | 3 | `structure_0155` | 31 | 273 | 55 | 0.67 | 100% |
| FabricationGuild | 4 | `structure_0163` | 41 | 390 | 38 | 0.57 | 100% |
| AgricultureGuild | 0 | `mynock_broth` | 3 | 21 | 48 | 0.18 | 100% |
| AgricultureGuild | 1 | `raivor_broth` | 13 | 101 | 40 | 0.17 | 100% |
| AgricultureGuild | 2 | `ara_broth` | 23 | 175 | 57 | 0.24 | 100% |
| AgricultureGuild | 3 | `womp_broth` | 33 | 273 | 55 | 0.23 | 100% |
| AgricultureGuild | 4 | `wild_broth` | 43 | 390 | 38 | 0.28 | 100% |

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

Every hand-in consumes one real player-crafted item (or four caught fish), which already limits the rate: the components. Nothing else limits it once the cap is lifted: the board's ten tasks per tier no longer bound it, because a completed task returns at once and one task can be repeated. Quick craft makes a base item in 2 s, so for a high-rank crafter the real bounds are the components and the turn-in time. Credits are the lever that matters: a task pays 1,000 to 7,200 credits plus the Guild Relations and Social bonuses, and the Smithery rank climb alone pays 954,321 credits for 5.2 hours of work. Nothing was added in this PR; the owner decides whether credits need a limit (see the proposals).

## Research cohort

Research is a Fabrication XP source in the revamp. The Research workstream set starting values in `ResearchXpRules`: the experiment pays 62% of the chart XP at full effort and completing the job 31%, each raised 10% per research level reached, where the chart XP is `GetDeltaXP(recipe level - Fabrication rank)`. This cohort is a Fabrication researcher at the tier their rank opens, doing the job loop that pays the most XP per hour, and it calls those rules directly. A job's length in seconds and its credit cost are one formula, `200 x tier^2.2 x level^2` (tier = recipe level / 10 + 1, at most 5; `Craft.CalculateResearchCost`), where level is the blueprint's new research level 1-10. A recipe's blueprint is first acquired by starting its level-1 job with no blueprint, so "blueprint acquisition" is that job. The tier gate is Fabrication rank / 10 and the job slots are 1, 2 from rank 25 and 3 from rank 50 (`CraftRankMilestones`). The experiment cuts up to 25% of a job's time (`ExperimentRules.TimeCutCapPercent`), which the cohort takes as the best case.

Time at the terminal per job: an experiment of 6 trials at 6 s, plus 30 s to start the job and 30 s to collect it (96 s; unmeasured). The recipe researched is the one the research window lists (`Craft.IsResearchableRecipe`: weapons, armor and food, and the space revamp's ship modules and deeds) that needs no learning and whose level pays the most against the rank within the tier. "Fast loop" keeps every slot on level-1 jobs for new blueprints; "climb" runs each slot up one blueprint from level 1 to 10. Base XP is the chart XP for the recipe level against the rank.

| Fabrication rank | Tier | Recipe level | Base XP | Job slots | Level-1 job | Level-10 job | Level-1 job credits | Fast loop jobs per h | Climb jobs per h | Crafting XP per h (incl. gathering and the pace multiplier) | Research XP per h at a 100% award |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 10 | 1 | 9 | 450 | 1 | 3.3 min | 5.6 h | 200 | 24.0 | 0.62 | 11,138 | 10,800 |
| 15 | 1 | 9 | 0 | 1 | 3.3 min | 5.6 h | 200 | 24.0 | 0.62 | 12,969 | 0 |
| 20 | 2 | 19 | 450 | 1 | 15.3 min | 25.5 h | 918 | 5.2 | 0.14 | 9,963 | 2,353 |
| 25 | 2 | 9 | 0 | 2 | 3.3 min | 5.6 h | 200 | 37.5 | 1.25 | 6,481 | 0 |
| 30 | 3 | 29 | 450 | 2 | 37.4 min | 62.3 h | 2,242 | 4.3 | 0.11 | 3,768 | 1,927 |
| 35 | 3 | 9 | 0 | 2 | 3.3 min | 5.6 h | 200 | 37.5 | 1.25 | 12,822 | 0 |
| 40 | 4 | 39 | 450 | 2 | 1.2 h | 117.3 h | 4,222 | 2.3 | 0.06 | 14,379 | 1,023 |
| 45 | 4 | 9 | 0 | 2 | 3.3 min | 5.6 h | 200 | 37.5 | 1.25 | 12,458 | 0 |
| 49 | 4 | 9 | 0 | 2 | 3.3 min | 5.6 h | 200 | 37.5 | 1.25 | 5,916 | 0 |

The tier gate lags the rank, so the best recipe a researcher may start is between 1 and 10 levels below their rank, and the XP chart pays nothing for a recipe five or more levels below. 24 of the 40 Fabrication ranks 10-49 have no researchable recipe that pays XP at all under the starting values; research XP arrives in bursts in the first four ranks of each tier. The third job slot (Fabrication 50) arrives when the skill stops paying XP, so it matters for blueprints only.

The starting values against at-level Fabrication crafting at the same rank (the hours-to-50 model, gathering and the per-craft overhead included). A ratio above 100% breaks the plan's ceiling that research XP per real hour stays below crafting XP per hour.

| Fabrication rank | Base XP | Level-1 job XP (starting values) | Starting values as a share of base XP | Research XP per elapsed h | Crafting XP per h | Research / crafting, elapsed | Research / crafting, player's own time |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 10 | 450 | 459 | 102% | 11,016 | 11,138 | 99% | 155% |
| 11 | 300 | 306 | 102% | 7,344 | 14,426 | 51% | 80% |
| 12 | 150 | 153 | 102% | 3,672 | 16,275 | 23% | 35% |
| 13 | 76 | 76 | 100% | 1,824 | 16,453 | 11% | 17% |
| 20 | 450 | 459 | 102% | 2,400 | 9,963 | 24% | 173% |
| 21 | 300 | 306 | 102% | 1,600 | 8,001 | 20% | 143% |
| 22 | 150 | 153 | 102% | 800 | 9,070 | 9% | 63% |
| 23 | 76 | 76 | 100% | 397 | 6,899 | 6% | 41% |
| 30 | 450 | 459 | 102% | 1,965 | 3,768 | 52% | 457% |
| 31 | 300 | 306 | 102% | 1,310 | 13,816 | 9% | 83% |
| 32 | 150 | 153 | 102% | 655 | 11,167 | 6% | 51% |
| 33 | 76 | 76 | 100% | 325 | 11,443 | 3% | 25% |
| 40 | 450 | 459 | 102% | 1,044 | 14,379 | 7% | 120% |
| 41 | 300 | 306 | 102% | 696 | 15,271 | 5% | 75% |
| 42 | 150 | 153 | 102% | 348 | 13,128 | 3% | 44% |
| 43 | 76 | 76 | 100% | 173 | 14,434 | 1% | 20% |

Per elapsed hour with every slot busy, the starting values stay under crafting at 16 of the 16 paying ranks. Per hour of the player's own time at the terminal (96 s a job) they break it at 5 of the 16 ranks (the lowest ratio is 17%), because a researcher who plays only the experiments and lets the jobs run earns a level-1 job's XP for about a minute and a half of attention.

Ceilings in terms of an award, as a share of base XP per job: the largest award that keeps research XP per hour under at-level Fabrication crafting XP per hour at that rank, by two yardsticks.

| Yardstick | Lowest ceiling over the paying ranks | At Fabrication rank |
| --- | --- | --- |
| Per elapsed hour with every job slot busy (research runs while the player does other things) | 103% | 10 |
| Per hour of the player's own time at the terminal | 22% | 30 |

The Research workstream's own report compares its best row with crafting at 24,000 XP per hour (15 actions at 6 s, no overhead, no gathering); this report's crafting rate includes the per-craft overhead and the gathering the crafts need, so it is lower and the ceiling tighter.

## Proposals for the owner

These are options with their measured effect, not code. All of them apply to the four crafting skills and their guilds only: combat and Piloting keep `GetRequiredXP` and `GetDeltaXP` as they are, and Espionage is excluded (`Craft.GetBaseRecipeXP`'s practice XP, `EspionageInfiltration.CalculateXp` and slicing's `Skill.CalculateEspionageXP` are untouched and pinned by tests). None moves the XP chart's cutoff (a craft five or more levels below the rank pays nothing), so quick craft stays at rank >= recipe level + 5 and never costs a player XP they could still earn.

### XP target

Today the four skills take Smithery 97 h, Engineering 90 h, Fabrication 53 h, Agriculture 36 h at mid gear with the model's unmeasured timings: a median of 71 h and a slowest of 97 h, against 137 h of RP XP. **Proposed target: the median crafting skill reaches rank 50 in 45 focused hours or fewer** (33% of the RP XP time), with the 50% guild award below. The target is set on the median skill because the slowest (Smithery) is slow from gathering, not from XP: 79% of its hours are spent gathering. Hours scale with 1 / multiplier, because the components and the craft time scale with the crafts, so one crafting-only multiplier moves every skill together and cannot close that gap.

What each target needs on top of the guild award. A multiplier of x1.00 means the award alone meets it:

| Median skill target (h) | Multiplier needed | Rank cost cap needed (XP) | Smithery h at that multiplier | Engineering h at that multiplier | Fabrication h at that multiplier | Agriculture h at that multiplier |
| --- | --- | --- | --- | --- | --- | --- |
| 45 | x1.28 | 8,984 | 56.4 | 62.7 | 33.6 | 25.9 |
| 35 | x1.75 | 6,522 | 43.6 | 46.3 | 26.4 | 19.9 |
| 25 | x2.69 | 4,261 | 30.9 | 31.0 | 19.1 | 13.8 |

| Option | Change | Smithery h | Engineering h | Fabrication h | Agriculture h | Median skill | Slowest skill | Migration of stored XP |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Today | nothing | 97.1 | 89.6 | 53.2 | 36.3 | 71.4 | 97.1 | no |
| 0 | hand in the guild's task recipes for credits and points, no XP award (isolates the recipe choice) | 83.8 | 79.7 | 51.3 | 38.4 | 65.5 | 83.8 | no |
| A | guild task XP only | 68.7 | 79.1 | 40.6 | 31.6 | 54.6 | 79.1 | no |
| B | A, and crafting XP x1.30 (crafts only) | 55.5 | 61.5 | 33.1 | 25.5 | 44.3 | 61.5 | no |
| C | A, and no rank costs more than 8,800 XP | 54.9 | 61.1 | 33.7 | 25.1 | 44.3 | 61.1 | yes |

- **Row 0 is not an option.** It shows that handing in the guild's task recipes, with no award at all, already changes the hours, because the task recipes' components differ from the level's average recipe; the award's own effect is the difference between rows 0 and A.
- **A: guild task XP only.** No craft XP change; the smallest change, with no stored-data conversion. It takes the median skill to 54.6 h, which is 9.6 h short of the 45 h target.
- **B: A plus one crafting-only multiplier, x1.30.** The smallest round multiplier that brings the median skill to the target (44.3 h). One parameter on what crafts pay, retuned from the client timings later. It changes gains only, so stored XP and ranks need no conversion.
- **C: A plus a cap of 8,800 XP on any rank's cost.** The same median as B by a different route: it cuts the climb from 411,125 to 329,995 XP and spares the late ranks that cost up to 16,000 XP and need 15-25 crafts each, while leaving the early ranks alone. It changes required XP per rank for the crafting skills, so it needs a crafting-specific table and the plan's migration (convert stored XP and ranks so nobody loses a rank).

**Recommendation: B.** The guild award alone leaves the median skill at 54.6 h, so the target needs a crafting-only multiplier of about x1.30 on top; B reaches it with no migration. Treat the multiplier as a starting point: the model's hours move more with the node travel time (Smithery runs from 65 h at 15 s to 291 h at 120 s) than any option moves them, so measure it, re-run `progression`, and retune. Smithery stays far above the median under every option here because its components are scarce drops (gems and crate goods); that is a supply question for the gathering workstream, not an XP one.

### Guild task XP

Each crafting-guild hand-in awards XP in the guild's skill. **Formula:** award = share x `Skill.GetDeltaXP(level - rank)` x item quantity, with `level` the recipe level of a crafted task and the fish's `FishType` level of a catch task (such as `agr_tsk_017`), and `rank` the player's rank in the guild's skill. The chart's cutoff makes a hand-in five or more levels below the rank worth nothing, so the quick-crafted items that raise the guild rank pay no XP, and a task never pays more than crafting the same recipe would.

| Share of base XP | Smithery h | Smithery XP share | Engineering h | Engineering XP share | Fabrication h | Fabrication XP share | Agriculture h | Agriculture XP share |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| none | 97.1 | - | 89.6 | - | 53.2 | - | 36.3 | - |
| 25% | 75.3 | 11% | 79.4 | 1% | 45.3 | 12% | 34.6 | 10% |
| 50% | 68.7 | 20% | 79.1 | 2% | 40.6 | 21% | 31.6 | 17% |
| 100% | 59.3 | 33% | 78.8 | 4% | 33.9 | 34% | 27.0 | 30% |

- **Option 1, 25%.** Hand-ins supply 9% of the climb's XP: a small bonus that is easy to ignore.
- **Option 2, 50% (recommended).** Hand-ins supply 15%: a real part of the pace that does not replace crafting. Quantity matters for catch tasks: at this share a four-fish task's hand-in XP is 23% of the task's XP when the award is per fish and 7% when it is per task, so the award is per item.
- **Option 3, 100%.** Hand-ins supply 25% of the climb, and a crafter who hands in every craft effectively doubles their XP per craft. That is the largest change and should be a deliberate choice.

The credit faucet is the risk to weigh with this: every hand-in pays 1,000-7,200 credits and quick craft makes the items in seconds. If the owner wants a limit, the least invasive option is a per-day cap on guild task credits (not points or XP) per character; nothing here adds one.

### Research XP ceiling

The Research workstream's starting values pay 102% of the chart XP for a level-1 job (`ResearchXpRules`: 62% experiment, 31% completion, +10% per level). **Ceiling, as the plan words it: research XP per real hour, with every job slot busy, stays below at-level Fabrication crafting XP per hour at the same rank** (the whole crafting loop, gathering and overhead included). The values in `ResearchXpRules` (62% and 31%) already sit under it. Counted per hour of the player's own time at the terminal (96 s a job, because the jobs run offline), the starting values exceed crafting at 5 of the 16 paying ranks and would need 22% of themselves; the plan's wording is the elapsed hour, so that is shown as the owner's call, not proposed. Keep a rule that a research award never exceeds the base XP of a craft of the same recipe level. Credits give a second bound: the first job on a new blueprint costs 200-4,200 credits and the level-10 job of a tier-4 recipe takes about five days. Because the chart pays nothing for recipes five or more levels below the rank, the owner may want a floor for research (like `GetPracticeXP`'s 150) so researchers are not paid only in the first four ranks of each tier; a floor must sit under the same ceiling.

## Limitations

- Seconds per action, craft overhead, node travel, turn-in time, research visits and seconds per fishing bite are unmeasured estimates. The client evidence replaces them; the command re-runs with the new values.
- Respawns, node contention, inventory trips, selling, restocking power cores and every other hour of real play are not modelled. These hours are a floor on a focused crafter's time, not a forecast of calendar days.
- Components no node supplies, and parts another recipe makes, are not timed; the sensitivity row prices them at a flat 20 s. Gems are priced as if self-gathered, although players trade them.
- One player policy (the legacy lookahead) at three gear sets. A better player or a different recipe choice changes quality, hence XP per craft. Stretch play (recipes up to three levels above the rank) and blueprints are not played.
- The crafter's Gathering rank is not a gate, XP bonuses (Social, food, DM) are not applied, and the total skill-rank cap and XP debt are ignored.
- Guild points follow the server's formula without Guild Relations or Social, and the cohort's catch-up uses one task item per tier. Credits are base rewards without the Guild Relations and Social bonuses.
- Fishing is shown for catch tasks only; Agriculture's fishing path is not part of the hours above, and fish availability by location is not modelled.
- The research cohort assumes the award follows `GetDeltaXP`; the Research workstream sets the real amount, and the ceiling is the bound on it.
