# Crafting revamp: the Line (fishing fight) against today's catch roll

- Command: `SWLOR.CraftSim fishing --runs 1000 --seed 1 --out design/testing/crafting-revamp/fishing-line-parity.md`
- Seed 1; 1000 runs per case.

## Inputs

- Today (the catch roll the fight replaces): a bite lands the fish at `40 + 10 x (Agriculture rank - fish level) + 2 x Willpower score` percent, kept from 0 to 95. It is played at Willpower scores 8, 10, 12, 14, 16, 18, 20 (`AttributeSweep`); the corridor the fight is judged against is Willpower 10 to 14, because the median Willpower of active characters is not measured yet.
- The fight: the Line engine, 1000 fights per case, each with its own dice from seed 1. No attributes are read. Fish strength comes from the fish's level against the angler's rank (`LineSetup.ForFish`); the rod tier and the rank set the Tension cap and the reel.
- Adaptive player: depth 3, forecast samples per result 2, budget 4000 engine copies per decision, 0.5% of value lost per extra action, heuristic LineLeafHeuristic. It is a floor on skilled play. Rule-of-thumb player: reads the announced move and answers it by a fixed rule, never looking further ahead.
- Time: 7.0 s for a cast and the wait for a bite (6-8 s in the game), in both loops; 8.0 s for each turn of the fight. Both are estimates until the client run measures them. The game enforces the turn as the least time between two actions (`LineRules.TurnSeconds`, the first action free), so fast clicking cannot beat the pace the XP multipliers assume.
- XP cohort (gate 6): fish-level bands 0-10, 11-20, 21-30, 31-40 and 41-52; the angler's rank equals the fish's level (at most 50) with the best rod tier that rank allows; every (rod, bait) table of every location that rod reaches counts equally, half by day and half by night, so both loops draw the same fish. XP is per bite.
- Tolerance: the adaptive player's catch rate is near today's when it sits within 10 points of the corridor.
- XP: today's XP for the fish against the rank is paid at the bite, caught or not, as today; the rest of the multiplied XP is paid when the fight ends on its own (landed, snapped or escaped), in proportion to how long the fight lasted up to 5 turns of 8.0 s, and never for a fight the angler abandons, so neither skipping the fight nor losing it on purpose after one move earns more per hour than today. The multiplied XP is today's times a multiplier by the fish's level band, as the game server applies it (`CraftMinigameXp`): 0-10 8.75x, 11-20 8.55x, 21-30 8.35x, 31-40 8.25x, 41-52 8.05x. Each is the least multiple of 0.05 that keeps gate 6 passing in its band at this report's runs and seed; 1.0 in every band restores today's XP per bite.
- Rod tiers: Bamboo, Carbon and Clothespole 0; Composite, Ebisu and Fastwater 10; Glass Fiber, Halcyon and Judge 20; Maze Monger, Willow and Yew 30; Butterfly, Tranquility and LuShang 40.
- Named starting values of the fight (`LineRules`), all tunable:
  - `StartTension` = 0
  - `RanksPerPower` = 10
  - `TensionCapBase` = 20
  - `TensionCapPerRodTier` = 1
  - `RanksPerTensionCapPoint` = 12
  - `ReelDistanceBase` = 6
  - `PowerPerReelDistanceStep` = 6
  - `ReelDistancePerStep` = 2
  - `StartDistanceBase` = 5
  - `StartDistancePerReelDistance` = 2
  - `EscapeBufferBase` = 2
  - `EscapeBufferPerReelDistance` = 2
  - `ReelTension` = 4
  - `GiveLineDistance` = 4
  - `GiveLineRelief` = 6
  - `HoldTension` = 2
  - `HoldDrain` = 4
  - `SetHookTension` = 2
  - `SetHookDrain` = 6
  - `RunReelStrain` = 2
  - `ThrashSpike` = 2
  - `TireReelDistance` = 2
  - `TireStaminaLoss` = 2
  - `RungAtRank` = 42
  - `RungsPerLevel` = 5
  - `RungMinimum` = 0
  - `RungMaximum` = 100
  - `PullPercentOfCapAtRung0` = 10
  - `PullPercentPerRung` = 0.8
  - `StaminaAtRung0` = 8
  - `StaminaPerRung` = 0.24
  - `TireWeightAtRung0` = 45
  - `TireWeightPerRung` = 0.45
  - `TireWeightMinimum` = 2
  - `TurnSeconds` = 8
  - `XpFullTurns` = 5
  - `XpMultiplierLevels0To10` = 8.75
  - `XpMultiplierLevels11To20` = 8.55
  - `XpMultiplierLevels21To30` = 8.35
  - `XpMultiplierLevels31To40` = 8.25
  - `XpMultiplierLevels41Up` = 8.05

