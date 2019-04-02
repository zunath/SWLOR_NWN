using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.CustomEffect
{
    public class ForceSpreadEffect: ICustomEffectHandler
    {
        public CustomEffectCategoryType CustomEffectCategoryType => CustomEffectCategoryType.NormalEffect;
        public CustomEffectType CustomEffectType => CustomEffectType.ForceSpread;

        public string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel)
        {
            return null;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int currentTick, int effectiveLevel, string data)
        {
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
        }

        public string StartMessage => "Your force heals will now target all party members within range.";
        public string ContinueMessage => "";
        public string WornOffMessage => "Your Force Spread effect wears off.";
    }
}
