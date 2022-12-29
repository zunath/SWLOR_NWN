using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.Feature
{
    public class PitfallTrap
    {
        /// <summary>
        /// When this trap is triggered, it'll toss the players to the designated waypoint after a delay.
        /// </summary>
        [NWNEventHandler("OnTrapTriggered")]

        public static void TrapTriggered()

        {
            if (GetTag(OBJECT_SELF) != "PitfallTrap")
                return;

            //Edit next line with the tag of your waypoint, which is where a player who steps onto the pitfall trap should fall to.
            var waypoint = GetWaypointByTag("HutTrapFall");
            var player = GetEnteringObject();

            {
                DelayCommand(1.0f, () =>
                {
                    AssignCommand(player, () =>
                    {
                        ClearAllActions();
                        JumpToObject(waypoint);
                    });
                });
            }
        }
    }
}