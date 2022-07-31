using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature
{
    public static class PlayerStatusWindow
    {

        [NWNEventHandler("item_eqp_bef")]
        public static void PlayerEquipItem()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player)) 
                return;

            Gui.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.HP));
            Gui.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.FP));
            Gui.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.STM));
        }

        [NWNEventHandler("item_uneqp_bef")]
        public static void PlayerUnequipItem()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player)) 
                return;

            Gui.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.HP));
            Gui.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.FP));
            Gui.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.STM));
        }

        [NWNEventHandler("pc_damaged")]
        public static void PlayerDamaged()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            Gui.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.HP));
        }

        [NWNEventHandler("pc_fp_adjusted")]
        public static void PlayerFPAdjusted()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            Gui.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.FP));
        }

        [NWNEventHandler("pc_stm_adjusted")]
        public static void PlayerSTMAdjusted()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            Gui.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.STM));
        }

        [NWNEventHandler("heal_aft")]
        public static void PlayerHealed()
        {
            var target = StringToObject(EventsPlugin.GetEventData("TARGET_OBJECT_ID"));
            Gui.PublishRefreshEvent(target, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.HP));
        }

        [NWNEventHandler("pc_shld_adjusted")]
        public static void PlayerShieldAdjusted()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            Gui.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.Shield));
        }

        [NWNEventHandler("pc_hull_adjusted")]
        public static void PlayerHullAdjusted()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            Gui.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.Hull));
        }

        [NWNEventHandler("pc_cap_adjusted")]
        public static void PlayerCapacitorAdjusted()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            Gui.PublishRefreshEvent(player, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.Capacitor));
        }

        [NWNEventHandler("pc_target_upd")]
        public static void PlayerSpaceTargetAdjusted()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            Gui.PublishRefreshEvent(player, new TargetStatusRefreshEvent());
        }

        [NWNEventHandler("mod_enter")]
        [NWNEventHandler("area_enter")]
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
