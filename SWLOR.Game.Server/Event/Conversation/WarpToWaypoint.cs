using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Event.Conversation
{
    internal class WarpToWaypoint
    {
        public static void Main()
        {
            using (new Profiler(nameof(WarpToWaypoint)))
            {
                NWPlayer player = _.GetPCSpeaker();
                NWObject talkingTo = NWGameObject.OBJECT_SELF;

                string waypointTag = talkingTo.GetLocalString("DESTINATION");
                NWObject waypoint = _.GetWaypointByTag(waypointTag);

                player.AssignCommand(() => { _.ActionJumpToLocation(waypoint.Location); });
            }
        }
    }
}
