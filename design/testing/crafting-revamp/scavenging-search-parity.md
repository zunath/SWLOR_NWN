# Crafting revamp: the Search board and Quick Search against today's scavenging

- Command: `SWLOR.CraftSim search --runs 12000 --seed 1 --out design/testing/crafting-revamp/scavenging-search-parity.md`
- Seed 1; 12000 runs per case.

## Inputs

- Today (what Quick Search keeps): one attempt, plus a second at `Hard Look level x 10 + Perception modifier x 5` percent. An attempt succeeds on `d20 + Perception modifier` against a DC of 6, rising by a d3 after each attempt; a success rolls the site's loot table once and pays the site's XP, a failure pays a quarter of it. Hard Look level is the Gathering rank's tier (`CraftRankMilestones`). The Perception modifier is the parity reference of a character with no investment, 0 (owner decision, October 22 2026); no code reads an attribute.
- The board: the Search engine, 12000 boards per case, each with its own dice from seed 1. A 3x3 board; the Gathering rank sets the searches (`CraftRankMilestones.SearchesForRank`, replacing Hard Look), the promising spots (`PromisingSpotsForRank`, replacing Perception) and the chance a Search finds on promising ground (`PromisingChanceForRank`): from rank 0: 6 searches, 2 promising spots, 29% on promising ground; from rank 10: 6 searches, 2 promising spots, 33% on promising ground; from rank 20: 6 searches, 2 promising spots, 36% on promising ground; from rank 30: 6 searches, 2 promising spots, 39% on promising ground; from rank 40: 6 searches, 2 promising spots, 42% on promising ground. Searches find 8% on ordinary ground. A Dig Deeper risks 30% of ruining the spot, 20 points more for each dig before it, and finds 20% of the time when it does not, rolling the loot table with its rare entries weighted x3. A Sift shows the four spots next to a spot. Every action costs one search.
- Players: **Careless** searches the spots in reading order and never sifts or digs. **Skilled** plays from what the window shows (it does not read the hidden layout): every spot is a chain of actions, the best split of the searches among the chains is a small knapsack, and it Sifts when the ground a Sift would show raises that value by more than the search it costs. It is a floor on skilled play, not an optimal player. **Oracle** is told the whole layout for free; no real player can reach it.
- Yield is the loot-table rolls a site gives (a find is one roll). Limits: Quick Search equals today's expected rolls exactly; careless at least 0.85x of it; skilled 1.03x to 1.08x with no perks; oracle at most 1.40x; each within 0.02 for sampling error. With the whole Treasure Seeker line the skilled player stays within the plan's +15% (1.15x), the line being sized to stay inside the cap with its value in the rare finds (+1 points on every Search, 80% of the dig risk, rare entries weigh 2 more in a dig).
- Time: 30.0 s to reach and open each site in both loops (the progression model's estimate, unmeasured); today's attempts take 2.0 s each (the get-low animation) and a board action 6.0 s (`SearchRules.ActionSeconds`, which the game enforces as the least time between two actions, the first free). Both are estimates until the client run measures them.
- XP: every action pays Gathering XP. A Search or Dig Deeper attempt pays what the site pays against the Gathering rank (`Skill.GetDeltaXP(10 x site level - rank)`), a quarter on failure, as scavenging always did, times the multiplier by site level the game applies (`CraftMinigameXp`): level 1 1.05x, level 2 1.05x, level 3 1.1x, level 4 1.1x, level 5 1.1x. A Sift finds nothing, so it pays the quarter an attempt that finds nothing pays.
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
  - `QuickSearchSecondsPerAttempt` = 2
  - `XpMultiplierLevel1` = 1.05
  - `XpMultiplierLevel2` = 1.05
  - `XpMultiplierLevel3` = 1.1
  - `XpMultiplierLevel4` = 1.1
  - `XpMultiplierLevel5` = 1.1

## Verdicts

| Gate | Verdict | Summary |
| --- | --- | --- |
| Economy parity | Passed | Quick Search equals today's expected rolls at every rank; a careless board finds at least 0.85x, the skilled player 1.03x to 1.08x, the oracle at most 1.40x, and the whole Treasure Seeker line stays inside the cap. |
| Length | Passed | Every board lasts 3-8 actions (gathering node), for every player, at every rank. |
| XP rate | Passed | Scavenging: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.02x in Site level 4 / rank 40, Hard Look level 5). |
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
| 0 | 6 | 2 | 0.815 | 0.748 | 0.865 | 1.079 | 0.92x | 1.06x | 1.32x | Within |
| 5 | 6 | 2 | 0.815 | 0.765 | 0.863 | 1.074 | 0.94x | 1.06x | 1.32x | Within |
| 10 | 6 | 2 | 0.880 | 0.805 | 0.938 | 1.165 | 0.91x | 1.07x | 1.32x | Within |
| 15 | 6 | 2 | 0.880 | 0.814 | 0.932 | 1.159 | 0.92x | 1.06x | 1.32x | Within |
| 20 | 6 | 2 | 0.945 | 0.846 | 0.990 | 1.226 | 0.90x | 1.05x | 1.30x | Within |
| 25 | 6 | 2 | 0.945 | 0.852 | 1.002 | 1.221 | 0.90x | 1.06x | 1.29x | Within |
| 30 | 6 | 2 | 1.010 | 0.880 | 1.058 | 1.293 | 0.87x | 1.05x | 1.28x | Within |
| 35 | 6 | 2 | 1.010 | 0.901 | 1.071 | 1.270 | 0.89x | 1.06x | 1.26x | Within |
| 40 | 6 | 2 | 1.075 | 0.930 | 1.137 | 1.350 | 0.87x | 1.06x | 1.26x | Within |
| 45 | 6 | 2 | 1.075 | 0.937 | 1.119 | 1.343 | 0.87x | 1.04x | 1.25x | Within |
| 50 | 6 | 2 | 1.075 | 0.933 | 1.119 | 1.347 | 0.87x | 1.04x | 1.25x | Within |

