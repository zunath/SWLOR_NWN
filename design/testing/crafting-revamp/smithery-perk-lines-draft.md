# Smithery perk lines: draft for owner approval

Draft only. No perk is coded. The owner approves these rows before any perk code, PerkType, StatType, icon or TLK entry is created.

Source: `Readmes/CraftingRevampPlan.md` ("Smithery: the Plasma Forge", "Line schedule", the Forgemaster and Artisan rows under "Lines"). Rules: `Readmes/SkillPerkStandards.md`. The engine these perks act on is `Service/CraftService/Minigame/Forge/` (`ForgeEngine`, `ForgeRules`).

## Structure

- Smithery has two independent, optional lines, 25 SP each, 50 SP in all: **Forgemaster** (fast, hot work) and **Artisan** (precise finishing).
- Each line has 10 purchases at Smithery ranks 5/10/15/20/25/30/35/40/45/50, priced 1/1/2/2/3/3/3/3/3/4 SP.
- Rank 20 is the line's **style**, the crafting equivalent of a stance. A character can own both styles, but only one is active, and it is toggled on the Plasma Forge setup page before the craft is committed. It cannot change once the craft starts. Smithery's two styles are not alternatives inside one line, so `PerkRequirementCannotHavePerk` is not used between them.
- Rank 50 is the capstone, with no quest requirement.
- Every perk acts only on the Plasma Forge. Nothing here changes a recipe's items, stats or components, and nothing here is needed to reach maximum quality: an unperked rank-50 crafter with good gear can still reach 100% on at-level recipes, and perks change how, and how efficiently.
- Smithery has no registered perks today, so nothing is refunded.
- A higher rank of a perk restates the earlier rank's benefit, because only the purchased rank's stat bonuses apply.

## How perks reach the engine

