using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SnippetService;

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
            ConditionCanAcceptQuest();

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
                .Phrase("the player has finished {questId}")
                .NegatedPhrase("the player has not finished {questId}")
                .Argument("questId", SnippetArgumentType.QuestId)
                .Repeats()
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
                .Phrase("the player is doing {questId}")
                .NegatedPhrase("the player is not doing {questId}")
                .Argument("questId", SnippetArgumentType.QuestId)
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
                .Phrase("the player is on step {state} of {questId}")
                .NegatedPhrase("the player is not on step {state} of {questId}")
                .Argument("questId", SnippetArgumentType.QuestId)
                .Argument("state", SnippetArgumentType.QuestState)
                .Repeats()
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

        private void ConditionCanAcceptQuest()
        {
            _builder.Create("condition-can-accept-quest")
                .Description("Checks whether a player can accept a quest without sending prerequisite feedback.")
                .Phrase("the player is allowed to start {questId}")
                .NegatedPhrase("the player is not yet allowed to start {questId}")
                .Argument("questId", SnippetArgumentType.QuestId)
                .AppearsWhenAction((player, args) =>
                {
                    if (args.Length <= 0)
                    {
                        const string Error = "'condition-can-accept-quest' requires a questId argument.";
                        SendMessageToPC(player, Error);
                        Log.Write(LogGroup.Error, Error);
                        return false;
                    }

                    var questId = args[0];
                    return Quest.CanAcceptQuest(player, questId);
                });
        }

        private void ActionAcceptQuest()
        {
            _builder.Create("action-accept-quest")
                .Description("Accepts a quest for a player.")
                .Phrase("starts {questId}")
                .Argument("questId", SnippetArgumentType.QuestId)
                .ActionsTakenAction((player, args) =>
                {
                    if (args.Length <= 0)
                    {
                        const string Error = "'action-accept-quest' requires a questId argument.";
                        SendMessageToPC(player, Error);
                        Log.Write(LogGroup.Error, Error);
                        return false;
                    }

                    var questId = args[0];
                    return Quest.AcceptQuest(player, Snippet.GetExecutionOwner(), questId);
                });
        }

        private void ActionAdvanceQuest()
        {
            _builder.Create("action-advance-quest")
                .Description("Advances a quest for a player.")
                .Phrase("moves {questId} to its next step, and pays out on the last one")
                .Argument("questId", SnippetArgumentType.QuestId)
                .ActionsTakenAction((player, args) =>
                {
                    if (args.Length <= 0)
                    {
                        const string Error = "'action-advance-quest' requires a questId argument.";
                        SendMessageToPC(player, Error);
                        Log.Write(LogGroup.Error, Error);
                        return false;
                    }

                    var questId = args[0];
                    return Quest.AdvanceQuest(player, Snippet.GetExecutionOwner(), questId);
                });
        }

        private void ActionRequestItemsFromPlayer()
        {
            _builder.Create("action-request-quest-items")
                .Description("Spawns a container and forces the player to open it. They are then instructed to insert any quest items inside.")
                .Phrase("opens the hand-in box for {questId}")
                .Argument("questId", SnippetArgumentType.QuestId)
                .ActionsTakenAction((player, args) =>
                {
                    if (!GetIsPC(player) || GetIsDM(player)) return false;

                    if (args.Length <= 0)
                    {
                        const string Error = "'action-request-quest-items' requires a questId argument.";
                        SendMessageToPC(player, Error);
                        Log.Write(LogGroup.Error, Error);
                        return false;
                    }

                    var questId = args[0];
                    return Quest.RequestItemsFromPlayer(player, Snippet.GetExecutionOwner(), questId);
                });
        }

    }
}
