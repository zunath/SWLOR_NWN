using SWLOR.Game.Server;

using SWLOR.Game.Server.GameObject;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class warp_to_wp
#pragma warning restore IDE1006 // Naming Styles
    {
        public static void Main()
        {
            NWPlayer player = _.GetPCSpeaker();
            NWObject talkingTo = Object.OBJECT_SELF;

            string waypointTag = talkingTo.GetLocalString("DESTINATION");
            NWObject waypoint = _.GetWaypointByTag(waypointTag);

            player.AssignCommand(() =>
            {
                _.ActionJumpToLocation(waypoint.Location);
            });
        }
    }
}
