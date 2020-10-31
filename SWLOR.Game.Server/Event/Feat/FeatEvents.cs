using System;
using System.Collections.Generic;
using System.Text;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Event.Feat
{
    public static class FeatEvents
    {
        [NWNEventHandler("swlor_craft")]
        public static void UseCraftingFeat()
        {
            MessageHub.Instance.Publish(new OnUseCraftingFeat());
        }

        [NWNEventHandler("onhit_castspell")]
        public static void OnHitCastSpell()
        {
            MessageHub.Instance.Publish(new OnHitCastSpell());
        }
    }
}
