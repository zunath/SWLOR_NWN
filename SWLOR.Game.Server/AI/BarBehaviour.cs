
using SWLOR.Game.Server.GameObject;
using System;
using static SWLOR.Game.Server.NWN._;
using NWN;

namespace SWLOR.Game.Server.AI
{
    public class BarBehaviour : StandardBehaviour
    {
        public override void OnHeartbeat(NWCreature self)
        {
            base.OnHeartbeat(self);

            Bar(self);

        }

        private void Bar(NWCreature self)
        {
            // barActivity local var on each creature is set 0-3. Leaving room for expansion as we may want people in the bar doing different junk on spawn.
            // barActivity Vars:
            // 0: Do nothing
            // 1: Smoking
            // 2: Drinking
            // 3: Sitting in a chair
           int barActivity = self.GetLocalInt("barActivity");
            /* Random head/clothes check
            *
            * 
            */

            switch (barActivity)
            {
                case 1:
                    ActionPlayAnimation(ANIMATION_LOOPING_CUSTOM7, 1.0F, 9999F);
                    break;
                case 2: 
                    ActionPlayAnimation(ANIMATION_LOOPING_CUSTOM9, 1.0F, 9999F);
                    break;
                case 3:
                    NWObject chair = GetNearestObjectByTag("chair", self);
                    ActionSit(chair);
                    break;
                default:
                    break;
            }
        }
    }
}