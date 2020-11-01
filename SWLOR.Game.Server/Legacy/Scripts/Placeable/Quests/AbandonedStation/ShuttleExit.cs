using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Legacy.Scripts.Placeable.Quests.AbandonedStation
{
    public class ShuttleExit: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlayer player = GetLastUsedBy();
            NWObject waypoint = GetObjectByTag("ABAN_STATION_EXIT_LANDING");
            player.AssignCommand(() => { ActionJumpToLocation(waypoint.Location); });

            // Attempt to clean up the instanced areas in 12 seconds.
            DelayCommand(12.0f, AttemptCleanup);
        }

        private void AttemptCleanup()
        {
            NWPlaceable exit = OBJECT_SELF;
            var area = GetArea(exit);
            uint mainLevel = GetLocalObject(area, "MAIN_LEVEL");
            uint restrictedLevel = GetLocalObject(area, "RESTRICTED_LEVEL");
            uint directorsChambers = GetLocalObject(area, "DIRECTORS_CHAMBERS");

            var playersInAreas = Core.NWNX.Area.GetNumberOfPlayersInArea(mainLevel) +
                                 Core.NWNX.Area.GetNumberOfPlayersInArea(restrictedLevel) +
                                 Core.NWNX.Area.GetNumberOfPlayersInArea(directorsChambers);

            // There are still players in the areas. We can't clean up yet.
            if (playersInAreas > 0) return;

            // Otherwise, everyone has left. Do the cleanup now.
            AreaService.DestroyAreaInstance(mainLevel);
            AreaService.DestroyAreaInstance(restrictedLevel);
            AreaService.DestroyAreaInstance(directorsChambers);
        }
    }
}
