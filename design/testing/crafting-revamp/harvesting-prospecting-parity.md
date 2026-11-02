# Crafting revamp: Prospecting (harvesting) against today's harvests

- Command: `SWLOR.CraftSim harvesting --runs 20000 --seed 1`
- Seed 1; 20000 runs per case.

## Inputs

- Today (the loop Quick Harvest keeps): a node holds d4 harvests, set when it is first worked. Each harvest spends one charge and one harvest delay (5.0 s) for one roll of the node's loot table, and pays `GetDeltaXP(10 x (tier - 1) + 5 - rank)` XP. It also gave a bonus item at 25% per Might modifier point; the owner's attribute parity reference is a Might modifier of 0, so today's yield is 2.50 items, charges and 12.5 s per node, and no code reads an attribute now.
- Quick Harvest: `ProspectingQuickHarvest`, the rule the game runs. It takes the units the node has left, as far as the harvester's charges go, a charge and a harvest delay for each, and pays today's XP for each, with no multiplier and no rank milestone.
- A session: the Prospecting engine, 20000 nodes per case, each with its own dice from seed 1. A node is made as the game makes one (`ProspectingNode.Create`): a pool of 1-4 units, Stability 100, one to three hidden seams that each seal a unit, a gem pocket with 15% of the chance that a roll of the node's table is a gem (so none where the table has no gem; the gem chance of each kind is listed below). Harvester: 50 charges (a fresh harvester).
- Charges: a Survey costs 0 (Stability only), a Strike 1, a Heavy Strike 2, an Extract 2 (the unit it takes, which today costs a charge too, and one more). A skilled session may spend at most 150% of today's charges per node.
- Gems: a unit is a roll of the table and a roll is a gem with the table's gem chance; a pocket gives a gem in place of its unit. Today's gems per node are the units times the gem chance. A skilled session may give at most 115% of them.
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
  - `GemPocketShare` = 0.15
  - `MaxSeams` = 3
  - `SurveyCharges` = 0
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
| Economy parity | Passed | Quick Harvest equals today's 2.50 items, 12.5 s and charges per node, with the same XP per item. A skilled session yields 1.115x (lowest, tier 1 / rank 10) of today's items per node, and no plan, careful or fast, reaches more than 1.140x (tier 1 / rank 35); the band is 99% to 115%. Gems per node, the ordinary rolls and the pockets together, reach at most 1.108x of today's (tier 5 / rank 50) against a cap of 115%. A skilled session spends at most 1.47x of today's harvester charges per node (tier 1 / rank 25) against a cap of 150%. |
| Length | Passed | The skilled player's sessions last 3.29 to 3.83 actions on average in every case (target 3-8). |
| XP rate | Passed | Harvesting, Quick Harvest: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.00x in Tier 1 vein (level 5) / harvester tier 1, rank = vein level). Harvesting, skilled session: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.12x in Tier 2 vein (level 15) / harvester tier 2, rank = vein level). |
| Quick Harvest XP rate cap | Passed | Quick Harvest's XP per hour is exactly today's in every case: the same units, the same XP for each, one harvest delay for each. |
| Evidence: strike-only session XP rate | NotApplicable | Not a gate. A player who only strikes gets today's yield (the whole pool, through the strikes that break into seams) in less time and fewer charges than Quick Harvest, so the session pays at least today's XP per hour. |
| Evidence: charges, time and gems | NotApplicable | Not a gate (the gem cap is checked in the economy-parity gate). What a node costs and what the skilled player's extra items are made of. Today a unit costs one charge and 5 seconds, and its roll of the table is a gem with the table's gem weight over its whole weight. A Survey costs no charge, an Extract costs one charge more than a Strike, and a gem pocket is rare enough that the gems of a skilled session, those of its ordinary rolls and of its pockets together, stay within the cap of today's. |
| Evidence: Heavy Strike plans | NotApplicable | Not a gate. A Heavy Strike is a faster Strike, not a bigger one: the plans that use it take the same items as the careful plans in fewer actions and less time, so their XP per hour is higher. They count toward the ceiling the +15% cap is checked against. |
| Evidence: Gathering rank milestones | NotApplicable | Not a gate. The milestones replace Might and Perception: they reveal more seams and take more units per action. The pool still ends the node, so they shorten a session and raise no yield on their own. |
| Evidence: collapse | NotApplicable | Not a gate. Stability is the node's budget: a player who surveys and extracts without leaving enough to mine the pool can collapse the node and lose the units left in it. The window warns before the click. |
| Evidence: refining XP | NotApplicable | Not a gate: refining has no minigame and no legacy rate to match. Each unit pays its ore's flat XP (25 to 150, what refining always paid) times GetDeltaXP(ore activity level - rank) / GetDeltaXP(0), where an ore's activity level is the level of the vein of its tier (10 x (tier - 1) + 5, so 5, 15, 25, 35 and 45). At the ore's level that is exactly today's flat XP, so refining XP per hour at level is today's; a refiner below the level earns up to twice the flat XP, and one five ranks above it earns nothing, so cheap ore stops training high ranks. |

