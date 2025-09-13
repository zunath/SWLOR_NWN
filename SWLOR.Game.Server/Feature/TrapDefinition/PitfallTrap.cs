using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.TrapDefinition
{
    public class PitfallTrap
    {
        /// <summary>
        /// When this trap is triggered, it'll toss the players to the designated waypoint after a delay.
        /// </summary>
        [NWNEventHandler(ScriptName.OnPitfallTrap)]
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
                    SendMessageToPC(player, "Cannot locate waypoint. Please use /bug to report this issue.");
                    return;
                }

                var location = GetLocation(waypoint);
                AssignCommand(player, () => JumpToLocation(location));
            });

            var vfx = EffectVisualEffect(VisualEffect.Vfx_Imp_Dust_Explosion);
            DelayCommand(3f, () => ApplyEffectToObject(DurationType.Instant, vfx, player));
            DelayCommand(1.0f, () => ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), player, 3.0f));
        }
    }
}