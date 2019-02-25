using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class AtomicReassembly: ConversationBase
    {
        private readonly IColorTokenService _color;

        public AtomicReassembly(
            INWScript script, 
            IDialogService dialog,
            IColorTokenService color) 
            : base(script, dialog)
        {
            _color = color;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage(
                _color.Green("Atomic Reassembler") + "\n\nThis device can be used to salvage completely-built equipment and reassemble them into components.\n\nStart by selecting the item you wish to create, then insert the item you wish to salvage. The new items created will have a chance to receive property bonuses from the salvaged item.\n\nStart by selecting a component type now.");

            DialogPage pickSalvageItem = new DialogPage("<SET LATER>",
                "Pick an item to salvage");

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("PickSalvageItem", pickSalvageItem);
            return dialog;
        }

        public override void Initialize()
        {
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
        }

        private void LoadMainPage()
        {

        }

        private void MainPageResponses(int responseID)
        {

        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        public override void EndDialog()
        {
        }
    }
}
