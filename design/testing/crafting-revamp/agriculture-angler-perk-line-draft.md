# Agriculture perk line: Angler (draft)

Draft for the owner's approval. No perk code, no StatType IDs and no Bible rows exist yet; this file is the only artifact. Every magnitude is a starting value that CraftSim tunes. Rules followed: `CraftingRevampPlan.md` (Line schedule, Lines, Agriculture fishing) and `SkillPerkStandards.md`. The Chef line is another workstream's draft.

**This draft was cut to fit the owner's decision of October 2026.** A bite now offers Quick Reel (today's catch roll, no fight) or a fight, and a skilled fight's expected catch rate is bounded to +15% (starting value) of Quick Reel's, plus 3 points for sampling error and for fish Quick Reel can hardly land. The bound covers the whole Angler line and the rare fish with Trophy Catch. The first draft's whole line lifted a skilled angler 40 points, well past it, so the line below buys seven of the plan's ten purchases; three rows are open for the owner (Open questions, 1).

Evidence: `fishing-line-parity.md` (command `SWLOR.CraftSim fishing --runs 1000 --seed 1`). Every percentage below is the adaptive (skilled) player's catch rate averaged over Agriculture ranks 20, 35 and 50, 1000 fights per case, on a fish at the rank's level and on a fish 3 levels above it. Quick Reel lands 64% and 34% of those fish, so the bound is 76.6% and 42.1%.

## Structure

