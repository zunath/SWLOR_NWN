using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Combat.Events;
using SWLOR.Shared.Abstractions.Contracts;
namespace SWLOR.Component.Combat.Feature.TrapDefinition
{
    public class PitfallTrap
    {
        public PitfallTrap(IEventAggregator eventAggregator)
        {
            // Subscribe to events
            eventAggregator.Subscribe<OnPitfallTrap>(e => TriggeringTrap());
        }

        /// <summary>
        /// When this trap is triggered, it'll toss the players to the designated waypoint after a delay.
        /// </summary>
        public void TriggeringTrap()
        {
            var player = GetEnteringObject();
            var destination = GetLocalString(OBJECT_SELF, "DESTINATION");

            AssignCommand(player, () =>
            {
                ClearAllActions();

                var waypoint = GetWaypointByTag(destination);

                if (!GetIsObjectValid(waypoint))
                {
                    SendMessageToPC(player, "Cannot locate waypoint. Please use /bug to report this issue.");
                    return;
                }

                var location = GetLocation(waypoint);
                AssignCommand(player, () => JumpToLocation(location));
            });

            var vfx = EffectVisualEffect(VisualEffectType.Vfx_Imp_Dust_Explosion);
            DelayCommand(3f, () => ApplyEffectToObject(DurationType.Instant, vfx, player));
            DelayCommand(1.0f, () => ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), player, 3.0f));
        }
    }
}
