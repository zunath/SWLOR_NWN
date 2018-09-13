using System;
using NWN;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.GameObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.CustomEffect
{
    public class ForceBreachEffect : ICustomEffect
    {
        private readonly INWScript _;

        public ForceBreachEffect(INWScript script)
        {
            _ = script;
        }

        public string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel)
        {
            return null;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data = "")
        {
            int damage = 0;
            switch (effectiveLevel)
            {
                case 2:
                case 3:
                case 4:
                    damage = 1;
                    break;
                case 5:
                case 6:
                case 7:
                    damage = 2;
                    break;
                case 8:
                case 9:
                case 10:
                case 11:
                    damage = 4;
                    break;
            }

            oTarget.AssignCommand(() =>
            {
                Effect effect = _.EffectDamage(damage, DAMAGE_TYPE_SONIC);
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, effect, oTarget);
            });
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data = "")
        {
        }
    }
}
