using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.GameObject;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Scripts.Placeable.Quests.AbandonedStation
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
            NWObject door = NWScript.OBJECT_SELF;
            string destinationAreaTag = door.GetLocalString("DESTINATION_AREA_TAG");
            string destinationWaypointTag = door.GetLocalString("DESTINATION_WAYPOINT");
            NWArea area = door.Area.GetLocalObject(destinationAreaTag);
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
