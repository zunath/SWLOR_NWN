using System;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Enumeration;
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
            Random random = new Random();
            int amount = random.Next(1, 2);
            oTarget.SetLocalInt(AbilityService.LAST_ATTACK + oCaster.GlobalID, AbilityService.ATTACK_DOT);

            oCaster.AssignCommand(() =>
            {
                Effect damage = _.EffectDamage(amount, _.DAMAGE_TYPE_FIRE);
                _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, damage, oTarget.Object);
            });

            Effect vfx = _.EffectVisualEffect(_.VFX_COM_HIT_FIRE);
            _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, vfx, oTarget.Object);
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
        }

        public string StartMessage => "You are on fire.";
        public string ContinueMessage => "You continue to burn...";
        public string WornOffMessage => "You are no longer on fire.";
    }
}