### How the skilled player spends its searches

| Gathering rank | Skilled Sifts | Skilled Dig Deeper attempts | Skilled spots spoiled | Skilled Dig Deeper finds | Skilled Search finds | Skilled attempts |
| --- | --- | --- | --- | --- | --- | --- |
| 0 | 1.84 | 1.95 | 0.62 | 0.270 | 0.595 | 4.16 |
| 5 | 1.84 | 1.95 | 0.62 | 0.277 | 0.586 | 4.16 |
| 10 | 1.83 | 1.96 | 0.62 | 0.268 | 0.671 | 4.17 |
| 15 | 1.84 | 1.95 | 0.63 | 0.270 | 0.663 | 4.16 |
| 20 | 1.83 | 1.96 | 0.63 | 0.263 | 0.727 | 4.17 |
| 25 | 1.83 | 1.95 | 0.62 | 0.273 | 0.729 | 4.17 |
| 30 | 1.83 | 1.96 | 0.63 | 0.263 | 0.795 | 4.17 |
| 35 | 1.84 | 1.95 | 0.62 | 0.272 | 0.800 | 4.16 |
| 40 | 1.83 | 1.96 | 0.63 | 0.268 | 0.869 | 4.17 |
| 45 | 1.84 | 1.95 | 0.61 | 0.267 | 0.853 | 4.16 |
| 50 | 1.83 | 1.96 | 0.62 | 0.264 | 0.855 | 4.17 |

### The whole Treasure Seeker line against the cap (skilled at most 1.15x)

