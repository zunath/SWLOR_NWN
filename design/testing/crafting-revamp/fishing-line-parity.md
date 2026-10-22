# Crafting revamp: Quick Reel and the Line (fishing fight) against today's catch roll

- Command: `SWLOR.CraftSim fishing --runs 1000 --seed 1 --out design/testing/crafting-revamp/fishing-line-parity.md`
- Seed 1; 1000 runs per case.

## Inputs

- Today (the catch roll before the Line): a bite lands the fish at `40 + 10 x (Agriculture rank - fish level) + 2 x Willpower score` percent, kept from 0 to 95. It is played at Willpower scores 8, 10, 12, 14, 16, 18, 20 (`AttributeSweep`); the corridor Quick Reel is judged against is Willpower 10 to 14, because the median Willpower of active characters is not measured yet.
- Quick Reel (owner decision, October 2026): on a bite the angler may resolve it at once by today's formula, with Willpower fixed at 12 (`FishingQuickReel.ReferenceWillpowerScore`, the middle of the corridor and **pending the owner's attribute-median decision**), pays today's XP for the bite and nothing more, takes no fight time and spends the bait as today. It is only a choice before the first move of a fight.
- The fight: the Line engine, 1000 fights per case, each with its own dice from seed 1. No attributes are read. Fish strength comes from the fish's level against the angler's rank (`LineSetup.ForFish`); the rod tier and the rank set the Tension cap and the reel. Fighting is optional; its catch rate is bounded by Quick Reel's (owner decision; starting value): at most 15% more than Quick Reel's chance for the same fish and angler, plus 3 points (two standard errors of a 1000-fight case, and a floor for fish Quick Reel can hardly land). The adaptive player stands for the skilled fight.
- Adaptive player: depth 3, forecast samples per result 2, budget 4000 engine copies per decision, 0.5% of value lost per extra action, heuristic LineLeafHeuristic. It is a floor on skilled play. Rule-of-thumb player: reads the announced move and answers it by a fixed rule, never looking further ahead.
- Time: 7.0 s for a cast and the wait for a bite (6-8 s in the game), in both loops; 8.0 s for each turn of the fight. Both are estimates until the client run measures them. The game enforces the turn as the least time between two actions (`LineRules.TurnSeconds`, the first action free), so fast clicking cannot beat the pace the XP multipliers assume.
- XP cohort (gate 6): fish-level bands 0-10, 11-20, 21-30, 31-40 and 41-52; the angler's rank equals the fish's level (at most 50) with the best rod tier that rank allows; every (rod, bait) table of every location that rod reaches counts equally, half by day and half by night, so both loops draw the same fish. XP is per bite.
- XP: today's XP for the fish against the rank is paid at the bite, caught or not, as today; the rest of the multiplied XP is paid when the fight ends on its own (landed, snapped or escaped), in proportion to how long the fight lasted up to 5 turns of 8.0 s, and never for a fight the angler abandons, so neither skipping the fight nor losing it on purpose after one move earns more per hour than today. The multiplied XP is today's times a multiplier by the fish's level band, as the game server applies it (`CraftMinigameXp`): 0-10 8.55x, 11-20 8.35x, 21-30 7.75x, 31-40 8.05x, 41-52 7.5x. Each is the least multiple of 0.05 that keeps gate 6 passing in its band at this report's runs and seed; 1.0 in every band restores today's XP per bite.
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
  - `RungAtRank` = 54
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
  - `XpMultiplierLevels0To10` = 8.55
  - `XpMultiplierLevels11To20` = 8.35
  - `XpMultiplierLevels21To30` = 7.75
  - `XpMultiplierLevels31To40` = 8.05
  - `XpMultiplierLevels41Up` = 7.5

## Verdicts

