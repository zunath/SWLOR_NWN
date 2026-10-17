# Agent Rules

Shared rules for every coding agent (Codex reads this file; Claude Code imports it through `CLAUDE.md`). It loads on every task: one line per rule here, details in the owning topic guide. `Readmes/` means `SWLOR.Game.Server/Readmes/`.

## Project

SWLOR (Star Wars: Legends of the Old Republic) is a live Neverwinter Nights: Enhanced Edition persistent world. Server logic is C# on .NET 10, hosted through NWNX DotNet. Changes reach real players, and the repository is public.

- `SWLOR.Game.Server/`: `Core/` wraps NWScript and NWNX, `Service/` holds the static game services, `Entity/` holds Redis-persisted models, `Feature/` holds builder-based content definitions, `ConversationData/` holds dialogue graphs, `Readmes/` holds system guides.
- Tests: `SWLOR.Game.Server.Tests/` (NUnit, pure logic and data) and `SWLOR.Game.Server.EngineTests/` (`[EngineTest]`, runs inside a live server).
- Tools: `SWLOR.Toolset*/` and `SWLOR.NWN.Formats*/` (module editor, NWN file formats); `SWLOR.CLI/`, `SWLOR.Runner/`, `SWLOR.CombatSim/`, `SWLOR.Web/`, `SWLOR.Admin/`, `SWLOR.BackgroundServices/`.
- Content: `Module/` (module as JSON), `SWLOR_Haks/` (submodule: 2DA, TLK, models, textures, icons), `design/bible/` (Design Bible workbooks, the source of truth for balance and descriptions), `tools/` (generators and audits).

## How to Work

- **Do what was asked, all of it, and nothing else.** Work every item until it is fixed and verified; if one is blocked, say why and finish the rest. Follow-up chores (deploy steps, Bible and Sheet syncs, cleanup) are part of the job, not to-dos for the user. Ask before changing shared code, unrelated systems, or behavior the user did not mention. Report unrequested defects instead of fixing them.
- **Check instead of assuming.** Look up paths, enum values, columns, helpers, and how similar features work. Never call a method you have not found. Balance numbers (stat caps, ranges) come from the Design Bible or code constants, never from extrapolation. In an unfamiliar area, follow two or three existing examples of the same kind of thing.
- For a bug report, pin down the exact reported object first: search for the label shown in the screenshot (doors use `LocName`). Player reports often misname the area.
- Ask only for real decisions, once, with a recommended option; take the obvious conventional choice and mention it. If an approach fails twice, change approach or report the blocker with evidence.
- **Deploy steps are not code problems.** Module JSON needs a repack, 2DA/TLK/model/icon changes need a hak rebuild, C# needs a server deploy. When in-game behavior looks stale, check what is deployed before changing code. Run the deploy steps this machine can run; name any it cannot (see `Readmes/AgentEnvironment.md`).
- **Replace by adding, not repurposing.** A replaced stat, item property, 2DA row, TLK string, or enum value gets a new definition; leave the old one untouched (hide it from builders only if asked).
- **Report honestly.** "Done" means built, tested, and verified. State anything failed, skipped, or unverified, with the reason.

## Response Style

- Short and direct; lead with the result. No preamble, no restating the request, no closing summary, no recap of a diff the user can read.
- Do not narrate steps or tools that did not change the outcome. Plain language: no marketing tone, filler, enthusiasm, or praise.
- A short paragraph or a few bullets; tables only to compare several values across rows. When the user asks for an explanation, give it in full.

## Token Economy

- Never read a whole `Module/` JSON file (area `.git.json` files reach 25 MB) or other large generated data. Search with `rg` or a script and read only the matching objects.
- Read only the sections of a long guide the task needs; long guides start with a section map.
- Keep command output small: build with `-v q -clp:ErrorsOnly`, filter tests, cap search results. Save long output to a temp file and search it instead of re-running the command.

## Environment

