# Crafting revamp: Prospecting (harvesting) against today's harvests

- Command: `SWLOR.CraftSim harvesting --runs 20000 --seed 1`
- Seed 1; 20000 runs per case.

## Inputs

- Today (the loop Quick Harvest keeps): a node holds d4 harvests, set when it is first worked. Each harvest spends one charge and one harvest delay (5.0 s) for one roll of the node's loot table, and pays `GetDeltaXP(10 x (tier - 1) + 5 - rank)` XP. It also gave a bonus item at 25% per Might modifier point; the owner's attribute parity reference is a Might modifier of 0, so today's yield is 2.50 items, charges and 12.5 s per node, and no code reads an attribute now.
- Quick Harvest: `ProspectingQuickHarvest`, the rule the game runs. It takes the units the node has left, as far as the harvester's charges go, a charge and a harvest delay for each, and pays today's XP for each, with no multiplier and no rank milestone.
- A session: the Prospecting engine, 20000 nodes per case, each with its own dice from seed 1. A node is made as the game makes one (`ProspectingNode.Create`): a pool of 1-4 units, Stability 100, one to three hidden seams that each seal a unit, a gem pocket with 25% chance on a node whose table has a gem. Harvester: 50 charges (a fresh harvester).
- Players: the skilled player is the best of three careful plans (survey once, twice or until every seam is found, extracting what is found and mining with Strikes), chosen for each tier and rank by the most items per node; every plan reads only what the window shows and keeps enough Stability and charges to mine the pool. The length and XP-rate gates use that player. The +15% cap is checked against every plan, three more that use Heavy Strikes included. The casual player strikes the node until it is spent. The reckless player surveys and extracts without a reserve.
- Parity: Quick Harvest equals today's expected items, XP, seconds and charges per node; a skilled session stays within 15% above and not more than 1% below today's items per node (the plan's +15%; the floor is the sampling noise).
- Length: the skilled player averages 3-8 actions per node (the plan's target for a gathering node).
- XP-rate cohort (gate 6): per node tier, the rank equal to the vein's level (5, 15, 25, 35, 45) with the highest harvester tier that rank allows (`CraftRankMilestones`); XP per item, at the attribute reference. Time is the sum of the seconds of the actions taken (2.5 s a Survey, 5.0 s a Strike, 8.0 s a Heavy Strike, 3.0 s an Extract, Leave immediate); the game enforces it, because an action resolves only after its seconds and the window cannot be clicked meanwhile. Both loops are in units of one harvest delay. Unmeasured estimates until the client run.
- The session's XP multiplier by the vein's level (`CraftMinigameXp`): tier 1 1.00x, tier 2 1.00x, tier 3 1.00x, tier 4 1.00x, tier 5 1.00x. 1.00x is today's XP per item; Quick Harvest never takes a multiplier.
- Rank milestones (`CraftRankMilestones`): Strike and Heavy Strike yield more at Gathering 15, 30, 45 (replacing Might), and a Survey reveals one more seam at 25 and 50 (replacing Perception).
- Node kinds, read from the module's placeables and the harvesting loot tables:
  - Tier 1 (level 5, harvester tier 1): `tree` (gem 8.3% of a roll), `veldite_vein` (gem 9.1% of a roll)
  - Tier 2 (level 15, harvester tier 2): `oak_tree` (gem 6.2% of a roll), `scordspar_vein` (gem 7.4% of a roll)
  - Tier 3 (level 25, harvester tier 3): `ancient_tree` (gem 4.0% of a roll), `plagionite_vein` (gem 5.7% of a roll)
  - Tier 4 (level 35, harvester tier 4): `aracia_tree` (gem 2.7% of a roll), `keromber_vein` (gem 3.8% of a roll)
  - Tier 5 (level 45, harvester tier 5): `arkoxit_vein` (no gem), `hyphae_tree` (gem 1.4% of a roll), `jasioclase_vein` (gem 2.0% of a roll)
- Named starting values of the session (`ProspectingRules`), all tunable:
  - `PoolDie` = 4
  - `MaxStability` = 100
  - `OneSeamWeight` = 30
  - `TwoSeamsWeight` = 40
  - `ThreeSeamsWeight` = 30
  - `GemPocketPercent` = 25
  - `MaxSeams` = 3
  - `SurveyCharges` = 1
  - `SurveyStability` = 12
  - `SurveySeconds` = 2.5
  - `StrikeCharges` = 1
  - `StrikeStability` = 20
  - `StrikeSeconds` = 5
  - `HeavyStrikeCharges` = 2
  - `HeavyStrikeStability` = 36
  - `HeavyStrikeSeconds` = 8
  - `ExtractCharges` = 2
  - `ExtractStability` = 24
  - `ExtractSeconds` = 3
  - `ShatterPercent` = 20
  - `RichSeamBonusPercent` = 25
  - `RichSeamBonusRolls` = 1
  - `GemPocketGems` = 1
  - `HarvestSeconds` = 5
  - `StrikeYieldShiftPercent` = 5
  - `HeavyStrikeYieldShiftPercent` = 5
  - `XpMultiplierVeinLevel5` = 1
  - `XpMultiplierVeinLevel15` = 1
  - `XpMultiplierVeinLevel25` = 1
  - `XpMultiplierVeinLevel35` = 1
  - `XpMultiplierVeinLevel45` = 1

## Verdicts

| Gate | Verdict | Summary |
| --- | --- | --- |
| Economy parity | Passed | Quick Harvest equals today's 2.50 items, 12.5 s and charges per node, with the same XP per item. A skilled session yields 1.091x (lowest, tier 1 / rank 5) of today's items per node, and no plan, careful or fast, reaches more than 1.116x (tier 5 / rank 40); the band is 99% to 115%. |
| Length | Passed | The skilled player's sessions last 3.29 to 3.85 actions on average in every case (target 3-8). |
| XP rate | Passed | Harvesting, Quick Harvest: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.00x in Tier 1 vein (level 5) / harvester tier 1, rank = vein level). Harvesting, skilled session: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.08x in Tier 2 vein (level 15) / harvester tier 2, rank = vein level). |
| Quick Harvest XP rate cap | Passed | Quick Harvest's XP per hour is exactly today's in every case: the same units, the same XP for each, one harvest delay for each. |
| Evidence: strike-only session XP rate | NotApplicable | Not a gate. A player who only strikes gets today's yield (the whole pool, through the strikes that break into seams) in less time and fewer charges than Quick Harvest, so the session pays at least today's XP per hour. |
| Evidence: charges, time and gems | NotApplicable | Not a gate. What a node costs and what the skilled player's extra items are made of. Today a unit costs one charge and 5 seconds, and its roll of the table is a gem with the table's gem weight over its whole weight. |
| Evidence: Heavy Strike plans | NotApplicable | Not a gate. A Heavy Strike is a faster Strike, not a bigger one: the plans that use it take the same items as the careful plans in fewer actions and less time, so their XP per hour is higher. They count toward the ceiling the +15% cap is checked against. |
| Evidence: Gathering rank milestones | NotApplicable | Not a gate. The milestones replace Might and Perception: they reveal more seams and take more units per action. The pool still ends the node, so they shorten a session and raise no yield on their own. |
| Evidence: collapse | NotApplicable | Not a gate. Stability is the node's budget: a player who surveys and extracts without leaving enough to mine the pool can collapse the node and lose the units left in it. The window warns before the click. |
| Evidence: refining XP | NotApplicable | Not a gate: refining has no minigame and no legacy rate to match. Refining pays GetDeltaXP(ore activity level - rank) for each unit, where an ore's activity level is the level of the vein of its tier (10 x (tier - 1) + 5, so 5, 15, 25, 35 and 45). The ore of the veins' tier pays the same per unit as harvesting that vein, so a unit that is harvested and then refined pays twice at the same rank. |

