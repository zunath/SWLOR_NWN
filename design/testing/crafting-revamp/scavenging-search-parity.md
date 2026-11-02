# Crafting revamp: the Search board and Quick Search against today's scavenging

- Command: `SWLOR.CraftSim search --runs 12000 --seed 1`
- Seed 1; 12000 runs per case.

## Inputs

- Today (what Quick Search keeps): one attempt, plus a second at `Hard Look level x 10 + Perception modifier x 5` percent. An attempt succeeds on `d20 + Perception modifier` against a DC of 6, rising by a d3 after each attempt; a success rolls the site's loot table once and pays the site's XP, a failure pays a quarter of it. Hard Look level is the Gathering rank's tier (`CraftRankMilestones`). The Perception modifier is the parity reference of a character with no investment, 0 (owner decision, October 22 2026); no code reads an attribute.
- The board: the Search engine, 12000 boards per case, each with its own dice from seed 1. A 3x3 board; the Gathering rank sets the searches (`CraftRankMilestones.SearchesForRank`, replacing Hard Look), the promising spots (`PromisingSpotsForRank`, replacing Perception) and the chance a Search finds on promising ground (`PromisingChanceForRank`): from rank 0: 6 searches, 2 promising spots, 29% on promising ground; from rank 10: 6 searches, 2 promising spots, 32% on promising ground; from rank 20: 6 searches, 2 promising spots, 36% on promising ground; from rank 30: 6 searches, 2 promising spots, 39% on promising ground; from rank 40: 6 searches, 2 promising spots, 43% on promising ground. Searches find 8% on ordinary ground. A Dig Deeper risks 30% of ruining the spot, 20 points more for each dig before it, and finds 20% of the time when it does not, rolling the loot table with its rare entries weighted x3. A Sift shows the four spots next to a spot. Every action costs one search.
- Players: **Careless** searches the spots in reading order and never sifts or digs. **Skilled** plays from what the window shows (it does not read the hidden layout): every spot is a chain of actions, the best split of the searches among the chains is a small knapsack, and it Sifts when the ground a Sift would show raises that value by more than the search it costs. It is a floor on skilled play, not an optimal player. **Oracle** is told the whole layout for free; no real player can reach it.
- Yield is the loot-table rolls a site gives (a find is one roll). Limits: Quick Search equals today's expected rolls exactly; careless at least 0.85x of it; skilled 1.03x to 1.08x with no perks; oracle at most 1.40x; each within 0.02 for sampling error. With the whole Treasure Seeker line the skilled player stays within the plan's +15% (1.15x), the line being sized to stay inside the cap with its value in the rare finds (+1 points on every Search, 85% of the dig risk, rare entries weigh 2 more in a dig, rare entries weigh 1 more in an ordinary find, rare entries weigh 4 more in the first dig find of a board).
- Time: 30.0 s to reach and open each site in both loops (the progression model's estimate, unmeasured); today's search takes 2.0 s once the site is open (one get-low animation before its one or two attempts, so a second attempt adds no time) and a board action 6.0 s (`SearchRules.ActionSeconds`, which the game enforces as the least time between two actions, the first free). Both are estimates until the client run measures them.
- XP: every action pays Gathering XP. A Search or Dig Deeper attempt pays what the site pays against the Gathering rank (`Skill.GetDeltaXP(10 x site level - rank)`), a quarter on failure, as scavenging always did, times the multiplier by site level the game applies (`CraftMinigameXp`): level 1 1.05x, level 2 1.05x, level 3 1.1x, level 4 1.12x, level 5 1.12x. A Sift finds nothing, so it pays the quarter an attempt that finds nothing pays.
- XP cohort (gate 6): site levels 1 to 5, the Gathering rank 10 times the level, no tool. The gate judges the lower XP per hour of the careless and the skilled player.
- Named starting values of the board (`SearchRules`), all tunable:
  - `OrdinaryChance` = 8
  - `MinimumChance` = 1
  - `MaximumChance` = 95
  - `DigRiskBase` = 30
  - `DigRiskStep` = 20
  - `DigRiskMaximum` = 90
  - `DigFindChance` = 20
  - `MaximumDigFindChance` = 95
  - `ActionSeconds` = 6
  - `QuickSearchSeconds` = 2
  - `XpMultiplierLevel1` = 1.05
  - `XpMultiplierLevel2` = 1.05
  - `XpMultiplierLevel3` = 1.1
  - `XpMultiplierLevel4` = 1.12
  - `XpMultiplierLevel5` = 1.12

## Verdicts

| Gate | Verdict | Summary |
| --- | --- | --- |
| Economy parity | Passed | Quick Search equals today's expected rolls at every rank; a careless board finds at least 0.85x, the skilled player 1.03x to 1.08x, the oracle at most 1.40x, and the whole Treasure Seeker line stays inside the cap. |
| Length | Passed | Every board lasts 3-8 actions (gathering node), for every player, at every rank. |
| XP rate | Passed | Scavenging: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.01x in Site level 4 / rank 40, Hard Look level 5). |
| Quick Search XP rate | Passed | Quick Search pays today's XP in today's time at every site level, so its XP per hour is today's and not above it. |
| Evidence: XP rate by player | NotApplicable | Not a gate. The XP per hour of each simulated scavenger against today's; the gate judges the lower of the two. |
| Evidence: rare supply | NotApplicable | Not a gate. How the board changes the rare items real scavenge sites give: Dig Deeper's finds roll the table with its rare entries weighted x3, and they replace searches that would mostly have rolled the common items. The plan caps the rolls, not what they roll; the owner decides whether the rare supply needs a cap of its own. |
| Evidence: Treasure Seeker magnitudes | NotApplicable | Not a gate. What each proposed Treasure Seeker purchase adds to the skilled player's finds; the draft quotes it. The cap above holds for the whole line. |

