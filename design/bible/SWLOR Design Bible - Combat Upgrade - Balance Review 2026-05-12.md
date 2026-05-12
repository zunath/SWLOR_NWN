# SWLOR Combat Upgrade Bible Balance Review - 2026-05-12

## Resolution Status

Resolved in the workbook on 2026-05-12. The local manifest and audit were refreshed from `design/bible/SWLOR Design Bible - Combat Upgrade.xlsx` after the edits.

The notes below are retained as the review trail for what was changed.

## Scope

Reviewed `design/bible/SWLOR Design Bible - Combat Upgrade.xlsx` using the refreshed manifest at `SWLOR.Game.Server/Readmes/CombatUpgradeBiblePerkManifest.csv`.

Focused on combat-related perk data:

- Armor
- Weapon skills
- Force
- Devices
- Beast Mastery
- Leadership combat styles
- First Aid
- Espionage

Crafting, gathering, piloting, and city/guild utility perks were only checked where they affected combat balance or manifest hygiene.

## Executive Summary

The combat perk structure is mostly healthy. The 55 SP style baseline is consistent across the redesigned combat/support lines, and Leadership's two combat styles already match that baseline: `Vanguard Command` is 55 SP and `Field Steward` is 55 SP.

Leadership is not structurally overpriced or underpriced. Its main risk is behavioral: the workbook does not define aura stacking, aura exclusivity, SOC scaling caps, or exact magnitudes for several party-wide effects. If Leadership auras stack freely and SOC scaling is uncapped, Leadership becomes the dominant party multiplier. If auras are mutually constrained and scaling is capped, it aligns well with Force, Devices, First Aid, and weapon support tools.

Before the code alignment pass, the workbook should fix several high-impact doc issues:

- `Beast Mastery!47` is being ingested as a fake perk header row.
- `Lightsaber!36` and `Heavy Vibroblade!23` are implemented combat abilities with missing cooldown data.
- `Spear` has six 3-second combat recasts; three of them apply an 8-second Force Disruption effect and are clear control outliers.
- Leadership needs an explicit aura rule and SOC scaling cap.
- Leadership's first revive arrives much earlier than First Aid's revive identity.
- Several combat rows still use qualitative magnitudes like "minor", "moderate", "strong", or "major" where the code pass will need numbers.

## Baseline Structure Check

| Area | Result |
| --- | --- |
| Core weapon skills | 110 SP each, generally two 55 SP styles. Some legacy styles have 17 or 19 rows, but the SP totals still land at 55 per style. |
| Force | 220 SP total, four 55 SP lines. Requirement progression is consistent: 0, 5, 8, 12, 15, 18, 22, 25, 28, 30, 35, 38, 40, 42, 45, 48, 50. |
| Devices | 220 SP total, four 55 SP lines. Same redesigned requirement progression as Force. |
| First Aid | 110 SP total, two 55 SP lines. Same redesigned requirement progression. |
| Espionage | 110 SP total, two 55 SP lines. Same redesigned requirement progression. |
| Leadership | 140 SP total: Mayor 20, Diplomat 10, Vanguard Command 55, Field Steward 55. The combat portion is aligned; the extra 30 SP is noncombat utility. |
| Beast Mastery | Combat role lines are 55 SP each. Training and Bioengineer are separate support/progression lines. One duplicate header row currently pollutes the manifest. |

## High-Impact Findings

### 1. Beast Mastery Header Row Is Imported As A Perk

`Beast Mastery!47` appears in the manifest as:

- Style: `Style`
- Price: `SP Price`
- Perk Name: `Perk Name`
- Skill Requirements: `Beast Level Req.`

This is a manifest blocker for the code alignment pass. It should either be removed from the sheet's import range or made visually distinct in a way the audit script ignores.

Recommended answer: remove the repeated header row from the data table and keep role section labels outside the importable perk range.

### 2. Implemented Combat Abilities Missing Cooldowns

These rows are active combat perks with missing cooldown data:

- `Lightsaber!36`, `Overwhelming Strike`: cone weapon damage plus Sunder for 30 seconds, 10 STM, no casting time, no cooldown.
- `Heavy Vibroblade!23`, `Bloodlust`: sacrifices 40% HP to restore 20% to 80% max STM, no casting time, no cooldown.

Recommended answers:

- `Overwhelming Strike`: `Casting Time = Instant`, `Cooldown Time = 90 seconds`.
- `Bloodlust`: `Casting Time = Instant`, `Cooldown Time = 3 minutes`.

The exact numbers can move, but both need explicit cooldowns before code alignment.

### 3. Spear Has 3-Second Combat Recasts That Break The Shared Baseline

The only weapon combat perks with cooldowns at or below 12 seconds are Spear perks. This makes Spear an outlier against the rest of the weapon family.

Control outliers:

