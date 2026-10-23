# Agent Rules

Shared rules for every coding agent (Codex reads this file; Claude Code imports it through `CLAUDE.md`). This file stays thin: rules that apply to every task, then pointers to the guides that hold the details. Add new rules to the owning guide and keep at most one line here. `Readmes/` means `SWLOR.Game.Server/Readmes/`.

## Project

SWLOR is a live Neverwinter Nights: Enhanced Edition persistent world. Server logic is C# on .NET 10 through NWNX DotNet. Changes reach real players, and the repository is public.

- `SWLOR.Game.Server/`: `Core/` NWScript and NWNX wrappers, `Service/` static game services, `Entity/` Redis models, `Feature/` builder-based content, `ConversationData/` dialogue graphs, `Readmes/` guides. Tests: `SWLOR.Game.Server.Tests/` (NUnit) and `SWLOR.Game.Server.EngineTests/` (in-engine).
- `Module/` module JSON; `SWLOR_Haks/` submodule (2DA, TLK, models, textures, icons); `design/bible/` Design Bible workbooks (source of truth for balance and descriptions); `tools/` generators and audits; `SWLOR.Toolset*/` module editor.

## How to Work

- **Do what was asked, all of it, and nothing else.** Work every item until it is fixed and verified; if one is blocked, say why and finish the rest. Follow-up chores (deploy steps, keeping the repo's Design Bible in sync, cleanup) are part of the job; the owner uploads Bible changes to Google, never agents. Ask before changing shared code, unrelated systems, or behavior the user did not mention; report unrequested defects instead of fixing them.
- **Check instead of assuming.** Look up paths, values, helpers, and how similar features work before using them. Balance numbers come from the Design Bible or code, never extrapolation. For a bug report, find the exact reported object first (search for the label in the screenshot).
- **Follow existing standards; ask before departing from them.** A new tier, a new direction, or a value outside the established standards (enemy stats, item stats, crafting requirements, and similar) needs the user's approval first.
- Ask only for real decisions, once, with a recommendation. If an approach fails twice, change approach or report the blocker with evidence.
- **Deploy steps are not code problems.** Module JSON needs a repack, hak data needs a hak rebuild, C# needs a server deploy. Check what is deployed before changing code for stale in-game behavior.
- **Report honestly.** "Done" means built, tested, and verified with client evidence. State anything failed, skipped, or unverified, with the reason.

## Response Style

- Short and direct; lead with the result. No preamble, restating, narration of steps, closing summary, or recap of a diff the user can read. Plain language, no filler or praise.
- A short paragraph or a few bullets; tables only to compare values across rows. Give full explanations when asked.

## Every Task

- Only playable species on the normal phenotype matter (`Readmes/ContentStandards.md` Characters).
- **Client evidence:** every change with an in-game effect needs NWN client evidence through the established procedure unless the user explicitly waives it; a change with no in-game surface (toolset app, CLI, docs, agent rules) says so in the handoff instead (`Readmes/InEngineVisualVerification.md`).
- **Tests:** build once with `-p:RunPostBuildEvent=Never -v q -clp:ErrorsOnly`, then `dotnet test <project> --no-build --filter "FullyQualifiedName~<Class>"` for the tests that guard the change. Run the full suite only when asked or the change is broad. Report the filters you ran.
- **Shared machine:** several Claude and Codex sessions run at once. Work in your own worktree and branch, never stash, reset, or run repo-wide generators in a shared checkout, and stop only processes you started (`Readmes/AgentEnvironment.md`).
- **Clean up as you go:** delete generated files (renders, models, textures, HAK and module copies, client profiles, containers) once nothing needs them, without being asked. Keep only the evidence the user needs.
- **Token economy:** never read a whole `Module/` JSON file (some reach 25 MB); search instead. Read only the guide sections you need, and keep command output small.
- **Pull requests:** one branch and pull request per request, from up-to-date `origin/master`. Request each Codex review with `tools/RequestCodexReview.ps1`, which waits for the answer, and keep fixing in the same turn until a review of the current head finds nothing; never wait on a rate-limited CodeRabbit; resolve every thread and every merge conflict with the base (`Readmes/PullRequests.md`).
- **Game text encoding:** punctuation is ASCII (straight quotes, `-`, `...`), never curly quotes or dashes pasted from documents or AI chats. Keep accented letters, but in Module JSON store them as single Windows-1252 bytes, never UTF-8, or the game shows `Ã©` and `â€™` (`Readmes/CodingStandards.md`).
- **Public repository:** keep secrets and RP XP OOC detection specifics out of commits, pull requests, and player-facing text.

## Guides

Read the guide before working in its area. `Readmes/README.md` lists every guide.

- Coding style (one type per file, one enum per file), renames (an enum rename needs a data migration), Module JSON, StatType design, TLK and 2DA data: `Readmes/CodingStandards.md`
- Items, enemies, loot, balance, dungeons, ship access, phenotypes: `Readmes/ContentStandards.md`
- Skills, perk lines, cross-skill design, progression, descriptions, and balance evidence: `Readmes/SkillPerkStandards.md`
- Migrations, persistence, and enum renames: `Readmes/Migrations.md`
- Pull requests and code review: `Readmes/PullRequests.md`
- Worktrees, haks submodule, git network, disk hygiene, builds, local deploy, cloud sessions: `Readmes/AgentEnvironment.md`
- Client evidence (offline and live server): `Readmes/InEngineVisualVerification.md`
- Engine tests: `Readmes/EngineTesting.md`
- Active abilities and targeting: `Readmes/AbilityTargeting.md`. Icons: `Readmes/IconStandards.md` (key items: `Readmes/KeyItemIconStandards.md`). VFX: `Readmes/VisualEffectSelection.md`
- Quests: `Readmes/QuestContentValidation.md`. Conversations: `Readmes/Conversations.md`. Player names: `Readmes/PlayerIdentity.md`. Player messages: `Readmes/PlayerFeedback.md`
- Item models (helmet model 223 is reserved): `Readmes/ItemAppearanceRules.md`. Item blueprints and pickers: `Readmes/EconomyRestrictedItems.md`. Creature HP: `Readmes/NpcHitPoints.md`
- NUI windows: `Readmes/NuiLayoutRules.md`, `Readmes/GuiWindowAuthoring.md`
- Design Bible workbooks (never edit with `openpyxl`): `Readmes/DesignBibleWorkbookRules.md`
- Module editor: `SWLOR.Toolset/README.md`

## Skills

Skills live only in `skills/`, shared by both agents; edit them in place and keep them agent-neutral. Read the matching `SKILL.md` before working in its scope or when the user names it, and follow its referenced files as needed.

- Beasts (definitions, taming and incubation, mutation paths, DNA and TLK assets, generators): `skills/swlor-beast-generation/SKILL.md`
- Bug reports ("run the bug report process": Discord bug-forum triage, reactions, fix sessions, PR links, closing): `skills/swlor-bug-report-triage/SKILL.md`
- Blaster models (imports, original designs, grip, scale, muzzle alignment, textures, inventory art): `skills/swlor-blaster-models/SKILL.md`
- NUI windows (tabs, tables, modals, bindings): `skills/swlor-gui-window-generation/SKILL.md`
- Quests (NPC quests, dialogue, journals, objectives, rewards, prerequisites, placement): `skills/swlor-quest-generation/SKILL.md`
- Rare spawns and named elites (stats, unique rare loot, spawn assets): `skills/swlor-rare-spawn-generation/SKILL.md`
- SWLOR Wiki release notes (creating, updating, finalizing): `skills/swlor-release-notes/SKILL.md`
- Player feedback logs (incremental summaries, master comparison, deployment status): `skills/swlor-feedback-review/SKILL.md`

## Rule Ownership and Read-Only Areas

- Shared rules go in this file or its guides, never in `CLAUDE.md` or agent memory.
- `C:\Projects\unified` is read-only reference material. Never change it.
