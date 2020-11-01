using System.Linq;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.CustomEffect.Contracts;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.CustomEffect
{
    public class BalancedStanceEffect: ICustomEffectHandler
    {
        public CustomEffectCategoryType CustomEffectCategoryType => CustomEffectCategoryType.Stance;
        public CustomEffectType CustomEffectType => CustomEffectType.BalancedStance;

        public string Apply(NWCreature oCaster, NWObject target, int effectiveLevel)
        {
            if (target.Effects.Any(x => NWScript.GetEffectTag(x) == "BALANCED_STANCE"))
            {
                return null;
            }

            var effect = NWScript.EffectImmunity(ImmunityType.Knockdown);
            effect = NWScript.TagEffect(effect, "BALANCED_STANCE");

            NWScript.ApplyEffectToObject(DurationType.Permanent, effect, target);
            return null;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int currentTick, int effectiveLevel, string data)
        {
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
            var effect = oTarget.Effects.SingleOrDefault(x => NWScript.GetEffectTag(x) == "BALANCED_STANCE");
            if (effect == null) return;

            NWScript.RemoveEffect(oCaster, effect);
        }

        public string StartMessage => "You shift to a balanced stance.";
        public string ContinueMessage => "";
        public string WornOffMessage => "";
    }
}
