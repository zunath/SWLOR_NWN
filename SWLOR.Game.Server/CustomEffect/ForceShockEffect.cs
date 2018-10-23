using NWN;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.GameObject;
using System;
using static NWN.NWScript;

namespace SWLOR.Game.Server.CustomEffect
{
    public class ForceShockEffect: ICustomEffect
    {
        private readonly INWScript _;

        public ForceShockEffect(INWScript script)
        {
            _ = script;
        }

        public string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel)
        {
            return null;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int currentTick, int effectiveLevel, string data)
        {
            int damage = Convert.ToInt32(data);
            oCaster.AssignCommand(() =>
            {
                Effect effect = _.EffectDamage(damage, DAMAGE_TYPE_ELECTRICAL);
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, effect, oTarget);

                Console.WriteLine("Goddamnit zunath! I hate all this hard shit.");
            });
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data = "")
        {
        }
    }
}
