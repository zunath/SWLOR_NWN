using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SnippetService;
using SWLOR.Shared.Core.Log;

namespace SWLOR.Game.Server.Feature.SnippetDefinition
{
    public class QuestSnippetDefinition: ISnippetListDefinition
    {
        private readonly SnippetBuilder _builder = new SnippetBuilder();

        public Dictionary<string, SnippetDetail> BuildSnippets()
        {
            // Conditions
            ConditionHasCompletedQuest();
            ConditionHasQuest();
            ConditionOnQuestState();

            // Actions
            ActionAcceptQuest();
            ActionAdvanceQuest();
            ActionRequestItemsFromPlayer();

            return _builder.Build();
        }

        private void ConditionHasCompletedQuest()
        {
            _builder.Create("condition-completed-quest")
                .Description("Checks whether a player has completed one or more quests.")
                .AppearsWhenAction((player, args) =>
                {
                    if (args.Length <= 0)
                    {
                        const string Error = "'condition-completed-quest' requires at least one questId argument.";
                        SendMessageToPC(player, Error);
                        Log.Write(LogGroup.Error, Error);
                        return false;
                    }

                    foreach (var questId in args)
                    {
                        var playerId = GetObjectUUID(player);
                        var dbPlayer = DB.Get<Player>(playerId);

                        // Doesn't have the quest at all.
                        if (!dbPlayer.Quests.ContainsKey(questId)) return false;

                        // Hasn't completed the quest.
                        if (dbPlayer.Quests[questId].DateLastCompleted == null) return false;
                    }

                    // Otherwise the player meets all necessary prerequisite quest completions.
                    return true;
                });
        }

        private void ConditionHasQuest()
        {
            _builder.Create("condition-has-quest")
                .Description("Checks whether a player has a quest.")
                .AppearsWhenAction((player, args) =>
                {
                    if (args.Length <= 0)
                    {
                        const string Error = "'condition-has-quest' requires a questId argument.";
                        SendMessageToPC(player, Error);
                        Log.Write(LogGroup.Error, Error);
                        return false;
                    }

                    var questId = args[0];
                    var playerId = GetObjectUUID(player);
                    var dbPlayer = DB.Get<Player>(playerId);

                    return dbPlayer.Quests.ContainsKey(questId) && dbPlayer.Quests[questId].DateLastCompleted == null;
                });
        }

        private void ConditionOnQuestState()
        {
            _builder.Create("condition-on-quest-state")
                .Description("Checks if a player is on one or more states of a quest.")
                .AppearsWhenAction((player, args) =>
                {
                    if (args.Length < 2)
                    {
                        const string Error = "'condition-on-quest-state' requires a questId argument and at least one stateNumber argument.";
                        SendMessageToPC(player, Error);
                        Log.Write(LogGroup.Error, Error);
                        return false;
                    }

                    var questId = args[0];
                    var playerId = GetObjectUUID(player);
                    var dbPlayer = DB.Get<Player>(playerId);
                    if (!dbPlayer.Quests.ContainsKey(questId)) 
                        return false;

                    // Try to parse each Id. If it parses, check the player's current state.
                    // If they're on this quest state, return true. Otherwise move to the next argument.
                    for (var index = 1; index < args.Length; index++)
                    {
                        if (int.TryParse(args[index], out var stateId))
                        {
                            if (dbPlayer.Quests[questId].CurrentState == stateId &&
                                dbPlayer.Quests[questId].DateLastCompleted == null)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            var error = $"Could not read stateNumber {index + 1} in the 'condition-on-quest-state' snippet.";
                            SendMessageToPC(player, error);
                            Log.Write(LogGroup.Error, error);

                            return false;
                        }
                    }

                    return false;
                });

        }

        private void ActionAcceptQuest()
        {
            _builder.Create("action-accept-quest")
                .Description("Accepts a quest for a player.")
                .ActionsTakenAction((player, args) =>
                {
                    if (args.Length <= 0)
                    {
                        const string Error = "'action-accept-quest' requires a questId argument.";
                        SendMessageToPC(player, Error);
                        Log.Write(LogGroup.Error, Error);
                        return;
                    }

                    var questId = args[0];
                    Quest.AcceptQuest(player, questId);
                });
        }

        private void ActionAdvanceQuest()
        {
            _builder.Create("action-advance-quest")
                .Description("Advances a quest for a player.")
                .ActionsTakenAction((player, args) =>
                {
                    if (args.Length <= 0)
                    {
                        const string Error = "'action-advance-quest' requires a questId argument.";
                        SendMessageToPC(player, Error);
                        Log.Write(LogGroup.Error, Error);
                        return;
                    }

                    var questId = args[0];
                    Quest.AdvanceQuest(player, OBJECT_SELF, questId);
                });
        }

        private void ActionRequestItemsFromPlayer()
        {
            _builder.Create("action-request-quest-items")
                .Description("Spawns a container and forces the player to open it. They are then instructed to insert any quest items inside.")
                .ActionsTakenAction((player, args) =>
                {
                    if (!GetIsPC(player) || GetIsDM(player)) return;

                    if (args.Length <= 0)
                    {
                        const string Error = "'action-request-quest-items' requires a questId argument.";
                        SendMessageToPC(player, Error);
                        Log.Write(LogGroup.Error, Error);
                        return;
                    }

                    var questId = args[0];
                    Quest.RequestItemsFromPlayer(player, questId);
                });
        }

    }
}