## Economy parity

### Quick Harvest against today's harvests, rank = the vein's level (the cohort's rank), per node at a Might modifier of 0 (20000 nodes played per row)

| Node tier | Rank | Today, items per node | Today, played | Quick Harvest, items per node | Quick Harvest, played | Quick Harvest, XP per node | Today, XP per node | Quick Harvest, seconds per node | Today, seconds per node | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 5 | 2.50 | 2.49 | 2.50 | 2.50 | 1500 | 1500 | 12.5 | 12.5 | Passed |
| 2 | 15 | 2.50 | 2.51 | 2.50 | 2.51 | 1500 | 1500 | 12.5 | 12.5 | Passed |
| 3 | 25 | 2.50 | 2.50 | 2.50 | 2.51 | 1500 | 1500 | 12.5 | 12.5 | Passed |
| 4 | 35 | 2.50 | 2.50 | 2.50 | 2.50 | 1500 | 1500 | 12.5 | 12.5 | Passed |
| 5 | 45 | 2.50 | 2.50 | 2.50 | 2.50 | 1500 | 1500 | 12.5 | 12.5 | Passed |

### Items per node by node tier and Gathering rank: a skilled session against today (20000 nodes per plan; the skilled player is the careful plan with the most items, and the last column is the highest of every plan, Heavy Strike plans included)

