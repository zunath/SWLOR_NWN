# Quest content validation

The quest audit loads every concrete `IQuestListDefinition` and the embedded authored conversation graphs. It currently covers **1,051 quests**: **269 story/access quests** and **782 guild tasks**. Native DLG importer fixtures are historical test inputs, not gameplay content.

## Automated coverage

- Unique quest IDs, contiguous journal states, nonempty journal text, valid prerequisite references, and acyclic prerequisite chains.
- All 1,495 conversation quest actions and conditions reference registered quests and valid states.
- Placed conversation offers and resumable routes for 559 story states, including collection hand-ins and placed scripted interactions. The route audit evaluates the target quest's saved state exactly; unrelated quest, key, and skill conditions remain symbolic.
- All 836 collection objectives have item blueprints and acquisition sources. Player-produced objectives also require recipe outputs.
- All 281 kill objectives have matching enemy groups in placed creatures, active area/waypoint spawn tables, or quest encounter activators.
- Guild tasks use objective and state structures supported by the guild board.
- All 39 capstone conversations accept and finish their five quests, acknowledge access keys in-world, and name the giver in the journal.
- Every kill-then-collect quest grants its proof separately to one or three credited players, without granting extra proof on acceptance, hand-in, or completion.
- Conversation sessions exercise interrupted Sith training and Sergeant Nahulu's Republic oath on his assigned graph.
- Shared lifecycle tests exercise the production quest implementation with in-memory engine/database boundaries: journal acceptance and advancement, repeat acceptance, stale counter cleanup, login repair, and prevention of duplicate completion rewards.

## Repairs included

Missing acceptance and turn-in actions were restored across the capstone conversations. Frog Guts, Arkanian Dragon Trophy, and stolen cargo now reach every credited quester. Sith training can resume after a conversation interruption, and the Republic oath is available through Nahulu's placed conversation.

Pending server migration 22 moves legacy active frog quests already at state 2 without a proof counter directly to the reward turn-in state. It preserves their kill credit without awarding items or rewards, leaves current/completed records alone, and is safe to retry. The legacy importer also rejects the retired First Rites `next_state_1` script rather than creating a broken quest action.

Agriculture tasks now request the craftable `peeled_crayfish` and `sliced_cod` outputs. Active quest counters reconcile against their current objectives on login and before a collection hand-in, preserving progress for unchanged objectives. Completed records remain available for prerequisites but are not restored as active journal entries or eligible for another payout.

First Rites, Collect Dantooine Starwort Herbs, and Neutralize the Rooftop Sniper were deliberately retired. Their offers and unused quest assets were removed, including the First Rites entrance, NPC, and crystal interaction hooks. Nahulu's unused duplicate graph was merged into his assigned conversation. An unrelated placeholder object-visibility hook was removed from the Viscara Sith Basement quest.

## Running the checks

Build once with deployment disabled, then run the relevant tests without rebuilding:

```powershell
dotnet build SWLOR.Game.Server.Tests/SWLOR.Game.Server.Tests.csproj -p:RunPostBuildEvent=Never
dotnet test SWLOR.Game.Server.Tests/SWLOR.Game.Server.Tests.csproj --no-build --filter "FullyQualifiedName~Quest|FullyQualifiedName~Conversation|FullyQualifiedName~DialogPrivacy|FullyQualifiedName~LootTableDefinitionTests|FullyQualifiedName~EconomyObtainabilityCoverageTests|FullyQualifiedName~PlayerMessageAuditTests"

dotnet build SWLOR.Toolset.Tests/SWLOR.Toolset.Tests.csproj -p:RunPostBuildEvent=Never
dotnet test SWLOR.Toolset.Tests/SWLOR.Toolset.Tests.csproj --no-build --filter "FullyQualifiedName~Conversation|FullyQualifiedName~QuestSourceScanner|FullyQualifiedName~DialogReference|FullyQualifiedName~ModuleContentCategory|FullyQualifiedName~SnippetCatalog"
```

These checks validate content connections and scripted behavior; they do not simulate travel, combat, native item creation, or a complete in-game playthrough. Deployment requires updated server binaries and a fresh module repack, including removal of the retired native DLG resources. No HAK changes are required.