- `Spear!10`, `Disabling Strike I`: 3-second recast, 8-second Force Disruption.
- `Spear!15`, `Disabling Strike II`: 3-second recast, 8-second Force Disruption.
- `Spear!22`, `Disabling Strike III`: 3-second recast, 8-second Force Disruption.

Damage outliers:

- `Spear!32`, `Side Assault I`: 3-second recast.
- `Spear!38`, `Side Assault II`: 3-second recast.
- `Spear!43`, `Side Assault III`: 3-second recast.

Recommended answer:

- Move `Disabling Strike` recasts to 30 seconds, or reduce Force Disruption to a very short interrupt-only window.
- Move `Side Assault` recasts to 12 seconds if it is intended as a frequent positional builder, or 18 to 30 seconds if it should match normal weapon ability cadence.
- If 3 seconds is intentional, add a note that these are basic-builder replacements and prevent the control versions from maintaining permanent disruption.

## Leadership Review

### Verdict

Leadership is structurally aligned with the rest of the redesigned combat perks:

- `Vanguard Command` is 17 perks, 55 SP.
- `Field Steward` is 17 perks, 55 SP.
- Requirement progression matches Force, Devices, First Aid, and Espionage.
- Its combat identity is distinct: party hit/damage/crit coordination and party mitigation/recovery.

The line becomes imbalanced only if the workbook's unspecified behaviors are interpreted generously.

### First Design Question To Lock

Question: How many Leadership auras can be active and how do duplicates stack?

Recommended answer:

A leader may maintain one `Vanguard Command` aura and one `Field Steward` aura. Duplicate aura families from multiple leaders do not stack; the strongest effective aura applies. Aura radius comes from the aura's base radius plus relevant range traits. Auras should clearly state whether they persist until toggled, expire after a duration, or are pulse buffs.

This preserves the value of investing deeply into both Leadership combat styles without allowing one leader to stack every aura in both trees.

### Aura Stacking Is Undefined

Leadership aura rows:

- Vanguard: `Leadership!24`, `!26`, `!28`, `!30`, `!32`, `!35`, `!38`
- Field Steward: `Leadership!43`, `!45`, `!47`, `!49`, `!51`, `!54`, `!57`

Current descriptions do not say whether auras:

- are toggles,
- have durations,
- are mutually exclusive,
- stack with other Leadership auras,
- stack between multiple leaders,
- stack with Force, First Aid, Devices, and weapon buffs.

Recommended answer: add a sheet note above the combat Leadership sections defining aura exclusivity, duration, stacking, radius, and duplicate handling.

### SOC Scaling Needs Caps

Leadership uses "plus SOC scaling" on party-wide percentage effects:

- hit chance
- damage
- critical chance
- critical damage
- evasion
- mitigation
- temporary HP
- STM restoration
- healing received

Party-wide percentage scaling is much more dangerous than single-target scaling. Without caps, high SOC can turn Leadership into the best damage line and the best defensive line at the same time.

Recommended answer:

- For small persistent auras, cap SOC scaling at roughly +50% of the base magnitude or a fixed hard cap of +2 percentage points, whichever is clearer.
- For burst shouts, cap final values explicitly in the description.
- Suggested cap targets:
  - `Decisive Command`: max +18% damage, +10% hit chance, +10% critical hit chance.
  - `Hold the Line`: max 12% max-HP temporary HP and 30% damage reduction.
  - Persistent hit/evasion/crit/mitigation auras: cap final aura values in the 4% to 6% range unless only one aura can be active total.

### Rousing Shout I Undercuts First Aid's Revive Identity

`Leadership!44`, `Rousing Shout I`, grants an in-combat ally revive at Leadership 5.

Comparable revive access:

- `First Aid!13`, `Resuscitation I`, arrives at First Aid 18, 4-second cast, 3-minute cooldown.
- `First Aid!18`, `Resuscitation II`, arrives at First Aid 35.
- Leadership revive casts are slower and have longer cooldowns, which is good, but the first revive arrives too early.

Recommended answer:

Change `Rousing Shout I` so it does not perform a true combat revive. It should either stabilize/prevent defeat, grant temporary HP to a conscious ally, or revive only outside combat. Let `Rousing Shout II` at Leadership 18 become the first true Leadership combat revive.

### Field Recovery Needs Numeric Throughput

`Leadership!47`, `Field Recovery I`, and `Leadership!54`, `Field Recovery II`, currently restore "minor" and "moderate" STM plus SOC scaling every 6 seconds.

This can accidentally eclipse:

- First Aid's `Adrenal Stim` line, which has 2-minute cooldowns and consumable costs.
- Force `Clarity`, which has active FP costs and 45-second cooldowns.

Recommended answer:

Give Field Recovery exact values and keep it below burst recovery tools:

- `Field Recovery I`: 1 STM every 6 seconds, SOC scaling capped to 2 STM per tick.
- `Field Recovery II`: 2 STM every 6 seconds, SOC scaling capped to 4 STM per tick.

