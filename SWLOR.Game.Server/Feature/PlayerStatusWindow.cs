using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
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

            Combat.ApplyDamageTakenEffects(player, GetLastDamager(player), GetTotalDamageDealt());
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
            ApplyStatusDisplay(player);
        }

        [NWNEventHandler(ScriptName.OnSpaceEnter)]
        [NWNEventHandler(ScriptName.OnSpaceExit)]
        public static void RefreshStatusDisplayOnSpaceTransition()
        {
            ApplyStatusDisplay(OBJECT_SELF);
        }

        private static readonly GuiWindowType[] _statusWindows =
        {
            GuiWindowType.PlayerStatus,
            GuiWindowType.PlayerStatusPortrait,
            GuiWindowType.PlayerStatusPortraitSpace,
        };

        /// <summary>
        /// Opens the appropriate vitals display for the player and closes the others. With the
        /// Mini-Vitals setting enabled, the compact portrait overlay is used - the 2-bar STM/FP
        /// version on foot, or the 3-bar shield/hull/capacitor version in space. Otherwise the
        /// docked window is used, which already adapts to both on foot and in space.
        /// </summary>
        public static void ApplyStatusDisplay(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            GuiWindowType target;
            if (ShouldUsePortraitVitals(player))
            {
                target = Space.IsPlayerInSpaceMode(player)
                    ? GuiWindowType.PlayerStatusPortraitSpace
                    : GuiWindowType.PlayerStatusPortrait;
            }
            else
            {
                target = GuiWindowType.PlayerStatus;
            }

            foreach (var window in _statusWindows)
            {
                if (window == target)
                    EnsureWindowOpen(player, window);
                else
                    EnsureWindowClosed(player, window);
            }
        }

        private static bool ShouldUsePortraitVitals(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            // Default to the portrait overlay when the setting has never been set.
            return dbPlayer?.Settings.PortraitVitals ?? true;
        }

        private static void EnsureWindowOpen(uint player, GuiWindowType type)
        {
            if (!Gui.IsWindowOpen(player, type))
                Gui.TogglePlayerWindow(player, type);
        }

        private static void EnsureWindowClosed(uint player, GuiWindowType type)
        {
            if (Gui.IsWindowOpen(player, type))
                Gui.TogglePlayerWindow(player, type);
        }
    }
}
