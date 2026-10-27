# Space combat mode

`dotnet run --project SWLOR.CombatSim -- space <scenario.json> [--runs 300] [--seed 1] [--out report.md] [--json results.json]`

Runs a ship-vs-ship fight (rules: `SWLOR.Game.Server/Readmes/SpaceRevampPlan.md` sections 5, 7, 8 and 9). Every number is a scenario input; only the shared ground formulas (`Combat.CalculateHitRate`, `CalculateDamageRange`, `CalculateCriticalRate`) are constants. Run `n` uses seed `seed + n`. The example under `Scenarios/Space/` uses placeholder numbers.

## Scenario keys

Unknown keys are rejected. Names and enum values are case-insensitive.

- Scenario: `name`, `description`, `durationSeconds` (120), `timeStepSeconds` (0.1), `sizeHitPenaltyPerStep` (whole percentage points), `sizeDamagePenaltyPerStep` (0-1), `criticalRateMinimum`, `criticalRateMaximum` (override the ground 5-50), `attackers`, `defenders`.
- Side: `name`, `passive` (fires no weapons or abilities; repairers still run), `ships`.
- Ship: `name`, `count`, `size` (Small, Medium, Large, Capital), `maxHull`, `maxShield`, `maxCapacitor`, `shieldRegenPerSecond`, `capacitorRegenPerSecond`, `attack`, `accuracy`, `defense`, `evasion`, `resistances` (`em`, `thermal`, `explosive`, -100..100), `pilot` (`agi`, `per`, `vit`, `wil`, `mgt`, `maxStm`, `stmRegenPerSecond`), `weapons`, `repairers`, `passives`, `abilities`.
- Weapon: `name`, `family` (Energy, Launcher), `damageType` (EM, Thermal, Explosive), `dmg`, `rank` (0-50), `cycleSeconds`, `capacitorPerCycle`, `attack`, `accuracy`, `size` (the ship's size when omitted), `shieldBypass`, `ignoreSizePenalty`, `shieldDamageMultiplier`.
- Repairer: `name`, `target` (Hull, Shield), `amountPerCycle`, `cycleSeconds`, `capacitorPerCycle`.
- Passive: `name`, `hull`, `shield`, `defense`, `evasion`, `resistances` (added to the ship's).
- Ability (a one-press burst): `name`, `weapon` (a fitted weapon whose shot it borrows), `bonusDmg`, `cooldownSeconds`, `capacitorCost`, `stmCost`.

## Behaviour worth knowing

- Weapon and repairer cycles are anchored to the fight start (cycle n is due at n x period) and fire at the first living enemy. A cycle the capacitor cannot pay for is skipped, not delayed. A burst ability waits until its cost can be paid, then starts its cooldown.
- Ships alive at the start of a step all act in it, so ships that destroy each other in one step both fire. A time to kill is the time of the killing shot (a kill in the first step is 0 s).
- Per shot: hit (accuracy against evasion, minus the size penalty), critical, damage range and roll, the size damage penalty, resistance, then shield and hull. Damage is not rounded after the roll.
