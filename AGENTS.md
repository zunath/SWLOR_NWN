# Agent Rules

This is the shared rule set for every coding agent. Codex reads it directly; Claude Code imports it through `CLAUDE.md`. Put cross-agent rules here, never in an agent-specific file.

Keep this file short. Codex stops reading project instructions at 32 KiB, and every line here competes for attention on every task. Put deep, topic-specific rules in `SWLOR.Game.Server/Readmes/` and add a one-line "read X before Y" pointer here.

## Project

SWLOR (Star Wars: Legends of the Old Republic) is a live Neverwinter Nights: Enhanced Edition persistent world. Server logic is C# on .NET 10, hosted through NWNX DotNet. Changes reach real players.

- `SWLOR.Game.Server/` - the game server. `Core/` wraps NWScript and NWNX, `Service/` holds the static game services (`Combat`, `Stat`, `Ability`, `Perk`, `Quest`, ...), `Entity/` holds Redis-persisted data models, `Feature/` holds content definitions built with builders (abilities, perks, items, recipes, quests, GUI windows, spawns), `ConversationData/` holds dialogue graphs, `Readmes/` holds system docs.
- `SWLOR.Game.Server.Tests/` - NUnit tests for pure logic and data. `SWLOR.Game.Server.EngineTests/` - `[EngineTest]` tests that run inside a live server; see `SWLOR.Game.Server/Readmes/EngineTesting.md`.
- `SWLOR.Toolset*/`, `SWLOR.NWN.Formats*/` - the module editor and NWN file-format libraries. `SWLOR.CLI/`, `SWLOR.Runner/`, `SWLOR.Web/`, `SWLOR.Admin/`, `SWLOR.BackgroundServices/` - tooling and supporting apps.
- `Module/` - module content as JSON (areas, blueprints, stores). `SWLOR_Haks/` - git submodule with 2DAs, TLK, models, textures, icons. `design/bible/` - Design Bible workbooks, the source of truth for balance and descriptions. `tools/` - generators and audit scripts.

Before writing code in an unfamiliar area, find two or three existing examples of the same kind of thing and follow them. Never call a method you have not confirmed exists; search for it first.

## How to Work

- **Do what was asked, all of it, and nothing else.** Work every item in a request until it is fixed and verified. If one item is genuinely blocked, say why for that item and finish the rest. Do not change shared code, unrelated systems, or behavior the user did not mention without asking first.
- **Check instead of assuming.** If a fact can be looked up in the repo (a path, a stat cap, a column, an enum value, an existing helper, how a similar feature works), look it up. Do not guess and do not fill gaps with plausible-sounding values.
- **Ask only for real decisions.** When a choice genuinely belongs to the user, ask once with a recommended option. When there is an obvious conventional choice, take it and mention it.
- **Do not loop.** If the same approach fails twice, stop and change approach or report the blocker with the evidence. Do not retry the same command or edit hoping for a different result.
- **Deploy steps are not code problems.** Module JSON changes need a module repack; 2DA, TLK, model, and icon changes need a hak rebuild; C# needs a server deploy. When a committed fix just has not been deployed, say which step is missing instead of coding a workaround.
- **Replace by adding, not repurposing.** When a stat, item property, 2DA row, TLK string, or enum value needs replacing, add a new one and leave the old definition untouched (hide it from builders only if asked).
- **Report honestly.** "Done" means built, tested, and verified. If something failed, was skipped, or was not verified, say so plainly with the reason.

## Response Style

- Keep responses short and direct. Lead with the result.
- No preamble, no restating the request, no closing summary, no recap of a diff the user can read.
- Do not narrate steps, tools, or reasoning that did not change the outcome. Report what the user needs to act on.
- Plain language. No marketing tone, filler adjectives, enthusiasm, or praise.
- Prefer a short paragraph or a few bullets. Use a table only to compare several values across several rows.
- When the user asks for an explanation (new skills, perk structure, design changes), give it; terse does not mean omitting what was asked for.

## Git and Pull Requests

- Start each request on a new feature branch from up-to-date `origin/master` unless the user names a different base. Never commit directly to `master`, and never mix one request's work into another request's branch.
- Put scratch files in a temp directory outside the repo.
- Use one branch and one pull request per request. Add follow-ups as new commits on the same branch. Open a second pull request only for a required submodule companion. Separate branches that edit the Design Bible `.xlsx` cannot be merged.
- When a parent pull request changes a submodule pointer (usually `SWLOR_Haks`), push the submodule branch, open its companion pull request against the branch matching the parent's base, and link the two pull requests in both descriptions. A pushed submodule branch alone is not a complete handoff.
- Addressing review findings means **resolving the review threads**, not just replying. After the fix is pushed, or after replying with a concrete reason the finding does not apply, resolve each thread with `gh api graphql` and the `resolveReviewThread` mutation. This applies to bot reviewers (CodeRabbit, Codex connector) too. Before calling pull request work done, confirm zero unresolved threads on the parent and every companion pull request.

