using SWLOR.Game.Server;

using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.ValueObject;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class warp_to_wp
#pragma warning restore IDE1006 // Naming Styles
    {
        public static void Main()
        {
            using (new Profiler(nameof(warp_to_wp)))
            {
                NWPlayer player = SWLOR.Game.Server.NWScript._.GetPCSpeaker();
                NWObject talkingTo = NWGameObject.OBJECT_SELF;

                string waypointTag = talkingTo.GetLocalString("DESTINATION");
                NWObject waypoint = SWLOR.Game.Server.NWScript._.GetWaypointByTag(waypointTag);

                player.AssignCommand(() => { SWLOR.Game.Server.NWScript._.ActionJumpToLocation(waypoint.Location); });
            }
        }
    }
}
