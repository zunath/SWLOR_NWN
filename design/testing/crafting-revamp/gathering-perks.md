# Crafting revamp: the Gathering perk lines (Prospector and Treasure Seeker)

Command: `SWLOR.CraftSim gathering-perks --runs 20000 --seed 1`

## Inputs

- Seed 1; 20000 nodes or boards per case and plan. The unperked gatherer is the one `harvesting-prospecting-parity.md` and `scavenging-search-parity.md` describe, and Quick Harvest and Quick Search read no perk, so their numbers do not move.
- The perks are read from the game's own definitions (`GatheringPerkDefinition`) and a gatherer owns every purchase their Gathering rank allows; a style adds what its status effect carries. The loadouts are the lines and the styles in every combination the gates run.
- Economy bounds (the coordinator's, October 23): a skilled session of any plan stays within 1.15x of today's items and gems per node and of Quick Search's rolls per board, and spends at most about 1.5x of today's harvester charges per node. The Treasure Seeker line's value is its rare finds, which no cap limits.
- Why the Prospector line spends time and not Stability: a skilled session already claims nearly every seam its Stability allows, so it already sits at the cap (1.136x, and 1.145x for the best of nine plans). Any Stability a perk saves lets it claim more and lifts it past 1.15x (Efficient Strikes at -2 and -4 Stability alone took it to 1.151x and 1.163x; a cheaper Extract of a rich seam to 1.165x). A perk that adds an item, an extra ore roll or a gem fails the cap by construction. Time and power cores are not what decides a seam, so the line saves those.

## Prospecting

### The skilled session with each purchase, over node tiers 1, 3, 5 and Gathering ranks 20, 30, 40, 50 (20000 nodes per plan and case). The cap: items and gems at most 1.15x of today's, charges at most 1.5x.

| Purchase or loadout | Skilled items / today | Highest of any plan / today | Where | Highest gems / today | Skilled charges / today (worst case) | Skilled seconds per node | Skilled actions per node | Nodes collapsed |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| No perks | 1.135 | 1.139 | tier 3 / rank 50 | 1.108 | 1.47 | 11.6 (1.00x) | 3.41 | 0.0% |
| Efficient Strikes I | 1.135 | 1.139 | tier 3 / rank 50 | 1.108 | 1.47 | 11.1 (0.96x) | 3.41 | 0.0% |
| Efficient Strikes II | 1.135 | 1.139 | tier 3 / rank 50 | 1.108 | 1.47 | 10.6 (0.92x) | 3.41 | 0.0% |
| Rich Veins I | 1.135 | 1.139 | tier 3 / rank 50 | 1.108 | 1.47 | 10.9 (0.94x) | 3.41 | 0.0% |
| Rich Veins II | 1.135 | 1.139 | tier 3 / rank 50 | 1.108 | 1.47 | 10.3 (0.88x) | 3.41 | 0.0% |
| Core Sample | 1.135 | 1.139 | tier 3 / rank 50 | 1.108 | 1.47 | 11.5 (0.99x) | 3.38 | 0.0% |
| Mother Lode | 1.135 | 1.139 | tier 3 / rank 50 | 1.108 | 1.47 | 10.2 (0.88x) | 3.41 | 0.0% |
| Strip Mining (style) | 1.113 | 1.125 | tier 3 / rank 50 | 1.094 | 1.39 | 11.5 (0.99x) | 3.11 | 0.0% |
| Sharp Chisel I | 1.135 | 1.139 | tier 3 / rank 50 | 1.108 | 1.47 | 11.6 (1.00x) | 3.41 | 0.0% |
| Sharp Chisel II | 1.135 | 1.139 | tier 3 / rank 50 | 1.108 | 1.47 | 11.6 (1.00x) | 3.41 | 0.0% |
| Deep Scan | 1.135 | 1.139 | tier 3 / rank 50 | 1.110 | 1.48 | 11.4 (0.98x) | 3.31 | 0.0% |
| Prospector, no style (every purchase the rank allows) | 1.135 | 1.139 | tier 3 / rank 50 | 1.108 | 1.47 | 9.3 (0.80x) | 3.41 | 0.0% |
| Prospector, Strip Mining (every purchase the rank allows) | 1.113 | 1.125 | tier 3 / rank 50 | 1.094 | 1.39 | 9.3 (0.80x) | 3.11 | 0.0% |
| Treasure Seeker, no style (every purchase the rank allows) | 1.135 | 1.139 | tier 3 / rank 50 | 1.110 | 1.47 | 11.6 (1.00x) | 3.41 | 0.0% |
| Treasure Seeker, Careful Hands (every purchase the rank allows) | 1.135 | 1.139 | tier 3 / rank 50 | 1.110 | 1.47 | 11.6 (1.00x) | 3.41 | 0.0% |
| Both lines, no style (every purchase the rank allows) | 1.135 | 1.139 | tier 3 / rank 50 | 1.110 | 1.47 | 9.3 (0.80x) | 3.41 | 0.0% |
| Both lines, Strip Mining (every purchase the rank allows) | 1.113 | 1.125 | tier 3 / rank 50 | 1.094 | 1.39 | 9.3 (0.80x) | 3.11 | 0.0% |
| Both lines, Careful Hands (every purchase the rank allows) | 1.135 | 1.139 | tier 3 / rank 50 | 1.110 | 1.47 | 9.3 (0.80x) | 3.41 | 0.0% |

## Search

### The skilled player at site level 1 with each purchase, mean over Gathering ranks 20, 35, 50 (20000 boards per case). The cap: finds at most 1.15x of Quick Search's rolls at every rank. Rare supply is over the tables SCAVENGING_BUSH_1, SCAVENGING_BUSH_3, SCAVENGING_BUSH_5, VEGETABLES_PATCH_3, SUPPLY_CRATE_3, SUPPLY_CRATE_5, KORRIBAN_SITH_CRATE_1, KORRIBAN_SITH_CRATE_3, TATOOINE_TUSKEN_CRATE, VISCARA_COXXIAN_CRATE, VISCARA_MANDALORIAN_CRATE, QIONHIVE_RESOURCES

| Purchase or loadout | Skilled finds per board | Added finds | Skilled / Quick Search (highest rank) | Dig Deeper finds | Rare items per site / Quick Search at the highest rank (mean of tables) | Lowest table | Highest table |
| --- | --- | --- | --- | --- | --- | --- | --- |
| No perks | 1.068 | - | 1.064x | 0.266 | 1.49x | 1.33x | 1.55x |
| Fine Trowel I | 1.073 | 0.006 | 1.076x | 0.270 | 1.68x | 1.42x | 1.80x |
| Fine Trowel II | 1.069 | 0.001 | 1.070x | 0.266 | 1.82x | 1.45x | 2.00x |
| Steady Dig | 1.090 | 0.022 | 1.081x | 0.290 | 1.53x | 1.36x | 1.60x |
| Keen Eye | 1.109 | 0.042 | 1.109x | 0.239 | 1.47x | 1.33x | 1.53x |
| Rich Pickings | 1.068 | 0.000 | 1.064x | 0.266 | 2.21x | 1.84x | 2.35x |
| Jackpot | 1.068 | 0.000 | 1.064x | 0.266 | 2.11x | 1.53x | 2.42x |
| Careful Hands, Search half (style) | 1.051 | -0.017 | 1.046x | 0.269 | 2.00x | 1.50x | 2.25x |
| Treasure Seeker, no style (every purchase the rank allows) | 1.122 | 0.054 | 1.120x | 0.269 | 3.18x | 2.18x | 3.72x |
| Treasure Seeker, Careful Hands (every purchase the rank allows) | 1.080 | 0.012 | 1.081x | 0.286 | 3.59x | 2.21x | 4.44x |
| Both lines, no style (every purchase the rank allows) | 1.122 | 0.054 | 1.120x | 0.269 | 3.18x | 2.18x | 3.72x |
| Both lines, Strip Mining (every purchase the rank allows) | 1.122 | 0.054 | 1.120x | 0.269 | 3.18x | 2.18x | 3.72x |
| Both lines, Careful Hands (every purchase the rank allows) | 1.080 | 0.012 | 1.081x | 0.286 | 3.59x | 2.21x | 4.44x |

## Gate 2: style balance

The two styles never compete inside one window. Strip Mining changes only the Prospecting session and Careful Hands only the Search board, so a harvester chooses between Strip Mining and the plain rules, and a scavenger between Careful Hands and the plain rules. Prospecting is judged on the nine kinds of node (a pool of 1 to 4 units and 1 to 3 seams) at the ranks a style is open at: both players are played on the same nodes and dice, each by the best of the skilled plans for the kind, and scored by items per second of play with 30 seconds to reach the next node. A point is one percent of the better score. The verdict is the pooled row; the rows of single ranks show how the balance moves. Careful Hands has no partner on the Search board, so it has no pair to balance: the table in the Search section shows what it trades (finds for rare items), and a scavenger who wants common materials plays the plain board.

### Prospecting: Strip Mining against no style, no other perks

| Gathering rank | Kinds of node | Strip Mining better | No style better | Mean lead of Strip Mining (points) | Same, weighted by how often the kind occurs | Verdict |
| --- | --- | --- | --- | --- | --- | --- |
| 20 | 9 | 78% | 22% | 2.6 | 0.2 | Outside |
| 25 | 9 | 56% | 44% | 1.5 | -0.5 | Within |
| 30 | 9 | 50% | 50% | 1.6 | -0.4 | Within |
| 35 | 9 | 56% | 44% | 1.6 | -0.5 | Within |
| 40 | 9 | 56% | 44% | 1.6 | -0.4 | Within |
| 45 | 9 | 56% | 44% | 1.9 | -0.1 | Within |
| 50 | 9 | 56% | 44% | 1.7 | -0.4 | Within |
| All ranks (the gate) | 63 | 58% | 42% | 1.8 | -0.3 | Passed |

### Prospecting: Strip Mining against no style, with the whole Prospector line

| Gathering rank | Kinds of node | Strip Mining better | No style better | Mean lead of Strip Mining (points) | Same, weighted by how often the kind occurs | Verdict |
| --- | --- | --- | --- | --- | --- | --- |
| 20 | 9 | 78% | 22% | 2.6 | 0.2 | Outside |
| 25 | 9 | 56% | 44% | 2.6 | 0.3 | Within |
| 30 | 9 | 61% | 39% | 2.7 | 0.4 | Within |
| 35 | 9 | 56% | 44% | 2.7 | 0.3 | Within |
| 40 | 9 | 56% | 44% | 1.5 | -0.5 | Within |
| 45 | 9 | 56% | 44% | 1.7 | -0.3 | Within |
| 50 | 9 | 44% | 56% | 0.4 | -1.4 | Within |
| All ranks (the gate) | 63 | 58% | 42% | 2.0 | -0.1 | Passed |

## The parity gates for every loadout

Each loadout plays the harvesting study (`HarvestingReport`: economy parity with the +15% item and gem caps and the 1.5x charge cap, length, XP rate) and the search study (`SearchReport`: the +15% cap on rolls, length, XP rate) as a gatherer who owns every purchase the Gathering rank allows. `SWLOR.CraftSim harvesting --loadout <name>` and `search --loadout <name>` write any of them in full.

### Verdicts

| Loadout | Harvesting: economy parity | Harvesting: length | Harvesting: XP rate | Search: economy parity | Search: length | Search: XP rate |
| --- | --- | --- | --- | --- | --- | --- |
| No perks | Passed | Passed | Passed | Passed | Passed | Passed |
| Prospector, no style | Passed | Passed | Passed | Passed | Passed | Passed |
| Prospector, Strip Mining | Passed | Passed | Passed | Passed | Passed | Passed |
| Treasure Seeker, no style | Passed | Passed | Passed | Passed | Passed | Passed |
| Treasure Seeker, Careful Hands | Passed | Passed | Passed | Passed | Passed | Passed |
| Both lines, no style | Passed | Passed | Passed | Passed | Passed | Passed |
| Both lines, Strip Mining | Passed | Passed | Passed | Passed | Passed | Passed |
| Both lines, Careful Hands | Passed | Passed | Passed | Passed | Passed | Passed |

### No perks

- Harvesting, economy parity: Quick Harvest equals today's 2.50 items, 12.5 s and charges per node, with the same XP per item. A skilled session yields 1.115x (lowest, tier 1 / rank 10) of today's items per node, and no plan, careful or fast, reaches more than 1.140x (tier 1 / rank 35); the band is 99% to 115%. Gems per node, the ordinary rolls and the pockets together, reach at most 1.108x of today's (tier 5 / rank 50) against a cap of 115%. A skilled session spends at most 1.47x of today's harvester charges per node (tier 1 / rank 25) against a cap of 150%.
- Harvesting, length: The skilled player's sessions last 3.29 to 3.83 actions on average in every case (target 3-8).
- Harvesting, XP rate: Harvesting, Quick Harvest: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.00x in Tier 1 vein (level 5) / harvester tier 1, rank = vein level). Harvesting, skilled session: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.12x in Tier 2 vein (level 15) / harvester tier 2, rank = vein level).
- Search, economy parity: Quick Search equals today's expected rolls at every rank; a careless board finds at least 0.85x, the skilled player 1.03x to 1.08x, the oracle at most 1.40x, and the whole Treasure Seeker line stays inside the cap.
- Search, length: Every board lasts 3-8 actions (gathering node), for every player, at every rank.
- Search, XP rate: Scavenging: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.02x in Site level 5 / rank 50, Hard Look level 5).

