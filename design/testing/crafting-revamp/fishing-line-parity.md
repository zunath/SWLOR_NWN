# Crafting revamp: Quick Reel and the Line (fishing fight) against today's catch roll

- Command: `SWLOR.CraftSim fishing --runs 1000 --seed 1 --repo . --out design/testing/crafting-revamp/fishing-line-parity.md`
- Seed 1; 1000 runs per case.

## Inputs

- Today (the catch roll before the Line): a bite lands the fish at `40 + 10 x (Agriculture rank - fish level) + 2 x Willpower score` percent, kept from 0 to 95. It is played at Willpower scores 8, 10, 12, 14, 16, 18, 20 (`AttributeSweep`); the corridor Quick Reel is judged against is Willpower 10 to 14, because the median Willpower of active characters is not measured yet.
- Quick Reel (owner decision, October 2026): on a bite the angler may resolve it at once by today's formula, with Willpower fixed at 10 (`FishingQuickReel.ReferenceWillpowerScore`: a character with no attribute investment, the low end of the corridor; characters with more Willpower land less than today with it), pays today's XP for the bite and nothing more, takes no fight time and spends the bait as today. It is only a choice before the first move of a fight.
- The fight: the Line engine, 1000 fights per case, each with its own dice from seed 1. No attributes are read. Fish strength comes from the fish's level against the angler's rank (`LineSetup.ForFish`); the rod tier and the rank set the Tension cap and the reel. Fighting is optional; an unperked skilled fight's catch rate is bounded by Quick Reel's (owner decision; starting value): at most 15% more than Quick Reel's chance for the same fish and angler, plus 3 points (two standard errors of a 1000-fight case, and a floor for fish Quick Reel can hardly land). The Angler line may lift the fight to about 90% of the fish at the angler's own level (owner decision, October 2026), with the fight perks at half strength on a rare fish and Trophy Catch the only full-strength rare-fish perk; Quick Reel is unaffected by perks. The adaptive player stands for the skilled fight.
- Adaptive player: depth 3, forecast samples per result 2, budget 4000 engine copies per decision, 0.5% of value lost per extra action, heuristic LineLeafHeuristic. It is a floor on skilled play. Rule-of-thumb player: reads the announced move and answers it by a fixed rule, never looking further ahead.
- Time: 7.0 s for a cast and the wait for a bite (6-8 s in the game), in both loops; 8.0 s for each turn of the fight. Both are estimates until the client run measures them. The game enforces the turn as the least time between two actions (`LineRules.TurnSeconds`, the first action free), so fast clicking cannot beat the pace the XP multipliers assume.
- XP cohort (gate 6): fish-level bands 0-10, 11-20, 21-30, 31-40 and 41-52; the angler's rank equals the fish's level (at most 50) with the best rod tier that rank allows; every (rod, bait) table of every location that rod reaches counts equally, half by day and half by night, so both loops draw the same fish. XP is per bite.
- XP: today's XP for the fish against the rank is paid at the bite, caught or not, as today; the rest of the multiplied XP is paid when the fight ends on its own (landed, snapped or escaped), in proportion to how long the fight lasted up to 5 turns of 8.0 s, and never for a fight the angler abandons, so neither skipping the fight nor losing it on purpose after one move earns more per hour than today. The multiplied XP is today's times a multiplier by the fish's level band, as the game server applies it (`CraftMinigameXp`): 0-10 8.55x, 11-20 8.65x, 21-30 8.35x, 31-40 8.65x, 41-52 8x. Each is the least multiple of 0.05 that keeps gate 6 passing in its band at this report's runs and seed; 1.0 in every band restores today's XP per bite.
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
  - `RungAtRank` = 44
  - `RungsPerLevel` = 5
  - `RungsPerLevelAbove` = 7
  - `HardLevelsFrom` = 3
  - `RungsPerHardLevel` = 9
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
  - `FishSenseTurns` = 1
  - `XpFullTurns` = 5
  - `XpMultiplierLevels0To10` = 8.55
  - `XpMultiplierLevels11To20` = 8.65
  - `XpMultiplierLevels21To30` = 8.35
  - `XpMultiplierLevels31To40` = 8.65
  - `XpMultiplierLevels41Up` = 8

## Verdicts

