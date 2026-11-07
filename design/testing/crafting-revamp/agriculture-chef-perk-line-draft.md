# Agriculture perk line: Chef (implemented)

The line is implemented on `claude/crafting-revamp-perks-agriculture`. This file started as the draft for the owner's approval; it now records what was built, the owner's decisions of October 2026 that amended the draft, the balance call on the styles, and the balance evidence. Rules followed: `CraftingRevampPlan.md` (Line schedule, the Chef row under Lines, Agriculture cooking) and `SkillPerkStandards.md`.

- Code: `Feature/PerkDefinition/AgricultureChefPerkDefinition.cs` (nine perks, ten purchases), `KitchenPerkStats` (the one place stats become engine numbers), `KitchenStats`, `KitchenEngine`, `KitchenText`, `KitchenViewModel` and `KitchenDefinition` (the setup view's perk lines and the Seasoned Palate switch, the help page).
- IDs: PerkType 904-912, StatType 1228-1240 (inside the reserved ranges 904-923 and 1228-1247). `iprp_resperk.2da` needs no rows (no perk here restricts an item), and no TLK strings are new (perk names and descriptions are C# text). No effect icon: the owned style is always active and is not a status effect, so the reserved EffectIconType 515-518 and TLK 192760-192769 are unused.
- Bible: the Agriculture tab, rows 16-27, written by `tools/UpdateAgriculturePerkBible.py` from the perk definitions.
- Evidence: `agriculture-chef-perks.md` (`SWLOR.CraftSim chef-perks`), `kitchen-perks-gates.md` (gates 2 and 3), `kitchen-slow-cooking-gates.md` and `kitchen-flash-cooking-gates.md` (the other gates for a Chef).

## Structure

- The Chef line acts only on the Kitchen (cooking, pet food and cooking enhancements alike). It is one of Agriculture's two independent, optional lines: 10 purchases at skill ranks 5/10/15/20/25/30/35/40/45/50, priced 1/1/2/2/3/3/3/3/3/4 SP, 25 SP in all.
- **Rank 20 is the style, a choice of two.** *Slow Cooking* and *Flash Cooking* use `RequirementCannotHavePerk` against each other, and switching costs a Perk Refund Token like any perk. The owned style is always active in the Kitchen: there is no toggle in the window (owner decision).
- Rank 50 is the capstone, with no quest requirement.
- A ranked perk occupies two purchases (Seasoned Palate at 15 and 30, Reduce at 25 and 40); each rank restates the one before inside its own total, because only the purchased rank's stat bonuses apply.
- Perks grant `StatType` adjustments. The window reads them once into a `KitchenPerkStats` record when the dish starts (`KitchenStats.Perks`), so previews and results stay pure and the engine never tests a perk by name. A perk refunded or bought during a dish changes the next dish. A cook without perks plays exactly the Kitchen as tuned (the existing Kitchen tests, unchanged, prove it).
- Every action a perk improves or adds is available at or before the perk's own rank: all Kitchen actions are available from rank 0.

## The base minigame these perks build on

The Kitchen (`Service/CraftService/Minigame/Kitchen/`; every value is a starting value in `KitchenTuning` that CraftSim tuned in phase 2, `kitchen-gates.md`): a dish is a point in a Savory/Sweet/Spice cube (each 0-100) steered toward the recipe's target. Quality is 100% inside the target's inner radius and falls to 0 at the outer radius. Doneness (120) is the progress meter, Freshness the integrity meter (the dish is ruined at 0), CP is gear CP plus 0.75 per rank plus 31 from rank 25. Heat is Low, Medium or High; the announced drift is 8 on one flavor and 100% at High, 50% at Medium, 0 at Low. Simmer gives 5 Doneness at Low and 12 at Medium, Sear 36 at High for 6 Freshness; Stir (12 CP) cancels the announced drift; Cover (6 CP) halves Freshness loss for 3 turns; a pantry pinch pushes 4, a committed ingredient's portion its listed vector, each push landing 50% light or heavy (narrowed by Control); Peak Aroma (25% of turns) doubles the next Add.

## Chef (cooking)

| Rank | SP | Perk | Type | Effect | StatTypes the engine reads |
| --- | --- | --- | --- | --- | --- |
| 5 | 1 | Pantry Staples | trait | Each free pantry pinch (Salt, Sugar, Spice) pushes 5 instead of 4. | `KitchenPantryPush` +1 |
| 10 | 1 | Sharp Palate | trait | Every push lands closer to its listed strength: the base spread is 47% instead of 50%, before Control narrows it. | `KitchenPushSpreadReduction` +3 |
| 15 | 2 | Seasoned Palate I | toggle | A switch in the Kitchen's setup view. While it is on, every Add of a committed ingredient pushes half as far but still uses a whole portion. Pantry pinches are not changed. | `KitchenSeasonedPalate` (flag) |
| 20 | 2 | Slow Cooking | style (exclusive with Flash Cooking) | A Simmer at Low heat gives 60% more Doneness (8 instead of 5), and the flavor drifts 25% less at Medium heat (4 becomes 3). | `KitchenLowHeatDonenessPercent` +60, `KitchenMediumDriftReductionPercent` +25 |
| 20 | 2 | Flash Cooking | style (exclusive with Slow Cooking) | The flavor drifts 15% less at High heat (8 becomes 7). | `KitchenHighHeatDriftReductionPercent` +15 |
| 25 | 3 | Reduce I | active | A new action: takes a turn and 12 CP and pulls the flavor 15% of the way toward the target's center on every flavor. | `KitchenReducePercent` +15 |
| 30 | 3 | Seasoned Palate II | trait | While Seasoned Palate is on, every push is also 15% less random, after Control narrows it. | `KitchenSeasonedPalate` (flag), `KitchenSeasonedPalateSpreadPercent` +15 |
| 35 | 3 | Aromatic Kitchen | trait | Each turn has a 30% chance of announcing Peak Aroma instead of 25%. | `KitchenPeakAromaChancePoints` +5 |
| 40 | 3 | Reduce II | active | Reduce pulls 20% of the way and costs 11 CP. | `KitchenReducePercent` +20, `KitchenReduceCpReduction` +1 |
| 45 | 3 | Steady Wrist | trait | Stir costs 2 less CP (10 instead of 12) and, at Medium heat, also gives half of a Simmer's Doneness. There is no Simmer at High heat, so Stir adds nothing there. | `KitchenStirCpReduction` +2, `KitchenStirDonenessPercent` +50 |
| 50 | 4 | Signature Dish | capstone, active | A new action, once per dish: takes a turn and no CP and sets one flavor of your choice exactly to the target's center. It is three actions (`Signature Dish: Savory`, `Sweet`, `Spice`). | `KitchenSignatureDish` (flag) |

SP check: 1 + 1 + 2 + 2 + 3 + 3 + 3 + 3 + 3 + 4 = 25. Only one of the two rank-20 perks can be bought.

Owner decisions applied (October 2026): Seasoned Palate halves an Add's push strength and keeps whole portions (so with it on the committed ingredients reach only half of their flavor and the dish needs pantry pinches for the rest); Reduce pulls toward the target's center; Signature Dish snaps exactly to the center (even where no push could reach it in one); Reduce and Signature Dish work on every Kitchen recipe, pet food and cooking enhancements included.

Engine details: a halved push rounds each flavor half away from zero; Peak Aroma doubles the halved push; Water (a diluting ingredient) is halved the same way (25% becomes 13%); Reduce moves each flavor by the rounded share of its distance and is refused when that would change nothing; Signature Dish is refused for a flavor already on the center and once it has been used; the announced drift still happens at the end of the turn that Signature Dish or Reduce used, so a cook snaps a flavor the drift is not about to push; Stir's CP and Reduce's CP never fall below 1.

### Mechanical contract

- **Triggers:** every effect applies to the Kitchen only, on the cook's own dish, for every Kitchen recipe (food, pet food and cooking enhancements alike). None touches combat, other crafting minigames or fishing.
- **Resources and state:** Reduce spends CP and a turn; Signature Dish spends a turn and is limited to once per dish by engine state (`KitchenState.SignatureUsed`), not by a cooldown. Seasoned Palate and the style are fixed when cooking starts. Nothing carries between dishes.
- **Costs and previews:** Reduce and Signature Dish show their exact result in the action list before the click, like Add, and are refused with a reason when they cannot be used (too little CP, nothing to change, already used).
- **Rank carry-over:** Seasoned Palate II and Reduce II restate the rank I effect and add to it, so owning the later purchase never loses the earlier one (`PerkRankProgressionTests` and `AgriculturePerkLineTests`).
- **Lifecycle:** closing the window, logging out, dying or a restart ends the dish as a failure under the 65% rule, and perk values are read from the player's perks when cooking starts, so a refund or purchase during a dish changes nothing until the next dish.
- **Perks optional:** an unperked rank-50 cook reaches full quality on at least half of at-level crafts (gate 3) by cooking at Medium or High heat, stirring against the announced drift and correcting with the pantry. The line changes how and how efficiently, not whether.

### Balance call: the styles (delegated by the owner, 23 October 2026)

The drafted styles failed gate 2 (style balance). Slow Cooking as drafted (a Low heat Simmer +40%, Medium drift -25%) lost to Flash Cooking as drafted (High drift -35%, Sear 1 Freshness cheaper) on 79-88% of recipes with a mean gap of 4 to 7 quality points at every gear: the Sear saving and the stronger High drift reduction make the fast High-heat route (a Sear gives three times a Medium Simmer's Doneness) far better, while a Low heat Simmer at 7 is still much slower than a Medium one at 12. The retune (`agriculture-chef-perks.md`, "The styles' magnitudes"; a drift is a whole number, so a reduction only matters where it changes the rounded drift: High 8 becomes 7 at 15% and 6 at 20%, Medium 4 becomes 3 at 25% and stays 3 up to 37%) drops Flash Cooking's Sear saving, cuts its High drift reduction to 15% and raises Slow Cooking's Low heat Simmer to +80% (9, close to a Medium simmer's 12, with no drift at all). Tried and rejected: Slow +40% / Flash -35% with the Sear saving (Flash better on 79-88% of recipes), Slow +100% / Flash -20% (Flash better on 71-75% of recipes alone), Slow +100% / Flash -15% and Slow +120% / Flash -15% (Slow better on 79-83% of recipes with the full line at no gear), Slow +60% / Flash -15% (Slow better on 71% with the full line at no gear). The Sear saving (`KitchenSearFreshnessCostReduction`) was dropped with it, so the StatType IDs run 1228-1240 for the Chef.

## Balance evidence

The gear difficulty pass (November 2026) cut these values so a full Chef line does not replace gear. `gear-difficulty-curve.md` has the reasoning and the three-seed gear ladder.

`agriculture-chef-perks.md` (seed 1, 20 attempts per recipe, 4 recipes per band; rank 50 plays recipes of levels 41-53) and the gate reports. The adaptive player is the model of skilled play throughout. Four recipes is a small sample, so a single purchase moves several points by chance; the ladder's three-seed averages are the numbers tuning was judged on.

**Rank 50, per purchase** (quality change against the unperked cook at the same gear, no gear / mid gear / best gear):
- The whole line is worth +16 to +20 points in no gear, +13 to +15 in mid gear and +11 to +15 in best gear, and 3.3 to 6.3 fewer actions. In fully modded gear it adds under 2 points and about 3 fewer actions, so it saves time there rather than quality.
- The strongest single purchases are Signature Dish (+8.5 / +10.0 / +8.7) and the styles (Slow Cooking +12.5 / +8.8 / +8.0, Flash Cooking +6.1 / +9.9 / +10.5). Reduce (+1.7 / +8.1 / +9.7) and Steady Wrist (+5.2 / +8.5 / +10.0) follow.
- Sharp Palate (+0.2 / +3.5 / +0.6) and Aromatic Kitchen (+0.5 / +1.3 / +3.4) are small on their own.
- Seasoned Palate switched on, alone, is about even at these gear tiers (-1.7 / -1.8 / +2.9) and costs quality in fully modded gear (-8.7), because half-strength Adds need pantry pinches to finish. That is the choice the switch offers.

**Gate 2, style balance** (`kitchen-perks-gates.md`, 6 recipes per band, 40 runs): passed in all twelve cells (the styles alone, with the full line, and with the full line and Seasoned Palate on, each at no, mid, best and fully modded gear). The better-recipe shares run 33% to 67%.

**Gate 3, perks optional**: passed against the fully modded target (lowest 82% of unperked rank-50 crafts in fully modded gear reach full quality).

**Gates 4 to 7** (`kitchen-gates.md` unperked, 6 recipes per band; `kitchen-slow-cooking-gates.md` and `kitchen-flash-cooking-gates.md`, 4 recipes per band; all 40 runs):
- Length (unperked only): passed, 10-20 actions on average in every band and gear tier.
- Economy parity: passed in all three.
- XP rate: unperked passed (lowest 1.03x). Both styles miss one case at 21-30, no gear, 1 enhancement: Flash Cooking 0.99x, Slow Cooking just under 1.00x.
- Enhancement parity (fully modded gear): Slow Cooking passed (lowest 0.98) and Flash Cooking passed (lowest 0.97). Unperked misses one case: 41-45 at 3 slots, 0.894 of today's against a limit of 0.90.

**Gate 1, no fixed rotation**: open for perked Chef builds, as for the Circuit, Fabricator and perked Forge (owner, October 2026; see the plan's gate 1 note). Not retuned here; judged from player feedback. The reports for this pass leave it out, because the search takes many CPU hours (`gear-difficulty-curve.md`, Limits).

## Player descriptions

The Bible and the perk window use the text in `AgricultureChefPerkDefinition.cs` (ASCII, no internal terms). The Kitchen window's setup list shows one line for every number the cook's perks change, the Seasoned Palate switch appears only for a cook who owns it, and the help page has a Chef paragraph.