## Economy parity

### Quick Harvest against today's harvests, rank = the vein's level (the cohort's rank), per node at a Might modifier of 0 (20000 nodes played per row)

| Node tier | Rank | Today, items per node | Today, played | Quick Harvest, items per node | Quick Harvest, played | Quick Harvest, XP per node | Today, XP per node | Quick Harvest, seconds per node | Today, seconds per node | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 5 | 2.50 | 2.49 | 2.50 | 2.50 | 1500 | 1500 | 12.5 | 12.5 | Passed |
| 2 | 15 | 2.50 | 2.51 | 2.50 | 2.52 | 1500 | 1500 | 12.5 | 12.5 | Passed |
| 3 | 25 | 2.50 | 2.50 | 2.50 | 2.50 | 1500 | 1500 | 12.5 | 12.5 | Passed |
| 4 | 35 | 2.50 | 2.50 | 2.50 | 2.49 | 1500 | 1500 | 12.5 | 12.5 | Passed |
| 5 | 45 | 2.50 | 2.50 | 2.50 | 2.49 | 1500 | 1500 | 12.5 | 12.5 | Passed |

### Items per node by node tier and Gathering rank: a skilled session against today (20000 nodes per plan; the skilled player is the careful plan with the most items, and the last column is the highest of every plan, Heavy Strike plans included)

