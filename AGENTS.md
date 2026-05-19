# Agent Rules

## Naming

- Do not use internal initiative, milestone, or phase labels such as `CombatUpgrade` in production code identifiers, filenames, namespaces, classes, methods, or comments. Use domain terms that describe gameplay behavior, such as ability targeting, ability effects, Leadership, Devices, or the specific system being changed.

## Stat-Driven Gameplay

- Shared combat, ability, and status-effect infrastructure must not special-case specific perk types or perk-specific status-effect classes to unlock gameplay behavior. Model perk-driven behavior as `StatType` adjustments, then have shared systems read those stats. Direct perk checks are only appropriate for ownership, unlock, purchase, UI, or progression gates.
- `StatType` classification, polarity, or category decisions must be declared with `StatTypeAttribute` on the enum entry. Do not add large `if`/`switch` lists elsewhere to infer stat meaning; shared systems should read the enum metadata instead.
- Attack Deflection, Shield Deflection, and Guard are separate combat mechanics. Attack Deflection and Shield Deflection are attack-roll outcomes that negate the hit and do not stack with each other; Guard is a damage-stage outcome that reduces damage and increases enmity. Do not implement one by reusing the state, stats, logs, or triggers of another.

## Design Bible

- After editing `design/bible/SWLOR Design Bible - Combat Upgrade.xlsx`, run `powershell -ExecutionPolicy Bypass -File tools/UpdateCombatUpgradeAudit.ps1 -RefreshLocalBible` to refresh `SWLOR.Game.Server/Readmes/CombatUpgradeBiblePerkManifest.csv` and `SWLOR.Game.Server/Readmes/CombatUpgradePerkAudit.csv` from the local workbook.

## Full Rebuild Changes

- For rebuild-era changes covered by a planned full character rebuild, do not add one-off player migrations solely to remove or refund deleted perks, blueprints, skills, or similar character-build data. Rely on the full rebuild path unless the change affects persistent data that survives rebuild or server/world state outside character builds.

## TLK Entries

- New custom TLK strings must use a pre-existing empty TLK slot or gap before appending new IDs at the end of `SWLOR_Haks/swlor2_tlk/swlor2_tlk.tlk.json`.
- NWN custom TLK references in 2DA files use `16777216 + tlkId`. When moving or adding a TLK entry, update every 2DA/reference to the matching custom strref.
- After editing `swlor2_tlk.tlk.json`, regenerate `swlor2_tlk.tlk` before building or handing off the change.

## Recast Groups

- `RecastGroup` short names are player-facing and limited to 14 characters. Never auto-truncate or use partial-word fragments; choose a meaningful short label and make generators/scripts fail if one is missing.

## Ability Definitions

- Each distinct gameplay ability must have its own `*AbilityDefinition.cs` file and matching `IAbilityListDefinition` class named for that ability. Do not group unrelated abilities into broad definition files such as creature, combat, NPC, or package-level collections. Multiple ranks of the same ability may live in that ability's own definition file.

## Ability Icons

- After adding or changing an ability icon referenced by `SWLOR_Haks/swlor2_2da/feat.2da` or `SWLOR_Haks/swlor2_2da/spells.2da`, run `powershell -ExecutionPolicy Bypass -File tools/GenerateCooldownIcons.ps1 -Force` to regenerate the `pr0_` through `pr5_` cooldown icon variants. This script must use ImageMagick output; do not replace it with a custom TGA writer.

## Ability Damage

- When an ability applies `EffectDamage` with `ApplyEffectToObject`, wrap that call in `AssignCommand(source, () => ApplyEffectToObject(...))` using the damage source as the command object so the damage appears in the player's combat log.
