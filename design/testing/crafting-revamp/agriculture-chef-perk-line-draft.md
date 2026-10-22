# Agriculture: Chef perk line (draft)

Status: draft for owner approval. Nothing here is coded. The Angler line belongs to the fishing workstream.

The Chef line acts only on the Kitchen minigame (cooking, pet food and cooking enhancements). It is one of Agriculture's two independent, optional lines: 10 purchases at skill ranks 5/10/15/20/25/30/35/40/45/50, priced 1/1/2/2/3/3/3/3/3/4 SP, 25 SP in all. The plan fixes the anchors: the rank 20 style choice (Slow Cooking or Flash Cooking), Seasoned Palate, Reduce, and the rank 50 capstone Signature Dish (no quest requirement). Every magnitude below is a starting value for the CraftSim gates to tune.

## Sources and comparators

- Anchors, schedule and price list: `Readmes/CraftingRevampPlan.md` ("Line schedule", "Lines", "Agriculture cooking: the Kitchen").
- Engine values the perks bend: `Service/CraftService/Minigame/Kitchen/KitchenTuning.cs` (pantry push 4, push spread 40% before Control, Simmer 5/12 Doneness at Low/Medium, Sear 36 for 6 Freshness, drift 8 at High and 4 at Medium, Cover 3 turns for 6 CP, Stir 8 CP, Peak Aroma 25%).
- Comparators: the rank-20 style and rank-50 capstone anchors of the other crafting lines in the same plan table (Hot Forging/Red Line, Overclock/Surge, Production Run/Master Assembler). The weapon schedule table in `SkillPerkStandards.md` does not apply to crafting lines; the plan's schedule does.
- No character-sheet stat changes: Craftsmanship and Control keep their roles (Doneness speed, narrower pushes). Perks add `StatType` adjustments that `KitchenStats` collects when the window opens the dish; the engine reads those and never checks a perk by name.

## The line

| Rank | SP | Perk | Type | Effect (starting values) |
| --- | --- | --- | --- | --- |
| 5 | 1 | Pantry Staples | Trait | Salt, Sugar and Spice push 5 instead of 4. |
| 10 | 1 | Sharp Palate | Trait | Every push lands closer to its listed strength: the light and heavy results are 4 points nearer (40% becomes 36% before Control). |
| 15 | 2 | Seasoned Palate I | Active (toggle) | Turn it on in the setup view. While on, every Add pushes half as far and uses half a portion, so you can creep up on the target. Pantry pinches are not changed. |
| 20 | 2 | Slow Cooking or Flash Cooking | Style | One of the two. Slow Cooking: Simmer at Low gives 40% more Doneness (5 becomes 7), and the drift at Medium is 25% weaker. Flash Cooking: the drift at High is 35% weaker (8 becomes 5) and Sear costs 1 less Freshness. |
| 25 | 3 | Reduce I | Active | Takes a turn and 12 CP. Moves the flavor 20% of the way toward the target's center on every axis. |
| 30 | 3 | Seasoned Palate II | Trait | While Seasoned Palate is on, pushes are also 25% less random (the spread is multiplied by 0.75 after Control). |
| 35 | 3 | Aromatic Kitchen | Trait | Peak Aroma is announced with a 35% chance each turn instead of 25%. |
| 40 | 3 | Reduce II | Trait | Reduce moves the flavor 35% of the way and costs 9 CP. |
| 45 | 3 | Steady Wrist | Trait | Stir costs 4 less CP and also gives half of a Simmer's Doneness at the current heat. |
| 50 | 4 | Signature Dish | Active (once per craft) | Takes a turn and no CP. Snaps one flavor axis, your choice, exactly onto the target's center value. |

Prices total 1+1+2+2+3+3+3+3+3+4 = 25 SP.

### Style exclusivity

Slow Cooking and Flash Cooking are mutually exclusive through `PerkRequirementCannotHavePerk`, so only one can be owned. Switching costs a Perk Refund Token, like any perk. A player who owns the style picks it up in the setup view of the Kitchen window; it is part of the craft and cannot change once cooking starts. Seasoned Palate is toggled the same way, in setup, and also stays fixed for the craft.

### How each perk meets the engine

- Pantry Staples: `KitchenPantryPush` (+1) is added to the pantry push in `AddFlavor`.
- Sharp Palate: `KitchenPushSpreadReduction` (4 points) is taken off `KitchenStats.PushSpreadPercent` after the Control formula, never below 0.
- Seasoned Palate: `KitchenSeasonedPalate` (1 enables the toggle). When the craft starts with it on, `KitchenIngredientLine` halves its portion vector and doubles its portion count, which the existing stock, preview and Peak Aroma code already handle. `KitchenSeasonedPalateSpreadPercent` (25 at rank II) scales the spread.
- Slow Cooking: `KitchenLowHeatDonenessPercent` (+40) scales the Low-heat Simmer, and `KitchenMediumDriftReductionPercent` (25) scales the Medium drift.
- Flash Cooking: `KitchenHighHeatDriftReductionPercent` (35) scales the High drift, and `KitchenSearFreshnessCostReduction` (1) lowers the Sear cost.
- Reduce: `KitchenReducePercent` (20, then 35) enables the action and sets how far it pulls, and `KitchenReduceCpReduction` (3 at rank II) lowers its CP cost. It is one more action in `ResolveAction`, with a preview like every other action.
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

- Triggers: every effect applies to the Kitchen only, on the cook's own dish. None of them touch combat, other crafting minigames or fishing.
- Resources and state: Reduce spends CP and a turn; Signature Dish spends a turn and is limited to once per craft by engine state, not by a cooldown. Seasoned Palate and the style are fixed when cooking starts. Nothing carries between crafts.
- Costs and previews: Reduce and Signature Dish show their exact result in the action list before the click, like Add, and are refused with a reason when they cannot be used (for example Reduce with too little CP).
- Rank carry-over: Seasoned Palate II and Reduce II restate the rank I effect and add to it, so owning the later purchase never loses the earlier one.
- Lifecycle: closing the window, logging out, dying or a restart ends the craft as a failure under the 65% rule, and perk values are read from the player's perks when cooking starts, so a refund or purchase during a craft changes nothing until the next craft.
- Perks optional: an unperked rank-50 cook reaches full quality on at-level dishes by Low-heat finishing and pantry corrections. The line changes how and how efficiently. The CraftSim "perks optional" gate checks this.

## Open questions for the owner

1. Seasoned Palate halving portions doubles the number of Adds, and so the Freshness spent on them. Is that the intended cost, or should it halve only the strength and use whole portions?
2. Reduce pulls toward the target's center, which shows the cook where the target is. The target is already shown in the window, so this is safe, but say so if Reduce should instead pull toward the nearest perfect point.
3. Signature Dish snaps an axis exactly onto the center. If a recipe's center is outside what the cook could reach, the result is still valid; confirm that is wanted.
4. Pet food and cooking enhancements share these perks. Say if Reduce and Signature Dish should be limited to food.

## Balance evidence to collect before coding

CraftSim gates for the Kitchen run in phase 2. For this line: style balance (each style the better choice on at least 30% of recipes, neither more than about 5 quality points ahead), perks-optional (unperked rank 50 can still reach full quality), and length (10 to 20 actions at level). Seasoned Palate and Reduce are the actions most likely to change the length gate. The inputs, seeds and results go in a report beside the other crafting reports in this folder.