| Node tier | Rank | Today | Strike only | Skilled session | Skilled / today | Skilled plan | Highest of any plan / today | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 0 | 2.50 | 2.50 | 2.73 | 1.092 | Survey twice, extract | 1.092 | Passed |
| 1 | 5 | 2.50 | 2.50 | 2.73 | 1.091 | Survey until done, extract | 1.091 | Passed |
| 1 | 10 | 2.50 | 2.51 | 2.73 | 1.092 | Survey until done, extract | 1.092 | Passed |
| 1 | 15 | 2.50 | 2.51 | 2.75 | 1.100 | Survey until done, extract | 1.101 | Passed |
| 1 | 20 | 2.50 | 2.51 | 2.75 | 1.101 | Survey until done, extract | 1.109 | Passed |
| 1 | 25 | 2.50 | 2.49 | 2.74 | 1.098 | Survey once, extract | 1.098 | Passed |
| 1 | 30 | 2.50 | 2.49 | 2.76 | 1.103 | Survey once, extract | 1.103 | Passed |
| 1 | 35 | 2.50 | 2.50 | 2.76 | 1.105 | Survey twice, extract | 1.107 | Passed |
| 1 | 40 | 2.50 | 2.49 | 2.76 | 1.105 | Survey until done, extract | 1.105 | Passed |
| 1 | 45 | 2.50 | 2.50 | 2.75 | 1.101 | Survey until done, extract | 1.101 | Passed |
| 1 | 50 | 2.50 | 2.50 | 2.74 | 1.098 | Survey once, extract | 1.098 | Passed |
| 2 | 10 | 2.50 | 2.50 | 2.73 | 1.093 | Survey twice, extract | 1.095 | Passed |
| 2 | 15 | 2.50 | 2.52 | 2.76 | 1.105 | Survey twice, extract | 1.105 | Passed |
| 2 | 20 | 2.50 | 2.51 | 2.77 | 1.109 | Survey twice, extract | 1.109 | Passed |
| 2 | 25 | 2.50 | 2.51 | 2.75 | 1.101 | Survey until done, extract | 1.101 | Passed |
| 2 | 30 | 2.50 | 2.49 | 2.75 | 1.100 | Survey once, extract | 1.100 | Passed |
| 2 | 35 | 2.50 | 2.50 | 2.75 | 1.101 | Survey until done, extract | 1.101 | Passed |
| 2 | 40 | 2.50 | 2.49 | 2.75 | 1.101 | Survey twice, extract | 1.101 | Passed |
| 2 | 45 | 2.50 | 2.51 | 2.75 | 1.100 | Survey twice, extract | 1.101 | Passed |
| 2 | 50 | 2.50 | 2.50 | 2.74 | 1.096 | Survey once, extract | 1.097 | Passed |
| 3 | 20 | 2.50 | 2.50 | 2.76 | 1.105 | Survey until done, extract | 1.105 | Passed |
| 3 | 25 | 2.50 | 2.50 | 2.76 | 1.102 | Survey once, extract | 1.102 | Passed |
| 3 | 30 | 2.50 | 2.51 | 2.75 | 1.101 | Survey twice, extract | 1.101 | Passed |
| 3 | 35 | 2.50 | 2.50 | 2.75 | 1.098 | Survey twice, extract | 1.098 | Passed |
| 3 | 40 | 2.50 | 2.49 | 2.75 | 1.102 | Survey twice, extract | 1.102 | Passed |
| 3 | 45 | 2.50 | 2.50 | 2.74 | 1.098 | Survey twice, extract | 1.098 | Passed |
| 3 | 50 | 2.50 | 2.50 | 2.75 | 1.100 | Survey once, extract | 1.100 | Passed |
| 4 | 30 | 2.50 | 2.50 | 2.75 | 1.099 | Survey once, extract | 1.106 | Passed |
| 4 | 35 | 2.50 | 2.49 | 2.75 | 1.099 | Survey once, extract | 1.100 | Passed |
| 4 | 40 | 2.50 | 2.50 | 2.76 | 1.102 | Survey until done, extract | 1.102 | Passed |
| 4 | 45 | 2.50 | 2.49 | 2.75 | 1.101 | Survey once, extract | 1.102 | Passed |
| 4 | 50 | 2.50 | 2.51 | 2.73 | 1.094 | Survey twice, extract | 1.098 | Passed |
| 5 | 40 | 2.50 | 2.49 | 2.78 | 1.111 | Survey once, extract | 1.116 | Passed |
| 5 | 45 | 2.50 | 2.49 | 2.79 | 1.114 | Survey once, extract | 1.114 | Passed |
| 5 | 50 | 2.50 | 2.50 | 2.77 | 1.108 | Survey once, extract | 1.109 | Passed |

