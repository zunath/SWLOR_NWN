using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.CustomEffect
{
    public class ChainspellEffect: ICustomEffectHandler
    {
        public CustomEffectCategoryType CustomEffectCategoryType => CustomEffectCategoryType.NormalEffect;
        public CustomEffectType CustomEffectType => CustomEffectType.Chainspell;

        public string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel)
        {
            return null;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int currentTick, int effectiveLevel, string data)
        {
            _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, _.EffectVisualEffect(_.VFX_IMP_EVIL_HELP), oTarget.Object, 1.0f);
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
        }

        public string StartMessage => "You receive the effect of chainspell.";
        public string ContinueMessage => "";
        public string WornOffMessage => "You no longer have the effect of chainspell.";
    }
}