- Each perk grants `StatType` adjustments. When a craft starts, the window reads the character's totals for the Forge stats listed below and builds one immutable `ForgePerkStats` record next to `ForgeSetup`. The engine reads only that record; it never checks a perk by name.
- A style's stats count only while that style is the active one, as a stance's stats do.
- Per-craft counters (Hammer Rhythm's streak, Red Line's charge, Temper Hold's remaining actions, Overdrive Press's last-action flag) live in `ForgeState`, so previews and results keep sharing one rules path.
- New actions (Overdrive Press, Surge Plasma, Micro-Hone, Temper Hold, Red Line Press) are added to the engine's action list only when the matching stat is above 0, so the window lists them with the same exact previews as the base actions.
- Each new `StatType` declares its category on its enum entry: `BeneficialWhenPositive` for every stat here (a style's negative value is its trade-off), with additive aggregation. New stats start at ID 1168. The names below are proposals; final IDs are assigned against current master and the HAK when the owner approves the rows.
- Proposed PerkType names are the perk names below with a `Smithery` prefix (for example `SmitheryHeatRetention`). The next free PerkType at `e77fca53` is 844; the final IDs follow the same rule as the StatTypes.

## The base minigame these perks build on

The Plasma Forge (`ForgeEngine`, starting values for the simulator to tune): Thermal 0-100 with the recipe's working range marked, Shape (progress), Finish (quality), Integrity 100 and CP. Thermal falls by the recipe's cooling rate after every action, and the only chance is the telegraphed Stable Containment (35% to be announced for the next action, +50% to that action's Hydro-Press or Vibro-Hone). Gear: Craftsmanship adds 1% to Shape gains per point and Control 1% to Finish gains per point; CP is gear CP plus 0.75 per rank, plus 31 from rank 25.

| Action | Unlocks at Smithery rank | Cost | Result |
| --- | --- | --- | --- |
| Hydro-Press | 0 | 4-5 Integrity in range; more outside | Shape 130 x (0.7 at the cool edge rising to 1.2 at the hot edge; 1.2 up to 1.6 above the range; 0.25 below) |
| Vibro-Hone | 5 | 4 Integrity | Finish 105 x (1.0 in the cool half, falling to 0.6 at the hot edge; 0.35 outside) |
| Ramp Plasma | 10 | 8 CP, 2 Integrity | Thermal +25 |
| Rebond | 15 | 10 CP, 12 Thermal | Integrity +25 |
| Cryo-Quench | 20 | 4 Integrity | Once per craft, at 80% Shape or more: Finish +300 at the center of the range, 150 at an edge, 45 outside it. Then Ramp Plasma and Hydro-Press are disabled and Vibro-Hone gives a flat 63 Finish and 55 Shape |

Shape and Finish are 1000 at an even split. A recipe's Shape share (35-65%) scales the Shape meter up and the Finish meter down, so armor needs more Shape and weapons more Finish. Leather and synthweave goods are heat-sealed: a low, wide range that cools slowly.

## Shared Forge stats

Both lines use these. Style stats are granted by the active style only.

| Proposed StatType | Meaning | Read by |
| --- | --- | --- |
| `ForgeRangeShift` | Moves the working range up (positive) or down (negative) by this many Thermal points, kept inside 0-100. | `ForgeSetup.RangeLow` and `RangeHigh` |
| `ForgePressOutputPercent` | Percent change to every Hydro-Press Shape gain, including the post-quench trim. | Hydro-Press, set-piece Vibro-Hone |
| `ForgeHoneOutputPercent` | Percent change to every Vibro-Hone Finish gain. | Vibro-Hone |

## Forgemaster: fast, hot work

The Forgemaster shapes quickly while the piece is hot, spends CP on heat and trades Integrity for tempo. It pairs with Hot Forging, whose higher range suits Hydro-Press, and with armor, which is mostly Shape.

| Rank | SP | Perk | Type | Player-facing effect (starting values) | StatType(s) the engine reads |
| --- | --- | --- | --- | --- | --- |
| 5 | 1 | Plasma Efficiency | trait | Ramp Plasma costs 1 less CP (8 becomes 7). | `ForgeRampCpReduction` +1 |
| 10 | 1 | Heat Retention I | trait | The piece loses 1 less Thermal after every action, never less than 1. | `ForgeCoolingReduction` +1 |
| 15 | 2 | Overdrive Press | active | A new action. A large Shape gain: 175% of a Hydro-Press at the current Thermal, using the same range rules. Costs 12 CP and twice the press's Integrity. Cannot be used twice in a row. | `ForgeOverdrivePressPercent` 75 (the action is available when above 0) |
| 20 | 2 | Hot Forging | style | The working range moves up 10 Thermal. Hydro-Press gains +25% Shape and Vibro-Hone gains -25% Finish. Only one style is active at a time. | `ForgeRangeShift` +10, `ForgePressOutputPercent` +25, `ForgeHoneOutputPercent` -25 (while active) |
| 25 | 3 | Hammer Rhythm | trait | A Hydro-Press right after another Hydro-Press gains +8% Shape, stacking up to +24%. Any other action resets it. | `ForgePressStreakPercent` 8, `ForgePressStreakMax` 3 |
| 30 | 3 | Heat Retention II | trait | The piece loses 2 less Thermal after every action, never less than 1. Replaces Heat Retention I. | `ForgeCoolingReduction` +2 |
| 35 | 3 | Scorch Guard | trait | A Hydro-Press above the working range cracks 25% less Integrity. | `ForgePressOverheatIntegrityReductionPercent` 25 |
| 40 | 3 | Surge Plasma | active | A new action. Raises Thermal by 45 for 16 CP and 2 Integrity, one action instead of two Ramp Plasmas. | `ForgeSurgePlasmaThermal` 45 (the action is available when above 0) |
| 45 | 3 | Fast Rebond | trait | Rebond costs 6 less Thermal and 2 less CP. | `ForgeRebondThermalReduction` 6, `ForgeRebondCpReduction` 2 |
| 50 | 4 | Red Line | active (capstone) | Once per craft, a Red Line Press: a Hydro-Press that is only available while Thermal is above the working range. It costs no Integrity and gives double Shape. | `ForgeRedLineCharges` 1 (the action is available when above 0, and per craft while a charge remains) |

Notes on the Forgemaster rows:

- **Why Overdrive Press and Surge Plasma are actives:** they add tempo choices (fewer actions, more CP and Integrity) the base five actions do not offer, and each shows an exact preview.
- **Heat Retention I and II** are two purchases of one anchor perk. Retention matters most at high levels, where cooling is 5-7 per action and a single Ramp Plasma covers about three actions.
- **Red Line is an action, not an automatic trigger,** so a player never spends the charge by accident. It is disabled whenever Thermal is inside or below the range, and the window shows why.
- **Hot Forging** raises the range, which helps Hydro-Press (best hot) and hurts Vibro-Hone (best cool), and the style's -25% Finish makes that trade explicit. Pair it with Shape-heavy recipes (armor) and Hammer Rhythm.

## Artisan: precise finishing

The Artisan finishes carefully: steadier Thermal, more Stable Containment, and Finish that does not depend on a perfect range. It pairs with Cold Working, whose lower range suits Vibro-Hone, and with weapons and jewelry, which are mostly Finish.

| Rank | SP | Perk | Type | Player-facing effect (starting values) | StatType(s) the engine reads |
| --- | --- | --- | --- | --- | --- |
| 5 | 1 | Steady Hands | trait | Vibro-Hone costs 1 less Integrity (4 becomes 3). | `ForgeHoneIntegrityReduction` +1 |
| 10 | 1 | Clean Containment I | trait | Stable Containment is announced 10 percentage points more often (35% becomes 45%). | `ForgeContainmentChancePercent` +10 |
| 15 | 2 | Micro-Hone | active | A new action. Finish at 80% of a Vibro-Hone's best result whatever the Thermal, for 4 CP and 3 Integrity. Works after a Cryo-Quench too. Stable Containment applies. | `ForgeMicroHonePercent` 80 (the action is available when above 0) |
| 20 | 2 | Cold Working | style | The working range moves down 10 Thermal. Vibro-Hone gains +25% Finish and Hydro-Press gains -25% Shape. Only one style is active at a time. | `ForgeRangeShift` -10, `ForgeHoneOutputPercent` +25, `ForgePressOutputPercent` -25 (while active) |
| 25 | 3 | Clean Containment II | trait | Stable Containment is announced 20 percentage points more often (35% becomes 55%). Replaces Clean Containment I. | `ForgeContainmentChancePercent` +20 |
| 30 | 3 | Steady Quench | trait | Cryo-Quench loses less toward the edges of the range: at an edge it gives 70% of its center bonus instead of 50%. | `ForgeQuenchEdgePercent` +20 |
| 35 | 3 | Cool Hand | trait | Vibro-Hone in the hot half of the range falls to 80% of its best result at the hot edge instead of 60%. | `ForgeHoneHotEdgePercent` +20 |
| 40 | 3 | Temper Hold | active | A new action. For the next 2 actions the piece does not cool. Costs 8 CP. | `ForgeTemperHoldActions` 2 (the action is available when above 0) |
| 45 | 3 | Containment Focus | trait | Stable Containment gives Vibro-Hone +75% instead of +50%. | `ForgeContainmentHonePercent` +25 |
| 50 | 4 | Perfect Temper | trait (capstone) | A Cryo-Quench with Thermal within 2 of the range's center gives +50% Finish. | `ForgePerfectTemperPercent` 50, `ForgePerfectTemperWindow` 2 |

Notes on the Artisan rows:

- **Clean Containment I and II** are two purchases of one anchor perk. Stable Containment is the only chance in the Forge, so its frequency is the Artisan's main lever: at 55% it is announced on more than half of the actions.
- **Micro-Hone** answers the Forge's hardest choice (when to hone) by making Finish available at any Thermal, at a CP price. Its 80% rate stays below a well-timed Vibro-Hone, so it does not replace Thermal management.
- **Temper Hold** gives the Artisan the steadiness that a Forgemaster buys with Heat Retention, for a CP price instead of a permanent trait.
- **Perfect Temper** needs the Thermal within 2 of the center when the piece is at least 80% shaped. The preview shows the bonus before the click.

## Why a player picks one or the other

| Choice | Forgemaster | Artisan |
| --- | --- | --- |
| Best recipes | Armor and shields (mostly Shape) | Weapons, lightsabers and jewelry (mostly Finish) |
| Spends | CP and Integrity for fewer actions | CP for steadier, cheaper Finish |
| Stable Containment | Takes it as it comes | Raises its frequency and its Vibro-Hone bonus |
| Style trade | Hot Forging: range up, Hydro-Press +25%, Vibro-Hone -25% | Cold Working: range down, Vibro-Hone +25%, Hydro-Press -25% |

A character who owns both styles toggles whichever suits the recipe. The styles are not alternatives of one purchase, so owning both costs the full 50 SP (all 20 purchases).

## Mechanical contract

- **Triggers and scope.** Every effect applies to the Plasma Forge only, to the owner's own craft, and to the named action. Hammer Rhythm counts Hydro-Press actions that were performed, not previews. Style effects count while the style is active.
- **Resources and state.** Red Line has one charge per craft, spent when a Red Line Press is performed. Temper Hold's remaining actions count down once per performed action. Overdrive Press cannot follow itself, tracked by a per-craft flag. All of it resets when a craft ends.
- **Costs.** CP and Integrity costs are paid when the action is performed and shown in the preview. A refused action spends nothing.
- **No refund loops.** Fast Rebond lowers Rebond's CP cost to 8; the Integrity it restores cannot exceed the meter's maximum, and Rebond is refused at full Integrity.
- **Lifecycle.** A craft that ends early (window closed, death, restart) settles as a failure under the 65% rule; per-craft counters die with it. Gear and perk stats are read once, when the craft starts, so changing gear mid-craft changes nothing.
- **Stacking.** Output percentages add together before they scale a gain. The Stable Containment bonus multiplies afterward. Range shift moves both edges and is clamped to 0-100.

## Balance evidence the perk PR must bring

Under `SkillPerkStandards.md`, with `SWLOR.CraftSim`:

- Perks optional: an unperked rank-50 crafter with good gear reaches maximum quality on at-level recipes; report the average quality, actions, CP and Integrity with and without each line.
- Style balance: for Hot Forging against Cold Working, each is the better choice on at least 30% of recipes and neither leads by more than about 5 quality points on average.
- Length: 10-20 actions with each line's purchases.
- No fixed rotation: adaptive play clearly beats the best fixed sequence, with and without perks, at the same 10-point target. Clean Containment raises the weight of the one chance in the minigame, so report it separately.
- Setup cost: report the SP, rank and gear assumptions of each build and the preparation cost of Red Line and Perfect Temper, whose bonuses need the Thermal in a narrow window.
