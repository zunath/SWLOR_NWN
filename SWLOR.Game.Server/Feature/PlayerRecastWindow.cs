using System;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature
{
    public static class PlayerRecastWindow
    {
        private const int MaxNumberOfRecastTimers = 10;
        private const int MaxNumberOfShipModules = 15;
        private static Gui.IdReservation _recastIdReservation;
        private static Gui.IdReservation _shipModuleIdReservation;

        /// <summary>
        /// When the module loads, reserve Gui Ids for both window types.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void ReserveGuiIds()
        {
            _recastIdReservation = Gui.ReserveIds(nameof(PlayerRecastWindow) + "_ABILITIES", MaxNumberOfRecastTimers * 2);
            _shipModuleIdReservation = Gui.ReserveIds(nameof(PlayerRecastWindow) + "_SHIPMODULES", MaxNumberOfShipModules * 2);
        }

        /// <summary>
        /// Every second, redraw the window for the player. Window drawn depends on the mode the player is currently in (Character or Space).
        /// </summary>
        //[NWNEventHandler("interval_pc_1s")] // todo: temporarily disabled due to performance reasons.
        public static void DrawGuiElements()
        {
            var player = OBJECT_SELF;

            if (Space.IsPlayerInSpaceMode(player))
            {
                DrawSpaceRecastComponent(player);
            }
            else
            {
                DrawCharacterRecastComponent(player);
            }
        }

        /// <summary>
        /// Handles drawing the window for ability recast timers.
        /// Only used when player is in Character mode (i.e not space)
        /// </summary>
        /// <param name="player">The player to draw the component for.</param>
        private static void DrawCharacterRecastComponent(uint player)
        {
            static string BuildTimerText(RecastGroup group, DateTime now, DateTime recastTime)
            {
                var recastName = (Recast.GetRecastGroupName(group) + ":").PadRight(14, ' ');
                var delta = recastTime - now;
                var formatTime = delta.ToString(@"hh\:mm\:ss").PadRight(8, ' ');
                return recastName + formatTime;
            }

            const int WindowX = 4;
            const int WindowY = 8;
            const int WindowWidth = 25;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null)
                return;

            var now = DateTime.UtcNow;

            var numberOfRecasts = 0;
            foreach (var (group, dateTime) in dbPlayer.RecastTimes)
            {
                // Skip over any date times that have expired but haven't been cleaned up yet.
                if(dateTime < now) continue;

                // Max of 10 recasts can be shown in the window.
                if (numberOfRecasts >= MaxNumberOfRecastTimers) break;

                var text = BuildTimerText(group, now, dateTime);
                var centerWindowX = Gui.CenterStringInWindow(text, WindowX, WindowWidth);

                numberOfRecasts++;
                PostString(player, text, centerWindowX+2, WindowY + numberOfRecasts, ScreenAnchor.TopRight, 1.1f, Gui.ColorWhite, Gui.ColorWhite, _recastIdReservation.StartId + numberOfRecasts, Gui.TextName);
            }

            if (numberOfRecasts > 0)
            {
                Gui.DrawWindow(player, _recastIdReservation.StartId + MaxNumberOfRecastTimers, ScreenAnchor.TopRight, WindowX, WindowY, WindowWidth-2, 1 + numberOfRecasts, 1.1f);
            }
        }

        /// <summary>
        /// Handles drawing the recast window for ship modules.
        /// Only used when player is in Space mode.
        /// </summary>
        /// <param name="player">The player to draw the component for.</param>
        private static void DrawSpaceRecastComponent(uint player)
        {
            static string BuildTimerText(string itemTag, DateTime now, DateTime recastTime)
            {
                var shipModule = Space.GetShipModuleDetailByItemTag(itemTag);

                var recastName = (shipModule.ShortName + ":").PadRight(14, ' ');

                // Display 'READY' instead of the timer since time has passed.
                if (recastTime < now)
                {
                    return recastName + "READY".PadRight(8, ' ');
                }

                // Otherwise display the recast timer
                var delta = recastTime - now;
                var formatTime = delta.ToString(@"hh\:mm\:ss").PadRight(8, ' ');
                return recastName + formatTime;
            }

            const int WindowX = 4;
            const int WindowY = 8;
            const int WindowWidth = 25;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null)
                return;

            var dbPlayerShip = DB.Get<PlayerShip>(dbPlayer.ActiveShipId);

            if (dbPlayerShip == null)
                throw new Exception($"Could not locate ship Id '{dbPlayer.ActiveShipId}' for player Id '{playerId}'.");

            var now = DateTime.UtcNow;
            var allModules = dbPlayerShip.Status.HighPowerModules.Concat(dbPlayerShip.Status.LowPowerModules).ToList();

            var numberOfRecasts = 0;
            foreach (var (_, shipModule) in allModules)
            {
                var moduleDetail = Space.GetShipModuleDetailByItemTag(shipModule.ItemTag);

                // Skip any passive modules (they don't have recast timers)
                if (moduleDetail.Type == ShipModuleType.Passive) continue;

                // Max of 15 recasts can be shown in the window.
                if (numberOfRecasts >= MaxNumberOfShipModules) break;

                var text = BuildTimerText(shipModule.ItemTag, now, shipModule.RecastTime);
                var centerWindowX = Gui.CenterStringInWindow(text, WindowX, WindowWidth);
                PostString(player, text, centerWindowX + 2, WindowY + numberOfRecasts, ScreenAnchor.TopRight, 1.1f, Gui.ColorWhite, Gui.ColorWhite, _shipModuleIdReservation.StartId + numberOfRecasts, Gui.TextName);

                numberOfRecasts++;
            }

            if (numberOfRecasts > 0)
            {
                Gui.DrawWindow(player, _shipModuleIdReservation.StartId + MaxNumberOfShipModules, ScreenAnchor.TopRight, WindowX, WindowY-1, WindowWidth - 2, 1 + numberOfRecasts, 1.1f);
            }
        }

        //[NWNEventHandler("interval_pc_1s")] // todo: temporarily disabled due to performance issues
        public static void CleanUpExpiredRecastTimers()
        {
            var player = OBJECT_SELF;
            if (GetIsDM(player) || GetIsDMPossessed(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (dbPlayer == null)
                return;

            var now = DateTime.UtcNow;

            foreach (var (group, dateTime) in dbPlayer.RecastTimes)
            {
                if (dateTime > now) continue;

                dbPlayer.RecastTimes.Remove(group);
            }

            DB.Set(dbPlayer);
        }
    }
}