| Gathering rank | Quick Search | Skilled, whole line | Oracle, whole line | Skilled, whole line / Quick | Oracle, whole line / Quick | Verdict |
| --- | --- | --- | --- | --- | --- | --- |
| 0 | 0.815 | 0.910 | 1.146 | 1.12x | 1.41x | Within |
| 5 | 0.815 | 0.921 | 1.133 | 1.13x | 1.39x | Within |
| 10 | 0.880 | 1.003 | 1.204 | 1.14x | 1.37x | Within |
| 15 | 0.880 | 0.997 | 1.232 | 1.13x | 1.40x | Within |
| 20 | 0.945 | 1.034 | 1.272 | 1.09x | 1.35x | Within |
| 25 | 0.945 | 1.057 | 1.270 | 1.12x | 1.34x | Within |
| 30 | 1.010 | 1.116 | 1.350 | 1.10x | 1.34x | Within |
| 35 | 1.010 | 1.126 | 1.344 | 1.11x | 1.33x | Within |
| 40 | 1.075 | 1.181 | 1.401 | 1.10x | 1.30x | Within |
| 45 | 1.075 | 1.185 | 1.403 | 1.10x | 1.30x | Within |
| 50 | 1.075 | 1.176 | 1.399 | 1.09x | 1.30x | Within |

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
| Site level 1 | rank 10, Hard Look level 2 | 576 | 1.2 | 64000 | 1328 | 6.0 | 72428 | 1.13 | - | Passed |
| Site level 2 | rank 20, Hard Look level 3 | 620 | 1.3 | 68494 | 1349 | 6.0 | 73588 | 1.07 | - | Passed |
| Site level 3 | rank 30, Hard Look level 4 | 664 | 1.4 | 72933 | 1434 | 6.0 | 78196 | 1.07 | - | Passed |
| Site level 4 | rank 40, Hard Look level 5 | 709 | 1.5 | 77318 | 1447 | 6.0 | 78941 | 1.02 | - | Passed |
| Site level 5 | rank 50, Hard Look level 5 | 709 | 1.5 | 77318 | 1451 | 6.0 | 79164 | 1.02 | - | Passed |

## Quick Search XP rate

### Quick Search against today's search, per site level at the Gathering rank 10 times the level

| Site level | Gathering rank | Today's XP per site | Quick Search XP per site | Today's attempts | Quick Search attempts | Today's XP per hour | Quick Search XP per hour | Ratio | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 10 | 576.0 | 576.0 | 1.20 | 1.20 | 64000 | 64000 | 1.000 | Equal |
| 2 | 20 | 620.2 | 620.2 | 1.30 | 1.30 | 68494 | 68494 | 1.000 | Equal |
| 3 | 30 | 664.5 | 664.5 | 1.40 | 1.40 | 72933 | 72933 | 1.000 | Equal |
| 4 | 40 | 708.8 | 708.8 | 1.50 | 1.50 | 77318 | 77318 | 1.000 | Equal |
| 5 | 50 | 708.8 | 708.8 | 1.50 | 1.50 | 77318 | 77318 | 1.000 | Equal |

## Evidence: XP rate by player

### Gathering XP per hour of a board against today's search, by player

| Site level | Gathering rank | Today's XP per hour | Careless XP per board | Careless XP per hour | Careless / today | Skilled XP per board | Skilled XP per hour | Skilled / today | Actions per board |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 10 | 64000 | 1328 | 72428 | 1.13x | 1391 | 75869 | 1.19x | 6.0 |
| 2 | 20 | 68494 | 1349 | 73588 | 1.07x | 1420 | 77442 | 1.13x | 6.0 |
| 3 | 30 | 72933 | 1434 | 78196 | 1.07x | 1524 | 83153 | 1.14x | 6.0 |
| 4 | 40 | 77318 | 1447 | 78941 | 1.02x | 1549 | 84508 | 1.09x | 6.0 |
| 5 | 50 | 77318 | 1451 | 79164 | 1.02x | 1544 | 84202 | 1.09x | 6.0 |

## Evidence: rare supply

### Rare finds per site at Gathering rank 30 (Hard Look level 4) with Treasure Hunter 0; the skilled player has no perks: Quick Search against the board