- Windows machine shared by several Claude and Codex sessions at once. Work in your own worktree and branch. Never `git stash`, `git checkout --`, reset, or run repo-wide generators in a checkout another session may be using.
- Stop only processes you started, by PID; never kill `nwmain`, `nwserver`, `dotnet`, or `docker` by name. Give servers and engine-test runs unique ports and compose project names.
- If `git fetch` or `push` fails with an SSL certificate error, add `-c http.sslBackend=schannel`. `gh` is authenticated.
- Worktrees start with `SWLOR_Haks` uninitialized, which breaks 2DA/TLK/icon tests. Check `git submodule status` (`-` missing, `+` stale) before trusting those results; setup steps are in `Readmes/AgentEnvironment.md`.
- Never start a search at `/` or a drive root. Use bounded foreground commands; start background jobs, watchers, servers, or GUI apps only when the task requires one, track the PID, and stop it before handoff.

## Git and Pull Requests

- Start each request on a new branch from up-to-date `origin/master` unless the user names a base. Never commit to `master`, and never mix one request's work into another's branch.
- One branch and one pull request per request; follow-ups are new commits on it. Open a second pull request only for a submodule companion. Separate branches that edit the Design Bible `.xlsx` cannot be merged.
- When a pull request moves a submodule pointer, push the submodule branch, open its companion pull request against the branch matching the parent's base, and link the two in both descriptions.
- Addressing review findings means fixing or answering each one, then **resolving its thread** (`gh api graphql`, `resolveReviewThread`), bots included. Also read each review body: the Codex connector sometimes puts findings there. Before calling pull request work done, confirm zero unresolved threads on the parent and every companion.
- Keep secrets and anti-abuse specifics (detection rules, limits, penalties, word lists, exploits) out of commits, pull requests, and player-facing text. Give those details to the user in chat.
- At handoff the working tree holds only the request's changes. Scratch files live in a temp directory outside the repo. Tests, generators, and icon tools can rewrite tracked files; review `git status` and revert side effects you did not intend. Stage explicit paths.
- Implementation notes and validation results go in the pull request description, not new README files.

## Build and Test

- Always build with `-p:RunPostBuildEvent=Never`; without it, building `SWLOR.Game.Server` deploys to the local NWN install.
- Build once, then run only the tests that guard the change, without rebuilding:
  - `dotnet build SWLOR.Game.Server.Tests\SWLOR.Game.Server.Tests.csproj -p:RunPostBuildEvent=Never -v q -clp:ErrorsOnly`
  - `dotnet test SWLOR.Game.Server.Tests\SWLOR.Game.Server.Tests.csproj --no-build --filter "FullyQualifiedName~<TestClass>|FullyQualifiedName~<OtherTestClass>"`
- Run the full suite only when asked or when the change is broad (shared services, combat/stat infrastructure, enum or 2DA-wide edits, generator output). Report which filters you ran.
- Before calling a failure pre-existing, reproduce it on the base commit in an equally initialized worktree.
- Visual changes (placements, models, textures, equipment, VFX, in-game UI) need real NWN client screenshots through the repository's background launcher. Follow `Readmes/InEngineVisualVerification.md`.
- Migration and persistence changes need in-engine coverage that starts from legacy data, runs the real conversion, then repeats use, save, and reload with ownership and quantity checks (`Readmes/EngineTesting.md`, "Persistence and migration coverage"). Droid item changes run the `DroidInventory` engine filter.
- Local server: `dotnet run --project SWLOR.Runner`. Module repack: `Module\PackModule.cmd`. Hak rebuild: `SWLOR_Haks\BuildHaks.cmd` (needs `nwn_erf` on PATH).

## Code Conventions

- New code puts one type per file, in a folder matching its namespace and feature. Each distinct ability gets its own `*AbilityDefinition.cs` and matching `IAbilityListDefinition` class; ranks of one ability share its file.
- Match the surrounding comment density. At most a one-line comment, only for a non-obvious constraint; no dates, incident history, or investigation notes in code.
- `[NWNEventHandler]` and `ExecuteScript` script names are constants in `SWLOR.Game.Server/Core/ScriptName.cs`, never string literals.
- `DB.Search` returns at most 50 rows without `AddPaging`. A query that must load a complete set calls `AddPaging(<cap>, 0)`.
- No internal initiative or phase labels (such as `CombatUpgrade`) in new identifiers, filenames, namespaces, or comments; use gameplay terms.
- Wrap `ApplyEffectToObject` of `EffectDamage` in `AssignCommand(source, ...)` so the damage reaches the player's combat log.
- Player chat commands use `.Permissions(AuthorizationLevel.All)`; `AuthorizationLevel.Player` silently rejects DM and admin accounts.
- Edit `Module/` JSON surgically. Never round-trip it through `json.dump` or a serializer: that rewrites float formatting and line endings across the file. Some files contain raw non-UTF-8 bytes (color tokens); preserve them.

