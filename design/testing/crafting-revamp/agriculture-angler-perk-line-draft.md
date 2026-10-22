# Agriculture perk line: Angler (draft)

Draft for the owner's approval. No perk code, no StatType IDs and no Bible rows exist yet; this file is the only artifact. Every magnitude is a starting value that CraftSim tunes. Rules followed: `CraftingRevampPlan.md` (Line schedule, Lines, Agriculture fishing) and `SkillPerkStandards.md`. The Chef line is another workstream's draft.

Evidence: `fishing-line-parity.md` (command `SWLOR.CraftSim fishing --runs 1000 --seed 1`). Every percentage below is the adaptive player's catch rate averaged over Agriculture ranks 20, 35 and 50, 1000 fights per case, on a fish at the rank's level and on a fish 3 levels above it.

## Structure

- One independent, optional line of 25 SP. It is not needed to catch fish: an unperked angler already lands 68% of at-level fish and 37% of fish 3 levels above the rank (the parity report checks that against today's catch roll).
- Ten purchases at Agriculture ranks 5/10/15/20/25/30/35/40/45/50, priced 1/1/2/2/3/3/3/3/3/4 SP. The rank-20 purchase is the line's style, a choice of two mutually exclusive styles through `PerkRequirementCannotHavePerk`; switching costs a Perk Refund Token, like any perk. The rank-50 purchase is the capstone with no quest requirement.
- Ranked perks use up to three purchases. Each rank restates the benefit inside its own total (only the purchased rank's stat bonuses apply).
- Every effect acts only on the Line, the fishing fight. All four actions (Set Hook, Reel, Give Line, Hold) are available at rank 0, so no perk unlocks an action; each perk adjusts one number.
- A style needs no toggle: ownership of one style excludes the other, so the fight reads whichever the angler owns.

## How the engine reads perks

When a bite opens the fight, the lifecycle builds an immutable `LineAnglerEffects` record from the angler's stat totals and passes it to `LineSetup.ForFish`, beside the fish, the rank and the rod's tier. Today it passes `LineAnglerEffects.None`. The engine reads only that record, never a perk, so previews and results stay pure, and a perk bought or refunded during a fight changes the next fight, not this one. Each stat below is a field of it.

The fish's pull is a share of the Tension cap before perks, so a perk that raises the cap does not make the fish stronger.

## Angler (fishing)

| Rank | SP | Perk | Type | Effect (starting values) | StatTypes the engine reads |
| --- | --- | --- | --- | --- | --- |
| 5 | 1 | Strong Line I | trait | The line holds 1 more Tension before it snaps. | `AnglerTensionCap` +1 |
| 10 | 1 | Hard Set I | trait | Set Hook drains 2 more Stamina. | `AnglerSetHookDrain` +2 |
| 15 | 2 | Strong Line II | trait | The line holds 2 more Tension in total (this rank replaces Strong Line I's +1). | `AnglerTensionCap` +2 |
| 20 | 2 | Patient Angler | style (exclusive with Aggressive Angler) | Give Line lets out 2 less Distance (4 becomes 2). | `AnglerGiveLineDistance` -2 |
| 20 | 2 | Aggressive Angler | style (exclusive with Patient Angler) | Reel pulls the fish in 2 more Distance and adds 2 more Tension. | `AnglerReelDistance` +2, `AnglerReelTension` +2 |
| 25 | 3 | Hard Set II | trait | Set Hook drains 3 more Stamina in total (replaces Hard Set I's 2). | `AnglerSetHookDrain` +3 |
| 30 | 3 | Fish Sense | trait | The window shows the fish's next two moves, not one. | `AnglerForecastDepth` +1 |
| 35 | 3 | Strong Line III | trait | The line holds 3 more Tension in total (replaces Strong Line II's 2). | `AnglerTensionCap` +3 |
| 40 | 3 | Hard Set III | trait | Set Hook drains 4 more Stamina in total (replaces Hard Set II's 3). | `AnglerSetHookDrain` +4 |
| 45 | 3 | Steady Hold | trait | Hold drains 1 more Stamina. | `AnglerHoldDrain` +1 |
| 50 | 4 | Trophy Catch | capstone, trait | A rare fish fights as if it were 8 steps easier on the difficulty ladder, about 1.6 levels weaker. | `AnglerRareFishRungs` +8 |

The rank-20 row appears twice because the purchase is a choice; the line has ten purchases and the angler owns one of the two.

Decisions the line creates:

- Patient Angler against Aggressive Angler: Patient keeps the fish close by making retreat cheap, so it suits an angler who gives line to calm a Dive or a Thrash; Aggressive reels faster and takes the strain, so it suits one who reels against Tire and Run. The styles are balanced at about +6 points each (below).
- Hard Set and Steady Hold against Strong Line: draining Stamina ends the fight early because an exhausted fish only rests, while a bigger cap lets the angler survive a longer one.
- Fish Sense changes planning more than any other perk: it turns "react to the next move" into "plan around two moves".

## Evidence

Catch rate of the adaptive player, perks one at a time (from `fishing-line-parity.md`):

| Purchase | Fish at the rank's level | Fish 3 levels above | Added points, mean of both |
| --- | --- | --- | --- |
| No perks | 67.8% | 36.8% | - |
| Strong Line I / II / III (+1 / +2 / +3 Tension cap) | 67.9% / 70.0% / 72.4% | 42.5% / 46.7% / 49.0% | 2.9 / 6.0 / 8.4 |
| Hard Set I / II / III (+2 / +3 / +4 drain) | 70.0% / 70.2% / 76.1% | 38.9% / 40.2% / 43.9% | 2.2 / 2.9 / 7.7 |
| Steady Hold (+1 drain) | 72.4% | 39.0% | 3.4 |
| Fish Sense | 83.2% | 48.5% | 13.5 |
| Patient Angler | 76.2% | 40.2% | 5.9 |
| Aggressive Angler | 70.6% | 46.4% | 6.2 |
| Whole line below the capstone, Patient Angler | 98.9% | 77.2% | 35.7 |
| Whole line below the capstone, Aggressive Angler | 95.2% | 66.8% | 28.7 |

Style balance (the plan's style gate in miniature: each style the better choice on at least 30% of cases, neither ahead by more than about 5 points on average): over ranks 20, 35 and 50 and fish from 4 levels above the rank to 2 below, Patient Angler is the better style on 69% of the cases and Aggressive Angler on 31% (ties within 2 points count half), and the mean gap is 0.2 points.

Rare fish and the capstone. The six special fish are level 52 and appear at one random spot per server start, caught only with the tier 4 rod. At rank 50 the adaptive player lands a rare fish 52% of the time and a common fish of the same level 59%; today's catch chance for that fish is 36-60% across Willpower 8-20 (40% at Willpower 10, 48% at 14). Trophy Catch at 2, 4, 6, 8 and 12 steps lands it 52%, 54%, 55%, 55% and 65% of the time with no other perk, so 8 steps is worth about +3 points alone and stays inside today's range.

## Open questions

1. **Whole-line power.** All nine fight purchases together lift the adaptive player from 68% to about 99% on at-level fish and from 37% to 77% on fish 3 levels above (Aggressive Angler: 95% and 67%). Steady Hold is the cheapest purchase to drop (3.4 points), Fish Sense the largest (13.5). The owner decides how much of the gap between unperked and perfect play a 25 SP line should buy.
2. **Rare-fish supply.** The parity check for the capstone is today's best case, 60% (Willpower 20). With the whole line below the capstone the adaptive player already lands a rare fish 97% of the time, and Trophy Catch adds about 2 points on top (the line has saturated), so Trophy Catch cannot keep rare-fish supply within the check alone. The rule-of-thumb player stays at 44-45% with the whole line, so the effect is concentrated in skilled play. Options for the owner: (a) accept it, since rare fish still appear at one random spot per start; (b) apply the cap, drain and Fish Sense perks at half strength to rare fish; (c) make the capstone the only perk-driven change to rare fish by exempting them from the rest of the line. Nothing here changes the encounter chance of a rare fish, which is outside the fight.
3. **Hard Set is a small perk until the drain is large.** Set Hook costs the first turn, so the first rank (+2 drain) alone adds about 2 points. The curve is steep: a +4 drain is worth about 9 points on its own and +6 about 15, so the steps are chosen to stay small.
4. **Styles and the first-use help.** The window's help page describes the fight without perks. If the owner wants the styles explained in the game, the help page needs a paragraph per style once the perks exist.

## Proposed StatTypes

Names only; the IDs are reserved against current master and the HAK when the owner approves the rows (the next free StatType today is 1168). Each declares its classification, polarity and category on its enum entry. All are beneficial when positive, except `AnglerGiveLineDistance`, whose negative value is the benefit (less Distance let out), and `AnglerReelTension`, whose positive value is the cost of Aggressive Angler (a style cost, not a benefit).

| Name | Aggregation | Used by |
| --- | --- | --- |
| `AnglerTensionCap` | additive | Strong Line |
| `AnglerSetHookDrain` | additive | Hard Set |
| `AnglerHoldDrain` | additive | Steady Hold |
| `AnglerForecastDepth` | additive, capped at 1 | Fish Sense |
| `AnglerGiveLineDistance` | additive | Patient Angler |
| `AnglerReelDistance` | additive | Aggressive Angler |
| `AnglerReelTension` | additive | Aggressive Angler |
| `AnglerRareFishRungs` | additive | Trophy Catch |

The engine fields they feed are `LineAnglerEffects.TensionCapBonus`, `SetHookDrainBonus`, `HoldDrainBonus`, `ForecastDepth`, `GiveLineDistanceReduction`, `ReelDistanceBonus`, `ReelTensionBonus` and `RareFishRungReduction`. They exist now and default to zero; a unit test pins what each one changes.

## Player descriptions (draft wording)

- Strong Line: "Your fishing line holds N more Tension before it snaps."
- Hard Set: "Setting the hook drains N more Stamina from the fish."
- Patient Angler: "Giving line lets out 2 less Distance."
- Aggressive Angler: "Reeling pulls the fish in 2 more Distance and adds 2 more Tension."
- Fish Sense: "You can see the fish's next two moves."
- Steady Hold: "Holding drains 1 more Stamina from the fish."
- Trophy Catch: "Rare fish fight about one and a half levels weaker."

## Integration notes

- Perks act on the fight's snapshot at the bite. Equipment, perk and rank changes during a fight do not change it, and a fight that ends early (window closed, logout, moving away, combat, death, the rod taken out of the hand, another activity such as resting taking over) loses the fish as an escape under the same rule as an unperked fight.
- Bait, the fishing point's bites and today's XP are spent at the bite, so no perk can change them. The rest of the multiplied XP is paid when the fight ends on its own, and the fight enforces `LineRules.TurnSeconds` between two actions, so no perk can speed the turns either. The multiplied XP is today's times the fish-level band's multiplier (`LineRules.XpMultiplierForLevel`, through `CraftMinigameXp`), so a perk that shortens a fight must not shorten it below what the multiplier was passed for; the XP-rate gate in `fishing-line-parity.md` is the check.
- No perk changes which fish bites or how often; fish selection, bait and location stay as today.
