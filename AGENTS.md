# Agent Rules

This file is the shared rule set for all coding agents. Codex reads it natively; Claude Code imports it through `CLAUDE.md`. Keep cross-agent rules here rather than in agent-specific files.

## Agent Skills

- Agent skills are canonical in `.codex/skills/` and mirrored to `.claude/skills/` so both Codex and Claude Code discover them. The `agents/openai.yaml` files are Codex-only interface metadata and are not mirrored.
- Edit skills only in `.codex/skills/`. Never hand-edit `.claude/skills/`.
- After adding, changing, or deleting any skill file, run `powershell -ExecutionPolicy Bypass -File tools/SyncAgentSkills.ps1` to refresh the mirror. Use `-CheckOnly` to verify sync without writing.
- Keep skill instructions and descriptions agent-neutral: write "Use when adding a beast...", not "Use when Codex/Claude needs to...".

## Read-Only Areas

- The Unified solution (`C:\Projects\unified`) is read-only reference material. Do not make changes to it.

## Background Processes

- Do not start background jobs, watchers, dev servers, publish tasks, or long-lived helper processes unless the user explicitly asks for them or they are strictly required for the current task. Prefer foreground commands with bounded timeouts. If a long-lived process is necessary, record what was started, track its PID when available, stop it before handing off, and report the cleanup. Do not use `Start-Process`, shell backgrounding, persistent REPL helpers, or detached commands to continue work after the turn unless the user has explicitly approved that behavior.

## Chat Commands

- Player-facing chat commands must use `.Permissions(AuthorizationLevel.All)`, not `AuthorizationLevel.Player` alone, unless the command is deliberately meant to exclude DMs/Admins. `AuthorizationLevel.Player`-only silently fails for DM-possessed or DM-authorization accounts with the same generic "Invalid chat command" message used for unregistered commands, which makes it look like the command was never wired up instead of a permissions gap.

## Tests

- Building or testing `SWLOR.Game.Server` fires a Windows post-build deploy (`SWLOR.CLI.exe -o`) that is slow and unnecessary for verification. Always skip it by passing `-p:RunPostBuildEvent=Never` on builds, and use a build-once/test-many flow.
- Build a single time, then run only the relevant tests without rebuilding: `dotnet build SWLOR.Game.Server.Tests\SWLOR.Game.Server.Tests.csproj -p:RunPostBuildEvent=Never`, followed by `dotnet test --no-build --filter "FullyQualifiedName~<RelevantTestClass>"`. Use `|` to combine multiple filters.
- Only run the full unfiltered suite (`dotnet test` with no `--filter`) when a change is broad enough to plausibly affect unrelated systems, or as a final pre-handoff check — not after every edit.

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

## Economy-Restricted Items

- Player-facing item search and economy surfaces (quest contract objective search, and any future market-style blueprint pickers) must not show NPC-only, creature, or internal items. `Item.IsEconomyRestricted` is the single source of truth; `Cache.IsItemSearchableByResref` consumes it. Never hardcode resref lists to exclude items — extend the shared classifier or flag the blueprint.
- Creature-equipment base item types (creature weapons and `CreatureItem` "stat skins") and items whose name carries the reserved `[NPC]`/`(NPC` prefix are excluded automatically, as are blueprints with no real inventory icon.
- For an NPC-only item that a normal player item is otherwise indistinguishable from — a real base type, a real icon, and no `[NPC]` name (e.g. the "Specialist" NPC weapons, "Republic Special Forces Rifle") — set the `NO_ECONOMY` local variable to `1` on the blueprint. This is the explicit opt-out the runtime classifier reads. Prefer this over broadening name/base-type heuristics, which risk hiding legitimate player items.
- If a genuinely new NPC naming convention or creature base type is introduced, update the pattern/base-type set in `Item.IsEconomyRestricted` (not a resref list). `EconomyRestrictedItemTests` guards that every `[NPC]`/`(NPC` blueprint stays covered; keep it green.
- Any item blueprint that players cannot obtain through some source must carry the `NO_ECONOMY` flag. `EconomyObtainabilityCoverageTests` enforces this: it scans every `uti` blueprint, subtracts every obtainable source (loot, stores, placed containers, recipe outputs/components, refining, fishing, quest rewards via `AddItemReward`, training store, starting gear, and `CreateItemOnObject`/`CopyItemAndModify` literals), and requires the remainder (excluding creature/`[NPC]` items the runtime already handles) to be flagged. When it fails on a new item, either wire the item to a real player source or run `python tools/FlagNpcEconomyItems.py` to stamp it. New flags require a module repack on deploy. If you add a genuinely new item-acquisition mechanism, extend the obtainable extraction in both the tool and the test.

## Design Bible

