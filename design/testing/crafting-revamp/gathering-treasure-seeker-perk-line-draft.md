# Gathering perk line: Treasure Seeker (draft)

Draft for the owner's approval. No perk code, no StatType IDs and no Bible rows exist yet; this file is the only artifact. Every magnitude is a starting value that CraftSim tunes. Rules followed: `CraftingRevampPlan.md` (Line schedule, Lines, Gathering scavenging: Search) and `SkillPerkStandards.md`. The Prospector line and the harvesting engine are workstream 9a's.

Evidence: `scavenging-search-parity.md` (command `SWLOR.CraftSim search --runs 12000 --seed 1`). Every figure below is the skilled player's finds per board (loot-table rolls) averaged over Gathering ranks 20, 35 and 50, 12000 boards per case, against Quick Search's expected rolls at the same rank.

## As implemented (23 October 2026)

This file is the original draft and stays as the record of it. The implementation changed some rows so that every loadout stays inside the economy bounds (a skilled session within 1.15x of Quick Search's rolls and of today's gems per node); the evidence is `gathering-perks.md`, and the rows live in `GatheringPerkDefinition`, `SearchPerkRules` and `ProspectingPerkRules`.

The unperked skilled player's gems are already 1.06-1.12x of today's, so no Prospecting perk can add a gem: the gem rows (Careful Hands' pocket conversion, Rich Pockets, Jackpot's bonus gem) are gone, and a gem pocket is about 2% of tier 1 nodes anyway. The line's value is in the rare items a site gives instead, which no cap limits (the plan caps rolls, not what they roll): the whole line takes a skilled board to 1.12x of Quick Search's rolls (1.08x with Careful Hands) and the rare items per site, at Gathering 50, to 3.18x Quick Search's (2.18x to 3.72x by table; the unperked board gives about 1.5x), 3.59x with Careful Hands (2.21x to 4.44x by table), as `gathering-perks.md` reports.

| Rank | SP | Perk | As implemented | The draft's row |
| --- | --- | --- | --- | --- |
| 5 | 1 | Fine Trowel I | A Dig Deeper find weighs the rare entries x4 instead of x3. | as drafted |
| 10 | 1 | Sharp Chisel I | Heavy Strike 5 points less likely to shatter an unsurveyed pocket. | as drafted |
| 15 | 2 | Steady Dig | Dig Deeper risks 15% less. | as drafted |
| 20 | 2 | Careful Hands (style) | Search only: a Search finds 1 point less often and a Dig Deeper find weighs the rare entries 3 more. | also a smaller Strike and a gem in place of a rich seam's ore |
| 25 | 3 | Keen Eye | A Search finds 1 point more often. | as drafted |
| 30 | 3 | Sharp Chisel II | 10 points in total. | as drafted |
| 35 | 3 | Deep Scan | A Survey reveals 1 more seam than the rank allows (up to 3) and reveals gem pockets first (the extra seam is worth nothing at Gathering 50, where the rank already reveals 3). | seam only |
| 40 | 3 | Fine Trowel II | x5. | as drafted |
| 45 | 3 | Rich Pickings | A Search that finds something weighs the rare entries x2. | Rich Pockets: a gem in place of a rich seam's ore |
| 50 | 4 | Jackpot | The first Dig Deeper find of each board weighs the rare entries 4 more (x7 instead of x3). | the first Extract adds a bonus gem |

Careful Hands cost 3 points of Search chance in the draft; at -2 or -3 the careless and the skilled scavenger's XP per hour at the high site levels fell under today's (gate 6), so it costs 1. A Prospecting window offers only styles that do something there, and Careful Hands does nothing in Prospecting: the two styles never share a window. Quick Search is unchanged.

## Structure

