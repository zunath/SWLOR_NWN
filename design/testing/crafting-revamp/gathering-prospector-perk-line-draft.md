# Gathering perk line: Prospector (draft)

Draft for the owner's approval. No perk code, no StatType IDs and no Bible rows exist yet; this file is the only artifact. Every magnitude is a starting value that CraftSim tunes once the rows are approved. Rules followed: `CraftingRevampPlan.md` (Line schedule, Lines, Gathering harvesting: Prospecting, Gathering refining) and `SkillPerkStandards.md`. The Treasure Seeker line is another workstream's draft.

Evidence: `harvesting-prospecting-parity.md` (command `SWLOR.CraftSim harvesting --runs 20000 --seed 1`) describes the unperked session these perks build on. The effect sizes below are arithmetic on that report's numbers, not simulations: the perks are not in the engine, so nothing here has been played.

## As implemented (23 October 2026)

This file is the original draft and stays as the record of it. The implementation changed some rows so that every loadout stays inside the economy bounds (a skilled session of any plan within 1.15x of today's items and gems per node, at most 1.5x of today's charges, 3-8 actions a node); the evidence is `gathering-perks.md`, and the rows live in `GatheringPerkDefinition` and `ProspectingPerkRules`.

A skilled session already claims nearly every seam its Stability allows, so it already yields 1.136x (1.145x for the best of nine plans) at its worst cell and gems at 1.12x. Efficient Strikes at -2 and -4 Stability took it to 1.151x and 1.163x, Rich Veins II as a cheaper Extract to 1.165x, Mother Lode's extra ore roll by the draft's own arithmetic to about 1.28x (not simulated), and Strip Mining's +1 unit cut the actions of a session from 3.3-3.8 to 2.5. So no Prospector perk adds an item, a gem or a roll, saves Stability, or cuts the session's actions: the line saves time and power cores, and the style trades items for time.

| Rank | SP | Perk | As implemented | The draft's row |
| --- | --- | --- | --- | --- |
| 5 | 1 | Efficient Strikes I | A Strike takes 0.5 s less and a Heavy Strike 1 s less. | Stability -2 and -4 |
| 10 | 1 | Bulk Refining I | Each power core refines 1 more item. | as drafted |
| 15 | 2 | Rich Veins I | Extract of a rich seam takes 0.5 s less. | +10 points on the extra ore roll |
| 20 | 2 | Strip Mining | A Strike takes 1.5 s less and costs 10 more Stability; a Survey and an Extract take 1 s more. | Strike +1 unit, +12 Stability |
| 25 | 3 | Efficient Strikes II | 1 s and 2 s less. | Stability -4 and -8 |
| 30 | 3 | Bulk Refining II | +2 items. | as drafted |
| 35 | 3 | Core Sample | As drafted; a Heavy Strike lists its shatter chance only while a gem pocket is hidden. | as drafted |
| 40 | 3 | Rich Veins II | Extract of a rich seam takes 1 s less. | +20 points on the extra ore roll |
| 45 | 3 | Bulk Refining III | +3 items. | as drafted |
| 50 | 4 | Mother Lode | The first Strike or Heavy Strike of each node takes half the time (once per node: the node remembers it). | an extra ore roll on a Heavy Strike of 3 or more units |

Bulk Refining: 12 items per power core at most (9 from the Gathering rank at 45, plus 3), 25% fewer power cores than the plan's batch of 9 and 33% over the old maximum; refining converts ore one for one, so it is a credit saving and no supply. The open questions above are answered by the cap: the whole line is inside it, and the extra ore roll is gone. Quick Harvest is unchanged.

## Structure