### Prospector, no style

- Harvesting, economy parity: Quick Harvest equals today's 2.50 items, 12.5 s and charges per node, with the same XP per item. A skilled session yields 1.115x (lowest, tier 1 / rank 10) of today's items per node, and no plan, careful or fast, reaches more than 1.140x (tier 1 / rank 35); the band is 99% to 115%. Gems per node, the ordinary rolls and the pockets together, reach at most 1.108x of today's (tier 5 / rank 50) against a cap of 115%. A skilled session spends at most 1.47x of today's harvester charges per node (tier 1 / rank 25) against a cap of 150%.
- Harvesting, length: The skilled player's sessions last 3.29 to 3.83 actions on average in every case (target 3-8).
- Harvesting, XP rate: Harvesting, Quick Harvest: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.00x in Tier 1 vein (level 5) / harvester tier 1, rank = vein level). Harvesting, skilled session: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.19x in Tier 1 vein (level 5) / harvester tier 1, rank = vein level).
- Search, economy parity: Quick Search equals today's expected rolls at every rank; a careless board finds at least 0.85x, the skilled player 1.03x to 1.08x, the oracle at most 1.40x, and the whole Treasure Seeker line stays inside the cap.
- Search, length: Every board lasts 3-8 actions (gathering node), for every player, at every rank.
- Search, XP rate: Scavenging: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.02x in Site level 5 / rank 50, Hard Look level 5).

