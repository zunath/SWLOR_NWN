using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Scripts.Item
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
            NWCreature oTarget = NWScript.GetSpellTargetObject();

            if (RandomService.D100(1) > 5) return;

            CustomEffectService.ApplyCustomEffect(NWScript.OBJECT_SELF, oTarget, CustomEffectType.Bleeding, 12, 1, null);
        }
    }
}
