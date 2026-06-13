---
name: swlor-quest-generation
description: Generate SWLOR NPC-delivered quests from concept or batch specs. Use when Codex needs to create or modify quests end to end in this repository, including quest definitions, NPC dialogue text, journal text, rewards, prerequisites, key items, kill or collect objectives, NPC/enemy placement in Module JSON, spawn table wiring, and validation.
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

## Workflow

1. Normalize the spec.
   - For many quests, use `assets/quest-batch-template.csv`.
   - Require at least: quest ID, quest name, repeatable mode, giver NPC, area resref or target area, objective type and target, journal states, dialogue beats, rewards, and prerequisites.
   - If a blocker cannot be resolved from the repo, ask one question at a time and include a recommended answer.

2. Inventory existing content.
   - Search for matching area, NPC, quest ID, item resref, NPC group, key item, and dialogue resref.
   - Reuse existing templates, groups, items, and reward scale when they fit.
   - Keep quest IDs lower snake case and unique across all `QuestBuilder.Create(...)` calls.

3. Implement the quest definition.
   - Add the quest to the appropriate planet/guild definition, or create a new `IQuestListDefinition` only when there is no appropriate owner.
   - Add a private method per quest and call it from `BuildQuests()`.
   - Use `QuestBuilder` states in order. State 1 is the accepted state; final state is the turn-in/completion state.
   - Consume the intake `repeatable` field deterministically: omit `.IsRepeatable()` for one-time/false/no quests, add `.IsRepeatable()` for repeatable/true/yes quests, and stop to design a custom gate if a daily/cooldown cadence is requested because `QuestBuilder` has no built-in daily helper.
   - Use built-in objectives before custom code: `AddKillObjective(...)` and `AddCollectItemObjective(...)`.
   - Use built-in rewards before custom code: credits, XP, item, key item, GP, faction standing, faction points.
   - Use `PrerequisiteQuest(...)` and `PrerequisiteKeyItem(...)` for acceptance gates. Add custom prerequisites only when the builder lacks the needed gate.

4. Write dialogue.
   - Prefer legacy `.dlg.json` plus snippets for ordinary NPC quest offers and turn-ins.
   - Use these snippet keys in `ActionParams`: `action-accept-quest`, `action-advance-quest`, `action-request-quest-items`.
   - Use these snippet keys in `ConditionParams`: `condition-has-quest`, `condition-on-quest-state`, `condition-completed-quest`; prefix with `!` for negation when needed.
   - Use a C# `DialogDefinition` only for dynamic runtime menus or logic-heavy conversations.
   - Dialogue should cover: not eligible, offer, accept response, in-progress reminder, ready-to-turn-in, completion, repeat/completed state, and any prerequisite explanation.

5. Wire NPCs and placement.
   - For fixed quest givers, create or update `Module/utc/<npc>.utc.json` and add/update a placed creature in `Module/git/<area>.git.json` under `Creature List.value`.
   - For legacy `.dlg` conversations, set the creature `Conversation` field to the dialogue resref.
   - For C# conversations, use the local `CONVERSATION` variable with the C# dialog class name only after confirming an existing object uses that path.
   - Set `TemplateResRef`, `Tag`, localized name fields, coordinates, and orientation deliberately. Do not duplicate an existing tag unless the existing area already does so intentionally.

6. Wire kill targets and spawn points.
   - Add a new `[NPCGroup("Name")]` enum entry in `NPCGroupType` when no existing group matches.
   - Set the enemy blueprint or placed enemy `VarTable` entry `QUEST_NPC_GROUP_ID` to the enum integer.
   - Add or update the planet spawn table with the enemy resref and frequency.
   - Place spawn tables through area locals (`CREATURE_SPAWN_TABLE_ID`, `CREATURE_SPAWN_COUNT`) for random area placement, or waypoint tags equal to the spawn table ID for fixed spawn points.

7. Validate.
   - Parse every touched Module JSON file with PowerShell `ConvertFrom-Json`.
   - Search for missing or duplicate quest IDs, dialogue resrefs, NPC tags, item resrefs, and NPC groups.
   - Run `dotnet build SWLOR.Game.Server\SWLOR.Game.Server.csproj --no-restore`.
   - If Module JSON changed and the handoff needs a packed module, run `Module\PackModule.cmd` from the `Module` directory.

## References

- Read `references/implementation-checklist.md` when implementing actual quests.
- Use `assets/quest-batch-template.csv` as the intake format for batch generation.