- One independent, optional line of 25 SP. It is not needed to scavenge: a scavenger with no perks already searches every site, and Quick Search gives today's yield.
- Ten purchases at Gathering ranks 5/10/15/20/25/30/35/40/45/50, priced 1/1/2/2/3/3/3/3/3/4 SP. Rank 20 is the line's style, **Careful Hands**. It is one style, not a choice of two (the plan names only one): the player picks it in the window before the first action of a visit (it locks for that visit), and only one style can be active (see the Two styles note below). Rank 50 is the capstone, **Jackpot**, with no quest requirement.
- Ranked perks use two purchases. Each rank restates the benefit inside its own total (only the purchased rank's stat bonuses apply).
- The line acts on two engines. The **Search board** (this PR) reads Keen Eye, Steady Dig, Fine Trowel and the Search half of Careful Hands. The **Prospecting** node session (workstream 9a) reads Sharp Chisel, Deep Scan, Rich Pockets, the Prospecting half of Careful Hands and Jackpot. The Prospecting rows take their magnitudes from `gathering-prospector-perk-line-draft.md` ("Magnitudes for the Treasure Seeker line's Prospecting rows"), which sizes them against the harvesting engine; this file names the rows and the StatTypes.
- No perk unlocks an action: Search, Dig Deeper and Sift are open from Gathering rank 0. Each perk adjusts one number.
- No perk reads an attribute, and no perk changes Quick Search: it is today's search at the parity reference.

## How the engine reads perks

When a player opens a scavenge site, the visit builds an immutable `SearchEffects` record from the player's stat totals and passes it to `SearchSetup.ForRank` and to the visit's loot rolls, beside the Gathering rank and the site's level. Today it passes `SearchEffects.None`. The engine reads only that record, never a perk, so previews and results stay pure, and a perk bought, refunded or toggled during a visit changes the next visit, not this one. The fields that exist now and default to neutral:

| `SearchEffects` field | Neutral | What it changes |
| --- | --- | --- |
| `ExtraPromisingSpots` | 0 | Promising spots on the board (the rank gives 2). Unused by the line (one more spot is worth +0.2x of Quick Search's rolls, far past the line's budget); kept for tuning |
| `DigRiskPercent` | 100 | The share of Dig Deeper's risk of ruining a spot that is left (30%, then 50%, then 70% at the rank-0 values) |
| `SearchChanceDelta` | 0 | Points added to a Search's chance on every spot (8% ordinary, 29% to 43% promising by rank) |
| `DeeperWeightBonus` | 0 | Added to the multiplier a Dig Deeper find puts on the weight of the loot table's rare entries (x3) |
| `DigFindBonus` | 0 | Points added to a dig's chance of finding anything (20%). Unused by the line; kept for tuning |

A unit test pins what each one changes.

## What the board gives (the baseline the perks lift)

- Every scavenger has 6 searches and 2 of the board's 9 spots are promising. Gathering rank improves the board through the chance a Search finds on promising ground: 29% from rank 0, then 32%, 36%, 39% and 43% from ranks 10, 20, 30 and 40 (8% on ordinary ground at every rank). Every action costs a search; nothing else limits the board.
- Sift shows the ground at the spots above, below and beside a spot. Dig Deeper at a searched spot finds 20% of the time (when it does not ruin the spot), risking 30%, 50% and 70% on its first three digs, and rolls the loot table with its rare entries weighted x3.
- Without perks a skilled board finds 1.04x to 1.07x of Quick Search's rolls at every rank (owner rule: the board adds bounded upside over Quick Search). The line takes that to at most 1.13x, inside the plan's +15% cap, and its value is in what the finds are: the cap counts rolls, and the line's rolls are mostly rare ones (see Rare supply).

## Treasure Seeker (rare finds)

| Rank | SP | Perk | Type | Effect (starting values) | StatTypes the engine reads |
| --- | --- | --- | --- | --- | --- |
| 5 | 1 | Fine Trowel I | trait | [Search] A Dig Deeper find favours the table's rare entries more: their weight in a dig is x4 instead of x3. | `TreasureSeekerDigRareWeight` +1 |
| 10 | 1 | Sharp Chisel I | trait | [Prospecting] A Heavy Strike is 5 points less likely to shatter an unsurveyed gem pocket (20% becomes 15%). | `TreasureSeekerPocketShatter` -5 |
| 15 | 2 | Steady Dig | trait | [Search] Dig Deeper risks 15% less of ruining a spot (30%, 50%, 70% become 26%, 43%, 60%). | `TreasureSeekerDigRiskPercent` -15 |
| 20 | 2 | Careful Hands | style (chosen before the first action) | [Search] Searches find 3 points less often on every spot, and a Dig Deeper find favours the rare entries 3 more (x6, with both Fine Trowels x8). [Prospecting] A Strike's chance of taking 2 units falls 15 points (30% becomes 15%), and extracting a rich seam gives a gem in place of its ore with half the chance a roll of the node's table is a gem (4.5% on a tier 1 node, 1% on a tier 5 node). | `TreasureSeekerSearchChance` -3, `TreasureSeekerDigRareWeight` +3, plus the Prospecting stats of 9a's draft |
| 25 | 3 | Keen Eye | trait | [Search] Searches find 1 point more often on every spot (8% becomes 9% on ordinary ground, 29% becomes 30% on promising ground at rank 0). | `TreasureSeekerSearchChance` +1 |
| 30 | 3 | Sharp Chisel II | trait | [Prospecting] A Heavy Strike is 10 points less likely in total to shatter an unsurveyed gem pocket (20% becomes 10%; this rank replaces Sharp Chisel I's 5). | `TreasureSeekerPocketShatter` -10 |
| 35 | 3 | Deep Scan | trait | [Prospecting] A Survey reveals 1 more seam than your Gathering rank allows, never more than the 3 a node holds (the rank gives 2 seams from Gathering 25 and 3 from 50, so Deep Scan is worth a seam until Gathering 50 and nothing at 50). | `TreasureSeekerSurveySeams` +1 |
| 40 | 3 | Fine Trowel II | trait | [Search] A dig's rare-entry weight is x5 in total (this rank replaces Fine Trowel I's +1 with +2). | `TreasureSeekerDigRareWeight` +2 |
| 45 | 3 | Rich Pockets | trait | [Prospecting] Extracting a rich seam gives a gem in place of its ore with half the chance a roll of the node's table is a gem (4.5% on a tier 1 node, 1% on a tier 5 node; about +27% of today's gems at every tier). | `TreasureSeekerPocketChance` +10 |
| 50 | 4 | Jackpot | capstone, trait | [Prospecting] The first Extract on each node has a chance equal to the node table's gem chance (9% on a tier 1 node, 2% on a tier 5 node) of adding a bonus gem from the node's gem entries, outside the pool; the chance is shown in the Extract preview. | `TreasureSeekerJackpot` 1 (flag) |

