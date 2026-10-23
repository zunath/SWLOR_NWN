# Agriculture perk line: Angler (draft)

Draft for the owner's approval. No perk code, no StatType IDs and no Bible rows exist yet; this file is the only artifact. Every magnitude is a starting value that CraftSim tunes. Rules followed: `CraftingRevampPlan.md` (Line schedule, Lines, Agriculture fishing) and `SkillPerkStandards.md`. The Chef line is another workstream's draft.

**This draft follows the owner's decisions of October 2026.** A bite offers Quick Reel (today's catch roll at Willpower 10, no fight) or a fight. Fighting is optional, for a slightly better catch: with no perks a skilled angler lands about 1.03 times what Quick Reel lands at the fish's own level, and a skilled fight's expected catch rate is bounded to +15% (starting value) of Quick Reel's, plus 3 points for sampling error and for fish Quick Reel can hardly land. The bound covers the whole Angler line and the rare fish with Trophy Catch. The owner also decided: no Steady Hold; Fish Sense shrunk; fight perks at half strength on rare fish with Trophy Catch the only full-strength rare-fish perk; and a short help paragraph per style once perks exist.

**What the bound leaves the line.** With the unperked fight at 1.03 times Quick Reel at level, the room under the bound at the fish's own level is about 10 points (62% unperked, 72% at most). The line below fills it with Strong Line I, Hard Set I, a style and Trophy Catch: four of the plan's ten purchases. **Six rows (15, 25, 30, 35, 40 and 45) are open** for the owner (Open questions, 1).

Evidence: `fishing-line-parity.md` (command `SWLOR.CraftSim fishing --runs 1000 --seed 1`). Every percentage below is the adaptive (skilled) player's catch rate averaged over Agriculture ranks 20, 35 and 50, 1000 fights per case, on a fish at the rank's level and on a fish 3 levels above it. Quick Reel lands 60% and 30% of those fish, so the bound is 72.0% and 37.5%.

## Structure

- One independent, optional line of 25 SP. It is not needed to catch fish: Quick Reel lands them at today's rate for everyone, and an unperked skilled fight lands slightly more of them (62% of at-level fish against Quick Reel's 60%) for much more XP.
- Ten rows at Agriculture ranks 5/10/15/20/25/30/35/40/45/50, priced 1/1/2/2/3/3/3/3/3/4 SP. The rank-20 purchase is the line's style, a choice of two mutually exclusive styles through `PerkRequirementCannotHavePerk`; switching costs a Perk Refund Token, like any perk. The rank-50 purchase is the capstone with no quest requirement. **Rows 15, 25, 30, 35, 40 and 45 are open.**
- Every effect acts only on the Line, the fishing fight. All four actions (Set Hook, Reel, Give Line, Hold) are available at rank 0, so no perk unlocks an action; each perk adjusts one number. No perk touches Quick Reel, whose chance is today's formula at one fixed Willpower score.
- A style needs no toggle: ownership of one style excludes the other, so the fight reads whichever the angler owns.

## How the engine reads perks

When a bite opens the fight, the lifecycle builds an immutable `LineAnglerEffects` record from the angler's stat totals and passes it to `LineSetup.ForFish`, beside the fish, the rank and the rod's tier. Today it passes `LineAnglerEffects.None`. The engine reads only that record, never a perk, so previews and results stay pure, and a perk bought or refunded during a fight changes the next fight, not this one. Each stat below is a field of it.

The fish's pull is a share of the Tension cap before perks, so a perk that raises the cap does not make the fish stronger.

**Rare fish.** A rare fish takes the fight perks at half strength, rounded down (`LineAnglerEffects.AtHalfStrengthAgainstRareFish`): a bonus of 2 becomes 1, a bonus of 1 does nothing, and Fish Sense (which cannot be halved) is off. With the line's sizes the whole line therefore does nothing to a rare fish. Trophy Catch is the one rare-fish perk at full strength.

**Fish Sense**, if the owner fills a row with it, shows the fish's next two moves, but only on the first turn of a fight (`LineRules.FishSenseTurns` = 1); after that the window shows one move, as for everyone. The engine supports it; see Open questions, 1 for why it does not fit.

## Angler (fishing)

| Rank | SP | Perk | Type | Effect (starting values) | StatTypes the engine reads |
| --- | --- | --- | --- | --- | --- |
| 5 | 1 | Strong Line I | trait | The line holds 1 more Tension before it snaps. | `AnglerTensionCap` +1 |
| 10 | 1 | Hard Set I | trait | Set Hook drains 1 more Stamina. | `AnglerSetHookDrain` +1 |
| 15 | 2 | open | - | See Open questions, 1. | - |
| 20 | 2 | Patient Angler | style (exclusive with Aggressive Angler) | Give Line lets out 1 less Distance (4 becomes 3). | `AnglerGiveLineDistance` -1 |
| 20 | 2 | Aggressive Angler | style (exclusive with Patient Angler) | Reel pulls the fish in 1 more Distance. | `AnglerReelDistance` +1 |
| 25 | 3 | open | - | See Open questions, 1. | - |
| 30 | 3 | open | - | See Open questions, 1. | - |
| 35 | 3 | open | - | See Open questions, 1. | - |
| 40 | 3 | open | - | See Open questions, 1. | - |
| 45 | 3 | open | - | See Open questions, 1. | - |
| 50 | 4 | Trophy Catch | capstone, trait | A rare fish fights as if it were 4 steps easier on the difficulty ladder, a little over half a level weaker. It is the only perk that works on a rare fish at full strength. | `AnglerRareFishRungs` +4 |

