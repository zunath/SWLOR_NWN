---
name: swlor-quest-generation
description: Generate SWLOR NPC-delivered quest content from concepts or batch specs. Use when Codex needs to create, expand, or repair quests end to end in this repository, including quest definitions, NPC dialogue and player replies, journal text, rewards, prerequisite quests or key items, key item grants/removals, kill or collect objectives, lore-appropriate content hooks, NPC/enemy placement in Module JSON, spawn table wiring, and validation.
---

# SWLOR Quest Generation

## Core Rule

Build quests in the real SWLOR artifacts, not in a side memo. Use the current repo patterns first:

- Quest data: `SWLOR.Game.Server/Feature/QuestDefinition/*QuestDefinition.cs`
- Quest service APIs: `SWLOR.Game.Server/Service/Quest.cs` and `Service/QuestService/*`
- Legacy dialogue and snippet wiring: `Module/dlg/*.dlg.json`, `Feature/SnippetDefinition/QuestSnippetDefinition.cs`
- NPC templates and placement: `Module/utc/*.utc.json`, `Module/git/<area>.git.json`
- Enemy groups and spawn tables: `NPCGroupType.cs`, `Feature/SpawnDefinition/*SpawnDefinition.cs`, enemy `Module/utc/*.utc.json`

Before inventing a pattern, inspect nearby quests for the same planet, hub, guild, reward tier, and objective style.

When the concept is underspecified, grill the request one dependency at a time. Ask one question, include a recommended answer, and answer it yourself by searching the repo when the repository can provide the answer.

## Workflow

1. Normalize the spec.
   - For many quests, use `assets/quest-batch-template.csv`.
   - Require at least: quest ID, quest name, repeatable mode, giver NPC, area resref or target area, objective type and target, journal states, dialogue beats, rewards, prerequisites, NPC role, NPC voice, local context, stakes, and any lore anchors.
   - If a blocker cannot be resolved from the repo, ask one question at a time and include a recommended answer.
   - Treat the spec as both an implementation contract and a creative brief. Resolve mechanics, then resolve why this NPC asks for this work in this location.

2. Inventory existing content.
   - Search for matching area, NPC, quest ID, item resref, NPC group, key item, and dialogue resref.
   - Search existing `AddCollectItemObjective(...)` usage for each proposed collect item. Avoid reusing the same item in a new non-guild quest unless there is a clear gameplay or story reason, such as a quest-chain callback, a deliberate repeatable resource loop, tutorial reinforcement, faction supply pressure, or a different acquisition route.
   - If duplicate item use is justified, write the reason into the working notes, quest dialogue, journal context, or intake `duplicate_item_reason` field. If the reason is weak, pick a different item or objective.
   - Reuse existing templates, groups, items, and reward scale when they fit.
   - Keep quest IDs lower snake case and unique across all `QuestBuilder.Create(...)` calls.
   - Read at least one nearby quest definition and one nearby `.dlg.json` conversation for the same planet, faction, or hub before writing new dialogue.