## Build and Test

- Always pass `-p:RunPostBuildEvent=Never` when building. Without it, building `SWLOR.Game.Server` runs a slow deploy to the local NWN install.
- Build once, then run only the relevant tests without rebuilding:
  - `dotnet build SWLOR.Game.Server.Tests\SWLOR.Game.Server.Tests.csproj -p:RunPostBuildEvent=Never`
  - `dotnet test SWLOR.Game.Server.Tests\SWLOR.Game.Server.Tests.csproj --no-build --filter "FullyQualifiedName~<TestClass>|FullyQualifiedName~<OtherTestClass>"`
- Never run the full unfiltered suite unless the user asks or the change is genuinely broad (shared services, combat/stat infrastructure, enum or 2DA-wide edits, generator output). A localized change is verified by the tests that guard it. Say which test filters you ran.
- Many tests read `SWLOR_Haks`. In a git worktree the submodule may be uninitialized or a stale plain copy; check `git submodule status` before treating 2DA, TLK, or icon test failures as real.
- Some tests rewrite tracked files as a side effect (for example `CombatUpgradeBibleImplementationReview.csv`). Review that diff; do not commit it by accident.
- Do not start background jobs, watchers, dev servers, or detached processes unless the user asks or the task strictly requires one. Use foreground commands with bounded timeouts. If a long-lived process is necessary, track its PID, stop it before handing off, and report the cleanup.
- Deploy tooling: `Module\PackModule.cmd` repacks the module and `SWLOR_Haks\BuildHaks.cmd` rebuilds haks (needs `nwn_erf` on PATH). The user normally runs these. Run the server locally with `dotnet run --project SWLOR.Runner`.
- Do not add task-specific README files or writeups for routine work unless the user asks. Implementation notes and validation results go in the pull request description.

## Code Conventions

- New code puts one type per file, in a folder that matches its namespace and feature.
- `[NWNEventHandler]` and `ExecuteScript` script names are constants in `SWLOR.Game.Server/Core/ScriptName.cs`, never inline string literals.
- `DB.Search` returns at most 50 rows when the query has no `AddPaging`. Any query meant to load a complete set must call `AddPaging(<cap>, 0)`.
- Do not use internal initiative, milestone, or phase labels (such as `CombatUpgrade`) in new identifiers, filenames, namespaces, or comments. Use domain terms that describe the gameplay system.
- When an ability applies `EffectDamage` with `ApplyEffectToObject`, wrap it in `AssignCommand(source, () => ApplyEffectToObject(...))` so the damage appears in the player's combat log.
- Player-facing chat commands use `.Permissions(AuthorizationLevel.All)`. `AuthorizationLevel.Player` alone silently rejects DM and admin accounts with the generic "Invalid chat command" message.
- Builder-facing toolset dropdowns and pickers must never show 2DA placeholder rows (`DELETED`, `USER`, `UNUSED`, `INVALID*`, `Padding`, numbered `NULL` slots, and similar). Route 2DA options through `TwoDaChoicePolicy`, declare required columns in `TwoDaLookupTables`, fail closed when validity cannot be proven, and add regression coverage for each new 2DA-backed option source.

## Stat-Driven Gameplay

- Shared combat, ability, and status-effect code must not special-case specific perks or perk-specific status-effect classes. Model perk-driven behavior as `StatType` adjustments that shared systems read. Direct perk checks are only for ownership, unlock, purchase, UI, or progression gates.
- Declare `StatType` classification, polarity, and category with `StatTypeAttribute` on the enum entry, not with `if`/`switch` lists elsewhere.
- Attack Deflection, Shield Deflection, and Guard are separate mechanics. Attack and Shield Deflection are attack-roll outcomes that negate the hit and do not stack with each other. Guard is a damage-stage outcome that reduces damage and raises enmity. Never implement one with another's state, stats, logs, or triggers.

## Content Rules