| Gate | Verdict | Summary |
| --- | --- | --- |
| Economy parity | Passed | Quick Reel rolls today's formula at Willpower 10 (a character with no attribute investment; owner decision) and stays inside today's Willpower 10-14 corridor at every rank above the fish's level and at every rank over the real pools. Quick Reel lands 1.00x to 1.00x of today's fish per hour at Willpower 10 (0.88x to 1.00x across the Willpower 10-14 corridor, by band) and pays 1.00x to 1.00x of today's XP per hour. |
| Fight bound | Passed | With no perks the skilled (adaptive) fight lands at most 15% above Quick Reel's chance plus 3 points for the same fish, at every rank above the fish's level and over the real pools (furthest below the bound by 2.4 points in the tables). The whole Angler line, in either style, lands about 90% of the fish at the angler's own level (within 5 points) at rank 50, and the special rare fish stay within the bound with and without Trophy Catch. |
| Length | Passed | The adaptive player's fights last 3-6 actions (fishing fight) in every row. |
| XP rate | Passed | Fishing: XP per hour is at least 1.00x the legacy rate in every case (lowest 1.00x in 0-10 / best rod for the rank, rank = fish level (at most 50)). |
| Evidence: XP rate at the fastest allowed pace | NotApplicable | Not a gate. A player who takes the free first action at once and then waits the turn each time earns 1.03x to 1.08x of today's XP per hour across the bands; the multipliers above are calibrated for the typical player, who reads the first move for a turn like the others. |
| Length (Patient Angler (full line)) | Passed | The adaptive player's fights last 3-6 actions (fishing fight) in every row. |
| XP rate (Patient Angler (full line)) | Passed | Fishing (Patient Angler (full line)): XP per hour is at least 1.00x the legacy rate in every case (lowest 1.00x in 31-40 / best rod for the rank, rank = fish level (at most 50)). |
| Length (Aggressive Angler (full line)) | Passed | The adaptive player's fights last 3-6 actions (fishing fight) in every row. |
| XP rate (Aggressive Angler (full line)) | Passed | Fishing (Aggressive Angler (full line)): XP per hour is at least 1.00x the legacy rate in every case (lowest 1.01x in 11-20 / best rod for the rank, rank = fish level (at most 50)). |
| Evidence: Angler perk magnitudes | NotApplicable | Not a gate. What each purchase of the Angler line adds to the adaptive player's catch rate on its own, and the line's catch rate at the fish's own level at every rank; the perk-line evidence file quotes it. |
| Evidence: style balance | NotApplicable | Not a gate. With the whole line, Patient Angler is the better style on 52% of the cases and Aggressive Angler on 48%; the mean gap is 0.8 points (Patient less Aggressive). |

## Economy parity

### Landed per bite by rank above the fish's level: today's catch roll by Willpower and Quick Reel (corridor Willpower 10-14)

| Rank - fish level | Today, Willpower 8 | Today, Willpower 10 | Today, Willpower 12 | Today, Willpower 14 | Today, Willpower 16 | Today, Willpower 18 | Today, Willpower 20 | Quick Reel (Willpower 10) | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| -8 | 0% | 0% | 0% | 0% | 0% | 0% | 0% | 0% | Inside |
| -7 | 0% | 0% | 0% | 0% | 2% | 6% | 10% | 0% | Inside |
| -6 | 0% | 0% | 4% | 8% | 12% | 16% | 20% | 0% | Inside |
| -5 | 6% | 10% | 14% | 18% | 22% | 26% | 30% | 10% | Inside |
| -4 | 16% | 20% | 24% | 28% | 32% | 36% | 40% | 20% | Inside |
| -3 | 26% | 30% | 34% | 38% | 42% | 46% | 50% | 30% | Inside |
| -2 | 36% | 40% | 44% | 48% | 52% | 56% | 60% | 40% | Inside |
| -1 | 46% | 50% | 54% | 58% | 62% | 66% | 70% | 50% | Inside |
| 0 | 56% | 60% | 64% | 68% | 72% | 76% | 80% | 60% | Inside |
| +1 | 66% | 70% | 74% | 78% | 82% | 86% | 90% | 70% | Inside |
| +2 | 76% | 80% | 84% | 88% | 92% | 95% | 95% | 80% | Inside |
| +3 | 86% | 90% | 94% | 95% | 95% | 95% | 95% | 90% | Inside |
| +4 | 95% | 95% | 95% | 95% | 95% | 95% | 95% | 95% | Inside |
| +5 | 95% | 95% | 95% | 95% | 95% | 95% | 95% | 95% | Inside |
| +6 | 95% | 95% | 95% | 95% | 95% | 95% | 95% | 95% | Inside |

