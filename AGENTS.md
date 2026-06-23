# Agent Rules

## Naming

- Do not use internal initiative, milestone, or phase labels such as `CombatUpgrade` in production code identifiers, filenames, namespaces, classes, methods, or comments. Use domain terms that describe gameplay behavior, such as ability targeting, ability effects, Leadership, Devices, or the specific system being changed.

## Stat-Driven Gameplay

- Shared combat, ability, and status-effect infrastructure must not special-case specific perk types or perk-specific status-effect classes to unlock gameplay behavior. Model perk-driven behavior as `StatType` adjustments, then have shared systems read those stats. Direct perk checks are only appropriate for ownership, unlock, purchase, UI, or progression gates.
- `StatType` classification, polarity, or category decisions must be declared with `StatTypeAttribute` on the enum entry. Do not add large `if`/`switch` lists elsewhere to infer stat meaning; shared systems should read the enum metadata instead.
- Attack Deflection, Shield Deflection, and Guard are separate combat mechanics. Attack Deflection and Shield Deflection are attack-roll outcomes that negate the hit and do not stack with each other; Guard is a damage-stage outcome that reduces damage and increases enmity. Do not implement one by reusing the state, stats, logs, or triggers of another.

## Player Identity

- Player-facing surfaces must use the `PlayerName` service instead of raw player names. For live player objects, use `PlayerName.GetDisplayName(observer, target)` or `PlayerName.GetColoredDisplayName(observer, target)`. For offline/persisted player records, use `PlayerName.GetDisplayNameByPlayerId(observer, playerId, fallbackName)`.
- Do not expose raw `GetName(player)`, `Player.Name`, `dbPlayer.Name`, `GetPCPlayerName`, public CD keys, or account names in ordinary player-facing UI, dialogs, nearby broadcasts, combat/status logs, HoloNet-style broadcasts, market/civic/property lists, or generated public object names.
- Unnamed player characters use a stable unknown display descriptor. Blank descriptors are generated once from the persisted original appearance/species and base stats during migration or login, and fall back to a generic humanoid descriptor if species or stats cannot be resolved. Descriptor generation, descriptor persistence, and descriptor fallback lookup belong in the `PlayerDescriptor` service; `PlayerName` should consume descriptors while remaining responsible for observer-specific name resolution. Self-targeted `/name` replaces that descriptor and permanently discards the generated one.
- Self-targeted `/name` sets the player's unknown display description. This remains an unnamed/unknown identity and must continue to render with the unknown gray name token. If the observer has not named the target, show only the gray descriptor. If the observer has named the target, show the assigned name plus the gray descriptor in brackets by default, such as `Joe Blow [A Seedy Individual]`; non-DM players may hide descriptors for named targets in Settings, in which case they see only their assigned name. Staff observers should always see the canonical character name plus the gray descriptor in brackets, such as `Joe Smith [A Seedy Individual]`.
- `/name` input is limited to 64 characters and must reject player-entered color tokens. Color styling for known, unknown, and staff-facing name displays is controlled by the `PlayerName` service.
- Property and ship permission management is a narrow exception because it grants persistent access to real character records. These screens may search canonical character names as well as observer-known names, and should display `PlayerName.GetKnownNameOrFallbackByPlayerId(observer, playerId, fallbackName)` so fake/known names are preserved when present and canonical names are available when no known name exists.
- Server logs and audit trails must retain raw/canonical player identity for moderation and traceability. Raw/canonical player identity is also acceptable for DM/admin-only tools, persisted ownership fields, and messages shown only to that same player. Public custom names deliberately entered by players, such as renamed properties or droids, may remain visible.

## Design Bible

- After editing `design/bible/SWLOR Design Bible - Combat Upgrade.xlsx`, run `powershell -ExecutionPolicy Bypass -File tools/UpdateCombatUpgradeAudit.ps1 -RefreshLocalBible` to refresh `SWLOR.Game.Server/Readmes/CombatUpgradeBiblePerkManifest.csv` and `SWLOR.Game.Server/Readmes/CombatUpgradePerkAudit.csv` from the local workbook.

## Full Rebuild Changes

- For rebuild-era changes covered by a planned full character rebuild, do not add one-off player migrations solely to remove or refund deleted perks, blueprints, skills, or similar character-build data. Rely on the full rebuild path unless the change affects persistent data that survives rebuild or server/world state outside character builds.
- Until the combat-upgrade migration set ships, fold additional combat-upgrade migration work into the existing in-flight combat-upgrade migrations instead of adding new numbered migration files. Add new numbered migrations only after the prior migration version has shipped, or when a change must run separately because of execution timing.

## TLK Entries

- New custom TLK strings must use a pre-existing empty TLK slot or gap before appending new IDs at the end of `SWLOR_Haks/swlor2_tlk/swlor2_tlk.tlk.json`.
- NWN custom TLK references in 2DA files use `16777216 + tlkId`. When moving or adding a TLK entry, update every 2DA/reference to the matching custom strref.
- After editing `swlor2_tlk.tlk.json`, regenerate `swlor2_tlk.tlk` before building or handing off the change.

## Recast Groups

- `RecastGroup` short names are player-facing and limited to 14 characters. Never auto-truncate or use partial-word fragments; choose a meaningful short label and make generators/scripts fail if one is missing.

## Ability Definitions

- Each distinct gameplay ability must have its own `*AbilityDefinition.cs` file and matching `IAbilityListDefinition` class named for that ability. Do not group unrelated abilities into broad definition files such as creature, combat, NPC, or package-level collections. Multiple ranks of the same ability may live in that ability's own definition file.
- Ability-specific targeting metadata must be declared through the ability definition builder/detail pattern. Do not maintain separate explicit production lists of abilities for targeting behavior; shared targeting systems should consume the cached ability definitions.

## Ability Icons

- After adding or changing an ability icon referenced by `SWLOR_Haks/swlor2_2da/feat.2da` or `SWLOR_Haks/swlor2_2da/spells.2da`, run `powershell -ExecutionPolicy Bypass -File tools/GenerateCooldownIcons.ps1 -Force` to regenerate the `pr0_` through `pr5_` cooldown icon variants. This script must use ImageMagick output; do not replace it with a custom TGA writer.

## Ability VFX

- Before choosing or changing perk, ability, status-effect, trap, or scripted creature VFX, consult `SWLOR.Game.Server/Readmes/VisualEffectSelection.md` and `SWLOR.Game.Server/Readmes/VisualEffectReference.csv`. Pick VFX by gameplay moment, visual group, colors, location, and screenshot reference rather than by constant name alone.
- Use the CSV `CSharpEnum` value in C# code. Use `BEAM` entries with `EffectBeam`, `FNF` entries for location/area bursts, `IMP` or `COM` entries for target impact feedback, `DUR` entries for persistent auras or field markers, and `EYES` entries only when the eye/head cue is the intended player-facing signal.

## Ability Damage

- When an ability applies `EffectDamage` with `ApplyEffectToObject`, wrap that call in `AssignCommand(source, () => ApplyEffectToObject(...))` using the damage source as the command object so the damage appears in the player's combat log.
