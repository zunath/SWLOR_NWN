# Fabrication perk line: Builder (draft)

Draft for the owner's approval. No perk code, no StatType or PerkType IDs and no Bible rows exist yet; this file is the only artifact. Every magnitude is a proposal: the Fabricator's own numbers were tuned in phase 2 (`fabricator-gates.md`), and the perk magnitudes below are written against those tuned values but are not tuned themselves. Rules followed: `CraftingRevampPlan.md` (Line schedule, the Builder row under Lines) and `SkillPerkStandards.md`. The Researcher line belongs to the research workstream and is not drafted here.

## Structure

- Builder is one of Fabrication's two independent, optional lines, 25 SP each. Neither requires the other, and no part of it is needed to reach maximum quality (phase 2 gate: an unperked rank-50 crafter with the best gear reaches full quality on at least 50% of at-level crafts; perks change how, and how efficiently).
- Ten purchases at Fabrication ranks 5/10/15/20/25/30/35/40/45/50, priced 1/1/2/2/3/3/3/3/3/4 SP.
- **Rank 20 is the line's style, a choice of two mutually exclusive styles.** Fabrication is an activity-split skill, so the style is chosen at purchase: *Production Run* and *Fine Tolerance* use `PerkRequirementCannotHavePerk` against each other, and switching costs a Perk Refund Token like any perk. The owned style is always active in the Fabricator; there is nothing to toggle in the window. (The Researcher line's style acts on research, so the two Fabrication styles never compete for the same minigame.)
- Rank 50 is the capstone, with no quest requirement.
- A ranked perk occupies two purchases. Its second rank restates the first rank's benefit inside its own total (only the purchased rank's stat bonuses apply).
- Every effect acts only on the Fabricator (Fabrication item crafts). Perks grant StatType adjustments and the engine reads them; the engine never tests a perk by name.
- Every action a perk improves or adds is unlocked at or before the perk's own rank: Run, Precision Pass, Retool and Assemble are available from rank 0 and Service from rank 10.

## The base minigame these perks build on

The Fabricator (`Service/CraftService/Minigame/Fabricator/`; every value below is a starting value in `FabricatorRules` that CraftSim tuned in phase 2): a piece is three sub-assemblies, the Frame (Cutter head), the Shell (Extruder head) and the Fittings (Bonder head). One head is loaded at a time. Machine Condition is the integrity meter (100 for a plain piece, and more in proportion to the extra work an enhanced piece needs; the piece breaks at 0).

| Action | Unlocks at Fabrication rank | Cost | Result |
| --- | --- | --- | --- |
| Run (Frame, Shell, Fittings) | 0 | Wear: 2-9 Machine Condition by the recipe's materials | Progress on the chosen sub-assembly: 28% of the recipe's work, scaled by Craftsmanship (+1% a point) and rank, at full effect with its own head loaded and half effect with another head loaded |
| Precision Pass | 0 | Wear, as Run | The loaded head's sub-assembly gains 46 quality points (of 100), scaled by Control (+1% a point) and rank. It needs some progress on that sub-assembly first |
| Retool (Cutter, Extruder, Bonder) | 0 | CP: 1 + recipe level / 6; 1 Machine Condition | Loads the head |
| Service | 10 | CP: 2 + recipe level / 3 | Restores 40 Machine Condition |
| Assemble | 0 | none | Finishes the item once all three sub-assemblies are complete. The quality is the average of the three, weighted by the recipe's quality shares, and no higher than the weakest sub-assembly plus 20 |

**Prime Feedstock** (telegraphed): each action with none in play, each head that still has work has a 12% chance to be named. The named head's next action gets +50%, and the prime waits for it for 2 actions, so a Retool to it in time catches it. It only ever helps.