### Landed per bite by Agriculture rank, over every rod and bait pool the angler can use (each location, rod and bait, daytime and nighttime counted separately; pools weighted equally, fish within a pool by frequency; the pool average mixes pools no single angler fishes, so it compares rules and is not a rank's own experience)

| Agriculture rank | Today, Willpower 10 | Today, Willpower 14 | Today, Willpower 8-20 | Quick Reel (Willpower 10) | Pools | Verdict |
| --- | --- | --- | --- | --- | --- | --- |
| 0 | 30% | 36% | 27-47% | 30% | 194 | Inside |
| 5 | 71% | 76% | 68-82% | 71% | 194 | Inside |
| 10 | 62% | 64% | 61-68% | 62% | 313 | Inside |
| 15 | 79% | 82% | 78-86% | 79% | 313 | Inside |
| 20 | 73% | 76% | 72-79% | 73% | 445 | Inside |
| 25 | 87% | 89% | 87-90% | 87% | 445 | Inside |
| 30 | 79% | 81% | 78-83% | 79% | 571 | Inside |
| 35 | 89% | 91% | 89-92% | 89% | 571 | Inside |
| 40 | 81% | 82% | 80-83% | 81% | 699 | Inside |
| 45 | 87% | 88% | 86-89% | 87% | 699 | Inside |
| 50 | 92% | 93% | 92-94% | 92% | 699 | Inside |

### Fish and XP per hour by fish-level band, rank equal to the fish's level (at most 50) with the best rod for it: today's catch roll, Quick Reel and the fight (7 s for a cast and the wait, 8 s for each turn of the fight; unmeasured estimates)

| Fish-level band | Today, fish per hour, Willpower 10 | Today, fish per hour, Willpower 12 | Today, fish per hour, Willpower 14 | Quick Reel, fish per hour | Quick Reel against today (Willpower 10) | Quick Reel against today (Willpower 10-14) | Fight, fish per hour | Fight against today (Willpower 10) | Today, XP per hour | Quick Reel, XP per hour | Fight, XP per hour | Fight XP against today |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0-10 | 309 | 329 | 350 | 309 | 1.00x | 1.00x-0.88x | 38 | 0.12x | 308571 | 308571 | 308623 | 1.00x |
| 11-20 | 309 | 329 | 350 | 309 | 1.00x | 1.00x-0.88x | 38 | 0.12x | 308571 | 308571 | 314241 | 1.02x |
| 21-30 | 309 | 329 | 350 | 309 | 1.00x | 1.00x-0.88x | 44 | 0.14x | 308571 | 308571 | 322794 | 1.05x |
| 31-40 | 309 | 329 | 350 | 309 | 1.00x | 1.00x-0.88x | 49 | 0.16x | 308571 | 308571 | 326487 | 1.06x |
| 41-52 | 297 | 317 | 338 | 297 | 1.00x | 1.00x-0.88x | 47 | 0.16x | 317473 | 317473 | 318882 | 1.00x |

## Fight bound

### No perks: landed per bite by rank above the fish's level, at ranks 10, 20, 30, 40, 50 with the best rod each can use (1000 fights per fish). The bound is Quick Reel's chance plus 15% plus 3 points

| Rank - fish level | Quick Reel (Willpower 10) | Bound | Fight, adaptive | Adaptive, by rank | Fight, rule of thumb | Adaptive turns | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| -8 | 0% | 3.0% | 0% | 0-0% | 0% | 3.5 | Within |
| -7 | 0% | 3.0% | 0% | 0-0% | 0% | 3.5 | Within |
| -6 | 0% | 3.0% | 1% | 0-1% | 1% | 3.8 | Within |
| -5 | 10% | 14.5% | 3% | 2-4% | 3% | 4.2 | Within |
| -4 | 20% | 26.0% | 8% | 7-12% | 7% | 4.7 | Within |
| -3 | 30% | 37.5% | 20% | 13-33% | 11% | 4.9 | Within |
| -2 | 40% | 49.0% | 35% | 24-44% | 19% | 5.0 | Within |
| -1 | 50% | 60.5% | 50% | 38-58% | 30% | 5.0 | Within |
| 0 | 60% | 72.0% | 62% | 56-66% | 43% | 5.2 | Within |
| +1 | 70% | 83.5% | 70% | 63-74% | 54% | 5.4 | Within |
| +2 | 80% | 95.0% | 80% | 73-86% | 64% | 5.3 | Within |
| +3 | 90% | 106.5% | 87% | 80-94% | 73% | 5.4 | Within |
| +4 | 95% | 112.2% | 95% | 89-99% | 82% | 4.7 | Within |
| +5 | 95% | 112.2% | 99% | 97-100% | 89% | 3.5 | Within |
| +6 | 95% | 112.2% | 100% | 100-100% | 97% | 3.0 | Within |

