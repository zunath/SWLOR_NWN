using System.Linq;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;
using SWLOR.Game.Server.Legacy.ValueObject.Dialog;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Legacy.Conversation
{
    public class GuildMaster: ConversationBase
    {
        private class Model
        {
            public GuildType Guild { get; set; }
            public int TaskID { get; set; }
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            var dialog = new PlayerDialog("MainPage");
            var mainPage = new DialogPage("<SET LATER>",
                "Tell me about guilds.",
                "Show me the task list.",
                "Show me the guild shop.");

            var tellMePage = new DialogPage();
            var rankTooLowPage = new DialogPage("I'm sorry but your rank is too low to grant you access to that. Perform tasks for us and come back when you've increased your rank with our guild.");
            var taskListPage = new DialogPage("The following tasks are available for you.");
            var taskDetailsPage = new DialogPage("<SET LATER>",
                "Accept Task",
                "Give Report");
            var guildStorePage = new DialogPage("Which store would you like to view?",
                "Rank 1",
                "Rank 2",
                "Rank 3",
                "Rank 4",
                "Rank 5");

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("TellMePage", tellMePage);
            dialog.AddPage("RankTooLowPage", rankTooLowPage);
            dialog.AddPage("TaskListPage", taskListPage);
            dialog.AddPage("TaskDetailsPage", taskDetailsPage);
            dialog.AddPage("GuildStorePage", guildStorePage);
            return dialog;
        }

        public override void Initialize()
        {
            var player = GetPC();

            // Don't let non-players use this convo.
            if (!player.IsPlayer)
            {
                EndConversation();
                return;
            }

            var speaker = GetDialogTarget();
            var guild = (GuildType)speaker.GetLocalInt("GUILD_ID");

            var model = new Model
            {
                Guild = guild
            };
            SetDialogCustomData(model);

            LoadMainPage();
        }


        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    MainPageResponse(responseID);
                    break;
                case "TaskListPage":
                    TaskListPageResponses(responseID);
                    break;
                case "TaskDetailsPage":
                    TaskDetailsPageResponses(responseID);
                    break;
                case "GuildStorePage":
                    GuildStoreResponses(responseID);
                    break;
            }
        }

        private void LoadMainPage()
        {
            var player = GetPC();
            var model = GetDialogCustomData<Model>();
            var guild = DataService.Guild.GetByID((int) model.Guild);
            var pcGP = DataService.PCGuildPoint.GetByPlayerIDAndGuildID(player.GlobalID, guild.ID);
            var requiredPoints = Guild.GetGPRequiredForRank(pcGP.Rank);

            var header = ColorToken.Green("Guild: ") + guild.Name + "\n";
            header += ColorToken.Green("Rank: ") + pcGP.Rank + " (" + pcGP.Points + " / " + requiredPoints + " GP)\n"; 
            header += ColorToken.Green("Description: ") + guild.Description + "\n\n";
            header += "Welcome to my guild, " + player.Name + ". What can I help you with?";

            SetPageHeader("MainPage", header);
        }

        private void MainPageResponse(int responseID)
        {
            switch (responseID)
            {
                case 1: // Tell me about guilds.
                    LoadTellMePage();
                    ChangePage("TellMePage");
                    break;
                case 2: // Show me the task list.
                    LoadTaskListPage();
                    ChangePage("TaskListPage");
                    break;
                case 3: // Show me the guild shop.
                    LoadGuildStorePage();
                    break;
            }
        }

        private void LoadTellMePage()
        {
            var guilds = DataService.Guild.GetAll();

            var header = "Guilds are organizations focused on the advancement of a particular task. Every guild is freely open for you to contribute as you see fit. Those who contribute the most will receive the biggest benefits.\n\n";
            header += "One of the ways we reward contributors is by way of Guild Points or GP. When you complete a task - such as hunting a beast or creating needed supplies - you'll receive not only payment but also GP.\n\n";
            header += "When you acquire enough GP, the guild will increase your rank. Higher ranks unlock benefits like access to new items in the guild store.\n\n";
            header += "There's no fee to join and you may come and go as you please. Here's some information on the guilds currently operating in this sector.\n\n";

            foreach (var guild in guilds)
            {
                header += ColorToken.Green(guild.Name) + ": " + guild.Description + "\n\n";
            }

            SetPageHeader("TellMePage", header);
        }

        private void LoadGuildStorePage()
        {
            var player = GetPC();
            var model = GetDialogCustomData<Model>();
            var pcGP = DataService.PCGuildPoint.GetByPlayerIDAndGuildID(player.GlobalID, (int) model.Guild);

            // If player's rank is too low, send them to the page explaining that.
            if (pcGP.Rank <= 0)
            {
                ChangePage("RankTooLowPage");
            }
            // Otherwise, show/hide options depending on player's rank at this guild.
            else
            {
                SetResponseVisible("GuildStorePage", 1, pcGP.Rank >= 1);
                SetResponseVisible("GuildStorePage", 2, pcGP.Rank >= 2);
                SetResponseVisible("GuildStorePage", 3, pcGP.Rank >= 3);
                SetResponseVisible("GuildStorePage", 4, pcGP.Rank >= 4);
                SetResponseVisible("GuildStorePage", 5, pcGP.Rank >= 5);
                ChangePage("GuildStorePage");
            }
        }

        private void GuildStoreResponses(int responseID)
        {
            var player = GetPC();
            var model = GetDialogCustomData<Model>();
            var pcGP = DataService.PCGuildPoint.GetByPlayerIDAndGuildID(player.GlobalID, (int)model.Guild);

            // Check the player's rank and ensure they can access this store.
            if (pcGP.Rank < responseID)
            {
                ChangePage("RankTooLowPage");
            }
            // Otherwise open up the shop the player has access to.
            else
            {
                var speaker = GetDialogTarget();
                var storeTag = speaker.GetLocalString("STORE_TAG_RANK_" + responseID);

                // Invalid local variable set.
                if (string.IsNullOrWhiteSpace(storeTag))
                {
                    speaker.SpeakString("ERROR: Store could not be found. Inform a developer that a local variable is missing.");
                    return;
                }

                // Store object is invalid or hasn't been placed.
                NWObject store = NWScript.GetObjectByTag(storeTag);
                if (!store.IsValid)
                {
                    speaker.SpeakString("ERROR: Store object could not be found. Inform a developer that either the specified tag is wrong or the store has not been placed in the module.");
                    return;
                }

                // Valid store. Open it.
                NWScript.OpenStore(store, player);
            }

        }

        private void LoadTaskListPage()
        {
            var player = GetPC();
            var model = GetDialogCustomData<Model>();
            var header = "These are our currently available tasks. Please check back periodically because our needs are always changing.";
            SetPageHeader("TaskListPage", header);

            ClearPageResponses("TaskListPage");

            var lastUpdate = DataService.ServerConfiguration.Get().LastGuildTaskUpdate;
            var pcGP = DataService.PCGuildPoint.GetByPlayerIDAndGuildID(player.GlobalID, (int) model.Guild);

            // It's possible for players to have tasks which are no longer offered. 
            // In this case, we still display them on the menu. Once they complete them, they'll disappear from the list.
            var questIDs = DataService.PCQuestStatus
                .GetAllByPlayerID(player.GlobalID)
                .Where(x => x.CompletionDate == null)
                .Select(s => s.QuestID);
            var expiredTasks = DataService.GuildTask
                .GetAll()
                .Where(x => !x.IsCurrentlyOffered &&
                            questIDs.Contains(x.QuestID) &&
                            x.GuildID == (int)model.Guild)
                .OrderByDescending(o => o.RequiredRank);
            foreach (var task in expiredTasks)
            {
                var quest = Quest.GetQuestById(task.QuestID.ToString()); // todo need to update this to new system
                var status = ColorToken.Green("{ACCEPTED}");
                AddResponseToPage("TaskListPage", quest.Name + " [Rank " + (task.RequiredRank+1) + "] " + status + ColorToken.Red(" [EXPIRED]"), true, task.ID);
            }

            // Pull back all currently available tasks. This list rotates after 24 hours and a reboot occurs.
            var tasks = DataService.GuildTask
                .GetAllByCurrentlyOffered()
                .Where(x => x.GuildID == (int) model.Guild &&
                            x.RequiredRank <= pcGP.Rank)
                .OrderByDescending(o => o.RequiredRank);
            foreach (var task in tasks)
            {
                var quest = Quest.GetQuestById(task.QuestID.ToString()); // todo need to update this to new system
                var questStatus = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(player.GlobalID, task.QuestID);

                // If the player has completed the task during this task cycle, it will be excluded from this list.
                // The reason for this is to prevent players from repeating the same tasks over and over without impunity.
                if (questStatus != null && questStatus.CompletionDate >= lastUpdate) continue;

                var status = ColorToken.Green("{ACCEPTED}");
                // Player has never accepted the quest, or they've already completed it at least once and can accept it again.
                if (questStatus == null || questStatus.CompletionDate != null)
                {
                    status = ColorToken.Yellow("{Available}");
                }

                AddResponseToPage("TaskListPage", quest.Name + " [Rank " + (task.RequiredRank+1) + "] " + status, true, task.ID);
            }

        }

        private void TaskListPageResponses(int responseID)
        {
            var response = GetResponseByID("TaskListPage", responseID);
            var model = GetDialogCustomData<Model>();
            model.TaskID = (int) response.CustomData;

            LoadTaskDetailsPage();
            ChangePage("TaskDetailsPage");
        }

        private void LoadTaskDetailsPage()
        {
            var player = GetPC();
            var model = GetDialogCustomData<Model>();
            var task = DataService.GuildTask.GetByID(model.TaskID);
            var quest = Quest.GetQuestById(task.QuestID.ToString()); // todo need to update this to new system
            var status = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(player.GlobalID, task.QuestID);
            var showQuestAccept = status == null || status.CompletionDate != null; // Never accepted, or has already been completed once.
            var showGiveReport = status != null && status.CompletionDate == null; // Accepted, but not completed.
            var gpRewards = quest.GetRewards().Where(x => x.GetType() == typeof(GPReward)).Cast<GPReward>();
            var goldRewards = quest.GetRewards().Where(x => x.GetType() == typeof(GoldReward)).Cast<GoldReward>();

            var gpAmount = 0;
            var goldAmount = 0;

            foreach (var gpReward in gpRewards)
            {
                gpAmount += Guild.CalculateGPReward(player, gpReward.Guild, gpReward.Amount);
            }

            foreach (var goldReward in goldRewards)
            {
                goldAmount += goldReward.Amount;
            }

            var header = ColorToken.Green("Task: ") + quest.Name + "\n\n";

            header += "Rewards:\n\n";
            header += ColorToken.Green("Credits: ") + goldAmount + "\n";
            header += ColorToken.Green("Guild Points: ") + gpAmount;
            
            SetPageHeader("TaskDetailsPage", header);
            
            SetResponseVisible("TaskDetailsPage", 1, showQuestAccept);
            SetResponseVisible("TaskDetailsPage", 2, showGiveReport);
        }

        private void TaskDetailsPageResponses(int responseID)
        {
            var player = GetPC();
            var model = GetDialogCustomData<Model>();
            var task = DataService.GuildTask.GetByID(model.TaskID);
            var quest = Quest.GetQuestById(task.QuestID.ToString()); // todo need to update this to new system

            switch (responseID)
            {
                case 1: // Accept Task
                    quest.Accept(player, NWScript.OBJECT_SELF);
                    LoadTaskDetailsPage();
                    LoadTaskListPage();
                    break;
                case 2: // Give Report
                    HandleGiveReport(player, task.QuestID);
                    break;
            }
        }

        private void HandleGiveReport(NWPlayer player, int questID)
        {
            var pcStatus = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(player.GlobalID, questID);
            if (pcStatus == null) return;
            var quest = Quest.GetQuestById(questID.ToString()); // todo need to update this to new system
            var state = quest.States[pcStatus.QuestState];
            var hasItemObjective = state.GetObjectives().FirstOrDefault(x => x.GetType() == typeof(CollectItemObjective)) != null;

            // todo need to update this entire section to new system

            // Quest has at least one "collect item" objective.
            //if (hasItemObjective)
            //{
            //    Quest.RequestItemsFromPC(player, GetDialogTarget(), questID);
            //}
            //// All other quest types
            //else if(quest.CanComplete(player))
            //{
            //    quest.Complete(player, NWScript.OBJECT_SELF, null);
            //    EndConversation();
            //}
            //// Missing a requirement.
            //else
            //{
            //    player.SendMessage(ColorToken.Red("One or more task is incomplete. Refer to your journal for more information."));
            //}

        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        public override void EndDialog()
        {
        }
    }
}
