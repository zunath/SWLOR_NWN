using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject.Dialog;
using QuestType = SWLOR.Game.Server.Enumeration.QuestType;

namespace SWLOR.Game.Server.Conversation
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
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage("<SET LATER>",
                "Tell me about guilds.",
                "Show me the task list.",
                "Show me the guild shop.");

            DialogPage tellMePage = new DialogPage();
            DialogPage rankTooLowPage = new DialogPage("I'm sorry but your rank is too low to grant you access to that. Perform tasks for us and come back when you've increased your rank with our guild.");
            DialogPage taskListPage = new DialogPage("The following tasks are available for you.");
            DialogPage taskDetailsPage = new DialogPage("<SET LATER>",
                "Accept Task",
                "Give Report");
            DialogPage guildStorePage = new DialogPage("Which store would you like to view?",
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

            NWObject speaker = GetDialogTarget();
            GuildType guild = (GuildType)speaker.GetLocalInt("GUILD_ID");

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
            int requiredPoints = GuildService.RankProgression[pcGP.Rank];

            string header = ColorTokenService.Green("Guild: ") + guild.Name + "\n";
            header += ColorTokenService.Green("Rank: ") + pcGP.Rank + " (" + pcGP.Points + " / " + requiredPoints + " GP)\n"; 
            header += ColorTokenService.Green("Description: ") + guild.Description + "\n\n";
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

            string header = "Guilds are organizations focused on the advancement of a particular task. Every guild is freely open for you to contribute as you see fit. Those who contribute the most will receive the biggest benefits.\n\n";
            header += "One of the ways we reward contributors is by way of Guild Points or GP. When you complete a task - such as hunting a beast or creating needed supplies - you'll receive not only payment but also GP.\n\n";
            header += "When you acquire enough GP, the guild will increase your rank. Higher ranks unlock benefits like access to new items in the guild store.\n\n";
            header += "There's no fee to join and you may come and go as you please. Here's some information on the guilds currently operating in this sector.\n\n";

            foreach (var guild in guilds)
            {
                header += ColorTokenService.Green(guild.Name) + ": " + guild.Description + "\n\n";
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
                string storeTag = speaker.GetLocalString("STORE_TAG_RANK_" + responseID);

                // Invalid local variable set.
                if (string.IsNullOrWhiteSpace(storeTag))
                {
                    speaker.SpeakString("ERROR: Store could not be found. Inform a developer that a local variable is missing.");
                    return;
                }

                // Store object is invalid or hasn't been placed.
                NWObject store = _.GetObjectByTag(storeTag);
                if (!store.IsValid)
                {
                    speaker.SpeakString("ERROR: Store object could not be found. Inform a developer that either the specified tag is wrong or the store has not been placed in the module.");
                    return;
                }

                // Valid store. Open it.
                _.OpenStore(store, player);
            }

        }

        private void LoadTaskListPage()
        {
            var player = GetPC();
            var model = GetDialogCustomData<Model>();
            string header = "These are our currently available tasks. Please check back periodically because our needs are always changing.";
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
                var quest = DataService.Quest.GetByID(task.QuestID);
                string status = ColorTokenService.Green("{ACCEPTED}");
                AddResponseToPage("TaskListPage", quest.Name + " [Rank " + (task.RequiredRank+1) + "] " + status + ColorTokenService.Red(" [EXPIRED]"), true, task.ID);
            }

            // Pull back all currently available tasks. This list rotates after 24 hours and a reboot occurs.
            var tasks = DataService.GuildTask
                .GetAllByCurrentlyOffered()
                .Where(x => x.GuildID == (int) model.Guild &&
                            x.RequiredRank <= pcGP.Rank)
                .OrderByDescending(o => o.RequiredRank);
            foreach (var task in tasks)
            {
                var quest = DataService.Quest.GetByID(task.QuestID);
                var questStatus = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(player.GlobalID, task.QuestID);

                // If the player has completed the task during this task cycle, it will be excluded from this list.
                // The reason for this is to prevent players from repeating the same tasks over and over without impunity.
                if (questStatus != null && questStatus.CompletionDate >= lastUpdate) continue;

                string status = ColorTokenService.Green("{ACCEPTED}");
                // Player has never accepted the quest, or they've already completed it at least once and can accept it again.
                if (questStatus == null || questStatus.CompletionDate != null)
                {
                    status = ColorTokenService.Yellow("{Available}");
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
            var quest = DataService.Quest.GetByID(task.QuestID);
            var status = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(player.GlobalID, task.QuestID);
            bool showQuestAccept = status == null || status.CompletionDate != null; // Never accepted, or has already been completed once.
            bool showGiveReport = status != null && status.CompletionDate == null; // Accepted, but not completed.
            
            string header = ColorTokenService.Green("Task: ") + quest.Name + "\n\n";

            header += "Rewards:\n\n";
            header += ColorTokenService.Green("Credits: ") + quest.RewardGold + "\n";
            header += ColorTokenService.Green("Guild Points: ") + quest.RewardGuildPoints;
            
            SetPageHeader("TaskDetailsPage", header);
            
            SetResponseVisible("TaskDetailsPage", 1, showQuestAccept);
            SetResponseVisible("TaskDetailsPage", 2, showGiveReport);
        }

        private void TaskDetailsPageResponses(int responseID)
        {
            var player = GetPC();
            var npc = GetDialogTarget();
            var model = GetDialogCustomData<Model>();
            var task = DataService.GuildTask.GetByID(model.TaskID);
            
            switch (responseID)
            {
                case 1: // Accept Task
                    QuestService.AcceptQuest(player, npc, task.QuestID);
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
            var state = DataService.QuestState.GetByID(pcStatus.CurrentQuestStateID);
            
            // Quest is calling for collecting items. Run that method.
            if (state.QuestTypeID == (int) QuestType.CollectItems)
            {
                QuestService.RequestItemsFromPC(player, GetDialogTarget(), questID);
            }
            // All other quest types
            else if(QuestService.CanPlayerCompleteQuest(player, questID))
            {
                QuestService.CompleteQuest(player, GetDialogTarget(), questID, null);
                EndConversation();
            }
            // Missing a requirement.
            else
            {
                player.SendMessage(ColorTokenService.Red("One or more task is incomplete. Refer to your journal for more information."));
            }

        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        public override void EndDialog()
        {
        }
    }
}