| Gate | Verdict | Summary |
| --- | --- | --- |
| Economy parity | Passed | Quick Reel rolls today's formula at Willpower 12 (pending the owner's attribute-median decision) and stays inside today's Willpower 10-14 corridor at every rank above the fish's level and at every rank over the real pools. Quick Reel lands 1.00x to 1.00x of today's fish per hour at Willpower 12 (0.94x to 1.07x across the Willpower 10-14 corridor, by band) and pays 1.00x to 1.00x of today's XP per hour. |
| Fight bound | Passed | The skilled (adaptive) fight lands at most 15% above Quick Reel's chance plus 3 points for the same fish: with no perks at every rank above the fish's level and over the real pools, with the whole Angler line in either style, and on the special rare fish with and without Trophy Catch (furthest below the bound by 0.1 points in the tables). |
| Length | Passed | The adaptive player's fights last 3-6 actions (fishing fight) in every row. |
| XP rate | Passed | Fishing: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.00x in 41-52 / best rod for the rank, rank = fish level (at most 50)). |
| Evidence: XP rate at the fastest allowed pace | NotApplicable | Not a gate. A player who takes the free first action at once and then waits the turn each time earns 1.02x to 1.04x of today's XP per hour across the bands; the multipliers above are calibrated for the typical player, who reads the first move for a turn like the others. |
| Evidence: Angler perk magnitudes | NotApplicable | Not a gate. What each proposed Angler purchase adds to the adaptive player's catch rate, and whether it stays within the fight bound; the draft quotes it. The row marked Rejected is the first draft's whole line, kept to show how far over the bound it went; each of its purchases alone is within it. |
| Evidence: style balance | NotApplicable | Not a gate. Patient Angler is the better style on 48% of the cases and Aggressive Angler on 52%; the mean gap is -0.4 points (Patient less Aggressive). |

## Economy parity

### Landed per bite by rank above the fish's level: today's catch roll by Willpower and Quick Reel (corridor Willpower 10-14)

| Rank - fish level | Today, Willpower 8 | Today, Willpower 10 | Today, Willpower 12 | Today, Willpower 14 | Today, Willpower 16 | Today, Willpower 18 | Today, Willpower 20 | Quick Reel (Willpower 12) | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| -8 | 0% | 0% | 0% | 0% | 0% | 0% | 0% | 0% | Inside |
| -7 | 0% | 0% | 0% | 0% | 2% | 6% | 10% | 0% | Inside |
| -6 | 0% | 0% | 4% | 8% | 12% | 16% | 20% | 4% | Inside |
| -5 | 6% | 10% | 14% | 18% | 22% | 26% | 30% | 14% | Inside |
| -4 | 16% | 20% | 24% | 28% | 32% | 36% | 40% | 24% | Inside |
| -3 | 26% | 30% | 34% | 38% | 42% | 46% | 50% | 34% | Inside |
| -2 | 36% | 40% | 44% | 48% | 52% | 56% | 60% | 44% | Inside |
| -1 | 46% | 50% | 54% | 58% | 62% | 66% | 70% | 54% | Inside |
| 0 | 56% | 60% | 64% | 68% | 72% | 76% | 80% | 64% | Inside |
| +1 | 66% | 70% | 74% | 78% | 82% | 86% | 90% | 74% | Inside |
| +2 | 76% | 80% | 84% | 88% | 92% | 95% | 95% | 84% | Inside |
| +3 | 86% | 90% | 94% | 95% | 95% | 95% | 95% | 94% | Inside |
| +4 | 95% | 95% | 95% | 95% | 95% | 95% | 95% | 95% | Inside |
| +5 | 95% | 95% | 95% | 95% | 95% | 95% | 95% | 95% | Inside |
| +6 | 95% | 95% | 95% | 95% | 95% | 95% | 95% | 95% | Inside |

### Landed per bite by Agriculture rank, over every rod and bait pool the angler can use (each location, rod and bait, daytime and nighttime counted separately; pools weighted equally, fish within a pool by frequency; the pool average mixes pools no single angler fishes, so it compares rules and is not a rank's own experience)

| Agriculture rank | Today, Willpower 10 | Today, Willpower 14 | Today, Willpower 8-20 | Quick Reel (Willpower 12) | Pools | Verdict |
| --- | --- | --- | --- | --- | --- | --- |
| 0 | 30% | 36% | 27-47% | 33% | 194 | Inside |
| 5 | 71% | 76% | 68-82% | 73% | 194 | Inside |
| 10 | 62% | 64% | 61-68% | 63% | 313 | Inside |
| 15 | 79% | 82% | 78-86% | 81% | 313 | Inside |
| 20 | 73% | 76% | 72-79% | 75% | 445 | Inside |
| 25 | 87% | 89% | 87-90% | 88% | 445 | Inside |
| 30 | 79% | 81% | 78-83% | 80% | 571 | Inside |
| 35 | 89% | 91% | 89-92% | 90% | 571 | Inside |
| 40 | 81% | 82% | 80-83% | 81% | 699 | Inside |
| 45 | 87% | 88% | 86-89% | 87% | 699 | Inside |
| 50 | 92% | 93% | 92-94% | 93% | 699 | Inside |

