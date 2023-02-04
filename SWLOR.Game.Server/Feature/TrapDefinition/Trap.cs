using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature
{
    public class PitfallTrap
    {
        /// <summary>
        /// When this trap is triggered, it'll toss the players to the designated waypoint after a delay.
        /// </summary>
        [NWNEventHandler("pitfalltrap")]
        public static void TriggeringTrap()
        {
            var player = GetEnteringObject();
            var destination = GetLocalString(OBJECT_SELF, "DESTINATION");

            AssignCommand(player, () =>
            {
                ClearAllActions();

                var waypoint = GetWaypointByTag(destination);

                if (!GetIsObjectValid(waypoint))
                {
                    SendMessageToPC(player, "Cannot locate waypoint. Inform an admin this trap is broken.");
                    return;
                }

                var location = GetLocation(waypoint);
                AssignCommand(player, () => ActionJumpToLocation(location));
            });

            var vfx = EffectVisualEffect(VisualEffect.Vfx_Imp_Dust_Explosion);

            // Causes a cloud of dust to plume around the character after the fall.
            // This is delayed so that the player can actually see the effect when the load is over. 3 seconds is arbitrary, it's about how long I take to load in, but it coincides with the knockdown time.
            DelayCommand(3f, () => ApplyEffectToObject(DurationType.Instant, vfx, player));

            // This must be a delayed command or it won't work. If the knockdown effect happens at the same time as everything else, the teleportation will not go off.
            DelayCommand(1.0f, () => ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), player, 3.0f));
        }
    }
}