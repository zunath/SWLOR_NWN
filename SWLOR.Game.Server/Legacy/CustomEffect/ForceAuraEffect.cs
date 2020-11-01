using SWLOR.Game.Server.Legacy.CustomEffect.Contracts;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.CustomEffect
{
    public class ForceAuraEffect: ICustomEffectHandler
    {
        public CustomEffectCategoryType CustomEffectCategoryType => CustomEffectCategoryType.NormalEffect;
        public CustomEffectType CustomEffectType => CustomEffectType.ForceAura;

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

        public string StartMessage => "You are surrounded by a Force Aura.";
        public string ContinueMessage => "";
        public string WornOffMessage => "You are no longer surrounded by a Force Aura.";
    }
}
