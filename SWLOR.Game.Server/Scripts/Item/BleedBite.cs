using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Legacy;

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
            NWCreature oTarget = NWScript.GetSpellTargetObject();

            if (RandomService.D100(1) > 5) return;

            CustomEffectService.ApplyCustomEffect(NWScript.OBJECT_SELF, oTarget, CustomEffectType.Bleeding, 12, 1, null);
        }
    }
}
