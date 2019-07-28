using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    internal class CharacterManagement: ConversationBase
    {
        private class Model
        {
            public bool IsConfirmingDisableSanctuary { get; set; }
        }
        
        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage characterManagementPage = new DialogPage(
                "Character Management & Information Page",
                "Disable PVP Protection",
                "Toggle Helmet Display",
                "Change Character Description"
            );

            dialog.AddPage("MainPage", characterManagementPage);
            
            return dialog;
        }

        public override void Initialize()
        {
            ToggleDisablePVPProtectionOption();
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
                            HandleDisablePVPProtection();
                            break;
                        case 2: // Toggle Helmet Display
                            HelmetToggleService.ToggleHelmetDisplay(GetPC());
                            break;
                        case 3: // Change Character Description
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

        private void ToggleDisablePVPProtectionOption()
        {
            SetResponseVisible("MainPage", 1, PVPSanctuaryService.PlayerHasPVPSanctuary(GetPC()));
        }

        private void HandleDisablePVPProtection()
        {
            Model dto = GetDialogCustomData<Model>();
            
            if (!PVPSanctuaryService.PlayerHasPVPSanctuary(GetPC()))
            {
                return;
            }

            if (dto.IsConfirmingDisableSanctuary)
            {
                PVPSanctuaryService.SetPlayerPVPSanctuaryOverride(GetPC(), true);
                dto.IsConfirmingDisableSanctuary = false;
                _.FloatingTextStringOnCreature(ColorTokenService.Red("PVP protection has been disabled. You may now attack and be attacked by other players."), GetPC().Object, _.FALSE);
                SetResponseText("MainPage", 1, "Disable PVP Protection");
            }
            else
            {
                dto.IsConfirmingDisableSanctuary = true;
                _.FloatingTextStringOnCreature(ColorTokenService.Red("WARNING: PVP protection prevents other players from attacking you. If you disable this, players will immediately be able to attack you anywhere. Click again to confirm."), GetPC().Object, _.FALSE);
                SetResponseText("MainPage", 1, "CONFIRM DISABLE PVP PROTECTION");
            }

            SetDialogCustomData(dto);

            ToggleDisablePVPProtectionOption();
        }

        public override void EndDialog()
        {
        }
    }
}
