using System.Linq;
using NWN;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using static NWN._;

namespace SWLOR.Game.Server.CustomEffect
{
    public class BalancedStanceEffect: ICustomEffectHandler
    {
        public CustomEffectCategoryType CustomEffectCategoryType => CustomEffectCategoryType.Stance;
        public CustomEffectType CustomEffectType => CustomEffectType.BalancedStance;

        public string Apply(NWCreature oCaster, NWObject target, int effectiveLevel)
        {
            if (target.Effects.Any(x => _.GetEffectTag(x) == "BALANCED_STANCE"))
            {
                return null;
            }

            var effect = _.EffectImmunity(IMMUNITY_TYPE_KNOCKDOWN);
            effect = _.TagEffect(effect, "BALANCED_STANCE");

            _.ApplyEffectToObject(DURATION_TYPE_PERMANENT, effect, target);
            return null;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int currentTick, int effectiveLevel, string data)
        {
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
            var effect = oTarget.Effects.SingleOrDefault(x => _.GetEffectTag(x) == "BALANCED_STANCE");
            if (effect == null) return;

            _.RemoveEffect(oCaster, effect);
        }

        public string StartMessage => "You shift to a balanced stance.";
        public string ContinueMessage => "";
        public string WornOffMessage => "";
    }
}
