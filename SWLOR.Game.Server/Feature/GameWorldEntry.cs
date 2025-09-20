using SWLOR.Game.Server.Core;
using SWLOR.Shared.Core.Event;

namespace SWLOR.Game.Server.Feature
{
    public class GameWorldEntry
    {
        [ScriptHandler(ScriptName.OnEnterWorld)]
        public static void EnterGameWorld()
        {
            var player = GetPCSpeaker();
            var waypoint = GetObjectByTag("ENTRY_STARTING_WP");
            var location = GetLocation(waypoint);

            AssignCommand(player, () =>
            {
                ActionJumpToLocation(location);
            });
        }
    }
}
