using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.NWScript;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class GuildMasterDialog: DialogBase
    {
        private class Model
        {
            public GuildType Guild { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";
        private const string TellMePageId = "TELL_ME_PAGE";
        private const string RankTooLowPageId = "RANK_TOO_LOW_PAGE";
        private const string GuildStorePageId = "GUILD_STORE_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .WithDataModel(new Model())
                .AddInitializationAction(Initialization)
                .AddPage(MainPageId, MainPageInit)
                .AddPage(TellMePageId, TellMePageInit)
                .AddPage(RankTooLowPageId, RankTooLowPageInit)
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
                var payload = new GuildTasksPayload(model.Guild, GetDialogTarget());
                Gui.TogglePlayerWindow(player, GuiWindowType.GuildTasks, payload, GetDialogTarget());
                EndConversation();
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
