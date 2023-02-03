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

                var vfx = EffectVisualEffect(VisualEffect.Vfx_Imp_Dust_Explosion);
                ApplyEffectToObject(DurationType.Instant, vfx, player);

                if (!GetIsObjectValid(waypoint))
                {
                    SendMessageToPC(player, "Cannot locate waypoint. Inform an admin this trap is broken.");
                    return;
                }

                var location = GetLocation(waypoint);
                AssignCommand(player, () => ActionJumpToLocation(location));
            });
        }
    }
}