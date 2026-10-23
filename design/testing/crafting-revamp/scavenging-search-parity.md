# Crafting revamp: the Search board and Quick Search against today's scavenging

- Command: `SWLOR.CraftSim search --runs 12000 --seed 1 --out design/testing/crafting-revamp/scavenging-search-parity.md`
- Seed 1; 12000 runs per case.

## Inputs

- Today (what Quick Search keeps): one attempt, plus a second at `Hard Look level x 10 + Perception modifier x 5` percent. An attempt succeeds on `d20 + Perception modifier` against a DC of 6, rising by a d3 after each attempt; a success rolls the site's loot table once and pays the site's XP, a failure pays a quarter of it. Hard Look level is the Gathering rank's tier (`CraftRankMilestones`). The Perception modifier is the parity reference of a character with no investment, 0 (owner decision, October 22 2026); no code reads an attribute.
- The board: the Search engine, 12000 boards per case, each with its own dice from seed 1. A 3x3 board; the Gathering rank sets the searches (`CraftRankMilestones.SearchesForRank`, replacing Hard Look) and the promising spots (`PromisingSpotsForRank`, replacing Perception): rank 0: 5 searches, 2 promising; rank 10: 5 searches, 2 promising; rank 20: 6 searches, 2 promising; rank 30: 6 searches, 2 promising; rank 40: 6 searches, 3 promising. Searches find 8% on ordinary ground and 30% on promising ground. A Dig Deeper risks 30% of ruining the spot, 20 points more for each dig before it, and finds 20% of the time when it does not, rolling the loot table with its rare entries weighted x3. A Sift shows the four spots next to a spot. Every action costs one search.
- Players: **Careless** searches the spots in reading order and never sifts or digs. **Skilled** plays from what the window shows (it does not read the hidden layout): every spot is a chain of actions, the best split of the searches among the chains is a small knapsack, and it Sifts when the ground a Sift would show raises that value by more than the search it costs. It is a floor on skilled play, not an optimal player. **Oracle** is told the whole layout for free; no real player can reach it.
- Yield is the loot-table rolls a site gives (a find is one roll). Limits: Quick Search equals today's expected rolls exactly; careless at least 0.70x of it; skilled 0.80x to 1.00x with no perks; oracle at most 1.30x; each within 0.02 for sampling error. With the whole Treasure Seeker line the skilled player stays within the plan's +15% (1.15x), the line being sized to lift a skilled scavenger up to the cap and no further (+1 promising spot, 50% of the dig risk, rare entries weigh 2 more in a dig).
- Time: 30.0 s to reach and open each site in both loops (the progression model's estimate, unmeasured); today's attempts take 2.0 s each (the get-low animation) and a board action 6.0 s (`SearchRules.ActionSeconds`, which the game enforces as the least time between two actions, the first free). Both are estimates until the client run measures them.
- XP: every action pays Gathering XP. A Search or Dig Deeper attempt pays what the site pays against the Gathering rank (`Skill.GetDeltaXP(10 x site level - rank)`), a quarter on failure, as scavenging always did, times the multiplier by site level the game applies (`CraftMinigameXp`): level 1 1.05x, level 2 1.05x, level 3 1.1x, level 4 1.1x, level 5 1.1x. A Sift finds nothing, so it pays the quarter an attempt that finds nothing pays.
- XP cohort (gate 6): site levels 1 to 5, the Gathering rank 10 times the level, no tool. The gate judges the lower XP per hour of the careless and the skilled player.
- Named starting values of the board (`SearchRules`), all tunable:
  - `OrdinaryChance` = 8
  - `PromisingChance` = 30
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
| Economy parity | Passed | Quick Search equals today's expected rolls at every rank; a careless board finds at least 0.70x, the skilled player 0.80x to 1.00x, the oracle at most 1.30x, and the whole Treasure Seeker line stays inside the cap. |
| Length | Passed | Every board lasts 3-8 actions (gathering node), for every player, at every rank. |
| XP rate | Passed | Scavenging: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.02x in Site level 5 / rank 50, Hard Look level 5). |
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

