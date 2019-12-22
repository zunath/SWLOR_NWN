
using SWLOR.Game.Server.GameObject;
using System;
using SWLOR.Game.Server.NWScript.Enumerations;
using static NWN._;

namespace SWLOR.Game.Server.AI
{
    public class DancerBehaviour : StandardBehaviour
    {
        public override void OnHeartbeat(NWCreature self)
        {
            base.OnHeartbeat(self);

            Dance(self);

        }

        private void Dance(NWCreature self)
        {
            ActionPlayAnimation(Animation.FireForget_Dodge_Side);
            ActionPlayAnimation(Animation.FireForget_Spasm, 3.0F);
            ActionPlayAnimation(Animation.FireForget_Victory3, 3.0F);
            ActionPlayAnimation(Animation.FireForget_Dodge_Duck);
            ActionPlayAnimation(Animation.FireForget_Dodge_Side);
            ActionPlayAnimation(Animation.FireForget_Victory2, 3.0F);
            ActionPlayAnimation(Animation.FireForget_Dodge_Duck);
            ActionPlayAnimation(Animation.FireForget_Spasm, 3.0F);
            ActionPlayAnimation(Animation.Pause_Drunk, 3.0F, 1.0F);
            ActionPlayAnimation(Animation.Conjure1, 3.0F, 0.5F);
            ActionPlayAnimation(Animation.FireForget_Dodge_Side);
            ActionPlayAnimation(Animation.Pause_Drunk, 3.0F, 1.0F);
            ActionPlayAnimation(Animation.Conjure2, 3.0F, 0.5F);
            ActionPlayAnimation(Animation.FireForget_Dodge_Side);
            ActionPlayAnimation(Animation.FireForget_Victory1, 3.0F);
            ActionDoCommand(() => SetCommandable(true));
            SetCommandable(false);
        }
    }
}