- One independent, optional line of 25 SP. Nothing in it is needed to work a node: an unperked skilled session already yields 1.12 to 1.14 times today's items per node, 1.06 to 1.11 times today's gems and at most 1.47 times today's harvester charges (a Survey costs none), and Quick Harvest and a strike-only session yield exactly today's items and gems.
- Ten purchases at Gathering ranks 5/10/15/20/25/30/35/40/45/50, priced 1/1/2/2/3/3/3/3/3/4 SP. The rank-20 purchase is the line's style. The rank-50 purchase is the capstone with no quest requirement.
- Ranked perks use up to three purchases. Each rank restates the benefit inside its own total (only the purchased rank's stat bonuses apply).
- The line acts only on Prospecting sessions and on refining, the two Gathering activities it names. It does not touch Quick Harvest, which stays exactly today's loop with no perk and no rank milestone, so it never trains Gathering faster than today.
- The style is a trade-off, so a character that owns Strip Mining here and Careful Hands in the Treasure Seeker line has two styles and one is active. The Prospecting window shows the owned styles in a row above the actions and lets the player pick one until the first action of a session; it then locks for that session. Styles are stored as a status effect, as the owner decided for the Engineering styles. Neither style has a downside that disappears outside Prospecting, because both act only there.

## How the engine reads perks

When a harvester opens a session, the lifecycle builds one immutable `ProspectingEffects` record from the player's stat totals and passes it to `ProspectingSetup.ForNode`, beside the node, the charges and the Gathering rank's milestones. Today it passes none. The engine reads only that record, never a perk, so previews and results stay pure, and a perk bought or refunded during a session changes the next session, not this one. Refining reads `ProspectorRefineBatch` in `RefineryViewModel.CalculateCoresRequired`, beside the rank milestone that already adds units per power core.

The window shows every effect in the exact previews the actions already have: a Strike's units and Stability, a rich seam's chance, the seams' types when Core Sample shows them.

## Prospector (volume)

| Rank | SP | Perk | Type | Effect (starting values) | StatTypes the engine reads |
| --- | --- | --- | --- | --- | --- |
| 5 | 1 | Efficient Strikes I | trait | Strike costs 2 less Stability and Heavy Strike 4 less. | `ProspectorStrikeStability` -2, `ProspectorHeavyStability` -4 |
| 10 | 1 | Bulk Refining I | trait | Each power core refines 1 more unit. | `ProspectorRefineBatch` +1 |
| 15 | 2 | Rich Veins I | trait | A rich seam's chance of an extra ore roll is 10 points higher (25% becomes 35%). | `ProspectorRichSeamBonus` +10 |
| 20 | 2 | Strip Mining | style | A Strike takes 1 more unit and costs 12 more Stability. | `ProspectorStrikeUnits` +1, `ProspectorStrikeStability` +12 (while active) |
| 25 | 3 | Efficient Strikes II | trait | Strike costs 4 less Stability in total and Heavy Strike 8 less (this rank replaces Efficient Strikes I). | `ProspectorStrikeStability` -4, `ProspectorHeavyStability` -8 |
| 30 | 3 | Bulk Refining II | trait | Each power core refines 2 more units in total (replaces Bulk Refining I's 1). | `ProspectorRefineBatch` +2 |
| 35 | 3 | Core Sample | trait | The window shows how many seams the node still holds and whether each is a rich seam or a gem pocket, before you find them. Surveying still locates a seam, and Extract still claims it. | `ProspectorSeamSight` +1 |
| 40 | 3 | Rich Veins II | trait | A rich seam's chance of an extra ore roll is 20 points higher in total (replaces Rich Veins I's 10). | `ProspectorRichSeamBonus` +20 |
| 45 | 3 | Bulk Refining III | trait | Each power core refines 3 more units in total (replaces Bulk Refining II's 2). | `ProspectorRefineBatch` +3 |
| 50 | 4 | Mother Lode | capstone, trait | Once per node, the first Heavy Strike that takes 3 or more units also gives 1 extra ore roll, from outside the pool. | `ProspectorMotherLodeRolls` +1 |

Decisions the line creates:

- Strip Mining against careful play. A Strike takes the loose units first and breaks into a seam only when none is left; a bigger Strike reaches the sealed units sooner, so a Strip Miner should open the seams before the last loose units go, or give them up. The style is for the player who wants the pool taken quickly: at the unperked 1.3 units a Strike, a node of 2.5 units takes about 1.9 Strikes; at 2.3 it takes about 1.1, about 4 seconds less on a node whose skilled session takes about 12. The extra 12 Stability a Strike costs is the price, and a node whose Stability is already spent on seams cannot afford it.
- Efficient Strikes against Strip Mining. Stability is the budget a thorough player runs short of on a large pool with several seams, so Efficient Strikes lets the careful player claim a seam they would otherwise skip; it also pays back part of Strip Mining's cost.
- Core Sample changes planning more than any other perk: it turns Heavy Strike's shatter chance into a known risk (the pocket is there or it is not) and tells the player how many Surveys are worth taking. It does not locate a seam or open one.

Rough size of each effect on a node of 2.5 units (arithmetic on the report's numbers):

| Purchase | Effect on the skilled session | Basis |
| --- | --- | --- |
| Efficient Strikes I / II | Under 1 point of items | Mining a node's pool costs about 38 Stability of 100 (1.9 Strikes at 20), so 2 to 4 less per Strike saves 4 to 8 Stability a node, which lets an Extract fit on the large pools where the careful player now skips one |
| Strip Mining | Same items, about 30% faster mining | Strikes per node 1.9 to 1.1 |
| Rich Veins I / II | +5 and +11 points of items | A skilled session claims about 1.3 rich seams a node (the report's 0.3 extra rolls at a 25% chance), so each point of chance is worth 0.013 items a node |
| Bulk Refining I / II / III | 17%, 22% and 25% fewer power cores for the same ore | A core refines 3 plus the rank milestone's 1 to 6 units (4 at rank 0, 5 at rank 10, 7 at rank 30, 9 at rank 45); the perk's +1, +2 and +3 make 6, 9 and 12 |
| Core Sample | No change in items; no Survey spent looking for a seam that is not there, and no shattered pockets | The node's count and types are known before the first Survey |
| Mother Lode | About +0.4 items a node, +15 points, for a player who uses Heavy Strikes | A Heavy Strike taking 3 or more units needs a pool of 3 or 4 (half of nodes) and one is rolled for 3 or 4 units 75% of the time; a player who only Strikes never sees it |

The whole line below the capstone lifts the careful player's items by about 11 points (Rich Veins II) and speeds mining; with Mother Lode the unperked +13% becomes about +39% over today's items per node, for the 4 SP capstone. No perk changes Quick Harvest.

## Magnitudes for the Treasure Seeker line's Prospecting rows

The Treasure Seeker draft (`gathering-treasure-seeker-perk-line-draft.md`, the Search workstream) names five rows that act on the Prospecting engine and leaves their numbers to this file. Starting values against the engine above:

| Treasure Seeker row | Magnitude for Prospecting | Why this size |
| --- | --- | --- |
| Sharp Chisel I / II (`TreasureSeekerPocketShatter`) | A Heavy Strike's chance to shatter an unsurveyed gem pocket is 5 points lower at rank 10 (20% becomes 15%) and 10 points lower in total at rank 30 (10%) | The shatter chance is 20% and is shown on the button. Outcome percentages are rounded to whole numbers (`ProspectingRules.WholePercentages`), so any chance works |
| Deep Scan (`TreasureSeekerSurveySeams`) | A Survey reveals 1 more seam than the rank milestone allows, never more than the 3 a node can hold | The rank milestone already reveals 2 seams from Gathering 25 and 3 from 50, so Deep Scan is worth a seam at ranks 5-24, one at ranks 25-49 and nothing at 50. The Treasure Seeker draft's "reveals 2 instead of 1" is overtaken by that milestone |
| Careful Hands, Prospecting half (style) | A Strike's chance of taking 2 units falls 15 points (30% becomes 15%), and extracting a rich seam gives a gem in place of its ore with half the chance a roll of the node's table is a gem (4.5% on a tier 1 node, 1% on a tier 5 node) | Fewer units per Strike means more Strikes and a slower session with the same items; the gem chance moves value into gems without adding items. It is a share of the table's gem chance, not a fixed 10%, so the gem supply moves in proportion to today's at every tier (about +27%). Neither adds items, so the style costs speed for gems |
| Rich Pockets (`TreasureSeekerPocketChance`) | Extracting a rich seam gives a gem in place of its ore with half the chance a roll of the node's table is a gem (the seam's other results scale to fit) | A gem pocket is chosen when a node is first worked, by nobody's perk, so the perk acts when the seam is opened, where the preview can show it. A rich seam is about 1.3 of the careful player's claims a node, so this is about 0.67 times the table's gem chance in gems a node: +27% of today's gems at every tier, where a fixed 10% would be +0.13 gems a node, 4.7 times today's tier 5 gems |
| Jackpot (`TreasureSeekerJackpot`) | The first Extract on each node has a chance equal to the node table's gem chance (9% on a tier 1 node, 2% on a tier 5 node) of adding a bonus gem from the node's gem entries, outside the pool | A bonus gem on every node would be about +0.9 items a node (+36%) and 30 times today's emeralds at tier 5. At the table's own gem chance it is about +0.9 times that chance in gems a node: +36% of today's gems at every tier, and the chance is shown in the Extract preview |

The report holds an unperked skilled session to 1.06x-1.11x of today's gems at every tier (a gem pocket is rare, a fixed fraction of the table's own gem chance). The three rows that add gems (Careful Hands, Rich Pockets and Jackpot) are sized as shares of that same chance, so each moves the supply in proportion to today's at every tier: about +27%, +27% and +36%, and about 2x today's gems for a player with all three. The owner decides how far that may go.

## Open questions

1. **Whole-line yield.** The plan caps the unperked skilled session at +15% (`HarvestingYieldGate`), and the line adds about 26 points more at rank 50 (Rich Veins II about 11, Mother Lode about 15). Perks are optional, and the plan lets them change "how, and how efficiently", so the draft treats a volume line as the place where yield may rise. If the owner wants the whole line inside the cap, Mother Lode is the one to shrink (for example to a 50% chance of its extra roll) or drop.
2. **Mother Lode's extra roll.** The plan names "one double-yield Heavy Strike per node". Doubling a Heavy Strike's 2 to 4 units would give about 3 extra items a node, +120% of a node, so the draft grants one extra ore roll instead. The owner decides whether "double" should mean more.
3. **Core Sample shows the seam count.** The plan makes the seams hidden "discovering them is the point"; Core Sample trades that for knowing the count and the types. It is a rank-35 purchase, so it never spoils a new player's first sessions.
4. **Bulk Refining and the power core economy.** Rank 45 plus the line's third rank refines 12 units per core, which is a quarter fewer power cores for the same ore. Power cores are bought for credits, so the line is a credit saving for refiners. Refining XP does not depend on the batch.
5. **Overlap with Treasure Seeker's Deep Scan.** "Survey reveals 2 seams" is now the Gathering rank milestone at rank 25 (3 at rank 50), so Deep Scan should read "Survey reveals 1 more seam than your rank allows". That row belongs to the Treasure Seeker draft.

## Proposed StatTypes

Names only; the IDs are reserved against current master and the HAK when the owner approves the rows (the next free StatType today is 1168). Each declares its classification, polarity and category on its enum entry. `ProspectorStrikeStability` and `ProspectorHeavyStability` are beneficial when negative (a style's positive value is its cost); the rest are beneficial when positive.

| Name | Aggregation | Used by |
| --- | --- | --- |
| `ProspectorStrikeStability` | additive | Efficient Strikes, Strip Mining |
| `ProspectorHeavyStability` | additive | Efficient Strikes |
| `ProspectorStrikeUnits` | additive | Strip Mining |
| `ProspectorRichSeamBonus` | additive, capped so the chance stays at or below 100 | Rich Veins |
| `ProspectorSeamSight` | additive, capped at 1 | Core Sample |
| `ProspectorMotherLodeRolls` | additive, capped at 1 | Mother Lode |
| `ProspectorRefineBatch` | additive | Bulk Refining |

The engine fields they will feed are `ProspectingEffects.StrikeStabilityChange`, `HeavyStrikeStabilityChange`, `StrikeUnitsBonus`, `RichSeamBonusPercent`, `SeesSeamTypes`, `MotherLodeRolls`; refining reads `ProspectorRefineBatch` directly. None exists yet.

## Player descriptions (draft wording)

- Efficient Strikes: "Strikes and Heavy Strikes cost less Stability."
- Bulk Refining: "Each power core refines N more units."
- Rich Veins: "Rich seams have a N% higher chance of an extra ore roll."
- Strip Mining (style): "Your Strikes take 1 more unit and cost 12 more Stability."
- Core Sample: "You can see how many seams a node holds and what each one is."
- Mother Lode: "Once per node, your first Heavy Strike of 3 or more units also gives 1 extra ore roll."

## Integration notes

- Perks act on the session's snapshot when it opens. Rank and perk changes during a session do not change it, and a session that ends early (window closed, logout, moving away, combat, death, the harvester lost, another activity such as resting taking over) loses nothing already gathered and leaves the node as it stands, as an unperked session does.
- Every perk that changes a Stability cost or a unit count shows the change in the action's preview, and the window's help page needs a paragraph for the style and for Core Sample once they exist.
- Harvester charges are spent as the report's session spends them (a Survey none, a Strike 1, a Heavy Strike 2, an Extract 2); no perk changes the charges an action costs, because the budget is the harvester's, not the player's.
- Refining's XP per unit is the ore's flat XP scaled by the rank (`GatheringLevels.RefiningXp`: flat XP times `GetDeltaXP(ore level - rank)` over `GetDeltaXP(0)`) whatever the batch, so Bulk Refining refines more units per core without paying more XP per hour of play.
- New PerkType IDs start at the next free value at the time (844 at `e77fca53`); names are the perk names with a `Gathering` prefix. Prospector is a new line, so nothing here is refunded.
