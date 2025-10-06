using SWLOR.NWN.API.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Domain.Space.Events;
using SWLOR.Shared.Domain.UI.Events;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Area;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;
using SWLOR.Shared.Events.Events.Player;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Component.Character.Feature
{
    public class PlayerStatusWindow
    {
        private readonly IGuiService _guiService;
        private readonly IEventsPluginService _eventsPlugin;

        public PlayerStatusWindow(IGuiService guiService, IEventsPluginService eventsPlugin)
        {
            _guiService = guiService;
            _eventsPlugin = eventsPlugin;
        }

        [ScriptHandler<OnSWLORItemEquipValidBefore>]
        public void PlayerEquipItem()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player)) 
                return;

            _guiService.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.HP));
            _guiService.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.FP));
            _guiService.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.STM));
        }

        [ScriptHandler<OnItemUnequipBefore>]
        public void PlayerUnequipItem()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player)) 
                return;

            _guiService.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.HP));
            _guiService.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.FP));
            _guiService.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.STM));
        }

        [ScriptHandler<OnPlayerDamaged>]
        public void PlayerDamaged()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            _guiService.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.HP));
        }

        [ScriptHandler<OnPlayerFPAdjusted>]
        public void PlayerFPAdjusted()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            _guiService.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.FP));
        }

        [ScriptHandler<OnPlayerStaminaAdjusted>]
        public void PlayerSTMAdjusted()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            _guiService.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.STM));
        }

        [ScriptHandler<OnHealAfter>]
        public void PlayerHealed()
        {
            var target = StringToObject(_eventsPlugin.GetEventData("TARGET_OBJECT_ID"));
            _guiService.PublishRefreshEvent(target, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.HP));
        }

        [ScriptHandler<OnPlayerShieldAdjusted>]
        public void PlayerShieldAdjusted()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            _guiService.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.Shield));
        }

        [ScriptHandler<OnPlayerHullAdjusted>]
        public void PlayerHullAdjusted()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            _guiService.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.Hull));
        }

        [ScriptHandler<OnPlayerCapAdjusted>]
        public void PlayerCapacitorAdjusted()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            _guiService.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.Capacitor));
        }

        [ScriptHandler<OnPlayerTargetUpdated>]
        public void PlayerSpaceTargetAdjusted()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            _guiService.PublishRefreshEvent(player, new TargetStatusRefreshEvent());
        }

        [ScriptHandler<OnModuleEnter>]
        [ScriptHandler<OnAreaEnter>]
        public void LoadPlayerStatusWindow()
        {
            var player = GetEnteringObject();

            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            if(!_guiService.IsWindowOpen(player, GuiWindowType.PlayerStatus))
                _guiService.TogglePlayerWindow(player, GuiWindowType.PlayerStatus);
        }
    }
}
