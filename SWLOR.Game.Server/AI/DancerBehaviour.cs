
using SWLOR.Game.Server.GameObject;
using System;
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
            ActionPlayAnimation(ANIMATION_FIREFORGET_DODGE_SIDE);
            ActionPlayAnimation(ANIMATION_FIREFORGET_SPASM, 3.0F);
            ActionPlayAnimation(ANIMATION_FIREFORGET_VICTORY3, 3.0F);
            ActionPlayAnimation(ANIMATION_FIREFORGET_DODGE_DUCK);
            ActionPlayAnimation(ANIMATION_FIREFORGET_DODGE_SIDE);
            ActionPlayAnimation(ANIMATION_FIREFORGET_VICTORY2, 3.0F);
            ActionPlayAnimation(ANIMATION_FIREFORGET_DODGE_DUCK);
            ActionPlayAnimation(ANIMATION_FIREFORGET_SPASM, 3.0F);
            ActionPlayAnimation(ANIMATION_LOOPING_PAUSE_DRUNK, 3.0F, 1.0F);
            ActionPlayAnimation(ANIMATION_LOOPING_CONJURE1, 3.0F, 0.5F);
            ActionPlayAnimation(ANIMATION_FIREFORGET_DODGE_SIDE);
            ActionPlayAnimation(ANIMATION_LOOPING_PAUSE_DRUNK, 3.0F, 1.0F);
            ActionPlayAnimation(ANIMATION_LOOPING_CONJURE2, 3.0F, 0.5F);
            ActionPlayAnimation(ANIMATION_FIREFORGET_DODGE_SIDE);
            ActionPlayAnimation(ANIMATION_FIREFORGET_VICTORY1, 3.0F);
            ActionDoCommand(() => SetCommandable(TRUE));
            SetCommandable(FALSE);
        }
    }
}