using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class AtomicReassembly: ConversationBase
    {
        private readonly IColorTokenService _color;
        private readonly IDataService _data;
        private readonly ICraftService _craft;

        public AtomicReassembly(
            INWScript script, 
            IDialogService dialog,
            IColorTokenService color,
            IDataService data,
            ICraftService craft) 
            : base(script, dialog)
        {
            _color = color;
            _data = data;
            _craft = craft;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage( // Responses are dynamically built
                _color.Green("Atomic Reassembler") + "\n\nThis device can be used to salvage completely-built equipment and reassemble them into components.\n\nStart by selecting the item you wish to create, then insert the item you wish to salvage. The new items created will have a chance to receive property bonuses from the salvaged item.\n\nStart by selecting a component type now.");

            DialogPage pickSalvageItem = new DialogPage(); // Dynamically built

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("PickSalvageItem", pickSalvageItem);
            return dialog;
        }

        public override void Initialize()
        {
            LoadMainPage();
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
        }

        private void LoadMainPage()
        {
            var componentTypes = _data.Where<ComponentType>(x => !string.IsNullOrWhiteSpace(x.ReassembledResref));

            foreach (var type in componentTypes)
            {
                AddResponseToPage("MainPage", type.Name, true, type.ID);
            }
        }

        private void MainPageResponses(int responseID)
        {
            var player = GetPC();
            var model = _craft.GetPlayerCraftingData(player);
            DialogResponse response = GetResponseByID("MainPage", responseID);
            model.SalvageComponentTypeID = (int)response.CustomData;

        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        public override void EndDialog()
        {
        }
    }
}