## Length

### Actions per node, target 3-8 (Leave is not an action; every action takes a few seconds)

| Node tier | Rank | Skilled actions per node | Skilled sessions of 3-8 actions | Strike only, actions per node | Skilled plan | Verdict |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | 0 | 3.55 | 75% | 2.11 | Survey twice, extract | Within |
| 1 | 5 | 3.56 | 75% | 2.12 | Survey until done, extract | Within |
| 1 | 10 | 3.56 | 75% | 2.12 | Survey until done, extract | Within |
| 1 | 15 | 3.83 | 75% | 2.06 | Survey until done, extract | Within |
| 1 | 20 | 3.83 | 75% | 2.06 | Survey until done, extract | Within |
| 1 | 25 | 3.33 | 75% | 2.04 | Survey once, extract | Within |
| 1 | 30 | 3.33 | 76% | 2.00 | Survey once, extract | Within |
| 1 | 35 | 3.32 | 75% | 2.00 | Survey twice, extract | Within |
| 1 | 40 | 3.32 | 75% | 1.99 | Survey until done, extract | Within |
| 1 | 45 | 3.30 | 75% | 1.95 | Survey until done, extract | Within |
| 1 | 50 | 3.29 | 75% | 1.95 | Survey once, extract | Within |
| 2 | 10 | 3.56 | 75% | 2.11 | Survey twice, extract | Within |
| 2 | 15 | 3.84 | 75% | 2.07 | Survey twice, extract | Within |
| 2 | 20 | 3.85 | 76% | 2.06 | Survey twice, extract | Within |
| 2 | 25 | 3.33 | 75% | 2.06 | Survey until done, extract | Within |
| 2 | 30 | 3.32 | 75% | 2.00 | Survey once, extract | Within |
| 2 | 35 | 3.32 | 75% | 2.00 | Survey until done, extract | Within |
| 2 | 40 | 3.31 | 75% | 2.00 | Survey twice, extract | Within |
| 2 | 45 | 3.30 | 75% | 1.96 | Survey twice, extract | Within |
| 2 | 50 | 3.29 | 75% | 1.95 | Survey once, extract | Within |
| 3 | 20 | 3.83 | 75% | 2.05 | Survey until done, extract | Within |
| 3 | 25 | 3.34 | 75% | 2.05 | Survey once, extract | Within |
| 3 | 30 | 3.32 | 75% | 2.02 | Survey twice, extract | Within |
| 3 | 35 | 3.31 | 75% | 2.00 | Survey twice, extract | Within |
| 3 | 40 | 3.32 | 75% | 2.00 | Survey twice, extract | Within |
| 3 | 45 | 3.29 | 75% | 1.95 | Survey twice, extract | Within |
| 3 | 50 | 3.30 | 75% | 1.95 | Survey once, extract | Within |
| 4 | 30 | 3.31 | 75% | 2.00 | Survey once, extract | Within |
| 4 | 35 | 3.31 | 75% | 2.00 | Survey once, extract | Within |
| 4 | 40 | 3.32 | 75% | 2.00 | Survey until done, extract | Within |
| 4 | 45 | 3.29 | 75% | 1.94 | Survey once, extract | Within |
| 4 | 50 | 3.29 | 75% | 1.96 | Survey twice, extract | Within |
| 5 | 40 | 3.31 | 75% | 2.00 | Survey once, extract | Within |
| 5 | 45 | 3.30 | 75% | 1.95 | Survey once, extract | Within |
| 5 | 50 | 3.29 | 75% | 1.95 | Survey once, extract | Within |

