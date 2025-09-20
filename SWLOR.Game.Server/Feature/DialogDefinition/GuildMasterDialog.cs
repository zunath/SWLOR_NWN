using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.NWN.API.NWScript;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class GuildMasterDialog: DialogBase
    {
        private class Model
        {
            public GuildType Guild { get; set; }
            public string QuestId { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";
        private const string TellMePageId = "TELL_ME_PAGE";
        private const string RankTooLowPageId = "RANK_TOO_LOW_PAGE";
        private const string TaskListPageId = "TASK_LIST_PAGE";
        private const string TaskDetailsPageId = "TASK_DETAILS_PAGE";
        private const string GuildStorePageId = "GUILD_STORE_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .WithDataModel(new Model())
                .AddInitializationAction(Initialization)
                .AddPage(MainPageId, MainPageInit)
                .AddPage(TellMePageId, TellMePageInit)
                .AddPage(RankTooLowPageId, RankTooLowPageInit)
                .AddPage(TaskListPageId, TaskListPageInit)
                .AddPage(TaskDetailsPageId, TaskDetailsPageInit)
                .AddPage(GuildStorePageId, GuildStorePageInit);

            return builder.Build();
        }

        private void Initialization()
        {
            var player = GetPC();
            var model = GetDataModel<Model>();

            // Don't let non-players use this convo.
            if (GetIsDM(player) || !GetIsPC(player))
            {
                EndConversation();
                return;
            }

            var speaker = GetDialogTarget();
            model.Guild = (GuildType)GetLocalInt(speaker, "GUILD_ID");
        }

        private void MainPageInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var playerName = GetName(player);
            var guild = Guild.GetGuild(model.Guild);
            var pcGuild = dbPlayer.Guilds.ContainsKey(model.Guild)
                ? dbPlayer.Guilds[model.Guild]
                : new PlayerGuild();
            var requiredPoints = Guild.GetGPRequiredForRank(pcGuild.Rank);

            page.Header = ColorToken.Green("Guild: ") + guild.Name + "\n" +
                          ColorToken.Green("Rank: ") + pcGuild.Rank + " (" + pcGuild.Points + " / " + requiredPoints + " GP)\n" + 
                          ColorToken.Green("Description: ") + guild.Name + "\n\n" + 
                          "Welcome to my guild, " + playerName + ". What can I help you with?";

            page.AddResponse("Tell me about guilds.", () =>
            {
                ChangePage(TellMePageId);
            });

            page.AddResponse("Show me the task list.", () =>
            {
                ChangePage(TaskListPageId);
            });

            page.AddResponse("Show me the guild shop.", () =>
            {
                if (pcGuild.Rank <= 0)
                {
                    ChangePage(RankTooLowPageId);
                }
                else
                {
                    ChangePage(GuildStorePageId);
                }
            });
        }

        private void TellMePageInit(DialogPage page)
        {
            page.Header = "Guilds are organizations focused on the advancement of a particular task. Every guild is freely open for you to contribute as you see fit. Those who contribute the most will receive the biggest benefits.\n\n" +
                "One of the ways we reward contributors is by way of Guild Points or GP. When you complete a task - such as hunting a beast or creating needed supplies - you'll receive not only payment but also GP.\n\n" +
                "When you acquire enough GP, the guild will increase your rank. Higher ranks unlock benefits like access to new items in the guild store.\n\n" +
                "There's no fee to join and you may come and go as you please.";
        }

        private void RankTooLowPageInit(DialogPage page)
        {
            page.Header = "I'm sorry but your rank is too low to grant you access to that. Perform tasks for us and come back when you've increased your rank with our guild.";
        }

        private void TaskListPageInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            page.Header = "The following tasks are available for you.";

            var currentTasks = Guild.GetAllActiveGuildTasks(model.Guild);
            // Expired quests - Quests the player accepted but are no longer offered by the guild master.
            foreach (var (questId, pcQuest) in dbPlayer.Quests)
            {
                var task = Quest.GetQuestById(questId);
                if (task.GuildType != model.Guild) continue; // This quest isn't associated with this guild type.
                if (pcQuest.DateLastCompleted != null) continue; // Has already been completed.
                if (currentTasks.ContainsKey(questId)) continue; // This task is currently offered

                var status = ColorToken.Green("{ACCEPTED}");
                var text = $"{task.Name} [Rank {task.GuildRank + 1}] {status}";
                page.AddResponse(text, () =>
                {
                    model.QuestId = questId;
                    ChangePage(TaskDetailsPageId);
                });
            }

            foreach (var (_, task)in currentTasks)
            {
                // If the player has completed the task during this task cycle, it will be excluded from this list.
                // The reason for this is to prevent players from repeating the same tasks over and over without impunity.
                if (dbPlayer.Quests.ContainsKey(task.QuestId) &&
                    dbPlayer.Quests[task.QuestId].DateLastCompleted >= Guild.DateTasksLoaded)
                    continue;

                // Player doesn't have the requisite guild rank to accept this task. Skip over it.
                var playerRank = 0;
                if (dbPlayer.Guilds.ContainsKey(task.GuildType))
                    playerRank = dbPlayer.Guilds[task.GuildType].Rank;

                if (playerRank < task.GuildRank)
                    continue;

                var status = ColorToken.Green("{ACCEPTED}");
                // Player has never accepted the quest, or they've already completed it at least once and can accept it again.
                if (!dbPlayer.Quests.ContainsKey(task.QuestId) ||
                    (dbPlayer.Quests[task.QuestId].DateLastCompleted != null && dbPlayer.Quests[task.QuestId].TimesCompleted > 0))
                {
                    status = ColorToken.Yellow("{Available}");
                }

                var text = $"{task.Name} [Rank {task.GuildRank + 1}] {status}";
                page.AddResponse(text, () =>
                {
                    model.QuestId = task.QuestId;
                    ChangePage(TaskDetailsPageId);
                });
            }
        }

        private void TaskDetailsPageInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var pcQuest = dbPlayer.Quests.ContainsKey(model.QuestId)
                ? dbPlayer.Quests[model.QuestId]
                : null;

            var task = Quest.GetQuestById(model.QuestId);

            var gpRewards = task.Rewards.Where(x => x.GetType() == typeof(GPReward)).Cast<GPReward>();
            var goldRewards = task.Rewards.Where(x => x.GetType() == typeof(GoldReward)).Cast<GoldReward>();
            var gpAmount = 0;
            var goldAmount = 0;

            foreach (var gpReward in gpRewards)
            {
                gpAmount += Guild.CalculateGPReward(player, model.Guild, gpReward.Amount);
            }

            foreach (var goldReward in goldRewards)
            {
                goldAmount += Quest.CalculateQuestGoldReward(player, true, goldReward.Amount);
            }

            page.Header = ColorToken.Green("Task: ") + task.Name + "\n\n" +
                          "Rewards:\n\n" +
                          ColorToken.Green("Credits: ") + goldAmount + "\n" +
                          ColorToken.Green("Guild Points: ") + gpAmount;

            // Never accepted, or has already been completed once.
            if (pcQuest == null || pcQuest.DateLastCompleted != null)
            {
                page.AddResponse("Accept Task", () =>
                {
                    Quest.AcceptQuest(player, model.QuestId);
                });
            }

            // Accepted, but not completed.
            if (pcQuest != null && pcQuest.DateLastCompleted == null)
            {
                page.AddResponse("Give Report", GiveReport);
            }
        }

        private void GiveReport()
        {
            var model = GetDataModel<Model>();
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (!dbPlayer.Quests.ContainsKey(model.QuestId)) return;

            var pcStatus = dbPlayer.Quests[model.QuestId];
            var quest = Quest.GetQuestById(model.QuestId);
            var state = quest.States[pcStatus.CurrentState];
            var hasItemObjective =
                state.GetObjectives().FirstOrDefault(x => x.GetType() == typeof(CollectItemObjective)) != null;

            // Quest has at least one "collect item" objective.
            if (hasItemObjective)
            {
                Quest.RequestItemsFromPlayer(player, model.QuestId);
            }
            // All other quest types
            else if (quest.CanComplete(player))
            {
                quest.Complete(player, GetDialogTarget(), null);
                EndConversation();
            }
            else
            {
                SendMessageToPC(player, ColorToken.Red("One or more task is incomplete. Refer to your journal for more information."));
            }
        }

        private void GuildStorePageInit(DialogPage page)
        {
            var player = GetPC();

            void OpenStore(int rank)
            {
                var speaker = GetDialogTarget();
                var storeTag = GetLocalString(speaker, $"STORE_TAG_RANK_{rank}");

                // Invalid local variable set.
                if (string.IsNullOrWhiteSpace(storeTag))
                {
                    SendMessageToPC(player, "ERROR: Store could not be found. Inform a developer that a local variable is missing.");
                    return;
                }

                // Store object is invalid or hasn't been placed.
                var store = GetObjectByTag(storeTag);
                if (!GetIsObjectValid(store))
                {
                    SendMessageToPC(player, "ERROR: Store object could not be found. Inform a developer that either the specified tag is wrong or the store has not been placed in the module.");
                    return;
                }

                NWScript.OpenStore(store, player);
            }

            var model = GetDataModel<Model>();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var pcGuild = dbPlayer.Guilds.ContainsKey(model.Guild)
                ? dbPlayer.Guilds[model.Guild]
                : new PlayerGuild();

            page.Header = "Which store would you like to view?";

            for (var rank = 1; rank <= Guild.MaxRank; rank++)
            {
                if (pcGuild.Rank >= rank)
                {
                    var level = rank;
                    page.AddResponse($"Rank {rank}", () =>
                    {
                        OpenStore(level);
                    });
                }
            }
        }
    }
}
