using SWLOR.Game.Server.Legacy.GameObject;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Legacy.Scripts.Placeable.Quests.AbandonedStation
{
    public class TransitionClick: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlayer player = GetClickingObject();
            NWObject door = OBJECT_SELF;
            var doorArea = GetArea(door);
            var destinationAreaTag = door.GetLocalString("DESTINATION_AREA_TAG");
            var destinationWaypointTag = door.GetLocalString("DESTINATION_WAYPOINT");
            uint area = GetLocalObject(doorArea, destinationAreaTag);
            NWObject waypoint = GetNearestObjectByTag(destinationWaypointTag, GetFirstObjectInArea(area));
            NWLocation location = waypoint.Location;
            player.AssignCommand(() => 
            { 
                ActionJumpToLocation(location);
                SetFacing(GetFacing(waypoint));
            });
        }
    }
}