### No perks: landed per bite by Agriculture rank, over every rod and bait pool the angler can use (pools weighted equally, fish within a pool by frequency)

| Agriculture rank | Quick Reel (Willpower 10) | Bound | Fight, adaptive | Adaptive turns | Pools | Verdict |
| --- | --- | --- | --- | --- | --- | --- |
| 0 | 30% | 37.4% | 23% | 4.7 | 194 | Within |
| 5 | 71% | 84.4% | 66% | 5.6 | 194 | Within |
| 10 | 62% | 74.0% | 61% | 4.1 | 313 | Within |
| 15 | 79% | 94.3% | 79% | 3.9 | 313 | Within |
| 20 | 73% | 87.2% | 74% | 3.9 | 445 | Within |
| 25 | 87% | 103.4% | 89% | 3.6 | 445 | Within |
| 30 | 79% | 93.5% | 82% | 3.6 | 571 | Within |
| 35 | 89% | 105.8% | 94% | 3.4 | 571 | Within |
| 40 | 81% | 95.8% | 85% | 3.4 | 699 | Within |
| 45 | 87% | 102.9% | 91% | 3.3 | 699 | Within |
| 50 | 92% | 109.2% | 97% | 3.2 | 699 | Within |

### The Angler line at the fish's own level, by rank: the adaptive player with no perks and with every purchase the rank allows (1000 fights per case). The owner's target for the whole line, at rank 50 where it is complete, is about 90%, judged within 5 points

| Rank | Quick Reel | No perks | Patient Angler line | Aggressive Angler line | Target | Verdict |
| --- | --- | --- | --- | --- | --- | --- |
| 10 | 60% | 56% | 66% | 66% | - | - |
| 20 | 60% | 59% | 80% | 78% | - | - |
| 30 | 60% | 65% | 86% | 83% | - | - |
| 40 | 60% | 66% | 94% | 94% | - | - |
| 50 | 60% | 64% | 92% | 91% | 85-95% | Within |

### The six special fish (level 52) at Agriculture rank 50 with a tier 4 rod, against Quick Reel's chance and the bound (1000 fights per case). A rare fish takes the fight perks at half strength, rounded down; Trophy Catch is the only perk at full strength

| Case | Today, Willpower 10 | Today, Willpower 14 | Quick Reel (Willpower 10) | Bound | Fight, adaptive | Fight, rule of thumb | Adaptive turns | Within the bound |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Today (level 52 fish, rank 50) | 40% | 48% | 40% | 49.0% | - | - | - | - |
| Fight, a common fish of level 52 (for comparison) | - | - | - | - | 41% | 22% | 4.7 | Not judged |
| Fight, a rare fish: No perks | - | - | - | - | 39% | 16% | 4.9 | Yes |
| Fight, a rare fish: Patient Angler and the line below the capstone (half strength) | - | - | - | - | 51% | 30% | 4.7 | Not judged |
| Fight, a rare fish: Aggressive Angler and the line below the capstone (half strength) | - | - | - | - | 51% | 30% | 4.8 | Not judged |
| Fight, a rare fish: Trophy Catch alone | - | - | - | - | 46% | 18% | 4.6 | Yes |
| Fight, a rare fish: Patient Angler, the whole line with Trophy Catch | - | - | - | - | 49% | 39% | 4.9 | Not judged |
| Fight, a rare fish: Aggressive Angler, the whole line with Trophy Catch | - | - | - | - | 52% | 36% | 4.9 | Not judged |

## Length

### Fight length by rank above the fish's level, No perks (target 3-6 actions (fishing fight); mean over ranks 10, 20, 30, 40, 50)

