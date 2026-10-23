# Agriculture: Chef perk line (draft)

Status: draft for owner approval. Nothing here is coded. The Angler line belongs to the fishing workstream.

The Chef line acts only on the Kitchen minigame (cooking, pet food and cooking enhancements). It is one of Agriculture's two independent, optional lines: 10 purchases at skill ranks 5/10/15/20/25/30/35/40/45/50, priced 1/1/2/2/3/3/3/3/3/4 SP, 25 SP in all. The plan fixes the anchors: the rank 20 style choice (Slow Cooking or Flash Cooking), Seasoned Palate, Reduce, and the rank 50 capstone Signature Dish (no quest requirement). **Every magnitude below is a proposal.** Phase 2 tuned the unperked Kitchen against the CraftSim gates, so these numbers are written against the tuned engine values; the perks themselves are not in the simulator yet, so no gate has measured them.

Revision after phase 2: the engine values in "Sources" are the tuned ones, Sharp Palate and Steady Wrist were re-based on them, and the owner's decisions on the open questions are applied (see "Owner decisions").

## Sources and comparators

- Anchors, schedule and price list: `Readmes/CraftingRevampPlan.md` ("Line schedule", "Lines", "Agriculture cooking: the Kitchen").
- Engine values the perks bend: `Service/CraftService/Minigame/Kitchen/KitchenTuning.cs`. After phase 2 tuning: a dish needs 120 Doneness (was 100); Simmer gives 5 at Low and 12 at Medium; Sear gives 36 for 6 Freshness; Craftsmanship adds 0.8% to each of those per point (was 1.5%); pantry push 4; push spread 50% before Control (was 40%); drift 8 at High and 4 at Medium; Cover 3 turns for 6 CP; Stir 12 CP (was 8); Peak Aroma 25%. Recipe Freshness carries 12% slack over the turns the dish needs (was 5%).
- Comparators: the rank-20 style and rank-50 capstone anchors of the other crafting lines in the same plan table (Hot Forging/Red Line, Overclock/Surge, Production Run/Master Assembler). The weapon schedule table in `SkillPerkStandards.md` does not apply to crafting lines; the plan's schedule does.
- No character-sheet stat changes: Craftsmanship and Control keep their roles (Doneness speed, narrower pushes). Perks add `StatType` adjustments that `KitchenStats` collects when the window opens the dish; the engine reads those and never checks a perk by name.

## The line

| Rank | SP | Perk | Type | Effect (proposed magnitude) |
| --- | --- | --- | --- | --- |
| 5 | 1 | Pantry Staples | Trait | Salt, Sugar and Spice push 5 instead of 4. |
| 10 | 1 | Sharp Palate | Trait | Every push lands closer to its listed strength: the light and heavy results are 5 points nearer (50% becomes 45% before Control). |
| 15 | 2 | Seasoned Palate I | Active (toggle) | Turn it on in the setup view. While on, every Add pushes half as far but still uses one whole portion, so you can creep up on the target. Pantry pinches are not changed. |
| 20 | 2 | Slow Cooking or Flash Cooking | Style | One of the two. Slow Cooking: Simmer at Low gives 40% more Doneness (5 becomes 7), and the drift at Medium is 25% weaker. Flash Cooking: the drift at High is 35% weaker (8 becomes 5) and Sear costs 1 less Freshness. |
| 25 | 3 | Reduce I | Active | Takes a turn and 12 CP. Moves the flavor 20% of the way toward the target's center on every axis. |
| 30 | 3 | Seasoned Palate II | Trait | While Seasoned Palate is on, pushes are also 25% less random (the spread is multiplied by 0.75 after Control). |
| 35 | 3 | Aromatic Kitchen | Trait | Peak Aroma is announced with a 35% chance each turn instead of 25%. |
| 40 | 3 | Reduce II | Trait | Reduce moves the flavor 35% of the way and costs 9 CP. |
| 45 | 3 | Steady Wrist | Trait | Stir costs 4 less CP (12 becomes 8) and also gives half of a Simmer's Doneness at the current heat. |
| 50 | 4 | Signature Dish | Active (once per craft) | Takes a turn and no CP. Snaps one flavor axis, your choice, exactly onto the target's center value. |

Prices total 1+1+2+2+3+3+3+3+3+4 = 25 SP.

### Style exclusivity

Slow Cooking and Flash Cooking are mutually exclusive through `PerkRequirementCannotHavePerk`, so only one can be owned. Switching costs a Perk Refund Token, like any perk. A player who owns the style picks it up in the setup view of the Kitchen window; it is part of the craft and cannot change once cooking starts. Seasoned Palate is toggled the same way, in setup, and also stays fixed for the craft.

### How each perk meets the engine