## Gameplay Rules

- Shared combat, ability, and status-effect code never special-cases specific perks or perk status-effect classes. Model perk behavior as `StatType` adjustments that shared systems read; direct perk checks are only for ownership, unlock, purchase, UI, or progression gates.
- Declare `StatType` classification, polarity, and category with `StatTypeAttribute` on the enum entry, not with lists elsewhere.
- Attack Deflection, Shield Deflection, and Guard are separate mechanics. Attack and Shield Deflection are attack-roll outcomes that negate the hit and do not stack with each other; Guard is a damage-stage outcome that reduces damage and raises enmity. Never implement one with another's state, stats, logs, or triggers.
- Training sabers, training saberstaves, lightsabers, and saberstaves never appear in loot tables of any kind (including rare, boss, chest, and steal). Validate loot by blueprint base item type, not display name.
- A stat skin's `NPCHP` is the final maximum HP. Never write it to UTC `HitPoints` or pass it to `SetMaxHitPoints`; read `Readmes/NpcHitPoints.md` before adding or restatting creatures.
- Authored dialogue lives only in `SWLOR.Game.Server/ConversationData/*.conversation.json`. Never create DLG sources or regenerate graphs from legacy files.
- Player-facing surfaces never show raw character names, account names, or CD keys; route them through the `PlayerName` service.
- NPC-only and unobtainable items stay out of player search and economy surfaces; `Item.IsEconomyRestricted` is the only classifier; never hardcode resref lists.
- Do not write one-off migrations to remove or refund deleted perks, blueprints, or skills; the planned full character rebuild handles build data.

## Haks, TLK, and Design Bible

- New TLK strings take the first placeholder slot in `SWLOR_Haks/sw_tlk/sw_tlk.tlk.json` before appending. Never set a TLK entry's text to empty or delete a 2DA row: both reindex every entry after it. Regenerate `sw_tlk.tlk` after editing the JSON.
- 2DA references to custom TLK entries use `16777216 + tlkId` (entry `50003` is `16827219`); raw IDs are only for base-game `dialog.tlk`. Update every reference when moving an entry.
- `RecastGroup` short names are player-facing and at most 14 characters. Choose a meaningful label; never auto-truncate, and make generators fail when one is missing.
- Never edit a Design Bible workbook with `openpyxl` or any library that rewrites the whole file; it drops cached formula results. Follow `Readmes/DesignBibleWorkbookRules.md`.

## Topic Guides

Read the guide before working in its area. `Readmes/README.md` lists every guide.

- Worktrees, submodule setup, git and reviews, builds, local deploy: `Readmes/AgentEnvironment.md`
- Visual verification in the NWN client: `Readmes/InEngineVisualVerification.md`
- Engine tests, migrations, persistence: `Readmes/EngineTesting.md`
- Active abilities, targeting, native feat/spell rows: `Readmes/AbilityTargeting.md`
- Gameplay icons: `Readmes/IconStandards.md`. Visual effects: `Readmes/VisualEffectSelection.md`
- Conversations: `Readmes/Conversations.md`. Player names: `Readmes/PlayerIdentity.md`. Player messages: `Readmes/PlayerFeedback.md`
- Item blueprints and item pickers: `Readmes/EconomyRestrictedItems.md`
- Creature stats and hit points: `Readmes/NpcHitPoints.md`
- NUI windows: `Readmes/NuiLayoutRules.md`, `Readmes/GuiWindowAuthoring.md`
- Design Bible workbooks: `Readmes/DesignBibleWorkbookRules.md`
- Module editor (toolset): `SWLOR.Toolset/README.md`

## Agent Skills and Read-Only Areas

- Skills live in `.codex/skills/` and are mirrored to `.claude/skills/`. Edit only `.codex/skills/`, then run `powershell -ExecutionPolicy Bypass -File tools/SyncAgentSkills.ps1` (`-CheckOnly` to verify). `agents/openai.yaml` files are Codex-only. Keep skill text agent-neutral.
- Shared rules go in this file, never in `CLAUDE.md` or agent memory. When a user correction is a project-wide rule, add it here or to its topic guide.
- `C:\Projects\unified` is read-only reference material. Never change it.