| Loot table | Entries | Rare finds | Share of a roll that is rare | Quick Search per site | Careless per site | Skilled per site | Careless / Quick | Skilled / Quick |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| SCAVENGING_BUSH_1 | 2 | lowest weight | 9.1% | 0.092 | 0.080 | 0.133 | 0.9x | 1.4x |
| SCAVENGING_BUSH_3 | 3 | lowest weight | 6.7% | 0.067 | 0.059 | 0.099 | 0.9x | 1.5x |
| SCAVENGING_BUSH_5 | 3 | lowest weight | 6.7% | 0.067 | 0.059 | 0.099 | 0.9x | 1.5x |
| VEGETABLES_PATCH_3 | 4 | lowest weight | 7.7% | 0.078 | 0.068 | 0.114 | 0.9x | 1.5x |
| SUPPLY_CRATE_3 | 8 | 1 flagged | 0.8% | 0.008 | 0.007 | 0.012 | 0.9x | 1.6x |
| SUPPLY_CRATE_5 | 8 | 1 flagged | 0.8% | 0.008 | 0.007 | 0.012 | 0.9x | 1.6x |
| KORRIBAN_SITH_CRATE_1 | 12 | 1 flagged | 1.3% | 0.013 | 0.011 | 0.020 | 0.9x | 1.5x |
| KORRIBAN_SITH_CRATE_3 | 23 | 13 flagged | 23.0% | 0.232 | 0.203 | 0.307 | 0.9x | 1.3x |
| TATOOINE_TUSKEN_CRATE | 9 | 4 flagged | 5.6% | 0.056 | 0.049 | 0.084 | 0.9x | 1.5x |
| VISCARA_COXXIAN_CRATE | 13 | 4 flagged | 4.0% | 0.040 | 0.035 | 0.060 | 0.9x | 1.5x |
| VISCARA_MANDALORIAN_CRATE | 16 | 3 flagged | 7.8% | 0.079 | 0.069 | 0.116 | 0.9x | 1.5x |
| QIONHIVE_RESOURCES | 8 | 1 flagged | 0.8% | 0.008 | 0.007 | 0.012 | 0.9x | 1.6x |

### Rare finds per site at Gathering rank 30 (Hard Look level 4) with Treasure Hunter 3; the skilled player has no perks: Quick Search against the board

| Loot table | Entries | Rare finds | Share of a roll that is rare | Quick Search per site | Careless per site | Skilled per site | Careless / Quick | Skilled / Quick |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| SCAVENGING_BUSH_1 | 2 | lowest weight | 9.1% | 0.092 | 0.080 | 0.133 | 0.9x | 1.4x |
| SCAVENGING_BUSH_3 | 3 | lowest weight | 6.7% | 0.067 | 0.059 | 0.099 | 0.9x | 1.5x |
| SCAVENGING_BUSH_5 | 3 | lowest weight | 6.7% | 0.067 | 0.059 | 0.099 | 0.9x | 1.5x |
| VEGETABLES_PATCH_3 | 4 | lowest weight | 7.7% | 0.078 | 0.068 | 0.114 | 0.9x | 1.5x |
| SUPPLY_CRATE_3 | 8 | 1 flagged | 19.3% | 0.194 | 0.169 | 0.263 | 0.9x | 1.4x |
| SUPPLY_CRATE_5 | 8 | 1 flagged | 19.3% | 0.194 | 0.169 | 0.263 | 0.9x | 1.4x |
| KORRIBAN_SITH_CRATE_1 | 12 | 1 flagged | 28.7% | 0.290 | 0.253 | 0.372 | 0.9x | 1.3x |
| KORRIBAN_SITH_CRATE_3 | 23 | 13 flagged | 82.7% | 0.835 | 0.728 | 0.903 | 0.9x | 1.1x |
| TATOOINE_TUSKEN_CRATE | 9 | 4 flagged | 59.5% | 0.601 | 0.524 | 0.687 | 0.9x | 1.1x |
| VISCARA_COXXIAN_CRATE | 13 | 4 flagged | 56.1% | 0.567 | 0.494 | 0.654 | 0.9x | 1.2x |
| VISCARA_MANDALORIAN_CRATE | 16 | 3 flagged | 42.0% | 0.424 | 0.369 | 0.514 | 0.9x | 1.2x |
| QIONHIVE_RESOURCES | 8 | 1 flagged | 19.3% | 0.194 | 0.169 | 0.263 | 0.9x | 1.4x |

### Rare finds per site at Gathering rank 30 (Hard Look level 4) with Treasure Hunter 0; the skilled player has the whole Treasure Seeker line and Careful Hands: Quick Search against the board

