using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.KeyItemService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class SliceTerminalDialog: ConversationMenuDefinition
    {
        private const string MainPageId = "MAIN_PAGE";

        public override ConversationMenuSpec Build()
        {
            var builder = new ConversationMenuBuilder()
                .AddPage(MainPageId, MainPageInit);

            return builder.Build();
        }

        private void MainPageInit(ConversationMenuPage page)
        {
            page.Header = "You can slice this terminal. What would you like to do?";

            page.AddResponse("Slice the terminal", () =>
            {
                var player = Player;
                var self = Owner;
                var keyItemId = GetLocalInt(self, "KEY_ITEM_ID");

                if (keyItemId <= 0)
                {
                    FloatingTextStringOnCreature("ERROR: Improperly configured key item. Id is not set. Notify an admin this quest is broken.", player, false);
                    return;
                }

                var keyItemType = (KeyItemType) keyItemId;
                KeyItem.GiveKeyItem(player, keyItemType);
                ObjectVisibility.AdjustVisibility(player, self, VisibilityType.Hidden);

                Close();
            });
        }
    }
}
