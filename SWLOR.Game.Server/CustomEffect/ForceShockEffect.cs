using NWN;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using System;
using static NWN._;

namespace SWLOR.Game.Server.CustomEffect
{
    public class ForceShockEffect: ICustomEffect
    {
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
    }
}