### Prospector, Strip Mining

- Harvesting, economy parity: Quick Harvest equals today's 2.50 items, 12.5 s and charges per node, with the same XP per item. A skilled session yields 1.083x (lowest, tier 3 / rank 20) of today's items per node, and no plan, careful or fast, reaches more than 1.136x (tier 2 / rank 15); the band is 99% to 115%. Gems per node, the ordinary rolls and the pockets together, reach at most 1.094x of today's (tier 5 / rank 50) against a cap of 115%. A skilled session spends at most 1.47x of today's harvester charges per node (tier 1 / rank 15) against a cap of 150%.
- Harvesting, length: The skilled player's sessions last 3.07 to 3.83 actions on average in every case (target 3-8).
- Harvesting, XP rate: Harvesting, Quick Harvest: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.00x in Tier 1 vein (level 5) / harvester tier 1, rank = vein level). Harvesting, skilled session: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.19x in Tier 1 vein (level 5) / harvester tier 1, rank = vein level).
- Search, economy parity: Quick Search equals today's expected rolls at every rank; a careless board finds at least 0.85x, the skilled player 1.03x to 1.08x, the oracle at most 1.40x, and the whole Treasure Seeker line stays inside the cap.
- Search, length: Every board lasts 3-8 actions (gathering node), for every player, at every rank.
- Search, XP rate: Scavenging: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.02x in Site level 5 / rank 50, Hard Look level 5).