### Fish and XP per hour by fish-level band, rank equal to the fish's level (at most 50) with the best rod for it: today's catch roll, Quick Reel and the fight (7 s for a cast and the wait, 8 s for each turn of the fight; unmeasured estimates)

| Fish-level band | Today, fish per hour, Willpower 10 | Today, fish per hour, Willpower 12 | Today, fish per hour, Willpower 14 | Quick Reel, fish per hour | Quick Reel against today (Willpower 12) | Quick Reel against today (Willpower 10-14) | Fight, fish per hour | Fight against today (Willpower 12) | Today, XP per hour | Quick Reel, XP per hour | Fight, XP per hour | Fight XP against today |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0-10 | 309 | 329 | 350 | 329 | 1.00x | 1.07x-0.94x | 20 | 0.06x | 308571 | 308571 | 309519 | 1.00x |
| 11-20 | 309 | 329 | 350 | 329 | 1.00x | 1.07x-0.94x | 21 | 0.06x | 308571 | 308571 | 310234 | 1.01x |
| 21-30 | 309 | 329 | 350 | 329 | 1.00x | 1.07x-0.94x | 34 | 0.10x | 308571 | 308571 | 309489 | 1.00x |
| 31-40 | 309 | 329 | 350 | 329 | 1.00x | 1.07x-0.94x | 37 | 0.11x | 308571 | 308571 | 309578 | 1.00x |
| 41-52 | 297 | 317 | 338 | 317 | 1.00x | 1.07x-0.94x | 40 | 0.13x | 317473 | 317473 | 318167 | 1.00x |

## Fight bound

### No perks: landed per bite by rank above the fish's level, at ranks 10, 20, 30, 40, 50 with the best rod each can use (1000 fights per fish). The bound is Quick Reel's chance plus 15% plus 3 points

| Rank - fish level | Quick Reel (Willpower 12) | Bound | Fight, adaptive | Adaptive, by rank | Fight, rule of thumb | Adaptive turns | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| -8 | 0% | 3.0% | 0% | 0-1% | 1% | 3.6 | Within |
| -7 | 0% | 3.0% | 1% | 1-1% | 1% | 3.9 | Within |
| -6 | 4% | 7.6% | 2% | 1-3% | 3% | 4.1 | Within |
| -5 | 14% | 19.1% | 3% | 2-5% | 3% | 4.3 | Within |
| -4 | 24% | 30.6% | 8% | 7-12% | 7% | 4.7 | Within |
| -3 | 34% | 42.1% | 12% | 7-15% | 9% | 4.8 | Within |
| -2 | 44% | 53.6% | 20% | 10-31% | 11% | 5.0 | Within |
| -1 | 54% | 65.1% | 33% | 24-41% | 19% | 5.0 | Within |
| 0 | 64% | 76.6% | 45% | 32-53% | 26% | 5.0 | Within |
| +1 | 74% | 88.1% | 57% | 51-61% | 36% | 5.1 | Within |
| +2 | 84% | 99.6% | 63% | 58-68% | 43% | 5.2 | Within |
| +3 | 94% | 111.1% | 70% | 64-75% | 53% | 5.4 | Within |
| +4 | 95% | 112.2% | 80% | 70-86% | 65% | 5.5 | Within |
| +5 | 95% | 112.2% | 87% | 80-94% | 72% | 5.5 | Within |
| +6 | 95% | 112.2% | 95% | 91-98% | 82% | 4.7 | Within |

### No perks: landed per bite by Agriculture rank, over every rod and bait pool the angler can use (pools weighted equally, fish within a pool by frequency)