| Node tier | Rank | Today | Strike only | Skilled session | Skilled / today | Skilled plan | Highest of any plan / today | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 0 | 2.50 | 2.49 | 2.79 | 1.118 | Survey until done, extract | 1.118 | Passed |
| 1 | 5 | 2.50 | 2.50 | 2.80 | 1.120 | Survey until done, extract | 1.120 | Passed |
| 1 | 10 | 2.50 | 2.51 | 2.79 | 1.115 | Survey twice, extract | 1.115 | Passed |
| 1 | 15 | 2.50 | 2.51 | 2.84 | 1.135 | Survey until done, extract | 1.135 | Passed |
| 1 | 20 | 2.50 | 2.50 | 2.84 | 1.135 | Survey until done, extract | 1.135 | Passed |
| 1 | 25 | 2.50 | 2.50 | 2.84 | 1.136 | Survey twice, extract | 1.136 | Passed |
| 1 | 30 | 2.50 | 2.50 | 2.83 | 1.132 | Survey twice, extract | 1.138 | Passed |
| 1 | 35 | 2.50 | 2.51 | 2.85 | 1.139 | Survey once, extract | 1.140 | Passed |
| 1 | 40 | 2.50 | 2.50 | 2.83 | 1.133 | Survey once, extract | 1.134 | Passed |
| 1 | 45 | 2.50 | 2.50 | 2.83 | 1.133 | Survey once, extract | 1.133 | Passed |
| 1 | 50 | 2.50 | 2.50 | 2.84 | 1.135 | Survey until done, extract | 1.135 | Passed |
| 2 | 10 | 2.50 | 2.50 | 2.80 | 1.119 | Survey twice, extract | 1.119 | Passed |
| 2 | 15 | 2.50 | 2.51 | 2.84 | 1.136 | Survey twice, extract | 1.136 | Passed |
| 2 | 20 | 2.50 | 2.50 | 2.84 | 1.134 | Survey twice, extract | 1.135 | Passed |
| 2 | 25 | 2.50 | 2.50 | 2.84 | 1.135 | Survey once, extract | 1.135 | Passed |
| 2 | 30 | 2.50 | 2.51 | 2.84 | 1.138 | Survey twice, extract | 1.138 | Passed |
| 2 | 35 | 2.50 | 2.50 | 2.84 | 1.136 | Survey once, extract | 1.139 | Passed |
| 2 | 40 | 2.50 | 2.50 | 2.83 | 1.132 | Survey until done, extract | 1.136 | Passed |
| 2 | 45 | 2.50 | 2.50 | 2.84 | 1.138 | Survey until done, extract | 1.138 | Passed |
| 2 | 50 | 2.50 | 2.49 | 2.83 | 1.132 | Survey until done, extract | 1.135 | Passed |
| 3 | 20 | 2.50 | 2.51 | 2.83 | 1.133 | Survey until done, extract | 1.137 | Passed |
| 3 | 25 | 2.50 | 2.51 | 2.84 | 1.136 | Survey once, extract | 1.136 | Passed |
| 3 | 30 | 2.50 | 2.50 | 2.84 | 1.137 | Survey twice, extract | 1.137 | Passed |
| 3 | 35 | 2.50 | 2.49 | 2.84 | 1.136 | Survey once, extract | 1.136 | Passed |
| 3 | 40 | 2.50 | 2.49 | 2.83 | 1.133 | Survey once, extract | 1.135 | Passed |
| 3 | 45 | 2.50 | 2.50 | 2.84 | 1.136 | Survey twice, extract | 1.138 | Passed |
| 3 | 50 | 2.50 | 2.51 | 2.85 | 1.139 | Survey twice, extract | 1.139 | Passed |
| 4 | 30 | 2.50 | 2.49 | 2.84 | 1.137 | Survey until done, extract | 1.138 | Passed |
| 4 | 35 | 2.50 | 2.50 | 2.84 | 1.135 | Survey twice, extract | 1.136 | Passed |
| 4 | 40 | 2.50 | 2.51 | 2.85 | 1.140 | Survey once, extract | 1.140 | Passed |
| 4 | 45 | 2.50 | 2.51 | 2.84 | 1.137 | Survey twice, extract | 1.137 | Passed |
| 4 | 50 | 2.50 | 2.50 | 2.84 | 1.135 | Survey twice, extract | 1.139 | Passed |
| 5 | 40 | 2.50 | 2.50 | 2.84 | 1.136 | Survey once, extract | 1.137 | Passed |
| 5 | 45 | 2.50 | 2.50 | 2.84 | 1.138 | Survey once, extract | 1.138 | Passed |
| 5 | 50 | 2.50 | 2.49 | 2.84 | 1.137 | Survey once, extract | 1.137 | Passed |

### Gems per node by node tier and Gathering rank, the gems of the ordinary rolls and of the gem pockets together, averaged over the tier's kinds of node (20000 nodes per plan; the last column is the highest of every plan)

