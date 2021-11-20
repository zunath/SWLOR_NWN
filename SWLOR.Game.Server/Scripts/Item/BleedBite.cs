﻿using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Item
{
    public class BleedBite: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWCreature oTarget = _.GetSpellTargetObject();

            if (RandomService.D100(1) > 5) return;

            CustomEffectService.ApplyCustomEffect(_.OBJECT_SELF, oTarget, CustomEffectType.Bleeding, 12, 1, null);
        }
    }
}