- **Conversations:** authored dialogue lives only in `SWLOR.Game.Server/ConversationData/*.conversation.json`. Edit those graphs directly; never create DLG sources or regenerate graphs from legacy files. `Module/dlg/dmfi_universal.dlg.json` is the only native exception. Preserve NPC `Conversation` IDs and route through `dialog_start`. Read `SWLOR.Game.Server/Readmes/Conversations.md` first.
- **Player identity:** player-facing surfaces never show raw character names, account names, or CD keys; they go through the `PlayerName` service. Read `SWLOR.Game.Server/Readmes/PlayerIdentity.md` before touching any surface that displays a player.
- **Economy-restricted items:** NPC-only and unobtainable items must stay out of player search and economy surfaces. `Item.IsEconomyRestricted` is the only classifier; never hardcode resref lists. Read `SWLOR.Game.Server/Readmes/EconomyRestrictedItems.md` before adding item blueprints or item pickers.
- **NPC hit points:** a stat skin's `NPCHP` is the final maximum HP. Never write it to UTC `HitPoints` or pass it to `ObjectPlugin.SetMaxHitPoints`; NWN adds Vitality, Toughness, and Epic Toughness on top. Set `CurrentHitPoints`/`MaxHitPoints` to `NPCHP` and `HitPoints` to `NPCHP` minus those bonuses, apply runtime budgets only through `Stat.SetNPCMaxHitPoints` after Vitality is final, and run `powershell -ExecutionPolicy Bypass -File tools/NormalizeNpcHitPoints.ps1` after adding or restatting creatures.
- **Rebuild-era changes:** do not write one-off migrations just to remove or refund deleted perks, blueprints, or skills; the planned full character rebuild handles character-build data.

## Design Bible

- Read `SWLOR.Game.Server/Readmes/DesignBibleWorkbookRules.md` before editing any Design Bible workbook.
- Never edit a workbook with `openpyxl` or any library that rewrites the whole file. It drops cached formula results and silently breaks formula-backed tabs and tests such as `NPCEnemyBalanceAuditTests`. Edit target cells at the zip/XML level (`<c r="G31" s="..." t="inlineStr"><is><t>TEXT</t></is></c>`) and copy every other zip entry byte-for-byte.
- After editing `design/bible/SWLOR Design Bible - Combat Upgrade.xlsx`, run `powershell -ExecutionPolicy Bypass -File tools/UpdateCombatUpgradeAudit.ps1 -RefreshLocalBible`.

## TLK and 2DA

- New custom TLK strings take the first empty slot or gap in `SWLOR_Haks/sw_tlk/sw_tlk.tlk.json` before appending at the end. Regenerate `sw_tlk.tlk` after editing the JSON.
- 2DA references to custom TLK entries use `16777216 + tlkId` (entry `50003` is `16827219`). Raw IDs are only valid for base-game `dialog.tlk`. When moving or adding an entry, update every reference.
- `RecastGroup` short names are player-facing and at most 14 characters. Choose a meaningful label; never auto-truncate, and make generators fail when one is missing.

## Abilities

- Each distinct ability gets its own `*AbilityDefinition.cs` file and matching `IAbilityListDefinition` class named for it. Ranks of one ability share its file; unrelated abilities never share a file.
- Targeting metadata is declared on the ability definition through the builder, never in separate per-ability lists.
- Only single-target hostile casts and **aimed** areas ("in a line", "in a cone") show a target cursor. Queued weapon abilities and **self-centered** areas ("enemies within Nm") use `TARGETSELF=1` with `HostileFeat` cleared and never call `RequiresTarget()`. Every rank of an area ability needs its own `spells.2da` row and `Spell` value, never `Spell.Invalid`. Read `SWLOR.Game.Server/Readmes/AbilityTargeting.md` before adding or changing an active ability; `AreaAbilityTargetingTests` enforces it.
- **Icons:** read `SWLOR.Game.Server/Readmes/IconStandards.md` first. Icon resrefs are meaningful abbreviations within 16 characters, with no hash or generator suffixes. After changing an icon referenced by `feat.2da` or `spells.2da`, run `powershell -ExecutionPolicy Bypass -File tools/GenerateCooldownIcons.ps1 -Force` (ImageMagick output only). After changing any gameplay icon or manifest entry, run `powershell -ExecutionPolicy Bypass -File tools/UpdateGameplayIconStandards.ps1 -AuditOnly` and fix every failure.
- **VFX:** choose perk, ability, status-effect, trap, and creature VFX from `SWLOR.Game.Server/Readmes/VisualEffectSelection.md` and `VisualEffectReference.csv` by gameplay moment, colors, and location, not by constant name. Use the CSV `CSharpEnum` value: `BEAM` with `EffectBeam`, `FNF` for location bursts, `IMP`/`COM` for impacts, `DUR` for persistent auras, `EYES` only when the eye cue is intended.

## Agent Skills and Read-Only Areas

- Skills live in `.codex/skills/` and are mirrored to `.claude/skills/`. Edit only `.codex/skills/`, then run `powershell -ExecutionPolicy Bypass -File tools/SyncAgentSkills.ps1` (`-CheckOnly` to verify). `agents/openai.yaml` files are Codex-only and not mirrored. Keep skill text agent-neutral.
- The Unified solution (`C:\Projects\unified`) is read-only reference material. Never change it.
