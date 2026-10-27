using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The Ship HUD window's service side (plan section 8): opening and closing it with the flight, the refresh
    /// loop while it is open, the All Weapons feat and the numbers its tabs show.
    /// </summary>
    public static class SpaceHud
    {
        private const double RefreshSeconds = 0.5;
        private const int QuickBarSlotCount = 36;

        private static readonly HashSet<uint> _viewers = new();
        private static IDisposable _refreshSchedule;

        /// <summary>Starts refreshing a player's open HUD. The window calls this when it opens.</summary>
        public static void Register(uint player)
        {
            _viewers.Add(player);
            _refreshSchedule ??= Scheduler.ScheduleRepeating(RefreshViewers, TimeSpan.FromSeconds(RefreshSeconds));
        }

        /// <summary>Stops refreshing a player's HUD. The window calls this when it closes.</summary>
        public static void Unregister(uint player)
        {
            _viewers.Remove(player);
        }

        private static void RefreshViewers()
        {
            if (_viewers.Count == 0)
                return;

            foreach (var player in _viewers.ToList())
            {
                if (!GetIsObjectValid(player))
                {
                    _viewers.Remove(player);
                    continue;
                }

                Gui.PublishRefreshEvent(player, new ShipHudRefreshEvent());
            }
        }

        private static bool CanUseHud(uint player)
        {
            return GetIsPC(player) && !GetIsDM(player) && !GetIsDMPossessed(player);
        }

        [NWNEventHandler(ScriptName.OnSpaceEnter)]
        public static void OpenOnSpaceEnter()
        {
            var player = OBJECT_SELF;
            if (CanUseHud(player) && !Gui.IsWindowOpen(player, GuiWindowType.ShipHud))
                Gui.TogglePlayerWindow(player, GuiWindowType.ShipHud);
        }

        [NWNEventHandler(ScriptName.OnSpaceExit)]
        public static void CloseOnSpaceExit()
        {
            var player = OBJECT_SELF;
            if (CanUseHud(player))
                Gui.ClosePlayerWindow(player, GuiWindowType.ShipHud);
        }

        /// <summary>Opens the HUD, or closes it when it is open. Only works while piloting a ship.</summary>
        public static void Toggle(uint player)
        {
            if (!Space.IsPlayerInSpaceMode(player))
            {
                SendMessageToPC(player, "The Ship HUD can only be used while piloting a starship.");
                return;
            }

            Gui.TogglePlayerWindow(player, GuiWindowType.ShipHud);
        }

        [NWNEventHandler(ScriptName.OnFeatUseBefore)]
        public static void HandleAllWeaponsFeat()
        {
            var feat = (FeatType)Convert.ToInt32(EventsPlugin.GetEventData("FEAT_ID"));
            if (feat != FeatType.ShipAllWeapons)
                return;

            Space.ToggleAllWeapons(OBJECT_SELF);
            Gui.PublishRefreshEvent(OBJECT_SELF, new ShipHudRefreshEvent());
        }

        /// <summary>Puts the All Weapons feat in the first empty quickbar slot unless the bar already has it.</summary>
        public static void EnsureAllWeaponsOnHotBar(uint player)
        {
            var firstEmpty = -1;
            for (var index = 0; index < QuickBarSlotCount; index++)
            {
                var slot = PlayerPlugin.GetQuickBarSlot(player, index);
                if (slot.ObjectType == QuickBarSlotType.Feat && slot.INTParam1 == (int)FeatType.ShipAllWeapons)
                    return;

                if (firstEmpty < 0 && slot.ObjectType == QuickBarSlotType.Empty)
                    firstEmpty = index;
            }

            if (firstEmpty >= 0)
                PlayerPlugin.SetQuickBarSlot(player, firstEmpty, PlayerQuickBarSlot.UseFeat(FeatType.ShipAllWeapons));
        }

        /// <summary>The modules that are on, as the HUD lists them for a seat.</summary>
        public static List<ShipHudModuleRow> GetModuleRows(uint player, ShipStatus status, ShipCrewSeat seat)
        {
            var (target, _) = Space.GetCurrentTarget(player);
            return ShipHudRules.BuildModuleRows(
                status,
                seat,
                DateTime.UtcNow,
                Space.FindShipModuleDetail,
                GetIsObjectValid(target) ? target : OBJECT_INVALID,
                player,
                other => GetIsObjectValid(other) ? PlayerName.GetDisplayName(player, other) : null);
        }

        /// <summary>The weapon toggles a seat controls and how many are on.</summary>
        public static (int On, int Total) GetWeaponCounts(ShipStatus status, ShipCrewSeat seat)
        {
            var slots = ShipHudRules.GetWeaponSlots(status, seat, Space.FindShipModuleDetail);
            return (ShipHudRules.CountOn(status, slots), slots.Count);
        }

        /// <summary>The Stats tab's numbers, from the same getters combat uses.</summary>
        public static ShipHudStats CollectStats(uint player, ShipStatus status)
        {
            var stats = ShipHudStats.FromStatus(status, DateTime.UtcNow);
            var energy = new SpaceWeaponShot { Family = SpaceWeaponFamily.Energy };
            var launcher = new SpaceWeaponShot { Family = SpaceWeaponFamily.Launcher };

            stats.ShieldRegenPerSecond = Space.GetShipShieldRegenPerSecond(player, status);
            stats.CapacitorRegenPerSecond = Space.GetShipCapacitorRegenPerSecond(player, status);
            stats.EnergyAttack = Space.GetShipAttack(player, status, energy);
            stats.LauncherAttack = Space.GetShipAttack(player, status, launcher);
            stats.EnergyAccuracy = Space.GetShipAccuracy(player, status, energy);
            stats.LauncherAccuracy = Space.GetShipAccuracy(player, status, launcher);
            stats.Defense = Space.GetShipDefense(player, status);
            stats.Evasion = Space.GetShipEvasion(player, status);
            stats.CriticalRate = Space.GetShipCriticalRate(player, status);

            var ship = Space.FindShipDetail(status.ItemTag);
            if (ship != null)
            {
                stats.HighSlots = ship.HighPowerNodes;
                stats.MidSlots = ship.MidPowerNodes;
                stats.LowSlots = ship.LowPowerNodes;
                stats.Cargo = ship.Hull?.Cargo;
            }

            return stats;
        }
    }
}