### Finds per board (loot-table rolls) against Quick Search: careless at least 0.70x, skilled 0.80x to 1.00x, oracle at most 1.30x

| Gathering rank | Searches | Promising spots | Quick Search | Careless | Skilled | Oracle | Careless / Quick | Skilled / Quick | Oracle / Quick | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | 5 | 2 | 0.815 | 0.636 | 0.724 | 0.931 | 0.78x | 0.89x | 1.14x | Within |
| 5 | 5 | 2 | 0.815 | 0.648 | 0.717 | 0.922 | 0.80x | 0.88x | 1.13x | Within |
| 10 | 5 | 2 | 0.880 | 0.645 | 0.720 | 0.936 | 0.73x | 0.82x | 1.06x | Within |
| 15 | 5 | 2 | 0.880 | 0.647 | 0.715 | 0.931 | 0.74x | 0.81x | 1.06x | Within |
| 20 | 6 | 2 | 0.945 | 0.765 | 0.858 | 1.101 | 0.81x | 0.91x | 1.16x | Within |
| 25 | 6 | 2 | 0.945 | 0.773 | 0.867 | 1.095 | 0.82x | 0.92x | 1.16x | Within |
| 30 | 6 | 2 | 1.010 | 0.758 | 0.857 | 1.112 | 0.75x | 0.85x | 1.10x | Within |
| 35 | 6 | 2 | 1.010 | 0.780 | 0.872 | 1.087 | 0.77x | 0.86x | 1.08x | Within |
| 40 | 6 | 3 | 1.075 | 0.916 | 1.007 | 1.312 | 0.85x | 0.94x | 1.22x | Within |
| 45 | 6 | 3 | 1.075 | 0.922 | 1.016 | 1.330 | 0.86x | 0.94x | 1.24x | Within |
| 50 | 6 | 3 | 1.075 | 0.921 | 1.005 | 1.329 | 0.86x | 0.94x | 1.24x | Within |

### How the skilled player spends its searches

| Gathering rank | Skilled Sifts | Skilled Dig Deeper attempts | Skilled spots spoiled | Skilled Dig Deeper finds | Skilled Search finds | Skilled attempts |
| --- | --- | --- | --- | --- | --- | --- |
| 0 | 1.84 | 0.52 | 0.16 | 0.074 | 0.650 | 3.16 |
| 5 | 1.84 | 0.52 | 0.15 | 0.075 | 0.642 | 3.16 |
| 10 | 1.83 | 0.54 | 0.17 | 0.074 | 0.646 | 3.17 |
| 15 | 1.84 | 0.52 | 0.17 | 0.074 | 0.641 | 3.16 |
| 20 | 1.83 | 1.61 | 0.51 | 0.218 | 0.640 | 4.17 |
| 25 | 1.83 | 1.61 | 0.51 | 0.227 | 0.641 | 4.17 |
| 30 | 1.83 | 1.62 | 0.52 | 0.214 | 0.643 | 4.17 |
| 35 | 1.84 | 1.61 | 0.51 | 0.224 | 0.648 | 4.16 |
| 40 | 1.83 | 0.32 | 0.10 | 0.046 | 0.961 | 4.17 |
| 45 | 1.83 | 0.31 | 0.10 | 0.045 | 0.970 | 4.17 |
| 50 | 1.83 | 0.32 | 0.10 | 0.044 | 0.962 | 4.17 |

### The whole Treasure Seeker line against the cap (skilled at most 1.15x)