### Treasure Seeker, no style

- Harvesting, economy parity: Quick Harvest equals today's 2.50 items, 12.5 s and charges per node, with the same XP per item. A skilled session yields 1.115x (lowest, tier 1 / rank 10) of today's items per node, and no plan, careful or fast, reaches more than 1.140x (tier 1 / rank 35); the band is 99% to 115%. Gems per node, the ordinary rolls and the pockets together, reach at most 1.110x of today's (tier 5 / rank 40) against a cap of 115%. A skilled session spends at most 1.47x of today's harvester charges per node (tier 1 / rank 25) against a cap of 150%.
- Harvesting, length: The skilled player's sessions last 3.29 to 3.83 actions on average in every case (target 3-8).
- Harvesting, XP rate: Harvesting, Quick Harvest: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.00x in Tier 1 vein (level 5) / harvester tier 1, rank = vein level). Harvesting, skilled session: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.12x in Tier 2 vein (level 15) / harvester tier 2, rank = vein level).
- Search, economy parity: Quick Search equals today's expected rolls at every rank; a careless board finds at least 0.85x, the skilled player 1.03x to 1.15x, the oracle at most 1.40x, and the whole Treasure Seeker line stays inside the cap.
- Search, length: Every board lasts 3-8 actions (gathering node), for every player, at every rank.
- Search, XP rate: Scavenging: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.04x in Site level 4 / rank 40, Hard Look level 5).