| Node tier | Rank | Today, gems per node | Quick Harvest | Strike only | Skilled session | Skilled / today | Highest of any plan / today | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 0 | 0.218 | 0.217 | 0.217 | 0.232 | 1.067 | 1.068 (Survey until done, extract, heavy strikes) | Passed |
| 1 | 5 | 0.218 | 0.218 | 0.218 | 0.232 | 1.066 | 1.066 (Survey until done, extract) | Passed |
| 1 | 10 | 0.218 | 0.219 | 0.219 | 0.230 | 1.058 | 1.063 (Survey until done, extract (cautious)) | Passed |
| 1 | 15 | 0.218 | 0.218 | 0.218 | 0.234 | 1.075 | 1.079 (Survey until done, extract, heavy strikes) | Passed |
| 1 | 20 | 0.218 | 0.218 | 0.218 | 0.235 | 1.078 | 1.078 (Survey until done, extract) | Passed |
| 1 | 25 | 0.218 | 0.218 | 0.218 | 0.237 | 1.089 | 1.092 (Survey until done, extract (cautious)) | Passed |
| 1 | 30 | 0.218 | 0.218 | 0.218 | 0.235 | 1.077 | 1.084 (Survey until done, extract, heavy strikes) | Passed |
| 1 | 35 | 0.218 | 0.218 | 0.218 | 0.236 | 1.085 | 1.087 (Survey until done, extract, heavy strikes) | Passed |
| 1 | 40 | 0.218 | 0.218 | 0.218 | 0.236 | 1.083 | 1.086 (Survey twice, extract) | Passed |
| 1 | 45 | 0.218 | 0.218 | 0.218 | 0.237 | 1.086 | 1.089 (Survey until done, extract (cautious)) | Passed |
| 1 | 50 | 0.218 | 0.218 | 0.218 | 0.237 | 1.087 | 1.092 (Survey once, extract) | Passed |
| 2 | 10 | 0.171 | 0.171 | 0.171 | 0.182 | 1.067 | 1.067 (Survey twice, extract) | Passed |
| 2 | 15 | 0.171 | 0.171 | 0.171 | 0.184 | 1.080 | 1.080 (Survey twice, extract) | Passed |
| 2 | 20 | 0.171 | 0.171 | 0.171 | 0.183 | 1.072 | 1.082 (Survey until done, extract) | Passed |
| 2 | 25 | 0.171 | 0.171 | 0.171 | 0.184 | 1.080 | 1.090 (Survey until done, extract, heavy strikes) | Passed |
| 2 | 30 | 0.171 | 0.171 | 0.171 | 0.185 | 1.085 | 1.087 (Survey once, extract, heavy strikes) | Passed |
| 2 | 35 | 0.171 | 0.170 | 0.170 | 0.184 | 1.080 | 1.087 (Survey until done, extract (cautious)) | Passed |
| 2 | 40 | 0.171 | 0.171 | 0.171 | 0.185 | 1.082 | 1.091 (Survey until done, extract, heavy strikes) | Passed |
| 2 | 45 | 0.171 | 0.171 | 0.171 | 0.186 | 1.088 | 1.089 (Survey until done, extract (cautious)) | Passed |
| 2 | 50 | 0.171 | 0.170 | 0.170 | 0.187 | 1.094 | 1.094 (Survey until done, extract (cautious)) | Passed |
| 3 | 20 | 0.121 | 0.121 | 0.121 | 0.129 | 1.070 | 1.083 (Survey until done, extract, heavy strikes) | Passed |
| 3 | 25 | 0.121 | 0.121 | 0.121 | 0.131 | 1.082 | 1.093 (Survey until done, extract) | Passed |
| 3 | 30 | 0.121 | 0.121 | 0.121 | 0.132 | 1.092 | 1.097 (Survey until done, extract, heavy strikes) | Passed |
| 3 | 35 | 0.121 | 0.120 | 0.120 | 0.131 | 1.087 | 1.089 (Survey until done, extract (cautious)) | Passed |
| 3 | 40 | 0.121 | 0.120 | 0.120 | 0.131 | 1.083 | 1.088 (Survey until done, extract, heavy strikes) | Passed |
| 3 | 45 | 0.121 | 0.121 | 0.121 | 0.132 | 1.090 | 1.090 (Survey twice, extract) | Passed |
| 3 | 50 | 0.121 | 0.121 | 0.121 | 0.133 | 1.100 | 1.102 (Survey once, extract (cautious)) | Passed |
| 4 | 30 | 0.082 | 0.082 | 0.082 | 0.089 | 1.085 | 1.099 (Survey until done, extract, heavy strikes) | Passed |
| 4 | 35 | 0.082 | 0.082 | 0.082 | 0.089 | 1.091 | 1.097 (Survey once, extract (cautious)) | Passed |
| 4 | 40 | 0.082 | 0.082 | 0.082 | 0.089 | 1.084 | 1.095 (Survey once, extract, heavy strikes) | Passed |
| 4 | 45 | 0.082 | 0.082 | 0.082 | 0.089 | 1.090 | 1.098 (Survey once, extract (cautious)) | Passed |
| 4 | 50 | 0.082 | 0.082 | 0.082 | 0.091 | 1.107 | 1.107 (Survey twice, extract) | Passed |
| 5 | 40 | 0.028 | 0.028 | 0.028 | 0.030 | 1.093 | 1.101 (Survey twice, extract) | Passed |
| 5 | 45 | 0.028 | 0.028 | 0.028 | 0.030 | 1.084 | 1.098 (Survey until done, extract) | Passed |
| 5 | 50 | 0.028 | 0.028 | 0.028 | 0.031 | 1.101 | 1.108 (Survey once, extract, heavy strikes) | Passed |