## Economy parity

### Quick Search against today's expected rolls (a Perception modifier of 0, the parity reference)

| Gathering rank | Hard Look level | Second attempt | Today's rolls | Quick Search rolls | Quick Search played | Today's attempts | Quick Search attempts | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | 1 | 10% | 0.815 | 0.815 | 0.815 | 1.10 | 1.10 | Equal |
| 5 | 1 | 10% | 0.815 | 0.815 | 0.817 | 1.10 | 1.10 | Equal |
| 10 | 2 | 20% | 0.880 | 0.880 | 0.880 | 1.20 | 1.20 | Equal |
| 15 | 2 | 20% | 0.880 | 0.880 | 0.880 | 1.20 | 1.20 | Equal |
| 20 | 3 | 30% | 0.945 | 0.945 | 0.943 | 1.30 | 1.30 | Equal |
| 25 | 3 | 30% | 0.945 | 0.945 | 0.946 | 1.30 | 1.30 | Equal |
| 30 | 4 | 40% | 1.010 | 1.010 | 1.010 | 1.40 | 1.40 | Equal |
| 35 | 4 | 40% | 1.010 | 1.010 | 1.013 | 1.40 | 1.40 | Equal |
| 40 | 5 | 50% | 1.075 | 1.075 | 1.075 | 1.50 | 1.50 | Equal |
| 45 | 5 | 50% | 1.075 | 1.075 | 1.074 | 1.50 | 1.50 | Equal |
| 50 | 5 | 50% | 1.075 | 1.075 | 1.077 | 1.50 | 1.50 | Equal |

### Finds per board (loot-table rolls) against Quick Search: careless at least 0.85x, skilled 1.03x to 1.08x, oracle at most 1.40x

| Gathering rank | Searches | Promising spots | Quick Search | Careless | Skilled | Oracle | Careless / Quick | Skilled / Quick | Oracle / Quick | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | 6 | 2 | 0.815 | 0.748 | 0.867 | 1.078 | 0.92x | 1.06x | 1.32x | Within |
| 5 | 6 | 2 | 0.815 | 0.765 | 0.863 | 1.074 | 0.94x | 1.06x | 1.32x | Within |
| 10 | 6 | 2 | 0.880 | 0.792 | 0.919 | 1.146 | 0.90x | 1.04x | 1.30x | Within |
| 15 | 6 | 2 | 0.880 | 0.800 | 0.915 | 1.139 | 0.91x | 1.04x | 1.29x | Within |
| 20 | 6 | 2 | 0.945 | 0.846 | 0.996 | 1.232 | 0.90x | 1.05x | 1.30x | Within |
| 25 | 6 | 2 | 0.945 | 0.852 | 1.005 | 1.225 | 0.90x | 1.06x | 1.30x | Within |
| 30 | 6 | 2 | 1.010 | 0.880 | 1.061 | 1.289 | 0.87x | 1.05x | 1.28x | Within |
| 35 | 6 | 2 | 1.010 | 0.901 | 1.074 | 1.269 | 0.89x | 1.06x | 1.26x | Within |
| 40 | 6 | 2 | 1.075 | 0.942 | 1.154 | 1.362 | 0.88x | 1.07x | 1.27x | Within |
| 45 | 6 | 2 | 1.075 | 0.950 | 1.140 | 1.363 | 0.88x | 1.06x | 1.27x | Within |
| 50 | 6 | 2 | 1.075 | 0.947 | 1.145 | 1.369 | 0.88x | 1.06x | 1.27x | Within |

