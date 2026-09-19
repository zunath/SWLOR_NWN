# Authoring SWLOR conversations

`SWLOR.Game.Server/ConversationData/*.conversation.json` is the sole source of authored gameplay dialogue. The server embeds these graphs, and the SWLOR Toolset creates and edits them directly. They are not generated build output. Do not recreate matching files in `Module/dlg` or regenerate existing graphs from a legacy DLG.

The only native module conversation is `Module/dlg/dmfi_universal.dlg.json`. DMFI wands start and restart it with native `ActionStartConversation` calls, so it remains packed with the module. Other SWLOR conversations require no physical DLG resource.

## Editing and wiring

- Edit a graph through the SWLOR Toolset's conversation editor or its JSON source. Preserve stable node/choice IDs and ordered routes. Validate changes with `ConversationGraphValidator`.
- Quest actions belong on explicit player choices in `Actions`, with a `Key` and string-array `Arguments`. Optional lore branches must not advance quests.
- Route `Conditions` use a `Key`, string-array `Arguments`, and `IsNegated`; do not prefix graph keys with `!`. For example, an offer can use `condition-can-accept-quest` with `Arguments: ["quest_id"]`.
- Set an NPC's `Conversation` field to the graph ID and its `ScriptDialogue` to `dialog_start`, on both blueprints and placed instances. Placeables and doors use the corresponding `OnUsed` or `OnFailToOpen` event. These resrefs identify SWLOR graphs, not DLG files.
- Keep key handoffs in-world. The key service already notifies the recipient; NPCs should not mention the Key Items window.

Conversation-only edits require an updated server build. Changes to NPC/event wiring or other Module resources also require a module repack. Deploy this cleanup with a fresh module pack so removed legacy resources do not remain in the deployed archive.

## Importing an external legacy conversation

The legacy editor and importer remain available for external modules and the DMFI exception. Import one file into a new graph, preserving its conversation ID:

```powershell
dotnet run --project tools/SWLOR.ConversationMigrator -p:RunPostBuildEvent=Never -- path/to/new_npc.dlg.json SWLOR.Game.Server/ConversationData/new_npc.conversation.json
```

The importer refuses an existing output, unsupported legacy behavior, and the native DMFI conversation. It does not scan the repository, remove other graphs, regenerate migration reports, or rewrite module routing. Review the imported graph, wire its module interaction, and author all subsequent changes in the graph. The old bulk `--overwrite` command is intentionally unsupported.

## Verification

Build test projects with `-p:RunPostBuildEvent=Never`, then run relevant tests with `--no-build`. The server's conversation and quest tests validate graph operations, quest progression, NPC routing, and the absence of duplicate native sources. Toolset checks cover graph discovery, search, reference validation, authoring, and import safety.

`SWLOR.Toolset.Tests/Fixtures/LegacyConversations` contains frozen legacy input samples used only by parser, importer, and legacy-editor tests. Never synchronize those samples with live dialogue or use them as gameplay sources. Live-content tests must read the authored SWLOR graphs.
