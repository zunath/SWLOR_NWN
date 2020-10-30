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
    public class PoisonEffect: ICustomEffectHandler
    {
        public CustomEffectCategoryType CustomEffectCategoryType => CustomEffectCategoryType.NormalEffect;
        public CustomEffectType CustomEffectType => CustomEffectType.Poison;

        public string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel)
        {
            return null;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int currentTick, int effectiveLevel, string data)
        {
            var random = new Random();
            var amount = random.Next(3, 7);
            oTarget.SetLocalInt(AbilityService.LAST_ATTACK + oCaster.GlobalID, AbilityService.ATTACK_DOT);

            oCaster.AssignCommand(() =>
            {
                var damage = NWScript.EffectDamage(amount);
                NWScript.ApplyEffectToObject(DurationType.Instant, damage, oTarget.Object);
            });
            
            var decreaseAC = NWScript.EffectACDecrease(2);
            oCaster.AssignCommand(() =>
            {
                NWScript.ApplyEffectToObject(DurationType.Temporary, decreaseAC, oTarget.Object, 1.0f);
            });
            
            NWScript.ApplyEffectToObject(DurationType.Instant, NWScript.EffectVisualEffect(VisualEffect.Vfx_Imp_Acid_S), oTarget.Object);
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
        }

        public string StartMessage => "You are poisoned.";
        public string ContinueMessage => "Poison continues to course through your body.";
        public string WornOffMessage => "You are no longer poisoned.";
    }
}
