# SWLOR Quest Implementation Checklist

## Intake Fields

For each quest, capture:

- Quest: `quest_id`, display name, required `repeatable` mode (`one-time`/`false`/`no`, `repeatable`/`true`/`yes`, or a named cadence such as `daily` that needs custom gating), guild/rank if any, prerequisite quest IDs, prerequisite key items.
- Giver: NPC name, UTC template resref, tag, dialogue resref or C# dialog class, target area resref, coordinates, facing.
- Capstone quest lines must use one dedicated quest giver per capstone line. Area groups may be shared by up to three lines, but the player-facing trainer/requester must remain distinct for each line.
- Capstone quest lines must assign the unlocked capstone perk to the final boss. Add the granted feat/spell and `PERK_LEVEL_<perk id>` local when the perk uses stat bonuses, and add regression coverage that proves the boss blueprint carries the capstone.
- Beast capstone quest acceptance must require the master's `SkillType.BeastMastery` rank, not an active beast, active beast level, or active beast role. Beast role remains useful for perk-line identity, content packages, and enemy theming, but it is not a quest prerequisite.
- Generated capstone `NPCGroupType` identifiers must use the planet prefix from the line's area group, such as `Viscara_` or `Dathomir_`, not an internal `Capstone_` prefix.
- Capstone enemies must each have a first-class reusable NPC signature ability plus a distinct support ability package and a documented resistance profile. Humanoid enemies use humanoid, tech, Force, command, or weapon abilities; beast enemies use beast-appropriate attacks, roars, hides, or movement abilities. Add one ability definition per reusable signature ability directly under `SWLOR.Game.Server/Feature/AbilityDefinition/NPC`, wire feat/spell rows, TLK, source icons, cooldown icon variants, and blueprint feats, and fill World NPCs resistance adjustment cells numerically, using `0` for no adjustment.
- Capstone-tier NPC signature abilities must use the existing `RecastGroup.Capstone` cooldown bucket. Do not add duplicate recast groups such as `CapstoneSignature` or `NPCSignature` unless a separate gameplay cooldown is explicitly designed and approved.
- Reusable capstone assets must not be branded with the quest line, capstone perk, boss, NPC, area name, source category, combat profile, or skill family. This includes signature/support abilities, feat labels, TLK text, icons, generated stat skins, generated weapons, and other generated reusable item display names. Use behavior, effect, or mechanical purpose only, such as `SustainBurnAbilityDefinition`, not `BeastSustainBurnAbilityDefinition`. Capstone/perk names are still required in quest text, achievements, progression keys/proofs when they identify the line, and the final boss's actual unlock package.
- Objectives: kill group, collect item resref, quantity, producer requirement, trigger/placeable objective if any, and duplicate item justification when a proposed collect item already appears in a non-guild quest.
- Text: not eligible, offer, acceptance, in-progress reminder, turn-in, completion, completed/repeat text, player replies, journal text for each state.
- Dialogue flow: opening beat, optional player questions, NPC answers, accept reply, decline reply, reminder path, ready-to-turn-in path, and completed path. For major, chain, capstone, faction, or signature quests, include optional branches for motive, stakes, local lore, directions, target context, proof, or tactical advice.
- Creative brief: NPC role, NPC voice, local pressure, stakes, lore anchors, why this NPC asks the player, and how the reward is justified in-world.
- Rewards: XP, credits, items, key items, GP, faction standing/points, completion achievement, selectable vs automatic.
- Temporary proof key items: lore-appropriate artifact names tied to the area, faction, enemy, or trial context; no generic generator labels such as `Field Report`, `Calibration Core`, `Broken Seal`, `Command Mark`, or vague `Mark` names.
- Property rewards: only include player-placeable property structures or furniture when the user explicitly requests them. Quest and capstone setup must not add or modify `StructureType`, fabrication furniture recipes, `structure_####` or `bpstructure####` UTIs, or structure/furniture loot tables as incidental quest rewards. Ordinary furniture/placeable structures must be added to the end of the non-building `StructureType` section before `// Buildings start here (5000+)`, not after city buildings or in the 5000+ building/layout range.
- Placement: fixed NPC placement, enemy spawn table, enemy spawn coordinates or area random count, and whether unique quest activator placeables are world-instance-only or reusable palette blueprints.

If a field is missing and cannot be resolved by searching the repo, ask one question at a time and include a recommended answer.

Before accepting a collect objective, search `SWLOR.Game.Server/Feature/QuestDefinition` for existing `AddCollectItemObjective(...)` usage of the same item resref. Avoid duplicating items across non-guild quests unless the repetition is intentional and justified by gameplay or story. Good reasons include quest-chain continuity, tutorial reinforcement, a deliberate repeatable resource sink, faction or local supply pressure, or a different acquisition route. Weak reasons like "the item already exists" or "it is nearby" should trigger a different item or objective.

## Code Surfaces

Quest definition:

- Add to `SWLOR.Game.Server/Feature/QuestDefinition/<AreaOrPlanet>QuestDefinition.cs`.
- Create a private method per quest.
- Call the method from `BuildQuests()`.
- Use `.Create("quest_id", "Quest Name")`, then flags, prerequisites, states, objectives, rewards, hooks.
- For generated capstone quest batches, keep each skill-owned quest definition as a concrete `IQuestListDefinition` whose private methods call `QuestBuilder` directly. Do not route quest setup through abstract quest bases, shared `BuildQuest(line, step)` helpers, catalog-driven quest models, partial classes, one-line generated records, or central capstone metadata/catalog classes. Keep line-specific quest IDs, NPC groups, proof key items, quest givers, enemy resrefs, encounter waypoint tags, and related asset constants in the owning skill quest definition file. Keep those constants private by default, and expose them as `internal` only when another production server type needs the value, such as spawn tables or perk gates. Do not make quest constants public for tests, area-builder handoff, or convenience; tests should validate built quest, perk, module, and workbook artifacts instead of depending on quest-definition constant visibility.
- For beast capstone quests, use `.PrerequisiteSkill(SkillType.BeastMastery, 50)` for every step and do not add active-beast quest prerequisites.
- For major, chain, capstone, faction, or signature quest lines, add an active `AchievementType` entry and grant it from the final quest's `.OnCompleteAction(...)` with `Achievement.GiveAchievement(...)` unless the user explicitly opts out.

NPC group:

- Add a `[NPCGroup("Display Name")]` entry to `SWLOR.Game.Server/Service/NPCService/NPCGroupType.cs` only when no group already matches.
- Use the next integer value.
- For generated capstone groups, prefix the enum identifier with the planet from the line's `AreaGroup.PlanetType`, not `Capstone_`.
- Set enemy `VarTable` `QUEST_NPC_GROUP_ID` to that integer in `Module/utc/<enemy>.utc.json` or the relevant placed creature.
- For capstone or high-level signature enemies, update `Module/utc`, `Module/uti` stat skins/weapons, reusable NPC ability definition files under `SWLOR.Game.Server/Feature/AbilityDefinition/NPC`, feat/spell rows, TLK, gameplay icons, cooldown icon variants, and `design/bible/SWLOR Design Bible - Combat Upgrade.xlsx` together so reusable signature abilities, support packages, role/difficulty/type/modifier inputs, resistance adjustments, and runtime item properties stay aligned.
- Keep capstone-tier NPC signature abilities on `RecastGroup.Capstone`; do not create or regenerate `CapstoneSignature`/`NPCSignature` recast groups for them.

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
- If a capstone quest giver's target area does not exist yet, create its UTC, dialogue, and creature palette entry, then leave actual `Module/git` placement to the area builder.

Spawn placement:

- Spawn table definitions live in `SWLOR.Game.Server/Feature/SpawnDefinition/*SpawnDefinition.cs`.
- Define spawn tables in the owning planet or location spawn definition like the existing planet spawn files: one named private method per table, a direct `_builder.Create("TABLE_ID", "Display Name")`, and explicit fluent `.AddSpawn(...).WithFrequency(...).RandomlyWalks().ReturnsHome()` rows. Do not create quest-category spawn buckets or use generated helper methods, catalogs, records, or table-ID constants merely to compress spawn rows.
- Keep spawn and loot table ID constants private to the owning definition by default. Expose them as `internal` only when another production server type truly needs direct access; tests, area-builder handoff notes, and documentation should use built artifacts or literal expected IDs instead of forcing wider visibility.
- Do not use quest or capstone loot tables to introduce structure/furniture rewards unless the user explicitly requested player-placeable property rewards. If such rewards are requested, handle the `StructureType`, `structure_####` item, `bpstructure####` blueprint, fabrication recipe, and loot-table changes as a separate property/fabrication change. Keep ordinary furniture/placeable structure IDs below the 5000+ building/layout range.
- Area-random spawns use area locals `CREATURE_SPAWN_TABLE_ID` and `CREATURE_SPAWN_COUNT`.
- Fixed spawn points use `WaypointList` entries whose `Tag` equals the spawn table ID.
- Spawn tables are cached by reflection and duplicate IDs are logged as errors.
- If the target area does not exist yet, stop short of `Module/git` placement. Create quest definitions, key items, achievements, NPC groups, perk gates, and documentation, then record explicit area-builder follow-ups instead of placing temporary high-level content in unrelated areas.
- Capstone content packages that mimic Blood Frenzy require two attached physical areas: one gated dungeon/lesson area for ambient level 50 enemies and one attached boss arena area for `quest_enc` warden/master encounters. Do not describe a capstone area group as if it were a single physical area.
- If the target area does not exist yet, still create reusable `Module/utw` waypoint blueprints and add them to `Module/itp/waypointpalcus.itp.json` for spawn tables and boss spawn positions. Do not place those waypoints into an unrelated existing area.

On-demand quest encounter activators:

- Use placed world instances with `OnUsed = quest_enc` and the required `QUEST_ID`, `QUEST_STATE`, and `QUEST_ENCOUNTER_*` locals for unique boss markers.
- Do not add one-off quest encounter marker placeables to `Module/itp/placeablepalcus.itp.json` or create matching `Module/utp` files unless the user or area builder explicitly requests a reusable palette blueprint.
- Keep tests focused on the placed area instances and their locals when the marker is world-instance-only.
- If an on-demand boss area does not exist yet, document the required `quest_enc` activator locals and boss spawn waypoint names, but do not create placeholder world instances in another area.
- For capstone quest lines, keep warden/master enemies out of ambient spawn tables. Spawn them only from `quest_enc` activators in the attached boss arena, with generated boss spawn waypoint blueprints available for area builders.

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
- A unique on-demand quest encounter marker is reintroduced into the placeable blueprint palette when the intended source of truth is the placed world instance.
- High-level quest enemies or boss activators are placed in unrelated low-level/public areas because the intended target area has not been built yet.