## Verdicts

| Gate | Verdict | Summary |
| --- | --- | --- |
| Economy parity | Passed | Catch rate per bite: the adaptive player stays within 10 points of today's Willpower 10-14 corridor at every rank above the fish's level and at every rank over the real pools (furthest outside: 5.1 points). |
| Length | Passed | The adaptive player's fights last 3-6 actions (fishing fight) in every row. |
| XP rate | Passed | Fishing: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.00x in 31-40 / best rod for the rank, rank = fish level (at most 50)). |
| Evidence: Angler perk magnitudes | NotApplicable | Not a gate. What each proposed Angler purchase adds to the adaptive player's catch rate; the draft quotes it. |
| Evidence: style balance | NotApplicable | Not a gate. Patient Angler is the better style on 69% of the cases and Aggressive Angler on 31%; the mean gap is 0.2 points (Patient less Aggressive). |
| Evidence: rare fish and Trophy Catch | NotApplicable | Not a gate. The ceiling for the capstone is today's catch chance for a level 52 fish at rank 50 for a character with Willpower 20: 60%. |

## Economy parity

### Landed per bite by rank above the fish's level, at ranks 10, 20, 30, 40, 50 with the best rod each can use (1000 fights per fish; corridor Willpower 10-14)

| Rank - fish level | Today, Willpower 8 | Today, Willpower 10 | Today, Willpower 12 | Today, Willpower 14 | Today, Willpower 16 | Today, Willpower 18 | Today, Willpower 20 | Fight, adaptive | Adaptive, by rank | Fight, rule of thumb | Between the two players | Adaptive turns | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| -8 | 0% | 0% | 0% | 0% | 0% | 0% | 0% | 3% | 2-4% | 3% | 3% | 4.2 | Near |
| -7 | 0% | 0% | 0% | 0% | 2% | 6% | 10% | 5% | 3-7% | 5% | 5% | 4.5 | Near |
| -6 | 0% | 0% | 4% | 8% | 12% | 16% | 20% | 10% | 7-14% | 8% | 9% | 4.8 | Near |
| -5 | 6% | 10% | 14% | 18% | 22% | 26% | 30% | 16% | 10-20% | 10% | 13% | 5.0 | Near |
| -4 | 16% | 20% | 24% | 28% | 32% | 36% | 40% | 26% | 16-35% | 14% | 20% | 4.8 | Near |
| -3 | 26% | 30% | 34% | 38% | 42% | 46% | 50% | 35% | 25-43% | 19% | 27% | 4.9 | Near |
| -2 | 36% | 40% | 44% | 48% | 52% | 56% | 60% | 51% | 37-59% | 30% | 40% | 5.0 | Near |
| -1 | 46% | 50% | 54% | 58% | 62% | 66% | 70% | 57% | 49-63% | 38% | 48% | 5.0 | Near |
| 0 | 56% | 60% | 64% | 68% | 72% | 76% | 80% | 67% | 61-70% | 46% | 56% | 5.2 | Near |
| +1 | 66% | 70% | 74% | 78% | 82% | 86% | 90% | 73% | 68-76% | 58% | 65% | 5.4 | Near |
| +2 | 76% | 80% | 84% | 88% | 92% | 95% | 95% | 83% | 79-86% | 69% | 76% | 5.5 | Near |
| +3 | 86% | 90% | 94% | 95% | 95% | 95% | 95% | 92% | 86-96% | 77% | 84% | 5.3 | Near |
| +4 | 95% | 95% | 95% | 95% | 95% | 95% | 95% | 98% | 94-100% | 86% | 92% | 4.3 | Near |
| +5 | 95% | 95% | 95% | 95% | 95% | 95% | 95% | 100% | 99-100% | 93% | 96% | 3.2 | Near |
| +6 | 95% | 95% | 95% | 95% | 95% | 95% | 95% | 100% | 100-100% | 97% | 99% | 3.0 | Near |