### Treasure Seeker, Careful Hands

- Harvesting, economy parity: Quick Harvest equals today's 2.50 items, 12.5 s and charges per node, with the same XP per item. A skilled session yields 1.115x (lowest, tier 1 / rank 10) of today's items per node, and no plan, careful or fast, reaches more than 1.140x (tier 1 / rank 35); the band is 99% to 115%. Gems per node, the ordinary rolls and the pockets together, reach at most 1.110x of today's (tier 5 / rank 40) against a cap of 115%. A skilled session spends at most 1.47x of today's harvester charges per node (tier 1 / rank 25) against a cap of 150%.
- Harvesting, length: The skilled player's sessions last 3.29 to 3.83 actions on average in every case (target 3-8).
- Harvesting, XP rate: Harvesting, Quick Harvest: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.00x in Tier 1 vein (level 5) / harvester tier 1, rank = vein level). Harvesting, skilled session: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.12x in Tier 2 vein (level 15) / harvester tier 2, rank = vein level).
- Search, economy parity: Quick Search equals today's expected rolls at every rank; a careless board has no floor (a style trades finds for rarer ones), the skilled player 1.03x to 1.15x, the oracle at most 1.40x, and the whole Treasure Seeker line stays inside the cap.
- Search, length: Every board lasts 3-8 actions (gathering node), for every player, at every rank.
- Search, XP rate: Scavenging: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.01x in Site level 4 / rank 40, Hard Look level 5).

### Both lines, no style

- Harvesting, economy parity: Quick Harvest equals today's 2.50 items, 12.5 s and charges per node, with the same XP per item. A skilled session yields 1.115x (lowest, tier 1 / rank 10) of today's items per node, and no plan, careful or fast, reaches more than 1.140x (tier 1 / rank 35); the band is 99% to 115%. Gems per node, the ordinary rolls and the pockets together, reach at most 1.110x of today's (tier 5 / rank 40) against a cap of 115%. A skilled session spends at most 1.47x of today's harvester charges per node (tier 1 / rank 25) against a cap of 150%.
- Harvesting, length: The skilled player's sessions last 3.29 to 3.83 actions on average in every case (target 3-8).
- Harvesting, XP rate: Harvesting, Quick Harvest: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.00x in Tier 1 vein (level 5) / harvester tier 1, rank = vein level). Harvesting, skilled session: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.19x in Tier 1 vein (level 5) / harvester tier 1, rank = vein level).
- Search, economy parity: Quick Search equals today's expected rolls at every rank; a careless board finds at least 0.85x, the skilled player 1.03x to 1.15x, the oracle at most 1.40x, and the whole Treasure Seeker line stays inside the cap.
- Search, length: Every board lasts 3-8 actions (gathering node), for every player, at every rank.
- Search, XP rate: Scavenging: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.04x in Site level 4 / rank 40, Hard Look level 5).

### Both lines, Strip Mining

