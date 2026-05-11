# SWLOR Combat Upgrade Scaling Implementation Plan

Date: 2026-05-11

## Goal

Bring the Combat Upgrade Bible and implemented code into alignment for stat scaling.

This pass adds `Primary Stat`, `Secondary Stat`, and `Scaling Source` columns to every perk row in `SWLOR Design Bible - Combat Upgrade.xlsx`, documents existing scaling, adds or normalizes explicit scaling where appropriate, removes stale saving-throw language, and updates audit tooling so future agents can read the workbook without misinterpreting inherited combat scaling as missing per-ability code.

## Agreed Rules

- Add the three scaling columns consistently across every perk sheet.
- Use stat abbreviations only: `MGT`, `PER`, `VIT`, `WIL`, `AGI`, `SOC`, or `None`.
- `Primary Stat` and `Secondary Stat` are mechanical first, archetypal only where mechanical scaling is absent and a design choice is needed.
- Do not introduce new combat-stat scaling to legacy noncombat systems such as crafting, gathering, piloting, research, agriculture, smithery, engineering, fabrication, or other pre-combat-upgrade systems.
- Crafting in particular should not receive combat stat scaling.
- If an ability or perk appears to want more than two stats, list it in the final report but continue the rest of the pass.
- Avoid stat modifiers for scaling. A single point of a stat should provide a benefit.
- For ordinary `weapon DMG + X` or shared combat-impact abilities, do not add extra stat damage. Document the inherited shared combat formula instead.
- Saving throws are obsolete. Bible text and code should not use Fortitude/Reflex/Will DC gates for combat-upgrade perks.
- Active debuffs should generally apply on hit/use, with Resistances responsible for shortening effect lengths.
- Passive/proc debuffs may keep non-save gates such as critical hit, cooldown throttle, stance requirement, or target condition.
- Most status durations remain fixed. Do not add duration scaling by default.
- Fixed stat-granting traits, such as `+10% Attack`, are not stat scaling. Use `None` unless the granted amount itself scales.

## Scaling Source Values

- `Explicit Code`: Implemented perk has direct stat scaling in ability, status, or perk code.
- `Combat Formula`: Implemented perk inherits stat scaling from shared combat resolution, including weapon formula, `ApplyCombatImpact`, `Stat.GetAttack`, and related shared paths. No per-ability scaling code is expected.
- `Design Added`: This pass adds direct explicit scaling code.
- `Design Only`: Intended scaling metadata for a design or unimplemented row. No code should exist yet.
- `Legacy Noncombat`: Older noncombat or pre-combat-upgrade systems. Do not introduce combat-stat scaling.
- `None`: Implemented combat perk is intentionally unscaled and does not inherit relevant shared combat stat scaling.

## Shared Combat Formula Metadata

For shared combat formula rows, document the inherited mechanical stats without changing ability code:

- Melee damage: `MGT`
- Melee accuracy: `PER`
- Ranged damage: `PER`
- Ranged accuracy: `AGI`
- Force and device combat-impact abilities use the stat shown in their shared attack or ability code.

## Explicit Scaling Formula

Use a shared helper for explicit non-combat-formula scaling, but keep it small and easy to audit.

Default normalized formula:

```text
scaledAmount = baseAmount + ceil(baseAmount * ((primaryStat * 0.01) + (secondaryStat * 0.005)))
```

Rules:

- Use direct stat scores, not modifiers.
- If an effect has only a primary stat, omit the secondary term.
- Every stat point should be capable of increasing the result through the percent-of-base formula.
- Use deterministic scaling, not random dice based on stats.
- Percent-of-maximum resource restores may use `basePercent + statScore * percentPerPoint`, capped at a moderate maximum.
- Percent buffs/debuffs generally remain fixed unless already explicitly designed to scale.

## Workbook Changes

1. Insert columns immediately after `Description` on every perk sheet:
   - `Primary Stat`
   - `Secondary Stat`
   - `Scaling Source`