| Gathering rank | Quick Search | Skilled, whole line | Oracle, whole line | Skilled, whole line / Quick | Oracle, whole line / Quick | Verdict |
| --- | --- | --- | --- | --- | --- | --- |
| 0 | 0.815 | 0.838 | 1.177 | 1.03x | 1.44x | Within |
| 5 | 0.815 | 0.857 | 1.159 | 1.05x | 1.42x | Within |
| 10 | 0.880 | 0.847 | 1.168 | 0.96x | 1.33x | Within |
| 15 | 0.880 | 0.847 | 1.178 | 0.96x | 1.34x | Within |
| 20 | 0.945 | 1.048 | 1.409 | 1.11x | 1.49x | Within |
| 25 | 0.945 | 1.046 | 1.399 | 1.11x | 1.48x | Within |
| 30 | 1.010 | 1.060 | 1.392 | 1.05x | 1.38x | Within |
| 35 | 1.010 | 1.048 | 1.406 | 1.04x | 1.39x | Within |
| 40 | 1.075 | 1.095 | 1.467 | 1.02x | 1.36x | Within |
| 45 | 1.075 | 1.110 | 1.478 | 1.03x | 1.37x | Within |
| 50 | 1.075 | 1.091 | 1.483 | 1.01x | 1.38x | Within |

## Length

### Actions per board by Gathering rank (target 3-8 actions (gathering node))

| Gathering rank | Searches | Careless actions | Skilled actions | Oracle actions | Verdict |
| --- | --- | --- | --- | --- | --- |
| 0 | 5 | 5.00 | 5.00 | 5.00 | Within |
| 5 | 5 | 5.00 | 5.00 | 5.00 | Within |
| 10 | 5 | 5.00 | 5.00 | 5.00 | Within |
| 15 | 5 | 5.00 | 5.00 | 5.00 | Within |
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
| Site level 1 | rank 10, Hard Look level 2 | 576 | 1.2 | 64000 | 1095 | 5.0 | 65673 | 1.03 | - | Passed |
| Site level 2 | rank 20, Hard Look level 3 | 620 | 1.3 | 68494 | 1313 | 6.0 | 71627 | 1.05 | - | Passed |
| Site level 3 | rank 30, Hard Look level 4 | 664 | 1.4 | 72933 | 1374 | 6.0 | 74934 | 1.03 | - | Passed |
| Site level 4 | rank 40, Hard Look level 5 | 709 | 1.5 | 77318 | 1444 | 6.0 | 78784 | 1.02 | - | Passed |
| Site level 5 | rank 50, Hard Look level 5 | 709 | 1.5 | 77318 | 1439 | 6.0 | 78505 | 1.02 | - | Passed |

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
| 1 | 10 | 64000 | 1095 | 65673 | 1.03x | 1130 | 67795 | 1.06x | 5.0 |
| 2 | 20 | 68494 | 1313 | 71627 | 1.05x | 1354 | 73835 | 1.08x | 6.0 |
| 3 | 30 | 72933 | 1374 | 74934 | 1.03x | 1428 | 77915 | 1.07x | 6.0 |
| 4 | 40 | 77318 | 1444 | 78784 | 1.02x | 1493 | 81412 | 1.05x | 6.0 |
| 5 | 50 | 77318 | 1439 | 78505 | 1.02x | 1490 | 81266 | 1.05x | 6.0 |

## Evidence: rare supply

### Rare finds per site at Gathering rank 30 (Hard Look level 4) with Treasure Hunter 0; the skilled player has no perks: Quick Search against the board

| Loot table | Entries | Rare finds | Share of a roll that is rare | Quick Search per site | Careless per site | Skilled per site | Careless / Quick | Skilled / Quick |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| SCAVENGING_BUSH_1 | 2 | lowest weight | 9.1% | 0.092 | 0.069 | 0.108 | 0.8x | 1.2x |
| SCAVENGING_BUSH_3 | 3 | lowest weight | 6.7% | 0.067 | 0.051 | 0.081 | 0.8x | 1.2x |
| SCAVENGING_BUSH_5 | 3 | lowest weight | 6.7% | 0.067 | 0.051 | 0.081 | 0.8x | 1.2x |
| VEGETABLES_PATCH_3 | 4 | lowest weight | 7.7% | 0.078 | 0.058 | 0.092 | 0.8x | 1.2x |
| SUPPLY_CRATE_3 | 8 | 1 flagged | 0.8% | 0.008 | 0.006 | 0.010 | 0.8x | 1.3x |
| SUPPLY_CRATE_5 | 8 | 1 flagged | 0.8% | 0.008 | 0.006 | 0.010 | 0.8x | 1.3x |
| KORRIBAN_SITH_CRATE_1 | 12 | 1 flagged | 1.3% | 0.013 | 0.010 | 0.016 | 0.8x | 1.3x |
| KORRIBAN_SITH_CRATE_3 | 23 | 13 flagged | 23.0% | 0.232 | 0.174 | 0.249 | 0.8x | 1.1x |
| TATOOINE_TUSKEN_CRATE | 9 | 4 flagged | 5.6% | 0.056 | 0.042 | 0.068 | 0.8x | 1.2x |
| VISCARA_COXXIAN_CRATE | 13 | 4 flagged | 4.0% | 0.040 | 0.030 | 0.049 | 0.8x | 1.2x |
| VISCARA_MANDALORIAN_CRATE | 16 | 3 flagged | 7.8% | 0.079 | 0.059 | 0.094 | 0.8x | 1.2x |
| QIONHIVE_RESOURCES | 8 | 1 flagged | 0.8% | 0.008 | 0.006 | 0.010 | 0.8x | 1.3x |