### Harvester charges per node by node tier and Gathering rank: the skilled session against today's one charge a unit (20000 nodes per plan; a Survey costs none and an Extract 2)

| Node tier | Rank | Today, charges per node | Strike only | Skilled session | Skilled / today | Verdict |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | 0 | 2.50 | 2.10 | 3.47 | 1.39 | Passed |
| 1 | 5 | 2.50 | 2.11 | 3.48 | 1.39 | Passed |
| 1 | 10 | 2.50 | 2.12 | 3.47 | 1.39 | Passed |
| 1 | 15 | 2.50 | 2.06 | 3.68 | 1.47 | Passed |
| 1 | 20 | 2.50 | 2.05 | 3.68 | 1.47 | Passed |
| 1 | 25 | 2.50 | 2.06 | 3.69 | 1.47 | Passed |
| 1 | 30 | 2.50 | 2.00 | 3.65 | 1.46 | Passed |
| 1 | 35 | 2.50 | 2.01 | 3.67 | 1.47 | Passed |
| 1 | 40 | 2.50 | 2.00 | 3.66 | 1.47 | Passed |
| 1 | 45 | 2.50 | 1.96 | 3.64 | 1.46 | Passed |
| 1 | 50 | 2.50 | 1.95 | 3.64 | 1.46 | Passed |
| 2 | 10 | 2.50 | 2.11 | 3.48 | 1.39 | Passed |
| 2 | 15 | 2.50 | 2.06 | 3.68 | 1.47 | Passed |
| 2 | 20 | 2.50 | 2.05 | 3.68 | 1.47 | Passed |
| 2 | 25 | 2.50 | 2.06 | 3.68 | 1.47 | Passed |
| 2 | 30 | 2.50 | 2.00 | 3.66 | 1.46 | Passed |
| 2 | 35 | 2.50 | 2.00 | 3.67 | 1.47 | Passed |
| 2 | 40 | 2.50 | 2.00 | 3.66 | 1.46 | Passed |
| 2 | 45 | 2.50 | 1.96 | 3.65 | 1.46 | Passed |
| 2 | 50 | 2.50 | 1.95 | 3.64 | 1.46 | Passed |
| 3 | 20 | 2.50 | 2.06 | 3.67 | 1.47 | Passed |
| 3 | 25 | 2.50 | 2.06 | 3.68 | 1.47 | Passed |
| 3 | 30 | 2.50 | 2.00 | 3.67 | 1.47 | Passed |
| 3 | 35 | 2.50 | 2.01 | 3.67 | 1.47 | Passed |
| 3 | 40 | 2.50 | 2.00 | 3.66 | 1.46 | Passed |
| 3 | 45 | 2.50 | 1.95 | 3.64 | 1.46 | Passed |
| 3 | 50 | 2.50 | 1.96 | 3.65 | 1.46 | Passed |
| 4 | 30 | 2.50 | 2.00 | 3.66 | 1.47 | Passed |
| 4 | 35 | 2.50 | 2.00 | 3.67 | 1.47 | Passed |
| 4 | 40 | 2.50 | 2.01 | 3.67 | 1.47 | Passed |
| 4 | 45 | 2.50 | 1.96 | 3.65 | 1.46 | Passed |
| 4 | 50 | 2.50 | 1.95 | 3.64 | 1.46 | Passed |
| 5 | 40 | 2.50 | 2.00 | 3.66 | 1.46 | Passed |
| 5 | 45 | 2.50 | 1.95 | 3.65 | 1.46 | Passed |
| 5 | 50 | 2.50 | 1.95 | 3.65 | 1.46 | Passed |

## Length

### Actions per node, target 3-8 (Leave is not an action; every action takes a few seconds)