Decisions the line creates:

- **Careful Hands on or off.** On, a Search finds less and the dig favours the rare entries much more, so a scavenger hunting the rare items plays the style and one after volume leaves it off. The style takes about 0.06 finds off a board (-6%) in exchange for a rarer dig.
- **Keen Eye and Steady Dig are small on purpose.** Together they add about 6 percent of Quick Search's rolls to a skilled board (+0.04 and +0.02 finds). The cap is on rolls, so the line spends almost none of it on the count and puts its value in Fine Trowel and Careful Hands, which change what a dig finds.
- **The Prospecting half is a separate build.** A scavenger who only searches sites gets nothing from Sharp Chisel, Deep Scan, Rich Pockets or Jackpot, and a prospector gets nothing from Keen Eye or Steady Dig. Both are in one line because the plan puts rare finds in one line, so the line is worth 25 SP only to a Gathering character who does both.
- **Two styles, one active.** A character with Careful Hands here and Strip Mining in the Prospector line owns two styles and only one is active. Each window shows the owned styles and lets the player pick one until the first action of a visit or session; it then locks for that visit. Styles are stored as a status effect, as the owner decided for the Engineering styles.

## Evidence

Skilled player, perks one at a time (from `scavenging-search-parity.md`, ranks 20, 35 and 50):