| Rank - fish level | Adaptive turns | Rule-of-thumb turns | Verdict |
| --- | --- | --- | --- |
| -6 | 3.8 | 2.8 | Within |
| -5 | 4.2 | 3.0 | Within |
| -4 | 4.7 | 3.1 | Within |
| -3 | 4.9 | 3.4 | Within |
| -2 | 5.0 | 4.2 | Within |
| -1 | 5.0 | 4.7 | Within |
| 0 | 5.2 | 5.6 | Within |
| +1 | 5.4 | 5.4 | Within |
| +2 | 5.3 | 5.4 | Within |
| +3 | 5.4 | 5.4 | Within |
| +4 | 4.7 | 5.5 | Within |

## XP rate

### Fishing: XP per hour against the legacy loop (0.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per bite | Legacy actions per bite | Legacy XP per hour | New XP per bite | New actions per bite | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0-10 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 4345 | 5.5 | 308623 | 1.00 | - | Passed |
| 11-20 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 4413 | 5.4 | 314241 | 1.02 | - | Passed |
| 21-30 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 4144 | 4.9 | 322794 | 1.05 | - | Passed |
| 31-40 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 4365 | 5.1 | 326487 | 1.06 | - | Passed |
| 41-52 | best rod for the rank, rank = fish level (at most 50) | 617 | 0.0 | 317473 | 4157 | 5.0 | 318882 | 1.00 | - | Passed |

## Evidence: XP rate at the fastest allowed pace

### Fishing at the fastest allowed pace: XP per hour against the legacy loop (0.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per bite | Legacy actions per bite | Legacy XP per hour | New XP per bite | New actions per bite | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0-10 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 3771 | 4.5 | 318049 | 1.03 | - | Passed |
| 11-20 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 3830 | 4.4 | 323990 | 1.05 | - | Passed |
| 21-30 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 3497 | 3.9 | 329398 | 1.07 | - | Passed |
| 31-40 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 3719 | 4.1 | 333638 | 1.08 | - | Passed |
| 41-52 | best rod for the rank, rank = fish level (at most 50) | 617 | 0.0 | 317473 | 3528 | 4.0 | 326315 | 1.03 | - | Passed |

## Length (Patient Angler (full line))

### Fight length by rank above the fish's level, Patient Angler (full line) (target 3-6 actions (fishing fight); mean over ranks 10, 20, 30, 40, 50)

| Rank - fish level | Adaptive turns | Rule-of-thumb turns | Verdict |
| --- | --- | --- | --- |
| -6 | 4.2 | 2.8 | Within |
| -5 | 4.4 | 3.0 | Within |
| -4 | 4.6 | 3.1 | Within |
| -3 | 4.8 | 3.4 | Within |
| -2 | 5.0 | 4.2 | Within |
| -1 | 5.2 | 4.7 | Within |
| 0 | 5.1 | 5.6 | Within |
| +1 | 4.5 | 5.4 | Within |
| +2 | 4.1 | 5.4 | Within |
| +3 | 3.9 | 5.4 | Within |
| +4 | 3.4 | 5.5 | Within |

## XP rate (Patient Angler (full line))

### Fishing (Patient Angler (full line)): XP per hour against the legacy loop (0.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per bite | Legacy actions per bite | Legacy XP per hour | New XP per bite | New actions per bite | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0-10 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 4326 | 5.3 | 312849 | 1.01 | - | Passed |
| 11-20 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 4308 | 5.4 | 310315 | 1.01 | - | Passed |
| 21-30 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 4177 | 5.2 | 310006 | 1.00 | - | Passed |
| 31-40 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 4238 | 5.3 | 308924 | 1.00 | - | Passed |
| 41-52 | best rod for the rank, rank = fish level (at most 50) | 617 | 0.0 | 317473 | 3842 | 4.5 | 322141 | 1.01 | - | Passed |

## Length (Aggressive Angler (full line))

### Fight length by rank above the fish's level, Aggressive Angler (full line) (target 3-6 actions (fishing fight); mean over ranks 10, 20, 30, 40, 50)