| Node tier | Rank | Skilled actions per node | Skilled sessions of 3-8 actions | Strike only, actions per node | Skilled plan | Verdict |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | 0 | 3.55 | 75% | 2.10 | Survey until done, extract | Within |
| 1 | 5 | 3.56 | 76% | 2.11 | Survey until done, extract | Within |
| 1 | 10 | 3.55 | 75% | 2.12 | Survey twice, extract | Within |
| 1 | 15 | 3.83 | 75% | 2.06 | Survey until done, extract | Within |
| 1 | 20 | 3.83 | 75% | 2.05 | Survey until done, extract | Within |
| 1 | 25 | 3.33 | 75% | 2.06 | Survey twice, extract | Within |
| 1 | 30 | 3.30 | 75% | 2.00 | Survey twice, extract | Within |
| 1 | 35 | 3.31 | 75% | 2.01 | Survey once, extract | Within |
| 1 | 40 | 3.31 | 75% | 2.00 | Survey once, extract | Within |
| 1 | 45 | 3.29 | 75% | 1.96 | Survey once, extract | Within |
| 1 | 50 | 3.29 | 75% | 1.95 | Survey until done, extract | Within |
| 2 | 10 | 3.55 | 75% | 2.11 | Survey twice, extract | Within |
| 2 | 15 | 3.83 | 75% | 2.06 | Survey twice, extract | Within |
| 2 | 20 | 3.83 | 75% | 2.05 | Survey twice, extract | Within |
| 2 | 25 | 3.33 | 75% | 2.06 | Survey once, extract | Within |
| 2 | 30 | 3.31 | 75% | 2.00 | Survey twice, extract | Within |
| 2 | 35 | 3.32 | 75% | 2.00 | Survey once, extract | Within |
| 2 | 40 | 3.31 | 75% | 2.00 | Survey until done, extract | Within |
| 2 | 45 | 3.30 | 75% | 1.96 | Survey until done, extract | Within |
| 2 | 50 | 3.29 | 75% | 1.95 | Survey until done, extract | Within |
| 3 | 20 | 3.83 | 75% | 2.06 | Survey until done, extract | Within |
| 3 | 25 | 3.33 | 75% | 2.06 | Survey once, extract | Within |
| 3 | 30 | 3.32 | 75% | 2.00 | Survey twice, extract | Within |
| 3 | 35 | 3.31 | 75% | 2.01 | Survey once, extract | Within |
| 3 | 40 | 3.30 | 75% | 2.00 | Survey once, extract | Within |
| 3 | 45 | 3.29 | 75% | 1.95 | Survey twice, extract | Within |
| 3 | 50 | 3.30 | 75% | 1.96 | Survey twice, extract | Within |
| 4 | 30 | 3.32 | 75% | 2.00 | Survey until done, extract | Within |
| 4 | 35 | 3.32 | 75% | 2.00 | Survey twice, extract | Within |
| 4 | 40 | 3.32 | 75% | 2.01 | Survey once, extract | Within |
| 4 | 45 | 3.30 | 75% | 1.96 | Survey twice, extract | Within |
| 4 | 50 | 3.29 | 75% | 1.95 | Survey twice, extract | Within |
| 5 | 40 | 3.31 | 75% | 2.00 | Survey once, extract | Within |
| 5 | 45 | 3.30 | 75% | 1.95 | Survey once, extract | Within |
| 5 | 50 | 3.30 | 75% | 1.95 | Survey once, extract | Within |

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
| Tier 1 vein (level 5) | harvester tier 1, rank = vein level | 1500 | 2.5 | 432000 | 1679 | 2.5 | 490672 | 1.14 | - | Passed |
| Tier 2 vein (level 15) | harvester tier 2, rank = vein level | 1500 | 2.5 | 432000 | 1704 | 2.5 | 482602 | 1.12 | - | Passed |
| Tier 3 vein (level 25) | harvester tier 3, rank = vein level | 1500 | 2.5 | 432000 | 1705 | 2.3 | 534987 | 1.24 | - | Passed |
| Tier 4 vein (level 35) | harvester tier 4, rank = vein level | 1500 | 2.5 | 432000 | 1703 | 2.3 | 538540 | 1.25 | - | Passed |
| Tier 5 vein (level 45) | harvester tier 5, rank = vein level | 1500 | 2.5 | 432000 | 1706 | 2.3 | 544775 | 1.26 | - | Passed |

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
| Tier 1 vein (level 5) | harvester tier 1, rank = vein level | 1500 | 2.5 | 432000 | 1500 | 2.1 | 511439 | 1.18 | - | Passed |
| Tier 2 vein (level 15) | harvester tier 2, rank = vein level | 1500 | 2.5 | 432000 | 1504 | 2.1 | 525254 | 1.22 | - | Passed |
| Tier 3 vein (level 25) | harvester tier 3, rank = vein level | 1500 | 2.5 | 432000 | 1504 | 2.1 | 526246 | 1.22 | - | Passed |
| Tier 4 vein (level 35) | harvester tier 4, rank = vein level | 1500 | 2.5 | 432000 | 1499 | 2.0 | 539472 | 1.25 | - | Passed |
| Tier 5 vein (level 45) | harvester tier 5, rank = vein level | 1500 | 2.5 | 432000 | 1497 | 1.9 | 552898 | 1.28 | - | Passed |