### How the skilled player spends its searches

| Gathering rank | Skilled Sifts | Skilled Dig Deeper attempts | Skilled spots spoiled | Skilled Dig Deeper finds | Skilled Search finds | Skilled attempts |
| --- | --- | --- | --- | --- | --- | --- |
| 0 | 1.67 | 1.81 | 0.58 | 0.248 | 0.619 | 4.33 |
| 5 | 1.67 | 1.81 | 0.58 | 0.256 | 0.607 | 4.33 |
| 10 | 1.83 | 1.96 | 0.62 | 0.270 | 0.648 | 4.17 |
| 15 | 1.84 | 1.95 | 0.63 | 0.270 | 0.645 | 4.16 |
| 20 | 1.83 | 1.96 | 0.63 | 0.264 | 0.732 | 4.17 |
| 25 | 1.83 | 1.95 | 0.62 | 0.273 | 0.732 | 4.17 |
| 30 | 1.83 | 1.96 | 0.62 | 0.264 | 0.796 | 4.17 |
| 35 | 1.84 | 1.95 | 0.62 | 0.269 | 0.805 | 4.16 |
| 40 | 1.83 | 1.96 | 0.63 | 0.268 | 0.886 | 4.17 |
| 45 | 1.84 | 1.95 | 0.61 | 0.268 | 0.873 | 4.16 |
| 50 | 1.83 | 1.96 | 0.62 | 0.268 | 0.877 | 4.17 |

### The whole Treasure Seeker line against the cap (skilled at most 1.15x)

| Gathering rank | Quick Search | Skilled, whole line | Oracle, whole line | Skilled, whole line / Quick | Oracle, whole line / Quick | Verdict |
| --- | --- | --- | --- | --- | --- | --- |
| 0 | 0.815 | 0.899 | 1.139 | 1.10x | 1.40x | Within |
| 5 | 0.815 | 0.918 | 1.136 | 1.13x | 1.39x | Within |
| 10 | 0.880 | 0.991 | 1.202 | 1.13x | 1.37x | Within |
| 15 | 0.880 | 0.978 | 1.199 | 1.11x | 1.36x | Within |
| 20 | 0.945 | 1.062 | 1.292 | 1.12x | 1.37x | Within |
| 25 | 0.945 | 1.060 | 1.272 | 1.12x | 1.35x | Within |
| 30 | 1.010 | 1.113 | 1.350 | 1.10x | 1.34x | Within |
| 35 | 1.010 | 1.112 | 1.344 | 1.10x | 1.33x | Within |
| 40 | 1.075 | 1.187 | 1.414 | 1.10x | 1.32x | Within |
| 45 | 1.075 | 1.196 | 1.408 | 1.11x | 1.31x | Within |
| 50 | 1.075 | 1.214 | 1.422 | 1.13x | 1.32x | Within |

## Length

### Actions per board by Gathering rank (target 3-8 actions (gathering node))

| Gathering rank | Searches | Careless actions | Skilled actions | Oracle actions | Verdict |
| --- | --- | --- | --- | --- | --- |
| 0 | 6 | 6.00 | 6.00 | 6.00 | Within |
| 5 | 6 | 6.00 | 6.00 | 6.00 | Within |
| 10 | 6 | 6.00 | 6.00 | 6.00 | Within |
| 15 | 6 | 6.00 | 6.00 | 6.00 | Within |
| 20 | 6 | 6.00 | 6.00 | 6.00 | Within |
| 25 | 6 | 6.00 | 6.00 | 6.00 | Within |
| 30 | 6 | 6.00 | 6.00 | 6.00 | Within |
| 35 | 6 | 6.00 | 6.00 | 6.00 | Within |
| 40 | 6 | 6.00 | 6.00 | 6.00 | Within |
| 45 | 6 | 6.00 | 6.00 | 6.00 | Within |
| 50 | 6 | 6.00 | 6.00 | 6.00 | Within |