| Rank - fish level | Adaptive turns | Rule-of-thumb turns | Verdict |
| --- | --- | --- | --- |
| -6 | 4.0 | 2.8 | Within |
| -5 | 4.2 | 3.0 | Within |
| -4 | 4.5 | 3.1 | Within |
| -3 | 4.6 | 3.4 | Within |
| -2 | 4.7 | 4.2 | Within |
| -1 | 5.0 | 4.7 | Within |
| 0 | 4.8 | 5.6 | Within |
| +1 | 4.3 | 5.4 | Within |
| +2 | 4.0 | 5.4 | Within |
| +3 | 3.7 | 5.4 | Within |
| +4 | 3.3 | 5.5 | Within |

## XP rate (Aggressive Angler (full line))

### Fishing (Aggressive Angler (full line)): XP per hour against the legacy loop (0.0 s per action legacy, 8.0 s per action new; unmeasured estimates)

| Group | Variant | Legacy XP per bite | Legacy actions per bite | Legacy XP per hour | New XP per bite | New actions per bite | New XP per hour | Ratio | XP multiplier needed | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0-10 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 4326 | 5.3 | 312849 | 1.01 | - | Passed |
| 11-20 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 4304 | 5.3 | 312292 | 1.01 | - | Passed |
| 21-30 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 4119 | 4.9 | 323180 | 1.05 | - | Passed |
| 31-40 | best rod for the rank, rank = fish level (at most 50) | 600 | 0.0 | 308571 | 4208 | 5.0 | 321955 | 1.04 | - | Passed |
| 41-52 | best rod for the rank, rank = fish level (at most 50) | 617 | 0.0 | 317473 | 3810 | 4.3 | 330453 | 1.04 | - | Passed |

## Evidence: Angler perk magnitudes

### Each purchase alone: the adaptive player's catch rate at rank 50, owning only that purchase at the level named, against a fish of the angler's own level and one 3 levels above

| Purchase (alone) | Effect read by the Line | Fish at the rank's level | Fish 3 levels above | Added points (mean of both) |
| --- | --- | --- | --- | --- |
| No perks | - | 63.5% | 33.2% | - |
| Strong Line I | cap +2 | 64.0% | 39.8% | 3.5 |
| Hard Set I | Set Hook drain +1 | 66.3% | 32.0% | 0.8 |
| Fish Sense I | two moves ahead for 1 turn(s) | 68.8% | 38.7% | 5.4 |
| Patient Angler | Give Line -1 | 64.7% | 29.9% | -1.1 |
| Aggressive Angler | Reel +1 | 66.6% | 32.7% | 1.3 |
| Strong Line II | cap +5 | 66.4% | 44.6% | 7.2 |
| Hard Set II | Set Hook drain +2 | 66.2% | 34.0% | 1.8 |
| Fish Sense II | two moves ahead for 3 turn(s) | 71.6% | 42.5% | 8.7 |
| Strong Line III | cap +8 | 70.4% | 51.1% | 12.4 |
| Hard Set III | Set Hook drain +3 | 67.3% | 34.0% | 2.3 |
| Trophy Catch | rare fish -4 rungs | 59.9% | 31.6% | -2.6 |

### The fish at the angler's own level, by rank: the adaptive player with no perks and with the whole line the rank allows

| Rank | Quick Reel | Bound (no perks) | No perks | Patient Angler line | Aggressive Angler line | Target |
| --- | --- | --- | --- | --- | --- | --- |
| 5 | 60% | 72.0% | 53.5% | 60.8% | 60.8% | - |
| 10 | 60% | 72.0% | 55.9% | 65.8% | 65.8% | - |
| 15 | 60% | 72.0% | 54.1% | 76.2% | 76.2% | - |
| 20 | 60% | 72.0% | 58.6% | 79.9% | 78.4% | - |
| 25 | 60% | 72.0% | 55.3% | 79.6% | 76.2% | - |
| 30 | 60% | 72.0% | 65.0% | 86.0% | 83.4% | - |
| 35 | 60% | 72.0% | 66.5% | 87.6% | 86.2% | - |
| 40 | 60% | 72.0% | 65.9% | 93.8% | 94.4% | - |
| 45 | 60% | 72.0% | 63.6% | 96.1% | 96.0% | - |
| 50 | 60% | 72.0% | 63.5% | 91.5% | 90.6% | 90% |

## Evidence: style balance

### Style balance with the whole line (ties within 2.0 points count half to each)