2. Add a `Scaling Legend` sheet explaining the column meanings and source values.
3. Fill legacy noncombat rows with `None`, `None`, `Legacy Noncombat`.
4. Fill design/unimplemented combat rows with intended stats and `Design Only`, or `None` where intentionally unscaled.
5. Fill implemented combat rows based on code:
   - `Combat Formula` for shared formula scaling.
   - `Explicit Code` for pre-existing direct scaling after normalization.
   - `Design Added` when this pass adds direct scaling.
   - `None` for intentionally unscaled implemented perks.
6. Rewrite stale saving-throw text such as `Fortitude DC`, `Reflex DC`, `Will DC`, and `saving throw`.

## Code Changes

1. Add a small shared helper for normalized explicit scaling.
2. Normalize existing explicit stat scaling to use direct stat score and deterministic formulas.
3. Add explicit scaling only to non-damage numeric effects that need it and are implemented.
4. Remove any remaining save/DC gates from implemented combat-upgrade code.
5. Do not add code for `Design Only` rows.

## Tooling Changes

1. Update `tools/UpdateCombatUpgradeAudit.ps1` to parse and export:
   - `PrimaryStat`
   - `SecondaryStat`
   - `ScalingSource`
2. Update Google CSV and local workbook parsing paths.
3. Add `StaleSavingThrowText` audit rows for stale save/DC language.
4. Keep `Combat Formula` from being interpreted as missing per-ability scaling code.

## Verification

Run after workbook or audit-tool edits:

```powershell
powershell -ExecutionPolicy Bypass -File tools/UpdateCombatUpgradeAudit.ps1 -RefreshLocalBible
```

Run after code edits:

```powershell
dotnet build SWLOR.Game.Server.sln --no-restore
```

Final report should include:

- Changed workbook/code/tooling files.
- Count of rows updated by `Scaling Source`.
- Any stale save/DC text remaining.
- Any code save gates remaining.
- Any perks that appear to want more than two stats.
- Any rows intentionally left as `None` or `Legacy Noncombat`.

## Future Reconciliation Notes

These are not blockers for the scaling pass, but they should be revisited before treating the `Design` rows as cleanly unimplemented.

### Design Rows With Existing Code Evidence

- Force: `Force Push I-III`, `Force Leap I-II`, `Deflective Presence`, `Benevolence I-III`, `Mind Trick I-II`, `Comprehend Speech`, `Force Spark I-III`, `Force Body I-II`, `Force Lightning I-II`, `Force Drain I-III`, `Force Rage I-II`, and `Creeping Terror I-III`.
- Devices: `Frag Grenade I-III`, `Concussion Grenade I-III`, `Ion Grenade I-II`, `Adhesive Grenade I-II`, `Deflector Shield I-III`, `Flamethrower I-III`, and `Wrist Rocket I-III`.
- Beast Mastery: `Bite I-III`, `Anger I-II`, `Claw I-III`, `Bolster Attack I-III`, `Hasten I-II`, `Poison Breath I-III`, `Ice Breath I-III`, `Evasive Maneuver I-III`, `Assault I-III`, `Force Touch I-III`, and `Innervate I-III`.
- First Aid: `Med Kit I-IV`, `Treatment Kit I-II`, `Resuscitation I-II`, `Infusion I-II`, `Adrenal Stim I-III`, and `Shielding I-III`.

The current Bible metadata marks these as `Design Only`, so future agents should interpret matching code as legacy or pre-existing implementation needing reconciliation/removal, not as a scaling-pass failure.

### More-Than-Two-Stat Candidates

- Rest/resource regeneration code touches `VIT`, `WIL`, and `MGT`.
- `Absolute Defense`-style protection effects can touch HP, STM, and FP contexts.
- Beast abilities may combine master stats, beast stats, and target/defender stats.
- Shield and temporary HP effects may involve source skill stat plus max-HP or target context.

Do not add third-stat columns during this pass. Prefer documenting primary/secondary stat intent and leaving the contextual value as effect context rather than a third scaling stat.