The rank-20 row appears twice because the purchase is a choice; the line has ten rows and the angler owns one of the two at rank 20.

Decisions the line creates:

- Patient Angler against Aggressive Angler: Patient keeps the fish close by making retreat cheap, so it suits an angler who gives line to calm a Dive or a Thrash; Aggressive reels faster, so it suits one who reels against Tire and Run. The styles are balanced (below), almost to a tie.

## Evidence

Catch rate of the adaptive player, perks one at a time, against the bound (72.0% at the rank's level, 37.5% for fish 3 levels above; from `fishing-line-parity.md`):

| Purchase | Fish at the rank's level | Fish 3 levels above | Added points, mean of both | Within the bound |
| --- | --- | --- | --- | --- |
| No perks | 62.9% | 23.4% | - | Yes |
| Strong Line I (+1 Tension cap) | 62.3% | 23.1% | -0.4 | Yes |
| Hard Set I (+1 drain) | 66.1% | 22.5% | 1.2 | Yes |
| Patient Angler | 66.8% | 22.8% | 1.7 | Yes |
| Aggressive Angler | 65.2% | 24.3% | 1.6 | Yes |
| Whole line below the capstone, Patient Angler | 69.4% | 26.2% | 4.6 | Yes |
| Whole line below the capstone, Aggressive Angler | 69.2% | 24.2% | 3.6 | Yes |
| The first draft's whole line (Strong Line III, Hard Set III, Steady Hold, a full-length Fish Sense, Patient Angler -2) | 94.2% | 55.9% | 31.9 | No: over by 22.2 points |

Strong Line I alone adds nothing measurable (the -0.4 is the dice of two samples); its +1 matters only with the other purchases.

The owner asked for the whole line at level to come down to about 90%. The bound is stricter: it holds the whole line to 69% at level (72% at most).

The Fight bound gate in the report holds the whole line, in both styles, to the bound at every rank above the fish's level from 8 below to 6 above; the tightest margin anywhere in the gate's tables is 1.0 point.

Style balance (the plan's style gate in miniature: each style the better choice on at least 30% of cases, neither ahead by more than about 5 points on average): over ranks 20, 35 and 50 and fish from 4 levels above the rank to 2 below, Patient Angler is the better style on 50% of the cases and Aggressive Angler on 50% (ties within 2 points count half), and the mean gap is -0.5 points. Almost every case is a tie, so the choice between styles is small in catch rate; its weight is in how the fight feels.

Rare fish and the capstone. The six special fish are level 52 and appear at one random spot per server start, caught only with the tier 4 rod. At rank 50 Quick Reel lands one 40% of the time, so the bound is 49.0%. A skilled angler with no perks lands a rare fish 39% of the time and a common fish of the same level 41%. A rare fish takes the line at half strength, which at the line's sizes is nothing (34-36%, the dice of other samples). Trophy Catch alone at 2, 4, 6 and 8 steps lands it 45%, 46%, 50% and 49% of the time; 4 steps gives 46% alone and 41-42% with the whole line, under the 49.0% bound, where 6 steps would touch it. The capstone is worth about +7 points and stays inside the bound.

## Open questions

1. **Six open rows (15, 25, 30, 35, 40, 45).** The bound leaves about 10 points at the fish's own level and about 10 to 14 on fish one and two levels above the rank, and the line above uses most of the room at the fish's own level; fish six levels above are held to the 3-point floor. What each candidate costs when added to the whole line (Patient Angler), at the fish's own level and at the tightest row:

   | Candidate | At the rank's level | Room left in the tightest row | Within the bound |
   | --- | --- | --- | --- |
   | The whole line as drafted | 69.4% | 2.3 points | Yes |
   | with Strong Line II (+2 cap in all) | 71.4% | 0.5 | Yes |
   | with Hard Set II (+2 drain in all) | 70.9% | 0.3 | Yes |
   | with Strong Line II and Hard Set II | 70.6% | -2.8 | No |
   | with Fish Sense (two moves on the first turn) | 79.0% | -7.0 | No: over by 7.0 points |

   With the Aggressive Angler line the picture is the same (Fish Sense: over by 4.6 points). Fish Sense alone would fit (69.5% at level) but not beside a style, and a line with a style and no Strong Line or Hard Set has no more room for it. Options for the owner: (a) put Strong Line II or Hard Set II in row 15 or 25 (each fits alone, with 0.3 to 0.5 points of room); (b) loosen the bound, which each further catch-rate purchase needs; (c) fill the other rows with effects that do not raise the catch rate, such as a chance not to spend the bait or an Agriculture XP bonus for fights, each of which needs its own approval and balance evidence; (d) drop Strong Line I and Hard Set I and take Fish Sense with a style instead (Fish Sense alone is +7 points); (e) accept a four-purchase line and re-price it.
2. **Fighting is a slightly better catch, not a lot better.** `LineRules.RungAtRank` is 44 (it was 42 in the first merge and 54 in the first Quick Reel draft): a skilled angler with no perks lands 62% of at-level fish by fighting where Quick Reel lands 60%, 20% of fish 3 levels above where Quick Reel lands 30%, and 8% four levels above where Quick Reel lands 20%. A fish above the rank climbs the difficulty ladder 7 rungs a level (it was 5) and, past 3 levels, 9, so the fight falls behind Quick Reel there. Raising `RungAtRank` gives the line room one for one and takes it from the unperked fight.
3. **How "+15%" is read.** The bound is 15% more than Quick Reel's chance for the same fish and angler, plus 3 points (two standard errors of a 1000-fight case, and a floor for fish Quick Reel can hardly land). If the owner meant 15 percentage points, the line has more room.
4. **Styles in the game.** Once the perks exist, the Line window's help page gets one short paragraph per style. Draft wording: "Patient Angler: Give Line lets out one less Distance, so a fish you give line to stays close." and "Aggressive Angler: each Reel pulls the fish in one more Distance, so you land it in fewer turns."

## Proposed StatTypes

Names only; the IDs are reserved against current master and the HAK when the owner approves the rows (the next free StatType today is 1168). Each declares its classification, polarity and category on its enum entry. All are beneficial when positive, except `AnglerGiveLineDistance`, whose negative value is the benefit (less Distance let out).

| Name | Aggregation | Used by |
| --- | --- | --- |
| `AnglerTensionCap` | additive | Strong Line |
| `AnglerSetHookDrain` | additive | Hard Set |
| `AnglerGiveLineDistance` | additive | Patient Angler |
| `AnglerReelDistance` | additive | Aggressive Angler |
| `AnglerRareFishRungs` | additive | Trophy Catch |
| `AnglerForecastDepth` | additive, capped at 1 | Fish Sense, only if the owner fills a row with it |

The engine fields they feed are `LineAnglerEffects.TensionCapBonus`, `SetHookDrainBonus`, `GiveLineDistanceReduction`, `ReelDistanceBonus`, `RareFishRungReduction` and `ForecastDepth`. They exist now and default to zero; a unit test pins what each one changes. `HoldDrainBonus` and `ReelTensionBonus` also exist and stay zero: Steady Hold (dropped by the owner) and the first draft's Aggressive Angler (+2 Reel Distance and +2 Tension) took the line over the bound.

## Player descriptions (draft wording)

- Strong Line: "Your fishing line holds N more Tension before it snaps."
- Hard Set: "Setting the hook drains N more Stamina from the fish."
- Patient Angler: "Giving line lets out 1 less Distance."
- Aggressive Angler: "Reeling pulls the fish in 1 more Distance."
- Trophy Catch: "Rare fish fight a little over half a level weaker. Your other fishing perks work at half strength on rare fish."

## Integration notes

- Perks act on the fight's snapshot at the bite. Equipment, perk and rank changes during a fight do not change it, and a fight that ends early (window closed, logout, moving away, combat, death, the rod taken out of the hand, another activity such as resting taking over) loses the fish as an escape under the same rule as an unperked fight.
- Bait, the fishing point's bites and today's XP are spent at the bite, so no perk can change them. The rest of the multiplied XP is paid when the fight ends on its own, in proportion to how long it lasted (up to five turns), so a perk that shortens a fight also shortens its pay, and the fight enforces `LineRules.TurnSeconds` between two actions, so no perk can speed the turns either. The multiplied XP is today's times the fish-level band's multiplier (`LineRules.XpMultiplierForLevel`, through `CraftMinigameXp`), so a perk that shortens a fight must not shorten it below what the multiplier was passed for; the XP-rate gate in `fishing-line-parity.md` is the check.
- Quick Reel is the fish choice and a perk never changes it: the angler resolves the bite at once by today's catch roll at Willpower 10 (a character with no attribute investment), and only before the first move of a fight, so no angler can start a fight and fall back on it.
- No perk changes which fish bites or how often; fish selection, bait and location stay as today.
- The bound is a CraftSim gate (`Fight bound` in `fishing-line-parity.md`) and tests pin it: every purchase above is within it, the whole line in either style is within it, and so is a rare fish with Trophy Catch.