3. Implement the quest definition.
   - Add the quest to the appropriate planet/guild definition, or create a new `IQuestListDefinition` only when there is no appropriate owner.
   - Add a private method per quest and call it from `BuildQuests()`.
   - For large generated or batch quest sets, follow the existing concrete `IQuestListDefinition` pattern: split the quests into concrete definition classes by planet, guild, skill, area group, or content package; have each `BuildQuests()` call named private quest methods that call `QuestBuilder` directly. Do not hide quest setup behind partial classes, abstract quest bases, shared `BuildQuest(line, step)` helpers, catalog-driven quest models, generated one-line records, or central capstone metadata/catalog classes. Capstone quest definitions must be organized by the skill that gates the quest line, such as `VibrobladeCapstoneQuestDefinition`, and the line-specific quest IDs, NPC groups, proof key items, quest givers, enemy resrefs, encounter waypoint tags, and related asset constants must live in the owning skill quest definition file. Keep those constants private by default, and expose them as `internal` only when another production server type needs the value, such as spawn tables or perk gates. Do not make quest constants public for tests, area-builder handoff, or convenience; tests should validate built quest, perk, module, and workbook artifacts instead of depending on quest-definition constant visibility.
   - Use `QuestBuilder` states in order. State 1 is the accepted state; final state is the turn-in/completion state.
   - Consume the intake `repeatable` field deterministically: omit `.IsRepeatable()` for one-time/false/no quests, add `.IsRepeatable()` for repeatable/true/yes quests, and stop to design a custom gate if a daily/cooldown cadence is requested because `QuestBuilder` has no built-in daily helper.
   - Use built-in objectives before custom code: `AddKillObjective(...)` and `AddCollectItemObjective(...)`.
   - For collect objectives, prefer items that are not already used by nearby non-guild quests unless the quest explicitly benefits from repetition.
   - Use built-in rewards before custom code: credits, XP, item, key item, GP, faction standing, faction points.
   - Use `PrerequisiteQuest(...)`, `PrerequisiteKeyItem(...)`, and `PrerequisiteSkill(...)` for acceptance gates. Add custom prerequisites only when the builder lacks the needed gate.
   - For beast capstone quest lines, gate acceptance on the master's `SkillType.BeastMastery` rank, not on the currently active beast's level or role. Beast role belongs to beast perk/content identity, not quest prerequisites.
   - Match rewards to nearby quests of similar difficulty, objective count, and repeatability. Do not add repeatable permanent key item rewards unless the current system already models that exact pattern.
   - Quest and capstone generation must not add or modify property structures, fabrication furniture recipes, `StructureType`, `structure_####` or `bpstructure####` UTIs, or structure/furniture loot entries unless the user explicitly requests player-placeable property rewards. If those rewards are explicitly requested, treat them as a separate property/fabrication change and follow the existing `StructureType` enum layout and `structure_####` resref convention. Ordinary furniture/placeable structures belong at the end of the non-building section before `// Buildings start here (5000+)`; do not put them in the 5000+ building/layout range.
   - For major, chain, capstone, faction, or signature quest lines, define a specific completion achievement and grant it from the final quest completion hook using the existing achievement service unless the user explicitly opts out.
   - For temporary proof key items in capstone or signature quest lines, use lore-appropriate artifact names tied to the location, faction, enemy, or trial context. Avoid generic generator labels such as `Field Report`, `Calibration Core`, `Broken Seal`, `Command Mark`, or vague `Mark` names.
   - For capstone or high-level signature enemies, give each generated enemy a first-class reusable NPC signature ability plus a distinct support ability package. Reusable NPC signature abilities live directly under `SWLOR.Game.Server/Feature/AbilityDefinition/NPC`, not in a capstone-specific subfolder. Do not brand reusable abilities, items, icons, feat labels, TLK text, or generated asset display names with the quest line, capstone perk, boss, NPC name, source category, combat profile, or skill family. Name reusable abilities for the behavior or effect only, such as `SustainBurnAbilityDefinition`; keep capstone/perk names only where they represent player-facing quest progression or the final boss's actual unlock.
   - Generated capstone `NPCGroupType` identifiers must use the planet prefix from the line's area group, such as `Viscara_` or `Dathomir_`, not an internal `Capstone_` prefix.
   - Capstone-tier NPC signature abilities must use the existing `RecastGroup.Capstone` cooldown bucket. Do not add duplicate recast groups such as `CapstoneSignature` or `NPCSignature` unless a separate gameplay cooldown is explicitly designed and approved.
   - Keep humanoids on humanoid/tech/Force/command/weapon abilities, keep beasts on beast-appropriate abilities, wire signature abilities through feat/spell rows, TLK, icons, cooldown icons, and blueprint feats, and document the reusable signature ability plus matching resistance profile in the World NPCs Bible rows.

