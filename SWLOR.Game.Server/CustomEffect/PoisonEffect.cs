using System;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.GameObject;

using NWN;

namespace SWLOR.Game.Server.CustomEffect
{
    public class PoisonEffect: ICustomEffect
    {
        private readonly INWScript _;

        public PoisonEffect(INWScript script)
        {
            _ = script;
        }

        public string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel)
        {
            return null;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
            Random random = new Random();
            int amount = random.Next(3, 7);

            oCaster.AssignCommand(() =>
            {
                Effect damage = _.EffectDamage(amount);
                _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, damage, oTarget.Object);
            });
            
            Effect decreaseAC = _.EffectACDecrease(2);
            oCaster.AssignCommand(() =>
            {
                _.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, decreaseAC, oTarget.Object, 1.0f);
            });
            
            _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, _.EffectVisualEffect(NWScript.VFX_IMP_ACID_S), oTarget.Object);
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
        }
    }
}
