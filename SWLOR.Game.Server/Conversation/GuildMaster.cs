using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class GuildMaster: ConversationBase
    {
        private class Model
        {
            public GuildType Guild { get; set; }
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage("<SET LATER>",
                "Tell me about guilds.",
                "What tasks do you have for me?",
                "Show me the guild shop.");

            DialogPage tellMePage = new DialogPage();
            DialogPage rankTooLowPage = new DialogPage("I'm sorry but your rank is too low to grant you access to that. Perform tasks for us and come back when you've increased your rank with our guild.");
            DialogPage taskPage = new DialogPage("The following tasks are available for you.");

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("TellMePage", tellMePage);
            dialog.AddPage("RankTooLowPage", rankTooLowPage);
            dialog.AddPage("TaskPage", taskPage);
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
            }
        }

        private void LoadMainPage()
        {
            var player = GetPC();
            var model = GetDialogCustomData<Model>();
            var guild = DataService.Get<Guild>((int) model.Guild);
            var pcGP = DataService.Single<PCGuildPoint>(x => x.GuildID == guild.ID && x.PlayerID == player.GlobalID);
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
                case 2: // What tasks do you have for me?
                    break;
                case 3: // Show me the guild shop.
                    HandleGuildShopPage();
                    break;
            }
        }

        private void LoadTellMePage()
        {
            var guilds = DataService.GetAll<Guild>();

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

        private void HandleGuildShopPage()
        {
            var player = GetPC();
            var model = GetDialogCustomData<Model>();
            var pcGP = DataService.Single<PCGuildPoint>(x => x.GuildID == (int)model.Guild && x.PlayerID == player.GlobalID);

            // If player's rank is too low, send them to the page explaining that.
            if (pcGP.Rank <= 0)
            {
                ChangePage("RankTooLowPage");
            }
            // Otherwise open up the shop the player has access to.
            else
            {
                var speaker = GetDialogTarget();
                string storeTag = speaker.GetLocalString("STORE_TAG_RANK_" + pcGP.Rank);

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
        

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        public override void EndDialog()
        {
        }
    }
}