### Rare finds per site at Gathering rank 30 (Hard Look level 4) with Treasure Hunter 3; the skilled player has no perks: Quick Search against the board

| Loot table | Entries | Rare finds | Share of a roll that is rare | Quick Search per site | Careless per site | Skilled per site | Careless / Quick | Skilled / Quick |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| SCAVENGING_BUSH_1 | 2 | lowest weight | 9.1% | 0.092 | 0.069 | 0.108 | 0.8x | 1.2x |
| SCAVENGING_BUSH_3 | 3 | lowest weight | 6.7% | 0.067 | 0.051 | 0.081 | 0.8x | 1.2x |
| SCAVENGING_BUSH_5 | 3 | lowest weight | 6.7% | 0.067 | 0.051 | 0.081 | 0.8x | 1.2x |
| VEGETABLES_PATCH_3 | 4 | lowest weight | 7.7% | 0.078 | 0.058 | 0.092 | 0.8x | 1.2x |
| SUPPLY_CRATE_3 | 8 | 1 flagged | 19.3% | 0.194 | 0.146 | 0.213 | 0.8x | 1.1x |
| SUPPLY_CRATE_5 | 8 | 1 flagged | 19.3% | 0.194 | 0.146 | 0.213 | 0.8x | 1.1x |
| KORRIBAN_SITH_CRATE_1 | 12 | 1 flagged | 28.7% | 0.290 | 0.217 | 0.302 | 0.8x | 1.0x |
| KORRIBAN_SITH_CRATE_3 | 23 | 13 flagged | 82.7% | 0.835 | 0.627 | 0.732 | 0.8x | 0.9x |
| TATOOINE_TUSKEN_CRATE | 9 | 4 flagged | 59.5% | 0.601 | 0.451 | 0.557 | 0.8x | 0.9x |
| VISCARA_COXXIAN_CRATE | 13 | 4 flagged | 56.1% | 0.567 | 0.425 | 0.531 | 0.8x | 0.9x |
| VISCARA_MANDALORIAN_CRATE | 16 | 3 flagged | 42.0% | 0.424 | 0.318 | 0.416 | 0.8x | 1.0x |
| QIONHIVE_RESOURCES | 8 | 1 flagged | 19.3% | 0.194 | 0.146 | 0.213 | 0.8x | 1.1x |

### Rare finds per site at Gathering rank 30 (Hard Look level 4) with Treasure Hunter 0; the skilled player has the whole Treasure Seeker line and Careful Hands: Quick Search against the board

