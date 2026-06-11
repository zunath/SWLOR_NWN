using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class PazaakTableDialog : DialogBase
    {
        private const string MainPageId = "MAIN_PAGE";
        private const string DefaultCardStoreTag = "pazaak_cards";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .AddPage(MainPageId, MainPageInit);

            return builder.Build();
        }

        private void MainPageInit(DialogPage page)
        {
            var player = GetPC();
            var target = GetDialogTarget();
            var npcProfileId = GetLocalString(target, Pazaak.NpcProfileLocalVariable);
            var npcRewardId = GetLocalString(target, Pazaak.NpcRewardIdLocalVariable);
            var npcDisplayName = GetLocalString(target, Pazaak.NpcDisplayNameLocalVariable);
            var vendorStoreTag = GetLocalString(target, Pazaak.VendorStoreTagLocalVariable);
            var vendorTier = GetLocalInt(target, Pazaak.VendorTierLocalVariable);
            if (string.IsNullOrWhiteSpace(npcDisplayName))
                npcDisplayName = GetName(target);

            page.Header = string.IsNullOrWhiteSpace(npcProfileId)
                ? "The Pazaak table is ready."
                : $"{npcDisplayName} is ready for Pazaak.";

            page.AddResponse("Play Pazaak", () =>
            {
                Gui.TogglePlayerWindow(player, GuiWindowType.Pazaak, new PazaakPayload(target, npcProfileId, npcRewardId, npcDisplayName), target);
                EndConversation();
            });

            if (!string.IsNullOrWhiteSpace(vendorStoreTag) || vendorTier > 0)
            {
                page.AddResponse("Browse Pazaak cards", () =>
                {
                    var storeTag = string.IsNullOrWhiteSpace(vendorStoreTag)
                        ? DefaultCardStoreTag
                        : vendorStoreTag;
                    var store = GetNearestObjectByTag(storeTag, target);

                    if (!GetIsObjectValid(store))
                    {
                        SendMessageToPC(player, "This card vendor's store could not be located.");
                    }
                    else
                    {
                        OpenStore(store, player);
                    }

                    EndConversation();
                });
            }
        }
    }
}