- Harvesting, economy parity: Quick Harvest equals today's 2.50 items, 12.5 s and charges per node, with the same XP per item. A skilled session yields 1.083x (lowest, tier 3 / rank 20) of today's items per node, and no plan, careful or fast, reaches more than 1.136x (tier 2 / rank 15); the band is 99% to 115%. Gems per node, the ordinary rolls and the pockets together, reach at most 1.094x of today's (tier 5 / rank 50) against a cap of 115%. A skilled session spends at most 1.47x of today's harvester charges per node (tier 1 / rank 15) against a cap of 150%.
- Harvesting, length: The skilled player's sessions last 3.07 to 3.83 actions on average in every case (target 3-8).
- Harvesting, XP rate: Harvesting, Quick Harvest: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.00x in Tier 1 vein (level 5) / harvester tier 1, rank = vein level). Harvesting, skilled session: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.19x in Tier 1 vein (level 5) / harvester tier 1, rank = vein level).
- Search, economy parity: Quick Search equals today's expected rolls at every rank; a careless board finds at least 0.85x, the skilled player 1.03x to 1.15x, the oracle at most 1.40x, and the whole Treasure Seeker line stays inside the cap.
- Search, length: Every board lasts 3-8 actions (gathering node), for every player, at every rank.
- Search, XP rate: Scavenging: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.04x in Site level 4 / rank 40, Hard Look level 5).

### Both lines, Careful Hands

- Harvesting, economy parity: Quick Harvest equals today's 2.50 items, 12.5 s and charges per node, with the same XP per item. A skilled session yields 1.115x (lowest, tier 1 / rank 10) of today's items per node, and no plan, careful or fast, reaches more than 1.140x (tier 1 / rank 35); the band is 99% to 115%. Gems per node, the ordinary rolls and the pockets together, reach at most 1.110x of today's (tier 5 / rank 40) against a cap of 115%. A skilled session spends at most 1.47x of today's harvester charges per node (tier 1 / rank 25) against a cap of 150%.
- Harvesting, length: The skilled player's sessions last 3.29 to 3.83 actions on average in every case (target 3-8).
- Harvesting, XP rate: Harvesting, Quick Harvest: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.00x in Tier 1 vein (level 5) / harvester tier 1, rank = vein level). Harvesting, skilled session: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.19x in Tier 1 vein (level 5) / harvester tier 1, rank = vein level).
- Search, economy parity: Quick Search equals today's expected rolls at every rank; a careless board has no floor (a style trades finds for rarer ones), the skilled player 1.03x to 1.15x, the oracle at most 1.40x, and the whole Treasure Seeker line stays inside the cap.
- Search, length: Every board lasts 3-8 actions (gathering node), for every player, at every rank.
- Search, XP rate: Scavenging: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.01x in Site level 4 / rank 40, Hard Look level 5).

## Bulk Refining

Refining converts ore one for one, so Bulk Refining changes only the power cores a batch costs, which are bought with credits: it is a credit saving and no supply. A core refined 3 items before the revamp, and 9 with Refinery Management VI at Gathering 45 (the plan gives those six to everyone by rank). The line adds 1, 2 and 3 on top: 12 at most, 33% over the old maximum of 9 and 25% fewer cores than the plan's batch at Gathering 45. Refining XP does not depend on the batch.

### Power cores for a batch of ore, by Gathering rank, with the Prospector line

| Gathering rank | Items per power core, rank only | Bulk Refining | Items per power core, with it | Power cores for 100 ore | With Bulk Refining | Power cores saved |
| --- | --- | --- | --- | --- | --- | --- |
| 0 | 4 | +0 | 4 | 25 | 25 | 0% |
| 10 | 5 | +1 | 6 | 20 | 17 | 15% |
| 20 | 6 | +1 | 7 | 17 | 15 | 12% |
| 30 | 7 | +2 | 9 | 15 | 12 | 20% |
| 40 | 8 | +2 | 10 | 13 | 10 | 23% |
| 45 | 9 | +3 | 12 | 12 | 9 | 25% |
| 50 | 9 | +3 | 12 | 12 | 9 | 25% |

## Limitations

- No client run: the windows, the style lists and the timing of the quicker actions are unverified in the game. Seconds per action are the engine's, and 30 seconds to reach the next node is the progression model's unmeasured estimate.
- The skilled player is the study's best careful plan; real players are neither as consistent nor as unaware of what is hidden. Sampling noise on the highest of nine plans is about half a point.
- Gate 2's score is items per second, and gems are counted as items.
- Gem pockets are about 2% of tier 1 nodes and 0.3% of tier 5 nodes (a pocket is `GemPocketShare` of a table's gem chance), so the pocket perks (Sharp Chisel, Deep Scan's pocket order, Core Sample's pocket warning) rarely matter in play.
