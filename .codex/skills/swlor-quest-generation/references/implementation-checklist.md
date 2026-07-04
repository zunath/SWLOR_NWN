# SWLOR Quest Implementation Checklist

## Intake Fields

For each quest, capture:

- Quest: `quest_id`, display name, required `repeatable` mode (`one-time`/`false`/`no`, `repeatable`/`true`/`yes`, or a named cadence such as `daily` that needs custom gating), guild/rank if any, prerequisite quest IDs, prerequisite key items.
- Giver: NPC name, UTC template resref, tag, dialogue resref or C# dialog class, target area resref, coordinates, facing.
- Objectives: kill group, collect item resref, quantity, producer requirement, trigger/placeable objective if any, and duplicate item justification when a proposed collect item already appears in a non-guild quest.
- Text: not eligible, offer, acceptance, in-progress reminder, turn-in, completion, completed/repeat text, player replies, journal text for each state.
- Dialogue flow: opening beat, optional player questions, NPC answers, accept reply, decline reply, reminder path, ready-to-turn-in path, and completed path. For major, chain, capstone, faction, or signature quests, include optional branches for motive, stakes, local lore, directions, target context, proof, or tactical advice.
- Creative brief: NPC role, NPC voice, local pressure, stakes, lore anchors, why this NPC asks the player, and how the reward is justified in-world.
- Rewards: XP, credits, items, key items, GP, faction standing/points, completion achievement, selectable vs automatic.
- Placement: fixed NPC placement, enemy spawn table, enemy spawn coordinates or area random count.

If a field is missing and cannot be resolved by searching the repo, ask one question at a time and include a recommended answer.

Before accepting a collect objective, search `SWLOR.Game.Server/Feature/QuestDefinition` for existing `AddCollectItemObjective(...)` usage of the same item resref. Avoid duplicating items across non-guild quests unless the repetition is intentional and justified by gameplay or story. Good reasons include quest-chain continuity, tutorial reinforcement, a deliberate repeatable resource sink, faction or local supply pressure, or a different acquisition route. Weak reasons like "the item already exists" or "it is nearby" should trigger a different item or objective.

## Code Surfaces

Quest definition:

- Add to `SWLOR.Game.Server/Feature/QuestDefinition/<AreaOrPlanet>QuestDefinition.cs`.
- Create a private method per quest.
- Call the method from `BuildQuests()`.
- Use `.Create("quest_id", "Quest Name")`, then flags, prerequisites, states, objectives, rewards, hooks.
- For major, chain, capstone, faction, or signature quest lines, add an active `AchievementType` entry and grant it from the final quest's `.OnCompleteAction(...)` with `Achievement.GiveAchievement(...)` unless the user explicitly opts out.

NPC group:

- Add a `[NPCGroup("Display Name")]` entry to `SWLOR.Game.Server/Service/NPCService/NPCGroupType.cs` only when no group already matches.
- Use the next integer value.
- Set enemy `VarTable` `QUEST_NPC_GROUP_ID` to that integer in `Module/utc/<enemy>.utc.json` or the relevant placed creature.

Dialogue:

- Legacy `.dlg.json` nodes use `ActionParams` and `ConditionParams` lists.
- `ActionParams` values for quest actions:
  - `action-accept-quest`: `quest_id`
  - `action-advance-quest`: `quest_id`
  - `action-request-quest-items`: `quest_id`
- `ConditionParams` values for quest visibility:
  - `condition-has-quest`: `quest_id`
  - `condition-on-quest-state`: `quest_id stateNumber [stateNumber...]`
  - `condition-completed-quest`: `quest_id [quest_id...]`
  - Negate by prefixing the key with `!`, for example `!condition-has-quest`.

NPC placement:

- UTC templates live in `Module/utc/<resref>.utc.json`.
- Fixed placed creatures live in `Module/git/<area>.git.json` under `Creature List.value`.
- Important fields: `FirstName`, `LastName`, `Conversation`, `Tag`, `TemplateResRef`, `VarTable`, `XPosition`, `YPosition`, `ZPosition`, `XOrientation`, `YOrientation`.
- Legacy dialogue uses the `Conversation` resref.
- C# dialog classes should use a local `CONVERSATION` variable only when the target object is already wired for SWLOR's C# dialog opener.

Spawn placement:

- Spawn table definitions live in `SWLOR.Game.Server/Feature/SpawnDefinition/*SpawnDefinition.cs`.
- Area-random spawns use area locals `CREATURE_SPAWN_TABLE_ID` and `CREATURE_SPAWN_COUNT`.
- Fixed spawn points use `WaypointList` entries whose `Tag` equals the spawn table ID.
- Spawn tables are cached by reflection and duplicate IDs are logged as errors.