## XP rate

### Harvesting, Quick Harvest: XP per hour against the legacy loop (5.0 s per action legacy, 5.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per node | Legacy actions per node | Legacy XP per hour | New XP per node | New actions per node | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Tier 1 vein (level 5) | harvester tier 1, rank = vein level | 1500 | 2.5 | 432000 | 1500 | 2.5 | 432000 | 1.00 | - | Passed |
| Tier 2 vein (level 15) | harvester tier 2, rank = vein level | 1500 | 2.5 | 432000 | 1500 | 2.5 | 432000 | 1.00 | - | Passed |
| Tier 3 vein (level 25) | harvester tier 3, rank = vein level | 1500 | 2.5 | 432000 | 1500 | 2.5 | 432000 | 1.00 | - | Passed |
| Tier 4 vein (level 35) | harvester tier 4, rank = vein level | 1500 | 2.5 | 432000 | 1500 | 2.5 | 432000 | 1.00 | - | Passed |
| Tier 5 vein (level 45) | harvester tier 5, rank = vein level | 1500 | 2.5 | 432000 | 1500 | 2.5 | 432000 | 1.00 | - | Passed |

### Harvesting, skilled session: XP per hour against the legacy loop (5.0 s per action legacy, 5.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per node | Legacy actions per node | Legacy XP per hour | New XP per node | New actions per node | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Tier 1 vein (level 5) | harvester tier 1, rank = vein level | 1500 | 2.5 | 432000 | 1637 | 2.5 | 478467 | 1.11 | - | Passed |
| Tier 2 vein (level 15) | harvester tier 2, rank = vein level | 1500 | 2.5 | 432000 | 1657 | 2.5 | 468421 | 1.08 | - | Passed |
| Tier 3 vein (level 25) | harvester tier 3, rank = vein level | 1500 | 2.5 | 432000 | 1653 | 2.3 | 517727 | 1.20 | - | Passed |
| Tier 4 vein (level 35) | harvester tier 4, rank = vein level | 1500 | 2.5 | 432000 | 1649 | 2.3 | 521481 | 1.21 | - | Passed |
| Tier 5 vein (level 45) | harvester tier 5, rank = vein level | 1500 | 2.5 | 432000 | 1671 | 2.3 | 533021 | 1.23 | - | Passed |

## Quick Harvest XP rate cap

### Quick Harvest XP per hour against today's (rank = the vein's level)

| Group | Today, XP per hour | Quick Harvest, XP per hour | Ratio | Verdict |
| --- | --- | --- | --- | --- |
| Tier 1 vein (level 5) | 432000 | 432000 | 1.000 | Passed |
| Tier 2 vein (level 15) | 432000 | 432000 | 1.000 | Passed |
| Tier 3 vein (level 25) | 432000 | 432000 | 1.000 | Passed |
| Tier 4 vein (level 35) | 432000 | 432000 | 1.000 | Passed |
| Tier 5 vein (level 45) | 432000 | 432000 | 1.000 | Passed |

## Evidence: strike-only session XP rate

