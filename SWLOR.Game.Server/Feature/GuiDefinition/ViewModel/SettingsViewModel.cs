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

        public bool SubdualMode
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
            SubdualMode = dbPlayer.Settings.IsSubdualModeEnabled;

            WatchOnClient(model => model.DisplayAchievementNotification);
            WatchOnClient(model => model.DisplayHelmet);
            WatchOnClient(model => model.DisplayHolonetChannel);
            WatchOnClient(model => model.SubdualMode);
        }

        public Action OnSave() => () =>
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            dbPlayer.Settings.DisplayAchievementNotification = DisplayAchievementNotification;
            dbPlayer.Settings.ShowHelmet = DisplayHelmet;
            dbPlayer.Settings.IsHolonetEnabled = DisplayHolonetChannel;
            dbPlayer.Settings.IsSubdualModeEnabled = SubdualMode;

            DB.Set(dbPlayer);

            Gui.TogglePlayerWindow(Player, GuiWindowType.Settings);

            // Post-save actions
            UpdateHelmetDisplay();
            UpdateHolonetSetting();
            UpdateSubdualMode();
        };

        public Action OnCancel() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.Settings);
        };

        public Action OnClickChangeDescription() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.ChangeDescription);
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
        private void UpdateSubdualMode()
        {
            SetLocalBool(Player, "SUBDUAL_MODE", SubdualMode);
        }
    }
}
