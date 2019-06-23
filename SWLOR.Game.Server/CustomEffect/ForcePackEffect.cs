using System;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.CustomEffect
{
    public class ForcePackEffect: ICustomEffectHandler
    {
        public CustomEffectCategoryType CustomEffectCategoryType => CustomEffectCategoryType.NormalEffect;
        public CustomEffectType CustomEffectType => CustomEffectType.ForcePack;

        public string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel)
        {
            return null;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int currentTick, int effectiveLevel, string data)
        {
            string[] split = data.Split(',');
            int interval = Convert.ToInt32(split[0]);
            int amount = Convert.ToInt32(split[1]);

            if (currentTick % interval != 0) return;

            AbilityService.RestorePlayerFP(oTarget.Object, amount);
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
        }

        public string StartMessage => "A force pack has been applied on you.";
        public string ContinueMessage => "";
        public string WornOffMessage => "The force pack wears off.";
    }
}