If multiple aura stacking is allowed, these caps should be lower.

### Field Steward Cleanse And Mitigation Are Acceptable With Limits

`Cleanse Order I/II`, `Bolster Resolve`, `Watchful Presence`, and `Hold the Line` are reasonable as a support identity if they are bounded:

- First Aid should remain the best single-target healing and fast cleanse line.
- Force should remain the best FP-based alternative support line.
- Leadership should be slower, party-oriented, and dependent on positioning/aura rules.

Recommended answer: keep Leadership's support broader but lower-throughput than First Aid, with longer cooldowns and precise caps.

## Cross-Line Balance Notes

### Legacy Weapon Party Buffs Need Terminology Normalization

Several implemented weapon perks use older raw-stat terms or very large party buffs:

- `Spear!37`, `Improved Attentiveness`: party members gain +25% accuracy for 1 minute.
- `Heavy Vibroblade!21`, `Soul Storm`: all nearby allies gain +35% Attack for 1 minute, with HP sacrifice.
- `Heavy Vibroblade!39`, `Rampart`: allies gain +25% defense bonus for 1 minute.
- `Lightsaber!34`, `Brutal Assault`: allies gain +10% critical hit rate for 1 minute.

These may be balanced by long cooldowns, self-exclusion, or HP costs, but they use terminology that does not match the redesigned support lines. They can also make Leadership look weak or strong depending on whether "Attack", "Accuracy", and "Defense" mean raw stats or final percentages.

Recommended answer: normalize these descriptions to the same language used by Leadership and the redesign pass: hit chance, damage, mitigation, evasion, critical chance, and critical damage.

### Capstone Cadence Needs A Rule

The workbook currently mixes two kinds of level-50 perks:

- Long-cooldown ultimates, often 30 minutes, common in weapon lines and Leadership.
- Rotation capstones, often 2 to 5 minutes, common in Force, Devices, First Aid, Espionage, and Beast roles.

This can be valid, but the doc should state the rule. Otherwise code alignment will treat all level-50 5 SP perks as comparable even when their cooldown philosophy differs.

Recommended answer:

Add a short note to the scaling or combat perk section:

"A level-50 perk may be either an ultimate or a rotation capstone. Ultimates use 30-minute cooldowns and swing a fight. Rotation capstones use 2 to 5 minute cooldowns and complete a style's normal kit. The description and cooldown should make the category obvious."

### Qualitative Magnitudes Need Numbers Before Code Alignment

Rows still using qualitative terms should be made numeric before implementation alignment. High-priority examples:

- `Force!20`, `Purifying Wave`: "small amount of HP"
- `Force!36`, `Force Mend`: "moderate HP"
- `Force!39`, `Force Sanctuary`: "minor regeneration"
- `Force!43`, `Circle of Harmony`: "moderate HP", "minor FP and STM regeneration"
- `Beast Mastery!82`, `Unbreakable Beast`: "major damage reduction"
- `Beast Mastery!98`, `Pack Recovery`: "small amount of STM"
- `Beast Mastery!154`, `Force-Bonded Beast`: "minor FP regeneration"
- `Leadership!47`, `Field Recovery I`: "minor STM"
- `Leadership!50`, `Cleanse Order I`: "minor temporary HP"
- `Leadership!52`, `Triage Protocol I`: "minor healing received bonus"
- `Leadership!54`, `Field Recovery II`: "moderate STM"
- `Leadership!56`, `Cleanse Order II`: "damage reduction"
- `First Aid!11`, `Emergency Sealant I`: "minor HP regeneration"
- `First Aid!27`, `Adrenal Stim I`: "minor STM regeneration"

Recommended answer: replace each qualitative term with exact base values and explicit stat scaling or caps.

## Low-Risk Cleanup

- `Twin Blade!32`, `Duelist Stance`, has a blank STM cell while comparable stance rows use `-`. Normalize to `-`.
- Several descriptions contain grammar issues that may carry into player-facing text:
  - "attempts inflicts"
  - "reduce" where "reduces" is intended
  - "a Inflicts"
  - "crticial"
- Leadership Mayor and Diplomat are fine to leave alone, but add a note that they are excluded from combat balancing.

## Recommended Spreadsheet Edit Order

1. Fix manifest blockers: `Beast Mastery!47`, missing cooldowns on `Lightsaber!36` and `Heavy Vibroblade!23`.
2. Define Leadership aura rules and SOC caps.
3. Adjust Leadership revive progression so First Aid keeps the earliest true combat revive.
4. Fix Spear 3-second recast outliers.
5. Quantify all "minor/moderate/strong/major" combat magnitudes.
6. Normalize legacy weapon party-buff terminology.
7. Run `powershell -ExecutionPolicy Bypass -File tools/UpdateCombatUpgradeAudit.ps1 -RefreshLocalBible` after editing the XLSX.
