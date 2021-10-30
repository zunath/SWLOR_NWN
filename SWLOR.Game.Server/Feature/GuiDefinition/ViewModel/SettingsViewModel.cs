using System;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class SettingsViewModel: GuiViewModelBase<SettingsViewModel, GuiPayloadBase>
    {
        public bool DisplayAchievementNotification
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool DisplayHelmet
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool DisplayHolonetChannel
        {
            get => Get<bool>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            DisplayAchievementNotification = dbPlayer.Settings.DisplayAchievementNotification;
            DisplayHelmet = dbPlayer.Settings.ShowHelmet;
            DisplayHolonetChannel = dbPlayer.Settings.IsHolonetEnabled;
            
            WatchOnClient(model => model.DisplayAchievementNotification);
            WatchOnClient(model => model.DisplayHelmet);
            WatchOnClient(model => model.DisplayHolonetChannel);
        }

        public Action OnSave() => () =>
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            dbPlayer.Settings.DisplayAchievementNotification = DisplayAchievementNotification;
            dbPlayer.Settings.ShowHelmet = DisplayHelmet;
            dbPlayer.Settings.IsHolonetEnabled = DisplayHolonetChannel;

            DB.Set(playerId, dbPlayer);

            Gui.TogglePlayerWindow(Player, GuiWindowType.Settings);

            // Post-save actions
            UpdateHelmetDisplay();
            UpdateHolonetSetting();
        };

        public Action OnCancel() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.Settings);
        };

        private void UpdateHelmetDisplay()
        {
            var helmet = GetItemInSlot(InventorySlot.Head, Player);
            if (GetIsObjectValid(helmet))
            {
                SetHiddenWhenEquipped(helmet, !DisplayHelmet);
            }
        }

        private void UpdateHolonetSetting()
        {
            SetLocalBool(Player, "DISPLAY_HOLONET", DisplayHolonetChannel);
        }

    }
}