| Rank | Rank - fish level | Patient Angler (full line) | Aggressive Angler (full line) | Better |
| --- | --- | --- | --- | --- |
| 20 | -4 | 16% | 14% | Tie |
| 20 | -3 | 37% | 40% | Aggressive |
| 20 | -2 | 49% | 46% | Patient |
| 20 | -1 | 64% | 61% | Patient |
| 20 | 0 | 80% | 78% | Tie |
| 20 | +1 | 86% | 83% | Patient |
| 20 | +2 | 88% | 86% | Tie |
| 35 | -4 | 40% | 39% | Tie |
| 35 | -3 | 54% | 52% | Tie |
| 35 | -2 | 66% | 65% | Tie |
| 35 | -1 | 82% | 80% | Tie |
| 35 | 0 | 88% | 86% | Tie |
| 35 | +1 | 95% | 94% | Tie |
| 35 | +2 | 100% | 100% | Tie |
| 50 | -4 | 44% | 46% | Aggressive |
| 50 | -3 | 60% | 60% | Tie |
| 50 | -2 | 64% | 65% | Tie |
| 50 | -1 | 84% | 83% | Tie |
| 50 | 0 | 92% | 91% | Tie |
| 50 | +1 | 99% | 99% | Tie |
| 50 | +2 | 100% | 100% | Tie |

## Limitations

- Today's catch chance depends on the character's Willpower, which is not measured for active characters yet; the corridor between Willpower 10 and 14 stands in for it.
- The adaptive player is a bounded-depth expectimax with a generic fish heuristic. It is not an optimal player, and real players are less skilled; the rule-of-thumb player marks the other end. The study does not measure how real players play.
- The game places the six special fish at a random location and bait on every server start. The study fixes one placement (`FishingCatchModel.SpecialPlacementSeed` = 1) so the report can be reproduced; another placement moves the top band's mix and the pooled rows a little.
- The fight has no hidden chance, so a very easy fish is landed every time by a skilled player, where today's chance stops at 95%.
- The pooled table averages every pool a rod can reach with equal weight, as the legacy baseline does, and ignores the 80% chance of no bite when only the default fish is on offer. Fish per hour is not modelled: location choice, bait supply and respawn are outside the study.
- Gate 6 weighs the time a fight adds (8.0 s a turn is unmeasured) against the XP multiplier, so the multipliers are starting values: they shrink or grow with the real seconds per turn. The game keeps the pace from below in two ways: each action after the first waits the turn, and the rest of the multiplied XP is paid in proportion to how long the fight lasted (up to the full turns), so a fast player, or one who loses the fight on purpose after a move, earns about today's XP per hour and no more; the evidence table for the fastest allowed pace (first action at once) shows it a few percent above today's. The gate plays every fight at the modelled turn, so a player slower than that earns less than today per hour.
- The multiplier raises the XP of a fight that lasts the full turns about eightfold, because the fight takes several times as long as the cast it follows; a fighter lands fewer fish per hour than a quick reeler by the same factor. Gate 6 asks for it; it is flagged here for the owner. Quick Reel pays today's XP, so it needs no multiplier.
- `LineRules.RungAtRank` (44) is set so that, with no perks, a skilled fight lands slightly more than Quick Reel at the fish's own level (about 1.03 times; the owner's reading of fighting as optional, for a slightly better catch). It sets the unperked fight's margin over Quick Reel; the bound applies to that fight only and does not limit the Angler line, which is judged against its own target. A fish above the rank climbs the difficulty ladder 7 rungs a level instead of 5, so the unperked fight falls behind Quick Reel there and the bound holds for the fish above the rank too. Lowering `RungAtRank` strengthens the unperked fight and raises its margin over Quick Reel.
- The Angler line fills all ten rows (owner decisions, October 2026): Steady Hold is dropped, Fish Sense is limited to the first turns of a fight, the bound of Quick Reel plus 15% applies to the unperked fight only, and the whole line aims at about 90% of the fish at the angler's own level. A rare fish takes the fight perks at half strength (rounded down; Fish Sense off) and Trophy Catch at full strength; the rare-fish rows keep the old bound as a check on supply.
- A fish more than 3 levels above the rank climbs the difficulty ladder 9 rungs a level instead of 7, so a fish Quick Reel cannot land at all (6 or more levels above the rank) is as hopeless in the fight whatever perks the angler has.
- Perks, food and status effects are not modelled except where an evidence table names a perk. The client was not run.