### Landed per bite by Agriculture rank, over every rod and bait pool the angler can use (each location, rod and bait, daytime and nighttime counted separately; pools weighted equally, fish within a pool by frequency; the pool average mixes pools no single angler fishes, so it compares rules and is not a rank's own experience)

| Agriculture rank | Today, Willpower 10 | Today, Willpower 14 | Today, Willpower 8-20 | Fight, adaptive | Fight, rule of thumb | Between the two players | Adaptive turns | Pools | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | 30% | 36% | 27-47% | 30% | 17% | 23% | 5.2 | 194 | Near |
| 5 | 71% | 76% | 68-82% | 70% | 51% | 60% | 5.6 | 194 | Near |
| 10 | 62% | 64% | 61-68% | 64% | 57% | 61% | 4.3 | 313 | Near |
| 15 | 79% | 82% | 78-86% | 83% | 75% | 79% | 3.9 | 313 | Near |
| 20 | 73% | 76% | 72-79% | 76% | 70% | 73% | 3.9 | 445 | Near |
| 25 | 87% | 89% | 87-90% | 91% | 86% | 89% | 3.6 | 445 | Near |
| 30 | 79% | 81% | 78-83% | 84% | 80% | 82% | 3.6 | 571 | Near |
| 35 | 89% | 91% | 89-92% | 95% | 92% | 93% | 3.4 | 571 | Near |
| 40 | 81% | 82% | 80-83% | 86% | 84% | 85% | 3.4 | 699 | Near |
| 45 | 87% | 88% | 86-89% | 92% | 90% | 91% | 3.3 | 699 | Near |
| 50 | 92% | 93% | 92-94% | 98% | 96% | 97% | 3.2 | 699 | Near |

## Length

### Fight length by rank above the fish's level (target 3-6 actions (fishing fight); mean over ranks 10, 20, 30, 40, 50)

| Rank - fish level | Adaptive turns | Rule-of-thumb turns | Verdict |
| --- | --- | --- | --- |
| -6 | 4.8 | 3.3 | Within |
| -5 | 5.0 | 3.3 | Within |
| -4 | 4.8 | 3.4 | Within |
| -3 | 4.9 | 4.2 | Within |
| -2 | 5.0 | 4.8 | Within |
| -1 | 5.0 | 5.3 | Within |
| 0 | 5.2 | 5.5 | Within |
| +1 | 5.4 | 5.4 | Within |
| +2 | 5.5 | 5.3 | Within |
| +3 | 5.3 | 5.4 | Within |
| +4 | 4.3 | 5.5 | Within |

## XP rate

### Fishing: XP per hour against the legacy loop (0.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per bite | Legacy actions per bite | Legacy XP per hour | New XP per bite | New actions per bite | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0-10 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 4432 | 5.6 | 309637 | 1.00 | - | Passed |
| 11-20 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 4350 | 5.4 | 309941 | 1.00 | - | Passed |
| 21-30 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 4200 | 5.2 | 309680 | 1.00 | - | Passed |
| 31-40 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 4169 | 5.2 | 309045 | 1.00 | - | Passed |
| 41-52 | best rod for the rank, rank = fish level (at most 50) | 617 | 0.0 | 317473 | 4185 | 5.0 | 319258 | 1.01 | - | Passed |

