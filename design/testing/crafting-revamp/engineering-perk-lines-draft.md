# Engineering perk lines: Overclocker and Technician (draft)

Draft for the owner's approval. No perk code, no StatType IDs and no Bible rows exist yet; this file is the only artifact. Every magnitude is a starting value that CraftSim tunes in phase 2. Rules followed: `CraftingRevampPlan.md` (Line schedule, Lines) and `SkillPerkStandards.md`.

## Structure

- Two independent, optional lines of 25 SP each. Neither requires the other, and neither is needed to reach maximum quality (phase 2 gate: an unperked rank-50 crafter with good gear still reaches full quality on at-level recipes; perks change how, and how efficiently).
- Ten purchases per line at Engineering ranks 5/10/15/20/25/30/35/40/45/50, priced 1/1/2/2/3/3/3/3/3/4 SP. The rank-20 purchase is the line's style; the rank-50 purchase is the capstone with no quest requirement.
- A ranked perk occupies two purchases. Its second rank restates the first rank's benefit inside its own total (only the purchased rank's stat bonuses apply).
- Every effect acts only on the Engineering Circuit. Perks grant StatType adjustments and the engine reads them; the engine never tests a perk by name.
- Every action a perk improves or adds is unlocked by skill rank at or before the perk's own rank: Wire/Discharge (Assembly) 0, Tune and Discharge (Calibration) 5, Patch 10, Ground 15, Boost 20.

## How the engine reads perks

At craft start the window builds an immutable `CircuitPerkStats` record from the crafter's stat totals and passes it to `CircuitSetup`, beside rank and gear. The engine reads only that record, so previews and results stay pure. Each stat below is a field of it.

Styles: only one style is active at a time, picked in the craft window before the craft starts (a selector listing "None" and each owned style). The window applies the chosen style as a status effect that carries the style's StatType adjustments, as combat stances do, and removes it when the window closes. Open question 1.

## Overclocker (high load)

The line rewards living above the overload line: more output per Load point, bigger bursts, a higher line, cheaper overloads, and a capstone that turns the worst result into the best one.