## Dialogue State Matrix

Create or verify one path for each relevant player state:

- Not eligible: prerequisites missing, explain the nearest concrete requirement.
- Eligible and not accepted: offer the quest, expose any intended optional branches, and run `action-accept-quest` only from the explicit accept reply.
- Accepted and objectives incomplete: remind the player what to do.
- Accepted and collect objectives ready or partly ready: run `action-request-quest-items`.
- Accepted and final state ready: run `action-advance-quest`.
- Completed non-repeatable: acknowledge completion, no new accept action.
- Completed repeatable: show repeat offer with repeatable rewards clearly implied.

## Dialogue Quality Pass

Read `dialogue-and-content-standards.md` before writing quest text. Then verify:

- The NPC's role and local pressure shape the conversation.
- The opening is not a default generic greeting unless the NPC's role demands it.
- The offer, accept response, reminder, and completion lines have different sentence shapes.
- Player replies offer intentful choices, not only generic accept/decline buttons.
- Major, chain, capstone, faction, and signature quests have a specific conversation flow with optional non-action branches for lore, directions, target context, stakes, or tactical advice.
- Optional dialogue branches return to actionable choices and do not accidentally accept, advance, or request quest items.
- Local nouns match existing repo content: area names, NPC names, enemy groups, item names, guilds, key items, and prior quest events.
- Reused collect items have an explicit reason in dialogue, journal text, or implementation notes.
- Prerequisite failure text explains the nearest useful next step.
- Journal text stays clear and objective-oriented while dialogue carries personality.
- Rewards are explained as pay, access, proof, trust, hazard compensation, guild credit, or another in-world reason.

## Journal Text Rules

- State 1 should tell the player what to do immediately after accepting.
- Intermediate states should name the next objective or return target.
- Final state should tell the player who or what to return to for completion.
- Include area names and NPC names when they reduce ambiguity.
- Keep journal text factual and objective-oriented; put personality in dialogue.

## Validation Commands

Run targeted JSON parsing for touched module files:

```powershell
Get-Content 'Module\dlg\<dialog>.dlg.json' -Raw | ConvertFrom-Json > $null
Get-Content 'Module\utc\<npc>.utc.json' -Raw | ConvertFrom-Json > $null
Get-Content 'Module\git\<area>.git.json' -Raw | ConvertFrom-Json > $null
```

Search for duplicates and missing references:

```powershell
rg -n -U 'Create\s*\(\s*"<quest_id>"' SWLOR.Game.Server\Feature\QuestDefinition
rg -n -U '"value"\s*:\s*"<dialog_resref>"' Module\dlg Module\utc Module\git
rg -n -U '"value"\s*:\s*"<npc_or_item_resref>"' Module
rg -n -U '"<npc_or_item_resref>"' SWLOR.Game.Server
```

Build:

```powershell
dotnet build SWLOR.Game.Server\SWLOR.Game.Server.csproj --no-restore
```

Pack the module only when the handoff requires a refreshed `.mod`:

```powershell
Push-Location Module
.\PackModule.cmd
Pop-Location
```

## Common Failure Checks

- Quest ID created in C# but dialogue snippet points to a different ID.
- Collect objective created but dialogue never calls `action-request-quest-items`.
- Kill objective created but enemy has no `QUEST_NPC_GROUP_ID`.
- New `NPCGroupType` value added but enemy `VarTable` uses the wrong integer.
- Reward item or collect item resref does not exist in `Module/uti`.
- Quest giver UTC exists but no placed creature exists in the target area's `Creature List`.
- Placed creature has a dialogue resref but the `.dlg.json` file does not exist.
- Repeatable quest accidentally grants a permanent key item on every completion.
- Dialogue uses a static generated pattern across multiple NPCs.
- Major quest dialogue is only a functional accept/remind/turn-in kiosk with no optional player questions or NPC-specific flow.
- Optional dialogue branches carry quest actions accidentally instead of returning to accept, decline, item-request, or turn-in choices.
- Prerequisite gates exist mechanically but have no player-facing explanation.
- Reward amounts or item rewards drift from nearby quest/guild scale without an explicit reason.
- A new non-guild collect quest reuses items from existing non-guild quests without a clear gameplay or story reason.
- A major, chain, capstone, faction, or signature quest line completes without granting its specific completion achievement.