- One independent, optional line of 25 SP. It is not needed to catch fish: Quick Reel lands them at today's rate for everyone, and an unperked fight is the XP choice (a skilled angler lands 47% of at-level fish by fighting against Quick Reel's 64%).
- Ten rows at Agriculture ranks 5/10/15/20/25/30/35/40/45/50, priced 1/1/2/2/3/3/3/3/3/4 SP. The rank-20 purchase is the line's style, a choice of two mutually exclusive styles through `PerkRequirementCannotHavePerk`; switching costs a Perk Refund Token, like any perk. The rank-50 purchase is the capstone with no quest requirement. **Rows 35, 40 and 45 are open.**
- Ranked perks use up to three purchases. Each rank restates the benefit inside its own total (only the purchased rank's stat bonuses apply).
- Every effect acts only on the Line, the fishing fight. All four actions (Set Hook, Reel, Give Line, Hold) are available at rank 0, so no perk unlocks an action; each perk adjusts one number. No perk touches Quick Reel, whose chance is today's formula at one fixed Willpower score.
- A style needs no toggle: ownership of one style excludes the other, so the fight reads whichever the angler owns.

## How the engine reads perks

When a bite opens the fight, the lifecycle builds an immutable `LineAnglerEffects` record from the angler's stat totals and passes it to `LineSetup.ForFish`, beside the fish, the rank and the rod's tier. Today it passes `LineAnglerEffects.None`. The engine reads only that record, never a perk, so previews and results stay pure, and a perk bought or refunded during a fight changes the next fight, not this one. Each stat below is a field of it.

The fish's pull is a share of the Tension cap before perks, so a perk that raises the cap does not make the fish stronger. **A rare fish ignores every field but `RareFishRungReduction`**: only Trophy Catch helps against it, so the rest of the line cannot lift the supply of rare fish past the bound.

## Angler (fishing)

| Rank | SP | Perk | Type | Effect (starting values) | StatTypes the engine reads |
| --- | --- | --- | --- | --- | --- |
| 5 | 1 | Strong Line I | trait | The line holds 1 more Tension before it snaps. | `AnglerTensionCap` +1 |
| 10 | 1 | Hard Set I | trait | Set Hook drains 1 more Stamina. | `AnglerSetHookDrain` +1 |
| 15 | 2 | Strong Line II | trait | The line holds 2 more Tension in total (this rank replaces Strong Line I's +1). | `AnglerTensionCap` +2 |
| 20 | 2 | Patient Angler | style (exclusive with Aggressive Angler) | Give Line lets out 1 less Distance (4 becomes 3). | `AnglerGiveLineDistance` -1 |
| 20 | 2 | Aggressive Angler | style (exclusive with Patient Angler) | Reel pulls the fish in 1 more Distance. | `AnglerReelDistance` +1 |
| 25 | 3 | Hard Set II | trait | Set Hook drains 2 more Stamina in total (replaces Hard Set I's 1). | `AnglerSetHookDrain` +2 |
| 30 | 3 | Fish Sense | trait | The window shows the fish's next two moves, not one. | `AnglerForecastDepth` +1 |
| 35 | 3 | open | - | See Open questions, 1. | - |
| 40 | 3 | open | - | See Open questions, 1. | - |
| 45 | 3 | open | - | See Open questions, 1. | - |
| 50 | 4 | Trophy Catch | capstone, trait | A rare fish fights as if it were 12 steps easier on the difficulty ladder, about 2.4 levels weaker. It is the only perk a rare fish lets through. | `AnglerRareFishRungs` +12 |

The rank-20 row appears twice because the purchase is a choice; the line has ten rows and the angler owns one of the two at rank 20.

Decisions the line creates:

- Patient Angler against Aggressive Angler: Patient keeps the fish close by making retreat cheap, so it suits an angler who gives line to calm a Dive or a Thrash; Aggressive reels faster, so it suits one who reels against Tire and Run. The styles are balanced (below).
- Hard Set against Strong Line: draining Stamina ends the fight early because an exhausted fish only rests, while a bigger cap lets the angler survive a longer one.
- Fish Sense changes planning more than any other perk: it turns "react to the next move" into "plan around two moves", and it is the largest single purchase of the line.

## Evidence

Catch rate of the adaptive player, perks one at a time, against the bound (76.6% at the rank's level, 42.1% for fish 3 levels above; from `fishing-line-parity.md`):

| Purchase | Fish at the rank's level | Fish 3 levels above | Added points, mean of both | Within the bound |
| --- | --- | --- | --- | --- |
| No perks | 47.4% | 12.1% | - | Yes |
| Strong Line I / II (+1 / +2 Tension cap) | 48.2% / 52.4% | 17.1% / 22.1% | 3.0 / 7.5 | Yes |
| Hard Set I / II (+1 / +2 drain) | 49.9% / 49.1% | 14.1% / 13.6% | 2.3 / 1.6 | Yes |
| Fish Sense | 59.2% | 19.4% | 9.6 | Yes |
| Patient Angler | 49.9% | 13.5% | 2.0 | Yes |
| Aggressive Angler | 48.9% | 14.1% | 1.8 | Yes |
| Whole line below the capstone, Patient Angler | 69.8% | 33.7% | 22.0 | Yes |
| Whole line below the capstone, Aggressive Angler | 68.0% | 35.9% | 22.2 | Yes |
| The first draft's whole line (Strong Line III, Hard Set III, Steady Hold, Fish Sense, Patient Angler -2) | 81.5% | 58.3% | 40.2 | No: over by 16.2 points |

The Fight bound gate in the report also holds the whole line, in both styles, to the bound at every rank above the fish's level from 8 below to 6 above. It is among the tightest on fish 7 levels above the rank, which Quick Reel cannot land at all, so the bound there is the 3-point floor and the line lands about 3% of them.

Style balance (the plan's style gate in miniature: each style the better choice on at least 30% of cases, neither ahead by more than about 5 points on average): over ranks 20, 35 and 50 and fish from 4 levels above the rank to 2 below, Patient Angler is the better style on 48% of the cases and Aggressive Angler on 52% (ties within 2 points count half), and the mean gap is -0.4 points.

Rare fish and the capstone. The six special fish are level 52 and appear at one random spot per server start, caught only with the tier 4 rod. At rank 50 Quick Reel lands one 44% of the time, so the bound is 53.6%. A skilled angler with no perks lands a rare fish 30% of the time and a common fish of the same level 31%. A rare fish ignores the rest of the line, so the whole line adds nothing (28%). Trophy Catch at 4, 8, 10, 12 and 14 steps lands it 38%, 42%, 42%, 49% and 52% of the time, so 12 steps puts a skilled angler at 49%, under the 53.6% bound, and the whole line changes nothing (48-49%). The capstone is worth about +19 points and stays inside the bound.

## Open questions

1. **Three open rows.** The bound leaves room for about seven purchases that raise the catch rate: the first draft's whole line used 40 points where this one uses 22, and the fight bound is tightest in the middle of the range where the line is most useful. Options for the owner: (a) loosen the bound, since each further catch-rate purchase (Strong Line III, Hard Set III, Steady Hold) adds a few points and needs that much room; (b) fill rows 35, 40 and 45 with effects that do not raise the catch rate, such as a chance not to spend the bait or an Agriculture XP bonus for fights, each of which needs its own approval and balance evidence; (c) accept a seven-purchase line and re-price it.
2. **The unperked fight lands fewer fish than Quick Reel.** To leave the line room under the bound, `LineRules.RungAtRank` is 54 (it was 42): a skilled angler with no perks lands 47% of at-level fish by fighting where Quick Reel lands 64%, and 12% of fish 3 levels above where Quick Reel lands 34%. The fight pays the XP; Quick Reel gives the fish. Lowering `RungAtRank` strengthens the unperked fight and takes the room away from the line one for one.
3. **How "+15%" is read.** The bound is 15% more than Quick Reel's chance for the same fish and angler, plus 3 points (two standard errors of a 1000-fight case, and a floor for fish Quick Reel can hardly land). If the owner meant 15 percentage points, the line and the unperked fight both have more room.
4. **Fish Sense** is the largest single purchase (9.6 points) and the one that changes how the fight is played. It is the first to drop if the owner wants the line's catch-rate gain smaller still.
5. **Styles and the first-use help.** The window's help page describes Quick Reel and the fight without perks. If the owner wants the styles explained in the game, the help page needs a paragraph per style once the perks exist.

## Proposed StatTypes

Names only; the IDs are reserved against current master and the HAK when the owner approves the rows (the next free StatType today is 1168). Each declares its classification, polarity and category on its enum entry. All are beneficial when positive, except `AnglerGiveLineDistance`, whose negative value is the benefit (less Distance let out).

| Name | Aggregation | Used by |
| --- | --- | --- |
| `AnglerTensionCap` | additive | Strong Line |
| `AnglerSetHookDrain` | additive | Hard Set |
| `AnglerForecastDepth` | additive, capped at 1 | Fish Sense |
| `AnglerGiveLineDistance` | additive | Patient Angler |
| `AnglerReelDistance` | additive | Aggressive Angler |
| `AnglerRareFishRungs` | additive | Trophy Catch |

The engine fields they feed are `LineAnglerEffects.TensionCapBonus`, `SetHookDrainBonus`, `ForecastDepth`, `GiveLineDistanceReduction`, `ReelDistanceBonus` and `RareFishRungReduction`. They exist now and default to zero; a unit test pins what each one changes. `HoldDrainBonus` and `ReelTensionBonus` also exist and stay zero: Steady Hold and the first draft's Aggressive Angler (+2 Reel Distance and +2 Tension) were cut because they took the line over the bound.

## Player descriptions (draft wording)

- Strong Line: "Your fishing line holds N more Tension before it snaps."
- Hard Set: "Setting the hook drains N more Stamina from the fish."
- Patient Angler: "Giving line lets out 1 less Distance."
- Aggressive Angler: "Reeling pulls the fish in 1 more Distance."
- Fish Sense: "You can see the fish's next two moves."
- Trophy Catch: "Rare fish fight about two and a half levels weaker. Rare fish are not helped by your other fishing perks."

## Integration notes

- Perks act on the fight's snapshot at the bite. Equipment, perk and rank changes during a fight do not change it, and a fight that ends early (window closed, logout, moving away, combat, death, the rod taken out of the hand, another activity such as resting taking over) loses the fish as an escape under the same rule as an unperked fight.
- Bait, the fishing point's bites and today's XP are spent at the bite, so no perk can change them. The rest of the multiplied XP is paid when the fight ends on its own, in proportion to how long it lasted (up to five turns), so a perk that shortens a fight also shortens its pay, and the fight enforces `LineRules.TurnSeconds` between two actions, so no perk can speed the turns either. The multiplied XP is today's times the fish-level band's multiplier (`LineRules.XpMultiplierForLevel`, through `CraftMinigameXp`), so a perk that shortens a fight must not shorten it below what the multiplier was passed for; the XP-rate gate in `fishing-line-parity.md` is the check.
- Quick Reel is the fish choice and a perk never changes it: the angler resolves the bite at once by today's catch roll at Willpower 12 (pending the owner's attribute-median decision), and only before the first move of a fight, so no angler can start a fight and fall back on it.
- No perk changes which fish bites or how often; fish selection, bait and location stay as today.
- The bound is a CraftSim gate (`Fight bound` in `fishing-line-parity.md`) and tests pin it: every purchase above is within it, the whole line in either style is within it, and so is a rare fish with Trophy Catch.