## Evidence: charges, time and gems

### Charges, seconds and gems per node at rank = the vein's level (a Might modifier of 0 for today)

| Node tier | Rank | Charges per node, today and Quick Harvest | Charges per node, strike only | Charges per node, skilled | Charges per item, skilled | Seconds per node, today and Quick Harvest | Seconds per node, strike only | Seconds per node, skilled | Gems per node, today | Gems per node, skilled | Gems from pockets, skilled | Gems, skilled / today | Charges, skilled / today |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 5 | 2.50 | 2.11 | 3.48 | 1.24 | 12.5 | 10.6 | 12.3 | 0.218 | 0.232 | 0.015 | 1.066x | 1.39x |
| 2 | 15 | 2.50 | 2.06 | 3.68 | 1.30 | 12.5 | 10.3 | 12.7 | 0.171 | 0.184 | 0.014 | 1.080x | 1.47x |
| 3 | 25 | 2.50 | 2.06 | 3.68 | 1.30 | 12.5 | 10.3 | 11.5 | 0.121 | 0.131 | 0.010 | 1.082x | 1.47x |
| 4 | 35 | 2.50 | 2.00 | 3.67 | 1.29 | 12.5 | 10.0 | 11.4 | 0.082 | 0.089 | 0.008 | 1.091x | 1.47x |
| 5 | 45 | 2.50 | 1.95 | 3.65 | 1.28 | 12.5 | 9.7 | 11.3 | 0.028 | 0.030 | 0.002 | 1.084x | 1.46x |

## Evidence: Heavy Strike plans

### Careful play against Heavy Strike play at rank = the vein's level

| Node tier | Rank | Careful, items per node | Careful, actions | Careful, XP per hour / today | Heavy Strikes, items per node | Heavy Strikes, actions | Heavy Strikes, XP per hour / today |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 5 | 2.80 | 3.56 | 1.14 | 2.79 | 3.55 | 1.14 |
| 2 | 15 | 2.84 | 3.83 | 1.12 | 2.83 | 3.83 | 1.12 |
| 3 | 25 | 2.84 | 3.33 | 1.24 | 2.83 | 3.23 | 1.26 |
| 4 | 35 | 2.84 | 3.32 | 1.25 | 2.83 | 3.21 | 1.28 |
| 5 | 45 | 2.84 | 3.30 | 1.26 | 2.83 | 3.20 | 1.28 |

## Evidence: Gathering rank milestones

### Rank milestones at a tier 1 node (the same milestones apply at every tier)

| Rank | Seams one Survey reveals | Tier of yield | Units of a Strike (mean) | Units of a Heavy Strike (mean) | Skilled items per node | Skilled actions per node | Strike-only actions per node |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | 1 | 0 | 1.30 | 3.00 | 2.795 | 3.55 | 2.10 |
| 5 | 1 | 0 | 1.30 | 3.00 | 2.799 | 3.56 | 2.11 |
| 10 | 1 | 0 | 1.30 | 3.00 | 2.788 | 3.55 | 2.12 |
| 15 | 1 | 1 | 1.35 | 3.10 | 2.838 | 3.83 | 2.06 |
| 20 | 1 | 1 | 1.35 | 3.10 | 2.836 | 3.83 | 2.05 |
| 25 | 2 | 1 | 1.35 | 3.10 | 2.840 | 3.33 | 2.06 |
| 30 | 2 | 2 | 1.40 | 3.20 | 2.829 | 3.30 | 2.00 |
| 35 | 2 | 2 | 1.40 | 3.20 | 2.847 | 3.31 | 2.01 |
| 40 | 2 | 2 | 1.40 | 3.20 | 2.834 | 3.31 | 2.00 |
| 45 | 2 | 3 | 1.45 | 3.30 | 2.833 | 3.29 | 1.96 |
| 50 | 3 | 3 | 1.45 | 3.30 | 2.838 | 3.29 | 1.95 |