## XP rate

### Scavenging: XP per hour against the legacy loop (2.0 s per action legacy, 6.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per site | Legacy actions per site | Legacy XP per hour | New XP per site | New actions per site | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Site level 1 | rank 10, Hard Look level 2 | 576 | 1.0 | 64800 | 1322 | 6.0 | 72104 | 1.11 | - | Passed |
| Site level 2 | rank 20, Hard Look level 3 | 620 | 1.0 | 69778 | 1349 | 6.0 | 73588 | 1.05 | - | Passed |
| Site level 3 | rank 30, Hard Look level 4 | 664 | 1.0 | 74756 | 1434 | 6.0 | 78196 | 1.05 | - | Passed |
| Site level 4 | rank 40, Hard Look level 5 | 709 | 1.0 | 79734 | 1480 | 6.0 | 80729 | 1.01 | - | Passed |
| Site level 5 | rank 50, Hard Look level 5 | 709 | 1.0 | 79734 | 1485 | 6.0 | 80984 | 1.02 | - | Passed |

## Quick Search XP rate

### Quick Search against today's search, per site level at the Gathering rank 10 times the level

| Site level | Gathering rank | Today's XP per site | Quick Search XP per site | Today's attempts | Quick Search attempts | Today's XP per hour | Quick Search XP per hour | Ratio | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 10 | 576.0 | 576.0 | 1.20 | 1.20 | 64800 | 64800 | 1.000 | Equal |
| 2 | 20 | 620.2 | 620.2 | 1.30 | 1.30 | 69778 | 69778 | 1.000 | Equal |
| 3 | 30 | 664.5 | 664.5 | 1.40 | 1.40 | 74756 | 74756 | 1.000 | Equal |
| 4 | 40 | 708.8 | 708.8 | 1.50 | 1.50 | 79734 | 79734 | 1.000 | Equal |
| 5 | 50 | 708.8 | 708.8 | 1.50 | 1.50 | 79734 | 79734 | 1.000 | Equal |

## Evidence: XP rate by player

### Gathering XP per hour of a board against today's search, by player

| Site level | Gathering rank | Today's XP per hour | Careless XP per board | Careless XP per hour | Careless / today | Skilled XP per board | Skilled XP per hour | Skilled / today | Actions per board |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 10 | 64800 | 1322 | 72104 | 1.11x | 1382 | 75361 | 1.16x | 6.0 |
| 2 | 20 | 69778 | 1349 | 73588 | 1.05x | 1420 | 77444 | 1.11x | 6.0 |
| 3 | 30 | 74756 | 1434 | 78196 | 1.05x | 1530 | 83432 | 1.12x | 6.0 |
| 4 | 40 | 79734 | 1480 | 80729 | 1.01x | 1585 | 86461 | 1.08x | 6.0 |
| 5 | 50 | 79734 | 1485 | 80984 | 1.02x | 1581 | 86239 | 1.08x | 6.0 |

## Evidence: rare supply

### Rare finds per site at Gathering rank 30 (Hard Look level 4) with Treasure Hunter 0; the skilled player has no perks: Quick Search against the board

