using SWLOR.Shared.Domain.World.Events;
using SWLOR.Shared.Abstractions.Contracts;
namespace SWLOR.Component.World.Feature
{
    public class GameWorldEntry
    {
        public GameWorldEntry(IEventAggregator eventAggregator)
        {
            // Subscribe to events
            eventAggregator.Subscribe<OnEnterWorld>(e => EnterGameWorld());
        }

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
