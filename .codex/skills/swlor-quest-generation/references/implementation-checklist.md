# SWLOR Quest Implementation Checklist

## Intake Fields

For each quest, capture:

- Quest: `quest_id`, display name, required `repeatable` mode (`one-time`/`false`/`no`, `repeatable`/`true`/`yes`, or a named cadence such as `daily` that needs custom gating), guild/rank if any, prerequisite quest IDs, prerequisite key items.
- Giver: NPC name, UTC template resref, tag, dialogue resref or C# dialog class, target area resref, coordinates, facing.
- Objectives: kill group, collect item resref, quantity, producer requirement, trigger/placeable objective if any.
- Text: offer, acceptance, in-progress reminder, turn-in, completion, completed/repeat text, journal text for each state.
- Rewards: XP, credits, items, key items, GP, faction standing/points, selectable vs automatic.
- Placement: fixed NPC placement, enemy spawn table, enemy spawn coordinates or area random count.

## Code Surfaces

Quest definition:

- Add to `SWLOR.Game.Server/Feature/QuestDefinition/<AreaOrPlanet>QuestDefinition.cs`.
- Create a private method per quest.
- Call the method from `BuildQuests()`.
- Use `.Create("quest_id", "Quest Name")`, then flags, prerequisites, states, objectives, rewards, hooks.

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
- Eligible and not accepted: offer the quest and run `action-accept-quest`.
- Accepted and objectives incomplete: remind the player what to do.
- Accepted and collect objectives ready or partly ready: run `action-request-quest-items`.
- Accepted and final state ready: run `action-advance-quest`.
- Completed non-repeatable: acknowledge completion, no new accept action.
- Completed repeatable: show repeat offer with repeatable rewards clearly implied.

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