| Loot table | Entries | Rare finds | Share of a roll that is rare | Quick Search per site | Careless per site | Skilled per site | Careless / Quick | Skilled / Quick |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| SCAVENGING_BUSH_1 | 2 | lowest weight | 9.1% | 0.092 | 0.080 | 0.195 | 0.9x | 2.1x |
| SCAVENGING_BUSH_3 | 3 | lowest weight | 6.7% | 0.067 | 0.059 | 0.154 | 0.9x | 2.3x |
| SCAVENGING_BUSH_5 | 3 | lowest weight | 6.7% | 0.067 | 0.059 | 0.154 | 0.9x | 2.3x |
| VEGETABLES_PATCH_3 | 4 | lowest weight | 7.7% | 0.078 | 0.068 | 0.172 | 0.9x | 2.2x |
| SUPPLY_CRATE_3 | 8 | 1 flagged | 0.8% | 0.008 | 0.007 | 0.022 | 0.9x | 2.9x |
| SUPPLY_CRATE_5 | 8 | 1 flagged | 0.8% | 0.008 | 0.007 | 0.022 | 0.9x | 2.9x |
| KORRIBAN_SITH_CRATE_1 | 12 | 1 flagged | 1.3% | 0.013 | 0.011 | 0.036 | 0.9x | 2.8x |
| KORRIBAN_SITH_CRATE_3 | 23 | 13 flagged | 23.0% | 0.232 | 0.203 | 0.374 | 0.9x | 1.6x |
| TATOOINE_TUSKEN_CRATE | 9 | 4 flagged | 5.6% | 0.056 | 0.049 | 0.133 | 0.9x | 2.4x |
| VISCARA_COXXIAN_CRATE | 13 | 4 flagged | 4.0% | 0.040 | 0.035 | 0.100 | 0.9x | 2.5x |
| VISCARA_MANDALORIAN_CRATE | 16 | 3 flagged | 7.8% | 0.079 | 0.069 | 0.174 | 0.9x | 2.2x |
| QIONHIVE_RESOURCES | 8 | 1 flagged | 0.8% | 0.008 | 0.007 | 0.022 | 0.9x | 2.9x |

## Evidence: Treasure Seeker magnitudes

### Skilled player, perks one at a time (mean over Gathering ranks 20, 35, 50)

| Purchase | Skilled finds per board | Dig Deeper finds per board | Sifts | Dig Deeper attempts | Added finds against no perks | Skilled / Quick Search |
| --- | --- | --- | --- | --- | --- | --- |
| No perks | 1.060 | 0.266 | 1.83 | 1.96 | - | 1.05x |
| Keen Eye (+1 point on every Search) | 1.102 | 0.249 | 1.67 | 1.82 | 0.041 | 1.09x |
| Steady Dig (20% less dig risk) | 1.093 | 0.295 | 1.83 | 1.96 | 0.032 | 1.08x |
| Fine Trowel I (rare entries weigh 1 more in a dig) | 1.069 | 0.271 | 1.83 | 1.96 | 0.009 | 1.06x |
| Fine Trowel II (rare entries weigh 2 more in a dig) | 1.061 | 0.262 | 1.84 | 1.95 | 0.001 | 1.05x |
| Careful Hands (style: -3 search chance, rare entries weigh 3 more in a dig) | 1.006 | 0.269 | 1.83 | 1.96 | -0.055 | 1.00x |
| Whole line below the capstone, no style | 1.112 | 0.267 | 1.67 | 1.82 | 0.052 | 1.10x |
| Whole line with Careful Hands | 1.039 | 0.287 | 1.83 | 1.96 | -0.021 | 1.03x |

## Limitations

- The skilled player is a one-step value-of-information player, not an optimal one, and real players are less skilled; the careless player marks the other end. The oracle bounds what any play could find. The study does not measure how real players play.
- Yield counts loot-table rolls, not their value: a find from Dig Deeper is one roll like any other. Sites' extra loot tables (`LOOT_TABLE_n`), which roll once for every search that finds, and Creditfinder are not modelled. The rare-supply tables show what the rolls land on for real tables.
- Gate 6 weighs the time a board adds (6.0 s an action, and 30.0 s to reach a site, both unmeasured) against the XP of its attempts, so the result is a starting value: it grows or shrinks with the real seconds. The game keeps the pace from below (each action after the first waits the action time), so a fast player earns about the modelled rate and no more; a slower one earns less.
- Respawn, node density and contention are not modelled, so the study compares one site against one site, not sites per hour.
- Perks other than the Treasure Seeker effects the Search board reads are not modelled. The client was not run.