CP is gear CP plus 0.75 per rank, plus 31 from rank 25. Fabrication XP per craft is multiplied by a constant per recipe-level band, 2.4 / 1.45 / 1.25 / 1.1 / 1.0 for levels 1-10 / 11-20 / 21-30 / 31-40 / 41 and up (`FabricatorRules.XpMultiplierLevels*`, see `fabricator-gates.md`).

## How the engine reads perks

At craft start the window builds an immutable `FabricatorPerkStats` record from the crafter's stat totals and passes it to `FabricatorSetup`, beside rank and gear. The engine reads only that record, so previews and results stay pure. Each StatType below is a field of it. A style's stats count while its perk is owned.

## Builder (production and service)

The line rewards a lean production floor: cheaper retooling and servicing, a machine that wears slower, a second feed that builds two sub-assemblies at once, a longer-lived Prime Feedstock, and a capstone that lets a lopsided piece still assemble well.

| Rank | SP | Perk | Type | Effect (starting values) | StatTypes the engine reads |
| --- | --- | --- | --- | --- | --- |
| 5 | 1 | Quick Change I | trait | Retool costs 25% less CP, to a minimum of 1. | `FabricatorRetoolCpReductionPercent` +25 |
| 10 | 1 | Hardened Heads | trait | Run and Precision Pass wear the machine 1 less Machine Condition, to a minimum of 1. Retool's wear is left alone. | `FabricatorWearReduction` +1 |
| 15 | 2 | Field Service I | trait | Service restores 10 more Machine Condition and costs 2 less CP. | `FabricatorServiceRestore` +10, `FabricatorServiceCpReduction` +2 |
| 20 | 2 | Production Run | style (exclusive with Fine Tolerance) | Run is 25% stronger and Precision Pass 25% weaker. | `FabricatorRunPowerPercent` +25, `FabricatorPrecisionPowerPercent` -25 |
| 20 | 2 | Fine Tolerance | style (exclusive with Production Run) | Precision Pass is 25% stronger and Run 25% weaker. | `FabricatorPrecisionPowerPercent` +25, `FabricatorRunPowerPercent` -25 |
| 25 | 3 | Dual Feed | active | Two new actions, Dual Feed (the two other sub-assemblies): one Run that advances the loaded head's sub-assembly and one other at 60% of a full Run each (120% in all), for the CP of a Retool. It costs one Run's wear, so it saves an action and a point of wear against two Runs, and the loaded head does not change. | `FabricatorDualFeedUnlocked` (flag) |
| 30 | 3 | Feedstock Reserve | trait | Prime Feedstock waits 1 more action (2 becomes 3). | `FabricatorPrimeWindowActions` +1 |
| 35 | 3 | Quick Change II | trait | Retool costs 50% less CP in total, to a minimum of 1 (this rank replaces Quick Change I's 25%). | `FabricatorRetoolCpReductionPercent` +50 |
| 40 | 3 | Field Service II | trait | Service restores 20 more Machine Condition and costs 4 less CP in total (this rank replaces Field Service I's +10 and 2). | `FabricatorServiceRestore` +20, `FabricatorServiceCpReduction` +4 |
| 45 | 3 | Feedstock Contract | trait | Prime Feedstock gives +75% instead of +50%. | `FabricatorPrimeBonusPercent` +25 |
| 50 | 4 | Master Assembler | capstone, trait | The Assemble cap rises from the weakest sub-assembly + 20 to the weakest + 35. | `FabricatorAssembleCapPoints` +15 |

The two rank-20 rows are alternatives for one purchase, so the line is ten purchases and 25 SP (1+1+2+2+3+3+3+3+3+4).

Decisions the line creates:

- **Production Run against Fine Tolerance.** Production Run builds faster and refines slower: at the starting values a full Run is 35% of the recipe's work, so a third-share sub-assembly finishes in one Run, while a Precision Pass of about 35 quality points needs three passes a head. Fine Tolerance is the mirror: two Precision Passes reach full quality at every gear, and a Run of about 21% needs two Runs a head. Which is better depends on how much of a recipe is work (large Frame beds and flooring) and how much is quality (terminals and tools); CraftSim's style-balance gate must show each style better on at least 30% of recipes.
- **Dual Feed against retooling.** A Dual Feed builds two sub-assemblies at once without a retool, but each only at 60%, so it pays when two sub-assemblies both need work and the CP is there, and it loses to a plain Run when one is nearly done.
- **Feedstock Reserve and Feedstock Contract.** A longer wait makes it easier to retool to the primed head in time; a stronger prime makes catching it worth a retool. Together they reward planning the head order around the announcement.
- **Master Assembler** lets a crafter with a lopsided piece (one weak sub-assembly) still assemble at a good quality, which is the capstone's trade for the time it saves on the weakest sub-assembly.

## Proposed StatTypes

Names only; the IDs are reserved against current master and the HAK when the owner approves the rows (the next free StatType today is 1168). Each declares its classification, polarity and category on its enum entry. All are beneficial when positive, except the negative value a style gives to its weaker action, which is a style cost (a trade-off, not beneficial).

| Name | Aggregation | Used by |
| --- | --- | --- |
| `FabricatorRunPowerPercent` | additive | Production Run, Fine Tolerance |
| `FabricatorPrecisionPowerPercent` | additive | Production Run, Fine Tolerance |
| `FabricatorWearReduction` | additive | Hardened Heads |
| `FabricatorRetoolCpReductionPercent` | additive | Quick Change |
| `FabricatorServiceRestore` | additive | Field Service |
| `FabricatorServiceCpReduction` | additive | Field Service |
| `FabricatorDualFeedUnlocked` | maximum | Dual Feed |
| `FabricatorPrimeWindowActions` | additive | Feedstock Reserve |
| `FabricatorPrimeBonusPercent` | additive | Feedstock Contract |
| `FabricatorAssembleCapPoints` | additive | Master Assembler |

## Engine changes the perks need (phase 3, after approval)

- A `FabricatorPerkStats` record on `FabricatorSetup`, read by every rule in `FabricatorRules` and `FabricatorSetup` that a stat above changes (the Run and Precision gains, the wear, the Retool and Service costs, the Prime window and bonus, and the Assemble cap).
- Two new action ids (`dual_feed_<sub-assembly>` for each sub-assembly the loaded head does not serve) listed only when the stat is above 0, with the same exact previews as the base actions. No new state: Dual Feed changes only sub-assembly progress, Machine Condition and CP.
- Unit tests for each stat at each rank, for style exclusivity and switching (a refund token), for Dual Feed's previews, and for the order in which cost reductions and minimums apply.
- CraftSim: a `StylePairs` entry for Production Run and Fine Tolerance, and a perks-on run of the perks-optional gate.

## Balance evidence needed before coding

- CraftSim (phase 3): the style-balance gate for Production Run against Fine Tolerance (each style the better choice on at least 30% of recipes, neither ahead by more than about 5 quality points on average), action counts with each ranked perk, and the perks-optional gate with and without the line.
- Comparators for the SP prices: the plan's schedule (1/1/2/2/3/3/3/3/3/4) and the Smithery and Engineering drafts; no combat comparator applies.
- Stacking check: Quick Change II (Retool at half CP) with Dual Feed (costing a Retool's CP) makes Dual Feed cheaper as well; CraftSim must confirm that a fully perked Builder does not make CP irrelevant.

## Open questions for the owner

1. The style is owned and exclusive (plan: `PerkRequirementCannotHavePerk`, refund token to switch) and always active, with no window toggle. Is that the intended reading of "toggled in the craft window" for an activity-split line?
2. Dual Feed is two actions (one per other sub-assembly) so the preview is exact. Is a target chosen in the window preferred instead?
3. Are the capstone's +15 (cap weakest + 35 in all) and the per-band XP multipliers acceptable as the starting values the plan asks the simulator to tune?