4. Write dialogue.
   - Read `references/dialogue-and-content-standards.md` before creating or rewriting quest dialogue, player replies, journal text, or prerequisite/completion text.
   - Prefer legacy `.dlg.json` plus snippets for ordinary NPC quest offers and turn-ins.
   - Use these snippet keys in `ActionParams`: `action-accept-quest`, `action-advance-quest`, `action-request-quest-items`.
   - Use these snippet keys in `ConditionParams`: `condition-has-quest`, `condition-on-quest-state`, `condition-completed-quest`; prefix with `!` for negation when needed.
   - Use a C# `DialogDefinition` only for dynamic runtime menus or logic-heavy conversations.
   - Dialogue should cover: not eligible, offer, accept response, in-progress reminder, ready-to-turn-in, completion, repeat/completed state, and any prerequisite explanation.
   - Do not ship quest-giver dialogue as a purely functional accept/remind/turn-in kiosk unless the quest is intentionally trivial or the NPC role demands it. For major, chain, capstone, faction, or signature quests, give the conversation a deliberate flow with optional player branches that reveal motive, stakes, local lore, directions, target context, or tactical advice before acceptance.
   - Do not write every NPC in the same "greeting, request, acceptance, reminder, thanks" rhythm. Pick the structure from the NPC's job, mood, leverage, and relationship to the objective.
   - Keep the mechanical snippet actions on clear accept, item-request, and turn-in replies while placing flavor and optional questions on safe non-action branches.
   - Keep journal text objective-oriented. Put personality, subtext, hesitation, pressure, and local flavor in the conversation.

5. Wire NPCs and placement.
   - For fixed quest givers, create or update `Module/utc/<npc>.utc.json` and add/update a placed creature in `Module/git/<area>.git.json` under `Creature List.value`.
   - For legacy `.dlg` conversations, set the creature `Conversation` field to the dialogue resref.
   - For C# conversations, use the local `CONVERSATION` variable with the C# dialog class name only after confirming an existing object uses that path.
   - Set `TemplateResRef`, `Tag`, localized name fields, coordinates, and orientation deliberately. Do not duplicate an existing tag unless the existing area already does so intentionally.

6. Wire kill targets and spawn points.
   - Add a new `[NPCGroup("Name")]` enum entry in `NPCGroupType` when no existing group matches.
   - Set the enemy blueprint or placed enemy `VarTable` entry `QUEST_NPC_GROUP_ID` to the enum integer.
   - Add or update the planet spawn table with the enemy resref and frequency.
   - Define spawn tables in the owning planet or location spawn definition using the existing concrete fluent `SpawnTableBuilder` style: one named private method per table, `_builder.Create("TABLE_ID", "Display Name")`, then explicit `.AddSpawn(...).WithFrequency(...).RandomlyWalks().ReturnsHome()` chains. Do not create quest-category spawn buckets or hide generated spawn rows behind helper methods, catalogs, records, or constant-only indirection just to reduce repetition.
   - Keep spawn and loot table ID constants private to the owning definition by default. Expose them as `internal` only when another production server type truly needs direct access; tests, area-builder handoff notes, and documentation should use built artifacts or literal expected IDs instead of forcing wider visibility.
   - Place spawn tables through area locals (`CREATURE_SPAWN_TABLE_ID`, `CREATURE_SPAWN_COUNT`) for random area placement, or waypoint tags equal to the spawn table ID for fixed spawn points.
   - When adding fixed-placement spawn tables, also create matching `Module/utw` waypoint blueprints and add them to `Module/itp/waypointpalcus.itp.json`. The blueprint `Tag` should be the spawn table ID; `TemplateResRef` should be a short unique resref.
   - For quest-specific teleport destinations or boss spawn locations, create palette waypoint blueprints too. Leave actual area placement to the area builder unless the task explicitly asks for area placement.
   - For unique on-demand quest encounter activators, prefer placed world instances configured with `OnUsed = quest_enc` and the required `QUEST_*` locals. Do not add one-off marker placeables to the placeable blueprint palette unless the user or area builder explicitly asks for a reusable palette blueprint. Tests should verify the placed area instance, not force a palette entry.

7. Validate.
   - Parse every touched Module JSON file with PowerShell `ConvertFrom-Json`.
   - Search for missing or duplicate quest IDs, dialogue resrefs, NPC tags, item resrefs, and NPC groups.
   - Run the dialogue quality pass from `references/dialogue-and-content-standards.md`.
   - Run `dotnet build SWLOR.Game.Server\SWLOR.Game.Server.csproj --no-restore`.
   - If Module JSON changed and the handoff needs a packed module, run `Module\PackModule.cmd` from the `Module` directory.

## References

- Read `references/implementation-checklist.md` when implementing actual quests.
- Read `references/dialogue-and-content-standards.md` when writing or reviewing quest text.
- Use `assets/quest-batch-template.csv` as the intake format for batch generation.
