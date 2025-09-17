using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Feature
{
    public static class PlayerStatusWindow
    {

        [NWNEventHandler(ScriptName.OnSWLORItemEquipValidBefore)]
        public static void PlayerEquipItem()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player)) 
                return;

            Gui.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.HP));
            Gui.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.FP));
            Gui.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.STM));
        }

        [NWNEventHandler(ScriptName.OnItemUnequipBefore)]
        public static void PlayerUnequipItem()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player)) 
                return;

            Gui.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.HP));
            Gui.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.FP));
            Gui.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.STM));
        }

        [NWNEventHandler(ScriptName.OnPlayerDamaged)]
        public static void PlayerDamaged()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            Gui.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.HP));
        }

        [NWNEventHandler(ScriptName.OnPlayerFPAdjusted)]
        public static void PlayerFPAdjusted()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            Gui.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.FP));
        }

        [NWNEventHandler(ScriptName.OnPlayerStaminaAdjusted)]
        public static void PlayerSTMAdjusted()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            Gui.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.STM));
        }

        [NWNEventHandler(ScriptName.OnHealAfter)]
        public static void PlayerHealed()
        {
            var target = StringToObject(EventsPlugin.GetEventData("TARGET_OBJECT_ID"));
            Gui.PublishRefreshEvent(target, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.HP));
        }

        [NWNEventHandler(ScriptName.OnPlayerShieldAdjusted)]
        public static void PlayerShieldAdjusted()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            Gui.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.Shield));
        }

        [NWNEventHandler(ScriptName.OnPlayerHullAdjusted)]
        public static void PlayerHullAdjusted()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            Gui.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.Hull));
        }

        [NWNEventHandler(ScriptName.OnPlayerCapAdjusted)]
        public static void PlayerCapacitorAdjusted()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            Gui.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.Capacitor));
        }

        [NWNEventHandler(ScriptName.OnPlayerTargetUpdated)]
        public static void PlayerSpaceTargetAdjusted()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            Gui.PublishRefreshEvent(player, new TargetStatusRefreshEvent());
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void LoadPlayerStatusWindow()
        {
            var player = GetEnteringObject();

            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            if(!Gui.IsWindowOpen(player, GuiWindowType.PlayerStatus))
                Gui.TogglePlayerWindow(player, GuiWindowType.PlayerStatus);
        }
    }
}
