﻿using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.Scripting.Item
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

            CustomEffectService.ApplyCustomEffect(NWGameObject.OBJECT_SELF, oTarget, CustomEffectType.Bleeding, 12, 1, null);
        }
    }
}
