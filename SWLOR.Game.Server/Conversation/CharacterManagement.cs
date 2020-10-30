using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Legacy;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
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

        private void ToggleDisablePVPProtectionOption()
        {
            SetResponseVisible("MainPage", 1, PVPSanctuaryService.PlayerHasPVPSanctuary(GetPC()));
        }

        private void HandleDisablePVPProtection()
        {
            var dto = GetDialogCustomData<Model>();
            
            if (!PVPSanctuaryService.PlayerHasPVPSanctuary(GetPC()))
            {
                return;
            }

            if (dto.IsConfirmingDisableSanctuary)
            {
                PVPSanctuaryService.SetPlayerPVPSanctuaryOverride(GetPC(), true);
                dto.IsConfirmingDisableSanctuary = false;
                NWScript.FloatingTextStringOnCreature(ColorTokenService.Red("PVP protection has been disabled. You may now attack and be attacked by other players."), GetPC().Object, false);
                SetResponseText("MainPage", 1, "Disable PVP Protection");
            }
            else
            {
                dto.IsConfirmingDisableSanctuary = true;
                NWScript.FloatingTextStringOnCreature(ColorTokenService.Red("WARNING: PVP protection prevents other players from attacking you. If you disable this, players will immediately be able to attack you anywhere. Click again to confirm."), GetPC().Object, false);
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
