using NWN;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.GameObject;
using System;
using static NWN.NWScript;

namespace SWLOR.Game.Server.CustomEffect
{
    public class ForceLeechEffect : ICustomEffect
    {
        private readonly INWScript _;

        public ForceLeechEffect(INWScript script)
        {
            _ = script;
        }

        public string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel)
        {
            return null;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int currentTick, int effectiveLevel, string data)
        {
            string[] split = data.Split(',');
            int damage = Convert.ToInt32(split[0]);
            float percent = Convert.ToSingle(split[1]);
            oCaster.AssignCommand(() =>
            {
                Effect effect = _.EffectDamage(damage, DAMAGE_TYPE_NEGATIVE);
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, effect, oTarget);
            });

            // Bail out early if oCaster is no longer valid (player logged out)
            if (oCaster == null || !oCaster.IsValid) return;

            int heal = (int)(damage * percent);
            if (heal < 1) heal = 1;

            oCaster.AssignCommand(() =>
            {
                Effect healEffect = _.EffectHeal(heal);
                _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, healEffect, oCaster);

            });

        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data = "")
        {
        }
    }
}
