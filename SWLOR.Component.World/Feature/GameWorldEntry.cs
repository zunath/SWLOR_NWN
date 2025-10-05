using SWLOR.Shared.Domain.World.Events;
using SWLOR.Shared.Events.Attributes;

namespace SWLOR.Component.World.Feature
{
    public class GameWorldEntry
    {
        [ScriptHandler<OnEnterWorld>]
        public void EnterGameWorld()
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
