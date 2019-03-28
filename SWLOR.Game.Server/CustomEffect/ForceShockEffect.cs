using NWN;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using System;
using SWLOR.Game.Server.Enumeration;
using static NWN._;

namespace SWLOR.Game.Server.CustomEffect
{
    public class ForceShockEffect: ICustomEffectHandler
    {
        public CustomEffectCategoryType CustomEffectCategoryType => CustomEffectCategoryType.NormalEffect;
        public CustomEffectType CustomEffectType => CustomEffectType.ForceShock;

        public string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel)
        {
            return null;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int currentTick, int effectiveLevel, string data)
        {
            int damage = Convert.ToInt32(data);
            oTarget.SetLocalInt(AbilityService.LAST_ATTACK + oCaster.GlobalID, AbilityService.ATTACK_DOT);

            oCaster.AssignCommand(() =>
            {
                Effect effect = _.EffectDamage(damage, DAMAGE_TYPE_ELECTRICAL);
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, effect, oTarget);
            });
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data = "")
        {
        }

        public string StartMessage => "You have been inflicted with shock.";
        public string ContinueMessage => "You continue to be inflicted with shock.";
        public string WornOffMessage => "You are no longer shocked.";
    }
}
