using System;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.CustomEffect
{
    public class BurningEffect: ICustomEffectHandler
    {
        public CustomEffectCategoryType CustomEffectCategoryType => CustomEffectCategoryType.NormalEffect;
        public CustomEffectType CustomEffectType => CustomEffectType.Burning;

        public string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel)
        {
            return null;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int currentTick, int effectiveLevel, string data)
        {
            var random = new Random();
            var amount = random.Next(1, 2);
            oTarget.SetLocalInt(AbilityService.LAST_ATTACK + oCaster.GlobalID, AbilityService.ATTACK_DOT);

            oCaster.AssignCommand(() =>
            {
                var damage = NWScript.EffectDamage(amount, DamageType.Fire);
                NWScript.ApplyEffectToObject(DurationType.Instant, damage, oTarget.Object);
            });

            var vfx = NWScript.EffectVisualEffect(VisualEffect.Vfx_Com_Hit_Fire);
            NWScript.ApplyEffectToObject(DurationType.Instant, vfx, oTarget.Object);
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
        }

        public string StartMessage => "You are on fire.";
        public string ContinueMessage => "You continue to burn...";
        public string WornOffMessage => "You are no longer on fire.";
    }
}
