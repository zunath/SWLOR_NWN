using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.CustomEffect
{
    public class PrecisionTargetingEffect : ICustomEffectHandler
    {
        public CustomEffectCategoryType CustomEffectCategoryType => CustomEffectCategoryType.Stance;
        public CustomEffectType CustomEffectType => CustomEffectType.PrecisionTargeting;

        public string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel)
        {
            PlayerStatService.ApplyStatChanges(oTarget.Object, null);
            return null;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int currentTick, int effectiveLevel, string data)
        {
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
            PlayerStatService.ApplyStatChanges(oTarget.Object, null);
        }

        public string StartMessage => "You shift to a precision targeting stance.";
        public string ContinueMessage => "";
        public string WornOffMessage => "";
    }
}