| Loot table | Entries | Rare finds | Share of a roll that is rare | Quick Search per site | Careless per site | Skilled per site | Careless / Quick | Skilled / Quick |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| SCAVENGING_BUSH_1 | 2 | lowest weight | 9.1% | 0.092 | 0.080 | 0.133 | 0.9x | 1.5x |
| SCAVENGING_BUSH_3 | 3 | lowest weight | 6.7% | 0.067 | 0.059 | 0.100 | 0.9x | 1.5x |
| SCAVENGING_BUSH_5 | 3 | lowest weight | 6.7% | 0.067 | 0.059 | 0.100 | 0.9x | 1.5x |
| VEGETABLES_PATCH_3 | 4 | lowest weight | 7.7% | 0.078 | 0.068 | 0.114 | 0.9x | 1.5x |
| SUPPLY_CRATE_3 | 8 | 1 flagged | 0.8% | 0.008 | 0.007 | 0.012 | 0.9x | 1.6x |
| SUPPLY_CRATE_5 | 8 | 1 flagged | 0.8% | 0.008 | 0.007 | 0.012 | 0.9x | 1.6x |
| KORRIBAN_SITH_CRATE_1 | 12 | 1 flagged | 1.3% | 0.013 | 0.011 | 0.020 | 0.9x | 1.6x |
| KORRIBAN_SITH_CRATE_3 | 23 | 13 flagged | 23.0% | 0.232 | 0.203 | 0.308 | 0.9x | 1.3x |
| TATOOINE_TUSKEN_CRATE | 9 | 4 flagged | 5.6% | 0.056 | 0.049 | 0.084 | 0.9x | 1.5x |
| VISCARA_COXXIAN_CRATE | 13 | 4 flagged | 4.0% | 0.040 | 0.035 | 0.061 | 0.9x | 1.5x |
| VISCARA_MANDALORIAN_CRATE | 16 | 3 flagged | 7.8% | 0.079 | 0.069 | 0.116 | 0.9x | 1.5x |
| QIONHIVE_RESOURCES | 8 | 1 flagged | 0.8% | 0.008 | 0.007 | 0.012 | 0.9x | 1.6x |

### Rare finds per site at Gathering rank 30 (Hard Look level 4) with Treasure Hunter 3; the skilled player has no perks: Quick Search against the board

| Loot table | Entries | Rare finds | Share of a roll that is rare | Quick Search per site | Careless per site | Skilled per site | Careless / Quick | Skilled / Quick |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| SCAVENGING_BUSH_1 | 2 | lowest weight | 9.1% | 0.092 | 0.080 | 0.133 | 0.9x | 1.5x |
| SCAVENGING_BUSH_3 | 3 | lowest weight | 6.7% | 0.067 | 0.059 | 0.100 | 0.9x | 1.5x |
| SCAVENGING_BUSH_5 | 3 | lowest weight | 6.7% | 0.067 | 0.059 | 0.100 | 0.9x | 1.5x |
| VEGETABLES_PATCH_3 | 4 | lowest weight | 7.7% | 0.078 | 0.068 | 0.114 | 0.9x | 1.5x |
| SUPPLY_CRATE_3 | 8 | 1 flagged | 19.3% | 0.194 | 0.169 | 0.264 | 0.9x | 1.4x |
| SUPPLY_CRATE_5 | 8 | 1 flagged | 19.3% | 0.194 | 0.169 | 0.264 | 0.9x | 1.4x |
| KORRIBAN_SITH_CRATE_1 | 12 | 1 flagged | 28.7% | 0.290 | 0.253 | 0.373 | 0.9x | 1.3x |
| KORRIBAN_SITH_CRATE_3 | 23 | 13 flagged | 82.7% | 0.835 | 0.728 | 0.906 | 0.9x | 1.1x |
| TATOOINE_TUSKEN_CRATE | 9 | 4 flagged | 59.5% | 0.601 | 0.524 | 0.689 | 0.9x | 1.1x |
| VISCARA_COXXIAN_CRATE | 13 | 4 flagged | 56.1% | 0.567 | 0.494 | 0.656 | 0.9x | 1.2x |
| VISCARA_MANDALORIAN_CRATE | 16 | 3 flagged | 42.0% | 0.424 | 0.369 | 0.515 | 0.9x | 1.2x |
| QIONHIVE_RESOURCES | 8 | 1 flagged | 19.3% | 0.194 | 0.169 | 0.264 | 0.9x | 1.4x |

### Rare finds per site at Gathering rank 30 (Hard Look level 4) with Treasure Hunter 0; the skilled player has the whole Treasure Seeker line and Careful Hands: Quick Search against the board