| Agriculture rank | Quick Reel (Willpower 12) | Bound | Fight, adaptive | Adaptive turns | Pools | Verdict |
| --- | --- | --- | --- | --- | --- | --- |
| 0 | 33% | 41.1% | 14% | 4.8 | 194 | Within |
| 5 | 73% | 87.3% | 50% | 5.7 | 194 | Within |
| 10 | 63% | 75.4% | 56% | 4.5 | 313 | Within |
| 15 | 81% | 95.9% | 73% | 3.9 | 313 | Within |
| 20 | 75% | 88.8% | 68% | 3.9 | 445 | Within |
| 25 | 88% | 104.2% | 84% | 3.7 | 445 | Within |
| 30 | 80% | 94.6% | 78% | 3.6 | 571 | Within |
| 35 | 90% | 106.5% | 91% | 3.6 | 571 | Within |
| 40 | 81% | 96.3% | 82% | 3.4 | 699 | Within |
| 45 | 87% | 103.5% | 89% | 3.4 | 699 | Within |
| 50 | 93% | 109.7% | 96% | 3.2 | 699 | Within |

### The Angler line below the capstone (Strong Line II, Hard Set II, Fish Sense and a style; the perk draft): landed per bite by rank above the fish's level, at ranks 10, 20, 30, 40, 50 (1000 fights per fish)

| Rank - fish level | Quick Reel (Willpower 12) | Bound | No perks | Whole line, Patient Angler | Whole line, Aggressive Angler | Verdict |
| --- | --- | --- | --- | --- | --- | --- |
| -8 | 0% | 3.0% | 0% | 1% | 1% | Within |
| -7 | 0% | 3.0% | 1% | 3% | 3% | Within |
| -6 | 4% | 7.6% | 2% | 6% | 6% | Within |
| -5 | 14% | 19.1% | 3% | 11% | 11% | Within |
| -4 | 24% | 30.6% | 8% | 22% | 23% | Within |
| -3 | 34% | 42.1% | 12% | 34% | 35% | Within |
| -2 | 44% | 53.6% | 20% | 46% | 46% | Within |
| -1 | 54% | 65.1% | 33% | 62% | 59% | Within |
| 0 | 64% | 76.6% | 45% | 70% | 67% | Within |
| +1 | 74% | 88.1% | 57% | 83% | 81% | Within |
| +2 | 84% | 99.6% | 63% | 87% | 85% | Within |
| +3 | 94% | 111.1% | 70% | 92% | 90% | Within |
| +4 | 95% | 112.2% | 80% | 99% | 99% | Within |
| +5 | 95% | 112.2% | 87% | 100% | 100% | Within |
| +6 | 95% | 112.2% | 95% | 100% | 100% | Within |

### The six special fish (level 52) at Agriculture rank 50 with a tier 4 rod, against Quick Reel's chance and the bound (1000 fights per case). A rare fish ignores the line; only Trophy Catch helps against it

| Case | Today, Willpower 10 | Today, Willpower 14 | Quick Reel (Willpower 12) | Bound | Fight, adaptive | Fight, rule of thumb | Adaptive turns | Within the bound |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Today (level 52 fish, rank 50) | 40% | 48% | 44% | 53.6% | - | - | - | - |
| Fight, a common fish of level 52 (for comparison) | - | - | - | - | 31% | 13% | 4.5 | Not judged |
| Fight, a rare fish (chained moves) | - | - | - | - | 30% | 11% | 4.6 | Yes |
| Fight, a rare fish with Trophy Catch (-4 rungs) and no other perk | - | - | - | - | 38% | 14% | 4.9 | Not judged |
| Fight, a rare fish with Trophy Catch (-8 rungs) and no other perk | - | - | - | - | 42% | 18% | 4.7 | Not judged |
| Fight, a rare fish with Trophy Catch (-10 rungs) and no other perk | - | - | - | - | 42% | 21% | 4.7 | Not judged |
| Fight, a rare fish with Trophy Catch (-12 rungs) and no other perk | - | - | - | - | 49% | 28% | 4.9 | Yes |
| Fight, a rare fish with Trophy Catch (-14 rungs) and no other perk | - | - | - | - | 52% | 30% | 4.9 | Not judged |
| Fight, a rare fish with the whole line below the capstone (Patient Angler) | - | - | - | - | 28% | 10% | 4.7 | Yes |
| Fight, a rare fish with the whole line below the capstone (Aggressive Angler) | - | - | - | - | 28% | 8% | 4.6 | Yes |
| Fight, a rare fish with the whole line (Patient Angler) and Trophy Catch (-12 rungs) | - | - | - | - | 48% | 30% | 4.9 | Yes |
| Fight, a rare fish with the whole line (Aggressive Angler) and Trophy Catch (-12 rungs) | - | - | - | - | 49% | 28% | 5.0 | Yes |