### Harvesting, strike-only session: XP per hour against the legacy loop (5.0 s per action legacy, 5.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per node | Legacy actions per node | Legacy XP per hour | New XP per node | New actions per node | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Tier 1 vein (level 5) | harvester tier 1, rank = vein level | 1500 | 2.5 | 432000 | 1499 | 2.1 | 509807 | 1.18 | - | Passed |
| Tier 2 vein (level 15) | harvester tier 2, rank = vein level | 1500 | 2.5 | 432000 | 1511 | 2.1 | 526216 | 1.22 | - | Passed |
| Tier 3 vein (level 25) | harvester tier 3, rank = vein level | 1500 | 2.5 | 432000 | 1498 | 2.1 | 526100 | 1.22 | - | Passed |
| Tier 4 vein (level 35) | harvester tier 4, rank = vein level | 1500 | 2.5 | 432000 | 1496 | 2.0 | 537319 | 1.24 | - | Passed |
| Tier 5 vein (level 45) | harvester tier 5, rank = vein level | 1500 | 2.5 | 432000 | 1496 | 1.9 | 553673 | 1.28 | - | Passed |

## Evidence: charges, time and gems

### Charges, seconds and gems per node at rank = the vein's level (a Might modifier of 0 for today)

| Node tier | Rank | Charges per node, today and Quick Harvest | Charges per node, strike only | Charges per node, skilled | Charges per item, skilled | Seconds per node, today and Quick Harvest | Seconds per node, strike only | Seconds per node, skilled | Gems per node, today | Gems per node, skilled | Gems, skilled / today |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 5 | 2.50 | 2.12 | 4.73 | 1.73 | 12.5 | 10.6 | 12.3 | 0.218 | 0.295 | 1.4x |
| 2 | 15 | 2.50 | 2.07 | 5.19 | 1.88 | 12.5 | 10.3 | 12.7 | 0.171 | 0.334 | 2.0x |
| 3 | 25 | 2.50 | 2.05 | 4.69 | 1.70 | 12.5 | 10.3 | 11.5 | 0.121 | 0.372 | 3.1x |
| 4 | 35 | 2.50 | 2.00 | 4.66 | 1.70 | 12.5 | 10.0 | 11.4 | 0.082 | 0.372 | 4.5x |
| 5 | 45 | 2.50 | 1.95 | 4.65 | 1.67 | 12.5 | 9.7 | 11.3 | 0.028 | 0.245 | 8.8x |

## Evidence: Heavy Strike plans

### Careful play against Heavy Strike play at rank = the vein's level

| Node tier | Rank | Careful, items per node | Careful, actions | Careful, XP per hour / today | Heavy Strikes, items per node | Heavy Strikes, actions | Heavy Strikes, XP per hour / today |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 5 | 2.73 | 3.56 | 1.11 | 2.72 | 3.54 | 1.11 |
| 2 | 15 | 2.76 | 3.84 | 1.08 | 2.74 | 3.83 | 1.08 |
| 3 | 25 | 2.76 | 3.34 | 1.20 | 2.74 | 3.24 | 1.22 |
| 4 | 35 | 2.75 | 3.31 | 1.21 | 2.74 | 3.22 | 1.23 |
| 5 | 45 | 2.79 | 3.30 | 1.23 | 2.78 | 3.21 | 1.26 |

## Evidence: Gathering rank milestones

### Rank milestones at a tier 1 node (the same milestones apply at every tier)

| Rank | Seams one Survey reveals | Tier of yield | Units of a Strike (mean) | Units of a Heavy Strike (mean) | Skilled items per node | Skilled actions per node | Strike-only actions per node |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | 1 | 0 | 1.30 | 3.00 | 2.731 | 3.55 | 2.11 |
| 5 | 1 | 0 | 1.30 | 3.00 | 2.728 | 3.56 | 2.12 |
| 10 | 1 | 0 | 1.30 | 3.00 | 2.730 | 3.56 | 2.12 |
| 15 | 1 | 1 | 1.35 | 3.10 | 2.750 | 3.83 | 2.06 |
| 20 | 1 | 1 | 1.35 | 3.10 | 2.752 | 3.83 | 2.06 |
| 25 | 2 | 1 | 1.35 | 3.10 | 2.744 | 3.33 | 2.04 |
| 30 | 2 | 2 | 1.40 | 3.20 | 2.758 | 3.33 | 2.00 |
| 35 | 2 | 2 | 1.40 | 3.20 | 2.762 | 3.32 | 2.00 |
| 40 | 2 | 2 | 1.40 | 3.20 | 2.762 | 3.32 | 1.99 |
| 45 | 2 | 3 | 1.45 | 3.30 | 2.754 | 3.30 | 1.95 |
| 50 | 3 | 3 | 1.45 | 3.30 | 2.745 | 3.29 | 1.95 |