## Evidence: Angler perk magnitudes

### Angler perk magnitudes: the adaptive player's catch rate at ranks 20, 35, 50, averaged over the ranks (1000 fights per case)

| Perk | Landed, fish at the rank's level | Landed, fish 3 levels above the rank | Added points, mean |
| --- | --- | --- | --- |
| No perks | 67.8% | 36.8% | - |
| Strong Line I (+1 Tension cap) | 67.9% | 42.5% | 2.9 |
| Strong Line II (+2 Tension cap) | 70.0% | 46.7% | 6.0 |
| Strong Line III (+3 Tension cap) | 72.4% | 49.0% | 8.4 |
| Hard Set I (Set Hook drains 2 more Stamina) | 70.0% | 38.9% | 2.2 |
| Hard Set II (Set Hook drains 3 more Stamina) | 70.2% | 40.2% | 2.9 |
| Hard Set III (Set Hook drains 4 more Stamina) | 76.1% | 43.9% | 7.7 |
| Steady Hold I (Hold drains 1 more Stamina) | 72.4% | 39.0% | 3.4 |
| Steady Hold II (Hold drains 2 more Stamina) | 78.5% | 45.6% | 9.7 |
| Fish Sense (sees the next two moves) | 83.2% | 48.5% | 13.5 |
| Patient Angler (Give Line lets out 2 less Distance) | 76.2% | 40.2% | 5.9 |
| Aggressive Angler (Reel pulls 2 more Distance and adds 2 more Tension) | 70.6% | 46.4% | 6.2 |
| Whole line below the capstone, Patient Angler | 98.9% | 77.2% | 35.7 |
| Whole line below the capstone, Aggressive Angler | 95.2% | 66.8% | 28.7 |

## Evidence: style balance

### Style balance: Patient Angler against Aggressive Angler (ties within 2.0 points count half to each)

| Rank | Fish levels above the rank | Patient Angler | Aggressive Angler | Better |
| --- | --- | --- | --- | --- |
| 20 | +4 | 24% | 35% | Aggressive |
| 20 | +3 | 30% | 43% | Aggressive |
| 20 | +2 | 52% | 54% | Tie |
| 20 | +1 | 59% | 58% | Tie |
| 20 | 0 | 71% | 68% | Patient |
| 20 | -1 | 76% | 74% | Patient |
| 20 | -2 | 88% | 85% | Patient |
| 35 | +4 | 24% | 40% | Aggressive |
| 35 | +3 | 47% | 48% | Tie |
| 35 | +2 | 58% | 56% | Patient |
| 35 | +1 | 72% | 62% | Patient |
| 35 | 0 | 77% | 71% | Patient |
| 35 | -1 | 84% | 80% | Patient |
| 35 | -2 | 92% | 88% | Patient |
| 50 | +4 | 36% | 41% | Aggressive |
| 50 | +3 | 44% | 48% | Aggressive |
| 50 | +2 | 61% | 56% | Patient |
| 50 | +1 | 67% | 62% | Patient |
| 50 | 0 | 80% | 73% | Patient |
| 50 | -1 | 82% | 79% | Patient |
| 50 | -2 | 92% | 89% | Patient |

## Evidence: rare fish and Trophy Catch

### The six special fish (level 52) at Agriculture rank 50 with a tier 4 rod, against today's catch chance (1000 fights per case)

