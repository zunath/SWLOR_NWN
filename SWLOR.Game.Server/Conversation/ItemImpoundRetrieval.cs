using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject.Dialog;
using static NWN._;

namespace SWLOR.Game.Server.Conversation
{
    public class ItemImpoundRetrieval: ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage("Welcome to the planetary impound. Any items which were seized by our government may be retrieved here... for a price, of course!\n\nEach item may be retrieved for 50 credits.\n\nHow may I help you?");
            
            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
            LoadMainPage();
        }

        private void LoadMainPage()
        {
            var player = GetPC();
            var items = DataService.PCImpoundedItem.GetAllByPlayerIDAndNotRetrieved(player.GlobalID);

            ClearPageResponses("MainPage");
            foreach (var item in items)
            {
                AddResponseToPage("MainPage", item.ItemName, true, item.ID);
            }
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            if (player.Gold < 50)
            {
                player.FloatingText("You don't have enough credits to retrieve that item.");
                return;
            }

            var response = GetResponseByID("MainPage", responseID);
            Guid pcImpoundedItemID = (Guid)response.CustomData;
            var item = DataService.PCImpoundedItem.GetByID(pcImpoundedItemID);

            if (item.DateRetrieved != null)
            {
                player.FloatingText("You have already retrieved that item.");
                return;
            }

            item.DateRetrieved = DateTime.UtcNow;
            DataService.SubmitDataChange(item, DatabaseActionType.Update);
            SerializationService.DeserializeItem(item.ItemObject, player);
            _.TakeGoldFromCreature(50, player, TRUE);

            LoadMainPage();
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        public override void EndDialog()
        {
        }
    }
}
