using SWLOR.Game.Server.Service.DialogService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class EntryDialog: DialogBase
    {
        private const string MainPageId = "MAIN_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .AddPage(MainPageId, MainPageInit);


            return builder.Build();
        }

        private void MainPageInit(DialogPage page)
        {
            page.Header = "Are you ready to enter the game world? This is the LAST chance for you to use the '/customize' chat command to change your appearance.\n\nAre you sure you want to proceed?";

            page.AddResponse("Customize my character", () =>
            {
                SwitchConversation(nameof(CharacterCustomizationDialog));
            });

            page.AddResponse("Enter the game", () =>
            {
                var player = GetPC();
                var waypoint = GetObjectByTag("ENTRY_STARTING_WP");
                var location = GetLocation(waypoint);

                AssignCommand(player, () =>
                {
                    ActionJumpToLocation(location);
                });
                EndConversation();
            });
        }
    }
}
