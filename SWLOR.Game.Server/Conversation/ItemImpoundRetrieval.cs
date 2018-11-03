using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Conversation
{
    public class ItemImpoundRetrieval: ConversationBase
    {
        private readonly IDataService _data;
        private readonly ISerializationService _serialization;

        public ItemImpoundRetrieval(
            INWScript script, 
            IDialogService dialog,
            IDataService data,
            ISerializationService serialization) 
            : base(script, dialog)
        {
            _data = data;
            _serialization = serialization;
        }

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
            var items = _data.PCImpoundedItems.Where(x => x.PlayerID == player.GlobalID && x.DateRetrieved == null).ToList();

            ClearPageResponses("MainPage");
            foreach (var item in items)
            {
                AddResponseToPage("MainPage", item.ItemName, true, item.PCImpoundedItemID);
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
            int pcImpoundedItemID = (int)response.CustomData;
            var item = _data.PCImpoundedItems.Single(x => x.PCImpoundedItemID == pcImpoundedItemID);

            if (item.DateRetrieved != null)
            {
                player.FloatingText("You have already retrieved that item.");
                return;
            }

            item.DateRetrieved = DateTime.UtcNow;
            _data.SaveChanges();

            _serialization.DeserializeItem(item.ItemObject, player);
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
