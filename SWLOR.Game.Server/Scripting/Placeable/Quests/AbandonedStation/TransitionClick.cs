using System;
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;
using static NWN._;

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
            NWObject door = NWGameObject.OBJECT_SELF;
            string destinationAreaTag = door.GetLocalString("DESTINATION_AREA_TAG");
            string destinationWaypointTag = door.GetLocalString("DESTINATION_WAYPOINT");
            NWArea area = door.Area.GetLocalObject(destinationAreaTag);
            NWObject waypoint = GetNearestObjectByTag(destinationWaypointTag, GetFirstObjectInArea(area));
            NWLocation location = waypoint.Location;
            player.AssignCommand(() => { ActionJumpToLocation(location); });
        }
    }
}
