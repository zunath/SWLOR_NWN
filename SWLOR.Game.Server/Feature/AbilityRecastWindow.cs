using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public static class AbilityRecastWindow
    {
        private const int MaxNumberOfRecastTimers = 10;
        private static Gui.IdReservation _idReservation;

        [NWNEventHandler("mod_load")]
        public static void ReserveGuiIds()
        {
            _idReservation = Gui.ReserveIds(nameof(AbilityRecastWindow), MaxNumberOfRecastTimers * 2);
        }

        [NWNEventHandler("interval_pc_1s")]
        public static void DrawGuiElements()
        {
            DrawRecastComponent(OBJECT_SELF);
        }

        private static void DrawRecastComponent(uint player)
        {
            const int WindowX = 4;
            const int WindowY = 8;
            const int WindowWidth = 25;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
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
                PostString(player, text, centerWindowX+2, WindowY + numberOfRecasts, ScreenAnchor.TopRight, 1.1f, Gui.ColorWhite, Gui.ColorWhite, _idReservation.StartId + numberOfRecasts, Gui.TextName);
            }

            if (numberOfRecasts > 0)
            {
                Gui.DrawWindow(player, _idReservation.StartId + MaxNumberOfRecastTimers, ScreenAnchor.TopRight, WindowX, WindowY, WindowWidth-2, 1 + numberOfRecasts, 1.1f);
            }
        }

        private static string BuildTimerText(RecastGroup group, DateTime now, DateTime recastTime)
        {
            var recastName = (Ability.GetRecastGroupName(group) + ":").PadRight(14, ' ');
            var delta = recastTime - now;
            var formatTime = delta.ToString(@"hh\:mm\:ss").PadRight(8, ' ');
            return recastName + formatTime;
        }
    }
}
