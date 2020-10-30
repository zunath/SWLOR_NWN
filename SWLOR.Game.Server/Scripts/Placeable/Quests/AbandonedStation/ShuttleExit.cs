using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Scripts.Placeable.Quests.AbandonedStation
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
            NWPlaceable exit = NWScript.OBJECT_SELF;
            NWArea mainLevel = exit.Area.GetLocalObject("MAIN_LEVEL");
            NWArea restrictedLevel = exit.Area.GetLocalObject("RESTRICTED_LEVEL");
            NWArea directorsChambers = exit.Area.GetLocalObject("DIRECTORS_CHAMBERS");

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
