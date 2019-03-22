using NWN;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.Conversation
{
    public class WarpToWaypoint: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlayer player = _.GetPCSpeaker();
            NWObject talkingTo = Object.OBJECT_SELF;

            string waypointTag = talkingTo.GetLocalString("DESTINATION");
            NWObject waypoint = _.GetWaypointByTag(waypointTag);

            player.AssignCommand(() =>
            {
                _.ActionJumpToLocation(waypoint.Location);
            });

            return true;
        }
    }
}