## Evidence: collapse

### A player who does not keep a Stability reserve (averaged over the tier's kinds of node)

| Node tier | Rank | Seams first, no reserve: items per node | Share of nodes collapsed | Units lost with the node | Skilled items per node |
| --- | --- | --- | --- | --- | --- |
| 1 | 5 | 2.77 | 14.6% | 0.15 | 2.80 |
| 2 | 15 | 2.78 | 14.1% | 0.14 | 2.84 |
| 3 | 25 | 2.92 | 0.0% | 0.00 | 2.84 |
| 4 | 35 | 2.92 | 0.0% | 0.00 | 2.84 |
| 5 | 45 | 2.91 | 0.0% | 0.00 | 2.84 |

## Evidence: refining XP

### Refining XP per unit by ore, before and now

| Ore | Ore tier | Ore activity level | Refining XP per unit before (flat) | Now, at rank = level | At level - 5 ranks | At level + 1 rank | At level + 3 ranks | At level + 5 ranks | At rank 0 | At rank 50 | At level, now / before |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| ore_tilarium | 1 | 5 | 25 | 25 | 44 | 19 | 6 | 0 | 44 | 0 | 1.00x |
| raw_veldite | 1 | 5 | 25 | 25 | 44 | 19 | 6 | 0 | 44 | 0 | 1.00x |
| ore_currian | 2 | 15 | 50 | 50 | 88 | 38 | 13 | 0 | 100 | 0 | 1.00x |
| raw_scordspar | 2 | 15 | 50 | 50 | 88 | 38 | 13 | 0 | 100 | 0 | 1.00x |
| ore_idailia | 3 | 25 | 75 | 75 | 131 | 56 | 19 | 0 | 150 | 0 | 1.00x |
| raw_plagionite | 3 | 25 | 75 | 75 | 131 | 56 | 19 | 0 | 150 | 0 | 1.00x |
| ore_barinium | 4 | 35 | 100 | 100 | 175 | 75 | 25 | 0 | 200 | 0 | 1.00x |
| raw_keromber | 4 | 35 | 100 | 100 | 175 | 75 | 25 | 0 | 200 | 0 | 1.00x |
| ore_gostian | 5 | 45 | 125 | 125 | 219 | 94 | 31 | 0 | 250 | 0 | 1.00x |
| raw_jasioclase | 5 | 45 | 125 | 125 | 219 | 94 | 31 | 0 | 250 | 0 | 1.00x |
| ore_arda | 5 | 45 | 150 | 150 | 263 | 113 | 38 | 0 | 300 | 0 | 1.00x |
| raw_arkoxit | 5 | 45 | 150 | 150 | 263 | 113 | 38 | 0 | 300 | 0 | 1.00x |

## Limitations

- The skilled player is the best of a set of plans a person can follow from the window, not an optimal player. A better plan could take more items per node, so the +15% cap is checked against these plans, and the margin below it is the room for one.
- Items are counted, not priced. A gem pocket gives a gem in place of a unit, so it adds no item but moves value into gems; the economy-parity gate caps the gems of a session against today's, and a pocket is rare enough to stay within the cap (the evidence table's gems from pockets). The extra ore roll of a rich seam is the only way a session yields more than the pool.
- Respawn, node density and travel are not modelled, so these are yields per node, not per hour. The XP-rate gate counts the seconds of the actions only; walking between nodes costs both loops the same.
- Seconds per action are estimates until the client run measures them. They are the cohort's only timing input: a session whose actions take longer than modelled earns less per hour than the gate shows, and one that takes less earns more.
- A skilled session spends more charges per item than today (the evidence table), because an Extract costs a charge more than a Strike and yields no unit of its own beyond the seam's (a Survey costs none). A harvester lasts about a third fewer skilled sessions than Quick Harvests; a strike-only session spends fewer charges than either.
- Perks are not modelled; the Prospector draft's magnitudes are estimates against these sessions, and no perk code exists. The client was not run.
