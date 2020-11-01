using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Legacy.Messaging;

namespace SWLOR.Game.Server.Legacy.Event.Feat
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