## Length

### Fight length by rank above the fish's level (target 3-6 actions (fishing fight); mean over ranks 10, 20, 30, 40, 50)

| Rank - fish level | Adaptive turns | Rule-of-thumb turns | Verdict |
| --- | --- | --- | --- |
| -6 | 4.1 | 2.9 | Within |
| -5 | 4.3 | 3.0 | Within |
| -4 | 4.7 | 3.1 | Within |
| -3 | 4.8 | 3.2 | Within |
| -2 | 5.0 | 3.4 | Within |
| -1 | 5.0 | 4.1 | Within |
| 0 | 5.0 | 4.7 | Within |
| +1 | 5.1 | 5.2 | Within |
| +2 | 5.2 | 5.6 | Within |
| +3 | 5.4 | 5.4 | Within |
| +4 | 5.5 | 5.3 | Within |

## XP rate

### Fishing: XP per hour against the legacy loop (0.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per bite | Legacy actions per bite | Legacy XP per hour | New XP per bite | New actions per bite | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0-10 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 4557 | 5.7 | 309519 | 1.00 | - | Passed |
| 11-20 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 4387 | 5.5 | 310234 | 1.01 | - | Passed |
| 21-30 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 3726 | 4.5 | 309489 | 1.00 | - | Passed |
| 31-40 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 4086 | 5.1 | 309578 | 1.00 | - | Passed |
| 41-52 | best rod for the rank, rank = fish level (at most 50) | 617 | 0.0 | 317473 | 3821 | 4.5 | 318167 | 1.00 | - | Passed |

## Evidence: XP rate at the fastest allowed pace

### Fishing at the fastest allowed pace: XP per hour against the legacy loop (0.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per bite | Legacy actions per bite | Legacy XP per hour | New XP per bite | New actions per bite | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0-10 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 4014 | 4.7 | 321135 | 1.04 | - | Passed |
| 11-20 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 3838 | 4.5 | 322061 | 1.04 | - | Passed |
| 21-30 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 3100 | 3.5 | 315730 | 1.02 | - | Passed |
| 31-40 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 3491 | 4.1 | 317999 | 1.03 | - | Passed |
| 41-52 | best rod for the rank, rank = fish level (at most 50) | 617 | 0.0 | 317473 | 3179 | 3.5 | 324816 | 1.02 | - | Passed |

## Evidence: Angler perk magnitudes

### Angler perk magnitudes: the adaptive player's catch rate at ranks 20, 35, 50, averaged over the ranks (1000 fights per case). The bound is Quick Reel's chance plus 15% plus 3 points: 76.6% at the rank's level and 42.1% for fish 3 levels above

| Perk | Landed, fish at the rank's level | Landed, fish 3 levels above the rank | Added points, mean | Within the bound |
| --- | --- | --- | --- | --- |
| No perks | 47.4% | 12.1% | - | Yes |
| Strong Line I (+1 Tension cap) | 48.2% | 17.1% | 3.0 | Yes |
| Strong Line II (+2 Tension cap) | 52.4% | 22.1% | 7.5 | Yes |
| Hard Set I (Set Hook drains 1 more Stamina) | 49.9% | 14.1% | 2.3 | Yes |
| Hard Set II (Set Hook drains 2 more Stamina) | 49.1% | 13.6% | 1.6 | Yes |
| Fish Sense (sees the next two moves) | 59.2% | 19.4% | 9.6 | Yes |
| Patient Angler (Give Line lets out 1 less Distance) | 49.9% | 13.5% | 2.0 | Yes |
| Aggressive Angler (Reel pulls the fish in 1 more Distance) | 48.9% | 14.1% | 1.8 | Yes |
| Whole line below the capstone, Patient Angler | 69.8% | 33.7% | 22.0 | Yes |
| Whole line below the capstone, Aggressive Angler | 68.0% | 35.9% | 22.2 | Yes |
| Rejected: the first draft's whole line (Strong Line III, Hard Set III, Steady Hold, Fish Sense, Patient Angler -2) | 81.5% | 58.3% | 40.2 | No: over by 16.2 points |