| Purchase | Finds per board | Added finds | Against Quick Search | Dig Deeper attempts | Sifts |
| --- | --- | --- | --- | --- | --- |
| No perks | 1.071 | - | 1.06x | 1.96 | 1.83 |
| Keen Eye | 1.114 | +0.043 | 1.10x | 1.76 | 1.56 |
| Steady Dig | 1.092 | +0.020 | 1.08x | 1.96 | 1.83 |
| Fine Trowel I | 1.076 | +0.005 | 1.07x | 1.96 | 1.83 |
| Fine Trowel II | 1.067 | -0.005 | 1.06x | 1.95 | 1.84 |
| Careful Hands | 1.009 | -0.063 | 1.00x | 1.96 | 1.83 |
| Whole line below the capstone, no style | 1.129 | +0.058 | 1.12x | 1.77 | 1.55 |
| Whole line with Careful Hands | 1.043 | -0.028 | 1.03x | 1.96 | 1.84 |

The whole line, with the style off, takes the skilled player from 1.04-1.07x of Quick Search (no perks) to 1.10-1.13x, never past the plan's +15% at any rank. The line adds about 0.06 finds (5%) to a board (Keen Eye +0.04, Steady Dig +0.02, Fine Trowel nothing), so its value is not in the count of finds. A scavenger told the whole layout for free (the oracle, a bound no real player reaches) reaches 1.31-1.40x with the whole line, which is why the cap is set on the skilled player and not on the oracle. Fine Trowel changes what a dig finds, not how often, so it adds no finds and costs the cap nothing.

Rare supply. A dig rolls the site's loot table with its rare entries weighted x3, so the rare items real scavenge sites give rise for an unperked skilled board (1.3-1.6x of Quick Search's per site at Gathering 30, Treasure Hunter 0, from the two dig finds a visit) and more with the line: with the whole line and Careful Hands on, the skilled player gets 2.1-2.9x the rare items per site on most tables (the supply crates, the Qion hive and the first Sith crate 2.8-2.9x, the dungeon crates 2.2-2.5x, the bushes and vegetable patches 2.1-2.3x of their lowest-weight item), and 1.6x on the one Sith crate whose rare entries are nearly a quarter of its table. The tables are in the report under "Evidence: rare supply".

## Open questions