- Follow `SWLOR.Game.Server/Readmes/DesignBibleWorkbookRules.md` when editing any Design Bible workbook.
- Never edit a Design Bible workbook with `openpyxl` (or any library that rewrites the whole workbook without recalculating formulas). It discards the cached formula-result values on every formula cell: the perk sync tests still pass (text tabs have no formulas), but formula-backed tabs silently lose their cached numbers and break tests such as `NPCEnemyBalanceAuditTests`. These workbooks are Google Sheets exports that store text as inline strings, so edit the target cells surgically at the zip/XML level (cells look like `<c r="G31" s="..." t="inlineStr"><is><t>TEXT</t></is></c>`) and repackage copying every other zip entry byte-for-byte, so untouched sheets keep their cached values. The `tools/UpdateCombatUpgradeAudit.ps1 -RefreshLocalBible` formatter preserves caches and is safe to run afterward.
- After editing `design/bible/SWLOR Design Bible - Combat Upgrade.xlsx`, run `powershell -ExecutionPolicy Bypass -File tools/UpdateCombatUpgradeAudit.ps1 -RefreshLocalBible` to refresh `SWLOR.Game.Server/Readmes/CombatUpgradeBiblePerkManifest.csv` and `SWLOR.Game.Server/Readmes/CombatUpgradePerkAudit.csv` from the local workbook.

## Full Rebuild Changes

- For rebuild-era changes covered by a planned full character rebuild, do not add one-off player migrations solely to remove or refund deleted perks, blueprints, skills, or similar character-build data. Rely on the full rebuild path unless the change affects persistent data that survives rebuild or server/world state outside character builds.
- Until the combat-upgrade migration set ships, fold additional combat-upgrade migration work into the existing in-flight combat-upgrade migrations instead of adding new numbered migration files. Add new numbered migrations only after the prior migration version has shipped, or when a change must run separately because of execution timing.

## TLK Entries

- New custom TLK strings must use a pre-existing empty TLK slot or gap before appending new IDs at the end of `SWLOR_Haks/sw_tlk/sw_tlk.tlk.json`.
- NWN custom TLK references in 2DA files use `16777216 + tlkId`. When moving or adding a TLK entry, update every 2DA/reference to the matching custom strref.
- After editing `sw_tlk.tlk.json`, regenerate `sw_tlk.tlk` before building or handing off the change.

## Recast Groups

- `RecastGroup` short names are player-facing and limited to 14 characters. Never auto-truncate or use partial-word fragments; choose a meaningful short label and make generators/scripts fail if one is missing.

## Ability Definitions

- Each distinct gameplay ability must have its own `*AbilityDefinition.cs` file and matching `IAbilityListDefinition` class named for that ability. Do not group unrelated abilities into broad definition files such as creature, combat, NPC, or package-level collections. Multiple ranks of the same ability may live in that ability's own definition file.
- Ability-specific targeting metadata must be declared through the ability definition builder/detail pattern. Do not maintain separate explicit production lists of abilities for targeting behavior; shared targeting systems should consume the cached ability definitions.
- An active ability presents a manual target cursor only when it is a single-target hostile *cast*. Queued weapon abilities (fire on the wearer's next landed auto-attack) and self-centered area abilities (originate on the caster) must NOT prompt for a target: in `feat.2da` they use `TARGETSELF=1` with `HostileFeat` cleared, and in C# they must not call `RequiresTarget()` (`ConfigureGeneratedWeaponAbility` already skips it for `IsQueuedWeaponAbility`). `tools/GenerateWeaponArchetypeImplementation.py` encodes this: a Combat active is `HostileFeat` only when it is neither self-targeting, queued (`CastingTime == "queued"`), nor a self-origin area. When adding or regenerating a queued or self-centered-area weapon active, verify its `feat.2da` row is `TARGETSELF=1`, then rebuild the haks and repack the module so the change deploys.

## Ability Icons

- Before adding, changing, generating, or renaming ability, feat, spell, or status-effect icons, read `SWLOR.Game.Server/Readmes/IconStandards.md` and follow it as the source of truth for artwork, semantic category, rank badges, and resource naming.
- Gameplay icon resrefs must be short, meaningful abbreviations within NWN's 16-character resource limit. Do not use opaque hash, collision, or generator suffixes such as random-looking letters or digits after the meaningful abbreviation.
- After adding or changing an ability icon referenced by `SWLOR_Haks/sw_2da/feat.2da` or `SWLOR_Haks/sw_2da/spells.2da`, run `powershell -ExecutionPolicy Bypass -File tools/GenerateCooldownIcons.ps1 -Force` to regenerate the `pr0_` through `pr5_` cooldown icon variants. This script must use ImageMagick output; do not replace it with a custom TGA writer.
- After adding, changing, generating, or renaming any gameplay icon manifest entry or gameplay icon resource, run `powershell -ExecutionPolicy Bypass -File tools/UpdateGameplayIconStandards.ps1 -AuditOnly` and fix every failure before handing off the work.

## Ability VFX

- Before choosing or changing perk, ability, status-effect, trap, or scripted creature VFX, consult `SWLOR.Game.Server/Readmes/VisualEffectSelection.md` and `SWLOR.Game.Server/Readmes/VisualEffectReference.csv`. Pick VFX by gameplay moment, visual group, colors, location, and screenshot reference rather than by constant name alone.
- Use the CSV `CSharpEnum` value in C# code. Use `BEAM` entries with `EffectBeam`, `FNF` entries for location/area bursts, `IMP` or `COM` entries for target impact feedback, `DUR` entries for persistent auras or field markers, and `EYES` entries only when the eye/head cue is the intended player-facing signal.

## Ability Damage

- When an ability applies `EffectDamage` with `ApplyEffectToObject`, wrap that call in `AssignCommand(source, () => ApplyEffectToObject(...))` using the damage source as the command object so the damage appears in the player's combat log.
