using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    internal class CharacterManagement: ConversationBase
    {
        private class Model
        {
            public bool IsConfirmingDisableSanctuary { get; set; }
        }

        private readonly IPVPSanctuaryService _pvpSanctuary;
        private readonly IColorTokenService _color;
        private readonly IHelmetToggleService _helmetToggle;

        public CharacterManagement(
             
            IDialogService dialog,
            IPVPSanctuaryService pvpSanctuary,
            IColorTokenService color,
            IHelmetToggleService helmetToggle) 
            : base(dialog)
        {
            _pvpSanctuary = pvpSanctuary;
            _color = color;
            _helmetToggle = helmetToggle;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage characterManagementPage = new DialogPage(
                "Character Management & Information Page",
                "Disable PVP Protection",
                "Toggle Helmet Display"
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
                            _helmetToggle.ToggleHelmetDisplay(GetPC());
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
            SetResponseVisible("MainPage", 1, _pvpSanctuary.PlayerHasPVPSanctuary(GetPC()));
        }

        private void HandleDisablePVPProtection()
        {
            Model dto = GetDialogCustomData<Model>();
            
            if (!_pvpSanctuary.PlayerHasPVPSanctuary(GetPC()))
            {
                return;
            }

            if (dto.IsConfirmingDisableSanctuary)
            {
                _pvpSanctuary.SetPlayerPVPSanctuaryOverride(GetPC(), true);
                dto.IsConfirmingDisableSanctuary = false;
                _.FloatingTextStringOnCreature(_color.Red("PVP protection has been disabled. You may now attack and be attacked by other players."), GetPC().Object, _.FALSE);
                SetResponseText("MainPage", 1, "Disable PVP Protection");
            }
            else
            {
                dto.IsConfirmingDisableSanctuary = true;
                _.FloatingTextStringOnCreature(_color.Red("WARNING: PVP protection prevents other players from attacking you. If you disable this, players will immediately be able to attack you anywhere. Click again to confirm."), GetPC().Object, _.FALSE);
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