1. **The line is small in finds on purpose.** The owner's rule is that the board adds bounded upside over Quick Search (1.04-1.07x unperked) and the cap covers perked play too (1.15x), so the line has about 9% of Quick Search's rolls to spend at most and spends 5% of them: Keen Eye and Steady Dig are +0.04 and +0.02 finds. Their value to the player is small, which is the price of a cap on rolls. The owner may price them lower, or add a purchase that spends the rest of the room (for example Keen Eye +2 points, which is +0.04 more).
2. **Rare supply.** The plan caps the rolls a site gives, not what they roll, so the cap does not bound the rare items, and the unperked board already gives 1.3-1.6x the rare items of Quick Search (two dig finds a visit). If the owner wants a cap on them, the levers are the dig's x3 base weight (`ScavengeLoot.DeeperWeightMultiplier`), Fine Trowel and Careful Hands' +3. With all at 0 the board's rare supply is a skilled board's finds times the table's ordinary rare share.
3. **Promising spots are fixed at 2.** A step in the number of spots, or in the searches, is worth more of Quick Search's rolls (+0.1 to +0.2) than a tier of Quick Search rises (0.065), so a count that steps cannot keep the unperked board at 1.03-1.08x at every rank. The rank improves the board through the chance on promising ground instead. Keen Eye used to be "one more promising spot"; it is now +1 point on every Search because a spot is worth +0.2x of Quick Search, far past the line's room.
4. **The Prospecting rows are 9a's sizing.** Their magnitudes come from 9a's draft against the harvesting engine (Heavy Strike's 20% shatter chance, a Strike's 30% chance of 2 units, the node table's gem chance). Careful Hands, Rich Pockets and Jackpot each move the gem supply by a share of today's (about +27%, +27% and +36%, about 2x for all three) and Deep Scan is worth nothing at Gathering 50; 9a's draft raises both for the owner. The Search rows do not depend on them.
5. **Help text.** The Search window's help describes the board without perks. If the owner wants the style and the perks explained in the game, the help page needs a paragraph for Careful Hands once the perks exist, and the window needs the same row of owned styles the Prospecting window gets.

## Proposed StatTypes

Names only; the IDs are reserved against current master and the HAK when the owner approves the rows (the next free StatType today is 1168). Each declares its classification, polarity and category on its enum entry. All are beneficial when positive, except `TreasureSeekerDigRiskPercent` and `TreasureSeekerPocketShatter`, whose negative values are the benefit (less risk, fewer shattered pockets), and `TreasureSeekerSearchChance`, whose negative value is the cost of Careful Hands (a style cost, not a benefit).

| Name | Aggregation | Used by | Read by |
| --- | --- | --- | --- |
| `TreasureSeekerDigRareWeight` | additive | Fine Trowel, Careful Hands | Search (`SearchEffects.DeeperWeightBonus`) |
| `TreasureSeekerDigRiskPercent` | additive, floor -99 | Steady Dig | Search (`SearchEffects.DigRiskPercent` = 100 + value) |
| `TreasureSeekerSearchChance` | additive | Keen Eye (+1), Careful Hands (-3) | Search (`SearchEffects.SearchChanceDelta`) |
| `TreasureSeekerPocketShatter` | additive | Sharp Chisel | Prospecting (9a) |
| `TreasureSeekerSurveySeams` | additive | Deep Scan | Prospecting (9a) |
| `TreasureSeekerPocketChance` | additive | Rich Pockets | Prospecting (9a) |
| `TreasureSeekerJackpot` | flag, capped at 1 | Jackpot | Prospecting (9a) |

The style is stored the way the other lines' styles are (the owner's October 22 decision: as a status effect), and the Search engine reads its numbers from the same stat totals as every other perk.

## Player descriptions (draft wording)

- Fine Trowel: "When you Dig Deeper, rare finds are N times likelier to be what you turn up."
- Sharp Chisel: "Heavy Strike is N points less likely to shatter a gem pocket you have not surveyed."
- Steady Dig: "Dig Deeper risks 15% less of ruining the spot."
- Careful Hands: "While on, Searches find 3 points less often, Dig Deeper favours rare finds much more, and Prospecting trades yield for a better chance of gem pockets."
- Keen Eye: "Searches find 1 point more often on every spot."
- Deep Scan: "Survey reveals 2 seams."
- Rich Pockets: "A node's hidden seams are 10 points likelier to be gem pockets."
- Jackpot: "The first Extract on each node adds a bonus gem."

## Integration notes

- Perks act on the visit's snapshot at the moment the site is opened. A perk bought, or a style chosen or changed, during a visit changes the next visit. A visit that ends early (window closed, moving away, combat, death, logout) keeps what it found and loses the searches not yet spent, with or without perks.
- Treasure Hunter's rare weight (+10 on rare-flagged entries per level) applies to every roll, a dig's too, before the dig's multiplier; Creditfinder raises gold finds. Neither is part of this line (they are General perks).
- The XP of an action is the site's XP against the Gathering rank times `SearchRules.XpMultiplierForLevel`, the same with or without perks. No perk changes the pace: the game keeps `SearchRules.ActionSeconds` between two actions, so no perk can speed the board.
- Quick Search is unchanged by every perk.
