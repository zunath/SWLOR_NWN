using SWLOR.NWN.API.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Domain.Space.Events;
using SWLOR.Shared.Domain.UI.Events;
using SWLOR.Shared.Events.Events.Area;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;
using SWLOR.Shared.Events.Events.Player;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Character.Feature
{
    public class PlayerStatusWindow
    {
        private readonly IGuiService _guiService;
        private readonly IEventsPluginService _eventsPlugin;

        public PlayerStatusWindow(
            IGuiService guiService,
            IEventsPluginService eventsPlugin,
            IEventAggregator eventAggregator)
        {
            _guiService = guiService;
            _eventsPlugin = eventsPlugin;

            // Subscribe to events
            eventAggregator.Subscribe<OnSWLORItemEquipValidBefore>(e => PlayerEquipItem());
            eventAggregator.Subscribe<OnItemUnequipBefore>(e => PlayerUnequipItem());
            eventAggregator.Subscribe<OnPlayerDamaged>(e => PlayerDamaged());
            eventAggregator.Subscribe<OnPlayerFPAdjusted>(e => PlayerFPAdjusted());
            eventAggregator.Subscribe<OnPlayerStaminaAdjusted>(e => PlayerSTMAdjusted());
            eventAggregator.Subscribe<OnHealAfter>(e => PlayerHealed());
            eventAggregator.Subscribe<OnPlayerShieldAdjusted>(e => PlayerShieldAdjusted());
            eventAggregator.Subscribe<OnPlayerHullAdjusted>(e => PlayerHullAdjusted());
            eventAggregator.Subscribe<OnPlayerCapAdjusted>(e => PlayerCapacitorAdjusted());
            eventAggregator.Subscribe<OnPlayerTargetUpdated>(e => PlayerSpaceTargetAdjusted());
            eventAggregator.Subscribe<OnModuleEnter>(e => LoadPlayerStatusWindow());
            eventAggregator.Subscribe<OnAreaEnter>(e => LoadPlayerStatusWindow());
        }
        public void PlayerEquipItem()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player)) 
                return;

            _guiService.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.HP));
            _guiService.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.FP));
            _guiService.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.STM));
        }
        public void PlayerUnequipItem()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player)) 
                return;

            _guiService.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.HP));
            _guiService.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.FP));
            _guiService.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.STM));
        }
        public void PlayerDamaged()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            _guiService.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.HP));
        }
        public void PlayerFPAdjusted()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            _guiService.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.FP));
        }
        public void PlayerSTMAdjusted()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            _guiService.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.STM));
        }
        public void PlayerHealed()
        {
            var target = StringToObject(_eventsPlugin.GetEventData("TARGET_OBJECT_ID"));
            _guiService.PublishRefreshEvent(target, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.HP));
        }
        public void PlayerShieldAdjusted()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            _guiService.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.Shield));
        }
        public void PlayerHullAdjusted()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            _guiService.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.Hull));
        }
        public void PlayerCapacitorAdjusted()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            _guiService.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.Capacitor));
        }
        public void PlayerSpaceTargetAdjusted()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            _guiService.PublishRefreshEvent(player, new TargetStatusRefreshEvent());
        }

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