| Loot table | Entries | Rare finds | Share of a roll that is rare | Quick Search per site | Careless per site | Skilled per site | Careless / Quick | Skilled / Quick |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| SCAVENGING_BUSH_1 | 2 | lowest weight | 9.1% | 0.092 | 0.080 | 0.288 | 0.9x | 3.1x |
| SCAVENGING_BUSH_3 | 3 | lowest weight | 6.7% | 0.067 | 0.059 | 0.230 | 0.9x | 3.4x |
| SCAVENGING_BUSH_5 | 3 | lowest weight | 6.7% | 0.067 | 0.059 | 0.230 | 0.9x | 3.4x |
| VEGETABLES_PATCH_3 | 4 | lowest weight | 7.7% | 0.078 | 0.068 | 0.256 | 0.9x | 3.3x |
| SUPPLY_CRATE_3 | 8 | 1 flagged | 0.8% | 0.008 | 0.007 | 0.036 | 0.9x | 4.7x |
| SUPPLY_CRATE_5 | 8 | 1 flagged | 0.8% | 0.008 | 0.007 | 0.036 | 0.9x | 4.7x |
| KORRIBAN_SITH_CRATE_1 | 12 | 1 flagged | 1.3% | 0.013 | 0.011 | 0.058 | 0.9x | 4.5x |
| KORRIBAN_SITH_CRATE_3 | 23 | 13 flagged | 23.0% | 0.232 | 0.203 | 0.521 | 0.9x | 2.2x |
| TATOOINE_TUSKEN_CRATE | 9 | 4 flagged | 5.6% | 0.056 | 0.049 | 0.201 | 0.9x | 3.6x |
| VISCARA_COXXIAN_CRATE | 13 | 4 flagged | 4.0% | 0.040 | 0.035 | 0.154 | 0.9x | 3.9x |
| VISCARA_MANDALORIAN_CRATE | 16 | 3 flagged | 7.8% | 0.079 | 0.069 | 0.259 | 0.9x | 3.3x |
| QIONHIVE_RESOURCES | 8 | 1 flagged | 0.8% | 0.008 | 0.007 | 0.036 | 0.9x | 4.7x |

## Evidence: Treasure Seeker magnitudes

### Skilled player, perks one at a time (mean over Gathering ranks 20, 35, 50)

| Purchase | Skilled finds per board | Dig Deeper finds per board | Sifts | Dig Deeper attempts | Added finds against no perks | Skilled / Quick Search |
| --- | --- | --- | --- | --- | --- | --- |
| No perks | 1.071 | 0.267 | 1.83 | 1.96 | - | 1.06x |
| Keen Eye (+1 point on every Search) | 1.114 | 0.242 | 1.56 | 1.76 | 0.043 | 1.10x |
| Steady Dig (15% less dig risk) | 1.092 | 0.291 | 1.83 | 1.96 | 0.020 | 1.08x |
| Fine Trowel I (rare entries weigh 1 more in a dig) | 1.076 | 0.271 | 1.83 | 1.96 | 0.005 | 1.07x |
| Fine Trowel II (rare entries weigh 2 more in a dig) | 1.067 | 0.264 | 1.84 | 1.95 | -0.005 | 1.06x |
| Careful Hands (style: -1 search chance, rare entries weigh 3 more in a dig) | 1.051 | 0.269 | 1.83 | 1.96 | -0.020 | 1.04x |
| Whole line below the capstone, no style | 1.129 | 0.257 | 1.55 | 1.77 | 0.058 | 1.12x |
| Whole line with Careful Hands | 1.093 | 0.284 | 1.83 | 1.96 | 0.021 | 1.08x |

## Limitations

- The skilled player is a one-step value-of-information player, not an optimal one, and real players are less skilled; the careless player marks the other end. The oracle bounds what any play could find. The study does not measure how real players play.
- Yield counts loot-table rolls, not their value: a find from Dig Deeper is one roll like any other. Sites' extra loot tables (`LOOT_TABLE_n`), which roll once for every search that finds, and Creditfinder are not modelled. The rare-supply tables show what the rolls land on for real tables.
- Gate 6 weighs the time a board adds (6.0 s an action, and 30.0 s to reach a site, both unmeasured) against the XP of its attempts, so the result is a starting value: it grows or shrinks with the real seconds. The game keeps the pace from below (each action after the first waits the action time), so a fast player earns about the modelled rate and no more; a slower one earns less.
- Respawn, node density and contention are not modelled, so the study compares one site against one site, not sites per hour.
- Perks other than the Treasure Seeker effects the Search board reads are not modelled. The client was not run.