| Rank | SP | Perk | Type | Effect (starting values) | StatTypes the engine reads |
| --- | --- | --- | --- | --- | --- |
| 5 | 1 | Conductive Wiring I | trait | Each Load point adds 1 more percentage point of output to Wire, Tune and Discharge (a recipe's 10% becomes 11%). | `CircuitLoadBoostPercent` +1 |
| 10 | 1 | Capacitor Bank | trait | Discharge bursts are 10% larger. | `CircuitDischargePowerPercent` +10 |
| 15 | 2 | Heat Sinks I | trait | The overload line rises by 1. | `CircuitOverloadLine` +1 |
| 20 | 2 | Overclock | style | While active, Wire and Tune add 1 extra Load (2 per action), and each Load point adds 3 more percentage points of output. | `CircuitExtraLoadPerBuildAction` +1, `CircuitLoadBoostPercent` +3 (both carried by the style effect) |
| 25 | 3 | Cascade Discharge | active | A new action that costs 5 CP and spends all Load as one burst into both meters at 60% each (120% of a normal burst in total). It never overloads. | `CircuitCascadeDischargeUnlocked` (flag) |
| 30 | 3 | Overdrive Boost | trait | Boost adds 4 Load instead of 3 and costs 1 less CP. | `CircuitBoostLoad` +1, `CircuitBoostCpReduction` +1 |
| 35 | 3 | Heat Sinks II | trait | The overload line rises by 2 in total (this rank replaces Heat Sinks I's +1). | `CircuitOverloadLine` +2 |
| 40 | 3 | Shock Absorbers | trait | An overload costs a third less Integrity (30% of the piece's maximum becomes 20%). | `CircuitOverloadIntegrityReductionPercent` +33 |
| 45 | 3 | Conductive Wiring II | trait | Each Load point adds 3 more percentage points of output in total (this rank replaces Conductive Wiring I's +1). | `CircuitLoadBoostPercent` +3 |
| 50 | 4 | Surge | capstone, trait | Once per craft, the first overload does not cost Integrity or reset Load; instead it becomes a full-strength Discharge into the meter that is further from full (Assembly on a tie). | `CircuitSurgeUnlocked` (flag) |

Decisions the line creates:

- Heat Sinks against Conductive Wiring and Overclock: a higher line lets Load sit higher safely, while output per Load point makes sitting above the line worth the risk.
- Cascade Discharge against two ordinary Discharges: it costs CP and a turn but fills both meters from one Load stack, which matters when Integrity or CP is the short resource.
- Surge makes the first overload free, so an Overclocker plans to reach it deliberately once per craft.

## Technician (stable, precise)

The line rewards control: fewer wasted Integrity points, finer Calibration, earlier warning of a Clean Signal, and a capstone that makes a craft survive one mistake.

| Rank | SP | Perk | Type | Effect (starting values) | StatTypes the engine reads |
| --- | --- | --- | --- | --- | --- |
| 5 | 1 | Insulated Traces I | trait | Wire, Tune and Discharge cost 1 less Integrity, to a minimum of 1. Boost joins at rank II, when it is unlocked. Ground (2 Integrity) and Patch (none) are left out on purpose. | `CircuitActionIntegrityReduction` +1 |
| 10 | 1 | Fine Calibration I | trait | Tune adds 10% more Calibration. | `CircuitTunePowerPercent` +10 |
| 15 | 2 | Diagnostics | trait | The forecast shows two actions ahead: a Clean Signal is announced two actions before it applies. | `CircuitForecastTurns` +1 |
| 20 | 2 | Safe Mode | style | While active, an action cannot overload until Load is 2 points past the line, and each Load point adds 3 fewer percentage points of output. | `CircuitOverloadGrace` +2, `CircuitLoadBoostPercent` -3 (both carried by the style effect) |
| 25 | 3 | Micro-Tune | active | A new action that adds Calibration equal to 70% of a Tune, adds no Load and costs 2 CP. It follows the ordinary overload rule, so it can overload only when Load already sits above the line. | `CircuitMicroTuneUnlocked` (flag) |
| 30 | 3 | Signal Lock | trait | The chance that a Clean Signal is announced each action rises by 10 points (25% to 35%). | `CircuitSignalChancePercent` +10 |
| 35 | 3 | Precision Patch | trait | Patch restores 10 more Integrity and costs 2 less CP. | `CircuitPatchRestore` +10, `CircuitPatchCpReduction` +2 |
| 40 | 3 | Insulated Traces II | trait | Wire, Tune, Boost and Discharge cost 2 less Integrity in total, to a minimum of 1 (this rank replaces Insulated Traces I's 1 and adds Boost). | `CircuitActionIntegrityReduction` +2 |
| 45 | 3 | Fine Calibration II | trait | Tune adds 25% more Calibration in total (this rank replaces Fine Calibration I's 10%). | `CircuitTunePowerPercent` +25 |
| 50 | 4 | Failsafe | capstone, trait | The first time Integrity would reach 0 in a craft, it stays at 1. | `CircuitFailsafeUnlocked` (flag) |

Decisions the line creates:

- Safe Mode against Overclock: the two styles cannot both be active. Safe Mode gives up output per Load point for freedom from overload, so the Technician spends turns on Tune, Micro-Tune and Diagnostics-timed Clean Signals instead of banking Load.
- Micro-Tune against Tune: Micro-Tune builds Calibration without raising Load, so it is the Calibration action to use at the line; Tune is stronger but raises Load.
- Failsafe lets a Technician take one risk without losing the piece, which is the one place a Technician can borrow from the Overclocker's playbook.

## Proposed StatTypes

Names only; the IDs are reserved against current master and the HAK when the owner approves the rows (the next free StatType today is 1168). Each declares its classification, polarity and category on its enum entry. All are beneficial when positive, except `CircuitLoadBoostPercent` from Safe Mode, which is a style cost (the negative value is a tradeoff and is not beneficial).

| Name | Aggregation | Used by |
| --- | --- | --- |
| `CircuitLoadBoostPercent` | additive | Conductive Wiring, Overclock, Safe Mode |
| `CircuitDischargePowerPercent` | additive | Capacitor Bank |
| `CircuitOverloadLine` | additive | Heat Sinks |
| `CircuitExtraLoadPerBuildAction` | additive | Overclock |
| `CircuitCascadeDischargeUnlocked` | maximum | Cascade Discharge |
| `CircuitBoostLoad` | additive | Overdrive Boost |
| `CircuitBoostCpReduction` | additive | Overdrive Boost |
| `CircuitOverloadIntegrityReductionPercent` | additive | Shock Absorbers |
| `CircuitSurgeUnlocked` | maximum | Surge |
| `CircuitActionIntegrityReduction` | additive | Insulated Traces |
| `CircuitTunePowerPercent` | additive | Fine Calibration |
| `CircuitForecastTurns` | additive | Diagnostics |
| `CircuitOverloadGrace` | additive | Safe Mode |
| `CircuitMicroTuneUnlocked` | maximum | Micro-Tune |
| `CircuitSignalChancePercent` | additive | Signal Lock |
| `CircuitPatchRestore` | additive | Precision Patch |
| `CircuitPatchCpReduction` | additive | Precision Patch |
| `CircuitFailsafeUnlocked` | maximum | Failsafe |

## Engine changes the perks need (phase 3, after approval)

- A `CircuitPerkStats` record on `CircuitSetup`, read by every rule in `CircuitRules` that a stat above changes.
- Two new action ids (`discharge_both`, `micro_tune`) and the state they need: a Surge-used flag, a Failsafe-used flag, and a two-slot announced-opportunity list for Diagnostics. All of it lives in `CircuitState`, so previews stay pure.
- Preview text for each: Cascade Discharge and Micro-Tune show their own rows; Surge and Failsafe appear in the risk text as the result an overload or a break would have (for example, "Overload 24%: Surge turns it into a Discharge").
- Unit tests for each stat at each rank, for style exclusivity and switching, and for the order in which cost reductions and minimums apply.

## Balance evidence needed before coding

- CraftSim (phase 2): fixed-rotation gap, style balance (Overclock against Safe Mode must each be better on at least 30% of recipes, with neither ahead by more than about 5 quality points on average), action counts, and the perks-optional gate with and without each line.
- Comparators for the SP prices: the plan's schedule (1/1/2/2/3/3/3/3/3/4) and the other crafting lines; no combat comparator applies.
- Stacking check: Heat Sinks II (+2 line) with Safe Mode (+2 grace) leaves 4 points of free Load above the base line, and Overclock's extra Load can reach it faster; CraftSim must confirm that owning both lines does not make overload irrelevant.

## Open questions for the owner

1. Style storage: a status effect applied by the window's selector (proposed), or a per-skill style field on the player that the engine reads?
2. Surge's target: "the meter that is further from full" is a rule that needs no prompt in the middle of a craft. Should the player choose the meter instead?
3. Diagnostics shows the next two actions' announcements. Should a Clean Signal announced two actions ahead then also remain visible when it is one action ahead? (The draft says yes.)
4. Are both line capstones acceptable as traits rather than actives? Surge triggers by itself on the first overload, and Failsafe triggers by itself on the first break.