- Pantry Staples: `KitchenPantryPush` (+1) is added to the pantry push in `AddFlavor`.
- Sharp Palate: `KitchenPushSpreadReduction` (5 points) is taken off `KitchenStats.PushSpreadPercent` after the Control formula, never below 0.
- Seasoned Palate: `KitchenSeasonedPalate` (1 enables the toggle). When the craft starts with it on, `KitchenIngredientLine` halves its portion vector and keeps its portion count, which the existing stock, preview and Peak Aroma code already handle. `KitchenSeasonedPalateSpreadPercent` (25 at rank II) scales the spread.
- Slow Cooking: `KitchenLowHeatDonenessPercent` (+40) scales the Low-heat Simmer, and `KitchenMediumDriftReductionPercent` (25) scales the Medium drift.
- Flash Cooking: `KitchenHighHeatDriftReductionPercent` (35) scales the High drift, and `KitchenSearFreshnessCostReduction` (1) lowers the Sear cost.
- Reduce: `KitchenReducePercent` (20, then 35) enables the action and sets how far it pulls toward the target's center, and `KitchenReduceCpReduction` (3 at rank II) lowers its CP cost. It is one more action in `ResolveAction`, with a preview like every other action.
- Aromatic Kitchen: `KitchenPeakAromaChancePoints` (+10) raises the forecast roll's threshold.
- Steady Wrist: `KitchenStirCpReduction` (4) and `KitchenStirDonenessPercent` (50) change the Stir rule.
- Signature Dish: `KitchenSignatureDish` (1 enables the action). The engine remembers in its immutable state that it has been used.

### Proposed StatType names

Names only, in the order above. IDs are reserved when the lines are coded: the next free StatType at this writing is 1168, and the PerkType numbers are checked against master and `iprp_resperk.2da` then. Each would carry a `StatTypeAttribute` on its enum entry.

| Name | Category | Aggregation |
| --- | --- | --- |
| KitchenPantryPush | BeneficialWhenPositive | Additive |
| KitchenPushSpreadReduction | BeneficialWhenPositive | Additive |
| KitchenSeasonedPalate | BeneficialWhenPositive | Maximum |
| KitchenSeasonedPalateSpreadPercent | BeneficialWhenPositive | Maximum |
| KitchenLowHeatDonenessPercent | BeneficialWhenPositive | Additive |
| KitchenMediumDriftReductionPercent | BeneficialWhenPositive | Additive |
| KitchenHighHeatDriftReductionPercent | BeneficialWhenPositive | Additive |
| KitchenSearFreshnessCostReduction | BeneficialWhenPositive | Additive |
| KitchenReducePercent | BeneficialWhenPositive | Maximum |
| KitchenReduceCpReduction | BeneficialWhenPositive | Additive |
| KitchenPeakAromaChancePoints | BeneficialWhenPositive | Additive |
| KitchenStirCpReduction | BeneficialWhenPositive | Additive |
| KitchenStirDonenessPercent | BeneficialWhenPositive | Additive |
| KitchenSignatureDish | BeneficialWhenPositive | Maximum |

## Mechanical contract

- Triggers: every effect applies to the Kitchen only, on the cook's own dish, for every Kitchen recipe (food, pet food and cooking enhancements alike). None of them touch combat, other crafting minigames or fishing.
- Resources and state: Reduce spends CP and a turn; Signature Dish spends a turn and is limited to once per craft by engine state, not by a cooldown. Seasoned Palate and the style are fixed when cooking starts. Nothing carries between crafts.
- Costs and previews: Reduce and Signature Dish show their exact result in the action list before the click, like Add, and are refused with a reason when they cannot be used (for example Reduce with too little CP).
- Rank carry-over: Seasoned Palate II and Reduce II restate the rank I effect and add to it, so owning the later purchase never loses the earlier one.
- Lifecycle: closing the window, logging out, dying or a restart ends the craft as a failure under the 65% rule, and perk values are read from the player's perks when cooking starts, so a refund or purchase during a craft changes nothing until the next craft.
- Perks optional: an unperked rank-50 cook reaches full quality on at least half of at-level crafts (gate 3, `kitchen-gates.md`) by cooking at Medium or High heat, stirring against the announced drift and correcting with the pantry. The line changes how and how efficiently, not whether.

## Owner decisions

Applied from the owner's answers to the four open questions of the first draft:

1. Seasoned Palate halves each Add's push strength and keeps whole portions (it does not double the number of Adds). Consequence to check when it is coded: with the toggle on, the committed ingredients reach only half of their flavor, so the dish needs pantry pinches (4 each, 5 with Pantry Staples) to cover the rest. That trades reach for precision and spends a turn and Freshness on every pinch; the style, length and perks-optional gates should be run with it on before the magnitude is final.
2. Reduce pulls toward the target's center.
3. Signature Dish snaps one axis exactly onto the center value, even where the cook could not otherwise have reached it.
4. Reduce and Signature Dish work on every Kitchen recipe, pet food and cooking enhancements included.

## Balance evidence

Phase 2 measured the unperked Kitchen: `design/testing/crafting-revamp/kitchen-gates.md` has the seeds, runs, inputs and limitations. For this line, the gates that still have to be run once the perks are in the simulator are style balance (each style the better choice on at least 30% of recipes, neither more than about 5 quality points ahead; gate 2 is not applicable today because the Kitchen offers no styles), and length, perks-optional and no-single-rotation with Seasoned Palate and Reduce on, which are the perks most likely to move them. Seasoned Palate, Reduce and Signature Dish also change the quality an unperked cook can reach, so the enhancement-parity gate (gate 7) should be re-run with the full line.
