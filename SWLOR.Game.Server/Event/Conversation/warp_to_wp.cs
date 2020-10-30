using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class warp_to_wp
#pragma warning restore IDE1006 // Naming Styles
    {
        public static void Main()
        {
            using (new Profiler(nameof(warp_to_wp)))
            {
                NWPlayer player = NWScript.GetPCSpeaker();
                NWObject talkingTo = NWScript.OBJECT_SELF;

                var waypointTag = talkingTo.GetLocalString("DESTINATION");
                NWObject waypoint = NWScript.GetWaypointByTag(waypointTag);

                player.AssignCommand(() => { NWScript.ActionJumpToLocation(waypoint.Location); });
            }
        }
    }
}
