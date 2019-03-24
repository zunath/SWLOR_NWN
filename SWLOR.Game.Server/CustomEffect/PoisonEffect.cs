using System;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Enumeration;
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
            Random random = new Random();
            int amount = random.Next(3, 7);
            oTarget.SetLocalInt(AbilityService.LAST_ATTACK + oCaster.GlobalID, AbilityService.ATTACK_DOT);

            oCaster.AssignCommand(() =>
            {
                Effect damage = _.EffectDamage(amount);
                _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, damage, oTarget.Object);
            });
            
            Effect decreaseAC = _.EffectACDecrease(2);
            oCaster.AssignCommand(() =>
            {
                _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, decreaseAC, oTarget.Object, 1.0f);
            });
            
            _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, _.EffectVisualEffect(_.VFX_IMP_ACID_S), oTarget.Object);
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
        }

        public string StartMessage => "You are poisoned.";
        public string ContinueMessage => "Poison continues to course through your body.";
        public string WornOffMessage => "You are no longer poisoned.";
    }
}
