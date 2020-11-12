using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;
using SWLOR.Game.Server.Legacy.ValueObject.Dialog;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Legacy.Conversation
{
    public class CharacterManagement: ConversationBase
    {
        private class Model
        {
            public bool IsConfirmingDisableSanctuary { get; set; }
        }
        
        public override PlayerDialog SetUp(NWPlayer player)
        {
            var dialog = new PlayerDialog("MainPage");
            var characterManagementPage = new DialogPage(
                "Character Management & Information Page",
                "Disable PVP Protection",
                "Toggle Helmet Display",
                "Toggle Dual Blaster Pistol Mode",
                "Change Character Description"
            );

            dialog.AddPage("MainPage", characterManagementPage);
            
            return dialog;
        }

        public override void Initialize()
        {
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                {
                    switch (responseID)
                    {
                        case 1: // Disable PVP Protection
                            break;
                        case 2: // Toggle Helmet Display
                            break;
                        case 3: // Toggle Dual Pistol Display
                            DualPistolService.ToggleDualPistolMode(GetPC());
                            break;
                        case 4: // Change Character Description
                            SwitchConversation("ChangeDescription");
                            break;
                    }
                    break;
                }
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
