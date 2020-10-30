using System;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.CustomEffect
{
    public class ForceBreachEffect : ICustomEffectHandler
    {
        public CustomEffectCategoryType CustomEffectCategoryType => CustomEffectCategoryType.NormalEffect;
        public CustomEffectType CustomEffectType => CustomEffectType.ForceBreach;

        public string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel)
        {
            return null;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int currentTick, int effectiveLevel, string data)
        {
            var damage = Convert.ToInt32(data);
            oTarget.SetLocalInt(AbilityService.LAST_ATTACK + oCaster.GlobalID, AbilityService.ATTACK_DOT);

            oCaster.AssignCommand(() =>
            {
                var effect = NWScript.EffectDamage(damage, DamageType.Sonic);
                NWScript.ApplyEffectToObject(DurationType.Instant, effect, oTarget);
            });
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data = "")
        {
        }

        public string StartMessage => "You have been breached by the force.";
        public string ContinueMessage => "You continue to be breached by the force.";
        public string WornOffMessage => "You are no longer breached by the force.";
    }
}