## Evidence: style balance

### Style balance: Patient Angler against Aggressive Angler (ties within 2.0 points count half to each)

| Rank | Fish levels above the rank | Patient Angler | Aggressive Angler | Better |
| --- | --- | --- | --- | --- |
| 20 | +4 | 7% | 8% | Tie |
| 20 | +3 | 10% | 10% | Tie |
| 20 | +2 | 18% | 19% | Tie |
| 20 | +1 | 28% | 27% | Tie |
| 20 | 0 | 46% | 44% | Tie |
| 20 | -1 | 53% | 53% | Tie |
| 20 | -2 | 63% | 64% | Tie |
| 35 | +4 | 9% | 11% | Tie |
| 35 | +3 | 16% | 17% | Tie |
| 35 | +2 | 23% | 23% | Tie |
| 35 | +1 | 43% | 42% | Tie |
| 35 | 0 | 52% | 49% | Patient |
| 35 | -1 | 62% | 65% | Aggressive |
| 35 | -2 | 69% | 67% | Patient |
| 50 | +4 | 11% | 12% | Tie |
| 50 | +3 | 14% | 15% | Tie |
| 50 | +2 | 32% | 32% | Tie |
| 50 | +1 | 39% | 42% | Aggressive |
| 50 | 0 | 52% | 53% | Tie |
| 50 | -1 | 63% | 62% | Tie |
| 50 | -2 | 64% | 68% | Aggressive |

## Limitations

- Today's catch chance depends on the character's Willpower, which is not measured for active characters yet; the corridor between Willpower 10 and 14 stands in for it.
- The adaptive player is a bounded-depth expectimax with a generic fish heuristic. It is not an optimal player, and real players are less skilled; the rule-of-thumb player marks the other end. The study does not measure how real players play.
- The game places the six special fish at a random location and bait on every server start. The study fixes one placement (`FishingCatchModel.SpecialPlacementSeed` = 1) so the report can be reproduced; another placement moves the top band's mix and the pooled rows a little.
- The fight has no hidden chance, so a very easy fish is landed every time by a skilled player, where today's chance stops at 95%.
- The pooled table averages every pool a rod can reach with equal weight, as the legacy baseline does, and ignores the 80% chance of no bite when only the default fish is on offer. Fish per hour is not modelled: location choice, bait supply and respawn are outside the study.
- Gate 6 weighs the time a fight adds (8.0 s a turn is unmeasured) against the XP multiplier, so the multipliers are starting values: they shrink or grow with the real seconds per turn. The game keeps the pace from below in two ways: each action after the first waits the turn, and the rest of the multiplied XP is paid in proportion to how long the fight lasted (up to the full turns), so a fast player, or one who loses the fight on purpose after a move, earns about today's XP per hour and no more; the evidence table for the fastest allowed pace (first action at once) shows it a few percent above today's. The gate plays every fight at the modelled turn, so a player slower than that earns less than today per hour.
- The multiplier raises the XP of a fight that lasts the full turns about eightfold, because the fight takes several times as long as the cast it follows; a fighter lands fewer fish per hour than a quick reeler by the same factor. Gate 6 asks for it; it is flagged here for the owner. Quick Reel pays today's XP, so it needs no multiplier.
- The bound leaves the unperked fight below Quick Reel: `LineRules.RungAtRank` (54) is set so that, with no perks, a skilled fight lands fewer fish than Quick Reel at the fish's own level and the Angler line can lift it up to the bound and no further. The fight is the XP choice and Quick Reel the fish choice until the line is bought. Lowering `RungAtRank` strengthens the unperked fight but leaves the line no room under the bound.
- The Angler line is cut to what the bound allows: Strong Line and Hard Set to two steps each, Fish Sense, a style and Trophy Catch are seven of the plan's ten purchases; the other three need effects that do not raise the catch rate, or a looser bound. A rare fish ignores the rest of the line, so only Trophy Catch moves it.
- Perks, food and status effects are not modelled except where an evidence table names a perk. The client was not run.