| Loot table | Entries | Rare finds | Share of a roll that is rare | Quick Search per site | Careless per site | Skilled per site | Careless / Quick | Skilled / Quick |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| SCAVENGING_BUSH_1 | 2 | lowest weight | 9.1% | 0.092 | 0.069 | 0.185 | 0.8x | 2.0x |
| SCAVENGING_BUSH_3 | 3 | lowest weight | 6.7% | 0.067 | 0.051 | 0.147 | 0.8x | 2.2x |
| SCAVENGING_BUSH_5 | 3 | lowest weight | 6.7% | 0.067 | 0.051 | 0.147 | 0.8x | 2.2x |
| VEGETABLES_PATCH_3 | 4 | lowest weight | 7.7% | 0.078 | 0.058 | 0.164 | 0.8x | 2.1x |
| SUPPLY_CRATE_3 | 8 | 1 flagged | 0.8% | 0.008 | 0.006 | 0.021 | 0.8x | 2.8x |
| SUPPLY_CRATE_5 | 8 | 1 flagged | 0.8% | 0.008 | 0.006 | 0.021 | 0.8x | 2.8x |
| KORRIBAN_SITH_CRATE_1 | 12 | 1 flagged | 1.3% | 0.013 | 0.010 | 0.035 | 0.8x | 2.7x |
| KORRIBAN_SITH_CRATE_3 | 23 | 13 flagged | 23.0% | 0.232 | 0.174 | 0.350 | 0.8x | 1.5x |
| TATOOINE_TUSKEN_CRATE | 9 | 4 flagged | 5.6% | 0.056 | 0.042 | 0.127 | 0.8x | 2.3x |
| VISCARA_COXXIAN_CRATE | 13 | 4 flagged | 4.0% | 0.040 | 0.030 | 0.096 | 0.8x | 2.4x |
| VISCARA_MANDALORIAN_CRATE | 16 | 3 flagged | 7.8% | 0.079 | 0.059 | 0.166 | 0.8x | 2.1x |
| QIONHIVE_RESOURCES | 8 | 1 flagged | 0.8% | 0.008 | 0.006 | 0.021 | 0.8x | 2.8x |

## Evidence: Treasure Seeker magnitudes

### Skilled player, perks one at a time (mean over Gathering ranks 20, 35, 50)

| Purchase | Skilled finds per board | Dig Deeper finds per board | Sifts | Dig Deeper attempts | Added finds against no perks | Skilled / Quick Search |
| --- | --- | --- | --- | --- | --- | --- |
| No perks | 0.912 | 0.162 | 1.83 | 1.18 | - | 0.90x |
| Keen Eye (+1 promising spot) | 1.033 | 0.054 | 1.78 | 0.40 | 0.122 | 1.02x |
| Steady Dig (half the dig risk) | 0.960 | 0.253 | 1.74 | 1.48 | 0.048 | 0.95x |
| Fine Trowel I (rare entries weigh 1 more in a dig) | 0.913 | 0.163 | 1.83 | 1.18 | 0.001 | 0.90x |
| Fine Trowel II (rare entries weigh 2 more in a dig) | 0.911 | 0.161 | 1.83 | 1.18 | -0.001 | 0.90x |
| Careful Hands (style: -3 search chance, rare entries weigh 3 more in a dig) | 0.835 | 0.199 | 1.74 | 1.45 | -0.076 | 0.83x |
| Whole line below the capstone, no style | 1.062 | 0.155 | 1.59 | 0.92 | 0.151 | 1.05x |
| Whole line with Careful Hands | 0.954 | 0.212 | 1.66 | 1.26 | 0.042 | 0.95x |

## Limitations

- The skilled player is a one-step value-of-information player, not an optimal one, and real players are less skilled; the careless player marks the other end. The oracle bounds what any play could find. The study does not measure how real players play.
- Yield counts loot-table rolls, not their value: a find from Dig Deeper is one roll like any other. Sites' extra loot tables (`LOOT_TABLE_n`), which roll once for every search that finds, and Creditfinder are not modelled. The rare-supply tables show what the rolls land on for real tables.
- Gate 6 weighs the time a board adds (6.0 s an action, and 30.0 s to reach a site, both unmeasured) against the XP of its attempts, so the result is a starting value: it grows or shrinks with the real seconds. The game keeps the pace from below (each action after the first waits the action time), so a fast player earns about the modelled rate and no more; a slower one earns less.
- Respawn, node density and contention are not modelled, so the study compares one site against one site, not sites per hour.
- Perks other than the Treasure Seeker effects the Search board reads are not modelled. The client was not run.