## Evidence: collapse

### A player who does not keep a Stability reserve (gem-bearing node kinds)

| Node tier | Rank | Seams first, no reserve: items per node | Share of nodes collapsed | Units lost with the node | Skilled items per node |
| --- | --- | --- | --- | --- | --- |
| 1 | 5 | 2.67 | 14.4% | 0.14 | 2.73 |
| 2 | 15 | 2.67 | 14.1% | 0.14 | 2.76 |
| 3 | 25 | 2.81 | 0.0% | 0.00 | 2.76 |
| 4 | 35 | 2.82 | 0.0% | 0.00 | 2.75 |
| 5 | 45 | 2.85 | 0.0% | 0.00 | 2.79 |

## Evidence: refining XP

### Refining XP per unit by ore, before and now

| Ore | Ore tier | Ore activity level | Refining XP per unit before | Refining XP per unit at rank = level | At level + 5 ranks | At level + 10 ranks | At rank 0 | At rank 50 | Harvest XP per item of the same tier at rank = level |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| ore_tilarium | 1 | 5 | 25 | 600 | 0 | 0 | 1050 | 0 | 600 |
| raw_veldite | 1 | 5 | 25 | 600 | 0 | 0 | 1050 | 0 | 600 |
| ore_currian | 2 | 15 | 50 | 600 | 0 | 0 | 1200 | 0 | 600 |
| raw_scordspar | 2 | 15 | 50 | 600 | 0 | 0 | 1200 | 0 | 600 |
| ore_idailia | 3 | 25 | 75 | 600 | 0 | 0 | 1200 | 0 | 600 |
| raw_plagionite | 3 | 25 | 75 | 600 | 0 | 0 | 1200 | 0 | 600 |
| ore_barinium | 4 | 35 | 100 | 600 | 0 | 0 | 1200 | 0 | 600 |
| raw_keromber | 4 | 35 | 100 | 600 | 0 | 0 | 1200 | 0 | 600 |
| ore_gostian | 5 | 45 | 125 | 600 | 0 | 0 | 1200 | 0 | 600 |
| raw_jasioclase | 5 | 45 | 125 | 600 | 0 | 0 | 1200 | 0 | 600 |
| ore_arda | 5 | 45 | 150 | 600 | 0 | 0 | 1200 | 0 | 600 |
| raw_arkoxit | 5 | 45 | 150 | 600 | 0 | 0 | 1200 | 0 | 600 |

## Limitations

- The skilled player is the best of a set of plans a person can follow from the window, not an optimal player. A better plan could take more items per node, so the +15% cap is checked against these plans, and the margin below it is the room for one.
- Items are counted, not priced. A gem pocket gives a gem in place of a unit, so it adds no item but moves value into gems; the evidence table shows the gem supply against today's. The extra ore roll of a rich seam is the only way a session yields more than the pool.
- Respawn, node density and travel are not modelled, so these are yields per node, not per hour. The XP-rate gate counts the seconds of the actions only; walking between nodes costs both loops the same.
- Seconds per action are estimates until the client run measures them. They are the cohort's only timing input: a session whose actions take longer than modelled earns less per hour than the gate shows, and one that takes less earns more.
- A skilled session spends more charges per item than today (the evidence table), because Survey and Extract cost charges and yield no unit of their own. A harvester lasts fewer skilled sessions than Quick Harvests; a strike-only session spends fewer charges than either.
- Perks are not modelled; the Prospector draft's magnitudes are estimates against these sessions, and no perk code exists. The client was not run.