| Case | Today, Willpower 8 | Today, Willpower 10 | Today, Willpower 12 | Today, Willpower 14 | Today, Willpower 16 | Today, Willpower 18 | Today, Willpower 20 | Fight, adaptive | Fight, rule of thumb | Adaptive turns |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Today (level 52 fish, rank 50) | 36% | 40% | 44% | 48% | 52% | 56% | 60% | - | - | - |
| Fight, a common fish of level 52 | - | - | - | - | - | - | - | 59% | 41% | 4.7 |
| Fight, a rare fish (chained moves) | - | - | - | - | - | - | - | 52% | 28% | 4.9 |
| Fight, a rare fish with Trophy Catch (-2 rungs) and no other perk | - | - | - | - | - | - | - | 52% | 26% | 4.8 |
| Fight, a rare fish with Trophy Catch (-4 rungs) and no other perk | - | - | - | - | - | - | - | 54% | 27% | 4.9 |
| Fight, a rare fish with Trophy Catch (-6 rungs) and no other perk | - | - | - | - | - | - | - | 55% | 35% | 5.3 |
| Fight, a rare fish with Trophy Catch (-8 rungs) and no other perk | - | - | - | - | - | - | - | 55% | 31% | 5.3 |
| Fight, a rare fish with Trophy Catch (-12 rungs) and no other perk | - | - | - | - | - | - | - | 65% | 45% | 5.6 |
| Fight, a rare fish with the whole line below the capstone (Patient Angler) | - | - | - | - | - | - | - | 97% | 44% | 5.2 |
| Fight, a rare fish with the whole line below the capstone (Aggressive Angler) | - | - | - | - | - | - | - | 91% | 45% | 4.7 |
| Fight, a rare fish with the whole line (Patient Angler) and Trophy Catch (-2 rungs) | - | - | - | - | - | - | - | 98% | 42% | 5.3 |
| Fight, a rare fish with the whole line (Patient Angler) and Trophy Catch (-4 rungs) | - | - | - | - | - | - | - | 98% | 45% | 5.3 |
| Fight, a rare fish with the whole line (Patient Angler) and Trophy Catch (-6 rungs) | - | - | - | - | - | - | - | 98% | 54% | 5.3 |
| Fight, a rare fish with the whole line (Patient Angler) and Trophy Catch (-8 rungs) | - | - | - | - | - | - | - | 99% | 54% | 5.1 |
| Fight, a rare fish with the whole line (Patient Angler) and Trophy Catch (-12 rungs) | - | - | - | - | - | - | - | 99% | 66% | 5.0 |

## Limitations

- Today's catch chance depends on the character's Willpower, which is not measured for active characters yet; the corridor between Willpower 10 and 14 stands in for it.
- The adaptive player is a bounded-depth expectimax with a generic fish heuristic. It is not an optimal player, and real players are less skilled; the rule-of-thumb player marks the other end. The study does not measure how real players play.
- The game places the six special fish at a random location and bait on every server start. The study fixes one placement (`FishingCatchModel.SpecialPlacementSeed` = 1) so the report can be reproduced; another placement moves the top band's mix and the pooled rows a little.
- The fight has no hidden chance, so a very easy fish is landed every time by a skilled player, where today's chance stops at 95%.
- The pooled table averages every pool a rod can reach with equal weight, as the legacy baseline does, and ignores the 80% chance of no bite when only the default fish is on offer. Fish per hour is not modelled: location choice, bait supply and respawn are outside the study.
- Gate 6 weighs the time a fight adds (8.0 s a turn is unmeasured) against the XP multiplier, so the multipliers are starting values: they shrink or grow with the real seconds per turn. The game keeps the pace from below in two ways: each action after the first waits the turn, and the rest of the multiplied XP is paid in proportion to how long the fight lasted (up to the full turns), so a fast player, or one who loses the fight on purpose after a move, earns about today's XP per hour and no more. The gate plays every fight at the modelled turn, so a player slower than that earns less than today per hour.
- The multiplier raises the XP of a fight that lasts the full turns about eightfold, because the fight takes several times as long as the cast it follows; the catch rate per bite stays near today's, so fish per hour falls by the same factor. Gate 6 asks for it; it is flagged here for the owner.
- Perks, food and status effects are not modelled except where an evidence table names a perk. The client was not run.
